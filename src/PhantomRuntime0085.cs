// COTW-0085 Phantom runtime bridge. Contract C-0085-20261115-r1.
// Private serializable master state, identity-checked lifecycle actions,
// native rest/respec integration and bounded private handlers for the
// emotion mechanics that have no native Wrath component. No game or UMM is
// launched here; scene-dependent paths are player-TEST boundaries.
using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using Kingmaker;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Facts;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.Designers.Mechanics.Facts;
using Kingmaker.ElementsSystem;
using Kingmaker.EntitySystem;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Enums;
using Kingmaker.GameModes;
using Kingmaker.Enums.Damage;
using Kingmaker.Items;
using Kingmaker.PubSubSystem;
using Kingmaker.RuleSystem;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.RuleSystem.Rules.Damage;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Components.Base;
using Kingmaker.UnitLogic.Buffs;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Buffs.Components;
using Kingmaker.UnitLogic.Abilities.Components.TargetCheckers;
using Kingmaker.UnitLogic.Commands.Base;
using Kingmaker.UnitLogic.FactLogic;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Mechanics.Actions;
using Kingmaker.UnitLogic.Mechanics.Components;
using Kingmaker.UnitLogic.Parts;
using Kingmaker.UnitLogic.ActivatableAbilities;
using Kingmaker.UnitLogic.ActivatableAbilities.Restrictions;
using Kingmaker.UnitLogic.Abilities.Components.AreaEffects;
using Kingmaker.Controllers.Rest;
using Kingmaker.Utility;
using Newtonsoft.Json;
using static CallOfTheWild.Wrath.Prototype.PsychicAssets;

namespace CallOfTheWild.Wrath.Prototype
{
    // ---- Serialized master state (source of truth for Confined intent) ----
    public sealed class UnitPartPhantom0085 : UnitPart
    {
        [JsonProperty] internal bool Confined;
        [JsonProperty] internal string Emotion;
        [JsonProperty] internal string PetId;
        [JsonProperty] internal bool PendingManifest;
        [JsonProperty] internal readonly List<string> GrantedFocus = new List<string>();

        internal UnitEntityData BoundPet(UnitEntityData master)
        {
            if (master == null || string.IsNullOrEmpty(Emotion)) return null;
            // Only the committed primary emotion authorizes companion access.
            var emotionFact = ResourcesLibrary.TryGetBlueprint<BlueprintFeature>(Spiritualist0085Ids.Id(Emotion + "Emotion"));
            if (emotionFact == null || !master.HasFact(emotionFact)) return null;
            var list = master.Get<UnitPartPetMaster>();
            if (list == null) return null;
            UnitEntityData found = null;
            foreach (var reference in list.Pets)
            {
                var pet = reference.Entity;
                var petPart = reference.EntityPart;
                if (pet == null || petPart == null || petPart.Type != Spiritualist0085Ids.PhantomPetType || petPart.Master != master) continue;
                if (!Phantom0085Marker.IsPhantom(pet)) continue;
                if (!string.IsNullOrEmpty(PetId) && pet.UniqueId != PetId) continue;
                if (found != null) return null; // reserved-type collision: keep foreign units, disable this operation
                found = pet;
            }
            if (found != null && string.IsNullOrEmpty(PetId)) PetId = found.UniqueId;
            return found;
        }
    }

    // ---- Unit marker: only marked units of the reserved type are ours ----
    [TypeId("b7e10085a10045008500850085008501")]
    public sealed class Phantom0085Marker : BlueprintComponent
    {
        public string Emotion;
        internal static bool IsPhantom(UnitEntityData unit)
            => unit?.Blueprint?.GetComponent<Phantom0085Marker>() != null;
        internal static string EmotionOf(UnitEntityData unit)
            => unit?.Blueprint?.GetComponent<Phantom0085Marker>()?.Emotion;
    }

    // ---- Emotion link: owns the master state and the lifecycle events ----
    [TypeId("b7e10085a10045008500850085008502")]
    public sealed class Phantom0085Link : UnitFactComponentDelegate, IAreaHandler, IUnitRestHandler, IUnitHandler, IGlobalSubscriber
    {
        public string Emotion;

        protected override void OnActivate()
        {
            var master = Owner;
            if (master == null || master.IsPet || !master.PreviewOf.IsEmpty) return;
            var part = master.Ensure<UnitPartPhantom0085>();
            part.Emotion = Emotion;
            EventBus.Subscribe(this);
            Reproject(master, part);
            Phantom0085ConsciousnessGrant.RefreshAll(master);
        }

        protected override void OnDeactivate()
        {
            if (IsReapplying) return;
            var master = Owner;
            EventBus.Unsubscribe(this);
            var part = master?.Get<UnitPartPhantom0085>();
            if (part == null || part.Emotion != Emotion) return;
            // Release only this emotion's owned focus grants and effects.
            foreach (var guid in part.GrantedFocus.ToArray())
            {
                var focus = ResourcesLibrary.TryGetBlueprint<BlueprintFeature>(BlueprintGuid.Parse(guid));
                if (focus != null && master.HasFact(focus)) master.RemoveFact(focus);
                part.GrantedFocus.Remove(guid);
            }
            // Deterministic cleanup: the emotion is gone, so every focus this
            // master state tracks must be released regardless of fact-list
            // ordering during native deactivation.
            foreach (var key in part.GrantedFocus.ToArray())
            {
                var focus = ResourcesLibrary.TryGetBlueprint<BlueprintFeature>(BlueprintGuid.Parse(key));
                if (focus != null && master.HasFact(focus)) master.RemoveFact(focus);
                part.GrantedFocus.Remove(key);
            }
            Phantom0085ConsciousnessGrant.RefreshAll(master);
            part.Confined = false;
            part.PendingManifest = false;
            part.PetId = null;
            part.Emotion = null;
        }

        public void OnAreaBeginUnloading() { }
        public void OnAreaDidLoad()
        {
            var master = Owner;
            var part = master?.Get<UnitPartPhantom0085>();
            if (part != null) Reproject(master, part);
        }

        public void HandleUnitRest(UnitEntityData unit)
        {
            if (unit != Owner) return;
            var part = unit.Get<UnitPartPhantom0085>();
            if (part == null || part.Emotion != Emotion) return;
            // Completed rest releases living confinement (original RemoveOnRest).
            var pet = part.BoundPet(unit);
            ClearBonded(unit);
            if (pet != null && !pet.Descriptor.State.IsFinallyDead && part.Confined)
            {
                part.Confined = false;
                part.PendingManifest = !CanPlace(unit);
                if (!part.PendingManifest) ManifestNow(unit, pet, part);
            }
            Reproject(unit, part);
            Phantom0085ConsciousnessGrant.RefreshAll(unit);
        }

        public void HandleUnitSpawned(UnitEntityData unit) { }
        public void HandleUnitDestroyed(UnitEntityData unit)
        {
            var part = Owner?.Get<UnitPartPhantom0085>();
            if (part != null && unit.UniqueId == part.PetId) part.PetId = null;
        }
        public void HandleUnitDeath(UnitEntityData unit)
        {
            var part = Owner?.Get<UnitPartPhantom0085>();
            if (part == null || unit.UniqueId != part.PetId) return;
            // Death suppresses active 0085 linkage; native death state stays.
            part.Confined = false;
            part.PendingManifest = false;
            ClearBonded(Owner);
            Phantom0085ConsciousnessGrant.RefreshAll(Owner);
        }

        internal static bool CanPlace(UnitEntityData master)
            => master != null && master.IsInGame && master.HoldingState != null &&
               Game.Instance?.CurrentlyLoadedArea != null && !Game.Instance.IsModeActive(GameModeType.GlobalMap);

        internal static void Reproject(UnitEntityData master, UnitPartPhantom0085 part)
        {
            var pet = part.BoundPet(master);
            if (pet == null) return;
            var petPart = pet.Get<UnitPartPet>();
            if (petPart == null) return;
            if (pet.Descriptor.State.IsFinallyDead) return;
            if (part.Confined)
            {
                if (!petPart.ShouldBeHidden) petPart.SetHidden(true);
            }
            else if (part.PendingManifest)
            {
                if (CanPlace(master))
                {
                    part.PendingManifest = false;
                    ManifestNow(master, pet, part);
                }
            }
            else if (petPart.ShouldBeHidden)
            {
                ClearBonded(master);
                petPart.SetHidden(false);
            }
            Phantom0085ConsciousnessGrant.RefreshAll(master);
        }

        internal static void ManifestNow(UnitEntityData master, UnitEntityData pet, UnitPartPhantom0085 part)
        {
            var petPart = pet.Get<UnitPartPet>();
            if (petPart == null) return;
            ClearBonded(master);
            petPart.SetHidden(false);
            petPart.TeleportToMaster();
            part.Confined = false;
        }

        internal static void ClearBonded(UnitEntityData master)
        {
            if (master == null) return;
            var bonded = master.Buffs.Enumerable.FirstOrDefault(b => b.Blueprint.AssetGuid == Spiritualist0085Ids.Id("BondedManifestationEffect"));
            if (bonded != null) bonded.Remove();
        }

        internal static bool Alive(UnitEntityData pet)
            => pet != null && !pet.Descriptor.State.IsDead && !pet.Descriptor.State.IsFinallyDead;
    }

    // ---- Alignment sync: phantom mirrors the master ----
    [TypeId("b7e10085a10045008500850085008503")]
    public sealed class Phantom0085AlignmentSync : UnitFactComponentDelegate, IUnitHandler
    {
        protected override void OnActivate()
        {
            Sync();
            EventBus.Subscribe(this);
        }
        protected override void OnDeactivate() => EventBus.Unsubscribe(this);
        private void Sync()
        {
            var master = Owner;
            var part = master?.Get<UnitPartPhantom0085>();
            var pet = part?.BoundPet(master);
            if (pet != null && pet.Descriptor.Alignment.ValueRaw != master.Descriptor.Alignment.ValueRaw)
                pet.Descriptor.Alignment.Set(master.Descriptor.Alignment.ValueRaw);
        }
        public void HandleUnitSpawned(UnitEntityData unit) => Sync();
        public void HandleUnitDestroyed(UnitEntityData unit) { }
        public void HandleUnitDeath(UnitEntityData unit) { }
    }

    // ---- Slam dice replacement (native rulebook, bounded to natural slams) ----
    [TypeId("b7e10085a10045008500850085008504")]
    public sealed class Phantom0085SlamDice : UnitFactComponentDelegate, IInitiatorRulebookHandler<RuleCalculateWeaponStats>, IInitiatorRulebookSubscriber
    {
        public bool Large;
        private static readonly DiceFormula[] Standard = { new DiceFormula(1, DiceType.D6), new DiceFormula(1, DiceType.D8), new DiceFormula(1, DiceType.D10), new DiceFormula(2, DiceType.D6), new DiceFormula(2, DiceType.D8) };
        private static readonly DiceFormula[] Anger = { new DiceFormula(1, DiceType.D8), new DiceFormula(2, DiceType.D6), new DiceFormula(2, DiceType.D8), new DiceFormula(3, DiceType.D6), new DiceFormula(3, DiceType.D8) };
        public void OnEventAboutToTrigger(RuleCalculateWeaponStats evt) { }
        public void OnEventDidTrigger(RuleCalculateWeaponStats evt)
        {
            if (evt.Weapon?.Blueprint?.Category != WeaponCategory.OtherNaturalWeapons) return;
            var rank = Math.Max(0, Math.Min(4, Context?[AbilityRankType.Default] ?? 0));
            var dice = (Large ? Anger : Standard)[rank];
            evt.WeaponDamageDice.Modify(dice, Fact);
        }
    }

    // ---- Emotion skill ranks equal to phantom class level ----
    [TypeId("b7e10085a10045008500850085008505")]
    public sealed class Phantom0085SkillRanks : UnitFactComponentDelegate
    {
        public StatType[] Skills;
        protected override void OnActivate() => Apply();
        protected override void OnDeactivate() { }
        internal void Apply()
        {
            var unit = Owner;
            if (unit == null || Skills == null) return;
            int level = unit.Progression.GetClassLevel(unit.Blueprint.GetComponent<Phantom0085Marker>() != null
                ? ResourcesLibrary.TryGetBlueprint<BlueprintCharacterClass>(Spiritualist0085Ids.Id("PhantomClass")) : null);
            if (level <= 0) level = unit.Descriptor.Progression.CharacterLevel;
            foreach (var skill in Skills)
            {
                var value = unit.Stats.GetStat<ModifiableValueSkill>(skill);
                if (value != null && value.BaseValue < level) value.BaseValue = level;
            }
        }
    }

    // ---- Power from Despair: +2 attack/damage vs feared targets ----
    [TypeId("b7e10085a10045008500850085008506")]
    public sealed class Phantom0085PowerFromDespair : UnitFactComponentDelegate,
        IInitiatorRulebookHandler<RuleCalculateAttackBonus>, IInitiatorRulebookHandler<RuleCalculateDamage>, IInitiatorRulebookSubscriber
    {
        private static readonly BlueprintGuid[] Checked =
        {
            BlueprintGuid.Parse(Spiritualist0085Ids.ShakenBuffId),
            BlueprintGuid.Parse(Spiritualist0085Ids.FrightenedBuffId),
            BlueprintGuid.Parse(Spiritualist0085Ids.CrushingDespairBuffId),
            Spiritualist0085Ids.Id("DespairAuraOfDespairEffect")
        };
        private static bool HasFear(UnitEntityData target)
        {
            if (target == null) return false;
            foreach (var buff in target.Buffs.Enumerable)
                if (Checked.Contains(buff.Blueprint.AssetGuid)) return true;
            return false;
        }
        public void OnEventAboutToTrigger(RuleCalculateAttackBonus evt) { }
        public void OnEventDidTrigger(RuleCalculateAttackBonus evt)
        {
            if (HasFear(evt.Target)) evt.AddModifier(2, Fact, ModifierDescriptor.UntypedStackable);
        }
        public void OnEventAboutToTrigger(RuleCalculateDamage evt) { }
        public void OnEventDidTrigger(RuleCalculateDamage evt)
        {
            if (!HasFear(evt.Target)) return;
            ((DamageBundle)evt.DamageBundle).Add(new DirectDamage(DiceFormula.Zero, 2));
        }
    }

    // ---- Hateful Aura: retaliation damage on attackers of pet or master ----
    [TypeId("b7e10085a10045008500850085008507")]
    public sealed class Phantom0085HatefulAura : UnitFactComponentDelegate, IGlobalRulebookHandler<RuleDealDamage>, IGlobalSubscriber
    {
        public void OnEventAboutToTrigger(RuleDealDamage evt) { }
        public void OnEventDidTrigger(RuleDealDamage evt)
        {
            var attacker = evt.Initiator;
            var target = evt.Target;
            if (attacker == null || target == null || !attacker.HasFact(Fact)) return;
            var phantom = Owner;
            var master = phantom?.Get<UnitPartPet>()?.Master;
            if (target != phantom && target != master) return;
            if (attacker.DistanceTo(phantom) > 20.Feet().Meters) return;
            int bonus = Math.Max(0, phantom.Stats.Charisma.Bonus);
            if (bonus > 0)
                Rulebook.Trigger(new RuleDealDamage(phantom, attacker, new DamageBundle(new DirectDamage(DiceFormula.Zero, bonus))));
        }
    }

    // ---- Zeal's Resolve: reroll failed attack/save, spend resource ----
    [TypeId("b7e10085a10045008500850085008508")]
    public sealed class Phantom0085Reroll : UnitBuffComponentDelegate, IInitiatorRulebookHandler<RuleRollD20>, IInitiatorRulebookSubscriber
    {
        public RuleType Rule;
        public BlueprintAbilityResourceReference Resource;
        public void OnEventAboutToTrigger(RuleRollD20 evt) { }
        public void OnEventDidTrigger(RuleRollD20 evt)
        {
            if (!Buff.IsTurnedOn || Buff.IsSuppressed || evt.IsFake || evt.Initiator != Owner) return;
            if (evt.Rerolls?.Any(r => ReferenceEquals(r.Source, Fact)) == true) return;
            var previous = Rulebook.CurrentContext?.PreviousEvent;
            if (Rule == RuleType.AttackRoll)
            {
                var attack = previous as RuleAttackRoll;
                if (attack == null || attack.IsHit) return;
            }
            else
            {
                var save = previous as RuleSavingThrow;
                if (save == null || save.IsPassed) return;
            }
            var resource = Resource?.Get();
            if (resource == null || Owner.Resources.GetResourceAmount(resource) <= 0) return;
            Owner.Resources.Spend(resource, 1);
            evt.AddReroll(1, true, Fact);
        }
    }

    // ---- Opening Strike: designate a threatening ally for a swift attack ----
    [TypeId("b7e10085a10045008500850085008509")]
    public sealed class Phantom0085OpeningStrike : ContextAction
    {
        public BlueprintBuffReference TargetBuff;
        public BlueprintBuffReference AllyBuff;
        public override string GetCaption() => "Opening Strike";
        public override void RunAction()
        {
            var caster = Context?.MaybeCaster;
            var target = Target?.Unit;
            if (caster == null || target == null) return;
            var attack = new RuleAttackWithWeapon(caster, target, caster.Body.PrimaryHand?.Weapon, 0);
            Rulebook.Trigger(attack);
            if (attack.AttackRoll == null || !attack.AttackRoll.IsHit) return;
            var targetBuff = TargetBuff?.Get();
            if (targetBuff != null) target.Buffs.AddBuff(targetBuff, caster, TimeSpan.MaxValue, AbilityParams.Empty);
            var allyBuff = AllyBuff?.Get();
            if (allyBuff == null) return;
            foreach (var unit in Game.Instance.Player.AllCharacters)
            {
                if (unit == caster || unit == target || !unit.IsPlayerFaction) continue;
                if (unit.DistanceTo(target) <= (unit.Body.PrimaryHand?.Weapon?.AttackRange.Meters ?? 5f))
                {
                    unit.Buffs.AddBuff(allyBuff, caster, TimeSpan.MaxValue, AbilityParams.Empty);
                    break;
                }
            }
        }
    }

    // ---- Etheric Healing target restriction: self or master ----
    [TypeId("b7e10085a1004500850085008500850a")]
    public sealed class Phantom0085TargetSelfOrMaster : BlueprintComponent, IAbilityTargetRestriction
    {
        public bool IsTargetRestrictionPassed(UnitEntityData caster, TargetWrapper target)
            => target?.Unit != null && (target.Unit == caster || target.Unit == caster?.Get<UnitPartPet>()?.Master);
        public string GetAbilityTargetRestrictionUIText(UnitEntityData caster, TargetWrapper target)
            => "The target must be the phantom itself or its master.";
    }

    // ---- Aid another action type (Wrath native command, bounded patch) ----
    [TypeId("b7e10085a1004500850085008500850b")]
    public sealed class Phantom0085AidAnotherActionType : UnitFactComponentDelegate
    {
        public UnitCommand.CommandType ActionType;
        protected override void OnActivate() => Phantom0085Patches.RegisterAidAnother(Owner, this);
        protected override void OnDeactivate() => Phantom0085Patches.UnregisterAidAnother(Owner, this);
    }

    // ---- Grant a selection N times (mercy picks) ----
    [TypeId("b7e10085a1004500850085008500850c")]
    public sealed class Phantom0085GrantSelection : UnitFactComponentDelegate
    {
        public BlueprintFeatureSelectionReference Selection;
        public int Count = 1;
        protected override void OnActivate()
        {
            var selection = Selection?.Get();
            var unit = Owner;
            if (selection == null || unit == null) return;
            for (int i = 0; i < Count; i++)
                if (!unit.HasFact(selection)) unit.Progression.Features.AddFeature(selection);
        }
    }

    // ---- Benevolent: aid another grants +2 more (bounded patch) ----
    [TypeId("b7e10085a1004500850085008500850d")]
    public sealed class Phantom0085Benevolent : UnitFactComponentDelegate
    {
        protected override void OnActivate() => Phantom0085Patches.RegisterBenevolent(Owner);
        protected override void OnDeactivate() => Phantom0085Patches.UnregisterBenevolent(Owner);
    }

    // ---- Shared/Fused Consciousness: deduplicated Skill Focus + save buff ----
    [TypeId("b7e10085a1004500850085008500850e")]
    public sealed class Phantom0085ConsciousnessGrant : UnitFactComponentDelegate
    {
        public BlueprintProgressionReference Emotion;
        public BlueprintFeatureReference Focus;
        public BlueprintFeatureReference Fused;
        public BlueprintBuffReference SaveBuff;
        public bool Permanent;

        protected override void OnActivate() => RefreshAll(Owner);
        protected override void OnDeactivate() => RefreshAll(Owner);

        private bool Eligible(UnitEntityData master, UnitPartPhantom0085 part)
        {
            var emotion = Emotion?.Get();
            if (emotion == null || !master.HasFact(emotion)) return false;
            if (Permanent) return true;
            var fused = Fused?.Get();
            var pet = part.BoundPet(master);
            return part.Confined && pet != null && !pet.Descriptor.State.IsFinallyDead &&
                (fused == null || !master.HasFact(fused));
        }

        // One coordinator reconciles every grant on the master so shared and
        // fused never double-grant or strip each other's Skill Focus.
        internal static void RefreshAll(UnitEntityData master)
        {
            if (master == null) return;
            var part = master.Get<UnitPartPhantom0085>();
            if (part == null) return;
            var grants = new List<Phantom0085ConsciousnessGrant>();
            foreach (var fact in master.Facts.List)
            {
                if (!fact.IsTurnedOn) continue;
                foreach (var component in fact.Components)
                    if (component.SourceBlueprintComponent is Phantom0085ConsciousnessGrant grant)
                        grants.Add(grant);
            }
            var desired = new HashSet<BlueprintGuid>();
            bool sharedActive = false, fusedActive = false;
            foreach (var grant in grants)
            {
                if (!grant.Eligible(master, part)) continue;
                var focus = grant.Focus?.Get();
                if (focus != null) desired.Add(focus.AssetGuid);
                if (grant.Permanent) fusedActive = true; else sharedActive = true;
            }
            // Reconcile Skill Focus: add missing desired, remove tracked stale.
            foreach (var guid in desired)
            {
                var focus = ResourcesLibrary.TryGetBlueprint<BlueprintFeature>(guid);
                if (focus != null && !master.HasFact(focus))
                {
                    master.AddFact(focus);
                    var key = guid.ToString();
                    if (!part.GrantedFocus.Contains(key)) part.GrantedFocus.Add(key);
                }
            }
            foreach (var key in part.GrantedFocus.ToArray())
            {
                var guid = BlueprintGuid.Parse(key);
                if (desired.Contains(guid)) continue;
                var focus = ResourcesLibrary.TryGetBlueprint<BlueprintFeature>(guid);
                if (focus != null && master.HasFact(focus)) master.RemoveFact(focus);
                part.GrantedFocus.Remove(key);
            }
            // Reconcile the mode save buffs.
            foreach (var grant in grants)
            {
                var buff = grant.SaveBuff?.Get();
                if (buff == null) continue;
                bool active = grant.Permanent ? fusedActive : sharedActive;
                var present = master.Buffs.GetBuff(buff) != null;
                if (active && !present) master.Buffs.AddBuff(buff, master, TimeSpan.MaxValue, AbilityParams.Empty);
                else if (!active && present) master.Buffs.GetBuff(buff).Remove();
            }
        }
    }

    // ---- Etheric Tether / Spiritual Bond damage transfer ----
    [TypeId("b7e10085a1004500850085008500850f")]
    public sealed class Phantom0085DamageTransfer : UnitFactComponentDelegate, IGlobalRulebookHandler<RuleCalculateDamage>, IGlobalSubscriber
    {
        public bool SpiritualBond;
        public void OnEventAboutToTrigger(RuleCalculateDamage evt) { }
        public void OnEventDidTrigger(RuleCalculateDamage evt)
        {
            var deal = evt.ParentRule as RuleDealDamage;
            if (deal == null || deal.IsFake || deal.IsDot) return;
            var master = Owner;
            var part = master?.Get<UnitPartPhantom0085>();
            var pet = part?.BoundPet(master);
            if (pet == null || !Phantom0085Link.Alive(pet)) return;
            var petPart = pet.Get<UnitPartPet>();
            if (petPart == null || petPart.ShouldBeHidden || pet.HoldingState != master.HoldingState) return;
            int result = 0;
            foreach (var value in evt.CalculatedDamage) result += value.FinalValue;
            if (result <= 0) return;
            int con = pet.Stats.Constitution.ModifiedValue;
            int amount;
            UnitEntityData target;
            if (SpiritualBond)
            {
                // C14: keep the master at 1 HP; phantom absorbs down to 1-CON.
                if (deal.Target != master || pet.Damage < 1) return;
                int excess = 1 - (master.Damage - result);
                int capacity = pet.Damage - (1 - con);
                amount = Math.Min(excess, Math.Max(0, capacity));
                target = pet;
            }
            else
            {
                // C13: pull lethal overflow back to 1-CON; master pays down to 1 HP.
                if (deal.Target != pet) return;
                int overflow = (1 - con) - (pet.Damage - result);
                amount = Math.Min(overflow, Math.Max(0, master.Damage - 1));
                target = master;
            }
            if (amount <= 0) return;
            // Native redirection is an integer percent of the final Result;
            // round up so the transferred amount is exact below 100 damage.
            deal.RedirectedPercent = (amount * 100 + result - 1) / result;
            deal.RedirectionTarget = target;
        }
    }

    // ---- Bonded Manifestation: tendrils and confinement restriction ----
    [TypeId("b7e10085a10045008500850085008510")]
    public sealed class Phantom0085BondedTendrils : UnitFactComponentDelegate
    {
        // Tendril attacks are granted by the master progression at 8/18; the
        // buff only carries the shield. Scene attack visuals are player TEST.
    }

    [TypeId("b7e10085a10045008500850085008511")]
    public sealed class Phantom0085BondedRestriction : ActivatableAbilityRestriction
    {
        protected override bool IsAvailable()
        {
            var master = Owner;
            var part = master?.Get<UnitPartPhantom0085>();
            var pet = part?.BoundPet(master);
            return part != null && part.Confined && pet != null && Phantom0085Link.Alive(pet);
        }
    }

    // ---- Spiritual Inference aura: owner/ally buffs, condition gated ----
    [TypeId("b7e10085a10045008500850085008512")]
    public sealed class Phantom0085InferenceEnter : ContextAction
    {
        public bool Greater;
        public BlueprintBuffReference OwnerBuff;
        public BlueprintBuffReference AllyBuff;
        public override string GetCaption() => "Spiritual Inference";
        public override void RunAction()
        {
            var phantom = Context?.MaybeCaster;
            var unit = Target?.Unit;
            var master = phantom?.Get<UnitPartPet>()?.Master;
            if (master == null || unit == null || !Phantom0085Link.Alive(phantom)) return;
            if (phantom.Descriptor.State.HasCondition(UnitCondition.Helpless) ||
                phantom.Descriptor.State.HasCondition(UnitCondition.Unconscious)) return;
            var buff = (unit == master ? OwnerBuff : (Greater ? AllyBuff : null))?.Get();
            if (buff != null && unit.Buffs.GetBuff(buff) == null) unit.Buffs.AddBuff(buff, phantom, TimeSpan.MaxValue, AbilityParams.Empty);
        }
    }

    [TypeId("b7e10085a10045008500850085008518")]
    public sealed class Phantom0085InferenceExit : ContextAction
    {
        public BlueprintBuffReference OwnerBuff;
        public BlueprintBuffReference AllyBuff;
        public override string GetCaption() => "Spiritual Inference end";
        public override void RunAction()
        {
            var unit = Target?.Unit;
            if (unit == null) return;
            foreach (var reference in new[] { OwnerBuff, AllyBuff })
            {
                var buff = reference?.Get();
                if (buff != null && unit.Buffs.GetBuff(buff) != null) unit.Buffs.GetBuff(buff)?.Remove();
            }
        }
    }

    // ---- Phantom Recall: move only the bound manifested phantom ----
    [TypeId("b7e10085a10045008500850085008513")]
    public sealed class Phantom0085Recall : ContextAction
    {
        public override string GetCaption() => "Phantom Recall";
        public override void RunAction()
        {
            var master = Context?.MaybeCaster;
            var part = master?.Get<UnitPartPhantom0085>();
            var pet = part?.BoundPet(master);
            if (pet == null || part.Confined || !Phantom0085Link.Alive(pet)) return;
            if (pet.HoldingState != master.HoldingState) return;
            var petPart = pet.Get<UnitPartPet>();
            if (petPart == null || petPart.ShouldBeHidden) return;
            // Already at the landing point: redundant call spends nothing.
            if (pet.DistanceTo(master) <= 5.Feet().Meters) return;
            var resource = ResourcesLibrary.TryGetBlueprint<BlueprintAbilityResource>(Spiritualist0085Ids.Id("PhantomRecallResource"));
            if (resource == null || master.Resources.GetResourceAmount(resource) < 1) return;
            master.Resources.Spend(resource, 1);
            petPart.TeleportToMaster();
        }
    }

    // ---- Potent Phantom: secondary emotion's four capabilities ----
    [TypeId("b7e10085a10045008500850085008514")]
    public sealed class Phantom0085PotentGrant : UnitFactComponentDelegate
    {
        public BlueprintProgressionReference Emotion;
        protected override void OnActivate()
        {
            var master = Owner;
            var part = master?.Get<UnitPartPhantom0085>();
            var pet = part?.BoundPet(master);
            var emotion = Emotion?.Get();
            if (pet == null || emotion == null) return;
            // Only the secondary emotion's 1/7/12/17 pet capabilities apply to
            // the same phantom; no second AddPet, no master SLA side effects.
            foreach (var entry in emotion.LevelEntries)
            {
                if (entry.Level != 1 && entry.Level != 7 && entry.Level != 12 && entry.Level != 17) continue;
                foreach (var reference in entry.Features)
                {
                    var grantFeature = reference as BlueprintFeature;
                    var toPet = grantFeature?.GetComponent<AddFeatureToPet>();
                    if (toPet == null) continue;
                    var inner = (AccessTools.Field(toPet.GetType(), "m_Feature").GetValue(toPet) as BlueprintFeatureReference)?.Get();
                    if (inner != null && !pet.HasFact(inner)) pet.Progression.Features.AddFeature(inner);
                }
            }
        }
    }

    // ---- Bonded Manifestation tendrils ----
    [TypeId("b7e10085a10045008500850085008519")]
    public sealed class Phantom0085TendrilDice : UnitFactComponentDelegate, IInitiatorRulebookHandler<RuleCalculateWeaponStats>, IInitiatorRulebookSubscriber
    {
        private static readonly DiceFormula[] Standard = { new DiceFormula(1, DiceType.D6), new DiceFormula(1, DiceType.D8), new DiceFormula(1, DiceType.D10), new DiceFormula(2, DiceType.D6), new DiceFormula(2, DiceType.D8) };
        private static readonly DiceFormula[] Anger = { new DiceFormula(1, DiceType.D8), new DiceFormula(2, DiceType.D6), new DiceFormula(2, DiceType.D8), new DiceFormula(3, DiceType.D6), new DiceFormula(3, DiceType.D8) };
        public void OnEventAboutToTrigger(RuleCalculateWeaponStats evt) { }
        public void OnEventDidTrigger(RuleCalculateWeaponStats evt)
        {
            if (evt.Weapon?.Blueprint?.Category != WeaponCategory.OtherNaturalWeapons) return;
            var part = Owner?.Get<UnitPartPhantom0085>();
            bool large = part != null && part.Emotion == "Anger";
            var rank = Math.Max(0, Math.Min(4, Context?[AbilityRankType.Default] ?? 0));
            evt.WeaponDamageDice.Modify((large ? Anger : Standard)[rank], Fact);
        }
    }

    [TypeId("b7e10085a1004500850085008500851a")]
    public sealed class Phantom0085TendrilAttack : ContextAction
    {
        public bool FullRound;
        public BlueprintBuffReference DiceBuff;
        public BlueprintBuffReference RerollBuff;
        public BlueprintItemWeaponReference Slam;
        public override string GetCaption() => "Bonded Manifestation tendrils";
        public override void RunAction()
        {
            var caster = Context?.MaybeCaster;
            if (caster == null) return;
            var slamBlueprint = Slam?.Get();
            var dice = DiceBuff?.Get();
            if (slamBlueprint == null || dice == null) return;
            var slam = new Kingmaker.Items.ItemEntityWeapon(slamBlueprint);
            var diceFact = caster.Buffs.AddBuff(dice, caster, TimeSpan.MaxValue, AbilityParams.Empty);
            Buff rerollFact = null;
            var reroll = RerollBuff?.Get();
            if (FullRound && reroll != null)
                rerollFact = caster.Buffs.AddBuff(reroll, caster, TimeSpan.MaxValue, AbilityParams.Empty);
            try
            {
                if (FullRound)
                {
                    foreach (var unit in Game.Instance.Player.AllCharacters)
                    {
                        if (unit == caster || !unit.IsEnemy(caster) || unit.State.IsDead) continue;
                        if (unit.DistanceTo(caster) > 5.Feet().Meters) continue;
                        Rulebook.Trigger(new RuleAttackWithWeapon(caster, unit, slam, 0) { Reason = new RuleReason(Context) });
                    }
                }
                else
                {
                    var target = Target?.Unit;
                    if (target != null && !target.State.IsDead)
                        Rulebook.Trigger(new RuleAttackWithWeapon(caster, target, slam, 0) { Reason = new RuleReason(Context) });
                }
            }
            finally
            {
                diceFact?.Remove();
                rerollFact?.Remove();
            }
        }
    }

    // ---- Confine / Manifest actions ----
    [TypeId("b7e10085a10045008500850085008515")]
    public sealed class Phantom0085Confine : ContextAction
    {
        public override string GetCaption() => "Confine Phantom";
        public override void RunAction()
        {
            var master = Context?.MaybeCaster;
            var part = master?.Get<UnitPartPhantom0085>();
            var pet = part?.BoundPet(master);
            if (pet == null || part.Confined) return;
            if (!Phantom0085Link.Alive(pet) || pet.Damage < 1) return;
            if (pet.HoldingState != master.HoldingState) return;
            var petPart = pet.Get<UnitPartPet>();
            if (petPart == null || petPart.ShouldBeHidden) return;
            petPart.SetHidden(true);
            part.Confined = true;
            Phantom0085ConsciousnessGrant.RefreshAll(master);
        }
    }

    [TypeId("b7e10085a10045008500850085008516")]
    public sealed class Phantom0085Manifest : ContextAction
    {
        public override string GetCaption() => "Manifest Phantom";
        public override void RunAction()
        {
            var master = Context?.MaybeCaster;
            var part = master?.Get<UnitPartPhantom0085>();
            var pet = part?.BoundPet(master);
            if (pet == null || !part.Confined || !Phantom0085Link.Alive(pet)) return;
            if (!Phantom0085Link.CanPlace(master)) { part.PendingManifest = true; return; }
            // Bonded Manifestation must end before the phantom manifests.
            var bonded = master.Buffs.Enumerable.FirstOrDefault(b => b.Blueprint.AssetGuid == Spiritualist0085Ids.Id("BondedManifestationEffect"));
            if (bonded != null) bonded.Remove();
            Phantom0085Link.ManifestNow(master, pet, part);
            Phantom0085ConsciousnessGrant.RefreshAll(master);
        }
    }

    [TypeId("b7e10085a10045008500850085008517")]
    public sealed class Phantom0085NoOp : BlueprintComponent { }

    // ---- Bounded Harmony patches: mount, rest fallback, aid another ----
    internal static class Phantom0085Patches
    {
        private static readonly Dictionary<UnitEntityData, List<Phantom0085AidAnotherActionType>> AidAnother =
            new Dictionary<UnitEntityData, List<Phantom0085AidAnotherActionType>>();
        private static readonly HashSet<UnitEntityData> Benevolent = new HashSet<UnitEntityData>();
        internal static void RegisterAidAnother(UnitEntityData unit, Phantom0085AidAnotherActionType component)
        {
            if (unit == null) return;
            if (!AidAnother.TryGetValue(unit, out var list)) AidAnother[unit] = list = new List<Phantom0085AidAnotherActionType>();
            list.Add(component);
        }
        internal static void UnregisterAidAnother(UnitEntityData unit, Phantom0085AidAnotherActionType component)
        {
            if (unit != null && AidAnother.TryGetValue(unit, out var list)) list.Remove(component);
        }
        internal static void RegisterBenevolent(UnitEntityData unit) { if (unit != null) Benevolent.Add(unit); }
        internal static void UnregisterBenevolent(UnitEntityData unit) { if (unit != null) Benevolent.Remove(unit); }
        internal static bool HasBenevolent(UnitEntityData unit) => unit != null && Benevolent.Contains(unit);
        internal static UnitCommand.CommandType? AidAnotherOverride(UnitEntityData unit)
        {
            if (unit == null || !AidAnother.TryGetValue(unit, out var list) || list.Count == 0) return null;
            // Swift (17) wins over Move (12).
            return list.Any(c => c.ActionType == UnitCommand.CommandType.Swift)
                ? UnitCommand.CommandType.Swift
                : list.OrderByDescending(c => (int)c.ActionType).First().ActionType;
        }

        // Emotion skill ranks follow the phantom class level. The native
        // AddClassLevels.LevelUp is the only upgrade point the contract allows
        // us to touch; the postfix is narrowed to marked 0085 units.
        [HarmonyPatch(typeof(AddClassLevels), "LevelUp", new[] { typeof(AddClassLevels), typeof(UnitDescriptor), typeof(int) })]
        internal static class SkillRankLevelSync
        {
            [HarmonyPostfix]
            private static void Postfix(UnitDescriptor __1)
            {
                var unit = __1?.Unit;
                if (unit == null || !Phantom0085Marker.IsPhantom(unit)) return;
                foreach (var fact in unit.Facts.List)
                    foreach (var component in fact.Components)
                        if (component.SourceBlueprintComponent is Phantom0085SkillRanks ranks)
                            ranks.Apply();
            }
        }

        // The phantom must never be selectable as a mount; narrowed to the
        // reserved type and the native suitable-mount checker only.
        [HarmonyPatch(typeof(Kingmaker.UnitLogic.Abilities.Components.TargetCheckers.AbilityTargetIsSuitableMount), "IsTargetRestrictionPassed")]
        internal static class MountTargetExclusion
        {
            [HarmonyPrefix]
            private static bool Prefix(TargetWrapper target, ref bool __result)
            {
                var unit = target?.Unit;
                if (unit == null) return true;
                var pet = unit.Get<UnitPartPet>();
                if (pet != null && pet.Type == Spiritualist0085Ids.PhantomPetType && Phantom0085Marker.IsPhantom(unit))
                {
                    __result = false;
                    return false;
                }
                return true;
            }
        }

        [HarmonyPatch(typeof(UnitPartPetMaster), "UpdateMountAbility")]
        internal static class MountSuppression
        {
            [HarmonyPostfix]
            private static void Postfix(UnitPartPetMaster __instance)
            {
                var owner = __instance.Owner;
                if (owner == null) return;
                bool onlyPhantom = __instance.Pets.Count > 0;
                foreach (var reference in __instance.Pets)
                {
                    var part = reference.EntityPart;
                    if (part == null || part.Type != Spiritualist0085Ids.PhantomPetType) { onlyPhantom = false; break; }
                }
                if (!onlyPhantom) return;
                var mount = Kingmaker.Blueprints.Root.BlueprintRoot.Instance?.SystemMechanics?.MountAbility;
                if (mount == null) return;
                var fact = owner.ActivatableAbilities.GetFact(mount);
                if (fact != null) owner.RemoveFact(fact);
            }
        }

        // Native rest already resurrects AddResurrectOnRest units; this fallback
        // only covers a finally-dead 0085 phantom the native queue missed.
        [HarmonyPatch(typeof(Kingmaker.Controllers.Rest.RestController), "ResurrectDeadUnits")]
        internal static class RestResurrectFallback
        {
            [HarmonyPostfix]
            private static void Postfix()
            {
                var player = Game.Instance?.Player;
                if (player == null) return;
                foreach (var master in player.AllCharacters)
                {
                    var part = master.Get<UnitPartPhantom0085>();
                    var pet = part?.BoundPet(master);
                    if (pet == null || !pet.Descriptor.State.IsFinallyDead) continue;
                    if (!pet.State.Features.ResurrectOnRest) continue;
                    pet.Descriptor.Resurrect();
                    pet.Descriptor.State.UpdateLifeState(UnitLifeState.Conscious);
                    pet.Damage = (int)pet.Stats.HitPoints - Math.Min(1, pet.Descriptor.Progression.CharacterLevel);
                    if (Game.Instance.IsModeActive(GameModeType.GlobalMap)) pet.IsInGame = false;
                    else if (pet.Master != null) pet.Position = Kingmaker.UnitLogic.Buffs.Components.ResurrectionLogic.GetResurrectPosition(pet.Master, pet);
                    part.Confined = false;
                    part.PendingManifest = !Phantom0085Link.CanPlace(master);
                }
            }
        }
    }
}
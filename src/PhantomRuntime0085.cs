// COTW-0085 Phantom runtime bridge. Contract C-0085-20261115-r1.
// Private serializable master state, identity-checked lifecycle actions,
// native rest/respec integration and bounded private handlers for the
// emotion mechanics that have no native Wrath component. No game or UMM is
// launched here; scene-dependent paths are player-TEST boundaries.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
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
using Kingmaker.UnitLogic.Abilities.Blueprints;
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
using Kingmaker.UnitLogic.Mechanics.Properties;
using Kingmaker.UnitLogic.Parts;
using Kingmaker.UnitLogic.ActivatableAbilities;
using Kingmaker.UnitLogic.ActivatableAbilities.Restrictions;
using Kingmaker.UnitLogic.Abilities.Components.AreaEffects;
using Kingmaker.Controllers.Rest;
using Kingmaker.Controllers.Rest.State;
using Kingmaker.Utility;
using Kingmaker.View;
using Newtonsoft.Json;
using Pathfinding;
using static CallOfTheWild.Wrath.Prototype.PsychicAssets;

namespace CallOfTheWild.Wrath.Prototype
{
    // ---- Serialized master state (source of truth for Confined intent) ----
    [TypeId("b7e10085a10045008500850085008521")]
    public sealed class UnitPartPhantom0085 : UnitPart
    {
        [JsonProperty] internal bool Confined;
        [JsonProperty] internal string Emotion;
        [JsonProperty] internal string PetId;
        [JsonProperty] internal bool PendingManifest;
        [JsonProperty] internal readonly List<string> GrantedFocus = new List<string>();
        private string lastBindingConflict;

        internal UnitEntityData BoundPet(UnitEntityData master)
        {
            if (master == null || master.IsPet || !master.PreviewOf.IsEmpty || string.IsNullOrEmpty(Emotion)) return null;
            var cls = ResourcesLibrary.TryGetBlueprint<BlueprintCharacterClass>(Spiritualist0085Ids.Id(Spiritualist0085.RootToken));
            if (cls == null || master.Progression.GetClassLevel(cls) < 1) return null;
            var emotionFact = ResourcesLibrary.TryGetBlueprint<BlueprintFeature>(Spiritualist0085Ids.Id(Emotion + "Emotion"));
            var fact = emotionFact == null ? null : master.GetFact(emotionFact);
            if (fact == null || !fact.IsTurnedOn) return null;
            var grant = fact.GetComponentWithRuntime<AddPet>();
            var authorized = grant.Runtime == null ? null : grant.GetData<AddPetData>()?.SpawnedPetRef.Value;
            var expected = grant.Component?.Pet;
            if (expected == null) return null;
            var list = master.Get<UnitPartPetMaster>();
            if (list == null) return null;
            UnitEntityData found = null;
            foreach (var reference in list.Pets)
            {
                var petPart = reference.EntityPart;
                if (petPart == null || petPart.Type != Spiritualist0085Ids.PhantomPetType) continue;
                var pet = reference.Entity;
                if (pet == null) return null;
                if (found != null || pet.Blueprint != expected || Phantom0085Marker.EmotionOf(pet) != Emotion ||
                    (authorized != null && pet != authorized) || petPart.Master != master)
                {
                    LogBindingConflict(master, pet, expected);
                    return null;
                }
                if (authorized == null) return null;
                found = pet;
            }
            if (found != null) PetId = found.UniqueId;
            if (found != null) lastBindingConflict = null;
            return found;
        }

        private void LogBindingConflict(UnitEntityData master, UnitEntityData pet, BlueprintUnit expected)
        {
            var identity = Emotion + "/" + pet.UniqueId;
            if (identity == lastBindingConflict) return;
            lastBindingConflict = identity;
            ModEntry.LogSpellDiagnostic($"0085 phantom binding conflict for owner {master.UniqueId}: " +
                $"slot {pet.UniqueId} ({pet.Blueprint?.AssetGuid}) does not match the active {Emotion} source ({expected.AssetGuid}).");
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
    public sealed class Phantom0085Link : UnitFactComponentDelegate, IAreaHandler, IUnitRestHandler, IUnitHandler, IPetHandler, IGlobalSubscriber
    {
        public string Emotion;

        protected override void OnActivate()
        {
            var master = Owner;
            if (master == null || master.IsPet || !master.PreviewOf.IsEmpty) return;
            var part = master.Ensure<UnitPartPhantom0085>();
            part.Emotion = Emotion;
            Reproject(master, part);
            Phantom0085ConsciousnessGrant.RefreshAll(master);
        }

        protected override void OnDeactivate()
        {
            var master = Owner;
            if (IsReapplying) return;
            var part = master?.Get<UnitPartPhantom0085>();
            if (part == null || part.Emotion != Emotion) return;
            foreach (var key in part.GrantedFocus.ToArray())
            {
                var focus = ResourcesLibrary.TryGetBlueprint<BlueprintFeature>(BlueprintGuid.Parse(key));
                if (focus != null && master.HasFact(focus)) master.RemoveFact(focus);
                part.GrantedFocus.Remove(key);
            }
            ClearCombatLink(master);
            part.Confined = false;
            part.PendingManifest = false;
            part.PetId = null;
            part.Emotion = null;
            Phantom0085ConsciousnessGrant.RefreshAll(master);
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
            if (Alive(pet) && part.Confined)
            {
                part.PendingManifest = true;
            }
            Reproject(unit, part);
            Phantom0085ConsciousnessGrant.RefreshAll(unit);
        }

        public void HandleUnitSpawned(UnitEntityData unit) { }
        public void HandleAddPet(UnitEntityData master, UnitEntityData pet)
        {
            if (master == Owner && Phantom0085Marker.EmotionOf(pet) == Emotion)
            {
                var part = master.Get<UnitPartPhantom0085>();
                if (part?.Emotion == Emotion) Reproject(master, part);
            }
        }
        public void HandleRemovePet(UnitEntityData master, UnitEntityData pet)
        {
            if (master == Owner) ForgetPet(pet);
        }

        public void HandleUnitDestroyed(UnitEntityData unit)
            => ForgetPet(unit);

        private void ForgetPet(UnitEntityData pet)
        {
            var master = Owner;
            var part = master?.Get<UnitPartPhantom0085>();
            if (part?.Emotion != Emotion || !MatchesTrackedPet(part, pet)) return;
            part.PetId = null;
            part.Confined = false;
            part.PendingManifest = false;
            ClearCombatLink(master);
            Phantom0085ConsciousnessGrant.RefreshAll(master);
        }

        private static bool MatchesTrackedPet(UnitPartPhantom0085 part, UnitEntityData pet)
            => part?.PetId != null && pet != null && pet.UniqueId == part.PetId &&
                Phantom0085Marker.EmotionOf(pet) == part.Emotion;

        public void HandleUnitDeath(UnitEntityData unit)
        {
            var master = Owner;
            var part = master?.Get<UnitPartPhantom0085>();
            if (part?.Emotion != Emotion) return;
            if (MatchesTrackedPet(part, unit))
            {
                part.Confined = false;
                part.PendingManifest = false;
            }
            else if (!ReferenceEquals(master, unit))
            {
                return;
            }
            ClearCombatLink(master);
            Phantom0085ConsciousnessGrant.RefreshAll(master);
        }

        internal static bool CanPlace(UnitEntityData master)
            => Alive(master) && master.IsInGame && master.HoldingState != null &&
               Game.Instance?.CurrentlyLoadedArea != null && !Game.Instance.IsModeActive(GameModeType.GlobalMap);

        internal static bool SameLocalArea(UnitEntityData master, UnitEntityData pet)
            => pet != null && CanPlace(master) && pet.HoldingState == master.HoldingState;

        internal static bool TryMoveNearMaster(UnitEntityData master, UnitEntityData pet, bool requireMovement = true)
        {
            var path = AstarPath.active;
            if (!SameLocalArea(master, pet) || !path) return false;
            FreePlaceSelector.PlaceSpawnPlaces(1, 0.5f, master.Position);
            var destination = FreePlaceSelector.GetRelaxedPosition(0, projectOnGround: true);
            var nearest = path.GetNearest(destination);
            if (nearest.node?.Walkable != true || VectorDistanceHorizontal(nearest.position, destination) > 0.5f ||
                VectorDistanceHorizontal(master.Position, destination) > 10.Feet().Meters)
                return false;
            if (VectorDistanceHorizontal(pet.Position, destination) <= 0.1f) return !requireMovement;
            pet.Position = destination;
            return true;
        }

        private static float VectorDistanceHorizontal(UnityEngine.Vector3 from, UnityEngine.Vector3 to)
        {
            from.y = to.y;
            return UnityEngine.Vector3.Distance(from, to);
        }

        internal static void Reproject(UnitEntityData master, UnitPartPhantom0085 part)
        {
            var pet = part.BoundPet(master);
            if (pet == null) return;
            var petPart = pet.Get<UnitPartPet>();
            if (petPart == null) return;
            Phantom0085Patches.MountSuppression.SuppressWithoutOtherNativeMountSource(master, pet);
            Phantom0085PetGrant.RefreshAll(master, pet);
            if (pet.Descriptor.Alignment.ValueRaw != master.Descriptor.Alignment.ValueRaw)
                pet.Descriptor.Alignment.Set(master.Descriptor.Alignment.ValueRaw);
            if (pet.Descriptor.State.IsFinallyDead) return;
            if (part.PendingManifest && Alive(pet))
            {
                ManifestNow(master, pet, part);
            }
            else if (part.Confined)
            {
                if (!petPart.ShouldBeHidden) petPart.SetHidden(true);
            }
            else if (petPart.ShouldBeHidden && Alive(pet))
            {
                ManifestNow(master, pet, part);
            }
            Phantom0085ConsciousnessGrant.RefreshAll(master);
        }

        internal static bool ManifestNow(UnitEntityData master, UnitEntityData pet, UnitPartPhantom0085 part)
        {
            var petPart = pet.Get<UnitPartPet>();
            if (petPart == null || !TryMoveNearMaster(master, pet, requireMovement: false)) return false;
            petPart.SetHidden(false);
            part.Confined = false;
            part.PendingManifest = false;
            return true;
        }

        internal static Buff BondedEffect(UnitEntityData master)
            => master?.Buffs.Enumerable.FirstOrDefault(b => b.Blueprint.AssetGuid == Spiritualist0085Ids.Id("BondedManifestationEffect"));

        internal static void ClearBonded(UnitEntityData master)
        {
            var ability = ResourcesLibrary.TryGetBlueprint<BlueprintActivatableAbility>(Spiritualist0085Ids.Id("BondedManifestation"));
            if (ability != null) master?.ActivatableAbilities.GetFact(ability)?.TurnOff();
            BondedEffect(master)?.Remove();
        }

        private static void ClearCombatLink(UnitEntityData master)
        {
            ClearBonded(master);
            foreach (bool spiritualBond in new[] { false, true })
            {
                var ability = ResourcesLibrary.TryGetBlueprint<BlueprintActivatableAbility>(Phantom0085DamageTransfer.OtherToggle(spiritualBond));
                if (ability != null) master.ActivatableAbilities.GetFact(ability)?.TurnOff();
            }
        }

        internal static bool Alive(UnitEntityData pet)
            => pet != null && !pet.Descriptor.State.IsDead && !pet.Descriptor.State.IsFinallyDead;
    }

    // ---- Alignment sync: phantom mirrors the master ----
    [TypeId("b7e10085a10045008500850085008503")]
    public sealed class Phantom0085AlignmentSync : UnitFactComponentDelegate, IAlignmentChangeHandler
    {
        protected override void OnActivate() => Sync();
        private void Sync()
        {
            var master = Owner;
            var part = master?.Get<UnitPartPhantom0085>();
            var pet = part?.BoundPet(master);
            if (pet != null && pet.Descriptor.Alignment.ValueRaw != master.Descriptor.Alignment.ValueRaw)
                pet.Descriptor.Alignment.Set(master.Descriptor.Alignment.ValueRaw);
        }
        public void HandleAlignmentChange(UnitEntityData unit, Alignment newAlignment, Alignment prevAlignment)
        {
            if (unit == Owner) Sync();
        }
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
        public bool UseOpening;
        public override string GetCaption() => "Opening Strike";

        internal static Buff FindOpening(UnitEntityData ally, UnitEntityData target, BlueprintBuff targetBuff, BlueprintBuff allyBuff, out Buff grant)
        {
            grant = null;
            if (ally == null || target == null || targetBuff == null || allyBuff == null ||
                !target.CombatState.EngagedBy.Contains(ally)) return null;
            foreach (var mark in target.Buffs.Enumerable)
            {
                if (mark.Blueprint != targetBuff || mark.IsPermanent || !mark.IsTurnedOn || mark.IsSuppressed) continue;
                var phantom = mark.Context?.MaybeCaster;
                if (!Phantom0085Link.Alive(phantom)) continue;
                grant = ally.Buffs.Enumerable.FirstOrDefault(b => b.Blueprint == allyBuff &&
                    !b.IsPermanent && b.IsTurnedOn && !b.IsSuppressed && b.Context?.MaybeCaster == phantom);
                if (grant != null) return mark;
            }
            return null;
        }

        public override void RunAction()
        {
            var caster = Context?.MaybeCaster;
            var target = Target?.Unit;
            var targetBuff = TargetBuff?.Get();
            var allyBuff = AllyBuff?.Get();
            var weapon = caster?.GetThreatHandMelee()?.Weapon;
            if (weapon == null || target == null || targetBuff == null || allyBuff == null) return;

            var attack = new RuleAttackWithWeapon(caster, target, weapon, 0) { Reason = Context, ExtraAttack = true };
            if (UseOpening)
            {
                var mark = FindOpening(caster, target, targetBuff, allyBuff, out var grant);
                if (mark == null) return;
                var phantom = mark.Context.MaybeCaster;
                if (Phantom0085Patches.ActiveFeature(phantom, "KindnessExceptionalAid") is Feature exceptional)
                    attack.WeaponStats.AddDamageModifier(phantom.Stats.Charisma.Bonus, exceptional);
                mark.Remove();
                grant.Remove();
                Context.TriggerRule(attack);
                return;
            }

            Context.TriggerRule(attack);
            if (attack.AttackRoll?.IsHit != true) return;
            var allies = target.CombatState.EngagedBy
                .Where(unit => unit != caster && Phantom0085Link.Alive(unit) && !unit.IsEnemy(caster)).ToArray();
            if (allies.Length == 0) return;
            var duration = 1.Rounds().Seconds;
            target.Buffs.AddBuff(targetBuff, caster, duration, AbilityParams.Empty);
            foreach (var ally in allies)
                ally.Buffs.AddBuff(allyBuff, caster, duration, AbilityParams.Empty);
        }
    }

    [TypeId("b7e10085a10045008500850085008520")]
    public sealed class Phantom0085OpeningStrikeTarget : BlueprintComponent, IAbilityTargetRestriction
    {
        public BlueprintBuffReference TargetBuff;
        public BlueprintBuffReference AllyBuff;
        public bool IsTargetRestrictionPassed(UnitEntityData caster, TargetWrapper target)
            => Phantom0085OpeningStrike.FindOpening(caster, target?.Unit, TargetBuff?.Get(), AllyBuff?.Get(), out _) != null;
        public string GetAbilityTargetRestrictionUIText(UnitEntityData caster, TargetWrapper target)
            => "The target must be a threatened enemy with an unused Opening Strike.";
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
    }

    // ---- Benevolent: aid another grants +2 more (bounded patch) ----
    [TypeId("b7e10085a1004500850085008500850d")]
    public sealed class Phantom0085Benevolent : UnitFactComponentDelegate<AddFactsData>
    {
        public BlueprintAbilityReference Fallback;

        protected override void OnActivate() => Reconcile();
        protected override void OnApplyPostLoadFixes()
        {
            if (Fact.Active) Reconcile();
        }

        protected override void OnDeactivate()
        {
            if (IsReapplying) return;
            foreach (var fact in Data.AppliedFacts) Owner.RemoveFact(fact);
            Data.AppliedFacts.Clear();
        }

        private void Reconcile()
        {
            var ability = Phantom0085Patches.ProviderAidAnother(Owner) ?? Fallback?.Get();
            foreach (var old in Data.AppliedFacts.ToArray())
            {
                if (old != null && old.IsAttached && !old.IsDisposed && old.Blueprint == ability) continue;
                if (old != null && old.IsAttached && !old.IsDisposed) Owner.RemoveFact(old);
                Data.AppliedFacts.Remove(old);
            }
            if (ability != null && !Owner.HasFact(ability) && Owner.AddFact(ability, Context) is UnitFact granted)
                Data.AppliedFacts.Add(granted);
        }
    }

    [TypeId("b7e10085a1004500850085008500851f")]
    public sealed class Phantom0085AidAttackBonus : UnitFactComponentDelegate,
        IInitiatorRulebookHandler<RuleCalculateAttackBonus>, IInitiatorRulebookSubscriber
    {
        public void OnEventAboutToTrigger(RuleCalculateAttackBonus evt)
        {
            if (evt.Weapon?.Blueprint?.IsMelee == true)
                evt.AddModifier(4, Fact, ModifierDescriptor.UntypedStackable);
        }

        public void OnEventDidTrigger(RuleCalculateAttackBonus evt) { }
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
            if (emotion == null || part.Emotion == null || emotion.AssetGuid != Spiritualist0085Ids.Id(part.Emotion + "Emotion") ||
                !master.HasFact(emotion)) return false;
            if (Permanent) return true;
            var fused = Fused?.Get();
            var pet = part.BoundPet(master);
            return part.Confined && Phantom0085Link.Alive(pet) &&
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
            var characterClass = ResourcesLibrary.TryGetBlueprint<BlueprintCharacterClass>(Spiritualist0085Ids.Id(Spiritualist0085.RootToken));
            foreach (var guid in desired)
            {
                var focus = ResourcesLibrary.TryGetBlueprint<BlueprintFeature>(guid);
                if (focus == null) continue;
                var key = guid.ToString();
                var fact = master.Progression.Features.GetFact(focus);
                if (fact == null)
                {
                    fact = master.AddFact(focus) as Feature;
                    if (!part.GrantedFocus.Contains(key)) part.GrantedFocus.Add(key);
                }
                if (characterClass != null && fact != null && fact.SourceClass == null && part.GrantedFocus.Contains(key))
                    fact.SetSource(characterClass, master.Progression.GetClassLevel(characterClass));
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
            ReconcileSave(master, "SharedConsciousnessSave", sharedActive && !fusedActive);
            ReconcileSave(master, "FusedConsciousnessSave", fusedActive);
        }

        private static void ReconcileSave(UnitEntityData master, string token, bool active)
        {
            var blueprint = ResourcesLibrary.TryGetBlueprint<BlueprintBuff>(Spiritualist0085Ids.Id(token));
            if (blueprint == null) return;
            var existing = master.Buffs.GetBuff(blueprint);
            if (active && existing == null) master.Buffs.AddBuff(blueprint, master, TimeSpan.MaxValue, AbilityParams.Empty);
            if (!active) existing?.Remove();
        }
    }

    // ---- Etheric Tether / Spiritual Bond damage transfer ----
    [TypeId("b7e10085a1004500850085008500850f")]
    public sealed class Phantom0085DamageTransfer : UnitFactComponentDelegate
    {
        public bool SpiritualBond;

        protected override void OnActivate()
        {
            var other = ResourcesLibrary.TryGetBlueprint<BlueprintActivatableAbility>(OtherToggle(SpiritualBond));
            if (other != null) Owner.ActivatableAbilities.GetFact(other)?.TurnOff();
        }

        internal static BlueprintGuid OtherToggle(bool spiritualBond)
            => Spiritualist0085Ids.Id(spiritualBond ? "EthericTether" : "SpiritualBond");

        internal static int EffectiveHpDamage(int damage, int temporaryHp)
            => damage <= 0 ? 0 : Math.Max(0, damage - Math.Max(0, temporaryHp));

        internal static bool ExpectedVictim(bool spiritualBond, UnitEntityData victim, UnitEntityData master, UnitEntityData pet)
            => !ReferenceEquals(victim, null) && ReferenceEquals(victim, spiritualBond ? master : pet);

        internal static UnitEntityData ActivePet(UnitEntityData master)
        {
            var part = master?.Get<UnitPartPhantom0085>();
            if (part == null || part.Confined) return null;
            var pet = part.BoundPet(master);
            return Phantom0085Link.Alive(pet) && pet.IsInGame && !pet.Get<UnitPartPet>().ShouldBeHidden &&
                Phantom0085Link.SameLocalArea(master, pet) ? pet : null;
        }

        internal static UnitEntityData ResolveRecipient(RuleDealDamage deal, out int amount)
        {
            amount = 0;
            if (deal == null || deal.IsFake || deal.RedirectionTarget.Value != null || deal.RedirectedPercent != 0 || deal.RedirectedDamage != 0)
                return null;
            var victim = deal.Target;
            if (victim == null) return null;
            var victimPart = victim.Get<UnitPartPet>();
            bool spiritualBond = victimPart?.Type != Spiritualist0085Ids.PhantomPetType;
            var master = spiritualBond ? victim : victimPart.Master;
            var pet = ActivePet(master);
            if (pet == null || !ExpectedVictim(spiritualBond, victim, master, pet)) return null;
            if (!master.Buffs.Enumerable.Any(b => b.IsTurnedOn && !b.IsSuppressed &&
                b.Blueprint.GetComponent<Phantom0085DamageTransfer>()?.SpiritualBond == spiritualBond))
                return null;

            int hpDamage = EffectiveHpDamage(deal.Result, (int)victim.Stats.TemporaryHitPoints);
            amount = TransferAmount(spiritualBond, hpDamage, pet.HPLeft, master.HPLeft, pet.Stats.Constitution.ModifiedValue);
            return amount > 0 ? (spiritualBond ? pet : master) : null;
        }

        // Pure contract arithmetic (C13/C14), offline-testable. HP semantics:
        // current HP may be negative; the death floor is 1-CON.
        internal static int TransferAmount(bool spiritualBond, int result, int petHp, int masterHp, int con)
        {
            if (result <= 0) return 0;
            int floor = 1 - con;
            if (spiritualBond)
            {
                if (petHp < 1) return 0;
                int excess = 1 - (masterHp - result);
                if (excess <= 0) return 0;
                int capacity = petHp - floor;
                return Math.Min(result, Math.Min(excess, Math.Max(0, capacity)));
            }
            int overflow = floor - (petHp - result);
            if (overflow <= 0) return 0;
            return Math.Min(result, Math.Min(overflow, Math.Max(0, masterHp - 1)));
        }
    }

    [TypeId("b7e10085a10045008500850085008522")]
    public sealed class Phantom0085DamageLinkRestriction : ActivatableAbilityRestriction
    {
        protected override bool IsAvailable() => Phantom0085DamageTransfer.ActivePet(Owner) != null;
    }

    [TypeId("b7e10085a1004500850085008500851e")]
    public sealed class Phantom0085MasterClassLevel : PropertyValueGetter
    {
        public BlueprintCharacterClassReference CharacterClass;

        protected override int GetBaseValue(UnitEntityData unit)
        {
            var master = unit?.Master;
            var characterClass = CharacterClass?.Get();
            return master != null && characterClass != null
                ? master.Progression.GetClassLevel(characterClass)
                : 0;
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
            if (phantom == null || unit == null || unit == phantom) return;
            var master = phantom.Get<UnitPartPet>()?.Master;
            var buff = (unit == master ? OwnerBuff : (Greater ? AllyBuff : null))?.Get();
            if (buff == null) return;
            if (!Phantom0085InferenceBonus.Eligible(phantom, unit, Greater, unit == master))
            {
                Phantom0085InferenceExit.Remove(unit, phantom, buff);
                return;
            }
            if (!unit.Buffs.Enumerable.Any(b => b.Blueprint == buff && b.Context?.MaybeCaster == phantom))
                unit.Buffs.AddBuff(buff, phantom, TimeSpan.MaxValue, AbilityParams.Empty);
        }
    }

    [TypeId("b7e10085a1004500850085008500851c")]
    public sealed class Phantom0085InferenceBonus : UnitBuffComponentDelegate,
        ITargetRulebookHandler<RuleCalculateAC>, IInitiatorRulebookHandler<RuleSavingThrow>, IUnitSubscriber
    {
        public bool Greater;
        public bool ForMaster;
        public int Bonus;

        internal static bool Eligible(UnitEntityData phantom, UnitEntityData unit, bool greater, bool forMaster)
        {
            if (!Phantom0085Link.Alive(phantom) || unit == null || unit == phantom || !phantom.IsInGame ||
                phantom.HoldingState == null || phantom.HoldingState != unit.HoldingState ||
                phantom.DistanceTo(unit) > 10.Feet().Meters ||
                phantom.State.HasCondition(UnitCondition.Helpless) || phantom.State.HasCondition(UnitCondition.Unconscious) ||
                phantom.Get<UnitPartGrappleInitiator>() != null || phantom.Get<UnitPartGrappleTarget>() != null)
                return false;
            var master = phantom.Get<UnitPartPet>()?.Master;
            var part = master?.Get<UnitPartPhantom0085>();
            if (part == null || part.Confined || part.BoundPet(master) != phantom) return false;
            var upgraded = ResourcesLibrary.TryGetBlueprint<BlueprintFeature>(Spiritualist0085Ids.Id("GreaterSpiritualInference"));
            if (greater != (upgraded != null && master.HasFact(upgraded))) return false;
            return forMaster ? unit == master : greater && unit != master && unit.IsAlly(phantom);
        }

        public void OnEventAboutToTrigger(RuleCalculateAC evt)
        {
            if (Eligible(Buff.Context?.MaybeCaster, Owner, Greater, ForMaster))
                evt.AddModifier(Bonus, Fact, ModifierDescriptor.Shield);
        }
        public void OnEventAboutToTrigger(RuleSavingThrow evt)
        {
            if (Eligible(Buff.Context?.MaybeCaster, Owner, Greater, ForMaster))
                evt.AddModifier(Bonus, Fact, ModifierDescriptor.Circumstance);
        }
        public void OnEventDidTrigger(RuleCalculateAC evt) { }
        public void OnEventDidTrigger(RuleSavingThrow evt) { }
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
            var phantom = Context?.MaybeCaster;
            if (unit == null || phantom == null) return;
            Remove(unit, phantom, OwnerBuff?.Get());
            Remove(unit, phantom, AllyBuff?.Get());
        }

        internal static void Remove(UnitEntityData unit, UnitEntityData phantom, BlueprintBuff blueprint)
        {
            if (blueprint == null) return;
            foreach (var fact in unit.Buffs.Enumerable.Where(b => b.Blueprint == blueprint && b.Context?.MaybeCaster == phantom).ToArray())
                fact.Remove();
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
            if (!pet.IsInGame || !Phantom0085Link.SameLocalArea(master, pet)) return;
            var petPart = pet.Get<UnitPartPet>();
            if (petPart == null || petPart.ShouldBeHidden) return;
            var resource = ResourcesLibrary.TryGetBlueprint<BlueprintAbilityResource>(Spiritualist0085Ids.Id("PhantomRecallResource"));
            if (resource == null || master.Resources.GetResourceAmount(resource) < 1) return;
            if (pet.DistanceTo(master) <= 5.Feet().Meters) return;
            if (Phantom0085Link.TryMoveNearMaster(master, pet)) master.Resources.Spend(resource, 1);
        }
    }

    // Native AddPet owns the entity. These grants record only their own facts
    // so even a reserved-type collision cannot modify another mod's companion.
    public abstract class Phantom0085PetGrant : UnitFactComponentDelegate<Phantom0085PetGrant.State>, IPetHandler
    {
        public sealed class State
        {
            [JsonProperty] public string PetId;
            [JsonProperty] public List<EntityFactRef<Feature>> Features = new List<EntityFactRef<Feature>>();
        }

        protected override void OnActivate() => Refresh(Owner?.Get<UnitPartPhantom0085>()?.BoundPet(Owner));
        protected override void OnDeactivate()
        {
            if (!IsReapplying) Clear();
        }

        protected override void OnApplyPostLoadFixes() => OnActivate();

        public void HandleAddPet(UnitEntityData master, UnitEntityData pet)
        {
            if (master == Owner) OnActivate();
        }

        public void HandleRemovePet(UnitEntityData master, UnitEntityData pet)
        {
            if (master == Owner && pet?.UniqueId == Data.PetId) Clear();
        }

        private void Clear()
        {
            foreach (var reference in Data.Features)
                if (reference.Fact is Feature fact && fact.Manager != null) fact.Owner.RemoveFact(fact);
            Data.Features.Clear();
            Data.PetId = null;
        }

        protected abstract IEnumerable<BlueprintFeature> FeaturesToGrant();

        private void Refresh(UnitEntityData pet)
        {
            if (pet == null) return;
            if (Data.PetId != pet.UniqueId)
            {
                Clear();
                Data.PetId = pet.UniqueId;
            }
            foreach (var feature in FeaturesToGrant())
            {
                if (feature != null && !pet.HasFact(feature))
                {
                    var added = pet.Progression.Features.AddFeature(feature);
                    if (added != null) Data.Features.Add(added);
                }
            }
        }

        internal static void RefreshAll(UnitEntityData master, UnitEntityData pet)
        {
            foreach (var runtime in master.Facts.List.Where(f => f.IsTurnedOn).SelectMany(f => f.Components).ToArray())
            {
                if (!(runtime.SourceBlueprintComponent is Phantom0085PetGrant grant) || !(runtime is ISubscriptionProxy subscriber))
                    continue;
                using (subscriber.RequestEventContext()) grant.Refresh(pet);
            }
        }
    }

    [TypeId("b7e10085a1004500850085008500851d")]
    public sealed class Phantom0085BoundGrant : Phantom0085PetGrant
    {
        public BlueprintFeatureReference Feature;
        protected override IEnumerable<BlueprintFeature> FeaturesToGrant() { yield return Feature?.Get(); }
    }

    [TypeId("b7e10085a10045008500850085008514")]
    public sealed class Phantom0085PotentGrant : Phantom0085PetGrant
    {
        public BlueprintProgressionReference Emotion;
        protected override IEnumerable<BlueprintFeature> FeaturesToGrant()
            => (Emotion?.Get()?.LevelEntries ?? Array.Empty<LevelEntry>())
                .Where(entry => entry.Level == 1 || entry.Level == 7 || entry.Level == 12 || entry.Level == 17)
                .SelectMany(entry => entry.Features).OfType<BlueprintFeature>()
                .Select(feature => feature.GetComponent<Phantom0085BoundGrant>()?.Feature?.Get()).Where(feature => feature != null);
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

    // ---- Bonded shield applies to incorporeal touch attacks (original convention) ----
    [TypeId("b7e10085a1004500850085008500851b")]
    public sealed class Phantom0085ShieldToIncorporealTouch : UnitFactComponentDelegate, ITargetRulebookHandler<RuleCalculateAC>, ITargetRulebookSubscriber
    {
        private static readonly BlueprintGuid Incorporeal = BlueprintGuid.Parse("c4a7f98d743bc784c9d4cf2105852c39");
        public void OnEventAboutToTrigger(RuleCalculateAC evt)
        {
            if (!evt.AttackType.IsTouch()) return;
            if (evt.Initiator == null || !evt.Initiator.HasFact(ResourcesLibrary.TryGetBlueprint<BlueprintFeature>(Incorporeal))) return;
            int shield = Context?[AbilityRankType.Default] ?? 0;
            if (shield > 0) evt.AddModifier(shield, Fact, ModifierDescriptor.UntypedStackable);
        }
        public void OnEventDidTrigger(RuleCalculateAC evt) { }
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
            if (!Phantom0085Link.Alive(pet) || pet.HPLeft < 1 || !pet.IsInGame ||
                !Phantom0085Link.SameLocalArea(master, pet)) return;
            var petPart = pet.Get<UnitPartPet>();
            if (petPart == null || petPart.ShouldBeHidden) return;
            petPart.SetHidden(true);
            part.Confined = true;
            part.PendingManifest = false;
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
            if (Phantom0085Link.BondedEffect(master) != null) return;
            if (Phantom0085Link.ManifestNow(master, pet, part))
                Phantom0085ConsciousnessGrant.RefreshAll(master);
        }
    }

    [TypeId("b7e10085a10045008500850085008517")]
    public sealed class Phantom0085NoOp : BlueprintComponent { }

    // ---- Native damage, action, mount and rest integration ----
    internal static class Phantom0085Patches
    {
        [HarmonyPatch(typeof(RuleDealDamage), nameof(RuleDealDamage.OnTrigger))]
        internal static class ExactDamageTransfer
        {
            private static readonly ConditionalWeakTable<RuleDealDamage, object> Exact = new ConditionalWeakTable<RuleDealDamage, object>();
            private sealed class Payment
            {
                internal readonly int Amount;
                internal Payment(int amount) => Amount = amount;
            }

            private static readonly ConditionalWeakTable<RuleDealDamage, Payment> Payments = new ConditionalWeakTable<RuleDealDamage, Payment>();
            private static readonly object Marker = new object();
            private static readonly System.Reflection.MethodInfo SetResult = AccessTools.PropertySetter(typeof(RuleDealDamage), "Result");
            private static readonly System.Reflection.MethodInfo SetRedirected = AccessTools.PropertySetter(typeof(RuleDealDamage), "RedirectedDamage");

            [HarmonyTranspiler]
            private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> source)
            {
                var instructions = source.ToList();
                var lastDamage = AccessTools.PropertySetter(typeof(UnitEntityData), "LastHandledDamage");
                var directDamage = AccessTools.Method(typeof(Kingmaker.Designers.GameHelper), nameof(Kingmaker.Designers.GameHelper.DealDirectDamage),
                    new[] { typeof(UnitEntityData), typeof(UnitEntityData), typeof(int) });
                var points = instructions.Where(i => i.Calls(lastDamage)).ToArray();
                var directCalls = instructions.Where(i => i.Calls(directDamage)).ToArray();
                var handleDamage = AccessTools.Method(typeof(ModifiableValueTemporaryHitPoints), nameof(ModifiableValueTemporaryHitPoints.HandleDamage));
                var hpCalls = instructions.Where(i => i.Calls(handleDamage)).ToArray();
                if (points.Length != 1 || directCalls.Length != 1 || hpCalls.Length != 1 || SetResult == null || SetRedirected == null)
                {
                    ModEntry.LogSpellDiagnostic("0085 exact damage transfer disabled: native damage application boundary was not found.");
                    return instructions;
                }

                // The native rule has already applied MinHP, difficulty and other
                // redirection. It has not touched temporary HP or normal HP yet.
                var point = points[0];
                var load = new CodeInstruction(OpCodes.Ldarg_0);
                load.labels.AddRange(point.labels);
                point.labels.Clear();
                instructions.InsertRange(instructions.IndexOf(point), new[]
                {
                    load, new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(ExactDamageTransfer), nameof(Apply)))
                });

                var call = directCalls[0];
                var rule = new CodeInstruction(OpCodes.Ldarg_0);
                rule.labels.AddRange(call.labels);
                call.labels.Clear();
                call.operand = AccessTools.Method(typeof(ExactDamageTransfer), nameof(Deliver));
                instructions.Insert(instructions.IndexOf(call), rule);
                var hpCall = hpCalls[0];
                var payment = new CodeInstruction(OpCodes.Ldarg_0);
                payment.labels.AddRange(hpCall.labels);
                hpCall.labels.Clear();
                hpCall.opcode = OpCodes.Call;
                hpCall.operand = AccessTools.Method(typeof(ExactDamageTransfer), nameof(ApplyToHitPoints));
                instructions.Insert(instructions.IndexOf(hpCall), payment);
                return instructions;
            }

            private static void Apply(RuleDealDamage deal)
            {
                if (Payments.TryGetValue(deal, out var payment))
                {
                    SetResult.Invoke(deal, new object[] { payment.Amount });
                    SetRedirected.Invoke(deal, new object[] { 0 });
                    return;
                }
                var recipient = Phantom0085DamageTransfer.ResolveRecipient(deal, out int amount);
                if (recipient == null) return;
                Exact.Add(deal, Marker);
                SetResult.Invoke(deal, new object[] { deal.Result - amount });
                SetRedirected.Invoke(deal, new object[] { amount });
                deal.RedirectionTarget = recipient;
            }

            private static void Deliver(UnitEntityData initiator, UnitEntityData recipient, int amount, RuleDealDamage original)
            {
                if (!Exact.Remove(original))
                {
                    Kingmaker.Designers.GameHelper.DealDirectDamage(initiator, recipient, amount);
                    return;
                }
                var payment = new RuleDealDamage(initiator, recipient, new DirectDamage(DiceFormula.Zero, amount));
                Payments.Add(payment, new Payment(amount));
                Game.Instance.Rulebook.TriggerEvent(payment);
            }

            private static int ApplyToHitPoints(ModifiableValueTemporaryHitPoints temporaryHp, int result, RuleDealDamage deal)
                => Payments.TryGetValue(deal, out _) ? result : temporaryHp.HandleDamage(result);
        }

        private static readonly BlueprintGuid ExpandedContentAid = BlueprintGuid.Parse("1eea0d80c6b7407aa8a902f6e750ea95");
        private static readonly BlueprintGuid ExpandedContentSwiftAid = BlueprintGuid.Parse("840e53983d9843489554f76cc764b776");
        private static readonly BlueprintGuid NativeSkillAbilities = BlueprintGuid.Parse("e4c33ff99d638744686112e2a5f49856");
        private static readonly BlueprintGuid FallbackAid = Spiritualist0085Ids.Id("KindnessAidAnother");
        private static readonly HashSet<BlueprintGuid> ProviderAidBuffs = new HashSet<BlueprintGuid>
        {
            BlueprintGuid.Parse("31f151cd84d54ffcbf522cd7f4325bb6"),
            BlueprintGuid.Parse("bbbf1252b3644ed88703a93fdd5d3602"),
            BlueprintGuid.Parse("383e54f3fc234904bbaf8feae5bee2dc"),
            BlueprintGuid.Parse("604f41a042b745f7b26ac0da38e3ece3")
        };

        internal static BlueprintAbility ProviderAidAnother(UnitEntityData caster)
        {
            var aid = ResourcesLibrary.TryGetBlueprint<BlueprintAbility>(ExpandedContentAid);
            if (aid == null) return null;
            if (caster.HasFact(aid)) return aid;
            var skillAbilities = ResourcesLibrary.TryGetBlueprint<BlueprintFeature>(NativeSkillAbilities)?.GetComponent<AddFacts>();
            return skillAbilities != null && skillAbilities.Facts.Any(f => f?.AssetGuid == ExpandedContentAid) ? aid : null;
        }

        internal static Feature ActiveFeature(UnitEntityData unit, string token)
        {
            var blueprint = ResourcesLibrary.TryGetBlueprint<BlueprintFeature>(Spiritualist0085Ids.Id(token));
            var fact = blueprint == null ? null : unit?.Progression.Features.GetFact(blueprint);
            return fact?.IsTurnedOn == true ? fact : null;
        }

        [HarmonyPatch(typeof(AbilityData), nameof(AbilityData.ActionType), MethodType.Getter)]
        internal static class AidAnotherAction
        {
            [HarmonyPostfix]
            private static void Postfix(AbilityData __instance, ref UnitCommand.CommandType __result)
            {
                if (__result != UnitCommand.CommandType.Standard && __result != UnitCommand.CommandType.Move) return;
                var ability = __instance.Blueprint;
                var root = (ability.Parent ?? ability).AssetGuid;
                if (root != ExpandedContentAid && root != ExpandedContentSwiftAid && root != FallbackAid) return;
                var caster = __instance.Caster?.Unit;
                if (!Phantom0085Marker.IsPhantom(caster)) return;
                var feature = ActiveFeature(caster, "KindnessExceptionalAid") ?? ActiveFeature(caster, "KindnessExpandedAid");
                var action = feature?.Blueprint.GetComponent<Phantom0085AidAnotherActionType>();
                if (action != null) __result = action.ActionType;
            }
        }

        [HarmonyPatch(typeof(ContextValue), nameof(ContextValue.Calculate), new[] { typeof(MechanicsContext) })]
        internal static class ProviderBenevolentBonus
        {
            [HarmonyPostfix]
            private static void Postfix(ContextValue __instance, MechanicsContext context, ref int __result)
            {
                if (__instance.ValueType != ContextValueType.Simple || __result != -2 ||
                    context?.AssociatedBlueprint == null || !ProviderAidBuffs.Contains(context.AssociatedBlueprint.AssetGuid))
                    return;
                var caster = context.MaybeCaster;
                if (Phantom0085Marker.IsPhantom(caster) && ActiveFeature(caster, "KindnessPhantomBase") != null)
                    __result -= 2;
            }
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
                var phantom = owner.Get<UnitPartPhantom0085>()?.BoundPet(owner);
                if (phantom != null) SuppressWithoutOtherNativeMountSource(owner, phantom);
            }

            internal static bool IsOtherNativeMountSource(UnitEntityData other, UnitEntityData phantom, PetType? type)
                => !ReferenceEquals(other, null) && !ReferenceEquals(other, phantom) && type.HasValue &&
                    type != PetType.MythicSkeletalChampion && type != PetType.Clone;

            internal static void SuppressWithoutOtherNativeMountSource(UnitEntityData owner, UnitEntityData phantom)
            {
                var pets = owner.Get<UnitPartPetMaster>()?.Pets;
                if (pets == null || pets.Any(p => IsOtherNativeMountSource(p.Entity, phantom, p.EntityPart?.Type))) return;
                var mount = Kingmaker.Blueprints.Root.BlueprintRoot.Instance?.SystemMechanics?.MountAbility;
                if (mount == null) return;
                var fact = owner.ActivatableAbilities.GetFact(mount);
                if (fact != null) owner.RemoveFact(fact);
            }
        }

        [HarmonyPatch(typeof(Kingmaker.Controllers.Rest.RestController), "ResurrectDeadUnits")]
        internal static class RestResurrectionQueue
        {
            [HarmonyTranspiler]
            private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> source)
            {
                var instructions = source.ToList();
                var getParty = AccessTools.Method(typeof(CampingState), nameof(CampingState.GetPartyUnitsForRest));
                var calls = instructions.Where(i => i.Calls(getParty)).ToArray();
                if (calls.Length != 2)
                {
                    ModEntry.LogSpellDiagnostic("0085 rest resurrection queue extension disabled: the two native rest queries were not found.");
                    return instructions;
                }
                // Only extend the resurrection loop. The second query handles Death's Door.
                calls[0].operand = AccessTools.Method(typeof(RestResurrectionQueue), nameof(GetResurrectionUnits));
                return instructions;
            }

            private static IEnumerable<UnitEntityData> GetResurrectionUnits(bool removePets, bool includeDeadAndParalyzed)
            {
                var nativeUnits = CampingState.GetPartyUnitsForRest(removePets, includeDeadAndParalyzed).ToList();
                var queued = new HashSet<UnitEntityData>(nativeUnits);
                foreach (var master in nativeUnits.ToArray())
                {
                    var part = master.Get<UnitPartPhantom0085>();
                    var pet = part?.BoundPet(master);
                    if (pet == null || !pet.Descriptor.State.IsFinallyDead) continue;
                    if (pet.State.Features.ResurrectOnRest && queued.Add(pet)) nativeUnits.Add(pet);
                }
                foreach (var unit in nativeUnits)
                {
                    var master = unit.Master;
                    var part = master?.Get<UnitPartPhantom0085>();
                    bool recovery = unit.Descriptor.State.IsFinallyDead && part?.BoundPet(master) == unit;
                    yield return unit;
                    if (!recovery || !Phantom0085Link.Alive(unit)) continue;
                    part.Confined = false;
                    part.PendingManifest = !Phantom0085Link.CanPlace(master);
                    if (!part.PendingManifest && unit.Get<UnitPartPet>() is UnitPartPet petPart && petPart.ShouldBeHidden)
                        petPart.SetHidden(false);
                }
            }
        }
    }
}

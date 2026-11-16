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
using Kingmaker.UnitLogic.Mechanics.Conditions;
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
        [JsonProperty] internal readonly List<EntityFactRef<Feature>> OwnedFocus = new List<EntityFactRef<Feature>>();
        // Old local TEST saves tracked only the blueprint GUID.
        [JsonProperty] internal readonly List<string> GrantedFocus = new List<string>();
        private string lastBindingConflict;
        private string lastRecalledPet;
        private UnityEngine.Vector3 lastRecallAnchor;
        private UnityEngine.Vector3 lastRecallLanding;

        internal bool AtLastRecallLanding(string pet, UnityEngine.Vector3 anchor, UnityEngine.Vector3 position)
            => lastRecalledPet != null && lastRecalledPet == pet &&
                Phantom0085Link.SamePosition(lastRecallAnchor, anchor) &&
                Phantom0085Link.SamePosition(lastRecallLanding, position);

        internal void RememberRecall(string pet, UnityEngine.Vector3 anchor, UnityEngine.Vector3 position)
        {
            lastRecalledPet = pet;
            lastRecallAnchor = anchor;
            lastRecallLanding = position;
        }

        internal void ClearRecall() => lastRecalledPet = null;

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
    public sealed class Phantom0085Link : UnitFactComponentDelegate, IAreaHandler, IUnitRestHandler, IUnitHandler,
        IUnitLifeStateChanged, IPetHandler, IGlobalSubscriber
    {
        public string Emotion;

        protected override void OnTurnOn()
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
            ClearCombatLink(master);
            part.Confined = false;
            part.PendingManifest = false;
            part.PetId = null;
            part.Emotion = null;
            part.ClearRecall();
            Phantom0085ConsciousnessGrant.RefreshAll(master);
        }

        public void OnAreaBeginUnloading() => Owner?.Get<UnitPartPhantom0085>()?.ClearRecall();
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
            part.ClearRecall();
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

        public void HandleUnitLifeStateChanged(UnitEntityData unit, UnitLifeState prevLifeState)
        {
            var master = Owner;
            if (prevLifeState != UnitLifeState.Dead || master == null || !Alive(unit) || unit.Master != master) return;
            var part = master.Get<UnitPartPhantom0085>();
            if (part?.Emotion == Emotion && part.BoundPet(master) == unit)
                RestoreAfterRecovery(master, unit, part);
        }

        internal static void RestoreAfterRecovery(UnitEntityData master, UnitEntityData pet, UnitPartPhantom0085 part)
        {
            part.Confined = false;
            part.PendingManifest = !SameLocalArea(master, pet) || !master.IsPlayerFaction;
            if (!part.PendingManifest && pet.Get<UnitPartPet>() is UnitPartPet petPart &&
                (petPart.ShouldBeHidden || !pet.IsInGame))
            {
                petPart.SetHidden(false);
                part.PendingManifest = !pet.IsInGame;
            }
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
            var anchor = path.GetNearest(master.Position);
            var nearest = path.GetNearest(destination);
            if (anchor.node?.Walkable != true || nearest.node?.Walkable != true || anchor.node.Area != nearest.node.Area ||
                (nearest.position - destination).sqrMagnitude > 0.25f ||
                (master.Position - destination).sqrMagnitude > 10.Feet().Meters * 10.Feet().Meters)
                return false;
            var before = pet.Position;
            if (SamePosition(before, destination)) return !requireMovement;
            pet.Position = destination;
            return !requireMovement || !SamePosition(before, pet.Position);
        }

        internal static bool SamePosition(UnityEngine.Vector3 from, UnityEngine.Vector3 to)
            => (from - to).sqrMagnitude <= 0.01f;

        internal static void Reproject(UnitEntityData master, UnitPartPhantom0085 part)
        {
            var pet = part.BoundPet(master);
            if (pet == null) return;
            var petPart = pet.Get<UnitPartPet>();
            if (petPart == null) return;
            Phantom0085PetGrant.RefreshAll(master, pet);
            Phantom0085AlignmentSync.Sync(master, pet);
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
            if (petPart == null || !master.IsPlayerFaction || !TryMoveNearMaster(master, pet, requireMovement: false))
                return false;
            petPart.SetHidden(false);
            if (!pet.IsInGame) return false;
            part.Confined = false;
            part.PendingManifest = false;
            return true;
        }

        internal static Buff BondedEffect(UnitEntityData master)
            => master?.Buffs.Enumerable.FirstOrDefault(b => b.Blueprint.AssetGuid == Spiritualist0085Ids.Id("BondedManifestationEffect"));

        internal static UnitEntityData ConfinedPet(UnitEntityData master)
        {
            var part = master?.Get<UnitPartPhantom0085>();
            if (part?.Confined != true || !Alive(master)) return null;
            var pet = part.BoundPet(master);
            return Alive(pet) ? pet : null;
        }

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
            Sync(master, part?.BoundPet(master));
        }

        internal static void Sync(UnitEntityData master, UnitEntityData pet)
        {
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
        internal static DiceFormula Dice(bool large, int rank)
            => (large ? Anger : Standard)[Math.Max(0, Math.Min(rank, Standard.Length - 1))];

        public void OnEventAboutToTrigger(RuleCalculateWeaponStats evt) { }
        public void OnEventDidTrigger(RuleCalculateWeaponStats evt)
        {
            if (evt.Weapon?.Blueprint?.Category != WeaponCategory.OtherNaturalWeapons) return;
            evt.WeaponDamageDice.Modify(Dice(Large, Context?[AbilityRankType.Default] ?? 0), Fact);
        }
    }

    // ---- Emotion skill ranks equal to phantom class level ----
    [TypeId("b7e10085a10045008500850085008505")]
    public sealed class Phantom0085SkillRanks : UnitFactComponentDelegate, IOwnerGainLevelHandler
    {
        public StatType[] Skills;
        protected override void OnTurnOn() => HandleUnitGainLevel();

        public void HandleUnitGainLevel()
        {
            var unit = Owner;
            if (!Phantom0085Marker.IsPhantom(unit) || Skills == null) return;
            var phantomClass = ResourcesLibrary.TryGetBlueprint<BlueprintCharacterClass>(Spiritualist0085Ids.Id("PhantomClass"));
            if (phantomClass == null) return;
            int level = unit.Progression.GetClassLevel(phantomClass);
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
    public sealed class Phantom0085HatefulAura : UnitBuffComponentDelegate,
        IInitiatorRulebookHandler<RuleDealDamage>, IInitiatorRulebookSubscriber
    {
        public void OnEventAboutToTrigger(RuleDealDamage evt) { }
        public void OnEventDidTrigger(RuleDealDamage evt)
        {
            if (evt.IsFake || evt.Result <= 0 || evt.Initiator != Owner || !Buff.IsTurnedOn || Buff.IsSuppressed ||
                evt.Reason.Context?.AssociatedBlueprint?.GetComponent<Phantom0085HatefulAura>() != null) return;
            var phantom = Buff.Context.MaybeCaster;
            var master = phantom?.Get<UnitPartPet>()?.Master;
            if (!ReferenceEquals(Phantom0085DamageTransfer.ActivePet(master), phantom) || phantom == null ||
                !Owner.IsEnemy(phantom) || (evt.Target != phantom && evt.Target != master) ||
                Owner.DistanceTo(phantom) > 20.Feet().Meters) return;
            int bonus = Math.Max(0, phantom.Stats.Charisma.Bonus);
            if (bonus > 0)
                Buff.Context.TriggerRule(new RuleDealDamage(phantom, Owner, new DirectDamage(DiceFormula.Zero, bonus)) { Reason = Buff.Context });
        }
    }

    [TypeId("b7e10085a10045008500850085008523")]
    public sealed class Phantom0085HatredSneakAttack : UnitFactComponentDelegate,
        IInitiatorRulebookHandler<RuleAttackWithWeapon>, IInitiatorRulebookSubscriber
    {
        public BlueprintBuffReference Hated;

        public void OnEventAboutToTrigger(RuleAttackWithWeapon evt)
        {
            var mark = Hated?.Get();
            var dice = Context?[AbilityRankType.Default] ?? 0;
            if (mark == null || dice <= 0 || evt.Target == null ||
                !evt.Target.Buffs.Enumerable.Any(b => b.Blueprint == mark && b.IsTurnedOn &&
                    !b.IsSuppressed && ReferenceEquals(b.Context.MaybeCaster, Owner))) return;
            evt.AddTemporaryModifier(Owner.Stats.SneakAttack.AddModifier(dice, Runtime, ModifierDescriptor.Feat));
        }
        public void OnEventDidTrigger(RuleAttackWithWeapon evt) { }
    }

    [TypeId("b7e10085a10045008500850085008524")]
    public sealed class Phantom0085FearDifficulty : PropertyValueGetter
    {
        protected override int GetBaseValue(UnitEntityData unit)
            => unit == null ? 0 : 10 + unit.Progression.CharacterLevel / 2 + unit.Stats.Charisma.Bonus;
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
            if (!Buff.IsTurnedOn || Buff.IsSuppressed || evt.IsFake || evt.Initiator != Owner ||
                !(evt.RollHistory?.Count > 0)) return;
            var previous = Rulebook.CurrentContext?.PreviousEvent;
            if (Rule == RuleType.AttackRoll)
            {
                if (!(previous is RuleAttackRoll attack) || attack.AutoHit || attack.AutoMiss || attack.D20 != null) return;
            }
            else if (!(previous is RuleSavingThrow)) return;
            var resource = Resource?.Get();
            if (resource == null || Owner.Resources.GetResourceAmount(resource) <= 0 ||
                !SpeakerFailedReroll0046.Failed(evt, previous, Rule)) return;
            Owner.Resources.Spend(resource, 1);
            UseReplacement(evt, RulebookEvent.Dice.D(evt.DiceFormula));
        }

        internal static void UseReplacement(RuleRollD20 evt, int roll)
        {
            evt.Override(roll == 1 && evt.ReplaceOneWithMax ? evt.DiceFormula.MaxValue(0) : roll);
            evt.RollHistory.Add(roll);
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

        protected override void OnTurnOn() => RefreshAll(Owner);
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
            if (master == null || !master.PreviewOf.IsEmpty) return;
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
            if (characterClass == null) return;
            foreach (var key in part.GrantedFocus)
            {
                var focus = ResourcesLibrary.TryGetBlueprint<BlueprintFeature>(BlueprintGuid.Parse(key));
                var fact = focus == null ? null : master.Progression.Features.GetFact(focus);
                if (fact?.SourceClass == characterClass) part.OwnedFocus.Add(fact);
            }
            part.GrantedFocus.Clear();
            foreach (var reference in part.OwnedFocus.ToArray())
            {
                var fact = reference.Fact;
                if (fact == null || fact.SourceClass != characterClass ||
                    !ReferenceEquals(fact, master.Progression.Features.GetFact(fact.Blueprint)))
                {
                    part.OwnedFocus.Remove(reference);
                    continue;
                }
                if (desired.Contains(fact.Blueprint.AssetGuid)) continue;
                master.RemoveFact(fact);
                part.OwnedFocus.Remove(reference);
            }
            foreach (var guid in desired)
            {
                var focus = ResourcesLibrary.TryGetBlueprint<BlueprintFeature>(guid);
                if (focus != null && !master.HasFact(focus) && master.AddFact(focus) is Feature fact)
                {
                    fact.SetSource(characterClass, master.Progression.GetClassLevel(characterClass));
                    part.OwnedFocus.Add(fact);
                }
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

    [TypeId("b7e10085a10045008500850085008511")]
    public sealed class Phantom0085BondedRestriction : ActivatableAbilityRestriction
    {
        protected override bool IsAvailable() => Phantom0085Link.ConfinedPet(Owner) != null;
    }

    // ---- Spiritual Inference: native area applies buffs, combat rules recheck eligibility ----
    [TypeId("b7e10085a10045008500850085008525")]
    public sealed class Phantom0085InferenceCondition : ContextCondition
    {
        public bool Greater;
        public bool ForMaster;

        protected override string GetConditionCaption() => "Eligible for Spiritual Inference";
        protected override bool CheckCondition()
            => Phantom0085InferenceBonus.Eligible(Context?.MaybeCaster, Target?.Unit, Greater, ForMaster);
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

        private bool GrantsBonus()
            // Buffs written by earlier development builds have no native area owner.
            => !string.IsNullOrEmpty(Buff.SourceAreaEffectId) &&
                Eligible(Buff.Context?.MaybeCaster, Owner, Greater, ForMaster);

        public void OnEventAboutToTrigger(RuleCalculateAC evt)
        {
            if (GrantsBonus())
                evt.AddModifier(Bonus, Fact, ModifierDescriptor.Shield);
        }
        public void OnEventAboutToTrigger(RuleSavingThrow evt)
        {
            if (GrantsBonus())
                evt.AddModifier(Bonus, Fact, ModifierDescriptor.Circumstance);
        }
        public void OnEventDidTrigger(RuleCalculateAC evt) { }
        public void OnEventDidTrigger(RuleSavingThrow evt) { }
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
            if (part.AtLastRecallLanding(pet.UniqueId, master.Position, pet.Position) ||
                !Phantom0085Link.TryMoveNearMaster(master, pet)) return;
            master.Resources.Spend(resource, 1);
            part.RememberRecall(pet.UniqueId, master.Position, pet.Position);
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

        protected override void OnTurnOn() => RefreshForOwner();
        protected override void OnDeactivate()
        {
            if (!IsReapplying) Clear();
        }

        protected override void OnApplyPostLoadFixes()
        {
            if (Fact.IsTurnedOn) RefreshForOwner();
        }

        public void HandleAddPet(UnitEntityData master, UnitEntityData pet)
        {
            if (master == Owner) RefreshForOwner();
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

        private void RefreshForOwner()
        {
            var master = Owner;
            Refresh(master?.Get<UnitPartPhantom0085>()?.BoundPet(master));
        }

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
        public void OnEventAboutToTrigger(RuleCalculateWeaponStats evt) { }
        public void OnEventDidTrigger(RuleCalculateWeaponStats evt)
        {
            if (evt.Weapon?.Blueprint?.Category != WeaponCategory.OtherNaturalWeapons) return;
            var part = Owner?.Get<UnitPartPhantom0085>();
            bool large = part != null && part.Emotion == "Anger";
            evt.WeaponDamageDice.Modify(Phantom0085SlamDice.Dice(large, Context?[AbilityRankType.Default] ?? 0), Fact);
        }
    }

    [TypeId("b7e10085a1004500850085008500851a")]
    public sealed class Phantom0085TendrilAttack : ContextAction
    {
        public BlueprintBuffReference DiceBuff;
        public BlueprintBuffReference RerollBuff;
        public BlueprintItemWeaponReference Slam;
        public override string GetCaption() => "Bonded Manifestation tendrils";
        public override void RunAction()
        {
            var caster = Context?.MaybeCaster;
            var target = Target?.Unit;
            if (Phantom0085Link.ConfinedPet(caster) == null ||
                Phantom0085Link.BondedEffect(caster) is not Buff bonded || !bonded.IsTurnedOn || bonded.IsSuppressed ||
                target == null || target.State.IsDead || !target.IsEnemy(caster)) return;
            var slamBlueprint = Slam?.Get();
            var dice = DiceBuff?.Get();
            if (slamBlueprint == null || dice == null) return;
            var slam = new Kingmaker.Items.ItemEntityWeapon(slamBlueprint);
            var diceFact = caster.Buffs.AddBuff(dice, caster, TimeSpan.MaxValue, AbilityParams.Empty);
            Buff rerollFact = null;
            var reroll = RerollBuff?.Get();
            if (reroll != null)
                rerollFact = caster.Buffs.AddBuff(reroll, caster, TimeSpan.MaxValue, AbilityParams.Empty);
            try
            {
                Context.TriggerRule(new RuleAttackWithWeapon(caster, target, slam, 0) { Reason = new RuleReason(Context) });
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
        [HarmonyPatch(typeof(UnitPartTouch), nameof(UnitPartTouch.HandleObjectLeaveCombat))]
        internal static class HeldTouchCleanup
        {
            private static readonly HashSet<BlueprintGuid> EmotionalPowers = new HashSet<BlueprintGuid>(
                Spiritualist0085Ids.Emotions.SelectMany(row => Enumerable.Range(0, row.SlaGuids.Length)
                    .Select(slot => Spiritualist0085Ids.Id(row.Name + "Sla" + slot))));

            internal static bool IsEmotionalPower(BlueprintAbility ability)
                => ability != null && ability.Type == AbilityType.SpellLike &&
                    EmotionalPowers.Contains((ability.Parent ?? ability).AssetGuid);

            [HarmonyTranspiler]
            private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> source)
            {
                var instructions = source.ToList();
                var restore = AccessTools.Method(typeof(Spellbook), nameof(Spellbook.RestoreSpontaneousSlots), new[] { typeof(int), typeof(int) });
                var calls = instructions.Where(i => i.Calls(restore)).ToArray();
                if (calls.Length != 1)
                {
                    ModEntry.LogSpellDiagnostic("0085 held touch cleanup: native spontaneous slot recovery was not found.");
                    return instructions;
                }
                var call = calls[0];
                var argument = new CodeInstruction(OpCodes.Ldarg_0);
                argument.labels.AddRange(call.labels);
                call.labels.Clear();
                call.opcode = OpCodes.Call;
                call.operand = AccessTools.Method(typeof(HeldTouchCleanup), nameof(RestoreSlot));
                instructions.Insert(instructions.IndexOf(call), argument);
                return instructions;
            }

            private static void RestoreSlot(Spellbook book, int level, int amount, UnitPartTouch touch)
            {
                if (!IsEmotionalPower(touch.Ability?.StickyTouch?.Blueprint))
                    book.RestoreSpontaneousSlots(level, amount);
            }
        }

        internal static bool IsPrivatePetGrant(AddPet grant)
            => grant.Type == Spiritualist0085Ids.PhantomPetType && grant.Pet?.GetComponent<Phantom0085Marker>() != null;

        private static bool IsPrivatePhantom(UnitPartPet pet)
            => pet != null && pet.Type == Spiritualist0085Ids.PhantomPetType && Phantom0085Marker.IsPhantom(pet.Owner);

        [HarmonyPatch]
        internal static class ExistingPetReference
        {
            private static readonly System.Reflection.PropertyInfo Owner = AccessTools.Property(typeof(AddPet), "Owner");
            private static readonly System.Reflection.PropertyInfo Data = AccessTools.Property(typeof(AddPet), "Data");

            private static IEnumerable<System.Reflection.MethodBase> TargetMethods()
            {
                yield return AccessTools.Method(typeof(AddPet), "TryUpdatePet");
                yield return AccessTools.Method(typeof(AddPet), "OnDeactivate");
            }

            internal static bool WaitingForRecordedPet(UnitReference reference, UnitEntityData unit)
                => !string.IsNullOrEmpty(reference.UniqueId) && ReferenceEquals(unit, null);

            internal static bool RetiredRecordedPet(UnitEntityData pet)
                => pet != null && pet.DestroyMark && pet.Master == null;

            [HarmonyPrefix]
            private static bool Prefix(AddPet __instance, System.Reflection.MethodBase __originalMethod)
            {
                if (!IsPrivatePetGrant(__instance)) return true;
                var owner = (UnitEntityData)Owner.GetValue(__instance);
                if (owner == null || owner.IsPet || !owner.PreviewOf.IsEmpty) return false;
                // Native AddPet registers its spawn callback while the owner has no holding state.
                if (__originalMethod.Name == "TryUpdatePet" && owner.HoldingState == null) return true;
                var reference = ((AddPetData)Data.GetValue(__instance)).SpawnedPetRef;
                var pet = reference.Value;
                if (WaitingForRecordedPet(reference, pet) || RetiredRecordedPet(pet)) return false;
                if (pet == null || pet.Blueprint == __instance.Pet && (pet.Master == null || pet.Master == owner))
                    return true;
                ModEntry.LogSpellDiagnostic($"0085 ignored foreign recorded pet {pet.UniqueId} for owner {owner.UniqueId}.");
                return false;
            }
        }

        [HarmonyPatch(typeof(AddPet), "TryLevelUpPet")]
        internal static class PetLevelOverride
        {
            [HarmonyTranspiler]
            private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> source)
            {
                var instructions = source.ToList();
                var getter = AccessTools.PropertyGetter(typeof(AddClassLevelsToPets), nameof(AddClassLevelsToPets.LevelsProgression));
                var calls = instructions.Where(i => i.Calls(getter)).ToArray();
                if (calls.Length != 1)
                {
                    ModEntry.LogSpellDiagnostic("0085 pet level override: native owner progression lookup was not found.");
                    return instructions;
                }
                var call = calls[0];
                var argument = new CodeInstruction(OpCodes.Ldarg_0);
                argument.labels.AddRange(call.labels);
                call.labels.Clear();
                call.opcode = OpCodes.Call;
                call.operand = AccessTools.Method(typeof(PetLevelOverride), nameof(OwnerProgression));
                instructions.Insert(instructions.IndexOf(call), argument);
                return instructions;
            }

            internal static AddClassLevels OwnerProgression(AddClassLevelsToPets ownerOverride, AddPet grant)
                => IsPrivatePetGrant(grant) ? null : ownerOverride?.LevelsProgression;
        }

        [HarmonyPatch(typeof(ActivatableAbility), nameof(ActivatableAbility.TryStart))]
        internal static class BondedFirstRound
        {
            [HarmonyPrefix]
            private static void Prefix(ActivatableAbility __instance, out bool __state)
                => __state = !__instance.IsStarted && __instance.Blueprint.AssetGuid == Spiritualist0085Ids.Id("BondedManifestation");

            [HarmonyPostfix]
            private static void Postfix(ActivatableAbility __instance, bool __state)
            {
                if (!__state || !__instance.IsStarted) return;
                // Advance the game's scheduled first tick so switching off before it cannot yield a free activation.
                __instance.OnNewRound();
                if (__instance.IsStarted) __instance.TimeToNextRound = 6f;
            }
        }

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
                // A direct damage rule rejected at entry would leave this damage unpaid.
                if (recipient == null || !CanReceivePayment(deal, recipient)) return;
                Exact.Add(deal, Marker);
                SetResult.Invoke(deal, new object[] { deal.Result - amount });
                SetRedirected.Invoke(deal, new object[] { amount });
                deal.RedirectionTarget = recipient;
            }

            private static bool CanReceivePayment(RuleDealDamage deal, UnitEntityData recipient)
                => !UnitCommand.CommandTargetUntargetable(deal.Initiator, recipient, deal) &&
                    !Kingmaker.Designers.GameHelper.IsAttackingGreenNPC(deal.Initiator, recipient);

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
        private static readonly BlueprintGuid HatedTargetAbility = Spiritualist0085Ids.Id("HatredHatedTargetAbility");
        private static readonly BlueprintGuid PhantomClass = Spiritualist0085Ids.Id("PhantomClass");
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
                if (root == HatedTargetAbility)
                {
                    var unit = __instance.Caster?.Unit;
                    if (Phantom0085Marker.IsPhantom(unit) &&
                        ResourcesLibrary.TryGetBlueprint<BlueprintCharacterClass>(PhantomClass) is BlueprintCharacterClass cls &&
                        unit.Progression.GetClassLevel(cls) >= 7)
                        __result = UnitCommand.CommandType.Swift;
                    return;
                }
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

        // Target validation and the actual rider both call this predicate.
        [HarmonyPatch(typeof(AbilityTargetIsSuitableMount), nameof(AbilityTargetIsSuitableMount.CanMount))]
        internal static class MountTargetExclusion
        {
            [HarmonyPrefix]
            private static bool Prefix(UnitEntityData pet, ref bool __result)
            {
                if (!IsPrivatePhantom(pet?.Get<UnitPartPet>())) return true;
                __result = false;
                return false;
            }
        }

        [HarmonyPatch(typeof(UnitPartPetMaster), "UpdateMountAbility")]
        internal static class MountSuppression
        {
            [HarmonyTranspiler]
            private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> source)
            {
                var instructions = source.ToList();
                var type = AccessTools.PropertyGetter(typeof(UnitPartPet), nameof(UnitPartPet.Type));
                var calls = instructions.Where(instruction => instruction.Calls(type)).ToArray();
                if (calls.Length != 2)
                {
                    ModEntry.LogSpellDiagnostic("0085 mount button: native pet eligibility checks were not found.");
                    return instructions;
                }
                var mountType = AccessTools.Method(typeof(MountSuppression), nameof(NativeMountType));
                foreach (var call in calls)
                {
                    call.opcode = OpCodes.Call;
                    call.operand = mountType;
                }
                return instructions;
            }

            // Feed the native exclusion only within the mount-button scan.
            internal static PetType NativeMountType(UnitPartPet pet)
                => IsPrivatePhantom(pet) ? PetType.Clone : pet.Type;
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
                    // The native rest loop can set Conscious directly if its usual life-state
                    // event did not run. Only revisit visibility when recovery still needs it.
                    if (recovery && Phantom0085Link.Alive(unit) && !part.PendingManifest &&
                        (part.Confined || !unit.IsInGame || unit.Get<UnitPartPet>()?.ShouldBeHidden == true ||
                            !Phantom0085Link.SameLocalArea(master, unit)) && part.BoundPet(master) == unit)
                        Phantom0085Link.RestoreAfterRecovery(master, unit, part);
                }
            }
        }
    }
}

using System;
using System.Linq;
using System.Collections.Generic;
using HarmonyLib;
using Kingmaker.UnitLogic.Buffs;
using Kingmaker.ElementsSystem;
using Kingmaker.Designers.EventConditionActionSystem.Actions;
using Kingmaker.UnitLogic.Abilities.Components.AreaEffects;
using Kingmaker.UnitLogic.Mechanics.Actions;
using Kingmaker.UnitLogic.Mechanics.Conditions;
using System.Runtime.CompilerServices;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.EntitySystem;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.Enums;
using Kingmaker.Enums.Damage;
using Kingmaker.PubSubSystem;
using Kingmaker.RuleSystem;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Buffs.Components;
using Kingmaker.UnitLogic.FactLogic;
using Kingmaker.UnitLogic.Parts;
using Newtonsoft.Json;
using static CallOfTheWild.Wrath.Prototype.PsychicAssets;

namespace CallOfTheWild.Wrath.Prototype
{
    internal static class AntimagicRage01434
    {
        internal const string GhostModule = "Rage power: Ghost Rager";
        internal const string DisruptiveModule = "Rage power: Disruptive";
        internal const string SpellbreakerModule = "Rage power: Spellbreaker";
        internal const string GhostToken = "GhostRagerRagePower";
        internal const string DisruptiveToken = "DisruptiveRagePower";
        internal const string SpellbreakerToken = "SpellbreakerRagePower";
        internal const string DisruptiveFeatId = "f3ef866a9da642c4b9f7dcd76c3567fe";
        internal const string CounterstrikeId = "cd96b7275c206da4899c69ae127ffda6";
        internal static readonly string[] Dependencies = { DisruptiveFeatId, CounterstrikeId,
            "b5dc034170f2f7249bafedf6c6f526dc", "e57906afd2116b94aac2220d5912f4ea", "00c4eb8ffaf4b7d47bea8c8307704f3c",
            "8896f327c59569c4eaf129bf35b96c1f", "5826562b74d529e41af2d60feeca093d" };

        internal static void Register(BlueprintsCache cache) { RegisterGhostRager(cache); RegisterDisruptive(cache); RegisterSpellbreaker(cache); }
        internal static void Optional(BlueprintsCache cache) {
            foreach (var token in new[] { GhostToken, DisruptiveToken, SpellbreakerToken })
                RagePowerMigration01434.Optional(cache, token);
        }
        internal static void RegisterGhostRager(BlueprintsCache cache)
        {
            if (RagePowerMigration01434.Restore(cache, GhostToken)) return;
            var f = new PsychicAssets(cache);
            var superstition = cache.Load(Id("SuperstitionRagePower")) as BlueprintFeature
                ?? throw new InvalidOperationException("Ghost Rager requires the registered Superstition rage power.");
            const string text = "While raging, deal normal physical damage to incorporeal creatures, even with nonmagical weapons. Gain a +3 morale bonus to touch AC at 6th level, increasing to +4 at 8th level and by +1 every 4 levels thereafter, to a maximum of +7 at 20th level. This cannot raise touch AC above full AC. Requires Superstition and 6 qualifying rage-power levels.";
            var root = RagePowerMigration01434.Root(f, GhostToken, "Ghost Rager", text, 6, Migration01426.Require(superstition));
            var effect = f.Buff("GhostRagerRageEffectBuff", "Ghost Rager", text,
                new AddOutgoingPhysicalDamageProperty { AffectAnyPhysicalDamage = true, AddReality = true, Reality = DamageRealityType.Ghost },
                new GhostRagerTouchArmor01434());
            Prepare(root, effect, f.Native<BlueprintFeature>("8896f327c59569c4eaf129bf35b96c1f"));
            RagePowerMigration01434.Commit(f, root, effect);
        }
        internal static void RegisterDisruptive(BlueprintsCache cache)
        {
            if (RagePowerMigration01434.Restore(cache, DisruptiveToken)) return;
            var f = new PsychicAssets(cache);
            var superstition = cache.Load(Id("SuperstitionRagePower")) as BlueprintFeature
                ?? throw new InvalidOperationException("Disruptive requires the registered Superstition rage power.");
            var feat = f.Native<BlueprintFeature>(DisruptiveFeatId);
            var grants = feat.GetComponents<AddFacts>().ToArray();
            if (feat.name != "Disruptive" || grants.Length != 1 || grants[0].Facts.Length != 1 ||
                grants[0].Facts[0]?.AssetGuid != BlueprintGuid.Parse("b5dc034170f2f7249bafedf6c6f526dc"))
                throw new InvalidOperationException("Native Disruptive's feat/aura layout changed; preserved.");
            RageDisruptiveGrant01434.ValidateNativeGrant(feat);
            const string text = "While raging, gain the native Disruptive feat: enemies in its native area take a -4 penalty on concentration checks. Its original range, targeting and concentration rules apply. Requires Superstition and 8 qualifying rage-power levels. This temporary grant does not remove an independently owned Disruptive feat.";
            var root = RagePowerMigration01434.Root(f, DisruptiveToken, "Disruptive", text, 8, Migration01426.Require(superstition));
            var effect = f.Buff("DisruptiveRageEffectBuff", "Disruptive", text, new RageDisruptiveGrant01434());
            Prepare(root, effect, feat);
            RagePowerMigration01434.Commit(f, root, effect);
        }
        internal static void RegisterSpellbreaker(BlueprintsCache cache)
        {
            if (RagePowerMigration01434.Restore(cache, SpellbreakerToken)) return;
            var f = new PsychicAssets(cache);
            var disruptive = cache.Load(Id(DisruptiveToken)) as BlueprintFeature
                ?? throw new InvalidOperationException("Spellbreaker requires the registered Disruptive rage power.");
            var counter = f.Native<BlueprintFeature>(CounterstrikeId);
            var mechanic = counter.GetComponents<AddMagusMechanicPart>().ToArray();
            if (counter.name != "Counterstrike" || mechanic.Length != 1 || Get(mechanic[0], "m_Feature").ToString() != "Counterstrike")
                throw new InvalidOperationException("Native Counterstrike's defensive-casting mechanism changed; preserved.");
            const string text = "While raging, enemies in your threatened area that successfully cast defensively provoke an attack of opportunity from you. This follows original CotW Spellbreaker and the game's native Counterstrike rule; failed defensive casts retain the game's ordinary behavior. Normal melee threat, attack-of-opportunity availability and immunity rules apply. Requires the Disruptive rage power and 12 qualifying rage-power levels.";
            var root = RagePowerMigration01434.Root(f, SpellbreakerToken, "Spellbreaker", text, 12, Migration01426.Require(disruptive));
            var effect = f.Buff("SpellbreakerRageEffectBuff", "Spellbreaker", text, new RageSpellbreakerCounterstrike01434());
            Prepare(root, effect, f.Native<BlueprintFeature>(DisruptiveFeatId));
            RagePowerMigration01434.Commit(f, root, effect);
        }
        static void Prepare(BlueprintFeature root, BlueprintBuff effect, BlueprintFeature icon)
        {
            Set(root, "m_Icon", Get(icon, "m_Icon")); Set(effect, "m_Icon", Get(icon, "m_Icon"));
            effect.Stacking = StackingType.Stack; SetEnum(effect, "m_Flags", "HiddenInUi");
        }
    }

    [TypeId("32630afb910843ff849cb9972cb25397")]
    public sealed class GhostRagerTouchArmor01434 : UnitBuffComponentDelegate, ITargetRulebookHandler<RuleCalculateAC>
    {
        sealed class Applied { }
        static readonly ConditionalWeakTable<RuleCalculateAC, Applied> Applications = new ConditionalWeakTable<RuleCalculateAC, Applied>();
        public void OnEventAboutToTrigger(RuleCalculateAC evt)
        {
            if (!evt.AttackType.IsTouch() || Applications.TryGetValue(evt, out _)) return;
            // A personal rage, inspired rage and shared companion rage may
            // overlap. Only their strongest live source authors this event's
            // modifier; earlier copies must not inflate the cap of later ones.
            Kingmaker.UnitLogic.Buffs.Buff strongest = null; int bonus = 0;
            foreach (var candidate in Owner.Buffs.Enumerable) {
                if (!candidate.IsActive || !candidate.IsTurnedOn || candidate.IsSuppressed || candidate.IsDisposed ||
                    candidate.Blueprint.GetComponent<GhostRagerTouchArmor01434>() == null) continue;
                int value = Math.Min(7, 2 + RagePowerMigration01434.EffectiveLevel(candidate.Context?.MaybeCaster ?? Owner) / 4);
                if (value > bonus) { strongest = candidate; bonus = value; }
            }
            if (!ReferenceEquals(strongest, Fact)) return;
            var ac = Owner.Stats.AC;
            // Use the native descriptor resolver, including already authored
            // rule modifiers. Never permanently change the unit's normal AC.
            using (ac.GetTemporaryModifiersScope(evt.AllBonuses)) {
                int existing = Math.Max(0, ac.GetDescriptorBonus(ModifierDescriptor.Morale));
                bonus = Math.Min(bonus, existing + Math.Max(0, ac.ModifiedValue - ac.Touch));
            }
            if (bonus > 0) { evt.AddModifier(bonus, Fact, ModifierDescriptor.Morale); Applications.Add(evt, new Applied()); }
        }
        public void OnEventDidTrigger(RuleCalculateAC evt) { }
    }

    public sealed class RageDisruptiveState01434
    {
        [JsonProperty] public EntityFactRef<Feature> Feature;
        [JsonProperty] public bool Owned;
    }

    // Native AddFacts merges rank-one features, then removes the merged fact
    // when its host ends. Keep explicit ownership of our temporary instance.
    [TypeId("1eed761b63074fed8fa97137c0514bc2")]
    public sealed class RageDisruptiveGrant01434 : UnitBuffComponentDelegate<RageDisruptiveState01434>, IUnitLostFactHandler
    {
        sealed class Guard { internal bool Changing; internal Feature Creating; }
        sealed class Entry { internal Buff Buff; internal RageDisruptiveState01434 State; }
        static readonly ConditionalWeakTable<UnitEntityData, Guard> Guards = new ConditionalWeakTable<UnitEntityData, Guard>();
        static readonly BlueprintGuid NativeId = BlueprintGuid.Parse(AntimagicRage01434.DisruptiveFeatId);
        protected override void OnActivate() => Synchronize(Owner, null, Data);
        // Match native temporary grants: manager TurnOff/On is suspension,
        // not removal, and must not mutate its enumerated fact collection.
        protected override void OnDeactivate() { if (!Buff.IsReapplying) Synchronize(Owner, Buff, Data); }
        protected override void OnDispose() => Synchronize(Owner, Buff, Data);
        protected override void OnApplyPostLoadFixes() => Synchronize(Owner, null, Data);
        public void HandleUnitLostFact(EntityFact fact) {
            if (fact?.Blueprint?.AssetGuid == NativeId) Synchronize(Owner, null, Data);
        }
        static List<Entry> Entries(UnitEntityData owner) {
            var result = new List<Entry>();
            foreach (var buff in owner.Buffs.Enumerable.ToArray())
                foreach (var runtime in buff.Components)
                    if (runtime.SourceBlueprintComponent is RageDisruptiveGrant01434)
                        result.Add(new Entry { Buff = buff, State = runtime.GetData<RageDisruptiveState01434>() });
            return result;
        }
        static bool Live(Buff buff) => buff.IsActive && !buff.IsDisposed;
        static Feature Resolve(UnitEntityData owner, EntityFactRef<Feature> reference) =>
            owner.Progression.Features.Enumerable.FirstOrDefault(f => f.UniqueId == reference.FactId && f.Blueprint.AssetGuid == NativeId && !f.IsDisposed);
        static void Synchronize(UnitEntityData owner, Buff excluding, RageDisruptiveState01434 departing) {
            // Native Dispose runs after Detach has cleared the owner. The
            // active lifetime was already released by OnDeactivate.
            if (owner == null) return;
            var guard = Guards.GetOrCreateValue(owner); if (guard.Changing) return;
            guard.Changing = true;
            try {
                var entries = Entries(owner);
                // Manager.Remove may remove the departing buff from its list
                // before invoking Deactivate/Dispose. Its receipt still owns
                // cleanup, even when it is the last source.
                if (excluding != null && !entries.Any(e => ReferenceEquals(e.Buff, excluding))) entries.Add(new Entry { Buff = excluding, State = departing });
                var active = entries.Where(e => !ReferenceEquals(e.Buff, excluding) && Live(e.Buff)).ToArray();
                var owned = entries.Where(e => e.State.Owned).Select(e => Resolve(owner, e.State.Feature)).FirstOrDefault(f => f?.Manager != null);
                var current = owner.Progression.Features.Enumerable.FirstOrDefault(f => f.Blueprint.AssetGuid == NativeId && !f.IsDisposed);
                if (active.Length == 0) {
                    foreach (var e in entries) { e.State.Feature = default; e.State.Owned = false; }
                    if (owned?.Manager != null) owner.Facts.Remove(owned);
                    return;
                }
                if (current == null) {
                    // A null context prevents a later legitimate acquisition
                    // from retaining a reference to a removed rage source.
                    var blueprint = ResourcesLibrary.TryGetBlueprint<BlueprintFeature>(NativeId);
                    ValidateNativeGrant(blueprint);
                    var candidate = new Feature(blueprint, owner.Descriptor) { IsTemporary = true };
                    guard.Creating = candidate;
                    try { current = owner.Facts.Add(candidate); } finally { guard.Creating = null; }
                    owned = current;
                }
                foreach (var e in entries) {
                    bool live = active.Contains(e);
                    e.State.Feature = live ? (EntityFactRef<Feature>)current : default;
                    e.State.Owned = live && ReferenceEquals(current, owned);
                }
            } finally { guard.Changing = false; }
        }
        internal static void ValidateNativeGrant(BlueprintFeature blueprint) {
            var grants = blueprint?.GetComponents<AddFacts>().ToArray();
            if (blueprint?.AssetGuid != NativeId || blueprint.GetType() != typeof(BlueprintFeature) || blueprint.name != "Disruptive" ||
                grants.Length != 1 || grants[0].Facts.Length != 1 || !(grants[0].Facts[0] is BlueprintBuff buff) || buff.AssetGuid.ToString() != "b5dc034170f2f7249bafedf6c6f526dc" ||
                blueprint.ComponentsArray.Any(c => c == null || (!(c is AddFacts) && !(c is Kingmaker.Blueprints.Classes.Prerequisites.Prerequisite) &&
                    !(c.GetType().Assembly == typeof(Feature).Assembly && c.GetType().Name == "FeatureTagsComponent"))) ||
                buff.ComponentsArray.Length != 1 || !(buff.ComponentsArray[0] is AddAreaEffect area) ||
                area.AreaEffect?.AssetGuid.ToString() != "e57906afd2116b94aac2220d5912f4ea")
                throw new InvalidOperationException("Disruptive rage grant requires the exact native passive feat/aura graph; changed graph preserved.");
            var effect = ResourcesLibrary.TryGetBlueprint<BlueprintBuff>(BlueprintGuid.Parse("00c4eb8ffaf4b7d47bea8c8307704f3c"));
            if (effect?.ComponentsArray?.Length != 1 || effect.ComponentsArray[0]?.GetType().FullName != "Kingmaker.Designers.Mechanics.Facts.ConcentrationBonus" ||
                area.AreaEffect.ComponentsArray.Length != 1 || area.AreaEffect.ComponentsArray[0]?.GetType().FullName != "Kingmaker.UnitLogic.Abilities.Components.AreaEffects.AbilityAreaEffectRunAction")
                throw new InvalidOperationException("Disruptive's native concentration aura changed; grant rejected.");
            bool Passive(GameAction action) {
                if (action is Conditional condition) return condition.ConditionsChecker?.Conditions != null &&
                    condition.ConditionsChecker.Conditions.All(c => c is ContextConditionIsAlly) &&
                    (condition.IfTrue?.Actions ?? Array.Empty<GameAction>()).All(Passive) &&
                    (condition.IfFalse?.Actions ?? Array.Empty<GameAction>()).All(Passive);
                if (action is ContextActionApplyBuff apply) return apply.Buff == effect;
                if (action is ContextActionRemoveBuff remove) return remove.Buff == effect;
                return false;
            }
            var actions = (AbilityAreaEffectRunAction)area.AreaEffect.ComponentsArray[0];
            if (new[] { actions.UnitEnter, actions.UnitExit, actions.UnitMove, actions.Round }.Any(list => (list?.Actions ?? Array.Empty<GameAction>()).Any(action => !Passive(action))))
                throw new InvalidOperationException("Disruptive's native aura contains a non-passive payload; grant rejected.");
        }
        internal static void HandOff(Feature incoming, Feature existing) {
            if (incoming?.Blueprint?.AssetGuid != NativeId || existing?.Blueprint?.AssetGuid != NativeId || ReferenceEquals(incoming, existing)) return;
            var owner = existing.Owner?.Unit; if (owner == null) return;
            if (!Guards.TryGetValue(owner, out var guard) || ReferenceEquals(guard.Creating, incoming)) return;
            var receipts = Entries(owner).Where(e => e.State.Owned && ReferenceEquals(Resolve(owner, e.State.Feature), existing)).ToArray();
            if (receipts.Length == 0) return;
            // The native collection has accepted another source for this exact
            // rank-one fact. Transfer it instead of deleting that source later.
            existing.SetSameSource(incoming); existing.IsTemporary = incoming.IsTemporary; existing.IgnorePrerequisites = incoming.IgnorePrerequisites;
            foreach (string field in new[] { "<SourceFact>k__BackingField", "<SourceItem>k__BackingField", "<SourceAbility>k__BackingField", "<SourceCutscene>k__BackingField" }) {
                var member = AccessTools.Field(typeof(EntityFact), field); member.SetValue(existing, member.GetValue(incoming));
            }
            AccessTools.Field(typeof(Feature), "m_Context").SetValue(existing, incoming.Context);
            foreach (var entry in receipts) entry.State.Owned = false;
        }
    }

    [HarmonyPatch(typeof(FeatureCollection), "PrepareFactForAttach", new[] { typeof(Feature) })]
    internal static class RageDisruptiveAcquisition01434
    {
        static void Postfix(Feature __0, Feature __result) => RageDisruptiveGrant01434.HandOff(__0, __result);
    }

    public sealed class RageCounterstrikeState01434
    {
        // Native UnitPartMagus.Counterstrike persists its count. Persist this
        // fact's receipt as well, so loading does not retain or release twice.
        [JsonProperty] public bool Retained;
        [JsonIgnore] public UnitPartMagus RetainedPart;
    }

    [TypeId("f60062c809f2441b82d512c41b4f55d2")]
    public sealed class RageSpellbreakerCounterstrike01434 : UnitBuffComponentDelegate<RageCounterstrikeState01434>, IUnitLostFactHandler
    {
        protected override void OnTurnOn()
        {
            var part = Owner.Get<UnitPartMagus>();
            if (Data.Retained && part != null && part.Counterstrike.Count > 0 &&
                (Data.RetainedPart == null || ReferenceEquals(Data.RetainedPart, part))) {
                Data.RetainedPart = part; return;
            }
            Release(); part = Owner.Ensure<UnitPartMagus>();
            part.Counterstrike.Retain(); Data.Retained = true; Data.RetainedPart = part;
        }
        protected override void OnTurnOff() => Release();
        public void HandleUnitLostFact(EntityFact fact)
        {
            // Native AddMagusMechanicPart may remove its entire part after a
            // non-magus loses that fact. Transfer only our outstanding receipt
            // to the current part; no foreign mechanic or fact is recreated.
            if (Data.Retained && Fact.IsTurnedOn && !Fact.IsDisposed && !Buff.IsSuppressed &&
                !ReferenceEquals(Owner.Get<UnitPartMagus>(), Data.RetainedPart)) OnTurnOn();
        }
        internal static void ReconcileAfterNativeRemoval(UnitEntityData owner)
        {
            if (owner == null || owner.Get<UnitPartMagus>() != null) return;
            foreach (var buff in owner.Buffs.Enumerable.ToArray()) {
                if (!buff.IsActive || !buff.IsTurnedOn || buff.IsDisposed || buff.IsSuppressed) continue;
                foreach (var runtime in buff.Components)
                    if (runtime.SourceBlueprintComponent is RageSpellbreakerCounterstrike01434 component && runtime.GetData<RageCounterstrikeState01434>().Retained)
                        using (runtime.RequestEventContext()) component.OnTurnOn();
            }
        }
        void Release()
        {
            if (!Data.Retained) return;
            var part = Data.RetainedPart ?? Owner.Get<UnitPartMagus>();
            Data.Retained = false; Data.RetainedPart = null;
            if (part != null && part.Counterstrike.Count > 0) part.Counterstrike.Release();
            // Do not remove UnitPartMagus: other facts can own Counterstrike,
            // Wand Wielder, Spell Combat or Spellstrike on the same unit.
        }
    }
    // Native lost-fact notifications precede component deactivation. This one
    // native endpoint may then delete the complete part; reconcile afterwards.
    [HarmonyPatch(typeof(AddMagusMechanicPart), "OnDeactivate")]
    internal static class RageSpellbreakerNativeRemoval01434
    {
        static void Postfix(AddMagusMechanicPart __instance)
        {
            var owner = AccessTools.Property(typeof(AddMagusMechanicPart), "Owner").GetValue(__instance, null) as UnitEntityData;
            RageSpellbreakerCounterstrike01434.ReconcileAfterNativeRemoval(owner);
        }
    }

}

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using HarmonyLib;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Prerequisites;
using Kingmaker.Blueprints.Classes.Selection;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Enums;
using Kingmaker.PubSubSystem;
using Kingmaker.RuleSystem;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.RuleSystem.Rules.Abilities;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components;
using Kingmaker.UnitLogic.Class.LevelUp;
using Kingmaker.UnitLogic.Mechanics;
using static CallOfTheWild.Wrath.Prototype.PsychicAssets;

namespace CallOfTheWild.Wrath.Prototype
{
    // These two discoveries have separate publication/rollback boundaries.
    // Native and provider selections are extended, never rebuilt.
    internal static class WizardDiscoveries01435
    {
        internal const string ForestModule = "Arcane discovery: Forest's Blessing";
        internal const string ResilientModule = "Arcane discovery: Resilient Illusions";
        internal const string ForestToken = "ForestsBlessingDiscovery";
        internal const string ResilientToken = "ResilientIllusionsDiscovery";
        internal const string WizardId = "ba34257984f4c41408ce1dc2004e342e";
        internal const string ArcanistId = "52dbfd8505e22f84fad8d702611f60b7";
        internal const string WizardListId = "ba0401fdeb4062f40a7aa95b6f07fe89";
        internal const string DruidListId = "bad8638d40639d04fa2f80a1cac67d6b";
        internal const string DiscoveryPoolId = "45b72087ac2d4554aec4f44be852eddd";
        internal static readonly string[] CorePoolIds = {
            BlueprintRegistration.BasicFeatsId, "e10c4f18a6c8b4342afe6954bde0587b",
            "8c3102c2ff3b69444b139a98521a4899", "689959eef3e972e458b52598dcc2c752"
        };
        // The original CotW explicitly marks exactly these phantasmal spells as
        // disbelief effects. Fortitude follow-up saves are not disbelief saves.
        internal static readonly string[] PhantasmalIds = {
            "6717dbaef00c0eb4897a1c908a75dfe5", "870af83be6572594d84d276d7fc583e0",
            "12fb4a4c22549c74d949e2916a2f0b6a", "1f2e6019ece86d64baa5effa15e81ecc"
        };
        internal static string LastOptionalDiagnostic = "Arcane discovery provider not detected";
        sealed class Publication
        {
            internal BlueprintFeature Root;
            internal string Definition;
        }
        sealed class State {
            internal readonly Publication Forest = new Publication(), Resilient = new Publication();
            // The additional discovery has its own publication and rollback
            // receipt. It does not change the identities of the 035 leaves.
            internal BlueprintFeature TimeStutter;
        }
        static readonly ConditionalWeakTable<BlueprintsCache, State> Published = new ConditionalWeakTable<BlueprintsCache, State>();
        sealed class EntryReceipt
        {
            internal BlueprintFeatureSelection Pool;
            internal State State;
            internal WizardDiscoveryEntry01436 Adapter;
            internal string Definition;
        }
        static readonly ConditionalWeakTable<WizardDiscoveryEntry01436, EntryReceipt> Entries = new ConditionalWeakTable<WizardDiscoveryEntry01436, EntryReceipt>();
        static string Layout(BlueprintFeature root) => Migration01426.Definition(new object[] { root.name, root.GetType().FullName, root.ComponentsArray, root.Groups, root.Ranks, root.IsClassFeature });
        static BlueprintFeatureSelection OptionalPool(BlueprintsCache cache)
        {
            var raw = cache.Load(BlueprintGuid.Parse(DiscoveryPoolId));
            if (raw == null) { LastOptionalDiagnostic = "Arcane discovery provider not detected"; return null; }
            if (!(raw is BlueprintFeatureSelection pool) || pool.name != "ArcaneDiscoverySelection" || pool.IgnorePrerequisites ||
                !(pool.Group == FeatureGroup.WizardFeat || pool.Groups?.Contains(FeatureGroup.WizardFeat) == true)) {
                LastOptionalDiagnostic = "Arcane discovery provider selection incomplete or changed; saved discovery facts retained"; return null;
            }
            return pool;
        }
        static bool OwnRoot(EntryReceipt receipt, BlueprintFeature root) => root != null &&
            (ReferenceEquals(root, receipt.State.Forest.Root) || ReferenceEquals(root, receipt.State.Resilient.Root) ||
                ReferenceEquals(root, receipt.State.TimeStutter));
        internal static bool EligibleArcanist(WizardDiscoveryEntry01436 adapter, FeatureSelectionState selection, UnitDescriptor unit, LevelUpState state)
        {
            if (unit == null || !Entries.TryGetValue(adapter, out var receipt) || receipt.Pool.ComponentsArray?.Contains(adapter) != true) return false;
            foreach (var root in new[] { receipt.State.Forest.Root, receipt.State.Resilient.Root, receipt.State.TimeStutter }) {
                if (root == null || unit.Progression.Features.GetRank(root) >= root.Ranks) continue;
                // Use the leaf's native class prerequisite, including native
                // ClassLevelsForPrerequisites semantics. Never add class levels.
                if (root.GetComponents<PrerequisiteClassLevel>().Any(p => p.CharacterClass?.AssetGuid == BlueprintGuid.Parse(ArcanistId) && p.Check(selection, unit, state)) &&
                    root.MeetsPrerequisites(selection, unit, state, false, false)) return true;
            }
            return false;
        }
        internal static bool RestrictArcanistChoice(BlueprintFeatureSelection pool, UnitDescriptor unit, LevelUpState state, FeatureSelectionState selection, IFeatureSelectionItem item)
        {
            var adapter = pool?.GetComponents<WizardDiscoveryEntry01436>().FirstOrDefault();
            if (adapter == null || !Entries.TryGetValue(adapter, out var receipt) || !ReferenceEquals(receipt.Pool, pool) ||
                adapter.WizardEligible(selection, unit, state) || !EligibleArcanist(adapter, selection, unit, state)) return false;
            return !OwnRoot(receipt, item?.Feature);
        }
        static WizardDiscoveryEntry01436 Adapt(BlueprintFeatureSelection pool, State state)
        {
            if (pool.ComponentsArray == null || pool.ComponentsArray.Any(c => c == null)) throw new InvalidOperationException("Arcane discovery provider components incomplete.");
            var adapters = pool.GetComponents<WizardDiscoveryEntry01436>().ToArray();
            if (adapters.Length > 0) {
                var adapter = adapters[0];
                if (adapters.Length != 1 || !Entries.TryGetValue(adapter, out var receipt) || !ReferenceEquals(receipt.Pool, pool) ||
                    !ReferenceEquals(receipt.State, state) || Migration01426.Definition(adapter) != receipt.Definition)
                    throw new InvalidOperationException("Arcane discovery provider entry adapter changed.");
                return adapter;
            }
            // ECM's current ArcanistExploits integration deliberately replaces
            // TTT's Wizard 1/All gate with Wizard 1/Any + Arcanist 1/Any. That is
            // an existing native permission, not our narrower fallback. Keep
            // both original components and all of its other discoveries intact.
            var classes = pool.GetComponents<PrerequisiteClassLevel>().ToArray();
            if (classes.Length == 2 && classes.All(p => p.GetType() == typeof(PrerequisiteClassLevel) &&
                p.Level == 1 && !p.Not && p.Group == Prerequisite.GroupType.Any) &&
                classes.Count(p => p.CharacterClass?.AssetGuid == BlueprintGuid.Parse(WizardId)) == 1 &&
                classes.Count(p => p.CharacterClass?.AssetGuid == BlueprintGuid.Parse(ArcanistId)) == 1) return null;
            var wizard = pool.GetComponents<PrerequisiteClassLevel>().Where(p => p.GetType() == typeof(PrerequisiteClassLevel) &&
                p.CharacterClass?.AssetGuid == BlueprintGuid.Parse(WizardId)).ToArray();
            if (classes.Length != 1 || wizard.Length != 1 || wizard[0].Level != 1 || wizard[0].Not || wizard[0].Group != Prerequisite.GroupType.All)
                throw new InvalidOperationException("Arcane discovery provider original wizard entry prerequisite changed.");
            var original = wizard[0];
            var added = new WizardDiscoveryEntry01436 {
                name = original.name, Level = original.Level, Not = original.Not, Group = original.Group,
                CheckInProgression = original.CheckInProgression, HideInUI = original.HideInUI, IsFeatureSelectionWhiteList = original.IsFeatureSelectionWhiteList
            };
            Set(added, "m_CharacterClass", Ref<BlueprintCharacterClassReference>(original.CharacterClass));
            pool.ComponentsArray = pool.ComponentsArray.Select(c => ReferenceEquals(c, original) ? added : c).ToArray(); pool.OnEnable();
            Entries.Add(added, new EntryReceipt { Pool = pool, State = state, Adapter = added, Definition = Migration01426.Definition(added) });
            return added;
        }
        static void RestoreComponents(BlueprintFeatureSelection pool, object components)
        {
            pool.ComponentsArray = (BlueprintComponent[])components;
            try { pool.OnEnable(); }
            catch (Exception e) { ModEntry.LogSpellDiagnostic("Arcane discovery optional provider rollback refresh deferred: " + e); }
        }
        static void ReconcileAccess(BlueprintsCache cache, State state, BlueprintFeature root)
        {
            var pools = new List<BlueprintFeatureSelection>();
            BlueprintFeatureSelection provider = null; WizardDiscoveryEntry01436 added = null;
            object originalComponents = null; var before = new Dictionary<BlueprintFeatureSelection, object[]>();
            try {
                pools.AddRange(Snapshot(cache));
                provider = OptionalPool(cache);
                if (provider != null) {
                    if (!pools.Contains(provider)) pools.Add(provider);
                    originalComponents = provider.ComponentsArray;
                    try { var adapter = Adapt(provider, state); if (adapter != null && !((BlueprintComponent[])originalComponents).Contains(adapter)) added = adapter; }
                    catch (Exception e) { RestoreComponents(provider, originalComponents); provider = null; LastOptionalDiagnostic = "Arcane discovery provider entry deferred: " + e.Message; }
                }
                var forestId = Id(ForestToken); var resilientId = Id(ResilientToken); var timeStutterId = Id(TimeStutter01437.RootToken);
                foreach (var pool in pools) {
                    before.Add(pool, new[] { Get(pool, "m_Features"), Get(pool, "m_AllFeatures") });
                    // A changed or not-yet-complete provider must not make us
                    // delete leaves already placed in its actual submenu. An
                    // unknown shape is deferred; direct leaves in unrelated
                    // ordinary/external selections are still scoped out.
                    if (pool.AssetGuid == BlueprintGuid.Parse(DiscoveryPoolId)) continue;
                    foreach (string field in new[] { "m_Features", "m_AllFeatures" }) {
                        var previous = Get(pool, field) as BlueprintFeatureReference[];
                        if (previous == null) continue;
                        var kept = previous.Where(r => r?.Guid != forestId && r?.Guid != resilientId && r?.Guid != timeStutterId).ToArray();
                        if (kept.Length != previous.Length) Set(pool, field, kept);
                    }
                }
                if (provider != null) {
                    T09CombatFeats.Append(new[] { provider }, new[] { root });
                    LastOptionalDiagnostic = "Arcane discovery provider selection connected; wizard or qualified arcanist entry";
                }
            }
            catch (Exception e) {
                foreach (var pair in before) { Set(pair.Key, "m_Features", pair.Value[0]); Set(pair.Key, "m_AllFeatures", pair.Value[1]); }
                if (provider != null && originalComponents != null) RestoreComponents(provider, originalComponents);
                if (added != null) Entries.Remove(added);
                LastOptionalDiagnostic = "Arcane discovery provider connection deferred: " + e.GetType().Name + ": " + e.Message;
            }
        }
        static BlueprintFeatureSelection[] Snapshot(BlueprintsCache cache)
        {
            // Match the synchronization and bounded retry policy already used
            // by SpellListCompatibility. ToyBox can replace entries unlocked;
            // ValueCollection.CopyTo avoids its invalidated enumerators.
            var gate = AccessTools.Field(typeof(BlueprintsCache), "m_Lock")?.GetValue(cache);
            var loaded = AccessTools.Field(typeof(BlueprintsCache), "m_LoadedBlueprints")?.GetValue(cache) as IDictionary;
            if (gate == null || loaded == null) throw new InvalidOperationException("Wizard discovery cache snapshot unavailable.");
            var blueprint = AccessTools.Field(loaded.GetType().GetGenericArguments()[1], "Blueprint");
            object[] values;
            for (int attempt = 0; ; attempt++) {
                try { lock (gate) { values = new object[loaded.Count]; loaded.Values.CopyTo(values, 0); } break; }
                catch (Exception e) when (attempt < 2 && (e is InvalidOperationException || e is ArgumentException || e is IndexOutOfRangeException)) { }
            }
            return values.Where(v => v != null).Select(v => blueprint.GetValue(v)).OfType<BlueprintFeatureSelection>().ToArray();
        }
        static void RegisterOne(BlueprintsCache cache, bool forest)
        {
            var state = Published.GetValue(cache, _ => new State()); lock (state) {
                var published = forest ? state.Forest : state.Resilient;
                if (published.Root != null) {
                    if (!ReferenceEquals(cache.Load(published.Root.AssetGuid), published.Root) || Layout(published.Root) != published.Definition)
                        throw new InvalidOperationException("Wizard discovery published definition changed: " + published.Root.name);
                    ReconcileAccess(cache, state, published.Root); return;
                }
                string token = forest ? ForestToken : ResilientToken;
                if (cache.Load(Id(token)) != null) throw new InvalidOperationException("Wizard discovery private identity already exists without this publication: " + token);
                var f = new PsychicAssets(cache); var cls = f.Native<BlueprintCharacterClass>(WizardId);
                var level = new PrerequisiteClassLevel { Level = forest ? 5 : 8, Group = Prerequisite.GroupType.Any }; Set(level, "m_CharacterClass", Ref<BlueprintCharacterClassReference>(cls));
                var arcanist = new PrerequisiteClassLevel { Level = forest ? 10 : 16, Group = Prerequisite.GroupType.Any }; Set(arcanist, "m_CharacterClass", Ref<BlueprintCharacterClassReference>(f.Native<BlueprintCharacterClass>(ArcanistId)));
                BlueprintComponent logic;
                if (forest) {
                    logic = new ForestBlessing01435 {
                        WizardList = Ref<BlueprintSpellListReference>(f.Native<BlueprintSpellList>(WizardListId)),
                        DruidList = Ref<BlueprintSpellListReference>(f.Native<BlueprintSpellList>(DruidListId))
                    };
                } else {
                    foreach (var id in PhantasmalIds) {
                        var spell = f.Native<BlueprintAbility>(id);
                        if (spell.Type != AbilityType.Spell || spell.School != SpellSchool.Illusion)
                            throw new InvalidOperationException("Resilient Illusions original disbelief spell changed: " + id);
                    }
                    logic = new ResilientIllusions01435();
                }
                var root = f.Feature(token, forest ? "Forest's Blessing" : "Resilient Illusions", forest
                    ? "You cast spells that appear on both the wizard and druid spell lists at +1 caster level and with +1 to their saving throw DC. The casting spellbook does not have to be a wizard spellbook. Select through Arcane Discovery. Requires wizard level 5 or arcanist level 10; these class levels are not added together."
                    : "When a creature attempts to disbelieve your illusion, roll 1d20 + the illusion's caster level. Its save DC is the higher of its current DC and that result. Applies to native shadow disbelief and the Will disbelief saves of Phantasmal Killer, Weird, Phantasmal Web and Phantasmal Putrefaction; their Fortitude saves and other illusion saves are unchanged. Select through Arcane Discovery. Requires wizard level 8 or arcanist level 16; these class levels are not added together.", level, arcanist, logic);
                root.Groups = Array.Empty<FeatureGroup>(); root.IsClassFeature = true;
                var once = new PrerequisiteNoFeature(); Set(once, "m_Feature", Ref<BlueprintFeatureReference>(root));
                root.ComponentsArray = root.ComponentsArray.Concat(new[] { once }).ToArray();
                Set(root, "m_Icon", f.Native<BlueprintAbility>(forest ? "0fd00984a2c0e0a429cf1a911b4ec5ca" : "6717dbaef00c0eb4897a1c908a75dfe5").Icon);
                Migration01426.Commit(f, Array.Empty<BlueprintFeatureSelection>(), root);
                published.Root = (BlueprintFeature)cache.Load(root.AssetGuid); published.Definition = Layout(published.Root);
                ReconcileAccess(cache, state, published.Root);
            }
        }
        internal static void RegisterForest(BlueprintsCache cache) => RegisterOne(cache, true);
        internal static void RegisterResilient(BlueprintsCache cache) => RegisterOne(cache, false);
        internal static void Register(BlueprintsCache cache) { RegisterForest(cache); RegisterResilient(cache); }
        internal static void ReconcileTimeStutter(BlueprintsCache cache, BlueprintFeature root)
        {
            if (!TimeStutter01437.IsPublishedRoot(cache, root)) throw new InvalidOperationException("Time Stutter discovery access requires its exact publication receipt.");
            var state = Published.GetValue(cache, _ => new State());
            lock (state) { state.TimeStutter = root; ReconcileAccess(cache, state, root); }
        }
        internal static void RegisterOptional(BlueprintsCache cache)
        {
            if (cache.Load(Id(ForestToken)) != null) RegisterForest(cache);
            if (cache.Load(Id(ResilientToken)) != null) RegisterResilient(cache);
        }
    }

    [TypeId("789ecdfd64d0444c8eff01d535a03601")]
    public sealed class WizardDiscoveryEntry01436 : PrerequisiteClassLevel
    {
        internal bool WizardEligible(FeatureSelectionState selection, UnitDescriptor unit, LevelUpState state) => unit != null && base.CheckInternal(selection, unit, state);
        protected override bool CheckInternal(FeatureSelectionState selection, UnitDescriptor unit, LevelUpState state) =>
            WizardEligible(selection, unit, state) || WizardDiscoveries01435.EligibleArcanist(this, selection, unit, state);
        protected override string GetUITextInternal(UnitDescriptor unit) => base.GetUITextInternal(unit) + " OR qualified arcanist (Forest's Blessing: level 10; Resilient Illusions: level 16; Time Stutter: level 20; only these discoveries).";
    }

    [HarmonyPatch(typeof(BlueprintFeatureSelection), nameof(BlueprintFeatureSelection.CanSelect))]
    internal static class WizardDiscoveryChoice01436Patch
    {
        [HarmonyPrefix, HarmonyPriority(Priority.First)]
        static bool Prefix(BlueprintFeatureSelection __instance, UnitDescriptor __0, LevelUpState __1, FeatureSelectionState __2, IFeatureSelectionItem __3, ref bool __result)
        {
            if (!WizardDiscoveries01435.RestrictArcanistChoice(__instance, __0, __1, __2, __3)) return true;
            __result = false; return false;
        }
    }

    [TypeId("789ecdfd64d0444c8eff01d535a03501")]
    public sealed class ForestBlessing01435 : UnitFactComponentDelegate, IInitiatorRulebookHandler<RuleCalculateAbilityParams>, ISubscriber
    {
        public BlueprintSpellListReference WizardList, DruidList;
        sealed class Applied { }
        static readonly ConditionalWeakTable<RuleCalculateAbilityParams, Applied> Seen = new ConditionalWeakTable<RuleCalculateAbilityParams, Applied>();
        public void OnEventAboutToTrigger(RuleCalculateAbilityParams e)
        {
            if (!Fact.IsActive || !Fact.IsTurnedOn || e.Initiator != Owner || e.Spell?.Type != AbilityType.Spell) return;
            var spell = e.Spell; var wizard = WizardList?.Get(); var druid = DruidList?.Get();
            if (wizard == null || druid == null) return;
            bool listed = wizard.Contains(spell) && druid.Contains(spell);
            if (!listed) {
                var parent = spell.Parent;
                listed = parent?.Type == AbilityType.Spell && parent.GetComponent<AbilityVariants>()?.Variants.Any(v => ReferenceEquals(v, spell)) == true && wizard.Contains(parent) && druid.Contains(parent);
            }
            if (!listed || Seen.TryGetValue(e, out _)) return;
            Seen.Add(e, new Applied()); e.AddBonusCasterLevel(1, ModifierDescriptor.UntypedStackable); e.AddBonusDC(1, ModifierDescriptor.UntypedStackable);
        }
        public void OnEventDidTrigger(RuleCalculateAbilityParams e) { }
    }

    [TypeId("789ecdfd64d0444c8eff01d535a03502")]
    public sealed class ResilientIllusions01435 : UnitFactComponentDelegate, IGlobalRulebookHandler<RuleSavingThrow>, IGlobalSubscriber
    {
        sealed class Applied { }
        static readonly ConditionalWeakTable<RuleSavingThrow, Applied> Seen = new ConditionalWeakTable<RuleSavingThrow, Applied>();
        static readonly HashSet<BlueprintGuid> Phantasmal = new HashSet<BlueprintGuid>(WizardDiscoveries01435.PhantasmalIds.Select(BlueprintGuid.Parse));
        public void OnEventAboutToTrigger(RuleSavingThrow e)
        {
            var context = e.Reason?.Context;
            if (!Fact.IsActive || !Fact.IsTurnedOn || context?.MaybeCaster != Owner || context.Params == null || e.Type != SavingThrowType.Will) return;
            var spell = context.SourceAbility;
            // A native shadow conversion uses the emulated spell's school in
            // its execution context. Its scoped disbelief remains an illusion
            // check even when the copied payload is Evocation/Conjuration.
            bool phantasmal = context.SpellSchool == SpellSchool.Illusion && spell != null && spell.Type == AbilityType.Spell && Phantasmal.Contains(spell.AssetGuid);
            if (!phantasmal && !ResilientDisbeliefScope01435.Matches(context, e.Initiator) || Seen.TryGetValue(e, out _)) return;
            Seen.Add(e, new Applied());
            int check = RulebookEvent.Dice.D(new DiceFormula(1, DiceType.D20)) + context.Params.CasterLevel;
            if (check > e.DifficultyClass) e.AddBonusDC(check - e.DifficultyClass);
        }
        public void OnEventDidTrigger(RuleSavingThrow e) { }
    }

    // A save caused by a shadow-emulated spell is not automatically disbelief.
    // Only the original TryAffectByShadow invocation establishes this scope.
    [HarmonyPatch(typeof(MechanicsContext), nameof(MechanicsContext.TryAffectByShadow))]
    internal static class ResilientDisbeliefScope01435
    {
        internal sealed class Frame { internal MechanicsContext Context; internal UnitEntityData Target; }
        [ThreadStatic] static List<Frame> Frames;
        internal static bool Matches(MechanicsContext context, UnitEntityData target) => Frames != null && Frames.Count > 0 &&
            ReferenceEquals(Frames[Frames.Count - 1].Context, context) && ReferenceEquals(Frames[Frames.Count - 1].Target, target);
        static void Prefix(MechanicsContext __instance, UnitEntityData target, out Frame __state)
        {
            __state = null;
            if (__instance == null || !__instance.IsShadow || target == null) return;
            __state = new Frame { Context = __instance, Target = target }; (Frames ?? (Frames = new List<Frame>())).Add(__state);
        }
        static Exception Finalizer(Exception __exception, Frame __state)
        {
            if (__state != null && Frames != null) Frames.Remove(__state);
            return __exception;
        }
    }
}

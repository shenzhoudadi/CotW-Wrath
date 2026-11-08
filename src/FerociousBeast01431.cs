using System;
using System.Collections.Generic;
using System.Linq;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Prerequisites;
using Kingmaker.Blueprints.Classes.Selection;
using Kingmaker.Blueprints.Facts;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.Controllers.Units;
using Kingmaker.Designers.EventConditionActionSystem.Actions;
using Kingmaker.Designers.Mechanics.Buffs;
using Kingmaker.Designers.Mechanics.Facts;
using Kingmaker.ElementsSystem;
using Kingmaker.EntitySystem;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Enums;
using Kingmaker.PubSubSystem;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Buffs;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Buffs.Components;
using Kingmaker.UnitLogic.FactLogic;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Mechanics.Actions;
using Kingmaker.UnitLogic.Mechanics.Components;
using Kingmaker.UnitLogic.Mechanics.Conditions;
using Kingmaker.UnitLogic.Parts;
using Newtonsoft.Json;
using static CallOfTheWild.Wrath.Prototype.PsychicAssets;

namespace CallOfTheWild.Wrath.Prototype
{
    // Personal rage only: Ferocious Beast consumes an additional round of rage.
    // Inspired Rage uses a different resource and already reaches pets in its area.
    // No rewriting of the native rage actions, resources or accepted rage powers.
    internal static class FerociousBeast01431
    {
        internal const string ModuleName = "Ferocious Beast rage powers";
        internal const string BaseToken = "FerociousBeastFeature";
        internal const string GreaterToken = "FerociousBeastGreaterFeature";
        internal const string BloodrageId = "5eac31e457999334b98f98b60fc73b2f";
        internal const string LimitlessId = "5cb58e6e406525342842a073fb70d068";
        internal static readonly string[] Hosts = { RagePowers.HostIds[0], RagePowers.HostIds[1], RagePowers.HostIds[2] };
        internal static readonly string[] Resources = { "24353fcf8096ea54684a72bf58dedbc9", "ea99fe417bf54b90835c7bd0b8128b95", "4aec9ec9d9cd5e24a95da90e56c72e37" };
        internal static readonly string[] PoolIds = { RagePowers.SelectionIds[0], RagePowers.SelectionIds[2], RagePowers.SelectionIds[3] };
        internal static string LastOptionalDiagnostic { get; private set; }
        const string TextBase = "While you are raging, your animal companion gains your rage's basic benefits, including greater rage, mighty rage and tireless rage. You spend 1 additional round of the active rage resource each round while a living animal companion benefits. This applies to personal rage, focused rage and primalist bloodrage; it does not extend inspired rage or spend raging song rounds.";
        const string TextGreater = "While you are raging, your animal companion also shares your rage powers that are constant in effect. Powers requiring an action, including stance activations, are not shared. Requires Ferocious Beast and 8 levels of barbarian or primalist. The basic Ferocious Beast cost is paid once per round.";

        internal static void Register(BlueprintsCache cache)
        {
            var f = new PsychicAssets(cache);
            var barb = f.Native<BlueprintCharacterClass>(RagePowers.BarbarianId);
            var blood = f.Native<BlueprintCharacterClass>(RagePowers.BloodragerId);
            var primal = f.Native<BlueprintArchetype>(RagePowers.PrimalistId);
            if (primal.GetParentClass() != blood) throw new InvalidOperationException("Ferocious Beast primalist parent differs from the native layout.");
            var pools = Pools(f);
            var root = f.Feature(BaseToken, "Ferocious Beast", TextBase, Requirements(barb, blood, primal, 1).Concat(new BlueprintComponent[] {
                new PrerequisitePet { Type = PetType.AnimalCompanion }, new FerociousBeastAvailability01431()
            }).ToArray());
            var greater = f.Feature(GreaterToken, "Ferocious Beast, Greater", TextGreater, Requirements(barb, blood, primal, 8).Concat(new BlueprintComponent[] {
                new PrerequisitePet { Type = PetType.AnimalCompanion }, Migration01426.Require(root), new FerociousBeastAvailability01431()
            }).ToArray());
            root.Groups = greater.Groups = new[] { FeatureGroup.RagePower };
            var icon = f.Native<BlueprintFeature>("25954b1652bebc2409f9cb9d5728bceb");
            Set(root, "m_Icon", Get(icon, "m_Icon")); Set(greater, "m_Icon", Get(icon, "m_Icon"));
            var prior = new List<BlueprintComponent[]>(); var next = new List<BlueprintComponent[]>();
            var hosts = Hosts.Select(f.Native<BlueprintBuff>).ToArray();
            for (int mode = 0; mode < hosts.Length; mode++) {
                var host = hosts[mode]; var donor = mode == 2 ? f.Native<BlueprintBuff>(BloodrageId) : host;
                var baseBuff = f.Clone(donor, "FerociousBeastBaseRage" + mode, "Ferocious Beast", TextBase);
                baseBuff.Stacking = StackingType.Stack;
                SetEnum(baseBuff, "m_Flags", "HiddenInUi");
                var temp = baseBuff.GetComponent<TemporaryHitPointsPerLevel>();
                if (temp == null || temp.HitPointsPerLevel != 1 || !baseBuff.GetComponents<ContextRankConfig>().Any())
                    throw new InvalidOperationException("Ferocious Beast native basic rage contract changed.");
                // Native temporary HP uses pet HD and the caster's rage tier. Keep
                // its formula and native modifier/damage API, retaining consumed
                // HP across suppression rather than refilling it on host restart.
                baseBuff.ComponentsArray = baseBuff.ComponentsArray.Where(c => !(c is AddFactContextActions) && !(c is TemporaryHitPointsPerLevel) &&
                    !(c is ContextCalculateSharedValue) && !(c is FerociousBeastHost01431) && c.GetType().Name != "BuffParticleEffectPlay").Concat(new BlueprintComponent[] {
                    new FerociousBeastTemporaryHP01431 { HitPointsPerLevel = temp.HitPointsPerLevel, Value = CopyData(temp.Value), Descriptor = temp.Descriptor }
                }).ToArray();
                var passive = PassiveEffects(f, host, pools, mode);
                var shared = f.Buff("FerociousBeastConstantPowers" + mode, "Ferocious Beast, Greater", TextGreater,
                    new AddFactContextActions { Activated = Actions(passive.ToArray()), Deactivated = Actions(), NewRound = Actions(), Dispose = Actions() });
                shared.Stacking = StackingType.Stack; SetEnum(shared, "m_Flags", "HiddenInUi");
                var fatigue = donor.GetComponents<AddFactContextActions>().SelectMany(c => c.Deactivated?.Actions ?? Array.Empty<GameAction>())
                    .OfType<Conditional>().Where(c => ContainsBuff(c, "e6f2fc5d73d88064583cb828801212f4")).Select(c => (GameAction)CasterConditions(CopyData(c))).ToArray();
                var component = new FerociousBeastHost01431 { name = "FerociousBeast01431_PersonalHost", Mode = mode,
                    SourceHost = Ref<BlueprintBuffReference>(host),
                    BaseRage = Ref<BlueprintBuffReference>(baseBuff), ConstantPowers = Ref<BlueprintBuffReference>(shared),
                    BasePower = Ref<BlueprintFeatureReference>(root), GreaterPower = Ref<BlueprintFeatureReference>(greater),
                    Resource = Ref<BlueprintAbilityResourceReference>(f.Native<BlueprintAbilityResource>(Resources[mode])),
                    Limitless = Ref<BlueprintUnitFactReference>(f.Native<BlueprintUnitFact>(LimitlessId)), Fatigue = Actions(fatigue) };
                var previous = host.ComponentsArray; prior.Add(previous);
                var found = previous.Where(c => c?.name == component.name).ToArray();
                if (found.Length > 1 || found.Length == 1 && Definition(found[0]) != Definition(component))
                    throw new InvalidOperationException("Ferocious Beast personal host observer collision; preserved.");
                next.Add(found.Length == 0 ? previous.Concat(new BlueprintComponent[] { component }).ToArray() : previous);
            }
            var existing = new HashSet<BlueprintGuid>(f.Assets.Values.Where(b => cache.Load(b.AssetGuid) != null).Select(b => b.AssetGuid));
            var poolBefore = pools.Select(p => new[] { Get(p, "m_Features"), Get(p, "m_AllFeatures") }).ToArray();
            try {
                Migration01426.Commit(f, pools, root, greater);
                for (int i = 0; i < hosts.Length; i++) hosts[i].ComponentsArray = next[i];
                foreach (var host in hosts) host.OnEnable();
            } catch {
                for (int i = 0; i < hosts.Length; i++) hosts[i].ComponentsArray = prior[i];
                for (int i = 0; i < pools.Length; i++) { Set(pools[i], "m_Features", poolBefore[i][0]); Set(pools[i], "m_AllFeatures", poolBefore[i][1]); }
                foreach (var bp in f.Assets.Values) if (!existing.Contains(bp.AssetGuid) && cache.Load(bp.AssetGuid) != null) cache.RemoveCachedBlueprint(bp.AssetGuid);
                throw;
            }
            Optional(cache);
        }
        internal static void Optional(BlueprintsCache cache)
        {
            var f = new PsychicAssets(cache); var roots = new[] { BaseToken, GreaterToken }.Select(t => cache.Load(Id(t)) as BlueprintFeature).ToArray();
            if (roots.Any(r => r == null)) return;
            var pools = Pools(f); var before = pools.Select(p => new[] { Get(p, "m_Features"), Get(p, "m_AllFeatures") }).ToArray();
            try { T09CombatFeats.Append(pools, roots); }
            catch { for (int i = 0; i < pools.Length; i++) { Set(pools[i], "m_Features", before[i][0]); Set(pools[i], "m_AllFeatures", before[i][1]); } throw; }
            // This provider is optional. Its failure must neither roll back the
            // native roots nor change the successfully registered module state.
            LastOptionalDiagnostic = null;
            object[] previous = null; BlueprintFeatureSelection selection = null;
            try {
                var provider = cache.Load(BlueprintGuid.Parse(TerrifyingHowl.TttPrimalistId));
                if (provider == null) return;
                selection = provider as BlueprintFeatureSelection;
                // TTT copies Group and Groups separately from native RagePowerSelection.
                // The game's current native selection has Group=RagePower, Groups=[].
                if (selection == null || selection.name != "PrimalistRagePowerSelection" || selection.IgnorePrerequisites ||
                    (selection.Group != FeatureGroup.RagePower && !selection.HasGroup(FeatureGroup.RagePower)))
                    throw new InvalidOperationException("Unexpected TabletopTweaks Primalist selection; preserved.");
                previous = new[] { Get(selection, "m_Features"), Get(selection, "m_AllFeatures") };
                T09CombatFeats.Append(new[] { selection }, roots);
            } catch (Exception error) {
                if (previous != null) { Set(selection, "m_Features", previous[0]); Set(selection, "m_AllFeatures", previous[1]); }
                LastOptionalDiagnostic = "Ferocious Beast optional TTT Primalist integration skipped; native roots remain registered. " + error;
                ModEntry.LogSpellDiagnostic(LastOptionalDiagnostic);
            }
        }
        // Called by this batch's Anger registration after its actual rage ranks
        // and resource have been authored. All assets join Anger's transaction.
        internal static void ConfigureDivine(PsychicAssets f, BlueprintBuff divine, BlueprintAbilityResource uses)
        {
            var basis = f.Clone(divine, "FerociousBeastDivineBaseRage", "Ferocious Beast", TextBase);
            var temp = basis.GetComponent<TemporaryHitPointsPerLevel>() ?? f.Native<BlueprintBuff>(RagePowers.HostIds[0]).GetComponent<TemporaryHitPointsPerLevel>();
            if (temp == null || !basis.GetComponents<ContextRankConfig>().Any()) throw new InvalidOperationException("Divine Anger rage tier missing.");
            basis.Stacking = StackingType.Stack; SetEnum(basis, "m_Flags", "HiddenInUi");
            basis.ComponentsArray = basis.ComponentsArray.Where(c => !(c is AddFactContextActions) && !(c is TemporaryHitPointsPerLevel) &&
                !(c is ContextCalculateSharedValue) && !(c is FerociousBeastHost01431) && c.GetType().Name != "BuffParticleEffectPlay" && c.GetType().Name != "TemporaryHitPointsFromAbilityValue").Concat(new BlueprintComponent[] {
                new FerociousBeastTemporaryHP01431 { HitPointsPerLevel = temp.HitPointsPerLevel, Value = CopyData(temp.Value), Descriptor = temp.Descriptor }
            }).ToArray();
            // Author this bridge independently of whether Ferocious registered.
            // Stable GUID references resolve if its roots arrive later; without
            // the base feature the runtime remains inert. Anger never depends
            // on another optional module's successful publication.
            T ById<T>(string token) where T : BlueprintReferenceBase, new() {
                var reference = new T(); Set(reference, "deserializedGuid", Id(token)); return reference;
            }
            var native = f.Native<BlueprintBuff>(RagePowers.HostIds[0]);
            var fatigue = native.GetComponents<AddFactContextActions>().SelectMany(c => c.Deactivated?.Actions ?? Array.Empty<GameAction>())
                .OfType<Conditional>().Where(c => ContainsBuff(c, "e6f2fc5d73d88064583cb828801212f4")).Select(c => (GameAction)CasterConditions(CopyData(c))).ToArray();
            var driver = new FerociousBeastHost01431 { name = "FerociousBeast01431_PersonalHost", Mode = 3,
                SourceHost = Ref<BlueprintBuffReference>(divine), Resource = Ref<BlueprintAbilityResourceReference>(uses), BaseRage = Ref<BlueprintBuffReference>(basis),
                ConstantPowers = ById<BlueprintBuffReference>("FerociousBeastConstantPowers0"), BasePower = ById<BlueprintFeatureReference>(BaseToken),
                GreaterPower = ById<BlueprintFeatureReference>(GreaterToken), Limitless = Ref<BlueprintUnitFactReference>(f.Native<BlueprintUnitFact>(LimitlessId)), Fatigue = Actions(fatigue) };
            divine.ComponentsArray = divine.ComponentsArray.Where(c => !(c is FerociousBeastHost01431)).Concat(new BlueprintComponent[] { driver }).ToArray();
        }
        static BlueprintFeatureSelection[] Pools(PsychicAssets f)
        {
            return PoolIds.Select(f.Native<BlueprintFeatureSelection>).ToArray();
        }
        static BlueprintComponent[] Requirements(BlueprintCharacterClass barb, BlueprintCharacterClass blood, BlueprintArchetype primal, int level)
        {
            var a = new PrerequisiteClassLevel { Level = level, Group = Prerequisite.GroupType.Any }; Set(a, "m_CharacterClass", Ref<BlueprintCharacterClassReference>(barb));
            var b = new PrerequisiteArchetypeLevel { Level = level, Group = Prerequisite.GroupType.Any }; Set(b, "m_CharacterClass", Ref<BlueprintCharacterClassReference>(blood)); Set(b, "m_Archetype", Ref<BlueprintArchetypeReference>(primal));
            return new BlueprintComponent[] { a, b };
        }
        // Migration01426.Definition gained a second overload in 0.14.67 TEST; name
        // the single-argument original so this collision check keeps resolving.
        static string Definition(object value) => (string)HarmonyLib.AccessTools.Method(typeof(Migration01426), "Definition", new[] { typeof(object) }).Invoke(null, new[] { value });
        static bool ContainsBuff(object value, string guid) => Definition(value).Contains("ref:" + guid);
        internal static bool IsConstantPower(BlueprintFeature root)
        {
            bool ActiveGrant(BlueprintUnitFact fact, HashSet<BlueprintGuid> path) {
                if (fact is Kingmaker.UnitLogic.Abilities.Blueprints.BlueprintAbility || fact is Kingmaker.UnitLogic.ActivatableAbilities.BlueprintActivatableAbility || fact is BlueprintFeatureSelection) return true;
                // A listed but unresolved grant cannot be proven passive.
                if (fact == null) return true;
                if (!path.Add(fact.AssetGuid)) return false;
                try { return fact.GetComponents<AddFacts>().Any(grant => grant.Facts.Any(child => ActiveGrant(child, path))); }
                finally { path.Remove(fact.AssetGuid); }
            }
            return root != null && !ActiveGrant(root, new HashSet<BlueprintGuid>());
        }
        // A constant power is gated by features, not active stance/toggle buffs.
        // Only buff applications are copied; no ability grants or resource actions.
        static List<GameAction> PassiveEffects(PsychicAssets f, BlueprintBuff host, BlueprintFeatureSelection[] pools, int mode)
        {
            var result = new List<GameAction>(); var clones = new Dictionary<BlueprintGuid, BlueprintBuff>();
            var featureClones = new Dictionary<BlueprintGuid, BlueprintFeature>();
            BlueprintUnitFact ConstantFact(BlueprintUnitFact source) {
                if (source is Kingmaker.UnitLogic.Abilities.Blueprints.BlueprintAbility || source is Kingmaker.UnitLogic.ActivatableAbilities.BlueprintActivatableAbility || source is BlueprintFeatureSelection) return null;
                if (source is BlueprintBuff b) return Effect(b);
                if (!(source is BlueprintFeature feature)) return null;
                if (featureClones.TryGetValue(source.AssetGuid, out var existing)) return existing;
                var clone = f.Clone(feature, "FerociousBeastPassiveFact" + mode + source.AssetGuid, "Ferocious Beast", TextGreater);
                featureClones.Add(source.AssetGuid, clone);
                clone.HideInUI = clone.HideInCharacterSheetAndLevelUp = true; clone.Groups = Array.Empty<FeatureGroup>();
                clone.ComponentsArray = clone.ComponentsArray.Where(c => !(c is Prerequisite) && !(c is AddAbilityResources)).ToArray();
                Sanitize(clone); return clone;
            }
            void Nested(GameAction action) {
                if (action is ContextActionApplyBuff apply && apply.Buff != null) {
                    Set(apply, "m_Buff", Ref<BlueprintBuffReference>(Effect(apply.Buff)));
                    // Native extra effects often apply to their caster because
                    // caster and receiver are originally the same rage owner.
                    // The shared effect keeps that owner for rank/feature gates,
                    // but its constant payload belongs to the receiving pet.
                    apply.ToCaster = false; apply.AsChild = true;
                }
                if (action is Conditional condition) { ConstantConditions(condition); foreach (var child in (condition.IfTrue?.Actions ?? Array.Empty<GameAction>()).Concat(condition.IfFalse?.Actions ?? Array.Empty<GameAction>())) Nested(child); }
                if (action is ContextActionConditionalSaved save) foreach (var child in (save.Succeed?.Actions ?? Array.Empty<GameAction>()).Concat(save.Failed?.Actions ?? Array.Empty<GameAction>())) Nested(child);
            }
            void Sanitize(BlueprintUnitFact clone) {
                foreach (var grants in clone.GetComponents<AddFacts>()) {
                    var constants = grants.Facts.Select(ConstantFact).Where(fact => fact != null).Select(Ref<BlueprintUnitFactReference>).ToArray();
                    Set(grants, "m_Facts", constants);
                }
                foreach (var actions in clone.GetComponents<AddFactContextActions>())
                    foreach (var action in (actions.Activated?.Actions ?? Array.Empty<GameAction>()).Concat(actions.Deactivated?.Actions ?? Array.Empty<GameAction>()).Concat(actions.NewRound?.Actions ?? Array.Empty<GameAction>()).Concat(actions.Dispose?.Actions ?? Array.Empty<GameAction>())) Nested(action);
            }
            BlueprintBuff Effect(BlueprintBuff source) {
                if (clones.TryGetValue(source.AssetGuid, out var old)) return old;
                var clone = f.Clone(source, "FerociousBeastPassive" + mode + source.AssetGuid, "Ferocious Beast", TextGreater);
                // Native localization remains a reference. Do not evaluate its
                // Unity-backed text during registration or freeze one locale.
                Set(clone, "m_DisplayName", Get(source, "m_DisplayName")); Set(clone, "m_Description", Get(source, "m_Description"));
                clone.Stacking = StackingType.Stack; SetEnum(clone, "m_Flags", "HiddenInUi");
                clones.Add(source.AssetGuid, clone); Sanitize(clone); return clone;
            }
            GameAction Rewrite(GameAction action, bool featureGated = false) {
                if (action is ContextActionApplyBuff a) {
                    if (a.Buff == null || !a.Permanent || a.ToCaster) return null;
                    var copy = CopyData(a); Set(copy, "m_Buff", Ref<BlueprintBuffReference>(Effect(a.Buff))); copy.AsChild = true; return copy;
                }
                if (!(action is Conditional c) || c.ConditionsChecker?.Conditions == null || c.ConditionsChecker.Conditions.Length == 0) return null;
                bool gated = featureGated;
                foreach (var condition in c.ConditionsChecker.Conditions) {
                    if (!(condition is ContextConditionHasFact) && !(condition is ContextConditionCasterHasFact)) return null;
                    if (!(((BlueprintUnitFactReference)Get(condition, "m_Fact"))?.Get() is BlueprintFeature feature)) return null;
                    if (!IsConstantPower(feature)) return null;
                    gated = true;
                }
                if (!gated) return null;
                var yes = (c.IfTrue?.Actions ?? Array.Empty<GameAction>()).Select(a => Rewrite(a, gated)).ToArray();
                var no = (c.IfFalse?.Actions ?? Array.Empty<GameAction>()).Select(a => Rewrite(a, gated)).ToArray();
                if (yes.Length == 0 || yes.Any(a => a == null) || no.Any(a => a == null)) return null;
                var copyCondition = ConstantConditions(CopyData(c)); copyCondition.IfTrue = Actions(yes); copyCondition.IfFalse = Actions(no); return copyCondition;
            }
            foreach (var actions in host.GetComponents<AddFactContextActions>())
                foreach (var action in actions.Activated?.Actions ?? Array.Empty<GameAction>()) { var copy = Rewrite(action); if (copy != null) result.Add(copy); }
            foreach (var root in pools.SelectMany(p => p.AllFeatures).Where(IsConstantPower).GroupBy(p => p.AssetGuid).Select(g => g.First()))
                foreach (var extra in root.GetComponents<BuffExtraEffects>()) {
                    var checkedList = (BlueprintBuffReference[])Get(extra, "m_CheckedBuffList") ?? Array.Empty<BlueprintBuffReference>();
                    bool Matches(BlueprintGuid guid) => extra.CheckedBuff?.AssetGuid == guid || checkedList.Any(r => r?.Guid == guid);
                    // Primalist's power observer is an auxiliary host; native
                    // bloodrage-only constant riders check the real bloodrage.
                    if (extra.ExtraEffectBuff == null || !Matches(host.AssetGuid) && !(mode == 2 && Matches(BlueprintGuid.Parse(BloodrageId)))) continue;
                    var conditions = new List<Condition>(); var has = new FerociousBeastCasterHasFact01431(); Set(has, "m_Fact", Ref<BlueprintUnitFactReference>(root)); conditions.Add(has);
                    if (extra.ExceptionFact != null) { Condition no = extra.ExceptionFact is BlueprintFeature ? (Condition)new FerociousBeastCasterHasFact01431 { Not = true } : new ContextConditionCasterHasFact { Not = true }; Set(no, "m_Fact", Ref<BlueprintUnitFactReference>(extra.ExceptionFact)); conditions.Add(no); }
                    var apply = Apply(Effect(extra.ExtraEffectBuff), 0, fromSpell: false); apply.Permanent = apply.AsChild = apply.IsNotDispelable = true;
                    result.Add(new Conditional { ConditionsChecker = new ConditionsChecker { Conditions = conditions.ToArray() }, IfTrue = Actions(apply), IfFalse = Actions() });
                }
            return result;
        }
        static Conditional ConstantConditions(Conditional c)
        {
            c.ConditionsChecker.Conditions = c.ConditionsChecker.Conditions.Select(condition => {
                if (!(condition is ContextConditionHasFact) && !(condition is ContextConditionCasterHasFact)) return condition;
                var fact = (BlueprintUnitFactReference)Get(condition, "m_Fact");
                if (!(fact?.Get() is BlueprintFeature)) return condition;
                var replacement = new FerociousBeastCasterHasFact01431 { Not = condition.Not, name = condition.name };
                Set(replacement, "m_Fact", fact); return (Condition)replacement;
            }).ToArray();
            foreach (var child in (c.IfTrue?.Actions ?? Array.Empty<GameAction>()).Concat(c.IfFalse?.Actions ?? Array.Empty<GameAction>()).OfType<Conditional>()) ConstantConditions(child);
            return c;
        }
        static Conditional CasterConditions(Conditional c)
        {
            c.ConditionsChecker.Conditions = c.ConditionsChecker.Conditions.Select(condition => {
                if (!(condition is ContextConditionHasFact)) return condition;
                var replacement = new ContextConditionCasterHasFact { Not = condition.Not, name = condition.name };
                Set(replacement, "m_Fact", Get(condition, "m_Fact")); return (Condition)replacement;
            }).ToArray();
            foreach (var child in (c.IfTrue?.Actions ?? Array.Empty<GameAction>()).Concat(c.IfFalse?.Actions ?? Array.Empty<GameAction>()).OfType<Conditional>()) CasterConditions(child);
            return c;
        }
        internal static bool Has(UnitEntityData owner, BlueprintFeature feature) => owner != null && feature != null && owner.Progression.Features.Enumerable.Any(f => f.Blueprint == feature && f.IsTurnedOn && !f.IsDisposed);
        internal static UnitEntityData Pet(UnitEntityData owner) => owner?.Pets.Select(p => p.Entity).FirstOrDefault(p => p != null && p.Get<UnitPartPet>()?.Type == PetType.AnimalCompanion && p.Get<UnitPartPet>().Master == owner && !p.State.IsDead && !p.IsDisposed);
    }
    [TypeId("86a53e701d0c46d1b965fb6f1fa74431")]
    public sealed class FerociousBeastCasterHasFact01431 : ContextCondition
    {
        [UnityEngine.SerializeField] BlueprintUnitFactReference m_Fact = null;
        public BlueprintUnitFact Fact => m_Fact?.Get();
        protected override string GetConditionCaption() => "";
        protected override bool CheckCondition() => Fact is BlueprintFeature feature && FerociousBeast01431.Has(Context.MaybeCaster, feature);
    }
    internal interface IFerociousBeastChange01431 : IUnitSubscriber { void HandleFerociousBeastChange(); }
    [TypeId("d46ec7974b72489c86c01653b9f74431")]
    public sealed class FerociousBeastAvailability01431 : UnitFactComponentDelegate
    {
        void Changed() => EventBus.RaiseEvent(Owner, (IFerociousBeastChange01431 h) => h.HandleFerociousBeastChange());
        protected override void OnTurnOn() => Changed();
        protected override void OnTurnOff() => Changed();
    }
    [TypeId("9bf2107bb50a49c49f9f0c953da74431")]
    public sealed class FerociousBeastHost01431 : UnitBuffComponentDelegate<FerociousBeastHost01431.State>, ITickEachRound, IPetHandler,
        IUnitHandler, IUnitLifeStateChanged, IUnitSpawnOnlyHandler, IUnitResurrectedHandler, IUnitGainFactHandler, IUnitLostFactHandler, IFerociousBeastChange01431
    {
        public int Mode;
        public BlueprintBuffReference SourceHost;
        public BlueprintBuffReference BaseRage, ConstantPowers;
        public BlueprintFeatureReference BasePower, GreaterPower;
        public BlueprintAbilityResourceReference Resource;
        public BlueprintUnitFactReference Limitless;
        public ActionList Fatigue;
        public sealed class State
        {
            [JsonProperty] public EntityRef<UnitEntityData> Pet;
            [JsonProperty] public EntityFactRef<Buff> Basic;
            [JsonProperty] public EntityFactRef<Buff> Greater;
            [JsonProperty] public bool HadBenefit;
            [JsonProperty] public int LastPaidRound = -1;
            [JsonProperty] public string PassiveFeatures;
            [JsonIgnore] public bool Changing;
        }
        bool FreeRage => Owner.HasFact(Limitless.Get());
        bool Active => Buff.Blueprint == SourceHost.Get() && Buff.IsTurnedOn && !Buff.IsSuppressed && !Buff.IsDisposed && !Owner.State.IsDead &&
            (FreeRage || Owner.Resources.GetResourceAmount(Resource.Get()) > 0) &&
            FerociousBeast01431.Has(Owner, BasePower.Get()) && !Owner.Buffs.Enumerable.Any(b => b.IsTurnedOn && !b.IsSuppressed &&
                b.GetComponent<FerociousBeastHost01431>() is FerociousBeastHost01431 other && b.Blueprint == other.SourceHost.Get() &&
                (Owner.HasFact(other.Limitless.Get()) || Owner.Resources.GetResourceAmount(other.Resource.Get()) > 0) &&
                (other.Mode < Mode || other.Mode == Mode && StringComparer.Ordinal.Compare(b.UniqueId, Buff.UniqueId) < 0));
        Buff Basic => Data.Basic.Fact;
        Buff Greater => Data.Greater.Fact;
        protected override void OnTurnOn() { Refresh(); Broadcast(); }
        protected override void OnTurnOff() { Suspend(); Broadcast(); }
        protected override void OnDeactivate() => Finish(true);
        protected override void OnDispose() => Finish(true);
        protected override void OnApplyPostLoadFixes() => Refresh();
        void Broadcast() => EventBus.RaiseEvent(Owner, (IFerociousBeastChange01431 h) => h.HandleFerociousBeastChange());
        void Suspend() { if (Basic?.IsActive == true) Basic.Deactivate(); Remove(ref Data.Greater); Data.PassiveFeatures = null; }
        void Remove(ref EntityFactRef<Buff> reference) { var effect = reference.Fact; reference = default; if (effect?.Manager != null) effect.Remove(); }
        void Finish(bool fatigue)
        {
            var pet = Data.Pet.Entity;
            if (fatigue && Data.HadBenefit && pet != null && !pet.State.IsDead && Basic?.Manager != null && (Fatigue?.Actions?.Length ?? 0) > 0)
                Basic.RunActionInContext(Fatigue);
            Remove(ref Data.Greater); Remove(ref Data.Basic); Data.Pet = default; Data.HadBenefit = false;
        }
        Buff Add(UnitEntityData pet, BlueprintBuff blueprint)
        {
            var created = pet.AddBuff(blueprint, Buff.Context); if (created == null) return null;
            created.IsNotDispelable = true; created.IsFromSpell = false; return created;
        }
        void Refresh()
        {
            if (Data.Changing) return; Data.Changing = true;
            try {
                var pet = Active ? FerociousBeast01431.Pet(Owner) : null;
                if (pet == null) { Suspend(); return; }
                if (Data.Pet.Entity != pet) { Finish(false); Data.Pet = pet; }
                if (Basic == null || Basic.IsDisposed || Basic.Manager == null) Data.Basic = Add(pet, BaseRage.Get());
                else if (!Basic.IsActive) Basic.Activate();
                Data.HadBenefit |= Basic?.IsTurnedOn == true;
                if (FerociousBeast01431.Has(Owner, GreaterPower.Get())) {
                    var signature = string.Join(";", Owner.Progression.Features.Enumerable.Where(p => p.IsTurnedOn && !p.IsDisposed).Select(p => p.Blueprint.AssetGuid + ":" + p.GetRank()).OrderBy(s => s));
                    if (Data.PassiveFeatures != signature) { Remove(ref Data.Greater); Data.PassiveFeatures = signature; }
                    if (Greater == null || Greater.IsDisposed || Greater.Manager == null) Data.Greater = Add(pet, ConstantPowers.Get());
                    else if (!Greater.IsActive) Greater.Activate();
                } else Remove(ref Data.Greater);
            } finally { Data.Changing = false; }
        }
        public void OnNewRound()
        {
            Refresh();
            if (!Active || Basic?.IsTurnedOn != true || Data.LastPaidRound == Buff.RoundNumber) return;
            Data.LastPaidRound = Buff.RoundNumber;
            if (FreeRage) return;
            if (Owner.Resources.GetResourceAmount(Resource.Get()) < 1) { Finish(false); return; }
            Owner.Resources.Spend(Resource.Get(), 1);
        }
        public void HandleFerociousBeastChange() => Refresh();
        public void HandleUnitGainFact(EntityFact fact) { if (fact.Blueprint is BlueprintFeature) Refresh(); }
        public void HandleUnitLostFact(EntityFact fact) { if (fact.Blueprint == BasePower.Get()) Finish(false); else if (fact.Blueprint is BlueprintFeature) Refresh(); }
        public void HandleAddPet(UnitEntityData master, UnitEntityData pet) { if (master == Owner) Refresh(); }
        public void HandleRemovePet(UnitEntityData master, UnitEntityData pet) { if (master == Owner && Data.Pet.Entity == pet) Finish(false); }
        public void HandleUnitDeath(UnitEntityData unit) { if (unit == Owner || Data.Pet.Entity == unit) Finish(false); }
        // UpdateLifeState itself raises this native global death notification
        // after setting IsDead, before the later life-state/death controllers.
        public void HandleUnitDeathForSpawnOnly(UnitEntityData unit) { if (unit == Owner || Data.Pet.Entity == unit) Finish(false); }
        public void HandleUnitLifeStateChanged(UnitEntityData unit, UnitLifeState previous) { if (unit.State.IsDead && (unit == Owner || Data.Pet.Entity == unit)) Finish(false); }
        public void HandleUnitDestroyed(UnitEntityData unit) { if (unit == Owner || Data.Pet.Entity == unit) Finish(false); }
        public void HandleUnitSpawned(UnitEntityData unit) { if (unit == Owner || unit.Get<UnitPartPet>()?.Master == Owner) Refresh(); }
        public void HandleUnitResurrected(UnitEntityData unit) { if (unit == Owner || unit.Get<UnitPartPet>()?.Master == Owner) Refresh(); }
    }
    [TypeId("246899ee423a46f28d0d481746f74431")]
    public sealed class FerociousBeastTemporaryHP01431 : UnitFactComponentDelegate<FerociousBeastTemporaryHP01431.State>
    {
        public int HitPointsPerLevel;
        public ContextValue Value;
        public ModifierDescriptor Descriptor;
        public sealed class State
        {
            [JsonProperty] public bool Initialized;
            [JsonProperty] public int Remaining;
            [JsonProperty] public ModifiableValue.Modifier Modifier;
        }
        protected override void OnTurnOn()
        {
            if (!Data.Initialized) { Data.Remaining = Value.Calculate(Context) * HitPointsPerLevel * Owner.Progression.CharacterLevel; Data.Initialized = true; }
            if (Data.Modifier?.AppliedTo != null) return;
            if (Data.Remaining > 0) Data.Modifier = Owner.Stats.TemporaryHitPoints.AddModifier(Data.Remaining, Runtime, Descriptor);
        }
        protected override void OnTurnOff()
        {
            if (Data.Modifier != null) { Data.Remaining = Data.Modifier.AppliedTo != null ? Math.Max(0, Data.Modifier.ModValue) : 0; Data.Modifier.Remove(); Data.Modifier = null; }
        }
    }
}

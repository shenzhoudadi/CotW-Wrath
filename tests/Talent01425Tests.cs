using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using HarmonyLib;
using Newtonsoft.Json.Linq;
using Kingmaker;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Prerequisites;
using Kingmaker.Blueprints.Classes.Selection;
using Kingmaker.Blueprints.Items.Weapons;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.Designers.Mechanics.Facts;
using Kingmaker.EntitySystem;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.Enums;
using Kingmaker.Enums.Damage;
using Kingmaker.Items;
using Kingmaker.RuleSystem;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Class.LevelUp;
using CallOfTheWild.Wrath.Prototype;

internal static class Talent01425Tests
{
    static readonly Type Module = typeof(LinnormStyleEffect).Assembly.GetType("CallOfTheWild.Wrath.Prototype.ReapingStalker", true);
    const string Token = "ReapingStalkerSlayerTalent";
    static int checks, ruleCases;
    static bool failPublication, failAppend;
    static UnitEntityData weaponOwner;
    static BlueprintGuid Id => (BlueprintGuid)AccessTools.Method(Module.Assembly.GetType("CallOfTheWild.Wrath.Prototype.PsychicAssets"), "Id").Invoke(null, new object[] { Token });
    static T Empty<T>() where T : class => (T)FormatterServices.GetUninitializedObject(typeof(T));
    static void Set(object o, string f, object v) => (AccessTools.Field(o.GetType(), f) ?? AccessTools.Field(o.GetType(), "<" + f + ">k__BackingField")).SetValue(o, v);
    static object Get(object o, string f) => AccessTools.Field(o.GetType(), f).GetValue(o);
    static void Check(bool ok, string message) { if (!ok) throw new Exception("Talent01425: " + message); checks++; }
    static string Constant(string field) => (string)AccessTools.Field(Module, field).GetRawConstantValue();
    static void Register(BlueprintsCache cache) => AccessTools.Method(Module, "Register").Invoke(null, new object[] { cache });
    static void Optional(BlueprintsCache cache) => AccessTools.Method(Module, "Optional").Invoke(null, new object[] { cache });
    static bool Rejected(Action action) { try { action(); return false; } catch (Exception e) { while (e is TargetInvocationException && e.InnerException != null) e = e.InnerException; return e is InvalidOperationException; } }
    static BlueprintFeatureReference[] Refs(BlueprintFeatureSelection p, string f) => (BlueprintFeatureReference[])Get(p, f) ?? Array.Empty<BlueprintFeatureReference>();
    static T Ref<T>(SimpleBlueprint b) where T : BlueprintReferenceBase, new() { var r = BlueprintReferenceBase.CreateTyped<T>(b); AccessTools.Property(typeof(BlueprintReferenceBase), "Cached").SetValue(r, b, null); return r; }
    static bool No(ref bool __result) { __result = false; return false; }
    static bool Skip() => false;
    static bool Ignore(BlueprintFeatureSelection __instance, ref bool __result) { __result = __instance.IgnorePrerequisites; return false; }
    static bool NullController(ref LevelUpController __result) { __result = null; return false; }
    static bool Wielder(ref UnitDescriptor __result) { __result = weaponOwner?.Descriptor; return false; }
    static void PublishFault(BlueprintGuid guid) { if (failPublication && guid == Id) throw new InvalidOperationException("01425 publication fault"); }
    static void AppendFault(BlueprintFeature[] feats) { if (failAppend && feats.Any(f => f.AssetGuid == Id)) throw new InvalidOperationException("01425 append fault"); }
    static Feature Fact(UnitEntityData u, BlueprintFeature bp) => (Feature)AccessTools.Method(typeof(Linnorm01416Tests), "Fact").Invoke(null, new object[] { u, bp });
    static UnitEntityData Unit(BlueprintsCache cache, string name) => (UnitEntityData)AccessTools.Method(typeof(SpellAndSword01417Tests), "Unit").Invoke(null, new object[] { cache, name });
    static ItemEntityWeapon Weapon(UnitEntityData u, BlueprintWeaponType type, Size size) {
        var w = (ItemEntityWeapon)AccessTools.Method(typeof(WildShapeFeatTests), "Weapon").Invoke(null, new object[] { u, type.Category, false, PhysicalDamageForm.Slashing, "01425-" + type.Category + size });
        weaponOwner = u; Set(u.State, "m_Size", size);
        Set(w.Blueprint, "m_Type", Ref<BlueprintWeaponTypeReference>(type)); Set(w.Blueprint, "m_Size", Size.Medium); w.Blueprint.OnEnable(); return w;
    }
    static RuleCalculateWeaponStats Stats(UnitEntityData u, ItemEntityWeapon w, Feature fact, bool keenFirst, bool keenLast) {
        var evt = new RuleCalculateWeaponStats(u, w); evt.DoubleCriticalEdge = keenFirst;
        if (fact != null) foreach (var runtime in fact.Components) {
            using (runtime.RequestEventContext()) {
                if (runtime.SourceBlueprintComponent is WeaponSizeChange size) size.OnEventAboutToTrigger(evt);
                if (runtime.SourceBlueprintComponent is WeaponTypeCriticalEdgeIncrease critical) critical.OnEventAboutToTrigger(evt);
            }
        }
        if (keenLast) evt.DoubleCriticalEdge = true;
        var sizeRule = new RuleCalculateWeaponSizeBonus(u, w, evt.WeaponDamageDice, (int)Get(evt, "m_SizeShift"), false);
        sizeRule.OnTrigger(null); return evt;
    }
    internal static int Run(BlueprintsCache cache) {
        checks = ruleCases = 0; var h = new Harmony("CotW01425.NativeTalentContracts");
        h.Patch(AccessTools.Method(typeof(FeatureCollection), "CheckStatPrerequisitesAndUpdate"), prefix: new HarmonyMethod(typeof(Talent01425Tests), nameof(Skip)));
        h.Patch(AccessTools.PropertyGetter(typeof(IgnorePrerequisites), "Ignore"), prefix: new HarmonyMethod(typeof(Talent01425Tests), nameof(No)));
        h.Patch(AccessTools.Method(typeof(BlueprintFeatureSelection), "IsIgnorePrerequisites"), prefix: new HarmonyMethod(typeof(Talent01425Tests), nameof(Ignore)));
        h.Patch(AccessTools.PropertyGetter(typeof(Game), "LevelUpController"), prefix: new HarmonyMethod(typeof(Talent01425Tests), nameof(NullController)));
        h.Patch(AccessTools.PropertyGetter(typeof(ItemEntity), "Wielder"), prefix: new HarmonyMethod(typeof(Talent01425Tests), nameof(Wielder)));
        h.Patch(AccessTools.Method(typeof(BlueprintsCache), "AddCachedBlueprint"), postfix: new HarmonyMethod(typeof(Talent01425Tests), nameof(PublishFault)));
        h.Patch(AccessTools.Method(Module.Assembly.GetType("CallOfTheWild.Wrath.Prototype.T09CombatFeats"), "Append"), postfix: new HarmonyMethod(typeof(Talent01425Tests), nameof(AppendFault)));
        Provider01419Tests.PatchLabels(h);
        var pools = ((string[])AccessTools.Field(Module, "PoolIds").GetValue(null)).Select(id => (BlueprintFeatureSelection)cache.Load(BlueprintGuid.Parse(id))).ToArray();
        var before = pools.Select(p => new[] { Get(p, "m_Features"), Get(p, "m_AllFeatures") }).ToArray();
        var advanced = (BlueprintFeature)cache.Load(BlueprintGuid.Parse(Constant("AdvancedId")));
        var sickle = (BlueprintWeaponType)cache.Load(BlueprintGuid.Parse(Constant("SickleId")));
        var scythe = (BlueprintWeaponType)cache.Load(BlueprintGuid.Parse(Constant("ScytheId")));
        var tttId = BlueprintGuid.Parse(Constant("TttExtraId"));
        void SameArrays() { for (int i = 0; i < pools.Length; i++) for (int j = 0; j < 2; j++) Check(ReferenceEquals(Get(pools[i], j == 0 ? "m_Features" : "m_AllFeatures"), before[i][j]), "failed registration preserves exact old array " + i + "/" + j); }
        try {
            Check(cache.Load(Id) == null, "new talent absent before production registration");
            Check(advanced.name == "AdvanceTalents" && pools.All(p => p != null), "current native dependency identities loaded");
            Set(scythe, "m_Icon", Empty<UnityEngine.Sprite>());
            cache.RemoveCachedBlueprint(advanced.AssetGuid); Check(Rejected(() => Register(cache)) && cache.Load(Id) == null, "missing prerequisite fails before publication"); SameArrays(); cache.AddCachedBlueprint(advanced.AssetGuid, advanced);
            var collision = new BlueprintFeature { name = "01425ForeignIdentity", AssetGuid = Id }; cache.AddCachedBlueprint(Id, collision);
            Check(Rejected(() => Register(cache)) && ReferenceEquals(cache.Load(Id), collision), "foreign identity is never overwritten"); SameArrays(); cache.RemoveCachedBlueprint(Id);
            failPublication = true; Check(Rejected(() => Register(cache)), "failure after cache insertion is propagated"); failPublication = false; Check(cache.Load(Id) == null, "failed cache publication removed own root"); SameArrays();
            failAppend = true; Check(Rejected(() => Register(cache)), "failure after all native pools appended is propagated"); failAppend = false; Check(cache.Load(Id) == null, "append failure removes own new root"); SameArrays();
            Register(cache); var talent = (BlueprintFeature)cache.Load(Id);
            Check(talent.name == "CotWWrathPsychic" + Token && talent.Ranks == 1 && talent.Groups.SequenceEqual(new[] { FeatureGroup.SlayerTalent }), "one stable rank-one class talent, not ordinary feat");
            Check(ReferenceEquals(talent.Icon, scythe.Icon) && !ReferenceEquals(talent.Icon, null), "original CotW scythe icon source retained; managed icon identity, not Unity native-pointer liveness");
            Check(talent.ComponentsArray.Length == 5 && talent.ComponentsArray.All(c => c.GetType().Assembly == typeof(BlueprintFeature).Assembly), "all five prerequisites/effect components are native");
            Check(talent.ComponentsArray.Select(c => c.name).Distinct().Count() == 5, "component names are unique for native serialization");
            Check(talent.GetComponent<PrerequisiteFeature>().Feature == advanced, "requires actual Advanced Talents ability");
            foreach (var type in new[] { sickle, scythe }) Check(talent.GetComponents<WeaponTypeCriticalEdgeIncrease>().Count(c => c.WeaponType == type) == 1 && talent.GetComponents<WeaponSizeChange>().Count(c => c.CheckWeaponCategory && c.Category == type.Category && c.SizeCategoryChange == 1) == 1, "one size/critical component per eligible weapon");
            Register(cache); Check(ReferenceEquals(cache.Load(Id), talent), "repeat registration keeps exact saved blueprint identity");
            for (int i = 0; i < pools.Length; i++) foreach (string field in new[] { "m_Features", "m_AllFeatures" }) {
                var old = (BlueprintFeatureReference[])before[i][field == "m_Features" ? 0 : 1] ?? Array.Empty<BlueprintFeatureReference>(); var now = Refs(pools[i], field);
                Check(now.Take(old.Length).SequenceEqual(old), "preserves original reference objects and order " + i + field); Check(now.Count(r => r?.Guid == Id) == 1, "one new pool entry despite repetition " + i + field);
            }
            var basic = (BlueprintFeatureSelection)cache.Load(BlueprintGuid.Parse("247a4068296e8be42890143f451b4b45")); Check(!Refs(basic, "m_AllFeatures").Any(r => r?.Guid == Id), "never an ordinary feat-list entry");
            var u = Unit(cache, "01425-qualified"); var state = Empty<LevelUpState>(); Set(state, "m_Unit", u); Set(state, "Selections", new List<FeatureSelectionState>()); var pending = Empty<FeatureSelectionState>(); Set(pending, "Selection", pools[2]);
            foreach (int level in new[] { 1, 9, 10, 20 }) { Set(u.Progression, "m_CharacterLevel", (int?)level); Set(state, "NextCharacterLevel", level); Check(!pools[2].CanSelect(u.Descriptor, state, pending, talent), "level alone does not bypass Advanced Talents " + level); }
            Fact(u, advanced); Check(pools[2].CanSelect(u.Descriptor, state, pending, talent), "native CanSelect allows holder of Advanced Talents");
            var fact = Fact(u, talent); Check(!pools[2].CanSelect(u.Descriptor, state, pending, talent), "native CanSelect rejects second copy");
            var other = new BlueprintWeaponType { Category = WeaponCategory.Longsword }; Set(other, "m_BaseDamage", new DiceFormula(1, DiceType.D8)); Set(other, "m_CriticalRollEdge", 19); Set(other, "m_AttackType", AttackType.Melee); other.OnEnable();
            foreach (var type in new[] { sickle, scythe, other }) foreach (var size in new[] { Size.Small, Size.Medium, Size.Large }) foreach (int keen in new[] { 0, 1, 2 }) {
                var weapon = Weapon(u, type, size); var baseline = Stats(u, weapon, null, keen == 1, keen == 2); var result = Stats(u, weapon, fact, keen == 1, keen == 2); bool eligible = type != other;
                Check((int)Get(result, "m_SizeShift") == (eligible ? 1 : 0), "native category size shift " + type.Category + size + keen);
                Check(result.CriticalEdge == (eligible ? 19 : baseline.CriticalEdge), "native critical edge and double-edge nonstack " + type.Category + size + keen);
                Check(result.CriticalMultiplier == baseline.CriticalMultiplier, "talent never changes critical multiplier");
                Check(result.WeaponDamageDice.ModifiedValue == WeaponDamageScaleTable.Scale(type.BaseDamage, size.Shift(eligible ? 1 : 0), Size.Medium, weapon.Blueprint), "actual size-rule damage dice " + type.Category + size + keen + "; actual=" + result.WeaponDamageDice.ModifiedValue + "; type=" + type.BaseDamage + "; item=" + weapon.Blueprint.BaseDamage + "; weaponSize=" + weapon.Size + "; ruleSize=" + result.WeaponSize + "; patches=" + string.Join(",", Harmony.GetPatchInfo(AccessTools.Method(typeof(RuleCalculateWeaponSizeBonus), "OnTrigger"))?.Owners.ToArray() ?? Array.Empty<string>()));
                Check(weapon.Size == size && weapon.Blueprint.Type == type, "calculation leaves weapon unchanged");
                if (size == Size.Medium && eligible) Check(result.WeaponDamageDice.ModifiedValue == (type == sickle ? new DiceFormula(1, DiceType.D8) : new DiceFormula(2, DiceType.D6)), "medium reference damage: sickle d8/scythe 2d6");
                Check(Stats(u, weapon, null, false, false).WeaponDamageDice.ModifiedValue == WeaponDamageScaleTable.Scale(type.BaseDamage, size, Size.Medium, weapon.Blueprint), "no state leakage to a later calculation without talent"); ruleCases++;
            }
            Optional(cache); Check(ReferenceEquals(cache.Load(Id), talent), "missing optional TTT preserves native registration");
            // Current TTT Core copies Mode/IgnorePrerequisites/arrays, but does
            // not copy Group. Its ordinary feat Groups is the relevant marker.
            var ttt = new BlueprintFeatureSelection { name = "ExtraSlayerTalent", AssetGuid = tttId, Groups = new[] { FeatureGroup.Feat }, ComponentsArray = Array.Empty<BlueprintComponent>() }; Set(ttt, "m_Features", Refs(pools[2], "m_Features").Where(r => r.Guid != Id).ToArray()); Set(ttt, "m_AllFeatures", Refs(pools[2], "m_AllFeatures").Where(r => r.Guid != Id).ToArray()); cache.AddCachedBlueprint(tttId, ttt);
            var oldTtt = new[] { Get(ttt, "m_Features"), Get(ttt, "m_AllFeatures") }; failAppend = true; Check(Rejected(() => Optional(cache)), "late provider append failure isolated"); failAppend = false;
            Check(ReferenceEquals(Get(ttt, "m_Features"), oldTtt[0]) && ReferenceEquals(Get(ttt, "m_AllFeatures"), oldTtt[1]), "provider rollback exact original arrays");
            Optional(cache); Optional(cache); Check(Refs(ttt, "m_AllFeatures").Count(r => r.Guid == Id) == 1, "late TTT selection gets one root");
            ttt.IgnorePrerequisites = true; Check(Rejected(() => Optional(cache)) && ReferenceEquals(cache.Load(Id), talent), "unsafe provider shape cannot invalidate native talent"); ttt.IgnorePrerequisites = false;
            bool reported = false; var dispatcher = AccessTools.Method(Module.Assembly.GetType("CallOfTheWild.Wrath.Prototype.BlueprintRegistration"), "RegisterModule");
            dispatcher.Invoke(null, new object[] { "Reaping Stalker", new Action(() => { throw new InvalidOperationException("01425 isolated module error"); }), new Action<string, bool, Exception>((name, ok, error) => reported = name == "Reaping Stalker" && !ok && error is InvalidOperationException) }); Check(reported, "actual shared module dispatcher contains module failure");
            var contracts = new JObject { ["passed"] = true, ["assertions"] = checks, ["talentId"] = Id.ToString(), ["newTalentCount"] = 1, ["newFeatCount"] = 0, ["nativePools"] = pools.Length, ["nativeRuleCases"] = ruleCases, ["nativeOnlyComponents"] = true, ["nativeSelectionPredicates"] = true, ["lateTTTSelection"] = true, ["boundary"] = "Current game native dependencies from blueprints.zip, actual production registration and native CanSelect, native weapon components under actual Feature component contexts and original RuleCalculateWeaponSizeBonus.OnTrigger. Controlled unit/item ownership, scene prerequisite switches and icon objects; keen ordering cases use the native shared DoubleCriticalEdge flag. No Unity rendering, full mod DLL startup or save load." };
            PsychicExpansionTests.Inventory["talent01425Contracts"] = contracts; PsychicExpansionTests.Inventory["talent01425Assertions"] = checks; Console.WriteLine("PASS: " + checks + " 01425 talent contracts; " + ruleCases + " native weapon rule cases."); return checks;
        }
        finally { failAppend = failPublication = false; weaponOwner = null; cache.RemoveCachedBlueprint(tttId); h.UnpatchAll(h.Id); }
    }
}

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using HarmonyLib;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Selection;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.Blueprints.Facts;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.FactLogic;
using Kingmaker.UnitLogic.Mechanics.Components;
using CallOfTheWild.Wrath.Prototype;

// Native cache/OnEnable, the game's actual blueprint JSON serializer and the
// published TTT DLL execute. No game process or provider initialization runs.
internal static class RegistrationWarmup01443Tests
{
    static readonly Assembly Production = typeof(HowlShakenTarget).Assembly;
    static readonly string[] Modules = { "Spell: Time Stop", "Bardic masterpiece: Clamor of Heavens", "Bardic masterpiece: Dance of 23 Steps", "Bardic masterpiece: Symphony of the Elysian Heart", "Ki powers: five spell adaptations" };
    static JArray NativeLists;
    static int checks, jsonRoundTrips, nativeScans, nativeEnables, optionalDispatches, rejectedEdits, tttMethods;
    static readonly List<JObject> observations = new List<JObject>();
    static Type Module(string name) => Production.GetType("CallOfTheWild.Wrath.Prototype." + name, true);
    static object Call(string type, string method, params object[] args) => AccessTools.Method(Module(type), method).Invoke(null, args);
    static object Old(Type type, string method, params object[] args) => AccessTools.Method(type, method).Invoke(null, args);
    static object Get(object value, string field) => AccessTools.Field(value.GetType(), field).GetValue(value);
    static void Set(object value, string field, object data) => AccessTools.Field(value.GetType(), field).SetValue(value, data);
    static BlueprintGuid Id(string token) => (BlueprintGuid)Call("PsychicAssets", "Id", token);
    static T Asset<T>(BlueprintsCache cache, string token) where T : SimpleBlueprint => (T)cache.Load(Id(token));
    static string Definition(object value) => (string)Call("Migration01426", "Definition", value);
    static void Check(bool result, string reason) { if (!result) throw new Exception("RegistrationWarmup01443: " + reason); checks++; }
    static SimpleBlueprint Clone(SimpleBlueprint value) => (SimpleBlueprint)AccessTools.Method(typeof(object), "MemberwiseClone").Invoke(value, null);
    static Exception Failure(Action action) { try { action(); return null; } catch (Exception e) { while (e is TargetInvocationException) e = e.InnerException; return e; } }
    static bool Scope(string name) => Modules.Contains(name);
    static bool RawName(BlueprintUnitFact __instance, ref string __result) { __result = __instance.name; return false; }
    // Identity transpilers copy actual, unchanged provider IL into a Harmony
    // skip-visibility dynamic body. This matches its publicized compile access
    // on CLR without modifying the installed game assembly or provider file.
    static IEnumerable<CodeInstruction> SameProviderIL(IEnumerable<CodeInstruction> code) => code;
    static IEnumerable<SimpleBlueprint> Loaded(BlueprintsCache cache) => ((IDictionary)Get(cache, "m_LoadedBlueprints")).Values.Cast<object>().Select(e => (SimpleBlueprint)Get(e, "Blueprint")).Where(b => b != null).ToArray();

    static void Dispatch(BlueprintsCache cache)
    {
        var reports = new Dictionary<string, Exception>();
        Action<string, bool, Exception> report = (name, passed, error) => { if (Modules.Contains(name)) reports[name] = passed ? null : error ?? new Exception("Missing module error"); };
        AccessTools.Method(Module("BlueprintRegistration"), "RegisterOptional").Invoke(null, new object[] { cache, report }); optionalDispatches++;
        foreach (string name in Modules) Check(reports.ContainsKey(name) && reports[name] == null, "actual optional dispatcher succeeds after warmup: " + name + (reports.TryGetValue(name, out var e) && e != null ? ": " + e : ""));
    }
    static void Retry(BlueprintsCache cache, string module, string method = "Register") => Call(module, method, cache);
    static void Reject(BlueprintsCache cache, string module, Action change, Action restore, string reason, string method = "Register", string field = null)
    {
        change();
        try {
            var error = Failure(() => Retry(cache, module, method));
            Check(error is InvalidOperationException, "authored/identity edit rejected: " + reason + (error == null ? "" : ": " + error));
            if (field != null) Check(error.Message.Contains(field), "diagnostic names the exact changed field: " + field);
            rejectedEdits++;
        } finally { restore(); }
        Retry(cache, module, method);
    }
    static BlueprintComponent NativeRoundTrip(BlueprintScriptableObject blueprint, BlueprintComponent component)
    {
        string before = Definition(component); var previous = Kingmaker.Blueprints.JsonSystem.Json.BlueprintBeingRead;
        try {
            Kingmaker.Blueprints.JsonSystem.Json.BlueprintBeingRead = new BlueprintJsonWrapper(blueprint);
            var text = new StringWriter(); var writer = new JsonTextWriter(text);
            Kingmaker.Blueprints.JsonSystem.Json.Serializer.Serialize(writer, component); writer.Flush();
            var result = (BlueprintComponent)Kingmaker.Blueprints.JsonSystem.Json.Serializer.Deserialize(new JsonTextReader(new StringReader(text.ToString())), component.GetType());
            result.OwnerBlueprint = blueprint; jsonRoundTrips++;
            Check(Definition(result) == before, "actual native JSON null-to-empty reference roundtrip preserves authored definition: " + blueprint.name + "/" + component.GetType().Name);
            blueprint.ComponentsArray = blueprint.ComponentsArray.Select(c => ReferenceEquals(c, component) ? result : c).ToArray();
            return result;
        } finally { Kingmaker.Blueprints.JsonSystem.Json.BlueprintBeingRead = previous; }
    }
    static void JsonAndPrivateNegatives(BlueprintsCache cache)
    {
        string[] tokens = { "KiBurstOfAdrenalineMonkAbilityOption0", "ClamorOfHeavensMasterpieceEffect", "DanceOf23StepsMasterpieceEffect", "SymphonyOfElysianHeartMasterpieceEffect" };
        for (int i = 0; i < tokens.Length; i++) {
            var bp = Asset<BlueprintScriptableObject>(cache, tokens[i]); BlueprintComponent component;
            if (i < 2) component = bp.GetComponent<ContextCalculateAbilityParams>();
            else if (i == 2) component = bp.GetComponent<ContextRankConfig>();
            else component = bp.GetComponent<BuffDescriptorImmunity>();
            Check(component != null, "exact native component exists for reported graph: " + tokens[i]);
            string field = i < 2 ? "m_CustomProperty" : i == 2 ? "Archetype" : "m_FactToCheck";
            Check(Get(component, field) == null, "source native reference field is genuinely null before serialization: " + tokens[i] + "/" + field);
            var copied = NativeRoundTrip(bp, component);
            Check(Get(copied, field) is BlueprintReferenceBase reference && reference.Guid == default(BlueprintGuid), "native JSON supplies empty typed reference, not another asset: " + tokens[i]);
        }
        // Toggle costs and the third masterpiece immunity have distinct native
        // reference fields, so they are covered separately from caster params.
        foreach (string token in new[] { "ClamorOfHeavensMasterpieceBardToggle", "DanceOf23StepsMasterpieceBardToggle", "SymphonyOfElysianHeartMasterpieceBardToggle" }) {
            var bp = Asset<BlueprintScriptableObject>(cache, token);
            var costs = bp.ComponentsArray.Where(c => c.GetType().Name == "ActivatableAbilityResourceLogic").ToArray();
            Check(costs.Length > 0, "real masterpiece toggle resource components exist: " + token);
            foreach (var costComponent in costs) NativeRoundTrip(bp, costComponent);
        }
        Dispatch(cache);
        var ki = Asset<BlueprintAbility>(cache, tokens[0]); var parameters = ki.GetComponent<ContextCalculateAbilityParams>();
        var empty = Get(parameters, "m_CustomProperty"); var actualProperty = Get(parameters.CasterLevel, "m_CustomProperty");
        Reject(cache, "MonkKiPowers01427", () => Set(parameters, "m_CustomProperty", actualProperty), () => Set(parameters, "m_CustomProperty", empty), "non-empty unused reference is still an authored change", field: "m_CustomProperty");
        var otherProperty = Get(parameters.SpellLevel, "m_CustomProperty");
        Check(((BlueprintReferenceBase)actualProperty).Guid != ((BlueprintReferenceBase)otherProperty).Guid, "caster-level and spell-level sources are distinct real non-empty properties");
        Reject(cache, "MonkKiPowers01427", () => Set(parameters.CasterLevel, "m_CustomProperty", otherProperty), () => Set(parameters.CasterLevel, "m_CustomProperty", actualProperty), "active caster-level non-empty GUID substitution", field: "m_CustomProperty");
        var cost = ki.GetComponent<AbilityResourceLogic>(); int amount = cost.Amount;
        Reject(cache, "MonkKiPowers01427", () => cost.Amount++, () => cost.Amount = amount, "real ki cost", field: "Amount");
        var components = ki.ComponentsArray;
        Reject(cache, "MonkKiPowers01427", () => ki.ComponentsArray = components.Reverse().ToArray(), () => ki.ComponentsArray = components, "component order");
        Reject(cache, "MonkKiPowers01427", () => ki.ComponentsArray = components.Concat(new[] { cost }).ToArray(), () => ki.ComponentsArray = components, "duplicate component");
        Reject(cache, "MonkKiPowers01427", () => ki.ComponentsArray = components.Where(c => !ReferenceEquals(c, cost)).ToArray(), () => ki.ComponentsArray = components, "removed effect/resource component");
        Reject(cache, "MonkKiPowers01427", () => cache.AddCachedBlueprint(ki.AssetGuid, Clone(ki)), () => cache.AddCachedBlueprint(ki.AssetGuid, ki), "same-GUID cache replacement");
        var owner = cost.OwnerBlueprint;
        Reject(cache, "MonkKiPowers01427", () => cost.OwnerBlueprint = (BlueprintScriptableObject)Clone(ki), () => cost.OwnerBlueprint = owner, "same-GUID foreign component owner");
        for (int i = 1; i < tokens.Length; i++) {
            var bp = Asset<BlueprintScriptableObject>(cache, tokens[i]); string method = new[] { "RegisterClamor", "RegisterDance", "RegisterSymphony" }[i - 1];
            var component = bp.ComponentsArray[0]; var originalOwner = component.OwnerBlueprint;
            Reject(cache, "BardMasterpieces01435", () => cache.AddCachedBlueprint(bp.AssetGuid, Clone(bp)), () => cache.AddCachedBlueprint(bp.AssetGuid, bp), "masterpiece same-GUID replacement " + tokens[i], method);
            Reject(cache, "BardMasterpieces01435", () => component.OwnerBlueprint = (BlueprintScriptableObject)Clone(bp), () => component.OwnerBlueprint = originalOwner, "masterpiece foreign owner " + tokens[i], method);
            var array = bp.ComponentsArray;
            Reject(cache, "BardMasterpieces01435", () => bp.ComponentsArray = array.Concat(new[] { component }).ToArray(), () => bp.ComponentsArray = array, "masterpiece duplicate component " + tokens[i], method);
        }
        var dance = Asset<BlueprintScriptableObject>(cache, tokens[2]); var rank = dance.GetComponent<ContextRankConfig>(); var step = Get(rank, "m_StepLevel");
        Reject(cache, "BardMasterpieces01435", () => Set(rank, "m_StepLevel", (int)step + 1), () => Set(rank, "m_StepLevel", step), "actual dance rank scaling", "RegisterDance", "m_StepLevel");
    }

    static void HydrateProviderInputs(BlueprintsCache cache)
    {
        foreach (var row in PsychicExpansionTests.NativeRows.Where(r => r["blueprint"]["Data"]["BlueprintParameterVariants"] != null)) {
            var feature = cache.Load(BlueprintGuid.Parse((string)row["blueprint"]["AssetId"])) as BlueprintParametrizedFeature;
            if (feature != null) feature.BlueprintParameterVariants = (AnyBlueprintReference[])PsychicExpansionTests.Read(row["blueprint"]["Data"]["BlueprintParameterVariants"], typeof(AnyBlueprintReference[]));
        }
        // The central extractor already reads all 102 native spell lists from
        // this installed blueprints.zip, records per-entry SHA and binds its
        // output in the formal build. Reuse that real input rather than a
        // different ZIP parser or an invented specialist spell list.
        foreach (var row in NativeLists.Where(r => ((string)r["name"]).StartsWith("Wizard") || ((string)r["name"]).StartsWith("Thassilonian"))) {
            var id = BlueprintGuid.Parse((string)row["blueprint"]["AssetId"]);
            if (cache.Load(id) != null) continue;
            var list = new BlueprintSpellList { AssetGuid = id, name = (string)row["name"], ComponentsArray = Array.Empty<BlueprintComponent>(), SpellsByLevel = (SpellLevelList[])PsychicExpansionTests.Read(row["blueprint"]["Data"]["SpellsByLevel"], typeof(SpellLevelList[])) };
            list.OnEnable(); cache.AddCachedBlueprint(id, list);
        }
        foreach (var row in NativeLists.Where(r => ((string)r["name"]).StartsWith("Wizard") || ((string)r["name"]).StartsWith("Thassilonian")))
            foreach (var level in ((JArray)row["blueprint"]["Data"]["SpellsByLevel"]).Where(r => (int)r["SpellLevel"] == 9))
                foreach (var reference in (JArray)level["m_Spells"])
                    if (reference.Type == JTokenType.String) Check(cache.Load(BlueprintGuid.Parse(((string)reference).Replace("!bp_", ""))) is BlueprintAbility, "every real provider level9 sort reference resolves to its native ability");
    }

    static string FileHash(string path) { using (var h = SHA256.Create()) return string.Concat(h.ComputeHash(File.ReadAllBytes(path)).Select(b => b.ToString("X2"))); }
    static void ExternalLists(BlueprintsCache cache, Harmony harmony)
    {
        HydrateProviderInputs(cache);
        string path = Path.GetFullPath("work/repair01440/references/TabletopTweaks-Core.dll"); var external = Assembly.LoadFrom(path);
        var type = external.GetType("TabletopTweaks.Core.Utilities.SpellTools", true);
        var methods = type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.DeclaredOnly)
            .Concat(type.GetNestedTypes(BindingFlags.Public | BindingFlags.NonPublic).Where(t => t.Name.StartsWith("<>c")).SelectMany(t => t.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly)))
            .Where(m => m.Name.Contains("AddToSpellList")).ToArray();
        Check(methods.Length == 7, "actual audited TTT AddToSpellList helper inventory remains exact");
        foreach (var method in methods) { harmony.Patch(method, transpiler: new HarmonyMethod(typeof(RegistrationWarmup01443Tests), nameof(SameProviderIL))); tttMethods++; }
        harmony.Patch(AccessTools.PropertyGetter(typeof(BlueprintUnitFact), "Name"), prefix: new HarmonyMethod(typeof(RegistrationWarmup01443Tests), nameof(RawName)));
        var spell = Asset<BlueprintAbility>(cache, "TimeStopSpell01438"); var wizard = (BlueprintSpellList)cache.Load(BlueprintGuid.Parse("ba0401fdeb4062f40a7aa95b6f07fe89"));
        var add = AccessTools.Method(type, "AddToSpellList", new[] { typeof(BlueprintAbility), typeof(BlueprintSpellList), typeof(int) });
        add.Invoke(null, new object[] { spell, wizard, 9 });
        var components = spell.GetComponents<SpellListComponent>().ToArray();
        Check(components.Length == 8 && components.All(c => c.SpellLevel == 9), "real provider publishes wizard/psychic plus specialist and five eligible Thassilonian rows");
        var externalComponents = components.Where(c => c.SpellList != wizard && c.SpellList.AssetGuid != BlueprintGuid.Parse("6a4b9d6651b155058004354a803da3bf")).ToArray();
        var arrays = externalComponents.ToDictionary(c => c.SpellList, c => c.SpellList.SpellsByLevel);
        var entries = arrays.ToDictionary(p => p.Key, p => p.Value.Select(r => (object)Get(r, "m_Spells")).ToArray());
        Dispatch(cache); add.Invoke(null, new object[] { spell, wizard, 9 }); Dispatch(cache);
        Check(spell.GetComponents<SpellListComponent>().Count() == 8, "provider and CotW repeated dispatch never duplicate membership");
        Check(arrays.All(p => ReferenceEquals(p.Key.SpellsByLevel, p.Value) && p.Value.Select((r, i) => ReferenceEquals(Get(r, "m_Spells"), entries[p.Key][i])).All(x => x)), "CotW retains every external list array, row collection and their ownership after legitimate publication");
        var component = externalComponents.First(); var list = component.SpellList; var row = list.SpellsByLevel[9]; var refs = (List<BlueprintAbilityReference>)Get(row, "m_Spells"); var all = spell.ComponentsArray;
        Reject(cache, "TimeStop01438", () => component.SpellLevel = 8, () => component.SpellLevel = 9, "wrong external level lacks reciprocity");
        Reject(cache, "TimeStop01438", () => Set(row, "m_Spells", refs.Where(r => r?.Guid != spell.AssetGuid).ToList()), () => Set(row, "m_Spells", refs), "missing external reciprocal row reference");
        Reject(cache, "TimeStop01438", () => Set(row, "m_Spells", refs.Concat(new[] { refs.Single(r => r?.Guid == spell.AssetGuid) }).ToList()), () => Set(row, "m_Spells", refs), "duplicate external reciprocal reference");
        Reject(cache, "TimeStop01438", () => spell.ComponentsArray = all.Concat(new BlueprintComponent[] { component }).ToArray(), () => spell.ComponentsArray = all, "duplicate external membership component");
        Reject(cache, "TimeStop01438", () => component.OwnerBlueprint = (BlueprintScriptableObject)Clone(spell), () => component.OwnerBlueprint = spell, "foreign metadata component owner");
        var reference = Get(component, "m_SpellList");
        Reject(cache, "TimeStop01438", () => Set(component, "m_SpellList", null), () => Set(component, "m_SpellList", reference), "null external list reference");
        Reject(cache, "TimeStop01438", () => row.SpellLevel = 8, () => row.SpellLevel = 9, "foreign array index/level mismatch");
        Reject(cache, "TimeStop01438", () => cache.AddCachedBlueprint(list.AssetGuid, Clone(list)), () => cache.AddCachedBlueprint(list.AssetGuid, list), "same-GUID cached external list replacement cannot masquerade as the old resolved reference");
        var reciprocal = refs.Single(r => r?.Guid == spell.AssetGuid); var cached = AccessTools.Property(typeof(BlueprintReferenceBase), "Cached"); var oldTarget = cached.GetValue(reciprocal, null);
        Reject(cache, "TimeStop01438", () => cached.SetValue(reciprocal, Clone(spell), null), () => cached.SetValue(reciprocal, oldTarget, null), "same-GUID foreign reciprocal spell object");
        var own = components.First(c => c.SpellList == wizard);
        Reject(cache, "TimeStop01438", () => own.SpellLevel = 8, () => own.SpellLevel = 9, "own wizard metadata remains strictly level9");
        observations.Add(new JObject { ["providerPath"] = path, ["providerSHA256"] = FileHash(path), ["providerVersion"] = external.GetName().Version.ToString(), ["identityTranspilerMethods"] = new JArray(methods.Select(m => m.ToString())), ["externalMemberships"] = new JArray(externalComponents.Select(c => c.SpellList.AssetGuid.ToString())) });
    }
    internal static int Run(BlueprintsCache parent, string nativeEvidence = null)
    {
        checks = jsonRoundTrips = nativeScans = nativeEnables = optionalDispatches = rejectedEdits = tttMethods = 0; observations.Clear();
        NativeLists = (JArray)JObject.Parse(File.ReadAllText(nativeEvidence ?? "work/prototype-build/psychic-expansion-native.json"))["allNativeSpellLists"];
        Check(NativeLists != null && NativeLists.Count > 0, "central real native spell-list inventory is available");
        var original = ResourcesLibrary.BlueprintsCache; var harmony = new Harmony("CotW01443.RegistrationWarmup");
        var cache = (BlueprintsCache)Old(typeof(TimeStopSpell01438Tests), "Fresh", parent); cache = RegistrationFeatKi01432Tests.FreshKiCache(cache);
        AccessTools.Field(typeof(ResourcesLibrary), "BlueprintsCache").SetValue(null, cache);
        try {
            TimeStutter01437Tests.PrepareNative(cache); RegistrationFeatKi01432Tests.PrepareMetadataFrightfulDonor(cache); Old(typeof(BardMasterpieces01435Tests), "Hydrate", cache);
            harmony.Patch(AccessTools.Method(Module("BlueprintRegistration"), "RegisterModule"), prefix: new HarmonyMethod(typeof(RegistrationWarmup01443Tests), nameof(Scope)));
            Retry(cache, "MonkKiPowers01427"); Retry(cache, "BardMasterpieces01435"); Retry(cache, "TimeStop01438"); Dispatch(cache);
            cache.ForEachLoaded((guid, blueprint) => { if (blueprint is BlueprintScriptableObject bp && (bp.name.StartsWith("CotWWrathPsychicKi") || bp.name.Contains("Masterpiece") || bp.name.StartsWith("CotWWrathPsychicTimeStop"))) { bp.OnEnable(); nativeEnables++; } }); nativeScans++;
            foreach (var ability in Loaded(cache).OfType<BlueprintAbility>().Where(a => a.name.StartsWith("CotWWrathPsychicKi"))) { var restrictions = ability.Restrictions; var targets = ability.TargetRestrictions; var casters = ability.CasterRestrictions; var full = ability.IsFullRoundAction; }
            Dispatch(cache); JsonAndPrivateNegatives(cache); ExternalLists(cache, harmony); Dispatch(cache);
            PsychicExpansionTests.Inventory["registrationWarmup01443Contracts"] = new JObject { ["passed"] = true, ["assertions"] = checks, ["nativeJsonRoundTrips"] = jsonRoundTrips, ["nativeCacheScans"] = nativeScans, ["nativeOnEnableCalls"] = nativeEnables, ["actualOptionalDispatches"] = optionalDispatches, ["authoredAndIdentityRejections"] = rejectedEdits, ["realTTTMethodsExecutedViaIdentityBodies"] = tttMethods, ["providerEvidence"] = new JArray(observations), ["boundary"] = "Actual native BlueprintsCache/OnEnable, native Json.Serializer null-reference roundtrips and real CotW optional dispatch execute on an isolated cache hydrated from the installed archive. The actual published TTT Core AddToSpellList and six helpers run with unchanged IL copied into Harmony skip-visibility bodies for CLR access to fields publicized by the provider build; raw blueprint names replace localization display during its sort. Specialist list rows and all level9 sort abilities come from the central installed-archive evidence. No provider initialization, Unity resources, game startup or complete saves are claimed; tester logs must confirm the actual external author of any further graph edit." };
            Console.WriteLine("PASS: RegistrationWarmup01443 " + checks + " assertions, native JSON=" + jsonRoundTrips + ", rejected=" + rejectedEdits); return checks;
        } finally { harmony.UnpatchAll(harmony.Id); AccessTools.Field(typeof(ResourcesLibrary), "BlueprintsCache").SetValue(null, original); }
    }
}

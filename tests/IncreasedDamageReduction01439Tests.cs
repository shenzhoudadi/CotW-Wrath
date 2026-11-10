using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
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
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.Controllers;
using Kingmaker.EntitySystem;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.Enums.Damage;
using Kingmaker.RuleSystem;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.RuleSystem.Rules.Damage;
using Kingmaker.Settings;
using CallOfTheWild.Wrath.Prototype;
using Kingmaker.Items;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Class.LevelUp;
using Kingmaker.UnitLogic.FactLogic;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Mechanics.Components;
using Kingmaker.UnitLogic.Parts;

internal static class IncreasedDamageReduction01439Tests
{
    const string FeatureId = "d10496e92d0799a40bb3930b8f4fda0d";
    const string StalwartId = "d5917881586ff1d4d96d5b7cebda9464";
    const string PaladinId = "bfa11238e7ae3544bbeb4d0b92e897ec";
    const string StonelordId = "cf0f99b3cd7444a48681b1c1c4aa2a41";
    const string SelectionId = "2cd91c501bda80b47ac2df0d51b02973";
    const string ProgressionId = "e93eabf4f9b48914c9d880dd41c06385";
    const string StanceBuffId = "3dccdf27a8209af478ac71cded18a271";
    const string StanceFeatureId = "2a6a2f8e492ab174eb3f01acf5b7c90a";
    const string ClassDrId = "4d4f48f401d5d8b408c2e7a973fba9ea";
    const string ArmorDrId = "3563079f53894be9a5ace106107818e5";
    const string NoArmorDrId = "ce36635292764199a2460c7a74a60cb9";
    const string BarbarianId = "ddaee203ee4dcb24c880d633fbd77db6";
    const string WizardId = "ba34257984f4c41408ce1dc2004e342e";
    static int checks, manualApplies, eventApplies, uidSerial;
    static readonly List<string> order = new List<string>();
    static Type Production => typeof(HowlShakenTarget).Assembly.GetType("CallOfTheWild.Wrath.Prototype.IncreasedDamageReduction01439", true);
    static string Text(string field) => (string)AccessTools.Field(Production, field).GetValue(null);
    static object Get(object target, string field) => AccessTools.Field(target.GetType(), field).GetValue(target);
    static void Set(object target, string field, object value) => AccessTools.Field(target.GetType(), field).SetValue(target, value);
    static void Check(bool ok, string message) { checks++; if (!ok) throw new Exception("Increased DR 01439: " + message); }
    static T Load<T>(BlueprintsCache cache, string id) where T : SimpleBlueprint => (T)cache.Load(BlueprintGuid.Parse(id));
    static BlueprintFeature Power(BlueprintsCache cache) => Load<BlueprintFeature>(cache, FeatureId);
    static ContextRankConfig RankOf(BlueprintFeature feature) => feature.GetComponents<ContextRankConfig>().Single();
    static string NameOf(object target, string field) => Get(target, field)?.ToString() ?? "";
    static int ListCount(ContextRankConfig rank) => (Get(rank, "m_FeatureList") as Array)?.Length ?? 0;
    static string KeyOf(BlueprintFeature feature) => Get(feature, "m_Description")?.GetType().GetProperty("Key")?.GetValue(Get(feature, "m_Description")) as string ?? "";
    static void EnumSet(object target, string field, string name)
    {
        var info = AccessTools.Field(target.GetType(), field);
        info.SetValue(target, Enum.Parse(info.FieldType, name));
    }
    static UnitEntityData Unit(BlueprintsCache cache, string label)
    {
        var unit = (UnitEntityData)AccessTools.Method(typeof(FlameJetLifecycle01430Tests), "Unit").Invoke(null, new object[] { cache, "01439-" + label });
        Set(unit.Descriptor, "<Body>k__BackingField", new UnitBody(unit.Descriptor));
        return unit;
    }
    static void Class(UnitEntityData unit, BlueprintCharacterClass cls, int level, BlueprintArchetype archetype = null)
    {
        unit.Progression.Classes.Clear();
        var data = new ClassData(cls) { Level = level };
        if (archetype != null) data.Archetypes.Add(archetype);
        unit.Progression.Classes.Add(data);
    }
    static Feature Grant(UnitEntityData unit, BlueprintFeature feature, int ranks)
    {
        Feature fact = null;
        for (int i = 0; i < ranks; i++) fact = (Feature)unit.Facts.Add(new Feature(feature, unit.Descriptor));
        Check(fact != null && fact.GetRank() == ranks && fact.IsTurnedOn, "native feature rank " + ranks + " actual " + fact?.GetRank());
        return fact;
    }
    static void ClearFact(UnitEntityData unit, BlueprintFeature feature)
    {
        while (unit.HasFact(feature)) unit.Facts.Remove(unit.GetFact(feature));
        Check(!unit.HasFact(feature), "native fact removal leaves no feature");
    }
    static int Current(Feature fact, AddDamageResistancePhysical component)
    {
        var runtime = (AddDamageResistanceBase.ComponentRuntime)fact.Components.Single(item => ReferenceEquals(item.SourceBlueprintComponent, component));
        return runtime.GetCurrentValue();
    }
    static int RankValue(Feature fact, BlueprintFeature feature)
        => RankOf(feature).GetValue(fact.MaybeContext);
    static int Bonus(object bonus)
    {
        if (bonus == null) return 0;
        var op = bonus.GetType().GetMethods(BindingFlags.Public | BindingFlags.Static).FirstOrDefault(method => method.Name == "op_Implicit" && method.ReturnType == typeof(int));
        return op == null ? Convert.ToInt32(bonus) : (int)op.Invoke(null, new[] { bonus });
    }
    static int FinalOf(RuleCalculateDamage calc, BaseDamage damage)
    {
        var value = AccessTools.Method(typeof(RuleCalculateDamage), "CalculateDamageValue").Invoke(calc, new object[] { damage });
        return (int)value.GetType().GetProperty("FinalValue").GetValue(value);
    }
    struct Hit { public int PhysicalReduction, EnergyReduction, PhysicalFinal, EnergyFinal; public bool FromEvent; }
    static Hit Settle(UnitEntityData attacker, UnitEntityData defender, bool energyOnly = false)
    {
        var physical = new PhysicalDamage(new ModifiableDiceFormula(new DiceFormula(0, DiceType.Zero)), 0, PhysicalDamageForm.Slashing) { PreRolledValue = 30 };
        var energy = new EnergyDamage(new DiceFormula(0, DiceType.Zero), 0, DamageEnergyType.Fire) { PreRolledValue = 30 };
        var deal = new RuleDealDamage(attacker, defender, energyOnly ? new DamageBundle(energy) : new DamageBundle(physical, energy)) { IsFake = true };
        var calc = new RuleCalculateDamage(deal);
        calc.OnTrigger(null);
        bool fromEvent = Bonus(physical.ReductionBecauseResistance) != 0 || Bonus(energy.ReductionBecauseResistance) != 0;
        if (!fromEvent && defender.Get<UnitPartDamageReduction>() != null)
        {
            defender.Get<UnitPartDamageReduction>().ApplyDamageReduction(calc);
            manualApplies++;
        }
        else if (fromEvent) eventApplies++;
        return new Hit {
            PhysicalReduction = Bonus(physical.ReductionBecauseResistance), EnergyReduction = Bonus(energy.ReductionBecauseResistance),
            PhysicalFinal = energyOnly ? 0 : FinalOf(calc, physical), EnergyFinal = FinalOf(calc, energy), FromEvent = fromEvent
        };
    }
    static string Signature(BlueprintsCache cache, string id)
    {
        var blueprint = cache.Load(BlueprintGuid.Parse(id));
        var components = AccessTools.Property(blueprint.GetType(), "ComponentsArray")?.GetValue(blueprint) as Array;
        var text = blueprint.GetType().Name + ":" + (components == null ? "null" : components.Length.ToString());
        if (components != null) foreach (var component in components) text += "|" + component.GetType().Name + "#" + component.GetHashCode();
        if (blueprint is BlueprintFeature feature)
            foreach (var rank in feature.GetComponents<ContextRankConfig>())
                text += "/" + NameOf(rank, "m_BaseValueType") + "/" + NameOf(rank, "m_Progression") + "/" + ListCount(rank) + "/" + NameOf(rank, "m_Feature");
        if (blueprint is BlueprintFeatureSelection selection)
            foreach (var field in new[] { "m_AllFeatures", "m_Features" })
            {
                var refs = Get(selection, field) as BlueprintFeatureReference[] ?? Array.Empty<BlueprintFeatureReference>();
                text += ";" + field + "=" + string.Join(",", refs.Select(item => item == null ? "" : item.Guid.ToString()));
            }
        return text;
    }
    static Exception InvokeRegister(BlueprintsCache cache)
    {
        try { AccessTools.Method(Production, "Register").Invoke(null, new object[] { cache }); return null; }
        catch (TargetInvocationException ex) { return ex.InnerException ?? ex; }
    }
    static bool Scope(string name) { order.Add(name); return name == "Increased Damage Reduction"; }
    static List<Tuple<string, bool, Exception>> Dispatch(BlueprintsCache cache)
    {
        order.Clear();
        var results = new List<Tuple<string, bool, Exception>>();
        Action<string, bool, Exception> report = (name, ok, error) => results.Add(Tuple.Create(name, ok, error));
        AccessTools.Method(typeof(HowlShakenTarget).Assembly.GetType("CallOfTheWild.Wrath.Prototype.BlueprintRegistration", true), "RegisterAll").Invoke(null, new object[] { cache, report });
        return results;
    }
    static readonly (string Id, string Path)[] NativeZipEntries =
    {
        (FeatureId, "Classes/StalwartDefender/DefensiveStance/StalwartDefenderDefensivePower/IncreasedDamageReductionDefensivePower.jbp"),
        (ClassDrId, "Classes/StalwartDefender/StalwartDefenderDamageReduction.jbp"),
        (ArmorDrId, "Classes/StalwartDefender/StalwartDefenderDamageReductionArmor.jbp"),
        (NoArmorDrId, "Classes/StalwartDefender/StalwartDefenderDamageReductionNoArmor.jbp"),
        (BarbarianId, "Classes/Barbarian/RagePowers/IncreasedDamageReductionFeature.jbp"),
        (SelectionId, "Classes/StalwartDefender/DefensiveStance/StalwartDefenderDefensivePower/StalwartDefenderDefensivePowerSelection.jbp"),
        (ProgressionId, "Classes/StalwartDefender/StalwartDefenderProgression.jbp"),
        (StanceBuffId, "Classes/StalwartDefender/DefensiveStance/DefensiveStanceBuff.jbp"),
        (StanceFeatureId, "Classes/StalwartDefender/DefensiveStance/DefensiveStanceFeature.jbp"),
        (StalwartId, "Classes/StalwartDefender/StalwartDefenderClass.jbp"),
        (PaladinId, "Classes/Paladin/PaladinClass.jbp"),
        (WizardId, "Classes/Wizard/WizardClass.jbp"),
        (StonelordId, "Classes/Paladin/Archetypes/Stonelord/StonelordArchetype.jbp")
    };
    static string GameZip()
    {
        var managed = Path.GetDirectoryName(typeof(BlueprintFeature).Assembly.Location);
        return Path.GetFullPath(Path.Combine(managed, "..", "..", "blueprints.zip"));
    }
    static bool Hydrated(BlueprintScriptableObject blueprint)
        => blueprint?.ComponentsArray != null && (blueprint.ComponentsArray.Length > 0 || blueprint is BlueprintProgression || blueprint is BlueprintFeatureSelection);
    static void Publish(BlueprintsCache cache, BlueprintScriptableObject blueprint)
    {
        cache.AddCachedBlueprint(blueprint.AssetGuid, blueprint);
        if (ReferenceEquals(cache.Load(blueprint.AssetGuid), blueprint)) return;
        var table = (IDictionary)Get(cache, "m_LoadedBlueprints");
        var entry = table[blueprint.AssetGuid];
        var field = entry?.GetType().GetField("Blueprint", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (field != null) field.SetValue(entry, blueprint);
        else table[blueprint.AssetGuid] = blueprint;
        Check(ReferenceEquals(cache.Load(blueprint.AssetGuid), blueprint), "native zip blueprint is the cached object " + blueprint.AssetGuid);
    }
    static void Hydrate(BlueprintsCache cache, string id, string entryPath)
    {
        var current = cache.Load(BlueprintGuid.Parse(id)) as BlueprintScriptableObject;
        if (Hydrated(current) && string.Equals(current.AssetGuid.ToString(), id, StringComparison.OrdinalIgnoreCase)) return;
        Remember(id, current);
        var zipPath = GameZip();
        Check(File.Exists(zipPath), "game blueprints.zip is present at " + zipPath);
        var root = JObject.Parse(System.Text.Encoding.UTF8.GetString(ReadZipEntry(zipPath, entryPath)));
        Check((string)root["AssetId"] == id, "zip entry " + entryPath + " is asset " + id);
        var data = (JObject)root["Data"];
        var typeName = ((string)data["$type"]).Split(',').Last().Trim();
        var type = typeof(BlueprintFeature).Assembly.GetTypes().Single(item => item.Name == typeName && typeof(BlueprintScriptableObject).IsAssignableFrom(item));
        var blueprint = (BlueprintScriptableObject)Activator.CreateInstance(type);
        blueprint.name = Path.GetFileNameWithoutExtension(entryPath);
        blueprint.AssetGuid = BlueprintGuid.Parse(id);
        foreach (var property in data.Properties())
        {
            if (property.Name == "$type" || property.Name == "PrototypeLink" || property.Name == "m_Overrides" || property.Name == "Comment" || property.Name == "m_Icon" || property.Name == "IsPrerequisiteFor") continue;
            var fieldName = property.Name == "Components" ? "ComponentsArray" : property.Name;
            var field = AccessTools.Field(type, fieldName);
            var propertyInfo = field == null ? AccessTools.Property(type, fieldName) : null;
            if ((field == null || field.IsInitOnly) && (propertyInfo == null || !propertyInfo.CanWrite)) continue;
            var targetType = field != null ? field.FieldType : propertyInfo.PropertyType;
            object value;
            try { value = PsychicExpansionTests.Read(property.Value, targetType); }
            catch (Exception ex) { throw new Exception(entryPath + " field " + fieldName + " failed: " + ex.Message, ex); }
            if (field != null && !field.IsInitOnly) field.SetValue(blueprint, value);
            else propertyInfo.SetValue(blueprint, value, null);
        }
        blueprint.OnEnable();
        Publish(cache, blueprint);
    }
    static byte[] ReadZipEntry(string zipPath, string entryPath)
    {
        long offset;
        using (var archive = new ZipArchive(File.Open(zipPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite), ZipArchiveMode.Read))
        {
            var entry = archive.GetEntry(entryPath) ?? throw new Exception("missing blueprints.zip entry " + entryPath);
            offset = (long)AccessTools.Field(typeof(ZipArchiveEntry), "_offsetOfLocalHeader").GetValue(entry);
        }
        using (var stream = File.Open(zipPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
        {
            stream.Position = offset;
            var header = ReadExact(stream, 30);
            if (BitConverter.ToUInt32(header, 0) != 0x04034b50) throw new Exception("local header missing for " + entryPath);
            ushort method = BitConverter.ToUInt16(header, 8);
            ulong compressed = BitConverter.ToUInt32(header, 18);
            ulong uncompressed = BitConverter.ToUInt32(header, 22);
            int nameLength = BitConverter.ToUInt16(header, 26);
            int extraLength = BitConverter.ToUInt16(header, 28);
            stream.Position += nameLength;
            var extra = extraLength == 0 ? Array.Empty<byte>() : ReadExact(stream, extraLength);
            if (compressed == 0xFFFFFFFF || uncompressed == 0xFFFFFFFF) ReadZip64(extra, ref uncompressed, ref compressed);
            var payload = ReadExact(stream, checked((int)compressed));
            if (method == 0) return payload;
            if (method != 8) throw new Exception("unsupported zip method " + method + " for " + entryPath);
            using (var input = new MemoryStream(payload))
            using (var deflate = new DeflateStream(input, CompressionMode.Decompress))
            using (var output = new MemoryStream())
            {
                deflate.CopyTo(output);
                var bytes = output.ToArray();
                if (uncompressed != 0xFFFFFFFF && bytes.Length != (int)uncompressed) throw new Exception(entryPath + " inflated " + bytes.Length + " bytes, zip64 size " + uncompressed);
                return bytes;
            }
        }
    }
    static void ReadZip64(byte[] extra, ref ulong uncompressed, ref ulong compressed)
    {
        int cursor = 0;
        bool needRaw = uncompressed == 0xFFFFFFFF, needCompressed = compressed == 0xFFFFFFFF;
        while (cursor + 4 <= extra.Length)
        {
            ushort id = BitConverter.ToUInt16(extra, cursor);
            ushort size = BitConverter.ToUInt16(extra, cursor + 2);
            cursor += 4;
            if (id == 1)
            {
                if (needRaw) { uncompressed = BitConverter.ToUInt64(extra, cursor); cursor += 8; }
                if (needCompressed) compressed = BitConverter.ToUInt64(extra, cursor);
                return;
            }
            cursor += size;
        }
        throw new Exception("zip64 extra field missing");
    }
    static byte[] ReadExact(Stream stream, int count)
    {
        var buffer = new byte[count];
        int read = 0;
        while (read < count)
        {
            int got = stream.Read(buffer, read, count - read);
            if (got == 0) throw new EndOfStreamException("short zip read");
            read += got;
        }
        return buffer;
    }
    static readonly Dictionary<string, BlueprintScriptableObject> previousBlueprints = new Dictionary<string, BlueprintScriptableObject>();
    static void Remember(string id, BlueprintScriptableObject current)
    {
        if (!previousBlueprints.ContainsKey(id)) previousBlueprints[id] = current;
    }
    static void RestoreNativeFixtures(BlueprintsCache cache)
    {
        foreach (var pair in previousBlueprints)
        {
            var guid = BlueprintGuid.Parse(pair.Key);
            if (pair.Value == null)
            {
                var table = (IDictionary)Get(cache, "m_LoadedBlueprints");
                table.Remove(guid);
            }
            else Publish(cache, pair.Value);
        }
        previousBlueprints.Clear();
    }
    static void HydrateAll(BlueprintsCache cache)
    {
        foreach (var item in NativeZipEntries) Hydrate(cache, item.Id, item.Path);
    }
    static object priorState;
    static object priorClock;
    static bool createdState;
    static bool createdClock;
    static void EnsureGame(Harmony harmony)
    {
        var game = Game.Instance ?? throw new Exception("game instance fixture is missing");
        priorState = Get(game, "State");
        priorClock = Get(game, "TimeController");
        createdState = priorState == null;
        createdClock = priorClock == null;
        if (createdState)
        {
            var state = (PersistentState)FormatterServices.GetUninitializedObject(typeof(PersistentState));
            var player = (Player)FormatterServices.GetUninitializedObject(typeof(Player));
            player.GameTime = TimeSpan.FromHours(1);
            Set(player, "m_Party", new List<UnitEntityData>());
            Set(player, "m_CharacterListsValid", true);
            state.PlayerState = player;
            Set(game, "State", state);
        }
        if (createdClock) Set(game, "TimeController", FormatterServices.GetUninitializedObject(typeof(TimeController)));
        harmony.Patch(AccessTools.Method(typeof(Player), "GetNewUniqueId"), prefix: new HarmonyMethod(typeof(IncreasedDamageReduction01439Tests), nameof(UniqueId)));
        harmony.Patch(AccessTools.PropertyGetter(typeof(TimeController), "GameTime"), prefix: new HarmonyMethod(typeof(IncreasedDamageReduction01439Tests), nameof(Clock)));
        Check(game.Player != null && game.TimeController != null, "native fact attachment has a player and clock");
    }
    static void RestoreGame()
    {
        var game = Game.Instance;
        if (game == null) return;
        if (createdState) Set(game, "State", priorState);
        if (createdClock) Set(game, "TimeController", priorClock);
        createdState = createdClock = false;
    }
    static bool UniqueId(ref string __result) { __result = "01439-fact-" + (++uidSerial); return false; }
    static bool Clock(ref TimeSpan __result) { __result = TimeSpan.FromHours(1); return false; }
    internal static int Run(BlueprintsCache cache)
    {
        checks = manualApplies = eventApplies = uidSerial = 0;
        previousBlueprints.Clear();
        var harmony = new Harmony("CotW01439.IncreasedDamageReduction");
        try
        {
        EnsureGame(harmony);
        HydrateAll(cache);
        var feature = Power(cache);
        if (feature == null || feature.GetComponents<ContextRankConfig>().Count() != 1)
            throw new Exception("feature load name=" + feature?.name + " type=" + feature?.GetType().FullName + " components=" + (feature?.ComponentsArray == null ? "null" : string.Join(",", feature.ComponentsArray.Select(c => c.GetType().Name))));
        var rank = RankOf(feature);
        var dr = feature.GetComponent<AddDamageResistancePhysical>();
        var components = feature.ComponentsArray.ToArray();
        string originalKey = KeyOf(feature);
        var protectedIds = new[] { ClassDrId, ArmorDrId, NoArmorDrId, BarbarianId, StanceBuffId, StanceFeatureId, SelectionId, ProgressionId };
        var before = protectedIds.ToDictionary(id => id, id => Signature(cache, id));
        int classBefore = ClassValue(cache, "class-before");
        Check(feature.Ranks == 2 && feature.IsClassFeature && feature.ReapplyOnLevelUp && components.Length == 4 && dr != null && dr.IsStackable, "native power keeps two ranks, one stackable physical component and four components");
        Check(NameOf(rank, "m_BaseValueType") == "FeatureRank" && NameOf(rank, "m_Progression") == "AsIs" && ListCount(rank) == 0, "native rank reads this feature once");
        var gapUnit = Unit(cache, "gap");
        var gap1 = Grant(gapUnit, feature, 1);
        Check(RankValue(gap1, feature) == 1 && Current(gap1, dr) == 1, "before registration rank 1 is DR 1; rank=" + RankValue(gap1, feature) + " current=" + Current(gap1, dr));
        ClearFact(gapUnit, feature);
        var gap2 = Grant(gapUnit, feature, 2);
        Check(RankValue(gap2, feature) == 2 && Current(gap2, dr) == 2, "before registration rank 2 is DR 2; rank=" + RankValue(gap2, feature) + " current=" + Current(gap2, dr));
        ClearFact(gapUnit, feature);

        EnumSet(rank, "m_Progression", "Div2");
        var rejected = InvokeRegister(cache);
        Check(rejected is InvalidOperationException && NameOf(rank, "m_Progression") == "Div2" && NameOf(rank, "m_BaseValueType") == "FeatureRank" && KeyOf(feature) == originalKey && feature.ComponentsArray.SequenceEqual(components), "unknown progression is rejected and the native config is restored");
        EnumSet(rank, "m_Progression", "AsIs");
        harmony.Patch(AccessTools.Method(Production, "ConfirmCommitted"), prefix: new HarmonyMethod(typeof(IncreasedDamageReduction01439Tests), nameof(FailCommit)));
        harmony.Patch(AccessTools.Method(typeof(RuleCalculateDamage), "ApplyDifficultyBeforeCalculated"), prefix: new HarmonyMethod(typeof(IncreasedDamageReduction01439Tests), nameof(Skip)));
        harmony.Patch(AccessTools.Method(typeof(RuleCalculateDamage), "ApplyDifficultyForDamageReduction"), prefix: new HarmonyMethod(typeof(IncreasedDamageReduction01439Tests), nameof(Skip)));
        harmony.Patch(AccessTools.PropertyGetter(typeof(SettingsRoot), "Difficulty"), prefix: new HarmonyMethod(typeof(IncreasedDamageReduction01439Tests), nameof(NeutralDifficulty)));
        harmony.Patch(AccessTools.Method(typeof(ShadowSpellLogicExtension), "TryApplyShadowDamageFactor", new[] { typeof(RulebookEvent), typeof(UnitEntityData), typeof(int) }), prefix: new HarmonyMethod(typeof(IncreasedDamageReduction01439Tests), nameof(NoShadow)));
        harmony.Patch(AccessTools.PropertyGetter(typeof(IgnorePrerequisites), "Ignore"), prefix: new HarmonyMethod(typeof(IncreasedDamageReduction01439Tests), nameof(NoIgnore)));
            var rolled = InvokeRegister(cache);
            Check(rolled is InvalidOperationException && rolled.Message.Contains("commit fault") && NameOf(rank, "m_BaseValueType") == "FeatureRank" && ListCount(rank) == 0 && KeyOf(feature) == originalKey && feature.ComponentsArray.SequenceEqual(components), "a fault after the write rolls the rank and description back");
            harmony.Unpatch(AccessTools.Method(Production, "ConfirmCommitted"), HarmonyPatchType.Prefix, harmony.Id);
            var vanillaEnergyUnit = Unit(cache, "vanilla-energy-only");
            Grant(vanillaEnergyUnit, feature, 1);
            var vanillaEnergy = Settle(Unit(cache, "vanilla-energy-attacker"), vanillaEnergyUnit, true);
            Check(vanillaEnergy.EnergyReduction == 1 && vanillaEnergy.EnergyFinal == 29, "native shared stackable accounting already reduces a pure-energy bundle before the scale change: " + Format(vanillaEnergy));
            ClearFact(vanillaEnergyUnit, feature);
            Check(InvokeRegister(cache) == null && NameOf(rank, "m_BaseValueType") == "FeatureListRanks" && ListCount(rank) == 2 && NameOf(rank, "m_Progression") == "AsIs" && KeyOf(feature) == Text("DescriptionKey"), "registration doubles only this rank config and points the description at the new key");
            Check(ReferenceEquals(feature.GetComponent<AddDamageResistancePhysical>(), dr) && feature.GetComponents<ContextRankConfig>().Count() == 1 && feature.ComponentsArray.SequenceEqual(components) && feature.Ranks == 2, "registration does not add or replace a component");
            var one = Grant(gapUnit, feature, 1);
            Check(RankValue(one, feature) == 2 && Current(one, dr) == 2, "after registration rank 1 is DR 2; rank=" + RankValue(one, feature) + " current=" + Current(one, dr));
            ClearFact(gapUnit, feature);
            var two = Grant(gapUnit, feature, 2);
            two.TurnOff(); two.TurnOn();
            Check(RankValue(two, feature) == 4 && Current(two, dr) == 4, "after registration rank 2 is DR 4; rank=" + RankValue(two, feature) + " current=" + Current(two, dr));
            var runtime = (AddDamageResistanceBase.ComponentRuntime)two.Components.Single(item => ReferenceEquals(item.SourceBlueprintComponent, dr));
            var physicalProbe = new PhysicalDamage(new ModifiableDiceFormula(new DiceFormula(0, DiceType.Zero)), 0, PhysicalDamageForm.Slashing);
            var energyProbe = new EnergyDamage(new DiceFormula(0, DiceType.Zero), 0, DamageEnergyType.Fire);
            Check(!runtime.Bypassed(physicalProbe, null) && runtime.Bypassed(energyProbe, null), "native physical component still rejects energy and accepts physical damage");
            ClearFact(gapUnit, feature);
            var ranked = Grant(gapUnit, feature, 1);

            var attacker = Unit(cache, "attacker");
            var bare = Unit(cache, "bare");
            var none = Settle(attacker, bare);
            Check(none.PhysicalReduction == 0 && none.EnergyReduction == 0 && none.PhysicalFinal == 30 && none.EnergyFinal == 30, "no selection leaves physical and energy damage unchanged: " + Format(none));
            var first = Settle(attacker, gapUnit);
            Check(Current(ranked, dr) == 2 && first.PhysicalReduction == 2 && first.PhysicalFinal == 28 && first.EnergyReduction == 0 && first.EnergyFinal == 30, "rank 1 enters native reduction and final damage as 2, without energy reduction: " + Format(first));
            var energyOnly = Settle(attacker, gapUnit, true);
            Check(energyOnly.EnergyReduction == 2 && energyOnly.EnergyFinal == 28, "documented inherited pure-energy stackable allowance is now 2: " + Format(energyOnly));
            ClearFact(gapUnit, feature);
            var removed = Settle(attacker, gapUnit);
            Check(removed.PhysicalReduction == 0 && removed.PhysicalFinal == 30 && removed.EnergyReduction == 0, "removing the feature leaves no reduction: " + Format(removed));
            var again = Grant(gapUnit, feature, 1);
            var restored = Settle(attacker, gapUnit);
            Check(Current(again, dr) == 2 && restored.PhysicalReduction == 2 && restored.PhysicalFinal == 28, "re-adding rank 1 calculates DR 2 again: " + Format(restored));
            var stranger = Unit(cache, "stranger");
            var foreign = Settle(attacker, stranger);
            Check(foreign.PhysicalReduction == 0 && foreign.EnergyReduction == 0 && foreign.PhysicalFinal == 30, "another unit does not borrow this reduction: " + Format(foreign));
            ClearFact(gapUnit, feature);
            var second = Grant(gapUnit, feature, 2);
            second.TurnOff(); second.TurnOn();
            var doubled = Settle(attacker, gapUnit);
            Check(doubled.PhysicalReduction == 4 && doubled.PhysicalFinal == 26 && doubled.EnergyReduction == 0 && doubled.EnergyFinal == 30, "rank 2 enters native reduction and final damage as 4, without energy reduction: " + Format(doubled));
            ClearFact(gapUnit, feature);

            int classAfter = ClassValue(cache, "class-after");
            Check(classBefore > 0 && classBefore == classAfter, "stalwart class damage reduction value stays " + classBefore + " / " + classAfter);
            var combined = Unit(cache, "combined");
            var parent = Grant(combined, Load<BlueprintFeature>(cache, ClassDrId), 1);
            var unlocked = combined.Facts.List.OfType<Feature>().Single(fact => fact.Blueprint.AssetGuid.ToString() == NoArmorDrId);
            var noArmorDr = unlocked.Blueprint.GetComponent<AddDamageResistancePhysical>();
            int classDr = Current(unlocked, noArmorDr);
            var own = Grant(combined, feature, 1);
            var both = Settle(attacker, combined);
            var energySample = new EnergyDamage(new DiceFormula(0, DiceType.Zero), 0, DamageEnergyType.Fire);
            bool classBypassesEnergy = ((AddDamageResistanceBase.ComponentRuntime)unlocked.Components.Single(item => ReferenceEquals(item.SourceBlueprintComponent, noArmorDr))).Bypassed(energySample, null);
            bool ownBypassesEnergy = ((AddDamageResistanceBase.ComponentRuntime)own.Components.Single(item => ReferenceEquals(item.SourceBlueprintComponent, dr))).Bypassed(energySample, null);
            Check(classDr > 0 && Current(own, dr) == 2 && classBypassesEnergy && ownBypassesEnergy && both.PhysicalReduction == classDr + 2 && both.PhysicalFinal == 30 - (classDr + 2) && both.EnergyReduction == 2 && both.EnergyFinal == 28, "native part adds unarmored DR " + classDr + " to this stackable DR 2 on physical damage, then applies the unconsumed stackable 2 to later energy damage in the same bundle: " + Format(both));
            Check(ReferenceEquals(parent.Blueprint, Load<BlueprintFeature>(cache, ClassDrId)) && !combined.Facts.List.Any(fact => fact.Blueprint.AssetGuid.ToString() == ArmorDrId), "the unarmored body keeps the class feature and does not unlock the armor reduction");

            Check(InvokeRegister(cache) == null && ListCount(rank) == 2 && feature.ComponentsArray.Length == 4 && Current(Grant(Unit(cache, "repeat"), feature, 2), dr) == 4, "repeating registration does not add another copy or another doubling");
            var copy = new AddDamageResistancePhysical();
            for (var type = dr.GetType(); type != null && type != typeof(object); type = type.BaseType)
                foreach (var field in type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
                    field.SetValue(copy, field.GetValue(dr));
            feature.ComponentsArray = feature.ComponentsArray.Select(component => ReferenceEquals(component, dr) ? copy : component).ToArray();
            Check(InvokeRegister(cache) == null && ReferenceEquals(feature.GetComponent<AddDamageResistancePhysical>(), copy) && feature.GetComponents<AddDamageResistancePhysical>().Count() == 1 && NameOf(rank, "m_BaseValueType") == "FeatureListRanks", "a replaced damage-reduction component is left in place and is not given a second reduction");
            feature.ComponentsArray = components;
            var wizard = Load<BlueprintCharacterClass>(cache, WizardId);
            var extra = new PrerequisiteClassLevel { Level = 12, Group = Prerequisite.GroupType.Any };
            Set(extra, "m_CharacterClass", BlueprintReferenceBase.CreateTyped<BlueprintCharacterClassReference>(wizard));
            feature.ComponentsArray = components.Concat(new BlueprintComponent[] { extra }).ToArray();
            Check(InvokeRegister(cache) == null && feature.GetComponents<PrerequisiteClassLevel>().Count(item => ReferenceEquals(item, extra)) == 1 && feature.GetComponents<PrerequisiteClassLevel>().Count() == 2, "an extra Any class prerequisite is kept once");
            var wizardUnit = Unit(cache, "wizard12"); Class(wizardUnit, wizard, 12);
            var wizardLow = Unit(cache, "wizard11"); Class(wizardLow, wizard, 11);
            Check(feature.MeetsPrerequisites(null, wizardUnit.Descriptor, null, false, false) && !feature.MeetsPrerequisites(null, wizardLow.Descriptor, null, false, false), "the added level-12 prerequisite qualifies only at 12");
            feature.ComponentsArray = components;
            Check(feature.GetComponents<PrerequisiteClassLevel>().Count() == 1 && !feature.MeetsPrerequisites(null, wizardUnit.Descriptor, null, false, false), "removing the test prerequisite restores the original requirement");

            Prerequisites(cache, feature);
            Check(InvokeRegister(cache) == null, "registration remains idempotent after the prerequisite probe");
            var badOrder = new List<string>();
            EnumSet(rank, "m_Progression", "Div2");
            harmony.Patch(AccessTools.Method(typeof(HowlShakenTarget).Assembly.GetType("CallOfTheWild.Wrath.Prototype.BlueprintRegistration", true), "RegisterModule"), prefix: new HarmonyMethod(typeof(IncreasedDamageReduction01439Tests), nameof(Scope)));ScopedAvailable01463Tests.Bind(harmony,typeof(HowlShakenTarget).Assembly.GetType("CallOfTheWild.Wrath.Prototype.BlueprintRegistration", true),"ScopeIncreasedDamageReduction01439Tests");
            var failed = Dispatch(cache);
            badOrder.AddRange(order);
            var skippedFailure = new HashSet<string>(badOrder.Where(name => name != "Increased Damage Reduction"));
            var isolated = failed.Where(item => item.Item1 == "Increased Damage Reduction").ToArray();
            Check(isolated.Length == 1 && !isolated[0].Item2 && isolated[0].Item3 is InvalidOperationException && NameOf(rank, "m_Progression") == "Div2" && failed.All(item => item.Item1 == "Increased Damage Reduction" || !skippedFailure.Contains(item.Item1)), "RegisterAll isolates the rejected shape and does not report skipped modules as registered; ours=" + isolated.Length + "; reported=" + string.Join(" | ", failed.Select(item => item.Item1 + ":" + item.Item2)));
            EnumSet(rank, "m_Progression", "AsIs");
            var passed = Dispatch(cache);
            var skippedSuccess = new HashSet<string>(order.Where(name => name != "Increased Damage Reduction"));
            var accepted = passed.Where(item => item.Item1 == "Increased Damage Reduction").ToArray();
            Check(accepted.Length == 1 && accepted[0].Item2 && NameOf(rank, "m_BaseValueType") == "FeatureListRanks" && ListCount(rank) == 2 && KeyOf(feature) == Text("DescriptionKey") && passed.All(item => item.Item1 == "Increased Damage Reduction" || !skippedSuccess.Contains(item.Item1)), "the real RegisterAll caller applies the doubled shape once; ours=" + accepted.Length + "; reported=" + string.Join(" | ", passed.Select(item => item.Item1 + ":" + item.Item2)));
            string clamor = (string)AccessTools.Field(typeof(HowlShakenTarget).Assembly.GetType("CallOfTheWild.Wrath.Prototype.BardMasterpieces01435", true), "ClamorModule").GetValue(null);
            Check(badOrder.IndexOf("Increased Damage Reduction") >= 0 && badOrder.IndexOf("Increased Damage Reduction") < badOrder.IndexOf(clamor) && order.IndexOf(clamor) > order.IndexOf("Increased Damage Reduction"), "a later module call is still reached after rejection and after success");
            harmony.Unpatch(AccessTools.Method(typeof(HowlShakenTarget).Assembly.GetType("CallOfTheWild.Wrath.Prototype.BlueprintRegistration", true), "RegisterModule"), HarmonyPatchType.Prefix, harmony.Id);

            foreach (var id in protectedIds) Check(Signature(cache, id) == before[id], "unrelated blueprint stayed unchanged: " + id);
            Texts();
            var loaded = ((System.Collections.IDictionary)Get(cache, "m_LoadedBlueprints")).Keys.Cast<object>().Select(key => key.ToString()).ToArray();
            Check(!loaded.Contains("new-01439") && feature.ComponentsArray.SequenceEqual(components), "this change publishes no new blueprint");
        }
        finally { RestoreNativeFixtures(cache); RestoreGame(); harmony.UnpatchAll(harmony.Id); }
        PsychicExpansionTests.Inventory["repair01440Contracts"] = new JObject {
            ["passed"] = true, ["additionalItemCount"] = 0,
            ["activation"] = "native-always-active",
            ["mixedNativeEnergyReduction"] = 2,
            ["nativePureEnergyReductionBeforeScale"] = 1,
            ["nativePureEnergyReductionAfterScale"] = 2,
            ["mixedBehavior"] = "Inherited native stackable-DR accounting; both components independently bypass energy, but the shared part reuses the stackable allowance in a mixed bundle with class DR.",
            ["newBlueprints"] = new JArray()
        };
        PsychicExpansionTests.Inventory["increasedDamageReduction01439Contracts"] = new JObject {
            ["passed"] = true, ["assertions"] = checks, ["additionalItemCount"] = 1, ["itemIds"] = new JArray("COTW-0584"),
            ["newBlueprints"] = new JArray(), ["newSpellRootCount"] = 0, ["ordinaryFeatCount"] = 0,
            ["nativeGapRank1"] = 1, ["nativeGapRank2"] = 2, ["scaledRank1"] = 2, ["scaledRank2"] = 4,
            ["eventDeliveredCalculations"] = eventApplies, ["directPartApplications"] = manualApplies,
            ["boundary"] = "Native ContextRankConfig.GetValue, AddDamageResistancePhysical runtime, UnitPartDamageReduction.ApplyDamageReduction and RuleCalculateDamage.OnTrigger/CalculateDamageValue ran on the game assemblies. The damage-reduction event had no subscriber in this fixture, so the test called the native part directly. Difficulty modifiers were replaced with a neutral value. A bundle that contains physical damage and then energy damage follows the part's stackable accounting. No game process, scene, hit-point loss or UI was started."
        };
        Console.WriteLine("PASS: " + checks + " Increased Damage Reduction checks. event=" + eventApplies + " directPart=" + manualApplies);
        return checks;
    }

    static int ClassValue(BlueprintsCache cache, string label)
    {
        var unit = Unit(cache, label);
        Grant(unit, Load<BlueprintFeature>(cache, ClassDrId), 1);
        var armor = Load<BlueprintFeature>(cache, ArmorDrId);
        var fact = Grant(unit, armor, 1);
        return Current(fact, armor.GetComponent<AddDamageResistancePhysical>());
    }
    static void Prerequisites(BlueprintsCache cache, BlueprintFeature feature)
    {
        var stalwart = Load<BlueprintCharacterClass>(cache, StalwartId);
        var paladin = Load<BlueprintCharacterClass>(cache, PaladinId);
        var stone = Load<BlueprintArchetype>(cache, StonelordId);
        var pool = Load<BlueprintFeatureSelection>(cache, SelectionId);
        var refs = Get(pool, "m_AllFeatures") as BlueprintFeatureReference[] ?? Array.Empty<BlueprintFeatureReference>();
        Check(refs.Count(item => item != null && item.Guid == feature.AssetGuid) == 1, "the defensive-power selection still lists this feature once");
        bool Ok(UnitEntityData unit)
        {
            bool meets = feature.MeetsPrerequisites(null, unit.Descriptor, null, false, false);
            var state = (LevelUpState)FormatterServices.GetUninitializedObject(typeof(LevelUpState));
            Set(state, "m_Unit", unit); Set(state, "Selections", new List<FeatureSelectionState>());
            state.SelectedClass = unit.Progression.Classes[0].CharacterClass; state.NextClassLevel = unit.Progression.Classes[0].Level; Set(state, "NextCharacterLevel", unit.Progression.Classes[0].Level);
            var selection = state.AddSelection(null, default(FeatureSource), pool, 1);
            bool can = pool.CanSelect(unit.Descriptor, state, selection, feature);
            Check(meets == can, "selection entry and feature prerequisites agree: meets=" + meets + " can=" + can + " level=" + unit.Progression.Classes[0].Level);
            return meets;
        }
        var ready = Unit(cache, "stalwart6"); Class(ready, stalwart, 6); Check(Ok(ready), "stalwart defender 6 qualifies");
        var early = Unit(cache, "stalwart5"); Class(early, stalwart, 5); Check(!Ok(early), "stalwart defender 5 does not qualify");
        var lord = Unit(cache, "stonelord12"); Class(lord, paladin, 12, stone); Check(Ok(lord), "Stonelord 12 qualifies through the existing archetype prerequisite");
        var lordLow = Unit(cache, "stonelord11"); Class(lordLow, paladin, 11, stone); Check(!Ok(lordLow), "Stonelord 11 does not qualify");
        var plain = Unit(cache, "paladin12"); Class(plain, paladin, 12); Check(!Ok(plain), "paladin 12 without Stonelord does not qualify");
    }
    static void Texts()
    {
        var root = TestPaths.Outputs("package", "Localization");
        var en = File.ReadAllText(Path.Combine(root, "enGB.json"));
        var zh = File.ReadAllText(Path.Combine(root, "zhCN.json"));
        Check(en.Contains(Text("DescriptionKey")) && en.Contains(Text("DescriptionEn")) && zh.Contains(Text("DescriptionZh")), "Both locale files retain the same English 2/4 description at " + root);
    }
    static string Format(Hit hit) => "P" + hit.PhysicalReduction + "/" + hit.PhysicalFinal + " E" + hit.EnergyReduction + "/" + hit.EnergyFinal + " event=" + hit.FromEvent;
    static bool FailCommit() { throw new InvalidOperationException("01439 commit fault"); }
    static bool Skip() => false;
    static DifficultySettings neutralDifficulty;
    static bool NeutralDifficulty(ref DifficultySettings __result)
    {
        if (neutralDifficulty == null)
        {
            neutralDifficulty = (DifficultySettings)FormatterServices.GetUninitializedObject(typeof(DifficultySettings));
            foreach (var name in new[] { "EnemyCriticalHits", "StatsAdjustments" })
            {
                var field = AccessTools.Field(typeof(DifficultySettings), name);
                var entity = FormatterServices.GetUninitializedObject(field.FieldType);
                var valueType = field.FieldType.BaseType.GetGenericArguments()[0];
                AccessTools.Field(field.FieldType.BaseType, "m_CurrentValue").SetValue(entity, Enum.ToObject(valueType, 0));
                AccessTools.Field(field.FieldType.BaseType, "m_CurrentValueIsSet").SetValue(entity, true);
                field.SetValue(neutralDifficulty, entity);
            }
        }
        __result = neutralDifficulty;
        return false;
    }
    static bool NoShadow(int __2, ref int __result) { __result = __2; return false; }
    static bool NoIgnore(ref bool __result) { __result = false; return false; }
}

using System.IO.Compression;
using System.Reflection;
using System.Text.Json;
using BlueprintCore.Utils;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Selection;
using Kingmaker.UnitLogic;
using Kingmaker.EntitySystem.Entities;
using MediumClass.Medium;
using MediumClass.Utilities;
using Mono.Cecil;
using Mono.Cecil.Cil;

// Usage: MediumProgressionLevels [Assembly-CSharp.dll [blueprints.zip [merged MediumClass.dll]]]
int checks = 0, skipped = 0;
// MEDIUM_TEST_CONTINUE=1 lists every failing check instead of stopping at the first (used for the 0.3.3 negative control).
bool keepGoing = Environment.GetEnvironmentVariable("MEDIUM_TEST_CONTINUE") == "1"; int failures = 0;
void Check(bool ok, string label) { checks++; if (!ok) { failures++; Console.Error.WriteLine("FAIL: " + label); if (!keepGoing) throw new Exception(label); } }

// ---------------------------------------------------------------------------------------------------------------------
// 1. The native rule that causes the bug, read from the real assembly (not from a double).
// ---------------------------------------------------------------------------------------------------------------------
string gameDll = args.Length > 0 ? args[0] : null, blueprintZip = args.Length > 1 ? args[1] : null, modDll = args.Length > 2 ? args[2] : null;
if (gameDll != null)
{
    var game = AssemblyDefinition.ReadAssembly(gameDll);
    var progressionType = game.MainModule.GetType("Kingmaker.Blueprints.Classes.BlueprintProgression");
    var calc = progressionType.Methods.Single(m => m.Name == "CalcLevel").Body.Instructions.Take(12).Select(i => i.OpCode.Code + " " + (i.Operand as MemberReference)?.Name).ToArray();
    Check(calc[0] == "Ldarg_0 " && calc[1] == "Ldfld m_Classes" && calc[2].StartsWith("Call Empty") && calc[3].StartsWith("Brfalse")
        && calc[4] == "Ldarg_0 " && calc[5] == "Ldfld m_Archetypes" && calc[6].StartsWith("Call Empty") && calc[7].StartsWith("Brfalse")
        && calc[8].StartsWith("Ldarg_1") && calc[9] == "Ldfld Progression" && calc[10] == "Callvirt get_CharacterLevel" && calc[11].StartsWith("Ret"),
        "native CalcLevel: no classes and no archetypes -> returns the CHARACTER level (real IL)");
    var update = game.MainModule.GetType("Kingmaker.UnitLogic.Class.LevelUp.Actions.LevelUpHelper").Methods.Single(m => m.Name == "UpdateProgression");
    var updateRefs = update.Body.Instructions.Select(i => (i.Operand as MemberReference)?.Name).Where(n => n != null).ToArray();
    Check(updateRefs.Contains("CalcLevel") && updateRefs.Contains("set_Level") && updateRefs.Contains("GiveFeaturesForPreviousLevels") && updateRefs.Contains("GetLevelEntry"),
        "native UpdateProgression grants the level entries between the stored level and CalcLevel (real IL)");
    var apply = game.MainModule.GetType("Kingmaker.UnitLogic.Class.LevelUp.Actions.ApplyClassMechanics").Methods.Single(m => m.Name == "ApplyProgressions");
    Check(apply.Body.Instructions.Any(i => (i.Operand as MemberReference)?.Name == "UpdateProgression"),
        "every level-up (of any class) re-runs UpdateProgression for every progression the unit has (real IL)");
}
else skipped++;

// Native progressions that the mod's design follows: one class each (real blueprints).
if (blueprintZip != null)
{
    using var zip = ZipFile.OpenRead(blueprintZip);
    foreach (var (file, classGuid) in new[] { ("CavalierProgression.jbp", "3adc3439f98cb534ba98df59838f02c7"), ("BardProgression.jbp", "772c83a25e2268e448e841dcd548235f"), ("BloodragerProgression.jbp", "d77e67a814d686842802c9cfd8ef8499") })
    {
        var entry = zip.Entries.First(e => e.FullName.EndsWith("/" + file) || e.FullName == file);
        using var reader = new StreamReader(entry.Open());
        var data = JsonDocument.Parse(reader.ReadToEnd().TrimStart('﻿')).RootElement.GetProperty("Data");
        var classes = data.GetProperty("m_Classes").EnumerateArray().ToArray();
        Check(classes.Length == 1 && classes[0].GetProperty("m_Class").GetString().EndsWith(classGuid), $"native {file} lists exactly its class in m_Classes");
        Check(data.GetProperty("m_Archetypes").GetArrayLength() == 0 && !data.GetProperty("ForAllOtherClasses").GetBoolean(), $"native {file} uses no archetype/all-class levels");
    }
}
else skipped++;

// ---------------------------------------------------------------------------------------------------------------------
// 2. Production MediumProgression.Configure on the API model + the native level-up rules.
// ---------------------------------------------------------------------------------------------------------------------
var medium = new BlueprintCharacterClass { name = "Medium", AssetGuid = Guids.Medium };
BlueprintTool.Assets[Guids.Medium] = medium;
var names = typeof(Guids).GetFields(BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public).Where(f => f.IsLiteral).ToDictionary(f => (string)f.GetRawConstantValue(), f => f.Name);
foreach (var multiRank in new[] { Guids.MediumSpiritBonus, Guids.SpiritPower, Guids.SpiritSurge })
    BlueprintTool.Assets[multiRank] = new BlueprintFeature { name = names[multiRank], AssetGuid = multiRank, Ranks = 10 };
var binderMarker = new BlueprintFeature { name = "BinderMarker", AssetGuid = Guids.SpiritBinderFeature };
var binderSelection = new BlueprintFeatureSelection { name = "BinderBonusFeat", AssetGuid = Guids.SpiritBinderBonusFeat };
BlueprintTool.Assets[Guids.SpiritBinderFeature] = binderMarker; BlueprintTool.Assets[Guids.SpiritBinderBonusFeat] = binderSelection;
var progression = MediumProgression.Configure();
medium.Progression = progression;
string N(BlueprintScriptableObject b) => names.GetValueOrDefault(b.AssetGuid ?? "") ?? b.name;

// A second class with a normal progression (one class, one feature per level).
var knight = new BlueprintCharacterClass { name = "Knight", AssetGuid = "knight" };
var knightProgression = new BlueprintProgression { name = "KnightProgression", AssetGuid = "knight-progression", Classes = { knight } };
knightProgression.LevelEntries = Enumerable.Range(1, 20).Select(l => new LevelEntry { Level = l, Features = { new BlueprintFeature { name = "Knight" + l, AssetGuid = "knight-" + l } } }).ToArray();
knight.Progression = knightProgression;

var selectionsRequested = new List<(BlueprintScriptableObject Feature, int Level)>();
// Native LevelUpHelper.UpdateProgression / AddFeaturesFromProgression (prerequisites are irrelevant here).
void UpdateProgression(UnitEntityData unit, BlueprintProgression p)
{
    var data = unit.Descriptor.Progression.SureProgressionData(p);
    int old = data.Level; int now = data.Level = p.CalcLevel(unit.Descriptor);
    if (old < now)
    {
        if (!p.GiveFeaturesForPreviousLevels) old = now - 1;
        for (int i = old + 1; i <= now; i++)
            foreach (var feature in data.GetLevelEntry(i).Features.OfType<BlueprintFeature>())
            {
                if (feature is IFeatureSelection) selectionsRequested.Add((feature, i));
                var fact = unit.AddFact(feature);
                if (feature is BlueprintProgression nested) UpdateProgression(unit, nested);
                fact.SetSource(p, i);
            }
    }
}
// Native ApplyClassMechanics.ApplyProgressions after the class level was added.
void LevelUp(UnitEntityData unit, BlueprintCharacterClass cls)
{
    var pr = unit.Descriptor.Progression;
    pr.ClassLevels[cls] = pr.GetClassLevel(cls) + 1;
    if (pr.GetClassLevel(cls) == 1) unit.AddFact(cls.Progression).SetSource(cls, 1);
    foreach (var p in pr.Features.Enumerable.Select(f => f.Blueprint).OfType<BlueprintProgression>().ToList())
    {
        if (pr.GetClassLevel(cls) <= 1 && p.Classes.Contains(cls)) pr.Features.Enumerable.First(f => f.Blueprint == p).SetSource(cls, 1);
        UpdateProgression(unit, p);
    }
}
IEnumerable<(int Level, BlueprintFeature Feature)> PlainEntries(int upTo, ProgressionData data) =>
    Enumerable.Range(1, upTo).SelectMany(l => data.GetLevelEntry(l).Features.Where(f => !(f is IFeatureSelection)).Select(f => (l, (BlueprintFeature)f)));
// Expected medium features: the entries up to the MEDIUM level, ranks capped by the blueprint.
Dictionary<string, int> Expected(int mediumLevel, ProgressionData data) =>
    PlainEntries(mediumLevel, data).GroupBy(e => N(e.Feature)).ToDictionary(g => g.Key, g => Math.Min(g.Count(), g.First().Feature.Ranks));
Dictionary<string, int> Held(UnitEntityData unit) =>
    unit.Descriptor.Progression.Features.Enumerable.Where(f => f.Blueprint != progression && f.Blueprint != knightProgression && !f.Blueprint.name.StartsWith("Knight") && f.Blueprint is not IFeatureSelection)
        .ToDictionary(f => N(f.Blueprint), f => f.Rank);
string Show(Dictionary<string, int> d) => string.Join(", ", d.OrderBy(k => k.Key).Select(k => k.Key + "x" + k.Value));
bool SameFeatures(Dictionary<string, int> a, Dictionary<string, int> b) => Show(a) == Show(b);
string Snapshot(UnitEntityData unit) => string.Join("|", unit.Descriptor.Progression.Features.Enumerable.OrderBy(f => N(f.Blueprint))
    .Select(f => $"{N(f.Blueprint)}:{f.Rank}:{f.SourceLevel}:{string.Join(",", f.RankToSource?.Select(s => N(s.Blueprint) + "@" + s.Level) ?? new string[0])}"))
    + "|L=" + unit.Descriptor.Progression.GetProgression(progression)?.Level;

UnitEntityData Build(string order, bool oldBug = false, Action<UnitEntityData> before = null)
{
    var unit = new UnitEntityData();
    before?.Invoke(unit);
    var saved = progression.Classes.ToList();
    if (oldBug) progression.Classes.Clear();          // 0.3.3: MediumProgression had no class
    foreach (char c in order) LevelUp(unit, c == 'M' ? medium : knight);
    progression.Classes.Clear(); progression.Classes.AddRange(saved);
    return unit;
}
void AssertInvariant(UnitEntityData unit, string label)
{
    int m = unit.Descriptor.Progression.GetClassLevel(medium);
    if (m == 0) { Check(Held(unit).Count == 0, label + ": no Medium level, no Medium feature"); return; }
    var data = unit.Descriptor.Progression.GetProgression(progression);
    Check(SameFeatures(Expected(m, data), Held(unit)), $"{label}: at Medium level {m} exactly the entries up to level {m} are held [want {Show(Expected(m, data))}] [have {Show(Held(unit))}]");
    Check(data.Level == m, $"{label}: stored progression level is the Medium level {m}, not {data.Level}");
}

// 2a. The production configuration.
Check(progression.Classes.Count == 1 && progression.Classes[0] == medium, "MediumProgression lists the Medium class (so CalcLevel uses the Medium class level)");

// 2b. The bug case from the report, level by level.
{
    var unit = Build("K");
    Check(Held(unit).Count == 0, "Knight 1 alone holds no Medium feature");
    LevelUp(unit, medium);
    var want = Expected(1, unit.Descriptor.Progression.GetProgression(progression));
    Check(SameFeatures(want, Held(unit)), $"Knight 1 -> Medium 1 (total level 2) receives the whole Medium level-1 kit, not only Shared Seance [want {Show(want)}] [have {Show(Held(unit))}]");
    Check(!Held(unit).ContainsKey("MediumSharedSeance"), "and no level-2 entry");
    LevelUp(unit, medium);
    Check(Held(unit).ContainsKey("MediumSharedSeance"), "Medium 2 grants Shared Seance");
    var seance = unit.Descriptor.Progression.Features.Enumerable.Single(f => N(f.Blueprint) == "MediumSharedSeance");
    Check(seance.Rank == 1, "Shared Seance once");
    AssertInvariant(unit, "Knight 1, Medium 2");
    var beforeKnight = Snapshot(unit);
    LevelUp(unit, knight); LevelUp(unit, knight); LevelUp(unit, knight); LevelUp(unit, knight); LevelUp(unit, knight);
    Check(Held(unit).Count == Held(Build("KMM")).Count && SameFeatures(Held(unit), Held(Build("KMM"))), "five more Knight levels do not advance the Medium progression");
}
{
    var unit = Build("KKKKKKK");
    LevelUp(unit, medium);
    AssertInvariant(unit, "Knight 7 -> Medium 1");
    Check(Held(unit).ContainsKey("MediumProficiencies") && Held(unit).ContainsKey("SpiritSurge") && Held(unit).ContainsKey("MediumChannelSpirit") && Held(unit).ContainsKey("MediumInfluence"),
        "Knight 7 -> Medium 1 starts at Medium level 1 (proficiencies, surge, channel, influence)");
    Check(Held(unit)["MediumSpiritBonus"] == 1 && Held(unit)["SpiritPower"] == 1, "one rank of spirit bonus and power, not the level-8 rank instead");
}
// 2c. Pure Medium 1..20, plus every position of the first Medium level among 20 levels, then all Medium.
foreach (int lead in Enumerable.Range(0, 20))
{
    var unit = Build(new string('K', lead));
    for (int level = 1; level + lead <= 20; level++) { LevelUp(unit, medium); AssertInvariant(unit, $"{lead} Knight levels then Medium {level}"); }
}
{
    var unit = Build("");
    var granted = new List<string>();
    for (int level = 1; level <= 20; level++)
    {
        var before = Held(unit); LevelUp(unit, medium); var after = Held(unit);
        var entry = progression.LevelEntries.Where(e => e.Level == level).SelectMany(e => e.Features).Select(N).ToArray();
        foreach (var name in entry) Check(after.GetValueOrDefault(name) == before.GetValueOrDefault(name) + 1 || (after[name] == before.GetValueOrDefault(name) && BlueprintTool.Assets.Values.OfType<BlueprintFeature>().First(f => N(f) == name).Ranks == 1), $"Medium {level} grants {name}");
        Check(after.Sum(k => k.Value) - before.Sum(k => k.Value) >= entry.Count(n => !before.ContainsKey(n) || after[n] > before[n]) - 0, "no extra rank appears");
    }
    Check(Held(unit)["MediumSpiritBonus"] == 6 && Held(unit)["SpiritPower"] == 4 && Held(unit)["SpiritSurge"] == 3, "pure Medium 20: spirit bonus x6, power x4, surge x3 (no duplicates)");
}
// 2d. Random interleavings (fixed seed): other-class levels never move the Medium progression.
{
    var rng = new Random(20260930);
    for (int run = 0; run < 300; run++)
    {
        var unit = Build("");
        for (int level = 1; level <= 20; level++)
        {
            LevelUp(unit, rng.Next(3) == 0 ? medium : knight);
            AssertInvariant(unit, $"random run {run} level {level}");
        }
    }
}
// 2e. Spirit Binder: extra feat choices come at Medium levels 2/3/5/7/9/11, never by total level.
{
    void Binder(UnitEntityData unit)
    {
        var data = unit.Descriptor.Progression.SureProgressionData(progression);
        data.ArchetypeAdds.Add(new LevelEntry { Level = 1, Features = { binderMarker } });
        foreach (int l in new[] { 2, 3, 5, 7, 9, 11 }) data.ArchetypeAdds.Add(new LevelEntry { Level = l, Features = { binderSelection } });
    }
    selectionsRequested.Clear();
    var unit = Build("KKKKK", before: Binder);
    Check(selectionsRequested.Count == 0, "Binder: no bonus feat before any Medium level");
    LevelUp(unit, medium);
    Check(Held(unit).ContainsKey("SpiritBinderFeature") && selectionsRequested.Count == 0, "Binder Medium 1: marker, no bonus feat yet");
    LevelUp(unit, medium); LevelUp(unit, medium);
    Check(selectionsRequested.Select(s => s.Level).SequenceEqual(new[] { 2, 3 }), "Binder Medium 2 and 3 offer one bonus feat each");
    for (int i = 0; i < 4; i++) LevelUp(unit, knight);
    Check(selectionsRequested.Count == 2, "Knight levels offer no Binder feat");
    for (int i = 4; i <= 11; i++) LevelUp(unit, medium);
    Check(selectionsRequested.Select(s => s.Level).SequenceEqual(new[] { 2, 3, 5, 7, 9, 11 }), "Binder feats at exactly 2/3/5/7/9/11");
    AssertInvariant(unit, "Binder run");
}

// ---------------------------------------------------------------------------------------------------------------------
// 3. Characters created by 0.3.3 (progression without class): recovery, idempotence and what it must not touch.
// ---------------------------------------------------------------------------------------------------------------------
void Recover(UnitEntityData unit) => MediumProgressionMigration.Run(unit.Descriptor);
foreach (var order in new[] { "KM", "KKKKKKKM", "KMKKM", "KKKMMKKKKM", "MKKKKKKKKKKKKKKKKKKM".Substring(0, 12), "KKKKKKKKKKKKKKKKKKKM", "KKKKKKKKKKKKKKKKKKMM" })
{
    var affected = Build(order, oldBug: true);
    int m = order.Count(c => c == 'M');
    var dataBefore = affected.Descriptor.Progression.GetProgression(progression);
    bool affectedState = !SameFeatures(Expected(m, dataBefore), Held(affected)) || dataBefore.Level != m;
    Check(affectedState, $"[{order}] the 0.3.3 state is really wrong (model check)");
    Recover(affected);
    AssertInvariant(affected, $"[{order}] after recovery");
    var recovered = Snapshot(affected);
    Recover(affected);
    Check(recovered == Snapshot(affected), $"[{order}] recovery is idempotent");
    // From here on the character behaves like a new one.
    int knightLevels = order.Count(c => c == 'K');
    for (int i = m; i < 20 - knightLevels; i++) { LevelUp(affected, medium); AssertInvariant(affected, $"[{order}] recovered character, Medium {i + 1}"); }
    LevelUp(affected, knight);
    AssertInvariant(affected, $"[{order}] recovered character keeps its progression");
}
{
    // The reported case in detail: Knight 1 + Medium 1 saved by 0.3.3 holds only Shared Seance.
    var unit = Build("KM", oldBug: true);
    Check(Show(Held(unit)) == "MediumSharedSeancex1", "0.3.3 model: Knight 1 + Medium 1 holds only Shared Seance (the reported symptom)");
    Recover(unit);
    Check(!Held(unit).ContainsKey("MediumSharedSeance") && Held(unit).ContainsKey("SpiritSurge") && Held(unit).ContainsKey("MediumChannelSpirit"),
        "recovery grants the level-1 kit and takes back the level-2 entry granted early");
    LevelUp(unit, medium);
    Check(Held(unit).ContainsKey("MediumSharedSeance"), "Medium 2 then grants Shared Seance again, once");
    // An unaffected (correct) character is left exactly as it is.
    var good = Build("KKMM"); var snap = Snapshot(good); Recover(good);
    Check(snap == Snapshot(good), "a correctly progressed character is not modified");
    var none = Build("KKK"); var noneSnap = Snapshot(none); Recover(none);
    Check(noneSnap == Snapshot(none), "a character without Medium levels is not modified");
}
{
    // Ranks with another recorded source (e.g. the Prowler's Bloodrager progression) are never counted as surplus or missing.
    var other = new BlueprintProgression { name = "BloodragerProgression", AssetGuid = "bloodrager-progression" };
    var unit = Build("KKKKKKKM", oldBug: true, before: u => { });
    var bonus = BlueprintTool.Get<BlueprintFeature>(Guids.MediumSpiritBonus);
    var extra = unit.AddFact(bonus); extra.SetSource(other, 1);
    int ranksBefore = extra.Rank;
    Recover(unit);
    var data = unit.Descriptor.Progression.GetProgression(progression);
    var fact = unit.Descriptor.Progression.Features.Enumerable.Single(f => f.Blueprint == bonus);
    Check(fact.RankToSource.Count(s => s.Blueprint == other) == 1, "a rank from another source survives the recovery");
    Check(fact.RankToSource.Count(s => s.Blueprint == progression) == 1, "and Medium keeps exactly its own rank");
    Check(fact.Rank == 2, "the Prowler-style rank is additional to the Medium rank");
    var again = Snapshot(unit); Recover(unit); Check(again == Snapshot(unit), "still idempotent with a foreign rank");
    // Very old saves without per-rank sources: left alone (reported), single-rank features are still repaired.
    var old = Build("KKKKKKKM", oldBug: true);
    foreach (var f in old.Descriptor.Progression.Features.Enumerable.Where(f => f.Blueprint.Ranks > 1)) f.ForgetRankSources();
    var untouched = Show(Held(old).Where(k => k.Key == "MediumSpiritBonus").ToDictionary(k => k.Key, k => k.Value));
    Recover(old);
    Check(Show(Held(old).Where(k => k.Key == "MediumSpiritBonus").ToDictionary(k => k.Key, k => k.Value)) == untouched, "multi-rank features without a rank-source record are not modified");
    Check(Held(old).ContainsKey("MediumChannelSpirit"), "single-rank kit is repaired even then");
    Check(UnityModManagerNet.UnityModManager.ModEntry.ModLogger.Lines.Any(l => l.Contains("no complete per-rank source record")), "and the skipped feature is reported");
}
{
    // Binder selections cannot be granted or revoked outside level-up: untouched, reported.
    void Binder(UnitEntityData unit)
    {
        var data = unit.Descriptor.Progression.SureProgressionData(progression);
        data.ArchetypeAdds.Add(new LevelEntry { Level = 1, Features = { BlueprintTool.Get<BlueprintFeature>(Guids.SpiritBinderFeature) } });
        foreach (int l in new[] { 2, 3, 5, 7, 9, 11 }) data.ArchetypeAdds.Add(new LevelEntry { Level = l, Features = { BlueprintTool.Get<BlueprintFeature>(Guids.SpiritBinderBonusFeat) } });
    }
    var unit = Build("KKKMM", oldBug: true, before: Binder);
    selectionsRequested.Clear();
    Recover(unit);
    Check(Held(unit).ContainsKey("SpiritBinderFeature"), "Binder: the level-1 marker is granted by the recovery");
    Check(selectionsRequested.Count == 0, "the recovery does not request or grant bonus-feat selections");
    Check(UnityModManagerNet.UnityModManager.ModEntry.ModLogger.Lines.Any(l => l.Contains("feature selection(s)")), "and it says so");
}
{
    // Regression (found in review): the native SelectFeature.Apply records the PROGRESSION as the source of feats picked
    // through its selections (feature.SetSource(selectionState.Source, level)), and the selection fact itself carries it too.
    // Those picks are not part of any level entry and must survive every load, on new and on 0.3.3 characters alike.
    var powerAttack = new BlueprintFeature { name = "PickedFeat", AssetGuid = "picked-feat" };
    var multiPick = new BlueprintFeature { name = "PickedMultiRank", AssetGuid = "picked-multi", Ranks = 5 };
    void Binder(UnitEntityData unit)
    {
        var data = unit.Descriptor.Progression.SureProgressionData(progression);
        data.ArchetypeAdds.Add(new LevelEntry { Level = 1, Features = { BlueprintTool.Get<BlueprintFeature>(Guids.SpiritBinderFeature) } });
        foreach (int l in new[] { 2, 3, 5, 7, 9, 11 }) data.ArchetypeAdds.Add(new LevelEntry { Level = l, Features = { BlueprintTool.Get<BlueprintFeature>(Guids.SpiritBinderBonusFeat) } });
    }
    foreach (bool oldBug in new[] { false, true })
    {
        var unit = Build("KKKMMM", oldBug, before: Binder);
        unit.AddFact(powerAttack).SetSource(progression, 2);
        unit.AddFact(multiPick).SetSource(progression, 3); unit.AddFact(multiPick).SetSource(progression, 3);
        Recover(unit);
        var facts = unit.Descriptor.Progression.Features.Enumerable;
        Check(facts.Any(f => f.Blueprint == powerAttack && f.Rank == 1), $"[oldBug={oldBug}] a feat picked through the Binder selection survives the load");
        Check(facts.Any(f => f.Blueprint == multiPick && f.Rank == 2 && f.RankToSource.Count == 2), $"[oldBug={oldBug}] a multi-rank pick keeps all its ranks and their sources");
        Check(facts.Any(f => f.Blueprint == binderSelection), $"[oldBug={oldBug}] the selection fact itself is kept");
        var after = Snapshot(unit); Recover(unit);
        Check(after == Snapshot(unit), $"[oldBug={oldBug}] and the recovery stays idempotent with picks present");
    }
    var fresh = Build("KKKMMM", false, before: Binder);
    fresh.AddFact(powerAttack).SetSource(progression, 2);
    var freshBefore = Snapshot(fresh); Recover(fresh);
    Check(freshBefore == Snapshot(fresh), "a correct new character with picks is not modified by the load-time recovery");
}
{
    // Regression (found in review): ranks the game would refuse (cap reached by other sources) must not be planned again on every load,
    // and must not steal the fact's primary source.
    var other = new BlueprintProgression { name = "BloodragerProgression", AssetGuid = "bloodrager-progression" };
    var unit = Build("", false);
    unit.Descriptor.Progression.ClassLevels[medium] = 4;
    unit.Descriptor.Progression.SureProgressionData(progression);
    var bonus = BlueprintTool.Get<BlueprintFeature>(Guids.MediumSpiritBonus);
    for (int i = 0; i < bonus.Ranks; i++) unit.AddFact(bonus).SetSource(other, i + 1);
    Recover(unit);
    var fact = unit.Descriptor.Progression.Features.Enumerable.Single(f => f.Blueprint == bonus);
    Check(fact.Rank == bonus.Ranks, "a feature already at its rank cap gains no rank");
    Check(fact.SourceProgression == other, "and the recovery does not take over its primary source");
    var full = Snapshot(unit); Recover(unit);
    Check(full == Snapshot(unit), "and the plan is not repeated on the next load");
    Check(unit.Descriptor.Progression.Features.Enumerable.Any(f => N(f.Blueprint) == "MediumChannelSpirit"), "the rest of the missing kit is still granted");
}
{
    // Failure isolation: a broken unit never throws into the load.
    MediumProgressionMigration.Run(null);
    MediumProgressionMigration.Run(new UnitDescriptor());
    Check(true, "null and empty descriptors are ignored");
}

// ---------------------------------------------------------------------------------------------------------------------
// Regression: native SetSource can fill several rank records with the same source and level.
// Removing one surplus rank must keep one record for every retained rank, including foreign sources.
foreach (bool foreignRank in new[] { false, true })
{
    var unit = Build("KKKKKKKM", oldBug: true);
    var bonus = BlueprintTool.Get<BlueprintFeature>(Guids.MediumSpiritBonus);
    unit.AddFact(bonus).SetSource(progression, 8);
    var other = new BlueprintProgression { name = "BloodragerProgression", AssetGuid = "duplicate-foreign" };
    if (foreignRank) unit.AddFact(bonus).SetSource(other, 3);
    var before = unit.Descriptor.Progression.Features.Enumerable.Single(f => f.Blueprint == bonus);
    Check(before.RankToSource.Count(s => s.Blueprint == progression && s.Level == 8) == 2,
        "duplicate-source fixture starts with two equally recorded Medium ranks");
    Recover(unit);
    var after = unit.Descriptor.Progression.Features.Enumerable.Single(f => f.Blueprint == bonus);
    Check(after.Rank == (foreignRank ? 2 : 1), "duplicate-source migration removes exactly one excess rank");
    Check(after.RankToSource.Count == after.Rank, "duplicate-source migration keeps one record per retained rank");
    Check(after.RankToSource.Count(s => s.Blueprint == progression) == 1,
        "duplicate-source migration preserves the remaining Medium rank's source");
    if (foreignRank) Check(after.RankToSource.Count(s => s.Blueprint == other) == 1 && after.SourceProgression == other,
        "duplicate-source migration preserves the foreign rank and primary source");
    var snapshot = Snapshot(unit); Recover(unit);
    Check(snapshot == Snapshot(unit), "duplicate-source migration stays tracked and idempotent");
}

// 0.3.5: model an otherwise valid 0.3.4 save which lacks only the new casting feature.
{
    var unit = Build("KMM");
    var casting = BlueprintTool.Get<BlueprintFeature>(Guids.MediumPsychicCastingFeature);
    var fresh = unit.Descriptor.Progression.Features.Enumerable.Single(f => f.Blueprint == casting);
    Check(fresh.Rank == 1 && fresh.SourceLevel == 1, "psychic casting feature granted once at Medium level one in a multiclass build");
    unit.RemoveFact(casting);
    var previous = Held(unit);
    Recover(unit);
    var restored = unit.Descriptor.Progression.Features.Enumerable.Single(f => f.Blueprint == casting);
    Check(restored.Rank == 1 && restored.SourceProgression == progression && restored.SourceLevel == 1,
        "valid 0.3.4 save receives new casting feature with the correct source");
    var withoutCasting = Held(unit).Where(k => k.Key != "MediumPsychicCastingFeature").ToDictionary(k => k.Key, k => k.Value);
    Check(SameFeatures(previous, withoutCasting), "adding psychic casting preserves every other feature and rank");
    var snapshot = Snapshot(unit); Recover(unit);
    Check(snapshot == Snapshot(unit), "new casting feature recovery remains idempotent");
}

// 4. The delivery DLL: the progression is configured with its class and the restore entry runs the recovery first.
// ---------------------------------------------------------------------------------------------------------------------
if (modDll != null)
{
    var mod = AssemblyDefinition.ReadAssembly(modDll);
    var configure = mod.MainModule.GetType("MediumClass.Medium.MediumProgression").Methods.Single(m => m.Name == "Configure");
    var calls = configure.Body.Instructions.Select(i => (i.Operand as MemberReference)?.Name).ToArray();
    Check(calls.Contains("SetClasses") || calls.Contains("AddToClasses"), "delivery DLL: MediumProgression.Configure sets the progression's class");
    var restore = mod.MainModule.GetType("MediumClass.Medium.MediumChannelRestore").Methods.Single(m => m.Name == "Restore");
    var order = restore.Body.Instructions.Select(i => (i.Operand as MemberReference)?.Name).Where(n => n != null).ToList();
    Check(order.Contains("Run") && order.IndexOf("Run") < order.IndexOf("RemoveLegacyBuffs"), "delivery DLL: the restore entry runs the progression recovery first");
    var patch = mod.MainModule.GetType("MediumClass.Medium.MediumChannelRestorePatch");
    Check(patch != null && patch.CustomAttributes.Any(a => a.AttributeType.Name == "HarmonyPatch"), "delivery DLL: the restore runs from the UnitDescriptor.ApplyPostLoadFixes patch");
}
else skipped++;
if (failures > 0) { Console.Error.WriteLine($"{failures} of {checks} checks failed"); return 1; }
Console.WriteLine($"PASS: {checks} checks (native IL/blueprint anchors, production MediumProgression + recovery on the native level-up model); {skipped} optional input(s) skipped; not Unity gameplay.");
return 0;

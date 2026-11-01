using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.Serialization;
using HarmonyLib;
using Newtonsoft.Json.Linq;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Selection;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.Blueprints.Facts;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.UI.MVVM._VM.CharGen.Phases.Spells;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Class.LevelUp;
using Kingmaker.UnitLogic.Class.LevelUp.Actions;
using CallOfTheWild.Wrath.Prototype;

// Executes the native class/book progression and spell-choice data paths with
// actual archive metadata. Only native selector text/icon rendering endpoints
// are replaced; no spell list, known-slot, Check, Apply or learning body is.
internal static class TimeStopSpellbook01438Tests
{
    const string Wizard = "ba34257984f4c41408ce1dc2004e342e", Sorcerer = "b3a505fb61437dc4097f43c3f8f9a4cf", Arcanist = "52dbfd8505e22f84fad8d702611f60b7", Psychic = "83fa3b50138c566698801ba5967bbde0";
    const string WizardList = "ba0401fdeb4062f40a7aa95b6f07fe89", PsychicList = "6a4b9d6651b155058004354a803da3bf";
    static readonly string[] NativeBooks = { "5a38c9ac8607890409fcb8f6342da6f4", "b3db3766a4b605040b366265e2af0e50", "33903fe5c4abeaa45bc249adb9d98848" };
    static readonly string[] Classes = { Wizard, Sorcerer, Arcanist, Psychic };
    static readonly string[] Books = NativeBooks.Concat(new[] { "3ce26d41e96b5ca289f0c6ce9db28d3c" }).ToArray();
    static readonly int[] NinthLevel = { 17, 18, 18, 18 };
    static readonly string[] Names = { "Wizard", "Sorcerer", "Arcanist", "Psychic" };
    static readonly List<JObject> rows = new List<JObject>();
    static readonly List<Tuple<object, FieldInfo, object>> metadata = new List<Tuple<object, FieldInfo, object>>();
    static int checks, nativeProgressions, nativeChecks, nativeApplies, nativeChoices, nativeMemorizations, nativeSlotRefreshes, nativeRests, nativeSpends;
    static int displayNames, displayIcons, displaySchools;
    static Type Module => typeof(ForestBlessing01435).Assembly.GetType("CallOfTheWild.Wrath.Prototype.TimeStop01438", true);
    static object Get(object o, string n) => (AccessTools.Field(o.GetType(), n) ?? AccessTools.Field(o.GetType(), "<" + n + ">k__BackingField")).GetValue(o);
    static void Set(object o, string n, object v) => (AccessTools.Field(o.GetType(), n) ?? AccessTools.Field(o.GetType(), "<" + n + ">k__BackingField")).SetValue(o, v);
    static T Empty<T>() where T : class => (T)FormatterServices.GetUninitializedObject(typeof(T));
    static T Native<T>(BlueprintsCache c, string id) where T : SimpleBlueprint => c.Load(BlueprintGuid.Parse(id)) as T ?? throw new Exception("038 spellbook missing actual asset " + id);
    static BlueprintGuid Id(string token) => (BlueprintGuid)AccessTools.Method(Module.Assembly.GetType("CallOfTheWild.Wrath.Prototype.PsychicAssets"), "Id").Invoke(null, new object[] { token });
    static void Check(bool ok, string reason) { if (!ok) throw new Exception("TimeStopSpellbook01438: " + reason); checks++; }
    static JObject NativeRow(string id)
    {
        var matches = PsychicExpansionTests.NativeRows.Where(r => (string)r["blueprint"]["AssetId"] == id).ToArray();
        if (matches.Length != 1) throw new Exception("038 actual archive spellbook dependency must occur once " + id);
        return (JObject)matches[0]["blueprint"]["Data"];
    }
    static void RestoreField(object bp, FieldInfo field, object value)
    {
        if (!metadata.Any(m => ReferenceEquals(m.Item1, bp) && m.Item2 == field)) metadata.Add(Tuple.Create(bp, field, field.GetValue(bp)));
        field.SetValue(bp, value);
    }
    static void Hydrate(BlueprintsCache cache)
    {
        // Existing harness stubs omit many book/table fields. Rehydrate exactly
        // from real NativeRows, then restore original references in finally.
        foreach (string id in NativeBooks) {
            var bp = Native<BlueprintSpellbook>(cache, id); var data = NativeRow(id);
            foreach (var p in data.Properties()) {
                if (p.Name == "Name" || p.Name == "Components" || p.Name == "$type") continue;
                var field = AccessTools.Field(bp.GetType(), p.Name);
                if (field != null) RestoreField(bp, field, PsychicExpansionTests.Read(p.Value, field.FieldType));
            }
        }
        foreach (var bp in NativeBooks.Select(id => Native<BlueprintSpellbook>(cache, id))) {
            foreach (var table in new[] { bp.SpellsPerDay, bp.SpellsKnown, bp.SpellSlots }.Where(t => t != null).Distinct())
                RestoreField(table, AccessTools.Field(table.GetType(), "Levels"), PsychicExpansionTests.Read(NativeRow(table.AssetGuid.ToString())["Levels"], typeof(SpellsLevelEntry[])));
        }
        for (int i = 0; i < 3; i++) {
            var cls = Native<BlueprintCharacterClass>(cache, Classes[i]); var field = AccessTools.Field(cls.GetType(), "m_Spellbook");
            RestoreField(cls, field, PsychicExpansionTests.Read(NativeRow(Classes[i])["m_Spellbook"], field.FieldType));
            Check(cls.Spellbook == Native<BlueprintSpellbook>(cache, Books[i]) && cls.Spellbook.CharacterClass == cls, "native class and book reciprocally identify " + Names[i]);
        }
        var psychic = Native<BlueprintSpellbook>(cache, Books[3]);
        Check(Native<BlueprintCharacterClass>(cache, Psychic).Spellbook == psychic && psychic.CharacterClass.AssetGuid == BlueprintGuid.Parse(Psychic), "actual registered Psychic class owns its private production book");
        Check(psychic.SpellsKnown == Native<BlueprintSpellbook>(cache, NativeBooks[1]).SpellsKnown && psychic.SpellsPerDay == Native<BlueprintSpellbook>(cache, NativeBooks[1]).SpellsPerDay, "production Psychic uses actual native sorcerer known/day tables");
        Check(Native<BlueprintSpellbook>(cache, Books[2]).IsArcanist && Native<BlueprintSpellbook>(cache, Books[2]).MemorizeSpells && Native<BlueprintSpellbook>(cache, Books[2]).Spontaneous, "native Arcanist hybrid flags and separate prepared table are retained");
    }
    static LevelUpState State(UnitEntityData unit, BlueprintCharacterClass cls, int nextLevel)
    {
        var state = Empty<LevelUpState>(); Set(state, "m_Unit", unit); Set(state, "Selections", new List<FeatureSelectionState>()); Set(state, "SpellSelections", new List<SpellSelectionData>());
        Set(state, "SelectedClass", cls); Set(state, "NextClassLevel", nextLevel); Set(state, "NextCharacterLevel", nextLevel); return state;
    }
    static Spellbook Progress(BlueprintsCache cache, int index, int level, out UnitEntityData unit, out LevelUpState state)
    {
        var cls = Native<BlueprintCharacterClass>(cache, Classes[index]); var bp = Native<BlueprintSpellbook>(cache, Books[index]);
        unit = (UnitEntityData)AccessTools.Method(typeof(TimeStutter01437Tests), "Unit").Invoke(null, new object[] { cache, "038-real-book-" + Names[index] + level, 20, 0 });
        unit.Progression.Classes.Clear(); unit.Progression.Classes.Add(new ClassData(cls) { Level = level });
        unit.Stats.Intelligence.BaseValue = unit.Stats.Charisma.BaseValue = 30;
        Set(unit.Descriptor, "m_Spellbooks", new Dictionary<BlueprintSpellbook, Spellbook>());
        var book = unit.Descriptor.DemandSpellbook(bp);
        // This is an existing character at its preceding book level. Only the
        // ordinary native level-up action generates the new known/extra slots.
        Set(book, "m_BaseLevelInternal", level - 1); state = State(unit, cls, level);
        var apply = new ApplySpellbook(); Check(apply.Check(state, unit.Descriptor), "native ApplySpellbook.Check " + Names[index] + level);
        apply.Apply(state, unit.Descriptor); nativeProgressions++;
        Check(book.BaseLevel == level && unit.Descriptor.DemandSpellbook(bp) == book, "native ApplySpellbook increments and keeps actual demanded book " + Names[index] + level);
        Check(book.Blueprint == cls.Spellbook && book.Blueprint.SpellList.AssetGuid == BlueprintGuid.Parse(index == 3 ? PsychicList : WizardList), "native selected class routes to intended list " + Names[index] + level);
        Check(!unit.Facts.List.Any(f => f.Blueprint.AssetGuid == Id("TimeStutterDiscovery")), "ordinary spell acquisition does not require Time Stutter " + Names[index] + level);
        return book;
    }
    static string DisplayName(BlueprintUnitFact b) => b.name;
    static UnityEngine.Sprite DisplayIcon(BlueprintUnitFact b) => null;
    static string DisplaySchool(SpellSchool school) => school.ToString();
    static IEnumerable<CodeInstruction> DisplayBoundary(IEnumerable<CodeInstruction> input)
    {
        var code = input.ToList(); int names = 0, icons = 0, schools = 0;
        for (int i = 0; i < code.Count; i++) {
            if (code[i].operand is MethodInfo m && m.DeclaringType == typeof(BlueprintUnitFact) && m.Name == "get_Name") { code[i].opcode = OpCodes.Call; code[i].operand = AccessTools.Method(typeof(TimeStopSpellbook01438Tests), nameof(DisplayName)); names++; }
            else if (code[i].operand is MethodInfo icon && icon.DeclaringType == typeof(BlueprintUnitFact) && icon.Name == "get_Icon") { code[i].opcode = OpCodes.Call; code[i].operand = AccessTools.Method(typeof(TimeStopSpellbook01438Tests), nameof(DisplayIcon)); icons++; }
            else if (code[i].operand is MethodInfo text && text.DeclaringType == typeof(Kingmaker.Blueprints.Root.LocalizedTexts) && text.Name == "get_Instance") {
                if (i + 4 >= code.Count || !(code[i + 1].operand is FieldInfo f) || f.Name != "SpellSchoolNames" || !(code[i + 3].operand is MethodInfo s) || s.DeclaringType != typeof(BlueprintAbility) || s.Name != "get_School" || !(code[i + 4].operand is MethodInfo get) || get.Name != "GetText") throw new Exception("038 native selector display IL changed");
                var labels = code[i].labels.Concat(code[i + 1].labels).ToArray(); code.RemoveRange(i, 2); code[i].labels.AddRange(labels); code[i + 2].opcode = OpCodes.Call; code[i + 2].operand = AccessTools.Method(typeof(TimeStopSpellbook01438Tests), nameof(DisplaySchool)); schools++;
            }
        }
        if (names != 1 || icons != 1 || schools != 1) throw new Exception("038 native selector must retain exact display boundary sites");
        displayNames = names; displayIcons = icons; displaySchools = schools; return code;
    }
    static bool Choice(Spellbook book, LevelUpState state, BlueprintAbility spell, int start, int end, bool known = false)
    {
        for (int level = start; level <= end; level++) Check(book.Blueprint.SpellList.GetSpells(level).All(s => s != null), "real native selection row resolves every reference " + book.Blueprint.name + " circle" + level);
        var phase = Empty<CharGenSpellsPhaseVM>(); Set(phase, "PreviewSpellbook", book); Set(phase, "StartLevel", start); Set(phase, "EndLevel", end); Set(phase, "MechanicSelectedSpells", Array.Empty<BlueprintAbility>());
        var controller = Empty<LevelUpController>(); Set(controller, "State", state);
        var choices = (List<CharGenSpellSelectorItemVM>)AccessTools.Method(typeof(CharGenSpellsPhaseVM), "CreateSpellVMsCollection").Invoke(phase, new object[] { controller, book.Blueprint.SpellList }); nativeChoices++;
        try { return choices.Count(v => v.Spell == spell && v.Level == 9 && v.HasInSpellbook == known) == 1; }
        finally { foreach (var v in choices) v.Dispose(); }
    }
    static bool Selectable(SelectSpell action, LevelUpState state, UnitEntityData unit) { nativeChecks++; return action.Check(state, unit.Descriptor); }
    static void Exercise(BlueprintsCache cache, int index, BlueprintAbility spell)
    {
        string name = Names[index]; int threshold = NinthLevel[index];
        var low = Progress(cache, index, threshold - 1, out var lowUnit, out var lowState);
        Check(low.MaxSpellLevel == 8 && low.GetSpellsPerDay(9) == 0, "real native progression has no ninth-circle slot one level early " + name);
        Check(!Selectable(new SelectSpell(low.Blueprint, low.Blueprint.SpellList, 9, spell, 0), lowState, lowUnit), "native generated selection denies ninth circle one class level early " + name);
        Check(!Choice(low, lowState, spell, low.MaxSpellLevel, low.MaxSpellLevel), "actual native UI data cannot offer ninth circle on lower book's maximum circle8 " + name);
        var book = Progress(cache, index, threshold, out var unit, out var state); var selection = state.GetSpellSelection(book.Blueprint, book.Blueprint.SpellList);
        Check(book.MaxSpellLevel == 9 && book.GetSpellsPerDay(9) > 0, "actual native ninth circle unlocks at real class threshold " + name);
        Check(!book.IsKnown(spell) && selection != null, "native progression produces a learning selection without auto-granting new spell " + name);
        Check(Choice(book, state, spell, 9, 9), "actual native CharGen collection offers new Time Stop on circle9 " + name);
        Check(!Choice(book, state, spell, 8, 8), "actual native CharGen collection does not misfile Time Stop on circle8 " + name);
        var action = new SelectSpell(book.Blueprint, book.Blueprint.SpellList, 9, spell, 0);
        Check(Selectable(action, state, unit), "native generated known/extra selection accepts ninth-circle Time Stop " + name);
        action.Apply(state, unit.Descriptor); nativeApplies++;
        Check(book.IsKnownOnLevel(spell, 9) && !book.IsKnownOnLevel(spell, 8), "native SelectSpell.Apply learns only ninth-circle spell " + name);
        var data = book.GetKnownSpells(9).Single(a => a.Blueprint == spell);
        Check(data.Spellbook == book && data.SpellLevel == 9, "learned native AbilityData owns exact real class spellbook " + name);
        Check(!Selectable(action, state, unit), "already spent native learning slot cannot be reused " + name);
        Check(Choice(book, state, spell, 9, 9, true), "actual native rebuilt collection marks known spell unavailable for new selection " + name);
        var stat = (ModifiableValueAttributeStat)unit.Stats.GetStat(book.Blueprint.CastingAttribute); stat.BaseValue = 19;
        bool prepared = book.Blueprint.MemorizeSpells;
        if (prepared) {
            Check(!book.CanSpend(data), "native known-only prepared/hybrid spell needs preparation and daily slots " + name);
            // The real spellbook window's switcher and Rest call this native
            // allocator. DemandSpellbook alone intentionally has no slots.
            book.UpdateAllSlotsSize(false); nativeSlotRefreshes++;
            Check(book.GetMemorizedSpellSlots(9).Any(), "native slot allocator uses real preparation table " + name);
            Check(book.IsPossibleMemorize(data, null) && book.Memorize(data, null), "actual native preparation accepts spell in real table-derived slot " + name); nativeMemorizations++;
            Check(book.IsMemorized(data), "native prepared collection records the learned spell " + name);
        } else Check(book.Blueprint.Spontaneous && !book.CanSpend(data), "native spontaneous book needs rest-generated daily slots " + name);
        book.Rest(); nativeRests++;
        stat.BaseValue = 18; Check(!book.CanSpend(data), "native casting attribute below19 rejects ninth-circle spell " + name);
        stat.BaseValue = 19; Check(book.CanSpend(data), "native casting attribute19 permits actual ninth-circle slot " + name);
        int before = book.Blueprint.Spontaneous ? book.GetSpontaneousSlots(9) : book.GetMemorizedSpellSlots(9).Count(s => s.Available && s.SpellShell?.Blueprint == spell);
        Check(before > 0 && book.Spend(data), "actual native real-book slot spend succeeds " + name); nativeSpends++;
        int after = book.Blueprint.Spontaneous ? book.GetSpontaneousSlots(9) : book.GetMemorizedSpellSlots(9).Count(s => s.Available && s.SpellShell?.Blueprint == spell);
        Check(after == before - 1, "actual native prepared/hybrid/spontaneous spend consumes exactly one real slot " + name);
        Check(unit.Resources.GetResourceAmount(Native<BlueprintAbilityResource>(cache, Id("TimeStutterResource").ToString())) == 0 && !unit.Facts.List.Any(f => f.Blueprint.AssetGuid == Id("TimeStutterDiscovery")), "player spell consumes no Time Stutter resource or discovery grant " + name);
        rows.Add(new JObject { ["class"] = name, ["classId"] = Classes[index], ["bookId"] = book.Blueprint.AssetGuid.ToString(), ["listId"] = book.Blueprint.SpellList.AssetGuid.ToString(), ["castingAttribute"] = book.Blueprint.CastingAttribute.ToString(), ["ninthCircleClassLevel"] = threshold, ["lowerClassLevel"] = threshold - 1, ["lowerMaxSpellLevel"] = low.MaxSpellLevel, ["maxSpellLevel"] = book.MaxSpellLevel, ["nativeSpontaneous"] = book.Blueprint.Spontaneous, ["nativeMemorizeSpells"] = prepared, ["nativeArcanist"] = book.Blueprint.IsArcanist, ["level9LearningSlots"] = selection.LevelCount[9]?.SpellSelections.Length ?? 0, ["extraLearningSlots"] = selection.ExtraSelected?.Length ?? 0, ["extraMaxLevel"] = selection.ExtraMaxLevel, ["beforeSpend"] = before, ["afterSpend"] = after });
    }
    internal static int RunNative(BlueprintsCache cache, Harmony h)
    {
        checks = nativeProgressions = nativeChecks = nativeApplies = nativeChoices = nativeMemorizations = nativeSlotRefreshes = nativeRests = nativeSpends = displayNames = displayIcons = displaySchools = 0; rows.Clear(); metadata.Clear();
        var ctor = AccessTools.Constructor(typeof(CharGenSpellSelectorItemVM), new[] { typeof(BlueprintAbility), typeof(Kingmaker.UI.MVVM._VM.InfoWindow.InfoSectionVM), typeof(int), typeof(int), typeof(bool) });
        var display = AccessTools.Method(typeof(TimeStopSpellbook01438Tests), nameof(DisplayBoundary));
        try {
            Hydrate(cache); h.Patch(ctor, transpiler: new HarmonyMethod(display));
            AccessTools.Method(Module, "Register").Invoke(null, new object[] { cache }); var spell = (BlueprintAbility)cache.Load(Id("TimeStopSpell01438"));
            for (int i = 0; i < Classes.Length; i++) Exercise(cache, i, spell);
            Check(nativeProgressions == 8 && nativeApplies == 4 && nativeChecks == 12 && nativeChoices == 16 && nativeMemorizations == 2 && nativeSlotRefreshes == 2 && nativeRests == 4 && nativeSpends == 4, "all claimed actual native book paths execute");
            Check(displayNames == 1 && displayIcons == 1 && displaySchools == 1, "only exact selector display text/icon endpoints are controlled");
            PsychicExpansionTests.Inventory["timeStopSpellbook01438Contracts"] = new JObject { ["passed"] = true, ["assertions"] = checks, ["additionalItemCount"] = 0, ["newBlueprints"] = new JArray(), ["nativeApplySpellbookCalls"] = nativeProgressions, ["nativeSelectSpellChecks"] = nativeChecks, ["nativeSelectSpellApplies"] = nativeApplies, ["nativeCharGenCollections"] = nativeChoices, ["nativeMemorizations"] = nativeMemorizations, ["nativePreparationSlotRefreshes"] = nativeSlotRefreshes, ["nativeRests"] = nativeRests, ["nativeSpellSlotSpends"] = nativeSpends, ["books"] = new JArray(rows), ["boundary"] = "Real archive Wizard/Sorcerer/Arcanist class/book flags and known/day/preparation tables plus the actual registered Psychic class/book run through DemandSpellbook, ApplySpellbook.Apply, native generated learning slots, SelectSpell.Check/Apply, CharGenSpellsPhaseVM.CreateSpellVMsCollection, UpdateAllSlotsSize/Memorize/Rest and native slot spending. The native preparation allocator is the same method called by SpellbookSwitcherVM.CreateEntities and Rest. Existing characters start at the preceding internal book level; no level9 row, learning slot or daily slot is fabricated. Native selector Name/Icon/school-text endpoints are controlled without replacing its collection or selection bodies, and units/clock/scene endpoints use the surrounding native test environment. This validates managed circle8/9 choice data and acquisition, not rendered windows, controller clicks, full character progression from level1 or save deserialization." };
            Console.WriteLine("PASS: " + checks + " TimeStopSpellbook01438 native book checks."); return checks;
        } finally { h.Unpatch(ctor, display); foreach (var item in metadata.AsEnumerable().Reverse()) item.Item2.SetValue(item.Item1, item.Item3); metadata.Clear(); }
    }
}

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using Newtonsoft.Json.Linq;
using Kingmaker;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.Controllers;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.RuleSystem;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components;
using Kingmaker.UnitLogic.Buffs;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Mechanics.Actions;
using Kingmaker.Utility;

// Runs inside the original037 actual native action/buff environment. The
// separate038 lifecycle suite executes original UpdateUnits/UpdateUnit filters.
internal static class TimeStopSpell01438Tests
{
    static readonly Type Module = typeof(CallOfTheWild.Wrath.Prototype.ForestBlessing01435).Assembly.GetType("CallOfTheWild.Wrath.Prototype.TimeStop01438", true);
    static readonly Type Factory = Module.Assembly.GetType("CallOfTheWild.Wrath.Prototype.PsychicAssets", true);
    static readonly string[] Tokens = { "TimeStopSpell01438", "TimeStopHost01438", "TimeStopArea01438", "TimeStopEffect01438" };
    const string WizardList = "ba0401fdeb4062f40a7aa95b6f07fe89", PsychicList = "6a4b9d6651b155058004354a803da3bf", Wizard = "ba34257984f4c41408ce1dc2004e342e";
    static int checks, casts, spends, cacheFaults, listFaults, foreignCases, diceSamples, metadataCases;
    static bool injectCache, injectLists;
    static object Get(object o, string n) => (AccessTools.Field(o.GetType(), n) ?? AccessTools.Field(o.GetType(), "<" + n + ">k__BackingField")).GetValue(o);
    static void Set(object o, string n, object v) => (AccessTools.Field(o.GetType(), n) ?? AccessTools.Field(o.GetType(), "<" + n + ">k__BackingField")).SetValue(o, v);
    static object Call(Type type, string n, params object[] args) => AccessTools.Method(type, n).Invoke(null, args);
    static object Old(string n, params object[] args) => Call(typeof(TimeStutter01437Tests), n, args);
    static BlueprintGuid Id(string token) => (BlueprintGuid)Call(Factory, "Id", token);
    static T Asset<T>(BlueprintsCache c, string token) where T : SimpleBlueprint => (T)c.Load(Id(token));
    static BlueprintSpellList List(BlueprintsCache c, string id) => (BlueprintSpellList)c.Load(BlueprintGuid.Parse(id));
    static void Register(BlueprintsCache c) => Call(Module, "Register", c);
    static void Check(bool ok, string message) { if (!ok) throw new Exception("TimeStopSpell01438: " + message); checks++; }
    static bool Reject(Action a) { try { a(); return false; } catch (Exception e) { while (e is TargetInvocationException) e = e.InnerException; if (e is InvalidOperationException) return true; throw; } }
    static void CacheFault(BlueprintGuid __0) { if (injectCache && __0 == Id(Tokens[0])) { injectCache = false; cacheFaults++; throw new InvalidOperationException("038 actual post-add spell failure"); } }
    static void ListFault() { if (injectLists) { injectLists = false; listFaults++; throw new InvalidOperationException("038 actual post-append spell-list failure"); } }
    static int ManagedRandom(int min, int max) => min + diceSamples++ % (max - min);
    static IEnumerable<CodeInstruction> RandomEndpoint(IEnumerable<CodeInstruction> input)
    {
        int replaced = 0;
        foreach (var instruction in input) {
            if (instruction.operand is MethodInfo method && method.DeclaringType == typeof(UnityEngine.Random) && method.Name == "Range" && method.ReturnType == typeof(int)) {
                instruction.opcode = OpCodes.Call; instruction.operand = AccessTools.Method(typeof(TimeStopSpell01438Tests), nameof(ManagedRandom)); replaced++;
            }
            yield return instruction;
        }
        if (replaced != 1) throw new Exception("038 native dice RNG endpoint changed");
    }
    static BlueprintsCache Fresh(BlueprintsCache parent)
    {
        var c = (BlueprintsCache)Old("Fresh", parent);
        foreach (string token in Tokens.Concat(new[] { "TimeStopDismiss01440" })) c.RemoveCachedBlueprint(Id(token));
        foreach (string listId in new[] { WizardList, PsychicList }) {
            var list = List(c, listId); if (list == null) continue;
            foreach (var row in list.SpellsByLevel.Where(r => r != null)) {
                var refs = (List<BlueprintAbilityReference>)Get(row, "m_Spells");
                Set(row, "m_Spells", refs.Where(r => r == null || r.Guid != Id(Tokens[0])).ToList());
                Set(row, "m_SpellsProxy", null); Set(row, "m_SpellsFiltered", null);
            }
        }
        return c;
    }
    static void Registration(BlueprintsCache parent, Harmony h)
    {
        var originalCache = ResourcesLibrary.BlueprintsCache;
        try {
            var c = Fresh(parent); AccessTools.Field(typeof(ResourcesLibrary), "BlueprintsCache").SetValue(null, c);
            var lists = new[] { List(c, WizardList), List(c, PsychicList) }.Where(l => l != null).ToArray();
            var arrays = lists.Select(l => l.SpellsByLevel).ToArray(); var rows = arrays.Select(a => a.ToArray()).ToArray();
            var refs = rows.Select(a => a.Select(r => ((List<BlueprintAbilityReference>)Get(r, "m_Spells")).ToArray()).ToArray()).ToArray();
            Check(Asset<BlueprintFeature>(c, "TimeStutterDiscovery") == null && Asset<BlueprintAbility>(c, "TimeStutterAbility") == null, "spell registers independently without discovery assets");
            Register(c); var spell = Asset<BlueprintAbility>(c, Tokens[0]); Register(c); Call(Module, "RegisterOptional", c);
            Check(ReferenceEquals(spell, Asset<BlueprintAbility>(c, Tokens[0])) && Tokens.All(t => c.Load(Id(t)) != null), "four private nodes have one stable, repeatable spell publication");
            Check(spell.Type == AbilityType.Spell && spell.Range == AbilityRange.Personal && spell.ActionType == Kingmaker.UnitLogic.Commands.Base.UnitCommand.CommandType.Standard && spell.CanTargetSelf && !spell.CanTargetEnemies && !spell.CanTargetFriends && !spell.CanTargetPoint, "native player spell is standard and personal, never a discovery or targeted attack");
            Check(spell.GetComponent<SpellComponent>().School == SpellSchool.Transmutation && !spell.SpellResistance && !spell.GetComponents<AbilityResourceLogic>().Any(), "transmutation has no save/resistance or Time Stutter daily-resource logic");
            Check(spell.ComponentsArray.All(component => ReferenceEquals(component.OwnerBlueprint, spell)) && spell.ElementsArray.All(e => ReferenceEquals(e.Owner, spell)), "every added list component and cloned action has exact own blueprint ownership");
            for (int i = 0; i < lists.Length; i++) {
                Check(ReferenceEquals(lists[i].SpellsByLevel, arrays[i]) && arrays[i].Length == rows[i].Length && arrays[i].Zip(rows[i], ReferenceEquals).All(v => v), "list array length, indices and every original row object survive " + lists[i].AssetGuid);
                for (int level = 0; level < arrays[i].Length; level++) {
                    var now = (List<BlueprintAbilityReference>)Get(arrays[i][level], "m_Spells");
                    Check(now.Where(r => r == null || r.Guid != spell.AssetGuid).Zip(refs[i][level], ReferenceEquals).All(v => v) && now.Count == refs[i][level].Length + (level == 9 ? 1 : 0), "only level9 appends once; original refs/order preserved at " + level);
                }
                Check(spell.GetComponents<SpellListComponent>().Count(l => l.SpellLevel == 9 && l.SpellList == lists[i]) == 1, "own reciprocal spell-list metadata exactly once");
            }
            Check(Asset<BlueprintFeature>(c, "TimeStutterDiscovery") == null && Asset<BlueprintAbilityResource>(c, "TimeStutterResource") == null, "normal spell publication never registers discovery or its resource");
            var components = spell.ComponentsArray; var originalListComponent = spell.GetComponents<SpellListComponent>().First();
            var duplicateListComponent = (SpellListComponent)AccessTools.Method(typeof(object), "MemberwiseClone").Invoke(originalListComponent, null);
            spell.ComponentsArray = components.Concat(new BlueprintComponent[] { duplicateListComponent }).ToArray(); metadataCases++;
            Check(Reject(() => Register(c)) && ReferenceEquals(spell, Asset<BlueprintAbility>(c, Tokens[0])) && spell.GetComponents<SpellListComponent>().Count() == lists.Length + 1, "duplicate own list metadata is diagnosed without removing healthy graph or foreign edits");
            spell.ComponentsArray = components;
            originalListComponent.OwnerBlueprint = new BlueprintAbility { AssetGuid = spell.AssetGuid }; metadataCases++;
            try { Check(Reject(() => Register(c)), "foreign component owner object is rejected even with the same root GUID"); }
            finally { originalListComponent.OwnerBlueprint = spell; }
            var ownRow = lists[0].SpellsByLevel[9]; var ownRefs = (List<BlueprintAbilityReference>)Get(ownRow, "m_Spells");
            Set(ownRow, "m_Spells", ownRefs.Concat(new[] { ownRefs.Single(r => r?.Guid == spell.AssetGuid) }).ToList()); metadataCases++;
            try { Check(Reject(() => Register(c)) && ((List<BlueprintAbilityReference>)Get(ownRow, "m_Spells")).Count(r => r?.Guid == spell.AssetGuid) == 2, "duplicate exact own row reference is diagnosed without deleting external list content"); }
            finally { Set(ownRow, "m_Spells", ownRefs); }
            Register(c); Check(ReferenceEquals(spell, Asset<BlueprintAbility>(c, Tokens[0])), "restored authored metadata retries with the original healthy graph");
            var ownedAction = spell.ElementsArray.Single(); ownedAction.Owner = new BlueprintAbility { AssetGuid = spell.AssetGuid }; metadataCases++;
            try { Check(Reject(() => Register(c)), "same-GUID replacement element owner is rejected by object identity, beyond the serialized definition hash"); }
            finally { ownedAction.Owner = spell; }
            var late = Fresh(parent); var psychic = List(late, PsychicList); late.RemoveCachedBlueprint(BlueprintGuid.Parse(PsychicList)); AccessTools.Field(typeof(ResourcesLibrary), "BlueprintsCache").SetValue(null, late);
            Register(late); Check(Asset<BlueprintAbility>(late, Tokens[0]).GetComponents<SpellListComponent>().Count() == 1, "absent Psychic leaves ordinary arcane spell fully available");
            if (psychic != null) { late.AddCachedBlueprint(psychic.AssetGuid, psychic); Call(Module, "RegisterOptional", late); Check(psychic.SpellsByLevel[9].SpellsRefs.Count(r => r?.Guid == Id(Tokens[0])) == 1 && Asset<BlueprintAbility>(late, Tokens[0]).GetComponents<SpellListComponent>().Count() == 2, "real optional retry adds late private Psychic list once"); }
            foreach (string token in Tokens) {
                var foreign = Fresh(parent); AccessTools.Field(typeof(ResourcesLibrary), "BlueprintsCache").SetValue(null, foreign);
                var published = c.Load(Id(token)); var impostor = (BlueprintScriptableObject)AccessTools.Method(typeof(object), "MemberwiseClone").Invoke(published, null);
                foreign.AddCachedBlueprint(impostor.AssetGuid, impostor); foreignCases++;
                Check(Reject(() => Register(foreign)) && ReferenceEquals(foreign.Load(impostor.AssetGuid), impostor) && Tokens.Where(t => t != token).All(t => foreign.Load(Id(t)) == null), "foreign same-GUID node preserved, no partial spell " + token);
            }
            var cacheMethod = AccessTools.Method(typeof(BlueprintsCache), "AddCachedBlueprint"); h.Patch(cacheMethod, postfix: new HarmonyMethod(typeof(TimeStopSpell01438Tests), nameof(CacheFault)));
            var failed = Fresh(parent); AccessTools.Field(typeof(ResourcesLibrary), "BlueprintsCache").SetValue(null, failed); injectCache = true;
            try { Check(Reject(() => Register(failed)) && Tokens.All(t => failed.Load(Id(t)) == null), "actual post-write cache exception rolls back all new nodes"); }
            finally { injectCache = false; h.Unpatch(cacheMethod, AccessTools.Method(typeof(TimeStopSpell01438Tests), nameof(CacheFault))); }
            Register(failed); Check(Tokens.All(t => failed.Load(Id(t)) != null), "cache transaction can retry without stale registration receipt");
            var append = AccessTools.Method(Module, "AppendLists"); h.Patch(append, postfix: new HarmonyMethod(typeof(TimeStopSpell01438Tests), nameof(ListFault)));
            var listFailure = Fresh(parent); AccessTools.Field(typeof(ResourcesLibrary), "BlueprintsCache").SetValue(null, listFailure); var level9 = List(listFailure, WizardList).SpellsByLevel[9]; var originalRefs = Get(level9, "m_Spells"); injectLists = true;
            try { Check(Reject(() => Register(listFailure)) && Tokens.All(t => listFailure.Load(Id(t)) == null) && ReferenceEquals(Get(level9, "m_Spells"), originalRefs), "real post-append failure restores exact list and removes only new graph"); }
            finally { injectLists = false; h.Unpatch(append, AccessTools.Method(typeof(TimeStopSpell01438Tests), nameof(ListFault))); }
            Register(listFailure); Check(List(listFailure, WizardList).SpellsByLevel[9].SpellsRefs.Count(r => r?.Guid == Id(Tokens[0])) == 1, "list rollback retry publishes once with healthy engine receipt");
            if (List(parent, PsychicList) != null) {
                var lateFailure = Fresh(parent); var lateList = List(lateFailure, PsychicList); lateFailure.RemoveCachedBlueprint(lateList.AssetGuid);
                AccessTools.Field(typeof(ResourcesLibrary), "BlueprintsCache").SetValue(null, lateFailure); Register(lateFailure);
                var healthyRoot = Asset<BlueprintAbility>(lateFailure, Tokens[0]); var healthyComponents = healthyRoot.ComponentsArray; var lateRefs = Get(lateList.SpellsByLevel[9], "m_Spells");
                lateFailure.AddCachedBlueprint(lateList.AssetGuid, lateList); h.Patch(append, postfix: new HarmonyMethod(typeof(TimeStopSpell01438Tests), nameof(ListFault))); injectLists = true;
                try {
                    Check(Reject(() => Call(Module, "RegisterOptional", lateFailure)) && ReferenceEquals(healthyRoot, Asset<BlueprintAbility>(lateFailure, Tokens[0])) && ReferenceEquals(healthyRoot.ComponentsArray, healthyComponents) && ReferenceEquals(Get(lateList.SpellsByLevel[9], "m_Spells"), lateRefs), "late optional post-append exception restores exact metadata and row while preserving published root");
                    var engine = Module.Assembly.GetType("CallOfTheWild.Wrath.Prototype.TimeStopEngine01438", true);
                    Check(!(bool)Call(engine, "RegisterGraph", lateFailure, Asset<BlueprintAbilityAreaEffect>(lateFailure, Tokens[2]), Asset<BlueprintBuff>(lateFailure, Tokens[3])), "late optional failure never unregisters the existing healthy graph receipt");
                } finally { injectLists = false; h.Unpatch(append, AccessTools.Method(typeof(TimeStopSpell01438Tests), nameof(ListFault))); }
                Call(Module, "RegisterOptional", lateFailure);
                Check(ReferenceEquals(healthyRoot, Asset<BlueprintAbility>(lateFailure, Tokens[0])) && healthyRoot.GetComponents<SpellListComponent>().Count() == 2 && lateList.SpellsByLevel[9].SpellsRefs.Count(r => r?.Guid == healthyRoot.AssetGuid) == 1, "late optional retry connects Psychic once without replacing any healthy node");
            }
            var bad = Fresh(parent); AccessTools.Field(typeof(ResourcesLibrary), "BlueprintsCache").SetValue(null, bad); var badList = List(bad, WizardList); var beforeRows = badList.SpellsByLevel; badList.SpellsByLevel = beforeRows.Take(9).ToArray();
            Check(Reject(() => Register(bad)) && badList.SpellsByLevel.Length == 9 && Tokens.All(t => bad.Load(Id(t)) == null), "malformed list is rejected without silently changing other mod array length");
        } finally { AccessTools.Field(typeof(ResourcesLibrary), "BlueprintsCache").SetValue(null, originalCache); }
    }
    static void Casting(BlueprintsCache cache)
    {
        Register(cache); var spell = Asset<BlueprintAbility>(cache, Tokens[0]); var run = spell.GetComponent<AbilityEffectRunAction>(); var apply = (ContextActionApplyBuff)run.Actions.Actions.Single();
        var u = (UnitEntityData)Old("Unit", cache, "038-player-spell", 20, 0);
        u.Stats.Charisma.BaseValue = 30; u.Stats.Intelligence.BaseValue = 30;
        var baseline = ((BlueprintAbility)cache.Load(BlueprintGuid.Parse("70e55e5b0ac84af78c084eb6cf47ae06"))).ComponentsArray;
        Check(apply.IsFromSpell && apply.ToCaster && apply.DurationValue.DiceType == DiceType.D4 && apply.DurationValue.DiceCountValue.Value == 1 && apply.DurationValue.BonusValue.Value == 1, "native D4+1 duration and actual spell origin have no unexplained extra round");
        foreach (bool spontaneous in new[] { false, true }) {
            var cls = (BlueprintCharacterClass)cache.Load(BlueprintGuid.Parse(Wizard));
            var book = (Spellbook)Call(typeof(SpellResourceFeatTests), "Book", u, spontaneous, cls);
            var data = (AbilityData)Call(typeof(SpellResourceFeatTests), "Spell", u, book, 9, spell);
            ((List<AbilityData>[])Get(book, "m_KnownSpells"))[9].Add(data);
            Check(data.SpellLevel == 9 && data.Spellbook == book && book.GetKnownSpells(9).Any(a => a.Blueprint == spell), "native known-spell data carries level9 and owning book " + spontaneous);
            u.Stats.Charisma.BaseValue = 18; Check(!book.CanSpend(data), "native level9 slot rejects casting ability score below19");
            u.Stats.Charisma.BaseValue = 30; Check(book.CanSpend(data), "native level9 slot accepts sufficient ability score");
            int before = spontaneous ? book.GetSpontaneousSlots(9) : book.GetMemorizedSpellSlots(9).Count(s => s.Available);
            AccessTools.Method(typeof(AbilityData), "SpendOneSpellCharge").Invoke(data, null); spends++;
            int after = spontaneous ? book.GetSpontaneousSlots(9) : book.GetMemorizedSpellSlots(9).Count(s => s.Available);
            Check(after == before - 1 && u.Resources.GetResourceAmount(Asset<BlueprintAbilityResource>(cache, "TimeStutterResource")) == 0, "actual native prepared/spontaneous spell charge spends one slot without discovery resource " + spontaneous);
        }
        foreach (var meta in new[] { (Metamagic)0, Metamagic.Extend, Metamagic.Maximize, Metamagic.Extend | Metamagic.Maximize, Metamagic.Quicken, Metamagic.Empower, Metamagic.Empower | Metamagic.Maximize }) {
            var data = new AbilityData(spell, u.Descriptor); data.MetamagicData = new MetamagicData(); data.MetamagicData.Add(meta);
            var context = new AbilityExecutionContext(data, new AbilityParams { CasterLevel = 20, SpellLevel = 9, Metamagic = meta }, new TargetWrapper(u), null, u);
            var samples = Enumerable.Range(0, 12).Select(_ => apply.DurationValue.Calculate(context).Seconds.TotalSeconds).ToArray();
            double multiplier = meta.HasFlag(Metamagic.Extend) ? 2 : 1;
            bool empowered = meta.HasFlag(Metamagic.Empower);
            Check(samples.All(seconds => seconds >= (empowered ? 18 : 12) * multiplier && seconds <= (empowered ? 42 : 30) * multiplier && seconds % 6 == 0), "actual native dice duration and Extend/Empower apply once " + meta);
            if (meta.HasFlag(Metamagic.Maximize)) Check(samples.All(seconds => seconds == (empowered ? 42 : 30) * multiplier), "native Maximize duration with optional Empower " + meta);
            AbilityExecutionProcess.PrepareCast(context);
            using (context.GetDataScope(u)) run.Apply(context, new TargetWrapper(u)); casts++;
            var host = u.Buffs.Enumerable.Single(b => b.Blueprint == Asset<BlueprintBuff>(cache, Tokens[1]));
            Check(host.IsTurnedOn && !host.IsPermanent && host.EndTime > (TimeSpan)AccessTools.Field(typeof(TimeStutter01437Tests), "now").GetValue(null), "actual native PrepareCast/Apply applies timed source buff " + meta);
            var target = (UnitEntityData)Old("Unit", cache, "038-spell-target-" + casts, 1, 0); Set(target, "m_Position", new UnityEngine.Vector3(3, 0, 0));
            var area = (AreaEffectEntityData)Old("MakeArea", "038-spell-end-" + casts, host.Context, Asset<BlueprintAbilityAreaEffect>(cache, Tokens[2]), new[] { u, target }); Old("BindArea", host, area);
            area.Blueprint.HandleUnitEnter(host.Context, area, target);
            Check(target.Buffs.Enumerable.Count(b => b.Blueprint == Asset<BlueprintBuff>(cache, Tokens[3])) == 1 && target.State.HasCondition(Kingmaker.UnitLogic.UnitCondition.CantMove), "native source dispatch freezes other creature for player spell");
            Check(!Kingmaker.UnitLogic.Commands.Base.UnitCommand.CommandTargetUntargetable(area, target) && Kingmaker.UnitLogic.Commands.Base.UnitCommand.CommandTargetUntargetable(u, target), "independently registered spell area has exact own-marker exception while ordinary targeting stays blocked");
            if (meta == Metamagic.Quicken) { AccessTools.Field(typeof(TimeStutter01437Tests), "now").SetValue(null, host.EndTime); u.Buffs.TickBuff(host, host.EndTime); } else u.Facts.Remove(host);
            Check(area.IsEnded, "actual source removal/expiry signals native ForceEnd"); Old("EndArea", area);
            Check(!target.Buffs.Enumerable.Any(b => b.Blueprint == Asset<BlueprintBuff>(cache, Tokens[3])) && !target.State.HasCondition(Kingmaker.UnitLogic.UnitCondition.CantMove), "actual native area end resumes other creature without leaked permanent effect");
        }
        Check(ReferenceEquals(((BlueprintAbility)cache.Load(BlueprintGuid.Parse("70e55e5b0ac84af78c084eb6cf47ae06"))).ComponentsArray, baseline), "original Lich prototype graph is untouched by player spell casts");
    }
    internal static int RunNative(BlueprintsCache cache, Harmony h)
    {
        checks = casts = spends = cacheFaults = listFaults = foreignCases = diceSamples = metadataCases = 0; injectCache = injectLists = false;
        h.Patch(AccessTools.Method(typeof(RulebookEvent.Dice), "D", new[] { typeof(int), typeof(DiceType) }), transpiler: new HarmonyMethod(typeof(TimeStopSpell01438Tests), nameof(RandomEndpoint)));
        // Earlier rule fixtures deliberately replace this overload with queued
        // dice. Suspend only that test prefix so this suite reaches the real
        // native dice body, just as it does in a focused run.
        var formulaDice = AccessTools.Method(typeof(RulebookEvent.Dice), "D", new[] { typeof(DiceFormula) });
        var legacyMethod = AccessTools.Method(typeof(Psychic070Tests), "ControlledDice");
        var legacyPrefixes = Harmony.GetPatchInfo(formulaDice)?.Prefixes.Where(p => p.PatchMethod == legacyMethod).ToArray() ?? new Patch[0];
        try {
            if (legacyPrefixes.Length != 0) h.Unpatch(formulaDice, legacyMethod);
            Check(!(Harmony.GetPatchInfo(formulaDice)?.Prefixes.Any(p => p.PatchMethod == legacyMethod) ?? false), "native dice checks exclude only the legacy queued-dice test prefix");
            Registration(cache, h); Casting(cache);
        } finally {
            foreach (var previous in legacyPrefixes)
                new Harmony(previous.owner).Patch(formulaDice, prefix: new HarmonyMethod(previous.PatchMethod) { priority = previous.priority, before = previous.before, after = previous.after, debug = previous.debug });
        }
        var restored = Harmony.GetPatchInfo(formulaDice)?.Prefixes.Where(p => p.PatchMethod == legacyMethod).ToArray() ?? new Patch[0];
        Check(restored.Length == legacyPrefixes.Length && legacyPrefixes.All(p => restored.Any(r => r.owner == p.owner && r.priority == p.priority && r.before.SequenceEqual(p.before) && r.after.SequenceEqual(p.after) && r.debug == p.debug)), "exact legacy queued-dice test prefix metadata is restored after native checks");
        Check(casts == 7 && spends == 2 && cacheFaults == 1 && listFaults == 2 && foreignCases == 4 && metadataCases == 4 && diceSamples > 0, "all claimed native cast/slot/failure paths ran");
        PsychicExpansionTests.Inventory["timeStopSpell01438Contracts"] = new JObject {
            ["passed"] = true, ["assertions"] = checks, ["additionalItemCount"] = 1, ["itemIds"] = new JArray("COTW-0551"), ["ordinaryFeatCount"] = 0, ["newSpellRootCount"] = 1,
            ["newBlueprints"] = new JArray(Tokens.Select(t => { var b = cache.Load(Id(t)); return new JObject { ["id"] = b.AssetGuid.ToString(), ["name"] = b.name, ["type"] = b.GetType().FullName }; })),
            ["nativePrepareCastAndApplyCalls"] = casts, ["nativeSpellChargeSpends"] = spends, ["postWriteCacheFaults"] = cacheFaults, ["postAppendListFaults"] = listFaults, ["foreignIdentityCases"] = foreignCases, ["foreignOrDuplicateMetadataCases"] = metadataCases, ["nativeDiceRandomEndpointSamples"] = diceSamples,
            ["legacyTestDicePrefixIsolatedAndRestored"] = true,
            ["boundary"] = "Actual native duration calculator, prepared/spontaneous charge spender, PrepareCast, ApplyBuff, area enter/end and buff expiry execute with existing037 clock/rule/UI/scene boundaries and only the Unity RNG endpoint controlled. Slot fixtures initialize native Spellbook containers; real archive book acquisition is separately covered by timeStopSpellbook01438Contracts. The real membership/performance path is covered by timeStopLifecycle01438Contracts, and ordinary target-buff timing/turn-window compensation by timeStopTimers01438Contracts. The global world clock and complete rendered scene are not simulated."
        };
        Console.WriteLine("PASS: " + checks + " TimeStopSpell01438 native spell checks."); return checks;
    }
}

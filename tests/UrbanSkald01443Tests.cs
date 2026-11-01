using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.Serialization;
using HarmonyLib;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Kingmaker;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.Blueprints.Facts;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.Controllers;
using Kingmaker.Designers.Mechanics.Facts;
using Kingmaker.ElementsSystem;
using Kingmaker.EntitySystem;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Enums;
using Kingmaker.RuleSystem;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.RuleSystem.Rules.Abilities;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components.AreaEffects;
using Kingmaker.UnitLogic.ActivatableAbilities;
using Kingmaker.UnitLogic.Buffs;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Buffs.Components;
using Kingmaker.UnitLogic.FactLogic;
using Kingmaker.UnitLogic.Groups;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Mechanics.Actions;
using Kingmaker.UnitLogic.Mechanics.Components;
using Kingmaker.UnitLogic.Mechanics.Conditions;
using Kingmaker.Utility;
using CallOfTheWild.Wrath.Prototype;

internal static class UrbanSkald01443Tests
{
    static readonly Type Module = typeof(UrbanSkaldCrowd01443).Assembly.GetType("CallOfTheWild.Wrath.Prototype.UrbanSkald01443", true);
    static readonly Type Helpers = typeof(BardMasterpieces01435Tests);
    static readonly string[] Tokens = (string[])AccessTools.Field(Module, "OwnTokens").GetValue(null);
    const string Skald = "6afa347d804838b48bda16acb0573dc0", Progression = "26418fed2bc153245972a5b54204ed75";
    static int checks, serial, nativeSongs, nativeAreas, nativeCrowdRules, cacheFaults, appendFaults;
    static int nativeTypeIdAttributeReads, nativeSerializedComponentRoundTrips, nativeTypeIdsScanned, productionTypeIdsScanned;
    const string CrowdTypeId = "1808b74130144c0fb02fd40fd26f2e3c";
    static bool cacheFault, appendFault;
    static readonly List<UnitEntityData> Units = new List<UnitEntityData>();
    static readonly List<UnitEntityData> SceneUnits = new List<UnitEntityData>();
    static BlueprintFaction FriendsFaction, EnemiesFaction;
    static UnitGroup Friends, Enemies;
    static object Get(object o, string n) => (AccessTools.Field(o.GetType(), n) ?? AccessTools.Field(o.GetType(), "<" + n + ">k__BackingField")).GetValue(o);
    static void Set(object o, string n, object value) => (AccessTools.Field(o.GetType(), n) ?? AccessTools.Field(o.GetType(), "<" + n + ">k__BackingField") ?? throw new Exception("Urban fixture field " + o.GetType().Name + "." + n)).SetValue(o, value);
    static object Call(Type t, string n, params object[] args) => AccessTools.Method(t, n).Invoke(null, args);
    static object B(string n, params object[] args) => Call(Helpers, n, args);
    static T Empty<T>() where T : class => (T)FormatterServices.GetUninitializedObject(typeof(T));
    static BlueprintGuid Id(string token) => (BlueprintGuid)Call(Module.Assembly.GetType("CallOfTheWild.Wrath.Prototype.PsychicAssets"), "Id", token);
    static T Asset<T>(BlueprintsCache c, string token) where T : SimpleBlueprint => (T)c.Load(Id(token));
    static T Native<T>(BlueprintsCache c, string id) where T : SimpleBlueprint => (T)c.Load(BlueprintGuid.Parse(id));
    static void Check(bool value, string reason) { if (!value) throw new Exception("UrbanSkald01443: " + reason); checks++; }
    static void Register(BlueprintsCache c) => Call(Module, "Register", c);
    static string Token(string stat, string kind) => "UrbanSkald" + stat + kind + "01443";
    static T Ref<T>(SimpleBlueprint bp) where T : BlueprintReferenceBase, new() { var r = BlueprintReferenceBase.CreateTyped<T>(bp); AccessTools.Property(typeof(BlueprintReferenceBase), "Cached").SetValue(r, bp, null); return r; }
    static bool Reject(Action action) { try { action(); return false; } catch (Exception e) { while (e is TargetInvocationException) e = e.InnerException; if (e is InvalidOperationException) return true; throw; } }
    static bool PlayerFaction(UnitDescriptor __instance, ref bool __result) { __result = __instance.Faction == FriendsFaction; return false; }
    static void NativeLogEndpoint(Owlcat.Runtime.Core.Logging.LogChannel channel, string message, object[] arguments) { }
    static IEnumerable<CodeInstruction> PostLoadLogBoundary(IEnumerable<CodeInstruction> instructions)
    {
        int replaced=0;
        foreach(var original in instructions) {
            var instruction=new CodeInstruction(original);
            if(instruction.operand is MethodInfo method && method.DeclaringType==typeof(Owlcat.Runtime.Core.Logging.LogChannel) && method.Name=="Log" && method.GetParameters().Select(p=>p.ParameterType).SequenceEqual(new[]{typeof(string),typeof(object[])})) {
                instruction.opcode=OpCodes.Call;instruction.operand=AccessTools.Method(typeof(UrbanSkald01443Tests),nameof(NativeLogEndpoint));replaced++;
            }
            yield return instruction;
        }
        if(replaced!=1)throw new Exception("Urban fixture native post-load logging endpoint changed: "+replaced);
    }
    static IEnumerable<Element> Elements(object value, HashSet<object> seen = null)
    {
        if (value == null || value is string || value is SimpleBlueprint || value is BlueprintReferenceBase || value is UnityEngine.Object || value.GetType().IsPrimitive || value.GetType().IsEnum) yield break;
        seen = seen ?? new HashSet<object>(); if (!seen.Add(value)) yield break;
        if (value is Element element) yield return element;
        if (value is IEnumerable list) { foreach (var item in list) foreach (var e in Elements(item, seen)) yield return e; yield break; }
        for (var current = value.GetType(); current != null && current != typeof(object); current = current.BaseType) foreach (var field in current.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly)) if (!field.IsStatic) foreach (var e in Elements(field.GetValue(value), seen)) yield return e;
    }
    static void Hydrate(BlueprintsCache c, bool all)
    {
        var ids = (string[])AccessTools.Field(Module, "NativeIds").GetValue(null);
        foreach (string id in ids) {
            var bp = Native<BlueprintScriptableObject>(c, id); var row = PsychicExpansionTests.NativeRows.Single(x => (string)x["blueprint"]["AssetId"] == id)["blueprint"]["Data"];
            foreach (var p in ((JObject)row).Properties()) {
                if (p.Name == "Components" || p.Name.StartsWith("$")) continue;
                var field = AccessTools.Field(bp.GetType(), p.Name);
                if (field != null && !field.IsInitOnly && !field.IsStatic && (all || field.GetValue(bp) == null || field.GetValue(bp) is Array a && a.Length == 0)) field.SetValue(bp, PsychicExpansionTests.Read(p.Value, field.FieldType));
            }
            bp.OnEnable();
            if (bp is BlueprintActivatableAbility activation) {
                // The broad archive decoder deliberately omits some enum/UI
                // metadata. Performance grouping must use the actual native
                // row, not that fixture's enum-default None.
                foreach (string name in new[] { "Group", "ActivationType", "m_ActivateWithUnitCommand", "WeightInGroup", "DeactivateIfCombatEnded" }) {
                    var f = AccessTools.Field(activation.GetType(), name); if (row[name] != null) f.SetValue(activation, PsychicExpansionTests.Read(row[name], f.FieldType));
                }
            }
        }
        B("Hydrate", c);
    }
    static BlueprintsCache Fresh(BlueprintsCache parent)
    {
        var c = (BlueprintsCache)Call(typeof(TimeStutter01437Tests), "Fresh", parent); foreach (string token in Tokens) c.RemoveCachedBlueprint(Id(token));
        var before = ResourcesLibrary.BlueprintsCache; AccessTools.Field(typeof(ResourcesLibrary), "BlueprintsCache").SetValue(null, c);
        try {
            foreach (string id in (string[])AccessTools.Field(Module, "NativeIds").GetValue(null)) {
                var bp = Native<BlueprintScriptableObject>(c, id); var row = PsychicExpansionTests.NativeRows.Single(x => (string)x["blueprint"]["AssetId"] == id)["blueprint"]["Data"];
                bp.ComponentsArray = ((JArray)row["Components"]).Select(x => (BlueprintComponent)PsychicExpansionTests.Read(x, typeof(BlueprintComponent))).ToArray();
                Set(bp, "m_AllElements", new List<Element>());
            }
            Hydrate(c, true);
        } finally { AccessTools.Field(typeof(ResourcesLibrary), "BlueprintsCache").SetValue(null, before); }
        return c;
    }
    static void CacheFault(BlueprintGuid __0) { if (cacheFault && __0 == Id(Tokens[0])) { cacheFault = false; cacheFaults++; throw new InvalidOperationException("043 Urban post-write fault"); } }
    static void AppendFault() { if (appendFault) { appendFault = false; appendFaults++; throw new InvalidOperationException("043 Urban post-append fault"); } }
    static void Registration(BlueprintsCache parent, Harmony h)
    {
        var before = ResourcesLibrary.BlueprintsCache;
        try {
            var c = Fresh(parent); AccessTools.Field(typeof(ResourcesLibrary), "BlueprintsCache").SetValue(null, c); var cls = Native<BlueprintCharacterClass>(c, Skald); var rows = cls.Progression.LevelEntries; var prior = cls.Archetypes.ToArray();
            var donors = ((string[])AccessTools.Field(Module, "NativeIds").GetValue(null)).Select(id => Native<BlueprintScriptableObject>(c, id)).ToArray();
            foreach (var donor in donors) { foreach (var element in Elements(donor.ComponentsArray)) if (!donor.ElementsArray.Contains(element)) donor.AddToElementsList(element); donor.OnEnable(); }
            var donorLists = donors.Select(d => Get(d, "m_AllElements")).ToArray(); var donorElements = donors.Select(d => d.ElementsArray.ToArray()).ToArray(); var donorOwners = donorElements.Select(a => a.Select(e => e.Owner).ToArray()).ToArray();
            Register(c); var archetype = Asset<BlueprintArchetype>(c, Tokens[0]);
            Check(donors.Select((donor, i) => ReferenceEquals(Get(donor, "m_AllElements"), donorLists[i]) && donor.ElementsArray.SequenceEqual(donorElements[i]) && donor.ElementsArray.Select((e, n) => ReferenceEquals(e.Owner, donorOwners[i][n])).All(x => x)).All(x => x), "warmed native donor element-list identity, every element and every Owner survive private graph publication");
            Check(Tokens.Select(t => c.Load(Id(t))).OfType<BlueprintScriptableObject>().All(b => donors.All(d => !ReferenceEquals(Get(b, "m_AllElements"), Get(d, "m_AllElements")))), "every private element list is isolated from all native donors");
            foreach (int level in Enumerable.Range(1, 20)) { archetype.GetAddEntry(level); archetype.GetRemoveEntry(level); }
            Register(c);
            Check(Tokens.Length == 52 && Tokens.All(t => c.Load(Id(t)) != null) && cls.Archetypes.Count(a => a == archetype) == 1, "complete52-node graph and exactly one archetype after repeated registration");
            Check(ReferenceEquals(rows, cls.Progression.LevelEntries) && cls.Archetypes.Where(a => a != archetype).SequenceEqual(prior), "native progression and all prior archetypes retained");
            Check(archetype.AddFeatures.Select(e => e.Level).SequenceEqual(new[] { 1, 3, 10 }) && archetype.RemoveFeatures.Select(e => e.Level).SequenceEqual(new[] { 1, 9, 10, 14, 19 }), "native replacement levels; absent Song of Marching does not remove unrelated level3 features");
            Check(archetype.GetParentClass() == cls && !archetype.RemoveSpellbook && !archetype.ChangeCasterType && !archetype.ReplaceClassSkills && archetype.AddSkillPoints == 0, "native Skald casting, skills and chassis preserved");
            var refusal = Native<BlueprintBuff>(c, "9fd2dd27d838f0049ab56e9da3508b25").GetComponent<AddFactContextActions>();
            Check(refusal.Deactivated.Actions.Count(a => a.name?.StartsWith("UrbanSkaldRefuse") == true) == 6, "native refusal toggle owns six immediate cleanups, without duplicates");
            var repeatArrays = new[] { Get(cls, "m_Archetypes"), cls.Progression.UIGroups, Get(cls.Progression, "m_UIDeterminatorsGroup"), refusal.Deactivated }; var published = Tokens.Select(t => c.Load(Id(t))).ToArray();
            var repeatMethod = AccessTools.Method(Module, "Append"); var repeatPatch = AccessTools.Method(typeof(UrbanSkald01443Tests), nameof(AppendFault)); h.Patch(repeatMethod, postfix: new HarmonyMethod(repeatPatch)); appendFault = true;
            try { Check(Reject(() => Register(c)) && repeatArrays.Select((o, i) => ReferenceEquals(o, new[] { Get(cls, "m_Archetypes"), cls.Progression.UIGroups, Get(cls.Progression, "m_UIDeterminatorsGroup"), refusal.Deactivated }[i])).All(x => x) && Tokens.Select((t, i) => ReferenceEquals(c.Load(Id(t)), published[i])).All(x => x), "real published-repeat Append postfix exception preserves all four shared identities and every existing private asset"); }
            finally { appendFault = false; h.Unpatch(repeatMethod, repeatPatch); }
            Register(c); Check(repeatArrays.Select((o, i) => ReferenceEquals(o, new[] { Get(cls, "m_Archetypes"), cls.Progression.UIGroups, Get(cls.Progression, "m_UIDeterminatorsGroup"), refusal.Deactivated }[i])).All(x => x), "normal repeated attach also preserves all shared identities without reallocation");
            var authored = Asset<BlueprintBuff>(c, "UrbanSkaldMockeryBuff01443").GetComponents<AddStatBonus>().First(); int authoredValue = authored.Value; authored.Value = -9;
            try { Check(Reject(() => Register(c)), "real authored bonus mutation is still rejected after runtime cache exclusion"); }
            finally { authored.Value = authoredValue; } Register(c);
            var componentBp = Asset<BlueprintBuff>(c, "UrbanSkaldMockeryBuff01443"); var component = componentBp.ComponentsArray[0]; var elementBp = Asset<BlueprintAbilityAreaEffect>(c, "UrbanSkaldMockeryArea01443"); var ownedElement = elementBp.ElementsArray.First();
            foreach (int mode in new[] { 0, 1, 2 }) {
                BlueprintScriptableObject Foreign(BlueprintScriptableObject bp) { if (mode == 0) return null; var foreign = (BlueprintScriptableObject)AccessTools.Method(typeof(object), "MemberwiseClone").Invoke(bp, null); if (mode == 1) foreign.AssetGuid = BlueprintGuid.Parse("04300000000000000000000000000039"); return foreign; }
                component.OwnerBlueprint = Foreign(componentBp); try { Check(Reject(() => Register(c)), "component exact-owner guard rejects null/different/same-GUID foreign object mode " + mode); } finally { component.OwnerBlueprint = componentBp; }
                ownedElement.Owner = Foreign(elementBp); try { Check(Reject(() => Register(c)), "element exact-owner guard rejects null/different/same-GUID foreign object mode " + mode); } finally { ownedElement.Owner = elementBp; }
                Register(c);
            }
            Check(archetype.ReplaceStartingEquipment && ((BlueprintItemReference[])Get(archetype, "m_StartingItems")).Select(r => r.Guid).SequenceEqual(((BlueprintItemReference[])Get(Native<BlueprintCharacterClass>(c, "772c83a25e2268e448e841dcd548235f"), "m_StartingItems")).Select(r => r.Guid)), "original CotW Bard starting equipment replaces Skald medium armor equipment");
            var conflictCache = Fresh(parent); AccessTools.Field(typeof(ResourcesLibrary), "BlueprintsCache").SetValue(null, conflictCache);
            foreach (string token in Tokens) {
                var fail = conflictCache;
                var foreign = (BlueprintScriptableObject)AccessTools.Method(typeof(object), "MemberwiseClone").Invoke(c.Load(Id(token)), null); fail.AddCachedBlueprint(foreign.AssetGuid, foreign);
                Check(Reject(() => Register(fail)) && ReferenceEquals(fail.Load(foreign.AssetGuid), foreign) && Tokens.Where(t => t != token).All(t => fail.Load(Id(t)) == null), "identity collision leaves no partial graph: " + token);
                fail.RemoveCachedBlueprint(foreign.AssetGuid); Check(Tokens.All(t => fail.Load(Id(t)) == null), "conflict case restores exact private-identity absence before next case");
            }
            var covered = Fresh(parent); AccessTools.Field(typeof(ResourcesLibrary), "BlueprintsCache").SetValue(null, covered); var coveredClass = Native<BlueprintCharacterClass>(covered, Skald);
            var external = new BlueprintArchetype { name = "Provider_Urban_Skald_Archetype", AssetGuid = BlueprintGuid.Parse("01443000000000000000000000000031") }; Set(external, "m_ParentClass", coveredClass); covered.AddCachedBlueprint(external.AssetGuid, external);
            Set(coveredClass, "m_Archetypes", ((BlueprintArchetypeReference[])Get(coveredClass, "m_Archetypes")).Concat(new[] { Ref<BlueprintArchetypeReference>(external) }).ToArray());
            Check(Reject(() => Register(covered)) && Tokens.All(t => covered.Load(Id(t)) == null) && coveredClass.Archetypes.Contains(external), "external equivalent retained without duplicate implementation");
            foreach (bool atCache in new[] { true, false }) {
                var fail = Fresh(parent); AccessTools.Field(typeof(ResourcesLibrary), "BlueprintsCache").SetValue(null, fail); var fc = Native<BlueprintCharacterClass>(fail, Skald); var original = Get(fc, "m_Archetypes"); var ui = fc.Progression.UIGroups; var det = Get(fc.Progression, "m_UIDeterminatorsGroup");
                var ra = Native<BlueprintBuff>(fail, "9fd2dd27d838f0049ab56e9da3508b25").GetComponent<AddFactContextActions>(); var cleanup = ra.Deactivated;
                var method = atCache ? AccessTools.Method(typeof(BlueprintsCache), "AddCachedBlueprint") : AccessTools.Method(Module, "Append"); var patch = AccessTools.Method(typeof(UrbanSkald01443Tests), atCache ? nameof(CacheFault) : nameof(AppendFault));
                h.Patch(method, postfix: new HarmonyMethod(patch)); if (atCache) cacheFault = true; else appendFault = true;
                try { Check(Reject(() => Register(fail)) && Tokens.All(t => fail.Load(Id(t)) == null) && ReferenceEquals(Get(fc, "m_Archetypes"), original) && ReferenceEquals(fc.Progression.UIGroups, ui) && ReferenceEquals(Get(fc.Progression, "m_UIDeterminatorsGroup"), det) && ReferenceEquals(ra.Deactivated, cleanup), "post-cache/post-append failure restores exact class/UI/refusal arrays"); }
                finally { h.Unpatch(method, patch); cacheFault = appendFault = false; }
                Register(fail); Check(fc.Archetypes.Count(a => a != null && a.AssetGuid == Id(Tokens[0])) == 1, "failed transaction retries once");
            }
        } finally { AccessTools.Field(typeof(ResourcesLibrary), "BlueprintsCache").SetValue(null, before); }
    }
    static UnitEntityData Unit(BlueprintsCache c, int level = 12)
    {
        var u = (UnitEntityData)B("Unit", c, "urban043-" + ++serial, Skald, level); Set(u, "m_Position", new UnityEngine.Vector3(serial * 5, 0, 0)); Set(u, "m_Group", Friends); Set(u, "m_GroupId", Friends.Id); Set(u.Descriptor, "m_Faction", FriendsFaction); ((MultiSet<BlueprintFaction>)Get(Friends, "m_Factions")).Add(FriendsFaction); Units.Add(u); ((List<UnitEntityData>)Get(Game.Instance.Player,"m_Party")).Add(u); return u;
    }
    static void Enemy(UnitEntityData u) { Set(u, "m_Group", Enemies); Set(u, "m_GroupId", Enemies.Id); Set(u.Descriptor, "m_Faction", EnemiesFaction); ((MultiSet<BlueprintFaction>)Get(Enemies, "m_Factions")).Add(EnemiesFaction); }
    static Feature Fact(UnitEntityData u, BlueprintFeature b) => (Feature)B("Fact", u, b);
    static Buff Buff(UnitEntityData u, BlueprintBuff b, UnitEntityData caster = null) => (Buff)B("Buff", u, b, caster);
    static AreaEffectEntityData Area(string name, MechanicsContext context, BlueprintAbilityAreaEffect b, UnitEntityData target) => (AreaEffectEntityData)B("Area", name, context, b, new UnitEntityData[] { target });
    static void Enter(BlueprintAbilityAreaEffect b, Buff host, UnitEntityData target, bool enter)
    {
        var context = new MechanicsContext(host.Context.MaybeCaster, host.Context.MaybeCaster.Descriptor, b, host.Context); context.Recalculate();
        var area = Area("043-area-" + ++serial, context, b, target); var component = b.GetComponent<AbilityAreaEffectRunAction>(); nativeAreas++;
        if (enter) component.HandleUnitEnter(context, area, target); else component.HandleUnitExit(context, area, target);
    }
    static string Allocation(string stat, int portion, string kind) => "UrbanSkaldAllocation" + stat + portion + kind + "01442";
    static ActivatableAbility[] AllocationFacts(UnitEntityData singer) => singer.ActivatableAbilities.RawFacts.Where(a => a.Blueprint.name.StartsWith("CotWWrathPsychicUrbanSkaldAllocation")).ToArray();
    static void Allocate(UnitEntityData singer, int[] amounts)
    {
        foreach(var a in AllocationFacts(singer)) a.TurnOffImmediately();
        var stats=new[]{"Strength","Dexterity","Constitution"};
        for(int i=0;i<3;i++) for(int n=0;n<amounts[i];n++) B("Start",singer,singer.ActivatableAbilities.RawFacts.Single(a=>a.Blueprint.AssetGuid==Id(Allocation(stats[i],n,"Ability"))));
    }
    static int[] Bonuses(UnitEntityData unit, int[] baseline) => new[]{unit.Stats.Strength.ModifiedValue-baseline[0],unit.Stats.Dexterity.ModifiedValue-baseline[1],unit.Stats.Constitution.ModifiedValue-baseline[2]};
    static int[] Attributes(UnitEntityData unit) => new[]{unit.Stats.Strength.ModifiedValue,unit.Stats.Dexterity.ModifiedValue,unit.Stats.Constitution.ModifiedValue};
    static void Songs(BlueprintsCache c)
    {
        var proficient=Unit(c);var profFact=Fact(proficient,Asset<BlueprintFeature>(c,"UrbanSkaldProficiencies01443"));
        Check(proficient.Proficiencies.Contains(Kingmaker.Blueprints.Items.Armors.ArmorProficiencyGroup.Light)&&!proficient.Proficiencies.Contains(Kingmaker.Blueprints.Items.Armors.ArmorProficiencyGroup.Medium)&&proficient.Proficiencies.Contains(WeaponCategory.Longsword),"unchanged native proficiency contract");proficient.Facts.Remove(profFact);
        var performer=Unit(c,12);Fact(performer,Native<BlueprintFeature>(c,"334cb75aa673d1d4bb279761c2ef5cf1"));Fact(performer,Asset<BlueprintFeature>(c,"UrbanSkaldControlledRage01443"));
        var pool=Native<BlueprintAbilityResource>(c,"4a2302c4ec2cfb042bba67d825babfec");performer.Resources.Restore(pool);
        var songs=performer.ActivatableAbilities.RawFacts.Where(a=>a.Blueprint.Group==Native<BlueprintActivatableAbility>(c,"264e93ac44ace16488226b8f7756bf26").Group&&a.Blueprint.name.Contains("UrbanSkald")).ToArray();
        Check(songs.Length==1&&songs[0].Blueprint.AssetGuid==Id(Token("Strength","Ability")),"exactly one native Controlled Inspired Rage performance retains the old primary GUID");
        Check(AllocationFacts(performer).Length==9&&AllocationFacts(performer).All(a=>a.Blueprint.Group==ActivatableAbilityGroup.None&&a.Blueprint.ActivationType==AbilityActivationType.Immediately&&a.Blueprint.DoNotTurnOffOnRest&&!a.Blueprint.DeactivateIfCombatEnded&&!a.Blueprint.DeactivateIfOwnerDisabled&&a.Blueprint.GetComponents<ActivatableAbilityResourceLogic>().Count()==0),"nine persistent resource-free allocation controls use no shared performance group");
        int before=performer.Resources.GetResourceAmount(pool);Allocate(performer,new[]{1,1,0});Check(performer.Resources.GetResourceAmount(pool)==before,"allocation does not spend performance rounds");
        B("Start",performer,songs[0]);Check(songs[0].IsStarted&&performer.Resources.GetResourceAmount(pool)==before-1,"one actual performance startup spends one round");B("Tick",performer,songs[0]);Check(performer.Resources.GetResourceAmount(pool)==before-2,"next actual controller round spends one round");songs[0].TurnOffImmediately();Check(AllocationFacts(performer).Count(a=>a.IsOn)==2,"stopping performance preserves allocation state");
        foreach(var a in AllocationFacts(performer)) a.HandleUnitLeaveCombat(performer);
        Check(AllocationFacts(performer).Count(a=>a.IsOn)==2,"real leave-combat callbacks preserve allocation");
        foreach(int level in new[]{1,7,8,15,16,20}) {
            int budget=level>=16?3:level>=8?2:1;
            for(int strength=0;strength<=budget;strength++) for(int dexterity=0;dexterity<=budget-strength;dexterity++) {
                int constitution=budget-strength-dexterity;var amounts=new[]{strength,dexterity,constitution};
                var singer=Unit(c,level);var ally=Unit(c,1);var other=Unit(c,1);Fact(singer,Asset<BlueprintFeature>(c,"UrbanSkaldControlledRage01443"));Allocate(singer,amounts);
                Check(AllocationFacts(singer).Count(a=>a.IsOn)==budget,"native persistent selectors exactly fill the level budget "+level+"/"+string.Join(",",amounts));
                Check(AllocationFacts(singer).Where(a=>!a.IsOn).All(a=>!a.IsAvailableByRestrictions),"full budget prevents every extra allocation without spending a round");
                Fact(singer,Native<BlueprintFeature>(c,"2f59c85adec695b418a66242f2ba08b8"));
                var baseline=Attributes(ally);int will=ally.Stats.SaveWill.ModifiedValue,ac=ally.Stats.AC.ModifiedValue;
                var effect=Asset<BlueprintBuff>(c,Token("Strength","Effect"));var applied=Buff(ally,effect,singer);nativeSongs++;
                Check(Bonuses(ally,baseline).SequenceEqual(amounts.Select(n=>2*n)),"real native FeatureList rank/stat components deliver exact total morale allocation at level "+level);
                Check(ally.Stats.SaveWill.ModifiedValue==will&&ally.Stats.AC.ModifiedValue==ac+dexterity,"no Will bonus or AC penalty");
                Check(ally.Facts.List.Any(f=>f.Blueprint.AssetGuid.ToString()=="2f59c85adec695b418a66242f2ba08b8"),"native caster rage powers remain shared");
                Check(!ally.State.HasCondition(UnitCondition.SpellcastingForbidden),"accepting non-singer can cast before Master Skald; no native SpellcastingForbidden condition");
                ally.Facts.Remove(applied);Check(Bonuses(ally,baseline).All(n=>n==0)&&!ally.Facts.List.Any(f=>f.Blueprint.AssetGuid.ToString()=="2f59c85adec695b418a66242f2ba08b8"),"native removal reverses all allocated stats and transferred power");
                var host=Buff(singer,Asset<BlueprintBuff>(c,Token("Strength","Main")));var area=Asset<BlueprintAbilityAreaEffect>(c,Token("Strength","Area"));
                Enter(area,host,other,true);Check(!other.Buffs.Enumerable.Any(b=>b.Blueprint==effect),"native refusal boundary remains");
                var accept=Buff(other,Native<BlueprintBuff>(c,"9fd2dd27d838f0049ab56e9da3508b25"));Enter(area,host,other,true);Check(other.Buffs.Enumerable.Any(b=>b.Blueprint==effect)&&!other.State.HasCondition(UnitCondition.SpellcastingForbidden),"acceptance grants allocated stats without spellcasting restriction");
                other.Facts.Remove(accept);Check(!other.Buffs.Enumerable.Any(b=>b.Blueprint==effect),"actual acceptance deactivation immediately clears effect");singer.Facts.Remove(host);
            }
        }
        foreach(int level in new[]{1,8,16}) {
            var singer=Unit(c,level);var target=Unit(c);Fact(singer,Asset<BlueprintFeature>(c,"UrbanSkaldControlledRage01443"));int budget=level>=16?3:level>=8?2:1;Allocate(singer,new[]{0,budget,0});
            var mythic=Asset<BlueprintBuff>(c,Token("Strength","MythicEffect"));Fact(singer,Native<BlueprintFeature>(c,"69fad80df0d44dfea19aef513db959c2"));Buff(target,Native<BlueprintBuff>(c,"9fd2dd27d838f0049ab56e9da3508b25"));var host=Buff(singer,Asset<BlueprintBuff>(c,Token("Strength","Main")));var area=Asset<BlueprintAbilityAreaEffect>(c,Token("Strength","Area"));Enter(area,host,target,true);
            Check(target.Buffs.Enumerable.Any(b=>b.Blueprint==mythic)&&!target.State.HasCondition(UnitCondition.SpellcastingForbidden),"real mythic branch uses allocation and allows casting at level "+level);Enter(area,host,target,false);singer.Facts.Remove(host);
        }
        var source=Unit(c,12);var receiver=Unit(c);var otherSource=Unit(c,12);Fact(source,Asset<BlueprintFeature>(c,"UrbanSkaldControlledRage01443"));Allocate(source,new[]{1,1,0});var normalEffect=Asset<BlueprintBuff>(c,Token("Strength","Effect"));
        var descriptor=new BuffDescriptorImmunity{Descriptor=SpellDescriptor.MindAffecting};Set(descriptor,"m_IgnoreFeature",new BlueprintUnitFactReference());var immune=Buff(receiver,(BlueprintBuff)B("Custom","urban-controlled-immunity",new BlueprintComponent[]{descriptor}));Buff(receiver,Native<BlueprintBuff>(c,"9fd2dd27d838f0049ab56e9da3508b25"));
        var ownHost=Buff(source,Asset<BlueprintBuff>(c,Token("Strength","Main")));var ownArea=Asset<BlueprintAbilityAreaEffect>(c,Token("Strength","Area"));Enter(ownArea,ownHost,receiver,true);Check(!receiver.Buffs.Enumerable.Any(b=>b.Blueprint==normalEffect),"mind-affecting immunity preserved");receiver.Facts.Remove(immune);Enter(ownArea,ownHost,receiver,true);Check(receiver.Buffs.Enumerable.Any(b=>b.Blueprint==normalEffect),"immunity removal restores accepted song");
        Buff(receiver,normalEffect,otherSource);Enter(ownArea,ownHost,receiver,false);Check(receiver.Buffs.Enumerable.Any(b=>b.Blueprint==normalEffect&&b.Context.MaybeCaster==otherSource),"old source exit preserves other singer");receiver.Facts.Remove(receiver.Buffs.Enumerable.Single(b=>b.Blueprint==normalEffect));source.Facts.Remove(ownHost);
        var restricted=Unit(c);var originalRestriction=Buff(restricted,Native<BlueprintBuff>(c,"345d36cd45f5614409824209f26d0130"));
        Check(restricted.State.HasCondition(UnitCondition.SpellcastingForbidden),"actual native ForbidSpellCasting sets condition51");
        var controlledOnRestricted=Buff(restricted,normalEffect,source);restricted.Facts.Remove(controlledOnRestricted);
        Check(restricted.State.HasCondition(UnitCondition.SpellcastingForbidden),"controlled effect end preserves unrelated native casting restriction");restricted.Facts.Remove(originalRestriction);Check(!restricted.State.HasCondition(UnitCondition.SpellcastingForbidden),"removing only original native restriction restores casting condition");
        RepairRuntime(c);
        PsychicExpansionTests.Inventory["urbanControlledRepair01442Contracts"]=new JObject{["passed"]=true,["additionalMigrationItemCount"]=0,["persistentToggleCount"]=9,["performanceButtonCount"]=1,["allocationCases"]=nativeSongs,["budgets"]=new JArray(2,4,6),["noNewHarmonyTargets"]=true,["nativeAllocationRanks"]=true,["nativeCastingConditionAbsent"]=true,["preservesOtherRestrictions"]=true,["liveReallocation"]=true,["nativeSpellAvailability"]=true,["nativeLegacyGrantReconciliation"]=true,["nativeLegacyCastingCleanup"]=true,["nativeLegacyTemplateRebind"]=true};
    }
    static void RepairRuntime(BlueprintsCache c)
    {
        var singer=Unit(c,16);var ally=Unit(c,1);Fact(singer,Asset<BlueprintFeature>(c,"UrbanSkaldControlledRage01443"));Allocate(singer,new[]{3,0,0});
        var baseline=Attributes(ally);var effect=Asset<BlueprintBuff>(c,Token("Strength","Effect"));var active=Buff(ally,effect,singer);
        Check(Bonuses(ally,baseline).SequenceEqual(new[]{6,0,0}),"native initial single-attribute allocation");
        Allocate(singer,new[]{1,1,1});Check(Bonuses(ally,baseline).SequenceEqual(new[]{2,2,2}),"real party fact events recalculate active recipient without restarting performance");
        var spell=new BlueprintAbility { AssetGuid=BlueprintGuid.Parse("01442000000000000000000000000071"),name="UrbanRepairNativeSpellAvailability",Type=AbilityType.Spell,ActionType=Kingmaker.UnitLogic.Commands.Base.UnitCommand.CommandType.Standard,ComponentsArray=Array.Empty<BlueprintComponent>() };spell.OnEnable();
        var data=new AbilityData(spell,ally.Descriptor);
        Check(data.IsAvailableForCast,"real AbilityData spell availability passes while accepting controlled rage before Master Skald");
        var blocked=Buff(ally,Native<BlueprintBuff>(c,"345d36cd45f5614409824209f26d0130"));Check(!data.IsAvailableForCast,"real original rage ForbidSpellCasting makes same spell unavailable");ally.Facts.Remove(blocked);Check(data.IsAvailableForCast,"same spell immediately available after only external blocker is removed");
        ally.Facts.Remove(active);Check(Bonuses(ally,baseline).All(n=>n==0)&&data.IsAvailableForCast,"allocated bonuses cleanly end without impairing spell availability");
        var legacyPenalty=Asset<BlueprintBuff>(c,"UrbanSkaldNoCasting01443");var oldForbid=legacyPenalty.GetComponent<ForbidSpellCasting>();Check(oldForbid.Disabled,"old private restriction component is retained disabled for native saved receipt migration");
        Buff restoredPenalty;oldForbid.Disabled=false;
        try {restoredPenalty=Buff(ally,legacyPenalty,singer);}finally{oldForbid.Disabled=true;}
        var independentPenalty=Buff(ally,Native<BlueprintBuff>(c,"345d36cd45f5614409824209f26d0130"));
        Check(!data.IsAvailableForCast&&((sbyte[])Get(ally.State,"m_Conditions"))[(int)UnitCondition.SpellcastingForbidden]==2,"old private saved receipt and independent native restriction each own one count");
        restoredPenalty.ApplyPostLoadFixes();
        Check(!data.IsAvailableForCast&&((sbyte[])Get(ally.State,"m_Conditions"))[(int)UnitCondition.SpellcastingForbidden]==1,"native disabled-component post-load migration releases exactly the legacy private count");
        ally.Facts.Remove(independentPenalty);Check(data.IsAvailableForCast,"removing independent restriction after legacy cleanup leaves no orphan casting condition");
        var laterIndependent=Buff(ally,Native<BlueprintBuff>(c,"345d36cd45f5614409824209f26d0130"));ally.Facts.Remove(restoredPenalty);Check(!data.IsAvailableForCast&&((sbyte[])Get(ally.State,"m_Conditions"))[(int)UnitCondition.SpellcastingForbidden]==1,"later removal of retired private buff cannot decrement a new unrelated condition");ally.Facts.Remove(laterIndependent);Check(data.IsAvailableForCast,"final independent restriction removal restores actual spell availability");
        var ignored=Unit(c);Fact(ignored,Native<BlueprintFeature>(c,"1a639eadc2c3ed546bc4bb236864cd0c"));Buff ignoredReceipt;
        var retiredComponents=legacyPenalty.ComponentsArray;var nativeOld=(ForbidSpellCasting)AccessTools.Method(Module.Assembly.GetType("CallOfTheWild.Wrath.Prototype.PsychicAssets"),"CopyData").MakeGenericMethod(typeof(ForbidSpellCasting)).Invoke(null,new object[]{Native<BlueprintBuff>(c,"345d36cd45f5614409824209f26d0130").GetComponent<ForbidSpellCasting>()});nativeOld.name=oldForbid.name;
        try{legacyPenalty.ComponentsArray=new BlueprintComponent[]{nativeOld};legacyPenalty.OnEnable();ignoredReceipt=Buff(ignored,legacyPenalty,singer);}finally{legacyPenalty.ComponentsArray=retiredComponents;legacyPenalty.OnEnable();}
        Set(ignoredReceipt,"m_BlueprintComponents",null);AccessTools.Method(typeof(EntityFact),"PostLoadComponents").Invoke(ignoredReceipt,null);
        Check(ignoredReceipt.Components.Any(x=>ReferenceEquals(x.SourceBlueprintComponent,oldForbid)),"real native saved-runtime component name rebinding accepts retired subclass with unchanged EmptyComponentData schema");
        Check(!ignored.State.HasCondition(UnitCondition.SpellcastingForbidden),"actual native IgnoreFeature branch contributes no private condition");
        var independentForbid=new ForbidSpellCasting();Set(independentForbid,"m_IgnoreFeature",new BlueprintFeatureReference());var actualOther=Buff(ignored,(BlueprintBuff)B("Custom","urban-independent-forbid",new BlueprintComponent[]{independentForbid}));
        ignoredReceipt.ApplyPostLoadFixes();Check(ignored.State.HasCondition(UnitCondition.SpellcastingForbidden)&&((sbyte[])Get(ignored.State,"m_Conditions"))[(int)UnitCondition.SpellcastingForbidden]==1,"ignored legacy receipt cannot release another source condition");ignored.Facts.Remove(ignoredReceipt);ignored.Facts.Remove(actualOther);
        var controlled=Asset<BlueprintFeature>(c,"UrbanSkaldControlledRage01443");var grants=controlled.GetComponent<AddFacts>();var current=Get(grants,"m_Facts");var legacy=new[]{"Strength","Dexterity","Constitution"}.Select(s=>Ref<BlueprintUnitFactReference>(Asset<BlueprintActivatableAbility>(c,Token(s,"Ability")))).ToArray();
        var old=Unit(c,8);Feature restored;
        try { Set(grants,"m_Facts",legacy);restored=Fact(old,controlled); } finally { Set(grants,"m_Facts",current); }
        Check(old.ActivatableAbilities.RawFacts.Count(a=>a.Blueprint.name.StartsWith("CotWWrathPsychicUrbanSkald")&&!a.Blueprint.name.Contains("Allocation"))==3,"old AddFacts runtime receipt holds three legacy grants before actual native post-load fix");
        restored.ApplyPostLoadFixes();
        Check(old.ActivatableAbilities.RawFacts.Count(a=>a.Blueprint.Group==ActivatableAbilityGroup.BardicPerformance&&a.Blueprint.name.Contains("UrbanSkald"))==1&&AllocationFacts(old).Length==9,"real native AddFacts ApplyPostLoadFixes removes obsolete owned performances and restores allocation controls");
        Allocate(old,new[]{1,1,0});restored.ApplyPostLoadFixes();Check(AllocationFacts(old).Count(a=>a.IsOn)==2,"native repeated post-load grant reconciliation preserves selected persistent state");
        foreach(bool budgetType in new[]{true,false}) {
        var type=budgetType?typeof(UrbanSkaldAllocationRestriction01442):typeof(UrbanSkaldRetiredCasting01442);var expectedId=budgetType?"0d74cba56cce4a7098e8ebf5a2abe677":"346bf0488d9a45e8956d7e3a620b167a";var attr=(TypeIdAttribute)type.GetCustomAttributes(typeof(TypeIdAttribute),false).Single();
        Check(attr.GuidString==expectedId&&type.Assembly.GetTypes().SelectMany(t=>t.GetCustomAttributes(typeof(TypeIdAttribute),false).Cast<TypeIdAttribute>()).Count(a=>a.Guid==attr.Guid)==1,"new repair component has one actually constructed unique native TypeId");
        BlueprintScriptableObject bp=budgetType?(BlueprintScriptableObject)Asset<BlueprintActivatableAbility>(c,Allocation("Dexterity",1,"Ability")):legacyPenalty;var restriction=bp.ComponentsArray.Single(x=>x.GetType()==type);var components=bp.ComponentsArray;
        var binder=(GuidClassBinder)Kingmaker.Blueprints.JsonSystem.Json.Serializer.Binder;var toType=(Dictionary<string,Type>)Get(binder,"m_GuidToTypeCache");var toGuid=(Dictionary<Type,string>)Get(binder,"m_TypeToGuidCache");var beforeTypes=toType.ToArray();var beforeGuids=toGuid.ToArray();var reading=Kingmaker.Blueprints.JsonSystem.Json.BlueprintBeingRead;
        try {
            binder.AddToCache(type,attr.GuidString);Kingmaker.Blueprints.JsonSystem.Json.BlueprintBeingRead=new BlueprintJsonWrapper(bp);
            var text=new StringWriter();var writer=new JsonTextWriter(text);Kingmaker.Blueprints.JsonSystem.Json.Serializer.Serialize(writer,restriction,typeof(BlueprintComponent));writer.Flush();
            var copy=(BlueprintComponent)Kingmaker.Blueprints.JsonSystem.Json.Serializer.Deserialize(new JsonTextReader(new StringReader(text.ToString())),typeof(BlueprintComponent));
            Check(copy.GetType()==type&&(budgetType?((UrbanSkaldAllocationRestriction01442)copy).Portion==1:copy.Disabled&&((ForbidSpellCasting)copy).IgnoreFeature==oldForbid.IgnoreFeature),"real native polymorphic blueprint serializer restores exact repair type and authored budget/retirement fields");
            bp.ComponentsArray=components.Select(x=>ReferenceEquals(x,restriction)?copy:x).ToArray();bp.OnEnable();Register(c);Check(ReferenceEquals(copy.OwnerBlueprint,bp),"native OnEnable/repeated registration preserve deserialized restriction owner and fingerprint");
        } catch {bp.ComponentsArray=components;bp.OnEnable();throw;}
        finally {toType.Clear();foreach(var pair in beforeTypes)toType.Add(pair.Key,pair.Value);toGuid.Clear();foreach(var pair in beforeGuids)toGuid.Add(pair.Key,pair.Value);Kingmaker.Blueprints.JsonSystem.Json.BlueprintBeingRead=reading;}
        Check(toType.Count==beforeTypes.Length&&toGuid.Count==beforeGuids.Length,"explicit test schema input restores native binder maps");
        }
    }
    static void Mockery(BlueprintsCache c)
    {
        var singer = Unit(c, 12); var target = Unit(c); var friend = Unit(c); var bp = Asset<BlueprintBuff>(c, "UrbanSkaldMockeryBuff01443"); var area = Asset<BlueprintAbilityAreaEffect>(c, "UrbanSkaldMockeryArea01443"); var host = Buff(singer, Asset<BlueprintBuff>(c, "UrbanSkaldMockeryMain01443"));
        Enemy(target); Check(singer.IsEnemy(target) && singer.IsAlly(friend), "actual native faction/group relationships distinguish Mockery enemy and friend"); int saveBefore = (int)AccessTools.Field(Helpers, "saves").GetValue(null); Enter(area, host, friend, true);
        Check((int)AccessTools.Field(Helpers, "saves").GetValue(null) == saveBefore && !friend.Buffs.Enumerable.Any(b => b.Blueprint == bp), "friendly target never reaches Mockery Will save");
        int ac = target.Stats.AC.ModifiedValue, attack = target.Stats.AdditionalAttackBonus.ModifiedValue;
        AccessTools.Field(Helpers, "roll").SetValue(null, 20); Enter(area, host, target, true); Check(!target.Buffs.Enumerable.Any(b => b.Blueprint == bp), "native successful Will save negates Mockery");
        AccessTools.Field(Helpers, "roll").SetValue(null, 1); Enter(area, host, target, true);
        Check(target.Stats.AC.ModifiedValue == ac - 2 && target.Stats.AdditionalAttackBonus.ModifiedValue == attack - 2 && target.State.HasCondition(UnitCondition.SpellCastingIsDifficult), "native failed Will applies exactly -2/-2 and difficult casting");
        var save = (RuleSavingThrow)AccessTools.Field(Helpers, "lastSave").GetValue(null); Check(save.DifficultyClassBase == 20, "actual native Mockery DC10 + Skald12/2 + CHA18/4; actual=" + save.DifficultyClassBase);
        var second = Unit(c, 12); var host2 = Buff(second, Asset<BlueprintBuff>(c, "UrbanSkaldMockeryMain01443")); Enter(area, host2, target, true);
        Check(target.Buffs.Enumerable.Count(b => b.Blueprint == bp) == 1 && target.Stats.AC.ModifiedValue == ac - 2, "native Replace song semantics do not stack penalty from two performers");
        Enter(area, host, target, false); Check(target.Buffs.Enumerable.Any(b => b.Blueprint == bp && b.Context.MaybeCaster == second), "old singer's exit cannot clear newer singer's replacement");
        Enter(area, host2, target, false); Check(!target.Buffs.Enumerable.Any(b => b.Blueprint == bp) && target.Stats.AC.ModifiedValue == ac && !target.State.HasCondition(UnitCondition.SpellCastingIsDifficult), "new singer's exit clears own effect without resurrecting old source");
        var immunity = new SpecificBuffImmunity(); Set(immunity, "m_Buff", Ref<BlueprintBuffReference>(bp)); var protection = (BlueprintBuff)B("Custom", "urban-specific-immunity", new BlueprintComponent[] { immunity }); var protect = Buff(target, protection);
        Enter(area, host, target, true); Check(!target.Buffs.Enumerable.Any(b => b.Blueprint == bp), "native specific effect immunity blocks Mockery even on failed save"); target.Facts.Remove(protect);
        var descriptor = new BuffDescriptorImmunity { Descriptor = SpellDescriptor.MindAffecting }; Set(descriptor, "m_IgnoreFeature", new BlueprintUnitFactReference()); var mind = Buff(target, (BlueprintBuff)B("Custom", "urban-mind-immunity", new BlueprintComponent[] { descriptor }));
        Enter(area, host, target, true); Check(!target.Buffs.Enumerable.Any(b => b.Blueprint == bp), "native mind-affecting descriptor immunity blocks Mockery"); target.Facts.Remove(mind);
        Enter(area, host, target, true); Check(target.Buffs.Enumerable.Any(b => b.Blueprint == bp), "removing immunity restores valid failed-save application without a fabricated 24-hour lockout"); Enter(area, host, target, false);
        singer.Facts.Remove(host); second.Facts.Remove(host2); AccessTools.Field(Helpers, "hostile").SetValue(null, null);
    }
    static void Serialization(BlueprintsCache c)
    {
        // CustomAttributeData does not execute native Guid.Parse. Construct and
        // reflect the actual attribute before exercising polymorphic native JSON.
        var type = typeof(UrbanSkaldCrowd01443);
        var attributes = type.GetCustomAttributes(typeof(TypeIdAttribute), false).Cast<TypeIdAttribute>().ToArray(); nativeTypeIdAttributeReads += attributes.Length;
        Check(attributes.Length == 1 && attributes[0].GuidString == CrowdTypeId && attributes[0].Guid.ToString("N") == CrowdTypeId && Guid.TryParseExact(CrowdTypeId, "N", out var parsed), "real reflected native TypeIdAttribute constructs and parses the exact fresh 32-hex identity");
        var constructed = new TypeIdAttribute(CrowdTypeId); nativeTypeIdAttributeReads++;
        Check(constructed.Guid == attributes[0].Guid && constructed.Guid == Guid.ParseExact(CrowdTypeId, "N"), "direct real native TypeIdAttribute constructor and reflected attribute agree");
        bool rejected = false; try { new TypeIdAttribute("860229c7ab94426cb9130b22f798f954a"); } catch (FormatException) { rejected = true; }
        Check(rejected, "native constructor actually rejects the former 33-character identity");
        var own = type.Assembly.GetTypes().SelectMany(t => t.GetCustomAttributes(typeof(TypeIdAttribute), false).Cast<TypeIdAttribute>()).ToArray(); productionTypeIdsScanned = own.Length;
        var native = typeof(BlueprintsCache).Assembly.GetTypes().SelectMany(t => t.GetCustomAttributes(typeof(TypeIdAttribute), false).Cast<TypeIdAttribute>()).ToArray(); nativeTypeIdsScanned = native.Length;
        Check(own.Count(a => a.Guid == constructed.Guid) == 1 && native.All(a => a.Guid != constructed.Guid), "new identity is unique among all actually constructed production and native TypeId attributes");
        Check(PsychicExpansionTests.NativeRows.All(r => !string.Equals((string)r["blueprint"]["AssetId"], CrowdTypeId, StringComparison.OrdinalIgnoreCase)), "new type identity does not collide with any loaded actual native archived dependency asset identity");
        var bp = Asset<BlueprintFeature>(c, "UrbanSkaldBackOfTheCrowd01443");
        var original = bp.GetComponents<UrbanSkaldCrowd01443>().Single(); var before = bp.ComponentsArray; var previous = Kingmaker.Blueprints.JsonSystem.Json.BlueprintBeingRead;
        // Native startup prepares Assembly-CSharp types, not mod schemas. This
        // explicit fixture schema input tests blueprint JSON polymorphism; game
        // saves use DefaultJsonSettings instead. Neither binder is replaced.
        var binder = (GuidClassBinder)Kingmaker.Blueprints.JsonSystem.Json.Serializer.Binder;
        var guidToType = (Dictionary<string, Type>)Get(binder, "m_GuidToTypeCache"); var typeToGuid = (Dictionary<Type, string>)Get(binder, "m_TypeToGuidCache");
        var oldGuidToType = guidToType.ToArray(); var oldTypeToGuid = typeToGuid.ToArray();
        try {
            binder.AddToCache(type, attributes[0].GuidString);
            Kingmaker.Blueprints.JsonSystem.Json.BlueprintBeingRead = new BlueprintJsonWrapper(bp);
            var text = new StringWriter(); var writer = new JsonTextWriter(text);
            Kingmaker.Blueprints.JsonSystem.Json.Serializer.Serialize(writer, original, typeof(BlueprintComponent)); writer.Flush();
            var json = JObject.Parse(text.ToString());
            Check(((string)json["$type"] ?? "").IndexOf(CrowdTypeId, StringComparison.OrdinalIgnoreCase) >= 0, "real native serializer emits the custom component's actual TypeId for a polymorphic BlueprintComponent field");
            var copy = (BlueprintComponent)Kingmaker.Blueprints.JsonSystem.Json.Serializer.Deserialize(new JsonTextReader(new StringReader(text.ToString())), typeof(BlueprintComponent));
            Check(copy is UrbanSkaldCrowd01443 && !ReferenceEquals(copy, original) && copy.name == original.name, "real native serializer restores exact custom type and authored component name through the base component type");
            bp.ComponentsArray = before.Select(component => ReferenceEquals(component, original) ? copy : component).ToArray(); bp.OnEnable();
            Check(ReferenceEquals(copy.OwnerBlueprint, bp) && bp.GetComponents<UrbanSkaldCrowd01443>().Count() == 1, "real native OnEnable restores the deserialized component to its exact blueprint owner without duplication");
            Register(c); nativeSerializedComponentRoundTrips++;
            Check(ReferenceEquals(bp.GetComponent<UrbanSkaldCrowd01443>(), copy), "normal repeated production registration accepts and retains the genuinely deserialized authored component");
        } catch { bp.ComponentsArray = before; bp.OnEnable(); throw; }
        finally {
            guidToType.Clear(); foreach (var pair in oldGuidToType) guidToType.Add(pair.Key, pair.Value);
            typeToGuid.Clear(); foreach (var pair in oldTypeToGuid) typeToGuid.Add(pair.Key, pair.Value);
            Kingmaker.Blueprints.JsonSystem.Json.BlueprintBeingRead = previous;
        }
        Check(guidToType.Count == oldGuidToType.Length && oldGuidToType.All(p => guidToType.TryGetValue(p.Key, out var value) && ReferenceEquals(value, p.Value)) && typeToGuid.Count == oldTypeToGuid.Length && oldTypeToGuid.All(p => typeToGuid.TryGetValue(p.Key, out var value) && value == p.Value), "explicit fixture schema input restores both original native binder mappings exactly");
    }
    static void Crowd(BlueprintsCache c)
    {
        Check(!typeof(UrbanSkaldCrowd01443).GetInterfaces().Any(t => new[] { "IEntityPositionChangedHandler", "ITickEachRound", "IGlobalSubscriber" }.Contains(t.Name)), "Crowd subscribes to AC rules, with no movement, frame or global tick subscription");
        foreach (int level in new[] { 2, 3, 8, 9, 14, 15, 20 }) {
            var owner = Unit(c, level); var first = Unit(c); var second = Unit(c); var enemy = Unit(c); Fact(owner, Asset<BlueprintFeature>(c, "UrbanSkaldBackOfTheCrowd01443"));
            Set(owner, "m_Position", new UnityEngine.Vector3(0, 0, 0)); Set(first, "m_Position", new UnityEngine.Vector3(1, 0, 0)); Set(second, "m_Position", new UnityEngine.Vector3(0, 0, 1)); Set(enemy, "m_Position", new UnityEngine.Vector3(0, 0, 1)); SceneUnits.Clear(); SceneUnits.AddRange(new[] { owner, first, second, enemy }); Enemy(enemy);
            int Bonus() { var rule = new RuleCalculateAC(enemy, owner, AttackType.Melee); Kingmaker.PubSubSystem.RulebookEventBus.OnEventAboutToTrigger(rule); Kingmaker.PubSubSystem.RulebookEventBus.OnEventDidTrigger(rule); nativeCrowdRules++; return rule.AllBonuses.Sum(b => b.Value); }
            int expected = level < 3 ? 0 : level < 9 ? 1 : level < 15 ? 2 : 3; Check(Bonus() == expected, "real native EntityPool/GetTargetsAround two-ally AC at level " + level);
            Set(second, "m_Position", new UnityEngine.Vector3(10, 0, 0)); Check(Bonus() == 0, "one ally plus adjacent enemy grants no AC; moving apart reduces immediately");
            Set(second, "m_Position", new UnityEngine.Vector3(0, 0, 1)); second.State.Features.IsUntargetable.Retain(); Check(Bonus() == 0, "native nearby candidate filter excludes untargetable ally"); second.State.Features.IsUntargetable.Release();
            second.State.UpdateLifeState(UnitLifeState.Dead); Check(second.State.IsDead && Bonus() == 0, "native dead ally is excluded by native nearby enumeration"); second.State.UpdateLifeState(UnitLifeState.Conscious);
            Check(Bonus() == expected, "returning ally restores exactly one current-rule contribution"); int buffs = owner.Buffs.Enumerable.Count(); var watch = System.Diagnostics.Stopwatch.StartNew(); for (int i = 0; i < 1000; i++) if (Bonus() != expected) throw new Exception("unstable Crowd rule"); watch.Stop();
            Check(owner.Buffs.Enumerable.Count() == buffs, "1000 queries neither accumulate nor add/remove persistent AC buffs");
        }
        SceneUnits.Clear(); AccessTools.Field(Helpers, "hostile").SetValue(null, null);
    }
    internal static int Run(BlueprintsCache cache)
    {
        checks = serial = nativeSongs = nativeAreas = nativeCrowdRules = cacheFaults = appendFaults = nativeTypeIdAttributeReads = nativeSerializedComponentRoundTrips = nativeTypeIdsScanned = productionTypeIdsScanned = 0; Units.Clear(); SceneUnits.Clear();
        FriendsFaction = new BlueprintFaction { name = "043-friendly", AssetGuid = BlueprintGuid.Parse("04300000000000000000000000000001") }; EnemiesFaction = new BlueprintFaction { name = "043-enemy", AssetGuid = BlueprintGuid.Parse("04300000000000000000000000000002") }; Friends = new UnitGroup("043-friends"); Enemies = new UnitGroup("043-enemies"); Friends.AttackFactions.Add(EnemiesFaction); Enemies.AttackFactions.Add(FriendsFaction);
        var h = new Harmony("CotW01443.UrbanSkaldContracts"); var game = Game.Instance; var priorState = game.State; var priorTime = game.TimeController; var originalCache = ResourcesLibrary.BlueprintsCache;
        var currentField = AccessTools.Field(Helpers, "current"); var priorContext = currentField.GetValue(null); var hostileField = AccessTools.Field(Helpers, "hostile"); var priorHostile = hostileField.GetValue(null); currentField.SetValue(null, null); hostileField.SetValue(null, null);
        var helperUnits = (IList)AccessTools.Field(Helpers, "units").GetValue(null); int helperCount = helperUnits.Count;
        var state = Empty<PersistentState>(); var player = Empty<Player>(); player.GameTime = TimeSpan.FromHours(1); Set(player, "m_Party", new List<UnitEntityData>()); Set(player, "m_CharacterListsValid", true); state.PlayerState = player; var pool = Empty<EntityPool<UnitEntityData>>(); Set(pool, "m_Entities", SceneUnits); Set(state, "Units", pool); Set(game, "State", state); Set(game, "TimeController", Empty<TimeController>());
        var endpoints = new[] { new { Method = (MethodBase)AccessTools.Method(typeof(UnitHelper), "AddBuff", new[] { typeof(UnitDescriptor), typeof(BlueprintBuff), typeof(MechanicsContext), typeof(TimeSpan?) }), Prefix = AccessTools.Method(typeof(Psychic070Tests), "CaptureDefense") }, new { Method = (MethodBase)AccessTools.Method(typeof(RulebookEvent.Dice), "D", new[] { typeof(DiceFormula) }), Prefix = AccessTools.Method(typeof(Psychic070Tests), "ControlledDice") }, new { Method = (MethodBase)AccessTools.PropertyGetter(typeof(UnitDescriptor), "IsPlayerFaction"), Prefix = AccessTools.Method(typeof(Psychic070Tests), "Faction") } };
        var suspended = endpoints.SelectMany(e => (Harmony.GetPatchInfo(e.Method)?.Prefixes.AsEnumerable() ?? Enumerable.Empty<Patch>()).Where(p => p.PatchMethod == e.Prefix).Select(p => new { Original = e.Method, Record = p })).ToArray();
        void Scene(MethodBase method, string name) => h.Patch(method, prefix: new HarmonyMethod(Helpers, name));
        Exception failure = null;
        try {
            foreach (var p in suspended) h.Unpatch(p.Original, p.Record.PatchMethod);
            h.Patch(AccessTools.PropertyGetter(typeof(UnitDescriptor), "IsPlayerFaction"), prefix: new HarmonyMethod(typeof(UrbanSkald01443Tests), nameof(PlayerFaction)));
            Scene(AccessTools.Method(typeof(FeatureCollection), "CheckStatPrerequisitesAndUpdate"), "Skip"); Scene(AccessTools.PropertyGetter(typeof(Game), "LevelUpController"), "Controller"); Scene(AccessTools.Method(typeof(EntityFact), "Attach"), "Identity"); Scene(AccessTools.Method(typeof(UnitState), "UpdateStatusEffect"), "Skip"); Scene(AccessTools.Method(typeof(UnitEntityData), "UpdateGroup"), "Skip");
            Scene(AccessTools.PropertyGetter(typeof(TimeController), "GameTime"), "Time"); Scene(AccessTools.PropertyGetter(typeof(TimeController), "RealTime"), "Time"); Scene(AccessTools.PropertyGetter(typeof(Game), "CurrentMode"), "Mode"); Scene(AccessTools.PropertyGetter(typeof(UnitEntityData), "IsPlayersEnemy"), "False"); Scene(AccessTools.Method(typeof(TurnBased.Controllers.CombatController), "IsInTurnBasedCombat"), "False"); Scene(AccessTools.PropertyGetter(typeof(Kingmaker.Armies.TacticalCombat.TacticalCombatHelper), "IsActive"), "False"); Scene(AccessTools.Method(typeof(AddAreaEffect), "IsOwnerAvatarValid"), "False"); Scene(AccessTools.PropertyGetter(typeof(Rulebook), "CurrentContext"), "Context");
            Scene(AccessTools.Method(typeof(RulebookEvent.Dice), "D", new[] { typeof(DiceFormula) }), "Dice"); Scene(AccessTools.PropertyGetter(typeof(RulebookEvent.Dice), "D20"), "D20");
            Scene(AccessTools.Method(typeof(Kingmaker.UnitLogic.Commands.UnitCommands), "Run", new[] { typeof(Kingmaker.UnitLogic.Commands.Base.UnitCommand) }), "Submit"); Scene(AccessTools.Method(typeof(UnitEntityData), "IsCurrentUnit"), "CurrentUnit"); Scene(AccessTools.Method(typeof(Player), "IsTurnBasedModeOn"), "False");
            Scene(AccessTools.Method(typeof(Kingmaker.Controllers.Units.UnitActivatableAbilitiesController), "GetTimeToNextRound"), "NextRound"); Scene(AccessTools.Method(typeof(Kingmaker.Controllers.Units.UnitActivatableAbilitiesController), "CanTickNewRound"), "TickAllowed");
            Scene(AccessTools.Method(typeof(Kingmaker.QA.LogChannelEx), "ExceptionWithReport", new[] { typeof(Owlcat.Runtime.Core.Logging.LogChannel), typeof(Exception), typeof(string), typeof(object[]) }), "Expose");
            Scene(AccessTools.Method(typeof(Owlcat.Runtime.Core.Logging.LogChannel), "Log", new[] { typeof(string), typeof(object[]) }), "Skip");
            // The full suite may have JIT-inlined Log before this suite's
            // prefix exists. Rebuild only this logging call site; the complete
            // native disabled-component/deactivation/count algorithm remains.
            h.Patch(AccessTools.Method(typeof(EntityFact),"ApplyPostLoadFixes"),transpiler:new HarmonyMethod(typeof(UrbanSkald01443Tests),nameof(PostLoadLogBoundary)));
            Scene(AccessTools.Method(typeof(Kingmaker.Designers.GameHelper), "IsAttackingGreenNPC", new[] { typeof(UnitEntityData), typeof(UnitEntityData) }), "False");
            foreach (var method in new[] { AccessTools.Method(typeof(ContextActionSavingThrow), "RunAction"), AccessTools.Method(typeof(RuleSavingThrow), "OnTrigger"), AccessTools.Method(typeof(RuleSavingThrow), "RollD20"), AccessTools.Method(typeof(ContextActionApplyBuff), "RunAction"), AccessTools.Method(typeof(ContextCalculateAbilityParams), "Calculate", new[] { typeof(MechanicsContext), typeof(BlueprintScriptableObject), typeof(UnitEntityData), typeof(AbilityData) }) }) h.Patch(method, transpiler: new HarmonyMethod(Helpers, "RuleBoundary"));
            h.Patch(AccessTools.Method(typeof(RuleCalculateAbilityParams), "OnTrigger"), transpiler: new HarmonyMethod(typeof(Migration01426Tests), "DifficultyBoundary")); h.Patch(AccessTools.Method(typeof(RuleCanApplyBuff), "OnTrigger"), transpiler: new HarmonyMethod(Helpers, "DifficultyBoundary"));
            Provider01419Tests.PatchLabels(h); Hydrate(cache, false); Registration(cache, h); Register(cache); Serialization(cache); Songs(cache); Mockery(cache); Crowd(cache); Register(cache);
            Check(nativeSongs > 0 && nativeAreas > 0 && nativeCrowdRules >= 7000 && cacheFaults == 1 && appendFaults == 2, "all claimed native paths and first/published transaction failure endpoints executed");
            PsychicExpansionTests.Inventory["urbanSkald01443Contracts"] = new JObject { ["passed"] = true, ["assertions"] = checks, ["additionalItemCount"] = 1, ["itemIds"] = new JArray("COTW-0031"), ["newSpellRootCount"] = 0, ["archetypeCount"] = 1, ["privateResourceCount"] = Tokens.Length, ["newSerializedComponentTypeCount"] = 3, ["newHarmonyPatchCount"] = 0, ["nativeControlledSongApplications"] = nativeSongs, ["nativeAreaCallbacks"] = nativeAreas, ["nativeCrowdRules"] = nativeCrowdRules, ["cacheWriteFailures"] = cacheFaults, ["appendFailures"] = appendFaults,
                ["newBlueprints"] = new JArray(Tokens.Select(t => new JObject { ["token"] = t, ["id"] = Id(t).ToString(), ["type"] = cache.Load(Id(t)).GetType().FullName })),
                ["authoredGraphMutationRejected"] = true, ["warmedDonorElementOwnershipPreserved"] = true, ["exactOwnerReferenceGuards"] = true, ["foreignOwnerCases"] = 6, ["publishedAppendRollback"] = true, ["nativeFactionGroups"] = true, ["nativePerformanceResources"] = true, ["nativeRagePowerSharing"] = true, ["nativeAcceptRefuseMasterMythic"] = true, ["nativeMockerySaveImmunityReplace"] = true, ["nativeCrowdEnumerationAndEventBus"] = true, ["noMovementOrTickSubscription"] = true,
                ["crowdTypeId"] = CrowdTypeId, ["nativeTypeIdAttributeReads"] = nativeTypeIdAttributeReads, ["nativeSerializedComponentRoundTrips"] = nativeSerializedComponentRoundTrips, ["nativeTypeIdsScanned"] = nativeTypeIdsScanned, ["productionTypeIdsScanned"] = productionTypeIdsScanned, ["fixtureSchemaRegistration"] = true,
                ["boundary"] = "Actual native TypeIdAttribute construction/reflection, all production/native TypeId collision checks, polymorphic Json.Serializer roundtrip through BlueprintComponent and real OnEnable owner restoration execute. A real GuidClassBinder.AddToCache call supplies this custom type as explicit fixture schema input and both original mappings are fully restored. Native PrepareTypeCache scans Assembly-CSharp only; this does not imply game registration of CotW types. Production builds this component in memory and saves use DefaultJsonSettings with its separate default binder, not blueprint Json.Serializer. The restored Crowd component is then used by the native rule test. Actual game JSON, native fact/child ownership, AddFactsFromCaster, ContextRankConfig/AddContextStatBonus, TryStart/performance-controller resource rounds, accept/refuse/Master/mythic actions, native area enter/exit and Will/RuleCanApplyBuff execute. Crowd uses original GameHelper.GetTargetsAround, actual EntityPool enumeration, native distance filters, native faction/group IsAlly/IsEnemy and native RulebookEventBus dispatch to the attached component. It does not execute the final RuleCalculateAC.OnTrigger/full scene attack resolution. World clock, avatar allocation, player identity, green-NPC protection and rulebook/difficulty service endpoints use established controlled scene boundaries. Full area frame scheduling/Unity geometry, UI, save/load and presentation remain game observation steps; no game process was launched." };
            Console.WriteLine("PASS: " + checks + " UrbanSkald01443 contracts."); return checks;
        } catch (Exception e) { failure = e; throw; }
        finally {
            foreach (var u in Units.AsEnumerable().Reverse()) foreach (var f in u.Facts.List.ToArray()) if (f.Manager != null) try { u.Facts.Remove(f); } catch (Exception e) { if (failure == null) throw; Console.Error.WriteLine("Urban cleanup after primary: " + e.Message); }
            h.UnpatchAll(h.Id); foreach (var saved in suspended) { var p = saved.Record; new Harmony(p.owner).Patch(saved.Original, prefix: new HarmonyMethod(p.PatchMethod) { priority = p.priority, before = p.before, after = p.after, debug = p.debug }); }
            Set(game, "State", priorState); Set(game, "TimeController", priorTime); AccessTools.Field(typeof(ResourcesLibrary), "BlueprintsCache").SetValue(null, originalCache); currentField.SetValue(null, priorContext); hostileField.SetValue(null, priorHostile); while (helperUnits.Count > helperCount) helperUnits.RemoveAt(helperUnits.Count - 1);
        }
    }
}

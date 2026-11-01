using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.Serialization;
using HarmonyLib;
using Newtonsoft.Json.Linq;
using Kingmaker;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Prerequisites;
using Kingmaker.Blueprints.Classes.Selection;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.Controllers;
using Kingmaker.EntitySystem;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Enums;
using Kingmaker.Enums.Damage;
using Kingmaker.Items;
using Kingmaker.PubSubSystem;
using Kingmaker.RuleSystem;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.RuleSystem.Rules.Abilities;
using Kingmaker.RuleSystem.Rules.Damage;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components;
using Kingmaker.UnitLogic.ActivatableAbilities;
using Kingmaker.UnitLogic.Buffs;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Class.LevelUp;
using Kingmaker.UnitLogic.FactLogic;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Parts;
using Kingmaker.Utility;
using CallOfTheWild.Wrath.Prototype;

// The real game assembly supplies all donors, managers, rule subscriptions and
// calculation/activation bodies. Clock, unit/scene setup and display are bounded
// fixture endpoints. No process, rendered UI, animation or full game save runs.
internal static class Invoker01437Tests
{
    static Type Module => typeof(InvokerFacetRules01437).Assembly.GetType("CallOfTheWild.Wrath.Prototype.Invoker01437", true);
    static int checks, serial, invocationCalls, spendCalls, parameterCalls, damageCalls, fault;
    static readonly List<UnitEntityData> units = new List<UnitEntityData>();
    static object Get(object o, string n) => (AccessTools.Field(o.GetType(), n) ?? AccessTools.Field(o.GetType(), "<" + n + ">k__BackingField")).GetValue(o);
    static void Set(object o, string n, object v) => (AccessTools.Field(o.GetType(), n) ?? AccessTools.Field(o.GetType(), "<" + n + ">k__BackingField")).SetValue(o, v);
    static T Empty<T>() where T : class => (T)FormatterServices.GetUninitializedObject(typeof(T));
    static object Call(Type t, string n, params object[] args) => AccessTools.Method(t, n).Invoke(null, args);
    static string C(string name) => (string)AccessTools.Field(Module, name).GetRawConstantValue();
    static string[] Own => (string[])AccessTools.Field(Module, "OwnTokens").GetValue(null);
    static string[] Facets => (string[])AccessTools.Field(Module, "Facets").GetValue(null);
    static BlueprintGuid Id(string t) => (BlueprintGuid)Call(Module.Assembly.GetType("CallOfTheWild.Wrath.Prototype.PsychicAssets"), "Id", t);
    static T Asset<T>(BlueprintsCache c, string token) where T : SimpleBlueprint => (T)c.Load(Id(token));
    static T Native<T>(BlueprintsCache c, string id) where T : SimpleBlueprint => (T)c.Load(BlueprintGuid.Parse(id));
    static T Ref<T>(SimpleBlueprint bp) where T : BlueprintReferenceBase, new() { var r = BlueprintReferenceBase.CreateTyped<T>(bp); AccessTools.Property(typeof(BlueprintReferenceBase), "Cached").SetValue(r, bp); return r; }
    static void Check(bool result, string reason) { if (!result) throw new Exception("Invoker01437: " + reason); checks++; }
    static void Register(BlueprintsCache c) => Call(Module, "Register", c);
    static bool Reject(Action call) { try { call(); return false; } catch (Exception e) { while (e is TargetInvocationException) e = e.InnerException; return e is InvalidOperationException; } }
    static bool Skip() => false;
    static bool False(ref bool __result) { __result = false; return false; }
    static bool Clock(ref TimeSpan __result) { __result = TimeSpan.FromHours(1); return false; }
    static bool Controller(ref LevelUpController __result) { __result = null; return false; }
    static void Identity(EntityFact __instance) { if (string.IsNullOrEmpty(__instance.UniqueId)) Set(__instance, "UniqueId", "037-invoker-fact-" + ++serial); }
    static bool Expose(Exception __1) => throw new Exception("037 invoker native exception", __1);
    static void FaultAfterAdd(SimpleBlueprint __1) { if (fault > 0 && __1.AssetGuid == Id("CrisisInvokerEffect01437")) { fault--; throw new InvalidOperationException("037 invoker native cache insertion fault"); } }

    internal static void PrepareNative(BlueprintsCache cache)
    {
        // NativeRows contains exact JSON extracted from the installed game's
        // blueprints.zip. Rehydrate only the original class/progression shape;
        // runtime private additions are preserved on an already connected cache.
        var tokens = new[] { C("WitchClassId"), C("WitchProgressionId"), C("HexPoolId"), C("PatronPoolId") };
        foreach (var row in PsychicExpansionTests.NativeRows.Where(r => tokens.Contains((string)r["blueprint"]["AssetId"]) || ((string)r["name"]).StartsWith("Witch", StringComparison.Ordinal) || ((string)r["name"]) == "WizardSpellbook")) {
            var bp = cache.Load(BlueprintGuid.Parse((string)row["blueprint"]["AssetId"])) as BlueprintScriptableObject;
            if (bp == null) continue;
            var data = (JObject)row["blueprint"]["Data"];
            if (bp is BlueprintCharacterClass) foreach (string field in new[] { "m_Progression", "m_Spellbook" }) if (data[field] != null) {
                var fi = AccessTools.Field(bp.GetType(), field); fi.SetValue(bp, PsychicExpansionTests.Read(data[field], fi.FieldType));
            }
            if (bp is BlueprintSpellbook) foreach (string field in new[] { "m_CharacterClass", "CastingAttribute" }) if (data[field] != null) {
                var fi = AccessTools.Field(bp.GetType(), field); fi.SetValue(bp, PsychicExpansionTests.Read(data[field], fi.FieldType));
            }
            if (bp is BlueprintCharacterClass && data["m_Archetypes"] != null && !((BlueprintArchetypeReference[])Get(bp, "m_Archetypes") ?? Array.Empty<BlueprintArchetypeReference>()).Any(r => r.Guid == Id(C("RootToken")))) {
                var fi = AccessTools.Field(bp.GetType(), "m_Archetypes"); fi.SetValue(bp, PsychicExpansionTests.Read(data[fi.Name], fi.FieldType));
            }
            if (bp is BlueprintProgression progression) {
                if (data["LevelEntries"] != null) progression.LevelEntries = (LevelEntry[])PsychicExpansionTests.Read(data["LevelEntries"], typeof(LevelEntry[]));
                if (data["UIGroups"] != null && !(progression.UIGroups ?? Array.Empty<UIGroup>()).Any(g => g.Features.Any(f => f?.AssetGuid == Id(C("FeatureToken")))))
                    progression.UIGroups = (UIGroup[])PsychicExpansionTests.Read(data["UIGroups"], typeof(UIGroup[]));
            }
        }
    }
    static UnitEntityData Unit(BlueprintsCache cache, string label, int level)
    {
        var unit = (UnitEntityData)Call(typeof(BardMasterpieces01435Tests), "Unit", cache, "invoker-" + label, C("WitchClassId"), level);
        Set(unit, "UniqueId", "037-invoker-unit-" + ++serial); unit.Stats.Strength.BaseValue = unit.Stats.Dexterity.BaseValue = unit.Stats.Constitution.BaseValue = unit.Stats.Wisdom.BaseValue = 10;
        unit.Stats.SaveFortitude.BaseValue = unit.Stats.SaveReflex.BaseValue = unit.Stats.SaveWill.BaseValue = 0; units.Add(unit); return unit;
    }
    static Feature Add(UnitEntityData u, BlueprintFeature bp) { var feature = new Feature(bp, u.Descriptor); u.Facts.Add(feature); Check(feature.IsActive && feature.IsTurnedOn, "native feature activates " + bp.name); return feature; }
    static Buff Buff(UnitEntityData u, BlueprintBuff bp) { var buff = new Buff(bp, new MechanicsContext(u, u.Descriptor, bp), null); u.Facts.Add(buff); Check(buff.IsActive && buff.IsTurnedOn, "native buff activates " + bp.name); return buff; }
    static Feature Grant(BlueprintsCache c, UnitEntityData u) => Add(u, Asset<BlueprintFeature>(c, C("FeatureToken")));
    static ActivatableAbility Toggle(UnitEntityData u, int facet) => u.ActivatableAbilities.Enumerable.Single(a => a.Blueprint.AssetGuid == Id(Facets[facet] + "InvokerToggle01437"));
    static void Start(UnitEntityData u, int facet) { var t = Toggle(u, facet); t.IsOn = true; t.TryStart(); Check(t.IsStarted && t.AppliedBuff != null, "native TryStart attaches preference " + facet + " started=" + t.IsStarted + " on=" + t.IsOn + " available=" + t.IsAvailable + " restrictions=" + t.IsAvailableByRestrictions + " resource=" + t.IsAvailableByResources + " ownerClassLevel=" + Call(Module, "Level", u) + " has=" + Call(Module, "Has", u)); }
    static void Stop(UnitEntityData u, int facet) { var t = Toggle(u, facet); t.IsOn = false; t.Stop(); }
    static AbilityData Invocation(BlueprintsCache c, UnitEntityData unit) => new AbilityData(Asset<BlueprintAbility>(c, C("AbilityToken")), unit.Descriptor);
    static AbilityExecutionContext Context(AbilityData ability, UnitEntityData unit) => new AbilityExecutionContext(ability, new AbilityParams { CasterLevel = 16, SpellLevel = 1, DC = 16 }, unit, null, unit);
    static void Invoke(BlueprintsCache c, UnitEntityData unit)
    {
        var ability = Invocation(c, unit); Check(ability.Blueprint.GetComponent<InvokerInvocationRestriction01437>().IsAbilityRestrictionPassed(ability), "production pre-cast facet check passes");
        int previous = unit.Descriptor.Resources.GetResourceAmount(Asset<BlueprintAbilityResource>(c, C("ResourceToken")));
        ability.ResourceLogic.Spend(ability); spendCalls++;
        Check(unit.Descriptor.Resources.GetResourceAmount(Asset<BlueprintAbilityResource>(c, C("ResourceToken"))) == previous - 1, "native ability resource Spend consumes exactly one minute");
        AbilityExecutionProcess.ApplyEffectImmediate(Context(ability, unit), unit); invocationCalls++;
    }
    static RulebookSubscriptionManager<IInitiatorRulebookSubscriber> Subscribers(UnitEntityData unit)
    {
        var result = new RulebookSubscriptionManager<IInitiatorRulebookSubscriber>();
        foreach (var fact in unit.Facts.List.Where(f => f.IsActive && f.IsTurnedOn && !f.IsDisposed && (!(f is Buff b) || !b.IsSuppressed)))
            foreach (var runtime in fact.Components) if (runtime.SourceBlueprintComponent is IInitiatorRulebookSubscriber subscriber) result.Subscribe(subscriber, (ISubscriptionProxy)runtime);
        return result;
    }
    static T Rule<T>(T rule, bool body = false) where T : RulebookEvent
    {
        var bus = Subscribers(rule.Initiator); var context = new RulebookEventContext(); context.PushEvent(rule);
        bus.OnEventAboutToTrigger(rule); if (body) rule.OnTrigger(context); bus.OnEventDidTrigger(rule);
        if (rule is RuleCalculateAbilityParams) parameterCalls++; if (rule is RuleCalculateDamage) damageCalls++; return rule;
    }
    static BlueprintsCache Fresh(BlueprintsCache source)
    {
        var fresh = Empty<BlueprintsCache>(); foreach (var field in typeof(BlueprintsCache).GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)) field.SetValue(fresh, field.GetValue(source));
        var loaded = (IDictionary)Get(source, "m_LoadedBlueprints"); var copied = (IDictionary)Activator.CreateInstance(loaded.GetType()); var own = Own.Select(Id).ToArray();
        foreach (var key in loaded.Keys.Cast<BlueprintGuid>().ToArray()) if (!own.Contains(key)) copied.Add(key, loaded[key]); Set(fresh, "m_LoadedBlueprints", copied);
        var witch = (BlueprintCharacterClass)AccessTools.Method(typeof(object), "MemberwiseClone").Invoke(Native<BlueprintCharacterClass>(source, C("WitchClassId")), null);
        var progression = (BlueprintProgression)AccessTools.Method(typeof(object), "MemberwiseClone").Invoke(Native<BlueprintProgression>(source, C("WitchProgressionId")), null);
        Set(witch, "m_Archetypes", ((BlueprintArchetypeReference[])Get(witch, "m_Archetypes")).Where(r => r.Guid != Id(C("RootToken"))).ToArray());
        progression.UIGroups = progression.UIGroups.Where(g => !g.Features.Any(f => f?.AssetGuid == Id(C("FeatureToken")))).ToArray();
        Set(witch, "m_Progression", Ref<BlueprintProgressionReference>(progression)); fresh.AddCachedBlueprint(witch.AssetGuid, witch); fresh.AddCachedBlueprint(progression.AssetGuid, progression); return fresh;
    }
    static void Registration(BlueprintsCache cache, Harmony h)
    {
        Register(cache); var witch = Native<BlueprintCharacterClass>(cache, C("WitchClassId")); var progression = Native<BlueprintProgression>(cache, C("WitchProgressionId"));
        var refs = Get(witch, "m_Archetypes"); var groups = progression.UIGroups; var assets = Own.Select(t => cache.Load(Id(t))).ToArray();
        Check(assets.Length == 32 && assets.All(a => a != null) && assets.Select(a => a.AssetGuid).Distinct().Count() == 32, "all 32 unique owned assets published");
        Register(cache); Call(Module, "RegisterOptional", cache);
        Check(ReferenceEquals(refs, Get(witch, "m_Archetypes")) && ReferenceEquals(groups, progression.UIGroups) && Own.Select(t => cache.Load(Id(t))).SequenceEqual(assets), "actual duplicate core/optional register preserves exact graph and native arrays");
        var root = Asset<BlueprintArchetype>(cache, C("RootToken"));
        Check(root.RemoveFeatures.Select(e => e.Level).SequenceEqual(new[] { 1, 8, 10, 16 }) && root.RemoveFeatures.All(e => e.Features.Count == 1 && e.Features[0].AssetGuid == BlueprintGuid.Parse(C("HexPoolId"))), "four actual hex selections replaced and no other class features removed");
        Check(root.AddFeatures.Select(e => e.Level).SequenceEqual(new[] { 1, 10 }), "invoke and second invocation assigned to native archetype levels 1 and 10");
        Register(cache); Check(true, "native LevelEntry.Features lazy proxy initialization is not an authored identity change");
        int authoredLevel = root.RemoveFeatures[0].Level; root.RemoveFeatures[0].Level = 2;
        try { Check(Reject(() => Register(cache)), "authored archetype remove-level tampering is still rejected"); } finally { root.RemoveFeatures[0].Level = authoredLevel; }
        var authoredRefs = Get(root.AddFeatures[0], "m_Features"); Set(root.AddFeatures[0], "m_Features", new List<BlueprintFeatureBaseReference>());
        try { Check(Reject(() => Register(cache)), "authored archetype raw feature reference tampering is still rejected"); } finally { Set(root.AddFeatures[0], "m_Features", authoredRefs); }
        Check(((BlueprintArchetypeReference[])refs).Count(r => r.Guid == root.AssetGuid) == 1 && groups.Count(g => g.Features.Any(f => f?.AssetGuid == Id(C("FeatureToken")))) == 1, "exact class archetype/UI registration once");
        var fresh = Fresh(cache); var cls = Native<BlueprintCharacterClass>(fresh, C("WitchClassId")); var prog = Native<BlueprintProgression>(fresh, C("WitchProgressionId")); var beforeRefs = Get(cls, "m_Archetypes"); var beforeGroups = prog.UIGroups;
        var foreign = new BlueprintFeature { AssetGuid = Id(C("RootToken")), name = "foreign same GUID", ComponentsArray = Array.Empty<BlueprintComponent>() }; foreign.OnEnable(); fresh.AddCachedBlueprint(foreign.AssetGuid, foreign);
        Check(Reject(() => Register(fresh)) && ReferenceEquals(fresh.Load(foreign.AssetGuid), foreign) && ReferenceEquals(Get(cls, "m_Archetypes"), beforeRefs) && ReferenceEquals(prog.UIGroups, beforeGroups), "foreign private root collision preserves native parents and foreign resource"); fresh.RemoveCachedBlueprint(foreign.AssetGuid);
        h.Patch(AccessTools.Method(typeof(BlueprintsCache), "AddCachedBlueprint"), postfix: new HarmonyMethod(typeof(Invoker01437Tests), nameof(FaultAfterAdd)));
        fault = 1; Check(Reject(() => Register(fresh)) && fault == 0 && Own.All(t => fresh.Load(Id(t)) == null) && ReferenceEquals(Get(cls, "m_Archetypes"), beforeRefs) && ReferenceEquals(prog.UIGroups, beforeGroups), "real AddCachedBlueprint postmutation failure rolls back every private resource and exact parent arrays");
        Register(fresh); Check(Own.All(t => fresh.Load(Id(t)) != null), "same cache retries cleanly after real partial publication failure");
        var first = Asset<BlueprintBuff>(cache, "BondageInvokerEffect01437"); var components = first.ComponentsArray; first.ComponentsArray = Array.Empty<BlueprintComponent>();
        try { Check(Reject(() => Register(cache)) && ReferenceEquals(refs, Get(witch, "m_Archetypes")), "tampered authored effect is rejected without class mutation"); } finally { first.ComponentsArray = components; }
    }
    static void Invocations(BlueprintsCache cache)
    {
        foreach (int level in new[] { 1, 7, 8, 9, 10, 15, 16, 20 }) {
            var unit = Unit(cache, "facet-level-" + level, level); var grant = Grant(cache, unit); var resource = Asset<BlueprintAbilityResource>(cache, C("ResourceToken"));
            Check(unit.Descriptor.Resources.GetResourceAmount(resource) == level && resource.GetMaxAmount(unit.Descriptor) == level, "actual native daily pool equals witch level " + level);
            Check(unit.ActivatableAbilities.Enumerable.Count(a => Facets.Any(f => a.Blueprint.AssetGuid == Id(f + "InvokerToggle01437"))) == 9, "native AddFacts grants every facet preference once");
            var ability = Invocation(cache, unit); Check(!ability.Blueprint.GetComponent<InvokerInvocationRestriction01437>().IsAbilityRestrictionPassed(ability), "zero selected facets blocks invocation before spend");
            Start(unit, 0); Check(!Toggle(unit, 2).IsAvailableByRestrictions || level >= 10, "actual activatable native restriction capacity at " + level);
            if (level >= 10) { Start(unit, 2); Check(!Toggle(unit, 4).IsAvailableByRestrictions, "native third facet rejected at level " + level); }
            Invoke(cache, unit); int bonus = level >= 16 ? 6 : level >= 8 ? 4 : 2;
            Check(unit.Stats.Strength.ModifiedValue == 10 + bonus && unit.Stats.Constitution.ModifiedValue == 10 + (level >= 10 ? bonus : 0), "actual native rank-config/stat-bonus effect scaling " + level);
            var effects = unit.Buffs.Enumerable.Where(b => b.Blueprint.AssetGuid == Id("BondageInvokerEffect01437") || b.Blueprint.AssetGuid == Id("CrisisInvokerEffect01437")).ToArray();
            Check(effects.Length == (level >= 10 ? 2 : 1) && effects.All(b => b.IsNotDispelable && b.EndTime == TimeSpan.FromHours(1) + TimeSpan.FromMinutes(1)), "one-minute native buff lease exactly matches activation time and both facets");
            Stop(unit, 0); if (level >= 10) Stop(unit, 2); Start(unit, 4);
            Check(unit.Stats.Strength.ModifiedValue == 10 + bonus && unit.Stats.Dexterity.ModifiedValue == 10, "free preference changes cannot replace paid active effect");
            if (level > 1) { Invoke(cache, unit); Check(unit.Stats.Strength.ModifiedValue == 10 && unit.Stats.Constitution.ModifiedValue == 10 && unit.Stats.Dexterity.ModifiedValue == 10 + bonus, "reactivation spends a fresh minute and replaces exact prior own facets"); }
            if (level == 1) Check(!ability.AbilityResourceLogic.IsAbilityRestrictionPassed(ability), "native resource restriction blocks another invocation at zero daily minutes");
            foreach (var b in unit.Buffs.Enumerable.Where(b => Facets.Any(f => b.Blueprint.AssetGuid == Id(f + "InvokerEffect01437"))).ToArray()) { unit.Buffs.TickBuff(b, b.EndTime); Check(b.IsDisposed, "actual native TickBuff expires a paid facet at its one-minute deadline"); }
            Check(unit.Stats.Strength.ModifiedValue == 10 && unit.Stats.Dexterity.ModifiedValue == 10 && unit.Stats.Constitution.ModifiedValue == 10, "native expiry removes exact stat modifiers without removing the archetype source");
            grant.TurnOff(); Check(unit.Buffs.Enumerable.All(b => !Facets.Any(f => b.Blueprint.AssetGuid == Id(f + "InvokerEffect01437"))) && unit.Stats.Strength.ModifiedValue == 10 && unit.Stats.Dexterity.ModifiedValue == 10, "native source turn-off cleans every owned paid effect");
            unit.Facts.Remove(grant); Check(!unit.HasFact(Asset<BlueprintFeature>(cache, C("FeatureToken"))), "native source removal is final");
        }
        foreach (int level in new[] { 1, 8, 16 }) {
            var u = Unit(cache, "saves-paradise-" + level, level); Grant(cache, u); var fortune = Buff(u, Asset<BlueprintBuff>(cache, "FortuneInvokerEffect01437")); int save = level == 16 ? 5 : level == 8 ? 3 : 1;
            Check(u.Stats.SaveWill.ModifiedValue == save && u.Stats.SaveReflex.ModifiedValue == save && u.Stats.SaveFortitude.ModifiedValue == save, "native all-save resistance rank " + level + " actual=" + u.Stats.SaveWill.ModifiedValue + "/" + u.Stats.SaveReflex.ModifiedValue + "/" + u.Stats.SaveFortitude.ModifiedValue);
            fortune.Deactivate(); Check(u.Stats.SaveWill.ModifiedValue == 0, "actual native deactivate removes resistance"); fortune.Activate(); Check(u.Stats.SaveWill.ModifiedValue == save, "native reactivate restores one resistance modifier"); u.Facts.Remove(fortune);
            var paradise = Buff(u, Asset<BlueprintBuff>(cache, "ParadiseInvokerEffect01437")); int competence = level == 16 ? 9 : level == 8 ? 6 : 3;
            Check(u.Stats.SkillMobility.Modifiers.Any(m => m.ModValue == competence), "native Mobility competence scaling " + level);
            var parameters = Rule(new RuleCalculateAbilityParams(u, Invocation(cache, u)) { ReplaceCasterLevel = 10, ReplaceSpellLevel = 1, ReplaceConcentration = 10 }, true);
            Check((int)Get(parameters, "m_BonusConcentration") == competence && parameters.Result.Concentration == 10 + competence, "actual native ConcentrationBonus/parameter calculation includes Paradise " + level); u.Facts.Remove(paradise);
        }
    }
    static Spellbook Book(BlueprintsCache c, UnitEntityData u, bool witch)
    {
        var b = Empty<Spellbook>(); var bp = Native<BlueprintSpellbook>(c, witch ? "dd04f9239f655ea438976742728e4909" : "5a38c9ac8607890409fcb8f6342da6f4");
        Check(bp != null && bp.CharacterClass?.AssetGuid == BlueprintGuid.Parse(witch ? C("WitchClassId") : "ba34257984f4c41408ce1dc2004e342e"), "real native spellbook class metadata " + witch);
        var books = (IDictionary)Get(u.Descriptor, "m_Spellbooks"); if (books == null) { books = new Dictionary<BlueprintSpellbook, Spellbook>(); Set(u.Descriptor, "m_Spellbooks", books); }
        if (books.Contains(bp)) return (Spellbook)books[bp]; Set(b, "Blueprint", bp); Set(b, "Owner", u.Descriptor); books.Add(bp, b); return b;
    }
    static void Magic(BlueprintsCache cache)
    {
        var patron = Native<BlueprintProgression>(cache, "08518b2a62446c74b9ae08ee73664047");
        var spell = patron.LevelEntries.SelectMany(e => e.Features).SelectMany(f => f.GetComponents<AddKnownSpell>()).First().Spell;
        var slumber = Native<BlueprintAbility>(cache, "630ea63a63457ff4f9de059c578c930a"); var hexFeature = Native<BlueprintFeature>(cache, "498d7668a6086454cb2def921b89bd8f");
        foreach (int level in new[] { 1, 7, 8, 15, 16, 20 }) {
            var u = Unit(cache, "curiosity-" + level, level); Grant(cache, u); Add(u, patron); Add(u, hexFeature); var effect = Buff(u, Asset<BlueprintBuff>(cache, "CuriosityInvokerEffect01437"));
            RuleCalculateAbilityParams Params(BlueprintAbility a, Spellbook book) => Rule(new RuleCalculateAbilityParams(u, a, book) { ReplaceCasterLevel = 10, ReplaceSpellLevel = 3, ReplaceDC = 16, ReplaceStatBonusModifier = 4 }, true);
            Check(Params(spell, Book(cache, u, true)).Result.DC == 16 + (level >= 16 ? 2 : 1), "native patron spell parameter DC uses owned patron " + level);
            Check(Params(spell, Book(cache, u, false)).Result.DC == 16, "same patron spell from another book is excluded");
            Check(Params(slumber, null).Result.DC == 16 + (level >= 16 ? 2 : level >= 8 ? 1 : 0), "native learned witch hex DC uses original CotW progression " + level);
            var patronRows = patron.LevelEntries; patron.LevelEntries = new LevelEntry[] { null };
            try { Check(Params(spell, Book(cache, u, true)).Result.DC == 16, "malformed external patron rows are skipped safely without a false DC bonus"); } finally { patron.LevelEntries = patronRows; }
            var hexComponents = hexFeature.ComponentsArray; hexFeature.ComponentsArray = null;
            try { Check(Params(slumber, null).Result.DC == 16, "missing external hex grant components are skipped safely"); } finally { hexFeature.ComponentsArray = hexComponents; }
            effect.Deactivate(); Check(Params(spell, Book(cache, u, true)).Result.DC == 16, "suppressed/deactivated facet does not add DC"); effect.Activate();
        }
    }
    static void Physical(BlueprintsCache cache)
    {
        foreach (int level in new[] { 1, 8, 16 }) foreach (int dr in new[] { 0, 1, 2, 5, 12 }) foreach (int hardness in new[] { 0, 7 }) {
            var owner = Unit(cache, "bridge-" + level + "-" + dr + "-" + hardness, level); var target = Unit(cache, "bridge-target", 1); Grant(cache, owner); var facet = Buff(owner, Asset<BlueprintBuff>(cache, "BridgeInvokerEffect01437"));
            var physical = new PhysicalDamage(new ModifiableDiceFormula(new DiceFormula(2, DiceType.D6)), 0, PhysicalDamageForm.Slashing); var energy = new EnergyDamage(new DiceFormula(2, DiceType.D6), DamageEnergyType.Fire);
            physical.SetReductionBecauseResistance(dr, null); physical.ReductionBecauseHardness.Set(hardness, null); energy.SetReductionBecauseResistance(dr, null);
            var rule = new RuleCalculateDamage(new RuleDealDamage(owner, target, new DamageBundle(physical, energy))); Rule(rule);
            int expected = Math.Min(dr, level >= 16 ? 10 : level >= 8 ? 5 : 2); Check((int)physical.ReductionPenalty == expected && (int)energy.ReductionPenalty == 0, "physical-only late DR penalty clamps at DR without affecting energy resistance");
            var value = new DamageValue(physical, 30, 30, 30, 1); Check(value.Reduction == dr - expected + hardness && value.FinalValue == Math.Max(0, 30 - dr + expected - hardness), "actual native DamageValue keeps hardness and caps DR at zero");
            Rule(rule); Check((int)physical.ReductionPenalty == expected, "duplicate native subscription delivery does not apply Bridge twice");
            var foreign = new PhysicalDamage(new ModifiableDiceFormula(new DiceFormula(1, DiceType.D6)), 0, PhysicalDamageForm.Slashing); foreign.SetReductionBecauseResistance(dr, null);
            Rule(new RuleCalculateDamage(new RuleDealDamage(target, owner, new DamageBundle(foreign)))); Check((int)foreign.ReductionPenalty == 0, "other units and incoming damage do not borrow Bridge");
            facet.Deactivate(); var inactive = new PhysicalDamage(new ModifiableDiceFormula(new DiceFormula(1, DiceType.D6)), 0, PhysicalDamageForm.Slashing); inactive.SetReductionBecauseResistance(dr, null); Rule(new RuleCalculateDamage(new RuleDealDamage(owner, target, new DamageBundle(inactive)))); Check((int)inactive.ReductionPenalty == 0, "native inactive Bridge excluded");
        }
    }
    static bool NeutralDifficulty(ref Kingmaker.Settings.DifficultySettings __result) { __result = Empty<Kingmaker.Settings.DifficultySettings>(); return false; }
    static bool NoShadow(int __2, ref int __result) { __result = __2; return false; }
    static int NeutralDamage(RuleDealDamage rule, int amount) => amount;
    static void NoHitVisual(RuleDealDamage rule) { }
    static int consumedCalculations;
    static T ConsumerRule<T>(T rule)
    {
        // Preserve the native calculate body and real runtime subscriptions.
        // Preparing attack data/scene defenses is a bounded input in this probe.
        if (rule is RuleCalculateDamage calculate) { consumedCalculations++; Rule(calculate, true); }
        return rule;
    }
    static IEnumerable<CodeInstruction> ConsumerBoundary(IEnumerable<CodeInstruction> instructions)
    {
        foreach (var code in instructions) {
            if (code.operand as MethodInfo == AccessTools.Method(typeof(RuleDealDamage), "ApplyDifficultyModifiers")) { code.opcode = OpCodes.Call; code.operand = AccessTools.Method(typeof(Invoker01437Tests), nameof(NeutralDamage)); }
            if (code.operand as MethodInfo == AccessTools.Method(typeof(Kingmaker.Visual.HitSystem.HitPlayer), "PlayDamageHit")) { code.opcode = OpCodes.Call; code.operand = AccessTools.Method(typeof(Invoker01437Tests), nameof(NoHitVisual)); }
            if (code.operand is MethodInfo method && method.DeclaringType == typeof(Rulebook) && method.Name == "Trigger" && method.IsGenericMethod) { code.opcode = OpCodes.Call; code.operand = AccessTools.Method(typeof(Invoker01437Tests), nameof(ConsumerRule)).MakeGenericMethod(method.GetGenericArguments()); }
            yield return code;
        }
    }
    static void BridgeConsumption(BlueprintsCache cache)
    {
        var h = new Harmony("CotW01437.InvokerBridgeConsumption"); consumedCalculations = 0;
        void Patch(MethodBase method, string name) => h.Patch(method, prefix: new HarmonyMethod(typeof(Invoker01437Tests), name));
        try {
            Patch(AccessTools.PropertyGetter(typeof(Kingmaker.Settings.SettingsRoot), "Difficulty"), nameof(NeutralDifficulty)); Patch(AccessTools.PropertyGetter(typeof(UnitEntityData), "IsPlayerFaction"), nameof(False));
            Patch(AccessTools.Method(typeof(RuleCalculateDamage), "ApplyDifficultyBeforeCalculated"), nameof(Skip)); Patch(AccessTools.Method(typeof(RuleCalculateDamage), "ApplyDifficultyForDamageReduction"), nameof(Skip));
            Patch(AccessTools.Method(typeof(ShadowSpellLogicExtension), "TryApplyShadowDamageFactor", new[] { typeof(RulebookEvent), typeof(UnitEntityData), typeof(int) }), nameof(NoShadow));
            Patch(AccessTools.Method(typeof(Kingmaker.UnitLogic.Commands.Base.UnitCommand), "CommandTargetUntargetable"), nameof(False));
            h.Patch(AccessTools.Method(typeof(RuleDealDamage), "OnTrigger"), transpiler: new HarmonyMethod(typeof(Invoker01437Tests), nameof(ConsumerBoundary)));
            foreach (int level in new[] { 1, 8, 16 }) foreach (bool active in new[] { false, true }) {
                var owner = Unit(cache, "bridge-consumer-" + level + "-" + active, level); var target = Unit(cache, "bridge-consumer-target", 1); Grant(cache, owner);
                var facet = Buff(owner, Asset<BlueprintBuff>(cache, "BridgeInvokerEffect01437")); if (!active) facet.Deactivate();
                var physical = new PhysicalDamage(new ModifiableDiceFormula(new DiceFormula(0, DiceType.Zero)), 0, PhysicalDamageForm.Slashing); physical.PreRolledValue = 30; physical.SetReductionBecauseResistance(12, null); physical.ReductionBecauseHardness.Set(7, null);
                var deal = new RuleDealDamage(owner, target, new DamageBundle(physical)) { IsFake = true, Reason = new RuleReason(Context(Invocation(cache, owner), owner)) }; int before = target.Damage;
                deal.OnTrigger(null); int reduction = active ? (level >= 16 ? 10 : level >= 8 ? 5 : 2) : 0;
                Check(deal.Calculate != null && deal.Calculate.CalculatedDamage.Count == 1 && ReferenceEquals(deal.ResultList, deal.Calculate.CalculatedDamage), "native calculate body produces the exact result list consumed by native deal damage");
                Check((int)physical.ReductionPenalty == reduction && deal.Result == 30 - 12 + reduction - 7 && deal.DamageBeforeDifficulty == deal.Result, "native deal consumer sees Bridge OnDid DR change after calculate body at level " + level + " active=" + active);
                Check(target.Damage == before, "IsFake consumer probe computes the real final result without claiming a full HP/death lifecycle");
            }
            Check(consumedCalculations == 6, "six native calculate bodies and corresponding native deal consumption paths execute");
        } finally { h.UnpatchAll(h.Id); }
    }
    static ItemEntityWeapon Weapon(UnitEntityData unit, WeaponCategory kind, string name)
    {
        var weapon = (ItemEntityWeapon)Call(typeof(Linnorm01416Tests), "Weapon", unit, kind, "037-invoker-" + name);
        Set(weapon.Blueprint.Type, "m_IsNatural", kind == WeaponCategory.Bite); weapon.Blueprint.Type.OnEnable(); return weapon;
    }
    static void ReckoningAndRevelation(BlueprintsCache cache)
    {
        var spell = Native<BlueprintAbility>(cache, "4f8181e7a7f1d904fbaea64220e83379");
        foreach (int level in new[] { 1, 7, 8, 15, 16, 20 }) {
            var owner = Unit(cache, "attack-facets-" + level, level); var target = Unit(cache, "attack-target-" + level, 1); Grant(cache, owner);
            var reckoning = Buff(owner, Asset<BlueprintBuff>(cache, "ReckoningInvokerEffect01437"));
            var natural = Weapon(owner, WeaponCategory.Bite, "natural-" + level); var sword = Weapon(owner, WeaponCategory.Longsword, "sword-" + level); var ray = Weapon(owner, WeaponCategory.Ray, "ray-" + level); var touch = Weapon(owner, WeaponCategory.Touch, "touch-" + level);
            MechanicsContext SpellContext(bool witch, AbilityType type = AbilityType.Spell) {
                var ability = new AbilityData(spell, Book(cache, owner, witch), 1); return Context(ability, owner);
            }
            var witchCast = SpellContext(true); var otherCast = SpellContext(false);
            foreach (var sample in new[] { new { Weapon = natural, Context = (MechanicsContext)null, Qualifies = true, Name = "natural" },
                new { Weapon = sword, Context = (MechanicsContext)null, Qualifies = false, Name = "manufactured" }, new { Weapon = ray, Context = witchCast, Qualifies = true, Name = "witch-ray" },
                new { Weapon = touch, Context = witchCast, Qualifies = true, Name = "witch-touch" }, new { Weapon = ray, Context = otherCast, Qualifies = false, Name = "other-book" } }) {
                var reason = sample.Context == null ? default(RuleReason) : (RuleReason)sample.Context;
                var stats = new RuleCalculateWeaponStats(owner, sample.Weapon) { Reason = reason }; Rule(stats);
                Check(stats.CriticalMultiplier == (int)sample.Weapon.Blueprint.CriticalModifier, "Reckoning never changes the weapon critical multiplier");
                Check(stats.DoubleCriticalEdge == (sample.Qualifies && level >= 16), "actual native weapon-stat critical-range flag scope " + sample.Name + "/" + level);
                var nativeRange = stats.CriticalRange; stats.DoubleCriticalEdge = true; var doubled = stats.CriticalRange;
                if (sample.Qualifies && level >= 16) { Rule(stats); Check(stats.CriticalRange == doubled && nativeRange == doubled, "native critical range doubling is boolean and does not multiply again"); }
                var attack = new RuleAttackRoll(owner, target, sample.Weapon, 0) { Reason = reason }; Rule(attack);
                Check(attack.CriticalConfirmationBonus == (sample.Qualifies && level >= 8 ? 4 : 0), "critical confirmation starts at level 8 and stays scoped " + sample.Name); Rule(attack);
                Check(attack.CriticalConfirmationBonus == (sample.Qualifies && level >= 8 ? 4 : 0), "duplicate attack subscription does not double confirmation");
                var physical = new PhysicalDamage(new ModifiableDiceFormula(new DiceFormula(3, DiceType.D6)), 0, PhysicalDamageForm.Slashing);
                var precision = new PhysicalDamage(new ModifiableDiceFormula(new DiceFormula(2, DiceType.D6)), 0, PhysicalDamageForm.Slashing) { Precision = true };
                var parent = new RuleDealDamage(owner, target, new DamageBundle(physical, precision)) { Reason = reason }; Set(parent, "AttackRoll", attack); var calculate = new RuleCalculateDamage(parent); calculate.Reason = reason; Rule(calculate);
                Check(physical.Modifiers.Sum(m => m.Value) == (sample.Qualifies ? 3 : 0) && precision.Modifiers.Sum(m => m.Value) == (sample.Qualifies ? 2 : 0), "mixed ordinary and precision damage bundle gains each qualifying die exactly once " + sample.Name + " actual=" + physical.Modifiers.Sum(m => m.Value) + "/" + precision.Modifiers.Sum(m => m.Value) + " book=" + sample.Context?.SourceAbilityContext?.Ability?.Spellbook?.Blueprint.name);
                Rule(calculate); Check(physical.Modifiers.Sum(m => m.Value) == (sample.Qualifies ? 3 : 0) && precision.Modifiers.Sum(m => m.Value) == (sample.Qualifies ? 2 : 0), "duplicate damage event cannot add a second per-die modifier");
            }
            var area = new PhysicalDamage(new ModifiableDiceFormula(new DiceFormula(4, DiceType.D6)), 0, PhysicalDamageForm.Slashing);
            var noAttack = new RuleDealDamage(owner, target, new DamageBundle(area)) { Reason = witchCast }; Rule(new RuleCalculateDamage(noAttack) { Reason = witchCast });
            Check(!area.Modifiers.Any(), "witch area/ongoing damage without attack roll is excluded");
            var sourceType = spell.Type;
            try {
                foreach (var type in new[] { AbilityType.SpellLike, AbilityType.Supernatural, AbilityType.Extraordinary }) {
                    spell.Type = type;
                    var stats = Rule(new RuleCalculateWeaponStats(owner, ray) { Reason = witchCast }); var attack = Rule(new RuleAttackRoll(owner, target, ray, 0) { Reason = witchCast });
                    var damage = new PhysicalDamage(new ModifiableDiceFormula(new DiceFormula(3, DiceType.D6)), 0, PhysicalDamageForm.Slashing);
                    var parent = new RuleDealDamage(owner, target, new DamageBundle(damage)) { Reason = witchCast }; Set(parent, "AttackRoll", attack); Rule(new RuleCalculateDamage(parent) { Reason = witchCast });
                    Check(!stats.DoubleCriticalEdge && attack.CriticalConfirmationBonus == 0 && !damage.Modifiers.Any(), "a nonspell ability cannot borrow witch-book Reckoning " + type);
                }
            } finally { spell.Type = sourceType; }
            reckoning.Deactivate(); var dormant = Rule(new RuleCalculateWeaponStats(owner, natural)); Check(!dormant.DoubleCriticalEdge, "native deactivated Reckoning cannot extend a natural critical range");
            var revelation = Buff(owner, Asset<BlueprintBuff>(cache, "RevelationInvokerEffect01437"));
            foreach (var item in new[] { natural, sword, ray, touch }) {
                var evt = new RuleCalculateAttackBonusWithoutTarget(owner, item, 0); Rule(evt, true);
                Check(evt.AllBonuses.Where(m => m.Fact == revelation).Sum(m => m.Value) == (item == ray || item == touch ? 0 : 1 + (level >= 16 ? 2 : level >= 8 ? 1 : 0)), "native Revelation applies insight only to actual weapon attacks");
                evt.OnDidTrigger();
            }
            owner.Facts.Remove(revelation);
        }
    }
    internal static int Run(BlueprintsCache cache)
    {
        checks = serial = invocationCalls = spendCalls = parameterCalls = damageCalls = fault = 0; units.Clear();
        var h = new Harmony("CotW01437.InvokerNativeContracts"); var game = Game.Instance; var priorState = game.State; var priorTime = game.TimeController;
        var buffHelper = AccessTools.Method(typeof(UnitHelper), "AddBuff", new[] { typeof(UnitDescriptor), typeof(BlueprintBuff), typeof(MechanicsContext), typeof(TimeSpan?) });
        var suspended = (Harmony.GetPatchInfo(buffHelper)?.Prefixes.AsEnumerable() ?? Enumerable.Empty<HarmonyLib.Patch>()).Where(p => p.PatchMethod == AccessTools.Method(typeof(Psychic070Tests), "CaptureDefense")).ToArray();
        var state = Empty<PersistentState>(); var player = Empty<Player>(); player.GameTime = TimeSpan.FromHours(1); Set(player, "m_Party", new List<UnitEntityData>()); Set(player, "m_CharacterListsValid", true); state.PlayerState = player; Set(game, "State", state); Set(game, "TimeController", Empty<TimeController>());
        void Patch(MethodBase method, string name) => h.Patch(method, prefix: new HarmonyMethod(typeof(Invoker01437Tests), name));
        void BardPatch(MethodBase method, string name) => h.Patch(method, prefix: new HarmonyMethod(typeof(BardMasterpieces01435Tests), name));
        try {
            foreach (var p in suspended) h.Unpatch(buffHelper, p.PatchMethod);
            PrepareNative(cache);
            Patch(AccessTools.Method(typeof(FeatureCollection), "CheckStatPrerequisitesAndUpdate"), nameof(Skip)); Patch(AccessTools.PropertyGetter(typeof(IgnorePrerequisites), "Ignore"), nameof(False)); Patch(AccessTools.PropertyGetter(typeof(Game), "LevelUpController"), nameof(Controller)); Provider01419Tests.PatchLabels(h);
            Patch(AccessTools.PropertyGetter(typeof(TimeController), "GameTime"), nameof(Clock)); Patch(AccessTools.PropertyGetter(typeof(TimeController), "RealTime"), nameof(Clock)); Patch(AccessTools.Method(typeof(EntityFact), "Attach"), nameof(Identity)); Patch(AccessTools.Method(typeof(UnitState), "UpdateStatusEffect"), nameof(Skip)); Patch(AccessTools.Method(typeof(UnitEntityData), "UpdateGroup"), nameof(Skip));
            Patch(AccessTools.Method(typeof(TurnBased.Controllers.CombatController), "IsInTurnBasedCombat"), nameof(False)); Patch(AccessTools.PropertyGetter(typeof(Kingmaker.Armies.TacticalCombat.TacticalCombatHelper), "IsActive"), nameof(False)); Patch(AccessTools.PropertyGetter(typeof(UnitEntityData), "IsPlayersEnemy"), nameof(False));
            BardPatch(AccessTools.PropertyGetter(typeof(Game), "CurrentMode"), "Mode"); BardPatch(AccessTools.Method(typeof(UnitEntityData), "IsCurrentUnit"), "CurrentUnit");
            Patch(AccessTools.Method(typeof(Kingmaker.QA.LogChannelEx), "ExceptionWithReport", new[] { typeof(Owlcat.Runtime.Core.Logging.LogChannel), typeof(Exception), typeof(string), typeof(object[]) }), nameof(Expose));
            h.Patch(AccessTools.Method(typeof(RuleCalculateAbilityParams), "OnTrigger"), transpiler: new HarmonyMethod(typeof(Migration01426Tests), "DifficultyBoundary"));
            h.Patch(AccessTools.Method(typeof(RuleCanApplyBuff), "OnTrigger"), transpiler: new HarmonyMethod(typeof(BardMasterpieces01435Tests), "DifficultyBoundary"));
            Registration(cache, h); Invocations(cache); Magic(cache); Physical(cache); BridgeConsumption(cache); ReckoningAndRevelation(cache);
            Register(cache); Check(true, "published graph remains valid after actual native activation, effect calculation and facet lifecycle use");
            PsychicExpansionTests.Inventory["invoker01437Contracts"] = new JObject { ["passed"] = true, ["assertions"] = checks, ["additionalItemCount"] = 1, ["archetypeCount"] = 1, ["itemIds"] = new JArray("COTW-0016"), ["newSpellRootCount"] = 0,
                ["privateBlueprintCount"] = 32, ["nativeImmediateAbilityApplications"] = invocationCalls, ["nativeResourceSpendCalls"] = spendCalls, ["nativeAbilityParameterRules"] = parameterCalls, ["nativeDamageSubscriptionPasses"] = damageCalls,
                ["nativeCalculateAndDealConsumers"] = consumedCalculations,
                ["newBlueprints"] = JArray.FromObject(Own.Select(t => cache.Load(Id(t))).Select(b => new { id = b.AssetGuid.ToString(), name = b.name, type = b.GetType().FullName })),
                ["boundary"] = "Native feature/buff managers, AddFacts, resources, activatable restrictions/TryStart, AbilityExecutionProcess.ApplyEffectImmediate, ContextRankConfig/stat modifiers, rule subscription proxies, ability parameter calculation, DamageValue DR arithmetic and RuleCalculateDamage.OnTrigger to RuleDealDamage.OnTrigger final Result consumption execute. Unit/scene services, clock, target final DR input, neutral difficulty, shadow/render endpoints and UI labels are controlled. The six native damage consumers use IsFake and do not apply HP damage. Full cast command scheduling, rendered effects, upgrade screen, respec and complete save/load remain tester steps; no game process is started." };
            Console.WriteLine("PASS: " + checks + " Invoker01437 native contracts."); return checks;
        } finally {
            foreach (var unit in units.AsEnumerable().Reverse()) foreach (var fact in unit.Facts.List.ToArray()) if (fact.Manager != null) unit.Facts.Remove(fact);
            h.UnpatchAll(h.Id); foreach (var p in suspended) new Harmony(p.owner).Patch(buffHelper, prefix: new HarmonyMethod(p.PatchMethod) { priority = p.priority, before = p.before, after = p.after, debug = p.debug });
            Set(game, "State", priorState); Set(game, "TimeController", priorTime); units.Clear();
        }
    }
}

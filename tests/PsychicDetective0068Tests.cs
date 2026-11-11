using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using HarmonyLib;
using Newtonsoft.Json.Linq;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Prerequisites;
using Kingmaker.Blueprints.Classes.Selection;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.Blueprints.Facts;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.EntitySystem;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Enums;
using Kingmaker.Items;
using Kingmaker.RuleSystem.Rules;
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
using Kingmaker.UnitLogic.Mechanics.Components;
using CallOfTheWild.Wrath.Prototype;

// COTW-0068 B-stage directed checks, contract C-0068-20261111-r1.
// The host enters with COTW_0068_CASE=psychicdetective0068 and routes straight
// here after the shared Psychic fixture; no historical behaviour suite runs.
//
// Scope (recorded in the runner evidence): real game Managed blueprint types,
// the real production PsychicDetective0068 registration, the real
// S-PSYCHIC-0068 binding/runtime/menu resolution, real native ClassData
// archetype -> Spellbook -> DemandSpellbook, real native resource maximum,
// real native BlueprintFeatureSelection.CanSelect through the production
// Harmony prefix and real FeatureSelectionState ancestry.
//
// Declared fixture boundaries (NOT claimed as player-game acceptance):
//  - HomebrewArchetypes is not installed. Its audited identities are staged as
//    contract-shaped provider roots at the exact frozen GUIDs from A. The
//    production gate verifies name/type/link/progression/discovery options on
//    those roots; this is a provider-initialisation fixture, not a mod load.
//  - The eleven shared amplification roots and their effect/conversion buffs
//    are staged at their deterministic PsychicAssets.Id() identities. Their
//    own production registrations belong to earlier packages and are not run.
//  - 114 of the 176 distinct 180-row anchor GUIDs are present in the reviewed
//    native archive; unresolved rows are counted and disclosed, never faked.
//  - No game, no UMM, no SaveManager/retraining controller, no full historical
//    suite and no whole-library matrix.
internal static class PsychicDetective0068Tests
{
    private const string ClassId = "adb9e138bee9ecc4db246b64d563f900";
    private const string ProgressionId = "5eaea532547c7ce40a84563e15fd3440";
    private const string TalentSelectionId = "da89239cc5859a1478007cd5e6d44f67";
    private const string OldBookId = "961e6b4edd3c80e4a9fe4435deff6fe7";
    private const string PoisonResistanceId = "152ea6d8f279a73489c9dc0815e9e9b1";
    private const string PoisonImmunityId = "03d1877a5ffcbd34d9501b005434cb4f";
    private const string AlchemistDiscoveryId = "f68b959943348034d8469c165442e019";
    private const string DetectMagicId = "ee0b69e90bac14446a4cf9a050f87f2e";
    private const string BardBookId = "bc04fc157a8801d41b877ad0d9af03dd";
    private const string BardSpellsPerDayId = "0a8eec9ca5c0dc64795243ab3c55d924";
    private const string BardSpellsKnownId = "90de6b4f591d1184497fbfbae6e16547";
    private const string FindTrapsId = "4709274b2080b6444a3c11c6ebbe2404";
    private const string BanishmentId = "d361391f645db984bbf58907711a146a";
    private const string AlchemistClassId = "0937bec61c0dabc468428f496580c721";
    private const string PsychicBookId = "3ce26d41e96b5ca289f0c6ce9db28d3c";
    private const string PsychicListId = "6a4b9d6651b155058004354a803da3bf";
    private const string CenterAbilityId = "e66116541ac4546c9084a7bcfa47e5a6";
    private const string BasicFeatsId = "247a4068296e8be42890143f451b4b45";
    private const string GreaterStunningBarrierId = "9e33ed477ddd4fad9d90fb64b1a6f732";

    private static readonly string[] ForbiddenDiscoveryIds = {
        "cee8f65448ce71c4b8b8ca13751dd8ea", // Mutagen
        "2673ccdd6df742d42a8c94977c76a984", // Enhance Potion
        "1db8e126b77e71d4d89dcfa50fe87e93", // Extend Potion
        "57d5077b301ade749b840b0ea9230bb9"  // Infusion
    };
    private static readonly string[] BasicTokens = {
        "BiokineticHealingFeature","ConjuredArmorFeature","DefensivePrognosticationFeature",
        "FocusedForceFeature","MindsEyeFeature","OverpoweringMindFeature","WillOfTheDeadFeature01458",
        "OngoingDefenseFeature","RelentlessCastingFeature","UndercastSurgeFeature01458","PsychofeedbackFeature"
    };
    private static readonly string[] EffectTokens = {
        "DefensivePrognostication1Effect","DefensivePrognostication2Effect","ConjuredArmorEffect"
    };

    private static int checks;
    private static int hostIds, hostLogs;
    private static bool NotPlayerFaction(ref bool __result) { __result = false; return false; }
    private static bool NotTacticalCombat(ref bool __result) { __result = false; return false; }
    private static bool KeepPrerequisites(ref bool __result) { __result = false; return false; }
    private static bool NextUniqueId(ref string __result) { __result = "0068-" + (++hostIds).ToString("X"); return false; }
    private static bool HostLogFirst(Exception __0) { HostLog(__0); return false; }
    private static bool HostLogSecond(object __0, Exception __1) { HostLog(__1); return false; }
    private static void HostLog(Exception error)
    {
        if (error == null) return;
        hostLogs++;
        Console.WriteLine("0068 HOST LOG: " + error);
    }
    private static Assembly Main => AppDomain.CurrentDomain.GetAssemblies()
        .First(a => a.GetName().Name == "CallOfTheWild.Wrath.Prototype");
    private static Type T(string name) => Main.GetType("CallOfTheWild.Wrath.Prototype." + name, true);
    private static BlueprintGuid P(string id) => BlueprintGuid.Parse(id);
    private static object Get(object o, string f) => AccessTools.Field(o.GetType(), f)?.GetValue(o)
        ?? AccessTools.Field(o.GetType(), "<" + f + ">k__BackingField")?.GetValue(o);
    private static void Set(object o, string f, object v) =>
        (AccessTools.Field(o.GetType(), f) ?? AccessTools.Field(o.GetType(), "<" + f + ">k__BackingField")
         ?? throw new MissingFieldException(o.GetType().FullName, f)).SetValue(o, v);
    private static TEmpty Empty<TEmpty>() where TEmpty : class => (TEmpty)FormatterServices.GetUninitializedObject(typeof(TEmpty));
    private static TReference Ref<TReference>(SimpleBlueprint bp) where TReference : BlueprintReferenceBase, new()
    {
        var r = BlueprintReferenceBase.CreateTyped<TReference>(bp);
        AccessTools.Property(typeof(BlueprintReferenceBase), "Cached").SetValue(r, bp, null);
        return r;
    }
    private static BlueprintGuid Id(string token) => (BlueprintGuid)AccessTools.Method(T("PsychicAssets"), "Id")
        .Invoke(null, new object[] { token });
    private static void Check(bool ok, string message)
    {
        if (!ok) throw new Exception("0068 CHECK FAILED: " + message);
        checks++;
        Console.WriteLine("0068 CHECK: " + message);
    }
    private static object Call(Type type, string name, params object[] args) =>
        AccessTools.Method(type, name).Invoke(null, args);
    private static object Call(string type, string name, params object[] args) => Call(T(type), name, args);
    private static object Call(Type type, string name, Type[] signature, params object[] args) =>
        AccessTools.Method(type, name, signature).Invoke(null, args);

    private static T Stage<T>(BlueprintsCache cache, T bp, string guid, string name) where T : SimpleBlueprint
    {
        bp.AssetGuid = P(guid); bp.name = name;
        if (bp is BlueprintUnitFact) Set(bp, "m_Icon", Empty<UnityEngine.Sprite>());
        if (bp is BlueprintScriptableObject so && so.ComponentsArray == null)
            so.ComponentsArray = Array.Empty<BlueprintComponent>();
        bp.OnEnable();
        cache.AddCachedBlueprint(bp.AssetGuid, bp);
        return bp;
    }
    private static LevelEntry Entry(int level, params BlueprintFeatureBase[] facts)
    {
        var e = new LevelEntry { Level = level };
        Set(e, "m_Features", facts.Select(f => Ref<BlueprintFeatureBaseReference>(f)).ToList());
        return e;
    }

    // ---------------------------------------------------------------- gate ---
    private static string[] Missing(BlueprintsCache cache) => (string[])Call("PsychicDetective0068", "MissingProvider", cache);
    private static bool Register(BlueprintsCache cache) => (bool)Call("PsychicDetective0068", "Register", cache);
    private static string[] LastMissing() => (string[])AccessTools.Field(T("PsychicDetective0068"), "LastMissing").GetValue(null);
    private static string LastSummary() => (string)AccessTools.Field(T("PsychicDetective0068"), "LastSummary").GetValue(null);
    private static string LastSpellSummary() => (string)AccessTools.Field(T("PsychicDetective0068"), "LastSpellSummary").GetValue(null);
    private static string[] LastOptional() => (string[])AccessTools.Field(T("PsychicDetective0068"), "LastOptional").GetValue(null);

    private static void HostGame()
    {
        if (Kingmaker.Game.HasInstance) return;
        var game = (Kingmaker.Game)FormatterServices.GetUninitializedObject(typeof(Kingmaker.Game));
        AccessTools.Field(typeof(Kingmaker.Game), "s_Instance").SetValue(null, game);
        var stateField = AccessTools.Field(typeof(Kingmaker.Game), "State");
        var state = FormatterServices.GetUninitializedObject(stateField.FieldType);
        stateField.SetValue(game, state);
        AccessTools.Field(stateField.FieldType, "PlayerState").SetValue(state,
            FormatterServices.GetUninitializedObject(typeof(Kingmaker.Player)));
        var player = AccessTools.Field(stateField.FieldType, "PlayerState").GetValue(state);
        Set(player, "m_Party", new List<UnitEntityData>());
        Set(player, "m_PartyAndPets", new List<UnitEntityData>());
        Set(player, "m_PartyAndPetsDetached", new List<UnitEntityData>());
        Set(player, "PartyCharacters", new List<UnitReference>());
        AccessTools.Field(typeof(Kingmaker.Game), "TimeController").SetValue(game,
            FormatterServices.GetUninitializedObject(typeof(Kingmaker.Controllers.TimeController)));
        AccessTools.Field(typeof(Kingmaker.Game), "Rulebook").SetValue(game,
            Activator.CreateInstance(typeof(Kingmaker.RuleSystem.Rulebook)));
    }

    // A real native unit with a real Progression, real Resources, real Stats,
    // real Buffs/Facts and the descriptor spellbook dictionary DemandSpellbook
    // writes into. Modelled on the reviewed 0060 / 0.7.0 host fixtures.
    private static UnitEntityData Unit(BlueprintsCache cache, BlueprintCharacterClass cls, int level)
    {
        var caster = new Psychic070Tests.Caster(cache);
        var u = caster.Unit;
        var facts = new EntityFactsManager(u); Set(u, "Facts", facts);
        var buffs = new BuffCollection(); buffs.Initialize(facts); Set(u, "Buffs", buffs);
        var all = (List<ModifiableValue>)Get(u.Stats, "<AllStats>k__BackingField");
        foreach (var stat in new[] { StatType.Intelligence, StatType.Wisdom, StatType.Charisma })
        {
            var value = new ModifiableValueAttributeStat(u.Stats, stat) { BaseValue = 10 };
            Set(u.Stats, stat.ToString(), value); all.Add(value);
        }
        var classData = new ClassData(cls) { Level = level };
        Set(classData, "Spellbook", cls.Spellbook);
        u.Progression.Classes.Clear(); u.Progression.Classes.Add(classData);
        Set(u.Progression, "Features", u.Facts.EnsureFactProcessor<FeatureCollection>());
        Set(u.Descriptor, "Resources", new UnitAbilityResourceCollection(u.Descriptor));
        Set(u.Descriptor, "Proficiencies", new UnitProficiency());
        AccessTools.Field(typeof(UnitDescriptor), "m_Spellbooks").SetValue(u.Descriptor,
            new Dictionary<BlueprintSpellbook, Spellbook>());
        facts.TurnOn();
        return u;
    }
    private static void Resource(UnitEntityData u, BlueprintAbilityResource bp, int amount)
    {
        var collection = u.Descriptor.Resources;
        var entry = new UnitAbilityResource(bp);
        Set(entry, "Amount", amount);
        var map = (System.Collections.IDictionary)Get(collection, "m_Resources");
        if (map == null) { map = new Dictionary<BlueprintScriptableObject, UnitAbilityResource>(); Set(collection, "m_Resources", map); }
        map[bp] = entry;
    }

    // ------------------------------------------------------------- staging ---
    private static BlueprintCharacterClass haClass;
    private static BlueprintFeatureSelection haTalents, haDiscovery;
    private static BlueprintProgression haProgression;
    private static BlueprintFeature[] forbidden;
    private static BlueprintFeature legalDiscovery;
    private static BlueprintAbilityResource detectivePool;

    private static void StageProvider(BlueprintsCache cache)
    {
        var alchemist = cache.Load(P(AlchemistClassId)) as BlueprintCharacterClass;
        Check(alchemist != null, "SETUP audited native AlchemistClass is present in the reviewed archive");
        var detect = cache.Load(P(DetectMagicId)) as BlueprintFeature;
        Check(detect != null, "SETUP audited native DetectMagic is present");
        var bard = cache.Load(P(BardBookId)) as BlueprintSpellbook;
        Check(bard != null && Get(bard, "m_SpellsPerDay") != null && Get(bard, "m_SpellsKnown") != null,
            "SETUP audited native BardSpellbook and its two progression tables are hydrated");
        Check(cache.Load(P(BardSpellsPerDayId)) is BlueprintSpellsTable && cache.Load(P(BardSpellsKnownId)) is BlueprintSpellsTable,
            "SETUP audited Bard spells-per-day and spells-known tables are present");
        Check(cache.Load(P(CenterAbilityId)) != null && cache.Load(P(PsychicListId)) != null,
            "SETUP shared Psychic Center Self and canonical spell list roots are published");

        // The provider's OWN book deliberately points at native Alchemist, the
        // exact trap called out by Q03. It must never be inherited by the
        // replacement book.
        var oldBook = Stage(cache, new BlueprintSpellbook { Spontaneous = true, CastingAttribute = StatType.Intelligence,
            IsArcane = true, AllSpellsKnown = false, CanCopyScrolls = false }, OldBookId, "InvestigatorSpellbook");
        Set(oldBook, "m_CharacterClass", Ref<BlueprintCharacterClassReference>(alchemist));
        Check(((BlueprintCharacterClassReference)Get(oldBook, "m_CharacterClass")).Guid == alchemist.AssetGuid,
            "SETUP provider spellbook m_CharacterClass really points at native Alchemist");

        var poison = Stage(cache, new BlueprintFeature(), PoisonResistanceId, "InvestigatorPoisonResistance");
        var immunity = Stage(cache, new BlueprintFeature(), PoisonImmunityId, "InvestigatorPoisonImmunity");
        forbidden = ForbiddenDiscoveryIds.Select((id, i) =>
            Stage(cache, new BlueprintFeature(), id, "NativeDiscovery" + i)).ToArray();
        legalDiscovery = Stage(cache, new BlueprintFeature(), "1b78c53a6b8f4a14b70a18d8715cf2a4e1", "LegalInvestigatorDiscovery");
        haDiscovery = Stage(cache, new BlueprintFeatureSelection { Ranks = 1, IsClassFeature = true,
            ComponentsArray = Array.Empty<BlueprintComponent>() }, AlchemistDiscoveryId, "AlchemistDiscovery");
        Set(haDiscovery, "m_Features", forbidden.Concat(new[] { legalDiscovery }).Select(f => Ref<BlueprintFeatureReference>(f)).ToArray());
        Set(haDiscovery, "m_AllFeatures", forbidden.Concat(new[] { legalDiscovery }).Select(f => Ref<BlueprintFeatureReference>(f)).ToArray());

        haTalents = Stage(cache, new BlueprintFeatureSelection { Ranks = 1, IsClassFeature = true,
            IgnorePrerequisites = false, ComponentsArray = Array.Empty<BlueprintComponent>() },
            TalentSelectionId, "InvestigatorTalentSelection");
        // The four native discoveries are real options of the Investigator
        // talent path AND of its nested AlchemistDiscovery selection, so the
        // plain-Investigator positives and detective negatives are a genuine
        // paired proof rather than a trivially-empty native CanSelect.
        var talentOptions = new BlueprintFeatureBase[] { haDiscovery }
            .Concat(forbidden).Concat(new[] { legalDiscovery }).ToArray();
        Set(haTalents, "m_Features", talentOptions.Select(f => Ref<BlueprintFeatureReference>((BlueprintFeature)f)).ToArray());
        Set(haTalents, "m_AllFeatures", talentOptions.Select(f => Ref<BlueprintFeatureReference>((BlueprintFeature)f)).ToArray());

        var entries = new List<LevelEntry>();
        for (int level = 1; level <= 20; level++)
        {
            var grants = new List<BlueprintFeatureBase>();
            if (level == 1) grants.Add(detect);
            if (level == 2 || level == 5 || level == 8) grants.Add(poison);
            if (level == 11) grants.Add(immunity);
            if (level == 3 || (level >= 5 && level <= 19 && level % 2 == 1)) grants.Add(haTalents);
            entries.Add(Entry(level, grants.ToArray()));
        }
        haProgression = Stage(cache, new BlueprintProgression { LevelEntries = entries.ToArray(),
            UIGroups = Array.Empty<UIGroup>(), ComponentsArray = Array.Empty<BlueprintComponent>() },
            ProgressionId, "InvestigatorProgression");
        Set(haProgression, "m_UIDeterminatorsGroup", new[] { Ref<BlueprintFeatureBaseReference>(detect) });

        haClass = Stage(cache, new BlueprintCharacterClass { IsArcaneCaster = false, IsDivineCaster = false,
            ComponentsArray = Array.Empty<BlueprintComponent>() }, ClassId, "InvestigatorClass");
        Set(haClass, "m_Progression", Ref<BlueprintProgressionReference>(haProgression));
        Set(haClass, "m_Spellbook", Ref<BlueprintSpellbookReference>(oldBook));
        Set(haClass, "m_Archetypes", Array.Empty<BlueprintArchetypeReference>());
        Check(haClass.Progression == haProgression && haClass.Spellbook == oldBook,
            "SETUP provider class resolves its real progression and old spellbook");

        // Shared amplification roots owned by earlier packages: staged at their
        // deterministic private identities, never re-authored.
        foreach (var token in BasicTokens)
            if (cache.Load(Id(token)) == null) Stage(cache, new BlueprintFeature(), Id(token).ToString(), "CotWWrathPsychic" + token);
        foreach (var token in EffectTokens)
            if (cache.Load(Id(token)) == null) Stage(cache, new BlueprintBuff(), Id(token).ToString(), "CotWWrathPsychic" + token);
        foreach (var stat in new[] { StatType.Strength, StatType.Dexterity, StatType.Constitution })
            for (int level = 2; level <= 9; level++)
            {
                var id = Id("Psychofeedback" + stat + level + "Buff");
                if (cache.Load(id) == null) Stage(cache, new BlueprintBuff(), id.ToString(), "CotWWrathPsychicPsychofeedback" + stat + level + "Buff");
            }
        Check(BasicTokens.All(t => cache.Load(Id(t)) is BlueprintFeature) &&
              EffectTokens.All(t => cache.Load(Id(t)) is BlueprintBuff),
            "SETUP eleven shared amplification roots and three shared effects resolve at their audited identities");
    }

    private static BlueprintSpellbook DetectiveBook(BlueprintsCache cache) =>
        (BlueprintSpellbook)cache.Load(Id("PsychicDetective0068Spellbook"));
    private static BlueprintArchetype DetectiveRoot(BlueprintsCache cache) =>
        (BlueprintArchetype)cache.Load(Id("PsychicDetective0068Archetype"));
    private static BlueprintSpellList DetectiveList(BlueprintsCache cache) =>
        (BlueprintSpellList)cache.Load(Id("PsychicDetective0068SpellList"));

    // -------------------------------------------------------------- section ---
    internal static int Run(BlueprintsCache parent, Harmony h, JObject nativeRows)
    {
        checks = 0; hostIds = 0; hostLogs = 0;
        Console.WriteLine("0068 stage: isolated managed host, real game blueprint cache, C-0068-20261111-r1 directed cases");
        HostGame();
        // Declared fixture boundaries, not production hooks: the isolated host
        // has no Unity scene, no player party and no tactical mode. These are
        // the same four shims reviewed for the 0060 directed case plus the
        // Owlcat log redirect that keeps native fact-dispatch errors visible
        // instead of letting the Unity log callback mask the real exception.
        h.Patch(AccessTools.PropertyGetter(typeof(UnitDescriptor), "IsPlayerFaction"),
            prefix: new HarmonyMethod(AccessTools.Method(typeof(PsychicDetective0068Tests), nameof(NotPlayerFaction))));
        h.Patch(AccessTools.PropertyGetter(typeof(Kingmaker.Armies.TacticalCombat.TacticalCombatHelper), "IsActive"),
            prefix: new HarmonyMethod(AccessTools.Method(typeof(PsychicDetective0068Tests), nameof(NotTacticalCombat))));
        h.Patch(AccessTools.Method(typeof(Kingmaker.Player), "GetNewUniqueId"),
            prefix: new HarmonyMethod(AccessTools.Method(typeof(PsychicDetective0068Tests), nameof(NextUniqueId))));
        h.Patch(AccessTools.PropertyGetter(typeof(IgnorePrerequisites), "Ignore"),
            prefix: new HarmonyMethod(AccessTools.Method(typeof(PsychicDetective0068Tests), nameof(KeepPrerequisites))));
        foreach (var signature in new[] {
            new[] { typeof(Exception), typeof(string), typeof(object[]) },
            new[] { typeof(UnityEngine.Object), typeof(Exception), typeof(string), typeof(object[]) },
            new[] { typeof(Owlcat.Runtime.Core.Logging.ICanBeLogContext), typeof(Exception), typeof(string), typeof(object[]) } })
        {
            var method = AccessTools.Method(typeof(Owlcat.Runtime.Core.Logging.LogChannel), "Exception", signature);
            if (method == null) continue;
            h.Patch(method, prefix: new HarmonyMethod(signature.Length == 3
                ? AccessTools.Method(typeof(PsychicDetective0068Tests), nameof(HostLogFirst))
                : AccessTools.Method(typeof(PsychicDetective0068Tests), nameof(HostLogSecond))));
        }
        var cache = parent;
        AccessTools.Field(typeof(ResourcesLibrary), "BlueprintsCache").SetValue(null, cache);

        // Real production Harmony prefix for Q07; no other production patch is
        // installed by this host, exactly as in the 0060 directed case.
        h.CreateClassProcessor(T("PsychicDetectiveTalent0068Patch")).Patch();

        StageProvider(cache);

        // ================= C0068-01/02: provider gate =================
        var empty = new BlueprintsCache();
        var blank = Missing(empty);
        Check(blank.Length > 0 && blank.Any(m => m.Contains("InvestigatorClass missing")),
            "C01 empty cache defers with an explicit HA provider blocker");
        Check(!Register(empty) && LastMissing().Length == blank.Length,
            "C01 deferred registration publishes nothing and re-reports the same blockers");
        Check(empty.Load(Id("PsychicDetective0068Archetype")) == null &&
              empty.Load(Id("PsychicDetective0068Spellbook")) == null,
            "C01 no half-registered private archetype or spellbook after deferral");

        var wrong = new BlueprintsCache();
        Stage(wrong, new BlueprintFeature(), ClassId, "ForeignIdentity");
        bool rejected = false;
        try { Missing(wrong); } catch (TargetInvocationException e) when (e.InnerException is InvalidOperationException) { rejected = true; }
        Check(rejected, "C02 same-GUID wrong-type provider identity is rejected instead of treated as ready");
        wrong.RemoveCachedBlueprint(P(ClassId));
        Check(Missing(wrong).Any(m => m.Contains("InvestigatorClass missing")),
            "C02 removing the foreign identity leaves a clean retryable deferral");

        var missingBefore = Missing(cache);
        Check(missingBefore.Length == 0, "C01 full audited provider + shared roots satisfy the atomic gate: " +
            (missingBefore.Length == 0 ? "none" : string.Join("; ", missingBefore)));
        var publicAssetsBefore = PublicAssets(cache).Length;

        // ================= C0068-01: real registration =================
        Check(Register(cache), "C01 production PsychicDetective0068.Register succeeds against staged provider");
        Check(LastSummary().StartsWith("Registered", StringComparison.Ordinal), "C01 registration summary reports the real committed graph");
        var root = DetectiveRoot(cache);
        var book = DetectiveBook(cache);
        var list = DetectiveList(cache);
        Check(root != null && book != null && list != null, "C01 private archetype, spellbook and spell list are published");

        var archetypes = (BlueprintArchetypeReference[])Get(haClass, "m_Archetypes") ?? Array.Empty<BlueprintArchetypeReference>();
        Check(archetypes.Count(r => r?.Guid == root.AssetGuid) == 1 && root.GetParentClass() == haClass,
            "C01 archetype linked exactly once and its parent is the real HA Investigator");
        var talentAll = (BlueprintFeatureReference[])Get(haTalents, "m_AllFeatures") ?? Array.Empty<BlueprintFeatureReference>();
        foreach (var token in new[] { "PsychicDetective0068ExpandedPoolTalent", "PsychicDetective0068ExtraAmplificationTalent" })
            Check(talentAll.Count(r => r?.Guid == Id(token)) == 1, "C01 private talent added once to the provider selection: " + token);
        Check(haProgression.UIGroups.Length == 1, "C01 one UI group appended to the provider progression");

        var identitySnapshot = PublicAssets(cache).Select(a => a.AssetGuid).ToArray();
        var linkSnapshot = archetypes.Select(r => r.Guid).ToArray();
        Check(Register(cache) && LastSummary().StartsWith("Already registered", StringComparison.Ordinal),
            "C01 second registration takes the verified re-entry path");
        Check(PublicAssets(cache).Length == identitySnapshot.Length &&
              PublicAssets(cache).Select(a => a.AssetGuid).SequenceEqual(identitySnapshot),
            "C01 re-entry publishes no new private identity");
        Check(((BlueprintArchetypeReference[])Get(haClass, "m_Archetypes")).Select(r => r.Guid).SequenceEqual(linkSnapshot),
            "C01 re-entry does not duplicate the archetype link");
        Check(talentAll.Length == ((BlueprintFeatureReference[])Get(haTalents, "m_AllFeatures")).Length,
            "C01 re-entry does not duplicate either private talent");

        // ================= C0068-03/04: progression + real book ==========
        Check(root.AddFeatures.Length == 3 && root.AddFeatures.Select(e => e.Level).SequenceEqual(new[] { 1, 2, 3 }),
            "C03 add entries are exactly levels 1/2/3");
        Check(root.RemoveFeatures.Length == 5 && root.RemoveFeatures.Select(e => e.Level).SequenceEqual(new[] { 2, 3, 5, 8, 11 }),
            "C03 remove entries are exactly 2/3/5/8/11 poison-resistance, poison-immunity and level-3 talent");
        Check(root.AddFeatures.Single(e => e.Level == 3).Features.Count() == 2,
            "C03 level 3 grants Phrenic Dabbler and its amplification selection");
        Check(root.RemoveFeatures.Single(e => e.Level == 11).Features.Any(f => f.AssetGuid == P(PoisonImmunityId)) &&
              root.RemoveFeatures.Where(e => new[] { 2, 5, 8 }.Contains(e.Level))
                  .All(e => e.Features.Any(f => f.AssetGuid == P(PoisonResistanceId))),
            "C03 removed grants are the real provider poison features");
        Check(root.RemoveFeatures.Single(e => e.Level == 3).Features.Any(f => f.AssetGuid == haTalents.AssetGuid),
            "C03 level 3 removes one real Investigator Talent grant");
        Check(root.ChangeCasterType && !root.IsArcaneCaster && !root.IsDivineCaster && root.ReplaceClassSkills &&
              root.ClassSkills.Length == 9, "C03/Q03 caster type, arcane/divine flags and nine replacement class skills");

        Check(root.ReplaceSpellbook == book && book.CharacterClass == haClass,
            "C04 ReplaceSpellbook is the private book whose m_CharacterClass is the real HA Investigator");
        Check(((BlueprintCharacterClassReference)Get(book, "m_CharacterClass")).Guid != alchemistGuid(cache),
            "C04 the replacement book does NOT inherit the provider's native Alchemist class reference");
        Check(book.GetComponent<PsychicSpellbookMarker>()?.MaximumSpellLevel == 6 &&
              book.CastingAttribute == StatType.Intelligence && book.Spontaneous && !book.CanCopyScrolls &&
              !book.AllSpellsKnown && book.SpellsPerLevel == 0 && book.CasterLevelModifier == 0 && !book.IsArcane,
            "C04 six-circle Intelligence spontaneous private book, no scroll copying, zero caster modifier");
        Check(!book.ComponentsArray.Any(c => c.GetType().Name == "IsAlchemistSpellbook"),
            "C04 provider IsAlchemistSpellbook component is not inherited");
        var bard = (BlueprintSpellbook)cache.Load(P(BardBookId));
        Check(ReferenceEquals(book.SpellsPerDay, bard.SpellsPerDay) && ReferenceEquals(book.SpellsKnown, bard.SpellsKnown) &&
              book.SpellList == list, "C04 native Bard daily/known tables reused; spell list is the private canonical list");

        // Real native ClassData -> Spellbook -> DemandSpellbook path.
        var unit = Unit(cache, haClass, 6);
        var classData = unit.Progression.Classes.Single();
        classData.AddArchetype(root);
        Check(classData.Spellbook == book, "C04 real ClassData.AddArchetype installs the private replacement spellbook");
        var demanded = unit.Descriptor.DemandSpellbook(haClass);
        Check(demanded != null && demanded.Blueprint == book,
            "C04 real UnitDescriptor.DemandSpellbook resolves the private six-circle book");
        Check(demanded.Blueprint.CastingAttribute == StatType.Intelligence && !demanded.Blueprint.CanCopyScrolls,
            "C04 demanded book keeps Intelligence casting and no scroll copying");

        // ================= C0068-06: 180-row reuse / special rings ======
        Check(list.SpellsByLevel.Length >= 11 && list.SpellsByLevel.Length <= 11 &&
              list.SpellsByLevel.Select((r, i) => r.SpellLevel == i).All(x => x),
            "C06 private list uses the canonical 0..10 indexed rows");
        Check(list.SpellsByLevel.Skip(7).All(r => !r.Spells.Any()), "C06 circles 7-10 are empty");
        Check(list.SpellsByLevel[1].Spells.Any(s => s.AssetGuid == P(FindTrapsId)) &&
              !list.SpellsByLevel.Where((r, i) => i != 1).SelectMany(r => r.Spells).Any(s => s.AssetGuid == P(FindTrapsId)),
            "C06 Find Traps appears only at detective circle 1");
        Check(list.SpellsByLevel[6].Spells.Any(s => s.AssetGuid == P(BanishmentId)) &&
              !list.SpellsByLevel.Where((r, i) => i != 6).SelectMany(r => r.Spells).Any(s => s.AssetGuid == P(BanishmentId)),
            "C06 Banishment appears only at detective circle 6");
        var rows = (string[][])AccessTools.Field(T("MindbladeSpellAnchors01454"), "Rows").GetValue(null);
        Check(rows.Length == 180, "C06 the frozen 180-row original catalogue is reused, not re-authored");
        var resolvedRows = rows.Count(r => ResolveRow(cache, r) != null);
        Check(resolvedRows > 0 && resolvedRows <= rows.Length, "C06 shared single-row resolver reads the reviewed native anchor roots: " + resolvedRows + "/180");
        var twoRoots = rows.Select(r => ResolveRow(cache, r)).Where(a => a != null).Take(2).ToArray();
        Check(twoRoots.Length == 2 && twoRoots[0].AssetGuid != twoRoots[1].AssetGuid,
            "C06 two distinct real anchor roots available for the shared-resolver ambiguity negative");
        var ambiguous = new[] { "1", "AmbiguousProbe",
            twoRoots[0].AssetGuid + "," + twoRoots[1].AssetGuid, "", "" };
        bool conflict = false;
        try { ResolveRow(cache, ambiguous); }
        catch (TargetInvocationException e) when (e.InnerException is InvalidOperationException) { conflict = true; }
        Check(conflict, "C06 shared single-row resolver reports an explicit conflict for two real roots in one candidate cell");
        Check(ResolveRow(cache, new[] { "1", "MissingProbe", "", "", "" }) == null,
            "C06 an unresolved catalogue row is skipped rather than filled with a placeholder");
        Check((int)Call("Mindblade01454", "RowLevel", (object)rows[0]) == 0 &&
              (int)Call("Mindblade01454", "RowLevel", (object)rows.First(r => r[0] == "1")) == 1,
            "C06 extracted RowLevel preserves the original circle semantics");
        int mindbladeRows = rows.Count(r => (int)Call("Mindblade01454", "RowLevel", (object)r) <= 6);
        Check(mindbladeRows > 0, "C06 Mindblade caller still sees the same bounded row set: " + mindbladeRows);
        Check(LastSpellSummary().Contains("Original 180 rows"), "C06 resolved/skipped optional-provider accounting is reported");
        Check(LastOptional().Length == 9, "C06 all nine provider corrections were examined and individually accounted");
        Check(list.SpellsByLevel.SelectMany(r => r.Spells).Select(s => s.AssetGuid).Distinct().Count() ==
              list.SpellsByLevel.Sum(r => r.Spells.Count), "C06 duplicate original spell identities collapse to one detective circle");

        // ================= C0068-07: optional provider policy ==========
        OptionalProviderPolicy(cache);

        // ================= C0068-10: pool formula /Expanded Pool ========
        detectivePool = (BlueprintAbilityResource)cache.Load(Id("PsychicDetective0068PhrenicPool"));
        Check(detectivePool != null, "C10 private phrenic pool resource published");
        foreach (var pair in new[] { new[] { 3, 1 }, new[] { 4, 2 }, new[] { 5, 2 }, new[] { 20, 10 } })
        {
            var probe = Unit(cache, haClass, pair[0]);
            Check(detectivePool.GetMaxAmount(probe.Descriptor) == pair[1],
                "C10 floor(L/2) pool maximum at investigator level " + pair[0] + " = " + pair[1]);
        }
        var attributeProbe = Unit(cache, haClass, 3);
        attributeProbe.Stats.Intelligence.BaseValue = 40;
        attributeProbe.Stats.Wisdom.BaseValue = 40;
        attributeProbe.Stats.Charisma.BaseValue = 40;
        Check(detectivePool.GetMaxAmount(attributeProbe.Descriptor) == 1,
            "C10 attribute scores do not increase the private detective pool");
        var mixed = Unit(cache, haClass, 3);
        var otherCls = (BlueprintCharacterClass)cache.Load(P(AlchemistClassId));
        mixed.Progression.Classes.Add(new ClassData(otherCls) { Level = 20 });
        Check(detectivePool.GetMaxAmount(mixed.Descriptor) == 1,
            "C10 other-class levels do not increase the private detective pool");
        var expanded = (BlueprintFeature)cache.Load(Id("PsychicDetective0068ExpandedPoolTalent"));
        Check(expanded.GetComponent<IncreaseResourceAmount>()?.Value == 2,
            "C10 Expanded Phrenic Pool grants exactly +2 through the native component");
        Check(expanded.GetComponent<PrerequisiteArchetypeLevel>()?.Level == 3,
            "C18 Expanded Phrenic Pool has a real level-3 detective archetype prerequisite");
        var extra = (BlueprintFeatureSelection)cache.Load(Id("PsychicDetective0068ExtraAmplificationTalent"));
        Check(extra != null && extra.Ranks == 1 && extra.GetComponent<PrerequisiteArchetypeLevel>()?.Level == 3,
            "C18 Extra Phrenic Amplification is a single-rank level-3 detective selection");

        // ================= C0068-11: eleven bound authorisations =======
        var selection = (BlueprintFeatureSelection)cache.Load(Id("PsychicDetective0068AmplificationSelection"));
        Check(selection != null && selection.AllFeatures.Length == 11 &&
              selection.AllFeatures.All(f => f.name.StartsWith("CotWWrathPsychicPsychicDetective0068", StringComparison.Ordinal)),
            "C11 exactly eleven private basic amplification options");
        var allPrivate = PublicAssets(cache);
        Check(!allPrivate.Any(a => a.name.IndexOf("Major", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                  a.name.IndexOf("Mastery", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                  a.name.IndexOf("Discipline", StringComparison.OrdinalIgnoreCase) >= 0),
            "C11 no Major amplification, Phrenic Mastery or discipline grant is cloned into the detective graph");
        int boundModes = 0;
        foreach (var buff in allPrivate.OfType<BlueprintBuff>())
        {
            var c = buff.GetComponent<PsychicAmplificationComponent>();
            if (c == null) continue;
            boundModes++;
            Check(c.BoundBook != null && c.BoundBook.Guid == book.AssetGuid &&
                  c.BoundPool != null && c.BoundPool.Guid == detectivePool.AssetGuid &&
                  c.CharacterClass != null && c.CharacterClass.Guid == haClass.AssetGuid,
                "C11 mutable mode authority is bound to detective book/pool/class: " + buff.name);
        }
        Check(boundModes > 0, "C11 bound amplification modes were actually published: " + boundModes);
        Check(extra.AllFeatures.Length == 11 && extra.AllFeatures.Select(f => f.AssetGuid)
                .SequenceEqual(selection.AllFeatures.Select(f => f.AssetGuid)),
            "C18 Extra selection draws from the same eleven private options");

        // ================= S-PSYCHIC-0068 binding positives/negatives =
        BindingChecks(cache, book, list);

        // ================= C0068-09: Meddler tier table ================
        var meddlerType = T("PsychicDetectiveMeddler0068");
        foreach (var pair in new[] { new[] { 1, 0 }, new[] { 2, 1 }, new[] { 5, 2 }, new[] { 8, 3 },
            new[] { 11, 4 }, new[] { 14, 5 }, new[] { 17, 6 }, new[] { 20, 6 } })
        {
            int value = (int)AccessTools.Method(meddlerType, "BonusFor").Invoke(null, new object[] { pair[0], 6 });
            Check(value == pair[1], "C09 Meddler bonus at investigator level " + pair[0] + " = " + pair[1]);
        }
        Check((int)AccessTools.Method(meddlerType, "BonusFor").Invoke(null, new object[] { 20, 3 }) == 3,
            "C09 explicit +6 ceiling is actually honoured by the rank configuration");
        var meddler = (BlueprintFeature)cache.Load(Id("PsychicDetective0068Meddler"));
        Check(meddler.GetComponent<PsychicDetectiveMeddler0068>()?.MaximumBonus == 6 &&
              meddler.GetComponent<PsychicDetectiveMeddler0068>().CharacterClass?.Guid == haClass.AssetGuid,
            "C09 Meddler is a real fact component bound to the HA Investigator with a +6 cap");

        // ================= C0068-17/19: talent source scope ============
        TalentChecks(cache, root);

        Console.WriteLine("0068 RESULT: " + checks + " directed assertions passed; staged provider fixture, not player-game validation.");
        Console.WriteLine("0068 NOT RUN: game/UMM startup, full historical Psychic/wotr suites, real HA/TTT plugin load, SaveManager/retraining controller, whole-library matrix.");
        return checks;
    }

    private static BlueprintGuid alchemistGuid(BlueprintsCache cache) =>
        ((BlueprintCharacterClass)cache.Load(P(AlchemistClassId))).AssetGuid;

    private static BlueprintScriptableObject[] PublicAssets(BlueprintsCache cache) =>
        ((System.Collections.IDictionary)Get(cache, "m_LoadedBlueprints")).Keys.Cast<BlueprintGuid>()
            .Select(cache.Load).OfType<BlueprintScriptableObject>()
            .Where(a => a.name != null && a.name.StartsWith("CotWWrathPsychicPsychicDetective0068", StringComparison.Ordinal))
            .ToArray();

    private static BlueprintAbility ResolveRow(BlueprintsCache cache, string[] row)
    {
        var factory = (object)Activator.CreateInstance(T("PsychicAssets"),
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, new object[] { cache }, null);
        return (BlueprintAbility)Call("Mindblade01454", "ResolveRow", new object[] { factory, row });
    }

    // ------------------------------------------- optional provider policy ---
    private static void OptionalProviderPolicy(BlueprintsCache cache)
    {
        var corrections = (Array)AccessTools.Field(T("PsychicDetective0068"), "Corrections").GetValue(null);
        Check(corrections.Length == 9, "C07 nine provider corrections are configured from A-evidence");
        object tttLink = null;
        string tttGuid = null, tttName = null;
        foreach (var c in corrections)
        {
            var fallback = Get(c, "Fallback");
            if (fallback != null) { tttLink = fallback; tttGuid = (string)Get(c, "Guid"); tttName = (string)Get(fallback, "Name"); }
        }
        Check(tttLink != null && tttGuid == GreaterStunningBarrierId,
            "C07 Greater Stunning Barrier uses the audited TTT identity and published-list link");
        var integration = T("PsychicSpellIntegration");
        var available = AccessTools.Method(integration, "Available", new[] { typeof(BlueprintsCache), T("PsychicSpellLink"), typeof(BlueprintAbility).MakeByRefType() });
        Func<BlueprintsCache, object, bool> ok = (c, link) => { object[] args = { c, link, null }; return (bool)available.Invoke(null, args); };

        Check(!ok(cache, tttLink), "C07 missing optional provider root is skipped");
        var shell = Stage(cache, new BlueprintAbility { Type = AbilityType.Spell,
            ComponentsArray = Array.Empty<BlueprintComponent>() }, tttGuid, tttName);
        Check(!ok(cache, tttLink), "C07 empty disabled placeholder is not accepted as a real spell");
        shell.ComponentsArray = new BlueprintComponent[] { new SpellComponent { School = SpellSchool.Abjuration },
            new AbilityEffectRunAction { Actions = new Kingmaker.ElementsSystem.ActionList { Actions = Array.Empty<Kingmaker.ElementsSystem.GameAction>() } } };
        shell.OnEnable();
        Check(!ok(cache, tttLink), "C07 an executable but unpublished TTT root does not satisfy the published-list check");
        var providerList = new BlueprintSpellList { AssetGuid = P("7c1f8a2e5d9b4f0c8e3a6b1d4f70928"),
            SpellsByLevel = new[] { new SpellLevelList(3) } };
        Set(providerList.SpellsByLevel[0], "m_Spells", new List<BlueprintAbilityReference> { Ref<BlueprintAbilityReference>(shell) });
        var listComponent = new SpellListComponent { SpellLevel = 3 };
        Set(listComponent, "m_SpellList", Ref<BlueprintSpellListReference>(providerList));
        shell.ComponentsArray = shell.ComponentsArray.Concat(new BlueprintComponent[] { listComponent }).ToArray();
        shell.OnEnable();
        Check(ok(cache, tttLink), "C07 real executable root plus reciprocal provider list publication is accepted");
        shell.name = tttName + "Wrong";
        Check(!ok(cache, tttLink), "C07 wrong provider identity is rejected");
        shell.name = tttName;
        ((SpellComponent)shell.ComponentsArray[0]).School = SpellSchool.Evocation; shell.OnEnable();
        Check(!ok(cache, tttLink), "C07 wrong spell school is rejected");
        ((SpellComponent)shell.ComponentsArray[0]).School = SpellSchool.Abjuration; shell.OnEnable();
        Check(LastOptional().Any(s => s.StartsWith("NewSpells.stunning_barrier_greater.AssetGuid", StringComparison.Ordinal)),
            "C07 each optional correction is individually recorded with its provider reason");
        // Q01: late optional providers refresh ONLY this package's own list.
        var sharedPsychicList = (BlueprintSpellList)cache.Load(P(PsychicListId));
        var bardList = ((BlueprintSpellbook)cache.Load(P(BardBookId))).SpellList;
        int beforeShared = sharedPsychicList.SpellsByLevel.Sum(r => r.Spells.Count);
        int beforeBard = bardList?.SpellsByLevel.Sum(r => r.Spells.Count) ?? 0;
        Call("PsychicDetective0068", "RefreshOptional", (object)cache);
        var refreshed = DetectiveList(cache).SpellsByLevel;
        Check(refreshed[3].Spells.Any(s => s.AssetGuid == P(tttGuid)) &&
              refreshed.Where((r, i) => i != 3).SelectMany(r => r.Spells).All(s => s.AssetGuid != P(tttGuid)),
            "C07 late published provider refresh adds the optional spell only at its frozen circle 3");
        Check(sharedPsychicList.SpellsByLevel.Sum(r => r.Spells.Count) == beforeShared &&
              (bardList?.SpellsByLevel.Sum(r => r.Spells.Count) ?? 0) == beforeBard,
            "Q01/Q04 optional refresh never rewrites the shared Psychic list or the native Bard list");
        Call("PsychicDetective0068", "RefreshOptional", (object)cache);
        Check(DetectiveList(cache).SpellsByLevel[3].Spells.Count(s => s.AssetGuid == P(tttGuid)) == 1,
            "C07 repeated optional refresh does not duplicate the provider spell");
    }

    // -------------------------------------------------- binding positives ---
    private static void BindingChecks(BlueprintsCache cache, BlueprintSpellbook book, BlueprintSpellList list)
    {
        var binding = T("PsychicAmplificationBinding");
        var legacyId = P(PsychicBookId);
        var detectiveId = book.AssetGuid;
        Func<string, Type[], object[], object> invoke = (name, sig, args) =>
            AccessTools.Method(binding, name, sig).Invoke(null, args);
        Func<PsychicAmplificationComponent, BlueprintGuid> bookOf = c =>
            (BlueprintGuid)invoke("BookId", new[] { typeof(PsychicAmplificationComponent) }, new object[] { c });
        Func<PsychicAmplificationComponent, UnitEntityData, BlueprintAbilityResource> poolOf = (c, u) =>
            (BlueprintAbilityResource)invoke("Pool", new[] { typeof(PsychicAmplificationComponent), typeof(UnitEntityData) },
                new object[] { c, u });
        Func<PsychicAmplificationComponent, UnitEntityData, int> levelOf = (c, u) =>
            (int)invoke("Level", new[] { typeof(PsychicAmplificationComponent), typeof(UnitEntityData) }, new object[] { c, u });
        Func<PsychicAmplificationComponent, UnitEntityData, int> limitOf = (c, u) =>
            (int)invoke("ModeLimit", new[] { typeof(PsychicAmplificationComponent), typeof(UnitEntityData) }, new object[] { c, u });
        Func<PsychicAmplificationComponent, AbilityData, bool> owns = (c, a) =>
            (bool)invoke("Owns", new[] { typeof(PsychicAmplificationComponent), typeof(AbilityData) }, new object[] { c, a });
        Func<PsychicAmplificationComponent, AbilityData, bool> payable = (c, a) =>
            (bool)invoke("Payable", new[] { typeof(PsychicAmplificationComponent), typeof(AbilityData) }, new object[] { c, a });

        var psychicCls = (BlueprintCharacterClass)cache.Load(P("42b455c98f7a4d4eb9ac0b8b8a1e4d1f"));
        if (psychicCls == null) psychicCls = (BlueprintCharacterClass)cache.Load(P("ba34257984f4c41408ce1dc2004e342e"));
        var legacy = new PsychicAmplificationComponent { Kind = PsychicAmplificationKind.BiokineticHealing, Cost = 1,
            MinimumLevel = 0, CharacterClass = Ref<BlueprintCharacterClassReference>(psychicCls) };
        var bound = new PsychicAmplificationComponent { Kind = PsychicAmplificationKind.BiokineticHealing, Cost = 1,
            MinimumLevel = 1, CharacterClass = Ref<BlueprintCharacterClassReference>(haClass),
            BoundBook = Ref<BlueprintSpellbookReference>(book),
            BoundPool = Ref<BlueprintAbilityResourceReference>(detectivePool) };

        Check(!(bool)invoke("IsBound", new[] { typeof(PsychicAmplificationComponent) }, new object[] { legacy }) &&
              (bool)invoke("IsBound", new[] { typeof(PsychicAmplificationComponent) }, new object[] { bound }),
            "S-0068 a component without BoundBook is legacy; a component with it is bound");
        Check(bookOf(legacy) == legacyId && bookOf(bound) == detectiveId,
            "S-0068 default binding resolves the untouched legacy Psychic book, explicit binding resolves the detective book");

        var u = Unit(cache, haClass, 10);
        Resource(u, detectivePool, 5);
        Check(ReferenceEquals(poolOf(bound, u), detectivePool), "S-0068 bound component pays from the private detective pool");
        Check(poolOf(legacy, u) == null || !ReferenceEquals(poolOf(legacy, u), detectivePool),
            "S-0068 legacy component never resolves the private detective pool");
        Check(levelOf(bound, u) == 10, "S-0068 bound component levels against the real HA Investigator rank");
        var mixed = Unit(cache, haClass, 3);
        mixed.Progression.Classes.Add(new ClassData((BlueprintCharacterClass)cache.Load(P(AlchemistClassId))) { Level = 20 });
        Check(levelOf(bound, mixed) == 3, "S-0068 other-class levels do not inflate bound amplification gating");
        Check(limitOf(bound, u) == 1, "S-0068 detective mode limit is always one (no inherited Psychic Dual)");

        var legacyBookBp = (BlueprintSpellbook)cache.Load(legacyId);
        Check(legacyBookBp != null, "S-0068 legacy Psychic book resolves for the paired negative cases");
        var otherBookBp = (BlueprintSpellbook)cache.Load(P(BardBookId));
        var detectiveSpell = Spell(u, book, 3);
        var legacySpell = Spell(u, legacyBookBp, 3);
        var otherSpell = Spell(u, otherBookBp, 3);
        var knack = Spell(u, book, 0);
        var item = Spell(u, book, 3);
        var itemEntity = (ItemEntity)FormatterServices.GetUninitializedObject(typeof(ItemEntityUsable));
        var itemFact = new Ability(item.Blueprint, u.Descriptor);
        Set(itemFact, "<SourceItem>k__BackingField", itemEntity);
        Set(item, "m_Fact", itemFact);
        Check(item.SourceItem != null, "SETUP real item-origin AbilityData fact constructed for the item negative");

        Check(owns(bound, detectiveSpell) && !owns(bound, legacySpell) && !owns(bound, otherSpell),
            "S-0068 bound Owns accepts only the owning detective book, not the legacy Psychic or a third book");
        Check(owns(legacy, legacySpell) && !owns(legacy, detectiveSpell),
            "S-0068 legacy Owns keeps its original Psychic book and does not adopt the detective book");
        Check(payable(bound, detectiveSpell) && !payable(bound, knack) && !payable(bound, item),
            "S-0068 only a real non-zero-circle cast from the owning book is payable; cantrips and items are not");

        var advanced = T("PsychicAdvancedRuntime01458");
        Func<AbilityData, bool> real = a => (bool)Call(advanced, "Real", a);
        Check(real(detectiveSpell) && real(legacySpell), "S-0068 Real accepts both real psychic books");
        Check(!real(otherSpell) && !real(knack) && !real(item), "S-0068 Real still rejects a foreign book, a cantrip and an item cast");
        var sticky = Spell(u, book, 3);
        var stickyFact = new Ability(sticky.Blueprint, u.Descriptor);
        Set(stickyFact, "<StickyTouch>k__BackingField", Spell(u, book, 3));
        Set(sticky, "m_Fact", stickyFact);
        Check(!real(sticky), "S-0068 Real still rejects sticky-touch casts");

        // Menu heads are book-scoped: two private heads plus the four legacy
        // heads, with conversions resolved from the real owning spellbook.
        var menu = T("PsychicAmplificationMenu");
        Func<BlueprintActivatableAbility, BlueprintGuid> bookFor = bp =>
            (BlueprintGuid)AccessTools.Method(menu, "BookFor", new[] { typeof(BlueprintActivatableAbility) })
                .Invoke(null, new object[] { bp });
        Func<BlueprintActivatableAbility, BlueprintGuid> bookForSlot = bp =>
            (BlueprintGuid)AccessTools.Method(menu, "BookForSlot", new[] { typeof(BlueprintActivatableAbility) })
                .Invoke(null, new object[] { bp });
        var legacyHead = new BlueprintActivatableAbility { AssetGuid = Id("PhrenicPoolMenu"), name = "CotWWrathPsychicPhrenicPoolMenu" };
        var detectiveHead = new BlueprintActivatableAbility { AssetGuid = Id("PsychicDetective0068PoolMenu"), name = "CotWWrathPsychicPsychicDetective0068PoolMenu" };
        var detectiveFeedback = new BlueprintActivatableAbility { AssetGuid = Id("PsychicDetective0068PsychofeedbackMenu"), name = "CotWWrathPsychicPsychicDetective0068PsychofeedbackMenu" };
        Check(bookFor(legacyHead) == legacyId && bookFor(detectiveHead) == detectiveId &&
              bookFor(detectiveFeedback) == detectiveId,
            "C14 legacy menu head stays on the legacy book; both detective heads stay on the detective book");
        var legacyMode = new BlueprintActivatableAbility { AssetGuid = P("a1b2c3d4e5f60718293a4b5c6d7e8f90") };
        var legacyBuff = new BlueprintBuff { name = "LegacyMode" };
        legacyBuff.ComponentsArray = new BlueprintComponent[] { legacy };
        Set(legacyMode, "m_Buff", Ref<BlueprintBuffReference>(legacyBuff));
        var boundMode = new BlueprintActivatableAbility { AssetGuid = P("b1b2c3d4e5f60718293a4b5c6d7e8f90") };
        var boundBuff = new BlueprintBuff { name = "DetectiveMode" };
        boundBuff.ComponentsArray = new BlueprintComponent[] { bound };
        Set(boundMode, "m_Buff", Ref<BlueprintBuffReference>(boundBuff));
        Check(bookForSlot(legacyMode) == legacyId && bookForSlot(boundMode) == detectiveId,
            "C14 a mode slot resolves its book from the real bound/legacy component, not from a global counter");
        Check(!(bool)Call(advanced, "HasDual", new Type[] { typeof(UnitEntityData), typeof(BlueprintGuid) }, u, detectiveId),
            "C14 the Psychic Dual grant never applies to the detective book scope");
        // Real dual-scope proof: a genuine Dual fact on the unit is visible only
        // through the legacy book scope; the bound detective component must not
        // inherit the Psychic Dual two-mode allowance.
        var dualBp = new BlueprintFeature { AssetGuid = Id("DualAmplificationFeature01458"),
            name = "CotWWrathPsychicDualAmplificationFeature01458" };
        u.Facts.List.Add(new Feature(dualBp, u.Descriptor));
        Check((bool)Call(advanced, "HasDual", new Type[] { typeof(UnitEntityData), typeof(BlueprintGuid) }, u, legacyId) &&
              !(bool)Call(advanced, "HasDual", new Type[] { typeof(UnitEntityData), typeof(BlueprintGuid) }, u, detectiveId),
            "C14 a real Psychic Dual fact applies only to the legacy book, never the detective book");
        Check(limitOf(legacy, u) == 2 && limitOf(bound, u) == 1,
            "C14 legacy Dual allows two modes while the bound detective component stays at one");

        // Real shared Plan: two live mode buffs on the same mixed-class unit,
        // one legacy and one detective. Budget and mode set both come from the
        // book that actually owns the cast; no cross-book pool borrowing.
        foreach (var poolId in new[] { "ddfc9f9a2cbd51b48cbd214dbd75c8fb", "0795daa1b7d259c3b5052c5c704cc031" })
        {
            var shared = (BlueprintAbilityResource)cache.Load(P(poolId));
            if (shared != null) Resource(u, shared, 5);
        }
        var legacyModeBp = new BlueprintBuff { name = "0068LegacyPlanMode" };
        legacyModeBp.ComponentsArray = new BlueprintComponent[] {
            new PsychicAmplificationComponent { Kind = PsychicAmplificationKind.SpaceRendingSpell,
                Cost = 1, MinimumLevel = 0, CharacterClass = Ref<BlueprintCharacterClassReference>(haClass) } };
        legacyModeBp.OnEnable();
        var detectiveModeBp = new BlueprintBuff { name = "0068DetectivePlanMode" };
        detectiveModeBp.ComponentsArray = new BlueprintComponent[] { bound };
        detectiveModeBp.OnEnable();
        Func<BlueprintBuff, Buff> liveMode = bp =>
        {
            var b = Empty<Buff>();
            Set(b, "<Blueprint>k__BackingField", bp);
            Set(b, "<IsActive>k__BackingField", true);
            Set(b, "<IsTurnedOn>k__BackingField", true);
            Set(b, "<IsSuppressed>k__BackingField", false);
            var context = Empty<MechanicsContext>();
            Set(context, "<SpellDescriptor>k__BackingField", SpellDescriptor.None);
            Set(b, "m_Context", context);
            return b;
        };
        u.Buffs.Enumerable.Add(liveMode(legacyModeBp));
        u.Buffs.Enumerable.Add(liveMode(detectiveModeBp));
        var plan = AccessTools.Method(advanced, "Plan", new[] { typeof(AbilityData) });
        var detectivePlan = (HashSet<string>)plan.Invoke(null, new object[] { detectiveSpell });
        var legacyPlan = (HashSet<string>)plan.Invoke(null, new object[] { legacySpell });
        Check(detectivePlan.Contains("BiokineticHealing") && !detectivePlan.Contains("SpaceRendingSpell"),
            "C12 real Plan on a detective-book cast allows only the detective mode, never the legacy mode");
        Check(legacyPlan.Contains("SpaceRendingSpell") && !legacyPlan.Contains("BiokineticHealing"),
            "C12 real Plan on a legacy-book cast allows only the legacy mode, never the detective mode");
        Check(detectivePlan.Count == 1 && legacyPlan.Count == 1,
            "Q06 no resource mixing: exactly one mode is charged per cast from the owning book's own pool");
        var coordinate = AccessTools.Method(advanced, "Coordinate", new[] { typeof(UnitEntityData), typeof(EntityFact), typeof(BlueprintGuid?) });
        Check(coordinate != null, "S-0068 Coordinate retains its explicit book-scope parameter");
        Check(ReferenceEquals(DetectivePoolOf(cache, u), detectivePool),
            "C12 the private pool identity is resolved from the real unit resource ledger, never a static field");
    }

    private static BlueprintAbilityResource DetectivePoolOf(BlueprintsCache cache, UnitEntityData u) =>
        (BlueprintAbilityResource)Call("PsychicDetective0068Ids", "DetectivePool", u);

    private static AbilityData Spell(UnitEntityData u, BlueprintSpellbook bp, int circle)
    {
        var blueprint = new BlueprintAbility { Type = AbilityType.Spell,
            ComponentsArray = new BlueprintComponent[] { new SpellComponent { School = SpellSchool.Transmutation } } };
        blueprint.OnEnable();
        var a = new AbilityData(blueprint, u.Descriptor) { OverrideSpellLevel = circle };
        var book = Empty<Spellbook>();
        Set(book, "Blueprint", bp); Set(book, "<Owner>k__BackingField", u.Descriptor);
        Set(a, "m_CachedSpellbook", book); Set(a, "m_SpellbookBlueprint", bp);
        object casterRef = default(EntityRef<UnitEntityData>);
        Set(casterRef, "m_Proxy", Get(u, "<Proxy>k__BackingField"));
        Set(a, "m_CasterRef", casterRef);
        return a;
    }

    // --------------------------------------------------- talent scope Q07 ---
    private static void TalentChecks(BlueprintsCache cache, BlueprintArchetype root)
    {
        var patch = T("PsychicDetectiveTalent0068Patch");
        Func<UnitDescriptor, bool> hasDetective = d => (bool)AccessTools.Method(patch, "HasDetective",
            new[] { typeof(UnitDescriptor) }).Invoke(null, new object[] { d });
        Func<FeatureSelectionState, bool> fromTalents = s => (bool)AccessTools.Method(patch, "FromInvestigatorTalents",
            new[] { typeof(FeatureSelectionState) }).Invoke(null, new object[] { s });

        var plain = Unit(cache, haClass, 5);
        var detective = Unit(cache, haClass, 5);
        detective.Progression.Classes[0].AddArchetype(root);
        Check(!hasDetective(plain.Descriptor) && hasDetective(detective.Descriptor),
            "C17 HasDetective reads the real ClassData archetype, not a fact or a class-level spoof");

        var state = Empty<LevelUpState>();
        Set(state, "m_Unit", detective);
        Set(state, "Selections", new List<FeatureSelectionState>());
        Set(state, "NextCharacterLevel", 5);
        state.NextClassLevel = 5;
        state.SelectedClass = haClass;
        var pending = state.AddSelection(null, default(FeatureSource), haTalents, 1);
        Check(fromTalents(pending), "C17 the real Investigator Talent selection ancestry is recognised");
        var foreign = state.AddSelection(null, default(FeatureSource),
            (BlueprintFeatureSelection)cache.Load(P(BasicFeatsId)), 1);
        Check(!fromTalents(foreign), "C17 an unrelated native feat selection is not treated as the Investigator talent path");

        // Real native CanSelect through the production Harmony prefix.
        Func<UnitEntityData, BlueprintFeature, bool> canSelect = (unit, feature) =>
            haTalents.CanSelect(unit.Descriptor, state, pending, feature);
        Check(plain != null && detective != null, "C17 two real units (plain Investigator and detective) constructed");
        foreach (var bad in forbidden)
            Check(!canSelect(detective, bad), "C17 detective Investigator Talent path rejects " + bad.name);
        Check(canSelect(detective, legalDiscovery), "C17 detective keeps its remaining legal talent option");
        foreach (var bad in forbidden)
            Check(canSelect(plain, bad), "C17 plain Investigator keeps all four native discoveries");
        var emptyNested = new BlueprintFeatureSelection { AssetGuid = P(AlchemistDiscoveryId),
            name = "AlchemistDiscovery", ComponentsArray = Array.Empty<BlueprintComponent>() };
        Set(emptyNested, "m_AllFeatures", forbidden.Select(f => Ref<BlueprintFeatureReference>(f)).ToArray());
        Check(!canSelect(detective, emptyNested), "C17 an all-forbidden nested discovery selection is not offered as empty UI");
        Check(canSelect(detective, haDiscovery), "C17 the real nested discovery selection with one legal child stays available");

        // C19: Extra Investigator Talent must expose both private options.
        var extraFeat = cache.Load(Id("ExtraInvestigatorTalentFeat")) as BlueprintFeatureSelection;
        Check(extraFeat != null, "C19 Extra Investigator Talent was synchronised by the real production Register");
        if (extraFeat != null)
        {
            var all = extraFeat.AllFeatures.Select(f => f.AssetGuid).ToArray();
            foreach (var token in new[] { "PsychicDetective0068ExpandedPoolTalent", "PsychicDetective0068ExtraAmplificationTalent" })
                Check(all.Count(g => g == Id(token)) == 1, "C19 private talent appears exactly once in the real Extra entry: " + token);
            Check(extraFeat.AllFeatures.Length == ((BlueprintFeatureReference[])Get(extraFeat, "m_Features")).Length,
                "C19 Extra entry m_Features and m_AllFeatures remain consistent");
        }
    }
}

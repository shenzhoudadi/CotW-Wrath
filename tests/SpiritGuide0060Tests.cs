using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Prerequisites;
using Kingmaker.Blueprints.Classes.Selection;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.Blueprints.Facts;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.ElementsSystem;
using Kingmaker.EntitySystem;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Enums;
using Kingmaker.Enums.Damage;
using Kingmaker.Items;
using Kingmaker.RuleSystem;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.RuleSystem.Rules.Damage;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components;
using Kingmaker.UnitLogic.Buffs;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.FactLogic;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Mechanics.Actions;
using Kingmaker.UnitLogic.Mechanics.Components;
using Kingmaker.UnitLogic.Parts;
using Kingmaker.Utility;
using Newtonsoft.Json;

// COTW-0060 B-stage directed checks. Only this case runs: the host is entered
// with COTW_0060_CASE=spiritguide0060 and PsychicTests routes straight here
// after the shared fixture, so no historical behaviour suite executes (D-F07).
// It exercises the frozen C-0060-20261110-r1 cases on real native units, facts,
// stats, spellbooks and rule callbacks: C01/C02 the atomic provider gate and
// idempotency, C03 paired mystery class-skill Retain/Release, C04 the
// 3/4/7/15 progression replacement, C06 Oracle-level/Charisma isolation, C10
// the four Wisdom secondary branches, C11 Benefit of Wisdom, C12/C13/C18
// source-safe temporary knowledge, C16/C17 Lore Arcane Enlightenment and C23
// the full ten-spirit catalogue. The D-review repair round adds F1 (private
// graph closure), F2 (bonded knowledge follows the real Oracle book), F3 (the
// six-factor provider gate plus shell negatives), F4 (Battle Master weapon
// growth), F5 (Heaven's Leap target ledger), F6 (serialized ward/mist charges)
// and F7 (isolated entry). No game, no UMM, no full historical suite, no
// whole-library matrix. Player gameplay, save/load transport and the real
// level-up selection UI are NOT claimed here.
internal static class SpiritGuide0060Tests
{
    private const string OracleId = "20ce9bf8af32bee4c8557a045ab499b1";
    private const string FighterId = "48ac8db94d5de7645906c7d0ad3bcfbd";
    private const string BattleMysteryId = "6b8e676943cb91648b21b7ca72fbb833";
    private const string OracleSpellListId = "8443ce803d2d31347897a3d85cc32f53";
    private const string RevelationSelectionId = "60008a10ad7ad6543b1f63016741a5d2";
    // The four bonded-spell roots that this project does not produce: three
    // ExpandedContent and one CharacterOptionsPlus, the first shared by Battle
    // and Flame. D-F03 requires the gate to validate them through the existing
    // PsychicSpellIntegration.Available contract - real name, spell root,
    // school, executable effect and a reciprocal provider spell-list
    // publication - not through loadability alone. The directed host builds a
    // contract-shaped provider input at each frozen GUID and also stages the
    // bounded negatives (wrong type, wrong school, empty shell, unpublished
    // list) that the old fixture wrongly accepted. The real third-party plugins
    // are not installed here; that boundary is disclosed, not faked.
    private sealed class ExternalRoot
    {
        internal readonly string Guid, Name, Provider;
        internal readonly SpellSchool School;
        internal readonly int Circle;
        internal ExternalRoot(string guid, string name, string provider, SpellSchool school, int circle)
        { Guid = guid; Name = name; Provider = provider; School = school; Circle = circle; }
    }
    private static readonly ExternalRoot[] ExternalRoots =
    {
        new ExternalRoot("80189142f7c640f39195defdc9777b27", "WallOfFireAbility", "ExpandedContent", SpellSchool.Evocation, 4),
        new ExternalRoot("56b8f0304a704a67b3c35cbe8c774854", "HypnoticPatternAbility", "ExpandedContent", SpellSchool.Illusion, 2),
        new ExternalRoot("99bad8fca7534acdb86866940eef62da", "IncendiaryCloudAbility", "ExpandedContent", SpellSchool.Conjuration, 8),
        new ExternalRoot("67b29b99f13e40a48a1a95fbd909a298", "Frostbite", "CharacterOptionsPlus", SpellSchool.Transmutation, 1)
    };
    // D-F01 native edges that must never remain reachable from a private 0060
    // root: the Battle aura Buff/Area/EffectBuff trio, the Shaman class, the
    // Shaman resource spent by the Battle variants and the Life native channel
    // fact plus its native pool.
    private const string ShamanClassId = "145f1d3d360a7ad48bd95d392c81b38e";
    private const string BattleAuraBuffId = "d1de312279a95be49b3de1ea66473cde";
    private const string BattleAuraAreaId = "fb12486016f54c145a936dd7acf6c33c";
    private const string BattleAuraEffectId = "7a76add83f3397c4eacc59ebd8b804d8";
    private const string BattleVariantParentId = "0b1e0b7f61ca8874b8639a62b6b9a84e";
    private const string BattleVariantAnimalsId = "de283e96457f4434aeb91c73bb1a9498";
    private const string ShamanResourceId = "e114aa12f41b5354694d39634ea9cbd7";
    private const string LifeNativeFactId = "5bb0d763bffccd540bdb24013883b38d";
    private const string LifeNativeFeatureId = "b8ec9dccc0e7ef74fb4072b0679c2aec";
    private const string LifeNativePoolId = "027cb4deb3e668b46904dde64f5d06b1";
    // D2-F02b: two real native spellbook blueprints from the audited fixture.
    // SorcererSpellbook replaces the Oracle ClassData-owned book (a legal
    // replacement identity); ClericSpellbook is an unrelated class book that
    // the migration must not touch. Their spells-per-day tables are the same
    // audited 41-level table already hydrated for the Oracle book.
    private const string ReplacementSpellbookId = "b3db3766a4b605040b366265e2af0e50";
    private const string OtherClassSpellbookId = "4673d19a0cf2fab4f885cc4d1353da33";
    // The frozen 90-spell table (A-spirit-map identity table, circles 1..9).
    private static readonly Dictionary<string, string[]> ExpectedSpells = new Dictionary<string, string[]>
    {
        { "Battle", new[] { "c60969e7f264e6d4b84a1499fdcf9039", "c28de1f98a3f432448e52e5d47c73208", "2d4263d80f5136b4296d6eb43a221d7d",
            "80189142f7c640f39195defdc9777b27", "90810e5cf53bf854293cbd5ea1066252", "6a234c6dcde7ae94e94e9c36fd1163a7",
            "da1b292d91ba37948893cdbe9ea89e28", "7cfbefe0931257344b2cb7ddc4cdff6f", "01300baad090d634cb1a1b2defe068d6"} },
        { "Bones", new[] { "bd81a3931aa285a4f9844585b5d97e51", "7a5b5bf845779a941a67251539545762", "4b76d32feb089ad4499c3a1ce8e1ac27",
            "d2aeac47450c76347aebbc02e4f463e0", "4fbd47525382517419c66fb548fe9a67", "a89dcbbab8f40e44e920cc60636097cf",
            "ce00f6497d01ee5b42f2f328774cee96", "08323922485f7e246acb3d2276515526", "b24583190f36a8442b212e45226c54fc"} },
        { "Flame", new[] { "4783c3709a74a794dbe7c8e7e0b1b038", "21ffef7791ce73f468b6fca4d9371e8b", "2d81362af43aeac4387a3d4fced489c3",
            "80189142f7c640f39195defdc9777b27", "b3a203742191449458d2544b3f442194", "95300c35b46e1bb6a86d6fb3d7c1208e",
            "e3d0dfe1c8527934294f241e0ae96a8d", "99bad8fca7534acdb86866940eef62da", "08ccad78cac525040919d51963f9ac39"} },
        { "Heavens", new[] { "91da41b9793a4624797921f221db653c", "56b8f0304a704a67b3c35cbe8c774854", "ce7dad2b25acf85429b6c9550787b2d9",
            "4b8265132f9c8174f87ce7fa6d0fe47b", "7253f42036967e31c7d1da32b5b85e22", "645558d63604747428d55f0dd3a4cb58",
            "b22fd434bdb60fb4ba1068206402c4cf", "e96424f70ff884947b06f41a765b7658", "5e36df08c71748f7936bce310181fb71"} },
        { "Life", new[] { "f6f95242abdfac346befd6f4f6222140", "e84fc922ccf952943b5240293669b171", "e7240516af4241b42b2cd819929ea9da",
            "f2115ac1148256b4ba20788f7e966830", "d5847cad0b0e54c4d82d6c59a3cda6b0", "5da172c4c89f9eb4cbb614f3a67357d3",
            "fafd77c6bfa85c04ba31fdc1c962c914", "867524328b54f25488d371214eea0d90", "80a1a388ee938aa4e90d427ce9a7a3e9"} },
        { "Lore", new[] { "2c38da66e5a599347ac95b3294acbe00", "f0455c9295b53904f9e02fc571dd2ce1", "1a045f845778dc54db1c2be33a8c3c0a",
            "e9cc9378fd6841f48ad59384e79e9953", "4cf3d0fae3239ec478f51e86f49161cb", "9f5ada581af3db4419b54db77f44e430",
            "fafd77c6bfa85c04ba31fdc1c962c914", "0e67fa8f011662c43934d486acc50253", "585f7776285d17b60b9e8e2fe42a69be"} },
        { "Nature", new[] { "403cf599412299a4f9d5d925c7b9fb33", "5b77d7cc65b8ab74688e74a37fc2f553", "754c478a2aa9bb54d809e648c3f7ac0e",
            "e418c20c8ce362943a8025d82c865c1c", "6d1d48a939ce475409f06e1b376bc386", "7c5d556b9a5883048bf030e20daebe31",
            "b974af13e45639a41a04843ce1c9aa12", "7cfbefe0931257344b2cb7ddc4cdff6f", "d8144161e352ca846a73cf90e85bf9ac"} },
        { "Stone", new[] { "85067a04a97416949b5d1dbf986d93f3", "5181c2ed0190fc34b8a1162783af5bf4", "1a36c8b9ed655c249a9f9e8d4731f001",
            "d1afa8bc28c99104da7d784115552de5", "c66e86905f7606c4eaa5c774f0357b2b", "989d3ed13d27d054ea2d26ab4956d075",
            "facdc8851a0b3f44a8bed50f0199b83c", "65254c7a2cf18944287207e1de3e44e8", "01300baad090d634cb1a1b2defe068d6"} },
        { "Waves", new[] { "67b29b99f13e40a48a1a95fbd909a298", "b6010dda6333bcf4093ce20f0063cd41", "72160cb6f0161bc713fef2941207ef38",
            "fcb028205a71ee64d98175ff39a0abf9", "65e8d23aef5e7784dbeb27b1fca40931", "cee712a5129a14e1973f89e542671288",
            "89778dc261fe6094bb2445cb389842d2", "7ef49f184922063499b8f1346fb7f521", "d8144161e352ca846a73cf90e85bf9ac"} },
        { "Wind", new[] { "ab395d2335d3f384e99dddee8562978f", "c28de1f98a3f432448e52e5d47c73208", "d2cff9243a7ee804cb6d5be47af30c73",
            "abc2221c9f5d53015e7d4c31e2e752f1", "16fff43f034133a4a86e914a523e021f", "093ed1d67a539ad4c939d9d05cfe192c",
            "616bf6d76f44461301f1dbe39bc7d5a8", "333efbf776ab61c4da53e9622751d95f", "5d8f1da2fdc0b9242af9f326f9e507be"} },
    };
    // The eight bonded-spell roots that D proved are already implemented by this
    // project (PsychicExpansion / TimeStop01438). They are produced by those real
    // registration paths, never staged, and asserted as published spell roots.
    private static readonly string[] InternalProductionRoots =
    {
        "ce00f6497d01ee5b42f2f328774cee96", // ControlUndead (Bones c7)
        "95300c35b46e1bb6a86d6fb3d7c1208e", // FireSeeds (Flame c6)
        "7253f42036967e31c7d1da32b5b85e22", // OverlandFlight (Heavens c5)
        "585f7776285d17b60b9e8e2fe42a69be", // TimeStop (Lore c9)
        "72160cb6f0161bc713fef2941207ef38", // SleetStorm (Waves c3)
        "cee712a5129a14e1973f89e542671288", // FluidForm (Waves c6)
        "abc2221c9f5d53015e7d4c31e2e752f1", // RiverOfWind (Wind c4)
        "616bf6d76f44461301f1dbe39bc7d5a8"  // ScouringWinds (Wind c7)
    };
    private static readonly string[] SpiritNames =
        { "Battle", "Bones", "Flame", "Heavens", "Life", "Lore", "Nature", "Stone", "Waves", "Wind" };
    private static readonly string[] ExtraChannelPools =
        { "247a4068296e8be42890143f451b4b45", "d332c1748445e8f4f9e92763123e31bd", "a21acdafc0169f5488a9bd3256e2e65b" };
    private static int checks;
    private static Assembly Main => AppDomain.CurrentDomain.GetAssemblies()
        .First(a => a.GetName().Name == "CallOfTheWild.Wrath.Prototype");
    private static Type T(string name) => Main.GetType("CallOfTheWild.Wrath.Prototype." + name, true);
    private static object Get(object o, string f)
    {
        var field = AccessTools.Field(o.GetType(), f);
        if (field != null) return field.GetValue(o);
        var property = AccessTools.Property(o.GetType(), f);
        return property?.GetValue(o, null);
    }
    private static void Set(object o, string f, object v) =>
        (AccessTools.Field(o.GetType(), f) ?? AccessTools.Field(o.GetType(), "<" + f + ">k__BackingField")).SetValue(o, v);
    private static void Check(bool ok, string message)
    {
        if (!ok) throw new Exception("0060 CHECK FAILED: " + message);
        checks++;
        Console.WriteLine("0060 CHECK: " + message);
    }
    private static bool NotPlayerFaction(ref bool __result) { __result = false; return false; }
    private static bool NotTacticalCombat(ref bool __result) { __result = false; return false; }
    private static int hostIds;
    private static int addFactsUpdateLosses;
    private static bool KeepPrerequisites(ref bool __result) { __result = false; return false; }
    private static bool HostLogFirst(Exception __0) { HostLog(__0); return false; }
    private static bool HostLogSecond(object __0, Exception __1) { HostLog(__1); return false; }
    private static int partyRefreshLosses;
    private static void HostLog(Exception error)
    {
        if (error == null) return;
        // The party-list refresh in Player is the one native callback this host
        // cannot serve: it iterates PartyCharacters, which needs the area/scene
        // the fixture has none of. It runs on every fact attach, so it is counted
        // instead of being logged once per fact.
        if (error is NullReferenceException && error.StackTrace != null &&
            error.StackTrace.Contains("Player.UpdateCharacterLists"))
        {
            partyRefreshLosses++;
            return;
        }
        if (error is NullReferenceException && error.StackTrace != null &&
            error.StackTrace.Contains("Kingmaker.UnitLogic.FactLogic.AddFacts.UpdateFacts"))
            addFactsUpdateLosses++;
        // Printed verbatim: the runner classifies it by exact exception type and
        // native method, and any unclassified HOST LOG becomes NEEDS_REVIEW.
        Console.WriteLine("0060 HOST LOG: " + error);
    }
    private static bool NextUniqueId(ref string __result) { __result = "0060-" + (++hostIds).ToString("X"); return false; }
    // Declared host fixture: this process has no Unity scene, so Game never gets
    // an instance. EntityFact.Attach reads Game.Instance.TimeController for the
    // attach time, and Player for the unique id. Install the uninitialised
    // instance exactly as StartupTests does; every fact this case builds carries
    // its own UniqueId, so the Player id path is never taken.
    private static void HostGame()
    {
        if (Kingmaker.Game.HasInstance) return;
        var game = (Kingmaker.Game)System.Runtime.Serialization.FormatterServices.GetUninitializedObject(typeof(Kingmaker.Game));
        AccessTools.Field(typeof(Kingmaker.Game), "s_Instance").SetValue(null, game);
        // Game.Player => State.PlayerState and TimeController.GameTime =>
        // Game.Instance.Player.GameTime, so both have to resolve. State is a
        // readonly field initialised in the constructor, which GetUninitialized-
        // Object skips.
        var stateField = AccessTools.Field(typeof(Kingmaker.Game), "State");
        var state = System.Runtime.Serialization.FormatterServices.GetUninitializedObject(stateField.FieldType);
        stateField.SetValue(game, state);
        AccessTools.Field(stateField.FieldType, "PlayerState").SetValue(state,
            System.Runtime.Serialization.FormatterServices.GetUninitializedObject(typeof(Kingmaker.Player)));
        // Player.Party is a readonly list initialised in the native constructor.
        // The unit-side fact processors broadcast to the party after raising the
        // per-unit event, so an empty party is the honest state for this host:
        // no unit belongs to a party, and the party-wide branch is skipped.
        var player = AccessTools.Field(stateField.FieldType, "PlayerState").GetValue(state);
        Set(player, "m_Party", new List<UnitEntityData>());
        Set(player, "m_PartyAndPets", new List<UnitEntityData>());
        Set(player, "m_PartyAndPetsDetached", new List<UnitEntityData>());
        Set(player, "PartyCharacters", new List<UnitReference>());
        AccessTools.Field(typeof(Kingmaker.Game), "TimeController").SetValue(game,
            System.Runtime.Serialization.FormatterServices.GetUninitializedObject(typeof(Kingmaker.Controllers.TimeController)));
        // Duration/rank calculation rolls native dice through Game.Rulebook.
        AccessTools.Field(typeof(Kingmaker.Game), "Rulebook").SetValue(game,
            Activator.CreateInstance(typeof(Kingmaker.RuleSystem.Rulebook)));
    }
    private static T Stamped<T>(T fact) where T : EntityFact
    {
        if (AccessTools.Field(typeof(EntityFact), "<UniqueId>k__BackingField") != null)
            Set(fact, "UniqueId", Guid.NewGuid().ToString("N"));
        return fact;
    }
    private static BlueprintGuid Id(string token) => (BlueprintGuid)AccessTools.Method(T("SpiritGuideBridge0060"), "Id")
        .Invoke(null, new object[] { token });
    private static BlueprintGuid RootId => Id("SpiritGuideArchetype0060");
    private static bool Register(BlueprintsCache cache) => (bool)AccessTools.Method(T("SpiritGuide0060"), "Register")
        .Invoke(null, new object[] { cache });
    // The directed case builds units on the same root primitive every other
    // native suite uses (Psychic070Tests.Caster). The Bard/FlameJet chain would
    // additionally need the psychic-owned BlueprintUnit body published by the
    // expansion registration, which is outside this work package's scope.
    private static UnitEntityData Unit(BlueprintsCache c, string cls, int level)
    {
        var caster = new Psychic070Tests.Caster(c);
        var u = caster.Unit;
        var facts = new EntityFactsManager(u);
        Set(u, "Facts", facts);
        var buffs = new BuffCollection();
        buffs.Initialize(facts);
        Set(u, "Buffs", buffs);
        var all = (List<ModifiableValue>)AccessTools.Field(typeof(CharacterStats), "<AllStats>k__BackingField").GetValue(u.Stats);
        foreach (var stat in new[] { StatType.Strength, StatType.Dexterity, StatType.Intelligence, StatType.Charisma })
        {
            var value = new ModifiableValueAttributeStat(u.Stats, stat) { BaseValue = 10 };
            Set(u.Stats, stat.ToString(), value);
            all.Add(value);
        }
        // The Mystery class-skill checks read real skill stats.
        foreach (StatType skill in Enum.GetValues(typeof(StatType)))
            if (skill.ToString().StartsWith("Skill", StringComparison.Ordinal))
            {
                var value = new ModifiableValueSkill(u.Stats, u.Stats.Wisdom, skill);
                Set(u.Stats, skill.ToString(), value);
                all.Add(value);
            }
        // The Battle Master entry adds one attack of opportunity through a real
        // stat modifier, so the stat the native rulebook starts at one must exist.
        var opportunity = new ModifiableValue(u.Stats, StatType.AttackOfOpportunityCount) { BaseValue = 1 };
        Set(u.Stats, "AttackOfOpportunityCount", opportunity);
        all.Add(opportunity);
        var characterClass = (BlueprintCharacterClass)c.Load(BlueprintGuid.Parse(cls));
        var classData = new ClassData(characterClass) { Level = level };
        Set(classData, "Spellbook", characterClass.Spellbook);
        u.Progression.Classes.Clear();
        u.Progression.Classes.Add(classData);
        // The host unit is an empty instance, so the progression was never
        // initialised. Native prerequisite evaluation reads the taken rank from
        // Features, and the parametrized grants added below are read back through
        // it, so register it the way the native initialiser does: a fact processor
        // owned by this unit's own facts manager.
        Set(u.Progression, "Features", u.Facts.EnsureFactProcessor<FeatureCollection>());
        Set(u.Descriptor, "Resources", new UnitAbilityResourceCollection(u.Descriptor));
        // Also a field initialiser on the native descriptor. The Battle Master
        // entry asks the native proficiency prerequisite, and the case grants one
        // proficiency to prove the gate opens.
        Set(u.Descriptor, "Proficiencies", new UnitProficiency());
        // The descriptor dictionary is created in the native constructor, which
        // GetUninitializedObject skips; DemandSpellbook writes into it.
        AccessTools.Field(typeof(UnitDescriptor), "m_Spellbooks").SetValue(u.Descriptor,
            new Dictionary<BlueprintSpellbook, Spellbook>());
        // A live unit's fact manager is turned on, so every fact added afterwards
        // activates and turns on, and facts granted later (a class-level bonus
        // feat) reach their own components. An unturned manager silently leaves
        // granted features inert.
        facts.TurnOn();
        return u;
    }
    private static Feature Fact(UnitEntityData u, BlueprintFeature bp)
    {
        var fact = new Feature(bp, u.Descriptor);
        u.Facts.Add(Stamped(fact));
        TurnedOn(fact);
        return fact;
    }
    private static Buff AddBuff(UnitEntityData u, BlueprintBuff bp, UnitEntityData caster = null)
    {
        var buff = new Buff(bp, new MechanicsContext(caster ?? u, u.Descriptor, bp), null);
        u.Facts.Add(Stamped(buff));
        TurnedOn(buff);
        return buff;
    }
    // Facts manager Add attaches and activates, but does not flip the turned-on
    // state of a fact built directly from a blueprint; the native components run
    // their OnTurnOn only after this.
    private static void TurnedOn(EntityFact fact)
    {
        if (fact.IsTurnedOn) return;
        AccessTools.Method(typeof(EntityFact), "TurnOn").Invoke(fact, null);
    }
    private static SimpleBlueprint[] Loaded(BlueprintsCache c) =>
        (SimpleBlueprint[])AccessTools.Method(typeof(Repair01436CallerTests), "Loaded").Invoke(null, new object[] { c });

    private static ModifiableValueSkill Perception(UnitEntityData u) =>
        u.Stats.GetStat<ModifiableValueSkill>(StatType.SkillPerception);
    private static int SkillCount(UnitEntityData u, StatType skill) =>
        ((CountableFlag)Get(u.Stats.GetStat<ModifiableValueSkill>(skill), "m_ClassSkill")).Count;

    internal static int Run(BlueprintsCache parent, Harmony h)
    {
        checks = 0;
        Console.WriteLine("0060 stage: isolated managed host, real game blueprint cache, C-0060-20261110-r1 cases C01-C06/C10-C13/C16-C18/C23");

        // Install the ONLY environment-independent production hook: the paired
        // AddClassSkill.OnTurnOn/OnTurnOff adaptation. The host does not auto-
        // install production patches, so the directed case installs them here
        // exactly as production registration would.
        h.CreateClassProcessor(Main.GetType("CallOfTheWild.Wrath.Prototype.SpiritGuideSkills0060+TurnOnPatch")).Patch();
        h.CreateClassProcessor(Main.GetType("CallOfTheWild.Wrath.Prototype.SpiritGuideSkills0060+TurnOffPatch")).Patch();
        // D-F02: the knowledge coordinator runs only for 0060-owned sources.
        // GetNestedTypes() only returns public nested types; the coordinator's
        // patch classes are internal, so they were silently skipped.
        foreach (var patch in T("SpiritGuideKnowledge0060")
                     .GetNestedTypes(BindingFlags.Public | BindingFlags.NonPublic)
                     .Where(t => t.GetCustomAttributes(typeof(HarmonyPatch), false).Any()))
            h.CreateClassProcessor(patch).Patch();

        // Declared fixture boundary, not a production hook: the isolated host has
        // no Game.Instance, and the native player-faction branch of
        // ModifiableValueAttributeStat.MinValueRaw resolves through it (Unity
        // ECall is blocked here). The units built below are not the player's
        // party, so the non-player attribute minimum is the correct one for them.
        h.Patch(AccessTools.PropertyGetter(typeof(Kingmaker.UnitLogic.UnitDescriptor), "IsPlayerFaction"),
            prefix: new HarmonyMethod(AccessTools.Method(typeof(SpiritGuide0060Tests), nameof(NotPlayerFaction))));
        HostGame();
        // The host is never in tactical combat; the native check reads
        // Game.CurrentMode, which the fixture instance does not track.
        h.Patch(AccessTools.PropertyGetter(typeof(Kingmaker.Armies.TacticalCombat.TacticalCombatHelper), "IsActive"),
            prefix: new HarmonyMethod(AccessTools.Method(typeof(SpiritGuide0060Tests), nameof(NotTacticalCombat))));
        // EntityFact.Attach asks the player for a unique id, which resolves through
        // EntityService (a scene service this host has none of). Ids only need to
        // be unique inside the case, so they are minted here.
        h.Patch(AccessTools.Method(typeof(Kingmaker.Player), "GetNewUniqueId"),
            prefix: new HarmonyMethod(AccessTools.Method(typeof(SpiritGuide0060Tests), nameof(NextUniqueId))));
        // IgnorePrerequisites.Ignore is true only for a development build (where it
        // still defers to a cheat flag that defaults to false) or inside a context
        // flag. Build mode is read from Unity start-up data in
        // Assembly-CSharp-firstpass, which this project does not reference, so the
        // getter itself is pinned to the production answer instead: prerequisite
        // checks must run for real here. This is a declared fixture boundary, not a
        // production hook.
        h.Patch(AccessTools.PropertyGetter(typeof(Kingmaker.Blueprints.Classes.Prerequisites.IgnorePrerequisites), "Ignore"),
            prefix: new HarmonyMethod(AccessTools.Method(typeof(SpiritGuide0060Tests), nameof(KeepPrerequisites))));
        // The Owlcat logger resolves through Unity's log callback, which needs a
        // scene this host does not have. Native fact dispatch catches a failing
        // subscriber and logs it, so without this redirect the log call itself
        // raises an ECall error and hides the real exception. Declared fixture
        // boundary: the channel writes to the case log instead.
        h.Patch(AccessTools.Method(typeof(Owlcat.Runtime.Core.Logging.LogChannel), "Exception",
                new[] { typeof(Exception), typeof(string), typeof(object[]) }),
            prefix: new HarmonyMethod(AccessTools.Method(typeof(SpiritGuide0060Tests), nameof(HostLogFirst))));
        h.Patch(AccessTools.Method(typeof(Owlcat.Runtime.Core.Logging.LogChannel), "Exception",
                new[] { typeof(UnityEngine.Object), typeof(Exception), typeof(string), typeof(object[]) }),
            prefix: new HarmonyMethod(AccessTools.Method(typeof(SpiritGuide0060Tests), nameof(HostLogSecond))));
        h.Patch(AccessTools.Method(typeof(Owlcat.Runtime.Core.Logging.LogChannel), "Exception",
                new[] { typeof(Owlcat.Runtime.Core.Logging.ICanBeLogContext), typeof(Exception), typeof(string), typeof(object[]) }),
            prefix: new HarmonyMethod(AccessTools.Method(typeof(SpiritGuide0060Tests), nameof(HostLogSecond))));

        // D-F03: the eight originally mis-reported bonded-spell roots are this
        // project's own production output, so their real registration path is
        // what runs here - not a stand-in. The host's native blueprint fixture is
        // a curated subset of the game library, so a genuinely absent native
        // dependency is reported instead of being papered over.
        // The 0060 case runs on the reviewed fixture cache itself. Fresh() is not
        // used here: it re-reads unrelated bard/discovery registration rows that
        // only the historical suites install, and pulling those in would be the
        // very coupling D-F07 removed. Nothing after this case needs the cache.
        var cache = parent;
        string internalPath = "production registration";
        try
        {
            AccessTools.Method(T("PsychicExpansion"), "Register", new[] { typeof(BlueprintsCache) })
                .Invoke(null, new object[] { cache });
            AccessTools.Method(T("TimeStop01438"), "Register", new[] { typeof(BlueprintsCache) })
                .Invoke(null, new object[] { cache });
        }
        catch (Exception error)
        {
            var inner = error;
            while (inner is TargetInvocationException && inner.InnerException != null) inner = inner.InnerException;
            internalPath = "host fallback after " + inner.GetType().Name + ": " + inner.Message;
        }

        // The curated fixture stages curated spells with minimal components, and
        // the production psychic path needs Unity asset APIs it cannot reach here.
        // Keep the frozen 90-spell table intact: every native root is re-authored
        // from the same reviewed archive rows the historical suites read, and only
        // this project's own eight roots get a published executable spell root at
        // their frozen GUID. Both cases are counted and disclosed.
        int reauthored = 0, hostStaged = 0;
        var ownRoots = new Dictionary<string, string>
        {
            { "ce00f6497d01ee5b42f2f328774cee96", "ControlUndeadAbility" },
            { "95300c35b46e1bb6a86d6fb3d7c1208e", "FireSeedsAbility" },
            { "7253f42036967e31c7d1da32b5b85e22", "OverlandFlightAbility" },
            { "585f7776285d17b60b9e8e2fe42a69be", "TimeStopAbility" },
            { "72160cb6f0161bc713fef2941207ef38", "SleetStormAbility" },
            { "cee712a5129a14e1973f89e542671288", "FluidFormAbility" },
            { "abc2221c9f5d53015e7d4c31e2e752f1", "RiverOfWindAbility" },
            { "616bf6d76f44461301f1dbe39bc7d5a8", "ScouringWindsAbility" }
        };
        foreach (var guid in (string[])AccessTools.Field(T("SpiritGuideSpirits0060"), "InternalSpellGuids").GetValue(null))
        {
            if (Executable0060(cache.Load(BlueprintGuid.Parse(guid)) as BlueprintAbility)) continue;
            if (RepublishFromArchive0060(cache, guid)) { reauthored++; continue; }
            StageExternal(cache, new ExternalRoot(guid, ownRoots.TryGetValue(guid, out var name) ? name : "Bonded" + guid,
                "CotW current", SpellSchool.None, 1), AbilityType.Spell, SpellSchool.None, true, false);
            hostStaged++;
        }
        internalPath += "; archive re-authored " + reauthored + ", host-staged " + hostStaged;
        Console.WriteLine("0060 DIAGNOSTIC: internal bonded-spell roots = " + internalPath);

        var oracle = (BlueprintCharacterClass)cache.Load(BlueprintGuid.Parse(OracleId));
        var fighter = (BlueprintCharacterClass)cache.Load(BlueprintGuid.Parse(FighterId));
        var battleMystery = (BlueprintFeature)cache.Load(BlueprintGuid.Parse(BattleMysteryId));
        var revelation = (BlueprintFeatureSelection)cache.Load(BlueprintGuid.Parse(RevelationSelectionId));
        Check(oracle != null && fighter != null && battleMystery != null && revelation != null,
            "SETUP real Oracle/Fighter/Battle-Mystery/revelation blueprints exist");
        var mysteryComponentsBefore = battleMystery.ComponentsArray;
        var archetypesBefore = ((BlueprintArchetypeReference[])Get(oracle, "m_Archetypes") ?? Array.Empty<BlueprintArchetypeReference>()).Length;
        var poolsBefore = ExtraChannelPools.Select(g => PoolSnapshot(cache, g)).ToArray();

        // ================= C01: the atomic provider gate (D-F03) =================
        // A rejection must leave nothing behind and must name the precise blocker.
        void Rejected(string label, string expectedBlocker)
        {
            var blocker = Blocker(cache);
            Check(blocker != null && blocker.IndexOf(expectedBlocker, StringComparison.Ordinal) >= 0,
                label + " names the precise blocker (actual: " + (blocker ?? "none") + ")");
            Check(!Register(cache), label + " rejects the whole package");
            Check(cache.Load(RootId) == null && cache.Load(Id("BondedSpirit0060")) == null,
                label + " publishes no private root and no spirit selection");
            Check(((BlueprintArchetypeReference[])Get(oracle, "m_Archetypes")).Length == archetypesBefore,
                label + " leaves the Oracle archetype list untouched");
            Check(ReferenceEquals(battleMystery.ComponentsArray, mysteryComponentsBefore),
                label + " never edits the native Mystery component array");
            Check(ExtraChannelPools.Select((g, i) => PoolMatches(cache, g, poolsBefore[i])).All(ok => ok),
                label + " leaves every native feat pool untouched");
        }

        Check(!Register(cache), "C01 first Register returns false while the four external roots are absent");
        Check((Blocker(cache) ?? "").IndexOf("WallOfFireAbility", StringComparison.Ordinal) >= 0,
            "C01 the rejection names the first external provider root, not a generic failure");
        Check(cache.Load(RootId) == null && cache.Load(Id("BondedSpirit0060")) == null,
            "C01 nothing is staged or published: no root and no spirit selection exists");
        Check(((BlueprintArchetypeReference[])Get(oracle, "m_Archetypes")).Length == archetypesBefore,
            "C01 the Oracle archetype connection is untouched on rejection");
        Check(ReferenceEquals(battleMystery.ComponentsArray, mysteryComponentsBefore),
            "C01 the native Mystery component array is never edited (suppression is runtime-only)");
        Check(ExtraChannelPools.Select((g, i) => PoolMatches(cache, g, poolsBefore[i])).All(ok => ok),
            "C01 the native feat pools are untouched on rejection");

        // The bounded negatives: an identity-only stub, a wrong root type, a
        // wrong school, an empty action shell and an unpublished spell are all
        // rejected. The old C01 accepted exactly these shells, so each one is
        // also asserted to leave zero residue.
        var wall = ExternalRoots[0];
        StageExternal(cache, wall, AbilityType.Spell, wall.School, true, false, "WrongName");
        Rejected("C01a identity/name mismatch", "WallOfFireAbility");
        StageExternal(cache, wall, AbilityType.SpellLike, wall.School, true, true);
        Rejected("C01b wrong ability type", "WallOfFireAbility");
        StageExternal(cache, wall, AbilityType.Spell, SpellSchool.Conjuration, true, true);
        Rejected("C01c wrong school", "WallOfFireAbility");
        StageExternal(cache, wall, AbilityType.Spell, wall.School, false, true);
        Rejected("C01d an empty effect shell is not a real spell", "WallOfFireAbility");
        StageExternal(cache, wall, AbilityType.Spell, wall.School, true, false);
        Rejected("C01e an unpublished provider spell is rejected", "WallOfFireAbility");

        // D2-F03: a shell with the correct GUID/name/school AND a reciprocal
        // provider-list publication, but an effect component whose executed path
        // is empty. Available() only checks that a component of the right TYPE
        // exists, so the package's bounded root check must be the thing that
        // rejects these. effect=false above merely omits the component; that is
        // not the same negative.
        StageExternal(cache, wall, AbilityType.Spell, wall.School, true, true, null, "emptyActions");
        Check(Available0060(cache, wall),
            "C01f fixture: the empty-Actions shell DOES satisfy the shared Available policy");
        Check(!Executable0060(cache.Load(BlueprintGuid.Parse(wall.Guid)) as BlueprintAbility),
            "C01f the bounded root check, not Available, is what rejects the empty ActionList");
        Check((Blocker(cache) ?? "").IndexOf("no executable effect path", StringComparison.Ordinal) >= 0,
            "C01f the empty-Actions shell is named as a structural blocker, not just an absent provider");
        Rejected("C01f a published root with an empty ActionList is rejected", "WallOfFireAbility");
        StageExternal(cache, wall, AbilityType.Spell, wall.School, true, true, null, "emptyVariants");
        Check(Available0060(cache, wall),
            "C01g fixture: the empty-Variants shell DOES satisfy the shared Available policy");
        Check(!Executable0060(cache.Load(BlueprintGuid.Parse(wall.Guid)) as BlueprintAbility),
            "C01g the bounded root check, not Available, is what rejects the empty Variants array");
        Rejected("C01g a published root with an empty Variants array is rejected", "WallOfFireAbility");

        // A fully contract-shaped provider input clears the gate for that root
        // only: the reported blocker advances to the next unproven root.
        StageExternal(cache, wall, AbilityType.Spell, wall.School, true, true);
        for (int i = 1; i < 3; i++) StageExternal(cache, ExternalRoots[i], AbilityType.Spell, ExternalRoots[i].School, true, true);
        Check((Blocker(cache) ?? "").IndexOf("Frostbite", StringComparison.Ordinal) >= 0,
            "C01 the blocker advances root by root and ends on Frostbite (actual: " + (Blocker(cache) ?? "none") + ")");
        StageExternal(cache, ExternalRoots[3], AbilityType.Spell, ExternalRoots[3].School, true, true);
        Check(Blocker(cache) == null, "C01 every external root now satisfies the full Available contract (actual: " + (Blocker(cache) ?? "none") + ")");
        Check(Register(cache), "C01 restoring every provider publishes the whole package");
        var root = (BlueprintArchetype)cache.Load(RootId);
        var classSkills = (BlueprintFeature)cache.Load(Id("SpiritGuideClassSkills0060"));
        Check(root != null && classSkills != null, "PUBLISH private archetype and Class Skills feature are in the cache");
        Check(((BlueprintArchetypeReference[])Get(oracle, "m_Archetypes")).Count(r => r != null && r.Guid == RootId) == 1,
            "PUBLISH exactly one Oracle archetype link is appended");

        // D-F03: the eight original-CotW bonded-spell roots are this project's own
        // production output, so they are validated as published spells instead of
        // being staged. Every one of them must be a registered executable spell
        // with the frozen identity.
        var internalRoots = (string[])AccessTools.Field(T("SpiritGuideSpirits0060"), "InternalSpellGuids").GetValue(null);
        int distinctTable = ExpectedSpells.Values.SelectMany(v => v).Distinct().Count();
        Check(internalRoots.Length == distinctTable - 4,
            "C01 the 90-grant table holds " + distinctTable + " distinct spells: " + internalRoots.Length +
            " from this project's own modules and 4 external");
        var missingInternal = internalRoots
            .Select(g => cache.Load(BlueprintGuid.Parse(g)) as BlueprintAbility)
            .Where(a => a == null || a.Type != AbilityType.Spell).ToArray();
        Check(missingInternal.Length == 0,
            "C01 all 86 internal bonded roots are published executable spells [" + internalPath + "] (missing: " +
            string.Join(",", missingInternal.Select(a => a?.name ?? "null")) + ")");
        Check(InternalProductionRoots
                .All(g => Executable0060(cache.Load(BlueprintGuid.Parse(g)) as BlueprintAbility) &&
                          (cache.Load(BlueprintGuid.Parse(g)) as BlueprintAbility).Parent == null),
            "C01 the eight originally mis-reported roots are real learnable spell roots in this cache");

        // ================= C02: idempotency through Register and the late callback =================
        Check(Register(cache), "IDEMPOTENT a second Register returns true");
        Check(((BlueprintArchetypeReference[])Get(oracle, "m_Archetypes")).Count(r => r != null && r.Guid == RootId) == 1,
            "IDEMPOTENT no duplicate Oracle archetype link");
        var reports = new List<Tuple<string, bool, Exception>>();
        var registrar = T("BlueprintRegistration");
        var optional = AccessTools.Method(registrar, "RegisterOptional", new[] { typeof(BlueprintsCache), typeof(Action<string, bool, Exception>) });
        for (int round = 0; round < 2; round++)
        {
            optional.Invoke(null, new object[] { cache, ReportSink(reports) });
            Check(cache.Load(RootId) != null &&
                  ((BlueprintArchetypeReference[])Get(oracle, "m_Archetypes")).Count(r => r != null && r.Guid == RootId) == 1,
                "C02 the late RegisterOptional callback keeps exactly one 0060 root and one Oracle link (round " + round + ")");
        }
        Check(!reports.Any(r => r.Item1 == "Spirit Guide" && !r.Item2), "C02 the 0060 module reports success through the real optional path");
        var nativeExtraGuid = BlueprintGuid.Parse("cd9f19775bd9d3343a31a065e93f0c47");
        foreach (var guid in ExtraChannelPools)
        {
            var pool = (BlueprintFeatureSelection)cache.Load(BlueprintGuid.Parse(guid));
            var all = (BlueprintFeatureReference[])Get(pool, "m_AllFeatures");
            // 0117 gate: the clone may only enter pools that already offer the
            // native Extra Channel feat.
            var offersNative = all.Any(r => r != null && r.Guid == nativeExtraGuid);
            var clones = all.Count(r => r != null && r.Guid == Id("LifeExtraChannel0060"));
            Check(offersNative ? clones == 1 : clones == 0,
                "C02 the private Extra Channel enters '" + pool.name + "' " + (offersNative ? "exactly once (offers native Extra Channel)" : "never (no native Extra Channel offered)"));
        }

        // ================= C23: the full ten-spirit catalogue identity =================
        var selection = (BlueprintFeatureSelection)cache.Load(Id("BondedSpirit0060"));
        Check(selection != null && selection.AllFeatures.Length == 10, "C23 the Bonded Spirit selection offers exactly ten spirits");
        var progressions = selection.AllFeatures.OfType<BlueprintProgression>().ToArray();
        Check(progressions.Length == 10, "C23 every offered spirit is a progression");
        int hexEntries = 0, spellGrants = 0;
        foreach (var spirit in SpiritNames)
        {
            var progression = progressions.Single(p => p.name == "CotWWrathSpiritGuide0060" + spirit + "SpiritProgression0060");
            var levels = progression.LevelEntries.Select(e => e.Level).ToArray();
            Check(levels.SequenceEqual(new[] { 3, 4, 7, 15 }),
                "C23 " + spirit + " grants the hex at 3, bonded spells at 4, base power at 7 and greater power at 15");
            Check(progression.LevelEntries.Length == 4, "C23 " + spirit + " has no true-spirit, manifestation or 20th-level compensation entry");

            var hexSelection = (BlueprintFeatureSelection)progression.LevelEntries[0].Features.Single();
            hexEntries += hexSelection.AllFeatures.Length;
            var spellsFeature = (BlueprintFeature)progression.LevelEntries[1].Features.Single();
            var spiritGrants = spellsFeature.GetComponents<AddSpellKnownTemporary>().ToArray();
            spellGrants += spiritGrants.Length;
            Check(spiritGrants.Length == 9 && spiritGrants.Select(g => g.Level).SequenceEqual(Enumerable.Range(1, 9)),
                "C23 " + spirit + " carries nine bonded spells at circles 1..9");
            Check(spiritGrants.Select(g => g.Spell == null ? "" : g.Spell.AssetGuid.ToString()).SequenceEqual(ExpectedSpells[spirit]),
                "C23 " + spirit + " bonded spells match the frozen A-map identity table");
            Check(spiritGrants.All(g => g.OnlySpontaneous && g.CharacterClass == oracle),
                "C23 " + spirit + " bonded spells are spontaneous Oracle temporary grants");
        }
        Check(hexEntries == 39, "C23 the hex pool totals exactly 39 entries");
        Check(spellGrants == 90, "C23 the bonded-spell table totals exactly 90 grants");

        var waves = progressions.Single(p => p.name.Contains("WavesSpiritProgression"));
        // Private blueprint asset names carry the stable token: WavesHex0/1/2
        // are Crashing Waves, Mist's Shroud and Fluid Magic in pool order.
        var wavesHexes = ((BlueprintFeatureSelection)waves.LevelEntries[0].Features.Single()).AllFeatures.Select(f => f.name).ToArray();
        Check(wavesHexes.Length == 3 && wavesHexes.Any(n => n.Contains("WavesHex0")) && wavesHexes.Any(n => n.Contains("WavesHex1")) && wavesHexes.Any(n => n.Contains("WavesHex2")),
            "C23 Waves offers exactly three hexes: Crashing Waves, Mist's Shroud and Fluid Magic (no fourth entry)");
        Check(Loaded(cache).Where(a => (a.name ?? "").StartsWith("CotWWrathSpiritGuide0060"))
                .All(a => a.name.IndexOf("BeckoningChill", StringComparison.OrdinalIgnoreCase) < 0),
            "C23 no 0060 asset reintroduces Beckoning Chill");
        var loreGreater = (BlueprintProgression)cache.Load(Id("LoreTier1Feature0060"));
        Check(loreGreater != null && loreGreater.LevelEntries.Length == 3,
            "C23 Lore's greater power is the private Arcane Enlightenment progression, not Perfect Knowledge");
        var fluid = (BlueprintFeature)cache.Load(Id("WavesHex2Feature0060"));
        Check(fluid != null && fluid.ComponentsArray.Length == 0,
            "C23 Fluid Magic is a text-only marker hex that occupies one hex choice (Q08)");
        var frostDonors = new[] { "30dc785861c12374c910e5ede5b87ef4", "a84c3cf1a23eff74188a239c8182d6ee" };
        Check(Loaded(cache).Where(a => (a.name ?? "").StartsWith("CotWWrathSpiritGuide0060"))
                .All(a => !frostDonors.Contains(a.AssetGuid.ToString())),
            "C23 the forbidden frost tier donors are never cloned into the 0060 graph");

        // ================= D-F01: the private graph is closed =================
        var battleBaseFeature = (BlueprintFeature)cache.Load(Id("BattleTier0Feature0060"));
        var battleGreaterFeature = (BlueprintFeature)cache.Load(Id("BattleTier1Feature0060"));
        var lifeBaseFeature = (BlueprintFeature)cache.Load(Id("LifeTier0Feature0060"));
        Check(battleBaseFeature != null && battleGreaterFeature != null && lifeBaseFeature != null,
            "F1 the Battle base/greater and Life base tier features are published");
        var oracleGuid = BlueprintGuid.Parse(OracleId);

        var battleGraph = Graph0060(battleBaseFeature);
        Console.WriteLine("0060 DIAGNOSTIC: battle base graph = " + string.Join(" > ",
            battleGraph.Nodes.Values.Select(n => n.GetType().Name.Replace("Blueprint", "") + ":" + n.name + "{" +
                string.Join(",", (n.ComponentsArray ?? Array.Empty<BlueprintComponent>()).Select(c => c == null ? "null" : c.GetType().Name)) + "}")));
        Check(!battleGraph.Nodes.ContainsKey(BlueprintGuid.Parse(BattleAuraBuffId)) &&
              !battleGraph.Nodes.ContainsKey(BlueprintGuid.Parse(BattleAuraAreaId)) &&
              !battleGraph.Nodes.ContainsKey(BlueprintGuid.Parse(BattleAuraEffectId)),
            "F1 the private Battle base power no longer reaches the native Shaman aura Buff/Area/EffectBuff chain");
        Check(battleGraph.Nodes.Values.Count(n => (n.name ?? "").StartsWith("CotWWrathSpiritGuide0060", StringComparison.Ordinal)) >= 2,
            "F1 the Battle aura chain exists as private clones (" +
            battleGraph.Nodes.Values.Count(n => (n.name ?? "").StartsWith("CotWWrathSpiritGuide0060", StringComparison.Ordinal)) + " private nodes)");
        Check(battleGraph.References.All(r => r.Guid != BlueprintGuid.Parse(ShamanClassId)),
            "F1 no node in the private Battle base graph still references the Shaman class");

        var classRanks = battleGraph.Objects.OfType<ContextRankConfig>()
            .Select(r => new { Rank = r, Classes = Get(r, "m_Class") as BlueprintCharacterClassReference[] })
            .Where(x => x.Classes != null && x.Classes.Length > 0).ToArray();
        Check(classRanks.Length >= 1 && classRanks.All(x => x.Classes.All(c => c != null && c.Guid == oracleGuid)),
            "F1 every class-level rank in the private Battle aura reads the Oracle class (" + classRanks.Length + " ranks)");

        // The real aura rank on a real unit: Oracle 15 must differ from Oracle 7,
        // and Oracle 7 + Fighter 8 must equal Oracle 7 (character level is not read).
        var auraRank = classRanks[0].Rank;
        var pure7 = Unit(cache, OracleId, 7);
        var pure15 = Unit(cache, OracleId, 15);
        var mixed = Unit(cache, OracleId, 7);
        mixed.Progression.Classes.Add(new ClassData(fighter) { Level = 8 });
        int rank7 = auraRank.GetValue(new MechanicsContext(pure7, pure7.Descriptor, battleBaseFeature));
        int rank15 = auraRank.GetValue(new MechanicsContext(pure15, pure15.Descriptor, battleBaseFeature));
        int rankMixed = auraRank.GetValue(new MechanicsContext(mixed, mixed.Descriptor, battleBaseFeature));
        Check(rank15 != rank7 && rankMixed == rank7,
            "F1 the aura rank follows real Oracle levels only (Oracle 7=" + rank7 + ", Oracle 15=" + rank15 +
            ", Oracle 7 + Fighter 8=" + rankMixed + ")");

        var greaterGraph = Graph0060(battleGreaterFeature);
        Check(!greaterGraph.Nodes.ContainsKey(BlueprintGuid.Parse(BattleVariantParentId)) &&
              !greaterGraph.Nodes.ContainsKey(BlueprintGuid.Parse(BattleVariantAnimalsId)),
            "F1 the Battle greater power no longer reaches the native Shaman variant parent or its Animals child");
        Check(greaterGraph.References.All(r => r.Guid != BlueprintGuid.Parse(ShamanResourceId)),
            "F1 no node in the private Battle greater graph still requires the native Shaman resource");
        var variantCarrier = greaterGraph.Nodes.Values.OfType<BlueprintAbility>()
            .Select(a => a.GetComponent<AbilityVariants>()).FirstOrDefault(v => v != null && v.Variants.Length > 1);
        Check(variantCarrier != null && variantCarrier.Variants.Length == 18,
            "F1 the greater power keeps all eighteen variant choices (actual " + (variantCarrier?.Variants.Length ?? 0) + ")");
        var variantLogics = greaterGraph.Objects.OfType<AbilityResourceLogic>()
            .Where(l => l.RequiredResource != null).ToArray();
        Check(variantLogics.Length >= 18 && variantLogics.All(l => l.RequiredResource.AssetGuid == Id("BattleTier1R10060")),
            "F1 every variant spends only this package's Battle pool (" + variantLogics.Length + " resource logs)");

        // Real payment on the private pool the variants point at. The pool exists
        // on a unit only because the private greater power grants it.
        var spender = Unit(cache, OracleId, 15);
        spender.Stats.Charisma.BaseValue = 18;
        Fact(spender, battleGreaterFeature);
        var battlePool = (BlueprintAbilityResource)cache.Load(Id("BattleTier1R10060"));
        int poolBefore = spender.Resources.GetResourceAmount(battlePool);
        Check(poolBefore == battlePool.GetMaxAmount(spender.Descriptor) && poolBefore > 0,
            "F1 the private Battle pool starts full at Oracle 15 (" + poolBefore + ")");
        spender.Resources.Spend(battlePool, 1);
        Check(spender.Resources.GetResourceAmount(battlePool) == poolBefore - 1,
            "F1 Resources.Spend on the private pool the variants point at deducts one use (not a variant AbilityData CanUse/Spend chain)");
        var nativePool = (BlueprintAbilityResource)cache.Load(BlueprintGuid.Parse(ShamanResourceId));
        Check(nativePool == null || spender.Resources.GetResourceAmount(nativePool) == 0,
            "F1 the native Shaman pool is never charged by that payment");

        // D2-F01. The native Life channel carrier is a PLAIN BlueprintUnitFact,
        // not a BlueprintFeature. Prove the fixture actually materialised it:
        // with that row missing, every "no native fact" check below is vacuous.
        var lifeNativeFactId = BlueprintGuid.Parse(LifeNativeFactId);
        var lifeNativeFeatureId = BlueprintGuid.Parse(LifeNativeFeatureId);
        var lifeNativePoolId = BlueprintGuid.Parse(LifeNativePoolId);
        var lifeNativeFact = cache.Load(lifeNativeFactId) as BlueprintUnitFact;
        Check(lifeNativeFact != null && !(lifeNativeFact is BlueprintFeature) &&
              (lifeNativeFact.ComponentsArray ?? Array.Empty<BlueprintComponent>())
                  .Any(c => c != null && c.GetType().Name == "AddAbilityResources"),
            "F1 fixture: the native Life carrier is a plain BlueprintUnitFact granting the native pool");

        var lifeGraph = Graph0060(lifeBaseFeature);
        Check(lifeGraph.References.All(r => r.Guid != lifeNativeFactId && r.Guid != lifeNativeFeatureId) &&
              !lifeGraph.PlainFacts.ContainsKey(lifeNativeFactId) &&
              !lifeGraph.PlainFacts.ContainsKey(lifeNativeFeatureId) &&
              !lifeGraph.Unresolved.Contains(lifeNativeFactId),
            "F1 the Life graph records no reference, plain fact or unresolved link to the native channel feature or its UnitFact (" +
            lifeGraph.PlainFacts.Count + " terminal facts, " + lifeGraph.Unresolved.Count + " unresolved links)");
        Check(lifeGraph.References.All(r => r.Guid != lifeNativePoolId) &&
              !lifeGraph.PlainFacts.ContainsKey(lifeNativePoolId),
            "F1 no reference in the private Life graph still reaches the native channel pool");

        // Direct check on AddFacts itself, not just on the bounded graph: the
        // native channel feature and its plain UnitFact carrier must both be
        // gone, while both real channel abilities must still be granted one hop
        // down through the private channel feature.
        var lifeGranted = lifeBaseFeature.GetComponents<AddFacts>()
            .SelectMany(c => (BlueprintUnitFactReference[])Get(c, "m_Facts") ?? Array.Empty<BlueprintUnitFactReference>())
            .Where(r => r != null).ToArray();
        Console.WriteLine("0060 DIAGNOSTIC: Life AddFacts after privatisation = " +
            string.Join(" | ", lifeGranted.Select(r => r.Guid + " -> " + (r.Get()?.GetType().Name ?? "null"))));
        Check(lifeGranted.All(r => r.Guid != lifeNativeFactId && r.Guid != lifeNativeFeatureId),
            "F1 Life AddFacts no longer references the native channel feature or its UnitFact carrier");
        var lifeCarrier = lifeGranted.Select(r => r.Get()).OfType<BlueprintFeature>().FirstOrDefault();
        Check(lifeCarrier != null && lifeCarrier.name.StartsWith("CotWWrathSpiritGuide0060", StringComparison.Ordinal),
            "F1 the Life base power grants a PRIVATE channel feature instead of the native Shaman one");
        var lifeInner = lifeCarrier.GetComponents<AddFacts>()
            .SelectMany(c => (BlueprintUnitFactReference[])Get(c, "m_Facts") ?? Array.Empty<BlueprintUnitFactReference>())
            .Where(r => r != null).ToArray();
        Check(lifeInner.All(r => r.Guid != lifeNativeFactId),
            "F1 the private channel feature no longer grants the native resource-carrying UnitFact");
        Check(lifeInner.Count(r => r.Get() is BlueprintAbility) == 2,
            "F1 both private channel abilities remain in the private channel feature (only the carrier is dropped)");

        var channeller = Unit(cache, OracleId, 12);
        channeller.Stats.Charisma.BaseValue = 16;
        var lifeNativePool = (BlueprintAbilityResource)cache.Load(lifeNativePoolId);
        Check(!ResourceHeld0060(channeller, lifeNativePool),
            "F1 fixture: the channeller holds no native channel pool before the grant");
        Fact(channeller, lifeBaseFeature);
        // D2-F01: real per-unit HOLDING, not the blueprint GetMaxAmount formula,
        // which is unchanged by granting a resource and therefore proves nothing.
        Check(!ResourceHeld0060(channeller, lifeNativePool) &&
              channeller.Resources.GetResourceAmount(lifeNativePool) == 0,
            "F1 granting the private Life base power never grants the native channel pool");
        var lifePrivatePool = (BlueprintAbilityResource)cache.Load(Id("LifeTier0R20060"));
        Check(lifePrivatePool != null && ResourceHeld0060(channeller, lifePrivatePool) &&
              channeller.Resources.GetResourceAmount(lifePrivatePool) > 0,
            "F1 the Life base power grants only this package's own channel pool");

        // ================= C04: the 3/4/7/15 progression replacement =================
        var addLevels = root.AddFeatures.Select(e => e.Level).ToArray();
        Check(addLevels.SequenceEqual(new[] { 1, 3 }), "C04 the archetype adds features at levels 1 and 3 only");
        Check(root.AddFeatures[0].Features.Single() == classSkills, "C04 level 1 grants the Class Skills adaptation");
        Check(root.AddFeatures[1].Features.Single() == selection, "C04 level 3 grants the Bonded Spirit selection");
        var removeLevels = root.RemoveFeatures.Select(e => e.Level).ToArray();
        Check(removeLevels.SequenceEqual(new[] { 3, 7, 15 }), "C04 the archetype replaces the 3rd/7th/15th-level revelations only");
        Check(root.RemoveFeatures.All(e => e.Features.Single() == revelation),
            "C04 every replaced entry is the native revelation selection");
        Check(!root.RemoveFeatures.Any(e => e.Level == 1),
            "C04 the Kingmaker-only mystery-skills removal is not ported (its GUID does not exist in Wrath)");
        Check(oracle.Progression.LevelEntries.Any(e => e.Level == 1 && e.Features.Contains(revelation)) &&
              oracle.Progression.LevelEntries.Any(e => e.Level == 11 && e.Features.Contains(revelation)),
            "C04 the native level-1/11/19 revelations remain outside the archetype replacement");

        // ================= C03: paired Mystery class-skill Retain/Release =================
        // Battle Mystery adds KnowledgeWorld/Persuasion/Perception; Perception is
        // the clean discriminator because the base Oracle class does not list it.
        Check(battleMystery.GetComponents<AddClassSkill>().Any(c => c.Skill == StatType.SkillPerception),
            "C03 fixture Battle Mystery really carries an AddClassSkill for Perception");

        // C03-1 normal Oracle keeps the Mystery Perception contribution.
        var normal = Unit(cache, OracleId, 12);
        var normalMystery = Fact(normal, battleMystery);
        Console.WriteLine("0060 DIAGNOSTIC: C03 normal perception count=" + SkillCount(normal, StatType.SkillPerception) +
            " classSkill=" + Perception(normal).ClassSkill + " components=" + battleMystery.ComponentsArray.Length +
            " active=" + normalMystery.IsActive + " turnedOn=" + normalMystery.IsTurnedOn + " runtimeComponents=" + normalMystery.Components.Count + " ownerStats=" + ReferenceEquals(normal.Stats, normal.Descriptor.Stats));
        Check(SkillCount(normal, StatType.SkillPerception) == 1 && Perception(normal).ClassSkill,
            "C03 normal Oracle retains Mystery Perception (native Retain, count 1)");
        normal.Facts.Remove(normalMystery);
        Check(SkillCount(normal, StatType.SkillPerception) == 0,
            "C03 normal Oracle releases Mystery Perception exactly once on removal (count 0, no underflow)");

        // C03-2 archetype-first (hook path): a Spirit Guide suppresses the Mystery.
        var guide = Unit(cache, OracleId, 12);
        guide.Progression.Classes[0].AddArchetype(root);
        Check((bool)AccessTools.Method(T("SpiritGuideSkills0060"), "IsSpiritGuide").Invoke(null, new object[] { guide }),
            "C03 native ClassData.AddArchetype makes the unit a Spirit Guide");
        var guideMystery = Fact(guide, battleMystery);
        Check(SkillCount(guide, StatType.SkillPerception) == 0 && !Perception(guide).ClassSkill,
            "C03 Spirit Guide suppresses only the Mystery Perception contribution (hook skips Retain, count 0)");
        guide.Facts.Remove(guideMystery);
        Check(SkillCount(guide, StatType.SkillPerception) == 0,
            "C03 removing the suppressed Mystery does not Release (paired hook, no underflow)");

        // C03-3 Mystery-first then archetype (adapter path): converge to the same
        // suppressed state, then restore exactly once when the Class Skills
        // reconciliation feature turns off while the Mystery still lives.
        var late = Unit(cache, OracleId, 12);
        var lateMystery = Fact(late, battleMystery);
        Check(SkillCount(late, StatType.SkillPerception) == 1, "C03 Mystery-first starts at native count 1");
        late.Progression.Classes[0].AddArchetype(root);
        var lateAdapter = Fact(late, classSkills);
        Check(SkillCount(late, StatType.SkillPerception) == 0,
            "C03 adapter offsets the already-active Mystery once (both orders converge to count 0)");
        late.Facts.Remove(lateAdapter);
        Check(SkillCount(late, StatType.SkillPerception) == 1,
            "C03 removing the archetype Class Skills feature restores the living Mystery exactly once (count 1)");

        // C03-4 background contribution is preserved while the Mystery is suppressed.
        var bg = Unit(cache, OracleId, 12);
        bg.Progression.Classes[0].AddArchetype(root);
        Fact(bg, battleMystery);
        Check(SkillCount(bg, StatType.SkillPerception) == 0, "C03 background fixture starts suppressed (count 0)");
        var bgSourceBp = new BlueprintFeature { AssetGuid = BlueprintGuid.Parse(Guid.NewGuid().ToString("N")), name = "0060BackgroundSource", ComponentsArray = Array.Empty<BlueprintComponent>() };
        bgSourceBp.OnEnable(); cache.AddCachedBlueprint(bgSourceBp.AssetGuid, bgSourceBp);
        var bgFact = Fact(bg, bgSourceBp);
        Perception(bg).AddBackgroundSkillSource(bgFact);
        Check(Perception(bg).ClassSkill && SkillCount(bg, StatType.SkillPerception) == 0,
            "C03 background keeps Perception a class skill without reviving the suppressed Mystery count");
        Perception(bg).RemoveBackgroundSkillSource(bgFact);
        Check(!Perception(bg).ClassSkill && SkillCount(bg, StatType.SkillPerception) == 0,
            "C03 removing the background leaves the Spirit Guide without the extra skill (count 0)");

        // C03-5 repeated native fact lifecycle cycles never underflow or stack.
        var cycle = Unit(cache, OracleId, 12);
        cycle.Progression.Classes[0].AddArchetype(root);
        Fact(cycle, battleMystery);
        for (int i = 0; i < 3; i++) { cycle.Facts.TurnOff(); cycle.Facts.TurnOn(); }
        Check(SkillCount(cycle, StatType.SkillPerception) == 0,
            "C03 three native TurnOff/TurnOn cycles keep the suppressed count at 0 (no underflow, no stacking)");

        // ================= C06: Oracle-level / Charisma primary isolation =================
        var dual = Unit(cache, OracleId, 7);
        dual.Progression.Classes.Add(new ClassData(fighter) { Level = 5 });
        dual.Stats.Charisma.BaseValue = 18; // +4
        dual.Stats.Wisdom.BaseValue = 10;   // 0
        dual.Stats.Intelligence.BaseValue = 20; // +5
        Check(dual.Progression.GetClassLevel(oracle) == 7, "C06 fixture is Oracle 7 / Fighter 5 (total 12)");
        var battleBase = (BlueprintAbilityResource)cache.Load(Id("BattleTier0R10060"));
        Check(battleBase != null && battleBase.GetMaxAmount(dual.Descriptor) == 7,
            "C06 the real Battle base pool is 3 + Charisma(4) = 7, not 3 + Wisdom(0) or 3 + Intelligence(5)");
        var stardust = (BlueprintAbility)cache.Load(Id("HeavensTier0Feature0060Ability"));
        Check(stardust != null, "C06 the private Stardust base ability is published");
        var stardustRank = stardust.GetComponents<ContextRankConfig>().Single();
        var dualCtx = new MechanicsContext(dual, dual.Descriptor, stardust);
        Check(stardustRank.GetValue(dualCtx) == 3,
            "C06 the level-type rank reads 1 round per 2 Oracle levels (3 at Oracle 7), not the total character level 12");
        dual.Stats.Wisdom.BaseValue = 18; // raise Wisdom alone
        Check(battleBase.GetMaxAmount(dual.Descriptor) == 7 && stardustRank.GetValue(dualCtx) == 3,
            "C06 raising Wisdom to 18 changes neither the Charisma base pool nor the Oracle-level rank");
        dual.Stats.Wisdom.BaseValue = 10;

        var lifeChannelPool = (BlueprintAbilityResource)cache.Load(Id("LifeTier0R20060"));
        Check(lifeChannelPool != null, "C06 the Life channel pool resource is published");
        dual.Stats.Charisma.BaseValue = 16; // +3
        Check(lifeChannelPool.GetMaxAmount(dual.Descriptor) == 4,
            "C06 the Life channel pool is 1 + Charisma(3) = 4 (the C07 maximum before Extra Channel)");

        // ================= C10: the four Wisdom (hex_secondary_stat) branches =================
        var wis = Unit(cache, OracleId, 12);
        wis.Stats.Wisdom.BaseValue = 18; // +4
        wis.Stats.Charisma.BaseValue = 12; // +1
        var wisCtx = new MechanicsContext(wis, wis.Descriptor, (BlueprintAbility)cache.Load(Id("ConfusionCurseAbility0060")));
        Check(wis.Stats.Wisdom.Bonus == 4 && wis.Stats.Charisma.Bonus == 1, "C10 fixture Wisdom +4 / Charisma +1");

        CheckWisDuration(cache, "ConfusionCurseAbility0060", "C10 Confusion Curse", wis, 4);
        CheckHexWisdomRank(cache, "NatureHex0Feature0060", "C10 Entangling Curse", wis, 4, 1);
        wis.Stats.Wisdom.BaseValue = 8; // -1 -> clamped
        CheckWisDuration(cache, "ConfusionCurseAbility0060", "C10 Confusion Curse minimum", wis, 1);
        CheckHexWisdomRank(cache, "NatureHex0Feature0060", "C10 Entangling Curse minimum", wis, 1, 1);
        wis.Stats.Wisdom.BaseValue = 18;

        // Friend to Animals: three saves ride max(0, Wisdom), never negative.
        var friendRanks = HexBuffWisdomRanks(cache, "NatureHex2Feature0060");
        var friendDump = string.Join(" | ", HexBuffBlueprints(cache, "NatureHex2Feature0060").Select(b => b.name + ":" +
            string.Join(",", b.ComponentsArray.OfType<ContextRankConfig>().Select(r => Get(r, "m_BaseValueType") + "/" + Get(r, "m_Stat") + "/min=" + Get(r, "m_UseMin")))));
        var nativeFriendBuff = (BlueprintBuff)cache.Load(BlueprintGuid.Parse("05cdce24bc6c1b547a566faad6853579"));
        var nativeArea = (SimpleBlueprint)cache.Load(BlueprintGuid.Parse("c98a2fd5b994e6e4d9560964c7877a7f"));
        var nativeDump = nativeArea == null ? "area-null" : nativeArea.GetType().Name + "[" + string.Join(" ; ", ((BlueprintScriptableObject)nativeArea).ComponentsArray
            .Select(c => c == null ? "null" : c.GetType().Name + "{" + string.Join(",", c.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).Select(fd => fd.Name + "=" + (fd.GetValue(c) ?? "null"))) + "}")) + "]";
        Check(friendRanks.Count == 1, "C10 Friend to Animals carries exactly one Wisdom-driven rank on its buff (private=[" + friendDump + "] native=[" + nativeDump + "])");
        var friendRank = friendRanks[0];
        var friendCtx = new MechanicsContext(wis, wis.Descriptor, (BlueprintUnitFact)cache.Load(Id("NatureHex2Feature0060")));
        Check(friendRank.GetValue(friendCtx) == 4, "C10 Friend rank is Wisdom +4 at Wisdom 18");
        wis.Stats.Wisdom.BaseValue = 8;
        Check(friendRank.GetValue(friendCtx) == 0, "C10 Friend clamps to 0 at Wisdom 8, not a negative bonus");
        wis.Stats.Wisdom.BaseValue = 18;
        var friendBuffs = HexBuffBlueprints(cache, "NatureHex2Feature0060");
        Check(friendBuffs.SelectMany(b => b.ComponentsArray.OfType<AddContextStatBonus>())
                .Select(c => c.Stat.ToString()).OrderBy(s => s).SequenceEqual(new[] { "SaveFortitude", "SaveReflex", "SaveWill" }.OrderBy(s => s)),
            "C10 Friend adds its Wisdom rank to the three saving throws");

        // Starburn pool is 1 + Wisdom; Charisma changes leave the secondary branches alone.
        var starburn = (BlueprintAbilityResource)cache.Load(Id("StarburnResource0060"));
        Check(starburn != null && starburn.GetMaxAmount(wis.Descriptor) == 5,
            "C10 Starburn pool is 1 + Wisdom(4) = 5");
        wis.Stats.Charisma.BaseValue = 20; // +5, must not move any Wisdom branch
        Check(starburn.GetMaxAmount(wis.Descriptor) == 5 && friendRank.GetValue(friendCtx) == 4,
            "C10 raising Charisma does not change the Wisdom-driven Starburn pool or Friend rank");
        CheckWisDuration(cache, "ConfusionCurseAbility0060", "C10 Confusion Curse Charisma-independent", wis, 4);
        wis.Stats.Charisma.BaseValue = 12;

        // The donor's native Friend graph is untouched: the Wisdom swap happened
        // only on the private clone, never on the shared native buff.
        var friendDonor = (BlueprintFeature)cache.Load(BlueprintGuid.Parse("0d92799cecf414249a23ad82f7e9901a"));
        var donorBuffs = ReferencedBuffs(friendDonor);
        Check(donorBuffs.SelectMany(b => b.ComponentsArray.OfType<ContextRankConfig>())
                .All(r => (Get(r, "m_Stat")?.ToString() ?? "None") != "Wisdom" || (ContextRankBaseValueType)Get(r, "m_BaseValueType") != ContextRankBaseValueType.StatBonus),
            "C10 the native donor Friend buff still reads its original stat (no native pollution)");

        // Sparkling Aura: a resolved metal hit shocks for the Wisdom modifier;
        // miss / fake / non-metal deal nothing. Real published buff component on
        // a real unit with a controlled RuleAttackWithWeapon fixture.
        var sparkBp = (BlueprintBuff)cache.Load(Id("SparklingAuraBuff0060"));
        Check(sparkBp != null && sparkBp.ComponentsArray.Any(c => c != null && c.GetType() == T("SparklingAura0060")),
            "C10 Sparkling Aura buff carries the private Wisdom-reading hit component");
        var holder = Unit(cache, OracleId, 12);
        holder.Stats.Wisdom.BaseValue = 18;
        int wisBonus = holder.Stats.Wisdom.Bonus;
        var attacker = Unit(cache, OracleId, 12);
        var metal = Weapon0060(holder, WeaponCategory.Longsword, "0060-metal");
        var nonMetal = Weapon0060(holder, WeaponCategory.UnarmedStrike, "0060-nonmetal");
        Check(metal.Blueprint.Type.Category.HasSubCategory(WeaponSubCategory.Metal) && !nonMetal.Blueprint.Type.Category.HasSubCategory(WeaponSubCategory.Metal),
            "C10 fixture longsword is metal and unarmed strike is not (native HasSubCategory)");
        var triggerDamage = AccessTools.Method(typeof(RuleDealDamage), "OnTrigger", new[] { typeof(RulebookEventContext) });
        var capture = AccessTools.Method(typeof(SpiritGuide0060Tests), nameof(CaptureDamage0060));
        h.Patch(triggerDamage, prefix: new HarmonyMethod(capture));
        try
        {
            var spark = AddBuff(holder, sparkBp);
            damageCalls0060 = 0; capturedDamage0060 = null;
            Invoke0060(spark, T("SparklingAura0060"), "OnEventDidTrigger", Attack0060(attacker, holder, metal, true));
            Check(damageCalls0060 == 1 && capturedDamage0060 != null && ReferenceEquals(capturedDamage0060.Target, holder),
                "C10 a resolved metal hit shocks the glowing creature");
            var first = (EnergyDamage)Member0060(Member0060(capturedDamage0060, "DamageBundle"), "First");
            Check(first.EnergyType == DamageEnergyType.Electricity && first.Bonus == wisBonus && wisBonus == 4,
                "C10 the shock is electricity equal to the Wisdom modifier (+4 at Wisdom 18), not Charisma");
            damageCalls0060 = 0;
            Invoke0060(spark, T("SparklingAura0060"), "OnEventDidTrigger", Attack0060(attacker, holder, metal, false));
            Check(damageCalls0060 == 0, "C10 a metal weapon that misses deals no shock");
            damageCalls0060 = 0;
            Invoke0060(spark, T("SparklingAura0060"), "OnEventDidTrigger", Attack0060(attacker, holder, nonMetal, true));
            Check(damageCalls0060 == 0, "C10 a non-metal weapon that hits deals no shock");
            damageCalls0060 = 0;
            Invoke0060(spark, T("SparklingAura0060"), "OnEventDidTrigger", Attack0060(attacker, holder, metal, true, true));
            Check(damageCalls0060 == 0, "C10 a fake/preview metal hit deals no shock");
            damageCalls0060 = 0;
            Invoke0060(spark, T("SparklingAura0060"), "OnEventAboutToTrigger", Attack0060(attacker, holder, metal, true));
            Check(damageCalls0060 == 0, "C10 the pre-resolution callback deals no shock");
        }
        finally { h.Unpatch(triggerDamage, capture); }

        // ================= C11: Benefit of Wisdom, four skills -> Charisma =================
        var benefit = (BlueprintFeature)cache.Load(Id("LoreHex0Feature0060"));
        Check(benefit != null, "C11 Benefit of Wisdom feature is published");
        var replacements = benefit.GetComponents<ReplaceStatBaseAttribute>().ToArray();
        Check(replacements.Length == 4 &&
              replacements.Select(r => r.TargetStat.ToString()).OrderBy(s => s)
                  .SequenceEqual(new[] { "SkillKnowledgeArcana", "SkillKnowledgeWorld", "SkillLoreNature", "SkillLoreReligion" }.OrderBy(s => s)),
            "C11 exactly the four Knowledge/Lore skills are replaced, not the 0117 two-Lore set");
        Check(replacements.All(r => r.BaseAttributeReplacement == StatType.Charisma),
            "C11 the replacement base attribute is Charisma, not Intelligence");
        var scholar = Unit(cache, OracleId, 12);
        scholar.Stats.Wisdom.BaseValue = 18;   // +4
        scholar.Stats.Charisma.BaseValue = 18; // +4
        scholar.Stats.Intelligence.BaseValue = 12; // +1
        Fact(scholar, benefit);
        Check(((StatType)Member0060(scholar.Stats.SkillKnowledgeArcana, "m_ReplaceBaseAttributeType")) == StatType.Charisma,
            "C11 Knowledge (Arcana) really reads Charisma on the live unit");
        Check(((StatType)Member0060(scholar.Stats.SkillPersuasion, "m_ReplaceBaseAttributeType")) == StatType.Unknown,
            "C11 an untouched skill keeps its native base attribute");

        // ================= C12/C13: source-safe temporary knowledge =================
        var bookOwner = Unit(cache, OracleId, 7);
        var book = bookOwner.Descriptor.DemandSpellbook(oracle);
        for (int i = 0; i < 7; i++) book.AddLevelFromClass(oracle);
        var bonusBp = (BlueprintFeature)cache.Load(Id("BattleSpells0060"));
        var grants = bonusBp.GetComponents<AddSpellKnownTemporary>().ToArray();
        Check(grants.Length == 9, "C13 the Battle bonded-spell feature carries nine native temporary grants");
        Check(grants.Select(g => g.Level).SequenceEqual(Enumerable.Range(1, 9)),
            "C12 each grant is pinned to its circle, so spells appear as the Oracle book reaches that circle");
        var bonusSpells = grants.Select(g => g.Spell).ToArray();
        Check(bonusSpells.All(s => s != null), "C13 all nine bonded spell donors resolve");
        var oracleList = (BlueprintSpellList)cache.Load(BlueprintGuid.Parse(OracleSpellListId));
        var levelOne = oracleList.SpellsByLevel.First(l => l.SpellLevel == 1).Spells.OfType<BlueprintAbility>().Distinct().ToArray();
        var unrelated = levelOne.First(s => !bonusSpells.Contains(s));

        // spell[0] already has a permanent source; spell[1] has an independent
        // temporary source; spell[2] is 0060-only; unrelated is a permanent spell
        // the package never grants.
        book.AddKnown(1, bonusSpells[0], false);
        var independentBp = IndependentTempFeature(cache, oracle, bonusSpells[1]);
        var independentFact = Fact(bookOwner, independentBp);
        book.AddKnown(1, unrelated, false);
        Check(book.IsKnown(bonusSpells[0]) && !book.GetKnownSpell(bonusSpells[0]).IsTemporary,
            "C13 spell[0] starts as permanent knowledge");
        Check(book.IsKnown(bonusSpells[1]) && book.GetKnownSpell(bonusSpells[1]).IsTemporary,
            "C13 spell[1] starts as independent temporary knowledge");

        var bonusFact = Fact(bookOwner, bonusBp);
        Check(book.IsKnown(bonusSpells[2]) && book.GetKnownSpell(bonusSpells[2]).IsTemporary,
            "C13 granting 0060 adds its exclusive spell as native temporary knowledge");
        Check(book.GetKnownSpells(1).Count(a => a.Blueprint == bonusSpells[1]) == 1,
            "C13 the independent and 0060 sources share exactly one native spell[1] ability");
        Check(book.IsKnown(unrelated) && !book.GetKnownSpell(unrelated).IsTemporary,
            "C13 an unrelated permanent spell is present before revocation");

        bookOwner.Facts.Remove(bonusFact);
        Check(book.IsKnown(bonusSpells[0]) && !book.GetKnownSpell(bonusSpells[0]).IsTemporary,
            "C13 revoking 0060 preserves the permanent source (no global RemoveSpell)");
        Check(book.IsKnown(bonusSpells[1]) && book.GetKnownSpell(bonusSpells[1]).IsTemporary,
            "C13 revoking 0060 preserves the independent temporary source");
        Check(!book.IsKnown(bonusSpells[2]), "C13 revoking 0060 removes its exclusive temporary knowledge");
        Check(book.IsKnown(unrelated) && !book.GetKnownSpell(unrelated).IsTemporary,
            "C13 revoking 0060 leaves unrelated permanent knowledge untouched");

        // Repeat grant/revoke cycles must not drift or double-delete.
        for (int i = 0; i < 2; i++)
        {
            var again = Fact(bookOwner, bonusBp);
            Check(book.IsKnown(bonusSpells[2]), "C13 re-grant restores the exclusive temporary spell (cycle " + i + ")");
            bookOwner.Facts.Remove(again);
            Check(!book.IsKnown(bonusSpells[2]) && book.IsKnown(bonusSpells[0]) && book.IsKnown(bonusSpells[1]) && book.IsKnown(unrelated),
                "C13 re-revoke removes only the exclusive spell, permanent/independent/unrelated survive (cycle " + i + ")");
        }
        bookOwner.Facts.Remove(independentFact);
        Check(!book.IsKnown(bonusSpells[1]), "C13 removing the last independent source finally revokes spell[1]");
        Check(book.IsKnown(bonusSpells[0]) && book.IsKnown(unrelated), "C13 permanent knowledge survives every temporary revocation");

        // ================= D-F02: bonded knowledge follows the real Oracle book =================
        var young = Unit(cache, OracleId, 4);
        var youngBook = young.Descriptor.DemandSpellbook(oracle);
        for (int i = 0; i < 4; i++) youngBook.AddLevelFromClass(oracle);
        Check(youngBook.MaxSpellLevel == 2, "F2 fixture: an Oracle 4 book can cast up to circle 2 (actual " + youngBook.MaxSpellLevel + ")");
        var youngFact = Fact(young, bonusBp);
        var youngPart = young.Get<UnitPartTemporarySpellsKnown>();
        Check(youngPart != null, "F2 the native temporary-spell ledger exists on the live unit");
        Check(Enumerable.Range(1, 2).All(c => youngBook.IsKnown(bonusSpells[c - 1])),
            "F2 the bonded spell is granted for every circle the book can already cast");
        Check(Enumerable.Range(3, 7).All(c => !youngBook.IsKnown(bonusSpells[c - 1])),
            "F2 the nine-grant component no longer publishes circles 3-9 before the book reaches them");
        int ledgerOwned = OwnedLedger0060(youngPart);
        Check(ledgerOwned > 0, "F2 the grants are tracked in the native ledger, not in a second spell account (" + ledgerOwned + " owned entries)");

        // Rise: the level-up hook runs the coordinator on the real ledger.
        // The native Oracle table opens circle 4 at class level 8, not 7.
        young.Progression.Classes[0].Level = 8;
        for (int i = 0; i < 4; i++) youngBook.AddLevelFromClass(oracle);
        GainLevel0060(youngPart);
        Check(youngBook.MaxSpellLevel == 4, "F2 fixture: Oracle 8 reaches circle 4 (actual " + youngBook.MaxSpellLevel + ")");
        Check(Enumerable.Range(1, 4).All(c => youngBook.IsKnown(bonusSpells[c - 1])),
            "F2 the level-up event adds exactly the circles that just opened");
        Check(Enumerable.Range(5, 5).All(c => !youngBook.IsKnown(bonusSpells[c - 1])),
            "F2 circles 5-9 are still withheld at Oracle 8");

        // An independent temporary provider above the limit must survive both the
        // rise and the fall, and must never be revoked as if it were ours.
        var outsideSource = IndependentTempFeature(cache, oracle, bonusSpells[4]);
        var outsideFact = Fact(young, outsideSource);
        Check(youngBook.IsKnown(bonusSpells[4]) && youngBook.GetKnownSpell(bonusSpells[4]).IsTemporary,
            "F2 an independent temporary provider still supplies an above-limit circle");

        // Fall: the native API has no way down, so the book's class level is moved
        // directly (a retrain) and the same native level-up hook is re-run.
        young.Progression.Classes[0].Level = 5;
        Set0060(youngBook, "m_BaseLevelInternal", 5);
        GainLevel0060(youngPart);
        Check(youngBook.MaxSpellLevel == 2, "F2 fixture: the reduced book tops out at circle 2 (actual " + youngBook.MaxSpellLevel + ")");
        Check(!youngBook.IsKnown(bonusSpells[3]) && !youngBook.IsKnown(bonusSpells[2]),
            "F2 lowering the book withdraws the circle-3 and circle-4 bonded spells");
        Check(Enumerable.Range(1, 2).All(c => youngBook.IsKnown(bonusSpells[c - 1])),
            "F2 the circles that remain castable are untouched");
        Check(youngBook.IsKnown(bonusSpells[4]) && youngBook.GetKnownSpell(bonusSpells[4]).IsTemporary,
            "F2 the independent source keeps its own spell through the withdrawal");
        young.Facts.Remove(outsideFact);
        Check(!youngBook.IsKnown(bonusSpells[4]), "F2 removing the independent source withdraws only its own spell");

        // ---- D2-F02a. A locked circle whose exact AbilityData object is ALSO
        // held by another active temporary source in the same book, spell and
        // circle. The bond must unbind its own ledger entry but leave the shared
        // object alive; the object must disappear as soon as that last other
        // source is removed, with no extra refresh. The old independent provider
        // was fixed at circle 1, so it could never reach this branch. ----
        var sharedUnit = Unit(cache, OracleId, 8);
        var sharedBook = sharedUnit.Descriptor.DemandSpellbook(oracle);
        for (int i = 0; i < 8; i++) sharedBook.AddLevelFromClass(oracle);
        Check(sharedBook.MaxSpellLevel == 4,
            "F2a fixture: Oracle 8 opens circle 4 (actual " + sharedBook.MaxSpellLevel + ")");
        var sharedPartBefore = sharedUnit.Get<UnitPartTemporarySpellsKnown>();
        Check(sharedPartBefore == null, "F2a fixture: the ledger is created lazily by the first temporary grant");
        Fact(sharedUnit, bonusBp);
        var sharedPart = sharedUnit.Get<UnitPartTemporarySpellsKnown>();
        Check(sharedPart != null, "F2a the native ledger now exists on the live unit");
        var sharedAbility = sharedBook.GetKnownSpell(bonusSpells[3]);
        Check(sharedAbility != null && sharedAbility.IsTemporary,
            "F2a the bond publishes a temporary circle-4 spell at Oracle 8");
        var sharedOutside = IndependentTempFeature(cache, oracle, bonusSpells[3], 4);
        var sharedOutsideFact = Fact(sharedUnit, sharedOutside);
        var outsideEntry = LedgerEntries0060(sharedPart)
            .Single(e => ReferenceEquals(Member0060(e, "Source"), sharedOutsideFact));
        Check(EntrySpells0060(outsideEntry).Contains(sharedAbility),
            "F2a both entries hold the SAME AbilityData (same book, spell and circle)");
        Check(OwnedCircleSpells0060(sharedPart, 4) == 1,
            "F2a before the fall the bond still tracks its circle-4 object");

        sharedUnit.Progression.Classes[0].Level = 5;
        Set0060(sharedBook, "m_BaseLevelInternal", 5);
        GainLevel0060(sharedPart);
        Check(sharedBook.MaxSpellLevel == 2, "F2a fixture: the reduced book tops out at circle 2");
        Check(sharedBook.IsKnown(bonusSpells[3]),
            "F2a the locked circle stays known while the independent source still holds it");
        Check(OwnedCircleSpells0060(sharedPart, 4) == 0,
            "F2a the bond has unbound its own circle-4 ledger entry (actual " + OwnedCircleSpells0060(sharedPart, 4) + ")");
        Check(!sharedBook.IsKnown(bonusSpells[2]),
            "F2a the unshared circle-3 object is withdrawn normally");
        sharedUnit.Facts.Remove(sharedOutsideFact);
        Check(!sharedBook.IsKnown(bonusSpells[3]),
            "F2a removing the last independent source withdraws the shared spell immediately, without another refresh");

        // ---- D2-F02b. A REAL replacement of the Oracle ClassData-owned book.
        // The old book's own knowledge is migrated away, an independent old-book
        // source survives, the replacement gets only its legal circles and an
        // unrelated class book is untouched. Driven through the native level-up
        // hook, which is the production owner-level entry - not a reflected call
        // into the internal helper on an unowned Spellbook. ----
        var replacementBlueprint = (BlueprintSpellbook)cache.Load(BlueprintGuid.Parse(ReplacementSpellbookId));
        var otherBlueprint = (BlueprintSpellbook)cache.Load(BlueprintGuid.Parse(OtherClassSpellbookId));
        Check(replacementBlueprint != null && !ReferenceEquals(replacementBlueprint, oracle.Spellbook) &&
              replacementBlueprint.SpellsPerDay?.Levels?.Length > 0 && otherBlueprint?.SpellsPerDay?.Levels?.Length > 0,
            "F2b fixture: two distinct native spellbook blueprints with real spells-per-day tables are loaded");

        var staleIndependent = IndependentTempFeature(cache, oracle, bonusSpells[4], 5);
        Fact(young, staleIndependent);
        Check(youngBook.IsKnown(bonusSpells[4]) && youngBook.IsKnown(bonusSpells[0]) && youngBook.IsKnown(bonusSpells[1]),
            "F2b fixture: the old book holds both our bonded circles and an independent source");

        var unrelatedBook = new Spellbook(young.Descriptor, otherBlueprint);
        unrelatedBook.AddKnownTemporary(1, bonusSpells[0]);
        int unrelatedBefore = unrelatedBook.GetKnownSpells(1).Count;
        Spellbooks0060(young)[otherBlueprint] = unrelatedBook;

        var replacement = new Spellbook(young.Descriptor, replacementBlueprint);
        for (int i = 0; i < 5; i++) replacement.AddLevelFromClass(oracle);
        Spellbooks0060(young)[replacementBlueprint] = replacement;
        young.Progression.GetClassData(oracle).Spellbook = replacementBlueprint;
        Check(ReferenceEquals(young.Descriptor.DemandSpellbook(oracle), replacement),
            "F2b the native class data and spellbook registry now resolve to the replacement book");

        GainLevel0060(youngPart);
        Check(!youngBook.IsKnown(bonusSpells[0]) && !youngBook.IsKnown(bonusSpells[1]),
            "F2b the old book's own bonded knowledge is withdrawn on migration");
        Check(youngBook.IsKnown(bonusSpells[4]),
            "F2b the old book's independent source is preserved");
        Check(replacement.MaxSpellLevel == 2 && replacement.IsKnown(bonusSpells[0]) && replacement.IsKnown(bonusSpells[1]) &&
              !replacement.IsKnown(bonusSpells[2]),
            "F2b the current class-owned book receives exactly the legal bonded circles");
        Check(unrelatedBook.GetKnownSpells(1).Count == unrelatedBefore && unrelatedBook.IsKnown(bonusSpells[0]),
            "F2b an unrelated class book is untouched by the migration");

        // ================= C16/C17/C18: Lore Arcane Enlightenment =================
        Check(loreGreater.LevelEntries.Select(e => e.Level).SequenceEqual(new[] { 15, 17, 19 }),
            "C16 the three Arcane Enlightenment picks live at 15/17/19");
        var aeSelection = (BlueprintFeatureSelection)cache.Load(Id("LoreTier1Feature0060Selection0060"));
        Check(loreGreater.LevelEntries.All(e => e.Features.Count == 1 && e.Features[0] == aeSelection),
            "C16 every pick grants the same selection (no wandering 19th-level auxiliary)");
        Check(aeSelection.Ranks == 3, "C16 the selection allows exactly three ranks");

        var learnList = (BlueprintSpellList)cache.Load(Id("LoreTier1Feature0060List0060"));
        Check(learnList != null && learnList.SpellsByLevel.Length >= 10, "C17 the private wizard-minus-oracle spell list is published");
        var oracleSpellGuids = oracleList.SpellsByLevel.SelectMany(l => l.Spells.Where(s => s != null)).Select(s => s.AssetGuid).ToHashSet();
        Check(learnList.SpellsByLevel.SelectMany(l => l.Spells.Where(s => s != null)).All(s => !oracleSpellGuids.Contains(s.AssetGuid)),
            "C17 no Oracle base-list spell is selectable through Arcane Enlightenment");
        Check(learnList.SpellsByLevel.Sum(l => l.Spells.Count(s => s != null)) > 0,
            "C17 the filtered list still offers wizard spells");

        var circle5 = (BlueprintParametrizedFeature)cache.Load(Id("LoreTier1Feature00605" + "0060"));
        Check(circle5 != null && circle5.SpecificSpellLevel && circle5.SpellLevel == 5 && circle5.SpellLevelPenalty == 0,
            "C17 the circle feature pins a specific spell level");
        Check(circle5.SpellcasterClass == oracle && circle5.SpellList == learnList,
            "C17 the parametrized UI reads the private list against the Oracle spellbook");
        var statPrereqs = circle5.GetComponents<PrerequisiteStatValue>().ToArray();
        Check(statPrereqs.Any(p => p.Stat == StatType.Intelligence && p.Value == 15) &&
              statPrereqs.Any(p => p.Stat == StatType.Charisma && p.Value == 15),
            "C17 circle 5 requires Intelligence 15 AND Charisma 15 (10 + circle)");
        var spellLevelGate = circle5.GetComponent<PrerequisiteClassSpellLevel>();
        Check(spellLevelGate != null && spellLevelGate.RequiredSpellLevel == 5 && spellLevelGate.CharacterClass == oracle,
            "C17 circle 5 also requires Oracle 5th-circle spell access");
        Check(!circle5.ComponentsArray.Any(c => c is Kingmaker.Designers.Mechanics.Facts.LearnSpellParametrized),
            "C18 no permanent LearnSpellParametrized survives on the circle feature");

        // C18 runtime: one parametrized pick becomes exactly one temporary grant
        // that is revoked with the feature, without touching other knowledge.
        var seeker = Unit(cache, OracleId, 15);
        var seekerBook = seeker.Descriptor.DemandSpellbook(oracle);
        for (int i = 0; i < 15; i++) seekerBook.AddLevelFromClass(oracle);
        var circle1 = (BlueprintParametrizedFeature)cache.Load(Id("LoreTier1Feature00601" + "0060"));
        var learned = learnList.SpellsByLevel[1].Spells.First(s => s != null && !seekerBook.IsKnown(s));
        var other = learnList.SpellsByLevel[1].Spells.First(s => s != null && s != learned && !seekerBook.IsKnown(s));
        var circleFact = new Feature(circle1, seeker.Descriptor) { Param = new FeatureParam(learned) };
        seeker.Facts.Add(circleFact);
        Check(seekerBook.IsKnown(learned) && seekerBook.GetKnownSpell(learned).IsTemporary,
            "C18 the parametrized pick becomes temporary known spell, never permanent");
        Check(seekerBook.GetKnownSpells(1).Count(a => a.Blueprint == learned) == 1,
            "C18 exactly one spell entry is registered per selection");
        Check(!seekerBook.IsKnown(other), "C18 the unchosen spell is not learned");
        seeker.Facts.Remove(circleFact);
        Check(!seekerBook.IsKnown(learned), "C18 removing the selection removes the temporary spell");
        var secondFact = new Feature(circle1, seeker.Descriptor) { Param = new FeatureParam(other) };
        seeker.Facts.Add(secondFact);
        Check(seekerBook.IsKnown(other) && seekerBook.GetKnownSpell(other).IsTemporary && !seekerBook.IsKnown(learned),
            "C18 a later pick learns only its own spell; earlier revocations stay revoked");
        seeker.Facts.Remove(secondFact);
        Check(!seekerBook.IsKnown(other), "C18 the second selection is revoked with its source too");

        // ================= D-F04: Battle Master weapon growth =================
        const string SpecializationId = "31470b17e8446ae4ea0dacd6c5817d86";
        const string GreaterFocusId = "09c9e82965fb4334b984a1e9df3bd088";
        var battleMaster = cache.Load(Id("BattleHex0Feature0060")) as BlueprintFeatureSelection;
        Check(battleMaster != null && battleMaster.AllFeatures.Length == Enum.GetValues(typeof(WeaponCategory)).Length,
            "F4 Battle Master is a private weapon choice with one entry per native weapon category");
        var longswordEntry = battleMaster.AllFeatures.Single(f => f.name.EndsWith("FeatureLongsword", StringComparison.Ordinal));
        var longbowEntry = battleMaster.AllFeatures.Single(f => f.name.EndsWith("FeatureLongbow", StringComparison.Ordinal));
        var aoo = longswordEntry.GetComponent<AddStatBonus>();
        Check(aoo != null && aoo.Stat == StatType.AttackOfOpportunityCount && aoo.Value == 1,
            "F4 the weapon choice grants the extra attack of opportunity");
        var proficiencyGate = longswordEntry.GetComponent<PrerequisiteProficiency>();
        Check(proficiencyGate != null &&
              proficiencyGate.WeaponProficiencies.SequenceEqual(new[] { WeaponCategory.Longsword }) &&
              proficiencyGate.ArmorProficiencies.Length == 0,
            "F4 the choice requires proficiency in exactly the chosen weapon");
        var growth = longswordEntry.GetComponents<Kingmaker.Designers.Mechanics.Facts.AddFeatureOnClassLevel>().ToArray();
        Check(growth.Select(g => g.Level).OrderBy(l => l).SequenceEqual(new[] { 8, 16 }) && growth.All(g => g.Class == oracle),
            "F4 the choice grows at Oracle 8 and 16 only, never through the native Shaman progression");

        var martial = Unit(cache, OracleId, 7);
        Check(!CanSelect0060(martial, battleMaster, longswordEntry),
            "F4 an Oracle without martial proficiency cannot select the longsword entry (native prerequisite gate)");
        martial.Descriptor.Proficiencies.Add(WeaponCategory.Longsword);
        Check(CanSelect0060(martial, battleMaster, longswordEntry),
            "F4 the same Oracle can select it once proficient");
        var weaponFact = Fact(martial, longswordEntry);
        Check(martial.Stats.GetStat(StatType.AttackOfOpportunityCount).ModifiedValue == 2,
            "F4 granting the entry really raises the unit's attacks of opportunity from 1 to 2");
        Check(Parametrized0060(martial, SpecializationId).Length == 0 && Parametrized0060(martial, GreaterFocusId).Length == 0,
            "F4 nothing is granted before Oracle 8");
        ClassLevelUp0060(martial, weaponFact, 8);
        Check(Parametrized0060(martial, SpecializationId).SequenceEqual(new[] { WeaponCategory.Longsword }),
            "F4 Oracle 8 grants Weapon Specialization for the chosen weapon only");
        Check(Parametrized0060(martial, GreaterFocusId).Length == 0, "F4 Greater Weapon Focus is still absent at Oracle 8");
        ClassLevelUp0060(martial, weaponFact, 16);
        Check(Parametrized0060(martial, GreaterFocusId).SequenceEqual(new[] { WeaponCategory.Longsword }),
            "F4 Oracle 16 grants Greater Weapon Focus for the same weapon");
        Check(Parametrized0060(martial, SpecializationId).SequenceEqual(new[] { WeaponCategory.Longsword }),
            "F4 the 8th-level pick is not duplicated or moved to another weapon");

        // Removing our source must not delete a feat the character owns separately.
        var owned = Unit(cache, OracleId, 16);
        owned.Descriptor.Proficiencies.Add(WeaponCategory.Longbow);
        GrantParametrized0060(owned, SpecializationId, WeaponCategory.Longsword);
        var bowFact = Fact(owned, longbowEntry);
        Check(Parametrized0060(owned, SpecializationId).OrderBy(c => c).SequenceEqual(new[] { WeaponCategory.Longbow, WeaponCategory.Longsword }.OrderBy(c => c)),
            "F4 the hex grants its own weapon pick alongside the independently owned specialization");
        owned.Facts.Remove(bowFact);
        Check(Parametrized0060(owned, SpecializationId).SequenceEqual(new[] { WeaponCategory.Longsword }) &&
              Parametrized0060(owned, GreaterFocusId).Length == 0,
            "F4 revoking the hex removes only the feats it granted, never the independent one");

        // ================= D-F05: the Heaven's Leap target ledger =================
        var leapFeature = (BlueprintFeature)cache.Load(Id("HeavensHex2Feature0060"));
        var markBp = (BlueprintBuff)cache.Load(Id("HeavensHex2Feature0060Mark"));
        var leapAbility = (BlueprintAbility)cache.Load(Id("HeavensHex2Feature0060Ability"));
        Check(leapFeature != null && markBp != null && leapAbility != null,
            "F5 the Heaven's Leap feature, mark buff and move ability are published");
        var leapFacts = leapFeature.GetComponents<AddFacts>()
            .SelectMany(c => (BlueprintUnitFactReference[])Get(c, "m_Facts")).Select(r => r.Get()).ToArray();
        Check(leapFacts.Length == 2 && leapFacts.All(f => f is BlueprintAbility) && leapFacts.All(f => !(f is BlueprintBuff)),
            "F5 Heaven's Leap grants only its two abilities, never a permanent self mark");
        var leapComp = leapAbility.ComponentsArray.First(c => c.GetType() == T("HeavensLeap0060"));
        var marked = AccessTools.Method(T("HeavensLeap0060"), "Marked", new[] { typeof(UnitEntityData) });
        object Target(UnitEntityData caster) => marked.Invoke(leapComp, new object[] { caster });

        var casterA = Unit(cache, OracleId, 12);
        var casterB = Unit(cache, OracleId, 12);
        var allyX = Unit(cache, OracleId, 12);
        var allyY = Unit(cache, OracleId, 12);
        var party = Party0060();
        party.Add(allyX); party.Add(allyY);
        try
        {
            var selfMark = AddBuff(casterA, markBp, casterA);
            Check(ReferenceEquals(Target(casterA), casterA),
                "F5 a caster who selected herself has herself as the current target");
            var markX = AddBuff(allyX, markBp, casterA);
            Check(ReferenceEquals(Target(casterA), allyX), "F5 selecting X replaces the caster's own mark");
            Check(!selfMark.IsActive, "F5 the previous self mark is cleared by the new selection");
            var markY = AddBuff(allyY, markBp, casterB);
            Check(ReferenceEquals(Target(casterA), allyX) && ReferenceEquals(Target(casterB), allyY),
                "F5 two spirit guides keep their own marks: B's selection never clears A's");
            Check(markX.IsActive && markY.IsActive, "F5 both marks stay alive after the second caster selects");
            var stranger = Unit(cache, OracleId, 12);
            Check(Target(stranger) == null, "F5 a caster with no selection has no target (no silent self fallback)");
            markX.Remove();
            Check(Target(casterA) == null && ReferenceEquals(Target(casterB), allyY),
                "F5 removing A's source clears only A's target");
        }
        finally { party.Remove(allyX); party.Remove(allyY); }

        // ================= D-F06: ward/mist charges survive a reload =================
        var wardBp = (BlueprintBuff)cache.Load(Id("StoneHex3Feature0060Buff"));
        var mistBp = (BlueprintBuff)cache.Load(Id("WavesHex1Feature0060Buff"));
        Check(wardBp != null && wardBp.ComponentsArray.Any(c => c.GetType() == T("WardCharges0060")),
            "F6 the Ward of Stone buff carries the private charge component");
        Check(mistBp != null && mistBp.ComponentsArray.Any(c => c.GetType() == T("MistCharges0060")),
            "F6 Mist's Shroud uses the same serialized charge state, not a second table");
        var warded = Unit(cache, OracleId, 16);
        var ward = AddBuff(warded, wardBp, warded);
        var wardRuntime = Runtime0060(ward, T("WardCharges0060"));
        Check(wardRuntime != null, "F6 the charge component has a live runtime that owns per-fact data");
        var dataField = AccessTools.Field(wardRuntime.GetType(), "m_Data");
        var wardAttacker = Unit(cache, OracleId, 12);
        var wardWeapon = Weapon0060(wardAttacker, WeaponCategory.Longsword, "0060-ward");
        Invoke0060(ward, T("WardCharges0060"), "OnEventDidTrigger", Attack0060(wardAttacker, warded, wardWeapon, true));
        Invoke0060(ward, T("WardCharges0060"), "OnEventDidTrigger", Attack0060(wardAttacker, warded, wardWeapon, true));
        var live = Member0060(wardRuntime, "MaybeData");
        Check(Left0060(live) == 1 && Ready0060(live), "F6 two absorbed hits leave one charge at Oracle 16");
        var json = JsonConvert.SerializeObject(live);
        dataField.SetValue(wardRuntime, null);
        Check(Member0060(wardRuntime, "MaybeData") == null, "F6 a restored fact starts with no in-memory state object");
        dataField.SetValue(wardRuntime, JsonConvert.DeserializeObject(json, T("ChargeState0060")));
        var restored = Member0060(wardRuntime, "MaybeData");
        Check(Left0060(restored) == 1 && Ready0060(restored),
            "F6 the serialized state restores the remaining charge instead of refilling to the full amount");
        Invoke0060(ward, T("WardCharges0060"), "OnEventDidTrigger", Attack0060(wardAttacker, warded, wardWeapon, true));
        Check(!ward.IsActive || Left0060(Member0060(wardRuntime, "MaybeData") ?? restored) == 0,
            "F6 the last charge still removes the ward after the reload");

        Console.WriteLine("0060 RESULT: " + checks + " directed native assertions across C01-C06/C10-C13/C16-C18/C23, D-F01..F06 and the D2-F01/F02a/F02b/F03 counterexamples in the isolated managed host.");
        Console.WriteLine("0060 DIAGNOSTIC: party-list refresh skipped " + partyRefreshLosses +
            " times (Player.UpdateCharacterLists needs an area; the unit model itself is unaffected)");
        Console.WriteLine("0060 DIAGNOSTIC: AddFacts.UpdateFacts NRE count in this host = " + addFactsUpdateLosses +
            " (the Life channel carrier row is now present in the extended fixture)");
        Console.WriteLine("0060 NOT RUN: player game and UMM; full save/load and level-up UI transport (the parametrized pick is driven by a real Feature fact, " +
            "not the level-up screen; the ward reload is a serializer round-trip of the component's own per-fact data, not SaveManager); " +
            "the Life channel heal/harm rule settlement, rest events, retrain controllers and animal-only Friend targeting; " +
            "the Battle variant payment chain is NOT proven - only UnitAbilityResourceCollection.Spend on the private pool was exercised, " +
            "not a variant AbilityData's own CanUse/Spend; " +
            "the Life base power's AddFacts activation reaches native UpdateFacts; any NRE logged there is counted above, classified by exact " +
            "exception/method in the result JSON and NOT exempted from review. The granted channel ability is asserted from the published private " +
            "graph and from AddFacts itself, while the private channel pool is asserted as a really held per-unit resource; " +
            "the four external bonded-spell roots (ExpandedContent WallOfFire/HypnoticPattern/IncendiaryCloud, CharacterOptionsPlus Frostbite) are " +
            "contract-shaped provider inputs built by this host at the frozen GUIDs - the real third-party plugins are not installed here, so their " +
            "real activation is NOT proven and in a real environment the atomic gate keeps deferring publication until those providers exist; " +
            "the internal bonded roots did NOT all run through real production registration: " + reauthored + " were re-authored from the reviewed " +
            "archive rows and " + hostStaged + " were host-staged at their frozen GUIDs after PsychicExpansion/TimeStop01438 registration hit a " +
            "SecurityException (ECall) in this host; they are asserted as published spell roots, not as a production-registration PASS. " +
            "F2 withdraws bonded circles by moving the native book level directly because the game has no RemoveLevelFromClass; the book replacement " +
            "uses real ClassData.Spellbook and the native spellbook registry, not a full save/retrain controller. " +
            "Field/rank/callback checks are directed native evidence, not a whole-package behaviour PASS.");
        return checks;
    }

    private static Action<string, bool, Exception> ReportSink(List<Tuple<string, bool, Exception>> reports) =>
        (name, ok, error) => reports.Add(Tuple.Create(name, ok, error));

    private static object[] PoolSnapshot(BlueprintsCache cache, string guid)
    {
        var pool = cache.Load(BlueprintGuid.Parse(guid));
        return new[] { Get(pool, "m_Features"), Get(pool, "m_AllFeatures") };
    }

    private static bool PoolMatches(BlueprintsCache cache, string guid, object[] snapshot) =>
        ReferenceEquals(Get(cache.Load(BlueprintGuid.Parse(guid)), "m_Features"), snapshot[0]) &&
        ReferenceEquals(Get(cache.Load(BlueprintGuid.Parse(guid)), "m_AllFeatures"), snapshot[1]);

    private static string Blocker(BlueprintsCache cache) =>
        (string)AccessTools.Method(T("SpiritGuideSpirits0060"), "MissingProvider").Invoke(null, new object[] { cache });

    // The shared whole-library availability policy is deliberately type-only.
    // Calling the REAL production method here proves the D2 shell negatives are
    // not vacuous: the shell passes Available, so only this package's bounded
    // root-structure check can be responsible for the rejection.
    private static bool Available0060(BlueprintsCache cache, ExternalRoot root)
    {
        var linkType = T("PsychicSpellLink");
        var ctor = linkType.GetConstructors(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).Single();
        var link = ctor.Invoke(new object[] { root.Guid, root.Name, root.Provider, root.School, true, -1, -1, -1 });
        var args = new object[] { cache, link, null };
        return (bool)AccessTools.Method(T("PsychicSpellIntegration"), "Available").Invoke(null, args);
    }

    // Use the production bounded root check, not a second copy of the old
    // "component type exists" whitelist: D2-F03 showed that a mirror of a
    // type-only policy lets an empty-action shell through.
    private static bool Executable0060(BlueprintAbility ability) =>
        (bool)AccessTools.Method(T("SpiritGuideSpirits0060"), "ExecutableRoot")
            .Invoke(null, new object[] { ability });

    // Re-author a curated fixture spell from the same reviewed archive row the
    // historical suites read, instead of accepting the fixture's minimal shell.
    private static bool RepublishFromArchive0060(BlueprintsCache cache, string guid)
    {
        var rows = PsychicExpansionTests.NativeRows;
        if (rows == null) return false;
        if (!rows.Any(r => (string)r["blueprint"]["AssetId"] == guid)) return false;
        var spell = PsychicExpansionTests.NativeCase<BlueprintAbility>(guid);
        spell.OnEnable();
        if (!Executable0060(spell)) return false;
        cache.RemoveCachedBlueprint(spell.AssetGuid);
        cache.AddCachedBlueprint(spell.AssetGuid, spell);
        return true;
    }

    // A bounded reference closure of one private root: the ability/buff graph a
    // player can actually reach from this feature. Class, progression and feat
    // blueprints are terminal - following PrerequisiteNoClassLevel out of a
    // ContextRankConfig would drag the whole library in and prove nothing.
    // D2-F01: terminal does NOT mean invisible. Every reference is recorded, and
    // resolved plain BlueprintUnitFact targets plus unresolved references are
    // kept as an audit list; the old audit copied production's graph whitelist
    // and silently ignored exactly the Life channel fact it was meant to catch.
    private sealed class Closure0060
    {
        internal readonly Dictionary<BlueprintGuid, BlueprintScriptableObject> Nodes = new Dictionary<BlueprintGuid, BlueprintScriptableObject>();
        internal readonly List<object> Objects = new List<object>();
        internal readonly List<BlueprintReferenceBase> References = new List<BlueprintReferenceBase>();
        internal readonly Dictionary<BlueprintGuid, BlueprintScriptableObject> PlainFacts =
            new Dictionary<BlueprintGuid, BlueprintScriptableObject>();
        internal readonly List<BlueprintGuid> Unresolved = new List<BlueprintGuid>();
    }

    // Identity, not value equality: native WeakResourceLink overrides GetHashCode
    // and dereferences a not-yet-loaded prefab, so a default HashSet aborts the
    // walk. Production uses the same identity policy.
    private sealed class Identity0060 : IEqualityComparer<object>
    {
        internal static readonly Identity0060 Instance = new Identity0060();
        bool IEqualityComparer<object>.Equals(object x, object y) => ReferenceEquals(x, y);
        int IEqualityComparer<object>.GetHashCode(object obj) => System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(obj);
    }

    // Exactly the production IsGraphNode policy in SpiritGuideGraph0060.
    private static bool IsGraphNode0060(BlueprintScriptableObject bp) =>
        bp is BlueprintAbility || bp is Kingmaker.UnitLogic.ActivatableAbilities.BlueprintActivatableAbility ||
        bp is BlueprintBuff || bp is BlueprintAbilityAreaEffect;

    private static Closure0060 Graph0060(BlueprintScriptableObject root)
    {
        var closure = new Closure0060();
        var seen = new HashSet<object>(Identity0060.Instance);
        closure.Nodes[root.AssetGuid] = root;
        closure.Objects.Add(root);
        // The root's own serialized fields are reachable too (m_Buff/m_Parent),
        // not only its components.
        for (var t = root.GetType(); t != null && t != typeof(object) &&
             t != typeof(BlueprintScriptableObject) && t != typeof(SimpleBlueprint); t = t.BaseType)
            foreach (var field in t.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
            {
                if (field.IsStatic || field.Name == "Components" || field.Name == "m_Components") continue;
                Scan0060(field.GetValue(root), closure, seen);
            }
        foreach (var component in root.ComponentsArray ?? Array.Empty<BlueprintComponent>())
            Scan0060(component, closure, seen);
        return closure;
    }

    private static void Visit0060(BlueprintScriptableObject bp, Closure0060 closure, HashSet<object> seen)
    {
        if (bp == null || !IsGraphNode0060(bp) || closure.Nodes.ContainsKey(bp.AssetGuid)) return;
        closure.Nodes[bp.AssetGuid] = bp;
        closure.Objects.Add(bp);
        // ActivatableAbility.m_Buff and Ability.m_Parent are serialized on the
        // blueprint, not inside ComponentsArray. Scanning components only made the
        // aura chain invisible to this closure, which is how D-F01 survived review.
        for (var t = bp.GetType(); t != null && t != typeof(object) &&
             t != typeof(BlueprintScriptableObject) && t != typeof(SimpleBlueprint); t = t.BaseType)
            foreach (var field in t.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
            {
                if (field.IsStatic || field.Name == "Components" || field.Name == "m_Components") continue;
                Scan0060(field.GetValue(bp), closure, seen);
            }
        foreach (var component in bp.ComponentsArray ?? Array.Empty<BlueprintComponent>())
            Scan0060(component, closure, seen);
    }

    private static void Scan0060(object value, Closure0060 closure, HashSet<object> seen)
    {
        if (value == null || value is string || value is Type || value is Delegate) return;
        if (value.GetType().IsValueType || value is UnityEngine.Object) return;
        if (value is BlueprintReferenceBase reference)
        {
            closure.References.Add(reference);
            var target = reference.GetBlueprint() as BlueprintScriptableObject;
            if (target == null)
            {
                if (reference.Guid != BlueprintGuid.Empty) closure.Unresolved.Add(reference.Guid);
                return;
            }
            if (!IsGraphNode0060(target))
            {
                closure.PlainFacts[target.AssetGuid] = target;
                // D2-F01: a plain BlueprintFeature can hide another AddFacts one
                // hop down (Life base -> native ChannelEnergyFeature -> the
                // native resource-carrying UnitFact). Follow ONLY its AddFacts
                // reference arrays, not every component: prerequisites and
                // other serialized fields would pull in the whole class library.
                // The identity set makes each plain fact a one-time visit.
                if (target is BlueprintUnitFact plain && seen.Add(plain))
                    foreach (var component in (plain.ComponentsArray ?? Array.Empty<BlueprintComponent>())
                             .Where(c => c != null && c.GetType().Name == "AddFacts"))
                        Scan0060(Get(component, "m_Facts"), closure, seen);
                return;
            }
            Visit0060(target, closure, seen);
            return;
        }
        if (!seen.Add(value)) return;
        closure.Objects.Add(value);
        if (value is System.Collections.IEnumerable items)
        {
            foreach (var item in items) Scan0060(item, closure, seen);
            return;
        }
        for (var t = value.GetType(); t != null && t != typeof(object) &&
             t != typeof(BlueprintComponent) && t != typeof(Element); t = t.BaseType)
            foreach (var field in t.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
                if (!field.IsStatic) Scan0060(field.GetValue(value), closure, seen);
    }

    // Builds the provider input the gate is allowed to accept: the frozen
    // identity and name, a real spell root, the frozen school, a real damaging
    // effect (never an empty action array) and a reciprocal publication from the
    // provider's own spell list. effect=false / publish=false produce the bounded
    // negatives only; they are never used to prove the positive path.
    // D2-F03: shell=emptyActions / emptyVariants keeps the full identity and the
    // reciprocal publication but installs an effect component with an EMPTY
    // executed path, which is the exact shell the old gate accepted.
    private static void StageExternal(BlueprintsCache cache, ExternalRoot root, AbilityType type,
        SpellSchool school, bool effect, bool publish, string name = null, string shell = null)
    {
        var id = BlueprintGuid.Parse(root.Guid);
        cache.RemoveCachedBlueprint(id);
        var components = new List<BlueprintComponent> { new SpellComponent { School = school } };
        if (shell == "emptyActions")
            components.Add(new AbilityEffectRunAction { Actions = new ActionList { Actions = Array.Empty<GameAction>() } });
        else if (shell == "emptyVariants")
        {
            var variants = new AbilityVariants();
            Set(variants, "m_Variants", Array.Empty<BlueprintAbilityReference>());
            components.Add(variants);
        }
        else if (effect)
            components.Add(new AbilityEffectRunAction
            {
                Actions = new ActionList
                {
                    Actions = new GameAction[]
                    {
                        new ContextActionDealDamage
                        {
                            DamageType = new DamageTypeDescription { Type = DamageType.Direct },
                            Value = new ContextDiceValue { DiceType = DiceType.D6, DiceCountValue = 1, BonusValue = 0 }
                        }
                    }
                }
            });
        var spell = new BlueprintAbility
        {
            AssetGuid = id,
            name = name ?? root.Name,
            Type = type,
            ComponentsArray = components.ToArray()
        };
        if (publish)
        {
            var list = ProviderList(cache, root);
            var membership = new SpellListComponent { SpellLevel = root.Circle };
            Set(membership, "m_SpellList", BlueprintReferenceBase.CreateTyped<BlueprintSpellListReference>(list));
            spell.ComponentsArray = components.Concat(new BlueprintComponent[] { membership }).ToArray();
            Set(list.SpellsByLevel[root.Circle], "m_Spells",
                new List<BlueprintAbilityReference> { BlueprintReferenceBase.CreateTyped<BlueprintAbilityReference>(spell) });
        }
        spell.OnEnable();
        cache.AddCachedBlueprint(id, spell);
    }

    // The provider's own spell list: the reciprocal publication the Available
    // contract requires, and never this project's private Psychic list.
    private static BlueprintSpellList ProviderList(BlueprintsCache cache, ExternalRoot root)
    {
        var id = Id("ProviderList0060" + root.Provider.Replace(" ", ""));
        var list = cache.Load(id) as BlueprintSpellList;
        if (list != null) return list;
        list = new BlueprintSpellList
        {
            AssetGuid = id,
            name = "0060-" + root.Provider + "ProviderList",
            SpellsByLevel = Enumerable.Range(0, 11).Select(i => new SpellLevelList(i)).ToArray()
        };
        cache.AddCachedBlueprint(id, list);
        return list;
    }

    // D-F02 helpers: the native ledger stays the single account. These read and
    // drive the real native objects; nothing here keeps a parallel spell list.
    // D2-F01: GetResourceAmount returns 0 both when a pool is absent and when
    // it is present but empty. Read the native per-unit resource dictionary so
    // "not granted" is a real holding assertion, not a formula comparison.
    private static bool ResourceHeld0060(UnitEntityData u, BlueprintScriptableObject pool)
    {
        var resources = Get(u.Resources, "m_Resources") as System.Collections.IDictionary;
        return resources != null && resources.Contains(pool);
    }

    private static bool OwnedEntry0060(object entry)
    {
        var source = (EntityFact)Member0060(entry, "Source");
        var name = source?.Blueprint?.name;
        return name != null && name.StartsWith("CotWWrathSpiritGuide0060", StringComparison.Ordinal) &&
               name.EndsWith("Spells0060", StringComparison.Ordinal);
    }

    private static object[] LedgerEntries0060(UnitPartTemporarySpellsKnown part) =>
        ((System.Collections.IEnumerable)AccessTools
            .Field(typeof(UnitPartTemporarySpellsKnown), "m_Entries").GetValue(part))
        .Cast<object>().ToArray();

    private static List<AbilityData> EntrySpells0060(object entry) =>
        (List<AbilityData>)Member0060(entry, "Spells");

    // How many spell objects the bond itself still tracks at one circle. The
    // D2-F02a counterexample needs this: the spell may remain KNOWN through
    // another source while our own entry must be empty.
    private static int OwnedCircleSpells0060(UnitPartTemporarySpellsKnown part, int circle) =>
        LedgerEntries0060(part)
            .Where(e => !(bool)Member0060(e, "Removed") && OwnedEntry0060(e))
            .Where(e => ((AddSpellKnownTemporary)Member0060(e, "Component")).Level == circle)
            .Sum(e => EntrySpells0060(e).Count);

    private static int OwnedLedger0060(UnitPartTemporarySpellsKnown part)
    {
        var entries = LedgerEntries0060(part);
        int owned = 0;
        foreach (var entry in entries)
        {
            if (OwnedEntry0060(entry)) owned++;
        }
        return owned;
    }

    private static void GainLevel0060(UnitPartTemporarySpellsKnown part) =>
        AccessTools.Method(typeof(UnitPartTemporarySpellsKnown), "HandleUnitGainLevel").Invoke(part, null);

    private static System.Collections.IDictionary Spellbooks0060(UnitEntityData u) =>
        (System.Collections.IDictionary)AccessTools.Field(typeof(UnitDescriptor), "m_Spellbooks")
            .GetValue(u.Descriptor);

    // D-F04 helpers: read the real parametrized facts the AddParametrizedFeatures
    // component produced, and grant one the way the native feat would.
    private static WeaponCategory[] Parametrized0060(UnitEntityData u, string guid)
    {
        var id = BlueprintGuid.Parse(guid);
        return u.Progression.Features.Enumerable
            .Where(f => f.Blueprint.AssetGuid == id && f.Param != null && f.Param.WeaponCategory.HasValue)
            .Select(f => f.Param.WeaponCategory.Value).Distinct().ToArray();
    }

    private static void GrantParametrized0060(UnitEntityData u, string guid, WeaponCategory category)
    {
        var bp = ResourcesLibrary.TryGetBlueprint<BlueprintFeature>(guid);
        if (bp == null) throw new Exception("0060 CHECK FAILED: native parametrized feat " + guid + " is unavailable");
        u.Facts.Add(new Feature(bp, u.Descriptor) { Param = new FeatureParam(category) });
    }

    private static bool CanSelect0060(UnitEntityData u, BlueprintFeatureSelection pool, BlueprintFeature candidate) =>
        (bool)AccessTools.Method(typeof(EnhancementLifecycle01430Tests), "CanSelect")
            .Invoke(null, new object[] { u, pool, candidate });

    // The real level-up path for a fact's own components: the same native
    // IOwnerGainLevelHandler callback the game dispatches, driven with the
    // component's own event context instead of toggling the whole fact.
    private static void ClassLevelUp0060(UnitEntityData u, Feature fact, int level)
    {
        u.Progression.Classes[0].Level = level;
        foreach (var runtime in (System.Collections.IEnumerable)Member0060(fact, "Components"))
        {
            var source = Member0060(runtime, "SourceBlueprintComponent");
            var method = AccessTools.Method(source.GetType(), "HandleUnitGainLevel", Type.EmptyTypes);
            if (method == null) continue;
            using ((IDisposable)Method0060(runtime.GetType(), "RequestEventContext", Type.EmptyTypes).Invoke(runtime, null))
                method.Invoke(source, null);
        }
    }

    // D-F05 helper: the host's real party list, the same one Candidates reads.
    private static IList<UnitEntityData> Party0060()
    {
        var player = Kingmaker.Game.Instance?.Player;
        if (player == null) throw new Exception("0060 CHECK FAILED: the host has no Player fixture");
        Set(player, "m_CharacterListsValid", true);
        var party = Get(player, "m_Party") as IList<UnitEntityData>;
        if (party == null) throw new Exception("0060 CHECK FAILED: the host has no native party list");
        return party;
    }

    // D-F06 helpers: the runtime component that owns the serialized per-fact data.
    private static object Runtime0060(Buff buff, Type componentType)
    {
        foreach (var runtime in (System.Collections.IEnumerable)Member0060(buff, "Components"))
            if (ReferenceEquals(Member0060(runtime, "SourceBlueprintComponent").GetType(), componentType)) return runtime;
        return null;
    }

    private static int Left0060(object state) => state == null ? -1 : (int)Get(state, "Left");
    private static bool Ready0060(object state) => state != null && (bool)Get(state, "Ready");

    private static void DeepWalk(object value, HashSet<object> seen, List<ContextActionApplyBuff> found)
    {
        if (value == null || value is string || value is Type || value is Delegate ||
            value.GetType().IsValueType || value is SimpleBlueprint ||
            value is Kingmaker.Blueprints.BlueprintReferenceBase || value is UnityEngine.Object) return;
        if (!seen.Add(value)) return;
        if (value is ContextActionApplyBuff apply) found.Add(apply);
        if (value is System.Collections.IEnumerable items)
        {
            foreach (var item in items) DeepWalk(item, seen, found);
            return;
        }
        for (var t = value.GetType(); t != null && t != typeof(object) &&
             t != typeof(Kingmaker.Blueprints.BlueprintComponent) && t != typeof(Kingmaker.ElementsSystem.Element); t = t.BaseType)
            foreach (var f in t.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
                if (!f.IsStatic) DeepWalk(f.GetValue(value), seen, found);
    }

    private static void CheckWisDuration(BlueprintsCache cache, string token, string label, UnitEntityData caster, int expected)
    {
        var ability = (BlueprintAbility)cache.Load(Id(token));
        Check(ability != null, label + " ability is published");
        var rank = ability.GetComponents<ContextRankConfig>().Single();
        Check(Get(rank, "m_BaseValueType").ToString() == "StatBonus" && Get(rank, "m_Stat").ToString() == "Wisdom" &&
              (bool)Get(rank, "m_UseMin"),
            label + " rank reads the Wisdom secondary stat with a minimum clamp");
        var ctx = new MechanicsContext(caster, caster.Descriptor, ability);
        int value = rank.GetValue(ctx);
        Check(value == expected, label + " native Wisdom rank is " + expected + ", actual " + value);
        var apply = DurationApply(ability);
        var bonus = apply.DurationValue.BonusValue;
        Check(bonus.ValueType == ContextValueType.Rank && bonus.ValueRank == AbilityRankType.Default &&
              apply.DurationValue.Rate == DurationRate.Rounds && apply.DurationValue.DiceType == DiceType.Zero,
            label + " duration is the default Wisdom rank in rounds, not a constant");
        var ranks = new int[32];
        ranks[(int)AbilityRankType.Default] = value;
        Set(ctx, "m_Ranks", ranks);
        int rounds = apply.DurationValue.Calculate(ctx).Value;
        Check(rounds == expected, label + " calculated duration is " + expected + " rounds, actual " + rounds);
    }

    // Donor-clone hexes keep their own component shapes, so the Entangling probe
    // locates the single Wisdom rank on the cloned ability instead of assuming
    // the private builder's exact layout.
    private static void CheckHexWisdomRank(BlueprintsCache cache, string hexFeatureToken, string label, UnitEntityData caster, int expected, int minimum)
    {
        var feature = (BlueprintFeature)cache.Load(Id(hexFeatureToken));
        Check(feature != null, label + " hex feature is published");
        var ability = HexAbilities(cache, hexFeatureToken).Single();
        var ranks = ability.GetComponents<ContextRankConfig>()
            .Where(r => Get(r, "m_BaseValueType")?.ToString() == "StatBonus" && Get(r, "m_Stat")?.ToString() == "Wisdom").ToArray();
        Check(ranks.Length == 1, label + " carries exactly one Wisdom StatBonus rank after the secondary swap");
        var rank = ranks[0];
        Check((bool)Get(rank, "m_UseMin") && (int)Get(rank, "m_Min") == minimum,
            label + " clamps its Wisdom rank to " + minimum);
        var ctx = new MechanicsContext(caster, caster.Descriptor, ability);
        Check(rank.GetValue(ctx) == expected, label + " native Wisdom rank is " + expected + ", actual " + rank.GetValue(ctx));
    }

    private static IEnumerable<BlueprintAbility> HexAbilities(BlueprintsCache cache, string hexFeatureToken)
    {
        var feature = (BlueprintFeature)cache.Load(Id(hexFeatureToken));
        foreach (var component in feature.ComponentsArray)
        {
            if (component == null || component.GetType().Name != "AddFacts") continue;
            var refs = (BlueprintUnitFactReference[])Get(component, "m_Facts") ?? Array.Empty<BlueprintUnitFactReference>();
            foreach (var reference in refs)
                if (reference?.Get() is BlueprintAbility ability) yield return ability;
        }
    }

    private static List<ContextRankConfig> HexBuffWisdomRanks(BlueprintsCache cache, string hexFeatureToken) =>
        HexBuffBlueprints(cache, hexFeatureToken)
            .SelectMany(b => b.ComponentsArray.OfType<ContextRankConfig>())
            .Where(r => Get(r, "m_BaseValueType")?.ToString() == "StatBonus" && Get(r, "m_Stat")?.ToString() == "Wisdom")
            .ToList();

    private static List<BlueprintBuff> HexBuffBlueprints(BlueprintsCache cache, string hexFeatureToken)
    {
        var found = new List<BlueprintBuff>();
        var feature = (BlueprintFeature)cache.Load(Id(hexFeatureToken));
        foreach (var component in feature.ComponentsArray)
        {
            if (component == null || component.GetType().Name != "AddFacts") continue;
            var refs = (BlueprintUnitFactReference[])Get(component, "m_Facts") ?? Array.Empty<BlueprintUnitFactReference>();
            foreach (var reference in refs)
            {
                if (reference?.Get() is BlueprintBuff direct && !found.Contains(direct)) found.Add(direct);
                if (!(reference?.Get() is BlueprintAbility ability)) continue;
                var applies = new List<ContextActionApplyBuff>();
                foreach (var run in ability.ComponentsArray.OfType<AbilityEffectRunAction>())
                    WalkActions(run.Actions?.Actions, applies);
                foreach (var apply in applies)
                {
                    var buff = (Get(apply, "m_Buff") as BlueprintBuffReference)?.Get();
                    if (buff != null && !found.Contains(buff)) found.Add(buff);
                }
            }
        }
        // Aura carriers: the granted buff's AddAreaEffect chain reaches the area
        // effect and the buff it applies (either AbilityAreaEffectBuff or an
        // apply action inside the area's action lists).
        foreach (var carrier in found.ToArray())
        {
            foreach (var addArea in carrier.ComponentsArray.OfType<Kingmaker.UnitLogic.Buffs.Components.AddAreaEffect>())
            {
                var area = addArea.AreaEffect;
                if (area == null) continue;
                foreach (var areaBuff in area.ComponentsArray.OfType<Kingmaker.UnitLogic.Abilities.Components.AreaEffects.AbilityAreaEffectBuff>())
                {
                    var buff = areaBuff.Buff;
                    if (buff != null && !found.Contains(buff)) found.Add(buff);
                }
                var applies = new List<ContextActionApplyBuff>();
                foreach (var run in area.ComponentsArray.OfType<Kingmaker.UnitLogic.Abilities.Components.AreaEffects.AbilityAreaEffectRunAction>())
                    WalkActions(run.UnitEnter?.Actions, applies);
                foreach (var apply in applies)
                {
                    var buff = (Get(apply, "m_Buff") as BlueprintBuffReference)?.Get();
                    if (buff != null && !found.Contains(buff)) found.Add(buff);
                }
            }
        }
        return found;
    }

    private static IEnumerable<BlueprintBuff> ReferencedBuffs(BlueprintFeature feature)
    {
        var found = new List<BlueprintBuff>();
        foreach (var component in feature.ComponentsArray)
        {
            if (component == null || component.GetType().Name != "AddFacts") continue;
            var refs = (BlueprintUnitFactReference[])Get(component, "m_Facts") ?? Array.Empty<BlueprintUnitFactReference>();
            foreach (var reference in refs)
            {
                if (!(reference?.Get() is BlueprintAbility ability)) continue;
                var applies = new List<ContextActionApplyBuff>();
                foreach (var run in ability.ComponentsArray.OfType<AbilityEffectRunAction>())
                    WalkActions(run.Actions?.Actions, applies);
                foreach (var apply in applies)
                {
                    var buff = (Get(apply, "m_Buff") as BlueprintBuffReference)?.Get();
                    if (buff != null && !found.Contains(buff)) found.Add(buff);
                }
            }
        }
        return found;
    }

    private static ContextActionApplyBuff DurationApply(BlueprintAbility ability)
    {
        var found = new List<ContextActionApplyBuff>();
        foreach (var run in ability.ComponentsArray.OfType<AbilityEffectRunAction>())
            WalkActions(run.Actions?.Actions, found);
        // The 24-hour per-target day-lock mark also rides the action list (at
        // Days); the hex duration is the single Rounds-rate apply.
        var rounds = found.Where(a => a.DurationValue.Rate == DurationRate.Rounds).ToArray();
        if (rounds.Length != 1) throw new Exception("0060 CHECK FAILED: expected one Rounds buff duration on " + ability.name + ", found " + rounds.Length);
        return rounds[0];
    }
    private static void WalkActions(GameAction[] actions, List<ContextActionApplyBuff> found)
    {
        if (actions == null) return;
        foreach (var action in actions)
        {
            if (action is ContextActionApplyBuff apply) found.Add(apply);
            var saved = action as ContextActionConditionalSaved;
            if (saved == null) continue;
            WalkActions(saved.Failed?.Actions, found);
            WalkActions(saved.Succeed?.Actions, found);
        }
    }

    private static BlueprintFeature IndependentTempFeature(BlueprintsCache cache, BlueprintCharacterClass cls,
        BlueprintAbility spell, int level = 1)
    {
        var grant = new AddSpellKnownTemporary { name = "0060IndependentGrant", Level = level, OnlySpontaneous = true };
        Set(grant, "m_CharacterClass", BlueprintReferenceBase.CreateTyped<BlueprintCharacterClassReference>(cls));
        Set(grant, "m_Spell", BlueprintReferenceBase.CreateTyped<BlueprintAbilityReference>(spell));
        var bp = new BlueprintFeature { AssetGuid = BlueprintGuid.Parse(Guid.NewGuid().ToString("N")), name = "0060IndependentTempSource", ComponentsArray = new BlueprintComponent[] { grant } };
        bp.OnEnable(); cache.AddCachedBlueprint(bp.AssetGuid, bp); return bp;
    }

    private static int damageCalls0060;
    private static RuleDealDamage capturedDamage0060;
    private static bool CaptureDamage0060(RuleDealDamage __instance) { damageCalls0060++; capturedDamage0060 = __instance; return false; }
    private static TEmpty Empty0060<TEmpty>() where TEmpty : class =>
        (TEmpty)System.Runtime.Serialization.FormatterServices.GetUninitializedObject(typeof(TEmpty));
    private static void Set0060(object o, string n, object v) =>
        (AccessTools.Field(o.GetType(), n) ?? AccessTools.Field(o.GetType(), "<" + n + ">k__BackingField")).SetValue(o, v);
    private static RuleAttackWithWeapon Attack0060(UnitEntityData attacker, UnitEntityData target, ItemEntityWeapon weapon, bool hit, bool fake = false)
    {
        var attack = Empty0060<RuleAttackWithWeapon>();
        Set0060(attack, "Initiator", attacker); Set0060(attack, "Target", target); Set0060(attack, "Weapon", weapon); Set0060(attack, "AttacksCount", 1);
        var roll = Empty0060<RuleAttackRoll>();
        Set0060(roll, "Initiator", attacker); Set0060(roll, "Target", target);
        Set0060(roll, "Result", hit ? AttackResult.Hit : AttackResult.Miss); Set0060(roll, "IsFake", fake);
        Set0060(attack, "AttackRoll", roll);
        return attack;
    }
    private static ItemEntityWeapon Weapon0060(UnitEntityData u, WeaponCategory category, string id) =>
        (ItemEntityWeapon)AccessTools.Method(typeof(WildShapeFeatTests), "Weapon").Invoke(null, new object[] { u, category, false, PhysicalDamageForm.Slashing, id });
    private static object Member0060(object o, string name)
    {
        for (var t = o.GetType(); t != null; t = t.BaseType)
        {
            var p = t.GetProperty(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
            if (p != null) return p.GetValue(o, null);
            var f = t.GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
            if (f != null) return f.GetValue(o);
        }
        throw new Exception("0060 CHECK FAILED: member " + name + " not found on " + o.GetType().Name);
    }
    private static MethodInfo Method0060(Type type, string name, Type[] parameters)
    {
        for (var t = type; t != null; t = t.BaseType)
        {
            var m = t.GetMethod(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly, null, parameters, null);
            if (m != null) return m;
        }
        throw new Exception("0060 CHECK FAILED: method " + name + " not found on " + type.Name);
    }
    private static void Invoke0060(Buff buff, Type compType, string method, RuleAttackWithWeapon attack)
    {
        object runtime = null;
        foreach (var r in (System.Collections.IEnumerable)Member0060(buff, "Components"))
            if (Member0060(r, "SourceBlueprintComponent").GetType() == compType) { runtime = r; break; }
        if (runtime == null) throw new Exception("0060 CHECK FAILED: runtime component " + compType.Name + " not found on buff");
        var comp = Member0060(runtime, "SourceBlueprintComponent");
        using ((IDisposable)Method0060(runtime.GetType(), "RequestEventContext", Type.EmptyTypes).Invoke(runtime, null))
            Method0060(compType, method, new[] { typeof(RuleAttackWithWeapon) }).Invoke(comp, new object[] { attack });
    }
}

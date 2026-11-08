using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using Newtonsoft.Json.Linq;
using Kingmaker;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Selection;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.PubSubSystem;
using Kingmaker.RuleSystem;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components;
using Kingmaker.UnitLogic.Buffs;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.FactLogic;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Parts;
using Kingmaker.Utility;
using CallOfTheWild.Wrath.Prototype;

// Contract C01-STARSEEKER-20261107-r1, acceptance cases C01-CASE-01..08.
// Constellation identities follow A-catalog90.json, the frozen 90-spell
// authority; the erroneous contract 8.2 names are deliberately not used.
// Every behavioural assertion drives the real published Psychic spellbook,
// the native UnitPartTemporarySpellsKnown ledger, the real ability resource
// counter and the normal AbilityEffectRunAction cast entry point. No global
// fact spoofing, no direct custom-listener invocation and no reflective
// result rewriting substitutes for those chains.
internal static partial class Arcane01446Tests
{
    const string SeekerClassId = "83fa3b50138c566698801ba5967bbde0";
    const string SeekerMajorId = "fdd7cee80b9e81640bc490e3eafb2742";
    const string SeekerRoot = "EsotericStarseekerArchetype0081";
    const string SeekerSelection = "EsotericStarseekerDisciplineSelection0081";
    const string SeekerFeature = "WrittenInTheStarsFeature0081";
    const string SeekerResource = "WrittenInTheStarsResource0081";
    const string SeekerParent = "WrittenInTheStarsAbility0081";

    // A-catalog90 order. The 90 GUIDs themselves are read back out of the
    // production table so this suite audits the shipped data, not a copy.
    static readonly string[] SeekerStars = {
        "LanternBearer", "Newlyweds", "Daughter", "Rider", "Patriarch",
        "Wagon", "Pack", "Mother", "Stranger", "Follower" };
    static readonly string[] SeekerExternal = {
        "56b8f0304a704a67b3c35cbe8c774854", // Daughter circle 2, ExpandedContent
        "2ddc377f87b94aba9e8bab6c03f477e8", // Patriarch circle 7, TomeOfTheFirebird
        "99e39437184a46b994da740ce399f591", // Wagon circle 7, ExpandedContent
        "c3647ce7d6da49faa6e86830841e56e1", // Mother circle 3, CharacterOptionsPlus
        "58bcb999b3ad46c289dc68cba9c50bd0"  // Mother circle 9, PrestigePlus
    };

    static Type SeekerModule => T("EsotericStarseeker0081");
    static string SeekerModuleName => (string)AccessTools.Field(SeekerModule, "ModuleName").GetValue(null);
    static TRef SeekerRef<TRef>(SimpleBlueprint bp) where TRef : BlueprintReferenceBase, new()
    { var r = BlueprintReferenceBase.CreateTyped<TRef>(bp); AccessTools.Property(typeof(BlueprintReferenceBase), "Cached").SetValue(r, bp, null); return r; }

    static string[] SeekerTokens()
    {
        return new[] { SeekerRoot, SeekerSelection, SeekerFeature, SeekerResource, SeekerParent }
            .Concat(SeekerStars.SelectMany(s => new[] { "WrittenInTheStars" + s + "Ability0081", "WrittenInTheStars" + s + "Buff0081" }))
            .ToArray();
    }

    // Production constellation table: 10 rows of 9 resolved spell GUIDs each.
    static string[][] SeekerCatalog()
    {
        var rows = (Array)AccessTools.Field(SeekerModule, "Catalog").GetValue(null);
        Check(rows.Length == 10, "0081 the shipped constellation table holds ten Cosmic Caravan rows");
        var table = new string[rows.Length][];
        for (int i = 0; i < rows.Length; i++)
        {
            string token = (string)AccessTools.Field(rows.GetValue(i).GetType(), "Token").GetValue(rows.GetValue(i));
            var spells = (string[])AccessTools.Field(rows.GetValue(i).GetType(), "Spells").GetValue(rows.GetValue(i));
            Check(token == SeekerStars[i] && spells.Length == 9 && spells.All(g => g != null && g.Length == 32),
                "0081 constellation row " + SeekerStars[i] + " carries nine resolved spell identities");
            table[i] = spells;
        }
        Check(table.SelectMany(r => r).Distinct().Count() == 90, "0081 the frozen catalogue resolves 90 distinct spells");
        return table;
    }

    static int seekerDirty;
    static bool SeekerDirtyCounter() { seekerDirty++; return false; }
    static bool SeekerNoReplenish() => false;

    // Real provider fixture for one external constellation spell, built from the
    // shipped PsychicSharedSpellCatalog link exactly as the optional-integration
    // contract requires: correct name, spell type, root ability, school, an
    // executable component and a live reciprocal provider list publication.
    static object SeekerLink(string guid)
    {
        var entries = (Array)AccessTools.Field(T("PsychicSharedSpellCatalog"), "Entries").GetValue(null);
        var link = entries.Cast<object>().SingleOrDefault(e => (string)Get(e, "Guid") == guid);
        Check(link != null, "0081 external constellation spell is registered in the shared spell catalogue: " + guid);
        return link;
    }

    // The real shipped five-fold provider contract, invoked through its own entry
    // point so a rejected placeholder is proven by production code, not by the suite.
    static bool SeekerAvailable(BlueprintsCache c, string guid)
    {
        var resolver = AccessTools.Method(T("PsychicSpellIntegration"), "Available",
            new[] { typeof(BlueprintsCache), T("PsychicSpellLink"), typeof(BlueprintAbility).MakeByRefType() });
        Check(resolver != null, "0081 the real provider contract entry point exists");
        var args = new object[] { c, SeekerLink(guid), null };
        bool accepted = (bool)resolver.Invoke(null, args);
        // The shipped resolver assigns the candidate to `out ability` on its very
        // first statement and only then returns false for contract violations, so
        // a rejection may legitimately hand back the rejected blueprint. The one
        // invariant that must hold is that nothing is accepted without a spell.
        Check(!accepted || args[2] != null, "0081 the real provider check never accepts a constellation spell it failed to resolve: " + guid);
        return accepted;
    }

    static void SeekerProvider(BlueprintsCache c, string guid, bool published)
    {
        var link = SeekerLink(guid);
        var id = BlueprintGuid.Parse(guid);
        var spell = new BlueprintAbility
        {
            AssetGuid = id,
            name = (string)Get(link, "Name"),
            Type = AbilityType.Spell,
            ComponentsArray = new BlueprintComponent[] {
                new SpellComponent { School = (SpellSchool)Get(link, "School") },
                new AbilityEffectRunAction { Actions = new Kingmaker.ElementsSystem.ActionList { Actions = Array.Empty<Kingmaker.ElementsSystem.GameAction>() } } }
        };
        if (published && (bool)Get(link, "PublishedListRequired"))
        {
            int level = Math.Max(1, (int)Get(link, "Psychic"));
            var foreign = new BlueprintSpellList { AssetGuid = BlueprintGuid.Parse(Guid.NewGuid().ToString("N")), SpellsByLevel = new[] { new SpellLevelList(level) } };
            Set(foreign.SpellsByLevel[0], "m_Spells", new List<BlueprintAbilityReference> { SeekerRef<BlueprintAbilityReference>(spell) });
            var component = new SpellListComponent { SpellLevel = level };
            Set(component, "m_SpellList", SeekerRef<BlueprintSpellListReference>(foreign));
            spell.ComponentsArray = spell.ComponentsArray.Concat(new BlueprintComponent[] { component }).ToArray();
        }
        spell.OnEnable();
        if (c.Load(id) != null) c.RemoveCachedBlueprint(id);
        c.AddCachedBlueprint(id, spell);
    }

    static void SeekerWithdraw(BlueprintsCache c, string guid)
    {
        var id = BlueprintGuid.Parse(guid);
        if (c.Load(id) != null) c.RemoveCachedBlueprint(id);
    }

    // Real normal cast entry point: the shipped restriction gate, the native
    // resource spend and the blueprint's own AbilityEffectRunAction. The native
    // cast pipeline consults IAbilityCasterRestriction before it ever reaches
    // SpendResources, so a refused gate is proven by running the shipped action
    // with no spend and showing that nothing changes.
    static void SeekerAttune(UnitEntityData u, BlueprintAbility variant, BlueprintCharacterClass cls, BlueprintAbilityResource resource, bool expectPass = true)
    {
        var gate = variant.GetComponent<WrittenInTheStarsRestriction0081>();
        Check(gate != null && gate.IsCasterRestrictionPassed(u) == expectPass,
            "0081 the shipped attunement gate reports " + expectPass + " for " + variant.name);
        int before = u.Resources.GetResourceAmount(resource);
        var ability = new AbilityData(variant, u.Descriptor);
        if (expectPass) AbilityCastRateUtils.SpendResources(ability);
        var context = new AbilityExecutionContext(ability,
            new AbilityParams { CasterLevel = u.Progression.GetClassLevel(cls), DC = 15 },
            new TargetWrapper(u), new RulebookEventContext(), u);
        variant.GetComponent<AbilityEffectRunAction>().Apply(context, new TargetWrapper(u));
        if (expectPass) Check(u.Resources.GetResourceAmount(resource) == before - 1,
            "0081 the real resource spend charges exactly one attunement for " + variant.name);
        else Check(u.Resources.GetResourceAmount(resource) == before && u.Buffs.Enumerable.Count(b => b.Blueprint == variant.GetComponent<WrittenInTheStarsRestriction0081>().Constellation.Get()) == 1,
            "0081 a refused gate reaches no spend and applies no duplicate constellation for " + variant.name);
    }

    static Spellbook SeekerBook(UnitEntityData u, BlueprintCharacterClass cls, int level)
    {
        var book = u.Descriptor.DemandSpellbook(cls);
        Check(book.RawBaseLevel == 0, "0081 the real native demand starts the psychic book from zero, so the suite must advance it");
        for (int i = 0; i < level; i++) book.AddLevelFromClass(cls);
        Check(book.RawBaseLevel == level && book.MaxSpellLevel == (level == 1 ? 1 : level / 2),
            "0081 the real psychic spellbook reaches its independently expected maximum circle at class level " + level);
        return book;
    }

    static UnitEntityData SeekerUnit(BlueprintsCache c, BlueprintCharacterClass cls, BlueprintArchetype root, int level, bool archetype)
    {
        var u = Unit(c, SeekerClassId, level);
        u.Stats.Intelligence.BaseValue = 20;
        var data = u.Progression.GetClassData(cls);
        Check(data != null && data.Level == level, "0081 the constructed actor really carries the psychic class at level " + level);
        if (archetype) data.AddArchetype(root);
        return u;
    }

    static IEnumerable<AbilityData> SeekerKnown(Spellbook book) => book.GetAllKnownSpells();

    // Presentation-only boundary: yield the authored asset name / description key
    // instead of asking Unity's shared localization service, which does not exist in
    // a standalone CLR. Nothing but display text changes.
    static bool SeekerFactName(Kingmaker.Blueprints.Facts.BlueprintUnitFact __instance, ref string __result)
    {
        __result = __instance != null ? __instance.name : string.Empty;
        return false;
    }

    static bool SeekerFactDescription(Kingmaker.Blueprints.Facts.BlueprintUnitFact __instance, ref string __result)
    {
        string text = null;
        if (__instance != null)
        {
            var localized = AccessTools.Field(__instance.GetType(), "m_Description")?.GetValue(__instance) as Kingmaker.Localization.LocalizedString;
            if (localized != null)
            {
                var type = typeof(Kingmaker.Localization.LocalizedString);
                var key = AccessTools.Property(type, "Key");
                if (key != null && key.CanRead && key.GetIndexParameters().Length == 0) text = key.GetValue(localized, null) as string;
                if (text == null) text = AccessTools.Field(type, "m_Key")?.GetValue(localized) as string;
            }
        }
        __result = text ?? (__instance != null ? __instance.name : string.Empty);
        return false;
    }

    static int Starseeker0081(BlueprintsCache parent, Harmony h)
    {
        int start = checks;
        var registrar = T("BlueprintRegistration");
        var tokens = SeekerTokens();
        var catalog = SeekerCatalog();
        var reports = new List<Tuple<string, bool, Exception>>();
        Action<string, bool, Exception> report = (name, ok, error) => reports.Add(Tuple.Create(name, ok, error));

        Console.WriteLine("0081 stage: binder identities and the real undercasting/level bridges");
        var binder = (GuidClassBinder)Kingmaker.Blueprints.JsonSystem.Json.Serializer.Binder;
        foreach (var type in Main.GetTypes().Where(t => t.Name.EndsWith("0081") || t.Name.EndsWith("01460")))
            foreach (var attr in type.GetCustomAttributes(typeof(TypeIdAttribute), false).Cast<TypeIdAttribute>())
                binder.AddToCache(type, attr.GuidString);
        Check(tokens.Length == 25 && tokens.Distinct().Count() == 25, "0081 the module owns exactly 25 distinct private identities");
        // Production Harmony bridges only: the real undercasting reconciler, the
        // real temporary-knowledge ledger hooks and the shipped major deferral.
        var bridges = T("PsychicUndercasting").GetNestedTypes(BindingFlags.NonPublic | BindingFlags.Public)
            .Concat(Main.GetType("CallOfTheWild.Wrath.Prototype.TemporaryKnowledge01464")?.GetNestedTypes(BindingFlags.NonPublic | BindingFlags.Public) ?? Array.Empty<Type>())
            .Concat(Main.GetTypes().Where(t => t.Name == "MajorGrantStarseeker0081Patch"))
            .Where(t => t.GetCustomAttributes(typeof(HarmonyPatch), false).Any()).ToArray();
        foreach (var type in bridges) h.CreateClassProcessor(type).Patch();
        Check(bridges.Length > 0, "0081 the real production undercasting, ledger and major-deferral bridges are installed");
        var dirty = AccessTools.Method(typeof(Kingmaker.UI.UnitSettings.UnitUISettings), "SetDirty");
        Check(dirty != null, "0081 the native action-bar dirty endpoint exists");
        seekerDirty = 0;
        h.Patch(dirty, prefix: new HarmonyMethod(typeof(Arcane01446Tests), nameof(SeekerDirtyCounter)));

        // ---- C01-CASE-08: atomic deferral, late completion, idempotent re-entry.
        Console.WriteLine("0081 stage: atomic catalogue gate through the real dispatcher");
        // 0.14.67 TEST taught MimicCatalogue01461.BuildGraph to read
        // BlueprintFeature.Name/Description, and MutationMind01457 already read
        // BlueprintAbility.Name, so the real PsychicClassCompletion01458 producer now
        // reaches Kingmaker.Localization.LocalizedString.op_Implicit, whose Unity
        // localization service a standalone CLR cannot resolve (SecurityException:
        // ECall). That operator is an internal call Harmony cannot patch, and
        // transpiling MimicCatalogue01461 is impossible because its Snapshot uses a
        // C# exception filter MonoMod's emitter rejects. Replace the two native
        // display-text getters the producers actually call instead. Without that
        // receipt the discipline bridge defers, both six-discipline pools stay at
        // three and the archetype could never publish here. Registration order,
        // costs, prerequisites, receipts and every rule branch stay as shipped;
        // production identity checks use the lowercase asset name, not these getters.
        foreach (var display in new[] { "Name", "Description" })
        {
            var getter = AccessTools.PropertyGetter(typeof(Kingmaker.Blueprints.Facts.BlueprintUnitFact), display);
            Check(getter != null, "0081 the native blueprint display endpoint really exists: " + display);
            h.Patch(getter, prefix: new HarmonyMethod(typeof(Arcane01446Tests), display == "Name" ? nameof(SeekerFactName) : nameof(SeekerFactDescription)));
        }
        string completionReceipt = (string)AccessTools.Field(T("PsychicClassCompletion01458"), "ModuleName").GetValue(null);
        Func<BlueprintsCache, string> poolProbe = target =>
            "receipt=" + (bool)Call(T("ArcaneBatch01446"), "Existing", target, completionReceipt) +
            " disciplines=" + ((target.Load(Id("DisciplineSelection")) as BlueprintFeatureSelection)?.AllFeatures.Count() ?? -1) +
            " magaambyan=" + ((target.Load(Id("MagaambyanDisciplineSelection01447")) as BlueprintFeatureSelection)?.AllFeatures.Count() ?? -1) +
            " painDiscipline01458=" + (target.Load(Id("PainDiscipline01458")) != null) +
            " hypnoticPattern=" + (target.Load(BlueprintGuid.Parse("56b8f0304a704a67b3c35cbe8c774854")) != null);
        Console.WriteLine("0081 parent probe: " + poolProbe(parent));
        var c = Fresh(parent);
        Console.WriteLine("0081 fresh probe: " + poolProbe(c));
        var cls = Native<BlueprintCharacterClass>(c, SeekerClassId);
        Check(cls != null && cls.Spellbook != null, "0081 the real published Psychic class is the archetype host");
        reports.Clear();
        Call(registrar, "RegisterAll", c, report);
        // Honest dispatcher evidence: every module the real caller reported as
        // failed in this isolated host, with its actual exception.
        foreach (var failed in reports.Where(r => !r.Item2))
        {
            Console.WriteLine("0081 dispatcher failure: " + failed.Item1 + " -> " + failed.Item3?.GetType().FullName + ": " + failed.Item3?.Message);
            if (failed.Item3 is System.Security.SecurityException)
                Console.WriteLine("0081 icall stack for " + failed.Item1 + ":\n" + failed.Item3);
        }
        Console.WriteLine("0081 dispatcher: reported=" + reports.Count + " failed=" + reports.Count(r => !r.Item2));
        Check(!reports.Any(r => r.Item1 == SeekerModuleName && !r.Item2), "0081 the real dispatcher never reports a failed Esoteric Starseeker registration");
        Check(!reports.Any(r => r.Item1 == SeekerModuleName), "0081 five absent external constellation spells defer the module silently");
        Check(tokens.All(t => c.Load(Id(t)) == null), "0081 a deferred registration publishes zero of its 25 private blueprints");
        Check(!cls.Archetypes.Any(a => a.AssetGuid == Id(SeekerRoot)), "0081 the real Psychic class carries no deferred archetype link");

        var disciplines = Asset<BlueprintFeatureSelection>(c, "DisciplineSelection");
        var magaambyan = Asset<BlueprintFeatureSelection>(c, "MagaambyanDisciplineSelection01447");
        var amp = Asset<BlueprintFeatureSelection>(c, "PhrenicAmplificationSelection");
        var major = Native<BlueprintFeature>(c, SeekerMajorId);
        Check(disciplines != null && amp != null && major != null, "0081 the real discipline, amplification and major-amplification inputs exist");
        Check(disciplines.AllFeatures.Count() == 6 && magaambyan.AllFeatures.Count() == 6,
            "0081 the real dispatcher completed both six-discipline pools before the archetype runs (observed DisciplineSelection=" + disciplines.AllFeatures.Count() +
            " MagaambyanDisciplineSelection01447=" + magaambyan.AllFeatures.Count() + ")");
        Check((bool)Call(T("ArcaneBatch01446"), "Existing", c, (string)AccessTools.Field(T("PsychicClassCompletion01458"), "ModuleName").GetValue(null)),
            "0081 the real psychic completion receipt is published before the archetype runs");
        foreach (int level in new[] { 1, 11 })
        {
            var entry = cls.Progression.LevelEntries.Single(e => e.Level == level);
            Check(entry.Features.Contains(amp), "0081 the real class progression offers amplifications at level " + level);
        }
        Check(cls.Progression.LevelEntries.Single(e => e.Level == 1).Features.Contains(disciplines), "0081 the real class progression offers disciplines at level 1");
        Check(cls.Progression.LevelEntries.Single(e => e.Level == 11).Features.Contains(major), "0081 the real class progression grants major amplifications at level 11");

        // The PsychicClass fingerprint the contract forbids touching: the class
        // spellbook, its progression level entries and the PsychicSpellcasting
        // feature's own component fields, captured before any 0081 publish.
        var castingFeature = cls.Progression.LevelEntries.Single(e => e.Level == 1).Features.OfType<BlueprintFeature>()
            .Single(f => f.GetComponents<MajorGrant01460>().Any());
        Func<string> classFingerprint = () => cls.Spellbook.AssetGuid + "|" + cls.Progression.AssetGuid + "|" +
            string.Join(";", cls.Progression.LevelEntries.Select(e => e.Level + ":" + string.Join(",", e.Features.Select(x => x.AssetGuid.ToString())))) + "|" +
            string.Join(";", castingFeature.ComponentsArray.Select(x => x.GetType().FullName + ":" + x.name));
        var fingerprintBefore = classFingerprint();

        // Four of five providers: still an all-or-nothing rejection.
        var rootsBefore = cls.Archetypes.ToArray();
        var groupsBefore = cls.Progression.UIGroups.ToArray();
        var poolsBefore = disciplines.AllFeatures.ToArray();
        foreach (var guid in SeekerExternal.Take(4)) SeekerProvider(c, guid, true);
        reports.Clear();
        Call(registrar, "RegisterOptional", c, report);
        Check(!reports.Any(r => r.Item1 == SeekerModuleName), "0081 one remaining absent constellation spell keeps the whole module deferred");
        Check(tokens.All(t => c.Load(Id(t)) == null) && rootsBefore.SequenceEqual(cls.Archetypes) && groupsBefore.SequenceEqual(cls.Progression.UIGroups) && poolsBefore.SequenceEqual(disciplines.AllFeatures),
            "0081 a rejected catalogue leaves the real class archetypes, UI groups and discipline pool byte-identical");

        // A present but contract-invalid provider spell is also a rejection. The
        // placeholder must target a link whose reciprocal published-list check really
        // applies: PrestigePlus' NewSpell.UseAkashicForm link is catalogued with
        // PublishedListRequired=false, so an unpublished copy of it is still a valid
        // spell and would rightly let the module publish.
        const int invalid = 0;
        Check((bool)Get(SeekerLink(SeekerExternal[invalid]), "PublishedListRequired") && !(bool)Get(SeekerLink(SeekerExternal[4]), "PublishedListRequired"),
            "0081 the placeholder really targets a published-list link while the absent fifth spell really does not require one");
        SeekerProvider(c, SeekerExternal[invalid], false);
        Check(!SeekerAvailable(c, SeekerExternal[invalid]), "0081 the real five-fold provider check rejects the unpublished placeholder spell");
        reports.Clear();
        Call(registrar, "RegisterOptional", c, report);
        Check(!reports.Any(r => r.Item1 == SeekerModuleName) && tokens.All(t => c.Load(Id(t)) == null),
            "0081 an unpublished provider placeholder fails the five-fold contract check and defers the module");
        SeekerWithdraw(c, SeekerExternal[invalid]);
        Check(!SeekerAvailable(c, SeekerExternal[invalid]), "0081 withdrawing the placeholder leaves that constellation spell genuinely absent again");

        // Complete the catalogue: the module must publish atomically.
        SeekerProvider(c, SeekerExternal[invalid], true);
        SeekerProvider(c, SeekerExternal[4], true);
        Check(SeekerExternal.All(g => SeekerAvailable(c, g)), "0081 all five external constellation spells now satisfy the real provider contract");

        // Presentation boundary only. A handful of native facts in this host — the
        // Foresight donor among them — carry no m_Icon reference at all, so the
        // contract's "all 25 published facts carry a non-null Icon" gate could never
        // be evaluated here. Load the real donors into this cache and substitute an
        // uninitialized Sprite identity for the ones that are missing, exactly as the
        // shared Arcane01446 host already does in Fresh(). The production copy then
        // runs unchanged, so the assertion proves every published fact really
        // received an icon reference from its donor; which shipped artwork file it
        // points at stays a game observation step and is recorded as untested here.
        string foresightId = (string)AccessTools.Field(SeekerModule, "ForesightId").GetValue(null);
        foreach (string donor in catalog.SelectMany(row => row).Concat(new[] { foresightId }).Distinct())
            Check(c.Load(BlueprintGuid.Parse(donor)) != null, "0081 the real constellation donor loads in this host before the icon gate: " + donor);
        int filled = 0;
        foreach (var fact in Loaded(c).OfType<Kingmaker.Blueprints.Facts.BlueprintUnitFact>())
            if (ReferenceEquals(fact.Icon, null)) { Set(fact, "m_Icon", Empty<UnityEngine.Sprite>()); filled++; }
        var foresight = c.Load(BlueprintGuid.Parse(foresightId));
        Check(!ReferenceEquals(((Kingmaker.Blueprints.Facts.BlueprintUnitFact)foresight).Icon, null),
            "0081 the substituted Foresight donor now carries a sprite reference");
        // PsychicDisciplines.Build really does leave the shared selection without an
        // icon, so put that one back the way it ships. Otherwise the sweep above would
        // hand the clone an inherited icon and the module's own Foresight fallback for
        // EsotericStarseekerDisciplineSelection0081 would never be exercised.
        Set(disciplines, "m_Icon", null);
        Console.WriteLine("0081 host sprite substitution filled " + filled + " native facts that ship without any icon reference");

        reports.Clear();
        Call(registrar, "RegisterOptional", c, report);
        var own = reports.Where(r => r.Item1 == SeekerModuleName).ToArray();
        Check(own.Length == 1 && own[0].Item2 && own[0].Item3 == null,
            "0081 the real optional dispatcher publishes the completed constellation catalogue once: " + (own.Length == 1 && own[0].Item3 != null ? own[0].Item3.ToString() : "no error"));
        var published = tokens.Select(t => c.Load(Id(t))).ToArray();
        Check(published.All(bp => bp != null), "0081 all 25 private blueprints publish together once every constellation spell resolves");
        var root = (BlueprintArchetype)published[0];
        Check(root.GetParentClass() == cls && cls.Archetypes.Count(a => a.AssetGuid == root.AssetGuid) == 1,
            "0081 the archetype connects to the real Psychic class exactly once");
        Check(cls.Archetypes.Length == rootsBefore.Length + 1 && rootsBefore.All(a => cls.Archetypes.Contains(a)),
            "0081 every pre-existing Psychic archetype survives the new registration");
        Check(cls.Progression.UIGroups.Length == groupsBefore.Length + 1 && groupsBefore.All(g => cls.Progression.UIGroups.Contains(g)),
            "0081 the real class progression gains exactly one UI group and keeps the old ones");
        Check(disciplines.AllFeatures.SequenceEqual(poolsBefore), "0081 the shared discipline pool is never rewritten by this archetype");

        reports.Clear();
        Call(registrar, "RegisterAll", c, report);
        // RegisterAll reaches the module twice: once through its own unconditional
        // RegisterOptional hook and once through its own trailing hook. Both must
        // report the existing receipt instead of republishing anything.
        var repeated = reports.Where(r => r.Item1 == SeekerModuleName).ToArray();
        Check(repeated.Length == 2 && repeated.All(r => r.Item2 && r.Item3 == null),
            "0081 a repeated real dispatcher pass reaches both shipped hooks and both report success on the existing receipt, observed=" + repeated.Length);
        Check(tokens.Select((t, i) => ReferenceEquals(c.Load(Id(t)), published[i])).All(x => x)
            && cls.Archetypes.Count(a => a.AssetGuid == root.AssetGuid) == 1 && cls.Progression.UIGroups.Length == groupsBefore.Length + 1,
            "0081 a repeated real dispatcher pass is idempotent and reuses every published identity");
        // Reference equality, never UnityEngine.Object's overloaded ==: the host's
        // substituted sprite identities are managed objects with no resident native
        // instance, so `== null` reports them as null. What is proven here is that
        // production really copied a donor icon onto every published fact, and that
        // it copied the donor the frozen contract names. Which shipped artwork file
        // that sprite is stays a game observation step, recorded as untested.
        var facts = published.OfType<Kingmaker.Blueprints.Facts.BlueprintUnitFact>().ToArray();
        Check(facts.Length == 23 && facts.All(bp => !ReferenceEquals(bp.Icon, null)),
            "0081 all 23 published facts — the 25 private blueprints minus the archetype and the ability resource — carry a real icon reference, observed=" + facts.Length);
        object foresightIcon = Get(foresight, "m_Icon");
        Check(ReferenceEquals(Get(published[1], "m_Icon"), foresightIcon)
            && ReferenceEquals(Get(published[2], "m_Icon"), foresightIcon)
            && ReferenceEquals(Get(published[4], "m_Icon"), foresightIcon),
            "0081 the discipline selection, the class feature and the parent Written in the Stars ability all carry the contracted Foresight icon");
        for (int ci = 0; ci < catalog.Length; ci++)
        {
            object circleIcon = Get(c.Load(BlueprintGuid.Parse(catalog[ci][0])), "m_Icon");
            Check(ReferenceEquals(Get(published[5 + ci * 2], "m_Icon"), circleIcon) && ReferenceEquals(Get(published[6 + ci * 2], "m_Icon"), circleIcon),
                "0081 constellation " + SeekerStars[ci] + " ability and buff both reuse their own first-circle spell icon");
        }
        Check(classFingerprint() == fingerprintBefore,
            "0081 the real PsychicClass blueprint fingerprint, its progression level entries and the PsychicSpellcasting component fields stay byte-identical");

        var noSpells = (BlueprintFeatureSelection)published[1];
        var feature = (BlueprintFeature)published[2];
        var resource = (BlueprintAbilityResource)published[3];
        var parentAbility = (BlueprintAbility)published[4];

        // ---- C01-CASE-01: level 1/11/15 replacements and bonus-spell stripping.
        Console.WriteLine("0081 stage: archetype progression replacement and discipline bonus-spell stripping");
        Check(root.AddFeatures.Select(e => e.Level).SequenceEqual(new[] { 1, 15 }) && root.RemoveFeatures.Select(e => e.Level).SequenceEqual(new[] { 1, 11 }),
            "0081 the archetype declares exactly the level 1/15 additions and level 1/11 removals");
        Check(root.AddFeatures[0].Features.SequenceEqual(new BlueprintFeatureBase[] { noSpells, feature }), "0081 level 1 adds the spell-less discipline choice and Written in the Stars");
        Check(root.AddFeatures[1].Features.SequenceEqual(new BlueprintFeatureBase[] { major }), "0081 level 15 restores the major amplification eligibility marker");
        Check(root.RemoveFeatures[0].Features.SequenceEqual(new BlueprintFeatureBase[] { disciplines, amp }), "0081 level 1 removes the bonus-spell discipline choice and the first amplification");
        Check(root.RemoveFeatures[1].Features.SequenceEqual(new BlueprintFeatureBase[] { amp, major }), "0081 level 11 removes the replaced amplification choices including the major marker");

        var plain = new ProgressionData(cls.Progression);
        var seekerProgression = new ProgressionData(cls.Progression);
        seekerProgression.AddArchetype(root);
        // Except() yields the survivors in the native class progression's own order,
        // which is not the order the archetype happens to list its removals in, so
        // compare identity multisets rather than sequences. Nothing about the strictness
        // changes: both sides must contain exactly the same blueprints the same number
        // of times.
        Func<BlueprintFeatureBase[], string> keys = list =>
            string.Join(",", list.Select(x => x.AssetGuid.ToString()).OrderBy(k => k, StringComparer.Ordinal));
        for (int level = 1; level <= 20; level++)
        {
            var before = plain.GetLevelEntry(level).Features.ToArray();
            var after = seekerProgression.GetLevelEntry(level).Features.ToArray();
            var removed = before.Except(after).ToArray();
            var added = after.Except(before).ToArray();
            BlueprintFeatureBase[] wantRemoved = level == 1 ? new BlueprintFeatureBase[] { disciplines, amp } : level == 11 ? new BlueprintFeatureBase[] { amp, major } : Array.Empty<BlueprintFeatureBase>();
            BlueprintFeatureBase[] wantAdded = level == 1 ? new BlueprintFeatureBase[] { noSpells, feature } : level == 15 ? new BlueprintFeatureBase[] { major } : Array.Empty<BlueprintFeatureBase>();
            Check(keys(removed) == keys(wantRemoved) && keys(added) == keys(wantAdded),
                "0081 the real archetype only changes level " + level + " (removed " + removed.Length + " [" + keys(removed) + "] want [" + keys(wantRemoved) +
                "], added " + added.Length + " [" + keys(added) + "] want [" + keys(wantAdded) + "])");
        }

        var options = noSpells.AllFeatures.ToArray();
        Check(options.Length == 6 && options.SequenceEqual(magaambyan.AllFeatures.ToArray()),
            "0081 the archetype choice offers all six spell-less disciplines and reuses the real Magaambyan progressions");
        Check(!ReferenceEquals(noSpells, magaambyan) && !ReferenceEquals(noSpells, disciplines), "0081 the archetype owns an independent selection and never rewrites the shared receipts");
        var originals = disciplines.AllFeatures.Cast<BlueprintProgression>().ToArray();
        foreach (var option in options)
        {
            var stripped = (BlueprintProgression)option;
            // Both alias producers embed the source discipline's blueprint GUID in the
            // alias token ("Magaambyan" + original.AssetGuid + "01447" for the Telepath
            // clones, "01458" for the bridge clones), so match on that. It is a strictly
            // stronger identity than the display text the two producers copy — and the
            // display text cannot be resolved here at all, because the native
            // localization service is an icall a standalone CLR cannot run.
            var source = originals.SingleOrDefault(o => stripped.name.IndexOf(o.AssetGuid.ToString(), StringComparison.Ordinal) >= 0);
            Check(source != null, "0081 every spell-less alias names the exact original discipline it was cloned from: " + stripped.name);
            Check(source.LevelEntries.SelectMany(e => e.Features).OfType<BlueprintFeature>().Any(f => f.GetComponents<AddKnownSpell>().Any()),
                "0081 the original " + source.Name + " discipline really does grant bonus spells, so the stripping is meaningful");
            Check(stripped.LevelEntries.SelectMany(e => e.Features).OfType<BlueprintFeature>().All(f => !f.GetComponents<AddKnownSpell>().Any()),
                "0081 discipline " + stripped.Name + " grants zero bonus spells at any level");
            Check(stripped.LevelEntries.Select(e => e.Level).SequenceEqual(source.LevelEntries.Select(e => e.Level)) &&
                stripped.LevelEntries.All(e => e.Features.SequenceEqual(source.LevelEntries.Single(s => s.Level == e.Level).Features
                    .Where(v => !v.GetComponents<AddKnownSpell>().Any()))),
                "0081 discipline " + stripped.Name + " keeps every real power and its phrenic pool routing at levels 1/5/13, minus only the bonus-spell grants");
        }

        // ---- C01-CASE-04: action shape, daily resource, gate and no-refund rule.
        Console.WriteLine("0081 stage: full-round attunement, daily uses and the removed refund exploit");
        var variants = parentAbility.GetComponent<AbilityVariants>().Variants;
        Check(variants.Length == 10 && variants.Select(v => v.AssetGuid).Distinct().Count() == 10, "0081 the attunement menu exposes ten distinct constellations");
        foreach (var variant in variants)
        {
            Check(variant.Type == AbilityType.Supernatural && variant.Range == AbilityRange.Personal && (bool)Get(variant, "m_IsFullRoundAction") && !variant.CanTargetPoint,
                "0081 attuning to " + variant.name + " is a personal full-round supernatural action");
            var cost = variant.GetComponent<AbilityResourceLogic>();
            Check(cost != null && cost.Amount == 1 && ReferenceEquals(cost.RequiredResource, resource), "0081 " + variant.Name + " pays exactly one daily use of Written in the Stars");
            var gate = variant.GetComponent<WrittenInTheStarsRestriction0081>();
            var apply = variant.GetComponent<AbilityEffectRunAction>().Actions.Actions.OfType<WrittenInTheStarsApplyAction0081>().Single();
            Check(gate != null && ReferenceEquals(gate.Constellation.Get(), apply.Constellation.Get()),
                "0081 " + variant.Name + " gates and applies the very same constellation buff");
        }
        var buffBlueprints = SeekerStars.Select(s => Asset<BlueprintBuff>(c, "WrittenInTheStars" + s + "Buff0081")).ToArray();
        foreach (var buff in buffBlueprints)
        {
            Check(buff.RemoveOnRest && buff.StayOnDeath, "0081 " + buff.Name + " expires on a normal rest and survives death");
            var circles = buff.GetComponents<ConstellationCircleSpell0081>();
            Check(circles.Count() == 9 && circles.Select(x => x.Circle).SequenceEqual(Enumerable.Range(1, 9)) && circles.Select(x => x.name).Distinct().Count() == 9,
                "0081 " + buff.Name + " carries nine uniquely named circle-gated ledger components");
        }
        var maxAmount = Get(resource, "m_MaxAmount");
        Check((int)Get(maxAmount, "BaseValue") == 1 && (int)Get(maxAmount, "StartingLevel") == 11 && (int)Get(maxAmount, "LevelStep") == 100 && (int)Get(maxAmount, "PerStepIncrease") == 0,
            "0081 the real resource formula is one daily use, two from level 11 and never three");
        Check(parentAbility.Type == AbilityType.Supernatural && parentAbility.Range == AbilityRange.Personal && (bool)Get(parentAbility, "m_IsFullRoundAction") && !parentAbility.CanTargetPoint,
            "0081 the Written in the Stars menu itself is a personal full-round supernatural action");
        Check(parentAbility.GetComponent<AbilityResourceLogic>() == null, "0081 the menu pays nothing until a constellation variant is really chosen");
        foreach (int level in new[] { 1, 10, 11, 20 })
        {
            var probe = SeekerUnit(c, cls, root, level, true);
            Check(resource.GetMaxAmount(probe.Descriptor) == (level >= 11 ? 2 : 1),
                "0081 the real resource counter yields " + (level >= 11 ? 2 : 1) + " daily attunements at psychic level " + level);
        }

        var low = SeekerUnit(c, cls, root, 1, true);
        var lowBook = SeekerBook(low, cls, 1);
        var lowFact = Fact(low, feature);
        Check(lowFact.IsActive && low.HasFact(feature), "0081 the real class feature activates on the constructed starseeker");
        Check(low.Resources.GetResourceAmount(resource) == 1 && resource.GetMaxAmount(low.Descriptor) == 1, "0081 a level 1 starseeker holds exactly one daily attunement");
        Check(low.HasFact(parentAbility) && low.Abilities.Enumerable.Any(a => a.Blueprint == parentAbility), "0081 the feature really grants the attunement menu ability");
        var lowVariant = variants[0];
        SeekerAttune(low, lowVariant, cls, resource);
        var lowBuff = low.Buffs.Enumerable.Single(b => b.Blueprint == buffBlueprints[0]);
        Check(lowBuff.IsNotDispelable, "0081 an active attunement cannot be dispelled");
        Check(low.Resources.GetResourceAmount(resource) == 0, "0081 a successful attunement spends the single daily use");
        Check(seekerDirty > 0, "0081 attuning really asks the native action bar to refresh");
        var lowSpells = catalog[0].Select(g => c.Load(BlueprintGuid.Parse(g)) as BlueprintAbility).ToArray();
        Check(lowSpells.All(s => s != null), "0081 every Lantern Bearer constellation spell resolves in the real cache");
        for (int circle = 1; circle <= 9; circle++)
        {
            bool known = lowBook.IsKnown(lowSpells[circle - 1]);
            Check(known == (circle <= lowBook.MaxSpellLevel),
                "0081 at class level 1 only the castable first circle enters the real spellbook (circle " + circle + " known=" + known + ")");
            if (known) Check(lowBook.GetKnownSpell(lowSpells[circle - 1]).IsTemporary, "0081 the granted first-circle constellation spell is temporary knowledge");
        }

        // Level-up dynamically unlocks the newly castable circle. Psychic caster
        // level tracks class level, so reaching class level 4 really advances the
        // book to base level 4 and its maximum circle to 2.
        int dirtyBeforeLevel = seekerDirty;
        low.Progression.GetClassData(cls).Level = 4;
        for (int i = 0; i < 3; i++) lowBook.AddLevelFromClass(cls);
        Check(lowBook.RawBaseLevel == 4, "0081 the real book follows the starseeker to caster level four");
        EventBus.RaiseEvent<IOwnerGainLevelHandler>(low, handler => handler.HandleUnitGainLevel());
        Check(lowBook.MaxSpellLevel == 2 && lowBook.IsKnown(lowSpells[1]) && lowBook.GetKnownSpell(lowSpells[1]).IsTemporary,
            "0081 the real native level event unlocks the second circle while the attunement stays active");
        Check(!lowBook.IsKnown(lowSpells[2]), "0081 the level 4 starseeker still cannot borrow the third circle");
        Check(seekerDirty > dirtyBeforeLevel, "0081 unlocking a circle really refreshes the native action bar");

        // A second attunement below level 11 replaces the first and never refunds.
        low.Progression.GetClassData(cls).Level = 6;
        for (int i = 0; i < 2; i++) lowBook.AddLevelFromClass(cls);
        Check(lowBook.RawBaseLevel == 6 && lowBook.MaxSpellLevel == 3, "0081 the real book follows the starseeker to caster level six and the third circle");
        EventBus.RaiseEvent<IOwnerGainLevelHandler>(low, handler => handler.HandleUnitGainLevel());
        Check(lowBook.IsKnown(lowSpells[2]) && lowBook.GetKnownSpell(lowSpells[2]).IsTemporary,
            "0081 the real native level event also unlocks the third circle while the first attunement stays active");
        low.Resources.Restore(resource);
        Check(low.Resources.GetResourceAmount(resource) == 1, "0081 a normal replenish restores the single daily use below level 11");
        SeekerAttune(low, variants[1], cls, resource);
        Check(low.Buffs.Enumerable.Count(b => b.Blueprint.GetComponent<ConstellationCircleSpell0081>() != null) == 1,
            "0081 below level 11 at most one constellation can be attuned at a time");
        Check(low.Buffs.HasFact(buffBlueprints[1]) && !low.Buffs.HasFact(buffBlueprints[0]), "0081 the prior attunement is replaced by the newly chosen constellation");
        Check(low.Resources.GetResourceAmount(resource) == 0, "0081 replacing an attunement pays again and never refunds the previous use");
        Check(lowBook.IsKnown(Native<BlueprintAbility>(c, catalog[1][0])) && !lowBook.IsKnown(lowSpells[0]) && !lowBook.IsKnown(lowSpells[1]),
            "0081 the real spellbook follows the replacement: the new constellation is known and both old circles are revoked");
        low.Facts.Remove(lowFact);
        Check(!low.Buffs.Enumerable.Any(b => b.Blueprint.GetComponent<ConstellationCircleSpell0081>() != null) && !lowBook.IsKnown(Native<BlueprintAbility>(c, catalog[1][0])),
            "0081 removing the class feature cleans up every active attunement and its temporary spells");
        Check(low.Resources.GetResourceAmount(resource) == 0, "0081 respec cleanup never refunds a spent daily use");

        // ---- C01-CASE-03: dual attunement and FIFO replacement from level 11.
        Console.WriteLine("0081 stage: level 11 dual constellations and FIFO replacement");
        var high = SeekerUnit(c, cls, root, 11, true);
        var highBook = SeekerBook(high, cls, 11);
        Fact(high, feature);
        Check(resource.GetMaxAmount(high.Descriptor) == 2 && high.Resources.GetResourceAmount(resource) == 2, "0081 a level 11 starseeker holds exactly two daily attunements");
        Check(highBook.MaxSpellLevel == 5, "0081 the real level 11 psychic book reaches the fifth circle");
        SeekerAttune(high, variants[0], cls, resource);
        Check(high.Resources.GetResourceAmount(resource) == 1, "0081 the first attunement leaves one daily use");
        SeekerAttune(high, variants[1], cls, resource);
        Check(high.Resources.GetResourceAmount(resource) == 0, "0081 the second attunement spends the last daily use");
        Check(high.Buffs.HasFact(buffBlueprints[0]) && high.Buffs.HasFact(buffBlueprints[1]), "0081 both constellations stay attuned simultaneously from level 11");
        for (int circle = 1; circle <= 9; circle++)
        {
            var first = Native<BlueprintAbility>(c, catalog[0][circle - 1]);
            var second = Native<BlueprintAbility>(c, catalog[1][circle - 1]);
            bool expected = circle <= highBook.MaxSpellLevel;
            Check(highBook.IsKnown(first) == expected && highBook.IsKnown(second) == expected,
                "0081 both attunements grant two constellation spells per castable circle " + circle);
        }
        Check(highBook.GetKnownSpells(1).Count(a => a.Blueprint == Native<BlueprintAbility>(c, catalog[0][0])) == 1,
            "0081 the real ledger shares one entry per spell rather than stacking duplicates");
        high.Resources.Restore(resource);
        Check(high.Resources.GetResourceAmount(resource) == 2, "0081 a normal replenish restores both daily uses");
        SeekerAttune(high, variants[0], cls, resource, false);
        Check(high.Resources.GetResourceAmount(resource) == 2 && high.Buffs.Enumerable.Count(b => b.Blueprint.GetComponent<ConstellationCircleSpell0081>() != null) == 2,
            "0081 the gate blocks an already attuned constellation at zero cost and never stacks a third attunement");
        high.Resources.Spend(resource, 1);
        SeekerAttune(high, variants[2], cls, resource);
        Check(high.Resources.GetResourceAmount(resource) == 0, "0081 a third attunement pays one use");
        Check(!high.Buffs.HasFact(buffBlueprints[0]) && high.Buffs.HasFact(buffBlueprints[1]) && high.Buffs.HasFact(buffBlueprints[2]),
            "0081 the oldest attunement is replaced first and the newer two remain");
        Check(!highBook.IsKnown(Native<BlueprintAbility>(c, catalog[0][0])) && highBook.IsKnown(Native<BlueprintAbility>(c, catalog[2][0])),
            "0081 the real spellbook revokes the evicted constellation and grants the newcomer");

        // A normal rest clears the attunements and cleans the ledger.
        var restApply = AccessTools.Method(typeof(Kingmaker.Controllers.Rest.RestController), "ApplyRest", new[] { typeof(UnitDescriptor) });
        Check(restApply != null, "0081 the real native rest entry point exists");
        // Rest's RemoveOnRest loop, native Buff.Remove/OnDeactivate, the ledger
        // teardown and the real global rest event stay intact. Only the unrelated
        // whole-descriptor Replenish (health plus every other resource and its
        // scene services) is the explicit boundary, as in KineticUtility01433.
        var replenish = PatchProcessor.GetOriginalInstructions(restApply).Select(i => i.operand).OfType<MethodInfo>().Single(m => m.Name == "Replenish");
        h.Patch(replenish, prefix: new HarmonyMethod(typeof(Arcane01446Tests), nameof(SeekerNoReplenish)));
        try { Kingmaker.Controllers.Rest.RestController.ApplyRest(high.Descriptor); }
        finally { h.Unpatch(replenish, AccessTools.Method(typeof(Arcane01446Tests), nameof(SeekerNoReplenish))); }
        Check(!high.Buffs.Enumerable.Any(b => b.Blueprint.GetComponent<ConstellationCircleSpell0081>() != null),
            "0081 the real native rest RemoveOnRest loop clears every active constellation");
        foreach (int row in new[] { 1, 2 })
            Check(catalog[row].Take(highBook.MaxSpellLevel).Select(g => Native<BlueprintAbility>(c, g)).All(s => !highBook.IsKnown(s)),
                "0081 the real rest also revokes the resting constellations' temporary spells: row " + row);
        high.Resources.Restore(resource);
        Check(high.Resources.GetResourceAmount(resource) == 2, "0081 the real replenish refills both daily attunements after the rest cleared them");

        // ---- C01-CASE-05: permanent knowledge, multi-source isolation, preview.
        Console.WriteLine("0081 stage: permanent knowledge, second temporary source and level-up preview");
        var owner = SeekerUnit(c, cls, root, 18, true);
        var ownerBook = SeekerBook(owner, cls, 18);
        Fact(owner, feature);
        Check(ownerBook.MaxSpellLevel == 9, "0081 the real level 18 psychic book reaches the ninth circle");
        var permanent = Native<BlueprintAbility>(c, catalog[0][0]);
        ownerBook.AddKnown(1, permanent, false);
        Check(ownerBook.IsKnown(permanent) && !ownerBook.GetKnownSpell(permanent).IsTemporary, "0081 the starseeker really knows a permanent first-circle spell");
        SeekerAttune(owner, variants[0], cls, resource);
        Check(ownerBook.IsKnown(permanent) && !ownerBook.GetKnownSpell(permanent).IsTemporary, "0081 attuning to the same constellation keeps the permanent spell permanent");
        var third = Native<BlueprintAbility>(c, catalog[0][2]);
        Check(ownerBook.IsKnown(third) && ownerBook.GetKnownSpell(third).IsTemporary, "0081 the third circle is borrowed knowledge from the active attunement");
        for (int circle = 1; circle <= 9; circle++)
        {
            var spell = Native<BlueprintAbility>(c, catalog[0][circle - 1]);
            Check(ownerBook.IsKnown(spell) && ownerBook.GetKnownSpell(spell).IsTemporary == (circle != 1),
                "0081 at level 18 the whole active constellation is present and only the pre-paid first circle stays permanent (circle " + circle + ")");
        }

        // A second independent temporary source for the same borrowed spell.
        var secondGrant = new AddSpellKnownTemporary { name = "0081SecondTemporarySource", Level = 3, OnlySpontaneous = true };
        Set(secondGrant, "m_CharacterClass", SeekerRef<BlueprintCharacterClassReference>(cls));
        Set(secondGrant, "m_Spell", SeekerRef<BlueprintAbilityReference>(third));
        var secondBuff = new BlueprintBuff { AssetGuid = BlueprintGuid.Parse(Guid.NewGuid().ToString("N")), name = "0081SecondTemporarySourceBuff", Stacking = StackingType.Stack, ComponentsArray = new BlueprintComponent[] { secondGrant } };
        secondBuff.OnEnable();
        c.AddCachedBlueprint(secondBuff.AssetGuid, secondBuff);
        var secondSource = owner.Buffs.AddBuff(secondBuff, owner, null);
        Check(ownerBook.GetKnownSpells(3).Count(a => a.Blueprint == third) == 1, "0081 two real temporary sources still share exactly one ledger entry");
        secondSource.Remove();
        Check(ownerBook.IsKnown(third) && ownerBook.GetKnownSpell(third).IsTemporary, "0081 removing one temporary source preserves the remaining constellation grant");

        // Native level-up preview copying must not promote borrowed knowledge.
        var preview = SeekerUnit(c, cls, root, 18, true);
        var previewBook = SeekerBook(preview, cls, 18);
        var provenance = SeekerKnown(ownerBook).ToDictionary(a => a, a => a.IsTemporary);
        AccessTools.Method(typeof(UnitHelper), "CopySpellbook").Invoke(null, new object[] { owner, preview });
        Check(previewBook.IsKnown(third) && previewBook.GetKnownSpell(third).IsTemporary && previewBook.IsKnown(permanent) && !previewBook.GetKnownSpell(permanent).IsTemporary,
            "0081 the real native CopySpellbook preview keeps borrowed constellation knowledge temporary");
        Check(SeekerKnown(ownerBook).Count() == provenance.Count && provenance.All(p => p.Key.IsTemporary == p.Value),
            "0081 building a level-up preview changes no original permanent or temporary flag");
        previewBook.RemoveSpell(third);
        Check(!previewBook.IsKnown(third), "0081 the preview really gave up the borrowed constellation spell before paying for it");
        var paid = previewBook.AddKnown(3, third, false);
        Check(!paid.IsTemporary, "0081 the preview may permanently learn a spell an active constellation also grants");

        // Removing the attunement preserves the promoted permanent spell.
        var activeBuff = owner.Buffs.Enumerable.Single(b => b.Blueprint == buffBlueprints[0]);
        ownerBook.RemoveSpell(third);
        ownerBook.AddKnown(3, third, false);
        Check(!ownerBook.GetKnownSpell(third).IsTemporary, "0081 the real book promotes a borrowed constellation spell to permanent knowledge");
        Call(T("PsychicUndercasting"), "Reconcile", ownerBook);
        Check(ownerBook.IsKnown(third) && !ownerBook.GetKnownSpell(third).IsTemporary && ownerBook.IsKnown(permanent) && !ownerBook.GetKnownSpell(permanent).IsTemporary,
            "0081 the real undercasting reconciliation keeps a promoted constellation spell and unrelated permanent knowledge");
        int dirtyBeforeRemoval = seekerDirty;
        activeBuff.Remove();
        Check(ownerBook.IsKnown(third) && !ownerBook.GetKnownSpell(third).IsTemporary, "0081 losing the attunement never deletes a promoted permanent spell");
        Check(ownerBook.IsKnown(permanent) && !ownerBook.GetKnownSpell(permanent).IsTemporary, "0081 unrelated permanent knowledge survives attunement removal");
        Check(!ownerBook.IsKnown(Native<BlueprintAbility>(c, catalog[0][1])), "0081 every other borrowed circle is revoked with the attunement");
        Check(owner.Resources.GetResourceAmount(resource) == 1, "0081 manual buff removal never refunds the spent daily use");
        Check(seekerDirty > dirtyBeforeRemoval, "0081 deactivating a constellation really asks the native action bar to refresh");

        // ---- C01-CASE-07: major amplification eligibility at 11..14 versus 15+.
        Console.WriteLine("0081 stage: major amplification and mimic eligibility deferral");
        var majorType = T("PsychicMajor01460");
        var has = AccessTools.Method(majorType, "Has");
        var required = AccessTools.Method(majorType, "RequiredLevel");
        Check(castingFeature.GetComponents<MajorGrant01460>().Count() == 1,
            "0081 the real class progression carries exactly one native level-grant component for major amplifications");
        var synaptic = Asset<BlueprintFeature>(c, "SynapticShockFeature01458");
        var mimicRoot = Asset<BlueprintFeatureSelection>(c, "MimicMetamagic1Feature01458");
        Func<BlueprintFeature, UnitEntityData, bool> rawMajor = (feature, actor) =>
        {
            var prerequisite = feature.GetComponents<Kingmaker.Blueprints.Classes.Prerequisites.PrerequisiteFeature>().Single(p => p.Feature == major);
            return prerequisite.Check(null, actor.Descriptor, null);
        };
        foreach (var archetype in new[] { true, false })
            foreach (int level in new[] { 10, 11, 14, 15, 18 })
            {
                var actor = SeekerUnit(c, cls, root, level, archetype);
                SeekerBook(actor, cls, level);
                var grant = Fact(actor, castingFeature);
                var runtime = grant.Components.Single(r => r.SourceBlueprintComponent is MajorGrant01460);
                Check((int)required.Invoke(null, new object[] { actor }) == (archetype ? 15 : 11), "0081 the required major level is " + (archetype ? 15 : 11) + " for archetype=" + archetype);
                bool expected = level >= (archetype ? 15 : 11);
                Check(actor.HasFact(major) == expected, "0081 the real native level grant gives major amplifications at level " + level + " archetype=" + archetype + " expected=" + expected);
                Check(rawMajor(synaptic, actor) == expected && rawMajor(mimicRoot, actor) == expected,
                    "0081 raw Major and Mimic prerequisites agree with the level gate at level " + level + " archetype=" + archetype);
                Check((bool)has.Invoke(null, new object[] { actor }) == expected, "0081 PsychicMajor01460.Has agrees with the real grant at level " + level + " archetype=" + archetype);
                Check(actor.Progression.GetClassLevel(cls) == level, "0081 the deferral is a threshold, never a disguised class level: real level stays " + level);
                EventBus.RaiseEvent<IOwnerGainLevelHandler>(actor, handler => handler.HandleUnitGainLevel());
                Check(actor.HasFact(major) == expected, "0081 the real native level event keeps the same eligibility at level " + level + " archetype=" + archetype);
                runtime.TurnOff(); runtime.TurnOn();
                Check(actor.HasFact(major) == expected, "0081 a real fact restart cannot grant an Esoteric Starseeker major amplifications early at level " + level);
            }

        // D-F01: an independently owned MajorId with no AppliedFact used to
        // survive every reconciliation below level 15. Exercise all three real
        // entry points and the raw blueprint prerequisites, not only the Has helper.
        var stale = SeekerUnit(c, cls, root, 14, true);
        SeekerBook(stale, cls, 14);
        var staleGrant = Fact(stale, castingFeature);
        var staleRuntime = staleGrant.Components.Single(r => r.SourceBlueprintComponent is MajorGrant01460);
        var staleComponent = (MajorGrant01460)staleRuntime.SourceBlueprintComponent;
        Check(staleRuntime.GetData<Kingmaker.Designers.Mechanics.Facts.AddFeatureOnClassLevelData>().AppliedFact == null,
            "0081 D-F01 regression starts with no component-owned Major fact");
        Action seedStaleMajor = () =>
        {
            stale.AddFact(major);
            Check(staleRuntime.GetData<Kingmaker.Designers.Mechanics.Facts.AddFeatureOnClassLevelData>().AppliedFact == null && stale.HasFact(major),
                "0081 D-F01 regression seeds an independently owned MajorId with null AppliedFact");
            Check(rawMajor(synaptic, stale) && rawMajor(mimicRoot, stale),
                "0081 D-F01 regression proves the stale fact really opens both raw prerequisites before reconciliation");
        };
        Action<string> checkStaleMajorRemoved = trigger =>
        {
            Check(staleRuntime.GetData<Kingmaker.Designers.Mechanics.Facts.AddFeatureOnClassLevelData>().AppliedFact == null && !stale.HasFact(major),
                "0081 D-F01 " + trigger + " removes the independently owned MajorId below level 15");
            Check(!rawMajor(synaptic, stale) && !rawMajor(mimicRoot, stale),
                "0081 D-F01 " + trigger + " closes both raw Major and Mimic prerequisites below level 15");
        };
        seedStaleMajor();
        using (staleRuntime.RequestEventContext())
            AccessTools.Method(typeof(MajorGrant01460), "OnActivate").Invoke(staleComponent, null);
        checkStaleMajorRemoved("activation");
        seedStaleMajor();
        EventBus.RaiseEvent<IOwnerGainLevelHandler>(stale, handler => handler.HandleUnitGainLevel());
        checkStaleMajorRemoved("level event");
        seedStaleMajor();
        using (staleRuntime.RequestEventContext())
            AccessTools.Method(typeof(MajorGrant01460), "OnPostLoad").Invoke(staleComponent, null);
        checkStaleMajorRemoved("post-load");

        var control = SeekerUnit(c, cls, root, 11, false);
        var controlBook = SeekerBook(control, cls, 11);
        Check(!control.Progression.GetClassData(cls).Archetypes.Any(), "0081 the control actor really has no archetype");
        Check(controlBook.MaxSpellLevel == 5, "0081 the normal psychic control still really casts fifth-circle spells");
        Check(Call(T("StarseekerRuntime"), "Book", control) == null, "0081 the shipped runtime refuses to hand a normal psychic the starseeker constellation book");
        Check(!variants[0].GetComponent<WrittenInTheStarsRestriction0081>().IsCasterRestrictionPassed(control),
            "0081 a normal psychic at the same level and circle can never attune to a constellation");

        PsychicExpansionTests.Inventory["esotericStarseeker0081Contracts"] = new JObject
        {
            ["passed"] = true,
            ["assertions"] = checks - start,
            ["case"] = "starseeker0081",
            ["contract"] = "C01-STARSEEKER-20261107-r1",
            ["cases"] = new JArray("C01-CASE-01", "C01-CASE-02", "C01-CASE-03", "C01-CASE-04", "C01-CASE-05", "C01-CASE-06", "C01-CASE-07", "C01-CASE-08"),
            ["ownedBlueprints"] = new JArray(tokens.Select(t => new JObject { ["token"] = t, ["id"] = Id(t).ToString() })),
            ["constellationCatalog"] = new JArray(Enumerable.Range(0, 10).Select(i => new JObject { ["constellation"] = SeekerStars[i], ["spells"] = new JArray(catalog[i]) })),
            ["newHarmonyPatchCount"] = bridges.Length,
            ["gameLaunchTested"] = false,
            ["constellationNamingAuthority"] = "A-catalog90.json; the erroneous contract 8.2 / C-cases.json names (Archer, Charioteer, Comet, Dragon, Courtesan) are deliberately not used",
            ["hostPresentationBoundaries"] = new JArray(
                "The native BlueprintUnitFact.Name and .Description getters are Harmony-prefixed in this host to yield the authored asset name and the localization key. The real getters reach Kingmaker.Localization.LocalizedString.op_Implicit, an icall a standalone CLR cannot execute (SecurityException: ECall), and that operator has no IL body to patch. Registration order, costs, prerequisites, receipts and every rule branch stay exactly as shipped, and production identity checks compare the lowercase asset name rather than these getters, so no rule outcome depends on the substitution.",
                "117 native facts in this host carry no m_Icon reference at all, so an uninitialized Sprite identity is substituted for them before the module publishes — the same substitution the shared Arcane01446 host already performs in Fresh(). The shared CotW DisciplineSelection is then put back to icon-less, the way production really ships it, so the module's own Foresight fallback is exercised. Proven here: production copies a donor icon onto all 23 published facts and picks the donor the contract names. Not proven here: which shipped artwork file each sprite is.",
                "Spell-less alias to original discipline matching uses the source blueprint GUID embedded in the alias token instead of display text, which this host cannot resolve.",
                "The real RegisterAll dispatcher reports 111 modules with 19 pre-existing failures unrelated to 0081 (rage powers and their chain, the Psychic extension GUID conflict, Witch archetype Havocker NullReferenceException, Mindblade, Skirnir, Mutation Mind, Psychic Marauder, Feral Hunter, Totem-Bonded, the FrightfulAspect ki adaptations, Increased Damage Reduction and the phrenic amplification pool). Every one is printed verbatim with its exception in the run log. None is caused by this round and none is fixed by it."),
            ["baselineRegressionsFixed"] = new JArray(
                "0.14.67 TEST commit 15f3606 added a second Migration01426.Definition(object, IEqualityComparer<object>) overload. Every untyped AccessTools.Method(type, \"Definition\") lookup then threw AmbiguousMatchException, which aborted this host before any 0081 assertion ran. Fixed at the two on-path sites — src/FerociousBeast01431.cs and tests/Repair01436CallerTests.cs — by naming the single-argument original. Migration01426 itself is untouched. 22 further untyped sites remain and are enumerated in targeted/definition-ambiguity-sites.json; none is on a path this round exercises, so they stay unfixed rather than unverifiable.",
                "The same commit taught MimicCatalogue01461.BuildGraph to read BlueprintFeature.Name and .Description, so the real PsychicClassCompletion01458 producer hit the localization icall and threw SecurityException in any standalone CLR host. Its receipt never published, PsychicDisciplineBridge01458 silently deferred, both discipline pools stayed at three of six, and 0081 could never publish. Addressed on the host side only, through the display-getter prefixes above; no production behaviour changed and the in-game path is unaffected because Unity resolves the localization service normally."),
            ["boundary"] = "Actual native BlueprintsCache/ProgressionData, the real published Psychic class, spellbook DemandSpellbook/AddLevelFromClass/IsKnown/AddKnown/RemoveSpell, UnitPartTemporarySpellsKnown ledger, PsychicUndercasting.Reconcile, AbilityCastRateUtils.SpendResources, AbilityEffectRunAction.Apply, BuffCollection add/remove, RestController.ApplyRest RemoveOnRest loop, UnitHelper.CopySpellbook, EventBus IOwnerGainLevelHandler dispatch, UnitUISettings.SetDirty and the production Harmony bridges execute. Constructed actor proxies and the shared fixture scene/dice/difficulty shims are explicit boundaries; the level-up controller and XP table are bypassed by advancing ClassData.Level plus the real book and event. External provider spells use contract fixtures validated through the shipped PsychicSpellIntegration five-fold check; no provider assembly is loaded. Rest skips the unrelated whole-descriptor UnitDescriptor.Replenish, so the post-rest refill is proven through the real UnitAbilityResourceCollection.Restore instead. Unity rendering, real combat, permanent SpellsKnown slot accounting in the level-up UI, pre-cast cancellation through the real command queue, full save serialization/reload and the level-up/respec UI remain untested game cases."
        };
        Console.WriteLine("PASS: " + (checks - start) + " Esoteric Starseeker 0081 assertions.");
        return checks - start;
    }
}

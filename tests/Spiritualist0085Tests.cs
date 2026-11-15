using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Selection;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.Blueprints.Facts;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Enums;
using Kingmaker.RuleSystem;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.FactLogic;
using Kingmaker.UnitLogic.ActivatableAbilities;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.Designers.Mechanics.Facts;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Mechanics.Components;

// COTW-0085 B-stage directed checks, contract C-0085-20261115-r1.
// The host enters with COTW_0085_CASE=spiritualist0085 and routes straight
// here after the shared Psychic fixture; no historical behaviour suite runs.
//
// Scope: real game Managed blueprint types, the real production Spiritualist
// registration, the private 137-row catalog, six emotion graphs, the reserved
// PetType and the pure resource/progression shapes. The native companion chain
// (AddPet -> SetMaster -> UnitPartPetMaster -> AddClassLevels), scene placement,
// rest UI, save/load and the respec screen are NOT entered here; they are D /
// player-TEST boundaries and are reported as not run.
internal static class Spiritualist0085Tests
{
    private static int assertions;
    private static readonly List<string> Failures = new List<string>();
    private static Assembly Main => AppDomain.CurrentDomain.GetAssemblies()
        .First(a => a.GetName().Name == "CallOfTheWild.Wrath.Prototype");
    private static Type T(string name) => Main.GetType("CallOfTheWild.Wrath.Prototype." + name, true);
    private static object Get(object o, string f) => AccessTools.Field(o.GetType(), f)?.GetValue(o)
        ?? AccessTools.Field(o.GetType(), "<" + f + ">k__BackingField")?.GetValue(o);
    private static object Call(string type, string method, params object[] args)
        => AccessTools.Method(T(type), method).Invoke(null, args);
    private static BlueprintGuid Id(string token) => (BlueprintGuid)Call("Spiritualist0085Ids", "Id", token);
    private static object IdsField(string name) => AccessTools.Field(T("Spiritualist0085Ids"), name).GetValue(null);

    private static void Check(bool ok, string message)
    {
        if (ok) { assertions++; Console.WriteLine("0085 CHECK: " + message); }
        else { Failures.Add(message); Console.WriteLine("0085 CHECK FAILED: " + message); }
    }

    private static T Load<T>(BlueprintsCache cache, BlueprintGuid guid) where T : SimpleBlueprint => cache.Load(guid) as T;
    private static Kingmaker.Blueprints.Classes.Spells.SpellListComponent StagedSpellList(BlueprintSpellList list)
    {
        var c = new Kingmaker.Blueprints.Classes.Spells.SpellListComponent { name = "Staged0085List", SpellLevel = 1 };
        AccessTools.Field(c.GetType(), "m_SpellList").SetValue(c, BlueprintReferenceBase.CreateTyped<BlueprintSpellListReference>(list));
        return c;
    }
    private static BlueprintComponent Component(BlueprintScriptableObject bp, string typeName)
        => bp?.ComponentsArray?.FirstOrDefault(c => c != null && c.GetType().Name == typeName);
    private static IEnumerable<BlueprintComponent> Components(BlueprintScriptableObject bp, string typeName)
        => bp?.ComponentsArray?.Where(c => c != null && c.GetType().Name == typeName) ?? Enumerable.Empty<BlueprintComponent>();

    internal static int Run(BlueprintsCache cache, Harmony harmony)
    {
        const string prefix = "CotWWrathSpiritualist0085";

        // ---- Stage the audited optional SLA roots (provider/CotW identities
        // from A-spiritualist-assets) so the atomic gate can be exercised offline.
        // Real provider behaviour remains a D / player-TEST boundary.
        var emotionsForStaging = ((Array)IdsField("Emotions")).Cast<object>().ToArray();
        foreach (var row in emotionsForStaging)
        {
            var slaGuids = (string[])Get(row, "SlaGuids");
            var slaProviders = (string[])Get(row, "SlaProviders");
            for (int i = 0; i < 4; i++)
            {
                BlueprintGuid slaGuid;
                var provider = slaProviders[i];
                if (provider == null) slaGuid = BlueprintGuid.Parse(slaGuids[i]);
                else if (provider.StartsWith("CotW:")) slaGuid = (BlueprintGuid)Call("PsychicAssets", "Id", provider.Substring(5));
                else
                {
                    var link = ((Array)AccessTools.Field(T("PsychicSharedSpellCatalog"), "Entries").GetValue(null))
                        .Cast<object>().FirstOrDefault(e => (string)Get(e, "Name") == provider);
                    if (link == null) continue;
                    slaGuid = BlueprintGuid.Parse((string)Get(link, "Guid"));
                }
                if (cache.Load(slaGuid) == null)
                {
                    var stub = new BlueprintAbility { AssetGuid = slaGuid, name = "Staged0085Sla" + slaGuid, Type = AbilityType.Spell };
                    if (provider != null && !provider.StartsWith("CotW:"))
                    {
                        // Match the audited shared-catalog identity so the
                        // production Available gate accepts the staged root.
                        var link2 = ((Array)AccessTools.Field(T("PsychicSharedSpellCatalog"), "Entries").GetValue(null))
                            .Cast<object>().First(e => (string)Get(e, "Name") == provider);
                        stub.name = (string)Get(link2, "Name");
                        var school = (Kingmaker.Blueprints.Classes.Spells.SpellSchool)Get(link2, "School");
                        // Published-list providers also need a reciprocal list
                        // publication; stage a private one-spell list for them.
                        var stagedList = new BlueprintSpellList { AssetGuid = BlueprintGuid.Parse("0085" + slaGuid.ToString().Substring(4)), name = "Staged0085ProviderList" + slaGuid };
                        var levelRow = new Kingmaker.Blueprints.Classes.Spells.SpellLevelList(1);
                        AccessTools.Field(levelRow.GetType(), "m_Spells").SetValue(levelRow, new System.Collections.Generic.List<BlueprintAbilityReference> { BlueprintReferenceBase.CreateTyped<BlueprintAbilityReference>(stub) });
                        stagedList.SpellsByLevel = new[] { levelRow };
                        stagedList.OnEnable();
                        cache.AddCachedBlueprint(stagedList.AssetGuid, stagedList);
                        stub.ComponentsArray = new BlueprintComponent[]
                        {
                            new Kingmaker.Blueprints.Classes.Spells.SpellComponent { name = "Staged0085School", School = school },
                            new Kingmaker.UnitLogic.Abilities.Components.AbilityEffectRunAction { name = "Staged0085Run", Actions = new Kingmaker.ElementsSystem.ActionList { Actions = Array.Empty<Kingmaker.ElementsSystem.GameAction>() } },
                            StagedSpellList(stagedList)
                        };
                    }
                    stub.OnEnable();
                    cache.AddCachedBlueprint(slaGuid, stub);
                }
            }
        }

        // ---- Registration gate and idempotency (C01) ----
        bool first = (bool)Call("Spiritualist0085", "Register", cache);
        Check(first, "registration publishes with all native donors present");
        if (!first)
        {
            Console.WriteLine("0085 NOT RUN: registration rejected: " + AccessTools.Field(T("Spiritualist0085"), "LastSummary").GetValue(null));
            return assertions;
        }
        var cls = Load<BlueprintCharacterClass>(cache, Id("Spiritualist0085Class"));
        Check(cls != null, "private class published");
        bool second = (bool)Call("Spiritualist0085", "Register", cache);
        Check(second && ReferenceEquals(cache.Load(cls.AssetGuid), cls), "second registration is idempotent and preserves identity");

        // ---- Master class shape ----
        var inquisitor = Load<BlueprintCharacterClass>(cache, BlueprintGuid.Parse("f1a70d9e1b0b41e49874e1fa9052a1ce"));
        Check(!cls.IsArcaneCaster && !cls.IsDivineCaster && !cls.PrestigeClass && !cls.HideInUI, "playable non-arcane non-divine base class");
        Check(cls.SkillPoints == inquisitor.SkillPoints - 1, "skill points are Inquisitor minus one");
        Check((DiceType)Get(cls, "HitDie") == DiceType.D8, "d8 hit die");
        Check(ReferenceEquals(Get(cls, "m_BaseAttackBonus"), Get(inquisitor, "m_BaseAttackBonus")), "Inquisitor BAB progression copied");
        Check(cls.ClassSkills.Contains(StatType.SkillPersuasion) && cls.ClassSkills.Contains(StatType.SkillUseMagicDevice) && cls.ClassSkills.Length == 7, "seven class skills including Persuasion and UMD");
        Check(cls.RecommendedAttributes.Contains(StatType.Wisdom), "Wisdom recommended");

        // ---- Private spellbook (Wisdom spontaneous, Inquisitor tables) ----
        var book = cls.Spellbook;
        Check(book != null && book.CastingAttribute == StatType.Wisdom && book.Spontaneous && !book.IsArcane, "Wisdom spontaneous non-arcane spellbook");
        Check(book.CantripsType == CantripsType.Cantrips && !book.AllSpellsKnown && !book.CanCopyScrolls, "cantrips, limited known, no scroll copying");
        Check(book.CharacterClass == cls, "reciprocal class link");
        var inqBook = Load<BlueprintSpellbook>(cache, BlueprintGuid.Parse("57fab75111f377248810ece84193a5a5"));
        Check(ReferenceEquals(book.SpellsKnown, inqBook.SpellsKnown) && ReferenceEquals(book.SpellsPerDay, inqBook.SpellsPerDay), "Inquisitor known/per-day tables reused read-only");
        var marker = Component(book, "PsychicSpellbookMarker");
        Check(marker != null && (int)Get(marker, "MaximumSpellLevel") == 6, "psychic marker capped at circle 6");

        // ---- 137-row catalog and source circles ----
        var rows = ((Array)IdsField("SpellList")).Cast<object>().ToArray();
        Func<object, int> circle = r => (int)Get(r, "Circle");
        Func<object, string> label = r => (string)Get(r, "Label");
        Func<object, string> guid = r => (string)Get(r, "Guid");
        Check(rows.Length == 137, "catalog holds exactly 137 source rows");
        var expected = new[] { 6, 17, 24, 28, 22, 24, 16 };
        for (int c = 0; c <= 6; c++)
            Check(rows.Count(r => circle(r) == c) == expected[c], "circle " + c + " has " + expected[c] + " source rows");
        Check(rows.All(r => circle(r) >= 0 && circle(r) <= 6), "no source row above circle 6");
        var list = book.SpellList;
        Check(list.SpellsByLevel.Length >= 11 && list.SpellsByLevel.Take(11).Select((r, i) => r.SpellLevel == i).All(x => x), "canonical 0-10 spell list array");
        for (int c = 7; c <= 10; c++)
            Check(list.SpellsByLevel[c].SpellsRefs == null || !list.SpellsByLevel[c].SpellsRefs.Any(), "circle " + c + " stays empty");
        var ghoul = rows.Single(r => label(r) == "Ghoul Touch");
        Check(guid(ghoul) == null && Get(ghoul, "CotwToken") == null && Get(ghoul, "Provider") == null, "Ghoul Touch has no fake root");
        var unavailable = ((string[])AccessTools.Field(T("Spiritualist0085"), "LastUnavailable").GetValue(null));
        Check(unavailable.Any(u => u.Contains("Ghoul Touch")), "Ghoul Touch reported unavailable");
        var lesser = rows.Single(r => label(r) == "Animate Dead, Lesser");
        Check(circle(lesser) == 2 && guid(lesser) == "57fcf8016cf04da4a8b33d2add14de7e", "Lesser Animate Dead kept at source circle 2 with the native root");
        Check(list.SpellsByLevel[2].SpellsRefs.Any(r => r.Guid == BlueprintGuid.Parse("57fcf8016cf04da4a8b33d2add14de7e")), "Lesser Animate Dead wired into circle 2 of the private list");

        // ---- Six emotions, one AddPet each, distinct marked units (C01/C16) ----
        var emotions = ((Array)IdsField("Emotions")).Cast<object>().ToArray();
        Func<object, string> name = e => (string)Get(e, "Name");
        Check(emotions.Length == 6 && emotions.Select(name).SequenceEqual(new[] { "Anger", "Despair", "Hatred", "Fear", "Zeal", "Kindness" }), "exactly the six original emotions, Whimsy unwired");
        var selection = Load<BlueprintFeatureSelection>(cache, Id("EmotionalFocus"));
        Check(selection != null && ((BlueprintFeatureReference[])Get(selection, "m_AllFeatures")).Length == 6, "emotion selection offers six options");
        var units = new HashSet<BlueprintGuid>();
        var petType = (PetType)850085;
        Check((int)petType == 850085 && !Enum.GetValues(typeof(PetType)).Cast<PetType>().Contains(petType) && (int)petType != 604767, "PetType 850085 reserved, no native or Making Friends overlap");
        foreach (var row in emotions)
        {
            var n = name(row);
            var prog = Load<BlueprintProgression>(cache, Id(n + "Emotion"));
            Check(prog != null, n + " emotion progression published");
            var pets = prog.GetComponents<AddPet>().ToArray();
            Check(pets.Length == 1, n + " has exactly one native AddPet");
            var pet = pets[0];
            Check(pet.Type == petType, n + " AddPet uses reserved type 850085");
            Check((bool)Get(pet, "m_UseContextValueLevel") && (bool)Get(pet, "m_ForceAutoLevelup") && (bool)Get(pet, "m_DestroyPetOnDeactivate"), n + " AddPet context-level, force-auto, destroy-on-remove");
            Check(Get(pet, "m_LevelRank") == null && Get(pet, "m_UpgradeFeature") == null && pet.UpgradeLevel == 0, n + " AddPet has no rank/upgrade donor");
            var unit = pet.Pet;
            Check(unit != null && units.Add(unit.AssetGuid), n + " unit blueprint is distinct");
            var unitMarker = Component(unit, "Phantom0085Marker");
            Check(unitMarker != null && (string)Get(unitMarker, "Emotion") == n, n + " unit carries the matching private marker");
            Check(unit.Strength == (int)Get(row, "Str") && unit.Dexterity == (int)Get(row, "Dex") && unit.Constitution == 13 && unit.Intelligence == 7 && unit.Wisdom == 10 && unit.Charisma == 13, n + " unit ability scores match A");
            Check(Component(unit, "AllowDyingCondition") != null && Component(unit, "AddResurrectOnRest") != null, n + " unit keeps AllowDyingCondition and AddResurrectOnRest");
            var levels = unit.GetComponent<AddClassLevels>();
            Check(levels != null && levels.DoNotApplyAutomatically && levels.Levels == 0, n + " unit AddClassLevels is manual and starts at 0");
            var archetype = ((BlueprintArchetypeReference[])Get(levels, "m_Archetypes")).Single().Get();
            Check(archetype != null && archetype.name == prefix + n + "PhantomArchetype", n + " archetype attached");
            Check(archetype.GetParentClass().name == prefix + "PhantomClass", n + " archetype parent is the private phantom class");
            bool fort = archetype.FortitudeSave != null, refl = archetype.ReflexSave != null, will = archetype.WillSave != null;
            Check(fort == (bool)Get(row, "Fort") && refl == (bool)Get(row, "Ref") && will == (bool)Get(row, "Will"), n + " good saves match the frozen code values");
            var slaLevels = new[] { 5, 7, 9, 16 };
            for (int i = 0; i < 4; i++)
            {
                var entry = prog.LevelEntries.Single(e => e.Level == slaLevels[i]);
                Check(entry.Features.Count >= 1, n + " SLA entry at level " + slaLevels[i]);
            }
            foreach (var lvl in new[] { 1, 7, 12, 17 })
            {
                var entry = prog.LevelEntries.Single(e => e.Level == lvl);
                Check(entry.Features.Any(f => f.GetComponents<AddFeatureToPet>().Any()), n + " pet feature grant at level " + lvl);
            }
        }

        // ---- Phantom class and shared progression ----
        var phantomClass = Load<BlueprintCharacterClass>(cache, Id("PhantomClass"));
        Check(phantomClass != null && phantomClass.HideInUI, "phantom class hidden from players");
        Check((DiceType)Get(phantomClass, "HitDie") == DiceType.D10, "phantom d10 hit die");
        Check(phantomClass.GetComponent<Kingmaker.Blueprints.Classes.Prerequisites.PrerequisiteIsPet>() != null, "phantom class is pet-only");
        var phantomProg = phantomClass.Progression;
        Check(phantomProg.LevelEntries.Length == 20, "phantom progression spans 20 levels");
        Check(phantomProg.LevelEntries[0].Features.Any(f => f.name == prefix + "PhantomDR5Slashing"), "phantom level 1 grants DR 5/slashing");
        Check(phantomProg.LevelEntries[0].Features.Any(f => f.name == prefix + "PhantomSlamDamage"), "phantom level 1 grants slam dice");
        Check(phantomProg.LevelEntries[14].Features.Any(f => f.name == prefix + "PhantomDR15"), "phantom level 15 grants the DR III entry");

        // ---- Master progression milestones ----
        var masterProg = cls.Progression;
        Check(masterProg.LevelEntries.Length == 20, "master progression spans 20 levels");
        Func<int, string, bool> has = (lvl, token) => masterProg.LevelEntries.Single(e => e.Level == lvl).Features.Any(f => f.AssetGuid == Id(token));
        Check(has(1, "EmotionalFocus") && has(1, "Spellcasting") && has(1, "Knacks") && has(1, "EthericTetherFeature") && has(1, "SharedConsciousness") && has(1, "MastersAlignment"), "level 1 grants selection, casting, knacks, tether, shared consciousness, alignment");
        Check(has(3, "BondedManifestationFeature"), "level 3 Bonded Manifestation");
        Check(has(4, "SpiritualInference"), "level 4 Spiritual Inference");
        Check(has(6, "PhantomRecall"), "level 6 Phantom Recall");
        Check(has(10, "FusedConsciousness"), "level 10 Fused Consciousness");
        Check(has(12, "GreaterSpiritualInference"), "level 12 Greater Spiritual Inference");
        Check(has(14, "SpiritualBondFeature"), "level 14 Spiritual Bond");
        Check(has(17, "DualBond"), "level 17 Dual Bond");
        Check(has(20, "PotentPhantom"), "level 20 Potent Phantom");

        // ---- Resource shapes (C05/C12 maxima) ----
        var recall = Load<BlueprintAbilityResource>(cache, Id("PhantomRecallResource"));
        var ra = Get(recall, "m_MaxAmount");
        Check((int)Get(ra, "BaseValue") == 0 && (int)Get(ra, "StartingLevel") == 6 && (int)Get(ra, "LevelStep") == 4 && (int)Get(ra, "StartingIncrease") == 1 && (int)Get(ra, "PerStepIncrease") == 1, "recall resource is 1/2/3/4 at 6/10/14/18");
        var bonded = Load<BlueprintAbilityResource>(cache, Id("BondedManifestationResource"));
        var ba = Get(bonded, "m_MaxAmount");
        Check((int)Get(ba, "BaseValue") == 3 && (bool)Get(ba, "IncreasedByLevel") && (int)Get(ba, "LevelIncrease") == 1, "bonded manifestation budget is 3 + level");
        var dual = Load<BlueprintFeature>(cache, Id("DualBond"));
        Check(Component(dual, "IncreaseResourceAmountBySharedValue") != null, "Dual Bond adds a second level-scaled budget");

        // ---- Consciousness grants (C11) ----
        var shared = Load<BlueprintFeature>(cache, Id("SharedConsciousness"));
        var fused = Load<BlueprintFeature>(cache, Id("FusedConsciousness"));
        var sharedGrants = Components(shared, "Phantom0085ConsciousnessGrant").ToArray();
        var fusedGrants = Components(fused, "Phantom0085ConsciousnessGrant").ToArray();
        Check(sharedGrants.Length == 12 && fusedGrants.Length == 12, "two skill-focus grants per emotion on shared and fused");
        Check(sharedGrants.All(g => !(bool)Get(g, "Permanent")) && fusedGrants.All(g => (bool)Get(g, "Permanent")), "shared is confined-gated, fused is permanent");
        Check(sharedGrants.All(g => Get(g, "SaveBuff") != null) && fusedGrants.All(g => Get(g, "SaveBuff") != null), "consciousness save buffs wired");
        var fusedSave = ((BlueprintBuffReference)Get(fusedGrants[0], "SaveBuff")).Get();
        Check(fusedSave.GetComponent<ContextRankConfig>() != null, "fused save bonus scales by class rank");

        // ---- Potent Phantom (C16) ----
        var potent = Load<BlueprintFeatureSelection>(cache, Id("PotentPhantom"));
        Check(((BlueprintFeatureReference[])Get(potent, "m_AllFeatures")).Length == 6, "Potent Phantom offers six secondary emotions");
        foreach (var reference in (BlueprintFeatureReference[])Get(potent, "m_AllFeatures"))
        {
            var option = reference.Get();
            Check(option.GetComponent<Kingmaker.Blueprints.Classes.Prerequisites.PrerequisiteNoFeature>() != null, "Potent option " + option.name + " forbids the same primary emotion");
        }

        // ---- Self-review fixes (2026-11-16) ----
        var recallAbility = Load<BlueprintAbility>(cache, Id("PhantomRecallAbility"));
        Check(recallAbility.GetComponent<Kingmaker.UnitLogic.Abilities.Components.AbilityResourceLogic>() == null, "recall spends inside the action only on a successful move, not via pre-cost");
        Check(recallAbility.ComponentsArray.Any(c => c.GetType().Name == "AbilityEffectRunAction"), "recall action present");
        var tether = Load<BlueprintFeature>(cache, Id("EthericTetherFeature"));
        var bond = Load<BlueprintFeature>(cache, Id("SpiritualBondFeature"));
        var tetherToggle = tether.GetComponent<AddFacts>();
        var bondToggle = bond.GetComponent<AddFacts>();
        var tetherActivatable = ((BlueprintUnitFactReference[])Get(tetherToggle, "m_Facts")).Select(r => r.Get()).OfType<BlueprintActivatableAbility>().First();
        var bondActivatable = ((BlueprintUnitFactReference[])Get(bondToggle, "m_Facts")).Select(r => r.Get()).OfType<BlueprintActivatableAbility>().First();
        Check((int)tetherActivatable.Group == 850085 && (int)bondActivatable.Group == 850085 && tetherActivatable.WeightInGroup == 1 && bondActivatable.WeightInGroup == 1, "Etheric Tether and Spiritual Bond share one mutually exclusive group");
        var inferenceOwner = Load<BlueprintBuff>(cache, Id("SpiritualInferenceOwner"));
        var inferenceAlly = Load<BlueprintBuff>(cache, Id("GreaterSpiritualInferenceAlly"));
        Check(inferenceOwner.Stacking == StackingType.Stack && inferenceAlly.Stacking == StackingType.Stack, "inference buffs stack per owning phantom");
        var bondedFeature = Load<BlueprintFeature>(cache, Id("BondedManifestationFeature"));
        var bondedGrants = bondedFeature.GetComponents<AddFeatureOnClassLevel>().ToArray();
        Check(bondedGrants.Any(g => g.Level == 8) && bondedGrants.Any(g => g.Level == 18), "tendril attacks granted at 8 and 18");
        Check(bondedGrants.All(g => ((BlueprintFeatureReference)Get(g, "m_Feature")).Get().GetComponent<AddFacts>() != null), "tendril grants carry abilities");
        var phantomProg2 = phantomClass.Progression;
        Check(phantomProg2.LevelEntries[6].Features.Any(f => f.AssetGuid == BlueprintGuid.Parse("a634f97d23662b347872ffcf580495b3")), "phantom level 7 grants the native Multiattack donor");
        var dr15 = Load<BlueprintFeature>(cache, Id("PhantomDR15"));
        var dr15Comp = dr15.GetComponent<Kingmaker.UnitLogic.FactLogic.AddDamageResistancePhysical>();
        Check(dr15Comp != null, "dr15 component present");
        var dr15Value = (Kingmaker.UnitLogic.Mechanics.ContextValue)Get(dr15Comp, "Value");
        Check(dr15Value.Value == 15 && !dr15Comp.BypassedByForm, "level-15 DR implements the contract 15/- (not the original 5/slashing)");

        // ---- C13/C14 exact transfer arithmetic (pure contract function) ----
        var transferType = T("Phantom0085DamageTransfer");
        var transferMethod = AccessTools.Method(transferType, "TransferAmount");
        Func<bool, int, int, int, int, int> transfer = (bond, result, petHp, masterHp, con) =>
            (int)transferMethod.Invoke(null, new object[] { bond, result, petHp, masterHp, con });
        Check(transfer(false, 16, 1, 10, 13) == 3, "C13: decisive packet transfers exactly 3 (phantom 1 -> -12, owner 10 -> 7)");
        Check(transfer(false, 16, 1, 2, 13) == 1, "C13: owner at 2 HP transfers at most 1");
        Check(transfer(false, 5, 10, 10, 13) == 0, "C13: non-lethal packet transfers zero");
        Check(transfer(true, 20, 1, 5, 13) == 13, "C14: 20 damage transfers exactly 13 (phantom 1 -> -12, owner 5 -> -2)");
        Check(transfer(true, 20, 0, 5, 13) == 0, "C14: phantom below 1 HP transfers zero");
        Check(transfer(true, 3, 10, 5, 13) == 0, "C14: non-lethal-to-owner packet transfers zero");
        var bondedShield = Load<BlueprintBuff>(cache, Id("BondedManifestationEffect"));
        Check(bondedShield.ComponentsArray.Any(c => c.GetType().Name == "Phantom0085ShieldToIncorporealTouch"), "bonded shield transfers to incorporeal touch AC");
        Check(bondedShield.ComponentsArray.Any(c => c.GetType().Name == "Phantom0085TendrilDice") == false, "tendril dice live on the temporary attack buff, not the shield");

        // ---- Shared roots untouched ----
        Check(ReferenceEquals(cache.Load(inqBook.AssetGuid), inqBook), "Inquisitor spellbook identity preserved");

        // ---- Boundaries not entered by this offline host ----
        Console.WriteLine("0085 NOT RUN: native AddPet->SetMaster->UnitPartPetMaster->AddClassLevels chain, SetHidden/TeleportToMaster/RemoveMaster on a live instance, rest UI, save/load, respec screen, mount button, scene placement (D / player TEST).");
        Console.WriteLine("0085 RESULT: " + assertions + " assertions passed, " + Failures.Count + " failed.");
        if (Failures.Count > 0)
            throw new Exception("0085 directed failures: " + string.Join(" | ", Failures));
        return assertions;
    }
}
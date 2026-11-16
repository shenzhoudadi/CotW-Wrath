using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Prerequisites;
using Kingmaker.Blueprints.Classes.Selection;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.Blueprints.Facts;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.Blueprints.Root;
using Kingmaker.Designers.Mechanics.Facts;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Localization;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.FactLogic;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Mechanics.Components;
using Kingmaker.UnitLogic.Mechanics.Properties;
using Kingmaker.Enums;
using static CallOfTheWild.Wrath.Prototype.Occultist0099Abjuration;
using static CallOfTheWild.Wrath.Prototype.PsychicAssets;

namespace CallOfTheWild.Wrath.Prototype
{
    internal static class Occultist0099NativeRegistration
    {
        internal const string AlchemistClass = "0937bec61c0dabc468428f496580c721";
        internal const string Proficiencies = "c5e479367d07d62428f2fe92f39c0341";

        private sealed class Graph
        {
            internal BlueprintRoot Root;
            internal BlueprintCharacterClass Character;
            internal BlueprintScriptableObject[] Assets;
            internal BlueprintSpellList List;
            internal Dictionary<BlueprintSpellList, string> ImplementLists;
        }

        private static readonly ConditionalWeakTable<BlueprintsCache, Graph> Published = new ConditionalWeakTable<BlueprintsCache, Graph>();
        private static readonly string[] Schools = { Occultist0099Ids.SliceSchool, Occultist0099Enchantment.School,
            Occultist0099Divination.School, Occultist0099Illusion.School };
        private static readonly int[] ImplementLevels = { 1, 1, 2, 6, 10, 14, 18 };
        private static readonly Occultist0099SpellRoot[] Rows = Occultist0099SpellCatalog.Roots
            .Where(row => row.IsKnack || Schools.Contains(row.School)).ToArray();

        private static string SlotKey(int index) => "slot-" + (index + 1);
        private static string SelectionToken(int index) => index == 0 ? "ImplementSelection" :
            index == 1 ? "ExtraImplementSelection" : SlotKey(index) + "-ImplementSelection";
        private static string ChoiceToken(int index, string school, bool extra)
            => index == 1 && school == Occultist0099Ids.SliceSchool && extra ? "AbjurationExtraSpells" :
                SlotKey(index) + "-" + school + (extra ? "-ExtraSpells" : "-Implement");
        private static string ListToken(int index, string school, bool extra)
            => SlotKey(index) + "-" + school + (extra ? "-ExtraLearnList" : "-LearnList");

        internal static string[] IdentityTokens()
        {
            var ids = new HashSet<string>(Occultist0099Ids.SliceTokens)
            {
                "EnchantmentEffectiveLevel", "EnchantmentDCLevel", "GloriousPresenceValue", "GloriousPresenceBuff",
                "CloudMindAbility", "BindingPatternAbility", "InspiredAssaultBuff", "InspiredAssaultAbility", "ObeyAbility",
                "ObeyHaltAbility", "ObeyFallAbility", "ObeyRunAbility", "EnchantmentInvestFocusAbility", "EnchantmentReplenishFocusAbility",
                "EnchantmentImplementFocus", "BindingPatternPower", "InspiredAssaultPower", "ObeyPower", "FocusPowerSelection", "MagicItemSkill",
                "AbjurationEffectiveLevel", "AbjurationDCLevel", "EnergyShieldBuff", "EnergyShieldAbility", "EnergyShieldPower",
                "UnravelingAbility", "UnravelingPower", "GlobeOfNegationBuff", "GlobeOfNegationAbility", "GlobeOfNegationPower"
            };
            ids.UnionWith(Occultist0099Aegis.Tokens());
            ids.UnionWith(Occultist0099Divination.Tokens);
            ids.UnionWith(Occultist0099Illusion.Tokens);
            ids.UnionWith(Occultist0099ShadowBeasts.Tokens());
            foreach (var school in Schools) ids.Add(school + "ExtraSpellSource");
            for (int index = 0; index < ImplementLevels.Length; index++)
            {
                ids.Add(SelectionToken(index));
                foreach (var school in Schools)
                    foreach (bool extra in index == 0 ? new[] { false } : new[] { false, true })
                    {
                        ids.Add(ChoiceToken(index, school, extra));
                        ids.Add(ListToken(index, school, extra));
                    }
            }
            return ids.ToArray();
        }

        // The normal startup path stays closed until the complete set of
        // schools, panoplies and progression is built. A detached cache uses
        // the same transaction with the currently implemented school graph.
        internal static bool RegisterInDetachedCache(BlueprintsCache cache)
        {
            var processCache = ResourcesLibrary.BlueprintsCache;
            if (cache == null || ReferenceEquals(cache, processCache)) return false;
            var rootGuid = BlueprintGuid.Parse(PsychicClass.RootId);
            BlueprintRoot processRoot = null;
            if (processCache != null)
                lock (Get(processCache, "m_Lock"))
                {
                    var entry = ((IDictionary)Get(processCache, "m_LoadedBlueprints"))[rootGuid];
                    if (entry != null) processRoot = Get(entry, "Blueprint") as BlueprintRoot;
                }
            lock (Get(cache, "m_Lock"))
            {
                var root = cache.Load(rootGuid) as BlueprintRoot;
                if (root?.name != "BlueprintRoot" || root.AssetGuid != rootGuid || root.Progression == null ||
                    ReferenceEquals(root.Progression, processRoot?.Progression)) return false;
                var day = Occultist0099Donors.DailyTable(cache);
                var alchemist = cache.Load(BlueprintGuid.Parse(AlchemistClass)) as BlueprintCharacterClass;
                var proficiencies = cache.Load(BlueprintGuid.Parse(Proficiencies)) as BlueprintFeature;
                var spells = Occultist0099Donors.Resolve(cache, Rows);
                if (day == null || alchemist?.name != "AlchemistClass" || alchemist.AssetGuid != BlueprintGuid.Parse(AlchemistClass) ||
                    proficiencies?.AssetGuid != BlueprintGuid.Parse(Proficiencies) || spells == null) return false;
                if (!spells.Keys.Any(row => row.Guid == Occultist0099AbjurationCatalog.MageShield) ||
                    !spells.Keys.Any(row => row.Name == "SpellResistance")) return false;
                var previous = (BlueprintCharacterClassReference[])Get(root.Progression, "m_CharacterClasses");
                var loaded = (IDictionary)Get(cache, "m_LoadedBlueprints");
                if (Published.TryGetValue(cache, out var existing))
                {
                    if (existing.Assets.All(asset => !loaded.Contains(asset.AssetGuid)) &&
                        ClassReferences(previous, existing.Character.AssetGuid) == 0)
                        Published.Remove(cache);
                    else
                    {
                        Verify(cache, root, existing);
                        UpdateSpellLists(existing, spells);
                        return true;
                    }
                }
                var manifest = IdentityTokens().Select(token => BlueprintGuid.Parse(Occultist0099Ids.Id(token))).ToArray();
                if (ClassReferences(previous, BlueprintGuid.Parse(Occultist0099Ids.ClassGuid)) != 0 ||
                    manifest.Any(id => loaded.Contains(id))) return false;
                var enchantment = Occultist0099Enchantment.Build(cache, DetachedText);
                var abjuration = Occultist0099AbjurationPowers.Build(cache, DetachedText);
                var aegis = Occultist0099Aegis.Build(cache, DetachedText);
                var divination = Occultist0099Divination.Build(cache, DetachedText);
                var illusion = Occultist0099Illusion.Build(cache, DetachedText);
                if (enchantment == null || abjuration == null || aegis == null || divination == null || illusion == null) return false;
                BlueprintUnitProperty IllusionProperty(string token)
                    => illusion.OfType<BlueprintUnitProperty>().Single(property => property.AssetGuid == BlueprintGuid.Parse(Occultist0099Ids.Id(token)));
                var shadows = Occultist0099ShadowBeasts.Build(cache, IllusionProperty("IllusionEffectiveLevel"), IllusionProperty("IllusionDCLevel"), DetachedText);
                if (shadows == null) return false;
                var graph = BuildGraph(root, alchemist, day, proficiencies, spells,
                    enchantment.Concat(abjuration).Concat(aegis).Concat(divination).Concat(illusion).Concat(shadows).ToArray());
                var ids = new HashSet<BlueprintGuid>(graph.Assets.Select(asset => asset.AssetGuid));
                if (ids.Count != graph.Assets.Length || !ids.SetEquals(manifest))
                    throw new InvalidOperationException("Occultist development graph differs from its private identity manifest.");
                Publish(cache, graph, previous, loaded);
                return true;
            }
        }

        private static int ClassReferences(BlueprintCharacterClassReference[] list, BlueprintGuid guid)
            => list?.Count(entry => entry?.Guid == guid) ?? 0;

        private static void Verify(BlueprintsCache cache, BlueprintRoot root, Graph graph)
        {
            var refs = (BlueprintCharacterClassReference[])Get(root.Progression, "m_CharacterClasses");
            if (root != graph.Root || ClassReferences(refs, graph.Character.AssetGuid) != 1 ||
                !ReferenceEquals(refs.Single(r => r?.Guid == graph.Character.AssetGuid).Get(), graph.Character) ||
                graph.Assets.Any(asset => !ReferenceEquals(cache.Load(asset.AssetGuid), asset)))
                throw new InvalidOperationException("Occultist cache ownership or class list changed.");
        }

        private static void Publish(BlueprintsCache cache, Graph graph, BlueprintCharacterClassReference[] previous, IDictionary loaded)
        {
            var progression = graph.Root.Progression;
            var next = (previous ?? Array.Empty<BlueprintCharacterClassReference>())
                .Concat(new[] { Ref<BlueprintCharacterClassReference>(graph.Character) }).ToArray();
            try
            {
                foreach (var asset in graph.Assets)
                {
                    Set(asset, "m_AllElements", SpellBatch01447.Elements(asset.ComponentsArray, new HashSet<object>(IdentityComparer)).ToList());
                    asset.OnEnable();
                    if (loaded.Contains(asset.AssetGuid))
                        throw new InvalidOperationException("Occultist identity was occupied during registration: " + asset.name);
                    cache.AddCachedBlueprint(asset.AssetGuid, asset);
                }
                if (!ReferenceEquals(Get(progression, "m_CharacterClasses"), previous))
                    throw new InvalidOperationException("The native class list changed during Occultist registration.");
                Set(progression, "m_CharacterClasses", next);
                Verify(cache, graph.Root, graph);
                Published.Add(cache, graph);
            }
            catch
            {
                if (ReferenceEquals(Get(progression, "m_CharacterClasses"), next)) Set(progression, "m_CharacterClasses", previous);
                foreach (var asset in graph.Assets)
                    if (ReferenceEquals(cache.Load(asset.AssetGuid), asset)) cache.RemoveCachedBlueprint(asset.AssetGuid);
                throw;
            }
        }

        private static Graph BuildGraph(BlueprintRoot root, BlueprintCharacterClass alchemist, BlueprintSpellsTable day,
            BlueprintFeature proficiencies, Dictionary<Occultist0099SpellRoot, BlueprintAbility> spells, BlueprintScriptableObject[] additionalPowers)
        {
            var shield = spells.Single(row => row.Key.Guid == Occultist0099AbjurationCatalog.MageShield).Value;
            var resistance = spells.Single(row => row.Key.Name == "SpellResistance").Value;
            var mechanics = Build(shield, resistance, DetachedText).Concat(additionalPowers).ToArray();
            T Mechanic<T>(string token) where T : BlueprintScriptableObject
                => (T)mechanics.Single(asset => asset.AssetGuid == BlueprintGuid.Parse(Occultist0099Ids.Id(token)));

            var character = Identify(new BlueprintCharacterClass
            {
                LocalizedName = DetachedText("Occultist0099Class", "name", "Occultist"),
                LocalizedDescription = DetachedText("Occultist0099Class", "description", "An occultist channels psychic magic through implements."),
                LocalizedDescriptionShort = DetachedText("Occultist0099Class", "short", "A psychic spellcaster who focuses magic through implements."),
                SkillPoints = alchemist.SkillPoints, PrimaryColor = alchemist.PrimaryColor, SecondaryColor = alchemist.SecondaryColor,
                MaleEquipmentEntities = alchemist.MaleEquipmentEntities, FemaleEquipmentEntities = alchemist.FemaleEquipmentEntities,
                ClassSkills = new[] { StatType.SkillStealth, StatType.SkillThievery, StatType.SkillKnowledgeArcana,
                    StatType.SkillKnowledgeWorld, StatType.SkillLoreReligion, StatType.SkillPerception, StatType.SkillPersuasion, StatType.SkillUseMagicDevice },
                RecommendedAttributes = new[] { StatType.Intelligence }, ComponentsArray = Array.Empty<BlueprintComponent>()
            }, "OccultistClass", "Occultist", "", null);
            foreach (var field in new[] { "HitDie", "m_BaseAttackBonus", "m_FortitudeSave", "m_EquipmentEntities", "m_Icon" })
                Set(character, field, Get(alchemist, field));
            Set(character, "m_ReflexSave", Get(alchemist, "m_WillSave"));
            Set(character, "m_WillSave", Get(alchemist, "m_FortitudeSave"));
            Set(character, "m_Archetypes", Array.Empty<BlueprintArchetypeReference>());

            var list = EmptySpellList("OccultistSpellList");
            var implementLists = new Dictionary<BlueprintSpellList, string>();
            var descriptors = new List<Occultist0099ImplementSpellList>();
            var choices = new List<BlueprintFeature>();
            var selections = new List<BlueprintFeatureSelection>();
            var extraMarkers = Schools.ToDictionary(school => school, school => Feature(school + "ExtraSpellSource", school + " Extra Spells"));
            foreach (var marker in extraMarkers.Values)
            {
                marker.HideInUI = true;
                marker.HideInCharacterSheetAndLevelUp = true;
            }
            for (int i = 0; i < ImplementLevels.Length; i++)
            {
                var options = new List<BlueprintFeature>();
                foreach (string school in Schools)
                {
                    var first = Mechanic<BlueprintFeature>(school + "ImplementFocus");
                    var extraMarker = extraMarkers[school];
                    foreach (bool extra in i == 0 ? new[] { false } : new[] { false, true })
                    {
                        var source = EmptySpellList(ListToken(i, school, extra));
                        implementLists.Add(source, school);
                        descriptors.Add(new Occultist0099ImplementSpellList
                            { SlotKey = SlotKey(i), School = school, ExtraSpells = extra, List = Ref<BlueprintSpellListReference>(source) });
                        var requirement = extra ? new BlueprintComponent[] { Requires(first), Excludes(extraMarker), Facts(extraMarker) }
                            : new BlueprintComponent[] { Excludes(first), Facts(first) };
                        var slot = new Occultist0099ImplementSlot
                            { name = "OccultistImplementSlot", SlotKey = SlotKey(i), School = school, ExtraSpells = extra };
                        options.Add(Feature(ChoiceToken(i, school, extra), school + (extra ? " Extra Spells" : " Implement"),
                            requirement.Concat(new[] { slot }).ToArray()));
                    }
                }
                choices.AddRange(options);
                selections.Add(Selection(SelectionToken(i), "Implement", "Choose an occultist implement.", options));
            }
            var zero = Identify(new BlueprintSpellsTable
            {
                Levels = Enumerable.Range(0, 21).Select(_ => new SpellsLevelEntry { Count = new int[11] }).ToArray(),
                ComponentsArray = Array.Empty<BlueprintComponent>()
            }, "ZeroSpellsKnown", "", "", null);
            var book = Identify(new BlueprintSpellbook
            {
                Name = character.LocalizedName, Spontaneous = true, IsArcane = false,
                CastingAttribute = StatType.Intelligence, CantripsType = CantripsType.Cantrips,
                ComponentsArray = new BlueprintComponent[]
                {
                    new Occultist0099SpellbookMarker0099 { name = "OccultistPrivateBook", ImplementLists = descriptors.ToArray() },
                    new PsychicSpellbookMarker { name = "OccultistPsychicThoughtEmotionComponents", MaximumSpellLevel = 6 }
                }
            }, "OccultistSpellbook", "", "", null);
            Set(book, "m_CharacterClass", Ref<BlueprintCharacterClassReference>(character));
            Set(book, "m_SpellList", Ref<BlueprintSpellListReference>(list));
            Set(book, "m_SpellsPerDay", Ref<BlueprintSpellsTableReference>(day));
            Set(book, "m_SpellsKnown", Ref<BlueprintSpellsTableReference>(zero));
            Set(character, "m_Spellbook", Ref<BlueprintSpellbookReference>(book));

            var resource = Identify(new BlueprintAbilityResource { ComponentsArray = Array.Empty<BlueprintComponent>() }, "MentalFocusResource", "", "", null);
            var grant = new AddAbilityResources { name = "OccultistMentalFocusMirror", RestoreAmount = false };
            Set(grant, "m_Resource", Ref<BlueprintAbilityResourceReference>(resource));
            var focus = Feature("MentalFocusFact", "Mental Focus", grant, new Occultist0099FocusFact0099
            {
                name = "OccultistFocusAuthority", CharacterClass = Ref<BlueprintCharacterClassReference>(character),
                FocusResource = Ref<BlueprintAbilityResourceReference>(resource),
                MindBarrierBuff = Ref<BlueprintBuffReference>(Mechanic<BlueprintBuff>("MindBarrierBuff")),
                AbjurationEffects = new[] { Ref<BlueprintBuffReference>(Mechanic<BlueprintBuff>("EnergyShieldBuff")),
                    Ref<BlueprintBuffReference>(Mechanic<BlueprintBuff>("GlobeOfNegationBuff")),
                    Ref<BlueprintBuffReference>(Mechanic<BlueprintBuff>("AegisArmorBuff")) },
                DivinationEffects = new[] { "SuddenInsightBuff", "DangerSightBuff", "ProphecyBuff" }
                    .Select(token => Ref<BlueprintBuffReference>(Mechanic<BlueprintBuff>(token))).ToArray(),
                IllusionAura = Ref<BlueprintBuffReference>(Mechanic<BlueprintBuff>("BedevelingAuraBuff")),
                WardingTalisman = Ref<BlueprintBuffReference>(Mechanic<BlueprintBuff>("WardingTalismanBuff")),
                SchoolResonances = new[] {
                    new Occultist0099SchoolResonance { School = Occultist0099Enchantment.School,
                        Buff = Ref<BlueprintBuffReference>(Mechanic<BlueprintBuff>("GloriousPresenceBuff")) },
                    new Occultist0099SchoolResonance { School = Occultist0099Illusion.School,
                        Buff = Ref<BlueprintBuffReference>(Mechanic<BlueprintBuff>("DistortionBuff")) }
                }.Concat(
                    new[] { "ThirdEyeBuff", "ThirdEyeSeeInvisibility", "ThirdEyeBlindsense", "ThirdEyeBlindsight" }
                        .Select((token, tier) => new Occultist0099SchoolResonance { School = Occultist0099Divination.School,
                            Buff = Ref<BlueprintBuffReference>(Mechanic<BlueprintBuff>(token)), Tier = tier })).ToArray()
            }, Facts(Mechanic<BlueprintAbility>("ResetFocusAbility"), Mechanic<BlueprintAbility>("LockFocusAbility")));
            var learning = Feature("OccultistSpellbookFact", "Implement Spells", new Occultist0099SpellbookFact0099
                { name = "OccultistSpellSourceAuthority", CharacterClass = Ref<BlueprintCharacterClassReference>(character) });
            var enchantmentImplement = Mechanic<BlueprintFeature>("EnchantmentImplementFocus");
            var abjurationImplement = Mechanic<BlueprintFeature>("AbjurationImplementFocus");
            var divinationImplement = Mechanic<BlueprintFeature>("DivinationImplementFocus");
            var illusionImplement = Mechanic<BlueprintFeature>("IllusionImplementFocus");
            var powers = new[]
            {
                Power("BindingPattern", "Binding Pattern", enchantmentImplement, 7),
                Power("InspiredAssault", "Inspired Assault", enchantmentImplement),
                Power("Obey", "Obey", enchantmentImplement),
                Feature("AegisPower", "Aegis", Requires(abjurationImplement),
                    GrantPower(abjurationImplement, Mechanic<BlueprintFeature>("AegisOptions"))),
                Power("EnergyShield", "Energy Shield", abjurationImplement, 3),
                Power("Unraveling", "Unraveling", abjurationImplement, 5),
                Power("GlobeOfNegation", "Globe of Negation", abjurationImplement, 11),
                Power("DivinersFortune", "Diviner's Fortune", divinationImplement),
                Power("DangerSight", "Danger Sight", divinationImplement, 3),
                Power("MindEye", "Mind Eye", divinationImplement, 5),
                Power("Prophecy", "In Accordance with the Prophecy", divinationImplement, 9),
                Power("Terror", "Terror", illusionImplement),
                Power("Unseen", "Unseen", illusionImplement, 7),
                Power("BedevelingAura", "Bedeveling Aura", illusionImplement, 9),
                Power("ShadowBeasts", "Shadow Beasts", illusionImplement, 9)
            };
            BlueprintFeature Power(string token, string label, BlueprintFeature implement, int minimum = 0)
            {
                var components = new List<BlueprintComponent> { Requires(implement), GrantPower(implement, Mechanic<BlueprintUnitFact>(token + "Ability")) };
                if (minimum > 0)
                {
                    var prerequisite = new PrerequisiteClassLevel { name = "Occultist" + token + "Level", Level = minimum };
                    Set(prerequisite, "m_CharacterClass", Ref<BlueprintCharacterClassReference>(character));
                    components.Add(prerequisite);
                }
                return Feature(token + "Power", label, components.ToArray());
            }
            var powerSelection = Selection("FocusPowerSelection", "Focus Power", "Choose a focus power for one of your implements.", powers);
            var halfLevel = new ContextRankConfig { name = "OccultistHalfClassLevel" };
            SetEnum(halfLevel, "m_BaseValueType", "ClassLevel");
            SetEnum(halfLevel, "m_Progression", "Div2");
            Set(halfLevel, "m_Class", new[] { Ref<BlueprintCharacterClassReference>(character) });
            var itemSkill = Feature("MagicItemSkill", "Magic Item Skill", halfLevel,
                new AddContextStatBonus { name = "OccultistUseMagicDevice", Descriptor = ModifierDescriptor.UntypedStackable,
                    Stat = StatType.SkillUseMagicDevice, Value = new ContextValue { ValueType = ContextValueType.Rank } });
            itemSkill.ReapplyOnLevelUp = true;
            var knacks = Feature("OccultistKnacksFeature", "Knacks");
            var career = new List<BlueprintFeature>[21];
            for (int i = 1; i < career.Length; i++) career[i] = new List<BlueprintFeature>();
            career[1].AddRange(new[] { proficiencies, focus, learning, knacks });
            career[2].Add(itemSkill);
            for (int i = 0; i < selections.Count; i++) career[ImplementLevels[i]].Add(selections[i]);
            for (int i = 1; i <= 19; i += 2) career[i].Add(powerSelection);
            var progression = Identify(new BlueprintProgression
            {
                Ranks = 1, IsClassFeature = true, ComponentsArray = Array.Empty<BlueprintComponent>(), UIGroups = Array.Empty<UIGroup>(),
                LevelEntries = Enumerable.Range(1, 20).Where(level => career[level].Count > 0)
                    .Select(level => ArchetypeBatch01445.Entry(level, career[level].ToArray())).ToArray()
            }, "OccultistProgression", "Occultist", "", null, DetachedText);
            Set(progression, "m_Classes", new[] { new BlueprintProgression.ClassWithLevel { m_Class = Ref<BlueprintCharacterClassReference>(character) } });
            Set(character, "m_Progression", Ref<BlueprintProgressionReference>(progression));

            var graph = new Graph { Root = root, Character = character, List = list, ImplementLists = implementLists,
                Assets = new BlueprintScriptableObject[] { character, book, list, zero, resource, focus,
                    learning, knacks, progression, powerSelection, itemSkill }.Concat(mechanics).Concat(implementLists.Keys)
                    .Concat(extraMarkers.Values).Concat(choices).Concat(selections).Concat(powers).ToArray() };
            UpdateSpellLists(graph, spells);
            return graph;
        }

        private static BlueprintFeature Feature(string token, string label, params BlueprintComponent[] components)
            => Identify(new BlueprintFeature { Ranks = 1, IsClassFeature = true, ComponentsArray = components },
                token, label, "", null, DetachedText);

        private static BlueprintSpellList EmptySpellList(string token)
            => Identify(new BlueprintSpellList { ComponentsArray = Array.Empty<BlueprintComponent>() }, token, "", "", null);

        private static PrerequisiteFeature Requires(BlueprintFeature feature)
        {
            var requirement = new PrerequisiteFeature { name = "Requires" + feature.name };
            Set(requirement, "m_Feature", Ref<BlueprintFeatureReference>(feature));
            return requirement;
        }

        private static PrerequisiteNoFeature Excludes(BlueprintFeature feature)
        {
            var requirement = new PrerequisiteNoFeature { name = "Excludes" + feature.name };
            Set(requirement, "m_Feature", Ref<BlueprintFeatureReference>(feature));
            return requirement;
        }

        private static Occultist0099SchoolPowerGrant GrantPower(BlueprintFeature implement, BlueprintUnitFact power)
        {
            var grant = new Occultist0099SchoolPowerGrant { name = "OccultistRequires" + implement.name };
            Set(grant, "m_CheckedFact", Ref<BlueprintUnitFactReference>(implement));
            Set(grant, "m_Feature", Ref<BlueprintUnitFactReference>(power));
            return grant;
        }

        // A detached graph can carry localization keys without writing to the process pack.
        private static LocalizedString DetachedText(string token, string suffix, string _)
            => new LocalizedString { Key = "cotw-wrath.psychic.full." + token + "." + suffix };

        private static BlueprintFeatureSelection Selection(string token, string label, string description, IEnumerable<BlueprintFeature> features)
        {
            var selection = Identify(new BlueprintFeatureSelection { Ranks = 1, IsClassFeature = true,
                ComponentsArray = Array.Empty<BlueprintComponent>() }, token, label, description, null, DetachedText);
            var options = features.Select(Ref<BlueprintFeatureReference>).ToArray();
            Set(selection, "m_Features", options);
            Set(selection, "m_AllFeatures", options);
            return selection;
        }

        private static void UpdateSpellLists(Graph graph, Dictionary<Occultist0099SpellRoot, BlueprintAbility> spells)
        {
            var lists = new[] { graph.List }.Concat(graph.ImplementLists.Keys).ToArray();
            var next = lists.Select(list => SpellListCompatibility.Build(Enumerable.Range(0, 7).Select(circle =>
            {
                var row = new SpellLevelList(circle);
                foreach (var spell in spells.Where(s => s.Key.Circle == circle &&
                             (list == graph.List || !s.Key.IsKnack && s.Key.School == graph.ImplementLists[list])))
                    row.SpellsRefs.Add(Ref<BlueprintAbilityReference>(spell.Value));
                return row;
            }).ToArray())).ToArray();
            for (int i = 0; i < lists.Length; i++)
            {
                var existing = lists[i].SpellsByLevel;
                if (existing?.Length == next[i].Length && existing.Zip(next[i], (before, after) =>
                        before?.SpellLevel == after.SpellLevel && before.SpellsRefs.Select(reference => reference?.Get())
                            .SequenceEqual(after.SpellsRefs.Select(reference => reference.Get()))).All(same => same)) continue;
                lists[i].SpellsByLevel = next[i];
                Set(lists[i], "m_MaxLevel", 0);
            }
        }
    }
}

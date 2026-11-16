// Ultimate Magus (奥法追随者) prestige class for Pathfinder: Wrath of the Righteous.
using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Prerequisites;
using Kingmaker.Blueprints.Classes.Selection;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.Blueprints.Facts;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.Blueprints.Root;
using Kingmaker.Designers.Mechanics.Facts;
using Kingmaker.ElementsSystem;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Enums;
using Kingmaker.PubSubSystem;
using Kingmaker.RuleSystem;
using Kingmaker.RuleSystem.Rules.Abilities;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components;
using Kingmaker.UnitLogic.Abilities.Components.Base;
using Kingmaker.UnitLogic.Buffs;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Buffs.Components;
using Kingmaker.UnitLogic.Class.LevelUp;
using Kingmaker.UnitLogic.Commands.Base;
using Kingmaker.UnitLogic.FactLogic;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Mechanics.Actions;
using Newtonsoft.Json;
using static CallOfTheWild.Wrath.Prototype.PsychicAssets;

namespace CallOfTheWild.Wrath.Prototype
{
    internal static class UltimateMagus
    {
        internal const string ModuleName = "Ultimate Magus";
        internal const string WizardClassId = "ba34257984f4c41408ce1dc2004e342e";
        internal const string SorcererClassId = "b3a505fb61437dc4097f43c3f8f9a4cf";
        internal const string ArcanistClassId = "52dbfd8505e22f84fad8d702611f60b7";
        internal const string RootId = "2d77316c72b9ed44f888ceefc2a131f6";

        internal const string ClassDesc =
            "术士引导不可知的力量，而法师是远古知识的专家，但是极少有人能像奥法追随者一样彻底地了解和掌控奥术的能量。在将术士和法师力量结合后，奥法追随者使自己的力量达到了最伟大的大法师也难以达到的境界。";
        internal const string ClassShortDesc =
            "将准备奥术与自发奥术融会贯通的进阶职业。";

        private static string T(string token) => "UltimateMagus" + token;

        internal static BlueprintCharacterClass Class { get; private set; }
        internal static BlueprintFeatureSelection PreparedSelection { get; private set; }
        internal static BlueprintFeatureSelection SpontaneousSelection { get; private set; }
        internal static BlueprintFeatureSelection BonusMetamagicSelection { get; private set; }
        internal static BlueprintFeatureSelection LowerBookSelection { get; private set; }
        internal static BlueprintFeature AdvancePreparedOption { get; private set; }
        internal static BlueprintFeature AdvanceSpontaneousOption { get; private set; }
        internal static BlueprintFeature AugmentedCastingFeature { get; private set; }
        internal static BlueprintAbilityResource AugmentedCastingResource { get; private set; }

        private static BlueprintsCache Cache;
        private static bool Reconciling;
        private static bool Pending = true;
        private static int SelectionOpens;

        private static readonly Dictionary<BlueprintGuid, BlueprintFeature> PreparedBookOptions = new Dictionary<BlueprintGuid, BlueprintFeature>();
        private static readonly Dictionary<BlueprintGuid, BlueprintFeature> SpontaneousBookOptions = new Dictionary<BlueprintGuid, BlueprintFeature>();
        private static readonly Dictionary<Metamagic, BlueprintFeature> MetamagicFeatOptions = new Dictionary<Metamagic, BlueprintFeature>();
        private static readonly Dictionary<Metamagic, AugmentedEntry> AugmentedEntries = new Dictionary<Metamagic, AugmentedEntry>();
        private static readonly HashSet<BlueprintGuid> AugmentedChildGuids = new HashSet<BlueprintGuid>();
        private static readonly HashSet<BlueprintGuid> AugmentedBuffGuids = new HashSet<BlueprintGuid>();

        internal sealed class AugmentedEntry
        {
            internal Metamagic Metamagic;
            internal int Cost;
            internal BlueprintAbility ChildAbility;
            internal BlueprintBuff ToPreparedBuff;
            internal BlueprintBuff ToSpontaneousBuff;
        }

        private static readonly (Metamagic Magic, string FeatId, string Name)[] KnownNativeMetamagics =
        {
            (Metamagic.Extend, "f180e72e4a9cbaa4da8be9bc958132ef", "延时法术"),
            (Metamagic.Reach, "46fad72f54a33dc4692d3b62eca7bb78", "法术延远"),
            (Metamagic.Selective, "85f3340093d144dd944fff9a9adfd2f2", "选择法术"),
            (Metamagic.Bolstered, "fbf5d9ce931f47f3a0c818b3f8ef8414", "法术强固"),
            (Metamagic.Piercing, "c101ad6879a94204a780506f7a554865", "穿透法术"),
            (Metamagic.Intensified, "8ad7fd39abea4722b39eb5a67d606a41", "法术聚能"),
            (Metamagic.Empower, "a1de1e4f92195b442adb946f0e2b9d4e", "法术强效"),
            (Metamagic.Persistent, "cd26b9fa3f734461a0fcedc81cafaaac", "法术持久"),
            (Metamagic.Maximize, "7f2b282626862e345935bbea5e66424b", "法术极效"),
            (Metamagic.Quicken, "ef7ece7bb5bb66a41b256976b27f424e", "法术瞬发"),
        };

        private static TN Native<TN>(BlueprintsCache cache, string id) where TN : SimpleBlueprint
            => cache.Load(BlueprintGuid.Parse(id)) as TN
               ?? throw new InvalidOperationException("Ultimate Magus native dependency missing: " + id);

        internal static void Register(BlueprintsCache cache)
        {
            Cache = cache;
            if (cache.Load(Id(T("ClassAsset"))) is BlueprintCharacterClass existingClass)
            {
                Class = existingClass;
                Reconcile(cache);
                return;
            }

            var f = new PsychicAssets(cache);
            var wizard = Native<BlueprintCharacterClass>(cache, WizardClassId);
            var arcanist = cache.Load(BlueprintGuid.Parse(ArcanistClassId)) as BlueprintCharacterClass;
            var root = Native<BlueprintRoot>(cache, RootId);
            if (root.Progression == null)
                throw new InvalidOperationException("Ultimate Magus native root progression missing.");

            var prereqs = new List<BlueprintComponent>
            {
                new PrerequisiteStatValue { Stat = StatType.SkillKnowledgeArcana, Value = 5, Group = Prerequisite.GroupType.All },
                new UltimateMagusMetamagicPrerequisite { name = "UltimateMagusMetamagicPrereq", Group = Prerequisite.GroupType.All },
                new UltimateMagusSpellcastingPrerequisite { name = "UltimateMagusSpellcastingPrereq", Group = Prerequisite.GroupType.All }
            };
            if (arcanist != null)
            {
                var noArcanist = new PrerequisiteNoClassLevel { Group = Prerequisite.GroupType.All };
                Set(noArcanist, "m_CharacterClass", Ref<BlueprintCharacterClassReference>(arcanist));
                prereqs.Add(noArcanist);
            }

            Class = f.Add(new BlueprintCharacterClass
            {
                LocalizedName = Text(T("Class"), "name", "奥法追随者"),
                LocalizedDescription = Text(T("Class"), "description", ClassDesc),
                LocalizedDescriptionShort = Text(T("Class"), "short", ClassShortDesc),
                SkillPoints = 2,
                PrestigeClass = true,
                HideInUI = false,
                ClassSkills = new[]
                {
                    StatType.SkillKnowledgeArcana,
                    StatType.SkillKnowledgeWorld,
                    StatType.SkillUseMagicDevice
                },
                IsArcaneCaster = true,
                IsDivineCaster = false,
                RecommendedAttributes = new[] { StatType.Intelligence, StatType.Charisma },
                NotRecommendedAttributes = Array.Empty<StatType>(),
                PrimaryColor = wizard.PrimaryColor,
                SecondaryColor = wizard.SecondaryColor,
                StartingGold = wizard.StartingGold,
                MaleEquipmentEntities = wizard.MaleEquipmentEntities,
                FemaleEquipmentEntities = wizard.FemaleEquipmentEntities,
                ComponentsArray = prereqs.ToArray()
            }, T("ClassAsset"), "", "");

            Set(Class, "HitDie", DiceType.D4);
            Set(Class, "m_BaseAttackBonus", Get(wizard, "m_BaseAttackBonus"));
            Set(Class, "m_FortitudeSave", Get(wizard, "m_FortitudeSave"));
            Set(Class, "m_ReflexSave", Get(wizard, "m_ReflexSave"));
            Set(Class, "m_WillSave", Get(wizard, "m_WillSave"));
            foreach (var field in new[] { "m_StartingItems", "m_EquipmentEntities", "m_Difficulty" })
                Set(Class, field, Get(wizard, field));
            Set(Class, "m_Archetypes", Array.Empty<BlueprintArchetypeReference>());
            Set(Class, "m_Spellbook", new BlueprintSpellbookReference());

            // 1st-level dual arcane spellbook pickers.
            PreparedSelection = f.Add(new BlueprintFeatureSelection { Ranks = 1, IsClassFeature = true }, T("PreparedBookSelection"),
                "选择准备奥术职业", "选择你已有的一个准备奥术施法职业（不含奥能师）。奥法追随者的施法进度将按职业列表推进该法术书。");
            Set(PreparedSelection, "m_AllFeatures", Array.Empty<BlueprintFeatureReference>());
            Set(PreparedSelection, "m_Features", Array.Empty<BlueprintFeatureReference>());

            SpontaneousSelection = f.Add(new BlueprintFeatureSelection { Ranks = 1, IsClassFeature = true }, T("SpontaneousBookSelection"),
                "选择自发奥术职业", "选择你已有的一个自发奥术施法职业（不含奥能师）。奥法追随者的施法进度将按职业列表推进该法术书。");
            Set(SpontaneousSelection, "m_AllFeatures", Array.Empty<BlueprintFeatureReference>());
            Set(SpontaneousSelection, "m_Features", Array.Empty<BlueprintFeatureReference>());

            // Lower-level advancement at levels 1, 4, 7.
            AdvancePreparedOption = f.Feature(T("AdvancePreparedOption"),
                "提升准备奥术施法等级",
                "使你选定的准备奥术施法职业的每日法术数量、可知法术与施法者等级视同提升 1 级（不获得该职业的其它能力）。",
                new UltimateMagusAdvanceBookComponent { IsSpontaneous = false },
                new UltimateMagusLowerBookPrerequisite { IsSpontaneous = false });
            AdvancePreparedOption.Ranks = 3;

            AdvanceSpontaneousOption = f.Feature(T("AdvanceSpontaneousOption"),
                "提升自发奥术施法等级",
                "使你选定的自发奥术施法职业的每日法术数量、可知法术与施法者等级视同提升 1 级（不获得该职业的其它能力）。",
                new UltimateMagusAdvanceBookComponent { IsSpontaneous = true },
                new UltimateMagusLowerBookPrerequisite { IsSpontaneous = true });
            AdvanceSpontaneousOption.Ranks = 3;

            LowerBookSelection = f.Add(new BlueprintFeatureSelection { Ranks = 3, IsClassFeature = true }, T("LowerBookAdvanceSelection"),
                "较低奥术职业施法等级提升",
                "在第 1、4、7 级时，你只获得当前有效施法等级较低的奥术职业的每日法术量与可知法术量增加；若两者施法等级相同，则可任选其一。");
            var lowerRefs = new[] { Ref<BlueprintFeatureReference>(AdvancePreparedOption), Ref<BlueprintFeatureReference>(AdvanceSpontaneousOption) };
            Set(LowerBookSelection, "m_AllFeatures", lowerRefs);
            Set(LowerBookSelection, "m_Features", lowerRefs);

            // Standard dual advancement at levels 2, 3, 5, 6, 8, 9, 10.
            var bothAdvance = f.Feature(T("BothBooksAdvance"),
                "双奥术施法等级提升",
                "你选定的准备奥术施法职业与自发奥术施法职业的每日法术数量、可知法术与施法者等级均视同提升 1 级（不获得其它职业能力）。",
                new UltimateMagusAdvanceBookComponent { IsSpontaneous = false },
                new UltimateMagusAdvanceBookComponent { IsSpontaneous = true });
            bothAdvance.Ranks = 7;

            // Arcane Spell Power at 1, 4, 7, 10 (+1 to +4 CL).
            var spellPower = f.Feature(T("ArcaneSpellPower"),
                "奥法之力",
                "1 级时，你所有的奥术职业施法者等级提高 1 级。在 4、7、10 级时这项能力将再次提升，10 级时最高达到 +4。",
                new UltimateMagusSpellPowerComponent());
            spellPower.Ranks = 4;

            // Expanded Spell Knowledge at 2, 4, 6, 8, 10 (1st..5th circle, bidirectional).
            var expandedKnowledge = f.Feature(T("ExpandedSpellKnowledge"),
                "奥术同源",
                "2 级起，你选定的准备奥术法术书与自发奥术法术书之间可以互通彼此已掌握的 1 环及以下法术（互不重复加入对方的可知法术列表）。在 4、6、8、10 级时，可互通的法术环级上限依次提升至 2、3、4、5 环。",
                new UltimateMagusExpandedKnowledgeComponent());
            expandedKnowledge.Ranks = 5;

            // Bonus Metamagic Feat at 5, 9.
            BonusMetamagicSelection = f.Add(new BlueprintFeatureSelection
            {
                Ranks = 2,
                IsClassFeature = false,
                Groups = new[] { FeatureGroup.Feat }
            }, T("BonusMetamagicSelection"),
                "奖励超魔专长",
                "在 5 级和 9 级时，你可以选择任意满足先决条件的超魔专长作为你的奖励专长。");
            Set(BonusMetamagicSelection, "m_AllFeatures", Array.Empty<BlueprintFeatureReference>());
            Set(BonusMetamagicSelection, "m_Features", Array.Empty<BlueprintFeatureReference>());

            // Augmented Casting at level 3 (3 + 1/2 UM level uses per day).
            AugmentedCastingResource = f.Add(new BlueprintAbilityResource(), T("AugmentedCastingResource"), "", "");
            var maxAmt = Get(AugmentedCastingResource, "m_MaxAmount");
            Set(maxAmt, "BaseValue", 3);
            Set(maxAmt, "IncreasedByLevelStartPlusDivStep", true);
            Set(maxAmt, "StartingLevel", 2);
            Set(maxAmt, "StartingIncrease", 1);
            Set(maxAmt, "LevelStep", 2);
            Set(maxAmt, "PerStepIncrease", 1);
            Set(maxAmt, "m_Class", Array.Empty<BlueprintCharacterClassReference>());
            Set(maxAmt, "m_Archetypes", Array.Empty<BlueprintArchetypeReference>());
            Set(maxAmt, "m_ClassDiv", new[] { Ref<BlueprintCharacterClassReference>(Class) });
            Set(maxAmt, "m_ArchetypesDiv", Array.Empty<BlueprintArchetypeReference>());
            Set(AugmentedCastingResource, "m_MaxAmount", maxAmt);

            BuildAugmentedCastingEntries(cache, f);

            var resourceGrant = new AddAbilityResources { RestoreAmount = true, RestoreOnLevelUp = true };
            Set(resourceGrant, "m_Resource", Ref<BlueprintAbilityResourceReference>(AugmentedCastingResource));
            AugmentedCastingFeature = f.Feature(T("AugmentedCasting"),
                "奥术同调",
                "3 级起，你可以在一个奥术职业的法术背后选择对应的已知超魔子能力，以自由动作牺牲该法术或法术位，使你另一个奥术职业下一次施放的法术（原环级不超过奥法追随者等级的 1/2，且本身未搭配任何超魔专长）获得该超魔专长的效果。\n牺牲的法术或法术位环级至少须等同于该超魔专长的法术等级调整（最低 1 环）。每日可使用次数为 3 + 1/2 奥法追随者等级。",
                resourceGrant);

            var iconDonor = cache.Load(BlueprintGuid.Parse(KnownNativeMetamagics[6].FeatId)) as BlueprintUnitFact;
            if (iconDonor?.Icon != null)
            {
                Set(AugmentedCastingFeature, "m_Icon", iconDonor.Icon);
                Set(spellPower, "m_Icon", iconDonor.Icon);
            }

            var entries = Enumerable.Range(1, 10).Select(lvl =>
            {
                var entry = new LevelEntry { Level = lvl };
                var list = new List<BlueprintFeatureBase>();
                if (lvl == 1) list.AddRange(new BlueprintFeatureBase[] { PreparedSelection, SpontaneousSelection, LowerBookSelection, spellPower });
                if (lvl == 2) list.AddRange(new BlueprintFeatureBase[] { bothAdvance, expandedKnowledge });
                if (lvl == 3) list.AddRange(new BlueprintFeatureBase[] { bothAdvance, AugmentedCastingFeature });
                if (lvl == 4) list.AddRange(new BlueprintFeatureBase[] { LowerBookSelection, spellPower, expandedKnowledge });
                if (lvl == 5) list.AddRange(new BlueprintFeatureBase[] { bothAdvance, BonusMetamagicSelection });
                if (lvl == 6) list.AddRange(new BlueprintFeatureBase[] { bothAdvance, expandedKnowledge });
                if (lvl == 7) list.AddRange(new BlueprintFeatureBase[] { LowerBookSelection, spellPower });
                if (lvl == 8) list.AddRange(new BlueprintFeatureBase[] { bothAdvance, expandedKnowledge });
                if (lvl == 9) list.AddRange(new BlueprintFeatureBase[] { bothAdvance, BonusMetamagicSelection });
                if (lvl == 10) list.AddRange(new BlueprintFeatureBase[] { bothAdvance, spellPower, expandedKnowledge });
                entry.SetFeatures(list.ToArray());
                return entry;
            }).ToArray();

            var progression = f.Add(new BlueprintProgression
            {
                Ranks = 1,
                LevelEntries = entries,
                UIGroups = Array.Empty<UIGroup>()
            }, T("Progression"), "", "");
            Set(progression, "m_Classes", new[] { new BlueprintProgression.ClassWithLevel { m_Class = Ref<BlueprintCharacterClassReference>(Class) } });
            Set(progression, "m_UIDeterminatorsGroup", new BlueprintFeatureBaseReference[]
            {
                Ref<BlueprintFeatureBaseReference>(PreparedSelection),
                Ref<BlueprintFeatureBaseReference>(SpontaneousSelection),
                Ref<BlueprintFeatureBaseReference>(AugmentedCastingFeature)
            });
            Set(Class, "m_Progression", Ref<BlueprintProgressionReference>(progression));

            var assets = f.Assets.Values.ToArray();
            foreach (var bp in assets) PsychicRepairComponents.UniqueNames(bp);
            f.Enable();

            var existing = assets.Select(bp => cache.Load(bp.AssetGuid)).ToArray();
            for (int i = 0; i < assets.Length; i++)
                if (existing[i] != null && existing[i].GetType() != assets[i].GetType())
                    throw new InvalidOperationException("Ultimate Magus GUID belongs to unexpected content: " + assets[i].AssetGuid);
            if (existing.Any(bp => bp != null) && existing.Any(bp => bp == null))
                throw new InvalidOperationException("Ultimate Magus registration is only partially present.");

            var previousClasses = (BlueprintCharacterClassReference[])Get(root.Progression, "m_CharacterClasses") ?? Array.Empty<BlueprintCharacterClassReference>();
            var added = new List<SimpleBlueprint>();
            try
            {
                for (int i = 0; i < assets.Length; i++)
                    if (existing[i] == null) { cache.AddCachedBlueprint(assets[i].AssetGuid, assets[i]); added.Add(assets[i]); }
                if (!previousClasses.Any(r => r?.Guid == Class.AssetGuid))
                    Set(root.Progression, "m_CharacterClasses", previousClasses.Concat(new[] { Ref<BlueprintCharacterClassReference>(Class) }).ToArray());
            }
            catch
            {
                Set(root.Progression, "m_CharacterClasses", previousClasses);
                foreach (var bp in added) cache.RemoveCachedBlueprint(bp.AssetGuid);
                throw;
            }

            Reconcile(cache);
        }

        private static void BuildAugmentedCastingEntries(BlueprintsCache cache, PsychicAssets f)
        {
            foreach (var (magic, featId, fallbackName) in KnownNativeMetamagics)
            {
                var feat = cache.Load(BlueprintGuid.Parse(featId)) as BlueprintFeature;
                EnsureAugmentedEntry(cache, f, magic, feat, fallbackName);
            }
        }

        private static AugmentedEntry EnsureAugmentedEntry(BlueprintsCache cache, PsychicAssets f, Metamagic magic, BlueprintFeature sourceFeat, string fallbackName = null)
        {
            if (AugmentedEntries.TryGetValue(magic, out var existing)) return existing;
            int increase;
            try { increase = PsychicCompletionAmplifications01458.MetaCost(magic); }
            catch { return null; }
            if (increase <= 0 || magic == Metamagic.Heighten || magic == Metamagic.CompletelyNormal) return null;

            string featName = !string.IsNullOrWhiteSpace(sourceFeat?.Name) ? sourceFeat.Name : (fallbackName ?? magic.ToString());
            string featDesc = !string.IsNullOrWhiteSpace(sourceFeat?.Description)
                ? sourceFeat.Description
                : $"以奥术同调牺牲一个不低于 {increase} 环的法术或法术位，使另一个奥术职业的下一次施法获得{featName}效果。";

            var toPrepBuff = f.Buff(T("AugBuffPrep_" + magic),
                $"奥术同调（准备）：{featName}",
                $"你另一个奥术职业下一次施放的准备奥术（原环级不超过奥法追随者等级的 1/2，且本身未搭配超魔）将免费获得{featName}效果。",
                new UltimateMagusAugmentedBuffComponent { Metamagic = magic, TargetSpontaneous = false });
            SetEnum(toPrepBuff, "m_Flags", "RemoveOnRest");

            var toSpontBuff = f.Buff(T("AugBuffSpont_" + magic),
                $"奥术同调（自发）：{featName}",
                $"你另一个奥术职业下一次施放的自发奥术（原环级不超过奥法追随者等级的 1/2，且本身未搭配超魔）将免费获得{featName}效果。",
                new UltimateMagusAugmentedBuffComponent { Metamagic = magic, TargetSpontaneous = true });
            SetEnum(toSpontBuff, "m_Flags", "RemoveOnRest");

            var resLogic = new AbilityResourceLogic
            {
                Amount = 1
            };
            Set(resLogic, "m_IsSpendResource", true);
            Set(resLogic, "m_RequiredResource", Ref<BlueprintAbilityResourceReference>(AugmentedCastingResource));

            var child = f.Ability(T("AugChild_" + magic),
                $"奥术同调：{featName}",
                $"牺牲当前法术（或同环自发法术位）并消耗 1 次奥术同调次数，使你另一个奥术职业下一次施放的法术（原环级不超过奥法追随者等级的 1/2，且未搭配超魔）获得{featName}效果。",
                AbilityType.Supernatural,
                UnitCommand.CommandType.Free,
                AbilityRange.Personal,
                resLogic,
                Run(new ContextActionApplyAugmentedCasting
                {
                    Metamagic = magic,
                    ToPreparedBuff = Ref<BlueprintBuffReference>(toPrepBuff),
                    ToSpontaneousBuff = Ref<BlueprintBuffReference>(toSpontBuff)
                }));
            child.Hidden = true;
            child.NotOffensive = true;
            child.ActionBarAutoFillIgnored = true;
            child.AvailableMetamagic = 0;

            if (sourceFeat?.Icon != null)
            {
                Set(child, "m_Icon", sourceFeat.Icon);
                Set(toPrepBuff, "m_Icon", sourceFeat.Icon);
                Set(toSpontBuff, "m_Icon", sourceFeat.Icon);
            }

            foreach (var bp in new BlueprintScriptableObject[] { toPrepBuff, toSpontBuff, child })
                PsychicRepairComponents.UniqueNames(bp);

            if (cache.Load(Class.AssetGuid) != null)
            {
                f.Enable();
                foreach (var bp in new BlueprintScriptableObject[] { toPrepBuff, toSpontBuff, child })
                    if (cache.Load(bp.AssetGuid) == null) cache.AddCachedBlueprint(bp.AssetGuid, bp);
            }

            var entry = new AugmentedEntry
            {
                Metamagic = magic,
                Cost = increase,
                ChildAbility = child,
                ToPreparedBuff = toPrepBuff,
                ToSpontaneousBuff = toSpontBuff
            };
            AugmentedEntries[magic] = entry;
            AugmentedChildGuids.Add(child.AssetGuid);
            AugmentedBuffGuids.Add(toPrepBuff.AssetGuid);
            AugmentedBuffGuids.Add(toSpontBuff.AssetGuid);
            return entry;
        }

        internal static bool EligibleBook(BlueprintSpellbook book, bool spontaneous)
        {
            if (book == null || !book.IsArcane || book.Spontaneous != spontaneous) return false;
            if (book.IsArcanist || book.IsMythic || book.IsAlchemist) return false;
            if (PsychicCasting.IsPsychic(book)) return false;
            if (book.AssetGuid == BlueprintGuid.Parse(MediumIntegration.BookId)) return false;
            if (book.SpellsPerDay == null || book.CharacterClass == null) return false;
            if (book.CharacterClass.PrestigeClass && book != SublimeChord.Spellbook) return false;
            return true;
        }

        private static BlueprintFeature BuildBookOption(BlueprintsCache cache, PsychicAssets f, BlueprintSpellbook book, bool spontaneous)
        {
            var dict = spontaneous ? SpontaneousBookOptions : PreparedBookOptions;
            if (dict.TryGetValue(book.AssetGuid, out var cached) && cached != null) return cached;

            string token = T((spontaneous ? "SpontBook/" : "PrepBook/") + book.AssetGuid);
            if (cache.Load(Id(token)) is BlueprintFeature existing)
            {
                dict[book.AssetGuid] = existing;
                return existing;
            }

            var cls = book.CharacterClass;
            string label = cls?.Name ?? book.name;
            if (cls != null && cls.Spellbook != book && !string.IsNullOrEmpty(book.Name))
                label = label + "（" + book.Name + "）";

            string kindLabel = spontaneous ? "自发奥术" : "准备奥术";
            int minLevel = spontaneous ? 1 : 2;
            var feature = f.Feature(token,
                $"{kindLabel}：{label}",
                $"将{label}的法术书选定为奥法追随者的{kindLabel}基础法术书。",
                new UltimateMagusSelectBookComponent
                {
                    Book = Ref<BlueprintSpellbookReference>(book),
                    IsSpontaneous = spontaneous
                },
                new UltimateMagusBookOwnedPrerequisite
                {
                    Book = Ref<BlueprintSpellbookReference>(book),
                    IsSpontaneous = spontaneous,
                    MinSpellLevel = minLevel
                });
            if (cls?.m_Icon != null) Set(feature, "m_Icon", cls.m_Icon);
            PsychicRepairComponents.UniqueNames(feature);
            feature.OnEnable();
            if (cache.Load(feature.AssetGuid) == null) cache.AddCachedBlueprint(feature.AssetGuid, feature);
            dict[book.AssetGuid] = feature;
            return feature;
        }

        internal static void Reconcile(BlueprintsCache cache)
        {
            if (cache == null || Class == null || Reconciling) return;
            Reconciling = true;
            try
            {
                var f = new PsychicAssets(cache);
                var snapshot = SublimeChord.Snapshot(cache);

                // 1. Discover prepared & spontaneous arcane spellbooks.
                var prepBooks = snapshot.OfType<BlueprintSpellbook>().Where(b => EligibleBook(b, false))
                    .OrderBy(b => b.AssetGuid.ToString(), StringComparer.Ordinal).ToArray();
                var spontBooks = snapshot.OfType<BlueprintSpellbook>().Where(b => EligibleBook(b, true))
                    .OrderBy(b => b.AssetGuid.ToString(), StringComparer.Ordinal).ToArray();

                var prepFeatures = prepBooks.Select(b => BuildBookOption(cache, f, b, false)).ToArray();
                var spontFeatures = spontBooks.Select(b => BuildBookOption(cache, f, b, true)).ToArray();

                AppendMissing(PreparedSelection, prepFeatures);
                AppendMissing(SpontaneousSelection, spontFeatures);

                // 2. Discover metamagic feats for BonusMetamagicSelection and Augmented Casting.
                var stockMetas = KnownNativeMetamagics
                    .Select(x => cache.Load(BlueprintGuid.Parse(x.FeatId)) as BlueprintFeature)
                    .Where(x => x != null);
                var allFeatures = stockMetas.Concat(snapshot.OfType<BlueprintFeature>()).Distinct().ToArray();

                var bonusMetas = new List<BlueprintFeature>();
                foreach (var feat in allFeatures.OrderBy(v => v.AssetGuid.ToString(), StringComparer.Ordinal))
                {
                    var comps = feat.GetComponents<AddMetamagicFeat>().ToArray();
                    if (comps.Length != 1) continue;
                    bonusMetas.Add(feat);

                    var flag = comps[0].Metamagic;
                    uint bit = unchecked((uint)(int)flag);
                    if (bit != 0 && (bit & (bit - 1)) == 0 && !MetamagicFeatOptions.ContainsKey(flag))
                    {
                        MetamagicFeatOptions[flag] = feat;
                        EnsureAugmentedEntry(cache, f, flag, feat);
                    }
                }
                AppendMissing(BonusMetamagicSelection, bonusMetas);
            }
            catch (Exception e)
            {
                ModEntry.LogSpellDiagnostic("Ultimate Magus reconcile skipped: " + e.Message);
            }
            finally
            {
                Reconciling = false;
            }
        }

        private static void AppendMissing(BlueprintFeatureSelection selection, IEnumerable<BlueprintFeature> items)
        {
            if (selection == null || items == null) return;
            var current = (BlueprintFeatureReference[])Get(selection, "m_AllFeatures") ?? Array.Empty<BlueprintFeatureReference>();
            var ids = new HashSet<BlueprintGuid>(current.Where(r => r != null).Select(r => r.Guid));
            var missing = items.Where(x => x != null && ids.Add(x.AssetGuid)).ToArray();
            if (missing.Length == 0) return;
            var refs = missing.Select(Ref<BlueprintFeatureReference>).ToArray();
            Set(selection, "m_AllFeatures", current.Concat(refs).ToArray());
            var own = (BlueprintFeatureReference[])Get(selection, "m_Features") ?? Array.Empty<BlueprintFeatureReference>();
            Set(selection, "m_Features", own.Concat(refs).ToArray());
        }

        private static BlueprintFeatureReference[] NarrowBooks(BlueprintFeatureSelection selection, Dictionary<BlueprintGuid, BlueprintFeature> map)
        {
            if (selection == null || map.Count == 0) return null;
            var controller = Kingmaker.Game.Instance?.LevelUpController;
            var descriptor = (controller?.Preview ?? controller?.Unit)?.Descriptor;
            if (descriptor == null) return null;

            var owned = new HashSet<BlueprintGuid>();
            foreach (var sb in descriptor.Spellbooks)
                if (sb?.Blueprint != null) owned.Add(sb.Blueprint.AssetGuid);
            foreach (var cd in descriptor.Progression.Classes)
            {
                var def = cd?.CharacterClass?.Spellbook;
                if (def != null) owned.Add(def.AssetGuid);
            }
            if (owned.Count == 0) return null;

            var refs = map.Where(pair => owned.Contains(pair.Key) && pair.Value != null)
                .OrderBy(pair => pair.Key.ToString(), StringComparer.Ordinal)
                .Select(pair => Ref<BlueprintFeatureReference>(pair.Value)).ToArray();
            if (refs.Length == 0) return null;
            Set(selection, "m_AllFeatures", refs);
            Set(selection, "m_Features", refs);
            return refs;
        }

        internal static bool KnowsMetamagic(UnitDescriptor unit, Metamagic magic)
        {
            if (unit == null) return false;
            foreach (var fact in unit.Facts.List)
            {
                var add = fact?.Blueprint?.GetComponent<AddMetamagicFeat>();
                if (add != null && (add.Metamagic & magic) == magic) return true;
            }
            return false;
        }

        internal static int GetClassLevel(UnitDescriptor unit)
            => unit == null || Class == null ? 0 : unit.Progression.GetClassLevel(Class);

        internal static bool IsAugmentedBuff(BlueprintBuff bp)
            => bp != null && AugmentedBuffGuids.Contains(bp.AssetGuid);

        internal static bool IsAugmentedChild(BlueprintAbility bp)
            => bp != null && AugmentedChildGuids.Contains(bp.AssetGuid);

        [HarmonyPatch(typeof(BlueprintsCache), nameof(BlueprintsCache.AddCachedBlueprint))]
        private static class BlueprintPublishedPatch
        {
            private static void Postfix(SimpleBlueprint __1)
            {
                if (__1 is BlueprintSpellbook b && (EligibleBook(b, false) || EligibleBook(b, true)))
                    Pending = true;
                else if (__1 is BlueprintFeature f && f.GetComponent<AddMetamagicFeat>() != null)
                    Pending = true;
            }
        }

        [HarmonyPatch(typeof(BlueprintFeatureSelection), "get_AllFeatures")]
        private static class SelectionOpenedPatch
        {
            private static void Postfix(BlueprintFeatureSelection __instance,
                ref ReferenceArrayProxy<BlueprintFeature, BlueprintFeatureReference> __result)
            {
                if (Cache == null || Class == null) return;
                if (Pending || SelectionOpens < 6)
                {
                    Pending = false;
                    Reconcile(Cache);
                }
                SelectionOpens++;

                if (__instance == PreparedSelection)
                {
                    var narrowed = NarrowBooks(PreparedSelection, PreparedBookOptions);
                    if (narrowed != null) __result = new ReferenceArrayProxy<BlueprintFeature, BlueprintFeatureReference>(narrowed);
                }
                else if (__instance == SpontaneousSelection)
                {
                    var narrowed = NarrowBooks(SpontaneousSelection, SpontaneousBookOptions);
                    if (narrowed != null) __result = new ReferenceArrayProxy<BlueprintFeature, BlueprintFeatureReference>(narrowed);
                }
            }
        }

        [HarmonyPatch(typeof(AbilityData), nameof(AbilityData.GetConversions))]
        private static class AugmentedConversionsPatch
        {
            private static void Postfix(AbilityData __instance, ref IEnumerable<AbilityData> __result)
            {
                if (__instance == null || __instance.SourceItem != null) return;
                if (__instance.Blueprint == null || __instance.Blueprint.Type != AbilityType.Spell) return;
                var caster = __instance.Caster;
                if (caster?.Unit == null || AugmentedCastingFeature == null || AugmentedCastingResource == null) return;
                if (!caster.HasFact(AugmentedCastingFeature)) return;
                if (caster.Resources.GetResourceAmount(AugmentedCastingResource) <= 0) return;

                var sb = __instance.Spellbook;
                if (sb == null || !sb.Blueprint.IsArcane) return;
                var part = caster.Unit.Get<UnitPartUltimateMagus>();
                if (part == null || part.PreparedBook == null || part.SpontaneousBook == null) return;

                bool isPrep = sb.Blueprint == part.PreparedBook;
                bool isSpont = sb.Blueprint == part.SpontaneousBook;
                if (!isPrep && !isSpont) return;

                int spellLevel = __instance.SpellLevel;
                if (spellLevel < 1) return;
                if (!sb.CanSpend(__instance)) return;

                var list = __result?.ToList() ?? new List<AbilityData>();
                foreach (var pair in AugmentedEntries.OrderBy(p => p.Value.Cost).ThenBy(p => (int)p.Key))
                {
                    var entry = pair.Value;
                    if (spellLevel < entry.Cost) continue;
                    if (!KnowsMetamagic(caster, entry.Metamagic)) continue;
                    var converted = new AbilityData(__instance, entry.ChildAbility);
                    list.Add(converted);
                }
                __result = list;
            }
        }

        [HarmonyPatch(typeof(AbilityData), nameof(AbilityData.HasMetamagic))]
        private static class AugmentedQuickenPatch
        {
            private static void Postfix(AbilityData __instance, Metamagic metamagic, ref bool __result)
            {
                if (__result || metamagic != Metamagic.Quicken) return;
                if (__instance == null || __instance.SourceItem != null || __instance.Blueprint == null) return;
                if (__instance.Blueprint.Type != AbilityType.Spell) return;
                var caster = __instance.Caster;
                if (caster?.Unit == null) return;
                if (__instance.MetamagicData != null && __instance.MetamagicData.NotEmpty) return;
                if ((__instance.Blueprint.AvailableMetamagic & Metamagic.Quicken) == 0) return;

                var sb = __instance.Spellbook;
                if (sb == null) return;
                var part = caster.Unit.Get<UnitPartUltimateMagus>();
                if (part == null) return;

                int umLevel = GetClassLevel(caster);
                if (umLevel < 3) return;
                int spellLevel = __instance.SpellLevel;
                if (spellLevel < 1 || spellLevel > umLevel / 2) return;

                foreach (var buff in caster.Unit.Buffs.Enumerable)
                {
                    if (!buff.IsActive || buff.IsSuppressed || buff.IsDisposed) continue;
                    var comp = buff.Blueprint.GetComponent<UltimateMagusAugmentedBuffComponent>();
                    if (comp == null || comp.Metamagic != Metamagic.Quicken) continue;
                    var targetBook = comp.TargetSpontaneous ? part.SpontaneousBook : part.PreparedBook;
                    if (targetBook != null && sb.Blueprint == targetBook)
                    {
                        __result = true;
                        return;
                    }
                }
            }
        }
    }

    [TypeId("c8a1f2014d9a4c1b8e21a00100000001")]
    public sealed class UltimateMagusMetamagicPrerequisite : Prerequisite
    {
        protected override bool CheckInternal(FeatureSelectionState selectionState, UnitDescriptor unit, LevelUpState state)
        {
            if (unit == null) return false;
            foreach (var f in unit.Progression.Features.Enumerable)
            {
                if (f?.Blueprint?.GetComponent<AddMetamagicFeat>() != null)
                    return true;
            }
            return false;
        }

        protected override string GetUITextInternal(UnitDescriptor unit) => "任意超魔专长";
    }

    [TypeId("c8a1f2014d9a4c1b8e21a00100000002")]
    public sealed class UltimateMagusSpellcastingPrerequisite : Prerequisite
    {
        protected override bool CheckInternal(FeatureSelectionState selectionState, UnitDescriptor unit, LevelUpState state)
        {
            if (unit == null) return false;
            bool hasPrepared2 = false;
            bool hasSpontaneous1 = false;
            foreach (var sb in unit.Spellbooks)
            {
                if (sb?.Blueprint == null) continue;
                if (UltimateMagus.EligibleBook(sb.Blueprint, false) && sb.MaxSpellLevel >= 2)
                    hasPrepared2 = true;
                else if (UltimateMagus.EligibleBook(sb.Blueprint, true) && sb.MaxSpellLevel >= 1)
                    hasSpontaneous1 = true;
            }
            return hasPrepared2 && hasSpontaneous1;
        }

        protected override string GetUITextInternal(UnitDescriptor unit) =>
            "能够不经过准备施展 1 级奥术，并能够经过准备施展法术书上的 2 级奥术（不含奥能师）";
    }

    [TypeId("c8a1f2014d9a4c1b8e21a00100000003")]
    public sealed class UltimateMagusBookOwnedPrerequisite : Prerequisite
    {
        public BlueprintSpellbookReference Book;
        public bool IsSpontaneous;
        public int MinSpellLevel;

        protected override bool CheckInternal(FeatureSelectionState selectionState, UnitDescriptor unit, LevelUpState state)
        {
            var book = Book?.Get();
            if (unit == null || book == null) return false;
            var sb = unit.Spellbooks.FirstOrDefault(b => b?.Blueprint == book);
            if (sb != null) return sb.MaxSpellLevel >= MinSpellLevel;
            var cls = book.CharacterClass;
            return cls != null && cls.Spellbook == book && unit.Progression.Classes.Any(c => c.CharacterClass == cls);
        }

        protected override string GetUITextInternal(UnitDescriptor unit) =>
            IsSpontaneous ? $"已拥有该自发奥术法术书（至少 {MinSpellLevel} 环）" : $"已拥有该准备奥术法术书（至少 {MinSpellLevel} 环）";
    }

    [TypeId("c8a1f2014d9a4c1b8e21a00100000004")]
    public sealed class UltimateMagusLowerBookPrerequisite : Prerequisite
    {
        public bool IsSpontaneous;

        internal static void ResolveBooks(UnitDescriptor unit, out Spellbook prep, out Spellbook spont)
        {
            prep = null;
            spont = null;
            if (unit == null) return;

            var part = unit.Unit?.Get<UnitPartUltimateMagus>();
            if (part?.PreparedBook != null)
                prep = unit.Spellbooks.FirstOrDefault(b => b?.Blueprint == part.PreparedBook);
            if (part?.SpontaneousBook != null)
                spont = unit.Spellbooks.FirstOrDefault(b => b?.Blueprint == part.SpontaneousBook);

            if (prep == null || spont == null)
            {
                foreach (var feat in unit.Progression.Features.Enumerable)
                {
                    var sel = feat?.Blueprint?.GetComponent<UltimateMagusSelectBookComponent>();
                    if (sel?.Book?.Get() == null) continue;
                    var match = unit.Spellbooks.FirstOrDefault(b => b?.Blueprint == sel.Book.Get());
                    if (match != null)
                    {
                        if (sel.IsSpontaneous) spont = spont ?? match;
                        else prep = prep ?? match;
                    }
                }
            }

            if (prep == null)
            {
                var cands = unit.Spellbooks.Where(b => b?.Blueprint != null && UltimateMagus.EligibleBook(b.Blueprint, false) && b.MaxSpellLevel >= 2).ToArray();
                if (cands.Length == 1) prep = cands[0];
            }
            if (spont == null)
            {
                var cands = unit.Spellbooks.Where(b => b?.Blueprint != null && UltimateMagus.EligibleBook(b.Blueprint, true) && b.MaxSpellLevel >= 1).ToArray();
                if (cands.Length == 1) spont = cands[0];
            }
        }

        protected override bool CheckInternal(FeatureSelectionState selectionState, UnitDescriptor unit, LevelUpState state)
        {
            ResolveBooks(unit, out var prep, out var spont);
            if (prep == null || spont == null) return true;
            int prepCl = prep.CasterLevel;
            int spontCl = spont.CasterLevel;
            return IsSpontaneous ? spontCl <= prepCl : prepCl <= spontCl;
        }

        protected override string GetUITextInternal(UnitDescriptor unit) =>
            IsSpontaneous
                ? "当前自发奥术有效施法等级低于或等于准备奥术有效施法等级"
                : "当前准备奥术有效施法等级低于或等于自发奥术有效施法等级";
    }

    public sealed class UnitPartUltimateMagus : OldStyleUnitPart, IUnitCompleteLevelUpHandler
    {
        [JsonProperty]
        public BlueprintSpellbookReference PreparedBookRef;

        [JsonProperty]
        public BlueprintSpellbookReference SpontaneousBookRef;

        [JsonProperty]
        public int AppliedPreparedAdvances;

        [JsonProperty]
        public int AppliedSpontaneousAdvances;

        [JsonProperty]
        public int TargetPreparedAdvances;

        [JsonProperty]
        public int TargetSpontaneousAdvances;

        public BlueprintSpellbook PreparedBook => PreparedBookRef?.Get();
        public BlueprintSpellbook SpontaneousBook => SpontaneousBookRef?.Get();

        public void Reconcile()
        {
            if (Owner == null) return;
            var prep = PreparedBook;
            if (prep != null)
            {
                var sb = Owner.DemandSpellbook(prep);
                while (AppliedPreparedAdvances < TargetPreparedAdvances)
                {
                    sb.AddBaseLevel();
                    AppliedPreparedAdvances++;
                }
            }
            var spont = SpontaneousBook;
            if (spont != null)
            {
                var sb = Owner.DemandSpellbook(spont);
                while (AppliedSpontaneousAdvances < TargetSpontaneousAdvances)
                {
                    sb.AddBaseLevel();
                    AppliedSpontaneousAdvances++;
                }
            }
        }

        public void HandleUnitCompleteLevelup(UnitEntityData unit)
        {
            if (unit?.Descriptor == Owner) Reconcile();
        }
    }

    [AllowedOn(typeof(BlueprintFeature), false)]
    [TypeId("c8a1f2014d9a4c1b8e21a00100000005")]
    public sealed class UltimateMagusSelectBookComponent : UnitFactComponentDelegate
    {
        public BlueprintSpellbookReference Book;
        public bool IsSpontaneous;

        protected override void OnActivate()
        {
            var book = Book?.Get();
            if (Owner == null || book == null) return;
            var part = Owner.Ensure<UnitPartUltimateMagus>();
            if (IsSpontaneous) part.SpontaneousBookRef = Book;
            else part.PreparedBookRef = Book;
            part.Reconcile();
        }
    }

    [AllowedOn(typeof(BlueprintFeature), false)]
    [AllowMultipleComponents]
    [TypeId("c8a1f2014d9a4c1b8e21a00100000006")]
    public sealed class UltimateMagusAdvanceBookComponent : UnitFactComponentDelegate
    {
        public bool IsSpontaneous;

        protected override void OnActivate()
        {
            if (Owner == null) return;
            var part = Owner.Ensure<UnitPartUltimateMagus>();
            if (IsSpontaneous) part.TargetSpontaneousAdvances++;
            else part.TargetPreparedAdvances++;
            part.Reconcile();
        }
    }

    [AllowedOn(typeof(BlueprintFeature), false)]
    [TypeId("c8a1f2014d9a4c1b8e21a00100000007")]
    public sealed class UltimateMagusSpellPowerComponent : UnitFactComponentDelegate,
        IInitiatorRulebookHandler<RuleCalculateAbilityParams>, IInitiatorRulebookSubscriber
    {
        public void OnEventAboutToTrigger(RuleCalculateAbilityParams evt)
        {
            if (Owner == null || evt.Spellbook == null || evt.AbilityData?.SourceItem != null) return;
            if (!evt.Spellbook.Blueprint.IsArcane) return;
            int rank = Fact?.GetRank() ?? 0;
            if (rank > 0) evt.AddBonusCasterLevel(rank, ModifierDescriptor.UntypedStackable);
        }

        public void OnEventDidTrigger(RuleCalculateAbilityParams evt) { }
    }

    [AllowedOn(typeof(BlueprintFeature), false)]
    [TypeId("c8a1f2014d9a4c1b8e21a00100000008")]
    public sealed class UltimateMagusExpandedKnowledgeComponent : UnitFactComponentDelegate,
        ILearnSpellHandler, IUnitCompleteLevelUpHandler
    {
        protected override void OnTurnOn() => Refresh();
        public void HandleLearnSpell() => Refresh();
        public void HandleUnitCompleteLevelup(UnitEntityData unit)
        {
            if (unit?.Descriptor == Owner) Refresh();
        }

        private void Refresh()
        {
            if (Owner == null) return;
            var part = Owner.Get<UnitPartUltimateMagus>();
            if (part?.PreparedBook == null || part.SpontaneousBook == null) return;

            var prepBook = Owner.Spellbooks.FirstOrDefault(b => b?.Blueprint == part.PreparedBook);
            var spontBook = Owner.Spellbooks.FirstOrDefault(b => b?.Blueprint == part.SpontaneousBook);
            if (prepBook == null || spontBook == null) return;

            int maxCircle = Math.Min(5, Fact?.GetRank() ?? 0);
            for (int level = 1; level <= maxCircle; level++)
            {
                if (level <= spontBook.MaxSpellLevel)
                {
                    foreach (var known in prepBook.GetKnownSpells(level).ToArray())
                    {
                        if (known?.Blueprint == null || known.IsTemporary || known.MetamagicData?.NotEmpty == true) continue;
                        if (!spontBook.IsKnown(known.Blueprint))
                            spontBook.AddKnown(level, known.Blueprint, raiseLearnSpellEvent: false);
                    }
                }
                if (level <= prepBook.MaxSpellLevel)
                {
                    foreach (var known in spontBook.GetKnownSpells(level).ToArray())
                    {
                        if (known?.Blueprint == null || known.IsTemporary || known.MetamagicData?.NotEmpty == true) continue;
                        if (!prepBook.IsKnown(known.Blueprint))
                            prepBook.AddKnown(level, known.Blueprint, raiseLearnSpellEvent: false);
                    }
                }
            }
        }
    }

    [TypeId("c8a1f2014d9a4c1b8e21a00100000009")]
    public sealed class ContextActionApplyAugmentedCasting : ContextAction
    {
        public Metamagic Metamagic;
        public BlueprintBuffReference ToPreparedBuff;
        public BlueprintBuffReference ToSpontaneousBuff;

        public override string GetCaption() => $"奥术同调：{Metamagic}";

        public override void RunAction()
        {
            var caster = Context?.MaybeCaster;
            var ability = Context?.SourceAbilityContext?.Ability;
            var source = ability?.ConvertedFrom;
            if (caster == null || source?.Spellbook == null) return;

            var part = caster.Get<UnitPartUltimateMagus>();
            if (part == null || part.PreparedBook == null || part.SpontaneousBook == null) return;

            bool fromPrepared = source.Spellbook.Blueprint == part.PreparedBook;
            bool fromSpontaneous = source.Spellbook.Blueprint == part.SpontaneousBook;
            if (!fromPrepared && !fromSpontaneous) return;

            // Remove any previously armed Augmented Casting buff so only one is active at a time.
            foreach (var buff in caster.Buffs.Enumerable.ToArray())
            {
                if (UltimateMagus.IsAugmentedBuff(buff.Blueprint))
                    buff.Remove();
            }

            var targetBuff = fromPrepared ? ToSpontaneousBuff?.Get() : ToPreparedBuff?.Get();
            if (targetBuff != null)
            {
                var applied = caster.Buffs.AddBuff(targetBuff, caster, null);
                if (applied != null)
                {
                    applied.MakePermanent();
                    applied.IsNotDispelable = true;
                }
            }
        }
    }

    [AllowedOn(typeof(BlueprintBuff), false)]
    [TypeId("c8a1f2014d9a4c1b8e21a0010000000a")]
    public sealed class UltimateMagusAugmentedBuffComponent : UnitBuffComponentDelegate,
        IInitiatorRulebookHandler<RuleCalculateAbilityParams>, IInitiatorRulebookHandler<RuleCastSpell>,
        IInitiatorRulebookSubscriber
    {
        public Metamagic Metamagic;
        public bool TargetSpontaneous;
        private bool armed;

        private bool Matches(AbilityData data, BlueprintAbility spell, int spellLevel)
        {
            if (Owner == null || data == null || spell == null || data.SourceItem != null) return false;
            if (spell.Type != AbilityType.Spell) return false;
            if (UltimateMagus.IsAugmentedChild(spell)) return false;
            if (data.MetamagicData != null && data.MetamagicData.NotEmpty) return false;
            if ((spell.AvailableMetamagic & Metamagic) == 0) return false;

            var sb = data.Spellbook;
            if (sb == null) return false;
            var part = Owner.Get<UnitPartUltimateMagus>();
            if (part == null) return false;

            var expectedBook = TargetSpontaneous ? part.SpontaneousBook : part.PreparedBook;
            if (expectedBook == null || sb.Blueprint != expectedBook) return false;

            int umLevel = UltimateMagus.GetClassLevel(Owner);
            return spellLevel >= 1 && spellLevel <= umLevel / 2;
        }

        public void OnEventAboutToTrigger(RuleCalculateAbilityParams evt)
        {
            armed = false;
            if (!Matches(evt.AbilityData, evt.Spell, evt.SpellLevel)) return;
            armed = true;
            evt.AddMetamagic(Metamagic);
        }

        public void OnEventDidTrigger(RuleCalculateAbilityParams evt) { }

        public void OnEventAboutToTrigger(RuleCastSpell evt) { }

        public void OnEventDidTrigger(RuleCastSpell evt)
        {
            if (!armed) return;
            armed = false;
            if (evt.Spell == null || !Matches(evt.Spell, evt.Spell.Blueprint, evt.Spell.SpellLevel)) return;
            Buff?.Remove();
        }
    }
}

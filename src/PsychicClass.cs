// T15 first batch: a deliberately labelled casting prototype, not the full Psychic class.
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
using Kingmaker.Blueprints.Root;
using Kingmaker.Designers.Mechanics.Facts;
using Kingmaker.ElementsSystem;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Localization;
using Kingmaker.RuleSystem;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Commands.Base;
using Kingmaker.UnitLogic.FactLogic;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Mechanics.Actions;
using Kingmaker.Utility;

namespace CallOfTheWild.Wrath.Prototype
{
    internal static class PsychicClass
    {
        internal const string ModuleName = "Psychic casting prototype";
        internal const string ClassId = "83fa3b50138c566698801ba5967bbde0";
        internal const string BookId = "3ce26d41e96b5ca289f0c6ce9db28d3c";
        internal const string ListId = "6a4b9d6651b155058004354a803da3bf";
        internal const string ProgressionId = "b14115fd5dd15c59a6075b10ef73f4ee";
        internal const string CastingId = "4896622b699957208e128c3a5361609c";
        internal const string CenterAbilityId = "e66116541ac4546c9084a7bcfa47e5a6";
        internal const string CenterBuffId = "0b1c8fa9fda652ccbf0a5b89f2cc6d25";
        internal const string LogicalId = "2d837d1d6122589dbf8792f67147eead";
        internal const string PoolSelectionId = "b9cc062eaf665e8b9c2ba35fa107d273";
        internal const string WisdomPoolId = "ddfc9f9a2cbd51b48cbd214dbd75c8fb";
        internal const string CharismaPoolId = "0795daa1b7d259c3b5052c5c704cc031";
        internal const string WisdomFeatureId = "4e8266b477c65aaf836869377711184a";
        internal const string CharismaFeatureId = "e2e8d9e569805bbfa05af26df72b7e52";
        internal const string RootId = "2d77316c72b9ed44f888ceefc2a131f6";
        internal const string WizardId = "ba34257984f4c41408ce1dc2004e342e";
        internal const string SorcererBookId = "b3db3766a4b605040b366265e2af0e50";
        internal const string ProficienciesId = "25c97697236ccf2479d0c6a4185eae7f";
        internal const string ExtendFeatId = "f180e72e4a9cbaa4da8be9bc958132ef";

        internal static readonly string[] OwnIds = { ClassId, BookId, ListId, ProgressionId, CastingId,
            CenterAbilityId, CenterBuffId, LogicalId, PoolSelectionId, WisdomPoolId, CharismaPoolId, WisdomFeatureId, CharismaFeatureId, MajorId };
        private static void Set(object obj, string field, object value) =>
            (AccessTools.Field(obj.GetType(), field) ?? throw new MissingFieldException(obj.GetType().FullName, field)).SetValue(obj, value);
        private static object Get(object obj, string field) => AccessTools.Field(obj.GetType(), field).GetValue(obj);
        private static T Ref<T>(SimpleBlueprint obj) where T : BlueprintReferenceBase, new() => BlueprintReferenceBase.CreateTyped<T>(obj);
        private static LocalizedString Text(string token, string part) => new LocalizedString { Key = "cotw-wrath.psychic." + token + "." + part };
        private static T Require<T>(BlueprintsCache cache, string id, string name = null) where T : SimpleBlueprint {
            var obj = cache.Load(BlueprintGuid.Parse(id));
            if (!(obj is T typed) || (name != null && obj.name != name))
                throw new InvalidOperationException("Psychic dependency missing or changed: " + id + " / " + name);
            return typed;
        }
        private static T Asset<T>(T obj, string id, string token) where T : BlueprintScriptableObject {
            obj.AssetGuid = BlueprintGuid.Parse(id); obj.name = "CotWWrathPrototype" + token;
            if (obj is BlueprintUnitFact fact) {
                Set(fact, "m_DisplayName", Text(token, "name")); Set(fact, "m_Description", Text(token, "description"));
            }
            return obj;
        }
        private static AddFacts Facts(params BlueprintUnitFact[] facts) {
            var result = new AddFacts { name = "PsychicGrantedFacts" };
            Set(result, "m_Facts", facts.Select(Ref<BlueprintUnitFactReference>).ToArray()); return result;
        }

        internal static void Register(BlueprintsCache cache)
        {
            var wizard = Require<BlueprintCharacterClass>(cache, WizardId, "WizardClass");
            var sorcererBook = Require<BlueprintSpellbook>(cache, SorcererBookId, "SorcererSpellbook");
            var proficiency = Require<BlueprintFeature>(cache, ProficienciesId, "SorcererProficiencies");
            var icon = Require<BlueprintFeature>(cache, ExtendFeatId, "ExtendSpellFeat");
            var basic = Require<BlueprintFeatureSelection>(cache, BlueprintRegistration.BasicFeatsId, "BasicFeatSelection");
            var root = Require<BlueprintRoot>(cache, RootId, "BlueprintRoot");
            if (root.Progression == null || !sorcererBook.Spontaneous || sorcererBook.CastingAttribute != StatType.Charisma ||
                sorcererBook.SpellsKnown == null || sorcererBook.SpellsPerDay == null)
                throw new InvalidOperationException("Psychic native progression/book layout differs from the supported version.");
            var spells = PsychicSpellCatalog.ByLevel.Select(level => level.Select(id => Require<BlueprintAbility>(cache, id)).ToArray()).ToArray();
            if (spells.Length != 10 || spells.Any(level => level.Length == 0 || level.Any(s => s.Type != AbilityType.Spell)))
                throw new InvalidOperationException("Psychic spell catalog is incomplete or contains non-spells.");

            var character = Asset(new BlueprintCharacterClass {
                LocalizedName = Text("PsychicClass", "name"), LocalizedDescription = Text("PsychicClass", "description"),
                LocalizedDescriptionShort = Text("PsychicClass", "description"), SkillPoints = 2,
                IsArcaneCaster = false, IsDivineCaster = false, PrestigeClass = false, HideInUI = false,
                ClassSkills = new[] { StatType.SkillKnowledgeArcana, StatType.SkillKnowledgeWorld, StatType.SkillLoreNature,
                    StatType.SkillLoreReligion, StatType.SkillPerception, StatType.SkillPersuasion },
                RecommendedAttributes = new[] { StatType.Intelligence, StatType.Wisdom, StatType.Charisma },
                NotRecommendedAttributes = Array.Empty<StatType>(), PrimaryColor = wizard.PrimaryColor,
                SecondaryColor = wizard.SecondaryColor, StartingGold = wizard.StartingGold,
                MaleEquipmentEntities = wizard.MaleEquipmentEntities, FemaleEquipmentEntities = wizard.FemaleEquipmentEntities,
                ComponentsArray = new BlueprintComponent[] { new PrerequisiteIsPet { name = "PsychicNotPet", Not = true } }
            }, ClassId, "PsychicClass");
            foreach (var field in new[] { "HitDie", "m_BaseAttackBonus", "m_FortitudeSave", "m_ReflexSave", "m_WillSave", "m_StartingItems", "m_EquipmentEntities", "m_Difficulty" })
                Set(character, field, Get(wizard, field));
            Set(character, "m_Archetypes", Array.Empty<BlueprintArchetypeReference>());

            var list = Asset(new BlueprintSpellList { SpellsByLevel = SpellListCompatibility.Build(Enumerable.Range(0, 10).Select(level => {
                var entry = new SpellLevelList(level); Set(entry, "m_Spells", spells[level].Select(Ref<BlueprintAbilityReference>).ToList()); return entry;
            }).ToArray()) }, ListId, "PsychicSpellList");
            var book = Asset(new BlueprintSpellbook {
                Name = Text("PsychicSpellbook", "name"), CastingAttribute = StatType.Intelligence, Spontaneous = true,
                SpellsPerLevel = 0, AllSpellsKnown = false, CanCopyScrolls = false, CantripsType = CantripsType.Cantrips,
                IsArcane = false, CasterLevelModifier = 0,
                ComponentsArray = new BlueprintComponent[] { new PsychicSpellbookMarker { name = "PsychicThoughtEmotionComponents" } }
            }, BookId, "PsychicSpellbook");
            foreach (var field in new[] { "m_SpellsKnown", "m_SpellsPerDay" }) Set(book, field, Get(sorcererBook, field));
            Set(book, "m_CharacterClass", Ref<BlueprintCharacterClassReference>(character));
            Set(book, "m_SpellList", Ref<BlueprintSpellListReference>(list));
            Set(character, "m_Spellbook", Ref<BlueprintSpellbookReference>(book));

            var centerBuff = Asset(new BlueprintBuff { Stacking = StackingType.Replace }, CenterBuffId, "CenterSelfBuff");
            var action = new ContextActionApplyBuff { name = "CenterSelfOneRound", IsNotDispelable = true, ToCaster = true,
                DurationValue = new ContextDurationValue { Rate = DurationRate.Rounds, DiceType = DiceType.Zero, DiceCountValue = 0, BonusValue = 1 } };
            Set(action, "m_Buff", Ref<BlueprintBuffReference>(centerBuff));
            var center = Asset(new BlueprintAbility {
                Type = AbilityType.Special, ActionType = UnitCommand.CommandType.Move, Range = AbilityRange.Personal,
                CanTargetSelf = true, EffectOnAlly = AbilityEffectOnUnit.Helpful,
                ComponentsArray = new BlueprintComponent[] { new AbilityEffectRunAction { name = "CenterSelfActions", Actions = new ActionList { Actions = new GameAction[] { action } } } }
            }, CenterAbilityId, "CenterSelf");
            PsychicPresentation.Center(cache, center, centerBuff);
            var casting = Asset(new BlueprintFeature { Ranks = 1,
                ComponentsArray = new BlueprintComponent[] { Facts(center) } }, CastingId, "PsychicSpellcasting");
            // AddFacts restores missing entries on post-load, including old characters.
            var castingFacts=casting.GetComponent<AddFacts>();
            var menuReference=new BlueprintUnitFactReference();
            Set(menuReference,"deserializedGuid",PsychicAssets.Id(PsychicAmplificationMenu.RootToken));
            Set(castingFacts,"m_Facts",new[]{Ref<BlueprintUnitFactReference>(center),menuReference});

            var wisResource = Pool(character, WisdomPoolId, StatType.Wisdom);
            var chaResource = Pool(character, CharismaPoolId, StatType.Charisma);
            var wis = PoolFeature(wisResource, WisdomFeatureId, "WisdomPhrenicPool");
            var cha = PoolFeature(chaResource, CharismaFeatureId, "CharismaPhrenicPool");
            var poolSelection = Asset(new BlueprintFeatureSelection { Ranks = 1 }, PoolSelectionId, "PhrenicAbilitySelection");
            Set(poolSelection, "m_AllFeatures", new[] { Ref<BlueprintFeatureReference>(wis), Ref<BlueprintFeatureReference>(cha) });
            Set(poolSelection, "m_Features", new[] { Ref<BlueprintFeatureReference>(wis), Ref<BlueprintFeatureReference>(cha) });

            var logical = Asset(new BlueprintFeature { Ranks = 1, Groups = new[] { FeatureGroup.Feat, FeatureGroup.WizardFeat },
                ComponentsArray = new BlueprintComponent[] {
                    new AddMetamagicFeat { name = "PsychicLogicalMetamagic", Metamagic = PsychicCasting.Logical },
                    new PrerequisiteClassLevel { name = "LogicalRequiresPsychic", Level = 1 }
                } }, LogicalId, "LogicalSpell");
            Set(logical.GetComponent<PrerequisiteClassLevel>(), "m_CharacterClass", Ref<BlueprintCharacterClassReference>(character));
            Set(logical, "m_Icon", icon.Icon);
            var major=Asset(new BlueprintFeature{Ranks=1,IsClassFeature=true,ComponentsArray=Array.Empty<BlueprintComponent>()},MajorId,"MajorAmplifications");
            Set(major,"m_DisplayName",PsychicAssets.Text("MajorAmplificationsFeature01460","name","Major Amplifications"));
            Set(major,"m_Description",PsychicAssets.Text("MajorAmplificationsFeature01460","description","At 11th level, you can select major amplifications in place of phrenic amplifications. This feature grants eligibility, not an additional selection."));Set(major,"m_Icon",icon.Icon);
            // Native level-grant component also reconciles an older character
            // on PostLoad. The real progression still exposes the level11
            // marker in the class chart; native FeatureSet owns deduplication.
            var majorGrant=new MajorGrant01460{name="MajorAmplificationsGrant01460",Level=11};
            Set(majorGrant,"m_Class",Ref<BlueprintCharacterClassReference>(character));Set(majorGrant,"m_Feature",Ref<BlueprintFeatureReference>(major));
            Set(majorGrant,"m_AdditionalClasses",Array.Empty<BlueprintCharacterClassReference>());Set(majorGrant,"m_Archetypes",Array.Empty<BlueprintArchetypeReference>());
            casting.ComponentsArray=casting.ComponentsArray.Concat(new BlueprintComponent[]{majorGrant}).ToArray();
            var progression = Asset(new BlueprintProgression { Ranks = 1,
                LevelEntries = Enumerable.Range(1, 20).Select(level => {
                    var entry = new LevelEntry { Level = level };
                    entry.SetFeatures(level == 1 ? new BlueprintFeatureBase[] { proficiency, casting, poolSelection } : level==11?new BlueprintFeatureBase[]{major}:Array.Empty<BlueprintFeatureBase>());
                    return entry;
                }).ToArray(), UIGroups = Array.Empty<UIGroup>() }, ProgressionId, "PsychicProgression");
            Set(progression, "m_Classes", new[] { new BlueprintProgression.ClassWithLevel { m_Class = Ref<BlueprintCharacterClassReference>(character) } });
            Set(progression, "m_UIDeterminatorsGroup", new BlueprintFeatureBaseReference[] { Ref<BlueprintFeatureBaseReference>(casting), Ref<BlueprintFeatureBaseReference>(poolSelection) });
            Set(character, "m_Progression", Ref<BlueprintProgressionReference>(progression));

            var assets = new BlueprintScriptableObject[] { character, book, list, progression, casting, center, centerBuff,
                logical, poolSelection, wisResource, chaResource, wis, cha, major };
            foreach (var bp in assets) bp.OnEnable();
            var existing = assets.Select(bp => cache.Load(bp.AssetGuid)).ToArray();
            for (int i = 0; i < assets.Length; i++)
                if (existing[i] != null && (existing[i].GetType() != assets[i].GetType() || existing[i].name != assets[i].name ||
                    Fingerprint((BlueprintScriptableObject)existing[i]) != Fingerprint(assets[i])))
                    throw new InvalidOperationException("Psychic GUID belongs to unexpected content; preserving it: " + assets[i].AssetGuid);
            if (existing.Any(bp => bp != null) && existing.Any(bp => bp == null))
                throw new InvalidOperationException("Psychic registration is only partially present; preserving it for diagnosis.");

            var previousClasses = (BlueprintCharacterClassReference[])Get(root.Progression, "m_CharacterClasses") ?? Array.Empty<BlueprintCharacterClassReference>();
            var previousAll = (BlueprintFeatureReference[])Get(basic, "m_AllFeatures") ?? Array.Empty<BlueprintFeatureReference>();
            var previousFeatures = (BlueprintFeatureReference[])Get(basic, "m_Features") ?? Array.Empty<BlueprintFeatureReference>();
            if (previousClasses.Count(r => r?.Guid == character.AssetGuid) > 1 || previousAll.Count(r => r?.Guid == logical.AssetGuid) > 1 ||
                previousFeatures.Count(r => r?.Guid == logical.AssetGuid) > 1)
                throw new InvalidOperationException("Psychic registration contains duplicate shared-list entries.");
            if (existing.All(bp => bp != null)) { character = (BlueprintCharacterClass)existing[0]; logical = (BlueprintFeature)existing[7]; }
            var added = new List<SimpleBlueprint>();
            try {
                if (existing.All(bp => bp == null)) foreach (var bp in assets) { cache.AddCachedBlueprint(bp.AssetGuid, bp); added.Add(bp); }
                if (!previousClasses.Any(r => r?.Guid == character.AssetGuid)) Set(root.Progression, "m_CharacterClasses", previousClasses.Concat(new[] { Ref<BlueprintCharacterClassReference>(character) }).ToArray());
                if (!previousAll.Any(r => r?.Guid == logical.AssetGuid)) Set(basic, "m_AllFeatures", previousAll.Concat(new[] { Ref<BlueprintFeatureReference>(logical) }).ToArray());
                if (!previousFeatures.Any(r => r?.Guid == logical.AssetGuid)) Set(basic, "m_Features", previousFeatures.Concat(new[] { Ref<BlueprintFeatureReference>(logical) }).ToArray());
            } catch {
                Set(root.Progression, "m_CharacterClasses", previousClasses); Set(basic, "m_AllFeatures", previousAll); Set(basic, "m_Features", previousFeatures);
                foreach (var bp in added) cache.RemoveCachedBlueprint(bp.AssetGuid); throw;
            }
        }

        private static BlueprintAbilityResource Pool(BlueprintCharacterClass character, string id, StatType stat)
        {
            var resource = Asset(new BlueprintAbilityResource(), id, stat + "PhrenicResource");
            var amount = Get(resource, "m_MaxAmount");
            Set(amount, "m_Class", Array.Empty<BlueprintCharacterClassReference>());
            Set(amount, "m_Archetypes", Array.Empty<BlueprintArchetypeReference>());
            Set(amount, "IncreasedByLevelStartPlusDivStep", true); Set(amount, "StartingLevel", 2);
            Set(amount, "StartingIncrease", 1); Set(amount, "LevelStep", 2); Set(amount, "PerStepIncrease", 1);
            Set(amount, "m_ClassDiv", new[] { Ref<BlueprintCharacterClassReference>(character) });
            Set(amount, "m_ArchetypesDiv", Array.Empty<BlueprintArchetypeReference>());
            Set(amount, "IncreasedByStat", true); Set(amount, "ResourceBonusStat", stat);
            Set(resource, "m_MaxAmount", amount); return resource;
        }
        private static BlueprintFeature PoolFeature(BlueprintAbilityResource resource, string id, string token)
        {
            var add = new AddAbilityResources { name = "PsychicRestoredPhrenicPool", RestoreAmount = true, RestoreOnLevelUp = true };
            Set(add, "m_Resource", Ref<BlueprintAbilityResourceReference>(resource));
            return Asset(new BlueprintFeature { Ranks = 1, ComponentsArray = new BlueprintComponent[] { add } }, id, token);
        }
        private static string Fingerprint(BlueprintScriptableObject bp)
        {
            var components = string.Join(";", bp.ComponentsArray.Select(c => c.GetType().FullName + ":" + c.name +
                (c is AddMetamagicFeat meta ? ":" + (int)meta.Metamagic : "")));
            if (bp is BlueprintSpellbook book) components += ":" + book.IsArcane + ":" + book.Spontaneous + ":" + book.CastingAttribute;
            if (bp is BlueprintSpellList list) components += ":" + string.Join("/", list.SpellsByLevel.Select(l => string.Join(",", ((List<BlueprintAbilityReference>)Get(l, "m_Spells")).Where(r => PsychicSpellCatalog.ByLevel.SelectMany(ids => ids).Contains(r.Guid.ToString())).Select(r => r.Guid))));
            return components;
        }
        internal const string MajorId="fdd7cee80b9e81640bc490e3eafb2742";
    }
}

using BlueprintCore.Blueprints.Configurators.Classes;
using BlueprintCore.Blueprints.CustomConfigurators.Classes;
using BlueprintCore.Blueprints.References;
using BlueprintCore.Utils.Types;
using Kingmaker.Blueprints.Classes;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.RuleSystem;
using MediumClass.Utilities;
using MediumClass.Utils;
using static UnityModManagerNet.UnityModManager.ModEntry;

namespace MediumClass.Medium.Spirits.Adriel
{
    /// <summary>
    /// 传奇英灵：未冕王阿德里尔（斗士职介）。
    ///
    /// 复用勇士职介的英灵同调、降灵奖励与共鸣惩罚；次级英灵之力由“挑战”取代，
    /// 至高英灵之力由“恶咒”取代；中等（瞬袭）与高等（疾袭）沿用勇士的配置。
    /// 同一时间只能降灵阿德里尔与勇士其中之一。
    /// </summary>
    class Adriel
    {
        private static readonly string ClassName = "Adriel";
        internal const string AdrielName = "Adriel.Name";
        private static readonly string AdrielDescription = "Adriel.Description";
        private const string IconName = "assets/icons/spiritchampion.png";
        private static readonly ModLogger Logger = Logging.GetLogger(nameof(Adriel));

        public static void ConfigureEnabled()
        {
            Logger.Log("Generating Adriel (Legendary Champion) Spirit");
            CharacterClassConfigurator.New(ClassName, Guids.Adriel).Configure();
            Challenge.ConfigureEnabled();
            DeathCurse.ConfigureEnabled();

            CharacterClassConfigurator.For(Guids.Adriel)
                .SetLocalizedName(AdrielName)
                .SetLocalizedDescription(AdrielDescription)
                .SetSkillPoints(0)
                .SetHitDie(DiceType.D8)
                .SetPrestigeClass(false)
                .SetIsMythic(false)
                .SetHideIfRestricted(false)
                .SetBaseAttackBonus(StatProgressionRefs.BABMedium.Reference.Get())
                .SetFortitudeSave(StatProgressionRefs.SavesLow.Reference.Get())
                .SetReflexSave(StatProgressionRefs.SavesLow.Reference.Get())
                .SetWillSave(StatProgressionRefs.SavesHigh.Reference.Get())
                .SetProgression(AdrielProgression())
                .SetIsDivineCaster(false)
                .SetIsArcaneCaster(false)
                .SetStartingGold(411)
                .SetStartingItems(
                ItemWeaponRefs.ColdIronShortsword.Reference.Get(),
                ItemArmorRefs.ScalemailStandard.Reference.Get(),
                ItemEquipmentUsableRefs.PotionOfCureLightWounds.Reference.Get(),
                ItemEquipmentUsableRefs.ScrollOfMageArmor.Reference.Get(),
                ItemEquipmentUsableRefs.ScrollOfMageShield.Reference.Get())
                .SetPrimaryColor(11)
                .SetSecondaryColor(47)
                .SetDifficulty(3)
                .AddToMaleEquipmentEntities("f26d20fbaedf1374388c75d7ab1d9ecc", "69b184d9e882f204f99c2cff2d471a13")
                .AddToFemaleEquipmentEntities("2072db411b232024daf6fbfac1001065", "cec1fa08b14c22647834f2168336e16f")
                .AddToClassSkills(
                StatType.SkillPersuasion,
                StatType.SkillLoreReligion,
                StatType.SkillKnowledgeArcana,
                StatType.SkillPerception,
                StatType.SkillUseMagicDevice)
                .AddToRecommendedAttributes(StatType.Charisma)
                .Configure();
        }

        private static BlueprintProgression AdrielProgression()
        {
            var entries = LevelEntryBuilder.New()
                .AddEntry(1, Guids.MediumAdrielChallenge)
                .AddEntry(6, Guids.ChampionSuddenAttack)
                .AddEntry(11, Guids.ChampionFleetCharge)
                .AddEntry(17, Guids.MediumAdrielDeathCurse);

            return ProgressionConfigurator.New(ClassName + "Progression", Guids.AdrielProgression)
                .SetAllowNonContextActions(false)
                .SetHideInUI(false)
                .SetHideInCharacterSheetAndLevelUp(false)
                .SetHideNotAvailibleInUI(false)
                .SetRanks(1)
                .SetReapplyOnLevelUp(false)
                .SetIsClassFeature(false)
                .SetForAllOtherClasses(false)
                .SetLevelEntries(entries)
                .AddToClasses(Guids.Adriel)
                .Configure();
        }
    }
}

using BlueprintCore.Blueprints.CustomConfigurators.Classes;
using BlueprintCore.Utils.Types;
using Kingmaker.Blueprints.Classes;
using MediumClass.Medium;
using MediumClass.Utilities;
using MediumClass.Utils;
using static UnityModManagerNet.UnityModManager.ModEntry;

namespace MediumClass.Medium
{
    class MediumProgression
    {
        private static readonly string ProgressionName = "MediumProgression";
        private static readonly ModLogger Logger = Logging.GetLogger(nameof(MediumProgression));

        public static BlueprintProgression Configure()
        {
            Logger.Log("Generating Medium Progression");
            MediumPsychicBlueprints.ConfigureEnabled();
            MediumProficiencies.ConfigureEnabled();
            SpiritPower.ConfigureEnabled();
            SpiritSurge.ConfigureEnabled();
            SpiritBonus.ConfigureEnabled();
            ChannelSpirit.ConfigureEnabled();
            Influence.ConfigureEnabled();
            SharedSeance.ConfigureEnabled();
            Propitation.ConfigureEnabled();
            TranceOfThree.ConfigureEnabled();
            SpiritMastery.ConfigureEnabled();
            MediumSpellcasterFeat.ConfigureEnabled();
            WeakerSpiritChannel.ConfigureEnabled();
            AstralBeacon.ConfigureEnabled();
            TranceOfMany.ConfigureEnabled();
            SpiritAffinity.ConfigureEnabled();
            SpiritFeat.ConfigureEnabled();

            // 阶段 0（通灵者大改）：移除 1 级「放弃英灵之力」、9 级「慰灵之术」、
            // 15 级「三重降灵」、19 级「英灵化」的授予。蓝图本身仍然构建——老存档里
            // 已经拥有这些 fact 的角色、以及神话「多重降灵」引用的三重降灵技能，
            // 都依赖它们继续存在，只是不再由进度表发放。
            var entries = LevelEntryBuilder.New()
                .AddEntry(1, Guids.MediumPsychicCastingFeature, Guids.MediumSpiritBonus, Guids.MediumProficiencies, Guids.SpiritPower, Guids.SpiritSurge, Guids.MediumChannelSpirit, Guids.MediumInfluence, Guids.MediumSpellcasterFeat, Guids.MediumSpellcasterFeatProhibitArchmage, Guids.MediumSpellcasterFeatProhibitHierophant)
                .AddEntry(2, Guids.MediumSharedSeance)
                // 3 级英灵亲和：3/7/11/15/19 各一次，共 5 次（选择器自身 Ranks = 5）。
                .AddEntry(3, Guids.MediumSpiritAffinity)
                .AddEntry(4, Guids.MediumSpiritBonus, Guids.MediumSpiritFeat)
                .AddEntry(6, Guids.SpiritPower)
                .AddEntry(7, Guids.MediumSpiritAffinity)
                .AddEntry(8, Guids.MediumSpiritBonus, Guids.MediumSpiritFeat)
                .AddEntry(10, Guids.SpiritSurge, Guids.MediumTranceOfMany) 
                .AddEntry(11, Guids.SpiritPower, Guids.MediumSpiritAffinity)
                .AddEntry(12, Guids.MediumSpiritBonus, Guids.MediumSpiritFeat)
                .AddEntry(15, Guids.MediumSpiritAffinity)
                .AddEntry(16, Guids.MediumSpiritBonus, Guids.MediumSpiritFeat)
                .AddEntry(17, Guids.SpiritPower)
                .AddEntry(19, Guids.MediumSpiritAffinity)
                // 4 级英灵专长：4/8/12/16/20 各一个额外专长（选择器自身 Ranks = 5）。
                .AddEntry(20, Guids.MediumSpiritBonus, Guids.SpiritSurge, Guids.AstralBeacon, Guids.MediumSpiritFeat);

            // BlueprintProgression.CalcLevel returns the *character* level when both m_Classes and
            // m_Archetypes are empty (verified in Assembly-CSharp). Every native class progression
            // (Cavalier, Bard, Bloodrager ...) and the spirit progressions of this mod list their
            // class; without it the level entries were granted by total level (0.3.3 and earlier).
            return ProgressionConfigurator.New(ProgressionName, Guids.MediumProgression)
                .SetClasses(Guids.Medium)
                .SetAllowNonContextActions(false)
                .SetHideInUI(false)
                .SetHideInCharacterSheetAndLevelUp(false)
                .SetHideNotAvailibleInUI(false)
                .SetRanks(1)
                .SetReapplyOnLevelUp(false)
                .SetIsClassFeature(false)
                .SetForAllOtherClasses(false)
                .SetLevelEntries(entries)
                .Configure();
        }
    }
}

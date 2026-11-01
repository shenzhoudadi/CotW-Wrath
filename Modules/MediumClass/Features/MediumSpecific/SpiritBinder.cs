using BlueprintCore.Blueprints.CustomConfigurators.Classes;
using BlueprintCore.Blueprints.CustomConfigurators.Classes.Selection;
using BlueprintCore.Blueprints.References;
using BlueprintCore.Utils;
using BlueprintCore.Utils.Types;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.UnitLogic;
using MediumClass.Utilities;
using MediumClass.Utils;
using static UnityModManagerNet.UnityModManager.ModEntry;

namespace MediumClass.Features.MediumSpecific
{
    /// <summary>
    /// 通灵者变体：缚灵者。英灵永不安宁，因此共鸣惩罚在降灵期间常驻、共鸣上限 +1，
    /// 代价换成 2/3/5/7/9/11 级的额外专长。
    /// </summary>
    internal static class SpiritBinder
    {
        private static readonly ModLogger Logger = Logging.GetLogger(nameof(SpiritBinder));
        private static BlueprintFeature marker;

        internal static bool HasFeature(UnitDescriptor owner)
        {
            if (owner == null) return false;
            var blueprint = marker ?? BlueprintTool.Get<BlueprintFeature>(Guids.SpiritBinderFeature);
            return blueprint != null && owner.HasFact(blueprint);
        }

        internal static void Configure()
        {
            // 缚灵者 1 级职业能力“永不安宁”：MediumInfluenceRules 通过 HasFeature
            // 读取的正是这个蓝图，这里只让它以独立能力卡出现在界面上。
            marker = FeatureConfigurator.New("MediumSpiritBinder", Guids.SpiritBinderFeature)
                .SetDisplayName("SpiritBinderRestlessSpirit.Name")
                .SetDescription("SpiritBinderRestlessSpirit.Description")
                .SetIcon("assets/icons/tranceofthree.png")
                .SetIsClassFeature()
                .SetRanks(1)
                .SetHideInUI(false)
                .SetHideInCharacterSheetAndLevelUp(false)
                .Configure();

            // 战斗专长与法师额外专长合并为一个额外专长池：这里嵌套原生两个选择，
            // 与 LoremasterSecretSelection 的做法一致，因此其它 mod 往这两个池里
            // 追加的专长会自动出现在缚灵者的选项中。
            var bonusFeat = FeatureSelectionConfigurator.New("MediumSpiritBinderBonusFeat", Guids.SpiritBinderBonusFeat)
                .SetDisplayName("SpiritBinderBonusFeat.Name")
                .SetDescription("SpiritBinderBonusFeat.Description")
                .SetIsClassFeature()
                .SetRanks(1)
                .SetReapplyOnLevelUp(false)
                .AddToAllFeatures(
                    FeatureSelectionRefs.FighterFeatSelection.Cast<BlueprintFeatureReference>(),
                    FeatureSelectionRefs.WizardFeatSelection.Cast<BlueprintFeatureReference>())
                .Configure();

            // 交给 New() 的父职业会在 Configure 完成时自动把变体挂到通灵者上。
            ArchetypeConfigurator.New("MediumSpiritBinderArchetype", Guids.SpiritBinderArchetype, Guids.Medium)
                .SetLocalizedName("SpiritBinderArchetype.Name")
                .SetLocalizedDescription("SpiritBinderArchetype.Description")
                .SetAddFeatures(LevelEntryBuilder.New()
                    .AddEntry(1, marker)
                    .AddEntry(2, bonusFeat)
                    .AddEntry(3, bonusFeat)
                    .AddEntry(5, bonusFeat)
                    .AddEntry(7, bonusFeat)
                    .AddEntry(9, bonusFeat)
                    .AddEntry(11, bonusFeat))
                .Configure();
            Logger.Log("Spirit Binder archetype configured for the Medium class.");
        }
    }
}

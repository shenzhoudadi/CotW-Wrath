// Psychic components adapted from the pinned CotW implementation (MIT).
using System;
using BlueprintCore.Blueprints.CustomConfigurators.UnitLogic.Abilities;
using BlueprintCore.Blueprints.CustomConfigurators.UnitLogic.Buffs;
using BlueprintCore.Blueprints.CustomConfigurators.Classes;
using BlueprintCore.Utils;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Prerequisites;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.ElementsSystem;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Commands.Base;
using Kingmaker.UnitLogic.FactLogic;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Mechanics.Actions;
using MediumClass.Utilities;

namespace MediumClass.Medium
{
    internal static class MediumPsychicBlueprints
    {
        internal static ContextDurationValue CenterSelfDuration() => new ContextDurationValue {
            Rate = DurationRate.Rounds, DiceType = Kingmaker.RuleSystem.DiceType.Zero,
            DiceCountValue = 0, BonusValue = 1
        };

        internal static void ConfigureEnabled()
        {
            BuffConfigurator.New("MediumPsychicCenterSelfBuff", Guids.MediumPsychicCenterSelfBuff)
                .SetDisplayName("MediumPsychicCenterSelf.Name")
                .SetDescription("MediumPsychicCenterSelf.Description")
                .SetIcon("assets/icons/spiritarchmage.png")
                .SetStacking(StackingType.Replace)
                .Configure();
            AbilityConfigurator.New("MediumPsychicCenterSelf", Guids.MediumPsychicCenterSelf)
                .SetDisplayName("MediumPsychicCenterSelf.Name")
                .SetDescription("MediumPsychicCenterSelf.Description")
                .SetIcon("assets/icons/spiritarchmage.png")
                .SetType(AbilityType.Special)
                .SetActionType(UnitCommand.CommandType.Move)
                .SetRange(AbilityRange.Personal)
                .SetCanTargetSelf(true)
                .SetEffectOnAlly(AbilityEffectOnUnit.Helpful)
                .AddComponent<AbilityEffectRunAction>(c => c.Actions = new ActionList { Actions = new GameAction[] {
                    new ContextActionApplyBuff {
                        m_Buff = BlueprintTool.GetRef<BlueprintBuffReference>(Guids.MediumPsychicCenterSelfBuff),
                        ToCaster = true, IsNotDispelable = true, DurationValue = CenterSelfDuration()
                    }
                } })
                .Configure();
            FeatureConfigurator.New("MediumPsychicCasting", Guids.MediumPsychicCastingFeature)
                .SetDisplayName("MediumPsychicCasting.Name")
                .SetDescription("MediumPsychicCasting.Description")
                .SetIcon("assets/icons/spiritarchmage.png")
                .SetIsClassFeature(true)
                .SetRanks(1)
                .AddComponent<AddFacts>(c => c.m_Facts = new[] {
                    BlueprintTool.GetRef<BlueprintUnitFactReference>(Guids.MediumPsychicCenterSelf)
                })
                .Configure();
            // Retain the old GUID as a hidden, source-owned compatibility wrapper.
            // Existing saves get the canonical feat through native AddFacts; new
            // characters can select only the common CotW Logical Spell blueprint.
            var legacy = FeatureConfigurator.New("MediumPsychicLogicalSpell", Guids.MediumPsychicLogicalSpell)
                .SetDisplayName("MediumPsychicLogicalSpell.Name")
                .SetDescription("MediumPsychicLogicalSpell.Description")
                .SetIcon("assets/icons/spiritarchmage.png")
                .SetIsClassFeature(false)
                .SetRanks(1);
            if (Main.Integrated) {
                legacy.SetHideInUI(true)
                    .AddComponent<AddFacts>(c => c.m_Facts = new[] {
                        BlueprintTool.GetRef<BlueprintUnitFactReference>("2d837d1d6122589dbf8792f67147eead")
                    }).Configure();
            } else legacy.AddToGroups(FeatureGroup.Feat)
                .AddComponent<AddMetamagicFeat>(c => c.Metamagic = MediumPsychicCasting.Logical)
                .AddComponent<PrerequisiteClassLevel>(c => {
                    c.m_CharacterClass = BlueprintTool.GetRef<BlueprintCharacterClassReference>(Guids.Medium);
                    c.Level = 1;
                })
                .Configure();
        }
    }
}

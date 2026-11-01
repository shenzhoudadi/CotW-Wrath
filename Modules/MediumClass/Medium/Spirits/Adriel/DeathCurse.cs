using BlueprintCore.Blueprints.Configurators.UnitLogic.ActivatableAbilities;
using Kingmaker.UnitLogic.ActivatableAbilities;
using Kingmaker.Blueprints.Facts;
using System.Linq;
using BlueprintCore.Actions.Builder;
using BlueprintCore.Blueprints.CustomConfigurators.Classes;
using BlueprintCore.Blueprints.CustomConfigurators.UnitLogic.Abilities;
using BlueprintCore.Blueprints.CustomConfigurators.UnitLogic.Buffs;
using BlueprintCore.Blueprints.References;
using BlueprintCore.Conditions.Builder;
using BlueprintCore.Conditions.Builder.ContextEx;
using BlueprintCore.Utils;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Enums.Damage;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Mechanics.Actions;
using MediumClass.Medium.NewActions;
using MediumClass.Medium.NewComponents;
using MediumClass.Medium.NewComponents.AbilitySpecific;
using MediumClass.Utilities;
using MediumClass.Utils;
using System.Collections.Generic;
using UnityEngine;
using static UnityModManagerNet.UnityModManager.ModEntry;

namespace MediumClass.Medium.Spirits.Adriel
{
    /// <summary>
    /// 阿德里尔的至高英灵之力：恶咒。
    ///
    /// 负能量、火焰、寒冷、电击、酸蚀五种能量各为一个独立开关（恶咒能力下的
    /// 变体），同时最多开启两种（0/1/2 种均可，负能量同样计数）；再次使用同一
    /// 变体即关闭该能量。已开两种时开启第三种会被拒绝并提示先关闭一种，不会
    /// 自动替换。降灵阿德里尔时，武器攻击挑战目标对每种已开启的能量各造成
    /// 1d6 额外伤害。上限在开关动作（ContextActionToggleDeathCurse）执行时判定，
    /// 变体按钮上的 AbilityRequirementDeathCurseSlot 只负责界面提示。
    /// </summary>
    class DeathCurse
    {
        private static readonly string FeatName = "MediumAdrielDeathCurse";
        private static readonly ModLogger Logger = Logging.GetLogger(nameof(DeathCurse));

        // No curse icon ships with the mod; borrow the native Bestow Curse spell's.
        private static Sprite Icon => AbilityRefs.BestowCurse.Reference.Get().Icon;

        private static readonly (string Energy, string AbilityGuid, string BuffGuid, DamageEnergyType Type)[] Energies = {
            ("Negative", Guids.MediumAdrielDeathCurseAbilityNegative, Guids.MediumAdrielDeathCurseBuffNegative, DamageEnergyType.NegativeEnergy),
            ("Fire", Guids.MediumAdrielDeathCurseAbilityFire, Guids.MediumAdrielDeathCurseBuffFire, DamageEnergyType.Fire),
            ("Cold", Guids.MediumAdrielDeathCurseAbilityCold, Guids.MediumAdrielDeathCurseBuffCold, DamageEnergyType.Cold),
            ("Electricity", Guids.MediumAdrielDeathCurseAbilityElectricity, Guids.MediumAdrielDeathCurseBuffElectricity, DamageEnergyType.Electricity),
            ("Acid", Guids.MediumAdrielDeathCurseAbilityAcid, Guids.MediumAdrielDeathCurseBuffAcid, DamageEnergyType.Acid),
        };

        private static readonly (string Pair, string AbilityGuid, string BuffGuid)[] LegacyPairs = {
            ("NegativeFire", Guids.MediumAdrielDeathCurseAbilityNegativeFire, Guids.MediumAdrielDeathCurseBuffNegativeFire),
            ("NegativeCold", Guids.MediumAdrielDeathCurseAbilityNegativeCold, Guids.MediumAdrielDeathCurseBuffNegativeCold),
            ("NegativeElectricity", Guids.MediumAdrielDeathCurseAbilityNegativeElectricity, Guids.MediumAdrielDeathCurseBuffNegativeElectricity),
            ("NegativeAcid", Guids.MediumAdrielDeathCurseAbilityNegativeAcid, Guids.MediumAdrielDeathCurseBuffNegativeAcid),
            ("FireCold", Guids.MediumAdrielDeathCurseAbilityFireCold, Guids.MediumAdrielDeathCurseBuffFireCold),
            ("FireElectricity", Guids.MediumAdrielDeathCurseAbilityFireElectricity, Guids.MediumAdrielDeathCurseBuffFireElectricity),
            ("FireAcid", Guids.MediumAdrielDeathCurseAbilityFireAcid, Guids.MediumAdrielDeathCurseBuffFireAcid),
            ("ColdElectricity", Guids.MediumAdrielDeathCurseAbilityColdElectricity, Guids.MediumAdrielDeathCurseBuffColdElectricity),
            ("ColdAcid", Guids.MediumAdrielDeathCurseAbilityColdAcid, Guids.MediumAdrielDeathCurseBuffColdAcid),
            ("ElectricityAcid", Guids.MediumAdrielDeathCurseAbilityElectricityAcid, Guids.MediumAdrielDeathCurseBuffElectricityAcid),
        };

        public static void ConfigureEnabled()
        {
            Logger.Log("Generating Adriel Death Curse");

            // Native BlueprintConverter throws for missing saved blueprint GUIDs.
            // Keep the old identities resolvable, without exposing or enabling them.
            foreach (var (pair, abilityGuid, buffGuid) in LegacyPairs)
            {
                BuffConfigurator.New(FeatName + "Buff" + pair, buffGuid)
                    .SetDisplayName(FeatName + ".Name")
                    .SetDescription(FeatName + ".Description")
                    .SetIcon(Icon)
                    .SetFlags(BlueprintBuff.Flags.HiddenInUi | BlueprintBuff.Flags.RemoveOnRest)
                    .SetStacking(StackingType.Replace)
                    .AddComponent<MediumDeathCurseComponent>()
                    .Configure();
                AbilityConfigurator.New(FeatName + "Ability" + pair, abilityGuid)
                    .SetDisplayName(FeatName + ".Name")
                    .SetDescription(FeatName + ".Description")
                    .SetIcon(Icon)
                    .SetType(AbilityType.Supernatural)
                    .Configure();
            }

            var adriel = BlueprintTool.GetRef<BlueprintCharacterClassReference>(Guids.Adriel);
            var variants = new List<Blueprint<BlueprintAbilityReference>>();
            var removeAll = ActionsBuilder.New();
            var modes = new List<Blueprint<BlueprintUnitFactReference>>();
            foreach (var (energy, abilityGuid, buffGuid, type) in Energies)
            {
                var buff = BuffConfigurator.New(FeatName + "Buff" + energy, buffGuid)
                    .SetDisplayName(FeatName + energy + ".Name")
                    .SetDescription(FeatName + ".Description")
                    .SetIcon(Icon)
                    .SetFlags(BlueprintBuff.Flags.RemoveOnRest)
                    .SetStacking(StackingType.Replace)
                    .AddComponent<MediumDeathCurseComponent>(c => c.Energies = new[] { type })
                    .Configure();
                var buffRef = buff.ToReference<BlueprintBuffReference>();
                buff.FxOnStart = buff.FxOnStart ?? new Kingmaker.ResourceLinks.PrefabLink();
                buff.FxOnRemove = buff.FxOnRemove ?? new Kingmaker.ResourceLinks.PrefabLink();
                var modeId = MediumDeathCurseModes.Modes.Single(m => m.Buff == buffGuid).Mode;
                var mode = ActivatableAbilityConfigurator.New(FeatName + "Mode" + energy, modeId)
                    .SetDisplayName(FeatName + energy + ".Name").SetDescription(FeatName + ".Description").SetIcon(Icon)
                    .SetBuff(buff).SetActivationType(AbilityActivationType.Immediately).SetDeactivateImmediately(true)
                    .SetActivateWithUnitCommand(Kingmaker.UnitLogic.Commands.Base.UnitCommand.CommandType.Free)
                    .SetDeactivateIfOwnerDisabled(true).SetDeactivateIfOwnerUnconscious(true)
                    .AddComponent<DeathCurseModeRestriction>(c => c.Energy = buffRef).Configure();
                modes.Add(mode);


                // Toggle: a second use of the same energy turns it off; the other
                // energies' buffs are separate blueprints and untouched. Switching
                // on is refused while two energies are already active (0.3.4).
                variants.Add(AbilityConfigurator.New(FeatName + "Ability" + energy, abilityGuid)
                    .SetDisplayName(FeatName + energy + ".Name")
                    .SetDescription(FeatName + ".Description")
                    .SetIcon(Icon)
                    .SetType(AbilityType.Supernatural)
                    .SetRange(AbilityRange.Personal)
                    .SetCanTargetSelf(true)
                    .SetActionType(Kingmaker.UnitLogic.Commands.Base.UnitCommand.CommandType.Free)
                    .AddComponent<AbilityRequirementSpecificSpirit>(c => c.Spirit = adriel)
                    .AddComponent<AbilityRequirementDeathCurseSlot>(c => c.m_Buff = buffRef)
                    .AddAbilityEffectRunAction(
                        actions: ActionsBuilder.New()
                            .Add<ContextActionToggleDeathCurse>(c => c.m_Buff = buffRef))
                    .Configure());

                removeAll = removeAll.Add<ContextActionRemoveBuff>(c => c.m_Buff = buffRef);
            }

            var ability = AbilityConfigurator.New(FeatName + "Ability", Guids.MediumAdrielDeathCurseAbility)
                .SetDisplayName(FeatName + ".Name")
                .SetDescription(FeatName + ".Description")
                .SetIcon(Icon)
                .SetType(AbilityType.Supernatural)
                .SetActionType(Kingmaker.UnitLogic.Commands.Base.UnitCommand.CommandType.Free)
                .AddAbilityVariants(variants)
                .Configure();

            // Losing the power (seance ended) switches every energy off so a
            // later Adriel seance starts with no curse toggled on.
            FeatureConfigurator.New(FeatName, Guids.MediumAdrielDeathCurse)
                .SetDisplayName(FeatName + ".Name")
                .SetDescription(FeatName + ".Description")
                .SetIcon(Icon)
                .SetIsClassFeature(true)
                .SetHideInUI(false)
                .SetHideNotAvailibleInUI(false)
                .AddFacts(modes)
                .AddFactContextActions(deactivated: removeAll)
                .Configure();
        }
    }
}

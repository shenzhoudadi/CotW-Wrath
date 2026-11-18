using BlueprintCore.Actions.Builder;
using BlueprintCore.Actions.Builder.ContextEx;
using BlueprintCore.Blueprints.CustomConfigurators;
using BlueprintCore.Blueprints.CustomConfigurators.Classes;
using BlueprintCore.Blueprints.CustomConfigurators.Classes.Selection;
using BlueprintCore.Blueprints.CustomConfigurators.UnitLogic.Abilities;
using BlueprintCore.Blueprints.CustomConfigurators.UnitLogic.Buffs;
using BlueprintCore.Blueprints.References;
using BlueprintCore.Conditions.Builder;
using BlueprintCore.Conditions.Builder.ContextEx;
using BlueprintCore.Utils;
using BlueprintCore.Utils.Types;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Prerequisites;
using Kingmaker.Blueprints.Classes.Selection;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.Designers.Mechanics.Facts;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Enums;
using Kingmaker.Enums.Damage;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Commands.Base;
using Kingmaker.UnitLogic.FactLogic;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Mechanics.Components;
using Kingmaker.Utility;
using MediumClass.Medium.NewActions;
using MediumClass.Medium.NewComponents;
using MediumClass.Medium.NewComponents.AbilitySpecific;
using MediumClass.Utilities;
using MediumClass.Utils;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MediumClass.Medium
{
    /// <summary>
    /// 3rd-level Spirit Affinity: one pick at 3/7/11/15/19.
    ///
    /// Every entry is either a native ability rebuilt from its own blueprint components or a
    /// runtime copy of another mod's selection list, so new content added by those mods shows up
    /// here without a hardcoded list. Entries whose backing mod is missing stay out of the
    /// selection entirely (see <see cref="SpiritAffinityOptional"/>).
    /// </summary>
    internal static class SpiritAffinity
    {
        internal const string SelectionName = "MediumSpiritAffinity";
        internal const int Picks = 5;
        private static readonly string[] PickLevels = { "3", "7", "11", "15", "19" };
        private static readonly UnityModManagerNet.UnityModManager.ModEntry.ModLogger Logger =
            Logging.GetLogger(nameof(SpiritAffinity));

        internal static void ConfigureEnabled()
        {
            var entries = new List<BlueprintFeature>
            {
                AlignedWeaponry(),
                AssuredSurge(),
                SpiritedFamiliar(),
                SpiritWarding(),
                SpiritWardingGreater(),
                Spiritsense(),
                SpiritsenseGreater(),
                SpiritualDefense(),
                SpiritAvatar(),
                Trapfinding(),
                UncannyDodge(),
                InsightFromBeyond(),
                InspiringSpirit(),
                LegendarySkill(),
                ForcefulMetamagic(),
                FortifiedResolve(),
                PlentifulMagic(),
            };

            FeatureSelectionConfigurator.New(SelectionName, Guids.MediumSpiritAffinity)
                .SetDisplayName("SpiritAffinity.Name")
                .SetDescription("SpiritAffinity.Description")
                .SetIcon("assets/icons/spiritsurge.png")
                .SetIsClassFeature(true)
                .SetRanks(Picks)
                .SetIgnorePrerequisites(false)
                .SetReapplyOnLevelUp(false)
                .SetHideInUI(false)
                .AddToAllFeatures(entries.Select(e => (Blueprint<BlueprintFeatureReference>)e).ToArray())
                .Configure();

            Logger.Log("Spirit Affinity: " + entries.Count + " entries at levels " + string.Join("/", PickLevels));
        }

        // ---------------------------------------------------------------- aligned weaponry

        /// <summary>Your weapon counts as having your own alignment when overcoming DR.</summary>
        private static BlueprintFeature AlignedWeaponry() =>
            FeatureConfigurator.New("MediumSpiritAffinityAlignedWeaponry", Guids.MediumSpiritAffinityAlignedWeaponry)
                .SetDisplayName("SpiritAffinity.AlignedWeaponry.Name")
                .SetDescription("SpiritAffinity.AlignedWeaponry.Description")
                .SetIcon("assets/icons/decisivestrikeattack.png")
                .SetIsClassFeature(true)
                .SetRanks(1)
                // Native component: AddAlignment + MyAlignment reads the owner's own alignment.
                .AddOutgoingPhysicalDamageProperty(addAlignment: true, myAlignment: true)
                .Configure();

        // ---------------------------------------------------------------- assured surge

        /// <summary>Roll the spirit surge dice twice and keep the higher result.</summary>
        private static BlueprintFeature AssuredSurge() =>
            FeatureConfigurator.New("MediumSpiritAffinityAssuredSurge", Guids.MediumSpiritAffinityAssuredSurge)
                .SetDisplayName("SpiritAffinity.AssuredSurge.Name")
                .SetDescription("SpiritAffinity.AssuredSurge.Description")
                .SetIcon("assets/icons/arcanesurge.png")
                .SetIsClassFeature(true)
                .SetRanks(1)
                .Configure();

        /// <summary>Read by <see cref="MediumSpiritSurgeComponent"/>; the only place surge dice are drawn.</summary>
        internal static bool HasAssuredSurge(Kingmaker.EntitySystem.Entities.UnitEntityData owner)
        {
            if (owner?.Descriptor == null) return false;
            var blueprint = BlueprintTool.Get<BlueprintFeature>(Guids.MediumSpiritAffinityAssuredSurge);
            return blueprint != null && owner.Descriptor.HasFact(blueprint);
        }

        // ---------------------------------------------------------------- spirited familiar

        /// <summary>
        /// A familiar. The native Rogue talent's list is copied at runtime so every familiar the
        /// game (or another mod) adds is offered here too; the master benefits are flat, so the
        /// medium's own level is the caster level without any class substitution.
        /// </summary>
        private static BlueprintFeature SpiritedFamiliar()
        {
            var native = BlueprintTool.Get<BlueprintFeatureSelection>(Guids.NativeRogueFamiliarSelection);
            var choices = native == null
                ? new BlueprintFeatureReference[0]
                : (native.m_AllFeatures ?? new BlueprintFeatureReference[0]);
            return FeatureSelectionConfigurator
                .New("MediumSpiritAffinitySpiritedFamiliar", Guids.MediumSpiritAffinitySpiritedFamiliar)
                .SetDisplayName("SpiritAffinity.SpiritedFamiliar.Name")
                .SetDescription("SpiritAffinity.SpiritedFamiliar.Description")
                .SetIcon("assets/icons/foregospirit1.png")
                .SetIsClassFeature(true)
                .SetRanks(1)
                .AddToAllFeatures(choices.Select(c => (Blueprint<BlueprintFeatureReference>)c).ToArray())
                .Configure();
        }

        // ---------------------------------------------------------------- spirit warding

        // Death effects and negative-energy channeling. Both are real SpellDescriptor flags, so the
        // bonus follows every channel-negative-harm ability (cleric, warpriest, lich, monsters)
        // without a hardcoded ability list. Death Ward uses the same two flags.
        private const SpellDescriptor Warded = SpellDescriptor.Death | SpellDescriptor.ChannelNegativeHarm;

        private static BlueprintFeature SpiritWarding() =>
            FeatureConfigurator.New("MediumSpiritAffinitySpiritWarding", Guids.MediumSpiritAffinitySpiritWarding)
                .SetDisplayName("SpiritAffinity.SpiritWarding.Name")
                .SetDescription("SpiritAffinity.SpiritWarding.Description")
                .SetIcon("assets/icons/guardiansmite.png")
                .SetIsClassFeature(true)
                .SetRanks(1)
                // Same pieces Death Ward uses for its +4 save.
                .AddSavingThrowBonusAgainstDescriptor(
                    bonus: ContextValues.Constant(4),
                    modifierDescriptor: ModifierDescriptor.Sacred,
                    spellDescriptor: new SpellDescriptorWrapper(Warded))
                .Configure();

        private static BlueprintFeature SpiritWardingGreater() =>
            FeatureConfigurator.New("MediumSpiritAffinitySpiritWardingGreater", Guids.MediumSpiritAffinitySpiritWardingGreater)
                .SetDisplayName("SpiritAffinity.SpiritWardingGreater.Name")
                .SetDescription("SpiritAffinity.SpiritWardingGreater.Description")
                .SetIcon("assets/icons/guardiansmite.png")
                .SetIsClassFeature(true)
                .SetRanks(1)
                .AddPrerequisiteFeature(Guids.MediumSpiritAffinitySpiritWarding)
                .AddPrerequisiteClassLevel(Guids.Medium, 15)
                .AddEnergyImmunity(DamageEnergyType.NegativeEnergy)
                .AddImmunityToEnergyDrain()
                .AddSpellImmunityToSpellDescriptor(descriptor: new SpellDescriptorWrapper(Warded))
                .AddBuffDescriptorImmunity(descriptor: new SpellDescriptorWrapper(Warded))
                .Configure();

        // ---------------------------------------------------------------- spiritsense

        private static BlueprintFeature Spiritsense() =>
            FeatureConfigurator.New("MediumSpiritAffinitySpiritsense", Guids.MediumSpiritAffinitySpiritsense)
                .SetDisplayName("SpiritAffinity.Spiritsense.Name")
                .SetDescription("SpiritAffinity.Spiritsense.Description")
                .SetIcon("assets/icons/foregospirit2.png")
                .SetIsClassFeature(true)
                .SetRanks(1)
                .AddBlindsense(new Feet(30f))
                .Configure();

        private static BlueprintFeature SpiritsenseGreater() =>
            FeatureConfigurator.New("MediumSpiritAffinitySpiritsenseGreater", Guids.MediumSpiritAffinitySpiritsenseGreater)
                .SetDisplayName("SpiritAffinity.SpiritsenseGreater.Name")
                .SetDescription("SpiritAffinity.SpiritsenseGreater.Description")
                .SetIcon("assets/icons/foregospirit2.png")
                .SetIsClassFeature(true)
                .SetRanks(1)
                .AddPrerequisiteFeature(Guids.MediumSpiritAffinitySpiritsense)
                .AddPrerequisiteClassLevel(Guids.Medium, 7)
                // The greater sense replaces the lesser one instead of stacking two ranges.
                .AddRemoveFeatureOnApply(Guids.MediumSpiritAffinitySpiritsense)
                .AddBlindsense(new Feet(60f), blindsight: true)
                .Configure();

        // ---------------------------------------------------------------- spiritual defense

        private static BlueprintFeature SpiritualDefense() =>
            FeatureConfigurator.New("MediumSpiritAffinitySpiritualDefense", Guids.MediumSpiritAffinitySpiritualDefense)
                .SetDisplayName("SpiritAffinity.SpiritualDefense.Name")
                .SetDescription("SpiritAffinity.SpiritualDefense.Description")
                .SetIcon("assets/icons/foregospirit3.png")
                .SetIsClassFeature(true)
                .SetRanks(1)
                .AddComponent<MediumSpiritualDefenseComponent>()
                .Configure();

        // ---------------------------------------------------------------- spirit avatar

        /// <summary>Your weapon is treated as ghost touch.</summary>
        private static BlueprintFeature SpiritAvatar() =>
            FeatureConfigurator.New("MediumSpiritAffinitySpiritAvatar", Guids.MediumSpiritAffinitySpiritAvatar)
                .SetDisplayName("SpiritAffinity.SpiritAvatar.Name")
                .SetDescription("SpiritAffinity.SpiritAvatar.Description")
                .SetIcon("assets/icons/foregospirit4.png")
                .SetIsClassFeature(true)
                .SetRanks(1)
                .AddOutgoingPhysicalDamageProperty(addReality: true, reality: DamageRealityType.Ghost)
                .Configure();

        // ---------------------------------------------------------------- trapfinding

        /// <summary>
        /// Half the medium level (at least +1) on Perception and Trickery, rebuilt from the native
        /// Trapfinding blueprint with the class swapped to Medium.
        /// </summary>
        private static BlueprintFeature Trapfinding() =>
            FeatureConfigurator.New("MediumSpiritAffinityTrapfinding", Guids.MediumSpiritAffinityTrapfinding)
                .SetDisplayName("SpiritAffinity.Trapfinding.Name")
                .SetDescription("SpiritAffinity.Trapfinding.Description")
                .SetIcon("assets/icons/trickstersedge.png")
                .SetIsClassFeature(true)
                .SetRanks(1)
                .AddContextRankConfig(
                    ContextRankConfigs.ClassLevel(new[] { Guids.Medium }, max: 20, min: 1).WithDiv2Progression())
                .AddContextStatBonus(StatType.SkillPerception, ContextValues.Rank())
                .AddContextStatBonus(StatType.SkillThievery, ContextValues.Rank())
                .Configure();

        // ---------------------------------------------------------------- uncanny dodge

        /// <summary>
        /// Never flat-footed, invisible attackers do not deny Dexterity, and cannot be flanked.
        /// The first two are exactly the native Uncanny Dodge components; the flank immunity is the
        /// native Improved Uncanny Dodge implementation (MechanicsFeatureType.CannotBeFlanked), which
        /// is how the game models it — there is no separate "four levels higher" rule in Wrath.
        /// </summary>
        private static BlueprintFeature UncannyDodge() =>
            FeatureConfigurator.New("MediumSpiritAffinityUncannyDodge", Guids.MediumSpiritAffinityUncannyDodge)
                .SetDisplayName("SpiritAffinity.UncannyDodge.Name")
                .SetDescription("SpiritAffinity.UncannyDodge.Description")
                .SetIcon("assets/icons/decisivestrike.png")
                .SetIsClassFeature(true)
                .SetRanks(1)
                .AddFlatFootedIgnore(type: FlatFootedIgnoreType.UncannyDodge)
                .AddCondition(UnitCondition.AttackOfOpportunityBeforeInitiative)
                .AddMechanicsFeature(AddMechanicsFeature.MechanicsFeatureType.CannotBeFlanked)
                .Configure();

        // ---------------------------------------------------------------- insight from beyond

        /// <summary>Gain a bonus feat for which you meet the prerequisites. May be taken multiple times.</summary>
        private static BlueprintFeature InsightFromBeyond()
        {
            var native = BlueprintTool.Get<BlueprintFeatureSelection>(Guids.NativeBasicFeatSelection);
            var choices = native?.m_AllFeatures ?? Array.Empty<BlueprintFeatureReference>();
            var selection = FeatureSelectionConfigurator
                .New("MediumSpiritAffinityInsightFromBeyond", Guids.MediumSpiritAffinityInsightFromBeyond)
                .SetDisplayName("SpiritAffinity.InsightFromBeyond.Name")
                .SetDescription("SpiritAffinity.InsightFromBeyond.Description")
                .SetIcon("assets/icons/foregospirit.png")
                .SetIsClassFeature(true)
                .SetRanks(Picks)
                .SetIgnorePrerequisites(false)
                .SetReapplyOnLevelUp(false)
                .SetAllFeatures(choices.Select(c => (Blueprint<BlueprintFeatureReference>)c).ToArray())
                .Configure();
            selection.m_Features = selection.m_AllFeatures;
            return selection;
        }

        // ---------------------------------------------------------------- inspiring spirit

        /// <summary>Allies within 30 ft gain a morale bonus equal to your spirit bonus on saves against charm, compulsion, and fear.</summary>
        private static BlueprintFeature InspiringSpirit()
        {
            var allyBuff = BuffConfigurator
                .New("MediumSpiritAffinityInspiringSpiritAllyBuff", Guids.MediumSpiritAffinityInspiringSpiritAllyBuff)
                .SetDisplayName("SpiritAffinity.InspiringSpirit.Name")
                .SetDescription("SpiritAffinity.InspiringSpirit.Description")
                .SetIcon("assets/icons/spiritmarshal.png")
                .SetStacking(StackingType.Replace)
                .AddComponent<InspiringSpiritSaveBonusComponent>()
                .Configure();

            var area = AbilityAreaEffectConfigurator
                .New("MediumSpiritAffinityInspiringSpiritArea", Guids.MediumSpiritAffinityInspiringSpiritArea)
                .SetShape(AreaEffectShape.Cylinder)
                .SetSize(new Feet(30f))
                .SetAffectEnemies(false)
                .SetAggroEnemies(false)
                .AddAbilityAreaEffectBuff(
                    buff: allyBuff,
                    condition: ConditionsBuilder.New().IsAlly().Build())
                .Configure();

            var areaBuff = BuffConfigurator
                .New("MediumSpiritAffinityInspiringSpiritAreaBuff", Guids.MediumSpiritAffinityInspiringSpiritAreaBuff)
                .SetDisplayName("SpiritAffinity.InspiringSpirit.Name")
                .SetDescription("SpiritAffinity.InspiringSpirit.Description")
                .SetIcon("assets/icons/spiritmarshal.png")
                .SetFlags(BlueprintBuff.Flags.HiddenInUi)
                .AddAreaEffect(area)
                .Configure();

            return FeatureConfigurator
                .New("MediumSpiritAffinityInspiringSpirit", Guids.MediumSpiritAffinityInspiringSpirit)
                .SetDisplayName("SpiritAffinity.InspiringSpirit.Name")
                .SetDescription("SpiritAffinity.InspiringSpirit.Description")
                .SetIcon("assets/icons/spiritmarshal.png")
                .SetIsClassFeature(true)
                .SetRanks(1)
                .AddAuraFeatureComponent(areaBuff)
                .Configure();
        }

        // ---------------------------------------------------------------- legendary skill

        /// <summary>Each time you channel a spirit, choose one skill: it becomes a class skill and gains extra ranks up to your medium level (capped at character level).</summary>
        private static BlueprintFeature LegendarySkill()
        {
            var resource = AbilityResourceConfigurator
                .New("MediumSpiritAffinityLegendarySkillResource", Guids.MediumSpiritAffinityLegendarySkillResource)
                .SetMaxAmount(ResourceAmountBuilder.New(1).Build())
                .Configure();

            var buffs = new List<BlueprintUnitFactReference>();
            foreach (var skill in StatTypeHelper.Skills)
            {
                string buffGuid = VariantId.Of("LegendarySkillBuff", skill.ToString());
                var buff = BuffConfigurator
                    .New("MediumSpiritAffinityLegendarySkill_" + skill, buffGuid)
                    .SetDisplayName("SpiritAffinity.LegendarySkill." + skill + ".Name")
                    .SetDescription("SpiritAffinity.LegendarySkill.Description")
                    .SetIcon(LegendarySkillIcon(skill))
                    .SetFlags(BlueprintBuff.Flags.RemoveOnRest)
                    .SetStacking(StackingType.Replace)
                    .AddClassSkill(skill)
                    .AddComponent<AddLegendarySkillRank>(c => c.Skill = skill)
                    .Configure();
                buffs.Add(buff.ToReference<BlueprintUnitFactReference>());
            }

            var ability = AbilityConfigurator
                .New("MediumSpiritAffinityLegendarySkillAbility", Guids.MediumSpiritAffinityLegendarySkillAbility)
                .SetDisplayName("SpiritAffinity.LegendarySkill.Name")
                .SetDescription("SpiritAffinity.LegendarySkill.Description")
                .SetIcon("assets/icons/trickstersedge.png")
                .SetType(AbilityType.Supernatural)
                .SetActionType(UnitCommand.CommandType.Free)
                .SetRange(AbilityRange.Personal)
                .SetCanTargetSelf(true)
                .AddComponent<AbilityRequirementHasSpirit>()
                .AddAbilityApplyFact(
                    hasDuration: false,
                    restriction: AbilityApplyFact.FactRestriction.CasterHasNoFact,
                    facts: buffs.Select(b => (Blueprint<BlueprintUnitFactReference>)b).ToList())
                .AddAbilityResourceLogic(amount: 1, isSpendResource: true, requiredResource: resource)
                .Configure();

            return FeatureConfigurator
                .New("MediumSpiritAffinityLegendarySkill", Guids.MediumSpiritAffinityLegendarySkill)
                .SetDisplayName("SpiritAffinity.LegendarySkill.Name")
                .SetDescription("SpiritAffinity.LegendarySkill.Description")
                .SetIcon("assets/icons/trickstersedge.png")
                .SetIsClassFeature(true)
                .SetRanks(1)
                .AddFacts(new() { ability })
                .AddAbilityResources(amount: 1, restoreAmount: true, resource: resource)
                .AddFactsChangeTrigger(
                    checkedFacts: new() { BlueprintTool.GetRef<BlueprintUnitFactReference>(Guids.MediumChannelSpiritPrimarySpiritBuff) },
                    onFactGainedActions: ActionsBuilder.New().Add<ContextActionResetLegendarySkill>(),
                    onFactLostActions: ActionsBuilder.New().Add<ContextActionResetLegendarySkill>())
                .Configure();
        }

        private static UnityEngine.Sprite LegendarySkillIcon(StatType skill) => skill switch
        {
            StatType.SkillAthletics => BlueprintCore.Blueprints.References.FeatureRefs.SkillFocusPhysique.Reference.Get().Icon,
            StatType.SkillMobility => BlueprintCore.Blueprints.References.FeatureRefs.SkillFocusAcrobatics.Reference.Get().Icon,
            StatType.SkillThievery => BlueprintCore.Blueprints.References.FeatureRefs.SkillFocusThievery.Reference.Get().Icon,
            StatType.SkillStealth => BlueprintCore.Blueprints.References.FeatureRefs.SkillFocusStealth.Reference.Get().Icon,
            StatType.SkillKnowledgeArcana => BlueprintCore.Blueprints.References.FeatureRefs.SkillFocusKnowledgeArcana.Reference.Get().Icon,
            StatType.SkillKnowledgeWorld => BlueprintCore.Blueprints.References.FeatureRefs.SkillFocusKnowledgeWorld.Reference.Get().Icon,
            StatType.SkillLoreNature => BlueprintCore.Blueprints.References.FeatureRefs.SkillFocusLoreNature.Reference.Get().Icon,
            StatType.SkillLoreReligion => BlueprintCore.Blueprints.References.FeatureRefs.SkillFocusLoreReligion.Reference.Get().Icon,
            StatType.SkillPerception => BlueprintCore.Blueprints.References.FeatureRefs.SkillFocusPerception.Reference.Get().Icon,
            StatType.SkillPersuasion => BlueprintCore.Blueprints.References.FeatureRefs.SkillFocusDiplomacy.Reference.Get().Icon,
            StatType.SkillUseMagicDevice => BlueprintCore.Blueprints.References.FeatureRefs.SkillFocusUseMagicDevice.Reference.Get().Icon,
            _ => BlueprintTool.Get<BlueprintFeature>(Guids.MediumSpiritAffinityLegendarySkill)?.Icon
        };

        // ---------------------------------------------------------------- forceful metamagic

        internal static BlueprintFeatureReference[] MetamagicFeatPool()
        {
            var native = BlueprintTool.Get<BlueprintFeatureSelection>(Guids.NativeBasicFeatSelection);
            if (native?.m_AllFeatures == null) return Array.Empty<BlueprintFeatureReference>();
            return native.m_AllFeatures
                .Where(r => r?.Get()?.GetComponent<AddMetamagicFeat>() != null)
                .ToArray();
        }

        /// <summary>Gain a bonus metamagic feat; as a swift action, apply a known metamagic feat to a medium spell without raising its slot or casting time, then accept influence equal to its adjustment.</summary>
        private static BlueprintFeature ForcefulMetamagic()
        {
            var variants = new List<Blueprint<BlueprintAbilityReference>>();
            var allBuffRefs = new List<BlueprintBuffReference>();
            var buffByMetamagic = new Dictionary<Metamagic, BlueprintBuff>();

            foreach (var metamagic in ForcefulMetamagicHelper.SupportedMetamagics)
            {
                string buffGuid = VariantId.Of("ForcefulMetamagicBuff", metamagic.ToString());
                var buff = BuffConfigurator
                    .New("MediumSpiritAffinityForcefulMetamagicBuff_" + metamagic, buffGuid)
                    .SetDisplayName("SpiritAffinity.ForcefulMetamagic.Name")
                    .SetDescription("SpiritAffinity.ForcefulMetamagic.Description")
                    .SetIcon("assets/icons/wildarcana2.png")
                    .SetFlags(BlueprintBuff.Flags.RemoveOnRest)
                    .SetStacking(StackingType.Replace)
                    .AddComponent<ForcefulMetamagicBuffComponent>(c => c.Metamagic = metamagic)
                    .Configure();
                buffByMetamagic[metamagic] = buff;
                allBuffRefs.Add(buff.ToReference<BlueprintBuffReference>());
            }

            foreach (var metamagic in ForcefulMetamagicHelper.SupportedMetamagics)
            {
                var buff = buffByMetamagic[metamagic];
                var actions = ActionsBuilder.New();
                foreach (var other in allBuffRefs)
                {
                    if (other.deserializedGuid != buff.AssetGuid)
                        actions.RemoveBuff(other);
                }
                actions.ApplyBuff(buff, ContextDuration.Fixed(1, DurationRate.Rounds), toCaster: true, isFromSpell: false, isNotDispelable: true);

                string variantGuid = VariantId.Of("ForcefulMetamagicAbility", metamagic.ToString());
                var variant = AbilityConfigurator
                    .New("MediumSpiritAffinityForcefulMetamagicAbility_" + metamagic, variantGuid)
                    .SetDisplayName("SpiritAffinity.ForcefulMetamagic.Name")
                    .SetDescription("SpiritAffinity.ForcefulMetamagic.Description")
                    .SetIcon("assets/icons/wildarcana2.png")
                    .SetType(AbilityType.Supernatural)
                    .SetActionType(UnitCommand.CommandType.Swift)
                    .SetRange(AbilityRange.Personal)
                    .SetCanTargetSelf(true)
                    .AddComponent<AbilityShowIfHasMetamagic>(c => c.Metamagic = metamagic)
                    .AddComponent<ForcefulMetamagicRestriction>(c => c.Metamagic = metamagic)
                    .AddAbilityEffectRunAction(actions: actions)
                    .Configure();
                variants.Add(variant);
            }

            var ability = AbilityConfigurator
                .New("MediumSpiritAffinityForcefulMetamagicAbility", Guids.MediumSpiritAffinityForcefulMetamagicAbility)
                .SetDisplayName("SpiritAffinity.ForcefulMetamagic.Name")
                .SetDescription("SpiritAffinity.ForcefulMetamagic.Description")
                .SetIcon("assets/icons/wildarcana2.png")
                .SetType(AbilityType.Supernatural)
                .SetActionType(UnitCommand.CommandType.Swift)
                .SetRange(AbilityRange.Personal)
                .SetCanTargetSelf(true)
                .AddAbilityVariants(variants)
                .Configure();

            var pool = MetamagicFeatPool();
            var selection = FeatureSelectionConfigurator
                .New("MediumSpiritAffinityForcefulMetamagic", Guids.MediumSpiritAffinityForcefulMetamagic)
                .SetDisplayName("SpiritAffinity.ForcefulMetamagic.Name")
                .SetDescription("SpiritAffinity.ForcefulMetamagic.Description")
                .SetIcon("assets/icons/wildarcana2.png")
                .SetIsClassFeature(true)
                .SetRanks(1)
                .SetIgnorePrerequisites(false)
                .SetReapplyOnLevelUp(false)
                .AddFacts(new() { ability })
                .SetAllFeatures(pool.Select(c => (Blueprint<BlueprintFeatureReference>)c).ToArray())
                .Configure();
            selection.m_Features = selection.m_AllFeatures;
            SyncForcefulMetamagicPresentation();
            return selection;
        }

        internal static void SyncForcefulMetamagicPresentation()
        {
            var byMetamagic = new Dictionary<Metamagic, BlueprintFeature>();
            foreach (var r in MetamagicFeatPool())
            {
                var feat = r?.Get();
                var comp = feat?.GetComponent<AddMetamagicFeat>();
                if (feat != null && comp != null && !byMetamagic.ContainsKey(comp.Metamagic))
                    byMetamagic[comp.Metamagic] = feat;
            }

            foreach (var metamagic in ForcefulMetamagicHelper.SupportedMetamagics)
            {
                if (!byMetamagic.TryGetValue(metamagic, out var feat)) continue;
                var buff = BlueprintTool.Get<BlueprintBuff>(VariantId.Of("ForcefulMetamagicBuff", metamagic.ToString()));
                if (buff != null)
                {
                    buff.m_DisplayName = feat.m_DisplayName;
                    buff.m_Description = feat.m_Description;
                    buff.m_Icon = feat.Icon;
                }
                var variant = BlueprintTool.Get<BlueprintAbility>(VariantId.Of("ForcefulMetamagicAbility", metamagic.ToString()));
                if (variant != null)
                {
                    variant.m_DisplayName = feat.m_DisplayName;
                    variant.m_Description = feat.m_Description;
                    variant.m_Icon = feat.Icon;
                }
            }
        }

        // ---------------------------------------------------------------- fortified resolve

        /// <summary>At 7th level, as a swift action accept 1 point of influence to end all fear and negative emotion effects on yourself.</summary>
        private static BlueprintFeature FortifiedResolve()
        {
            var ability = AbilityConfigurator
                .New("MediumSpiritAffinityFortifiedResolveAbility", Guids.MediumSpiritAffinityFortifiedResolveAbility)
                .SetDisplayName("SpiritAffinity.FortifiedResolve.Name")
                .SetDescription("SpiritAffinity.FortifiedResolve.Description")
                .SetIcon("assets/icons/spiritguardian.png")
                .SetType(AbilityType.Supernatural)
                .SetActionType(UnitCommand.CommandType.Swift)
                .SetRange(AbilityRange.Personal)
                .SetCanTargetSelf(true)
                .AddAbilityResourceLogic(amount: 1, isSpendResource: true, requiredResource: Guids.MediumInfluenceResource)
                .AddAbilityEffectRunAction(
                    actions: ActionsBuilder.New()
                        .Add<ContextActionSpiritInfluence>()
                        .RemoveBuffsByDescriptor(
                            spellDescriptor: SpellDescriptor.Fear | SpellDescriptor.Shaken | SpellDescriptor.Frightened | SpellDescriptor.NegativeEmotion,
                            notSelf: false))
                .Configure();

            return FeatureConfigurator
                .New("MediumSpiritAffinityFortifiedResolve", Guids.MediumSpiritAffinityFortifiedResolve)
                .SetDisplayName("SpiritAffinity.FortifiedResolve.Name")
                .SetDescription("SpiritAffinity.FortifiedResolve.Description")
                .SetIcon("assets/icons/spiritguardian.png")
                .SetIsClassFeature(true)
                .SetRanks(1)
                .AddPrerequisiteClassLevel(Guids.Medium, 7)
                .AddFacts(new() { ability })
                .Configure();
        }

        // ---------------------------------------------------------------- plentiful magic

        /// <summary>Gain one additional spell slot for each spell level of your spirit spellbook.</summary>
        private static BlueprintFeature PlentifulMagic() =>
            FeatureConfigurator
                .New("MediumSpiritAffinityPlentifulMagic", Guids.MediumSpiritAffinityPlentifulMagic)
                .SetDisplayName("SpiritAffinity.PlentifulMagic.Name")
                .SetDescription("SpiritAffinity.PlentifulMagic.Description")
                .SetIcon("assets/icons/spiritarchmage.png")
                .SetIsClassFeature(true)
                .SetRanks(1)
                .Configure();
    }
}

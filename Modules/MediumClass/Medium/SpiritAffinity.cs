using BlueprintCore.Blueprints.CustomConfigurators.Classes;
using BlueprintCore.Blueprints.CustomConfigurators.Classes.Selection;
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
using Kingmaker.UnitLogic.FactLogic;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Mechanics.Components;
using Kingmaker.Utility;
using MediumClass.Medium.NewComponents.AbilitySpecific;
using MediumClass.Utilities;
using MediumClass.Utils;
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
    }
}

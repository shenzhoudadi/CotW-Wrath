// COTW-0095 Insinuator: an antipaladin archetype that bargains with an outsider
// each day instead of serving a deity. The parent class, its smite, touch of
// corruption, cruelties, auras and fiendish boon belong to
// MicroscopicContentExpansion, so the module stays dormant when that provider is
// absent and never publishes a partial archetype. Auras, Personal Champion and
// Ambitious Bond are a later pass; this one ships the six core features.
using System;
using System.Collections.Generic;
using System.Linq;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Prerequisites;
using Kingmaker.Blueprints.Classes.Selection;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.Blueprints.Facts;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.Designers.EventConditionActionSystem.Actions;
using Kingmaker.ElementsSystem;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Enums;
using Kingmaker.Enums.Damage;
using Kingmaker.ResourceLinks;
using Kingmaker.RuleSystem;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components;
using Kingmaker.UnitLogic.Abilities.Components.AreaEffects;
using Kingmaker.UnitLogic.Abilities.Components.Base;
using Kingmaker.UnitLogic.Abilities.Components.CasterCheckers;
using Kingmaker.UnitLogic.Alignments;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Buffs.Components;
using Kingmaker.UnitLogic.Commands.Base;
using Kingmaker.UnitLogic.FactLogic;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Mechanics.Actions;
using Kingmaker.UnitLogic.Mechanics.Components;
using Kingmaker.UnitLogic.Mechanics.Conditions;
using Kingmaker.Utility;
using static CallOfTheWild.Wrath.Prototype.PsychicAssets;

namespace CallOfTheWild.Wrath.Prototype
{
    // The invocation menu only offers the outsider alignments the insinuator can
    // actually bargain with, so the six variants show the reachable ones instead
    // of six permanently greyed-out entries.
    [TypeId("6f0f5c1b8a2d4c1e9b7a3d5c0e8f2a41")]
    public sealed class AbilityShowIfCasterAlignment0095 : BlueprintComponent, IAbilityVisibilityProvider
    {
        public AlignmentMaskType Alignment = AlignmentMaskType.Any;

        public bool IsAbilityVisible(AbilityData ability)
        {
            var caster = ability?.Caster?.Unit;
            if (caster == null) return true;
            return (AlignmentExtension.ToMask(caster.Descriptor.Alignment.ValueRaw) & Alignment) != 0;
        }
    }

    // The barter is free only when the invoked outsider's alignment is the
    // insinuator's own; anything one step away needs the skill check.
    [TypeId("d2b6f08c4e7a41d5b93c0a6f1e8d7b24")]
    public sealed class ContextConditionCasterAlignmentExact0095 : ContextCondition
    {
        public AlignmentMaskType Alignment = AlignmentMaskType.TrueNeutral;

        protected override bool CheckCondition()
        {
            var caster = Context?.MaybeCaster;
            if (caster == null) return false;
            return AlignmentExtension.ToMask(caster.Descriptor.Alignment.ValueRaw) == Alignment;
        }

        protected override string GetConditionCaption() => "Caster alignment is exactly " + Alignment;
    }

    // A bargained outsider cannot be replaced until the next rest. Personal
    // Champion lifts that limit, which is why the cooldown buff is only applied
    // to insinuators who have not reached 20th level. The entry stays in the
    // menu and explains itself instead of vanishing for the rest of the day.
    [TypeId("e5a91c74b3d84612a0f7c2e9d5b83f16")]
    public sealed class AbilityCasterCanInvoke0095 : BlueprintComponent, IAbilityCasterRestriction
    {
        public BlueprintBuffReference Cooldown;

        public bool IsCasterRestrictionPassed(UnitEntityData caster) => !OnCooldown(caster);

        public string GetAbilityCasterRestrictionUIText()
            => "You have already bargained with an outsider today.";

        private bool OnCooldown(UnitEntityData caster)
        {
            var buff = Cooldown?.Get();
            if (caster == null || buff == null) return false;
            foreach (var active in caster.Buffs)
                if (active?.Blueprint != null && active.Blueprint.AssetGuid == buff.AssetGuid) return true;
            return false;
        }
    }

    // Smite impudence may not be declared against a creature of exactly the
    // outsider's alignment; anything else, including a one-step neighbour, can
    // be smitten. The alignment lives on the invocation buff, so the check reads
    // the caster's active buff rather than a fixed blueprint value.
    [TypeId("c81e3a5d7f924b06a8e2d4c9b1f70a53")]
    public sealed class AbilityTargetNotInsinuatorAlignment0095 : BlueprintComponent, IAbilityTargetRestriction
    {
        public bool IsTargetRestrictionPassed(UnitEntityData caster, TargetWrapper target)
            => !Insinuator0095.TargetSharesInvokedAlignment(caster, target?.Unit);

        public string GetAbilityTargetRestrictionUIText(UnitEntityData caster, TargetWrapper target)
            => Insinuator0095.SmiteTargetText;
    }

    internal static class Insinuator0095
    {
        internal const string ModuleName = "Antipaladin archetype: Insinuator";

        internal const string ArchetypeToken = "InsinuatorArchetype0095";
        internal const string InvocationFeatureToken = "InsinuatorInvocationFeature0095";
        internal const string InvocationBaseToken = "InsinuatorInvocationBase0095";
        internal const string InvocationAbilityToken = "InsinuatorInvocationAbility0095";
        internal const string InvocationBuffToken = "InsinuatorInvocationBuff0095";
        internal const string SmiteFeatureToken = "InsinuatorSmiteImpudenceFeature0095";
        internal const string SmiteExtraToken = "InsinuatorSmiteImpudenceExtraUse0095";
        internal const string SmiteAbilityToken = "InsinuatorSmiteImpudenceAbility0095";
        internal const string SmiteBuffToken = "InsinuatorSmiteImpudenceBuff0095";
        internal const string SmiteHpToken = "InsinuatorSmiteImpudenceHp0095";
        internal const string SelfishFeatureToken = "InsinuatorSelfishHealingFeature0095";
        internal const string SelfishAbilityToken = "InsinuatorSelfishHealingAbility0095";
        internal const string GreedsToken = "InsinuatorGreeds0095";
        internal const string ChannelFeatureToken = "InsinuatorChannelEnergyFeature0095";
        internal const string ChannelHealToken = "InsinuatorChannelEnergyHeal0095";
        internal const string ChannelHarmToken = "InsinuatorChannelEnergyHarm0095";
        internal const string BonusFeatToken = "InsinuatorBonusFeat0095";
        internal const string CooldownToken = "InsinuatorInvocationCooldown0095";
        internal const string SmiteBuff2Token = "InsinuatorSmiteImpudenceChampionBuff0095";
        internal const string EgoToken = "InsinuatorAuraOfEgo0095";
        internal const string AmbitionToken = "InsinuatorAuraOfAmbition0095";
        internal const string GloryToken = "InsinuatorAuraOfGlory0095";
        internal const string GloryAbilityToken = "InsinuatorAuraOfGloryAbility0095";
        internal const string GloryBuffToken = "InsinuatorAuraOfGloryBuff0095";
        internal const string GloryBuff2Token = "InsinuatorAuraOfGloryChampionBuff0095";
        internal const string BeliefToken = "InsinuatorAuraOfBelief0095";
        internal const string IndomitabilityToken = "InsinuatorAuraOfIndomitability0095";
        internal const string ChampionToken = "InsinuatorPersonalChampion0095";

        // MicroscopicContentExpansion antipaladin roots.
        internal const string ClassId = "8939eff2-5a0a-4b77-ad1a-b6be4c760a6c";
        internal const string SmiteFeatureId = "0585118f-63e7-4d23-aa3c-99668933decb";
        internal const string SmiteResourceId = "4b9e8e71-7bb0-45d5-beb4-dae7fc246bcd";
        internal const string SmiteAbilityId = "09a8551d-f5e0-41b4-a872-bd33f67d70a3";
        internal const string SmiteBuffId = "7ff9e143-dd21-4641-a7f5-3ee10d7e98b5";
        internal const string SmiteExtraUseId = "716b843d-c7b0-4f3c-bb48-5f212444b401";
        internal const string TouchFeatureId = "e6107a52-292a-4b30-9492-1c58ccc6daa2";
        internal const string TouchResourceId = "811ac097-8a9a-4c02-ab4d-7c247ef66ec1";
        internal const string CrueltySelectionId = "402fccae-3c21-47e7-8da4-ff9f6f061461";
        internal const string ChannelNegativeFeatureId = "06d82422-7f66-4c5f-bb0e-88901339ca91";
        internal const string CowardiceId = "d2dc39f8-b434-40b3-aa57-b208195b8c15";
        internal const string DespairId = "613f62fe-b703-4bde-9483-66720ed67901";
        internal const string SinId = "e3aca909-c2cf-431b-95c4-7f4eea939f6a";
        internal const string DepravityId = "eb074adf-add5-49b2-95d3-bab5afac3cfe";
        internal const string TipOfTheSpearId = "b291b873-49b6-49ea-b200-827e3896ffa5";
        internal const string CapstoneId = "b9ee3714-40b7-456b-a851-fbe75a97c7ad";

        // Native aura donors. AuraOfCourageBuff is the hidden carrier that owns
        // the area, AuraOfCourageArea the 10-foot cylinder it spawns.
        internal const string AuraCarrierDonorId = "c96380f6dcac83c45acdb698ae70ffc4";
        internal const string AuraAreaDonorId = "7ced0efa297bd5142ab749f6e33b112b";
        internal const string AuraOfCourageIconId = "e45ab30f49215054e83b4ea12165409f";
        internal const string BlessIconId = "90e59f4a4ada87243b7b3535a06d0638";
        internal const string DefensiveStanceIconId = "2a6a2f8e492ab174eb3f01acf5b7c90a";

        // Aura of Glory is Owlcat's own take on "lend the smite to the whole
        // party": the paladin's Aura of Justice applies a copy of the smite
        // buff whose bonuses reach every friend of the caster, not just him.
        internal const string AuraOfJusticeAbilityId = "7a4f0c48829952e47bb1fd1e4e9da83a";
        internal const string AuraOfJusticeBuffId = "ac3c66782859eb84692a8782320ffd2c";

        // Native donors.
        internal const string LayOnHandsSelfId = "8d6073201e5395d458b8251386d72df1";
        internal const string MercySelectionId = "02b187038a8dce545bb34bbfb346428d";
        internal const string PaladinClassId = "bfa11238e7ae3544bbeb4d0b92e897ec";
        internal const string ChannelHealDonorId = "f5fc9a1a2a3c1a946a31b320d1dd31b2";
        internal const string ChannelHarmDonorId = "279447a6bf2d3544d93a0a39c3b8e91d";
        internal const string FighterFeatSelectionId = "41c8486641f7d6d4283ca9dae4147a9f";
        internal const string SkillFocusSelectionId = "c9629ef9eebb88b479b2fbc5e836656a";
        internal const string InvocationIconId = "d3a4cb7be97a6694290f0dcfbd147113";

        internal static string LastSummary = "Not registered";

        internal const string SmiteTargetText =
            "You can't smite a creature whose alignment is exactly that of the outsider you invoked.";

        internal const string InvocationText =
            "At the start of each day, an insinuator can meditate to contact and barter with an outsider to empower him for a day. " +
            "An insinuator can freely invoke an outsider of his own alignment. He can instead invoke an outsider within one step of his own alignment " +
            "by succeeding at a Diplomacy skill check (DC = 15 + the insinuator's antipaladin level). " +
            "While invoking the power of an outsider, the insinuator radiates an alignment aura that matches the outsider's. " +
            "None of an insinuator's supernatural or spell-like class abilities function unless he has invoked the power of an outsider.";

        internal const string SmiteText =
            "Once per day, an insinuator can beseech the forces empowering him to punish their shared enemies. As a swift action, the insinuator chooses one target within sight to smite.\n" +
            "An insinuator cannot smite a creature whose alignment is exactly that of the outsider he has invoked for the day, " +
            "but any other creature, including one a single step away, can be smitten. " +
            "The insinuator adds his Charisma bonus on his attack rolls and half his insinuator level on all damage rolls made against the target of his smite.\n" +
            "Regardless of the target, the smite attack automatically bypasses any damage reduction the creature might possess. " +
            "In addition, each time the insinuator declares a smite, he gains a number of temporary hit points equal to his antipaladin level.\n" +
            "The smite effect remains until the target is defeated or the next time the insinuator rests and regains his uses of this ability. " +
            "At 4th level and at every 3 levels thereafter, the insinuator can use smite one additional time per day, to a maximum of seven times per day at 19th level.";

        internal const string SmiteExtraText =
            "At 4th level and at every 3 levels thereafter, the insinuator can use smite impudence one additional time per day, to a maximum of seven times per day at 19th level.";

        internal const string SelfishText =
            "Beginning at 2nd level, an insinuator can heal his wounds by touch. This is treated exactly like the paladin's lay on hands class feature, " +
            "except it can be used only to heal the insinuator and cannot be used on other creatures. It uses the antipaladin's touch of corruption uses per day.";

        internal const string GreedsText =
            "Beginning at 3rd level, an insinuator can heal himself of certain conditions. This functions as the mercy paladin class ability, " +
            "but these mercies can only be applied to the insinuator himself.";

        internal const string ChannelText =
            "At 4th level, an insinuator can channel negative energy, treating his antipaladin level as his effective cleric level. " +
            "If he invokes a neutral outsider for the day, he may instead choose to channel positive energy, but treats his effective cleric level as half his antipaladin level. " +
            "Using this ability consumes two uses of his selfish healing ability. This is a Charisma-based ability.";

        internal const string ChannelPositiveText =
            "When an insinuator invokes a neutral outsider, he may channel positive energy like a cleric, using half his antipaladin level as his effective cleric level. " +
            "Using this ability consumes two uses of his selfish healing ability. This is a Charisma-based ability.";

        internal const string BonusFeatText =
            "At 4th level, an insinuator gains one bonus feat, which must be selected from the list of combat feats or Skill Focus. " +
            "At 7th level and every 3 antipaladin levels thereafter, the insinuator gains one additional combat or Skill Focus feat.";

        internal const string ArchetypeText =
            "Between the selfless nobility of paladins and the chaotic menace of antipaladins, there exists a path of dedicated self-interest.\n" +
            "Shunning the ties that bind them to a single deity, insinuators embrace whatever forces help them achieve their own agenda and glory, " +
            "borrowing power to emulate divine warriors.\n" +
            "An insinuator has no spellbook. Instead he invokes an outsider each day: Smite Impudence, Selfish Healing, Greeds, Channel Energy and a combat or Skill Focus feat replace the antipaladin's smite good, touch of corruption, cruelties, channel negative energy and auras.";

        internal const string CooldownText =
            "This insinuator has already bargained with an outsider today. Only a Personal Champion may meditate again before resting.";

        internal const string EgoText =
            "At 3rd level, an insinuator radiates an aura that bolsters allies and deters enemies. " +
            "Each ally within 10 feet gains a +2 morale bonus on saving throws against fear effects. " +
            "Enemies within 10 feet take a -2 penalty on saving throws against fear effects. " +
            "This ability functions only while the insinuator is conscious, not if he is unconscious or dead.";

        internal const string AmbitionText =
            "At 8th level, enemies within 10 feet of an insinuator take a -1 penalty on all saving throws. " +
            "All allies within 10 feet gain a +1 bonus on all saving throws.\n" +
            "This penalty does not stack with the penalty from aura of ego. " +
            "This ability functions only while the insinuator is conscious, not if he is unconscious or dead.";

        internal const string GloryText =
            "At 11th level, an insinuator can expend two uses of his smite impudence ability to let every ally share his smite. " +
            "As a swift action, he designates one creature within sight: the insinuator and all of his allies treat that creature as the target of his smite impudence, " +
            "adding his Charisma bonus on attack rolls and half his insinuator level on damage rolls against it, and ignoring its damage reduction. " +
            "The effect lasts for 1 minute. A personal champion shares the doubled version of his smite instead.";

        internal const string BeliefText =
            "At 14th level, an insinuator's weapons are treated as chaos-aligned while he invokes a chaotic outsider, " +
            "law-aligned when he invokes a lawful outsider, or evil-aligned while he invokes an evil outsider.";

        internal const string IndomitabilityText =
            "At 17th level, an insinuator gains DR 10 that is bypassed by the alignment opposite of the outsider he has invoked for the day, " +
            "or DR 5/- while invoking a neutral outsider.";

        internal const string ChampionText =
            "At 20th level, an insinuator becomes a living embodiment of his selfish desires. " +
            "His damage reduction from aura of indomitability increases to 15 (or 10 while invoking a neutral outsider). " +
            "Whenever he uses smite impudence, he adds twice his full Charisma bonus to the attack roll and doubles his effective bonus damage gained from the smite. " +
            "In addition, he can invoke a new outsider patron by meditating again.";

        // Invoked outsider alignment, the caster alignments that may invoke it,
        // and the aura alignment it lends. The last column drives smite
        // impudence's shared-alignment refusal and the neutral check behind
        // positive channeling.
        private static readonly AlignmentMaskType[] Invoked =
        {
            AlignmentMaskType.LawfulEvil,
            AlignmentMaskType.NeutralEvil,
            AlignmentMaskType.ChaoticEvil,
            AlignmentMaskType.LawfulNeutral,
            AlignmentMaskType.TrueNeutral,
            AlignmentMaskType.ChaoticNeutral
        };

        // The archetype's own alignment gate. An insinuator is still an
        // antipaladin, so only the three evil alignments may take it; the
        // neutral outsiders remain reachable because a lawful evil caster may
        // invoke a lawful neutral outsider and so on (see CasterMasks). The gate
        // is declared on the archetype rather than inherited from the class so
        // that CharGenClassSelectorItemVM.IsArchetypeAvailable evaluates the
        // class with ignoreAlignment: true and the archetype force-sets the
        // alignment restriction during character creation.
        private static readonly AlignmentMaskType ArchetypeAlignmentGate = AlignmentMaskType.Evil;

        private static readonly Dictionary<AlignmentMaskType, AlignmentMaskType> CasterMasks =
            new Dictionary<AlignmentMaskType, AlignmentMaskType>
            {
                { AlignmentMaskType.LawfulEvil, AlignmentMaskType.NeutralEvil | AlignmentMaskType.LawfulEvil },
                { AlignmentMaskType.NeutralEvil, AlignmentMaskType.Evil },
                { AlignmentMaskType.ChaoticEvil, AlignmentMaskType.NeutralEvil | AlignmentMaskType.ChaoticEvil },
                { AlignmentMaskType.LawfulNeutral, AlignmentMaskType.LawfulEvil },
                { AlignmentMaskType.TrueNeutral, AlignmentMaskType.NeutralEvil },
                { AlignmentMaskType.ChaoticNeutral, AlignmentMaskType.ChaoticEvil }
            };

        private static readonly Dictionary<AlignmentMaskType, string> AlignmentNames =
            new Dictionary<AlignmentMaskType, string>
            {
                { AlignmentMaskType.LawfulEvil, "Lawful Evil" },
                { AlignmentMaskType.NeutralEvil, "Neutral Evil" },
                { AlignmentMaskType.ChaoticEvil, "Chaotic Evil" },
                { AlignmentMaskType.LawfulNeutral, "Lawful Neutral" },
                { AlignmentMaskType.TrueNeutral, "True Neutral" },
                { AlignmentMaskType.ChaoticNeutral, "Chaotic Neutral" }
            };

        // Live wiring, populated by Connect and cleared by its undo action. The
        // invocation is identified by the buff the insinuator is carrying, so a
        // later pass can read it without a UnitPart or a per-tick scan.
        private static readonly Dictionary<BlueprintGuid, AlignmentMaskType> InvocationByBuff =
            new Dictionary<BlueprintGuid, AlignmentMaskType>();

        // Smite impudence is refused only against a creature of exactly the
        // invoked outsider's alignment. CotW compared the broader "shared" mask
        // instead (lawful-or-evil for a lawful evil patron), which also locked
        // out chaotic evil targets even though they are a different alignment.
        internal static bool TargetSharesInvokedAlignment(UnitEntityData caster, UnitEntityData target)
        {
            if (caster == null || target == null) return false;
            var invoked = InvokedAlignment(caster);
            if (invoked == null) return false;
            return AlignmentExtension.ToMask(target.Descriptor.Alignment.ValueRaw) == invoked.Value;
        }

        internal static AlignmentMaskType? InvokedAlignment(UnitEntityData unit)
        {
            if (unit == null || InvocationByBuff.Count == 0) return null;
            foreach (var buff in unit.Buffs)
            {
                var blueprint = buff?.Blueprint;
                if (blueprint == null) continue;
                if (InvocationByBuff.TryGetValue(blueprint.AssetGuid, out var alignment)) return alignment;
            }
            return null;
        }

        internal static bool Register(BlueprintsCache cache)
        {
            if (MagusBatch01454.Existing(cache, ModuleName)) { LastSummary = "Already registered"; return true; }

            var cls = Mce<BlueprintCharacterClass>(cache, ClassId);
            if (cls == null) { LastSummary = "MicroscopicContentExpansion antipaladin not found."; return false; }
            if (cls.Archetypes.Any(a => a != null && a.AssetGuid != Id(ArchetypeToken) &&
                    a.name.IndexOf("Insinuator", StringComparison.OrdinalIgnoreCase) >= 0))
            { LastSummary = "Another Insinuator archetype is already attached."; return false; }

            var donors = new Dictionary<string, SimpleBlueprint>
            {
                { "smite feature", Mce<BlueprintFeature>(cache, SmiteFeatureId) },
                { "smite resource", Mce<BlueprintAbilityResource>(cache, SmiteResourceId) },
                { "smite ability", Mce<BlueprintAbility>(cache, SmiteAbilityId) },
                { "smite buff", Mce<BlueprintBuff>(cache, SmiteBuffId) },
                { "smite additional use", Mce<BlueprintFeature>(cache, SmiteExtraUseId) },
                { "touch of corruption", Mce<BlueprintFeature>(cache, TouchFeatureId) },
                { "touch resource", Mce<BlueprintAbilityResource>(cache, TouchResourceId) },
                { "cruelty selection", Mce<BlueprintFeatureSelection>(cache, CrueltySelectionId) },
                { "channel negative energy", Mce<BlueprintFeature>(cache, ChannelNegativeFeatureId) },
                { "aura of cowardice", Mce<BlueprintFeature>(cache, CowardiceId) },
                { "aura of despair", Mce<BlueprintFeature>(cache, DespairId) },
                { "aura of sin", Mce<BlueprintFeature>(cache, SinId) },
                { "aura of depravity", Mce<BlueprintFeature>(cache, DepravityId) },
                { "tip of the spear", Mce<BlueprintFeature>(cache, TipOfTheSpearId) },
                { "antipaladin capstone", Mce<BlueprintFeature>(cache, CapstoneId) }
            };
            var missing = donors.Where(p => p.Value == null).Select(p => p.Key).ToArray();
            if (missing.Length > 0)
            {
                LastSummary = "Insinuator is dormant; missing MCE roots: " + string.Join(", ", missing) + ".";
                return false;
            }
            var paladin = cache.Load(BlueprintGuid.Parse(PaladinClassId)) as BlueprintCharacterClass;
            var mercy = cache.Load(BlueprintGuid.Parse(MercySelectionId)) as BlueprintFeatureSelection;
            if (paladin == null || mercy == null)
            {
                LastSummary = "Insinuator is dormant; the paladin mercy catalogue is unavailable.";
                return false;
            }

            var f = new PsychicAssets(cache);
            var smiteFeature = (BlueprintFeature)donors["smite feature"];
            var smiteResource = (BlueprintAbilityResource)donors["smite resource"];
            var smiteAbility = (BlueprintAbility)donors["smite ability"];
            var smiteBuff = (BlueprintBuff)donors["smite buff"];
            var touchFeature = (BlueprintFeature)donors["touch of corruption"];
            var touchResource = (BlueprintAbilityResource)donors["touch resource"];
            var cruelty = (BlueprintFeatureSelection)donors["cruelty selection"];
            var channelNegative = (BlueprintFeature)donors["channel negative energy"];

            var icon = cache.Load(BlueprintGuid.Parse(InvocationIconId)) as BlueprintUnitFact;

            // --- Personal Champion and the invocation cooldown ---------------
            // The capstone is a marker: it lifts the daily cooldown, doubles the
            // smite by applying a second copy of its buff, and (through the
            // class-level DR table) raises the aura of indomitability.
            var champion = f.Feature(ChampionToken, "Personal Champion", ChampionText, Array.Empty<BlueprintComponent>());
            Set(champion, "m_Icon", cls.Icon);

            var cooldown = f.Buff(CooldownToken, "Invocation Cooldown", CooldownText);
            SetEnum(cooldown, "m_Flags", "RemoveOnRest, HiddenInUi");
            if (icon != null) Set(cooldown, "m_Icon", icon.Icon);

            // --- Invocation -------------------------------------------------
            var buffs = new Dictionary<AlignmentMaskType, BlueprintBuff>();
            var abilities = new Dictionary<AlignmentMaskType, BlueprintAbility>();
            foreach (var alignment in Invoked)
            {
                string name = "Invocation: " + AlignmentNames[alignment];
                var buff = f.Buff(InvocationBuffToken + alignment, name, InvocationText);
                SetEnum(buff, "m_Flags", "RemoveOnRest");
                if (icon != null) Set(buff, "m_Icon", icon.Icon);
                buffs.Add(alignment, buff);
            }
            var allBuffs = Invoked.Select(a => buffs[a]).ToArray();

            // 15 + antipaladin level is the barter check DC. BonusValue adds the
            // step value to the class level, the same way the Golden Dragon's
            // spell resistance reads 15 + character level.
            foreach (var alignment in Invoked)
            {
                var buff = buffs[alignment];
                string name = "Invocation: " + AlignmentNames[alignment];
                var exact = new ContextConditionCasterAlignmentExact0095 { Alignment = alignment };

                // Each ability owns its own rank config; sharing one instance
                // across six blueprints would alias their element graphs.
                var invocationDc = new ContextRankConfig { name = "InsinuatorInvocationDC" + alignment };
                SetEnum(invocationDc, "m_Type", "Default");
                SetEnum(invocationDc, "m_BaseValueType", "ClassLevel");
                SetEnum(invocationDc, "m_Progression", "BonusValue");
                Set(invocationDc, "m_StepLevel", 15);
                Set(invocationDc, "m_Class", new[] { Ref<BlueprintCharacterClassReference>(cls) });

                var actions = new List<GameAction>();
                foreach (var other in allBuffs) actions.Add(RemoveBuff(other));
                actions.Add(new Conditional
                {
                    ConditionsChecker = new ConditionsChecker { Conditions = new Condition[] { exact } },
                    IfTrue = Actions(ApplyPermanent(buff, true)),
                    IfFalse = Actions(BarterCheck(buff))
                });
                // One bargain a day, whether or not the outsider accepted; only
                // a Personal Champion may meditate again before resting.
                actions.Add(new Conditional
                {
                    ConditionsChecker = new ConditionsChecker { Conditions = new Condition[] { CasterHas(champion, true) } },
                    IfTrue = Actions(ApplyPermanent(cooldown, true)),
                    IfFalse = Actions()
                });

                var parts = new List<BlueprintComponent>
                {
                    new AbilityCasterAlignment { Alignment = CasterMasks[alignment] },
                    new AbilityShowIfCasterAlignment0095 { Alignment = CasterMasks[alignment] },
                    new AbilityCasterCanInvoke0095 { Cooldown = Ref<BlueprintBuffReference>(cooldown) },
                    invocationDc,
                    new AbilityEffectRunAction { SavingThrowType = SavingThrowType.Unknown, Actions = Actions(actions.ToArray()) }
                };
                // Without an effect the daily bargain looks like nothing
                // happened: borrow the antipaladin smite's spawn effect and
                // re-anchor it on the caster.
                var invocationFx = SpawnFx(smiteAbility, AbilitySpawnFxAnchor.Caster);
                if (invocationFx != null) parts.Add(invocationFx);

                var ability = f.Ability(InvocationAbilityToken + alignment, name, InvocationText,
                    AbilityType.Extraordinary, UnitCommand.CommandType.Standard, AbilityRange.Personal,
                    parts.ToArray());
                Set(ability, "m_IsFullRoundAction", true);
                if (icon != null) Set(ability, "m_Icon", icon.Icon);
                abilities.Add(alignment, ability);
            }

            var variants = new AbilityVariants();
            Set(variants, "m_Variants", Invoked.Select(a => Ref<BlueprintAbilityReference>(abilities[a])).ToArray());
            var invocationBase = f.Ability(InvocationBaseToken, "Invocation", InvocationText,
                AbilityType.Extraordinary, UnitCommand.CommandType.Standard, AbilityRange.Personal, variants);
            Set(invocationBase, "m_IsFullRoundAction", true);
            if (icon != null) Set(invocationBase, "m_Icon", icon.Icon);
            foreach (var ability in abilities.Values) Set(ability, "m_Parent", Ref<BlueprintAbilityReference>(invocationBase));

            var invocationFeature = f.Feature(InvocationFeatureToken, "Invocation", InvocationText,
                Facts(invocationBase));
            if (icon != null) Set(invocationFeature, "m_Icon", icon.Icon);

            // --- Smite Impudence --------------------------------------------
            // Wrath's smite buff sits on the smitten creature and lends its
            // bonuses only to the unit that applied it (CheckCaster), so the
            // donor needs no trimming: attack, damage and DR bypass are exactly
            // what tabletop smite impudence grants.
            var impudenceBuff = f.Clone(smiteBuff, SmiteBuffToken, "Smite Impudence", SmiteText);
            Set(impudenceBuff, "m_Icon", smiteBuff.Icon);
            // MCE's buff carries the attack bonus under an out-of-enum descriptor
            // value, which the engine treats as a non-stacking modifier: two
            // copies then keep only the higher one instead of adding up. The
            // native smite buffs use UntypedStackable, so restore that here and
            // Personal Champion's duplicate really doubles the Charisma bonus.
            StackableAttackBonus(impudenceBuff);

            // Personal Champion doubles the smite by stacking a second copy of
            // the same buff; the damage bonus is descriptor-free and already
            // stacks, the attack bonus only stacks once it is untyped.
            var impudenceBuff2 = f.Clone(impudenceBuff, SmiteBuff2Token, "Smite Impudence (Personal Champion)", SmiteText);
            Set(impudenceBuff2, "m_Icon", smiteBuff.Icon);
            StackableAttackBonus(impudenceBuff2);

            var hpBuff = f.Buff(SmiteHpToken, "", "", new BlueprintComponent[]
            {
                new Kingmaker.Designers.Mechanics.Buffs.TemporaryHitPointsFromAbilityValue
                {
                    Descriptor = ModifierDescriptor.UntypedStackable,
                    RemoveWhenHitPointsEnd = true,
                    Value = new ContextValue { ValueType = ContextValueType.Rank }
                },
                Rank(cls, "ClassLevel", "AsIs", 0, 0, AbilityRankType.Default)
            });
            SetEnum(hpBuff, "m_Flags", "HiddenInUi");
            Set(hpBuff, "m_Icon", smiteBuff.Icon);

            var impudenceAbility = f.Clone(smiteAbility, SmiteAbilityToken, "Smite Impudence", SmiteText);
            Set(impudenceAbility, "m_Parent", null);
            Set(impudenceAbility, "m_Icon", smiteAbility.Icon);
            // The smite lasts until the target drops or the insinuator rests;
            // the shared clone label would otherwise read "Instantaneous".
            Set(impudenceAbility, "LocalizedDuration",
                Text(SmiteAbilityToken, "duration", "Until the target is defeated or you rest"));
            // Half antipaladin level as the damage bonus, as in the tabletop
            // wording; the donor measured the paladin's smite progression.
            var damageRank = impudenceAbility.GetComponents<ContextRankConfig>()
                .FirstOrDefault(c => IsClassLevel(c));
            if (damageRank != null)
            {
                SetEnum(damageRank, "m_Progression", "Div2");
                Set(damageRank, "m_Class", new[] { Ref<BlueprintCharacterClassReference>(cls) });
            }
            // Smite impudence draws on the antipaladin's own smite pool, never
            // on the paladin's Smite Evil resource.
            RepointResource(impudenceAbility, smiteResource, null);
            // The smite marker lives on the target, so an already smitten
            // creature is refused before the buff is applied again. Matching the
            // donor exactly ("my buff is not on it yet") is what makes a second
            // smite against the same creature a no-op instead of a second copy.
            var alreadySmitten = new ContextConditionHasBuffFromCaster { Not = true };
            Set(alreadySmitten, "m_Buff", Ref<BlueprintBuffReference>(impudenceBuff));
            impudenceAbility.ComponentsArray = ReplaceRunAction(impudenceAbility,
                new Conditional
                {
                    ConditionsChecker = new ConditionsChecker
                    {
                        Conditions = new Condition[] { alreadySmitten }
                    },
                    IfTrue = Actions(ApplyPermanent(impudenceBuff, false), ApplyPermanent(hpBuff, true),
                        new Conditional
                        {
                            ConditionsChecker = new ConditionsChecker { Conditions = new Condition[] { CasterHas(champion) } },
                            IfTrue = Actions(ApplyPermanent(impudenceBuff2, false)),
                            IfFalse = Actions()
                        }),
                    IfFalse = Actions()
                })
                .Concat(new BlueprintComponent[]
                {
                    new AbilityTargetNotInsinuatorAlignment0095 { name = "InsinuatorSmiteTarget" },
                    RequiresInvocation(allBuffs)
                }).ToArray();

            var impudenceFeature = f.Feature(SmiteFeatureToken, "Smite Impudence", SmiteText,
                Facts(impudenceAbility), Grant(smiteResource));
            Set(impudenceFeature, "m_Icon", smiteAbility.Icon);

            var impudenceExtra = f.Feature(SmiteExtraToken, "Smite Impudence — Additional Use", SmiteExtraText,
                Increase(smiteResource, 1));
            impudenceExtra.Ranks = 10;
            impudenceExtra.ReapplyOnLevelUp = true;
            Set(impudenceExtra, "m_Icon", smiteAbility.Icon);

            // --- Selfish Healing --------------------------------------------
            // Lay on hands is gated to lawful good casters; an insinuator is
            // always evil, so the clone must restate the gate or the ability is
            // refused with "requires a specific alignment".
            var layOnHands = f.Native<BlueprintAbility>(LayOnHandsSelfId);
            var selfish = f.Clone(layOnHands, SelfishAbilityToken, "Selfish Healing", SelfishText);
            Set(selfish, "m_Parent", null);
            Set(selfish, "m_Icon", layOnHands.Icon);
            foreach (var gate in selfish.GetComponents<AbilityCasterAlignment>())
                gate.Alignment = AlignmentMaskType.Evil;
            RepointResource(selfish, touchResource, null);
            foreach (var config in selfish.GetComponents<ContextRankConfig>().Where(IsClassLevel))
                Set(config, "m_Class", new[] { Ref<BlueprintCharacterClassReference>(cls) });
            selfish.ComponentsArray = selfish.ComponentsArray
                .Concat(new BlueprintComponent[] { RequiresInvocation(allBuffs) }).ToArray();

            var selfishFeature = f.Feature(SelfishFeatureToken, "Selfish Healing", SelfishText,
                Facts(selfish), Grant(touchResource));
            Set(selfishFeature, "m_Icon", layOnHands.Icon);

            // --- Greeds ------------------------------------------------------
            // Greeds are the paladin's mercies, reused rather than rebuilt: the
            // selection points at the live mercy features so names, icons,
            // descriptions and any provider-added mercy come along, and the fake
            // class levels make every "paladin N" prerequisite count the
            // insinuator's antipaladin level instead.
            if (mercy.AllFeatures.Any(feature => feature == null))
                throw new InvalidOperationException("Insinuator Greeds needs resolved paladin mercies.");
            var greeds = f.Clone(mercy, GreedsToken, "Greeds", GreedsText);
            Set(greeds, "m_Icon", mercy.Icon);
            // The paladin catalogue files these under the "Mercy" header; the
            // insinuator's own UI group already names them.
            SetEnum(greeds, "Group", "None");
            Set(greeds, "m_Features", Array.Empty<BlueprintFeatureReference>());
            Set(greeds, "m_AllFeatures", mercy.AllFeatures.Select(Ref<BlueprintFeatureReference>).ToArray());
            var mercyLevels = new Kingmaker.Designers.Mechanics.Facts.ClassLevelsForPrerequisites
            {
                Modifier = 1,
                Summand = 0
            };
            Set(mercyLevels, "m_FakeClass", Ref<BlueprintCharacterClassReference>(paladin));
            Set(mercyLevels, "m_ActualClass", Ref<BlueprintCharacterClassReference>(cls));
            greeds.ComponentsArray = greeds.ComponentsArray
                .Concat(new BlueprintComponent[] { mercyLevels }).ToArray();

            // --- Channel Energy ----------------------------------------------
            // CotW treats every outsider without a good or evil component as a
            // neutral patron, so lawful and chaotic neutral count too.
            var neutralOutsiders = new[]
            {
                buffs[AlignmentMaskType.LawfulNeutral],
                buffs[AlignmentMaskType.TrueNeutral],
                buffs[AlignmentMaskType.ChaoticNeutral]
            };
            var channelHeal = Channel(f, ChannelHealToken, "Channel Positive Energy — Heal Living",
                "Channeling positive energy causes a burst that heals all living creatures in a 30-foot radius centered on the insinuator. " +
                "The amount healed is 1d6 points of damage plus 1d6 for every four insinuator levels beyond 2nd (2d6 at 6th, 3d6 at 10th, and so on). " +
                ChannelPositiveText, ChannelHealDonorId, cls, touchResource, neutralOutsiders);
            var channelHarm = Channel(f, ChannelHarmToken, "Channel Positive Energy — Harm Undead",
                "Channeling positive energy causes a burst that damages all undead creatures in a 30-foot radius centered on the insinuator. " +
                "The amount inflicted is 1d6 points of damage plus 1d6 for every four insinuator levels beyond 2nd (2d6 at 6th, 3d6 at 10th, and so on). " +
                "Creatures that take damage receive a Will save to halve it; the DC is 10 + 1/2 the insinuator's level + his Charisma modifier. " +
                ChannelPositiveText, ChannelHarmDonorId, cls, touchResource, neutralOutsiders);

            var channelFeature = f.Feature(ChannelFeatureToken, "Channel Energy", ChannelText,
                Facts(channelNegative, channelHeal, channelHarm));
            Set(channelFeature, "m_Icon", channelNegative.Icon);

            // --- Bonus Feat --------------------------------------------------
            var fighterFeats = f.Native<BlueprintFeatureSelection>(FighterFeatSelectionId);
            var skillFocus = f.Native<BlueprintFeatureSelection>(SkillFocusSelectionId);
            var bonusFeat = f.Clone(fighterFeats, BonusFeatToken, "Bonus Feat", BonusFeatText);
            Set(bonusFeat, "m_Icon", fighterFeats.Icon);
            var pool = bonusFeat.AllFeatures.ToList();
            if (!pool.Any(feat => feat != null && feat.AssetGuid == skillFocus.AssetGuid)) pool.Add(skillFocus);
            Set(bonusFeat, "m_AllFeatures", pool.Select(Ref<BlueprintFeatureReference>).ToArray());

            // --- Auras -------------------------------------------------------
            var egoIcon = cache.Load(BlueprintGuid.Parse(AuraOfCourageIconId)) as BlueprintUnitFact;
            var blessIcon = cache.Load(BlueprintGuid.Parse(BlessIconId)) as BlueprintUnitFact;
            var stanceIcon = cache.Load(BlueprintGuid.Parse(DefensiveStanceIconId)) as BlueprintUnitFact;
            var despair = (BlueprintFeature)donors["aura of despair"];

            // Aura of Ego: allies steady themselves against fear, enemies quail.
            var ego = AuraFeature(f, EgoToken, "Aura of Ego", EgoText, egoIcon);
            AttachAll(allBuffs, ego,
                Carrier(f, EgoToken + "AllyCarrier", Area(f, EgoToken + "AllyArea",
                    Effect(f, EgoToken + "AllyEffect", SaveAgainstFear(2, ModifierDescriptor.Morale)),
                    "Ally", false, 13.Feet(), new ContextConditionIsAlly())),
                Carrier(f, EgoToken + "EnemyCarrier", Area(f, EgoToken + "EnemyArea",
                    Effect(f, EgoToken + "EnemyEffect", SaveAgainstFear(-2, ModifierDescriptor.UntypedStackable)),
                    "Enemy", true, 13.Feet(), new ContextConditionIsEnemy())));

            // Aura of Ambition: every saving throw shifts one point in the
            // insinuator's favour, in both directions at once.
            var ambition = AuraFeature(f, AmbitionToken, "Aura of Ambition", AmbitionText, despair);
            AttachAll(allBuffs, ambition,
                Carrier(f, AmbitionToken + "AllyCarrier", Area(f, AmbitionToken + "AllyArea",
                    Effect(f, AmbitionToken + "AllyEffect", new Kingmaker.Designers.Mechanics.Buffs.BuffAllSavesBonus
                    {
                        Value = 1,
                        Descriptor = ModifierDescriptor.UntypedStackable
                    }),
                    "Ally", false, 13.Feet(), new ContextConditionIsAlly())),
                Carrier(f, AmbitionToken + "EnemyCarrier", Area(f, AmbitionToken + "EnemyArea",
                    Effect(f, AmbitionToken + "EnemyEffect", new Kingmaker.Designers.Mechanics.Buffs.BuffAllSavesBonus
                    {
                        Value = -1,
                        Descriptor = ModifierDescriptor.UntypedStackable
                    }),
                    "Enemy", true, 13.Feet(), new ContextConditionIsEnemy())));

            // Aura of Glory: the insinuator spends two smite uses to mark one
            // creature for the whole party. The donor buff reaches every friend
            // of its applier, so the allies fight with his numbers.
            var justiceAbility = f.Native<BlueprintAbility>(AuraOfJusticeAbilityId);
            var gloryBuff = f.Clone(f.Native<BlueprintBuff>(AuraOfJusticeBuffId), GloryBuffToken,
                "Smite Impudence (Aura of Glory)", GloryText);
            Set(gloryBuff, "m_Icon", justiceAbility.Icon);
            // The donor is Smite Evil's buff under another name, so it would show
            // Smite Evil's flash. The mark this aura leaves is the insinuator's own
            // smite, so it reuses Smite Impudence's effect instead of a hardcoded
            // asset id: whatever that buff spawns, this one spawns too.
            var smiteFx = impudenceBuff.FxOnStart;
            Set(gloryBuff, "FxOnStart", smiteFx == null ? new PrefabLink() : new PrefabLink { AssetId = smiteFx.AssetId });
            // Personal Champion doubles the shared smite as well: the second
            // copy stacks because the native buff's attack bonus is untyped.
            var gloryBuff2 = f.Clone(gloryBuff, GloryBuff2Token,
                "Smite Impudence (Aura of Glory, Personal Champion)", GloryText);
            Set(gloryBuff2, "m_Icon", justiceAbility.Icon);
            // The donor lends the smite only to the caster's friends, because a
            // paladin is already smiting on his own. This aura *is* the
            // insinuator's smite, so it has to reach him too.
            ShareWithCaster(gloryBuff);
            ShareWithCaster(gloryBuff2);
            // Both copies have to sum, not compete, or the aura carries a single
            // Charisma bonus however many copies sit on the creature.
            StackableAttackBonus(gloryBuff);
            StackableAttackBonus(gloryBuff2);

            var gloryAbility = f.Clone(justiceAbility, GloryAbilityToken, "Aura of Glory", GloryText);
            Set(gloryAbility, "m_Parent", null);
            Set(gloryAbility, "m_Icon", justiceAbility.Icon);
            // The clone would otherwise read "Instantaneous", the shared label
            // every cloned ability starts with.
            Set(gloryAbility, "LocalizedDuration", Text(GloryAbilityToken, "duration", "1 minute"));
            gloryAbility.ComponentsArray = gloryAbility.ComponentsArray
                .Where(c => c != null && c.GetType().Name != "AbilityCasterAlignment").ToArray();
            RepointResource(gloryAbility, smiteResource, 2);
            foreach (var config in gloryAbility.GetComponents<ContextRankConfig>().Where(IsClassLevel))
            {
                SetEnum(config, "m_Progression", "Div2");
                Set(config, "m_Class", new[] { Ref<BlueprintCharacterClassReference>(cls) });
            }
            // The mark lives on the creature, so an already marked target is
            // refused before the buff is applied again. The donor asks that
            // through the target's buff list and so does Smite Impudence; this
            // aura now uses the same question, and the doubled copy rides inside
            // the guard instead of beside it so the two always land together.
            var alreadyMarked = new ContextConditionHasBuffFromCaster { Not = true };
            Set(alreadyMarked, "m_Buff", Ref<BlueprintBuffReference>(gloryBuff));
            gloryAbility.ComponentsArray = ReplaceRunAction(gloryAbility,
                new Conditional
                {
                    ConditionsChecker = new ConditionsChecker { Conditions = new Condition[] { alreadyMarked } },
                    // Personal Champion shares the doubled smite as well, so the
                    // second copy rides along with the first. Nested exactly like
                    // Smite Impudence: one proven shape, two abilities.
                    IfTrue = Actions(ApplyMinutes(gloryBuff, 1),
                        new Conditional
                        {
                            ConditionsChecker = new ConditionsChecker { Conditions = new Condition[] { CasterHas(champion) } },
                            IfTrue = Actions(ApplyMinutes(gloryBuff2, 1)),
                            IfFalse = Actions()
                        }),
                    IfFalse = Actions()
                })
                .Concat(new BlueprintComponent[]
                {
                    new AbilityCasterAlignment { Alignment = AlignmentMaskType.Evil },
                    RequiresInvocation(allBuffs)
                }).ToArray();
            var glory = f.Feature(GloryToken, "Aura of Glory", GloryText, Facts(gloryAbility));
            Set(glory, "m_Icon", justiceAbility.Icon);

            // Aura of Belief: the insinuator's own weapons take on the invoked
            // outsider's alignment. A neutral patron lends no alignment at all.
            var belief = AuraFeature(f, BeliefToken, "Aura of Belief", BeliefText, blessIcon);
            foreach (var alignment in Invoked)
            {
                if (alignment == AlignmentMaskType.TrueNeutral) continue;
                Attach(buffs[alignment], belief, Effect(f, BeliefToken + alignment,
                    new AddOutgoingPhysicalDamageProperty
                    {
                        AddAlignment = true,
                        AffectAnyPhysicalDamage = true,
                        Alignment = OutgoingAlignment(alignment)
                    }));
            }

            // Aura of Indomitability: DR 10 against the opposite alignment, or
            // DR 5/- while a neutral outsider is invoked; the capstone adds a
            // further 5 through the same feature-list rank.
            var indomitability = AuraFeature(f, IndomitabilityToken, "Aura of Indomitability", IndomitabilityText, stanceIcon);
            foreach (var alignment in Invoked)
            {
                var bypass = IsNeutralOutsider(alignment) ? (DamageAlignment)0 : BypassedBy(alignment);
                var reduction = new AddDamageResistancePhysical
                {
                    Value = new ContextValue { ValueType = ContextValueType.Rank },
                    BypassedByAlignment = bypass != 0,
                    Alignment = bypass
                };
                var ranks = new ContextRankConfig { name = "InsinuatorDR" + alignment };
                SetEnum(ranks, "m_Type", "Default");
                SetEnum(ranks, "m_BaseValueType", "FeatureListRanks");
                SetEnum(ranks, "m_Progression", "MultiplyByModifier");
                Set(ranks, "m_StepLevel", 5);
                // FeatureListRanks counts every listed fact the unit has, and a
                // repeated entry counts twice: the Invocation feature is always
                // present, so neutral patrons take one slot and the others two,
                // with the capstone adding one more step of 5.
                var facts = new List<BlueprintFeatureReference>();
                for (int i = 0; i < (IsNeutralOutsider(alignment) ? 1 : 2); i++)
                    facts.Add(Ref<BlueprintFeatureReference>(invocationFeature));
                facts.Add(Ref<BlueprintFeatureReference>(champion));
                Set(ranks, "m_FeatureList", facts.ToArray());
                Attach(buffs[alignment], indomitability, Effect(f, IndomitabilityToken + alignment, reduction, ranks));
            }

            // --- Ambitious Bond ----------------------------------------------
            // The bond itself is MCE's fiendish boon; this feature only adds the
            // borrowed lawful property, and the alignment gate is attached to the
            // shared weapon toggles in the connect step below.
            var ambitiousBond = AmbitiousBond0095.Build(f, cache);

            // --- Archetype ---------------------------------------------------
            var archetype = SpellBatch01447.Archetype(f, cls, ArchetypeToken, "Insinuator", ArchetypeText,
                new[]
                {
                    ArchetypeBatch01445.Entry(1, invocationFeature, impudenceFeature),
                    ArchetypeBatch01445.Entry(2, selfishFeature),
                    ArchetypeBatch01445.Entry(3, greeds, ego),
                    ArchetypeBatch01445.Entry(4, channelFeature, bonusFeat, impudenceExtra),
                    ArchetypeBatch01445.Entry(5, ambitiousBond),
                    ArchetypeBatch01445.Entry(6, greeds),
                    ArchetypeBatch01445.Entry(7, bonusFeat, impudenceExtra),
                    ArchetypeBatch01445.Entry(8, ambition),
                    ArchetypeBatch01445.Entry(9, greeds),
                    ArchetypeBatch01445.Entry(10, bonusFeat, impudenceExtra),
                    ArchetypeBatch01445.Entry(11, glory),
                    ArchetypeBatch01445.Entry(12, greeds),
                    ArchetypeBatch01445.Entry(13, bonusFeat, impudenceExtra),
                    ArchetypeBatch01445.Entry(14, belief),
                    ArchetypeBatch01445.Entry(15, greeds),
                    ArchetypeBatch01445.Entry(16, bonusFeat, impudenceExtra),
                    ArchetypeBatch01445.Entry(17, indomitability),
                    ArchetypeBatch01445.Entry(18, greeds),
                    ArchetypeBatch01445.Entry(19, bonusFeat, impudenceExtra),
                    ArchetypeBatch01445.Entry(20, champion)
                },
                new[]
                {
                    // The removal rows must line up with MCE's own antipaladin
                    // progression level by level: ProgressionData.CanAddArchetype
                    // rejects the whole archetype when a removed feature is not
                    // in the class entry of that exact level, and the level-up
                    // action then silently refuses to attach it. MCE differs from
                    // Kingmaker's antipaladin here: tip of the spear is 11th, and
                    // the 20th level slot is the capstone selection that holds
                    // mark of vengeance.
                    ArchetypeBatch01445.Entry(1, smiteFeature),
                    ArchetypeBatch01445.Entry(2, touchFeature),
                    ArchetypeBatch01445.Entry(3, cruelty, (BlueprintFeature)donors["aura of cowardice"]),
                    ArchetypeBatch01445.Entry(4, (BlueprintFeature)donors["smite additional use"], channelNegative),
                    ArchetypeBatch01445.Entry(6, cruelty),
                    ArchetypeBatch01445.Entry(7, (BlueprintFeature)donors["smite additional use"]),
                    ArchetypeBatch01445.Entry(8, (BlueprintFeature)donors["aura of despair"]),
                    ArchetypeBatch01445.Entry(9, cruelty),
                    ArchetypeBatch01445.Entry(10, (BlueprintFeature)donors["smite additional use"]),
                    ArchetypeBatch01445.Entry(11, (BlueprintFeature)donors["tip of the spear"]),
                    ArchetypeBatch01445.Entry(12, cruelty),
                    ArchetypeBatch01445.Entry(13, (BlueprintFeature)donors["smite additional use"]),
                    ArchetypeBatch01445.Entry(14, (BlueprintFeature)donors["aura of sin"]),
                    ArchetypeBatch01445.Entry(15, cruelty),
                    ArchetypeBatch01445.Entry(16, (BlueprintFeature)donors["smite additional use"]),
                    ArchetypeBatch01445.Entry(17, (BlueprintFeature)donors["aura of depravity"]),
                    ArchetypeBatch01445.Entry(18, cruelty),
                    ArchetypeBatch01445.Entry(19, (BlueprintFeature)donors["smite additional use"]),
                    ArchetypeBatch01445.Entry(20, (BlueprintFeature)donors["antipaladin capstone"])
                });
            // No spellbook at all: the insinuator borrows power instead of
            // preparing spells, so the archetype drops the antipaladin's book
            // and stops counting as a divine caster.
            Set(archetype, "RemoveSpellbook", true);
            Set(archetype, "ChangeCasterType", true);
            Set(archetype, "IsDivineCaster", false);
            Set(archetype, "m_ReplaceSpellbook", null);
            Set(archetype, "m_Icon", cls.Icon);
            // Same shape as the native Drunken Master archetype: the archetype
            // carries its own alignment prerequisite, so the engine stops
            // applying the parent class one and lets the archetype set the
            // restriction itself. The mask stays the class's evil gate because
            // an insinuator is still an antipaladin; without the component the
            // engine falls back to the class prerequisites and the archetype is
            // shown as forbidden whenever that class gate is not met.
            archetype.ComponentsArray = new BlueprintComponent[]
            {
                new PrerequisiteAlignment
                {
                    Alignment = ArchetypeAlignmentGate,
                    ArchetypeAlignment = true,
                    IngorePrerequisiteCheck = true
                }
            };
            // LevelEntry.Features materialises a native lazy list; prime our own
            // rows before the strict publication definition reads them.
            foreach (var entry in archetype.AddFeatures.Concat(archetype.RemoveFeatures)) entry.Features.ToArray();
            foreach (var feature in f.Assets.Values.OfType<BlueprintFeature>()) feature.IsClassFeature = true;

            var group = MagusBatch01454.Group(invocationFeature, impudenceFeature, impudenceExtra, selfishFeature,
                greeds, channelFeature, bonusFeat, ego, ambition, glory, belief, indomitability, champion, ambitiousBond);
            var undoClass = MagusBatch01454.CaptureClass(cls);

            MagusBatch01454.Publish(ModuleName, f,
                () =>
                {
                    InvocationByBuff.Clear();
                    foreach (var alignment in Invoked)
                        InvocationByBuff.Add(buffs[alignment].AssetGuid, alignment);
                    MagusBatch01454.ConnectArchetype(cls, archetype, group);
                    MagusBatch01454.Determinators(cls, invocationFeature);
                    AmbitiousBond0095.Connect(cache, invocationFeature);
                },
                () =>
                {
                    AmbitiousBond0095.Revert();
                    undoClass();
                    InvocationByBuff.Clear();
                },
                () => Verify(cls, archetype, group, invocationFeature, invocationBase, impudenceAbility,
                    selfish, channelHeal, channelHarm, greeds, bonusFeat, allBuffs, touchResource,
                    smiteResource, skillFocus, cache, ambitiousBond, new Auras
                    {
                        Ego = ego,
                        Ambition = ambition,
                        Glory = glory,
                        GloryAbility = gloryAbility,
                        Belief = belief,
                        Indomitability = indomitability,
                        Champion = champion,
                        Cooldown = cooldown,
                        Smite = impudenceBuff,
                        DoubledSmite = impudenceBuff2
                    }));

            LastSummary = "Registered: Invocation (six outsider alignments, one bargain a day, 15 + antipaladin level barter check), " +
                "Smite Impudence with temp HP and shared-alignment refusal, Selfish Healing, Greeds, " +
                "Channel Energy (negative, plus positive at half level while a neutral outsider is invoked), " +
                "a combat or Skill Focus bonus feat, the five auras (ego, ambition, glory, belief, indomitability), " +
                "Personal Champion, Ambitious Bond (weapon properties gated by the invoked alignment) and no spellbook.";
            return true;
        }

        private static BlueprintAbility Channel(PsychicAssets f, string token, string name,
            string description, string donorId, BlueprintCharacterClass cls, BlueprintAbilityResource resource,
            BlueprintBuff[] neutralOutsiders)
        {
            var donor = f.Native<BlueprintAbility>(donorId);
            var ability = f.Clone(donor, token, name, description);
            Set(ability, "m_Parent", null);
            Set(ability, "m_Icon", donor.Icon);
            // The cleric donor heals its own party on rest; a bargained channel
            // must not add a second rest heal.
            ability.ComponentsArray = ability.ComponentsArray
                .Where(c => c != null && c.GetType().Name != "AbilityUseOnRest").ToArray();
            RepointResource(ability, resource, 2);
            foreach (var config in ability.GetComponents<ContextRankConfig>())
            {
                if (!IsClassLevel(config)) continue;
                // Effective cleric level is half the antipaladin level: 1d6 at
                // 4th, 2d6 at 6th, 3d6 at 10th.
                SetEnum(config, "m_Progression", "StartPlusDivStep");
                Set(config, "m_StartLevel", 2);
                Set(config, "m_StepLevel", 4);
                Set(config, "m_Class", new[] { Ref<BlueprintCharacterClassReference>(cls) });
            }
            var parameters = new ContextCalculateAbilityParamsBasedOnClass { StatType = StatType.Charisma };
            Set(parameters, "m_CharacterClass", Ref<BlueprintCharacterClassReference>(cls));
            ability.ComponentsArray = ability.ComponentsArray.Concat(new BlueprintComponent[]
            {
                parameters,
                new AbilityCasterAlignment { Alignment = AlignmentMaskType.Evil },
                RequiresInvocation(neutralOutsiders)
            }).ToArray();
            return ability;
        }

        // Every supernatural insinuator ability is gated on a live invocation.
        // The buff itself is the marker, so no UnitPart or per-tick scan is
        // needed; the native component also hides the entry while it is missing.
        private static AbilityCasterHasFacts RequiresInvocation(BlueprintBuff[] buffs)
        {
            var component = new AbilityCasterHasFacts { NeedsAll = false };
            Set(component, "m_Facts", buffs.Select(Ref<BlueprintUnitFactReference>).ToArray());
            return component;
        }

        // --- Auras ---------------------------------------------------------
        // The native four-piece: a carrier buff owns an area effect and the area
        // applies one effect buff per qualifying unit. Cloning the paladin's
        // aura keeps Owlcat's cylinder, target type and spawn behaviour instead
        // of hand-building an area blueprint. The carrier is hung off the
        // invocation buff rather than a feature, because an insinuator's auras
        // only radiate while an outsider empowers him.
        // m_TargetType is a private nested enum of BlueprintAbilityAreaEffect,
        // not the public TargetType used by abilities, so it has to be written
        // by member name; Set would reject the foreign enum type.
        private static BlueprintAbilityAreaEffect Area(PsychicAssets f, string token, BlueprintBuff effect,
            string targetType, bool affectEnemies, Feet size, params Condition[] conditions)
        {
            var donor = f.Native<BlueprintAbilityAreaEffect>(AuraAreaDonorId);
            var area = f.Clone(donor, token, "", "");
            area.Size = size;
            SetEnum(area, "m_TargetType", targetType);
            Set(area, "AffectEnemies", affectEnemies);
            var applier = area.GetComponent<AbilityAreaEffectBuff>()
                ?? throw new InvalidOperationException("Aura area donor lost its AbilityAreaEffectBuff.");
            Set(applier, "m_Buff", Ref<BlueprintBuffReference>(effect));
            Set(applier, "Condition", new ConditionsChecker { Conditions = conditions });
            return area;
        }

        private static BlueprintBuff Carrier(PsychicAssets f, string token, BlueprintAbilityAreaEffect area)
        {
            var donor = f.Native<BlueprintBuff>(AuraCarrierDonorId);
            var carrier = f.Clone(donor, token, "", "");
            var component = new AddAreaEffect { name = "InsinuatorArea" + token };
            Set(component, "m_AreaEffect", Ref<BlueprintAbilityAreaEffectReference>(area));
            carrier.ComponentsArray = new BlueprintComponent[] { component };
            SetEnum(carrier, "m_Flags", "HiddenInUi");
            return carrier;
        }

        private static BlueprintBuff Effect(PsychicAssets f, string token, params BlueprintComponent[] components)
        {
            var buff = f.Buff(token, "", "", components);
            SetEnum(buff, "m_Flags", "HiddenInUi");
            return buff;
        }

        // The aura itself is only a marker: the carrier reads it, so the aura
        // can be granted at a level without radiating outside an invocation.
        private static BlueprintFeature AuraFeature(PsychicAssets f, string token, string name, string description,
            BlueprintUnitFact icon)
        {
            var feature = f.Feature(token, name, description, Array.Empty<BlueprintComponent>());
            if (icon != null) Set(feature, "m_Icon", icon.Icon);
            return feature;
        }

        // A private copy of a native spawn effect: component instances may not
        // be shared between blueprints, but the prefab link itself is a resource
        // handle and is meant to be reused.
        private static AbilitySpawnFx SpawnFx(BlueprintAbility donor, AbilitySpawnFxAnchor anchor)
        {
            var source = donor?.GetComponent<AbilitySpawnFx>();
            if (source?.PrefabLink == null) return null;
            return new AbilitySpawnFx
            {
                PrefabLink = source.PrefabLink,
                Time = source.Time,
                Anchor = anchor,
                WeaponTarget = source.WeaponTarget
            };
        }

        private static ContextConditionCasterHasFact CasterHas(BlueprintUnitFact fact, bool not = false)
        {
            var condition = new ContextConditionCasterHasFact { Not = not };
            Set(condition, "m_Fact", Ref<BlueprintUnitFactReference>(fact));
            return condition;
        }

        // Hang a carrier on an invocation buff: the buff's own activation runs
        // the conditional, and AsChild ties the carrier's lifetime to it. A
        // fresh AddFactContextActions leaves its lists null, and the native
        // engine runs them unconditionally, so fill all three.
        private static void Attach(BlueprintBuff invocation, BlueprintFeature aura, BlueprintBuff carrier)
        {
            var context = invocation.GetComponent<AddFactContextActions>();
            if (context == null)
            {
                context = new AddFactContextActions();
                invocation.ComponentsArray = invocation.ComponentsArray
                    .Concat(new BlueprintComponent[] { context }).ToArray();
            }
            context.Activated = context.Activated ?? Empty();
            context.Deactivated = context.Deactivated ?? Empty();
            context.NewRound = context.NewRound ?? Empty();

            var apply = ApplyPermanent(carrier, true);
            apply.AsChild = true;
            var conditional = new Conditional
            {
                ConditionsChecker = new ConditionsChecker { Conditions = new Condition[] { CasterHas(aura) } },
                IfTrue = Actions(apply),
                IfFalse = Actions()
            };
            context.Activated.Actions = context.Activated.Actions
                .Concat(new GameAction[] { conditional }).ToArray();
        }

        private static ActionList Empty() => new ActionList { Actions = Array.Empty<GameAction>() };

        // MCE stores the smite's attack bonus under an out-of-enum descriptor
        // value. The engine groups modifiers by descriptor and keeps only the
        // highest of a group, so a duplicate buff never added up and Personal
        // Champion's "twice your Charisma bonus" stayed at one times. The native
        // smite buffs use UntypedStackable, which is what makes copies stack.
        private static void StackableAttackBonus(BlueprintBuff buff)
        {
            foreach (var bonus in buff.GetComponents<Kingmaker.Designers.Mechanics.Buffs.AttackBonusAgainstTarget>())
                bonus.Descriptor = ModifierDescriptor.UntypedStackable;
        }

        private static void AttachAll(BlueprintBuff[] invocations, BlueprintFeature aura, params BlueprintBuff[] carriers)
        {
            foreach (var invocation in invocations)
                foreach (var carrier in carriers) Attach(invocation, aura, carrier);
        }

        private static DamageAlignment OutgoingAlignment(AlignmentMaskType alignment)
        {
            var result = (DamageAlignment)0;
            if ((alignment & AlignmentMaskType.Lawful) != 0) result |= DamageAlignment.Lawful;
            if ((alignment & AlignmentMaskType.Chaotic) != 0) result |= DamageAlignment.Chaotic;
            if ((alignment & AlignmentMaskType.Good) != 0) result |= DamageAlignment.Good;
            if ((alignment & AlignmentMaskType.Evil) != 0) result |= DamageAlignment.Evil;
            return result;
        }

        // DR 10 is bypassed by the opposite alignment; a neutral outsider grants
        // plain DR 5/-. CotW inverts each component, so an evil patron is
        // bypassed by good weapons.
        private static DamageAlignment BypassedBy(AlignmentMaskType alignment)
        {
            var result = (DamageAlignment)0;
            if ((alignment & AlignmentMaskType.Lawful) != 0) result |= DamageAlignment.Chaotic;
            if ((alignment & AlignmentMaskType.Chaotic) != 0) result |= DamageAlignment.Lawful;
            if ((alignment & AlignmentMaskType.Good) != 0) result |= DamageAlignment.Evil;
            if ((alignment & AlignmentMaskType.Evil) != 0) result |= DamageAlignment.Good;
            return result;
        }

        private static ContextRankConfig Rank(BlueprintCharacterClass cls, string baseValue, string progression,
            int start, int step, AbilityRankType type)
        {
            var config = new ContextRankConfig { name = "InsinuatorRank" };
            SetEnum(config, "m_Type", type.ToString());
            SetEnum(config, "m_BaseValueType", baseValue);
            SetEnum(config, "m_Progression", progression);
            Set(config, "m_StartLevel", start);
            Set(config, "m_StepLevel", step);
            Set(config, "m_Class", new[] { Ref<BlueprintCharacterClassReference>(cls) });
            return config;
        }

        private static bool IsClassLevel(ContextRankConfig config)
        {
            var kind = Get(config, "m_BaseValueType");
            return kind is ContextRankBaseValueType type &&
                   (type == ContextRankBaseValueType.ClassLevel ||
                    type == ContextRankBaseValueType.SummClassLevelWithArchetype);
        }

        // A cloned ability keeps the donor's resource logic. Pointing it at the
        // antipaladin's own pool is what makes Selfish Healing and both channels
        // spend the insinuator's uses rather than the paladin's or the cleric's.
        private static void RepointResource(BlueprintAbility ability, BlueprintAbilityResource resource, int? amount)
        {
            var logics = ability.GetComponents<AbilityResourceLogic>().ToArray();
            if (logics.Length == 0)
            {
                ability.ComponentsArray = ability.ComponentsArray
                    .Concat(new BlueprintComponent[] { ArchetypeBatch01445.Cost(resource, amount ?? 1) }).ToArray();
                return;
            }
            foreach (var logic in logics)
            {
                Set(logic, "m_RequiredResource", Ref<BlueprintAbilityResourceReference>(resource));
                Set(logic, "m_IsSpendResource", true);
                if (amount.HasValue) logic.Amount = amount.Value;
            }
        }

        private static BlueprintComponent[] ReplaceRunAction(BlueprintAbility ability, params GameAction[] actions)
        {
            var run = ability.GetComponent<AbilityEffectRunAction>();
            if (run == null)
                return ability.ComponentsArray
                    .Concat(new BlueprintComponent[] { new AbilityEffectRunAction { SavingThrowType = SavingThrowType.Unknown, Actions = Actions(actions) } })
                    .ToArray();
            run.Actions = Actions(actions);
            return ability.ComponentsArray;
        }

        // The Aura of Justice buff reaches friends of its applier only, so the
        // caster of the aura is the one unit it skips. Every offensive and
        // defensive half of the shared smite has to include him as well; the AC
        // component protects the defender from the marked creature, which is what
        // an ally wants and what the insinuator wants too.
        private static void ShareWithCaster(BlueprintBuff buff)
        {
            foreach (var component in buff.ComponentsArray ?? Array.Empty<BlueprintComponent>())
            {
                switch (component)
                {
                    case Kingmaker.Designers.Mechanics.Buffs.AttackBonusAgainstTarget attack:
                        attack.CheckCaster = true;
                        break;
                    case Kingmaker.Designers.Mechanics.Buffs.DamageBonusAgainstTarget damage:
                        damage.CheckCaster = true;
                        break;
                    case Kingmaker.Designers.Mechanics.Buffs.IgnoreTargetDR ignore:
                        ignore.CheckCaster = true;
                        break;
                    case Kingmaker.Designers.Mechanics.Buffs.ACBonusAgainstTarget armor:
                        armor.CheckCaster = true;
                        break;
                }
            }
        }

        private static ContextActionApplyBuff ApplyPermanent(BlueprintBuff buff, bool toCaster)
        {
            var action = new ContextActionApplyBuff
            {
                Permanent = true,
                ToCaster = toCaster,
                IsFromSpell = false,
                DurationValue = new ContextDurationValue
                {
                    Rate = DurationRate.Rounds,
                    DiceType = DiceType.Zero,
                    DiceCountValue = 0,
                    BonusValue = 0
                }
            };
            Set(action, "m_Buff", Ref<BlueprintBuffReference>(buff));
            Set(action, "IsNotDispelable", true);
            return action;
        }

        private static ContextActionApplyBuff ApplyMinutes(BlueprintBuff buff, int minutes)
        {
            var action = new ContextActionApplyBuff
            {
                Permanent = false,
                ToCaster = false,
                IsFromSpell = false,
                DurationValue = new ContextDurationValue
                {
                    Rate = DurationRate.Minutes,
                    DiceType = DiceType.Zero,
                    DiceCountValue = 0,
                    BonusValue = minutes
                }
            };
            Set(action, "m_Buff", Ref<BlueprintBuffReference>(buff));
            Set(action, "IsNotDispelable", true);
            return action;
        }

        private static Kingmaker.Designers.Mechanics.Facts.SavingThrowBonusAgainstDescriptor SaveAgainstFear(
            int value, ModifierDescriptor descriptor)
            => new Kingmaker.Designers.Mechanics.Facts.SavingThrowBonusAgainstDescriptor
            {
                Value = value,
                Bonus = new ContextValue { ValueType = ContextValueType.Simple, Value = 0 },
                ModifierDescriptor = descriptor,
                SpellDescriptor = SpellDescriptor.Fear | SpellDescriptor.Shaken
            };

        // "Neutral outsider" is the outsider without a good or evil component,
        // so the lawful and chaotic neutral patrons count too.
        private static bool IsNeutralOutsider(AlignmentMaskType alignment)
            => (alignment & (AlignmentMaskType.Good | AlignmentMaskType.Evil)) == 0;

        private static ContextActionRemoveBuff RemoveBuff(BlueprintBuff buff)
        {
            var action = new ContextActionRemoveBuff { ToCaster = true };
            Set(action, "m_Buff", Ref<BlueprintBuffReference>(buff));
            return action;
        }

        private static ContextActionSkillCheck BarterCheck(BlueprintBuff buff)
        {
            var check = new ContextActionSkillCheck
            {
                Stat = StatType.CheckDiplomacy,
                UseCustomDC = true,
                CheckForCaster = true
            };
            Set(check, "CustomDC", new ContextValue { ValueType = ContextValueType.Rank, ValueRank = AbilityRankType.Default });
            Set(check, "Success", Actions(ApplyPermanent(buff, true)));
            Set(check, "Failure", Actions());
            return check;
        }

        private static Kingmaker.Designers.Mechanics.Facts.AddAbilityResources Grant(BlueprintAbilityResource resource)
            => ArchetypeBatch01445.Grant(resource);

        private static IncreaseResourceAmount Increase(BlueprintAbilityResource resource, int value)
        {
            var increase = new IncreaseResourceAmount { Value = value };
            Set(increase, "m_Resource", Ref<BlueprintAbilityResourceReference>(resource));
            return increase;
        }

        private static T Mce<T>(BlueprintsCache cache, string id) where T : SimpleBlueprint
            => cache.Load(BlueprintGuid.Parse(id)) as T;

        // The second pass adds seven blueprints to the verification; grouping
        // them keeps the argument list of Verify readable.
        private sealed class Auras
        {
            internal BlueprintFeature Ego;
            internal BlueprintFeature Ambition;
            internal BlueprintFeature Glory;
            internal BlueprintAbility GloryAbility;
            internal BlueprintFeature Belief;
            internal BlueprintFeature Indomitability;
            internal BlueprintFeature Champion;
            internal BlueprintBuff Cooldown;
            internal BlueprintBuff Smite;
            internal BlueprintBuff DoubledSmite;
        }

        private static void Verify(BlueprintCharacterClass cls, BlueprintArchetype archetype, UIGroup group,
            BlueprintFeature invocationFeature, BlueprintAbility invocationBase, BlueprintAbility smite,
            BlueprintAbility selfish, BlueprintAbility channelHeal, BlueprintAbility channelHarm,
            BlueprintFeatureSelection greeds, BlueprintFeatureSelection bonusFeat, BlueprintBuff[] buffs,
            BlueprintAbilityResource touchResource, BlueprintAbilityResource smiteResource,
            BlueprintFeatureSelection skillFocus, BlueprintsCache cache, BlueprintFeature ambitiousBond, Auras auras)
        {
            MagusBatch01454.VerifyArchetype(cls, archetype, group);
            MagusBatch01454.VerifyDeterminators(cls, invocationFeature);
            if (!archetype.RemoveSpellbook || !archetype.ChangeCasterType)
                throw new InvalidOperationException("Insinuator still counts as a divine spellcaster.");

            var gate = archetype.ComponentsArray.OfType<PrerequisiteAlignment>().FirstOrDefault();
            if (gate == null || !gate.ArchetypeAlignment || !gate.IngorePrerequisiteCheck ||
                gate.Alignment != ArchetypeAlignmentGate)
                throw new InvalidOperationException("Insinuator lost its archetype alignment gate.");

            foreach (var entry in archetype.RemoveFeatures)
                if (entry == null || entry.Features.Count() == 0)
                    throw new InvalidOperationException("Insinuator removal row is empty at level " + entry?.Level + ".");

            // Mirror ProgressionData.CanAddArchetype: a removed feature must be
            // granted by the class progression at that exact level. When it is
            // not, AddArchetype.Check fails and the level-up action silently
            // refuses to attach the archetype, so the archetype looks fine in the
            // class list but cannot be picked.
            var classEntries = cls.Progression.LevelEntries ?? Array.Empty<LevelEntry>();
            foreach (var entry in archetype.RemoveFeatures)
            {
                var row = classEntries.FirstOrDefault(e => e != null && e.Level == entry.Level);
                foreach (var feature in entry.Features)
                {
                    int mine = entry.Features.Count(f => f == feature);
                    int granted = row == null ? 0 : row.Features.Count(f => f == feature);
                    if (mine > granted)
                        throw new InvalidOperationException(
                            "Insinuator removes " + feature?.name + " at level " + entry.Level +
                            ", but the antipaladin progression does not grant it there.");
                }
            }

            var variantComponent = invocationBase.GetComponent<AbilityVariants>();
            var variants = variantComponent == null ? new List<BlueprintAbility>() : variantComponent.Variants.ToList();
            if (variants.Count != Invoked.Length)
                throw new InvalidOperationException("Insinuator offers " + variants.Count + " invocation variants.");
            foreach (var ability in variants)
            {
                if (ability == null) throw new InvalidOperationException("Insinuator invocation variant is missing.");
                if (ability.GetComponent<AbilityShowIfCasterAlignment0095>() == null)
                    throw new InvalidOperationException("Insinuator invocation variant lost its alignment filter: " + ability.name);
                if (ability.GetComponent<AbilityCasterAlignment>() == null)
                    throw new InvalidOperationException("Insinuator invocation variant lost its caster restriction: " + ability.name);
                var cooldown = ability.GetComponent<AbilityCasterCanInvoke0095>();
                if (cooldown == null || cooldown.Cooldown?.Get() != auras.Cooldown)
                    throw new InvalidOperationException("Insinuator invocation variant lost its daily cooldown: " + ability.name);
                if (ability.GetComponent<AbilityEffectRunAction>() == null)
                    throw new InvalidOperationException("Insinuator invocation variant lost its run action: " + ability.name);
            }
            if (buffs.Length != Invoked.Length || buffs.Any(b => b == null))
                throw new InvalidOperationException("Insinuator invocation buffs are incomplete.");

            if (smite.GetComponent<AbilityTargetNotInsinuatorAlignment0095>() == null)
                throw new InvalidOperationException("Smite impudence lost its same-alignment refusal.");
            // The smite only lands its marker when the target is not already
            // carrying it; a stale or inverted condition here silently turns the
            // whole smite into a no-op.
            var smiteRun = smite.GetComponent<AbilityEffectRunAction>();
            var smiteAction = smiteRun?.Actions?.Actions?.OfType<Conditional>().FirstOrDefault();
            if (smiteAction?.ConditionsChecker?.Conditions == null ||
                smiteAction.ConditionsChecker.Conditions.Length != 1 ||
                !(smiteAction.ConditionsChecker.Conditions[0] is ContextConditionHasBuffFromCaster carried) ||
                !carried.Not || carried.Buff == null)
                throw new InvalidOperationException("Smite impudence applies its buff unconditionally.");
            if (smiteAction.IfTrue?.Actions == null ||
                !smiteAction.IfTrue.Actions.OfType<ContextActionApplyBuff>()
                    .Any(a => !a.ToCaster && BuffOf(a) == auras.Smite))
                throw new InvalidOperationException("Smite impudence no longer marks its target.");
            foreach (var doubled in new[] { auras.Smite, auras.DoubledSmite })
            {
                var attack = doubled?.GetComponents<Kingmaker.Designers.Mechanics.Buffs.AttackBonusAgainstTarget>()
                    .FirstOrDefault();
                if (attack == null || attack.Descriptor != ModifierDescriptor.UntypedStackable)
                    throw new InvalidOperationException(
                        "Personal Champion's smite copy cannot stack its attack bonus: " + doubled?.name);
            }
            var gloryRun = auras.GloryAbility?.GetComponent<AbilityEffectRunAction>()?.Actions?.Actions?
                .OfType<Conditional>().ToArray();
            if (gloryRun == null || gloryRun.Length != 1 || gloryRun[0].ConditionsChecker?.Conditions == null ||
                gloryRun[0].ConditionsChecker.Conditions.Length != 1 ||
                !(gloryRun[0].ConditionsChecker.Conditions[0] is ContextConditionHasBuffFromCaster unmarked) ||
                !unmarked.Not || unmarked.Buff == null)
                throw new InvalidOperationException("Aura of Glory marks an already marked creature.");
            // The champion copy is nested, so the copies are collected through the
            // whole action tree instead of one level down.
            var sharedCopies = Applies(gloryRun[0].IfTrue)
                .Select(BuffOf).Where(b => b != null).Distinct().ToArray();
            if (sharedCopies.Length != 2)
                throw new InvalidOperationException("Aura of Glory does not share both smite copies.");
            foreach (var sharedCopy in sharedCopies)
            {
                var attack = sharedCopy.GetComponents<Kingmaker.Designers.Mechanics.Buffs.AttackBonusAgainstTarget>().FirstOrDefault();
                if (attack == null || !attack.CheckCaster || !attack.CheckCasterFriend)
                    throw new InvalidOperationException("Aura of Glory skips the insinuator himself: " + sharedCopy.name);
            }
            foreach (var casterGate in selfish.GetComponents<AbilityCasterAlignment>())
                if (casterGate.Alignment != AlignmentMaskType.Evil)
                    throw new InvalidOperationException("Selfish Healing still demands the paladin's alignment.");
            var fakeLevels = greeds.GetComponent<Kingmaker.Designers.Mechanics.Facts.ClassLevelsForPrerequisites>();
            var paladinGuid = BlueprintGuid.Parse(PaladinClassId);
            if (fakeLevels == null || fakeLevels.FakeClass == null || fakeLevels.FakeClass.AssetGuid != paladinGuid ||
                fakeLevels.ActualClass == null || fakeLevels.ActualClass.AssetGuid != cls.AssetGuid ||
                fakeLevels.Modifier != 1)
                throw new InvalidOperationException("Greeds no longer counts antipaladin levels as paladin levels.");
            if (greeds.AllFeatures.Length == 0 || greeds.AllFeatures.Any(f => f == null))
                throw new InvalidOperationException("Greeds lost the paladin mercy catalogue.");
            if (!HasResourceLogic(smite, smiteResource) || !HasResourceLogic(selfish, touchResource))
                throw new InvalidOperationException("Smite Impudence or Selfish Healing spends the wrong pool.");
            if (!PsychicAssets.GrantedFacts(invocationFeature).Contains(invocationBase.AssetGuid))
                throw new InvalidOperationException("Invocation does not grant its variant menu.");
            foreach (var channel in new[] { channelHeal, channelHarm })
            {
                var logic = channel.GetComponents<AbilityResourceLogic>().FirstOrDefault();
                if (logic == null || logic.Amount != 2 || ResourceOf(logic)?.Guid != touchResource.AssetGuid)
                    throw new InvalidOperationException("Channel Energy does not consume two selfish healing uses: " + channel.name);
                if (channel.GetComponent<ContextCalculateAbilityParamsBasedOnClass>() == null)
                    throw new InvalidOperationException("Channel Energy lost its Charisma-based DC: " + channel.name);
            }

            if (greeds.AllFeatures.Count() == 0)
                throw new InvalidOperationException("Greeds has no cruelty choices.");
            if (!bonusFeat.AllFeatures.Any(feat => feat != null && feat.AssetGuid == skillFocus.AssetGuid))
                throw new InvalidOperationException("Bonus Feat does not offer Skill Focus.");

            // The second pass fills the levels the first pass left empty; a
            // silently missing aura is exactly the failure this catches.
            var expected = new[]
            {
                new KeyValuePair<int, BlueprintFeature>(3, auras.Ego),
                new KeyValuePair<int, BlueprintFeature>(5, ambitiousBond),
                new KeyValuePair<int, BlueprintFeature>(8, auras.Ambition),
                new KeyValuePair<int, BlueprintFeature>(11, auras.Glory),
                new KeyValuePair<int, BlueprintFeature>(14, auras.Belief),
                new KeyValuePair<int, BlueprintFeature>(17, auras.Indomitability),
                new KeyValuePair<int, BlueprintFeature>(20, auras.Champion)
            };
            foreach (var expectedEntry in expected)
            {
                int level = expectedEntry.Key;
                var feature = expectedEntry.Value;
                if (feature == null) throw new InvalidOperationException("Insinuator lost a feature at level " + level + ".");
                var entry = archetype.AddFeatures.FirstOrDefault(e => e != null && e.Level == level);
                if (entry == null || !entry.Features.Any(f => f != null && f.AssetGuid == feature.AssetGuid))
                    throw new InvalidOperationException("Insinuator does not grant " + feature.name + " at level " + level + ".");
                if (group.Features.Count(f => f != null && f.AssetGuid == feature.AssetGuid) != 1)
                    throw new InvalidOperationException("Insinuator does not list " + feature.name + " in its UI group.");
            }

            // Every invocation buff must hang the four aura carriers (or six
            // when the patron lends an alignment) on its own activation.
            foreach (var buff in buffs)
            {
                var context = buff.GetComponent<AddFactContextActions>();
                var applied = context?.Activated?.Actions?.OfType<Conditional>().Count() ?? 0;
                if (applied < 4)
                    throw new InvalidOperationException("Invocation buff lost its auras: " + buff.name + " (" + applied + ").");
            }

            var gloryLogic = auras.GloryAbility.GetComponents<AbilityResourceLogic>().FirstOrDefault();
            if (gloryLogic == null || gloryLogic.Amount != 2 || ResourceOf(gloryLogic)?.Guid != smiteResource.AssetGuid)
                throw new InvalidOperationException("Aura of Glory does not consume two smite impudence uses.");
            if (auras.GloryAbility.GetComponent<AbilityCasterHasFacts>() == null)
                throw new InvalidOperationException("Aura of Glory works without an invocation.");
            if (auras.DoubledSmite == null || auras.DoubledSmite.AssetGuid == auras.Smite.AssetGuid)
                throw new InvalidOperationException("Personal Champion has no second smite buff to stack.");
            AmbitiousBond0095.Verify(cache, ambitiousBond, invocationFeature);
        }

        private static BlueprintAbilityResourceReference ResourceOf(AbilityResourceLogic logic)
            => Get(logic, "m_RequiredResource") as BlueprintAbilityResourceReference;

        private static BlueprintBuff BuffOf(ContextActionApplyBuff action)
            => Get(action, "m_Buff") is BlueprintBuffReference reference ? reference.Get() : null;

        // Every buff application in an action list, following nested conditionals
        // so a guarded copy is still found where it actually lives.
        private static IEnumerable<ContextActionApplyBuff> Applies(ActionList list)
        {
            foreach (var action in list?.Actions ?? Array.Empty<GameAction>())
            {
                if (action is ContextActionApplyBuff apply) yield return apply;
                else if (action is Conditional conditional)
                {
                    foreach (var nested in Applies(conditional.IfTrue)) yield return nested;
                    foreach (var nested in Applies(conditional.IfFalse)) yield return nested;
                }
            }
        }

        private static bool HasResourceLogic(BlueprintAbility ability, BlueprintAbilityResource resource)
            => ability.GetComponents<AbilityResourceLogic>().Any(l => ResourceOf(l)?.Guid == resource.AssetGuid);
    }
}

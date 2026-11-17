using System;
using System.Collections.Generic;
using System.Linq;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.Designers.EventConditionActionSystem.Actions;
using Kingmaker.Designers.Mechanics.Facts;
using Kingmaker.ElementsSystem;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Enums;
using Kingmaker.Localization;
using Kingmaker.RuleSystem;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components;
using Kingmaker.UnitLogic.Abilities.Components.TargetCheckers;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Commands.Base;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Mechanics.Actions;
using Kingmaker.UnitLogic.Mechanics.Components;
using Kingmaker.UnitLogic.Mechanics.Conditions;
using Kingmaker.Utility;
using static CallOfTheWild.Wrath.Prototype.Occultist0099Abjuration;
using static CallOfTheWild.Wrath.Prototype.PsychicAssets;

namespace CallOfTheWild.Wrath.Prototype
{
    internal static class Occultist0099Conjuration
    {
        internal const string School = "Conjuration";
        private const string LayOnHands = "caae1dc6fcf7b37408686971ee27db13";
        private const string NeutralizePoison = "e7240516af4241b42b2cd819929ea9da";
        private const string RemoveDisease = "4093d5a0eb5cae94e909eb1e0e1a6b36";
        private const string DimensionDoor = "a9b8be9b87865744382f7c64e599aeb2";
        private const string AugmentSummoning = "38155ca9e4055bb48a89240a2055dcc3";
        private const string Undead = "734a29b693e9ec346ba2951b27987e33";
        private const string Construct = "fd389783027d63343b4a5634bd81645f";
        private const string Elemental = "198fd8924dabcb5478d0f78bd453c586";
        private const string ObscuringMist = "91651f6f9af8b82e5aff6e2cb7aedfa3";
        private const string SolidFog = "ea8d3fc6fe47a0ce20df9b472eebf1a3";

        // Summon Monster I-VII single-creature roots, in tier order. The A map
        // freezes the tiers at 1/4/7/10/13/16/19 and the maximum at VII.
        private static readonly string[] Summons =
        {
            "8fd74eddd9b6c224693d9ab241f25e84", "7ab27a0d547742741beb5d089f1c3852", "15b5efe371d47c944b58444e7b734ffb",
            "efa433a38e9c7c14bb4e780f8a3fe559", "0964bf88b582bed41b74e79596c4f6d9", "02de4dd8add69aa42a3d1330b573e2ab",
            "2920d48574933c24391fbb9e18f87bf5"
        };
        private static readonly int[] SummonLevels = { 1, 4, 7, 10, 13, 16, 19 };
        private static readonly string[] RomanTiers = { "I", "II", "III", "IV", "V", "VI", "VII" };

        internal static readonly string[] Donors = Summons.Concat(new[] { LayOnHands, NeutralizePoison, RemoveDisease,
            DimensionDoor, AugmentSummoning, Undead, Construct, Elemental, ObscuringMist, SolidFog }).ToArray();
        internal static readonly string[] Tokens = { "ConjurationEffectiveLevel", "ConjurationDCLevel", "CastingFocusValue",
            "CastingFocusBuff", "ServitorAbility", "FleshMendAbility", "FleshMendPower", "PsychicFogAbility", "PsychicFogMistAbility",
            "PsychicFogSolidAbility", "PsychicFogPower", "PurgeCorruptionAbility", "PurgeCorruptionPoisonAbility",
            "PurgeCorruptionDiseaseAbility", "PurgeCorruptionPower", "SideStepAbility", "SideStepPower",
            "ConjurationInvestFocusAbility", "ConjurationReplenishFocusAbility", "ConjurationImplementFocus" };

        internal static readonly BlueprintGuid SideStepId = BlueprintGuid.Parse(Occultist0099Ids.Id("SideStepAbility"));

        internal static BlueprintScriptableObject[] Build(BlueprintsCache cache, Func<string, string, string, LocalizedString> text)
        {
            var layOnHands = Occultist0099Illusion.Load<BlueprintAbility>(cache, LayOnHands);
            var poison = Occultist0099Illusion.Load<BlueprintAbility>(cache, NeutralizePoison);
            var disease = Occultist0099Illusion.Load<BlueprintAbility>(cache, RemoveDisease);
            var door = Occultist0099Illusion.Load<BlueprintAbility>(cache, DimensionDoor);
            var augment = Occultist0099Illusion.Load<BlueprintFeature>(cache, AugmentSummoning);
            var undead = Occultist0099Illusion.Load<BlueprintFeature>(cache, Undead);
            var construct = Occultist0099Illusion.Load<BlueprintFeature>(cache, Construct);
            var elemental = Occultist0099Illusion.Load<BlueprintFeature>(cache, Elemental);
            var mist = Occultist0099Illusion.Load<BlueprintAbility>(cache, ObscuringMist);
            var solid = Occultist0099Illusion.Load<BlueprintAbility>(cache, SolidFog);
            var summons = Summons.Select(id => Occultist0099Illusion.Load<BlueprintAbility>(cache, id)).ToArray();
            var spawns = summons.Select(ability => ability?.GetComponent<AbilityEffectRunAction>()).ToArray();
            if (layOnHands?.name != "LayOnHandsOthers" || poison?.name != "NeutralizePoison" || disease?.name != "RemoveDisease" ||
                door?.name != "DimensionDoorCasterOnly" || augment == null || undead == null || construct == null || elemental == null ||
                mist?.name != "CotWWrathPsychicObscuringMist" || solid?.name != "CotWWrathPsychicSolidFog" ||
                summons.Any(ability => ability == null || ability.Type != AbilityType.Spell ||
                    ability.School != SpellSchool.Conjuration || !ability.CanTargetPoint) ||
                spawns.Any(action => action == null || !SpellBatch01447.Elements(new[] { action }, new HashSet<object>(IdentityComparer))
                    .OfType<ContextActionSpawnMonster>().Any()) ||
                poison.GetComponent<AbilityEffectRunAction>() == null || disease.GetComponent<AbilityEffectRunAction>() == null ||
                door.GetComponent<AbilityCustomDimensionDoor>() == null) return null;

            var level = Occultist0099SchoolValue.Create("ConjurationEffectiveLevel", School, Occultist0099SchoolValueKind.EffectiveLevel);
            var dc = Occultist0099SchoolValue.Create("ConjurationDCLevel", School, Occultist0099SchoolValueKind.DCLevel);

            // Upstream extends buff durations and summon durations only. Area-effect
            // lifetimes go through ContextActionSpawnAreaEffect, which neither the
            // original nor this port touches, so the text does not promise them.
            const string castingText = "Your Conjuration spells that last rounds per level keep their buffs and summoned creatures for 1 additional round for every 2 points of mental focus locked in Conjuration, up to your occultist level. This does not extend the lifetime of an area effect such as a created pit.";
            var castingValue = Occultist0099SchoolValue.Create("CastingFocusValue", School, Occultist0099SchoolValueKind.Resonance);
            var casting = Identify(new BlueprintBuff { IsClassFeature = true, Stacking = StackingType.Replace,
                ComponentsArray = new BlueprintComponent[]
                {
                    new Occultist0099CastingFocus { name = "OccultistCastingFocus" },
                    new ContextRankConfig { name = "CastingFocusRounds" }
                } }, "CastingFocusBuff", "Casting Focus", castingText, poison, text);
            var castingRank = casting.GetComponent<ContextRankConfig>();
            SetEnum(castingRank, "m_BaseValueType", "CustomProperty");
            Set(castingRank, "m_CustomProperty", Ref<BlueprintUnitPropertyReference>(castingValue));
            SetEnum(castingRank, "m_Progression", "AsIs");
            SetEnum(casting, "m_Flags", "StayOnDeath");

            const string servitorText = "Spend 1 point of Conjuration focus to summon a single creature as summon monster, lasting 1 minute. The spell level is I at 1st level and rises by one every 3 levels, to VII at 19th level.";
            var tier = new ContextRankConfig { name = "ServitorTier" };
            SetEnum(tier, "m_BaseValueType", "CasterLevel");
            Set(tier, "m_Type", AbilityRankType.StatBonus);
            SetEnum(tier, "m_Progression", "StartPlusDivStep");
            Set(tier, "m_StartLevel", 1);
            Set(tier, "m_StepLevel", 3);
            Set(tier, "m_UseMax", true);
            Set(tier, "m_Max", 7);
            // The native summon duration reads the Default rank; pinning it to ten
            // rounds is what turns summon monster's round-per-level into 1 minute.
            var minute = new ContextRankConfig { name = "ServitorMinute" };
            SetEnum(minute, "m_BaseValueType", "CasterLevel");
            SetEnum(minute, "m_Progression", "AsIs");
            Set(minute, "m_UseMin", true);
            Set(minute, "m_Min", 10);
            Set(minute, "m_UseMax", true);
            Set(minute, "m_Max", 10);
            var branches = new List<GameAction>();
            for (int i = 0; i < summons.Length; i++)
            {
                var actions = CopyData(spawns[i]);
                branches.Add(new Conditional { ConditionsChecker = new ConditionsChecker
                    { Conditions = new Condition[] { Exactly(AbilityRankType.StatBonus, i + 1) } },
                    IfTrue = actions.Actions, IfFalse = Actions() });
            }
            var servitor = Power("ServitorAbility", "Servitor", servitorText, AbilityType.SpellLike, summons[0].Range, summons[0], 1,
                Run(branches.ToArray()), tier, minute);
            servitor.CanTargetPoint = true;
            servitor.CanTargetSelf = false;
            servitor.CustomRange = summons[0].CustomRange;
            servitor.SetIsFullRoundAction(false);

            const string mendText = "Spend 1 point of Conjuration focus to heal a living creature by touch for 1d8 plus your occultist level. Gain another d8 every 4 levels beyond 3rd, up to 5d8. Undead, constructs and elementals are unaffected.";
            var mendDice = new ContextRankConfig { name = "FleshMendDice" };
            SetEnum(mendDice, "m_BaseValueType", "CasterLevel");
            Set(mendDice, "m_Type", AbilityRankType.DamageDice);
            SetEnum(mendDice, "m_Progression", "StartPlusDivStep");
            Set(mendDice, "m_StartLevel", 3);
            Set(mendDice, "m_StepLevel", 4);
            Set(mendDice, "m_UseMax", true);
            Set(mendDice, "m_Max", 5);
            var mend = Power("FleshMendAbility", "Flesh Mend", mendText, AbilityType.SpellLike, AbilityRange.Touch, layOnHands, 1,
                Run(new ContextActionHealTarget { Value = new ContextDiceValue { DiceType = DiceType.D8,
                    DiceCountValue = Rank(AbilityRankType.DamageDice), BonusValue = PropertyValue(level) } }),
                mendDice, Descriptor(SpellDescriptor.Cure | SpellDescriptor.RestoreHP),
                Exclude(undead, construct, elemental), new AbilityUseOnRest { name = "FleshMendRest", Type = AbilityUseOnRestType.HealDamage });
            mend.CanTargetSelf = true;
            mend.CanTargetFriends = true;
            mend.CanTargetEnemies = false;
            mend.EffectOnAlly = AbilityEffectOnUnit.Helpful;
            mend.EffectOnEnemy = AbilityEffectOnUnit.None;
            mend.NotOffensive = true;

            const string fogText = "Spend 1 point of Conjuration focus to create a 20-foot fog cloud for 1 minute per occultist level. At 7th level you may spend 2 points instead to make it solid fog.";
            var fogMist = FogVariant("PsychicFogMistAbility", "Psychic Fog (Obscuring Mist)", fogText, mist, 1, 0);
            var fogSolid = FogVariant("PsychicFogSolidAbility", "Psychic Fog (Solid Fog)", fogText, solid, 2, 7);
            var fog = Occultist0099Illusion.Variants("PsychicFogAbility", "Psychic Fog", fogText, mist, text,
                SpellSchool.Conjuration, fogMist, fogSolid);
            fog.CanTargetPoint = true;

            const string purgeText = "Spend 1 point of Conjuration focus to draw out corruption as either neutralize poison or remove disease, using your occultist level as the caster level. Each use cures one poison or one disease.";
            var purgePoison = PurgeVariant("PurgeCorruptionPoisonAbility", "Purge Corruption: Neutralize Poison", purgeText, poison);
            var purgeDisease = PurgeVariant("PurgeCorruptionDiseaseAbility", "Purge Corruption: Remove Disease", purgeText, disease);
            var purge = Occultist0099Illusion.Variants("PurgeCorruptionAbility", "Purge Corruption", purgeText, poison, text,
                SpellSchool.Conjuration, purgePoison, purgeDisease);

            const string stepText = "Spend 1 point of Conjuration focus as a move action to teleport to a square you can see within 10 feet per occultist level.";
            // The native teleport logic is reused as-is; unlike Dimension Door it
            // keeps line of sight, because Side Step requires a square you can see.
            var step = Power("SideStepAbility", "Side Step", stepText, AbilityType.Supernatural, AbilityRange.Custom, door, 1,
                CopyData(door.GetComponent<AbilityCustomDimensionDoor>()));
            step.ActionType = UnitCommand.CommandType.Move;
            step.CanTargetPoint = true;
            step.CanTargetSelf = false;
            step.CanTargetEnemies = false;
            step.CanTargetFriends = false;
            step.NotOffensive = true;
            step.EffectOnAlly = AbilityEffectOnUnit.Helpful;
            step.CustomRange = new Feet(10);

            var invest = Operation("ConjurationInvestFocusAbility", "Invest Mental Focus: Conjuration",
                "Invest 1 point of unallocated mental focus in Conjuration before locking your focus.",
                Occultist0099FocusAction.Invest, UnitCommand.CommandType.Free, poison, text, School);
            var replenish = Operation("ConjurationReplenishFocusAbility", "Replenish Mental Focus: Conjuration",
                "Spend 1 unallocated point of mental focus to replenish a spent Conjuration point.",
                Occultist0099FocusAction.Replenish, UnitCommand.CommandType.Free, poison, text, School);
            var implement = Identify(new BlueprintFeature { Ranks = 1, IsClassFeature = true, ComponentsArray = new BlueprintComponent[]
                { new Occultist0099ImplementSchool { name = "OccultistConjurationSchool", School = School }, Facts(servitor, invest, replenish) } },
                "ConjurationImplementFocus", "Conjuration Implement", "Gain Servitor and Casting Focus.", poison, text);
            return new BlueprintScriptableObject[] { level, dc, castingValue, casting,
                servitor, mend, fog, fogMist, fogSolid, purge, purgePoison, purgeDisease, step, invest, replenish, implement };

            BlueprintAbility Power(string token, string label, string description, AbilityType type, AbilityRange range,
                BlueprintAbility icon, int cost, params BlueprintComponent[] components)
                => Identify(Occultist0099Illusion.NewSchoolPower(SpellSchool.Conjuration, type, range, cost, level, dc, components),
                    token, label, description, icon, text);

            BlueprintAbility FogVariant(string token, string label, string description, BlueprintAbility source, int cost, int minimum)
            {
                var spawn = CopyData(source.GetComponent<AbilityEffectRunAction>());
                var ability = Power(token, label, description, AbilityType.SpellLike, source.Range, source, cost, spawn);
                // The level gate must be re-checked at payment, not only in the menu.
                ability.GetComponent<Occultist0099FocusResourceLogic>().CheckCasterRestrictions = true;
                ability.CanTargetPoint = true;
                ability.CanTargetSelf = true;
                ability.CanTargetFriends = true;
                ability.CanTargetEnemies = true;
                ability.CustomRange = source.CustomRange;
                ability.EffectOnAlly = ability.EffectOnEnemy = AbilityEffectOnUnit.Harmful;
                if (minimum > 0)
                    ability.ComponentsArray = ability.ComponentsArray.Concat(new BlueprintComponent[]
                        { new Occultist0099PowerLevel { name = "PsychicFogSolidGate", Minimum = minimum } }).ToArray();
                return ability;
            }

            BlueprintAbility PurgeVariant(string token, string label, string description, BlueprintAbility source)
            {
                var actions = CopyData(source.GetComponent<AbilityEffectRunAction>());
                // Both native donors leave m_StopAfterCountRemoved false, which makes the
                // native action use int.MaxValue and ignore its count field. This power
                // cures only one poison or one disease per use.
                foreach (var dispel in SpellBatch01447.Elements(new[] { actions }, new HashSet<object>(IdentityComparer))
                    .OfType<ContextActionDispelMagic>())
                    Set(dispel, "m_StopAfterCountRemoved", true);
                var ability = Power(token, label, description, AbilityType.SpellLike, source.Range, source, 1, actions);
                ability.CanTargetSelf = true;
                ability.CanTargetFriends = true;
                ability.CanTargetEnemies = false;
                ability.NotOffensive = true;
                ability.EffectOnAlly = AbilityEffectOnUnit.Helpful;
                ability.CustomRange = source.CustomRange;
                return ability;
            }
        }

        private static ContextValue Rank(AbilityRankType type) => new ContextValue { ValueType = ContextValueType.Rank, ValueRank = type };

        private static ContextConditionCompare Exactly(AbilityRankType rank, int value)
        {
            var compare = new ContextConditionCompare { CheckValue = Rank(rank), TargetValue = value };
            SetEnum(compare, "m_Type", "Equal");
            return compare;
        }

        private static SpellDescriptorComponent Descriptor(SpellDescriptor descriptor)
            => new SpellDescriptorComponent { name = "OccultistConjurationDescriptors", Descriptor = descriptor };

        private static AbilityTargetHasFact Exclude(params BlueprintFeature[] features)
        {
            var rule = new AbilityTargetHasFact { name = "OccultistConjurationLivingTarget", Inverted = true };
            Set(rule, "m_CheckedFacts", features.Select(Ref<BlueprintUnitFactReference>).ToArray());
            return rule;
        }
    }
}
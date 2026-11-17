using System;
using System.Collections.Generic;
using System.Linq;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.Blueprints.Facts;
using Kingmaker.Designers.EventConditionActionSystem.Actions;
using Kingmaker.Designers.Mechanics.Buffs;
using Kingmaker.ElementsSystem;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Enums;
using Kingmaker.Localization;
using Kingmaker.RuleSystem;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components;
using Kingmaker.UnitLogic.Abilities.Components.Base;
using Kingmaker.UnitLogic.Abilities.Components.TargetCheckers;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Commands.Base;
using Kingmaker.UnitLogic.FactLogic;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Mechanics.Actions;
using Kingmaker.UnitLogic.Mechanics.Components;
using Kingmaker.UnitLogic.Mechanics.Conditions;
using Kingmaker.UnitLogic.Mechanics.Properties;
using Kingmaker.Utility;
using static CallOfTheWild.Wrath.Prototype.Occultist0099Abjuration;
using static CallOfTheWild.Wrath.Prototype.PsychicAssets;

namespace CallOfTheWild.Wrath.Prototype
{
    internal static class Occultist0099Enchantment
    {
        internal const string School = "Enchantment";
        private const string Command = "feb70aab86cc17f4bb64432c83737ac2";
        private const string Daze = "9934fedff1b14994ea90205d189c8759";
        private const string Stagger = "df3950af5a783bd4d91ab73eb8fa0fd3";
        private const string HoldMonster = "2cfcce5b62d3e6d4082ec31b58468cc8";
        private const string Heroism = "5ab0d42fb68c9e34abae4921822b9d63";
        private const string Construct = "fd389783027d63343b4a5634bd81645f";
        private const string Undead = "734a29b693e9ec346ba2951b27987e33";
        private static readonly (string Native, string Label, string Ability, string Buff)[] Commands =
        {
            ("Halt", "Halt", "a43abe1819699894c94a7cec3ccd3765", "1baa8a3dffc1f104f9942abe852f631a"),
            ("Fall", "Fall", "9e87cb2778afdc24e9ceb523aca512a8", "1e9d53aaa310894499ba610f0a2306a3"),
            ("Flee", "Run", "7c1d48449ecf4374497e7009c49f6376", "9c8b488120d2b4d4f8243720d754c88e")
        };
        internal static readonly string[] DonorIds = new[] { Command, Daze, Stagger, HoldMonster, Heroism, Construct, Undead }
            .Concat(Commands.SelectMany(command => new[] { command.Ability, command.Buff })).ToArray();

        internal static BlueprintScriptableObject[] Build(BlueprintsCache cache, Func<string, string, string, LocalizedString> text)
        {
            T Load<T>(string id, string name) where T : BlueprintScriptableObject
            {
                var guid = BlueprintGuid.Parse(id);
                var donor = cache.Load(guid) as T;
                return donor?.name == name && donor.AssetGuid == guid ? donor : null;
            }

            var command = Load<BlueprintAbility>(Command, "Command");
            var daze = Load<BlueprintBuff>(Daze, "DazeBuff");
            var staggered = Load<BlueprintBuff>(Stagger, "Staggered");
            var paralysis = Load<BlueprintBuff>(HoldMonster, "HoldMonsterBuff");
            var heroism = Load<BlueprintAbility>(Heroism, "Heroism");
            var construct = Load<BlueprintFeature>(Construct, "ConstructType");
            var undead = Load<BlueprintFeature>(Undead, "UndeadType");
            if (command == null || heroism == null || construct == null || undead == null ||
                daze?.GetComponent<BuffStatusCondition>()?.Condition != UnitCondition.Dazed ||
                staggered?.GetComponent<BuffStatusCondition>()?.Condition != UnitCondition.Staggered ||
                !(paralysis?.GetComponent<BuffStatusCondition>() is BuffStatusCondition hold) ||
                hold.Condition != UnitCondition.Paralyzed || !hold.SaveEachRound || hold.SaveType != SavingThrowType.Will) return null;

            var variantsComponent = command.GetComponent<AbilityVariants>();
            var commandVariants = variantsComponent == null ? null : Get(variantsComponent, "m_Variants") as BlueprintAbilityReference[];
            if (commandVariants == null) return null;
            var copiedCommands = new List<AbilityEffectRunAction>();
            foreach (var branchDonor in Commands)
            {
                var donor = Load<BlueprintAbility>(branchDonor.Ability, "Command" + branchDonor.Native);
                var run = donor?.GetComponent<AbilityEffectRunAction>();
                var saved = run?.Actions?.Actions is GameAction[] actions && actions.Length == 1 ? actions[0] as ContextActionConditionalSaved : null;
                var apply = saved?.Failed?.Actions is GameAction[] failed && failed.Length == 1 ? failed[0] as ContextActionApplyBuff : null;
                var buff = Load<BlueprintBuff>(branchDonor.Buff, "Command" + branchDonor.Native + "Buff");
                if (donor == null || donor.Type != AbilityType.Spell || PsychicSpellIntegration.ParentGuid(donor) != command.AssetGuid ||
                    !commandVariants.Any(reference => reference?.Guid == donor.AssetGuid) || run?.SavingThrowType != SavingThrowType.Will ||
                    buff == null || apply == null || (Get(apply, "m_Buff") as BlueprintBuffReference)?.Guid != buff.AssetGuid) return null;
                var save = new ContextActionSavingThrow { name = "OccultistObeyWill", Type = SavingThrowType.Will, Actions = CopyData(run.Actions) };
                Set(save, "m_ConditionalDCIncrease", new[]
                {
                    new ContextActionSavingThrow.ConditionalDCIncrease
                    {
                        Condition = new ConditionsChecker { Conditions = new Condition[] { new Occultist0099SameRace() } }, Value = 2
                    }
                });
                // Retain the native Command buff reference in this cache.
                var branch = (ContextActionConditionalSaved)save.Actions.Actions[0];
                Set(branch.Failed.Actions[0], "m_Buff", Ref<BlueprintBuffReference>(buff));
                copiedCommands.Add(Run(save));
            }

            var level = Occultist0099SchoolValue.Create("EnchantmentEffectiveLevel", School, Occultist0099SchoolValueKind.EffectiveLevel);
            var spellLevel = Occultist0099SchoolValue.Create("EnchantmentDCLevel", School, Occultist0099SchoolValueKind.DCLevel);
            var resonance = Occultist0099SchoolValue.Create("GloriousPresenceValue", School, Occultist0099SchoolValueKind.Resonance);
            var rank = new ContextRankConfig { name = "GloriousPresenceRank" };
            SetEnum(rank, "m_BaseValueType", "CustomProperty");
            Set(rank, "m_CustomProperty", Ref<BlueprintUnitPropertyReference>(resonance));
            var presence = Local(new BlueprintBuff
            {
                Stacking = StackingType.Replace, IsClassFeature = true,
                ComponentsArray = new BlueprintComponent[]
                {
                    rank, StatBonus(StatType.SkillPersuasion, ModifierDescriptor.Competence),
                    StatBonus(StatType.SkillUseMagicDevice, ModifierDescriptor.Competence),
                    new CourtBardCharismaCheck01445 { name = "GloriousPresenceCharisma", Descriptor = ModifierDescriptor.Competence }
                }
            }, "GloriousPresenceBuff", "Glorious Presence",
                "Gain a +1 competence bonus on Persuasion, Use Magic Device and Charisma checks per 2 locked focus points, up to 1 + 1 for every 4 occultist levels.", command);

            var hd = new ContextCalculateSharedValue { name = "CloudMindHitDice", ValueType = AbilitySharedValue.StatBonus,
                Value = new ContextDiceValue { DiceType = DiceType.Zero, DiceCountValue = 0, BonusValue = PropertyValue(level) } };
            var cloudEffect = new Conditional
            {
                ConditionsChecker = new ConditionsChecker { Conditions = new Condition[]
                    { new ContextConditionSharedValueHitDice { SharedValue = AbilitySharedValue.StatBonus } } },
                IfTrue = Actions(Apply(daze, 1, fromSpell: false)), IfFalse = Actions(Apply(staggered, 1, fromSpell: false))
            };
            var cloud = Power("CloudMindAbility", "Cloud Mind",
                "Spend 1 point of Enchantment focus. A foe that fails a Will save is dazed for 1 round if its Hit Dice do not exceed your occultist level, or staggered for 1 round otherwise.",
                AbilityType.Supernatural, AbilityRange.Medium, false, false, SpellDescriptor.MindAffecting, command,
                hd, Save(SavingThrowType.Will, Saved(Array.Empty<GameAction>(), new GameAction[] { cloudEffect })));

            var bindDuration = CasterRank("BindingPatternDuration", "Div2");
            var bind = Power("BindingPatternAbility", "Binding Pattern",
                "Spend 1 point of Enchantment focus. A living foe that fails a Will save is paralyzed and staggered for 1 round per 2 occultist levels. It can end the paralysis early with another Will save each round.",
                AbilityType.SpellLike, AbilityRange.Medium, false, true,
                SpellDescriptor.MindAffecting | SpellDescriptor.Compulsion | SpellDescriptor.Paralysis, command,
                LivingTarget(), bindDuration, Save(SavingThrowType.Will, Saved(Array.Empty<GameAction>(), new GameAction[]
                    { Timed(paralysis, Rank(), DurationRate.Rounds), Timed(staggered, Rank(), DurationRate.Rounds) })));

            var assaultRank = CasterRank("InspiredAssaultRank", "OnePlusDivStep");
            Set(assaultRank, "m_StepLevel", 6);
            Set(assaultRank, "m_UseMax", true);
            Set(assaultRank, "m_Max", 4);
            var assaultBuff = Local(new BlueprintBuff
            {
                Stacking = StackingType.Replace, IsClassFeature = true,
                ComponentsArray = new[] { StatType.AdditionalAttackBonus, StatType.AdditionalDamage, StatType.SaveFortitude,
                    StatType.SaveReflex, StatType.SaveWill }.Select(stat => (BlueprintComponent)StatBonus(stat, ModifierDescriptor.Morale))
                    .Concat(new[] { assaultRank }).ToArray()
            }, "InspiredAssaultBuff", "Inspired Assault", "", heroism);
            var assault = Power("InspiredAssaultAbility", "Inspired Assault",
                "Spend 1 point of Enchantment focus to give a living ally a morale bonus to attack, damage and saves for 1 minute per occultist level. The bonus is +1, plus 1 per 6 occultist levels.",
                AbilityType.SpellLike, AbilityRange.Touch, true, true, SpellDescriptor.MindAffecting | SpellDescriptor.Compulsion,
                heroism, LivingTarget(), Run(Timed(assaultBuff, PropertyValue(level), DurationRate.Minutes)));

            const string obeyText = "Spend 1 point of Enchantment focus to command a living foe within 30 feet to halt, fall or run. Will negates. A creature of your race saves against a DC 2 higher.";
            var obey = Local(new BlueprintAbility { Type = AbilityType.SpellLike, Range = AbilityRange.Custom, CustomRange = new Feet(30),
                ActionType = UnitCommand.CommandType.Standard, CanTargetEnemies = true, EffectOnEnemy = AbilityEffectOnUnit.Harmful },
                "ObeyAbility", "Obey", obeyText, command);
            var variants = Commands.Select((branch, i) =>
            {
                var child = Power("Obey" + branch.Label + "Ability", "Obey (" + branch.Label + ")", obeyText,
                    AbilityType.SpellLike, AbilityRange.Custom, false, true, SpellDescriptor.MindAffecting | SpellDescriptor.Compulsion,
                    command, LivingTarget(), copiedCommands[i]);
                child.CustomRange = new Feet(30);
                Set(child, "m_Parent", Ref<BlueprintAbilityReference>(obey));
                return child;
            }).ToArray();
            var choices = new AbilityVariants { name = "OccultistObeyVariants" };
            Set(choices, "m_Variants", variants.Select(Ref<BlueprintAbilityReference>).ToArray());
            obey.ComponentsArray = new BlueprintComponent[] { choices };

            var invest = Operation("EnchantmentInvestFocusAbility", "Invest Mental Focus: Enchantment",
                "Invest 1 point of unallocated mental focus in Enchantment before locking your focus.",
                Occultist0099FocusAction.Invest, UnitCommand.CommandType.Free, command, text, School);
            var replenish = Operation("EnchantmentReplenishFocusAbility", "Replenish Mental Focus: Enchantment",
                "Spend 1 unallocated point of mental focus to replenish a spent Enchantment point.",
                Occultist0099FocusAction.Replenish, UnitCommand.CommandType.Free, command, text, School);
            var implement = Local(new BlueprintFeature { Ranks = 1, IsClassFeature = true, ComponentsArray = new BlueprintComponent[]
            {
                new Occultist0099ImplementSchool { name = "OccultistEnchantmentSchool", School = School }, Facts(cloud, invest, replenish)
            } }, "EnchantmentImplementFocus", "Enchantment Implement", "Gain Cloud Mind and Glorious Presence.", command);
            return new BlueprintScriptableObject[] { level, spellLevel, resonance, presence, cloud, bind, assaultBuff, assault, obey,
                invest, replenish, implement }.Concat(variants).ToArray();

            T Local<T>(T blueprint, string token, string label, string description, BlueprintAbility icon) where T : BlueprintScriptableObject
                => Identify(blueprint, token, label, description, icon, text);

            BlueprintAbility Power(string token, string title, string description, AbilityType type, AbilityRange range, bool friendly,
                bool resistance, SpellDescriptor descriptor, BlueprintAbility icon, params BlueprintComponent[] components)
                => Local(new BlueprintAbility
                {
                    Type = type, ActionType = UnitCommand.CommandType.Standard, Range = range,
                    CanTargetSelf = friendly, CanTargetFriends = friendly, CanTargetEnemies = !friendly,
                    EffectOnEnemy = AbilityEffectOnUnit.Harmful, EffectOnAlly = AbilityEffectOnUnit.Helpful, SpellResistance = resistance,
                    ComponentsArray = components.Concat(new BlueprintComponent[]
                    {
                        new SpellComponent { name = "OccultistEnchantment", School = SpellSchool.Enchantment },
                        new SpellDescriptorComponent { name = "OccultistEnchantmentDescriptors", Descriptor = descriptor },
                        new Occultist0099FocusResourceLogic { name = "OccultistEnchantmentCost", School = School, Amount = 1 },
                        new ContextCalculateAbilityParams { name = "OccultistEnchantmentParams", StatType = StatType.Intelligence,
                            ReplaceCasterLevel = true, CasterLevel = PropertyValue(level),
                            ReplaceSpellLevel = true, SpellLevel = PropertyValue(spellLevel) }
                    }).ToArray()
                }, token, title, description, icon);

            AbilityTargetHasFact LivingTarget()
            {
                var target = new AbilityTargetHasFact { name = "OccultistLivingTarget", Inverted = true };
                Set(target, "m_CheckedFacts", new[] { Ref<BlueprintUnitFactReference>(construct), Ref<BlueprintUnitFactReference>(undead) });
                return target;
            }
        }

        private static AddContextStatBonus StatBonus(StatType stat, ModifierDescriptor descriptor)
            => new AddContextStatBonus { name = "Occultist" + stat, Stat = stat, Descriptor = descriptor, Value = Rank() };

        private static ContextRankConfig CasterRank(string name, string progression)
        {
            var rank = new ContextRankConfig { name = name };
            SetEnum(rank, "m_BaseValueType", "CasterLevel");
            SetEnum(rank, "m_Progression", progression);
            Set(rank, "m_UseMin", true);
            Set(rank, "m_Min", 1);
            return rank;
        }

        private static ContextValue Rank() => new ContextValue { ValueType = ContextValueType.Rank };
        private static ContextActionApplyBuff Timed(BlueprintBuff buff, ContextValue duration, DurationRate rate)
        {
            var apply = Apply(buff, 0, rate);
            apply.DurationValue.BonusValue = duration;
            return apply;
        }
    }

    public enum Occultist0099SchoolValueKind { EffectiveLevel, DCLevel, Resonance, ActualLevel, ActualDCLevel }

    [TypeId(Occultist0099Ids.SchoolValueTypeId)]
    public sealed class Occultist0099SchoolValue : PropertyValueGetter
    {
        public string School;
        public Occultist0099SchoolValueKind Value;

        internal static BlueprintUnitProperty Create(string token, string school, Occultist0099SchoolValueKind value)
            => Identify(new BlueprintUnitProperty { ComponentsArray = new BlueprintComponent[]
            {
                new Occultist0099SchoolValue { name = token, School = school, Value = value, Settings = new PropertySettings() }
            } }, token, "", "", null);

        protected override int GetBaseValue(UnitEntityData unit)
            => Occultist0099NativeScope.InRuntimeContext(
                Occultist0099NativeScope.RuntimeOf(unit, Occultist0099Ids.FocusFactGuid, typeof(Occultist0099FocusFact0099)),
                component =>
                {
                    var fact = (Occultist0099FocusFact0099)component;
                    if (!fact.IsOwnedBy(unit)) return 0;
                    if (Value == Occultist0099SchoolValueKind.Resonance) return fact.WardingTalismanResonance(School);
                    if (Value == Occultist0099SchoolValueKind.ActualLevel) return fact.ActualLevel;
                    if (Value == Occultist0099SchoolValueKind.ActualDCLevel) return fact.ActualLevel / 2;
                    var power = fact.MindBarrier(School);
                    return Value == Occultist0099SchoolValueKind.EffectiveLevel ? power.EffectiveLevel
                        : power.SaveDC - 10 - (unit.Stats?.Intelligence?.Bonus ?? 0);
                }, 0);
    }

    [TypeId(Occultist0099Ids.SameRaceTypeId)]
    public sealed class Occultist0099SameRace : ContextCondition
    {
        protected override string GetConditionCaption() => "Target and occultist share the same race";

        protected override bool CheckCondition()
        {
            var race = Context?.MaybeCaster?.Progression.Race;
            return race != null && race == Target?.Unit?.Progression.Race;
        }
    }
}

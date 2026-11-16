using System;
using System.Collections.Generic;
using System.Linq;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.Designers.EventConditionActionSystem.Actions;
using Kingmaker.Designers.Mechanics.Buffs;
using Kingmaker.Designers.Mechanics.Facts;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.ElementsSystem;
using Kingmaker.Enums;
using Kingmaker.Localization;
using Kingmaker.PubSubSystem;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components;
using Kingmaker.UnitLogic.Abilities.Components.Base;
using Kingmaker.UnitLogic.Abilities.Components.TargetCheckers;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Buffs.Components;
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
    internal static class Occultist0099Transmutation
    {
        internal const string School = "Transmutation";
        internal const string Enlarge = "c60969e7f264e6d4b84a1499fdcf9039";
        internal const string Reduce = "4e0e9aba6447d514f88eff1464cc4763";
        private const string EnlargeBuff = "4f139d125bb602f48bfaec3d3e1937cb";
        private const string ReduceBuff = "b0793973c61a19744a8630468e8f4174";
        private const string Speed = "4f8181e7a7f1d904fbaea64220e83379";
        private const string SpeedBuff = "9ea4ec3dc30cd7940a372a4d699032e7";
        private const string Haste = "486eaff58293f6441a5c2759c4872f98";
        private const string HasteBuff = "03464790f40c3c24aa684b57155f3280";
        private const string HasteEffect = "17113651fa8b40c2a7bbf9721462e646";
        private const string Construct = "fd389783027d63343b4a5634bd81645f";
        private const string Undead = "734a29b693e9ec346ba2951b27987e33";
        private const string Elemental = "198fd8924dabcb5478d0f78bd453c586";

        internal static readonly StatType[] Attributes = { StatType.Strength, StatType.Dexterity, StatType.Constitution };
        internal static readonly string[] DonorIds = { Enlarge, Reduce, EnlargeBuff, ReduceBuff, Speed, SpeedBuff, Haste,
            HasteBuff, HasteEffect, Construct, Undead, Elemental };
        internal static IEnumerable<string> Tokens() => new[] { "TransmutationEffectiveLevel", "PhysicalEnhancementValue",
            "PhysicalEnhancementAbility", "TransmutationImplementFocus", "TransmutationInvestFocusAbility", "TransmutationReplenishFocusAbility",
            "SuddenSpeedAbility", "SuddenSpeedPower", "MindOverGravityBuff", "MindOverGravityAbility", "MindOverGravityPower",
            "SizeAlterationAbility", "EnlargeAlterationAbility", "ReduceAlterationAbility", "SizeAlterationPower",
            "QuicknessBuff", "QuicknessAbility", "QuicknessPower" }
            .Concat(Attributes.SelectMany(stat => new[] { "PhysicalEnhancement" + stat + "Buff", "PhysicalEnhancement" + stat + "Ability" }));

        internal static BlueprintScriptableObject[] Build(BlueprintsCache cache, Func<string, string, string, LocalizedString> text)
        {
            var enlarge = Load<BlueprintAbility>(cache, Enlarge);
            var reduce = Load<BlueprintAbility>(cache, Reduce);
            var enlargeEffect = Load<BlueprintBuff>(cache, EnlargeBuff);
            var reduceEffect = Load<BlueprintBuff>(cache, ReduceBuff);
            var speed = Load<BlueprintAbility>(cache, Speed);
            var speedEffect = Load<BlueprintBuff>(cache, SpeedBuff);
            var haste = Load<BlueprintAbility>(cache, Haste);
            var hasteBuff = Load<BlueprintBuff>(cache, HasteBuff);
            var hasteEffect = Load<BlueprintBuff>(cache, HasteEffect);
            var notLiving = new[] { Construct, Undead, Elemental }.Select(id => Load<BlueprintFeature>(cache, id)).ToArray();
            if (enlarge?.name != "EnlargePerson" || reduce?.name != "ReducePerson" ||
                enlargeEffect?.name != "EnlargePersonBuff" || reduceEffect?.name != "ReducePersonBuff" ||
                speed?.name != "ExpeditiousRetreat" || speedEffect?.GetComponent<BuffMovementSpeed>()?.Value != 30 ||
                haste?.name != "Haste" || hasteBuff?.name != "HasteBuff" ||
                hasteEffect?.GetComponent<BuffExtraAttack>() == null || notLiving.Any(feature => feature == null)) return null;

            var level = Occultist0099SchoolValue.Create("TransmutationEffectiveLevel", School, Occultist0099SchoolValueKind.EffectiveLevel);
            var resonance = Occultist0099SchoolValue.Create("PhysicalEnhancementValue", School, Occultist0099SchoolValueKind.Resonance);
            const string physicalText = "Choose Strength, Dexterity or Constitution before locking mental focus. Gain a +2 enhancement bonus for each 3 points invested in Transmutation, up to +2 at level 1 plus another +2 per 6 levels.";
            var physicalBuffs = Attributes.Select(stat =>
            {
                var rank = new ContextRankConfig { name = "PhysicalEnhancementLockedRank" };
                SetEnum(rank, "m_BaseValueType", "CustomProperty");
                Set(rank, "m_CustomProperty", Ref<BlueprintUnitPropertyReference>(resonance));
                var buff = Buff("PhysicalEnhancement" + stat + "Buff", "Physical Enhancement (" + stat + ")", physicalText, enlarge,
                    rank, new AddContextStatBonus { name = "OccultistPhysicalEnhancement", Descriptor = ModifierDescriptor.Enhancement,
                    Stat = stat, Value = Rank() });
                SetEnum(buff, "m_Flags", "StayOnDeath");
                return buff;
            }).ToArray();
            var choices = Attributes.Select(stat =>
            {
                var select = Operation("PhysicalEnhancement" + stat + "Ability", "Physical Enhancement (" + stat + ")", physicalText,
                    Occultist0099FocusAction.ChoosePhysical, UnitCommand.CommandType.Free, enlarge, text, School);
                select.GetComponent<Occultist0099FocusOperation>().PhysicalAttribute = stat.ToString();
                return select;
            }).ToArray();
            var physical = Wrap("PhysicalEnhancementAbility", "Physical Enhancement", physicalText, enlarge, text, choices);
            physical.ActionType = UnitCommand.CommandType.Free;
            physical.Type = AbilityType.Special;

            const string speedText = "Spend 1 point of Transmutation focus as a swift action to increase your speed by 30 feet for 1 minute.";
            var sudden = Power("SuddenSpeedAbility", "Sudden Speed", speedText, speed, level, text, AbilityRange.Personal,
                Run(Apply(speedEffect, 1, DurationRate.Minutes, toCaster: true)));
            sudden.ActionType = UnitCommand.CommandType.Swift;

            const string flightText = "Spend 1 point of Transmutation focus to fly and gain 10 feet of movement speed for 1 minute per occultist level. Flight protects from difficult terrain and ground effects and helps defend against grounded melee attackers.";
            var flightBuff = Buff("MindOverGravityBuff", "Mind Over Gravity", flightText, haste, PsychicTravelSpells.Flight());
            var flight = Power("MindOverGravityAbility", "Mind Over Gravity", flightText, haste, level, text, AbilityRange.Personal,
                Run(Timed(flightBuff, PropertyValue(level), DurationRate.Minutes, true)));

            const string sizeText = "Spend 1 point of Transmutation focus to enlarge or reduce yourself or a willing adjacent creature by one size category.";
            var sizeUp = Size("EnlargeAlterationAbility", "Size Alteration (Enlarge)", enlarge);
            var sizeDown = Size("ReduceAlterationAbility", "Size Alteration (Reduce)", reduce);
            if (sizeUp == null || sizeDown == null) return null;
            var size = Wrap("SizeAlterationAbility", "Size Alteration", sizeText, enlarge, text, sizeUp, sizeDown);

            const string quickText = "Spend 1 point of Transmutation focus to haste a willing living creature for 1 round per occultist level. Its haste bonuses to attack, AC and Reflex increase by another 1 for each 6 occultist levels.";
            var quickRank = new ContextRankConfig { name = "QuicknessExtraBonus" };
            SetEnum(quickRank, "m_BaseValueType", "CasterLevel");
            SetEnum(quickRank, "m_Progression", "DivStep");
            Set(quickRank, "m_StepLevel", 6);
            var quickBuff = Buff("QuicknessBuff", "Quickness", quickText, haste,
                hasteEffect.ComponentsArray.Where(component => !(component is AddStatBonus)).Select(CopyData).Concat(new BlueprintComponent[]
                {
                    quickRank, new ChangeImpatience { name = "QuicknessPace", Delta = 1 },
                    new PsychicVigorHasteStats { name = "QuicknessNativeHasteBonuses", NativeEffect = Ref<BlueprintBuffReference>(hasteEffect), Extra = Rank() },
                    new Occultist0099QuicknessCounter { name = "QuicknessNativeSlowCounter" }
                }).ToArray());
            var living = new AbilityTargetHasFact { name = "QuicknessLivingTarget", Inverted = true };
            Set(living, "m_CheckedFacts", notLiving.Select(Ref<BlueprintUnitFactReference>).ToArray());
            var quickRun = CopyData(haste.GetComponent<AbilityEffectRunAction>());
            if (quickRun == null) return null;
            var quickApply = SpellBatch01447.Elements(new[] { quickRun }, new HashSet<object>(IdentityComparer))
                .OfType<ContextActionApplyBuff>().SingleOrDefault(apply => ReferenceGuid(apply, "m_Buff") == hasteBuff.AssetGuid);
            if (quickApply == null) return null;
            Set(quickApply, "m_Buff", Ref<BlueprintBuffReference>(quickBuff));
            quickApply.DurationValue.BonusValue = PropertyValue(level);
            var quickness = Power("QuicknessAbility", "Quickness", quickText, haste, level, text, AbilityRange.Touch, quickRun, living);
            quickness.CanTargetFriends = true;

            var invest = Operation("TransmutationInvestFocusAbility", "Invest Mental Focus: Transmutation",
                "Invest 1 point of unallocated mental focus in Transmutation before locking your focus.",
                Occultist0099FocusAction.Invest, UnitCommand.CommandType.Free, enlarge, text, School);
            var replenish = Operation("TransmutationReplenishFocusAbility", "Replenish Mental Focus: Transmutation",
                "Spend 1 unallocated point of mental focus to replenish a spent Transmutation point.",
                Occultist0099FocusAction.Replenish, UnitCommand.CommandType.Free, enlarge, text, School);
            return new BlueprintScriptableObject[] { level, resonance, physical, sudden, flightBuff, flight, size, sizeUp,
                sizeDown, quickBuff, quickness, invest, replenish }.Concat(physicalBuffs).Concat(choices).ToArray();

            BlueprintBuff Buff(string token, string label, string description, BlueprintAbility icon, params BlueprintComponent[] components)
                => Identify(new BlueprintBuff { IsClassFeature = true, Stacking = StackingType.Replace, ComponentsArray = components },
                    token, label, description, icon, text);

            BlueprintAbility Size(string token, string label, BlueprintAbility source)
            {
                var action = CopyData(source.GetComponent<AbilityEffectRunAction>());
                var restriction = source.GetComponent<AbilityTargetHasFact>();
                if (action == null || restriction == null || !(Get(restriction, "m_CheckedFacts") is BlueprintUnitFactReference[] types)) return null;
                RemoveCreatureTypeGate(action.Actions, new HashSet<BlueprintGuid>(types.Select(type => type.Guid)));
                foreach (var apply in SpellBatch01447.Elements(new[] { action }, new HashSet<object>(IdentityComparer)).OfType<ContextActionApplyBuff>())
                    apply.DurationValue.BonusValue = PropertyValue(level);
                var ability = Power(token, label, sizeText, source, level, text, AbilityRange.Touch, action);
                ability.CanTargetFriends = true;
                return ability;
            }
        }

        private static void RemoveCreatureTypeGate(ActionList actions, HashSet<BlueprintGuid> types)
        {
            var next = new List<GameAction>();
            foreach (var action in actions.Actions)
            {
                if (action is Conditional branch)
                {
                    RemoveCreatureTypeGate(branch.IfTrue, types);
                    RemoveCreatureTypeGate(branch.IfFalse, types);
                    if (branch.IfTrue.Actions.Length == 0 && branch.ConditionsChecker.Conditions.Length > 0 &&
                        branch.ConditionsChecker.Conditions.All(condition => condition is ContextConditionHasFact &&
                            !condition.Not && types.Contains(ReferenceGuid(condition, "m_Fact"))))
                    {
                        next.AddRange(branch.IfFalse.Actions);
                        continue;
                    }
                }
                next.Add(action);
            }
            actions.Actions = next.ToArray();
        }

        internal static T Load<T>(BlueprintsCache cache, string id) where T : BlueprintScriptableObject
            => Occultist0099Illusion.Load<T>(cache, id);

        internal static ContextActionApplyBuff Timed(BlueprintBuff buff, ContextValue duration, DurationRate rate, bool toCaster = false)
        {
            var apply = Apply(buff, 0, rate, toCaster);
            apply.DurationValue.BonusValue = duration;
            return apply;
        }

        internal static BlueprintAbility Power(string token, string label, string description, BlueprintAbility icon, BlueprintUnitProperty level,
            Func<string, string, string, LocalizedString> text, AbilityRange range, params BlueprintComponent[] components)
            => Identify(new BlueprintAbility { Type = AbilityType.SpellLike, Range = range, ActionType = UnitCommand.CommandType.Standard,
                CanTargetSelf = true, NotOffensive = true, EffectOnAlly = AbilityEffectOnUnit.Helpful,
                ComponentsArray = components.Concat(new BlueprintComponent[]
                {
                    SchoolComponent(), new Occultist0099FocusResourceLogic { name = "OccultistTransmutationCost", School = School, Amount = 1 },
                    new ContextCalculateAbilityParams { name = "OccultistTransmutationLevel", StatType = StatType.Intelligence,
                        ReplaceCasterLevel = true, CasterLevel = PropertyValue(level) }
                }).ToArray() }, token, label, description, icon, text);

        internal static BlueprintAbility Wrap(string token, string label, string description, BlueprintAbility icon,
            Func<string, string, string, LocalizedString> text, params BlueprintAbility[] leaves)
        {
            var variants = new AbilityVariants { name = token + "Variants" };
            Set(variants, "m_Variants", leaves.Select(Ref<BlueprintAbilityReference>).ToArray());
            var root = Identify(new BlueprintAbility { Type = AbilityType.SpellLike, Range = leaves[0].Range,
                ActionType = UnitCommand.CommandType.Standard, ComponentsArray = new BlueprintComponent[] { variants, SchoolComponent() } },
                token, label, description, icon, text);
            foreach (var leaf in leaves) Set(leaf, "m_Parent", Ref<BlueprintAbilityReference>(root));
            return root;
        }

        private static ContextValue Rank() => new ContextValue { ValueType = ContextValueType.Rank };
        private static BlueprintGuid ReferenceGuid(object value, string field) => (Get(value, field) as BlueprintReferenceBase)?.Guid ?? BlueprintGuid.Empty;
        private static SpellComponent SchoolComponent() => new SpellComponent { name = "OccultistTransmutation", School = SpellSchool.Transmutation };
    }

    [TypeId(Occultist0099Ids.QuicknessCounterTypeId)]
    public sealed class Occultist0099QuicknessCounter : UnitBuffComponentDelegate, IInitiatorRulebookHandler<RuleCanApplyBuff>
    {
        private static readonly BlueprintGuid Slow = BlueprintGuid.Parse("f492622e473d34747806bdb39356eb89");
        private static readonly BlueprintGuid SlowBuff = BlueprintGuid.Parse("0bc608c3f2b548b44b7146b7530613ac");

        public void OnEventAboutToTrigger(RuleCanApplyBuff evt) { }

        public void OnEventDidTrigger(RuleCanApplyBuff evt)
        {
            // Native Slow already handles its save and any stock Haste before reaching this branch.
            if (!evt.CanApply || evt.Blueprint.AssetGuid != SlowBuff || evt.Context?.SourceAbility?.AssetGuid != Slow) return;
            evt.CanApply = false;
            Buff.Remove();
        }
    }

    [TypeId(Occultist0099Ids.TransmutationLevelTypeId)]
    public sealed class Occultist0099TransmutationLevel : BlueprintComponent, IAbilityVisibilityProvider, IAbilityCasterRestriction
    {
        public int Minimum;
        public int Maximum = int.MaxValue;
        public bool IsAbilityVisible(AbilityData ability) => IsCasterRestrictionPassed(ability?.Caster?.Unit);
        public bool IsCasterRestrictionPassed(UnitEntityData caster)
            => Occultist0099NativeScope.InRuntimeContext(
                Occultist0099NativeScope.RuntimeOf(caster, Occultist0099Ids.FocusFactGuid, typeof(Occultist0099FocusFact0099)),
                component =>
                {
                    int level = ((Occultist0099FocusFact0099)component).ActualLevel;
                    return level >= Minimum && level <= Maximum;
                }, false);
        public string GetAbilityCasterRestrictionUIText() => "Your occultist level does not allow this variant.";
    }
}

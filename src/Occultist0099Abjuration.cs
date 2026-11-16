using System;
using System.Linq;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.Blueprints.Facts;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.Designers.Mechanics.Buffs;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Enums;
using Kingmaker.Localization;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components;
using Kingmaker.UnitLogic.Abilities.Components.Base;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Commands.Base;
using Kingmaker.UnitLogic.FactLogic;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Mechanics.Properties;
using Kingmaker.Utility;
using static CallOfTheWild.Wrath.Prototype.PsychicAssets;

namespace CallOfTheWild.Wrath.Prototype
{
    // Creates a detached graph. The caller gives the two shared operations to
    // the class and the school feature to the first Abjuration implement.
    internal static class Occultist0099Abjuration
    {
        internal static BlueprintScriptableObject[] Build(BlueprintAbility shield, BlueprintAbility resistance,
            Func<string, string, string, LocalizedString> text)
        {
            const string mind = "As a swift action, spend 1 point of mental focus invested in Abjuration to gain 2 temporary hit points per effective occultist level. The barrier lasts for one round or until exhausted.";
            const string ward = "Gain a resistance bonus on all saving throws for every 2 points of mental focus invested in your Abjuration implement, up to 1 + 1 for every 4 occultist levels. The bonus lasts while your focus is locked and the implement remains available.";
            var absorption = Property("MindBarrierAbsorption", true);
            var wardBonus = Property("WardingTalismanResistance", false);
            var mindBuff = Identify(new BlueprintBuff
            {
                Stacking = StackingType.Replace,
                IsClassFeature = true,
                ComponentsArray = new BlueprintComponent[]
                {
                    new TemporaryHitPointsFromAbilityValue
                    {
                        name = "OccultistMindBarrierTemporaryHitPoints",
                        Descriptor = ModifierDescriptor.UntypedStackable,
                        Value = PropertyValue(absorption),
                        RemoveWhenHitPointsEnd = true
                    }
                }
            }, "MindBarrierBuff", "Mind Barrier", mind, shield, text);
            var effects = new BlueprintComponent[]
            {
                Run(Apply(mindBuff, 1, toCaster: true)),
                new SpellComponent { name = "OccultistMindBarrierSchool", School = SpellSchool.Abjuration },
                new Occultist0099FocusResourceLogic { name = "OccultistMindBarrierCost", Amount = 1 }
            };
            var visual = shield.GetComponent<AbilitySpawnFx>();
            var mindAbility = Identify(SelfAbility(AbilityType.SpellLike, UnitCommand.CommandType.Swift,
                visual == null ? effects : effects.Concat(new[] { CopyData(visual) }).ToArray()),
                "MindBarrierAbility", "Mind Barrier", mind, shield, text);
            var wardBuff = Identify(new BlueprintBuff
            {
                Stacking = StackingType.Replace,
                IsClassFeature = true,
                ComponentsArray = new[] { StatType.SaveFortitude, StatType.SaveReflex, StatType.SaveWill }
                    .Select(stat => (BlueprintComponent)new AddContextStatBonus
                    {
                        name = "OccultistWardingTalisman" + stat, Stat = stat,
                        Descriptor = ModifierDescriptor.Resistance, Value = PropertyValue(wardBonus)
                    }).ToArray()
            }, "WardingTalismanBuff", "Warding Talisman", ward, resistance, text);

            var invest = Operation("InvestFocusAbility", "Invest Mental Focus: Abjuration",
                "Invest 1 point of unallocated mental focus in Abjuration before locking your focus.",
                Occultist0099FocusAction.Invest, UnitCommand.CommandType.Free, shield, text);
            var replenish = Operation("ReplenishFocusAbility", "Replenish Mental Focus: Abjuration",
                "Spend 1 unallocated point of mental focus to replenish a spent Abjuration point.",
                Occultist0099FocusAction.Replenish, UnitCommand.CommandType.Free, shield, text);
            var reset = Operation("ResetFocusAbility", "Reset Mental Focus",
                "Return unspent invested points to your general pool before locking your focus.",
                Occultist0099FocusAction.Reset, UnitCommand.CommandType.Free, shield, text);
            var locked = Operation("LockFocusAbility", "Lock Mental Focus",
                "Finalize your investments and activate your implements' resonant powers until you rest.",
                Occultist0099FocusAction.Lock, UnitCommand.CommandType.Standard, shield, text);
            locked.SetIsFullRoundAction(true);
            var implement = Identify(new BlueprintFeature
            {
                IsClassFeature = true, Ranks = 1,
                ComponentsArray = new BlueprintComponent[]
                {
                    new Occultist0099ImplementSchool { name = "OccultistAbjurationSchool", School = Occultist0099Ids.SliceSchool },
                    Facts(mindAbility, invest, replenish)
                }
            }, "AbjurationImplementFocus", "Abjuration Implement", "Gain Mind Barrier and Warding Talisman.", shield, text);
            return new BlueprintScriptableObject[] { absorption, wardBonus, mindBuff, mindAbility, wardBuff,
                invest, replenish, reset, locked, implement };
        }

        internal static BlueprintAbility Operation(string token, string label, string description, Occultist0099FocusAction action,
            UnitCommand.CommandType command, BlueprintAbility icon, Func<string, string, string, LocalizedString> text,
            string school = Occultist0099Ids.SliceSchool)
            => Identify(SelfAbility(AbilityType.Special, command, new BlueprintComponent[]
                { new Occultist0099FocusOperation { name = "OccultistFocus" + action, Action = action, School = school } }),
                token, label, description, icon, text);

        private static BlueprintAbility SelfAbility(AbilityType type, UnitCommand.CommandType command,
            BlueprintComponent[] components) => new BlueprintAbility
            {
                Type = type, ActionType = command, Range = AbilityRange.Personal, CanTargetSelf = true,
                EffectOnAlly = AbilityEffectOnUnit.Helpful, ComponentsArray = components
            };

        private static BlueprintUnitProperty Property(string token, bool absorption)
            => Identify(new BlueprintUnitProperty
            {
                ComponentsArray = new BlueprintComponent[]
                {
                    new Occultist0099AbjurationProperty
                    { name = "Occultist" + token, Settings = new PropertySettings(), Absorption = absorption }
                }
            }, token, "", "", null);

        internal static ContextValue PropertyValue(BlueprintUnitProperty property)
        {
            var value = new ContextValue { ValueType = ContextValueType.CasterCustomProperty };
            Set(value, "m_CustomProperty", Ref<BlueprintUnitPropertyReference>(property));
            return value;
        }

        internal static T Identify<T>(T blueprint, string token, string label, string description,
            BlueprintAbility icon, Func<string, string, string, LocalizedString> text = null) where T : BlueprintScriptableObject
        {
            blueprint.AssetGuid = BlueprintGuid.Parse(Occultist0099Ids.Id(token));
            blueprint.name = "CotWWrathOccultist0099" + token;
            if (blueprint is BlueprintUnitFact)
            {
                Set(blueprint, "m_DisplayName", text?.Invoke(blueprint.name, "name", label) ?? Text(blueprint.name, "name", label));
                Set(blueprint, "m_Description", text?.Invoke(blueprint.name, "description", description) ?? Text(blueprint.name, "description", description));
                if (icon != null) Set(blueprint, "m_Icon", Get(icon, "m_Icon"));
            }
            return blueprint;
        }
    }

    [TypeId(Occultist0099Ids.FocusPropertyTypeId)]
    public sealed class Occultist0099AbjurationProperty : PropertyValueGetter
    {
        public bool Absorption;

        protected override int GetBaseValue(UnitEntityData unit)
            => Occultist0099NativeScope.InRuntimeContext(
                Occultist0099NativeScope.RuntimeOf(unit, Occultist0099Ids.FocusFactGuid, typeof(Occultist0099FocusFact0099)),
                component =>
                {
                    var focus = (Occultist0099FocusFact0099)component;
                    if (!focus.IsOwnedBy(unit)) return 0;
                    if (!Absorption) return focus.WardingTalismanResonance(Occultist0099Ids.SliceSchool);
                    return focus.CanRetainMindBarrier() ? focus.MindBarrier(Occultist0099Ids.SliceSchool).Absorption : 0;
                }, 0);
    }

    public enum Occultist0099FocusAction { Invest, Reset, Lock, Replenish }

    [TypeId(Occultist0099Ids.FocusOperationTypeId)]
    public sealed class Occultist0099FocusOperation : AbilityApplyEffect, IAbilityRestriction
    {
        public Occultist0099FocusAction Action;
        public string School = Occultist0099Ids.SliceSchool;

        public bool IsAbilityRestrictionPassed(AbilityData ability) => CanExecute(ability, null);

        internal bool CanExecute(AbilityData ability, string executionId) => Operate(ability, executionId, false);

        public string GetAbilityRestrictionUIText() => "Your current mental focus does not allow this operation.";

        public override void Apply(AbilityExecutionContext context, TargetWrapper target)
        {
            var caster = context?.MaybeCaster;
            if (caster != null && target?.Unit == caster && context.Ability?.Caster?.Unit == caster)
                Operate(context.Ability, Occultist0099NativeCast.IdentityOf(context), true);
        }

        private bool Operate(AbilityData ability, string executionId, bool commit)
            => Occultist0099NativeScope.InRuntimeContext(Occultist0099NativeScope.FocusRuntime(ability),
                component => ((Occultist0099FocusFact0099)component).Operate(Action, School, executionId, commit), false);
    }

    [TypeId(Occultist0099Ids.ImplementSchoolTypeId)]
    public sealed class Occultist0099ImplementSchool : UnitFactComponentDelegate
    {
        public string School;

        protected override void OnActivate() => UpdateFocus(true);
        protected override void OnDeactivate() => UpdateFocus(false);

        private void UpdateFocus(bool present)
            => Occultist0099NativeScope.InFactContext(Owner, Occultist0099Ids.FocusFactGuid,
                typeof(Occultist0099FocusFact0099), component => ((Occultist0099FocusFact0099)component).SetSchoolPresent(School, present));
    }
}

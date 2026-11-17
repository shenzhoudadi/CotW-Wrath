using System;
using System.Collections.Generic;
using System.Linq;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Facts;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.Enums;
using Kingmaker.Localization;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components;
using Kingmaker.UnitLogic.Abilities.Components.Base;
using Kingmaker.UnitLogic.Abilities.Components.TargetCheckers;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Commands.Base;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Mechanics.Actions;
using Kingmaker.Utility;
using Kingmaker.View;
using static CallOfTheWild.Wrath.Prototype.Occultist0099Abjuration;
using static CallOfTheWild.Wrath.Prototype.PsychicAssets;

namespace CallOfTheWild.Wrath.Prototype
{
    // A:8 / C:20. Level-8 Repower Construct: one generic point (the native
    // Occultist0099FocusResourceLogic pays it through PayGeneric, which is the
    // only entry that is allowed before lock), a real destroyed-construct check,
    // an own summon pool and the 8/12/16 duration ladder. The corpse stays
    // recorded twice: the authoritative Occultist0099FocusState.ConsumedCorpses
    // entry and the consumed fact on the unit, which the target restriction
    // reads without opening the focus runtime.
    internal static class Occultist0099Repower
    {
        private const string Construct = "fd389783027d63343b4a5634bd81645f";
        internal static readonly string[] Donors = { Construct };
        internal static readonly string[] Tokens =
        {
            "RepowerConstructAbility", "RepowerConstructSummonPool",
            "RepowerConstructLifecycleBuff", "RepowerConstructConsumed",
            "RepowerConstructFeature"
        };

        internal static BlueprintScriptableObject[] Build(BlueprintsCache cache,
            Func<string, string, string, LocalizedString> text)
        {
            var construct = Occultist0099Illusion.Load<BlueprintFeature>(cache, Construct);
            var icon = Occultist0099Illusion.Load<BlueprintAbility>(cache, Occultist0099Necromancy.AnimateDeadLesser);
            if (construct == null || icon == null) return null;
            var constructRef = Ref<BlueprintFeatureReference>(construct);

            const string text8 = "Spend one point of mental focus as a swift action to restore a destroyed construct within 30 feet. It returns with full hit points and serves you for 1 round at 8th level, for a number of rounds equal to your occultist level at 12th level, and for a number of minutes equal to your occultist level at 16th level. A construct can be repowered only once.";
            var pool = Identify(new BlueprintSummonPool { ComponentsArray = Array.Empty<BlueprintComponent>() },
                "RepowerConstructSummonPool", "Repowered Construct", "", icon, text);
            var poolRef = Ref<BlueprintSummonPoolReference>(pool);

            var consumed = Identify(new BlueprintFeature { Ranks = 1, IsClassFeature = true,
                ComponentsArray = Array.Empty<BlueprintComponent>() },
                "RepowerConstructConsumed", "Repowered Construct", "", icon, text);
            var lifecycle = Identify(new BlueprintBuff { IsClassFeature = true, Stacking = StackingType.Replace,
                ComponentsArray = new BlueprintComponent[]
                {
                    new Occultist0099RepowerLifecycle { name = "OccultistRepowerLifecycle", Pool = poolRef }
                } },
                "RepowerConstructLifecycleBuff", "Repowered Construct", "", icon, text);

            var action = new Occultist0099RepowerConstruct
            {
                name = "OccultistRepowerConstruct",
                Pool = poolRef,
                ConstructType = constructRef,
                Lifecycle = Ref<BlueprintBuffReference>(lifecycle),
                Consumed = Ref<BlueprintFeatureReference>(consumed)
            };
            var run = new AbilityEffectRunAction { name = "OccultistRepowerRun", Actions = Actions(action) };
            var ability = Identify(new BlueprintAbility
            {
                Type = AbilityType.Supernatural,
                Range = AbilityRange.Medium,
                ActionType = UnitCommand.CommandType.Swift,
                CanTargetEnemies = true,
                CanTargetFriends = false,
                CanTargetSelf = false,
                ShouldTurnToTarget = true,
                ComponentsArray = new BlueprintComponent[]
                {
                    run,
                    new Occultist0099RepowerTarget { name = "OccultistRepowerTarget", ConstructType = constructRef,
                        Consumed = Ref<BlueprintFeatureReference>(consumed) },
                    new Occultist0099FocusResourceLogic { name = "OccultistRepowerCost", GenericPool = true, Amount = 1,
                        PayOnEffect = true }
                }
            }, "RepowerConstructAbility", "Repower Construct", text8, icon, text);
            return new BlueprintScriptableObject[] { pool, consumed, lifecycle, ability };
        }
    }

    [TypeId(Occultist0099Ids.RepowerTargetTypeId)]
    public sealed class Occultist0099RepowerTarget : BlueprintComponent, IAbilityTargetRestriction, ICanTargetDeadUnits
    {
        public BlueprintFeatureReference ConstructType;
        public BlueprintFeatureReference Consumed;

        public bool IsTargetRestrictionPassed(UnitEntityData caster, TargetWrapper target)
            => Occultist0099RepowerConstruct.Repowerable(caster, target?.Unit, ConstructType?.Get(), Consumed?.Get());

        public string GetAbilityTargetRestrictionUIText(UnitEntityData caster, TargetWrapper target)
            => "The target must be a destroyed construct that has not been repowered yet.";
    }

    [TypeId(Occultist0099Ids.RepowerConstructTypeId)]
    public sealed class Occultist0099RepowerConstruct : ContextAction
    {
        private static readonly HashSet<UnitEntityData> PendingCorpses = new HashSet<UnitEntityData>();

        public BlueprintSummonPoolReference Pool;
        public BlueprintFeatureReference ConstructType;
        public BlueprintBuffReference Lifecycle;
        public BlueprintFeatureReference Consumed;

        public override string GetCaption() => "Repower a destroyed construct";

        public override void RunAction()
        {
            var caster = Context?.MaybeCaster;
            var corpse = Target?.Unit;
            var pool = Pool?.Get();
            var construct = ConstructType?.Get();
            var consumed = Consumed?.Get();
            var lifecycle = Lifecycle?.Get();
            var execution = Context as AbilityExecutionContext ?? Context?.SourceAbilityContext;
            if (Context == null || pool == null || lifecycle == null) return;
            if (!(execution?.Ability?.ResourceLogic is Occultist0099FocusResourceLogic logic) ||
                !Occultist0099NativeCast.TryGetPaymentIdentity(execution, out var identity) ||
                !logic.CanPay(execution.Ability, identity)) return;
            if (!Repowerable(caster, corpse, construct, consumed)) return;
            var runtime = Occultist0099NativeScope.FocusRuntime(execution.Ability);
            // Check both payment eligibility and corpse availability before
            // summoning; commit the generic point and corpse record only after
            // RuleSummonUnit actually creates a friendly construct.
            bool canRecord = Occultist0099NativeScope.InRuntimeContext(runtime,
                component => ((Occultist0099FocusFact0099)component).CanConsumeConstruct(corpse.UniqueId), false);
            if (!canRecord || !PendingCorpses.Add(corpse)) return;
            try
            {
                int level = Occultist0099NativeScope.InRuntimeContext(runtime,
                    component => ((Occultist0099FocusFact0099)component).RepowerLevel(), 0);
                var duration = LifeRounds(level);
                var position = Target.Point;
                if (corpse.View != null)
                {
                    FreePlaceSelector.PlaceSpawnPlaces(1, corpse.View.Corpulence, corpse.Position);
                    position = FreePlaceSelector.GetRelaxedPosition(0, true);
                }
                var summoned = Context.TriggerRule(new RuleSummonUnit(caster, corpse.Blueprint, position, duration, 0)
                {
                    Context = Context
                }).SummonedUnit;
                if (summoned == null) return;
                bool committed = Repowerable(caster, corpse, construct, consumed) &&
                    ReferenceEquals(runtime, Occultist0099NativeScope.FocusRuntime(execution.Ability)) &&
                    Occultist0099NativeScope.InRuntimeContext(runtime,
                        component => ((Occultist0099FocusFact0099)component)
                            .TryRepowerConstruct(caster, corpse.UniqueId, identity), false);
                if (!committed)
                {
                    summoned.MarkForDestroy();
                    return;
                }

                corpse.Descriptor.AddFact(consumed);
                Occultist0099NativeScope.InRuntimeContext(runtime, component =>
                {
                    ((Occultist0099FocusFact0099)component).SyncResourceMirror();
                    return true;
                }, false);
                summoned.Descriptor.State.AddCondition(UnitCondition.Unlootable);
                Kingmaker.Game.Instance?.SummonPools?.Register(pool, summoned);
                var buff = summoned.Descriptor.AddBuff(lifecycle, Context, duration.Seconds);
                if (buff != null) buff.IsNotDispelable = true;
            }
            finally { PendingCorpses.Remove(corpse); }
        }

        internal static bool Repowerable(UnitEntityData caster, UnitEntityData corpse,
            BlueprintFeature construct, BlueprintFeature consumed)
            => caster != null && corpse?.Blueprint != null && construct != null && consumed != null &&
               corpse != caster && !corpse.IsPlayerFaction && corpse.State.IsDead &&
               !corpse.State.HasCondition(UnitCondition.Petrified) &&
               corpse.Descriptor.OriginalSize <= Size.Colossal &&
               corpse.Descriptor.HasFact(construct) && !corpse.Descriptor.HasFact(consumed);

        // A:8: 1 round at 8, occultist level in rounds at 12, occultist level
        // in minutes at 16. A round is six seconds of game time.
        internal static Rounds LifeRounds(int level)
            => new Rounds(level >= 16 ? 10 * level : level >= 12 ? level : 1);

        internal static TimeSpan LifeSpan(int level) => LifeRounds(level).Seconds;
    }

    // The repowered construct leaves the field when its own duration ends. The
    // pool entry is released and the native summoned-unit cleanup disposes the
    // unit exactly as it does for a native summon.
    [TypeId(Occultist0099Ids.RepowerLifecycleTypeId)]
    public sealed class Occultist0099RepowerLifecycle : UnitFactComponentDelegate
    {
        public BlueprintSummonPoolReference Pool;

        protected override void OnDeactivate()
        {
            var unit = Owner;
            if (unit == null || unit.IsDisposed) return;
            var live = Kingmaker.Game.Instance?.SummonPools?.GetPool(Pool?.Get());
            live?.Unregister(unit, true);
            var summoned = Kingmaker.Game.Instance?.BlueprintRoot?.SystemMechanics?.SummonedUnitBuff;
            if (summoned == null) return;
            foreach (var buff in unit.Buffs.Enumerable.Where(b => b.Blueprint == summoned).ToArray()) buff.Remove();
        }
    }
}

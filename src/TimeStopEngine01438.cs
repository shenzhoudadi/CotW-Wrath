// Adapted from Holic75/KingmakerRebalance, MIT; see LICENSE-CotW.txt.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using HarmonyLib;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.Designers.Mechanics.Facts;
using Kingmaker.ElementsSystem;
using Kingmaker.EntitySystem;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.RuleSystem;
using Kingmaker.PubSubSystem;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components;
using Kingmaker.UnitLogic.Abilities.Components.AreaEffects;
using Kingmaker.UnitLogic.Buffs;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Buffs.Components;
using Kingmaker.UnitLogic.Commands.Base;
using Kingmaker.UnitLogic.FactLogic;
using Kingmaker.UnitLogic.Mechanics.Actions;
using Kingmaker.UnitLogic.Mechanics.Conditions;
using Kingmaker.Utility;
using static CallOfTheWild.Wrath.Prototype.PsychicAssets;

namespace CallOfTheWild.Wrath.Prototype
{
    // Only exact private graph objects acquire the exception below. Native
    // untargetability and source-area cleanup otherwise remain authoritative.
    internal static class TimeStopEngine01438
    {
        internal sealed class Graph
        {
            internal BlueprintBuff Host;
            internal BlueprintAbilityAreaEffect Area;
            internal BlueprintBuff Effect;
        }
        sealed class Registration { internal BlueprintAbilityAreaEffect Area; internal BlueprintBuff Effect; internal BlueprintBuff Host; }
        sealed class Contributions { internal readonly HashSet<Buff> Buffs = new HashSet<Buff>(); }
        static readonly object Sync = new object();
        static readonly ConditionalWeakTable<BlueprintAbilityAreaEffect, Registration> Areas = new ConditionalWeakTable<BlueprintAbilityAreaEffect, Registration>();
        static readonly ConditionalWeakTable<BlueprintBuff, Registration> Effects = new ConditionalWeakTable<BlueprintBuff, Registration>();
        static readonly ConditionalWeakTable<BlueprintBuff, Registration> Hosts = new ConditionalWeakTable<BlueprintBuff, Registration>();
        static readonly ConditionalWeakTable<BlueprintAbilityAreaEffect, Graph> Staged = new ConditionalWeakTable<BlueprintAbilityAreaEffect, Graph>();
        static readonly ConditionalWeakTable<UnitEntityData, Contributions> Units = new ConditionalWeakTable<UnitEntityData, Contributions>();

        internal static Graph BuildGraph(PsychicAssets factory, string hostToken, string areaToken, string effectToken, string displayName, string description)
        {
            var host = factory.Native<BlueprintBuff>(TimeStutter01437.NativeBuff);
            var area = factory.Native<BlueprintAbilityAreaEffect>(TimeStutter01437.NativeArea);
            var effect = factory.Native<BlueprintBuff>(TimeStutter01437.NativeEffect);
            var sourceAreas = host.GetComponents<AddAreaEffect>().ToArray();
            if (host.ComponentsArray.Length != 1 || sourceAreas.Length != 1 || sourceAreas[0].AreaEffect?.AssetGuid != area.AssetGuid || effect.GetComponents<Untargetable>().Count() != 1)
                throw new InvalidOperationException("Native Time Stop graph changed; private graph construction deferred.");
            var copiedEffect = factory.Clone(effect, effectToken, displayName, "Time is stopped for this creature until the source area ends.");
            Set(copiedEffect, "m_AllElements", new List<Element>()); copiedEffect.Stacking = StackingType.Stack;
            copiedEffect.ComponentsArray = copiedEffect.ComponentsArray.Concat(new BlueprintComponent[] { new TimeStopFreezeMarker01438 { name = "$TimeStopFreezeMarker01438$" } }).ToArray();
            var copiedArea = factory.Clone(area, areaToken, displayName, description); Set(copiedArea, "m_AllElements", new List<Element>());
            var notCaster = new ContextConditionIsCaster { Not = true };
            var areaBuff = new AbilityAreaEffectBuff { Condition = new ConditionsChecker { Conditions = new Condition[] { notCaster } } };
            Set(areaBuff, "m_Buff", Ref<BlueprintBuffReference>(copiedEffect));
            copiedArea.ComponentsArray = new BlueprintComponent[] { areaBuff }; copiedArea.AddToElementsList(notCaster);
            var copiedHost = factory.Clone(host, hostToken, displayName, description); Set(copiedHost, "m_AllElements", new List<Element>());
            var grant = new AddAreaEffect(); Set(grant, "m_AreaEffect", Ref<BlueprintAbilityAreaEffectReference>(copiedArea));
            copiedHost.ComponentsArray = new BlueprintComponent[] { grant };
            var graph = new Graph { Host = copiedHost, Area = copiedArea, Effect = copiedEffect };
            Staged.Add(copiedArea, graph); return graph;
        }

        internal static void PrepareAbilityClone(BlueprintAbility ability)
        {
            Set(ability, "m_AllElements", new List<Element>());
            foreach (string field in new[] { "m_CachedRestrictions", "m_CachedTargetRestrictions", "m_CachedCasterRestrictions", "m_SpellComponent", "m_AbilityIsFullRoundInTurnBased", "m_StickyTouch", "m_AbilityVariants", "m_AbilityShadowSpell", "m_AbilityKineticist", "m_HasVariants", "m_IsCantrip" }) {
                var info = AccessTools.Field(typeof(BlueprintAbility), field);
                if (info == null) throw new MissingFieldException(typeof(BlueprintAbility).FullName, field);
                Set(ability, field, info.FieldType.IsValueType ? Activator.CreateInstance(info.FieldType) : null);
            }
            var run = ability.GetComponents<AbilityEffectRunAction>().ToArray();
            if (run.Length != 1 || run[0].Actions?.Actions?.Length != 1 || !(run[0].Actions.Actions[0] is ContextActionApplyBuff apply))
                throw new InvalidOperationException("Native Time Stop action changed; ability copy deferred.");
            ability.AddToElementsList(apply);
        }

        // True is a transaction receipt: callers undo only a registration that
        // this attempt added. A repeat cannot remove a prior healthy receipt.
        internal static bool RegisterGraph(BlueprintsCache cache, BlueprintAbilityAreaEffect area, BlueprintBuff effect)
        {
            if (cache == null || area == null || effect == null || !ReferenceEquals(cache.Load(area.AssetGuid), area) || !ReferenceEquals(cache.Load(effect.AssetGuid), effect))
                throw new InvalidOperationException("Time Stop graph publication identity does not match the cache.");
            if (!Staged.TryGetValue(area, out var staged) || !ReferenceEquals(staged.Effect, effect) || !ReferenceEquals(cache.Load(staged.Host.AssetGuid), staged.Host) || !ReferenceEquals(staged.Host.GetComponent<AddAreaEffect>()?.AreaEffect, area))
                throw new InvalidOperationException("Time Stop host publication does not match its staged private graph.");
            var buffs = area.GetComponents<AbilityAreaEffectBuff>().ToArray();
            if (buffs.Length != 1 || !ReferenceEquals(buffs[0].Buff, effect) || effect.GetComponents<Untargetable>().Count() != 1 || effect.GetComponents<TimeStopFreezeMarker01438>().Count() != 1)
                throw new InvalidOperationException("Time Stop graph has no exact source-owned freeze marker.");
            lock (Sync) {
                if (Areas.TryGetValue(area, out var old)) {
                    if (!ReferenceEquals(old.Effect, effect)) throw new InvalidOperationException("Time Stop area already owns a different freeze effect.");
                    return false;
                }
                if (Effects.TryGetValue(effect, out _)) throw new InvalidOperationException("Time Stop freeze effect already belongs to another area graph.");
                var receipt = new Registration { Area = area, Effect = effect, Host = staged.Host };
                Effects.Add(effect, receipt);
                try { Hosts.Add(staged.Host, receipt); Areas.Add(area, receipt); } catch { Effects.Remove(effect); Hosts.Remove(staged.Host); throw; }
                return true;
            }
        }
        internal static void UnregisterGraph(BlueprintAbilityAreaEffect area, BlueprintBuff effect)
        {
            if (area == null || effect == null) return;
            lock (Sync) if (Areas.TryGetValue(area, out var old) && ReferenceEquals(old.Effect, effect)) {
                Areas.Remove(area); Hosts.Remove(old.Host);
                if (Effects.TryGetValue(effect, out var corresponding) && ReferenceEquals(corresponding, old)) Effects.Remove(effect);
            }
        }
        internal static bool IsRegisteredFreeze(BlueprintBuff effect) => effect != null && Effects.TryGetValue(effect, out _);
        internal static bool IsOwnHost(BlueprintBuff host) => host != null && Hosts.TryGetValue(host, out _);
        internal static bool IsProtectedTimerBuff(BlueprintBuff buff) => IsRegisteredFreeze(buff) || IsOwnHost(buff);
        internal static void SetContribution(Buff buff, bool on)
        {
            // Restoring the fact manager precedes Descriptor.PostLoad and its
            // Unit back-reference. Never implicitly convert an unbound
            // descriptor into a null ConditionalWeakTable key.
            var owner = buff?.Owner?.Unit;
            if (owner == null) return;
            if (!on) {
                if (Units.TryGetValue(owner, out var existing)) lock (existing) existing.Buffs.Remove(buff);
                return;
            }
            if (!Effects.TryGetValue(buff.Blueprint, out _)) return;
            var own = Units.GetValue(owner, _ => new Contributions());
            lock (own) own.Buffs.Add(buff);
        }
        internal static int OwnContributions(UnitEntityData owner)
        {
            if (owner == null || !Units.TryGetValue(owner, out var own)) return 0;
            lock (own) return own.Buffs.Count;
        }
        internal static bool IsUntargetableForArea(CountableFlag flag, EntityDataBase initiator, UnitEntityData target)
        {
            if (!(bool)flag) return false;
            if (!(initiator is AreaEffectEntityData area) || !Areas.TryGetValue(area.Blueprint, out _)) return true;
            // No mutation, no buff scan, no allocation: another mod's retained
            // flag remains effective even while several own areas overlap.
            return flag.Count > OwnContributions(target);
        }
    }

    [TypeId("3e550fe44e8849c4a3a023f8a7c8f13c")]
    public sealed class TimeStopFreezeMarker01438 : UnitBuffComponentDelegate<TimeStopFreezeState01438>, IUnitBuffHandler
    {
        protected override void OnTurnOn() { TimeStopEngine01438.SetContribution(Buff, true); TimeStopTimers01438.Retain(Buff, Data); }
        protected override void OnTurnOff() { TimeStopEngine01438.SetContribution(Buff, false); TimeStopTimers01438.Release(Buff, Data); }
        protected override void OnDeactivate() { TimeStopEngine01438.SetContribution(Buff, false); TimeStopTimers01438.Release(Buff, Data); }
        protected override void OnApplyPostLoadFixes() {
            bool on = Fact.IsTurnedOn && !Fact.IsDisposed && !Buff.IsSuppressed;
            TimeStopEngine01438.SetContribution(Buff, on);
            if (on) TimeStopTimers01438.Retain(Buff, Data); else TimeStopTimers01438.Release(Buff, Data);
        }
        public void HandleBuffDidAdded(Buff buff) { if (ReferenceEquals(buff?.Owner?.Unit, Owner)) TimeStopTimers01438.Added(buff); }
        public void HandleBuffDidRemoved(Buff buff) { if (ReferenceEquals(buff?.Owner?.Unit, Owner)) TimeStopTimers01438.Removed(buff); }
    }

    [HarmonyPatch(typeof(UnitCommand), nameof(UnitCommand.CommandTargetUntargetable), new[] { typeof(EntityDataBase), typeof(UnitEntityData), typeof(RulebookEvent) })]
    internal static class TimeStopAreaUntargetable01438Patch
    {
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> input)
        {
            var code = input.ToList(); int count = 0;
            for (int i = 0; i < code.Count; i++) {
                var instruction = code[i];
                if (i > 0 && code[i - 1].operand is FieldInfo field && field.Name == "IsUntargetable" &&
                    instruction.operand is MethodInfo method && method.DeclaringType == typeof(CountableFlag) && method.Name == "op_Implicit") {
                    var initiator = new CodeInstruction(OpCodes.Ldarg_0); initiator.labels.AddRange(instruction.labels); instruction.labels.Clear();
                    yield return initiator; yield return new CodeInstruction(OpCodes.Ldarg_1);
                    yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(TimeStopEngine01438), nameof(TimeStopEngine01438.IsUntargetableForArea))); count++;
                } else yield return instruction;
            }
            if (count != 1) throw new InvalidOperationException("Native area untargetability conversion changed; Time Stop patch refused.");
        }
    }
}

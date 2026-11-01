using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using HarmonyLib;
using Newtonsoft.Json;
using Kingmaker;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Prerequisites;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.Controllers.Units;
using Kingmaker.Designers.Mechanics.Facts;
using Kingmaker.Designers.EventConditionActionSystem.Actions;
using Kingmaker.ElementsSystem;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.RuleSystem;
using Kingmaker.PubSubSystem;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.RuleSystem.Rules.Abilities;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.FactLogic;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Mechanics.Components;
using Kingmaker.UnitLogic.Mechanics.ContextData;
using Kingmaker.UnitLogic.Buffs;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Mechanics.Actions;
using static CallOfTheWild.Wrath.Prototype.PsychicAssets;
using TurnBased.Controllers;

namespace CallOfTheWild.Wrath.Prototype
{
    internal static class ImprovedTwoWeaponFeint
    {
        internal const string ModuleName = "Improved Two-Weapon Feint";
        internal const string Token = "ImprovedTwoWeaponFeintFeat";
        internal const string NativeFeat = "92c7e14b4da24549a9219e990b30d723", NativeMode = "621523572cfe4dc48ee1bdddba2f4725";
        internal const string Feint = "c610310d31414edabcedf0c8a6fe32c4", Flag = "e14f5e333e0e42fbbb7d7791c6072e7b";
        internal static BlueprintGuid FeatId => Id(Token);
        internal static ImprovedFeintWindow Component(UnitEntityData unit) => unit?.Facts.List
            .Where(f => f.Blueprint.AssetGuid == FeatId && T11TrainingOptions.Active(unit,f))
            .SelectMany(f => f.Components).Select(c => c.SourceBlueprintComponent).OfType<ImprovedFeintWindow>().FirstOrDefault();
        internal static bool Has(UnitEntityData unit) => Component(unit) != null;
        internal static IEnumerable<GameAction> Walk(ActionList list)
        {
            foreach(var a in list?.Actions ?? Array.Empty<GameAction>()) {
                yield return a;
                if(a is Conditional c) foreach(var nested in Walk(c.IfTrue).Concat(Walk(c.IfFalse))) yield return nested;
            }
        }
        static PrerequisiteFeature Need(PsychicAssets f,string id)
        { var p=new PrerequisiteFeature();Set(p,"m_Feature",Ref<BlueprintFeatureReference>(f.Native<BlueprintFeature>(id)));return p; }
        internal static void Register(BlueprintsCache cache)
        {
            var f=new PsychicAssets(cache);var native=f.Native<BlueprintFeature>(NativeFeat);var mode=f.Native<BlueprintBuff>(NativeMode);
            var candidates=mode.ComponentsArray.OfType<AddInitiatorAttackWithWeaponTrigger>().Where(c=>c.TriggerBeforeAttack && c.OnlyOnFullAttack && c.OnlyOnFirstAttack &&
                Walk(c.Action).OfType<ContextActionCastSpell>().Any(cast=>cast.Spell?.AssetGuid.ToString()==SpellAndSwordFeats.FeintAbility)).ToArray();
            if(candidates.Length!=1) throw new InvalidOperationException("Native Two-Weapon Feint trigger is absent or ambiguous; upgrade skipped.");
            if(Walk(candidates[0].Action).OfType<ContextActionCastSpell>().Count(cast=>cast.Spell?.AssetGuid.ToString()==SpellAndSwordFeats.FeintAbility)!=1)
                throw new InvalidOperationException("Native Two-Weapon Feint cast differs from expected structure.");
            const string text="Requires Dexterity 17, Intelligence 13, base attack bonus +6, Combat Expertise, Two-Weapon Fighting, Improved Two-Weapon Fighting and the native Two-Weapon Feint feat. Use the native Two-Weapon Feint mode with melee weapons: it retains its normal attack sacrifice, target restrictions and Bluff (or Slayer's Feint Mobility) check. On success, your remaining attacks against that opponent ignore its Dexterity bonus to AC until the end of your turn (six seconds in real-time). Other allies do not gain this benefit. Ordinary move-action Feint and Swordplay Upset are unchanged. Final Feint keeps its existing stronger effect.";
            var feat=f.Feature(Token,"Improved Two-Weapon Feint",text,new PrerequisiteStatValue {Stat=StatType.Dexterity,Value=17},
                new PrerequisiteStatValue {Stat=StatType.Intelligence,Value=13},new PrerequisiteStatValue {Stat=StatType.BaseAttackBonus,Value=6},
                Need(f,"4c44724ffa8844f4d9bedb5bb27d144a"),Need(f,"ac8aaf29054f5b74eb18f2af950e752d"),Need(f,"9af88f3ed8a017b45a6837eab7437629"),Need(f,NativeFeat),new ImprovedFeintWindow());
            feat.ComponentsArray=feat.ComponentsArray.Concat(native.ComponentsArray.Where(c=>c.GetType().Name=="DlcCondition").Select(CopyData)).ToArray();
            feat.Groups=new[]{FeatureGroup.Feat,FeatureGroup.CombatFeat};Set(feat,"m_Icon",PsychicOriginalIcons.Load("TwoWeaponFeintImproved"));
            // Preserve the shared native component/action graph, including its
            // concrete action types, for other mods' action-tree scanners.
            SpellResourceFeats.Commit(cache,f,new[]{feat});
            CombatFeatPoolRepair.Reconcile(cache);
        }
    }
    public sealed class ImprovedFeintMark
    {
        [JsonProperty,JsonConverter(typeof(ChargeUnitReferenceConverter))] public UnitReference Target;
        [JsonProperty] public string BuffId;
        [JsonProperty] public TimeSpan Expires;
    }
    public sealed class ImprovedFeintData { [JsonProperty] public List<ImprovedFeintMark> Marks=new List<ImprovedFeintMark>(); [JsonProperty] public int Epoch; }
    [TypeId("e00e6c7256a3484daa88c0f773573c53")]
    public sealed class ImprovedFeintWindow:UnitFactComponentDelegate<ImprovedFeintData>,ITickEachRound,IGlobalSubscriber
    {
        internal void Mark(UnitEntityData target,Buff buff,TimeSpan expires)
        {
            if(target==null || buff==null || buff.Context?.MaybeCaster!=Owner) return;
            Data.Marks.RemoveAll(m=>m.Target.Value==target);
            Data.Marks.Add(new ImprovedFeintMark {Target=target,BuffId=buff.UniqueId,Expires=expires});
        }
        internal bool Keep(UnitEntityData target) => target!=null && Data.Marks.Any(m=>m.Target.Value==target && m.Expires>Game.Instance.TimeController.GameTime &&
            target.Buffs.Enumerable.Any(b=>b.UniqueId==m.BuffId && b.IsActive && b.IsTurnedOn && !b.IsSuppressed && !b.IsDisposed && b.Context?.MaybeCaster==Owner));
        internal void Clear(bool expiredOnly=false)
        {
            if(!expiredOnly)Data.Epoch++;
            foreach(var m in Data.Marks.Where(m=>!expiredOnly || m.Expires<=Game.Instance.TimeController.GameTime || m.Target.Value==null).ToArray()) {
                var buff=m.Target.Value?.Buffs?.Enumerable.FirstOrDefault(b=>b.UniqueId==m.BuffId && b.Context?.MaybeCaster==Owner);
                buff?.Remove();Data.Marks.Remove(m);
            }
        }
        public void OnNewRound()=>Clear(true);
        protected override void OnTurnOff()=>Clear();
        protected override void OnApplyPostLoadFixes()=>Clear(true);
    }
    internal sealed class ImprovedFeintPending
    {
        internal UnitEntityData Caster,Target;internal ImprovedFeintData Data;internal int Epoch;internal TimeSpan Expires;
    }
    // Native casts enqueue an AbilityExecutionProcess. Carry identity with that
    // exact execution context; a thread-local window alone would end too early.
    [HarmonyPatch(typeof(RuleCastSpell),nameof(RuleCastSpell.OnTrigger))]
    internal static class ImprovedFeintCastPatch
    {
        internal static readonly ConditionalWeakTable<AbilityExecutionContext,ImprovedFeintPending> Pending=new ConditionalWeakTable<AbilityExecutionContext,ImprovedFeintPending>();
        [HarmonyPostfix] static void Postfix(RuleCastSpell __instance)
        {
            var caster=ImprovedFeintNativeCastPatch.CurrentCaster;var target=ImprovedFeintNativeCastPatch.CurrentTarget;
            if(caster==null || target==null || __instance.Context==null || __instance.Spell?.Blueprint.AssetGuid.ToString()!=SpellAndSwordFeats.FeintAbility || __instance.Context.MaybeCaster!=caster) return;
            var component=ImprovedTwoWeaponFeint.Component(caster);if(component==null)return;
            var runtime=caster.Facts.List.SelectMany(f=>f.Components).FirstOrDefault(c=>ReferenceEquals(c.SourceBlueprintComponent,component));
            var data=runtime.GetData<ImprovedFeintData>();Pending.Remove(__instance.Context);
            Pending.Add(__instance.Context,new ImprovedFeintPending {Caster=caster,Target=target,Data=data,Epoch=data.Epoch,Expires=Game.Instance.TimeController.GameTime+TimeSpan.FromSeconds(6)});
        }
    }
    [HarmonyPatch(typeof(ContextActionCastSpell),nameof(ContextActionCastSpell.RunAction))]
    internal static class ImprovedFeintNativeCastPatch
    {
        [ThreadStatic] internal static UnitEntityData CurrentCaster;
        [ThreadStatic] internal static UnitEntityData CurrentTarget;
        internal struct Scope { internal bool Entered; internal UnitEntityData Caster,Target; }
        [HarmonyPrefix,HarmonyPriority(Priority.First)]
        static void Prefix(ContextActionCastSpell __instance,out Scope __state)
        {
            __state=new Scope {Entered=true,Caster=CurrentCaster,Target=CurrentTarget};
            CurrentCaster=null;CurrentTarget=null;
            // Fast bypass for other casts; only the native mode of an active
            // feat owner opens a scope. Never replace or skip the native action.
            var scope=ContextData<MechanicsContext.Data>.Current;var context=scope?.Context;
            if(context?.AssociatedBlueprint?.AssetGuid.ToString()!=ImprovedTwoWeaponFeint.NativeMode ||
                __instance.Spell?.AssetGuid.ToString()!=SpellAndSwordFeats.FeintAbility)return;
            var caster=context.MaybeCaster;var target=scope.CurrentTarget?.Unit;
            if(target==null || !ImprovedTwoWeaponFeint.Has(caster))return;
            var attack=Rulebook.CurrentContext.LastEvent<RuleAttackWithWeapon>();
            if(attack?.Initiator!=caster || attack.Target!=target || !attack.IsFullAttack || !attack.IsFirstAttack || attack.IsAttackOfOpportunity ||
                attack.Weapon?.Blueprint.IsMelee!=true || attack.AttackRoll?.IsFake==true)return;
            CurrentCaster=caster;CurrentTarget=target;
        }
        [HarmonyFinalizer] static void Finalizer(Scope __state) {if(__state.Entered){CurrentCaster=__state.Caster;CurrentTarget=__state.Target;}}
    }
    // Retain the published TypeId for existing serialized data. New registration
    // does not install this wrapper anywhere; old data delegates to native cast.
    [TypeId("f79604440f684c4e9d5c9908d50e4c1b")]
    public sealed class ImprovedFeintNativeCast:ContextAction
    {
        public ContextActionCastSpell Cast;
        public override string GetCaption()=>"Native Two-Weapon Feint";
        public override void RunAction()=>Cast?.RunAction();
    }
    [HarmonyPatch(typeof(ContextActionApplyBuff),nameof(ContextActionApplyBuff.RunAction))]
    internal static class ImprovedFeintAppliedPatch
    {
        [HarmonyPostfix] static void Postfix(ContextActionApplyBuff __instance)
        {
            if(__instance.Buff?.AssetGuid.ToString()!=ImprovedTwoWeaponFeint.Flag || __instance.ToCaster) return;
            var scope=ContextData<MechanicsContext.Data>.Current;var context=scope?.Context?.SourceAbilityContext;
            if(context==null || !ImprovedFeintCastPatch.Pending.TryGetValue(context,out var pending) || pending.Epoch!=pending.Data.Epoch || pending.Expires<=Game.Instance.TimeController.GameTime) return;
            var caster=pending.Caster;var target=pending.Target;if(scope.CurrentTarget?.Unit!=target)return;
            var buff=target.Buffs.Enumerable.FirstOrDefault(b=>b.Blueprint.AssetGuid.ToString()==ImprovedTwoWeaponFeint.Flag && b.Context?.MaybeCaster==caster);
            var component=ImprovedTwoWeaponFeint.Component(caster);if(component==null)return;
            var runtime=caster.Facts.List.SelectMany(f=>f.Components).FirstOrDefault(c=>ReferenceEquals(c.SourceBlueprintComponent,component));
            if(!ReferenceEquals(runtime.GetData<ImprovedFeintData>(),pending.Data))return;
            using(runtime.RequestEventContext())component.Mark(target,buff,pending.Expires);
        }
    }
    [HarmonyPatch(typeof(ContextActionRemoveBuff),nameof(ContextActionRemoveBuff.RunAction))]
    internal static class ImprovedFeintConsumePatch
    {
        [HarmonyPrefix] static bool Prefix(ContextActionRemoveBuff __instance)
        {
            var scope=ContextData<MechanicsContext.Data>.Current;var context=scope?.Context;
            if(__instance.ToCaster || !__instance.OnlyFromCaster || __instance.Buff?.AssetGuid.ToString()!=ImprovedTwoWeaponFeint.Flag ||
                context?.AssociatedBlueprint?.AssetGuid.ToString()!=ImprovedTwoWeaponFeint.Feint) return true;
            var caster=context.MaybeCaster;var component=ImprovedTwoWeaponFeint.Component(caster);if(component==null)return true;
            var runtime=caster.Facts.List.SelectMany(f=>f.Components).FirstOrDefault(c=>ReferenceEquals(c.SourceBlueprintComponent,component));
            using(runtime.RequestEventContext())return !component.Keep(scope.CurrentTarget?.Unit);
        }
    }
    [HarmonyPatch(typeof(TurnController),"End")]
    internal static class ImprovedFeintEndPatch
    {
        [HarmonyPostfix] static void Postfix(TurnController __instance) {End(__instance.Rider);if(__instance.Mount!=__instance.Rider)End(__instance.Mount);}
        internal static void End(UnitEntityData unit)
        {
            var c=ImprovedTwoWeaponFeint.Component(unit);if(c==null)return;
            var runtime=unit.Facts.List.SelectMany(f=>f.Components).FirstOrDefault(r=>ReferenceEquals(r.SourceBlueprintComponent,c));
            using(runtime.RequestEventContext())c.Clear();
        }
    }
}

using System;
using System.Linq;
using HarmonyLib;
using Kingmaker;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.EntitySystem;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.PubSubSystem;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.ActivatableAbilities;
using Kingmaker.UnitLogic.ActivatableAbilities.Restrictions;
using Kingmaker.UnitLogic.Buffs;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Buffs.Components;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Mechanics.Actions;
using Newtonsoft.Json;
using static CallOfTheWild.Wrath.Prototype.PsychicAssets;

namespace CallOfTheWild.Wrath.Prototype
{
    internal static class PhysicalMutationToggle01458
    {
        internal static readonly BlueprintGuid Main=Id("PhysicalMutationBuff01457"),Controller=Id("PhysicalMutationControllerBuff01458"),Toggle=Id("PhysicalMutationToggle01458");
        internal static Buff Effect(UnitEntityData owner)=>owner.Buffs.Enumerable.FirstOrDefault(b=>b.Blueprint.AssetGuid==Main&&b.IsActive&&b.IsTurnedOn&&!b.IsSuppressed&&b.Context?.MaybeCaster==owner);
        internal static bool Healthy(UnitEntityData owner)=>!owner.Descriptor.State.HasCondition(UnitCondition.Fatigued)&&!owner.Descriptor.State.HasCondition(UnitCondition.Exhausted);
        internal static BlueprintAbilityResource Resource=>ResourcesLibrary.TryGetBlueprint<BlueprintAbilityResource>(Id("PhysicalMutationResource01457"));
        internal static bool Maintaining(UnitEntityData owner,Buff controller)=>owner.ActivatableAbilities.Enumerable.Any(a=>a.Blueprint.AssetGuid==Toggle&&a.IsOn&&a.IsStarted&&ReferenceEquals(a.AppliedBuff,controller));
        internal static void Stop(UnitEntityData owner){var a=owner.Facts.List.OfType<ActivatableAbility>().FirstOrDefault(v=>v.Blueprint.AssetGuid==Toggle);a?.TurnOffImmediately();}
        internal static PhysicalMutationDuration01458 Duration(Buff effect,out IDisposable scope){scope=null;var runtime=effect?.Components.FirstOrDefault(v=>v.SourceBlueprintComponent is PhysicalMutationDuration01458);if(runtime==null)return null;scope=runtime.RequestEventContext();return (PhysicalMutationDuration01458)runtime.SourceBlueprintComponent;}
    }
    public sealed class PhysicalMutationDurationState01458{[JsonProperty]public EntityFactRef<Buff> Controller;}
    [TypeId("45ec5158b9324c20ac57a16f58000301")]
    public sealed class PhysicalMutationDuration01458:UnitBuffComponentDelegate<PhysicalMutationDurationState01458>
    {
        internal void Attach(Buff controller)=>Data.Controller=controller;
        internal void Detach(Buff controller){if(Data.Controller.Fact==controller)Data.Controller=default;}
        internal void Renew(TimeSpan now)
        {
            var controller=Data.Controller.Fact;
            // A native swift-command toggle can retain its controller until
            // the round ends after switching off. Its IsOn flag, not that
            // delayed buff removal, decides whether another minute is paid.
            if(controller==null||!controller.IsAttached||!controller.IsActive||!controller.IsTurnedOn||controller.IsSuppressed||!PhysicalMutationToggle01458.Maintaining(Owner,controller)||Buff.EndTime>now||TimeStopTimers01438.IsPaused(Buff))return;
            var resource=PhysicalMutationToggle01458.Resource;int available=Owner.Resources.GetResourceAmount(resource);
            if(available>0){long due=(now.Ticks-Buff.EndTime.Ticks)/TimeSpan.TicksPerMinute+1;int paid=(int)Math.Min((long)available,due);Owner.Resources.Spend(resource,paid);Buff.EndTime+=TimeSpan.FromTicks(TimeSpan.TicksPerMinute*paid);}
            if(Buff.EndTime<=now)PhysicalMutationToggle01458.Stop(Owner);
            Owner.Buffs.UpdateNextEvent();
        }
    }
    public sealed class PhysicalMutationControllerState01458{[JsonProperty]public EntityFactRef<Buff> Effect;}
    [TypeId("45ec5158b9324c20ac57a16f58000302")]
    public sealed class PhysicalMutationController01458:UnitBuffComponentDelegate<PhysicalMutationControllerState01458>,IUnitBuffHandler
    {
        protected override void OnTurnOn()
        {
            var now=Game.Instance.TimeController.GameTime;var effect=PhysicalMutationToggle01458.Effect(Owner);
            if(effect!=null&&effect.EndTime<=now&&!TimeStopTimers01438.IsPaused(effect)){effect.Remove();effect=null;}
            if(!PhysicalMutationToggle01458.Healthy(Owner)){PhysicalMutationToggle01458.Stop(Owner);return;}
            if(effect==null){var resource=PhysicalMutationToggle01458.Resource;if(!Owner.Resources.HasEnoughResource(resource,1)){PhysicalMutationToggle01458.Stop(Owner);return;}Owner.Resources.Spend(resource,1);effect=Owner.Descriptor.AddBuff(ResourcesLibrary.TryGetBlueprint<BlueprintBuff>(PhysicalMutationToggle01458.Main),Context,TimeSpan.FromMinutes(1));if(effect==null){Owner.Resources.Restore(resource,1);PhysicalMutationToggle01458.Stop(Owner);return;}effect.IsNotDispelable=true;}
            Data.Effect=effect;var duration=PhysicalMutationToggle01458.Duration(effect,out var scope);using(scope)duration?.Attach(Buff);
        }
        protected override void OnTurnOff(){var effect=Data.Effect.Fact;Data.Effect=default;var duration=PhysicalMutationToggle01458.Duration(effect,out var scope);using(scope)duration?.Detach(Buff);}
        public void HandleBuffDidAdded(Buff buff){}
        public void HandleBuffDidRemoved(Buff buff){if(buff!=null&&Data.Effect.FactId==buff.UniqueId){Data.Effect=default;PhysicalMutationToggle01458.Stop(Owner);}}
    }
    [TypeId("45ec5158b9324c20ac57a16f58000303")]
    public sealed class PhysicalMutationRestriction01458:ActivatableAbilityRestriction
    {
        protected override bool IsAvailable(){var owner=Fact.Owner;var effect=PhysicalMutationToggle01458.Effect(owner);return PhysicalMutationToggle01458.Healthy(owner)&&((effect!=null&&effect.TimeLeft>TimeSpan.Zero)||owner.Resources.HasEnoughResource(PhysicalMutationToggle01458.Resource,1));}
    }
    [TypeId("45ec5158b9324c20ac57a16f58000304")]
    public sealed class StartPhysicalMutation01458:ContextAction
    {
        public override string GetCaption()=>"Physical Mutation: forward the legacy shortcut to the persistent toggle";
        public override void RunAction(){var caster=Context.MaybeCaster;var toggle=caster?.Facts.List.OfType<ActivatableAbility>().FirstOrDefault(a=>a.Blueprint.AssetGuid==PhysicalMutationToggle01458.Toggle);if(toggle!=null)toggle.IsOn=!toggle.IsOn;}
    }
    // Native ResourceCount ignores Never-spend logic. Report this one toggle's
    // actual remaining minutes without attaching an extra native spender or
    // resource-availability restriction (paid time may be reopened at zero).
    [HarmonyPatch(typeof(ActivatableAbility),nameof(ActivatableAbility.ResourceCount),MethodType.Getter)]
    internal static class PhysicalMutationCount01459Patch
    {
        static void Postfix(ActivatableAbility __instance,ref int? __result){if(__instance.Blueprint.AssetGuid==PhysicalMutationToggle01458.Toggle&&PhysicalMutationToggle01458.Resource!=null)__result=__instance.Owner.Resources.GetResourceAmount(PhysicalMutationToggle01458.Resource);}
    }
    [HarmonyPatch(typeof(Kingmaker.UI.UnitSettings.MechanicActionBarSlotActivableAbility),nameof(Kingmaker.UI.UnitSettings.MechanicActionBarSlotActivableAbility.IsDisabled))]
    internal static class PhysicalMutationPaidSlot01459Patch
    {
        static void Prefix(Kingmaker.UI.UnitSettings.MechanicActionBarSlotActivableAbility __instance,ref int __0)
        {
            var ability=__instance.ActivatableAbility;
            if(__0!=0||ability?.Blueprint.AssetGuid!=PhysicalMutationToggle01458.Toggle)return;
            var effect=PhysicalMutationToggle01458.Effect(ability.Owner.Unit);
            // Keep the native UI's consciousness/action/turn checks. Only its
            // zero-resource check is inapplicable during already paid time.
            if(effect!=null&&effect.TimeLeft>TimeSpan.Zero&&PhysicalMutationToggle01458.Healthy(ability.Owner.Unit))__0=1;
        }
    }
    [HarmonyPatch(typeof(BuffCollection),nameof(BuffCollection.TickBuff))]
    internal static class PhysicalMutationRenew01458Patch
    {
        // Native scheduled buff processing, immediately before native expiry.
        // No movement hook, unit scan, frame loop, or remove/reapply on renewal.
        static void Prefix(Buff __0,TimeSpan __1){if(__0?.Blueprint.AssetGuid!=PhysicalMutationToggle01458.Main||TimeStopTimers01438.IsPaused(__0))return;var duration=PhysicalMutationToggle01458.Duration(__0,out var scope);using(scope)duration?.Renew(__1);}
    }
}

using System;
using System.Linq;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Kingmaker;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.Controllers.Units;
using Kingmaker.ElementsSystem;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.EntitySystem;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.PubSubSystem;
using Kingmaker.RuleSystem;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.RuleSystem.Rules.Abilities;
using Kingmaker.RuleSystem.Rules.Damage;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Components.Base;
using Kingmaker.UnitLogic.ActivatableAbilities;
using Kingmaker.UnitLogic.Buffs;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Buffs.Components;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Mechanics.Actions;
using Kingmaker.UnitLogic.FactLogic;
using Kingmaker.Utility;
using Newtonsoft.Json;

namespace CallOfTheWild.Wrath.Prototype
{
    [TypeId("45ec5158b9324c20ac57a16f58000110")]
    public sealed class OptionalDisciplineSpell01458:AddKnownSpell,ILevelUpCompleteUIHandler
    {
        bool Available()=>Spell!=null&&PsychicSpellIntegration.Available(ResourcesLibrary.BlueprintsCache,PsychicSharedSpellCatalog.Entries.Single(e=>e.Guid=="ff31ae1abe3c418db7842dcc76eca7ee"),out _);
        protected override void OnActivate(){if(Available())base.OnActivate();}
        public new void HandleLevelUpComplete(UnitEntityData unit,bool isChargen){if(Available())base.HandleLevelUpComplete(unit,isChargen);}
    }
    internal static class PsychicDisciplineRuntime01458
    {
        internal static BlueprintCharacterClass Class=>ResourcesLibrary.TryGetBlueprint<BlueprintCharacterClass>(BlueprintGuid.Parse(PsychicClass.ClassId));
        internal static int Level(UnitEntityData unit)=>unit.Progression.GetClassLevel(Class);
        internal static int DC(UnitEntityData unit)=>10+Level(unit)/2+unit.Stats.Intelligence.Bonus;
        internal static BlueprintAbilityResource Pool(UnitEntityData unit){var w=ResourcesLibrary.TryGetBlueprint<BlueprintAbilityResource>(BlueprintGuid.Parse(PsychicClass.WisdomPoolId));return unit.Resources.ContainsResource(w)?w:ResourcesLibrary.TryGetBlueprint<BlueprintAbilityResource>(BlueprintGuid.Parse(PsychicClass.CharismaPoolId));}
        internal static Buff OwnMarker(UnitEntityData target,BlueprintBuff blueprint,UnitEntityData caster)=>target?.Buffs.Enumerable.FirstOrDefault(b=>b.Blueprint==blueprint&&b.Context?.MaybeCaster==caster&&b.IsActive&&b.IsTurnedOn&&!b.IsSuppressed);
        internal static Buff Apply(UnitEntityData target,BlueprintBuff bp,MechanicsContext context,TimeSpan? duration){var b=target.Descriptor.AddBuff(bp,context,duration);if(b!=null)b.IsNotDispelable=true;return b;}
    }
    [TypeId("45ec5158b9324c20ac57a16f58000101")]
    public sealed class RestorePainfulReminder01458:ContextAction
    {
        public override string GetCaption()=>"Painful Reminder: restore one phrenic point after at least five actual damage";
        public override void RunAction(){var owner=Context.MaybeCaster;if(owner!=null&&Context[AbilitySharedValue.Damage]>=5)owner.Resources.Restore(PsychicDisciplineRuntime01458.Pool(owner),1);}
    }
    public enum PsychicDamageKind01458{Reminder,Agonizing,DarkHalf}
    public sealed class AgonizingTargetsState01458{[JsonProperty]public List<EntityFactRef<Buff>> Markers=new List<EntityFactRef<Buff>>();}
    [TypeId("45ec5158b9324c20ac57a16f58000111")]
    public sealed class AgonizingTargets01458:UnitFactComponentDelegate<AgonizingTargetsState01458>
    {
        public BlueprintBuffReference Marker;
        internal static AgonizingTargets01458 Find(UnitEntityData owner,out IDisposable scope){scope=null;var fact=owner?.Facts.List.FirstOrDefault(f=>f.Blueprint.AssetGuid==PsychicAssets.Id("AgonizingWoundFeature01458"));var runtime=fact?.Components.FirstOrDefault(c=>c.SourceBlueprintComponent is AgonizingTargets01458);if(runtime==null)return null;scope=runtime.RequestEventContext();return (AgonizingTargets01458)runtime.SourceBlueprintComponent;}
        internal void Mark(UnitEntityData target,MechanicsContext context){Data.Markers.RemoveAll(r=>r.Fact==null||!r.Fact.IsAttached);foreach(var b in target.Buffs.Enumerable.Where(b=>b.Blueprint==Marker.Get()&&b.Context?.MaybeCaster==Owner).ToArray())b.Remove();Data.Markers.RemoveAll(r=>r.Fact==null||!r.Fact.IsAttached);var added=PsychicDisciplineRuntime01458.Apply(target,Marker.Get(),context,TimeSpan.FromSeconds(6));if(added!=null)Data.Markers.Add(added);}
        internal void Clear(){var prior=Data.Markers.ToArray();Data.Markers.Clear();foreach(var reference in prior){var b=reference.Fact;if(b!=null&&b.IsAttached&&b.Blueprint==Marker.Get()&&b.Context?.MaybeCaster==Owner)b.Remove();}}
        internal static void Clear(UnitEntityData owner){var runtime=Find(owner,out var scope);using(scope)runtime?.Clear();}
        protected override void OnTurnOff()=>Clear();
    }
    [TypeId("45ec5158b9324c20ac57a16f58000112")]
    public sealed class SelectAgonizingTarget01458:ContextAction
    {
        public override string GetCaption()=>"Agonizing Wound: mark this caster's next spell-damage target";
        public override void RunAction(){var owner=Context.MaybeCaster;if(owner==null||Target?.Unit==null||!Target.Unit.IsEnemy(owner))return;var runtime=AgonizingTargets01458.Find(owner,out var scope);using(scope)runtime?.Mark(Target.Unit,Context);}
    }
    [TypeId("45ec5158b9324c20ac57a16f58000102")]
    public sealed class PsychicDamageLink01458:UnitFactComponentDelegate,IInitiatorRulebookHandler<RuleDealDamage>,IInitiatorRulebookSubscriber
    {
        public PsychicDamageKind01458 Kind;
        public BlueprintBuffReference Marker,Effect;
        public BlueprintAbilityResourceReference Resource;
        public int Cost;
        static readonly ConditionalWeakTable<RuleDealDamage,HashSet<PsychicDamageLink01458>> Processed=new ConditionalWeakTable<RuleDealDamage,HashSet<PsychicDamageLink01458>>();
        protected override void OnTurnOn()
        {
            if(Kind!=PsychicDamageKind01458.Agonizing)return;
            foreach(var toggle in Owner.ActivatableAbilities.Enumerable.Where(a=>a.Blueprint.Buff!=Fact.Blueprint&&a.Blueprint.Buff?.GetComponents<PsychicDamageLink01458>().Any(c=>c.Kind==PsychicDamageKind01458.Agonizing)==true).ToArray())toggle.TurnOffImmediately();
            foreach(var b in Owner.Buffs.Enumerable.Where(b=>b!=Fact&&b.Blueprint.GetComponents<PsychicDamageLink01458>().Any(c=>c.Kind==PsychicDamageKind01458.Agonizing)).ToArray())b.Remove();
        }
        protected override void OnTurnOff(){if(Kind==PsychicDamageKind01458.Agonizing)AgonizingTargets01458.Clear(Owner);}
        public void OnEventAboutToTrigger(RuleDealDamage e){}
        public void OnEventDidTrigger(RuleDealDamage e)
        {
            if(e.Initiator!=Owner||e.Result<=0||e.Target==null||e.SourceAbility?.IsSpell!=true||e.Reason?.Context?.SourceAbilityContext?.IsDuplicateSpellApplied==true||!Processed.GetOrCreateValue(e).Add(this))return;
            if(Kind==PsychicDamageKind01458.Reminder){foreach(var b in e.Target.Buffs.Enumerable.Where(b=>b.Blueprint==Marker.Get()&&b.Context?.MaybeCaster==Owner).ToArray())b.Remove();PsychicDisciplineRuntime01458.Apply(e.Target,Marker.Get(),Context,TimeSpan.FromSeconds(12));return;}
            if(Kind==PsychicDamageKind01458.DarkHalf){PsychicDisciplineRuntime01458.Apply(e.Target,Effect.Get(),Context,null);return;}
            var marked=PsychicDisciplineRuntime01458.OwnMarker(e.Target,Marker.Get(),Owner);var resource=Resource.Get();
            if(marked==null||!Owner.Resources.HasEnoughResource(resource,Cost))return;
            var save=Context.TriggerRule(new RuleSavingThrow(e.Target,SavingThrowType.Will,PsychicDisciplineRuntime01458.DC(Owner)){Reason=Context});
            if(save.IsPassed)return;
            PsychicDisciplineRuntime01458.Apply(e.Target,Effect.Get(),Context,TimeSpan.FromSeconds(6));Owner.Resources.Spend(resource,Cost);marked.Remove();
        }
    }
    [TypeId("45ec5158b9324c20ac57a16f58000103")]
    public sealed class LiveOnCasterLevel01458:UnitFactComponentDelegate,IInitiatorRulebookHandler<RuleCalculateAbilityParams>,IInitiatorRulebookSubscriber
    {
        public void OnEventAboutToTrigger(RuleCalculateAbilityParams e){if(e.AbilityData?.Blueprint==OwnerBlueprint)e.ReplaceCasterLevel=Math.Max(0,PsychicDisciplineRuntime01458.Level(Owner)-3);}
        public void OnEventDidTrigger(RuleCalculateAbilityParams e){}
    }
    [TypeId("45ec5158b9324c20ac57a16f58000105")]
    public sealed class WarpedBrain01458:UnitFactComponentDelegate,IApplyAbilityEffectHandler,IGlobalSubscriber
    {
        public BlueprintBuffReference Effect;
        static readonly ConditionalWeakTable<AbilityExecutionContext,HashSet<UnitEntityData>> Attempts=new ConditionalWeakTable<AbilityExecutionContext,HashSet<UnitEntityData>>();
        public void OnTryToApplyAbilityEffect(AbilityExecutionContext context,TargetWrapper target)
        {
            var caster=context?.MaybeCaster;
            if(target?.Unit!=Owner||caster==null||PsychicWilling01460.Willing(context,Owner)||(context.SpellDescriptor&SpellDescriptor.MindAffecting)==0||!Attempts.GetOrCreateValue(context).Add(Owner))return;
            var save=Context.TriggerRule(new RuleSavingThrow(caster,SavingThrowType.Will,PsychicDisciplineRuntime01458.DC(Owner)){Reason=Context});
            if(!save.IsPassed)PsychicDisciplineRuntime01458.Apply(caster,Effect.Get(),Context,TimeSpan.FromSeconds(6));
        }
        public void OnAbilityEffectApplied(AbilityExecutionContext context){}
        public void OnAbilityEffectAppliedToTarget(AbilityExecutionContext context,TargetWrapper target){}
    }
    [TypeId("45ec5158b9324c20ac57a16f58000106")]
    public sealed class HallucinogenicAura01458:ContextAction
    {
        public BlueprintBuffReference Cooldown,Confusion;
        public override string GetCaption()=>"Hallucinogenic Aura: enemy Will save, source-specific confusion and24h immunity";
        public override void RunAction()
        {
            var caster=Context.MaybeCaster;var target=Target?.Unit;
            if(caster==null||target==null||!target.IsEnemy(caster)||PsychicDisciplineRuntime01458.OwnMarker(target,Cooldown.Get(),caster)!=null||PsychicDisciplineRuntime01458.OwnMarker(target,Confusion.Get(),caster)!=null)return;
            var save=Context.TriggerRule(new RuleSavingThrow(target,SavingThrowType.Will,PsychicDisciplineRuntime01458.DC(caster)){Reason=Context});
            if(save.IsPassed){PsychicDisciplineRuntime01458.Apply(target,Cooldown.Get(),Context,TimeSpan.FromDays(1));return;}
            int rounds=Context.TriggerRule(new RuleRollDice(caster,new DiceFormula(1,DiceType.D4))).Result;
            if(PsychicDisciplineRuntime01458.Apply(target,Confusion.Get(),Context,TimeSpan.FromSeconds(6*rounds))==null)PsychicDisciplineRuntime01458.Apply(target,Cooldown.Get(),Context,TimeSpan.FromDays(1));
        }
    }
    public sealed class DarkHalfState01458
    {
        [JsonProperty]public bool Returning;
        [JsonProperty]public TimeSpan AttemptAfter;
    }
    [TypeId("45ec5158b9324c20ac57a16f58000107")]
    public sealed class DarkHalf01458:UnitBuffComponentDelegate<DarkHalfState01458>,ITickEachRound,IInitiatorRulebookHandler<RuleCalculateAbilityParams>,IInitiatorRulebookSubscriber
    {
        public BlueprintAbilityResourceReference Resource;
        public BlueprintFeatureReference Morphic,Safeguard;
        public BlueprintBuffReference[] Forms;
        public BlueprintBuffReference Protection;
        internal bool CanAttempt=>Game.Instance.TimeController.GameTime>=Data.AttemptAfter;
        protected override void OnTurnOn()
        {
            foreach(var b in Owner.Buffs.Enumerable.Where(b=>b!=Buff&&((b.Context?.SpellDescriptor??SpellDescriptor.None)&(SpellDescriptor.Fear|SpellDescriptor.Shaken))!=0).ToArray())b.Remove();
            if(Owner.HasFact(Morphic.Get()))Child(Forms[Context.TriggerRule(new RuleRollDice(Owner,new DiceFormula(1,DiceType.D3))).Result-1].Get());
            if(Owner.HasFact(Safeguard.Get()))Child(Protection.Get());
        }
        void Child(BlueprintBuff bp){var child=PsychicDisciplineRuntime01458.Apply(Owner,bp,Context,null);if(child!=null)Buff.StoreFact(child);}
        internal void Attempt()
        {
            if(!CanAttempt)return;Data.Returning=true;Data.AttemptAfter=Game.Instance.TimeController.GameTime+TimeSpan.FromSeconds(6);
            var roll=Context.TriggerRule(new RuleSkillCheck(Owner,StatType.Intelligence,10){Reason=Context});if(roll.Success)Buff.Remove();
        }
        public void OnNewRound()
        {
            var resource=Resource.Get();if(!Owner.Resources.HasEnoughResource(resource,1)){Buff.Remove();return;}Owner.Resources.Spend(resource,1);if(Data.Returning)Attempt();
        }
        public void OnEventAboutToTrigger(RuleCalculateAbilityParams e){if(PsychicCasting.IsPsychic(e.AbilityData)&&e.AbilityData.Spellbook.Blueprint.AssetGuid==BlueprintGuid.Parse(PsychicClass.BookId))e.AddBonusDC(1);}
        public void OnEventDidTrigger(RuleCalculateAbilityParams e){}
    }
    [TypeId("45ec5158b9324c20ac57a16f58000108")]
    public sealed class DarkHalfReturn01458:ContextAction
    {
        public override string GetCaption()=>"Dark Half: Intelligence DC10 return attempt";
        public override void RunAction(){var caster=Context.MaybeCaster;var bp=ResourcesLibrary.TryGetBlueprint<BlueprintBuff>(PsychicAssets.Id("DarkHalfBuff01458"));var b=PsychicDisciplineRuntime01458.OwnMarker(caster,bp,caster);var runtime=b?.Components.FirstOrDefault(c=>c.SourceBlueprintComponent is DarkHalf01458);if(runtime!=null)using(runtime.RequestEventContext())((DarkHalf01458)runtime.SourceBlueprintComponent).Attempt();}
    }
    [TypeId("45ec5158b9324c20ac57a16f58000109")]
    public sealed class DarkHalfCaster01458:BlueprintComponent,IAbilityCasterRestriction
    {
        public BlueprintBuffReference Active;public bool Returning;
        public bool IsCasterRestrictionPassed(UnitEntityData caster){var b=caster.Buffs.Enumerable.FirstOrDefault(v=>v.Blueprint==Active.Get()&&v.IsActive&&v.IsTurnedOn&&!v.IsSuppressed);if(!Returning)return b==null;if(b==null)return false;var component=b.Components.FirstOrDefault(v=>v.SourceBlueprintComponent is DarkHalf01458);if(component==null)return false;using(component.RequestEventContext())return ((DarkHalf01458)component.SourceBlueprintComponent).CanAttempt;}
        public string GetAbilityCasterRestrictionUIText()=>Returning?"Dark Half is not active or a return attempt was already made this round.":"Dark Half is already active.";
    }
}

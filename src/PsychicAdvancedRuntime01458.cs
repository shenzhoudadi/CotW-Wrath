using System;
using System.Linq;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using HarmonyLib;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.Designers.Mechanics.Facts;
using Kingmaker.EntitySystem;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.PubSubSystem;
using Kingmaker.RuleSystem;
using Kingmaker.RuleSystem.Rules.Abilities;
using Kingmaker.RuleSystem.Rules.Damage;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components;
using Kingmaker.UnitLogic.Abilities.Components.Base;
using Kingmaker.UnitLogic.ActivatableAbilities;
using Kingmaker.UnitLogic.Buffs;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Mechanics.Actions;
using Kingmaker.Utility;
using Newtonsoft.Json;
using static CallOfTheWild.Wrath.Prototype.PsychicAssets;

namespace CallOfTheWild.Wrath.Prototype
{
    internal static class PsychicAdvancedRuntime01458
    {
        static readonly BlueprintGuid DualId=Id("DualAmplificationFeature01458");
        internal static bool HasDual(UnitEntityData owner)=>owner?.Facts?.List?.Any(f=>f.Blueprint?.AssetGuid==DualId)==true;
        internal static string Key(BlueprintBuff b){var c=b.GetComponent<PsychicAmplificationComponent>();return c!=null?c.Kind.ToString():b.GetComponent<MimicMetamagic01458>()!=null?"Mimic":b.GetComponent<NaturesCommand01447>()!=null?"Nature":null;}
        internal static bool CoordinateIfNeeded(UnitEntityData owner,EntityFact current)
        {
            if(!HasDual(owner)&&!owner.Buffs.Enumerable.Any(b=>b.Blueprint.GetComponent<MimicMetamagic01458>()!=null||b.Blueprint.GetComponent<PsychicAmplificationComponent>()?.Kind>=PsychicAmplificationKind.WillOfTheDead))return false;
            Coordinate(owner,current);return true;
        }
        internal static void Coordinate(UnitEntityData owner,EntityFact current=null)
        {
            var keep=new List<Buff>();var keys=new HashSet<string>();int limit=HasDual(owner)?2:1;
            var all=owner.Buffs.Enumerable.Where(b=>Key(b.Blueprint)!=null).ToArray();
            foreach(var b in all.OrderByDescending(b=>ReferenceEquals(b,current)).ThenByDescending(b=>Array.IndexOf(all,b))){string key=Key(b.Blueprint);if(keep.Count<limit&&keys.Add(key))keep.Add(b);}
            foreach(var ability in owner.Facts.List.OfType<ActivatableAbility>().Where(a=>a.Blueprint.Buff!=null&&Key(a.Blueprint.Buff)!=null&&!keep.Any(b=>b.Blueprint==a.Blueprint.Buff)).ToArray())ability.TurnOffImmediately();
            foreach(var b in all.Where(b=>!keep.Contains(b)&&b.IsAttached).ToArray())b.Remove();
        }
        internal static bool Real(AbilityData a)=>a!=null&&a.SourceItem==null&&a.StickyTouch==null&&PsychicCasting.IsPsychic(a)&&a.Spellbook.Blueprint.AssetGuid==BlueprintGuid.Parse(PsychicClass.BookId)&&a.SpellLevel>0;
        internal static bool AreaSpell(BlueprintAbility bp)=>bp.CanTargetPoint||bp.GetComponent<AbilityTargetsAround>()!=null||PsychicRemainingDisciplines01458.Graph(bp.ComponentsArray).OfType<ContextActionSpawnAreaEffect>().Any();
        internal static bool Eligible(PsychicAmplificationComponent c,RuleCastSpell e)
        {
            switch(c.Kind){
                case PsychicAmplificationKind.WillOfTheDead:return (e.Context.SpellDescriptor&SpellDescriptor.MindAffecting)!=0;
                case PsychicAmplificationKind.UndercastSurge:return SurgeShape(e.Spell,out int difference)&&difference==c.Strength;
                case PsychicAmplificationKind.SynapticShock:return (e.Context.SpellDescriptor&SpellDescriptor.MindAffecting)!=0&&AreaSpell(e.Spell.Blueprint)&&SynapticShock01458.Selected(e.Initiator)!=null;
                case PsychicAmplificationKind.SpaceRendingSpell:return true;
                default:return false;
            }
        }
        static bool Candidate(PsychicAmplificationComponent c,AbilityData a)
        {
            if(!PsychicMajor01460.Allows(a.Caster.Unit,c))return false;
            var bp=a.Blueprint;var tags=a.SpellDescriptor;
            switch(c.Kind){
                case PsychicAmplificationKind.BiokineticHealing:return bp.School==SpellSchool.Transmutation;
                case PsychicAmplificationKind.DefensivePrognostication:return bp.School==SpellSchool.Divination;
                case PsychicAmplificationKind.OverpoweringMind:case PsychicAmplificationKind.WillOfTheDead:return (tags&SpellDescriptor.MindAffecting)!=0;
                case PsychicAmplificationKind.RelentlessCasting:return bp.SpellResistance;
                case PsychicAmplificationKind.OngoingDefense:return PsychicAmplificationRuntime.IsDefense(bp);
                case PsychicAmplificationKind.FocusedForce:return PsychicLinkedSpellEffects.HasForceDice(bp);
                case PsychicAmplificationKind.MindsEye:return bp.GetComponents<AbilityDeliverProjectile>().Any(p=>p.NeedAttackRoll&&(p.Weapon==null||p.Weapon.IsRanged));
                case PsychicAmplificationKind.ConjuredArmor:return bp.School==SpellSchool.Conjuration&&(tags&SpellDescriptor.Summoning)!=0;
                case PsychicAmplificationKind.UndercastSurge:return SurgeShape(a,out int d)&&d==c.Strength;
                case PsychicAmplificationKind.SynapticShock:return (tags&SpellDescriptor.MindAffecting)!=0&&AreaSpell(bp)&&SynapticShock01458.Selected(a.Caster.Unit)!=null;
                case PsychicAmplificationKind.SpaceRendingSpell:return true;
                default:return false;
            }
        }
        sealed class CastPlan{internal HashSet<string> Allowed,Claimed=new HashSet<string>();}
        static readonly ConditionalWeakTable<RuleCastSpell,CastPlan> Plans=new ConditionalWeakTable<RuleCastSpell,CastPlan>();
        static HashSet<string> Plan(AbilityData a)
        {
            var result=new HashSet<string>();if(!Real(a))return result;
            var owner=a.Caster.Unit;var pool=PsychicDisciplineRuntime01458.Pool(owner);int points=pool==null?0:owner.Resources.GetResourceAmount(pool);int limit=HasDual(owner)?2:1;
            foreach(var b in owner.Buffs.Enumerable.Where(b=>b.IsActive&&b.IsTurnedOn&&!b.IsSuppressed&&Key(b.Blueprint)!=null).Reverse()){
                var c=b.Blueprint.GetComponent<PsychicAmplificationComponent>();var mimic=b.Blueprint.GetComponent<MimicMetamagic01458>();int cost;
                if(c!=null){if(!Candidate(c,a))continue;cost=c.Cost;}
                else if(mimic!=null){if(!MimicMetamagic01458.RawEligible(mimic,a))continue;cost=mimic.Price;}
                else{if((a.SpellDescriptor&SpellDescriptor.MindAffecting)==0)continue;cost=2;}
                string key=Key(b.Blueprint);if(result.Count>=limit||result.Contains(key)||points<cost)continue;points-=cost;result.Add(key);
            }return result;
        }
        internal static bool Allowed(AbilityData a,string key)
        {
            var current=NatureTargeting01448.CurrentCast;
            return current!=null&&ReferenceEquals(current.Spell,a)&&Plans.TryGetValue(current,out var plan)?plan.Allowed.Contains(key):Plan(a).Contains(key);
        }
        internal static bool Claim(RuleCastSpell e,string key)
        {
            if(!Plans.TryGetValue(e,out var plan)){
                var owner=e.Initiator;
                // Preserve the original first-callback reservation when only
                // legacy modes exist. Planning is needed for the new shared
                // budget only after Dual or a new mode actually participates.
                bool advanced=HasDual(owner)||owner?.Buffs?.Enumerable?.Any(b=>b.Blueprint.GetComponent<MimicMetamagic01458>()!=null||b.Blueprint.GetComponent<PsychicAmplificationComponent>()?.Kind>=PsychicAmplificationKind.WillOfTheDead)==true;
                plan=new CastPlan{Allowed=advanced?Plan(e.Spell):new HashSet<string>{key}};Plans.Add(e,plan);
            }
            return plan.Allowed.Contains(key)&&plan.Claimed.Add(key);
        }
        internal static bool SurgeShape(AbilityData a,out int difference)
        {
            difference=0;var low=a?.ConvertedFrom;
            if(!Real(a)||low==null||low.SourceItem!=null||low.ConvertedFrom!=null||!ReferenceEquals(a.Spellbook,low.Spellbook)||a.MetamagicData?.NotEmpty==true||low.MetamagicData?.NotEmpty==true)return false;
            var family=a.Blueprint.GetComponent<PsychicUndercastFamily>();var lower=low.Blueprint.GetComponent<PsychicUndercastFamily>();
            if(family==null||lower==null||family.LowerSpells?.Any(r=>r.Guid==low.Blueprint.AssetGuid)!=true||a.OverrideSpellLevel!=family.Circle||a.SpellLevelInSpellbook!=lower.Circle)return false;
            difference=family.Circle-lower.Circle;return difference>=1&&difference<=5;
        }
        internal static bool SurgeValid(AbilityData a,MechanicsContext context=null)
        {
            var high=a?.Blueprint.GetComponent<PsychicUndercastFamily>();var low=a?.ConvertedFrom?.Blueprint.GetComponent<PsychicUndercastFamily>();
            bool conversion=high!=null&&low!=null&&high.Circle>low.Circle&&high.LowerSpells?.Any(r=>r.Guid==a.ConvertedFrom.Blueprint.AssetGuid)==true;
            bool reducedOwnFamily=Real(a)&&high!=null&&a.SpellLevelInSpellbook>0&&a.SpellLevelInSpellbook<high.Circle;
            if(!conversion&&!reducedOwnFamily)return true;
            if(!SurgeShape(a,out int difference))return false;
            var owner=a.Caster.Unit;var pool=PsychicDisciplineRuntime01458.Pool(owner);
            if(!PsychicSurge01460.KnownVersion(a)||!owner.Buffs.Enumerable.Any(b=>b.IsActive&&b.IsTurnedOn&&!b.IsSuppressed&&b.Blueprint.GetComponents<PsychicAmplificationComponent>().Any(c=>c.Kind==PsychicAmplificationKind.UndercastSurge&&c.Strength==difference)))return false;
            context=context??(ReferenceEquals(NatureTargeting01448.CurrentCast?.Spell,a)?NatureTargeting01448.CurrentCast.Context:null);
            if(ReferenceEquals(context?.SourceAbilityContext?.Ability,a)&&PsychicAmplificationRuntime.TryReservation(context,PsychicAmplificationKind.UndercastSurge,out _))return true;
            return pool!=null&&owner.Resources.HasEnoughResource(pool,2*difference)&&Allowed(a,PsychicAmplificationKind.UndercastSurge.ToString());
        }
        internal static IEnumerable<AbilityData> SurgeConversions(AbilityData low)
        {
            if(!Real(low)||low.ConvertedFrom!=null||low.MetamagicData?.NotEmpty==true||low.Blueprint.GetComponent<PsychicUndercastFamily>()==null)yield break;
            var owner=low.Caster.Unit;var mode=owner.Buffs.Enumerable.Where(b=>b.IsActive&&b.IsTurnedOn&&!b.IsSuppressed).Select(b=>b.Blueprint.GetComponent<PsychicAmplificationComponent>()).FirstOrDefault(c=>c?.Kind==PsychicAmplificationKind.UndercastSurge);if(mode==null)yield break;
            foreach(var high in low.Spellbook.GetAllKnownSpells().Where(a=>a.MetamagicData?.NotEmpty!=true&&a.Blueprint.GetComponent<PsychicUndercastFamily>()?.LowerSpells?.Any(r=>r.Guid==low.Blueprint.AssetGuid)==true).GroupBy(a=>a.Blueprint.AssetGuid).Select(g=>g.First())){
                int circle=high.Blueprint.GetComponent<PsychicUndercastFamily>().Circle;if(circle-low.SpellLevel!=mode.Strength)continue;
                var converted=new AbilityData(high.Blueprint,low.Spellbook,low.SpellLevel){ConvertedFrom=low,OverrideSpellLevel=circle};if(SurgeValid(converted))yield return converted;
            }
        }
        internal static int SpaceLimit(UnitEntityData owner)=>owner.Buffs.Enumerable.Where(b=>b.IsActive&&b.IsTurnedOn&&!b.IsSuppressed).Select(b=>Enumerable.Range(1,9).FirstOrDefault(i=>b.Blueprint.AssetGuid==Id("SpaceRendingPermit"+i+"01458"))).DefaultIfEmpty(0).Max();
        internal static void EndSpace(UnitEntityData owner){var ids=new HashSet<BlueprintGuid>(Enumerable.Range(1,9).Select(i=>Id("SpaceRendingPermit"+i+"01458")));foreach(var b in owner.Buffs.Enumerable.Where(b=>ids.Contains(b.Blueprint.AssetGuid)).ToArray())b.Remove();}
        internal static void PermitSpace(RuleCastSpell e){EndSpace(e.Initiator);int level=Math.Min(9,e.Context.Params.SpellLevel);if(level<1)return;var b=ResourcesLibrary.TryGetBlueprint<BlueprintBuff>(Id("SpaceRendingPermit"+level+"01458"));PsychicDisciplineRuntime01458.Apply(e.Initiator,b,e.Context,TimeSpan.FromSeconds(6));}
    }
    [TypeId("45ec5158b9324c20ac57a16f58000201")]
    public sealed class DualAmplification01458:UnitFactComponentDelegate
    {protected override void OnTurnOff()=>PsychicAdvancedRuntime01458.Coordinate(Owner);}
    [TypeId("45ec5158b9324c20ac57a16f58000202")]
    public sealed class MimicMetamagic01458:AutoMetamagic,IInitiatorRulebookHandler<RuleCalculateAbilityParams>,IInitiatorRulebookHandler<RuleCastSpell>
    {
        public int Price,LevelCost;
        sealed class Charge{internal BlueprintAbilityResource Pool;internal bool Done;}
        static readonly ConditionalWeakTable<RuleCastSpell,Charge> Paid=new ConditionalWeakTable<RuleCastSpell,Charge>();
        sealed class Mask{internal Metamagic Value;}
        static readonly ConditionalWeakTable<AbilityExecutionContext,Mask> Contexts=new ConditionalWeakTable<AbilityExecutionContext,Mask>();
        sealed class ReferenceAbility:IEqualityComparer<AbilityData>{internal static readonly ReferenceAbility Instance=new ReferenceAbility();public bool Equals(AbilityData a,AbilityData b)=>ReferenceEquals(a,b);public int GetHashCode(AbilityData a)=>RuntimeHelpers.GetHashCode(a);}
        [ThreadStatic]static HashSet<AbilityData> Checking;
        [ThreadStatic]static HashSet<AbilityData> Queries;
        static readonly HashSet<string> EligibilityFailures=new HashSet<string>();
        internal static void Restore(MechanicsContext context){var root=context?.SourceAbilityContext;if(root==null||context.Params==null||!Contexts.TryGetValue(root,out var mask)||(context.Params.Metamagic&mask.Value)==mask.Value)return;var copy=context.Params.Clone();copy.Metamagic|=mask.Value;context.SetParams(copy);}
        internal static bool RawEligible(MimicMetamagic01458 c,AbilityData a){
            if(a==null)return false;if(Checking==null)Checking=new HashSet<AbilityData>(ReferenceAbility.Instance);if(!Checking.Add(a))return false;
            try{return PsychicAdvancedRuntime01458.Real(a)&&PsychicMajor01460.Has(a.Caster.Unit)&&(c.Metamagic==PsychicCasting.Logical||(c.Metamagic==TopplingSpell.Metamagic?TopplingSpell.Force(a.Blueprint):(a.Blueprint.AvailableMetamagic&c.Metamagic)!=0))&&a.MetamagicData?.Has(c.Metamagic)!=true&&a.SpellLevel+c.LevelCost<=a.Spellbook.MaxSpellLevel;}
            catch(Exception error){string key=a.Blueprint?.AssetGuid+"/"+c.Metamagic+"/"+error.GetType().FullName;lock(EligibilityFailures)if(EligibilityFailures.Add(key))ModEntry.LogSpellDiagnostic("Mimic eligibility failed for "+key+"; ordinary spell restrictions and casting remain in effect. "+error);return false;}
            finally{Checking.Remove(a);}
        }
        internal static Metamagic Preview(AbilityData a){
            if(a==null)return 0;if(Queries==null)Queries=new HashSet<AbilityData>(ReferenceAbility.Instance);if(!Queries.Add(a))return 0;
            try{
                // Do not register this optional amplifier in the native global
                // AutoMetamagic list. That list is queried even for ordinary
                // abilities during CanBeCastByCaster, outside fact context.
                // Bound both eligibility and budget lookup by ability identity.
                if(!PsychicAdvancedRuntime01458.Real(a))return 0;
                foreach(var b in a.Caster.Buffs.Enumerable.Where(b=>b.IsActive&&b.IsTurnedOn&&!b.IsSuppressed).Reverse().ToArray()){
                    var mimic=b.Blueprint.GetComponent<MimicMetamagic01458>();
                    if(mimic!=null&&RawEligible(mimic,a)&&PsychicAdvancedRuntime01458.Allowed(a,"Mimic"))return mimic.Metamagic;
                }return 0;
            }
            catch(Exception error){string key=a.Blueprint?.AssetGuid+"/preview/"+error.GetType().FullName;lock(EligibilityFailures)if(EligibilityFailures.Add(key))ModEntry.LogSpellDiagnostic("Mimic preview failed for "+key+"; ordinary casting remains subject to its native rules. "+error);return 0;}
            finally{Queries.Remove(a);}
        }
        internal static bool Applies(MimicMetamagic01458 c,AbilityData a)=>c!=null&&Preview(a)==c.Metamagic;
        // Preserve the serialized base/TypeId, but remove only this fact's old
        // registry entry on restore/reapply and never add it back. Other native
        // automatic metamagic and rods remain owned by the native part.
        protected override void OnTurnOn(){Owner.Get<Kingmaker.UnitLogic.Parts.UnitPartSpecialMetamagic>()?.Remove(Fact,this);PsychicAdvancedRuntime01458.Coordinate(Owner,Fact);}
        protected override void OnTurnOff()=>Owner.Get<Kingmaker.UnitLogic.Parts.UnitPartSpecialMetamagic>()?.Remove(Fact,this);
        public new void OnEventAboutToTrigger(RuleCalculateAbilityParams e){if(Applies(this,e.AbilityData))e.AddMetamagic(Metamagic);}
        public new void OnEventDidTrigger(RuleCalculateAbilityParams e){}
        public new void OnEventAboutToTrigger(RuleCastSpell e){if(Paid.TryGetValue(e,out _)||e.ForceFail||e.IsDuplicateSpellApplied||e.Context?.IsDuplicateSpellApplied==true||!RawEligible(this,e.Spell)||!PsychicAdvancedRuntime01458.Claim(e,"Mimic"))return;var pool=PsychicDisciplineRuntime01458.Pool(Owner);if(pool==null||!Owner.Resources.HasEnoughResource(pool,Price))return;Owner.Resources.Spend(pool,Price);Paid.Add(e,new Charge{Pool=pool});Contexts.GetOrCreateValue(e.Context).Value|=Metamagic;Restore(e.Context);}
        public new void OnEventDidTrigger(RuleCastSpell e){if(!Paid.TryGetValue(e,out var c)||c.Done)return;c.Done=true;if(!e.Success){Owner.Resources.Restore(c.Pool,Price);Contexts.Remove(e.Context);}}
    }
    public sealed class SynapticState01458{[JsonProperty]public EntityRef<UnitEntityData> Target;}
    [TypeId("45ec5158b9324c20ac57a16f58000203")]
    public sealed class SynapticShock01458:UnitFactComponentDelegate<SynapticState01458>,IUnitBuffHandler,IInitiatorRulebookHandler<RuleDealDamage>,IInitiatorRulebookSubscriber,IGlobalRulebookHandler<Kingmaker.RuleSystem.Rules.RuleSavingThrow>,IGlobalSubscriber
    {
        public BlueprintBuffReference Marker;
        static readonly ConditionalWeakTable<MechanicsContext,object> Used=new ConditionalWeakTable<MechanicsContext,object>();
        internal static SynapticShock01458 FindRuntime(UnitEntityData u,out IDisposable scope){scope=null;var feat=u?.Facts.List.FirstOrDefault(f=>f.Blueprint.AssetGuid==Id("SynapticShockFeature01458"));var runtime=feat?.Components.FirstOrDefault(c=>c.SourceBlueprintComponent is SynapticShock01458);if(runtime==null)return null;scope=runtime.RequestEventContext();return (SynapticShock01458)runtime.SourceBlueprintComponent;}
        internal static UnitEntityData Selected(UnitEntityData u){var component=FindRuntime(u,out var scope);using(scope)return component?.Data.Target.Entity;}
        internal void Select(UnitEntityData target){Clear();Data.Target=target;PsychicDisciplineRuntime01458.Apply(target,Marker.Get(),Context,null);}
        void Clear(){var target=Data.Target.Entity;Data.Target=default;if(target!=null)foreach(var b in target.Buffs.Enumerable.Where(b=>b.Blueprint==Marker.Get()&&b.Context?.MaybeCaster==Owner).ToArray())b.Remove();}
        protected override void OnTurnOff()=>Clear();
        void Affect(UnitEntityData target,MechanicsContext context,Buff followed=null)
        {
            var root=context?.SourceAbilityContext;if(root==null||target!=Data.Target.Entity||root.MaybeCaster!=Owner||Used.TryGetValue(root,out _)||!PsychicAmplificationRuntime.TryReservation(context,PsychicAmplificationKind.SynapticShock,out _))return;
            // The cast reservation has already paid its point. Delivery can
            // occur later, including when that was the caster's last point.
            Used.Add(root,new object());
            var confusion=ResourcesLibrary.TryGetBlueprint<BlueprintBuff>(Id("SynapticShockConfusion01458"));
            bool lasting=followed!=null&&(followed.IsPermanent||followed.TimeLeft>TimeSpan.Zero);
            var applied=PsychicDisciplineRuntime01458.Apply(target,lasting?ResourcesLibrary.TryGetBlueprint<BlueprintBuff>(Id("SynapticShockPending01460")):confusion,root,lasting?(TimeSpan?)null:TimeSpan.FromSeconds(6));
            if(applied==null){Used.Remove(root);return;}
            if(lasting){var runtime=applied.Components.First(c=>c.SourceBlueprintComponent is SynapticDelay01460);using(runtime.RequestEventContext())((SynapticDelay01460)runtime.SourceBlueprintComponent).Follow(followed);}
            Clear();
        }
        public void HandleBuffDidAdded(Buff b){if(b.Blueprint!=Marker.Get()&&b.Blueprint.AssetGuid!=Id("SynapticShockPending01460")&&b.Blueprint.AssetGuid!=Id("SynapticShockConfusion01458"))Affect(b.Owner.Unit,b.Context,b);}
        public void HandleBuffDidRemoved(Buff b){if(b.Owner.Unit==Owner&&b.Blueprint.GetComponent<PsychicAmplificationComponent>()?.Kind==PsychicAmplificationKind.SynapticShock)Clear();}
        public void OnEventAboutToTrigger(RuleDealDamage e){}
        public void OnEventDidTrigger(RuleDealDamage e){if(e.Result>0)Affect(e.Target,e.Reason?.Context);}
        public void OnEventAboutToTrigger(Kingmaker.RuleSystem.Rules.RuleSavingThrow e){}
        public void OnEventDidTrigger(Kingmaker.RuleSystem.Rules.RuleSavingThrow e){if(e.IsPassed)Affect(e.Initiator,e.Reason?.Context);}
    }
    [TypeId("45ec5158b9324c20ac57a16f58000204")]
    public sealed class SelectSynapticTarget01458:ContextAction
    {
        public override string GetCaption()=>"Synaptic Shock: select one source-owned target";
        public override void RunAction(){var caster=Context.MaybeCaster;if(caster==null||Target?.Unit==null||!Target.Unit.IsEnemy(caster))return;var runtime=SynapticShock01458.FindRuntime(caster,out var scope);using(scope)runtime?.Select(Target.Unit);}
    }
    [TypeId("45ec5158b9324c20ac57a16f58000205")]
    public sealed class SpaceRendingCaster01458:BlueprintComponent,IAbilityCasterRestriction
    {
        public int Minimum;public BlueprintAbilityResourceReference Resource;
        public bool IsCasterRestrictionPassed(UnitEntityData caster)=>caster.Resources.ContainsResource(Resource.Get())&&PsychicAdvancedRuntime01458.SpaceLimit(caster)>=Minimum;
        public string GetAbilityCasterRestrictionUIText()=>"A linked spell of sufficient level must have been cast within the last round.";
    }
    [TypeId("45ec5158b9324c20ac57a16f58000206")]
    public sealed class EndSpaceRending01458:ContextAction
    {public override string GetCaption()=>"Consume this caster's space-rending teleport opportunity";public override void RunAction(){if(Context.MaybeCaster!=null)PsychicAdvancedRuntime01458.EndSpace(Context.MaybeCaster);}}
    [HarmonyPatch(typeof(AbilityData),nameof(AbilityData.HasMetamagic))]
    internal static class MimicMetamagicEligibility01458Patch
    {
        static void Postfix(AbilityData __instance,Metamagic metamagic,ref bool __result){
            if(__result||metamagic==0)return;
            var mask=MimicMetamagic01458.Preview(__instance);var remaining=metamagic&~mask;
            if(remaining!=metamagic)__result=remaining==0||__instance.HasMetamagic(remaining);
        }
    }
    [HarmonyPatch(typeof(AbilityData),"get_IsAvailableForCast")]
    internal static class UndercastSurgeAvailability01458Patch
    {static void Postfix(AbilityData __instance,ref bool __result){if(__result&&!PsychicAdvancedRuntime01458.SurgeValid(__instance))__result=false;}}
    [HarmonyPatch(typeof(RuleCastSpell),nameof(RuleCastSpell.OnTrigger))]
    internal static class UndercastSurgeStaleCast01458Patch
    {static bool Prefix(RuleCastSpell __instance){if(PsychicAdvancedRuntime01458.SurgeValid(__instance.Spell,__instance.Context))return true;__instance.ForceFail=true;return false;}}
    [HarmonyPatch(typeof(MechanicsContext),nameof(MechanicsContext.Recalculate))]
    internal static class MimicMetamagicRecalculate01458Patch
    {static void Postfix(MechanicsContext __instance)=>MimicMetamagic01458.Restore(__instance);}
}

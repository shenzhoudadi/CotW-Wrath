using System;
using System.Linq;
using System.Collections.Generic;
using System.Runtime.Serialization;
using HarmonyLib;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.EntitySystem;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Enums;
using Kingmaker.Enums.Damage;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components;
using Kingmaker.UnitLogic.Buffs;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.FactLogic;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Mechanics.Actions;
using Kingmaker.UnitLogic.Mechanics.Components;
using Kingmaker.RuleSystem;
using Kingmaker.RuleSystem.Rules.Damage;
using Kingmaker.Designers.EventConditionActionSystem.Actions;
using CallOfTheWild.Wrath.Prototype;
internal static class PsychicShapeTests {
 static int n,healed,damaged,removed;static bool negative;static EntityFactsManager facts;
 static T Empty<T>()where T:class=>(T)FormatterServices.GetUninitializedObject(typeof(T));static object Get(object o,string s)=>AccessTools.Field(o.GetType(),s).GetValue(o);static void Set(object o,string s,object v)=>AccessTools.Field(o.GetType(),s).SetValue(o,v);static void Check(bool b,string s){if(!b)throw new Exception("Shapes: "+s);n++;}
 static T Asset<T>(BlueprintsCache c,string token)where T:SimpleBlueprint=>(T)c.Load((BlueprintGuid)AccessTools.Method(typeof(PsychicShapeReplacement).Assembly.GetType("CallOfTheWild.Wrath.Prototype.PsychicAssets"),"Id").Invoke(null,new object[]{token}));
 static MechanicsContext Context(UnitEntityData u,BlueprintScriptableObject a,int cl=10,Metamagic meta=0){var c=new MechanicsContext(u,u.Descriptor,a,null,new Kingmaker.Utility.TargetWrapper(u));c.SetParams(new AbilityParams {CasterLevel=cl,DC=23,SpellLevel=6,Metamagic=meta});var ranks=new int[32];foreach(var r in a.GetComponents<ContextRankConfig>())ranks[(int)Get(r,"m_Type")]=r.GetValue(c);Set(c,"m_Ranks",ranks);return c;}
 static Buff Fact(BlueprintBuff bp,MechanicsContext ctx){var b=Empty<Buff>();Set(b,"<Blueprint>k__BackingField",bp);Set(b,"Components",new List<EntityFactComponent>());Set(b,"m_Context",ctx);Set(b,"<Rank>k__BackingField",1);b.RestoreOwnerLink(facts);return b;}
 static EntityFactComponent Runtime(BlueprintComponent c,Buff f){var rt=(EntityFactComponent)AccessTools.Method(c.GetType(),"CreateRuntimeFactComponent").Invoke(c,null);rt.Setup(c);rt.RestoreOwnerLink(f);return rt;}
 internal static int Run(BlueprintsCache c){var u=new Psychic070Tests.Caster(c).Unit;Set(u.State,"m_Conditions",new sbyte[128]);Set(u.State,"m_ConditionsImmunity",new sbyte[128]);Set(u.State,"m_ConditionsExceptions",new List<UnitConditionExceptions>[128]);facts=new EntityFactsManager(u);Set(u,"Facts",facts);var h=new Harmony("CotW01322.ShapeTests");void Patch(System.Reflection.MethodBase m,string name)=>h.Patch(m,prefix:new HarmonyMethod(typeof(PsychicShapeTests),name){priority=Priority.First});
 Patch(AccessTools.Method(typeof(ContextActionHealTarget),"RunAction"),nameof(Heal));Patch(AccessTools.Method(typeof(ContextActionDealDamage),"RunAction"),nameof(Damage));Patch(AccessTools.Method(typeof(UnitHelper),"HasFact",new[]{typeof(UnitDescriptor),typeof(Kingmaker.Blueprints.Facts.BlueprintFact)}),nameof(Has));Patch(AccessTools.Method(typeof(Buff),"Remove"),nameof(Remove));
 try {
 string[] forms={"UndeadAnatomy1","UndeadAnatomy2","UndeadAnatomy3","GiantForm1Troll","GiantForm1Fire","GiantForm1Frost","GiantForm2Storm","GiantForm2Athach"};
 foreach(var token in forms){var a=Asset<BlueprintAbility>(c,token);var b=Asset<BlueprintBuff>(c,token+"Buff");Check(a.Range==AbilityRange.Personal&&!a.SpellResistance&&a.GetComponent<SpellComponent>().School==SpellSchool.Transmutation,"Personal no-save polymorph");Check(b.GetComponent<SpellDescriptorComponent>().Descriptor.HasAnyFlag(SpellDescriptor.Polymorph),"Polymorph descriptor");var acts=a.GetComponent<AbilityEffectRunAction>().Actions.Actions;Check(acts[0]is ContextActionRemoveBuffsByDescriptor&&((ContextActionRemoveBuffsByDescriptor)acts[0]).SpellDescriptor.HasAnyFlag(SpellDescriptor.Polymorph),"Old form removed before new application");var apply=(ContextActionApplyBuff)acts[1];Check(apply.Buff==b,"Correct form target");foreach(int cl in new[]{1,10,20})foreach(bool ext in new[]{false,true})Check(apply.DurationValue.Calculate(Context(u,a,cl,ext?Metamagic.Extend:0)).Seconds==TimeSpan.FromMinutes(cl*(ext?2:1)),"Native CL and Extend duration");
 var back=Asset<BlueprintAbility>(c,token+"Return");Check(back.Type==AbilityType.Supernatural&&back.ActionType==Kingmaker.UnitLogic.Commands.Base.UnitCommand.CommandType.Standard&&b.GetComponents<AddFacts>().Any(x=>x.Facts.Contains(back)),"Source owned return standard action");Check(((ContextActionRemoveBuff)back.GetComponent<AbilityEffectRunAction>().Actions.Actions.Single()).Buff==b,"Return removes only matching form");
 foreach(var bonus in b.GetComponents<AddStatBonus>()){var stat=new ModifiableValue(null,bonus.Stat){BaseValue=20};var other=stat.AddModifier(1,Empty<Buff>(),ModifierDescriptor.UntypedStackable);var own=stat.AddModifier(bonus.Value,Empty<Buff>(),bonus.Descriptor);Check(stat.ModifiedValue==21+bonus.Value,"Native stat modifiers and coexistence");stat.RemoveModifier(own);Check(stat.ModifiedValue==21,"No leftover form modifier");stat.RemoveModifier(other);}
 var fact=Fact(b,Context(u,a));var replace=b.GetComponent<PsychicShapeReplacement>();var rt=Runtime(replace,fact);removed=0;using(rt.RequestEventContext()){replace.OnPolymorphActivated(new Psychic070Tests.Caster(c).Unit,new Polymorph());Check(removed==0,"Other unit polymorph does not clear ours");replace.OnPolymorphActivated(u,new Polymorph());Check(removed==1,"Native polymorph activation clears our prior shape");}
 foreach(var dr in b.GetComponents<AddDamageResistancePhysical>()){var runtime=(AddDamageResistanceBase.ComponentRuntime)Runtime(dr,fact);foreach(var form in new[]{PhysicalDamageForm.Bludgeoning,PhysicalDamageForm.Piercing,PhysicalDamageForm.Slashing}){var d=new PhysicalDamage(new Kingmaker.RuleSystem.Rules.ModifiableDiceFormula(new DiceFormula(1,DiceType.D6)),0,form);Check(runtime.GetValue()==5,"Native DR5");Check(runtime.Bypassed(d,null)==(token=="UndeadAnatomy2"&&form==PhysicalDamageForm.Bludgeoning),"Native damage form bypass");}}
 foreach(var dr in b.GetComponents<AddDamageResistanceEnergy>()){var runtime=(AddDamageResistanceBase.ComponentRuntime)Runtime(dr,fact);foreach(var energy in new[]{DamageEnergyType.Acid,DamageEnergyType.Fire,DamageEnergyType.Cold,DamageEnergyType.Electricity}){var d=new EnergyDamage(new DiceFormula(1,DiceType.D6),0,energy);Check(runtime.GetValue()==20&&runtime.Bypassed(d,null)==(energy!=dr.Type),"Native elemental DR20 only matching type");}}
 }
 string[] roots={"UndeadAnatomy1","UndeadAnatomy2","UndeadAnatomy3","GiantForm1","GiantForm2"};int[][] levels={new[]{3,3,3},new[]{5,5,5},new[]{6,6,6},new[]{6,7},new[]{8}};for(int i=0;i<5;i++){var a=Asset<BlueprintAbility>(c,roots[i]);Check(a.GetComponents<SpellListComponent>().Select(x=>x.SpellLevel).OrderBy(x=>x).SequenceEqual(levels[i]),"Original class levels");foreach(var l in a.GetComponents<SpellListComponent>())Check(l.SpellList.SpellsByLevel[l.SpellLevel].Spells.Count(x=>x==a)==1,"One learnable root per list");if(i>=3){var vs=a.GetComponent<AbilityVariants>().Variants;Check(vs.Length==(i==3?3:2)&&vs.All(x=>x.Parent==a&&!x.GetComponents<SpellListComponent>().Any()),"Giant children belong to one learnable root");}}
 foreach(bool affinity in new[]{false,true})foreach(bool cure in new[]{false,true}){negative=affinity;healed=damaged=0;var a=(BlueprintAbility)c.Load(BlueprintGuid.Parse(cure?"47808d23c67033d4bbab86a1070fd62f":"e5cb4c4459e437e49a4cd73fde6b9063"));var cond=(Conditional)a.GetComponent<AbilityEffectRunAction>().Actions.Actions[0];bool result;using(Context(u,a).GetDataScope(u))result=cond.ConditionsChecker.Check();Check(result==(cure?!affinity:affinity),"Native cure/inflict condition routes on negative affinity");var branch=result?cond.IfTrue:cond.IfFalse;Check((branch.Actions[0] is ContextActionHealTarget)==(cure!=affinity),"Native matching healing branch; alternate is harmful save/damage");}
 }finally{h.UnpatchAll(h.Id);}
 PsychicExpansionTests.Inventory["shapeSpellAssertions"]=n;Console.WriteLine("PASS: "+n+" shape checks; native duration/modifier removal/DR bypass/cure branches, variants and scoped polymorph events. Scene and final HP endpoints seeded.");return n;
 }
 static bool Heal(){healed++;return false;}static bool Damage(){damaged++;return false;}static bool Remove(){removed++;return false;}static bool Has(Kingmaker.Blueprints.Facts.BlueprintFact __1,ref bool __result){__result=negative&&__1.AssetGuid.ToString()=="d5ee498e19722854198439629c1841a5";return false;}
}

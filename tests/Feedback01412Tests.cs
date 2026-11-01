using System;
using System.Linq;
using System.Collections.Generic;
using System.Runtime.Serialization;
using HarmonyLib;
using Kingmaker;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Items.Ecnchantments;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.Controllers.Combat;
using Kingmaker.EntitySystem;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.Enums;
using Kingmaker.Enums.Damage;
using Kingmaker.Items;
using Kingmaker.Items.Slots;
using Kingmaker.RuleSystem;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.FactLogic;
using Kingmaker.UnitLogic.Buffs;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Buffs.Components;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.Utility;
using CallOfTheWild.Wrath.Prototype;
using UnityEngine;

// Execute native enchantment collections and Engage/Disengage dictionaries.
// Unity rendering, faction/world query, awareness and final AoO command delivery
// remain explicit inputs. Multi-owner fact runtimes share the real blueprint.
internal static class Feedback01412Tests {
 static int n,removed,opportunities;static UnitEntityData[] world=Array.Empty<UnitEntityData>();
 static T Empty<T>()where T:class=>(T)FormatterServices.GetUninitializedObject(typeof(T));
 static object Get(object o,string f)=>(AccessTools.Field(o.GetType(),f)??AccessTools.Field(o.GetType(),"<"+f+">k__BackingField")).GetValue(o);
 static void Set(object o,string f,object v)=>(AccessTools.Field(o.GetType(),f)??AccessTools.Field(o.GetType(),"<"+f+">k__BackingField")).SetValue(o,v);
 static void Check(bool b,string m){if(!b)throw new Exception("Feedback 01412: "+m);n++;}
 static T Asset<T>(BlueprintsCache c,string token)where T:SimpleBlueprint=>Psychic034Tests.Asset<T>(c,token);
 static object Helper(string name,params object[] args)=>AccessTools.Method(typeof(WildShapeFeatTests),name).Invoke(null,args);
 static UnitEntityData Unit(BlueprintsCache c,string id){var u=(UnitEntityData)Helper("Unit",c,id);Set(u,"CombatState",Empty<UnitCombatState>());Set(u.CombatState,"Unit",u);Set(u.CombatState,"m_InCombat",true);Set(u.CombatState,"AIData",new CombatAiData());Set(u.CombatState,"m_EngagedUnits",new Dictionary<UnitEntityData,TimeSpan>());Set(u.CombatState,"m_EngagedBy",new Dictionary<UnitEntityData,TimeSpan>());Set(u.CombatState,"Cooldown",new UnitCombatState.Cooldowns());Set(u.CombatState,"DisengageAttackTargets",new List<UnitEntityData>());Set(u,"Passive",new ObservableCountFlag());Set(u.State.Features,"SoloTactics",new CountableFlag());return u;}
 static Feature Fact(UnitEntityData u,BlueprintFeature bp)=>(Feature)Helper("Fact",u,bp);
 static ItemEntityWeapon Weapon(UnitEntityData u,bool natural,string id)=>(ItemEntityWeapon)Helper("Weapon",u,natural?WeaponCategory.Claw:WeaponCategory.Longsword,natural,PhysicalDamageForm.Slashing,id);
 static EntityFactComponent Runtime(EntityFact f,Type t)=>f.Components.Single(r=>r.SourceBlueprintComponent.GetType()==t);
 static void Call(EntityFact f,Type t,string method,params object[] args){var rt=Runtime(f,t);using(rt.RequestEventContext())AccessTools.Method(t,method).Invoke(rt.SourceBlueprintComponent,args);}
 static bool Skip()=>false;static bool No(ref bool __result){__result=false;return false;}static bool Yes(ref bool __result){__result=true;return false;}
 static bool Targets(ref IEnumerable<UnitEntityData> __result){__result=world;return false;}
 static bool Time(ref TimeSpan __result){__result=TimeSpan.FromSeconds(100);return false;}
 static bool Wielder(ref UnitDescriptor __result){__result=null;return false;}
 static bool Remove(Buff __instance){removed++;__instance.CallComponents<PsychicFixedWeaponEnchantment>(x=>AccessTools.Method(typeof(PsychicFixedWeaponEnchantment),"OnTurnOff").Invoke(x,null));Set(__instance,"IsTurnedOn",false);return false;}
 static void Attached(EntityFact __instance){if(string.IsNullOrEmpty(__instance.UniqueId))Set(__instance,"UniqueId",Guid.NewGuid().ToString("N"));}
 static bool Join(UnitCombatState __instance){Set(__instance,"m_InCombat",true);return false;}
 static bool Conceal(ref Concealment __result){__result=Concealment.None;return false;}
 static bool Opportunity(UnitCombatState __instance,ref bool __result){if(__instance.CanAttackOfOpportunity){Set(__instance,"m_AttackOfOpportunityCount",__instance.AttackOfOpportunityCount-1);opportunities++;__result=true;}else __result=false;return false;} // Final command/payment/UI notification is captured.
 static bool CanSee(ref bool __result){__result=true;return false;}
 static bool View(ref Kingmaker.View.UnitEntityView __result){__result=Empty<Kingmaker.View.UnitEntityView>();return false;}
 static bool Reach(UnitEntityData unit,UnitEntityData enemy,float threatRange,ref bool __result){__result=((Vector3)Get(unit,"m_Position")-(Vector3)Get(enemy,"m_Position")).sqrMagnitude<(threatRange+1f)*(threatRange+1f);return false;}
 internal static int Run(BlueprintsCache c){
  n=0;var h=new Harmony("CotW01412.TargetedNative");var oldTime=Game.Instance.TimeController;Set(Game.Instance,"TimeController",Empty<Kingmaker.Controllers.TimeController>());
  void Patch(System.Reflection.MethodBase method,string name)=>h.Patch(method,prefix:new HarmonyMethod(typeof(Feedback01412Tests),name));
  Patch(AccessTools.PropertyGetter(typeof(Kingmaker.Controllers.TimeController),"GameTime"),nameof(Time));
  Patch(AccessTools.Method(typeof(EntityFact),"Attach"),nameof(Attached));Patch(AccessTools.Method(typeof(ItemEnchantment),"RespawnFx"),nameof(Skip));Patch(AccessTools.PropertyGetter(typeof(ItemEntity),"Wielder"),nameof(Wielder));
  Patch(AccessTools.Method(typeof(FeatureCollection),"CheckStatPrerequisitesAndUpdate"),nameof(Skip));Patch(AccessTools.Method(typeof(Buff),"Remove"),nameof(Remove));
  Patch(AccessTools.Method(typeof(Kingmaker.Designers.GameHelper),"GetTargetsAround",new[]{typeof(Vector3),typeof(float),typeof(bool),typeof(bool)}),nameof(Targets));
  Patch(AccessTools.Method(typeof(UnitEntityData),"IsAlly",new[]{typeof(UnitEntityData)}),nameof(Yes));Patch(AccessTools.Method(typeof(UnitEntityData),"IsEnemy",new[]{typeof(UnitEntityData)}),nameof(Yes));
  Patch(AccessTools.PropertyGetter(typeof(UnitState),"CanAct"),nameof(Yes));Patch(AccessTools.PropertyGetter(typeof(UnitState),"IsConscious"),nameof(Yes));Patch(AccessTools.Method(typeof(UnitCombatState),"JoinCombat"),nameof(Join));
  Patch(AccessTools.Method(typeof(Kingmaker.UnitLogic.Parts.UnitPartConcealment),"Calculate",new[]{typeof(UnitEntityData),typeof(UnitEntityData),typeof(bool)}),nameof(Conceal));
  Patch(AccessTools.Method(typeof(UnitEntityData),"HasLOS",new[]{typeof(UnitEntityData)}),nameof(CanSee));
  Patch(AccessTools.GetDeclaredMethods(typeof(UnitEntityData)).Single(m=>m.Name=="get_View"&&m.ReturnType==typeof(Kingmaker.View.UnitEntityView)),nameof(View));
  Patch(AccessTools.Method(typeof(UnitHelper),"IsAttackOfOpportunityReach"),nameof(Reach));
  Patch(AccessTools.Method(typeof(UnitCombatState),"AttackOfOpportunity"),nameof(Opportunity));
  Patch(AccessTools.Method(typeof(TurnBased.Controllers.CombatController),"IsInTurnBasedCombat"),nameof(No));
  Patch(AccessTools.PropertyGetter(typeof(UnitEntityData),"HasMotionThisTick"),nameof(No));
  try{Enchantments(c);Swarm(c);Ally(c);}finally{h.UnpatchAll(h.Id);Set(Game.Instance,"TimeController",oldTime);world=Array.Empty<UnitEntityData>();}
  PsychicExpansionTests.Inventory["feedback01412Assertions"]=n;Console.WriteLine("PASS: "+n+" 01412 fixed-item spell lifetime, multi-owner live AC and native Ally engagement checks; explicit view/faction/awareness/command boundaries.");return n;
 }
 static void Enchantments(BlueprintsCache c){
  string[] names=new[]{"FlamesOfTheFaithfulBuff","DazzlingBladeBuff"}.Concat(new[]{"Vicious","Keen","Cruel","Heartseeker"}.SelectMany(p=>new[]{"Main","Off"}.Select(hand=>"SpiritBoundBlade"+p+hand+"Buff"))).Concat(new[]{"Anarchic","Axiomatic","Holy","Unholy"}.SelectMany(p=>new[]{"MainHand","OffHand"}.Select(hand=>"WrathfulWeapon"+p+hand+"Buff"))).ToArray();
  foreach(string token in names){
   var u=Unit(c,"01412-"+token);u.Descriptor.Proficiencies.Add(WeaponCategory.Longsword);var source=Weapon(u,false,token+"source");var off=Weapon(u,false,token+"off");Helper("Equip",u.Body.PrimaryHand,source);Helper("Equip",u.Body.SecondaryHand,off);
   var buff=Psychic034Tests.Fact(u,Asset<BlueprintBuff>(c,token),u,15);
   foreach(var r in buff.Components.Where(r=>r.SourceBlueprintComponent is PsychicFixedWeaponEnchantment))using(r.RequestEventContext())AccessTools.Method(typeof(PsychicFixedWeaponEnchantment),"OnTurnOn").Invoke(r.SourceBlueprintComponent,null);
   var components=buff.Components.Where(r=>r.SourceBlueprintComponent is PsychicFixedWeaponEnchantment).ToArray();var selected=((PsychicFixedWeaponEnchantment)components[0].SourceBlueprintComponent).OffHand?off:source;
   var originals=selected.Enchantments.ToArray();Check(originals.Length>=2,"spell created real source enchantments "+token);
   Helper("Shape",u,(BlueprintBuff)c.Load(BlueprintGuid.Parse("470fb1a22e7eb5849999f1101eacc5dc")));
   var natural=Weapon(u,true,token+"claw");Helper("Equip",u.Body.PrimaryHand,natural);
   var shift=Fact(u,Asset<BlueprintFeature>(c,"WeaponShiftFeat"));Fact(u,Asset<BlueprintFeature>(c,"ImprovedWeaponShiftFeat"));
   foreach(var r in components)using(r.RequestEventContext())((PsychicFixedWeaponEnchantment)r.SourceBlueprintComponent).OnNewRound();
   Check(buff.IsTurnedOn&&originals.All(e=>selected.Enchantments.Contains(e)),"melded item retains spell after native-hand replacement "+token);
   Call(shift,typeof(WeaponShiftTransfer),"Refresh");
   Check(source.Enchantments.All(e=>natural.Enchantments.Any(x=>x.Blueprint==e.Blueprint)),"all main-hand spell properties transfer "+token);
   Check(natural.Enchantments.All(e=>e.ParentContext==buff.Context),"transferred spell context preserved "+token);
   if(token=="FlamesOfTheFaithfulBuff"){
    var judgment=Psychic034Tests.Fact(u,(BlueprintBuff)c.Load(BlueprintGuid.Parse("9b8bb2ce8f67e5b4fa634ed6a6671f7a")),u,15);
    foreach(var r in components)using(r.RequestEventContext())((PsychicFixedWeaponEnchantment)r.SourceBlueprintComponent).HandleUnitGainFact(judgment);
    Call(shift,typeof(WeaponShiftTransfer),"Refresh");var burst=Asset<BlueprintWeaponEnchantment>(c,"FlamesOfTheFaithfulBurstEnchantment");Check(source.Enchantments.Any(e=>e.Blueprint==burst)&&natural.Enchantments.Any(e=>e.Blueprint==burst),"Judgment adds native critical burst to melded item and natural weapon");
    foreach(var r in components)using(r.RequestEventContext())((PsychicFixedWeaponEnchantment)r.SourceBlueprintComponent).HandleUnitLostFact(judgment);
    Call(shift,typeof(WeaponShiftTransfer),"Refresh");Check(!source.Enchantments.Any(e=>e.Blueprint==burst)&&!natural.Enchantments.Any(e=>e.Blueprint==burst),"Judgment end removes burst while preserving ordinary flaming");u.Facts.List.Remove(judgment);
   }
   foreach(var r in components)using(r.RequestEventContext())((PsychicFixedWeaponEnchantment)r.SourceBlueprintComponent).HandleEquipmentSlotUpdated(u.Body.PrimaryHand,null);
   Check(buff.IsTurnedOn,"polymorph equipment event does not cancel spell "+token);
   foreach(var r in components)using(r.RequestEventContext())AccessTools.Method(typeof(PsychicFixedWeaponEnchantment),"OnTurnOff").Invoke(r.SourceBlueprintComponent,null);
   Call(shift,typeof(WeaponShiftTransfer),"Refresh");Check(!selected.Enchantments.Any()&&!natural.Enchantments.Any(),"expiration removes only spell-owned original and transferred properties "+token);
   Set(u.Body,"IsPolymorphed",false);Helper("Equip",u.Body.PrimaryHand,source);Helper("Equip",u.Body.SecondaryHand,off);foreach(var r in components)using(r.RequestEventContext())AccessTools.Method(typeof(PsychicFixedWeaponEnchantment),"OnTurnOn").Invoke(r.SourceBlueprintComponent,null);
   removed=0;Helper("Equip",((PsychicFixedWeaponEnchantment)components[0].SourceBlueprintComponent).OffHand?u.Body.SecondaryHand:u.Body.PrimaryHand,Weapon(u,false,token+"replacement"));
   foreach(var r in components)using(r.RequestEventContext())((PsychicFixedWeaponEnchantment)r.SourceBlueprintComponent).OnNewRound();Check(removed>0&&!selected.Enchantments.Any(),"real weapon replacement still ends binding "+token);
  }
  foreach(string token in Enumerable.Range(1,3).Select(i=>"ForceSwordWeapon"+i).Concat(Enumerable.Range(0,4).Select(i=>"EmblemOfGreedItem"+i))){
   var bp=Asset<Kingmaker.Blueprints.Items.Weapons.BlueprintItemWeapon>(c,token);var u=Unit(c,"01412-created-"+token);var source=Weapon(u,false,token+"source");Set(source,"m_Blueprint",bp);u.Proficiencies.Add(bp.Category);Helper("Equip",u.Body.PrimaryHand,source);
   var context=new MechanicsContext(u,u,bp,null);foreach(var e in (BlueprintWeaponEnchantmentReference[])Get(bp,"m_Enchantments"))source.AddEnchantment(e.Get(),context);
   Helper("Shape",u,(BlueprintBuff)c.Load(BlueprintGuid.Parse("470fb1a22e7eb5849999f1101eacc5dc")));var natural=Weapon(u,true,token+"claw");Helper("Equip",u.Body.PrimaryHand,natural);var shift=Fact(u,Asset<BlueprintFeature>(c,"WeaponShiftFeat"));Fact(u,Asset<BlueprintFeature>(c,"ImprovedWeaponShiftFeat"));Call(shift,typeof(WeaponShiftTransfer),"Refresh");Check(source.Enchantments.Any()&&source.Enchantments.All(e=>natural.Enchantments.Any(x=>x.Blueprint==e.Blueprint)),"existing created-weapon enhancement/enchantment instances use accepted transfer path "+token);
  }
 }
 static void Swarm(BlueprintsCache c){
  var units=Enumerable.Range(0,4).Select(i=>Unit(c,"01412-swarm-"+i)).ToArray();world=units;var bp=Asset<BlueprintFeature>(c,"SwarmScatterFeat");var facts=units.Select(u=>Fact(u,bp)).ToArray();
  for(int cycle=0;cycle<3;cycle++)foreach(int count in new[]{3,2,1,0,2,0}){
   for(int i=0;i<4;i++)Set(units[i],"m_Position",i<=count?new Vector3(i*.5f,0,0):new Vector3(i*10,0,0));
   // Real position events name each moved entity. Emit every changed position
   // instead of silently teleporting four entities behind a single event.
   foreach(var moved in units)foreach(var f in facts)Call(f,typeof(SwarmScatterDefense),"HandleEntityPositionChanged",moved);
   for(int i=0;i<4;i++){int expected=i<=count?count:0;Check(units[i].Stats.AC.ModifiedValue==10+expected,"all owners independently follow current neighbors");Check(units[i].Stats.AC.Modifiers.Count(m=>m.Source==facts[i])==(expected>0?1:0),"no historical max or duplicate after global movement");}
  }
  Check(!ReferenceEquals(Runtime(facts[0],typeof(SwarmScatterDefense)).GetData<SwarmScatterData>(),Runtime(facts[1],typeof(SwarmScatterDefense)).GetData<SwarmScatterData>()),"shared blueprint has per-owner runtime data");
  units[0].Stats.AC.AddModifier(7,facts[0],ModifierDescriptor.Circumstance);Call(facts[0],typeof(SwarmScatterDefense),"OnApplyPostLoadFixes");Check(units[0].Stats.AC.ModifiedValue==10,"old saved stale modifier removed at zero neighbors");
  for(int i=0;i<4;i++)Set(units[i],"m_Position",new Vector3(i*.5f,0,0));foreach(var f in facts)Call(f,typeof(SwarmScatterDefense),"OnNewRound");
  Set(facts[3],"IsTurnedOn",false);for(int i=0;i<3;i++)Call(facts[i],typeof(SwarmScatterDefense),"HandleEntityPositionChanged",units[3]);Check(units[0].Stats.AC.ModifiedValue==12,"inactive neighbor excluded");
  units[0].Stats.AC.AddModifier(5,facts[1],ModifierDescriptor.Deflection);Call(facts[0],typeof(SwarmScatterDefense),"OnTurnOff");Check(units[0].Stats.AC.ModifiedValue==15,"cleanup preserves other fact's AC");
 }
 static void Ally(BlueprintsCache c){
  var caster=Unit(c,"01412-caster");var ally=Unit(c,"01412-ally");var enemy=Unit(c,"01412-enemy");Set(ally.Descriptor,"Blueprint",Asset<BlueprintUnit>(c,"SpiritualAllyUnit"));Set(ally.Descriptor,"OriginalBlueprint",ally.Blueprint);
  Set(ally.State,"m_Conditions",new sbyte[128]);Set(ally.State,"m_ConditionsImmunity",new sbyte[128]);Set(ally.State,"m_ConditionsExceptions",new List<UnitConditionExceptions>[128]);
  var host=Psychic034Tests.Fact(caster,Asset<BlueprintBuff>(c,"SpiritualAllyController"),caster,15);Runtime(host,typeof(PsychicSpiritController)).GetData<PsychicSpiritState>().Summon=ally;
  var actor=Psychic034Tests.Fact(ally,Asset<BlueprintBuff>(c,"SpiritualAllyActorBuff"),caster,15);var sword=Weapon(ally,false,"01412-allysword");Helper("Equip",ally.Body.PrimaryHand,sword);Set(ally,"m_Position",Vector3.zero);Set(enemy,"m_Position",new Vector3(1,0,0));world=new[]{enemy};Set(ally.CombatState,"m_AttackOfOpportunityCount",1);
  Check(ally.GetThreatHand()==ally.Body.PrimaryHand,"actual native melee threat hand exists");Call(actor,typeof(PsychicSpiritAttackStats),"RefreshEngagement");Check(ally.CombatState.EngagedUnits.Contains(enemy)&&enemy.CombatState.EngagedBy.Contains(ally),"commandless Ally seeds both native engagement dictionaries");
  opportunities=0;ally.CombatState.Disengage(enemy);Check(opportunities==1&&ally.CombatState.AttackOfOpportunityCount==0,"native Disengage routes opportunity and spends existing limit");
  Call(actor,typeof(PsychicSpiritAttackStats),"RefreshEngagement");ally.CombatState.Disengage(enemy);Check(opportunities==1,"refresh does not replenish spent native opportunities");
  Set(enemy,"m_Position",new Vector3(30,0,0));Call(actor,typeof(PsychicSpiritAttackStats),"RefreshEngagement");Check(!enemy.CombatState.EngagedBy.Contains(ally),"out-of-range opponent not engaged");
  var bow=Weapon(ally,false,"01412-allybow");Set(bow.Blueprint.Type,"m_AttackType",AttackType.Ranged);bow.Blueprint.Type.OnEnable();Helper("Equip",ally.Body.PrimaryHand,bow);Check(ally.GetThreatHand()==null,"ranged favored form does not gain synthetic melee threat");Set(enemy,"m_Position",new Vector3(1,0,0));Call(actor,typeof(PsychicSpiritAttackStats),"RefreshEngagement");Check(!enemy.CombatState.EngagedBy.Contains(ally),"ranged form excluded from engagement repair");
  Check(AccessTools.Method(typeof(SpiritWeaponView),"SpiritMaterial")!=null&&actor.Blueprint.GetComponent<SpiritWeaponAnimatedVisual>().Kind==1,"scene view uses owned blue transparent human material and native hand animation");
 }
}

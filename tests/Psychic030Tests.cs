using System;
using System.Linq;
using System.Collections.Generic;
using System.Runtime.Serialization;
using HarmonyLib;
using Newtonsoft.Json;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.Blueprints.Items.Weapons;
using Kingmaker.EntitySystem;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components;
using Kingmaker.UnitLogic.Buffs;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Mechanics.Actions;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.RuleSystem.Rules.Damage;
using Kingmaker.Utility;
using UnityEngine;
using CallOfTheWild.Wrath.Prototype;
internal static class Psychic030Tests {
 static int n,attacks,destroyed,removed,resistance;static bool resisted;static TimeSpan now;static readonly List<int> indices=new List<int>();
 static T Empty<T>()where T:class=>(T)FormatterServices.GetUninitializedObject(typeof(T));static object Get(object o,string k)=>AccessTools.Field(o.GetType(),k).GetValue(o);static void Set(object o,string k,object v)=>AccessTools.Field(o.GetType(),k).SetValue(o,v);static void Check(bool b,string m){if(!b)throw new Exception("030: "+m);n++;}
 static T Asset<T>(BlueprintsCache c,string t)where T:SimpleBlueprint=>(T)c.Load((BlueprintGuid)AccessTools.Method(typeof(PsychicSpiritBind).Assembly.GetType("CallOfTheWild.Wrath.Prototype.PsychicAssets"),"Id").Invoke(null,new object[]{t}));
 static UnitEntityData Unit(BlueprintsCache c,string id){var u=new Psychic070Tests.Caster(c).Unit;Set(u,"<UniqueId>k__BackingField",id);var facts=new EntityFactsManager(u);Set(u,"Facts",facts);var bc=new BuffCollection();bc.Initialize(facts);Set(u,"<Buffs>k__BackingField",bc);return u;}
 static Buff Fact(UnitEntityData owner,BlueprintBuff bp){var b=Empty<Buff>();Set(b,"Components",new List<EntityFactComponent>());b.RestoreOwnerLink(owner.Facts);var ctx=new MechanicsContext(owner,owner.Descriptor,bp,null,new TargetWrapper(owner));ctx.SetParams(new AbilityParams{CasterLevel=15,DC=30});Set(b,"m_Context",ctx);Set(b,"<Rank>k__BackingField",1);AccessTools.Method(typeof(EntityFact),"Setup").Invoke(b,new object[]{bp});Set(b,"<IsActive>k__BackingField",true);Set(b,"<IsTurnedOn>k__BackingField",true);owner.Facts.List.Add(b);AccessTools.Method(Get(owner.Facts,"m_Cache").GetType(),"Clear").Invoke(Get(owner.Facts,"m_Cache"),null);return b;}
 static bool Time(ref TimeSpan __result){__result=now;return false;}static bool Range(ref bool __result){__result=true;return false;}static bool Enemy(UnitEntityData __instance,UnitEntityData unit,ref bool __result){__result=__instance!=unit;return false;}static bool Dead(ref bool __result){__result=false;return false;}
 static bool Strike(UnitEntityData u,UnitEntityData target,int index,int count){attacks++;indices.Add(index);return false;}static bool Place()=>false;static bool Destroy(){destroyed++;return false;}static bool Remove(){removed++;return false;}
 static bool SR(RuleSpellResistanceCheck __instance){resistance++;return false;}static bool Resisted(ref bool __result){__result=resisted;return false;}
 static BlueprintBuffReference Ref(BlueprintBuff b){var r=new BlueprintBuffReference();Set(r,"deserializedGuid",b.AssetGuid);return r;}
 static void Call(Buff b,Action<PsychicSpiritController> action){var rt=b.Components.Single(x=>x.SourceBlueprintComponent is PsychicSpiritController);using(rt.RequestEventContext())action((PsychicSpiritController)rt.SourceBlueprintComponent);}
 internal static int Run(BlueprintsCache c){var h=new Harmony("CotW030.SummonLifecycle");var old=Kingmaker.Game.Instance.TimeController;Set(Kingmaker.Game.Instance,"TimeController",Empty<Kingmaker.Controllers.TimeController>());
 void Patch(System.Reflection.MethodBase m,string name){Console.WriteLine("030 patch "+m);h.Patch(m,prefix:new HarmonyMethod(typeof(Psychic030Tests),name));}
 Patch(AccessTools.PropertyGetter(typeof(Kingmaker.Controllers.TimeController),"GameTime"),nameof(Time));Patch(AccessTools.Method(typeof(PsychicSpiritController),"InRange"),nameof(Range));Patch(AccessTools.Method(typeof(UnitEntityData),"IsEnemy",new[]{typeof(UnitEntityData)}),nameof(Enemy));Patch(AccessTools.PropertyGetter(typeof(UnitState),"IsDead"),nameof(Dead));Patch(AccessTools.Method(typeof(PsychicSpiritController),"Strike"),nameof(Strike));Patch(AccessTools.Method(typeof(PsychicSpiritController),"PlaceNear"),nameof(Place));Patch(AccessTools.Method(typeof(EntityDataBase),"MarkForDestroy"),nameof(Destroy));Patch(AccessTools.Method(typeof(Buff),"Remove"),nameof(Remove));Patch(AccessTools.Method(typeof(RuleSpellResistanceCheck),"OnTrigger"),nameof(SR));Patch(AccessTools.PropertyGetter(typeof(RuleSpellResistanceCheck),"IsSpellResisted"),nameof(Resisted));
 try{string[] tokens={"SpiritualWeapon","SpiritualAlly","MagesSword","TwilightKnife","ArcaneEye"};
 for(int kind=0;kind<5;kind++){
  var token=tokens[kind];var spell=Asset<BlueprintAbility>(c,token);var unit=Asset<BlueprintUnit>(c,token+"Unit");var buff=Asset<BlueprintBuff>(c,token+"Controller");var effects=(spell.GetComponent<AbilityVariants>()?.Variants.Last(a=>a.name.EndsWith("Form3"))??spell).GetComponent<AbilityEffectRunAction>().Actions.Actions;var spawn=effects.OfType<ContextActionSpawnMonster>().Single();
  Check(spawn.Blueprint==unit&&spawn.CountValue.BonusValue.Value==1,"one actual native summoned unit");Check(spawn.IsDirectlyControllable==(kind==4),"only eye direct control");Check(spawn.AfterSpawn.Actions.OfType<PsychicSpiritBind>().Single().Controller.Get()==buff,"actor bound to matching caster host");Check(spawn.AfterSpawn.Actions.OfType<ContextActionApplyBuff>().Single().Buff==Asset<BlueprintBuff>(c,token+"ActorBuff"),"damage buff receives original caster context");
  Check(unit.ComponentsArray.All(x=>x.GetType().Name=="AddClassLevels")&&unit.ComponentsArray.Length==1,"one level native creation; no native monster loot/XP/custom actions");Check(unit.AddFacts.Count()==1,"donor attacks/abilities removed");Check(unit.AlternativeBrains.Length==0,"no alternative attack AI");Check(unit.Body.SecondaryHand==null,"no extra offhand attack");
  if(kind!=4){var weapon=Asset<BlueprintItemWeapon>(c,token+"Item");Check(unit.Body.PrimaryHand==weapon,"own force weapon equipped in blueprint");Check(weapon.DamageType.Type==DamageType.Force,"force damage native item override");Check(((Array)Get(weapon,"m_Enchantments")).Length==0,"no donor enhancement or rider damage");Check(((Kingmaker.RuleSystem.DiceFormula)Get(weapon,"m_DamageDice")).Rolls==(kind==2?4:1),"base dice count");}
  var caster=Unit(c,"caster-"+kind);var target=Unit(c,"target-"+kind);var actor=Unit(c,"actor-"+kind);var host=Fact(caster,buff);var rt=host.Components.Single(x=>x.SourceBlueprintComponent is PsychicSpiritController);var data=rt.GetData<PsychicSpiritState>();data.Summon=actor;data.Target=target;data.Level=15;data.Bab=16;data.Mental=5;data.ResistancePassed.Add(target.UniqueId);
  Check(host.Components.Count==2,"controller and granted actions initialized");Check(host.Components.Select(x=>x.SourceBlueprintComponentName).Distinct().Count()==2,"save dictionary keys unique");
  now=TimeSpan.FromSeconds(10);attacks=0;indices.Clear();
  if(kind!=4){Call(host,x=>AccessTools.Method(typeof(PsychicSpiritController),"Attack").Invoke(x,new object[]{true}));Check(attacks==1&&data.NextAttack==TimeSpan.FromSeconds(16),"initial/redirect one attack and cooldown");Call(host,x=>x.OnNewRound());Check(attacks==1,"same round cannot repeat");now=TimeSpan.FromSeconds(16);Call(host,x=>{if(kind==3)AccessTools.Method(typeof(PsychicSpiritController),"Attack").Invoke(x,new object[]{true});else x.OnNewRound();});Check(attacks==1+(kind>=2?1:4),"later round iterative or single attacks");Check(indices.Last()==(kind>=2?0:3),"correct iterative index");
   int priorAttacks=attacks;Call(host,x=>AccessTools.Method(typeof(PsychicSpiritController),"Redirect").Invoke(x,new object[]{target}));Check(attacks==priorAttacks&&data.SingleNext,"redirect after attack queues single attack without extra strike");Check(new PsychicSpiritReady{Controller=Ref(buff)}.IsCasterRestrictionPassed(caster),"control remains usable after automatic attack");
   var encoded=JsonConvert.SerializeObject(new {data.NextAttack,data.Bab,data.Level,data.Mental,data.ResistancePassed});Check(encoded.Contains("target-"+kind)&&encoded.Contains("00:00:22"),"cooldown and SR identity data serializable");now=TimeSpan.FromSeconds(22);data.Target=Unit(c,"resistant-"+kind);resisted=true;int before=removed;Call(host,x=>AccessTools.Method(typeof(PsychicSpiritController),"Attack").Invoke(x,new object[]{true}));Check(removed==before+1,"SR failure ends only controller");resisted=false;
  }else {Call(host,x=>x.OnNewRound());Check(attacks==0,"eye never attacks");}
  int count=destroyed;Call(host,x=>AccessTools.Method(typeof(PsychicSpiritController),"OnDeactivate").Invoke(x,null));Check(destroyed==count+1&&data.Summon==null&&data.Target==null,"cleanup destroys exact owned actor and clears references");Call(host,x=>AccessTools.Method(typeof(PsychicSpiritController),"OnDeactivate").Invoke(x,null));Check(destroyed==count+1,"cleanup idempotent");
  var actorFact=Fact(caster,Asset<BlueprintBuff>(c,token+"ActorBuff"));var art=actorFact.Components.Single(x=>x.SourceBlueprintComponent is PsychicSpiritAttackStats);for(int cl=1;cl<=25;cl++){actorFact.Context.SetParams(new AbilityParams{CasterLevel=cl});var stats=Empty<RuleCalculateWeaponStats>();Set(stats,"DamageModifiers",new ModifiableBonus());using(art.RequestEventContext())((PsychicSpiritAttackStats)art.SourceBlueprintComponent).OnEventAboutToTrigger(stats);Check(kind==4?!stats.DamageModifiers.Modifiers.Any():stats.DamageBonusStat==null&&stats.DamageModifiers.Modifiers.Sum(m=>m.Value)==(kind==2?3:kind==3?0:Math.Min(5,cl/3)),"damage source excludes Strength and respects cap; final native result covered in 034");}
 }
 }finally{h.UnpatchAll(h.Id);Set(Kingmaker.Game.Instance,"TimeController",old);}
 PsychicExpansionTests.Inventory["new030Assertions"]=n;Console.WriteLine("PASS: "+n+" native summon graph, controller timing/SR/cleanup, force weapon and saved-state checks; unit placement/attack/destroy endpoints captured.");return n;
 }
}

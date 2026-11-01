using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.Serialization;
using HarmonyLib;
using Newtonsoft.Json;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.EntitySystem;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Buffs;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Mechanics.Actions;
using Kingmaker.UnitLogic.Parts;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.Utility;
using CallOfTheWild.Wrath.Prototype;
internal static class Psychic029Tests {
 static int n,saves,removed;static bool passed;static TimeSpan now;static readonly List<Buff> removedBuffs=new List<Buff>();
 static object Get(object o,string f)=>AccessTools.Field(o.GetType(),f).GetValue(o);static void Set(object o,string f,object v)=>AccessTools.Field(o.GetType(),f).SetValue(o,v);static T Empty<T>()where T:class=>(T)FormatterServices.GetUninitializedObject(typeof(T));
 static void Check(bool b,string m){if(!b)throw new Exception("029: "+m);n++;}
 static T Asset<T>(BlueprintsCache c,string token)where T:SimpleBlueprint=>(T)c.Load((BlueprintGuid)AccessTools.Method(typeof(PsychicMakeLink).Assembly.GetType("CallOfTheWild.Wrath.Prototype.PsychicAssets"),"Id").Invoke(null,new object[]{token}));
 static UnitEntityData Unit(BlueprintsCache c,string id){var u=new Psychic070Tests.Caster(c).Unit;Set(u,"<UniqueId>k__BackingField",id);var facts=new EntityFactsManager(u);Set(u,"Facts",facts);var buffs=new BuffCollection();buffs.Initialize(facts);Set(u,"<Buffs>k__BackingField",buffs);return u;}
 static Buff Add(UnitEntityData owner,BlueprintBuff bp,UnitEntityData caster){var b=Empty<Buff>();Set(b,"Components",new List<EntityFactComponent>());b.RestoreOwnerLink(owner.Facts);var ctx=new MechanicsContext(caster,owner.Descriptor,bp,null,new TargetWrapper(owner));ctx.SetParams(new AbilityParams{CasterLevel=12,DC=24});Set(b,"m_Context",ctx);Set(b,"<Rank>k__BackingField",1);AccessTools.Method(typeof(EntityFact),"Setup").Invoke(b,new object[]{bp});Set(b,"<IsActive>k__BackingField",true);Set(b,"<IsTurnedOn>k__BackingField",true);owner.Facts.List.Add(b);AccessTools.Method(Get(owner.Facts,"m_Cache").GetType(),"Clear").Invoke(Get(owner.Facts,"m_Cache"),null);return b;}
 static EntityFactComponent Runtime<T>(Buff b)=>b.Components.Single(x=>x.SourceBlueprintComponent is T);
 static void Call<T>(Buff b,Action<T> action){var rt=Runtime<T>(b);using(rt.RequestEventContext())action((T)(object)rt.SourceBlueprintComponent);}
 static bool Enemy(UnitEntityData __instance,UnitEntityData unit,ref bool __result){__result=unit!=__instance;return false;}
 static bool Save(RuleSavingThrow __instance){saves++;return false;}
 static bool Pass(ref bool __result){__result=passed;return false;}
 static bool Time(ref TimeSpan __result){__result=now;return false;}
 static bool Remove(Buff __instance){removed++;removedBuffs.Add(__instance);Set(__instance,"<IsTurnedOn>k__BackingField",false);foreach(var rt in __instance.Components.Where(x=>x.SourceBlueprintComponent is PsychicBattlemind))using(rt.RequestEventContext())AccessTools.Method(typeof(PsychicBattlemind),"OnDeactivate").Invoke(rt.SourceBlueprintComponent,null);return false;}
 internal static int Run(BlueprintsCache c){var a=Unit(c,"link-a");var b=Unit(c,"link-b");var enemy=Unit(c,"link-enemy");var other=Unit(c,"link-other");var h=new Harmony("CotW029.State");var oldTime=Kingmaker.Game.Instance.TimeController;Set(Kingmaker.Game.Instance,"TimeController",Empty<Kingmaker.Controllers.TimeController>());
 void Patch(System.Reflection.MethodBase m,string name)=>h.Patch(m,prefix:new HarmonyMethod(typeof(Psychic029Tests),name));
 Patch(AccessTools.Method(typeof(UnitEntityData),"IsEnemy",new[]{typeof(UnitEntityData)}),nameof(Enemy));Patch(AccessTools.Method(typeof(RuleSavingThrow),"OnTrigger"),nameof(Save));Patch(AccessTools.PropertyGetter(typeof(RuleSavingThrow),"IsPassed"),nameof(Pass));Patch(AccessTools.PropertyGetter(typeof(Kingmaker.Controllers.TimeController),"GameTime"),nameof(Time));Patch(AccessTools.Method(typeof(Buff),"Remove"),nameof(Remove));
 try{
 var ward=Add(a,Asset<BlueprintBuff>(c,"SanctuaryBuff"),a);Check(ward.Components.Count==1,"ward actual runtime constructed");passed=false;
 for(int i=0;i<5;i++)Call<PsychicSanctuary>(ward,x=>Check(!x.Allows(enemy),"failed attacker remains blocked"));Check(saves==1,"single failed save for casting");passed=true;for(int i=0;i<5;i++)Call<PsychicSanctuary>(ward,x=>Check(x.Allows(other),"passed attacker allowed"));Check(saves==2,"single successful save for casting");
 var data=Runtime<PsychicSanctuary>(ward).GetData<PsychicSanctuaryData>();var copy=JsonConvert.DeserializeObject<PsychicSanctuaryData>(JsonConvert.SerializeObject(data));Check(copy.Results.Count==2&&!copy.Results[enemy.UniqueId]&&copy.Results[other.UniqueId],"save-result data JSON roundtrip");data.Results=copy.Results;Call<PsychicSanctuary>(ward,x=>Check(!x.Allows(enemy),"restored outcome stays blocked"));Check(saves==2,"restored cache never rerolls");Call<PsychicSanctuary>(ward,x=>x.HandleUnitMakeOffensiveAction(enemy));Check(removedBuffs.Contains(ward),"offense removes own ward");
 var left=Add(a,Asset<BlueprintBuff>(c,"BattlemindLinkBuff"),a);var right=Add(b,Asset<BlueprintBuff>(c,"BattlemindLinkBuff"),a);var ld=Runtime<PsychicBattlemind>(left).GetData<PsychicBattlemindData>();var rd=Runtime<PsychicBattlemind>(right).GetData<PsychicBattlemindData>();ld.Pair=rd.Pair="paired";ld.Partner=b;rd.Partner=a;Check(left.Components.Count==2&&right.Components.Count==2,"initiative and link both constructed");
 now=TimeSpan.FromSeconds(10);ld.Spells[enemy.UniqueId]=now+TimeSpan.FromSeconds(6);var match=AccessTools.Method(typeof(PsychicBattlemind),"Match");Call<PsychicBattlemind>(right,x=>Check(ReferenceEquals(AccessTools.Method(typeof(PsychicBattlemind),"Partner").Invoke(x,null),left),"reciprocal pair resolved"));
 foreach(int t in new[]{10,15,16,17}){now=TimeSpan.FromSeconds(t);Call<PsychicBattlemind>(right,x=>Check((bool)match.Invoke(x,new object[]{ld.Spells,enemy})==(t<16),"one round marker boundary"));}
 rd.Pair="wrong";Call<PsychicBattlemind>(left,x=>Check(AccessTools.Method(typeof(PsychicBattlemind),"Partner").Invoke(x,null)==null,"foreign pair not shared"));rd.Pair=ld.Pair;rd.Partner=other;Call<PsychicBattlemind>(left,x=>Check(AccessTools.Method(typeof(PsychicBattlemind),"Partner").Invoke(x,null)==null,"nonreciprocal pair not shared"));rd.Partner=a;

 now=TimeSpan.FromSeconds(10);
 var cast=new MechanicsContext(b,enemy.Descriptor,Asset<BlueprintAbility>(c,"BattlemindLink"),null,new TargetWrapper(enemy));cast.SetParams(new AbilityParams{CasterLevel=12,DC=24});
 for(int tick=0;tick<3;tick++){now=TimeSpan.FromSeconds(tick==2?16:10);var save=new RuleSavingThrow(tick==1?other:enemy,SavingThrowType.Will,24){Reason=new Kingmaker.RuleSystem.RuleReason(cast)};Call<PsychicBattlemind>(right,x=>x.OnEventAboutToTrigger(save));Check(save.AllBonuses.Sum(x=>x.Value)==(tick==0?-2:0),"actual saving throw modifier target/expiry");}
 var unrelated=new RuleSavingThrow(enemy,SavingThrowType.Will,24);Call<PsychicBattlemind>(right,x=>x.OnEventAboutToTrigger(unrelated));Check(!unrelated.AllBonuses.Any(),"null-context save untouched");
 var markers=JsonConvert.DeserializeObject<Dictionary<string,TimeSpan>>(JsonConvert.SerializeObject(ld.Spells));Check(markers[enemy.UniqueId]==TimeSpan.FromSeconds(16),"absolute expiry roundtrips");
 left.Remove();Check(removedBuffs.Contains(right)&&rd.Pair==null&&ld.Pair==null,"ending either link cleans counterpart without recursion");
 foreach(var buff in new[]{ward,left,right})Check(buff.Components.Select(x=>x.SourceBlueprintComponentName).All(x=>!string.IsNullOrEmpty(x))&&buff.Components.Select(x=>x.SourceBlueprintComponentName).Distinct().Count()==buff.Components.Count,"native save component keys");
 Healing(c,a,b);
 }finally{h.UnpatchAll(h.Id);Set(Kingmaker.Game.Instance,"TimeController",oldTime);}
 PsychicExpansionTests.Inventory["new029Assertions"]=n;Console.WriteLine("PASS: "+n+" ward/link persistence, native lifecycle and optional eidolon checks.");return n;
 }
 static void Healing(BlueprintsCache c,UnitEntityData caster,UnitEntityData target){
 var assembly=typeof(PsychicEidolonTarget).Assembly;var integration=assembly.GetType("CallOfTheWild.Wrath.Prototype.PsychicEidolonHealing");var method=AccessTools.Method(integration,"Integrate");method.Invoke(null,new object[]{c});
 string[] tokens={"RejuvenateEidolonLesser","RejuvenateEidolon","RejuvenateEidolonGreater"};
 for(int i=0;i<3;i++){var a=Asset<BlueprintAbility>(c,tokens[i]);var action=a.GetComponent<Kingmaker.UnitLogic.Abilities.Components.AbilityEffectRunAction>().Actions.Actions.OfType<ContextActionHealTarget>().Single();Check(action.Value.DiceType==Kingmaker.RuleSystem.DiceType.D10&&action.Value.DiceCountValue.Value==1+2*i,"healing dice");var checker=a.GetComponent<PsychicEidolonTarget>();Check(!checker.IsTargetRestrictionPassed(caster,new TargetWrapper(target)),"ordinary creature excluded");Check(!checker.IsTargetRestrictionPassed(null,new TargetWrapper(target)),"null caster safely excluded");
 var ctx=new MechanicsContext(caster,target.Descriptor,a,null,new TargetWrapper(target));for(int cl=1;cl<=25;cl++){ctx.SetParams(new AbilityParams{CasterLevel=cl});ctx.Recalculate();Check(action.Value.BonusValue.Calculate(ctx)==Math.Min(cl,new[]{5,10,20}[i]),"level cap "+i+" "+cl);}}

 var master=caster.Ensure<UnitPartPetMaster>();var pet=target.Ensure<UnitPartPet>();Set(pet,"<Type>k__BackingField",(Kingmaker.Enums.PetType)604767);master.Pets.Add(new EntityPartRef<UnitEntityData,UnitPartPet>(target));
 var targetCheck=Asset<BlueprintAbility>(c,tokens[0]).GetComponent<PsychicEidolonTarget>();Check(targetCheck.IsTargetRestrictionPassed(caster,new TargetWrapper(target)),"actual owned eidolon accepted");Set(pet,"<Type>k__BackingField",(Kingmaker.Enums.PetType)0);Check(!targetCheck.IsTargetRestrictionPassed(caster,new TargetWrapper(target)),"ordinary pet excluded even when owned");Set(pet,"<Type>k__BackingField",(Kingmaker.Enums.PetType)604767);master.Pets.Clear();Check(!targetCheck.IsTargetRestrictionPassed(caster,new TargetWrapper(target)),"foreign eidolon excluded");
 var list=new Kingmaker.Blueprints.Classes.Spells.BlueprintSpellList {name="SummonerSpellbook.SpellList",SpellsByLevel=Enumerable.Range(0,7).Select(i=>new Kingmaker.Blueprints.Classes.Spells.SpellLevelList(i)).ToArray()};list.AssetGuid=BlueprintGuid.Parse("d1ae05197677491eb236e0aa97080da1");foreach(var l in list.SpellsByLevel)Set(l,"m_Spells",new List<BlueprintAbilityReference>());c.AddCachedBlueprint(list.AssetGuid,list);try{method.Invoke(null,new object[]{c});method.Invoke(null,new object[]{c});for(int i=0;i<3;i++)Check(list.SpellsByLevel[1+2*i].Spells.Count()==1&&list.SpellsByLevel[1+2*i].Spells.Single()==Asset<BlueprintAbility>(c,tokens[i]),"optional list once, correct level");}finally{c.RemoveCachedBlueprint(list.AssetGuid);}
 }
}

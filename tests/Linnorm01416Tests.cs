using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Security.Cryptography;
using HarmonyLib;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Kingmaker;
using Kingmaker.Controllers.Combat;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Prerequisites;
using Kingmaker.Blueprints.Classes.Selection;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.EntitySystem;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Enums;
using Kingmaker.Enums.Damage;
using Kingmaker.Items;
using Kingmaker.Items.Slots;
using Kingmaker.RuleSystem;
using Kingmaker.PubSubSystem;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components;
using Kingmaker.UnitLogic.ActivatableAbilities;
using Kingmaker.UnitLogic.Buffs;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Commands;
using Kingmaker.UnitLogic.Commands.Base;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Parts;
using Kingmaker.Utility;
using UnityEngine;
using TurnBased.Controllers;
using CallOfTheWild.Wrath.Prototype;

internal static class Linnorm01416Tests
{
 static readonly Type Module=typeof(LinnormStyleEffect).Assembly.GetType("CallOfTheWild.Wrath.Prototype.LinnormFeats");
 static readonly Type Opportunity=Module.Assembly.GetType("CallOfTheWild.Wrath.Prototype.LinnormOpportunity");
 static int n,submissions;static bool tb,legal=true,reach=true,allow=true;static TimeSpan now;static UnitEntityData enemy,other;
 static readonly List<UnitAttackOfOpportunity> submitted=new List<UnitAttackOfOpportunity>();
 static RuleAttackWithWeapon delivered;
 static IEnumerable<CodeInstruction> CaptureOpportunity(IEnumerable<CodeInstruction> instructions){
  foreach(var instruction in instructions){
   var method=instruction.operand as MethodInfo;
   if(method?.DeclaringType==typeof(Rulebook)&&method.Name=="Trigger"&&method.IsGenericMethod&&method.GetGenericArguments().SequenceEqual(new[]{typeof(RuleAttackWithWeapon)})&&method.GetParameters().Length==1){instruction.opcode=System.Reflection.Emit.OpCodes.Call;instruction.operand=AccessTools.Method(typeof(Linnorm01416Tests),nameof(DeliverOpportunity));}
   yield return instruction;
  }
 }
 static RuleAttackWithWeapon DeliverOpportunity(RuleAttackWithWeapon rule){delivered=rule;Check(rule.IsAttackOfOpportunity&&rule.Weapon.Blueprint.IsUnarmed,"actual native OnAction builds an unarmed opportunity rule");return rule;}
 static bool Marked(RuleAttackWithWeapon rule)=>(bool)AccessTools.Method(Opportunity,"IsReaction").Invoke(null,new object[]{rule});
 static object Get(object o,string f)=>(AccessTools.Field(o.GetType(),f)??AccessTools.Field(o.GetType(),"<"+f+">k__BackingField")).GetValue(o);
 static void Set(object o,string f,object v)=>(AccessTools.Field(o.GetType(),f)??AccessTools.Field(o.GetType(),"<"+f+">k__BackingField")).SetValue(o,v);
 static T Empty<T>()where T:class=>(T)FormatterServices.GetUninitializedObject(typeof(T));
 static void Check(bool b,string s){if(!b)throw new Exception("Linnorm01416: "+s);n++;}
 static object Helper(string name,params object[] args)=>AccessTools.Method(typeof(TeamworkFeatTests),name).Invoke(null,args);
 static UnitEntityData Unit(BlueprintsCache c,string id){var u=(UnitEntityData)Helper("Unit",c,id);Set(u.Descriptor,"Stats",new CharacterStats(u.Descriptor));Set(u.State,"Prone",new ProneState());u.Stats.Wisdom.BaseValue=18;u.Stats.Constitution.BaseValue=13;u.Stats.Strength.BaseValue=10;u.Stats.Dexterity.BaseValue=10;u.Stats.AC.BaseValue=10;return u;}
 static Feature Fact(UnitEntityData u,BlueprintFeature bp)=>(Feature)Helper("Fact",u,bp);
 static void Call(EntityFact f,Type t,string name,params object[] args){var runtime=f.Components.Single(x=>x.SourceBlueprintComponent.GetType()==t);using(runtime.RequestEventContext())AccessTools.Method(t,name,args.Select(x=>x.GetType()).ToArray()).Invoke(runtime.SourceBlueprintComponent,args);}
 static T Asset<T>(BlueprintsCache c,string token)where T:SimpleBlueprint=>Psychic034Tests.Asset<T>(c,token);
 static ItemEntityWeapon Weapon(UnitEntityData u,WeaponCategory kind,string id){var w=(ItemEntityWeapon)AccessTools.Method(typeof(WildShapeFeatTests),"Weapon").Invoke(null,new object[]{u,kind,false,PhysicalDamageForm.Bludgeoning,id});Set(w.Blueprint.Type,"m_IsUnarmed",kind==WeaponCategory.UnarmedStrike);w.Blueprint.Type.OnEnable();return w;}
 static void Equip(UnitEntityData u,ItemEntityWeapon w)=>AccessTools.Method(typeof(WildShapeFeatTests),"Equip").Invoke(null,new object[]{u.Body.PrimaryHand,w});
 static RuleAttackWithWeapon Attack(UnitEntityData u,UnitEntityData t,ItemEntityWeapon w,bool miss=false)=>(RuleAttackWithWeapon)Helper("Attack",u,t,w,false,miss);
 static bool Yes(ref bool __result){__result=true;return false;}
 static bool Skip()=>false;
 static bool Deactivate(EntityFact __instance){if(!(__instance is Buff))return true;Set(__instance,"IsActive",false);return false;}
 static bool Activate(EntityFact __instance){if(!(__instance is Buff))return true;Set(__instance,"IsActive",true);return false;}
 static bool Mode(ref bool __result){__result=tb;return false;}
 static bool Time(ref TimeSpan __result){__result=now;return false;}
 static bool Hit(RuleAttackRoll __instance,ref bool __result){__result=!__instance.AutoMiss;return false;}
 static bool Ally(UnitEntityData __instance,UnitEntityData __0,ref bool __result){__result=(__instance==enemy||__instance==other)==(__0==enemy||__0==other);return false;}
 static bool Legal(ref bool __result){__result=legal;return false;}
 static bool Reach(ref bool __result){__result=reach;return false;}
 static bool Hand(UnitEntityData __0,ref WeaponSlot __result){__result=__0.Body.PrimaryHand;return false;}
 static bool Range(ref float? __result){__result=1.524f;return false;}
 static bool Aoo(UnitCombatState __instance,UnitEntityData __0,ref bool __result){submissions++;__result=allow;if(!allow)return false;var command=new UnitAttackOfOpportunity(__0);Set(command,"Executor",__instance.Unit);Set(command,"Hand",__instance.Unit.Body.PrimaryHand);__instance.Unit.Commands.Queue.AddLast(command);submitted.Add(command);return false;}
 static int Bonus(Buff b,UnitEntityData u,UnitEntityData t,ItemEntityWeapon w){var r=new RuleCalculateAttackBonus(u,t,w,0);Call(b,typeof(LinnormStyleEffect),"OnEventAboutToTrigger",r);return r.AllBonuses.Sum(x=>x.Value);}
 static int Damage(Buff b,UnitEntityData u,UnitEntityData t,ItemEntityWeapon w,StatType? stat,bool secondary=false){
  var calculation=(RuleCalculateAttackBonusWithoutTarget)AccessTools.Method(typeof(FeralGraceTests),"Calculation").Invoke(null,new object[]{u,w,stat,secondary});
  var roll=Empty<RuleAttackRoll>();Set(roll,"Initiator",u);Set(roll,"Target",t);Set(roll,"WeaponStats",calculation.WeaponStats);
  int before=roll.WeaponStats.DamageDescription[0].CreateDamage().Bonus;Call(b,typeof(LinnormStyleEffect),"OnEventAboutToTrigger",roll);int result=roll.WeaponStats.DamageDescription[0].CreateDamage().Bonus-before;Call(b,typeof(LinnormStyleEffect),"OnEventAboutToTrigger",roll);Check(result==roll.WeaponStats.DamageDescription[0].CreateDamage().Bonus-before,"same calculated strike cannot apply replacement twice");return result;
 }
 static Buff Transmutation(UnitEntityData u,int cl,AbilityType type=AbilityType.Spell,SpellSchool school=SpellSchool.Transmutation){
  var spell=new BlueprintAbility {name="01416Transmutation",Type=type,ComponentsArray=new BlueprintComponent[]{new SpellComponent{School=school}}};spell.OnEnable();
  var bp=new BlueprintBuff{name="01416Effect",AssetGuid=BlueprintGuid.Parse(Guid.NewGuid().ToString("N")),ComponentsArray=Array.Empty<BlueprintComponent>()};bp.OnEnable();
  var b=Psychic034Tests.Fact(u,bp,u,cl);Set(b,"m_Context",new AbilityExecutionContext(new AbilityData(spell,u.Descriptor),new AbilityParams{CasterLevel=cl},new TargetWrapper(u)));return b;
 }
 static EntityFactComponent Runtime(EntityFact f,Type t)=>f.Components.Single(x=>x.SourceBlueprintComponent.GetType()==t);
 internal static int Run(BlueprintsCache c)
 {
  n=submissions=0;submitted.Clear();tb=true;now=TimeSpan.FromSeconds(100);var h=new Harmony("CotW01416.NativeFeats");var saved=Game.Instance.TimeController;Set(Game.Instance,"TimeController",Empty<Kingmaker.Controllers.TimeController>());
  void Patch(MethodBase m,string name)=>h.Patch(m,prefix:new HarmonyMethod(typeof(Linnorm01416Tests),name));
  Patch(AccessTools.Method(typeof(CombatController),"IsInTurnBasedCombat"),nameof(Mode));Patch(AccessTools.PropertyGetter(typeof(Kingmaker.Controllers.TimeController),"GameTime"),nameof(Time));
  Patch(AccessTools.Method(typeof(UnitEntityData),"IsAlly",new[]{typeof(UnitEntityData)}),nameof(Ally));Patch(AccessTools.Method(typeof(UnitEntityData),"CanAttack",new[]{typeof(UnitEntityData)}),nameof(Legal));
  Patch(AccessTools.PropertyGetter(typeof(UnitState),"CanAct"),nameof(Yes));Patch(AccessTools.PropertyGetter(typeof(RuleAttackRoll),"IsHit"),nameof(Hit));
  Patch(AccessTools.Method(typeof(UnitHelper),"GetThreatHand"),nameof(Hand));Patch(AccessTools.Method(typeof(UnitHelper),"GetThreatRange"),nameof(Range));Patch(AccessTools.Method(typeof(UnitHelper),"IsAttackOfOpportunityReach"),nameof(Reach));
  Patch(AccessTools.PropertyGetter(typeof(UnitCombatState),"CanAttackOfOpportunity"),nameof(Yes));Patch(AccessTools.Method(typeof(UnitCombatState),"AttackOfOpportunity"),nameof(Aoo));
  void FeralPatch(MethodBase m,string name)=>h.Patch(m,prefix:new HarmonyMethod(typeof(FeralGraceTests),name));
  FeralPatch(AccessTools.PropertyGetter(typeof(ItemEntityWeapon),"HoldInTwoHands"),"False");FeralPatch(AccessTools.PropertyGetter(typeof(ItemEntity),"EnchantmentValue"),"Zero");FeralPatch(AccessTools.PropertyGetter(typeof(ItemEntity),"Wielder"),"Wielder");FeralPatch(AccessTools.Method(typeof(RuleCalculateWeaponSizeBonus),"OnTrigger"),"Skip");
  try {
   AccessTools.Method(Module,"Register").Invoke(null,new object[]{c});var tokens=(string[])AccessTools.Field(Module,"Tokens").GetValue(null);var feats=tokens.Select(t=>Asset<BlueprintFeature>(c,t)).ToArray();AccessTools.Method(Module,"Register").Invoke(null,new object[]{c});
   for(int i=0;i<5;i++){Check(ReferenceEquals(c.Load(feats[i].AssetGuid),feats[i]),"repeat registration preserves each feat");Check(feats[i].HasGroup(FeatureGroup.Feat)&&feats[i].HasGroup(FeatureGroup.CombatFeat)==(i!=3)&&feats[i].HasGroup(FeatureGroup.StyleFeat)==(i<3),"five distinct roots correctly grouped");Check(feats[i].ComponentsArray.Select(x=>x.name).Distinct().Count()==feats[i].ComponentsArray.Length,"unique save component names");}
   foreach(var pool in ((string[])AccessTools.Field(Module.Assembly.GetType("CallOfTheWild.Wrath.Prototype.CombatFeatPoolRepair"),"NativeIds").GetValue(null)).Select(id=>c.Load(BlueprintGuid.Parse(id))as BlueprintFeatureSelection).Where(x=>x!=null))foreach(string field in new[]{"m_Features","m_AllFeatures"})foreach(var feat in feats)Check(((BlueprintFeatureReference[])Get(pool,field)).Count(x=>x.Guid==feat.AssetGuid)==(pool.AssetGuid.ToString()=="247a4068296e8be42890143f451b4b45"||feat!=feats[3]?1:0),"general and combat pools each contain eligible root once");
   var iconHashes=new[]{"E96B13AEE6AB7CC0313E92F0F0D36D53F0384465306487579176F5C22D076C66","2383F2126F8363FEC176032A7C579F74FFF94CEF283085A2460EACB95BE82D22","1F5437F5CC098663CEF4C42A9D6284B3AB3D9A87637322F68DDCA00F829420FE","1BCC31046A3958EFE3B779979C67F0AC627236E6BB43FDCA42C96DD7ED3B7346"};int iconIndex=0;
   foreach(var icon in new[]{"LinnormStyle","LinnormVengeance","LinnormWrath","ViciousStomp"})using(var stream=Module.Assembly.GetManifestResourceStream("CotW."+icon+".png")){Check(stream!=null,"original icon embedded");byte[] header=new byte[24];stream.Read(header,0,24);Check(header[0]==137&&header[1]==80&&header[19]==64&&header[23]==64,"original PNG is 64x64");stream.Position=0;using(var hash=SHA256.Create())Check(BitConverter.ToString(hash.ComputeHash(stream)).Replace("-","")==iconHashes[iconIndex++],"original CotW PNG bytes retained");}
   Check(Asset<BlueprintActivatableAbility>(c,"LinnormStyleToggle").Group==ActivatableAbilityGroup.CombatStyle,"native exclusive combat-style group");
   var u=Unit(c,"01416-first");var ally=Unit(c,"01416-second");enemy=Unit(c,"01416-enemy");other=Unit(c,"01416-other");var fist=Weapon(u,WeaponCategory.UnarmedStrike,"01416-fist");var sword=Weapon(enemy,WeaponCategory.Longsword,"01416-sword");var bow=Weapon(enemy,WeaponCategory.Longbow,"01416-bow");Set(bow.Blueprint.Type,"m_AttackType",AttackType.Ranged);bow.Blueprint.Type.OnEnable();Equip(u,fist);Equip(ally,Weapon(ally,WeaponCategory.UnarmedStrike,"01416-second-fist"));
   Fact(u,feats[0]);Fact(ally,feats[0]);var mode=Asset<BlueprintBuff>(c,"LinnormStyleMode");var stance=Psychic034Tests.Fact(u,mode,u,1);var stance2=Psychic034Tests.Fact(ally,mode,ally,1);
   foreach(int index in new[]{0,1,2}){var p=feats[index].GetComponents<PrerequisiteStatValue>().Single(x=>x.Stat==StatType.BaseAttackBonus);u.Stats.BaseAttackBonus.BaseValue=p.Value-1;Check(!(bool)Helper("Need",p,u),"BAB below each chain tier rejected");u.Stats.BaseAttackBonus.BaseValue=p.Value;Check((bool)Helper("Need",p,u),"BAB at chain tier accepted");}
   var qualifying=Unit(c,"01416-prerequisites");
   foreach(int index in new[]{0,1,2,3}){var p=feats[index].GetComponents<PrerequisiteStatValue>().Single(x=>x.Stat==(index==3?StatType.Constitution:StatType.Wisdom));qualifying.Stats.GetStat(p.Stat).BaseValue=12;Check(!(bool)Helper("Need",p,qualifying),"ability below 13 rejected");qualifying.Stats.GetStat(p.Stat).BaseValue=13;Check((bool)Helper("Need",p,qualifying),"ability 13 accepted");}
   foreach(var p in feats.SelectMany(f=>f.GetComponents<PrerequisiteFeature>()).GroupBy(x=>x.Feature.AssetGuid).Select(x=>x.First())){Check(!(bool)Helper("Need",p,qualifying),"missing prerequisite feat rejected");Fact(qualifying,p.Feature);Check((bool)Helper("Need",p,qualifying),"existing prerequisite feat accepted");}
   foreach(AttackType attackType in Enum.GetValues(typeof(AttackType))){var ac=new RuleCalculateAC(enemy,u,attackType);Call(stance,typeof(LinnormStyleEffect),"OnEventAboutToTrigger",ac);Check(ac.AllBonuses.Sum(x=>x.Value)==(attackType==AttackType.Melee||attackType==AttackType.Touch?-2:0),"AC penalty only melee and melee touch");}
   var miss=Attack(enemy,u,sword,true);Call(stance,typeof(LinnormStyleEffect),"OnEventDidTrigger",miss);Check(Bonus(stance,u,enemy,fist)==0,"miss opens Wisdom but no Vengeance");
   Check(Damage(stance,u,enemy,fist,StatType.Strength)==4,"completed native Strength damage replaced by Wisdom");u.Stats.Strength.BaseValue=22;Check(Damage(stance,u,enemy,fist,StatType.Strength)==0,"higher Strength retained");u.Stats.Strength.BaseValue=10;u.Stats.Dexterity.BaseValue=14;Check(Damage(stance,u,enemy,fist,StatType.Dexterity)==2,"actual Dexterity damage replaced only by higher Wisdom");u.Stats.Dexterity.BaseValue=20;Check(Damage(stance,u,enemy,fist,StatType.Dexterity)==0,"higher Dexterity retained");Check(Damage(stance,u,other,fist,StatType.Strength)==0&&Damage(stance,u,enemy,sword,StatType.Strength)==0,"target and unarmed weapon restriction");
   Fact(u,feats[1]);Fact(ally,feats[1]);var hit=Attack(enemy,u,sword);Call(stance,typeof(LinnormStyleEffect),"OnEventDidTrigger",hit);Call(stance,typeof(LinnormStyleEffect),"OnEventDidTrigger",hit);Check(Bonus(stance,u,enemy,fist)==2&&Bonus(stance,u,other,fist)==0&&Bonus(stance,u,enemy,sword)==0,"Vengeance +2 isolated by enemy and unarmed, no stacking");Check(Bonus(stance2,ally,enemy,ally.Body.PrimaryHand.Weapon)==0,"other owner has no opening");
   Call(stance,typeof(LinnormStyleEffect),"HandleTurnStarted",other);Check(Bonus(stance,u,enemy,fist)==2,"unrelated turn does not expire opening");Call(stance,typeof(LinnormStyleEffect),"HandleTurnStarted",enemy);Check(Bonus(stance,u,enemy,fist)==0,"target's next turn expires opening");
   var wrath=Fact(u,feats[2]);Fact(ally,feats[2]);var data=Runtime(wrath,typeof(LinnormWrathAllowance)).GetData<LinnormWrathData>();
   submissions=0;allow=false;Call(stance,typeof(LinnormStyleEffect),"OnEventDidTrigger",hit);Check(submissions==1&&!data.Used,"native rejected AoO does not consume Wrath");allow=true;Call(stance,typeof(LinnormStyleEffect),"OnEventDidTrigger",hit);Check(submissions==2&&data.Used&&submitted.Last().Hand.Weapon.Blueprint.IsUnarmed,"successful native submission consumes Wrath and uses actual fist hand");Call(stance,typeof(LinnormStyleEffect),"OnEventDidTrigger",hit);Check(submissions==2,"once per own round");
   Call(stance,typeof(LinnormStyleEffect),"OnTurnOff");Set(stance,"IsTurnedOn",false);var reopened=Psychic034Tests.Fact(u,mode,u,1);Call(reopened,typeof(LinnormStyleEffect),"OnEventDidTrigger",hit);Check(submissions==2,"off/on stance cannot refund persisted feat allowance");Call(wrath,typeof(LinnormWrathAllowance),"HandleTurnStarted",ally);Check(data.Used,"other turn does not refund");Call(wrath,typeof(LinnormWrathAllowance),"HandleTurnStarted",u);Check(!data.Used,"own turn refreshes allowance");
   reach=false;Call(reopened,typeof(LinnormStyleEffect),"OnEventDidTrigger",hit);Check(!data.Used&&submissions==2,"out of reach no payment");reach=true;legal=false;Call(reopened,typeof(LinnormStyleEffect),"OnEventDidTrigger",hit);Check(!data.Used&&submissions==2,"illegal target no payment");legal=true;
   tb=false;Call(reopened,typeof(LinnormStyleEffect),"OnEventDidTrigger",hit);int before=submissions;now+=TimeSpan.FromSeconds(5);Call(reopened,typeof(LinnormStyleEffect),"OnEventDidTrigger",hit);Check(submissions==before,"real-time sixth second not reached");now+=TimeSpan.FromSeconds(1);Call(reopened,typeof(LinnormStyleEffect),"OnEventDidTrigger",hit);Check(submissions==before+1,"real-time six seconds refresh");now+=TimeSpan.FromSeconds(6);Check(Bonus(reopened,u,enemy,fist)==0,"real-time opening expires at six seconds");
   var restored=JsonConvert.DeserializeObject<LinnormWrathData>(JsonConvert.SerializeObject(data));Check(restored.Used&&restored.Expires==data.Expires,"persist paid allowance and exact expiry");var openingData=Runtime(reopened,typeof(LinnormStyleEffect)).GetData<LinnormData>();Call(reopened,typeof(LinnormStyleEffect),"OnEventDidTrigger",hit);var savedOpenings=JsonConvert.DeserializeObject<LinnormData>(JsonConvert.SerializeObject(openingData));Check(savedOpenings.Openings.Single().Enemy.UniqueId==enemy.UniqueId&&savedOpenings.Openings[0].Hit,"persist independent target/hit window");
   tb=true;var subscribers=new RulebookSubscriptionManager<ITargetRulebookSubscriber>();
   foreach(var b in new[]{reopened,stance2}){var runtime=Runtime(b,typeof(LinnormStyleEffect));subscribers.Subscribe((ITargetRulebookSubscriber)runtime.SourceBlueprintComponent,(ISubscriptionProxy)runtime);}
   var wrath2=ally.Facts.List.Single(x=>x.Blueprint==feats[2]);var data2=Runtime(wrath2,typeof(LinnormWrathAllowance)).GetData<LinnormWrathData>();
   for(int round=0;round<2;round++){
    u.Commands.Queue.Clear();ally.Commands.Queue.Clear();Call(wrath,typeof(LinnormWrathAllowance),"HandleTurnStarted",u);Call(wrath2,typeof(LinnormWrathAllowance),"HandleTurnStarted",ally);before=submissions;
    subscribers.OnEventDidTrigger(hit);Check(submissions==before+1&&data.Used&&!data2.Used,"native shared subscription pays only first defender");
    subscribers.OnEventDidTrigger(Attack(enemy,ally,sword));Check(submissions==before+2&&data.Used&&data2.Used,"second defender receives independent native-dispatch reaction");
    subscribers.OnEventDidTrigger(hit);subscribers.OnEventDidTrigger(Attack(enemy,ally,sword));Check(submissions==before+2,"each defender preserves own once-per-round payment");
   }
   h.Patch(AccessTools.Method(typeof(UnitAttackOfOpportunity),"OnAction"),transpiler:new HarmonyMethod(typeof(Linnorm01416Tests),nameof(CaptureOpportunity)));
   var generated=(UnitAttackOfOpportunity)u.Commands.Queue.Single();u.Commands.Queue.Remove(generated);u.Commands.Raw[(int)UnitCommand.CommandType.Free]=generated;delivered=null;
   AccessTools.Method(typeof(UnitAttackOfOpportunity),"OnAction").Invoke(generated,null);Check(delivered!=null&&delivered.Initiator==u&&delivered.Target==enemy&&Marked(delivered),"queued generated command retains exact reaction marker after promotion and native execution");
   u.Commands.Raw[(int)UnitCommand.CommandType.Free]=null;Check(Marked(delivered),"constructed reaction remains marked after native command slot is cleared");
   var ordinary=new UnitAttackOfOpportunity(enemy);Set(ordinary,"Executor",u);Set(ordinary,"Hand",u.Body.PrimaryHand);u.Commands.Raw[(int)UnitCommand.CommandType.Free]=ordinary;
   AccessTools.Method(typeof(UnitAttackOfOpportunity),"OnAction").Invoke(ordinary,null);Check(!Marked(delivered),"later ordinary native opportunity is never captured by prior reaction marker");u.Commands.Raw[(int)UnitCommand.CommandType.Free]=null;
   var stomp=Fact(u,feats[4]);enemy.State.Prone.ShouldBeActive=false;before=submissions;Call(stomp,typeof(ViciousStompReaction),"HandleKnockOff",other,enemy);Check(submissions==before,"unsuccessful knockdown rejected");enemy.State.Prone.ShouldBeActive=true;Call(stomp,typeof(ViciousStompReaction),"HandleKnockOff",other,enemy);Check(submissions==before+1,"successful adjacent prone opening submits native unarmed AoO");Equip(u,sword);Call(stomp,typeof(ViciousStompReaction),"HandleKnockOff",other,enemy);Check(submissions==before+1,"stomp never submits sword attack");Equip(u,fist);reach=false;Call(stomp,typeof(ViciousStompReaction),"HandleKnockOff",other,enemy);Check(submissions==before+1,"nonadjacent stomp rejected");reach=true;
   u.Stats.Dexterity.BaseValue=10;var scales=Fact(u,feats[3]);var component=typeof(ScalesAndSkinEffect);Call(scales,component,"OnTurnOn");Check(u.Stats.AC.ModifiedValue==10,"no ongoing transmutation no armor");var low=Transmutation(u,9);Call(scales,component,"HandleBuffDidAdded",low);Check(u.Stats.AC.ModifiedValue==11,"CL9 +1 armor");var high=Transmutation(u,10);Call(scales,component,"HandleBuffDidAdded",high);Check(u.Stats.AC.ModifiedValue==12,"CL10 +2 armor, strongest only");var modifier=u.Stats.AC.Modifiers.Single(m=>m.Source==scales);for(int i=0;i<20;i++)Call(scales,component,"HandleBuffDidAdded",low);Check(ReferenceEquals(modifier,u.Stats.AC.Modifiers.Single(m=>m.Source==scales)),"unchanged bonus keeps exact modifier");
   var armor=u.Stats.AC.AddModifier(4,(EntityFact)null,ModifierDescriptor.NaturalArmor);var bark=u.Stats.AC.AddModifier(3,(EntityFact)null,ModifierDescriptor.NaturalArmorEnhancement);Check(u.Stats.AC.ModifiedValue==19,"increase stacks with native natural armor and enhancement");Check(u.Stats.AC.Touch==10&&u.Stats.AC.FlatFooted==19,"natural armor excludes touch and remains flat-footed");u.Stats.AC.RemoveModifier(armor);u.Stats.AC.RemoveModifier(bark);
   h.CreateClassProcessor(Module.Assembly.GetType("CallOfTheWild.Wrath.Prototype.ScalesAndSkinSuppressionPatch")).Patch();Patch(AccessTools.Method(typeof(EntityFact),"Deactivate"),nameof(Deactivate));Patch(AccessTools.Method(typeof(EntityFact),"Activate"),nameof(Activate));
   var suppress=new UnitPartBuffSuppress();Set(suppress,"Owner",u);suppress.PrepareForSuppression(high.Blueprint);suppress.ApplyChanges();Check(u.Stats.AC.ModifiedValue==11,"native suppression ApplyChanges immediately falls back to low CL");suppress.PrepareForSuppression(low.Blueprint);suppress.ApplyChanges();Check(u.Stats.AC.ModifiedValue==10,"suppress all eligible effects removes bonus");suppress.PrepareForRelease(high.Blueprint);suppress.ApplyChanges();Check(u.Stats.AC.ModifiedValue==12,"unsuppression restores bonus immediately");
   Set(high,"IsTurnedOn",false);Call(scales,component,"HandleBuffDidRemoved",high);Check(u.Stats.AC.ModifiedValue==10,"inactive buff not counted even while collection reference remains");foreach(var rejected in new[]{Transmutation(u,20,AbilityType.Supernatural),Transmutation(u,20,AbilityType.Extraordinary),Transmutation(u,20,AbilityType.Spell,SpellSchool.Abjuration)}){Call(scales,component,"HandleBuffDidAdded",rejected);Check(u.Stats.AC.ModifiedValue==10,"nonspell or other school does not qualify");}
   var like=Transmutation(u,10,AbilityType.SpellLike);Call(scales,component,"HandleBuffDidAdded",like);Check(u.Stats.AC.ModifiedValue==12,"transmutation spell-like ability qualifies");u.Stats.AC.AddModifier(99,scales,ModifierDescriptor.NaturalArmor);Call(scales,component,"OnApplyPostLoadFixes");Check(u.Stats.AC.Modifiers.Count(m=>m.Source==scales)==1&&u.Stats.AC.ModifiedValue==12,"load removes stale own modifiers and rebuilds once");Call(scales,component,"OnTurnOff");Check(u.Stats.AC.ModifiedValue==10,"feat removal clears own contribution");
   PsychicExpansionTests.Inventory["linnorm01416Assertions"]=n;PsychicExpansionTests.Inventory["linnorm01416Assets"]=new JArray(feats.Select(f=>new JObject{["name"]=f.name,["guid"]=f.AssetGuid.ToString()}));Console.WriteLine("PASS: "+n+" batch 1/4 feat registration, native completed damage, target expiry, stance payment persistence, unarmed AoO admission, natural armor and suppression checks.");return n;
  } finally {h.UnpatchAll(h.Id);Set(Game.Instance,"TimeController",saved);enemy=other=null;submitted.Clear();}
 }
}

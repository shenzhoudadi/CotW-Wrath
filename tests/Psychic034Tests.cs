using System;
using System.Linq;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;
using HarmonyLib;
using Kingmaker;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.Blueprints.Items;
using Kingmaker.Blueprints.Items.Weapons;
using Kingmaker.Blueprints.Root.Strings.GameLog;
using Kingmaker.EntitySystem;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Enums;
using Kingmaker.Items;
using Kingmaker.Items.Slots;
using Kingmaker.RuleSystem;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.RuleSystem.Rules.Damage;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Buffs;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.Utility;
using CallOfTheWild.Wrath.Prototype;

// Real native weapon-damage and attack-bonus calculations. Equipment insertion,
// medium-size dice adjustment, scene selection and final combat are explicit
// boundaries. This does not pretend to render a Unity scene or reload a save.
internal static class Psychic034Tests {
 static int n;static readonly List<BlueprintItem> inserted=new List<BlueprintItem>();
 static object Get(object o,string f)=>AccessTools.Field(o.GetType(),f).GetValue(o);
 static void Set(object o,string f,object v)=>AccessTools.Field(o.GetType(),f).SetValue(o,v);
 static T Empty<T>()where T:class=>(T)FormatterServices.GetUninitializedObject(typeof(T));
 static void Check(bool b,string m){if(!b)throw new Exception("034: "+m);n++;}
 internal static T Asset<T>(BlueprintsCache c,string t)where T:SimpleBlueprint=>(T)c.Load((BlueprintGuid)AccessTools.Method(typeof(PsychicSpiritBind).Assembly.GetType("CallOfTheWild.Wrath.Prototype.PsychicAssets"),"Id").Invoke(null,new object[]{t}));
 internal static Psychic070Tests.Caster Unit(BlueprintsCache c,string id){
  var f=new Psychic070Tests.Caster(c);Set(f.Unit,"<UniqueId>k__BackingField",id);
  var facts=new EntityFactsManager(f.Unit);Set(f.Unit,"Facts",facts);var bc=new BuffCollection();bc.Initialize(facts);Set(f.Unit,"<Buffs>k__BackingField",bc);
  foreach(var s in new[]{StatType.Strength,StatType.Dexterity,StatType.Intelligence,StatType.Charisma})Set(f.Unit.Stats,s.ToString(),new ModifiableValueAttributeStat(f.Unit.Stats,s){BaseValue=10});
  foreach(var s in new[]{StatType.BaseAttackBonus,StatType.AdditionalAttackBonus,StatType.AdditionalDamage,StatType.SneakAttack})Set(f.Unit.Stats,s.ToString(),new ModifiableValue(f.Unit.Stats,s));
  foreach(var type in new[]{StatType.Strength,StatType.Dexterity,StatType.Intelligence,StatType.Charisma,StatType.BaseAttackBonus,StatType.AdditionalAttackBonus,StatType.AdditionalDamage,StatType.SneakAttack})f.Unit.Stats.AllStats.Add((ModifiableValue)Get(f.Unit.Stats,type.ToString()));
  return f;
 }
 internal static Buff Fact(UnitEntityData owner,BlueprintBuff bp,UnitEntityData caster,int cl){
  var b=Empty<Buff>();Set(b,"Components",new List<EntityFactComponent>());b.RestoreOwnerLink(owner.Facts);
  var ctx=new MechanicsContext(caster,owner.Descriptor,bp,null,new TargetWrapper(owner));ctx.SetParams(new AbilityParams{CasterLevel=cl});
  Set(b,"m_Context",ctx);Set(b,"<Rank>k__BackingField",1);AccessTools.Method(typeof(EntityFact),"Setup").Invoke(b,new object[]{bp});Set(b,"<IsActive>k__BackingField",true);Set(b,"<IsTurnedOn>k__BackingField",true);
  owner.Facts.List.Add(b);AccessTools.Method(Get(owner.Facts,"m_Cache").GetType(),"Clear").Invoke(Get(owner.Facts,"m_Cache"),null);return b;
 }
 static bool No()=>false;
 static bool False(ref bool __result){__result=false;return false;}
 static bool Zero(ref int __result){__result=0;return false;}
 static bool Insert(BlueprintItem bpItem,ItemSlot slot){if(bpItem!=null)inserted.Add(bpItem);return false;}
 static UnitDescriptor wielder;
 static bool Wielder(ref UnitDescriptor __result){__result=wielder;return false;}
 static bool LogBody(StringBuilder sb,RuleCalculateAttackBonus rule){sb.Append("Native log boundary\n");return false;}
 static bool AttackCapture(RuleAttackWithWeapon __0,ref RuleAttackWithWeapon __result){
  Check(__0.Weapon!=null,"production strike has an actual equipped weapon");
  Check(__0.AttackBonusPenalty==__0.AttackNumber*5,"actual strike passes a positive native iterative penalty");__result=__0;return false;
 }
 internal static int Run(BlueprintsCache cache){
  var h=new Harmony("CotW034.NativeWeaponCalculation");
  void Patch(System.Reflection.MethodBase m,string method)=>h.Patch(m,prefix:new HarmonyMethod(typeof(Psychic034Tests),method));
  Patch(AccessTools.PropertyGetter(typeof(ItemEntityWeapon),"HoldInTwoHands"),nameof(False));
  Patch(AccessTools.PropertyGetter(typeof(ItemEntity),"EnchantmentValue"),nameof(Zero));
  Patch(AccessTools.PropertyGetter(typeof(ItemEntity),"Wielder"),nameof(Wielder));
  Patch(AccessTools.Method(typeof(RuleCalculateWeaponSizeBonus),"OnTrigger"),nameof(No));
  Patch(AccessTools.Method(typeof(UnitBody),"TryInsertItem"),nameof(Insert));
  Patch(AccessTools.Method(typeof(ItemSlot),"UpdateActive"),nameof(No));
  var patchType=typeof(PsychicSpiritBind).Assembly.GetType("CallOfTheWild.Wrath.Prototype.PsychicSpiritAttackLogPatch");
  h.CreateClassProcessor(patchType).Patch();
  Patch(AccessTools.Method(typeof(AttackLogMessage),"AppendAttackBonusBreakdown"),nameof(LogBody));
  PsychicRuleTestBoundary.Patch(h,typeof(RuleAttackWithWeapon),AccessTools.Method(typeof(Psychic034Tests),nameof(AttackCapture)));
  try{
   foreach(int kind in new[]{0,1,2,3}){
    string token=new[]{"SpiritualWeapon","SpiritualAlly","MagesSword","TwilightKnife"}[kind];
    var f=Unit(cache,"034-owner-"+kind);var actor=Unit(cache,"034-actor-"+kind);var target=Unit(cache,"034-target-"+kind);
    var bp=Asset<BlueprintUnit>(cache,token+"Unit");Set(actor.Descriptor,"<Blueprint>k__BackingField",bp);Set(actor.Descriptor,"<OriginalBlueprint>k__BackingField",bp);
    Check(!bp.Body.DisableHands&&bp.Body.ActiveHandSet==0,"summon has enabled active weapon hands "+token);
    var body=new UnitBody(actor.Descriptor);Set(actor.Descriptor,"<Body>k__BackingField",body);
    inserted.Clear();AccessTools.Method(typeof(UnitBody),"InitializeHands").Invoke(body,new object[]{bp.Body});
    Check(inserted.Count==1&&inserted[0]==Asset<BlueprintItemWeapon>(cache,token+"Item"),"native hand initialization inserts the spell weapon "+token);
    if(kind==1){
     // Reproduce the old ghost template without editing the production donor.
     Set(body,"m_HandsAreEnabled",false);inserted.Clear();AccessTools.Method(typeof(UnitBody),"InitializeHands").Invoke(body,new object[]{bp.Body});
     Check(inserted.Count==0,"old ghost disabled-hands configuration reproduces missing weapon");Set(body,"m_HandsAreEnabled",true);
    }
    var weapon=Empty<ItemEntityWeapon>();Set(weapon,"m_Blueprint",Asset<BlueprintItemWeapon>(cache,token+"Item"));wielder=actor.Descriptor;
    Set(body.PrimaryHand,"m_FakeItem",weapon);Set(body.PrimaryHand,"m_IsItemFake",true);Set(weapon,"<HoldingSlot>k__BackingField",body.PrimaryHand);
    var host=Fact(f.Unit,Asset<BlueprintBuff>(cache,token+"Controller"),f.Unit,18);var rt=host.Components.Single(x=>x.SourceBlueprintComponent is PsychicSpiritController);var data=rt.GetData<PsychicSpiritState>();data.Summon=actor.Unit;data.Target=target.Unit;data.Bab=16;data.Level=18;data.Mental=7;
    var buff=Fact(actor.Unit,Asset<BlueprintBuff>(cache,token+"ActorBuff"),f.Unit,18);var art=buff.Components.Single(x=>x.SourceBlueprintComponent is PsychicSpiritAttackStats);var comp=(PsychicSpiritAttackStats)art.SourceBlueprintComponent;
    Check(buff.Blueprint.FxOnStart.AssetId=="fdc7f8f37d3f8da42be2a1d35a617001","native persistent glow is owned by actor buff "+token);
    if(kind<3){
     actor.Unit.Stats.Strength.BaseValue=10;
     var oldStats=new RuleCalculateWeaponStats(actor.Unit,weapon){OverrideDamageBonus=kind==2?3:5};oldStats.OnTrigger(null);
     Check(oldStats.DamageDescription[0].CreateDamage().Bonus==0,"old 033 multiplier reproduces missing flat damage on native calculation "+token);
    }
    foreach(int cl in new[]{1,2,3,6,12,15,20,25,40})foreach(int strength in new[]{6,10,26}){
     actor.Unit.Stats.Strength.BaseValue=strength;buff.Context.SetParams(new AbilityParams{CasterLevel=cl});
     var stats=new RuleCalculateWeaponStats(actor.Unit,weapon);
     using(art.RequestEventContext())comp.OnEventAboutToTrigger(stats);
     stats.OnTrigger(null);
     int expected=kind==2?3:kind==3?0:Math.Min(5,cl/3);
     var damage=stats.DamageDescription[0].CreateDamage();
     Check(damage.Bonus==expected,"native final force damage contains exact flat bonus independent of Strength "+token+" CL"+cl);
     Check(stats.WeaponDamageDice.ModifiedValue.Rolls==(kind==2?4:1),"native base damage dice retained "+token);
    }
    actor.Unit.Stats.Strength.BaseValue=10;actor.Unit.Stats.BaseAttackBonus.BaseValue=kind==2?18:16;
    for(int index=0;index<4;index++){
     actor.Unit.Stats.AdditionalAttackBonus.BaseValue=kind==2?10:7;
     var ws=new RuleCalculateWeaponStats(actor.Unit,weapon);Set(ws,"m_Triggered",true);
     var outer=new RuleCalculateAttackBonus(actor.Unit,target.Unit,ws,index*5);var rule=(RuleCalculateAttackBonusWithoutTarget)Get(outer,"m_InnerRule");
     using(art.RequestEventContext())comp.OnEventAboutToTrigger(rule);
     rule.OnTrigger(null);int expected=(kind==2?18:16)+7+(kind==2?3:0)-index*5;
     Check(rule.Result==expected,"actual native AB includes mental/plus3 and subtracts iterative penalty "+token);
     Check(actor.Unit.Stats.AdditionalAttackBonus.BaseValue==0,"old saved anonymous bonus removed without double counting");
     Check(rule.AllBonuses.Any(b=>b.Fact==buff&&b.Value==7),"mental bonus is an attributed native modifier");
     Set(outer,"<Result>k__BackingField",rule.Result);
     var sb=new StringBuilder();new AttackLogMessage().AppendAttackBonusBreakdown(sb,outer);string details=sb.ToString();
     Check(details.Contains("Highest mental ability modifier: +7")&&details.Contains("Total: +"+expected)&&details.Contains("Iterative attack: "+(index==0?"+0":"-"+index*5)),"real patched log method emits resolved component details");
     Check(details.Contains(kind==2?"Caster level: +18":"Spell base attack bonus: +16"),"BAB and caster-level labels distinguish spell attack bases");
     if(kind==2)Check(details.Contains("Mage's Sword enhancement: +3"),"sword +3 explicitly reported");
     data.Mental=99;var later=new StringBuilder();new AttackLogMessage().AppendAttackBonusBreakdown(later,outer);Check(later.ToString()==details,"log explanation remains a historical rule snapshot");data.Mental=7;
     using(rt.RequestEventContext())AccessTools.Method(typeof(PsychicSpiritController),"Strike").Invoke(rt.SourceBlueprintComponent,new object[]{actor.Unit,target.Unit,index,4});
    }
   }
   var eye=Asset<BlueprintUnit>(cache,"ArcaneEyeUnit");var eyeFeature=eye.AddFacts.Single();
   Check(eyeFeature.ComponentsArray.Any(c=>c is Kingmaker.UnitLogic.FactLogic.Untargetable),"eye keeps combat untargetability; click and hover get scoped UI exceptions");
   Check(eyeFeature.ComponentsArray.OfType<Kingmaker.UnitLogic.FactLogic.AddCondition>().Any(c=>c.Condition==UnitCondition.Invisible),"eye retains invisibility");
   Check(eyeFeature.ComponentsArray.Any(c=>c is Kingmaker.UnitLogic.FactLogic.CompleteDamageImmunity),"eye retains damage immunity");
   var donor=(BlueprintUnit)cache.Load(BlueprintGuid.Parse("655ac57b330918c4aadc78a00fb2ccaf"));Check(donor.Body.DisableHands,"audited native ghost donor has the original disabled-hands cause and remains unchanged");
  }finally{h.UnpatchAll(h.Id);}
  PsychicExpansionTests.Inventory["repair034Assertions"]=n;Console.WriteLine("PASS: "+n+" 034 native damage/AB/weapon-hand/log checks; rendering/click/size/equipment insertion endpoints bounded.");return n;
 }
}

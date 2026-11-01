using System;
using System.Linq;
using System.Collections.Generic;
using System.Runtime.Serialization;
using HarmonyLib;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.ElementsSystem;
using Kingmaker.EntitySystem;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components;
using Kingmaker.UnitLogic.Abilities.Components.Base;
using Kingmaker.UnitLogic.Buffs;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.FactLogic;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Mechanics.Actions;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Utility;
using CallOfTheWild.Wrath.Prototype;

// Execute the native duration evaluator, production link/storage actions and
// real 2.0.4 prefixes on native Apply methods. Buff creation and final spell
// delivery are explicit scene boundaries; they are captured, not simulated UI.
internal static class Psychic033Tests {
 static int n,applications,discharges;static readonly List<TimeSpan?> durations=new List<TimeSpan?>();
 static UnitEntityData recipient;
 static object Get(object o,string f)=>AccessTools.Field(o.GetType(),f).GetValue(o);
 static void Set(object o,string f,object v)=>AccessTools.Field(o.GetType(),f).SetValue(o,v);
 static T Empty<T>()where T:class=>(T)FormatterServices.GetUninitializedObject(typeof(T));
 static void Check(bool b,string m){if(!b)throw new Exception("033: "+m);n++;}
 static T Asset<T>(BlueprintsCache c,string t)where T:SimpleBlueprint=>(T)c.Load((BlueprintGuid)AccessTools.Method(typeof(PsychicMakeLink).Assembly.GetType("CallOfTheWild.Wrath.Prototype.PsychicAssets"),"Id").Invoke(null,new object[]{t}));
 static BlueprintBuffReference Ref(BlueprintBuff b){var r=new BlueprintBuffReference();AccessTools.Property(typeof(BlueprintReferenceBase),"Cached").SetValue(r,b,null);return r;}
 static Psychic070Tests.Caster Unit(BlueprintsCache c,string id){var f=new Psychic070Tests.Caster(c);Set(f.Unit,"<UniqueId>k__BackingField",id);var facts=new EntityFactsManager(f.Unit);Set(f.Unit,"Facts",facts);var bc=new BuffCollection();bc.Initialize(facts);Set(f.Unit,"<Buffs>k__BackingField",bc);return f;}
 static Buff Add(UnitEntityData owner,BlueprintBuff bp,MechanicsContext context){
  var b=Empty<Buff>();Set(b,"Components",new List<EntityFactComponent>());b.RestoreOwnerLink(owner.Facts);
  var ctx=new MechanicsContext(context.MaybeCaster,owner.Descriptor,bp,context,new TargetWrapper(owner));ctx.SetParams(context.Params.Clone());
  Set(b,"m_Context",ctx);Set(b,"<Rank>k__BackingField",1);AccessTools.Method(typeof(EntityFact),"Setup").Invoke(b,new object[]{bp});Set(b,"<IsActive>k__BackingField",true);Set(b,"<IsTurnedOn>k__BackingField",true);
  owner.Facts.List.Add(b);AccessTools.Method(Get(owner.Facts,"m_Cache").GetType(),"Clear").Invoke(Get(owner.Facts,"m_Cache"),null);return b;
 }
 static bool AddBuff(BuffCollection __instance,BlueprintBuff blueprint,MechanicsContext parentContext,TimeSpan? duration,ref Buff __result){
  var owner=__instance.Owner.Unit;durations.Add(duration);__result=Add(owner,blueprint,parentContext);return false;
 }
 static bool DescriptorBuff(UnitDescriptor __0,BlueprintBuff __1,MechanicsContext __2,TimeSpan? __3,ref Buff __result){durations.Add(__3);__result=Add(__0.Unit,__1,__2);return false;}
 static EntityFactComponent Runtime<T>(Buff b)=>b.Components.Single(x=>x.SourceBlueprintComponent is T);
 static PsychicStoredSpellData State(Buff b)=>Runtime<PsychicStoredSpell>(b).GetData<PsychicStoredSpellData>();
 static bool Remove(Buff __instance){Set(__instance,"<IsTurnedOn>k__BackingField",false);return false;}
 static bool Deliver(AbilityData stored,AbilityParams parameters,UnitEntityData target,bool __3){discharges++;recipient=target;return false;}
 static bool DisplayName(Kingmaker.Blueprints.Facts.BlueprintUnitFact __instance,ref string __result){__result=__instance.name;return false;}
 static bool Hit(ref bool __result){__result=true;return false;}
 static bool Attack(bool single){return false;}
 static bool EngagementBoundary()=>false; // This delivery fixture does not construct a combat world; tested in Feedback01412Tests.
 // The after-spawn fixture has no inventory/view. Capture the native equipment
 // insertion endpoint while retaining real variant/bind routing.
 static bool Equip(UnitEntityData unit,Kingmaker.Blueprints.Items.Weapons.BlueprintItemWeapon blueprint,ref bool __result){Check(unit!=null&&blueprint!=null&&blueprint.Category==Kingmaker.Enums.WeaponCategory.Longsword,"Law form selects its private longsword before attacking");__result=true;return false;}
 static bool Player(ref bool __result){__result=true;return false;}
 sealed class EffectCounter:ContextAction {public override string GetCaption()=>"Native action test counter";public override void RunAction(){applications++;}}
 // Exact public replacement shape used by TTT; no optional assembly is needed.
 sealed class AbilityTargetHasNoFactUnlessAccursedHex:BlueprintComponent,IAbilityTargetRestriction {
  public BlueprintUnitFactReference[] m_CheckedFacts;public bool Passed;
  public bool IsTargetRestrictionPassed(UnitEntityData caster,TargetWrapper target)=>Passed;
  public string GetAbilityTargetRestrictionUIText(UnitEntityData caster,TargetWrapper target)=>"Test hex cooldown";
 }
 static void Execute(ContextAction action,MechanicsContext context,UnitEntityData target){using(context.GetDataScope(new TargetWrapper(target)))action.RunAction();}
 static AbilityExecutionContext Cast(Psychic070Tests.Caster f,int level,bool harmful=false){
  var c=f.Cast(SpellSchool.Transmutation,SpellDescriptor.None,level).Context;c.Ability.Blueprint.name="033Companion";c.Ability.OverrideSpellLevel=level;
  c.Ability.Blueprint.CanTargetSelf=!harmful;c.Ability.Blueprint.CanTargetEnemies=harmful;c.Ability.Blueprint.NotOffensive=!harmful;
  c.Ability.Blueprint.ActionType=Kingmaker.UnitLogic.Commands.Base.UnitCommand.CommandType.Standard;
  c.Ability.Blueprint.ComponentsArray=new BlueprintComponent[]{new SpellComponent{School=SpellSchool.Transmutation},harmful?(BlueprintComponent)new AbilityEffectStickyTouch():new AbilityEffectRunAction{Actions=new ActionList{Actions=new GameAction[]{new EffectCounter()}}}};c.Ability.Blueprint.OnEnable();return c;
 }
 internal static int Run(BlueprintsCache cache){
  var h=new Harmony("CotW033.NativeDelivery");var assembly=typeof(PsychicMakeLink).Assembly;var oldPack=Kingmaker.Localization.LocalizationManager.CurrentPack;Kingmaker.Localization.LocalizationManager.CurrentPack=new Kingmaker.Localization.LocalizationPack();AccessTools.Method(assembly.GetType("CallOfTheWild.Wrath.Prototype.PsychicAssets"),"ApplyLocalization").Invoke(null,null);
  var add=typeof(BuffCollection).GetMethods().Single(m=>m.Name=="AddBuff"&&m.GetParameters().Length==3&&m.GetParameters()[1].ParameterType==typeof(MechanicsContext));
  h.Patch(add,prefix:new HarmonyMethod(typeof(Psychic033Tests),nameof(AddBuff)));
  h.Patch(AccessTools.Method(typeof(UnitHelper),"AddBuff",new[]{typeof(UnitDescriptor),typeof(BlueprintBuff),typeof(MechanicsContext),typeof(TimeSpan?)}),prefix:new HarmonyMethod(typeof(Psychic033Tests),nameof(DescriptorBuff)){priority=Priority.First});
  h.Patch(AccessTools.Method(typeof(Buff),"Remove"),prefix:new HarmonyMethod(typeof(Psychic033Tests),nameof(Remove)));
  h.Patch(AccessTools.Method(typeof(PsychicStoredSpell),"Deliver"),prefix:new HarmonyMethod(typeof(Psychic033Tests),nameof(Deliver)));
  h.Patch(AccessTools.PropertyGetter(typeof(RuleAttackRoll),"IsHit"),prefix:new HarmonyMethod(typeof(Psychic033Tests),nameof(Hit)));
  h.Patch(AccessTools.PropertyGetter(typeof(Kingmaker.Blueprints.Facts.BlueprintUnitFact),"Name"),prefix:new HarmonyMethod(typeof(Psychic033Tests),nameof(DisplayName)));
  h.Patch(AccessTools.Method(typeof(PsychicSpiritController),"Attack"),prefix:new HarmonyMethod(typeof(Psychic033Tests),nameof(Attack)));
  h.Patch(AccessTools.Method(typeof(PsychicSpiritAttackStats),"RefreshEngagement"),prefix:new HarmonyMethod(typeof(Psychic033Tests),nameof(EngagementBoundary)));
  h.Patch(AccessTools.Method(assembly.GetType("CallOfTheWild.Wrath.Prototype.SpiritWeaponForms"),"Equip"),prefix:new HarmonyMethod(typeof(Psychic033Tests),nameof(Equip)));
  h.Patch(AccessTools.PropertyGetter(typeof(UnitEntityData),"IsPlayerFaction"),prefix:new HarmonyMethod(typeof(Psychic033Tests),nameof(Player)));
  foreach(string name in new[]{"RunActionEffect","StickyTouchEffect","ButtonSpellbook"})h.CreateClassProcessor(assembly.GetType("CallOfTheWild.Wrath.Prototype.PsychicStoragePatch+"+name)).Patch();
  try{
   var f=Unit(cache,"033-caster");var ally=Unit(cache,"033-ally");var enemy=Unit(cache,"033-enemy");
   var link=Asset<BlueprintAbility>(cache,"BattlemindLink");var action=link.GetComponent<AbilityEffectRunAction>().Actions.Actions.OfType<PsychicMakeLink>().Single();
   foreach(int cl in new[]{1,12,25,40})foreach(bool extend in new[]{false,true}){
    var ctx=new MechanicsContext(f.Unit,ally.Descriptor,link,null,new TargetWrapper(ally.Unit));ctx.SetParams(new AbilityParams{CasterLevel=cl,Metamagic=extend?Metamagic.Extend:0});ctx.Recalculate();
    Check(action.Duration.Calculate(ctx).Seconds==TimeSpan.FromMinutes(cl*(extend?2:1)),"native link duration including Extend and CL40");
    Execute(action,ctx,ally.Unit);var left=f.Unit.Buffs.Enumerable.Last(b=>b.IsTurnedOn);var right=ally.Unit.Buffs.Enumerable.Last(b=>b.IsTurnedOn);
    var ld=Runtime<PsychicBattlemind>(left).GetData<PsychicBattlemindData>();var rd=Runtime<PsychicBattlemind>(right).GetData<PsychicBattlemindData>();
    Check(ld.Partner==ally.Unit&&rd.Partner==f.Unit&&ld.Pair==rd.Pair&&ld.Pair!=null,"actual action applies reciprocal target and caster links");
    Check(left.IsFromSpell&&right.IsFromSpell&&durations.Last()==action.Duration.Calculate(ctx).Seconds,"links have spell provenance and evaluated lifetime");
   }
   foreach(string token in new[]{"SpiritualWeapon","SpiritualAlly","MagesSword","TwilightKnife","ArcaneEye"}){
    var bp=Asset<BlueprintAbility>(cache,token);var spawn=(bp.GetComponent<AbilityVariants>()?.Variants.Last(a=>a.name.EndsWith("Form3"))??bp).GetComponent<AbilityEffectRunAction>().Actions.Actions.OfType<ContextActionSpawnMonster>().Single();
    foreach(int cl in new[]{1,12,25,40}){
     var ctx=new MechanicsContext(f.Unit,enemy.Descriptor,bp,null,new TargetWrapper(enemy.Unit));ctx.SetParams(new AbilityParams{CasterLevel=cl});ctx.Recalculate();
     Check(spawn.DurationValue.Calculate(ctx).Seconds==TimeSpan.FromSeconds(cl*(token=="ArcaneEye"?60:6)),"native summon duration no null dice field "+token);
     Check(spawn.CountValue.Calculate(ctx)==1,"native summon count "+token);
    }
    var actor=Unit(cache,"033-actor-"+token);Set(actor.Descriptor,"<Blueprint>k__BackingField",Asset<BlueprintUnit>(cache,token+"Unit"));
    var bab=new ModifiableValue(f.Unit.Stats,StatType.BaseAttackBonus);bab.BaseValue=13;Set(f.Unit.Stats,"BaseAttackBonus",bab);
    foreach(var stat in new[]{StatType.Intelligence,StatType.Charisma}){var value=new ModifiableValueAttributeStat(f.Unit.Stats,stat);value.BaseValue=18;Set(f.Unit.Stats,stat.ToString(),value);}
    Set(actor.Unit.Stats,"BaseAttackBonus",new ModifiableValue(actor.Unit.Stats,StatType.BaseAttackBonus));Set(actor.Unit.Stats,"AdditionalAttackBonus",new ModifiableValue(actor.Unit.Stats,StatType.AdditionalAttackBonus));Set(actor.Unit.Stats,"SneakAttack",new ModifiableValue(actor.Unit.Stats,StatType.SneakAttack));
    var bindContext=new MechanicsContext(f.Unit,actor.Descriptor,bp,null,new TargetWrapper(enemy.Unit));bindContext.SetParams(new AbilityParams{CasterLevel=18});bindContext.Recalculate();
    var controller=Add(f.Unit,Asset<BlueprintBuff>(cache,token+"Controller"),bindContext);
    // These are the exact AfterSpawn actions installed on the live blueprint.
    foreach(ContextAction child in spawn.AfterSpawn.Actions)Execute(child,bindContext,actor.Unit);
    var state=Runtime<PsychicSpiritController>(controller).GetData<PsychicSpiritState>();
    Check(state.Summon==actor.Unit&&state.Target==enemy.Unit&&state.Level==18&&state.Bab==13&&state.Mental==10,"actual AfterSpawn binds own actor, target and caster stats "+token);
    Check(actor.Unit.Buffs.Enumerable.Any(x=>x.GetComponent<PsychicSpiritAttackStats>()!=null),"actual AfterSpawn creates actor attack/vision buff "+token);
    if(token=="ArcaneEye"){
     var control=AccessTools.Method(assembly.GetType("CallOfTheWild.Wrath.Prototype.PsychicEyeControlPatch"),"OwnSensor");
     Check((bool)control.Invoke(null,new object[]{actor.Unit}),"bound own eye passes scoped control gate");state.Summon=enemy.Unit;Check(!(bool)control.Invoke(null,new object[]{actor.Unit}),"unbound/stale eye cannot acquire control");state.Summon=actor.Unit;
    }
    Set(controller,"<IsTurnedOn>k__BackingField",false);
   }
   var hosts=new[]{"Contingency","Spite","DelayedConsumption","EmblemOfGreed"}.Select(t=>Add(f.Unit,Asset<BlueprintBuff>(cache,t+"Buff"),new MechanicsContext(f.Unit,f.Descriptor,link,null,new TargetWrapper(f.Unit)))).ToArray();
   foreach(var host in hosts)host.Context.SetParams(new AbilityParams{CasterLevel=18,DC=29});
   for(int kind=0;kind<4;kind++){
    string token=new[]{"Contingency","Spite","DelayedConsumption","EmblemOfGreed"}[kind];var host=hosts[kind];
    var facts=host.Blueprint.GetComponent<AddFacts>().Facts.ToArray();Check(facts.Any(x=>x==Asset<BlueprintAbility>(cache,token+"StoreNext")),"store button belongs to real granted facts "+token);
    var button=Asset<BlueprintAbility>(cache,token+"StoreNext");var ad=Empty<AbilityData>();Set(ad,"Blueprint",button);Set(ad,"m_CachedSpellbook",f.Book);
    Check(ad.Spellbook==null,"inherited slot cannot block storage button "+token);
    Execute(button.GetComponent<AbilityEffectRunAction>().Actions.Actions.OfType<PsychicStorageAction>().Single(),host.Context,f.Unit);
    Check(State(host).Armed,"real Store Next action arms exact ward "+token);
    var book=(BlueprintSpellbook)AccessTools.Method(typeof(object),"MemberwiseClone").Invoke(f.Book.Blueprint,null);book.ComponentsArray=kind==2?new BlueprintComponent[]{new IsAlchemistSpellbook()}:Array.Empty<BlueprintComponent>();book.OnEnable();Set(f.Book,"Blueprint",book);
    var cast=Cast(f,2,kind==1||kind==3);cast.IsForced=true; // Native cheater casts are not our release contexts.
    int before=applications;cast.Ability.Blueprint.GetComponents<AbilityApplyEffect>().Single().Apply(cast,new TargetWrapper(f.Unit));
    Check(State(host).Spell!=null&&!State(host).Armed&&applications==before,"native Apply captures before effects/touch charge "+token);
    Check(f.Unit.Get<Kingmaker.UnitLogic.Parts.UnitPartTouch>()==null,"storage grants no premature touch charge "+token);
    if(kind==0||kind==2){var release=Asset<BlueprintAbility>(cache,token+"Release");var rd=Empty<AbilityData>();Set(rd,"Blueprint",release);Set(rd,"m_CachedSpellbook",f.Book);Check(rd.Spellbook==null,"release needs no slot "+token);before=discharges;Execute(release.GetComponent<AbilityEffectRunAction>().Actions.Actions.OfType<PsychicStorageAction>().Single(),host.Context,f.Unit);Check(discharges==before+1&&recipient==f.Unit&&State(host).Spell==null,"real release action discharges once "+token);}
    else if(kind==1){var weapon=Empty<Kingmaker.Items.ItemEntityWeapon>();Set(weapon,"m_Blueprint",Asset<Kingmaker.Blueprints.Items.Weapons.BlueprintItemWeapon>(cache,"EmblemOfGreedItem0"));var hit=Empty<RuleAttackWithWeapon>();Set(hit,"Initiator",enemy.Unit);Set(hit,"Target",f.Unit);Set(hit,"Weapon",weapon);Set(hit,"<AttackRoll>k__BackingField",Empty<RuleAttackRoll>());before=discharges;var rt=Runtime<PsychicStoredSpell>(host);using(rt.RequestEventContext())((PsychicStoredSpell)rt.SourceBlueprintComponent).OnEventDidTrigger(hit);Check(discharges==before+1&&recipient==enemy.Unit&&State(host).Spell==null,"Spite native successful melee event discharges on attacker");}
   }
   // An unrelated direct-delivery spell still executes; this checks the actual
   // native RunAction body, rather than calling Capture in isolation.
   var normal=Cast(f,2);int count=applications;normal.Ability.Blueprint.GetComponent<AbilityEffectRunAction>().Apply(normal,new TargetWrapper(f.Unit));Check(applications==count+1,"unarmed native action delivery unchanged");
   var hex=assembly.GetType("CallOfTheWild.Wrath.Prototype.PsychicHexVulnerability");var pair=(Array)AccessTools.Field(hex,"Pairs").GetValue(null);var first=(string[])pair.GetValue(0);
   var cooldown=(BlueprintBuff)cache.Load(BlueprintGuid.Parse(first[1]));var hexAbility=(BlueprintAbility)cache.Load(BlueprintGuid.Parse(first[0]))??new BlueprintAbility{AssetGuid=BlueprintGuid.Parse(first[0]),ComponentsArray=Array.Empty<BlueprintComponent>()};
   var checker=new AbilityTargetHasNoFactUnlessAccursedHex{m_CheckedFacts=new[]{new BlueprintUnitFactReference()}};AccessTools.Property(typeof(BlueprintReferenceBase),"Cached").SetValue(checker.m_CheckedFacts[0],cooldown,null);Set(checker.m_CheckedFacts[0],"deserializedGuid",cooldown.AssetGuid);checker.OwnerBlueprint=hexAbility;
   var pass=AccessTools.Method(hex,"PassRestriction");var target=new TargetWrapper(enemy.Unit);
   Check(!(bool)pass.Invoke(null,new object[]{checker,f.Unit,target}),"TTT cooldown cannot bypass without vulnerability");
   var vulnerability=Add(enemy.Unit,Asset<BlueprintBuff>(cache,"HexVulnerabilityBuff"),new MechanicsContext(f.Unit,enemy.Descriptor,hexAbility,null,target));
   Check((bool)pass.Invoke(null,new object[]{checker,f.Unit,target}),"TTT replacement receives matching caster vulnerability bypass");
   Check(!(bool)pass.Invoke(null,new object[]{checker,ally.Unit,target}),"foreign caster cannot reuse another caster vulnerability");
   checker.m_CheckedFacts=Array.Empty<BlueprintUnitFactReference>();Check(!(bool)pass.Invoke(null,new object[]{checker,f.Unit,target}),"unrelated TTT restrictions remain blocked");
   checker.Passed=true;Check((bool)pass.Invoke(null,new object[]{checker,f.Unit,target}),"original valid target remains valid");Set(vulnerability,"<IsTurnedOn>k__BackingField",false);
  }finally{h.UnpatchAll(h.Id);Kingmaker.Localization.LocalizationManager.CurrentPack=oldPack;}
  PsychicExpansionTests.Inventory["repair033Assertions"]=n;Console.WriteLine("PASS: "+n+" 033 actual native duration/link action/Apply storage/slot/Spite/TTT replacement checks; buff insertion and final delivery endpoints captured.");return n;
 }
}

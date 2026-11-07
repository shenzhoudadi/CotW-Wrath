using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using HarmonyLib;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.Controllers;
using Kingmaker.EntitySystem;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components;
using Kingmaker.UnitLogic.Abilities.Components.Base;
using Kingmaker.UnitLogic.Buffs;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.Utility;
using CallOfTheWild.Wrath.Prototype;

// Real Harmony 2.0.4 dispatcher and production patches on a controlled effect
// endpoint. Native game DoApplyEffect installation is checked by HarmonyTests
// and both IntegratedStartupTests orders; this boundary avoids Unity scene FX.
internal static class Psychic032Tests {
 static int assertions,applied,finallyCalls; static bool skip,early,fail; static TimeSpan now;
 static object Get(object o,string k)=>AccessTools.Field(o.GetType(),k).GetValue(o);
 static void Set(object o,string k,object v)=>AccessTools.Field(o.GetType(),k).SetValue(o,v);
 static T Empty<T>()where T:class=>(T)FormatterServices.GetUninitializedObject(typeof(T));
 static void Check(bool value,string message){if(!value)throw new Exception("032: "+message);assertions++;}
 static T Asset<T>(BlueprintsCache c,string token)where T:SimpleBlueprint=>(T)c.Load((BlueprintGuid)AccessTools.Method(typeof(PsychicMakeLink).Assembly.GetType("CallOfTheWild.Wrath.Prototype.PsychicAssets"),"Id").Invoke(null,new object[]{token}));
 static Psychic070Tests.Caster Unit(BlueprintsCache c,string id){var f=new Psychic070Tests.Caster(c);Set(f.Unit,"<UniqueId>k__BackingField",id);var facts=new EntityFactsManager(f.Unit);Set(f.Unit,"Facts",facts);var buffs=new BuffCollection();buffs.Initialize(facts);Set(f.Unit,"<Buffs>k__BackingField",buffs);return f;}
 static Buff Add(UnitEntityData owner,BlueprintBuff bp,UnitEntityData caster){var b=Empty<Buff>();Set(b,"Components",new List<EntityFactComponent>());b.RestoreOwnerLink(owner.Facts);var ctx=new MechanicsContext(caster,owner.Descriptor,bp,null,new TargetWrapper(owner));ctx.SetParams(new AbilityParams{CasterLevel=18,DC=24});Set(b,"m_Context",ctx);Set(b,"<Rank>k__BackingField",1);AccessTools.Method(typeof(EntityFact),"Setup").Invoke(b,new object[]{bp});Set(b,"<IsActive>k__BackingField",true);Set(b,"<IsTurnedOn>k__BackingField",true);owner.Facts.List.Add(b);AccessTools.Method(Get(owner.Facts,"m_Cache").GetType(),"Clear").Invoke(Get(owner.Facts,"m_Cache"),null);return b;}
 static EntityFactComponent Runtime<T>(Buff b)=>b.Components.Single(x=>x.SourceBlueprintComponent is T);
 static bool Enemy(UnitEntityData __instance,UnitEntityData unit,ref bool __result){__result=unit!=__instance;return false;}
 static bool Time(ref TimeSpan __result){__result=now;return false;}
 static bool Foreign()=>!skip;
 [MethodImpl(MethodImplOptions.NoInlining)]
 static void EffectBoundary(AbilityExecutionContext context,TargetWrapper target,AbilityApplyEffect effect){
  if(early)return;
  try{if(fail)throw new InvalidOperationException("controlled delivery failure");applied++;}
  finally{finallyCalls++;}
 }
 static void Invoke(AbilityExecutionContext c,UnitEntityData target,AbilityApplyEffect effect)=>AccessTools.Method(typeof(Psychic032Tests),nameof(EffectBoundary)).Invoke(null,new object[]{c,new TargetWrapper(target),effect});
 internal static int Run(BlueprintsCache cache){
  Check(typeof(Harmony).Assembly.GetName().Version==new Version(2,3,6,0),"mandatory actual game Harmony version");
  var h=new Harmony("CotW032.Dispatcher");var foreign=new Harmony("CotW032.Foreign");var oldTime=Kingmaker.Game.Instance.TimeController;
  Set(Kingmaker.Game.Instance,"TimeController",Empty<TimeController>());
  h.Patch(AccessTools.Method(typeof(UnitEntityData),"IsEnemy",new[]{typeof(UnitEntityData)}),prefix:new HarmonyMethod(typeof(Psychic032Tests),nameof(Enemy)));
  h.Patch(AccessTools.PropertyGetter(typeof(TimeController),"GameTime"),prefix:new HarmonyMethod(typeof(Psychic032Tests),nameof(Time)));
  var assembly=typeof(PsychicMakeLink).Assembly;var linkPatch=assembly.GetType("CallOfTheWild.Wrath.Prototype.PsychicLinkedWardPatches+SpellPatch");
  var globePatch=assembly.GetType("CallOfTheWild.Wrath.Prototype.PsychicGlobeSpells+DeliveryPatch");
  var storagePatch=assembly.GetType("CallOfTheWild.Wrath.Prototype.PsychicStoragePatch+Effect");
  var boundary=AccessTools.Method(typeof(Psychic032Tests),nameof(EffectBoundary));
  try{
   // Check both insertion orders; a foreign prefix has no state dependency.
   foreach(bool foreignFirst in new[]{true,false}){
    if(foreignFirst)foreign.Patch(boundary,prefix:new HarmonyMethod(typeof(Psychic032Tests),nameof(Foreign)));
    h.Patch(boundary,prefix:new HarmonyMethod(linkPatch,"Prefix"),transpiler:new HarmonyMethod(linkPatch,"Transpiler"));
    h.Patch(boundary,prefix:new HarmonyMethod(globePatch,"Prefix"));
    h.Patch(boundary,prefix:new HarmonyMethod(storagePatch,"Prefix"));
    if(!foreignFirst)foreign.Patch(boundary,prefix:new HarmonyMethod(typeof(Psychic032Tests),nameof(Foreign)));
    var a=Unit(cache,"dispatch-a"+foreignFirst);var b=Unit(cache,"dispatch-b"+foreignFirst);var enemy=Unit(cache,"dispatch-enemy"+foreignFirst);
    var left=Add(a.Unit,Asset<BlueprintBuff>(cache,"BattlemindLinkBuff"),a.Unit);var right=Add(b.Unit,Asset<BlueprintBuff>(cache,"BattlemindLinkBuff"),a.Unit);
    var ld=Runtime<PsychicBattlemind>(left).GetData<PsychicBattlemindData>();var rd=Runtime<PsychicBattlemind>(right).GetData<PsychicBattlemindData>();
    ld.Pair=rd.Pair="dispatcher";ld.Partner=b.Unit;rd.Partner=a.Unit;
    var ctx=a.Cast(SpellSchool.Enchantment,SpellDescriptor.None,2).Context;ctx.Ability.Blueprint.CanTargetEnemies=true;
    now=TimeSpan.FromSeconds(10);skip=early=fail=false;int before=applied;
    Invoke(ctx,enemy.Unit,new AbilityEffectRunAction());
    Check(applied==before+1&&ld.Spells[enemy.Unit.UniqueId]==TimeSpan.FromSeconds(16),"completed original marks exactly one round");
    var rt=Runtime<PsychicBattlemind>(right);var other=b.Cast(SpellSchool.Enchantment,SpellDescriptor.None,2).Context;
    var save=new RuleSavingThrow(enemy.Unit,Kingmaker.EntitySystem.Stats.SavingThrowType.Will,24){Reason=new Kingmaker.RuleSystem.RuleReason(other)};
    using(rt.RequestEventContext())((PsychicBattlemind)rt.SourceBlueprintComponent).OnEventAboutToTrigger(save);
    Check(save.AllBonuses.Sum(x=>x.Value)==-2,"dispatcher marker grants partner save penalty");
    ld.Spells.Clear();skip=true;before=applied;Invoke(ctx,enemy.Unit,new AbilityEffectRunAction());
    Check(applied==before&&ld.Spells.Count==0,"foreign skipped original must not mark, either insertion order");skip=false;
    early=true;Invoke(ctx,enemy.Unit,new AbilityEffectRunAction());Check(ld.Spells.Count==1,"native normal early return preserves prior completed-original semantics");early=false;ld.Spells.Clear();
    fail=true;before=finallyCalls;try{Invoke(ctx,enemy.Unit,new AbilityEffectRunAction());throw new Exception("missing throw");}catch(TargetInvocationException e){Check(e.InnerException is InvalidOperationException,"exception propagates");}
    Check(finallyCalls==before+1&&ld.Spells.Count==0,"finally preserved and failed body never marks");fail=false;
    var ward=Add(enemy.Unit,Asset<BlueprintBuff>(cache,"SanctuaryBuff"),enemy.Unit);Runtime<PsychicSanctuary>(ward).GetData<PsychicSanctuaryData>().Results[a.Unit.UniqueId]=false;
    before=applied;Invoke(ctx,enemy.Unit,new AbilityEffectRunAction());Check(applied==before&&ld.Spells.Count==0,"production Sanctuary prefix skips callback");Set(ward,"<IsTurnedOn>k__BackingField",false);
    var globe=Add(enemy.Unit,Asset<BlueprintBuff>(cache,"GlobeOfInvulnerabilityLesserBuff"),enemy.Unit);
    before=applied;Invoke(ctx,enemy.Unit,new AbilityEffectRunAction());Check(applied==before&&ld.Spells.Count==0,"production Globe prefix skips callback");Set(globe,"<IsTurnedOn>k__BackingField",false);
    ctx.Ability.Blueprint.Type=AbilityType.Supernatural;Invoke(ctx,enemy.Unit,new AbilityEffectRunAction());Check(ld.Spells.Count==0,"non-spell delivery excluded");ctx.Ability.Blueprint.Type=AbilityType.Spell;
    // BlueprintAbility caches StickyTouch; use a fresh blueprint for a touch cast.
    ctx=a.Cast(SpellSchool.Necromancy,SpellDescriptor.None,2).Context;ctx.Ability.Blueprint.name="DispatcherStorageCompanion";ctx.Ability.Blueprint.CanTargetEnemies=true;
    ctx.Ability.Blueprint.ComponentsArray=new BlueprintComponent[]{new AbilityEffectStickyTouch()};ctx.Ability.Blueprint.OnEnable();Invoke(ctx,enemy.Unit,new AbilityEffectStickyTouch());Check(ld.Spells.Count==0,"touch preparation excluded");
    var host=Add(a.Unit,Asset<BlueprintBuff>(cache,"SpiteBuff"),a.Unit);var stored=Runtime<PsychicStoredSpell>(host).GetData<PsychicStoredSpellData>();stored.Armed=true;
    ctx.Ability.OverrideSpellLevel=2;ctx.Ability.Blueprint.NotOffensive=false;ctx.Ability.Blueprint.ActionType=Kingmaker.UnitLogic.Commands.Base.UnitCommand.CommandType.Standard;
    before=applied;Invoke(ctx,enemy.Unit,new AbilityEffectStickyTouch());Check(applied==before&&stored.Spell!=null&&!stored.Armed&&ld.Spells.Count==0,"real storage prefix captures without delivery or marker");
    Invoke(ctx,enemy.Unit,new AbilityEffectStickyTouch());Check(applied==before&&ld.Spells.Count==0,"captured cast repeat stays suppressed");
    h.Unpatch(boundary,HarmonyPatchType.All,h.Id);foreign.UnpatchAll(foreign.Id);
   }
  }finally{h.UnpatchAll(h.Id);foreign.UnpatchAll(foreign.Id);Set(Kingmaker.Game.Instance,"TimeController",oldTime);}
  PsychicExpansionTests.Inventory["new032Assertions"]=assertions;
  Console.WriteLine("PASS: "+assertions+" Harmony2.0.4 real dispatcher/completion/foreign skip/exception/Sanctuary/Globe/storage checks (controlled effect endpoint).");return assertions;
 }
}

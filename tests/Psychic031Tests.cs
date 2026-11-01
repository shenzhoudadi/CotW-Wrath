using System;
using Kingmaker.RuleSystem.Rules.Abilities;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Enums;
using System.Linq;
using System.Collections.Generic;
using System.Runtime.Serialization;
using HarmonyLib;
using Newtonsoft.Json;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.EntitySystem;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components;
using Kingmaker.UnitLogic.Abilities.Components.Base;
using Kingmaker.UnitLogic.Buffs;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Commands.Base;
using Kingmaker.Utility;
using CallOfTheWild.Wrath.Prototype;
internal static class Psychic031Tests {
 static int n,released,removed;static RuleCastSpell launched;static AbilityData delivered;static AbilityParams deliveredParams;static UnitEntityData recipient;static bool touch;
 static object Get(object o,string k)=>AccessTools.Field(o.GetType(),k).GetValue(o);static void Set(object o,string k,object v)=>AccessTools.Field(o.GetType(),k).SetValue(o,v);static T Empty<T>()where T:class=>(T)FormatterServices.GetUninitializedObject(typeof(T));static void Check(bool b,string m){if(!b)throw new Exception("031: "+m);n++;}
 static T Asset<T>(BlueprintsCache c,string t)where T:SimpleBlueprint=>(T)c.Load((BlueprintGuid)AccessTools.Method(typeof(PsychicStorageAction).Assembly.GetType("CallOfTheWild.Wrath.Prototype.PsychicAssets"),"Id").Invoke(null,new object[]{t}));
 static Buff Host(Psychic070Tests.Caster f,BlueprintBuff bp){var owner=f.Unit;if(owner.Facts==null){var facts=new EntityFactsManager(owner);Set(owner,"Facts",facts);var buffs=new BuffCollection();buffs.Initialize(facts);Set(owner,"<Buffs>k__BackingField",buffs);}var b=Empty<Buff>();Set(b,"Components",new List<EntityFactComponent>());b.RestoreOwnerLink(owner.Facts);var ctx=new MechanicsContext(owner,owner.Descriptor,bp,null,new TargetWrapper(owner));ctx.SetParams(new AbilityParams{CasterLevel=18,DC=28});Set(b,"m_Context",ctx);Set(b,"<Rank>k__BackingField",1);AccessTools.Method(typeof(EntityFact),"Setup").Invoke(b,new object[]{bp});Set(b,"<IsActive>k__BackingField",true);Set(b,"<IsTurnedOn>k__BackingField",true);owner.Facts.List.Add(b);AccessTools.Method(Get(owner.Facts,"m_Cache").GetType(),"Clear").Invoke(Get(owner.Facts,"m_Cache"),null);return b;}
 static EntityFactComponent Runtime(Buff b)=>b.Components.Single(x=>x.SourceBlueprintComponent is PsychicStoredSpell);
 static PsychicStoredSpellData State(Buff b)=>Runtime(b).GetData<PsychicStoredSpellData>();
 static void Call(Buff b,string method,params object[] args){var rt=Runtime(b);using(rt.RequestEventContext())AccessTools.Method(typeof(PsychicStoredSpell),method).Invoke(rt.SourceBlueprintComponent,args);}
 static void Alchemy(BlueprintSpellbook b,bool on){b.ComponentsArray=on?new BlueprintComponent[]{new IsAlchemistSpellbook()}:Array.Empty<BlueprintComponent>();b.OnEnable();}
 static bool Legal(ref bool __result){__result=true;return false;}
 static bool Launch(RuleCastSpell cast){launched=cast;return false;}
 static bool Execution(AbilityData __instance,TargetWrapper target,ref AbilityExecutionContext __result){var c=Empty<AbilityExecutionContext>();Set(c,"Ability",__instance);Set(c,"AssociatedBlueprint",__instance.Blueprint);Set(c,"m_CasterRef",Get(__instance,"m_CasterRef"));c.SetParams(new AbilityParams());__result=c;return false;}
 static bool Remove(){removed++;return false;}
 static bool Deliver(AbilityData stored,AbilityParams parameters,UnitEntityData target,bool __3){released++;delivered=stored;deliveredParams=parameters;recipient=target;touch=__3;return false;}
 static AbilityExecutionContext Cast(Psychic070Tests.Caster f,int circle,bool harmful=false){var c=f.Cast(SpellSchool.Transmutation,SpellDescriptor.None,circle).Context;c.Ability.Blueprint.name="StorageCompanion";c.Ability.Blueprint.CanTargetSelf=!harmful;c.Ability.Blueprint.CanTargetEnemies=harmful;c.Ability.Blueprint.ActionType=UnitCommand.CommandType.Standard;c.Ability.Blueprint.NotOffensive=!harmful;c.Ability.OverrideSpellLevel=circle;if(harmful)c.Ability.Blueprint.ComponentsArray=new BlueprintComponent[]{new SpellComponent{School=SpellSchool.Necromancy},new AbilityEffectStickyTouch()};c.Ability.Blueprint.OnEnable();return c;}
 internal static int Run(BlueprintsCache cache){var h=new Harmony("CotW031.Storage");h.Patch(AccessTools.Method(typeof(Buff),"Remove"),prefix:new HarmonyMethod(typeof(Psychic031Tests),nameof(Remove)));h.Patch(AccessTools.Method(typeof(PsychicStoredSpell),"Deliver"),prefix:new HarmonyMethod(typeof(Psychic031Tests),nameof(Deliver)));
 var eligible=AccessTools.Method(typeof(PsychicStoredSpell),"Eligible");var patch=typeof(PsychicStorageAction).Assembly.GetType("CallOfTheWild.Wrath.Prototype.PsychicStoragePatch");var capture=AccessTools.Method(patch,"Capture");
 try{
  var f=new Psychic070Tests.Caster(cache);Set(f.Unit,"<UniqueId>k__BackingField","storage-owner-A");var g=new Psychic070Tests.Caster(cache);Set(g.Unit,"<UniqueId>k__BackingField","storage-owner-B");
  // Isolate alchemist setting from the shared native/Psychic book blueprint.
  var book=(BlueprintSpellbook)AccessTools.Method(typeof(object),"MemberwiseClone").Invoke(f.Book.Blueprint,null);Set(f.Book,"Blueprint",book);
  var hosts=new[]{"Contingency","Spite","DelayedConsumption","EmblemOfGreed"}.Select(t=>Host(f,Asset<BlueprintBuff>(cache,t+"Buff"))).ToArray();var peer=Host(g,Asset<BlueprintBuff>(cache,"ContingencyBuff"));
  for(int k=0;k<4;k++)for(int cl=1;cl<=21;cl++)for(int circle=0;circle<=7;circle++){
   var c=Cast(f,circle,k==1||k==3);Alchemy(book,k==2);var expected=circle>0&&circle<=(k==0?Math.Min(6,cl/3):k==3?3:4)&&(k!=3||cl>=14);
   Check((bool)eligible.Invoke(null,new object[]{k,cl,c.Ability,new TargetWrapper(f.Unit)})==expected,"storage level/type threshold matrix");
  }
  Alchemy(book,false);
  Call(peer,"Arm");for(int k=0;k<4;k++){Call(hosts[k],"Arm");Check(hosts.Count(b=>State(b).Armed)==1&&State(hosts[k]).Armed,"only one local storage armed");Check(State(peer).Armed,"other caster arming independent");Call(hosts[k],"Arm");Check(!State(hosts[k]).Armed,"second click disarms");}
  var ctx=Cast(f,3);ctx.Params.CasterLevel=17;ctx.Params.DC=31;ctx.Params.Metamagic=Metamagic.Extend;Call(hosts[0],"Arm");
  Check(!(bool)capture.Invoke(null,new object[]{ctx,new TargetWrapper(f.Unit),new AbilityEffectRunAction()}),"eligible delivery captured");var data=State(hosts[0]);Check(data.Spell!=null&&!data.Armed&&data.Parameters.DC==31&&data.Parameters.CasterLevel==17,"capture stores original parameters once");Check(data.Spell.Caster.Unit==f.Unit,"stored native ability belongs to original caster");Check(!(bool)capture.Invoke(null,new object[]{ctx,new TargetWrapper(f.Unit),new AbilityEffectRunAction()}),"later components of same cast suppressed");Check(State(peer).Spell==null,"capture cannot consume another character ward");
  var saved=JsonConvert.DeserializeObject<AbilityParams>(JsonConvert.SerializeObject(data.Parameters));Check(saved.DC==31&&saved.CasterLevel==17&&saved.Metamagic==Metamagic.Extend,"snapshot scalar parameters roundtrip");
  int prior=released;Call(hosts[0],"Release",f.Unit);Check(released==prior+1&&data.Spell==null&&data.Parameters==null,"release clears storage before delivery");Check(delivered.Caster.Unit==f.Unit&&recipient==f.Unit&&deliveredParams.DC==31&&!touch,"release owner and original DC retained");Call(hosts[0],"Release",f.Unit);Check(released==prior+1,"release cannot duplicate spell");
  for(int k=1;k<4;k++){Alchemy(book,k==2);ctx=Cast(f,2,k!=2);Call(hosts[k],"Arm");Check(!(bool)capture.Invoke(null,new object[]{ctx,new TargetWrapper(f.Unit),(AbilityApplyEffect)(k==2?new AbilityEffectRunAction():new AbilityEffectStickyTouch())}),"touch/extract accepted in matching host");int removedBefore=removed;Call(hosts[k],"Release",g.Unit);Check(touch==(k!=2)&&recipient==g.Unit,"retaliation preserves target and auto-hit flag");Check(removed==removedBefore+(k==3?0:1),"glaive storage discharges without deleting glaive");}
  Alchemy(book,false);ctx=Cast(f,3);Call(hosts[0],"Arm");ctx.Ability.Blueprint.CanTargetPoint=true;Check((bool)capture.Invoke(null,new object[]{ctx,new TargetWrapper(f.Unit),new AbilityEffectRunAction()}),"area cast excluded without interception");ctx.Ability.Blueprint.CanTargetPoint=false;ctx.IsForced=true;AccessTools.Method(patch,"MarkRelease").Invoke(null,new object[]{ctx});Check((bool)capture.Invoke(null,new object[]{ctx,new TargetWrapper(f.Unit),new AbilityEffectRunAction()}),"own released context not re-stored");ctx.IsForced=false;Check((bool)capture.Invoke(null,new object[]{ctx,new TargetWrapper(g.Unit),new AbilityEffectRunAction()}),"contingency does not swallow spell on ally");
  Call(hosts[0],"Capture",ctx);Call(hosts[0],"OnDeactivate");Check(State(hosts[0]).Spell==null&&!State(hosts[0]).Armed,"expiry removes stored reference and arming");
  h.Unpatch(AccessTools.Method(typeof(PsychicStoredSpell),"Deliver"),HarmonyPatchType.Prefix,h.Id);
  h.Patch(AccessTools.Method(typeof(AbilityData),"CanTarget"),prefix:new HarmonyMethod(typeof(Psychic031Tests),nameof(Legal)));
  h.Patch(AccessTools.Method(typeof(AbilityData),"CreateExecutionContext"),prefix:new HarmonyMethod(typeof(Psychic031Tests),nameof(Execution)));
  h.Patch(AccessTools.Method(typeof(PsychicStoredSpell),"Launch"),prefix:new HarmonyMethod(typeof(Psychic031Tests),nameof(Launch)));
  ctx=Cast(f,2);ctx.Params.CasterLevel=17;ctx.Params.DC=29;ctx.Params.Metamagic=Metamagic.Extend;
  AccessTools.Method(typeof(PsychicStoredSpell),"Deliver").Invoke(null,new object[]{ctx.Ability,ctx.Params,g.Unit,false});
  Check(launched!=null&&launched.Spell.Caster.Unit==f.Unit&&launched.SpellTarget.Unit==g.Unit,"actual native RuleCastSpell constructor keeps caster and target");
  Check(launched.Spell.Spellbook==null&&launched.Spell.SaveSpellbookSlot,"release has no spellbook resource or phrenic spellbook charge");
  Check(launched.Context.Params.DC==29&&launched.Context.Params.CasterLevel==17&&launched.Context.Params.Metamagic==Metamagic.Extend,"release execution context receives original parameter clone");Check(launched.Spell.MetamagicData.Has(Metamagic.Extend),"automatic original metamagic copied into release AbilityData");Check(launched.IsCutscene&&!launched.Context.ForceIgnoreSpellResist,"release bypasses second casting-failure check but retains SR");
  var touchbp=new BlueprintAbility{Type=AbilityType.Spell,ComponentsArray=Array.Empty<BlueprintComponent>()};touchbp.OnEnable();var sticky=new AbilityEffectStickyTouch();var reference=new BlueprintAbilityReference();AccessTools.Property(typeof(BlueprintReferenceBase),"Cached").SetValue(reference,touchbp,null);Set(sticky,"m_TouchDeliveryAbility",reference);ctx.Ability.Blueprint.ComponentsArray=new BlueprintComponent[]{sticky};ctx.Ability.Blueprint.OnEnable();
  AccessTools.Method(typeof(PsychicStoredSpell),"Deliver").Invoke(null,new object[]{ctx.Ability,ctx.Params,g.Unit,true});Check(launched.Spell.Blueprint==touchbp&&launched.Context.ForceAlwaysHit,"spite releases actual touch delivery without a new charge or touch roll");
  foreach(var b in hosts){Check(b.Components.Select(x=>x.SourceBlueprintComponentName).Distinct().Count()==b.Components.Count,"all actual runtime component/save names unique");foreach(var v in new[]{"Spell","Parameters","Armed"})Check(typeof(PsychicStoredSpellData).GetField(v).IsDefined(typeof(JsonPropertyAttribute),false),"native persisted storage field");}
  for(int cl=1;cl<=30;cl++)Check((int)AccessTools.Method(typeof(PsychicGreedWeapon),"Enhancement").Invoke(null,new object[]{cl})==(cl>=19?4:cl>=17?3:cl>=15?2:1),"glaive enhancement/burst threshold");
  var bab=new ModifiableValue(f.Unit.Stats,StatType.BaseAttackBonus);Set(f.Unit.Stats,"BaseAttackBonus",bab);f.Unit.Stats.AllStats.Add(bab);bab.BaseValue=9;var br=hosts[3].Components.Single(x=>x.SourceBlueprintComponent is PsychicGreedBAB);
  for(int cl=11;cl<=40;cl++){hosts[3].Context.Params.CasterLevel=cl;using(br.RequestEventContext())AccessTools.Method(typeof(PsychicGreedBAB),"OnTurnOn").Invoke(br.SourceBlueprintComponent,null);Check(bab.ModifiedValue==cl,"actual native BAB becomes CL including iterative threshold");using(br.RequestEventContext())AccessTools.Method(typeof(PsychicGreedBAB),"OnTurnOn").Invoke(br.SourceBlueprintComponent,null);Check(bab.ModifiedValue==cl,"repeated/restored turn-on cannot double BAB");var foreign=bab.AddModifier(2,(EntityFact)null,ModifierDescriptor.UntypedStackable);using(br.RequestEventContext())AccessTools.Method(typeof(PsychicGreedBAB),"OnTurnOff").Invoke(br.SourceBlueprintComponent,null);Check(bab.ModifiedValue==11,"ending removes own adjustment and preserves foreign BAB");foreign.Remove();}
  var blade=Asset<BlueprintAbility>(cache,"SpiritBoundBlade");Check(blade.GetComponent<AbilityVariants>().Variants.Length==8,"four supported properties each main/offhand");foreach(var v in blade.GetComponent<AbilityVariants>().Variants){Check(v.Parent==blade&&v.GetComponent<PsychicWeaponTarget>()!=null,"blade child remains target-checked and grouped");}
 }finally{h.UnpatchAll(h.Id);}
 PsychicExpansionTests.Inventory["new031Assertions"]=n;Console.WriteLine("PASS: "+n+" storage threshold/ownership/arming/native-component/save-parameter/release-once and bound-weapon checks. Final cast and buff-removal endpoints captured.");return n;
 }
}

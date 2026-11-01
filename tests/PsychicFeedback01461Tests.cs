using System;using System.Linq;using System.Reflection;using HarmonyLib;using Newtonsoft.Json.Linq;using System.Collections.Generic;using UniRx;
using Kingmaker;using Kingmaker.Blueprints;using Kingmaker.Blueprints.JsonSystem;using Kingmaker.EntitySystem.Entities;using Kingmaker.UnitLogic;using Kingmaker.UnitLogic.Abilities;using Kingmaker.UnitLogic.Abilities.Blueprints;using Kingmaker.RuleSystem.Rules.Abilities;using Kingmaker.UI.UnitSettings;using CallOfTheWild.Wrath.Prototype;
using Kingmaker.UI.MVVM._VM.ActionBar;using Kingmaker.EntitySystem;using Kingmaker.UnitLogic.ActivatableAbilities;using Kingmaker.UnitLogic.FactLogic;using Kingmaker.UnitLogic.Abilities.Components;
internal static partial class MagusBatch01454Tests{
 static void Feedback061(BlueprintsCache parent,Harmony h){
  var c=PsychicCache057(parent);WarmMercies058(c);if(Environment.GetEnvironmentVariable("COTW_01454_CASE")=="foreign-meta061")PrepareForeign061(c,h);Register(c,"MutationMind01457");Register(c,"PsychicMarauder01457");Register(c,"PsychicClassCompletion01458");
  foreach(var t in Main.GetTypes().Where(t=>(t.Name.EndsWith("01460Patch")||t.Name.EndsWith("01461Patch"))&&t.GetCustomAttributes(typeof(HarmonyPatch),false).Any()))h.CreateClassProcessor(t).Patch();
  EffectInputs060(h);h.CreateClassProcessor(T("PsychicCasting").GetNestedType("EmotionPatch",BindingFlags.NonPublic)).Patch();h.CreateClassProcessor(T("PsychicCasting").GetNestedType("ThoughtPatch",BindingFlags.NonPublic)).Patch();
  if(Environment.GetEnvironmentVariable("COTW_01454_CASE")=="menu061")WoundMenu061(c,h);else if(Environment.GetEnvironmentVariable("COTW_01454_CASE")=="foreign-meta061")ForeignCatalogue061(c,h);else if(Environment.GetEnvironmentVariable("COTW_01454_CASE")=="mimic-reentry061")MimicReentry061(c,h);else if(Environment.GetEnvironmentVariable("COTW_01454_CASE")=="batch061")HunterRegistration061(c,h);else if(Environment.GetEnvironmentVariable("COTW_01454_CASE")=="hunter-lifecycle061")HunterLifecycle061(c,h);else if(Environment.GetEnvironmentVariable("COTW_01454_CASE")=="hunter-publication061")HunterPublication061(parent,h);else if(Environment.GetEnvironmentVariable("COTW_01454_CASE")=="surge-direct061")Surge060(c,h);else MimicDiagnostic061(c,h);
  var owned=new List<SimpleBlueprint>();c.ForEachLoaded((id,bp)=>{if(bp.name!=null&&bp.name.Contains("01461")&&bp.name.StartsWith("CotWWrathPsychic",StringComparison.Ordinal))owned.Add(bp);});
  PsychicExpansionTests.Inventory["newOwnedBlueprints01461"]=new JArray(owned.OrderBy(b=>b.AssetGuid.ToString()).Select(b=>new JObject{["id"]=b.AssetGuid.ToString(),["name"]=b.name,["type"]=b.GetType().FullName}));
  PsychicExpansionTests.Inventory["psychicFeedback01461Contracts"]=new JObject{["passed"]=true,["assertions"]=checks,["coverage"]=new JArray(Coverage),["gameLaunchTested"]=false};
 }
 static bool MenuNoTick061(ref IObservable<UniRx.Unit> __result){__result=Observable.Never<UniRx.Unit>();return false;}
 static bool MenuLabel061(EntityFact __instance,ref string __result){__result=__instance.Blueprint.name;return false;}
 static bool MenuSprite061(ref UnityEngine.Sprite __result){__result=null;return false;}
 static bool MenuColor061(ref UnityEngine.Color __result){__result=UnityEngine.Color.white;return false;}
 static void WoundMenu061(BlueprintsCache c,Harmony h){
  var owner=EquipmentUnit(c,13,Psychic058);Grant(owner,c,"AgonizingWoundFeature01458");var menu=A<BlueprintAbility>(c,"AgonizingWoundMenu01460");var target=A<BlueprintAbility>(c,"AgonizingWoundTargetAbility01458");var resource=A<BlueprintAbilityResource>(c,"AgonizingWoundResource01458");
  Check(!owner.Abilities.Enumerable.Any(a=>a.Blueprint==target)&&owner.Abilities.Enumerable.Count(a=>a.Blueprint==menu)==1,"061 native feature grants only parent, never standalone Target");
  Check(menu.GetComponent<AbilityVariants>().Variants.SequenceEqual(new[]{target})&&target.Parent==menu,"061 Target remains a genuine native parent variant");
  Check(Enumerable.Range(0,4).All(i=>c.Load(Id("AgonizingWoundMode"+i+"Ability01460"))==null)&&Main.GetType("CallOfTheWild.Wrath.Prototype.SelectAgonizingMode01460")==null,"061 obsolete four one-shot shortcuts and serialized action are not published");
  h.Patch(AccessTools.Method(typeof(MainThreadDispatcher),nameof(MainThreadDispatcher.LateUpdateAsObservable)),prefix:new HarmonyMethod(typeof(MagusBatch01454Tests),nameof(MenuNoTick061)));
  h.Patch(AccessTools.Method(typeof(Kingmaker.UI.Common.UIUtility),"GetDecorationBorderByIndex"),prefix:new HarmonyMethod(typeof(MagusBatch01454Tests),nameof(MenuSprite061)));
  h.Patch(AccessTools.Method(typeof(Kingmaker.UI.Common.UIUtility),"GetDecorationColorByIndex"),prefix:new HarmonyMethod(typeof(MagusBatch01454Tests),nameof(MenuColor061)));
  foreach(string p in new[]{"Name","Description"})h.Patch(AccessTools.PropertyGetter(typeof(EntityFact),p),prefix:new HarmonyMethod(typeof(MagusBatch01454Tests),nameof(MenuLabel061)));
  var ability=owner.Abilities.Enumerable.Single(a=>a.Blueprint==menu).Data;var slot=new MechanicActionBarSlotAbility{Ability=ability,Unit=owner};Check(!slot.IsBad(),"061 real granted parent fact creates a valid native ability slot");var vm=new ActionBarSlotVM(slot);int initial=owner.Resources.GetResourceAmount(resource);vm.OnMainClick();
  Check(vm.ConvertedVm.Value.Slots.Count==5&&slot.GetResource()==initial,"061 actual native main click opens one Target plus four persistent modes and real counter");
  var targetSlot=vm.ConvertedVm.Value.Slots.Select(v=>v.MechanicActionBarSlot).OfType<MechanicActionBarSlotSpontaneusConvertedSpell>().Single();Check(targetSlot.Spell.Blueprint==target&&!targetSlot.IsBad(),"061 native converted Target inherits the active parent and is usable");
  var toggles=vm.ConvertedVm.Value.Slots.Select(v=>v.MechanicActionBarSlot).OfType<MechanicActionBarSlotActivableAbility>().ToArray();Check(toggles.Length==4&&toggles.All(s=>!s.ActivatableAbility.Blueprint.DeactivateImmediately&&!s.IsBad()),"061 mixed popup uses actual native persistent activatables");
  foreach(var t in toggles){ToggleOn(t.ActivatableAbility);Check(t.ActivatableAbility.IsOn&&t.ActivatableAbility.IsStarted,"061 native mode starts and remains on: "+t.ActivatableAbility.Blueprint.name);Check(toggles.Count(s=>s.ActivatableAbility.IsOn)==1&&owner.Resources.GetResourceAmount(resource)==initial,"061 mutually exclusive native mode switch costs no daily use");}
  for(int round=0;round<4;round++){toggles.Last().ActivatableAbility.OnNewRound();Check(toggles.Last().ActivatableAbility.IsOn&&toggles.Last().ActivatableAbility.AppliedBuff!=null&&owner.Resources.GetResourceAmount(resource)==initial,"061 actual native round tick retains enabled mode without spending a use: "+round);}
  vm.CloseConvert();vm.OnShowConvertRequest();Check(vm.ConvertedVm.Value.Slots.OfType<ActionBarSlotVM>().Select(v=>v.MechanicActionBarSlot).OfType<MechanicActionBarSlotActivableAbility>().Count(s=>s.ActivatableAbility.IsOn)==1,"061 reopened popup preserves the real enabled mode");vm.CloseConvert();
  owner.Resources.Spend(resource,initial);vm.OnMainClick();Check(vm.ConvertedVm.Value.Slots.Count==5&&slot.GetResource()==0,"061 zero-use parent still opens for disabling modes");ToggleOff(toggles.Last().ActivatableAbility);Check(toggles.All(s=>!s.ActivatableAbility.IsOn),"061 disabling selected mode leaves no wound mode active");vm.Dispose();
  var bar=(ActionBarVM)System.Runtime.Serialization.FormatterServices.GetUninitializedObject(typeof(ActionBarVM));Set(bar,"GroupAbilities",new List<ActionBarSlotVM>());AccessTools.Method(typeof(ActionBarVM),"CollectAbilities").Invoke(bar,new object[]{owner});
  Check(bar.GroupAbilities.Count(v=>(v.MechanicActionBarSlot as MechanicActionBarSlotAbility)?.Ability.Blueprint==menu)==1&&!bar.GroupAbilities.Any(v=>(v.MechanicActionBarSlot as MechanicActionBarSlotAbility)?.Ability.Blueprint==target),"061 actual native CollectAbilities exposes exactly one wound parent and no standalone Target");
  Check(!bar.GroupAbilities.Select(v=>v.MechanicActionBarSlot).OfType<MechanicActionBarSlotActivableAbility>().Any(s=>toggles.Any(t=>t.ActivatableAbility==s.ActivatableAbility)),"061 native expanded ability list hides wound modes while popup retains them");foreach(var view in bar.GroupAbilities)view.Dispose();
 }
 static void MimicDiagnostic061(BlueprintsCache c,Harmony h){
  foreach(var method in new[]{AccessTools.PropertyGetter(typeof(AbilityData),"IsAvailableForCast"),AccessTools.Method(typeof(AbilityData),"GetAvailableForCastCount"),AccessTools.Method(typeof(Spellbook),"GetAvailableForCastSpellCount")}){
   var p=Harmony.GetPatchInfo(method);Console.WriteLine("061 AVAIL PATCHES "+method+" prefix="+string.Join(",",p?.Prefixes.Select(x=>x.PatchMethod.DeclaringType.FullName+"."+x.PatchMethod.Name)??Enumerable.Empty<string>())+" post="+string.Join(",",p?.Postfixes.Select(x=>x.PatchMethod.DeclaringType.FullName+"."+x.PatchMethod.Name)??Enumerable.Empty<string>()));
  }
  foreach(var meta in new[]{Metamagic.Empower,Metamagic.Maximize,Metamagic.Quicken,Metamagic.Reach,Metamagic.Extend,Metamagic.Persistent,Metamagic.Selective,Metamagic.Bolstered,(Metamagic)1024,(Metamagic)2048,(Metamagic)(1<<28),(Metamagic)(1<<29)}){
   var owner=EquipmentUnit(c,13,Psychic058);owner.Stats.Intelligence.BaseValue=20;var victim=EquipmentUnit(c,13);var pool=Pool058(c,owner);var book=Book058(c,owner);var spell=Known058(c,book,1,"MindThrust1");book.RestoreSpontaneousSlots(1,5);
   Console.WriteLine("061 BEFORE "+meta+" pool="+owner.Resources.GetResourceAmount(pool)+" slots="+book.GetSpontaneousSlots(1)+" count="+spell.GetAvailableForCastCount()+" state="+owner.State.CanCast+" available="+spell.IsAvailableForCast+" disabled="+spell.TemporarilyDisabled);
   Check(spell.IsAvailableForCast,"061 real native baseline spell is available before Mimic: "+meta);
   Check(spell.IsAvailable&&book.CanSpend(spell,false),"061 actual native spellbook payment and full availability before Mimic: "+meta);
   Grant(owner,MimicLeaf058(c,meta));var toggle=MimicToggle058(c,owner,meta);ToggleOn(toggle);
   Console.WriteLine("061 AFTER "+meta+" pool="+owner.Resources.GetResourceAmount(pool)+" count="+spell.GetAvailableForCastCount()+" available="+spell.IsAvailableForCast+" disabled="+spell.TemporarilyDisabled+" action="+spell.ActionType+" hasMeta="+spell.HasMetamagic(meta));
   Check(spell.IsAvailableForCast,"061 real native spell remains castable after Mimic activation: "+meta);
   Console.WriteLine("061 UI avail="+spell.IsAvailable+" canSpend="+book.CanSpend(spell,false)+" slot="+book.GetSpellLevel(spell)+" param="+spell.CalculateParams().SpellLevel);
   Check(spell.IsAvailable&&book.CanSpend(spell,false),"061 actual native spellbook payment and full availability after Mimic: "+meta);
   var spellSlot=new MechanicActionBarSlotSpontaneousSpell(spell){Unit=owner};Check(!spellSlot.IsBad()&&!spellSlot.IsDisabled(spellSlot.GetResource()),"061 actual native spontaneous actionbar slot remains enabled after Mimic: "+meta);
   bool applies=spell.HasMetamagic(meta);Console.WriteLine("061 APPLIES "+meta+" "+applies);
   int before=owner.Resources.GetResourceAmount(pool);var cast=Linked058(spell,victim);NativeCast058(cast,h);
   Console.WriteLine("061 CAST "+meta+" success="+cast.Success+" forceFail="+cast.ForceFail+" cost="+(before-owner.Resources.GetResourceAmount(pool))+" mask="+cast.Context.Params.Metamagic);
   Check(cast.Success&&cast.Context.HasMetamagic(meta)==applies,"061 actual native cast succeeds and retains paid Mimic: "+meta);
  }
  MimicSpellMatrix061(c,h);
  MimicSurgePair061(c,h);
 }
 static void MimicSurgePair061(BlueprintsCache c,Harmony h){
  foreach(int budget in new[]{3,5,6}){
   var owner=EquipmentUnit(c,20,Psychic058);var target=EquipmentUnit(c,20);var pool=Pool058(c,owner);var book=Book058(c,owner);var low=Known058(c,book,1,"MindThrust1");var high=Known058(c,book,2,"MindThrust2");Grant(owner,c,"DualAmplificationFeature01458");Grant(owner,c,"UndercastSurgeFeature01458");Grant(owner,MimicLeaf058(c,Metamagic.Empower));ToggleOn(Toggle(owner,c,"UndercastSurge101458Toggle"));ToggleOn(MimicToggle058(c,owner,Metamagic.Empower));owner.Resources.Spend(pool,owner.Resources.GetResourceAmount(pool));owner.Resources.Restore(pool,budget);book.RestoreSpontaneousSlots(1,3);
   Check(low.IsAvailable&&book.CanSpend(low,false),"061 actual native Dual Mimic/Surge keeps base spell available at budget "+budget);bool promoted=budget==3||budget==6,empowered=budget>=4;var cast=Linked058(low,target);Check(cast.Spell.Blueprint==(promoted?high.Blueprint:low.Blueprint),"061 actual native constructor allocates competing Dual effects by newest affordable mode at budget "+budget);NativeCast058(cast,h);Check(cast.Context.HasMetamagic(Metamagic.Empower)==empowered&&owner.Resources.GetResourceAmount(pool)==budget-(promoted?2:0)-(empowered?4:0),"061 actual native combined cast pays each allocated effect exactly once at budget "+budget);
   int original=book.GetSpontaneousSlots(1),higher=book.GetSpontaneousSlots(2);Check(book.Spend(cast.Spell,false)&&book.GetSpontaneousSlots(1)==original-1&&book.GetSpontaneousSlots(2)==higher,"061 actual native combined Mimic/Surge retains original low slot payment at budget "+budget);
  }
 }
 static void MimicSpellMatrix061(BlueprintsCache c,Harmony h){
  foreach(int level in new[]{11,13,17,20})foreach(var meta in new[]{Metamagic.Empower,Metamagic.Maximize,Metamagic.Quicken,Metamagic.Reach,Metamagic.Extend,Metamagic.Persistent,Metamagic.Selective,Metamagic.Bolstered,(Metamagic)1024,(Metamagic)2048,(Metamagic)(1<<28),(Metamagic)(1<<29)}){
   var owner=EquipmentUnit(c,level,Psychic058);var victim=EquipmentUnit(c,level);var pool=Pool058(c,owner);var book=Book058(c,owner);Grant(owner,MimicLeaf058(c,meta));var mode=MimicToggle058(c,owner,meta);ToggleOn(mode);
   foreach(string token in new[]{"MindThrust1","MindThrust3","MentalBarrier1","ThoughtShield1"}){
    int circle=token=="MindThrust1"?1:token=="MindThrust3"?3:2;var spell=Known058(c,book,circle,token);book.RestoreSpontaneousSlots(circle,3);owner.Resources.Restore(pool,99);int before=owner.Resources.GetResourceAmount(pool);
    int cost=meta==(Metamagic)(1<<28)||meta==(Metamagic)(1<<29)?1:meta.DefaultCost();bool compatible=meta==(Metamagic)(1<<29)||(spell.Blueprint.AvailableMetamagic&meta)!=0;bool expected=compatible&&circle+cost<=book.MaxSpellLevel;string label=level+"/"+meta+"/"+token;
    var slot=new MechanicActionBarSlotSpontaneousSpell(spell){Unit=owner};Check(spell.IsAvailableForCast&&spell.IsAvailable&&book.CanSpend(spell,false)&&!slot.IsBad()&&!slot.IsDisabled(slot.GetResource()),"061 multi-level native availability/actionbar and ordinary payment survive enabled Mimic: "+label);
    Check(spell.HasMetamagic(meta)==expected&&owner.Resources.GetResourceAmount(pool)==before,"061 supported and inapplicable or over-level native Mimic previews preserve ordinary casting without preview cost: "+label);
    var cast=Linked058(spell,token.StartsWith("MindThrust")?victim:owner);NativeCast058(cast,h);Check(cast.Success&&cast.Context.HasMetamagic(meta)==expected&&owner.Resources.GetResourceAmount(pool)==before-(expected?2*cost:0),"061 actual native cast applies only eligible Mimic and pays exact amount: "+label);
    int slots=book.GetSpontaneousSlots(circle);Check(book.Spend(cast.Spell,false)&&book.GetSpontaneousSlots(circle)==slots-1&&spell.SpellLevel==circle,"061 actual native multi-level Mimic payment keeps original spell slot level: "+label);
   }
   ToggleOff(mode);
  }
 }
 static AbilityData ProbeAbility061;static int ProbeDepth061,ProbeMaximum061;
 static void LevelReentry061(AbilityData __0){
  var spell=__0;if(!ReferenceEquals(spell,ProbeAbility061))return;ProbeDepth061++;ProbeMaximum061=Math.Max(ProbeMaximum061,ProbeDepth061);
  try{if(ProbeDepth061>2)throw new InvalidOperationException("061 controlled native metadata reentry bound");bool query=spell.HasMetamagic(Metamagic.Quicken);}finally{ProbeDepth061--;}
 }
 static void MimicReentry061(BlueprintsCache c,Harmony h){
  var owner=EquipmentUnit(c,13,Psychic058);owner.Stats.Intelligence.BaseValue=20;var victim=EquipmentUnit(c,13);var pool=Pool058(c,owner);var book=Book058(c,owner);var spell=Known058(c,book,1,"MindThrust1");book.RestoreSpontaneousSlots(1,3);
  Grant(owner,MimicLeaf058(c,Metamagic.Quicken));var toggle=MimicToggle058(c,owner,Metamagic.Quicken);ToggleOn(toggle);
  var method=AccessTools.Method(typeof(Spellbook),nameof(Spellbook.GetSpellLevel),new[]{typeof(AbilityData)});h.Patch(method,prefix:new HarmonyMethod(typeof(MagusBatch01454Tests),nameof(LevelReentry061)));
  bool current=Main.GetType("CallOfTheWild.Wrath.Prototype.MimicCatalogue01461")!=null;ProbeAbility061=spell;ProbeDepth061=ProbeMaximum061=0;Exception error=null;bool meta=false;
  try{try{meta=spell.HasMetamagic(Metamagic.Quicken);}catch(Exception e){error=e;}
   if(current){Check(error==null&&meta&&ProbeMaximum061<=2,"061 actual native metadata and special-metamagic lookup tolerate a reentrant caller without disabling the spell");int before=owner.Resources.GetResourceAmount(pool);Check(spell.IsAvailable&&book.CanSpend(spell,false),"061 reentrant native lookup preserves real availability and slot payment");var cast=Linked058(spell,victim);NativeCast058(cast,h);Check(cast.Success&&cast.Context.HasMetamagic(Metamagic.Quicken)&&owner.Resources.GetResourceAmount(pool)==before-8,"061 guarded lookup still applies and pays real Mimic during actual native cast");}
   else Check(error!=null&&error.ToString().Contains("061 controlled native metadata reentry bound")&&ProbeMaximum061==3,"061 real060 DLL control exposes the deliberately injected native metadata feedback loop before an unbounded stack overflow");
  }finally{ProbeAbility061=null;h.Unpatch(method,AccessTools.Method(typeof(MagusBatch01454Tests),nameof(LevelReentry061)));}
 }
}

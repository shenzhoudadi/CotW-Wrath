using System;using System.Linq;using System.Reflection;using HarmonyLib;using Newtonsoft.Json.Linq;
using Kingmaker.Blueprints;using Kingmaker.Blueprints.Classes;using Kingmaker.Blueprints.Classes.Selection;using Kingmaker.Blueprints.JsonSystem;using Kingmaker.EntitySystem.Entities;using Kingmaker.UnitLogic;using Kingmaker.UnitLogic.Abilities;using Kingmaker.UnitLogic.Abilities.Blueprints;using Kingmaker.UnitLogic.FactLogic;using Kingmaker.UI.UnitSettings;
internal static partial class MagusBatch01454Tests{
 static void Feedback062(BlueprintsCache parent,Harmony h){
  var c=PsychicCache057(parent);WarmMercies058(c);PrepareForeign061(c,h);
  var ext=TttLibrary061.GetType("TabletopTweaks.Core.MechanicsChanges.MetamagicExtention",true);
  foreach(string name in new[]{"CodexLib.Patch_ResourceOverride","CodexLib.Patches.Patch_AbilityIsFullRound"})h.CreateClassProcessor(ForeignLibrary061.GetType(name,true)).Patch();
  h.CreateClassProcessor(TttLibrary061.GetType("TabletopTweaks.Core.NewEvents.IAbilityGetCommandTypeHandler",true).GetNestedType("AbilityData_ActionType_IAbilityGetCommandTypeHandler_Patch",BindingFlags.NonPublic|BindingFlags.Public)).Patch();
  foreach(var item in new[]{new[]{1<<12,1},new[]{1<<26,4}}){var method=ext.GetMethods(BindingFlags.Public|BindingFlags.Static).Where(m=>m.Name=="RegisterMetamagic").OrderByDescending(m=>m.GetParameters().Length).First();var args=method.GetParameters().Select(p=>p.Name=="metamagic"?(object)(Metamagic)item[0]:p.Name=="defaultCost"?item[1]:p.Name=="favoriteMetamaticAdjustment"?1:null).ToArray();method.Invoke(null,args);}
  ForeignFeat061(c,"ForeignIntensified062",(Metamagic)(1<<12));ForeignFeat061(c,"ForeignTwin062",(Metamagic)(1<<26));
  Register(c,"MutationMind01457");Register(c,"PsychicMarauder01457");Register(c,"PsychicClassCompletion01458");
  foreach(var t in Main.GetTypes().Where(t=>(t.Name.EndsWith("01460Patch")||t.Name.EndsWith("01461Patch"))&&t.GetCustomAttributes(typeof(HarmonyPatch),false).Any()))h.CreateClassProcessor(t).Patch();
  EffectInputs060(h);h.CreateClassProcessor(T("PsychicCasting").GetNestedType("EmotionPatch",BindingFlags.NonPublic)).Patch();h.CreateClassProcessor(T("PsychicCasting").GetNestedType("ThoughtPatch",BindingFlags.NonPublic)).Patch();
  foreach(string name in new[]{"PsychicDivideMindActionPatch","PsychicDivideMindPatch","SpontaneousMetafocusTimingPatch","DazzlingTrainingActionPatch","DazzlingTrainingFullRoundPatch"})Feedback01450Tests.PatchOnce(h,T(name));
  string mode=Environment.GetEnvironmentVariable("COTW_01454_CASE");
  if(mode=="physical062")PhysicalFx062(c,h);else if(mode=="isolation062")Isolation062(c,h);else ProbeMimic062(c,h);
  PsychicExpansionTests.Inventory["psychicFeedback01462Contracts"]=new JObject{["passed"]=true,["assertions"]=checks,["coverage"]=new JArray(Coverage),["gameLaunchTested"]=false};
 }
 static int MimicGlobalQueries062,NativeGlobalQueries062;
 static bool GlobalQuery062(Kingmaker.Designers.Mechanics.Facts.AutoMetamagic __0){
  if(__0.GetType().Name=="MimicMetamagic01458"){MimicGlobalQueries062++;throw new InvalidOperationException("062 controlled global Mimic query exposure");}
  NativeGlobalQueries062++;return true;
 }
 static Spellbook MetadataFaultBook062;
 static int MetadataFaultHits062;
 static void MetadataFault062(Spellbook __instance){if(ReferenceEquals(__instance,MetadataFaultBook062)&&new System.Diagnostics.StackTrace().GetFrames().Any(f=>f.GetMethod().Name=="RawEligible")){MetadataFaultHits062++;throw new InvalidOperationException("062 controlled optional Mimic metadata fault");}}
 static System.Collections.IList AutoEntries062(UnitEntityData u){var part=u.Get<Kingmaker.UnitLogic.Parts.UnitPartSpecialMetamagic>();return part==null?null:(System.Collections.IList)AccessTools.Field(part.GetType(),"m_MetamagicOnSpellList").GetValue(part);}
 static bool IsMimicEntry062(object entry)=>AccessTools.Field(entry.GetType(),"Item2").GetValue(entry).GetType().Name=="MimicMetamagic01458";
 static void Isolation062(BlueprintsCache c,Harmony h){
  var owner=EquipmentUnit(c,15,Psychic058);owner.Progression.Classes[0].AddArchetype(A<BlueprintArchetype>(c,"MutationMindArchetype01457"));owner.Stats.Intelligence.BaseValue=24;
  var victim=EquipmentUnit(c,15);var pool=Pool058(c,owner);var book=Book058(c,owner);var spell=Known058(c,book,1,"MindThrust1");book.RestoreSpontaneousSlots(1,9);Grant(owner,MimicLeaf058(c,(Metamagic)(1<<29)));var mode=MimicToggle058(c,owner,(Metamagic)(1<<29));ToggleOn(mode);
  var auto=new Kingmaker.Designers.Mechanics.Facts.AutoMetamagic{Metamagic=Metamagic.Maximize};
  var native=new Kingmaker.UnitLogic.Buffs.Blueprints.BlueprintBuff{AssetGuid=Id("NativeMaximizeIsolation062"),name="NativeMaximizeIsolation062",ComponentsArray=new BlueprintComponent[]{auto}};native.OnEnable();c.AddCachedBlueprint(native.AssetGuid,native);var ordinaryFact=owner.Buffs.AddBuff(native,owner,null);
  var method=AccessTools.Method(typeof(Kingmaker.Designers.Mechanics.Facts.AutoMetamagic),"ShouldApplyTo");var patch=new HarmonyMethod(typeof(MagusBatch01454Tests),nameof(GlobalQuery062)){priority=Priority.First};h.Patch(method,prefix:patch);
  try{
   bool available=false;Exception exposure=null;try{available=spell.IsAvailable;}catch(Exception e){exposure=e;}
   Check(exposure==null&&available&&MimicGlobalQueries062==0,"062 isolated native availability never enters global Mimic AutoMetamagic callback; controlled original061 exposure is rejected");
   Check(NativeGlobalQueries062>0&&spell.HasMetamagic(Metamagic.Maximize),"062 unrelated native AutoMetamagic continues through original global query");
   Check(spell.HasMetamagic(Metamagic.Maximize|(Metamagic)(1<<29))&&!spell.HasMetamagic(Metamagic.Maximize|Metamagic.Empower|(Metamagic)(1<<29)),"062 composite native and Mimic flags merge without granting an unrelated bit");
   Check(AutoEntries062(owner).Cast<object>().All(e=>!IsMimicEntry062(e)),"062 native active registry contains no Mimic entry");
   var buff=mode.AppliedBuff;var comp=buff.Blueprint.ComponentsArray.OfType<Kingmaker.Designers.Mechanics.Facts.AutoMetamagic>().Single();buff.Deactivate();owner.Ensure<Kingmaker.UnitLogic.Parts.UnitPartSpecialMetamagic>().Add(buff,comp);buff.Activate();
   Check(AutoEntries062(owner).Count==1&&AutoEntries062(owner).Cast<object>().All(e=>!IsMimicEntry062(e))&&ordinaryFact.IsActive,"062 native buff reactivation removes stale own registry entry and preserves unrelated fact");
   ToggleOff(mode);Check(spell.HasMetamagic(Metamagic.Maximize)&&!spell.HasMetamagic((Metamagic)(1<<29)),"062 disabling Mimic retains native metamagic and revokes only its own preview");ToggleOn(mode);
  }finally{h.Unpatch(method,patch.method);}
  var max=AccessTools.Method(typeof(Spellbook),"GetSpellLevel",new[]{typeof(AbilityData)});var fault=AccessTools.Method(typeof(MagusBatch01454Tests),nameof(MetadataFault062));h.Patch(max,prefix:new HarmonyMethod(fault));MetadataFaultBook062=book;
  try{int points=owner.Resources.GetResourceAmount(pool);Check(spell.IsAvailable&&!spell.HasMetamagic((Metamagic)(1<<29)),"062 optional Mimic metadata failure preserves ordinary native availability without free metamagic");var cast=Linked058(spell,victim);NativeCast058(cast,h);Check(cast.Success&&!cast.Context.HasMetamagic((Metamagic)(1<<29))&&owner.Resources.GetResourceAmount(pool)==points,"062 optional metadata failure permits actual ordinary cast without Mimic charge");int slots=book.GetSpontaneousSlots(1);Check(book.Spend(cast.Spell,false)&&book.GetSpontaneousSlots(1)==slots-1,"062 metadata fallback still pays original native spell slot");}finally{MetadataFaultBook062=null;h.Unpatch(max,fault);}
  Check(MetadataFaultHits062>0,"062 controlled optional metadata fault actually reached native getter from Mimic eligibility");ToggleOff(mode);ordinaryFact.Remove();MimicSurgePair061(c,h);
 }
 static void ProbeMimic062(BlueprintsCache c,Harmony h){
  var pairs=new[]{new[]{Metamagic.Bolstered,(Metamagic)(1<<29)},new[]{Metamagic.Intensified,(Metamagic)(1<<26)}};
  foreach(bool mutation in new[]{false,true}){
   var owner=EquipmentUnit(c,15,Psychic058);owner.Progression.Classes[0].AddArchetype(A<BlueprintArchetype>(c,"MutationMindArchetype01457"));owner.Stats.Intelligence.BaseValue=24;
   var victim=EquipmentUnit(c,15);var pool=Pool058(c,owner);var book=Book058(c,owner);
   for(int tier=0;tier<2;tier++){
    var pair=pairs[tier];var root=A<BlueprintFeatureSelection>(c,"MimicMetamagic"+(tier+1)+"Feature01458");var first=root.AllFeatures.Single(f=>f.GetComponent<AddFacts>()?.Facts.Contains(MimicLeaf058(c,pair[0]))==true);var second=((BlueprintFeatureSelection)first).AllFeatures.Single(f=>f==MimicLeaf058(c,pair[1]));
    Check(Pick058(owner,root,first),"062 native level-up parent pool accepts exact tier "+(tier+1)+" first choice");Grant(owner,root);Grant(owner,first);
    Check(Pick058(owner,(BlueprintFeatureSelection)first,second),"062 native second choice accepts selected pair without duplicate flag");Grant(owner,second);
   }
   Check(pairs.SelectMany(a=>a).All(m=>owner.ActivatableAbilities.Enumerable.Any(a=>a.Blueprint==MimicToggle058(c,owner,m).Blueprint)),"062 actual two tier first/second selectors grant all four player modes");
   Grant(owner,c,"PhysicalMutationFeature01457");if(mutation)ToggleOn(owner.ActivatableAbilities.Enumerable.Single(a=>a.Blueprint.AssetGuid==Id("PhysicalMutationToggle01458")));
   Grant(owner,c,"DarkHalfFeature01458");
   foreach(var meta in pairs.SelectMany(a=>a)){var toggle=MimicToggle058(c,owner,meta);ToggleOn(toggle);
    foreach(var ability in owner.Abilities.Enumerable)Console.WriteLine("062 actual granted ability "+ability.Blueprint.name+" nativeCan="+AccessTools.PropertyGetter(typeof(AbilityData),"CanBeCastByCaster").Invoke(ability.Data,null));
    foreach(string token in new[]{"MindThrust1","MindThrust3","MentalBarrier1","ThoughtShield1"}){
     int level=token=="MindThrust1"?1:token=="MindThrust3"?3:2;var spell=Known058(c,book,level,token);book.RestoreSpontaneousSlots(level,4);owner.Resources.Restore(pool,99);
     // Tome's current UpdateSpells enables Twin on all known spell roots.
     // Its initializer/assets are not launched by this managed fixture.
     spell.Blueprint.AvailableMetamagic|=(Metamagic)(1<<26);
     Console.WriteLine("062 QUERY level15 MutationMind "+meta+"/"+token+" major="+owner.HasFact(A<BlueprintFeature>(c,"MajorAmplificationsFeature01460")));
     var slot=new MechanicActionBarSlotSpontaneousSpell(spell){Unit=owner};Console.WriteLine("062 availability="+spell.IsAvailable+" availableCast="+spell.IsAvailableForCast+" action="+spell.ActionType+" disabled="+slot.IsDisabled(slot.GetResource())+" meta="+spell.HasMetamagic(meta));
     Check(spell.IsAvailable&&spell.IsAvailableForCast&&!slot.IsDisabled(slot.GetResource()),"062 exact level15 MutationMind pair native availability: "+meta+"/"+token);
     int adjustment=meta==(Metamagic)(1<<26)?4:1;
     bool expected=(meta==(Metamagic)(1<<29)||(spell.Blueprint.AvailableMetamagic&meta)!=0)&&level+adjustment<=book.MaxSpellLevel;
     Check(spell.HasMetamagic(meta)==expected,"062 independent four-flag availability/level oracle agrees with native preview: "+meta+"/"+token);
     int points=owner.Resources.GetResourceAmount(pool);var cast=Linked058(spell,token.StartsWith("MindThrust")?victim:owner);NativeCast058(cast,h);int price=2*adjustment;Check(cast.Success&&cast.Context.HasMetamagic(meta)==expected&&owner.Resources.GetResourceAmount(pool)==points-(expected?price:0),"062 exact pair native cast/context and real point price: "+meta+"/"+token);
     int slots=book.GetSpontaneousSlots(level);Check(book.Spend(cast.Spell,false)&&book.GetSpontaneousSlots(level)==slots-1&&spell.SpellLevel==level,"062 exact pair native original spell-slot payment: "+meta+"/"+token);
     owner.Resources.Spend(pool,owner.Resources.GetResourceAmount(pool));Check(spell.IsAvailable&&!spell.HasMetamagic(meta),"062 exact enabled mode with zero Phrenic points preserves ordinary native cast: "+meta+"/"+token);
     var ordinary=Linked058(spell,token.StartsWith("MindThrust")?victim:owner);NativeCast058(ordinary,h);Check(ordinary.Success&&!ordinary.Context.HasMetamagic(meta)&&owner.Resources.GetResourceAmount(pool)==0,"062 zero-point real ordinary cast neither gains free Mimic nor fails: "+meta+"/"+token);
     var conditions=(sbyte[])AccessTools.Field(owner.State.GetType(),"m_Conditions").GetValue(owner.State);int forbidden=(int)Kingmaker.UnitLogic.UnitCondition.SpellcastingForbidden;var prior=conditions[forbidden];conditions[forbidden]=1;
     try{Check(!spell.IsAvailableForCast,"062 enabled Mimic retains unrelated native SpellcastingForbidden restriction: "+meta+"/"+token);}finally{conditions[forbidden]=prior;}
    }
    Check(toggle.IsOn&&owner.ActivatableAbilities.Enumerable.Count(a=>a.IsOn&&a.Blueprint.name.Contains("MimicMetamagicOption"))==1,"062 native direct switch retains only the newest enabled Mimic mode");
   }
  }
 }
}

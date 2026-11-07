using System;using System.Linq;using System.IO;using System.Reflection;using HarmonyLib;
using Kingmaker.Blueprints;using Kingmaker.Blueprints.Classes;using Kingmaker.Blueprints.Classes.Selection;using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.UnitLogic;using Kingmaker.UnitLogic.FactLogic;using Kingmaker.UnitLogic.Abilities;using Kingmaker.UnitLogic.Abilities.Blueprints;
using CallOfTheWild.Wrath.Prototype;
internal static partial class MagusBatch01454Tests{
 static Metamagic ForeignFlag061=(Metamagic)(1<<13);
 static Assembly ForeignLibrary061;
 static Assembly TttLibrary061;
 static bool FailLateMimic061;
 static void LatePublishFailure061(SimpleBlueprint __1){if(FailLateMimic061&&__1.AssetGuid==Id("MimicMetamagicOption65536Feature01458")){FailLateMimic061=false;throw new InvalidOperationException("061 deliberate late Mimic cache failure after insertion");}}
 static BlueprintFeature ForeignFeat061(BlueprintsCache c,string token,Metamagic flag){
  var f=new BlueprintFeature{AssetGuid=Id(token),name="ForeignFixture"+token,ComponentsArray=new BlueprintComponent[]{new AddMetamagicFeat{Metamagic=flag}},Ranks=1};
  f.OnEnable();c.AddCachedBlueprint(f.AssetGuid,f);return f;
 }
 static void PrepareForeign061(BlueprintsCache c,Harmony h){
  string path=Path.Combine(Root(),"work/court-bard01445/upstream-CodexLib.dll");ForeignLibrary061=Assembly.LoadFrom(path);
  Assembly.LoadFrom(Path.Combine(Root(),"work/repair01440/references/UnityModManager.dll"));
  var prefix=ForeignLibrary061.GetType("CodexLib.Patches.Patch_MetamagicExt",true).GetMethod("Prefix2",BindingFlags.Public|BindingFlags.Static);
  h.Patch(AccessTools.Method(typeof(MetamagicHelper),nameof(MetamagicHelper.DefaultCost)),prefix:new HarmonyMethod(prefix));
  Check(ForeignFlag061.DefaultCost()==3,"061 actual unmodified CodexLib DefaultCost prefix resolves foreign Dazing adjustment");
  TttLibrary061=Assembly.LoadFrom(Path.Combine(Root(),"work/repair01440/dependencies/TabletopTweaks-Core/TabletopTweaks-Core/TabletopTweaks-Core.dll"));
  var extension=TttLibrary061.GetType("TabletopTweaks.Core.MechanicsChanges.MetamagicExtention",true);
  h.CreateClassProcessor(extension.GetNestedType("MetamagicHelper_DefaultCost_NewMetamagic_Patch",BindingFlags.NonPublic)).Patch();
  ForeignFeat061(c,"ForeignDazingInitial061",ForeignFlag061);
 }
 static void ForeignCatalogue061(BlueprintsCache c,Harmony h){
  var pool=A<BlueprintFeatureSelection>(c,"PhrenicAmplificationSelection");var extra=A<BlueprintFeatureSelection>(c,"ExtraPhrenicAmplificationFeat");
  var root=A<BlueprintFeatureSelection>(c,"MimicMetamagic1Feature01458");var initial=root.AllFeatures.ToArray();var foreign=MimicLeaf058(c,ForeignFlag061);
  Check(foreign!=null&&initial.Any(f=>f.GetComponent<AddFacts>().Facts.Contains(foreign)),"061 initial native-cache foreign AddMetamagicFeat appears in real Mimic first-choice list");
  var originalPool=((BlueprintFeatureReference[])AccessTools.Field(pool.GetType(),"m_AllFeatures").GetValue(pool)).ToArray();var source=ForeignFeat061(c,"ForeignDazingDuplicate061",ForeignFlag061);
  var initialDonor=(BlueprintFeature)c.Load(Id("ForeignDazingInitial061"));
  var previousPack=Kingmaker.Localization.LocalizationManager.CurrentPack;var pack=previousPack??new Kingmaker.Localization.LocalizationPack{Locale=Kingmaker.Localization.Shared.Locale.enGB};
  if(previousPack==null)Kingmaker.Localization.LocalizationManager.CurrentPack=pack;
  pack.PutString("MimicDonorA061","Donor A");pack.PutString("MimicDonorB061","Donor B");
  pack.PutString("MimicDonorA061.description","Shared detail");pack.PutString("MimicDonorB061.description","Shared detail");
  var markerA=new Kingmaker.Localization.LocalizedString{Key="MimicDonorA061"};var markerB=new Kingmaker.Localization.LocalizedString{Key="MimicDonorB061"};
  var detailA=new Kingmaker.Localization.LocalizedString{Key="MimicDonorA061.description"};var detailB=new Kingmaker.Localization.LocalizedString{Key="MimicDonorB061.description"};
  Set(initialDonor,"m_DisplayName",markerA);Set(source,"m_DisplayName",markerB);Set(initialDonor,"m_Description",detailA);Set(source,"m_Description",detailB);
  Check(root.AllFeatures.Count()==initial.Length&&root.AllFeatures.Count(f=>f.GetComponent<AddFacts>().Facts.Contains(foreign))==1,"061 duplicate provider flag never duplicates a Mimic choice");
  object Field(BlueprintScriptableObject bp,string name)=>AccessTools.Field(bp.GetType(),name).GetValue(bp);
  var tied=string.Compare(initialDonor.AssetGuid.ToString(),source.AssetGuid.ToString(),StringComparison.Ordinal)<0?initialDonor:source;
  var tiedBuff=A<Kingmaker.UnitLogic.Buffs.Blueprints.BlueprintBuff>(c,"MimicMetamagicOption"+(uint)ForeignFlag061+"Buff01458");
  Check(ReferenceEquals(Field(tiedBuff,"m_DisplayName"),Field(tied,"m_DisplayName")),"061 equal presentation keeps the earlier GUID donor on the existing buff");
  var richer=ReferenceEquals(tied,initialDonor)?source:initialDonor;pack.PutString(ReferenceEquals(tied,initialDonor)?"MimicDonorA061.description":"MimicDonorB061.description","");
  AccessTools.Method(T("MimicCatalogue01461"),"Changed").Invoke(null,new object[]{c,richer});
  var synced=root.AllFeatures.ToArray();var selector=synced.OfType<BlueprintFeatureSelection>().Single(v=>v.GetComponent<AddFacts>().Facts.Contains(foreign));
  var tiedToggle=A<Kingmaker.UnitLogic.ActivatableAbilities.BlueprintActivatableAbility>(c,"MimicMetamagicOption"+(uint)ForeignFlag061+"01458Toggle");
  Check(synced.Length==initial.Length&&synced.Count(f=>f.GetComponent<AddFacts>().Facts.Contains(foreign))==1,"061 a higher-presentation refresh does not add or duplicate a Mimic choice");
  bool Same(BlueprintScriptableObject bp,string name)=>ReferenceEquals(Field(bp,name),Field(richer,name));
  Check(Same(tiedBuff,"m_DisplayName")&&Same(tiedBuff,"m_Description")&&Same(tiedBuff,"m_Icon")&&Same(tiedToggle,"m_DisplayName")&&Same(tiedToggle,"m_Description")&&Same(tiedToggle,"m_Icon")&&Same(foreign,"m_DisplayName")&&Same(foreign,"m_Description")&&Same(foreign,"m_Icon")&&Same(selector,"m_DisplayName")&&Same(selector,"m_Description")&&Same(selector,"m_Icon"),"061 late higher-presentation provider updates the existing buff, toggle, leaf and first selector");
  var lateFlag=(Metamagic)(1<<16);var extension=TttLibrary061.GetType("TabletopTweaks.Core.MechanicsChanges.MetamagicExtention",true);
  var register=extension.GetMethods(BindingFlags.Public|BindingFlags.Static).Single(m=>m.Name=="RegisterMetamagic"&&m.GetParameters().Length==9);
  register.Invoke(null,new object[]{null,lateFlag,null,null,1,null,1,null,null});Check(lateFlag.DefaultCost()==1,"061 actual TTT registration API and DefaultCost prefix supply Rime adjustment");
  var late=ForeignFeat061(c,"ForeignRimeLate061",lateFlag);
  var add=AccessTools.Method(typeof(BlueprintsCache),nameof(BlueprintsCache.AddCachedBlueprint));h.Patch(add,postfix:new HarmonyMethod(typeof(MagusBatch01454Tests),nameof(LatePublishFailure061)));
  FailLateMimic061=true;var rejected=root.AllFeatures.ToArray();Check(!FailLateMimic061&&rejected.SequenceEqual(initial)&&MimicLeaf058(c,lateFlag)==null,"061 actual cache insertion failure rolls back all staged metamagic assets and retains the old native selection view");
  Check(ReferenceEquals(c.Load(late.AssetGuid),late)&&originalPool.SequenceEqual((BlueprintFeatureReference[])AccessTools.Field(pool.GetType(),"m_AllFeatures").GetValue(pool)),"061 failed optional catalogue publication retains provider assets and original class pool arrays");h.Unpatch(add,AccessTools.Method(typeof(MagusBatch01454Tests),nameof(LatePublishFailure061)));
  ForeignFeat061(c,"ForeignRetrySignal061",ForeignFlag061);
  var now=root.AllFeatures.ToArray();var leaf=MimicLeaf058(c,lateFlag);
  Check(leaf!=null&&now.Count(f=>f.GetComponent<AddFacts>().Facts.Contains(leaf))==1,"061 actual AddCachedBlueprint callback and native getter publish late metamagic once");
  Check(initial.All(f=>now.Contains(f))&&originalPool.SequenceEqual((BlueprintFeatureReference[])AccessTools.Field(pool.GetType(),"m_AllFeatures").GetValue(pool)),"061 late view preserves every original selection and never rewrites shared class-pool arrays");
  Check(pool.AllFeatures.All(f=>f!=leaf)&&extra.AllFeatures.All(f=>f!=leaf),"061 metamagic leaves remain nested choices, not free amplifications or ordinary feats");
  Check((bool)Call(T("ArcaneBatch01446"),"Existing",c,"Psychic remaining disciplines and amplifications"),"061 original strict publication receipt remains valid after late catalogue growth");
  var combo=ForeignFeat061(c,"ForeignCompositeInvalid061",Metamagic.Empower|ForeignFlag061);var before=root.AllFeatures.ToArray();
  Check(root.AllFeatures.SequenceEqual(before)&&A<BlueprintFeature>(c,"MimicMetamagicOption"+unchecked((uint)(int)(Metamagic.Empower|ForeignFlag061))+"Feature01458")==null,"061 composite provider masks are rejected without disturbing prior choices");
  foreach(var flag in new[]{ForeignFlag061,lateFlag}){
   var owner=EquipmentUnit(c,20,Psychic058);owner.Stats.Intelligence.BaseValue=30;var victim=EquipmentUnit(c,20);var points=Pool058(c,owner);var book=Book058(c,owner);var spell=Known058(c,book,1,"MindThrust1");
   var originalMask=spell.Blueprint.AvailableMetamagic;spell.Blueprint.AvailableMetamagic|=flag;book.RestoreSpontaneousSlots(1,3);
   try{Grant(owner,MimicLeaf058(c,flag));var toggle=MimicToggle058(c,owner,flag);ToggleOn(toggle);int amount=owner.Resources.GetResourceAmount(points);int price=2*Math.Max(1,flag.DefaultCost());
    Check(spell.IsAvailable&&spell.HasMetamagic(flag),"061 actual native availability and preview accept foreign metamagic: "+flag);
    var cast=Linked058(spell,victim);NativeCast058(cast,h);Check(cast.Success&&cast.Context.HasMetamagic(flag)&&owner.Resources.GetResourceAmount(points)==amount-price,"061 actual native cast applies foreign flag and provider price once: "+flag);
    int slots=book.GetSpontaneousSlots(1);Check(book.Spend(cast.Spell,false)&&book.GetSpontaneousSlots(1)==slots-1,"061 foreign Mimic pays ordinary original spell slot: "+flag);
    ToggleOff(toggle);Check(!spell.HasMetamagic(flag),"061 foreign Mimic disables without modifying known spell data: "+flag);
   }finally{spell.Blueprint.AvailableMetamagic=originalMask;}
  }
 }
}

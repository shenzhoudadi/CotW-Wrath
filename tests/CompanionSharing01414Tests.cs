using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.Serialization;
using System.Security.Cryptography;
using System.Text;
using HarmonyLib;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Kingmaker;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Selection;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.EntitySystem;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.Enums;
using Kingmaker.Items;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components;
using Kingmaker.UnitLogic.Abilities.Components.Base;
using Kingmaker.UnitLogic.Abilities.Components.TargetCheckers;
using Kingmaker.UnitLogic.ActivatableAbilities;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.FactLogic;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Mechanics.Actions;
using Kingmaker.UnitLogic.Parts;
using Kingmaker.Utility;
using CallOfTheWild.Wrath.Prototype;

// Native AbilityData detours, feature provenance and pet references execute.
// Faction, Unity pet views/passive synchronization and full save reload are
// bounded here; serialized old-toggle state is tested separately from gameplay.
internal static class CompanionSharing01414Tests {
 static readonly Type Sharing=typeof(PrerequisiteBondedSharing).Assembly.GetType("CallOfTheWild.Wrath.Prototype.BondedSpellSharing");
 static readonly Type Companion=Sharing.Assembly.GetType("CallOfTheWild.Wrath.Prototype.CompanionSpellSharing");
 static readonly Func<AbilityData,bool> Eligible=(Func<AbilityData,bool>)Delegate.CreateDelegate(typeof(Func<AbilityData,bool>),AccessTools.Method(Sharing,"Eligible"));
 static int n,legacyHashes;
 static object Get(object o,string f)=>(AccessTools.Field(o.GetType(),f)??AccessTools.Field(o.GetType(),"<"+f+">k__BackingField")).GetValue(o);
 static void Set(object o,string f,object v)=>(AccessTools.Field(o.GetType(),f)??AccessTools.Field(o.GetType(),"<"+f+">k__BackingField")).SetValue(o,v);
 static T Empty<T>()where T:class=>(T)FormatterServices.GetUninitializedObject(typeof(T));
 static void Check(bool b,string s){if(!b)throw new Exception("Companion01414: "+s);n++;}
 static object Call(Type t,string m,params object[] args)=>AccessTools.Method(t,m).Invoke(null,args);
 static T Ref<T>(SimpleBlueprint bp)where T:BlueprintReferenceBase,new(){var r=BlueprintReferenceBase.CreateTyped<T>(bp);AccessTools.Property(typeof(BlueprintReferenceBase),"Cached").SetValue(r,bp);return r;}
 static T Asset<T>(BlueprintsCache c,string token)where T:SimpleBlueprint=>Psychic034Tests.Asset<T>(c,token);
 static UnitEntityData Unit(BlueprintsCache c,string id)=>(UnitEntityData)Call(typeof(CooperationFeatTests),"Unit",c,id);
 static Feature Fact(UnitEntityData u,BlueprintFeature bp)=>(Feature)Call(typeof(CooperationFeatTests),"Fact",u,bp);
 static Spellbook Book(UnitEntityData u,BlueprintCharacterClass cls)=>(Spellbook)Call(typeof(SpellResourceFeatTests),"Book",u,true,cls);
 static AbilityData Spell(UnitEntityData u,Spellbook book,BlueprintAbility bp=null)=>(AbilityData)Call(typeof(SpellResourceFeatTests),"Spell",u,book,2,bp??new BlueprintAbility{Type=AbilityType.Spell,Range=AbilityRange.Personal,CanTargetSelf=true,ComponentsArray=Array.Empty<BlueprintComponent>(),name="01414Personal"});
 static UnitPartPet Pet(UnitEntityData pet,UnitEntityData master,PetType type=PetType.AnimalCompanion){master.Ensure<UnitPartPetMaster>();var p=pet.Ensure<UnitPartPet>();Set(p,"Type",type);Set(p,"m_MasterRef",new EntityPartRef<UnitEntityData,UnitPartPetMaster>(master));return p;}
 static Feature Provider(UnitEntityData u,BlueprintCharacterClass cls,PetType type=PetType.AnimalCompanion){var f=Fact(u,new BlueprintFeature{AssetGuid=BlueprintGuid.Parse(Guid.NewGuid().ToString("N")),ComponentsArray=new BlueprintComponent[]{new AddPet{Type=type}},name="01414PetProvider"});Set(f,"m_Source",cls);return f;}
 static bool Ally(ref bool __result){__result=true;return false;}
 static bool No(ref bool __result){__result=false;return false;}
 static bool Skip()=>false;
 static void AttachId(EntityFact __instance){if(string.IsNullOrEmpty(__instance.UniqueId))Set(__instance,"UniqueId",Guid.NewGuid().ToString("N"));}
 static bool Clock(ref TimeSpan __result){__result=TimeSpan.FromSeconds(100);return false;}
 static bool OtherModBroadTarget(AbilityData __instance,TargetWrapper __0,ref bool __result){if(__instance.Blueprint.Range!=AbilityRange.Personal||__0.Unit==__instance.Caster.Unit)return true;__result=true;return false;}
 sealed class NeverTarget:BlueprintComponent,IAbilityTargetRestriction {
  public bool IsTargetRestrictionPassed(UnitEntityData caster,TargetWrapper target)=>false;
  public string GetAbilityTargetRestrictionUIText(UnitEntityData caster,TargetWrapper target)=>"independent restriction";
 }
 sealed class NativeToggleState:Newtonsoft.Json.Serialization.DefaultContractResolver {
  protected override IList<Newtonsoft.Json.Serialization.JsonProperty> CreateProperties(Type type,Newtonsoft.Json.MemberSerialization mode){
   if(type!=typeof(ActivatableAbility))throw new Exception("Only the native toggle state field is in this persistence boundary.");
   var field=AccessTools.Field(type,"m_IsOn");if(!field.IsDefined(typeof(JsonPropertyAttribute),false))throw new Exception("Native toggle state is no longer serialized.");
   return new[]{CreateProperty(field,Newtonsoft.Json.MemberSerialization.Fields)};
  }
 }
 // Exact former GUID formula and scan placement, intentionally retained only
 // as a benchmark comparator. Both paths see the same 300-feature units.
 static BlueprintGuid LegacyId(string token){legacyHashes++;using(var md5=MD5.Create())return BlueprintGuid.Parse(string.Concat(md5.ComputeHash(Encoding.UTF8.GetBytes("CotW.Wrath.Psychic.v1/"+token)).Select(b=>b.ToString("x2"))));}
 static bool LegacyPermanent(UnitEntityData u,string token)=>u.Facts.List.OfType<Feature>().Any(f=>f.Blueprint.AssetGuid==LegacyId(token)&&!f.IsTemporary&&f.IsActive&&f.IsTurnedOn&&!f.IsDisposed&&u.Facts.List.Contains(f));
 static bool LegacyEligible(AbilityData a)=>a?.Blueprint.Type==AbilityType.Spell&&a.Blueprint.Range==AbilityRange.Personal&&a.Spellbook!=null&&a.SourceItem==null&&!a.IsAlchemistSpell&&!a.ForceTargetOwner&&!a.OverrideRange.HasValue&&a.MagicHackData==null&&!a.ShouldDelegateToMount&&LegacyPermanent(a.Caster.Unit,"ShareSpellsFeat")&&LegacyPermanent(a.Caster.Unit,"BondedMindFeat")&&a.Caster.Unit.Buffs.Enumerable.Any(b=>b.Blueprint.AssetGuid==LegacyId("BondedShareEnabled")&&b.IsActive&&b.IsTurnedOn&&!b.IsSuppressed);
 static bool LegacyTarget(AbilityData a,TargetWrapper target)=>LegacyEligible(a)&&target?.Unit!=null&&target.Unit!=a.Caster.Unit&&!target.Unit.State.IsDead&&!target.Unit.State.Features.IsUntargetable&&target.Unit.IsAlly(a.Caster.Unit)&&LegacyPermanent(target.Unit,"BondedMindFeat")&&a.Blueprint.TargetRestrictions.All(r=>r.IsTargetRestrictionPassed(a.Caster.Unit,target))&&a.GetDeliverBlueprint(false).TargetRestrictions.All(r=>r.IsTargetRestrictionPassed(a.Caster.Unit,target));
 static long Ticks(Action action,int count){var watch=Stopwatch.StartNew();for(int i=0;i<count;i++)action();watch.Stop();return watch.ElapsedTicks;}
 internal static int Run(BlueprintsCache c){
  n=0;var h=new Harmony("CotW01414.CompanionSharing");var external=new Harmony("CotW01414.OptionalCompanionBoundary");var savedTime=Game.Instance.TimeController;Set(Game.Instance,"TimeController",Empty<Kingmaker.Controllers.TimeController>());
  void Patch(System.Reflection.MethodBase m,string prefix)=>h.Patch(m,prefix:new HarmonyMethod(typeof(CompanionSharing01414Tests),prefix));
  Patch(AccessTools.Method(typeof(UnitEntityData),"IsAlly",new[]{typeof(UnitEntityData)}),nameof(Ally));Patch(AccessTools.PropertyGetter(typeof(Kingmaker.Armies.TacticalCombat.TacticalCombatHelper),"IsActive"),nameof(No));Patch(AccessTools.Method(typeof(FeatureCollection),"CheckStatPrerequisitesAndUpdate"),nameof(Skip));
  // Native Attach still runs; only the scene's ID allocator and clock are
  // supplied, matching the existing native fact-attachment regression boundary.
  Patch(AccessTools.Method(typeof(EntityFact),"Attach"),nameof(AttachId));Patch(AccessTools.PropertyGetter(typeof(Kingmaker.Controllers.TimeController),"GameTime"),nameof(Clock));
  try{
   foreach(string patch in new[]{"BondedSpellRangePatch","BondedSpellAnchorPatch","BondedSpellTargetPatch","BondedSpellTargetReasonPatch","BondedSpellUnitValidationPatch","BondedSpellDeliveryPatch","BondedSpellBuffTargetPatch"})h.CreateClassProcessor(Sharing.Assembly.GetType("CallOfTheWild.Wrath.Prototype."+patch)).Patch();
   Call(Companion,"Register",c);var companionFeat=Asset<BlueprintFeature>(c,"CompanionShareSpellsFeature");Call(Companion,"Register",c);
   var prog=(BlueprintProgression)c.Load(BlueprintGuid.Parse("f54159e8cfda69f4dbcfdf6ffbbd3293"));Check(((List<BlueprintFeatureBaseReference>)Get(prog.LevelEntries.Single(e=>e.Level==1),"m_Features")).Count(r=>r.Guid==companionFeat.AssetGuid)==1,"native companion first-level progression grants passive exactly once");Check(!companionFeat.Groups.Contains(FeatureGroup.Feat),"companion ability is automatic, not another selected feat");
   UnitEntityData u=Unit(c,"01414-druid"),v=Unit(c,"01414-ranger"),pet=Unit(c,"01414-pet"),pet2=Unit(c,"01414-pet2"),ally=Unit(c,"01414-ally");
   var druid=new BlueprintCharacterClass{AssetGuid=BlueprintGuid.Parse("610d836f3a3a9ed42a4349b62f002e96"),name="DruidClass"};var ranger=new BlueprintCharacterClass{AssetGuid=BlueprintGuid.Parse("cda0615668a6df14eb36ba19ee881af6"),name="RangerClass"};var wizard=new BlueprintCharacterClass{AssetGuid=BlueprintGuid.Parse("ba34257984f4c41408ce1dc2004e342e"),name="WizardClass"};
   u.Progression.Classes.Add(new ClassData(druid){Level=1});u.Progression.Classes.Add(new ClassData(wizard){Level=1});v.Progression.Classes.Add(new ClassData(ranger){Level=4});
   var provider=Provider(u,druid);Provider(v,ranger);var pp=Pet(pet,u);Pet(pet2,v);
   var db=Book(u,druid);var wb=Book(u,wizard);var rb=Book(v,ranger);var ds=Spell(u,db);var ws=Spell(u,wb);var rs=Spell(v,rb);
   Check(Eligible(ds)&&ds.Range==AbilityRange.Touch&&ds.TargetAnchor==AbilityTargetAnchor.Unit,"companion grant immediately enables personal spellbook touch targeting without teamwork feats");Check(ds.CanTarget(new TargetWrapper(pet))&&!ds.CanTarget(new TargetWrapper(pet2))&&!ds.CanTarget(new TargetWrapper(ally)),"first caster can share only with own companion");Check(rs.CanTarget(new TargetWrapper(pet2))&&!rs.CanTarget(new TargetWrapper(pet)),"second caster keeps independent ownership");Check(!Eligible(ws)&&!ws.CanTarget(new TargetWrapper(pet)),"unrelated wizard spellbook rejected for druid companion");Check(ds.Blueprint.Range==AbilityRange.Personal&&ds.Blueprint.CanTargetSelf,"original personal blueprint is not rewritten");
   external.Patch(AccessTools.Method(typeof(AbilityData),nameof(AbilityData.CanTarget)),prefix:new HarmonyMethod(typeof(CompanionSharing01414Tests),nameof(OtherModBroadTarget)){priority=Priority.Normal});Check(!ws.CanTarget(new TargetWrapper(pet))&&ds.CanTarget(new TargetWrapper(pet)),"optional broad companion prefix cannot bypass source-book check or duplicate delivery");external.UnpatchAll(external.Id);
   provider.IsTemporary=true;Check(!Eligible(ds),"temporary pet grant does not authorize companion class sharing");provider.IsTemporary=false;Set(provider,"IsTurnedOn",false);Check(!Eligible(ds),"removed or disabled provider immediately revokes sharing");Set(provider,"IsTurnedOn",true);
   var childSource=new BlueprintProgression{AssetGuid=BlueprintGuid.Parse(Guid.NewGuid().ToString("N")),ComponentsArray=Array.Empty<BlueprintComponent>()};var parent=Fact(u,childSource);Set(parent,"m_Source",druid);Set(provider,"m_Source",childSource);Check(Eligible(ds)&&!Eligible(ws),"feature source chain resolves exact class");Set(provider,"m_Source",druid);
   var domain=new BlueprintProgression{AssetGuid=BlueprintGuid.Parse(Guid.NewGuid().ToString("N")),ComponentsArray=Array.Empty<BlueprintComponent>()};Set(domain,"m_Classes",new[]{new BlueprintProgression.ClassWithLevel{m_Class=Ref<BlueprintCharacterClassReference>(druid)},new BlueprintProgression.ClassWithLevel{m_Class=Ref<BlueprintCharacterClassReference>(wizard)}});var domainFact=Fact(u,domain);Set(domainFact,"m_Source",druid);Set(provider,"m_Source",domain);Check(Eligible(ds)&&!Eligible(ws),"shared domain blueprint class candidates cannot override actual grant source");Set(domainFact,"m_Source",null);Check(!Eligible(ws),"ambiguous multi-class progression without provenance fails closed");Set(provider,"m_Source",druid);
   var rankBP=new BlueprintFeature{AssetGuid=BlueprintGuid.Parse("1670990255e4fe948a863bafd5dbda5d"),ComponentsArray=Array.Empty<BlueprintComponent>()};Set(provider.Blueprint.GetComponent<AddPet>(),"m_LevelRank",Ref<BlueprintFeatureReference>(rankBP));var rank=Fact(u,rankBP);Set(rank,"m_Source",ranger);u.Progression.Classes.Add(new ClassData(ranger){Level=4});var ur=Spell(u,Book(u,ranger));Check(Eligible(ur)&&ur.CanTarget(new TargetWrapper(pet)),"additional class advancing the companion qualifies through rank provenance");Check(!Eligible(ws),"rank provenance does not unlock unrelated class");
   var typeBP=new BlueprintFeature{AssetGuid=BlueprintGuid.Parse("a95311b3dc996964cbaa30ff9965aaf6"),ComponentsArray=Array.Empty<BlueprintComponent>()};var restriction=new AbilityTargetHasFact();Set(restriction,"m_CheckedFacts",new[]{Ref<BlueprintUnitFactReference>(typeBP)});var restrictedBP=new BlueprintAbility{Type=AbilityType.Spell,Range=AbilityRange.Personal,CanTargetSelf=true,ComponentsArray=new BlueprintComponent[]{restriction},name="01414TypeRestricted"};restrictedBP.OnEnable();var restricted=Spell(u,db,restrictedBP);Check(restricted.CanTarget(new TargetWrapper(pet)),"pure native creature-type restriction is waived only for companion sharing");
   restrictedBP.ComponentsArray=new BlueprintComponent[]{restriction,new NeverTarget()};restrictedBP.OnEnable();Set(restrictedBP,"m_CachedTargetRestrictions",null);Check(!restricted.CanTarget(new TargetWrapper(pet)),"independent non-type restriction remains enforced");restrictedBP.ComponentsArray=new BlueprintComponent[]{restriction};restriction.FromCaster=true;restrictedBP.OnEnable();Set(restrictedBP,"m_CachedTargetRestrictions",null);Check(!restricted.CanTarget(new TargetWrapper(pet)),"from-caster fact restrictions are never treated as creature types");restriction.FromCaster=false;
   foreach(var type in new[]{AbilityType.SpellLike,AbilityType.Extraordinary,AbilityType.Supernatural}){var a=Spell(u,db,new BlueprintAbility{Type=type,Range=AbilityRange.Personal,CanTargetSelf=true,ComponentsArray=Array.Empty<BlueprintComponent>()});Check(!Eligible(a),"non-spell ability excluded: "+type);}
   db.Blueprint.ComponentsArray=new BlueprintComponent[]{new IsAlchemistSpellbook()};db.Blueprint.OnEnable();Check(!Eligible(ds),"alchemical extracts excluded");db.Blueprint.ComponentsArray=Array.Empty<BlueprintComponent>();db.Blueprint.OnEnable();Set(ds,"ForceTargetOwner",true);Check(!Eligible(ds),"forced-owner spell excluded");Set(ds,"ForceTargetOwner",false);ds.OverrideRange=AbilityRange.Close;Check(!Eligible(ds)&&ds.Range==AbilityRange.Close,"existing range override preserved");ds.OverrideRange=null;
   var itemSpell=new AbilityData(ds.Blueprint,u.Descriptor);Check(!Eligible(itemSpell),"spell without source spellbook cannot use companion path");
   var itemFact=(Ability)Call(typeof(SpellResourceFeatTests),"Learn",u,ds.Blueprint);Set(itemFact,"SourceItem",Empty<ItemEntityUsable>());var fromItem=Spell(u,db,ds.Blueprint);Set(fromItem,"m_Fact",itemFact);Check(fromItem.SourceItem!=null&&!Eligible(fromItem),"actual item-backed native ability is excluded even with a cached spellbook");var foreignBook=Spell(u,rb);Check(!Eligible(foreignBook),"a different caster's spellbook cannot authorize this caster's companion");
   foreach(var type in new[]{PetType.AnimalCompanion,PetType.NightHag,PetType.MythicSkeletalChampion,PetType.AzataHavocDragon}){var owner=Unit(c,"01414-provider-"+type);var companion=Unit(c,"01414-native-"+type);Provider(owner,druid,type);Pet(companion,owner,type);var a=Spell(owner,Book(owner,druid));Check(a.CanTarget(new TargetWrapper(companion)),"existing native permanent pet type uses exact provider: "+type);}
   Set(pp,"Type",(PetType)604767);Check(!ds.CanTarget(new TargetWrapper(pet)),"new eidolon or phantom resource is not synthesized");Set(pp,"Type",PetType.AnimalCompanion);
   var mythic=new BlueprintCharacterClass{AssetGuid=BlueprintGuid.Parse(Guid.NewGuid().ToString("N")),name="MythicLich",IsMythic=true};UnitEntityData mu=Unit(c,"01414-lich"),skeleton=Unit(c,"01414-skeleton");var merged=Book(mu,wizard);var md=new ClassData(mythic){Level=3,Spellbook=merged.Blueprint};mu.Progression.Classes.Add(md);Provider(mu,mythic,PetType.MythicSkeletalChampion);Pet(skeleton,mu,PetType.MythicSkeletalChampion);var mergedSpell=Spell(mu,merged);Check(!Eligible(mergedSpell),"class mapping alone cannot forge a mythic merge");Set(merged,"m_Type",Enum.Parse(AccessTools.Field(typeof(Spellbook),"m_Type").FieldType,"Mythic"));Check(mergedSpell.CanTarget(new TargetWrapper(skeleton)),"native mythic class-to-book merge authorizes only the merged book");Check(!Spell(mu,Book(mu,wizard)).CanTarget(new TargetWrapper(skeleton)),"second same-class book without native merge mapping remains rejected");
   var bonded=Asset<BlueprintFeature>(c,"BondedMindFeat");var shared=Asset<BlueprintFeature>(c,"ShareSpellsFeat");var share=Fact(u,shared);Fact(u,bonded);Fact(ally,bonded);Check(ws.CanTarget(new TargetWrapper(ally)),"permanent Bonded Mind and Share Spells enable unrelated books for bonded ally by feat rule");var toggle=Asset<BlueprintActivatableAbility>(c,"BondedShareToggle");var oldBuff=Asset<BlueprintBuff>(c,"BondedShareEnabled");Check(toggle.HiddenInUI&&toggle.ActionBarAutoFillIgnored,"actual native UI flags hide legacy toggle and stop automatic hotbar filling");Check(oldBuff.IsHiddenInUI,"old enabled buff is hidden while its stable identity remains");
   foreach(bool wasOn in new[]{false,true}){var saved=JsonConvert.SerializeObject(new{Toggle=toggle.AssetGuid.ToString(),Buff=oldBuff.AssetGuid.ToString(),IsOn=wasOn});var state=JObject.Parse(saved);Check(c.Load(BlueprintGuid.Parse((string)state["Toggle"]))==toggle&&c.Load(BlueprintGuid.Parse((string)state["Buff"]))==oldBuff,"saved legacy GUIDs resolve after update");var b=Psychic034Tests.Fact(u,oldBuff,u,10);Set(b,"IsTurnedOn",(bool)state["IsOn"]);Check(ws.CanTarget(new TargetWrapper(ally)),"restored old toggle state does not gate permanent sharing");u.Facts.List.Remove(b);}
   foreach(bool wasOn in new[]{false,true}){var legacy=Empty<ActivatableAbility>();Set(legacy,"Components",new List<EntityFactComponent>());legacy.RestoreOwnerLink(u.Facts);AccessTools.Method(typeof(EntityFact),"Setup").Invoke(legacy,new object[]{toggle});Set(legacy,"m_IsOn",wasOn);var settings=new JsonSerializerSettings{ContractResolver=new NativeToggleState()};string json=JsonConvert.SerializeObject(legacy,settings);Set(legacy,"m_IsOn",false);JsonConvert.PopulateObject(json,legacy,settings);Check(legacy.IsOn==wasOn&&legacy.Hidden,"native serialized toggle state round-trips and restored native UI fact stays hidden");Check(ws.CanTarget(new TargetWrapper(ally)),"actual restored native toggle state does not affect passive access");}
   share.IsTemporary=true;Check(!ws.CanTarget(new TargetWrapper(ally)),"temporary Share Spells excluded after passive conversion");share.IsTemporary=false;u.Facts.List.Remove(share);Check(!Eligible(ws),"respec removal immediately revokes shared target access without cached feature grants");u.Facts.List.Add(share);
   var execution=new AbilityExecutionContext(ds,new AbilityParams{CasterLevel=5,SpellLevel=2},new TargetWrapper(pet));Call(Sharing,"Record",execution,new TargetWrapper(pet));var action=new ContextActionApplyBuff{ToCaster=true};Check((UnitEntityData)AccessTools.Method(typeof(ContextActionApplyBuff),"GetBuffTarget").Invoke(action,new object[]{execution})==pet&&execution.MaybeCaster==u,"selected companion receives initial caster-directed buff while source caster stays unchanged");
   // Exercise both migration entrypoints with an actual native feature grant.
   Call(Sharing.Assembly.GetType("CallOfTheWild.Wrath.Prototype.CompanionSharingLoadedPetPatch"),"Postfix",pp);Check(pet.HasFact(companionFeat),"old companion receives visible passive on post-load entrypoint");Call(Sharing.Assembly.GetType("CallOfTheWild.Wrath.Prototype.CompanionSharingLoadedPetPatch"),"Postfix",pp);Check(pet.Facts.List.Count(f=>f.Blueprint==companionFeat)==1,"repeat load does not duplicate companion passive");var newPet=Unit(c,"01414-newpet");var newPart=Pet(newPet,u);Call(Sharing.Assembly.GetType("CallOfTheWild.Wrath.Prototype.CompanionSharingNewPetPatch"),"Postfix",newPart);Check(newPet.HasFact(companionFeat),"new companion receives visible passive at acquisition");
   UnitEntityData perf=Unit(c,"01414-perf-caster"),perfAlly=Unit(c,"01414-perf-ally");var filler=new BlueprintFeature{AssetGuid=BlueprintGuid.Parse("12345678901234567890123456789012"),ComponentsArray=Array.Empty<BlueprintComponent>()};for(int i=0;i<300;i++){Fact(perf,filler);Fact(perfAlly,filler);}Fact(perf,shared);Fact(perf,bonded);Fact(perfAlly,bonded);Psychic034Tests.Fact(perf,oldBuff,perf,10);var ps=Spell(perf,Book(perf,wizard));var target=new TargetWrapper(perfAlly);Check(LegacyTarget(ps,target)&&ps.CanTarget(target),"old and new target checks agree on same populated units");for(int i=0;i<10;i++){LegacyTarget(ps,target);ps.CanTarget(target);}legacyHashes=0;const int loops=400;bool result=false;long oldTicks=Ticks(()=>result=LegacyTarget(ps,target),loops);int hashes=legacyHashes;long newTicks=Ticks(()=>result=ps.CanTarget(target),loops);Check(result&&hashes>=loops*900,"baseline executes original repeated MD5 path over all facts");Check(newTicks<oldTicks,"new real CanTarget hot path is measurably faster than former checks");
   PsychicExpansionTests.Inventory["companionSharing01414Performance"]=new JObject{["iterations"]=loops,["factsPerUnit"]=300,["legacyTargetTicks"]=oldTicks,["currentCanTargetTicks"]=newTicks,["stopwatchFrequency"]=Stopwatch.Frequency,["legacyGuidHashes"]=hashes,["speedup"]=(double)oldTicks/Math.Max(1,newTicks)};
   PsychicExpansionTests.Inventory["companionSharing01414Assertions"]=n;Console.WriteLine("PASS: "+n+" passive bonded/companion provenance, legacy identity, class book, native target and hot-path checks; full Unity save reload remains a game boundary.");return n;
  }finally{external.UnpatchAll(external.Id);h.UnpatchAll(h.Id);Set(Game.Instance,"TimeController",savedTime);}
 }
}

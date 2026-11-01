using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.Serialization;
using HarmonyLib;
using Newtonsoft.Json.Linq;
using Kingmaker;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Prerequisites;
using Kingmaker.Blueprints.Classes.Selection;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.Blueprints.Facts;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Class.LevelUp;
using Kingmaker.UnitLogic.Class.LevelUp.Actions;
using Kingmaker.UnitLogic.Mechanics;

// Provider identity and source rules are audited separately. These are explicit
// controlled native-type HA contract fixtures, not an execution of HA's binary.
// SelectFeature.Apply remains native: only its final AddFact call is redirected
// to the fixture fact collection. State/history/Parent/Next/source/checks execute.
internal static class Feat01424Tests {
 const string ClassId="adb9e138bee9ecc4db246b64d563f900", ProgressionId="5eaea532547c7ce40a84563e15fd3440", TalentId="da89239cc5859a1478007cd5e6d44f67";
 const string BasicId="247a4068296e8be42890143f451b4b45", Token="ExtraInvestigatorTalentFeat";
 static readonly Type module=typeof(CallOfTheWild.Wrath.Prototype.CrueltyFocusDC).Assembly.GetType("CallOfTheWild.Wrath.Prototype.ExtraInvestigatorTalent",true);
 static int checks,applyCalls,addFactCalls,applyBoundaryReplacements;static LevelUpController controller;
 static BlueprintGuid failPublish;static bool publishFailure,appendFailure;
 static T Empty<T>()where T:class=>(T)FormatterServices.GetUninitializedObject(typeof(T));
 static object Get(object o,string n)=>(AccessTools.Field(o.GetType(),n)??AccessTools.Field(o.GetType(),"<"+n+">k__BackingField")).GetValue(o);
 static void Set(object o,string n,object v)=>(AccessTools.Field(o.GetType(),n)??AccessTools.Field(o.GetType(),"<"+n+">k__BackingField")).SetValue(o,v);
 static void Check(bool ok,string text){if(!ok)throw new Exception("Feat01424: "+text);checks++;}
 static BlueprintGuid Id(string token)=>(BlueprintGuid)AccessTools.Method(module.Assembly.GetType("CallOfTheWild.Wrath.Prototype.PsychicAssets"),"Id").Invoke(null,new object[]{token});
 static BlueprintGuid FeatId=>Id(Token);
 static T Ref<T>(SimpleBlueprint bp)where T:BlueprintReferenceBase,new(){var r=BlueprintReferenceBase.CreateTyped<T>(bp);AccessTools.Property(typeof(BlueprintReferenceBase),"Cached").SetValue(r,bp,null);return r;}
 static BlueprintFeatureReference[] Refs(BlueprintFeatureSelection selection,string field)=>(BlueprintFeatureReference[])Get(selection,field)??Array.Empty<BlueprintFeatureReference>();
 static string[] PoolSignature(BlueprintFeatureSelection pool)=>new[]{string.Join(",",Refs(pool,"m_Features").Select(r=>r?.Guid.ToString()??"null")),string.Join(",",Refs(pool,"m_AllFeatures").Select(r=>r?.Guid.ToString()??"null"))};
 static void Choices(BlueprintFeatureSelection selection,params BlueprintFeature[] features){Set(selection,"m_Features",features.Select(Ref<BlueprintFeatureReference>).ToArray());Set(selection,"m_AllFeatures",features.Select(Ref<BlueprintFeatureReference>).ToArray());}
 static PrerequisiteFeature Requires(BlueprintFeature feature){var p=new PrerequisiteFeature();Set(p,"m_Feature",Ref<BlueprintFeatureReference>(feature));return p;}
 static bool Register(BlueprintsCache cache)=>(bool)AccessTools.Method(module,"Register").Invoke(null,new object[]{cache});
 static Exception Root(Exception e){while(e is TargetInvocationException&&e.InnerException!=null)e=e.InnerException;return e;}
 static bool Rejected(BlueprintsCache cache){try{Register(cache);return false;}catch(Exception e){return Root(e)is InvalidOperationException;}}
 static BlueprintComponent Copy(BlueprintComponent value)=>(BlueprintComponent)AccessTools.Method(module.Assembly.GetType("CallOfTheWild.Wrath.Prototype.PsychicAssets"),"CopyData").MakeGenericMethod(typeof(BlueprintComponent)).Invoke(null,new object[]{value});
 static bool Skip()=>false;
 static bool No(ref bool __result){__result=false;return false;}
 static bool Ignore(BlueprintFeatureSelection __instance,ref bool __result){__result=__instance.IgnorePrerequisites;return false;}
 static bool Controller(ref LevelUpController __result){__result=controller;return false;}
 static void CountApply(){applyCalls++;}
 static void PublishFailure(BlueprintGuid guid){if(publishFailure&&guid==failPublish)throw new InvalidOperationException("01424 injected publication failure after native cache insertion");}
 static void AppendFailure(BlueprintFeature[] feats){if(appendFailure&&feats.Any(f=>f.AssetGuid==FeatId))throw new InvalidOperationException("01424 injected failure after native pool append");}
 // The real optional dispatcher also links spells. Restore its unrelated spell
 // fixture changes after this test, so the later spell-link suite keeps its own
 // pre-registration baseline; the investigator fixture and root remain present.
 sealed class SpellListsSnapshot {
  readonly Dictionary<BlueprintSpellList,SpellLevelList[]> lists;
  readonly Dictionary<SpellLevelList,object[]> rows;
  readonly string[] fields={"m_Spells","m_SpellsProxy","m_SpellsFiltered"};
  internal SpellListsSnapshot(BlueprintsCache cache){var loaded=(IDictionary)Get(cache,"m_LoadedBlueprints");lists=loaded.Keys.Cast<BlueprintGuid>().ToArray().Select(cache.Load).OfType<BlueprintSpellList>().ToDictionary(b=>b,b=>b.SpellsByLevel);rows=lists.Values.Where(a=>a!=null).SelectMany(a=>a).Where(r=>r!=null).Distinct().ToDictionary(r=>r,r=>fields.Select(f=>Get(r,f)).ToArray());}
  internal void Restore(){foreach(var p in lists)p.Key.SpellsByLevel=p.Value;foreach(var p in rows)for(int i=0;i<fields.Length;i++)Set(p.Key,fields[i],p.Value[i]);}
 }
 static IEnumerable<CodeInstruction> ApplyBoundary(IEnumerable<CodeInstruction> code){
  foreach(var x in code){var m=x.operand as MethodInfo;if(m?.Name=="AddFact"&&m.IsGenericMethod&&m.GetGenericArguments().SequenceEqual(new[]{typeof(Feature)})){
   Check(m.IsStatic&&m.GetParameters().Select(p=>p.ParameterType).SequenceEqual(new[]{typeof(UnitDescriptor),typeof(BlueprintUnitFact),typeof(MechanicsContext),typeof(FeatureParam)}),"native AddFact endpoint signature is exact");
   x.opcode=OpCodes.Call;x.operand=AccessTools.Method(typeof(Feat01424Tests),nameof(AddFeature));applyBoundaryReplacements++;
  }yield return x;}
 }
 static Feature Fact(UnitEntityData unit,BlueprintFeature bp)=>(Feature)AccessTools.Method(typeof(Linnorm01416Tests),"Fact").Invoke(null,new object[]{unit,bp});
 static Feature AddFeature(UnitDescriptor unit,BlueprintUnitFact blueprint,MechanicsContext context,FeatureParam param){
  addFactCalls++;var bp=(BlueprintFeature)blueprint;var existing=unit.Progression.Features.GetFact(bp);
  if(existing!=null){if(existing.Rank<bp.Ranks)Set(existing,"Rank",existing.Rank+1);return existing;}
  var fact=Fact(unit.Unit,bp);fact.Param=param;return fact;
 }
 sealed class Provider {
  internal readonly BlueprintCharacterClass Class;
  internal readonly BlueprintProgression Progression;
  internal readonly BlueprintFeatureSelection Talents,Nested;
  internal readonly BlueprintFeature A,B,Advanced,Chain,Leaf,Ranked;
  internal readonly SimpleBlueprint[] Assets;
  internal Provider(BlueprintsCache cache){
   Class=new BlueprintCharacterClass{name="InvestigatorClass",AssetGuid=BlueprintGuid.Parse(ClassId),ComponentsArray=Array.Empty<BlueprintComponent>()};
   Progression=new BlueprintProgression{name="InvestigatorProgression",AssetGuid=BlueprintGuid.Parse(ProgressionId),ComponentsArray=Array.Empty<BlueprintComponent>()};
   Talents=new BlueprintFeatureSelection{name="InvestigatorTalentSelection",AssetGuid=BlueprintGuid.Parse(TalentId),ComponentsArray=new BlueprintComponent[]{new PrerequisiteStatValue{Stat=StatType.Intelligence,Value=13}},Ranks=1};
   Set(Talents,"m_Icon",Empty<UnityEngine.Sprite>());
   A=Feature("A");B=Feature("B");Chain=Feature("Chain",Requires(A));Leaf=Feature("Leaf",Requires(B));Ranked=Feature("Ranked");Ranked.Ranks=3;
   var level=new PrerequisiteClassLevel{Level=8};Set(level,"m_CharacterClass",Ref<BlueprintCharacterClassReference>(Class));Advanced=Feature("Advanced",level);
   Nested=new BlueprintFeatureSelection{name="01424FixtureNestedTalent",AssetGuid=Id("01424FixtureNestedTalent"),ComponentsArray=Array.Empty<BlueprintComponent>(),Ranks=1};Choices(Nested,Leaf);
   Choices(Talents,A,B,Advanced,Chain,Nested,Ranked);
   Set(Class,"m_Progression",Ref<BlueprintProgressionReference>(Progression));
   var entry=(LevelEntry)PsychicExpansionTests.Read(new JObject{["Level"]=3,["m_Features"]=new JArray("!bp_"+TalentId)},typeof(LevelEntry));Progression.LevelEntries=new[]{entry};
   Assets=new SimpleBlueprint[]{Class,Progression,Talents,A,B,Advanced,Chain,Nested,Leaf,Ranked};
   foreach(var a in Assets){if(a is BlueprintScriptableObject bp)bp.OnEnable();cache.AddCachedBlueprint(a.AssetGuid,a);}
  }
  static BlueprintFeature Feature(string label,params BlueprintComponent[] components)=>new BlueprintFeature{name="01424FixtureTalent"+label,AssetGuid=Id("01424FixtureTalent"+label),ComponentsArray=components,Ranks=1};
 }
 static UnitEntityData Unit(BlueprintsCache cache,Provider provider,string name,int level=3){
  var u=(UnitEntityData)AccessTools.Method(typeof(SpellAndSword01417Tests),"Unit").Invoke(null,new object[]{cache,name});
  Set(u.Progression,"m_CharacterLevel",(int?)level);u.Progression.Classes.Clear();u.Progression.Classes.Add(new ClassData(provider.Class){Level=level});
  var selections=AccessTools.Field(typeof(UnitProgressionData),"m_Selections");selections.SetValue(u.Progression,Activator.CreateInstance(selections.FieldType));
  return u;
 }
 static LevelUpState State(UnitEntityData unit,int level){var state=Empty<LevelUpState>();Set(state,"m_Unit",unit);Set(state,"NextCharacterLevel",level);Set(state,"Selections",new List<FeatureSelectionState>());state.NextClassLevel=level;state.SelectedClass=unit.Progression.Classes[0].CharacterClass;return state;}
 static FeatureSelectionState Pending(LevelUpState state,BlueprintFeatureSelection selection,int level)=>state.AddSelection(null,default(FeatureSource),selection,level);
 static SelectFeature Apply(LevelUpState state,UnitEntityData unit,FeatureSelectionState selection,BlueprintFeature item){var action=new SelectFeature(selection,item);Check(action.Check(state,unit.Descriptor),"native Check permits "+item.name);action.Apply(state,unit.Descriptor);Check(selection.SelectedItem==item,"native Apply commits selected item "+item.name);Check(!action.Check(state,unit.Descriptor),"native Check rejects replay of committed action");return action;}
 static JObject SelectionFlow(BlueprintsCache cache,Provider p,BlueprintFeatureSelection root,BlueprintFeatureSelection basic){
  int start=checks;var u=Unit(cache,p,"01424-two-feats");var state=State(u,3);var first=Pending(state,basic,3);
  Check(!basic.CanSelect(u.Descriptor,state,first,root),"class level alone cannot replace talent class feature prerequisite");Fact(u,p.Talents);
  Check(basic.CanSelect(u.Descriptor,state,first,root),"ordinary native pool accepts qualified root");
  Check(FeatureSelectionExtensions.CanSelectAny(root,u.Descriptor,state,first),"native nested root has available child");
  u.Stats.Intelligence.BaseValue=10;Check(!basic.CanSelect(u.Descriptor,state,first,root),"provider parent prerequisite remains enforced");u.Stats.Intelligence.BaseValue=18;
  Check(!root.CanSelect(u.Descriptor,state,first,p.Advanced),"advanced child retains class-level gate");Check(!root.CanSelect(u.Descriptor,state,first,p.Chain),"child retains feature prerequisite");Check(!root.CanSelect(u.Descriptor,state,first,p.Nested),"nested branch with no eligible child is unavailable");
  var rootAction=Apply(state,u,first,root);var child=first.Next;
  Check(child!=null&&child.Parent==first&&child.Selection==root&&child.Index==0&&child.Level==3,"original Apply creates correct first parent/next child state");
  Check(state.Selections.Count==2&&state.Selections.Contains(child),"one root click creates one child state");
  Check(u.Progression.GetSelections(basic,3).Count(f=>f==root)==1,"native progression records first general feat choice");
  Apply(state,u,child,p.A);Check(u.Progression.Features.GetRank(p.A)==1,"first talent is present once");Check(u.Progression.GetSelections(root,3).Count(f=>f==p.A)==1,"native progression records first talent choice once");
  Check(!root.CanSelect(u.Descriptor,state,child,p.A),"already-owned rank-one talent cannot repeat");Check(!root.CanSelect(u.Descriptor,state,child,p.Chain),"native current selected branch cannot supply its own replacement prerequisite");
  var second=Pending(state,basic,3);Check(second.Index==1,"ordinary feat states carry independent indices");
  Check(root.CanSelect(u.Descriptor,state,second,p.Chain),"earlier completed choice unlocks dependent talent in independent pending choice");
  Apply(state,u,second,root);var child2=second.Next;
  Check(child2!=null&&child2!=child&&child2.Parent==second&&child2.Index==1&&child2.Selection==root,"repeat root creates a separate second child state");
  Check(u.Progression.Features.GetRank(root)==1&&u.Progression.Features.GetRank(p.A)==1,"repeatable selection root does not duplicate root fact or first talent");
  Check(!root.CanSelect(u.Descriptor,state,child2,p.A),"second root cannot select the already learned talent");Apply(state,u,child2,p.B);
  Check(u.Progression.GetSelections(root,3).SequenceEqual(new[]{p.A,p.B}),"two root choices record exactly two distinct selected talents");
  var third=Pending(state,basic,3);Check(root.CanSelect(u.Descriptor,state,third,p.Nested),"nested provider selection becomes available in independent choice after its leaf prerequisite");Apply(state,u,third,root);Apply(state,u,third.Next,p.Nested);var leaf=third.Next.Next;
  Check(leaf!=null&&leaf.Parent==third.Next&&leaf.Selection==p.Nested,"original Apply builds deeper provider selection chain");Apply(state,u,leaf,p.Leaf);
  Check(u.Progression.Features.GetRank(p.Leaf)==1&&!root.CanSelect(u.Descriptor,state,third.Next,p.Nested),"consumed nested rank-one leaf makes that branch unavailable");
  var saved=new SelectFeature(first,root);Set(saved,"Item",null);saved.PostLoad();Check(saved.Item==root&&saved.SelectionIndex==first.Index,"native PostLoad resolves stable root item and index");
  saved=new SelectFeature(child,p.A);Set(saved,"Item",null);saved.PostLoad();Check(saved.Item==p.A&&saved.SelectionIndex==child.Index,"native PostLoad resolves stable child item and index");
  var exhausted=Unit(cache,p,"01424-exhausted");Fact(exhausted,p.Talents);foreach(var bp in new[]{p.A,p.B,p.Chain,p.Leaf})Fact(exhausted,bp);var ranked=Fact(exhausted,p.Ranked);Set(ranked,"Rank",3);var exhaustedState=State(exhausted,3);var exhaustedSelection=Pending(exhaustedState,basic,3);
  Check(!FeatureSelectionExtensions.CanSelectAny(root,exhausted.Descriptor,exhaustedState,exhaustedSelection)&&!basic.CanSelect(exhausted.Descriptor,exhaustedState,exhaustedSelection,root),"exhausted or level-gated children cannot open an empty root choice");
  Set(ranked,"Rank",2);Check(root.CanSelect(exhausted.Descriptor,exhaustedState,exhaustedSelection,p.Ranked),"provider rankable talent retains native rank limit rather than forced one-rank filtering");Set(ranked,"Rank",3);
  exhausted.Progression.Classes[0].Level=8;Set(exhausted.Progression,"m_CharacterLevel",(int?)8);exhaustedState.NextClassLevel=8;Check(root.CanSelect(exhausted.Descriptor,exhaustedState,exhaustedSelection,p.Advanced),"native later class level unlocks its advanced talent");
  controller=Empty<LevelUpController>();controller.Unit=u;Set(controller,"Preview",u);controller.State=state;
  foreach(int level in new[]{1,2,3,8,20}){Set(u.Progression,"m_CharacterLevel",(int?)level);Set(state,"NextCharacterLevel",level);foreach(var item in new BlueprintFeature[]{root,p.A,p.Advanced,p.Nested}){var selection=item==root?basic:root;var view=Empty<FeatureSelectionViewState>();view.RefreshCanSelectState(first,selection,item);Check(view.CanSelect==selection.CanSelect(u.Descriptor,state,first,item),"native viewstate agrees at character level "+level+" for "+item.name);}}
  Check(applyBoundaryReplacements==1&&applyCalls==7&&addFactCalls==7,"seven original Apply executions with one exact AddFact callsite boundary");
  return new JObject{["passed"]=true,["assertions"]=checks-start,["originalApplyExecutions"]=applyCalls,["originalCanSelect"]=true,["originalSelectionHistory"]=true,["parentNextAndIndependentIndices"]=true,["postLoadIdentityResolution"]=true,["viewLevels"]=new JArray(1,2,3,8,20),["boundary"]="SelectFeature.Check/Apply/PostLoad and native parent/child/history/source bookkeeping execute. Its one UnitHelper.AddFact<Feature> callsite is replaced with controlled actual Feature collection insertion/rank endpoint; game controller and prerequisite-ignore scene lookup bounded. No whole Apply replacement, Unity UI rendering, HA runtime component execution or save loading."};
 }
 internal static int Run(BlueprintsCache cache){
  checks=applyCalls=addFactCalls=applyBoundaryReplacements=0;var h=new Harmony("CotW01424.NativeSelectionContracts");var reports=new List<Tuple<string,bool,Exception>>();var unrelatedSpells=new SpellListsSnapshot(cache);
  var basic=(BlueprintFeatureSelection)cache.Load(BlueprintGuid.Parse(BasicId));var before=PoolSignature(basic);Check(cache.Load(FeatId)==null,"new root is absent before optional provider registration");
  void Optional(){reports.Clear();AccessTools.Method(module.Assembly.GetType("CallOfTheWild.Wrath.Prototype.BlueprintRegistration"),"RegisterOptional").Invoke(null,new object[]{cache,new Action<string,bool,Exception>((name,ok,error)=>reports.Add(Tuple.Create(name,ok,error)))});}
  h.Patch(AccessTools.Method(typeof(FeatureCollection),"CheckStatPrerequisitesAndUpdate"),prefix:new HarmonyMethod(typeof(Feat01424Tests),nameof(Skip)));
  h.Patch(AccessTools.PropertyGetter(typeof(IgnorePrerequisites),"Ignore"),prefix:new HarmonyMethod(typeof(Feat01424Tests),nameof(No)));
  h.Patch(AccessTools.Method(typeof(BlueprintFeatureSelection),"IsIgnorePrerequisites"),prefix:new HarmonyMethod(typeof(Feat01424Tests),nameof(Ignore)));
  h.Patch(AccessTools.PropertyGetter(typeof(Game),"LevelUpController"),prefix:new HarmonyMethod(typeof(Feat01424Tests),nameof(Controller)));
  h.Patch(AccessTools.Method(typeof(SelectFeature),"Apply"),prefix:new HarmonyMethod(typeof(Feat01424Tests),nameof(CountApply)),transpiler:new HarmonyMethod(typeof(Feat01424Tests),nameof(ApplyBoundary)));
  Provider01419Tests.PatchLabels(h);
  try {
   Check(!Register(cache)&&cache.Load(FeatId)==null&&PoolSignature(basic).SequenceEqual(before),"absent HA safely skips without touching general feat pool");Optional();Check(!reports.Any(r=>r.Item1=="Extra Investigator Talent"&&r.Item2),"real optional caller does not claim unavailable feat registered");
   var p=new Provider(cache);var donorBefore=PoolSignature(p.Talents);var donorComponents=p.Talents.ComponentsArray;var providerArrays=new[]{Get(p.Talents,"m_Features"),Get(p.Talents,"m_AllFeatures")};
   foreach(var asset in new SimpleBlueprint[]{p.Class,p.Progression,p.Talents,p.A}){cache.RemoveCachedBlueprint(asset.AssetGuid);Check(!Register(cache)&&cache.Load(FeatId)==null&&PoolSignature(basic).SequenceEqual(before),"incomplete provider safely skips: "+asset.name);cache.AddCachedBlueprint(asset.AssetGuid,asset);}
   p.Talents.IgnorePrerequisites=true;Check(Rejected(cache)&&cache.Load(FeatId)==null&&PoolSignature(basic).SequenceEqual(before),"provider prerequisite bypass shape is rejected without publication");Optional();Check(reports.Any(r=>r.Item1=="Extra Investigator Talent"&&!r.Item2&&r.Item3!=null),"real optional caller isolates prerequisite bypass shape");p.Talents.IgnorePrerequisites=false;
   var original=Refs(p.Talents,"m_AllFeatures");Set(p.Talents,"m_AllFeatures",Array.Empty<BlueprintFeatureReference>());Check(!Register(cache)&&cache.Load(FeatId)==null,"empty provider selection not published");Set(p.Talents,"m_AllFeatures",original);
   Set(p.Talents,"m_AllFeatures",original.Concat(new BlueprintFeatureReference[]{null}).ToArray());Check(!Register(cache)&&cache.Load(FeatId)==null,"null child reference not published");Set(p.Talents,"m_AllFeatures",original);
   var originalName=p.Class.name;p.Class.name="WrongProviderIdentity";Check(Rejected(cache)&&cache.Load(FeatId)==null&&PoolSignature(basic).SequenceEqual(before),"same GUID wrong provider name is rejected");p.Class.name=originalName;
   var nestedFeatures=Refs(p.Nested,"m_Features");var nestedAll=Refs(p.Nested,"m_AllFeatures");Choices(p.Nested,p.Talents);Check(Rejected(cache)&&cache.Load(FeatId)==null&&PoolSignature(basic).SequenceEqual(before),"nested provider cycle is rejected before native recursive choice evaluation");Optional();Check(reports.Any(r=>r.Item1=="Extra Investigator Talent"&&!r.Item2&&r.Item3!=null)&&PoolSignature(basic).SequenceEqual(before),"real optional caller isolates cyclic provider selection");Set(p.Nested,"m_Features",nestedFeatures);Set(p.Nested,"m_AllFeatures",nestedAll);
   var foreign=new BlueprintFeature{name="ForeignCollision01424",AssetGuid=FeatId,ComponentsArray=Array.Empty<BlueprintComponent>()};cache.AddCachedBlueprint(FeatId,foreign);bool collision=false;try{Register(cache);}catch(Exception e){collision=Root(e)is InvalidOperationException;}Check(collision&&ReferenceEquals(cache.Load(FeatId),foreign)&&PoolSignature(basic).SequenceEqual(before),"foreign root identity conflict rejected with no mutation");Optional();Check(reports.Any(r=>r.Item1=="Extra Investigator Talent"&&!r.Item2&&r.Item3!=null)&&ReferenceEquals(cache.Load(FeatId),foreign),"real optional caller isolates identity conflict");cache.RemoveCachedBlueprint(FeatId);
   failPublish=FeatId;h.Patch(AccessTools.Method(typeof(BlueprintsCache),"AddCachedBlueprint"),postfix:new HarmonyMethod(typeof(Feat01424Tests),nameof(PublishFailure)));publishFailure=true;bool rollback=false;try{Register(cache);}catch(Exception e){rollback=Root(e)is InvalidOperationException;}finally{publishFailure=false;}Check(rollback&&cache.Load(FeatId)==null&&PoolSignature(basic).SequenceEqual(before),"failed native cache publication rolls back root and prior pool arrays");
   Optional();var root=cache.Load(FeatId)as BlueprintFeatureSelection;Check(root!=null&&reports.Count(r=>r.Item1=="Extra Investigator Talent"&&r.Item2)==1,"real optional dispatcher registers complete late provider exactly once");
   Check(root.name=="CotWWrathPsychic"+Token&&root.Groups.SequenceEqual(new[]{FeatureGroup.Feat})&&root.Ranks==1,"one ordinary native selection feat with stable identity and no combat group");
   Check(root.GetComponents<PrerequisiteFeature>().Any(pr=>pr.Feature==p.Talents),"root requires exact investigator talent class feature");Check(root.GetComponent<PrerequisiteStatValue>()?.Value==13&&!ReferenceEquals(root.GetComponent<PrerequisiteStatValue>(),p.Talents.GetComponent<PrerequisiteStatValue>()),"provider prerequisites are copied independently");
   Check(root.Icon==p.Talents.Icon,"uses provider talent icon");
   foreach(string field in new[]{"m_Features","m_AllFeatures"}){Check(Refs(root,field).Select(r=>r.Guid).SequenceEqual(Refs(p.Talents,field).Select(r=>r.Guid)),"original provider child identity/order "+field);Check(!ReferenceEquals(Get(root,field),Get(p.Talents,field))&&!ReferenceEquals(Refs(root,field)[0],Refs(p.Talents,field)[0]),"private child array and reference wrappers "+field);Check(Refs(basic,field).Count(r=>r.Guid==FeatId)==1,"ordinary pool root once "+field);}
   Check(Refs(basic,"m_AllFeatures").Take(before[1].Length==0?0:before[1].Split(',').Length).Select(r=>r.Guid.ToString()).SequenceEqual(before[1].Split(new[]{','},StringSplitOptions.RemoveEmptyEntries)),"ordinary pool original entries and order preserved");
   foreach(var id in (string[])AccessTools.Field(module.Assembly.GetType("CallOfTheWild.Wrath.Prototype.CombatFeatPoolRepair"),"NativeIds").GetValue(null)){if(id==BasicId)continue;var pool=cache.Load(BlueprintGuid.Parse(id))as BlueprintFeatureSelection;if(pool!=null)foreach(string field in new[]{"m_Features","m_AllFeatures"})Check(!Refs(pool,field).Any(r=>r.Guid==FeatId),"not in combat/restricted native pool "+pool.name+" "+field);}
   Check(Register(cache)&&ReferenceEquals(cache.Load(FeatId),root)&&Refs(basic,"m_AllFeatures").Count(r=>r.Guid==FeatId)==1,"repeat registration keeps root identity and no duplicate pool entry");
   var flow=SelectionFlow(cache,p,root,basic);
   // Equivalent external selection uses copied native structure, not its label.
   var equivalent=new BlueprintFeatureSelection{name="DifferentLabelEquivalent01424",AssetGuid=Id("01424FixtureEquivalent"),ComponentsArray=root.ComponentsArray.Select(Copy).ToArray(),Groups=new[]{FeatureGroup.Feat},Ranks=root.Ranks,Mode=root.Mode,IgnorePrerequisites=root.IgnorePrerequisites,ExceptWhiteListed=root.ExceptWhiteListed,Obligatory=root.Obligatory,Group=root.Group,Group2=root.Group2,ShowThisSelection=root.ShowThisSelection};Choices(equivalent,root.AllFeatures.ToArray());equivalent.OnEnable();cache.AddCachedBlueprint(equivalent.AssetGuid,equivalent);
   var oldPools=new[]{Refs(basic,"m_Features"),Refs(basic,"m_AllFeatures")};Set(basic,"m_Features",Refs(basic,"m_Features").Concat(new[]{Ref<BlueprintFeatureReference>(equivalent)}).ToArray());Check(Register(cache)&&Refs(basic,"m_AllFeatures").Any(r=>r.Guid==FeatId),"external reference absent from visible AllFeatures cannot suppress own root");Set(basic,"m_AllFeatures",Refs(basic,"m_AllFeatures").Concat(new[]{Ref<BlueprintFeatureReference>(equivalent)}).ToArray());
   equivalent.Obligatory=!root.Obligatory;Check(Register(cache)&&Refs(basic,"m_AllFeatures").Any(r=>r.Guid==FeatId),"same children with different obligatory policy is not an equivalent feat");equivalent.Obligatory=root.Obligatory;
   var equivalentComponents=equivalent.ComponentsArray;equivalent.ComponentsArray=equivalentComponents.Concat(new BlueprintComponent[]{Requires(p.A)}).ToArray();equivalent.OnEnable();Check(Register(cache)&&Refs(basic,"m_AllFeatures").Any(r=>r.Guid==FeatId),"same children with stricter prerequisite does not suppress owned root");equivalent.ComponentsArray=equivalentComponents;equivalent.OnEnable();
   Check(!Register(cache)&&ReferenceEquals(cache.Load(FeatId),root)&&Refs(basic,"m_AllFeatures").All(r=>r.Guid!=FeatId),"late equivalent external feat removes only own menu entry and preserves cached save identity");
   cache.RemoveCachedBlueprint(FeatId);Check(!Register(cache)&&cache.Load(FeatId)==null,"pre-existing structurally equivalent selection prevents own duplicate root creation");cache.AddCachedBlueprint(FeatId,root);
   for(int i=0;i<2;i++)Set(basic,i==0?"m_Features":"m_AllFeatures",oldPools[i]);cache.RemoveCachedBlueprint(equivalent.AssetGuid);Check(Register(cache)&&ReferenceEquals(cache.Load(FeatId),root),"removing equivalent external entry restores owned menu without replacing saved root");
   var late=new BlueprintFeature{name="01424FixtureLateTalent",AssetGuid=Id("01424FixtureLateTalent"),ComponentsArray=Array.Empty<BlueprintComponent>(),Ranks=1};late.OnEnable();cache.AddCachedBlueprint(late.AssetGuid,late);foreach(string field in new[]{"m_Features","m_AllFeatures"})Set(p.Talents,field,Refs(p.Talents,field).Concat(new[]{Ref<BlueprintFeatureReference>(late)}).ToArray());
   var preAppendBasic=new[]{Get(basic,"m_Features"),Get(basic,"m_AllFeatures")};var preAppendRoot=new[]{Get(root,"m_Features"),Get(root,"m_AllFeatures")};h.Patch(AccessTools.Method(module.Assembly.GetType("CallOfTheWild.Wrath.Prototype.T09CombatFeats"),"Append"),postfix:new HarmonyMethod(typeof(Feat01424Tests),nameof(AppendFailure)));appendFailure=true;bool appendRolledBack;try{appendRolledBack=Rejected(cache);}finally{appendFailure=false;}Check(appendRolledBack&&ReferenceEquals(cache.Load(FeatId),root),"failure after actual pool append retains existing root identity");for(int i=0;i<2;i++){var field=i==0?"m_Features":"m_AllFeatures";Check(ReferenceEquals(Get(basic,field),preAppendBasic[i])&&ReferenceEquals(Get(root,field),preAppendRoot[i]),"post-append failure restores exact prior pool and root arrays "+field);}Check(!root.AllFeatures.Contains(late),"failed late child update is rolled back");
   Check(Register(cache)&&ReferenceEquals(cache.Load(FeatId),root)&&root.AllFeatures.Contains(late),"late provider child synchronizes without replacing feat root");
   for(int i=0;i<2;i++)Set(p.Talents,i==0?"m_Features":"m_AllFeatures",providerArrays[i]);Register(cache);Check(PoolSignature(p.Talents).SequenceEqual(donorBefore)&&ReferenceEquals(p.Talents.ComponentsArray,donorComponents),"provider arrays, components and prerequisites never mutated by registration");
   var contracts=new JObject{["passed"]=true,["assertions"]=checks,["featId"]=FeatId.ToString(),["actualOptionalRegistration"]=true,["nativeSelectionFlow"]=flow,["newFeatCount"]=1,["providerFixture"]="Controlled native BlueprintCharacterClass/BlueprintProgression/BlueprintFeatureSelection with audited public HA identities; no HA DLL execution. Exact latest provider payload was not available.",["optionalLifecycle"]="absent/partial/empty/null/wrong identity/late/repeat/conflict/transaction/structurally equivalent foreign root/late child",["ordinaryOnly"]=true,["providerAssetsRetainedForWholePoolAndPreviewChecks"]=true};
   PsychicExpansionTests.Inventory["feat01424Contracts"]=contracts;PsychicExpansionTests.Inventory["feat01424Assertions"]=checks;Console.WriteLine("PASS: "+checks+" 01424 optional provider, native repeated talent selection and transaction contracts.");return checks;
  }finally{unrelatedSpells.Restore();publishFailure=false;appendFailure=false;controller=null;h.UnpatchAll(h.Id);}
 }
}

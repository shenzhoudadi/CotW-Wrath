using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.Serialization;
using HarmonyLib;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Kingmaker;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Prerequisites;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.UnitLogic.Class.LevelUp.Actions;
using Kingmaker.UI.MVVM._VM.CharGen.Phases.Spells;
using Kingmaker.Blueprints.Classes.Selection;
using Kingmaker.Blueprints.Facts;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.Controllers;
using Kingmaker.Controllers.Combat;
using Kingmaker.Controllers.Units;
using Kingmaker.Designers.Mechanics.Facts;
using Kingmaker.EntitySystem;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Enums;
using Kingmaker.Items;
using Kingmaker.PubSubSystem;
using Kingmaker.RuleSystem;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.RuleSystem.Rules.Abilities;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components.AreaEffects;
using Kingmaker.UnitLogic.ActivatableAbilities;
using Kingmaker.UnitLogic.Buffs;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Buffs.Components;
using Kingmaker.UnitLogic.Class.LevelUp;
using Kingmaker.UnitLogic.Commands;
using Kingmaker.UnitLogic.Commands.Base;
using Kingmaker.UnitLogic.FactLogic;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Mechanics.Actions;
using Kingmaker.UnitLogic.Mechanics.Components;
using Kingmaker.UnitLogic.Parts;
using Kingmaker.Utility;
using CallOfTheWild.Wrath.Prototype;

internal static class BardMasterpieces01435Tests
{
    const string Bard="772c83a25e2268e448e841dcd548235f",Skald="6afa347d804838b48bda16acb0573dc0";
    const string BardPool="e190ba276831b5c4fa28737e5e49e6a6",SkaldPool="4a2302c4ec2cfb042bba67d825babfec";
    const string BardGrant="b92bfc201c6a79e49afd0b5cfbfc269f",SkaldGrant="334cb75aa673d1d4bb279761c2ef5cf1";
    static readonly string[] Tokens={"ClamorOfHeavensMasterpiece","DanceOf23StepsMasterpiece","SymphonyOfElysianHeartMasterpiece"};
    static Type Module=>typeof(MasterpiecePerformanceRestriction01435).Assembly.GetType("CallOfTheWild.Wrath.Prototype.BardMasterpieces01435",true);
    static int checks,serial,saves,applications,fxRequests,resourceSpends,ticks,nativeConcentrations,nativeMeleeRules,nativeRangedRules;
    static int roll=1;
    static bool appendFailure,prestigeFailure;
    static bool rowCacheFailure,rowCacheFaultObserved;
    static object rowCacheFaultTarget;
    static int learned036,removed036,selected036,uiBuilt036,loaded036,copied036;
    static int displayNames036,displayIcons036,displaySchools036;
    static int disposedChoiceVMs036,prewarmedSpellRows036,rowCacheFaultCount036;
    static List<CharGenSpellSelectorItemVM> constructedChoiceVMs036;
    static UnitEntityData hostile;
    static RulebookEventContext current;
    static RuleCheckConcentration lastConcentration;
    static RuleSavingThrow lastSave;
    static RuleCanApplyBuff lastApplyRule;
    static Kingmaker.Settings.StatsAdjustmentsType sceneDifficulty=Kingmaker.Settings.StatsAdjustmentsType.None;
    static readonly List<UnitEntityData> units=new List<UnitEntityData>();
    static object Get(object o,string n)=>(AccessTools.Field(o.GetType(),n)??AccessTools.Field(o.GetType(),"<"+n+">k__BackingField")).GetValue(o);
    static void Set(object o,string n,object v)=>(AccessTools.Field(o.GetType(),n)??AccessTools.Field(o.GetType(),"<"+n+">k__BackingField")??throw new Exception("035 missing fixture field "+o.GetType().Name+"."+n)).SetValue(o,v);
    static T Empty<T>()where T:class=>(T)FormatterServices.GetUninitializedObject(typeof(T));
    static BlueprintGuid Id(string t)=>(BlueprintGuid)AccessTools.Method(typeof(Migration01426Tests),"Id").Invoke(null,new object[]{t});
    static T Asset<T>(BlueprintsCache c,string t)where T:SimpleBlueprint=>(T)c.Load(Id(t));
    static T Native<T>(BlueprintsCache c,string id)where T:SimpleBlueprint=>(T)c.Load(BlueprintGuid.Parse(id));
    static void Check(bool ok,string reason){if(!ok)throw new Exception("BardMasterpieces01435: "+reason);checks++;}
    static void Register(BlueprintsCache c,string method="Register")=>AccessTools.Method(Module,method).Invoke(null,new object[]{c});
    static T Ref<T>(SimpleBlueprint b)where T:BlueprintReferenceBase,new(){var r=BlueprintReferenceBase.CreateTyped<T>(b);AccessTools.Property(typeof(BlueprintReferenceBase),"Cached").SetValue(r,b,null);return r;}
    static string Definition(object value)=>(string)AccessTools.Method(Module.Assembly.GetType("CallOfTheWild.Wrath.Prototype.Migration01426"),"Definition", new[] { typeof(object) }).Invoke(null,new[]{value});
    static bool Skip()=>false;
    static string DisplayName01436(BlueprintUnitFact bp)=>bp.name;
    static UnityEngine.Sprite DisplayIcon01436(BlueprintUnitFact bp)=>null;
    static string DisplaySchool01436(SpellSchool school)=>school.ToString();
    static void ObserveChoiceVM01436(CharGenSpellSelectorItemVM __instance)=>constructedChoiceVMs036?.Add(__instance);
    static IEnumerable<CodeInstruction> DisplayBoundary01436(IEnumerable<CodeInstruction> input){
        var code=input.ToList();int names=0,icons=0,schools=0;
        for(int i=0;i<code.Count;i++){
            if(code[i].operand is MethodInfo method&&method.DeclaringType==typeof(BlueprintUnitFact)&&method.Name=="get_Name"){code[i].opcode=OpCodes.Call;code[i].operand=AccessTools.Method(typeof(BardMasterpieces01435Tests),nameof(DisplayName01436));names++;}
            else if(code[i].operand is MethodInfo icon&&icon.DeclaringType==typeof(BlueprintUnitFact)&&icon.Name=="get_Icon"){code[i].opcode=OpCodes.Call;code[i].operand=AccessTools.Method(typeof(BardMasterpieces01435Tests),nameof(DisplayIcon01436));icons++;}
            else if(code[i].operand is MethodInfo texts&&texts.DeclaringType==typeof(Kingmaker.Blueprints.Root.LocalizedTexts)&&texts.Name=="get_Instance"){
                if(i+4>=code.Count||!(code[i+1].operand is FieldInfo field)||field.Name!="SpellSchoolNames"||!(code[i+3].operand is MethodInfo school)||school.DeclaringType!=typeof(BlueprintAbility)||school.Name!="get_School"||!(code[i+4].operand is MethodInfo get)||get.Name!="GetText")throw new Exception("036 native spell-selector school display IL changed");
                var labels=code[i].labels.Concat(code[i+1].labels).ToArray();code.RemoveRange(i,2);code[i].labels.AddRange(labels);code[i+2].opcode=OpCodes.Call;code[i+2].operand=AccessTools.Method(typeof(BardMasterpieces01435Tests),nameof(DisplaySchool01436));schools++;
            }
        }
        if(names!=1||icons!=1||schools!=1)throw new Exception("036 native spell-selector display endpoints changed");displayNames036=names;displayIcons036=icons;displaySchools036=schools;return code;
    }
    static bool False(ref bool __result){__result=false;return false;}
    static bool Time(ref TimeSpan __result){__result=TimeSpan.FromHours(1);return false;}
    static bool Mode(ref Kingmaker.GameModes.GameModeType __result){__result=Kingmaker.GameModes.GameModeType.Default;return false;}
    static bool Controller(ref LevelUpController __result){__result=null;return false;}
    static bool Ignore(BlueprintFeatureSelection __instance,ref bool __result){__result=__instance.IgnorePrerequisites;return false;}
    static bool Identity(EntityFact __instance){if(string.IsNullOrEmpty(__instance.UniqueId))Set(__instance,"UniqueId","035-masterpiece-fact-"+(++serial));return true;}
    static bool Ally(UnitEntityData __instance,UnitEntityData __0,ref bool __result){__result=(__instance==hostile)==(__0==hostile);return false;}
    static bool Submit(UnitCommand __0)=>false;
    static bool CurrentUnit(ref bool __result){__result=false;return false;}
    static bool TickAllowed(ref bool __result){__result=true;return false;}
    static bool NextRound(ref float __result){__result=0;return false;}
    static bool Fx(){fxRequests++;return false;}
    static void Spent(int amount){resourceSpends+=amount;}
    static void Applied(){applications++;}
    static void BuffRuleObserved(RuleCanApplyBuff __instance){lastApplyRule=__instance;}
    static void AppendFault(BlueprintFeature leaf){if(appendFailure)throw new InvalidOperationException("035 real masterpiece append fault");}
    static bool Expose(Exception __1)=>throw new Exception("035 native masterpiece action exception",__1);
    static bool Context(ref RulebookEventContext __result){if(current==null)return true;__result=current;return false;}
    static bool Dice(DiceFormula __0,ref int __result){__result=__0.Dice==DiceType.D20?roll:0;return false;}
    static bool D20(ref RuleRollD20 __result){__result=NativeRule(new RuleRollD20(current.LastEvent<RulebookEvent>().Initiator));return false;}
    static T NativeRule<T>(T rule)where T:RulebookEvent {
        var before=current;current=new RulebookEventContext();current.PushEvent(rule);
        try{if(rule is RuleCanApplyBuff applyRule)lastApplyRule=applyRule;if(rule is RuleSavingThrow saving){saves++;lastSave=saving;}if(rule is RuleCheckConcentration concentration){nativeConcentrations++;lastConcentration=concentration;}RulebookEventBus.OnEventAboutToTrigger(rule);rule.OnTrigger(current);RulebookEventBus.OnEventDidTrigger(rule);return rule;}finally{current=before;}
    }
    static T ContextRule<T>(MechanicsContext context,T rule)where T:RulebookEvent{rule.Reason=context;return NativeRule(rule);}
    static IEnumerable<CodeInstruction> RuleBoundary(IEnumerable<CodeInstruction> input){
        foreach(var c in input){if(c.operand is MethodInfo m&&m.IsGenericMethod&&m.Name=="TriggerRule"&&m.DeclaringType==typeof(MechanicsContext)){c.opcode=OpCodes.Call;c.operand=AccessTools.Method(typeof(BardMasterpieces01435Tests),nameof(ContextRule)).MakeGenericMethod(m.GetGenericArguments());}
            else if(c.operand is MethodInfo r&&r.IsGenericMethod&&r.Name=="Trigger"&&r.DeclaringType==typeof(Rulebook)){c.opcode=OpCodes.Call;c.operand=AccessTools.Method(typeof(BardMasterpieces01435Tests),nameof(NativeRule)).MakeGenericMethod(r.GetGenericArguments());}yield return c;}
    }
    static Kingmaker.Settings.StatsAdjustmentsType Difficulty() => sceneDifficulty;
    static IEnumerable<CodeInstruction> DifficultyBoundary(IEnumerable<CodeInstruction> input){
        var codes=input.ToList();int start=codes.FindIndex(i=>i.operand is MethodInfo m&&m.DeclaringType==typeof(Kingmaker.Settings.SettingsRoot)&&m.Name=="get_Difficulty");
        if(start<0)throw new Exception("035 native difficulty boundary changed");int end=codes.FindIndex(start,i=>i.operand is MethodInfo m&&m.Name=="op_Implicit");
        if(end<start||end-start>4)throw new Exception("035 native difficulty getter chain changed");var replacement=new CodeInstruction(OpCodes.Call,AccessTools.Method(typeof(BardMasterpieces01435Tests),nameof(Difficulty)));replacement.labels.AddRange(codes[start].labels);codes.RemoveRange(start,end-start+1);codes.Insert(start,replacement);return codes;
    }
    static UnitEntityData Unit(BlueprintsCache cache,string label,string cls=Bard,int level=12){
        var u=(UnitEntityData)AccessTools.Method(typeof(FlameJetLifecycle01430Tests),"Unit").Invoke(null,new object[]{cache,"035-masterpiece-"+label});
        u.Progression.Classes.Clear();u.Progression.Classes.Add(new ClassData(Native<BlueprintCharacterClass>(cache,cls)){Level=level});
        Set(u,"UniqueId","035-masterpiece-unit-"+(++serial));var proxy=FormatterServices.GetUninitializedObject(AccessTools.Inner(typeof(EntityService),"Proxy"));Set(proxy,"Entity",u);Set(u,"Proxy",proxy);
        Set(u.Descriptor,"Resources",new UnitAbilityResourceCollection(u.Descriptor));Set(u,"Abilities",u.Facts.EnsureFactProcessor<AbilityCollection>());Set(u,"ActivatableAbilities",u.Facts.EnsureFactProcessor<ActivatableAbilityCollection>());
        Set(u,"CombatState",Empty<UnitCombatState>());Set(u.CombatState,"Unit",u);Set(u.CombatState,"Cooldown",new UnitCombatState.Cooldowns());Set(u.CombatState,"m_InCombat",true);Set(u,"Commands",new UnitCommands(u));
        Set(u.Descriptor,"UISettings",Empty<Kingmaker.UI.UnitSettings.UnitUISettings>());
        Set(u,"m_IsInGame",true);Set(u,"HoldingState",Empty<SceneEntitiesState>());if(u.Passive==null)Set(u,"Passive",new ObservableCountFlag());
        u.Stats.Charisma.BaseValue=18;u.Stats.SaveWill.BaseValue=0;u.Stats.BaseAttackBonus.BaseValue=10;units.Add(u);return u;
    }
    static Feature Fact(UnitEntityData u,BlueprintFeature bp){var f=new Feature(bp,u.Descriptor);u.Facts.Add(f);Check(f.IsActive&&f.IsTurnedOn,"native feature activation "+bp.name);return f;}
    static Buff Buff(UnitEntityData u,BlueprintBuff bp,UnitEntityData caster=null){var context=new MechanicsContext(caster??u,u.Descriptor,bp);var b=new Buff(bp,context,null);u.Facts.Add(b);Check(b.IsActive&&b.IsTurnedOn,"native buff activation "+bp.name);return b;}
    static BlueprintBuff Custom(string name,params BlueprintComponent[] components){var b=new BlueprintBuff{name="035-"+name,AssetGuid=BlueprintGuid.Parse(Guid.NewGuid().ToString("N")),Stacking=StackingType.Stack,ComponentsArray=components};b.OnEnable();return b;}
    static bool CanSelect(UnitEntityData u,BlueprintFeatureSelection p,BlueprintFeature f)=>(bool)AccessTools.Method(typeof(EnhancementLifecycle01430Tests),"CanSelect").Invoke(null,new object[]{u,p,f});
    static void Hydrate(BlueprintsCache cache){
        foreach(string id in new[]{Bard,Skald,BardPool,SkaldPool,BardGrant,SkaldGrant,"1533e782fca42b84ea370fc1dcbf4fc1","09d39b38bb7c6014394b6daced9bacd3","df3950af5a783bd4d91ab73eb8fa0fd3","0ec36e7596a4928489d2049e1e1c76a7","25ec6cb6ab1845c48a95f9c20b034220","734a29b693e9ec346ba2951b27987e33","9054d3988d491d944ac144e27b6bc318","17239b298065efc459cffe2220ecb559"}){
            var bp=Native<BlueprintScriptableObject>(cache,id);var row=PsychicExpansionTests.NativeRows.FirstOrDefault(r=>(string)r["blueprint"]["AssetId"]==id);
            if(bp is BlueprintAbilityResource&&row!=null)foreach(string field in new[]{"m_MaxAmount","m_UseMax","m_Max","m_Min"}){var raw=row["blueprint"]["Data"][field];Set(bp,field,PsychicExpansionTests.Read(raw,AccessTools.Field(bp.GetType(),field).FieldType));}
            if(bp!=null&&bp.ComponentsArray.Length==0&&row!=null&&((JArray)row["blueprint"]["Data"]["Components"]).Count>0){bp.ComponentsArray=((JArray)row["blueprint"]["Data"]["Components"]).Select(c=>(BlueprintComponent)PsychicExpansionTests.Read(c,typeof(BlueprintComponent))).ToArray();bp.OnEnable();}
        }
    }
    const string PpCore="0901ea8f1cae465da272bab8fee863e4",PpSelection="22df5ebca04e4c3bb72bd6a032df26bb",BardList="25a5013493bdcf74bb2424532214d0c8",BardBook="bc04fc157a8801d41b877ad0d9af03dd";
    static readonly string[] PpFeatures={"ea78acb4d56044c3a6da2e3ddd20006b","c73eb87ced4e42e882c9c73c793b8797","21606a268ea7453892e23ac549548efa","9185044273244e208f00845cf44b81c4","2ebc866b610a465583032c5006f55bc3"};
    static readonly string[] PpAbilities={"ad42caf043154c088ebb1fb888f32f84","b2c149bdce2347539d759a74c642c9e3","df05fc9b9f394d26ad3a51e21ec6ac06","c897a7577c0a4e68b733b3c655c9eb79","292c89759fa14af383d5b7dbccf72b92"};
    // Metadata fixture bound to official PP da29f683 Blueprint/Masterpiece.cs.
    // It does not load or start PrestigePlus.dll and never substitutes for the
    // native AddKnown/SelectSpell/RemoveSpell methods exercised below.
    internal static void InstallPrestigeProvider01436(BlueprintsCache cache,int shape=0){
        if(shape==1)return;
        var core=new BlueprintFeature{AssetGuid=BlueprintGuid.Parse(PpCore),name="BardMasterpiece.BardMasterpieceCore",ComponentsArray=Array.Empty<BlueprintComponent>()};core.OnEnable();cache.AddCachedBlueprint(core.AssetGuid,core);
        if(shape==3){var wrong=new BlueprintFeature{AssetGuid=BlueprintGuid.Parse(PpSelection),name="BardMasterpiece.BardMasterpiece",ComponentsArray=Array.Empty<BlueprintComponent>()};wrong.OnEnable();cache.AddCachedBlueprint(wrong.AssetGuid,wrong);return;}
        var requires=new PrerequisiteFeature();Set(requires,"m_Feature",Ref<BlueprintFeatureReference>(core));
        var selection=new BlueprintFeatureSelection{AssetGuid=BlueprintGuid.Parse(PpSelection),name=shape==4?"ForeignMasterpiece":"BardMasterpiece.BardMasterpiece",IgnorePrerequisites=false,Obligatory=false,ComponentsArray=new BlueprintComponent[]{requires}};
        var features=new List<BlueprintFeatureReference>();var extra=Native<BlueprintFeature>(cache,"0d3651b2cb0d89448b112e23214e744e");if(extra!=null)features.Add(Ref<BlueprintFeatureReference>(extra));
        string[] names={"TripleTime","TwistingSteel","StoneFace","RatQuadrille","PriestKing"};int[] levels={1,2,3,2,3};
        for(int i=0;i<names.Length-(shape==2?1:0);i++){
            var spellList=new SpellListComponent{SpellLevel=levels[i]};Set(spellList,"m_SpellList",Ref<BlueprintSpellListReference>(Native<BlueprintSpellList>(cache,BardList)));
            var ability=new BlueprintAbility{AssetGuid=BlueprintGuid.Parse(PpAbilities[i]),name="Masterpiece.Use"+names[i],Type=AbilityType.Supernatural,ComponentsArray=new BlueprintComponent[]{spellList}};ability.OnEnable();cache.AddCachedBlueprint(ability.AssetGuid,ability);
            var add=new AddFacts();Set(add,"m_Facts",new[]{Ref<BlueprintUnitFactReference>(ability)});var learn=new LearnSpells();Set(learn,"m_CharacterClass",Ref<BlueprintCharacterClassReference>(Native<BlueprintCharacterClass>(cache,Bard)));Set(learn,"m_Spells",new[]{Ref<BlueprintAbilityReference>(ability)});
            var feature=new BlueprintFeature{AssetGuid=BlueprintGuid.Parse(PpFeatures[i]),name="Masterpiece."+names[i],ComponentsArray=new BlueprintComponent[]{add,learn}};feature.OnEnable();cache.AddCachedBlueprint(feature.AssetGuid,feature);features.Add(Ref<BlueprintFeatureReference>(feature));
        }
        Set(selection,"m_Features",Array.Empty<BlueprintFeatureReference>());Set(selection,"m_AllFeatures",features.ToArray());selection.OnEnable();cache.AddCachedBlueprint(selection.AssetGuid,selection);
    }
    static void HydrateBook(BlueprintsCache cache){
        var book=Native<BlueprintSpellbook>(cache,BardBook)??throw new Exception("036 native Bard book fixture missing");
        var row=PsychicExpansionTests.NativeRows.First(r=>(string)r["blueprint"]["AssetId"]==BardBook)["blueprint"]["Data"];
        foreach(string field in new[]{"m_CharacterClass","m_SpellList","m_SpellsPerDay","m_SpellsKnown","Spontaneous","CastingAttribute","AllSpellsKnown","IsArcane"}){var info=AccessTools.Field(typeof(BlueprintSpellbook),field);if(info!=null&&row[field]!=null)info.SetValue(book,PsychicExpansionTests.Read(row[field],info.FieldType));}
        foreach(var table in new[]{book.SpellsPerDay,book.SpellsKnown}){var native=PsychicExpansionTests.NativeRows.First(r=>(string)r["blueprint"]["AssetId"]==table.AssetGuid.ToString())["blueprint"]["Data"];table.Levels=(SpellsLevelEntry[])PsychicExpansionTests.Read(native["Levels"],typeof(SpellsLevelEntry[]));}
    }
    static Spellbook Book(UnitEntityData owner,BlueprintsCache cache,bool wrong=false){
        var source=Native<BlueprintSpellbook>(cache,BardBook);var blueprint=source;
        if(wrong){blueprint=new BlueprintSpellbook{AssetGuid=BlueprintGuid.Parse(Guid.NewGuid().ToString("N")),name="036-foreign-book",Spontaneous=true,ComponentsArray=Array.Empty<BlueprintComponent>()};Set(blueprint,"m_CharacterClass",Ref<BlueprintCharacterClassReference>(Native<BlueprintCharacterClass>(cache,Skald)));Set(blueprint,"m_SpellList",Ref<BlueprintSpellListReference>(source.SpellList));Set(blueprint,"m_SpellsPerDay",Get(source,"m_SpellsPerDay"));Set(blueprint,"m_SpellsKnown",Get(source,"m_SpellsKnown"));blueprint.CastingAttribute=StatType.Charisma;blueprint.OnEnable();}
        Set(owner.Descriptor,"m_Spellbooks",new Dictionary<BlueprintSpellbook,Spellbook>());var book=new Spellbook(owner.Descriptor,blueprint);Set(book,"m_BaseLevelInternal",owner.Progression.Classes.First().Level);((IDictionary)Get(owner.Descriptor,"m_Spellbooks")).Add(blueprint,book);return book;
    }
    static LevelUpState ChoiceState(Spellbook book,int level){var state=Empty<LevelUpState>();Set(state,"m_Unit",book.Owner.Unit);Set(state,"Selections",new List<FeatureSelectionState>());Set(state,"SpellSelections",new List<SpellSelectionData>());Set(state,"SelectedClass",book.Blueprint.CharacterClass);var select=state.DemandSpellSelection(book.Blueprint,book.Blueprint.SpellList);for(int i=0;i<11;i++)select.SetLevelSpells(i,i==level?1:0);return state;}
    static MasterpieceKnownState01436 Receipt(UnitEntityData u)=>u.Progression.Features.Enumerable.Single(f=>f.Blueprint.AssetGuid==Id("BardMasterpieceKnownSource01436")).Components.Single(c=>c.SourceBlueprintComponent is MasterpieceKnownSource01436).GetData<MasterpieceKnownState01436>();
    static bool NativeCheck(SelectSpell action,LevelUpState state,UnitDescriptor owner)=>(bool)AccessTools.Method(typeof(SelectSpell),nameof(SelectSpell.Check)).Invoke(action,new object[]{state,owner});
    static void PrestigeFault(){if(prestigeFailure)throw new InvalidOperationException("036 source-faithful PP append injected failure");}
    static void RowCacheFault01436(object __0,string __1,object __2){
        if(!rowCacheFailure||!ReferenceEquals(__0,rowCacheFaultTarget)||__1!="m_SpellsFiltered"||__2!=null)return;
        rowCacheFaultCount036++;rowCacheFaultObserved=((List<BlueprintAbilityReference>)Get(__0,"m_Spells")).Any(r=>r.Guid==Id(Tokens[0]+"KnownSpellMarker"))&&Get(__0,"m_SpellsProxy")==null&&Get(__0,"m_SpellsFiltered")==null;
        throw new InvalidOperationException("036 post-write Bard row cache fault");
    }
    static List<CharGenSpellSelectorItemVM> ChoiceList(Spellbook book,BlueprintSpellList list,int level,BlueprintAbility[] selected=null){
        var phase=Empty<CharGenSpellsPhaseVM>();Set(phase,"PreviewSpellbook",book);Set(phase,"EndLevel",level);Set(phase,"StartLevel",level);Set(phase,"MechanicSelectedSpells",selected??Array.Empty<BlueprintAbility>());
        var controller=Empty<LevelUpController>();Set(controller,"State",ChoiceState(book,level));uiBuilt036++;
        var prior=constructedChoiceVMs036;constructedChoiceVMs036=new List<CharGenSpellSelectorItemVM>();
        try{
            var result=(List<CharGenSpellSelectorItemVM>)AccessTools.Method(typeof(CharGenSpellsPhaseVM),"CreateSpellVMsCollection").Invoke(phase,new object[]{controller,list});var removed=constructedChoiceVMs036.Where(v=>!result.Contains(v)).ToArray();
            Check(removed.All(v=>v.IsDisposed&&v.Recommendation.IsDisposed),"filtered actual native marker VMs dispose their recommendation/owned subscriptions");Check(result.All(v=>!v.IsDisposed),"actual ordinary and retained native choice VMs stay alive");disposedChoiceVMs036+=removed.Length;return result;
        }finally{constructedChoiceVMs036=prior;}
    }
    static void Access01436(BlueprintsCache cache,Harmony h){
        int first=checks;learned036=removed036=selected036=uiBuilt036=loaded036=copied036=0;HydrateBook(cache);
        displayNames036=displayIcons036=displaySchools036=disposedChoiceVMs036=prewarmedSpellRows036=rowCacheFaultCount036=0;h.Patch(AccessTools.Constructor(typeof(CharGenSpellSelectorItemVM),new[]{typeof(BlueprintAbility),typeof(Kingmaker.UI.MVVM._VM.InfoWindow.InfoSectionVM),typeof(int),typeof(int),typeof(bool)}),postfix:new HarmonyMethod(typeof(BardMasterpieces01435Tests),nameof(ObserveChoiceVM01436)),transpiler:new HarmonyMethod(typeof(BardMasterpieces01435Tests),nameof(DisplayBoundary01436)));
        foreach(string name in new[]{"MasterpieceLearn01436","MasterpieceForget01436","MasterpieceBookLoad01436","MasterpieceBookRemoved01436","MasterpieceOtherSource01436","MasterpieceSelect01436","MasterpieceChoiceList01436"})h.CreateClassProcessor(Module.Assembly.GetType("CallOfTheWild.Wrath.Prototype."+name,true)).Patch();
        var list=Native<BlueprintSpellList>(cache,BardList);var originalRows=list.SpellsByLevel;var rowLists=originalRows.Select(r=>Get(r,"m_Spells")).ToArray();var rowProxies=originalRows.Select(r=>Get(r,"m_SpellsProxy")).ToArray();var rowFiltered=originalRows.Select(r=>Get(r,"m_SpellsFiltered")).ToArray();var oldPpIds=new[]{PpCore,PpSelection}.Concat(PpFeatures).Concat(PpAbilities).ToArray();var originals=oldPpIds.Select(id=>cache.Load(BlueprintGuid.Parse(id))).ToArray();
        try{
            var legacy=Asset<BlueprintFeatureSelection>(cache,"BardicMasterpieces01435Selection");Check(legacy.HideInUI&&legacy.HideInCharacterSheetAndLevelUp&&!legacy.AllFeatures.Any(),"036 old container remains resolved but hidden and empty");
            Check(!list.SpellsByLevel.Any(r=>r.Spells.Any(a=>a?.GetComponent<MasterpieceKnownMarker01436>()!=null)),"PP absent: cached private markers are not ordinary learnable Bard spells");
            foreach(int circle in new[]{2,3,5}){Check(list.GetSpells(circle).Count>0&&Get(list.SpellsByLevel[circle],"m_SpellsFiltered")!=null,"actual native Bard GetSpells cache warmed before optional provider circle "+circle);prewarmedSpellRows036++;}var warmedFiltered=originalRows.Select(r=>Get(r,"m_SpellsFiltered")).ToArray();var warmedProxies=originalRows.Select(r=>Get(r,"m_SpellsProxy")).ToArray();
            InstallPrestigeProvider01436(cache);var pool=Native<BlueprintFeatureSelection>(cache,PpSelection);var beforeAll=(BlueprintFeatureReference[])Get(pool,"m_AllFeatures");var beforeFeatures=Get(pool,"m_Features");
            h.Patch(AccessTools.Method(Module,"AppendPrestige"),postfix:new HarmonyMethod(typeof(BardMasterpieces01435Tests),nameof(PrestigeFault)));prestigeFailure=true;Register(cache,"Optional");prestigeFailure=false;
            Check(ReferenceEquals(beforeAll,Get(pool,"m_AllFeatures"))&&ReferenceEquals(beforeFeatures,Get(pool,"m_Features"))&&rowLists.SequenceEqual(list.SpellsByLevel.Select(r=>Get(r,"m_Spells")))&&warmedFiltered.SequenceEqual(list.SpellsByLevel.Select(r=>Get(r,"m_SpellsFiltered")))&&warmedProxies.SequenceEqual(originalRows.Select(r=>Get(r,"m_SpellsProxy"))),"actual PP post-append failure restores exact provider arrays, row lists and prewarmed filtered caches");Check(((string)AccessTools.Field(Module,"LastOptionalDiagnostic").GetValue(null)).Contains("036 source-faithful PP append injected failure"),"optional integration keeps actual error diagnostic");
            h.Patch(AccessTools.Method(Module.Assembly.GetType("CallOfTheWild.Wrath.Prototype.PsychicAssets"),"Set"),postfix:new HarmonyMethod(typeof(BardMasterpieces01435Tests),nameof(RowCacheFault01436)));rowCacheFaultTarget=originalRows[5];rowCacheFaultObserved=false;rowCacheFailure=true;Register(cache,"Optional");rowCacheFailure=false;
            Check(rowCacheFaultObserved,"injected error occurs after actual raw-list write and native proxy/filtered invalidation");
            Check(ReferenceEquals(beforeAll,Get(pool,"m_AllFeatures"))&&ReferenceEquals(beforeFeatures,Get(pool,"m_Features"))&&rowLists.SequenceEqual(originalRows.Select(r=>Get(r,"m_Spells")))&&warmedFiltered.SequenceEqual(originalRows.Select(r=>Get(r,"m_SpellsFiltered")))&&warmedProxies.SequenceEqual(originalRows.Select(r=>Get(r,"m_SpellsProxy"))),"post-write failure restores original PP arrays, raw spell lists and prewarmed filtered objects");
            Check(((string)AccessTools.Field(Module,"LastOptionalDiagnostic").GetValue(null)).Contains("036 post-write Bard row cache fault"),"post-write rollback records its actual failure");Register(cache,"Optional");Register(cache,"Optional");
            Check(ReferenceEquals(originalRows,list.SpellsByLevel)&&originalRows.Select((r,i)=>r.SpellLevel==i).All(x=>x),"Bard list array length/index and row objects remain native");
            Check(((BlueprintFeatureReference[])Get(pool,"m_AllFeatures")).Take(beforeAll.Length).SequenceEqual(beforeAll),"all original PP masterpiece child references/order retained");
            int[] circles={5,2,3};
            for(int i=0;i<3;i++){
                var root=Asset<BlueprintFeature>(cache,Tokens[i]);var marker=Asset<BlueprintAbility>(cache,Tokens[i]+"KnownSpellMarker");
                Check(pool.AllFeatures.Count(a=>a.AssetGuid==root.AssetGuid)==1&&list.SpellsByLevel[circles[i]].Spells.Count(a=>a?.AssetGuid==marker.AssetGuid)==1,"one actual PP root and native Bard pseudo choice at intended circle "+i);
                Check(list.GetSpells(circles[i]).Count(a=>a?.AssetGuid==marker.AssetGuid)==1,"actual native GetSpells sees the marker after optional integration of prewarmed circle "+circles[i]);
                var u=Unit(cache,"036-paid"+i,Bard,new[]{13,4,7}[i]);Fact(u,Native<BlueprintFeature>(cache,PpCore));Fact(u,Native<BlueprintFeature>(cache,BardGrant));var book=Book(u,cache);var state=ChoiceState(book,circles[i]);var choice=new SelectSpell(book.Blueprint,list,circles[i],marker,0);
                Console.WriteLine("036 native Bard unresolved refs: "+string.Join(",",list.SpellsByLevel[circles[i]].SpellsRefs.Where(r=>r.Get()==null).Select(r=>r.Guid.ToString())));var choices=ChoiceList(book,list,circles[i]);Check(choices.Any(v=>v.Spell==marker),"actual native char-gen list includes eligible masterpiece "+i);foreach(var v in choices)v.Dispose();
                Check(NativeCheck(choice,state,u.Descriptor),"actual native SelectSpell.Check permits valid known choice "+i);choice.Apply(state,u.Descriptor);selected036++;learned036++;
                Check(book.IsKnownOnLevel(marker,circles[i])&&u.Descriptor.HasFact(root)&&u.ActivatableAbilities.RawFacts.Count==1,"actual native known selection immediately grants existing root and paid resource leaf "+i);
                Check(state.SpellSelections[0].LevelCount[circles[i]].SpellSelections[0]==marker,"one actual known-selection slot is consumed "+i);
                var rebuilt=ChoiceList(book,list,circles[i],new[]{marker});Check(rebuilt.Any(v=>v.Spell==marker&&!v.HasInSpellbook),"native phase rebuild preserves current selected masterpiece after preview grant "+i);foreach(var v in rebuilt)v.Dispose();
                var freshState=ChoiceState(book,circles[i]);Check(!NativeCheck(new SelectSpell(book.Blueprint,list,circles[i],marker,0),freshState,u.Descriptor),"already learned masterpiece cannot occupy a new known slot "+i);var freshChoices=ChoiceList(book,list,circles[i]);Check(!freshChoices.Any(v=>v.Spell==marker),"phase without current selection does not offer an already known mastery "+i);foreach(var v in freshChoices)v.Dispose();
                var knownBefore=book.GetKnownSpell(marker);book.AddKnown(circles[i],marker,false);learned036++;Check(ReferenceEquals(knownBefore,book.GetKnownSpell(marker))&&book.GetKnownSpells(circles[i]).Count(a=>a.Blueprint==marker)==1,"replayed exact-marker AddKnown retains one native known entry "+i);
                var nativeAlternative=list.SpellsByLevel[circles[i]].Spells.First(a=>a!=null&&a.GetComponent<MasterpieceKnownMarker01436>()==null);book.RemoveSpell(marker);removed036++;state.SpellSelections[0].SpendSlot(circles[i],null,0);
                var alternativeChoice=new SelectSpell(book.Blueprint,list,circles[i],nativeAlternative,0);Check(NativeCheck(alternativeChoice,state,u.Descriptor),"cancelled marker slot can select an ordinary native spell "+i);alternativeChoice.Apply(state,u.Descriptor);selected036++;Check(!u.Descriptor.HasFact(root)&&book.IsKnown(nativeAlternative),"replacement does not retain cancelled mastery "+i);book.RemoveSpell(nativeAlternative);removed036++;state.SpellSelections[0].SpendSlot(circles[i],null,0);Check(NativeCheck(choice,state,u.Descriptor),"cancelled choice can be selected again without relaxed prerequisites "+i);choice.Apply(state,u.Descriptor);selected036++;learned036++;
                Check(!marker.GetComponent<MasterpieceKnownMarker01436>().IsAbilityRestrictionPassed(book.GetKnownSpell(marker))&&!marker.GetComponent<MasterpieceKnownMarker01436>().IsAbilityVisible(book.GetKnownSpell(marker)),"pseudo choice cannot cast from spellbook or appear as an extra castable ability "+i);
                var fact=u.Progression.Features.Enumerable.Single(f=>f.Blueprint==root);var active=Toggle(u,Tokens[i],false);u.Resources.Restore(Native<BlueprintAbilityResource>(cache,BardPool));int amount=u.Resources.GetResourceAmount(Native<BlueprintAbilityResource>(cache,BardPool));
                var receipt=Receipt(u);var savedSettings=new JsonSerializerSettings{ContractResolver=new Kingmaker.EntitySystem.Persistence.JsonUtility.OptInContractResolver()};var saved=JObject.FromObject(receipt,JsonSerializer.Create(savedSettings));Check(saved.Properties().Select(p=>p.Name).SequenceEqual(new[]{"Owned"})&&saved["Owned"] is JArray owned&&owned.Count==3,"native OptIn resolver serializes the three source receipts only "+i);receipt.Owned=new EntityFactRef<Feature>[3];JsonConvert.PopulateObject(saved.ToString(),receipt,savedSettings);Check(receipt.Owned[i].FactId==fact.UniqueId,"serialized ownership restores exact native fact id "+i);
                book.PostLoad(u.Descriptor);loaded036++;Check(ReferenceEquals(fact,u.Progression.Features.Enumerable.Single(f=>f.Blueprint==root))&&u.Resources.GetResourceAmount(Native<BlueprintAbilityResource>(cache,BardPool))==amount,"actual Spellbook.PostLoad keeps same owned root and does not refill its resource "+i);
                u.Facts.TurnOff();u.Facts.TurnOn();u.Facts.ApplyPostLoadFixes();Check(ReferenceEquals(fact,u.Progression.Features.Enumerable.Single(f=>f.Blueprint==root))&&u.ActivatableAbilities.RawFacts.Count==1&&Receipt(u).Owned[i].FactId==fact.UniqueId,"native fact-manager suspension/post-load preserves JSON-restored own grant "+i);
                book.RemoveSpell(marker);removed036++;Check(!u.Descriptor.HasFact(root)&&!u.ActivatableAbilities.RawFacts.Contains(active),"native RemoveSpell revokes only source's root and leaf "+i);
                book.AddKnown(circles[i],marker,false);learned036++;Check(u.Descriptor.HasFact(root),"event-disabled native AddKnown still synchronizes own exact marker "+i);book.RemoveSpell(marker);removed036++;
                book.AddKnownTemporary(circles[i],marker);Check(!u.Descriptor.HasFact(root),"temporary known pseudo spell is not a paid mastery source "+i);book.AddKnown(circles[i],marker,false);learned036++;Check(u.Descriptor.HasFact(root),"promoting native temporary known to permanent creates mastery "+i);book.RemoveSpell(marker);removed036++;
                var low=Unit(cache,"036-low"+i,Bard,new[]{9,3,6}[i]);Fact(low,Native<BlueprintFeature>(cache,PpCore));var lowBook=Book(low,cache);var lowState=ChoiceState(lowBook,circles[i]);Check(!NativeCheck(new SelectSpell(lowBook.Blueprint,list,circles[i],marker,0),lowState,low.Descriptor),"native commit denies lower class-level prerequisite even if a known slot is externally offered "+i);var lowChoices=ChoiceList(lowBook,list,circles[i]);Check(!lowChoices.Any(v=>v.Spell==marker),"actual native choice data filters unmet masterpiece prerequisite "+i);foreach(var v in lowChoices)v.Dispose();
                var foreign=Unit(cache,"036-foreign"+i);Fact(foreign,Native<BlueprintFeature>(cache,PpCore));var foreignBook=Book(foreign,cache,true);var foreignState=ChoiceState(foreignBook,circles[i]);Check(!NativeCheck(new SelectSpell(foreignBook.Blueprint,list,circles[i],marker,0),foreignState,foreign.Descriptor),"unrelated spellbook sharing Bard list cannot buy Bard mastery "+i);foreignBook.AddKnown(circles[i],marker,false);Check(!foreign.Descriptor.HasFact(root),"unrelated book native AddKnown never leaks a mastery "+i);
            }
            var danceRoot=Asset<BlueprintFeature>(cache,Tokens[1]);var danceMarker=Asset<BlueprintAbility>(cache,Tokens[1]+"KnownSpellMarker");var borrowed=Unit(cache,"036-foreign-feature");Fact(borrowed,Native<BlueprintFeature>(cache,BardGrant));var original=Fact(borrowed,danceRoot);var borrowedBook=Book(borrowed,cache);borrowedBook.AddKnown(2,danceMarker,false);learned036++;borrowedBook.RemoveSpell(danceMarker);removed036++;Check(original.Manager!=null&&borrowed.Descriptor.HasFact(danceRoot),"removing known source never removes pre-existing feat source");
            Fact(borrowed,Native<BlueprintFeature>(cache,PpCore));Check(!NativeCheck(new SelectSpell(borrowedBook.Blueprint,list,2,danceMarker,0),ChoiceState(borrowedBook,2),borrowed.Descriptor),"an independent feat source cannot buy a duplicate known choice");var foreignChoices=ChoiceList(borrowedBook,list,2);Check(!foreignChoices.Any(v=>v.Spell==danceMarker),"independent feat filters marker from a new selection");foreach(var v in foreignChoices)v.Dispose();
            borrowedBook.AddKnown(2,danceMarker,false);learned036++;borrowed.Facts.Remove(original);Check(borrowed.Descriptor.HasFact(danceRoot),"native source removal falls back to still-known masterpiece");borrowedBook.RemoveSpell(danceMarker);removed036++;Check(!borrowed.Descriptor.HasFact(danceRoot),"removing last known source removes restored fallback");
            var handoff=Unit(cache,"036-handoff");Fact(handoff,Native<BlueprintFeature>(cache,BardGrant));var hb=Book(handoff,cache);hb.AddKnown(2,danceMarker,false);learned036++;var paid=handoff.Progression.Features.Enumerable.Single(f=>f.Blueprint==danceRoot);var external=new Feature(danceRoot,handoff.Descriptor);handoff.Facts.Add(external);hb.RemoveSpell(danceMarker);removed036++;Check(paid.Manager!=null&&handoff.Descriptor.HasFact(danceRoot),"native rank-one merge transfers owned fact to a later independent source");handoff.Facts.Remove(paid);Check(!handoff.Descriptor.HasFact(danceRoot),"native independent source can still be removed normally after handoff");
            var source=Unit(cache,"036-copy-source");Fact(source,Native<BlueprintFeature>(cache,PpCore));Fact(source,Native<BlueprintFeature>(cache,BardGrant));var sourceBook=Book(source,cache);sourceBook.AddKnown(2,danceMarker,false);learned036++;var target=Unit(cache,"036-preview");Fact(target,Native<BlueprintFeature>(cache,PpCore));Fact(target,Native<BlueprintFeature>(cache,BardGrant));var targetBook=Book(target,cache);AccessTools.Method(typeof(UnitHelper),"CopySpellbook").Invoke(null,new object[]{source,target});copied036++;
            Check(sourceBook.IsKnown(danceMarker)&&targetBook.IsKnown(danceMarker)&&source.Descriptor.HasFact(danceRoot)&&target.Descriptor.HasFact(danceRoot),"actual native CopySpellbook grants preview only on its own owner");targetBook.RemoveSpell(danceMarker);removed036++;Check(source.Descriptor.HasFact(danceRoot)&&!target.Descriptor.HasFact(danceRoot),"preview removal cannot mutate original known-source facts");AccessTools.Method(typeof(UnitDescriptor),nameof(UnitDescriptor.DeleteSpellbook),new[]{typeof(BlueprintSpellbook)}).Invoke(source.Descriptor,new object[]{sourceBook.Blueprint});Check(!source.Descriptor.HasFact(danceRoot),"native DeleteSpellbook removes mastery owned solely by the deleted book");
            var convertedOwner=Unit(cache,"036-conversion");Fact(convertedOwner,Native<BlueprintFeature>(cache,PpCore));var convertedBook=Book(convertedOwner,cache);var ordinary=list.SpellsByLevel[2].Spells.First(a=>a!=null&&a.GetComponent<MasterpieceKnownMarker01436>()==null);var ordinaryData=convertedBook.AddKnown(2,ordinary,false);var convertedData=new AbilityData(ordinaryData,danceMarker);convertedData.ConvertedFrom=ordinaryData;Check(!convertedOwner.Descriptor.HasFact(danceRoot)&&!danceMarker.GetComponent<MasterpieceKnownMarker01436>().IsAbilityRestrictionPassed(convertedData),"native conversion data is not a known source and marker cannot be cast after conversion");
            Check(displayNames036==1&&displayIcons036==1&&displaySchools036==1&&disposedChoiceVMs036>0,"exact three display IL endpoints and positive native filtered-VM disposal count");
            var own=(string[])AccessTools.Field(Module,"OwnTokens").GetValue(null);var added=own.Skip(23).Select(t=>cache.Load(Id(t))).ToArray();
            PsychicExpansionTests.Inventory["bardMasterpieceAccess01436Contracts"]=new JObject{["passed"]=true,["assertions"]=checks-first,["additionalItemCount"]=0,["newCombatSpellRoots"]=0,["repairItemIds"]=new JArray("COTW-0378","COTW-0379","COTW-0380"),["addedBlueprints"]=JArray.FromObject(added.Select(a=>new{id=a.AssetGuid.ToString(),name=a.name,type=a.GetType().FullName})),["nativeLearnCalls"]=learned036,["nativeRemoveCalls"]=removed036,["nativeSelectSpellApplies"]=selected036,["nativeCharGenChoiceBuilds"]=uiBuilt036,["nativeSpellbookPostLoads"]=loaded036,["nativeCopySpellbookCalls"]=copied036,["prestigePoolAndKnownCircles"]=true,["knownSourceLifecycleAndForeignOwnership"]=true,["nativeSelectionPrerequisiteFiltering"]=true,["optionalFaultRollbackAndRetry"]=true,["legacyContainerRetired"]=true,["nativeSelectedReceiptSurvivesListRebuild"]=true,["nativeDuplicateAndCancelledSlotBoundaries"]=true,["nativeOwnedReceiptJsonAndPostLoad"]=true,["nativePrewarmedSpellRowCachesAndRollback"]=true,["nativePrewarmedSpellRows"]=prewarmedSpellRows036,["nativePostWriteCacheFaults"]=rowCacheFaultCount036,["nativeFilteredChoiceVmDisposals"]=disposedChoiceVMs036,["nativeChoiceDisplayIlReplacements"]=new JObject{["rawName"]=displayNames036,["nullSprite"]=displayIcons036,["schoolText"]=displaySchools036},["providerFixtureBoundary"]="PrestigePlus exact official da29f683 core/selection/original five child metadata constructed; PP DLL is not started. Actual native learning/selection/list building/removal/book copy/PostLoad and fact/resource lifecycle execute. Only the native spell-selector constructor display endpoints (raw name, null Sprite, school text) are replaced after exact IL shape checks (one site each); actual list/known/selected logic runs. Native OptIn receipt serialization and fact-manager post-load run; rendered UI and full game saves remain game regression cases."};
        }finally{prestigeFailure=rowCacheFailure=false;rowCacheFaultTarget=null;for(int i=0;i<originalRows.Length;i++){Set(originalRows[i],"m_Spells",rowLists[i]);Set(originalRows[i],"m_SpellsProxy",rowProxies[i]);Set(originalRows[i],"m_SpellsFiltered",rowFiltered[i]);}for(int i=0;i<oldPpIds.Length;i++){if(originals[i]==null)cache.RemoveCachedBlueprint(BlueprintGuid.Parse(oldPpIds[i]));else cache.AddCachedBlueprint(BlueprintGuid.Parse(oldPpIds[i]),originals[i]);}}
    }
    static void Registration(BlueprintsCache cache,Harmony h){
        Hydrate(cache);var basic=Native<BlueprintFeatureSelection>(cache,"247a4068296e8be42890143f451b4b45");var prior=((BlueprintFeatureReference[])Get(basic,"m_AllFeatures")).ToArray();
        var donors=new[]{BardPool,SkaldPool,BardGrant,SkaldGrant,"1533e782fca42b84ea370fc1dcbf4fc1","09d39b38bb7c6014394b6daced9bacd3"}.Select(id=>Native<BlueprintScriptableObject>(cache,id)).ToArray();var definitions=donors.Select(b=>Definition(b.ComponentsArray)).ToArray();
        Register(cache);var container=Asset<BlueprintFeatureSelection>(cache,"BardicMasterpieces01435Selection");
        Check(container.Ranks==10&&!container.IgnorePrerequisites&&container.HideInUI&&container.HideInCharacterSheetAndLevelUp,"old container identity retained hidden for old saves");
        Check(!container.AllFeatures.Any(),"old hidden container has no selectable children");
        var proofPool=new BlueprintFeatureSelection{IgnorePrerequisites=false,ComponentsArray=Array.Empty<BlueprintComponent>()};Set(proofPool,"m_AllFeatures",Tokens.Select(t=>Ref<BlueprintFeatureReference>(Asset<BlueprintFeature>(cache,t))).ToArray());Set(proofPool,"m_Features",Get(proofPool,"m_AllFeatures"));proofPool.OnEnable();
        Check(((BlueprintFeatureReference[])Get(basic,"m_AllFeatures")).Take(prior.Length).SequenceEqual(prior),"all original native feat references/order retained");
        Check(((BlueprintFeatureReference[])Get(basic,"m_AllFeatures")).Count(r=>r.Guid==container.AssetGuid)==0,"retired ordinary container is never published");
        for(int i=0;i<3;i++){
            var root=Asset<BlueprintFeature>(cache,Tokens[i]);Check(root.Groups.Length==0,"masterpiece leaves are selectable only through their real provider child pool");
            Check(!container.AllFeatures.Any(),"legacy hidden container remains empty "+Tokens[i]);
            foreach(string cls in new[]{Bard,Skald,"f7d7eb166b3dd594fb330d085df41853"})foreach(int level in new[]{3,4,6,7,9,10,12}){
                var u=Unit(cache,"select-"+i+cls+level,cls,level);bool expected=cls!= "f7d7eb166b3dd594fb330d085df41853"&&level>=new[]{10,4,7}[i];
                Check(CanSelect(u,proofPool,root)==expected,"native CanSelect class/minimum "+i+"/"+cls+"/"+level);
                if(expected){var owned=Fact(u,root);Check(!CanSelect(u,proofPool,root),"native duplicate prerequisite excludes owned piece");u.Facts.Remove(owned);}
            }
            Register(cache);Check(ReferenceEquals(root,Asset<BlueprintFeature>(cache,Tokens[i])),"repeat preserves root object");
        }
        h.Patch(AccessTools.Method(Module,"AppendToPools"),postfix:new HarmonyMethod(typeof(BardMasterpieces01435Tests),nameof(AppendFault)));
        var old=new[]{Get(basic,"m_Features"),Get(basic,"m_AllFeatures"),Get(container,"m_Features"),Get(container,"m_AllFeatures")};appendFailure=true;
        bool rejected=false;try{Register(cache,"RegisterDance");}catch(TargetInvocationException e){rejected=e.InnerException is InvalidOperationException;}finally{appendFailure=false;}
        Check(rejected,"real append exception propagates within module");Check(old.SequenceEqual(new[]{Get(basic,"m_Features"),Get(basic,"m_AllFeatures"),Get(container,"m_Features"),Get(container,"m_AllFeatures")}),"actual late append restores exact old arrays");
        Check(definitions.SequenceEqual(donors.Select(b=>Definition(b.ComponentsArray))),"native resource and status graphs unchanged");
    }
    static ActivatableAbility Toggle(UnitEntityData u,string token,bool skald)=>u.ActivatableAbilities.RawFacts.First(a=>a.Blueprint.AssetGuid==Id(token+(skald?"SkaldToggle":"BardToggle")));
    static void Tick(UnitEntityData owner,ActivatableAbility ability){ticks++;AccessTools.Method(typeof(UnitActivatableAbilitiesController),"TickAbility").Invoke(new UnitActivatableAbilitiesController(),new object[]{owner,ability,false});}
    static void Start(UnitEntityData u,ActivatableAbility a){a.IsOn=true;foreach(var prior in u.ActivatableAbilities.RawFacts.Where(p=>p!=a&&p.IsStarted&&!p.IsOn).ToArray())Tick(u,prior);a.TryStart();Check(a.IsStarted&&a.AppliedBuff!=null,"native TryStart constructs actual applied buff "+a.Blueprint.name+" available="+a.IsAvailable+" resources="+a.IsAvailableByResources+" restrictions="+a.IsAvailableByRestrictions+" amount="+a.ResourceCount);Tick(u,a);}
    static void Rounds(BlueprintsCache cache,Harmony h){
        h.Patch(AccessTools.Method(typeof(UnitActivatableAbilitiesController),"GetTimeToNextRound"),prefix:new HarmonyMethod(typeof(BardMasterpieces01435Tests),nameof(NextRound)));
        h.Patch(AccessTools.Method(typeof(UnitActivatableAbilitiesController),"CanTickNewRound"),prefix:new HarmonyMethod(typeof(BardMasterpieces01435Tests),nameof(TickAllowed)));
        foreach(bool skald in new[]{false,true})foreach(int piece in Enumerable.Range(0,3)){
            var u=Unit(cache,"round-"+skald+piece,skald?Skald:Bard);var root=Asset<BlueprintFeature>(cache,Tokens[piece]);
            var noResource=Fact(u,root);Check(!u.ActivatableAbilities.RawFacts.Any(),"owning piece alone grants no resource or fake ability");u.Facts.Remove(noResource);
            Fact(u,Native<BlueprintFeature>(cache,skald?SkaldGrant:BardGrant));u.Resources.Restore(Native<BlueprintAbilityResource>(cache,skald?SkaldPool:BardPool));
            Fact(u,root);var ability=Toggle(u,Tokens[piece],skald);Check(u.ActivatableAbilities.RawFacts.Count==1,"only owned performance resource grants a leaf");
            var resource=Native<BlueprintAbilityResource>(cache,skald?SkaldPool:BardPool);int before=u.Resources.GetResourceAmount(resource);
            if(before<3)Console.WriteLine("035 resource diagnostic "+skald+"/"+piece+" max="+resource.GetMaxAmount(u.Descriptor)+" grant-ref="+ReferenceEquals(resource,Native<BlueprintFeature>(cache,skald?SkaldGrant:BardGrant).GetComponent<AddAbilityResources>().Resource)+" keys="+string.Join(",",((IDictionary)Get(u.Resources,"m_Resources")).Keys.Cast<BlueprintScriptableObject>().Select(b=>b.name+":"+b.AssetGuid+":"+u.Resources.GetResourceAmount(b))));
            Start(u,ability);
            Check(u.Resources.GetResourceAmount(resource)==before-(piece==0?3:1),"actual native start and first controller Tick pay expected cost "+piece);
            before=u.Resources.GetResourceAmount(resource);Tick(u,ability);Check(u.Resources.GetResourceAmount(resource)==before-1,"following native controller round costs one");
            var host=ability.AppliedBuff;ability.IsOn=false;Tick(u,ability);Check(!ability.IsStarted,"turning off native performance ends active state");
            Check(piece==1?host.IsDisposed:host.TimeLeft<=TimeSpan.FromSeconds(6),"native stop removes free dance or schedules standard-song last round");
            u.Resources.Spend(resource,u.Resources.GetResourceAmount(resource));Check(!ability.IsAvailable,"exhausted real resource makes leaf unavailable");
        }
    }
    static void ResourceBoundaries(BlueprintsCache cache){
        foreach(bool skald in new[]{false,true}){
            var u=Unit(cache,"exact-clamor-"+skald,skald?Skald:Bard);Fact(u,Native<BlueprintFeature>(cache,skald?SkaldGrant:BardGrant));Fact(u,Asset<BlueprintFeature>(cache,Tokens[0]));
            var resource=Native<BlueprintAbilityResource>(cache,skald?SkaldPool:BardPool);u.Resources.Restore(resource);u.Resources.Spend(resource,u.Resources.GetResourceAmount(resource)-2);var ability=Toggle(u,Tokens[0],skald);
            Check(!ability.IsAvailable,"exactly two resource rounds cannot start Clamor "+skald);ability.IsOn=true;ability.TryStart();Check(!ability.IsStarted&&u.Resources.GetResourceAmount(resource)==2,"rejected native TryStart neither applies host nor spends resource");ability.IsOn=false;
            u.Resources.Restore(resource,1);Check(u.Resources.GetResourceAmount(resource)==3&&ability.IsAvailable,"exactly three resource rounds permit Clamor "+skald);Start(u,ability);Check(u.Resources.GetResourceAmount(resource)==0,"exact first Clamor round consumes all three");Tick(u,ability);Check(!ability.IsStarted,"native next tick stops exhausted performance");
        }
        var mixed=Unit(cache,"mixed-performance");mixed.Progression.Classes.Add(new ClassData(Native<BlueprintCharacterClass>(cache,Skald)){Level=8});
        Fact(mixed,Native<BlueprintFeature>(cache,BardGrant));Fact(mixed,Native<BlueprintFeature>(cache,SkaldGrant));Fact(mixed,Asset<BlueprintFeature>(cache,Tokens[1]));
        var bard=Native<BlueprintAbilityResource>(cache,BardPool);var skaldPool=Native<BlueprintAbilityResource>(cache,SkaldPool);mixed.Resources.Restore(bard);mixed.Resources.Restore(skaldPool);
        var first=Toggle(mixed,Tokens[1],false);var second=Toggle(mixed,Tokens[1],true);Check(mixed.ActivatableAbilities.RawFacts.Count==2,"actual Bard/Skald multiclass owns both distinct resource leaves");
        int bardBefore=mixed.Resources.GetResourceAmount(bard),skaldBefore=mixed.Resources.GetResourceAmount(skaldPool);Start(mixed,first);Check(mixed.Resources.GetResourceAmount(bard)==bardBefore-1&&mixed.Resources.GetResourceAmount(skaldPool)==skaldBefore,"Bard leaf pays only its real Bard pool");
        var firstBuff=first.AppliedBuff;Start(mixed,second);Check(!first.IsOn&&second.IsStarted&&firstBuff.IsDisposed,"native performance group replaces first leaf when switching resource");Check(mixed.Resources.GetResourceAmount(skaldPool)==skaldBefore-1&&mixed.Resources.GetResourceAmount(bard)==bardBefore-1,"Skald leaf pays only its real Skald pool");
        var linger=Fact(mixed,Native<BlueprintFeature>(cache,"17239b298065efc459cffe2220ecb559"));var lingeringBuff=second.AppliedBuff;second.IsOn=false;Tick(mixed,second);
        Check(!second.IsStarted&&!lingeringBuff.IsDisposed&&lingeringBuff.TimeLeft==TimeSpan.FromSeconds(12),"actual native Stop retains bonus and penalties together for two lingering rounds");
        mixed.Facts.Remove(linger);mixed.Facts.Remove(lingeringBuff);Check(!mixed.State.HasCondition(UnitCondition.SpellCastingIsDifficult),"last lingering buff removal balances difficult casting");
        var arch=Native<BlueprintArchetype>(cache,"38384e0c1e99c2e42ac6ed70a04aca46");
        var archRow=PsychicExpansionTests.NativeRows.First(r=>(string)r["blueprint"]["AssetId"]==arch.AssetGuid.ToString());
        Check(!archRow["blueprint"]["Data"]["RemoveFeatures"].ToString().Contains(BardGrant),"real Archaeologist archetype does not remove native performance resource fact");
        var later=Unit(cache,"late-resource");var rootFact=Fact(later,Asset<BlueprintFeature>(cache,Tokens[1]));Check(!later.ActivatableAbilities.RawFacts.Any(),"selected piece without an existing pool has no free activation");Fact(later,Native<BlueprintFeature>(cache,BardGrant));
        EventBus.RaiseEvent<IOwnerGainLevelHandler>(later,h=>h.HandleUnitGainLevel());Check(later.ActivatableAbilities.RawFacts.Count==1,"actual native gain-level event grants newly available resource leaf once");
        EventBus.RaiseEvent<IOwnerGainLevelHandler>(later,h=>h.HandleUnitGainLevel());Check(later.ActivatableAbilities.RawFacts.Count==1,"repeated native gain-level callback does not duplicate leaf");
        later.Resources.Restore(bard);var active=Toggle(later,Tokens[1],false);Start(later,active);var actualHost=active.AppliedBuff;int paid=later.Resources.GetResourceAmount(bard);later.Facts.TurnOff();later.Facts.TurnOn();
        Check(later.Facts.List.Contains(rootFact)&&later.Facts.List.Contains(active)&&later.Facts.List.Contains(actualHost)&&later.Resources.GetResourceAmount(bard)==paid,"native manager save-style TurnOff/TurnOn preserves same grant and paid pool");
        later.Facts.Remove(rootFact);Check(!later.ActivatableAbilities.RawFacts.Contains(active),"native root removal revokes its granted performance leaf");
    }
    static AreaEffectEntityData Area(string id,MechanicsContext context,BlueprintAbilityAreaEffect blueprint,params UnitEntityData[] targets){
        var area=Empty<AreaEffectEntityData>();Set(area,"UniqueId",id);Set(area,"m_Context",context);Set(area,"m_Blueprint",blueprint);var field=AccessTools.Field(typeof(AreaEffectEntityData),"m_UnitsInside");var list=(IList)Activator.CreateInstance(field.FieldType);field.SetValue(area,list);
        foreach(var target in targets){var entry=Activator.CreateInstance(field.FieldType.GetGenericArguments()[0],true);Set(entry,"Reference",(UnitReference)target);list.Add(entry);}return area;
    }
    static void Symphony(BlueprintsCache cache){
        var caster=Unit(cache,"symphony-caster");var other=Unit(cache,"symphony-other",Skald);var target=Unit(cache,"symphony-ally");hostile=Unit(cache,"symphony-hostile");
        var bp=Asset<BlueprintAbilityAreaEffect>(cache,Tokens[2]+"Area");var owned=bp.GetComponent<AbilityAreaEffectBuff>();var host=Buff(caster,Asset<BlueprintBuff>(cache,Tokens[2]+"Host"));var host2=Buff(other,host.Blueprint);
        var area=Area("035-symphony-area",host.Context,bp,target);var area2=Area("035-symphony-area2",host2.Context,bp,target);
        owned.HandleUnitEnter(host.Context,area,target);Check(target.State.HasConditionImmunity(UnitCondition.Paralyzed)&&target.State.HasConditionImmunity(UnitCondition.Entangled),"real native area grants native Freedom of Movement immunities");
        owned.HandleUnitEnter(host.Context,area,hostile);Check(!hostile.State.HasConditionImmunity(UnitCondition.Paralyzed),"actual area ally condition rejects enemy");
        owned.HandleUnitEnter(host2.Context,area2,target);Check(target.Buffs.Enumerable.Count(b=>b.Blueprint.AssetGuid==Id(Tokens[2]+"Effect"))==2,"overlapping performances retain separate native area-owned buffs");
        var foreign=Buff(target,Native<BlueprintBuff>(cache,"1533e782fca42b84ea370fc1dcbf4fc1"));
        owned.HandleUnitExit(host.Context,area,target);Check(target.State.HasConditionImmunity(UnitCondition.Paralyzed)&&!foreign.IsDisposed,"first native area exit preserves other caster and real foreign FoM");
        owned.HandleUnitExit(host2.Context,area2,target);Check(target.State.HasConditionImmunity(UnitCondition.Paralyzed)&&target.Buffs.Enumerable.Count(b=>b.Blueprint.AssetGuid==Id(Tokens[2]+"Effect"))==0,"last own area exit leaves only foreign freedom");
        target.Facts.Remove(foreign);Check(!target.State.HasConditionImmunity(UnitCondition.Paralyzed),"removing foreign source balances last native condition counter");hostile=null;
        var silence=Buff(target,Native<BlueprintBuff>(cache,"e4b8a158e8aabfe41b5e0bd334d8c386"));owned.HandleUnitEnter(host.Context,area,target);Check(!target.State.HasConditionImmunity(UnitCondition.Paralyzed),"actual hearing condition rejects silenced target on native area enter");
        target.Facts.Remove(silence);owned.HandleRound(host.Context,area);Check(target.State.HasConditionImmunity(UnitCondition.Paralyzed),"native area round adds effect when target can hear again");
        var silence2=Buff(target,Native<BlueprintBuff>(cache,"e4b8a158e8aabfe41b5e0bd334d8c386"));owned.HandleRound(host.Context,area);Check(!target.State.HasConditionImmunity(UnitCondition.Paralyzed),"native area round removes only effect while target is silenced");target.Facts.Remove(silence2);
        owned.HandleRound(host.Context,area);var casterSilence=Buff(caster,Native<BlueprintBuff>(cache,"e4b8a158e8aabfe41b5e0bd334d8c386"));owned.HandleRound(host.Context,area);Check(!target.State.HasConditionImmunity(UnitCondition.Paralyzed),"native hearing recheck removes effect when caster is silenced");caster.Facts.Remove(casterSilence);owned.HandleUnitExit(host.Context,area,target);
    }
    static void Clamor(BlueprintsCache cache){
        var caster=Unit(cache,"clamor-caster");var areaBp=Asset<BlueprintAbilityAreaEffect>(cache,Tokens[0]+"Area");var owned=areaBp.GetComponent<AbilityAreaEffectBuff>();var host=Buff(caster,Asset<BlueprintBuff>(cache,Tokens[0]+"Host"));
        foreach(string type in new[]{"ordinary","undead","outsider"})foreach(bool success in new[]{false,true})foreach(bool evil in new[]{false,true}){
            var target=Unit(cache,"clamor-"+type+success+evil);target.Descriptor.Alignment.Set(evil?Alignment.ChaoticEvil:Alignment.TrueNeutral);
            if(type!="ordinary")Fact(target,Native<BlueprintFeature>(cache,type=="undead"?"734a29b693e9ec346ba2951b27987e33":"9054d3988d491d944ac144e27b6bc318"));
            roll=success?20:1;var area=Area("035-clamor-"+ ++serial,host.Context,areaBp,target);int before=saves;owned.HandleUnitEnter(host.Context,area,target);
            Check(saves==before+(evil?1:0),"native evil alignment gating precedes actual Will save "+type+success+evil);
            if(evil)Check(lastSave.DifficultyClassBase==20,"actual native Clamor context uses caster12/Charisma18 DC20; actual="+lastSave.DifficultyClassBase);
            var expected=type=="ordinary"?(success?UnitCondition.Shaken:UnitCondition.Blindness):(success?UnitCondition.Staggered:UnitCondition.Stunned);
            if(evil)Check(target.State.HasCondition(expected)||target.State.HasConditionImmunity(expected),"native actual status or retained native condition immunity "+type+success);
            int once=saves;owned.HandleRound(host.Context,area);Check(saves==once,"continuing area does not reroll the same owned entry");
            owned.HandleUnitExit(host.Context,area,target);Check(!target.State.HasCondition(expected),"native area exit removes exact own status child");
        }
        var immune=Unit(cache,"clamor-stun-immune");immune.Descriptor.Alignment.Set(Alignment.ChaoticEvil);Fact(immune,Native<BlueprintFeature>(cache,"9054d3988d491d944ac144e27b6bc318"));var protection=Buff(immune,Custom("stun-immunity",new AddConditionImmunity{Condition=UnitCondition.Stunned}));
        roll=1;var immuneArea=Area("035-clamor-immune",host.Context,areaBp,immune);owned.HandleUnitEnter(host.Context,immuneArea,immune);Check(!immune.State.HasCondition(UnitCondition.Stunned)&&immune.State.HasConditionImmunity(UnitCondition.Stunned),"private CotW stun exception preserves actual native condition immunity");owned.HandleUnitExit(host.Context,immuneArea,immune);immune.Facts.Remove(protection);
        var specific=new SpecificBuffImmunity();Set(specific,"m_Buff",Ref<BlueprintBuffReference>(Asset<BlueprintBuff>(cache,Tokens[0]+"Stunned")));var specificFact=Buff(immune,Custom("specific-stun-immunity",specific));owned.HandleUnitEnter(host.Context,immuneArea,immune);Check(!immune.Buffs.Enumerable.Any(b=>b.Blueprint.AssetGuid==Id(Tokens[0]+"Stunned")),"native specific-effect immunity rejects authored private stun");owned.HandleUnitExit(host.Context,immuneArea,immune);immune.Facts.Remove(specificFact);
        var silent=Unit(cache,"clamor-silent");silent.Descriptor.Alignment.Set(Alignment.ChaoticEvil);Buff(silent,Native<BlueprintBuff>(cache,"e4b8a158e8aabfe41b5e0bd334d8c386"));var silentArea=Area("035-clamor-silent",host.Context,areaBp,silent);int silentBefore=saves;owned.HandleUnitEnter(host.Context,silentArea,silent);Check(saves==silentBefore,"silenced evil target never reaches Will saving throw");
        var other=Unit(cache,"clamor-other",Skald);var secondHost=Buff(other,host.Blueprint);var shared=Unit(cache,"clamor-overlap");shared.Descriptor.Alignment.Set(Alignment.ChaoticEvil);var one=Area("035-clamor-one",host.Context,areaBp,shared);var two=Area("035-clamor-two",secondHost.Context,areaBp,shared);roll=1;
        owned.HandleUnitEnter(host.Context,one,shared);owned.HandleUnitEnter(secondHost.Context,two,shared);Check(shared.Buffs.Enumerable.Count(b=>b.Blueprint.AssetGuid==Id(Tokens[0]+"Blind"))==2,"two casters own distinct private blindness children: "+string.Join(";",shared.Buffs.Enumerable.Select(b=>b.Blueprint.name+"/"+b.Context.MaybeCaster?.UniqueId))+" success="+lastSave?.Success);var foreign=Buff(shared,Native<BlueprintBuff>(cache,"0ec36e7596a4928489d2049e1e1c76a7"));owned.HandleUnitExit(host.Context,one,shared);Check(shared.State.HasCondition(UnitCondition.Blindness)&&!foreign.IsDisposed,"first Clamor exit preserves other source and foreign blindness");owned.HandleUnitExit(secondHost.Context,two,shared);Check(shared.State.HasCondition(UnitCondition.Blindness)&&!shared.Buffs.Enumerable.Any(b=>b.Blueprint.AssetGuid==Id(Tokens[0]+"Blind")),"last own Clamor exit retains independent native blindness");shared.Facts.Remove(foreign);Check(!shared.State.HasCondition(UnitCondition.Blindness),"all actual blindness counters balance after final foreign removal");
    }
    static void StoryMode(BlueprintsCache cache){
        var source=Unit(cache,"story-performance-source");var second=Unit(cache,"story-performance-second",Skald);var victim=Unit(cache,"story-performance-player");victim.Descriptor.Alignment.Set(Alignment.ChaoticEvil);
        Check(victim.IsPlayerFaction,"native lowest-difficulty regression targets actual player faction");var bp=Asset<BlueprintAbilityAreaEffect>(cache,Tokens[0]+"Area");var owned=bp.GetComponent<AbilityAreaEffectBuff>();
        var firstHost=Buff(source,Asset<BlueprintBuff>(cache,Tokens[0]+"Host"));var secondHost=Buff(second,firstHost.Blueprint);var firstArea=Area("035-story-first",firstHost.Context,bp,victim);var secondArea=Area("035-story-second",secondHost.Context,bp,victim);var blind=Asset<BlueprintBuff>(cache,Tokens[0]+"Blind");
        try{
            sceneDifficulty=Kingmaker.Settings.StatsAdjustmentsType.ExtraDecline;roll=1;owned.HandleUnitEnter(firstHost.Context,firstArea,victim);var first=victim.Buffs.Enumerable.First(b=>b.Blueprint==blind);
            Check(first.TimeLeft==TimeSpan.FromSeconds(18),"actual native lowest-difficulty RuleCanApplyBuff limits negative duration to three rounds");owned.HandleUnitEnter(secondHost.Context,secondArea,victim);
            Check(lastApplyRule.StoryModeImmunity&&!lastApplyRule.CanApply&&victim.Buffs.Enumerable.Count(b=>b.Blueprint==blind)==1,"actual native story-mode repeated-descriptor immunity rejects second Clamor status without changing production rule: last="+lastApplyRule?.Blueprint.name+" immune="+lastApplyRule?.StoryModeImmunity+" can="+lastApplyRule?.CanApply+" count="+victim.Buffs.Enumerable.Count(b=>b.Blueprint==blind)+" combat="+victim.IsInCombat+" ledger="+victim.CombatState.StoryModeBuffImmunity);
            owned.HandleUnitExit(firstHost.Context,firstArea,victim);owned.HandleUnitExit(secondHost.Context,secondArea,victim);Check(!victim.State.HasCondition(UnitCondition.Blindness),"story-mode source exit still balances native condition counter");
        }finally{sceneDifficulty=Kingmaker.Settings.StatsAdjustmentsType.None;}
    }
    static void SpecificImmunity(BlueprintsCache cache){
        var areaBp=Asset<BlueprintAbilityAreaEffect>(cache,Tokens[0]+"Area");var owned=areaBp.GetComponent<AbilityAreaEffectBuff>();
        foreach(int status in new[]{1,2,3}){
            string nativeId=status==1?"df3950af5a783bd4d91ab73eb8fa0fd3":status==2?"0ec36e7596a4928489d2049e1e1c76a7":"25ec6cb6ab1845c48a95f9c20b034220";
            string label=status==1?"Staggered":status==2?"Blind":"Shaken";
            var caster=Unit(cache,"native-immunity-caster"+status);var target=Unit(cache,"native-immunity-target"+status);target.Descriptor.Alignment.Set(Alignment.ChaoticEvil);
            if(status==1)Fact(target,Native<BlueprintFeature>(cache,"9054d3988d491d944ac144e27b6bc318"));
            var host=Buff(caster,Asset<BlueprintBuff>(cache,Tokens[0]+"Host"));var area=Area("035-immunity"+status,host.Context,areaBp,target);
            var original=Native<BlueprintBuff>(cache,nativeId);var clone=Asset<BlueprintBuff>(cache,Tokens[0]+label);
            var immunity=new SpecificBuffImmunity();Set(immunity,"m_Buff",Ref<BlueprintBuffReference>(original));var guard=Buff(target,Custom("original-"+label+"-immunity",immunity));
            Check(!ContextRule(host.Context,new RuleCanApplyBuff(target,original,host.Context,null)).CanApply,"actual native original GUID specific immunity matches "+label);
            Check(ContextRule(host.Context,new RuleCanApplyBuff(target,clone,host.Context,null)).CanApply,"native GUID-only handler does not recognize private clone; regression reproducer "+label);
            roll=status==2?1:20;int before=saves;owned.HandleUnitEnter(host.Context,area,target);
            Check(saves==before+1&&!target.Buffs.Enumerable.Any(b=>b.Blueprint==clone),"real area/save plus local native immunity gate rejects private child "+label);owned.HandleUnitExit(host.Context,area,target);
            guard.TurnOff();owned.HandleUnitEnter(host.Context,area,target);Check(target.Buffs.Enumerable.Any(b=>b.Blueprint==clone),"turned-off native specific immunity no longer rejects private child "+label);owned.HandleUnitExit(host.Context,area,target);guard.TurnOn();
            guard.IsSuppressed=true;owned.HandleUnitEnter(host.Context,area,target);Check(target.Buffs.Enumerable.Any(b=>b.Blueprint==clone),"suppressed native buff immunity is not treated as active "+label);owned.HandleUnitExit(host.Context,area,target);guard.IsSuppressed=false;
        }
        var source=Unit(cache,"conditional-immunity-source");source.Descriptor.Alignment.Set(Alignment.ChaoticEvil);var victim=Unit(cache,"conditional-immunity-victim");victim.Descriptor.Alignment.Set(Alignment.ChaoticEvil);
        var sourceHost=Buff(source,Asset<BlueprintBuff>(cache,Tokens[0]+"Host"));var caseArea=Area("035-conditional-immunity",sourceHost.Context,areaBp,victim);var blind=Asset<BlueprintBuff>(cache,Tokens[0]+"Blind");
        var aligned=new SpecificBuffImmunity{Alignment=AlignmentComponent.Good};Set(aligned,"m_Buff",Ref<BlueprintBuffReference>(Native<BlueprintBuff>(cache,"0ec36e7596a4928489d2049e1e1c76a7")));
        var guardBp=new BlueprintFeature{name="035-conditional-native-immunity",AssetGuid=BlueprintGuid.Parse(Guid.NewGuid().ToString("N")),ComponentsArray=new BlueprintComponent[]{aligned}};guardBp.OnEnable();var featureGuard=Fact(victim,guardBp);
        void Enter(bool expected,string reason){roll=1;owned.HandleUnitEnter(sourceHost.Context,caseArea,victim);Check(victim.Buffs.Enumerable.Any(b=>b.Blueprint==blind)==expected,reason);owned.HandleUnitExit(sourceHost.Context,caseArea,victim);}
        Enter(true,"native feature immunity retains nonmatching Good-caster alignment condition");aligned.Alignment=AlignmentComponent.Evil;Enter(false,"actual attached native feature handler rejects matching Evil-caster alignment");
        Set(aligned,"m_CheckCasterFact",true);Set(aligned,"m_FactToCheck",Ref<BlueprintUnitFactReference>(Native<BlueprintFeature>(cache,BardGrant)));Enter(true,"native immunity preserves absent caster-fact exception");var grant=Fact(source,Native<BlueprintFeature>(cache,BardGrant));Enter(false,"same native immunity begins rejecting once caster has checked fact");source.Facts.Remove(grant);Enter(true,"removing checked caster fact restores allowed application");
        Set(aligned,"m_Buff",Ref<BlueprintBuffReference>(Native<BlueprintBuff>(cache,"25ec6cb6ab1845c48a95f9c20b034220")));Set(aligned,"m_CheckCasterFact",false);Enter(true,"immunity to unrelated native Shaken does not block failed-save Blind");victim.Facts.Remove(featureGuard);
        Set(aligned,"m_Buff",Ref<BlueprintBuffReference>(Native<BlueprintBuff>(cache,"0ec36e7596a4928489d2049e1e1c76a7")));
        var nativeUnit=new BlueprintUnit{name="035-native-unit-immunity-host",AssetGuid=BlueprintGuid.Parse(Guid.NewGuid().ToString("N")),ComponentsArray=new BlueprintComponent[]{aligned}};nativeUnit.OnEnable();var main=new MainUnitFact(nativeUnit);victim.Facts.Add(main);
        Check(main.IsActive&&main.IsTurnedOn&&victim.Facts.List.Contains(main),"actual native MainUnitFact registers BlueprintUnit immunity in target fact list");Enter(false,"native BlueprintUnit/MainUnitFact immunity host remains effective");main.TurnOff();Enter(true,"turned-off MainUnitFact immunity is inactive");victim.Facts.Remove(main);
    }
    static void Dance(BlueprintsCache cache){
        foreach(int level in new[]{4,7,8,11,12,15,16,19,20}){
            var u=Unit(cache,"dance-rank"+level,Bard,level);int baseline=u.Stats.AC.ModifiedValue,baseCmb=u.Stats.AdditionalCMB.ModifiedValue;
            var weapons=new[]{Weapon(u,false),Weapon(u,true)};var oldAttack=weapons.Select(w=>Attack(u,w)).ToArray();
            var buff=Buff(u,Asset<BlueprintBuff>(cache,Tokens[1]+"Effect"));int bonus=Math.Max(2,1+level/4);
            Check(u.Stats.AC.ModifiedValue==baseline+bonus&&u.Stats.AdditionalCMB.ModifiedValue==baseCmb-bonus,"native AC/CMB scaling "+level);
            int meleeNow=Attack(u,weapons[0]),rangedNow=Attack(u,weapons[1]);Check(meleeNow==oldAttack[0]-bonus&&rangedNow==oldAttack[1],"native attack subscription/final calculation penalizes melee only "+level+" base="+string.Join(",",oldAttack)+" actual="+meleeNow+","+rangedNow);
            Check(u.State.HasCondition(UnitCondition.SpellCastingIsDifficult)&&!u.State.HasCondition(UnitCondition.SpellCastingIsVeryDifficult),"actual native difficult casting uses condition26, never49");
            buff.TurnOff();Check(u.Stats.AC.ModifiedValue==baseline&&!u.State.HasCondition(UnitCondition.SpellCastingIsDifficult),"native dance TurnOff reverses its modifiers");buff.TurnOn();u.Facts.Remove(buff);Check(u.Stats.AC.ModifiedValue==baseline,"native dance final removal restores AC");
        }
    }
    static ItemEntityWeapon Weapon(UnitEntityData u,bool ranged){var weapon=(ItemEntityWeapon)AccessTools.Method(typeof(Linnorm01416Tests),"Weapon").Invoke(null,new object[]{u,ranged?WeaponCategory.Longbow:WeaponCategory.Longsword,"035-weapon-"+(++serial)});Set(weapon.Blueprint.Type,"m_AttackType",ranged?AttackType.Ranged:AttackType.Melee);weapon.Blueprint.Type.OnEnable();return weapon;}
    static int Attack(UnitEntityData u,ItemEntityWeapon weapon){
        if(weapon.Blueprint.IsRanged)nativeRangedRules++;else nativeMeleeRules++;
        // Weapon damage construction is outside this attack-bonus test. Feed a
        // completed native stats object, then execute native subscriptions and
        // the entire native attack-bonus arithmetic without replacing handlers.
        var stats=new RuleCalculateWeaponStats(u,weapon){IsSecondaryOverride=false};Set(stats,"m_Triggered",true);
        return NativeRule(new RuleCalculateAttackBonusWithoutTarget(u,stats,0)).Result;
    }
    static void Concentration(BlueprintsCache cache,Harmony h){
        var method=AccessTools.Method(typeof(UnitUseAbility),"MakeConcentrationCheckIfCastingIsDifficult");
        h.Patch(method,transpiler:new HarmonyMethod(typeof(BardMasterpieces01435Tests),nameof(RuleBoundary)));
        h.Patch(AccessTools.Method(typeof(RuleCheckConcentration),"OnTrigger"),transpiler:new HarmonyMethod(typeof(BardMasterpieces01435Tests),nameof(RuleBoundary)));
        var spell=new BlueprintAbility{name="035-native-concentration-input",AssetGuid=BlueprintGuid.Parse(Guid.NewGuid().ToString("N")),Type=AbilityType.Spell,ActionType=UnitCommand.CommandType.Standard,ComponentsArray=Array.Empty<BlueprintComponent>()};spell.OnEnable();
        var dancer=Unit(cache,"concentration-dancer");var other=Unit(cache,"concentration-other");
        UnitUseAbility Cast(UnitEntityData u,int spellLevel,int die){
            var data=new AbilityData(spell,u.Descriptor){OverrideCasterLevel=10,OverrideSpellLevel=spellLevel,OverrideCasterModifier=0};
            var command=new UnitUseAbility(data,new TargetWrapper(u),UnitCommand.CommandType.Standard);Set(command,"Executor",u);roll=die;lastConcentration=null;method.Invoke(command,null);return command;
        }
        var ordinary=Cast(dancer,2,1);Check(lastConcentration==null&&!ordinary.ConcentrationCheckFailed,"actual native cast without difficult condition needs no concentration");
        var dance=Buff(dancer,Asset<BlueprintBuff>(cache,Tokens[1]+"Effect"));
        foreach(int level in new[]{1,2,4})foreach(int die in new[]{1,20}){
            var command=Cast(dancer,level,die);Check(lastConcentration!=null&&lastConcentration.DC==15+level,"real UnitUseAbility condition26 produces DC15+spell level");
            Check(command.ConcentrationCheckFailed==(die==1),"real native concentration success/failure controls command flag");
        }
        var difficult=Buff(dancer,Custom("very-difficult",new AddCondition{Condition=UnitCondition.SpellCastingIsVeryDifficult}));
        Cast(dancer,4,20);Check(lastConcentration.DC==23&&lastConcentration.AddTwiceSpellLevel,"external VeryDifficult source retains DC15+2spell level while dancing");
        difficult.TurnOff();Cast(dancer,4,20);Check(lastConcentration.DC==19&&!lastConcentration.AddTwiceSpellLevel,"removing external very-difficult source restores only native dance condition");
        dance.TurnOff();Cast(dancer,4,1);Check(lastConcentration==null,"native dance TurnOff ceases difficult-casting check");
        var foreign=Buff(other,Custom("other-difficult",new AddCondition{Condition=UnitCondition.SpellCastingIsVeryDifficult}));Cast(other,3,20);Check(lastConcentration.DC==21,"other character's very difficult casting unaffected");
        var source=new AbilityData(spell,dancer.Descriptor){OverrideCasterLevel=10,OverrideSpellLevel=2,OverrideCasterModifier=0};
        var damage=Empty<Kingmaker.RuleSystem.Rules.Damage.RuleDealDamage>();Set(damage,"Result",10);
        var damaged=NativeRule(new RuleCheckConcentration(dancer,source,damage));Check(damaged.DC==17,"native damage concentration DC10+SL+damage/2 unchanged");
        var custom=NativeRule(new RuleCheckConcentration(dancer,source){CustomDC=47});Check(custom.DC==47,"native unrelated custom concentration DC unchanged");
        dancer.Facts.Remove(dance);other.Facts.Remove(foreign);
    }
    internal static int Run(BlueprintsCache cache){
        checks=serial=saves=applications=fxRequests=resourceSpends=ticks=nativeConcentrations=nativeMeleeRules=nativeRangedRules=0;units.Clear();hostile=null;current=null;
        var h=new Harmony("CotW01435.BardMasterpieceNativeContracts");var game=Game.Instance;var priorState=game.State;var priorTime=game.TimeController;
        var state=Empty<PersistentState>();var player=Empty<Player>();player.GameTime=TimeSpan.FromHours(1);Set(player,"m_Party",new List<UnitEntityData>());Set(player,"m_CharacterListsValid",true);state.PlayerState=player;Set(game,"State",state);Set(game,"TimeController",Empty<TimeController>());
        var buffHelper=AccessTools.Method(typeof(UnitHelper),"AddBuff",new[]{typeof(UnitDescriptor),typeof(BlueprintBuff),typeof(MechanicsContext),typeof(TimeSpan?)});var capture=AccessTools.Method(typeof(Psychic070Tests),"CaptureDefense");var dice=AccessTools.Method(typeof(Psychic070Tests),"ControlledDice");
        var endpoints=new[]{new{Method=(MethodBase)buffHelper,Prefix=capture},new{Method=(MethodBase)AccessTools.Method(typeof(RulebookEvent.Dice),"D",new[]{typeof(DiceFormula)}),Prefix=dice}};
        var suspended=endpoints.SelectMany(e=>(Harmony.GetPatchInfo(e.Method)?.Prefixes.AsEnumerable()??Enumerable.Empty<HarmonyLib.Patch>()).Where(p=>p.PatchMethod==e.Prefix).Select(p=>new{Original=e.Method,Record=p})).ToArray();
        var outerQueue=(ICollection)AccessTools.Field(typeof(Psychic070Tests),"dice").GetValue(null);var outerValues=outerQueue.Cast<int>().ToArray();JObject contract=null;
        void Patch(MethodBase method,string name)=>h.Patch(method,prefix:new HarmonyMethod(typeof(BardMasterpieces01435Tests),name));
        Exception primaryFailure=null;try{
            foreach(var p in suspended)h.Unpatch(p.Original,p.Record.PatchMethod);
            Patch(AccessTools.Method(typeof(FeatureCollection),"CheckStatPrerequisitesAndUpdate"),nameof(Skip));Patch(AccessTools.PropertyGetter(typeof(IgnorePrerequisites),"Ignore"),nameof(False));Patch(AccessTools.Method(typeof(BlueprintFeatureSelection),"IsIgnorePrerequisites"),nameof(Ignore));Patch(AccessTools.PropertyGetter(typeof(Game),"LevelUpController"),nameof(Controller));Provider01419Tests.PatchLabels(h);
            Patch(AccessTools.PropertyGetter(typeof(TimeController),"GameTime"),nameof(Time));Patch(AccessTools.PropertyGetter(typeof(TimeController),"RealTime"),nameof(Time));Patch(AccessTools.PropertyGetter(typeof(Game),"CurrentMode"),nameof(Mode));Patch(AccessTools.Method(typeof(EntityFact),"Attach"),nameof(Identity));Patch(AccessTools.Method(typeof(UnitState),"UpdateStatusEffect"),nameof(Skip));Patch(AccessTools.Method(typeof(UnitEntityData),"UpdateGroup"),nameof(Skip));
            Patch(AccessTools.Method(typeof(TurnBased.Controllers.CombatController),"IsInTurnBasedCombat"),nameof(False));Patch(AccessTools.PropertyGetter(typeof(Kingmaker.Armies.TacticalCombat.TacticalCombatHelper),"IsActive"),nameof(False));Patch(AccessTools.PropertyGetter(typeof(UnitEntityData),"IsPlayersEnemy"),nameof(False));Patch(AccessTools.Method(typeof(UnitEntityData),"IsAlly",new[]{typeof(UnitEntityData)}),nameof(Ally));Patch(AccessTools.Method(typeof(AddAreaEffect),"IsOwnerAvatarValid"),nameof(False));Patch(AccessTools.Method(typeof(UnitCommands),"Run",new[]{typeof(UnitCommand)}),nameof(Submit));Patch(AccessTools.Method(typeof(UnitEntityData),"IsCurrentUnit"),nameof(CurrentUnit));
            Patch(AccessTools.Method(typeof(Player),"IsTurnBasedModeOn"),nameof(False));
            Patch(AccessTools.Method(typeof(ContextActionSpawnFx),"RunAction"),nameof(Fx));Patch(AccessTools.Method(typeof(Kingmaker.QA.LogChannelEx),"ExceptionWithReport",new[]{typeof(Owlcat.Runtime.Core.Logging.LogChannel),typeof(Exception),typeof(string),typeof(object[])}),nameof(Expose));
            Patch(AccessTools.PropertyGetter(typeof(Rulebook),"CurrentContext"),nameof(Context));Patch(AccessTools.Method(typeof(RulebookEvent.Dice),"D",new[]{typeof(DiceFormula)}),nameof(Dice));Patch(AccessTools.PropertyGetter(typeof(RulebookEvent.Dice),"D20"),nameof(D20));
            h.Patch(AccessTools.Method(typeof(RuleCanApplyBuff),"OnTrigger"),postfix:new HarmonyMethod(typeof(BardMasterpieces01435Tests),nameof(BuffRuleObserved)));
            h.Patch(AccessTools.Method(typeof(UnitAbilityResourceCollection),"Spend"),postfix:new HarmonyMethod(typeof(BardMasterpieces01435Tests),nameof(Spent)));h.Patch(AccessTools.Method(typeof(ContextActionApplyBuff),"RunAction"),postfix:new HarmonyMethod(typeof(BardMasterpieces01435Tests),nameof(Applied)));
            foreach(var method in new[]{AccessTools.Method(typeof(ContextActionSavingThrow),"RunAction"),AccessTools.Method(typeof(RuleSavingThrow),"OnTrigger"),AccessTools.Method(typeof(RuleSavingThrow),"RollD20"),AccessTools.Method(typeof(ContextActionApplyBuff),"RunAction"),AccessTools.Method(typeof(AbilityExecutionProcess),"PrepareCast"),AccessTools.Method(typeof(ContextCalculateAbilityParams),"Calculate",new[]{typeof(MechanicsContext),typeof(BlueprintScriptableObject),typeof(UnitEntityData),typeof(AbilityData)})})h.Patch(method,transpiler:new HarmonyMethod(typeof(BardMasterpieces01435Tests),nameof(RuleBoundary)));
            h.Patch(AccessTools.Method(typeof(RuleCalculateAbilityParams),"OnTrigger"),transpiler:new HarmonyMethod(typeof(Migration01426Tests),"DifficultyBoundary"));h.Patch(AccessTools.Method(typeof(RuleCanApplyBuff),"OnTrigger"),transpiler:new HarmonyMethod(typeof(BardMasterpieces01435Tests),nameof(DifficultyBoundary)));
            Registration(cache,h);Rounds(cache,h);ResourceBoundaries(cache);Dance(cache);Concentration(cache,h);Symphony(cache);Clamor(cache);SpecificImmunity(cache);StoryMode(cache);Access01436(cache,h);
            var own=(string[])AccessTools.Field(Module,"OwnTokens").GetValue(null);var assets=own.Take(23).Select(t=>cache.Load(Id(t))).ToArray();Check(assets.All(a=>a!=null),"all declared owned assets published");
            Check(nativeConcentrations>0&&nativeMeleeRules>0&&nativeRangedRules>0&&saves>0&&ticks>0,"all claimed native execution counters are positive");
            PsychicExpansionTests.Inventory["bardMasterpieces01435Contracts"]=contract=new JObject{["passed"]=true,["assertions"]=checks,["historicalRegression"]=true,["currentRepairAdditionalItemCount"]=0,["additionalItemCount"]=3,["ordinaryFeatCount"]=0,["masterpieceCount"]=3,["newSpellRootCount"]=0,["itemIds"]=new JArray("COTW-0378","COTW-0379","COTW-0380"),["newBlueprints"]=JArray.FromObject(assets.Select(a=>new{id=a.AssetGuid.ToString(),name=a.name,type=a.GetType().FullName})),["nativeSavingThrows"]=saves,["nativeBuffApplications"]=applications,["nativeResourceUnitsSpent"]=resourceSpends,["nativeControllerTicks"]=ticks,["nativeFxRequests"]=fxRequests,["nativeConcentrations"]=nativeConcentrations,["nativeMeleeRules"]=nativeMeleeRules,["nativeRangedRules"]=nativeRangedRules,
                ["nativeSelectionAndPrerequisites"]=true,["nativePerformanceResourceAndControllerLifecycle"]=true,["nativeDanceScalingAndDifficultCasting"]=true,["nativeFreedomAreaOverlapOwnership"]=true,["nativeClamorAlignmentSaveAndConditionPaths"]=true,["nativeOriginalGuidSpecificImmunityHandlers"]=true,["nativeLowestDifficultyRepeatedDescriptorImmunity"]=true,
                ["boundary"]="Actual native selection, feature/buff manager, performance resource components, TryStart, controller TickAbility, area HandleUnitEnter/HandleRound/HandleUnitExit, save and native buff condition paths execute. Constructed unit/world/area-membership services, fixed dice and clock, native resource-round clock, absent views/area allocation and final command scheduler are controlled scene endpoints. Rendered FX, full game UI, spatial membership updates and full save/reload are tester cases."};
            Console.WriteLine("PASS: "+checks+" BardMasterpieces01435 checks.");return checks;
        }catch(Exception e){primaryFailure=e;Console.Error.WriteLine("035 PRIMARY FAILURE: "+e);throw;}finally{
            foreach(var u in units.AsEnumerable().Reverse())foreach(var fact in u.Facts.List.ToArray())if(fact.Manager!=null)try{u.Facts.Remove(fact);}catch(Exception e){if(primaryFailure==null)throw;Console.Error.WriteLine("035 cleanup after primary: "+e.Message);}
            h.UnpatchAll(h.Id);foreach(var saved in suspended){var p=saved.Record;new Harmony(p.owner).Patch(saved.Original,prefix:new HarmonyMethod(p.PatchMethod){priority=p.priority,before=p.before,after=p.after,debug=p.debug});}
            Set(game,"State",priorState);Set(game,"TimeController",priorTime);current=null;hostile=null;units.Clear();
            if(!outerQueue.Cast<int>().SequenceEqual(outerValues))throw new Exception("035 masterpiece altered outer fixed-dice queue");
            foreach(var saved in suspended){var p=saved.Record;if(!(Harmony.GetPatchInfo(saved.Original)?.Prefixes.Any(now=>now.PatchMethod==p.PatchMethod&&now.owner==p.owner&&now.priority==p.priority&&now.debug==p.debug&&(now.before??Array.Empty<string>()).SequenceEqual(p.before??Array.Empty<string>())&&(now.after??Array.Empty<string>()).SequenceEqual(p.after??Array.Empty<string>()))??false))throw new Exception("035 failed to restore exact outer prefix metadata");}
            if(Harmony.GetAllPatchedMethods().Any(m=>{var p=Harmony.GetPatchInfo(m);return p!=null&&p.Prefixes.Concat(p.Postfixes).Concat(p.Transpilers).Concat(p.Finalizers).Any(x=>x.owner==h.Id);}))throw new Exception("036 failed to remove own native/display test patches");
            if(contract!=null)contract["outerFixtureInputsAndPatchesRestored"]=true;if(PsychicExpansionTests.Inventory["bardMasterpieceAccess01436Contracts"] is JObject access)access["outerFixtureInputsAndPatchesRestored"]=true;
        }
    }
}

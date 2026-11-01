using System;
using System.Linq;
using System.Collections.Generic;
using System.Runtime.Serialization;
using HarmonyLib;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.Blueprints.Classes.Prerequisites;
using Kingmaker.Blueprints.Classes.Selection;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.EntitySystem;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.RuleSystem;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.RuleSystem.Rules.Abilities;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components;
using Kingmaker.UnitLogic.Abilities.Components.Base;
using Kingmaker.UnitLogic.Buffs;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Commands.Base;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Mechanics.Actions;
using Kingmaker.Utility;
using CallOfTheWild.Wrath.Prototype;
internal static class SpellResourceFeatTests {
 static int n,diceRolls;static BlueprintsCache cache;static readonly List<Buff> removed=new List<Buff>();
 static readonly Type Module=typeof(RageCastingBoost).Assembly.GetType("CallOfTheWild.Wrath.Prototype.SpellResourceFeats");
 static object Get(object o,string f)=>(AccessTools.Field(o.GetType(),f)??AccessTools.Field(o.GetType(),"<"+f+">k__BackingField")).GetValue(o);
 static void Set(object o,string f,object v)=>(AccessTools.Field(o.GetType(),f)??AccessTools.Field(o.GetType(),"<"+f+">k__BackingField")).SetValue(o,v);
 static T Empty<T>()where T:class=>(T)FormatterServices.GetUninitializedObject(typeof(T));
 static void Check(bool v,string s){if(!v)throw new Exception("Spell feats: "+s);n++;}
 static BlueprintGuid Id(string t)=>(BlueprintGuid)AccessTools.Method(Module.Assembly.GetType("CallOfTheWild.Wrath.Prototype.PsychicAssets"),"Id").Invoke(null,new object[]{t});
 static T Asset<T>(string t)where T:SimpleBlueprint=>(T)cache.Load(Id(t));
 static UnitEntityData Unit(string name) {var u=(UnitEntityData)AccessTools.Method(typeof(T11TrainingTests),"Unit").Invoke(null,new object[]{cache,name});var abilities=new AbilityCollection();abilities.Initialize(u.Facts);Set(u,"Abilities",abilities);u.Stats.Charisma.BaseValue=30;return u;}
 static Feature Feature(UnitEntityData u,BlueprintFeature b)=>(Feature)AccessTools.Method(typeof(T11TrainingTests),"Fact").Invoke(null,new object[]{u,b});
 static EntityFactComponent Runtime(UnitFact f,Type t)=>f.Components.Single(c=>c.SourceBlueprintComponent.GetType()==t);
 static void Call(UnitFact f,Type t,string method,object evt=null){var r=Runtime(f,t);using(r.RequestEventContext())AccessTools.Method(t,method,evt==null?Type.EmptyTypes:new[]{evt.GetType()}).Invoke(r.SourceBlueprintComponent,evt==null?null:new[]{evt});}
 static Ability Learn(UnitEntityData u,BlueprintAbility bp) {var a=Empty<Ability>();Set(a,"Components",new List<EntityFactComponent>());a.RestoreOwnerLink(u.Facts);AccessTools.Method(typeof(EntityFact),"Setup").Invoke(a,new object[]{bp});Set(a,"IsActive",true);Set(a,"IsTurnedOn",true);Set(a,"Data",new AbilityData(a));u.Facts.List.Add(a);var c=Get(u.Facts,"m_Cache");AccessTools.Method(c.GetType(),"Clear").Invoke(c,null);return a;}
 static Spellbook Book(UnitEntityData u,bool spontaneous,BlueprintCharacterClass cls) {
  var bp=new BlueprintSpellbook{Spontaneous=spontaneous,CastingAttribute=StatType.Charisma};var reference=BlueprintReferenceBase.CreateTyped<BlueprintCharacterClassReference>(cls);AccessTools.Property(typeof(BlueprintReferenceBase),"Cached").SetValue(reference,cls);Set(bp,"m_CharacterClass",reference);
  var b=Empty<Spellbook>();Set(b,"Blueprint",bp);Set(b,"Owner",u.Descriptor);Set(b,"m_MaxSpellLevel",(int?)10);Set(b,"m_SpontaneousSlots",Enumerable.Repeat(3,11).ToArray());Set(b,"m_MemorizedSpells",Enumerable.Range(0,11).Select(i=>new List<SpellSlot>()).ToArray());Set(b,"m_KnownSpells",Enumerable.Range(0,11).Select(i=>new List<AbilityData>()).ToArray());Set(b,"m_KnownSpellLevels",new Dictionary<BlueprintAbility,List<int>>());Set(b,"m_SpecialLists",new List<BlueprintSpellList>());Set(b,"m_SpellConversionLists",new Dictionary<string,BlueprintAbility[]>());return b;
 }
 static AbilityData Spell(UnitEntityData u,Spellbook b,int level,BlueprintAbility blueprint=null) {
  var bp=blueprint??new BlueprintAbility{Type=AbilityType.Spell,ComponentsArray=Array.Empty<BlueprintComponent>(),name="0144Slot"+level};var a=new AbilityData(bp,u.Descriptor){OverrideSpellLevel=level};Set(a,"m_CachedSpellbook",b);Set(a,"m_SpellbookBlueprint",b.Blueprint);((Dictionary<BlueprintAbility,List<int>>)Get(b,"m_KnownSpellLevels"))[bp]=new List<int>{level};
  if(!b.Blueprint.Spontaneous){var slot=new SpellSlot(level,SpellSlotType.Common,0,b);Set(slot,"Spell",a);slot.Available=true;((List<SpellSlot>[])Get(b,"m_MemorizedSpells"))[level].Add(slot);}return a;
 }
 static AbilityData Conversion(UnitEntityData u,AbilityData source){var a=new AbilityData(Asset<BlueprintAbility>("AmplifiedHexConversion"),u.Descriptor){ConvertedFrom=source};Set(a,"ParamSpellLevel",(int?)source.SpellLevel);Set(a,"m_CachedSpellbook",source.Spellbook);return a;}
 static RuleCalculateAbilityParams Params(UnitEntityData u,AbilityData a) {var e=new RuleCalculateAbilityParams(u,a){ReplaceDC=15,ReplaceCasterLevel=10,ReplaceSpellLevel=a.SpellLevel,ReplaceConcentration=0,ReplaceStatBonusModifier=0};return e;}
 static int Bonus(RuleCalculateAbilityParams e){var b=(ModifiableBonus)Get(e,"m_BonusDC");return b==null?0:(int)b;}
 static RuleCastSpell Cast(UnitEntityData u,AbilityData a,bool forced=false,bool duplicate=false) {var e=Empty<RuleCastSpell>();Set(e,"Initiator",u);Set(e,"Spell",a);Set(e,"ForceFail",forced);Set(e,"IsDuplicateSpellApplied",duplicate);return e;}
 static bool Add(BuffCollection __instance,BlueprintBuff blueprint,MechanicsContext parentContext,TimeSpan? duration,ref Buff __result) {__result=Psychic034Tests.Fact(__instance.Owner,blueprint,__instance.Owner,10);return false;}
 static bool Remove(Buff __instance){removed.Add(__instance);Set(__instance,"IsTurnedOn",false);__instance.Owner.Facts.List.Remove(__instance);var c=Get(__instance.Owner.Facts,"m_Cache");AccessTools.Method(c.GetType(),"Clear").Invoke(c,null);return false;}
 static bool Dice(ref int __result){diceRolls++;__result=3;return false;}
 static bool Skip()=>false;
 static bool ContextParams(Kingmaker.UnitLogic.Mechanics.Components.ContextSetAbilityParams __instance,MechanicsContext context,ref AbilityParams __result) {
  // Rulebook/difficulty services are scene boundaries; construct the real native
  // rule so the installed ContextSetAbilityParams still evaluates caster properties.
  var r=new RuleCalculateAbilityParams(context.MaybeCaster,context.AssociatedBlueprint,null);
  __result=new AbilityParams{DC=r.ReplaceDC??15,CasterLevel=r.ReplaceCasterLevel??10,SpellLevel=r.ReplaceSpellLevel??0,Concentration=r.ReplaceConcentration??0};return false;
 }
 static AbilityData Fixed(AbilityData a) {var fact=Learn(a.Caster.Unit,a.Blueprint);Set(fact,"OverrideParams",new AbilityParams{DC=15,CasterLevel=10,SpellLevel=a.SpellLevel});Set(a,"m_Fact",fact);return a;}
 static RuleCastSpell Attempt(UnitEntityData u,AbilityData a,bool forced=false,bool duplicate=false,bool success=true) {
  var e=Cast(u,a,forced,duplicate);Set(e,"Context",new AbilityExecutionContext(a,a.CalculateParams(),new TargetWrapper(u)));e.SetSuccess(success);e.OnTrigger(null);return e;
 }
 static bool Approach(AbilityData __instance,ref float __result) {__result=__instance.Blueprint.Range==AbilityRange.Touch?1.5f:30*0.3048f;return false;}
 internal static int Run(BlueprintsCache c) {
  cache=c;n=0;var h=new Harmony("CotW.SpellFeats0146.Native");
  h.Patch(AccessTools.Method(typeof(FeatureCollection),"CheckStatPrerequisitesAndUpdate"),prefix:new HarmonyMethod(typeof(SpellResourceFeatTests),nameof(Skip)));
  h.Patch(AccessTools.Method(typeof(BuffCollection),"AddBuff",new[]{typeof(BlueprintBuff),typeof(MechanicsContext),typeof(TimeSpan?)}),prefix:new HarmonyMethod(typeof(SpellResourceFeatTests),nameof(Add)));
  h.Patch(AccessTools.Method(typeof(Buff),"Remove"),prefix:new HarmonyMethod(typeof(SpellResourceFeatTests),nameof(Remove)));
  h.Patch(AccessTools.PropertyGetter(typeof(RulebookEvent.Dice),"D6"),prefix:new HarmonyMethod(typeof(SpellResourceFeatTests),nameof(Dice)));
  // Scene execution and view corpulence are boundaries. Native param getter,
  // ContextSetAbilityParams/recalculation, duration calculator, slots and HP run.
  h.Patch(AccessTools.Method(typeof(RuleCastSpell),"OnTrigger"),prefix:new HarmonyMethod(typeof(SpellResourceFeatTests),nameof(Skip)){priority=Priority.First});
  h.Patch(AccessTools.Method(typeof(AbilityData),"GetApproachDistance"),prefix:new HarmonyMethod(typeof(SpellResourceFeatTests),nameof(Approach)){priority=Priority.First});
  h.Patch(AccessTools.Method(typeof(Kingmaker.UnitLogic.Mechanics.Components.ContextSetAbilityParams),"Calculate",new[]{typeof(MechanicsContext)}),prefix:new HarmonyMethod(typeof(SpellResourceFeatTests),nameof(ContextParams)));
  foreach(var name in new[]{"SpellResourceParamsPatch","SpellResourceCastPatch","SpellResourceRecalculatePatch","AmplifiedHexRangePatch","AmplifiedHexDurationPatch"})h.CreateClassProcessor(Module.Assembly.GetType("CallOfTheWild.Wrath.Prototype."+name)).Patch();
  // On standalone CLR, MethodBase.Invoke can sporadically lose this patched
  // native Nullable<TimeSpan> return (observed postfix30s -> reflection null,
  // also on the actual054 DLL). Call the same private native method directly
  // through its exact open delegate; all native arithmetic/patches and both
  // finite/permanent assertions below remain unchanged.
  var nativeDuration=(Func<ContextActionApplyBuff,MechanicsContext,TimeSpan?>)Delegate.CreateDelegate(typeof(Func<ContextActionApplyBuff,MechanicsContext,TimeSpan?>),AccessTools.Method(typeof(ContextActionApplyBuff),"CalculateDuration"));
  try {
   var register=AccessTools.Method(Module,"Register");register.Invoke(null,new object[]{cache});var first=Asset<BlueprintFeature>("AmplifiedHexFeat");register.Invoke(null,new object[]{cache});Check(ReferenceEquals(first,Asset<BlueprintFeature>("AmplifiedHexFeat")),"module repeat keeps identity");var empty=new BlueprintsCache();bool rejected=false;try{register.Invoke(null,new object[]{empty});}catch{rejected=true;}Check(rejected&&empty.Load(Id("AmplifiedHexFeat"))==null,"missing dependency leaves no partial assets");
   var u=Unit("0146-resource");var other=Unit("0146-other");var feat=Feature(u,first);var daily=Runtime(feat,typeof(AmplifiedHexDaily)).GetData<AmplifiedHexDailyState>();var prereq=first.GetComponent<PrerequisiteSupportedHex>();
   Check(!(bool)AccessTools.Method(typeof(PrerequisiteSupportedHex),"CheckInternal").Invoke(prereq,new object[]{null,u.Descriptor,null}),"no hex denied");
   var hexBp=(BlueprintAbility)cache.Load(BlueprintGuid.Parse("630ea63a63457ff4f9de059c578c930a"));Learn(u,hexBp);Learn(u,Asset<BlueprintAbility>("AmplifiedHexConversion"));Check((bool)AccessTools.Method(typeof(PrerequisiteSupportedHex),"CheckInternal").Invoke(prereq,new object[]{null,u.Descriptor,null}),"real native witch hex qualifies");
   var cls=(BlueprintCharacterClass)cache.Load(BlueprintGuid.Parse("d77e67a814d686842802c9cfd8ef8499"));var conversion=Asset<BlueprintAbility>("AmplifiedHexConversion").GetComponent<AmplifiedHexConversion>();
   foreach(bool spontaneous in new[]{false,true})foreach(int oldUses in new[]{0,1,8,9,50})foreach(int level in Enumerable.Range(0,10)) {
    daily.Uses=oldUses;var book=Book(u,spontaneous,cls);var source=Spell(u,book,level);var a=Conversion(u,source);bool legal=level>0;Check(conversion.IsAbilityVisible(a)==legal&&conversion.IsAbilityRestrictionPassed(a)==legal,"every 1-9 slot usable regardless of old daily count");if(!legal)continue;
    Check(source.CanBeConvertedTo(a.Blueprint)&&source.GetConversions().Any(x=>x.Blueprint.AssetGuid==a.Blueprint.AssetGuid),"native prepared/spontaneous conversion menu");
    int before=spontaneous?book.GetSpontaneousSlots(level):book.GetMemorizedSpellSlots(level).Count(s=>s.Available);AccessTools.Method(typeof(AbilityData),"SpendOneSpellCharge").Invoke(a,null);int after=spontaneous?book.GetSpontaneousSlots(level):book.GetMemorizedSpellSlots(level).Count(s=>s.Available);Check(after==before-1,"one actual slot spent");
    conversion.Apply(new AbilityExecutionContext(a,new AbilityParams{DC=15,CasterLevel=10,SpellLevel=level},new TargetWrapper(u)),new TargetWrapper(u));var b=u.Buffs.Enumerable.Single(x=>x.Blueprint.AssetGuid==Id("AmplifiedHexBuff"));var state=Runtime(b,typeof(AmplifiedHexBoost)).GetData<SpellBoostState>();Check(state.Level==level&&daily.Uses==oldUses+1&&b.IsNotDispelable,"saved charge records actual sacrificed level");Check(!conversion.IsAbilityRestrictionPassed(a),"no stacking armed hexes");
    var hex=Fixed(new AbilityData(hexBp,u.Descriptor));int original=hex.Fact.OverrideParams.DC;Check(hex.CalculateParams().DC==15+level&&hex.Fact.OverrideParams.DC==original,"native OverrideParams route boosts DC without mutating source params");
    var normal=Fixed(Spell(u,book,level));Check(normal.CalculateParams().DC==15,"ordinary spell unchanged");Attempt(u,normal);Check(u.Buffs.Enumerable.Contains(b),"ordinary spell preserves hex charge");Attempt(u,hex,true);Attempt(u,hex,false,true);Attempt(u,hex,false,false,false);Check(u.Buffs.Enumerable.Contains(b),"forced duplicate and skipped unsuccessful cast retain charge");
    var e=Attempt(u,hex);Check(!u.Buffs.Enumerable.Contains(b)&&state.Consumed&&e.Context.Params.DC==15+level,"actual cast transaction consumes one charge and keeps DC snapshot");
    // Execute the same native context calculator type used by installed Witch:
    // it overwrites DC after the buff is gone. Property input is bounded to 15.
    var calc=new Kingmaker.UnitLogic.Mechanics.Components.ContextSetAbilityParams{DC=15,CasterLevel=10,SpellLevel=0,Concentration=0};var owned=(BlueprintAbility)AccessTools.Method(typeof(object),"MemberwiseClone").Invoke(hexBp,null);owned.ComponentsArray=new BlueprintComponent[]{calc};Set(e.Context,"AssociatedBlueprint",owned);e.Context.Recalculate();Check(e.Context.Params.DC==15+level,"native ContextSetAbilityParams recalculation cannot erase amplified DC");e.Context.Recalculate();Check(e.Context.Params.DC==15+level,"repeated recalculation never stacks DC");
    var action=new ContextActionApplyBuff{DurationValue=new ContextDurationValue{Rate=DurationRate.Rounds,DiceType=DiceType.Zero,DiceCountValue=0,BonusValue=2}};Set(action,"m_Buff",BlueprintReferenceBase.CreateTyped<BlueprintBuffReference>(Asset<BlueprintBuff>("RageCasting1Buff")));var duration=nativeDuration(action,e.Context);Check(duration==TimeSpan.FromSeconds((2+level)*6),"native finite duration gets slot-level rounds after charge consumed: level="+level+", actual="+duration+", buff="+action.Buff?.name+", globalCache="+ReferenceEquals(ResourcesLibrary.BlueprintsCache,cache));action.Permanent=true;Check(nativeDuration(action,e.Context)==null,"permanent duration unchanged");
    Check(hex.CalculateParams().DC==15,"next hex no leftover bonus");
   }
   foreach(int level in Enumerable.Range(1,9)) {
    var b=Psychic034Tests.Fact(u,Asset<BlueprintBuff>("AmplifiedHexBuff"),u,1);Runtime(b,typeof(AmplifiedHexBoost)).GetData<SpellBoostState>().Level=level;var hex=Fixed(new AbilityData(hexBp,u.Descriptor));Check(Math.Abs(hex.GetApproachDistance()-((30+10*((level+2)/3))*0.3048f))<0.001,"range tier exactly 10/20/30 feet");b.Remove();
   }
   foreach(int dose in Enumerable.Range(1,4))foreach(int level in Enumerable.Range(0,5))foreach(bool blood in new[]{false,true}) {
    var book=Book(u,true,blood?cls:new BlueprintCharacterClass{AssetGuid=BlueprintGuid.Parse("1b9873f1e7bfe5449bc84d03e9c8e3cc")});if(blood)book.Blueprint.AssetGuid=BlueprintGuid.Parse("e19484252c2f80e4a9439b3681b20f00");var spell=Fixed(Spell(u,book,level));var ability=Asset<BlueprintAbility>("RageCasting"+dose+"Ability");ability.GetComponent<RageCastingArm>().Apply(new AbilityExecutionContext(new AbilityData(ability,u.Descriptor),new AbilityParams(),new TargetWrapper(u)),new TargetWrapper(u));var b=u.Buffs.Enumerable.Single(x=>x.Blueprint.AssetGuid==Id("RageCasting"+dose+"Buff"));bool legal=blood&&level>0;int count=Math.Min(dose,level),hp=u.Descriptor.Damage;var temp=u.Stats.TemporaryHitPoints.AddModifier(50,(EntityFact)null,Kingmaker.Enums.ModifierDescriptor.UntypedStackable);Check(u.Descriptor.Damage==hp,"activation arms without HP payment");Check(spell.CalculateParams().DC==15+(legal?count:0),"native bloodrager book/level eligibility and real DC");diceRolls=0;var e=Attempt(u,spell);Check(u.Descriptor.Damage==hp+(legal?count*3:0)&&diceRolls==(legal?count:0),"native HP setter loses capped d6 only on actual bloodrager attempt");Check(u.Stats.TemporaryHitPoints.ModifiedValue==50,"HP loss bypasses actual temporary HP modifier");e.OnTrigger(null);Check(u.Descriptor.Damage==hp+(legal?count*3:0),"same cast notification cannot pay twice");if(legal){e.Context.Recalculate();Check(e.Context.Params.DC==15+count,"bloodrager DC preserved after consume");}else b.Remove();temp.Remove();
   }
   foreach(string id in (System.Collections.IEnumerable)AccessTools.Field(Module,"HexIds").GetValue(null)){var bp=(BlueprintAbility)cache.Load(BlueprintGuid.Parse(id));Check(bp!=null&&(bool)AccessTools.Method(Module,"Hex").Invoke(null,new object[]{bp}),"supported native hex stable identity "+id);}
   var armed=Psychic034Tests.Fact(u,Asset<BlueprintBuff>("AmplifiedHexBuff"),u,1);Runtime(armed,typeof(AmplifiedHexBoost)).GetData<SpellBoostState>().Level=9;var input=Fixed(new AbilityData(hexBp,u.Descriptor));Set(armed,"IsSuppressed",true);Check(input.CalculateParams().DC==15,"suppressed charge no DC");Attempt(u,input);Check(u.Buffs.Enumerable.Contains(armed),"suppressed charge not consumed");Set(armed,"IsSuppressed",false);Check(JsonConvert.DeserializeObject<SpellBoostState>(JsonConvert.SerializeObject(Runtime(armed,typeof(AmplifiedHexBoost)).GetData<SpellBoostState>())).Level==9,"level exact JSON roundtrip");
   Call(feat,typeof(AmplifiedHexDaily),"HandleUnitRest",other);Check(u.Buffs.Enumerable.Contains(armed),"other rest isolated");Call(feat,typeof(AmplifiedHexDaily),"HandleUnitRest",u);Check(!u.Buffs.Enumerable.Contains(armed)&&daily.Uses==0,"own rest clears charge");
   armed=Psychic034Tests.Fact(u,Asset<BlueprintBuff>("AmplifiedHexBuff"),u,1);Call(feat,typeof(AmplifiedHexDaily),"OnTurnOff");Check(!u.Buffs.Enumerable.Contains(armed),"retraining clears hex charge");
   var rageFeat=Feature(u,Asset<BlueprintFeature>("RageCastingFeat"));for(int i=1;i<=4;i++)Psychic034Tests.Fact(u,Asset<BlueprintBuff>("RageCasting"+i+"Buff"),u,1);Call(rageFeat,typeof(SpellFeatCleanup),"OnTurnOff");Check(!u.Buffs.Enumerable.Any(b=>Enumerable.Range(1,4).Any(i=>b.Blueprint.AssetGuid==Id("RageCasting"+i+"Buff"))),"retraining cleans all rage doses");
   var tokens=new[]{"AmplifiedHexFeat","AmplifiedHexBuff","AmplifiedHexConversion","AmplifiedHexCancel","RageCastingFeat","RageCastingAbility"}.Concat(Enumerable.Range(1,4).SelectMany(i=>new[]{"RageCasting"+i+"Buff","RageCasting"+i+"Ability"}));var assets=new JArray();foreach(var token in tokens){var bp=Asset<BlueprintScriptableObject>(token);Check(bp.ComponentsArray.All(x=>x.OwnerBlueprint==bp)&&bp.ComponentsArray.Select(x=>x.name).Distinct().Count()==bp.ComponentsArray.Length,"stable component identity "+token);assets.Add(new JObject{["token"]=token,["guid"]=bp.AssetGuid.ToString(),["name"]=bp.name,["type"]=bp.GetType().Name});}
   PsychicExpansionTests.Inventory["spellResource0144Assets"]=assets;PsychicExpansionTests.Inventory["spellResource0144Assertions"]=n;Console.WriteLine("PASS: "+n+" repaired spell-resource checks; native OverrideParams, ContextSetAbilityParams recalculation, duration, conversion/slots and HP; view/scene input endpoints bounded.");return n;
  } finally {h.UnpatchAll(h.Id);}
 }

}

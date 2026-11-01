using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.Serialization;
using HarmonyLib;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Facts;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.Blueprints.Items.Weapons;
using Kingmaker.EntitySystem;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Enums;
using Kingmaker.Items;
using Kingmaker.ResourceLinks;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components;
using Kingmaker.UnitLogic.Buffs;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Buffs.Components;
using Kingmaker.UnitLogic.FactLogic;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Parts;
using Kingmaker.Utility;
using CallOfTheWild.Wrath.Prototype;
internal static class Psychic027Tests {
 static int n,bodyCalls;static Gender gender;static UnitEntityData u;static EntityFactsManager facts;static readonly List<BlueprintUnitFact> grants=new List<BlueprintUnitFact>();static BlueprintItemWeapon[] weapons;
 static T Empty<T>()where T:class=>(T)FormatterServices.GetUninitializedObject(typeof(T));static object Get(object o,string k)=>AccessTools.Field(o.GetType(),k).GetValue(o);static void Set(object o,string k,object v)=>AccessTools.Field(o.GetType(),k).SetValue(o,v);static void Check(bool b,string m){if(!b)throw new Exception("027: "+m);n++;}
 static T Asset<T>(BlueprintsCache c,string t)where T:SimpleBlueprint=>(T)c.Load((BlueprintGuid)AccessTools.Method(typeof(PsychicSuffocation).Assembly.GetType("CallOfTheWild.Wrath.Prototype.PsychicAssets"),"Id").Invoke(null,new object[]{t}));
 static void Clear(){facts.List.Clear();AccessTools.Method(Get(facts,"m_Cache").GetType(),"Clear").Invoke(Get(facts,"m_Cache"),null);}
 static MechanicsContext Context(BlueprintScriptableObject bp){var c=new MechanicsContext(u,u.Descriptor,bp,null,new TargetWrapper(u));c.SetParams(new AbilityParams {CasterLevel=20,DC=30,SpellLevel=7});return c;}
 static Buff Fact(BlueprintBuff bp){var b=Empty<Buff>();Set(b,"Components",new List<EntityFactComponent>());b.RestoreOwnerLink(facts);Set(b,"m_Context",Context(bp));Set(b,"<Rank>k__BackingField",1);AccessTools.Method(typeof(EntityFact),"Setup").Invoke(b,new object[]{bp});Set(b,"<IsActive>k__BackingField",true);Set(b,"<IsTurnedOn>k__BackingField",true);facts.List.Add(b);AccessTools.Method(Get(facts,"m_Cache").GetType(),"Clear").Invoke(Get(facts,"m_Cache"),null);return b;}
 internal static int Run(BlueprintsCache c){u=new Psychic070Tests.Caster(c).Unit;facts=new EntityFactsManager(u);Set(u,"Facts",facts);var bc=new BuffCollection();bc.Initialize(facts);Set(u,"<Buffs>k__BackingField",bc);Set(u.State,"m_Conditions",new sbyte[128]);Set(u.State,"m_ConditionsImmunity",new sbyte[128]);Set(u.State,"m_ConditionsExceptions",new List<UnitConditionExceptions>[128]);Set(u.Descriptor,"<Body>k__BackingField",Empty<UnitBody>());
 foreach(var stat in new[]{StatType.Strength,StatType.Dexterity,StatType.Constitution}){var v=new ModifiableValueAttributeStat(u.Stats,stat){BaseValue=20};Set(u.Stats,stat.ToString(),v);u.Stats.AllStats.Add(v);}var ac=new ModifiableValueArmorClass(u.Stats);Set(u.Stats,"AC",ac);u.Stats.AllStats.Add(ac);var speed=new ModifiableValueSpeed(u.Stats){BaseValue=30};Set(u.Stats,"Speed",speed);u.Stats.AllStats.Add(speed);
 var h=new Harmony("CotW027.NativeRepairs");try{Construction(c);Stats(c);NativePolymorph(c,h);Tar(c,h);}finally{h.UnpatchAll(h.Id);Clear();}PsychicExpansionTests.Inventory["repair027Assertions"]=n;Console.WriteLine("PASS: "+n+" native repair checks: real fact initialization, component dictionary, stat/condition turn-on-off, polymorph activation with equipment/scene endpoints captured, native gender prefab getters, scoped Tar delivery and area filtering.");return n;}
 static string[] Forms={"UndeadAnatomy1","UndeadAnatomy2","UndeadAnatomy3","GiantForm1Troll","GiantForm1Fire","GiantForm1Frost","GiantForm2Storm","GiantForm2Athach","MagicalBeastShapeBulette","MagicalBeastShapeHydra"};
 static void Construction(BlueprintsCache c){foreach(string token in Forms.Select(t=>t+"Buff").Concat(new[]{"SuffocationBuff","ControlConstructBuff","ScouringWindsController","ScouringWindsFog","BabbleBuff","BabbleFascinatedBuff"})){Clear();var bp=Asset<BlueprintBuff>(c,token);var f=Fact(bp);Check(f.Components.Count==bp.ComponentsArray.Count(x=>x is IRuntimeEntityFactComponentProvider),"No runtime skipped: "+token);Check(f.Components.Select(x=>x.SourceBlueprintComponentName).Distinct().Count()==f.Components.Count&&f.Components.All(x=>!string.IsNullOrEmpty(x.SourceBlueprintComponentName)),"Unique persistence IDs: "+token);Check(((IDictionary)AccessTools.Property(typeof(EntityFact),"ComponentsDictionary").GetValue(f)).Count==f.Components.Count,"Native save dictionary accepts repaired graph");}Clear();
 var old=Asset<BlueprintBuff>(c,"UndeadAnatomy1Buff");var names=old.ComponentsArray.Select(x=>x.name).ToArray();try{foreach(var comp in old.ComponentsArray)comp.name=null;var fact=Fact(old);Check(fact.Components.Count==1,"Prior unnamed graph reproduces size-only runtime");}finally{for(int i=0;i<names.Length;i++)old.ComponentsArray[i].name=names[i];Clear();}
 var babble=Fact(Asset<BlueprintBuff>(c,"BabbleBuff"));Check(babble.Components.Any(x=>x.SourceBlueprintComponent is AddAreaEffect),"Native graph includes fascination aura emitter alongside nausea");Clear();}
 static void Stats(BlueprintsCache c){foreach(var token in Forms.Take(8)){Clear();var b=Fact(Asset<BlueprintBuff>(c,token+"Buff"));var stats=b.Components.Where(x=>x.SourceBlueprintComponent is AddStatBonus).ToArray();var before=stats.ToDictionary(x=>((AddStatBonus)x.SourceBlueprintComponent).Stat,x=>u.Stats.GetStat(((AddStatBonus)x.SourceBlueprintComponent).Stat).ModifiedValue);foreach(var rt in stats)rt.TurnOn();foreach(var rt in stats){var comp=(AddStatBonus)rt.SourceBlueprintComponent;Check(comp.Stat==StatType.AC?u.Stats.AC.GetModifiers(comp.Descriptor).Sum(x=>x.ModValue)==comp.Value:u.Stats.GetStat(comp.Stat).ModifiedValue==before[comp.Stat]+comp.Value,"Real form stat modifier: "+token+" "+comp.Stat);}foreach(var rt in stats.Reverse())rt.TurnOff();Check(before.All(x=>u.Stats.GetStat(x.Key).ModifiedValue==x.Value),"Form stat cleanup restores original values");}
 foreach(var pair in new[]{Tuple.Create("SuffocationBuff",UnitCondition.Staggered),Tuple.Create("BabbleBuff",UnitCondition.Nauseated)}){Clear();var b=Fact(Asset<BlueprintBuff>(c,pair.Item1));var rt=b.Components.Single(x=>x.SourceBlueprintComponent is AddCondition);rt.TurnOn();Check(u.State.HasCondition(pair.Item2),"Native condition active: "+pair.Item1);rt.TurnOff();Check(!u.State.HasCondition(pair.Item2),"Native condition cleanup");}Clear();}
 static void NativePolymorph(BlueprintsCache c,Harmony h){
 void Patch(System.Reflection.MethodBase m,string n)=>h.Patch(m,prefix:new HarmonyMethod(typeof(Psychic027Tests),n));
 Patch(AccessTools.PropertyGetter(typeof(UnitDescriptor),"Gender"),nameof(GenderValue));
 Patch(AccessTools.Method(typeof(UnitPartSizeModifier),"Add",new[]{typeof(EntityFact)}),nameof(Skip));
 Patch(AccessTools.Method(typeof(UnitPartSizeModifier),"Remove",new[]{typeof(EntityFact)}),nameof(Skip));
 Patch(AccessTools.Method(typeof(UnitBody),"ApplyPolymorphEffect"),nameof(Body));
 Patch(AccessTools.Method(typeof(UnitBody),"CancelPolymorphEffect"),nameof(Skip));
 Patch(AccessTools.PropertySetter(typeof(UnitEntityData),"ReplaceBlueprintForInspection"),nameof(Skip));
 Patch(typeof(UnitHelper).GetMethods().Single(m=>m.Name=="AddFact"&&!m.IsGenericMethod&&m.GetParameters().Select(x=>x.ParameterType).SequenceEqual(new[]{typeof(UnitEntityData),typeof(BlueprintUnitFact),typeof(MechanicsContext),typeof(Kingmaker.Blueprints.Classes.Selection.FeatureParam)})),nameof(Grant));
 Patch(AccessTools.Method(typeof(EntityFact),"Dispose"),nameof(Skip));h.Patch(AccessTools.Method(typeof(Polymorph),"OnDeactivate"),transpiler:new HarmonyMethod(typeof(Psychic027Tests),nameof(NoRestoreView)));
 foreach(var token in Forms.Skip(8))foreach(var sex in new[]{Gender.Male,Gender.Female}){Clear();gender=sex;bodyCalls=0;grants.Clear();var b=Fact(Asset<BlueprintBuff>(c,token+"Buff"));var poly=b.Blueprint.GetComponent<Polymorph>();var rt=b.Components.Single(x=>x.SourceBlueprintComponent==poly);Check(poly.GetPrefab(u)==Get(poly,"m_Prefab"),"Native male/female prefab lookup returns configured body without null wrapper");Check(poly.Portrait==null&&poly.ReplaceUnitForInspection==null&&poly.Race==null,"Empty optional native reference getters safe");using(rt.RequestEventContext())AccessTools.Method(typeof(Polymorph),"OnActivate").Invoke(poly,null);Check(u.Stats.Strength.ModifiedValue==28&&u.Stats.Dexterity.ModifiedValue==16&&u.Stats.Constitution.ModifiedValue==22,"Native polymorph applies all ability modifiers");Check(bodyCalls==1&&weapons.Length==(token.EndsWith("Hydra")?5:3)&&weapons.All(x=>x!=null),"Native polymorph passes all natural weapons to equipment endpoint");Check(grants.Count==poly.Facts.Count()&&grants.Contains(poly.Facts.First()),"Native polymorph grants configured facts");using(rt.RequestEventContext())AccessTools.Method(typeof(Polymorph),"OnDeactivate").Invoke(poly,null);Check(u.Stats.Strength.ModifiedValue==20&&u.Stats.Dexterity.ModifiedValue==20&&u.Stats.Constitution.ModifiedValue==20,"Native polymorph modifier removal restores attributes");}
 // Positive control for old explicit null female link: same native getter throws.
 var probe=new Polymorph();Set(probe,"m_Prefab",new UnitViewLink{AssetId="body"});gender=Gender.Female;bool failed=false;try{probe.GetPrefab(u);}catch(NullReferenceException){failed=true;}Check(failed,"Old null female prefab reproduces native getter failure");Clear();}
 static void Tar(BlueprintsCache c,Harmony h){var assembly=typeof(PsychicSuffocation).Assembly;var flight=assembly.GetType("CallOfTheWild.Wrath.Prototype.PsychicFlightGroundPatch");var blocks=AccessTools.Method(flight,"BlocksTar");h.CreateClassProcessor(flight).Patch();var method=AccessTools.Method(typeof(AreaEffectEntityData),"IsSuitableTargetType");h.Patch(method,prefix:new HarmonyMethod(typeof(Psychic027Tests),nameof(Allow)){priority=Priority.Last});foreach(var spell in new[]{"AirWalkBuff","OverlandFlightBuff"})foreach(bool active in new[]{false,true})foreach(bool tar in new[]{false,true}){Clear();var b=Fact(Asset<BlueprintBuff>(c,spell));Set(b,"<IsTurnedOn>k__BackingField",active);var bp=new BlueprintAbility{AssetGuid=BlueprintGuid.Parse(tar?"7d700cdf260d36e48bb7af3a8ca5031f":"aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa")};Check((bool)blocks.Invoke(null,new object[]{u,Context(bp)})==(tar&&active),"Initial Tar pulse rejected only for active flight and exact source");var area=Empty<AreaEffectEntityData>();Set(area,"m_Blueprint",new BlueprintAbilityAreaEffect {AssetGuid=BlueprintGuid.Parse(tar?"d59890fb468915946bae085439bd0881":"aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa"),ComponentsArray=Array.Empty<BlueprintComponent>()});Set(area,"m_Context",Context(bp));Check((bool)method.Invoke(area,new object[]{u})==(!tar||!active),"Installed area hook filters Tar but leaves non-ground effects");}Clear();}
 static IEnumerable<CodeInstruction> NoRestoreView(IEnumerable<CodeInstruction> codes){foreach(var code in codes){if(code.Calls(AccessTools.Method(typeof(Polymorph),"RestoreView"))){code.opcode=System.Reflection.Emit.OpCodes.Pop;code.operand=null;}yield return code;}}
 static bool GenderValue(ref Gender __result){__result=gender;return false;}static bool Skip()=>false;static bool Allow(ref bool __result){__result=true;return false;}
 static bool Body(BlueprintItemWeapon __0,BlueprintItemWeapon __1,BlueprintItemWeapon[] __2,BlueprintItemWeapon[] __3){bodyCalls++;weapons=new[]{__0,__1}.Concat(__2).Concat(__3).ToArray();return false;}
 static bool Grant(BlueprintUnitFact __1,ref EntityFact __result){grants.Add(__1);__result=Empty<UnitFact>();return false;}
}

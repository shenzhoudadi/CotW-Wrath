using System;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using CallOfTheWild.Wrath.Prototype;
internal static class Compatibility01420Tests {
 static int checks;static readonly Dictionary<BlueprintSpellList,BlueprintGuid[]> foreign=new Dictionary<BlueprintSpellList,BlueprintGuid[]>();
 static readonly Type helper=typeof(CrueltyFocusDC).Assembly.GetType("CallOfTheWild.Wrath.Prototype.SpellListCompatibility");
 static void Check(bool ok,string name){if(!ok)throw new Exception("Compatibility01420: "+name);checks++;}
 static BlueprintAbilityReference[] Spells(SpellLevelList row)=>(AccessTools.Field(typeof(SpellLevelList),"m_Spells").GetValue(row)as List<BlueprintAbilityReference>)?.ToArray()??Array.Empty<BlueprintAbilityReference>();
 static SpellLevelList Row(int level,params BlueprintAbilityReference[] refs){var row=new SpellLevelList(level);AccessTools.Field(typeof(SpellLevelList),"m_Spells").SetValue(row,refs.ToList());return row;}
 internal static void Inject(BlueprintsCache cache) {
  foreach(var pair in new[]{new[]{"f60d0cd93edc65c42ad31e34a905fb2f","4"},new[]{"57c894665b7895c499b3dce058c284b3","6"},new[]{"8443ce803d2d31347897a3d85cc32f53","1"}}) {
   var list=(BlueprintSpellList)cache.Load(BlueprintGuid.Parse(pair[0]));int level=int.Parse(pair[1]);var refs=list.SpellsByLevel.SelectMany(Spells).Where(r=>r!=null).GroupBy(r=>r.Guid).Select(g=>g.ElementAt(0)).Take(3).ToArray();
   foreign[list]=refs.Select(r=>r.Guid).ToArray();list.SpellsByLevel=list.SpellsByLevel.Concat(new[]{Row(level,refs),Row(level,refs.Reverse().ToArray())}).ToArray();
  }
  Console.WriteLine("01420 injected duplicate-level append shape from DragonChanges AddToSpellsByLevel, with foreign references retained.");
 }
 internal static int Run(BlueprintsCache cache) {
  checks=0;
  Check(foreign.Count==3,"ordinary build includes append-shape input before production registration");
  foreach(var p in foreign){Check(p.Key.SpellsByLevel.Select((r,i)=>r!=null&&r.SpellLevel==i).All(x=>x),"full production registration canonicalized external rows");foreach(var id in p.Value)Check(p.Key.SpellsByLevel.SelectMany(Spells).Any(r=>r.Guid==id),"foreign spell identity retained");}
  var a=new BlueprintAbilityReference();AccessTools.Field(typeof(BlueprintReferenceBase),"deserializedGuid").SetValue(a,BlueprintGuid.Parse("dbcb84e088e8417caae276b6e69dd713"));
  var b=new BlueprintAbilityReference();AccessTools.Field(typeof(BlueprintReferenceBase),"deserializedGuid").SetValue(b,BlueprintGuid.Parse("2d14d4ffbfce4892900132bba45616a0"));
  var shared=Row(4,a,b);var list=new BlueprintSpellList{name="01420late",AssetGuid=BlueprintGuid.Parse("fe4447c856bf4214885e58f67a5ba520"),SpellsByLevel=new[]{Row(0),shared,Row(4,a)}};cache.AddCachedBlueprint(list.AssetGuid,list);
  var sibling=new BlueprintSpellList{name="01420shared",AssetGuid=BlueprintGuid.Parse("8883a0da41c04439b07e8b8ec23bb6d3"),SpellsByLevel=new[]{Row(0),shared}};cache.AddCachedBlueprint(sibling.AssetGuid,sibling);
  int repaired=(int)AccessTools.Method(helper,"Reconcile").Invoke(null,new object[]{cache});Check(repaired>=2,"late cleanup repairs appended and sparse rows");
  Check(list.SpellsByLevel.Length==11&&list.SpellsByLevel[4].SpellLevel==4,"native numeric indexing and reserved 0..10 slots");Check(Spells(list.SpellsByLevel[4]).Select(r=>r.Guid).SequenceEqual(new[]{a.Guid,b.Guid}),"stable identity union, duplicates removed");Check(Spells(shared).Length==2,"shared donor row untouched");Check(!ReferenceEquals(list.SpellsByLevel[4],sibling.SpellsByLevel[4]),"shared rows isolated per list");
  var once=list.SpellsByLevel;AccessTools.Method(helper,"Reconcile").Invoke(null,new object[]{cache});Check(ReferenceEquals(once,list.SpellsByLevel),"canonical repeat is allocation-free");
  var bad=new[]{Row(-1,a)};Check(AccessTools.Method(helper,"Build").Invoke(null,new object[]{bad,0})==null,"unsupported external level skipped safely");
  var policy=typeof(CrueltyFocusDC).Assembly.GetType("CotW.Runtime.HarmonyRuntimeCompatibility");AccessTools.Method(policy,"Verify",new[]{typeof(Assembly)}).Invoke(null,new object[]{typeof(Harmony).Assembly});Check(true,"actual loaded baseline API accepted");
  var fake=AppDomain.CurrentDomain.DefineDynamicAssembly(new AssemblyName("0Harmony"){Version=new Version(2,3,6,0)},AssemblyBuilderAccess.Run);bool rejected=false;try{AccessTools.Method(policy,"Verify",new[]{typeof(Assembly)}).Invoke(null,new object[]{fake});}catch(TargetInvocationException e){rejected=e.InnerException is InvalidOperationException;}Check(rejected,"version alone does not approve missing Harmony API");
  PsychicExpansionTests.Inventory["compatibility01420Assertions"]=checks;PsychicExpansionTests.Inventory["compatibility01420InjectedLists"]=foreign.Count;
  Console.WriteLine("PASS: "+checks+" 01420 duplicate-level, foreign identity, alias, late cleanup, idempotence and actual Harmony API contracts.");return checks;
 }
}

using System;using System.Collections.Generic;using System.IO;using System.Linq;using System.Reflection;using HarmonyLib;using Newtonsoft.Json;using Newtonsoft.Json.Linq;
using Kingmaker.Blueprints;using Kingmaker.Blueprints.JsonSystem;using Kingmaker.Localization;using Kingmaker.Localization.Shared;
internal static partial class MagusBatch01454Tests{
 static JObject GameEnglish060,PackageEnglish060;static string QlPath060;static bool LocaleExport060;
 static bool QlPathInput060(ref string __result){__result=QlPath060;return false;}
 static string EnglishName060(BlueprintScriptableObject bp){
  var value=AccessTools.Field(bp.GetType(),"m_DisplayName")?.GetValue(bp) as LocalizedString;
  var authored=(Dictionary<string,string>)AccessTools.Field(T("PsychicAssets"),"Strings").GetValue(null);
  string key=value?.Key;if(key!=null&&authored.TryGetValue(key,out var own))return own;
  if(key!=null&&GameEnglish060[key]?.Type==JTokenType.String)return (string)GameEnglish060[key];
  if(key!=null&&PackageEnglish060[key]?.Type==JTokenType.String)return (string)PackageEnglish060[key];
  if(paths.TryGetValue(bp.AssetGuid.ToString(),out var path)){JObject raw;using(var reader=new StreamReader(archive.GetEntry(path).Open()))raw=JObject.Parse(reader.ReadToEnd());var text=raw["Data"]?["m_DisplayName"];string shared=(string)(text?["Shared"] as JObject)?["stringkey"],direct=(string)text?["m_Key"];string original=string.IsNullOrEmpty(direct)?shared:direct;if(!string.IsNullOrEmpty(original)&&GameEnglish060[original]?.Type==JTokenType.String)return (string)GameEnglish060[original];if(text!=null&&string.IsNullOrEmpty(original))return "";}
  if(bp is Kingmaker.Blueprints.Items.Weapons.BlueprintItemWeapon weapon){foreach(var label in new[]{weapon.Type.DefaultName,weapon.Type.TypeName}){string fallback=label?.Key;if(fallback!=null&&GameEnglish060[fallback]?.Type==JTokenType.String)return (string)GameEnglish060[fallback];}}
  throw new InvalidOperationException("060 localization donor has no actual English key: "+bp.name+" / "+key);
 }
 static void LocalizationInputs060(Harmony h){
  GameEnglish060=(JObject)JObject.Parse(File.ReadAllText(TestPaths.Game("Wrath_Data/StreamingAssets/Localization/enGB.json")))["strings"];
  PackageEnglish060=(JObject)JObject.Parse(File.ReadAllText(TestPaths.Outputs("package/Localization/enGB.json")))["strings"];
  LocaleExport060=true;
 }
 static void Localization060(BlueprintsCache c,Harmony h){
  bool current061=Main.GetType("CallOfTheWild.Wrath.Prototype.MimicCatalogue01461")!=null;
  if(current061){Register(c,"FeralHunter01461");Register(c,"TotemBonded01461");}
  var authored=(Dictionary<string,string>)AccessTools.Field(T("PsychicAssets"),"Strings").GetValue(null);
  var original=(JObject)JObject.Parse(File.ReadAllText(TestPaths.Outputs("package/Localization/enGB.json")))["strings"];
  var catalog=new SortedDictionary<string,string>(original.Properties().ToDictionary(p=>p.Name,p=>(string)p.Value),StringComparer.Ordinal);
  // Snapshot only inside the cache callback. Reflection and key resolution
  // occur afterwards, so export cannot load into a dictionary being enumerated.
  var snapshot=new List<SimpleBlueprint>();c.ForEachLoaded((id,bp)=>snapshot.Add(bp));var referenced=new HashSet<string>(StringComparer.Ordinal);
  foreach(var bp in snapshot)for(var type=bp.GetType();type!=null;type=type.BaseType)foreach(var field in type.GetFields(BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.DeclaredOnly))
   if(field.GetValue(bp) is LocalizedString value&&!string.IsNullOrEmpty(value.Key))referenced.Add(value.Key);
  foreach(string key in referenced)if(authored.TryGetValue(key,out var text)&&!catalog.ContainsKey(key))catalog.Add(key,text);
  Check(catalog.Where(p=>original.Property(p.Key)==null).All(p=>!p.Value.Contains("CotWWrathPsychic")),"060 exported author English rejects internal test blueprint-name labels");
  Check(original.Properties().All(p=>catalog[p.Name]==(string)p.Value),"060 QL export preserves every existing native language-pack key and exact English text");
  foreach(string token in new[]{"WarpedBrainFeature01458","HallucinogenicAuraHost01460","AgonizingWoundMenu01460","SynapticShockPending01460","MajorAmplificationsFeature01460","UndercastSurgeFeature01458"}){
   var bp=A<BlueprintScriptableObject>(c,token);Check(bp!=null,"060 actual localization graph includes "+token);
   foreach(string field in new[]{"m_DisplayName","m_Description"}){string key=((LocalizedString)AccessTools.Field(bp.GetType(),field).GetValue(bp)).Key;Check(catalog.ContainsKey(key)&&catalog[key]==authored[key],"060 actual published text is present in QL export: "+token+" / "+field);}
  }
  var export=new JArray(catalog.Select(p=>new JObject{["Key"]=p.Key,["ProcessTemplates"]=false,["enGB"]=p.Value,["zhCN"]=p.Value}));
  var exportPath=Path.Combine(Root(),current061?"work/batch01461/localization-catalog061.json":"work/repair01460/localization-catalog060.json");string bytes=export.ToString(Formatting.Indented)+"\n";
  if(!File.Exists(exportPath))File.WriteAllText(exportPath,bytes,new System.Text.UTF8Encoding(false));Check(File.ReadAllText(exportPath)==bytes,"060 independently repeated actual graph export is byte-identical");
  Check(export.Count>original.Count&&export.Select(r=>(string)r["Key"]).Distinct().Count()==export.Count,"060 complete QL array adds runtime fallback keys without duplicates");
  var ql=T("QlTexts01460");QlPath060=Path.Combine(Root(),(current061?"work/batch01461/ql-input-":"work/repair01460/ql-input-")+Guid.NewGuid().ToString("N"),"Localization-QL/CallOfTheWildWrathPrototypeLocalized.json");Directory.CreateDirectory(Path.GetDirectoryName(QlPath060));
  h.Patch(AccessTools.Method(ql,"CatalogPath"),prefix:new HarmonyMethod(typeof(MagusBatch01454Tests),nameof(QlPathInput060)));
  var oldPack=LocalizationManager.CurrentPack;var locale=AccessTools.Field(typeof(LocalizationManager),"s_CurrentLocale");var oldLocale=locale.GetValue(null);
  string key1="cotw-wrath.psychic.full.WarpedBrainFeature01458.name",key2="cotw-wrath.psychic.full.SynapticShockPending01460.name",key3="cotw-wrath.psychic.full.MajorAmplificationsFeature01460.name";
  var rows=new JArray(new JObject{["Key"]=key1,["ProcessTemplates"]=false,["enGB"]=catalog[key1],["zhCN"]="扭曲心智 \"测试\" {g|Encyclopedia:Will}意志{/g}\n2026-10-29"},new JObject{["Key"]=key2,["enGB"]=catalog[key2],["zhCN"]="等待源状态结束"},new JObject{["Key"]=key3,["enGB"]=catalog[key3],["zhCN"]="高等增幅"},new JObject{["Key"]="foreign.not.cotw",["enGB"]="Foreign",["zhCN"]="不得写入"});
  try{
   File.WriteAllText(QlPath060,rows.ToString());var pack=new LocalizationPack{Locale=Locale.zhCN};LocalizationManager.CurrentPack=pack;locale.SetValue(null,Locale.zhCN);pack.PutString(key2,"foreign translation");pack.PutString(key3,"");
   Call(T("PsychicAssets"),"ApplyQLLocalization");Check(pack.GetText(key1,false)==(string)rows[0]["zhCN"],"060 real optional QL reader and native pack preserve Chinese, quotes, newline, tags and date-shaped text");
   Check(pack.GetText(key2,false)=="foreign translation"&&pack.GetText(key3,false)=="","060 QL application preserves foreign translations and intentionally empty native keys");
   Check(!((System.Collections.IDictionary)AccessTools.Field(typeof(LocalizationPack),"m_Strings").GetValue(pack)).Contains("foreign.not.cotw"),"060 QL cannot overwrite an unrelated module key");
   pack.Locale=Locale.enGB;Call(T("PsychicAssets"),"ApplyQLLocalization");Check(pack.GetText(key1,false)==catalog[key1],"060 actual loaded-pack locale switches our applied translation back to English");
   pack.Locale=Locale.zhCN;rows[0]["zhCN"]="已编辑";File.WriteAllText(QlPath060,rows.ToString());Call(T("PsychicAssets"),"ApplyQLLocalization");Check(pack.GetText(key1,false)=="已编辑","060 explicit localization refresh reloads edited QL source without changing blueprint identities");
   foreach(string invalid in new[]{"{}","[{}]","[{\"Key\":\"x\",\"Key\":\"y\",\"enGB\":\"x\"}]",new JArray(rows[0],rows[0].DeepClone()).ToString(),"["}){File.WriteAllText(QlPath060,invalid);Call(T("PsychicAssets"),"ApplyQLLocalization");Check(pack.GetText(key1,false)=="已编辑","060 malformed/duplicate optional QL source is isolated without losing active text: "+invalid.Length);}
   File.WriteAllText(QlPath060,new JArray(new JObject{["Key"]=key1,["ProcessTemplates"]=false,["enGB"]="2026-10-29",["zhCN"]=JValue.CreateNull()}).ToString());Call(T("PsychicAssets"),"ApplyQLLocalization");Check(pack.GetText(key1,false)=="2026-10-29","060 null Chinese falls back to exact English rather than deserializing a date");
  }finally{LocalizationManager.CurrentPack=oldPack;locale.SetValue(null,oldLocale);h.Unpatch(AccessTools.Method(ql,"CatalogPath"),AccessTools.Method(typeof(MagusBatch01454Tests),nameof(QlPathInput060)));}
  PsychicExpansionTests.Inventory["qlCatalogPolicy061"]=current061;
  PsychicExpansionTests.Inventory["qlLocalization01460Export"]=new JObject{["entries"]=export.Count,["nativeEntries"]=original.Count,["actualPublishedKeys"]=referenced.Count,["path"]=exportPath,["actualNativePackReaderTested"]=true,["externalQLProgramRun"]=false};
 }
}

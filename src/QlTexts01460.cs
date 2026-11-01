using System;using System.Collections;using System.Collections.Generic;using System.IO;using System.Linq;using System.Runtime.CompilerServices;
using Kingmaker.Localization;using Kingmaker.Localization.Shared;using Newtonsoft.Json;using Newtonsoft.Json.Linq;
namespace CallOfTheWild.Wrath.Prototype{
 // Optional editable QL source, separate from Owlcat's native {strings:{}}
 // directory. Stable localization keys never become blueprint identities.
 internal static class QlTexts01460{
  internal const string FileName="CallOfTheWildWrathPrototypeLocalized.json";
  sealed class Row{internal string English,Chinese;}
  static Dictionary<string,Row> rows=new Dictionary<string,Row>(StringComparer.Ordinal);
  static bool loaded;
  static readonly ConditionalWeakTable<LocalizationPack,Dictionary<string,string>> Applied=new ConditionalWeakTable<LocalizationPack,Dictionary<string,string>>();
  internal static string CatalogPath(){var location=typeof(QlTexts01460).Assembly.Location;if(string.IsNullOrEmpty(location))return null;var folder=new DirectoryInfo(Path.GetDirectoryName(location));if(folder.Name.Equals("Assemblies",StringComparison.OrdinalIgnoreCase))folder=folder.Parent;return Path.Combine(folder.FullName,"Localization-QL",FileName);}
  static bool Reload(){
   loaded=true;var path=CatalogPath();if(path==null||!File.Exists(path)){rows=new Dictionary<string,Row>(StringComparer.Ordinal);return true;}
   try{
    string raw=File.ReadAllText(path);var scopes=new Stack<HashSet<string>>();
    using(var reader=new JsonTextReader(new StringReader(raw)){DateParseHandling=DateParseHandling.None})while(reader.Read()){
     if(reader.TokenType==JsonToken.StartObject)scopes.Push(new HashSet<string>(StringComparer.Ordinal));
     else if(reader.TokenType==JsonToken.EndObject)scopes.Pop();
     else if(reader.TokenType==JsonToken.PropertyName&&!scopes.Peek().Add((string)reader.Value))throw new JsonException("Duplicate QL property.");
    }
    JArray array;using(var reader=new JsonTextReader(new StringReader(raw)){DateParseHandling=DateParseHandling.None})array=JArray.Load(reader);
    var next=new Dictionary<string,Row>(StringComparer.Ordinal);
    foreach(var token in array){var item=token as JObject;if(item==null||item["Key"]?.Type!=JTokenType.String||item["enGB"]?.Type!=JTokenType.String||string.IsNullOrEmpty((string)item["Key"])||(item["zhCN"]!=null&&item["zhCN"].Type!=JTokenType.String&&item["zhCN"].Type!=JTokenType.Null)||(item["ProcessTemplates"]!=null&&item["ProcessTemplates"].Type!=JTokenType.Boolean))throw new JsonException("Invalid QL localization entry.");next.Add((string)item["Key"],new Row{English=(string)item["enGB"],Chinese=(string)item["zhCN"]});}
    rows=next;return true;
   }catch(Exception e){Console.Error.WriteLine("CotW optional QL localization rejected; original text retained: "+e.GetType().Name);return false;}
  }
  static string Value(Row row)=>LocalizationManager.CurrentPack?.Locale==Locale.zhCN?row.Chinese??row.English:row.English;
  internal static string Resolve(string key,string fallback){if(!loaded)Reload();return rows.TryGetValue(key,out var row)?Value(row):fallback;}
  internal static void Apply(IReadOnlyDictionary<string,string> authored){
   var pack=LocalizationManager.CurrentPack;if(pack==null||!Reload())return;
   var known=authored.ToDictionary(p=>p.Key,p=>p.Value,StringComparer.Ordinal);var path=CatalogPath();
   try{
    if(path!=null){var native=Path.Combine(Directory.GetParent(Path.GetDirectoryName(path)).FullName,"Localization","enGB.json");if(File.Exists(native))foreach(var p in ((JObject)JObject.Parse(File.ReadAllText(native))["strings"]).Properties())if(p.Value.Type==JTokenType.String&&!known.ContainsKey(p.Name))known.Add(p.Name,(string)p.Value);}
    var previous=Applied.GetOrCreateValue(pack);
    lock(PsychicAssets.Get(pack,"m_Lock"))foreach(var item in rows){
     if(!known.TryGetValue(item.Key,out var original))continue;
     var entries=(IDictionary)PsychicAssets.Get(pack,"m_Strings");string current=entries.Contains(item.Key)?pack.GetText(item.Key,false):null;
     // Preserve a foreign translation and an intentionally empty native key.
     if(current!=null&&current!=original&&current!=item.Value.English&&(!previous.TryGetValue(item.Key,out var before)||current!=before))continue;
     string translated=Value(item.Value);pack.PutString(item.Key,translated);previous[item.Key]=translated;
    }
   }catch(Exception e){Console.Error.WriteLine("CotW optional QL localization could not be applied: "+e.GetType().Name);}
  }
 }
}

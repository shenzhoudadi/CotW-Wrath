using System.Collections.Generic;
using Kingmaker.Blueprints;
namespace CallOfTheWild.Wrath.Prototype {
 internal static class PsychicRepairComponents {
  internal static void UniqueNames(BlueprintScriptableObject bp){
   Name(bp);var used=new HashSet<string>();
   foreach(var c in bp.ComponentsArray){string stem=c.name;int index=0;while(!used.Add(c.name))c.name=stem+"CotW027"+(++index);}
  }
  internal static void Name(BlueprintScriptableObject bp){
   var used=new HashSet<string>();foreach(var c in bp.ComponentsArray)if(!string.IsNullOrEmpty(c.name))used.Add(c.name);
   foreach(var c in bp.ComponentsArray)if(string.IsNullOrEmpty(c.name)){string stem="CotW025"+c.GetType().Name;int index=0;string name=stem;while(!used.Add(name))name=stem+(++index);c.name=name;}
  }
 }
}

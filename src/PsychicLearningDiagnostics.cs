using System;
using System.Linq;
using System.Runtime.CompilerServices;
using HarmonyLib;
using Kingmaker.Blueprints;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Class.LevelUp.Actions;

namespace CallOfTheWild.Wrath.Prototype
{
    // Observes existing learning hooks only. Never adds, deletes or serializes spells.
    internal static class PsychicLearningDiagnostics
    {
        private static readonly string[] Watched = {
            "bd81a3931aa285a4f9844585b5d97e51", "c7104f7526c4c524f91474614054547e",
            "7658b74f626c56a49939d9c20580885e", "d2aeac47450c76347aebbc02e4f463e0"
        };
        private static bool reportedHooks;
        internal static string KnownState(Spellbook book, BlueprintAbility spell)
        {
            var known=book.GetAllKnownSpells().ToArray();
            var ids=Watched.Concat(new[]{PsychicAssets.Id("BurstOfAdrenaline").ToString(),PsychicAssets.Id("BurstOfInsight").ToString(),spell?.AssetGuid.ToString()}).Where(s=>s!=null).Distinct();
            return string.Join("; ",ids.Select(id=>id+"="+string.Join(",",known.Where(a=>a.Blueprint.AssetGuid.ToString()==id)
                .Select(a=>"circle:"+a.SpellLevel+"/temporary:"+a.IsTemporary+"/book:"+RuntimeHelpers.GetHashCode(a.Spellbook)).DefaultIfEmpty("absent"))))+"; total="+known.Length;
        }
        internal static void Trace(Spellbook book,string operation,BlueprintAbility spell=null,Exception error=null)
        {
            if(!PsychicCasting.IsPsychic(book?.Blueprint)) return;
            try {
                if(!reportedHooks) {
                    reportedHooks=true;
                    foreach(var target in new[]{AccessTools.Method(typeof(Spellbook),"AddKnown"),AccessTools.Method(typeof(Spellbook),"RemoveSpell"),AccessTools.Method(typeof(SelectSpell),"Apply"),AccessTools.Method(typeof(SelectSpell),"Check")}) {
                        var patches=Harmony.GetPatchInfo(target);
                        if(patches==null)continue;
                        var descriptions=patches.Prefixes.Concat(patches.Postfixes).Concat(patches.Transpilers).Concat(patches.Finalizers)
                            .Select(p=>p.owner+":"+p.PatchMethod.DeclaringType.FullName+"."+p.PatchMethod.Name);
                        ModEntry.LogSpellDiagnostic("[Psychic learning] hooks "+target.DeclaringType.Name+"."+target.Name+"="+string.Join(",",descriptions));
                    }
                }
                ModEntry.LogSpellDiagnostic("[Psychic learning] "+operation+"; owner="+book.Owner?.Unit?.UniqueId+
                    "; ownerInstance="+(book.Owner==null?0:RuntimeHelpers.GetHashCode(book.Owner))+"; book="+book.Blueprint.AssetGuid+
                    "; bookInstance="+RuntimeHelpers.GetHashCode(book)+"; spell="+spell?.name+"/"+spell?.AssetGuid+"; "+KnownState(book,spell)+
                    (error==null?"":"; error="+error)+(operation=="RemoveSpell"?"; caller="+new System.Diagnostics.StackTrace(1,false):""));
            } catch(Exception e) {ModEntry.LogSpellDiagnostic("[Psychic learning] diagnostic unavailable: "+e);}
        }
        internal static void Selection(SelectSpell action,UnitDescriptor unit,string phase,Exception error=null)
        {
            if(!PsychicCasting.IsPsychic(action?.Spellbook))return;
            try {
                var book=unit?.GetSpellbook(action.Spellbook);
                if(book==null)ModEntry.LogSpellDiagnostic("[Psychic learning] SelectSpell "+phase+"; no existing book; spell="+action.Spell?.AssetGuid+"; circle="+action.SpellLevel+(error==null?"":"; error="+error));
                else Trace(book,"SelectSpell "+phase+" circle="+action.SpellLevel+" slot="+action.SlotIndex,action.Spell,error);
            } catch(Exception e) {ModEntry.LogSpellDiagnostic("[Psychic learning] selection diagnostic unavailable: "+e);}
        }
    }
}

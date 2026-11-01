using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Class.LevelUp;
using Kingmaker.UnitLogic.Class.LevelUp.Actions;
using CallOfTheWild.Wrath.Prototype;

internal static class Psychic095Tests
{
    static int count;
    internal static int Assertions=>count;
    static void Check(bool ok,string why){if(!ok)throw new Exception(why);count++;}
    static void Set(object o,string n,object v)=>AccessTools.Field(o.GetType(),n).SetValue(o,v);
    static Type undercast=typeof(PsychicUndercastFamily).Assembly.GetType("CallOfTheWild.Wrath.Prototype.PsychicUndercasting");
    static BlueprintAbility Asset(BlueprintsCache cache,string token)=>(BlueprintAbility)cache.Load((BlueprintGuid)AccessTools.Method(typeof(PsychicUndercastFamily).Assembly.GetType("CallOfTheWild.Wrath.Prototype.PsychicAssets"),"Id").Invoke(null,new object[]{token}));
    static Psychic070Tests.Caster Make(BlueprintsCache cache,BlueprintSpellbook blueprint=null)
    {
        var f=new Psychic070Tests.Caster(cache);if(blueprint!=null)Set(f.Book,"Blueprint",blueprint);
        Set(f.Book,"m_KnownSpells",Enumerable.Range(0,10).Select(i=>new List<AbilityData>()).ToArray());
        Set(f.Book,"m_SpecialSpells",Enumerable.Range(0,10).Select(i=>new List<AbilityData>()).ToArray());
        Set(f.Book,"m_CustomSpells",Enumerable.Range(0,10).Select(i=>new List<AbilityData>()).ToArray());
        Set(f.Book,"m_KnownSpellLevels",new Dictionary<BlueprintAbility,List<int>>());Set(f.Book,"m_SpecialLists",new List<BlueprintSpellList>());
        Set(f.Book,"m_SpellConversionLists",new Dictionary<string,BlueprintAbility[]>());
        Set(f.Book,"OppositionSchools",new List<SpellSchool>());Set(f.Book,"ExOppositionSchools",new List<SpellSchool>());Set(f.Book,"IgnoreOppositionSchools",new List<SpellSchool>());
        Set(f.Descriptor,"m_Spellbooks",new Dictionary<BlueprintSpellbook,Spellbook>{{f.Book.Blueprint,f.Book}});
        Set(f.Descriptor.Progression,"m_MythicLevel",(int?)0);return f;
    }
    static LevelUpState State(Psychic070Tests.Caster f,int circle,int slots)
    {
        var state=(LevelUpState)System.Runtime.Serialization.FormatterServices.GetUninitializedObject(typeof(LevelUpState));
        Set(state,"SpellSelections",new List<SpellSelectionData>());Set(state,"<SelectedClass>k__BackingField",f.Book.Blueprint.CharacterClass);
        var selection=state.DemandSpellSelection(f.Book.Blueprint,f.Book.Blueprint.SpellList);
        for(int i=0;i<11;i++)selection.SetLevelSpells(i,i==circle?slots:0);
        // Original ApplySpellbook initializes base slots before this production postfix.
        AccessTools.Method(undercast.GetNestedType("ExistingReplacementsPatch",BindingFlags.NonPublic),"Postfix").Invoke(null,new object[]{state,f.Descriptor});
        return state;
    }
    static void Copy(Psychic070Tests.Caster original,Psychic070Tests.Caster target)=>AccessTools.Method(typeof(UnitHelper),"CopySpellbook").Invoke(null,new object[]{original.Unit,target.Unit});
    static SelectSpell Choice(Psychic070Tests.Caster f,int circle,BlueprintAbility spell,int slot)=>new SelectSpell(f.Book.Blueprint,f.Book.Blueprint.SpellList,circle,spell,slot);
    static string Snapshot(Spellbook book)=>string.Join(";",book.GetAllKnownSpells().Select(a=>a.Blueprint.AssetGuid+":"+a.SpellLevel+":"+a.IsTemporary));
    internal static int Run(BlueprintsCache cache)
    {
        var three=Asset(cache,"MindThrust3");var two=Asset(cache,"MindThrust2");var one=Asset(cache,"MindThrust1");
        var animal=PsychicExpansionTests.NativeCase<BlueprintAbility>("41bab342089c0254ca222eb918e98cd4");cache.AddCachedBlueprint(animal.AssetGuid,animal);
        var fear=(BlueprintAbility)cache.Load(BlueprintGuid.Parse("bd81a3931aa285a4f9844585b5d97e51"));
        var original=Make(cache);original.Book.AddKnown(3,three,false);var originalSnapshot=Snapshot(original.Book);
        var broken=Make(cache);Copy(original,broken);
        Check(!broken.Book.GetKnownSpell(two).IsTemporary && original.Book.GetKnownSpell(two).IsTemporary,"Native pre-fix copy promotes free Mind Thrust II only in the preview");
        var brokenState=State(broken,2,1);var originalState=State(original,2,1);
        var phantom=Choice(broken,2,animal,1);
        Check(phantom.Check(brokenState,broken.Descriptor) && !phantom.Check(originalState,original.Descriptor),"Reproduces reported failure: preview offers Hold Animal at slot 1, native commit Check rejects it");
        phantom.Apply(brokenState,broken.Descriptor);
        Check(broken.Book.IsKnown(animal) && !original.Book.IsKnown(animal),"Rejected choice exists in preview only, not the real book");
        Console.WriteLine("PASS: native pre-fix CopySpellbook reproduces phantom compensation and rejected commit, no Unity.");
        var h=new Harmony("CotW.Psychic095.CopyFix");
        try {
            h.CreateClassProcessor(undercast.GetNestedType("CopiedBookPatch",BindingFlags.NonPublic)).Patch();
            foreach(var token in new[]{"MindThrust2","MindThrust3","MindThrust4","MindThrust5","MindThrust6","MentalBarrier2","MentalBarrier5","ThoughtShield2","ThoughtShield3","PsychicCrush2","PsychicCrush3","PsychicCrush4","PsychicCrush5"}) {
                var anchor=Asset(cache,token);var family=anchor.GetComponent<PsychicUndercastFamily>();var source=Make(cache);source.Book.AddKnown(family.Circle,anchor,false);
                var before=Snapshot(source.Book);var preview=Make(cache);Copy(source,preview);
                Check(Snapshot(preview.Book)==before && Snapshot(source.Book)==before,"Copied family retains temporary/permanent status and leaves original untouched: "+token);
                foreach(var lower in family.LowerSpells)Check(preview.Book.GetKnownSpell(lower.Get()).IsTemporary,"No paid lower version invented: "+lower.Guid);
                var previewState=State(preview,2,1);var committedState=State(source,2,1);
                Check(previewState.SpellSelections[0].LevelCount.Select(x=>x.SpellSelections.Length).SequenceEqual(committedState.SpellSelections[0].LevelCount.Select(x=>x.SpellSelections.Length)),"Preview and commit have identical slot counts: "+token);
                Check(previewState.SpellSelections[0].LevelCount.Sum(x=>x.SpellSelections.Length)==1,"No repeat compensation: "+token);
                var choose=Choice(preview,2,animal,0);Check(choose.Check(previewState,preview.Descriptor) && choose.Check(committedState,source.Descriptor),"Normal learning passes both native checks after undercasting: "+token);
                choose.Apply(previewState,preview.Descriptor);choose.Apply(committedState,source.Descriptor);
                Check(source.Book.IsKnown(animal) && Kingmaker.UI.Common.UIUtilityUnit.GetKnownSpellsForLevel(2,source.Book).Any(a=>a.Blueprint==animal),"Committed spell reaches native book/UI data: "+token);
                var next=Make(cache);Copy(source,next);var nextState=State(next,2,1);
                Check(next.Book.IsKnown(animal) && nextState.SpellSelections[0].LevelCount.Sum(x=>x.SpellSelections.Length)==1,"Next preview preserves normal spell without refunding old lower versions: "+token);
            }
            var crushLow=Asset(cache,"PsychicCrush1");var crushHigh=Asset(cache,"PsychicCrush5");
            var crushReal=Make(cache);crushReal.Book.AddKnown(5,crushLow,false);var crushPreview=Make(cache);Copy(crushReal,crushPreview);
            var crushChoice=Choice(crushReal,9,crushHigh,0);var crushReplacement=Asset(cache,"BurstOfForce");
            foreach(var pair in new[]{Tuple.Create(crushReal,State(crushReal,9,1)),Tuple.Create(crushPreview,State(crushPreview,9,1))}) {
                Check(crushChoice.Check(pair.Item2,pair.Item1.Descriptor),"Crush V is selectable in preview and commit");crushChoice.Apply(pair.Item2,pair.Item1.Descriptor);
                var replace=Choice(crushReal,5,crushReplacement,0);
                Check(replace.Check(pair.Item2,pair.Item1.Descriptor),"Paid Crush I gets a current-upgrade fifth-circle replacement");replace.Apply(pair.Item2,pair.Item1.Descriptor);
                Check(pair.Item1.Book.GetKnownSpell(crushLow).IsTemporary && pair.Item1.Book.IsKnown(crushReplacement),"Crush compensation preserves free I and the actual replacement");
                foreach(int circle in new[]{6,7,8})Check(pair.Item2.SpellSelections[0].LevelCount[circle].SpellSelections.Length==0,"Unpaid intermediate Crush versions never grant replacement slots");
            }
            Check(Snapshot(crushPreview.Book)==Snapshot(crushReal.Book),"Crush family preview and commit remain identical");
            var crushNext=Make(cache);Copy(crushReal,crushNext);
            Check(State(crushNext,9,0).SpellSelections[0].LevelCount.Sum(x=>x.SpellSelections.Length)==0,"Crush compensation is not repeated next upgrade");
            // Legitimate same-upgrade compensation is still offered and committed.
            var paid=Make(cache);paid.Book.AddKnown(1,one,false);var paidPreview=Make(cache);Copy(paid,paidPreview);
            var ps=State(paidPreview,2,1);var cs=State(paid,2,1);var higher=Choice(paid,2,two,0);
            foreach(var pair in new[]{Tuple.Create(paidPreview,ps),Tuple.Create(paid,cs)}) {
                Check(higher.Check(pair.Item2,pair.Item1.Descriptor),"High version remains selectable");higher.Apply(pair.Item2,pair.Item1.Descriptor);
                var replacement=Choice(paid,1,fear,0);Check(replacement.Check(pair.Item2,pair.Item1.Descriptor),"Paid lower version grants a valid same-upgrade replacement");replacement.Apply(pair.Item2,pair.Item1.Descriptor);
            }
            Check(paid.Book.IsKnown(fear) && paid.Book.GetKnownSpell(one).IsTemporary && Snapshot(paidPreview.Book)==Snapshot(paid.Book),"Actual I to II upgrade and Cause Fear replacement match preview and commit");
            var further=Make(cache);Copy(paid,further);var fs=State(further,3,1);var final=State(paid,3,1);var third=Choice(paid,3,three,0);
            foreach(var pair in new[]{Tuple.Create(further,fs),Tuple.Create(paid,final)}) {
                third.Apply(pair.Item2,pair.Item1.Descriptor);var replacement=Choice(paid,2,animal,0);
                Check(replacement.Check(pair.Item2,pair.Item1.Descriptor),"II to III refunds only paid II in both preview and commit");replacement.Apply(pair.Item2,pair.Item1.Descriptor);
                Check(pair.Item2.SpellSelections[0].LevelCount[1].SpellSelections.Length==0,"Already-refunded I earns no duplicate slot");
            }
            Check(paid.Book.IsKnown(animal) && Snapshot(further.Book)==Snapshot(paid.Book),"Sequential I to II to III upgrades retain both ordinary replacements");
            Check(Snapshot(original.Book)==originalSnapshot,"Negative-control source is unchanged by preview work");
            var legacy=Make(cache);legacy.Book.AddKnown(1,one,false);legacy.Book.AddKnown(3,three,false);var legacyCopy=Make(cache);Copy(legacy,legacyCopy);
            Check(!legacyCopy.Book.GetKnownSpell(one).IsTemporary,"Genuinely paid lower version in an old save remains paid when copied");
            var legacyPreviewState=State(legacyCopy,3,0);var legacyRealState=State(legacy,3,0);
            Check(legacyPreviewState.SpellSelections[0].LevelCount[1].SpellSelections.Length==1 && legacyRealState.SpellSelections[0].LevelCount[1].SpellSelections.Length==1,"Old save paid lower version is compensated once on both paths");
            var ordinary=(BlueprintSpellbook)cache.Load(BlueprintGuid.Parse("b3db3766a4b605040b366265e2af0e50"));var other=Make(cache,ordinary);other.Book.AddKnownTemporary(1,one);var otherCopy=Make(cache,ordinary);Copy(other,otherCopy);
            Check(!otherCopy.Book.GetKnownSpell(one).IsTemporary,"Other spellbook retains native copy behavior");
        } finally {h.UnpatchAll(h.Id);}
        Console.WriteLine("PASS: "+count+" 0.9.5 preview-copy/slot-check/replay regressions; actual managed CopySpellbook/SelectSpell, seeded base slots; not complete game level-up.");
        return count;
    }
}

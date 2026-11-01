using System;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using Newtonsoft.Json.Linq;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Buffs;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Parts;
using Kingmaker.UnitLogic.FactLogic;

internal static partial class MagusBatch01454Tests
{
    static BlueprintBuff TemporaryBuff064(BlueprintsCache c,string token,BlueprintCharacterClass cls,BlueprintAbility spell,int circle)
    {
        var grant=new AddSpellKnownTemporary{name=token+"Grant",Level=circle,OnlySpontaneous=true};
        Set(grant,"m_CharacterClass",BlueprintReferenceBase.CreateTyped<BlueprintCharacterClassReference>(cls));
        Set(grant,"m_Spell",BlueprintReferenceBase.CreateTyped<BlueprintAbilityReference>(spell));
        var buff=new BlueprintBuff{AssetGuid=Id(token),name=token,Stacking=StackingType.Stack,ComponentsArray=new BlueprintComponent[]{grant}};
        buff.OnEnable();c.AddCachedBlueprint(buff.AssetGuid,buff);return buff;
    }
    static Spellbook NativePsychicBook064(BlueprintsCache c,UnitEntityData owner,int level)
    {
        var cls=N<BlueprintCharacterClass>(c,Psychic058);var book=owner.Descriptor.DemandSpellbook(cls);
        Check(book.RawBaseLevel==0,"064 native demand starts from zero, so the test must actually advance the book");
        for(int i=0;i<level;i++)book.AddLevelFromClass(cls);
        Check(book.RawBaseLevel==level&&book.MaxSpellLevel==(level==1?1:level/2),"064 native Psychic book reaches independently expected maximum circle at level "+level);
        return book;
    }
    static void Temporary064(BlueprintsCache parent,Harmony h)
    {
        EquipmentPatches(h);
        var bridge=Main.GetType("CallOfTheWild.Wrath.Prototype.TemporaryKnowledge01464");
        foreach(var type in T("PsychicUndercasting").GetNestedTypes(BindingFlags.NonPublic|BindingFlags.Public)
            .Concat(bridge?.GetNestedTypes(BindingFlags.NonPublic|BindingFlags.Public)??Array.Empty<Type>())
            .Where(t=>t.GetCustomAttributes(typeof(HarmonyPatch),false).Any()))h.CreateClassProcessor(type).Patch();
        var c=PsychicCache057(parent);var cls=N<BlueprintCharacterClass>(c,Psychic058);
        var owner=EquipmentUnit(c,10,Psychic058);var book=NativePsychicBook064(c,owner,10);
        var other=EquipmentUnit(c,10,Psychic058);var otherBook=NativePsychicBook064(c,other,10);
        var high=A<BlueprintAbility>(c,"MindThrust3");var low=A<BlueprintAbility>(c,"MindThrust1");
        Check(high!=null&&low!=null&&!book.IsKnown(high)&&!book.IsKnown(low),"064 native shared test begins with no paid or temporary family knowledge");
        var bp=TemporaryBuff064(c,"NativeTemporaryFamily064",cls,high,3);
        var first=owner.Buffs.AddBuff(bp,owner,null);
        Call(T("PsychicUndercasting"),"Reconcile",book);
        Check(book.IsKnown(high)&&book.GetKnownSpell(high).IsTemporary,"064 native source ledger grants the actual high spell as temporary knowledge");
        Check(book.IsKnown(low)&&book.GetKnownSpell(low).IsTemporary,"064 temporary high-family source grants real native undercast knowledge");
        Check(!otherBook.IsKnown(high)&&!otherBook.IsKnown(low),"064 temporary source never grants knowledge to another actor");
        var second=owner.Buffs.AddBuff(bp,owner,null);
        var foreignBlueprint=TemporaryBuff064(c,"OtherProviderTemporaryFamily064",cls,high,3);
        var foreignSource=owner.Buffs.AddBuff(foreignBlueprint,owner,null);
        Check(book.GetKnownSpells(3).Count(a=>a.Blueprint==high)==1,"064 two real source facts share exactly one native high ability");
        first.Remove();
        Check(book.IsKnown(high)&&book.IsKnown(low),"064 removing one real source preserves the other source and its lower family");
        // Native level-up preview copying calls AddKnown, which would otherwise
        // promote these entries. Exercise that exact production caller.
        var preview=EquipmentUnit(c,10,Psychic058);var previewBook=NativePsychicBook064(c,preview,10);
        var originalKnowledge=book.GetAllKnownSpells().ToDictionary(a=>a,a=>a.IsTemporary);
        AccessTools.Method(typeof(UnitHelper),"CopySpellbook").Invoke(null,new object[]{owner,preview});
        Check(previewBook.IsKnown(high)&&previewBook.GetKnownSpell(high).IsTemporary&&previewBook.IsKnown(low)&&previewBook.GetKnownSpell(low).IsTemporary,
            "064 actual native CopySpellbook retains temporary family provenance in the preview");
        Check(book.GetAllKnownSpells().Count()==originalKnowledge.Count&&originalKnowledge.All(pair=>pair.Key.IsTemporary==pair.Value),"064 copying a preview preserves every original permanent and temporary knowledge flag");
        var nextPreview=EquipmentUnit(c,10,Psychic058);var nextBook=NativePsychicBook064(c,nextPreview,10);
        AccessTools.Method(typeof(UnitHelper),"CopySpellbook").Invoke(null,new object[]{preview,nextPreview});
        Check(nextBook.IsKnown(high)&&nextBook.GetKnownSpell(high).IsTemporary&&nextBook.IsKnown(low)&&nextBook.GetKnownSpell(low).IsTemporary,
            "064 actual repeated native CopySpellbook preserves provenance across two preview generations");
        var borrowed=nextBook.GetKnownSpell(high);nextBook.RemoveSpell(high);var paidAgain=nextBook.AddKnown(3,high,false);
        Check(!ReferenceEquals(borrowed,paidAgain)&&borrowed.Equals(paidAgain)&&!paidAgain.IsTemporary,"064 native equal-valued replacement is a distinct permanently relearned family spell");
        Call(T("PsychicUndercasting"),"Reconcile",nextBook);
        Check(nextBook.IsKnown(high)&&!nextBook.GetKnownSpell(high).IsTemporary,"064 stale preview receipt never owns an equal-valued newly paid family spell");
        var paidPreview=EquipmentUnit(c,10,Psychic058);var paidBook=NativePsychicBook064(c,paidPreview,10);
        AccessTools.Method(typeof(UnitHelper),"CopySpellbook").Invoke(null,new object[]{nextPreview,paidPreview});
        Check(paidBook.IsKnown(high)&&!paidBook.GetKnownSpell(high).IsTemporary,"064 remove and permanently relearn family then actual copy preserves paid identity");
        second.Remove();
        Check(book.IsKnown(high)&&book.GetKnownSpell(high).IsTemporary&&book.IsKnown(low),"064 removing both first-provider sources preserves the independent native source and its lower family");
        foreignSource.Remove();
        Check(!book.IsKnown(high)&&!book.IsKnown(low),"064 last native source removal revokes its high spell and derived undercast family");
        Check(previewBook.IsKnown(high)&&previewBook.GetKnownSpell(high).IsTemporary,"064 independent preview snapshot remains temporary until discarded");
        var promoted=owner.Buffs.AddBuff(bp,owner,null);
        book.AddKnown(3,high,false);
        Check(!book.GetKnownSpell(high).IsTemporary,"064 actual native AddKnown promotes the source-owned high spell to permanent knowledge");
        promoted.Remove();
        Check(book.IsKnown(high)&&!book.GetKnownSpell(high).IsTemporary&&book.IsKnown(low)&&book.GetKnownSpell(low).IsTemporary,
            "064 revoking a source preserves paid promotion and its free native lower family");
        book.RemoveSpell(high);
        Check(!book.IsKnown(high)&&!book.IsKnown(low),"064 removing paid high knowledge finally removes its unsupported lower family");
        var ordinary=N<BlueprintAbility>(c,"c7104f7526c4c524f91474614054547e");
        var ordinaryBuff=TemporaryBuff064(c,"NativeTemporaryOrdinary064",cls,ordinary,2);
        var ordinarySource=owner.Buffs.AddBuff(ordinaryBuff,owner,null);
        Check(book.GetKnownSpell(ordinary).IsTemporary,"064 native non-family temporary spell uses the same source ledger");
        var ordinaryPreview=EquipmentUnit(c,10,Psychic058);var ordinaryPreviewBook=NativePsychicBook064(c,ordinaryPreview,10);
        AccessTools.Method(typeof(UnitHelper),"CopySpellbook").Invoke(null,new object[]{owner,ordinaryPreview});
        Check(ordinaryPreviewBook.GetKnownSpell(ordinary).IsTemporary,"064 actual native CopySpellbook also retains temporary provenance for a non-family spell");
        var oldOrdinary=ordinaryPreviewBook.GetKnownSpell(ordinary);ordinaryPreviewBook.RemoveSpell(ordinary);var paidOrdinary=ordinaryPreviewBook.AddKnown(2,ordinary,false);
        Check(!ReferenceEquals(oldOrdinary,paidOrdinary)&&oldOrdinary.Equals(paidOrdinary)&&!paidOrdinary.IsTemporary,"064 non-family paid replacement has native value equality but independent reference ownership");
        var ordinaryNext=EquipmentUnit(c,10,Psychic058);var ordinaryNextBook=NativePsychicBook064(c,ordinaryNext,10);
        AccessTools.Method(typeof(UnitHelper),"CopySpellbook").Invoke(null,new object[]{ordinaryPreview,ordinaryNext});
        Check(ordinaryNextBook.IsKnown(ordinary)&&!ordinaryNextBook.GetKnownSpell(ordinary).IsTemporary,"064 remove and permanently relearn ordinary spell then actual copy cannot revive an expired source receipt");
        var part=owner.Get<UnitPartTemporarySpellsKnown>();
        var entries=(System.Collections.IEnumerable)AccessTools.Field(typeof(UnitPartTemporarySpellsKnown),"m_Entries").GetValue(part);
        foreach(var entry in entries)AccessTools.Property(entry.GetType(),"Component").SetValue(entry,null,null);
        Call(T("PsychicUndercasting"),"Reconcile",book);
        Check(book.GetKnownSpell(ordinary).IsTemporary,"064 book reconciliation before native component reconstruction retains its serialized source ledger");
        AccessTools.Method(typeof(UnitPartTemporarySpellsKnown),"OnPostLoad").Invoke(part,null);
        Check(book.GetKnownSpell(ordinary).IsTemporary,"064 actual native source-part PostLoad preserves temporary non-family knowledge");
        ordinarySource.Remove();Check(!book.IsKnown(ordinary),"064 native source removal revokes only its non-family temporary entry");
        foreach(var root in new[]{high,ordinary}){
            int circle=root==high?3:2;var grant=root==high?bp:ordinaryBuff;var source=owner.Buffs.AddBuff(grant,owner,null);var borrowedRoot=book.GetKnownSpell(root);
            book.RemoveSpell(root);var permanentRoot=book.AddKnown(circle,root,false);
            Check(!ReferenceEquals(borrowedRoot,permanentRoot)&&borrowedRoot.Equals(permanentRoot)&&!permanentRoot.IsTemporary,"064 active native source may retain an old equal-valued object after permanent relearning: "+circle);
            source.Remove();
            Check(book.IsKnown(root)&&ReferenceEquals(book.GetKnownSpell(root),permanentRoot)&&!permanentRoot.IsTemporary,"064 removing stale native source cannot delete a permanently relearned replacement object: "+circle);
            book.RemoveSpell(root);
        }
        var unrelated=book.AddKnownTemporary(2,ordinary);
        Call(T("PsychicUndercasting"),"Reconcile",book);
        Check(book.GetKnownSpell(ordinary)==unrelated&&unrelated.IsTemporary,"064 shared reconciliation leaves an unrelated native non-family temporary grant untouched");
        book.RemoveTemporarySpell(unrelated);
        PsychicExpansionTests.Inventory["temporaryKnowledge01464Contracts"]=new JObject{
            ["passed"]=true,["assertions"]=checks,["coverage"]=new JArray(Coverage),["gameLaunchTested"]=false,
            ["boundary"]="Actual native spellbook, AddSpellKnownTemporary source part, buff add/remove, paid promotion, source-part PostLoad and CopySpellbook; full game save/scene transport remains separate."};
    }
}

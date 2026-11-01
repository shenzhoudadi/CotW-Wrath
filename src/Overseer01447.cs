// Original CotW Classes/Shaman.cs, createOverseer (MIT).
using System;
using System.Linq;
using HarmonyLib;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Prerequisites;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.FactLogic;
using static CallOfTheWild.Wrath.Prototype.PsychicAssets;
using static CallOfTheWild.Wrath.Prototype.ArchetypeBatch01445;

namespace CallOfTheWild.Wrath.Prototype
{
    internal static class Overseer01447
    {
        internal const string ModuleName="Shaman archetype: Overseer",RootToken="OverseerArchetype01447",ClassId="145f1d3d360a7ad48bd95d392c81b38e",FeatureToken="OverseerControllingMagic01447";
        internal const string Description="While all shamans use their connection to the spirits of the world to draw upon otherworldly magic powers, some shamans have a tradition in which they use the power of patron spirits to directly control their enemies. Such overseers may assume roles as religious leaders and protectors of their tribes, turning foes into short-term allies for the tribe’s greater good. Other overseers become tyrants who enforce their will upon the weak for personal gain. In combat, an overseer manages the battlefield by debilitating foes using her hexes and specialized spells.";
        internal const string MagicText="The overseer adds the following spells to the list of spells she can cast using spirit magic: hypnotism (1st), hideous laughter (2nd), hold person (3rd), crushing despair (4th), feeblemind (5th), serenity (6th), power word blind (7th), irresistible dance (8th), and dominate monster (9th). This ability replaces the spirit magic spells gained from the shaman’s spirit.";
        internal static readonly string[] NativeSpells={"88367310478c10b47903463c5d0152b0","fd4d9fd7f87575d47aafe2a64a6e2d8d","c7104f7526c4c524f91474614054547e","4baf4109145de4345861fe0f2209d903","444eed6e26f773a40ab6e4d160c67faa","d316d3d94d20c674db2c24d7de96f6a7","261e1788bfc5ac1419eec68b1d485dbc","","3c17035ec4717674cae2e841a190e757"};
        // These are the actual Wrath primary-spirit lists. Wandering spirits do
        // not grant a second list. Domains and other special lists stay intact.
        internal static string[] SpiritLists;
        internal static void Register(BlueprintsCache cache) {
            var f=new PsychicAssets(cache);var cls=f.Native<BlueprintCharacterClass>(ClassId);if(Existing(cache,RootToken,cls))return;
            var spirit=f.Native<Kingmaker.Blueprints.Classes.Selection.BlueprintFeatureSelection>("00c8c566d1825dd4a871250f35285982");
            var lists=spirit.AllFeatures.OfType<BlueprintProgression>().Concat(new[]{f.Native<BlueprintProgression>("557cd05f067b44e8a916d6324c0c6c42")}).SelectMany(p=>p.LevelEntries).SelectMany(e=>e.Features).SelectMany(x=>x.GetComponents<AddSpecialSpellList>()).Where(c=>c.CharacterClass==cls).Select(c=>c.SpellList).Distinct().ToArray();
            if(lists.Length<9)throw new InvalidOperationException("01447 native primary-spirit special-list anchors missing.");
            var spells=NativeSpells.Select((id,i)=>i==7?cache.Load(Id("IrresistibleDance")) as BlueprintAbility:f.Native<BlueprintAbility>(id)).ToArray();if(spells.Any(s=>s==null))throw new InvalidOperationException("01447 Overseer requires the existing Irresistible Dance spell.");
            // CotW uses an independent Hold Person duplicate at spell level3.
            spells[2]=f.Clone(spells[2],"OverseerHoldPerson01447","Hold Person","");Set(spells[2],"m_DisplayName",Get(f.Native<BlueprintAbility>(NativeSpells[2]),"m_DisplayName"));Set(spells[2],"m_Description",Get(f.Native<BlueprintAbility>(NativeSpells[2]),"m_Description"));spells[2].ComponentsArray=spells[2].ComponentsArray.Where(c=>!(c is SpellListComponent)).ToArray();
            var list=SpellBatch01447.List(f,"OverseerSpellList01447",level=>level==0?Array.Empty<BlueprintAbility>():new[]{spells[level-1]});
            var special=new AddSpecialSpellList();Set(special,"m_CharacterClass",Ref<BlueprintCharacterClassReference>(cls));Set(special,"m_SpellList",Ref<BlueprintSpellListReference>(list));
            var feature=f.Feature(FeatureToken,"Controlling Magic",MagicText,new OverseerSpiritLists01447{OriginalLists=lists.Select(Ref<BlueprintSpellListReference>).ToArray(),Class=Ref<BlueprintCharacterClassReference>(cls)},special);SpellBatch01447.Icon(feature,spells[8]);
            var root=SpellBatch01447.Archetype(f,cls,RootToken,"Overseer",Description,new[]{Entry(1,feature)},Array.Empty<LevelEntry>());
            // Wrath encodes these spirit-magic replacements through conditional
            // list features rather than a shared removable Spirit Magic fact.
            root.ComponentsArray=new[]{"7939ff57cc2ce104693ea2f5afa595b6","556590a43467a27459ac1a80324c9f9f","a6ab54dbe2cc9314a8ad62863fbc64c4"}.Select(id=>{var no=new PrerequisiteNoArchetype();Set(no,"m_CharacterClass",Ref<BlueprintCharacterClassReference>(cls));Set(no,"m_Archetype",Ref<BlueprintArchetypeReference>(f.Native<BlueprintArchetype>(id)));return (BlueprintComponent)no;}).ToArray();
            SpellBatch01447.PublishArchetype(cache,f,cls,root,new[]{feature});SpiritLists=lists.Select(l=>l.AssetGuid.ToString()).ToArray();
        }
        internal static bool Suppress(AddSpecialSpellList component) {
            if(component.CharacterClass?.AssetGuid!=BlueprintGuid.Parse(ClassId))return false;
            var root=ResourcesLibrary.BlueprintsCache.Load(Id(RootToken)) as BlueprintArchetype;
            var owner=(Kingmaker.EntitySystem.Entities.UnitEntityData)AccessTools.Property(typeof(AddSpecialSpellList),"Owner").GetValue(component,null);
            return root!=null&&owner.Progression.IsArchetype(root)&&SpiritLists?.Contains(component.SpellList?.AssetGuid.ToString())==true;
        }
    }
    [TypeId("a287471a093a41a18a9c59bd13aa0147")]
    public sealed class OverseerSpiritLists01447 : UnitFactComponentDelegate
    {
        public BlueprintSpellListReference[] OriginalLists;
        public BlueprintCharacterClassReference Class;
        protected override void OnTurnOn(){var book=Owner.GetSpellbook(Class.Get());if(book!=null)foreach(var list in OriginalLists)book.RemoveSpecialList(list.Get());}
        protected override void OnTurnOff(){
            var root=ResourcesLibrary.TryGetBlueprint<BlueprintArchetype>(Id(Overseer01447.RootToken));var book=Owner.GetSpellbook(Class.Get());if(book==null||root!=null&&Owner.Progression.IsArchetype(root))return;
            var ids=OriginalLists.Select(r=>r.Guid).ToArray();foreach(var fact in Owner.Facts.List.Where(f=>f.IsActive&&f.IsTurnedOn))foreach(var list in fact.Blueprint.GetComponents<AddSpecialSpellList>().Where(c=>c.CharacterClass==Class.Get()&&ids.Contains(c.SpellList.AssetGuid)))book.AddSpecialList(list.SpellList);
        }
    }
    [HarmonyPatch(typeof(AddSpecialSpellList),"OnActivate")]
    internal static class OverseerSpiritActivation01447Patch {static bool Prefix(AddSpecialSpellList __instance)=>!Overseer01447.Suppress(__instance);}
    [HarmonyPatch(typeof(AddSpecialSpellList),"OnDeactivate")]
    internal static class OverseerSpiritDeactivation01447Patch {static bool Prefix(AddSpecialSpellList __instance)=>!Overseer01447.Suppress(__instance);}
}

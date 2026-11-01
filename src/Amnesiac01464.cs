// Adapted from Holic75/KingmakerRebalance Psychic.createAmnesiac (MIT).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using HarmonyLib;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.ElementsSystem;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Commands.Base;
using Kingmaker.UnitLogic.FactLogic;
using static CallOfTheWild.Wrath.Prototype.PsychicAssets;

namespace CallOfTheWild.Wrath.Prototype
{
    internal static class Amnesiac01464
    {
        internal const string ModuleName="Psychic archetype: Amnesiac",RootToken="AmnesiacArchetype01464",BookToken="AmnesiacSpellbook01464";
        internal const string RecallToken="AmnesiacRecollectionFeature01464",CooldownToken="AmnesiacRecollectionCooldown01464",ForbiddenToken="AmnesiacRecollectionForbidden01464";
        internal const string CastingText="An amnesiac retains fewer ordinary spells known at her two highest spell levels. The remaining knowledge can be recalled with Spell Recollection. Knacks, discipline spells, daily spell slots and Psychic casting are unchanged. On gaining a new spell level, spells two levels lower regain full recall through normal spell selection.";
        internal const string RecallText="Once per hour as a swift action, attempt to recall an unknown spell from the Psychic list in a circle with an available amnesia slot. Roll d100, adding d10 in combat. 1-10: unable to cast spells for one round. 11-35: no spell recalled. 36-95: know the spell until rest. 96+: also refund the amnesia slot. Recollection uses no ordinary spell slot or phrenic points; casting the recalled spell uses ordinary slots. Recalled knowledge is cleared when beginning a level-up, preserving paid knowledge.";
        internal static readonly int[][] Retained={
            new int[0],new[]{0,1},new[]{0,1},new[]{0,2},new[]{0,2,1},new[]{0,2,1},
            new[]{0,4,1,1},new[]{0,5,2,1},new[]{0,5,3,1,1},new[]{0,5,4,2,1},
            new[]{0,5,4,3,1,1},new[]{0,5,5,4,2,1},new[]{0,5,5,4,3,1,1},
            new[]{0,5,5,4,4,2,1},new[]{0,5,5,4,4,3,1,1},new[]{0,5,5,4,4,4,2,1},
            new[]{0,5,5,4,4,4,3,1,1},new[]{0,5,5,4,4,4,3,2,1},
            new[]{0,5,5,4,4,4,3,3,1,1},new[]{0,5,5,4,4,4,3,3,2,1},new[]{0,5,5,4,4,4,3,3,2,2}};
        // This is the original class-level resource contract, independently
        // checked against all 180 normal-known minus retained-known cells.
        internal static int Capacity(int level,int circle)=>level<1||level>20||circle<1||circle>9?0:
            circle==1?(level<=5?1+(level>=5?1:0):0):
            level>=circle*2+1&&(circle>=8||level<=circle*2+3)?1:0;
        internal static string ResourceToken(int circle)=>"AmnesiacSlot"+circle+"01464";
        internal static string MenuToken(int circle)=>"AmnesiacRecallMenu"+circle+"01464";
        internal static string SpellToken(int circle,BlueprintGuid spell)=>"AmnesiacRecall"+circle+spell+"01464";
        sealed class State
        {
            internal BlueprintCharacterClass Class;internal BlueprintArchetype Root;internal UIGroup Group;
            internal BlueprintAbility[] Menus;internal BlueprintAbilityResource[] Resources;
            internal BlueprintBuff Cooldown,Forbidden;
            internal readonly Dictionary<BlueprintGuid,BlueprintScriptableObject> Assets=new Dictionary<BlueprintGuid,BlueprintScriptableObject>();
            internal readonly Dictionary<BlueprintGuid,string> Shapes=new Dictionary<BlueprintGuid,string>();
            internal readonly Dictionary<string,BlueprintAbility> Donors=new Dictionary<string,BlueprintAbility>();
        }
        static readonly ConditionalWeakTable<BlueprintsCache,State> States=new ConditionalWeakTable<BlueprintsCache,State>();
        static void Verify(BlueprintsCache cache,State state)
        {
            if(!ReferenceEquals(cache.Load(state.Class.AssetGuid),state.Class))throw new InvalidOperationException("064 Psychic parent identity changed.");
            foreach(var row in state.Assets){
                if(!ReferenceEquals(cache.Load(row.Key),row.Value))throw new InvalidOperationException("064 published Amnesiac identity changed: "+row.Value.name);
                var actual=MagusBatch01454.Definition(row.Value);var expected=state.Shapes[row.Key];
                if(actual!=expected){int at=0;while(at<actual.Length&&at<expected.Length&&actual[at]==expected[at])at++;int start=Math.Max(0,at-90);throw new InvalidOperationException("064 published Amnesiac resource changed: "+row.Value.name+"; firstDifference="+at+"; expected="+expected.Substring(start,Math.Min(240,expected.Length-start))+"; actual="+actual.Substring(start,Math.Min(240,actual.Length-start)));}
            }
            var current=Spells(state.Class).ToDictionary(p=>p.Item1+":"+p.Item2.AssetGuid,p=>p.Item2);
            foreach(var donor in state.Donors)if(!ReferenceEquals(cache.Load(donor.Value.AssetGuid),donor.Value)||!current.TryGetValue(donor.Key,out var value)||!ReferenceEquals(value,donor.Value))
                throw new InvalidOperationException("064 accepted spell donor identity changed: "+donor.Key);
            MagusBatch01454.VerifyArchetype(state.Class,state.Root,state.Group);
        }
        // A bounded incremental transaction: old private resources retain
        // their exact objects; only the nine owned variant arrays may grow.
        static void Publish(PsychicAssets f,State state,Action connect,Action undo,Action verify)
        {
            var added=f.Assets.Values.ToArray();
            foreach(var bp in added)if(f.Cache.Load(bp.AssetGuid)!=null)throw new InvalidOperationException("064 private GUID occupied: "+bp.name);
            try{
                foreach(var bp in added){
                    PsychicRepairComponents.UniqueNames(bp);
                    if(bp is BlueprintAbility ability)SpellBatch01447.ResetAbilityCaches(ability);
                    var elements=(IEnumerable<Element>)AccessTools.Method(typeof(ArchetypeBatch01445),"Elements").Invoke(null,new object[]{bp.ComponentsArray,new HashSet<object>()});
                    Set(bp,"m_AllElements",elements.ToList());
                }
                f.Enable();foreach(var bp in added)f.Cache.AddCachedBlueprint(bp.AssetGuid,bp);
                foreach(var bp in added)if(!ReferenceEquals(f.Cache.Load(bp.AssetGuid),bp))throw new InvalidOperationException("064 private asset publication failed: "+bp.name);
                connect();verify();
                var shapes=state.Assets.Values.Concat(added).ToDictionary(bp=>bp.AssetGuid,MagusBatch01454.Definition);
                foreach(var bp in added)state.Assets.Add(bp.AssetGuid,bp);
                foreach(var row in shapes)state.Shapes[row.Key]=row.Value;
            }catch{undo();foreach(var bp in added)if(ReferenceEquals(f.Cache.Load(bp.AssetGuid),bp))f.Cache.RemoveCachedBlueprint(bp.AssetGuid);throw;}
        }
        static IEnumerable<Tuple<int,BlueprintAbility>> Spells(BlueprintCharacterClass cls)=>cls.Spellbook.SpellList.SpellsByLevel
            .Where(e=>e.SpellLevel>=1&&e.SpellLevel<=9).SelectMany(e=>e.Spells.Select(s=>Tuple.Create(e.SpellLevel,s)))
            .Where(p=>p.Item2!=null&&p.Item2.Type==AbilityType.Spell).GroupBy(p=>p.Item1+":"+p.Item2.AssetGuid).Select(g=>g.First())
            .OrderBy(p=>p.Item1).ThenBy(p=>p.Item2.AssetGuid.ToString(),StringComparer.Ordinal);
        static Dictionary<int,List<BlueprintAbility>> BuildSpells(PsychicAssets f,State state,Tuple<int,BlueprintAbility>[] spells)
        {
            var variants=Enumerable.Range(1,9).ToDictionary(i=>i,i=>new List<BlueprintAbility>());
            foreach(var row in spells){
                int circle=row.Item1;var spell=row.Item2;
                if(!ReferenceEquals(f.Cache.Load(spell.AssetGuid),spell))throw new InvalidOperationException("064 spell-list identity changed: "+spell.AssetGuid);
                string token=SpellToken(circle,spell.AssetGuid);
                var known=new AddSpellKnownTemporary{Level=circle,OnlySpontaneous=true};Set(known,"m_CharacterClass",Ref<BlueprintCharacterClassReference>(state.Class));Set(known,"m_Spell",Ref<BlueprintAbilityReference>(spell));
                var source=f.Buff(token+"Known","Recalled: "+spell.Name,RecallText,known,
                    new AmnesiaSource01464{Resource=Ref<BlueprintAbilityResourceReference>(state.Resources[circle-1])});
                SetEnum(source,"m_Flags","RemoveOnRest, StayOnDeath");SpellBatch01447.Icon(source,spell);
                var gate=new AmnesiaRecallGate01464{Circle=circle,Spell=Ref<BlueprintAbilityReference>(spell),Cooldown=Ref<BlueprintBuffReference>(state.Cooldown)};
                var action=new RecallAmnesiaSpell01464{Circle=circle,Spell=Ref<BlueprintAbilityReference>(spell),Known=Ref<BlueprintBuffReference>(source),Resource=Ref<BlueprintAbilityResourceReference>(state.Resources[circle-1]),Cooldown=Ref<BlueprintBuffReference>(state.Cooldown),Forbidden=Ref<BlueprintBuffReference>(state.Forbidden)};
                var ability=f.Ability(token,"Recall: "+spell.Name,spell.Description+"\n\n"+RecallText,AbilityType.Extraordinary,UnitCommand.CommandType.Swift,AbilityRange.Personal,
                    gate,ArchetypeBatch01445.Cost(state.Resources[circle-1]),Run(action));
                SpellBatch01447.Icon(ability,spell);Set(ability,"m_Parent",Ref<BlueprintAbilityReference>(state.Menus[circle-1]));variants[circle].Add(ability);
            }
            return variants;
        }
        internal static bool Register(BlueprintsCache cache)
        {
            if(States.TryGetValue(cache,out var existing)){
                if(ConflictingRoot(existing.Class))return false;
                Verify(cache,existing);
                var missing=Spells(existing.Class).Where(p=>!existing.Donors.ContainsKey(p.Item1+":"+p.Item2.AssetGuid)).ToArray();
                if(missing.Length==0)return true;
                var late=new PsychicAssets(cache);var lateChildren=BuildSpells(late,existing,missing);
                var prior=existing.Menus.Select(m=>Get(m.GetComponent<AbilityVariants>(),"m_Variants")).ToArray();
                void Connect(){for(int i=0;i<9;i++)Set(existing.Menus[i].GetComponent<AbilityVariants>(),"m_Variants",((BlueprintAbilityReference[])prior[i]).Concat(lateChildren[i+1].Select(Ref<BlueprintAbilityReference>)).ToArray());}
                void Undo(){for(int i=0;i<9;i++)Set(existing.Menus[i].GetComponent<AbilityVariants>(),"m_Variants",prior[i]);}
                Publish(late,existing,Connect,Undo,()=>MagusBatch01454.VerifyArchetype(existing.Class,existing.Root,existing.Group));
                foreach(var row in missing)existing.Donors.Add(row.Item1+":"+row.Item2.AssetGuid,row.Item2);return true;
            }
            var cls=cache.Load(BlueprintGuid.Parse(PsychicClass.ClassId)) as BlueprintCharacterClass;
            if(cls==null||!ArcaneBatch01446.Existing(cache,PsychicClassCompletion01458.ModuleName))return false;
            if(ConflictingRoot(cls))return false;
            var f=new PsychicAssets(cache);var state=new State{Class=cls};
            var normal=cls.Spellbook.SpellsKnown;
            for(int level=1;level<=20;level++)for(int circle=1;circle<=9;circle++){
                int retained=circle<Retained[level].Length?Retained[level][circle]:0;
                if(Math.Max(0,normal.GetCount(level,circle))-retained!=Capacity(level,circle))throw new InvalidOperationException("064 Psychic spells-known donor changed.");
            }
            var table=f.Clone(normal,"AmnesiacSpellsKnown01464","","");table.Levels=Retained.Select(row=>new SpellsLevelEntry{Count=(int[])row.Clone()}).ToArray();
            var book=f.Clone(cls.Spellbook,BookToken,"","");book.Name=Text(BookToken,"name","Amnesiac");Set(book,"m_SpellsKnown",Ref<BlueprintSpellsTableReference>(table));
            var casting=f.Clone(f.Native<BlueprintFeature>(PsychicClass.CastingId),"AmnesiacSpellcasting01464","Amnesiac Spellcasting",CastingText);
            state.Cooldown=f.Buff(CooldownToken,"Spell Recollection Cooldown",RecallText);SetEnum(state.Cooldown,"m_Flags","RemoveOnRest");
            var forbid=new ForbidSpellCasting{ForbidMagicItems=false};Set(forbid,"m_IgnoreFeature",new BlueprintFeatureReference());
            state.Forbidden=f.Buff(ForbiddenToken,"Spell Recollection: Unable to Cast","Unable to cast spells for one round.",forbid);
            var icon=f.Native<BlueprintAbility>("0a5ddfbcfb3989543ac7c936fc256889");SpellBatch01447.Icon(state.Cooldown,icon);SpellBatch01447.Icon(state.Forbidden,icon);
            state.Resources=Enumerable.Range(1,9).Select(i=>{var r=f.Add(new BlueprintAbilityResource(),ResourceToken(i),"","");var max=Get(r,"m_MaxAmount");Set(max,"BaseValue",0);Set(r,"m_MaxAmount",max);return r;}).ToArray();
            state.Menus=Enumerable.Range(1,9).Select(i=>{var m=f.Ability(MenuToken(i),"Spell Recollection "+i,RecallText,AbilityType.Extraordinary,UnitCommand.CommandType.Swift,AbilityRange.Personal,new AbilityVariants(),new AmnesiaRecallGate01464{Circle=i,Cooldown=Ref<BlueprintBuffReference>(state.Cooldown)});SpellBatch01447.Icon(m,icon);return m;}).ToArray();
            var spells=Spells(cls).ToArray();if(spells.Length==0)throw new InvalidOperationException("064 Psychic spell list is empty.");
            var children=BuildSpells(f,state,spells);
            for(int i=0;i<9;i++)Set(state.Menus[i].GetComponent<AbilityVariants>(),"m_Variants",children[i+1].Select(Ref<BlueprintAbilityReference>).ToArray());
            var components=new List<BlueprintComponent>{new AmnesiaCapacity01464{Resources=state.Resources.Select(Ref<BlueprintAbilityResourceReference>).ToArray()},Facts(state.Menus)};
            components.AddRange(state.Resources.Select(ArchetypeBatch01445.Grant));
            var recall=f.Feature(RecallToken,"Spell Recollection",RecallText,components.ToArray());SpellBatch01447.Icon(recall,icon);
            state.Root=SpellBatch01447.Archetype(f,cls,RootToken,"Amnesiac","Mental blocks have obscured an amnesiac's former psychic power. Recollect forgotten spells from the recesses of memory.",new[]{ArchetypeBatch01445.Entry(1,casting,recall)},new[]{ArchetypeBatch01445.Entry(1,f.Native<BlueprintFeature>(PsychicClass.CastingId))});
            Set(state.Root,"m_ReplaceSpellbook",Ref<BlueprintSpellbookReference>(book));
            // LevelEntry.Features materializes a native lazy list. Prime our
            // own rows before freezing their strict publication definitions.
            foreach(var entry in state.Root.AddFeatures.Concat(state.Root.RemoveFeatures))entry.Features.ToArray();
            foreach(var feature in f.Assets.Values.OfType<BlueprintFeature>())feature.IsClassFeature=true;
            state.Group=MagusBatch01454.Group(new BlueprintFeatureBase[]{casting,recall});var undo=MagusBatch01454.CaptureClass(cls);
            Publish(f,state,()=>MagusBatch01454.ConnectArchetype(cls,state.Root,state.Group),undo,()=>MagusBatch01454.VerifyArchetype(cls,state.Root,state.Group));
            foreach(var row in spells)state.Donors.Add(row.Item1+":"+row.Item2.AssetGuid,row.Item2);
            States.Add(cache,state);return true;
        }
        static bool ConflictingRoot(BlueprintCharacterClass cls)=>cls.Archetypes.Any(a=>a!=null&&a.AssetGuid!=Id(RootToken)&&a.name.IndexOf("Amnesiac",StringComparison.OrdinalIgnoreCase)>=0);
    }
}

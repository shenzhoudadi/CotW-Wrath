using System;
using System.Linq;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Selection;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.Blueprints.Facts;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.ElementsSystem;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.FactLogic;
using static CallOfTheWild.Wrath.Prototype.PsychicAssets;
using static CallOfTheWild.Wrath.Prototype.ArchetypeBatch01445;

namespace CallOfTheWild.Wrath.Prototype
{
    internal static class SpellBatch01447
    {
        static readonly ConcurrentDictionary<Type, FieldInfo[]> ElementFieldCache = new ConcurrentDictionary<Type, FieldInfo[]>();
        static FieldInfo[] ElementFields(Type type) => ElementFieldCache.GetOrAdd(type, t => {
            var list = new List<FieldInfo>();
            for (var c = t; c != null && c != typeof(object) && c != typeof(BlueprintComponent) && c != typeof(Element); c = c.BaseType)
                foreach (var f in c.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
                    if (!f.IsStatic) list.Add(f);
            return list.ToArray();
        });
        static readonly FieldInfo[] AbilityCacheFields = typeof(BlueprintAbility)
            .GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly)
            .Where(f => (f.FieldType.IsGenericType && f.FieldType.GetGenericTypeDefinition() == typeof(Kingmaker.Utility.Cacheable<>))
                || f.Name == "m_CachedRestrictions" || f.Name == "m_CachedTargetRestrictions" || f.Name == "m_CachedCasterRestrictions"
                || f.Name == "m_HasVariants" || f.Name == "m_IsCantrip" || f.Name == "m_SpellDescriptor")
            .ToArray();

        internal static IEnumerable<Element> Elements(object value,HashSet<object> seen) {
            if(value==null||value is string||value is Type||value is Delegate||value.GetType().IsValueType||value is SimpleBlueprint||value is BlueprintReferenceBase||value is UnityEngine.Object)yield break;
            // Traversal tracks object identity, never a resource link's value
            // equality/hash. Native empty WeakResourceLink.AssetId can be null.
            if(seen==null||!ReferenceEquals(seen.Comparer,IdentityComparer))seen=new HashSet<object>(seen??Enumerable.Empty<object>(),IdentityComparer);
            if(!seen.Add(value))yield break;
            if(value is Element element)yield return element;
            if(value is IEnumerable items){foreach(var item in items)foreach(var node in Elements(item,seen))yield return node;yield break;}
            foreach(var field in ElementFields(value.GetType()))foreach(var node in Elements(field.GetValue(value),seen))yield return node;
        }
        internal static void ResetAbilityCaches(BlueprintAbility ability) {
            foreach(var field in AbilityCacheFields) {
                if(field.FieldType.IsGenericType&&field.FieldType.GetGenericTypeDefinition()==typeof(Kingmaker.Utility.Cacheable<>))field.SetValue(ability,Activator.CreateInstance(field.FieldType));
                else field.SetValue(ability,null);
            }
        }
        internal static void Publish(string module,PsychicAssets f,Action connect,Action undo,Action verify) {
            // A shallow blueprint clone also inherits its native element cache.
            // Rebuild it from the independent component/action graph first.
            foreach(var bp in f.Assets.Values){Set(bp,"m_AllElements",Elements(bp.ComponentsArray,new HashSet<object>(IdentityComparer)).ToList());if(bp is BlueprintAbility ability)ResetAbilityCaches(ability);}
            ArcaneBatch01446.Publish(module,f,connect,undo,verify);
        }
        internal static void Icon(BlueprintUnitFact target,BlueprintUnitFact source)=>ArcaneBatch01446.Icon(target,source);
        internal static BlueprintAbilityResource Resource(PsychicAssets f,string token,BlueprintCharacterClass cls,bool rounds) {
            var r=f.Add(new BlueprintAbilityResource(),token,"","");var a=Get(r,"m_MaxAmount");
            Set(a,"BaseValue",rounds?0:3);Set(a,"m_Class",new[]{Ref<BlueprintCharacterClassReference>(cls)});Set(a,"m_ClassDiv",Array.Empty<BlueprintCharacterClassReference>());Set(a,"m_Archetypes",Array.Empty<BlueprintArchetypeReference>());Set(a,"m_ArchetypesDiv",Array.Empty<BlueprintArchetypeReference>());
            if(rounds){Set(a,"IncreasedByLevel",true);Set(a,"LevelIncrease",1);}else{Set(a,"IncreasedByStat",true);Set(a,"ResourceBonusStat",StatType.Intelligence);}Set(r,"m_MaxAmount",a);return r;
        }
        internal static BlueprintProgression Progression(PsychicAssets f,BlueprintProgression source,string token,string name,string description) {
            var p=f.Clone(source,token,name,description);p.LevelEntries=source.LevelEntries.Select(e=>Entry(e.Level,e.Features.ToArray())).ToArray();
            p.UIGroups=(source.UIGroups??Array.Empty<UIGroup>()).Select(g=>{if(g==null)return null;var copy=new UIGroup();Set(copy,"m_Features",g.Features.Select(x=>Ref<BlueprintFeatureBaseReference>(x)).ToList());return copy;}).ToArray();
            foreach(string field in new[]{"m_UIDeterminatorsGroup","m_Classes","m_Archetypes"})Set(p,field,CopyData(Get(source,field)));return p;
        }
        internal static BlueprintArchetype Archetype(PsychicAssets f,BlueprintCharacterClass cls,string token,string title,string description,LevelEntry[] add,LevelEntry[] remove) {
            var a=f.Add(new BlueprintArchetype{LocalizedName=Text(token,"name",title),LocalizedDescription=Text(token,"description",description),AddFeatures=add,RemoveFeatures=remove,ComponentsArray=Array.Empty<BlueprintComponent>()},token,title,description);Set(a,"m_ParentClass",cls);return a;
        }
        internal static void PublishArchetype(BlueprintsCache cache,PsychicAssets f,BlueprintCharacterClass cls,BlueprintArchetype root,BlueprintFeatureBase[] ui) {
            foreach(var ability in f.Assets.Values.OfType<BlueprintAbility>())ResetAbilityCaches(ability);
            var tokens=new[]{f.Assets.Single(p=>ReferenceEquals(p.Value,root)).Key}.Concat(f.Assets.Where(p=>!ReferenceEquals(p.Value,root)).Select(p=>p.Key)).ToArray();
            ArchetypeBatch01445.Vacant(cache,tokens);ArchetypeBatch01445.Publish(cache,f,tokens,cls,root,ui);
        }
        internal static BlueprintSpellList List(PsychicAssets f,string token,Func<int,BlueprintAbility[]> spells) {
            var list=f.Add(new BlueprintSpellList(),token,"","");list.SpellsByLevel=Enumerable.Range(0,10).Select(i=>{var e=new SpellLevelList(i);Set(e,"m_Spells",spells(i).Select(Ref<BlueprintAbilityReference>).ToList());return e;}).ToArray();return list;
        }
    }
}

using System;
using System.Collections;
using System.Reflection;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Selection;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.UnitLogic.Abilities.Components;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.ActivatableAbilities;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using static CallOfTheWild.Wrath.Prototype.PsychicAssets;

namespace CallOfTheWild.Wrath.Prototype
{
    // Only the five new archetypes use this transaction. Existing modules and
    // their receipts/identities are not changed.
    internal static class ArchetypeBatch01445
    {
        sealed class Receipt {
            internal BlueprintScriptableObject[] Assets; internal string[] Definitions;
            internal BlueprintCharacterClass Class; internal BlueprintProgression Progression;
            internal BlueprintArchetype Root; internal UIGroup Group;
            internal Func<Action> ExtraSnapshot; internal Action ExtraConnect;
        }
        static readonly ConditionalWeakTable<BlueprintsCache, Dictionary<string, Receipt>> Published = new ConditionalWeakTable<BlueprintsCache, Dictionary<string, Receipt>>();
        internal static LevelEntry Entry(int level, params BlueprintFeatureBase[] facts) { var e = new LevelEntry { Level = level }; Set(e,"m_Features",facts.Select(Ref<BlueprintFeatureBaseReference>).ToList()); return e; }
        internal static Kingmaker.UnitLogic.Mechanics.Components.ContextRankConfig Rank(BlueprintCharacterClass cls,string progression,params int[] thresholds) {
            var r=new Kingmaker.UnitLogic.Mechanics.Components.ContextRankConfig();SetEnum(r,"m_BaseValueType","ClassLevel");SetEnum(r,"m_Progression",progression);Set(r,"m_Class",new[]{Ref<BlueprintCharacterClassReference>(cls)});
            if(thresholds.Length>0){var t=HarmonyLib.AccessTools.Field(r.GetType(),"m_CustomProgression").FieldType.GetElementType();var entries=Array.CreateInstance(t,thresholds.Length/2);for(int i=0;i<thresholds.Length;i+=2){var e=Activator.CreateInstance(t);Set(e,"BaseValue",thresholds[i]);Set(e,"ProgressionValue",thresholds[i+1]);entries.SetValue(e,i/2);}Set(r,"m_CustomProgression",entries);}return r;
        }
        internal static BlueprintAbilityResource Resource(PsychicAssets f,string token,BlueprintCharacterClass cls,int start,int step) {
            var r=f.Add(new BlueprintAbilityResource(),token,"","");var a=Get(r,"m_MaxAmount");Set(a,"BaseValue",start);Set(a,"IncreasedByLevelStartPlusDivStep",true);Set(a,"StartingLevel",step);Set(a,"StartingIncrease",1);Set(a,"LevelStep",step);Set(a,"PerStepIncrease",1);Set(a,"m_ClassDiv",new[]{Ref<BlueprintCharacterClassReference>(cls)});Set(a,"m_ArchetypesDiv",Array.Empty<BlueprintArchetypeReference>());Set(a,"m_Class",Array.Empty<BlueprintCharacterClassReference>());Set(a,"m_Archetypes",Array.Empty<BlueprintArchetypeReference>());Set(r,"m_MaxAmount",a);return r;
        }
        internal static Kingmaker.Designers.Mechanics.Facts.AddAbilityResources Grant(BlueprintAbilityResource res) {var c=new Kingmaker.Designers.Mechanics.Facts.AddAbilityResources{RestoreAmount=true,RestoreOnLevelUp=false};Set(c,"m_Resource",Ref<BlueprintAbilityResourceReference>(res));return c;}
        internal static AbilityResourceLogic Cost(BlueprintAbilityResource res,int amount=1){var c=new AbilityResourceLogic{Amount=amount};Set(c,"m_IsSpendResource",true);Set(c,"m_RequiredResource",Ref<BlueprintAbilityResourceReference>(res));return c;}
        internal static string Definition(BlueprintScriptableObject bp)
        {
            object Entries(LevelEntry[] entries) => (entries??Array.Empty<LevelEntry>()).Select(e=>new object[]{e.Level,Get(e,"m_Features")}).ToArray();
            object shape;
            if(bp is BlueprintSpellList list) shape=list.SpellsByLevel.Select(e=>new object[]{e.SpellLevel,Get(e,"m_Spells")}).ToArray();
            else if(bp is BlueprintAbilityResource res) shape=new object[]{Get(res,"m_MaxAmount"),Get(res,"m_Min")};
            else if(bp is BlueprintAbilityAreaEffect area) shape=new object[]{area.Size,area.Shape,area.AffectEnemies,area.SpellResistance};
            else if(bp is BlueprintParametrizedFeature par) shape=new object[]{par.ParameterType,par.SpecificSpellLevel,par.SpellLevel,Get(par,"m_SpellList"),Get(par,"m_SpellcasterClass"),par.Ranks};
            else if (bp is BlueprintArchetype a) shape=new object[]{a.GetParentClass(),Entries(a.AddFeatures),Entries(a.RemoveFeatures),a.RemoveSpellbook,a.ReplaceClassSkills,a.ChangeCasterType,a.AddSkillPoints};
            else if(bp is BlueprintProgression p) shape=new object[]{Entries(p.LevelEntries),Get(p,"m_Classes"),Get(p,"m_Archetypes"),(p.UIGroups??Array.Empty<UIGroup>()).Select(g=>g==null?null:Get(g,"m_Features")).ToArray(),Get(p,"m_UIDeterminatorsGroup"),p.Ranks,p.Groups};
            else if(bp is BlueprintFeatureSelection s) shape=new object[]{Get(s,"m_Features"),Get(s,"m_AllFeatures"),s.Mode,s.IgnorePrerequisites,s.Groups,s.Ranks};
            else if(bp is BlueprintFeature f) shape=new object[]{f.Ranks,f.Groups,f.IsClassFeature,f.HideInUI,f.HideInCharacterSheetAndLevelUp};
            else if(bp is BlueprintBuff b) shape=new object[]{b.Stacking,b.Ranks,Get(b,"m_Flags")};
            else if(bp is BlueprintActivatableAbility t) shape=new object[]{t.Buff,t.Group,t.ActivationType,t.DeactivateImmediately,t.DoNotTurnOffOnRest,t.DeactivateIfCombatEnded,t.WeightInGroup};
            else if(bp is BlueprintAbility ability) shape=new object[]{ability.Type,ability.ActionType,ability.Range,ability.CanTargetSelf,ability.CanTargetFriends,ability.CanTargetEnemies,ability.SpellResistance};
            else throw new InvalidOperationException("Unsupported01445 resource: "+bp.name);
            return Authored(new object[]{bp.name,bp.ComponentsArray,shape},new System.Collections.Generic.HashSet<object>(IdentityComparer));
        }
        static string Authored(object value, HashSet<object> path) {
            if (value == null) return "null";
            if (value is BlueprintReferenceBase reference) return "ref:" + reference.Guid;
            if (value is SimpleBlueprint blueprint) return "bp:" + blueprint.AssetGuid;
            var type = value.GetType();
            if (type.IsPrimitive || type.IsEnum || value is string || value is decimal) return type.FullName + ":" + value;
            if (value is UnityEngine.Object) return type.FullName;
            if (!path.Add(value)) return "cycle:" + type.FullName;
            try {
                if (value is IEnumerable items) return "[" + string.Join(",", items.Cast<object>().Select(x => Authored(x, path))) + "]";
                var fields = new List<FieldInfo>();
                for (var t = type; t != null && t != typeof(object) && t != typeof(BlueprintComponent) && t != typeof(UnityEngine.Object); t = t.BaseType)
                    fields.AddRange(t.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly)
                        .Where(f => !f.IsStatic && !(f.DeclaringType == typeof(Kingmaker.ElementsSystem.Condition) && f.Name == "<LastResult>k__BackingField") &&
                            !(f.DeclaringType == typeof(Kingmaker.UnitLogic.Abilities.Components.CasterCheckers.AbilityCasterHasFacts) && f.Name == "m_FactsMissingCache") && !(f.DeclaringType == typeof(Kingmaker.UnitLogic.Abilities.Components.TargetCheckers.AbilityTargetHasFact) && f.Name == "m_FactCache")));
                return type.FullName + "{" + string.Join(";", fields.OrderBy(f => f.DeclaringType.FullName + f.Name).Select(f => {
                    var fieldValue = f.GetValue(value);
                    // Native blueprint JSON deserializes a null reference field
                    // as an empty typed reference. Neither names an asset; an
                    // actual non-empty GUID remains part of the authored graph.
                    return f.Name + "=" + (fieldValue == null && typeof(BlueprintReferenceBase).IsAssignableFrom(f.FieldType)
                        ? "ref:" : Authored(fieldValue, path));
                })) + "}";
            } finally { path.Remove(value); }
        }
        static Action Snapshot(Receipt r) {
            var archetypes=Get(r.Class,"m_Archetypes");var groups=r.Progression.UIGroups;var extra=r.ExtraSnapshot?.Invoke();
            return ()=>{Set(r.Class,"m_Archetypes",archetypes);r.Progression.UIGroups=groups;extra?.Invoke();};
        }
        static void Connect(Receipt r) {
            var undo=Snapshot(r);
            try {
                if(!ReferenceEquals(ResourcesLibrary.BlueprintsCache.Load(r.Class.AssetGuid),r.Class)||!ReferenceEquals(ResourcesLibrary.BlueprintsCache.Load(r.Progression.AssetGuid),r.Progression))throw new InvalidOperationException("01445 native parent publication changed.");
                var refs=(BlueprintArchetypeReference[])Get(r.Class,"m_Archetypes")??Array.Empty<BlueprintArchetypeReference>();var own=refs.Where(x=>x?.Guid==r.Root.AssetGuid).ToArray();
                if(own.Length>1||own.Length==1&&!ReferenceEquals(own[0].Get(),r.Root))throw new InvalidOperationException("01445 archetype parent reference changed.");
                if(own.Length==0)Set(r.Class,"m_Archetypes",refs.Concat(new[]{Ref<BlueprintArchetypeReference>(r.Root)}).ToArray());
                var groups=r.Progression.UIGroups??Array.Empty<UIGroup>();var ids=r.Group.Features.Select(x=>x.AssetGuid).ToArray();
                var matching=groups.Where(g=>g?.Features.Any(x=>x!=null&&ids.Contains(x.AssetGuid))==true).ToArray();
                if(matching.Length==0)r.Progression.UIGroups=groups.Concat(new[]{r.Group}).ToArray();
                else if(matching.Length!=1||!ReferenceEquals(matching[0],r.Group))throw new InvalidOperationException("01445 archetype UI group changed.");
                r.ExtraConnect?.Invoke();
            }catch{undo();throw;}
        }
        internal static bool Existing(BlueprintsCache cache,string root,BlueprintCharacterClass cls) {
            var states=Published.GetValue(cache,_=>new Dictionary<string,Receipt>());
            if(!states.TryGetValue(root,out var r))return false;
            if(!ReferenceEquals(cls,r.Class)||!ReferenceEquals(cls.Progression,r.Progression))throw new InvalidOperationException("01445 native parent identity changed.");
            for(int i=0;i<r.Assets.Length;i++)if(!ReferenceEquals(cache.Load(r.Assets[i].AssetGuid),r.Assets[i])||Definition(r.Assets[i])!=r.Definitions[i])throw new InvalidOperationException("01445 published private resource changed: "+r.Assets[i].name);
            Connect(r);return true;
        }
        // Incremental publication is used only by Drill Sergeant's late pool.
        // Preserve all previously published identities and commit the receipt
        // only after every new blueprint and all three selection lists exist.
        internal static void Extend(BlueprintsCache cache,string root,PsychicAssets f,Action connect,Action undo)
        {
            var r=Published.GetValue(cache,_=>new Dictionary<string,Receipt>())[root];
            var added=f.Assets.Values.ToArray();
            foreach(var bp in added)if(cache.Load(bp.AssetGuid)!=null)throw new InvalidOperationException("01445 late private GUID occupied: "+bp.name);
            foreach(var bp in added){PsychicRepairComponents.UniqueNames(bp);Set(bp,"m_AllElements",Elements(bp.ComponentsArray,new HashSet<object>()).ToList());}
            try {
                f.Enable();
                foreach(var bp in added)cache.AddCachedBlueprint(bp.AssetGuid,bp);
                foreach(var bp in added)if(!ReferenceEquals(cache.Load(bp.AssetGuid),bp))throw new InvalidOperationException("01445 late blueprint was not published: "+bp.name);
                connect();
                var assets=r.Assets.Concat(added).ToArray();var definitions=assets.Select(Definition).ToArray();
                r.Assets=assets;r.Definitions=definitions;
            }catch{undo();foreach(var bp in added)if(ReferenceEquals(cache.Load(bp.AssetGuid),bp))cache.RemoveCachedBlueprint(bp.AssetGuid);throw;}
        }
        internal static void Vacant(BlueprintsCache cache,string[] tokens) {foreach(string t in tokens)if(cache.Load(Id(t))!=null)throw new InvalidOperationException("01445 private GUID occupied: "+t);}
        static IEnumerable<Kingmaker.ElementsSystem.Element> Elements(object value,HashSet<object> seen) {
            if(value==null||value is string||value.GetType().IsValueType||value is SimpleBlueprint||value is BlueprintReferenceBase||value is UnityEngine.Object||!seen.Add(value))yield break;
            if(value is Kingmaker.ElementsSystem.Element element)yield return element;
            if(value is IEnumerable items){foreach(var item in items)foreach(var child in Elements(item,seen))yield return child;yield break;}
            for(var t=value.GetType();t!=null&&t!=typeof(object)&&t!=typeof(BlueprintComponent)&&t!=typeof(Kingmaker.ElementsSystem.Element);t=t.BaseType)
                foreach(var field in t.GetFields(BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.DeclaredOnly).Where(f=>!f.IsStatic))foreach(var child in Elements(field.GetValue(value),seen))yield return child;
        }
        internal static void Publish(BlueprintsCache cache,PsychicAssets f,string[] tokens,BlueprintCharacterClass cls,BlueprintArchetype root,BlueprintFeatureBase[] ui,Func<Action> extraSnapshot=null,Action extraConnect=null) {
            if(tokens.Length!=f.Assets.Count||tokens.Distinct().Count()!=tokens.Length)throw new InvalidOperationException("01445 staged resource manifest mismatch.");
            var assets=tokens.Select(t=>f.Assets[t]).ToArray();foreach(var bp in assets)if(cache.Load(bp.AssetGuid)!=null)throw new InvalidOperationException("01445 private GUID occupied: "+bp.name);foreach(var bp in assets){PsychicRepairComponents.UniqueNames(bp);Set(bp,"m_AllElements",Elements(bp.ComponentsArray,new HashSet<object>()).ToList());}
            var group=new UIGroup();Set(group,"m_Features",ui.Select(Ref<BlueprintFeatureBaseReference>).ToList());
            var r=new Receipt{Assets=assets,Class=cls,Progression=cls.Progression,Root=root,Group=group,ExtraSnapshot=extraSnapshot,ExtraConnect=extraConnect};var undo=Snapshot(r);
            try{f.Enable();foreach(var bp in assets)cache.AddCachedBlueprint(bp.AssetGuid,bp);foreach(var bp in assets)if(!ReferenceEquals(cache.Load(bp.AssetGuid),bp))throw new InvalidOperationException("01445 staged blueprint was not published: "+bp.name);Connect(r);r.Definitions=assets.Select(Definition).ToArray();Published.GetValue(cache,_=>new Dictionary<string,Receipt>()).Add(tokens[0],r);}
            catch{undo();foreach(var bp in assets)if(ReferenceEquals(cache.Load(bp.AssetGuid),bp))cache.RemoveCachedBlueprint(bp.AssetGuid);throw;}
        }
    }
}

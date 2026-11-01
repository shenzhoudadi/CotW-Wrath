using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Selection;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.ActivatableAbilities;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using static CallOfTheWild.Wrath.Prototype.PsychicAssets;

namespace CallOfTheWild.Wrath.Prototype
{
    // Only the three new archetypes use this transaction. Existing modules and
    // their receipts/identities are not changed.
    internal static class ArchetypeBatch01444
    {
        sealed class Receipt {
            internal BlueprintScriptableObject[] Assets; internal string[] Definitions;
            internal BlueprintCharacterClass Class; internal BlueprintProgression Progression;
            internal BlueprintArchetype Root; internal UIGroup Group;
            internal Func<Action> ExtraSnapshot; internal Action ExtraConnect;
        }
        static readonly ConditionalWeakTable<BlueprintsCache, Dictionary<string, Receipt>> Published = new ConditionalWeakTable<BlueprintsCache, Dictionary<string, Receipt>>();
        internal static LevelEntry Entry(int level, params BlueprintFeatureBase[] facts) { var e = new LevelEntry { Level = level }; Set(e,"m_Features",facts.Select(Ref<BlueprintFeatureBaseReference>).ToList()); return e; }
        internal static string Definition(BlueprintScriptableObject bp)
        {
            object Entries(LevelEntry[] entries) => (entries??Array.Empty<LevelEntry>()).Select(e=>new object[]{e.Level,Get(e,"m_Features")}).ToArray();
            object shape;
            if (bp is BlueprintArchetype a) shape=new object[]{a.GetParentClass(),Entries(a.AddFeatures),Entries(a.RemoveFeatures),a.RemoveSpellbook,a.ReplaceClassSkills,a.ChangeCasterType,a.AddSkillPoints};
            else if(bp is BlueprintProgression p) shape=new object[]{Entries(p.LevelEntries),Get(p,"m_Classes"),Get(p,"m_Archetypes"),(p.UIGroups??Array.Empty<UIGroup>()).Select(g=>g==null?null:Get(g,"m_Features")).ToArray(),Get(p,"m_UIDeterminatorsGroup"),p.Ranks,p.Groups};
            else if(bp is BlueprintFeatureSelection s) shape=new object[]{Get(s,"m_Features"),Get(s,"m_AllFeatures"),s.Mode,s.IgnorePrerequisites,s.Groups,s.Ranks};
            else if(bp is BlueprintFeature f) shape=new object[]{f.Ranks,f.Groups,f.IsClassFeature,f.HideInUI,f.HideInCharacterSheetAndLevelUp};
            else if(bp is BlueprintBuff b) shape=new object[]{b.Stacking,b.Ranks,Get(b,"m_Flags")};
            else if(bp is BlueprintActivatableAbility t) shape=new object[]{t.Buff,t.Group,t.ActivationType,t.DeactivateImmediately,t.DoNotTurnOffOnRest,t.DeactivateIfCombatEnded,t.WeightInGroup};
            else if(bp is BlueprintAbility ability) shape=new object[]{ability.Type,ability.ActionType,ability.Range,ability.CanTargetSelf,ability.CanTargetFriends,ability.CanTargetEnemies,ability.SpellResistance};
            else throw new InvalidOperationException("Unsupported01444 resource: "+bp.name);
            return Migration01426.Definition(new object[]{bp.name,bp.ComponentsArray,shape});
        }
        static Action Snapshot(Receipt r) {
            var archetypes=Get(r.Class,"m_Archetypes");var groups=r.Progression.UIGroups;var extra=r.ExtraSnapshot?.Invoke();
            return ()=>{Set(r.Class,"m_Archetypes",archetypes);r.Progression.UIGroups=groups;extra?.Invoke();};
        }
        static void Connect(Receipt r) {
            var undo=Snapshot(r);
            try {
                var refs=(BlueprintArchetypeReference[])Get(r.Class,"m_Archetypes")??Array.Empty<BlueprintArchetypeReference>();var own=refs.Where(x=>x?.Guid==r.Root.AssetGuid).ToArray();
                if(own.Length>1||own.Length==1&&!ReferenceEquals(own[0].Get(),r.Root))throw new InvalidOperationException("01444 archetype parent reference changed.");
                if(own.Length==0)Set(r.Class,"m_Archetypes",refs.Concat(new[]{Ref<BlueprintArchetypeReference>(r.Root)}).ToArray());
                var groups=r.Progression.UIGroups??Array.Empty<UIGroup>();var ids=r.Group.Features.Select(x=>x.AssetGuid).ToArray();
                var matching=groups.Where(g=>g?.Features.Any(x=>x!=null&&ids.Contains(x.AssetGuid))==true).ToArray();
                if(matching.Length==0)r.Progression.UIGroups=groups.Concat(new[]{r.Group}).ToArray();
                else if(matching.Length!=1||!ReferenceEquals(matching[0],r.Group))throw new InvalidOperationException("01444 archetype UI group changed.");
                r.ExtraConnect?.Invoke();
            }catch{undo();throw;}
        }
        internal static bool Existing(BlueprintsCache cache,string root,BlueprintCharacterClass cls) {
            var states=Published.GetValue(cache,_=>new Dictionary<string,Receipt>());
            if(!states.TryGetValue(root,out var r))return false;
            if(!ReferenceEquals(cls,r.Class)||!ReferenceEquals(cls.Progression,r.Progression))throw new InvalidOperationException("01444 native parent identity changed.");
            for(int i=0;i<r.Assets.Length;i++)if(!ReferenceEquals(cache.Load(r.Assets[i].AssetGuid),r.Assets[i])||Definition(r.Assets[i])!=r.Definitions[i])throw new InvalidOperationException("01444 published private resource changed: "+r.Assets[i].name);
            Connect(r);return true;
        }
        internal static void Vacant(BlueprintsCache cache,string[] tokens) {foreach(string t in tokens)if(cache.Load(Id(t))!=null)throw new InvalidOperationException("01444 private GUID occupied: "+t);}
        internal static void Publish(BlueprintsCache cache,PsychicAssets f,string[] tokens,BlueprintCharacterClass cls,BlueprintArchetype root,BlueprintFeatureBase[] ui,Func<Action> extraSnapshot=null,Action extraConnect=null) {
            if(tokens.Length!=f.Assets.Count||tokens.Distinct().Count()!=tokens.Length)throw new InvalidOperationException("01444 staged resource manifest mismatch.");
            var assets=tokens.Select(t=>f.Assets[t]).ToArray();foreach(var bp in assets)PsychicRepairComponents.UniqueNames(bp);
            var group=new UIGroup();Set(group,"m_Features",ui.Select(Ref<BlueprintFeatureBaseReference>).ToList());
            var r=new Receipt{Assets=assets,Class=cls,Progression=cls.Progression,Root=root,Group=group,ExtraSnapshot=extraSnapshot,ExtraConnect=extraConnect};var undo=Snapshot(r);
            try{f.Enable();foreach(var bp in assets)cache.AddCachedBlueprint(bp.AssetGuid,bp);Connect(r);r.Definitions=assets.Select(Definition).ToArray();Published.GetValue(cache,_=>new Dictionary<string,Receipt>()).Add(tokens[0],r);}
            catch{undo();foreach(var bp in assets)if(ReferenceEquals(cache.Load(bp.AssetGuid),bp))cache.RemoveCachedBlueprint(bp.AssetGuid);throw;}
        }
    }
}

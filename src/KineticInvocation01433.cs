using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using Newtonsoft.Json;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Prerequisites;
using Kingmaker.Blueprints.Classes.Selection;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.Blueprints.Facts;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.Controllers.Units;
using Kingmaker.EntitySystem;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.PubSubSystem;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components;
using Kingmaker.UnitLogic.Abilities.Components.CasterCheckers;
using Kingmaker.UnitLogic.Buffs;
using Kingmaker.UnitLogic.Class.Kineticist;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Mechanics.Components;
using static CallOfTheWild.Wrath.Prototype.PsychicAssets;

namespace CallOfTheWild.Wrath.Prototype
{
    // One ordinary feat unlocks seven separately selected utility talents.
    // Native/provider spell effects are referenced, never edited or replaced.
    internal static class KineticInvocation01433
    {
        internal const string ModuleName = "Kinetic Invocation";
        internal const string Token = "KineticInvocationFeat";
        internal const string CloakId = "6fb10fff27b84bff81746194bf8e38a8";
        internal const string SlowingMudId = "6b30813c3709fc44b92dc8fd8191f345";
        internal const string SalamanderId = "9256a86aec14ad14e9497f6b60e26f3f";
        internal static readonly string[] Tokens = { "CloakOfWinds", "WindWalk", "SlowingMud", "BlessingOfTheSalamander", "Invigorate", "FluidForm", "SleetStorm" };
        internal static readonly string[] Titles = { "Cloak of Winds", "Wind Walk", "Slowing Mud", "Blessing of the Salamander", "Invigorate", "Fluid Form", "Sleet Storm" };
        internal static readonly int[] Levels = { 3, 6, 4, 5, 1, 6, 3 };
        static readonly string[] Elements = { "air", "air", "earth", "fire", "fire", "water", "water" };
        static readonly string[] Effects = {
            "Apply the enabled TabletopTweaks Cloak of Winds spell's protective wind effects and original duration.",
            "Use the existing Wind Walk world-map travel effect: the party travels at a speed multiplier of at least 7 for 1 hour per kineticist level. This does not grant combat flight.",
            "Use native Slowing Mud, retaining its targeting, saves, mud effects, cleaning actions and duration.",
            "Apply native Blessing of the Salamander to yourself, retaining its fire protection, healing, retaliation and duration.",
            "Use the existing Invigorate fatigue-suppression effect and duration. Existing recipient and fatigue restrictions remain.",
            "Use the existing Fluid Form transformation, retaining its movement, reach, damage reduction and duration.",
            "Create the existing Sleet Storm, retaining its original area, movement checks, concealment, ranged-attack restrictions and duration."
        };
        internal static string LastOptionalDiagnostic = "Cloak of Winds provider not detected";
        const string Text = "For each kineticist element you possess, you may choose the associated spell as a utility wild talent at its listed level. This feat does not grant those talents automatically. Air: Cloak of Winds (3), Wind Walk (6). Earth: Slowing Mud (4). Fire: Blessing of the Salamander (5, self only), Invigorate (1). Water: Fluid Form (6), Sleet Storm (3). Each costs 1 burn. Caster level is your kineticist level; save DC is 10 + half your kineticist level + your kineticist main ability modifier. Nonpermanent, non-instantaneous effects end when your burn is cleared. Cloak of Winds requires the enabled TabletopTweaks spell. Other spell effects and restrictions follow their existing implementations.";
        sealed class Graph
        {
            internal readonly Dictionary<BlueprintGuid, BlueprintScriptableObject> Assets;
            internal readonly Dictionary<BlueprintGuid, string> Definitions;
            internal readonly BlueprintFeature[] Feats, Talents;
            internal Graph(PsychicAssets f, BlueprintFeature[] feats, BlueprintFeature[] talents) {
                Assets=f.Assets.Values.ToDictionary(b=>b.AssetGuid); Definitions=Assets.ToDictionary(p=>p.Key,p=>Layout(p.Value));
                Feats=feats; Talents=talents;
            }
        }
        sealed class Publication { internal Graph Core, Cloak; }
        static readonly ConditionalWeakTable<BlueprintsCache, Publication> Published = new ConditionalWeakTable<BlueprintsCache, Publication>();
        static string Layout(BlueprintScriptableObject bp) {
            var values=new List<object> { bp.name,bp.GetType().FullName,bp.ComponentsArray };
            if(bp is BlueprintFeature f)values.AddRange(new object[]{f.Groups,f.Ranks,f.IsClassFeature});
            if(bp is BlueprintAbility a)values.AddRange(new object[]{a.Type,a.ActionType,a.Range,a.CanTargetSelf,a.CanTargetFriends,a.CanTargetEnemies,a.CanTargetPoint,a.AvailableMetamagic,a.SpellResistance,a.MaterialComponent,Get(a,"m_Parent"),a.IsFullRoundAction});
            return Migration01426.Definition(values);
        }
        static IEnumerable<BlueprintReferenceBase> References(object value, HashSet<object> seen) {
            if(value==null)yield break;
            if(value is BlueprintReferenceBase r){yield return r;yield break;}
            var t=value.GetType();
            if(value is SimpleBlueprint||value is UnityEngine.Object||t.IsPrimitive||t.IsEnum||value is string||value is decimal||value is Type||value is Delegate||!seen.Add(value))yield break;
            if(value is IEnumerable list){foreach(var x in list)foreach(var child in References(x,seen))yield return child;yield break;}
            for(;t!=null&&t!=typeof(object)&&t!=typeof(BlueprintComponent);t=t.BaseType)
                foreach(var field in t.GetFields(BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.DeclaredOnly).Where(f=>!f.IsStatic))
                    foreach(var child in References(field.GetValue(value),seen))yield return child;
        }
        static BlueprintFeatureSelection[] Pools(BlueprintsCache cache)=>new[]{
            cache.Load(BlueprintGuid.Parse(BlueprintRegistration.BasicFeatsId)) as BlueprintFeatureSelection ?? throw new InvalidOperationException("Kinetic Invocation ordinary feat pool missing"),
            cache.Load(BlueprintGuid.Parse(PurifyingFlames01428.PoolId)) as BlueprintFeatureSelection ?? throw new InvalidOperationException("Kinetic Invocation wild talent pool missing")};
        static void Append(BlueprintsCache cache, Graph graph) {
            var pools=Pools(cache);var before=pools.Select(p=>new[]{Get(p,"m_Features"),Get(p,"m_AllFeatures")}).ToArray();
            try {if(graph.Feats.Length>0)T09CombatFeats.Append(new[]{pools[0]},graph.Feats);if(graph.Talents.Length>0)T09CombatFeats.Append(new[]{pools[1]},graph.Talents);}
            catch {for(int i=0;i<pools.Length;i++){Set(pools[i],"m_Features",before[i][0]);Set(pools[i],"m_AllFeatures",before[i][1]);}throw;}
        }
        static void Restore(BlueprintsCache cache,Graph graph) {
            foreach(var pair in graph.Assets) {
                if(!ReferenceEquals(cache.Load(pair.Key),pair.Value)||Layout(pair.Value)!=graph.Definitions[pair.Key])throw new InvalidOperationException("Kinetic Invocation published graph missing, replaced or changed: "+pair.Value.name);
                var refs=References(pair.Value.ComponentsArray,new HashSet<object>()).ToList();
                if(pair.Value is BlueprintAbility a)refs.Add((BlueprintAbilityReference)Get(a,"m_Parent"));
                foreach(var r in refs)if(r!=null&&graph.Assets.TryGetValue(r.Guid,out var expected)) {
                    var cached=typeof(BlueprintReferenceBase).GetProperty("Cached",BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic).GetValue(r,null);
                    if(cached!=null&&!ReferenceEquals(cached,expected))throw new InvalidOperationException("Kinetic Invocation private reference replaced: "+pair.Value.name);
                }
            }
            Append(cache,graph);
        }
        static Graph Commit(PsychicAssets f,BlueprintFeature[] feats,BlueprintFeature[] talents) {
            if(f.Assets.Values.Any(b=>f.Cache.Load(b.AssetGuid)!=null))throw new InvalidOperationException("Kinetic Invocation unowned or incomplete existing private graph");
            var pools=Pools(f.Cache);var before=pools.Select(p=>new[]{Get(p,"m_Features"),Get(p,"m_AllFeatures")}).ToArray();
            try {Migration01426.Commit(f,Array.Empty<BlueprintFeatureSelection>());var graph=new Graph(f,feats,talents);Append(f.Cache,graph);return graph;}
            catch {for(int i=0;i<pools.Length;i++){Set(pools[i],"m_Features",before[i][0]);Set(pools[i],"m_AllFeatures",before[i][1]);}foreach(var bp in f.Assets.Values)if(ReferenceEquals(f.Cache.Load(bp.AssetGuid),bp))f.Cache.RemoveCachedBlueprint(bp.AssetGuid);throw;}
        }
        static BlueprintAbility Source(BlueprintsCache cache,int index) {
            string id=index==0?CloakId:index==2?SlowingMudId:index==3?SalamanderId:Id(Tokens[index]).ToString();
            var raw=cache.Load(BlueprintGuid.Parse(id));
            if(index==0&&raw==null)return null;
            string expected=index==0?"CloakOfWindsAbility":index==2?"SlowMud":index==3?"BlessingOfTheSalamander":"CotWWrathPsychic"+Tokens[index];
            if(!(raw is BlueprintAbility a)||a.name!=expected||a.Type!=AbilityType.Spell||a.GetComponent<AbilityEffectRunAction>()==null&&a.GetComponent<AbilityVariants>()==null)
                throw new InvalidOperationException("Kinetic Invocation spell source missing or changed: "+expected);
            if(index==0&&!a.GetComponents<SpellListComponent>().Any(c=>c.SpellList!=null&&c.SpellList.AssetGuid!=BlueprintGuid.Parse(PsychicClass.ListId)&&c.SpellList.SpellsByLevel!=null&&c.SpellList.SpellsByLevel.Any(l=>l!=null&&l.SpellLevel==c.SpellLevel&&l.Spells.Any(s=>s?.AssetGuid==a.AssetGuid))))return null;
            return a;
        }
        static PrerequisiteClassLevel Level(BlueprintCharacterClass cls,int value){var p=new PrerequisiteClassLevel{Level=value};Set(p,"m_CharacterClass",Ref<BlueprintCharacterClassReference>(cls));return p;}
        static BlueprintFeature Talent(PsychicAssets f,BlueprintFeature feat,int index,BlueprintAbility source) {
            var cls=f.Native<BlueprintCharacterClass>(PurifyingFlames01428.ClassId);string token="KineticInvocation"+Tokens[index];
            string text="Element: "+Elements[index]+". Utility wild talent, level "+Levels[index]+". Burn: 1. Requires Kinetic Invocation and kineticist level "+(Levels[index]==1?1:2*Levels[index])+". "+(index==3?"Self only. ":"")+"Caster level and save DC use your kineticist level and main ability. Nonpermanent effects end when your burn is cleared. "+Effects[index];
            BlueprintAbility Convert(BlueprintAbility original,string key,bool child) {
                var variants=original.GetComponent<AbilityVariants>();var a=f.Clone(original,key,"Kinetic Invocation: "+Titles[index],text);
                a.Type=AbilityType.SpellLike;a.AvailableMetamagic=0;a.MaterialComponent=new BlueprintAbility.MaterialComponentData();Set(a,"m_Parent",new BlueprintAbilityReference());
                if(child)Set(a,"m_DisplayName",Get(original,"m_DisplayName"));
                if(index==3){a.Range=AbilityRange.Personal;a.CanTargetSelf=true;a.CanTargetFriends=a.CanTargetEnemies=a.CanTargetPoint=false;}
                var parameters=Inquisitions01426.Parameters(f,key,new[]{cls},StatType.Constitution).OfType<ContextCalculateAbilityParams>().Single();parameters.UseKineticistMainStat=true;
                var qualified=new AbilityCasterHasFacts();Set(qualified,"m_Facts",new[]{Ref<BlueprintUnitFactReference>(feat)});
                a.ComponentsArray=a.ComponentsArray.Where(c=>!(c is SpellListComponent)&&!(c is Kingmaker.Craft.CraftInfoComponent)&&!(c is AbilityResourceLogic)&&!(c is ContextCalculateAbilityParams)&&!(c is AbilityVariants))
                    .Concat(new BlueprintComponent[]{parameters,qualified,new KineticInvocationOrigin01433{Original=Ref<BlueprintAbilityReference>(source)}}).ToArray();
                if(variants!=null){var children=variants.Variants.Select((v,n)=>Convert(v,key+"Option"+n,true)).ToArray();var menu=new AbilityVariants();Set(menu,"m_Variants",children.Select(Ref<BlueprintAbilityReference>).ToArray());a.ComponentsArray=a.ComponentsArray.Concat(new[]{menu}).ToArray();foreach(var c in children)Set(c,"m_Parent",Ref<BlueprintAbilityReference>(a));}
                else {var native=f.Native<BlueprintAbility>(PurifyingFlames01428.HealerId).GetComponents<AbilityKineticist>().ToArray();if(native.Length!=1)throw new InvalidOperationException("Native utility burn cost changed");var cost=CopyData(native[0]);cost.Amount=cost.WildTalentBurnCost=1;cost.InfusionBurnCost=cost.BlastBurnCost=0;a.ComponentsArray=a.ComponentsArray.Concat(new[]{cost}).ToArray();}
                return a;
            }
            var ability=Convert(source,token+"Ability",false);
            var components=new List<BlueprintComponent>{Migration01426.Require(feat),Level(cls,Levels[index]==1?1:2*Levels[index]),Facts(ability)};
            if(index==2){var p=new PrerequisiteFeaturesFromList{Amount=1};Set(p,"m_Features",new[]{"c6816ad80a3df9c4ea7d3b012b06bacd","956b65effbf37e5419c13100ab4385a3","c43d9c2d23e56fb428a4eb60da9ba1cb","d2a93ab18fcff8c419b03a2c3d573606"}.Select(id=>Ref<BlueprintFeatureReference>(f.Native<BlueprintFeature>(id))).ToArray());components.Add(p);}
            else {string element=index<2?KineticUtility01433.AirPrerequisiteId:index<5?PurifyingFlames01428.FoxfireId:KineticUtility01433.WaterPrerequisiteId;var donor=f.Native<BlueprintFeature>(element).GetComponents<PrerequisiteFeaturesFromList>().ToArray();if(donor.Length!=1||donor[0].Features.Length==0)throw new InvalidOperationException("Native kinetic element prerequisite changed");components.Add(CopyData(donor[0]));}
            var root=f.Feature(token+"Talent","Kinetic Invocation: "+Titles[index],text,components.ToArray());root.IsClassFeature=true;root.Groups=new[]{FeatureGroup.KineticWildTalent};Set(root,"m_Icon",Get(source,"m_Icon"));
            var once=new PrerequisiteNoFeature();Set(once,"m_Feature",Ref<BlueprintFeatureReference>(root));root.ComponentsArray=root.ComponentsArray.Concat(new[]{once}).ToArray();return root;
        }
        internal static void Register(BlueprintsCache cache) {
            var state=Published.GetValue(cache,_=>new Publication());lock(state) {
                if(state.Core!=null)Restore(cache,state.Core);
                else {
                    var sources=Enumerable.Range(1,6).Select(i=>Source(cache,i)).ToArray();var f=new PsychicAssets(cache);var cls=f.Native<BlueprintCharacterClass>(PurifyingFlames01428.ClassId);
                    var feat=f.Feature(Token,ModuleName,Text,Level(cls,1),new KineticInvocationLifetime01433());feat.Groups=new[]{FeatureGroup.Feat};feat.IsClassFeature=false;
                    Set(feat,"m_Icon",Get(f.Native<BlueprintFeature>(KineticUtility01433.AirPrerequisiteId),"m_Icon"));var once=new PrerequisiteNoFeature();Set(once,"m_Feature",Ref<BlueprintFeatureReference>(feat));feat.ComponentsArray=feat.ComponentsArray.Concat(new[]{once}).ToArray();
                    var talents=Enumerable.Range(1,6).Select(i=>Talent(f,feat,i,sources[i-1])).ToArray();state.Core=Commit(f,new[]{feat},talents);
                }
                try {if(state.Cloak!=null){Restore(cache,state.Cloak);return;}
                    var source=Source(cache,0);if(source==null){LastOptionalDiagnostic="Kinetic Invocation: enabled TabletopTweaks Cloak of Winds not detected; six core talents remain registered.";return;}
                    var f=new PsychicAssets(cache);var feat=(BlueprintFeature)cache.Load(Id(Token));var talent=Talent(f,feat,0,source);state.Cloak=Commit(f,Array.Empty<BlueprintFeature>(),new[]{talent});LastOptionalDiagnostic="Kinetic Invocation: all seven talents registered.";
                } catch(Exception e){LastOptionalDiagnostic="Kinetic Invocation: optional Cloak of Winds isolated; six core talents preserved. "+e;ModEntry.LogSpellDiagnostic(LastOptionalDiagnostic);}
            }
        }
    }
    [TypeId("e5b27bc22ed34c589f5d0a7b8c390133")]
    public sealed class KineticInvocationOrigin01433 : BlueprintComponent { public BlueprintAbilityReference Original; }

    public sealed class KineticInvocationState01433
    {
        [JsonProperty] public List<EntityFactRef<Buff>> Buffs=new List<EntityFactRef<Buff>>(), PendingBuffs=new List<EntityFactRef<Buff>>();
        [JsonProperty] public List<EntityRef<AreaEffectEntityData>> Areas=new List<EntityRef<AreaEffectEntityData>>(), PendingAreas=new List<EntityRef<AreaEffectEntityData>>();
        [JsonIgnore] public bool Clearing;
    }
    [TypeId("9347ebbd51b24ee8917c98fcaa6a0331")]
    public sealed class KineticInvocationLifetime01433 : UnitFactComponentDelegate<KineticInvocationState01433>, IUnitBuffHandler, IAreaEffectHandler, IKineticistBurnValueHandler, IUnitRestHandler, ITickEachRound, IGlobalSubscriber
    {
        static readonly FieldInfo Duration=typeof(AreaEffectEntityData).GetField("m_Duration",BindingFlags.Instance|BindingFlags.NonPublic);
        internal bool Owns(MechanicsContext context) {
            if(context?.MaybeCaster!=Owner)return false;
            var seen=new HashSet<MechanicsContext>();
            for(var c=context;c!=null&&seen.Add(c);c=c.ParentContext)
                if((c.SourceAbilityContext?.Ability?.Blueprint??c.SourceAbility)?.GetComponent<KineticInvocationOrigin01433>()!=null)return true;
            return false;
        }
        public void HandleBuffDidAdded(Buff buff) {if(buff!=null&&!buff.IsPermanent&&Owns(buff.Context)&&!Data.Buffs.Any(r=>r.Fact==buff))Data.Buffs.Add(buff);}
        public void HandleBuffDidRemoved(Buff buff) {Data.Buffs.RemoveAll(r=>r.Fact==buff);Data.PendingBuffs.RemoveAll(r=>r.Fact==buff);}
        public void HandleAreaEffectSpawned(AreaEffectEntityData area) {if(area!=null&&Duration?.GetValue(area) is TimeSpan&&Owns(area.Context)&&!Data.Areas.Any(r=>r.Entity==area))Data.Areas.Add(area);}
        public void HandleAreaEffectDestroyed(AreaEffectEntityData area) {Data.Areas.RemoveAll(r=>r.Entity==area);Data.PendingAreas.RemoveAll(r=>r.Entity==area);}
        internal void ClearEffects(bool schedule) {
            if(Data.Clearing)return;Data.Clearing=true;
            try {
                if(schedule){Data.PendingBuffs.AddRange(Data.Buffs);Data.PendingAreas.AddRange(Data.Areas);Data.Buffs.Clear();Data.Areas.Clear();}
                foreach(var r in Data.PendingAreas.ToArray())try{var area=r.Entity;if(area!=null&&Owns(area.Context))area.MarkForDestroy();Data.PendingAreas.Remove(r);}catch(Exception e){ModEntry.LogSpellDiagnostic("Kinetic Invocation area cleanup isolated: "+e);}
                foreach(var r in Data.PendingBuffs.ToArray())try{var buff=r.Fact;if(buff!=null&&!buff.IsPermanent&&Owns(buff.Context))buff.Remove();Data.PendingBuffs.Remove(r);}catch(Exception e){ModEntry.LogSpellDiagnostic("Kinetic Invocation buff cleanup isolated: "+e);}
            } finally {Data.Clearing=false;}
        }
        public void HandleKineticistBurnValueChanged(UnitPartKineticist part,int previous,AbilityData ability) {
            if(part?.Owner==Owner.Descriptor&&part.AcceptedBurn==0&&(previous>0||ability==null))ClearEffects(true);
        }
        // Native ClearAcceptedBurn emits no event for an already-zero burn
        // total. Rest must also end invocations whose final cost was reduced
        // to zero by a legitimate kineticist cost modifier.
        public void HandleUnitRest(UnitEntityData unit){if(unit==Owner)ClearEffects(true);}
        public void OnNewRound(){if(Data.PendingBuffs.Count>0||Data.PendingAreas.Count>0)ClearEffects(false);}
        protected override void OnApplyPostLoadFixes(){if(Data.PendingBuffs.Count>0||Data.PendingAreas.Count>0)ClearEffects(false);}
        // Save/scene suspension turns facts off without removing them. Cleanup
        // belongs to actual manager removal, after removal from its list and
        // before Detach clears Owner; Reapply/temporary deactivation preserves it.
        protected override void OnDeactivate() {
            if(Owner?.Facts!=null&&!Owner.Facts.List.Contains(Fact))ClearEffects(true);
        }
    }
}

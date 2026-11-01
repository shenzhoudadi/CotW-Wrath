// Original CotW Features/WizardDiscoveries.cs createInfectiousCharms (MIT).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using HarmonyLib;
using Newtonsoft.Json;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Prerequisites;
using Kingmaker.Blueprints.Classes.Selection;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.Designers;
using Kingmaker.EntitySystem;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.PubSubSystem;
using Kingmaker.RuleSystem.Rules.Abilities;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components;
using Kingmaker.UnitLogic.Abilities.Components.Base;
using Kingmaker.UnitLogic.Buffs;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Commands.Base;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Mechanics.Actions;
using Kingmaker.Utility;
using static CallOfTheWild.Wrath.Prototype.PsychicAssets;

namespace CallOfTheWild.Wrath.Prototype
{
    internal static class InfectiousCharms01447
    {
        internal const string ModuleName="Wizard discovery: Infectious Charms",RootToken="InfectiousCharmsFeature01447";
        internal const string Description="Anytime you target and successfully affect a single creature with a charm or compulsion spell and that creature is within 30 feet of another opponent, your spell has a chance of affecting the second creature as well. As a swift action immediately after affecting a creature with a charm or compulsion spell, you can cause the spell to carry over to the nearest creature within 30 feet. The spell behaves in all ways as though its new target were the original target of the spell.";
        sealed class Binding {internal BlueprintAbility Original,Swift;internal BlueprintBuff Window,Marker;}
        static readonly ConditionalWeakTable<BlueprintsCache,Dictionary<BlueprintGuid,Binding>> Bindings=new ConditionalWeakTable<BlueprintsCache,Dictionary<BlueprintGuid,Binding>>();
        static readonly ConditionalWeakTable<AbilityExecutionContext,object> Seen=new ConditionalWeakTable<AbilityExecutionContext,object>();
        internal static string T(BlueprintAbility ability,string suffix)=>"InfectiousCharms"+ability.AssetGuid+suffix+"01447";
        internal static void Register(BlueprintsCache cache) {
            if(ArcaneBatch01446.Existing(cache,ModuleName)){Reconcile(cache);return;}
            var f=new PsychicAssets(cache);var wizard=f.Native<BlueprintCharacterClass>(WizardSubschools01428.WizardId);var icon=f.Native<BlueprintAbility>("d7cbd2004ce66a042aeab2e95a3c5c61");
            var spells=wizard.Spellbook.SpellList.SpellsByLevel.Where(e=>e.SpellLevel>=1&&e.SpellLevel<=9).SelectMany(e=>e.Spells).Where(s=>s!=null&&s.School==SpellSchool.Enchantment&&(s.SpellDescriptor&SpellDescriptor.Compulsion)!=0&&s.Range!=AbilityRange.Personal&&!s.CanTargetPoint&&s.GetComponent<AbilityTargetsAround>()==null&&s.GetComponent<AbilityDeliverChain>()==null&&!SpellBatch01447.Elements(s.ComponentsArray,new HashSet<object>()).OfType<Kingmaker.UnitLogic.Mechanics.Actions.ContextActionSpawnAreaEffect>().Any()).Select(s=>s.GetComponent<AbilityEffectStickyTouch>()?.TouchDeliveryAbility??s).Distinct().ToArray();
            if(spells.Length==0||spells.Any(s=>s.GetComponent<AbilityEffectRunAction>()==null))throw new InvalidOperationException("01447 native single-target Compulsion spell graph changed.");
            var bindings=new Dictionary<BlueprintGuid,Binding>();var variants=new List<BlueprintAbility>();
            foreach(var spell in spells) {
                var marker=f.Buff(T(spell,"Target"),"Infectious Charms (Possible Target)",Description);marker.Stacking=StackingType.Stack;SetEnum(marker,"m_Flags","HiddenInUi");SpellBatch01447.Icon(marker,spell);
                var window=f.Buff(T(spell,"Window"),"Infectious Charms",Description,new InfectiousWindow01447());SpellBatch01447.Icon(window,spell);
                var swift=f.Clone(spell,T(spell,"Swift"),"","");Set(swift,"m_DisplayName",Get(spell,"m_DisplayName"));Set(swift,"m_Description",Get(spell,"m_Description"));swift.ActionType=UnitCommand.CommandType.Swift;swift.Range=AbilityRange.Unlimited;swift.AvailableMetamagic=0;swift.MaterialComponent=new BlueprintAbility.MaterialComponentData();
                swift.ComponentsArray=swift.ComponentsArray.Where(c=>!(c is AbilityDeliverTouch)&&!(c is AbilityResourceLogic)&&!(c is SpellListComponent)).Concat(new BlueprintComponent[]{new InfectiousSwift01447{Window=Ref<BlueprintBuffReference>(window)}}).ToArray();
                variants.Add(swift);bindings.Add(spell.AssetGuid,new Binding{Original=spell,Swift=swift,Window=window,Marker=marker});
            }
            var variantComponent=new AbilityVariants();Set(variantComponent,"m_Variants",variants.Select(Ref<BlueprintAbilityReference>).ToArray());
            var wrapper=f.Ability("InfectiousCharmsAbility01447","Infectious Charms",Description,AbilityType.Supernatural,UnitCommand.CommandType.Swift,AbilityRange.Unlimited,variantComponent);SpellBatch01447.Icon(wrapper,icon);foreach(var swift in variants)Set(swift,"m_Parent",Ref<BlueprintAbilityReference>(wrapper));
            var prerequisite=new PrerequisiteClassLevel{Level=11};Set(prerequisite,"m_CharacterClass",Ref<BlueprintCharacterClassReference>(wizard));
            var root=f.Feature(RootToken,"Infectious Charms",Description,prerequisite,Facts(wrapper));root.Groups=new[]{FeatureGroup.Feat,FeatureGroup.WizardFeat};SpellBatch01447.Icon(root,icon);
            var pools=WizardDiscoveries01435.CorePoolIds.Select(id=>f.Native<BlueprintFeatureSelection>(id)).ToArray();var previous=pools.ToDictionary(p=>p,p=>new[]{Get(p,"m_Features"),Get(p,"m_AllFeatures")});
            Action undo=()=>{foreach(var pair in previous){Set(pair.Key,"m_Features",pair.Value[0]);Set(pair.Key,"m_AllFeatures",pair.Value[1]);}};
            Action connect=()=>T09CombatFeats.Append(pools,new[]{root});Action verify=()=>{foreach(var pool in pools)if(!ReferenceEquals(cache.Load(pool.AssetGuid),pool)||pool.AllFeatures.Count(x=>x==root)!=1||pool.Features.Count(x=>x==root)!=1)throw new InvalidOperationException("01447 Infectious Charms feat selection missing or duplicated.");};
            SpellBatch01447.Publish(ModuleName,f,connect,undo,verify);Bindings.Add(cache,bindings);Reconcile(cache);
        }
        internal static void Reconcile(BlueprintsCache cache) {
            var root=cache.Load(Id(RootToken)) as BlueprintFeature;var pool=cache.Load(BlueprintGuid.Parse(WizardDiscoveries01435.DiscoveryPoolId)) as BlueprintFeatureSelection;
            if(root!=null&&pool?.name=="ArcaneDiscoverySelection")T09CombatFeats.Append(new[]{pool},new[]{root});
        }
        internal static bool Legal(BlueprintAbility spell,UnitEntityData caster,UnitEntityData target) {
            if(target==null||target.State.IsDead||target.State.MarkedForDeath||!target.IsInGame)return false;
            bool side=target==caster?spell.CanTargetSelf:target.IsAlly(caster)?spell.CanTargetFriends:spell.CanTargetEnemies;
            return side&&spell.GetComponents<IAbilityTargetRestriction>().All(r=>r.IsTargetRestrictionPassed(caster,new TargetWrapper(target)));
        }
        internal static void Applied(UnitEntityData target,Buff result) {
            if(result==null||!result.IsActive||!result.IsTurnedOn)return;
            Open(target,result.Context);
        }
        internal static void Open(UnitEntityData target,MechanicsContext context) {
            var cast=context?.SourceAbilityContext;var caster=context?.MaybeCaster;var source=context?.SourceAbility;
            if(caster==null||cast==null||source==null||cast.IsDuplicateSpellApplied||source.GetComponent<InfectiousSwift01447>()!=null||!Bindings.TryGetValue(ResourcesLibrary.BlueprintsCache,out var bindings)||!bindings.TryGetValue(source.AssetGuid,out var binding)||Seen.TryGetValue(cast,out _))return;
            var discovery=ResourcesLibrary.TryGetBlueprint<BlueprintFeature>(Id(RootToken));if(!caster.HasFact(discovery))return;
            var next=GameHelper.GetTargetsAround(target.Position,9.144f,false,false).Where(u=>u!=target&&Legal(binding.Original,caster,u)).OrderBy(u=>(u.Position-target.Position).sqrMagnitude).ThenBy(u=>u.UniqueId).FirstOrDefault();if(next==null)return;
            Seen.Add(cast,new object());var window=caster.Buffs.AddBuff(binding.Window,context,TimeSpan.FromSeconds(3));if(window==null)return;
            var marker=next.Buffs.AddBuff(binding.Marker,context,TimeSpan.FromSeconds(3));if(marker==null){window.Remove();return;}
            window.Components.Single(r=>r.SourceBlueprintComponent is InfectiousWindow01447).GetData<InfectiousWindowData01447>().Marker=marker;
        }
        internal sealed class KillObservation {internal UnitEntityData Target;internal MechanicsContext Context;internal bool Before;}
        internal static KillObservation ObserveKill(ContextActionKill action) {
            if(!Bindings.TryGetValue(ResourcesLibrary.BlueprintsCache,out var map))return null;
            var context=(MechanicsContext)AccessTools.Method(typeof(ContextAction),"get_Context").Invoke(action,null);var target=((TargetWrapper)AccessTools.Method(typeof(ContextAction),"get_Target").Invoke(action,null))?.Unit;
            return target==null||context?.SourceAbility==null||!map.ContainsKey(context.SourceAbility.AssetGuid)?null:new KillObservation{Target=target,Context=context,Before=target.State.MarkedForDeath||target.State.IsDead};
        }
        internal static void Killed(KillObservation state){if(state!=null&&!state.Before&&state.Target.State.MarkedForDeath)Open(state.Target,state.Context);}
        [ThreadStatic] static RuleCastSpell current;
        static readonly ConditionalWeakTable<Buff,RuleCastSpell> Reservations=new ConditionalWeakTable<Buff,RuleCastSpell>();
        internal static Buff Window(UnitEntityData caster,BlueprintBuff blueprint)=>caster?.Buffs.Enumerable.FirstOrDefault(b=>b.Blueprint==blueprint&&b.IsActive&&b.IsTurnedOn&&!b.IsSuppressed&&b.TimeLeft>TimeSpan.Zero&&(!Reservations.TryGetValue(b,out var rule)||rule==current));
        internal static bool Target(Buff window,UnitEntityData target) {
            var marker=window?.Components.Single(r=>r.SourceBlueprintComponent is InfectiousWindow01447).GetData<InfectiousWindowData01447>().Marker.Fact;
            return marker!=null&&marker.IsActive&&marker.IsTurnedOn&&marker.Owner==target&&marker.TimeLeft>TimeSpan.Zero;
        }
        internal sealed class CastScope : IDisposable {
            readonly RuleCastSpell previous;readonly Buff window;
            internal CastScope(RuleCastSpell cast,Buff buff){previous=current;current=cast;window=buff;Reservations.Add(buff,cast);}
            public void Dispose(){current=previous;Reservations.Remove(window);if(window.Manager!=null)window.Remove();}
        }
        internal static CastScope Begin(RuleCastSpell cast) {
            var binding=cast.Spell?.Blueprint.GetComponent<InfectiousSwift01447>();if(binding==null)return null;
            var window=Window(cast.Initiator,binding.Window.Get());
            if(cast.ForceFail||cast.IsDuplicateSpellApplied||cast.Context==null||window==null||Reservations.TryGetValue(window,out _)||!Target(window,cast.SpellTarget?.Unit)){cast.ForceFail=true;return null;}
            cast.Context.SetParams(window.Context.Params.Clone());return new CastScope(cast,window);
        }
    }
    public sealed class InfectiousWindowData01447 {[JsonProperty] public EntityFactRef<Buff> Marker;}
    [TypeId("7aa4cd10e9cb4853a7db447abb020147")]
    public sealed class InfectiousWindow01447 : UnitFactComponentDelegate<InfectiousWindowData01447>
    {
        protected override void OnTurnOff(){var marker=Data.Marker.Fact;Data.Marker=default;if(marker?.Manager!=null)marker.Remove();}
    }
    [TypeId("f4b1c818f1834826b16aaa4f80420147")]
    public sealed class InfectiousSwift01447 : BlueprintComponent,IAbilityTargetRestriction,IAbilityCasterRestriction
    {
        public BlueprintBuffReference Window;
        public bool IsTargetRestrictionPassed(UnitEntityData caster,TargetWrapper target)=>InfectiousCharms01447.Target(InfectiousCharms01447.Window(caster,Window.Get()),target.Unit);
        public string GetAbilityTargetRestrictionUIText(UnitEntityData caster,TargetWrapper target)=>"Infectious Charms (Possible Target)";
        public bool IsCasterRestrictionPassed(UnitEntityData caster)=>InfectiousCharms01447.Window(caster,Window.Get())!=null;
        public string GetAbilityCasterRestrictionUIText()=>"Infectious Charms";
    }
    [HarmonyPatch(typeof(BuffCollection),nameof(BuffCollection.AddBuff),new[]{typeof(BlueprintBuff),typeof(MechanicsContext),typeof(TimeSpan?)})]
    internal static class InfectiousSuccess01447Patch {static void Postfix(BuffCollection __instance,Buff __result)=>InfectiousCharms01447.Applied(__instance.Owner,__result);}
    // Power Word Kill is a native Compulsion with no buff. Observe only its
    // successful native death-mark transition, not cast success or failed HP/SR.
    [HarmonyPatch(typeof(ContextActionKill),nameof(ContextActionKill.RunAction))]
    internal static class InfectiousDeathSuccess01447Patch {
        static void Prefix(ContextActionKill __instance,out InfectiousCharms01447.KillObservation __state)=>__state=InfectiousCharms01447.ObserveKill(__instance);
        static void Postfix(InfectiousCharms01447.KillObservation __state)=>InfectiousCharms01447.Killed(__state);
    }
    [HarmonyPatch(typeof(AbilityData),nameof(AbilityData.CalculateParams))]
    internal static class InfectiousParameters01447Patch
    {
        static bool Prefix(AbilityData __instance,ref AbilityParams __result){var binding=__instance.Blueprint.GetComponent<InfectiousSwift01447>();if(binding==null)return true;var window=InfectiousCharms01447.Window(__instance.Caster.Unit,binding.Window.Get());__result=window?.Context.Params.Clone()??new AbilityParams();return false;}
    }
    [HarmonyPatch(typeof(RuleCastSpell),nameof(RuleCastSpell.OnTrigger))]
    internal static class InfectiousConsume01447Patch
    {
        static void Prefix(RuleCastSpell __instance,out InfectiousCharms01447.CastScope __state)=>__state=InfectiousCharms01447.Begin(__instance);
        static Exception Finalizer(Exception __exception,InfectiousCharms01447.CastScope __state){__state?.Dispose();return __exception;}
    }
}

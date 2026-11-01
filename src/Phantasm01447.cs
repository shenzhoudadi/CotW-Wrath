// Original CotW Features/Subschools.cs createPhantasm / AooMechanics (MIT).
using System;
using System.Linq;
using HarmonyLib;
using Kingmaker;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Prerequisites;
using Kingmaker.Blueprints.Classes.Selection;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.ElementsSystem;
using Kingmaker.Controllers.Combat;
using Kingmaker.EntitySystem;
using Kingmaker.ResourceLinks;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components;
using Kingmaker.UnitLogic.Abilities.Components.Base;
using Kingmaker.UnitLogic.Abilities.Components.AreaEffects;
using Kingmaker.UnitLogic.ActivatableAbilities;
using Kingmaker.UnitLogic.Buffs.Components;
using Kingmaker.UnitLogic.Commands.Base;
using Kingmaker.UnitLogic.FactLogic;
using Kingmaker.UnitLogic.Mechanics.Actions;
using Kingmaker.UnitLogic.Mechanics.Conditions;
using Kingmaker.Utility;
using static CallOfTheWild.Wrath.Prototype.PsychicAssets;
using static CallOfTheWild.Wrath.Prototype.ArchetypeBatch01445;

namespace CallOfTheWild.Wrath.Prototype
{
    internal static class Phantasm01447
    {
        internal const string ModuleName="Wizard school: Phantasm",RootToken="PhantasmSchool01447",SourceId="24d5402c0c1de48468b563f6174c6256";
        internal const string TerrorText="As a standard action, you can make a melee touch attack that causes a creature to be assailed by nightmares only it can see. The creature provokes an attack of opportunity from you or an ally of your choice. Creatures with more Hit Dice than your wizard level are unaffected. This is a mind-affecting fear effect. You can use this ability a number of times per day equal to 3 + your Intelligence modifier.";
        internal const string AuraText="At 8th level, you can emit a 30-foot aura that bedevils your enemies with phantasmal assailants. Enemies within this aura move at half speed, are unable to take attacks of opportunity, and are considered to be flanked. This is a mind-affecting effect. You can use this ability for a number of rounds per day equal to your wizard level. These rounds do not need to be consecutive.";
        internal static void Register(BlueprintsCache cache) {
            if(ArcaneBatch01446.Existing(cache,ModuleName))return;var f=new PsychicAssets(cache);
            var wizard=f.Native<BlueprintCharacterClass>(WizardSubschools01428.WizardId);var pool=f.Native<BlueprintFeatureSelection>(WizardSubschools01428.SchoolPoolId);var source=f.Native<BlueprintProgression>(SourceId);
            var opposition=source.LevelEntries.Single(e=>e.Level==1).Features.OfType<BlueprintFeatureSelection>().ToArray();if(opposition.Length!=2||opposition[0]!=opposition[1])throw new InvalidOperationException("01447 Illusion opposition anchors changed.");
            var terrorIcon=f.Native<BlueprintAbility>("d2aeac47450c76347aebbc02e4f463e0");var auraIcon=f.Native<BlueprintAbility>("b48674cef2bff5e478a007cf57d8345b");
            var uses=SpellBatch01447.Resource(f,"PhantasmTerrorResource01447",wizard,false);var rounds=SpellBatch01447.Resource(f,"PhantasmAuraResource01447",wizard,true);
            var action=new PhantasmTerror01447{Wizard=Ref<BlueprintCharacterClassReference>(wizard)};
            var delivery=new AbilityDeliverTouch();Set(delivery,"m_TouchWeapon",Ref<BlueprintItemWeaponReference>(f.Native<Kingmaker.Blueprints.Items.Weapons.BlueprintItemWeapon>("bb337517547de1a4189518d404ec49d4")));
            var touch=f.Ability("PhantasmTerrorTouch01447","Terror",TerrorText,AbilityType.Supernatural,UnitCommand.CommandType.Standard,AbilityRange.Touch,delivery,Run(action),new SpellDescriptorComponent{Descriptor=SpellDescriptor.MindAffecting},new AbilitySpawnFx{PrefabLink=new PrefabLink{AssetId="49a8069c238b1a8429f2123654d4f45b"},Time=AbilitySpawnFxTime.OnApplyEffect,Anchor=AbilitySpawnFxAnchor.SelectedTarget,PositionAnchor=AbilitySpawnFxAnchor.None,OrientationAnchor=AbilitySpawnFxAnchor.None});touch.CanTargetFriends=touch.CanTargetSelf=false;SpellBatch01447.Icon(touch,terrorIcon);
            var sticky=new AbilityEffectStickyTouch();Set(sticky,"m_TouchDeliveryAbility",Ref<BlueprintAbilityReference>(touch));
            var cast=f.Ability("PhantasmTerrorAbility01447","Terror",TerrorText,AbilityType.Supernatural,UnitCommand.CommandType.Standard,AbilityRange.Touch,sticky,Cost(uses),new SpellDescriptorComponent{Descriptor=SpellDescriptor.MindAffecting});cast.CanTargetFriends=cast.CanTargetSelf=false;SpellBatch01447.Icon(cast,terrorIcon);
            var basic=f.Feature("PhantasmTerrorFeature01447","Terror",TerrorText,Facts(cast),Grant(uses));SpellBatch01447.Icon(basic,terrorIcon);
            var effect=f.Buff("PhantasmAuraEffect01447","Bedeveling Aura",AuraText,new AddCondition{Condition=UnitCondition.Slowed},new AddCondition{Condition=UnitCondition.DisableAttacksOfOpportunity},new PhantasmFlanked01447(),new SpellDescriptorComponent{Descriptor=SpellDescriptor.MindAffecting});SpellBatch01447.Icon(effect,auraIcon);
            var areaApply=new AbilityAreaEffectBuff{CheckConditionEveryRound=true,Condition=new ConditionsChecker{Conditions=new Condition[]{new ContextConditionIsEnemy()}}};Set(areaApply,"m_Buff",Ref<BlueprintBuffReference>(effect));
            var area=f.Add(new BlueprintAbilityAreaEffect{Fx=new PrefabLink{AssetId="dfadb7fa26de0384d9d9a6dabb0bea72"},Size=new Feet(30),Shape=AreaEffectShape.Cylinder,AffectEnemies=true,AggroEnemies=false,SpellResistance=false,ComponentsArray=new BlueprintComponent[]{areaApply}},"PhantasmAuraArea01447","Bedeveling Aura",AuraText);SetEnum(area,"m_TargetType","Any");
            var add=new AddAreaEffect();Set(add,"m_AreaEffect",Ref<BlueprintAbilityAreaEffectReference>(area));var host=f.Buff("PhantasmAuraHost01447","Bedeveling Aura",AuraText,add);SpellBatch01447.Icon(host,auraIcon);
            var toggle=BloodlineMutations01446.Toggle(f,"PhantasmAuraToggle01447","Bedeveling Aura",AuraText,host,rounds,auraIcon,true);toggle.ActivationType=AbilityActivationType.WithUnitCommand;Set(toggle,"m_ActivateWithUnitCommand",UnitCommand.CommandType.Standard);
            var greater=f.Feature("PhantasmAuraFeature01447","Bedeveling Aura",AuraText,Facts(toggle),Grant(rounds));SpellBatch01447.Icon(greater,auraIcon);
            string description="Illusionists use magic to weave confounding images, figments, and phantoms to baffle and vex their foes.\nTerror: "+TerrorText+"\nBedeveling Aura: "+AuraText;
            var root=SpellBatch01447.Progression(f,source,RootToken,"Focused School — Phantasm",description);root.LevelEntries=new[]{Entry(1,basic,opposition[0],opposition[1]),Entry(8,greater)};root.UIGroups=Array.Empty<UIGroup>();Set(root,"m_UIDeterminatorsGroup",new[]{Ref<BlueprintFeatureBaseReference>(basic),Ref<BlueprintFeatureBaseReference>(greater)});SpellBatch01447.Icon(root,terrorIcon);
            var illusionOpposition=f.Native<BlueprintFeature>("6750ead44c0c034428c6509c68110375");
            if(!source.GetComponents<PrerequisiteNoFeature>().Any(c=>c.Feature==illusionOpposition))throw new InvalidOperationException("01447 Illusion opposition prerequisite changed.");
            var old=illusionOpposition.ComponentsArray;var refs=Get(pool,"m_Features");var all=Get(pool,"m_AllFeatures");var no=new PrerequisiteNoFeature();Set(no,"m_Feature",Ref<BlueprintFeatureReference>(root));
            Action undo=()=>{illusionOpposition.ComponentsArray=old;Set(pool,"m_Features",refs);Set(pool,"m_AllFeatures",all);};
            Action connect=()=>{illusionOpposition.ComponentsArray=old.Concat(new[]{no}).ToArray();T09CombatFeats.Append(new[]{pool},new BlueprintFeature[]{root});};
            Action verify=()=>{if(!ReferenceEquals(cache.Load(pool.AssetGuid),pool)||!ReferenceEquals(cache.Load(illusionOpposition.AssetGuid),illusionOpposition)||pool.AllFeatures.Count(x=>x==root)!=1||pool.Features.Count(x=>x==root)!=1||illusionOpposition.GetComponents<PrerequisiteNoFeature>().Count(c=>c.Feature==root)!=1)throw new InvalidOperationException("01447 Phantasm selection link missing or duplicated.");};
            SpellBatch01447.Publish(ModuleName,f,connect,undo,verify);
        }
    }
    [TypeId("5d49147bda184e839bac46aa14180147")]
    public sealed class PhantasmTerror01447 : ContextAction
    {
        public BlueprintCharacterClassReference Wizard;
        public override string GetCaption()=>"Target provokes AoO";
        public override void RunAction() {
            var caster=Context.MaybeCaster;var target=Target.Unit;if(caster==null||target==null||target.Progression.CharacterLevel>caster.Progression.GetClassLevel(Wizard.Get()))return;
            foreach(var unit in target.CombatState.EngagedBy.Where(u=>u!=caster).Take(100).ToArray())Game.Instance.CombatEngagementController.ForceAttackOfOpportunity(unit,target);
        }
    }
    [TypeId("afbf8f95dda440f19dc74d0cfa330147")]
    public sealed class PhantasmFlanked01447 : UnitFactComponentDelegate {}
    [HarmonyPatch(typeof(UnitCombatState),"get_IsFlanked")]
    internal static class PhantasmFlanking01447Patch
    {
        static void Postfix(UnitCombatState __instance,ref bool __result){if(!__result&&!__instance.Unit.State.Features.CannotBeFlanked&&__instance.Unit.Buffs.Enumerable.Any(b=>b.IsActive&&b.IsTurnedOn&&b.Blueprint.GetComponent<PhantasmFlanked01447>()!=null))__result=true;}
    }
}

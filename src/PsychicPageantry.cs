using System;
using System.Linq;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Selection;
using Kingmaker.Designers.Mechanics.Facts;
using Kingmaker.ElementsSystem;
using Kingmaker.ResourceLinks;
using Kingmaker.UnitLogic.Abilities.Components.Base;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Enums;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components;
using Kingmaker.UnitLogic.ActivatableAbilities;
using Kingmaker.UnitLogic.Commands.Base;
using Kingmaker.UnitLogic.FactLogic;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Mechanics.Actions;
using Kingmaker.UnitLogic.Mechanics.Components;
using static CallOfTheWild.Wrath.Prototype.PsychicAssets;

namespace CallOfTheWild.Wrath.Prototype
{
    internal static class PsychicPageantry
    {
        private const string RitualText="As a standard action, assist another ally within touch range with a chosen skill. You attempt a DC 20 check in that skill. On success, the ally gains a +4 bonus on their next check with that skill and you regain 1 phrenic point, up to your maximum. The bonus ends after that check or on resting. Uses per day equal your Charisma modifier (minimum 0); failed attempts also spend a use.";
        private const string PowerText="Spend 1 phrenic point as a move action to gain +1 caster level and +1 saving throw DC for your next Psychic spell cast before the end of your turn (within 1 round in real-time combat). The preparation ends after the cast, even if concentration fails. Other spellbooks and magic items do not use it. Repeated uses replace the preparation; they do not stack.";
        private const string FocusText="While enabled, automatically spend 1 phrenic point before each concentration check to treat its d20 roll as 20. This is not automatic success: the final result must still meet the DC. With no phrenic points, roll normally. This free-action toggle takes effect immediately and can be used together with a phrenic amplification.";
        internal static BlueprintProgression Build(PsychicAssets f,BlueprintCharacterClass character)
        {
            var icon=f.Native<BlueprintAbility>("a5e23522eda32dc45801e32c05dc9f96").Icon;
            var powerBuff=f.Buff("PageantryPowerBuff","Power from Pageantry",PowerText,new PsychicPageantryPower {name="PageantryNextPsychicCast"});Set(powerBuff,"m_Icon",icon);
            var apply=Apply(powerBuff,1,toCaster:true,fromSpell:false);apply.IsNotDispelable=true;
            var power=f.Ability("PageantryPowerAbility","Power from Pageantry",PowerText,AbilityType.Supernatural,UnitCommand.CommandType.Move,AbilityRange.Personal,Run(apply),Cost(f.Native<BlueprintAbilityResource>(PsychicClass.CharismaPoolId)),Visual("0c78f73f0b9b3134d880ad7dbde5304c"));
            Set(power,"m_Icon",icon);power.LocalizedDuration=Text("PageantryPowerAbility","duration","Until end of turn (1 round in real time), or next Psychic cast");
            var powerFeature=f.Feature("PageantryPowerFeature","Power from Pageantry",PowerText,Facts(power));Set(powerFeature,"m_Icon",icon);
            var focusIcon=f.Native<BlueprintFeature>("06964d468fde1dc4aa71a92ea04d930d").Icon;
            var focusBuff=f.Buff("PageantryFocusBuff","Unrivaled Focus",FocusText);Set(focusBuff,"m_Icon",focusIcon);
            var toggle=f.Add(new BlueprintActivatableAbility {ActivationType=AbilityActivationType.Immediately,DeactivateImmediately=true,DeactivateIfOwnerDisabled=true,DeactivateIfOwnerUnconscious=true},"PageantryFocusToggle","Unrivaled Focus",FocusText);
            Set(toggle,"m_Buff",Ref<BlueprintBuffReference>(focusBuff));Set(toggle,"m_ActivateWithUnitCommand",UnitCommand.CommandType.Free);Set(toggle,"m_Icon",focusIcon);
            var focus=f.Feature("PageantryFocusFeature","Unrivaled Focus",FocusText,Facts(toggle));Set(focus,"m_Icon",focusIcon);
            var spells=new[]{f.Native<BlueprintAbility>("90e59f4a4ada87243b7b3535a06d0638"),f.Find<BlueprintAbility>("Silence"),f.Native<BlueprintAbility>("f492622e473d34747806bdb39356eb89"),f.Native<BlueprintAbility>("4b8265132f9c8174f87ce7fa6d0fe47b"),f.Native<BlueprintAbility>("cb15cc8d7a5480648855a23b3ba3f93d"),f.Native<BlueprintAbility>("e15e5e7045fda2244b98c8f010adfe31"),f.Native<BlueprintAbility>("defbbeaef79eda64abc645036228a31b"),f.Native<BlueprintAbility>("e788b02f8d21014488067bdd3ba7b325"),f.Native<BlueprintAbility>("43740dab07286fe4aa00a6ee104ce7c1")};
            var names=new[]{"Bless","Silence","Slow","Rainbow Pattern","Greater Command","Greater Heroism","Mass Hold Person","Frightful Aspect","Heroic Invocation"};
            var levels=new[]{1,4,6,8,10,12,14,16,18};
            var bonuses=spells.Select((spell,i)=>{var add=new AddKnownSpell {name="PageantryBonusSpell"+(i+1),SpellLevel=i+1};Set(add,"m_CharacterClass",Ref<BlueprintCharacterClassReference>(character));Set(add,"m_Spell",Ref<BlueprintAbilityReference>(spell));var feature=f.Feature("PageantryBonusSpell"+(i+1),"Discipline Spell: "+names[i],"Learn "+names[i]+" in addition to your normal Psychic spells known.",add);Set(feature,"m_Icon",spell.Icon);return feature;}).ToArray();
            var ritual=Ritual(f);
            var result=f.Add(new BlueprintProgression {Ranks=1,IsClassFeature=true,UIGroups=Array.Empty<UIGroup>(),LevelEntries=levels.Select((level,i)=>Entry(level,i==0?new BlueprintFeatureBase[]{bonuses[i],ritual,f.Native<BlueprintFeature>(PsychicClass.CharismaFeatureId)}:new BlueprintFeatureBase[]{bonuses[i]})).Concat(new[]{Entry(5,powerFeature),Entry(13,focus)}).OrderBy(e=>e.Level).ToArray()},"PageantryDiscipline","Pageantry","Elaborate rituals unlock your psychic power. Gain Ritual Unity, Power from Pageantry, Unrivaled Focus and nine bonus spells. Your phrenic pool is half your psychic level plus your Charisma modifier. Uses the Call of the Wild adaptation of this discipline.");
            Set(result,"m_Classes",new[]{new BlueprintProgression.ClassWithLevel {m_Class=Ref<BlueprintCharacterClassReference>(character)}});Set(result,"m_Icon",icon);return result;
        }
        private static BlueprintFeature Ritual(PsychicAssets f)
        {
            var resource=f.Add(new BlueprintAbilityResource(),"PageantryRitualUses","Ritual Unity",RitualText);var amount=Get(resource,"m_MaxAmount");
            Set(amount,"IncreasedByStat",true);Set(amount,"ResourceBonusStat",StatType.Charisma);Set(amount,"m_Class",Array.Empty<BlueprintCharacterClassReference>());Set(amount,"m_Archetypes",Array.Empty<BlueprintArchetypeReference>());Set(amount,"m_ClassDiv",Array.Empty<BlueprintCharacterClassReference>());Set(amount,"m_ArchetypesDiv",Array.Empty<BlueprintArchetypeReference>());Set(resource,"m_MaxAmount",amount);
            var donor=f.Native<BlueprintFeatureSelection>("c9629ef9eebb88b479b2fbc5e836656a");
            var variants=donor.AllFeatures.Select(feature=>{
                var stat=feature.GetComponent<AddContextStatBonus>().Stat;string token="PageantryRitual"+stat,title="Ritual Unity: "+SkillName(stat);
                var skills=stat==StatType.SkillPersuasion?new[]{stat,StatType.CheckBluff,StatType.CheckDiplomacy,StatType.CheckIntimidate}:new[]{stat};
                var components=skills.SelectMany(s=>new BlueprintComponent[]{new AddInitiatorSkillRollTrigger {name=token+"Consume"+s,Skill=s,OnlySuccess=false,Action=Actions(new ContextActionRemoveSelf())},new AddInitiatorPartySkillRollTrigger {name=token+"PartyConsume"+s,Skill=s,OnlySuccess=false,Action=Actions(new ContextActionRemoveSelf())}}).Concat(new BlueprintComponent[]{new AddStatBonus {name=token+"Bonus",Stat=stat,Value=4,Descriptor=ModifierDescriptor.UntypedStackable}}).ToArray();
                var buff=f.Buff(token+"Buff",title,RitualText,components);Set(buff,"m_Icon",feature.Icon);SetEnum(buff,"m_Flags","RemoveOnRest");
                var apply=Apply(buff,0,fromSpell:false);apply.Permanent=true;apply.IsNotDispelable=true;
                var check=new ContextActionSkillCheck {name=token+"CasterCheck",Stat=stat,CheckForCaster=true,UseCustomDC=true,CustomDC=20,Success=Actions(apply,new PsychicRitualUnityRestore()),Failure=Actions(),FailureDiffMoreOrEqual5Less10=Actions(),FailureDiffMoreOrEqual10=Actions()};
                var ability=f.Ability(token+"Ability",title,RitualText,AbilityType.Supernatural,UnitCommand.CommandType.Standard,AbilityRange.Touch,Run(check),Cost(resource),Visual("d119d19888a8f964b8acc5dfce6ea9e9"));
                ability.CanTargetSelf=false;ability.CanTargetFriends=true;ability.CanTargetEnemies=false;Set(ability,"m_Icon",feature.Icon);ability.LocalizedDuration=Text(token,"duration","Until next matching skill check or rest");return ability;
            }).ToArray();
            var choices=new AbilityVariants {name="PageantryRitualSkillChoices"};Set(choices,"m_Variants",variants.Select(Ref<BlueprintAbilityReference>).ToArray());
            var root=f.Ability("PageantryRitualAbility","Ritual Unity",RitualText,AbilityType.Supernatural,UnitCommand.CommandType.Standard,AbilityRange.Touch,choices);root.CanTargetSelf=false;root.CanTargetEnemies=false;Set(root,"m_Icon",donor.Icon);root.LocalizedDuration=Text("PageantryRitualAbility","duration","Until next matching skill check or rest");
            foreach(var v in variants) Set(v,"m_Parent",Ref<BlueprintAbilityReference>(root));
            var grant=new AddAbilityResources {name="PageantryDailyRitualUses",RestoreAmount=true,RestoreOnLevelUp=true};Set(grant,"m_Resource",Ref<BlueprintAbilityResourceReference>(resource));
            var result=f.Feature("PageantryRitualFeature","Ritual Unity",RitualText,Facts(root),grant);Set(result,"m_Icon",donor.Icon);return result;
        }
        private static string SkillName(StatType stat) {
            switch(stat) {
                case StatType.SkillKnowledgeArcana:return "Knowledge (Arcana)";
                case StatType.SkillKnowledgeWorld:return "Knowledge (World)";
                case StatType.SkillLoreNature:return "Lore (Nature)";
                case StatType.SkillLoreReligion:return "Lore (Religion)";
                case StatType.SkillUseMagicDevice:return "Use Magic Device";
                default:return stat.ToString().Replace("Skill", "");
            }
        }
        private static AbilitySpawnFx Visual(string id)=>new AbilitySpawnFx {name="PageantryNativeVisual",PrefabLink=new PrefabLink {AssetId=id},Time=AbilitySpawnFxTime.OnApplyEffect,Anchor=AbilitySpawnFxAnchor.Caster,PositionAnchor=AbilitySpawnFxAnchor.None,OrientationAnchor=AbilitySpawnFxAnchor.None};
        private static AbilityResourceLogic Cost(BlueprintAbilityResource resource) {var result=new AbilityResourceLogic {name="PageantryResourceCost",Amount=1};Set(result,"m_RequiredResource",Ref<BlueprintAbilityResourceReference>(resource));Set(result,"m_IsSpendResource",true);return result;}
        private static LevelEntry Entry(int level,params BlueprintFeatureBase[] features) {var result=new LevelEntry {Level=level};result.SetFeatures(features);return result;}
    }
}

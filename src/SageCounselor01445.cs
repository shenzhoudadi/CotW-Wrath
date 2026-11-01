// Adapted from Holic75/KingmakerRebalance (MIT), Archetypes/SageCounselor.cs.
using System;
using System.Linq;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Prerequisites;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.Designers.Mechanics.Buffs;
using Kingmaker.Designers.Mechanics.Facts;
using Kingmaker.ElementsSystem;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.PubSubSystem;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components;
using Kingmaker.UnitLogic.Abilities.Components.CasterCheckers;
using Kingmaker.UnitLogic.ActivatableAbilities;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Buffs.Components;
using Kingmaker.UnitLogic.Class.LevelUp;
using Kingmaker.UnitLogic.Commands.Base;
using Kingmaker.UnitLogic.FactLogic;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Mechanics.Actions;
using Kingmaker.UnitLogic.Mechanics.Components;
using Kingmaker.UnitLogic.Mechanics.Conditions;
using static CallOfTheWild.Wrath.Prototype.PsychicAssets;
using static CallOfTheWild.Wrath.Prototype.ArchetypeBatch01445;

namespace CallOfTheWild.Wrath.Prototype
{
    internal static class SageCounselor01445
    {
        internal const string ModuleName="Monk archetype: Sage Counselor",RootToken="SageCounselorArchetype01445",ClassId="e8f21e5b58e0569468e420ebea456124",FlurryId="332362f3bd39ebe46a740a36960fdcb4";
        internal static string T(string s)=>"SageCounselor"+s+"01445";
        internal static readonly string[] OwnTokens=new[]{RootToken,T("CunningFist1"),T("CunningFist2"),T("CunningFist6"),T("DeceptiveKi"),T("KiFeint"),T("DeceptiveKiAbility"),T("DeceptiveKiBuff"),T("FeintingFlurry"),T("FeintingFlurryToggle"),T("FeintingFlurryBuff")};
        internal static readonly string[] WaiverIds={"0f15c6f70d8fb2b49aa6cc24239cc5fa","4cc71ae82bdd85b40b3cfe6697bb7949","25bc9c439ac44fd44ac3b1e58890916f","ed699d64870044b43bb5a7fbe3f29494","52c6b07a68940af41b270b3710682dc7","63d8e3a9ab4d72e4081a7862d7246a79","c610310d31414edabcedf0c8a6fe32c4","32429740d6a5470aaaa02f20d61e43d3"};
        internal static void Register(BlueprintsCache cache)
        {
            var f=new PsychicAssets(cache);var cls=f.Native<BlueprintCharacterClass>(ClassId);if(Existing(cache,RootToken,cls))return;Vacant(cache,OwnTokens);
            var expertise=f.Native<BlueprintFeature>("4c44724ffa8844f4d9bedb5bb27d144a");var feint=f.Native<BlueprintFeature>("c610310d31414edabcedf0c8a6fe32c4");var greater=f.Native<BlueprintFeature>("32429740d6a5470aaaa02f20d61e43d3");var flurry=f.Native<BlueprintFeature>(FlurryId);
            const string cunning="A sage counselor gains Combat Expertise as a bonus feat, even if he doesn’t meet the prerequisites, and he can ignore the Intelligence prerequisite on feats that have Combat Expertise as a prerequisite. At 2nd level, he gains Improved Feint, and at 6th level, he gains Greater Feint, even if he doesn’t meet the prerequisites.";
            var fists=new[]{expertise,feint,greater}.Select((feat,i)=>{var v=f.Feature(T("CunningFist"+new[]{1,2,6}[i]),"Cunning Fist",cunning,Facts(feat));Set(v,"m_Icon",feat.Icon);return v;}).ToArray();
            var nativeKi=f.Native<BlueprintFeature>("e9590244effb4be4f830b1e3fffced13");var resource=nativeKi.GetComponent<AddAbilityResources>()?.Resource??throw new InvalidOperationException("Native Monk ki resource missing.");
            const string kiText="A sage counselor can spend 1 ki point while performing a flurry of blows to feint an opponent as a swift action, but he can’t spend 1 ki point to make an additional attack when making a flurry of blows.\nIn addition, the sage counselor can spend 1 point from his ki pool as a swift action to give himself a +4 insight bonus on his next Bluff check. The sage counselor does not gain the ability to spend ki to increase his speed by 20 feet for 1 round.";
            var kiFeint=f.Clone(f.Native<BlueprintAbility>(SpellAndSwordFeats.FeintAbility),T("KiFeint"),"Ki Feint","A sage counselor can spend 1 ki point while performing a flurry of blows to feint an opponent as a swift action, but he can’t spend 1 ki point to make an additional attack when making a flurry of blows.");kiFeint.Type=AbilityType.Supernatural;kiFeint.ActionType=UnitCommand.CommandType.Swift;kiFeint.ComponentsArray=kiFeint.ComponentsArray.Concat(new BlueprintComponent[]{Cost(resource),new AbilityCasterHasFacts()}).ToArray();Set(kiFeint.ComponentsArray.OfType<AbilityCasterHasFacts>().Last(),"m_Facts",new[]{Ref<BlueprintUnitFactReference>(flurry)});Set(kiFeint,"m_Icon",PsychicOriginalIcons.Load("FeintingFlurry"));
            var buff=f.Buff(T("DeceptiveKiBuff"),"Deceptive Ki","At 3rd level, the sage counselor can spend 1 point from his ki pool as a swift action to give himself a +4 insight bonus on his next Bluff check.",new AddStatBonus{Stat=StatType.CheckBluff,Value=4,Descriptor=Kingmaker.Enums.ModifierDescriptor.Insight});Set(buff,"m_Icon",f.Native<BlueprintAbility>("b3da3fbee6a751d4197e446c7e852bcb").Icon);
            var remove=new ContextActionRemoveBuff{ToCaster=true};Set(remove,"m_Buff",Ref<BlueprintBuffReference>(buff));buff.ComponentsArray=buff.ComponentsArray.Concat(new BlueprintComponent[]{new AddInitiatorSkillRollTrigger{Skill=StatType.CheckBluff,Action=Actions(remove)},new AddInitiatorPartySkillRollTrigger{Skill=StatType.CheckBluff,Action=Actions(remove)}}).ToArray();
            var apply=Apply(buff,0,fromSpell:false);apply.Permanent=true;apply.IsNotDispelable=true;var kiAbility=f.Ability(T("DeceptiveKiAbility"),"Deceptive Ki","At 3rd level, the sage counselor can spend 1 point from his ki pool as a swift action to give himself a +4 insight bonus on his next Bluff check.",AbilityType.Supernatural,UnitCommand.CommandType.Swift,AbilityRange.Personal,Cost(resource),Run(apply));Set(kiAbility,"m_Icon",buff.Icon);
            // Reuse both native pre/post Feint triggers, including the cloak
            // conditional and cleanup. Private copies recognize only Ki Feint;
            // ordinary Feint and its native feature remain unchanged.
            var feintTriggers=feint.GetComponents<AddAbilityUseTrigger>().Where(c=>c.ForOneSpell&&c.Ability?.AssetGuid==BlueprintGuid.Parse(SpellAndSwordFeats.FeintAbility)).Select(c=>{var copy=CopyData(c);Set(copy,"m_Ability",Ref<BlueprintAbilityReference>(kiFeint));return copy;}).ToArray();
            if(feintTriggers.Length!=2||feintTriggers.Count(c=>c.AfterCast)!=1)throw new InvalidOperationException("Native Feint pre/post triggers missing.");
            var ki=f.Clone(nativeKi,T("DeceptiveKi"),"Deceptive Ki",kiText);ki.ComponentsArray=ki.ComponentsArray.Where(c=>!(c is AddFacts)).Concat(new BlueprintComponent[]{Facts(kiFeint,kiAbility)}).Concat(feintTriggers).ToArray();Set(ki,"m_Icon",buff.Icon);
            const string flurryText="At 10th level, sage counselor can choose to replace his first attack during a flurry of blows with a feint check.";
            var has=new ContextConditionCasterHasFact();Set(has,"m_Fact",Ref<BlueprintUnitFactReference>(flurry));
            // Native Two-Weapon Feint supplies the supported attack-sacrifice
            // and first-attack paths; only the eligibility becomes actual flurry.
            var source=f.Native<BlueprintBuff>("621523572cfe4dc48ee1bdddba2f4725");var reduction=CopyData(source.GetComponent<ReduceAttacksCount>()??throw new InvalidOperationException("Native attack sacrifice missing."));reduction.Condition=new ConditionsChecker{Conditions=new Condition[]{has}};
            var attack=CopyData(source.GetComponent<AddInitiatorAttackWithWeaponTrigger>()??throw new InvalidOperationException("Native first-attack feint trigger missing."));attack.OnlyHit=false;attack.Action=Actions(new Kingmaker.Designers.EventConditionActionSystem.Actions.Conditional{ConditionsChecker=new ConditionsChecker{Conditions=new Condition[]{CopyData(has)}},IfTrue=Actions(new ContextActionCastSpell()),IfFalse=Actions()});
            // Use the free original Feint graph here, not the paid swift Ki Feint.
            var cast=(ContextActionCastSpell)((Kingmaker.Designers.EventConditionActionSystem.Actions.Conditional)attack.Action.Actions[0]).IfTrue.Actions[0];Set(cast,"m_Spell",Ref<BlueprintAbilityReference>(f.Native<BlueprintAbility>(SpellAndSwordFeats.FeintAbility)));
            var flurryBuff=f.Buff(T("FeintingFlurryBuff"),"Feinting Flurry",flurryText,reduction,attack);Set(flurryBuff,"m_Icon",kiFeint.Icon);var toggle=f.Add(new BlueprintActivatableAbility{ActivationType=AbilityActivationType.Immediately,DoNotTurnOffOnRest=true,DeactivateIfCombatEnded=false},T("FeintingFlurryToggle"),"Feinting Flurry",flurryText);Set(toggle,"m_Buff",Ref<BlueprintBuffReference>(flurryBuff));Set(toggle,"m_Icon",kiFeint.Icon);var feature=f.Feature(T("FeintingFlurry"),"Feinting Flurry",flurryText,Facts(toggle));Set(feature,"m_Icon",kiFeint.Icon);
            var root=f.Add(new BlueprintArchetype{LocalizedName=Text(RootToken,"name","Sage Counselor"),LocalizedDescription=Text(RootToken,"description","Sage counselors are ascetics and mystics who leave the confines of the monastery walls to advise secular people about spiritual truths and to seek knowledge of the outside world. They often find work as mentors teaching religion and martial arts, and some of them even become counselors for people in high places. Sage counselors often speak in metaphors, knowing that indirect persuasion is more effective than speaking plainly, and they use indirect tactics in combat as well."),RemoveFeatures=new[]{Entry(1,f.Native<BlueprintFeature>("ac3b7f5c11bce4e44aeb332f66b75bab")),Entry(2,f.Native<BlueprintFeature>("ac3b7f5c11bce4e44aeb332f66b75bab")),Entry(3,nativeKi),Entry(4,f.Native<BlueprintFeature>("3049386713ff04245a38b32483362551")),Entry(6,f.Native<BlueprintFeature>("b993f42cb119b4f40ac423ae76394374")),Entry(10,f.Native<BlueprintFeature>("1051170c612d5b844bfaa817d6f4cfff"))},AddFeatures=new[]{Entry(1,fists[0]),Entry(2,fists[1]),Entry(3,ki),Entry(6,fists[2]),Entry(10,feature)}},RootToken,"Sage Counselor","");Set(root,"m_ParentClass",cls);
            var currentFeats=f.Native<Kingmaker.Blueprints.Classes.Selection.BlueprintFeatureSelection>(BlueprintRegistration.BasicFeatsId).AllFeatures.Where(bp=>bp!=null&&bp.GetComponents<PrerequisiteFeature>().Any(p=>p.Feature?.AssetGuid==expertise.AssetGuid));
            var nativeTargets=WaiverIds.Select(f.Native<BlueprintFeature>).Concat(new[]{f.Native<BlueprintFeature>(BlueprintRegistration.ImprovedCombatExpertiseId),f.Native<BlueprintFeature>(ImprovedTwoWeaponFeint.FeatId.ToString())}).Concat(new[]{"FellingSmashFeat","SwordplayStyleFeat","SwordplayUpsetFeat"}.Select(t=>f.Native<BlueprintFeature>(Id(t).ToString()))).Concat(currentFeats).Distinct().ToArray();
            var replacements=nativeTargets.Where(bp=>bp.GetComponents<PrerequisiteStatValue>().Any(p=>p.Stat==StatType.Intelligence)).Select(bp=>new{Target=bp,Old=bp.ComponentsArray,New=bp.ComponentsArray.Select(c=>c is PrerequisiteStatValue p&&p.Stat==StatType.Intelligence?(BlueprintComponent)new SageCounselorIntelligence01445{Stat=p.Stat,Value=p.Value,Group=p.Group,CheckInProgression=p.CheckInProgression,HideInUI=p.HideInUI,CunningFist=Ref<BlueprintFeatureReference>(fists[0]),name=p.name,OwnerBlueprint=bp}:c).ToArray()}).ToArray();
            Func<Action> snapshot=()=>{var before=replacements.Select(x=>x.Target.ComponentsArray).ToArray();return ()=>{for(int i=0;i<before.Length;i++)replacements[i].Target.ComponentsArray=before[i];};};Action connect=()=>{foreach(var x in replacements){if(!ReferenceEquals(cache.Load(x.Target.AssetGuid),x.Target)||!ReferenceEquals(x.Target.ComponentsArray,x.Old)&&!ReferenceEquals(x.Target.ComponentsArray,x.New))throw new InvalidOperationException("Sage Counselor prerequisite graph changed.");x.Target.ComponentsArray=x.New;}};
            Publish(cache,f,OwnTokens,cls,root,new BlueprintFeatureBase[]{fists[0],fists[1],fists[2],ki,feature},snapshot,connect);
        }
    }
    [TypeId("60938e5dc0424132b0b661f785990204")]
    public sealed class SageCounselorIntelligence01445:PrerequisiteStatValue
    {public BlueprintFeatureReference CunningFist;protected override bool CheckInternal(FeatureSelectionState selectionState,UnitDescriptor unit,LevelUpState state)=>unit.HasFact(CunningFist.Get())||base.CheckInternal(selectionState,unit,state);}
}

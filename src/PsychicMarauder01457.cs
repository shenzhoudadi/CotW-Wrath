// Adapted from Holic75/KingmakerRebalance Psychic.createPsychicMaraudeur (MIT).
using System;
using System.Linq;
using System.Collections.Generic;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.Blueprints.Facts;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.Designers.EventConditionActionSystem.Actions;
using Kingmaker.Designers.Mechanics.Facts;
using Kingmaker.ElementsSystem;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Enums;
using Kingmaker.ResourceLinks;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components;
using Kingmaker.UnitLogic.Abilities.Components.AreaEffects;
using Kingmaker.UnitLogic.Abilities.Components.Base;
using Kingmaker.UnitLogic.Abilities.Components.CasterCheckers;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Buffs.Components;
using Kingmaker.UnitLogic.Commands.Base;
using Kingmaker.UnitLogic.FactLogic;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Mechanics.Actions;
using Kingmaker.UnitLogic.Mechanics.Components;
using Kingmaker.UnitLogic.Mechanics.Conditions;
using Kingmaker.UnitLogic.Parts;
using Kingmaker.Utility;
using static CallOfTheWild.Wrath.Prototype.PsychicAssets;
using static CallOfTheWild.Wrath.Prototype.ArchetypeBatch01445;

namespace CallOfTheWild.Wrath.Prototype
{
    internal static class PsychicMarauder01457
    {
        internal const string ModuleName="Psychic archetype: Psychic Marauder",RootToken="PsychicMarauderArchetype01457",ConfusionId="886c7407dc629dc499b9f1465ff382df";
        internal static readonly string[] Modes={"I","II","IIIStill","IIIHarm","IIIAttack"};
        internal static readonly string[] Pools={"Wisdom","Charisma"};
        internal static readonly string[] OwnTokens=Tokens();
        static string T(string name)=>"Marauder"+name+"01457";
        static string[] Tokens(){var t=new List<string>{RootToken,T("Skewed"),T("Cracked"),T("Unreal"),T("AuraI"),T("AuraII"),T("AuraIII")};
            foreach(var mode in Modes){t.Add(T(mode+"Member"));if(mode!="I")t.Add(T(mode+"Control"));}
            for(int i=0;i<6;i++){
                foreach(var mode in Modes){t.Add(T(mode+i+"Area"));t.Add(T(mode+i+"Aura"));foreach(var pool in Pools)t.Add(T(mode+i+pool+"Ability"));}
                foreach(var pool in Pools)t.Add(T("III"+i+pool+"Wrapper"));
                foreach(var tier in new[]{"I","II","III"}){t.Add(T(tier+i+"PoolGrant"));if(i<5)t.Add(T(tier+i+"LevelWindow"));}
            }return t.ToArray();}
        static string N(string key)=>PsychicOriginalTexts01457.Name(key);
        static string D(string key)=>PsychicOriginalTexts01457.Description(key);
        static string Key(string mode)=>"AuraOfInsanity"+(mode.StartsWith("III")?"III":mode)+"Feature";
        static void Icon(BlueprintUnitFact dst,BlueprintUnitFact src)=>Set(dst,"m_Icon",Get(src,"m_Icon"));
        static ContextCalculateAbilityParamsBasedOnClass Params(BlueprintCharacterClass cls){var x=new ContextCalculateAbilityParamsBasedOnClass{StatType=StatType.Intelligence};Set(x,"m_CharacterClass",Ref<BlueprintCharacterClassReference>(cls));return x;}
        static AddFeatureOnClassLevel At(BlueprintCharacterClass cls,BlueprintFeature feat,int level,bool before=false){var x=new AddFeatureOnClassLevel{Level=level,BeforeThisLevel=before};Set(x,"m_Class",Ref<BlueprintCharacterClassReference>(cls));Set(x,"m_Feature",Ref<BlueprintFeatureReference>(feat));Set(x,"m_AdditionalClasses",Array.Empty<BlueprintCharacterClassReference>());Set(x,"m_Archetypes",Array.Empty<BlueprintArchetypeReference>());return x;}
        static BlueprintFeature Hidden(PsychicAssets f,string token,params BlueprintComponent[] parts){var feat=f.Feature(token,"","",parts);feat.HideInUI=true;feat.HideInCharacterSheetAndLevelUp=true;return feat;}
        static SpellDescriptorComponent Tags()=>new SpellDescriptorComponent{Descriptor=SpellDescriptor.MindAffecting|SpellDescriptor.Compulsion|SpellDescriptor.Confusion};
        static AbilityAoERadius Radius(int feet){var c=new AbilityAoERadius();Set(c,"m_Radius",feet.Feet());Set(c,"m_TargetType",TargetType.Enemy);return c;}
        static BlueprintComponent[] SkewedMentality()
        {
            // SavingThrow is not IModifiableValueReplaceBaseStat in current
            // Wrath, so ReplaceStatBaseAttribute silently does nothing. Native
            // context modifiers subtract Wisdom and add Charisma, including
            // negative modifiers, and refresh when either attribute changes.
            ContextRankConfig RankFor(StatType stat,AbilityRankType type){var r=new ContextRankConfig();Set(r,"m_Type",type);SetEnum(r,"m_BaseValueType","StatBonus");Set(r,"m_Stat",stat);Set(r,"m_UseMin",false);Set(r,"m_UseMax",false);return r;}
            return new BlueprintComponent[]{RankFor(StatType.Charisma,AbilityRankType.Default),RankFor(StatType.Wisdom,AbilityRankType.StatBonus),new AddContextStatBonus{Stat=StatType.SaveWill,Descriptor=ModifierDescriptor.UntypedStackable,Value=new ContextValue{ValueType=ContextValueType.Rank,ValueRank=AbilityRankType.Default}},new AddContextStatBonus{Stat=StatType.SaveWill,Descriptor=ModifierDescriptor.UntypedStackable,Multiplier=-1,Value=new ContextValue{ValueType=ContextValueType.Rank,ValueRank=AbilityRankType.StatBonus}},new RecalculateOnStatChange{Stat=StatType.Charisma},new RecalculateOnStatChange{Stat=StatType.Wisdom}};
        }
        internal static void Register(BlueprintsCache cache)
        {
            var f=new PsychicAssets(cache);var cls=f.Native<BlueprintCharacterClass>(PsychicClass.ClassId);
            if(Existing(cache,RootToken,cls))return;Vacant(cache,OwnTokens);
            var amp=(BlueprintFeature)cache.Load(Id("PhrenicAmplificationSelection"));var mastery=(BlueprintFeature)cache.Load(Id("PhrenicMasteryFeature"));
            if(amp==null||mastery==null||new[]{3,11,19}.Any(level=>!cls.Progression.LevelEntries.Any(e=>e.Level==level&&e.Features.Contains(amp))))throw new InvalidOperationException("Psychic Marauder requires the complete Psychic amplification and mastery progression.");
            var confused=f.Native<BlueprintBuff>(ConfusionId);var areaTemplate=f.Native<BlueprintAbilityAreaEffect>("7fc8dbff8ba688d4b864b1c1be45fe97");
            var skewed=f.Feature(T("Skewed"),N("SkewedMentalityFeature"),D("SkewedMentalityFeature"),SkewedMentality());Icon(skewed,f.Native<BlueprintAbility>("eabf94e4edc6e714cabd96aa69f8b207"));
            BlueprintComponent[] Immunity(SpellDescriptor descriptor,bool condition){var spell=new SpellImmunityToSpellDescriptor{Descriptor=descriptor};var buff=new BuffDescriptorImmunity{Descriptor=descriptor};var list=new List<BlueprintComponent>{spell,buff};if(condition)list.Add(new AddConditionImmunity{Condition=UnitCondition.Confusion});return list.ToArray();}
            var cracked=f.Feature(T("Cracked"),N("CrackedPerspectivesFeature"),D("CrackedPerspectivesFeature"),Immunity(SpellDescriptor.Confusion,true));Icon(cracked,f.Native<BlueprintAbility>("eabf94e4edc6e714cabd96aa69f8b207"));
            var unreal=f.Feature(T("Unreal"),N("UnrealUnderstandingFeature"),D("UnrealUnderstandingFeature"),Immunity(SpellDescriptor.MindAffecting,false));Icon(unreal,f.Native<BlueprintAbility>("b3da3fbee6a751d4197e446c7e852bcb"));
            var tiers=new Dictionary<string,BlueprintFeature>();foreach(string tier in new[]{"I","II","III"}){var feat=f.Feature(T("Aura"+tier),N(Key(tier)),D(Key(tier)));Icon(feat,confused);tiers.Add(tier,feat);}
            var members=new Dictionary<string,BlueprintBuff>();
            foreach(string mode in Modes){
                var fail=new List<GameAction>();var apply=Apply(confused,1,fromSpell:false);apply.IsNotDispelable=true;apply.AsChild=false;fail.Add(apply);
                if(mode!="I"){
                    var control=f.Buff(T(mode+"Control"),N(Key(mode)),D(Key(mode)),new MarauderConfusionControl01457{RerollNormally=mode=="II",ForcedState=mode=="IIIHarm"?ConfusionState.SelfHarm:mode=="IIIAttack"?ConfusionState.AttackNearest:ConfusionState.DoNothing});
                    control.Stacking=StackingType.Stack;Icon(control,confused);
                    var has=new ContextConditionHasBuffFromCaster();Set(has,"m_Buff",Ref<BlueprintBuffReference>(confused));var influence=Apply(control,1,fromSpell:false);influence.IsNotDispelable=true;influence.AsChild=false;
                    fail.Add(new Conditional{ConditionsChecker=new ConditionsChecker{Conditions=new Condition[]{has}},IfTrue=Actions(influence),IfFalse=Actions()});
                }
                var save=new ContextActionSavingThrow{Type=SavingThrowType.Will,Actions=Actions(Saved(Array.Empty<GameAction>(),fail.ToArray()))};
                var enemy=new Conditional{ConditionsChecker=new ConditionsChecker{Conditions=new Condition[]{new ContextConditionIsEnemy()}},IfTrue=Actions(save),IfFalse=Actions()};
                // Native Buff's per-owner round callback uses its area-derived
                // context. Entry, movement and unrelated turns do not reroll.
                var marker=f.Buff(T(mode+"Member"),"","",Tags(),new AddFactContextActions{Activated=Actions(),Deactivated=Actions(),NewRound=Actions(enemy),Dispose=Actions()});
                marker.Stacking=StackingType.Stack;SetEnum(marker,"m_Flags","HiddenInUi");members.Add(mode,marker);
            }
            for(int radiusIndex=0;radiusIndex<6;radiusIndex++){
                var granted=new Dictionary<string,BlueprintUnitFact>();
                foreach(string mode in Modes){
                    var membership=new AbilityAreaEffectBuff{Condition=new ConditionsChecker{Conditions=new Condition[]{new ContextConditionIsEnemy()}},CheckConditionEveryRound=true};Set(membership,"m_Buff",Ref<BlueprintBuffReference>(members[mode]));
                    var area=f.Clone(areaTemplate,T(mode+radiusIndex+"Area"),N(Key(mode)),D(Key(mode)));area.Size=(10+5*radiusIndex).Feet();area.SpellResistance=false;area.Fx=new PrefabLink{AssetId=AuraRadiusVisual01458.RingAsset};area.ComponentsArray=new BlueprintComponent[]{membership,Params(cls),Tags()};
                    var areaFact=new AddAreaEffect();Set(areaFact,"m_AreaEffect",Ref<BlueprintAbilityAreaEffectReference>(area));var aura=f.Buff(T(mode+radiusIndex+"Aura"),N(Key(mode)),D(Key(mode)),areaFact);Icon(aura,confused);
                    foreach(string pool in Pools){
                        var resource=f.Native<BlueprintAbilityResource>(pool=="Wisdom"?PsychicClass.WisdomPoolId:PsychicClass.CharismaPoolId);
                        var durationRank=new ContextRankConfig();SetEnum(durationRank,"m_BaseValueType","StatBonus");Set(durationRank,"m_Stat",StatType.Charisma);Set(durationRank,"m_UseMin",true);Set(durationRank,"m_Min",0);
                        var applied=Apply(aura,0,fromSpell:false);applied.DurationValue.BonusValue=new ContextValue{ValueType=ContextValueType.Rank};applied.IsNotDispelable=true;applied.AsChild=false;
                        string suffix=mode=="IIIStill"?": Do Nothing":mode=="IIIHarm"?": Self Harm":mode=="IIIAttack"?": Attack Nearest":"";
                        // Mind-affecting tags belong to the enemy area/member,
                        // not the personal activation: otherwise Unreal
                        // Understanding blocks the caster's own aura at20.
                        var ability=f.Ability(T(mode+radiusIndex+pool+"Ability"),N(Key(mode))+suffix,D(Key(mode)),AbilityType.Supernatural,UnitCommand.CommandType.Standard,AbilityRange.Personal,Run(applied),durationRank,Params(cls),Cost(resource,mode=="I"?1:mode=="II"?2:3),Radius(10+5*radiusIndex),new AbilitySpawnFx{PrefabLink=new PrefabLink{AssetId="c14a2f46018cb0e41bfeed61463510ff"},Time=AbilitySpawnFxTime.OnApplyEffect,Anchor=AbilitySpawnFxAnchor.Caster,PositionAnchor=AbilitySpawnFxAnchor.None,OrientationAnchor=AbilitySpawnFxAnchor.None});
                        Icon(ability,confused);ability.SpellResistance=false;ability.LocalizedDuration=Text(T(mode+radiusIndex+pool+"Ability"),"duration","Charisma modifier rounds");ability.LocalizedSavingThrow=Text(T(mode+radiusIndex+pool+"Ability"),"savingThrow","Will negates");granted.Add(mode+pool,ability);
                    }
                }
                foreach(string pool in Pools){var leaves=new[]{"IIIStill","IIIHarm","IIIAttack"}.Select(mode=>(BlueprintAbility)granted[mode+pool]).ToArray();var variants=new AbilityVariants();Set(variants,"m_Variants",leaves.Select(Ref<BlueprintAbilityReference>).ToArray());var wrapper=f.Ability(T("III"+radiusIndex+pool+"Wrapper"),N(Key("III")),D(Key("III")),AbilityType.Supernatural,UnitCommand.CommandType.Standard,AbilityRange.Personal,variants);Icon(wrapper,confused);foreach(var leaf in leaves)Set(leaf,"m_Parent",Ref<BlueprintAbilityReference>(wrapper));granted.Add("III"+pool,wrapper);}
                foreach(string tier in new[]{"I","II","III"}){
                    var conditional=new List<BlueprintComponent>();foreach(string pool in Pools){var part=new AddFeatureIfHasFact();Set(part,"m_CheckedFact",Ref<BlueprintUnitFactReference>(f.Native<BlueprintFeature>(pool=="Wisdom"?PsychicClass.WisdomFeatureId:PsychicClass.CharismaFeatureId)));Set(part,"m_Feature",Ref<BlueprintUnitFactReference>(granted[tier+pool]));conditional.Add(part);}
                    var poolFeature=Hidden(f,T(tier+radiusIndex+"PoolGrant"),conditional.ToArray());var gated=radiusIndex<5?Hidden(f,T(tier+radiusIndex+"LevelWindow"),At(cls,poolFeature,(radiusIndex+2)*3,true)):poolFeature;
                    tiers[tier].ComponentsArray=tiers[tier].ComponentsArray.Concat(new[]{At(cls,gated,(radiusIndex+1)*3)}).ToArray();
                }
            }
            // A fresh paid activation replaces this caster's previous aura.
            // Other casters' personal auras and their contexts are untouched.
            var ownAuras=f.Assets.Where(p=>p.Key.EndsWith("Aura01457",StringComparison.Ordinal)).Select(p=>(BlueprintBuff)p.Value).ToArray();
            foreach(var a in f.Assets.Values.OfType<BlueprintAbility>()){
                var effect=a.GetComponent<AbilityEffectRunAction>();if(effect==null)continue;
                var clear=ownAuras.Select(buff=>{var action=new ContextActionRemoveBuff();Set(action,"m_Buff",Ref<BlueprintBuffReference>(buff));return (GameAction)action;}).ToArray();
                effect.Actions=Actions(clear.Concat(effect.Actions.Actions).ToArray());
            }
            var root=f.Add(new BlueprintArchetype{LocalizedName=Text(RootToken,"name",N("PsychicMaraudeurArchetype")),LocalizedDescription=Text(RootToken,"description",D("PsychicMaraudeurArchetype")),AddFeatures=new[]{Entry(2,skewed),Entry(3,tiers["I"]),Entry(9,cracked),Entry(11,tiers["II"]),Entry(19,tiers["III"]),Entry(20,unreal)},RemoveFeatures=new[]{Entry(3,amp),Entry(11,amp),Entry(19,amp),Entry(20,mastery,amp,amp)},ComponentsArray=Array.Empty<BlueprintComponent>()},RootToken,N("PsychicMaraudeurArchetype"),D("PsychicMaraudeurArchetype"));Set(root,"m_ParentClass",cls);
            Publish(cache,f,OwnTokens,cls,root,new BlueprintFeatureBase[]{skewed,tiers["I"],cracked,tiers["II"],tiers["III"],unreal});
        }
    }
}

using System;
using System.IO;
using System.Reflection;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.ElementsSystem;
using Kingmaker.Enums.Damage;
using Kingmaker.ResourceLinks;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components;
using Kingmaker.UnitLogic.Abilities.Components.AreaEffects;
using Kingmaker.UnitLogic.Abilities.Components.Base;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Commands.Base;
using Kingmaker.UnitLogic.FactLogic;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Mechanics.Actions;
using Kingmaker.UnitLogic.Mechanics.Components;
using Kingmaker.Utility;
using UnityEngine;
using static CallOfTheWild.Wrath.Prototype.PsychicAssets;

namespace CallOfTheWild.Wrath.Prototype
{
    internal static class PsychicSilence
    {
        private static Sprite icon;
        internal static Sprite Icon()
        {
            if(!ReferenceEquals(icon,null))return icon;
            using(var stream=Assembly.GetExecutingAssembly().GetManifestResourceStream("CotW.Silence.png"))
            using(var bytes=new MemoryStream()) {
                if(stream==null)throw new InvalidOperationException("Missing Silence icon.");stream.CopyTo(bytes);
                var texture=new Texture2D(2,2);if(!ImageConversion.LoadImage(texture,bytes.ToArray()))throw new InvalidOperationException("Invalid Silence icon.");
                icon=Sprite.Create(texture,new Rect(0,0,texture.width,texture.height),new Vector2(.5f,.5f));
            }
            return icon;
        }
        internal static void Build(PsychicAssets f)
        {
            const string text="Create a stationary 20-foot-radius area of silence for 1 round per caster level. Creatures inside cannot cast spells requiring speech; psychic spellcasting is unaffected. The area grants immunity to sonic damage and sonic spell effects and suppresses sonic buffs and Echolocation. Leaving the area immediately ends its effects. Because the spell is centered on a point in space, there is no saving throw or spell resistance. The area remains at its chosen point and does not move with a creature.";
            var suppress=new SuppressBuffs {name="SilenceSuppressSonicAndEcholocation",Descriptor=SpellDescriptor.Sonic,Schools=Array.Empty<SpellSchool>()};
            Set(suppress,"m_Buffs",new[]{Ref<BlueprintBuffReference>(f.Native<BlueprintBuff>("cbfd2f5279f5946439fe82570fd61df2"))});Set(suppress,"m_ExcludedBuffs",Array.Empty<BlueprintBuffReference>());
            var buff=f.Buff("SilenceBuff","Silence",text,new ForbidSpellCasting {name="SilenceVerbalSpellcasting",ForbidMagicItems=false},
                new AddEnergyImmunity {name="SilenceSonicDamageImmunity",Type=DamageEnergyType.Sonic},
                new SpellImmunityToSpellDescriptor {name="SilenceSonicSpellImmunity",Descriptor=SpellDescriptor.Sonic},
                new BuffDescriptorImmunity {name="SilenceSonicBuffImmunity",Descriptor=SpellDescriptor.Sonic},suppress);
            Set(buff.GetComponent<ForbidSpellCasting>(),"m_IgnoreFeature",new BlueprintFeatureReference());
            Set(buff.GetComponent<SpellImmunityToSpellDescriptor>(),"m_CasterIgnoreImmunityFact",new BlueprintUnitFactReference());
            Set(buff.GetComponent<BuffDescriptorImmunity>(),"m_IgnoreFeature",new BlueprintUnitFactReference());
            Set(buff.GetComponent<BuffDescriptorImmunity>(),"m_FactToCheck",new BlueprintUnitFactReference());
            // Each area owns its own fact. Exiting one overlapping area must not
            // erase the silence supplied by another caster or another cast.
            buff.Stacking=StackingType.Stack;Set(buff,"m_Icon",Icon());
            var apply=new AbilityAreaEffectBuff {name="SilenceAreaOwnedBuff",Condition=new ConditionsChecker {Conditions=Array.Empty<Condition>()}};Set(apply,"m_Buff",Ref<BlueprintBuffReference>(buff));
            var area=f.Add(new BlueprintAbilityAreaEffect {Size=new Feet(20),Shape=AreaEffectShape.Cylinder,AffectEnemies=true,AggroEnemies=false,SpellResistance=false,Fx=new PrefabLink {AssetId="63f322580ec0e7c4c96fc62ecabad40f"},ComponentsArray=new BlueprintComponent[]{apply}},"SilenceArea","Silence",text);SetEnum(area,"m_TargetType","Any");
            var spawn=new ContextActionSpawnAreaEffect {name="SilenceStationaryArea",OnUnit=false,DurationValue=new ContextDurationValue {Rate=DurationRate.Rounds,DiceType=Kingmaker.RuleSystem.DiceType.Zero,DiceCountValue=0,BonusValue=new ContextValue {ValueType=ContextValueType.Rank}}};Set(spawn,"m_AreaEffect",Ref<BlueprintAbilityAreaEffectReference>(area));
            var rank=new ContextRankConfig {name="SilenceCasterLevelDuration"};SetEnum(rank,"m_BaseValueType","CasterLevel");
            var spell=f.Ability("Silence","Silence",text,AbilityType.Spell,UnitCommand.CommandType.Standard,AbilityRange.Long,new SpellComponent {name="SilenceSchool",School=SpellSchool.Illusion},rank,Run(spawn),new AbilitySpawnFx {name="SilenceCastVisual",PrefabLink=new PrefabLink {AssetId="c4e5e6e8407f1774b97af4957364852c"},Time=AbilitySpawnFxTime.OnApplyEffect,Anchor=AbilitySpawnFxAnchor.SelectedTarget,PositionAnchor=AbilitySpawnFxAnchor.None,OrientationAnchor=AbilitySpawnFxAnchor.None});
            Set(spell,"m_IsFullRoundAction",true);spell.CanTargetPoint=true;spell.CanTargetSelf=false;spell.CanTargetFriends=false;spell.CanTargetEnemies=false;spell.SpellResistance=false;
            spell.AvailableMetamagic=Metamagic.Extend|Metamagic.Heighten|Metamagic.Quicken|Metamagic.Reach;Set(spell,"m_Icon",Icon());
            spell.LocalizedDuration=Text("Silence","duration","1 round/level");spell.LocalizedSavingThrow=Text("Silence","savingThrow","");
        }
    }
}

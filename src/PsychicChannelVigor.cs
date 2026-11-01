using System;
using System.Linq;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.Designers.Mechanics.Buffs;
using Kingmaker.Designers.Mechanics.Facts;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Enums;
using Kingmaker.ResourceLinks;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components;
using Kingmaker.UnitLogic.Abilities.Components.Base;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Commands.Base;
using Kingmaker.UnitLogic.FactLogic;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Mechanics.Actions;
using Kingmaker.UnitLogic.Mechanics.Components;
using static CallOfTheWild.Wrath.Prototype.PsychicAssets;
namespace CallOfTheWild.Wrath.Prototype
{
    internal static class PsychicChannelVigor
    {
        internal static void Build(PsychicAssets f)
        {
            const string text="Focus your vigor for 1 round per caster level. Choose one: Limbs grants the benefits of haste; Mind grants a +4 competence bonus on Knowledge, Lore and Perception checks and ranged attack rolls; Spirit grants a +6 competence bonus on Will saves and Bluff and Intimidate checks; Torso grants a +6 competence bonus on Fortitude saves and a +6 bonus on concentration checks. Only one Channel Vigor effect can benefit you at a time. Casting another option replaces the previous one. Limbs does not stack with haste.";
            var icon=f.Native<BlueprintAbility>("c3a8f31778c3980498d8f00c980be5f5").Icon;
            var haste=f.Native<BlueprintBuff>("17113651fa8b40c2a7bbf9721462e646");
            var limbs=f.Clone(haste,"ChannelVigorLimbsBuff","Channel Vigor: Limbs",text);
            // Keep a separate, owned duration. Native haste-style extra attacks and
            // enhancement movement already take the highest value. Its three stat
            // bonuses yield to native HasteBuffEffect, and resume when that fact leaves.
            // Never delete another caster's Haste parent or shared child.
            limbs.ComponentsArray=limbs.ComponentsArray.Where(c=>!(c is AddStatBonus)).Concat(new BlueprintComponent[]{
                new PsychicVigorHasteStats {name="VigorHasteStats",NativeEffect=Ref<BlueprintBuffReference>(haste)},new ChangeImpatience {name="VigorHasteTempo",Delta=1}}).ToArray();
            SetEnum(limbs,"m_Flags","IsFromSpell");limbs.Stacking=StackingType.Replace;
            AddStatBonus Bonus(StatType stat,int value)=>new AddStatBonus {name="Vigor"+stat,Stat=stat,Value=value,Descriptor=ModifierDescriptor.Competence};
            var mind=f.Buff("ChannelVigorMindBuff","Channel Vigor: Mind",text,
                Bonus(StatType.SkillKnowledgeArcana,4),Bonus(StatType.SkillKnowledgeWorld,4),Bonus(StatType.SkillLoreNature,4),Bonus(StatType.SkillLoreReligion,4),Bonus(StatType.SkillPerception,4),
                new AttackTypeAttackBonus {name="VigorRangedAccuracy",Type=WeaponRangeType.Ranged,AttackBonus=4,Value=1,Descriptor=ModifierDescriptor.Competence});
            var spirit=f.Buff("ChannelVigorSpiritBuff","Channel Vigor: Spirit",text,Bonus(StatType.CheckBluff,6),Bonus(StatType.CheckIntimidate,6),Bonus(StatType.SaveWill,6));
            var torso=f.Buff("ChannelVigorTorsoBuff","Channel Vigor: Torso",text,Bonus(StatType.SaveFortitude,6),new ConcentrationBonus {name="VigorConcentration",Value=6});
            var buffs=new[]{limbs,mind,spirit,torso};var names=new[]{"Limbs","Mind","Spirit","Torso"};var variants=new BlueprintAbility[4];
            for(int i=0;i<4;i++){
                Set(buffs[i],"m_Icon",icon);
                buffs[i].FxOnStart=new PrefabLink {AssetId="91ef30ab58fa0d3449d4d2ccc20cb0f8"};
                var rank=new ContextRankConfig {name="VigorDuration"};SetEnum(rank,"m_BaseValueType","CasterLevel");
                var apply=Apply(buffs[i],0);apply.AsChild=false;apply.DurationValue.BonusValue=new ContextValue {ValueType=ContextValueType.Rank};
                var removes=buffs.Select(b=>{var a=new ContextActionRemoveBuff();Set(a,"m_Buff",Ref<BlueprintBuffReference>(b));return a;}).Cast<Kingmaker.ElementsSystem.GameAction>().ToArray();
                var spell=f.Ability("ChannelVigor"+names[i],"Channel Vigor: "+names[i],text,AbilityType.Spell,UnitCommand.CommandType.Standard,AbilityRange.Personal,
                    new SpellComponent {name="VigorSchool",School=SpellSchool.Transmutation},rank,Run(removes.Concat(new[]{apply}).ToArray()),
                    new AbilitySpawnFx {name="VigorCast",PrefabLink=new PrefabLink {AssetId="c14a2f46018cb0e41bfeed61463510ff"},Time=AbilitySpawnFxTime.OnApplyEffect,Anchor=AbilitySpawnFxAnchor.SelectedTarget,PositionAnchor=AbilitySpawnFxAnchor.None,OrientationAnchor=AbilitySpawnFxAnchor.None});
                Set(spell,"m_Icon",icon);spell.SpellResistance=false;spell.AvailableMetamagic=Metamagic.Extend|Metamagic.Quicken|Metamagic.Heighten;
                spell.LocalizedDuration=Text("ChannelVigor"+names[i],"duration","1 round per caster level");spell.LocalizedSavingThrow=Text("ChannelVigor"+names[i],"savingThrow","");variants[i]=spell;
            }
            var component=new AbilityVariants {name="VigorChoices"};Set(component,"m_Variants",variants.Select(Ref<BlueprintAbilityReference>).ToArray());
            var root=f.Ability("ChannelVigor","Channel Vigor",text,AbilityType.Spell,UnitCommand.CommandType.Standard,AbilityRange.Personal,new SpellComponent {name="VigorSchool",School=SpellSchool.Transmutation},component);
            Set(root,"m_Icon",icon);root.SpellResistance=false;root.AvailableMetamagic=Metamagic.Extend|Metamagic.Quicken|Metamagic.Heighten;
            root.LocalizedDuration=Text("ChannelVigor","duration","1 round per caster level");root.LocalizedSavingThrow=Text("ChannelVigor","savingThrow","");
            foreach(var v in variants)Set(v,"m_Parent",Ref<BlueprintAbilityReference>(root));
        }
    }
}

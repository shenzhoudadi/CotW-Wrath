using System.Collections.Generic;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.Designers.Mechanics.Facts;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.ResourceLinks;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components;
using Kingmaker.UnitLogic.Abilities.Components.Base;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Commands.Base;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Mechanics.Components;
using static CallOfTheWild.Wrath.Prototype.PsychicAssets;

namespace CallOfTheWild.Wrath.Prototype
{
    internal static class PsychicInsightSpells
    {
        internal static IEnumerable<PsychicSpells.Entry> Build(PsychicAssets f)
        {
            const string mindText="For 1 minute, roll twice and take the higher result on Will saves, Intelligence checks and Intelligence-based skill checks. Your secondary mind can cast Psychic spells and use spell-like abilities of 5th level or lower as swift actions, without increasing their spell slots or costs. Only abilities with a standard or move casting action qualify; full-round casting and item abilities do not. The normal swift-action limit applies. Other kinds of purely mental actions are not automated.";
            var will=new ModifyD20 {name="DivideMindWill",Rule=RuleType.SavingThrow,RollsAmount=1,TakeBest=true,ActionsToTrigger=Actions(),ActionsToTriggerBeforeRoll=Actions()};
            SetEnum(will,"m_SavingThrowType","Will");
            var skills=new ModifyD20 {name="DivideMindIntelligence",Rule=RuleType.SkillCheck,RollsAmount=1,TakeBest=true,SpecificSkill=true,
                Skill=new[]{StatType.Intelligence,StatType.SkillKnowledgeArcana,StatType.SkillKnowledgeWorld},ActionsToTrigger=Actions(),ActionsToTriggerBeforeRoll=Actions()};
            var mind=f.Buff("DivideMindBuff","Divide Mind",mindText,will,skills,
                new PsychicDivideMindMetamagic {name="DivideMindSecondaryCasting",Metamagic=Metamagic.Quicken,MaxSpellLevel=5,Abilities=new List<BlueprintAbilityReference>()});
            Set(mind,"m_Icon",PsychicOriginalIcons.Load("DivideMind") /* previous Wrath donor: f.Native<BlueprintAbility>("ae4d3ad6a8fda1542acf2e9bbc13d113").Icon */);
            var divide=f.Ability("DivideMind","Divide Mind",mindText,AbilityType.Spell,UnitCommand.CommandType.Standard,AbilityRange.Personal,
                new SpellComponent {name="DivideMindSchool",School=SpellSchool.Enchantment},
                new SpellDescriptorComponent {name="DivideMindDescriptor",Descriptor=SpellDescriptor.MindAffecting},Run(Apply(mind,1,DurationRate.Minutes)),
                new AbilitySpawnFx {name="DivideMindVisual",PrefabLink=new PrefabLink {AssetId="c388856d0e8855f429a83ccba67944ba"},Time=AbilitySpawnFxTime.OnApplyEffect,Anchor=AbilitySpawnFxAnchor.Caster,PositionAnchor=AbilitySpawnFxAnchor.None,OrientationAnchor=AbilitySpawnFxAnchor.None});
            Set(divide,"m_Icon",mind.Icon);divide.AvailableMetamagic=Metamagic.Quicken|Metamagic.Extend|Metamagic.Heighten;
            divide.LocalizedDuration=Text("DivideMind","duration","1 minute");
            yield return new PsychicSpells.Entry {Token="DivideMind",Level=9,Ability=divide};

            // CotW intentionally uses maximum weapon dice, not the tabletop
            // double-roll formula. Extra elemental/precision packets stay ordinary.
            const string weakText="For 1 minute per caster level, your confirmed weapon critical hits deal maximum weapon dice damage. Extra damage dice, such as sneak attack and flaming weapon damage, are not maximized. This follows the Call of the Wild adaptation of Locate Weakness.";
            var weakness=f.Buff("LocateWeaknessBuff","Locate Weakness",weakText,new PsychicLocateWeakness {name="LocateWeaknessCriticalDamage"});
            Set(weakness,"m_Icon",PsychicOriginalIcons.Load("LocateWeakness") /* previous Wrath donor: f.Native<BlueprintAbility>("2c38da66e5a599347ac95b3294acbe00").Icon */);
            var rank=new ContextRankConfig {name="LocateWeaknessDuration"};SetEnum(rank,"m_BaseValueType","CasterLevel");
            var apply=Apply(weakness,0,DurationRate.Minutes);apply.DurationValue.BonusValue=new ContextValue {ValueType=ContextValueType.Rank};
            var locate=f.Ability("LocateWeakness","Locate Weakness",weakText,AbilityType.Spell,UnitCommand.CommandType.Standard,AbilityRange.Personal,
                new SpellComponent {name="LocateWeaknessSchool",School=SpellSchool.Divination},rank,Run(apply),
                new AbilitySpawnFx {name="LocateWeaknessVisual",PrefabLink=new PrefabLink {AssetId="8de64fbe047abc243a9b4715f643739f"},
                    Time=AbilitySpawnFxTime.OnApplyEffect,Anchor=AbilitySpawnFxAnchor.Caster,PositionAnchor=AbilitySpawnFxAnchor.None,OrientationAnchor=AbilitySpawnFxAnchor.None});
            Set(locate,"m_Icon",weakness.Icon);locate.AvailableMetamagic=Metamagic.Extend|Metamagic.Quicken|Metamagic.Heighten;
            locate.LocalizedDuration=Text("LocateWeakness","duration","1 minute per caster level");
            yield return new PsychicSpells.Entry {Token="LocateWeakness",Level=3,Ability=locate};
        }
    }
}

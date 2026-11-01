using System;
using System.Collections.Generic;
using System.Linq;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.ElementsSystem;
using Kingmaker.Designers.EventConditionActionSystem.Actions;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Enums;
using Kingmaker.Enums.Damage;
using Kingmaker.ResourceLinks;
using Kingmaker.RuleSystem;
using Kingmaker.RuleSystem.Rules.Damage;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components;
using Kingmaker.UnitLogic.Abilities.Components.Base;
using Kingmaker.UnitLogic.Abilities.Components.TargetCheckers;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Commands.Base;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Mechanics.Actions;
using Kingmaker.UnitLogic.Mechanics.Conditions;
using static CallOfTheWild.Wrath.Prototype.PsychicAssets;

namespace CallOfTheWild.Wrath.Prototype
{
    // Temporary HP is not part of the health fraction. Integer comparison also
    // keeps odd maximum HP and exactly half HP on the correct side of the rule.
    [TypeId("c90dbd4a7965413a961483764bf420aa")]
    public sealed class PsychicCrushHealth : ContextCondition
    {
        public enum Threshold { AboveHalf, Full, BelowHalf }
        public Threshold Comparison;
        protected override string GetConditionCaption()=>"Psychic Crush: target health threshold";
        protected override bool CheckCondition()
        {
            var unit=Target?.Unit;
            if(unit==null) return false;
            long hp=unit.Descriptor.HPLeft,max=unit.Descriptor.MaxHP;
            return Comparison==Threshold.Full?hp>=max:Comparison==Threshold.AboveHalf?hp*2>max:hp*2<max;
        }
    }

    [TypeId("9735d95fc131467ba7b579fe013c319b")]
    public sealed class PsychicCrushDamage : ContextAction
    {
        public int DiceCount;
        public bool Collapse;
        public override string GetCaption()=>Collapse?"Psychic Crush: reduce to -1 HP":"Psychic Crush: damage, minimum -1 HP";
        public override void RunAction()
        {
            var target=Target?.Unit;
            if(target==null || Context?.MaybeCaster==null || target.Descriptor.HPLeft<=-1) return;
            // Wrath 2.7's native cap subtracts MinHPAfterDamage from MAXIMUM
            // HP + temporary HP, without subtracting existing damage. Offset
            // that parameter by current damage so an injured target stops at
            // -1 too. Do not mark a creature for death or write its HP directly.
            var damage=new ContextActionDealDamage {
                name="PsychicCrushNativeDamage", DamageType=new DamageTypeDescription {Type=DamageType.Direct},
                Value=new ContextDiceValue {DiceType=Collapse||DiceCount==0?DiceType.Zero:DiceType.D6,
                    DiceCountValue=Collapse?0:DiceCount,
                    BonusValue=Collapse?target.Descriptor.HPLeft+target.Descriptor.TemporaryHP+1:Context.Params.CasterLevel},
                UseMinHPAfterDamage=true,MinHPAfterDamage=target.Descriptor.Damage-1,
                IgnoreCritical=true,DisableSneakDamage=true,IgnoreUnitModifiers=Collapse
            };
            damage.RunAction();
        }
    }

    internal static class PsychicCrushSpells
    {
        internal static IEnumerable<PsychicSpells.Entry> Build(PsychicAssets f)
        {
            var icon=f.Native<BlueprintAbility>("137af566f68fd9b428e2e12da43c1482").Icon;
            var sickened=f.Native<BlueprintBuff>("4e42460798665fd4cb9173ffa7ada323");
            var lower=new List<BlueprintAbility>();
            for(int tier=1;tier<=5;tier++) {
                string token="PsychicCrush"+tier,title="Psychic Crush "+PsychicSpells.Roman[tier];
                int dice=2*tier+1;
                var damage=new PsychicCrushDamage {DiceCount=dice};
                var collapse=new PsychicCrushDamage {Collapse=true};
                var fort=new ContextActionSavingThrow {name=token+"Fortitude",Type=SavingThrowType.Fortitude,
                    Actions=Actions(Saved(new GameAction[]{damage,Apply(sickened,1)},new GameAction[]{collapse}))};
                var adjustments=new List<ContextActionSavingThrow.ConditionalDCIncrease>();
                if(tier<=2)adjustments.Add(Adjustment(PsychicCrushHealth.Threshold.AboveHalf,tier==1?-4:-2));
                if(tier==2 || tier==3)adjustments.Add(Adjustment(PsychicCrushHealth.Threshold.Full,-2));
                Set(fort,"m_ConditionalDCIncrease",adjustments.ToArray());
                GameAction fail=tier<4?(GameAction)fort:new Conditional {name=token+"BelowHalfSkipsFortitude",
                    ConditionsChecker=Conditions(PsychicCrushHealth.Threshold.BelowHalf),IfTrue=Actions(collapse),IfFalse=Actions(fort)};
                var onWill=new List<GameAction>();
                if(tier>=3)onWill.Add(new PsychicCrushDamage {DiceCount=tier==3?0:dice});
                onWill.Add(Apply(sickened,1));
                var will=new ContextActionSavingThrow {name=token+"Will",Type=SavingThrowType.Will,
                    Actions=Actions(Saved(onWill.ToArray(),new[]{fail}))};
                if(tier==5)Set(will,"m_ConditionalDCIncrease",new[]{Adjustment(PsychicCrushHealth.Threshold.BelowHalf,2)});
                var intelligent=new AbilityTargetHasFact {name=token+"IntelligentTarget",Inverted=true};
                Set(intelligent,"m_CheckedFacts",new[]{Ref<BlueprintUnitFactReference>(f.Native<Kingmaker.Blueprints.Classes.BlueprintFeature>("fd389783027d63343b4a5634bd81645f")),Ref<BlueprintUnitFactReference>(f.Native<Kingmaker.Blueprints.Classes.BlueprintFeature>("09478937695300944a179530664e42ec"))});
                string willText=tier<3?"A successful Will save sickens the target for 1 round.":tier==3?"A successful Will save deals 1 direct damage per caster level and sickens the target for 1 round.":"A successful Will save deals "+dice+"d6 direct damage plus your caster level and sickens the target for 1 round.";
                string healthText=tier==1?"The Fortitude save gains a +4 circumstance bonus if the target has more than half its maximum HP.":tier==2?"The Fortitude save gains a +4 circumstance bonus at full HP, or +2 above half HP.":tier==3?"The Fortitude save gains a +2 circumstance bonus at full HP.":"If the target has less than half its maximum HP, a failed Will save skips the Fortitude save and reduces it to -1 HP. Exactly half HP still allows Fortitude.";
                string text="Crush a creature's mind. "+willText+" A failed Will save requires a Fortitude save: success deals "+dice+"d6 direct damage plus your caster level and sickens the target for 1 round; failure reduces it to -1 HP. Damage from this spell cannot reduce HP below -1. "+healthText+(tier==5?" Below half HP, the Will save also takes a -2 penalty.":"")+" Temporary HP does not affect these thresholds. Mind-affecting immunity and spell resistance apply. As in CotW, constructs and vermin cannot be targeted."+(tier>1?" This spell can be undercast.":"");
                var spell=f.Ability(token,title,text,AbilityType.Spell,UnitCommand.CommandType.Standard,AbilityRange.Close,
                    new SpellComponent {name=token+"School",School=SpellSchool.Necromancy},
                    new SpellDescriptorComponent {name=token+"MindAffecting",Descriptor=SpellDescriptor.MindAffecting},
                    intelligent,Run(will),
                    new PsychicUndercastFamily {name=token+"Undercast",Family="PsychicCrush",Circle=tier+4,LowerSpells=lower.Select(Ref<BlueprintAbilityReference>).ToArray()},
                    new AbilitySpawnFx {name=token+"CastVisual",PrefabLink=new PrefabLink {AssetId="cbfe312cb8e63e240a859efaad8e467c"},
                        Time=AbilitySpawnFxTime.OnApplyEffect,Anchor=AbilitySpawnFxAnchor.SelectedTarget,PositionAnchor=AbilitySpawnFxAnchor.None,OrientationAnchor=AbilitySpawnFxAnchor.None});
                spell.CanTargetEnemies=true;spell.CanTargetFriends=true;spell.CanTargetSelf=false;
                spell.EffectOnEnemy=AbilityEffectOnUnit.Harmful;spell.EffectOnAlly=AbilityEffectOnUnit.Harmful;spell.SpellResistance=true;
                spell.AvailableMetamagic=Metamagic.Empower|Metamagic.Maximize|Metamagic.Quicken|Metamagic.Reach|Metamagic.Heighten|Metamagic.Persistent|Metamagic.Piercing;
                Set(spell,"m_Icon",icon);spell.LocalizedDuration=Text(token,"duration","Instantaneous; sickened for 1 round");
                spell.LocalizedSavingThrow=Text(token,"savingThrow","Will partial; Fortitude partial (see text)");
                lower.Add(spell);yield return new PsychicSpells.Entry {Token=token,Level=tier+4,Ability=spell};
            }
        }
        private static ConditionsChecker Conditions(PsychicCrushHealth.Threshold threshold)=>new ConditionsChecker {Conditions=new Condition[]{new PsychicCrushHealth {Comparison=threshold}}};
        private static ContextActionSavingThrow.ConditionalDCIncrease Adjustment(PsychicCrushHealth.Threshold threshold,int delta)=>new ContextActionSavingThrow.ConditionalDCIncrease {Condition=Conditions(threshold),Value=delta};
    }
}

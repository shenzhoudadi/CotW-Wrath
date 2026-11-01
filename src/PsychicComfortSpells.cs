using System;
using System.Collections.Generic;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.ElementsSystem;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Enums;
using Kingmaker.ResourceLinks;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components;
using Kingmaker.UnitLogic.Abilities.Components.Base;
using Kingmaker.UnitLogic.ActivatableAbilities;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Buffs.Components;
using Kingmaker.UnitLogic.Commands.Base;
using Kingmaker.UnitLogic.FactLogic;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Mechanics.Components;
using Kingmaker.Utility;
using static CallOfTheWild.Wrath.Prototype.PsychicAssets;

namespace CallOfTheWild.Wrath.Prototype
{
    internal static class PsychicComfortSpells
    {
        internal static IEnumerable<PsychicSpells.Entry> Build(PsychicAssets f)
        {
            const string vigorText="Temporarily suppress fatigue and exhaustion for 10 minutes per caster level. This does not provide rest or restore any resources. When the spell ends, any remaining fatigue and exhaustion resume. Multiple castings replace the previous effect. The spell causes no damage when it ends.";
            // Suppress the two condition buffs, not every effect with a fatigue
            // descriptor: a composite spell must keep its unrelated penalties.
            // The native reference-counted part also covers newly added buffs.
            var suppress=new SuppressBuffs {name="InvigorateSuppression",Schools=Array.Empty<SpellSchool>(),Descriptor=SpellDescriptor.None};
            Set(suppress,"m_Buffs",new[]{Ref<BlueprintBuffReference>(f.Native<BlueprintBuff>("e6f2fc5d73d88064583cb828801212f4")),Ref<BlueprintBuffReference>(f.Native<BlueprintBuff>("46d1b9cc3d0fd36469a471b047d773a2"))});
            var vigor=f.Buff("InvigorateBuff","Invigorate",vigorText,suppress);
            Set(vigor,"m_Icon",PsychicOriginalIcons.Load("Invigorate") /* previous Wrath donor: f.Native<BlueprintActivatableAbility>("4ebaf39efb8ffb64baf92784808dc49c").Icon */);
            foreach(bool mass in new[]{false,true}) {
                string token=mass?"InvigorateMass":"Invigorate";
                var apply=Apply(vigor,0,DurationRate.TenMinutes);apply.AsChild=false;apply.DurationValue.BonusValue=new ContextValue {ValueType=ContextValueType.Rank};
                var parts=new List<BlueprintComponent>{new SpellComponent {name=token+"School",School=SpellSchool.Illusion},Rank(token),Run(apply),Fx(token,"790eb82d267bf0749943fba92b7953c2")};
                if(mass)parts.Add(Targets(token,15,TargetType.Ally));
                var spell=f.Ability(token,mass?"Invigorate, Mass":"Invigorate",(mass?"Affects you and allies within 15 feet. ":"")+vigorText,
                    AbilityType.Spell,UnitCommand.CommandType.Standard,mass?AbilityRange.Personal:AbilityRange.Touch,parts.ToArray());
                spell.CanTargetSelf=true;spell.CanTargetFriends=true;spell.CanTargetEnemies=false;
                spell.EffectOnEnemy=AbilityEffectOnUnit.None;spell.SpellResistance=false;
                spell.AvailableMetamagic=Metamagic.Extend|Metamagic.Quicken|Metamagic.Heighten|(mass?0:Metamagic.Reach);
                Set(spell,"m_Icon",vigor.Icon);spell.LocalizedDuration=Text(token,"duration","10 minutes per caster level");
                // Friendly-only spells use the normal Wrath no-save convention.
                yield return new PsychicSpells.Entry {Token=token,Level=mass?4:1,Ability=spell};
            }

            const string agonyText="Enemies in a 30-foot burst within close range suffer wracking pain for 1 round per caster level. They take a -2 penalty to AC, attack rolls, melee weapon damage and Reflex saves. Casting a spell requires a concentration check (DC 15 + spell level). Fortitude negates; spell resistance applies. The pain cannot be relieved by spending an action to scream.";
            var agony=f.Buff("HowlingAgonyBuff","Howling Agony",agonyText,
                new AddStatBonus {name="HowlingAgonyAC",Stat=StatType.AC,Value=-2,Descriptor=ModifierDescriptor.UntypedStackable},
                new AddStatBonus {name="HowlingAgonyAttack",Stat=StatType.AdditionalAttackBonus,Value=-2,Descriptor=ModifierDescriptor.UntypedStackable},
                new AddStatBonus {name="HowlingAgonyReflex",Stat=StatType.SaveReflex,Value=-2,Descriptor=ModifierDescriptor.UntypedStackable},
                new AddCondition {name="HowlingAgonyConcentration",Condition=UnitCondition.SpellCastingIsDifficult},
                new WeaponAttackTypeDamageBonus {name="HowlingAgonyMeleeDamage",Type=WeaponRangeType.Melee,AttackBonus=1,Value=-2,Descriptor=ModifierDescriptor.UntypedStackable});
            Set(agony,"m_Icon",PsychicOriginalIcons.Load("HowlingAgony") /* previous Wrath donor: f.Native<BlueprintAbility>("d42c6d3f29e07b6409d670792d72bc82").Icon */);SetEnum(agony,"m_Flags","Harmful");
            var pain=Apply(agony,0);pain.AsChild=false;pain.DurationValue.BonusValue=new ContextValue {ValueType=ContextValueType.Rank};
            var howling=f.Ability("HowlingAgony","Howling Agony",agonyText,AbilityType.Spell,UnitCommand.CommandType.Standard,AbilityRange.Close,
                new SpellComponent {name="HowlingAgonySchool",School=SpellSchool.Necromancy},Rank("HowlingAgony"),
                Save(SavingThrowType.Fortitude,Saved(Array.Empty<GameAction>(),new GameAction[]{pain})),Targets("HowlingAgony",30,TargetType.Enemy),Fx("HowlingAgony","cbfe312cb8e63e240a859efaad8e467c"));
            Set(howling,"m_Icon",agony.Icon);howling.CanTargetSelf=false;howling.CanTargetFriends=false;howling.CanTargetEnemies=true;howling.CanTargetPoint=true;
            howling.EffectOnAlly=AbilityEffectOnUnit.Harmful;howling.SpellResistance=true;
            howling.AvailableMetamagic=Metamagic.Extend|Metamagic.Quicken|Metamagic.Heighten|Metamagic.Reach|Metamagic.Persistent|Metamagic.Piercing;
            howling.LocalizedDuration=Text("HowlingAgony","duration","1 round per caster level");howling.LocalizedSavingThrow=Text("HowlingAgony","savingThrow","Fortitude negates");
            // Wrath has no Pain descriptor. Never import CotW's numeric bit;
            // it collides with unrelated native Wrath descriptors.
            yield return new PsychicSpells.Entry {Token="HowlingAgony",Level=2,Ability=howling};
        }
        private static ContextRankConfig Rank(string token) {var c=new ContextRankConfig {name=token+"Duration"};SetEnum(c,"m_BaseValueType","CasterLevel");return c;}
        private static AbilityTargetsAround Targets(string token,int feet,TargetType type) {
            var c=new AbilityTargetsAround {name=token+"Targets"};Set(c,"m_Radius",new Feet(feet));Set(c,"m_TargetType",type);
            Set(c,"m_Condition",new ConditionsChecker {Conditions=Array.Empty<Condition>()});return c;
        }
        private static AbilitySpawnFx Fx(string token,string id)=>new AbilitySpawnFx {name=token+"Visual",PrefabLink=new PrefabLink {AssetId=id},
            Time=AbilitySpawnFxTime.OnApplyEffect,Anchor=AbilitySpawnFxAnchor.SelectedTarget,PositionAnchor=AbilitySpawnFxAnchor.None,OrientationAnchor=AbilitySpawnFxAnchor.None};
    }
}

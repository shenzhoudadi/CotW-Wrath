using System;
using System.Collections.Generic;
using System.Linq;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.Designers.EventConditionActionSystem.Actions;
using Kingmaker.ElementsSystem;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.PubSubSystem;
using Kingmaker.ResourceLinks;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components;
using Kingmaker.UnitLogic.Abilities.Components.Base;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Commands.Base;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Mechanics.Actions;
using Kingmaker.UnitLogic.Mechanics.Components;
using Kingmaker.UnitLogic.Mechanics.Conditions;
using Kingmaker.Utility;
using static CallOfTheWild.Wrath.Prototype.PsychicAssets;

namespace CallOfTheWild.Wrath.Prototype
{
    internal static class PsychicWardingSpells
    {
        internal static IEnumerable<PsychicSpells.Entry> Build(PsychicAssets f)
        {
            const string wardText="A spectral weapon guards your spellcasting for 1 round per caster level. You can cast spells and spell-like abilities without provoking attacks of opportunity for casting and without a defensive casting check. This does not protect movement, ranged attacks, or other provoking actions, and does not waive concentration checks caused by damage or other interference. The weapon does not attack or deal damage.";
            var ward=f.Buff("WardingWeaponBuff","Warding Weapon",wardText,new PsychicWardingWeapon {name="WardingWeaponCastingProtection"});
            Set(ward,"m_Icon",PsychicOriginalIcons.Load("WardingWeapon"));
            var apply=Apply(ward,0);apply.AsChild=false;apply.DurationValue.BonusValue=new ContextValue {ValueType=ContextValueType.Rank};
            var rank=new ContextRankConfig {name="WardingWeaponDuration"};SetEnum(rank,"m_BaseValueType","CasterLevel");
            var weapon=f.Ability("WardingWeapon","Warding Weapon",wardText,AbilityType.Spell,UnitCommand.CommandType.Standard,AbilityRange.Personal,
                new SpellComponent {name="WardingWeaponSchool",School=SpellSchool.Abjuration},rank,Run(apply),new AbilitySpawnFx {name="WardingWeaponVisual",PrefabLink=new PrefabLink {AssetId="c4d861e816edd6f4eab73c55a18fdadd"},Time=AbilitySpawnFxTime.OnApplyEffect,Anchor=AbilitySpawnFxAnchor.SelectedTarget,PositionAnchor=AbilitySpawnFxAnchor.None,OrientationAnchor=AbilitySpawnFxAnchor.None});
            Set(weapon,"m_Icon",ward.Icon);weapon.SpellResistance=false;
            weapon.LocalizedDuration=Text("WardingWeapon","duration","1 round per caster level");
            weapon.AvailableMetamagic=Metamagic.Extend|Metamagic.Heighten|Metamagic.Quicken;
            yield return new PsychicSpells.Entry {Ability=weapon,Level=2};

            const string dazeText="Daze humanoid enemies in a 15-foot burst within close range for 1 round, with no Hit Dice limit. Will negates; spell resistance applies. Dazed creatures cannot act. This follows Call of the Wild's creature-type filtering: aberrations, constructs, dragons, fey, outsiders, and plants are excluded. Animals, magical beasts, monstrous humanoids, and vermin require the caster's Serpentine bloodline arcana. Normal mind-affecting and daze immunities still apply.";
            var donor=f.Native<BlueprintAbility>("55f14bc84d7c85446b07a1b5dd6b2b4c");
            var dazed=f.Clone(f.Native<BlueprintBuff>("9934fedff1b14994ea90205d189c8759"),"DazeMassBuff","Daze, Mass",dazeText);
            Set(dazed,"m_Icon",donor.Icon);SetEnum(dazed,"m_Flags","Harmful");
            // Retain DazeMassBuff solely for old save identity; new casts use native DazeBuff.
            var dazeApply=Apply(f.Native<BlueprintBuff>("9934fedff1b14994ea90205d189c8759"),1);dazeApply.AsChild=false;
            var save=new ContextActionSavingThrow {Type=SavingThrowType.Will,Actions=Actions(Saved(Array.Empty<GameAction>(),new GameAction[]{dazeApply}))};
            var serpent=new ContextConditionCasterHasFact();Set(serpent,"m_Fact",Ref<BlueprintUnitFactReference>(f.Native<BlueprintFeature>("02707231be1d3a74ba7e38a426c8df37")));
            var special=CheckAny(f.Native<BlueprintFeature>("a95311b3dc996964cbaa30ff9965aaf6"),f.Native<BlueprintFeature>("625827490ea69d84d8e599a33929fdc6"),f.Native<BlueprintFeature>("57614b50e8d86b24395931fffc5e409b"),f.Native<BlueprintFeature>("09478937695300944a179530664e42ec"));
            var gate=new Conditional {ConditionsChecker=special,IfTrue=Actions(new Conditional {ConditionsChecker=new ConditionsChecker {Conditions=new Condition[]{serpent}},IfTrue=Actions(save),IfFalse=Actions()}),IfFalse=Actions(save)};
            var forbidden=CheckAny(f.Native<BlueprintFeature>("3bec99efd9a363242a6c8d9957b75e91"),f.Native<BlueprintFeature>("fd389783027d63343b4a5634bd81645f"),f.Native<BlueprintFeature>("455ac88e22f55804ab87c2467deff1d6"),f.Native<BlueprintFeature>("018af8005220ac94a9a4f47b3e9c2b4e"),f.Native<BlueprintFeature>("9054d3988d491d944ac144e27b6bc318"),f.Native<BlueprintFeature>("706e61781d692a042b35941f14bc41c5"));
            var around=new AbilityTargetsAround {name="DazeMassTargets"};Set(around,"m_Radius",new Feet(15));Set(around,"m_TargetType",TargetType.Enemy);Set(around,"m_Condition",new ConditionsChecker {Conditions=Array.Empty<Condition>()});
            var daze=f.Ability("DazeMass","Daze, Mass",dazeText,AbilityType.Spell,UnitCommand.CommandType.Standard,AbilityRange.Close,
                new SpellComponent {name="DazeMassSchool",School=SpellSchool.Enchantment},new SpellDescriptorComponent {name="DazeMassDescriptors",Descriptor=SpellDescriptor.Compulsion|SpellDescriptor.MindAffecting},around,
                Run(new Conditional {ConditionsChecker=forbidden,IfTrue=Actions(),IfFalse=Actions(gate)}),
                new AbilitySpawnFx {name="DazeMassVisual",PrefabLink=new PrefabLink {AssetId="28b3cd92c1fdc194d9ee1e378c23be6b"},Time=AbilitySpawnFxTime.OnApplyEffect,Anchor=AbilitySpawnFxAnchor.SelectedTarget,PositionAnchor=AbilitySpawnFxAnchor.None,OrientationAnchor=AbilitySpawnFxAnchor.None});
            Set(daze,"m_Icon",donor.Icon);daze.CanTargetPoint=true;daze.CanTargetSelf=false;daze.CanTargetFriends=false;daze.CanTargetEnemies=true;daze.SpellResistance=true;
            daze.LocalizedDuration=Text("DazeMass","duration","1 round");daze.LocalizedSavingThrow=Text("DazeMass","savingThrow","Will negates");
            daze.AvailableMetamagic=Metamagic.Heighten|Metamagic.Extend|Metamagic.Quicken|Metamagic.Reach|Metamagic.Piercing|Metamagic.Persistent;
            yield return new PsychicSpells.Entry {Ability=daze,Level=3};
        }
        static ConditionsChecker CheckAny(params BlueprintFeature[] facts)=>new ConditionsChecker {Operation=Operation.Or,Conditions=facts.Select(f=>{var c=new ContextConditionHasFact();Set(c,"m_Fact",Ref<BlueprintUnitFactReference>(f));return (Condition)c;}).ToArray()};
    }

    // Wrath already offers the exact event CotW had to patch in Kingmaker.
    // Native buff activation/suppression/removal owns subscription lifetime.
    [AllowedOn(typeof(BlueprintBuff),false)]
    [TypeId("a0be59f8e1344692931324b3fb1d065c")]
    public sealed class PsychicWardingWeapon : UnitFactComponentDelegate,IUnitTryCastingDefensivelyHandler,IUnitSubscriber
    {
        public void HandleUnitTryCastingDefensively(AbilityData ability,ref bool forceSuccess)
        {
            if(ability?.Caster==Owner && (ability.Blueprint.Type==AbilityType.Spell || ability.Blueprint.Type==AbilityType.SpellLike))forceSuccess=true;
        }
    }
}

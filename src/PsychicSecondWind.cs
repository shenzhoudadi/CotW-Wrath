using Kingmaker.Blueprints;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.ResourceLinks;
using Kingmaker.RuleSystem;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components;
using Kingmaker.UnitLogic.Abilities.Components.Base;
using Kingmaker.UnitLogic.ActivatableAbilities;
using Kingmaker.UnitLogic.Commands.Base;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Mechanics.Actions;
using Kingmaker.UnitLogic.Mechanics.Components;
using static CallOfTheWild.Wrath.Prototype.PsychicAssets;
namespace CallOfTheWild.Wrath.Prototype
{
    internal static class PsychicSecondWind
    {
        internal static void Build(PsychicAssets f)
        {
            const string text="You can cast this spell only when you have fewer than one-quarter of your maximum hit points. With a gasping utterance, you summon invigorating air to fill your lungs. You heal 2d8 points of damage + 1 point per caster level (maximum +10).";
            var rank=new ContextRankConfig {name="SecondWindCasterLevel"};SetEnum(rank,"m_BaseValueType","CasterLevel");Set(rank,"m_UseMax",true);Set(rank,"m_Max",10);
            var heal=new ContextActionHealTarget {Value=new ContextDiceValue {DiceType=DiceType.D8,DiceCountValue=2,BonusValue=new ContextValue {ValueType=ContextValueType.Rank}}};
            var spell=f.Ability("SecondWind","Second Wind",text,AbilityType.Spell,UnitCommand.CommandType.Swift,AbilityRange.Personal,
                new SpellComponent {name="SecondWindSchool",School=SpellSchool.Conjuration},new SpellDescriptorComponent {name="SecondWindHealing",Descriptor=SpellDescriptor.Cure|SpellDescriptor.RestoreHP},rank,
                new PsychicSecondWindRequirement {name="SecondWindLowHealth"},Run(heal),
                new AbilitySpawnFx {name="SecondWindVisual",PrefabLink=new PrefabLink {AssetId="e9399b6d57369ab4a9c3d88798d92f33"},Time=AbilitySpawnFxTime.OnApplyEffect,Anchor=AbilitySpawnFxAnchor.SelectedTarget,PositionAnchor=AbilitySpawnFxAnchor.None,OrientationAnchor=AbilitySpawnFxAnchor.None});
            Set(spell,"m_Icon",f.Native<BlueprintActivatableAbility>("4ebaf39efb8ffb64baf92784808dc49c").Icon);
            spell.SpellResistance=false;spell.AvailableMetamagic=Metamagic.Empower|Metamagic.Maximize|Metamagic.Heighten;
            spell.LocalizedDuration=Text("SecondWind","duration","Instantaneous");spell.LocalizedSavingThrow=Text("SecondWind","savingThrow","");
        }
    }
    [AllowedOn(typeof(BlueprintAbility),false)]
    [TypeId("dd233fc51baf48a6a3bd1adca7916fde")]
    public sealed class PsychicSecondWindRequirement : BlueprintComponent,IAbilityCasterRestriction
    {
        public bool IsCasterRestrictionPassed(UnitEntityData caster)
        {
            var hp=caster?.Stats?.HitPoints;
            return hp!=null&&hp.ModifiedValue>0&&(long)caster.HPLeft*4<hp.ModifiedValue;
        }
        public string GetAbilityCasterRestrictionUIText()=>"Requires fewer than one-quarter of maximum hit points.";
    }
}

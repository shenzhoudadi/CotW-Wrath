using System.Collections.Generic;
using Kingmaker.UnitLogic.Abilities.Components.Base;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Enums;
using Kingmaker.Enums.Damage;
using Kingmaker.ResourceLinks;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components;
using Kingmaker.UnitLogic.Commands.Base;
using Kingmaker.UnitLogic.FactLogic;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Mechanics.Components;
using static CallOfTheWild.Wrath.Prototype.PsychicAssets;
namespace CallOfTheWild.Wrath.Prototype
{
    internal static class PsychicFluidForm
    {
        internal static IEnumerable<PsychicSpells.Entry> Build(PsychicAssets f)
        {
            const string text="Your body becomes fluid for 1 minute per caster level. You gain damage reduction 10/slashing, a 10-foot enhancement bonus to melee reach, and the water subtype. This does not grant concealment or extend ranged weapon range.";
            var buff=f.Buff("FluidFormBuff","Fluid Form",text,
                new AddDamageResistancePhysical {name="FluidFormResistance",Value=10,BypassedByForm=true,Form=PhysicalDamageForm.Slashing},
                // Wrath Reach is measured directly in feet (CharacterStats.ReachRange).
                new AddStatBonus {name="FluidFormReach",Stat=StatType.Reach,Value=10,Descriptor=ModifierDescriptor.Enhancement},
                Facts(f.Native<BlueprintFeature>("bf7ee56ec9e43c14fa17727997e91993")));
            var icon=f.Native<BlueprintAbility>("40681ea748d98f54ba7f5dc704507f39").Icon;
            Set(buff,"m_Icon",icon);
            var apply=Apply(buff,0,DurationRate.Minutes);apply.AsChild=false;apply.DurationValue.BonusValue=new ContextValue {ValueType=ContextValueType.Rank};
            var rank=new ContextRankConfig {name="FluidFormDuration"};SetEnum(rank,"m_BaseValueType","CasterLevel");
            var spell=f.Ability("FluidForm","Fluid Form",text,AbilityType.Spell,UnitCommand.CommandType.Standard,AbilityRange.Personal,
                new SpellComponent {name="FluidFormSchool",School=SpellSchool.Transmutation},rank,Run(apply),
                new AbilitySpawnFx {name="FluidFormVisual",PrefabLink=new PrefabLink {AssetId="44295e9774b2864488f4a3790b8b0bcf"},Time=AbilitySpawnFxTime.OnApplyEffect,Anchor=AbilitySpawnFxAnchor.SelectedTarget,PositionAnchor=AbilitySpawnFxAnchor.None,OrientationAnchor=AbilitySpawnFxAnchor.None});
            Set(spell,"m_Icon",icon);spell.SpellResistance=false;
            spell.LocalizedDuration=Text("FluidForm","duration","1 minute per caster level");
            spell.LocalizedSavingThrow=Text("FluidForm","savingThrow","");
            spell.AvailableMetamagic=Metamagic.Extend|Metamagic.Heighten|Metamagic.Quicken;
            yield return new PsychicSpells.Entry {Ability=spell,Level=6};
        }
    }
}

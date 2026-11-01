using System;
using System.Linq;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Components;
using Kingmaker.UnitLogic.Abilities.Components.Base;
using Kingmaker.UnitLogic.FactLogic;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Mechanics.Components;
using static CallOfTheWild.Wrath.Prototype.PsychicAssets;

namespace CallOfTheWild.Wrath.Prototype
{
    internal static class PsychicUtilitySpells
    {
        internal static PsychicSpells.Entry CountlessEyes(PsychicAssets f)
        {
            const string text="The target sprouts extra eyes all over its body, including on the back of its head. It gains all-around vision and cannot be flanked for 1 hour per caster level. This does not prevent sneak attacks from other causes, such as being flat-footed.";
            var donor=f.Native<BlueprintAbility>("c927a8b0cd3f5174f8c0b67cdbfde539");
            var immunity=new AddMechanicsFeature {name="CountlessEyesCannotBeFlanked"};SetEnum(immunity,"m_Feature","CannotBeFlanked");
            var buff=f.Buff("CountlessEyesBuff","Countless Eyes",text,immunity);Set(buff,"m_Icon",donor.Icon);
            var spell=f.Clone(donor,"CountlessEyes","Countless Eyes",text);
            var rank=new ContextRankConfig {name="CountlessEyesCasterLevel"};SetEnum(rank,"m_BaseValueType","CasterLevel");
            var apply=Apply(buff,0,DurationRate.Hours);apply.DurationValue.BonusValue=new ContextValue {ValueType=ContextValueType.Rank};
            spell.ComponentsArray=spell.ComponentsArray.Where(c=>c is AbilitySpawnFx).Concat(new BlueprintComponent[]{
                new SpellComponent {name="CountlessEyesTransmutation",School=SpellSchool.Transmutation},rank,Run(apply)}).ToArray();
            spell.CanTargetSelf=true;spell.CanTargetFriends=true;spell.CanTargetEnemies=false;spell.SpellResistance=false;
            spell.AvailableMetamagic=Metamagic.Heighten|Metamagic.Quicken|Metamagic.Reach|Metamagic.Extend;
            spell.LocalizedDuration=Text("CountlessEyes","duration","1 hour per caster level");
            return new PsychicSpells.Entry {Token="CountlessEyes",Level=3,Ability=spell};
        }
    }
}

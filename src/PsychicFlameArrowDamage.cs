using System.Linq;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.Enums;
using Kingmaker.Enums.Damage;
using Kingmaker.PubSubSystem;
using Kingmaker.RuleSystem;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.RuleSystem.Rules.Damage;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Buffs.Blueprints;

namespace CallOfTheWild.Wrath.Prototype
{
    // Same weapon-stat route used by TTT's Geniekind fix. No TTT dependency:
    // both equipment previews and actual attacks consume this description.
    [AllowedOn(typeof(BlueprintBuff),false)]
    [TypeId("ce843be4ca2d4da58b7140be2ea47271")]
    public sealed class PsychicFlameArrowDamage : UnitFactComponentDelegate,
        IInitiatorRulebookHandler<RuleCalculateWeaponStats>,
        IInitiatorRulebookHandler<RulePrepareDamage>,IInitiatorRulebookSubscriber
    {
        public WeaponCategory[] Categories;
        public void OnEventAboutToTrigger(RuleCalculateWeaponStats evt)
        {
            if(evt.Weapon==null || !Categories.Contains(evt.Weapon.Blueprint.Category))return;
            evt.DamageDescription.Add(new DamageDescription {
                TypeDescription=new DamageTypeDescription {Type=DamageType.Energy,Energy=DamageEnergyType.Fire},
                Dice=new DiceFormula(1,DiceType.D6),SourceFact=Fact});
        }
        public void OnEventDidTrigger(RuleCalculateWeaponStats evt) { }
        public void OnEventAboutToTrigger(RulePrepareDamage evt)
        {
            foreach(var damage in evt.DamageBundle.Where(d=>d.SourceFact==Fact)) {
                // Affect only our packet, never the weapon/other elemental dice.
                damage.CriticalModifier=1;
                if(Context.HasMetamagic(Metamagic.Empower))damage.EmpowerBonus.Set(1.5f,Metamagic.Empower);
                if(Context.HasMetamagic(Metamagic.Maximize))damage.CalculationType.Set(DamageCalculationType.Maximized,Metamagic.Maximize);
            }
        }
        public void OnEventDidTrigger(RulePrepareDamage evt) { }
    }
}

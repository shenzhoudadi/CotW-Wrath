using System;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.UnitLogic.FactLogic;
using Kingmaker.UnitLogic.Parts;

namespace CallOfTheWild.Wrath.Prototype
{
    // Use the native resistance runtime, pool, immunity, suppression and energy
    // bypass rules. Only its value changes; existing sources are never edited.
    [TypeId("f0e10314bc6d4eaf885f26a5b441c912")]
    public sealed class PrimalSummonResistance01444 : AddDamageResistanceEnergy
    {
        protected override int CalculateValue(ComponentRuntime runtime)
        {
            var part=runtime.Owner.Get<UnitPartDamageReduction>();
            int strongest=0,stackable=0;
            if(part!=null)foreach(var source in part.AllSources) {
                if(source.Settings is PrimalSummonResistance01444 ||
                    !(source.Settings is AddDamageResistanceEnergy energy) || energy.Type!=Type ||
                    source.Fact==null || !source.Fact.IsActive || !source.Fact.IsTurnedOn || source.ShouldBeRemoved)continue;
                int value=Math.Max(0,source.GetCurrentValue());
                if(source.Settings.IsStackable)stackable+=value;else strongest=Math.Max(strongest,value);
            }
            // Native DR adds stackable sources separately. Do not count them
            // twice in this nonstackable candidate's effective base value.
            return strongest>0?strongest+5:stackable>0?5:10;
        }
    }
}

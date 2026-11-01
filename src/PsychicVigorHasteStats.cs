using System.Linq;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.EntitySystem;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Enums;
using Kingmaker.PubSubSystem;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Buffs.Blueprints;
namespace CallOfTheWild.Wrath.Prototype
{
    [AllowedOn(typeof(BlueprintBuff),false)]
    [TypeId("734fa9669ca34d18a93ac42d2d48ef05")]
    public sealed class PsychicVigorHasteStats : UnitFactComponentDelegate,
        IUnitGainFactHandler,IUnitLostFactHandler,IUnitSubscriber
    {
        public BlueprintBuffReference NativeEffect;
        static readonly StatType[] Stats={StatType.AC,StatType.SaveReflex,StatType.AdditionalAttackBonus};
        void Clear(){foreach(var stat in Stats)Owner.Stats.GetStat(stat)?.RemoveModifiersFrom(Runtime);}
        void Refresh(EntityFact leaving=null,bool entering=false){
            Clear();
            if(entering||Owner.Facts.List.Any(f=>f!=leaving&&!f.IsDisposed&&f.Blueprint==NativeEffect.Get()))return;
            foreach(var stat in Stats)Owner.Stats.GetStat(stat)?.AddModifier(1,Runtime,stat==StatType.AdditionalAttackBonus?ModifierDescriptor.UntypedStackable:ModifierDescriptor.Dodge);
        }
        protected override void OnTurnOn()=>Refresh();
        protected override void OnTurnOff()=>Clear();
        public void HandleUnitGainFact(EntityFact fact){if(fact?.Blueprint==NativeEffect.Get())Refresh(entering:true);}
        public void HandleUnitLostFact(EntityFact fact){if(fact?.Blueprint==NativeEffect.Get())Refresh(leaving:fact);}
    }
}

using Kingmaker.Controllers.Units;
using System;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Designers.Mechanics.Facts;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Mechanics;

namespace Way_of_the_shield.NewComponents
{
    public class AuraFeatureComponentLadder : AuraFeatureComponent, ITickEachRound
    {
        public BlueprintBuffReference[] m_Buffs;
        public BlueprintFeatureReference m_featureToCheck;
        public bool UseDuration;
        public ContextDurationValue Duration;

        public BlueprintBuff TrueBuff
        {
            get
            {
                if (m_Buffs == null || m_Buffs.Length == 0) return base.Buff;
                UnitEntityData caster = Fact.MaybeContext?.MaybeCaster ?? Owner;
                if (caster == null) return null;
                BlueprintFeature feature = m_featureToCheck?.Get();
                int rank = feature == null ? Fact.GetRank() : caster.Progression.Features.GetRank(feature);
                for (int index = Math.Min(rank, m_Buffs.Length) - 1; index >= 0; index--)
                {
                    BlueprintBuff candidate = m_Buffs[index]?.Get();
                    if (candidate != null) return candidate;
                }
                return null;
            }
        }
        private void Refresh()
        {
            BlueprintBuff desired = Fact.Active ? TrueBuff : null;
            if (Data.AppliedBuff != null && Data.AppliedBuff.Blueprint == desired) return;
            Data.AppliedBuff?.Remove();
            Data.AppliedBuff = null;
            if (desired == null) return;
            TimeSpan? duration = UseDuration && Duration != null && Fact.MaybeContext != null
                ? Duration.Calculate(Fact.MaybeContext).Seconds : (TimeSpan?)null;
            Data.AppliedBuff = Owner.AddBuff(desired, Fact.MaybeContext, duration);
        }
        protected override void OnActivate() { Refresh(); }
        protected override void OnApplyPostLoadFixes() { Refresh(); }
        void ITickEachRound.OnNewRound() { Refresh(); }
    }
}
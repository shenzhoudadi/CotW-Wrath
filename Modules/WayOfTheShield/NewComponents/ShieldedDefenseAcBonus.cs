using Kingmaker.EntitySystem.Stats;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.Blueprints.Items.Armors;
using System.Linq;
using Kingmaker.Controllers.Units;

namespace Way_of_the_shield.NewComponents
{
    public class ShieldedDefenseAcBonus : UnitFactComponentDelegate, ITargetRulebookHandler<RuleCalculateAC>, ITickEachRound
    {
        public ContextValue Value;
        public ModifiableValue.StackMode StackMode;
        public ModifierDescriptor Descriptor = ModifierDescriptor.None;
        public void Refresh()
        {
            var ac=Owner?.Stats?.AC;if(ac==null)return;
            var shield=Owner.Body?.SecondaryHand?.MaybeShield;var group=shield?.Blueprint.Type.ProficiencyGroup;
            bool valid=Fact.IsTurnedOn && Value!=null && (Value.IsValueSimple||Fact.MaybeContext!=null) &&
                Owner.HasFact(ResourcesLibrary.TryGetBlueprint<BlueprintBuff>("807ca5b2501444aab22a0f08a64691e1")) &&
                Owner.HasFact(ResourcesLibrary.TryGetBlueprint<BlueprintBuff>("6ffd93355fb3bcf4592a5d976b1d32a9")) &&
                group is ArmorProficiencyGroup.LightShield or ArmorProficiencyGroup.HeavyShield or ArmorProficiencyGroup.TowerShield &&
                Owner.Proficiencies.Contains(group.Value) && Owner.Body.PrimaryHand.MaybeWeapon?.HoldInTwoHands!=true && !ShieldRuntimePatches.IsShieldDenied(Owner);
            int bonus=valid?System.Math.Max(0,Value.Calculate(Fact.MaybeContext)):0;
            var old=ac.Modifiers.Where(m=>m.Source==Fact&&m.SourceComponent==Runtime.SourceBlueprintComponentName).ToArray();
            if(old.Length==1&&old[0].ModValue==bonus&&bonus>0)return;
            foreach(var m in old)m.Remove();
            if(bonus>0)ac.AddModifier(new ModifiableValue.Modifier{ModValue=bonus,ModDescriptor=Descriptor,StackMode=StackMode,Source=Fact,SourceComponent=Runtime.SourceBlueprintComponentName});
        }
        protected override void OnTurnOn()=>Refresh();
        protected override void OnTurnOff()=>Owner?.Stats?.AC?.RemoveModifiersFrom(Runtime);
        protected override void OnApplyPostLoadFixes()=>Refresh();
        public void OnEventAboutToTrigger(RuleCalculateAC evt)=>Refresh();
        void ITickEachRound.OnNewRound()=>Refresh();
        public void OnEventDidTrigger(RuleCalculateAC evt) { }
    }
}

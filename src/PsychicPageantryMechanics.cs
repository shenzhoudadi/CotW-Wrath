using System;
using System.Linq;
using System.Runtime.CompilerServices;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.Enums;
using Kingmaker.PubSubSystem;
using Kingmaker.RuleSystem;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.RuleSystem.Rules.Abilities;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Mechanics.Actions;

namespace CallOfTheWild.Wrath.Prototype
{
    [TypeId("3fb4cb456de8497cb17a2b3018116e17")]
    public sealed class PsychicRitualUnityRestore : ContextAction
    {
        public override string GetCaption()=>"Restore 1 Pageantry phrenic point to the caster";
        public override void RunAction()
        {
            var owner=Context?.MaybeCaster;
            var pool=ResourcesLibrary.TryGetBlueprint<BlueprintAbilityResource>(PsychicClass.CharismaPoolId);
            if(owner!=null && pool!=null && owner.Descriptor.Resources.ContainsResource(pool)) owner.Descriptor.Resources.Restore(pool,1);
        }
    }

    [TypeId("6efc218fb04b4adebfc900e71c9d3448")]
    public sealed class PsychicPageantryPower : UnitFactComponentDelegate,
        IInitiatorRulebookHandler<RuleCalculateAbilityParams>,IInitiatorRulebookHandler<RuleCastSpell>,IInitiatorRulebookHandler<RuleCheckConcentration>,IInitiatorRulebookSubscriber,
        ITurnBasedUnitTurnEndedHandler,IGlobalSubscriber
    {
        internal static bool Eligible(AbilityData spell)=>spell?.Blueprint.Type==AbilityType.Spell && spell.SourceItem==null &&
            spell.Spellbook?.Blueprint.AssetGuid==BlueprintGuid.Parse(PsychicClass.BookId);
        public void OnEventAboutToTrigger(RuleCalculateAbilityParams evt)
        {
            if(evt.Initiator!=Owner || !Eligible(evt.AbilityData)) return;
            evt.AddBonusCasterLevel(1,ModifierDescriptor.UntypedStackable);evt.AddBonusDC(1,ModifierDescriptor.UntypedStackable);
        }
        public void OnEventDidTrigger(RuleCalculateAbilityParams evt) { }
        public void OnEventAboutToTrigger(RuleCheckConcentration evt) { }
        public void OnEventDidTrigger(RuleCheckConcentration evt)
        {
            // A failed concentration check may cancel UnitUseAbility before it
            // ever creates RuleCastSpell. Do not leave the preparation reusable.
            if(evt.Initiator==Owner && Eligible(evt.Spell) && !evt.Success) Owner.RemoveFact(Fact);
        }
        public void OnEventAboutToTrigger(RuleCastSpell evt) { }
        public void OnEventDidTrigger(RuleCastSpell evt)
        {
            // The next attempted Psychic cast consumes the preparation, including
            // concentration failure; previews, items and other books do not.
            if(evt.Initiator==Owner && Eligible(evt.Spell) && !evt.IsDuplicateSpellApplied && evt.Context?.IsDuplicateSpellApplied!=true) Owner.RemoveFact(Fact);
        }
        public void HandleUnitTurnEnded(UnitEntityData unit) {if(unit==Owner) Owner.RemoveFact(Fact);}
    }

    internal static class PsychicPageantryFocus
    {
        private static readonly ConditionalWeakTable<RuleRollDice,object> Paid=new ConditionalWeakTable<RuleRollDice,object>();
        internal static void Apply(RuleRollDice roll,RulebookEventContext context)
        {
            if(!(roll is RuleRollD20) || !(context?.PreviousEvent is RuleCheckConcentration check) || check.Initiator!=roll.Initiator || Paid.TryGetValue(roll,out _)) return;
            var owner=roll.Initiator;
            if(owner?.Buffs==null || !owner.Buffs.Enumerable.Any(b=>b.IsTurnedOn && !b.IsSuppressed && b.Blueprint.AssetGuid==PsychicAssets.Id("PageantryFocusBuff"))) return;
            var pool=ResourcesLibrary.TryGetBlueprint<BlueprintAbilityResource>(PsychicClass.CharismaPoolId);
            if(pool==null || !owner.Descriptor.Resources.ContainsResource(pool) || owner.Descriptor.Resources.GetResourceAmount(pool)<1) return;
            owner.Descriptor.Resources.Spend(pool,1);Paid.Add(roll,new object());roll.Override(20);
        }
    }
}

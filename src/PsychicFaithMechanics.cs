using System;
using System.Linq;
using System.Runtime.CompilerServices;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.Enums;
using Kingmaker.PubSubSystem;
using Kingmaker.RuleSystem.Rules.Abilities;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Alignments;
using Kingmaker.UnitLogic.Mechanics.Conditions;

namespace CallOfTheWild.Wrath.Prototype
{
    [TypeId("d427dbb219d248909545e355effcc104")]
    public sealed class PsychicFaithConversionRestore : UnitFactComponentDelegate,
        IInitiatorRulebookHandler<RuleCastSpell>, IInitiatorRulebookSubscriber
    {
        public BlueprintAbilityReference[] Conversions=Array.Empty<BlueprintAbilityReference>();
        private static readonly ConditionalWeakTable<RuleCastSpell,object> Restored=new ConditionalWeakTable<RuleCastSpell,object>();
        public void OnEventAboutToTrigger(RuleCastSpell evt) { }
        public void OnEventDidTrigger(RuleCastSpell evt)
        {
            var spell=evt.Spell;
            if(!evt.Success || evt.IsDuplicateSpellApplied || evt.Context?.IsDuplicateSpellApplied==true || evt.Initiator!=Owner || spell==null || spell.ConvertedFrom==null || spell.SourceItem!=null ||
                spell.Spellbook?.Blueprint.AssetGuid!=BlueprintGuid.Parse(PsychicClass.BookId) || !Conversions.Any(r=>r.Guid==spell.Blueprint.AssetGuid) || Restored.TryGetValue(evt,out _)) return;
            Restored.Add(evt,new object());
            var pool=ResourcesLibrary.TryGetBlueprint<BlueprintAbilityResource>(PsychicClass.WisdomPoolId);
            if(pool!=null && Owner.Descriptor.Resources.ContainsResource(pool)) Owner.Descriptor.Resources.Restore(pool,1);
        }
    }

    [TypeId("7ae7c0ce93854033a648b7393d76d5b5")]
    public sealed class PsychicFaithOpposedAlignment : ContextCondition
    {
        protected override string GetConditionCaption()=>"Faith: opposing good/evil or law/chaos alignment";
        protected override bool CheckCondition()
        {
            var caster=Context?.MaybeCaster;var target=Target?.Unit;
            if(caster==null || target==null) return false;
            var a=caster.Descriptor.Alignment.ValueRaw;var b=target.Descriptor.Alignment.ValueRaw;
            return Math.Abs(a.GetGoodness()-b.GetGoodness())==2 || Math.Abs(a.GetLawfulness()-b.GetLawfulness())==2;
        }
    }
}

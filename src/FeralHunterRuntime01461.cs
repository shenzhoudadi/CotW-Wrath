using System;
using System.Linq;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.PubSubSystem;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.RuleSystem.Rules.Abilities;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Abilities;using Kingmaker.UnitLogic.Mechanics;using Kingmaker.UnitLogic.Mechanics.Components;using Kingmaker.EntitySystem.Stats;
using Kingmaker.ElementsSystem;using Kingmaker.UnitLogic.Mechanics.Actions;
namespace CallOfTheWild.Wrath.Prototype
{
    [TypeId("45ec5161b9324c20ac57a16f61000103")]
    public sealed class FeralClearOwnSummons01461:ContextAction
    {
        public BlueprintSummonPoolReference Pool;
        public override string GetCaption()=>"End this feral hunter's previous nature's allies";
        public override void RunAction(){
            var caster=Context.MaybeCaster;if(caster==null)return;
            var pool=Kingmaker.Game.Instance.SummonPools.GetPool(Pool.Get());if(pool==null)return;
            var summoned=Kingmaker.Game.Instance.BlueprintRoot.SystemMechanics.SummonedUnitBuff;
            foreach(var unit in pool.Units.ToArray())foreach(var buff in unit.Buffs.Enumerable.Where(b=>b.Blueprint==summoned&&b.Context?.MaybeCaster==caster).ToArray())buff.Remove();
        }
    }
    [TypeId("45ec5161b9324c20ac57a16f61000101")]
    public sealed class FeralSummonTactics01461:UnitFactComponentDelegate,IInitiatorRulebookHandler<RuleSummonUnit>,IInitiatorRulebookSubscriber
    {
        public BlueprintAbilityReference[] Origins;
        public BlueprintBuffReference Effect;
        public void OnEventAboutToTrigger(RuleSummonUnit evt){}
        public void OnEventDidTrigger(RuleSummonUnit evt)
        {
            if(evt.SummonedUnit==null||evt.Context?.MaybeCaster!=Owner||evt.Initiator!=Owner)return;
            var source=evt.Context.SourceAbility;bool match=false;for(int i=0;source!=null&&i<16;i++,source=source.Parent)if(Origins?.Any(r=>r.Guid==source.AssetGuid)==true){match=true;break;}
            if(!match||evt.SummonedUnit.Buffs.Enumerable.Any(b=>b.Blueprint==Effect.Get()&&b.Context?.MaybeCaster==Owner))return;
            var buff=evt.SummonedUnit.Descriptor.AddBuff(Effect.Get(),evt.Context,null);if(buff!=null)buff.IsNotDispelable=true;
        }
    }
}

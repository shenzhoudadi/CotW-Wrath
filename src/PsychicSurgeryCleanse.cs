using System.Linq;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.UnitLogic.Buffs;
using Kingmaker.UnitLogic.FactLogic;
using Kingmaker.UnitLogic.Mechanics.Actions;
using Kingmaker.UnitLogic;
using Kingmaker.Designers.Mechanics.Buffs;
using Kingmaker.UnitLogic.Abilities.Blueprints;

namespace CallOfTheWild.Wrath.Prototype
{
    [TypeId("8f02b646d560411ba9674cd10a6024fd")]
    public sealed class PsychicSurgeryCleanse : ContextAction
    {
        // Audited native debuff: both it and its source spell omit harmful flags.
        // Do not classify arbitrary mixed-benefit buffs merely by a stat penalty.
        private static readonly BlueprintGuid CrushingDespairBuffId=BlueprintGuid.Parse("36897b36a4bd63146b1d8509a17fc5ad");
        public override string GetCaption()=>"Remove ongoing mental afflictions";
        internal static bool CanRemove(Buff buff)
        {
            if(buff==null) return false;
            var context=buff.Context;
            var source=context?.SourceAbility;
            var descriptor=(context?.SpellDescriptor??SpellDescriptor.None) |
                (buff.Blueprint.GetComponent<SpellDescriptorComponent>()?.Descriptor??SpellDescriptor.None);
            var conditions=buff.Blueprint.GetComponents<AddCondition>().Select(c=>c.Condition)
                .Concat(buff.Blueprint.GetComponents<BuffStatusCondition>().Select(c=>c.Condition)).ToArray();
            // Native harmful spell buffs often omit BlueprintBuff.Harmful.
            // Use their offensive source as additional evidence, never the mere
            // presence of a mental descriptor (also used by beneficial buffs).
            bool harmful=buff.Blueprint.Harmful || buff.Blueprint.AssetGuid==CrushingDespairBuffId || (source!=null && source.EffectOnEnemy==AbilityEffectOnUnit.Harmful &&
                source.EffectOnAlly!=AbilityEffectOnUnit.Helpful) ||
                conditions.Any(c=>c==UnitCondition.Confusion || c==UnitCondition.Frightened || c==UnitCondition.Shaken || c==UnitCondition.Paralyzed);
            // These conditions are removed at any level; permanent class facts
            // are never traversed, only removable Buff instances.
            bool fearOrConfusion=(harmful && (descriptor&(SpellDescriptor.Fear|SpellDescriptor.Shaken|SpellDescriptor.Frightened|SpellDescriptor.Confusion))!=0) ||
                conditions.Any(c=>c==UnitCondition.Confusion || c==UnitCondition.Frightened || c==UnitCondition.Shaken);
            if(fearOrConfusion) return true;
            if(!harmful || ((descriptor&SpellDescriptor.MindAffecting)==0 &&
                (context?.SpellSchool??SpellSchool.None)!=SpellSchool.Enchantment && source?.School!=SpellSchool.Enchantment)) return false;
            if(buff.IsFromSpell && !buff.IsNotDispelable) return true;
            // Unknown effect level is not permission to remove an innate ability.
            int level=context?.Params?.SpellLevel??0;
            return level>=1 && level<=6;
        }
        public override void RunAction()
        {
            var target=Target?.Unit;if(target==null)return;
            foreach(var buff in target.Buffs.Enumerable.ToArray()) if(CanRemove(buff)) buff.Remove();
        }
    }
}

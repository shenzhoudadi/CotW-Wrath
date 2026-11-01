using System.Linq;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components;
using Kingmaker.UnitLogic.Mechanics.Actions;
namespace CallOfTheWild.Wrath.Prototype{
 internal static class PsychicWilling01460{
  // Wrath has no independent willing-subject flag. Friendly harmful/save
  // effects must not be silently treated as consent; ordinary friendly
  // helpful effects without an offensive/save action remain voluntary.
  internal static bool Willing(AbilityExecutionContext context,UnitEntityData target){
   var caster=context?.MaybeCaster;var spell=context?.SourceAbility;
   if(caster==target)return true;
   if(caster==null||spell==null||caster.IsEnemy(target)||!spell.CanTargetFriends||spell.EffectOnAlly!=AbilityEffectOnUnit.Helpful)return false;
   var graph=PsychicRemainingDisciplines01458.Graph(spell.ComponentsArray).ToArray();
   return !graph.OfType<AbilityEffectRunAction>().Any(a=>a.SavingThrowType!=SavingThrowType.Unknown)&&!graph.OfType<ContextActionSavingThrow>().Any()&&!graph.OfType<ContextActionDealDamage>().Any();
  }
 }
}

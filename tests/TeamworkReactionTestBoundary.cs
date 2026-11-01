using HarmonyLib;
using Kingmaker.UnitLogic.Commands;
using Kingmaker.UnitLogic.Commands.Base;
using CallOfTheWild.Wrath.Prototype;

// Legacy rule tests stop at the scheduler/Unity boundary. Execute the command's
// actual action-frame method. Animation and queue timing have separate coverage.
internal static class TeamworkReactionTestBoundary {
 internal static void Patch(Harmony h)=>h.Patch(AccessTools.Method(typeof(TeamworkRangedReactionCommand).Assembly.GetType("CallOfTheWild.Wrath.Prototype.TeamworkRangedReaction"),"Enqueue"),prefix:new HarmonyMethod(typeof(TeamworkReactionTestBoundary),nameof(Run)));
 static bool Run(Kingmaker.EntitySystem.Entities.UnitEntityData __0,TeamworkRangedReactionCommand __1){
  __1.Init(__0);AccessTools.Method(typeof(TeamworkRangedReactionCommand),"OnAction").Invoke(__1,null);return false;
 }
}

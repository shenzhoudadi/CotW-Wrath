using System.Linq;
using HarmonyLib;
using Kingmaker.Blueprints;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities;
using static CallOfTheWild.Wrath.Prototype.PsychicAssets;

namespace CallOfTheWild.Wrath.Prototype
{
    internal static class PsychicMawCleanup
    {
        private static readonly BlueprintGuid RoarId=Id("SavageMawRoar"),MawId=Id("SavageMawBuff"),ReleaseId=Id("DazzlingBladeRelease"),DazzlingId=Id("DazzlingBladeBuff"),AspectId=Id("ThreefoldAspectChange"),AspectHostId=Id("ThreefoldAspectBuff"),BowId=Id("BowSpiritShot"),BowHostId=Id("BowSpiritBuff"),CloakId=Id("AlliedCloakAid"),CloakHostId=Id("AlliedCloakBuff"),SeedsId=Id("FireSeedsThrow"),SeedsHostId=Id("FireSeedsBuff"),ShurikenId=Id("FieryShurikenLaunch"),ShurikenHostId=Id("FieryShurikenBuff");
        // Native AddFacts is still the grant owner. Only bypass the branch that
        // keeps this temporary ability as a disabled, slotted fact. Normal
        // native detach/disposal/cache/UI notifications run afterwards.
        [HarmonyPatch(typeof(AbilityCollection),"PrepareFactForDetach",new[]{typeof(Ability)})]
        private static class Detach
        {
            [HarmonyPrefix]
            private static bool Prefix(Ability fact,ref Ability __result) {
                if(!PsychicStorageSpells.Tokens.SelectMany(t=>new[]{t+"StoreNext",t+"Release",t+"Dismiss"}).Any(t=>Id(t)==fact?.Blueprint?.AssetGuid)&&!PsychicSpiritWeapons.Tokens.SelectMany(t=>new[]{t+"Direct",t+"Dismiss"}).Any(t=>Id(t)==fact?.Blueprint?.AssetGuid)&&!PsychicShapeSpells.Forms.Any(t=>Id(t+"Return")==fact?.Blueprint?.AssetGuid)&&!PsychicOrbSpells.Tokens.Concat(new[]{"ScouringWinds","OrbOfTheVoid"}).Any(t=>Id(t+"Move")==fact?.Blueprint?.AssetGuid)&&fact?.Blueprint?.AssetGuid!=RoarId&&fact?.Blueprint?.AssetGuid!=ReleaseId&&fact?.Blueprint?.AssetGuid!=AspectId&&fact?.Blueprint?.AssetGuid!=BowId&&fact?.Blueprint?.AssetGuid!=CloakId&&fact?.Blueprint?.AssetGuid!=SeedsId&&fact?.Blueprint?.AssetGuid!=ShurikenId)return true;
                __result=fact;return false;
            }
        }
        internal static void Repair(UnitEntityData owner) {
            if(owner?.Facts==null)return;
            foreach(var pair in new[]{new[]{RoarId,MawId},new[]{ReleaseId,DazzlingId},new[]{AspectId,AspectHostId},new[]{BowId,BowHostId},new[]{CloakId,CloakHostId},new[]{SeedsId,SeedsHostId},new[]{ShurikenId,ShurikenHostId}}.Concat(PsychicOrbSpells.Tokens.Concat(new[]{"ScouringWinds","OrbOfTheVoid"}).Select(t=>new[]{Id(t+"Move"),Id(t+"Controller")})).Concat(PsychicSpiritWeapons.Tokens.SelectMany(t=>new[]{new[]{Id(t+"Direct"),Id(t+"Controller")},new[]{Id(t+"Dismiss"),Id(t+"Controller")}})).Concat(PsychicStorageSpells.Tokens.SelectMany(t=>new[]{new[]{Id(t+"StoreNext"),Id(t+"Buff")},new[]{Id(t+"Release"),Id(t+"Buff")},new[]{Id(t+"Dismiss"),Id(t+"Buff")}})).Concat(PsychicShapeSpells.Forms.Select(t=>new[]{Id(t+"Return"),Id(t+"Buff")}))){
                if(owner.Facts.List.Any(f=>f.Blueprint?.AssetGuid==pair[1]))continue;
                foreach(var ability in owner.Facts.List.OfType<Ability>().Where(a=>a.Blueprint.AssetGuid==pair[0]).ToArray())owner.RemoveFact(ability);
            }
        }
        [HarmonyPatch(typeof(UnitDescriptor),nameof(UnitDescriptor.ApplyPostLoadFixes))]
        private static class Restore
        {
            [HarmonyPostfix]
            private static void Postfix(UnitDescriptor __instance)=>Repair(__instance?.Unit);
        }
    }
}

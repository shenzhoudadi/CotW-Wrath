using Kingmaker.ResourceLinks;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Buffs.Blueprints;

namespace CallOfTheWild.Wrath.Prototype
{
    internal static class Visuals01448
    {
        // Original CotW CourtBard.cs assets, checked against the current game's
        // location map and actual prefab bundles. These are visual links only.
        internal static void Court(string performance,BlueprintAbilityAreaEffect area,BlueprintBuff host,BlueprintBuff target)
        {
            string ring,caster,effect;
            switch(performance) {
                case "Satire":ring="2f93a2909cb766f4d961aee34a3c84c2";caster="9353083f430e5a44a8e9d6e26faec248";effect="6e01d9f56e260ea4088836571d0e6404";break;
                case "Mockery":ring="79665f3d500fdf44083feccf4cbfc00a";caster="c87c798cd0a410c419ee4bafd4adb68f";effect="ff26ee73fa464a44ca8bf20e858dc3bc";break;
                case "GloriousEpic":ring="20caf000cd4c3434da00a74f4a49dccc";caster="39da71647ad4747468d41920d0edd721";effect="6e01d9f56e260ea4088836571d0e6404";break;
                default:
                    // KM's Scandal ring5d430... is absent from Wrath. Its native
                    // Inspire Courage ring is the same performance/50ft donor.
                    ring="2f93a2909cb766f4d961aee34a3c84c2";caster="c87c798cd0a410c419ee4bafd4adb68f";effect=null;break;
            }
            area.Fx=new PrefabLink{AssetId=ring};host.FxOnStart=new PrefabLink{AssetId=caster};
            if(effect!=null)target.FxOnStart=new PrefabLink{AssetId=effect};
        }
    }
}

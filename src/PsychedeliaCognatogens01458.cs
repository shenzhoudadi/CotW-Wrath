using System;
using System.Linq;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using HarmonyLib;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.RuleSystem.Rules.Abilities;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.FactLogic;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.Utility;

namespace CallOfTheWild.Wrath.Prototype
{
    internal static class PsychedeliaCognatogens01458
    {
        static readonly ConditionalWeakTable<BlueprintsCache,HashSet<BlueprintGuid>> Abilities=new ConditionalWeakTable<BlueprintsCache,HashSet<BlueprintGuid>>();
        static readonly BlueprintGuid Own=PsychicAssets.Id("PsychedeliaCognatogenFeature01458");
        internal static void Initialize(BlueprintsCache cache)=>Abilities.GetValue(cache,c=>{
            var ids=new HashSet<BlueprintGuid>();string[] features={"e3f460ea61fcc504183c7d6818bbbf7a","18eb29676492e844eb5a55d1c855ce69","af4a320648eb5724889d6ff6255090b2"};int[] counts={3,6,6};
            for(int i=0;i<features.Length;i++){
                var f=c.Load(BlueprintGuid.Parse(features[i])) as BlueprintFeature??throw new InvalidOperationException("058 native Cognatogen discovery missing.");
                var spells=f.GetComponents<AddFacts>().SelectMany(v=>v.Facts).OfType<BlueprintAbility>().ToArray();
                if(spells.Length!=counts[i])throw new InvalidOperationException("058 native Cognatogen discovery action graph changed.");
                foreach(var a in spells)ids.Add(a.AssetGuid);
            }
            return ids;
        });
        internal static bool Owns(UnitEntityData owner,BlueprintAbility ability)
        {
            var cache=ResourcesLibrary.BlueprintsCache;var feature=cache.Load(Own) as BlueprintFeature;
            return feature!=null&&owner?.HasFact(feature)==true&&ability!=null&&Abilities.TryGetValue(cache,out var ids)&&ids.Contains(ability.AssetGuid);
        }
    }
    [HarmonyPatch(typeof(RuleCalculateAbilityParams),nameof(RuleCalculateAbilityParams.OnTrigger))]
    internal static class PsychedeliaCognatogenLevel01458Patch
    {
        static void Prefix(RuleCalculateAbilityParams __instance)
        {
            if(!PsychedeliaCognatogens01458.Owns(__instance.Initiator,__instance.Spell))return;
            var alchemist=ResourcesLibrary.TryGetBlueprint<BlueprintCharacterClass>(BlueprintGuid.Parse("0937bec61c0dabc468428f496580c721"));
            __instance.ReplaceCasterLevel=PsychicDisciplineRuntime01458.Level(__instance.Initiator)+__instance.Initiator.Progression.GetClassLevel(alchemist);
        }
    }
    [HarmonyPatch(typeof(ContextDurationValue),nameof(ContextDurationValue.Calculate))]
    internal static class PsychedeliaCognatogenDuration01458Patch
    {
        static void Postfix(ContextDurationValue __instance,MechanicsContext __0,ref Rounds __result)
        {
            if(__instance.Rate==DurationRate.TenMinutes&&PsychedeliaCognatogens01458.Owns(__0?.MaybeCaster,__0?.SourceAbility))__result=new Rounds(__result.Value/10);
        }
    }
}

using System;
using System.Linq;
using System.Runtime.CompilerServices;
using HarmonyLib;
using Kingmaker.Blueprints;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.RuleSystem;
using Kingmaker.RuleSystem.Rules.Abilities;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Buffs;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Mechanics.Actions;
using static CallOfTheWild.Wrath.Prototype.PsychicAssets;

namespace CallOfTheWild.Wrath.Prototype {
 internal static class SpellResourceNative {
  internal sealed class Charge {internal Buff Buff;internal SpellBoostState State;internal int HexLevel,DC,SnapshotDC;internal bool Committed;}
  static readonly ConditionalWeakTable<AbilityExecutionContext,Charge> Casts=new ConditionalWeakTable<AbilityExecutionContext,Charge>();
  internal static bool BloodSpell(AbilityData a) {
   if(a?.Caster?.Unit==null||a.SourceItem!=null||a.Blueprint.Type!=AbilityType.Spell||a.SpellLevel<1)return false;
   var b=a.OverrideSpellbook??a.Spellbook??a.ConvertedFrom?.Spellbook;if(b==null||b.Owner!=a.Caster)return false;
   var id=BlueprintGuid.Parse(SpellResourceFeats.Bloodrager);
   return b.Blueprint.AssetGuid==BlueprintGuid.Parse("e19484252c2f80e4a9439b3681b20f00")||b.Blueprint.CharacterClass?.AssetGuid==id||a.Caster.Progression.Classes.Any(c=>c.CharacterClass.AssetGuid==id&&c.Spellbook?.AssetGuid==b.Blueprint.AssetGuid);
  }
  internal static Charge Armed(AbilityData a) {
   var u=a?.Caster?.Unit;if(u==null||a.SourceItem!=null)return null;
   bool hex=SpellResourceFeats.Hex(a.Blueprint),blood=BloodSpell(a);if(!hex&&!blood)return null;
   foreach(var b in u.Buffs.Enumerable.Where(b=>b.IsActive&&b.IsTurnedOn&&!b.IsSuppressed)) {
    var r=b.Components.FirstOrDefault(c=>hex?c.SourceBlueprintComponent is AmplifiedHexBoost:c.SourceBlueprintComponent is RageCastingBoost);if(r==null)continue;
    var s=r.GetData<SpellBoostState>();if(s.Consumed)continue;
    int level=hex?Math.Max(1,Math.Min(9,s.Level)):Math.Min(((RageCastingBoost)r.SourceBlueprintComponent).Dice,a.SpellLevel);
    return new Charge{Buff=b,State=s,DC=level,HexLevel=hex?level:0};
   }
   return null;
  }
  internal static void Capture(RuleCastSpell e) {
   if(e.Context==null||e.ForceFail||e.IsDuplicateSpellApplied||e.IsCutscene||e.Context.IsDuplicateSpellApplied||Casts.TryGetValue(e.Context,out _))return;
   var c=Armed(e.Spell);if(c==null)return;
   // Native Witch uses ContextSetAbilityParams and overwrites params in RecalculateRanks.
   // Capture the final AbilityData path, including OverrideParams, then keep it per cast.
   var p=e.Spell.CalculateParams().Clone();c.SnapshotDC=p.DC;e.Context.SetParams(p);Casts.Add(e.Context,c);
  }
  internal static Charge ForContext(MechanicsContext ctx) {
   var source=ctx?.SourceAbilityContext;return source!=null&&Casts.TryGetValue(source,out var c)?c:null;
  }
  internal static void Commit(RuleCastSpell e) {
   if(e.Context==null||!Casts.TryGetValue(e.Context,out var c)||c.Committed||!e.ShouldSpendResource||e.IsCutscene||e.IsDuplicateSpellApplied||e.Context.IsDuplicateSpellApplied||(!e.Success&&!e.IsSpellFailed&&!e.IsArcaneSpellFailed))return;
   c.Committed=true;c.State.Consumed=true;c.Buff.Remove();
   if(c.HexLevel==0){int hp=0;for(int i=0;i<c.DC;i++)hp+=RulebookEvent.Dice.D6;e.Initiator.Descriptor.Damage=checked(e.Initiator.Descriptor.Damage+hp);}
  }
 }
 [HarmonyPatch(typeof(AbilityData),nameof(AbilityData.CalculateParams))]
 internal static class SpellResourceParamsPatch {
  static void Postfix(AbilityData __instance,ref AbilityParams __result) {var c=SpellResourceNative.Armed(__instance);if(c==null||__result==null)return;__result=__result.Clone();__result.DC+=c.DC;}
 }
 [HarmonyPatch(typeof(RuleCastSpell),nameof(RuleCastSpell.OnTrigger))]
 internal static class SpellResourceCastPatch {
  static void Prefix(RuleCastSpell __instance)=>SpellResourceNative.Capture(__instance);
  static void Postfix(RuleCastSpell __instance)=>SpellResourceNative.Commit(__instance);
 }
 [HarmonyPatch(typeof(MechanicsContext),nameof(MechanicsContext.Recalculate))]
 internal static class SpellResourceRecalculatePatch {
  static void Postfix(MechanicsContext __instance) {ProphecyAccordance01431.RestorePaidMetamagic(__instance);var c=SpellResourceNative.ForContext(__instance);if(c==null||__instance.Params==null)return;var p=__instance.Params.Clone();p.DC=c.SnapshotDC;__instance.SetParams(p);}
 }
 [HarmonyPatch(typeof(AbilityData),nameof(AbilityData.GetApproachDistance))]
 internal static class AmplifiedHexRangePatch {
  static void Postfix(AbilityData __instance,ref float __result) {
   var c=SpellResourceNative.Armed(__instance);if(c?.HexLevel>0&&__instance.Range!=AbilityRange.Personal&&__instance.Range!=AbilityRange.Touch&&__instance.Range!=AbilityRange.Weapon&&__instance.Range!=AbilityRange.DoubleMove&&__instance.GetDeliverBlueprint().GetRange(false,__instance).Value>=30)__result+=((c.HexLevel+2)/3)*10*0.3048f;
  }
 }
 [HarmonyPatch(typeof(ContextActionApplyBuff),"CalculateDuration")]
 internal static class AmplifiedHexDurationPatch {
  static void Postfix(ContextActionApplyBuff __instance,MechanicsContext context,ref TimeSpan? __result) {
   var c=SpellResourceNative.ForContext(context);
   if(c?.HexLevel>0&&__result?.TotalSeconds>=6&&!__instance.Permanent&&!__instance.SameDuration&&__instance.Buff?.name?.IndexOf("Cooldown",StringComparison.OrdinalIgnoreCase)<0)__result+=TimeSpan.FromSeconds(c.HexLevel*6);
  }
 }
}

// Casting foundation adapted from the pinned CotW Psychic/SpellFailureMechanics sources (MIT).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.Items;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.RuleSystem.Rules.Abilities;
using Kingmaker.UI.Common;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.FactLogic;
using Kingmaker.Utility;

namespace CallOfTheWild.Wrath.Prototype
{
    [AllowedOn(typeof(BlueprintSpellbook), false)]
    [TypeId("d13da02a408057b09b20a83d5a0e7026")]
    public sealed class PsychicSpellbookMarker : BlueprintComponent
    {
        public int MaximumSpellLevel = 9;
    }

    internal static class PsychicCasting
    {
        internal static bool RuntimePatchesAvailable = true;
        // 0x400 used by Kingmaker CotW is native Piercing in Wrath. Never reuse it.
        // 1<<29 also avoids the bits in the pinned DarkCodex/CodexLib inventory.
        internal const Metamagic Logical = (Metamagic)(1 << 29);
        internal const SpellDescriptor EmotionBlock = SpellDescriptor.NegativeEmotion | SpellDescriptor.Fear |
            SpellDescriptor.Shaken | SpellDescriptor.Frightened;
        private static readonly BlueprintGuid CenterId = BlueprintGuid.Parse(PsychicClass.CenterBuffId);
        internal static readonly HashSet<BlueprintGuid> CenterBuffs = new HashSet<BlueprintGuid> { CenterId };
        private static readonly HashSet<BlueprintGuid> SilenceIds = new HashSet<BlueprintGuid> {
            // Audited native debuffs. Boss-wide anti-casting and nausea/confusion are not exempted.
            BlueprintGuid.Parse("e4b8a158e8aabfe41b5e0bd334d8c386"), // ThrowingAxeOfSilenceBuff
            PsychicAssets.Id("SilenceBuff"),
            BlueprintGuid.Parse("73e09d4c3e3a4d918988a0aae069f372")  // SlaughteringSilenceBuff
        };
        internal static bool IsPsychic(BlueprintSpellbook book) => book != null &&
            (book.ComponentsArray ?? Array.Empty<BlueprintComponent>()).Any(c => c is PsychicSpellbookMarker);
        internal static bool IsPsychic(AbilityData spell) => spell != null && spell.SourceItem == null &&
            spell.Blueprint.Type == Kingmaker.UnitLogic.Abilities.Blueprints.AbilityType.Spell && IsPsychic(spell.Spellbook?.Blueprint);

        internal static bool IsEmotionBlocked(AbilityData spell)
        {
            if (!IsPsychic(spell) || spell.HasMetamagic(Logical)) return false;
            return spell.Caster.Buffs.Enumerable.Any(b => !b.IsSuppressed && b.IsTurnedOn &&
                // This exact buff emits the aura; its owner is not a fear victim.
                // Native contexts inherit (and save) the spell's descriptors.
                // Keep explicit buff descriptors and every separate hostile buff.
                ((((b.Blueprint.AssetGuid == DoomAuraHostId ? SpellDescriptor.None : b.Context?.SpellDescriptor ?? SpellDescriptor.None)) |
                    BlueprintDescriptor(b.Blueprint)) & EmotionBlock) != 0);
        }

        private static SpellDescriptor BlueprintDescriptor(BlueprintScriptableObject blueprint)
            => blueprint.GetComponent<SpellDescriptorComponent>() is SpellDescriptorComponent component
                ? (SpellDescriptor)component.Descriptor : SpellDescriptor.None;

        internal static void ApplyThoughtPenalty(RuleCalculateAbilityParams evt)
        {
            if (!IsPsychic(evt.Spellbook?.Blueprint) || evt.AbilityData?.SourceItem != null) return;
            if (evt.Initiator.Descriptor.Buffs.Enumerable.Any(b => CenterBuffs.Contains(b.Blueprint.AssetGuid) && !b.IsSuppressed && b.IsTurnedOn)) return;
            evt.AddBonusConcentration(-10);
        }

        internal static bool CheckPolymorph(UnitBody body, AbilityData spell)
            => body.IsPolymorphed && !IsPsychic(spell);

        internal static bool CheckCastingCondition(UnitState state, UnitCondition condition, AbilityData spell)
        {
            if (!state.HasCondition(condition)) return false;
            if (condition != UnitCondition.SpellcastingForbidden || !IsPsychic(spell)) return true;
            int silenceCount = 0;
            foreach (var buff in spell.Caster.Buffs) {
                if (!buff.IsTurnedOn || buff.IsSuppressed || !SilenceIds.Contains(buff.Blueprint.AssetGuid)) continue;
                foreach (var component in buff.Blueprint.ComponentsArray) {
                    if (component is AddCondition add && add.Condition == condition) silenceCount++;
                    if (component is ForbidSpellCasting forbid && (forbid.IgnoreFeature == null || !spell.Caster.HasFact(forbid.IgnoreFeature))) silenceCount++;
                }
            }
            // A silence exception must account for every active source; rage and other
            // anti-casting effects still block the psychic spell. Never change the unit's flags.
            var counts = state.ConditionsConverter;
            return silenceCount == 0 || counts == null || (int)condition >= counts.Length ||
                counts[(int)condition] != silenceCount;
        }

        [HarmonyPatch(typeof(AbilityData), "get_IsAvailableForCast")]
        private static class EmotionPatch
        {
            [HarmonyPostfix] private static void Postfix(AbilityData __instance, ref bool __result)
            { if (__result && IsEmotionBlocked(__instance)) __result = false; }
        }

        [HarmonyPatch(typeof(RuleCalculateAbilityParams), nameof(RuleCalculateAbilityParams.OnTrigger))]
        private static class ThoughtPatch
        {
            [HarmonyPrefix] private static void Prefix(RuleCalculateAbilityParams __instance) => ApplyThoughtPenalty(__instance);
        }

        [HarmonyPatch(typeof(AbilityData), "CalcSpellSource")]
        private static class SourcePatch
        {
            [HarmonyPostfix] private static void Postfix(AbilityData __instance, ref SpellSource __result)
            { if (IsPsychic(__instance)) __result = SpellSource.None; }
        }

        [HarmonyPatch(typeof(AbilityData), "get_CanBeCastByCaster")]
        private static class ComponentsPatch
        {
            [HarmonyTranspiler]
            private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                var codes = instructions.ToList();
                var condition = AccessTools.Method(typeof(UnitState), nameof(UnitState.HasCondition));
                var polymorph = AccessTools.PropertyGetter(typeof(UnitBody), nameof(UnitBody.IsPolymorphed));
                int conditions = 0, bodies = 0;
                for (int i = 0; i < codes.Count; i++) {
                    var code = codes[i];
                    if (code.Calls(condition) && i > 0 && codes[i-1].opcode == OpCodes.Ldc_I4_S &&
                        Convert.ToInt32(codes[i-1].operand) == (int)UnitCondition.SpellcastingForbidden) {
                        var load = new CodeInstruction(OpCodes.Ldarg_0); load.labels.AddRange(code.labels); code.labels.Clear();
                        codes.Insert(i++, load); code.opcode = OpCodes.Call;
                        code.operand = AccessTools.Method(typeof(PsychicCasting), nameof(CheckCastingCondition)); conditions++;
                    } else if (code.Calls(polymorph)) {
                        var load = new CodeInstruction(OpCodes.Ldarg_0); load.labels.AddRange(code.labels); code.labels.Clear();
                        codes.Insert(i++, load); code.opcode = OpCodes.Call;
                        code.operand = AccessTools.Method(typeof(PsychicCasting), nameof(CheckPolymorph)); bodies++;
                    }
                }
                if (conditions != 1 || bodies != 1)
                    throw new InvalidOperationException("Psychic casting: native component-check layout changed. Expected one casting-condition check and one polymorph check.");
                return codes;
            }
        }

        [HarmonyPatch(typeof(RuleCollectMetamagic), nameof(RuleCollectMetamagic.AddMetamagic))]
        private static class LogicalSelectionPatch
        {
            [HarmonyPostfix] private static void Postfix(RuleCollectMetamagic __instance, Feature metamagicFeature)
            {
                if (metamagicFeature.GetComponent<AddMetamagicFeat>()?.Metamagic != Logical || __instance.Spell == null ||
                    !IsPsychic(__instance.Spellbook.Blueprint)) return;
                int level = (int)AccessTools.Field(typeof(RuleCollectMetamagic), "m_SpellLevel").GetValue(__instance);
                int maximum = __instance.Spellbook.Blueprint.GetComponent<PsychicSpellbookMarker>().MaximumSpellLevel;
                if (level >= 0 && level + 1 <= maximum && !__instance.SpellMetamagics.Contains(metamagicFeature))
                    __instance.SpellMetamagics.Add(metamagicFeature);
            }
        }

        [HarmonyPatch(typeof(MetamagicHelper), nameof(MetamagicHelper.DefaultCost))]
        private static class LogicalCostPatch
        {
            [HarmonyPrefix] private static bool Prefix(Metamagic metamagic, ref int __result)
            { if (metamagic != Logical) return true; __result = 1; return false; }
        }
        [HarmonyPatch(typeof(MetamagicHelper), nameof(MetamagicHelper.SpellIcon))]
        private static class LogicalIconPatch
        {
            [HarmonyPrefix] private static void Prefix(ref Metamagic metamagic)
            { if (metamagic == Logical) metamagic = Metamagic.Extend; }
        }
        [HarmonyPatch(typeof(UIUtilityTexts), nameof(UIUtilityTexts.GetMetamagicName))]
        private static class LogicalNamePatch
        {
            [HarmonyPrefix] private static bool Prefix(Metamagic metamagic, ref string __result)
            { if (metamagic == TopplingSpell.Metamagic) { __result = "Toppling"; return false; } if (metamagic != Logical) return true; __result = "Logical"; return false; }
        }
        [HarmonyPatch(typeof(UIUtilityTexts), nameof(UIUtilityTexts.GetMetamagicList))]
        private static class LogicalListPatch
        {
            [HarmonyPostfix] private static void Postfix(Metamagic mask, ref string __result)
            { if ((mask & Logical) != 0) __result = string.IsNullOrEmpty(__result) ? "Logical" : __result + ", Logical"; if ((mask & TopplingSpell.Metamagic) != 0) __result = string.IsNullOrEmpty(__result) ? "Toppling" : __result + ", Toppling"; }
        }
        // Keep existing compiler-generated casting predicate identities stable.
        private static readonly BlueprintGuid DoomAuraHostId = PsychicAssets.Id("AuraOfDoomBuff");
    }
}

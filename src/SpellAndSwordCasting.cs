using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using HarmonyLib;
using Kingmaker.PubSubSystem;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes.Prerequisites;
using Kingmaker.Blueprints.Classes.Selection;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Enums.Damage;
using Kingmaker.RuleSystem;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.RuleSystem.Rules.Damage;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Class.LevelUp;
using Kingmaker.UnitLogic.FactLogic;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Parts;

namespace CallOfTheWild.Wrath.Prototype
{
    [TypeId("e990930619844909a4522a65b0c1e98c")]
    public sealed class SpontaneousMetafocusPrerequisite : Prerequisite, IParamPrerequisite
    {
        protected override bool CheckInternal(FeatureSelectionState selectionState, UnitDescriptor unit, LevelUpState state)
        {
            var known = SpontaneousMetafocus.Known(unit);
            // Parent feat availability queries Items before opening the parameter
            // list. Include late-provider known spells in this owned catalogue too.
            SpontaneousMetafocus.EnsureParameters(OwnerBlueprint as BlueprintParametrizedFeature, known);
            return unit?.Unit != null && unit.Unit.Facts.List.Any(f => T11TrainingOptions.Active(unit.Unit, f) && f.GetComponent<AddMetamagicFeat>() != null) && known.Count != 0;
        }
        public bool CanBeSelected(BlueprintParametrizedFeature feature, UnitDescriptor unit, FeatureParam param) =>
            param?.Blueprint is BlueprintAbility spell && SpontaneousMetafocus.Known(unit).Contains(SpontaneousMetafocus.Root(spell));
        protected override string GetUITextInternal(UnitDescriptor unit) => "A metamagic feat and at least one known spell in a spontaneous spellbook";
    }
    internal static class SpontaneousMetafocus
    {
        internal static BlueprintAbility Root(BlueprintAbility spell)
        {
            var seen = new HashSet<BlueprintAbility>();
            while (spell?.Parent != null && seen.Add(spell)) spell = spell.Parent;
            return spell;
        }
        internal static List<BlueprintAbility> Known(UnitDescriptor unit) => unit == null ? new List<BlueprintAbility>() : unit.Spellbooks
            .Where(b => b?.Blueprint != null && b.Blueprint.Spontaneous && !b.Blueprint.IsArcanist).SelectMany(b => b.GetAllKnownSpells())
            .Where(a => a != null && !a.IsTemporary && a.MetamagicData == null).Select(a => Root(a.Blueprint)).Where(a => a != null).Distinct().ToList();
        internal static void EnsureParameters(BlueprintParametrizedFeature feature, IEnumerable<BlueprintAbility> known)
        {
            if (feature == null) return;
            var variants = feature.BlueprintParameterVariants ?? Array.Empty<AnyBlueprintReference>();
            var ids = new HashSet<BlueprintGuid>(variants.Where(v => v != null).Select(v => v.Guid));
            var added = known.Where(a => a != null && ids.Add(a.AssetGuid)).Select(PsychicAssets.Ref<AnyBlueprintReference>).ToArray();
            if (added.Length == 0) return;
            feature.BlueprintParameterVariants = variants.Concat(added).ToArray();
            PsychicAssets.Set(feature, "m_CachedItems", null);
        }
        internal static bool Selected(AbilityData ability)
        {
            if (ability?.Caster == null || ability.SourceItem != null || !ability.IsSpontaneous || ability.IsArcanist || ability.Spellbook == null || ability.MetamagicData?.NotEmpty != true) return false;
            var spell = Root(ability.Blueprint);
            return ability.Caster.Unit.Facts.List.OfType<Feature>().Any(f => f.Blueprint.AssetGuid == SpellAndSwordFeats.Ids[1] && T11TrainingOptions.Active(ability.Caster.Unit, f) && f.Param.Blueprint == spell);
        }
    }
    [HarmonyPatch(typeof(BlueprintParametrizedFeature), nameof(BlueprintParametrizedFeature.ExtractSpellsFromSpellbooks))]
    internal static class SpontaneousMetafocusSelectionPatch
    {
        [HarmonyPrefix] static bool Prefix(BlueprintParametrizedFeature __instance, UnitDescriptor __0, ref List<BlueprintAbility> __result)
        {
            if (__instance.AssetGuid != SpellAndSwordFeats.Ids[1]) return true;
            __result = SpontaneousMetafocus.Known(__0);
            SpontaneousMetafocus.EnsureParameters(__instance, __result);
            return false;
        }
    }
    [HarmonyPatch(typeof(AbilityData), nameof(AbilityData.RequireFullRoundAction), MethodType.Getter)]
    internal static class SpontaneousMetafocusTimingPatch
    {
        static readonly System.Reflection.MethodInfo DefaultAction = AccessTools.Method(typeof(AbilityData), "GetDefaultActionType");
        [HarmonyPostfix] static void Postfix(AbilityData __instance, ref bool __result)
        {
            if (__result && SpontaneousMetafocus.Selected(__instance) && !__instance.Blueprint.IsFullRoundAction &&
                !(__instance.Caster.Unit.Get<UnitPartAbilityModifiers>()?.RequireFullRound(__instance.Blueprint, (Kingmaker.UnitLogic.Commands.Base.UnitCommand.CommandType)DefaultAction.Invoke(__instance, null)) ?? false)) __result = false;
        }
    }

    internal static class TopplingSpell
    {
        // Audited against native and current mod masks; Logical retains bit 29.
        internal const Metamagic Metamagic = (Metamagic)(1 << 28);
        internal static bool Force(BlueprintAbility spell) => spell != null && spell.GetComponents<SpellDescriptorComponent>().Any(c => (c.Descriptor & SpellDescriptor.Force) != 0);
        [HarmonyPatch(typeof(RuleCollectMetamagic), nameof(RuleCollectMetamagic.AddMetamagic))]
        internal static class SelectionPatch
        {
            [HarmonyPostfix] static void Postfix(RuleCollectMetamagic __instance, Feature metamagicFeature)
            {
                if (metamagicFeature.GetComponent<AddMetamagicFeat>()?.Metamagic != Metamagic || __instance.Spell?.Type != AbilityType.Spell || !Force(__instance.Spell)) return;
                int level = (int)AccessTools.Field(typeof(RuleCollectMetamagic), "m_SpellLevel").GetValue(__instance);
                if (level >= 0 && level < 9 && !__instance.SpellMetamagics.Contains(metamagicFeature)) __instance.SpellMetamagics.Add(metamagicFeature);
            }
        }
        [HarmonyPatch(typeof(MetamagicHelper), nameof(MetamagicHelper.DefaultCost))]
        internal static class CostPatch
        {
            [HarmonyPrefix] static bool Prefix(Metamagic metamagic, ref int __result) { if (metamagic != Metamagic) return true; __result = 1; return false; }
        }
        [HarmonyPatch(typeof(MetamagicHelper), nameof(MetamagicHelper.SpellIcon))]
        internal static class IconPatch
        {
            [HarmonyPrefix] static void Prefix(ref Metamagic metamagic) { if (metamagic == Metamagic) metamagic = Kingmaker.UnitLogic.Abilities.Metamagic.Extend; }
        }
    }
    [TypeId("cd24d97a85cc4b6faecce9bd6c66e880")]
    public sealed class TopplingSpellDamage : UnitFactComponentDelegate, IInitiatorRulebookHandler<RuleDealDamage>
    {
        static readonly ConditionalWeakTable<MechanicsContext, HashSet<string>> Attempts = new ConditionalWeakTable<MechanicsContext, HashSet<string>>();
        public void OnEventAboutToTrigger(RuleDealDamage evt) { }
        public void OnEventDidTrigger(RuleDealDamage evt)
        {
            var context = evt.Reason?.Context; var root = context?.SourceAbilityContext;
            var spell = root?.Ability?.Blueprint ?? context?.SourceAbility;
            if (!Fact.IsActive || !Fact.IsTurnedOn || Fact.IsDisposed || evt.IsFake || evt.Initiator != Owner || evt.Target == null || evt.Target.State.IsDead || evt.Result <= 0 || context?.Params == null ||
                !context.HasMetamagic(TopplingSpell.Metamagic) || spell?.Type != AbilityType.Spell || !TopplingSpell.Force(spell)) return;
            var key = (MechanicsContext)root ?? context;
            var book = root?.Ability?.Spellbook;
            if (book == null) return;
            if (!Attempts.GetValue(key, _ => new HashSet<string>()).Add(evt.Target.UniqueId)) return;
            var stat = book.Blueprint.CastingAttribute;
            context.TriggerRule(new RuleCombatManeuver(Owner, evt.Target, CombatManeuver.Trip) { Reason = new RuleReason(context), ReplaceBaseStat = stat, ReplaceAttackBonus = context.Params.CasterLevel, IgnoreConcealment = true });
        }
    }

    [TypeId("4796f4179d334abca57a9f4b61d2c559")]
    public sealed class FeyFoundlingFirstLevel : Prerequisite
    {
        protected override bool CheckInternal(FeatureSelectionState selectionState, UnitDescriptor unit, LevelUpState state) => unit?.Progression.CharacterLevel == 1;
        protected override string GetUITextInternal(UnitDescriptor unit) => "Character level 1 only";
    }
    [TypeId("2122c54c39e04bdd9270f6bf8fc0b677")]
    public sealed class FeyFoundlingColdIron : UnitFactComponentDelegate, ITargetRulebookHandler<RulePrepareDamage>
    {
        static readonly ConditionalWeakTable<RulePrepareDamage, object> Applied = new ConditionalWeakTable<RulePrepareDamage, object>();
        public void OnEventAboutToTrigger(RulePrepareDamage evt)
        {
            if (!Fact.IsActive || !Fact.IsTurnedOn || Fact.IsDisposed || evt.Target != Owner || evt.DamageBundle.Weapon == null) return;
            var physical = evt.DamageBundle.OfType<PhysicalDamage>().FirstOrDefault(p => (p.MaterialsMask & PhysicalDamageMaterial.ColdIron) != 0);
            if (physical != null && !Applied.TryGetValue(evt, out _)) { Applied.Add(evt, new object()); physical.PostCritIncrements.AddBonusModifier(1, Fact); }
        }
        public void OnEventDidTrigger(RulePrepareDamage evt) { }
    }
    [HarmonyPatch(typeof(RuleHealDamage), nameof(RuleHealDamage.OnTrigger))]
    internal static class FeyFoundlingHealingPatch
    {
        // Add the recipient's flat per-die benefit after Empower, before native
        // healing resistance and HP application. Retain all native reductions.
        internal static float Add(float amount, RuleHealDamage rule)
        {
            var source = rule.Reason?.Context?.SourceAbilityContext?.Ability?.Blueprint ?? rule.Reason?.Context?.SourceAbility;
            if (rule.IsFake || rule.Interrupted || !SpellAndSwordFeats.Has(rule.Target, 4) || source == null ||
                source.Type != AbilityType.Spell && source.Type != AbilityType.SpellLike && source.Type != AbilityType.Supernatural) return amount;
            Kingmaker.RuleSystem.DiceFormula dice = rule.HealFormula.ModifiedValue;
            return dice.Dice == DiceType.Zero ? amount : amount + 2 * dice.Rolls;
        }
        [HarmonyTranspiler] static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var codes = instructions.ToList(); int matches = 0;
            for (int i = 2; i + 2 < codes.Count; i++)
                if (codes[i].opcode == OpCodes.Mul && codes[i - 1].opcode == OpCodes.Ldloc_0 && codes[i - 2].opcode == OpCodes.Conv_R4 && codes[i + 1].opcode == OpCodes.Ldloc_3 && codes[i + 2].opcode == OpCodes.Mul) {
                    codes.InsertRange(i + 1, new[] { new CodeInstruction(OpCodes.Ldarg_0), new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(FeyFoundlingHealingPatch), nameof(Add))) }); matches++; i += 2;
                }
            if (matches != 1) throw new InvalidOperationException("Fey Foundling: native healing amount/Empower/resistance layout changed.");
            return codes;
        }
    }
}

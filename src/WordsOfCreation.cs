using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Prerequisites;
using Kingmaker.Blueprints.Classes.Selection;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.Blueprints.Facts;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.Designers;
using Kingmaker.EntitySystem;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Enums;
using Kingmaker.PubSubSystem;
using Kingmaker.RuleSystem;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.RuleSystem.Rules.Abilities;
using Kingmaker.RuleSystem.Rules.Damage;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components;
using Kingmaker.UnitLogic.Abilities.Components.AreaEffects;
using Kingmaker.UnitLogic.ActivatableAbilities;
using Kingmaker.UnitLogic.Alignments;
using Kingmaker.UnitLogic.Buffs;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Commands.Base;
using Kingmaker.UnitLogic.FactLogic;
using Kingmaker.UnitLogic.Mechanics;
using static CallOfTheWild.Wrath.Prototype.PsychicAssets;

namespace CallOfTheWild.Wrath.Prototype
{
    internal static class WordsOfCreation
    {
        internal const string ModuleName = "Words of Creation";
        internal const string FeatToken = "WordsOfCreationFeat";
        internal const string ToggleToken = "WordsOfCreationToggle";
        internal const string BuffToken = "WordsOfCreationBuff";

        private const string FeeblemindBuffId = "8b3b4c225fe0fb046bfa8881c3ddad0d";
        private const string InspireCourageFeatureId = "ac68b4479221d6d4793691d464897074";
        private static readonly BlueprintGuid BardPool = BlueprintGuid.Parse(SublimeChord.BardicPerformanceResourceId);
        private static readonly BlueprintGuid SkaldPool = BlueprintGuid.Parse(SublimeChord.RagingSongResourceId);

        private const string DescriptionText =
            "创生圣言是一种失落语言的遗存片段，被认为是天界语的前身。你可以用创生圣言来加强诗人的音乐，并帮助施放善良法术。\n" +
            "先决条件：智力 15，魅力 15，基础意志豁免 +5，善良阵营。\n" +
            "天界咏唱：当此能力激活时，你的吟游诗人与歌者表演对盟友提供的数值加值翻倍，对邪恶阵营敌人施加的数值减值与造成的伤害翻倍，且邪恶阵营敌人对抗表演效果的豁免 DC +4。每次在激活此能力的情况下启动表演能力，都会受到等同于该表演计算效果的职业等级 d4 点伤害（此伤害不会使自身生命低于 1）。\n" +
            "崇高之力：当此能力激活时，你施放的善良法术的施法者等级 +1，但会对施法者造成每 2 法术环级 1d4 点的伤害（0 环或 1 环法术不受伤害，至 9 环法术为 4d4 点；此伤害不会使自身生命低于 1）。\n" +
            "限制：若在阵营不再为善良时激活此能力并使用表演或施放善良法术，非邪恶者将立即受到弱智术影响，邪恶者则会立刻死亡。";

        private static BlueprintBuff WordsBuff;
        private static BlueprintActivatableAbility WordsToggle;
        private static BlueprintBuff FeeblemindBuff;
        private static int ActiveHolderCount;
        private static readonly object GraphGate = new object();
        private static readonly HashSet<BlueprintGuid> PerformanceRoots = new HashSet<BlueprintGuid>();
        private static readonly HashSet<BlueprintGuid> PerformanceBuffs = new HashSet<BlueprintGuid>();
        private static readonly HashSet<BlueprintGuid> PerformanceAreas = new HashSet<BlueprintGuid>();

        [ThreadStatic]
        private static bool InModifierHook;

        internal static void TrackActive(bool active)
        {
            if (active) System.Threading.Interlocked.Increment(ref ActiveHolderCount);
            else System.Threading.Interlocked.Decrement(ref ActiveHolderCount);
        }

        internal static void Register(BlueprintsCache cache)
        {
            var pool = cache.Load(BlueprintGuid.Parse(BlueprintRegistration.BasicFeatsId)) as BlueprintFeatureSelection
                ?? throw new InvalidOperationException("Words of Creation basic feat pool missing.");

            var f = new PsychicAssets(cache);
            FeeblemindBuff = cache.Load(BlueprintGuid.Parse(FeeblemindBuffId)) as BlueprintBuff;
            var iconSource = cache.Load(BlueprintGuid.Parse(InspireCourageFeatureId)) as BlueprintUnitFact;

            var buff = f.Buff(BuffToken, "创生圣言", DescriptionText, new WordsOfCreationComponent());
            buff.Stacking = StackingType.Replace;
            if (iconSource?.Icon != null) Set(buff, "m_Icon", iconSource.Icon);
            WordsBuff = buff;

            var toggle = f.Add(new BlueprintActivatableAbility
            {
                ActivationType = AbilityActivationType.Immediately,
                DeactivateImmediately = true,
                DoNotTurnOffOnRest = true,
                DeactivateIfCombatEnded = false,
                Group = ActivatableAbilityGroup.None,
                ComponentsArray = Array.Empty<BlueprintComponent>()
            }, ToggleToken, "创生圣言", DescriptionText);
            Set(toggle, "m_Buff", Ref<BlueprintBuffReference>(buff));
            Set(toggle, "m_ActivateWithUnitCommand", UnitCommand.CommandType.Free);
            if (iconSource?.Icon != null) Set(toggle, "m_Icon", iconSource.Icon);
            WordsToggle = toggle;

            var feat = f.Feature(FeatToken, "创生圣言", DescriptionText,
                new PrerequisiteStatValue { Stat = StatType.Intelligence, Value = 15 },
                new PrerequisiteStatValue { Stat = StatType.Charisma, Value = 15 },
                new PrerequisiteBaseWillSave { Required = 5 },
                new PrerequisiteAlignment { Alignment = AlignmentMaskType.Good },
                Facts(toggle));
            feat.Groups = new[] { FeatureGroup.Feat };
            if (iconSource?.Icon != null) Set(feat, "m_Icon", iconSource.Icon);

            PsychicRepairComponents.UniqueNames(buff);
            PsychicRepairComponents.UniqueNames(toggle);
            PsychicRepairComponents.UniqueNames(feat);

            var existing = cache.Load(feat.AssetGuid);
            if (existing == null)
            {
                f.Enable();
                foreach (var bp in f.Assets.Values)
                    cache.AddCachedBlueprint(bp.AssetGuid, bp);
            }
            var actual = cache.Load(feat.AssetGuid) as BlueprintFeature ?? feat;
            WordsBuff = cache.Load(buff.AssetGuid) as BlueprintBuff ?? buff;
            WordsToggle = cache.Load(toggle.AssetGuid) as BlueprintActivatableAbility ?? toggle;
            T09CombatFeats.Append(new[] { pool }, new[] { actual });

            RefreshPerformanceGraph(cache);
        }

        // Built exactly once, at registration. The graph is only a fast path:
        // IsPerformanceBlueprint() additionally checks the blueprint directly
        // and IsPerformanceChain() walks the live MechanicsContext parent chain.
        // Nothing in the rule/UI read path ever rebuilds it, which is what used
        // to cause a full cache snapshot + BFS the first time a performance was
        // started with Words of Creation active.
        private static void RefreshPerformanceGraph(BlueprintsCache cache)
        {
            if (cache == null) return;
            lock (GraphGate)
            {
                var snapshot = SublimeChord.Snapshot(cache);
                var byGuid = new Dictionary<BlueprintGuid, SimpleBlueprint>(snapshot.Length);
                var roots = new List<SimpleBlueprint>();
                foreach (var bp in snapshot)
                {
                    if (bp == null) continue;
                    byGuid[bp.AssetGuid] = bp;
                    if ((bp is BlueprintAbility || bp is BlueprintActivatableAbility) && UsesPerformancePool(bp))
                        roots.Add(bp);
                }

                var newRoots = new HashSet<BlueprintGuid>();
                var newBuffs = new HashSet<BlueprintGuid>();
                var newAreas = new HashSet<BlueprintGuid>();
                var visited = new HashSet<BlueprintGuid>();
                var frontier = new List<SimpleBlueprint>();

                foreach (var r in roots)
                {
                    newRoots.Add(r.AssetGuid);
                    if (visited.Add(r.AssetGuid)) frontier.Add(r);
                }

                for (int depth = 0; depth < 8 && frontier.Count > 0 && visited.Count < 8192; depth++)
                {
                    var next = new List<SimpleBlueprint>();
                    foreach (var bp in frontier)
                    {
                        if (bp is BlueprintBuff) newBuffs.Add(bp.AssetGuid);
                        else if (bp is BlueprintAbilityAreaEffect) newAreas.Add(bp.AssetGuid);
                        else if (bp is BlueprintAbility) newRoots.Add(bp.AssetGuid);

                        foreach (var guid in CollectReferences(bp))
                        {
                            if (!visited.Add(guid)) continue;
                            if (byGuid.TryGetValue(guid, out var target) && target != null)
                                next.Add(target);
                        }
                    }
                    frontier = next;
                }

                PerformanceRoots.Clear();
                foreach (var g in newRoots) PerformanceRoots.Add(g);
                PerformanceBuffs.Clear();
                foreach (var g in newBuffs) PerformanceBuffs.Add(g);
                PerformanceAreas.Clear();
                foreach (var g in newAreas) PerformanceAreas.Add(g);
            }
        }

        private static bool UsesPerformancePool(SimpleBlueprint bp)
        {
            var comps = (bp as BlueprintScriptableObject)?.ComponentsArray;
            if (comps == null) return false;
            foreach (var c in comps)
            {
                if (c is AbilityResourceLogic al)
                {
                    var res = Get(al, "m_RequiredResource") as BlueprintAbilityResourceReference;
                    if (res != null && (res.Guid == BardPool || res.Guid == SkaldPool)) return true;
                }
                else if (c is ActivatableAbilityResourceLogic tl)
                {
                    var res = Get(tl, "m_RequiredResource") as BlueprintAbilityResourceReference;
                    if (res != null && (res.Guid == BardPool || res.Guid == SkaldPool)) return true;
                }
            }
            return false;
        }

        private static IEnumerable<BlueprintGuid> CollectReferences(SimpleBlueprint root)
        {
            var result = new List<BlueprintGuid>();
            var visited = new HashSet<object>(IdentityComparer) { root };
            ScanReferences(root, result, visited, 0);
            return result;
        }

        private static void ScanReferences(object node, List<BlueprintGuid> found, HashSet<object> visited, int depth)
        {
            if (node == null || depth > 7 || visited.Count > 6000 || found.Count > 4096) return;
            for (var type = node.GetType(); type != null && type != typeof(object); type = type.BaseType)
            {
                foreach (var field in type.GetFields(System.Reflection.BindingFlags.Instance |
                    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic |
                    System.Reflection.BindingFlags.DeclaredOnly))
                {
                    if (field.IsStatic || field.FieldType.IsPrimitive || field.FieldType.IsEnum) continue;
                    object value;
                    try { value = field.GetValue(node); } catch { continue; }
                    if (value == null || value is string || value is SimpleBlueprint || value is UnityEngine.Object || value is Type || value is Delegate) continue;
                    if (value is BlueprintReferenceBase single)
                    {
                        if (single.Guid != BlueprintGuid.Empty) found.Add(single.Guid);
                        continue;
                    }
                    if (value is System.Collections.IEnumerable seq)
                    {
                        int taken = 0;
                        foreach (var item in seq)
                        {
                            if (taken++ > 512 || found.Count > 4096) break;
                            if (item is BlueprintReferenceBase r)
                            {
                                if (r.Guid != BlueprintGuid.Empty) found.Add(r.Guid);
                                continue;
                            }
                            if (item != null && !item.GetType().IsPrimitive && !item.GetType().IsEnum &&
                                !(item is string) && !(item is SimpleBlueprint) && !(item is UnityEngine.Object) &&
                                !(item is Type) && !(item is Delegate) && visited.Add(item))
                                ScanReferences(item, found, visited, depth + 1);
                        }
                    }
                    else if (!value.GetType().IsPrimitive && !value.GetType().IsEnum && visited.Add(value))
                    {
                        ScanReferences(value, found, visited, depth + 1);
                    }
                }
            }
        }

        internal static bool IsActive(UnitDescriptor unit)
        {
            if (unit == null || WordsBuff == null) return false;
            return unit.Buffs.HasFact(WordsBuff);
        }

        internal static bool IsGood(UnitDescriptor unit)
            => unit?.Alignment?.ValueRaw.HasComponent(AlignmentComponent.Good) == true;

        internal static bool IsEvil(UnitDescriptor unit)
            => unit?.Alignment?.ValueRaw.HasComponent(AlignmentComponent.Evil) == true;

        // Cheap: a GUID lookup plus a component scan of the single blueprint.
        // No snapshot, no BFS, no reflection walk of the whole cache.
        internal static bool IsPerformanceBlueprint(BlueprintScriptableObject bp)
        {
            if (bp == null) return false;
            lock (GraphGate)
            {
                if (PerformanceBuffs.Contains(bp.AssetGuid)
                    || PerformanceRoots.Contains(bp.AssetGuid)
                    || PerformanceAreas.Contains(bp.AssetGuid))
                    return true;
            }
            return UsesPerformancePool(bp);
        }

        // A performance's effect buff rarely carries the resource logic itself;
        // its MechanicsContext parent chain still points at the ability/toggle
        // that spent the Bardic Performance / Raging Song pool. Walking that
        // chain covers blueprints that were only TOC entries at registration,
        // without maintaining a global closure of every loaded blueprint.
        private static bool IsPerformanceChain(MechanicsContext context)
        {
            for (int depth = 0; context != null && depth < 8; depth++, context = context.ParentContext)
            {
                if (context.SourceAbility is BlueprintAbility ability && IsPerformanceBlueprint(ability)) return true;
                if (context.AssociatedBlueprint is BlueprintScriptableObject assoc && IsPerformanceBlueprint(assoc)) return true;
            }
            return false;
        }

        internal static bool IsPerformanceFact(EntityFact fact, out UnitEntityData caster)
        {
            caster = fact?.MaybeContext?.MaybeCaster;
            if (caster == null || fact?.Blueprint == null) return false;
            if (!IsActive(caster.Descriptor) || !IsGood(caster.Descriptor)) return false;
            return IsPerformanceBlueprint(fact.Blueprint) || IsPerformanceChain(fact.MaybeContext);
        }

        internal static bool IsPerformanceContext(MechanicsContext context, BlueprintAbility sourceAbility, out UnitEntityData caster)
        {
            caster = context?.MaybeCaster;
            if (caster == null) return false;
            if (!IsActive(caster.Descriptor) || !IsGood(caster.Descriptor)) return false;
            if (sourceAbility != null && IsPerformanceBlueprint(sourceAbility)) return true;
            return IsPerformanceChain(context);
        }

        internal static int EffectivePerformanceLevel(UnitDescriptor unit)
        {
            if (unit == null) return 0;
            int level = 0;
            var bard = unit.Progression.Classes.FirstOrDefault(c => c.CharacterClass?.AssetGuid.ToString() == SublimeChord.BardClassId);
            if (bard != null) level += bard.Level;
            var skald = unit.Progression.Classes.FirstOrDefault(c => c.CharacterClass?.AssetGuid.ToString() == SublimeChord.SkaldClassId);
            if (skald != null) level += skald.Level;
            level += SublimeChordPerformances.BonusLevels(unit);
            return Math.Max(1, level);
        }

        internal static void EnforceAlignmentPenaltyOrDamage(UnitEntityData caster, int d4Count)
        {
            if (caster == null || caster.Descriptor == null || caster.Descriptor.State.IsDead) return;
            var desc = caster.Descriptor;
            if (IsEvil(desc))
            {
                GameHelper.KillUnit(caster);
                return;
            }
            if (!IsGood(desc))
            {
                var fm = FeeblemindBuff ?? ResourcesLibrary.TryGetBlueprint<BlueprintBuff>(FeeblemindBuffId);
                if (fm != null)
                {
                    var applied = desc.AddBuff(fm, caster, null);
                    applied?.MakePermanent();
                }
                return;
            }
            if (d4Count <= 0) return;
            // Leave the dice formula on the damage packet. RuleCalculateDamage
            // rolls it and the combat log's AppendDamageDetails() renders
            // "Nd4" only when the dice are non-zero. Pre-rolling and folding the
            // result into a fixed bonus (DiceFormula.Zero + actual) is exactly
            // what made the log show a bare number with no dice breakdown.
            int maxDeal = Math.Max(0, caster.HPLeft - 1);
            if (maxDeal <= 0) return;

            var dmg = new DirectDamage(new DiceFormula(d4Count, DiceType.D4), 0) { IgnoreReduction = true };
            var rule = new RuleDealDamage(caster, caster, new DamageBundle(dmg))
            {
                MinHPAfterDamage = 1,
                IgnoreGrowthByDifficulty = true
            };
            Rulebook.Trigger(rule);
        }

        internal static bool HasGoodDescriptor(BlueprintAbility spell, AbilityData data)
        {
            if (spell == null) return false;
            var desc = spell.GetComponents<SpellDescriptorComponent>()
                .Aggregate(SpellDescriptor.None, (acc, c) => acc | (SpellDescriptor)c.Descriptor);
            if ((desc & SpellDescriptor.Good) != 0) return true;
            if (data != null && (data.Blueprint?.SpellDescriptor & SpellDescriptor.Good) != 0) return true;
            return false;
        }

        [HarmonyPatch(typeof(ActivatableAbility), "OnDidTurnOn")]
        private static class PerformanceStartedPatch
        {
            private static void Postfix(ActivatableAbility __instance)
            {
                if (System.Threading.Volatile.Read(ref ActiveHolderCount) <= 0) return;
                if (__instance == null || !__instance.IsOn) return;
                if (!SublimeChordPerformances.IsPerformanceToggle(__instance.Blueprint)) return;
                var caster = __instance.Owner?.Unit;
                if (caster == null || !IsActive(caster.Descriptor)) return;
                EnforceAlignmentPenaltyOrDamage(caster, EffectivePerformanceLevel(caster.Descriptor));
            }
        }

        [HarmonyPatch(typeof(ModifiableValue), nameof(ModifiableValue.AddModifier), new[] { typeof(ModifiableValue.Modifier) })]
        private static class StatModifierDoublePatch
        {
            private static void Prefix(ModifiableValue __instance, ModifiableValue.Modifier mod)
            {
                if (System.Threading.Volatile.Read(ref ActiveHolderCount) <= 0) return;
                if (InModifierHook || __instance == null || mod == null || mod.ModValue == 0) return;
                var target = __instance.Owner?.Unit;
                if (target == null) return;
                if (!IsPerformanceFact(mod.Source, out var caster)) return;

                bool isAlly = !target.IsEnemy(caster);
                bool isEvilEnemy = target.IsEnemy(caster) && IsEvil(target.Descriptor);
                if ((isAlly && mod.ModValue > 0) || (isEvilEnemy && mod.ModValue < 0))
                {
                    InModifierHook = true;
                    try { mod.ModValue *= 2; }
                    finally { InModifierHook = false; }
                }
            }
        }

        [HarmonyPatch(typeof(RuleCalculateWeaponStats), nameof(RuleCalculateWeaponStats.AddDamageModifier))]
        private static class WeaponDamageModifierDoublePatch
        {
            private static void Prefix(RuleCalculateWeaponStats __instance, ref int bonus, EntityFact fact)
            {
                if (System.Threading.Volatile.Read(ref ActiveHolderCount) <= 0) return;
                if (__instance == null || bonus <= 0 || fact == null) return;
                var target = __instance.Initiator;
                if (target == null) return;
                if (!IsPerformanceFact(fact, out var caster)) return;
                if (!target.IsEnemy(caster))
                    bonus *= 2;
            }
        }

        [HarmonyPatch(typeof(RulebookEvent), "AddModifier", new[] { typeof(int), typeof(EntityFact), typeof(ModifierDescriptor) })]
        private static class RulebookModifierDoublePatch
        {
            private static void Prefix(RulebookEvent __instance, ref int bonus, EntityFact source)
            {
                if (System.Threading.Volatile.Read(ref ActiveHolderCount) <= 0) return;
                if (__instance == null || bonus == 0 || source == null) return;
                var target = __instance.Initiator;
                if (target == null) return;
                if (!IsPerformanceFact(source, out var caster)) return;

                bool isAlly = !target.IsEnemy(caster);
                bool isEvilEnemy = target.IsEnemy(caster) && IsEvil(target.Descriptor);
                if ((isAlly && bonus > 0) || (isEvilEnemy && bonus < 0))
                    bonus *= 2;
            }
        }

        [HarmonyPatch(typeof(RuleSavingThrow), nameof(RuleSavingThrow.OnTrigger))]
        private static class PerformanceSaveDcPatch
        {
            private static void Prefix(RuleSavingThrow __instance)
            {
                if (System.Threading.Volatile.Read(ref ActiveHolderCount) <= 0) return;
                if (__instance == null || __instance.Initiator == null) return;
                var target = __instance.Initiator;
                if (!IsEvil(target.Descriptor)) return;
                var ctx = __instance.Reason?.Context;
                if (!IsPerformanceContext(ctx, __instance.Reason?.Ability?.Blueprint, out var caster)) return;
                if (!target.IsEnemy(caster)) return;
                __instance.AddBonusDC(4);
            }
        }

        [HarmonyPatch(typeof(RuleDealDamage), nameof(RuleDealDamage.OnTrigger))]
        private static class PerformanceDamageDoublePatch
        {
            private static void Prefix(RuleDealDamage __instance)
            {
                if (System.Threading.Volatile.Read(ref ActiveHolderCount) <= 0) return;
                if (__instance == null || __instance.Target == null || __instance.Initiator == null) return;
                var target = __instance.Target;
                var caster = __instance.Initiator;
                if (!target.IsEnemy(caster) || !IsEvil(target.Descriptor)) return;
                var ctx = __instance.Reason?.Context;
                if (!IsPerformanceContext(ctx, __instance.SourceAbility ?? __instance.Reason?.Ability?.Blueprint, out var perfCaster)) return;
                if (perfCaster != caster) return;

                foreach (var dmg in __instance.DamageBundle)
                {
                    if (dmg == null) continue;
                    dmg.BonusPercent = (100 + dmg.BonusPercent) * 2 - 100;
                }
            }
        }
    }

    [TypeId("d1c00000000000000000000000000012")]
    public sealed class PrerequisiteBaseWillSave : Prerequisite
    {
        public int Required;

        internal static int GetBaseWillSave(UnitDescriptor unit)
        {
            if (unit?.Progression?.Classes == null) return 0;
            int total = 0;
            foreach (var cls in unit.Progression.Classes)
            {
                if (cls == null) continue;
                var prog = cls.WillSave;
                if (prog != null)
                    total += prog.GetBonus(cls.Level);
            }
            return total;
        }

        protected override bool CheckInternal(Kingmaker.UnitLogic.Class.LevelUp.FeatureSelectionState selectionState,
            UnitDescriptor unit, Kingmaker.UnitLogic.Class.LevelUp.LevelUpState state)
            => GetBaseWillSave(unit) >= Required;

        protected override string GetUITextInternal(UnitDescriptor unit)
            => $"基础意志豁免 +{Required}（当前：{GetBaseWillSave(unit)}）";
    }

    [AllowedOn(typeof(BlueprintBuff), false)]
    [TypeId("d1c00000000000000000000000000013")]
    public sealed class WordsOfCreationComponent : UnitFactComponentDelegate,
        IInitiatorRulebookHandler<RuleCalculateAbilityParams>,
        IInitiatorRulebookHandler<RuleCastSpell>
    {
        private bool counted;

        protected override void OnTurnOn()
        {
            if (!counted)
            {
                counted = true;
                WordsOfCreation.TrackActive(true);
            }
        }

        protected override void OnTurnOff()
        {
            if (counted)
            {
                counted = false;
                WordsOfCreation.TrackActive(false);
            }
        }

        public void OnEventAboutToTrigger(RuleCalculateAbilityParams evt)
        {
            if (evt == null || evt.Initiator != Owner || evt.Spell == null) return;
            if (evt.Spell.Type != AbilityType.Spell) return;
            if (!WordsOfCreation.HasGoodDescriptor(evt.Spell, evt.AbilityData)) return;
            if (WordsOfCreation.IsGood(Owner.Descriptor))
                evt.AddBonusCasterLevel(1, ModifierDescriptor.UntypedStackable);
        }

        public void OnEventDidTrigger(RuleCalculateAbilityParams evt) { }

        public void OnEventAboutToTrigger(RuleCastSpell evt) { }

        public void OnEventDidTrigger(RuleCastSpell evt)
        {
            if (evt == null || evt.Initiator != Owner || evt.Spell == null) return;
            var bp = evt.Spell.Blueprint;
            if (bp == null) return;

            if (bp.Type == AbilityType.Spell && WordsOfCreation.HasGoodDescriptor(bp, evt.Spell))
            {
                int dice = Math.Min(4, Math.Max(0, evt.Spell.SpellLevel / 2));
                WordsOfCreation.EnforceAlignmentPenaltyOrDamage(Owner, dice);
                return;
            }

            if (WordsOfCreation.IsPerformanceBlueprint(bp) && evt.Spell.SourceItem == null)
            {
                WordsOfCreation.EnforceAlignmentPenaltyOrDamage(Owner,
                    WordsOfCreation.EffectivePerformanceLevel(Owner.Descriptor));
            }
        }
    }
}

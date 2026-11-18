// Sublime Chord — Bardic Performance scaling (plan B) and the three songs.
// Performances are DISCOVERED, never hard-listed: any ability that spends the
// Bardic Performance or Raging Song resource is a root, and its reachable
// blueprint graph contributes the rank configs that need Sublime Chord levels.
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using HarmonyLib;
using Kingmaker;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.Blueprints.Facts;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.Designers;
using Kingmaker.ElementsSystem;
using Kingmaker.EntitySystem;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Enums;
using Kingmaker.Enums.Damage;
using Kingmaker.PubSubSystem;
using Kingmaker.ResourceLinks;
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
using Kingmaker.UnitLogic.Buffs;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Commands.Base;
using Kingmaker.UnitLogic.FactLogic;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Mechanics.Actions;
using Kingmaker.UnitLogic.Mechanics.Components;
using Kingmaker.Utility;
using static CallOfTheWild.Wrath.Prototype.PsychicAssets;

namespace CallOfTheWild.Wrath.Prototype
{
    internal sealed class SublimeChordFeatures
    {
        internal BlueprintFeature Knowledge, Performance, ArcaneSong, EternalSong, InfiniteFire;
    }

    internal static class SublimeChordPerformances
    {
        private static readonly BlueprintGuid BardPool = BlueprintGuid.Parse(SublimeChord.BardicPerformanceResourceId);
        private static readonly BlueprintGuid SkaldPool = BlueprintGuid.Parse(SublimeChord.RagingSongResourceId);
        private static readonly BlueprintGuid KnowledgeId = BlueprintGuid.Parse(SublimeChord.BardicKnowledgeId);
        private static readonly BlueprintGuid BardClassGuid = BlueprintGuid.Parse(SublimeChord.BardClassId);
        private static readonly BlueprintGuid SkaldClassGuid = BlueprintGuid.Parse(SublimeChord.SkaldClassId);

        private static BlueprintCharacterClass Class;
        private static BlueprintAbility ArcaneAbility, EternalAbility, FireAbility;
        private static BlueprintBuff FreezeBuff;
        // Ranks reached from a performance-spending ability. Reconcile appends
        // the SC class to the m_Class of sum-type ranks it can see; this registry
        // covers the rest at ContextRankConfig.GetBaseValue, including deep-copied
        // archetype effect buffs (e.g. our own Urban Skald) whose ContextRankConfig
        // instances differ from the native ones and were never mutated.
        private static readonly HashSet<ContextRankConfig> PerformanceRanks =
            new HashSet<ContextRankConfig>(Identity<ContextRankConfig>.Instance);
        private static readonly object RankGate = new object();

        private static string T(string token) => SublimeChord.T(token);

        // Shared helper for modules that key a Bard/Skald performance value off
        // GetClassLevel() directly instead of a ContextRankConfig (e.g. the
        // Urban Skald Controlled Inspired Rage allocation budget). Returns 0
        // when Sublime Chord is not registered, so a disabled module and
        // characters without the class are unaffected.
        internal static int BonusLevels(UnitDescriptor owner)
        {
            var cls = Class;
            return cls == null || owner == null ? 0 : owner.Progression.GetClassLevel(cls);
        }

        // A Bard-origin character owns BardicPerformanceResource, a pure Skald
        // owns RagingSongResource, and a multiclass character may own both.
        // The pool must therefore be resolved per caster, not pinned at build
        // time to the Bard resource (which was the original limitation).
        private static AbilityResourceLogic Cost(BlueprintAbilityResource bard, BlueprintAbilityResource skald, int amount)
        {
            var cost = new SublimeChordPerformanceCost {
                Amount = amount,
                BardPool = Ref<BlueprintAbilityResourceReference>(bard),
                SkaldPool = Ref<BlueprintAbilityResourceReference>(skald)
            };
            // These private fields are declared on the native base class; use
            // its exact type so a subclass instance cannot miss them.
            SetResourceLogicField(cost, "m_IsSpendResource", true);
            SetResourceLogicField(cost, "m_RequiredResource", Ref<BlueprintAbilityResourceReference>(bard));
            return cost;
        }

        private static void SetResourceLogicField(AbilityResourceLogic logic, string field, object value)
        {
            var target = AccessTools.Field(typeof(AbilityResourceLogic), field)
                ?? throw new MissingFieldException(typeof(AbilityResourceLogic).FullName, field);
            target.SetValue(logic, value);
        }

        private static ContextRankConfig ClassRank(BlueprintCharacterClass cls)
        {
            var rank = new ContextRankConfig();
            SetEnum(rank, "m_BaseValueType", "ClassLevel");
            Set(rank, "m_Class", new[] { Ref<BlueprintCharacterClassReference>(cls) });
            SetEnum(rank, "m_Progression", "AsIs");
            return rank;
        }

        internal static SublimeChordFeatures Build(PsychicAssets f, BlueprintsCache cache,
            BlueprintCharacterClass cls, BlueprintSpellbook book)
        {
            Class = cls;
            var bardResource = f.Native<BlueprintAbilityResource>(SublimeChord.BardicPerformanceResourceId);
            var skaldResource = f.Native<BlueprintAbilityResource>(SublimeChord.RagingSongResourceId);
            var knowledgeBlueprint = f.Native<BlueprintFeature>(SublimeChord.BardicKnowledgeId);
            var classRef = Ref<BlueprintCharacterClassReference>(cls);
            var bardClass = f.Native<BlueprintCharacterClass>(SublimeChord.BardClassId);
            var skaldClass = f.Native<BlueprintCharacterClass>(SublimeChord.SkaldClassId);

            var knowledge = f.Feature(T("Knowledge"), "逸闻见识",
                "超凡之弦继续收集各种古怪的传闻与知识。她获得吟游诗人的逸闻见识能力；若已拥有该能力，则吟游诗人等级与超凡之弦等级叠加计算效果。",
                Facts(knowledgeBlueprint));
            // Every UnitFactComponentDelegate is a runtime-component provider:
            // EntityFact.Setup dedupes by component.name, so each of these MUST
            // carry a unique name or only the first one would ever activate.
            // Combined Bard/Skald + Sublime Chord level 7 / 13 also grants the
            // native "start a performance as a move/swift action" features.
            BlueprintFeature NativeAction(string id) => f.Native<BlueprintFeature>(id);
            var performance = f.Add(new BlueprintFeature { Ranks = 1, IsClassFeature = true,
                ComponentsArray = new BlueprintComponent[] {
                    new SublimeChordPerformanceRounds { name = "SublimeChordPerformanceRounds", Class = classRef },
                    new SublimeChordPerformanceDC { name = "SublimeChordPerformanceDC", Class = classRef },
                    Grant("SublimeChordBardMove7", bardClass, cls, NativeAction(SublimeChord.BardMovePerformanceId), 7),
                    Grant("SublimeChordBardSwift13", bardClass, cls, NativeAction(SublimeChord.BardSwiftPerformanceId), 13),
                    Grant("SublimeChordSkaldMove7", skaldClass, cls, NativeAction(SublimeChord.SkaldMovePerformanceId), 7),
                    Grant("SublimeChordSkaldSwift13", skaldClass, cls, NativeAction(SublimeChord.SkaldSwiftPerformanceId), 13)
                } },
                T("Performance"),
                "吟游演艺", "超凡之弦把职业等级加到提供吟游演艺的其他职业等级上，决定任何吟游演艺能力的效果与可用轮数，但不会获得新的表演。诗人／怒歌与超凡之弦等级叠加达到 7 与 13 时，还会获得对应的移动动作／迅捷动作启动表演能力。");

            SublimeChordActionFeatureGrant Grant(string name, BlueprintCharacterClass baseClass,
                BlueprintCharacterClass sc, BlueprintFeature feature, int level)
                => new SublimeChordActionFeatureGrant {
                    name = name,
                    Class = Ref<BlueprintCharacterClassReference>(baseClass),
                    SublimeChord = Ref<BlueprintCharacterClassReference>(sc),
                    Feature = Ref<BlueprintFeatureReference>(feature),
                    Level = level
                };

            // --- 奥术增强之歌: move action, Persuasion check, one-shot CL bonus ---
            var bonuses = new[] { 1, 2, 4 };
            var prepBuffs = bonuses.Select(n => f.Buff(T("ArcaneSongBuff" + n), "奥术增强之歌 +" + n,
                "下一个法术的施法者等级 +" + n + "。", new SublimeChordSpellBonus { Bonus = n })).ToArray();
            ArcaneAbility = f.Ability(T("ArcaneSongAbility"), "奥术增强之歌",
                "以一个移动动作进行一次沟通检定。下一次施展的法术按检定结果获得施法者等级加值：9 或更低 +0，10–19 +1，20–29 +2，30 或更高 +4。",
                AbilityType.Supernatural, UnitCommand.CommandType.Move, AbilityRange.Personal,
                Run(new SublimeChordPrepareSpell { Class = classRef, Buffs = prepBuffs.Select(Ref<BlueprintBuffReference>).ToArray() }));
            var arcaneSong = f.Feature(T("ArcaneSong"), "奥术增强之歌",
                "2 级起，以一个移动动作准备施法，按沟通检定结果提高下一个法术的施法者等级。", Facts(ArcaneAbility));

            // --- 永恒之歌: standard action, 60ft single target, Will negates ---
            // Clone the native Time Stop effect buff instead of hand-writing the
            // same six components. Hand-written components all had name == "",
            // and EntityFact.Setup dedupes runtime components by
            // SourceBlueprintComponentName: only the first one was ever created,
            // so the target stayed damageable and fully active.
            var freeze = f.Clone(f.Native<BlueprintBuff>(SublimeChord.TimeStopEffectBuffId),
                T("EternalFreeze"), "永恒之歌·静止",
                "被冻结在微微发光的永恒光环中，无法行动，也无法被任何力量影响。");
            FreezeBuff = freeze;
            var applyFreeze = new ContextActionApplyBuff {
                AsChild = true, IsFromSpell = false,
                DurationValue = new ContextDurationValue {
                    Rate = DurationRate.Minutes, DiceType = DiceType.Zero,
                    DiceCountValue = 0, BonusValue = new ContextValue { ValueType = ContextValueType.Rank }
                }
            };
            Set(applyFreeze, "m_Buff", Ref<BlueprintBuffReference>(freeze));
            var willSave = new ContextActionSavingThrow {
                Type = SavingThrowType.Will, HasCustomDC = true,
                CustomDC = new ContextValue { ValueType = ContextValueType.Shared, ValueShared = AbilitySharedValue.Duration },
                Actions = Actions(Saved(new GameAction[0], new GameAction[] { applyFreeze }))
            };
            EternalAbility = f.Ability(T("EternalSongAbility"), "永恒之歌",
                "以一个标准动作，用永恒力场包围 60 尺内的一个生物。目标可进行意志检定（DC = 10 + 超凡之弦等级 + 魅力调整值）忽略此效果；失败则被冻结，最多每等级一分钟。开始演奏会停止其他吟游演艺／怒歌，启动其他表演也会解除冻结；也可用「停止永恒之歌」提前解除。",
                AbilityType.Supernatural, UnitCommand.CommandType.Standard, AbilityRange.Custom,
                Run(new SublimeChordStopOtherPerformances(),
                    new SublimeChordComputeDC { Class = classRef, Output = AbilitySharedValue.Duration }, willSave),
                ClassRank(cls), Cost(bardResource, skaldResource, 1));
            EternalAbility.CustomRange = new Feet(60);
            EternalAbility.CanTargetFriends = false;
            EternalAbility.CanTargetPoint = false;
            var stop = f.Ability(T("EternalSongStopAbility"), "停止永恒之歌",
                "停止演奏，立即解除由自己施加的所有永恒之歌冻结效果。",
                AbilityType.Supernatural, UnitCommand.CommandType.Standard, AbilityRange.Personal,
                Run(new SublimeChordStopEternalSong { Freeze = Ref<BlueprintBuffReference>(freeze) }));
            var eternalSong = f.Feature(T("EternalSong"), "永恒之歌",
                "6 级起可用。冻结 60 尺内一个生物，意志豁免否定，最多每超凡之弦等级一分钟。一次只能维持一种表演：开启永恒之歌会停止其他吟游演艺／怒歌，开启其他表演也会解除冻结；另可用「停止永恒之歌」手动提前解除。",
                Facts(EternalAbility, stop));

            // --- 无限火焰之歌: 100ft point, 60ft WALL (Wall of Fire shape/FX),
            //     2x Persuasion, Reflex half, one-shot (not persistent) ---
            // AreaEffectView.SpawnFxs() loads Blueprint.Fx, so setting the FX
            // on a wall-shaped area reproduces the Wall of Fire visual. The
            // wall is spawned for a single round: it appears, hits once, and
            // disappears instead of persisting like the real spell.
            var wallDamage = new ContextActionDealDamage {
                ReadPreRolledFromSharedValue = true, PreRolledSharedValue = AbilitySharedValue.Damage,
                HalfIfSaved = true, IsAoE = true, IgnoreCritical = true, DisableSneakDamage = true,
                DamageType = new DamageTypeDescription { Type = DamageType.Energy, Energy = DamageEnergyType.Fire },
                Value = new ContextDiceValue { DiceType = DiceType.Zero, DiceCountValue = 0, BonusValue = 0 }
            };
            // ContextActionSavingThrow pushes SavingThrowData into the context,
            // which is what ContextActionDealDamage.HalfIfSaved reads; it also
            // lets the DC come from our own shared value rather than spell DC.
            var wallReflex = new ContextActionSavingThrow {
                Type = SavingThrowType.Reflex, HasCustomDC = true,
                CustomDC = new ContextValue { ValueType = ContextValueType.Shared, ValueShared = AbilitySharedValue.Duration },
                Actions = Actions(wallDamage)
            };
            var wallRun = new AbilityAreaEffectRunAction {
                name = "SublimeChordWallDamage",
                UnitEnter = Actions(wallReflex), UnitExit = Actions(),
                UnitMove = Actions(), Round = Actions()
            };
            var wall = f.Add(new BlueprintAbilityAreaEffect {
                Shape = AreaEffectShape.Wall, Size = new Feet(60),
                Fx = new PrefabLink { AssetId = SublimeChord.WallOfFireFxId },
                AffectEnemies = true, AggroEnemies = true, AffectDead = false,
                SpellResistance = false, CanBeUsedInTacticalCombat = false,
                ComponentsArray = new BlueprintComponent[] { wallRun }
            }, T("InfiniteFireWall"), "无限火焰之歌·火墙", "一瞬间显现的火焰之墙。");
            // IsSuitableTargetType() rejects allies when m_TargetType = Enemy.
            SetEnum(wall, "m_TargetType", "Enemy");
            Set(wall, "m_TickRoundAfterSpawn", false);
            var spawnWall = new ContextActionSpawnAreaEffect {
                DurationValue = new ContextDurationValue {
                    Rate = DurationRate.Rounds, DiceType = DiceType.Zero,
                    DiceCountValue = 0, BonusValue = 1
                },
                OnUnit = false
            };
            Set(spawnWall, "m_AreaEffect", Ref<BlueprintAbilityAreaEffectReference>(wall));
            // A single Persuasion check fixes the damage for the whole wall.
            // ContextActionSpawnAreaEffect clones the ability context with
            // CloneFor(), whose MechanicsContext constructor copies every
            // shared value, so UnitEnter sees Damage and Duration.
            var roll = new SublimeChord.SublimeChordSkillRoll {
                Stat = StatType.SkillPersuasion, Output = AbilitySharedValue.Damage,
                Multiplier = 2, Once = true
            };
            FireAbility = f.Ability(T("InfiniteFireAbility"), "无限火焰之歌",
                "消耗 2 轮吟游演艺轮数并进行一次沟通检定，在 100 尺内任意一点唤起一道 60 尺火焰之墙（与火墙术同形、同特效，但只显现一瞬）。墙内生物受到等于检定结果两倍的火焰伤害，可进行反射检定（DC = 10 + 超凡之弦等级 + 魅力调整值）使伤害减半；不伤害友军。",
                AbilityType.Supernatural, UnitCommand.CommandType.Standard, AbilityRange.Custom,
                Run(new SublimeChordComputeDC { Class = classRef, Output = AbilitySharedValue.Duration },
                    roll, spawnWall),
                Cost(bardResource, skaldResource, 2));
            FireAbility.CustomRange = new Feet(100);
            FireAbility.CanTargetPoint = true;
            FireAbility.CanTargetEnemies = false;
            FireAbility.CanTargetFriends = false;
            FireAbility.CanTargetSelf = false;
            FireAbility.EffectOnAlly = AbilityEffectOnUnit.None;
            var infiniteFire = f.Feature(T("InfiniteFire"), "无限火焰之歌",
                "10 级起可用。消耗 2 轮吟游演艺轮数，以沟通检定结果的两倍造成 60 尺火墙范围内的火焰伤害，反射减半。",
                Facts(FireAbility));

            return new SublimeChordFeatures {
                Knowledge = knowledge, Performance = performance, ArcaneSong = arcaneSong,
                EternalSong = eternalSong, InfiniteFire = infiniteFire
            };
        }

        // ---------- plan B: automatic performance reconciliation ----------

        internal static void Reconcile(BlueprintsCache cache, BlueprintCharacterClass cls, SimpleBlueprint[] snapshot)
        {
            Class = cls;
            if (cache == null || cls == null || snapshot == null) return;
            var byGuid = snapshot.Where(bp => bp != null)
                .GroupBy(bp => bp.AssetGuid).ToDictionary(g => g.Key, g => g.First());

            // Daily rounds are handled by SublimeChordPerformanceRounds on the
            // class's own performance feature, NOT by editing the shared native
            // resource blueprints' m_MaxAmount. Appending the SC class there
            // only worked for Raging Song: Bard rounds are split across two
            // sources (m_MaxAmount +1/level AND IncreaseResourcesByClass
            // +1/level +2 base +Cha), so appending to m_MaxAmount gave only
            // +1 per SC level instead of +2 (20 rounds instead of 30).
            // A single +2/SC-level bonus covers both pools uniformly and leaves
            // the shared native blueprints untouched.

            // Bardic Knowledge is not a performance-spending ability, so it is
            // the one pinned rank graph appended explicitly.
            if (byGuid.TryGetValue(KnowledgeId, out var knowledge) && knowledge is BlueprintFeature knowledgeFeature)
                foreach (var rank in (knowledgeFeature.ComponentsArray ?? Array.Empty<BlueprintComponent>()).OfType<ContextRankConfig>())
                { Register(rank); AppendClass(rank, cls); }

            var roots = snapshot.Where(bp => bp is BlueprintAbility || bp is BlueprintActivatableAbility)
                .Where(bp => Components(bp).Any(UsesPool)).ToArray();
            foreach (var rank in ReachableRanks(roots, byGuid))
            {
                Register(rank);
                var type = (ContextRankBaseValueType)Get(rank, "m_BaseValueType");
                // Sum-type ranks keep the original persistent m_Class append:
                // that is the behaviour the player already confirmed works for
                // native Bard/Skald performances. MaxClassLevelWithArchetype
                // must NOT be appended (it would double-count); the runtime
                // patch handles it, as well as any rank discovery missed.
                if (type == ContextRankBaseValueType.ClassLevel ||
                    type == ContextRankBaseValueType.SummClassLevelWithArchetype ||
                    type == ContextRankBaseValueType.OwnerSummClassLevelWithArchetype)
                {
                    if (!(bool)Get(rank, "m_ExceptClasses")) AppendClass(rank, cls);
                }
            }

            Icons(cache, snapshot);
        }

        private static BlueprintComponent[] Components(SimpleBlueprint bp)
            => (bp as BlueprintScriptableObject)?.ComponentsArray ?? Array.Empty<BlueprintComponent>();

        private static bool UsesPool(BlueprintComponent component)
        {
            if (component is AbilityResourceLogic ability)
                return IsPool((BlueprintAbilityResourceReference)Get(ability, "m_RequiredResource"));
            if (component is ActivatableAbilityResourceLogic toggle)
                return IsPool((BlueprintAbilityResourceReference)Get(toggle, "m_RequiredResource"));
            return false;
        }

        private static bool IsPool(BlueprintAbilityResourceReference reference)
            => reference != null && (reference.Guid == BardPool || reference.Guid == SkaldPool);

        private static void Register(ContextRankConfig rank)
        {
            if (rank == null) return;
            lock (RankGate) PerformanceRanks.Add(rank);
        }

        private static bool IsRegistered(ContextRankConfig rank)
        {
            lock (RankGate) return PerformanceRanks.Contains(rank);
        }

        private static void AppendClass(ContextRankConfig rank, BlueprintCharacterClass cls)
        {
            var current = (BlueprintCharacterClassReference[])Get(rank, "m_Class")
                ?? Array.Empty<BlueprintCharacterClassReference>();
            if (current.Any(r => r?.Guid == cls.AssetGuid)) return;
            Set(rank, "m_Class", current.Concat(new[] { Ref<BlueprintCharacterClassReference>(cls) }).ToArray());
        }

        // Walk the reference graph of every performance root, but only through
        // blueprints that are ALREADY deserialized. No Load() is ever forced,
        // so a late provider cannot turn this into a startup-wide scan.
        private static HashSet<ContextRankConfig> ReachableRanks(SimpleBlueprint[] roots,
            Dictionary<BlueprintGuid, SimpleBlueprint> byGuid)
        {
            var result = new HashSet<ContextRankConfig>(Identity<ContextRankConfig>.Instance);
            var seen = new HashSet<BlueprintGuid>(roots.Select(bp => bp.AssetGuid));
            var frontier = roots.ToList();
            // Inspire Courage's rank lives on InspireCourageEffectBuff, which is
            // three references below the toggle ability. Depth 3 stopped just
            // short of it, which is why the bonus stayed at 1. Go deeper, but
            // keep hard bounds so a late provider cannot widen this into a
            // startup-wide scan.
            for (int depth = 0; depth < 8 && frontier.Count > 0 &&
                 result.Count < 4096 && seen.Count < 8192; depth++)
            {
                var next = new List<SimpleBlueprint>();
                foreach (var bp in frontier)
                {
                    foreach (var rank in Components(bp).OfType<ContextRankConfig>())
                        result.Add(rank);
                    foreach (var guid in References(bp))
                    {
                        if (!seen.Add(guid) || !byGuid.TryGetValue(guid, out var target)) continue;
                        next.Add(target);
                    }
                }
                frontier = next.Distinct(Identity<SimpleBlueprint>.Instance).ToList();
            }
            return result;
        }

        private static IEnumerable<BlueprintGuid> References(SimpleBlueprint root)
        {
            var result = new List<BlueprintGuid>();
            var visited = new HashSet<object>(Identity<object>.Instance) { root };
            // Scan the whole blueprint, not just ComponentsArray: an activatable
            // performance keeps its effect buff in m_Buff, and variants in
            // AbilityVariants.m_Variants. Neither is a component field.
            ScanFields(root, result, visited, 0);
            return result;
        }

        private static void ScanFields(object node, List<BlueprintGuid> found,
            HashSet<object> visited, int depth)
        {
            if (node == null || depth > 7 || visited.Count > 6000 || found.Count > 4096) return;
            for (var type = node.GetType(); type != null && type != typeof(object); type = type.BaseType)
                foreach (var field in type.GetFields(BindingFlags.Instance | BindingFlags.Public |
                                                     BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
                {
                    if (field.IsStatic || field.FieldType.IsPrimitive || field.FieldType.IsEnum) continue;
                    object value;
                    try { value = field.GetValue(node); } catch { continue; }
                    if (value == null) continue;
                    if (value is BlueprintReferenceBase single)
                    {
                        if (single.Guid != default(BlueprintGuid)) found.Add(single.Guid);
                        continue;
                    }
                    if (value is string || value is SimpleBlueprint || value is UnityEngine.Object ||
                        value is Type || value is Delegate) continue;
                    if (value is IEnumerable sequence)
                    {
                        int taken = 0;
                        foreach (var item in sequence)
                        {
                            if (taken++ > 512 || found.Count > 4096) break;
                            if (item == null) continue;
                            if (item is BlueprintReferenceBase reference)
                            {
                                if (reference.Guid != default(BlueprintGuid)) found.Add(reference.Guid);
                                continue;
                            }
                            ScanValue(item, found, visited, depth + 1);
                        }
                    }
                    else ScanValue(value, found, visited, depth + 1);
                }
        }

        private static void ScanValue(object node, List<BlueprintGuid> found,
            HashSet<object> visited, int depth)
        {
            if (node == null || node is string || node is SimpleBlueprint ||
                node is UnityEngine.Object || node is Type || node is Delegate) return;
            var type = node.GetType();
            if (type.IsPrimitive || type.IsEnum) return;
            if (node is BlueprintReferenceBase reference)
            {
                if (reference.Guid != default(BlueprintGuid)) found.Add(reference.Guid);
                return;
            }
            if (!visited.Add(node)) return;
            ScanFields(node, found, visited, depth);
        }

        private static void Icons(BlueprintsCache cache, SimpleBlueprint[] snapshot)
        {
            // ExpandedContent is a hard dependency for these two icons; when the
            // provider is absent the icons simply stay blank (user decision).
            SetIcon(FireAbility, snapshot.FirstOrDefault(bp => bp.name == "WallOfFireAbility") as BlueprintUnitFact);
            SetIcon(ArcaneAbility, snapshot.FirstOrDefault(bp => bp.name == "ArcaneConcordanceAbility") as BlueprintUnitFact);
            SetIcon(EternalAbility, cache.Load(BlueprintGuid.Parse(SublimeChord.TimeStopAbilityId)) as BlueprintUnitFact);
        }

        private static void SetIcon(BlueprintAbility target, BlueprintUnitFact source)
        {
            if (target == null || source?.Icon == null) return;
            Set(target, "m_Icon", source.Icon);
        }

        // ---------- performance coexistence ----------

        // Native Bard performances use ActivatableAbilityGroup.BardicPerformance,
        // but some archetypes / third-party songs keep Group=None while still
        // spending one of the two pools (e.g. Deadly Vibrato). Check the group
        // first, then the actual resource logic so no provider is missed.
        internal static bool IsPerformanceToggle(BlueprintActivatableAbility bp)
        {
            if (bp == null) return false;
            if (bp.Group == ActivatableAbilityGroup.BardicPerformance) return true;
            return (bp.ComponentsArray ?? Array.Empty<BlueprintComponent>()).Any(UsesPool);
        }

        // Eternal Song has no separate "maintained" buff: stopping the song is
        // exactly removing the freeze buffs this caster applied. Both the manual
        // button and the native performance-toggle hook go through this path.
        //
        // The freeze is a clone of the native Time Stop effect buff, which carries
        // an Untargetable component. GameHelper.GetTargetsAround() explicitly
        // filters out untargetable units, so the old 200ft scan could never see
        // the frozen target at all - that is why neither the manual button nor
        // switching performances stopped the song. Enumerate the persistent unit
        // pool directly (no range / untargetable filter) and match by blueprint
        // GUID rather than by object identity so a reloaded blueprint cache or a
        // re-resolved reference cannot break the comparison.
        internal static void StopEternalSong(UnitEntityData caster)
        {
            var freeze = FreezeBuff;
            if (caster == null || freeze == null) return;
            var units = Game.Instance?.State?.Units;
            if (units == null) return;
            var freezeGuid = freeze.AssetGuid;
            foreach (var unit in units.ToArray())
            {
                var buffs = unit?.Buffs?.Enumerable;
                if (buffs == null) continue;
                foreach (var buff in buffs.ToArray())
                {
                    if (buff?.Blueprint == null || buff.Blueprint.AssetGuid != freezeGuid) continue;
                    if (!AppliedBy(buff, caster)) continue;
                    buff.Remove();
                }
            }
        }

        // A buff applied inside a saving throw / AsChild context can nest the
        // caster one or more parent contexts up, so walk the chain rather than
        // relying on a single MaybeCaster comparison.
        private static bool AppliedBy(EntityFact buff, UnitEntityData caster)
        {
            var ctx = buff?.MaybeContext;
            for (int depth = 0; ctx != null && depth < 8; depth++, ctx = ctx.ParentContext)
                if (ctx.MaybeCaster == caster) return true;
            return false;
        }

        private sealed class Identity<T> : IEqualityComparer<T> where T : class
        {
            internal static readonly Identity<T> Instance = new Identity<T>();
            public bool Equals(T x, T y) => ReferenceEquals(x, y);
            public int GetHashCode(T obj) => RuntimeHelpers.GetHashCode(obj);
        }

        // Priority.Last: Exemplar's own rank correction runs first and still
        // sees its expected __result. Patching GetBaseValue (not GetValue) is
        // required so the extra levels enter ApplyProgression.
        [HarmonyPatch(typeof(ContextRankConfig), "GetBaseValue")]
        [HarmonyPriority(Priority.Last)]
        private static class RankPatch
        {
            [HarmonyPostfix]
            private static void Postfix(ContextRankConfig __instance, MechanicsContext context, ref int __result)
            {
                var cls = Class;
                if (cls == null || context?.MaybeCaster == null) return;
                // Registration sees mostly an unloaded TOC. Once real rank
                // values are requested, discover late performance providers.
                SublimeChord.LazyDiscover();
                if (!ShouldScale(__instance)) return;
                int sc = context.MaybeCaster.Descriptor.Progression.GetClassLevel(cls);
                if (sc > 0) __result += sc;
            }

            // One rule for all four class-level rank flavours. Adding at
            // GetBaseValue (before ApplyProgression) means Custom/Div2 tables
            // still see the combined Bard/Skald + Sublime Chord level, which is
            // what Court Poet's Insightful Contemplation uses.
            private static bool ShouldScale(ContextRankConfig rank)
            {
                if (rank == null) return false;
                var type = (ContextRankBaseValueType)Get(rank, "m_BaseValueType");
                if (type != ContextRankBaseValueType.ClassLevel &&
                    type != ContextRankBaseValueType.MaxClassLevelWithArchetype &&
                    type != ContextRankBaseValueType.SummClassLevelWithArchetype &&
                    type != ContextRankBaseValueType.OwnerSummClassLevelWithArchetype) return false;
                if ((bool)Get(rank, "m_ExceptClasses")) return false;
                var classes = (BlueprintCharacterClassReference[])Get(rank, "m_Class")
                    ?? Array.Empty<BlueprintCharacterClassReference>();
                // Reconcile already appended Sublime Chord to m_Class for this
                // rank, so the native GetBaseValue already includes those
                // levels. Adding again here would double-count them.
                if (classes.Any(r => r != null && r.Guid == Class.AssetGuid)) return false;
                // Ranks discovered from a performance-spending ability are
                // covered even when a third-party mod uses its own class in
                // m_Class instead of Bard/Skald.
                if (IsRegistered(rank)) return true;
                // Fallback for ranks discovery missed: archetype effect buffs
                // that were never deserialized at scan time, and deep-copied
                // clones (Urban Skald's Controlled Inspired Rage) whose
                // ContextRankConfig instances differ from the native ones.
                // Restricting to buff/area owners excludes unrelated
                // Bard/Skald class features (summon spells, skill features,
                // companion buffs) from picking up the bonus.
                var owner = rank.OwnerBlueprint;
                if (!(owner is BlueprintBuff) && !(owner is BlueprintAbilityAreaEffect)) return false;
                return UsesPerformanceClass(rank);
            }

            private static bool UsesPerformanceClass(ContextRankConfig rank)
            {
                var classes = (BlueprintCharacterClassReference[])Get(rank, "m_Class")
                    ?? Array.Empty<BlueprintCharacterClassReference>();
                return classes.Any(r => r != null && (r.Guid == BardClassGuid || r.Guid == SkaldClassGuid));
            }
        }

        // AbilityCastRateUtils.GetSpellResourceAndCost() reads
        // AbilityData.RequiredResource when computing the available cast count,
        // so it must resolve to the pool the caster actually owns. Otherwise a
        // pure Skald always computes 0 casts and the ability is greyed out even
        // though the custom restriction passes. The UI reads Resource instead,
        // so both getters are redirected, guarded by our own component type.
        [HarmonyPatch(typeof(AbilityData), "get_RequiredResource")]
        private static class RequiredPoolPatch
        {
            private static void Postfix(AbilityData __instance, ref BlueprintScriptableObject __result)
            {
                if (!(__instance?.ResourceLogic is SublimeChordPerformanceCost cost)) return;
                var pool = cost.ResolveFor(__instance);
                if (pool != null) __result = pool;
            }
        }

        [HarmonyPatch(typeof(AbilityData), "get_Resource")]
        private static class DisplayPoolPatch
        {
            private static void Postfix(AbilityData __instance, ref BlueprintAbilityResource __result)
            {
                if (!(__instance?.ResourceLogic is SublimeChordPerformanceCost cost)) return;
                var pool = cost.ResolveFor(__instance);
                if (pool != null) __result = pool;
            }
        }

        // The native ActivatableAbilityGroup mutex only covers activatable
        // facts. Eternal Song is a targeted BlueprintAbility, so starting any
        // other performance needs this hook to end it. Scoped to characters
        // that actually have Sublime Chord levels: no global behaviour change.
        [HarmonyPatch(typeof(ActivatableAbility), "OnDidTurnOn")]
        private static class PerformanceTurnedOnPatch
        {
            private static void Postfix(ActivatableAbility __instance)
            {
                var cls = Class;
                if (cls == null || __instance == null || !__instance.IsOn) return;
                if (!IsPerformanceToggle(__instance.Blueprint)) return;
                var caster = __instance.Owner?.Unit;
                if (caster == null || caster.Descriptor.Progression.GetClassLevel(cls) <= 0) return;
                StopEternalSong(caster);
            }
        }

        // ---------- custom actions / components ----------

        [TypeId("d1c00000000000000000000000000005")]
        public sealed class SublimeChordPrepareSpell : ContextAction
        {
            public BlueprintCharacterClassReference Class;
            public BlueprintBuffReference[] Buffs;   // +1, +2, +4
            public override string GetCaption() => "超凡之弦：准备下一个法术";
            public override void RunAction()
            {
                var caster = Context?.MaybeCaster;
                if (caster == null || Buffs == null) return;
                var check = new RuleSkillCheck(caster, StatType.SkillPersuasion, 0) { Reason = Context, ShowAnyway = true };
                Context.TriggerRule(check);
                int roll = check.RollResult;
                int index = roll >= 30 ? 2 : roll >= 20 ? 1 : roll >= 10 ? 0 : -1;
                // A new preparation replaces the old one, and a low roll clears it.
                foreach (var buff in caster.Buffs.Enumerable.ToArray())
                    if (Buffs.Any(r => r?.Get() != null && r.Get() == buff.Blueprint)) buff.Remove();
                if (index < 0 || index >= Buffs.Length) return;
                var chosen = Buffs[index]?.Get();
                if (chosen != null) caster.Descriptor.AddBuff(chosen, Context, null);
            }
        }

        [TypeId("d1c00000000000000000000000000006")]
        public sealed class SublimeChordSpellBonus : UnitFactComponentDelegate,
            IInitiatorRulebookHandler<RuleCalculateAbilityParams>,
            IInitiatorRulebookHandler<RuleCastSpell>, IInitiatorRulebookSubscriber
        {
            public int Bonus;
            public void OnEventAboutToTrigger(RuleCalculateAbilityParams evt)
                => evt.AddBonusCasterLevel(Bonus, ModifierDescriptor.UntypedStackable);
            public void OnEventDidTrigger(RuleCalculateAbilityParams evt) { }
            public void OnEventAboutToTrigger(RuleCastSpell evt) { }
            public void OnEventDidTrigger(RuleCastSpell evt)
            {
                if (evt.Spell == null) return;
                var buff = Fact as Buff;
                if (buff != null && !buff.IsDisposed) buff.Remove();
            }
        }

        [TypeId("d1c00000000000000000000000000007")]
        public sealed class SublimeChordComputeDC : ContextAction
        {
            public BlueprintCharacterClassReference Class;
            public AbilitySharedValue Output;
            public override string GetCaption() => "超凡之弦：计算豁免 DC";
            public override void RunAction()
            {
                var caster = Context?.MaybeCaster;
                if (caster == null || Class == null) return;
                int sc = caster.Descriptor.Progression.GetClassLevel(Class.Get());
                int cha = caster.Stats.GetStat(StatType.Charisma)?.ModifiedValue ?? 10;
                Context[Output] = 10 + sc + (cha - 10) / 2;
            }
        }

        [TypeId("d1c00000000000000000000000000008")]
        public sealed class SublimeChordStopEternalSong : ContextAction
        {
            // Kept for old serialized action shapes; the freeze is now resolved
            // by GUID from the built blueprint, never by this reference.
            public BlueprintBuffReference Freeze;
            public override string GetCaption() => "停止永恒之歌";
            public override void RunAction() => StopEternalSong(Context?.MaybeCaster);
        }

        // Starting Eternal Song stops every other maintained performance. Uses
        // the public IsOn setter so the native OnDidTurnOff cleanup runs.
        [TypeId("d1c0000000000000000000000000000a")]
        public sealed class SublimeChordStopOtherPerformances : ContextAction
        {
            public override string GetCaption() => "超凡之弦：停止其他表演";
            public override void RunAction()
            {
                var caster = Context?.MaybeCaster;
                if (caster == null) return;
                foreach (var ability in caster.Descriptor.ActivatableAbilities.Enumerable.ToArray())
                    if (ability.IsOn && IsPerformanceToggle(ability.Blueprint)) ability.IsOn = false;
            }
        }

        // Dual-pool resource logic. AbilityData.AbilityResourceLogic is obtained
        // with Blueprint.GetComponent<AbilityResourceLogic>(), which matches by
        // `is T`, so this subclass is picked up by the native availability,
        // cast-rate and spend paths without any global patch.
        [TypeId("d1c00000000000000000000000000009")]
        public sealed class SublimeChordPerformanceCost : AbilityResourceLogic
        {
            public BlueprintAbilityResourceReference BardPool;
            public BlueprintAbilityResourceReference SkaldPool;

            internal BlueprintAbilityResource ResolveFor(AbilityData ability)
            {
                var bard = BardPool?.Get();
                var skald = SkaldPool?.Get();
                var caster = ability?.Caster;
                if (caster == null) return RequiredResource;
                int cost = CalculateCost(ability);
                var resources = caster.Resources;
                bool hasBard = bard != null && resources.ContainsResource(bard);
                bool hasSkald = skald != null && resources.ContainsResource(skald);
                // Prefer the pool that can actually pay; a multiclass character
                // with both pools never gets stuck on an empty Bard pool.
                if (hasBard && resources.HasEnoughResource(bard, cost)) return bard;
                if (hasSkald && resources.HasEnoughResource(skald, cost)) return skald;
                return hasBard ? bard : hasSkald ? skald : (bard ?? skald ?? RequiredResource);
            }

            public override bool IsAbilityRestrictionPassed(AbilityData ability)
            {
                if (ability?.Caster == null) return false;
                if (!IsSpendResource) return true;
                int cost = CalculateCost(ability);
                var pool = ResolveFor(ability);
                return pool != null && ability.Caster.Resources.HasEnoughResource(pool, cost);
            }

            public override void Spend(AbilityData ability)
            {
                if (!IsSpendResource || ability?.Caster == null) return;
                var unit = ability.Caster.Unit;
                if (unit == null || unit.Blueprint.IsCheater) return;
                int cost = CalculateCost(ability);
                var pool = ResolveFor(ability);
                if (pool == null || !ability.Caster.Resources.HasEnoughResource(pool, cost)) return;
                unit.Descriptor.Resources.Spend(pool, cost);
            }
        }

        // Adds Sublime Chord levels to the daily rounds of BOTH performance
        // pools. Native Bard rounds = 2 + 2*BardLevel + Cha (m_MaxAmount +1/lvl
        // plus IncreaseResourcesByClass +2 base +Cha +1/lvl); native Skald
        // rounds = 1 + 2*SkaldLevel + Cha (all in m_MaxAmount, level step 2).
        // The tabletop "4 + Cha at level 1, +2 per further level" is exactly
        // 2 + 2*Level + Cha, so adding 2 per SC level to either pool gives the
        // intended effective level without touching shared native resources.
        // IResourceAmountBonusHandler is raised from
        // BlueprintAbilityResource.GetMaxAmount() via EventBus on unit.Unit;
        // being a UnitFactComponentDelegate on the class feature makes it a
        // per-unit subscriber, so it only affects characters with the class.
        [TypeId("d1c0000000000000000000000000000b")]
        public sealed class SublimeChordPerformanceRounds : UnitFactComponentDelegate,
            IResourceAmountBonusHandler, IUnitSubscriber, ISubscriber
        {
            public BlueprintCharacterClassReference Class;
            public void CalculateMaxResourceAmount(BlueprintAbilityResource resource, ref int bonus)
            {
                var cls = Class?.Get();
                if (cls == null || Owner == null || resource == null) return;
                var guid = resource.AssetGuid.ToString();
                if (guid != SublimeChord.BardicPerformanceResourceId &&
                    guid != SublimeChord.RagingSongResourceId) return;
                int sc = Owner.Progression.GetClassLevel(cls);
                if (sc > 0) bonus += 2 * sc;
            }
        }

        // Grants the native Bard/Skald "start a performance as a move/swift
        // action" features when the COMBINED base-class + Sublime Chord level
        // reaches 7 / 13. Native progressions grant these at fixed class
        // levels via LevelEntries; AddFeatureOnClassLevel would re-add an
        // already-present Ranks=1 feature (bumping it to rank 2) and its
        // OnDeactivate could remove the native copy, so this stays idempotent
        // and never grants a duplicate. Respec rebuilds the unit from scratch,
        // so no explicit removal is needed.
        [TypeId("d1c0000000000000000000000000000c")]
        public sealed class SublimeChordActionFeatureGrant : UnitFactComponentDelegate,
            IOwnerGainLevelHandler, IUnitSubscriber, ISubscriber
        {
            public BlueprintCharacterClassReference Class;
            public BlueprintCharacterClassReference SublimeChord;
            public BlueprintFeatureReference Feature;
            public int Level;

            protected override void OnActivate() => Apply();
            public void HandleUnitGainLevel() => Apply();

            private void Apply()
            {
                var owner = Owner;
                var feature = Feature?.Get();
                if (owner == null || feature == null || Class == null || SublimeChord == null) return;
                int total = owner.Progression.GetClassLevel(Class.Get())
                          + owner.Progression.GetClassLevel(SublimeChord.Get());
                if (total < Level) return;
                if (owner.Progression.Features.GetRank(feature) > 0) return;
                owner.AddFact(feature);
            }
        }

        // Performance saving-throw DCs are computed by
        // ContextCalculateAbilityParamsBasedOnClass as
        // DC = 10 + GetClassLevel(Bard/Skald)/2 + Cha. That component writes
        // ReplaceSpellLevel directly from the class level, with no
        // ContextRankConfig, so the rank patch cannot reach it. Add the SC
        // levels at the rule level, scoped to Bard/Skald class calculators
        // (Fascinate, Frightening Tune, Battle Singer verses, Crumbling Blast,
        // Urban Skald's Infuriating Mockery, and third-party equivalents).
        [TypeId("d1c0000000000000000000000000000d")]
        public sealed class SublimeChordPerformanceDC : UnitFactComponentDelegate,
            IInitiatorRulebookHandler<RuleCalculateAbilityParams>, IInitiatorRulebookSubscriber
        {
            public BlueprintCharacterClassReference Class;

            public void OnEventAboutToTrigger(RuleCalculateAbilityParams evt)
            {
                var cls = Class?.Get();
                var blueprint = evt?.Blueprint;
                if (cls == null || blueprint == null || evt.Initiator?.Descriptor == null) return;
                var calculator = blueprint.GetComponent<ContextCalculateAbilityParamsBasedOnClass>();
                if (calculator == null) return;
                var target = calculator.CharacterClass;
                if (target == null || (target.AssetGuid != BardClassGuid && target.AssetGuid != SkaldClassGuid)) return;
                var owner = evt.Initiator.Descriptor;
                int sc = owner.Progression.GetClassLevel(cls);
                if (sc <= 0) return;
                int combined = owner.Progression.GetClassLevel(target) + sc;
                if (evt.ReplaceCasterLevel.HasValue) evt.ReplaceCasterLevel += sc;
                if (evt.ReplaceSpellLevel.HasValue) evt.ReplaceSpellLevel = combined / 2;
            }

            public void OnEventDidTrigger(RuleCalculateAbilityParams evt) { }
        }
    }
}

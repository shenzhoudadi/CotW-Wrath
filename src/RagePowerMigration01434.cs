using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Prerequisites;
using Kingmaker.Blueprints.Classes.Selection;
using Kingmaker.Blueprints.Facts;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.Designers.EventConditionActionSystem.Actions;
using Kingmaker.ElementsSystem;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.Enums;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.FactLogic;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Mechanics.Actions;
using Kingmaker.UnitLogic.Mechanics.Components;
using Kingmaker.UnitLogic.Mechanics.Conditions;
using static CallOfTheWild.Wrath.Prototype.PsychicAssets;

namespace CallOfTheWild.Wrath.Prototype
{
    // Each power publishes independently. Native action order, resources, rage
    // durations and all pre-existing selection entries remain authored by their
    // original owners; only a named feature-gated child action is appended.
    internal static class RagePowerMigration01434
    {
        static readonly string[] HostNames = { "StandartRageBuff", "StandartFocusedRageBuff", "PrimalistRagePowersBuff", "InspiredRageEffectBuff", "InspiredRageEffectBuffMythic" };
        static readonly string[] PoolNames = { "RagePowerSelection", "SkaldRagePowerSelection", "InstinctualWarriorRagePowerSelection", "ExtraRagePowerSelection" };
        sealed class Publication {
            internal BlueprintFeature Root;
            internal BlueprintBuff Effect;
            internal BlueprintScriptableObject[] Assets;
            internal string[] Definitions;
        }
        static readonly ConditionalWeakTable<BlueprintsCache, Dictionary<string, Publication>> Published = new ConditionalWeakTable<BlueprintsCache, Dictionary<string, Publication>>();
        internal static readonly Dictionary<string, string> OptionalDiagnostics = new Dictionary<string, string>();
        static string Definition(BlueprintScriptableObject bp) {
            var result = bp.GetType().FullName + "|" + bp.AssetGuid + "|" + bp.name + "|" + Migration01426.Definition(bp.ComponentsArray);
            if (bp is BlueprintFeature f) result += "|" + f.Ranks + "|" + f.IsClassFeature + "|" + string.Join(",", f.Groups) + "|" + f.HideInUI + "|" + f.HideInCharacterSheetAndLevelUp;
            if (bp is BlueprintBuff b) result += "|" + b.Stacking + "|" + Get(b, "m_Flags");
            return result;
        }
        internal static bool Restore(BlueprintsCache cache, string token) {
            if (!Published.TryGetValue(cache, out var entries) || !entries.TryGetValue(token, out var p)) return false;
            for (int i = 0; i < p.Assets.Length; i++) {
                var actual = cache.Load(p.Assets[i].AssetGuid) as BlueprintScriptableObject;
                if (!ReferenceEquals(actual, p.Assets[i]) || actual.ComponentsArray == null || actual.ComponentsArray.Any(c => c == null) || Definition(actual) != p.Definitions[i])
                    throw new InvalidOperationException("01434 published rage power identity or definition changed: " + p.Assets[i].name);
            }
            Connect(cache, token, p.Root, p.Effect, null);
            return true;
        }
        internal static void Optional(BlueprintsCache cache, string token) { Restore(cache, token); }
        internal static BlueprintFeature Root(PsychicAssets f, string token, string title, string text, int level, params BlueprintComponent[] prerequisites) {
            var barb = f.Native<BlueprintCharacterClass>(RagePowers.BarbarianId);
            var skald = f.Native<BlueprintCharacterClass>(RagePowers.SkaldId);
            var blood = f.Native<BlueprintCharacterClass>(RagePowers.BloodragerId);
            var primal = f.Native<BlueprintArchetype>(RagePowers.PrimalistId);
            if (primal.GetParentClass() != blood || !blood.Archetypes.Contains(primal)) throw new InvalidOperationException("01434 native Primalist parent changed.");
            var needs = new List<BlueprintComponent>();
            foreach (var cls in new[] { barb, skald }) {
                var p = new PrerequisiteClassLevel { Level = level, Group = Prerequisite.GroupType.Any };
                Set(p, "m_CharacterClass", Ref<BlueprintCharacterClassReference>(cls)); needs.Add(p);
            }
            var archetype = new PrerequisiteArchetypeLevel { Level = level, Group = Prerequisite.GroupType.Any };
            Set(archetype, "m_CharacterClass", Ref<BlueprintCharacterClassReference>(blood));
            Set(archetype, "m_Archetype", Ref<BlueprintArchetypeReference>(primal)); needs.Add(archetype);
            var root = f.Feature(token, title, text); root.IsClassFeature = true; root.Groups = new[] { FeatureGroup.RagePower };
            var once = new PrerequisiteNoFeature(); Set(once, "m_Feature", Ref<BlueprintFeatureReference>(root));
            root.ComponentsArray = needs.Concat(new BlueprintComponent[] { once }).Concat(prerequisites).ToArray();
            Set(root, "m_Icon", Get(f.Native<BlueprintFeature>(RagePowers.FearlessId), "m_Icon")); return root;
        }
        internal static ContextRankConfig Rank(PsychicAssets f, int baseBonus = 2, int step = 4, AbilityRankType type = AbilityRankType.Default) {
            if (baseBonus < 1 || step < 1) throw new ArgumentOutOfRangeException(nameof(baseBonus));
            var rank = new ContextRankConfig();
            Set(rank, "m_Type", type); Set(rank, "m_BaseValueType", ContextRankBaseValueType.SummClassLevelWithArchetype);
            Set(rank, "m_Progression", ContextRankProgression.StartPlusDivStep);
            Set(rank, "m_StartLevel", -(baseBonus - 1) * step); Set(rank, "m_StepLevel", step); Set(rank, "m_DisableRankBonus", true);
            Set(rank, "m_Class", new[] { RagePowers.BarbarianId, RagePowers.SkaldId, RagePowers.BloodragerId }.Select(id => Ref<BlueprintCharacterClassReference>(f.Native<BlueprintCharacterClass>(id))).ToArray());
            Set(rank, "Archetype", Ref<BlueprintArchetypeReference>(f.Native<BlueprintArchetype>(RagePowers.PrimalistId)));
            Set(rank, "m_AdditionalArchetypes", Array.Empty<BlueprintArchetypeReference>()); Set(rank, "m_FeatureList", Array.Empty<BlueprintFeatureReference>());
            return rank;
        }
        internal static int EffectiveLevel(UnitEntityData caster) {
            if (caster?.Progression == null) return 0;
            int level = 0;
            foreach (var c in caster.Progression.Classes) {
                string id = c.CharacterClass?.AssetGuid.ToString();
                if (id == RagePowers.BarbarianId || id == RagePowers.SkaldId || id == RagePowers.BloodragerId && c.Archetypes.Any(a => a.AssetGuid.ToString() == RagePowers.PrimalistId)) level += c.Level;
            }
            return level;
        }
        internal static void Commit(PsychicAssets f, BlueprintFeature root, BlueprintBuff effect) {
            if (Published.TryGetValue(f.Cache, out var previous) && previous.ContainsKey(f.Assets.First(x => ReferenceEquals(x.Value, root)).Key))
                throw new InvalidOperationException("01434 use Restore before rebuilding a published rage power.");
            foreach (var bp in f.Assets.Values) if (f.Cache.Load(bp.AssetGuid) != null)
                throw new InvalidOperationException("01434 foreign or partial rage power graph preserved: " + bp.AssetGuid);
            string token = f.Assets.First(x => ReferenceEquals(x.Value, root)).Key;
            Connect(f.Cache, token, root, effect, f);
            var assets = f.Assets.Values.ToArray();
            Published.GetOrCreateValue(f.Cache).Add(token, new Publication { Root = root, Effect = effect, Assets = assets, Definitions = assets.Select(Definition).ToArray() });
        }
        static AddFactContextActions Host(BlueprintBuff buff, int index) {
            if (buff.name != HostNames[index]) throw new InvalidOperationException("01434 native rage host identity changed: " + buff.name);
            var anchors = buff.GetComponents<AddFactContextActions>().Where(a => (a.Activated?.Actions ?? Array.Empty<GameAction>()).OfType<Conditional>().Any(c =>
                c.ConditionsChecker?.Conditions?.Length == 1 && !c.ConditionsChecker.Conditions[0].Not &&
                (c.ConditionsChecker.Conditions[0] is ContextConditionHasFact || c.ConditionsChecker.Conditions[0] is ContextConditionCasterHasFact) &&
                (Get(c.ConditionsChecker.Conditions[0], "m_Fact") as BlueprintUnitFactReference)?.Guid.ToString() == RagePowers.FearlessId &&
                c.IfTrue?.Actions?.Length == 1 && c.IfTrue.Actions[0] is ContextActionApplyBuff apply && apply.Buff?.AssetGuid.ToString() == RagePowers.FearlessBuffId &&
                apply.Permanent && apply.AsChild && !apply.ToCaster)).ToArray();
            if (anchors.Length != 1) throw new InvalidOperationException("01434 native Fearless Rage anchor missing or ambiguous: " + buff.name);
            return anchors[0];
        }
        static Conditional Child(string token, BlueprintFeature root, BlueprintBuff effect, bool fromCaster) {
            Condition condition = fromCaster ? (Condition)new ContextConditionCasterHasFact() : new ContextConditionHasFact();
            Set(condition, "m_Fact", Ref<BlueprintUnitFactReference>(root)); condition.name = "01434_" + token + "_Known";
            var apply = new ContextActionApplyBuff { name = "01434_" + token + "_Effect", Permanent = true, AsChild = true, IsNotDispelable = true };
            Set(apply, "m_Buff", Ref<BlueprintBuffReference>(effect));
            return new Conditional { name = "01434_" + token, ConditionsChecker = new ConditionsChecker { Operation = Operation.And, Conditions = new[] { condition } }, IfTrue = Actions(apply), IfFalse = Actions() };
        }
        sealed class Edit {
            internal object Target, Before, After; internal string Field;
            internal void Apply() => Set(Target, Field, After);
            internal void Restore() => Set(Target, Field, Before);
        }
        static void Connect(BlueprintsCache cache, string token, BlueprintFeature root, BlueprintBuff effect, PsychicAssets staged) {
            var f = staged ?? new PsychicAssets(cache);
            var pools = RagePowers.SelectionIds.Select((id, i) => {
                var p = f.Native<BlueprintFeatureSelection>(id);
                if (p.name != PoolNames[i] || p.IgnorePrerequisites || p.Group != FeatureGroup.RagePower && !p.HasGroup(FeatureGroup.RagePower)) throw new InvalidOperationException("01434 native rage selection changed: " + p.name);
                return p;
            }).ToArray();
            var edits = new List<Edit>();
            for (int h = 0; h < RagePowers.HostIds.Length; h++) {
                var host = f.Native<BlueprintBuff>(RagePowers.HostIds[h]); var actions = Host(host, h);
                var child = Child(token, root, effect, h >= 3); var old = actions.Activated.Actions ?? Array.Empty<GameAction>();
                var found = old.Where(a => a?.name == child.name).ToArray();
                if (found.Length > 1 || found.Length == 1 && Migration01426.Definition(found[0]) != Migration01426.Definition(child)) throw new InvalidOperationException("01434 rage child action collision preserved: " + host.name + "/" + token);
                if (found.Length == 0) edits.Add(new Edit { Target = actions, Field = "Activated", Before = actions.Activated, After = Actions(old.Concat(new GameAction[] { child }).ToArray()) });
                if (h < 3) continue;
                var shares = host.GetComponents<AddFactsFromCaster>().ToArray();
                if (shares.Length != 1) throw new InvalidOperationException("01434 Skald feature sharing missing or ambiguous.");
                var share = shares[0];
                if (share.FeatureFromSelection) {
                    string id = (Get(share, "m_Selection") as BlueprintFeatureSelectionReference)?.Guid.ToString();
                    if (id != RagePowers.SelectionIds[0] && id != RagePowers.SelectionIds[1]) throw new InvalidOperationException("01434 Skald shared selection changed.");
                } else {
                    var before = (BlueprintUnitFactReference[])Get(share, "m_Facts"); var values = before ?? Array.Empty<BlueprintUnitFactReference>();
                    if (!values.Any(r => r?.Guid.ToString() == RagePowers.FearlessId)) throw new InvalidOperationException("01434 Skald explicit sharing anchor changed.");
                    int count = values.Count(r => r?.Guid == root.AssetGuid);
                    if (count > 1) throw new InvalidOperationException("01434 duplicate shared rage root.");
                    if (count == 0) edits.Add(new Edit { Target = share, Field = "m_Facts", Before = before, After = values.Concat(new[] { Ref<BlueprintUnitFactReference>(root) }).ToArray() });
                }
            }
            var poolBefore = pools.Select(p => new[] { Get(p, "m_Features"), Get(p, "m_AllFeatures") }).ToArray();
            try {
                if (staged != null) Migration01426.Commit(staged, pools, root); else T09CombatFeats.Append(pools, new[] { root });
                foreach (var edit in edits) edit.Apply();
            } catch {
                foreach (var edit in edits.AsEnumerable().Reverse()) edit.Restore();
                for (int i = 0; i < pools.Length; i++) { Set(pools[i], "m_Features", poolBefore[i][0]); Set(pools[i], "m_AllFeatures", poolBefore[i][1]); }
                if (staged != null) foreach (var bp in staged.Assets.Values) if (ReferenceEquals(cache.Load(bp.AssetGuid), bp)) cache.RemoveCachedBlueprint(bp.AssetGuid);
                throw;
            }
            OptionalDiagnostics.Remove(token);
            BlueprintFeatureSelection optional = null; object[] optionalBefore = null;
            try {
                var candidate = cache.Load(BlueprintGuid.Parse(TerrifyingHowl.TttPrimalistId)); if (candidate == null) return;
                optional = candidate as BlueprintFeatureSelection;
                if (optional == null || optional.name != "PrimalistRagePowerSelection" || optional.IgnorePrerequisites || optional.Group != FeatureGroup.RagePower && !optional.HasGroup(FeatureGroup.RagePower)) throw new InvalidOperationException("01434 optional TTT Primalist selection changed.");
                optionalBefore = new[] { Get(optional, "m_Features"), Get(optional, "m_AllFeatures") };
                T09CombatFeats.Append(new[] { optional }, new[] { root });
            } catch (Exception e) {
                if (optionalBefore != null) { Set(optional, "m_Features", optionalBefore[0]); Set(optional, "m_AllFeatures", optionalBefore[1]); }
                OptionalDiagnostics[token] = "01434 optional TTT Primalist integration skipped; native rage power remains registered: " + token + ". " + e;
                ModEntry.LogSpellDiagnostic(OptionalDiagnostics[token]);
            }
        }
    }
}

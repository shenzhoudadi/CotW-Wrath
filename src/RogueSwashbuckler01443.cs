// Adapted from Holic75/KingmakerRebalance (MIT); see LICENSE-CotW.txt.
using System;
using System.Linq;
using System.Runtime.CompilerServices;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Prerequisites;
using Kingmaker.Blueprints.Classes.Selection;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.Designers.Mechanics.Facts;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Enums;
using Kingmaker.UnitLogic.FactLogic;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Mechanics.Components;
using static CallOfTheWild.Wrath.Prototype.PsychicAssets;

namespace CallOfTheWild.Wrath.Prototype
{
    internal static class RogueSwashbuckler01443
    {
        internal const string ModuleName = "Swashbuckler (Rogue archetype)";
        internal const string RootToken = "RogueSwashbucklerArchetype01443", TrainingToken = "RogueSwashbucklerMartialTraining01443", DaringToken = "RogueSwashbucklerDaring01443", SecondTrickToken = "RogueSwashbucklerSecondCombatTrick01443";
        internal const string RogueId = "299aa766dee3cbf4790da4efb8c72484", ProgressionId = "b57b2a75a5abcaf47a01cf84672b50e9";
        internal const string TrapfindingId = "dbb6b3bffe6db3547b31c3711653838e", DangerId = "0bcbe9e450b0e7b428f08f66c53c5136", MartialId = "203992ef5b35c864390b4e4a1e200629";
        internal const string CombatTrickId = "c5158a6622d0b694a99efb1d0025d2c1", TalentId = "c074a5d615200494b8f2a9c845799d93", IconId = "4be5757b85af47545a5789f1d03abda9";
        internal static readonly string[] NativeIds = { RogueId, ProgressionId, TrapfindingId, DangerId, MartialId, CombatTrickId, TalentId, IconId };
        internal static readonly string[] OwnTokens = { RootToken, TrainingToken, DaringToken, SecondTrickToken };
        internal static readonly int[] DaringLevels = { 3, 6, 9, 12, 15, 18 };
        sealed class Receipt { internal BlueprintScriptableObject[] Assets; internal string[] Definitions; internal BlueprintCharacterClass Class; internal BlueprintProgression Progression; internal string[] FirstRefs, AllRefs; }
        static readonly ConditionalWeakTable<BlueprintsCache, Receipt> Published = new ConditionalWeakTable<BlueprintsCache, Receipt>();
        static LevelEntry Entry(int level, BlueprintFeature feature) { var e = new LevelEntry { Level = level }; Set(e, "m_Features", new[] { Ref<BlueprintFeatureBaseReference>(feature) }.ToList()); return e; }
        static string Definition(BlueprintScriptableObject bp)
        {
            object shape;
            if (bp is BlueprintArchetype a) shape = new object[] { a.GetParentClass(), a.AddFeatures.Select(e => new object[] { e.Level, Get(e, "m_Features") }).ToArray(), a.RemoveFeatures.Select(e => new object[] { e.Level, Get(e, "m_Features") }).ToArray(), a.RemoveSpellbook, a.ChangeCasterType, a.ReplaceClassSkills, a.AddSkillPoints };
            else if (bp is BlueprintFeatureSelection s) shape = new object[] { s.Ranks, s.Groups, s.IsClassFeature, s.Mode, s.Group, s.Group2, s.IgnorePrerequisites };
            else if (bp is BlueprintFeature f) shape = new object[] { f.Ranks, f.Groups, f.IsClassFeature, f.ReapplyOnLevelUp };
            else throw new InvalidOperationException("Unexpected rogue Swashbuckler private type.");
            return Migration01426.Definition(new object[] { bp.name, bp.ComponentsArray, shape });
        }
        static void Append(BlueprintCharacterClass rogue, BlueprintProgression progression, BlueprintArchetype root, BlueprintFeature training, BlueprintFeature daring)
        {
            var archetypes = (BlueprintArchetypeReference[])Get(rogue, "m_Archetypes"); var groups = progression.UIGroups;
            var matches = (archetypes ?? Array.Empty<BlueprintArchetypeReference>()).Count(r => r.Guid == root.AssetGuid);
            if (matches > 1) throw new InvalidOperationException("Rogue Swashbuckler duplicate archetype reference.");
            var ui = (groups ?? Array.Empty<UIGroup>()).Where(g => g.Features.Any(f => f == training || f == daring)).ToArray();
            if (ui.Length > 1 || ui.Length == 1 && !ui[0].Features.SequenceEqual(new[] { training, daring })) throw new InvalidOperationException("Rogue Swashbuckler UI group changed.");
            if (matches == 0) Set(rogue, "m_Archetypes", (archetypes ?? Array.Empty<BlueprintArchetypeReference>()).Concat(new[] { Ref<BlueprintArchetypeReference>(root) }).ToArray());
            if (ui.Length == 0) { var group = new UIGroup(); Set(group, "m_Features", new[] { training, daring }.Select(Ref<BlueprintFeatureBaseReference>).ToList()); progression.UIGroups = (groups ?? Array.Empty<UIGroup>()).Concat(new[] { group }).ToArray(); }
        }
        static void Attach(BlueprintCharacterClass rogue, BlueprintProgression progression, BlueprintArchetype root, BlueprintFeature training, BlueprintFeature daring)
        {
            var archetypes = Get(rogue, "m_Archetypes"); var groups = progression.UIGroups;
            try { Append(rogue, progression, root, training, daring); }
            catch { Set(rogue, "m_Archetypes", archetypes); progression.UIGroups = groups; throw; }
        }
        static BlueprintFeatureReference[] Refs(BlueprintFeatureSelection s, string field) => (BlueprintFeatureReference[])Get(s, field) ?? Array.Empty<BlueprintFeatureReference>();
        static string[] Signature(BlueprintFeatureSelection s, string field) => Refs(s, field).Select(r => r?.Guid.ToString() ?? "null").ToArray();
        static bool OnceOnly(BlueprintFeatureSelection trick)
        {
            var prerequisites = trick.GetComponents<Prerequisite>().ToArray();
            if (prerequisites.Any(p => !(p is PrerequisiteNoFeature no && no.Feature == trick && no.Group == Prerequisite.GroupType.All))) throw new InvalidOperationException("Rogue Swashbuckler preserves an unsupported external Combat Trick prerequisite; no bypass was installed.");
            return trick.Mode == SelectionMode.OnlyNew || prerequisites.Length > 0;
        }
        // A second choice is needed only when the upstream talent is once-only
        // (as in current TTT). Vanilla's repeatable talent gets no duplicate UI.
        // Private child arrays are checked against their last expected source
        // projection before a legitimate late feat-list refresh is applied.
        static void Connect(BlueprintCharacterClass rogue, BlueprintProgression progression, Receipt receipt, BlueprintFeatureSelection trick, BlueprintFeatureSelection pool)
        {
            var second = (BlueprintFeatureSelection)receipt.Assets[3]; bool once = OnceOnly(trick);
            if (receipt.FirstRefs != null && (!Signature(second, "m_Features").SequenceEqual(receipt.FirstRefs) || !Signature(second, "m_AllFeatures").SequenceEqual(receipt.AllRefs))) throw new InvalidOperationException("Rogue Swashbuckler second Combat Trick child references changed.");
            var children = new[] { Refs(trick, "m_Features"), Refs(trick, "m_AllFeatures") }.Select(a => a.GroupBy(r => r?.Guid ?? default(BlueprintGuid)).Select(g => g.First()).ToArray()).ToArray();
            if (children.Any(a => a.Any(r => r == null || r.Guid == default(BlueprintGuid) || r.Guid == second.AssetGuid || r.Guid == receipt.Assets[0].AssetGuid))) throw new InvalidOperationException("Rogue Swashbuckler Combat Trick contains an invalid or recursive reference.");
            var archetypes = Get(rogue, "m_Archetypes"); var groups = progression.UIGroups; var oldChildren = new[] { Get(second, "m_Features"), Get(second, "m_AllFeatures") }; var oldPools = new[] { Get(pool, "m_Features"), Get(pool, "m_AllFeatures") };
            try {
                Attach(rogue, progression, (BlueprintArchetype)receipt.Assets[0], (BlueprintFeature)receipt.Assets[1], (BlueprintFeature)receipt.Assets[2]);
                for (int i = 0; i < 2; i++) { var name = i == 0 ? "m_Features" : "m_AllFeatures"; Set(second, name, children[i]); var prior = Refs(pool, name); var others = prior.Where(r => r.Guid != second.AssetGuid).ToArray(); if (prior.Count(r => r.Guid == second.AssetGuid) > 1) throw new InvalidOperationException("Rogue Swashbuckler duplicate second Combat Trick entry."); Set(pool, name, once ? others.Concat(new[] { Ref<BlueprintFeatureReference>(second) }).ToArray() : others); }
                receipt.FirstRefs = Signature(second, "m_Features"); receipt.AllRefs = Signature(second, "m_AllFeatures");
            } catch { Set(rogue, "m_Archetypes", archetypes); progression.UIGroups = groups; for (int i = 0; i < 2; i++) { var n = i == 0 ? "m_Features" : "m_AllFeatures"; Set(second, n, oldChildren[i]); Set(pool, n, oldPools[i]); } throw; }
        }
        sealed class LinkSnapshot
        {
            readonly BlueprintCharacterClass cls; readonly BlueprintProgression progression; readonly BlueprintFeatureSelection second, pool; readonly Receipt receipt;
            readonly object archetypes, firstChildren, allChildren, firstPool, allPool; readonly UIGroup[] groups; readonly string[] firstExpected, allExpected;
            internal LinkSnapshot(BlueprintCharacterClass c, BlueprintProgression p, BlueprintFeatureSelection s, BlueprintFeatureSelection t, Receipt r) { cls = c; progression = p; second = s; pool = t; receipt = r; archetypes = Get(c, "m_Archetypes"); groups = p.UIGroups; firstChildren = Get(s, "m_Features"); allChildren = Get(s, "m_AllFeatures"); firstPool = Get(t, "m_Features"); allPool = Get(t, "m_AllFeatures"); firstExpected = r.FirstRefs; allExpected = r.AllRefs; }
            internal void Restore() { Set(cls, "m_Archetypes", archetypes); progression.UIGroups = groups; Set(second, "m_Features", firstChildren); Set(second, "m_AllFeatures", allChildren); Set(pool, "m_Features", firstPool); Set(pool, "m_AllFeatures", allPool); receipt.FirstRefs = firstExpected; receipt.AllRefs = allExpected; }
        }
        internal static void Register(BlueprintsCache cache)
        {
            var receipt = Published.GetValue(cache, _ => new Receipt()); lock (receipt) {
                var f = new PsychicAssets(cache); var rogue = f.Native<BlueprintCharacterClass>(RogueId); var progression = f.Native<BlueprintProgression>(ProgressionId);
                var trap = f.Native<BlueprintFeature>(TrapfindingId); var danger = f.Native<BlueprintFeature>(DangerId); var martial = f.Native<BlueprintFeature>(MartialId);
                var trick = f.Native<BlueprintFeatureSelection>(CombatTrickId); var pool = f.Native<BlueprintFeatureSelection>(TalentId); OnceOnly(trick);
                if (rogue.Progression != progression || progression.LevelEntries.Count(e => e.Level == 1 && e.Features.Contains(trap)) != 1 || DaringLevels.Any(n => progression.LevelEntries.Count(e => e.Level == n && e.Features.Contains(danger)) != 1)) throw new InvalidOperationException("Rogue Swashbuckler requires the native Rogue progression and replacement anchors.");
                if (rogue.Archetypes.Any(a => a != null && a.AssetGuid != Id(RootToken) && (a.name ?? "").Replace("_", "").Replace(" ", "").Replace("-", "").IndexOf("Swashbuckler", StringComparison.OrdinalIgnoreCase) >= 0)) throw new InvalidOperationException("An external Rogue Swashbuckler archetype already exists; preserved.");
                if (receipt.Assets != null) {
                    if (!ReferenceEquals(rogue, receipt.Class) || !ReferenceEquals(progression, receipt.Progression)) throw new InvalidOperationException("Rogue Swashbuckler parent identity changed.");
                    for (int i = 0; i < receipt.Assets.Length; i++) {
                        if (!ReferenceEquals(cache.Load(receipt.Assets[i].AssetGuid), receipt.Assets[i])) throw new InvalidOperationException("Rogue Swashbuckler published identity changed: " + receipt.Assets[i].name);
                        if (receipt.Assets[i].ComponentsArray.Any(c => c == null || !ReferenceEquals(c.OwnerBlueprint, receipt.Assets[i]))) throw new InvalidOperationException("Rogue Swashbuckler published component ownership changed: " + receipt.Assets[i].name);
                        var actual = Definition(receipt.Assets[i]); if (actual != receipt.Definitions[i]) throw new InvalidOperationException("Rogue Swashbuckler published asset changed: " + receipt.Assets[i].name + "; " + Migration01426.Difference(receipt.Definitions[i], actual));
                    }
                    var previous = new LinkSnapshot(rogue, progression, (BlueprintFeatureSelection)receipt.Assets[3], pool, receipt);
                    try { Connect(rogue, progression, receipt, trick, pool); } catch { previous.Restore(); throw; } return;
                }
                foreach (var token in OwnTokens) if (cache.Load(Id(token)) != null) throw new InvalidOperationException("Rogue Swashbuckler private identity collision: " + token);
                var grantMartial = new AddFeatureIfHasFact { Not = true }; Set(grantMartial, "m_CheckedFact", Ref<BlueprintUnitFactReference>(martial)); Set(grantMartial, "m_Feature", Ref<BlueprintUnitFactReference>(martial));
                var training = f.Feature(TrainingToken, "Martial Training", "At Rogue level 1, gain proficiency with all martial weapons. You may take Combat Trick twice. When an upstream mod restricts the native talent to once, a second Combat Trick becomes available after your first; Wrath's unrestricted repeatable native talent otherwise remains unchanged.", grantMartial); training.IsClassFeature = true;
                var daring = f.Feature(DaringToken, "Daring", "At Rogue level 3, gain a +1 morale bonus on Mobility checks and saving throws against fear. The bonus increases by 1 at levels 6, 9, 12, 15 and 18, to a maximum of +6.",
                    new AddContextStatBonus { Stat = StatType.SkillMobility, Descriptor = ModifierDescriptor.Morale, Multiplier = 1, Value = new ContextValue { ValueType = ContextValueType.Rank } },
                    new SavingThrowBonusAgainstDescriptor { SpellDescriptor = SpellDescriptor.Fear, ModifierDescriptor = ModifierDescriptor.Morale, Value = 1, Bonus = 0 });
                daring.Ranks = 6; daring.ReapplyOnLevelUp = true; daring.IsClassFeature = true;
                var rank = new ContextRankConfig(); Set(rank, "m_Type", AbilityRankType.Default); Set(rank, "m_BaseValueType", ContextRankBaseValueType.FeatureRank); Set(rank, "m_Feature", Ref<BlueprintFeatureReference>(daring)); Set(rank, "m_Progression", ContextRankProgression.AsIs); daring.ComponentsArray = daring.ComponentsArray.Concat(new BlueprintComponent[] { rank }).ToArray();
                Set(training, "m_Icon", Get(martial, "m_Icon")); Set(daring, "m_Icon", Get(f.Native<BlueprintScriptableObject>(IconId), "m_Icon"));
                // Construct a private selection instead of cloning the native
                // blueprint's lazy element/proxy registries. Only its two feat
                // group filters and explicit child GUID projection are reused.
                var second = f.Add(new BlueprintFeatureSelection { IsClassFeature = true, Group = trick.Group, Group2 = trick.Group2 }, SecondTrickToken, "Combat Trick (Swashbuckler's Second Choice)", "Requires Martial Training and the original Combat Trick. Gain one additional combat feat, satisfying its normal prerequisites. This additional talent can be selected once."); Set(second, "m_Icon", Get(trick, "m_Icon"));
                var noSecond = new PrerequisiteNoFeature(); Set(noSecond, "m_Feature", Ref<BlueprintFeatureReference>(second)); second.ComponentsArray = new BlueprintComponent[] { Migration01426.Require(training), Migration01426.Require(trick), noSecond };
                // The explicit Rogue parent is its only acquisition route.
                // Avoid TTT's global RogueTalent-group rewrite adding another
                // self prerequisite to this already once-only private option.
                second.Groups = Array.Empty<FeatureGroup>(); second.Ranks = 1; second.Mode = SelectionMode.OnlyNew; second.IgnorePrerequisites = false;
                const string description = "A Rogue archetype focused on martial training and daring acrobatics. Martial Training replaces Trapfinding at level 1 and permits a second Combat Trick when an upstream mod makes the native talent once-only. Daring replaces Danger Sense at levels 3, 6, 9, 12, 15 and 18. Wrath's unrestricted Combat Trick and all unrelated talents, sneak attack and class features are retained. This is a Rogue archetype, separate from any standalone Swashbuckler class.";
                var root = f.Add(new BlueprintArchetype { AddFeatures = new[] { Entry(1, training) }.Concat(DaringLevels.Select(n => Entry(n, daring))).ToArray(), RemoveFeatures = new[] { Entry(1, trap) }.Concat(DaringLevels.Select(n => Entry(n, danger))).ToArray() }, RootToken, "Swashbuckler (Rogue)", description);
                Set(root, "m_ParentClass", rogue); root.LocalizedName = Text(RootToken, "name", "Swashbuckler (Rogue)"); root.LocalizedDescription = Text(RootToken, "description", description);
                var assets = OwnTokens.Select(t => f.Assets[t]).ToArray(); foreach (var bp in assets) PsychicRepairComponents.UniqueNames(bp);
                var beforeLinks = new LinkSnapshot(rogue, progression, second, pool, receipt);
                try {
                    Migration01426.Commit(f, Array.Empty<Kingmaker.Blueprints.Classes.Selection.BlueprintFeatureSelection>());
                    var next = new Receipt { Assets = assets, Definitions = assets.Select(Definition).ToArray(), Class = rogue, Progression = progression }; Connect(rogue, progression, next, trick, pool);
                    receipt.Assets = next.Assets; receipt.Definitions = next.Definitions; receipt.Class = rogue; receipt.Progression = progression; receipt.FirstRefs = next.FirstRefs; receipt.AllRefs = next.AllRefs;
                } catch { beforeLinks.Restore(); foreach (var bp in assets) if (ReferenceEquals(cache.Load(bp.AssetGuid), bp)) cache.RemoveCachedBlueprint(bp.AssetGuid); throw; }
            }
        }
    }
}

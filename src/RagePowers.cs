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
using Kingmaker.Designers.EventConditionActionSystem.Actions;
using Kingmaker.Designers.Mechanics.Facts;
using Kingmaker.ElementsSystem;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Enums;
using Kingmaker.Enums.Damage;
using Kingmaker.Localization;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.FactLogic;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Mechanics.Actions;
using Kingmaker.UnitLogic.Mechanics.Components;
using Kingmaker.UnitLogic.Mechanics.Conditions;

namespace CallOfTheWild.Wrath.Prototype
{
    // Rage effects and the lethal-stance rider use native components and child buffs.
    // No additional patches, custom save component types, or changes to rage action economy.
    // Cover penalties are not implemented: Wrath has no general cover penalty on attacks.
    internal static class RagePowers
    {
        internal const string ModuleName = "Rage powers";
        internal const string BarbarianId = "f7d7eb166b3dd594fb330d085df41853";
        internal const string SkaldId = "6afa347d804838b48bda16acb0573dc0";
        internal const string BloodragerId = "d77e67a814d686842802c9cfd8ef8499";
        internal const string PrimalistId = "4d588290ed0736e4f8cd3f4c3beacd69";
        internal const string FearlessId = "2f59c85adec695b418a66242f2ba08b8";
        internal const string FearlessBuffId = "7f043b6980cdcbe42b52f0837a0e7361";
        internal static readonly string[] SelectionIds = {
            "28710502f46848d48b3f0d6132817c4e", // Barbarian and native Primalist progressions
            "2476514e31791394fa140f1a07941c96", // Skald
            "609f0e5336084442a0dafa3abd4d31c5", // Instinctual Warrior
            "0c7f01fbbe687bb4baff8195cb02fe6a"  // Extra Rage Power
        };
        private static readonly string[] SelectionNames = {
            "RagePowerSelection", "SkaldRagePowerSelection", "InstinctualWarriorRagePowerSelection", "ExtraRagePowerSelection"
        };
        internal static readonly string[] HostIds = {
            "da8ce41ac3cd74742b80984ccc3c9613", "3513326cd64f475781799685c57fa452",
            "ecc22ca1eea1bf6488a0d7c6ee2527d8", "75b3978757908d24aaaecaf2dc209b89", "ee51f69e69cb43c3bc7c02002e482d17"
        };
        private static readonly string[] HostNames = {
            "StandartRageBuff", "StandartFocusedRageBuff", "PrimalistRagePowersBuff", "InspiredRageEffectBuff", "InspiredRageEffectBuffMythic"
        };
        internal static readonly string[] Tokens = {
            "QuickReflexes", "UnrestrainedRage", "AcidResistance", "ColdResistance", "FireResistance", "ElectricityResistance", "SonicResistance"
        };
        internal static readonly string[] FeatureIds = {
            "93f4b74bb82a5dbdb483d87c3b32370f", "b7f296c3f32d51e381d24135325aefe3", "4c320d9835715d71aa742ba0f4d20798",
            "09e53ab12aa65345940a544e9a22f12a", "0a957c73217953d584b4f11586e2a9d7", "11cf2df53c7853cbb11bfbe58affdb12", "1498b2dba0a45799be97b1a12c03b5d5"
        };
        internal static readonly string[] BuffIds = {
            "743651376bb15046a95b21adba4cac24", "1c6ec72ebd2a5d338753a221b96148a0", "7453e034c72a54d69c02fa2dd9788dee",
            "18dade26d6d258e397a6ac544e7ebcd9", "79b0ca2893bd552590951d09dcf5647f", "0ad0a239aca35525a589ddb2a2347546", "ebabafc0c32851cdba9bba168ca1f437"
        };
        internal const string LethalStanceFeatureId = "e4450dd9c06dc034fb7c0c08abcc202b";
        internal const string LethalStanceEffectId = "c6271b3183c48d54b8defd272bea0665";
        internal const string DeadlyAccuracyId = "c841ffa13d39ce442a408f57feb3cb8e";
        internal const string DeadlyAccuracyBuffId = "0c6e198a78210954c9fe245a26b0c315";
        internal const string SharpenedFeatureId = "92308b69f8864b76841a50a918c84300";
        internal const string SharpenedBuffId = "2fa747d6373a4d5a86144fc601a99592";
        private static readonly DamageEnergyType[] Energies = {
            DamageEnergyType.Acid, DamageEnergyType.Cold, DamageEnergyType.Fire, DamageEnergyType.Electricity, DamageEnergyType.Sonic
        };

        internal static void Register(BlueprintsCache cache)
        {
            var barbarian = Require<BlueprintCharacterClass>(cache, BarbarianId, "BarbarianClass");
            var skald = Require<BlueprintCharacterClass>(cache, SkaldId, "SkaldClass");
            var bloodrager = Require<BlueprintCharacterClass>(cache, BloodragerId, "BloodragerClass");
            var primalist = Require<BlueprintArchetype>(cache, PrimalistId, "PrimalistArchetype");
            if (primalist.GetParentClass() != bloodrager || !bloodrager.Archetypes.Contains(primalist))
                throw new InvalidOperationException("Primalist class/archetype relationship differs from the native layout.");
            var icon = Require<BlueprintFeature>(cache, FearlessId, "FearlessRageFeature");
            Require<BlueprintBuff>(cache, FearlessBuffId, "FearlessRageRageBuff");
            var pools = SelectionIds.Select((id, i) => Require<BlueprintFeatureSelection>(cache, id, SelectionNames[i])).ToArray();
            var hosts = HostIds.Select((id, i) => Require<BlueprintBuff>(cache, id, HostNames[i])).ToArray();
            var actions = hosts.Select(ValidateHost).ToArray();
            var shares = hosts.Skip(3).Select(ValidateSharing).ToArray();
            var lethalStance = Require<BlueprintFeature>(cache, LethalStanceFeatureId, "LethalStanceFeature");
            var stanceActions = ValidateStance(Require<BlueprintBuff>(cache, LethalStanceEffectId, "LethalStanceEffectBuff"));

            var features = new BlueprintFeature[Tokens.Length];
            var buffs = new BlueprintBuff[Tokens.Length];
            var created = new List<SimpleBlueprint>();
            // Construct and validate every blueprint before touching any shared object.
            for (int i = 0; i < Tokens.Length; i++) {
                var f = new BlueprintFeature {
                    name = "CotWWrathPrototype" + Tokens[i] + "RagePower", AssetGuid = BlueprintGuid.Parse(FeatureIds[i]),
                    Ranks = 1, IsClassFeature = true, Groups = new[] { FeatureGroup.RagePower },
                    ComponentsArray = Requirements(barbarian, skald, bloodrager, primalist, i == 1 ? 12 : 1)
                };
                var b = new BlueprintBuff {
                    name = "CotWWrathPrototype" + Tokens[i] + "RageEffect", AssetGuid = BlueprintGuid.Parse(BuffIds[i]),
                    // Separate parent-owned instances survive overlapping rage/song sources.
                    // Their native bonuses/resistances do not add together.
                    Stacking = StackingType.Stack, ComponentsArray = Effects(i, barbarian, skald, bloodrager, primalist)
                };
                Set(b, "m_Flags", Enum.ToObject(AccessTools.Field(typeof(BlueprintBuff), "m_Flags").FieldType, 2));
                foreach (var bp in new BlueprintUnitFact[] { f, b }) {
                    Set(bp, "m_DisplayName", new LocalizedString { Key = TextKey(i, "name") });
                    Set(bp, "m_Description", new LocalizedString { Key = TextKey(i, "description") });
                    Set(bp, "m_Icon", icon.Icon);
                    bp.OnEnable();
                }
                features[i] = ExistingOrNew(cache, f, created);
                buffs[i] = ExistingOrNew(cache, b, created);
            }
            var sharpened = new BlueprintFeature {
                name = "CotWWrathPrototypeSharpenedAccuracyRagePower", AssetGuid = BlueprintGuid.Parse(SharpenedFeatureId),
                Ranks = 1, IsClassFeature = true, Groups = new[] { FeatureGroup.RagePower },
                ComponentsArray = Requirements(barbarian, skald, bloodrager, primalist, 8).Concat(new BlueprintComponent[] { StancePrerequisite(lethalStance) }).ToArray()
            };
            var sharpenedBuff = new BlueprintBuff {
                name = "CotWWrathPrototypeSharpenedAccuracyRageEffect", AssetGuid = BlueprintGuid.Parse(SharpenedBuffId),
                Stacking = StackingType.Stack, ComponentsArray = new BlueprintComponent[] {
                    new RecalculateConcealment { name = "SharpenedAccuracyConcealment", IgnorePartial = true, TreatTotalAsPartial = true }
                }
            };
            Set(sharpenedBuff, "m_Flags", Enum.ToObject(AccessTools.Field(typeof(BlueprintBuff), "m_Flags").FieldType, 2));
            foreach (var bp in new BlueprintUnitFact[] { sharpened, sharpenedBuff }) {
                Set(bp, "m_DisplayName", new LocalizedString { Key = "cotw-wrath-prototype.rage.SharpenedAccuracy.name" });
                Set(bp, "m_Description", new LocalizedString { Key = "cotw-wrath-prototype.rage.SharpenedAccuracy.description" });
                Set(bp, "m_Icon", lethalStance.Icon);
                bp.OnEnable();
            }
            sharpened = ExistingOrNew(cache, sharpened, created);
            sharpenedBuff = ExistingOrNew(cache, sharpenedBuff, created);

            var changes = new List<Change>();
            foreach (var pool in pools)
                foreach (var field in new[] { "m_Features", "m_AllFeatures" }) {
                    var previous = (BlueprintFeatureReference[])Get(pool, field);
                    var updated = previous ?? Array.Empty<BlueprintFeatureReference>();
                    foreach (var feature in features.Concat(new[] { sharpened })) updated = Append(updated, Ref<BlueprintFeatureReference>(feature));
                    changes.Add(new Change(pool, field, previous, updated));
                }
            for (int h = 0; h < hosts.Length; h++) {
                var previous = actions[h].Activated;
                var updated = previous.Actions ?? Array.Empty<GameAction>();
                for (int i = 0; i < Tokens.Length; i++) {
                    var expected = Apply(features[i], buffs[i], h >= 3, "CotWWrathPrototypeRagePower_" + Tokens[i]);
                    var present = updated.Where(a => a != null && a.name == expected.name).ToArray();
                    if (present.Length > 1 || (present.Length == 1 && ActionSignature(present[0]) != ActionSignature(expected)))
                        throw new InvalidOperationException("Existing rage power action has an unexpected layout: " + hosts[h].name + "/" + expected.name);
                    if (present.Length == 0) updated = updated.Concat(new GameAction[] { expected }).ToArray();
                }
                changes.Add(new Change(actions[h], "Activated", previous, new ActionList { Actions = updated }));
            }
            foreach (var share in shares) {
                if (share.FeatureFromSelection) continue; // Verified native selection already receives the new options.
                var previous = (BlueprintUnitFactReference[])Get(share, "m_Facts");
                var updated = previous ?? Array.Empty<BlueprintUnitFactReference>();
                foreach (var feature in features.Concat(new[] { sharpened })) updated = Append(updated, Ref<BlueprintUnitFactReference>(feature));
                changes.Add(new Change(share, "m_Facts", previous, updated));
            }
            var stancePrevious = stanceActions.Activated;
            var stanceUpdated = stancePrevious.Actions ?? Array.Empty<GameAction>();
            var stanceExpected = Apply(sharpened, sharpenedBuff, false, "CotWWrathPrototypeRagePower_SharpenedAccuracy", false);
            var stancePresent = stanceUpdated.Where(a => a != null && a.name == stanceExpected.name).ToArray();
            if (stancePresent.Length > 1 || (stancePresent.Length == 1 && ActionSignature(stancePresent[0]) != ActionSignature(stanceExpected)))
                throw new InvalidOperationException("Existing sharpened accuracy action has an unexpected layout.");
            if (stancePresent.Length == 0) stanceUpdated = stanceUpdated.Concat(new GameAction[] { stanceExpected }).ToArray();
            changes.Add(new Change(stanceActions, "Activated", stancePrevious, new ActionList { Actions = stanceUpdated }));
            var added = new List<SimpleBlueprint>();
            try {
                foreach (var bp in created) { cache.AddCachedBlueprint(bp.AssetGuid, bp); added.Add(bp); }
                foreach (var change in changes) change.Apply();
            } catch {
                foreach (var change in changes.AsEnumerable().Reverse()) change.Restore();
                foreach (var bp in added) cache.RemoveCachedBlueprint(bp.AssetGuid);
                throw;
            }
        }

        private static BlueprintComponent[] Requirements(BlueprintCharacterClass barbarian, BlueprintCharacterClass skald,
            BlueprintCharacterClass bloodrager, BlueprintArchetype primalist, int level)
        {
            var result = new List<BlueprintComponent>();
            foreach (var cls in new[] { barbarian, skald }) {
                var p = new PrerequisiteClassLevel { name = "RagePowerRequires" + cls.name, Level = level, Group = Prerequisite.GroupType.Any };
                Set(p, "m_CharacterClass", Ref<BlueprintCharacterClassReference>(cls)); result.Add(p);
            }
            var archetype = new PrerequisiteArchetypeLevel { name = "RagePowerRequiresPrimalist", Level = level, Group = Prerequisite.GroupType.Any };
            Set(archetype, "m_CharacterClass", Ref<BlueprintCharacterClassReference>(bloodrager));
            Set(archetype, "m_Archetype", Ref<BlueprintArchetypeReference>(primalist)); result.Add(archetype);
            return result.ToArray();
        }

        private static BlueprintComponent[] Effects(int index, BlueprintCharacterClass barbarian, BlueprintCharacterClass skald,
            BlueprintCharacterClass bloodrager, BlueprintArchetype primalist)
        {
            if (index == 0) return new BlueprintComponent[] {
                new AddStatBonus { name = "QuickReflexesOneOpportunity", Stat = StatType.AttackOfOpportunityCount, Value = 1, Descriptor = ModifierDescriptor.Rage }
            };
            if (index == 1) return new BlueprintComponent[] {
                new AddConditionImmunity { name = "UnrestrainedRageParalysisCondition", Condition = UnitCondition.Paralyzed },
                new BuffDescriptorImmunity { name = "UnrestrainedRageParalysisDescriptor", Descriptor = SpellDescriptor.Paralysis }
            };
            var rank = new ContextRankConfig { name = "RagePowerResistanceLevel" };
            Set(rank, "m_Type", AbilityRankType.Default);
            Set(rank, "m_BaseValueType", ContextRankBaseValueType.SummClassLevelWithArchetype);
            Set(rank, "m_Progression", ContextRankProgression.Div2);
            Set(rank, "m_UseMin", true); Set(rank, "m_Min", 1);
            Set(rank, "m_DisableRankBonus", true);
            Set(rank, "m_Class", new[] { Ref<BlueprintCharacterClassReference>(barbarian), Ref<BlueprintCharacterClassReference>(skald), Ref<BlueprintCharacterClassReference>(bloodrager) });
            Set(rank, "Archetype", Ref<BlueprintArchetypeReference>(primalist));
            Set(rank, "m_AdditionalArchetypes", Array.Empty<BlueprintArchetypeReference>());
            Set(rank, "m_FeatureList", Array.Empty<BlueprintFeatureReference>());
            return new BlueprintComponent[] { rank, new AddDamageResistanceEnergy {
                name = "RagePowerEnergyResistance", Type = Energies[index - 2],
                Value = new ContextValue { ValueType = ContextValueType.Rank, ValueRank = AbilityRankType.Default }
            } };
        }

        private static PrerequisiteFeature StancePrerequisite(BlueprintFeature lethal)
        {
            var prerequisite = new PrerequisiteFeature { name = "RagePowerRequiresLethalStance", Group = Prerequisite.GroupType.All };
            Set(prerequisite, "m_Feature", Ref<BlueprintFeatureReference>(lethal));
            return prerequisite;
        }

        private static Conditional Apply(BlueprintFeature feature, BlueprintBuff buff, bool caster, string name, bool notDispelable = true)
        {
            Condition condition = caster ? (Condition)new ContextConditionCasterHasFact() : new ContextConditionHasFact();
            condition.name = name + "_Known";
            Set(condition, "m_Fact", Ref<BlueprintUnitFactReference>(feature));
            var apply = new ContextActionApplyBuff { name = name + "_Apply", Permanent = true, AsChild = true, IsNotDispelable = notDispelable };
            Set(apply, "m_Buff", Ref<BlueprintBuffReference>(buff));
            return new Conditional {
                name = name, ConditionsChecker = new ConditionsChecker { Operation = Operation.And, Conditions = new[] { condition } },
                IfTrue = new ActionList { Actions = new GameAction[] { apply } }, IfFalse = new ActionList { Actions = Array.Empty<GameAction>() }
            };
        }

        private static AddFactContextActions ValidateHost(BlueprintBuff buff)
        {
            var matches = (buff.ComponentsArray ?? Array.Empty<BlueprintComponent>()).OfType<AddFactContextActions>().ToArray();
            // Inspired Rage has a separate cleanup action component. Select the
            // unique component containing the native Fearless Rage anchor.
            var anchors = matches.SelectMany(component => (component.Activated?.Actions ?? Array.Empty<GameAction>())
                .OfType<Conditional>().Where(c =>
                c.ConditionsChecker?.Conditions?.Length == 1 && !c.ConditionsChecker.Conditions[0].Not &&
                (c.ConditionsChecker.Conditions[0] is ContextConditionHasFact || c.ConditionsChecker.Conditions[0] is ContextConditionCasterHasFact) &&
                Id(Get(c.ConditionsChecker.Conditions[0], "m_Fact")) == FearlessId &&
                c.IfTrue?.Actions?.Length == 1 && c.IfTrue.Actions[0] is ContextActionApplyBuff a &&
                Id(Get(a, "m_Buff")) == FearlessBuffId && a.AsChild && a.Permanent && !a.ToCaster)
                .Select(c => component)).ToArray();
            if (anchors.Length != 1) throw new InvalidOperationException("Native Fearless Rage child-buff anchor differs: " + buff.name);
            return anchors[0];
        }

        private static AddFactContextActions ValidateStance(BlueprintBuff buff)
        {
            var matches = (buff.ComponentsArray ?? Array.Empty<BlueprintComponent>()).OfType<AddFactContextActions>().ToArray();
            var anchors = matches.SelectMany(component => (component.Activated?.Actions ?? Array.Empty<GameAction>())
                .OfType<Conditional>().Where(c =>
                c.ConditionsChecker?.Conditions?.Length == 1 && !c.ConditionsChecker.Conditions[0].Not &&
                c.ConditionsChecker.Conditions[0] is ContextConditionHasFact &&
                Id(Get(c.ConditionsChecker.Conditions[0], "m_Fact")) == DeadlyAccuracyId &&
                c.IfTrue?.Actions?.Length == 1 && c.IfTrue.Actions[0] is ContextActionApplyBuff a &&
                Id(Get(a, "m_Buff")) == DeadlyAccuracyBuffId && a.AsChild && a.Permanent && !a.ToCaster)
                .Select(c => component)).ToArray();
            if (anchors.Length != 1) throw new InvalidOperationException("Native Deadly Accuracy stance anchor differs: " + buff.name);
            return anchors[0];
        }

        private static AddFactsFromCaster ValidateSharing(BlueprintBuff buff)
        {
            var shares = buff.ComponentsArray.OfType<AddFactsFromCaster>().ToArray();
            if (shares.Length != 1) throw new InvalidOperationException("Skald sharing component is missing or ambiguous: " + buff.name);
            var share = shares[0];
            if (share.FeatureFromSelection) {
                var selection = Id(Get(share, "m_Selection"));
                if (selection != SelectionIds[0] && selection != SelectionIds[1]) throw new InvalidOperationException("Unexpected Skald shared selection.");
            } else if (!((BlueprintUnitFactReference[])Get(share, "m_Facts") ?? Array.Empty<BlueprintUnitFactReference>()).Any(r => Id(r) == FearlessId))
                throw new InvalidOperationException("Skald shared feature list differs from the native layout.");
            return share;
        }

        private static T ExistingOrNew<T>(BlueprintsCache cache, T expected, List<SimpleBlueprint> created) where T : BlueprintUnitFact
        {
            var existing = cache.Load(expected.AssetGuid);
            if (existing == null) { created.Add(expected); return expected; }
            if (existing.GetType() != expected.GetType() || existing.name != expected.name || Signature((BlueprintUnitFact)existing) != Signature(expected))
                throw new InvalidOperationException("Rage power identity/layout collision; foreign asset preserved: " + expected.AssetGuid);
            return (T)existing;
        }

        private static string Signature(BlueprintUnitFact bp)
        {
            var feature = bp as BlueprintFeature; var buff = bp as BlueprintBuff;
            var header = feature != null ? feature.Ranks + "/" + feature.IsClassFeature + "/" + string.Join(",", feature.Groups)
                : buff.Stacking + "/" + buff.IsHiddenInUI;
            return header + "/" + string.Join(";", (bp.ComponentsArray ?? Array.Empty<BlueprintComponent>()).Select(c => {
                string s = c.GetType().FullName + "/" + c.name;
                if (c is PrerequisiteClassLevel p) return s + "/" + p.Group + "/" + p.Level + "/" + p.Not + "/" + Id(Get(p, "m_CharacterClass"));
                if (c is PrerequisiteArchetypeLevel a) return s + "/" + a.Group + "/" + a.Level + "/" + Id(Get(a, "m_CharacterClass")) + "/" + Id(Get(a, "m_Archetype"));
                if (c is PrerequisiteFeature required) return s + "/" + required.Group + "/" + Id(Get(required, "m_Feature"));
                if (c is RecalculateConcealment concealment) return s + "/" + concealment.IgnorePartial + "/" + concealment.TreatTotalAsPartial;
                if (c is AddStatBonus stat) return s + "/" + stat.Stat + "/" + stat.Value + "/" + stat.Descriptor + "/" + stat.ScaleByBasicAttackBonus;
                if (c is AddConditionImmunity immune) return s + "/" + immune.Condition;
                if (c is BuffDescriptorImmunity descriptor) return s + "/" + descriptor.Descriptor + "/" + descriptor.CheckFact + "/" + Id(Get(descriptor, "m_IgnoreFeature")) + "/" + Id(Get(descriptor, "m_FactToCheck"));
                if (c is AddDamageResistanceEnergy energy) return s + "/" + energy.Type + "/" + energy.Value.ValueType + "/" + energy.Value.ValueRank + "/" + energy.UsePool + "/" + energy.UseValueMultiplier + "/" + energy.HealOnDamage;
                if (c is ContextRankConfig rank) return s + "/" + string.Join("/", new[] { "m_Type", "m_BaseValueType", "m_Progression", "m_UseMin", "m_Min", "m_UseMax", "m_Max", "m_ExceptClasses", "m_DisableRankBonus" }.Select(f => Get(rank, f)))
                    + "/" + string.Join(",", ((BlueprintCharacterClassReference[])Get(rank, "m_Class")).Select(Id)) + "/" + Id(Get(rank, "Archetype"))
                    + "/" + string.Join(",", ((BlueprintArchetypeReference[])Get(rank, "m_AdditionalArchetypes")).Select(Id))
                    + "/" + string.Join(",", ((BlueprintFeatureReference[])Get(rank, "m_FeatureList")).Select(Id));
                throw new InvalidOperationException("Unexpected component in rage power identity: " + c.GetType().FullName);
            }));
        }

        private static string ActionSignature(GameAction action)
        {
            var c = action as Conditional;
            if (c?.ConditionsChecker?.Conditions?.Length != 1 || c.IfTrue?.Actions?.Length != 1 || c.IfFalse?.Actions?.Length != 0) return "invalid";
            var condition = c.ConditionsChecker.Conditions[0];
            if (!(condition is ContextConditionHasFact) && !(condition is ContextConditionCasterHasFact)) return "invalid";
            var a = c.IfTrue.Actions[0] as ContextActionApplyBuff;
            return a == null ? "invalid" : c.ConditionsChecker.Operation + "/" + condition.GetType().FullName + "/" + condition.Not + "/" + Id(Get(condition, "m_Fact"))
                + "/" + Id(Get(a, "m_Buff")) + "/" + a.Permanent + "/" + a.AsChild + "/" + a.ToCaster + "/" + a.IsFromSpell + "/" + a.IsNotDispelable
                + "/" + a.IgnoreParentContext + "/" + a.NotLinkToAreaEffect + "/" + a.SameDuration + "/" + a.UseDurationSeconds;
        }

        internal static string TextKey(int index, string suffix) => "cotw-wrath-prototype.rage." + Tokens[index] + "." + suffix;
        private static string Id(object value) => (value as BlueprintReferenceBase)?.Guid.ToString() ?? "";
        private static T Ref<T>(SimpleBlueprint bp) where T : BlueprintReferenceBase, new() => BlueprintReferenceBase.CreateTyped<T>(bp);
        private static T[] Append<T>(T[] values, T reference) where T : BlueprintReferenceBase
        {
            var count = values.Count(r => r != null && r.Guid == reference.Guid);
            if (count > 1) throw new InvalidOperationException("Duplicate rage power references in a native list: " + reference.Guid);
            return count == 1 ? values : values.Concat(new[] { reference }).ToArray();
        }
        private static T Require<T>(BlueprintsCache cache, string id, string name) where T : SimpleBlueprint
        {
            var bp = cache.Load(BlueprintGuid.Parse(id));
            if (!(bp is T) || bp.name != name) throw new InvalidOperationException("Missing or changed rage dependency: " + name + "/" + id);
            return (T)bp;
        }
        private static object Get(object target, string field) => (AccessTools.Field(target.GetType(), field) ?? throw new MissingFieldException(target.GetType().FullName, field)).GetValue(target);
        private static void Set(object target, string field, object value) => (AccessTools.Field(target.GetType(), field) ?? throw new MissingFieldException(target.GetType().FullName, field)).SetValue(target, value);
        private sealed class Change
        {
            private readonly object target, before, after; private readonly string field;
            internal Change(object target, string field, object before, object after) { this.target = target; this.field = field; this.before = before; this.after = after; }
            internal void Apply() => Set(target, field, after);
            internal void Restore() => Set(target, field, before);
        }
    }
}

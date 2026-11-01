using System;
using System.Linq;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Prerequisites;
using Kingmaker.Blueprints.Classes.Selection;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.Designers.EventConditionActionSystem.Actions;
using Kingmaker.ElementsSystem;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Enums;
using Kingmaker.RuleSystem;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.FactLogic;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components;
using Kingmaker.UnitLogic.Abilities.Components.Base;
using Kingmaker.UnitLogic.Abilities.Components.CasterCheckers;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Commands.Base;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Mechanics.Actions;
using Kingmaker.UnitLogic.Mechanics.Components;
using Kingmaker.UnitLogic.Mechanics.Conditions;
using Kingmaker.Utility;
using static CallOfTheWild.Wrath.Prototype.PsychicAssets;

namespace CallOfTheWild.Wrath.Prototype
{
    internal static class TerrifyingHowl
    {
        internal const string ModuleName = "Terrifying Howl";
        internal const string Token = "TerrifyingHowlRagePower";
        internal const string TttPrimalistId = "6baf176bc22c4a41ba421bce3fcd9497";
        internal static readonly string[] PoolIds = { "28710502f46848d48b3f0d6132817c4e", "2476514e31791394fa140f1a07941c96", "609f0e5336084442a0dafa3abd4d31c5", "0c7f01fbbe687bb4baff8195cb02fe6a" };
        internal static void Register(BlueprintsCache cache) {
            var f = new PsychicAssets(cache); var pools = PoolIds.Select(f.Native<BlueprintFeatureSelection>).Concat(OptionalPools(cache)).ToArray();
            var barbarian = f.Native<BlueprintCharacterClass>(RagePowers.BarbarianId); var bloodrager = f.Native<BlueprintCharacterClass>(RagePowers.BloodragerId);
            var skald = f.Native<BlueprintCharacterClass>(RagePowers.SkaldId);
            var primalist = f.Native<BlueprintArchetype>(RagePowers.PrimalistId);
            var display = f.Native<BlueprintAbility>("5f3126d4120b2b244a95cb2ec23d69fb");
            const string text = "Enemies must already be shaken: this howl does not inflict shaken by itself. While raging or accepting inspired rage, use a standard action to frighten shaken enemies within 30 feet for 1d4+1 rounds (Will negates; DC 10 + half your barbarian/skald/primalist level + your Strength modifier). A creature that attempts this save is immune to your howl for 24 hours, whether it succeeds or fails. Targets that are not shaken, cannot hear the howl because of silence, or still have this caster's 24-hour immunity do not attempt a save. Requires barbarian, skald or primalist level 8. Inspired rage grants this ability through the native sharing mechanism, like Renewed Vigor; the character using the howl is its caster.";
            var cooldown = f.Buff("TerrifyingHowlImmunity", "Terrifying Howl Immunity", "Immune to the same character's Terrifying Howl for 24 hours."); cooldown.Stacking = StackingType.Stack;
            var cooldownCondition = new ContextConditionHasBuffFromCaster { Not = true }; Set(cooldownCondition, "m_Buff", Ref<BlueprintBuffReference>(cooldown));
            var fear = Apply(f.Native<BlueprintBuff>("f08a7239aa961f34c8301518e71d4cdf"), 1, fromSpell: false);
            fear.IsNotDispelable = true; fear.DurationValue.DiceType = DiceType.D4; fear.DurationValue.DiceCountValue = 1;
            var immunity = Apply(cooldown, 1, DurationRate.Days, fromSpell: false); immunity.IsNotDispelable = true;
            var save = new ContextActionSavingThrow { Type = SavingThrowType.Will, Actions = Actions(Saved(Array.Empty<GameAction>(), new GameAction[] { fear }), immunity) };
            var shakenCondition = new HowlShakenTarget();
            var audibleCondition = new AudibleAbilityTargetCondition();
            var condition = new Conditional { ConditionsChecker = new ConditionsChecker { Operation = Operation.And,
                Conditions = new Condition[] { cooldownCondition, shakenCondition, new ContextConditionIsEnemy(), audibleCondition } }, IfTrue = Actions(save), IfFalse = Actions() };
            // Native Select dereferences m_Condition even when there is no filter.
            // A manually constructed component does not receive JSON defaults.
            var area = new AbilityTargetsAround(); Set(area, "m_Radius", new Feet(30)); Set(area, "m_TargetType", TargetType.Enemy);
            Set(area, "m_Condition", new ConditionsChecker { Conditions = Array.Empty<Condition>() });
            var rage = new AbilityCasterHasFacts { NeedsAll = false }; Set(rage, "m_Facts", RagePowers.HostIds.Select(id => Ref<BlueprintUnitFactReference>(f.Native<BlueprintBuff>(id))).ToArray());
            var scaling = Inquisitions01426.Parameters(f, "TerrifyingHowl", new[] { barbarian, skald, bloodrager }, StatType.Strength, primalist);
            foreach (var rank in scaling.OfType<ContextRankConfig>()) { Set(rank, "m_BaseValueType", ContextRankBaseValueType.SummClassLevelWithArchetype); Set(rank, "Archetype", Ref<BlueprintArchetypeReference>(primalist)); }
            var fx = display.GetComponents<AbilitySpawnFx>().FirstOrDefault() ?? throw new InvalidOperationException("Native Dazzling Display visual missing.");
            var summary = Text("TerrifyingHowl", "targetSummary", "Terrifying Howl ({0}): {1} enemy target(s) in range and line of sight; {2} meet the target requirements (Will DC {3}; normal immunities still apply). Skipped: {4} with this caster's 24-hour immunity, {5} not shaken, {6} silenced.");
            var noTargets = Text("TerrifyingHowl", "noTargets", "Terrifying Howl ({0}): no enemy targets in range and line of sight. No saving throws are made.");
            // Retain the 028 localization keys for compatibility. The diagnostic
            // component is no longer attached; native save/status events remain.
            summary.ShouldProcess = noTargets.ShouldProcess = true;
            var ability = f.Ability("TerrifyingHowlAbility", "Terrifying Howl", text, AbilityType.Extraordinary, UnitCommand.CommandType.Standard, AbilityRange.Personal,
                scaling.Concat(new BlueprintComponent[] { rage, new AudibleAbilityRestriction(), area, Run(condition), CopyData(fx),
                    new SpellDescriptorComponent { Descriptor = SpellDescriptor.Fear | SpellDescriptor.MindAffecting } }).ToArray());
            ability.Animation = display.Animation; ability.SpellResistance = false;
            Set(ability, "m_Icon", Get(display, "m_Icon")); ability.LocalizedDuration = Text(ability.name, "duration", "1d4+1 rounds; same caster immunity 24 hours"); ability.LocalizedSavingThrow = Text(ability.name, "save", "Will negates");
            var level = new PrerequisiteClassLevel { Level = 8, Group = Prerequisite.GroupType.Any }; Set(level, "m_CharacterClass", Ref<BlueprintCharacterClassReference>(barbarian));
            var archetype = new PrerequisiteArchetypeLevel { Level = 8, Group = Prerequisite.GroupType.Any }; Set(archetype, "m_CharacterClass", Ref<BlueprintCharacterClassReference>(bloodrager)); Set(archetype, "m_Archetype", Ref<BlueprintArchetypeReference>(primalist));
            var skaldLevel = new PrerequisiteClassLevel { Level = 8, Group = Prerequisite.GroupType.Any }; Set(skaldLevel, "m_CharacterClass", Ref<BlueprintCharacterClassReference>(skald));
            var feature = f.Feature(Token, "Terrifying Howl", text, level, skaldLevel, archetype, Facts(ability)); feature.IsClassFeature = true; feature.Groups = new[] { FeatureGroup.RagePower }; Set(feature, "m_Icon", Get(display, "m_Icon"));
            // Current Wrath explicitly offers and shares standard-action Renewed Vigor.
            // Follow that native grant/removal path, without modifying its components.
            var shares = RagePowers.HostIds.Skip(3).Select(id => {
                var components = f.Native<BlueprintBuff>(id).GetComponents<AddFactsFromCaster>().ToArray();
                if (components.Length != 1) throw new InvalidOperationException("Unexpected inspired rage sharing layout.");
                var share = components[0];
                if (share.FeatureFromSelection ? !PoolIds.Contains(share.Selection?.AssetGuid.ToString()) : !share.Facts.Any(x => x?.AssetGuid.ToString() == "aad496a7c9bee224cbd01a3cf8e42061"))
                    throw new InvalidOperationException("Inspired rage no longer follows the Renewed Vigor sharing contract.");
                return share;
            }).ToArray();
            var shareBefore = shares.Select(x => Get(x, "m_Facts")).ToArray();
            var poolBefore = pools.Select(p => new[] { Get(p, "m_Features"), Get(p, "m_AllFeatures") }).ToArray();
            var created = f.Assets.Values.Where(b => cache.Load(b.AssetGuid) == null).ToArray();
            try {
                Migration01426.Commit(f, pools, feature);
                var published = (BlueprintFeature)cache.Load(feature.AssetGuid);
                foreach (var share in shares.Where(x => !x.FeatureFromSelection)) {
                    var previous = (BlueprintUnitFactReference[])Get(share, "m_Facts") ?? Array.Empty<BlueprintUnitFactReference>();
                    if (!previous.Any(x => x.Guid == feature.AssetGuid)) Set(share, "m_Facts", previous.Concat(new[] { Ref<BlueprintUnitFactReference>(published) }).ToArray());
                }
            } catch {
                for (int i = 0; i < shares.Length; i++) Set(shares[i], "m_Facts", shareBefore[i]);
                for (int i = 0; i < pools.Length; i++) { Set(pools[i], "m_Features", poolBefore[i][0]); Set(pools[i], "m_AllFeatures", poolBefore[i][1]); }
                foreach (var bp in created) if (ReferenceEquals(cache.Load(bp.AssetGuid), bp)) cache.RemoveCachedBlueprint(bp.AssetGuid);
                throw;
            }
        }

        static BlueprintFeatureSelection[] OptionalPools(BlueprintsCache cache) {
            var provider = cache.Load(BlueprintGuid.Parse(TttPrimalistId));
            if (provider == null) return Array.Empty<BlueprintFeatureSelection>();
            if (!(provider is BlueprintFeatureSelection selection) || selection.name != "PrimalistRagePowerSelection" ||
                selection.IgnorePrerequisites || (selection.Group != FeatureGroup.RagePower && !selection.HasGroup(FeatureGroup.RagePower)))
                throw new InvalidOperationException("Unexpected TabletopTweaks Primalist selection; preserved.");
            return new[] { selection };
        }

        internal static void Optional(BlueprintsCache cache) {
            var feature = cache.Load(Id(Token)) as BlueprintFeature;
            if (feature == null) return;
            var pools = OptionalPools(cache);
            var before = pools.Select(p => new[] { Get(p, "m_Features"), Get(p, "m_AllFeatures") }).ToArray();
            try { T09CombatFeats.Append(pools, new[] { feature }); }
            catch {
                for (int i = 0; i < pools.Length; i++) { Set(pools[i], "m_Features", before[i][0]); Set(pools[i], "m_AllFeatures", before[i][1]); }
                throw;
            }
        }
    }

    [TypeId("c42b5933e5f5415f82aefaa8862c2f26")]
    public sealed class HowlShakenTarget : ContextCondition
    {
        protected override string GetConditionCaption() => "Target is shaken";
        protected override bool CheckCondition() => Target.Unit?.State.HasCondition(UnitCondition.Shaken) == true;
    }
    [TypeId("614d556b995b47ccab8e482a08d75c31")]
    public sealed class AudibleAbilityRestriction : BlueprintComponent, IAbilityCasterRestriction, IAbilityTargetRestriction
    {
        static readonly BlueprintGuid[] SilenceIds = { PsychicAssets.Id("SilenceBuff"), BlueprintGuid.Parse("e4b8a158e8aabfe41b5e0bd334d8c386"), BlueprintGuid.Parse("73e09d4c3e3a4d918988a0aae069f372") };
        internal static bool Audible(UnitEntityData unit) => unit != null && !unit.Buffs.Enumerable.Any(b => b.IsTurnedOn && !b.IsSuppressed && SilenceIds.Contains(b.Blueprint.AssetGuid));
        public bool IsCasterRestrictionPassed(UnitEntityData caster) => Audible(caster);
        public bool IsTargetRestrictionPassed(UnitEntityData caster, TargetWrapper target) => Audible(caster) && Audible(target?.Unit);
        public string GetAbilityCasterRestrictionUIText() => "Cannot speak inside silence.";
        public string GetAbilityTargetRestrictionUIText(UnitEntityData caster, TargetWrapper target) => "Caster and target must be outside silence.";
    }
    [TypeId("7b0346f702d349aba718e8165bfbd255")]
    public sealed class AudibleAbilityTargetCondition : ContextCondition
    {
        protected override string GetConditionCaption() => "Caster and listener are outside silence";
        protected override bool CheckCondition() => AudibleAbilityRestriction.Audible(Context.MaybeCaster) && AudibleAbilityRestriction.Audible(Target.Unit);
    }
}

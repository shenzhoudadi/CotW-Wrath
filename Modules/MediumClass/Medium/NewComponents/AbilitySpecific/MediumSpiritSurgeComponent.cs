using BlueprintCore.Utils;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.Designers.Mechanics.Facts;
using Kingmaker.EntitySystem;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Enums;
using Kingmaker.PubSubSystem;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using MediumClass.Medium.NewUnitParts;
using MediumClass.Prowler;
using MediumClass.Utilities;
using MediumClass.Utils;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using static UnityModManagerNet.UnityModManager.ModEntry;

namespace MediumClass.Medium.NewComponents.AbilitySpecific
{
    /// <summary>
    /// Spirit Surge / Marshal's Orders / Legendary Marshal buff logic.
    ///
    /// One buff instance rolls its dice exactly once. The roll (dice type and every individual result)
    /// lives in the per-instance runtime data of this component, which the game saves with the buff, so
    /// every stat, the concentration query, a re-run of OnTurnOn (reapply, area change, load) and a later
    /// save/load all reuse it. A new buff instance (a new cast replaces the old buff; another character
    /// gets its own buff) gets its own runtime data and therefore a new roll. Nothing about a roll is
    /// stored on the shared blueprint component or in a static field.
    /// </summary>
    [TypeId("96306dd2-f947-44d4-a6d8-1eafe7c937dc")]
    class MediumSpiritSurgeComponent : UnitFactComponentDelegate<MediumSpiritSurgeComponent.SurgeData>, IConcentrationBonusProvider, IInitiatorRulebookHandler<RuleSkillCheck>
    {
        /// <summary>Per-instance runtime data. Old saves have none; the first use then rolls once.</summary>
        public class SurgeData
        {
            [JsonProperty] public int Version;
            [JsonProperty] public int[] Sides;
            [JsonProperty] public int[] Results;
            [JsonProperty] public bool Announced;
        }

        private static readonly ModLogger Logger = Logging.GetLogger(nameof(MediumSpiritSurgeComponent));
        private const int DataVersion = 1;

        public override void OnTurnOn()
        {
            var plan = BuildPlan();
            if (plan == null) return;
            // A repeated OnTurnOn without an OnTurnOff in between (reapply, load) must not stack.
            RemoveModifiers();
            foreach (var (stat, total) in plan.StatBonuses)
                Owner.Stats.GetStat(stat)?.AddModifier(total, Runtime, ModifierDescriptor.UntypedStackable);
            Announce(plan.Report);
            // Free uses are consumed atomically by InfluenceAbilitySpendPatch.
        }

        public override void OnTurnOff() => RemoveModifiers();

        public void OnEventAboutToTrigger(RuleSkillCheck evt)
        {
            // Attribute checks use RuleSkillCheck too. Never increase the actual
            // attribute score: that would also raise skills, saves, HP and DCs.
            if (!MarshalSpiritRules.AttributeChecks.Contains(evt.StatType)) return;
            var plan = BuildPlan();
            if (plan?.AttributeChecks == null || !plan.AttributeChecks.Stats.Contains(evt.StatType)) return;
            evt.Bonus.RemoveModifiersFrom(Runtime);
            evt.Bonus.AddModifier(plan.AttributeChecks.Total, Runtime, ModifierDescriptor.UntypedStackable);
        }
        public void OnEventDidTrigger(RuleSkillCheck evt) { }

        private void RemoveModifiers()
        {
            // The catalogue survives save/load and no per-caster state is stored
            // on the shared blueprint component.
            foreach (var entry in BlueprintTool.Get<BlueprintFeature>(Guids.MediumChannelSpirit).GetComponents<MediumSpiritComponent>())
                foreach (var stat in (entry.Stats ?? Array.Empty<StatType>()).Concat(MarshalSpiritRules.SurgeStats).Distinct())
                    Owner.Stats.GetStat(stat)?.RemoveModifiersFrom(Runtime);
        }

        public int GetStaticConcentrationBonus(EntityFactComponent runtime)
        {
            using (runtime.RequestEventContext())
            {
                try
                {
                    return BuildPlan()?.Concentration?.Total ?? 0;
                }
                catch (Exception e)
                {
                    // Runtime data is unavailable once the fact is disposed; a concentration query must not throw.
                    Logger.LogException("Spirit Surge concentration bonus unavailable", e);
                    return 0;
                }
            }
        }

        /// <summary>The single calculation behind both the modifiers and the log line.</summary>
        private sealed class Plan
        {
            internal readonly List<(StatType Stat, int Total)> StatBonuses = new List<(StatType, int)>();
            internal SpiritSurgeReport.Line Concentration;
            internal SpiritSurgeReport.Line AttributeChecks;
            internal SpiritSurgeReport Report;
        }

        private Plan BuildPlan()
        {
            var caster = Context?.MaybeCaster;
            var medium = caster?.Get<UnitPartMedium>();
            if (medium == null) return null;
            var marshal = BlueprintTool.GetRef<BlueprintCharacterClassReference>(Guids.Marshal);
            bool legendary = IsLegendary();
            bool alliedOrder = Owner != caster && Context.SourceAbility == BlueprintTool.Get<BlueprintAbility>(Guids.MarshalMarshalsOrdersAbility);
            var classes = legendary || alliedOrder ? new[] { marshal } : medium.ActiveSpiritClasses.ToArray();
            var validClasses = classes.Where(medium.Spirits.ContainsKey).ToArray();
            var entries = validClasses.Select(s => medium.Spirits[s]).ToArray();
            var stats = validClasses.SelectMany(s => s == marshal ? MarshalSpiritRules.SurgeStats
                : medium.Spirits[s].SpiritBonus.Stats ?? Array.Empty<StatType>()).Distinct().ToArray();
            bool concentration = validClasses.Contains(marshal) || entries.Any(e => e.SpiritBonus.Concentration);
            if (stats.Length == 0 && !concentration) return null;

            (int Rank, int Focus) marshalTerm = legendary ? (0, 0) : GrantingMarshalBonus(caster, medium, marshal);
            int extra = marshalTerm.Rank + marshalTerm.Focus;
            var roll = EnsureRoll(caster);

            var plan = new Plan { Report = new SpiritSurgeReport(roll) };
            foreach (StatType stat in stats)
                plan.StatBonuses.Add((stat, roll.Total + extra));

            // Stats that receive the same terms share one log line; the final bonus is the one applied above.
            var plain = extra > 0 ? Array.Empty<StatType>() : stats;
            var boosted = extra > 0 ? stats : Array.Empty<StatType>();
            if (plain.Length > 0)
                plan.Report.Lines.Add(new SpiritSurgeReport.Line { Stats = plain, Total = roll.Total });
            if (boosted.Length > 0)
                plan.Report.Lines.Add(MarshalLine(caster, marshalTerm, roll, boosted, false));
            if (validClasses.Contains(marshal)) {
                plan.AttributeChecks = extra > 0 ? MarshalLine(caster, marshalTerm, roll, MarshalSpiritRules.AttributeChecks, false)
                    : new SpiritSurgeReport.Line { Stats = MarshalSpiritRules.AttributeChecks, Total = roll.Total };
                plan.Report.Lines.Add(plan.AttributeChecks);
            }
            if (concentration)
            {
                plan.Concentration = extra > 0 ? MarshalLine(caster, marshalTerm, roll, Array.Empty<StatType>(), true)
                    : new SpiritSurgeReport.Line { IsConcentration = true, Total = roll.Total };
                plan.Report.Lines.Add(plan.Concentration);
            }
            return plan;
        }

        private static SpiritSurgeReport.Line MarshalLine(UnitEntityData caster, (int Rank, int Focus) term,
            SurgeRoll roll, StatType[] stats, bool isConcentration) =>
            new SpiritSurgeReport.Line
            {
                Stats = stats,
                IsConcentration = isConcentration,
                MarshalOwner = caster.CharacterName,
                MarshalSpiritBonus = term.Rank,
                MarshalFocus = term.Focus,
                Total = roll.Total + term.Rank + term.Focus,
            };

        // Legendary Marshal lends only its fixed die. Otherwise the Marshal term
        // always belongs to the granting medium: Orders on an ally carries the
        // caster's own Marshal rank and focus, never the ally's.
        private static (int Rank, int Focus) GrantingMarshalBonus(UnitEntityData caster, UnitPartMedium medium,
            BlueprintCharacterClassReference marshal)
        {
            if (!medium.IsActiveSpirit(marshal) || !medium.Spirits.TryGetValue(marshal, out var entry)) return (0, 0);
            return (caster.Progression.Features.GetRank(entry.SpiritBonus.SpiritBonusFeature.Get()),
                MultiSpiritRules.EffectiveSpiritFocus(medium, marshal));
        }

        private bool IsLegendary() =>
            Context.SourceAbility == BlueprintTool.Get<BlueprintAbility>(Guids.MarshalLegendaryMarshalAbility);

        /// <summary>The instance's roll: read from the saved data, or made now, once.</summary>
        private SurgeRoll EnsureRoll(UnitEntityData caster)
        {
            var data = Data;
            if (data.Version == DataVersion && SurgeRoll.IsValid(data.Sides, data.Results))
                return new SurgeRoll(data.Sides, data.Results);
            var roll = SpiritSurgeMath.RollDice(ProwlerSpiritRules.SpiritClassLevel(caster),
                ProwlerSpiritRules.IsProwler(caster?.Descriptor),
                HardenedSoul.HasFeature(caster?.Descriptor),
                IsLegendary(),
                sides => rnd.Next(1, sides + 1));
            data.Version = DataVersion;
            data.Sides = roll.Sides;
            data.Results = roll.Results;
            data.Announced = false;
            return roll;
        }

        // The line is shown once per instance, not again after a reapply, area change or load.
        // CombatLog swallows and reports its own failures, so the effect above is never affected.
        private void Announce(SpiritSurgeReport report)
        {
            var data = Data;
            if (data.Announced) return;
            data.Announced = true;
            try
            {
                CombatLog.SpiritSurge(Owner, report);
            }
            catch
            {
                // The log is UI only and reports its own failures; whatever it throws, the bonus stays.
            }
        }

        private static readonly Random rnd = new Random();
    }
}

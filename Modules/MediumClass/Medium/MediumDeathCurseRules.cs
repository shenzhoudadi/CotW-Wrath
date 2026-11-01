using System;
using System.Collections.Generic;
using System.Linq;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Facts;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.Enums.Damage;
using Kingmaker.UnitLogic.Buffs;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using MediumClass.Utils;

namespace MediumClass.Medium
{
    /// <summary>
    /// Adriel's Death Curse allows at most <see cref="MaxActive"/> energy toggles at once.
    /// One rule set serves every entry that can create or use a toggle:
    /// the toggle action (the real execution entry), the UI restriction, the damage
    /// component and the load-time normalisation of 0.3.3 saves that hold 3-5 toggles.
    ///
    /// A "toggle" is an active buff whose blueprint carries a Death Curse damage component (<see cref="IDeathCurseToggle"/>)
    /// with at least one energy. The 0.3.0/0.3.1 paired buffs carry none and are neither counted
    /// nor kept (MediumDeathCurseMigration removes them).
    ///
    /// Order policy (deterministic, never random): toggles are ordered as they appear in the
    /// unit's buff list, which is the order they were switched on and is preserved by save/load.
    /// The first <see cref="MaxActive"/> stay, later ones are surplus.
    /// </summary>
    /// <summary>Implemented by the damage component carried by each energy toggle buff.</summary>
    internal interface IDeathCurseToggle
    {
        DamageEnergyType[] ToggleEnergies { get; }
    }

    internal static class MediumDeathCurseRules
    {
        internal const int MaxActive = 2;

        internal enum ToggleOutcome { SwitchedOn, SwitchedOff, LimitReached, Ignored }

        internal static IReadOnlyList<DamageEnergyType> EnergiesOf(BlueprintBuff blueprint) =>
            blueprint == null
                ? Array.Empty<DamageEnergyType>()
                : blueprint.GetComponents<IDeathCurseToggle>()
                    .SelectMany(c => c.ToggleEnergies ?? Array.Empty<DamageEnergyType>()).Distinct().ToArray();

        internal static bool IsToggle(BlueprintBuff blueprint) => EnergiesOf(blueprint).Count > 0;

        /// <summary>Active toggles in switch-on order.</summary>
        internal static IReadOnlyList<Buff> ActiveToggles(UnitEntityData unit) =>
            unit == null
                ? Array.Empty<Buff>()
                : unit.Buffs.Enumerable.Where(b => b.IsActive && IsToggle(b.Blueprint)).ToArray();

        /// <summary>Energies that may currently deal damage: those of the first two active toggles.</summary>
        internal static bool IsEnergyEffective(UnitEntityData unit, DamageEnergyType energy) =>
            ActiveToggles(unit).Take(MaxActive).Any(b => EnergiesOf(b.Blueprint).Contains(energy));

        /// <summary>Whether the toggle for <paramref name="blueprint"/> may be used now. Switching off is always allowed.</summary>
        internal static bool CanToggle(UnitEntityData unit, BlueprintBuff blueprint)
        {
            if (unit == null || !IsToggle(blueprint)) return false;
            var active = ActiveToggles(unit);
            return active.Any(b => b.Blueprint == blueprint) || active.Count < MaxActive;
        }

        /// <summary>
        /// Executes one toggle. Decision and change happen in the same call on the live buff list, so a
        /// second command run after the first sees the first one's result.
        /// </summary>
        internal static ToggleOutcome Toggle(UnitEntityData unit, BlueprintBuff blueprint, Action switchOn, Action<Buff> switchOff)
        {
            if (unit == null || !IsToggle(blueprint)) return ToggleOutcome.Ignored;
            var active = ActiveToggles(unit);
            var existing = active.FirstOrDefault(b => b.Blueprint == blueprint);
            if (existing != null)
            {
                switchOff(existing);
                return ToggleOutcome.SwitchedOff;
            }
            if (active.Count >= MaxActive) return ToggleOutcome.LimitReached;
            switchOn();
            return ToggleOutcome.SwitchedOn;
        }

        /// <summary>Removes toggles beyond the limit (old saves, other mods). Idempotent; returns the number removed.</summary>
        internal static int Normalize(UnitEntityData unit, Action<Buff> remove)
        {
            var surplus = ActiveToggles(unit).Skip(MaxActive).ToArray();
            foreach (var buff in surplus) remove(buff);
            return surplus.Length;
        }

        internal static string LimitMessage() => LocalizationText.Get(
            "Death Curse: only two energy types can be active at once. Turn one off first.",
            "恶咒：最多同时开启两种能量，请先关闭其中一种。");
    }
}

using Kingmaker.Blueprints;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.Enums.Damage;
using Kingmaker.RuleSystem.Rules.Damage;
using Kingmaker.UnitLogic.Buffs;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Mechanics;
using MediumClass.Medium;
using MediumClass.Medium.NewActions;
using MediumClass.Medium.NewComponents;
using MediumClass.Medium.NewComponents.AbilitySpecific;
using MediumClass.Medium.NewUnitParts;
using MediumClass.Utilities;
using BlueprintCore.Utils;
using Kingmaker.EntitySystem;
using Kingmaker.PubSubSystem;

// 0.3.4: Adriel's Death Curse allows at most two energies at once. Runs the production rules, toggle action,
// UI restriction, damage component and load-time normalisation on the explicit API doubles of this suite.
internal static class DeathCurseLimitChecks
{
    private static readonly (string Name, string Buff, DamageEnergyType Energy)[] Energies =
    {
        ("Negative", Guids.MediumAdrielDeathCurseBuffNegative, DamageEnergyType.NegativeEnergy),
        ("Fire", Guids.MediumAdrielDeathCurseBuffFire, DamageEnergyType.Fire),
        ("Cold", Guids.MediumAdrielDeathCurseBuffCold, DamageEnergyType.Cold),
        ("Electricity", Guids.MediumAdrielDeathCurseBuffElectricity, DamageEnergyType.Electricity),
        ("Acid", Guids.MediumAdrielDeathCurseBuffAcid, DamageEnergyType.Acid),
    };

    internal static int Run(Action<bool, string> check, BlueprintCharacterClassReference adrielRef, BlueprintBuff challengeTarget)
    {
        int count = 0;
        void Check(bool ok, string label) { count++; check(ok, label); }

        // Blueprints as the mod builds them: one buff per energy whose component carries that energy; the ten
        // paired 0.3.0/0.3.1 buffs carry none (hidden compatibility blueprints).
        var blueprints = new Dictionary<DamageEnergyType, BlueprintBuff>();
        foreach (var (name, guid, energy) in Energies)
        {
            var blueprint = new BlueprintBuff { Name = "Curse" + name };
            blueprint.Components.Add(new MediumDeathCurseComponent { Energies = new[] { energy } });
            BlueprintTool.Assets[guid] = blueprint;
            blueprints[energy] = blueprint;
        }
        foreach (var legacy in MediumDeathCurseMigration.LegacyBuffGuids)
            BlueprintTool.Assets[legacy] = new BlueprintBuff { Name = legacy, Components = new() { new MediumDeathCurseComponent() } };
        var unrelated = new BlueprintBuff { Name = "Unrelated" };

        UnitEntityData NewAdriel(out UnitEntityData enemy)
        {
            var unit = new UnitEntityData();
            var part = unit.Ensure<UnitPartMedium>();
            part.PrimarySpirit = adrielRef;
            part.Spirits[adrielRef] = new SpiritEntry();
            enemy = new UnitEntityData();
            enemy.Buffs.Enumerable.Add(new Buff { Blueprint = challengeTarget, Context = new MechanicsContext { MaybeCaster = unit } });
            return unit;
        }
        ContextActionToggleDeathCurse Action(UnitEntityData caster, DamageEnergyType energy) =>
            new() { Context = new MechanicsContext { MaybeCaster = caster }, m_Buff = new BlueprintBuffReference { Blueprint = blueprints[energy] } };
        bool UiAllows(UnitEntityData caster, DamageEnergyType energy) =>
            new AbilityRequirementDeathCurseSlot { m_Buff = new BlueprintBuffReference { Blueprint = blueprints[energy] } }
                .IsAbilityRestrictionPassed(new AbilityData { Caster = caster.Descriptor });
        DamageEnergyType[] Active(UnitEntityData unit) => MediumDeathCurseRules.ActiveToggles(unit)
            .Select(b => MediumDeathCurseRules.EnergiesOf(b.Blueprint).Single()).ToArray();
        // One weapon hit on the challenged target: every active toggle's damage component gets the event.
        DamageEnergyType[] HitDice(UnitEntityData unit, UnitEntityData enemy)
        {
            var hit = new RulePrepareDamage { DamageBundle = new DamageBundle { Weapon = new object() }, Target = enemy };
            foreach (var buff in unit.Buffs.Enumerable.Where(b => b.IsActive).ToArray())
                foreach (var component in buff.Blueprint.GetComponents<MediumDeathCurseComponent>())
                {
                    var runtime = new MediumDeathCurseComponent { Owner = unit, Energies = component.Energies };
                    runtime.OnEventAboutToTrigger(hit);
                }
            return hit.Added.Select(d => d.Energy).ToArray();
        }
        void Add(UnitEntityData unit, DamageEnergyType energy) => unit.Buffs.Enumerable.Add(new Buff { Blueprint = blueprints[energy] });

        var all = Energies.Select(e => e.Energy).ToArray();
        EventBus.Handler = new EventBus.Warnings();

        // ---- 1. every pair of the five energies can be switched on (negative energy counts like the others) --------------------
        int pairs = 0;
        for (int i = 0; i < all.Length; i++)
        for (int j = i + 1; j < all.Length; j++)
        {
            pairs++;
            var unit = NewAdriel(out var enemy);
            EventBus.Handler = new EventBus.Warnings();
            Action(unit, all[i]).RunAction();
            Action(unit, all[j]).RunAction();
            Check(Active(unit).SequenceEqual(new[] { all[i], all[j] }), $"{all[i]} + {all[j]} both switch on");
            Check(HitDice(unit, enemy).OrderBy(e => e).SequenceEqual(new[] { all[i], all[j] }.OrderBy(e => e)), $"{all[i]} + {all[j]}: one d6 of each on a hit");
            Check(EventBus.Handler is EventBus.Warnings w0 && w0.Shown.Count == 0, "no warning while within the limit");
            // ---- 2. any third is rejected, with a warning, and nothing changes -------------------------------------------------
            foreach (var third in all.Where(e => e != all[i] && e != all[j]))
            {
                var warnings = (EventBus.Warnings)EventBus.Handler; int before = warnings.Shown.Count;
                Check(!UiAllows(unit, third), $"UI restriction blocks {third} when {all[i]} and {all[j]} are on");
                Action(unit, third).RunAction();
                Check(Active(unit).SequenceEqual(new[] { all[i], all[j] }), $"{third} is refused as a third energy ({all[i]}, {all[j]} stay)");
                Check(warnings.Shown.Count == before + 1 && warnings.Shown.Last() == MediumDeathCurseRules.LimitMessage(), "the player is told to turn one off first");
                Check(HitDice(unit, enemy).Length == 2, "a refused third energy never adds damage");
            }
            // ---- 3. an active energy can always be switched off, and then another one on ---------------------------------------
            Check(UiAllows(unit, all[i]) && UiAllows(unit, all[j]), "active energies stay clickable to switch off");
            Action(unit, all[i]).RunAction();
            Check(Active(unit).SequenceEqual(new[] { all[j] }), $"{all[i]} switches off at the limit");
            foreach (var third in all.Where(e => e != all[i] && e != all[j]).Take(1))
            {
                Check(UiAllows(unit, third), "a free slot re-enables the other buttons");
                Action(unit, third).RunAction();
                Check(Active(unit).SequenceEqual(new[] { all[j], third }), $"after switching {all[i]} off, {third} can take its place");
            }
        }
        Check(pairs == 10, "all ten pairs of five energies were exercised");

        // ---- 4. zero and one energy are fine; double click = on then off; hits with none add nothing -------------------------
        {
            var unit = NewAdriel(out var enemy);
            Check(Active(unit).Length == 0 && HitDice(unit, enemy).Length == 0, "no energy: no extra damage");
            foreach (var energy in all)
            {
                Action(unit, energy).RunAction();
                Check(Active(unit).SequenceEqual(new[] { energy }) && HitDice(unit, enemy).SequenceEqual(new[] { energy }), $"{energy} alone");
                Action(unit, energy).RunAction();
                Check(Active(unit).Length == 0, $"a second click on {energy} switches it off");
            }
        }

        // ---- 5. the limit lives in the execution entry: stale buttons and queued commands cannot exceed it -------------------------
        {
            var unit = NewAdriel(out var enemy);
            EventBus.Handler = new EventBus.Warnings();
            var stale = all.Select(e => UiAllows(unit, e)).ToArray();          // every button looked available when queued
            Check(stale.All(x => x), "with nothing active every button is available");
            foreach (var energy in all) Action(unit, energy).RunAction();       // the five queued commands run one after another
            Check(Active(unit).Length == MediumDeathCurseRules.MaxActive, "five queued switch-ons leave exactly two active");
            Check(Active(unit).SequenceEqual(new[] { all[0], all[1] }), "the first two commands win, deterministically");
            Check(((EventBus.Warnings)EventBus.Handler).Shown.Count == 3, "the other three were refused");
            Check(HitDice(unit, enemy).Length == 2, "damage is two dice");
            // The same action object executed repeatedly cannot exceed the limit either.
            var again = Action(unit, all[4]);
            for (int n = 0; n < 5; n++) again.RunAction();
            Check(Active(unit).Length <= 2, "repeated execution of one command never exceeds two");
        }

        // ---- 6. a warning that fails to display cannot undo the refusal -----------------------------------------------------------
        {
            var unit = NewAdriel(out _);
            Action(unit, all[0]).RunAction(); Action(unit, all[1]).RunAction();
            EventBus.Handler = new EventBus.Warnings { Throw = true };
            Action(unit, all[2]).RunAction();
            Check(Active(unit).Length == 2, "a failing warning display leaves the two-energy limit intact");
            EventBus.Handler = new EventBus.Warnings();
        }

        // ---- 7. damage path: even if a third toggle exists it is inert (old save before recovery, foreign code) -----------------
        {
            var unit = NewAdriel(out var enemy);
            foreach (var energy in new[] { DamageEnergyType.Acid, DamageEnergyType.Cold, DamageEnergyType.NegativeEnergy, DamageEnergyType.Fire, DamageEnergyType.Electricity })
                Add(unit, energy);
            Check(HitDice(unit, enemy).SequenceEqual(new[] { DamageEnergyType.Acid, DamageEnergyType.Cold }),
                "five switched-on toggles: only the first two in switch-on order deal damage");
        }

        // ---- 8. old 0.3.3 saves with 3-5 toggles are normalised deterministically -------------------------------------------------
        foreach (int total in new[] { 3, 4, 5 })
        {
            var order = new[] { DamageEnergyType.Cold, DamageEnergyType.NegativeEnergy, DamageEnergyType.Acid, DamageEnergyType.Electricity, DamageEnergyType.Fire }.Take(total).ToArray();
            for (int attempt = 0; attempt < 3; attempt++)
            {
                var unit = NewAdriel(out var enemy);
                var other = new Buff { Blueprint = unrelated };
                var legacy = new Buff { Blueprint = BlueprintTool.Get<BlueprintBuff>(MediumDeathCurseMigration.LegacyBuffGuids[0]) };
                unit.Buffs.Enumerable.Add(other);
                foreach (var energy in order) Add(unit, energy);
                unit.Buffs.Enumerable.Add(legacy);
                MediumDeathCurseMigration.NormalizeActiveEnergies(unit);
                Check(Active(unit).SequenceEqual(order.Take(2)), $"{total} old toggles -> the first two switched on stay ({string.Join(", ", order.Take(2))})");
                Check(unit.Buffs.Enumerable.Contains(other), "an unrelated buff is untouched");
                Check(unit.Buffs.Enumerable.Contains(legacy), "the normalisation does not touch paired legacy buffs (removed by their own cleanup)");
                Check(HitDice(unit, enemy).Length == 2, "and the hit deals two dice");
                int before = unit.Buffs.Enumerable.Count;
                MediumDeathCurseMigration.NormalizeActiveEnergies(unit);
                Check(unit.Buffs.Enumerable.Count == before && Active(unit).Length == 2, "normalisation is idempotent");
                // Saving now and loading again: the list that is saved contains two toggles, so no third one can come back.
                var loaded = NewAdriel(out _);
                foreach (var buff in unit.Buffs.Enumerable) loaded.Buffs.Enumerable.Add(new Buff { Blueprint = buff.Blueprint });
                MediumDeathCurseMigration.NormalizeActiveEnergies(loaded);
                Check(Active(loaded).SequenceEqual(order.Take(2)), "after save and load the same two remain and no third one is revived");
            }
        }
        {
            var unit = NewAdriel(out _);
            Add(unit, DamageEnergyType.Fire); Add(unit, DamageEnergyType.Cold);
            MediumDeathCurseMigration.NormalizeActiveEnergies(unit);
            Check(Active(unit).Length == 2, "two toggles are left alone");
            MediumDeathCurseMigration.NormalizeActiveEnergies(null);
            Check(true, "a missing unit is ignored");
        }

        // ---- 9. no channel, wrong target, ended seance / rest -------------------------------------------------------------------------
        {
            var unit = NewAdriel(out var enemy);
            Action(unit, all[0]).RunAction(); Action(unit, all[1]).RunAction();
            Check(HitDice(unit, new UnitEntityData()).Length == 0, "a target that is not this medium's challenge target takes no curse damage");
            unit.Get<UnitPartMedium>().PrimarySpirit = null;
            Check(HitDice(unit, enemy).Length == 0, "without an Adriel channel the toggles are inert");
            unit.Get<UnitPartMedium>().PrimarySpirit = adrielRef;
            Check(HitDice(unit, enemy).Length == 2, "and work again when Adriel is channeled");
            // Ending the seance / resting removes all five buffs (feature deactivation, RemoveOnRest): a new seance starts at zero.
            foreach (var buff in unit.Buffs.Enumerable.ToArray()) unit.Buffs.RemoveFact(buff);
            Check(Active(unit).Length == 0 && MediumDeathCurseRules.CanToggle(unit, blueprints[all[3]]), "after the seance every energy can be chosen again");
            Action(unit, all[3]).RunAction(); Action(unit, all[4]).RunAction();
            Check(Active(unit).SequenceEqual(new[] { all[3], all[4] }), "a different pair after the reset");
        }

        // ---- 10. blueprint hygiene: legacy paired buffs are never toggles; unknown buffs cannot be toggled -----------------------
        Check(MediumDeathCurseMigration.LegacyBuffGuids.All(id => !MediumDeathCurseRules.IsToggle(BlueprintTool.Get<BlueprintBuff>(id))), "legacy paired buffs are not counted as energies");
        Check(!MediumDeathCurseRules.CanToggle(new UnitEntityData(), unrelated) && MediumDeathCurseRules.Toggle(new UnitEntityData(), unrelated, () => { }, _ => { }) == MediumDeathCurseRules.ToggleOutcome.Ignored,
            "a buff that is not a Death Curse energy is ignored");
        Check(MediumDeathCurseRules.MaxActive == 2, "the limit is two");
        return count;
    }
}

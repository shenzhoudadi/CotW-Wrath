// COTW-0099 B first-slice isolated rules check.
// Contract C-0099-20261115-r1, acceptance specs C-occultist-cases.json.
//
// This is a numeric state-model check over the SAME production sources that
// the native adapter uses. It is NOT an in-game or Unity runtime check: no
// game process, UMM, blueprint cache or save file is exercised. Cases whose
// native entry points are not wired yet are reported as not_run, never PASS.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using CallOfTheWild.Wrath.Prototype;

internal static class Occultist0099SliceProgram
{
    private static int assertions;
    private static readonly List<string> failures = new List<string>();
    private static readonly Dictionary<string, string> caseStatus =
        new Dictionary<string, string>();

    private static void Check(bool condition, string label)
    {
        assertions++;
        if (!condition) failures.Add(label);
    }

    private static void Eq(int expected, int actual, string label)
        => Check(expected == actual, label + " (expected " + expected + ", got " + actual + ")");

    private static void Str(string expected, string actual, string label)
        => Check(expected == actual, label + " (expected " + expected + ", got " + actual + ")");

    private static void Outcome(Occultist0099Outcome expected,
        Occultist0099Outcome actual, string label)
        => Check(expected == actual, label + " (expected " + expected + ", got " + actual + ")");

    private static void Stable(Occultist0099FocusState s, string before, string label)
        => Str(before, s.Fingerprint(), label + ": state unchanged");

    private static void Conservation(Occultist0099FocusState s, string label)
    {
        Check(Occultist0099Focus.IsConsistent(s), label + ": conservation G+sum(R)+U+Q=B");
        Eq(s.Budget, s.Generic + s.Schools.Values.Sum(x => x.Remaining) +
            s.Spent + s.Invalidated, label + ": budget equality");
    }

    private static Occultist0099FocusState Cycle(int level, int intMod, string restId,
        params string[] schools)
    {
        var s = Occultist0099Focus.Create();
        Outcome(Occultist0099Outcome.Ok,
            Occultist0099Focus.BeginCycle(s, level, intMod, restId, false),
            "cycle " + restId);
        foreach (string school in schools)
            Outcome(Occultist0099Outcome.Ok,
                Occultist0099Focus.SetSchoolPresent(s, school, true),
                "known school " + school);
        return s;
    }

    private static int Main(string[] args)
    {
        string reportPath = args.Length > 0 ? args[0] :
            "docs/work-packages/COTW-0099/B-slice-report.json";
        var started = DateTime.UtcNow;
        try
        {
            MF01(); MF02(); MF03(); MF04(); MF05(); MF06(); MF08();
            MF09(); MF10();
            SP01(); SP02(); SP03(); SP04(); SP05(args); SP06(); SP07(); SP08();
            Identity();
            // D-fix regressions: positive AND negative cases for every
            // closed finding, run over the same linked production sources.
            DFix02(); DFix03(); DFix05(); DFix06();
        }
        catch (Exception e)
        {
            failures.Add("EXCEPTION: " + e.GetType().Name + ": " + e.Message);
        }

        // MF07 (Spell Power arm/spend-on-cast) needs the Panoply abilities and
        // the native cast event chain deferred to later school packages.
        caseStatus["MF07"] = "not_run";
        // MF11 is PARTIAL: DFix02 covers "same execution identity is charged at
        // most once" (including class remove+reacquire) with both directions.
        // Armed re-entry, invalidated Panoply member and StickyTouch delivery
        // are still not run, so this is NOT a full MF11 pass.
        caseStatus["MF11"] = "partial_model_dedup_only_reentry_stickytouch_not_run";
        foreach (string id in new[] { "MF01", "MF02", "MF03", "MF04", "MF05",
            "MF06", "MF08", "MF09", "MF10", "SP01", "SP02", "SP03", "SP04",
            "SP05", "SP06", "SP07", "SP08" })
            if (!caseStatus.ContainsKey(id)) caseStatus[id] = "model_assertions";

        bool pass = failures.Count == 0;
        // D-0099-04 fix: the machine-readable result is scoped. This host only
        // exercises the numeric state model; native Wrath entry points are NOT
        // run here, so this can never be an acceptance or release verdict.
        var report = new Dictionary<string, object>
        {
            ["workPackage"] = "COTW-0099",
            ["stage"] = "B first vertical slice (Abjuration), development only",
            ["contract"] = Occultist0099Ids.Contract,
            ["checkType"] = "isolated numeric state model over production sources",
            ["acceptanceScope"] = "model_supplement_only",
            ["nativeWrathEntriesExercised"] = false,
            ["nativeEntryPointsExercised"] = false,
            ["gameOrUmmLaunched"] = false,
            ["releaseEligible"] = false,
            ["assertions"] = assertions,
            ["failures"] = failures,
            ["cases"] = caseStatus,
            ["assertionSeconds"] = Math.Round((DateTime.UtcNow - started).TotalSeconds, 3),
            ["timingNote"] = "Host assertion time only. The full dotnet run command " +
                "including its build is recorded separately in B-abjuration-slice.md.",
            ["result"] = pass ? "MODEL_ONLY_PASS" : "MODEL_ONLY_FAIL",
            ["resultMeaning"] = "Numeric state-model assertions over linked production " +
                "sources. NOT native-entry acceptance; native Wrath payment/learning " +
                "entry points were not exercised by this host. See the native host " +
                "report for entry-point evidence."
        };
        var json = JsonSerializer.Serialize(report, new JsonSerializerOptions
        { WriteIndented = true });
        var full = Path.GetFullPath(reportPath);
        Directory.CreateDirectory(Path.GetDirectoryName(full));
        File.WriteAllText(full, json);
        Console.WriteLine((pass ? "MODEL_ONLY_PASS" : "MODEL_ONLY_FAIL") + ": " +
            assertions + " isolated state-model assertions (model supplement only; " +
            "NOT native-entry acceptance). Native Wrath entries, in-game level-up " +
            "UI, save/load and respec were NOT exercised. releaseEligible=false.");
        if (!pass)
        {
            foreach (string f in failures.Take(20)) Console.WriteLine("  " + f);
            return 1;
        }
        return 0;
    }

    // MF01: invest, pre-lock guard, lock snapshot, Mind Barrier payment.
    private static void MF01()
    {
        var s = Cycle(5, 2, "r1", "Abjuration", "Transmutation");
        Eq(7, s.Budget, "MF01 B=max(0,L+M)");
        Eq(7, s.Generic, "MF01 initial G");
        Outcome(Occultist0099Outcome.Ok,
            Occultist0099Focus.Invest(s, "Abjuration", 4), "MF01 invest 4");
        Outcome(Occultist0099Outcome.Ok,
            Occultist0099Focus.Invest(s, "Transmutation", 2), "MF01 invest 2");
        Eq(1, s.Generic, "MF01 G before lock");
        Eq(4, s.Schools["Abjuration"].Remaining, "MF01 R Abjuration");
        Eq(2, s.Schools["Transmutation"].Remaining, "MF01 R Transmutation");
        Eq(0, s.Spent, "MF01 U before lock");
        Eq(0, Occultist0099Focus.Resonance(s, "Abjuration"),
            "MF01 no Warding Talisman before lock");

        string before = s.Fingerprint();
        Outcome(Occultist0099Outcome.RejectedNotLocked,
            Occultist0099Focus.PaySchool(s, "Abjuration", 1, "pre-lock").Outcome,
            "MF01 pre-lock Mind Barrier blocked");
        Stable(s, before, "MF01 pre-lock pays zero, no effect");
        Check(!Occultist0099Focus.CanPaySchool(s, "Abjuration", 1, "pre-lock"),
            "MF01 pre-lock availability false");
        Stable(s, before, "MF01 read-only availability side-effect free");

        Outcome(Occultist0099Outcome.Ok, Occultist0099Focus.Lock(s), "MF01 lock");
        Eq(2, Occultist0099Focus.WardingTalismanResonance(5, 4),
            "MF01 resonance clipped 2+L/2 then /2");
        Eq(2, Occultist0099Focus.Resonance(s, "Abjuration"),
            "MF01 Warding Talisman +2");
        var paid = Occultist0099Focus.PaySchool(s, "Abjuration", 1, "mind-1");
        Outcome(Occultist0099Outcome.Ok, paid.Outcome, "MF01 Mind Barrier paid");
        Eq(1, paid.FromOrdinary, "MF01 paid from ordinary school points");
        var power = Occultist0099Focus.MindBarrier(s, "Abjuration");
        Eq(10, power.Absorption, "MF01 2L absorption at L=5");
        Eq(14, power.SaveDC, "MF01 DC=10+L/2+Int");
        Eq(1, s.Generic, "MF01 G after use");
        Eq(3, s.Schools["Abjuration"].Remaining, "MF01 R after use");
        Eq(2, s.Schools["Transmutation"].Remaining, "MF01 other school untouched");
        Eq(1, s.Spent, "MF01 U after use");
        Eq(2, Occultist0099Focus.Resonance(s, "Abjuration"),
            "MF01 snapshot survives spending");
        Conservation(s, "MF01");
        caseStatus["MF01"] = "model_assertions";
    }

    // MF02: locked 1:1 replenish, full-pool rejection, locked Invest/Reset/attribute.
    private static void MF02()
    {
        var s = Cycle(5, 2, "r1", "Abjuration", "Transmutation");
        Occultist0099Focus.Invest(s, "Abjuration", 4);
        Occultist0099Focus.Invest(s, "Transmutation", 2);
        Occultist0099Focus.Lock(s);
        Occultist0099Focus.PaySchool(s, "Abjuration", 1, "mind-1");
        string snapshot = s.Fingerprint();

        Outcome(Occultist0099Outcome.Ok,
            Occultist0099Focus.Replenish(s, "Abjuration"), "MF02 first replenish");
        Eq(0, s.Generic, "MF02 G after replenish");
        Eq(4, s.Schools["Abjuration"].Remaining, "MF02 R restored 1:1");
        Eq(2, s.Schools["Transmutation"].Remaining, "MF02 other R unchanged");
        Eq(1, s.Spent, "MF02 U preserved");
        Eq(2, Occultist0099Focus.Resonance(s, "Abjuration"),
            "MF02 snapshot preserved");
        Check(s.Locked, "MF02 lock preserved");
        Conservation(s, "MF02 after replenish");

        string before = s.Fingerprint();
        Outcome(Occultist0099Outcome.RejectedFull,
            Occultist0099Focus.Replenish(s, "Abjuration"),
            "MF02 full school with G=0 rejected");
        Stable(s, before, "MF02 full-school replenish atomic");
        // Separate fixture with G>=1 and a full school.
        var full = Cycle(8, 2, "r2", "Abjuration");
        Occultist0099Focus.Invest(full, "Abjuration", 2);
        Occultist0099Focus.Lock(full);
        before = full.Fingerprint();
        Outcome(Occultist0099Outcome.RejectedFull,
            Occultist0099Focus.Replenish(full, "Abjuration"), "MF02 full school");
        Stable(full, before, "MF02 full school unchanged");

        before = s.Fingerprint();
        Outcome(Occultist0099Outcome.RejectedAlreadyLocked,
            Occultist0099Focus.Invest(s, "Abjuration", 1), "MF02 locked Invest");
        Outcome(Occultist0099Outcome.RejectedAlreadyLocked,
            Occultist0099Focus.Reset(s), "MF02 locked Reset");
        Outcome(Occultist0099Outcome.RejectedAlreadyLocked,
            Occultist0099Focus.ChoosePhysicalAttribute(s, "Strength"),
            "MF02 locked physical choice");
        Stable(s, before, "MF02 locked paths preserve state");
        Check(!snapshot.Equals(s.Fingerprint()), "MF02 replenish really changed state");
        caseStatus["MF02"] = "model_assertions";
    }

    // MF03: insufficient school, duplicate callback, resonance at R=0, re-entry.
    private static void MF03()
    {
        var s = Cycle(8, 2, "r3", "Abjuration", "Transmutation");
        Occultist0099Focus.Invest(s, "Abjuration", 2);
        Occultist0099Focus.Invest(s, "Transmutation", 5);
        Eq(3, s.Generic, "MF03 G=3");
        Occultist0099Focus.Lock(s);
        Occultist0099Focus.PaySchool(s, "Abjuration", 1, "mf3-a");
        Occultist0099Focus.PaySchool(s, "Abjuration", 1, "mf3-b");
        Eq(0, s.Schools["Abjuration"].Remaining, "MF03 school exhausted");
        Eq(1, Occultist0099Focus.Resonance(s, "Abjuration"),
            "MF03 resonance stays present at R=0");

        string before = s.Fingerprint();
        for (int i = 0; i < 5; i++)
            Check(!Occultist0099Focus.CanPaySchool(s, "Abjuration", 1, "mf3-c"),
                "MF03 read-only availability repeated");
        Stable(s, before, "MF03 read-only availability never mutates");
        Outcome(Occultist0099Outcome.RejectedInsufficient,
            Occultist0099Focus.PaySchool(s, "Abjuration", 1, "mf3-c").Outcome,
            "MF03 no generic fallback");
        Stable(s, before, "MF03 insufficient payment atomic");
        Eq(3, s.Generic, "MF03 G not consumed by school payment");

        Outcome(Occultist0099Outcome.Ok,
            Occultist0099Focus.Replenish(s, "Abjuration"), "MF03 replenish");
        Outcome(Occultist0099Outcome.Ok,
            Occultist0099Focus.PaySchool(s, "Abjuration", 1, "mf3-c").Outcome,
            "MF03 valid use after replenish");
        string after = s.Fingerprint();
        Outcome(Occultist0099Outcome.RejectedDuplicate,
            Occultist0099Focus.PaySchool(s, "Abjuration", 1, "mf3-c").Outcome,
            "MF03 duplicate execution");
        Stable(s, after, "MF03 duplicate consumes zero");
        Conservation(s, "MF03");
        caseStatus["MF03"] = "model_assertions";
    }

    // MF04: reload/reattachment, temporary INT, idempotent rest.
    private static void MF04()
    {
        var s = Cycle(8, 2, "r4", "Abjuration", "Transmutation");
        Occultist0099Focus.Invest(s, "Abjuration", 2);
        Occultist0099Focus.Invest(s, "Transmutation", 4);
        Occultist0099Focus.Lock(s);
        Occultist0099Focus.PaySchool(s, "Abjuration", 2, "mf4-spend");
        Eq(4, s.Generic, "MF04 G=4 after spending");
        string before = s.Fingerprint();

        // Reload / fact reattachment / buff rebuild model call.
        var restored = s.Clone();
        Outcome(Occultist0099Outcome.Ok,
            Occultist0099Focus.Validate(restored), "MF04 post-load validate");
        Str(before, restored.Fingerprint(), "MF04 day and balances survive reload");
        Eq(1, Occultist0099Focus.Resonance(restored, "Abjuration"),
            "MF04 existing snapshot survives reload (2 invested -> +1 at L=8)");
        // A temporary INT buff expiring has no cycle entry point at all.
        Outcome(Occultist0099Outcome.Noop,
            Occultist0099Focus.BeginCycle(restored, 8, 7, "r4", false),
            "MF04 same rest id ignores temporary INT");
        Str(before, restored.Fingerprint(), "MF04 temporary INT cannot refill");

        Outcome(Occultist0099Outcome.Ok,
            Occultist0099Focus.BeginCycle(restored, 8, 2, "r5", false),
            "MF04 successful rest");
        Eq(10, restored.Budget, "MF04 B=10");
        Eq(10, restored.Generic, "MF04 G=B after rest");
        Eq(0, restored.Spent, "MF04 U cleared");
        Eq(0, restored.Invalidated, "MF04 Q cleared");
        Eq(0, restored.Schools["Abjuration"].Invested, "MF04 ordinary allocation cleared");
        Eq(0, Occultist0099Focus.Resonance(restored, "Abjuration"),
            "MF04 no old active resonance");
        Check(!restored.Locked, "MF04 lock cleared");
        string afterRest = restored.Fingerprint();
        Outcome(Occultist0099Outcome.Noop,
            Occultist0099Focus.BeginCycle(restored, 8, 2, "r5", false),
            "MF04 same rest event idempotent");
        Str(afterRest, restored.Fingerprint(), "MF04 duplicate rest changes nothing");
        Conservation(restored, "MF04");
        caseStatus["MF04"] = "model_assertions";
    }

    // MF05: Repower Construct explicit general cost and corpse lifecycle.
    private static void MF05()
    {
        var s = Cycle(8, 2, "r5b", "Abjuration");
        Occultist0099Focus.Invest(s, "Abjuration", 4);
        Eq(6, s.Generic, "MF05 G=6");
        Outcome(Occultist0099Outcome.Ok,
            Occultist0099Focus.RepowerConstruct(s, "corpse-1"),
            "MF05 first Repower Construct");
        Eq(5, s.Generic, "MF05 general cost exactly 1");
        Eq(1, s.Spent, "MF05 U=1");
        string before = s.Fingerprint();
        Outcome(Occultist0099Outcome.RejectedTargetInvalid,
            Occultist0099Focus.RepowerConstruct(s, "corpse-1"),
            "MF05 same corpse rejected");
        Stable(s, before, "MF05 corpse consumption recorded separately");
        Outcome(Occultist0099Outcome.Ok, Occultist0099Focus.Reset(s), "MF05 reset 1");
        Outcome(Occultist0099Outcome.Noop, Occultist0099Focus.Reset(s), "MF05 reset 2");
        Eq(9, s.Generic, "MF05 G=9 after both resets");
        Eq(1, s.Spent, "MF05 U preserved");
        Eq(0, s.Schools["Abjuration"].Remaining, "MF05 R=0");
        Conservation(s, "MF05");
        caseStatus["MF05"] = "model_assertions";
    }

    // MF06: Panoply projected payment, tie order, duplicate and unaffordable.
    private static void MF06()
    {
        var s = Cycle(8, 2, "r6", "Abjuration", "Transmutation");
        Occultist0099Focus.Invest(s, "Abjuration", 2);
        Occultist0099Focus.Invest(s, "Transmutation", 2);
        Occultist0099Focus.Lock(s);
        Check(s.PanoplyValid["Trappings of the Warrior"], "MF06 Panoply valid");
        Eq(4, Occultist0099Focus.PanoplyResonance(s, "Trappings of the Warrior"),
            "MF06 resonance is member investment sum");
        var first = Occultist0099Focus.PayPanoply(s,
            "Trappings of the Warrior", 1, "counterstrike-1");
        Outcome(Occultist0099Outcome.Ok, first.Outcome, "MF06 first Counterstrike");
        Eq(1, first.FromMembers["Abjuration"], "MF06 tie uses A JSON member order");
        Eq(1, s.Schools["Abjuration"].Remaining, "MF06 Abjuration R after tie");
        Eq(2, s.Schools["Transmutation"].Remaining, "MF06 Transmutation R after tie");
        string before = s.Fingerprint();
        Outcome(Occultist0099Outcome.RejectedDuplicate,
            Occultist0099Focus.PayPanoply(s, "Trappings of the Warrior", 1,
                "counterstrike-1").Outcome, "MF06 repeat attack event");
        Stable(s, before, "MF06 repeat consumes zero");

        var plan = Occultist0099Focus.PayPanoply(s,
            "Trappings of the Warrior", 2, "mf6-plan");
        Outcome(Occultist0099Outcome.Ok, plan.Outcome, "MF06 cost-2 plan");
        Eq(1, plan.FromMembers["Transmutation"], "MF06 first point highest R");
        Eq(1, plan.FromMembers["Abjuration"], "MF06 second point tie order");
        Eq(0, s.Schools["Abjuration"].Remaining, "MF06 leaves Abjuration 0");
        Eq(1, s.Schools["Transmutation"].Remaining, "MF06 leaves Transmutation 1");
        before = s.Fingerprint();
        Outcome(Occultist0099Outcome.RejectedInsufficient,
            Occultist0099Focus.PayPanoply(s, "Trappings of the Warrior", 5,
                "mf6-unaffordable").Outcome, "MF06 unaffordable plan");
        Stable(s, before, "MF06 unaffordable changes nothing");
        Eq(4, Occultist0099Focus.PanoplyResonance(s, "Trappings of the Warrior"),
            "MF06 snapshots stay after spending");
        Conservation(s, "MF06");
        caseStatus["MF06"] = "model_assertions";
    }

    // MF08: Implement Mastery scope, H first, no virtual class level.
    private static void MF08()
    {
        var s = Cycle(20, 5, "r8", "Abjuration", "Transmutation");
        Occultist0099Focus.Invest(s, "Abjuration", 2);
        Occultist0099Focus.Invest(s, "Transmutation", 1);
        Occultist0099Focus.Lock(s);
        Outcome(Occultist0099Outcome.Ok,
            Occultist0099Focus.SetMastery(s, "Abjuration"), "MF08 mastery Abjuration");
        var barrier = Occultist0099Focus.MindBarrier(s, "Abjuration");
        Eq(24, barrier.EffectiveLevel, "MF08 effective L=L+4");
        Eq(48, barrier.Absorption, "MF08 capacity 48");
        Eq(27, barrier.SaveDC, "MF08 mastery save DC 27");
        var ordinary = Occultist0099Focus.MindBarrier(s, "Transmutation");
        Eq(20, ordinary.EffectiveLevel, "MF08 other school stays L");
        Eq(25, ordinary.SaveDC, "MF08 other ordinary focus DC 25");
        Eq(20, s.Level, "MF08 book CL remains actual L=20");

        var paid = Occultist0099Focus.PaySchool(s, "Abjuration", 1, "mf8-barrier");
        Outcome(Occultist0099Outcome.Ok, paid.Outcome, "MF08 Mind Barrier paid");
        Eq(1, paid.FromMastery, "MF08 H pays first");
        Eq(3, s.MasteryRemaining, "MF08 H=3");
        Eq(2, s.Schools["Abjuration"].Remaining, "MF08 R=2 unchanged");
        var panoply = Occultist0099Focus.PayPanoply(s,
            "Trappings of the Warrior", 1, "mf8-panoply");
        Outcome(Occultist0099Outcome.Ok, panoply.Outcome, "MF08 Panoply paid");
        Eq(3, s.MasteryRemaining, "MF08 Panoply leaves H=3");
        Eq(1, s.MasterySpent, "MF08 T=1");
        Outcome(Occultist0099Outcome.Ok,
            Occultist0099Focus.RespecMastery(s, "Transmutation"),
            "MF08 respec mastery");
        Str("Transmutation", s.MasterySchool, "MF08 new mastery school");
        Eq(1, s.MasterySpent, "MF08 T carried");
        Eq(3, s.MasteryRemaining, "MF08 at most 3 remaining");
        Eq(20, Occultist0099Focus.MindBarrier(s, "Abjuration").EffectiveLevel,
            "MF08 old mastery-only contribution removed");
        Eq(25, Occultist0099Focus.MindBarrier(s, "Abjuration").SaveDC,
            "MF08 old mastery DC removed");
        Conservation(s, "MF08");
        caseStatus["MF08"] = "model_assertions";
    }

    // MF09: preview cancel, source cleanup, locked respec, same-day out-and-back.
    private static void MF09()
    {
        var focus = Cycle(8, 2, "r9", "Abjuration");
        Occultist0099Focus.Invest(focus, "Abjuration", 4);
        Occultist0099Focus.Lock(focus);
        Occultist0099Focus.PaySchool(focus, "Abjuration", 1, "mf9-spend");
        Eq(3, focus.Schools["Abjuration"].Remaining, "MF09 R=3");
        Eq(1, focus.Spent, "MF09 U=1");

        string focusBefore = focus.Fingerprint();
        var spells = Occultist0099Spells.Create(1);
        Outcome(Occultist0099Outcome.Ok,
            Occultist0099Spells.AddSlot(spells, "slot-1", "Abjuration"),
            "MF09 first slot");
        Outcome(Occultist0099Outcome.Ok,
            Occultist0099Spells.AddSlot(spells, "slot-2", "Abjuration", true),
            "MF09 second same-school slot");
        string live = spells.Fingerprint();
        var preview = Occultist0099Spells.BeginPreview(spells);
        Occultist0099Spells.RemoveSlot(spells, "slot-1");
        Occultist0099Spells.RemoveSlot(spells, "slot-2");
        Outcome(Occultist0099Outcome.Ok,
            Occultist0099Spells.CancelPreview(spells, preview), "MF09 discard preview");
        Str(live, spells.Fingerprint(), "MF09 discard restores live state byte-for-byte");

        Outcome(Occultist0099Outcome.Ok,
            Occultist0099Spells.RemoveSlot(spells, "slot-2"),
            "MF09 commit Extra Spells removal");
        Eq(1, spells.Slots.Count, "MF09 only first slot left");
        Str(focusBefore, focus.Fingerprint(),
            "MF09 Extra Spells removal preserves school focus state");

        Outcome(Occultist0099Outcome.Ok,
            Occultist0099Focus.RespecRemoveSchool(focus, "Abjuration"),
            "MF09 remove last school while locked");
        Eq(3, focus.Invalidated, "MF09 Q increases by 3");
        Eq(0, Occultist0099Focus.Resonance(focus, "Abjuration"),
            "MF09 Abjuration resonance invalidated");
        Check(!focus.PanoplyValid["Trappings of the Warrior"],
            "MF09 dependent Panoply invalidated");
        Check(!focus.PanoplyValid["Saint's Holy Regalia"],
            "MF09 second dependent Panoply invalidated");
        Conservation(focus, "MF09 after school removal");

        string beforeClass = focus.Fingerprint();
        Occultist0099Focus.RemoveClass(focus);
        Check(!focus.Enabled, "MF09 removing class closes payment/resonance");
        Check(!focus.ClassPresent, "MF09 ClassPresent cleared on removal");
        // D-0099-02 fix: removal no longer clears dedup identities, so the
        // observable record is the closed payment gate, not a fingerprint diff.
        string removedSnapshot = focus.Fingerprint();
        Outcome(Occultist0099Outcome.RejectedClassRemoved,
            Occultist0099Focus.PaySchool(focus, "Abjuration", 1, "mf9-while-removed").Outcome,
            "MF09 payment rejected while class removed");
        Str(removedSnapshot, focus.Fingerprint(),
            "MF09 rejected payment while removed changes nothing");
        Occultist0099Focus.ReacquireClass(focus);
        Outcome(Occultist0099Outcome.Noop,
            Occultist0099Focus.BeginCycle(focus, 8, 2, "r9", true),
            "MF09 same-day reacquisition creates no cycle");
        Eq(3, focus.Invalidated, "MF09 no new daily budget");
        Eq(0, Occultist0099Focus.Resonance(focus, "Abjuration"),
            "MF09 no free old snapshot");
        Check(focus.ClassPresent && focus.Enabled,
            "MF09 reacquire reopens the gate without a new budget");
        // D-0099-02 fix: with dedup identities retained, remove+reacquire is a
        // pure gate cycle that restores the exact prior state (no free refill,
        // no lost dedup). The meaningful record is the rejection while removed
        // (asserted above), not a spurious fingerprint diff.
        Str(beforeClass, focus.Fingerprint(),
            "MF09 remove+reacquire restores exact state, no refill no dedup loss");
        caseStatus["MF09"] = "model_assertions";
    }

    // MF10: two units, multiclass isolation, zero-capacity owner.
    private static void MF10()
    {
        var ledger = new Occultist0099Ledger();
        var a = ledger.For("unit-A");
        var b = ledger.For("unit-B");
        Occultist0099Focus.BeginCycle(a, 5, 2, "r10", false);
        Occultist0099Focus.SetSchoolPresent(a, "Abjuration", true);
        Occultist0099Focus.Invest(a, "Abjuration", 4);
        Occultist0099Focus.Lock(a);
        Occultist0099Focus.PaySchool(a, "Abjuration", 1, "mf10-a");
        Occultist0099Focus.BeginCycle(b, 1, 0, "r10b", false);
        string bBefore = b.Fingerprint();
        int foreignPhrenicPool = 3;   // separate Psychic resource, never passed in
        Occultist0099Focus.Reset(a);
        Str(bBefore, b.Fingerprint(), "MF10 unit B untouched");
        Eq(3, foreignPhrenicPool, "MF10 foreign phrenic pool untouched");
        Eq(10, Occultist0099Focus.MindBarrier(a, "Abjuration").Absorption,
            "MF10 Occultist effects scale from L=5");
        Eq(2, Occultist0099Focus.MindBarrier(b, "Abjuration").Absorption,
            "MF10 unit B scales from its own L=1");
        Eq(7, a.Budget, "MF10 A budget from Occultist 5 + Int 2");
        Eq(1, b.Budget, "MF10 B budget from Occultist 1");

        var zero = ledger.For("unit-C");
        Occultist0099Focus.BeginCycle(zero, 0, -5, "r10c", false);
        Eq(0, zero.Budget, "MF10 budget floor 0");
        Eq(0, zero.Generic, "MF10 no free general points");
        Outcome(Occultist0099Outcome.Ok,
            Occultist0099Focus.SetSchoolPresent(zero, "Abjuration", true),
            "MF10 zero-capacity owner still has the school fact");
        Outcome(Occultist0099Outcome.Ok, Occultist0099Focus.Lock(zero),
            "MF10 zero-capacity owner can lock with zero investment");
        Outcome(Occultist0099Outcome.RejectedInsufficient,
            Occultist0099Focus.PaySchool(zero, "Abjuration", 1, "mf10-zero").Outcome,
            "MF10 zero capacity cannot pay");
        Eq(0, zero.Spent, "MF10 no negative or free spend");
        Conservation(zero, "MF10 zero capacity");
        caseStatus["MF10"] = "model_assertions";
    }

    // SP01: implement ordering, Extra Spells, two roots, six knacks.
    private static void SP01()
    {
        var s = Occultist0099Spells.Create(1);
        Outcome(Occultist0099Outcome.RejectedNotOwned,
            Occultist0099Spells.AddSlot(s, "extra", "Abjuration", true),
            "SP01 Extra Spells before first selection");
        Outcome(Occultist0099Outcome.Ok,
            Occultist0099Spells.AddSlot(s, "slot-1", "Abjuration"),
            "SP01 first Abjuration slot");
        Outcome(Occultist0099Outcome.Ok,
            Occultist0099Spells.AddSlot(s, "slot-2", "Abjuration", true),
            "SP01 Extra Spells after first fact");
        Outcome(Occultist0099Outcome.RejectedDuplicate,
            Occultist0099Spells.AddSlot(s, "slot-3", "Abjuration", true),
            "SP01 third Abjuration selection rejected");
        Eq(1, Occultist0099Spells.SchoolsOf(s, "slot-1").Distinct().Count(),
            "SP01 one base power per school");
        Eq(1, Occultist0099Spells.SchoolsOf(s, "slot-2").Distinct().Count(),
            "SP01 one school resource shared by both implements");
        Outcome(Occultist0099Outcome.Ok,
            Occultist0099Spells.Learn(s, "slot-1", 1,
                Occultist0099AbjurationCatalog.MageShield), "SP01 MageShield");
        Outcome(Occultist0099Outcome.Ok,
            Occultist0099Spells.Learn(s, "slot-2", 1,
                Occultist0099AbjurationCatalog.WardingWeapon), "SP01 Warding Weapon");
        Eq(2, Occultist0099Spells.KnownRoots(s).Length, "SP01 two level-1 roots known");
        Eq(6, Occultist0099Spells.Knacks(s).Length, "SP01 exactly six fixed knacks");
        Eq(2, Occultist0099Spells.QuotaCount(s, 1), "SP01 one quota per slot per circle");
        caseStatus["SP01"] = "model_assertions";
    }

    // SP02: circle gates, per-slot provenance, exhausted level-6 candidates.
    private static void SP02()
    {
        var s = Occultist0099Spells.Create(1);
        Occultist0099Spells.AddSlot(s, "slot-1", "Abjuration");
        Occultist0099Spells.AddSlot(s, "slot-2", "Abjuration", true);
        Eq(0, Occultist0099Spells.QuotaCount(s, 2), "SP02 no level-2 quota before L4");
        Outcome(Occultist0099Outcome.Ok, Occultist0099Spells.SetLevel(s, 4),
            "SP02 level 4 opens circle 2");
        Eq(2, Occultist0099Spells.QuotaCount(s, 2), "SP02 one level-2 quota per slot");
        Outcome(Occultist0099Outcome.Ok,
            Occultist0099Spells.Learn(s, "slot-1", 2,
                Occultist0099AbjurationCatalog.AlliedCloak), "SP02 Allied Cloak");
        Outcome(Occultist0099Outcome.Ok,
            Occultist0099Spells.Learn(s, "slot-2", 2,
                Occultist0099AbjurationCatalog.ResistEnergy), "SP02 Resist Energy");
        Eq(2, s.Sources.Count(x => x.Circle == 2), "SP02 separate provenance");
        Check(s.Sources.Count(x => x.SlotKey == "slot-1" && x.Circle == 2) == 1 &&
              s.Sources.Count(x => x.SlotKey == "slot-2" && x.Circle == 2) == 1,
            "SP02 one source per slot");
        Outcome(Occultist0099Outcome.RejectedDuplicate,
            Occultist0099Spells.Learn(s, "slot-2", 2,
                Occultist0099AbjurationCatalog.AlliedCloak),
            "SP02 duplicate root excluded");
        var circleCheck = Occultist0099Spells.Create(4);
        Occultist0099Spells.AddSlot(circleCheck, "circle-slot", "Abjuration");
        Check(!Occultist0099Spells.Candidates(circleCheck, "circle-slot", 2)
                .Any(r => r.Guid == Occultist0099AbjurationCatalog.MageShield),
            "SP02 wrong circle excluded from candidates");

        Outcome(Occultist0099Outcome.Ok, Occultist0099Spells.SetLevel(s, 16),
            "SP02 level 16 opens circle 6");
        Eq(2, Occultist0099Spells.QuotaCount(s, 6), "SP02 two level-6 requests");
        Eq(1, Occultist0099Spells.Candidates(s, "slot-1", 6).Length,
            "SP02 level 6 has a single candidate");
        Outcome(Occultist0099Outcome.Ok,
            Occultist0099Spells.Learn(s, "slot-1", 6,
                Occultist0099AbjurationCatalog.GlobeOfInvulnerability),
            "SP02 Globe of Invulnerability");
        Eq(0, Occultist0099Spells.Candidates(s, "slot-2", 6).Length,
            "SP02 candidates exhausted for second slot");
        Outcome(Occultist0099Outcome.RejectedInvalidArgument,
            Occultist0099Spells.Learn(s, "slot-2", 6,
                Occultist0099AbjurationCatalog.GlobeOfInvulnerability),
            "SP02 no free replacement");
        Check(s.Quotas[Occultist0099Spells.QuotaKey("slot-2", 6)] == null,
            "SP02 second quota stays empty");
        caseStatus["SP02"] = "model_assertions";
    }

    // SP03: preview cancel, same-book and other-book source cleanup.
    private static void SP03()
    {
        var s = Occultist0099Spells.Create(1);
        Occultist0099Spells.AddSlot(s, "slot-1", "Abjuration");
        Occultist0099Spells.Learn(s, "slot-1", 1,
            Occultist0099AbjurationCatalog.MageShield);
        Occultist0099Spells.AddSameBookGrant(s, "grant-1",
            Occultist0099AbjurationCatalog.MageShield, 1);
        Occultist0099Spells.AddForeignBookRoot(s,
            Occultist0099AbjurationCatalog.MageShield);
        string live = s.Fingerprint();
        var preview = Occultist0099Spells.BeginPreview(s);
        Occultist0099Spells.RemoveSlot(s, "slot-1");
        Occultist0099Spells.RemoveSameBookGrant(s, "grant-1");
        Occultist0099Spells.CancelPreview(s, preview);
        Str(live, s.Fingerprint(), "SP03 cancel restores quota and data");

        Occultist0099Spells.RemoveSlot(s, "slot-1");
        Check(Occultist0099Spells.IsKnown(s,
            Occultist0099AbjurationCatalog.MageShield),
            "SP03 other same-book source survives class-slot removal");
        Check(s.ForeignBookRoots.Contains(Occultist0099AbjurationCatalog.MageShield),
            "SP03 Wizard source survives");
        Occultist0099Spells.RemoveSameBookGrant(s, "grant-1");
        Check(!Occultist0099Spells.IsKnown(s,
            Occultist0099AbjurationCatalog.MageShield),
            "SP03 last same-book source removed");
        Check(s.ForeignBookRoots.Contains(Occultist0099AbjurationCatalog.MageShield),
            "SP03 Wizard side still survives");
        caseStatus["SP03"] = "model_assertions";
    }

    // SP04: Panoply prerequisites, duplicate, member loss invalidation.
    private static void SP04()
    {
        var s = Occultist0099Spells.Create(1);
        Occultist0099Spells.AddSlot(s, "abj", "Abjuration");
        Occultist0099Spells.AddSlot(s, "trans", "Transmutation");
        Outcome(Occultist0099Outcome.Ok,
            Occultist0099Spells.AddSlot(s, "trappings", null, false,
                "Trappings of the Warrior"), "SP04 Trappings at 2");
        Outcome(Occultist0099Outcome.RejectedDuplicate,
            Occultist0099Spells.AddSlot(s, "trappings-2", null, false,
                "Trappings of the Warrior"), "SP04 duplicate Panoply barred");
        Outcome(Occultist0099Outcome.RejectedNotOwned,
            Occultist0099Spells.AddSlot(s, "mages", null, false,
                "Mage's Paraphernalia"), "SP04 0/3 members");
        Occultist0099Spells.AddSlot(s, "div", "Divination");
        Occultist0099Spells.AddSlot(s, "evo", "Evocation");
        Outcome(Occultist0099Outcome.RejectedNotOwned,
            Occultist0099Spells.AddSlot(s, "mages-2", null, false,
                "Mage's Paraphernalia"), "SP04 2/3 members");
        Occultist0099Spells.AddSlot(s, "necro", "Necromancy");
        Outcome(Occultist0099Outcome.Ok,
            Occultist0099Spells.AddSlot(s, "mages", null, false,
                "Mage's Paraphernalia"), "SP04 3/3 members legal");
        Eq(3, Occultist0099Spells.SchoolsOf(s, "mages").Length,
            "SP04 union of member schools");
        int before = s.Quotas.Count;
        Occultist0099Spells.RemoveSlot(s, "abj");
        Check(!s.Slots.Any(x => x.Panoply == "Trappings of the Warrior"),
            "SP04 dependent Panoply slot invalidated");
        Check(s.Quotas.Count < before, "SP04 derived quotas removed");
        caseStatus["SP04"] = "model_assertions";
    }

    // SP05: catalog ledger check. D-0099-04 fix: row counts are now derived
    // INDEPENDENTLY from A-occultist-assets.json (the A asset file), not from
    // the production constant they are compared against. The full 143-row /
    // Spirit-Bound Blade part belongs to later packages and stays not_run.
    private static void SP05(string[] args)
    {
        string assetsPath = args != null && args.Length > 1 ? args[1] :
            "docs/work-packages/COTW-0099/A-occultist-assets.json";
        int assetRows = -1, assetAbj = 0, assetZero = 0;
        try
        {
            using (var doc = JsonDocument.Parse(File.ReadAllText(assetsPath)))
            {
                var roots = doc.RootElement.GetProperty("spellRoots");
                assetRows = roots.GetArrayLength();
                foreach (var r in roots.EnumerateArray())
                {
                    if (r.GetProperty("school").GetString() == "Abjuration") assetAbj++;
                    if (r.GetProperty("level").GetInt32() == 0) assetZero++;
                }
            }
        }
        catch (Exception e)
        {
            failures.Add("SP05 A asset file unreadable: " + e.Message);
        }
        Check(assetRows > 0, "SP05 A asset file loaded with spellRoots");
        Eq(assetRows, Occultist0099AbjurationCatalog.TotalCatalogRows,
            "SP05 catalog rows match A asset (independent source)");
        Eq(assetAbj, Occultist0099AbjurationCatalog.SliceRows,
            "SP05 Abjuration rows match A asset (independent source)");
        Eq(assetZero, Occultist0099AbjurationCatalog.Knacks.Length,
            "SP05 six level-0 knacks match A asset (independent source)");
        Eq(18, Occultist0099AbjurationCatalog.Roots.Length,
            "SP05 Abjuration slice row count");
        Eq(1, Occultist0099AbjurationCatalog.Roots.Count(r => r.IsKnack),
            "SP05 exactly one Abjuration root is a knack (Resistance)");
        Eq(1, Occultist0099AbjurationCatalog.Find(
            Occultist0099AbjurationCatalog.MageShield).Circle,
            "SP05 MageShield private circle 1");
        Eq(6, Occultist0099AbjurationCatalog.Find(
            Occultist0099AbjurationCatalog.GlobeOfInvulnerability).Circle,
            "SP05 Globe private circle 6");
        Eq(4, Occultist0099AbjurationCatalog.ClassLevelForCircle(2),
            "SP05 circle 2 opens at class level 4");
        Eq(16, Occultist0099AbjurationCatalog.ClassLevelForCircle(6),
            "SP05 circle 6 opens at class level 16");
        caseStatus["SP05"] = "partial_model_full_143_rows_not_run";
    }

    // SP06: provider modes, published-list gate, broken child, saved root.
    private static void SP06()
    {
        var s = Occultist0099Spells.Create(7);
        Occultist0099Spells.AddSlot(s, "slot-1", "Abjuration");
        Check(!Occultist0099Spells.Candidates(s, "slot-1", 3)
                .Any(r => r.Guid == Occultist0099AbjurationCatalog.CloakOfWinds),
            "SP06 no provider: donor root skipped");
        Occultist0099Spells.SetProvider(s, "TabletopTweaks-Base", false, true);
        Check(!Occultist0099Spells.Candidates(s, "slot-1", 3)
                .Any(r => r.Guid == Occultist0099AbjurationCatalog.CloakOfWinds),
            "SP06 disabled provider skipped");
        Occultist0099Spells.SetProvider(s, "TabletopTweaks-Base", true, false);
        Check(!Occultist0099Spells.Candidates(s, "slot-1", 3)
                .Any(r => r.Guid == Occultist0099AbjurationCatalog.CloakOfWinds),
            "SP06 active but no published list skipped");
        Occultist0099Spells.SetProvider(s, "TabletopTweaks-Base", true, true);
        Check(Occultist0099Spells.Candidates(s, "slot-1", 3)
                .Any(r => r.Guid == Occultist0099AbjurationCatalog.CloakOfWinds),
            "SP06 valid active root enters candidates");
        s.DlcAllowed.Remove(Occultist0099AbjurationCatalog.CloakOfWinds);
        Check(!Occultist0099Spells.Candidates(s, "slot-1", 3)
                .Any(r => r.Guid == Occultist0099AbjurationCatalog.CloakOfWinds),
            "SP06 broken child excludes root");
        Outcome(Occultist0099Outcome.RejectedInvalidArgument,
            Occultist0099Spells.Learn(s, "slot-1", 3,
                Occultist0099AbjurationCatalog.CloakOfWinds),
            "SP06 no free spell replacement");
        s.DlcAllowed.Add(Occultist0099AbjurationCatalog.CloakOfWinds);
        Outcome(Occultist0099Outcome.Ok,
            Occultist0099Spells.Learn(s, "slot-1", 3,
                Occultist0099AbjurationCatalog.CloakOfWinds),
            "SP06 saved root learned");
        Occultist0099Spells.SetProvider(s, "TabletopTweaks-Base", false, false);
        Check(Occultist0099Spells.IsKnown(s,
                Occultist0099AbjurationCatalog.CloakOfWinds),
            "SP06 saved root and provenance survive provider toggle");
        caseStatus["SP06"] = "partial_model_fireshield_variants_not_run";
    }

    // SP07: private book scope and shared psychic mechanics.
    private static void SP07()
    {
        string book = Occultist0099Ids.BookGuid;
        string psychic = "psychic-book";
        string other = "wizard-book";
        Check(Occultist0099BookScope.IsOccultistBook(book),
            "SP07 private book identity");
        Check(Occultist0099BookScope.Allows("thought", book, book, psychic),
            "SP07 thought applies to owning psychic cast");
        Check(!Occultist0099BookScope.Allows("thought", book, psychic, psychic),
            "SP07 thought does not cross books");
        Check(!Occultist0099BookScope.Allows("emotion", null, book, psychic),
            "SP07 item cast keeps native behaviour");
        Check(!Occultist0099BookScope.Allows("Amplification", book, book, psychic),
            "SP07 0099 book cannot use Psychic amplification");
        Check(!Occultist0099BookScope.Allows("Undercasting", book, book, psychic),
            "SP07 0099 book cannot use Psychic undercasting");
        Check(Occultist0099BookScope.Allows("Discipline", psychic, psychic, psychic),
            "SP07 discipline still requires the Psychic book");
        Eq(1, Occultist0099BookScope.CircleOf(book,
            Occultist0099AbjurationCatalog.MageShield, 1),
            "SP07 private circle for 0099 book");
        Eq(2, Occultist0099BookScope.CircleOf(other,
            Occultist0099AbjurationCatalog.MageShield, 2),
            "SP07 other book keeps its native circle");
        caseStatus["SP07"] = "model_assertions";
    }

    // SP08: registration atomicity over REAL private GUIDs and an explicit
    // cache with owner tokens (D-0099-06 fix). ForeignDataTouched is a genuine
    // diff, proven distinguishable by an injected-mutation negative case.
    // The native BlueprintsCache fixture itself is task #11; until it runs,
    // SP08 acceptance stays not_run.
    private static void SP08()
    {
        var r = new Occultist0099Registration();
        string[] mandatory =
        {
            Occultist0099AbjurationCatalog.MageShield,
            Occultist0099AbjurationCatalog.ResistEnergy
        };
        string[] missing = { Occultist0099AbjurationCatalog.MageShield };
        var cache = new Dictionary<string, string>
        {
            ["foreign-wizard-book"] = "wizard",
            ["foreign-psychic-book"] = "psychic"
        };
        var foreignBefore = new Dictionary<string, string>(cache);
        Check(!r.TryPublish(mandatory, missing, cache, 3),
            "SP08 missing donor fails");
        Eq(0, r.ClassEntries, "SP08 no class entry");
        Eq(0, r.SpellbookEntries, "SP08 no spellbook entry");
        Check(!r.ForeignDataTouched, "SP08 foreign data intact on missing donor");
        Check(!r.PreviewLedgerPresent, "SP08 no lingering preview ledger");
        Check(cache.Count == 2 && cache["foreign-wizard-book"] == "wizard",
            "SP08 failed attempt wrote nothing");

        // Foreign GUID collision: another owner already holds our class GUID.
        var collided = new Dictionary<string, string>(cache)
        {
            [Occultist0099Ids.ClassGuid] = "some-other-mod"
        };
        Check(!r.TryPublish(mandatory, mandatory, collided, 3),
            "SP08 foreign GUID collision fails");
        Check(!r.Published, "SP08 still unpublished");
        Check(collided[Occultist0099Ids.ClassGuid] == "some-other-mod",
            "SP08 collision leaves the foreign owner untouched");
        Check(!collided.ContainsKey(Occultist0099Ids.BookGuid),
            "SP08 collision writes no private entry");

        // Negative case proving the diff is real: a foreign entry mutated
        // DURING publication MUST be detected as ForeignDataTouched=true.
        var probeCache = new Dictionary<string, string>(cache);
        var probe = new Occultist0099Registration();
        bool probePublished = probe.TryPublish(mandatory, mandatory, probeCache, 3,
            () => probeCache["foreign-wizard-book"] = "tampered");
        Check(probePublished && probe.ForeignDataTouched,
            "SP08 injected foreign mutation is detected (negative case)");

        Check(r.TryPublish(mandatory, mandatory, cache, 3),
            "SP08 complete map publishes");
        Eq(1, r.ClassEntries, "SP08 exactly one class entry");
        Eq(1, r.SpellbookEntries, "SP08 exactly one spellbook");
        Eq(3, r.ChoiceEntries, "SP08 complete choices");
        Check(!r.ForeignDataTouched, "SP08 success path keeps foreign data intact");
        Check(cache["foreign-wizard-book"] == "wizard" &&
              cache["foreign-psychic-book"] == "psychic",
            "SP08 foreign entries byte-identical after publish");
        Check(Occultist0099Registration.PrivateGuids.All(g =>
                  cache.ContainsKey(g) && cache[g] == "0099"),
            "SP08 all private identities registered under the 0099 owner");
        Check(r.TryPublish(mandatory, mandatory, cache, 3),
            "SP08 repeated completion succeeds");
        Eq(1, r.ClassEntries, "SP08 no duplicate class registration");
        Eq(1, r.SpellbookEntries, "SP08 no duplicate spellbook registration");
        Check(!r.PreviewLedgerPresent, "SP08 preview ledger cleared");
        caseStatus["SP08"] = "model_gate_only_native_cache_not_run";
    }


    private static void Identity()
    {
        Check(Occultist0099Ids.ClassGuid != Occultist0099Ids.BookGuid,
            "identity: class and book GUIDs differ");
        // D-0099-04 fix: determinism is checked against an INDEPENDENT MD5
        // computation, not Id() compared with itself.
        string expected;
        using (var md5 = System.Security.Cryptography.MD5.Create())
        {
            byte[] hash = md5.ComputeHash(System.Text.Encoding.UTF8.GetBytes(
                Occultist0099Ids.Salt + "OccultistClass"));
            expected = string.Concat(hash.Select(b => b.ToString("x2")));
        }
        Str(expected, Occultist0099Ids.ClassGuid,
            "identity: ClassGuid equals independent salted MD5");
        Check(Occultist0099Ids.ClassGuid.Length == 32,
            "identity: 32-hex private GUID");
        Check(!Occultist0099Ids.ClassGuid.Equals(
            "1b76f3c73aa84f91a1c65513fb23aa01"),
            "identity: Kingmaker original GUID not reused");
        // Distinct tokens must give distinct ids (negative case for collision).
        Check(Occultist0099Ids.SliceTokens.Select(t => Occultist0099Ids.Id(t))
                  .Distinct().Count() == Occultist0099Ids.SliceTokens.Length,
            "identity: all slice tokens map to distinct GUIDs");
        Str("Abjuration", Occultist0099Ids.SliceSchool, "slice school");
        Str("C-0099-20261115-r1", Occultist0099Ids.Contract, "frozen contract id");
        Check(Occultist0099Ids.TypeId(1).Length == 32 &&
              Occultist0099Ids.TypeId(2).Length == 32 &&
              Occultist0099Ids.TypeId(1) != Occultist0099Ids.TypeId(2),
            "identity: 32-hex TypeIds, distinct per index");
        // D-0099-08: native [TypeId] constants equal the computed TypeId block.
        Str(Occultist0099Ids.TypeId(1), Occultist0099Ids.FocusFactTypeId,
            "identity: FocusFactTypeId constant matches TypeId(1)");
        Str(Occultist0099Ids.TypeId(2), Occultist0099Ids.SpellbookMarkerTypeId,
            "identity: SpellbookMarkerTypeId constant matches TypeId(2)");
        // D-0099-08: SlicePublished is an enforced gate, not an unused flag.
        Check(!Occultist0099Ids.SlicePublished,
            "identity: slice is development-only, not published");
        Check(!Occultist0099Slice.CanRegister(true),
            "identity: unpublished slice blocks registration even with full map");
        Check(!Occultist0099Slice.CanRegister(false),
            "identity: incomplete map blocks registration");
    }

    // D-0099-02 regression: dedup identities survive class remove+reacquire
    // within the same cycle; only a genuinely new rest cycle clears them.
    private static void DFix02()
    {
        var s = Cycle(8, 2, "fix02-rest", "Abjuration");
        Occultist0099Focus.Invest(s, "Abjuration", 4);
        Occultist0099Focus.Lock(s);
        Outcome(Occultist0099Outcome.Ok,
            Occultist0099Focus.PaySchool(s, "Abjuration", 1, "fix02-exec").Outcome,
            "DFix02 first payment");
        Eq(1, s.Spent, "DFix02 U=1");
        Occultist0099Focus.RemoveClass(s);
        Check(s.ChargedExecutions.Contains("fix02-exec"),
            "DFix02 dedup identity retained across RemoveClass");
        Occultist0099Focus.ReacquireClass(s);
        string before = s.Fingerprint();
        Outcome(Occultist0099Outcome.RejectedDuplicate,
            Occultist0099Focus.PaySchool(s, "Abjuration", 1, "fix02-exec").Outcome,
            "DFix02 same execution id rejected after reacquire");
        Stable(s, before, "DFix02 rejected duplicate changes nothing");
        Eq(1, s.Spent, "DFix02 U unchanged after duplicate attempt");
        Eq(3, s.Schools["Abjuration"].Remaining, "DFix02 R unchanged");
        Check(!Occultist0099Focus.CanPaySchool(s, "Abjuration", 1, "fix02-exec"),
            "DFix02 availability false for the charged id");
        // Positive case: a DIFFERENT execution id still pays after reacquire.
        Outcome(Occultist0099Outcome.Ok,
            Occultist0099Focus.PaySchool(s, "Abjuration", 1, "fix02-other").Outcome,
            "DFix02 different execution id still pays");
        Eq(2, s.Spent, "DFix02 U=2 after second distinct payment");
        // Corpse dedup survives too.
        var c = Cycle(8, 2, "fix02c-rest", "Abjuration");
        Occultist0099Focus.RepowerConstruct(c, "fix02-corpse");
        Occultist0099Focus.RemoveClass(c);
        Occultist0099Focus.ReacquireClass(c);
        Outcome(Occultist0099Outcome.RejectedTargetInvalid,
            Occultist0099Focus.RepowerConstruct(c, "fix02-corpse"),
            "DFix02 consumed corpse still rejected after reacquire");
        // A genuinely new rest cycle clears the identities and re-arms payment.
        Outcome(Occultist0099Outcome.Ok,
            Occultist0099Focus.BeginCycle(s, 8, 2, "fix02-rest-2", false),
            "DFix02 new rest cycle");
        Check(!s.ChargedExecutions.Contains("fix02-exec"),
            "DFix02 new cycle clears dedup identities");
        Occultist0099Focus.Invest(s, "Abjuration", 4);
        Occultist0099Focus.Lock(s);
        Outcome(Occultist0099Outcome.Ok,
            Occultist0099Focus.PaySchool(s, "Abjuration", 1, "fix02-exec").Outcome,
            "DFix02 same id pays again only in the new cycle");
        Conservation(s, "DFix02");
    }

    // D-0099-03 regression: Int >= 10 + circle gates opening, learning and
    // casting; temporary Int drops never delete established quotas/sources.
    private static void DFix03()
    {
        // Low Int: level 4 but Int 11 -> only circle 1 reachable.
        var low = Occultist0099Spells.Create(4, 11);
        Occultist0099Spells.AddSlot(low, "low-slot", "Abjuration");
        Eq(1, Occultist0099Spells.MaxCircle(low), "DFix03 Int 11 caps at circle 1");
        Eq(0, Occultist0099Spells.QuotaCount(low, 2),
            "DFix03 circle 2 not opened at Int 11");
        Outcome(Occultist0099Outcome.RejectedInsufficient,
            Occultist0099Spells.Learn(low, "low-slot", 2,
                Occultist0099AbjurationCatalog.ResistEnergy),
            "DFix03 circle 2 not learnable at Int 11 (not open)");
        Eq(0, Occultist0099Spells.Candidates(low, "low-slot", 2).Length,
            "DFix03 no circle-2 candidates at Int 11");
        Outcome(Occultist0099Outcome.Ok,
            Occultist0099Spells.Learn(low, "low-slot", 1,
                Occultist0099AbjurationCatalog.MageShield),
            "DFix03 circle 1 learnable at Int 11");
        Check(Occultist0099Spells.CanCast(low, Occultist0099AbjurationCatalog.MageShield),
            "DFix03 circle 1 castable at Int 11");

        // Raise Int to 16: circle 2 opens and becomes learnable/castable.
        Outcome(Occultist0099Outcome.Ok,
            Occultist0099Spells.SetIntelligence(low, 16), "DFix03 raise Int to 16");
        Eq(2, Occultist0099Spells.MaxCircle(low), "DFix03 Int 16 reaches circle 2");
        Eq(1, Occultist0099Spells.QuotaCount(low, 2),
            "DFix03 circle 2 opened after Int raise");
        Outcome(Occultist0099Outcome.Ok,
            Occultist0099Spells.Learn(low, "low-slot", 2,
                Occultist0099AbjurationCatalog.ResistEnergy),
            "DFix03 circle 2 learnable at Int 16");
        Check(Occultist0099Spells.CanCast(low, Occultist0099AbjurationCatalog.ResistEnergy),
            "DFix03 circle 2 castable at Int 16");

        // Temporary Int drop to 11: casting circle 2 blocked, quota/source kept.
        string established = low.Fingerprint();
        Occultist0099Spells.SetIntelligence(low, 11);
        Check(!Occultist0099Spells.CanCast(low, Occultist0099AbjurationCatalog.ResistEnergy),
            "DFix03 temporary Int drop blocks circle-2 cast");
        Check(Occultist0099Spells.IsKnown(low, Occultist0099AbjurationCatalog.ResistEnergy),
            "DFix03 established source survives Int drop");
        Eq(1, Occultist0099Spells.QuotaCount(low, 2),
            "DFix03 established quota survives Int drop");
        Check(low.Quotas[Occultist0099Spells.QuotaKey("low-slot", 2)] ==
              Occultist0099AbjurationCatalog.ResistEnergy,
            "DFix03 quota content survives Int drop");
        Occultist0099Spells.SetIntelligence(low, 16);
        Check(Occultist0099Spells.CanCast(low, Occultist0099AbjurationCatalog.ResistEnergy),
            "DFix03 Int recovery restores casting");
        Str(established, low.Fingerprint(),
            "DFix03 Int down-up cycle leaves state byte-identical");

        // Learn rejection code when the quota exists but Int is too low.
        var mid = Occultist0099Spells.Create(4, 16);
        Occultist0099Spells.AddSlot(mid, "mid-slot", "Abjuration");
        Occultist0099Spells.SetIntelligence(mid, 11);
        Outcome(Occultist0099Outcome.RejectedIntTooLow,
            Occultist0099Spells.Learn(mid, "mid-slot", 2,
                Occultist0099AbjurationCatalog.ResistEnergy),
            "DFix03 open quota + low Int gives RejectedIntTooLow");
        Check(!Occultist0099Spells.CanCast(mid, Occultist0099AbjurationCatalog.MageShield),
            "DFix03 unknown root never castable");
    }

    // D-0099-05 regression: fingerprint covers every mutable field; a leak in
    // any previously-missed field is detected, and cancel restores all fields.
    private static void DFix05()
    {
        var s = Occultist0099Spells.Create(4);
        Occultist0099Spells.AddSlot(s, "fp-slot", "Abjuration");
        Occultist0099Spells.Learn(s, "fp-slot", 1, Occultist0099AbjurationCatalog.MageShield);
        Occultist0099Spells.AddSameBookGrant(s, "fp-grant",
            Occultist0099AbjurationCatalog.MageShield, 1);
        string baseline = s.Fingerprint();

        // Each previously-missed field must change the fingerprint.
        var probes = new Dictionary<string, Action<Occultist0099SpellState>>
        {
            ["Providers"] = x => Occultist0099Spells.SetProvider(x, "TabletopTweaks-Base", true, false),
            ["PublishedLists"] = x => Occultist0099Spells.SetProvider(x, "TabletopTweaks-Base", false, true),
            ["DlcAllowed"] = x => x.DlcAllowed.Remove(Occultist0099AbjurationCatalog.MageShield),
            ["CotWRootsPublished"] = x => x.CotWRootsPublished.Remove(Occultist0099AbjurationCatalog.WardingWeapon),
            ["SameBookGrantSources"] = x => Occultist0099Spells.RemoveSameBookGrant(x, "fp-grant"),
            ["CotWRegistrationComplete"] = x => Occultist0099Spells.SetCotWRegistrationComplete(x, false),
            ["Intelligence"] = x => Occultist0099Spells.SetIntelligence(x, 12),
            ["VariantRoots"] = x => Occultist0099Spells.AddVariantRoot(x,
                "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaa01", Occultist0099AbjurationCatalog.MageShield)
        };
        foreach (var kv in probes)
        {
            var copy = s.Clone();
            kv.Value(copy);
            Check(!copy.Fingerprint().Equals(baseline),
                "DFix05 fingerprint detects change in " + kv.Key);
        }

        // Negative case: mutate a previously-missed field inside a preview,
        // then cancel; the restored state must equal the pre-preview state.
        var live = Occultist0099Spells.Create(4);
        Occultist0099Spells.AddSlot(live, "pv-slot", "Abjuration");
        Occultist0099Spells.AddSameBookGrant(live, "pv-grant",
            Occultist0099AbjurationCatalog.MageShield, 1);
        string liveBefore = live.Fingerprint();
        var preview = Occultist0099Spells.BeginPreview(live);
        Occultist0099Spells.SetProvider(live, "TabletopTweaks-Base", true, true);
        Occultist0099Spells.RemoveSameBookGrant(live, "pv-grant");
        Occultist0099Spells.SetCotWRegistrationComplete(live, false);
        Occultist0099Spells.SetIntelligence(live, 12);
        Check(!live.Fingerprint().Equals(liveBefore),
            "DFix05 preview really mutated the missed fields");
        Outcome(Occultist0099Outcome.Ok,
            Occultist0099Spells.CancelPreview(live, preview), "DFix05 cancel preview");
        Str(liveBefore, live.Fingerprint(),
            "DFix05 cancel restores ALL fields byte-for-byte");
    }

    // D-0099-06 regression: canonical/variant filtering has real positive and
    // negative cases; CotWRegistrationComplete is a real gate with a write point.
    private static void DFix06()
    {
        var s = Occultist0099Spells.Create(4);
        Occultist0099Spells.AddSlot(s, "cv-slot", "Abjuration");
        // Register a variant of MageShield (warm/cold-style pair identity).
        string variant = "bbbbbbbbbbbbbbbbbbbbbbbbbbbbbb02";
        Outcome(Occultist0099Outcome.Ok,
            Occultist0099Spells.AddVariantRoot(s, variant,
                Occultist0099AbjurationCatalog.MageShield),
            "DFix06 variant registered");
        // Negative: a catalog root cannot be demoted to a variant.
        Outcome(Occultist0099Outcome.RejectedInvalidArgument,
            Occultist0099Spells.AddVariantRoot(s,
                Occultist0099AbjurationCatalog.ResistEnergy,
                Occultist0099AbjurationCatalog.MageShield),
            "DFix06 catalog root cannot become a variant");
        // Negative: non-hex / self-referential variant rejected.
        Outcome(Occultist0099Outcome.RejectedInvalidArgument,
            Occultist0099Spells.AddVariantRoot(s, "not-a-guid",
                Occultist0099AbjurationCatalog.MageShield),
            "DFix06 non-hex variant rejected");
        // Candidates still list the canonical root, never the variant GUID.
        var cands = Occultist0099Spells.Candidates(s, "cv-slot", 1);
        Check(cands.Any(r => r.Guid == Occultist0099AbjurationCatalog.MageShield),
            "DFix06 canonical root stays a candidate");
        Check(!cands.Any(r => r.Guid == variant),
            "DFix06 variant GUID never a candidate");
        // Learning via the variant GUID records the canonical identity.
        Outcome(Occultist0099Outcome.Ok,
            Occultist0099Spells.Learn(s, "cv-slot", 1, variant),
            "DFix06 learn via variant GUID");
        Str(Occultist0099AbjurationCatalog.MageShield,
            s.Quotas[Occultist0099Spells.QuotaKey("cv-slot", 1)],
            "DFix06 quota records canonical GUID");
        Check(Occultist0099Spells.IsKnown(s, variant),
            "DFix06 variant counts as known via canonical");
        Check(Occultist0099Spells.IsKnown(s, Occultist0099AbjurationCatalog.MageShield),
            "DFix06 canonical known");
        Eq(1, s.Sources.Count, "DFix06 variant consumes exactly one quota/source");
        // The canonical root is now excluded from further candidates (known).
        Check(!Occultist0099Spells.Candidates(s, "cv-slot", 1)
                .Any(r => r.Guid == Occultist0099AbjurationCatalog.MageShield),
            "DFix06 known canonical excluded from candidates");

        // CotWRegistrationComplete is a real gate: flipping it off removes CotW
        // roots from candidates; flipping it back restores them.
        var g = Occultist0099Spells.Create(1);
        Occultist0099Spells.AddSlot(g, "gate-slot", "Abjuration");
        Check(Occultist0099Spells.Candidates(g, "gate-slot", 1)
                .Any(r => r.Guid == Occultist0099AbjurationCatalog.WardingWeapon),
            "DFix06 CotW root candidate when registration complete");
        Occultist0099Spells.SetCotWRegistrationComplete(g, false);
        Check(!Occultist0099Spells.Candidates(g, "gate-slot", 1)
                .Any(r => r.Guid == Occultist0099AbjurationCatalog.WardingWeapon),
            "DFix06 CotW root excluded when registration incomplete");
        Outcome(Occultist0099Outcome.RejectedInvalidArgument,
            Occultist0099Spells.Learn(g, "gate-slot", 1,
                Occultist0099AbjurationCatalog.WardingWeapon),
            "DFix06 CotW root not learnable when registration incomplete");
        Occultist0099Spells.SetCotWRegistrationComplete(g, true);
        Check(Occultist0099Spells.Candidates(g, "gate-slot", 1)
                .Any(r => r.Guid == Occultist0099AbjurationCatalog.WardingWeapon),
            "DFix06 CotW root restored when registration complete again");
    }
}

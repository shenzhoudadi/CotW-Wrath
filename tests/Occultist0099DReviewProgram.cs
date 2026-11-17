// COTW-0099 D review: independent, read-only reproduction of contract
// expectations against the B slice production sources. This host links the
// same production files; it does NOT modify them, does not start the game or
// UMM, and does not exercise any native Wrath entry point. A FAIL below means
// the candidate does not satisfy the frozen contract, not that D code broke.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using CallOfTheWild.Wrath.Prototype;

internal static class Occultist0099DReviewProgram
{
    private static readonly List<Dictionary<string, object>> findings =
        new List<Dictionary<string, object>>();

    private static void Contract(string id, string location, string expectation,
        bool satisfied, string actual)
    {
        findings.Add(new Dictionary<string, object>
        {
            ["id"] = id,
            ["location"] = location,
            ["contractExpectation"] = expectation,
            ["observedSatisfied"] = satisfied,
            ["actual"] = actual,
            ["status"] = satisfied ? "PASS" : "FAIL"
        });
    }

    private static void Main(string[] args)
    {
        string path = args.Length > 0 ? args[0] :
            "docs/work-packages/COTW-0099/D-review-assertions.json";

        // D-F01: C:23 / MF11 "same execution identity is charged at most once".
        // RemoveClass clears the dedup identity and ReacquireClass never restores it.
        var focus = Occultist0099Focus.Create();
        Occultist0099Focus.BeginCycle(focus, 8, 2, "d-review-rest-1", false);
        Occultist0099Focus.SetSchoolPresent(focus, "Abjuration", true);
        Occultist0099Focus.Invest(focus, "Abjuration", 4);
        Occultist0099Focus.Lock(focus);
        var first = Occultist0099Focus.PaySchool(focus, "Abjuration", 1,
            "same-execution").Outcome;
        Occultist0099Focus.RemoveClass(focus);
        Occultist0099Focus.ReacquireClass(focus);
        var second = Occultist0099Focus.PaySchool(focus, "Abjuration", 1,
            "same-execution").Outcome;
        Contract("D-F01", "src/Occultist0099Rules.cs:646,650-657",
            "C-0099-20261115-r1 O-S01 native entry / MF11: the same execution " +
            "identity is charged at most once across class remove+reacquire",
            second != Occultist0099Outcome.Ok,
            "first payment " + first + ", second payment " + second +
            ", Spent=" + focus.Spent + " (expected at most 1)");

        // D-F02: C:31 "Int >= 10 + circle" gate for learning and casting.
        // Field presence alone was a weak assertion (D-0099-04); this now runs
        // the real gate behaviourally, both directions, on the same level.
        var stateFields = typeof(Occultist0099SpellState).GetFields()
            .Select(f => f.Name).ToArray();
        bool hasIntState = stateFields.Any(n =>
            n.IndexOf("Intelligence", StringComparison.OrdinalIgnoreCase) >= 0 ||
            n.IndexOf("IntModifier", StringComparison.OrdinalIgnoreCase) >= 0);
        var lowInt = Occultist0099Spells.Create(4, 10);
        Occultist0099Spells.AddSlot(lowInt, "int-gate", "Abjuration");
        int lowCircle = Occultist0099Spells.MaxCircle(lowInt);
        bool lowHasNoCircle2 = !lowInt.Quotas.ContainsKey(
            Occultist0099Spells.QuotaKey("int-gate", 2));
        // circle 2 root, so the ONLY difference between the two characters is Int
        string circle2Root = Occultist0099AbjurationCatalog.ResistEnergy;
        var lowLearn = Occultist0099Spells.Learn(lowInt, "int-gate", 2, circle2Root);
        var highInt = Occultist0099Spells.Create(4, 16);
        Occultist0099Spells.AddSlot(highInt, "int-gate", "Abjuration");
        int highCircle = Occultist0099Spells.MaxCircle(highInt);
        var highLearn = Occultist0099Spells.Learn(highInt, "int-gate", 2, circle2Root);
        bool highCanCast = Occultist0099Spells.CanCast(highInt, circle2Root);
        Occultist0099Spells.SetIntelligence(highInt, 11);
        bool dropBlocksCast = !Occultist0099Spells.CanCast(highInt, circle2Root);
        bool dropKeepsQuota = highInt.Quotas[
            Occultist0099Spells.QuotaKey("int-gate", 2)] == circle2Root;
        Occultist0099Spells.SetIntelligence(highInt, 16);
        bool restoreCasts = Occultist0099Spells.CanCast(highInt, circle2Root);
        Contract("D-F02", "src/Occultist0099Spells.cs (MaxCircle/IntAllowsCircle/" +
            "CanCast/SetIntelligence/Learn)",
            "C-0099-20261115-r1 O-S02: learning/casting gate Int >= 10 + circle " +
            "must be expressible and enforced",
            hasIntState && lowCircle == 0 && lowHasNoCircle2 &&
            lowLearn != Occultist0099Outcome.Ok && highCircle == 2 &&
            highLearn == Occultist0099Outcome.Ok && highCanCast &&
            dropBlocksCast && dropKeepsQuota && restoreCasts,
            "INT field present=" + hasIntState +
            "; L4/Int10 maxCircle=" + lowCircle + ", circle2 quota absent=" +
            lowHasNoCircle2 + ", learn=" + lowLearn +
            "; L4/Int16 maxCircle=" + highCircle + ", learn=" + highLearn +
            ", cast=" + highCanCast +
            "; Int drop to 11 blocks cast=" + dropBlocksCast +
            ", keeps established quota=" + dropKeepsQuota +
            "; Int restored casts again=" + restoreCasts);

        // D-F03: C:22 preview cancel must restore every field, and the
        // byte-for-byte assertions in the B host rely on SpellState.Fingerprint.
        // Every mutable field is now probed, not just Providers/PublishedLists.
        var spells = Occultist0099Spells.Create(4, 16);
        Occultist0099Spells.AddSlot(spells, "fp", "Abjuration");
        var mutations = new Dictionary<string, Action<Occultist0099SpellState>>
        {
            ["ClassLevel"] = x => Occultist0099Spells.SetLevel(x, 7),
            ["Intelligence"] = x => Occultist0099Spells.SetIntelligence(x, 18),
            ["Providers"] = x => Occultist0099Spells.SetProvider(x,
                "TabletopTweaks-Base", true, true),
            ["DlcAllowed"] = x => x.DlcAllowed.Add("dlc-probe"),
            ["CotWRootsPublished"] = x => x.CotWRootsPublished.Add("cotw-probe"),
            ["ForeignBookRoots"] = x => Occultist0099Spells.AddForeignBookRoot(x,
                "ffffffffffffffffffffffffffffff01"),
            ["CotWRegistrationComplete"] = x =>
                Occultist0099Spells.SetCotWRegistrationComplete(x, false),
            ["VariantRoots"] = x => Occultist0099Spells.AddVariantRoot(x,
                "ffffffffffffffffffffffffffffff02",
                Occultist0099AbjurationCatalog.MageShield),
            ["Quotas+Sources"] = x => Occultist0099Spells.Learn(x, "fp", 1,
                Occultist0099AbjurationCatalog.MageShield),
            ["SameBookGrantSources"] = x => Occultist0099Spells.AddSameBookGrant(x,
                "grant-probe", Occultist0099AbjurationCatalog.MageShield, 1)
        };
        var uncovered = new List<string>();
        foreach (var probe in mutations)
        {
            var live = Occultist0099Spells.Create(4, 16);
            Occultist0099Spells.AddSlot(live, "fp", "Abjuration");
            string baseline = live.Fingerprint();
            probe.Value(live);
            if (live.Fingerprint() == baseline) uncovered.Add(probe.Key);
        }
        string before = spells.Fingerprint();
        Occultist0099Spells.SetProvider(spells, "TabletopTweaks-Base", true, true);
        string after = spells.Fingerprint();
        Contract("D-F03", "src/Occultist0099Spells.cs (Fingerprint/Clone/" +
            "CancelPreview); tests/Occultist0099SliceProgram.cs MF09/SP03",
            "C-0099-20261115-r1 O-S01/O-S02: preview cancel byte-for-byte " +
            "equivalence must be verifiable over all mutable fields",
            before != after && uncovered.Count == 0,
            "changing Providers/PublishedLists leaves Fingerprint identical=" +
            (before == after) + "; mutable fields still outside the fingerprint=" +
            (uncovered.Count == 0 ? "none" : string.Join(",", uncovered)) +
            " (" + mutations.Count + " fields probed)");

        // D-F04: C:32-33 canonical root / variant / sticky-touch filtering must
        // have a real negative case. The Abjuration catalog legitimately has no
        // variants (Fire Shield is Evocation, later package), so the fix adds a
        // writable variant mechanism; verify it actually filters and resolves.
        var cv = Occultist0099Spells.Create(4);
        Occultist0099Spells.AddSlot(cv, "cv", "Abjuration");
        string variantGuid = "cccccccccccccccccccccccccccccc03";
        var addVariant = Occultist0099Spells.AddVariantRoot(cv, variantGuid,
            Occultist0099AbjurationCatalog.MageShield);
        bool variantExcluded = !Occultist0099Spells.Candidates(cv, "cv", 1)
            .Any(r => r.Guid == variantGuid);
        bool canonicalPresent = Occultist0099Spells.Candidates(cv, "cv", 1)
            .Any(r => r.Guid == Occultist0099AbjurationCatalog.MageShield);
        var learnViaVariant = Occultist0099Spells.Learn(cv, "cv", 1, variantGuid);
        bool quotaCanonical = cv.Quotas[Occultist0099Spells.QuotaKey("cv", 1)] ==
            Occultist0099AbjurationCatalog.MageShield;
        bool oneSource = cv.Sources.Count == 1;
        bool variantKnown = Occultist0099Spells.IsKnown(cv, variantGuid);
        Contract("D-F04", "src/Occultist0099Spells.cs (AddVariantRoot/CanonicalOf/" +
            "Candidates/Learn/IsKnown)",
            "C-0099-20261115-r1 O-S02: canonical/variant identity filtering has " +
            "both positive and negative cases (Fire Shield warm/cold, sticky touch)",
            addVariant == Occultist0099Outcome.Ok && variantExcluded &&
            canonicalPresent && learnViaVariant == Occultist0099Outcome.Ok &&
            quotaCanonical && oneSource && variantKnown,
            "variant registered=" + (addVariant == Occultist0099Outcome.Ok) +
            ", variant excluded from candidates=" + variantExcluded +
            ", canonical present=" + canonicalPresent +
            ", learn via variant=" + learnViaVariant +
            ", quota records canonical=" + quotaCanonical +
            ", single source=" + oneSource + ", variant known=" + variantKnown +
            "; catalog itself has no variants (correct for Abjuration)");

        var report = new Dictionary<string, object>
        {
            ["workPackage"] = "COTW-0099",
            ["stage"] = "D counterexample reproduction, rerun after the B fix round",
            ["reviewedCandidate"] = "b6be98b",
            ["recheckTarget"] = "B fix-round working tree on feat/cotw-0099-occultist",
            ["contract"] = Occultist0099Ids.Contract,
            ["SP08"] = "See the native graph report. Owner-token simulation retired.",
            ["checkType"] = "isolated contract reproduction over linked production sources",
            ["acceptanceScope"] = "d_counterexample_reproduction_only",
            ["nativeEntryPointsExercised"] = false,
            ["nativeWrathEntriesExercised"] = false,
            ["gameOrUmmLaunched"] = false,
            ["productionSourcesModified"] = false,
            ["releaseEligible"] = false,
            ["independentReviewer"] = false,
            ["resultMeaning"] = "Each D finding is reproduced against the frozen " +
                "contract on the current production sources. PASS means the " +
                "counterexample no longer reproduces; it is NOT native-entry " +
                "acceptance and NOT an independent D sign-off (the reviewer is " +
                "also the B implementer). See the native host report for " +
                "entry-point evidence.",
            ["result"] = findings.All(f => (string)f["status"] == "PASS")
                ? "REVIEW_PASS" : "NEEDS_FIX",
            ["findings"] = findings
        };
        var full = Path.GetFullPath(path);
        Directory.CreateDirectory(Path.GetDirectoryName(full));
        File.WriteAllText(full, JsonSerializer.Serialize(report,
            new JsonSerializerOptions { WriteIndented = true }));
        foreach (var f in findings)
            Console.WriteLine(f["id"] + " " + f["status"] + ": " + f["actual"]);
        Console.WriteLine("D contract reproduction: " +
            findings.Count(x => (string)x["status"] == "FAIL") + " FAIL / " +
            findings.Count + " checks. Native Wrath entries were NOT exercised.");
    }
}

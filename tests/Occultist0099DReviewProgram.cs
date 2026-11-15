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
        // Neither the state nor any learning/candidate entry carries INT.
        var stateFields = typeof(Occultist0099SpellState).GetFields()
            .Select(f => f.Name).ToArray();
        bool hasIntState = stateFields.Any(n =>
            n.IndexOf("Intelligence", StringComparison.OrdinalIgnoreCase) >= 0 ||
            n.IndexOf("IntModifier", StringComparison.OrdinalIgnoreCase) >= 0);
        var learnParams = typeof(Occultist0099Spells).GetMethod("Learn")
            .GetParameters().Select(p => p.Name).ToArray();
        var candidateParams = typeof(Occultist0099Spells).GetMethod("Candidates")
            .GetParameters().Select(p => p.Name).ToArray();
        Contract("D-F02", "src/Occultist0099Spells.cs:133-147,197-209,215-227,318-355",
            "C-0099-20261115-r1 O-S02: learning/casting gate Int >= 10 + circle " +
            "must be expressible and enforced",
            hasIntState,
            "no INT field in SpellState (" + string.Join(",", stateFields) +
            "), Learn(" + string.Join(",", learnParams) + "), Candidates(" +
            string.Join(",", candidateParams) + "); MaxCircle/Learn use level only");

        // D-F03: C:22 preview cancel must restore every field, and the
        // byte-for-byte assertions in the B host rely on SpellState.Fingerprint.
        var spells = Occultist0099Spells.Create(4);
        string before = spells.Fingerprint();
        Occultist0099Spells.SetProvider(spells, "TabletopTweaks-Base", true, true);
        string after = spells.Fingerprint();
        Contract("D-F03", "src/Occultist0099Spells.cs:149-162; " +
            "tests/Occultist0099SliceProgram.cs:431,601-606",
            "C-0099-20261115-r1 O-S01/O-S02: preview cancel byte-for-byte " +
            "equivalence must be verifiable over all mutable fields",
            before != after,
            "changing Providers/PublishedLists leaves Fingerprint identical=" +
            (before == after) + "; 7 mutable fields are outside the fingerprint");

        // D-F04: C:32-33 canonical root / variant / sticky-touch filtering.
        // Every slice root is its own canonical with IsVariant=false, so the
        // canonical filter in Candidates has no negative case.
        bool hasVariantData = Occultist0099AbjurationCatalog.Roots.Any(r =>
            r.IsVariant || r.Guid != r.CanonicalGuid);
        Contract("D-F04", "src/Occultist0099Spells.cs:74-85,292-313,318-332",
            "C-0099-20261115-r1 O-S02: canonical/variant identity filtering has " +
            "both positive and negative cases (Fire Shield warm/cold, sticky touch)",
            hasVariantData,
            "all " + Occultist0099AbjurationCatalog.Roots.Length +
            " roots have CanonicalGuid==Guid and IsVariant=false; " +
            "r.Guid==r.CanonicalGuid is a tautology in Candidates");

        // D-F05: SP08 registration atomicity is asserted against a self-made
        // toy object. ForeignDataTouched is never set true on any path, so
        // "foreign data intact" has no distinguishing negative case.
        var reg = new Occultist0099Registration();
        reg.TryPublish(new[] { "core-a" }, new[] { "core-a" }, true, 3);
        bool collisionMarksForeign = reg.ForeignDataTouched;
        reg.TryPublish(new[] { "core-a" }, new[] { "core-a" }, false, 3);
        bool successMarksForeign = reg.ForeignDataTouched;
        Contract("D-F05", "src/Occultist0099Spells.cs:506-538; " +
            "tests/Occultist0099SliceProgram.cs:748-772",
            "C-occultist-cases.json SP08: foreign GUID collision fixture must be " +
            "able to distinguish foreign data being touched from intact",
            collisionMarksForeign || successMarksForeign,
            "ForeignDataTouched is false on both collision and success paths; " +
            "the assertion has no negative case and the toy object never reads " +
            "a blueprint cache or a real GUID");

        var report = new Dictionary<string, object>
        {
            ["workPackage"] = "COTW-0099",
            ["stage"] = "D review of B first slice (Abjuration), production read-only",
            ["candidate"] = "b6be98b",
            ["contract"] = Occultist0099Ids.Contract,
            ["checkType"] = "isolated contract reproduction over linked production sources",
            ["nativeWrathEntriesExercised"] = false,
            ["gameOrUmmLaunched"] = false,
            ["productionSourcesModified"] = false,
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

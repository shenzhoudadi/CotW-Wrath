using System;
using System.IO;
using System.Linq;

// Single source of truth for the three filesystem anchors the offline suite
// needs. Historically every test re-implemented these by walking up until a
// HANDOFF.md was found, and several others hard-coded D:\clew\outputs or
// D:/steam. HANDOFF.md later moved under docs/agent-handoff, so the walk is
// anchored on the project file instead and the evidence tree is derived from
// the repository location (repo = <evidence>/code/wotr).
internal static class TestPaths
{
    static string repoRoot;

    internal static string RepoRoot
    {
        get
        {
            if (repoRoot != null) return repoRoot;
            var configured = Environment.GetEnvironmentVariable("COTW_REPO_ROOT");
            if (!string.IsNullOrWhiteSpace(configured) && File.Exists(Path.Combine(configured, "CallOfTheWild.Wrath.Prototype.csproj")))
                return repoRoot = Path.GetFullPath(configured);
            // Walk up from the working directory and the host binary, then walk
            // back down through code/wotr so the suite also runs from a parent
            // such as D:\clew where the project is not on the ancestor chain.
            foreach (var anchor in new[] { Environment.CurrentDirectory, AppDomain.CurrentDomain.BaseDirectory })
            {
                for (var dir = new DirectoryInfo(anchor); dir != null; dir = dir.Parent)
                {
                    if (File.Exists(Path.Combine(dir.FullName, "CallOfTheWild.Wrath.Prototype.csproj"))) return repoRoot = dir.FullName;
                    var nested = Path.Combine(dir.FullName, "code", "wotr");
                    if (File.Exists(Path.Combine(nested, "CallOfTheWild.Wrath.Prototype.csproj"))) return repoRoot = nested;
                }
            }
            throw new DirectoryNotFoundException("Project root (CallOfTheWild.Wrath.Prototype.csproj) not found from " + Environment.CurrentDirectory + "; set COTW_REPO_ROOT.");
        }
    }

    // <evidence>/code/wotr -> <evidence>. Holds work/ and outputs/.
    internal static string EvidenceRoot
    {
        get
        {
            for (var dir = new DirectoryInfo(RepoRoot); dir != null; dir = dir.Parent)
                if (Directory.Exists(Path.Combine(dir.FullName, "work")) && Directory.Exists(Path.Combine(dir.FullName, "outputs")))
                    return dir.FullName;
            return RepoRoot;
        }
    }

    internal static string GameRoot
    {
        get
        {
            var configured = Environment.GetEnvironmentVariable("COTW_WRATH_PATH");
            if (!string.IsNullOrWhiteSpace(configured) && Directory.Exists(configured)) return configured;
            foreach (var drive in new[] { "D:", "C:", "E:", "F:" })
            {
                var candidate = Path.Combine(drive + Path.DirectorySeparatorChar, "steam", "steamapps", "common", "Pathfinder Second Adventure");
                if (Directory.Exists(candidate)) return candidate;
            }
            throw new DirectoryNotFoundException("Wrath install not found; set COTW_WRATH_PATH.");
        }
    }

    internal static string Work(params string[] parts) => Path.Combine(new[] { EvidenceRoot, "work" }.Concat(parts).ToArray());

    // The frozen copy under outputs/ was pruned when the evidence tree was
    // reorganised, so fall back to the live repository tree of the same name.
    internal static string Outputs(params string[] parts)
    {
        var frozen = Path.Combine(new[] { EvidenceRoot, "outputs", "CallOfTheWild.Wrath.Prototype" }.Concat(parts).ToArray());
        if (File.Exists(frozen) || Directory.Exists(frozen)) return frozen;
        return Path.Combine(new[] { RepoRoot }.Concat(parts).ToArray());
    }

    internal static string Game(params string[] parts) => Path.Combine(new[] { GameRoot }.Concat(parts).ToArray());
}

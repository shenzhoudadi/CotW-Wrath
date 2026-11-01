using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using Kingmaker.Blueprints.Facts;
using Kingmaker.Blueprints.JsonSystem;
using CallOfTheWild.Wrath.Prototype;

// Tests the shipped assembly's original PNG bytes and registered blueprint
// associations. PsychicExpansionTests substitutes managed Sprite objects for
// Unity image loading; these checks do not claim to exercise Unity rendering.
internal static class CooperationIcon01414Tests
{
    private static readonly string[][] Groups = {
        new[] { "BondedMind", "37de578d68385aadd11504f87620cef90da036545ea6d69bc05793b8cc5557c5", "BondedMindFeat" },
        new[] { "ShareSpells", "4975d8d74f808d03eb17131871fc822aea6164e93202643a4299ab5e66c6cde6", "ShareSpellsFeat", "BondedShareToggle", "BondedShareEnabled" },
        new[] { "BrokenWingGambit", "0ef2612bd042dff503a0560e27a34172fb3000d23559887924eaedf683d56871", "BrokenWingGambitFeat", "BrokenWingToggle", "BrokenWingEnabled" },
        new[] { "WoundedPawGambit", "21d684edce625e34f2cd85dfdd0a438c897b562c2ff18cdae56c8c8dd085dbf9", "WoundedPawGambitFeat", "WoundedPawToggle", "WoundedPawEnabled" },
        new[] { "StunningAssault", "4687aaf7ea530cade774d674b33c0c7e79e14d655ac584e41f3d77a42301ac5b", "StunningAssaultFeat", "StunningAssaultAbility", "StunningAssaultArmed" }
    };

    internal static int Run(BlueprintsCache cache)
    {
        int assertions = 0;
        void Check(bool condition, string message) {
            if (!condition) throw new Exception("Cooperation icons 0.14.14: " + message);
            assertions++;
        }

        var assembly = typeof(BrokenWingGambit).Assembly;
        var earlierIcons = new List<object>();
        foreach (var group in Groups) {
            string resource = "CotW." + group[0] + ".png";
            using (var stream = assembly.GetManifestResourceStream(resource)) {
                Check(stream != null, "embedded resource exists: " + resource);
                using (var hash = SHA256.Create()) {
                    string actual = string.Concat(hash.ComputeHash(stream).Select(value => value.ToString("x2")));
                    Check(actual == group[1], "embedded PNG matches original CotW bytes: " + resource);
                }
            }

            var root = Psychic034Tests.Asset<BlueprintUnitFact>(cache, group[2]);
            Check(root != null, "root blueprint registered: " + group[2]);
            object icon = root.Icon;
            Check(!ReferenceEquals(icon, null), "root icon assigned: " + group[2]);
            foreach (var previous in earlierIcons)
                Check(!ReferenceEquals(icon, previous), "different feats retain independent original icons: " + group[2]);
            earlierIcons.Add(icon);

            foreach (string token in group.Skip(2)) {
                var blueprint = Psychic034Tests.Asset<BlueprintUnitFact>(cache, token);
                Check(blueprint != null, "associated blueprint registered: " + token);
                Check(ReferenceEquals(blueprint.Icon, icon), "associated blueprint uses its feat icon: " + token);
            }
        }

        PsychicExpansionTests.Inventory["cooperationIcons01414Assertions"] = assertions;
        Console.WriteLine("PASS: " + assertions + " original cooperation PNG resource/hash and 13 blueprint icon association checks; Unity decoding/rendering bounded.");
        return assertions;
    }
}

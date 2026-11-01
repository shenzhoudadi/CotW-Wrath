using System;
using CallOfTheWild.Wrath.Prototype;

int assertions = 0;
// Expected FINAL attribute contributions from the original feat description.
foreach (var (wisdom, charisma, expectedContribution) in new[]
{
    (3, 4, 4), (-1, 4, 3), (3, -2, -2), (-1, -2, -3),
    (0, 4, 4), (3, 0, 0), (0, 0, 0), (-2, 0, -2), (5, 2, 2)
})
{
    var actual = wisdom + SteadfastRules.Adjustment(wisdom, charisma);
    if (actual != expectedContribution)
        throw new Exception($"WIS {wisdom}, CHA {charisma}: expected {expectedContribution}, got {actual}");
    assertions++;
}
for (int mask = 0; mask < 16; mask++)
{
    bool actual = SteadfastRules.Applies((mask & 1) != 0, (mask & 2) != 0,
        (mask & 4) != 0, (mask & 8) != 0);
    if (actual != (mask == 15)) throw new Exception($"Invalid trigger conditions: {mask}");
    assertions++;
}
Console.WriteLine($"PASS: {assertions} rule assertions. This does not exercise Unity or an in-game save.");

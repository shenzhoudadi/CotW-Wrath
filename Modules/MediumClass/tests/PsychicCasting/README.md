# Medium psychic casting verification

This net472 executable loads the final merged MediumClass DLL, the installed game managed types, and CotW 0.6.0. Pass the game Managed directory, merged MediumClass DLL, selected Harmony DLL, CotW DLL, and JSON output path. Run in a fresh process for each Harmony version.

The 153 assertions cover thought/emotion, suppressed/inactive effects, centering, items and spell-like powers, two native silence sources versus other casting prohibitions, polymorph, Logical collection/cost/labels, nine actual Harmony targets, both CotW load orders and owner removal. Unity profiling is bypassed; entities and buffs are seeded. This is not a game, combat, UI or real save-pipeline test.

Build with `dotnet build tests/PsychicCasting/PsychicCasting.csproj -c Release -p:GameManaged=<path> -p:HarmonyReference=<path>`. Compilation requires the supplied game/Harmony references; they are not redistributed. The original regression projects retain their explicit API-model scope. For the old-Harmony action-bar host use `-p:TestFramework=net472` and `-p:HarmonyReferencePath=<path>`.

The additional checks separate the actual production Marshal passive/active scopes, then call the merged cleanup helper on seeded native fact/cache objects. Native fact removal and disposal run without replacement; two spirit abilities are removed, while a referenced native ability, permanent Center Self and the SpiritPower level counter remain. The fixture seeds inactive facts and cached blueprint references. It does not execute AddFacts activation, UI events, rest, or a real save lifecycle.

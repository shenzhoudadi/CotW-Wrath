# Whole module registration

Project: `WayShieldBlueprintTests.csproj` (the unique project name avoids the parent `BlueprintTests` output directory).

First run `tests/Extract-Native.py <installed-game-directory> <native.json-output>`. It derives dependencies from this module and the parent combat pool repair, reads the installed `blueprints.zip`, preserves native JSON/name/path/SHA data, and records the complete native identity inventory.

Build with the installed game's `WrathPath`. The suite references the module's generated `obj/game-reference/Assembly-CSharp.dll`, whose native method bodies are unchanged and whose private/internal visibility is widened for fixture execution. Harmony resolves directly from the installed game with the pinned version/SHA; no test-side Harmony replacement is loaded.

Run `WayShieldBlueprintTests.exe` with:

1. Installed `Wrath_Data/Managed` directory.
2. Freshly built `Way of the shield.dll` path.
3. Extracted `native.json` path.
4. Result JSON path.
5. Optional freshly built parent `CallOfTheWild.Wrath.Prototype.dll` path.

The optional fifth argument tests real parent reconciliation across its native pool inventory, nine optional provider contracts, complete three-anchor clones, restricted submenu protection, and the external provider Light Shield Parry fallback. It checks both arrays, old-entry preservation, repeated registration and shared engine identity. These contracts do not claim execution of external provider assemblies.

The fixture reads real native components, equipment, selection and progression fields before calling the actual `Main.ConfigureIntegrated`. It verifies all owned blueprints after native OnEnable, requested feature/mythic/ability/archetype entrances, GUID uniqueness, prerequisites, cache identity and idempotence. Runtime target evidence comes from `Harmony.GetAllPatchedMethods` and owner metadata after real installation.

Only icon loading, profiling/log output and root service access are isolated; root contents and gameplay fields remain the native records. The suite does not launch a Unity scene or validate combat, leveling UI or save reload. Ordinary CLR execution with the original unmodified assembly has a separately reported private-access limit; the normal suite uses the visibility-adjusted reference and does not imply Mono scene acceptance.

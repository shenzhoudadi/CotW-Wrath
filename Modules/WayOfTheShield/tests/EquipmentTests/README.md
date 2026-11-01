# Equipment native checks

Build the module first, then this `EquipmentTests.csproj`, using the installed game's `WrathPath` property. The parent `Directory.Build.props/targets` validates the exact game Harmony 2.0.4.0 version and SHA and removes test-side competing runtime copies.

Run the built `EquipmentTests.exe` with two arguments:

1. Absolute installed `Wrath_Data/Managed` directory.
2. Absolute freshly built `Way of the shield.dll` path.

The suite resolves game dependencies directly from the first argument. Its source has no CSV, external fixtures, packages or extracted-blueprint prerequisites. The module itself must already have been compiled against the generated game reference.

Checks execute Harmony against actual installed native methods, audit real equipment/ACP/default-activation IL, verify nested insertion finalizers, run production non-proficiency callbacks against native unit/proficiency/rule/bonus objects, verify ordinary units remain unaffected, and cover forced TWF values. Scene-dependent direct-control membership is the sole runtime boundary for these callbacks; it does not replace proficiency, category or native rule modifier behavior.

Output: `equipment-smoke-result.json` beside the supplied module DLL, plus a console assertion count. The suite restores its Harmony owner in `finally`. It does not install the mod, launch a Unity scene, or claim in-game combat/save/UI acceptance.

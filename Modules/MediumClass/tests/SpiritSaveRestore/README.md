# Spirit save/restore regression

Run `dotnet run --project tests/SpiritSaveRestore/SpiritSaveRestore.csproj -- "<game>/Wrath_Data/Managed/Assembly-CSharp.dll" "<stage>/MediumClass.dll"` from the source directory. The second argument adds checks of the actual compiled delivery DLL's serialized fields and lifecycle/patch wiring.

This suite links the production UnitPartMedium, catalogue lifecycle, recovery prefix, ApplySpirits, focus, weaker-channel and mastery components. It serializes a real Newtonsoft JSON payload using the same opt-in member policy as the installed game's OptInContractResolver, deserializes a new part with an empty runtime catalogue, and executes restoration. Blueprint references and game facts are explicit API doubles. It is not an actual .zks save, Unity scene, or game UI test.

Cases include both Medium and Prowler, one/two/four formal spirits, preserved primary and secondary choices, forgone powers, zero/one remaining free surge, temporary turn-off during saves/scene changes, missing legacy part, primary recovery from the saved buff source action, focus before catalogue initialization, repeat restoration, actual catalogue feature removal, and a missing channel buff. Native IL checks confirm the opt-in member policy, pre-save TurnOff path, and UnitDescriptor load boundary.

The source dictionary is intentionally not serialized. Only choices and remaining daily counts persist; the catalogue is rebuilt from owned class facts. Complement this suite with SpiritSpellcastingScenarios and SpiritActionBar for book/slot/UI behavior, and MythicRegistration for orphan-buff click protection.

0.3.4：恢复入口先执行通灵者进度恢复（此处为计数桩，真实逻辑由 `tests/MediumProgressionLevels` 覆盖），并把 0.3.3 的五个恶咒开关规范为最早开启的两个（幂等，其他 buff 不动）。

0.3.5 adds the production MediumSpiritPowerCleanup helper. Regressions cover all six primary spirits, orphan child ability facts with missing parent/selection/catalogue, reentrant clear callbacks, native shared abilities and permanent counters remaining, and no-channel old-load repair versus an inactive-but-present marker. These are explicit API-model scenarios; native deletion/cache behavior has a separate managed fixture in PsychicCasting.

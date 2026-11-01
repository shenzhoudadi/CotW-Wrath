# Medium progression by class level, and recovery of 0.3.3 saves (0.3.4)

```
dotnet run --project tests/MediumProgressionLevels/MediumProgressionLevels.csproj -- \
  <game>/Wrath_Data/Managed/Assembly-CSharp.dll <game>/blueprints.zip artifacts/Release/MediumClass/MediumClass.dll
```

All three arguments are optional (skipped inputs are reported). The suite:

1. reads the real `Assembly-CSharp.dll` IL: `BlueprintProgression.CalcLevel` returns the character level when the progression
   has no classes and no archetypes; `LevelUpHelper.UpdateProgression` grants the entries up to `CalcLevel`;
   `ApplyClassMechanics.ApplyProgressions` re-runs it for every progression on every level-up;
2. reads the real blueprints: the native Cavalier, Bard and Bloodrager progressions list exactly their own class;
3. runs the production `MediumProgression.Configure` (BlueprintCore configurator doubles record the calls) and the production
   `MediumProgressionMigration` on a transcription of the native level-up rules: Knight 1 -> Medium 1, Knight 7 -> Medium 1,
   every lead of 0..19 other-class levels, 300 seeded random interleavings, Spirit Binder bonus feats at Medium levels only;
4. recovers characters shaped like 0.3.3 saves (progression without class): missing kit granted, early entries removed, foreign
   sources untouched, unverifiable multi-rank features and Binder selections left alone and reported, idempotent, and the
   recovered character keeps levelling correctly; feats picked through a selection (recorded with the progression as their
   source by the native level-up) and the selection facts survive every load; ranks at the cap are not planned again;
5. reads the delivery DLL (Cecil): `MediumProgression.Configure` sets its class and the restore entry runs the recovery first.

Negative controls: pass the 0.3.3 `Medium/MediumProgression.cs` as `-p:ProductionProgression=<path>` and/or the 0.3.3 delivery DLL
as the third argument. Set `MEDIUM_TEST_CONTINUE=1` to list every failing check instead of stopping at the first.

The native level-up rules are a transcription (decompiled from the shipped assembly), not the game itself; nothing here runs Unity.

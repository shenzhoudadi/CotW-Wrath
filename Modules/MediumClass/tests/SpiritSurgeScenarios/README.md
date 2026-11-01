# Spirit Surge scenarios (0.3.4)

Runs the actual production `MediumSpiritSurgeComponent`, `SpiritSurgeMath` and `SpiritSurgeReport` on explicit game API doubles.
A deterministic die source records every draw. It checks:

- exactly one draw per effect instance (1d6/1d8/1d10 by level, Prowler d6, Legendary Marshal d6, Hardened Soul two independent
  d8), reused by every stat, every concentration query and every refresh (`OnTurnOff`/`OnTurnOn`);
- a repeated `OnTurnOn` does not stack, a new cast rolls again, characters and Orders allies never share a roll;
- the per-instance data is round-tripped through Newtonsoft with the game's opt-in member policy: loading draws nothing and
  keeps the roll, the log line is not repeated, an old save without data rolls once, damaged data is rerolled once;
- the report that feeds the log lists the granting medium's spirit bonus and focus, the totals equal the applied modifiers and
  the concentration provider, and a receiver's own attunement never replaces the granter's numbers;
- a failing log never removes the bonus.

Negative control (0.3.3 component, expected to FAIL at the first "one draw per effect" check):

```
dotnet build tests/SpiritSurgeScenarios/SpiritSurgeScenarios.csproj -p:ProductionSurgeComponent=<0.3.3 source>/Medium/NewComponents/AbilitySpecific/MediumSpiritSurgeComponent.cs -o <out>
mono <out>/SpiritSurgeScenarios.exe   # or run the .exe directly on Windows
```

This does not run Unity or an actual save. Run `dotnet run --project tests/SpiritSurgeScenarios/SpiritSurgeScenarios.csproj` on Windows
(.NET Framework 4.7.2) or, on Linux, build with `dotnet build` and run the exe with Mono.

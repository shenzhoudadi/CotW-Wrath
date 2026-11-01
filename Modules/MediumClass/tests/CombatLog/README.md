# Combat log rendering and failure isolation (0.3.4)

Run `dotnet run --project tests/CombatLog/CombatLog.csproj`.

Links production `Utilities/CombatLog.cs`, `Medium/SpiritSurgeReport.cs` and `Medium/SpiritSurgeMath.cs`, and renders the
real templates from the shipped `Strings/Medium.json` (the file is copied next to the test). Checks, with exact strings in
both languages:

- the line names the recipient, the dice type and every individual die (`1d6 = 4`, `2d8 = 3 + 6 = 9`), then one segment per
  target group with each term (roll, the granting Marshal's spirit bonus, spirit focus; zero terms are omitted) and the
  final `= +N`, including the concentration segment;
- a damaged or missing localized template (missing placeholders, extra placeholders, lookup failure) falls back to the
  built-in wording instead of dropping a value or throwing;
- empty or null reports write nothing; UI failure, malformed format, missing threads, failing logger creation and failing
  fallback logging never escape, and the warning is attempted once.

These are deliberately throwable API doubles, not claims that those failures normally occur in the game. Rendering and native
thread registration need in-game testing; native signatures are checked during the production build against the target game's
publicized references.

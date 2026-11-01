#!/usr/bin/env bash
# Runs the new 0.3.4 checks against the 0.3.3 code; every command below is EXPECTED to fail.
#
#   PKG=<data package root> OLD=<0.3.3 source dir> OLDDLL=<0.3.3 MediumClass.dll> scripts/run_negative_controls.sh <output dir>
set -uo pipefail
: "${PKG:?}" "${OLD:?}" "${OLDDLL:?}"
SRC=$(cd "$(dirname "$0")/.." && pwd); OUT=$(mkdir -p "$1" && cd "$1" && pwd); cd "$SRC"
GAME_CS=$PKG/GameReferences/Wrath_Data/Managed/Assembly-CSharp.dll; BPZ=$PKG/GameReferences/blueprints.zip
export FrameworkPathOverride=${FrameworkPathOverride:-$(ls -d "$HOME"/.nuget/packages/microsoft.netframework.referenceassemblies.net472/*/build/.NETFramework/v4.7.2 | head -1)}
echo "== progression: 0.3.3 MediumProgression.cs (lists every failing check)"
MEDIUM_TEST_CONTINUE=1 dotnet run --project tests/MediumProgressionLevels/MediumProgressionLevels.csproj "-p:ProductionProgression=$OLD/Medium/MediumProgression.cs" -- "$GAME_CS" "$BPZ" "$OLDDLL" > "$OUT/progression-0.3.3-source.log" 2>&1; echo "rc=$? (expected non-zero)"
echo "== surge: 0.3.3 MediumSpiritSurgeComponent.cs"
dotnet build tests/SpiritSurgeScenarios/SpiritSurgeScenarios.csproj "-p:ProductionSurgeComponent=$OLD/Medium/NewComponents/AbilitySpecific/MediumSpiritSurgeComponent.cs" -o "$OUT/surge-old" > "$OUT/surge-build.log" 2>&1
mono "$OUT/surge-old/SpiritSurgeScenarios.exe" > "$OUT/surge-0.3.3-source.log" 2>&1; echo "rc=$? (expected non-zero)"
echo "== death curse: 0.3.3 delivery DLL"
dotnet run --project tests/SpiritInfluenceScenarios/SpiritInfluenceScenarios.csproj -- "$OLDDLL" Strings/Feats.json > "$OUT/deathcurse-0.3.3-dll.log" 2>&1; echo "rc=$? (expected non-zero)"

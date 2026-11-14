#!/usr/bin/env bash
# Runs every offline suite on Linux (dotnet 9 SDK + Mono for the net472 suites).
#
#   PKG=<extracted data package root> WOTR_GAME_PATH=<installed game root> scripts/run_linux_tests.sh <output dir> [suite ...]
#
# Run scripts/linux-build-and-stage.sh first (several suites read artifacts/Release/MediumClass/MediumClass.dll and
# bin/Release/net472/BlueprintCore.dll). Linux differences from the Windows instructions in each tests/*/README.md:
#  * Only the audited game Harmony 2.3.6.0 is allowed. A Mono/native detour failure
#    is a failed suite; never substitute another Harmony or execute stale output.
#  * The mandatory release regression runs on Windows. This auxiliary Linux host
#    does not establish native Linux detour support.
#  * net472 reference assemblies come from the Microsoft.NETFramework.ReferenceAssemblies NuGet package.
set -uo pipefail
: "${PKG:?set PKG to the extracted data package root}"
: "${WOTR_GAME_PATH:?set WOTR_GAME_PATH to the installed game root}"
SRC=$(cd "$(dirname "$0")/.." && pwd); OUT=$(mkdir -p "$1" && cd "$1" && pwd); shift
WRATH=$(cd "$WOTR_GAME_PATH" && pwd) || exit 2
MAN=$WRATH/Wrath_Data/Managed
GAME_CS=$MAN/Assembly-CSharp.dll; H236=$MAN/UnityModManager/0Harmony.dll; BPZ=$PKG/GameReferences/blueprints.zip
STAGE=$SRC/artifacts/Release/MediumClass/MediumClass.dll
EXPECTED_HARMONY_SHA=df043f05c6e43602fa8e8f38f52a3a2cf962aab7a65a9d85573ea25f01a63e80
if [[ ! -f "$H236" ]] || [[ $(sha256sum "$H236" | cut -d' ' -f1) != "$EXPECTED_HARMONY_SHA" ]]; then
  echo 'FAIL: require the audited game Managed/UnityModManager/0Harmony.dll 2.3.6.0; no NuGet/replacement-copy fallback.' >&2
  exit 2
fi
COMMON_PROPS=("-p:WrathPath=$WRATH" "-p:HarmonyReferencePath=$H236" "-p:HarmonyReference=$H236")
export FrameworkPathOverride=${FrameworkPathOverride:-$(ls -d "$HOME"/.nuget/packages/microsoft.netframework.referenceassemblies.net472/*/build/.NETFramework/v4.7.2 | head -1)}
cd "$SRC"
declare -A CMD
CMD[ArchmageDamage]="run|--|$GAME_CS|$BPZ"
CMD[CombatLog]="run"
CMD[HardenedSoul]="run"
CMD[HarmonyCompatibility]="run|--|$STAGE|$H236"
CMD[MythicRegistration]="run|--|$SRC/bin/Release/net472/BlueprintCore.dll"
CMD[SpiritActionBar]="run|--|$GAME_CS"
CMD[SpiritActionBar2]="run|-p:TestFramework=net9.0|--|--reflection-smoke|$GAME_CS"
CMD[SpiritBinderArchetype]="run|--|$STAGE|$GAME_CS|$SRC/Strings/Feats.json"
CMD[SpiritChannelLifecycle]="run"
CMD[SpiritInfluenceScenarios]="run|--|$STAGE|$SRC/Strings/Feats.json"
CMD[SpiritSaveRestore]="run|--|$GAME_CS|$STAGE"
CMD[SpiritSpellcasting]="run|--|$GAME_CS|$STAGE"
CMD[SpiritSpellcastingCompatibility]="run|-p:HarmonyReferencePath=$H236"
CMD[SpiritSpellcastingScenarios]="run"
CMD[MediumProgressionLevels]="run|--|$GAME_CS|$BPZ|$STAGE"
CMD[SuddenAttack]="run|--|$GAME_CS|$STAGE|$BPZ"
NET472=(InfluenceScenarios SpiritSurgeScenarios SpiritActionBar SpiritSpellcastingScenarios SpiritSpellcastingCompatibility)
SUITES=("$@"); [ ${#SUITES[@]} -eq 0 ] && SUITES=(ArchmageDamage CombatLog HardenedSoul HarmonyCompatibility MythicRegistration SpiritActionBar SpiritActionBar2 SpiritBinderArchetype SpiritChannelLifecycle SpiritInfluenceScenarios SpiritSaveRestore SpiritSpellcasting SpiritSpellcastingCompatibility SpiritSpellcastingScenarios MediumProgressionLevels SuddenAttack InfluenceScenarios SpiritSurgeScenarios)
status=0
for s in "${SUITES[@]}"; do
  d=${s%2}; proj=$(ls tests/$d/*.csproj | head -1); log=$OUT/$s.log
  if [[ " ${NET472[*]} " == *" $s "* ]]; then
    if dotnet build "$proj" -c Debug "${COMMON_PROPS[@]}" -o "$OUT/bin-$s" > "$log" 2>&1; then
      args=(); [[ "$s" = InfluenceScenarios || "$s" = SpiritActionBar ]] && args=("$GAME_CS")
      mono "$(ls "$OUT/bin-$s"/*.exe | head -1)" "${args[@]}" >> "$log" 2>&1; rc=$?
    else
      rc=$?
    fi
  else
    IFS='|' read -ra parts <<< "${CMD[$s]}"
    dotnet "${parts[0]}" --project "$proj" "${COMMON_PROPS[@]}" "${parts[@]:1}" > "$log" 2>&1; rc=$?
  fi
  [ $rc -ne 0 ] && status=1
  printf '%-34s rc=%s  %s\n' "$s" "$rc" "$(grep -E 'PASS|FAIL|passed|checks' "$log" | tail -1 | cut -c1-200)"
done
exit $status

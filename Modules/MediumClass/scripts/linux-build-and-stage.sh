#!/usr/bin/env bash
# Linux build + StageMod equivalent for MediumClass (0.3.4 maintenance).
#
#   PKG=<extracted data package root, containing GameReferences/ and OfflineNuGet/>  scripts/linux-build-and-stage.sh
#
# Needs: .NET SDK 8+ (`dotnet`) and Mono (`mono`, any 6.x with the 4.7.2 reference assemblies via NuGet).
# The MSBuild StageMod target cannot run ILRepack on Linux (its Core branch execs a Windows path), so this script builds with
# `dotnet build` and then merges BlueprintCore with the SAME pinned ilrepack.exe (ilrepack.msbuild.task 2.0.13) under Mono,
# with the SAME arguments and working directory as that target. Output: artifacts/Release/MediumClass/{MediumClass.dll,Info.json,mediumclass_assets}.
# The result is a Linux-built candidate: repeat it on Windows (msbuild MediumClass.csproj /t:StageMod) before publishing widely.
set -euo pipefail
: "${PKG:?set PKG to the extracted data package root}"
cd "$(dirname "$0")/.."
REF="$PKG/GameReferences"
dotnet restore MediumClass.csproj --source "$PKG/OfflineNuGet" --source https://api.nuget.org/v3/index.json
dotnet build MediumClass.csproj -c Release "-p:WrathPath=$REF"
ILREPACK=$(ls "${NUGET_PACKAGES:-$HOME/.nuget/packages}"/ilrepack.msbuild.task/2.0.13/tools/ilrepack.exe)
rm -rf obj/mod-merge/Release artifacts/Release/MediumClass
mkdir -p obj/mod-merge/Release artifacts/Release/MediumClass
( cd bin/Release/net472 && mono "$ILREPACK" /target:library "/out:$OLDPWD/obj/mod-merge/Release/MediumClass.dll" MediumClass.dll BlueprintCore.dll )
cp obj/mod-merge/Release/MediumClass.dll Info.json mediumclass_assets artifacts/Release/MediumClass/
sha256sum artifacts/Release/MediumClass/MediumClass.dll

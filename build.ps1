#Requires -Version 7.4
param(
    [Parameter(Mandatory = $true)][string]$WrathPath,
    [string]$HarmonyReferencePath,
    [string[]]$AdditionalHarmonyPaths = @(),
    [string]$TestManagedPath,
    [string]$BrawlerReferencePath,
    [string]$ModMenuReferencePath,
    [string]$UnityModManagerReferencePath
)
$ErrorActionPreference = 'Stop'
$projectRoot = $PSScriptRoot
$workspaceRoot = [IO.Path]::GetFullPath((Join-Path $projectRoot '../..'))
$manifest = Get-Content -LiteralPath (Join-Path $projectRoot 'package/OwlcatModificationManifest.json') -Raw | ConvertFrom-Json
$latestGuideText = Get-Content -LiteralPath (Join-Path $projectRoot '0.14.63UrbanBloodrager-test-guide.txt') -Raw
foreach ($incompleteGuideText in @('未实测', '未验收', '不替代游戏验收', '本节在最终构建与安装包实际 DLL 回归完成后写入')) {
    if ($latestGuideText.Contains($incompleteGuideText)) { throw 'The latest test guide contains an unfinished section or disallowed wording.' }
}
$managed = Join-Path $WrathPath 'Wrath_Data/Managed'
if (!(Test-Path -LiteralPath (Join-Path $managed 'Assembly-CSharp.dll'))) {
    throw 'WrathPath must point to the installed Wrath of the Righteous game.'
}
$gameHarmonyPath = [IO.Path]::GetFullPath((Join-Path $managed '0Harmony.dll'))
if (!$HarmonyReferencePath) { $HarmonyReferencePath = $gameHarmonyPath }
$HarmonyReferencePath = [IO.Path]::GetFullPath($HarmonyReferencePath)
if ($HarmonyReferencePath -ine $gameHarmonyPath) { throw 'HarmonyReferencePath must be the actual game Managed/0Harmony.dll; replacement copies are forbidden.' }
$harmonyBaseline = [Reflection.AssemblyName]::GetAssemblyName([IO.Path]::GetFullPath($HarmonyReferencePath))
if ($harmonyBaseline.Name -ne '0Harmony' -or $harmonyBaseline.Version -ne [Version]'2.0.4.0') { throw 'The current game runtime regression baseline is Harmony 2.0.4.0.' }
if ((Get-FileHash -LiteralPath $gameHarmonyPath).Hash -cne 'DE50CD610EA22854C8930EC5433276713AEE1BB0ECD785D6B15A0661C2FA9FD5') { throw 'Game Harmony SHA256 differs from the audited 2.0.4.0 runtime.' }
# Retained only to reject obsolete multi-version invocations before any DLL loads.
foreach ($requestedRuntime in $AdditionalHarmonyPaths) {
    if ([IO.Path]::GetFullPath($requestedRuntime) -ine $gameHarmonyPath) { throw 'AdditionalHarmonyPaths cannot select another runtime. Regression uses only the actual game Harmony 2.0.4.0.' }
}
if ($TestManagedPath) {
    if ((Get-FileHash (Join-Path $TestManagedPath 'Assembly-CSharp.dll')).Hash -ne (Get-FileHash (Join-Path $managed 'Assembly-CSharp.dll')).Hash) { throw 'Test runtime must use identical game assembly bytes.' }
    if ((Get-FileHash (Join-Path $TestManagedPath '0Harmony.dll')).Hash -ne (Get-FileHash $HarmonyReferencePath).Hash) { throw 'Test runtime Harmony differs from selected target.' }
    $managed = $TestManagedPath
}
if (!$ModMenuReferencePath) { $ModMenuReferencePath = Join-Path $workspaceRoot 'work/modmenu-release/ModMenu.dll' }
if (!$UnityModManagerReferencePath) { $UnityModManagerReferencePath = Join-Path $workspaceRoot 'work/repair01440/references/UnityModManager.dll' }
foreach ($reference in @($ModMenuReferencePath, $UnityModManagerReferencePath)) {
    if (!(Test-Path -LiteralPath $reference)) { throw "Missing menu test reference: $reference. Supply -ModMenuReferencePath and -UnityModManagerReferencePath (validation only; not packaged)." }
}
if (!$BrawlerReferencePath) { $BrawlerReferencePath = Join-Path $workspaceRoot 'work/brawler-1.0.9-release/BrawlerClassWrath.dll' }
if (!(Test-Path -LiteralPath $BrawlerReferencePath)) { throw 'Missing Brawler 1.0.9 test reference. Supply -BrawlerReferencePath (not packaged).' }
$mediumRoot = Join-Path $projectRoot 'Modules/MediumClass'
$harmonyPinDirectory = Join-Path $workspaceRoot 'work/prototype-build/harmony-reference-pin'
& (Join-Path $projectRoot 'tests/Test-HarmonyReferencePin.ps1') -WrathPath $WrathPath -OutputDirectory $harmonyPinDirectory
$harmonyPinResults = Get-Content -LiteralPath (Join-Path $harmonyPinDirectory 'results.json') -Raw | ConvertFrom-Json
foreach ($relative in @('artifacts/Release/MediumClass', 'obj/mod-merge/Release')) {
    $stageTarget = [IO.Path]::GetFullPath((Join-Path $mediumRoot $relative))
    if (!$stageTarget.StartsWith([IO.Path]::GetFullPath($mediumRoot) + [IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase)) { throw 'Unsafe Medium staging path.' }
}
& dotnet msbuild (Join-Path $mediumRoot 'MediumClass.csproj') -restore -t:StageMod -p:Configuration=Release "-p:WrathPath=$WrathPath" "-p:HarmonyReferencePath=$HarmonyReferencePath" -nologo
if ($LASTEXITCODE -ne 0) { throw 'Bundled Medium build failed.' }
$mediumStage = Join-Path $mediumRoot 'artifacts/Release/MediumClass'
$wayRoot = Join-Path $projectRoot 'Modules/WayOfTheShield'
& dotnet msbuild (Join-Path $wayRoot 'WayOfTheShield.csproj') -restore -t:StageMod -p:Configuration=Release "-p:WrathPath=$WrathPath" "-p:HarmonyReferencePath=$HarmonyReferencePath" -nologo
if ($LASTEXITCODE -ne 0) { throw 'Bundled Way of the Shield build failed.' }
$wayAssembly = Join-Path $wayRoot 'artifacts/Release/WayOfTheShield/Way of the shield.dll'

$buildArgs = @('-c', 'Release', "-p:WrathPath=$WrathPath", "-p:HarmonyReferencePath=$HarmonyReferencePath", '--nologo')
& dotnet build (Join-Path $projectRoot 'CallOfTheWild.Wrath.Prototype.csproj') @buildArgs
if ($LASTEXITCODE -ne 0) { throw 'Prototype build failed.' }
& dotnet build (Join-Path $projectRoot 'tests/IntegratedStartupTests.csproj') @buildArgs
if ($LASTEXITCODE -ne 0) { throw 'Integrated startup test build failed.' }
& dotnet run --project (Join-Path $projectRoot 'tests/RulesTests.csproj') -c Release "-p:WrathPath=$WrathPath" --nologo
if ($LASTEXITCODE -ne 0) { throw 'Rules verification failed.' }
& dotnet build (Join-Path $projectRoot 'tests/BlueprintTests.csproj') @buildArgs
if ($LASTEXITCODE -ne 0) { throw 'Blueprint tests build failed.' }
$assemblyPath = Join-Path $workspaceRoot 'work/prototype-build/CallOfTheWild.Wrath.Prototype/bin/Release/net472/CallOfTheWild.Wrath.Prototype.dll'
$blueprintResultPath = Join-Path $workspaceRoot 'work/prototype-build/blueprint-results.json'
& (Join-Path $workspaceRoot 'work/prototype-build/BlueprintTests/bin/Release/net472/BlueprintTests.exe') $managed $assemblyPath $blueprintResultPath
if ($LASTEXITCODE -ne 0) { throw 'Managed blueprint tests failed.' }
$blueprintResults = Get-Content -LiteralPath $blueprintResultPath -Raw | ConvertFrom-Json
& dotnet build (Join-Path $projectRoot 'tests/ResourceTests.csproj') @buildArgs
if ($LASTEXITCODE -ne 0) { throw 'Resource tests build failed.' }
$resourceResultPath = Join-Path $workspaceRoot 'work/prototype-build/resource-results.json'
& (Join-Path $workspaceRoot 'work/prototype-build/ResourceTests/bin/Release/net472/ResourceTests.exe') $managed $assemblyPath $resourceResultPath
if ($LASTEXITCODE -ne 0) { throw 'Managed resource component tests failed.' }
$resourceResults = Get-Content -LiteralPath $resourceResultPath -Raw | ConvertFrom-Json
& dotnet build (Join-Path $projectRoot 'tests/HarmonyTests.csproj') @buildArgs
if ($LASTEXITCODE -ne 0) { throw 'Harmony tests build failed.' }
$harmonyAuditProject = Join-Path $mediumRoot 'tests/HarmonyCompatibility/HarmonyCompatCheck.csproj'
foreach ($consumer in @($assemblyPath, (Join-Path $mediumStage 'MediumClass.dll'), $wayAssembly)) {
    & dotnet run --project $harmonyAuditProject -c Release --no-launch-profile "-p:WrathPath=$WrathPath" -- $consumer $gameHarmonyPath
    if ($LASTEXITCODE -ne 0) { throw "Harmony 2.0.4 metadata/injection audit failed: $consumer" }
}
$release01440Directory = Join-Path $workspaceRoot 'work/prototype-build/release01440-static'
& (Join-Path $projectRoot 'tests/Verify-Release01440.ps1') -MainAssembly $assemblyPath -MediumAssembly (Join-Path $mediumStage 'MediumClass.dll') -WrathPath $WrathPath -OutputDirectory $release01440Directory
$release01440Static = Get-Content (Join-Path $release01440Directory 'results.json') -Raw | ConvertFrom-Json
$scene058Directory=Join-Path $workspaceRoot 'work/prototype-build/harmony-scene058'
$scene058Path=Join-Path $scene058Directory 'UnityEngine.CoreModule.dll'
$scene058Audit=Join-Path $scene058Directory 'audit.json'
if (!(Test-Path -LiteralPath $scene058Audit)) {
    & (Join-Path $projectRoot 'tests/Prepare-UnitySceneEndpoints01458.ps1') -NativeAssembly (Join-Path $managed 'UnityEngine.CoreModule.dll') -CecilAssembly (Join-Path $mediumRoot 'tests/HarmonyCompatibility/bin/Release/net9.0/Mono.Cecil.dll') -OutputAssembly $scene058Path -AuditPath $scene058Audit
}
$scene058=Get-Content -LiteralPath $scene058Audit -Raw | ConvertFrom-Json
if (!$scene058.passed -or $scene058.changedMethodCount -ne 1 -or !$scene058.otherMethodBodiesAndFieldsExact -or $scene058.sourceSHA256 -cne (Get-FileHash (Join-Path $managed 'UnityEngine.CoreModule.dll')).Hash -or $scene058.fixtureSHA256 -cne (Get-FileHash $scene058Path).Hash) { throw '058 scene dependency exact audit failed.' }
$harmonyResults = @()
foreach ($runtimePath in @($gameHarmonyPath)) {
    & (Join-Path $workspaceRoot 'work/prototype-build/HarmonyTests/bin/Release/net472/HarmonyTests.exe') $managed $assemblyPath $runtimePath $scene058Path $scene058.fixtureSHA256
    if ($LASTEXITCODE -ne 0) { throw "Harmony runtime test failed: $runtimePath" }
    $harmonyResults += [ordered]@{
        version = ([Reflection.AssemblyName]::GetAssemblyName([IO.Path]::GetFullPath($runtimePath))).Version.ToString()
        sha256 = (Get-FileHash -LiteralPath $runtimePath -Algorithm SHA256).Hash
        result = 'PASS: production PatchAll, isolated postfix execution, owner-scoped unpatch'
    }
}

$startupRegressionDirectory = Join-Path $workspaceRoot 'work/prototype-build/startup-regressions'
& (Join-Path $projectRoot 'tests/Test-StartupRegressions.ps1') -WrathPath $WrathPath -MainAssemblyPath $assemblyPath -MediumAssemblyPath (Join-Path $mediumStage 'MediumClass.dll') -OutputDirectory $startupRegressionDirectory
$startupRegressionResults = Get-Content -LiteralPath (Join-Path $startupRegressionDirectory 'results.json') -Raw | ConvertFrom-Json
if ($startupRegressionResults.passedCases -ne 16 -or $startupRegressionResults.blueprintCorePatchesInstalled -ne 5) { throw 'All startup/runtime rejection checks and five actual BlueprintCore hooks are required.' }

$mediumRegressionDirectory = Join-Path $workspaceRoot 'work/prototype-build/medium-regressions'
& (Join-Path $projectRoot 'tests/Run-MediumRegressions.ps1') -WrathPath $WrathPath -MediumAssemblyPath (Join-Path $mediumStage 'MediumClass.dll') -OutputDirectory $mediumRegressionDirectory
$mediumRegressionResults = Get-Content -LiteralPath (Join-Path $mediumRegressionDirectory 'results.json') -Raw | ConvertFrom-Json
if ($mediumRegressionResults.passedSuites -ne 16) { throw 'All 16 mandatory Medium regression suites are required.' }

& dotnet build (Join-Path $projectRoot 'tests/ModMenuTests.csproj') @buildArgs
if ($LASTEXITCODE -ne 0) { throw 'ModMenu adapter tests build failed.' }
$menuResultPath = Join-Path $workspaceRoot 'work/prototype-build/modmenu-results.json'
& (Join-Path $workspaceRoot 'work/prototype-build/ModMenuTests/bin/Release/net472/ModMenuTests.exe') $managed $assemblyPath $ModMenuReferencePath $UnityModManagerReferencePath $menuResultPath
if ($LASTEXITCODE -ne 0) { throw 'ModMenu adapter tests failed.' }
$menuResults = Get-Content -LiteralPath $menuResultPath -Raw | ConvertFrom-Json

& dotnet build (Join-Path $projectRoot 'tests/ExemplarTests.csproj') @buildArgs
if ($LASTEXITCODE -ne 0) { throw 'Exemplar tests build failed.' }
$exemplarResultPath = Join-Path $workspaceRoot 'work/prototype-build/exemplar-results.json'
& (Join-Path $workspaceRoot 'work/prototype-build/ExemplarTests/bin/Release/net472/ExemplarTests.exe') $managed $assemblyPath $exemplarResultPath $scene058Path $scene058.fixtureSHA256
if ($LASTEXITCODE -ne 0) { throw 'Exemplar scaling tests failed.' }
$exemplarResults = Get-Content -LiteralPath $exemplarResultPath -Raw | ConvertFrom-Json

& dotnet build (Join-Path $projectRoot 'tests/ShieldTests.csproj') @buildArgs
if ($LASTEXITCODE -ne 0) { throw 'Shielded Mage tests build failed.' }
$shieldResultPath = Join-Path $workspaceRoot 'work/prototype-build/shield-results.json'
& (Join-Path $workspaceRoot 'work/prototype-build/ShieldTests/bin/Release/net472/ShieldTests.exe') $managed $assemblyPath $shieldResultPath
if ($LASTEXITCODE -ne 0) { throw 'Shielded Mage and identity tests failed.' }
$shieldResults = Get-Content -LiteralPath $shieldResultPath -Raw | ConvertFrom-Json

& dotnet build (Join-Path $projectRoot 'tests/PerformanceTests.csproj') @buildArgs
if ($LASTEXITCODE -ne 0) { throw 'Performance resource tests build failed.' }
$performanceResultPath = Join-Path $workspaceRoot 'work/prototype-build/performance-results.json'
& (Join-Path $workspaceRoot 'work/prototype-build/PerformanceTests/bin/Release/net472/PerformanceTests.exe') $managed $assemblyPath $BrawlerReferencePath $performanceResultPath
if ($LASTEXITCODE -ne 0) { throw 'Performance resource tests failed.' }
$performanceResults = Get-Content -LiteralPath $performanceResultPath -Raw | ConvertFrom-Json

& dotnet build (Join-Path $projectRoot 'tests/RageTests.csproj') @buildArgs
if ($LASTEXITCODE -ne 0) { throw 'Rage tests build failed.' }
$rageNativePath = Join-Path $workspaceRoot 'work/prototype-build/rage-native-input.json'
& python (Join-Path $projectRoot 'tests/export_rage_native.py') (Join-Path $WrathPath 'blueprints.zip') $rageNativePath
if ($LASTEXITCODE -ne 0) { throw 'Native rage test evidence extraction failed.' }
$rageResultPath = Join-Path $workspaceRoot 'work/prototype-build/rage-results.json'
& (Join-Path $workspaceRoot 'work/prototype-build/RageTests/bin/Release/net472/RageTests.exe') $managed $assemblyPath $rageResultPath $rageNativePath
if ($LASTEXITCODE -ne 0) { throw 'Rage power tests failed.' }
$rageResults = Get-Content -LiteralPath $rageResultPath -Raw | ConvertFrom-Json

& dotnet build (Join-Path $projectRoot 'tests/PsychicTests.csproj') @buildArgs
if ($LASTEXITCODE -ne 0) { throw 'Psychic tests build failed.' }
$psychicNativePath = Join-Path $workspaceRoot 'work/psychic060/native-test.json'
$psychicExpansionNativePath = Join-Path $workspaceRoot 'work/prototype-build/psychic-expansion-native.json'
& python (Join-Path $projectRoot 'tests/Extract-PsychicNative.py') $WrathPath $psychicExpansionNativePath
if ($LASTEXITCODE -ne 0) { throw 'Psychic extension native evidence extraction failed.' }
& python (Join-Path $projectRoot 'tests/Extract-PsychicCompletion01458.py') $WrathPath (Join-Path $workspaceRoot 'work/prototype-build/psychic-completion01458-native.json')
if ($LASTEXITCODE -ne 0) { throw '058 isolated native input extraction failed.' }
& python (Join-Path $projectRoot 'tests/Pin-LegacyNativeInput01458.py') $psychicExpansionNativePath
if ($LASTEXITCODE -ne 0) { throw '058 fresh old-domain native payload equality/order pin failed.' }
# Actual official ToTF claw paths require additional native dependencies and a
# test-only CLR access adapter. Never package or install the adapter DLL.
$bloodlineNativePath = Join-Path $workspaceRoot 'work/prototype-build/01449-native.json'
& python (Join-Path $projectRoot 'tests/Extract-BloodlineCompatibility01449.py') $WrathPath $bloodlineNativePath
if ($LASTEXITCODE -ne 0) { throw '01449 actual ToTF native input extraction failed.' }
$tomeOriginal = Join-Path $workspaceRoot 'work/repair01449/dependencies/TomeOfTheFirebird/TomeOfTheFirebird.dll'
$bloodlineHost = Join-Path $workspaceRoot 'work/prototype-build/PsychicTests/bin/Release/net472/PsychicTests.exe'
$tomeAdapter = Join-Path $workspaceRoot 'work/psychic01458/runtime-adapter/TomeOfTheFirebird.dll'
& dotnet run --project (Join-Path $projectRoot 'tests/BloodlineAccessAdapter01449/BloodlineAccessAdapter01449.csproj') -c Release "-p:WrathPath=$WrathPath" -- $tomeOriginal $bloodlineHost $tomeAdapter (Join-Path $WrathPath 'Wrath_Data/Managed')
if ($LASTEXITCODE -ne 0) { throw '01449 precise ToTF CLR access adapter failed.' }
# Current feedback: actual provider builders/PP preview use test-only, input-bound CLR access copies.
& python (Join-Path $projectRoot 'tests/Extract-BloodlineCompatibility01450.py') $WrathPath (Join-Path $workspaceRoot 'work/prototype-build/01450-native.json')
if ($LASTEXITCODE -ne 0) { throw '01450 actual external native input extraction failed.' }
foreach ($pair in @(@('work/repair01440/dependencies/TabletopTweaks-Core/TabletopTweaks-Core/TabletopTweaks-Core.dll','TabletopTweaks-Core.dll'),@('work/batch01447/dependencies/ECM/EbonsContentMod.dll','EbonsContentMod.dll'),@('work/repair01440/dependencies/TabletopTweaks-Base/TabletopTweaks-Base/TabletopTweaks-Base.dll','TabletopTweaks-Base.dll'),@('work/repair01450/dependencies/PrestigePlus/PrestigePlus.dll','PrestigePlus.dll'))) {
    & dotnet run --project (Join-Path $projectRoot 'tests/BloodlineAccessAdapter01449/BloodlineAccessAdapter01449.csproj') -c Release "-p:WrathPath=$WrathPath" -- (Join-Path $workspaceRoot $pair[0]) $bloodlineHost (Join-Path $workspaceRoot ('work/psychic01458/runtime-adapter/'+$pair[1])) (Join-Path $WrathPath 'Wrath_Data/Managed')
    if ($LASTEXITCODE -ne 0) { throw "01450 exact CLR access copy failed: $($pair[1])" }
}
& python (Join-Path $workspaceRoot 'work/psychic01458/prepare_native_inputs.py')
if ($LASTEXITCODE -ne 0) { throw '01451 exact native power/matrix extraction failed.' }
& python (Join-Path $workspaceRoot 'work/psychic01458/prepare_external_inputs.py')
if ($LASTEXITCODE -ne 0) { throw '01451 exact external matrix extraction failed.' }
$selectionContractPath = Join-Path $workspaceRoot 'work/prototype-build/selection-contracts.json'
& python (Join-Path $projectRoot 'tests/Check-SelectionContracts.py') $psychicExpansionNativePath $projectRoot $selectionContractPath
if ($LASTEXITCODE -ne 0) { throw 'Offline release selection/module contracts failed.' }

$psychicResultPath = Join-Path $workspaceRoot 'work/prototype-build/psychic-results.json'
if (!(Test-Path -LiteralPath $psychicNativePath)) { throw 'Missing audited Psychic native input: work/psychic060/native-test.json' }
& (Join-Path $workspaceRoot 'work/prototype-build/PsychicTests/bin/Release/net472/PsychicTests.exe') $managed $assemblyPath $psychicResultPath $psychicNativePath $psychicExpansionNativePath
if ($LASTEXITCODE -ne 0) { throw 'Psychic casting tests failed.' }
$psychicResults = Get-Content -LiteralPath $psychicResultPath -Raw | ConvertFrom-Json
if (!$psychicResults.enhancementLifecycle01430Contracts.passed -or !$psychicResults.flameJetLifecycle01430Contracts.passed) { throw '01430 Enhancement/Flame Jet lifecycle contracts failed.' }
if (!$psychicResults.enhancementLifecycle01430Contracts.nativeManagerLifecycle -or !$psychicResults.enhancementLifecycle01430Contracts.nativeEventBus -or !$psychicResults.enhancementLifecycle01430Contracts.noLifecycleMethodReplacement -or !$psychicResults.enhancementLifecycle01430Contracts.positiveAndNegativeCasesCompleted -or !$psychicResults.enhancementLifecycle01430Contracts.registrationAppendRollback) { throw '01430 Enhancement native lifecycle/selection coverage is incomplete.' }
if ($psychicResults.flameJetLifecycle01430Contracts.isPartial -ne $false -or !$psychicResults.flameJetLifecycle01430Contracts.nativeManagerRestart -or !$psychicResults.flameJetLifecycle01430Contracts.nativeDeathTickUnmodified -or !$psychicResults.flameJetLifecycle01430Contracts.nativeAddFactsOwnership -or $psychicResults.flameJetLifecycle01430Contracts.nativeResurrectedEvents -ne 2 -or $psychicResults.flameJetLifecycle01430Contracts.nativeResurrectEntryUnmodified -ne $false -or $psychicResults.flameJetLifecycle01430Contracts.resurrectBridge.nativeMethodIlSha256 -cne 'FDAB8FBC493660EBF48DD9AAE95C13D0FD693321A511319427D9361581CCD4AB') { throw '01430 Flame Jet full lifecycle or bounded native resurrection evidence is incomplete.' }
if (!$psychicResults.howl01429CleanupContracts.passed -or $psychicResults.howl01429CleanupContracts.diagnosticComponentAttached -or $psychicResults.howl01429CleanupContracts.extraCombatMessages -ne 0 -or $psychicResults.howl01429CleanupContracts.extraModDiagnosticMessages -ne 0) { throw '01429 Howl diagnostic cleanup failed.' }
if (!$psychicResults.migration01429Contracts.passed -or $psychicResults.migration01429Contracts.additionalItemCount -ne 2 -or !$psychicResults.overlap01429Contracts.passed -or !$psychicResults.hoverOwnership01429Contracts.passed) { throw '01429 migration/overlap/ownership contracts failed.' }
if ($psychicResults.migration01429Contracts.rootFeatureCount -ne 3 -or $psychicResults.migration01429Contracts.newItemsThisRelease -ne 0 -or !$psychicResults.overlap01429Contracts.strongestNaturalArmorRestores -or !$psychicResults.overlap01429Contracts.privateFlightOwnership -or !$psychicResults.overlap01429Contracts.flightSpeedNotDoubled) { throw 'Retained Enhancement/Flame Jet029 contracts must survive Atavism removal.' }
# 01436 repairs five existing035 options. Preserve their rule contracts and all
# old identities; the retired035 caller asserted the now-rejected feat entries.
if ($psychicResults.PSObject.Properties.Name -contains 'migration01435CallerContracts') { throw 'Retired035 entry assumptions must be replaced by the actual036 registration caller.' }
foreach ($entry in @(@('wizardDiscoveries01435Contracts',2,2),@('bardMasterpieces01435Contracts',3,23))) {
    $key = $entry[0]
    if ($psychicResults.$key.passed -ne $true -or $psychicResults.$key.assertions -le 0 -or $psychicResults.$key.additionalItemCount -ne $entry[1] -or $psychicResults.$key.newBlueprints.Count -ne $entry[2]) { throw "Retained035 native rule contract failed: $key" }
}
$expected01436Ids=@('COTW-0364','COTW-0369','COTW-0378','COTW-0379','COTW-0380')
$old01435Ids=@($psychicResults.wizardDiscoveries01435Contracts.itemIds)+@($psychicResults.bardMasterpieces01435Contracts.itemIds)
if ($old01435Ids.Count -ne 5 -or @($old01435Ids | Sort-Object -Unique).Count -ne 5 -or @(Compare-Object $expected01436Ids $old01435Ids).Count -ne 0) { throw '036 repair must retain the same five original035 rows, not count them as new migrations.' }
$old01435Assets=@($psychicResults.wizardDiscoveries01435Contracts.newBlueprints)+@($psychicResults.bardMasterpieces01435Contracts.newBlueprints)
if ($old01435Assets.Count -ne 25 -or @($old01435Assets.id | Sort-Object -Unique).Count -ne 25) { throw '035 private graph must retain all25 distinct original identities.' }
if ($psychicResults.wizardDiscoveries01435Contracts.ordinaryFeatCount -ne 0 -or $psychicResults.wizardDiscoveries01435Contracts.arcaneDiscoveryCount -ne 2 -or $psychicResults.wizardDiscoveries01435Contracts.newSpellRootCount -ne 0) { throw 'Repaired discoveries must use the class submenu, retaining two original discoveries and zero combat-spell roots.' }
if ($psychicResults.bardMasterpieces01435Contracts.ordinaryFeatCount -ne 0 -or $psychicResults.bardMasterpieces01435Contracts.masterpieceCount -ne 3 -or $psychicResults.bardMasterpieces01435Contracts.newSpellRootCount -ne 0) { throw 'Retained three masterpieces must not become direct ordinary feats or new combat-spell roots.' }
foreach ($field in @('nativeCorePoolPreservationAndClassLevelSelection','nativeIndependentPublicationAndRetry','optionalTttAbsentDisabledMalformedAndLate','nativeDualListParametersAndOtherSpellbook','realVariantAndUnrelatedParentBoundaries','nativeFactLifecycleAndOtherCasterBoundaries','originalFourPhantasmalWillAndFortitudeBoundaries','nativeShadowDisbeliefAndDamageFactorPath','ordinaryShadowWillNotDisbelief','nativeNaturalOneTwentyAndDCCannotDecrease','nativePerTargetShadowCacheAndExceptionScopeCleanup','nativeConvertedShadowEvocationRetainsEmulatedSchool','multipleOwnedSourcesApplyOnlyOncePerRule','nativePhantasmalAbilityApplyPath','outerFixtureDiceAndPatchMetadataRestored','nativeExternalPoolMembershipAndDiscoveryClassSelection','nativeDiscoveryOwnedRankBoundaries','nativeExternalPoolNativeFlagsAndArraysPreserved')) {
    if ($psychicResults.wizardDiscoveries01435Contracts.$field -ne $true) { throw "036 native coverage incomplete: wizardDiscoveries01435Contracts.$field" }
}
foreach ($field in @('nativeSelectionAndPrerequisites','nativePerformanceResourceAndControllerLifecycle','nativeDanceScalingAndDifficultCasting','nativeFreedomAreaOverlapOwnership','nativeClamorAlignmentSaveAndConditionPaths','nativeOriginalGuidSpecificImmunityHandlers','nativeLowestDifficultyRepeatedDescriptorImmunity','outerFixtureInputsAndPatchesRestored')) {
    if ($psychicResults.bardMasterpieces01435Contracts.$field -ne $true) { throw "036 native coverage incomplete: bardMasterpieces01435Contracts.$field" }
}
foreach ($key in @('wizardDiscoveryAccess01436Contracts','bardMasterpieceAccess01436Contracts','repair01436CallerContracts')) {
    if ($psychicResults.$key.passed -ne $true -or $psychicResults.$key.assertions -le 0 -or $psychicResults.$key.additionalItemCount -ne 0) { throw "036 zero-new-migration repair contract failed: $key" }
}
$actual01436Ids=@($psychicResults.wizardDiscoveryAccess01436Contracts.repairItemIds)+@($psychicResults.bardMasterpieceAccess01436Contracts.repairItemIds)
$caller01436Ids=@($psychicResults.repair01436CallerContracts.repairItemIds)
if ($actual01436Ids.Count -ne 5 -or @($actual01436Ids | Sort-Object -Unique).Count -ne 5 -or @(Compare-Object $expected01436Ids $actual01436Ids).Count -ne 0 -or $caller01436Ids.Count -ne 5 -or @(Compare-Object $expected01436Ids $caller01436Ids).Count -ne 0) { throw '036 actual module repairs and real caller must cover exactly the same five existing035 IDs.' }
foreach ($field in @('nativeExternalPoolMembershipExcluded','nativeWizardOrDoubleArcanistThresholds','nativeMixedClassLevelsNotSummed','nativeParentAndLeafUnrelatedPrerequisitesRetained','nativeOriginalWizardFakeClassAndSelectionScopePreserved','nativeArcanistEntryCannotUnlockOtherDiscoveries','nativeRankAndOwnedEligibilityBoundaries','optionalProviderAbsentMalformedGroupOnlyLateRetry','nativePostAppendRestoresComponentsAndAllPoolArrays','same035ResourceIdentitiesAndEffectsRetained','nativeExactRootIdentityAndUnrelatedPoolBoundaries','nativeConcurrentCacheReplacementAndSnapshotFailureIsolation','optionalProviderRefreshAndRollbackRefreshFailureIsolated','nativeExistingCombatPrefixCooperation')) {
    if ($psychicResults.wizardDiscoveryAccess01436Contracts.$field -ne $true) { throw "036 native coverage incomplete: wizardDiscoveryAccess01436Contracts.$field" }
}
foreach ($field in @('prestigePoolAndKnownCircles','knownSourceLifecycleAndForeignOwnership','nativeSelectionPrerequisiteFiltering','optionalFaultRollbackAndRetry','legacyContainerRetired','nativeSelectedReceiptSurvivesListRebuild','nativeDuplicateAndCancelledSlotBoundaries','nativeOwnedReceiptJsonAndPostLoad','outerFixtureInputsAndPatchesRestored','nativePrewarmedSpellRowCachesAndRollback')) {
    if ($psychicResults.bardMasterpieceAccess01436Contracts.$field -ne $true) { throw "036 native coverage incomplete: bardMasterpieceAccess01436Contracts.$field" }
}
foreach ($field in @('actualRegisterAllAndOptionalDispatchers','fiveIndependentFailuresRollBackAndLaterActionsContinue','realPostAppendProviderFailuresIsolatedAndRetried','realModuleCatalogueAndHonestStates','legacyDirectChoicesRemovedOnlyByOwnedIdentity','onlyProviderSubmenuChoices','legacyPrivateContainerHiddenAndNotSelectable','foreignSameIdentityContainerRejectedAndPreserved','sourceFaithfulAbsentMalformedAndLateProviders','allOwnedIdentitiesRetainedAcrossRetry','allUnrelatedPoolReferencesAndOrderPreserved','parentCacheComponentsPoolsLazyPointersAndStringsRestored','parentSpellListRowsReferencesAndProxiesRestored','nonTargetActionsScopedOutWithoutSuccessReports')) {
    if ($psychicResults.repair01436CallerContracts.$field -ne $true) { throw "036 native coverage incomplete: repair01436CallerContracts.$field" }
}
# The old104 ordinary/Wizard/Mythic/Secret positive matrix is intentionally
# replaced by26 actual class-submenu access rows and external-pool exclusions.
if ($psychicResults.wizardDiscoveryAccess01436Contracts.actualSelectionMatrix.Count -ne 26 -or $psychicResults.wizardDiscoveries01435Contracts.actualFourPoolChoiceMatrix.Count -ne 26 -or $psychicResults.wizardDiscoveryAccess01436Contracts.nativeCanSelectCalls -lt 200 -or $psychicResults.wizardDiscoveryAccess01436Contracts.nativeCacheSnapshotPasses -lt 8) { throw '036 discovery eligibility/native concurrent-cache coverage is incomplete.' }
if ($psychicResults.wizardDiscoveryAccess01436Contracts.ordinaryFeatCount -ne 0 -or $psychicResults.wizardDiscoveryAccess01436Contracts.newBlueprints.Count -ne 0 -or $psychicResults.wizardDiscoveryAccess01436Contracts.newBlueprintCount -ne 0 -or $psychicResults.wizardDiscoveryAccess01436Contracts.newProductionTypeIdCount -ne 1 -or $psychicResults.wizardDiscoveryAccess01436Contracts.newCanSelectPrefixCount -ne 1 -or $psychicResults.wizardDiscoveryAccess01436Contracts.newUniqueHarmonyTargetCount -ne 0) { throw '036 discovery repair must add only its reviewed prerequisite adapter and existing-target prefix.' }
if ($psychicResults.bardMasterpieces01435Contracts.historicalRegression -ne $true -or $psychicResults.bardMasterpieces01435Contracts.currentRepairAdditionalItemCount -ne 0) { throw '035 masterpiece rule counts describe retained history, not new036 content.' }
if ($psychicResults.bardMasterpieceAccess01436Contracts.nativeFilteredChoiceVmDisposals -lt 1 -or $psychicResults.bardMasterpieceAccess01436Contracts.nativeChoiceDisplayIlReplacements.rawName -ne 1 -or $psychicResults.bardMasterpieceAccess01436Contracts.nativeChoiceDisplayIlReplacements.nullSprite -ne 1 -or $psychicResults.bardMasterpieceAccess01436Contracts.nativeChoiceDisplayIlReplacements.schoolText -ne 1 -or @($psychicResults.bardMasterpieceAccess01436Contracts.nativeChoiceDisplayIlReplacements.PSObject.Properties).Count -ne 3) { throw '036 actual native choice VM disposal or exact three display-only fixture endpoints are incomplete.' }
$added01436Assets=@($psychicResults.bardMasterpieceAccess01436Contracts.addedBlueprints)
if ($psychicResults.bardMasterpieceAccess01436Contracts.newCombatSpellRoots -ne 0 -or $added01436Assets.Count -ne 4 -or @($added01436Assets.id | Sort-Object -Unique).Count -ne 4 -or @($added01436Assets | Where-Object { $_.type -eq 'Kingmaker.UnitLogic.Abilities.Blueprints.BlueprintAbility' }).Count -ne 3 -or @($added01436Assets | Where-Object { $_.type -eq 'Kingmaker.Blueprints.Classes.BlueprintFeature' }).Count -ne 1 -or @($added01436Assets | Where-Object { $_.id -in $old01435Assets.id }).Count -ne 0) { throw '036 permits only three stable known-spell markers and one ownership bridge; no old identity reuse or new combat-spell root.' }
$expected01436AssetRows=@(@($old01435Assets)+@($added01436Assets) | ForEach-Object { "$($_.id)|$($_.name)|$($_.type)" } | Sort-Object)
$caller01436AssetRows=@($psychicResults.repair01436CallerContracts.observedOwnedBlueprints | ForEach-Object { "$($_.id)|$($_.name)|$($_.type)" } | Sort-Object)
if ($caller01436AssetRows.Count -ne 29 -or @($caller01436AssetRows | Sort-Object -Unique).Count -ne 29 -or @(Compare-Object $expected01436AssetRows $caller01436AssetRows).Count -ne 0 -or $psychicResults.repair01436CallerContracts.newBlueprints.Count -ne 0 -or $psychicResults.repair01436CallerContracts.selectionPoolAssertions -lt 1) { throw '036 real registration caller must observe the exact original25 plus four necessary repair resources.' }
if ($psychicResults.bardMasterpieceAccess01436Contracts.nativePrewarmedSpellRows -lt 3 -or $psychicResults.bardMasterpieceAccess01436Contracts.nativePostWriteCacheFaults -lt 1) { throw '036 must exercise native prewarmed spell rows and exact post-write proxy/filter cache rollback.' }
foreach ($entry in @(@('wizardDiscoveries01435Contracts','nativeParameterRules'),@('wizardDiscoveries01435Contracts','nativeSavingThrows'),@('wizardDiscoveries01435Contracts','fixedRawD20Rolls'),@('bardMasterpieces01435Contracts','nativeSavingThrows'),@('bardMasterpieces01435Contracts','nativeBuffApplications'),@('bardMasterpieces01435Contracts','nativeResourceUnitsSpent'),@('bardMasterpieces01435Contracts','nativeControllerTicks'),@('bardMasterpieces01435Contracts','nativeConcentrations'),@('bardMasterpieces01435Contracts','nativeMeleeRules'),@('bardMasterpieces01435Contracts','nativeRangedRules'),@('bardMasterpieceAccess01436Contracts','nativeLearnCalls'),@('bardMasterpieceAccess01436Contracts','nativeRemoveCalls'),@('bardMasterpieceAccess01436Contracts','nativeSelectSpellApplies'),@('bardMasterpieceAccess01436Contracts','nativeCharGenChoiceBuilds'),@('bardMasterpieceAccess01436Contracts','nativeSpellbookPostLoads'),@('bardMasterpieceAccess01436Contracts','nativeCopySpellbookCalls'))) {
    if ($psychicResults.($entry[0]).($entry[1]) -lt 1) { throw "036 native execution count missing: $($entry[0]).$($entry[1])" }
}
# The038 contracts add actual membership, local timing, real provider entry,
# independent spell acquisition and caller evidence; retained037 gates below
# remain limited historical regressions, not proof of the missing paths.
foreach ($key in @('discoveryAccess01438Contracts','timeStopLifecycle01438Contracts','timeStopTimers01438Contracts','timeStopSpell01438Contracts','timeStopSpellbook01438Contracts','repair01438CallerContracts')) {
    if ($null -eq $psychicResults.PSObject.Properties[$key] -or $psychicResults.$key.passed -ne $true -or $psychicResults.$key.assertions -le 0) { throw "038 actual-path contract failed: $key" }
}
foreach ($key in @('discoveryAccess01438Contracts','repair01438CallerContracts')) {
    if ($null -eq $psychicResults.$key.PSObject.Properties['additionalItemCount'] -or $psychicResults.$key.additionalItemCount -ne 0 -or $psychicResults.$key.newBlueprints -isnot [array] -or $psychicResults.$key.newBlueprints.Count -ne 0) { throw "038 existing discovery/caller repairs cannot count new items or resources: $key" }
}
$timeStop01438 = $psychicResults.timeStopSpell01438Contracts
$timeStop01438Assets = @($timeStop01438.newBlueprints)
if ($timeStop01438.additionalItemCount -ne 1 -or $timeStop01438.ordinaryFeatCount -ne 0 -or $timeStop01438.newSpellRootCount -ne 1 -or @($timeStop01438.itemIds).Count -ne 1 -or $timeStop01438.itemIds[0] -cne 'COTW-0551' -or $timeStop01438Assets.Count -ne 4 -or @($timeStop01438Assets.id | Sort-Object -Unique).Count -ne 4) { throw '038 permits exactly the separately authorized Time Stop spell item and four unique private spell resources.' }
if (@($timeStop01438Assets | Where-Object type -CEQ 'Kingmaker.UnitLogic.Abilities.Blueprints.BlueprintAbility').Count -ne 1 -or @($timeStop01438Assets | Where-Object type -CEQ 'Kingmaker.UnitLogic.Abilities.Blueprints.BlueprintAbilityAreaEffect').Count -ne 1 -or @($timeStop01438Assets | Where-Object type -CEQ 'Kingmaker.UnitLogic.Buffs.Blueprints.BlueprintBuff').Count -ne 2) { throw '038 Time Stop resources must be exactly one spell, one area and two buffs.' }
foreach ($key in @('timeStopLifecycle01438Contracts','timeStopTimers01438Contracts','timeStopSpellbook01438Contracts')) {
    if ($null -ne $psychicResults.$key.PSObject.Properties['additionalItemCount'] -and $psychicResults.$key.additionalItemCount -ne 0) { throw "038 lifecycle/timer/spellbook coverage must not count the spell again: $key" }
}
$repair01440 = $psychicResults.repair01440Contracts
if ($repair01440.passed -ne $true -or $repair01440.additionalItemCount -ne 0 -or $repair01440.activation -cne 'native-always-active' -or $repair01440.mixedNativeEnergyReduction -ne 2) { throw '040 repair contract failed.' }
# BEGIN 01443 ADDITIVE CONTRACT GATES
$reg01443 = $psychicResults.registrationWarmup01443Contracts
if ($null -eq $reg01443 -or !$reg01443.passed -or $reg01443.assertions -lt 229 -or $reg01443.nativeJsonRoundTrips -ne 9 -or $reg01443.nativeCacheScans -ne 1 -or $reg01443.nativeOnEnableCalls -ne 128 -or $reg01443.actualOptionalDispatches -ne 6 -or $reg01443.authoredAndIdentityRejections -ne 28 -or $reg01443.realTTTMethodsExecutedViaIdentityBodies -ne 7) { throw '01443 registrationWarmup01443Contracts exact native-path contract failed.' }
$war01443 = $psychicResults.archetypeWarmup01443Contracts
if ($null -eq $war01443 -or !$war01443.passed -or $war01443.assertions -lt 4 -or $war01443.nativeGetEntryCalls -ne 40 -or !$war01443.authorLevelMutationRejected -or !$war01443.authorReferenceMutationRejected -or !$war01443.privateIdentityPreserved) { throw '01443 archetypeWarmup01443Contracts exact native-path contract failed.' }
$sun01443 = $psychicResults.sunsinger01443Contracts
if ($null -eq $sun01443 -or !$sun01443.passed -or $sun01443.assertions -lt 442 -or $sun01443.additionalItemCount -ne 1 -or $sun01443.archetypeCount -ne 1 -or $sun01443.privateResourceCount -ne 13 -or $sun01443.newSpellRootCount -ne 0 -or $sun01443.newHarmonyPatchCount -ne 0 -or $sun01443.newSerializedComponentTypeCount -ne 0 -or $sun01443.resourceSpends -ne 12 -or $sun01443.castPrepares -ne 52 -or $sun01443.outlinePaths -ne 20 -or $sun01443.nativeAreaSaveEntries -ne 3 -or $sun01443.nativeChannelHealingRuleCalls -ne 6 -or $sun01443.nativeChannelDamageDispatches -ne 6 -or $sun01443.nativeRepeatableRankChanges -ne 12 -or $sun01443.nativeOriginalGuidImmunityBranches -ne 4 -or $sun01443.warmNativeDonorsPreserved -ne 9 -or $sun01443.nativeJsonRoundTrips -ne 3 -or $sun01443.nativeResourceLinkLoads -ne 1 -or $sun01443.sarenraeCatalogueEndpoints -ne 2 -or $sun01443.postCacheWriteFaults -ne 1 -or $sun01443.postAppendFaults -ne 1 -or !$sun01443.baseProgressionPreserved) { throw '01443 sunsinger01443Contracts exact native-path contract failed.' }
$urban01443 = $psychicResults.urbanSkald01443Contracts
if ($null -eq $urban01443 -or !$urban01443.passed -or $urban01443.assertions -lt 614 -or $urban01443.additionalItemCount -ne 1 -or $urban01443.archetypeCount -ne 1 -or $urban01443.privateResourceCount -ne 52 -or $urban01443.newSpellRootCount -ne 0 -or $urban01443.newHarmonyPatchCount -ne 0 -or $urban01443.newSerializedComponentTypeCount -ne 3 -or $urban01443.nativeControlledSongApplications -ne 38 -or $urban01443.nativeAreaCallbacks -ne 95 -or $urban01443.nativeCrowdRules -ne 7035 -or $urban01443.cacheWriteFailures -ne 1 -or $urban01443.appendFailures -ne 2 -or $urban01443.foreignOwnerCases -ne 6 -or $urban01443.nativeTypeIdAttributeReads -ne 2 -or $urban01443.nativeSerializedComponentRoundTrips -ne 1 -or $urban01443.nativeTypeIdsScanned -ne 2924 -or $urban01443.productionTypeIdsScanned -ne 301 -or !$urban01443.fixtureSchemaRegistration -or !$urban01443.authoredGraphMutationRejected -or !$urban01443.warmedDonorElementOwnershipPreserved -or !$urban01443.exactOwnerReferenceGuards -or !$urban01443.publishedAppendRollback -or !$urban01443.nativeFactionGroups -or !$urban01443.nativePerformanceResources -or !$urban01443.nativeRagePowerSharing -or !$urban01443.nativeAcceptRefuseMasterMythic -or !$urban01443.nativeMockerySaveImmunityReplace -or !$urban01443.nativeCrowdEnumerationAndEventBus -or !$urban01443.noMovementOrTickSubscription) { throw '01443 urbanSkald01443Contracts exact native-path contract failed.' }
$rogue01443 = $psychicResults.rogueSwashbuckler01443Contracts
if ($null -eq $rogue01443 -or !$rogue01443.passed -or $rogue01443.assertions -lt 230 -or $rogue01443.additionalItemCount -ne 1 -or $rogue01443.privateResourceCount -ne 4 -or $rogue01443.newSpellRootCount -ne 0 -or $rogue01443.newHarmonyPatchCount -ne 0 -or $rogue01443.newSerializedComponentTypeCount -ne 0 -or $rogue01443.nativeSavingThrowHandlers -ne 54 -or $rogue01443.nativeSelectFeatureApplyCalls -ne 12 -or $rogue01443.postCacheWriteFaults -ne 1 -or $rogue01443.postAppendFaults -ne 1 -or $rogue01443.postConnectFaults -ne 2 -or !$rogue01443.baseProgressionPreserved -or !$rogue01443.nativeCombatTrickRetained -or !$rogue01443.lateTTTCapAdapted -or !$rogue01443.thirdCappedChoiceRejected -or !$rogue01443.duplicateVanillaChoiceAvoided) { throw '01443 rogueSwashbuckler01443Contracts exact native-path contract failed.' }
$caller01443 = $psychicResults.migration01443CallerContracts
if ($null -eq $caller01443 -or !$caller01443.passed -or $caller01443.assertions -lt 632 -or $caller01443.actualDispatches -ne 4 -or $caller01443.moduleCount -ne 3 -or $caller01443.privateResourceCount -ne 69 -or $caller01443.nativeHydratedDependencies -ne 44 -or $caller01443.nativeOnEnableCalls -ne 44 -or !$caller01443.actualRegisterAllDispatcher -or !$caller01443.actualRegisterOptionalDispatcher -or !$caller01443.exactNewRootsAndRetriedIdentities -or !$caller01443.actualCatalogueDefinitionsAndCallerStates -or !$caller01443.nonTargetActionsSkippedWithoutSuccessReports -or !$caller01443.nonTargetGraphsAndNativeDonorElementsPreserved -or !$caller01443.oldPoolReferencesAndOrderPreserved) { throw '01443 migration01443CallerContracts exact native-path contract failed.' }
$repairUrban01442 = $psychicResults.urbanControlledRepair01442Contracts
if ($null -eq $repairUrban01442 -or !$repairUrban01442.passed -or $repairUrban01442.additionalMigrationItemCount -ne 0 -or $repairUrban01442.persistentToggleCount -ne 9 -or $repairUrban01442.performanceButtonCount -ne 1 -or $repairUrban01442.allocationCases -ne 38 -or !$repairUrban01442.noNewHarmonyTargets -or !$repairUrban01442.nativeAllocationRanks -or !$repairUrban01442.nativeCastingConditionAbsent -or !$repairUrban01442.preservesOtherRestrictions -or !$repairUrban01442.liveReallocation -or !$repairUrban01442.nativeSpellAvailability -or !$repairUrban01442.nativeLegacyGrantReconciliation -or !$repairUrban01442.nativeLegacyCastingCleanup -or !$repairUrban01442.nativeLegacyTemplateRebind) { throw '01443 urbanControlledRepair01442Contracts exact native-path contract failed.' }
$hedge01443 = $psychicResults.hedgeWitch01443Contracts
if ($null -eq $hedge01443 -or !$hedge01443.passed -or $hedge01443.assertions -lt 80 -or $hedge01443.additionalItemCount -ne 1 -or $hedge01443.privateResourceCount -ne 3 -or $hedge01443.newSpellRootCount -ne 0 -or $hedge01443.newHarmonyPatchCount -ne 0 -or $hedge01443.newSerializedComponentTypeCount -ne 0 -or $hedge01443.nativePreparedConversions -ne 9 -or $hedge01443.nativePreparedSlotSpends -ne 9 -or $hedge01443.publicationRollbackFaults -ne 2 -or $hedge01443.actualRegisterAllDispatches -ne 1 -or !$hedge01443.baseProgressionPreserved -or !$hedge01443.onlyOwnConversionRemoved -or !$hedge01443.nativeSkillBonusAddedAndRemoved) { throw '01443 hedgeWitch01443Contracts exact native-path contract failed.' }
$icons01443 = $psychicResults.urbanAllocationIcons01443Contracts
if ($null -eq $icons01443 -or !$icons01443.passed -or $icons01443.toggleCount -ne 9 -or $icons01443.newPrivateResources -ne 0 -or !$icons01443.mechanicsUnchanged) { throw '01443 urbanAllocationIcons01443Contracts exact native-path contract failed.' }
# END 01443 ADDITIVE CONTRACT GATES
# BEGIN 01444 ADDITIVE CONTRACT GATES
$terror044 = $psychicResults.terrorWeaver01444Contracts
if ($null -eq $terror044 -or !$terror044.passed -or $terror044.assertions -lt 86 -or $terror044.additionalItemCount -ne 1 -or $terror044.privateResourceCount -ne 9 -or $terror044.newSpellRootCount -ne 0 -or $terror044.newHarmonyPatchCount -ne 0 -or $terror044.newSerializedComponentTypeCount -ne 0 -or $terror044.cacheRollbackFaults -ne 1 -or $terror044.connectionRollbackFaults -ne 1 -or $terror044.conditionalGrants -ne 4 -or $terror044.resourceSpends -ne 4 -or $terror044.nativeArcanaRules -ne 0 -or $terror044.nativeSummonEvents -ne 0 -or $terror044.nativeStanceStarts -ne 0 -or $terror044.nativeDemoralizeActions -ne 0 -or $terror044.nativeDeferredResolveCallbacks -ne 0 -or !$terror044.baseProgressionPreserved) { throw '01444 terrorWeaver01444Contracts exact native-path contract failed.' }
$primal044 = $psychicResults.primalSorcerer01444Contracts
if ($null -eq $primal044 -or !$primal044.passed -or $primal044.assertions -lt 265 -or $primal044.additionalItemCount -ne 1 -or $primal044.privateResourceCount -ne 18 -or $primal044.newSpellRootCount -ne 0 -or $primal044.newHarmonyPatchCount -ne 0 -or $primal044.newSerializedComponentTypeCount -ne 1 -or $primal044.cacheRollbackFaults -ne 1 -or $primal044.connectionRollbackFaults -ne 1 -or $primal044.conditionalGrants -ne 0 -or $primal044.resourceSpends -ne 0 -or $primal044.nativeArcanaRules -ne 8 -or $primal044.nativeSummonEvents -ne 8 -or $primal044.nativeStanceStarts -ne 0 -or $primal044.nativeDemoralizeActions -ne 0 -or $primal044.nativeDeferredResolveCallbacks -ne 0 -or !$primal044.baseProgressionPreserved) { throw '01444 primalSorcerer01444Contracts exact native-path contract failed.' }
$rake044 = $psychicResults.rake01444Contracts
if ($null -eq $rake044 -or !$rake044.passed -or $rake044.assertions -lt 287 -or $rake044.additionalItemCount -ne 1 -or $rake044.privateResourceCount -ne 22 -or $rake044.newSpellRootCount -ne 0 -or $rake044.newHarmonyPatchCount -ne 0 -or $rake044.newSerializedComponentTypeCount -ne 1 -or $rake044.cacheRollbackFaults -ne 1 -or $rake044.connectionRollbackFaults -ne 1 -or $rake044.conditionalGrants -ne 0 -or $rake044.resourceSpends -ne 0 -or $rake044.nativeArcanaRules -ne 0 -or $rake044.nativeSummonEvents -ne 0 -or $rake044.nativeStanceStarts -ne 5 -or $rake044.nativeDemoralizeActions -ne 30 -or $rake044.nativeDeferredResolveCallbacks -ne 0 -or !$rake044.baseProgressionPreserved) { throw '01444 rake01444Contracts exact native-path contract failed.' }
$caller044 = $psychicResults.archetypeCaller01444Contracts
if ($null -eq $caller044 -or !$caller044.passed -or $caller044.dispatches -ne 2 -or $caller044.moduleCount -ne 3 -or $caller044.privateResourceCount -ne 49 -or !$caller044.allPriorGraphsPreserved) { throw '01444 archetypeCaller01444Contracts exact native-path contract failed.' }
$repair044 = $psychicResults.repair01444Contracts
if ($null -eq $repair044 -or !$repair044.passed -or $repair044.additionalMigrationItemCount -ne 0 -or $repair044.rayDamageRules -ne 40 -or $repair044.weaponDamageRules -ne 30 -or $repair044.multiRaySalvos -ne 5 -or $repair044.repeatDeliveriesChecked -ne 50 -or $repair044.nativeUICloneAndRollbackCases -ne 4 -or $repair044.resistanceCases -ne 28 -or $repair044.nativeDRApplications -ne 104 -or $repair044.englishRestoredKeys -ne 106 -or $repair044.nativeLocaleReads -ne 212 -or !$repair044.noGlobalSneakMutation -or !$repair044.allNativeGroupIdentitiesPreserved -or !$repair044.activeLocaleRespected -or !$repair044.existingResistancePlusFive) { throw '01444 repair01444Contracts exact native-path contract failed.' }
# END 01444 ADDITIVE CONTRACT GATES
# BEGIN 01444 REGISTRATION ORDER GATE
$order044 = $psychicResults.registrationOrder01444Contracts
if ($null -eq $order044 -or !$order044.passed -or $order044.assertions -lt 331 -or $order044.catalogueModulesReviewed -ne 93 -or $order044.dispatcherEntriesChecked -ne 213 -or $order044.modulesReached -ne 91 -or $order044.optionalPasses -ne 3 -or $order044.actualColdDispatches -ne 4 -or $order044.preFixDependencyFailures -ne 1 -or $order044.injectedProviderFailures -ne 1 -or $order044.providerFailureRetries -ne 1 -or !$order044.actualDispatcherOrder -or !$order044.oldOrderRejected -or !$order044.actualColdAmplificationAndTerrorBodies -or !$order044.actualPsychicArchetypeOptionPresent -or !$order044.allOtherProductionBodiesPreserved) { throw '01444 actual dispatcher dependency/cold registration contract failed.' }
# END 01444 REGISTRATION ORDER GATE
# BEGIN 01444 REAL STARTUP RETRY GATE
$startup044 = $psychicResults.startupRetry01444Contracts
if ($null -eq $startup044 -or !$startup044.passed -or $startup044.assertions -lt 88 -or $startup044.scenarios -ne 4 -or $startup044.cacheReadyCallbacks -ne 14 -or $startup044.localizationReadyCallbacks -ne 13 -or $startup044.reentrantCallbacks -ne 8 -or $startup044.interruptedTraversals -ne 1 -or $startup044.actualRegisterAllPasses -ne 5 -or $startup044.actualAmplificationBodies -ne 8 -or $startup044.actualTerrorBodies -ne 8 -or $startup044.patchInstallerCalls -ne 4 -or $startup044.actualProbeExecutions -ne 4 -or !$startup044.realEntryRecovery -or !$startup044.actualAmplificationAndTerrorBodies -or !$startup044.completedInitialModulesSkipped -or !$startup044.patchInstallationOncePerStartup -or !$startup044.actualHarmonyProbe -or !$startup044.originalParentGraphPreserved) { throw '01444 real startup callback recovery contract failed.' }
# END 01444 REAL STARTUP RETRY GATE
$save01442 = $psychicResults.saveLoadTimers01442Contracts
if ($null -eq $save01442 -or !$save01442.passed -or $save01442.assertions -le 0 -or $save01442.nativeQueueRebuilds -ne 4 -or $save01442.nativeUnitOnPostLoadBodies -ne 1 -or $save01442.nativeJsonRestoredBuffs -ne 4 -or $save01442.descriptorBindingEndpoints -ne 1 -or !$save01442.descriptorWithoutUnit -or !$save01442.allTimerEntrypointsGuarded -or !$save01442.nativeQueueBeforeDescriptorBinding -or !$save01442.frozenReceiptPreservedAcrossBinding -or !$save01442.singleElapsedCreditAfterRebind) { throw '01442 real save-load timer lifecycle contracts failed.' }
$war01441 = $psychicResults.warDrummer01441Contracts
if ($war01441.passed -ne $true -or $war01441.assertions -lt 160 -or $war01441.additionalItemCount -ne 1 -or @($war01441.itemIds).Count -ne 1 -or $war01441.itemIds[0] -cne 'COTW-0033' -or $war01441.archetypeCount -ne 1 -or $war01441.ordinaryFeatCount -ne 0 -or $war01441.newSpellRootCount -ne 0 -or @($war01441.newBlueprints).Count -ne 13 -or $war01441.nativeSongApplications -ne 18 -or $war01441.nativeDamageHandlers -lt 1 -or $war01441.nativeCriticalHandlers -ne 6 -or $war01441.postCacheWriteFaults -ne 1 -or $war01441.postAppendFaults -ne 1 -or $war01441.newHarmonyPatchCount -ne 0 -or $war01441.newSerializedComponentTypeCount -ne 0 -or !$war01441.baseProgressionPreserved -or !$war01441.personalOnlyRhythm) { throw '041 permits exactly one War Drummer archetype with the native lifecycle contract.' }
$dismiss01440 = $psychicResults.timeStopDismiss01440Contracts
if ($dismiss01440.passed -ne $true -or $dismiss01440.assertions -lt 24 -or $dismiss01440.additionalItemCount -ne 0 -or $dismiss01440.newSpellRootCount -ne 0 -or @($dismiss01440.newBlueprints).Count -ne 1 -or $dismiss01440.newBlueprints[0].token -cne 'TimeStopDismiss01440' -or $dismiss01440.newBlueprints[0].type -cne 'Kingmaker.UnitLogic.Abilities.Blueprints.BlueprintAbility' -or $dismiss01440.nativeCasts -ne 4 -or $dismiss01440.nativeAreaEnds -ne 4 -or !$dismiss01440.sourceSpecificCleanup -or !$dismiss01440.independentSourcePreserved -or !$dismiss01440.unrelatedBuffPreserved) { throw '040 Time Stop manual-end native lifecycle contract failed.' }
$idr01439 = $psychicResults.increasedDamageReduction01439Contracts
if ($null -eq $idr01439 -or $idr01439.passed -ne $true -or $idr01439.assertions -lt 1) { throw '039 Increased Damage Reduction contract failed.' }
if ($idr01439.additionalItemCount -ne 1 -or @($idr01439.itemIds).Count -ne 1 -or $idr01439.itemIds[0] -cne 'COTW-0584' -or @($idr01439.newBlueprints).Count -ne 0 -or $idr01439.newSpellRootCount -ne 0 -or $idr01439.ordinaryFeatCount -ne 0) { throw '039 changes only the existing Increased Damage Reduction defensive power and adds no blueprint, feat or spell root.' }
if ($idr01439.nativeGapRank1 -ne 1 -or $idr01439.nativeGapRank2 -ne 2 -or $idr01439.scaledRank1 -ne 2 -or $idr01439.scaledRank2 -ne 4 -or $idr01439.directPartApplications -lt 1) { throw '039 must prove the native 1/2 gap, the scaled 2/4 result, and a native damage-reduction part application.' }
foreach ($entry in @(@('timeStutter01437Contracts',1),@('invoker01437Contracts',1),@('migration01437CallerContracts',0))) {
    $key = $entry[0]
    if ($psychicResults.$key.passed -ne $true -or $psychicResults.$key.assertions -le 0 -or $psychicResults.$key.additionalItemCount -ne $entry[1]) { throw "037 native contract failed: $key" }
}
foreach ($entry in @(@('superstitionRage01434Contracts',2),@('antimagicRage01434Contracts',3),@('migration01434CallerContracts',0))) {
    $key = $entry[0]
    if (!$psychicResults.$key.passed -or $psychicResults.$key.assertions -le 0 -or $psychicResults.$key.additionalItemCount -ne $entry[1] -or $psychicResults.$key.newBlueprints.Count -le 0) { throw "01434 selected migration contract failed: $key" }
}
$migration01434Ids=@($psychicResults.superstitionRage01434Contracts.itemIds)+@($psychicResults.antimagicRage01434Contracts.itemIds)
$expected01434Ids=@('COTW-0262','COTW-0263','COTW-0264','COTW-0284','COTW-0285')
if ($migration01434Ids.Count -ne 5 -or @($migration01434Ids | Sort-Object -Unique).Count -ne 5 -or @(Compare-Object $expected01434Ids $migration01434Ids).Count -ne 0) { throw '01434 must cover the five selected original rage-power rows exactly once.' }
foreach ($field in @('actualRegisterAllDispatcher','actualRegisterOptionalDispatcher','fiveIndependentModulesBeforeFerocious','ferociousConstantExtractionIncludesFiveChildren','realAppendFaultIsolatedToWitchHunter','laterModulesExecuteAfterFailure','failedCacheStatusHonestNewStartupRecovers','fourNativePoolsPreserveOldEntries','lateGroupOnlyTttPoolHasFiveRootsOnce','optionalAbsentDisabledMalformedPreserveCore','actualCatalogueDefinitionsAndReportedStates','nonTargetActionsScopedOutWithoutSuccessReports','freshNativeHostComponentsFromRealArchive','freshSelectionArraysAndUncachedReferences','parentFullCacheIdentityComponentsPoolsAndLazyPointersPreserved','globalTextKeysAndValuesPreserved')) {
    if (!$psychicResults.migration01434CallerContracts.$field) { throw "01434 real registration caller coverage incomplete: $field" }
}
foreach ($field in @('nativeApplyPassFailAndExactLists','multiTargetAndOverlapSaveDeduplication','nativeOuterSaveNotDoubled','nativeFinalHealingMath','fakeInterruptedAndSourceBoundaries','nativeCompetenceBonusAndClassScaling','nativeAppendedHostActionLifecycle','nativeCasterAndReceiverOwnershipGates','nativeDamageSubscriptionProxy','nativeAbilityCollectionAndClassBookBoundaries','nativeDirectAndManagerTurnOffOn','strongSourceTurnOffRestoresWeaker','outerFixtureInputsAndPatchesRestored','nativeStickyTouchChargeAndDeliveryHealing','unrelatedAbilityParentsCannotBorrowHarmlessEligibility')) {
    if (!$psychicResults.superstitionRage01434Contracts.$field) { throw "01434 Superstition/Witch Hunter native coverage incomplete: $field" }
}
foreach ($field in @('nativeRegistrationAndRepeatIdentity','nativePoolAppendRollback','nativeTouchACMoraleAndFullCap','ordinaryAndFlatFootedACPreserved','nativeOutgoingGhostPhysicalScope','nativeDisruptiveFactOwnership','nativeDisruptiveAreaEntryExitAndConcentration','nativeCounterstrikeLifecycleComposes','nativeDefensiveSuccessMagusQueueAllowanceAndCommand','spellbreakerUsesOriginalCotwSuccessRule','nativeCounterstrikePostRemovalReconciliation','nativeWandWielderRemovalReconcilesOnlyOwnReceipts','nativeDisruptiveAcquisitionHandoff','nativeDisruptiveMultipleSourcesAndSerializedReceipt','nativeOpportunityIdentityRefreshPreservesIL')) {
    if (!$psychicResults.antimagicRage01434Contracts.$field) { throw "01434 Antimagic native coverage incomplete: $field" }
}
if ($psychicResults.antimagicRage01434Contracts.newHarmonyTargetCount -ne 2) { throw '01434 must retain exactly two reviewed new native Harmony targets.' }
if ($psychicResults.superstitionRage01434Contracts.originalWillRows -ne 98 -or $psychicResults.superstitionRage01434Contracts.originalFortRows -ne 36 -or $psychicResults.superstitionRage01434Contracts.originalHealingRows -ne 20) { throw '01434 must retain exact complete original harmless-spell lists.' }
foreach ($entry in @(@('spiritualGuardian01433Contracts',1),@('kineticInvocation01433Contracts',1),@('kineticUtility01433Contracts',3))) {
    $key = $entry[0]
    if (!$psychicResults.$key.passed -or $psychicResults.$key.assertions -le 0 -or $psychicResults.$key.additionalItemCount -ne $entry[1] -or $psychicResults.$key.newBlueprints.Count -le 0) { throw "01433 selected migration contract failed: $key" }
}
foreach ($field in @('registrationRollbackAndIdentity','nativeSelectionPreview','nativeInitialSRPath','nativeRedirectAndAoOSRPaths','nativeControllerBindingAndIteratives','nativeDamageCalculation','casterAndSpellScope','serializedCastTimeSnapshot','resolvedHistoricalAttackLog')) {
    if (!$psychicResults.spiritualGuardian01433Contracts.$field) { throw "01433 Spiritual Guardian coverage incomplete: $field" }
}
foreach ($field in @('actualNativeCanSelect','nativeBurnCostAndSpend','nativePrepareCastAndRulebookParams','sourceComponentsAndPrivateGraphPreserved','providerAbsentDisabledLate','twoPoolAppendRollback','foreignPartialGraphRejected','publishedRetryRollback','nativeFactManagerAndEventBus','unmodifiedClearAcceptedBurnEventBus','exactCasterAndPrivateOriginOwnership','parentChildOrigin','nativeSpellSamePayloadPreserved','partialHealAndIncreasePreserveEffects','failedCleanupRetryPreservesLaterCast','zeroCostOwnRestCleanup','missingPendingRefsPruned','dataReferenceIdsRoundTrip','postLoadPendingEntry','donorSchoolsAndScalesAndSkin','nativeManagerRestartPreservesEffects','nativeReapplyAndTemporaryDeactivatePreserveEffects','nativeFeatRemovalRevokesEffects','publishedOptionalFailureIsolated','actualParentPublicationBeforeUrgent','outerFixtureInputsAndPatchesRestored','outerFixturePrefixesRestored','outerControlledDiceQueuePreserved')) {
    if (!$psychicResults.kineticInvocation01433Contracts.$field) { throw "01433 Kinetic Invocation coverage incomplete: $field" }
}
if ($psychicResults.kineticInvocation01433Contracts.talentCount -ne 7 -or $psychicResults.kineticInvocation01433Contracts.rootFeatCount -ne 1 -or $psychicResults.kineticInvocation01433Contracts.newSpellRootCount -ne 0) { throw '01433 Kinetic Invocation one-feat/seven-leaf scope failed.' }
foreach ($field in @('nativeSelectionAndPrerequisites','independentRegistrationAndRollback','nativeFogOnlyCalculate','replacementFogCalculate','nestedAndExceptionRestore','nativeFactManagerAndEventBus','dynamicColdResistanceAndOverlap','burnClearRemovesActivation','nativeSmokeAreaSaveAndApply','nativeColdRootRemoveAndReapply','nativeColdSaveRestart','nativeColdZeroBurnRestRemovalLoop','outerFixtureInputsAndPatchesRestored','outerFixturePrefixesRestored','outerControlledDiceQueuePreserved')) {
    if (!$psychicResults.kineticUtility01433Contracts.$field) { throw "01433 kinetic utility coverage incomplete: $field" }
}
foreach ($key in @('registrationFeatKi01432Contracts','registrationRemoval01432Contracts')) {
    if (!$psychicResults.$key.passed -or $psychicResults.$key.assertions -le 0) { throw "01432 registration/removal contract failed: $key" }
}
foreach ($field in @('initialAndActualOptionalRefresh','unresolvedBranchesDeferredUntilReady','recursiveAndExtraTalentExcluded','donorLateMutationPreservesOwnedGraph','ownedReferenceAndDefinitionValidation','foreignPartialGraphRejected','optionalProvidersAbsentDisabledLate','isolatedNativeReferenceCaches','parentDaemonDefinitionsAndCachedPointersPreserved','knownMetadataDonorPreparedInPlace','realAndForeignDonorsNeverOverwritten','globalTextKeysAndValuesPreserved')) {
    if (!$psychicResults.registrationFeatKi01432Contracts.$field) { throw "01432 Feat/Ki coverage incomplete: $field" }
}
if ($psychicResults.registrationRemoval01432Contracts.retiredRootCount -ne 3 -or $psychicResults.registrationRemoval01432Contracts.retiredAuthoredAssetCount -ne 9 -or $psychicResults.registrationRemoval01432Contracts.retiredTypeIds.Count -ne 3 -or !$psychicResults.registrationRemoval01432Contracts.actualModuleCatalogueAndStatusCaller -or !$psychicResults.registrationRemoval01432Contracts.allCachedSelectionsAndComponentGraphsClean -or !$psychicResults.registrationRemoval01432Contracts.nativeDonorIdentityAndGraphPreserved -or !$psychicResults.registrationRemoval01432Contracts.foreignSameNameAnaloguePreserved) { throw '01432 Atavism retirement scope or analogue preservation is incomplete.' }
foreach ($entry in @(@('anger01431Contracts',1),@('rage01431Contracts',2),@('wizard01431Contracts',2))) {
    $contract=$psychicResults.($entry[0])
    if (!$contract.passed -or $contract.additionalItemCount -ne $entry[1] -or $contract.assertions -le 0 -or $contract.newBlueprints.Count -le 0) { throw "01431 new module contracts failed: $($entry[0])" }
}
$migration01431Ids=@($psychicResults.anger01431Contracts.itemIds)+@($psychicResults.rage01431Contracts.itemIds)+@($psychicResults.wizard01431Contracts.itemIds)
$expected01431Ids=@('COTW-0281','COTW-0282','COTW-0299','COTW-0375','COTW-0377')
if ($migration01431Ids.Count -ne 5 -or @($migration01431Ids | Sort-Object -Unique).Count -ne 5 -or @(Compare-Object $expected01431Ids $migration01431Ids).Count -ne 0) { throw '01431 must cover the five selected original migration rows exactly once.' }
if (!$psychicResults.howl01428NativeResolution.passed -or !$psychicResults.howl01428NativeResolution.feedbackApiPresent -or !$psychicResults.howl01428NativeResolution.nativeIsPassedUnpatched -or !$psychicResults.howl01428NativeResolution.nativeApplyBuffUnpatched -or $psychicResults.howl01428NativeResolution.savingThrowResolutions -le 0) { throw '01428 native Howl resolution and feedback contracts failed.' }
if (!$psychicResults.inquisitions01428Contracts.passed -or $psychicResults.inquisitions01428Contracts.additionalItemCount -ne 2 -or !$psychicResults.inquisitions01428Contracts.publicationAndAppendRollback -or !$psychicResults.inquisitions01428Contracts.nativeHoldPerRoundSaves -or !$psychicResults.inquisitions01428Contracts.nativeBuffAndConditionImmunity -or !$psychicResults.inquisitions01428Contracts.nativeSpellImmunityGate -or $psychicResults.inquisitions01428Contracts.nativeSavingThrows -le 0) { throw '01428 two inquisition native contracts failed.' }
if (!$psychicResults.wizardSubschools01428Contracts.passed -or $psychicResults.wizardSubschools01428Contracts.additionalItemCount -ne 2 -or $psychicResults.wizardSubschools01428Contracts.nativeSelectFeatureApplyCalls -le 0 -or $psychicResults.wizardSubschools01428Contracts.nativeLevelGrants -le 0 -or $psychicResults.wizardSubschools01428Contracts.nativeLevelRemovals -le 0) { throw '01428 two wizard school native contracts failed.' }
if (!$psychicResults.purifyingFlames01428Native.passed -or $psychicResults.purifyingFlames01428Native.additionalItemCount -ne 1 -or !$psychicResults.purifyingFlames01428Native.nativeBurnCostGateSpend -or !$psychicResults.purifyingFlames01428Native.nativeNeutralizePoisonAction -or $psychicResults.purifyingFlames01428Native.nativeBurnSpendEvents -le 0 -or $psychicResults.purifyingFlames01428Native.nativeDispelRules -le 0) { throw '01428 Purifying Flames native contracts failed.' }

if (!$psychicResults.howl01427Contracts.passed -or !$psychicResults.howl01427Contracts.nativeExecutionProcess) { throw '01427 native Howl execution contracts failed.' }
if (!$psychicResults.ki01427Contracts.passed -or $psychicResults.ki01427Contracts.additionalItemCount -ne 5 -or !$psychicResults.ki01427Contracts.secondPoolRollback -or $psychicResults.ki01427Contracts.nativeApplyCalls -lt 30 -or $psychicResults.ki01427Contracts.newBlueprints.Count -ne 120) { throw '01427 five ki powers native contracts failed.' }
if (!$psychicResults.prerequisites01426Contracts.passed -or !$psychicResults.prerequisites01426Contracts.nativeCanSelect -or !$psychicResults.prerequisites01426Contracts.noGrantedCriticalFacts) { throw '01426 Reaping Stalker qualification contracts failed.' }
if (!$psychicResults.migration01426Contracts.passed -or $psychicResults.migration01426Contracts.additionalItemCount -ne 5 -or $psychicResults.migration01426Contracts.nativeSelectFeatureApplyCalls -ne 2 -or $psychicResults.migration01426Contracts.transactionFaults -ne 6) { throw '01426 five-item native migration/rollback contracts failed.' }
if (!$psychicResults.talent01425Contracts.passed -or !$psychicResults.talent01425Contracts.nativeOnlyComponents -or !$psychicResults.talent01425Contracts.nativeSelectionPredicates -or $psychicResults.talent01425Contracts.nativeRuleCases -lt 27) { throw 'Reaping Stalker native registration/selection/weapon contracts failed.' }
if (!$psychicResults.feat01424Contracts.passed -or !$psychicResults.feat01424Contracts.actualOptionalRegistration -or !$psychicResults.feat01424Contracts.nativeSelectionFlow.passed) {
    throw 'Extra Investigator Talent registration/native-selection contracts did not pass.'
}
& dotnet build (Join-Path $projectRoot 'tests/Startup01422Tests.csproj') @buildArgs
if ($LASTEXITCODE -ne 0) { throw 'Real blueprint-cache startup test build failed.' }
$cacheStartupPath = Join-Path $workspaceRoot 'work/prototype-build/startup01422-results.json'
& (Join-Path $workspaceRoot 'work/prototype-build/Startup01422Tests/bin/Release/net472/Startup01422Tests.exe') $managed $assemblyPath $cacheStartupPath
if ($LASTEXITCODE -ne 0) { throw 'Real blueprint-cache LoadPackTOC postfix regression failed.' }
$cacheStartupResults = Get-Content -LiteralPath $cacheStartupPath -Raw | ConvertFrom-Json
# Keep cache concurrency on the untouched native DLL. Medium's bundled
# BlueprintCore directly accesses native private fields, so its separate process
# uses the existing visibility-only fixture and a UMM environment boundary.
$mediumListDirectory = Join-Path $workspaceRoot 'work/prototype-build/medium-list-contract'
$mediumLocalPaths = [xml](Get-Content -LiteralPath (Join-Path $mediumRoot 'MediumClass.local.props') -Raw)
& (Join-Path $projectRoot 'tests/Prepare-MediumListBoundary.ps1') -ReferenceDirectory ([string]$mediumLocalPaths.Project.PropertyGroup.UMMPath) -CecilAssembly (Join-Path $mediumRoot 'tests/HarmonyCompatibility/bin/Release/net9.0/Mono.Cecil.dll') -OutputDirectory $mediumListDirectory
$mediumListResultPath = Join-Path $mediumListDirectory 'results.json'
& (Join-Path $workspaceRoot 'work/prototype-build/Startup01422Tests/bin/Release/net472/Startup01422Tests.exe') $managed $assemblyPath $mediumListResultPath (Join-Path $mediumStage 'MediumClass.dll') $mediumListDirectory (Join-Path $startupRegressionDirectory 'startup-fixture/Assembly-CSharp.dll') 'medium-only'
if ($LASTEXITCODE -ne 0) { throw 'Actual Medium spell-list fixed-index contract failed.' }
$mediumListResults = (Get-Content -LiteralPath $mediumListResultPath -Raw | ConvertFrom-Json).mediumListContract
if (!$mediumListResults.passed -or !$mediumListResults.actualCreateSpellList -or $mediumListResults.length -ne 11 -or $mediumListResults.maxLevel -ne 4) { throw 'Medium spell-list result is incomplete.' }
# Requested Way of the Shield integration: exact staged module, pinned Harmony.
$wayRegressionDirectory = Join-Path $workspaceRoot 'work/prototype-build/way-shield-regressions'
$null = New-Item -ItemType Directory -Path $wayRegressionDirectory -Force
$wayNative = Join-Path $wayRegressionDirectory 'native.json'
& python (Join-Path $wayRoot 'tests/Extract-Native.py') $WrathPath $wayNative
if ($LASTEXITCODE -ne 0) { throw 'Way of the Shield native extraction failed.' }
$wayResults = @()
foreach ($case in @(
    @{ name='EquipmentTests'; project='EquipmentTests/EquipmentTests.csproj'; args=@($managed,$wayAssembly) },
    @{ name='FeatTests'; project='FeatTests/FeatTests.csproj'; args=@($managed,$wayAssembly,(Join-Path $wayRegressionDirectory 'feat-results.json')) },
    @{ name='SacredTests'; project='SacredTests/SacredTests.csproj'; args=@($managed,$wayAssembly) },
    @{ name='WayShieldBlueprintTests'; project='BlueprintTests/WayShieldBlueprintTests.csproj'; args=@($managed,$wayAssembly,$wayNative,(Join-Path $wayRegressionDirectory 'blueprint-results.json'),$assemblyPath) }
)) {
    & dotnet build (Join-Path $wayRoot ('tests/'+$case.project)) @buildArgs
    if ($LASTEXITCODE -ne 0) { throw "Shield test build failed: $($case.name)" }
    $exe = Join-Path $workspaceRoot ("work/prototype-build/$($case.name)/bin/Release/net472/$($case.name).exe")
    $lines = @(& $exe @($case.args) 2>&1)
    $code = $LASTEXITCODE
    $body = ($lines | ForEach-Object { $_.ToString() }) -join "`n"
    $body | Set-Content -LiteralPath (Join-Path $wayRegressionDirectory ($case.name+'.log')) -Encoding utf8
    Write-Output $body
    if ($code -ne 0) { throw "Shield test execution failed: $($case.name)" }
    $wayResults += [ordered]@{name=$case.name;exitCode=$code;log=(Join-Path $wayRegressionDirectory ($case.name+'.log'))}
}
$wayResults | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath (Join-Path $wayRegressionDirectory 'results.json') -Encoding utf8
# BEGIN 01444 SHIELD RETRY GATE
$shieldRetry044 = (Get-Content -LiteralPath (Join-Path $wayRegressionDirectory 'blueprint-results.json') -Raw | ConvertFrom-Json).shieldStartupRetry01444
if ($null -eq $shieldRetry044 -or !$shieldRetry044.passed -or $shieldRetry044.assertions -lt 28 -or $shieldRetry044.cacheReadyCallbacks -ne 2 -or $shieldRetry044.localizationReadyCallbacks -ne 3 -or $shieldRetry044.actualBridgeCalls -ne 3 -or $shieldRetry044.actualInstallRuntimeCalls -ne 1 -or $shieldRetry044.dependencyFailures -ne 2 -or $shieldRetry044.singleOwnerPatchEntries -ne 59 -or !$shieldRetry044.exactPatchSnapshotPreserved -or !$shieldRetry044.actualParentCallbacks -or !$shieldRetry044.actualShieldBridgeAndBuilders -or !$shieldRetry044.mountedTranspilerPreserved -or !$shieldRetry044.successfulRegistrationSkipped) { throw '01444 actual shield late-blueprint failure/startup retry contract failed.' }
# END 01444 SHIELD RETRY GATE

# BEGIN 01444 SHIELD BLUEPRINT RESULTS GATE
$faultCasesPath = Join-Path $wayRoot 'tests/BlueprintTests/RetryFaultCases01444.json'
$faultCases = @(Get-Content -LiteralPath $faultCasesPath -Raw | ConvertFrom-Json)
if ($faultCases.Count -lt 15 -or @($faultCases.case | Sort-Object -Unique).Count -ne $faultCases.Count) { throw 'Shield dependency fault case inventory incomplete.' }
$faultRows = @()
$priorShieldFault = $env:COTW_SHIELD_RETRY_FAULT
try {
    foreach ($fault in $faultCases) {
        $env:COTW_SHIELD_RETRY_FAULT = $fault | ConvertTo-Json -Depth 12 -Compress
        $resultPath = Join-Path $wayRegressionDirectory ('retry-'+$fault.case+'.json')
        $logPath = Join-Path $wayRegressionDirectory ('retry-'+$fault.case+'.log')
        $exe = Join-Path $workspaceRoot 'work/prototype-build/WayShieldBlueprintTests/bin/Release/net472/WayShieldBlueprintTests.exe'
        $lines = @(& $exe $managed $wayAssembly $wayNative $resultPath $assemblyPath 2>&1)
        $code = $LASTEXITCODE
        ($lines | ForEach-Object { $_.ToString() }) -join "`n" | Set-Content -LiteralPath $logPath -Encoding utf8
        if ($code -ne 0) { throw "Shield real callback dependency result case failed: $($fault.case); see $logPath" }
        $whole = Get-Content -LiteralPath $resultPath -Raw | ConvertFrom-Json
        $contract = $whole.shieldStartupRetry01444
        if (!$whole.passed -or $whole.assertions -lt 949 -or $whole.ownedAssets -ne 108 -or !$contract.passed -or $contract.assertions -lt 27 -or $contract.case -ne $fault.case -or $contract.completedBeforeFailure -ne $fault.completedBeforeFailure -or $contract.actualBridgeCalls -ne 3 -or $contract.actualInstallRuntimeCalls -ne 1 -or $contract.dependencyFailures -ne 2 -or $contract.singleOwnerPatchEntries -ne 59 -or !$contract.exactPatchSnapshotPreserved -or !$contract.actualParentCallbacks -or !$contract.actualShieldBridgeAndBuilders -or !$contract.successfulRegistrationSkipped -or ($fault.expectNormalReturn -and !$contract.actualNormalReturnRejected)) { throw "Shield callback result contract incomplete: $($fault.case)" }
        $faultRows += [ordered]@{fault=$fault;result=$resultPath;resultSHA256=(Get-FileHash -LiteralPath $resultPath -Algorithm SHA256).Hash;log=$logPath;logSHA256=(Get-FileHash -LiteralPath $logPath -Algorithm SHA256).Hash;contract=$contract}
        Write-Output "PASS actual shield dependency case: $($fault.case)"
    }
} finally { $env:COTW_SHIELD_RETRY_FAULT = $priorShieldFault }
$normalReturnCases = @($faultRows | Where-Object { $_.contract.actualNormalReturnRejected }).Count
if ($normalReturnCases -lt 1) { throw 'Actual missing-dependency normal-return case not covered.' }
$shieldBlueprintResults044 = [ordered]@{passed=$true;assertions=($faultRows.contract.assertions | Measure-Object -Sum).Sum;cases=$faultRows.Count;actualNormalReturnCases=$normalReturnCases;actualInstallRuntimeCalls=$faultRows.Count;ownerPatchEntriesPerProcess=59;actualParentCallbacks=$true;allRestoredViaCallbacks=$true;allPatchesSingle=$true;allPublishedIdentitiesPreserved=$true}
[ordered]@{passed=$true;contract=$shieldBlueprintResults044;cases=$faultRows} | ConvertTo-Json -Depth 16 | Set-Content -LiteralPath (Join-Path $wayRegressionDirectory 'blueprint-retry-results.json') -Encoding utf8
# END 01444 SHIELD BLUEPRINT RESULTS GATE

# BEGIN 01444 DOOM CASTING GATE
$doomCasting044 = $psychicResults.auraOfDoomCasting01444Contracts
if ($null -eq $doomCasting044 -or !$doomCasting044.passed -or $doomCasting044.assertions -lt 40 -or $doomCasting044.nativeAvailabilityCalls -ne 32 -or $doomCasting044.nativeContexts -ne 10 -or !$doomCasting044.legacyContextDescriptorInput -or !$doomCasting044.hostOnlyContextExemption -or !$doomCasting044.hostExplicitDescriptorPreserved -or !$doomCasting044.enemyAndForeignFearPreserved -or !$doomCasting044.logicalAndThoughtRulesPreserved -or !$doomCasting044.globalCastingRestrictionsPreserved -or !$doomCasting044.blueprintGraphsUnchanged) { throw '01444 Aura of Doom native casting/legacy descriptor contract failed.' }
# END 01444 DOOM CASTING GATE
# BEGIN 01445 ARCHETYPE GATES
$batch045 = $psychicResults.archetypes01445Contracts
if (!$batch045.passed -or $batch045.assertions -lt 400 -or $batch045.additionalItemCount -ne 5 -or $batch045.newHarmonyPatchCount -ne 0 -or $batch045.cacheRollbackFaults -ne 5 -or $batch045.connectionRollbackFaults -ne 5 -or $batch045.normalReturnMissingOutputsRejected -ne 5 -or !$batch045.originalParentGraphPreserved) { throw '01445 archetype batch contracts failed.' }
foreach ($pair in @(@('CourtBard01445Contracts',27),@('NatureBondedMagus01445Contracts',18),@('SageCounselor01445Contracts',11),@('GraveWarden01445Contracts',11),@('DrillSergeant01445Contracts',47))) {
    $row = $psychicResults.($pair[0])
    if (!$row.passed -or $row.assertions -lt 40 -or $row.privateResourceCount -ne $pair[1] -or $row.newHarmonyPatchCount -ne 0 -or $row.cacheRollbackFaults -ne 1 -or $row.connectionRollbackFaults -ne 1 -or !$row.normalReturnMissingOutputRejected -or !$row.nativeClassPreserved) { throw "01445 archetype contract failed: $($pair[0])" }
}
if (($psychicResults.CourtBard01445Contracts.nativeSerializedTypes + $psychicResults.NatureBondedMagus01445Contracts.nativeSerializedTypes + $psychicResults.SageCounselor01445Contracts.nativeSerializedTypes + $psychicResults.GraveWarden01445Contracts.nativeSerializedTypes + $psychicResults.DrillSergeant01445Contracts.nativeSerializedTypes) -ne 14) { throw '01445 native serialized type coverage failed.' }
$court9=$psychicResults.courtBardLevel901445Contracts
if (!$court9.passed -or $court9.assertions -lt 600 -or $court9.nativeActionbarRefreshQueries -ne 500) { throw 'Court Bard malformed free-list resource regression failed.' }
foreach ($flag in @('nativeLevel9Identified','nativeNullListNegativeControl','earlierAndDarkCodexFreeFactsRetained','nativeFiniteRoundSpend','nativeUiPostLoadRebuild','validListsAndOtherBardsUnchanged','sharedBlueprintsNotRewritten')) { if (!$court9.$flag) { throw "Court Bard regression missing: $flag" } }
$startup045 = $psychicResults.startupArchetypes01445Contracts
if (!$startup045.passed -or $startup045.assertions -lt 167 -or $startup045.scenarios -ne 5 -or $startup045.cacheReadyCallbacks -ne 10 -or $startup045.localizationReadyCallbacks -ne 18 -or $startup045.patchInstallerCalls -ne 5 -or !$startup045.actualNewModuleBodies -or !$startup045.completedModulesSkipped -or !$startup045.nativeDependencyRecovery -or !$startup045.originalParentPreserved) { throw '01445 actual startup retry contracts failed.' }
# END 01445 ARCHETYPE GATES

# BEGIN 01445 FOUR REPAIR GATES
$repair = $psychicResults.dustInvisibility01445Contracts
if (!$repair.passed -or $repair.assertions -lt 7 -or !$repair.nativeOffensiveController -or !$repair.oldFlagNegativeControl -or !$repair.preparationPreservesInvisibility -or !$repair.attackStillBreaksInvisibility) { throw 'dustInvisibility01445Contracts failed.' }
$repair = $psychicResults.symbiosisStacking01445Contracts
if (!$repair.passed -or $repair.assertions -lt 70 -or $repair.rankAndOrderCases -ne 10 -or !$repair.nativeLizardFamiliar -or !$repair.nonstackingPolicy -or !$repair.otherSourcesUnchanged) { throw 'symbiosisStacking01445Contracts failed.' }
$repair = $psychicResults.kiFeintTriggers01445Contracts
if (!$repair.passed -or $repair.assertions -lt 12 -or !$repair.nativePreAndPostTriggers -or !$repair.nativeAndKiWithAndWithoutCloak -or !$repair.temporaryCleanup -or !$repair.nativeFeatureUnmodified) { throw 'kiFeintTriggers01445Contracts failed.' }
$repair = $psychicResults.drillLatePool01445Contracts
if (!$repair.passed -or $repair.assertions -lt 15 -or !$repair.incrementalCacheRollback -or !$repair.missingOutputRejected -or !$repair.threeSelectionsUpdated -or !$repair.liveKnownFeatButton -or !$repair.actualShareAction -or !$repair.identitiesPreserved) { throw 'drillLatePool01445Contracts failed.' }
if (!$startup045.lateDrillRealCallbackRecovery) { throw 'Late Drill Sergeant real callback recovery failed.' }
# END 01445 FOUR REPAIR GATES

$arc046=$psychicResults.arcane01446Contracts
if (!$arc046.passed -or $arc046.assertions -lt 428 -or $arc046.additionalItemCount -ne 4 -or $arc046.retiredItemCount -ne 1 -or $arc046.newHarmonyTargets -ne 5 -or $arc046.cacheRollbackFaults -ne 2 -or $arc046.connectionRollbackFaults -ne 2 -or $arc046.normalReturnMissingOutputsRejected -ne 2 -or !$arc046.originalParentPreserved) { throw '01446 arcane rules/publication gate failed.' }
$fix046=$psychicResults.repair01446Contracts
if (!$fix046.passed -or $fix046.nativeGrowthAnchors -ne 119 -or $fix046.nativeFXViews -ne 4 -or $fix046.nullFXNegativeControls -ne 4 -or $fix046.nativeConversionOrderCases -ne 3 -or !$fix046.originalAuraDescription) { throw '01446 five-repair native paths missing.' }
$startup046=$psychicResults.startupArcane01446Contracts
if (!$startup046.passed -or $startup046.assertions -lt 74 -or $startup046.scenarios -ne 5 -or $startup046.cacheReadyCallbacks -ne 10 -or $startup046.localizationReadyCallbacks -ne 15 -or $startup046.patchInstallerCalls -ne 5 -or $startup046.normalReturnMissingOutputCases -ne 2 -or !$startup046.actualNewModuleBodies -or !$startup046.completedModulesSkipped -or !$startup046.nativeDependencyRecovery) { throw '01446 actual ready callback gate failed.' }
foreach ($key in @('bloodlineMutations01446','admixture01446','bloodlineRetirement01449Contracts','bloodlineCasting01446','admixtureCasting01446','arcaneLifecycle01446','naturePreview01450Contracts','bloodlineComposition01450Contracts')) { if (!$psychicResults.$key.passed) { throw "01446 sub-contract failed: $key" } }
foreach ($key in @('repair01447Contracts','bloodlineCompatibility01447Contracts','spellBatch01447Contracts','startupSpellBatch01447Contracts','naturesCommand01447Contracts','infectiousCharms01447Contracts')) { if (!$psychicResults.$key.passed) { throw "01447 required gate failed: $key" } }
if ($psychicResults.spellBatch01447Contracts.rollbackCases -ne 15 -or $psychicResults.spellBatch01447Contracts.newItemCount -ne 5) { throw '01447 five-item transaction gate failed.' }
if ($psychicResults.startupSpellBatch01447Contracts.scenarios -ne 10 -or $psychicResults.startupSpellBatch01447Contracts.patchInstallerCalls -ne 10) { throw '01447 real callback retry gate failed.' }
foreach ($key in @('feedback01448Contracts','bloodlineReference01448Contracts','natureTargeting01448Contracts','visualFeedback01448Contracts')) { if (!$psychicResults.$key.passed) { throw "01448 required gate failed: $key" } }
if ($psychicResults.feedback01448Contracts.assertions -lt 130 -or !$psychicResults.feedback01448Contracts.actualProviderConfigurator -or !$psychicResults.feedback01448Contracts.nativePrecastTarget -or !$psychicResults.feedback01448Contracts.nativeVisualDispatch) { throw '01448 all repair paths must execute.' }
if ($psychicResults.bloodlineReference01448Contracts.nativeBloodlines -ne 45 -or $psychicResults.bloodlineReference01448Contracts.seekerBloodlines -ne 21 -or $psychicResults.bloodlineReference01448Contracts.primalBloodlines -ne 4 -or !$psychicResults.bloodlineReference01448Contracts.nativeLevelUpCanSelect) { throw '01448 actual bloodline selection coverage failed.' }
foreach ($flag in @('nativeCanTarget','nativeTargetReason','separateBookAndItem','constructRetained','plantPlusConstructRetained','nativeNonforcedCast','lastTwoPoints','independentMindImmunity','logicalCannotForgePaid','nativeParameterJson')) { if (!$psychicResults.natureTargeting01448Contracts.$flag) { throw "01448 native Nature target contract failed: $flag" } }
if ($psychicResults.natureTargeting01448Contracts.paidMarker -ne [int]::MinValue -or $psychicResults.natureTargeting01448Contracts.resourceThresholds -ne 4 -or $psychicResults.visualFeedback01448Contracts.areaViews.Count -ne 5 -or !$psychicResults.visualFeedback01448Contracts.terrorImpact -or !$psychicResults.visualFeedback01448Contracts.terrorWeaverFourVariants -or !$psychicResults.visualFeedback01448Contracts.urbanMockeryTarget -or $psychicResults.visualFeedback01448Contracts.nullFXNegativeControls -ne 5) { throw '01448 marker/visual native dispatch coverage failed.' }

# Only the explicitly retired CotW mutation positive contract is superseded.
# Native growth/visual/conversion/lifecycle and every historical gate remain.
$new01449=@('bloodlineRetirement01449Contracts','bloodlineIntegration01449Contracts','ecmMutations01449Contracts','tomeClaws01449Contracts','bloodlineFingerprint01449Contracts','natureDiagnostics01449Contracts')
foreach ($key in $new01449) { if (!$psychicResults.$key.passed) { throw "01449 mandatory contract failed: $key" } }
$ret=$psychicResults.bloodlineRetirement01449Contracts
if ($ret.retiredAssetCount -ne 1 -or !$ret.legacyHavocInert -or !$ret.topplingUnchanged -or $ret.supersededByUser01450.Count -ne 2) { throw '01450 Havoc retirement / explicit two-item restoration incomplete.' }
$ecm=$psychicResults.ecmMutations01449Contracts
if ($ecm.variantQualifications -ne 25 -or $ecm.actualECMDamageCases -ne 24) { throw '01449 actual ECM paths incomplete.' }
foreach ($flag in @('ecmOwnFeatureAndHandler','noGrantAndUnrelatedNegatives','singleQualificationPatch','noDamagePrefix','ecmFeatEntryRetained')) { if (!$ecm.$flag) { throw "01449 ECM authority incomplete: $flag" } }
$tome=$psychicResults.tomeClaws01449Contracts
if ($tome.paths -ne 20 -or $tome.growthRows -ne 60 -or !$tome.actualReleasedOverhaul -or !$tome.sameReplacementAsUserLog -or !$tome.nonPowerParentFactsPreserved) { throw '01449 actual ToTF replacement/growth incomplete.' }
foreach ($flag in @('foreignAppendRejected','unrelatedRankRejected','originalRemovalRejected','knownProviderDuplicateNormalized','noForeignOverwrite','samePublishedIdentities')) { if (!$psychicResults.bloodlineFingerprint01449Contracts.$flag) { throw "01449 narrow receipt verification incomplete: $flag" } }
$diag=$psychicResults.natureDiagnostics01449Contracts
if ($diag.maximumLogsPerCaster -ne 64 -or $diag.userReportedFailureReproduced -ne $false) { throw '01449 diagnostic limits or honest unresolved status changed.' }
foreach ($flag in @('nativeTogglePath','nativeRejectionTrace','successfulQuerySilent','diagnosticFailureIsolated','noResourceSpend')) { if (!$diag.$flag) { throw "01449 diagnostic native boundary missing: $flag" } }
foreach ($flag in @('actualTomeOverhaul','actualECMConfigure','nativeProxyClone','ownHavocAbsentIntensityPiercingRestored')) { if (!$psychicResults.bloodlineIntegration01449Contracts.$flag) { throw "01449 actual integration missing: $flag" } }
$bloodlineLatePath=Join-Path $workspaceRoot 'work/prototype-build/01449-late-results.json'
$savedArcaneOnly=$env:COTW_ARCANE_01446_ONLY; $saved01449Case=$env:COTW_01449_CASE
try {
    $env:COTW_ARCANE_01446_ONLY='1'; $env:COTW_01449_CASE='bloodline-late'
    & $bloodlineHost $managed $assemblyPath $bloodlineLatePath $psychicNativePath $psychicExpansionNativePath
    if ($LASTEXITCODE -ne 0) { throw '01449 actual late ECM Configure failed.' }
} finally { $env:COTW_ARCANE_01446_ONLY=$savedArcaneOnly; $env:COTW_01449_CASE=$saved01449Case }
$bloodlineLate=Get-Content -LiteralPath $bloodlineLatePath -Raw | ConvertFrom-Json
if (!$bloodlineLate.bloodlineIntegration01449Contracts.actualLateECMConfigure -or !$bloodlineLate.bloodlineFingerprint01449Contracts.passed -or $bloodlineLate.ecmMutations01449Contracts.variantQualifications -ne 25 -or $bloodlineLate.tomeClaws01449Contracts.growthRows -ne 60) { throw '01449 late-provider evidence incomplete.' }

if (!$psychicResults.arcaneLifecycle01446.nativeHeldTouchTransfer -or !$psychicResults.arcaneLifecycle01446.nativeResourceCounter -or !$psychicResults.arcaneLifecycle01446.nativeRuleEventBus) { throw '01446 native lifecycle coverage missing.' }






$preview050=$psychicResults.naturePreview01450Contracts
foreach ($flag in @('actualPrestigePlusPostfix','actualMindThrustCanTarget','noPreviewPayment','unpaidExecutionRetainsImmunity','independentImmunityRetained','constructRetained','wrongBookRetained','scopeRestored')) { if (!$preview050.$flag) { throw "01450 actual Nature preview missing: $flag" } }
if (!$preview050.passed -or $preview050.assertions -lt 117) { throw '01450 Nature preview checks incomplete.' }
$composition050=$psychicResults.bloodlineComposition01450Contracts
foreach ($flag in @('sameOrderDuplicateNotification','independentPaymentAndRefund','noTopplingCollision','originalParamsRetained','actualRankAndDamagePrefixes','nativeChildContext','nativeDescriptorSerialization')) { if (!$composition050.$flag) { throw "01450 paid cast composition missing: $flag" } }
if (!$composition050.passed -or $composition050.assertions -lt 62 -or $composition050.eventOrders -ne 6) { throw '01450 all six paid-mode event orders required.' }
if (!$psychicResults.bloodlineFingerprint01449Contracts.permutationPreserved) { throw '01450 pure provider selection sorting must be preserved.' }
$external01450Path=Join-Path $workspaceRoot 'work/prototype-build/01450-external-results.json'
& (Join-Path $projectRoot 'tests/Test-Feedback01450.ps1') -WrathPath $WrathPath -HostPath $bloodlineHost -MainAssembly $assemblyPath -NativePath $psychicNativePath -ExpansionPath $psychicExpansionNativePath -OutputPath $external01450Path
$external01450=Get-Content -LiteralPath $external01450Path -Raw | ConvertFrom-Json
if (!$external01450.passed -or $external01450.rows.Count -ne 3) { throw '01450 all real provider/native/fault paths required.' }
$external01451Path=Join-Path $workspaceRoot 'work/prototype-build/01451-results.json'
& (Join-Path $projectRoot 'tests/Test-Feedback01451.ps1') -WrathPath $WrathPath -HostPath $bloodlineHost -MainAssembly $assemblyPath -NativePath $psychicNativePath -ExpansionPath $psychicExpansionNativePath -OutputPath $external01451Path
$external01451=Get-Content -LiteralPath $external01451Path -Raw | ConvertFrom-Json
if (!$external01451.passed -or $external01451.rows.Count -ne 7) { throw '01451 all seven native/provider/arrival/fault matrices required.' }
$external01452Path=Join-Path $workspaceRoot 'work/prototype-build/01452-results.json'
& (Join-Path $projectRoot 'tests/Test-Feedback01452.ps1') -WrathPath $WrathPath -HostPath $bloodlineHost -MainAssembly $assemblyPath -NativePath $psychicNativePath -ExpansionPath $psychicExpansionNativePath -OutputPath $external01452Path
$external01452=Get-Content -LiteralPath $external01452Path -Raw | ConvertFrom-Json
if (!$external01452.passed -or $external01452.rows.Count -ne 2) { throw '01452 actual parent/generated menu/native UI/commit and single-notification-fault cases required.' }
$external01453Path=Join-Path $workspaceRoot 'work/prototype-build/01453-results.json'
& (Join-Path $projectRoot 'tests/Test-Feedback01453.ps1') -WrathPath $WrathPath -HostPath $bloodlineHost -MainAssembly $assemblyPath -NativePath $psychicNativePath -ExpansionPath $psychicExpansionNativePath -OutputPath $external01453Path
$external01453=Get-Content -LiteralPath $external01453Path -Raw | ConvertFrom-Json
if (!$external01453.passed -or $external01453.rows.Count -ne 3) { throw '01453 actual released ECM scanner both orders, native restrictions, strict negatives/isolation and real052 package control required.' }


$magus01454Path=Join-Path $workspaceRoot ("work/prototype-build/01454-required-"+[guid]::NewGuid().ToString("N")+".json")
& python (Join-Path $projectRoot "tests/Test-Magus01454.py") $assemblyPath $magus01454Path
if ($LASTEXITCODE -ne 0) { throw "01454 all five mandatory native-path suites failed." }
$magus01454=Get-Content -LiteralPath $magus01454Path -Raw | ConvertFrom-Json
if (!$magus01454.passed -or !$magus01454.completeScopeContracts -or $magus01454.rows.Count -ne 5 -or !$magus01454.allRequiredNamedNativePaths -or $magus01454.additionalMigrationItemCount -ne 3) { throw "01454 complete scope contracts required before packaging." }

$magus055Path=Join-Path $workspaceRoot ("work/prototype-build/01455-required-"+[guid]::NewGuid().ToString("N")+".json")
& python (Join-Path $projectRoot "tests/Test-MagusFeedback01455.py") $assemblyPath $magus055Path
if ($LASTEXITCODE -ne 0) { throw "01455 actual feedback/native ownership and old054 controls failed." }
$magus055=Get-Content -LiteralPath $magus055Path -Raw | ConvertFrom-Json
$skirnir056Path=Join-Path $workspaceRoot ("work/prototype-build/01456-required-"+[guid]::NewGuid().ToString("N")+".json")
& python (Join-Path $projectRoot "tests/Test-Skirnir01456.py") $assemblyPath $skirnir056Path
if ($LASTEXITCODE -ne 0) { throw "01456 native shield AC / Greater Spell Combat contracts failed." }
$skirnir056=Get-Content -LiteralPath $skirnir056Path -Raw | ConvertFrom-Json
if (!$skirnir056.passed -or $skirnir056.rows.Count -ne 2 -or !$skirnir056.old055Control -or !$skirnir056.nativeGreaterGrantedExactlyOnce -or !$skirnir056.legacyPenaltyInert) { throw "01456 complete native policy coverage required." }

$psychic057Path=Join-Path $workspaceRoot ("work/prototype-build/01457-required-"+[guid]::NewGuid().ToString("N")+".json")
& python (Join-Path $projectRoot "tests/Test-PsychicArchetypes01457.py") $assemblyPath $psychic057Path
if ($LASTEXITCODE -ne 0) { throw "01457 native Psychic archetype contracts failed." }
$psychic057=Get-Content -LiteralPath $psychic057Path -Raw | ConvertFrom-Json
if (!$psychic057.passed -or $psychic057.rows.Count -ne 3 -or $psychic057.ownedBlueprints.Count -ne 214 -or !$psychic057.allNamedNativePaths) { throw "01457 complete native effects, rollback and ownership coverage required." }

$psychic058Path=Join-Path $workspaceRoot ("work/prototype-build/01458-required-"+[guid]::NewGuid().ToString("N")+".json")
& python (Join-Path $projectRoot "tests/Test-PsychicCompletion01458.py") $assemblyPath $psychic058Path
if ($LASTEXITCODE -ne 0) { throw "01458 native Psychic completion contracts failed." }
$psychic058=Get-Content -LiteralPath $psychic058Path -Raw | ConvertFrom-Json
if (!$psychic058.passed -or $psychic058.rows.Count -ne 5 -or $psychic058.controls.Count -ne 2 -or $psychic058.ownedBlueprints.Count -ne 257 -or !$psychic058.allNamedNativePaths -or !$psychic058.exactOld057PolicyControl -or !$psychic058.warmedNativeCacheDraftRejected) { throw "01458 complete native paths and actual historical/draft controls required." }

$psychic059Path=Join-Path $workspaceRoot ("work/prototype-build/01459-required-"+[guid]::NewGuid().ToString("N")+".json")
& python (Join-Path $projectRoot "tests/Test-PsychicFeedback01459.py") $assemblyPath $psychic059Path
if ($LASTEXITCODE -ne 0) { throw "01459 actual empty-link registration, actionbar and melee reach contracts failed." }
$psychic059=Get-Content -LiteralPath $psychic059Path -Raw | ConvertFrom-Json
if (!$psychic059.passed -or $psychic059.rows.Count -ne 3 -or $psychic059.controls.Count -ne 1 -or !$psychic059.actualOld058FailureReproduced -or !$psychic059.allThreeNamedNativeGroups) { throw "01459 complete native paths and actual058 control required." }

$psychic060Path=Join-Path $workspaceRoot ("work/prototype-build/01460-required-"+[guid]::NewGuid().ToString("N")+".json")
& python (Join-Path $projectRoot "tests/Test-PsychicFeedback01460.py") $assemblyPath $psychic060Path
if ($LASTEXITCODE -ne 0) { throw "01460 actual native psychic feedback/QL paths and delivered059 controls failed." }
$psychic060=Get-Content -LiteralPath $psychic060Path -Raw | ConvertFrom-Json
if (!$psychic060.passed -or $psychic060.rows.Count -ne 5 -or $psychic060.controls.Count -ne 4 -or $psychic060.newBlueprints.Count -ne 4 -or !$psychic060.allFiveNamedNativeGroups -or !$psychic060.allFourActual059ControlsPerRuntime) { throw "Retained01460 five groups/four real059 controls and exact four live identities required under061 retirement contract." }

$psychic061Path=Join-Path $workspaceRoot ("work/prototype-build/01461-required-"+[guid]::NewGuid().ToString("N")+".json")
& python (Join-Path $projectRoot "tests/Test-PsychicFeedback01461.py") $assemblyPath $psychic061Path
if ($LASTEXITCODE -ne 0) { throw "01461 eight actual native groups and three actual060 controls failed." }
$psychic061=Get-Content -LiteralPath $psychic061Path -Raw | ConvertFrom-Json
if (!$psychic061.passed -or $psychic061.rows.Count -ne 8 -or $psychic061.controls.Count -ne 3 -or !$psychic061.allEightNamedNativeGroups -or !$psychic061.allThreeActual060ControlsPerRuntime) { throw "01461 exact eight named native groups and three real060 controls required." }

$psychic062Path=Join-Path $workspaceRoot ("work/prototype-build/01462-required-"+[guid]::NewGuid().ToString("N")+".json")
& python (Join-Path $projectRoot "tests/Test-PsychicFeedback01462.py") $assemblyPath $psychic062Path
if ($LASTEXITCODE -ne 0) { throw "01462 three native groups/two actual061 controlled rejection paths failed." }
$psychic062=Get-Content -LiteralPath $psychic062Path -Raw | ConvertFrom-Json
if (!$psychic062.passed -or $psychic062.rows.Count -ne 3 -or $psychic062.controls.Count -ne 2 -or !$psychic062.allThreeNamedGroups -or !$psychic062.allTwoActual061ControlsPerRuntime) { throw "01462 exact native FX/pairs/isolation and actual061 controls required." }

$urban063Path=Join-Path $workspaceRoot ("work/prototype-build/01463-required-"+[guid]::NewGuid().ToString("N")+".json")
& python (Join-Path $projectRoot "tests/Test-UrbanBloodrager01463.py") $assemblyPath $urban063Path
if ($LASTEXITCODE -ne 0) { throw "01463 Urban Bloodrager native paths failed." }
$urban063=Get-Content -LiteralPath $urban063Path -Raw | ConvertFrom-Json
if (!$urban063.passed -or $urban063.rows.Count -ne 1 -or $urban063.controls.Count -ne 1 -or $urban063.additionalMigrationItems -ne 1) { throw "01463 exact Urban Bloodrager native graph required." }

if (!$magus055.passed -or !$magus055.allRequestedNativePaths -or $magus055.rows.Count -ne 8 -or $magus055.currentCasesPerRuntime -ne 4 -or $magus055.old054NegativeControlsPerRuntime -ne 3 -or $magus055.badDraftSourceKeysControlsPerRuntime -ne 1 -or $magus055.newMigrationItems -ne 0 -or $magus055.shieldACProductionUnchanged -or !$magus055.shieldACPolicy056 -or !$magus055.noSourceOwnershipOrQualificationBypass) { throw "01455 complete feedback contracts and exact old054 controls required before packaging." }


# Package language files are fixed inputs. Runtime English fallback supplies new01445 text.

# Build outside the bot-watched delivery directory. Publishing is a separate final
# step after the source ZIP, post-build regressions and certificate are complete.
# Preserve native-loader-required EMPTY directories and never package game DLLs.
$builtVersion = [Reflection.AssemblyName]::GetAssemblyName($assemblyPath).Version
if ([Version]$manifest.Version -ne [Version]::new($builtVersion.Major, $builtVersion.Minor, $builtVersion.Build)) {
    throw 'Manifest version differs from the built DLL; refusing to write any delivery archive.'
}
$stagingDirectory = Join-Path $workspaceRoot ("work/release-staging/$($manifest.Version)/" + [guid]::NewGuid().ToString('N'))
$null = New-Item -ItemType Directory -Path $stagingDirectory
$archivePath = Join-Path $stagingDirectory ("CallOfTheWildWrathPrototype-$($manifest.Version)-experimental.zip")
$prefix = 'CallOfTheWildWrathPrototype/'
Add-Type -AssemblyName System.IO.Compression
Add-Type -AssemblyName System.IO.Compression.FileSystem
$stream = [IO.File]::Open($archivePath, [IO.FileMode]::CreateNew)
$archive = [IO.Compression.ZipArchive]::new($stream, [IO.Compression.ZipArchiveMode]::Create)
try {
    foreach ($directory in @('Assemblies/', 'Bundles/', 'Blueprints/', 'Localization/')) {
        $null = $archive.CreateEntry($prefix + $directory)
    }
    $files = @{
        '0.14.43启动注册修复与职业变体测试说明.txt' = Join-Path $projectRoot '0.14.43启动注册修复与职业变体测试说明.txt'
        '0.14.42都市歌者修复测试说明.txt' = Join-Path $projectRoot '0.14.42都市歌者修复测试说明.txt'
        '0.14.43属性图标与乡野女巫测试说明.txt' = Join-Path $projectRoot '0.14.43属性图标与乡野女巫测试说明.txt'
        '0.14.44三项职业变体测试说明.txt' = Join-Path $projectRoot '0.14.44三项职业变体测试说明.txt'
        '0.14.45五项职业变体测试说明.txt' = Join-Path $projectRoot '0.14.45五项职业变体测试说明.txt'
        '0.14.46血脉与Admixture测试说明.txt' = Join-Path $projectRoot '0.14.46血脉与Admixture测试说明.txt'
        '0.14.47施法修复与五项CotW内容测试说明.txt' = Join-Path $projectRoot '0.14.47施法修复与五项CotW内容测试说明.txt'
        '0.14.48目标与血脉及视觉反馈修复测试说明.txt' = Join-Path $projectRoot '0.14.48目标与血脉及视觉反馈修复测试说明.txt'
        '0.14.49血脉兼容与重复能力移除测试说明.txt' = Join-Path $projectRoot '0.14.49血脉兼容与重复能力移除测试说明.txt'
        '0.14.50植物目标与血脉选项修复测试说明.txt' = Join-Path $projectRoot '0.14.50植物目标与血脉选项修复测试说明.txt'
        '0.14.51血脉入口与能力交换修复测试说明.txt' = Join-Path $projectRoot '0.14.51血脉入口与能力交换修复测试说明.txt'
        '0.14.52术士血脉实际菜单修复测试说明.txt' = Join-Path $projectRoot '0.14.52术士血脉实际菜单修复测试说明.txt'
        '0.14.53血脉变异启动兼容修复测试说明.txt' = Join-Path $projectRoot '0.14.53血脉变异启动兼容修复测试说明.txt'
        '0.14.54心刃与盾法师及鞭飓风测试说明.txt' = Join-Path $projectRoot '0.14.54心刃与盾法师及鞭飓风测试说明.txt'
        '0.14.55心刃与盾法师反馈修复测试说明.txt' = Join-Path $projectRoot '0.14.55心刃与盾法师反馈修复测试说明.txt'
        '0.14.56盾法师施法战斗调整测试说明.txt' = Join-Path $projectRoot '0.14.56盾法师施法战斗调整测试说明.txt'
        '0.14.57两项心灵变体测试说明.txt' = Join-Path $projectRoot '0.14.57两项心灵变体测试说明.txt'
        '0.14.63UrbanBloodrager-test-guide.txt' = Join-Path $projectRoot '0.14.63UrbanBloodrager-test-guide.txt'
        '0.14.62PsychicFeedback-test-guide.txt' = Join-Path $projectRoot '0.14.62PsychicFeedback-test-guide.txt'
        '0.14.61异能者菜单与猎人变体测试说明.txt' = Join-Path $projectRoot '0.14.61异能者菜单与猎人变体测试说明.txt'
        '0.14.60异能者反馈修复与QL文本测试说明.txt' = Join-Path $projectRoot '0.14.60异能者反馈修复与QL文本测试说明.txt'
        'Localization-QL使用说明.txt' = Join-Path $projectRoot 'Localization-QL使用说明.txt'
        'Localization-QL/CallOfTheWildWrathPrototypeLocalized.json' = Join-Path $projectRoot 'package/Localization-QL/CallOfTheWildWrathPrototypeLocalized.json'
        '0.14.59异能者登记与次数触及修复测试说明.txt' = Join-Path $projectRoot '0.14.59异能者登记与次数触及修复测试说明.txt'
        'LICENSE-LongArm-TTT.txt' = Join-Path $projectRoot 'assets/ElongatedFingers-LICENSE.txt'
        '0.14.58异能者本体补齐与突变修复测试说明.txt' = Join-Path $projectRoot '0.14.58异能者本体补齐与突变修复测试说明.txt'
        '0.14.42存档加载紧急修复测试说明.txt' = Join-Path $projectRoot '0.14.42存档加载紧急修复测试说明.txt'
        '0.14.41战鼓师测试说明.txt' = Join-Path $projectRoot '0.14.41战鼓师测试说明.txt'
        '0.14.40伤害减免与UMM兼容修复测试说明.txt' = Join-Path $projectRoot '0.14.40伤害减免与UMM兼容修复测试说明.txt'
        'Assemblies/CallOfTheWild.Wrath.Prototype.dll' = $assemblyPath
        'Modules/Medium/MediumClass.dll' = Join-Path $mediumStage 'MediumClass.dll'
        'Modules/WayOfTheShield/Way of the shield.dll' = $wayAssembly
        'Modules/WayOfTheShield/NOTICE-WayOfTheShield.txt' = Join-Path $wayRoot 'NOTICE-WayOfTheShield.txt'
        'Modules/WayOfTheShield/upstream-provenance.json' = Join-Path $wayRoot 'upstream-provenance.json'
        '0.14.9盾牌整合测试说明.txt' = Join-Path $projectRoot '0.14.9盾牌整合测试说明.txt'
        '0.14.9盾牌反馈修复测试说明.txt' = Join-Path $projectRoot '0.14.9盾牌反馈修复测试说明.txt'
        '0.14.10盾牌回归与野性变身专长测试说明.txt' = Join-Path $projectRoot '0.14.10盾牌回归与野性变身专长测试说明.txt'
        '0.14.11变形修复与协作专长测试说明.txt' = Join-Path $projectRoot '0.14.11变形修复与协作专长测试说明.txt'
        '0.14.12附魔与灵体协作修复测试说明.txt' = Join-Path $projectRoot '0.14.12附魔与灵体协作修复测试说明.txt'
        '0.14.13徒手灵体修复与新专长测试说明.txt' = Join-Path $projectRoot '0.14.13徒手灵体修复与新专长测试说明.txt'
        '0.14.14共享施法与诱敌修复测试说明.txt' = Join-Path $projectRoot '0.14.14共享施法与诱敌修复测试说明.txt'
        '0.14.15移动性能与远程反应修复测试说明.txt' = Join-Path $projectRoot '0.14.15移动性能与远程反应修复测试说明.txt'
        '0.14.16新专长第1批测试说明.txt' = Join-Path $projectRoot '0.14.16新专长第1批测试说明.txt'
        '0.14.17新专长第2批测试说明.txt' = Join-Path $projectRoot '0.14.17新专长第2批测试说明.txt'
        '0.14.18升级与菜单紧急修复测试说明.txt' = Join-Path $projectRoot '0.14.18升级与菜单紧急修复测试说明.txt'
        '0.14.19反馈修复与新专长第3批测试说明.txt' = Join-Path $projectRoot '0.14.19反馈修复与新专长第3批测试说明.txt'
        '0.14.20兼容与参数显示修复测试说明.txt' = Join-Path $projectRoot '0.14.20兼容与参数显示修复测试说明.txt'
        '0.14.21法术表修复与单专长测试说明.txt' = Join-Path $projectRoot '0.14.21法术表修复与单专长测试说明.txt'
        '0.14.22启动兼容紧急修复测试说明.txt' = Join-Path $projectRoot '0.14.22启动兼容紧急修复测试说明.txt'
        '0.14.23法术表结构兼容修复测试说明.txt' = Join-Path $projectRoot '0.14.23法术表结构兼容修复测试说明.txt'
        '0.14.24单专长测试说明.txt' = Join-Path $projectRoot '0.14.24单专长测试说明.txt'
        '0.14.25进阶杀戮者天赋测试说明.txt' = Join-Path $projectRoot '0.14.25进阶杀戮者天赋测试说明.txt'
        '0.14.26前置增强与五项移植测试说明.txt' = Join-Path $projectRoot '0.14.26前置增强与五项移植测试说明.txt'
        '0.14.27嚎叫修复与五项真气能力测试说明.txt' = Join-Path $projectRoot '0.14.27嚎叫修复与五项真气能力测试说明.txt'
        '0.14.28嚎叫反馈与五项职业选项测试说明.txt' = Join-Path $projectRoot '0.14.28嚎叫反馈与五项职业选项测试说明.txt'
        '0.14.29嚎叫清理与五项移植测试说明.txt' = Join-Path $projectRoot '0.14.29嚎叫清理与五项移植测试说明.txt'
        '0.14.30四项生命周期修正测试说明.txt' = Join-Path $projectRoot '0.14.30四项生命周期修正测试说明.txt'
        '0.14.31五项移植测试说明.txt' = Join-Path $projectRoot '0.14.31五项移植测试说明.txt'
        '0.14.32注册与资源修复测试说明.txt' = Join-Path $projectRoot '0.14.32注册与资源修复测试说明.txt'
        '0.14.33五项移植测试说明.txt' = Join-Path $projectRoot '0.14.33五项移植测试说明.txt'
        '0.14.34五项移植测试说明.txt' = Join-Path $projectRoot '0.14.34五项移植测试说明.txt'
        '0.14.35五项移植测试说明.txt' = Join-Path $projectRoot '0.14.35五项移植测试说明.txt'
        '0.14.36选择入口与杰作可知修复测试说明.txt' = Join-Path $projectRoot '0.14.36选择入口与杰作可知修复测试说明.txt'
        '0.14.37两项职业选项移植测试说明.txt' = Join-Path $projectRoot '0.14.37两项职业选项移植测试说明.txt'
        '0.14.38时间凝滞与奥术发现修复测试说明.txt' = Join-Path $projectRoot '0.14.38时间凝滞与奥术发现修复测试说明.txt'
        '0.14.39伤害减免防御之力测试说明.txt' = Join-Path $projectRoot '0.14.39伤害减免防御之力测试说明.txt'
        '0.14.21-spell-links.json' = Join-Path $projectRoot '0.14.21-spell-links.json'
        'Modules/Medium/mediumclass_assets' = Join-Path $mediumStage 'mediumclass_assets'
        'Modules/Medium/LICENSE-CotW.txt' = Join-Path $mediumStage 'LICENSE-CotW.txt'
        '0.8.0合并测试说明.txt' = Join-Path $projectRoot '0.8.0合并测试说明.txt'
        '0.9.0测试说明.txt' = Join-Path $projectRoot '0.9.0测试说明.txt'
        '0.9.1修复与诊断说明.txt' = Join-Path $projectRoot '0.9.1修复与诊断说明.txt'
        '0.9.2测试说明.txt' = Join-Path $projectRoot '0.9.2测试说明.txt'
        '0.13.0测试说明.txt' = Join-Path $projectRoot '0.13.0测试说明.txt'
        '0.13.1测试说明.txt' = Join-Path $projectRoot '0.13.1测试说明.txt'
        '0.13.2测试说明.txt' = Join-Path $projectRoot '0.13.2测试说明.txt'
        '0.13.3测试说明.txt' = Join-Path $projectRoot '0.13.3测试说明.txt'
        '0.13.4测试说明.txt' = Join-Path $projectRoot '0.13.4测试说明.txt'
        '0.13.5测试说明.txt' = Join-Path $projectRoot '0.13.5测试说明.txt'
        '0.13.11测试说明.txt' = Join-Path $projectRoot '0.13.11测试说明.txt'
        '0.13.12测试说明.txt' = Join-Path $projectRoot '0.13.12测试说明.txt'
        '0.13.13测试说明.txt' = Join-Path $projectRoot '0.13.13测试说明.txt'
        '0.13.14测试说明.txt' = Join-Path $projectRoot '0.13.14测试说明.txt'
        '0.13.15测试说明.txt' = Join-Path $projectRoot '0.13.15测试说明.txt'
        '0.13.16测试说明.txt' = Join-Path $projectRoot '0.13.16测试说明.txt'
        '0.13.17汇总测试说明.txt' = Join-Path $projectRoot '0.13.17汇总测试说明.txt'
        '0.13.18测试说明.txt' = Join-Path $projectRoot '0.13.18测试说明.txt'
        '0.13.19测试说明.txt' = Join-Path $projectRoot '0.13.19测试说明.txt'
        '0.13.22测试说明.txt' = Join-Path $projectRoot '0.13.22测试说明.txt'
        '0.13.23测试说明.txt' = Join-Path $projectRoot '0.13.23测试说明.txt'
        '0.13.21-0.13.24测试说明.txt' = Join-Path $projectRoot '0.13.21-0.13.24测试说明.txt'
        '0.13.25修复测试说明.txt' = Join-Path $projectRoot '0.13.25修复测试说明.txt'
        '0.13.26修复测试说明.txt' = Join-Path $projectRoot '0.13.26修复测试说明.txt'
        '0.14.6专长反馈修复测试说明.txt' = Join-Path $projectRoot '0.14.6专长反馈修复测试说明.txt'
        '0.14.8属性选择与召唤武器修复测试说明.txt' = Join-Path $projectRoot '0.14.8属性选择与召唤武器修复测试说明.txt'
        '0.14.7战斗专长列表修复测试说明.txt' = Join-Path $projectRoot '0.14.7战斗专长列表修复测试说明.txt'
        '0.14.5信仰武器专长测试说明.txt' = Join-Path $projectRoot '0.14.5信仰武器专长测试说明.txt'
        '0.14.3-0.14.5汇总测试说明.txt' = Join-Path $projectRoot '0.14.3-0.14.5汇总测试说明.txt'
        '0.14.4施法资源专长测试说明.txt' = Join-Path $projectRoot '0.14.4施法资源专长测试说明.txt'
        '0.14.3训练与奥秘测试说明.txt' = Join-Path $projectRoot '0.14.3训练与奥秘测试说明.txt'
        '0.14.2专长反馈修复测试说明.txt' = Join-Path $projectRoot '0.14.2专长反馈修复测试说明.txt'
        '0.14.1专长与目标动作修复测试说明.txt' = Join-Path $projectRoot '0.14.1专长与目标动作修复测试说明.txt'
        '0.14.0专长与蓝色灵体测试说明.txt' = Join-Path $projectRoot '0.14.0专长与蓝色灵体测试说明.txt'
        '0.13.35修复测试说明.txt' = Join-Path $projectRoot '0.13.35修复测试说明.txt'
        '0.13.34修复测试说明.txt' = Join-Path $projectRoot '0.13.34修复测试说明.txt'
        '0.13.33修复测试说明.txt' = Join-Path $projectRoot '0.13.33修复测试说明.txt'
        '0.13.33法术迁移定稿与后续路线.txt' = Join-Path $projectRoot '0.13.33法术迁移定稿与后续路线.txt'
        '0.13.32修复测试说明.txt' = Join-Path $projectRoot '0.13.32修复测试说明.txt'
        '0.13.32剩余根法术清单.txt' = Join-Path $projectRoot '0.13.32剩余根法术清单.txt'
        '0.13.28测试说明.txt' = Join-Path $projectRoot '0.13.28测试说明.txt'
        '0.13.28-0.13.31汇总测试说明.txt' = Join-Path $projectRoot '0.13.28-0.13.31汇总测试说明.txt'
        '0.13.27修复测试说明.txt' = Join-Path $projectRoot '0.13.27修复测试说明.txt'
        '0.13.21测试说明.txt' = Join-Path $projectRoot '0.13.21测试说明.txt'
        '0.13.18-0.13.20测试说明.txt' = Join-Path $projectRoot '0.13.18-0.13.20测试说明.txt'
        '0.12.2测试说明.txt' = Join-Path $projectRoot '0.12.2测试说明.txt'
        '0.12.1测试说明.txt' = Join-Path $projectRoot '0.12.1测试说明.txt'
        '0.12.0测试说明.txt' = Join-Path $projectRoot '0.12.0测试说明.txt'
        '0.11.1测试说明.txt' = Join-Path $projectRoot '0.11.1测试说明.txt'
        '0.11.0测试说明.txt' = Join-Path $projectRoot '0.11.0测试说明.txt'
        '0.10.1测试说明.txt' = Join-Path $projectRoot '0.10.1测试说明.txt'
        '0.10.0测试说明.txt' = Join-Path $projectRoot '0.10.0测试说明.txt'
        '0.9.5修复测试说明.txt' = Join-Path $projectRoot '0.9.5修复测试说明.txt'
        '0.9.4修复与诊断说明.txt' = Join-Path $projectRoot '0.9.4修复与诊断说明.txt'
        '0.9.3测试说明.txt' = Join-Path $projectRoot '0.9.3测试说明.txt'
        'LICENSE-CotW.txt' = Join-Path $projectRoot 'LICENSE-CotW.txt'
        'README.zh-CN.md' = Join-Path $projectRoot 'README.zh-CN.md'
        '测试者必读.txt' = Join-Path $projectRoot '测试者必读.txt'
        '百宝袋快速测试.txt' = Join-Path $projectRoot '百宝袋快速测试.txt'
        '0.3.0测试说明.txt' = Join-Path $projectRoot '0.3.0测试说明.txt'
        '0.3.1测试说明.txt' = Join-Path $projectRoot '0.3.1测试说明.txt'
        '0.3.3测试说明.txt' = Join-Path $projectRoot '0.3.3测试说明.txt'
        '0.3.4测试说明.txt' = Join-Path $projectRoot '0.3.4测试说明.txt'
        '0.4.0测试说明.txt' = Join-Path $projectRoot '0.4.0测试说明.txt'
        '0.4.1测试说明.txt' = Join-Path $projectRoot '0.4.1测试说明.txt'
        '0.5.0测试说明.txt' = Join-Path $projectRoot '0.5.0测试说明.txt'
        '0.5.1测试说明.txt' = Join-Path $projectRoot '0.5.1测试说明.txt'
        '0.6.0测试说明.txt' = Join-Path $projectRoot '0.6.0测试说明.txt'
        '0.6.1启动修复测试说明.txt' = Join-Path $projectRoot '0.6.1启动修复测试说明.txt'
        '0.6.4修复测试说明.txt' = Join-Path $projectRoot '0.6.4修复测试说明.txt'
        '0.6.5修复测试说明.txt' = Join-Path $projectRoot '0.6.5修复测试说明.txt'
        '0.7.0测试说明.txt' = Join-Path $projectRoot '0.7.0测试说明.txt'
        '0.7.1修复测试说明.txt' = Join-Path $projectRoot '0.7.1修复测试说明.txt'
        '0.7.2合并测试说明.txt' = Join-Path $projectRoot '0.7.2合并测试说明.txt'
        '0.7.0-spell-links.json' = Join-Path $projectRoot '0.7.0-spell-links.json'
        '0.6.3修复测试说明.txt' = Join-Path $projectRoot '0.6.3修复测试说明.txt'
        '0.6.2修复测试说明.txt' = Join-Path $projectRoot '0.6.2修复测试说明.txt'
        'OwlcatModificationManifest.json' = Join-Path $projectRoot 'package/OwlcatModificationManifest.json'
        'OwlcatModificationSettings.json' = Join-Path $projectRoot 'package/OwlcatModificationSettings.json'
        'Localization/enGB.json' = Join-Path $projectRoot 'package/Localization/enGB.json'
        'Localization/zhCN.json' = Join-Path $projectRoot 'package/Localization/zhCN.json'
    }
    foreach ($entry in $files.GetEnumerator() | Sort-Object Key) {
        $null = [IO.Compression.ZipFileExtensions]::CreateEntryFromFile($archive, $entry.Value, $prefix + $entry.Key)
    }
} finally {
    $archive.Dispose()
    $stream.Dispose()
}
& (Join-Path $projectRoot 'tests/Verify-Package.ps1') -ArchivePath $archivePath
& (Join-Path $projectRoot 'tests/Test-PackageRejections.ps1') -ArchivePath $archivePath
$previousRecordPath = Join-Path $workspaceRoot 'outputs/验证记录.json'
$recordPath = Join-Path $stagingDirectory '验证记录.json'
$previous = if (Test-Path -LiteralPath $previousRecordPath) { Get-Content -LiteralPath $previousRecordPath -Raw | ConvertFrom-Json -AsHashtable } else { @{} }
$record = [ordered]@{
    checkedAt = (Get-Date -Format o)
    deliveryReady = $false
    stagingDirectory = $stagingDirectory
    upstream = $previous.upstream
    gameAssemblySha256 = (Get-FileHash -LiteralPath (Join-Path $managed 'Assembly-CSharp.dll') -Algorithm SHA256).Hash
    harmonyCompileReference = $harmonyBaseline.Version.ToString()
    harmonyRuntimeTests = $harmonyResults
    harmonyCompileReferencePin = $harmonyPinResults
    harmonyStartupRuntimeRegressions = $startupRegressionResults
    requiredGameHarmony = [ordered]@{
        version = $harmonyBaseline.Version.ToString()
        path = $gameHarmonyPath
        sha256 = (Get-FileHash -LiteralPath $gameHarmonyPath).Hash
        testManagedPath = [IO.Path]::GetFullPath($managed)
        mandatoryRegression = $true
    }
    modMenuReference = [ordered]@{
        source = 'https://github.com/CasDragon/ModMenu/releases/tag/2.0.14'
        assemblyVersion = ([Reflection.AssemblyName]::GetAssemblyName([IO.Path]::GetFullPath($ModMenuReferencePath))).Version.ToString()
        sha256 = (Get-FileHash -LiteralPath $ModMenuReferencePath -Algorithm SHA256).Hash
        ummTestReferenceSha256 = (Get-FileHash -LiteralPath $UnityModManagerReferencePath -Algorithm SHA256).Hash
    }
    verification = [ordered]@{
        release01440StaticChecks = $release01440Static
        compilation = 'PASS'
        mediumMandatoryRegressionSuites = $mediumRegressionResults.passedSuites
        mediumMandatoryRegressionEvidence = Join-Path $mediumRegressionDirectory 'results.json'
        startupRuntimeCasesPassed = $startupRegressionResults.passedCases
        startupRuntimeRejectionsPassed = $startupRegressionResults.expectedRejections
        blueprintCorePatchClassesInstalled = $startupRegressionResults.blueprintCorePatchesInstalled
        independentRuleAssertionsPassed = 25
        managedBlueprintScenariosPassed = $blueprintResults.scenariosPassed
        managedBlueprintAssertionsPassed = $blueprintResults.assertionsPassed
        managedResourceScenariosPassed = $resourceResults.scenariosPassed
        wayOfTheShield = [ordered]@{upstreamVersion='4.0.13';sourceCommit='5c0490f058f2163e3edfc9b13f007e1e9534f87c';assemblySha256=(Get-FileHash -LiteralPath $wayAssembly).Hash;regressionResults=$wayResults;nativeInputSha256=(Get-FileHash -LiteralPath $wayNative).Hash}
        managedResourceAssertionsPassed = $resourceResults.assertionsPassed
        modMenuAdapterAssertionsPassed = $menuResults.assertionsPassed
        modMenuTestScope = $menuResults.scope
        modMenuInGameUITested = $false
        shieldedMageAssertionsPassed = $shieldResults.assertionsPassed
        shieldedMageTestScope = $shieldResults.scope
        shieldedMageInGameTested = $false
        performanceResourceAssertionsPassed = $performanceResults.assertionsPassed
        performanceResourceTestScope = $performanceResults.scope
        performanceResourceInGameTested = $false
        performanceProviderReferenceVersion = $performanceResults.providerAssemblyVersion
        performanceProviderReferenceSha256 = (Get-FileHash -LiteralPath $BrawlerReferencePath -Algorithm SHA256).Hash
        exemplarAssertionsPassed = $exemplarResults.assertionsPassed
        exemplarTestScope = $exemplarResults.scope
        exemplarInGameTested = $false
        rageAssertionsPassed = $rageResults.assertionsPassed
        rageTestScope = $rageResults.scope
        rageNativeInputSha256 = (Get-FileHash -LiteralPath $rageNativePath -Algorithm SHA256).Hash
        rageInGameTested = $false
        repair033Assertions = $psychicResults.repair033Assertions
        repair034Assertions = $psychicResults.repair034Assertions
        repair035Assertions = $psychicResults.repair035Assertions
        feat0140Assertions = $psychicResults.feat0140Assertions
        repair0141Assertions = $psychicResults.repair0141Assertions
        devastatingStrikeAssertions = $psychicResults.devastatingStrikeAssertions
        devastatingFeedback0142Assertions = $psychicResults.devastatingFeedback0142Assertions
        feralGrace0142Assertions = $psychicResults.feralGrace0142Assertions
        feralGraceAssertions = $psychicResults.feralGrace0141Assertions
        psychicAssertionsPassed = $psychicResults.assertionsPassed
        feat01424Contracts = $psychicResults.feat01424Contracts
        talent01425Contracts = $psychicResults.talent01425Contracts
        prerequisites01426Contracts = $psychicResults.prerequisites01426Contracts
        migration01426Contracts = $psychicResults.migration01426Contracts
        howl01427Contracts = $psychicResults.howl01427Contracts
        ki01427Contracts = $psychicResults.ki01427Contracts
        howl01428NativeResolution = $psychicResults.howl01428NativeResolution
        howl01429CleanupContracts = $psychicResults.howl01429CleanupContracts
        migration01429Contracts = $psychicResults.migration01429Contracts
        spiritualGuardian01433Contracts = $psychicResults.spiritualGuardian01433Contracts
        kineticInvocation01433Contracts = $psychicResults.kineticInvocation01433Contracts
        kineticUtility01433Contracts = $psychicResults.kineticUtility01433Contracts
        superstitionRage01434Contracts = $psychicResults.superstitionRage01434Contracts
        antimagicRage01434Contracts = $psychicResults.antimagicRage01434Contracts
        migration01434CallerContracts = $psychicResults.migration01434CallerContracts
        wizardDiscoveries01435Contracts = $psychicResults.wizardDiscoveries01435Contracts
        bardMasterpieces01435Contracts = $psychicResults.bardMasterpieces01435Contracts
        wizardDiscoveryAccess01436Contracts = $psychicResults.wizardDiscoveryAccess01436Contracts
        bardMasterpieceAccess01436Contracts = $psychicResults.bardMasterpieceAccess01436Contracts
        repair01436CallerContracts = $psychicResults.repair01436CallerContracts
        timeStutter01437Contracts = $psychicResults.timeStutter01437Contracts
        invoker01437Contracts = $psychicResults.invoker01437Contracts
        migration01437CallerContracts = $psychicResults.migration01437CallerContracts
        discoveryAccess01438Contracts = $psychicResults.discoveryAccess01438Contracts
        timeStopLifecycle01438Contracts = $psychicResults.timeStopLifecycle01438Contracts
        timeStopTimers01438Contracts = $psychicResults.timeStopTimers01438Contracts
        timeStopSpell01438Contracts = $psychicResults.timeStopSpell01438Contracts
        timeStopSpellbook01438Contracts = $psychicResults.timeStopSpellbook01438Contracts
        repair01438CallerContracts = $psychicResults.repair01438CallerContracts
        repair01440Contracts = $psychicResults.repair01440Contracts
        timeStopDismiss01440Contracts = $psychicResults.timeStopDismiss01440Contracts
        registrationWarmup01443Contracts = $psychicResults.registrationWarmup01443Contracts
        archetypeWarmup01443Contracts = $psychicResults.archetypeWarmup01443Contracts
        sunsinger01443Contracts = $psychicResults.sunsinger01443Contracts
        urbanSkald01443Contracts = $psychicResults.urbanSkald01443Contracts
        urbanControlledRepair01442Contracts = $psychicResults.urbanControlledRepair01442Contracts
        hedgeWitch01443Contracts = $psychicResults.hedgeWitch01443Contracts
        urbanAllocationIcons01443Contracts = $psychicResults.urbanAllocationIcons01443Contracts
        terrorWeaver01444Contracts = $psychicResults.terrorWeaver01444Contracts
        primalSorcerer01444Contracts = $psychicResults.primalSorcerer01444Contracts
        rake01444Contracts = $psychicResults.rake01444Contracts
        archetypeCaller01444Contracts = $psychicResults.archetypeCaller01444Contracts
        repair01444Contracts = $psychicResults.repair01444Contracts
        registrationOrder01444Contracts = $psychicResults.registrationOrder01444Contracts
        startupRetry01444Contracts = $psychicResults.startupRetry01444Contracts
        shieldStartupRetry01444Contracts = $shieldRetry044
        shieldBlueprintResults01444Contracts = $shieldBlueprintResults044
        auraOfDoomCasting01444Contracts = $doomCasting044
        courtBardLevel901445Contracts = $psychicResults.courtBardLevel901445Contracts
        archetypes01445Contracts = $psychicResults.archetypes01445Contracts
        dustInvisibility01445Contracts = $psychicResults.dustInvisibility01445Contracts
        symbiosisStacking01445Contracts = $psychicResults.symbiosisStacking01445Contracts
        kiFeintTriggers01445Contracts = $psychicResults.kiFeintTriggers01445Contracts
        drillLatePool01445Contracts = $psychicResults.drillLatePool01445Contracts
        startupArchetypes01445Contracts = $psychicResults.startupArchetypes01445Contracts
        CourtBard01445Contracts = $psychicResults.CourtBard01445Contracts
        NatureBondedMagus01445Contracts = $psychicResults.NatureBondedMagus01445Contracts
        SageCounselor01445Contracts = $psychicResults.SageCounselor01445Contracts
        GraveWarden01445Contracts = $psychicResults.GraveWarden01445Contracts
        DrillSergeant01445Contracts = $psychicResults.DrillSergeant01445Contracts
        repair01446Contracts = $psychicResults.repair01446Contracts
        arcane01446Contracts = $psychicResults.arcane01446Contracts
        startupArcane01446Contracts = $psychicResults.startupArcane01446Contracts
        bloodlineMutations01446 = $psychicResults.bloodlineMutations01446
        admixture01446 = $psychicResults.admixture01446
        bloodlineCasting01446 = $psychicResults.bloodlineCasting01446
        admixtureCasting01446 = $psychicResults.admixtureCasting01446
        arcaneLifecycle01446 = $psychicResults.arcaneLifecycle01446
        repair01447Contracts = $psychicResults.repair01447Contracts
        bloodlineCompatibility01447Contracts = $psychicResults.bloodlineCompatibility01447Contracts
        spellBatch01447Contracts = $psychicResults.spellBatch01447Contracts
        startupSpellBatch01447Contracts = $psychicResults.startupSpellBatch01447Contracts
        naturesCommand01447Contracts = $psychicResults.naturesCommand01447Contracts
        infectiousCharms01447Contracts = $psychicResults.infectiousCharms01447Contracts
        Overseer01447Contracts = $psychicResults.Overseer01447Contracts
        SpellSpecialist01447Contracts = $psychicResults.SpellSpecialist01447Contracts
        MagaambyanTelepath01447Contracts = $psychicResults.MagaambyanTelepath01447Contracts
        Phantasm01447Contracts = $psychicResults.Phantasm01447Contracts
        infectiousBlueprints01447Contracts = $psychicResults.infectiousBlueprints01447Contracts
        feedback01448Contracts = $psychicResults.feedback01448Contracts
        bloodlineReference01448Contracts = $psychicResults.bloodlineReference01448Contracts
        natureTargeting01448Contracts = $psychicResults.natureTargeting01448Contracts
        visualFeedback01448Contracts = $psychicResults.visualFeedback01448Contracts
        mutationEntries01451 = [ordered]@{ passed=$true; result=$external01451Path; resultSHA256=(Get-FileHash -LiteralPath $external01451Path).Hash; rows=$external01451.rows; inputs=$external01451.inputs }
        actualMenus01452 = [ordered]@{ passed=$true; result=$external01452Path; resultSHA256=(Get-FileHash -LiteralPath $external01452Path).Hash; rows=$external01452.rows; inputs=$external01452.inputs }
        magusBatch01454 = [ordered]@{ passed=$true; result=$magus01454Path; resultSHA256=(Get-FileHash -LiteralPath $magus01454Path).Hash; contract=$magus01454 }
        magusFeedback01455 = [ordered]@{ passed=$true; result=$magus055Path; resultSHA256=(Get-FileHash -LiteralPath $magus055Path).Hash; contract=$magus055 }
        skirnir01456 = [ordered]@{ passed=$true; result=$skirnir056Path; resultSHA256=(Get-FileHash -LiteralPath $skirnir056Path).Hash; rows=$skirnir056.rows }
        psychicArchetypes01457 = [ordered]@{ passed=$true; result=$psychic057Path; resultSHA256=(Get-FileHash -LiteralPath $psychic057Path).Hash; rows=$psychic057.rows }
        urbanBloodrager01463 = [ordered]@{ passed=$true; result=$urban063Path; resultSHA256=(Get-FileHash -LiteralPath $urban063Path).Hash; rows=$urban063.rows; controls=$urban063.controls }
        psychicFeedback01462 = [ordered]@{ passed=$true; result=$psychic062Path; resultSHA256=(Get-FileHash -LiteralPath $psychic062Path).Hash; rows=$psychic062.rows; controls=$psychic062.controls }
        psychicFeedback01461 = [ordered]@{ passed=$true; result=$psychic061Path; resultSHA256=(Get-FileHash -LiteralPath $psychic061Path).Hash; rows=$psychic061.rows; controls=$psychic061.controls }
        psychicFeedback01460 = [ordered]@{ passed=$true; result=$psychic060Path; resultSHA256=(Get-FileHash -LiteralPath $psychic060Path).Hash; rows=$psychic060.rows; controls=$psychic060.controls; newBlueprints=$psychic060.newBlueprints }
        psychicFeedback01459 = [ordered]@{ passed=$true; result=$psychic059Path; resultSHA256=(Get-FileHash -LiteralPath $psychic059Path).Hash; rows=$psychic059.rows; controls=$psychic059.controls }
        psychicCompletion01458 = [ordered]@{ passed=$true; result=$psychic058Path; resultSHA256=(Get-FileHash -LiteralPath $psychic058Path).Hash; rows=$psychic058.rows; controls=$psychic058.controls; sceneDependency=$scene058 }
        actualECMScan01453 = [ordered]@{ passed=$true; result=$external01453Path; resultSHA256=(Get-FileHash -LiteralPath $external01453Path).Hash; rows=$external01453.rows; inputs=$external01453.inputs }
        externalBloodlines01450 = [ordered]@{ passed=$true; result=$external01450Path; resultSHA256=(Get-FileHash -LiteralPath $external01450Path).Hash; rows=$external01450.rows; inputs=$external01450.inputs }
        naturePreview01450Contracts = $psychicResults.naturePreview01450Contracts
        bloodlineComposition01450Contracts = $psychicResults.bloodlineComposition01450Contracts
        bloodlineRetirement01449Contracts = $psychicResults.bloodlineRetirement01449Contracts
        bloodlineIntegration01449Contracts = $psychicResults.bloodlineIntegration01449Contracts
        ecmMutations01449Contracts = $psychicResults.ecmMutations01449Contracts
        tomeClaws01449Contracts = $psychicResults.tomeClaws01449Contracts
        bloodlineFingerprint01449Contracts = $psychicResults.bloodlineFingerprint01449Contracts
        natureDiagnostics01449Contracts = $psychicResults.natureDiagnostics01449Contracts
        bloodlineLate01449 = [ordered]@{ passed=$true; result=$bloodlineLatePath; resultSHA256=(Get-FileHash -LiteralPath $bloodlineLatePath).Hash; contract=$bloodlineLate.bloodlineIntegration01449Contracts }
        bloodlineInputs01449 = [ordered]@{ native=$bloodlineNativePath; nativeSHA256=(Get-FileHash -LiteralPath $bloodlineNativePath).Hash; officialTome=$tomeOriginal; officialTomeSHA256=(Get-FileHash -LiteralPath $tomeOriginal).Hash; adapter=$tomeAdapter; adapterSHA256=(Get-FileHash -LiteralPath $tomeAdapter).Hash; host=$bloodlineHost; hostSHA256=(Get-FileHash -LiteralPath $bloodlineHost).Hash }


        rogueSwashbuckler01443Contracts = $psychicResults.rogueSwashbuckler01443Contracts
        migration01443CallerContracts = $psychicResults.migration01443CallerContracts
        saveLoadTimers01442Contracts = $psychicResults.saveLoadTimers01442Contracts
        warDrummer01441Contracts = $psychicResults.warDrummer01441Contracts
        increasedDamageReduction01439Contracts = $psychicResults.increasedDamageReduction01439Contracts
        registrationFeatKi01432Contracts = $psychicResults.registrationFeatKi01432Contracts
        registrationRemoval01432Contracts = $psychicResults.registrationRemoval01432Contracts
        anger01431Contracts = $psychicResults.anger01431Contracts
        rage01431Contracts = $psychicResults.rage01431Contracts
        wizard01431Contracts = $psychicResults.wizard01431Contracts
        overlap01429Contracts = $psychicResults.overlap01429Contracts
        hoverOwnership01429Contracts = $psychicResults.hoverOwnership01429Contracts
        enhancementLifecycle01430Contracts = $psychicResults.enhancementLifecycle01430Contracts
        flameJetLifecycle01430Contracts = $psychicResults.flameJetLifecycle01430Contracts
        inquisitions01428Contracts = $psychicResults.inquisitions01428Contracts
        wizardSubschools01428Contracts = $psychicResults.wizardSubschools01428Contracts
        purifyingFlames01428Native = $psychicResults.purifyingFlames01428Native
        psychicTestScope = $psychicResults.scope
        loadPackTocCacheRegression = $cacheStartupResults
        mediumSpellListContract = $mediumListResults
        psychicNativeInputSha256 = (Get-FileHash -LiteralPath $psychicNativePath -Algorithm SHA256).Hash
        offlineSelectionContracts = Get-Content -LiteralPath $selectionContractPath -Raw | ConvertFrom-Json
        psychicExtensionNativeInputSha256 = (Get-FileHash -LiteralPath $psychicExpansionNativePath -Algorithm SHA256).Hash
        psychicExtensionAssertionsPassed = $psychicResults.extensionAssertions
        psychicExtensionAssets = $psychicResults.assets
        psychicExclusiveSpellCount = $psychicResults.exclusiveSpellCount
        psychicPhrenicAssets = $psychicResults.phrenicAssets
        psychic090Assertions = $psychicResults.psychic090Assertions
        psychic092Assertions = $psychicResults.psychic092Assertions
        psychic093Assertions = $psychicResults.psychic093Assertions
        psychic095Assertions = $psychicResults.psychic095Assertions
        faithAssertions = $psychicResults.faithAssertions
        pageantryAssertions = $psychicResults.pageantryAssertions
        undeadSpellAssertions = $psychicResults.undeadSpellAssertions
        wardingSpellAssertions = $psychicResults.wardingSpellAssertions
        bloodSpellAssertions = $psychicResults.bloodSpellAssertions
        globeSpellAssertions = $psychicResults.globeSpellAssertions
        compulsionSpellAssertions = $psychicResults.compulsionSpellAssertions
        mawCleanupAssertions = $psychicResults.mawCleanupAssertions
        fluidFormAssertions = $psychicResults.fluidFormAssertions
        channelVigorAssertions = $psychicResults.channelVigorAssertions
        bladeTutorAssertions = $psychicResults.bladeTutorAssertions
        haltControlAssertions = $psychicResults.haltControlAssertions
        secondWindAssertions = $psychicResults.secondWindAssertions
        comfortSpellAssertions = $psychicResults.comfortSpellAssertions
        insightSpellAssertions = $psychicResults.insightSpellAssertions
        afflictionAssertions = $psychicResults.afflictionAssertions
        phrenicMenuAssertions = $psychicResults.psychicMenuAssertions
        forceSpellAssertions = $psychicResults.forceSpellAssertions
        crushSpellAssertions = $psychicResults.crushSpellAssertions
        crushNativeCapAssertions = $psychicResults.crushNativeCapAssertions
        psychicCompleteDiscipline = 'Ferocity/Faith/Pageantry retained; Pain/Psychedelia/Abomination completed058 with named native contracts and separate gameplay feedback.'
        psychicInGameTested = $false
        nativePackageStructure = 'PASS'
        invalidPackagesRejected = 5
        runtimeScope = 'Agent-run standalone .NET Framework processes using real game managed types, seeded blueprints and a minimal resource component fixture; no Unity/game startup. Tester feedback is recorded separately by version.'
        gameLaunchTested = $false
        combatTested = $false
        saveReloadTested = $false
        modCoexistenceTested = $false
        installed = $false
    }
    package = [ordered]@{
        file = [IO.Path]::GetFileName($archivePath)
        sha256 = (Get-FileHash -LiteralPath $archivePath -Algorithm SHA256).Hash
        assemblyVersion = ([Reflection.AssemblyName]::GetAssemblyName($assemblyPath)).Version.ToString()
        assemblySha256 = (Get-FileHash -LiteralPath $assemblyPath -Algorithm SHA256).Hash
    }
    sourceFiles = @(Get-ChildItem -LiteralPath $projectRoot -Recurse -File | Where-Object { $_.FullName -notmatch '[\\/](bin|obj|artifacts|lib)[\\/]' -and $_.Name -ne 'MediumClass.local.props' } | Sort-Object FullName | ForEach-Object {
        [ordered]@{ path = [IO.Path]::GetRelativePath($projectRoot, $_.FullName); sha256 = (Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash }
    })
    testerReports = @(if (Test-Path -LiteralPath (Join-Path $workspaceRoot 'outputs/测试者反馈记录.json')) {
        (Get-Content -LiteralPath (Join-Path $workspaceRoot 'outputs/测试者反馈记录.json') -Raw | ConvertFrom-Json).reports
    })
    scope = '0.14.63: Urban Bloodrager. All historical contracts and verified Feral Hunter/Mimic/Physical Mutation behavior retained; two dependency-blocked candidates deferred.'
}
$record | ConvertTo-Json -Depth 60 | Set-Content -LiteralPath $recordPath -Encoding utf8
Write-Output "Staged experimental package (not published): $archivePath"
Write-Output 'Complete source archive, post-build regressions and final certificate in staging, then use Publish-Release.ps1. No ZIP or latest verification record was written to the delivery directory. Not installed or game-tested.'

#Requires -Version 7.4
param(
    [Parameter(Mandatory = $true)][string]$WrathPath,
    [string]$HarmonyReferencePath,
    [string[]]$AdditionalHarmonyPaths = @(),
    [string]$TestManagedPath,
    [string]$BrawlerReferencePath,
    [string]$ModMenuReferencePath,
    [string]$UnityModManagerReferencePath,
    [switch]$CheckStartup,
    [switch]$CheckRules,
    [ValidateSet('none', 'amnesiac066')][string]$Focus = 'none'
)
$ErrorActionPreference = 'Stop'
$projectRoot = $PSScriptRoot
$workspaceRoot = [IO.Path]::GetFullPath((Join-Path $projectRoot '../..'))
$manifest = Get-Content -LiteralPath (Join-Path $projectRoot 'package/OwlcatModificationManifest.json') -Raw | ConvertFrom-Json
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
# Default: compile and check current behavior; legacy full history is not run.
$mediumRoot = Join-Path $projectRoot 'Modules/MediumClass'
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
$assemblyPath = Join-Path $workspaceRoot 'work/prototype-build/CallOfTheWild.Wrath.Prototype/bin/Release/net472/CallOfTheWild.Wrath.Prototype.dll'

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
        '0.14.66Amnesiac-feedback-test-guide.txt' = Join-Path $projectRoot '0.14.66Amnesiac-feedback-test-guide.txt'
        'Localization-QL使用说明.txt' = Join-Path $projectRoot 'Localization-QL使用说明.txt'
        'Localization-QL/CallOfTheWildWrathPrototypeLocalized.json' = Join-Path $projectRoot 'package/Localization-QL/CallOfTheWildWrathPrototypeLocalized.json'
        'LICENSE-LongArm-TTT.txt' = Join-Path $projectRoot 'assets/ElongatedFingers-LICENSE.txt'
        'LICENSE-CotW.txt' = Join-Path $projectRoot 'LICENSE-CotW.txt'
        'README.zh-CN.md' = Join-Path $projectRoot 'README.zh-CN.md'
        '测试者必读.txt' = Join-Path $projectRoot '测试者必读.txt'
        '百宝袋快速测试.txt' = Join-Path $projectRoot '百宝袋快速测试.txt'
        'Assemblies/CallOfTheWild.Wrath.Prototype.dll' = $assemblyPath
        'Modules/Medium/MediumClass.dll' = Join-Path $mediumStage 'MediumClass.dll'
        'Modules/Medium/mediumclass_assets' = Join-Path $mediumStage 'mediumclass_assets'
        'Modules/Medium/LICENSE-CotW.txt' = Join-Path $mediumStage 'LICENSE-CotW.txt'
        'Modules/WayOfTheShield/Way of the shield.dll' = $wayAssembly
        'Modules/WayOfTheShield/NOTICE-WayOfTheShield.txt' = Join-Path $wayRoot 'NOTICE-WayOfTheShield.txt'
        'Modules/WayOfTheShield/upstream-provenance.json' = Join-Path $wayRoot 'upstream-provenance.json'
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

# Test the DLLs read back from the actual ZIP rather than an earlier build copy.
$readback = Join-Path $stagingDirectory 'package-readback'
$null = New-Item -ItemType Directory -Path $readback
$zip = [IO.Compression.ZipFile]::OpenRead($archivePath)
try {
    $names = @($zip.Entries | ForEach-Object FullName)
    if (($names | Select-Object -Unique).Count -ne $names.Count) { throw 'Package contains duplicate entries.' }
    foreach ($name in $files.Keys) {
        $entry = $zip.GetEntry($prefix + $name)
        if (!$entry -or $entry.Length -eq 0) { throw "Missing or empty package file: $name" }
    }
    foreach ($directory in @('Assemblies/', 'Bundles/', 'Blueprints/', 'Localization/')) {
        if (!$zip.GetEntry($prefix + $directory)) { throw "Missing loader directory: $directory" }
    }
    $assemblies = [ordered]@{
        'Assemblies/CallOfTheWild.Wrath.Prototype.dll' = $assemblyPath
        'Modules/Medium/MediumClass.dll' = (Join-Path $mediumStage 'MediumClass.dll')
        'Modules/WayOfTheShield/Way of the shield.dll' = $wayAssembly
    }
    $packagedHashes = [ordered]@{}
    foreach ($entryName in $assemblies.Keys) {
        $entry = $zip.GetEntry($prefix + $entryName)
        if (!$entry) { throw "Missing packaged DLL: $entryName" }
        $destination = Join-Path $readback ([IO.Path]::GetFileName($entryName))
        [IO.Compression.ZipFileExtensions]::ExtractToFile($entry, $destination)
        $actual = (Get-FileHash -LiteralPath $destination -Algorithm SHA256).Hash
        if ($actual -cne (Get-FileHash -LiteralPath $assemblies[$entryName] -Algorithm SHA256).Hash) { throw "Packaged DLL differs from build: $entryName" }
        $packagedHashes[$entryName] = $actual
    }
    $assetEntry = $zip.GetEntry($prefix + 'Modules/Medium/mediumclass_assets')
    if (!$assetEntry) { throw 'Missing packaged Medium asset bundle.' }
    $assetReadback = Join-Path $readback 'mediumclass_assets'
    [IO.Compression.ZipFileExtensions]::ExtractToFile($assetEntry, $assetReadback)
    if ((Get-FileHash -LiteralPath $assetReadback).Hash -cne (Get-FileHash -LiteralPath (Join-Path $mediumStage 'mediumclass_assets')).Hash) { throw 'Packaged Medium asset bundle differs from build.' }
} finally { $zip.Dispose() }
$packagedMain = Join-Path $readback 'CallOfTheWild.Wrath.Prototype.dll'
$packagedMedium = Join-Path $readback 'MediumClass.dll'
$startupRecord = $null
if ($CheckStartup) {
    $startupDirectory = Join-Path $stagingDirectory 'startup'
    & (Join-Path $projectRoot 'tests/Test-StartupRegressions.ps1') -WrathPath $WrathPath -MainAssemblyPath $packagedMain -MediumAssemblyPath $packagedMedium -OutputDirectory $startupDirectory
    $startup = Get-Content -LiteralPath (Join-Path $startupDirectory 'results.json') -Raw | ConvertFrom-Json
    if ($startup.passedCases -ne 16 -or $startup.blueprintCorePatchesInstalled -ne 5) { throw 'Startup checks failed.' }
    $startupRecord = Join-Path $startupDirectory 'results.json'
}
if ($CheckRules) {
    & dotnet run --project (Join-Path $projectRoot 'tests/RulesTests.csproj') -c Release "-p:WrathPath=$WrathPath" --nologo
    if ($LASTEXITCODE -ne 0) { throw 'Core rules verification failed.' }
}
$feedbackRecord = $null
$combatRecord = $null
if ($Focus -eq 'amnesiac066') {
    & python (Join-Path $projectRoot 'tests/Extract-PsychicCompletion01458.py') $WrathPath (Join-Path $workspaceRoot 'work/prototype-build/psychic-completion01458-native.json')
    if ($LASTEXITCODE -ne 0) { throw 'Current Amnesiac native input extraction failed.' }
    $feedback = Join-Path $stagingDirectory 'amnesiac-feedback'
    & python (Join-Path $projectRoot 'tests/Run-AmnesiacFeedback066.py') $packagedMain $WrathPath $feedback
    if ($LASTEXITCODE -ne 0) { throw 'Current Amnesiac feedback checks failed.' }
    $feedbackRecord = Join-Path $feedback 'result.json'
    $combat = Join-Path $stagingDirectory 'amnesiac-combat'
    & python (Join-Path $workspaceRoot 'work/amnesiac-combat-proposal/repeatable-v2/run_combat.py') --candidate $packagedMain --output $combat --game $WrathPath
    if ($LASTEXITCODE -ne 0) { throw 'Current Amnesiac combat checks failed.' }
    $combatRecord = Join-Path $combat 'receipt.json'
}
$record = [ordered]@{
    checkedAt = (Get-Date -Format o)
    version = $manifest.Version
    archive = $archivePath
    archiveSHA256 = (Get-FileHash -LiteralPath $archivePath -Algorithm SHA256).Hash
    packagedDLLs = $packagedHashes
    startup = $startupRecord
    rulesChecked = [bool]$CheckRules
    focus = $Focus
    amnesiacFeedback = $feedbackRecord
    amnesiacCombat = $combatRecord
    scope = 'Compile and package checks plus explicitly selected diagnostics; no historical full regression or player game test.'
    deliveryReady = $false
    gameLaunched = $false
    ummLaunched = $false
}
$record | ConvertTo-Json -Depth 15 | Set-Content -LiteralPath (Join-Path $stagingDirectory '验证记录.json') -Encoding utf8
Write-Output "Staged package: $archivePath"
Write-Output "Validation record: $(Join-Path $stagingDirectory '验证记录.json')"

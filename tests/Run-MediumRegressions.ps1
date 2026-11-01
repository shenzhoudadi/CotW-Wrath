#Requires -Version 7.4
param(
    [Parameter(Mandatory = $true)][string]$WrathPath,
    [Parameter(Mandatory = $true)][string]$MediumAssemblyPath,
    [Parameter(Mandatory = $true)][string]$OutputDirectory
)
$ErrorActionPreference = 'Stop'
$moduleRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../Modules/MediumClass'))
$managed = Join-Path $WrathPath 'Wrath_Data/Managed'
$runtime = Join-Path $managed '0Harmony.dll'
if (([Reflection.AssemblyName]::GetAssemblyName($runtime)).Version -ne [Version]'2.0.4.0') {
    throw 'Medium regression requires the actual game Harmony 2.0.4.'
}
$null = New-Item -ItemType Directory -Path $OutputDirectory -Force
$native = Join-Path $managed 'Assembly-CSharp.dll'
$blueprints = Join-Path $WrathPath 'blueprints.zip'
$strings = Join-Path $moduleRoot 'Strings/Feats.json'
$suites = @(
    @{ Name='MediumProgressionLevels'; Project='MediumProgressionLevels.csproj'; Arguments=@($native,$blueprints,$MediumAssemblyPath) },
    @{ Name='SpiritSpellcasting'; Project='SpiritSpellcasting.Tests.csproj'; Arguments=@($native,$MediumAssemblyPath) },
    @{ Name='SpiritSpellcastingScenarios'; Project='SpiritSpellcastingScenarios.csproj'; Arguments=@() },
    @{ Name='SpiritSaveRestore'; Project='SpiritSaveRestore.csproj'; Arguments=@($native,$MediumAssemblyPath) },
    @{ Name='SpiritChannelLifecycle'; Project='SpiritChannelLifecycle.csproj'; Arguments=@() },
    @{ Name='SpiritInfluenceScenarios'; Project='SpiritInfluenceScenarios.csproj'; Arguments=@($MediumAssemblyPath,$strings) },
    @{ Name='SpiritSurgeScenarios'; Project='SpiritSurgeScenarios.csproj'; Arguments=@() },
    @{ Name='InfluenceScenarios'; Project='InfluenceScenarios.csproj'; Arguments=@($native) },
    @{ Name='SpiritSpellcastingCompatibility'; Project='SpiritSpellcastingCompatibility.csproj'; Arguments=@() },
    @{ Name='SuddenAttack'; Project='SuddenAttack.csproj'; Arguments=@($native,$MediumAssemblyPath,$blueprints) },
    @{ Name='ArchmageDamage'; Project='ArchmageDamage.csproj'; Arguments=@($native,$blueprints) },
    @{ Name='SpiritActionBar'; Project='SpiritActionBar.csproj'; Arguments=@($native); Framework='net472' },
    @{ Name='HardenedSoul'; Project='HardenedSoul.csproj'; Arguments=@() },
    @{ Name='MythicRegistration'; Project='MythicRegistration.csproj'; Arguments=@((Join-Path $moduleRoot 'bin/Release/net472/BlueprintCore.dll')) },
    @{ Name='CombatLog'; Project='CombatLog.csproj'; Arguments=@() },
    @{ Name='SpiritBinderArchetype'; Project='SpiritBinderArchetype.csproj'; Arguments=@($MediumAssemblyPath,$native,$strings) }
)
$results = @()
foreach ($suite in $suites) {
    $project = Join-Path $moduleRoot "tests/$($suite.Name)/$($suite.Project)"
    $command = @('run','--project',$project,'-c','Release','--no-launch-profile',
        "-p:WrathPath=$WrathPath", "-p:HarmonyReferencePath=$runtime", "-p:HarmonyReference=$runtime")
    if ($suite.Framework) { $command += "-p:TestFramework=$($suite.Framework)" }
    $command += @('--') + @($suite.Arguments)
    $lines = @(& dotnet @command 2>&1)
    $resultCode = $LASTEXITCODE
    $log = Join-Path $OutputDirectory "$($suite.Name).log"
    $lines | Set-Content -LiteralPath $log -Encoding utf8
    $summary = @($lines | Where-Object { "$_" -match 'PASS|FAIL|assertions passed|checks passed' })
    $results += [ordered]@{ suite=$suite.Name; exitCode=$resultCode; log=$log; summary=$summary; harmonyReference=$runtime }
    if ($resultCode -ne 0) { throw "Medium regression failed: $($suite.Name); see $log" }
    Write-Output "PASS: Medium $($suite.Name)"
}
$record = [ordered]@{
    passedSuites=$results.Count; results=$results
    requiredGameHarmony=@{ version='2.0.4.0'; path=[IO.Path]::GetFullPath($runtime); sha256=(Get-FileHash -LiteralPath $runtime).Hash }
    scope='Linked production/API models or actual native IL/blueprint anchors; dynamic legacy Harmony models use net472. Game UI/Unity endpoints are separate.'
}
$record | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath (Join-Path $OutputDirectory 'results.json') -Encoding utf8

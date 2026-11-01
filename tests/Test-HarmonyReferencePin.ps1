#Requires -Version 7.4
param(
    [Parameter(Mandatory = $true)][string]$WrathPath,
    [Parameter(Mandatory = $true)][string]$OutputDirectory
)
$ErrorActionPreference = 'Stop'
$projectRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$runtime = [IO.Path]::GetFullPath((Join-Path $WrathPath 'Wrath_Data/Managed/0Harmony.dll'))
$expectedIdentity = '0Harmony, Version=2.0.4.0, Culture=neutral, PublicKeyToken=null'
$session = Join-Path $OutputDirectory ([guid]::NewGuid().ToString('N'))
$null = New-Item -ItemType Directory -Path $session -Force
$cases = @()

function Invoke-PinCase([string]$Name, [string[]]$Arguments, [string]$ExpectedError) {
    $lines = @(& dotnet @Arguments 2>&1)
    $code = $LASTEXITCODE
    $body = ($lines | ForEach-Object { $_.ToString() }) -join "`n"
    $log = Join-Path $session "$Name.log"
    $body | Set-Content -LiteralPath $log -Encoding utf8
    if ($ExpectedError) {
        if ($code -eq 0 -or $body -notmatch [regex]::Escape($ExpectedError)) {
            throw "Expected build rejection $ExpectedError in $Name; exit=$code; see $log"
        }
    } elseif ($code -ne 0) {
        throw "Pinned game reference check failed: $Name; see $log"
    }
    return @{ name=$Name; exitCode=$code; expectedError=$ExpectedError; log=$log; output=$body }
}

# Evaluate and execute the inherited guard for every real project, including
# projects whose source does not currently call Harmony. No compile fallback.
$projects = @(Get-ChildItem -LiteralPath $projectRoot -Recurse -Filter '*.csproj' -File |
    Where-Object { $_.FullName -notmatch '[\\/](bin|obj|artifacts|lib)[\\/]' } | Sort-Object FullName)
foreach ($project in $projects) {
    $name = 'project-' + [IO.Path]::GetRelativePath($projectRoot,$project.FullName).Replace('\','_').Replace('/','_')
    $case = Invoke-PinCase $name @('msbuild',$project.FullName,"-p:WrathPath=$WrathPath",'-t:ValidatePinnedGameHarmony','-getItem:Reference,PackageReference','-getProperty:UseGameHarmonyReference','-nologo','-verbosity:quiet') ''
    $evaluation = $case.output | ConvertFrom-Json
    $items = $evaluation.Items
    $refs = @($items.Reference | Where-Object { $_.Identity -like '0Harmony*' })
    $expectedCount = if ($evaluation.Properties.UseGameHarmonyReference -eq 'true') { 1 } else { 0 }
    if ($refs.Count -ne $expectedCount -or ($refs.Count -eq 1 -and
        ($refs[0].Identity -ne $expectedIdentity -or $refs[0].SpecificVersion -ne 'true' -or [IO.Path]::GetFullPath($refs[0].HintPath) -ne $runtime)) -or
        @($items.PackageReference | Where-Object { $_.Identity -match 'Harmony' }).Count -ne 0) {
        throw "Project has an unpinned or duplicate Harmony reference: $($project.FullName)"
    }
    $case.Remove('output'); $cases += $case
}

# A deliberately wrong assembly identity, built without Harmony/NuGet, is only
# a negative-test fixture. It is never loaded or referenced by the mod/tests.
$wrongSource = Join-Path $session 'wrong-identity'
$null = New-Item -ItemType Directory -Path $wrongSource
'<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>net9.0</TargetFramework><AssemblyName>0Harmony</AssemblyName><AssemblyVersion>2.4.2.0</AssemblyVersion></PropertyGroup></Project>' |
    Set-Content -LiteralPath (Join-Path $wrongSource 'Fixture.csproj') -Encoding utf8
'public sealed class DeliberatelyWrongIdentity {}' |
    Set-Content -LiteralPath (Join-Path $wrongSource 'Fixture.cs') -Encoding utf8
$fixture = Invoke-PinCase 'create-wrong-identity' @('build',(Join-Path $wrongSource 'Fixture.csproj'),'-c','Release','--nologo','-verbosity:quiet') ''
$wrongRuntime = Join-Path $wrongSource 'bin/Release/net9.0/0Harmony.dll'

function New-GameFixture([string]$Name, [string]$Source) {
    $root = Join-Path $session $Name
    $managed = Join-Path $root 'Wrath_Data/Managed'
    $null = New-Item -ItemType Directory -Path $managed -Force
    if ($Source) { Copy-Item -LiteralPath $Source -Destination (Join-Path $managed '0Harmony.dll') }
    return $root
}
$wrongGame = New-GameFixture 'wrong-version-game' $wrongRuntime
$hashGame = New-GameFixture 'wrong-hash-game' $runtime
$hashFile = Join-Path $hashGame 'Wrath_Data/Managed/0Harmony.dll'
# Trailing fixture byte preserves the assembly identity but changes SHA256.
[IO.File]::WriteAllBytes($hashFile, [byte[]]([IO.File]::ReadAllBytes($hashFile) + [byte]127))
$missingGame = New-GameFixture 'missing-game' ''
$copyGame = New-GameFixture 'identical-copy-game' $runtime
$copyRuntime = Join-Path $copyGame 'Wrath_Data/Managed/0Harmony.dll'

$representatives = @{
    Main='CallOfTheWild.Wrath.Prototype.csproj'
    Medium='Modules/MediumClass/MediumClass.csproj'
    Test='Modules/MediumClass/tests/PsychicCasting/PsychicCasting.csproj'
}
foreach ($name in $representatives.Keys) {
    $project = Join-Path $projectRoot $representatives[$name]
    foreach ($scenario in @(
        @{ name='override'; root=$WrathPath; extra=@("-p:HarmonyReferencePath=$wrongRuntime"); error='COTWHARMONY005' },
        @{ name='version'; root=$wrongGame; extra=@(); error='COTWHARMONY003' },
        @{ name='hash'; root=$hashGame; extra=@(); error='COTWHARMONY004' }
    )) {
        # Actual dotnet build entry point must reject before compilation.
        $args = @('build',$project,'-c','Release',"-p:WrathPath=$($scenario.root)",'--nologo','-verbosity:quiet') + $scenario.extra
        $case = Invoke-PinCase "$name-$($scenario.name)" $args $scenario.error
        $case.Remove('output'); $cases += $case
    }
}
$main = Join-Path $projectRoot 'CallOfTheWild.Wrath.Prototype.csproj'
foreach ($scenario in @(
    @{ name='legacy-alias'; root=$WrathPath; extra=@("-p:HarmonyReference=$wrongRuntime"); error='COTWHARMONY005' },
    @{ name='identical-copy'; root=$WrathPath; extra=@("-p:HarmonyReferencePath=$copyRuntime"); error='COTWHARMONY005' },
    @{ name='missing'; root=$missingGame; extra=@(); error='COTWHARMONY002' }
)) {
    $args = @('build',$main,'-c','Release',"-p:WrathPath=$($scenario.root)",'--nologo','-verbosity:quiet') + $scenario.extra
    $case = Invoke-PinCase $scenario.name $args $scenario.error
    $case.Remove('output'); $cases += $case
}

# Direct target probes also reject reintroduced packages, duplicate references
# and a compiler-resolved shadow copy. They don't restore forbidden packages.
$props = [System.Security.SecurityElement]::Escape((Join-Path $projectRoot 'Directory.Build.props'))
$escapedCopy = [System.Security.SecurityElement]::Escape($copyRuntime)
foreach ($scenario in @(
    @{ name='nuget'; items='<PackageReference Include="Lib.Harmony" Version="2.3.6" />'; target='ValidatePinnedGameHarmony'; error='COTWHARMONY006' },
    @{ name='duplicate'; items="<Reference Include=`"0Harmony`"><HintPath>$escapedCopy</HintPath></Reference>"; target='ValidatePinnedGameHarmony'; error='COTWHARMONY007' },
    @{ name='resolved-shadow'; items="<ReferencePath Include=`"$escapedCopy`" />"; target='ValidateResolvedGameHarmony'; error='COTWHARMONY008' }
)) {
    $probe = Join-Path $session "$($scenario.name).proj"
    "<Project><Import Project=`"$props`" /><ItemGroup>$($scenario.items)</ItemGroup></Project>" | Set-Content -LiteralPath $probe -Encoding utf8
    $case = Invoke-PinCase $scenario.name @('msbuild',$probe,"-p:WrathPath=$WrathPath","-t:$($scenario.target)",'-nologo','-verbosity:quiet') $scenario.error
    $case.Remove('output'); $cases += $case
}
$record = [ordered]@{
    checkedProjects=$projects.Count; rejectedCases=@($cases | Where-Object expectedError).Count
    runtime=@{ path=$runtime; version=([Reflection.AssemblyName]::GetAssemblyName($runtime)).Version.ToString(); sha256=(Get-FileHash -LiteralPath $runtime).Hash }
    cases=$cases; fixtureScope='Wrong identity / changed hash copies are isolated negative-test inputs; installed game DLL is never modified.'
}
$record | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath (Join-Path $OutputDirectory 'results.json') -Encoding utf8
Write-Output "PASS: Harmony compile pin: $($projects.Count) projects, $($record.rejectedCases) rejected cases."

#Requires -Version 7.4
param([string]$MainAssembly,[string]$MediumAssembly,[string]$WrathPath,[string]$OutputDirectory)
$ErrorActionPreference='Stop'
$project=Split-Path $PSScriptRoot -Parent
$workspace=[IO.Path]::GetFullPath((Join-Path $project '../..'))
$deps=Join-Path $workspace 'work/repair01440/dependencies'
$refs=Join-Path $workspace 'work/repair01440/references'
$tool=Join-Path $project 'Modules/MediumClass/tests/HarmonyCompatibility/bin/Release/net9.0/HarmonyCompatCheck.dll'
$null=New-Item -ItemType Directory -Path $OutputDirectory -Force
$umm=Join-Path $refs 'UnityModManager.dll'
if (([Reflection.AssemblyName]::GetAssemblyName($umm)).Version -ne [Version]'0.32.4.0') { throw '040 requires the actual UMM 0.32.4 reference.' }
$base=@(Get-ChildItem (Join-Path $deps 'TabletopTweaks-Base') -Recurse -Filter 'TabletopTweaks-Base.dll')[0].FullName
$core=@(Get-ChildItem (Join-Path $deps 'TabletopTweaks-Core') -Recurse -Filter 'TabletopTweaks-Core.dll')[0].FullName
$ec=Join-Path $deps 'ExpandedContent/ExpandedContent.dll'
$reports=@()
foreach($dll in @($MainAssembly,$MediumAssembly,(Join-Path $refs 'ModMenu.dll'),$core)) {
    $report=Join-Path $OutputDirectory ((Split-Path $dll -Leaf)+'.umm.json')
    & dotnet $tool $dll $umm $report
    if($LASTEXITCODE -ne 0){throw "UMM 0.32.4 member compatibility failed: $dll"}
    $r=Get-Content $report -Raw | ConvertFrom-Json
    if($dll -eq $MediumAssembly -and $r.DeclaredReference -notlike 'UnityModManager, Version=0.32.4.0,*'){throw 'Medium must be compiled against UMM 0.32.4.0.'}
    $reports+=@{path=$report;sha256=(Get-FileHash $report).Hash;checkedTypes=$r.CheckedTypes;checkedMethods=$r.CheckedMethods;checkedFields=$r.CheckedFields;missing=$r.MissingCount}
}
$coexist=Join-Path $OutputDirectory 'coexistence.json'
& dotnet $tool --coexist01440 $base $core $ec $MainAssembly (Join-Path $WrathPath 'Wrath_Data/Managed/Assembly-CSharp.dll') $coexist
if($LASTEXITCODE -ne 0){throw 'Actual external-DLL static coexistence checks failed.'}
$co=Get-Content $coexist -Raw|ConvertFrom-Json
$result=@{passed=$true;ummVersion='0.32.4.0';ummSHA256=(Get-FileHash $umm).Hash;ummMemberReports=$reports;coexistenceReport=$coexist;coexistenceSHA256=(Get-FileHash $coexist).Hash;coexistenceChecks=$co.checks;boundary=$co.boundary;provenance=(Get-Content (Join-Path $deps 'provenance.json') -Raw|ConvertFrom-Json)}
$result|ConvertTo-Json -Depth 12|Set-Content (Join-Path $OutputDirectory 'results.json') -Encoding utf8
Write-Output 'PASS: 040 UMM 0.32.4 and actual external-DLL static coexistence'

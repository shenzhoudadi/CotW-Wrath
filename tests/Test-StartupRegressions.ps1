#Requires -Version 7.4
param(
    [Parameter(Mandatory=$true)][string]$WrathPath,
    [Parameter(Mandatory=$true)][string]$MainAssemblyPath,
    [Parameter(Mandatory=$true)][string]$MediumAssemblyPath,
    [Parameter(Mandatory=$true)][string]$OutputDirectory
)
$ErrorActionPreference='Stop'
$project=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$workspace=[IO.Path]::GetFullPath((Join-Path $project '../..'))
$module=Join-Path $project 'Modules/MediumClass'
$output=[IO.Path]::GetFullPath($OutputDirectory)
$null=New-Item -ItemType Directory -Path $output -Force
$managed=[IO.Path]::GetFullPath((Join-Path $WrathPath 'Wrath_Data/Managed'))
$runtime=Join-Path $managed 'UnityModManager/0Harmony.dll'
$expectedSHA='DF043F05C6E43602FA8E8F38F52A3A2CF962AAB7A65A9D85573EA25F01A63E80'
if(([Reflection.AssemblyName]::GetAssemblyName($runtime)).Version -ne [Version]'2.3.6.0' -or (Get-FileHash -LiteralPath $runtime).Hash -cne $expectedSHA){
    throw 'Startup regression requires the audited game Harmony 2.3.6.0 bytes.'
}
$main=[IO.Path]::GetFullPath($MainAssemblyPath)
$medium=[IO.Path]::GetFullPath($MediumAssemblyPath)
$inputs=@($main,$medium,$runtime,(Join-Path $managed 'Assembly-CSharp.dll'),(Join-Path $managed 'UnityEngine.AssetBundleModule.dll'),(Join-Path $managed 'UnityEngine.CoreModule.dll'))
$originalHashes=@{};foreach($file in $inputs){$originalHashes[$file]=(Get-FileHash -LiteralPath $file).Hash}

foreach($relative in @('tests/IntegratedStartupTests.csproj','tests/StartupTests.csproj','Modules/MediumClass/tests/Startup/StartupTests.csproj','tests/HarmonyTests.csproj','Modules/MediumClass/tests/HarmonyCompatibility/HarmonyCompatCheck.csproj')){
    $log=Join-Path $output ('build-'+($relative -replace '[\\/]','_')+'.log')
    $lines=@(& dotnet build (Join-Path $project $relative) -c Release "-p:WrathPath=$WrathPath" --nologo -verbosity:quiet 2>&1)
    $code=$LASTEXITCODE;$lines|Set-Content -LiteralPath $log -Encoding utf8
    if($code -ne 0){throw "Startup test build failed: $relative; see $log"}
}
$fixture=Join-Path $output 'startup-fixture'
$null=New-Item -ItemType Directory -Path $fixture -Force
$cecil=Join-Path $module 'tests/HarmonyCompatibility/bin/Release/net9.0/Mono.Cecil.dll'
$nativeFixture=Join-Path $fixture 'Assembly-CSharp.dll'
& (Join-Path $PSScriptRoot 'Prepare-StartupVisibility.ps1') -NativeAssembly (Join-Path $managed 'Assembly-CSharp.dll') -PublicizedAssembly (Join-Path $module 'obj/publicized/Assembly-CSharp.dll') -CecilAssembly $cecil -OutputAssembly $nativeFixture
& (Join-Path $PSScriptRoot 'Prepare-StartupAssetBundle.ps1') -NativeAssembly (Join-Path $managed 'UnityEngine.AssetBundleModule.dll') -CecilAssembly $cecil -OutputAssembly (Join-Path $fixture 'UnityEngine.AssetBundleModule.dll')
$scene058=Join-Path $fixture 'UnityEngine.CoreModule.dll'
$sceneAudit058=Join-Path $fixture 'unity-scene01458-audit.json'
if(Test-Path -LiteralPath $scene058){
    $sceneProof058=Get-Content -LiteralPath $sceneAudit058 -Raw|ConvertFrom-Json
    if(!$sceneProof058.passed -or !$sceneProof058.otherMethodBodiesAndFieldsExact -or $sceneProof058.changedMethodCount -ne 1 -or $sceneProof058.sourceSHA256 -cne (Get-FileHash -LiteralPath (Join-Path $managed 'UnityEngine.CoreModule.dll')).Hash -or $sceneProof058.fixtureSHA256 -cne (Get-FileHash -LiteralPath $scene058).Hash){throw 'Existing standalone scene dependency does not match exact audited bytes.'}
}else{
    & (Join-Path $PSScriptRoot 'Prepare-UnitySceneEndpoints01458.ps1') -NativeAssembly (Join-Path $managed 'UnityEngine.CoreModule.dll') -CecilAssembly $cecil -OutputAssembly $scene058 -AuditPath $sceneAudit058
}
# Ordinary dependencies resolve from the staged module directory or these local references.
$localPaths=[xml](Get-Content -LiteralPath (Join-Path $module 'MediumClass.local.props') -Raw)
$references=[string]$localPaths.Project.PropertyGroup.UMMPath
$results=[Collections.Generic.List[object]]::new()
function Run-Case([string]$Name,[string]$Exe,[string[]]$Arguments,[string]$ExpectedFailure=''){
    $lines=@(& $Exe @Arguments 2>&1);$code=$LASTEXITCODE
    $log=Join-Path $output ($Name+'.log');$lines|Set-Content -LiteralPath $log -Encoding utf8
    if($ExpectedFailure){
        if($code -eq 0 -or ($lines -join "`n") -notmatch $ExpectedFailure){throw "Expected rejection did not occur: $Name; see $log"}
    }elseif($code -ne 0){throw "Startup/runtime regression failed: $Name; see $log"}
    $results.Add(@{name=$Name;exitCode=$code;expectedRejection=[bool]$ExpectedFailure;log=$log})
    Write-Output "PASS: $Name"
}
$launchers=@{
    integrated=Join-Path $workspace 'work/prototype-build/IntegratedStartupTests/bin/Release/net472/IntegratedStartupTests.exe'
    main=Join-Path $workspace 'work/prototype-build/StartupTests/bin/Release/net472/StartupTests.exe'
    medium=Join-Path $module 'tests/Startup/bin/Release/net472/StartupTests.exe'
}
foreach($name in @('integrated','main','medium')){
    foreach($order in @('mediumfirst','cotwfirst')){
        Run-Case "$name-$order" $launchers[$name] @($managed,$medium,$main,$runtime,$references,$order,(Join-Path $output "$name-$order.json"),$nativeFixture)
        $record=Get-Content -LiteralPath (Join-Path $output "$name-$order.json") -Raw|ConvertFrom-Json
        if($record.blueprintCorePatchesInstalled -ne 5){throw 'All five BlueprintCore hooks must have installed.'}
    }
}

# A package-layout directory fixture uses the exact tested DLL bytes, without creating an installer.
$packaged=Join-Path $output 'packaged/CallOfTheWildWrathPrototype'
$null=New-Item -ItemType Directory -Path (Join-Path $packaged 'Assemblies'),(Join-Path $packaged 'Modules/Medium') -Force
$packagedMain=Join-Path $packaged 'Assemblies/CallOfTheWild.Wrath.Prototype.dll'
$packagedMedium=Join-Path $packaged 'Modules/Medium/MediumClass.dll'
Copy-Item -LiteralPath $main -Destination $packagedMain
Copy-Item -LiteralPath $medium -Destination $packagedMedium
Copy-Item -LiteralPath (Join-Path ([IO.Path]::GetDirectoryName($medium)) 'mediumclass_assets') -Destination (Join-Path $packaged 'Modules/Medium/mediumclass_assets')
Run-Case 'integrated-bundle' $launchers.integrated @($managed,$packagedMedium,$packagedMain,$runtime,$references,'bundle',(Join-Path $output 'bundle.json'),$nativeFixture)
if((Get-Content -LiteralPath (Join-Path $output 'bundle.json') -Raw|ConvertFrom-Json).blueprintCorePatchesInstalled -ne 5){throw 'Packaged preparation omitted BlueprintCore patches.'}
Run-Case 'integrated-conflict' $launchers.integrated @($managed,$medium,$packagedMain,$runtime,$references,'conflict',(Join-Path $output 'conflict.json'),$nativeFixture)
Run-Case 'missing-BlueprintCore-hook' $launchers.integrated @($managed,$medium,$main,$runtime,$references,'missingbc',(Join-Path $output 'missingbc.json'),$nativeFixture) 'BlueprintCore.*(never patched|missing)'

$harmonyExe=Join-Path $workspace 'work/prototype-build/HarmonyTests/bin/Release/net472/HarmonyTests.exe'
Run-Case 'actual-game-Harmony-production-targets' $harmonyExe @($managed,$main,$runtime,$scene058,(Get-FileHash -LiteralPath $scene058).Hash)
$changed=Join-Path $output 'same-version-different-bytes.dll'
Copy-Item -LiteralPath $runtime -Destination $changed
$stream=[IO.File]::Open($changed,[IO.FileMode]::Append)
try{$stream.WriteByte(0)}finally{$stream.Dispose()}
if(([Reflection.AssemblyName]::GetAssemblyName($changed)).Version -ne [Version]'2.3.6.0' -or (Get-FileHash -LiteralPath $changed).Hash -ceq $expectedSHA){throw 'Invalid same-version negative fixture.'}
Run-Case 'direct-EXE-same-version-wrong-hash' $harmonyExe @($managed,$main,$changed) 'Regression requires actual game Harmony 2.3.6 bytes'
foreach($name in @('integrated','main','medium')){
    Run-Case "$name-rejects-wrong-hash" $launchers[$name] @($managed,$medium,$main,$changed,$references,'mediumfirst',(Join-Path $output "$name-wrong-hash.json"),$nativeFixture) 'Regression requires actual game Harmony 2.3.6 bytes'
}
$shadow=Join-Path $output 'direct-EXE-shadow'
$null=New-Item -ItemType Directory -Path $shadow -Force
Copy-Item -LiteralPath $harmonyExe,($harmonyExe+'.config') -Destination $shadow
Copy-Item -LiteralPath $changed -Destination (Join-Path $shadow '0Harmony.dll')
Run-Case 'direct-EXE-shadowed-runtime' (Join-Path $shadow 'HarmonyTests.exe') @($managed,$main,$runtime) 'Regression requires actual game Harmony 2.3.6 bytes'
Copy-Item -LiteralPath $runtime -Destination (Join-Path $shadow '0Harmony.dll') -Force
Run-Case 'direct-EXE-wrong-expected-path' (Join-Path $shadow 'HarmonyTests.exe') @($managed,$main,$changed) 'Regression requires actual game Harmony 2.3.6 bytes'
foreach($file in $inputs){if((Get-FileHash -LiteralPath $file).Hash -cne $originalHashes[$file]){throw "Regression modified a production input: $file"}}
@{
    passedCases=$results.Count;expectedRejections=@($results|Where-Object expectedRejection).Count;results=$results
    requiredGameHarmony=@{version='2.3.6.0';path=$runtime;sha256=$expectedSHA}
    blueprintCorePatchesInstalled=5
    fixtures=@{visibility=$nativeFixture;assetBundle=Join-Path $fixture 'UnityEngine.AssetBundleModule.dll';scope='Original public LoadAsset patch target preserved; private native endpoint is a managed trap. All five actual patch classes installed/owner-checked/removed.'}
    sceneDependency01458=@{fixture=$scene058;fixtureSHA256=(Get-FileHash -LiteralPath $scene058).Hash;audit=$sceneAudit058;auditSHA256=(Get-FileHash -LiteralPath $sceneAudit058).Hash;productionPatchTargetsUnchanged=$true}
    productionInputHashes=$originalHashes
}|ConvertTo-Json -Depth 8|Set-Content -LiteralPath (Join-Path $output 'results.json') -Encoding utf8
Write-Output "PASS: $($results.Count) startup/runtime cases; all five BlueprintCore hooks covered."

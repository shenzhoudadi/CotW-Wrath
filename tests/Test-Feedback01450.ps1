param([Parameter(Mandatory=$true)][string]$WrathPath,[Parameter(Mandatory=$true)][string]$HostPath,[Parameter(Mandatory=$true)][string]$MainAssembly,[Parameter(Mandatory=$true)][string]$NativePath,[Parameter(Mandatory=$true)][string]$ExpansionPath,[Parameter(Mandatory=$true)][string]$OutputPath)
$ErrorActionPreference='Stop'
$root=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../../..'))
$inputs=@($HostPath,$MainAssembly,$NativePath,$ExpansionPath,(Join-Path $root 'work/prototype-build/01450-native.json'))+@(Get-ChildItem -LiteralPath (Join-Path $root 'work/psychic01458/runtime-adapter') -File | Where-Object {$_.Extension -in @('.dll','.json')} | Select-Object -ExpandProperty FullName)
$inputs+=@((Join-Path ([Environment]::GetFolderPath('UserProfile')) '.nuget/packages/dnlib/3.1.0/lib/net461/dnlib.dll'),(Join-Path $root 'work/modmenu-release/ModMenu.dll'),(Join-Path $root 'work/repair01440/references/UnityModManager.dll'),(Join-Path $root 'work/psychic01458/current-source-manifest.json'),(Join-Path $root 'work/psychic01458/external-anchor-audit.json'))
foreach($folder in @('work/batch01447/sources/Vek17__TabletopTweaks-Base/tree/TabletopTweaks-Base/Config','work/batch01447/sources/Vek17__TabletopTweaks-Core/tree/TabletopTweaks-Core/Config','work/repair01449/sources/pheonix99__TomeOfTheFirebird/tree/TomeOfTheFirebird/Config','work/repair01449/sources/pheonix99__TomeOfTheFirebird/tree/TomeOfTheFirebird/Localization')){$inputs+=@(Get-ChildItem -LiteralPath (Join-Path $root $folder) -File -Filter '*.json' | Select-Object -ExpandProperty FullName)}
$before=@{};foreach($path in $inputs){$before[$path]=(Get-FileHash -LiteralPath $path).Hash}
$savedOnly=$env:COTW_ARCANE_01446_ONLY;$savedCase=$env:COTW_01450_CASE
$rows=@()
try {
  $env:COTW_ARCANE_01446_ONLY='1'
  foreach($case in @('external','external-late','external-fault')) {
    $env:COTW_01450_CASE=$case;$result=[IO.Path]::ChangeExtension($OutputPath,"$case.json")
    & $HostPath (Join-Path $WrathPath 'Wrath_Data/Managed') $MainAssembly $result $NativePath $ExpansionPath
    if($LASTEXITCODE -ne 0){throw "01450 actual provider/native selection case failed: $case"}
    $j=Get-Content -LiteralPath $result -Raw | ConvertFrom-Json;$contract=$j.externalBloodlines01450Contracts
    if(!$contract.passed -or $contract.progressions -ne 10 -or $contract.growthRows -ne 12 -or $contract.nativeCanSelectCalls -ne 72 -or $contract.actualExternalECMQualifications -ne 10 -or !$contract.repeatIdentityPreserved -or !$contract.singleECMPublicationCallback -or !$contract.automaticECMCallback -or $contract.actualReleasedBuilders.Count -ne 5){throw "01450 actual external graph coverage incomplete: $case"}
    if($case -eq 'external-fault' -and !$contract.optionalCollisionAndRetry){throw '01450 independent optional-row rollback/retry missing.'}
    $rows += [ordered]@{case=$case;result=$result;resultSHA256=(Get-FileHash -LiteralPath $result).Hash;contract=$contract}
  }
} finally {$env:COTW_ARCANE_01446_ONLY=$savedOnly;$env:COTW_01450_CASE=$savedCase}
foreach($path in $inputs){if((Get-FileHash -LiteralPath $path).Hash -cne $before[$path]){throw "01450 input changed during execution: $path"}}
[ordered]@{passed=$true;inputs=$before;rows=$rows} | ConvertTo-Json -Depth 40 | Set-Content -LiteralPath $OutputPath -Encoding utf8

param([Parameter(Mandatory=$true)][string]$WrathPath,[Parameter(Mandatory=$true)][string]$HostPath,[Parameter(Mandatory=$true)][string]$MainAssembly,[Parameter(Mandatory=$true)][string]$NativePath,[Parameter(Mandatory=$true)][string]$ExpansionPath,[Parameter(Mandatory=$true)][string]$OutputPath)
$ErrorActionPreference='Stop'
$root=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../../..'))
$inputPaths=@($HostPath,$MainAssembly,$NativePath,$ExpansionPath,(Join-Path $root 'work/prototype-build/01449-native.json'),(Join-Path $root 'work/prototype-build/01450-native.json'))
foreach($n in @('native-power-input.json','current-source-manifest.json','external-anchor-audit.json')){$inputPaths+=Join-Path $root ('work/psychic01458/'+$n)}
$inputPaths+=@(Get-ChildItem -LiteralPath (Join-Path $root 'work/psychic01458/runtime-adapter') -File | Where-Object {$_.Extension -in @('.dll','.json')} | Select-Object -ExpandProperty FullName)
$before=@{};foreach($p in $inputPaths){$before[$p]=(Get-FileHash -LiteralPath $p).Hash}
$saved=@{};foreach($n in @('COTW_ARCANE_01446_ONLY','COTW_01452_CASE','COTW_01452_OBSERVATION','COTW_01451_CASE','COTW_01450_CASE','COTW_01449_CASE')){$saved[$n]=[Environment]::GetEnvironmentVariable($n,'Process')}
$rows=@()
try {
 $env:COTW_ARCANE_01446_ONLY='1';$env:COTW_01449_CASE=$null;$env:COTW_01451_CASE=$null;$env:COTW_01450_CASE='external-late'
 foreach($case in @('menu-external','menu-external-missed')) {
  $env:COTW_01452_CASE=$case;$result=[IO.Path]::ChangeExtension($OutputPath,"$case.json");$env:COTW_01452_OBSERVATION=[IO.Path]::ChangeExtension($OutputPath,"$case.observation.json")
  & $HostPath (Join-Path $WrathPath 'Wrath_Data/Managed') $MainAssembly $result $NativePath $ExpansionPath
  if($LASTEXITCODE -ne 0){throw "01452 actual parent/generated menu/UI-state case failed: $case"}
  $j=Get-Content -LiteralPath $result -Raw | ConvertFrom-Json;$c=$j.actualExternalMenu01452Contracts
  if(!$c.passed -or $c.observation.failedPaths -ne 0 -or $c.observation.paths.Count -ne 8 -or $c.observation.guardCases.Count -ne 5 -or $c.observation.unchangedBloodragerGraphAssets -lt 40){throw "01452 actual menu/guard/Bloodrager protection evidence incomplete: $case"}
  foreach($p in $c.observation.paths){if(!$p.parentAllowed -or !$p.actualParentItemIsCanonical -or !$p.preexistingProgressionCacheAdvancedOnce -or !$p.havocAppliedOnce -or !$p.nativeUIStates.Count){throw "01452 actual native parent/UI/commit evidence missing: $case"}}
  $rows += [ordered]@{case=$case;result=$result;resultSHA256=(Get-FileHash -LiteralPath $result).Hash;contract=$c}
 }
}finally{foreach($n in $saved.Keys){[Environment]::SetEnvironmentVariable($n,$saved[$n],'Process')}}
foreach($p in $inputPaths){if((Get-FileHash -LiteralPath $p).Hash -cne $before[$p]){throw "01452 actual input changed: $p"}}
[ordered]@{passed=$true;rows=$rows;inputs=$before} | ConvertTo-Json -Depth 60 | Set-Content -LiteralPath $OutputPath -Encoding utf8

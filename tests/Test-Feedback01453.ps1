param([Parameter(Mandatory=$true)][string]$WrathPath,[Parameter(Mandatory=$true)][string]$HostPath,[Parameter(Mandatory=$true)][string]$MainAssembly,[Parameter(Mandatory=$true)][string]$NativePath,[Parameter(Mandatory=$true)][string]$ExpansionPath,[Parameter(Mandatory=$true)][string]$OutputPath)
$ErrorActionPreference='Stop'
$root=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../../..'))
$work=Join-Path $root 'work/psychic01458'
$old=Join-Path $root 'work/repair01453/baseline/zip-dll/CallOfTheWild.Wrath.Prototype.dll'
$inputs=@($HostPath,$MainAssembly,$old,$NativePath,$ExpansionPath,(Join-Path $root 'work/prototype-build/01449-native.json'),(Join-Path $root 'work/prototype-build/01450-native.json'),(Join-Path $root 'work/batch01447/dependencies/ECM/EbonsContentMod.dll'))
foreach($n in @('native-power-input.json','current-source-manifest.json','external-anchor-audit.json')){$inputs+=Join-Path $work $n}
$inputs+=@(Get-ChildItem -LiteralPath (Join-Path $work 'runtime-adapter') -File | Where-Object {$_.Extension -in @('.dll','.json')} | Select-Object -ExpandProperty FullName)
$before=@{};foreach($p in $inputs){$before[$p]=(Get-FileHash -LiteralPath $p).Hash}
$saved=@{};foreach($n in @('COTW_ARCANE_01446_ONLY','COTW_01453_CASE','COTW_01453_OBSERVATION','COTW_01452_CASE','COTW_01452_OBSERVATION','COTW_01451_CASE','COTW_01450_CASE','COTW_01449_CASE')){$saved[$n]=[Environment]::GetEnvironmentVariable($n,'Process')}
$rows=@()
try {
 $env:COTW_ARCANE_01446_ONLY='1';$env:COTW_01449_CASE=$null;$env:COTW_01451_CASE='external';$env:COTW_01452_CASE='menu-external'
 foreach($case in @('new-late','new-early','old-late')) {
  $env:COTW_01453_CASE=$case;$env:COTW_01450_CASE=if($case -like '*late'){'external-late'}else{'external-early'}
  $result=[IO.Path]::ChangeExtension($OutputPath,"$case.json");$env:COTW_01452_OBSERVATION=[IO.Path]::ChangeExtension($OutputPath,"$case.menu.json");$env:COTW_01453_OBSERVATION=[IO.Path]::ChangeExtension($OutputPath,"$case.scan.json")
  $target=if($case -like 'old*'){$old}else{$MainAssembly}
  & $HostPath (Join-Path $WrathPath 'Wrath_Data/Managed') $target $result $NativePath $ExpansionPath
  if($LASTEXITCODE -ne 0){throw "01453 actual ECM scanner case failed: $case"}
  $j=Get-Content -LiteralPath $result -Raw | ConvertFrom-Json;$c=$j.actualECMScan01453Contracts;$o=$c.observation
  if(!$c.passed -or !$o.actualReleasedConfigure -or $o.actualMenus.paths.Count -ne 8){throw "01453 actual scanner/menu evidence missing: $case"}
  if($case -like '*late' -and $o.changedChoices.Count -ne 46){throw "01453 actual46scanner edits missing: $case"}
  if($case -like 'old*'){
   if(!$o.negativeControl -or $o.actualMenus.failedPaths -ne 8 -or !$o.registrationException.Contains('firstDifference=131')){throw '01453 real052 negative control not reproduced.'}
  }else{
   if($o.actualMenus.failedPaths -ne 0 -or $o.nativeRestrictionChecks -lt 2 -or $o.negativeCases.Count -ne 10 -or !$o.independentReceiptIsolation -or !$o.completeTargetIntegrityRequired){throw "01453 current native guard/isolation evidence missing: $case"}
   foreach($p in $o.actualMenus.paths){if(!$p.parentAllowed -or !$p.havocAppliedOnce -or !$p.actualParentItemIsCanonical){throw "01453 actual native path failed: $case"}}
  }
  $rows += [ordered]@{case=$case;result=$result;resultSHA256=(Get-FileHash -LiteralPath $result).Hash;contract=$c;targetSHA256=(Get-FileHash -LiteralPath $target).Hash}
 }
}finally{foreach($n in $saved.Keys){[Environment]::SetEnvironmentVariable($n,$saved[$n],'Process')}}
foreach($p in $inputs){if((Get-FileHash -LiteralPath $p).Hash -cne $before[$p]){throw "01453 actual input changed: $p"}}
[ordered]@{passed=$true;rows=$rows;inputs=$before} | ConvertTo-Json -Depth 70 | Set-Content -LiteralPath $OutputPath -Encoding utf8

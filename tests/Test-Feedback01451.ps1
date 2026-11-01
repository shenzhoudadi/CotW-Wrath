param([Parameter(Mandatory=$true)][string]$WrathPath,[Parameter(Mandatory=$true)][string]$HostPath,[Parameter(Mandatory=$true)][string]$MainAssembly,[Parameter(Mandatory=$true)][string]$NativePath,[Parameter(Mandatory=$true)][string]$ExpansionPath,[Parameter(Mandatory=$true)][string]$OutputPath)
$ErrorActionPreference='Stop'
$root=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../../..'))
$inputPaths=@($HostPath,$MainAssembly,$NativePath,$ExpansionPath,(Join-Path $root 'work/prototype-build/01449-native.json'),(Join-Path $root 'work/prototype-build/01450-native.json'))
foreach($name in @('native-power-input.json','native-matrix-input.json','external-matrix-input.json','slot-anchor-audit.json','current-source-manifest.json')) {$inputPaths+=Join-Path $root ('work/psychic01458/'+$name)}
$inputPaths+=@(Get-ChildItem -LiteralPath (Join-Path $root 'work/psychic01458/runtime-adapter') -File | Where-Object {$_.Extension -in @('.dll','.json')} | Select-Object -ExpandProperty FullName)
$before=@{};foreach($path in $inputPaths){$before[$path]=(Get-FileHash -LiteralPath $path).Hash}
$saved=@{};foreach($name in @('COTW_ARCANE_01446_ONLY','COTW_01451_CASE','COTW_01451_OBSERVATION','COTW_01450_CASE','COTW_01449_CASE')) {$saved[$name]=[Environment]::GetEnvironmentVariable($name,'Process')}
$rows=@()
try {
 $env:COTW_ARCANE_01446_ONLY='1';$env:COTW_01449_CASE=$null
 foreach($case in @('slots','slots-late-assembly','external-before','external-after','external-ttt-last','external-assembly-late','external-fault-new')) {
  $env:COTW_01451_CASE=$case;$env:COTW_01450_CASE=if($case -eq 'external-before'){'external'}elseif($case.StartsWith('external')){'external-late'}else{$null}
  $result=[IO.Path]::ChangeExtension($OutputPath,"$case.json");$env:COTW_01451_OBSERVATION=[IO.Path]::ChangeExtension($OutputPath,"$case.observation.json")
  & $HostPath (Join-Path $WrathPath 'Wrath_Data/Managed') $MainAssembly $result $NativePath $ExpansionPath
  if($LASTEXITCODE -ne 0){throw "01451 actual native/provider case failed: $case"}
  $j=Get-Content -LiteralPath $result -Raw | ConvertFrom-Json
  if($case.StartsWith('slots')) {
   $c=$j.nativeMutationMatrix01451Contracts
   if(!$c.passed -or $c.progressions -ne 71 -or $c.authoredSlots -ne 264 -or $c.growthAndCompanionRows -ne 184 -or $c.actualSorcererBonusPathCalls -lt 54 -or !$c.allRealPrerequisitesExecute -or !$c.repeatIdentityPreserved -or !$c.ownHavocAbsent){throw "01451 native matrix incomplete: $case"}
   if(!$j.bloodlineSlots01451Contracts.passed -or !$j.bloodlineSlots01451Contracts.initialProbe.nativeSorcererOneCanSelectHavoc){throw '01451 actual Sorcerer1 Havoc selection missing.'}
   if($case -eq 'slots-late-assembly' -and $j.bloodlineSlots01451Contracts.initialProbe.ECMAssemblyLoadedBeforeRegistration){throw '01451 supposed late-assembly case loaded ECM early.'}
  } else {
   $c=$j.externalMutationMatrix01451Contracts
   if(!$c.passed -or $c.progressions -ne 14 -or $c.authoredSlots -ne 54 -or $c.growthRows -ne 14 -or $c.newTTTBloodragerECMQueries -ne 4 -or !$c.realTTTPrimaryAndSecondParents -or !$c.primalistForkNotBonusPool -or !$c.singleSafeTTTCompletionHook -or !$c.repeatedIdentities){throw "01451 actual external matrix incomplete: $case"}
  }
  $rows += [ordered]@{case=$case;result=$result;resultSHA256=(Get-FileHash -LiteralPath $result).Hash;contract=$c}
 }
} finally {foreach($name in $saved.Keys){[Environment]::SetEnvironmentVariable($name,$saved[$name],'Process')}}
foreach($path in $inputPaths){if((Get-FileHash -LiteralPath $path).Hash -cne $before[$path]){throw "01451 actual input changed: $path"}}
[ordered]@{passed=$true;inputs=$before;rows=$rows} | ConvertTo-Json -Depth 40 | Set-Content -LiteralPath $OutputPath -Encoding utf8

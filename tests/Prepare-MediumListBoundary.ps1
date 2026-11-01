#Requires -Version 7.4
param(
    [Parameter(Mandatory=$true)][string]$ReferenceDirectory,
    [Parameter(Mandatory=$true)][string]$CecilAssembly,
    [Parameter(Mandatory=$true)][string]$OutputDirectory
)
$ErrorActionPreference='Stop'
$source=[IO.Path]::GetFullPath((Join-Path $ReferenceDirectory 'UnityModManager.dll'))
$output=[IO.Path]::GetFullPath($OutputDirectory)
$target=Join-Path $output 'UnityModManager.dll'
if ($source -ieq $target) { throw 'The Medium list boundary must never overwrite the UMM reference.' }
$before=(Get-FileHash -LiteralPath $source).Hash
$null=New-Item -ItemType Directory -Path $output -Force
Add-Type -Path $CecilAssembly
$assembly=[Mono.Cecil.AssemblyDefinition]::ReadAssembly($source)
try {
    $type=$assembly.MainModule.GetType('UnityModManagerNet.UnityModManager')
    $constructor=@($type.Methods | Where-Object Name -eq '.cctor')
    if ($constructor.Count -ne 1) { throw 'Expected exactly one UMM environment initializer.' }
    # Test-only boundary: Medium needs ModLogger's type and a patched mod-presence
    # query. Do not initialize UMM, its dnlib loader, Unity scene, or filesystem.
    $body=$constructor[0].Body
    $body.Instructions.Clear();$body.ExceptionHandlers.Clear();$body.Variables.Clear()
    $body.GetILProcessor().Append([Mono.Cecil.Cil.Instruction]::Create([Mono.Cecil.Cil.OpCodes]::Ret))
    $assembly.Write($target)
} finally { $assembly.Dispose() }
foreach ($name in @('ModMenu.dll','TabletopTweaks-Core.dll')) {
    $path=Join-Path $ReferenceDirectory $name
    if (Test-Path -LiteralPath $path) {Copy-Item -LiteralPath $path -Destination (Join-Path $output $name) -Force}
}
if ((Get-FileHash -LiteralPath $source).Hash -cne $before) {throw 'The UMM source reference changed.'}
@{source=$source;sourceSHA256=$before;fixture=$target;fixtureSHA256=(Get-FileHash -LiteralPath $target).Hash;changedMethod='UnityModManagerNet.UnityModManager::.cctor';scope='Test only: skip UMM environment startup; actual Medium and BlueprintCore production methods stay intact.'}|ConvertTo-Json|Set-Content -LiteralPath (Join-Path $output 'medium-list-boundary.json') -Encoding utf8

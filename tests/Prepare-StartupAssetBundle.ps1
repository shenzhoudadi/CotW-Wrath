# Standalone CLR fixture only. Never install or package the generated Unity DLL.
param([Parameter(Mandatory=$true)][string]$NativeAssembly,
      [Parameter(Mandatory=$true)][string]$CecilAssembly,
      [Parameter(Mandatory=$true)][string]$OutputAssembly)
$ErrorActionPreference='Stop'
$inputPath=[IO.Path]::GetFullPath($NativeAssembly)
$outputPath=[IO.Path]::GetFullPath($OutputAssembly)
if($inputPath -eq $outputPath){throw 'Startup fixture must not overwrite the game assembly.'}
$null=[Reflection.Assembly]::LoadFrom([IO.Path]::GetFullPath($CecilAssembly))
$resolver=[Mono.Cecil.DefaultAssemblyResolver]::new()
$resolver.AddSearchDirectory([IO.Path]::GetDirectoryName($inputPath))
$parameters=[Mono.Cecil.ReaderParameters]::new();$parameters.AssemblyResolver=$resolver
$assembly=[Mono.Cecil.AssemblyDefinition]::ReadAssembly($inputPath,$parameters)
function TargetBody($Module){
    $target=@($Module.GetType('UnityEngine.AssetBundle').Methods | Where-Object {
        $_.Name -eq 'LoadAsset' -and $_.Parameters.Count -eq 2 -and
        $_.Parameters[0].ParameterType.FullName -eq 'System.String' -and $_.Parameters[1].ParameterType.FullName -eq 'System.Type'
    })
    if($target.Count -ne 1){throw 'Unexpected AssetBundle.LoadAsset target.'}
    return ($target[0].Body.Instructions | ForEach-Object { $_.OpCode.ToString()+':'+[string]$_.Operand }) -join "`n"
}
try{
    $originalTarget=TargetBody $assembly.MainModule
    $endpoint=@($assembly.MainModule.GetType('UnityEngine.AssetBundle').Methods | Where-Object Name -eq 'LoadAsset_Internal')
    if($endpoint.Count -ne 1 -or !$endpoint[0].IsInternalCall -or $endpoint[0].HasBody){throw 'Unexpected Unity native endpoint.'}
    $method=$endpoint[0]
    # Keep the patched public method intact. Only the unreachable native engine
    # endpoint becomes a managed trap so CLR can compile its caller for detouring.
    $method.ImplAttributes=[Mono.Cecil.MethodImplAttributes]::IL
    $method.Body=[Mono.Cecil.Cil.MethodBody]::new($method)
    $exception=[Mono.Cecil.TypeReference]::new('System','InvalidOperationException',$assembly.MainModule,$assembly.MainModule.TypeSystem.CoreLibrary)
    $constructor=[Mono.Cecil.MethodReference]::new('.ctor',$assembly.MainModule.TypeSystem.Void,$exception)
    $constructor.HasThis=$true
    $constructor.Parameters.Add([Mono.Cecil.ParameterDefinition]::new($assembly.MainModule.TypeSystem.String))
    $il=$method.Body.GetILProcessor()
    $il.Append($il.Create([Mono.Cecil.Cil.OpCodes]::Ldstr,'Startup fixture must not execute Unity native asset loading.'))
    $il.Append($il.Create([Mono.Cecil.Cil.OpCodes]::Newobj,$constructor))
    $il.Append($il.Create([Mono.Cecil.Cil.OpCodes]::Throw))
    $null=New-Item -ItemType Directory -Path (Split-Path $outputPath -Parent) -Force
    $assembly.Write($outputPath)
    $verified=[Mono.Cecil.AssemblyDefinition]::ReadAssembly($outputPath,$parameters)
    try{
        if((TargetBody $verified.MainModule) -cne $originalTarget){throw 'The production patch target was changed by the fixture.'}
        if(@($verified.MainModule.GetType('UnityEngine.AssetBundle').Methods | Where-Object Name -eq 'LoadAsset_Internal')[0].IsInternalCall){throw 'Native endpoint was not isolated.'}
    }finally{$verified.Dispose()}
    Write-Output 'PASS: AssetBundle.LoadAsset target body unchanged; only private native endpoint is a test trap.'
}finally{$assembly.Dispose();$resolver.Dispose()}

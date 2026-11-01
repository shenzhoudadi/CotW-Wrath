# Standalone CLR dependency fixture. Never install or package its output.
param([Parameter(Mandatory=$true)][string]$NativeAssembly,
      [Parameter(Mandatory=$true)][string]$CecilAssembly,
      [Parameter(Mandatory=$true)][string]$OutputAssembly,
      [Parameter(Mandatory=$true)][string]$AuditPath)
$ErrorActionPreference='Stop'
$sourcePath=[IO.Path]::GetFullPath($NativeAssembly)
$targetPath=[IO.Path]::GetFullPath($OutputAssembly)
$auditFile=[IO.Path]::GetFullPath($AuditPath)
$taskRoot=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../../..'))
$fixtureRoot=[IO.Path]::GetFullPath((Join-Path $taskRoot 'work'))+[IO.Path]::DirectorySeparatorChar
foreach($path in @($targetPath,$auditFile)){
    if(!$path.StartsWith($fixtureRoot,[StringComparison]::OrdinalIgnoreCase) -or (Test-Path -LiteralPath $path)){throw 'New scene fixture/audit must be a fresh file inside this workspace work directory.'}
}
if($sourcePath -eq $targetPath -or [IO.Path]::GetFileName($sourcePath) -cne 'UnityEngine.CoreModule.dll' -or [IO.Path]::GetFileName($targetPath) -cne 'UnityEngine.CoreModule.dll'){throw 'Unexpected Unity scene fixture paths.'}
$beforeSHA=(Get-FileHash -LiteralPath $sourcePath).Hash
$null=[Reflection.Assembly]::LoadFrom([IO.Path]::GetFullPath($CecilAssembly))
$resolver=[Mono.Cecil.DefaultAssemblyResolver]::new();$resolver.AddSearchDirectory([IO.Path]::GetDirectoryName($sourcePath))
$parameters=[Mono.Cecil.ReaderParameters]::new();$parameters.AssemblyResolver=$resolver
function All-Types($type){$type;foreach($nested in $type.NestedTypes){All-Types $nested}}
function Method-Shape($method){
    $body=if($method.HasBody){($method.Body.Instructions | ForEach-Object { [string]$_.OpCode+':'+[string]$_.Operand }) -join "`n"}else{''}
    return ([string]$method.Attributes)+'|'+([string]$method.ImplAttributes)+'|'+$method.FullName+'|'+$body
}
function Snap-Methods($module){$rows=@{};foreach($type in $module.Types | ForEach-Object { All-Types $_ }){foreach($method in $type.Methods){$rows[$method.FullName]=Method-Shape $method}};return $rows}
function Snap-Fields($module){$rows=@{};foreach($type in $module.Types | ForEach-Object { All-Types $_ }){foreach($field in $type.Fields){$rows[$field.FullName]=([string]$field.Attributes)+'|'+[Convert]::ToBase64String($field.InitialValue)}};return $rows}
$assembly=[Mono.Cecil.AssemblyDefinition]::ReadAssembly($sourcePath,$parameters)
try{
    $before=Snap-Methods $assembly.MainModule;$fields=Snap-Fields $assembly.MainModule
    $endpoint=@($assembly.MainModule.GetType('UnityEngine.Component').Methods | Where-Object { $_.Name -eq 'get_gameObject' -and $_.Parameters.Count -eq 0 })
    if($endpoint.Count -ne 1 -or !$endpoint[0].IsInternalCall -or $endpoint[0].HasBody -or $endpoint[0].ReturnType.FullName -cne 'UnityEngine.GameObject'){throw 'Unexpected Component.gameObject engine endpoint.'}
    $method=$endpoint[0];$identity=$method.FullName
    # AreaEffectView.SpawnFxs and every production patch target remain the
    # actual original game methods. Only this dependency's engine getter is a
    # managed trap, allowing CLR detour compilation without executing Unity.
    $method.ImplAttributes=[Mono.Cecil.MethodImplAttributes]::IL
    $method.Body=[Mono.Cecil.Cil.MethodBody]::new($method)
    $exception=[Mono.Cecil.TypeReference]::new('System','InvalidOperationException',$assembly.MainModule,$assembly.MainModule.TypeSystem.CoreLibrary)
    $constructor=[Mono.Cecil.MethodReference]::new('.ctor',$assembly.MainModule.TypeSystem.Void,$exception);$constructor.HasThis=$true
    $constructor.Parameters.Add([Mono.Cecil.ParameterDefinition]::new($assembly.MainModule.TypeSystem.String))
    $il=$method.Body.GetILProcessor();$il.Append($il.Create([Mono.Cecil.Cil.OpCodes]::Ldstr,'058 standalone scene fixture must not execute Unity Component.gameObject.'));$il.Append($il.Create([Mono.Cecil.Cil.OpCodes]::Newobj,$constructor));$il.Append($il.Create([Mono.Cecil.Cil.OpCodes]::Throw))
    $null=New-Item -ItemType Directory -Path (Split-Path $targetPath -Parent) -Force
    $assembly.Write($targetPath)
    $read=[Mono.Cecil.AssemblyDefinition]::ReadAssembly($targetPath,$parameters)
    try{
        $after=Snap-Methods $read.MainModule;$afterFields=Snap-Fields $read.MainModule
        if($before.Count -ne $after.Count -or $fields.Count -ne $afterFields.Count){throw 'Fixture changed method/field roster.'}
        $changed=@($before.Keys | Where-Object { $before[$_] -cne $after[$_] })
        if($changed.Count -ne 1 -or $changed[0] -cne $identity){throw ('Scene fixture changed other methods: '+($changed -join ', '))}
        foreach($key in $fields.Keys){if($fields[$key] -cne $afterFields[$key]){throw ('Scene fixture changed field '+$key)}}
        if($read.MainModule.GetType('UnityEngine.Component').Methods.Where({$_.FullName -ceq $identity})[0].IsInternalCall){throw 'Expected native endpoint was not isolated.'}
    }finally{$read.Dispose()}
    if((Get-FileHash -LiteralPath $sourcePath).Hash -cne $beforeSHA){throw 'Original game dependency changed.'}
    [ordered]@{passed=$true;source=$sourcePath;sourceSHA256=$beforeSHA;fixture=$targetPath;fixtureSHA256=(Get-FileHash -LiteralPath $targetPath).Hash;changedMethod=$identity;changedMethodCount=1;otherMethodBodiesAndFieldsExact=$true;methodCount=$before.Count;fieldCount=$fields.Count;productionAssemblyAndPatchTargetsUnchanged=$true;scope='Only a Unity engine getter becomes a throwing standalone CLR endpoint. This is a test dependency, never an installed game file or package member.'}|ConvertTo-Json -Depth 8|Set-Content -LiteralPath $auditFile -Encoding utf8
    Write-Output 'PASS058: one Unity engine getter isolated; all other dependency methods/fields and every production patch target unchanged.'
}finally{$assembly.Dispose();$resolver.Dispose()}

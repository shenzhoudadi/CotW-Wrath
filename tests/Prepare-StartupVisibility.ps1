# Test fixture only: never distribute the generated game assembly or install it.
param([Parameter(Mandatory=$true)][string]$NativeAssembly,
      [Parameter(Mandatory=$true)][string]$PublicizedAssembly,
      [Parameter(Mandatory=$true)][string]$CecilAssembly,
      [Parameter(Mandatory=$true)][string]$OutputAssembly)
$ErrorActionPreference='Stop'
if([IO.Path]::GetFullPath($OutputAssembly) -in @([IO.Path]::GetFullPath($NativeAssembly),[IO.Path]::GetFullPath($PublicizedAssembly))) {throw 'Fixture output must be a separate file.'}
$null=[Reflection.Assembly]::LoadFrom([IO.Path]::GetFullPath($CecilAssembly))
$resolver=[Mono.Cecil.DefaultAssemblyResolver]::new()
$resolver.AddSearchDirectory([IO.Path]::GetDirectoryName([IO.Path]::GetFullPath($NativeAssembly)))
$parameters=[Mono.Cecil.ReaderParameters]::new();$parameters.AssemblyResolver=$resolver
$native=[Mono.Cecil.AssemblyDefinition]::ReadAssembly([IO.Path]::GetFullPath($NativeAssembly),$parameters)
$fixture=[Mono.Cecil.AssemblyDefinition]::ReadAssembly([IO.Path]::GetFullPath($PublicizedAssembly),$parameters)
try {
    # Publicizer promotes this protected virtual hook to public. CLR rejects the
    # correctly protected CotW override against that test-only API. Preserve its
    # actual game visibility; Medium's public overrides may legally widen it.
    $typeName='Kingmaker.EntitySystem.EntityFactComponentDelegate`2'
    $original=@($native.MainModule.GetType($typeName).Methods | Where-Object Name -eq 'OnTurnOn')
    $target=@($fixture.MainModule.GetType($typeName).Methods | Where-Object Name -eq 'OnTurnOn')
    if($original.Count -ne 1 -or $target.Count -ne 1 -or !$original[0].IsFamily -or !$target[0].IsPublic) {throw 'Unexpected game/publicizer API'}
    # Preserve native access for every virtual member. CotW has protected
    # overrides beyond OnTurnOn; promoting their bases makes CLR reject the
    # unchanged production types. Medium's public overrides may legally widen.
    function AllTypes($Roots){foreach($type in $Roots){$type;AllTypes $type.NestedTypes}}
    $nativeVirtual=@{}
    foreach($type in (AllTypes $native.MainModule.Types)){
        foreach($method in $type.Methods){if($method.IsVirtual){$nativeVirtual[$method.FullName]=$method.Attributes -band [Mono.Cecil.MethodAttributes]::MemberAccessMask}}
    }
    $restored=0
    foreach($type in (AllTypes $fixture.MainModule.Types)){
        foreach($method in $type.Methods){
            if($method.IsVirtual -and $nativeVirtual.ContainsKey($method.FullName)){
                $access=$nativeVirtual[$method.FullName]
                if(($method.Attributes -band [Mono.Cecil.MethodAttributes]::MemberAccessMask) -ne $access){
                    $method.Attributes=($method.Attributes -band (-bnot [int][Mono.Cecil.MethodAttributes]::MemberAccessMask)) -bor $access
                    $restored++
                }
            }
        }
    }
    $fixture.Write([IO.Path]::GetFullPath($OutputAssembly))
    Write-Output "PASS: separate startup fixture restores native visibility for $restored virtual members; game/build references unchanged."
} finally {$native.Dispose();$fixture.Dispose();$resolver.Dispose()}

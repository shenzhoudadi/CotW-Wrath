#requires -Version 7.0
param(
    [Parameter(Mandatory)][string] $WrathPath,
    [Parameter(Mandatory)][string] $OutputPath
)

$ErrorActionPreference = 'Stop'
$folder = 'Equipment/Armor/Enchantments/'
$members = @(1..5 | ForEach-Object { "${folder}ArmorEnhancementBonus$_.jbp" })
$properties = @('Fortification25', 'Fortification50', 'Fortification75')
foreach ($energy in @('Acid', 'Cold', 'Electricity', 'Fire')) {
    foreach ($amount in @(10, 20, 30)) {
        $properties += "${energy}Resistance${amount}"
    }
}
foreach ($property in $properties) {
    $members += @("${folder}${property}Enchant.jbp", "${folder}${property}Feature.jbp")
}
$members += @('Equipment/Armor/Items/Standard/ChainshirtStandard.jbp', 'Equipment/Armor/Types/ChainshirtType.jbp')

$zip = [IO.Compression.ZipFile]::OpenRead((Join-Path $WrathPath 'blueprints.zip'))
try {
    $rows = @(foreach ($member in $members) {
        $entry = $zip.GetEntry($member)
        if ($null -eq $entry) { throw "Missing Aegis archive entry $member" }
        $reader = [IO.StreamReader]::new($entry.Open())
        try {
            @{
                name = [IO.Path]::GetFileNameWithoutExtension($member)
                member = $member
                blueprint = ($reader.ReadToEnd() | ConvertFrom-Json -AsHashtable -Depth 100)
            }
        }
        finally { $reader.Dispose() }
    })
    ConvertTo-Json -InputObject $rows -Depth 100 | Set-Content -LiteralPath $OutputPath -Encoding utf8NoBOM
}
finally { $zip.Dispose() }

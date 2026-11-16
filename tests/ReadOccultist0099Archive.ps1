#requires -Version 7.0
param(
    [Parameter(Mandatory)][string] $WrathPath,
    [Parameter(Mandatory)][string] $OutputPath,
    [ValidateSet('Aegis', 'Divination')][string] $Group = 'Aegis'
)

$ErrorActionPreference = 'Stop'
if ($Group -eq 'Aegis') {
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
}
else {
    $members = @(
        'Classes/Wizard/Divination/DivinationSchoolBaseAbility.jbp',
        'Classes/Wizard/Divination/DivinationSchoolBaseBuff.jbp',
        'Spells/Level0/GuidanceBuff.jbp',
        'Spells/Level1/TrueStrike.jbp',
        'Spells/Level2/FindTraps.jbp',
        'Spells/Level2/SeeInvisibility.jbp',
        'Spells/Level2/SeeInvisibilitytBuff.jbp',
        'Spells/Level6/TrueSeeingCast.jbp',
        'Spells/Level8/PredictionOfFailure.jbp',
        'Units/Monsters/Will-o-Wisp/CR6_WillOWispStandart.jbp'
    )
}

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

#requires -Version 7.0
param(
    [Parameter(Mandatory)][string] $WrathPath,
    [Parameter(Mandatory)][string] $OutputPath,
    [ValidateSet('Aegis', 'Divination', 'Illusion')][string] $Group = 'Aegis'
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
elseif ($Group -eq 'Divination') {
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
else {
    $members = @(
        'Classes/Wizard/Illusion/IllusionSchoolBaseAbility.jbp',
        'Spells/Level0/DazzledBuff.jbp',
        'Spells/Level1/Vanish.jbp',
        'Spells/Level2/BlindnessBuff.jbp',
        'Spells/Level2/Blur.jbp',
        'Spells/Level2/Invisibility.jbp',
        'Spells/Level2/MirrorImage.jbp',
        'Spells/Level3/Displacement.jbp',
        'Spells/Level3/RemoveCurse.jbp',
        'Spells/Level4/Fear.jbp',
        'Spells/Level4/InvisibilityGreater.jbp',
        'Spells/Level4/InvisibilityGreaterBuff.jbp',
        'Spells/Level5/PhantasmalWeb.jbp',
        'Spells/Level6/PhantasmalPutrefaction.jbp',
        'Weapons/Types/Special/TouchItem.jbp',
        'Classes/Wizard/WizardSpellList.jbp'
    )
    $roman = @('I', 'II', 'III', 'IV', 'V', 'VI', 'VII', 'VIII', 'IX')
    for ($circle = 1; $circle -le 9; $circle++) {
        $stem = "Spells/Level$circle/SummonMonster$($roman[$circle - 1])"
        $members += "${stem}Single.jbp"
        if ($circle -ge 2) { $members += @("${stem}Base.jbp", "${stem}d3.jbp") }
        if ($circle -ge 3) { $members += "${stem}d4plus1.jbp" }
    }
    $elementals = [ordered]@{ Small=2; Medium=4; Large=5; Huge=6; Greater=7; Elder=8 }
    foreach ($size in $elementals.Keys) {
        $stem = "Spells/Level$($elementals[$size])/SummonElemental$size"
        foreach ($suffix in @('Base', 'Air', 'Earth', 'Fire', 'Water')) { $members += "$stem$suffix.jbp" }
    }
}

$zip = [IO.Compression.ZipFile]::OpenRead((Join-Path $WrathPath 'blueprints.zip'))
try {
    $rows = @(foreach ($member in $members) {
        $entry = $zip.GetEntry($member)
        if ($null -eq $entry) { throw "Missing $Group archive entry $member" }
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

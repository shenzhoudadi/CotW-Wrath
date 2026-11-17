#requires -Version 7.0
param(
    [Parameter(Mandatory)][string] $WrathPath,
    [Parameter(Mandatory)][string] $OutputPath,
    [ValidateSet('Aegis', 'Conjuration', 'Divination', 'Evocation', 'Illusion', 'Necromancy', 'Transmutation')][string] $Group = 'Aegis'
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
elseif ($Group -eq 'Conjuration') {
    $members = @(
        'Classes/Paladin/LayOnHands/LayOnHandsOthers.jbp',
        'Spells/Level3/NeutralizePoison.jbp', 'Spells/Level3/RemoveDisease.jbp',
        'Spells/Level4/DimensionDoorCasterOnly.jbp',
        'Feats/AugmentSummoning/AugmentSummoning.jbp',
        'Traits/Races/Common/UndeadType.jbp', 'Traits/Races/Common/ConstructType.jbp',
        'CreatureAbilities/Subtypes/SubtypeElemental.jbp',
        # The existing CotW fog and Blade Tutor builders read these native icon donors.
        'Classes/Kineticist/Infusions/FormInfusions/CloudInfusion.jbp',
        'Spells/Level4/HolySword.jbp'
    )
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
elseif ($Group -eq 'Necromancy') {
    $members = @(
        'Spells/Level1/CauseFear.jbp',
        'Spells/Level2/FalseLife.jbp', 'Spells/Level2/FalseLifeBuff.jbp',
        'Spells/Level3/AnimateDeadLesser.jbp',
        'Buffs/Frightened.jbp', 'Buffs/Shaken.jbp', 'Buffs/Sickened.jbp',
        'Weapons/Types/Special/TouchItem.jbp',
        'Traits/Races/Common/UndeadType.jbp', 'Traits/Races/Common/ConstructType.jbp',
        'Traits/Races/Common/AberrationType.jbp', 'Traits/Races/Common/VerminType.jbp',
        'Traits/Races/Common/PlantType.jbp', 'CreatureAbilities/Subtypes/SubtypeElemental.jbp',
        'Units/NPC/Commoners/Skeleton/SummonedSkeletonChampion.jbp',
        'Classes/Inquisitor/Archetypes/TacticalLeader/TacticalLeaderFeatShareBuff.jbp',
        'Classes/Rogue/RogueTalentsAdvanced/Familiar.jbp',
        'DLC/DLC5/Dlc5RewardCampaign.jbp',
        'Items/Scrolls/Level1/ScrollOfCauseFear.jbp'
    )
    $familiars = @('Cat', 'Centipede', 'Chicken', 'Dog', 'Duck', 'Hare', 'Jerboa', 'Lizard', 'Monkey', 'Rabbit', 'Rat', 'Tarantula', 'Viper')
    foreach ($familiar in $familiars) {
        $members += @("Classes/Wizard/ArcaneBond/FamiliarBond/${familiar}FamiliarBondFeature.jbp",
            "Classes/Wizard/ArcaneBond/FamiliarBond/${familiar}FamiliarAbility.jbp",
            "Classes/Wizard/ArcaneBond/FamiliarBond/${familiar}FamiliarBuff.jbp")
    }
    # The creature type features grant their own immunity facts; activating a type
    # on a fixture corpse needs the whole native chain present.
    $members += @(
        'CreatureAbilities/Immunities/ImmunityToCritical.jbp', 'CreatureAbilities/Immunities/ImmunityToStun.jbp',
        'CreatureAbilities/Immunities/ImmunityToBleed.jbp', 'CreatureAbilities/Immunities/ImmunityToPoison.jbp',
        'CreatureAbilities/Immunities/ImmunityToParalysis.jbp', 'CreatureAbilities/Immunities/ImmunityToSleep.jbp',
        'CreatureAbilities/Immunities/ImmunityToPolymorph.jbp', 'CreatureAbilities/Immunities/ImmunityToMindAffecting.jbp',
        'CreatureAbilities/Immunities/VerminImmunities.jbp', 'Traits/Races/Common/ConstructImmunities.jbp',
        'CreatureAbilities/Immunities/ImmunityToAbilityDamageConstructs.jbp', 'CreatureAbilities/Bleed/BleedImmunity.jbp',
        'CreatureAbilities/Immunities/ImmunityToDeathEffects.jbp', 'CreatureAbilities/Immunities/ImmunityToCompulsion.jbp',
        'CreatureAbilities/Immunities/ImmunityToCharm.jbp', 'CreatureAbilities/Immunities/ImmunityToFear.jbp',
        'CreatureAbilities/Immunities/UndeadImmunities.jbp', 'CreatureAbilities/NegativeEnergyAffinity.jbp'
    )
}
elseif ($Group -eq 'Transmutation') {
    $members = @(
        'Spells/Level1/EnlargePerson.jbp', 'Spells/Level1/EnlargePersonBuff.jbp',
        'Spells/Level1/ReducePerson.jbp', 'Spells/Level1/ReducePersonBuff.jbp',
        'Spells/Level1/ExpeditiousRetreat.jbp', 'Spells/Level1/ExpeditiousRetreatBuff.jbp',
        'Spells/Level1/HurricaneBow.jbp', 'Spells/Level1/LeadBlades.jbp', 'Spells/Level1/MagicWeapon/MagicWeapon.jbp',
        'Spells/Level2/EffortlessArmor.jbp', 'Spells/DLC6NewSpells/KeenEdge.jbp',
        'Spells/Level3/Haste.jbp', 'Spells/Level3/HasteBuff.jbp', 'Spells/Level3/HasteBuffEffect.jbp', 'Spells/Level3/Slow.jbp',
        'Spells/Level3/SlowBuff.jbp',
        'Spells/Level3/MagicalVestment.jbp', 'Spells/Level3/MagicWeaponGreater/MagicWeaponGreater.jbp',
        'Spells/Level4/Echolocation.jbp', 'Spells/Level6/Disintegrate.jbp', 'Spells/Level6/Transformation.jbp',
        'DLC/DLC6/Dlc6Reward.jbp',
        'CreatureAbilities/Subtypes/SubtypeElemental.jbp',
        'Weapons/Items/Standard/SimpleOneHanded/StandardClub.jbp', 'Weapons/Types/SimpleOneHanded/Club.jbp',
        'Weapons/Items/Standard/MartialOneHanded/StandardLongsword.jbp', 'Weapons/Types/MartialOneHanded/Longsword.jbp'
    )
    $enchantments = @('Flaming', 'Frost', 'Shock', 'GhostTouch', 'Keen', 'CruelEnchantment', 'ViciousEnchantment',
        'Disruption', 'Holy', 'Unholy', 'Axiomatic', 'Anarchic', 'Speed', 'BrilliantEnergy', 'BaneEverything', 'Vorpal',
        'ColdIronWeaponEnchantment', 'MithralWeaponEnchantment', 'AdamantineWeaponEnchantment')
    $enchantments += @(1..5 | ForEach-Object { "TemporaryEnhancement$_" })
    $members += @($enchantments | ForEach-Object { "Weapons/Enchantments/$_.jbp" })
}
elseif ($Group -eq 'Evocation') {
    $members = @(
        'Classes/Sorcerer/ElementalBloodline/Earth/ScorchingRayAcid.jbp',
        'Classes/Sorcerer/ElementalBloodline/Water/ScorchingRayCold.jbp',
        'Classes/Sorcerer/ElementalBloodline/Air/ScorchingRayElecricity.jbp',
        'Spells/Level2/ScorchingRay.jbp',
        'Spells/Level1/Snowball.jbp',
        'Spells/Level3/Fireball.jbp',
        'Classes/Magus/ArcanePool/ArcaneWeaponBrilliantEnergyBuff.jbp',
        'Spells/Level1/FaerieFireBuff.jbp',
        'Spells/Level0/MageLight.jbp',
        'Spells/Level0/MageLightBuff.jbp',
        'Spells/Level2/BlindnessBuff.jbp',
        'Spells/Level2/Blindness.jbp',
        'Classes/Cleric/Domains/Fire/FireDomainBaseAbility.jbp',
        'Classes/Kineticist/GatherPower/GatherPowerModeHigh.jbp',
        'Classes/Kineticist/Infusions/FormInfusions/KineticBladeEnchantments/PlasmaKineticBladeEnchantment.jbp',
        'Classes/Wizard/Evocation/ElementalWallAcidArea.jbp',
        'Classes/Wizard/Evocation/ElementalWallAcidAbility.jbp',
        'Classes/Wizard/Evocation/ElementalWallColdArea.jbp',
        'Classes/Wizard/Evocation/ElementalWallColdAbility.jbp',
        'Classes/Wizard/Evocation/ElementalWallElectricityArea.jbp',
        'Classes/Wizard/Evocation/ElementalWallElectricityAbility.jbp',
        'Classes/Wizard/Evocation/ElementalWallFireArea.jbp',
        'Classes/Wizard/Evocation/ElementalWallFireAbility.jbp',
        'Weapons/Items/Standard/ExoticLight/StandardKama.jbp',
        'Weapons/Items/Standard/ExoticLight/StandardNunchaku.jbp',
        'Weapons/Items/Standard/ExoticLight/StandardSai.jbp',
        'Weapons/Items/Standard/ExoticOneHanded/StandardBastardSword.jbp',
        'Weapons/Items/Standard/ExoticOneHanded/StandardDuelingSword.jbp',
        'Weapons/Items/Standard/ExoticOneHanded/StandardDwarvenWaraxe.jbp',
        'Weapons/Items/Standard/ExoticOneHanded/StandardEstoc.jbp',
        'Weapons/Items/Standard/ExoticOneHanded/StandardFalcata.jbp',
        'Weapons/Items/Standard/ExoticOneHanded/StandardTongi.jbp',
        'Weapons/Items/Standard/ExoticRanged/StandardSlingStaff.jbp',
        'Weapons/Items/Standard/ExoticTwoHanded/StandardDoubleAxe.jbp',
        'Weapons/Items/Standard/ExoticTwoHanded/StandardDoubleSword.jbp',
        'Weapons/Items/Standard/ExoticTwoHanded/StandardDwarvenUrgrosh.jbp',
        'Weapons/Items/Standard/ExoticTwoHanded/StandardElvenCurvedBlade.jbp',
        'Weapons/Items/Standard/ExoticTwoHanded/StandardFauchard.jbp',
        'Weapons/Items/Standard/ExoticTwoHanded/StandardGnomeHookedHammer.jbp',
        'Weapons/Items/Standard/MartialLight/StandardHandaxe.jbp',
        'Weapons/Items/Standard/MartialLight/StandardKukri.jbp',
        'Weapons/Items/Standard/MartialLight/StandardLightHammer.jbp',
        'Weapons/Items/Standard/MartialLight/StandardLightPick.jbp',
        'Weapons/Items/Standard/MartialLight/StandardShortsword.jbp',
        'Weapons/Items/Standard/MartialLight/StandardSpikedLightShield.jbp',
        'Weapons/Items/Standard/MartialLight/StandardStarknife.jbp',
        'Weapons/Items/Standard/MartialLight/StandardThrowingAxe.jbp',
        'Weapons/Items/Standard/MartialLight/StandardWeaponLightShield.jbp',
        'Weapons/Items/Standard/MartialOneHanded/StandardBattleaxe.jbp',
        'Weapons/Items/Standard/MartialOneHanded/StandardFlail.jbp',
        'Weapons/Items/Standard/MartialOneHanded/StandardHeavyPick.jbp',
        'Weapons/Items/Standard/MartialOneHanded/StandardLongsword.jbp',
        'Weapons/Items/Standard/MartialOneHanded/StandardRapier.jbp',
        'Weapons/Items/Standard/MartialOneHanded/StandardScimitar.jbp',
        'Weapons/Items/Standard/MartialOneHanded/StandardSpikedHeavyShield.jbp',
        'Weapons/Items/Standard/MartialOneHanded/StandardTrident.jbp',
        'Weapons/Items/Standard/MartialOneHanded/StandardWarhammer.jbp',
        'Weapons/Items/Standard/MartialOneHanded/StandardWeaponHeavyShield.jbp',
        'Weapons/Items/Standard/MartialRanged/StandardLongbow.jbp',
        'Weapons/Items/Standard/MartialRanged/StandardShortbow.jbp',
        'Weapons/Items/Standard/MartialTwoHanded/StandardBardiche.jbp',
        'Weapons/Items/Standard/MartialTwoHanded/StandardEarthBreaker.jbp',
        'Weapons/Items/Standard/MartialTwoHanded/StandardFalchion.jbp',
        'Weapons/Items/Standard/MartialTwoHanded/StandardGlaive.jbp',
        'Weapons/Items/Standard/MartialTwoHanded/StandardGreataxe.jbp',
        'Weapons/Items/Standard/MartialTwoHanded/StandardGreatsword.jbp',
        'Weapons/Items/Standard/MartialTwoHanded/StandardHeavyFlail.jbp',
        'Weapons/Items/Standard/MartialTwoHanded/StandardScythe.jbp',
        'Weapons/Items/Standard/NaturalWeapon/Unarmed1d3.jbp',
        'Weapons/Items/Standard/SimpleLight/StandardDagger.jbp',
        'Weapons/Items/Standard/SimpleLight/StandardLightMace.jbp',
        'Weapons/Items/Standard/SimpleLight/StandardPunchingDagger.jbp',
        'Weapons/Items/Standard/SimpleLight/StandardSickle.jbp',
        'Weapons/Items/Standard/SimpleOneHanded/StandardClub.jbp',
        'Weapons/Items/Standard/SimpleOneHanded/StandardHeavyMace.jbp',
        'Weapons/Items/Standard/SimpleOneHanded/StandardShortspear.jbp',
        'Weapons/Items/Standard/SimpleRanged/StandardDart.jbp',
        'Weapons/Items/Standard/SimpleRanged/StandardHeavyCrossbow.jbp',
        'Weapons/Items/Standard/SimpleRanged/StandardJavelin.jbp',
        'Weapons/Items/Standard/SimpleRanged/StandardLightCrossbow.jbp',
        'Weapons/Items/Standard/SimpleTwoHanded/StandardGreatclub.jbp',
        'Weapons/Items/Standard/SimpleTwoHanded/StandardLongspear.jbp',
        'Weapons/Items/Standard/SimpleTwoHanded/StandardQuarterstaff.jbp',
        'Weapons/Items/Standard/SimpleTwoHanded/StandardSpear.jbp',
        'Weapons/Types/ExoticLight/Kama.jbp',
        'Weapons/Types/ExoticLight/Nunchaku.jbp',
        'Weapons/Types/ExoticLight/Sai.jbp',
        'Weapons/Types/ExoticOneHanded/BastardSword.jbp',
        'Weapons/Types/ExoticOneHanded/DuelingSword.jbp',
        'Weapons/Types/ExoticOneHanded/DwarvenWaraxe.jbp',
        'Weapons/Types/ExoticOneHanded/Estoc.jbp',
        'Weapons/Types/ExoticOneHanded/Falcata.jbp',
        'Weapons/Types/ExoticOneHanded/Tongi.jbp',
        'Weapons/Types/ExoticRanged/SlingStaff.jbp',
        'Weapons/Types/ExoticTwoHanded/DoubleAxe.jbp',
        'Weapons/Types/ExoticTwoHanded/DoubleSword.jbp',
        'Weapons/Types/ExoticTwoHanded/DwarvenUrgrosh.jbp',
        'Weapons/Types/ExoticTwoHanded/ElvenCurvedBlade.jbp',
        'Weapons/Types/ExoticTwoHanded/Fauchard.jbp',
        'Weapons/Types/ExoticTwoHanded/GnomeHookedHammerHead.jbp',
        'Weapons/Types/MartialLight/Handaxe.jbp',
        'Weapons/Types/MartialLight/Kukri.jbp',
        'Weapons/Types/MartialLight/LightHammer.jbp',
        'Weapons/Types/MartialLight/LightPick.jbp',
        'Weapons/Types/MartialLight/Shortsword.jbp',
        'Weapons/Types/MartialLight/SpikedLightShield.jbp',
        'Weapons/Types/MartialLight/Starknife.jbp',
        'Weapons/Types/MartialLight/ThrowingAxe.jbp',
        'Weapons/Types/MartialLight/WeaponLightShield.jbp',
        'Weapons/Types/MartialOneHanded/Battleaxe.jbp',
        'Weapons/Types/MartialOneHanded/Flail.jbp',
        'Weapons/Types/MartialOneHanded/HeavyPick.jbp',
        'Weapons/Types/MartialOneHanded/Longsword.jbp',
        'Weapons/Types/MartialOneHanded/Rapier.jbp',
        'Weapons/Types/MartialOneHanded/Scimitar.jbp',
        'Weapons/Types/MartialOneHanded/SpikedHeavyShield.jbp',
        'Weapons/Types/MartialOneHanded/Warhammer.jbp',
        'Weapons/Types/MartialOneHanded/WeaponHeavyShield.jbp',
        'Weapons/Types/MartialRanged/Longbow.jbp',
        'Weapons/Types/MartialRanged/Shortbow.jbp',
        'Weapons/Types/MartialTwoHanded/Bardiche.jbp',
        'Weapons/Types/MartialTwoHanded/EarthBreaker.jbp',
        'Weapons/Types/MartialTwoHanded/Falchion.jbp',
        'Weapons/Types/MartialTwoHanded/Glaive.jbp',
        'Weapons/Types/MartialTwoHanded/Greataxe.jbp',
        'Weapons/Types/MartialTwoHanded/Greatsword.jbp',
        'Weapons/Types/MartialTwoHanded/HeavyFlail.jbp',
        'Weapons/Types/MartialTwoHanded/Scythe.jbp',
        'Weapons/Types/SimpleLight/Dagger.jbp',
        'Weapons/Types/SimpleLight/LightMace.jbp',
        'Weapons/Types/SimpleLight/PunchingDagger.jbp',
        'Weapons/Types/SimpleLight/Sickle.jbp',
        'Weapons/Types/SimpleOneHanded/Club.jbp',
        'Weapons/Types/SimpleOneHanded/HeavyMace.jbp',
        'Weapons/Types/SimpleOneHanded/Shortspear.jbp',
        'Weapons/Types/SimpleRanged/Dart.jbp',
        'Weapons/Types/SimpleRanged/HeavyCrossbow.jbp',
        'Weapons/Types/SimpleRanged/Javelin.jbp',
        'Weapons/Types/SimpleRanged/LightCrossbow.jbp',
        'Weapons/Types/SimpleTwoHanded/Greatclub.jbp',
        'Weapons/Types/SimpleTwoHanded/Longspear.jbp',
        'Weapons/Types/SimpleTwoHanded/Quarterstaff.jbp',
        'Weapons/Types/SimpleTwoHanded/Spear.jbp',
        'Weapons/Types/SimpleTwoHanded/Trident.jbp',
        'Weapons/Types/Special/Unarmed.jbp'
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

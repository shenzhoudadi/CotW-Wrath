"""COTW-0109 B3 fixture builder: the minimal reviewed native row set the
Iroran Paladin registration actually loads.

Rows already present in the reviewed spirit-guide-0060-native-ext.json are
copied verbatim; the sixteen rows that archive lacks (weapon bond switch
ability/duration buff/additional use, Flaming/Keen choices, five temporary
enhancement enchantments and the six native unarmed fists) are extracted from
the installed game's blueprints.zip by exact AssetId and wrapped in the same
{name, blueprint:{AssetId, Data}} shape LoadNativeRows consumes.
"""
import json
from pathlib import Path
import zipfile

root = Path('D:/CodexProjects/CotW-Wrath')
game = Path('D:/steam/steamapps/common/Pathfinder Second Adventure')
src = root / 'work/prototype-build/spirit-guide-0060-native-ext.json'
out = root / 'outputs/work/prototype-build/iroran-0109-native.json'

# Every native identity IroranPaladin0109.Register touches, in dependency order.
SHARED = [
    'bfa11238e7ae3544bbeb4d0b92e897ec',  # PaladinClass
    'fd325cbba872e5f40b618970678db002',  # PaladinProgression
    'e8f21e5b58e0569468e420ebea456124',  # MonkClass
    '23a77a5985de08349820429ce1b5a234',  # IroriFeature
    '59e7a76987fe3b547b9cce045f4db3e4',  # DeitySelection (all deities)
    '5868fc82eb11a4244926363983897279',  # ScaledFistArchetype
    '64acb179cc6a4f19bb3513d094b28d02',  # ScaledFistACBonusBuff
    'ca948bb4ce1a2014fbf4d8d44b553074',  # ScaledFistExtraAttack
    'ae98ab7bda409ef4bb39149a212d6732',  # ScaledFistPowerFeature
    '7d002c1025fbfe2458f1509bf7a89ce1',  # ScaledFistPowerResource (shared ki)
    'c3fbeb2ffebaaa64aa38ce7a0bb18fb0',  # MonkUnarmedStrike
    '7812ad3672a4b9a4fb894ea402095167',  # ImprovedUnarmedStrike
    '6d3728d4e9c9898458fe5e9532951132',  # LightArmorProficiency
    '203992ef5b35c864390b4e4a1e200629',  # MartialWeaponProficiency
    'e70ecf1ed95ca2f40b754f1adb22bbdd',  # SimpleWeaponProficiency
    '1188005ee3160f84f8bed8b19a7d46cf',  # KiStrikeMagic
    '7b657938fde78b14cae10fc0d3dcb991',  # KiStrikeColdIronSilver
    '34439e527a8f5fb4588024e71960dd42',  # KiStrikeLawful
    'ddc10a3463bd4d54dbcbe993655cf64e',  # KiStrikeAdamantine
    'b10ff88c03308b649b50c31611c2fefb',  # PaladinProficiencies
    '3a6db57fce75b0244a6a5819528ddf26',  # SmiteEvilFeature
    '7bb9eb2042e67bf489ccd1374423cdec',  # SmiteEvilAbility
    '0f5c99ffb9c084545bbbe960b825d137',  # SmiteEvilAdditionalUse
    'e45ab30f49215054e83b4ea12165409f',  # AuraOfCourageFeature
    'c96380f6dcac83c45acdb698ae70ffc4',  # AuraOfCourageBuff
    'cb6d55dda5ab906459d18a435994a760',  # ChannelEnergyPaladinFeature
    'ad7dc4eba7bf92f4aba23f716d7a9ba6',  # PaladinDivineBondSelection
    '9f13fdd044ccb8a439f27417481cb00e',  # AuraOfJusticeFeature
    'c8db0772b7059ec4eabe55b7e0e79824',  # WeaponBondPlus2
    'd2f45a2034d4f7643ba1a450bc5c4c06',  # WeaponBondPlus3
    '6d73f49b602e29a43a6faa2ea1e4a425',  # WeaponBondPlus4
    'f17c3ba33bb44d44782cb3851d823011',  # WeaponBondPlus5
    'b936ee90c070edb46bd76025dc1c5936',  # WeaponBondPlus6
]
# Only in the installed game archive.
EXTRACT = {
    '7ced0efa297bd5142ab749f6e33b112b': 'Classes/Paladin/AuraOfCourage/AuraOfCourageArea.jbp',
    '7ff088ab58c69854b82ea95c2b0e35b4': 'Classes/Paladin/WeaponBond/WeaponBondSwitchAbility.jbp',
    'bf570774501886f47b395a4bfe75eeb2': 'Classes/Paladin/WeaponBond/WeaponBondDurationBuff.jbp',
    '5a64de5435667da4eae2e4c95ec87917': 'Classes/Paladin/WeaponBond/WeaponBondAdditionalUse.jbp',
    '7902941ef70a0dc44bcfc174d6193386': 'Classes/Paladin/WeaponBond/WeaponBondFlamingChoice.jbp',
    '27d76f1afda08a64d897cc81201b5218': 'Classes/Paladin/WeaponBond/WeaponBondKeenChoice.jbp',
    'd704f90f54f813043a525f304f6c0050': 'Weapons/Enchantments/TemporaryEnhancement1.jbp',
    '9e9bab3020ec5f64499e007880b37e52': 'Weapons/Enchantments/TemporaryEnhancement2.jbp',
    'd072b841ba0668846adeb007f623bd6c': 'Weapons/Enchantments/TemporaryEnhancement3.jbp',
    '6a6a0901d799ceb49b33d4851ff72132': 'Weapons/Enchantments/TemporaryEnhancement4.jbp',
    '746ee366e50611146821d61e391edf16': 'Weapons/Enchantments/TemporaryEnhancement5.jbp',
    'f60c5a820b69fb243a4cce5d1d07d06e': 'Weapons/Items/Standard/NaturalWeapon/Unarmed1d6.jbp',
    '9e87f72f8069310469d4431e925f035a': 'Weapons/Items/Standard/NaturalWeapon/Unarmed1d8.jbp',
    'ee31a19c8aa965f46a90ae02d59d4af7': 'Weapons/Items/Standard/NaturalWeapon/Unarmed1d10.jbp',
    '889e2927a67bd724ea1c203fe1b8b652': 'Weapons/Items/Standard/NaturalWeapon/Unarmed2d6.jbp',
    '84af5e0b4f5f251449aac2b530fab3d0': 'Weapons/Items/Standard/NaturalWeapon/Unarmed2d8.jbp',
    '7afc18ecb2cd96541b5cb8395f40d48c': 'Weapons/Items/Standard/NaturalWeapon/Unarmed2d10.jbp',
}

shared = json.loads(src.read_text(encoding='utf-8'))['requiredNative']
by_guid = {r['blueprint']['AssetId']: r for r in shared}
rows = []
for guid in SHARED:
    if guid not in by_guid:
        raise SystemExit('missing in 0060 archive: %s' % guid)
    rows.append(by_guid[guid])

z = zipfile.ZipFile(game / 'blueprints.zip')
for guid, path in EXTRACT.items():
    raw = z.read(path)
    data = json.loads(raw.decode('utf-8'))
    if data.get('AssetId') != guid:
        raise SystemExit('AssetId mismatch at %s: %s' % (path, data.get('AssetId')))
    rows.append({'name': Path(path).stem, 'blueprint': {'AssetId': guid, 'Data': data['Data']}})

out.write_text(json.dumps({'requiredNative': rows}, ensure_ascii=False, indent=1) + '\n', encoding='utf-8')
print('rows=%d shared=%d extracted=%d -> %s' % (len(rows), len(SHARED), len(EXTRACT), out))

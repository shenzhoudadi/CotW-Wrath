from pathlib import Path
import re, json, zipfile, hashlib, sys

module = Path(__file__).resolve().parents[1]
game = Path(sys.argv[1]).resolve()
output = Path(sys.argv[2]).resolve()
needed = set()
sources = [f for f in module.rglob('*.cs') if not {'tests', 'obj', 'bin', 'artifacts'}.intersection(f.relative_to(module).parts)]
# Include every audited parent combat pool and the three full-list anchors.
sources.append(module.parents[1] / 'src' / 'CombatFeatPoolRepair.cs')
for source in sources:
    text = re.sub(r'\[TypeId\("[0-9a-fA-F-]+"\)\]', '', source.read_text('utf-8-sig'))
    needed.update(x.lower() for x in re.findall(r'"([0-9a-fA-F]{32})"', text))
allids, raw, rows = [], {}, {}
archive = game / 'blueprints.zip'
with zipfile.ZipFile(archive) as z:
    for name in z.namelist():
        if not name.endswith('.jbp'):
            continue
        data = z.read(name)
        identity = re.search(rb'"AssetId"\s*:\s*"([0-9a-f]{32})"', data[:500])
        if not identity:
            continue
        guid = identity[1].decode()
        allids.append(guid)
        raw[guid] = name, data
        if guid in needed or b'BlueprintRoot"' in data[:300]:
            rows[guid] = {'name': Path(name).stem, 'path': name, 'sha256': hashlib.sha256(data).hexdigest().upper(), 'blueprint': json.loads(data)}
keys = {'m_Type', 'm_WeaponComponent', 'm_ArmorComponent', 'm_CharacterClass', 'm_Spellbook', 'm_Progression', 'm_SpellsPerDay', 'm_SpellsKnown', 'm_ParentClass', 'm_MythicFeatSelection'}
def links(obj):
    if isinstance(obj, dict):
        for key, value in obj.items():
            if key in keys and isinstance(value, str) and value.startswith('!bp_'):
                yield value[4:]
            elif isinstance(value, (dict, list)):
                yield from links(value)
    elif isinstance(obj, list):
        for value in obj:
            yield from links(value)
while True:
    extra = {guid for row in rows.values() for guid in links(row['blueprint']['Data']) if guid in raw and guid not in rows}
    if not extra:
        break
    for guid in extra:
        name, data = raw[guid]
        rows[guid] = {'name': Path(name).stem, 'path': name, 'sha256': hashlib.sha256(data).hexdigest().upper(), 'blueprint': json.loads(data)}
output.parent.mkdir(parents=True, exist_ok=True)
output.write_text(json.dumps({'requiredNative': list(rows.values()), 'allNativeIds': allids, 'sourceCommit': '5c0490f058f2163e3edfc9b13f007e1e9534f87c', 'ownOrOptionalIds': sorted(needed - set(allids)), 'archive': str(archive)}, ensure_ascii=False), 'utf-8')
print(f'Way of the Shield: {len(rows)} actual dependency rows, {len(allids)} actual native identities')

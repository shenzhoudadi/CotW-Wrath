"""Extract the audited COTW-0070 native classification rows from the installed
game archive and verify every fingerprint against the frozen A-evidence.

Read-only input: <game>/blueprints.zip. Output: a requiredNative row fixture in
the same shape as the reviewed Psychic extension fixtures. No game process is
launched; this is a managed archive read.
"""
import hashlib, json, pathlib, re, sys, zipfile

root = pathlib.Path(__file__).resolve().parents[3]
repo = pathlib.Path(__file__).resolve().parents[1]
game = pathlib.Path(sys.argv[1])
out = pathlib.Path(sys.argv[2])
evidence = json.loads((repo / 'docs/work-packages/COTW-0070/A-evidence.json').read_text(encoding='utf-8-sig'))

wanted = {}
native = evidence['native']
for group in ('broadTypeFacts', 'subtypeFacts', 'outsiderGroupingFacts', 'raceEvidence',
              'excludedClassificationFacts', 'favoriteEnemyDonors'):
    for row in native[group]:
        wanted[row['guid']] = row.get('sha256')
# Icon donors used by the original ability presentation.
wanted['183d5bb91dea3a1489a6db6c9cb64445'] = None  # ShieldOfFaith
wanted['2c38da66e5a599347ac95b3294acbe00'] = None  # TrueStrike

def parse(raw):
    text = raw.decode('utf-8-sig')
    try:
        return json.loads(text)
    except json.JSONDecodeError:
        return json.loads(re.sub(r',\s*([}\]])', r'\1', text))

rows = []
found = {}
with zipfile.ZipFile(game / 'blueprints.zip') as archive:
    for name in archive.namelist():
        if not name.endswith('.jbp'):
            continue
        raw = archive.read(name)
        head = raw[:200].decode('utf-8-sig', errors='replace')
        match = re.search(r'"AssetId":\s*"([0-9a-f]{32})"', head)
        if not match or match.group(1) not in wanted:
            continue
        guid = match.group(1)
        if guid in found:
            continue
        sha = hashlib.sha256(raw).hexdigest()
        expected = wanted[guid]
        if expected is not None and sha.lower() != expected.lower():
            raise SystemExit('Fingerprint mismatch for %s: archive %s != A-evidence %s' % (guid, sha, expected))
        blueprint = parse(raw)
        rows.append({'name': name.rsplit('/', 1)[-1][:-4], 'sha256': sha.upper(), 'blueprint': blueprint})
        found[guid] = sha

missing = set(wanted) - set(found)
if missing:
    raise SystemExit('Missing native rows in the installed archive: %s' % sorted(missing))
out.parent.mkdir(parents=True, exist_ok=True)
out.write_text(json.dumps({'requiredNative': rows}, ensure_ascii=False), encoding='utf-8')
print('Cryptid Scholar 0070: %d native rows extracted, all A-evidence fingerprints verified.' % len(rows))
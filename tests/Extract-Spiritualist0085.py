"""Extract the audited COTW-0085 native donor and spell rows from the installed
game archive and verify every recorded fingerprint against A-spiritualist-assets.

Read-only input: <game>/blueprints.zip. Output: a requiredNative row fixture in
the same shape as the reviewed Psychic extension fixtures. No game process is
launched; this is a managed archive read.
"""
import hashlib, json, pathlib, re, sys, zipfile

repo = pathlib.Path(__file__).resolve().parents[1]
game = pathlib.Path(sys.argv[1])
out = pathlib.Path(sys.argv[2])
assets = json.loads((repo / 'docs/work-packages/COTW-0085/A-spiritualist-assets.json').read_text(encoding='utf-8-sig'))

wanted = {}
for guid, row in assets.get('observedAssets', {}).items():
    wanted[guid] = row.get('recordSha256')
for group in ('sourceDonors', 'wrathComparisonDonors'):
    for row in assets.get(group, []):
        guid = row.get('originalGuid') or row.get('guid')
        if guid:
            wanted.setdefault(guid, row.get('recordSha256'))
for emotion in assets.get('emotions', []):
    for ref in emotion.get('sourceGuidReferences', []):
        if ref.get('wrathStatus') == 'notInDump':
            continue
        wanted.setdefault(ref['guid'], None)
    for sla in emotion.get('ownerSpellLike', []):
        if sla.get('originalGuid'):
            wanted.setdefault(sla['originalGuid'], None)
        cand = sla.get('candidate') or {}
        if cand.get('provider') == 'Wrath' and cand.get('guid'):
            wanted.setdefault(cand['guid'], None)
for row in assets.get('privateSpellList', []):
    if row.get('originalGuid') and row.get('wrathStatus') in ('native', 'nativeCandidateRequiresBehaviorComparison'):
        wanted.setdefault(row['originalGuid'], None)
    cand = row.get('candidate') or {}
    if cand.get('provider') == 'Wrath' and cand.get('guid'):
        wanted.setdefault(cand['guid'], None)

# Core class/book/root donors required by the registration gate.
for guid in ('02b187038a8dce545bb34bbfb346428d', '0c80276018694f24fbaf59ec7b841f2b', '0d20d88abb7c33a47902bd99019f2ed1', '20f79fea035330b479fc899fa201d232', '226f939b7dfd47b4697ec52f79799012', '247a4068296e8be42890143f451b4b45', '25c97697236ccf2479d0c6a4185eae7f', '25ec6cb6ab1845c48a95f9c20b034220', '2d77316c72b9ed44f888ceefc2a131f6', '2f91d7337b60e3b4b9b137198a8c8745', '36897b36a4bd63146b1d8509a17fc5ad', '3bc40c9cbf9a0db4b8b43d8eedf2e6ec', '45a52ce762f637f4c80cc741c91f58b7', '486eaff58293f6441a5c2759c4872f98', '48ac8db94d5de7645906c7d0ad3bcfbd', '4baf4109145de4345861fe0f2209d903', '4cd1757a0eea7694ba5c933729a53920', '4f139d125bb602f48bfaec3d3e1937cb', '57fab75111f377248810ece84193a5a5', '5bdc37e4acfa209408334326076a43bc', '62ef1cdb90f1d654d996556669caf7fa', '6d3728d4e9c9898458fe5e9532951132', '767e6932882a99c4b8ca95c88d823137', '8a6986e17799d7d4b90f0c158b31c5b9', '8d6073201e5395d458b8251386d72df1', '9054d3988d491d944ac144e27b6bc318', '90e54424d682d104ab36436bd527af09', '96c174b0ebca7b246b82d4bc4aac4574', '9972f33f977fc724c838e59641b2fca5', '9b9eac6709e1c084cb18c3a366e0ec87', 'a1ffec0ce7c167a40aaea13dc49b757b', 'a7e822a8507e44b0a981ca55586dfad9', 'b24583190f36a8442b212e45226c54fc', 'c5c86809a1c834e42a2eb33133e90a28', 'c7e1d5ef809325943af97f093e149c4f', 'c9629ef9eebb88b479b2fbc5e836656a', 'caae1dc6fcf7b37408686971ee27db13', 'cf986dd7ba9d4ec46ad8a3a0406d02ae', 'd3e6275cfa6e7a04b9213b7b292a011c', 'd8de50cc80eb4dc409a983991e0b77ad', 'ee0b69e90bac14446a4cf9a050f87f2e', 'f08a7239aa961f34c8301518e71d4cdf', 'f18cbcb39a1b35643a8d129b1ec4e716', 'f1a70d9e1b0b41e49874e1fa9052a1ce', 'ff4662bde9e75f145853417313842751',
             '918939943bf32ba4a95470ea696c2ba5', 'f894e003d31461f48a02f5caec4e3359', 'e70ecf1ed95ca2f40b754f1adb22bbdd', 'a634f97d23662b347872ffcf580495b3',
             'c133d22305bab964c88a767cc69b1f9b', '83d3e15962e5d6949b90b5c226a2b487'):
    wanted.setdefault(guid, None)

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
            raise SystemExit('Fingerprint mismatch for %s: archive %s != A-assets %s' % (guid, sha, expected))
        blueprint = parse(raw)
        rows.append({'name': name.rsplit('/', 1)[-1][:-4], 'sha256': sha.upper(), 'blueprint': blueprint})
        found[guid] = sha

missing = set(wanted) - set(found)
if missing:
    raise SystemExit('Missing native rows in the installed archive: %s' % sorted(missing))
out.parent.mkdir(parents=True, exist_ok=True)
out.write_text(json.dumps({'requiredNative': rows}, ensure_ascii=False), encoding='utf-8')
print('Spiritualist 0085: %d native rows extracted, all recorded fingerprints verified.' % len(rows))
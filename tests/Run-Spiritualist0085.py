"""B-stage targeted check: COTW-0085 Spiritualist, frozen contract
C-0085-20261115-r1.

Runs only the isolated PsychicTests host with COTW_ARCANE_01446_ONLY=1 plus the
single COTW_0085_CASE selector. No other suite, no game and no UMM is launched;
the host resolves the real game Managed assemblies through the reviewed
engine02 copy via EngineLauncher. The reviewed native blueprint fixture and the
audited 0085 donor rows (fingerprints verified against A-spiritualist-assets by
Extract-Spiritualist0085.py) are read-only inputs.

Generated artifacts carry absolute local paths; per the B/D handoff convention
they stay in the local delivery directory, never in the public repository.
"""
from pathlib import Path
import hashlib, json, os, subprocess, sys, time
from datetime import datetime

sys.stdout.reconfigure(errors='replace')
sys.stderr.reconfigure(errors='replace')

repo = Path('D:/CodexProjects/CotW-Wrath-0046-fix')
game = Path('D:/steam/steamapps/common/Pathfinder Second Adventure')
work = Path('D:/work/prototype-build')
out = Path(__file__).resolve().parent
delivery = Path('C:/Users/xuenc/agent-tools/spiritualist-next')
launcher = Path('D:/CodexProjects/CotW-Wrath/work/repair01456/engine-launcher/bin/Release/net472/EngineLauncher.exe')
engine = Path('D:/CodexProjects/CotW-Wrath/work/repair01456/engine02')
host = work / 'PsychicTests/bin/Release/net472/PsychicTests.exe'
candidate = work / 'CallOfTheWild.Wrath.Prototype/bin/Release/net472/CallOfTheWild.Wrath.Prototype.dll'
managed = game / 'Wrath_Data/Managed'
evidence = Path('D:/CodexProjects/CotW-Wrath/work/psychic060/native-test.json')
native_base = Path('D:/CodexProjects/CotW-Wrath/work/prototype-build/psychic-completion01458-native.json')
native_0085 = work / 'spiritualist-0085-native.json'
native_merged = work / 'spiritualist-0085-native-ext.json'
sha = lambda p: hashlib.sha256(Path(p).read_bytes()).hexdigest().upper()

for p in (launcher, candidate, evidence, native_base, engine / 'Assembly-CSharp.dll'):
    if not Path(p).is_file():
        raise SystemExit('missing input: %s' % p)

# Build the isolated host against the real game Managed assemblies.
build = subprocess.run(['dotnet', 'build', str(repo / 'tests/PsychicTests.csproj'), '-c', 'Release',
                        '-p:WrathPath=%s' % game, '--nologo'], capture_output=True, text=True, encoding='utf-8', errors='replace')
if build.returncode != 0:
    raise SystemExit('host build failed:\n' + build.stdout + build.stderr)
if not host.is_file():
    raise SystemExit('missing host after build: %s' % host)

# Extract the audited 0085 rows fresh from the installed archive every run.
extract = subprocess.run([sys.executable, str(repo / 'tests/Extract-Spiritualist0085.py'),
                          str(game), str(native_0085)], capture_output=True, text=True, encoding='utf-8', errors='replace')
if extract.returncode != 0:
    raise SystemExit('0085 native extraction failed:\n' + extract.stdout + extract.stderr)
print(extract.stdout.strip())

base = json.loads(native_base.read_text(encoding='utf-8-sig'))
extra = json.loads(native_0085.read_text(encoding='utf-8-sig'))
seen = {r['blueprint']['AssetId']: r['sha256'] for r in base['requiredNative']}
added = 0
for row in extra['requiredNative']:
    guid = row['blueprint']['AssetId']
    if guid in seen:
        if seen[guid].lower() != row['sha256'].lower():
            raise SystemExit('0085 row fingerprint differs from the reviewed fixture: ' + guid)
        continue
    base['requiredNative'].append(row)
    seen[guid] = row['sha256']
    added += 1
native_merged.write_text(json.dumps(base, ensure_ascii=False), encoding='utf-8')
print('Merged fixture: +%d audited 0085 rows, %d total.' % (added, len(base['requiredNative'])))

sources = ['src/Spiritualist0085.cs', 'src/Phantom0085.cs', 'src/PhantomRuntime0085.cs',
           'src/BlueprintRegistration.cs', 'src/ModuleCatalog.cs',
           'tests/Spiritualist0085Tests.cs', 'tests/PsychicTests.cs', 'tests/PsychicTests.csproj',
           'tests/Extract-Spiritualist0085.py']

env = {k: v for k, v in os.environ.items() if not k.startswith('COTW_')}
env.update(COTW_ARCANE_01446_ONLY='1', COTW_0085_CASE='spiritualist0085')
stamp = datetime.now().strftime('%Y%m%d-%H%M%S')
delivery.mkdir(parents=True, exist_ok=True)
report = delivery / 'B-report.json'
log = delivery / ('B-directed-' + stamp + '.log')
for p in (report, log):
    if p.exists():
        p.unlink()
cmd = [str(launcher), str(engine), str(host), str(managed), str(candidate), str(report), str(evidence), str(native_merged)]
started = time.time()
with log.open('wb') as f:
    process = subprocess.run(cmd, cwd=str(repo), env=env, stdout=f, stderr=subprocess.STDOUT)
elapsed = time.time() - started
text = log.read_text(encoding='utf-8-sig', errors='replace')
lines = text.splitlines()

result = {
    'case': 'spiritualist0085',
    'contract': 'C-0085-20261115-r1',
    'workPackage': 'COTW-0085',
    'stage': 'B directed: registration gate/idempotency, master class and private spellbook, 137-row catalog and source circles, six emotion graphs with one reserved-type AddPet each, phantom class/progression, master milestones, recall/bonded resource shapes, consciousness grants, Potent Phantom, shared-root preservation',
    'command': cmd,
    'environment': {k: v for k, v in env.items() if k.startswith('COTW_')},
    'exitCode': process.returncode,
    'seconds': round(elapsed, 2),
    'hostSHA256': sha(host),
    'candidateSHA256': sha(candidate),
    'engineSHA256': sha(engine / 'Assembly-CSharp.dll'),
    'gameAssemblySHA256': sha(managed / 'Assembly-CSharp.dll'),
    'harmonySHA256': sha(managed / '0Harmony.dll'),
    'nativeFixture': str(native_merged),
    'nativeFixtureSHA256': sha(native_merged),
    'native0085Rows': str(native_0085),
    'native0085RowsSHA256': sha(native_0085),
    'evidenceFixture': str(evidence),
    'evidenceFixtureSHA256': sha(evidence),
    'sourceSHA256': {s: sha(repo / s) for s in sources},
    'log': str(log),
    'logSHA256': sha(log),
    'report': str(report) if report.is_file() else None,
    'passedLine': [l for l in lines if l.startswith('PASS:')],
    'resultLine': [l for l in lines if l.startswith('0085 RESULT:')],
    'notRunLine': [l for l in lines if l.startswith('0085 NOT RUN:')],
    'checkCount': len([l for l in lines if l.startswith('0085 CHECK:')]),
    'failureLines': [l for l in lines if 'CHECK FAILED' in l or
                     (l.startswith('System.') and not l.startswith('0085 HOST LOG:'))],
}
(delivery / 'B-result.json').write_text(json.dumps(result, ensure_ascii=False, indent=2), encoding='utf-8')
print(json.dumps({k: result[k] for k in ('exitCode', 'seconds', 'passedLine', 'resultLine', 'checkCount', 'failureLines')}, ensure_ascii=False, indent=2))
sys.exit(process.returncode)
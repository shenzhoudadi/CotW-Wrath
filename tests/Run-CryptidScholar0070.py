"""B-stage targeted check: COTW-0070 Cryptid Scholar, frozen contract
C-0070-20261112-r1.

Runs only the isolated PsychicTests host with COTW_ARCANE_01446_ONLY=1 plus the
single COTW_0070_CASE selector. No other suite, no game and no UMM is launched;
the host resolves the real game Managed assemblies through the reviewed
engine02 copy via EngineLauncher. The reviewed native blueprint fixture, the
extended 0060 row closure and the audited 0070 classification rows (fingerprints
verified against A-evidence by Extract-CryptidScholar0070.py) are read-only
inputs.

The worktree builds into work/work/prototype-build, so both the candidate DLL
and the PsychicTests host are taken from that double-work path.
"""
from pathlib import Path
import hashlib, json, os, subprocess, sys, time
from datetime import datetime

sys.stdout.reconfigure(errors='replace')
sys.stderr.reconfigure(errors='replace')

root = Path('D:/CodexProjects/CotW-Wrath')
game = Path('D:/steam/steamapps/common/Pathfinder Second Adventure')
repo = root / 'work/cryptid-scholar-0070-b/repo'
out = Path(__file__).resolve().parent
delivery = Path('C:/Users/xuenc/agent-tools/cryptid-scholar-next')
launcher = root / 'work/repair01456/engine-launcher/bin/Release/net472/EngineLauncher.exe'
engine = root / 'work/repair01456/engine02'
host = root / 'work/work/prototype-build/PsychicTests/bin/Release/net472/PsychicTests.exe'
candidate = root / 'work/work/prototype-build/CallOfTheWild.Wrath.Prototype/bin/Release/net472/CallOfTheWild.Wrath.Prototype.dll'
managed = game / 'Wrath_Data/Managed'
evidence = root / 'work/psychic060/native-test.json'
native_base = root / 'work/prototype-build/psychic-completion01458-native.json'
native_ext = root / 'work/prototype-build/spirit-guide-0060-native-ext.json'
native_0070 = root / 'work/prototype-build/cryptid-scholar-0070-native.json'
native_merged = root / 'work/prototype-build/cryptid-scholar-0070-native-ext.json'
sha = lambda p: hashlib.sha256(Path(p).read_bytes()).hexdigest().upper()

for p in (launcher, host, candidate, evidence, native_base, engine / 'Assembly-CSharp.dll'):
    if not Path(p).is_file():
        raise SystemExit('missing input: %s' % p)

# Extract the audited 0070 rows fresh from the installed archive every run.
extract = subprocess.run([sys.executable, str(repo / 'tests/Extract-CryptidScholar0070.py'),
                          str(game), str(native_0070)], capture_output=True, text=True)
if extract.returncode != 0:
    raise SystemExit('0070 native extraction failed:\n' + extract.stdout + extract.stderr)
print(extract.stdout.strip())

base = json.loads((native_ext if native_ext.is_file() else native_base).read_text(encoding='utf-8-sig'))
extra = json.loads(native_0070.read_text(encoding='utf-8-sig'))
seen = {r['blueprint']['AssetId']: r['sha256'] for r in base['requiredNative']}
added = 0
for row in extra['requiredNative']:
    guid = row['blueprint']['AssetId']
    if guid in seen:
        if seen[guid].lower() != row['sha256'].lower():
            raise SystemExit('0070 row fingerprint differs from the reviewed fixture: ' + guid)
        continue
    base['requiredNative'].append(row)
    seen[guid] = row['sha256']
    added += 1
native_merged.write_text(json.dumps(base, ensure_ascii=False), encoding='utf-8')
print('Merged fixture: +%d audited 0070 rows, %d total.' % (added, len(base['requiredNative'])))

sources = ['src/CryptidScholar0070.cs', 'src/CryptidScholarRuntime0070.cs',
           'src/BlueprintRegistration.cs', 'src/ModuleCatalog.cs',
           'tests/CryptidScholar0070Tests.cs', 'tests/PsychicTests.cs', 'tests/PsychicTests.csproj',
           'tests/Extract-CryptidScholar0070.py']

env = {k: v for k, v in os.environ.items() if not k.startswith('COTW_')}
env.update(COTW_ARCANE_01446_ONLY='1', COTW_0070_CASE='cryptidscholar0070')
stamp = datetime.now().strftime('%Y%m%d-%H%M%S')
# Generated artifacts carry absolute local paths; per the B/D handoff convention
# they stay in the local delivery directory, never in the public repository.
delivery.mkdir(parents=True, exist_ok=True)
report = delivery / 'B-report.json'
log = delivery / ('B-directed-' + stamp + '.log')
for p in (report, log):
    if p.exists():
        p.unlink()
cmd = [str(launcher), str(engine), str(host), str(managed), str(candidate), str(report), str(evidence), str(native_merged)]
started = time.time()
with log.open('wb') as f:
    process = subprocess.run(cmd, cwd=str(root), env=env, stdout=f, stderr=subprocess.STDOUT)
elapsed = time.time() - started
text = log.read_text(encoding='utf-8-sig', errors='replace')
lines = text.splitlines()

result = {
    'case': 'cryptidscholar0070',
    'contract': 'C-0070-20261112-r1',
    'workPackage': 'COTW-0070',
    'stage': 'B directed: registration gate/rollback, level table, Intuitive Lore, personal knowledge credential, exact classification, 24h receipts, real inspiration payment, range/hearing, AC/save sources, strike conversion and precision boundaries, talent scope, restore/cleanup, expiry/type change',
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
    'native0070Rows': str(native_0070),
    'native0070RowsSHA256': sha(native_0070),
    'evidenceFixture': str(evidence),
    'evidenceFixtureSHA256': sha(evidence),
    'sourceSHA256': {s: sha(repo / s) for s in sources},
    'log': str(log),
    'logSHA256': sha(log),
    'report': str(report) if report.is_file() else None,
    'passedLine': [l for l in lines if l.startswith('PASS:')],
    'resultLine': [l for l in lines if l.startswith('0070 RESULT:')],
    'notRunLine': [l for l in lines if l.startswith('0070 NOT RUN:')],
    'checkCount': len([l for l in lines if l.startswith('0070 CHECK:')]),
    'hostLogLines': [l for l in lines if l.startswith('0070 HOST LOG:')],
    'failureLines': [l for l in lines if 'CHECK FAILED' in l or
                     (l.startswith('System.') and not l.startswith('0070 HOST LOG:'))],
    'gameLaunched': False,
    'ummLaunched': False,
    'scope': ('Isolated managed host only. Real native blueprint cache, real production 0070 registration '
              'and runtime, real InspectUnitsManager.TryMakeKnowledgeCheck (skill/training/DC), real '
              'RulebookEventBus dispatch for AC/saves/prepare-damage, real native resource spend/restore, '
              'real fact/buff lifecycles and real BlueprintFeatureSelection.CanSelect through the production '
              'prefix. HA provider roots are staged at the frozen A-evidence identities; classification rows '
              'are extracted from the installed archive with A-evidence fingerprint verification. Scene-dependent '
              'inspect visibility, area scan, faction model and dice are controlled host inputs. Real third-party '
              'plugin load, player gameplay, save/load transport, SaveManager/retraining controller and the full '
              'historical suites are NOT run.'),
    'notRun': ['game/UMM startup', 'full historical Psychic/wotr-testing suites',
               'real HomebrewArchetypes plugin load',
               'SaveManager and retraining controller', 'whole-library matrix',
               'packaging, version bump, push, GitHub release or issue closure'],
}
result['verdict'] = ('PASS' if process.returncode == 0 and result['passedLine'] and
                     not result['failureLines'] and not result['hostLogLines'] else 'NEEDS_REVIEW')
(delivery / 'B-result.json').write_text(json.dumps(result, ensure_ascii=False, indent=2) + '\n', encoding='utf-8')
(delivery / 'B-evidence.json').write_text(json.dumps(result, ensure_ascii=False, indent=2) + '\n', encoding='utf-8')
print('exit=%s seconds=%.2f checks=%s verdict=%s' % (process.returncode, elapsed, result['checkCount'], result['verdict']))
print('PASS lines:', result['passedLine'])
print('RESULT lines:', result['resultLine'])
print('NOT RUN lines:', result['notRunLine'])
if result['failureLines']:
    print('FAILURE lines:', result['failureLines'][:20])
if result['hostLogLines']:
    print('HOST LOG lines:', result['hostLogLines'][:20])
sys.exit(0 if result['verdict'] == 'PASS' else 1)
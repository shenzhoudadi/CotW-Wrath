"""B-stage targeted check: COTW-0068 Psychic Detective (S-PSYCHIC-0068 binding
plus the complete F-0068 variant), frozen contract C-0068-20261111-r1.

Runs only the isolated PsychicTests host with COTW_ARCANE_01446_ONLY=1 plus the
single COTW_0068_CASE selector. No other suite, no game and no UMM is launched;
the host resolves the real game Managed assemblies through the reviewed
engine02 copy via EngineLauncher. The reviewed native blueprint fixture and the
extended 0060 row closure are read-only inputs owned by earlier packages.

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
repo = root / 'work/psychic-detective-0068-b/repo'
out = Path(__file__).resolve().parent
delivery = Path('C:/Users/xuenc/agent-tools/psychic-detective-next')
launcher = root / 'work/repair01456/engine-launcher/bin/Release/net472/EngineLauncher.exe'
engine = root / 'work/repair01456/engine02'
host = root / 'work/work/prototype-build/PsychicTests/bin/Release/net472/PsychicTests.exe'
candidate = root / 'work/work/prototype-build/CallOfTheWild.Wrath.Prototype/bin/Release/net472/CallOfTheWild.Wrath.Prototype.dll'
managed = game / 'Wrath_Data/Managed'
evidence = root / 'work/psychic060/native-test.json'
native_base = root / 'work/prototype-build/psychic-completion01458-native.json'
native_ext = root / 'work/prototype-build/spirit-guide-0060-native-ext.json'
native = native_ext if native_ext.is_file() else native_base
sha = lambda p: hashlib.sha256(Path(p).read_bytes()).hexdigest().upper()

for p in (launcher, host, candidate, evidence, native_base, engine / 'Assembly-CSharp.dll'):
    if not Path(p).is_file():
        raise SystemExit('missing input: %s' % p)

sources = ['src/PsychicDetective0068Ids.cs', 'src/PsychicDetective0068Binding.cs',
           'src/PsychicDetective0068.cs', 'src/PsychicAmplificationComponent.cs',
           'src/PsychicAdvancedRuntime01458.cs', 'src/PsychicAmplificationMenu.cs',
           'src/Mindblade01454.cs', 'src/BlueprintRegistration.cs', 'src/ModuleCatalog.cs',
           'tests/PsychicDetective0068Tests.cs', 'tests/PsychicTests.cs', 'tests/PsychicTests.csproj']

env = {k: v for k, v in os.environ.items() if not k.startswith('COTW_')}
env.update(COTW_ARCANE_01446_ONLY='1', COTW_0068_CASE='psychicdetective0068')
stamp = datetime.now().strftime('%Y%m%d-%H%M%S')
report = out / 'psychicdetective0068-report.json'
log = out / ('psychicdetective0068-%s.log' % stamp)
for p in (report, log):
    if p.exists():
        p.unlink()
cmd = [str(launcher), str(engine), str(host), str(managed), str(candidate), str(report), str(evidence), str(native)]
started = time.time()
with log.open('wb') as f:
    process = subprocess.run(cmd, cwd=str(root), env=env, stdout=f, stderr=subprocess.STDOUT)
elapsed = time.time() - started
text = log.read_text(encoding='utf-8-sig', errors='replace')
lines = text.splitlines()

result = {
    'case': 'psychicdetective0068',
    'contract': 'C-0068-20261111-r1',
    'workPackage': 'COTW-0068',
    'stage': 'B directed: S-PSYCHIC-0068 binding positives/negatives + F-0068 registration structure',
    'command': cmd,
    'environment': {k: v for k, v in env.items() if k.startswith('COTW_')},
    'exitCode': process.returncode,
    'seconds': round(elapsed, 2),
    'hostSHA256': sha(host),
    'candidateSHA256': sha(candidate),
    'engineSHA256': sha(engine / 'Assembly-CSharp.dll'),
    'gameAssemblySHA256': sha(managed / 'Assembly-CSharp.dll'),
    'harmonySHA256': sha(managed / '0Harmony.dll'),
    'nativeFixture': str(native),
    'nativeFixtureSHA256': sha(native),
    'nativeFixtureIsExtended': native == native_ext,
    'evidenceFixture': str(evidence),
    'evidenceFixtureSHA256': sha(evidence),
    'sourceSHA256': {s: sha(repo / s) for s in sources},
    'log': str(log),
    'logSHA256': sha(log),
    'report': str(report) if report.is_file() else None,
    'passedLine': [l for l in lines if l.startswith('PASS:')],
    'resultLine': [l for l in lines if l.startswith('0068 RESULT:')],
    'notRunLine': [l for l in lines if l.startswith('0068 NOT RUN:')],
    'checkCount': len([l for l in lines if l.startswith('0068 CHECK:')]),
    'hostLogLines': [l for l in lines if l.startswith('0068 HOST LOG:')],
    'failureLines': [l for l in lines if 'CHECK FAILED' in l or
                     (l.startswith('System.') and not l.startswith('0068 HOST LOG:'))],
    'gameLaunched': False,
    'ummLaunched': False,
    'scope': ('Isolated managed host only. Real native blueprint cache, real production 0068 registration '
              'and binding runtime, real ClassData/DemandSpellbook, real native resource maximum and '
              'real BlueprintFeatureSelection.CanSelect through the production prefix. HA provider roots, '
              'eleven shared amplification roots and Psychofeedback effects are staged at their audited '
              'identities; real third-party plugin load, player gameplay, save/load transport, '
              'SaveManager/retraining controller and the full historical suites are NOT run.'),
    'notRun': ['game/UMM startup', 'full historical Psychic/wotr-testing suites',
               'real HomebrewArchetypes / TabletopTweaks plugin load',
               'SaveManager and retraining controller', 'whole-library matrix',
               'packaging, version bump, push, GitHub release or issue closure'],
}
result['verdict'] = ('PASS' if process.returncode == 0 and result['passedLine'] and
                     not result['failureLines'] and not result['hostLogLines'] else 'NEEDS_REVIEW')
(out / 'psychicdetective0068-result.json').write_text(json.dumps(result, ensure_ascii=False, indent=2) + '\n', encoding='utf-8')
delivery.mkdir(parents=True, exist_ok=True)
(delivery / ('B-negative-' + stamp + '.log')).write_bytes(log.read_bytes())
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

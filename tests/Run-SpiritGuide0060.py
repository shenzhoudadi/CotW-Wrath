"""B-stage targeted check: COTW-0060 Spirit Guide dangerous ownership paths,
contract C-0060-20261110-r1 (C03 paired mystery class-skill Retain/Release,
C06 Oracle-level/Charisma isolation, C10 four Wisdom secondary branches,
C13 source-safe temporary knowledge).

Runs only the isolated PsychicTests host with COTW_ARCANE_01446_ONLY=1 plus the
single COTW_0060_CASE selector, so no other suite executes. The game is never
launched; the host resolves the real game Managed assemblies through the
reviewed engine02 copy via EngineLauncher.

The worktree builds into work/work/prototype-build (the csproj workspaceRoot is
repo/../.. = work/, then +work/prototype-build), so BOTH the candidate DLL and
the PsychicTests host are taken from that double-work path. The native blueprint
fixture and anchor evidence remain the reviewed single-work inputs.
"""
from pathlib import Path
import hashlib, json, os, subprocess, sys, time

# The GBK console cannot encode replacement chars from native log output.
sys.stdout.reconfigure(errors='replace')
sys.stderr.reconfigure(errors='replace')

root = Path('D:/CodexProjects/CotW-Wrath')
game = Path('D:/steam/steamapps/common/Pathfinder Second Adventure')
out = Path(__file__).resolve().parent
launcher = root / 'work/repair01456/engine-launcher/bin/Release/net472/EngineLauncher.exe'
engine = root / 'work/repair01456/engine02'
host = root / 'work/work/prototype-build/PsychicTests/bin/Release/net472/PsychicTests.exe'
candidate = root / 'work/work/prototype-build/CallOfTheWild.Wrath.Prototype/bin/Release/net472/CallOfTheWild.Wrath.Prototype.dll'
managed = game / 'Wrath_Data/Managed'
evidence = root / 'work/psychic060/native-test.json'
native_base = root / 'work/prototype-build/psychic-completion01458-native.json'
# The Battle spirit chain the private-clone checks walk (aura ability -> aura buff
# -> area effect -> effect buff -> variants) is 39 native rows that the 01458
# completion slice never referenced, so they are absent from that fixture while
# still being present in the audited full native dump. build-0060-native-ext.py
# re-emits the same fixture shape with those rows closed over, read-only, keeping
# requiredNative/allNativeIds and every other section byte-identical. The case
# consumes that superset when it exists and records which input it used, so a
# missing extension degrades to a clear fixture-boundary report instead of a
# silent partial graph.
native_ext = root / 'work/prototype-build/spirit-guide-0060-native-ext.json'
native = native_ext if native_ext.is_file() else native_base
sha = lambda p: hashlib.sha256(Path(p).read_bytes()).hexdigest().upper()

for p in (launcher, host, candidate, evidence, native_base, engine / 'Assembly-CSharp.dll'):
    if not Path(p).is_file():
        raise SystemExit('missing input: %s' % p)

env = {k: v for k, v in os.environ.items() if not k.startswith('COTW_')}
env.update(COTW_ARCANE_01446_ONLY='1', COTW_0060_CASE='spiritguide0060')
report = out / 'spiritguide0060-report.json'
log = out / 'spiritguide0060.log'
if report.exists():
    report.unlink()
if log.exists():
    log.unlink()
cmd = [str(launcher), str(engine), str(host), str(managed), str(candidate), str(report), str(evidence), str(native)]
started = time.time()
with log.open('wb') as f:
    process = subprocess.run(cmd, cwd=str(root), env=env, stdout=f, stderr=subprocess.STDOUT)
elapsed = time.time() - started
text = log.read_text(encoding='utf-8-sig', errors='replace')
lines = text.splitlines()
# D2: a HOST LOG prefix alone must not exempt an exception from review. Only an
# explicitly identified environment boundary (exact exception type + the exact
# native method that produces it) is classified; anything else is unknown and
# turns the recorded verdict into NEEDS_REVIEW. All lines are preserved.
def classify_host_logs(lines):
    known, unknown = [], []
    for i, line in enumerate(lines):
        if not line.startswith('0060 HOST LOG:'):
            continue
        block = '\n'.join(lines[i:i + 8])
        if ('System.NullReferenceException' in block and
                'Kingmaker.UnitLogic.FactLogic.AddFacts.UpdateFacts' in block):
            known.append({'line': line,
                          'classification': 'native AddFacts.UpdateFacts NRE: the isolated host cannot '
                                            'construct an ability fact without BlueprintRoot/Unity assets'})
        else:
            unknown.append(line)
    return known, unknown


host_boundaries, unknown_host_logs = classify_host_logs(lines)
result = {
    'case': 'spiritguide0060',
    'contract': 'C-0060-20261110-r1',
    'workPackage': 'COTW-0060',
    'stage': 'B dangerous-path slice (S-0060-OWNERSHIP); full F-0060-SPIRITS not claimed',
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
    'log': str(log),
    'logSHA256': sha(log),
    'report': str(report) if report.is_file() else None,
    'passedLine': [l for l in lines if l.startswith('PASS:')],
    'resultLine': [l for l in lines if l.startswith('0060 RESULT:')],
    'notRunLine': [l for l in lines if l.startswith('0060 NOT RUN:')],
    'checkCount': len([l for l in lines if l.startswith('0060 CHECK:')]),
    # The host redirects Owlcat's log channel to stdout, so a native exception
    # that the game itself only logs shows up as a HOST LOG line. Those are kept
    # verbatim, split into identified environment boundaries and unknown ones;
    # a real failure aborts the case with CHECK FAILED, it never hides in a log.
    'hostLogLines': [l for l in lines if l.startswith('0060 HOST LOG:')],
    'classifiedHostBoundaries': host_boundaries,
    'unknownHostLogLines': unknown_host_logs,
    'diagnosticLines': [l for l in lines if l.startswith('0060 DIAGNOSTIC:')],
    'failureLines': [l for l in lines if 'CHECK FAILED' in l or
                     (l.startswith('System.') and not l.startswith('0060 HOST LOG:'))],
    'gameLaunched': False,
    'ummLaunched': False,
    'scope': 'Isolated managed host only; directed native evidence for C03/C06/C10/C13, not player-game acceptance and not a whole-package behaviour PASS.',
}
result['verdict'] = ('PASS' if process.returncode == 0 and not result['failureLines']
                     and not unknown_host_logs else 'NEEDS_REVIEW')
result['unknownHostLogPolicy'] = 'Any HOST LOG exception without the exact AddFacts.UpdateFacts NRE signature is NEEDS_REVIEW.'
(out / 'spiritguide0060-result.json').write_text(json.dumps(result, ensure_ascii=False, indent=2) + '\n', encoding='utf-8')
print('exit=%s seconds=%.2f checks=%s verdict=%s' % (process.returncode, elapsed, result['checkCount'], result['verdict']))
print('PASS lines:', result['passedLine'])
print('RESULT lines:', result['resultLine'])
print('classified host boundaries:', len(host_boundaries), 'unknown host logs:', len(unknown_host_logs))
if result['failureLines']:
    print('FAILURE lines:', result['failureLines'][:20])
sys.exit(0 if result['verdict'] == 'PASS' else 1)
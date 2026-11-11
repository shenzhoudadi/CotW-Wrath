"""B-stage targeted check: COTW-0109 Iroran Paladin, contract
C-0109-20261111-r1. Runs B1 reroll primitive checks (C0109-10/11/12) plus the
B3 archetype registration checks (C0109-01..09 structure) in one isolated host
invocation. The game is never launched; the host resolves the real game Managed
assemblies through the reviewed engine02 copy via EngineLauncher. Build outputs
derive from this checkout's Directory.Build.props workspace root, so both the
candidate DLL and the host come from the same candidate build. The native row
fixture is tests/Build-Iroran0109Native.py output: the reviewed 0060 rows this
registration loads plus sixteen donors extracted from blueprints.zip.
"""
from pathlib import Path
import hashlib, json, os, subprocess, sys, time

sys.stdout.reconfigure(errors='replace')
sys.stderr.reconfigure(errors='replace')

root = Path('D:/CodexProjects/CotW-Wrath')
game = Path('D:/steam/steamapps/common/Pathfinder Second Adventure')
out = Path(__file__).resolve().parent
build = out.parent.parent.parent / 'work/prototype-build'
launcher = root / 'work/repair01456/engine-launcher/bin/Release/net472/EngineLauncher.exe'
engine = root / 'work/repair01456/engine02'
host = build / 'PsychicTests/bin/Release/net472/PsychicTests.exe'
candidate = build / 'CallOfTheWild.Wrath.Prototype/bin/Release/net472/CallOfTheWild.Wrath.Prototype.dll'
managed = game / 'Wrath_Data/Managed'
fixture = build / 'iroran-0109-native.json'
sha = lambda p: hashlib.sha256(Path(p).read_bytes()).hexdigest().upper()

for p in (launcher, host, candidate, engine / 'Assembly-CSharp.dll', fixture):
    if not Path(p).is_file():
        raise SystemExit('missing input: %s' % p)
# engine02 is the reviewed compatibility-process engine used by the historical
# runners (0060/054); its Assembly-CSharp bytes differ from the installed game.
# Both hashes are recorded in the result, exactly as Run-SpiritGuide0060.py does.

env = {k: v for k, v in os.environ.items() if not k.startswith('COTW_')}
env.update(COTW_0109_CASE='iroran0109')
report = out / 'iroran0109-report.json'
registration_report = out / 'iroran0109-registration-report.json'
log = out / 'iroran0109.log'
for stale in (report, registration_report, log):
    if stale.exists():
        stale.unlink()
cmd = [str(launcher), str(engine), str(host), str(managed), str(candidate), str(report), '-', str(fixture)]
started = time.time()
with log.open('wb') as f:
    process = subprocess.run(cmd, cwd=str(root), env=env, stdout=f, stderr=subprocess.STDOUT)
elapsed = time.time() - started
text = log.read_text(encoding='utf-8-sig', errors='replace')
lines = text.splitlines()
result = {
    'case': 'iroran0109',
    'contract': 'C-0109-20261111-r1',
    'workPackage': 'COTW-0109',
    'stage': 'B1 reroll primitive (C0109-10/11/12) + B3 archetype registration graph (C0109-01..09 structure)',
    'command': cmd,
    'environment': {k: v for k, v in env.items() if k.startswith('COTW_')},
    'exitCode': process.returncode,
    'seconds': round(elapsed, 2),
    'hostSHA256': sha(host),
    'candidateSHA256': sha(candidate),
    'engineSHA256': sha(engine / 'Assembly-CSharp.dll'),
    'gameAssemblySHA256': sha(managed / 'Assembly-CSharp.dll'),
    'harmonySHA256': sha(managed / '0Harmony.dll'),
    'nativeFixture': str(fixture),
    'nativeFixtureSHA256': sha(fixture),
    'log': str(log),
    'logSHA256': sha(log),
    'report': str(report) if report.is_file() else None,
    'registrationReport': str(registration_report) if registration_report.is_file() else None,
    'passedLine': [l for l in lines if l.startswith('PASS:')],
    'checkCount': len([l for l in lines if l.startswith('0109 CHECK:')]),
    'failureLines': [l for l in lines if 'CHECK FAILED' in l or
                     (l.startswith('System.') and not l.startswith('0109 HOST LOG:'))],
    'gameLaunched': False,
    'ummLaunched': False,
    'scope': 'Isolated managed host only; real RuleRollD20 native entries with fixed RNG for the reroll primitive, and the publication graph against the reviewed native row fixture for the archetype. Runtime unit behaviour (modifier stacks, resource spending, enchant lifetimes, area-scene lifecycle) remains untested. Not player-game acceptance.',
}
result['verdict'] = ('PASS' if process.returncode == 0 and not result['failureLines']
                     and result['checkCount'] > 0 else 'NEEDS_REVIEW')
(out / 'iroran0109-result.json').write_text(json.dumps(result, ensure_ascii=False, indent=2) + '\n', encoding='utf-8')
print('exit=%s seconds=%.2f checks=%s verdict=%s' % (process.returncode, elapsed, result['checkCount'], result['verdict']))
print('PASS lines:', result['passedLine'])
if result['failureLines']:
    print('FAILURE lines:', result['failureLines'][:20])
sys.exit(0 if result['verdict'] == 'PASS' else 1)
"""B-stage targeted check: F-0081 Esoteric Starseeker contract C01-STARSEEKER-20261107-r1.

Runs only the isolated PsychicTests host with COTW_ARCANE_01446_ONLY=1 plus the
single COTW_0081_CASE selector, so no other suite executes. The game is never
launched; the host resolves the real game Managed assemblies through the
reviewed engine02 copy via EngineLauncher.
"""
from pathlib import Path
import hashlib, json, os, subprocess, sys, time

root = Path('D:/CodexProjects/CotW-Wrath')
game = Path('D:/steam/steamapps/common/Pathfinder Second Adventure')
out = Path(__file__).resolve().parent
launcher = root / 'work/repair01456/engine-launcher/bin/Release/net472/EngineLauncher.exe'
engine = root / 'work/repair01456/engine02'
host = root / 'work/prototype-build/PsychicTests/bin/Release/net472/PsychicTests.exe'
candidate = root / 'work/prototype-build/CallOfTheWild.Wrath.Prototype/bin/Release/net472/CallOfTheWild.Wrath.Prototype.dll'
managed = game / 'Wrath_Data/Managed'
evidence = root / 'work/psychic060/native-test.json'
native = root / 'work/prototype-build/psychic-completion01458-native.json'
sha = lambda p: hashlib.sha256(Path(p).read_bytes()).hexdigest().upper()

for p in (launcher, host, candidate, evidence, native, engine / 'Assembly-CSharp.dll'):
    if not Path(p).is_file():
        raise SystemExit('missing input: %s' % p)

env = {k: v for k, v in os.environ.items() if not k.startswith('COTW_')}
env.update(COTW_ARCANE_01446_ONLY='1', COTW_0081_CASE='starseeker0081')
report = out / 'starseeker0081-report.json'
log = out / 'starseeker0081.log'
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
inventory = None
if report.is_file():
    try:
        inventory = json.loads(report.read_text(encoding='utf-8-sig'))
    except Exception as error:  # keep the raw text for diagnosis
        inventory = {'unparsable': str(error)}
result = {
    'case': 'starseeker0081',
    'contract': 'C01-STARSEEKER-20261107-r1',
    'command': cmd,
    'environment': {k: v for k, v in env.items() if k.startswith('COTW_')},
    'exitCode': process.returncode,
    'seconds': round(elapsed, 2),
    'hostSHA256': sha(host),
    'candidateSHA256': sha(candidate),
    'engineSHA256': sha(engine / 'Assembly-CSharp.dll'),
    'gameAssemblySHA256': sha(managed / 'Assembly-CSharp.dll'),
    'harmonySHA256': sha(managed / '0Harmony.dll'),
    'log': str(log),
    'logSHA256': sha(log),
    'report': str(report) if report.is_file() else None,
    'inventory': inventory.get('esotericStarseeker0081Contracts') if isinstance(inventory, dict) else None,
    'passedLine': [l for l in text.splitlines() if l.startswith('PASS:')],
    'gameLaunched': False,
    'ummLaunched': False,
    'scope': 'Isolated managed host only; not player-game acceptance.',
}
(out / 'starseeker0081-result.json').write_text(json.dumps(result, ensure_ascii=False, indent=2) + '\n', encoding='utf-8')
print('exit=%s seconds=%.2f' % (process.returncode, elapsed))
print('PASS lines:', result['passedLine'])
sys.exit(0 if process.returncode == 0 else 1)

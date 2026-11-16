"""B-stage directed native check: COTW-0099 Occultist, contract C-0099-20261115-r1.

Runs the isolated Occultist0099NativeTests host against the real game Managed
assemblies resolved through the reviewed engine02 publicized copy by
EngineLauncher. The host exercises RuleCastSpell, the resource precheck,
SelectSpell.Check/Apply, Spellbook.AddKnown, UnitHelper.CopySpellbook and
RestController.ApplyRest. It verifies that ordinary startup skips the slice
before installing the development patches explicitly.

The game is never launched, UMM is never loaded, no historical suite runs, and
the shared 0099 registration chain stays untouched (SlicePublished=false).
This is NOT player-game acceptance and NOT a release sign-off.
"""
from pathlib import Path
import hashlib, json, os, subprocess, sys, time

root = Path('D:/CodexProjects/CotW-Wrath')
game = Path('D:/steam/steamapps/common/Pathfinder Second Adventure')
out = Path(__file__).resolve().parent
package = out.parent / 'docs/work-packages/COTW-0099'
launcher = root / 'work/repair01456/engine-launcher/bin/Release/net472/EngineLauncher.exe'
engine = root / 'work/repair01456/engine02'
host = root / 'work/prototype-build/Occultist0099NativeTests/bin/Release/net472/Occultist0099NativeTests.exe'
candidate = root / 'work/prototype-build/CallOfTheWild.Wrath.Prototype/bin/Release/net472/CallOfTheWild.Wrath.Prototype.dll'
managed = game / 'Wrath_Data/Managed'
evidence = package / 'A-occultist-assets.json'
native = root / 'work/prototype-build/spirit-guide-0060-native-ext.json'
sha = lambda p: hashlib.sha256(Path(p).read_bytes()).hexdigest().upper()

for p in (launcher, host, candidate, evidence, native, engine / 'Assembly-CSharp.dll'):
    if not Path(p).is_file():
        raise SystemExit('missing input: %s' % p)

env = {k: v for k, v in os.environ.items() if not k.startswith('COTW_')}
env['COTW_HARMONY_MATRIX_PATH'] = str(managed / '0Harmony.dll')
report = out / 'occultist0099-native-report.json'
log = out / 'occultist0099-native.log'
for stale in (report, log):
    if stale.exists():
        stale.unlink()
cmd = [str(launcher), str(engine), str(host), str(managed), str(candidate),
       str(report), str(evidence), str(native)]
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

# Source fingerprints are the authoritative code identity for this stage; the
# DLL hashes below are build artefacts only (D-0099-07).
source_dir = out.parent / 'src'
sources = [source_dir / 'ModEntry.cs', *sorted(source_dir.glob('Occultist0099*.cs'))]
result = {
    'workPackage': 'COTW-0099',
    'case': 'occultist0099-native',
    'contract': 'C-0099-20261115-r1',
    'findings': ['D-0099-01', 'D2-01', 'D2-02', 'D2-03', 'D2-04', 'D2-05', 'D2-06'],
    'command': cmd,
    'environment': {k: v for k, v in env.items() if k.startswith('COTW_')},
    'exitCode': process.returncode,
    'seconds': round(elapsed, 2),
    'sourceSHA256': {p.name: sha(p) for p in sources},
    'hostSourceSHA256': sha(out / 'Occultist0099NativeProgram.cs'),
    'hostSHA256': sha(host),
    'candidateSHA256': sha(candidate),
    'engineSHA256': sha(engine / 'Assembly-CSharp.dll'),
    'gameAssemblySHA256': sha(managed / 'Assembly-CSharp.dll'),
    'harmonySHA256': sha(managed / '0Harmony.dll'),
    'evidenceSHA256': sha(evidence),
    'nativeFixtureSHA256': sha(native),
    'log': str(log),
    'logSHA256': sha(log),
    'report': str(report) if report.is_file() else None,
    'result': inventory.get('result') if isinstance(inventory, dict) else None,
    'checks': inventory.get('checks') if isinstance(inventory, dict) else None,
    'failures': inventory.get('failures') if isinstance(inventory, dict) else None,
    'caseStatus': inventory.get('caseStatus') if isinstance(inventory, dict) else None,
    'slicePublished': inventory.get('slicePublished') if isinstance(inventory, dict) else None,
    'passedLine': [l for l in text.splitlines() if l.startswith('PASS:')],
    'failedLine': [l for l in text.splitlines() if 'CHECK FAILED' in l],
    'gameLaunched': False,
    'ummLaunched': False,
    'scope': 'Isolated managed native host only; not player-game acceptance.',
    'notRun': [
        'game/UMM startup', 'player gameplay acceptance', 'full historical suites',
        'shared class/spellbook/progression registration',
        'level-up UI and respec controller', 'save/load transport',
        'packaging',
        'upload and issue closure',
    ],
}
(out / 'occultist0099-native-result.json').write_text(
    json.dumps(result, ensure_ascii=False, indent=2) + '\n', encoding='utf-8')
print('exit=%s seconds=%.2f' % (process.returncode, elapsed))
print('result:', result['result'], 'checks:', result['checks'])
print('PASS lines:', result['passedLine'])
for line in result['failedLine'][:20]:
    print(line)
sys.exit(0 if process.returncode == 0 else 1)

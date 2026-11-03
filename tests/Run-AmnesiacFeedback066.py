"""Current Amnesiac feedback cases against the actual packaged DLL, without launching the game."""
from pathlib import Path
import hashlib, json, os, re, subprocess, sys

project = Path(__file__).resolve().parents[1]
root = project.parents[1]
candidate, game, out = map(lambda p: Path(p).resolve(), sys.argv[1:4])
if out.exists():
    raise SystemExit('Output already exists; earlier results are retained.')
out.mkdir(parents=True)
runtime = root / 'work/release01466-runtime'
fixture = runtime / 'feedback-host'
host = fixture / 'project/build/PsychicTests/bin/Release/net472/PsychicTests.exe'
launcher = root / 'work/repair01456/engine-launcher/bin/Release/net472/EngineLauncher.exe'
engine = root / 'work/repair01456/engine02/Assembly-CSharp.dll'
native = root / 'work/prototype-build/psychic-completion01458-native.json'
evidence = root / 'work/psychic060/native-test.json'
umm_harmony = root / 'work/feedback01420/harmony236/0Harmony.dll'
managed = game / 'Wrath_Data/Managed'
sha = lambda p: hashlib.sha256(Path(p).read_bytes()).hexdigest().upper()
# The isolated host includes reviewed test-only fault injection. It must still
# match both its reviewed binary and the current source from which it derives.
reviewed = json.loads((runtime / 'reviewed-fixtures.json').read_text(encoding='utf-8'))['files']
for p, expected in reviewed.items():
    p = Path(p)
    if sha(p) != expected:
        raise SystemExit(f'Reviewed feedback fixture changed: {p}')
for p in (fixture / 'baseline-tests').glob('*.cs'):
    current = project / 'tests' / p.name
    if not current.is_file() or sha(current) != sha(p):
        raise SystemExit(f'Feedback host needs updating for changed test source: {p.name}')
cases = ['labels','capacity','ui','log','combatlog','growth','migration','manager','snapshot','logfault','uifault','logendpoint','uiendpoint']
minimum = [8,45,11,10,10,205,45,45,17,10,11,10,10]
rows = []
for name, extra in [('game204', None), ('umm236', umm_harmony)]:
    env = {k:v for k,v in os.environ.items() if not k.startswith('COTW_')}
    env.update(COTW_ARCANE_01446_ONLY='1', COTW_01454_CASE='feedback066all')
    report = out / (name + '.json')
    cmd = [launcher, engine.parent, host, managed, candidate, report, evidence, native]
    if extra:
        cmd.append(extra)
        env['COTW_HARMONY_MATRIX_PATH'] = str(extra)
    log = out / (name + '.log')
    with log.open('xb') as f:
        process = subprocess.run([str(x) for x in cmd], cwd=root, env=env, stdout=f, stderr=subprocess.STDOUT)
    text = log.read_text(encoding='utf-8-sig', errors='replace')
    counts = {}
    for case in cases:
        matches = re.findall(r'^FINAL066_CASE_PASS feedback066' + re.escape(case) + r' checks=(\d+)$', text, re.MULTILINE)
        if len(matches) == 1:
            counts[case] = int(matches[0])
    ok = (process.returncode == 0 and all(counts.get(c, 0) >= n for c,n in zip(cases,minimum))
          and text.count('FINAL066_FAULT_BOUNDARY EventBus.RaiseEvent<ILogMessageUIHandler> actual delegate present') == 1
          and (not extra or '054_RUNTIME236_PIN=1' in text))
    rows.append({'runtime':name, 'command':[str(x) for x in cmd], 'exitCode':process.returncode,
                 'counts':counts, 'passed':ok, 'log':str(log), 'logSHA256':sha(log)})
    if not ok:
        break
passed = len(rows) == 2 and all(r['passed'] for r in rows)
result = {'passed':passed, 'candidateSHA256':sha(candidate), 'rows':rows,
          'gameLaunched':False, 'ummLaunched':False,
          'scope':'Current feedback managed checks only; not player game acceptance.'}
(out / 'result.json').write_text(json.dumps(result, ensure_ascii=False, indent=2)+'\n', encoding='utf-8')
print('PASS' if passed else 'FAILED', out / 'result.json')
sys.exit(0 if passed else 1)

"""062 actual native-path contracts; controlled exposure is not a player repro."""
from pathlib import Path
import concurrent.futures,hashlib,json,subprocess,sys,uuid
R=Path(__file__).resolve().parents[3];W=R/'work/repair01462';B=R/'work/prototype-build'
main=Path(sys.argv[1]).resolve();out=Path(sys.argv[2]).resolve();both=len(sys.argv)>3 and sys.argv[3]=='both'
load=lambda p:json.loads(Path(p).read_text('utf-8-sig'))
sha=lambda p:hashlib.sha256(Path(p).read_bytes()).hexdigest().upper()
assert not out.exists() and out.is_relative_to(R)
host=B/'PsychicTests/bin/Release/net472/PsychicTests.exe';runner=W/'devrun062.py';old=W/'baseline/CallOfTheWild.Wrath.Prototype.dll'
assert sha(old)=='E3B44EE066E99746DFC89320F8AC51CC285FCBDB5F3AC647D3F58177DEA0AB44'
inputs={str(p):sha(p) for p in [main,host,runner,old,Path(__file__),B/'psychic-completion01458-native.json',W/'physical-fx-asset.json']}
prefix='required062-'+uuid.uuid4().hex
spec={
 'physical062':(24,['native','start','expiry']),
 'mimic062':(331,['native level-up parent pool','two tier first/second selectors','independent four-flag','exact pair native cast/context','original spell-slot payment','zero-point real ordinary cast','SpellcastingForbidden']),
 'isolation062':(28,['never enters global Mimic','unrelated native AutoMetamagic','composite native and Mimic','stale own registry entry','metadata fault actually reached','actual ordinary cast without Mimic charge','combined cast pays','original low slot payment'])}
controls={'physical062':'start','isolation062':'controlled original061 exposure is rejected'}
def execute(runtime,case,assembly,control=False):
 name=prefix+'-'+runtime+'-'+('real061-' if control else '')+case
 args=[sys.executable,'-X','utf8',str(runner),name,case,str(assembly)]
 if runtime=='umm236':args.append(str(R/'work/feedback01420/harmony236/0Harmony.dll'))
 q=subprocess.run(args,cwd=R,capture_output=True);receipt=W/(name+'.exit.json');e=load(receipt);log=W/(name+'.log');text=log.read_text('utf-8-sig')
 assert e['inputIntegrityPassed'] and e['actualNativeExitCode']==q.returncode
 if runtime=='umm236':assert '054_RUNTIME236_PIN=1' in text
 row={'runtime':runtime,'case':case,'mainSHA256':sha(assembly),'hostSHA256':sha(host),'exitCode':e['exitCode'],'execution':str(receipt),'executionSHA256':sha(receipt),'log':str(log),'logSHA256':sha(log)}
 if control:
  assert q.returncode==e['exitCode']==1 and controls[case] in text,(name,e)
  row.update(actual061ControlledRejection=True,notPlayerSceneReproduction=True);return row
 assert q.returncode==e['exitCode']==0,(name,e)
 result=W/(name+'.json');contract=load(result)['psychicFeedback01462Contracts'];minimum,phrases=spec[case]
 assert contract['passed'] and contract['assertions']==len(contract['coverage']) and contract['assertions']>=minimum,(case,contract['assertions'],minimum)
 for phrase in phrases:assert any(phrase.lower() in value.lower() for value in contract['coverage']),(case,phrase)
 row.update(result=str(result),resultSHA256=sha(result),contract=contract)
 print('PASS062',runtime,case,contract['assertions'],flush=True);return row
runtimes=['game204','umm236'] if both else ['game204']
with concurrent.futures.ThreadPoolExecutor(2) as pool:rows=list(pool.map(lambda p:execute(*p),[(r,c,main) for r in runtimes for c in spec]))
with concurrent.futures.ThreadPoolExecutor(2) as pool:negative=list(pool.map(lambda p:execute(*p),[(r,c,old,True) for r in runtimes for c in controls]))
assert all(sha(p)==h for p,h in inputs.items())
out.write_text(json.dumps(dict(passed=True,version='0.14.62',rows=rows,controls=negative,allThreeNamedGroups=True,allTwoActual061ControlsPerRuntime=True,inputsBeforeAndAfterIdentical=inputs,gameLaunchTested=False,playerSceneReproduced=False,repairClassification='Defensive native-global-query isolation; actual player exception unavailable. FX restores explicit original prefab.'),ensure_ascii=False,indent=2)+'\n','utf-8')

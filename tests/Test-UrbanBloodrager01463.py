"""Urban Bloodrager actual managed native paths; never a game-launch claim."""
from pathlib import Path
import json,hashlib,subprocess,sys,uuid,concurrent.futures
R=Path(__file__).resolve().parents[3];W=R/'work/batch01463';B=R/'work/prototype-build'
main=Path(sys.argv[1]).resolve();out=Path(sys.argv[2]).resolve();both=len(sys.argv)>3 and sys.argv[3]=='both'
sha=lambda p:hashlib.sha256(Path(p).read_bytes()).hexdigest().upper()
load=lambda p:json.loads(Path(p).read_text('utf-8-sig'))
host=B/'PsychicTests/bin/Release/net472/PsychicTests.exe';runner=W/'devrun063.py';catalogPath=R/'outputs/CallOfTheWild.Wrath.Prototype/package/Localization-QL/CallOfTheWildWrathPrototypeLocalized.json';old=R/'work/release01463/formal-attempt05/stopped-binaries/CallOfTheWild.Wrath.Prototype.dll'
assert sha(old)=='94555DE8BD392668D899A4439934FC19C3B3F4748594873BC9503E8DAB369BB8'
assert not out.exists() and out.is_relative_to(R)
inputs={str(p):sha(p) for p in [main,host,runner,old,catalogPath,Path(__file__),W/'CONTRACT.md',W/'dependency-decisions063.json',W/'official-heads063.json']}
prefix='required063-'+uuid.uuid4().hex
def execute(runtime,assembly,control=False):
 name=prefix+'-'+runtime+('-pre-title063' if control else '');args=[sys.executable,'-X','utf8',str(runner),name,'urban063',str(assembly)]
 if runtime=='umm236':args.append(str(R/'work/feedback01420/harmony236/0Harmony.dll'))
 q=subprocess.run(args,cwd=R,capture_output=True);receipt=W/(name+'.exit.json');e=load(receipt);log=W/(name+'.log');result=W/(name+'.json')
 assert q.returncode==e['exitCode']==e['actualNativeExitCode']==(1 if control else 0) and e['inputIntegrityPassed'],(name,e)
 text=log.read_text('utf-8-sig')
 if runtime=='umm236':assert '054_RUNTIME236_PIN=1' in text
 row=dict(runtime=runtime,case='urban063',exitCode=e['exitCode'],mainSHA256=sha(assembly),hostSHA256=sha(host),execution=str(receipt),executionSHA256=sha(receipt),log=str(log),logSHA256=sha(log))
 if control:
  assert 'authored toggle display name remains semantic English' in text and 'System.Exception: Magus01454:' in text
  row.update(actualPreRelease063TitleFailure=True,notPlayerSceneReproduction=True);return row
 contract=load(result)['urbanBloodrager01463Contracts']
 assert contract['passed'] and not contract['gameLaunchTested'] and contract['assertions']==len(contract['coverage']) and contract['assertions']>=360
 assert contract['nativeLearningPositivePaths']==13 and contract['nativeLearningNegativePaths']==7
 for phrase in ['replacement levels','actual native deactivation revokes','selected buff-versus-feature context','native removal revokes','native resource availability rejects','actual native Limitless Rage','real native parameter selection','actual native LearnSpellParametrized','native aura exit','saving-throw publisher','failed publication rolls back','equivalent external Urban root','occupied private GUID','real native relation changes','current native enemy','actual native aura round','native second relation change','actual RegisterAll/available caller executes','native Greater/Mighty child respects','children are removed','authored toggle display name remains semantic English']:
  assert any(phrase in row for row in contract['coverage']),phrase
 assets=contract['ownedBlueprints'];assert len(assets)==len({r['id'] for r in assets})==19
 assert len(contract['ownedTexts'])==34
 catalog=load(catalogPath);mapping={r['Key']:r for r in catalog}
 for key,value in contract['ownedTexts'].items():assert mapping[key]['enGB']==value and mapping[key]['zhCN']==value,(key,value,mapping[key])
 row.update(result=str(result),resultSHA256=sha(result),contract=contract);return row
runtimes=['game204','umm236'] if both else ['game204']
with concurrent.futures.ThreadPoolExecutor(2) as pool:rows=list(pool.map(lambda r:execute(r,main),runtimes))
with concurrent.futures.ThreadPoolExecutor(2) as pool:controls=list(pool.map(lambda r:execute(r,old,True),runtimes))
assert all(sha(p)==h for p,h in inputs.items())
out.write_text(json.dumps(dict(passed=True,version='0.14.63',rows=rows,controls=controls,selectedIds=['COTW-0026'],additionalMigrationItems=1,inputsBeforeAndAfterIdentical=inputs,gameLaunchTested=False),ensure_ascii=False,indent=2)+'\n','utf-8')
print('PASS Urban Bloodrager063',[(r['runtime'],r['contract']['assertions']) for r in rows],flush=True)

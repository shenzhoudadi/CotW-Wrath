"""064 actual native temporary knowledge and complete Amnesiac; no game launch."""
from pathlib import Path
import json,hashlib,subprocess,sys,uuid,concurrent.futures
R=Path(__file__).resolve().parents[3];W=R/'work/batch01464';B=R/'work/prototype-build'
main=Path(sys.argv[1]).resolve();out=Path(sys.argv[2]).resolve();both=len(sys.argv)>3 and sys.argv[3]=='both'
sha=lambda p:hashlib.sha256(Path(p).read_bytes()).hexdigest().upper()
load=lambda p:json.loads(Path(p).read_text('utf-8-sig'))
host=B/'PsychicTests/bin/Release/net472/PsychicTests.exe';runner=W/'devrun064.py';catalog=R/'outputs/CallOfTheWild.Wrath.Prototype/package/Localization-QL/CallOfTheWildWrathPrototypeLocalized.json';old=R/'work/release01464/baseline063/zip-dll/CallOfTheWild.Wrath.Prototype.dll'
preLedger=W/'dev-attempts/temporary-native05/CallOfTheWild.Wrath.Prototype.dll'
preStart=R/'work/release01464/formal-attempt02/stopped-binaries/CallOfTheWild.Wrath.Prototype.dll'
assert sha(old)=='15ABEAFCAC75EABA86BBF2F6D794383379AE5D3E88891BA35E5DBC8201C8420C'
assert sha(preLedger)=='C15863579F11FA2B91BE01CCD8D3D96233030708251E155D6C17184CF91A7032'
assert sha(preStart)=='D7E3724D49E0799CC82A839899726DE8E64BEBBF9199ECB2DCB567AA06B4097A'
assert not out.exists() and out.is_relative_to(R)
inputs={str(p):sha(p) for p in [main,host,runner,old,preLedger,preStart,catalog,Path(__file__),W/'AMNESIAC_CONTRACT.md',W/'dependency-decisions064.json',W/'official-heads064.json',W/'amnesiac-table-oracle064.json']}
prefix='required064-'+uuid.uuid4().hex
def execute(args):
 runtime,case,control=args;name=prefix+'-'+runtime+'-'+case+('-actual-pre-start064' if control=='start' else '-actual-pre-ledger064' if control=='ledger' else '-actual063' if control else '')
 assembly=preStart if control=='start' else preLedger if control=='ledger' else old if control else main
 command=[sys.executable,'-X','utf8',str(runner),name,case,str(assembly)]
 if runtime=='umm236':command.append(str(R/'work/feedback01420/harmony236/0Harmony.dll'))
 q=subprocess.run(command,cwd=R,capture_output=True);receipt=W/(name+'.exit.json');e=load(receipt);log=W/(name+'.log');result=W/(name+'.json')
 assert q.returncode==e['exitCode']==e['actualNativeExitCode']==(1 if control else 0) and e['inputIntegrityPassed'],(name,e)
 text=log.read_text('utf-8-sig')
 if runtime=='umm236':assert '054_RUNTIME236_PIN=1' in text
 row=dict(runtime=runtime,case=case,exitCode=e['exitCode'],mainSHA256=sha(assembly),hostSHA256=sha(host),execution=str(receipt),executionSHA256=sha(receipt),log=str(log),logSHA256=sha(log))
 if control:
  if control=='start':
   assert '064 recalled knowledge must be cleared before native StartController reaches its construction endpoint' in text and 'System.Exception: Magus01454:' in text
   return dict(row,actualPreStartController064Negative=True,developmentDLLNotDelivered063=True,notPlayerSceneReproduction=True)
  if control=='ledger':
   assert '064 removing stale native source cannot delete a permanently relearned replacement object: 3' in text and 'System.Exception: Magus01454:' in text
   return dict(row,actualPreLedger064Negative=True,developmentDLLNotDelivered063=True,notPlayerSceneReproduction=True)
  assert '064 native source ledger grants the actual high spell as temporary knowledge' in text and 'System.Exception: Magus01454:' in text
  return dict(row,actualDelivered063Negative=True,notPlayerSceneReproduction=True)
 c=load(result)['temporaryKnowledge01464Contracts' if case=='temporary064' else 'amnesiac01464Contracts']
 assert c['passed'] and c['assertions']==len(c['coverage']) and not c['gameLaunchTested']
 if case=='temporary064':
  assert c['assertions']>=54
  for phrase in ['two preview generations','independent native source','PostLoad','promotes','another actor','permanently relearned replacement object']:
   assert any(phrase in s for s in c['coverage']),phrase
 else:
  assert c['assertions']>=841 and c['capacityCells']==180 and c['rollCases']==12 and c['d100Calls']==12 and c['d10Calls']==4
  for phrase in ['same native actor class gain','serialized capacity state','actual RegisterAll','actual RegisterOptional','accepted spell donor','late catalogue cache failure','late real spell catalogue','occupied private GUID','failed publication rolls back','twenty-one','native one-round','ordinary native spellbook slot','before native StartController reaches its construction endpoint','actual preview CopySpellbook after StartController entry','single native start-event dispatch']:
   assert any(phrase in s for s in c['coverage']),phrase
  assets=c['ownedBlueprints'];assert len(assets)==len({r['id'] for r in assets})==397 and len(c['ownedTexts'])==1163
  mapping={r['Key']:r for r in load(catalog)}
  for k,v in c['ownedTexts'].items():
   assert 'CotWWrathPsychic' not in v and mapping[k]['enGB']==mapping[k]['zhCN']==v,(k,v)
 row.update(result=str(result),resultSHA256=sha(result),contract=c);return row
runtimes=['game204','umm236'] if both else ['game204']
with concurrent.futures.ThreadPoolExecutor(2) as pool:rows=list(pool.map(execute,[(r,c,False) for r in runtimes for c in ['temporary064','amnesiac064']]))
with concurrent.futures.ThreadPoolExecutor(2) as pool:controls=list(pool.map(execute,[job for r in runtimes for job in [(r,'temporary064',True),(r,'temporary064','ledger'),(r,'amnesiac064','start')]]))
assert all(sha(p)==h for p,h in inputs.items())
out.write_text(json.dumps(dict(passed=True,version='0.14.64',rows=rows,controls=controls,selectedIds=['COTW-0080'],additionalMigrationItems=1,inputsBeforeAndAfterIdentical=inputs,gameLaunchTested=False),ensure_ascii=False,indent=2)+'\n','utf-8')
print('PASS Amnesiac064',[(r['runtime'],r['case'],r['contract']['assertions']) for r in rows],flush=True)

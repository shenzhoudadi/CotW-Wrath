"""Required standalone-native054 suite; no game/UMM launch, no installed files.
Uses the checked test-only adapters and exact native archive in work/batch01454.
All logs, failed runs and per-run source/input snapshots are retained.
"""
from pathlib import Path
import sys,subprocess,json,hashlib,uuid,concurrent.futures
R=Path(__file__).resolve().parents[3];W=R/'work/psychic01458';B=R/'work/prototype-build'
main=Path(sys.argv[1]).resolve();out=Path(sys.argv[2]).resolve();both=len(sys.argv)>3 and sys.argv[3]=='both'
assert not out.exists() and out.is_relative_to(R)
def sha(p):return hashlib.sha256(Path(p).read_bytes()).hexdigest().upper()
def load(p):return json.loads(Path(p).read_text('utf-8-sig'))
prefix='required-'+uuid.uuid4().hex;adapter='whip-'+prefix
subprocess.run([sys.executable,str(W/'prepare_whip_adapter.py'),adapter],cwd=R,check=True)
auditlog=W/(prefix+'-access-audit.log')
with auditlog.open('wb') as f:subprocess.run(['dotnet',str(W/'whip-access-audit/bin/Release/net9.0/WhipAccessAudit01454.dll'),str(R),adapter],cwd=R,stdout=f,stderr=subprocess.STDOUT,check=True)
audit=load(W/adapter/'access-adapter-audit.json');assert audit['passed'] and [r['methods'] for r in audit['rows']]==[2604,740]
engine=load(W/'engine02/audit.json');assert engine['passed'] and engine['methods']==82209 and engine['allOtherPEBytesExact'] and engine['allMemberIdentitiesAndMetadataTokensExact']
for row in audit['rows']:
 for k in ['input','output','host']:assert sha(row[k])==row[k+'SHA256']
host=B/'PsychicTests/bin/Release/net472/PsychicTests.exe';bound={str(p):sha(p) for p in [main,host,Path(__file__),W/'engine02/Assembly-CSharp.dll',W/'engine02/audit.json',W/'native-archive-index.json',W/'native-clr-fixture-provenance.json',W/'engine-launcher/bin/Release/net472/EngineLauncher.exe']}
required={
 'structural':(20,['native original archetypes retained','every actual Magus level/circle','original Mindblade replacement levels','no seventh+']),
 'mechanics':(97,['actual native Spell Combat mode survives','actual native LearnSpellParametrized','paired Bane pays','native resource override','actual component post-load hook','native parametrized selection','fixed identity is hashed once']),
 'skirnir':(74,['actual native GetConversions','forged different spell','native bonded restoration restores','actual native MagusController accepts shield bash','actual native Enduring Blade','retains buckler AC','native brilliant-energy']),
 'whip':(43,['real provider creates','Hurricane rollback restores','real optional dispatcher retries','actual native controller, mode and Gather Power','AbilityKineticist.Spend','actual native empower','external activation marker','reactivation refuses a mismatched','missing persisted activation-marker']),
 'publication':(35,['both actual registration orders','foreign occupied identity','actual optional dispatcher isolates','actual native external-parent prerequisite','actual late optional caller'])}
def run(pair):
 runtime,case=pair;name=prefix+'-'+runtime+'-'+case;args=[sys.executable,str(W/'devrun054.py'),name,case,str(main),adapter]
 if runtime=='umm236':args.append(str(R/'work/feedback01420/harmony236/0Harmony.dll'))
 process=subprocess.run(args,cwd=R,stdout=subprocess.PIPE,stderr=subprocess.STDOUT)
 e=load(W/(name+'.exit.json'));assert process.returncode==e['exitCode']==0,(name,process.stdout.decode(errors='replace'))
 result=W/(name+'.json');j=load(result)['magusBatch01454Contracts'];minimum,phrases=required[case]
 assert j['passed'] and j['case']==case and j['assertions']>=minimum and len(j['coverage'])==j['assertions'],name
 for phrase in phrases:assert any(phrase in x for x in j['coverage']),(name,phrase)
 log=W/(name+'.log')
 if runtime=='umm236':assert '054_RUNTIME236_PIN=1' in log.read_text('utf-8-sig')
 row={'runtime':runtime,'case':case,'exitCode':0,'mainSHA256':sha(main),'hostSHA256':sha(host),'result':str(result),'resultSHA256':sha(result),'log':str(log),'logSHA256':sha(log),'execution':str(W/(name+'.exit.json')),'executionSHA256':sha(W/(name+'.exit.json')),'contract':j}
 print('PASS054',runtime,case,j['assertions'],flush=True);return row
with concurrent.futures.ThreadPoolExecutor(max_workers=2) as pool:rows=list(pool.map(run,[(r,c) for r in (['game204','umm236'] if both else ['game204']) for c in required]))
assert all(sha(p)==h for p,h in bound.items())
owned={}
for row in rows:
 for bp in row['contract']['ownedBlueprints']:
  if bp['id'] in owned:assert owned[bp['id']]==bp
  owned[bp['id']]=bp
native=load(W/'native-archive-index.json');assert not set(owned).intersection(native)
assert not any(x['type']=='Kingmaker.UnitLogic.Abilities.Blueprints.BlueprintAbility' and 'SpellRoot' in x['name'] for x in owned.values())
report={'passed':True,'completeScopeContracts':True,'selectedIds':['COTW-0316','COTW-0130','COTW-0131'],'additionalMigrationItemCount':3,'rows':rows,'requiredCaseCountPerRuntime':5,'allRequiredNamedNativePaths':True,'ownedBlueprints':sorted(owned.values(),key=lambda x:x['id']),'nativeGUIDCollisions':[],'adapterAudit':str(W/adapter/'access-adapter-audit.json'),'adapterAuditSHA256':sha(W/adapter/'access-adapter-audit.json'),'inputsBeforeAndAfterIdentical':bound,'gameLaunchTested':False,'boundary':'Actual native managed methods, original native blueprint fields and real released DarkCodex entry execute. Test-only same-member provider access bridges and one playing=true item-cache engine endpoint are audited separately. Localization, FX rendering, scene/UI/save service inputs and terminal spell casts are controlled. TTT parent integration uses the official current member shape, not its full initializer; full game targeting/rendering/turns/save lifecycle remain tester observations.'}
out.parent.mkdir(parents=True,exist_ok=True);out.write_text(json.dumps(report,ensure_ascii=False,indent=2)+'\n','utf-8');print('054 mandatory native suites complete:',out,flush=True)

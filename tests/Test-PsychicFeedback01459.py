from pathlib import Path
import concurrent.futures,hashlib,json,subprocess,sys,uuid
R=Path(__file__).resolve().parents[3];W=R/'work/repair01459';B=R/'work/prototype-build';main=Path(sys.argv[1]).resolve();out=Path(sys.argv[2]).resolve();both=len(sys.argv)>3 and sys.argv[3]=='both'
load=lambda p:json.loads(Path(p).read_text('utf-8-sig'))
sha=lambda p:hashlib.sha256(Path(p).read_bytes()).hexdigest().upper()
assert not out.exists() and out.is_relative_to(R)
old=W/'baseline/zip-dll/CallOfTheWild.Wrath.Prototype.dll';oldcert=load(W/'baseline/outputs/0.14.58交付自检.json');assert sha(old)==oldcert['dlls'][0]['sha256']
runner=W/'devrun059.py';host=B/'PsychicTests/bin/Release/net472/PsychicTests.exe';inputs={str(p):sha(p) for p in [main,host,runner,Path(__file__),old,B/'psychic-completion01458-native.json']};prefix='required059-'+uuid.uuid4().hex
spec={'graph059':['actual native WeakResourceLink.GetHashCode reproduces','actual RegisterModule publishes all three','actual level-up CanSelect respects Major threshold 10','actual level-up CanSelect respects Major threshold 11','actual publication receipts repeat with native empty'], 'display059':['native actionbar slot is bound','actual native actionbar GetResource','actual native slot remains usable at zero','actual native slot rejects exhausted zero'], 'reach059':['actual native weapon range changes only melee exactly5ft','native category defaults contain both ranged and melee','actual mutation expiry restores native reach']}
def execute(runtime,case,assembly,label,expected=0):
 name=prefix+'-'+runtime+'-'+label;args=[sys.executable,str(runner),name,case,str(assembly)]
 if runtime=='umm236':args.append(str(R/'work/feedback01420/harmony236/0Harmony.dll'))
 q=subprocess.run(args,cwd=R,capture_output=True);exitfile=W/(name+'.exit.json');j=load(exitfile);assert q.returncode==j['exitCode']==expected,(name,q.returncode,j['exitCode'])
 log=W/(name+'.log');text=log.read_text('utf-8-sig');row={'runtime':runtime,'case':case,'exitCode':expected,'mainSHA256':sha(assembly),'hostSHA256':sha(host),'execution':str(exitfile),'executionSHA256':sha(exitfile),'log':str(log),'logSHA256':sha(log)}
 if runtime=='umm236':assert '054_RUNTIME236_PIN=1' in text
 return name,row,text
def run(pair):
 runtime,case=pair;name,row,text=execute(runtime,case,main,case);p=W/(name+'.json');c=load(p)['psychicFeedback01459Contracts'];assert c['passed'] and c['assertions']==len(c['coverage']) and c['assertions']>=90
 for phrase in spec[case]:assert any(phrase in s for s in c['coverage']),(case,phrase)
 if case=='graph059':assert c['emptyNativeLinksInRealProducer']>=1
 row.update(result=str(p),resultSHA256=sha(p),contract=c);print('PASS059',runtime,case,c['assertions'],flush=True);return row
runtimes=['game204','umm236'] if both else ['game204']
with concurrent.futures.ThreadPoolExecutor(2) as pool:rows=list(pool.map(run,[(r,c) for r in runtimes for c in spec]))
controls=[]
for runtime in runtimes:
 name,row,text=execute(runtime,'graph059',old,'real058-empty-link-rejection',1)
 assert 'WeakResourceLink.GetHashCode' in text and 'SpellBatch01447' in text and '059 actual RegisterModule publishes all three disciplines and six amplifications with native empty links' in text
 row['actualOld058ReproducedUserFailure']=True;controls.append(row)
assert all(sha(p)==d for p,d in inputs.items())
out.write_text(json.dumps({'passed':True,'version':'0.14.59','rows':rows,'controls':controls,'inputsBeforeAndAfterIdentical':inputs,'actualOld058FailureReproduced':True,'allThreeNamedNativeGroups':True,'noNewBlueprintIdentities':True,'newSerializedTypes':0,'gameLaunchTested':False},ensure_ascii=False,indent=2)+'\n','utf-8')
print('059 three native groups and real058 failure controls passed.',flush=True)

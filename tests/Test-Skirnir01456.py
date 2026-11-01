from pathlib import Path
import json,hashlib,sys,subprocess,uuid,concurrent.futures
R=Path(__file__).resolve().parents[3];W=R/'work/repair01456';B=R/'work/prototype-build';main=Path(sys.argv[1]).resolve();out=Path(sys.argv[2]).resolve();both=len(sys.argv)>3 and sys.argv[3]=='both'
def sha(p):return hashlib.sha256(Path(p).read_bytes()).hexdigest().upper()
def load(p):return json.loads(Path(p).read_text('utf-8-sig'))
assert not out.exists();old=W/'baseline/zip-dll/CallOfTheWild.Wrath.Prototype.dll';c=load(W/'baseline/root/outputs/0.14.55交付自检.json');assert sha(old)==c['dlls'][0]['sha256']
inspect=subprocess.run(['dotnet',str(R/'work/api-audit/Inspector/bin/Release/net9.0/Inspector.dll'),'names',str(main),'Skirnir01454'],capture_output=True,text=True,encoding='utf-8-sig',check=True).stdout;assert any('Version='+v+'.0' in inspect for v in ['0.14.56','0.14.57','0.14.58','0.14.59','0.14.60','0.14.61','0.14.62','0.14.63'])
host=B/'PsychicTests/bin/Release/net472/PsychicTests.exe';runner=W/'devrun.py';inputs={str(p):sha(p) for p in [main,old,host,runner,Path(__file__),W/'feedback.json']};prefix='required056-'+uuid.uuid4().hex
def run(pair):
 runtime,prior=pair;target=old if prior else main;name=prefix+'-'+runtime+('-old055' if prior else '-current');args=[sys.executable,str(runner),name,'skirnir056',str(target)]
 if runtime=='umm236':args.append(str(R/'work/feedback01420/harmony236/0Harmony.dll'))
 q=subprocess.run(args,cwd=R,capture_output=True);exe=W/(name+'.exit.json');e=load(exe);assert q.returncode==e['exitCode']==0,(name,q.stdout.decode(errors='replace'),q.stderr.decode(errors='replace'))
 result=W/(name+'.json');obs=W/(name+'.observation.json');log=W/(name+'.log');v=load(obs);contract=load(result)['magusBatch01454Contracts'];assert contract['passed'] and contract['case']=='skirnir056' and contract['assertions']==len(contract['coverage'])
 assert v['rules056']==(not prior) and v['nativeGreaterId']=='379887a82a7248946bbf6d0158663b5e'
 assert [r['level'] for r in v['levels']]==[7,8,14,18,19,20] and all(r['nativeGreaterSpellCombat']==(r['level']>=19 and not prior) and r['count']==int(r['level']>=19 and not prior) for r in v['levels'])
 assert len(v['shield'])==4 and all(r['sheetAC']==12 and r['actualAC']==(10 if prior else 12) and r['automaticPenalty']==prior for r in v['shield'])
 assert v['nativeOldNodePostLoadChecked'] and v['nativeConcentrationCallbackChecked'] and v['externalOwnershipPreserved']
 row={'runtime':runtime,'old055Control':prior,'exitCode':0,'mainSHA256':sha(target),'hostSHA256':sha(host),'contract':contract}
 for k,p in [('result',result),('observation',obs),('log',log),('execution',exe)]:row[k]=str(p);row[k+'SHA256']=sha(p)
 print('PASS056',runtime,'old055' if prior else 'current',contract['assertions'],flush=True);return row
with concurrent.futures.ThreadPoolExecutor(2) as pool:rows=list(pool.map(run,[(runtime,prior) for runtime in (['game204','umm236'] if both else ['game204']) for prior in [False,True]]))
assert all(sha(p)==h for p,h in inputs.items())
out.write_text(json.dumps({'passed':True,'version':'0.14.56','rows':rows,'old055Control':True,'nativeGreaterGrantedExactlyOnce':True,'legacyPenaltyInert':True,'newMigrationItems':0,'inputsBeforeAndAfterIdentical':inputs,'gameLaunchTested':False},ensure_ascii=False,indent=2)+'\n','utf-8');print('056 required native policy contracts passed.',flush=True)

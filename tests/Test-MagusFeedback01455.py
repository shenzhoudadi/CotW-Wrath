"""Mandatory 055 feedback contracts, with the actual 054 installer DLL controls.
Runs managed native paths only. No game/UMM launch or installation.
"""
from pathlib import Path
import sys,subprocess,json,hashlib,uuid,concurrent.futures
R=Path(__file__).resolve().parents[3];W=R/'work/repair01456';B=R/'work/prototype-build'
main=Path(sys.argv[1]).resolve();out=Path(sys.argv[2]).resolve();both=len(sys.argv)>3 and sys.argv[3]=='both'
assert not out.exists() and out.is_relative_to(R)
def sha(p):return hashlib.sha256(Path(p).read_bytes()).hexdigest().upper()
def load(p):return json.loads(Path(p).read_text('utf-8-sig'))
old=R/'work/repair01455/baseline/zip-dll/CallOfTheWild.Wrath.Prototype.dll';assert sha(old)=='08322F4B34291C81488A2EF15405BF412E877328CCEF5EA65878637B13424088'
draft=R/'work/repair01455/formal-attempt01/stopped-binaries/CallOfTheWild.Wrath.Prototype.dll';assert sha(draft)=='4C399A08DD4200C7CFB970CD9FAAA6E7FE86DFD3375BB2C21B4D546BD06CEA2B'
inspect=subprocess.run(['dotnet',str(R/'work/api-audit/Inspector/bin/Release/net9.0/Inspector.dll'),'names',str(main),'MindbladeModeGrant01455'],capture_output=True,text=True,encoding='utf-8-sig',check=True).stdout
assert ('Version=0.14.55.0' in inspect or 'Version=0.14.56.0' in inspect or 'Version=0.14.57.0' in inspect or 'Version=0.14.58.0' in inspect or 'Version=0.14.59.0' in inspect or 'Version=0.14.60.0' in inspect or 'Version=0.14.61.0' in inspect or 'Version=0.14.62.0' in inspect) and 'MindbladeModeGrant01455' in inspect
host=B/'PsychicTests/bin/Release/net472/PsychicTests.exe';runner=W/'devrun.py';prefix='required055-'+uuid.uuid4().hex
inputs={str(p):sha(p) for p in [main,old,draft,host,runner,Path(__file__),W/'feedback.json',W/'baseline.json',R/'work/batch01454/engine02/Assembly-CSharp.dll',R/'work/batch01454/engine02/audit.json',R/'work/batch01454/native-archive-index.json']}
cases=['feedback055','feedback055-store','feedback055-shield','feedback055-icons'];controls=['feedback055','feedback055-store','feedback055-icons']
def run(case):
 runtime,scenario,prior,badDraft=case;binary=draft if badDraft else old if prior else main;name=prefix+'-'+runtime+'-'+scenario+('-bad-source-keys' if badDraft else '-old054' if prior else '')
 args=[sys.executable,str(runner),name,scenario,str(binary)]
 if runtime=='umm236':args.append(str(R/'work/feedback01420/harmony236/0Harmony.dll'))
 q=subprocess.run(args,cwd=R,capture_output=True);execution=W/(name+'.exit.json');e=load(execution)
 expectedExit=1 if badDraft else 0;assert q.returncode==e['exitCode']==expectedExit,(name,q.stdout.decode(errors='replace'),q.stderr.decode(errors='replace'))
 result=W/(name+'.json');observation=W/(name+'.observation.json');v=load(observation);log=W/(name+'.log')
 if badDraft:
  guard='native serialized Dual Weapons source names match actual054 runtime keys';assert guard in log.read_text('utf-8-sig') and v['legacySourceKeys']['expected']==['CotW025AddFeatureOnClassLevel','CotW025AddFeatureOnClassLevel1'] and v['legacySourceKeys']['actual']==['CotW025MindbladeModeGrant01455','CotW025MindbladeModeGrant014551']
  j={'passed':True,'actualRejectedCandidateExitCode':1,'exactSourceKeyGuard':guard,'expectedLegacyKeys':v['legacySourceKeys']['expected'],'actualBadDraftKeys':v['legacySourceKeys']['actual'],'pinnedBadDraftSHA256':sha(draft)}
  rawResult=result;result=W/(name+'.expected-rejection.json');assert not result.exists();result.write_text(json.dumps({'testPassed':True,'actualNativeProcessExitCode':1,'expectedExitCode':1,'proof':j,'nativeResultProduced':rawResult.exists(),'nativeResult':str(rawResult) if rawResult.exists() else None,'log':str(log),'logSHA256':sha(log),'execution':str(execution),'executionSHA256':sha(execution),'observation':str(observation),'observationSHA256':sha(observation)},indent=2)+'\n','utf-8')
  row={'runtime':runtime,'case':scenario,'old054NegativeControl':False,'badDraftSourceKeysControl':True,'resultKind':'expected-rejection-proof','exitCode':1,'expectedExitCode':1,'mainSHA256':sha(binary),'hostSHA256':sha(host),'result':str(result),'resultSHA256':sha(result),'log':str(log),'logSHA256':sha(log),'execution':str(execution),'executionSHA256':sha(execution),'observation':str(observation),'observationSHA256':sha(observation),'contract':j};print('PASS055',runtime,'pinned bad draft rejected by actual source-key guard',flush=True);return row
 j=load(result)['magusBatch01454Contracts']
 assert j['passed'] and j['case']==scenario and j['assertions']==len(j['coverage'])
 if scenario=='feedback055':
  a=v['mindbladeAfterNativeListReconciliation'];assert a['beforeRows']==(10 if prior else 11) and a['afterRows']==11
  assert bool(a['error'])==prior and all(r['restoredToggles']==(0 if prior else 1) for r in v['nativeMissingGrantPostLoad'])
  assert v['problems']==[] and v['level7To12NativeEvent']==[{'name':'CotWWrathPsychicMindbladeDualMode2Toggle01454','hidden':False,'weight':2}]
  assert v['legacySourceKeys']['actual']==v['legacySourceKeys']['expected']==['CotW025AddFeatureOnClassLevel','CotW025AddFeatureOnClassLevel1']
  assert [r['level'] for r in v['nativeSavedGrantDataRebind']]==[7,12] and all(r['nativePostLoadDataIdentityPreserved'] and r['ownedChildReferencesPreserved'] and r['activeModeAndExactFactsPreserved'] for r in v['nativeSavedGrantDataRebind'])
  assert any('actual native popup enables owned dual-form leaf' in x for x in j['coverage']) and any('native post-load' in x for x in j['coverage'])
 elif scenario=='feedback055-store':
  assert v['noShieldGuard'] and v['emptyPoolGuard'];rows=v['storageUiProbe'];assert [(r['spell'],r['circle']) for r in rows]==[('MagicMissile',1),('ShockingGraspCast',1),('FrigidTouchCast',2)]
  for r in rows:
   assert 'uiBoundaryError' not in r and r['nativeInitRejected']==prior and r['ownedAbilityFact']==(not prior)
   if prior:assert r['rejectionPreservesSlotAndPool']
   else:assert r['nativeOnActionResult']=='Success' and r['scheduled'] and r['nativeContextParamsSame'] and r['stored'] and r['slotConsumed'] and r['storedBookSame'] and r['storedShieldSame'] and r['paid']==r['circle'] and r['storedCasterLevel']==8
 elif scenario=='feedback055-shield':
  assert [r['improvedShieldBash'] for r in v['shield']]==[False,True]
  policy056=any('Version='+v+'.0' in inspect for v in ['0.14.56','0.14.57','0.14.58','0.14.59','0.14.60','0.14.61','0.14.62'])
  assert all('error' not in r and r['penaltyPresent']==(not policy056) and (r['sheetAC'],r['penalty'],r['actualAC'])==((12,0,12) if policy056 else (12,-2,10)) for r in v['shield'])
 elif scenario=='feedback055-icons':
  assert v['foreignConversionUnchanged'] and [r['category'] for r in v['nativePopupIcons']]==['Dagger','Longsword','DoubleSword']
  assert all(r['selectedLeafIcon']==(not prior) and r['parentIcon']==prior for r in v['nativePopupIcons'])
 row={'runtime':runtime,'case':scenario,'old054NegativeControl':prior,'badDraftSourceKeysControl':False,'exitCode':0,'expectedExitCode':0,'mainSHA256':sha(binary),'hostSHA256':sha(host),'result':str(result),'resultSHA256':sha(result),'log':str(log),'logSHA256':sha(log),'execution':str(execution),'executionSHA256':sha(execution),'observation':str(observation),'observationSHA256':sha(observation),'contract':j}
 print('PASS055',runtime,scenario,'old054' if prior else 'current',j['assertions'],flush=True);return row
matrix=[(runtime,scenario,False,False) for runtime in (['game204','umm236'] if both else ['game204']) for scenario in cases]+[(runtime,scenario,True,False) for runtime in (['game204','umm236'] if both else ['game204']) for scenario in controls]+[(runtime,'feedback055',False,True) for runtime in (['game204','umm236'] if both else ['game204'])]
with concurrent.futures.ThreadPoolExecutor(max_workers=2) as pool:rows=list(pool.map(run,matrix))
assert all(sha(p)==h for p,h in inputs.items())
report={'passed':True,'version':'0.14.55','newMigrationItems':0,'allRequestedNativePaths':True,'currentCasesPerRuntime':4,'old054NegativeControlsPerRuntime':3,'badDraftSourceKeysControlsPerRuntime':1,'rows':rows,'inputsBeforeAndAfterIdentical':inputs,'gameLaunchTested':False,'shieldACProductionUnchanged':not any('Version='+v+'.0' in inspect for v in ['0.14.56','0.14.57','0.14.58','0.14.59','0.14.60','0.14.61','0.14.62']),'shieldACPolicy056':any('Version='+v+'.0' in inspect for v in ['0.14.56','0.14.57','0.14.58','0.14.59','0.14.60','0.14.61','0.14.62']),'noSourceOwnershipOrQualificationBypass':True,'boundary':'Native UI filtering/click, Init ownership rejection, OnAction, rule/event and DoApplyEffect, real preparations/equipment/resources/AC execute. Actual native EntityFact.PostLoad rebinds054 runtime keys and retains native data/owned-child records; a pinned stopped draft with changed keys is explicitly rejected (raw exit1). Sound/warning/FX rendering, self-approach radius and scene scheduling are controlled. The no-book payment boundary asserts null book, no item and own storage marker, then uses native material/resource spending; native selected SpellSlot.Spend remains in production effect. Sprite identity tests execute native popup selection, not pixel rendering. No complete game/save/turn observation.'}
out.parent.mkdir(parents=True,exist_ok=True);out.write_text(json.dumps(report,ensure_ascii=False,indent=2)+'\n','utf-8');print('055 mandatory feedback and actual old-installer controls complete:',out,flush=True)

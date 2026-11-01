"""Required060 native paths and exact delivered059 negative controls; no game launch."""
from pathlib import Path
import concurrent.futures,hashlib,json,subprocess,sys,uuid
R=Path(__file__).resolve().parents[3];W=R/'work/repair01460';B=R/'work/prototype-build';main=Path(sys.argv[1]).resolve();out=Path(sys.argv[2]).resolve();both=len(sys.argv)>3 and sys.argv[3]=='both'
load=lambda p:json.loads(Path(p).read_text('utf-8-sig'))
sha=lambda p:hashlib.sha256(Path(p).read_bytes()).hexdigest().upper()
assert not out.exists() and out.is_relative_to(R)
host=B/'PsychicTests/bin/Release/net472/PsychicTests.exe';runner=W/'devrun060.py';old=W/'baseline/zip-dll/CallOfTheWild.Wrath.Prototype.dll'
assert sha(old)==load(W/'baseline/outputs/0.14.59交付自检.json')['dlls'][0]['sha256']
inputs={str(p):sha(p) for p in [host,main,runner,old,Path(__file__),B/'psychic-completion01458-native.json']};prefix='required060-'+uuid.uuid4().hex
spec={
 'psychedelia060':(46,['outside combat','retaliates against allied harmful spell irrespective of save','actual Warped retaliation nausea follows','friendly helpful Heroism and self-cast','actual native AddAreaEffect.OnActivate reaches allocation','native area activation recreates','native AddFacts old-character postload restores','native area entry from real host context rolls Will','source-owned24h immunity']),
 'wound-major060':(88,['actual native actionbar slot displays wound resource','native menu condition selection spends no wound uses','native AddFacts old-state repair restores','entirely new component data missing from059','native CanSelect Major eligibility at 10','native CanSelect Major eligibility at 11','actual native class-level gain event enables','all three new serialized component/action','exactly eight declared additive blueprint identities']),
 'synaptic060':(41,['reserves the final Synaptic point','native failed Synaptic Pulse postpones confusion','actual native timed stun expiry triggers','successful linked-spell save applies immediate confusion','native save converters restore exact live watched buff references','first status expiry cannot confuse','actual last source dispel','actual native PostLoad reconciles an off pending link','unselected victim cannot consume','preserves another caster']),
 'surge060':(128,['actual temporary undercast version: MindThrust','actual temporary undercast version: MentalBarrier','actual temporary undercast version: ThoughtShield','actual promoted Mind Thrust effect deals native HP damage','actual promoted defensive spell applies the higher real buff bonus','native cast pays exactly twice the promotion','Spellbook.Spend consumes exactly one lower slot','never exceeds the highest permanently known','insufficient points cannot silently create','mode-off rejects a previously offered promotion']),
 'localization060':(43,['every existing native language-pack key and exact English text','rejects internal test blueprint-name labels','actual published text is present in QL export','complete QL array adds runtime fallback keys','real optional QL reader and native pack preserve Chinese','preserves foreign translations and intentionally empty','cannot overwrite an unrelated module key','loaded-pack locale switches','malformed/duplicate optional QL source is isolated','null Chinese falls back to exact English'])}
negative={'psychedelia060':['060 real native spell publisher retaliates against allied harmful spell'],'aura-control060':['System.NullReferenceException','Kingmaker.UnitLogic.Buffs.Components.AddAreaEffect.OnActivate'],'synaptic060':['060 real linked cast reserves the final Synaptic point'],'surge060':['System.InvalidOperationException','Surge060']}
def execute(runtime,case,assembly,expected):
 name=prefix+'-'+runtime+'-'+('real059-' if expected else '')+case;args=[sys.executable,'-X','utf8',str(runner),name,case,str(assembly)]
 if runtime=='umm236':args.append(str(R/'work/feedback01420/harmony236/0Harmony.dll'))
 q=subprocess.run(args,cwd=R,capture_output=True);receipt=W/(name+'.exit.json');e=load(receipt);assert q.returncode==e['exitCode']==expected,(name,q.returncode,e['exitCode'])
 log=W/(name+'.log');text=log.read_text('utf-8-sig')
 if runtime=='umm236':assert '054_RUNTIME236_PIN=1' in text
 row={'runtime':runtime,'case':case,'exitCode':expected,'mainSHA256':sha(assembly),'hostSHA256':sha(host),'execution':str(receipt),'executionSHA256':sha(receipt),'log':str(log),'logSHA256':sha(log)}
 if expected:
  for phrase in negative[case]:assert phrase in text,(case,phrase)
  row['actualDelivered059RejectedBySameNativePath']=True;return row
 result=W/(name+'.json');data=load(result);c=data['psychicFeedback01460Contracts'];minimum,phrases=spec[case]
 assert c['passed'] and c['case']==case and c['assertions']==len(c['coverage']) and c['assertions']>=minimum
 for phrase in phrases:
  if data.get('freshSaveRetirementPolicy061') and phrase in ['all three new serialized component/action','exactly eight declared additive blueprint identities']:
   required='all retained060 serialized identities complete native polymorphic roundtrip' if phrase.startswith('all three') else '061 exact superseded wound shortcut is absent'
   assert any(required in value for value in c['coverage']),(case,required)
  elif phrase=='mode-off rejects a previously offered promotion':
   assert all(any(('mode-off rejects an already constructed promotion' in value or phrase in value) and family in value for value in c['coverage']) for family in ['MindThrust','MentalBarrier','ThoughtShield'])
  else:assert any(phrase in value for value in c['coverage']),(case,phrase)
 row.update(result=str(result),resultSHA256=sha(result),contract=c)
 if case=='wound-major060':row['newBlueprints']=data['newBlueprints01460'];assert len(row['newBlueprints'])==len({r['id'] for r in row['newBlueprints']})==(4 if data.get('freshSaveRetirementPolicy061') else 8)
 if data.get('freshSaveRetirementPolicy061'):
  retirement=load(R/'work/batch01461/identity-contract061.json');old060=load(W/'060-final-packaged.json')['newBlueprints'];assert row['newBlueprints']==[r for r in old060 if r not in retirement['retiredWoundShortcuts']]
 if case=='localization060':row['catalog']=data['qlLocalization01460Export'];assert row['catalog']['nativeEntries']==4110 and (row['catalog']['entries']==len(load(R/'work/batch01461/localization-catalog061.json')) if data.get('qlCatalogPolicy061') else row['catalog']['entries']==5130) and row['catalog']['actualNativePackReaderTested'] and not row['catalog']['externalQLProgramRun']
 print('PASS060',runtime,case,c['assertions'],flush=True);return row
runtimes=['game204','umm236'] if both else ['game204']
with concurrent.futures.ThreadPoolExecutor(2) as pool:rows=list(pool.map(lambda p:execute(*p),[(r,c,main,0) for r in runtimes for c in spec]))
with concurrent.futures.ThreadPoolExecutor(2) as pool:controls=list(pool.map(lambda p:execute(*p),[(r,c,old,1) for r in runtimes for c in negative]))
assert all(sha(p)==d for p,d in inputs.items())
out.write_text(json.dumps({'passed':True,'version':'0.14.60','rows':rows,'controls':controls,'newBlueprints':next(r['newBlueprints'] for r in rows if r['case']=='wound-major060'),'inputsBeforeAndAfterIdentical':inputs,'allFiveNamedNativeGroups':True,'allFourActual059ControlsPerRuntime':True,'gameLaunchTested':False,'externalReviewPending':True,'boundary':'Actual native cast/save/effect/buff/timing/postload/area-emitter/actionbar/selection/slot-payment/JSON/localization paths. Avatar/scene allocation, native view/FX and deterministic RNG/faction inputs are explicitly bounded. No game process, pixel or complete save-transport claim.'},ensure_ascii=False,indent=2)+'\n','utf-8')
print('060 mandatory native groups and actual059 controls complete.',flush=True)

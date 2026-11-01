"""061 native feedback and Hunter contracts. Original060 controls remain byte exact."""
from pathlib import Path
import concurrent.futures,hashlib,json,subprocess,sys,uuid
R=Path(__file__).resolve().parents[3];W=R/'work/batch01461';B=R/'work/prototype-build'
main=Path(sys.argv[1]).resolve();out=Path(sys.argv[2]).resolve();both=len(sys.argv)>3 and sys.argv[3]=='both'
load=lambda p:json.loads(Path(p).read_text('utf-8-sig'))
sha=lambda p:hashlib.sha256(Path(p).read_bytes()).hexdigest().upper()
assert not out.exists() and out.is_relative_to(R)
host=B/'PsychicTests/bin/Release/net472/PsychicTests.exe';runner=W/'devrun061.py';old=W/'baseline/zip-dll/CallOfTheWild.Wrath.Prototype.dll'
assert sha(old)=='8786F76364C6FF70F39DFC224BA16728F986DEAB3A06412436DC7921C0E9FC7F'
inputs={str(p):sha(p) for p in [main,host,runner,old,Path(__file__),B/'psychic-completion01458-native.json',W/'mimic-tester-context-unavailable.json']}
prefix='required061-'+uuid.uuid4().hex
spec={
 'menu061':(40,['never standalone Target','actual native main click','persistent activatables','actual native round tick','actual native CollectAbilities','zero-use parent']),
 'mimic061':(1262,['native spontaneous actionbar slot','native multi-level Mimic payment','ordinary payment survive enabled Mimic','inapplicable or over-level native Mimic previews','actual native cast applies only eligible Mimic','constructor allocates competing Dual effects','combined cast pays each allocated effect exactly once','combined Mimic/Surge retains original low slot payment']),
 'foreign-meta061':(38,['initial native-cache foreign AddMetamagicFeat','duplicate provider flag','actual TTT registration API','cache insertion failure rolls back all staged','publish late metamagic once','never rewrites shared class-pool arrays','composite provider masks','actual native cast applies foreign flag and provider price']),
 'mimic-reentry061':(18,['reentrant caller without disabling','preserves real availability','guarded lookup still applies and pays']),
 'surge-direct061':(139,['actual temporary undercast version: MindThrust','actual temporary undercast version: MentalBarrier','actual temporary undercast version: ThoughtShield','promoted Mind Thrust effect deals native HP damage','promoted defensive spell applies the higher real buff bonus','UI follows explicit061 removal','twice the promotion','exactly one lower slot']),
 'batch061':(388,['native CanSelect accepts Feral','complete six-form','actual native AbilityData.CalculateParams','native execution context recalculation','actual native Wild Shape action pays','actual native polymorph applies','native polymorph removal restores body and attributes','native summon-completion publisher','native paid Shared Strength','actual master gain-level publisher','actual native damage creation consumes','duplicate native completion notification','weapon type supplies missing Gore/Slam item icon','item-specific natural attack icon retains priority','native CMD has no four-legged Trip defense before transformation','native polymorph grants exactly one four-legged defense fact','actual native CMD publisher and rule apply four-legged Trip defense plus four','native four-legged defense does not affect unrelated maneuvers','native four-legged defense remains scoped to the transformed defender','native polymorph removal revokes four-legged fact and actual Trip defense']),
 'hunter-lifecycle061':(53,['every one of the eight','native save converters restore the exact live owned Totem','native runtime postload','whole fact-manager suspension','fact-manager resume','native paid-minute expiration','real native caster-scoped pool clear','actual native disappearance-buff expiry']),
 'hunter-publication061':(46,['rejects injected publication failure','restores exact parent class','leaves no partial owned graph','retries fully and repeats','rejects altered own Hunter level entries','both actual new Hunter publication orders','appended foreign focus selector'])}
negatives={'menu061':['061 native feature grants only parent, never standalone Target'],'foreign-meta061':['061 initial native-cache foreign AddMetamagicFeat appears in real Mimic first-choice list'],'mimic-reentry061':[]}
def execute(runtime,case,assembly,control=False):
 name=prefix+'-'+runtime+'-'+('real060-' if control else '')+case
 args=[sys.executable,'-X','utf8',str(runner),name,case,str(assembly)]
 if runtime=='umm236':args.append(str(R/'work/feedback01420/harmony236/0Harmony.dll'))
 q=subprocess.run(args,cwd=R,capture_output=True);receipt=W/(name+'.exit.json');e=load(receipt);log=W/(name+'.log');text=log.read_text('utf-8-sig')
 if runtime=='umm236':assert '054_RUNTIME236_PIN=1' in text
 row={'runtime':runtime,'case':case,'mainSHA256':sha(assembly),'hostSHA256':sha(host),'exitCode':e['exitCode'],'execution':str(receipt),'executionSHA256':sha(receipt),'log':str(log),'logSHA256':sha(log)}
 if control and case!='mimic-reentry061':
  assert q.returncode==e['exitCode']==1,(name,e)
  for phrase in negatives[case]:assert phrase in text,(name,phrase)
  row['actual060RejectedBySameNativePath']=True;return row
 assert q.returncode==e['exitCode']==0,(name,e)
 result=W/(name+'.json');data=load(result);contract=data['psychicFeedback01461Contracts']
 assert contract['passed'] and contract['assertions']==len(contract['coverage'])
 if control:
  assert any('real060 DLL control exposes the deliberately injected' in s for s in contract['coverage'])
  row['actual060ExpectedReentryControl']=True;row['injectedCallerNotIdentifiedPlayerTrigger']=True
 else:
  minimum,phrases=spec[case];assert contract['assertions']>=minimum,(name,contract['assertions'],minimum)
  for phrase in phrases:assert any(phrase in s for s in contract['coverage']),(name,phrase)
 row.update(result=str(result),resultSHA256=sha(result),contract=contract,newOwnedBlueprints=data['newOwnedBlueprints01461'])
 print('PASS061',runtime,case,'control' if control else contract['assertions'],flush=True);return row
runtimes=['game204','umm236'] if both else ['game204']
with concurrent.futures.ThreadPoolExecutor(2) as pool:rows=list(pool.map(lambda p:execute(*p),[(r,c,main) for r in runtimes for c in spec]))
with concurrent.futures.ThreadPoolExecutor(2) as pool:controls=list(pool.map(lambda p:execute(*p),[(r,c,old,True) for r in runtimes for c in negatives]))
assert all(sha(p)==h for p,h in inputs.items())
out.write_text(json.dumps({'passed':True,'version':'0.14.61','rows':rows,'controls':controls,'allEightNamedNativeGroups':True,'allThreeActual060ControlsPerRuntime':True,'inputsBeforeAndAfterIdentical':inputs,'gameLaunchTested':False,'externalReviewPending':True,'specificTesterMimicTriggerEstablished':False,'boundary':'Actual native availability, menus, rules, payments, progression prerequisites, effects, resource/expiry, body/equipment, cache rollback and native JSON execute. Native polymorph view replacement/restoration, empty despawn FX, deferred Unity scheduler and world geometry/terminal scene services are declared endpoints. Not a game process or pixel/full save-transport observation.'},ensure_ascii=False,indent=2)+'\n','utf-8')


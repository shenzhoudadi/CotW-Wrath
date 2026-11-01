"""Required058 native contracts, exact old057 policy control and a real draft rejection."""
from pathlib import Path
import concurrent.futures,hashlib,json,subprocess,sys,uuid
R=Path(__file__).resolve().parents[3];W=R/'work/psychic01458';B=R/'work/prototype-build';main=Path(sys.argv[1]).resolve();out=Path(sys.argv[2]).resolve();both=len(sys.argv)>3 and sys.argv[3]=='both'
sha=lambda p:hashlib.sha256(Path(p).read_bytes()).hexdigest().upper()
load=lambda p:json.loads(Path(p).read_text('utf-8-sig'))
assert not out.exists() and out.is_relative_to(R)
host=B/'PsychicTests/bin/Release/net472/PsychicTests.exe';runner=W/'devrun058.py';old=W/'baseline/zip-dll/CallOfTheWild.Wrath.Prototype.dll';bad=W/'dev-attempts/discipline-effects-native17/CallOfTheWild.Wrath.Prototype.dll'
oldcert=load(W/'baseline/root/outputs/0.14.57交付自检.json');assert sha(old)==oldcert['dlls'][0]['sha256']
negative=load(W/'disciplines-warm-cache-negative01.exit.json');assert negative['exitCode']==1 and sha(bad) in negative['inputsBeforeAndAfterIdentical'].values()
inputs={str(p):sha(p) for p in [main,host,runner,Path(__file__),old,bad,W/'source-audit058.json',W/'aura-fx-asset058.json',B/'psychic-expansion-native.json',B/'psychic-completion01458-native.json',W/'native-input-split058.json']};prefix='required058-'+uuid.uuid4().hex
spec={
 'feedback058':('psychicFeedback01458Contracts',277,['actual native melee-touch approach gains exactly five feet','native toggle-off preserves exact remaining paid buff','large actual native clock advance spends bounded remaining minutes','actual production pooled-FX OnDisable restores original local scale','actual pooled object can be reused for thirty-five-foot aura','destroyed asynchronous native handle']),
 'disciplines058':('psychicDisciplines01458Contracts',292,['actual native Live On action runs its private selected fatigue mercy','warmed native paladin prerequisite cannot satisfy private Psychic mercy through cached reference','native private mercy qualification preserves both class offset','native Grand discovery uses stacked classes','unrelated native Alchemist retains its original ten-minute-per-level cognatogen','actual native level-up grant adds available EC-source spell at Psychic sixth circle']),
 'discipline-effects058':('psychicDisciplineEffects01458Contracts',127,['actual native spell-apply pre-effect publisher triggers Warped Brain','native confusion deactivation starts second caster immunity','native wound successful save retains marked target','native failed wound save applies actual selected condition','native mode-off cleans only its saved source-owned target references','native applied Color Spray buff publisher pays once','actual native healing rule and AddHealTrigger terminate private Dark Half bleed','actual native physical damage calculation applies Morphic DR5']),
 'amplifications058':('psychicAmplifications01458Contracts',115,['actual native cast event dispatch applies both old modes','native failed cast notifications refund both reservations','actual native nonforced cast validates paid ability','actual native second leaf grant completes two distinct options','actual native Spellbook.Spend consumes lower circle','paid Will execution bypasses only inherited Undead immunity','actual native Undead buff immunity is bypassed only for exact paid execution','actual native final delivery applies appended permit-consumption action once']),
 'registration058':('psychicRegistration01458Contracts',1663,['actual core058 rollback preserves both original arrays','failed core058 producer leaves no partial owned graph','same real cache retries completely','original producer rejects duplicated058 link in both pools/fields','core and original receipt both reject changed058 owned graph','actual optional bridge registers after both producers publish','actual Magaambyan alias retains powers and excludes discipline bonus spells','actual RegisterOptional caller reports exactly one successful058 discipline bridge','actual RegisterOptional caller preserves each058 alias exactly once','all twenty-one new058 component/action TypeIds execute actual native polymorphic JSON roundtrip'])}

def execute(runtime,case,assembly,label,expected=0):
    name=prefix+'-'+runtime+'-'+label;args=[sys.executable,str(runner),name,case,str(assembly)]
    if runtime=='umm236':args.append(str(R/'work/feedback01420/harmony236/0Harmony.dll'))
    p=subprocess.run(args,cwd=R,capture_output=True);receipt=W/(name+'.exit.json');j=load(receipt);assert p.returncode==j['exitCode']==expected,(name,p.returncode,j['exitCode'],p.stderr.decode(errors='replace')[-2000:])
    result=W/(name+'.json');log=W/(name+'.log');text=log.read_text('utf-8-sig')
    if runtime=='umm236':assert '054_RUNTIME236_PIN=1' in text
    row={'runtime':runtime,'case':case,'label':label,'exitCode':expected,'mainSHA256':sha(assembly),'hostSHA256':sha(host)}
    for key,path in [('log',log),('execution',receipt)]:row[key]=str(path);row[key+'SHA256']=sha(path)
    if expected==0:row.update(result=str(result),resultSHA256=sha(result))
    return row,load(result) if expected==0 else None,text

def run(pair):
    runtime,case=pair;row,data,text=execute(runtime,case,main,case);key,minimum,phrases=spec[case];c=data[key]
    assert c['passed'] and c['assertions']==len(c['coverage']) and c['assertions']>=minimum
    for phrase in phrases:
        if phrase=='native applied Color Spray buff publisher pays once' and data.get('synapticTimingPolicy060') is True:
            assert all(any(p in value for value in c['coverage']) for p in ['native failed Synaptic Pulse postpones confusion','native successful linked-spell save applies immediate confusion','actual native PostLoad reconciles an off pending link'])
        else:assert any(phrase in value for value in c['coverage']),(case,phrase)
    if case=='registration058':
        actual=c['ownedBlueprints'];policy=load(R/'work/batch01461/identity-contract061.json');old058=load(R/'work/repair01460/058-final-packaged.json')['ownedBlueprints'];assert actual in [old058,policy['current058Owned']];assert len(c['ownedBlueprints'])==(257 if actual==policy['current058Owned'] else 228) and len({r['id'] for r in c['ownedBlueprints']})==len(actual) and not {r['id'] for r in c['ownedBlueprints']}.intersection(load(W/'native-archive-index.json'))
    row['contract']=c;print('PASS058',runtime,case,c['assertions'],flush=True);return row

runtimes=['game204','umm236'] if both else ['game204']
with concurrent.futures.ThreadPoolExecutor(2) as pool:rows=list(pool.map(run,[(runtime,case) for runtime in runtimes for case in spec]))
controls=[]
for runtime in runtimes:
    row,data,text=execute(runtime,'mutation057',old,'retained057-original-policy');c=data['psychicArchetypes01457Contracts'];assert c['passed'] and c['assertions']>=193 and len(c['coverage'])==c['assertions']
    for phrase in ['existing physical mutation blocks repeat activation','actual native resource restriction rejects exhausted mutation pool','actual original amplification producer safely repeats']:assert any(phrase in value for value in c['coverage'])
    row['contract']=c;row['purpose']='Actual057 installer retains eleven choices and original one-use activation requirements.';controls.append(row)
    row,data,text=execute(runtime,'disciplines058',bad,'warm-native-cache-draft-rejection',1);assert 'Magus01454: warmed native paladin prerequisite cannot satisfy private Psychic mercy through cached reference: 12' in text
    row['purpose']='Before-fix actual draft DLL is rejected by the same warmed native prerequisite test, not a manufactured expected-value model.';controls.append(row)
assert all(sha(p)==value for p,value in inputs.items())
owned=next(r['contract']['ownedBlueprints'] for r in rows if r['case']=='registration058')
out.write_text(json.dumps({'passed':True,'version':'0.14.58','rows':rows,'controls':controls,'ownedBlueprints':owned,'allNamedNativePaths':True,'exactOld057PolicyControl':True,'warmedNativeCacheDraftRejected':True,'inputsBeforeAndAfterIdentical':inputs,'gameLaunchTested':False,'boundary':'Actual native methods/events/selection/resource/delivery/JSON paths execute with declared RNG, faction, geometry, terminal scene and Unity rendering API boundaries. Exact reverse-copied Unity-dependent bodies are identified separately in contracts. These checks do not record user game feedback or close external review.'},ensure_ascii=False,indent=2)+'\n','utf-8')
print('058 mandatory five native groups and exact historical/draft controls passed.',flush=True)

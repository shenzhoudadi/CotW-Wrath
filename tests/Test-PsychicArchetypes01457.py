"""Mandatory native Psychic archetype suite. Never starts the game or UMM."""
from pathlib import Path
import json,hashlib,sys,subprocess,uuid,concurrent.futures
R=Path(__file__).resolve().parents[3];W=R/'work/psychic01458';B=R/'work/prototype-build';main=Path(sys.argv[1]).resolve();out=Path(sys.argv[2]).resolve();both=len(sys.argv)>3 and sys.argv[3]=='both'
def sha(p):return hashlib.sha256(Path(p).read_bytes()).hexdigest().upper()
def load(p):return json.loads(Path(p).read_text('utf-8-sig'))
assert not out.exists() and out.is_relative_to(R)
host=B/'PsychicTests/bin/Release/net472/PsychicTests.exe';runner=W/'devrun.py';inputs={str(p):sha(p) for p in [main,host,runner,Path(__file__),W/'original-texts-icon-audit.json']};prefix='required057-'+uuid.uuid4().hex
probe=subprocess.run(['dotnet',str(R/'work/api-audit/Inspector/bin/Release/net9.0/Inspector.dll'),'names',str(main),'CallOfTheWild.Wrath.Prototype.PhysicalMutationToggle01458'],check=True,capture_output=True,text=True)
policy058='TYPE CallOfTheWild.Wrath.Prototype.PhysicalMutationToggle01458 :' in probe.stdout
required={
 'mutation057':(190,['actual selected mutation activates exactly one native permanent child: Wings','native bite removal balances the extra limb','native claw removal restores original empty-hand weapon','native mutation cleanup restores speed/CMD/perception: Wings','actual native resource restriction rejects exhausted mutation pool','actual original amplification producer safely repeats']),
 'marauder057':(160,['native Will replacement recalculates when Charisma changes','native Will replacement remains independent of changed Wisdom','native Will replacement uses Charisma even when lower: 8/6','actual native confusion roll/event/command selection follows controlled state IIIAttack','actual native overlapping aura keeps one independent marker per caster','leaving first caster aura preserves second caster native marker','switching own aura replaces only own previous native aura','native area exit removes its marker while failed one-round confusion remains','native aura duration uses Charisma modifier rounds IIIStill']),
 'faults057':(89,['actual057 producer rejects injected connect fault','same native cache retries completely after missing','original producer rejects duplicated declared mutation link in m_Features','original producer rejects mutated private extension definition','actual native polymorphic serializer restores057 TypeId and blueprint owner'])}
if policy058:
 old_minimum,old_phrases=required['mutation057'];assert old_minimum==190 and old_phrases.count('actual native resource restriction rejects exhausted mutation pool')==1
 required['mutation057']=(270,[s for s in old_phrases if s!='actual native resource restriction rejects exhausted mutation pool']+['058 zero resource without a paid minute cannot start a free mutation','058 legacy shortcut toggles off without removing or changing already paid minute','058 legacy shortcut reopens remaining paid minute without resetting or charging it'])
def run(pair):
 runtime,case=pair;name=prefix+'-'+runtime+'-'+case;args=[sys.executable,str(runner),name,case,str(main)]
 if runtime=='umm236':args.append(str(R/'work/feedback01420/harmony236/0Harmony.dll'))
 q=subprocess.run(args,cwd=R,capture_output=True);exe=W/(name+'.exit.json');e=load(exe);assert q.returncode==e['exitCode']==0,(name,q.stdout.decode(errors='replace'),q.stderr.decode(errors='replace'))
 result=W/(name+'.json');log=W/(name+'.log');contract=load(result)['psychicArchetypes01457Contracts'];minimum,phrases=required[case]
 assert contract['passed'] and contract['case']==case and contract['assertions']>=minimum and contract['assertions']==len(contract['coverage'])
 for phrase in phrases:assert any(phrase in s for s in contract['coverage']),(case,phrase)
 if runtime=='umm236':assert '054_RUNTIME236_PIN=1' in log.read_text('utf-8-sig')
 row={'runtime':runtime,'case':case,'exitCode':0,'mainSHA256':sha(main),'hostSHA256':sha(host),'contract':contract}
 for k,p in [('result',result),('log',log),('execution',exe)]:row[k]=str(p);row[k+'SHA256']=sha(p)
 print('PASS057',runtime,case,contract['assertions'],flush=True);return row
with concurrent.futures.ThreadPoolExecutor(2) as pool:rows=list(pool.map(run,[(runtime,case) for runtime in (['game204','umm236'] if both else ['game204']) for case in required]))
assert all(sha(p)==h for p,h in inputs.items())
owned={}
for row in rows:
 for item in row['contract']['ownedBlueprints']:
  if item['id'] in owned:assert owned[item['id']]==item
  owned[item['id']]=item
assert len(owned)==(214 if policy058 else 210) and not set(owned).intersection(load(W/'native-archive-index.json'))
out.write_text(json.dumps({'passed':True,'version':'0.14.58' if policy058 else '0.14.57','retainedContract':'0.14.57 archetypes','explicitMutationPolicy058':policy058,'selectedIds':['COTW-0082','COTW-0083'],'additionalMigrationItemCount':2,'rows':rows,'ownedBlueprints':sorted(owned.values(),key=lambda v:v['id']),'allNamedNativePaths':True,'inputsBeforeAndAfterIdentical':inputs,'gameLaunchTested':False,'boundary':'Native cache/progression/prerequisites/resources/body components, Buff round/save/confusion state and command branch, source ownership/rollback/JSON. Unity spatial/rendered UI/final command insertion and full save transport remain controlled endpoints; no game process launched.'},ensure_ascii=False,indent=2)+'\n','utf-8')
print('057 mandatory native archetypes complete.',flush=True)

import hashlib,json,pathlib,re,sys,zipfile
game=pathlib.Path(sys.argv[1]);target=pathlib.Path(sys.argv[2])
root=pathlib.Path(__file__).resolve().parents[3]
manifest=json.loads((root/'work/repair01449/current-source-manifest.json').read_text('utf-8-sig'))
row=next(r for r in manifest if r['repo']=='pheonix99/TomeOfTheFirebird')
source=pathlib.Path(row['sourceTree'])/'TomeOfTheFirebird/Modified Content/Bloodlines/DraconicBloodlineModifications.cs'
ids=set(re.findall(r'"([0-9a-f]{32})"',source.read_text('utf-8-sig')))
with zipfile.ZipFile(game/'blueprints.zip') as z:
 raw={}
 for p in z.namelist():
  if not p.endswith('.jbp'):continue
  data=z.read(p)
  # AssetId precedes Data, avoiding expansion of unneeded large action graphs.
  m=re.search(rb'"AssetId"\s*:\s*"([0-9a-f]{32})"',data[:160])
  if m:raw[m[1].decode()]=(p,data)
 done=set();rows=[]
 while ids-done:
  guid=min(ids-done);done.add(guid)
  if guid not in raw:continue
  path,data=raw[guid];bp=json.loads(data);kind=bp['Data']['$type'].split(',')[-1].strip()
  if kind not in {'BlueprintProgression','BlueprintFeature','BlueprintAbility','BlueprintActivatableAbility','BlueprintBuff','BlueprintAbilityResource','BlueprintItemWeapon','BlueprintWeaponType'}:continue
  rows.append({'path':path,'name':pathlib.PurePosixPath(path).stem,'sha256':hashlib.sha256(data).hexdigest().upper(),'blueprint':bp})
  ids.update(re.findall(r'!bp_([0-9a-f]{32})',data.decode('utf-8-sig')))
target.parent.mkdir(parents=True,exist_ok=True)
target.write_text(json.dumps({'sourceCommit':row['commit'],'sourceSHA256':hashlib.sha256(source.read_bytes()).hexdigest().upper(),'rows':rows},ensure_ascii=False,indent=2),'utf-8')
print('Actual ToTF claw native dependencies',len(rows))

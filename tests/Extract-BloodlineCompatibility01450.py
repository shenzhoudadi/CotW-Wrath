import hashlib,json,pathlib,re,sys,zipfile
game=pathlib.Path(sys.argv[1]);target=pathlib.Path(sys.argv[2]);root=pathlib.Path(__file__).resolve().parents[3]
manifest=json.loads((root/'work/psychic01458/current-source-manifest.json').read_text('utf-8-sig'))
trees={r['repo']:pathlib.Path(r['sourceTree']) for r in manifest}
sources=[trees['EdgarEbonfowl/EbonsContentMod']/'Bloodlines'/n for n in ['OrcSorcererBloodline.cs','ProteanSorcererBloodline.cs']]
sources +=[trees['Vek17/TabletopTweaks-Base']/'TabletopTweaks-Base/NewContent/Bloodlines'/n for n in ['AberrantBloodline.cs','DestinedBloodline.cs']]
sources +=[trees['pheonix99/TomeOfTheFirebird']/'TomeOfTheFirebird/New Content/Bloodlines/PheonixBloodline.cs',trees['pheonix99/TomeOfTheFirebird']/'TomeOfTheFirebird/Modified Content/Bloodlines/DraconicBloodlineModifications.cs']
sources +=[trees['Vek17/TabletopTweaks-Core']/'TabletopTweaks-Core/Utilities/FeatTools.cs']
text='\n'.join(p.read_text('utf-8-sig') for p in sources)
ids=set(re.findall(r'"([0-9a-f]{32})"',text));names=set(re.findall(r'\b\w+Refs\.(\w+)',text))
with zipfile.ZipFile(game/'blueprints.zip') as z:
 raw={}
 for p in z.namelist():
  if not p.endswith('.jbp'):continue
  data=z.read(p);m=re.search(rb'"AssetId"\s*:\s*"([0-9a-f]{32})"',data[:160])
  if m:
   guid=m[1].decode();raw[guid]=(p,data)
   if pathlib.PurePosixPath(p).stem in names:ids.add(guid)
 done=set();rows=[]
 allowed={'BlueprintProgression','BlueprintFeature','BlueprintFeatureSelection','BlueprintAbility','BlueprintActivatableAbility','BlueprintBuff','BlueprintAbilityResource','BlueprintItemWeapon','BlueprintWeaponType','BlueprintCharacterClass','BlueprintArchetype','BlueprintSpellList','BlueprintSpellbook','BlueprintUnitProperty','BlueprintWeaponEnchantment','BlueprintProjectile'}
 while ids-done:
  guid=min(ids-done);done.add(guid)
  if guid not in raw:continue
  path,data=raw[guid];bp=json.loads(data);kind=bp['Data']['$type'].split(',')[-1].strip()
  if kind not in allowed:continue
  rows.append({'path':path,'name':pathlib.PurePosixPath(path).stem,'sha256':hashlib.sha256(data).hexdigest().upper(),'blueprint':bp})
  # Class/archetype/selection roots expose broad unrelated campaign/feat trees.
  # Their existing fixture dependencies are kept; do not hydrate the entire
  # game's optional weapon/DLC graph for a bloodline-builder contract.
  if kind not in {'BlueprintCharacterClass','BlueprintArchetype','BlueprintFeatureSelection'}:
   ids.update(re.findall(r'!bp_([0-9a-f]{32})',data.decode('utf-8-sig')))
target.parent.mkdir(parents=True,exist_ok=True)
target.write_text(json.dumps({'sources':[{'path':str(p),'sha256':hashlib.sha256(p.read_bytes()).hexdigest().upper()} for p in sources],'rows':rows},ensure_ascii=False,indent=2),'utf-8')
print('Actual official bloodline native dependencies',len(rows))

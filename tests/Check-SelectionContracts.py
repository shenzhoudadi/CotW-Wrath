"""Offline contracts over the actual installed game's selection definitions.
The managed suite separately applies production registration/repair to these pools.
"""
import json, sys, re, hashlib
from pathlib import Path
native=Path(sys.argv[1]); project=Path(sys.argv[2]); target=Path(sys.argv[3])
d=json.loads(native.read_text('utf-8-sig')); types=d['nativeBlueprintTypes']; failures=[]; references=0; inherited_duplicates=[]
for row in d['allNativeSelectionPools']:
    bp=row['blueprint']; data=bp['Data']
    for field in ['m_Features','m_AllFeatures']:
        values=data.get(field) or []
        seen=set()
        for value in values:
            references+=1
            guid=value.removeprefix('!bp_') if isinstance(value,str) else ''
            if not re.fullmatch('[0-9a-f]{32}',guid) or guid not in types:
                failures.append({'pool':bp['AssetId'],'field':field,'reference':value})
            if guid in seen: inherited_duplicates.append({'pool':bp['AssetId'],'field':field,'guid':guid})
            seen.add(guid)
source=(project/'src/ModuleCatalog.cs').read_text('utf-8-sig')
entry=(project/'src/ModEntry.cs').read_text('utf-8-sig')
menu=(project/'src/ModMenuIntegration.cs').read_text('utf-8-sig')
assert 'ModuleCatalog.InitialStates()' in entry and 'ModuleCatalog.Snapshot(moduleStates)' in entry
assert 'GetModuleStatuses(), log, error' in entry
assert 'private static readonly string[] Names' not in menu and 'Unexpected module status count' not in menu
assert 'currentModules' in menu and 'if (failed) return;' in menu
assert '__runOriginal' not in '\n'.join(f.read_text('utf-8-sig') for f in (project/'src').glob('*.cs'))
result={'passed':not failures,'nativePoolsChecked':len(d['allNativeSelectionPools']),'nativeReferencesChecked':references,'invalidReferences':failures,'inheritedDuplicateReferences':inherited_duplicates,'moduleCatalogueSourceSHA256':hashlib.sha256(source.encode()).hexdigest().upper(),'nativeEvidenceSHA256':hashlib.sha256(native.read_bytes()).hexdigest().upper(),'scope':'All current native feature selections, including feat pools; existing duplicate rows reported separately and preserved. Production caller/module checks and runtime mutation preservation are verified by managed suites.'}
target.write_text(json.dumps(result,ensure_ascii=False,indent=2)+'\n','utf-8')
assert not failures,failures[:12]
print(f'PASS: {len(d["allNativeSelectionPools"])} native selection pools, {references} valid references; shared module catalogue contract; inherited duplicates preserved.')

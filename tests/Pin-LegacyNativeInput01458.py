"""Validate fresh original extraction, then retain the exact057 fixture ordering."""
from pathlib import Path
import hashlib,json,shutil,sys,uuid
R=Path(__file__).resolve().parents[3];W=R/'work/psychic01458';p=Path(sys.argv[1]).resolve()
sha=lambda q:hashlib.sha256(Path(q).read_bytes()).hexdigest().upper()
load=lambda q:json.loads(Path(q).read_text('utf-8-sig'))
assert p==R/'work/prototype-build/psychic-expansion-native.json'
source=load(W/'native-input-split058.json');old=Path(source['old057InputCopy']);assert sha(old)==source['old057InputSHA256']
fresh=load(p);prior=load(old);a=fresh.pop('requiredNative');b=prior.pop('requiredNative')
assert fresh==prior,'A native field outside required-root ordering changed.'
key=lambda row:row['blueprint']['AssetId']
assert len(a)==len(b)==8530 and len({key(r) for r in a})==len(a) and len({key(r) for r in b})==len(b)
assert {key(r):r for r in a}=={key(r):r for r in b},'Fresh actual native payload values changed; refuse historical ordering pin.'
out=W/'native-input-order-history';out.mkdir(exist_ok=True);name=uuid.uuid4().hex;copy=out/(name+'.json');shutil.copyfile(p,copy)
p.write_bytes(old.read_bytes());assert sha(p)==source['old057InputSHA256']
record={'passed':True,'freshActualExtraction':str(copy),'freshSHA256':sha(copy),'pinnedInput':str(p),'pinnedSHA256':sha(p),'old057Input':str(old),'requiredRootPayloadValuesExact':8530,'allOtherNativePayloadsAndFieldsExact':True,'onlyRootArrayOrderingAndJSONFormattingPinned':True,'gameBlueprintsNotChanged':True}
(out/(name+'-audit.json')).write_text(json.dumps(record,indent=2)+'\n','utf-8')
print('PASS058 legacy input pin:8530 fresh native payloads and every other field exact; original057 byte ordering retained.')

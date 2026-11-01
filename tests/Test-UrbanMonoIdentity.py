"""Check the actual Urban identity against game Mono; does not launch the game."""
from pathlib import Path
import ctypes, hashlib, json, os, subprocess, sys

def embed(exe, managed, main, runtime):
    os.add_dll_directory(str(runtime))
    os.environ['PATH']=str(runtime)+os.pathsep+os.environ.get('PATH','')
    mono=ctypes.CDLL(str(runtime/'mono-2.0-bdwgc.dll'))
    def api(name,result,*params):
        f=getattr(mono,name);f.restype=result;f.argtypes=list(params);return f
    game=managed.parents[1]
    api('mono_set_assemblies_path',None,ctypes.c_char_p)(os.fsencode(managed))
    api('mono_set_dirs',None,ctypes.c_char_p,ctypes.c_char_p)(os.fsencode(managed),os.fsencode(game/'MonoBleedingEdge/etc'))
    api('mono_config_parse',None,ctypes.c_char_p)(None)
    domain=api('mono_jit_init_version',ctypes.c_void_p,ctypes.c_char_p,ctypes.c_char_p)(b'CotW.UrbanIdentity',b'v4.0.30319')
    assert domain
    assembly=api('mono_domain_assembly_open',ctypes.c_void_p,ctypes.c_void_p,ctypes.c_char_p)(domain,os.fsencode(exe))
    assert assembly
    args=[os.fsencode(x) for x in (exe,managed,main)];argv=(ctypes.c_char_p*len(args))(*args)
    code=api('mono_jit_exec',ctypes.c_int,ctypes.c_void_p,ctypes.c_void_p,ctypes.c_int,ctypes.POINTER(ctypes.c_char_p))(domain,assembly,len(args),argv)
    print('ISOLATED_MONO_EXIT='+str(code),flush=True)
    return code

if len(sys.argv)>1 and sys.argv[1]=='--embed':
    sys.exit(embed(*(Path(x).resolve() for x in sys.argv[2:])))

project=Path(__file__).resolve().parents[1];root=project.parents[1];main=Path(sys.argv[1]).resolve();out=Path(sys.argv[2]).resolve()
assert not out.exists() and out.is_relative_to(root)
game=Path(sys.argv[3] if len(sys.argv)>3 else os.environ.get('WOTR_GAME_PATH','D:/steam/steamapps/common/Pathfinder Second Adventure')).resolve()
managed=game/'Wrath_Data/Managed';runtime=game/'MonoBleedingEdge/EmbedRuntime'
old=root/'work/repair_urban_bloodrage/delivered063.dll'
sha=lambda p:hashlib.sha256(Path(p).read_bytes()).hexdigest().upper()
assert sha(old)=='15ABEAFCAC75EABA86BBF2F6D794383379AE5D3E88891BA35E5DBC8201C8420C'
proj=project/'tests/UrbanMonoIdentity.csproj'
build=subprocess.run(['dotnet','build',str(proj),'-c','Release','-p:WrathPath='+str(game)],cwd=root,capture_output=True,text=True,encoding='utf8',errors='replace')
assert build.returncode==0,build.stdout+'\n'+build.stderr
host=root/'work/prototype-build/UrbanMonoIdentity/bin/Release/net472/UrbanMonoIdentity.exe'
inputs=[main,old,host,proj,project/'tests/UrbanMonoIdentity.cs',Path(__file__),runtime/'mono-2.0-bdwgc.dll',managed/'Assembly-CSharp.dll']
before={str(p):sha(p) for p in inputs}
rows=[]
for label,assembly,mono,expected in [('new-clr',main,False,0),('new-mono',main,True,0),('delivered063-mono-negative',old,True,1)]:
    args=([sys.executable,str(Path(__file__).resolve()),'--embed',str(host),str(managed),str(assembly),str(runtime)] if mono else [str(host),str(managed),str(assembly)])
    run=subprocess.run(args,cwd=root,capture_output=True,text=True,encoding='utf8',errors='replace')
    expectedText=('runtime=Mono' if mono else 'runtime=CLR')+' own='+('True' if expected==0 else 'False')+' native=False foreign=False null=False typedEqual=True'
    assert run.returncode==expected and expectedText in run.stdout,(label,run.returncode,run.stdout,run.stderr)
    if mono:assert 'arrayContains=False' in run.stdout and 'ISOLATED_MONO_EXIT='+str(expected) in run.stdout
    rows.append(dict(case=label,exitCode=run.returncode,assemblySHA256=sha(assembly),command=args,stdout=run.stdout,stderr=run.stderr))
after={str(p):sha(p) for p in inputs};assert before==after
out.parent.mkdir(parents=True,exist_ok=True)
out.write_text(json.dumps(dict(passed=True,rows=rows,inputsBeforeAndAfterIdentical=before,notGameProcess=True,boundary='Actual installed game Mono with isolated native blueprint identity; no game scene or UMM process.'),indent=2)+'\n',encoding='utf8')
print('PASS Urban identity: current CLR and game Mono, actual delivered063 negative control')

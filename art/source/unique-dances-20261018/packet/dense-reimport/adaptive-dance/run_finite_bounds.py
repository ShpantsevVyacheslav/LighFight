"""Explicitly invoked offline job; never launches Unity/Blender. No automatic run."""
from pathlib import Path
import argparse,hashlib,json,os,subprocess,time
ROOT=Path(__file__).resolve().parents[6]
HERE=Path(__file__).resolve().parent
MONO=Path('/Applications/Unity/Hub/Editor/6000.3.26f1/Unity.app/Contents/Resources/Scripting/MonoBleedingEdge')
def sha(path):
 h=hashlib.sha256()
 with Path(path).open('rb') as f:
  while b:=f.read(8388608):h.update(b)
 return h.hexdigest()
def main():
 ap=argparse.ArgumentParser();ap.add_argument('--run',action='store_true');ap.add_argument('--margin-native-m',type=float,default=.025);ap.add_argument('--output',type=Path,required=True);args=ap.parse_args()
 if not args.run:raise SystemExit('Explicit --run required; coordinate CPU window first')
 if not 0<=args.margin_native_m<=1:raise SystemExit('Finite explicit native margin required')
 out=args.output.resolve();out.mkdir(parents=True,exist_ok=False)
 env=dict(os.environ,OPENBLAS_NUM_THREADS='1',OMP_NUM_THREADS='1')
 inputs=[Path('/tmp/lighfight-dense-adaptive-dance.nda'),ROOT/'art/drafts/newrig-sailor-v1/dense-def-fbx-packet-v1/original-indexed-attributes.npz',ROOT/'art/drafts/newrig-sailor-v1/dense-def-fbx-packet-v1/manifest.json',HERE/'AdaptiveDanceMath.cs',HERE/'FiniteClipBounds.cs',HERE/'prepare_bounds_mesh.py']
 manifest={'status':'PREPARED_NOT_COMPLETE','scope':'FINITE120HZ_PLUS_ALL2535KEYS_UNGROUNDED_STEADY_DANCE_ONLY','continuousGuarantee':False,'maxDegree':1,'timeoutSeconds':1200,'absoluteNativeMarginMetres':args.margin_native_m,'inputs':{str(p):{'SHA256':sha(p),'bytes':p.stat().st_size} for p in inputs}}
 (out/'inputs.json').write_text(json.dumps(manifest,indent=2)+'\n')
 subprocess.run([os.sys.executable,str(HERE/'prepare_bounds_mesh.py')],env=env,check=True)
 mesh=Path('/tmp/lighfight-dense-original-bounds.mesh');manifest['typedMesh']={'SHA256':sha(mesh),'bytes':mesh.stat().st_size};(out/'inputs.json').write_text(json.dumps(manifest,indent=2)+'\n')
 exe=out/'FiniteClipBounds.exe'
 subprocess.run([str(MONO/'bin/mono'),str(MONO/'lib/mono/4.5/csc.exe'),'/nologo','/optimize+','/out:'+str(exe),str(HERE/'AdaptiveDanceMath.cs'),str(HERE/'FiniteClipBounds.cs')],check=True)
 subprocess.run([str(MONO/'bin/mono'),str(exe),'--test'],check=True)
 command=[str(MONO/'bin/mono'),str(exe),str(inputs[0]),str(mesh),str(out/'bounds.json'),str(args.margin_native_m)]
 (out/'command.json').write_text(json.dumps(command,indent=2)+'\n')
 started=time.monotonic()
 with (out/'stdout.log').open('w') as log:
  process=subprocess.Popen(command,stdout=log,stderr=subprocess.STDOUT,env=env)
  while True:
   try:code=process.wait(timeout=30);break
   except subprocess.TimeoutExpired:
    elapsed=time.monotonic()-started
    print(json.dumps({'status':'RUNNING','elapsedSeconds':round(elapsed,1),'pid':process.pid,'maxDegree':1}),flush=True)
    if elapsed>1260:
     process.terminate()
     try:process.wait(timeout=10)
     except subprocess.TimeoutExpired:process.kill();process.wait()
     raise RuntimeError('Owned offline process exceeded outer budget; no acceptance')
 manifest.update(status='PASS_FINITE_ONLY' if code==0 and (out/'bounds.json').exists() else 'FAILED_NO_ACCEPTANCE',exitCode=code,wallSeconds=time.monotonic()-started)
 manifest['inputsUnchanged']=all(sha(p)==manifest['inputs'][str(p)]['SHA256'] for p in inputs)
 if not manifest['inputsUnchanged']:manifest['status']='FAILED_INPUT_CHANGED'
 (out/'outcome.json').write_text(json.dumps(manifest,indent=2)+'\n')
 print(json.dumps(manifest),flush=True)
 if manifest['status']!='PASS_FINITE_ONLY':raise SystemExit(1)
if __name__=='__main__':main()

#!/usr/bin/env python3
"""Explicit future heavy-window invocation only; never called by preparation."""
import argparse,subprocess,sys,json,time,shutil,os,resource
from pathlib import Path
p=argparse.ArgumentParser();p.add_argument('job',choices=['build-CPU','build-GPU','player-CPU','player-GPU']);p.add_argument('--profile',action='store_true');p.add_argument('--trace',type=Path);a=p.parse_args()
repo=Path(__file__).resolve().parents[4];owned=repo/'.tools/skinning-current100k-review';packet=Path(__file__).resolve().parent.parent/'fixture';mode=a.job.split('-')[1];logs=owned/'BufferRunLogsV10';logs.mkdir(exist_ok=True);name=a.job+('-profile' if a.profile else '-correctness');log=logs/(name+'.log')
# Fail closed if another engine/player owns the heavy window. No process is killed by name.
procs=subprocess.check_output(['ps','-axo','pid=,comm='],text=True)
for line in procs.splitlines():
 if any(s in line for s in ['/Unity.app/Contents/MacOS/Unity','/Blender.app/Contents/MacOS/Blender','/Contents/MacOS/CurrentBufferFixture']):raise SystemExit('Heavy process active: '+line.strip())
if a.profile:raise SystemExit('V10 has no timing/profile permit')
if mode!='GPU':raise SystemExit('V10 one-frame scope GPU only')
if a.job.startswith('build'):
 if a.profile:raise SystemExit('Profile uses same existing binary after correctness; no profile build')
 subprocess.run([sys.executable,str(Path(__file__).resolve().parent/'verify_frozen_owned.py')],check=True,cwd=repo)
 exe='/Applications/Unity/Hub/Editor/6000.3.26f1/Unity.app/Contents/MacOS/Unity'
 cmd=[exe,'-batchmode','-forceGfxMetal','-buildTarget','StandaloneOSX','-projectPath',str(owned),'-executeMethod','CurrentBufferBuild.'+mode,'-quit','-logFile',str(log)];budget=240
else:
 exe=owned/f'CurrentBufferBuildsV10/{mode}.app/Contents/MacOS/CurrentBufferFixture'
 cmd=[str(exe),'-forceGfxMetal','-screen-width','800','-screen-height','1000','-screen-fullscreen','0','-logFile',str(log)]
 if a.profile:cmd.append('-lfSkinProfile')
 budget=180
 if not a.trace:raise SystemExit('--trace unique path required')
 trace=a.trace.resolve();traceRoot=(owned/'CurrentBufferBuildsV10'/'Traces').resolve()
 if trace.parent!=traceRoot or trace.suffix!='.gputrace' or trace.exists():raise SystemExit('Require new .gputrace immediately under owned V10/Traces')
 trace.parent.mkdir(parents=True,exist_ok=True);cmd+=['-lfMetalTracePath',str(trace)]
free=shutil.disk_usage(owned).free
ownedBytes=sum(p.stat().st_size for p in owned.rglob('*') if p.is_file() and not p.is_symlink())
if free<8*1024**3:raise SystemExit('Need8GiB free for bounded2-app/cache/temp budget; actual='+str(free))
if ownedBytes>5*1024**3:raise SystemExit('Owned project exceeds5GiB cap; inspect before another build')
manifest=json.loads((owned/'buffer-source-manifest.json').read_text())
import hashlib
for inputPath,sha in manifest.items():
 if hashlib.sha256((owned/inputPath).read_bytes()).hexdigest()!=sha:raise SystemExit('Buffer source changed: '+inputPath)
tracePath=a.trace.resolve() if a.trace else None
traceLimit=1024**3
def trace_bytes():
 if tracePath is None or not tracePath.exists():return 0
 return tracePath.stat().st_size if tracePath.is_file() else sum(p.stat().st_size for p in tracePath.rglob('*') if p.is_file())
def player_limits():resource.setrlimit(resource.RLIMIT_FSIZE,(traceLimit,traceLimit))
env=os.environ.copy()
if tracePath is not None:env['METAL_CAPTURE_ENABLED']='1'
start=time.time();child=subprocess.Popen(cmd,cwd=owned,env=env,preexec_fn=player_limits if tracePath is not None else None);reason=None
while child.poll() is None:
 if time.time()-start>budget:reason='wall deadline'
 if trace_bytes()>traceLimit:reason='aggregate trace exceeded1GiB; unqualified'
 if shutil.disk_usage(owned).free<8*1024**3:reason='free disk fell below8GiB'
 if reason:
  child.terminate()
  try:child.wait(timeout=5)
  except subprocess.TimeoutExpired:child.kill();child.wait()
  break
 time.sleep(.1)
code=child.returncode if reason is None else 124
size=trace_bytes()
if size>traceLimit:code=125;reason='aggregate trace exceeds1GiB; no acceptance'
result={'argv':cmd,'pid':child.pid,'exit':code,'wallSeconds':time.time()-start,'budgetSeconds':budget,'log':str(log),'traceBytes':size,'traceLimitBytes':traceLimit,'stopReason':reason,'childEnvironmentAdditions':{'METAL_CAPTURE_ENABLED':'1'} if tracePath is not None else{}}
(logs/(name+'-result.json')).write_text(json.dumps(result,indent=2)+'\n');print(json.dumps(result));sys.exit(code)

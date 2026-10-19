#!/usr/bin/env python3
"""Explicit future heavy-window invocation only; never called by preparation."""
import argparse,subprocess,sys,json,time,shutil
from pathlib import Path
p=argparse.ArgumentParser();p.add_argument('job',choices=['build-CPU','build-GPU','player-CPU','player-GPU']);p.add_argument('--profile',action='store_true');a=p.parse_args()
repo=Path(__file__).resolve().parents[4];owned=repo/'.tools/skinning-current100k-review';packet=Path(__file__).resolve().parent;mode=a.job.split('-')[1];logs=owned/'RunLogs';logs.mkdir(exist_ok=True);name=a.job+('-profile' if a.profile else '-correctness');log=logs/(name+'.log')
# Fail closed if another engine/player owns the heavy window. No process is killed by name.
procs=subprocess.check_output(['ps','-axo','pid=,comm='],text=True)
for line in procs.splitlines():
 if any(s in line for s in ['/Unity.app/Contents/MacOS/Unity','/Blender.app/Contents/MacOS/Blender','/Contents/MacOS/CurrentSkinningFixture']):raise SystemExit('Heavy process active: '+line.strip())
if a.job.startswith('build'):
 if a.profile:raise SystemExit('Profile uses same existing binary after correctness; no profile build')
 subprocess.run([sys.executable,str(packet/'verify.py')],check=True,cwd=repo)
 exe='/Applications/Unity/Hub/Editor/6000.3.26f1/Unity.app/Contents/MacOS/Unity'
 cmd=[exe,'-batchmode','-forceGfxMetal','-buildTarget','StandaloneOSX','-projectPath',str(owned),'-executeMethod','CurrentSkinningBuild.'+mode,'-quit','-logFile',str(log)];budget=240
else:
 exe=owned/f'CurrentSkinningBuilds/{mode}.app/Contents/MacOS/CurrentSkinningFixture'
 cmd=[str(exe),'-forceGfxMetal','-screen-width','800','-screen-height','1000','-screen-fullscreen','0','-logFile',str(log)]
 if a.profile:cmd.append('-lfSkinProfile')
 budget=180
free=shutil.disk_usage(owned).free
ownedBytes=sum(p.stat().st_size for p in owned.rglob('*') if p.is_file() and not p.is_symlink())
if free<8*1024**3:raise SystemExit('Need8GiB free for bounded2-app/cache/temp budget; actual='+str(free))
if ownedBytes>5*1024**3:raise SystemExit('Owned project exceeds5GiB cap; inspect before another build')
start=time.time();child=subprocess.Popen(cmd,cwd=owned)
try:code=child.wait(timeout=budget)
except subprocess.TimeoutExpired:
 child.terminate()
 try:child.wait(timeout=10)
 except subprocess.TimeoutExpired:child.kill();child.wait()
 code=124
result={'argv':cmd,'pid':child.pid,'exit':code,'wallSeconds':time.time()-start,'budgetSeconds':budget,'log':str(log)};(logs/(name+'-result.json')).write_text(json.dumps(result,indent=2)+'\n');print(json.dumps(result));sys.exit(code)

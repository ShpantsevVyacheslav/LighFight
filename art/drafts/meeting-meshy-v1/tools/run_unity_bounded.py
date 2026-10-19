"""One owned headless process, bounded time/RSS; no global process termination."""
import subprocess,sys,time,json,os
from pathlib import Path
args=sys.argv[1:];log=Path(args.pop(0));result=log.with_suffix('.result.json');log.parent.mkdir(parents=True,exist_ok=True)
for line in subprocess.check_output(['ps','-axo','pid=,comm='],text=True).splitlines():
 if any(x in line for x in ['/Unity.app/Contents/MacOS/Unity','/Blender.app/Contents/MacOS/Blender']):raise SystemExit('Heavy engine already active: '+line.strip())
cmd=['/Applications/Unity/Hub/Editor/6000.3.26f1/Unity.app/Contents/MacOS/Unity','-batchmode','-forceGfxMetal','-projectPath',str(Path('unity').resolve()),'-job-worker-count','5']+args
start=time.time();peak=0;reason='';env=dict(os.environ,OMP_NUM_THREADS='6',OPENBLAS_NUM_THREADS='6')
with log.open('w') as output:
 child=subprocess.Popen(cmd,stdout=output,stderr=subprocess.STDOUT,env=env)
 while child.poll() is None:
  try:rows=subprocess.check_output(['ps','-axo','pid=,ppid=,rss='],text=True).splitlines();owned={child.pid};parsed=[tuple(map(int,r.split())) for r in rows if len(r.split())==3]
  except Exception:parsed=[]
  for unused in range(4):owned.update(p for p,pp,rss in parsed if pp in owned)
  rss=sum(rss for p,pp,rss in parsed if p in owned)*1024;peak=max(peak,rss)
  if time.time()-start>600 or rss>6*1024**3:
   reason='timeout600s' if time.time()-start>600 else 'RSS6GiB';child.terminate()
   try:child.wait(timeout=8)
   except subprocess.TimeoutExpired:child.kill();child.wait()
   break
  time.sleep(2)
code=child.returncode;data={'pid':child.pid,'exit':code,'reason':reason,'seconds':time.time()-start,'peakOwnedRSSBytes':peak,'command':cmd};result.write_text(json.dumps(data,indent=2));print(json.dumps(data));sys.exit(code or (124 if reason else 0))

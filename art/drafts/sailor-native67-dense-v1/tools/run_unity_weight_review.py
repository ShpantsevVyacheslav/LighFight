"""Bounded isolated review; never starts the user's main project."""
import subprocess,time,os,json,sys,re
from pathlib import Path
r=Path(__file__).resolve().parents[4];o=r/'art/drafts/sailor-native67-dense-v1'/sys.argv[1];o.mkdir(exist_ok=False);env=os.environ.copy();env.update(LF_NATIVE67_REPORT=str(o/'result.json'),LF_NATIVE67_PACKET=str(r/'.tools/native67-weight-packet-v1'));env['LF_NATIVE67_GEOMETRY_REPORT']=str(r/'art/drafts/sailor-native67-dense-v1/native67-weight-restore-v3/result.json');start=time.monotonic();samples=[];reason=''
p=subprocess.Popen(['/Applications/Unity/Hub/Editor/6000.3.26f1/Unity.app/Contents/MacOS/Unity','-batchmode','-buildTarget','StandaloneOSX','-projectPath',str(r/'.tools/skinning-current100k-review'),'-executeMethod',('Native67FloatGpuReview.Run' if '--gpu' in sys.argv else 'Native67FullWeightRestoreReview.Run'),'-logFile',str(o/'unity.log')],env=env);print('Owned Unity PID',p.pid,flush=True)
while p.poll() is None:
 time.sleep(5);q=subprocess.run(['ps','-p',str(p.pid),'-o','rss='],capture_output=True,text=True).stdout.strip();rss=int(q)*1024 if q else 0
 pressure=subprocess.run(['sysctl','-n','kern.memorystatus_vm_pressure_level'],capture_output=True,text=True).stdout.strip();mp=subprocess.run(['memory_pressure','-Q'],capture_output=True,text=True).stdout;m=re.search(r'free percentage:\s*(\d+)%',mp);free=int(m.group(1)) if m else None;samples.append({'seconds':time.monotonic()-start,'rssBytes':rss,'pressure':pressure,'freePercent':free})
 if rss>6*2**30 or time.monotonic()-start>600 or pressure=='4' or (free is not None and free<10):
  reason='Owned RSS/time/pressure budget exceeded';p.terminate()
  try:p.wait(timeout=15)
  except subprocess.TimeoutExpired:p.kill()
  break
rc=p.wait();(o/'process.json').write_text(json.dumps({'exitCode':rc,'seconds':time.monotonic()-start,'reason':reason,'samples':samples},indent=2));print('Unity exit',rc)

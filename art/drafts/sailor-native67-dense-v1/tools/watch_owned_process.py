"""Bounded owned Blender guard; never touches another process."""
import subprocess,time,json,sys,re,os,signal
from pathlib import Path
pid=int(sys.argv[1]);out=Path(sys.argv[2]);r={'ownedPID':pid,'rssLimitGiB':6,'samples':[],'status':'MONITORING'}
def cmd(args):return subprocess.run(args,text=True,stdout=subprocess.PIPE,stderr=subprocess.DEVNULL).stdout.strip()
for i in range(100):
 line=cmd(['ps','-p',str(pid),'-o','rss=','-o','comm='])
 if not line:r['status']='PROCESS_EXITED';break
 assert '/Blender.app/Contents/MacOS/Blender' in line,'PID reused/not owned Blender'
 rss=int(line.split()[0])*1024;mp=cmd(['memory_pressure','-Q']);m=re.search(r'free percentage:\s*(\d+)%',mp);pct=int(m.group(1)) if m else None;pressure=cmd(['sysctl','-n','kern.memorystatus_vm_pressure_level']);r['samples'].append({'monotonic':time.monotonic(),'rssBytes':rss,'availablePercent':pct,'pressureLevel':pressure})
 r['peakRSSBytes']=max(x['rssBytes'] for x in r['samples'])
 if rss>6*2**30 or pressure=='4' or (pct is not None and pct<10):r['status']='GUARD_SIGINT_OWNED_PROCESS';os.kill(pid,signal.SIGINT);out.write_text(json.dumps(r,indent=2)+'\n');break
 out.write_text(json.dumps(r,indent=2)+'\n');time.sleep(10)
else:r['status']='MONITOR_BUDGET_EXHAUSTED'
out.write_text(json.dumps(r,indent=2)+'\n')

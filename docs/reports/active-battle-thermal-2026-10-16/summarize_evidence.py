#!/usr/bin/env python3
"""Post-run evidence summary; no engine/phone access or quality writes."""
import argparse,csv,json,math,statistics
from pathlib import Path
p=argparse.ArgumentParser();p.add_argument('run',type=Path);a=p.parse_args()
def props(path):return dict(line.split('=',1) for line in path.read_text().splitlines() if '=' in line)
config=props(a.run/'configuration.txt');result=props(a.run/'result.txt');frames=list(csv.DictReader((a.run/'frames.csv').open()));slow=list(csv.DictReader((a.run/'slow.csv').open()))
def stats(values):
 x=sorted(v for v in values if math.isfinite(v))
 return None if not x else dict(samples=len(x),mean=statistics.mean(x),median=statistics.median(x),p95=x[max(0,math.ceil(.95*len(x))-1)],worst=x[-1])
# Inclusive recorder samples can overlap; do not sum or subtract them as CPU attribution.
marker_names=('main','render','wait','ground','cap','pose','contact')
def marker_stats(rows):
 out={}
 for name in marker_names:
  key=name+'Ms'
  valid=config.get(name+'Marker','').lower()=='true'
  values=[float(f[key]) for f in rows if key in f] if valid else []
  out[name]=stats(v for v in values if v>=0)
 return out
seen=set();gpu_by_stamp={};raw_zero=repeat=missing=late_positive=0
for f in frames:
 stamp=int(f['timestamp']);v=float(f['gpuRawMs'])
 if not stamp or not math.isfinite(v):missing+=1;continue
 if v<=0:
  if stamp in seen:repeat+=1
  else:raw_zero+=1
  seen.add(stamp);continue
 if stamp in gpu_by_stamp:repeat+=1;continue
 if stamp in seen:late_positive+=1
 seen.add(stamp);gpu_by_stamp[stamp]=v
cpu=[];cpu_intervals=[]
for before,after in zip(slow,slow[1:]):
 t=float(after['elapsed'])-float(before['elapsed']);d=float(after['cpuProcessSeconds'])-float(before['cpuProcessSeconds'])
 if t>0 and d>=0 and float(before['cpuProcessSeconds'])>=0:
  cpu.append(d/t);cpu_intervals.append((float(before['elapsed']),float(after['elapsed']),d))
expected=int(config['durationSeconds']);completed=result.get('reason')==f'completed{expected}seconds' and result.get('errors')=='0' and float(result.get('elapsed','0'))>=expected-1
out=dict(status='COMPLETED_DIAGNOSTIC_NOT_POWER_OR_QUALITY_ACCEPTANCE' if completed else 'INCOMPLETE_OR_FAILED',configuration=config,result=result,cpuMarkersMs=marker_stats(frames),cpuMarkerScope='Inclusive per-frame recorder samples; overlapping markers must not be summed; unavailable recorders are null. Valid-but-all-zero timing without sample counts does not prove zero work',cpuMarkerPositiveSamples={name:sum(float(f.get(name+'Ms','0'))>0 for f in frames) for name in marker_names},wallMs=stats([float(f['wallMs']) for f in frames]),validUniqueGpuMs=stats(list(gpu_by_stamp.values())),gpuMissingOrInvalid=missing,gpuNewZeroOrNegative=raw_zero,gpuRepeated=repeat,gpuCompletedPositiveAfterEarlierZero=late_positive,processCpuCoreEquivalents=stats(cpu),observedThermalStates=sorted(set(int(f['thermal']) for f in slow)),maxPhysicalFootprintBytes=max([int(f['physicalFootprintBytes']) for f in slow if int(f['physicalFootprintBytes'])>0] or [0]) or None,fightImpactsObserved=any(int(f['hitA'])>0 or int(f['hitB'])>0 for f in slow),buckets=[])
for start in range(0,expected,60):
 fs=[f for f in frames if start<=float(f['elapsed'])<start+60]
 ss=[f for f in slow if start<=float(f['elapsed'])<start+60]
 # Whole native CPU intervals only: no invented interpolation across bucket boundaries.
 intervals=[(b-a,d) for a,b,d in cpu_intervals if a>=start and b<=start+60]
 covered=sum(t for t,d in intervals)
 out['buckets'].append(dict(seconds=[start,start+60],cpuMarkersMs=marker_stats(fs),wallMs=stats([float(f['wallMs']) for f in fs]),processCpuCoreEquivalents=sum(d for t,d in intervals)/covered if covered else None,cpuCoveredSeconds=covered,observedThermalStates=sorted(set(int(f['thermal']) for f in ss)),maxPhysicalFootprintBytes=max([int(f['physicalFootprintBytes']) for f in ss if int(f['physicalFootprintBytes'])>0] or [0]) or None))
(a.run/'summary.json').write_text(json.dumps(out,indent=2));print(json.dumps({k:v for k,v in out.items() if k not in ('configuration','buckets')},indent=2));raise SystemExit(0 if completed else 1)

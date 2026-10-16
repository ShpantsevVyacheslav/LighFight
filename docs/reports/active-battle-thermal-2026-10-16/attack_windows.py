#!/usr/bin/env python3
"""Describe observed player attack windows, not causal CPU attribution or phone power."""
import argparse,csv,json,math,statistics
from pathlib import Path

def summarize(run):
 config=dict(line.split('=',1) for line in (run/'configuration.txt').read_text().splitlines() if '=' in line)
 ids=config['allMoveIds'].split(';')
 timings={}
 for entry in config['allMoveTimings'].split(';'):
  name,startup,active,recovery=entry.split(':');timings[name]=tuple(map(int,(startup,active,recovery)))
 groups={}
 for row in csv.DictReader((run/'frames.csv').read_text().splitlines()):
  index=int(row['moveAIndex'])
  if index==-1: name,phase='idle','idle'
  else:
   if not 0<=index<len(ids):raise ValueError(f'Unknown actual attack index {index}')
   name=ids[index];startup,active,_=timings[name];frame=int(row['moveAFrame'])
   if frame<0:raise ValueError('Attack has negative phase')
   phase='startup' if frame<startup else 'active' if frame<startup+active else 'recovery'
  groups.setdefault((name,phase),[]).append(row)
 def stats(rows,key):
  values=[float(r[key]) for r in rows if key in r and math.isfinite(float(r[key])) and float(r[key])>=0]
  values.sort()
  return None if not values else dict(samples=len(values),median=statistics.median(values),p95=values[max(0,math.ceil(.95*len(values))-1)],worst=values[-1])
 result=[]
 for (name,phase),rows in sorted(groups.items()):
  markers={m:stats(rows,m+'Ms') if config.get(m+'Marker','').lower()=='true' else None for m in ('main','render','wait','ground','cap','pose','contact')}
  result.append(dict(playerMove=name,phase=phase,renderedFrames=len(rows),wallMs=stats(rows,'wallMs'),inclusiveCpuMarkersMs=markers,opponentAttackingFrames=sum(int(r['moveBIndex'])>=0 for r in rows),playerDownFrames=sum(int(r['downA'])>0 for r in rows)))
 return dict(scope='Observed rendered player phases; opponent work and all other scene work coexist. Inclusive CPU markers overlap. No causal, energy, or iPhone heat verdict.',windows=result)
if __name__=='__main__':
 parser=argparse.ArgumentParser();parser.add_argument('run',type=Path);args=parser.parse_args()
 output=summarize(args.run);(args.run/'attack-windows.json').write_text(json.dumps(output,indent=2));print(json.dumps(output,indent=2))

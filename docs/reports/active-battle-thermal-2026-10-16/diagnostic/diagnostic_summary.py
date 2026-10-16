"""Offline EOF diagnostics. No additional runtime scans or changes to root summaries."""
import csv,json,math,sys
from pathlib import Path

def stats(values):
 a=sorted(float(x) for x in values if math.isfinite(float(x)))
 return {'count':len(a),'minimum':min(a) if a else None,'mean':sum(a)/len(a) if a else None,'p95':a[min(len(a)-1,int((len(a)-1)*.95))] if a else None,'worst':max(a) if a else None}
def summarize(rows):
 if not rows:raise ValueError('No measured frames')
 out={'scope':'Actual EOF support state; rawMinimum+correction is cached support floor, NOT independent full mesh floor','frames':len(rows),'actors':{},'markers':{}}
 for a in 'AB':
  delta=[int(r['minEvalDelta'+a]) for r in rows]
  if min(delta)<0:raise ValueError('Counter moved backwards')
  if any(int(r['supportPoints'+a])<=0 for r in rows):raise ValueError('Empty support')
  out['actors'][a]={'minimumEvaluations':sum(delta),'evaluationsPerFrame':stats(delta),'supportKinds':{str(k):sum(int(r['supportKind'+a])==k for r in rows) for k in [0,1,2]},'bodyMode':{},'supportFloorAfterCorrectionM':stats(float(r['rawMinimum'+a])+float(r['correction'+a]) for r in rows),'correctionM':stats(r['correction'+a] for r in rows)}
  for k in [1,2]:
   subset=[r for r in rows if int(r['supportKind'+a])==k]
   out['actors'][a]['bodyMode'][str(k)]={'frames':len(subset),'points':sorted(set(int(r['supportPoints'+a]) for r in subset)),'evaluationsPerFrame':stats(r['minEvalDelta'+a] for r in subset),'capMs':stats(r['capMs'] for r in subset)}
 for m in ['main','render','wait','ground','cap','pose','contact']:
  out['markers'][m]={'observedBlockFrames':sum(int(r[m+'Blocks'])>0 for r in rows),'blocks':stats(r[m+'Blocks'] for r in rows),'elapsedMs':stats(r[m+'Ms'] for r in rows)}
 return out
if __name__=='__main__':
 p=Path(sys.argv[1]);result=summarize(list(csv.DictReader((p/'frames.csv').open())));(p/'diagnostic-summary.json').write_text(json.dumps(result,indent=2));print(json.dumps(result,indent=2))

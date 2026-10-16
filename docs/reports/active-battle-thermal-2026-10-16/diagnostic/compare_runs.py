import csv,json,sys
from pathlib import Path
from diagnostic_summary import stats,summarize
p=Path(__file__).parent
out={'scope':'Matched120s Mac Development selected-inline; not phone/power, not deterministic gameplay parity','runs':{}}
for name in ['baseline','candidate']:
 d=p/'Evidence'/name;rows=list(csv.DictReader((d/'frames.csv').open()));diag=summarize(rows);(d/'diagnostic-summary.json').write_text(json.dumps(diag,indent=2))
 r={'result':(d/'result.txt').read_text(),'coverage':(d/'coverage.csv').read_text(),'coverageResult':(d/'coverage-result.txt').read_text(),'frames':len(rows),'metrics':{k:stats(x[k] for x in rows) for k in ['wallMs','mainMs','gpuRawMs','groundMs','capMs','poseMs','contactMs']},'actors':diag['actors'],'markers':diag['markers'],'retroBody':{},'bothNativeSupport':{}}
 for label,predicate in [('retroBody',lambda x:int(x['supportKindA'])==2),('bothNativeSupport',lambda x:int(x['supportKindA'])==1 and int(x['supportKindB'])==1)]:
  subset=[x for x in rows if predicate(x)];r[label]={'frames':len(subset),'metrics':{k:stats(x[k] for x in subset) for k in ['wallMs','groundMs','capMs','poseMs','contactMs']},'scansA':stats(x['minEvalDeltaA'] for x in subset),'scansB':stats(x['minEvalDeltaB'] for x in subset)}
 out['runs'][name]=r
(p/'comparison.json').write_text(json.dumps(out,indent=2));print(json.dumps({k:{'frames':v['frames'],'metrics':v['metrics'],'retroBody':v['retroBody']} for k,v in out['runs'].items()},indent=2))

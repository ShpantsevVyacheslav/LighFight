from pathlib import Path
import json,math
R=Path(__file__).resolve().parents[4];B=R/'art/drafts/sailor-native67-dense-v1';O=R/'docs/reports/native67-zero-twist-dances-2026-10-19';d=json.loads((B/'native67-zero-twist-dense-motion-v1/result.json').read_text());assert len(d['poses'])==1111 and not d.get('error');old=json.loads((B/'native67-swing-twist-dense-audit-v1/result.json').read_text());assert len(old['poses'])==1111

def angle(a,b):
 av=[a[k] for k in ['x','y','z','w']];bv=[b[k] for k in ['x','y','z','w']];den=math.sqrt(sum(x*x for x in av)*sum(x*x for x in bv));dot=min(1,abs(sum(x*y for x,y in zip(av,bv))/den));return 2*math.atan2(math.sqrt(max(0,1-dot*dot)),dot)*180/math.pi
rows=[];maxPos=0
for p,q in zip(d['poses'],old['poses']):
 assert p['action']==q['action'] and p['seconds']==q['seconds']
 maxPos=max(maxPos,max(math.sqrt(sum((a[k]-b[k])**2 for k in ['x','y','z'])) for a,b in zip(p['target'],q['target'])))
for action in sorted(set(p['action'] for p in d['poses'])):
 ps=[p for p in d['poses'] if p['action']==action]
 for j,name in [(7,'LeftUpperArm'),(11,'RightUpperArm'),(8,'LeftForearm'),(12,'RightForearm')]:
  src=[];dst=[];dt=[]
  for a,b in zip(ps,ps[1:]):src.append(angle(a['sourceWorldRotations'][j],b['sourceWorldRotations'][j]));dst.append(angle(a['targetWorldRotations'][j],b['targetWorldRotations'][j]));dt.append(b['seconds']-a['seconds'])
  rows.append({'action':action,'bone':name,'sourceMaxStepDeg':max(src),'zeroMaxStepDeg':max(dst),'sourceMaxAngularSpeedDegSec':max(v/t for v,t in zip(src,dt)),'zeroMaxAngularSpeedDegSec':max(v/t for v,t in zip(dst,dt)),'maxSourceVsTargetStepDifferenceDeg':max(abs(a-b) for a,b in zip(src,dst))})
r={'samples':1111,'maxNormalizedJointPositionDifferenceVsBaseline':maxPos,'rows':rows,'limits':'Uniform101phase samples per clip; finite sampling does not establish continuous smoothness. Normalized joint positions use body basis/hip height, not world meters. Source-vs-target upper-arm angular speed may differ by design; forearm angular step should remain equivalent. No dance-to-guard transition tested here.'};(O/'dense-motion-summary.json').write_text(json.dumps(r,indent=2));print('max position normalized',maxPos);print('largest angular step',max(rows,key=lambda r:r['zeroMaxStepDeg']));print('max forearm angular step error',max(r['maxSourceVsTargetStepDifferenceDeg'] for r in rows if 'Forearm' in r['bone']))

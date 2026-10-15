"""Verify durable actual-player CSV; never treats a log-only PASS as sufficient."""
import csv,json,sys
from pathlib import Path
p=Path(sys.argv[1]);rows=list(csv.DictReader(p.open()));assert rows
assert {r['scenario'] for r in rows}=={'0','1','2'}
assert {r['ticksPerFrame'] for r in rows}=={'1','3'}
issues=[]
for r in rows:
 def n(k):return float(r[k])
 if n('wholeMeshMinY')<-.0005 or n('fullShoeMinY')<-.0005:issues.append(('floor',r))
 if r['sameContactState']=='True' and n('contactEofMaxM')>.0005:issues.append(('cached parity',r))
 if n('sourceShapeDeltaM')>.00001 or n('hipXZDeltaM')>.00001 or n('boneLocalDeltaM')>.00001:issues.append(('shape/anchor',r))
 if r['scenario']=='2' and 210<=int(r['tick'])<240:
  if any(r[k]=='True' for k in ['bodySupportSelected','previousPoseContainsRecovery','fighterRecoveryRequired']):issues.append(('fresh Greeting stale recovery',r))
fresh=[r for r in rows if r['scenario']=='2' and 210<=int(r['tick'])<240]
settled=[r for r in rows if (r['scenario']!='2' and int(r['tick'])>=138) or (r['scenario']=='2' and 180<=int(r['tick'])<210)]
assert fresh and settled
assert all(r['bodySupportSelected']=='False' and r['previousPoseContainsRecovery']=='False' and r['fighterRecoveryRequired']=='False' for r in settled),'stale support after settled return'
assert not issues,f'{len(issues)} failures; first={issues[0]}'
print(json.dumps({'status':'PASS','samples':len(rows),'freshGreetingSamples':len(fresh),'settledClearanceSamples':len(settled),'sameStateCachedSamples':sum(r['sameContactState']=='True' for r in rows),'legitimatePostCoreStateChanges':sum(r['sameContactState']=='False' for r in rows),'wholeMinM':min(float(r['wholeMeshMinY']) for r in rows),'shoeMinM':min(float(r['fullShoeMinY']) for r in rows),'sameStateContactMaxM':max(float(r['contactEofMaxM']) for r in rows if r['sameContactState']=='True'),'sourceShapeMaxM':max(float(r['sourceShapeDeltaM']) for r in rows),'hipXZMaxM':max(float(r['hipXZDeltaM']) for r in rows)},indent=2))

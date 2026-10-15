import csv,json,math
from pathlib import Path
p=Path(__file__).parent
canon=lambda n:n.lower().replace('mixamorig:','').replace('_','').replace(' ','')
def angle(a,b,c):
 u=[y-x for x,y in zip(a,b)];v=[y-x for x,y in zip(b,c)];den=math.sqrt(sum(x*x for x in u)*sum(x*x for x in v));assert den>1e-12
 return math.degrees(math.acos(max(-1,min(1,sum(x*y for x,y in zip(u,v))/den))))
report=[]
for name in ['box_01','box_02','front_kick_01']:
 frames={}
 for r in csv.DictReader((p/(name+'.trajectory.csv')).open()):frames.setdefault(float(r['seconds']),{})[canon(r['joint'])]=tuple(float(r[k]) for k in ['x','y','z'])
 entry={'take':name,'geometricPhalanxBendsDegrees':{},'warning':'Bone-direction diagnostic only; not skin/fist/thumb contact acceptance.'}
 for side in ['left','right']:
  for finger in ['index','middle','ring','pinky']:
   prefix=side+finger;values=[angle(f[prefix+'proximal'],f[prefix+'intermediate'],f[prefix+'distal']) for f in frames.values()]
   entry['geometricPhalanxBendsDegrees'][prefix]={'min':min(values),'max':max(values)}
 report.append(entry)
(p/'finger-bends.json').write_text(json.dumps(report,indent=2)+'\n');print(json.dumps(report,indent=1))

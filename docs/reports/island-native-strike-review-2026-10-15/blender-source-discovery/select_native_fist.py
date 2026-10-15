import csv,math,json
from pathlib import Path
p=Path(__file__).parent;fs={}
canon=lambda n:n.lower().replace('mixamorig:','').replace('_','').replace(' ','')
for r in csv.DictReader((p/'box_02.trajectory.csv').open()):fs.setdefault(float(r['seconds']),{})[canon(r['joint'])]=tuple(float(r[k]) for k in ['x','y','z'])
def angle(a,b,c):
 u=[y-x for x,y in zip(a,b)];v=[y-x for x,y in zip(b,c)];return math.degrees(math.acos(max(-1,min(1,sum(x*y for x,y in zip(u,v))/math.sqrt(sum(x*x for x in u)*sum(x*x for x in v))))))
rank=[]
for t,f in fs.items():
 bends=[angle(f[s+k+'proximal'],f[s+k+'intermediate'],f[s+k+'distal']) for s in ['left','right'] for k in ['index','middle','ring','pinky']];rank.append((min(bends),sum(bends)/8,t,bends))
best=max(rank);r={'take':'box_02','sourceSeconds':best[2],'minNonThumbBendDegrees':best[0],'meanNonThumbBendDegrees':best[1],'allEightBends':best[3],'accepted':False,'selectionCriterion':'Maximize minimum geometric bend of eight nonthumb fingers across671 box_02 samples. Not thumb or skinned fist contact acceptance.'};print(json.dumps(r,indent=2));(p/'native-fist-candidate.UNACCEPTED.json').write_text(json.dumps(r,indent=2)+'\n')

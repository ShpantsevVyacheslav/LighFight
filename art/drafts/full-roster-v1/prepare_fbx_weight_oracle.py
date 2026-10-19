"""Read actual derived FBX coefficients, no DCC or source edits."""
from pathlib import Path
import sys,json,hashlib,numpy as np
R=Path(__file__).resolve().parents[3];sys.path.insert(0,str(R/'art/drafts/sailor-native67-dense-v1/tools'));from audit_sources_offline import Reader,child,clean
role=sys.argv[1];p=R/('unity/Assets/Native67SpatialReview/'+role.title()+'DenseReview.fbx');ns=Reader(p).all();objs=next(n for n in ns if n.name=='Objects').c;connections=next(n for n in ns if n.name=='Connections').c;models={n.p[0]:n for n in objs if n.name=='Model'};clusters={n.p[0]:n for n in objs if n.name=='Deformer' and n.p[-1]=='Cluster'};geo=next(n for n in objs if n.name=='Geometry' and n.p[-1]=='Mesh');N=len(child(geo,'Vertices').p[0])//3;rows=[]
for c in connections:
 if c.name=='C' and c.p[0]=='OO' and c.p[1] in models and c.p[2] in clusters:
  cl=clusters[c.p[2]];idx=child(cl,'Indexes');w=child(cl,'Weights')
  if idx and w:rows.append((clean(models[c.p[1]].p[1]),idx.p[0],w.p[0]))
names=sorted(n for n,i,w in rows);v=np.concatenate([i for n,i,w in rows]);j=np.concatenate([np.full(len(i),names.index(n),np.uint16) for n,i,w in rows]);w=np.concatenate([w for n,i,w in rows]);assert np.all(w>0);order=np.lexsort((j,v));counts=np.bincount(v,minlength=N);assert counts.min()>0 and counts.max()<256;O=R/('art/drafts/full-roster-v1/'+role+'-weight-oracle');O.mkdir(exist_ok=True);counts.astype('u1').tofile(O/'counts.bin');j[order].astype('<u2').tofile(O/'joints.bin');w[order].astype('<f4').tofile(O/'weights.bin');(O/'bones.txt').write_text('\n'.join(names)+'\n');m={'role':role,'vertices':N,'positiveCoefficients':len(w),'maxInfluences':int(counts.max()),'sourceFBX':str(p.relative_to(R)),'sourceSHA256':hashlib.sha256(p.read_bytes()).hexdigest(),'sourceIdUVChannel':1,'sourceIdDenominator':2097152,'files':{f.name:hashlib.sha256(f.read_bytes()).hexdigest() for f in O.iterdir() if f.is_file()}};(O/'manifest.json').write_text(json.dumps(m,indent=2));print(json.dumps(m))

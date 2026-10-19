"""Offline original-index topology/weight witnesses. No bpy or weight changes."""
from pathlib import Path
import json,hashlib
import numpy as np
O=Path(__file__).resolve().parents[1];BASE=O.parent;PACKET=BASE.parent/'dense-def-fbx-packet-v1'
source=PACKET/'original-indexed-attributes.npz';z=np.load(source);tri=z['triangles'];pos=z['positionsBlenderLocal'];ji=z['jointIndices'];w=z['weights'];names=json.loads((PACKET/'manifest.json').read_text())['boneNames']
controls=json.loads((BASE/'corrective-v1/artist-contour-v3/prepare-v3/explicit-artist-controls.json').read_text());rows=[]
for face in controls['originalFaceIDs']:
 core=tri[face];ring=core.copy()
 for _ in range(2):ring=np.unique(tri[np.any(np.isin(tri,ring),axis=1)])
 local=tri[np.all(np.isin(tri,ring),axis=1)];e=np.unique(np.sort(np.concatenate([local[:,[0,1]],local[:,[1,2]],local[:,[2,0]]]),axis=1),axis=0)
 def named(v):return {names[int(j)]:float(vw) for j,vw in zip(ji[v],w[v]) if vw>0}
 rows.append({'originalFaceID':int(face),'corners':[{'vertex':int(v),'restLocal':pos[v].tolist(),'weights':named(v)} for v in core],'twoRingVertices':ring.tolist(),'twoRingTriangleCount':len(local),'twoRingEdgeCount':len(e),'restEdgeLengthLocal':dict(zip(['min','median','max'],map(float,np.quantile(np.linalg.norm(pos[e[:,0]]-pos[e[:,1]],axis=1),[0,.5,1])))),'meanNamedWeight':{names[i]:float(sum(w[ring][ji[ring]==i])/len(ring)) for i in np.unique(ji[ring][w[ring]>0])}})
r={'status':'ORIGINAL_DIAGNOSTIC_NO_WEIGHT_CHANGE','sourceSHA256':hashlib.sha256(source.read_bytes()).hexdigest(),'controls':rows,'limits':['Control faces identify actual visible cloth from reviewed -X camera, not a proved sleeve/torso seam.','Two rings are topology witnesses, not an approved correction mask.','Named weights alone do not demonstrate anatomical error.']}
(O/'control-topology-original.json').write_text(json.dumps(r,indent=2)+'\n')
print('WROTE',len(rows),'original control witnesses')

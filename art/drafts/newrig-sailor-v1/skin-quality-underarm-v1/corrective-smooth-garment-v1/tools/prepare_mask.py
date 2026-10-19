"""Offline bounded mask feather inside reviewed original cloth IDs only."""
import os
for k in ['OPENBLAS_NUM_THREADS','OMP_NUM_THREADS','MKL_NUM_THREADS']:os.environ[k]='1'
from pathlib import Path
import numpy as np,json,heapq,hashlib,time
O=Path(__file__).resolve().parents[1];B=O.parent;P=B.parent/'dense-def-fbx-packet-v1';start=time.monotonic();source=B/'corrective-v1/artist-contour-v3/artist-mask-v3.npz';mask=np.load(source);ids=mask['originalVertexIndices'];protected=mask['protectedOriginalVertexIndices'];z=np.load(P/'original-indexed-attributes.npz');tri=z['triangles'];raw=z['positionsBlenderLocal'];mw=z['meshWorldBlender'];rest=raw@mw[:3,:3].T+mw[:3,3];m=np.zeros(len(raw),bool);m[ids]=True;assert not m[protected].any()
t=tri[np.any(m[tri],axis=1)];e=np.unique(np.sort(np.concatenate([t[:,[0,1]],t[:,[1,2]],t[:,[2,0]]]),axis=1),axis=0);cross=e[m[e[:,0]]!=m[e[:,1]]];boundary=np.unique(cross[m[cross]]);inner=e[np.all(m[e],axis=1)];adj={int(v):[] for v in ids}
for (a,b),d in zip(inner,np.linalg.norm(rest[inner[:,0]]-rest[inner[:,1]],axis=1)):adj[int(a)].append((int(b),float(d)));adj[int(b)].append((int(a),float(d)))
dist=np.full(len(raw),np.inf);dist[boundary]=0;queue=[(0.,int(v)) for v in boundary];heapq.heapify(queue)
while queue:
 d,v=heapq.heappop(queue)
 if d!=dist[v]:continue
 for q,l in adj[v]:
  nd=d+l
  if nd<dist[q]:dist[q]=nd;heapq.heappush(queue,(nd,q))
assert np.isfinite(dist[ids]).all();u=np.minimum(dist[ids]/.006,1);feather=(u*u*(3-2*u)).astype(np.float32);assert np.all(feather[np.isin(ids,boundary)]==0)
np.savez_compressed(O/'garment-mask-feather-v1.npz',originalVertexIndices=ids,modifierWeight=feather,protectedOriginalVertexIndices=protected,fixedBoundary=boundary)
r={'status':'OFFLINE_FEATHER_SUBSET_OF_REVIEWED_GARMENT','reviewedSupportSource':str(source),'sourceMaskSHA256':hashlib.sha256(source.read_bytes()).hexdigest(),'sourceIndexedSHA256':hashlib.sha256((P/'original-indexed-attributes.npz').read_bytes()).hexdigest(),'reviewedSupportVertices':len(ids),'positiveModifierWeightVertices':int(np.sum(feather>0)),'zeroBoundaryVertices':len(boundary),'protectedVertices':len(protected),'protectedPositiveWeight':0,'featherM':.006,'method':'Original-edge intrinsic distance inward from reviewed support boundary; cubic smoothstep. No support expansion.','elapsedSeconds':time.monotonic()-start};(O/'mask-report.json').write_text(json.dumps(r,indent=2)+'\n');print(json.dumps(r))

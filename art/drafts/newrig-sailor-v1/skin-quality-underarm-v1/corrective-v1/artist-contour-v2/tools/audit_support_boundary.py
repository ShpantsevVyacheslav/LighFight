"""Offline one-ring support expansion witness; does not change protected IDs."""
from pathlib import Path
import json,numpy as np
O=Path(__file__).resolve().parents[1];CO=O.parent;SKIN=CO.parent;P=SKIN.parent/'dense-def-fbx-packet-v1';a=np.load(P/'original-indexed-attributes.npz');raw=a['positionsBlenderLocal'].astype(float);tri=a['triangles'];j=a['jointIndices'];w=a['weights'].astype(float);mw=a['meshWorldBlender'];rest=raw@mw[:3,:3].T+mw[:3,3];pose=np.load(CO/'weight-witness-v1/frozen-dance50-world-skin.npz')['worldSkin'];points=np.zeros_like(raw)
for lo in range(0,len(raw),65536):
 hi=min(lo+65536,len(raw));p=np.column_stack((raw[lo:hi],np.ones(hi-lo)));q=np.zeros((hi-lo,4))
 for k in range(4):q+=np.einsum('nij,nj->ni',pose[j[lo:hi,k]],p)*w[lo:hi,k,None]
 points[lo:hi]=q[:,:3]
r=np.load(SKIN/'regions-v1/region-proposals.npz');protected=np.unique(np.concatenate([r[n] for n in r.files if 'anchor' in n]+[np.flatnonzero(rest[:,2]>=.815)]));allowed=(rest[:,0]>-.215)&(rest[:,0]<-.035)&(rest[:,2]>.595)&(rest[:,2]<.815)&(np.abs(rest[:,1])<.12);allowed[protected]=False;flags=allowed[tri];border=flags.any(1)&~flags.all(1);inside=np.unique(tri[border][flags[border]]);outside=np.unique(tri[border][~flags[border]]);controls=json.loads((O/'prepare/explicit-artist-controls.json').read_text());rows=[]
for center in np.array(controls['sourceWorldM']):
 row={}
 for name,ids in [('oldInnerBoundary',inside),('expandedZeroBoundary',outside),('originalProtected',protected)]:
  d=np.linalg.norm(points[ids]-center,axis=1);i=int(np.argmin(d));v=int(ids[i]);row[name]={'distanceM':float(d[i]),'originalVertexIndex':v,'restWorldM':rest[v].tolist(),'protected':bool(np.any(protected==v))}
 rows.append(row)
result={'status':'OFFLINE_EXACT_BOUNDARY_WITNESS_NOT_RENDER_ACCEPTANCE','fixedMarginM':.006,'explanation':'Old feather forced zero at last allowed cloth vertices. New zero boundary uses adjacent excluded vertices; allowed boundary cloth gets support while every excluded/protected ID remains zero. No threshold or protected membership changed.','protectedOriginalCount':len(protected),'oldInnerBoundaryVertices':len(inside),'excludedZeroBoundaryVertices':len(outside),'controls':rows};(O/'evidence/support-expansion-offline.json').write_text(json.dumps(result,indent=2)+'\n');print(json.dumps(rows,indent=2))

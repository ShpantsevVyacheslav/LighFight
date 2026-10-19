"""Offline1core topology proposal; no weights/geometry changed. Vertex z-buffer preview only."""
import os
for key in ['OPENBLAS_NUM_THREADS','OMP_NUM_THREADS','MKL_NUM_THREADS']:os.environ[key]='1'
from pathlib import Path
import numpy as np,json,heapq,hashlib,time
from PIL import Image,ImageDraw
O=Path(__file__).resolve().parents[1];B=O.parent;P=B.parent/'dense-def-fbx-packet-v1';C=B/'corrective-v1';start=time.monotonic()
z=np.load(P/'original-indexed-attributes.npz');raw=z['positionsBlenderLocal'];tri=z['triangles'];ji=z['jointIndices'];w=z['weights'];mw=z['meshWorldBlender'];N=len(raw);names=json.loads((P/'manifest.json').read_text())['boneNames']
rest=raw@mw[:3,:3].T+mw[:3,3];skin=np.load(C/'weight-witness-v1/frozen-dance50-world-skin.npz')['worldSkin'];points=np.zeros((N,3));hp=np.c_[raw,np.ones(N)]
for k in range(4):points+=np.einsum('nij,nj->ni',skin[ji[:,k]],hp)[:,:3]*w[:,k,None]
mask=np.load(C/'artist-contour-v3/artist-mask-v3.npz');protected=mask['protectedOriginalVertexIndices'];seed=mask['originalVertexIndices'];allowed=(rest[:,0]>-.215)&(rest[:,0]<-.035)&(rest[:,2]>.595)&(rest[:,2]<.815)&(abs(rest[:,1])<.12);allowed[protected]=False
# Only real original triangle edges. Never nearest-neighbor weld close arm/torso.
t=tri[np.all(allowed[tri],axis=1)];edges=np.unique(np.sort(np.concatenate([t[:,[0,1]],t[:,[1,2]],t[:,[2,0]]]),axis=1),axis=0);ids=np.unique(t);adj={int(v):[] for v in ids};length=np.linalg.norm(rest[edges[:,0]]-rest[edges[:,1]],axis=1)
for (a,b),d in zip(edges,length):adj[int(a)].append((int(b),float(d)));adj[int(b)].append((int(a),float(d)))
faces=json.loads((C/'artist-contour-v3/prepare-v3/explicit-artist-controls.json').read_text())['originalFaceIDs'];dist=np.full(N,np.inf);owner=np.full(N,-1,np.int8);heap=[]
for group,face in enumerate(faces):
 for v in tri[face]:
  assert allowed[v] and int(v) in adj;dist[v]=0;owner[v]=group;heapq.heappush(heap,(0,int(v),group))
while heap:
 d,v,g=heapq.heappop(heap)
 if d!=dist[v] or g!=owner[v]:continue
 for q,l in adj[v]:
  nd=d+l
  if nd<dist[q]:dist[q]=nd;owner[q]=g;heapq.heappush(heap,(nd,q,g))
# Propose limited25mm intrinsic neighborhoods around actual visible control faces.
patch=np.flatnonzero(dist<=.025);assert not np.intersect1d(patch,protected).size
pm=np.zeros(N,bool);pm[patch]=True;cut=edges[pm[edges[:,0]]!=pm[edges[:,1]]];boundary=np.unique(cut[pm[cut]]);interior=patch[~np.isin(patch,boundary)];components=[];todo=set(map(int,patch))
while todo:
 v=todo.pop();stack=[v];part=[v]
 while stack:
  for q,_ in adj[stack.pop()]:
   if q in todo:todo.remove(q);stack.append(q);part.append(q)
 components.append(np.array(part,int))
# Single-source witness of the real cloth route between torso-side and sleeve-side
# controls. This does NOT add a bridge or change proposed support.
routeDist={int(v):0. for v in tri[faces[0]]};parent={};queue=[(0.,int(v)) for v in tri[faces[0]]];heapq.heapify(queue)
while queue:
 d,v=heapq.heappop(queue)
 if d!=routeDist[v]:continue
 for q,l in adj[v]:
  nd=d+l
  if nd<routeDist.get(q,float('inf')):routeDist[q]=nd;parent[q]=v;heapq.heappush(queue,(nd,q))
routeEnd=min(map(int,tri[faces[1]]),key=lambda v:routeDist.get(v,float('inf')));route=[routeEnd]
while route[-1] in parent:route.append(parent[route[-1]])
route=np.array(route,int);coreBands=[patch[(owner[patch]==i)&(dist[patch]<=.004)] for i in range(4)]
view=next(x for x in json.loads((C/'weight-witness-v1/renders/report.json').read_text())['renders'] if x['file']=='worst-edge-side.png');cam=np.array(view['cameraWorld']);target=np.array(view['cameraTarget']);forward=target-cam;forward/=np.linalg.norm(forward);right=np.cross(forward,[0,0,1]);right/=np.linalg.norm(right);up=np.cross(right,forward);q=points-target;W,H=640,768;scale=view['orthoScale'];xy=np.c_[W/2+(q@right)*H/scale,H/2-(q@up)*H/scale];pixels=np.rint(xy).astype(int);valid=(pixels[:,0]>=0)&(pixels[:,0]<W)&(pixels[:,1]>=0)&(pixels[:,1]<H);depth=(points-cam)@forward;zbuf=np.full(W*H,np.inf);pid=pixels[valid,1]*W+pixels[valid,0];np.minimum.at(zbuf,pid,depth[valid]);visible=np.zeros(N,bool);visible[valid]=depth[valid]<=zbuf[pid]+.0008
colors=[(255,70,70),(255,190,30),(70,220,80),(180,90,255)]
im=Image.open(C/'artist-contour-v3/build-v3/source-before-side.png').convert('RGB');draw=ImageDraw.Draw(im)
# Original prepared/build view is same fixed witness camera; no new render/crop.
for v in patch[visible[patch]]:
 x,y=pixels[v];draw.point((int(x),int(y)),fill=colors[int(owner[v])])
for v in boundary[visible[boundary]]:
 x,y=pixels[v];draw.ellipse((x-1,y-1,x+1,y+1),fill=(40,230,255))
draw.rectangle((0,0,W,64),fill=(15,15,15));draw.text((8,8),'B proposal: original-edge graph, 25mm rest geodesic',fill='white');draw.text((8,26),'4 colors=control domains, NOT sleeve/torso labels',fill='white');draw.text((8,44),'cyan=fixed outer boundary; vertex visibility approximation',fill='white');im.save(O/'connected-patch-side-proposal.png')
im=Image.open(C/'artist-contour-v3/build-v3/source-before-side.png').convert('RGB');draw=ImageDraw.Draw(im)
for v in route[visible[route]]:
 x,y=pixels[v];draw.ellipse((x-1,y-1,x+1,y+1),fill=(255,255,255))
for i,band in enumerate(coreBands):
 for v in band[visible[band]]:
  x,y=pixels[v];draw.ellipse((x-1,y-1,x+1,y+1),fill=colors[i])
for i,band in enumerate(coreBands):
 center=np.median(xy[band],axis=0);x,y=map(float,center);draw.ellipse((x-9,y-9,x+9,y+9),outline=colors[i],width=2);draw.text((x+12,y-8),str(i),fill=colors[i])
draw.rectangle((0,0,W,64),fill=(15,15,15));draw.text((8,8),'White = real original-edge route control0 to1',fill='white');draw.text((8,26),'Colored 4mm control cores = anchor-band proposals only',fill='white');draw.text((8,44),'No bridge/no weight edits. Needs anatomical review.',fill='white');im.save(O/'control-route-anchor-proposal.png')
np.savez_compressed(O/'connected-patch-proposal.npz',originalVertexIndices=patch,fixedOuterBoundary=boundary,protectedOriginalVertexIndices=protected,controlDomain=owner[patch],restGeodesicM=dist[patch],torsoSleeveRoute=route,**{'controlCore'+str(i):band for i,band in enumerate(coreBands)})
def audit(v):
 vals={names[int(i)]:float(w[v][ji[v]==i].sum()/len(v)) for i in np.unique(ji[v][w[v]>0])};return sorted(vals.items(),key=lambda x:-x[1])
r={'status':'TOPOLOGY_PROPOSAL_NOT_REVIEWED_NO_WEIGHT_CHANGE','sourceIndexedSHA256':hashlib.sha256((P/'original-indexed-attributes.npz').read_bytes()).hexdigest(),'vertices':N,'triangles':len(tri),'protected':len(protected),'allowedClothVertices':int(allowed.sum()),'controlFaces':faces,'intrinsicRadiusM':.025,'patchVertices':len(patch),'fixedOuterBoundary':len(boundary),'componentSizes':sorted([len(c) for c in components],reverse=True),'control0to1OriginalEdgeDistanceM':float(routeDist[routeEnd]),'routeVertexCount':len(route),'controlCore4mmCounts':[len(band) for band in coreBands],'unreachableAllowedVertices':int(np.sum(allowed&~np.isfinite(dist))),'weightAudit':{'wholePatch':audit(patch),'domains':[{'control':i,'vertices':int(np.sum(owner[patch]==i)),'meanNamedWeights':audit(patch[owner[patch]==i])} for i in range(4)]},'camera':view,'limits':['Unreviewed control Voronoi domains, not anatomical partitions.','25mm is a diagnostic support proposal, not accepted correction radius.','Original connectivity avoids spatial nearest-neighbor bridges; a real topological seam may still need manual classification.','Vertex z-buffer overlay approximates visibility, not full triangle rasterization.','No bone family automatically assigned; review named distributions with actual geometry before choosing torso/sleeve anchors.'],'elapsedSeconds':time.monotonic()-start};(O/'connected-patch-proposal.json').write_text(json.dumps(r,indent=2)+'\n');print(json.dumps({k:r[k] for k in ['patchVertices','fixedOuterBoundary','componentSizes','elapsedSeconds']}))

"""Offline1core true-bind bone overlay and connected cloth corridor. No bpy."""
import os
for key in ['OPENBLAS_NUM_THREADS','OMP_NUM_THREADS','MKL_NUM_THREADS']:os.environ[key]='1'
import struct
from pathlib import Path
import numpy as np,json,heapq,time
from PIL import Image,ImageDraw
HERE=Path(__file__).resolve();helper=HERE.parent/'prepare_connected_patch.py';prefix=helper.read_text().split('view=next(')[0];ns={'__file__':str(helper),'__name__':'source_graph'};exec(compile(prefix,str(helper),'exec'),ns)
globals().update({k:v for k,v in ns.items() if not k.startswith('__')});O=HERE.parents[1]
nda=Path('/tmp/lighfight-dense-adaptive-dance.nda')
with nda.open('rb') as f:
 assert f.read(4)==b'NDA1';version,keys,nb=struct.unpack('<iii',f.read(12));duration=struct.unpack('<d',f.read(8))[0];assert version==1 and nb==136
 nn=[f.read(struct.unpack('<i',f.read(4))[0]).decode() for _ in range(nb)];parents=np.frombuffer(f.read(nb*4),'<i4');rr=np.frombuffer(f.read(nb*16*8),'<f8').reshape(nb,4,4);aw=np.frombuffer(f.read(128),'<f8').reshape(4,4);mm=np.frombuffer(f.read(128),'<f8').reshape(4,4)
order=np.array([nn.index(n) for n in names]);restm=z['DEFRestArmature'];armworld=z['armatureWorldBlender'];err=float(np.max(abs(rr[order]-restm)));headerr=float(np.max(np.linalg.norm(rr[order,:3,3]-restm[:,:3,3],axis=1)));assert headerr<1e-6 and np.array_equal(aw,armworld) and np.array_equal(mm,mw),'Independent NDA head/world mismatch'
worldbind=armworld@restm;bind=np.linalg.inv(worldbind)@mw;pose=skin@np.linalg.inv(bind);headsrest=worldbind[:,:3,3];headspose=pose[:,:3,3]
# The explicitly named chain is an overlay of actual heads, not inferred anatomy.
chain=['DEF-spine.001','DEF-spine.002','DEF-breast.R','DEF-shoulder.R','DEF-upper_arm.R','DEF-upper_arm.R.001','DEF-forearm.R','DEF-forearm.R.001','DEF-hand.R'];chain=[n for n in chain if n in names]
view=next(x for x in json.loads((C/'weight-witness-v1/renders/report.json').read_text())['renders'] if x['file']=='worst-edge-side.png');cam=np.array(view['cameraWorld']);target=np.array(view['cameraTarget']);forward=target-cam;forward/=np.linalg.norm(forward);right=np.cross(forward,[0,0,1]);right/=np.linalg.norm(right);up=np.cross(right,forward);W,H=640,768;scale=view['orthoScale']
def project(v,center=target):
 q=v-center;return np.c_[W/2+(q@right)*H/scale,H/2-(q@up)*H/scale]
def overlay(im,heads,center,title):
 xy=project(heads,center);d=ImageDraw.Draw(im)
 for name in chain:
  i=names.index(name);p=int(parents[nn.index(name)]);parentName=nn[p] if p>=0 else None
  if parentName in names:
   j=names.index(parentName);a,b=xy[i],xy[j];d.line([tuple(a),tuple(b)],fill=(255,190,20),width=2)
  x,y=xy[i];d.ellipse((x-4,y-4,x+4,y+4),fill=(255,70,60));d.text((x+5,y-7),name.replace('DEF-',''),fill=(255,220,80))
 d.rectangle((0,0,W,48),fill=(15,15,15));d.text((8,8),title,fill='white');d.text((8,27),'Actual heads/parent lines. X-ray overlay, not visible joints.',fill='white');return im
actual=Image.open(C/'artist-contour-v3/build-v3/source-before-side.png').convert('RGB');overlay(actual,headspose,target,'Dance50 frozen C1 / true-bind FK heads').save(O/'bone-heads-posed-side.png')
# Rest diagnostic point raster, no invented camera-matched beauty render.
center=headsrest[names.index('DEF-upper_arm.R')].copy();center[2]-=.035;coords=project(rest,center);pix=np.rint(coords).astype(int);valid=(pix[:,0]>=0)&(pix[:,0]<W)&(pix[:,1]>=0)&(pix[:,1]<H);dd=rest@forward;zb=np.full(W*H,np.inf);pid=pix[valid,1]*W+pix[valid,0];np.minimum.at(zb,pid,dd[valid]);vis=np.zeros(N,bool);vis[valid]=dd[valid]<=zb[pid]+.0005
arr=np.zeros((H,W,3),np.uint8)+25;vv=np.flatnonzero(valid&vis);arr[pix[vv,1],pix[vv,0]]=(155,155,165);gar=vv[allowed[vv]];arr[pix[gar,1],pix[gar,0]]=(90,135,160);overlay(Image.fromarray(arr),headsrest,center,'True bind/rest shirt point raster + actual bone heads').save(O/'bone-heads-rest-side.png')
# Consecutive original-edge routes0→1→2→3, never a spatial weld.
paths=[]
for begin,end in zip(faces[:-1],faces[1:]):
 rd={int(v):0. for v in tri[begin]};par={};hq=[(0.,int(v)) for v in tri[begin]];heapq.heapify(hq);goal=set(map(int,tri[end]));last=None
 while hq:
  d,v=heapq.heappop(hq)
  if d!=rd[v]:continue
  if v in goal:last=v;break
  for q,l in adj[v]:
   nd=d+l
   if nd<rd.get(q,float('inf')):rd[q]=nd;par[q]=v;heapq.heappush(hq,(nd,q))
 assert last is not None;path=[last]
 while path[-1] in par:path.append(par[path[-1]])
 paths.append({'vertices':path,'lengthM':rd[last]})
routeids=np.unique(np.concatenate([x['vertices'] for x in paths]));cd={int(v):0. for v in routeids};hq=[(0.,int(v)) for v in routeids];heapq.heapify(hq)
while hq:
 d,v=heapq.heappop(hq)
 if d!=cd[v]:continue
 for q,l in adj[v]:
  nd=d+l
  if nd<=.008 and nd<cd.get(q,float('inf')):cd[q]=nd;heapq.heappush(hq,(nd,q))
corridor=np.unique(np.r_[patch,np.array(list(cd),int)]);cm=np.zeros(N,bool);cm[corridor]=True;cross=edges[cm[edges[:,0]]!=cm[edges[:,1]]];outer=np.unique(cross[cm[cross]]);assert np.all(allowed[corridor]) and not np.intersect1d(corridor,protected).size
fixedred=coreBands[0];fixedpurple=coreBands[3];assert not np.intersect1d(fixedred,fixedpurple).size
np.savez_compressed(O/'corridor-proposal.npz',originalVertexIndices=corridor,fixedOuterBoundary=outer,fixedTorsoCore=fixedred,fixedSleeveCore=fixedpurple,protectedOriginalVertexIndices=protected,originalEdgeRoute=routeids)
xy=project(points);pix=np.rint(xy).astype(int);valid=(pix[:,0]>=0)&(pix[:,0]<W)&(pix[:,1]>=0)&(pix[:,1]<H);depth=(points-cam)@forward;zb=np.full(W*H,np.inf);pid=pix[valid,1]*W+pix[valid,0];np.minimum.at(zb,pid,depth[valid]);vis=np.zeros(N,bool);vis[valid]=depth[valid]<=zb[pid]+.0008
im=Image.open(C/'artist-contour-v3/build-v3/source-before-side.png').convert('RGB');d=ImageDraw.Draw(im)
for ids,color in [(corridor,(245,170,30)),(outer,(40,230,255)),(routeids,(255,255,255)),(fixedred,(255,60,60)),(fixedpurple,(180,90,255))]:
 for v in ids[vis[ids]]:
  x,y=pix[v];d.point((int(x),int(y)),fill=color)
d.rectangle((0,0,W,64),fill=(15,15,15));d.text((8,8),'B corridor: actual original-edge route0>1>2>3',fill='white');d.text((8,26),'red/purple fixed; yellow/green free; cyan outer fixed',fill='white');d.text((8,44),'Orange support=8mm intrinsic route margin, review only',fill='white');im.save(O/'corridor-posed-side-proposal.png')
r={'status':'OFFLINE_CORRIDOR_BONE_REVIEW_NO_WEIGHTS_CHANGED','independentNDARestMaxComponentError':err,'independentNDARestHeadMaxDistanceM':headerr,'restMatrixScope':'NDA and older indexed sidecar orientations differ slightly; overlay uses original sidecar gauge consistently with frozen skin, not a full bind-parity claim.','NDAPath':str(nda),'boneHeads':{n:{'restWorldM':headsrest[names.index(n)].tolist(),'dance50WorldM':headspose[names.index(n)].tolist(),'parent':nn[int(parents[nn.index(n)])] if parents[nn.index(n)]>=0 else None} for n in chain},'paths':paths,'corridorVertices':len(corridor),'fixedOuterBoundary':len(outer),'fixedTorsoCore':len(fixedred),'fixedSleeveCore':len(fixedpurple),'protected':len(protected),'supportMarginM':.008,'limits':['Rest image is point-raster diagnostic, posed background actual existing source render.','Overlay is X-ray joint heads; parent lines are not bone tail/anatomical surface.','Family membership is proposed from mapped head positions/chain, no forearm weight declared wrong.']};(O/'corridor-bone-audit.json').write_text(json.dumps(r,indent=2)+'\n');print(r['status'],len(corridor),len(outer))

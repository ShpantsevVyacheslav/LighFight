import os
os.environ['OPENBLAS_NUM_THREADS']='6'
import numpy as np,json
from pathlib import Path
R=Path(__file__).resolve().parents[4];S=R/'art/drafts/sailor-native67-dense-v1/native67-surface-strain-v1';O=S.parent/'native67-local-delta-mush-v1';O.mkdir(exist_ok=False);N=994452
p=np.fromfile(S/'original-world-rest.bin','<f4').reshape(N,3).astype(float);mask=np.load(S.parent/'native67-collar-weight-v1/mask.npz')['mask'];tri=np.fromfile(R/'.tools/native67-bary-weight-packet-v1/triangles.bin','<i4').reshape(-1,3);edges=np.concatenate([tri[:,[0,1]],tri[:,[1,2]],tri[:,[2,0]]]);edges.sort(1);edges=np.unique(edges,axis=0);edges=edges[(mask[edges]>0).any(1)];a=np.r_[edges[:,0],edges[:,1]];b=np.r_[edges[:,1],edges[:,0]];degree=np.bincount(a,minlength=N);valid=degree>0;selected=np.flatnonzero(mask>0);mapping=np.full(N,-1);mapping[selected]=np.arange(len(selected));sel=mask[a]>0;aa=mapping[a[sel]];bb=b[sel];av=a[sel]
def smooth(q):
 q=q.copy()
 for i in range(5):
  avg=np.stack([np.bincount(a,weights=q[b,k],minlength=N) for k in range(3)],1);avg[valid]/=degree[valid,None];q[valid]=q[valid]*.5+avg[valid]*.5
 return q
ps=smooth(p);restDetail=p[selected]-ps[selected];restEdges=ps[bb]-ps[av]
def correct(q):
 qs=smooth(q);poseEdges=qs[bb]-qs[av];cov=np.zeros((len(selected),3,3))
 for i in range(3):
  for j in range(3):cov[:,i,j]=np.bincount(aa,weights=poseEdges[:,i]*restEdges[:,j],minlength=len(selected))
 u,s,vh=np.linalg.svd(cov);rot=u@vh;bad=np.linalg.det(rot)<0;u[bad,:,-1]*=-1;rot=u@vh;goal=qs[selected]+np.einsum('nij,nj->ni',rot,restDetail);delta=(goal-q[selected])*(.75*mask[selected,None]);length=np.linalg.norm(delta,axis=1);factor=np.ones(len(length));nz=length>0;factor[nz]=.008*np.tanh(length[nz]/.008)/length[nz];delta*=factor[:,None];out=q.copy();out[selected]+=delta;assert np.array_equal(out[mask==0],q[mask==0]);assert np.isfinite(out).all();return out,float(np.linalg.norm(delta,axis=1).max())
rest,test=correct(p);error=float(np.linalg.norm(rest-p,axis=1).max());assert error<1e-8
report={'status':'EXPERIMENT_PENDING_RENDER','method':'Local 5-step uniform Laplacian delta-mush with SVD proper-rotation detail transport, smooth collar mask, blend .75, tanh displacement cap8mm. Original topology unchanged. Rest identity and outside-mask identity checked. Not accepted rig or realtime implementation.','restIdentityMaxM':error,'selectedVertices':len(selected),'poses':[]}
for f in sorted(S.glob('action-*-world-positions.bin')):
 q=np.fromfile(f,'<f4').reshape(N,3).astype(float);out,shift=correct(q);out.astype('<f4').tofile(O/f.name);report['poses'].append({'name':f.stem,'maxDisplacementM':shift});print(f.name,shift,flush=True)
(O/'report.json').write_text(json.dumps(report,indent=2))

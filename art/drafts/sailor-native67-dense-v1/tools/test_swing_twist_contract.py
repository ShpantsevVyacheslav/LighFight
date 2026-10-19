import numpy as np,json
from pathlib import Path
rng=np.random.default_rng(20261019);n=10000;q=rng.normal(size=(n,4));q/=np.linalg.norm(q,axis=1)[:,None];q[q[:,3]<0]*=-1;axis=rng.normal(size=(n,3));axis/=np.linalg.norm(axis,axis=1)[:,None]
def mul(a,b):return np.c_[a[:,3,None]*b[:,:3]+b[:,3,None]*a[:,:3]+np.cross(a[:,:3],b[:,:3]),a[:,3]*b[:,3]-np.sum(a[:,:3]*b[:,:3],axis=1)]
def inv(a):return a*np.array([-1,-1,-1,1])
def apply(a,v):return mul(mul(a,np.c_[v,np.zeros(len(v))]),inv(a))[:,:3]
proj=(q[:,:3]*axis).sum(1);tw=np.c_[axis*proj[:,None],q[:,3]];norm=np.linalg.norm(tw,axis=1);assert norm.min()>1e-5;tw/=norm[:,None];swing=mul(q,inv(tw));angle=rng.uniform(-.5,.5,n);desired=np.c_[axis*np.sin(angle/2)[:,None],np.cos(angle/2)];new=mul(swing,desired);err=np.linalg.norm(apply(new,axis)-apply(q,axis),axis=1).max();assert err<1e-12;report={'randomSamples':n,'maxBoneDirectionError':float(err),'scope':'Algebraic swing invariance, not anatomical quality or continuous-clip acceptance.'};print(json.dumps(report));Path('docs/reports/native67-swing-twist-2026-10-19/algebra-contract.json').write_text(json.dumps(report,indent=2))

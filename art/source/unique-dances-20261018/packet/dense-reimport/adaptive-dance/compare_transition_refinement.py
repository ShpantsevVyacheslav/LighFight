"""Offline replay of captured matrices,10complete-original-mesh holdouts; no engine."""
import os
os.environ.setdefault('OPENBLAS_NUM_THREADS','1');os.environ.setdefault('OMP_NUM_THREADS','1')
import importlib.util,json,time,sys
from pathlib import Path
import numpy as np
here=Path(__file__).resolve().parent
spec=importlib.util.spec_from_file_location('bounds',here/'weighted_transition_bounds.py');b=importlib.util.module_from_spec(spec);spec.loader.exec_module(b)
repo=here.parents[5];source=repo/'art/drafts/newrig-sailor-v1/dense-def-fbx-packet-v1/original-indexed-attributes.npz'
a=np.load(source);p=a['positionsBlenderLocal'].astype(np.float64);j=a['jointIndices'];w=a['weights'].astype(np.float64)
summary_path=Path(sys.argv[1]) if len(sys.argv)>1 else Path('/tmp/lighfight-original-feature-leaf128.npz');s=np.load(summary_path);timings=[];holdouts=[];alllo=[];allhi=[]
for stage in ['entry','return']:
 c=np.load('/tmp/lighfight-transition-'+stage+'.npz')
 for t,m in zip(c['times'],c['skinMatrices']):
  start=time.process_time();lo,hi=b.evaluate(s,m);timings.append((time.process_time()-start)*1000);alllo.append(lo);allhi.append(hi)
  if not any(abs(t-x)<1e-12 for x in [0,.07,.14,.21,.28]):continue
  actual_lo=np.full(3,np.inf);actual_hi=np.full(3,-np.inf)
  for start in range(0,len(p),32768):
   stop=min(start+32768,len(p));pp=np.column_stack((p[start:stop],np.ones(stop-start)));out=np.zeros((stop-start,3))
   for k in range(4):out+=np.einsum('nij,nj->ni',m[j[start:stop,k],:3,:],pp)*w[start:stop,k,None]
   actual_lo=np.minimum(actual_lo,out.min(0));actual_hi=np.maximum(actual_hi,out.max(0))
  if np.any(actual_lo<lo) or np.any(actual_hi>hi):raise ValueError('Full source mesh containment RED')
  holdouts.append(dict(stage=stage,time=float(t),actualMin=actual_lo.tolist(),actualMax=actual_hi.tolist(),boxMin=lo.tolist(),boxMax=hi.tolist()))
low=np.min(alllo,axis=0)-.025;high=np.max(allhi,axis=0)+.025
result=dict(scope='FINITE_CAPTURE_REPLAY_NOT_NEW_UNITY_GPU',summarySHA256=b.sha(summary_path),poses=len(timings),fullSourceHoldouts=len(holdouts),verticesEach=len(p),leafCount=len(s['joints']),leafSize128=True,min=low.tolist(),max=high.tolist(),extents=((high-low)/2).tolist(),queryCpuMillisecondsMedian=float(np.median(timings)),queryCpuMillisecondsP95=float(np.percentile(timings,95)),holdouts=holdouts,newGpuAccepted=False)
Path(str(summary_path)+'.comparison.json').write_text(json.dumps(result,indent=2)+'\n');print(json.dumps({k:v for k,v in result.items() if k!='holdouts'},indent=2))

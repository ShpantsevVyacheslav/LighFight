"""Offline local active orientation constraint. Source and rejected candidates unchanged."""
import os
for k in ['OPENBLAS_NUM_THREADS','OMP_NUM_THREADS','MKL_NUM_THREADS']:os.environ[k]='1'
from pathlib import Path
import json,sys,heapq,time
import numpy as np
HERE=Path(__file__).resolve();helper=HERE.parent/'build_offline_candidates.py';ns={'__file__':str(helper),'__name__':'source_mixture'};prefix=helper.read_text().split('OUT=O/')[0];exec(compile(prefix,str(helper),'exec'),ns);globals().update({k:v for k,v in ns.items() if not k.startswith('__')});O=HERE.parents[1]
from mixture_kernel import orientation_witnesses,bounded_attenuation
OUT=O/'orientation-constrained-v2';OUT.mkdir(exist_ok=True);start=time.monotonic();localtriIDs=np.flatnonzero(np.any(local[tri]>=0,axis=1));lt=tri[localtriIDs];lti=local[lt];baseTri=points[lt];active=np.any(lti>=0,axis=1);assert active.all();lp=hp[patch];skinPoints=np.stack([np.einsum('nij,nj->ni',skin[ji[patch,k]],lp)[:,:3] for k in range(4)],axis=1)
# Local graph adjacency retains original edges; no spatial nearest-neighbor blend.
ladj=[[] for _ in patch]
for (u,v),lengthM in zip(el,length):ladj[int(u)].append((int(v),float(lengthM)));ladj[int(v)].append((int(u),float(lengthM)))
def evalfield(field):
 candidate64=mix(original,armSlots,fit,field,fixed);stored=candidate64.astype(np.float32);pp=np.einsum('nkj,nk->nj',skinPoints,stored);coords=baseTri.copy()
 for corner in range(3):
  choose=lti[:,corner]>=0;coords[choose,corner]=pp[lti[choose,corner]]
 return stored,pp,coords,orientation_witnesses(baseTri,coords),float(np.max(abs(stored-candidate64)))
def witness(r,ids):
 return [{'originalTriangleID':int(localtriIDs[i]),'vertexIDs':lt[i].tolist(),'baselineNormal':r['beforeNormal'][i].tolist(),'candidateNormal':r['afterNormal'][i].tolist(),'normalDot':float(r['dot'][i]),'cosine':float(r['dot'][i]/max(4*r['beforeArea'][i]*r['afterArea'][i],1e-300)),'beforeAreaMm2':float(r['beforeArea'][i]*1e6),'afterAreaMm2':float(r['afterArea'][i]*1e6)} for i in ids]
field=np.where(fixed,0.,.5);history=[]
for iteration in range(33):
 stored,posed,coords,r,quant=evalfield(field);bad=np.unique(np.r_[r['opposed'],r['newCollapsed']]).astype(int);row={'iteration':iteration,'opposed':len(r['opposed']),'newCollapsed':len(r['newCollapsed']),'fieldMin':float(field[~fixed].min()),'fieldMedian':float(np.median(field[~fixed])),'fieldMax':float(field.max()),'witnesses':witness(r,bad)};history.append(row)
 if not len(bad):break
 if iteration==32:raise RuntimeError('Active constraint failed; candidates remain unaccepted')
 seeds=np.unique(lti[bad]);seeds=seeds[seeds>=0];distance=np.full(len(patch),np.inf);distance[seeds]=0;queue=[(0.,int(v)) for v in seeds];heapq.heapify(queue)
 while queue:
  d,v=heapq.heappop(queue)
  if d!=distance[v]:continue
  for q,l in ladj[v]:
   nd=d+l
   if nd<=.006 and nd<distance[q]:distance[q]=nd;heapq.heappush(queue,(nd,q))
 nextfield=bounded_attenuation(field,distance,.006,fixed);assert np.all(nextfield>=0) and np.all(nextfield<=field) and np.array_equal(nextfield[fixed],field[fixed]);field=nextfield
assert np.array_equal(stored[fixed],w[patch][fixed]) and np.array_equal(stored>0,original>0) and not np.intersect1d(patch[np.any(stored!=w[patch],axis=1)],protected).size
np.savez_compressed(OUT/'lambda50-active-weights.npz',originalVertexIndices=patch,jointIndices=ji[patch],originalWeights=w[patch],candidateWeights=stored,lambdaField=field,fixedOriginalVertexIndices=patch[fixed])
report={'status':'OFFLINE_ORIENTATION_CONSTRAINED_NO_ENGINE_ACCEPTANCE','sourceVertices':N,'sourceTriangles':len(tri),'rejectedCandidatesPreserved':'../offline-candidates-v1/','fieldModel':'Per-vertex local backtracking; WendlandC2 six-mm intrinsic support; half attenuation at offending triangle/one-ring neighborhood.','orientationCriterion':'dot(original posed normal,new normal)>=0; no new area<1e-18m². Opposed-baseline-normal is NOT a self-intersection proof.','allIncidentTriangles':len(lt),'history':history,'backtrackingSteps':iteration,'protectedWeightChanges':0,'fixedWeightChanges':0,'max4ProjectionResidual':0,'positiveOriginalSupportRetained':True,'float32StorageMaxComponentError':quant,'sumMaxDeviation':float(abs(stored.sum(1)-1).max()),'freeVertexLambdaQuantiles':dict(zip(['min','p25','median','p75','max'],map(float,np.quantile(field[~fixed],[0,.25,.5,.75,1])))),'freeVerticesStillExactlyHalf':int(np.sum(field[~fixed]==.5)),'maxPredictedDance50DeltaM':float(np.linalg.norm(posed-points[patch],axis=1).max()),'elapsedSeconds':time.monotonic()-start,'limits':['Only frozen dance50 source checked; all other poses and actual Blender readback/visual review pending.','No strict strain/contour/skin-intersection acceptance inferred from orientation feasibility.','Field changes weights only within reviewed cloth graph; no geometry/UV/maps/bind/source file mutation.']};(OUT/'report.json').write_text(json.dumps(report,indent=2)+'\n');print(json.dumps({k:report[k] for k in ['status','backtrackingSteps','freeVertexLambdaQuantiles','freeVerticesStillExactlyHalf','elapsedSeconds']}))

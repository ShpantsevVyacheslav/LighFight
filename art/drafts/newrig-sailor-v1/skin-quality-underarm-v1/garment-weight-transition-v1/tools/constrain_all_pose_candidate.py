"""Offline simultaneous7pose active constraint, original archive never modified."""
import os
for k in ['OPENBLAS_NUM_THREADS','OMP_NUM_THREADS','MKL_NUM_THREADS']:os.environ[k]='1'
from pathlib import Path
import numpy as np,json,heapq,time
HERE=Path(__file__).resolve();helper=HERE.parent/'build_offline_candidates.py';ns={'__file__':str(helper),'__name__':'source_mixture'};exec(compile(helper.read_text().split('OUT=O/')[0],str(helper),'exec'),ns);globals().update({k:v for k,v in ns.items() if not k.startswith('__')});O=HERE.parents[1]
from mixture_kernel import orientation_witnesses,bounded_attenuation
OUT=O/'orientation-constrained-seven-v3';OUT.mkdir(exist_ok=True);began=time.monotonic();actual=O/'actual-constrained-v2';ar=json.loads((actual/'report.json').read_text());assert len(ar['cases'])==7
triangleIDs=np.flatnonzero(np.any(local[tri]>=0,axis=1));lt=tri[triangleIDs];vids=np.unique(lt);vmap=np.full(N,-1,int);vmap[vids]=np.arange(len(vids));tvi=vmap[lt];patchInAll=vmap[patch];graph=[[] for _ in patch]
for (u,v),d in zip(el,length):graph[int(u)].append((int(v),float(d)));graph[int(v)].append((int(u),float(d)))
poses=[]
for row in ar['cases']:
 name=row['case']['name'];f=np.load(actual/('source-worldskin-'+name+'.npz'));assert f['boneNames'].tolist()==names;sk=f['worldSkin'];vectors=np.stack([np.einsum('nij,nj->ni',sk[ji[vids,k]],hp[vids])[:,:3] for k in range(4)],axis=1);base=np.einsum('nkj,nk->nj',vectors,w[vids]);poses.append({'name':name,'vectors':vectors,'base':base,'baseTriangles':base[tvi]})
field=np.where(fixed,0.,.5);history=[];last=None
for iteration in range(41):
 double=mix(original,armSlots,fit,field,fixed);stored=double.astype(np.float32);allbad=set();step={'iteration':iteration,'poses':[]};minimumCosine=1.
 for pose in poses:
  out=pose['base'].copy();out[patchInAll]=np.einsum('nkj,nk->nj',pose['vectors'][patchInAll],stored);rr=orientation_witnesses(pose['baseTriangles'],out[tvi]);den=4*rr['beforeArea']*rr['afterArea'];cos=np.divide(rr['dot'],den,out=np.ones_like(den),where=den>0);eligible=(rr['beforeArea']>=1e-18);bad=np.flatnonzero(((cos<.05)&eligible)|((rr['afterArea']<1e-18)&eligible));minimumCosine=min(minimumCosine,float(cos[eligible].min(initial=1)));allbad.update(map(int,bad));step['poses'].append({'name':pose['name'],'opposedCount':len(rr['opposed']),'newCollapsedCount':len(rr['newCollapsed']),'internalMarginFailCount':len(bad),'minimumCosine':float(cos[eligible].min(initial=1)),'badOriginalTriangleIDs':triangleIDs[bad].tolist()})
 history.append(step)
 if not allbad:last=(stored,double,minimumCosine);break
 if iteration==40:raise RuntimeError('Composite active constraint did not converge; no candidate acceptance')
 bad=np.array(sorted(allbad),int);seed=np.unique(local[lt[bad]]);seed=seed[seed>=0];dist=np.full(len(patch),np.inf);dist[seed]=0;queue=[(0.,int(v)) for v in seed];heapq.heapify(queue)
 while queue:
  d,v=heapq.heappop(queue)
  if d!=dist[v]:continue
  for q,lengthM in graph[v]:
   nd=d+lengthM
   if nd<=.006 and nd<dist[q]:dist[q]=nd;heapq.heappush(queue,(nd,q))
 field=bounded_attenuation(field,dist,.006,fixed)
stored,double,mincos=last;assert np.array_equal(stored[fixed],w[patch][fixed]) and np.array_equal(stored>0,w[patch]>0);changed=patch[np.any(stored!=w[patch],axis=1)];assert not np.intersect1d(changed,protected).size
np.savez_compressed(OUT/'lambda50-seven-pose-weights.npz',originalVertexIndices=patch,jointIndices=ji[patch],originalWeights=w[patch],candidateWeights=stored,lambdaField=field,fixedOriginalVertexIndices=patch[fixed])
r={'status':'OFFLINE_SEVEN_POSE_CONSTRAINED_PENDING_BLENDER','internalMarginCosine':.05,'globalOrientationGateUnchanged':'dot>=0 and no new area<1e-18m²','internalMarginExplanation':'Stronger offline fit margin anticipates float32 armature evaluation; not a relaxed global criterion.','history':history,'allIncidentTriangles':len(lt),'sourceFrozenPoseCount':len(poses),'backtrackingSteps':iteration,'freeVertexLambdaQuantiles':dict(zip(['min','p25','median','p75','max'],map(float,np.quantile(field[~fixed],[0,.25,.5,.75,1])))),'freeVerticesStillExactlyHalf':int(np.sum(field[~fixed]==.5)),'changedVertices':len(changed),'protectedWeightChanges':0,'fixedWeightChanges':0,'projectionResidual':0,'float32StorageMaxComponentError':float(abs(stored-double).max()),'sumMaxDeviation':float(abs(stored.sum(1)-1).max()),'minimumCosineAllSeven':mincos,'elapsedSeconds':time.monotonic()-began,'limits':['Only7frozen source samples, not a full animation surface/temporal proof.','Actual Blender readback/pairedbeauty and full skin quality still pending.','Original triangle/rest/UV/maps untouched; derived weights deliberately differ.']};(OUT/'report.json').write_text(json.dumps(r,indent=2)+'\n');print(json.dumps({k:r[k] for k in ['status','backtrackingSteps','freeVertexLambdaQuantiles','freeVerticesStillExactlyHalf','elapsedSeconds']}))

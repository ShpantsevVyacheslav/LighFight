"""Offline exact original-neighbor audit, no Blender or source mutation."""
from pathlib import Path
from fractions import Fraction
import hashlib,json,numpy as np
O=Path(__file__).resolve().parents[1];BASE=O.parent;P=BASE.parent/'dense-def-fbx-packet-v1';OUT=O/'weight-witness-v1';OUT.mkdir(exist_ok=True)
a=np.load(P/'original-indexed-attributes.npz');raw=a['positionsBlenderLocal'].astype(float);tri=a['triangles'];j=a['jointIndices'];w=a['weights'].astype(float);mw=a['meshWorldBlender'];rest=raw@mw[:3,:3].T+mw[:3,3];names=json.loads((P/'manifest.json').read_text())['boneNames'];regions=np.load(BASE/'regions-v1/region-proposals.npz');report=json.loads((O/'candidate-v1/report.json').read_text());sourceCache=P/'adaptive-dance_01-FULL_ADAPTIVE_TIGHT008_EXACT_BATCH-cache.npz';cache=np.load(sourceCache);t=Fraction(139,12);row=next(i for i,x in enumerate(cache['times']) if Fraction(int(x[0]),int(x[1]))==t);order=[list(cache['boneNames']).index(n) for n in names];allSkin=cache['worldSkin'];skin=allSkin[row,order].copy();del allSkin

def lbs(ids):
 points=np.column_stack((raw[ids],np.ones(len(ids))));out=np.zeros((len(ids),4))
 for k in range(4):out+=np.einsum('nij,nj->ni',skin[j[ids,k]],points)*w[ids,k,None]
 return out[:,:3]
targetArtifact=np.load(O/'candidate-v1/target-deltas.npz');candidateDelta=np.zeros_like(raw);candidateDelta[targetArtifact['originalVertexIndices']]=targetArtifact['deltaPosedWorld']
def l1(edges):
 i,z=edges.T;shared=np.zeros(len(edges))
 for k in range(4):
  for v in range(4):shared+=np.minimum(w[i,k],w[z,v])*(j[i,k]==j[z,v])
 return np.maximum(w[i].sum(1)+w[z].sum(1)-2*shared,0)
def stats(v):return dict(zip(['min','median','p95','p99','max'],map(float,np.quantile(v,[0,.5,.95,.99,1]))))
def named(v):return [{'bone':names[int(b)],'weight':float(q)} for b,q in zip(j[v],w[v]) if q>0]
def witness(ids,ratio=None,edgePopulation=None,popL1=None):
 ids=np.array(ids,int);rw=rest[ids];pw=lbs(ids);rl=float(np.linalg.norm(rw[1]-rw[0]));pl=float(np.linalg.norm(pw[1]-pw[0]));jump=float(l1(ids[None])[0]);near=tri[np.any(np.isin(tri,ids),axis=1)];record={'originalVertexIDs':ids.tolist(),'restLengthM':rl,'posedLengthM':pl,'poseToRestRatio':pl/rl,'weightL1Jump':jump,'weightL1PerRestMM':jump/(rl*1000),'restWorldM':rw.tolist(),'posedWorldM':pw.tolist(),'sourceNamedWeights':[named(v) for v in ids],'adjacentOriginalTriangles':near.tolist(),'adjacentOriginalTriangleIDs':np.flatnonzero(np.any(np.isin(tri,ids),axis=1)).tolist()}
 wi=np.zeros(len(names));wj=np.zeros(len(names))
 for bone,value in zip(j[ids[0]],w[ids[0]]):wi[int(bone)]+=value
 for bone,value in zip(j[ids[1]],w[ids[1]]):wj[int(bone)]+=value
 dx=raw[ids[1]]-raw[ids[0]];middle=np.r_[(raw[ids[0]]+raw[ids[1]])*.5,1.];average=(wi+wj)*.5;difference=wj-wi
 sameWeights=np.einsum('b,bij,j->i',average,skin[:,:3,:3],dx);gradient=np.einsum('b,bij,j->i',difference,skin[:,:3,:],middle);actual=pw[1]-pw[0]
 record['exactEdgeDecomposition']={'constantMeanWeightsVectorM':sameWeights.tolist(),'weightGradientVectorM':gradient.tolist(),'constantMeanWeightsLengthM':float(np.linalg.norm(sameWeights)),'weightGradientLengthM':float(np.linalg.norm(gradient)),'residualM':float(np.linalg.norm(actual-sameWeights-gradient)),'interpretation':'Exact algebraic decomposition of original LBS edge; does not establish whether a weight is anatomically wrong.'}
 record['positiveEdgeExcessMM']=max(pl-rl,0)*1000;record['absoluteLengthChangeMM']=abs(pl-rl)*1000;newEdge=(pw[1]+candidateDelta[ids[1]])-(pw[0]+candidateDelta[ids[0]]);record['candidateDeltaPosedEdgeMM']=(float(np.linalg.norm(newEdge))-pl)*1000
 if ratio is not None:assert abs(pl/rl-ratio)<1e-5,('Frozen source mismatch',pl/rl,ratio);record['baselineMetricRatio']=ratio
 if popL1 is not None:record['weightJumpPercentileInRegion']=float(np.mean(popL1<=jump)*100)
 return record
def sha(path):
 h=hashlib.sha256()
 with Path(path).open('rb') as f:
  for block in iter(lambda:f.read(4*1024*1024),b''):h.update(block)
 return h.hexdigest()
result={'auditScriptSHA256':sha(Path(__file__)),'sourceIndexedSHA256':sha(P/'original-indexed-attributes.npz'),'sourceCacheSHA256':sha(sourceCache),'status':'OFFLINE_MEASURED_NOT_WEIGHT_LEAK_DIAGNOSIS','sourceSeconds':float(t),'sourceRational':[t.numerator,t.denominator],'sourceCache':str(sourceCache),'sourcePose':'Frozen original fullDance C1 sample, not exported/interpolated take','regions':{},'worstEdges':[]}
for region in ['sleeve_armhole_R','torso_underarm_R','medial_trapezius_R']:
 mask=np.zeros(len(raw),bool);mask[regions[region]]=True;faces=tri[np.any(mask[tri],axis=1)];edges=np.unique(np.sort(np.concatenate([faces[:,[0,1]],faces[:,[1,2]],faces[:,[2,0]]]),axis=1),axis=0);edges=edges[np.any(mask[edges],axis=1)];lengths=np.linalg.norm(rest[edges[:,1]]-rest[edges[:,0]],axis=1);jump=l1(edges);result['regions'][region]={'originalNeighborEdges':len(edges),'restEdgeLengthMM':stats(lengths*1000),'weightL1Jump':stats(jump),'weightL1PerRestMM':stats(jump/np.maximum(lengths*1000,1e-12)),'interpretation':'Spatial original-mesh neighbors in overlapping envelope; not proven garment armhole seam'}
 posedA=lbs(edges[:,0]);posedB=lbs(edges[:,1]);posedLengths=np.linalg.norm(posedB-posedA,axis=1);candidateLengths=np.linalg.norm(posedB+candidateDelta[edges[:,1]]-posedA-candidateDelta[edges[:,0]],axis=1);result['regions'][region]['absoluteLengthDistributions']={}
 for threshold in [.1,1.]:
  use=lengths*1000>threshold;assert use.any();result['regions'][region]['absoluteLengthDistributions']['restGreaterThan'+str(threshold)+'MM']={'edgeCount':int(use.sum()),'signedPosedMinusRestMM':stats((posedLengths[use]-lengths[use])*1000),'positiveEdgeExcessMM':stats(np.maximum(posedLengths[use]-lengths[use],0)*1000),'absoluteLengthChangeMM':stats(np.abs(posedLengths[use]-lengths[use])*1000),'candidateDeltaPosedEdgeMM':stats((candidateLengths[use]-posedLengths[use])*1000),'scope':'Diagnostic thresholded distribution only; full-region strict fence remains unchanged RED.'}
 if region=='sleeve_armhole_R':
  e=report['metrics'][region]['before']['edge']
  for ids,ratio in zip(e['worstEdgesVertexPairs'],e['worstRatios']):result['worstEdges'].append(witness(ids,ratio,edges,jump))
  topExcess=np.argsort(posedLengths-lengths)[-8:][::-1];result['largestAbsoluteEdgeExcess']=[witness(edges[i],popL1=jump) for i in topExcess]
  top=np.argsort(jump)[-8:][::-1];result['largestNeighborWeightJumps']=[witness(edges[i],popL1=jump) for i in top]
# Original distribution separated by names, not an anatomical leakage assertion.
for region in result['regions']:
 ids=regions[region];means={n:float(np.sum(w[ids]*(j[ids]==k))/len(ids)) for k,n in enumerate(names)};result['regions'][region]['meanNamedWeight']={n:v for n,v in means.items() if v>1e-6}
(OUT/'audit.json').write_text(json.dumps(result,indent=2)+'\n');np.savez_compressed(OUT/'frozen-dance50-world-skin.npz',worldSkin=skin,boneNames=np.array(names),sourceSeconds=np.array(float(t)))
print(json.dumps({'status':result['status'],'firstWorst':result['worstEdges'][0],'regionStats':result['regions']['sleeve_armhole_R']},indent=2))

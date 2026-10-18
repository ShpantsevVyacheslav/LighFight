"""Independent subframe/entryreturn/skin proof usingactualGLB matrices, no mocks."""
from pathlib import Path
import json,struct,hashlib
import numpy as np,math
ROOT=Path(__file__).resolve().parents[4];O=ROOT/'art/drafts/newrig-sailor-v1/native67-trapezius-v1'
s=(ROOT/'art/drafts/newrig-sailor-v1/tools/20_native67_clavicle_corrective.py').read_text();exec(compile(s[:s.index('tracks={')],str(ROOT/'art/drafts/newrig-sailor-v1/tools/20_native67_clavicle_corrective.py'),'exec'))
# Keeporiginal channels as immutablearrays; loadcandidate accessor table independently.
source=g;originalNodes=nodes;f=O/'sailor-native67-trapezius-candidate.glb';b=f.read_bytes();n=struct.unpack_from('<I',b,12)[0];g=json.loads(b[20:20+n]);off=20+n;size=struct.unpack_from('<I',b,off)[0];data=bytearray(b[off+8:off+8+size]);candidateChannels=channels(next(a for a in g['animations'] if a['name']=='Dance_TrapeziusSupport_v1'))
prim=g['meshes'][0]['primitives'][0];positions=access(prim['attributes']['POSITION']);joints=access(prim['attributes']['JOINTS_0']).astype(int);weights=access(prim['attributes']['WEIGHTS_0']);skin=g['skins'][0];ibm=access(skin['inverseBindMatrices']).reshape(-1,4,4).transpose(0,2,1);bind,_=world(defaults());meshNode=next(i for i,n in enumerate(nodes) if n.get('mesh')==0);homo=np.c_[positions,np.ones(len(positions))]
def skinpoints(wm,ids):
 mats=np.stack([wm[j]@ibm[k] for k,j in enumerate(skin['joints'])]);return np.einsum('nv,nvij,nj->ni',weights[ids],mats[joints[ids]],homo[ids])[:,:3]
ids=np.arange(len(positions));restWorld=homo@bind[meshNode].T;restError=np.linalg.norm(skinpoints(bind,ids)-restWorld[:,:3],axis=1);assert restError.max()<1e-5,restError.max()
headNodes={i for i,n in enumerate(nodes) if n.get('name') in ['Head','Left_Eye','Right_Eye']};headJointIds={k for k,n in enumerate(skin['joints']) if n in headNodes};headWeight=np.sum(weights*np.isin(joints,list(headJointIds)),axis=1);headIds=np.flatnonzero(headWeight>.8)
footNodes={i for i,n in enumerate(nodes) if any(k in n.get('name','') for k in ['Foot','Toes'])};footJointIds={k for k,n in enumerate(skin['joints']) if n in footNodes};footIds=np.flatnonzero(np.any(np.isin(joints,list(footJointIds))&(weights>0),axis=1))
manifest=json.loads((O/'manifest.json').read_text());checkHz=2*manifest['correctionSamplingHz'];rotationError=[];matrixAxisError=[];handMove=[];endpointSurface=[];sampleRows=[]
# Exact ancestor closure for dense subframe checks: fingers/feet cannot affect shoulder/hand FK.
# Full surface checks below still evaluate all channels and every original mesh vertex.
relevant={neck,chest}
for side,S,A,E,H,ra,rc in sides:relevant.update([S,A,E,H])
for i in list(relevant):
 while i in parent:i=parent[i];relevant.add(i)
sourceRelevant=[c for c in sourceChannels if c[0] in relevant];candidateRelevant=[c for c in candidateChannels if c[0] in relevant]
def relevantSample(ch,t):return {i:v for i,v in sample(ch,t).items() if i in relevant}
for t in np.arange(round(duration*checkHz)+1)/checkHz:
 if t>duration+1e-4:continue
 wm,wq=world(relevantSample(sourceRelevant,t));cm,cq=world(relevantSample(candidateRelevant,t));rotationError.append(max(math.degrees(2*math.acos(np.clip(abs(np.dot(wq[A],cq[A])),0,1))) for side,S,A,E,H,ra,rc in sides));matrixAxisError.append(max(math.degrees(math.acos(np.clip(np.dot(wm[A][:3,k],cm[A][:3,k])/(np.linalg.norm(wm[A][:3,k])*np.linalg.norm(cm[A][:3,k])),-1,1))) for side,S,A,E,H,ra,rc in sides for k in range(3)));handMove.append(max(np.linalg.norm(cm[H][:3,3]-wm[H][:3,3]) for side,S,A,E,H,ra,rc in sides))
for phase in [0,.25,.5,.75,1]:
 t=phase*duration;wm,wq=world(sample(sourceChannels,t));cm,cq=world(sample(candidateChannels,t));old=skinpoints(wm,ids);new=skinpoints(cm,ids);delta=np.linalg.norm(new-old,axis=1);sampleRows.append({'phase':phase,'maxSurfaceMovementM':float(delta.max()),'headSurfaceMaxM':float(delta[headIds].max()),'fullFootSurfaceMaxM':float(delta[footIds].max()),'originalFullMeshFloorY':float(old[:,1].min()),'correctedFullMeshFloorY':float(new[:,1].min())})
 if phase in [0,1]:endpointSurface.append(float(delta.max()))
manifest=json.loads((O/'manifest.json').read_text());result={'status':'PASS' if max(rotationError)<.025 and max(endpointSurface)<1e-6 else 'FAIL','actualSubframeSamples':len(rotationError),'exactAncestorClosureNodes':len(relevant),'independentSamplingHz':checkHz,'rotationMaxErrorDeg':float(max(rotationError)),'actualWorldMatrixAxisMaxErrorDeg':float(max(matrixAxisError)),'handMovementMedianM':float(np.median(handMove)),'handMovementP95M':float(np.quantile(handMove,.95)),'handMovementMaxM':float(max(handMove)),'sourceRestHeightM':float(np.ptp(restWorld[:,1])),'handMovementMaxPercentRestHeight':float(max(handMove)/np.ptp(restWorld[:,1])*100),'bindRestSurfaceMaxErrorM':float(restError.max()),'entryReturnSurfaceMaxM':max(endpointSurface),'headVertices':len(headIds),'fullFootVertices':len(footIds),'poseSurfaceRows':sampleRows,'originalClipPayloadsBitExact':g['animations'][:-1]==source['animations'] and bytes(data[:oldLength])==originalBin[:oldLength],'originalSourceFileStillExact':hashlib.sha256(SOURCE.read_bytes()).hexdigest()==hashlib.sha256(raw).hexdigest(),'limits':'GLB numerical proof, not Unity/iPhone/visual acceptance. Derivedanimationintentionallymovesarmsviajointgirdle.'};assert result['originalClipPayloadsBitExact'] and result['originalSourceFileStillExact'];(O/'numerical-gate.json').write_text(json.dumps(result,indent=2));print(json.dumps(result,indent=2));assert result['status']=='PASS',result

"""All3311 frozen source samples +498 independent source poses; no exports/source edits."""
from pathlib import Path
import json,time,hashlib
import numpy as np
O=Path(__file__).resolve().parents[1];script=O/'tools/44_profile_fk_finger_batches.py';text=script.read_text();exec(compile(text[:text.index('cases=[')],str(script),'exec'))
P=O/'dense-def-fbx-packet-v1';cache=np.load(P/'adaptive-box_02-FULL_ADAPTIVE_TIGHT008-cache.npz');cfg=json.loads(str(cache['sourceConfig']));assert cfg['attributes']==original
for name in ['05_full_motion_gallery.py','27_actual_arm_elevation.py']:
 assert hashlib.sha256((O/'tools'/name).read_bytes()).hexdigest()==cfg['code'][name]
assert hashlib.sha256(source.read_bytes()).hexdigest()==cfg['sourceFBX'];assert hashlib.sha256((O/'stage02-bound-original-dense.blend').read_bytes()).hexdigest()==cfg['sourceBlend']
cacheNames=list(cache['boneNames']);order=[cacheNames.index(n) for n in names];times=cache['times'];worlds=cache['worldPose'][:,order];skins=cache['worldSkin'][:,order];oracle=np.load(P/'offkey-original-source-reference.npz');ot=oracle['timesClipIndexSeconds'];ow=oracle['boneWorldBlenderMetres'];rows=[];start=time.monotonic();worst=None
report={'status':'RUNNING','scope':'ALL3311frozenboxsourceposes+498independentoriginalposes','rows':rows,'sourceCacheSHA256':hashlib.sha256((P/'adaptive-box_02-FULL_ADAPTIVE_TIGHT008-cache.npz').read_bytes()).hexdigest(),'sourceCallbackSHA256':hashlib.sha256(script.read_bytes()).hexdigest()}
def visit(label,clip,seconds,truthW,truthS):
 global worst
 duration=(next(a for a in clips if clip in a.name).frame_range[1]-next(a for a in clips if clip in a.name).frame_range[0])/24
 elapsed=run(retarget_batched,clip,seconds/duration);W,S=snapshot();bound=bounds.error_bound(truthS,S);angle=bone_angle_deg(truthW,W);element=float(np.abs(truthW-W).max());row={'source':label,'clip':clip,'seconds':seconds,'surfaceBoundM':bound,'boneMaxDeg':angle,'worldMatrixMaxElementDifference':element,'candidateSeconds':elapsed};rows.append(row)
 if worst is None or bound>worst[0]:worst=(bound,S-truthS,clip,seconds)
 if len(rows)%120==0:
  print('PROGRESS '+str(len(rows))+' maxBoundM='+str(max(r['surfaceBoundM'] for r in rows))+' wall='+str(time.monotonic()-start),flush=True);(P/'finger-batch-frozen-cache-gate.json').write_text(json.dumps(report,indent=2))
 assert bound<=1e-6 and angle<=.001 and element<=1e-5, 'BATCHING_NOT_EQUIVALENT '+json.dumps(row)
for i,t in enumerate(times):visit('frozen3311','box_02',int(t[0])/int(t[1]),worlds[i],skins[i])
# Original498 oracle order is manifest boneNames, identical to this snapshot order.
B=np.array([bind[n] for n in names])
for (clipIndex,seconds),W in zip(ot,ow):visit('independent498',['dance_01','box_02','front_kick_01'][int(clipIndex)],float(seconds),W,np.einsum('nij,njk->nik',W,B))
actual=0.;D=worst[1]
for begin in range(0,N,65536):
 end=min(N,begin+65536);points=np.concatenate([raw[begin:end],np.ones((end-begin,1))],1);delta=np.zeros((end-begin,4))
 for k in range(4):delta+=np.einsum('nij,nj->ni',D[joints[begin:end,k]],points)*weights[begin:end,k,None]
 actual=max(actual,float(np.linalg.norm(delta[:,:3],axis=1).max()))
assert actual<=worst[0]+1e-9;assert hash_arrays(body)==original
report.update(status='PASS_ALL_FROZEN_SOURCE_POSES',samples=len(rows),seconds=time.monotonic()-start,maxSurfaceBoundM=max(r['surfaceBoundM'] for r in rows),maxBoneDeg=max(r['boneMaxDeg'] for r in rows),maxWorldMatrixElement=max(r['worldMatrixMaxElementDifference'] for r in rows),actualAll994452VertexWorstM=actual,sourceAttributesExact=True,limits='Finite frozen source equivalence; fullDance temporal fitting and reimport still separate.')
(P/'finger-batch-frozen-cache-gate.json').write_text(json.dumps(report,indent=2));print('RESULT '+json.dumps({k:v for k,v in report.items() if k!='rows'}),flush=True)

"""Second exact candidate: batch only independent finger depths in C1 orientation replay."""
from pathlib import Path
import json,time,hashlib
import numpy as np
O=Path(__file__).resolve().parents[1];script=O/'tools/44_profile_fk_finger_batches.py';text=script.read_text();exec(compile(text[:text.index('cases=[')],str(script),'exec'))
sourceSupport=(O/'tools/27_actual_arm_elevation.py').read_text();sourceSupport=sourceSupport[sourceSupport.index('def support('):sourceSupport.index('# Corrected actual elevation fixture')];old=''' for t,d in mapping:
  if t in keep:
   pb=rig.pose.bones[t];q=rig.matrix_world.to_quaternion().inverted()@keep[t];pb.matrix=Matrix.Translation(pb.matrix.translation)@q.to_matrix().to_4x4();bpy.context.view_layer.update()
''';new=''' batches=[[(t,d)] for t,d in mapping if t in keep and not is_finger(t)]+[[(t,d) for t,d in mapping if t in keep and is_finger(t) and t.split('.')[1]==segment] for segment in ['01','02','03']]
 for batch in batches:
  for t,d in batch:
   pb=rig.pose.bones[t];q=rig.matrix_world.to_quaternion().inverted()@keep[t];pb.matrix=Matrix.Translation(pb.matrix.translation)@q.to_matrix().to_4x4()
  bpy.context.view_layer.update()
''';assert old in sourceSupport;batchedSupportCode=sourceSupport.replace('def support(', 'def support_batched(').replace(old,new);exec(compile(batchedSupportCode,'C1-support-independent-fingers','exec'));baselineSupport=support
# Compare original evaluator and fully optimized candidate on all498 frozen original samples.
P=O/'dense-def-fbx-packet-v1';oracle=np.load(P/'offkey-original-source-reference.npz');times=oracle['timesClipIndexSeconds'];worlds=oracle['boneWorldBlenderMetres'];B=np.array([bind[n] for n in names]);rows=[];started=time.monotonic();worst=None
for i,((clipIndex,seconds),truthW) in enumerate(zip(times,worlds)):
 clip=['dance_01','box_02','front_kick_01'][int(clipIndex)];duration=(next(a for a in clips if clip in a.name).frame_range[1]-next(a for a in clips if clip in a.name).frame_range[0])/24;phase=float(seconds)/duration;support=support_batched;elapsed=run(retarget_batched,clip,phase);W,S=snapshot();truthS=np.einsum('nij,njk->nik',truthW,B);bound=bounds.error_bound(truthS,S);angle=bone_angle_deg(truthW,W);element=float(np.abs(W-truthW).max());row={'clip':clip,'seconds':float(seconds),'phase':phase,'candidateSeconds':elapsed,'surfaceBoundM':bound,'boneMaxDeg':angle,'matrixMaxElement':element};rows.append(row)
 if worst is None or bound>worst[0]:worst=(bound,S-truthS)
 if i%48==0:print('PROGRESS '+str(i)+' '+json.dumps(row),flush=True)
 assert bound<=1e-6 and angle<=.001 and element<=1e-5,'SUPPORT_BATCH_NOT_EQUIVALENT '+json.dumps(row)
actual=0.;D=worst[1]
for begin in range(0,N,65536):
 end=min(N,begin+65536);points=np.concatenate([raw[begin:end],np.ones((end-begin,1))],1);delta=np.zeros((end-begin,4))
 for k in range(4):delta+=np.einsum('nij,nj->ni',D[joints[begin:end,k]],points)*weights[begin:end,k,None]
 actual=max(actual,float(np.linalg.norm(delta[:,:3],axis=1).max()))
assert actual<=worst[0]+1e-9
assert hash_arrays(body)==original
r={'status':'PASS_ALL498_FROZEN_ORIGINAL_POSES','samples':len(rows),'seconds':time.monotonic()-started,'maxSurfaceBoundM':max(r['surfaceBoundM'] for r in rows),'maxBoneDeg':max(r['boneMaxDeg'] for r in rows),'maxWorldMatrixElement':max(r['matrixMaxElement'] for r in rows),'sourceAttributesExact':True,'actualAll994452VertexWorstM':actual,'supportReplayUpdatesBaseline':36,'supportReplayUpdatesCandidate':9,'sourceCallbackSHA256':hashlib.sha256(script.read_bytes()).hexdigest(),'supportScriptSHA256':hashlib.sha256(Path(__file__).read_bytes()).hexdigest(),'rows':rows};(P/'support-finger-batch-frozen-gate.json').write_text(json.dumps(r,indent=2));print('RESULT '+json.dumps({k:v for k,v in r.items() if k!='rows'}),flush=True)

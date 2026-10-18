"""Read-only original source support fixture; no Unity/Blender, no sampling stride."""
import os
os.environ.setdefault('OPENBLAS_NUM_THREADS','1')
os.environ.setdefault('OMP_NUM_THREADS','1')
import json, pathlib, struct, hashlib
import numpy as np
repo=pathlib.Path(__file__).resolve().parents[6]
packet=repo/'art/drafts/newrig-sailor-v1/dense-def-fbx-packet-v1'
out=pathlib.Path(__file__).resolve().parent
original=packet/'original-indexed-attributes.npz'
a=np.load(original); reference=json.loads((packet/'pose-reference.json').read_text())
names=reference['boneNames'];foot={names.index(x) for x in ('DEF-foot.L','DEF-toe.L','DEF-foot.R','DEF-toe.R')}
j=a['jointIndices'];w=a['weights'];ids=np.nonzero(np.any(np.isin(j,list(foot))&(w>0),axis=1))[0]
assert len(ids)==143168
j=j[ids].astype('<i4');w=w[ids].astype('<f4')
rest=a['armatureWorldBlender']@a['DEFRestArmature']; inv=np.linalg.inv(rest)
p=a['positionsBlenderLocal'][ids].astype(np.float64); ph=np.column_stack((p,np.ones(len(p))))
world=ph@a['meshWorldBlender'].T
# Convert original vertex world to each original bone's bind-local frame once;
# float32 bindpoint contract equals the portable runtime leaf input contract.
bind=np.empty((len(ids),4,3),dtype='<f4')
for k in range(4):bind[:,k]=np.einsum('nij,nj->ni',inv[j[:,k]],world)[:,:3]
B=np.array([[1,0,0,0],[0,0,1,0],[0,-1,0,0],[0,0,0,1]],float)
rows=[]
for pose in reference['references']:
    yup=np.array(pose['boneWorldUnityYUp'],float)
    # Y-up matrices use B local coordinate basis too; bind-local points remain
    # Blender bone-local, so multiply B^-1 on the right only for these Y rows.
    rows.append((yup@B)[:,1,:].astype('<f4'))
# Also all498 independent frozen source matrices; source world Blender Z is floor Y.
independent=np.load(packet/'offkey-original-source-reference.npz')
rows.extend(independent['boneWorldBlenderMetres'][:,:,2,:].astype('<f4'))
rows=np.array(rows,dtype='<f4')
with (out/'original-full-feet.fsf').open('wb') as f:
    f.write(b'FSF1');f.write(struct.pack('<iii',len(ids),len(names),len(rows)))
    f.write(j.tobytes());f.write(w.tobytes());f.write(bind.tobytes());f.write(rows.tobytes())
manifest={'sourceVertices':int(a['positionsBlenderLocal'].shape[0]),'allOriginalWeightedFootPoints':len(ids),'bones':len(names),'frozenSourcePoses':len(rows),'sourceNPZSHA256':hashlib.sha256(original.read_bytes()).hexdigest(),'fixtureSHA256':hashlib.sha256((out/'original-full-feet.fsf').read_bytes()).hexdigest(),'contract':'Full143168 original weighted foot points, all4 original influences, float32 bind-local points + vertical bone rows. This is mathematical accel parity, not Unity point adapter/floor/visual/phone acceptance.'}
(out/'fixture-provenance.json').write_text(json.dumps(manifest,indent=2)+'\n')
print(json.dumps(manifest))

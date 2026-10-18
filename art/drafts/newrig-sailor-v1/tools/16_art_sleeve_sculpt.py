"""Bounded posed-space designed cap-envelope corrective; diagnostic, no promotion.
Rest garment radial envelope is shape prior; collar/central chest/distal arms exact0.
Not Laplacian smoothing: displacement is radial to posed humeral axis only.
"""
import bpy,sys,json,hashlib,math
from pathlib import Path
import numpy as np
from mathutils import Vector
ROOT=Path(__file__).resolve().parents[4];O=ROOT/'art/drafts/newrig-sailor-v1';D=O/'corrective-v5';D.mkdir(exist_ok=True)
s=(O/'tools/05_full_motion_gallery.py').read_text();exec(compile(s[:s.index('reports=[]')],str(O/'tools/05_full_motion_gallery.py'),'exec'))
for bone in rig.data.bones:
 if bone.use_deform:bone.bbone_segments=1
assert not body.modifiers[0].use_deform_preserve_volume
r=retarget('dance_01',.75)
N=len(body.data.vertices);base=np.empty(N*3,np.float32);body.data.vertices.foreach_get('co',base);base=base.reshape(-1,3);bw=np.array(body.matrix_world);rw=base@bw[:3,:3].T+bw[:3,3]
ob=body.evaluated_get(bpy.context.evaluated_depsgraph_get());me=ob.to_mesh();pw=np.empty(N*3,np.float32);me.vertices.foreach_get('co',pw);pw=pw.reshape(-1,3)@bw[:3,:3].T+bw[:3,3];ob.to_mesh_clear()
mask=np.zeros(N);delta=np.zeros((N,3));sideReports=[]
def smooth(x):x=np.clip(x,0,1);return x*x*(3-2*x)
chestUp=np.array(rig.matrix_world.to_3x3()@rig.pose.bones['ORG-spine.003'].matrix.col[1].xyz);chestUp/=np.linalg.norm(chestUp)
for suffix in ['L']:
 b=rig.data.bones['ORG-upper_arm.'+suffix];bh=np.array(rig.matrix_world@b.head_local);bt=np.array(rig.matrix_world@b.tail_local);L=np.linalg.norm(bt-bh);axis=(bt-bh)/L
 ph=np.array(rig.matrix_world@rig.pose.bones[b.name].head);pt=np.array(rig.matrix_world@rig.pose.bones[b.name].tail);PL=np.linalg.norm(pt-ph);pa=(pt-ph)/PL;up=chestUp-pa*np.dot(chestUp,pa);up/=np.linalg.norm(up)
 restAlong=(rw-bh)@axis/L;distance=np.linalg.norm(rw-bh,axis=1);t=(pw-ph)@pa/PL;h=(pw-ph)@up;rad=np.linalg.norm((pw-ph)-t[:,None]*PL*pa,axis=1)
 fb=rig.data.bones['ORG-forearm.'+suffix];fh=np.array(rig.matrix_world@fb.head_local);ft=np.array(rig.matrix_world@fb.tail_local);fa=(ft-fh);FL=np.linalg.norm(fa);fa/=FL;foreAlong=(rw-fh)@fa/FL
 own=(rw[:,0]>.095) if suffix=='L' else (rw[:,0]<-.095)
 local=smooth((L*1.3-distance)/(L*.3));local*=smooth((np.abs(rw[:,0])-.095)/.035);local*=smooth((restAlong+.10)/.15);local*=1-smooth((restAlong-.85)/.30);local*=1-smooth((rw[:,2]-.825)/.025);local*=1-smooth((foreAlong-.10)/.20);local*=own
 brush=np.exp(-.5*((t-.65)/.30)**2)*smooth((h/np.maximum(rad,1e-10)-.30)/.50)
 strength=.012;move=-up[None,:]*(strength*local*brush)[:,None]
 delta+=move;mask=np.maximum(mask,local*brush);sideReports.append({'side':suffix,'maskedVertices':int(np.count_nonzero(local*brush)),'movedVertices':int(np.count_nonzero(np.linalg.norm(move,axis=1)>1e-7)),'maxDesiredDisplacementM':float(np.linalg.norm(move,axis=1).max()),'sculptStroke':{'posedUpperArmCenterT':.65,'gaussianSigmaUpperArmLength':.30,'direction':'negative chest-up projectedperpendicularhumerus','maxBrushStrengthM':strength,'artAuthored':True}})
roi=np.flatnonzero(np.linalg.norm(delta,axis=1)>1e-10);assert len(roi)>0
# Exact inverse of final current LBS linear map, not a guessed inverse bone.
M={name:np.array(rig.matrix_world@rig.pose.bones[name].matrix@rig.data.bones[name].matrix_local.inverted()@rig.matrix_world.inverted()@body.matrix_world) for name in {g.name for g in body.vertex_groups}}
A=np.zeros((len(roi),3,3))
for k,i in enumerate(roi):
 for g in body.data.vertices[int(i)].groups:A[k]+=M[body.vertex_groups[g.group].name][:3,:3]*g.weight
localDelta=np.linalg.solve(A,delta[roi]);assert np.all(np.isfinite(localDelta))
shape=np.array(base,copy=True);shape[roi]+=localDelta
body.shape_key_add(name='Basis');key=body.shape_key_add(name='DesignedCapEnvelope75_REVIEW_ONLY');key.data.foreach_set('co',shape.ravel());key.value=0
np.savez_compressed(D/'designed-cap75-v5.npz',indices=roi,localDelta=localDelta,mask=mask[roi],basePositionSHA256=np.array(hashlib.sha256(base.tobytes()).hexdigest()))
center=rig.matrix_world@((rig.pose.bones['ORG-upper_arm.L'].head+rig.pose.bones['ORG-upper_arm.R'].head)*.5);center.z+=.04
scene.render.resolution_x=768;scene.render.resolution_y=768;cd.ortho_scale=.65
for view,off in [('front',(0,-2,.05)),('threequarter',(.80,-2,.14))]:
 cam.location=center+Vector(off);cam.rotation_euler=(center-cam.location).to_track_quat('-Z','Y').to_euler()
 for strength in [0,.5,1]:
  key.value=strength;bpy.context.view_layer.update();scene.render.filepath=str(D/f'dance75-{view}-strength{strength}.png');bpy.ops.render.render(write_still=True)
key.value=0
report={'status':'DIAGNOSTIC_NOT_ACCEPTED','method':'art-directed broad upper-sleeve grab sculpt12mm withGaussian brushand exactprotectedanchors; inverse exact LBS','sides':sideReports,'targetCount':1,'changedVertices':int(len(roi)),'maxRestSpaceDeltaM':float(np.linalg.norm(localDelta,axis=1).max()),'collarCentralChestExactZero':bool(np.all(np.linalg.norm(delta[np.abs(rw[:,0])<=.095],axis=1)==0)),'basePositionsSHA256':hashlib.sha256(base.tobytes()).hexdigest(),'limitations':['One selected dance pose only; not portable animation accepted.','12mm posedgrab stroke is explicit art design, not source-authored anatomy; requirevisualcontinuity gate.','No source mesh/UV/skin weight changes; proposed art corrective may alter silhouette.']}
(D/'report.json').write_text(json.dumps(report,indent=2));print('RESULT '+json.dumps(report),flush=True)

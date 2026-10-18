"""Corrected brief: diagnose low medialtrapezius, not lower outer sleeve.
Temporary pose-driven clavicle support is a sculpt guide, not source-identical dance.
"""
import bpy,json,math,hashlib
from pathlib import Path
import numpy as np
from mathutils import Vector,Matrix
ROOT=Path(__file__).resolve().parents[4];O=ROOT/'art/drafts/newrig-sailor-v1';D=O/'trapezius-v1';D.mkdir(exist_ok=True)
s=(O/'tools/05_full_motion_gallery.py').read_text();exec(compile(s[:s.index('reports=[]')],str(O/'tools/05_full_motion_gallery.py'),'exec'))
for b in rig.data.bones:
 if b.use_deform:b.bbone_segments=1
N=len(body.data.vertices);bindCoords=np.empty(N*3,np.float32);body.data.vertices.foreach_get('co',bindCoords);bindCoords=bindCoords.reshape(-1,3);mw=np.array(body.matrix_world);rw=bindCoords@mw[:3,:3].T+mw[:3,3]
def evaluated():
 ob=body.evaluated_get(bpy.context.evaluated_depsgraph_get());me=ob.to_mesh();a=np.empty(N*3,np.float32);me.vertices.foreach_get('co',a);ob.to_mesh_clear();return a.reshape(-1,3)@mw[:3,:3].T+mw[:3,3]
def elevation(v,up):return math.degrees(math.asin(np.clip(np.dot(v/np.linalg.norm(v),up/np.linalg.norm(up)),-1,1)))
restUp=np.array(rig.matrix_world.to_3x3()@rig.data.bones['ORG-spine.003'].matrix_local.col[1].xyz)
def support():
 keep={t:(rig.matrix_world@rig.pose.bones[t].matrix).to_quaternion() for t,d in mapping if any(k in t for k in ['upper_arm','forearm','hand','thumb','f_index','f_middle','f_ring','f_pinky'])};up=np.array(rig.matrix_world.to_3x3()@rig.pose.bones['ORG-spine.003'].matrix.col[1].xyz);report=[]
 for side in ['L','R']:
  arm=rig.pose.bones['ORG-upper_arm.'+side];cl=rig.pose.bones['ORG-shoulder.'+side];ar=rig.data.bones[arm.name];cr=rig.data.bones[cl.name]
  ae=elevation(np.array(rig.matrix_world.to_3x3()@(arm.tail-arm.head)),up);are=elevation(np.array(rig.matrix_world.to_3x3()@(ar.tail_local-ar.head_local)),restUp);ce=elevation(np.array(rig.matrix_world.to_3x3()@(cl.tail-cl.head)),up);cre=elevation(np.array(rig.matrix_world.to_3x3()@(cr.tail_local-cr.head_local)),restUp)
  desired=max(0,ae-are)*.25;degrees=np.clip(desired-(ce-cre),0,15);R.hinge(rig,'shoulder.'+side,float(degrees),tuple(up));report.append({'side':side,'armElevation':ae,'armRestElevation':are,'clavicleElevation':ce,'clavicleRestElevation':cre,'supportDegrees':float(degrees)})
 # Keep original arm/finger world rotations; guide onlychangesgirdleplacement.
 for t,d in mapping:
  if t not in keep:continue
  pb=rig.pose.bones[t];q=rig.matrix_world.to_quaternion().inverted()@keep[t];pb.matrix=Matrix.Translation(pb.matrix.translation)@q.to_matrix().to_4x4();bpy.context.view_layer.update()
 return report
scene.render.resolution_x=768;scene.render.resolution_y=768;cd.ortho_scale=.58
reports=[]
for phase in ['rest',.5,.75]:
 if phase=='rest':R.apply_pose(rig,{'name':'rest','ops':[('fk',)]},.98)
 else:retarget('dance_01',phase)
 before=evaluated();headBefore=(rig.matrix_world@rig.pose.bones['ORG-spine.006'].head).copy();poseReport={'phase':phase}
 for variant in ['baseline','clavicleSupportGuide']:
  if variant=='clavicleSupportGuide':
   if phase=='rest':continue
   poseReport['support']=support();after=evaluated();delta=after-before;poseReport['headBoneMovementM']=(rig.matrix_world@rig.pose.bones['ORG-spine.006'].head-headBefore).length;poseReport['headSurfaceMaxMovementM']=float(np.linalg.norm(delta[rw[:,2]>.88],axis=1).max());np.savez_compressed(D/f'guide-delta-{phase}.npz',worldDelta=delta.astype(np.float32))
  # Samecamera target perpose, includeshead/neck/trapezius andshoulderline.
  target=Vector((float((before[:,0].min()+before[:,0].max())/2),float((before[:,1].min()+before[:,1].max())/2),float(np.quantile(before[:,2],.83))));target.z+=.025
  for view,off in [('front',(0,-2,.05)),('backthreequarter',(.8,2,.10))]:
   cam.location=target+Vector(off);cam.rotation_euler=(target-cam.location).to_track_quat('-Z','Y').to_euler();scene.render.filepath=str(D/f'{phase}-{variant}-{view}.png');bpy.ops.render.render(write_still=True)
 reports.append(poseReport);(D/'report.json').write_text(json.dumps({'status':'SCULPT_GUIDE_REVIEW_NOT_ACCEPTED','note':'Underlyingsourceunchanged. Supportguidemoveswholearmspositions andisnotfinal localizedcorrective. Nextmask willkeepheadanddistalarmexact.','poses':reports},indent=2))
print('RESULT '+json.dumps(reports),flush=True)

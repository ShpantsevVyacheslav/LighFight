"""Original-density fresh-rig diagnostic. Versioned, no source edits; actual LBS."""
import bpy,json,math
from pathlib import Path
import numpy as np
from mathutils import Vector,Matrix,Quaternion
O=Path(__file__).resolve().parents[1];D=O/'dense-trapezius-truebind-v2';D.mkdir(exist_ok=True)
s=(O/'tools/05_full_motion_gallery.py').read_text();exec(compile(s[:s.index('reports=[]')],str(O/'tools/05_full_motion_gallery.py'),'exec'))
for b in rig.data.bones:
 if b.use_deform:b.bbone_segments=1
body.modifiers[0].use_deform_preserve_volume=False
N=len(body.data.vertices);body.data.calc_loop_triangles();assert N==994452 and len(body.data.loop_triangles)==1988914
src=(O/'tools/01_audit_fit.py').read_text();exec(src[src.index('def hash_arrays'):src.index('def write')]);original=hash_arrays(body);assert original==json.loads((O/'stage02.json').read_text())['sourceAttributesSHA256']
# Evaluated identity FK control frames account for Rigify constraints; DEF inverse bind remains matrix_local.
R.apply_pose(rig,{'name':'trueRest','ops':[('fk',)]},.98);bpy.context.view_layer.update()
oldRest=rest.copy();rest={t:(rig.matrix_world@rig.pose.bones[t].matrix).to_quaternion() for t,d in mapping}
calibration={t:math.degrees(oldRest[t].rotation_difference(rest[t]).angle) for t,d in mapping}
mw=np.array(body.matrix_world);raw=np.empty(N*3,np.float32);body.data.vertices.foreach_get('co',raw);raw=raw.reshape(-1,3);rw=raw@mw[:3,:3].T+mw[:3,3]
def evaluated():
 ob=body.evaluated_get(bpy.context.evaluated_depsgraph_get());me=ob.to_mesh();a=np.empty(N*3,np.float32);me.vertices.foreach_get('co',a);ob.to_mesh_clear();return a.reshape(-1,3)@mw[:3,:3].T+mw[:3,3]
# Independent actual weighted matrix skinning witness.
def witness():
 ids=np.linspace(0,N-1,12000,dtype=int);obs=evaluated()[ids];M={g.name:np.array(rig.matrix_world@rig.pose.bones[g.name].matrix@rig.data.bones[g.name].matrix_local.inverted()@rig.matrix_world.inverted()@body.matrix_world) for g in body.vertex_groups};out=[]
 for i in ids:
  v=body.data.vertices[int(i)];p=np.r_[np.array(v.co),1.];q=np.zeros(4)
  for g in v.groups:q+=M[body.vertex_groups[g.group].name]@p*g.weight
  out.append(q[:3])
 e=np.linalg.norm(obs-np.array(out),axis=1);assert e.max()<1e-5;return float(e.max())
def elevation(v,up):return math.degrees(math.asin(float(np.clip(v.normalized().dot(up.normalized()),-1,1))))
restUp=rig.matrix_world.to_3x3()@rig.data.bones['ORG-spine.003'].matrix_local.col[1].xyz
report={'status':'DENSE_VISUAL_REVIEW_REQUIRED_NO_PROMOTION','vertices':N,'triangles':1988914,'evaluatedControlVsBoneRestAngleDeg':calibration,'poses':[]}
scene.cycles.samples=8;scene.render.resolution_x=720;scene.render.resolution_y=720;cd.ortho_scale=.64
for phase in ['rest',.25,.5,.75]:
 if phase=='rest':R.apply_pose(rig,{'name':'rest','ops':[('fk',)]},.98)
 else:retarget('dance_01',phase)
 before=evaluated();hands={side:(rig.matrix_world@rig.pose.bones['ORG-hand.'+side].head).copy() for side in ['L','R']};head=(rig.matrix_world@rig.pose.bones['ORG-spine.006'].head).copy();row={'phase':phase,'LBSWitnessMaxM':witness()}
 target=(rig.matrix_world@rig.pose.bones['ORG-spine.004'].head).copy();target.z-=.025
 for variant in ['baseline','support']:
  if variant=='support':
   if phase=='rest':continue
   keep={t:(rig.matrix_world@rig.pose.bones[t].matrix).to_quaternion() for t,d in mapping if any(k in t for k in ['upper_arm','forearm','hand','thumb','f_index','f_middle','f_ring','f_pinky'])};up=rig.matrix_world.to_3x3()@rig.pose.bones['ORG-spine.003'].matrix.col[1].xyz;angles=[]
   for side in ['L','R']:
    a=rig.pose.bones['ORG-upper_arm.'+side];c=rig.pose.bones['ORG-shoulder.'+side];ar=rig.data.bones[a.name];cr=rig.data.bones[c.name];v=rig.matrix_world.to_3x3()@(c.tail-c.head);ae=elevation(rig.matrix_world.to_3x3()@(a.tail-a.head),up);are=elevation(rig.matrix_world.to_3x3()@(ar.tail_local-ar.head_local),restUp);ce=elevation(v,up);cre=elevation(rig.matrix_world.to_3x3()@(cr.tail_local-cr.head_local),restUp);deficit=max(0,(ae-are)*.25-(ce-cre));degrees=15*math.tanh(deficit/15);axis=v.cross(up).normalized();pb=rig.pose.bones['shoulder.'+side];world=rig.matrix_world@pb.matrix;q=Quaternion(axis,math.radians(degrees))@world.to_quaternion();pb.matrix=rig.matrix_world.inverted()@(Matrix.Translation(world.translation)@q.to_matrix().to_4x4());bpy.context.view_layer.update();angles.append({'side':side,'supportDeg':degrees,'armElevationDeg':ae,'clavicleElevationDeg':ce})
   for t,d in mapping:
    if t in keep:
     pb=rig.pose.bones[t];q=rig.matrix_world.to_quaternion().inverted()@keep[t];pb.matrix=Matrix.Translation(pb.matrix.translation)@q.to_matrix().to_4x4();bpy.context.view_layer.update()
   after=evaluated();row.update({'support':angles,'surfaceMaxDisplacementM':float(np.linalg.norm(after-before,axis=1).max()),'headBoneDisplacementM':float((rig.matrix_world@rig.pose.bones['ORG-spine.006'].head-head).length),'handDisplacementM':{side:float((rig.matrix_world@rig.pose.bones['ORG-hand.'+side].head-hands[side]).length) for side in hands},'correctedLBSWitnessMaxM':witness()})
  for view,off in [('front',(0,-2,.06)),('backthreequarter',(.85,2,.12))]:
   cam.location=target+Vector(off);cam.rotation_euler=(target-cam.location).to_track_quat('-Z','Y').to_euler();scene.render.filepath=str(D/f'{phase}-{variant}-{view}.png');bpy.ops.render.render(write_still=True)
 row['sourceAttributesExact']=hash_arrays(body)==original;assert row['sourceAttributesExact'];report['poses'].append(row);(D/'report.json').write_text(json.dumps(report,indent=2))
assert hash_arrays(body)==original
bpy.context.preferences.filepaths.save_version=0;bpy.ops.wm.save_as_mainfile(filepath=str(D/'dense-trapezius-review.blend'),compress=True)
print('RESULT '+json.dumps(report),flush=True)

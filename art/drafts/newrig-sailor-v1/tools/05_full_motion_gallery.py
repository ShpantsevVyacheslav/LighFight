"""Actual dense new-DEF pose renders; diagnostic no acceptance or rig edits."""
import bpy,types,json,math,sys,hashlib
import numpy as np
from pathlib import Path
from mathutils import Vector, Matrix
ROOT=Path(__file__).resolve().parents[4];O=ROOT/'art/drafts/newrig-sailor-v1';(O/'gallery-v1').mkdir(exist_ok=True)
bpy.ops.wm.open_mainfile(filepath=str(O/'stage02-bound-original-dense.blend'))
code=(Path.home()/'.agents/skills/scenario-blender-rigging/scripts/bx_rig.py').read_text();R=types.ModuleType('pilot_rig');R.__file__=str(Path.home()/'.agents/skills/scenario-blender-rigging/scripts/bx_rig.py');exec(compile(code,R.__file__,'exec'),R.__dict__)
rig=next(o for o in bpy.data.objects if o.type=='ARMATURE' and o.name!='FreshSailor_CONTROL_METARIG_v1');body=bpy.data.objects['Sailor_OriginalDense_UNTOUCHED'];scene=bpy.context.scene
for o in scene.objects:
 if o.type=='MESH' and o!=body:o.hide_render=True
scene.render.engine='CYCLES';scene.cycles.samples=8;scene.cycles.use_denoising=True;scene.render.resolution_x=768;scene.render.resolution_y=768;scene.render.resolution_percentage=100;scene.render.image_settings.file_format='PNG';scene.view_settings.view_transform='AgX';scene.world.color=(.15,.15,.15)
c=Vector((0,0,.75));cd=bpy.data.cameras.new('PilotReview');cam=bpy.data.objects.new('PilotReview',cd);scene.collection.objects.link(cam);cam.location=c+Vector((0,-2,.05));cam.rotation_euler=(c-cam.location).to_track_quat('-Z','Y').to_euler();cd.type='ORTHO';cd.ortho_scale=.65;scene.camera=cam
for name,loc,power in [('Key',(-1,-2,2),450),('Fill',(1,-1,1),250),('Rim',(0,1,2),300)]:
 d=bpy.data.lights.new(name,'AREA');d.energy=power;d.size=2;l=bpy.data.objects.new(name,d);scene.collection.objects.link(l);l.location=loc;l.rotation_euler=(c-l.location).to_track_quat('-Z','Y').to_euler()

# Rest-frame-aware rotation retarget pilot; no native local rotations copied blindly.
pre=set(bpy.data.objects);actions=set(bpy.data.actions)
source=next((ROOT/'art/source/tripo-striped-sailor-native-full-pack/unpacked').glob('*.fbx'))
bpy.ops.import_scene.fbx(filepath=str(source),use_anim=True,automatic_bone_orientation=False)
new=[o for o in bpy.data.objects if o not in pre];donor=next(o for o in new if o.type=='ARMATURE');clips=[a for a in bpy.data.actions if a not in actions]
for o in new:
 if o.type=='MESH':o.hide_render=True

G=O/'gallery-v1'
mapping=[('torso','Hips'),('spine_fk.001','Spine'),('spine_fk.002','Chest'),('spine_fk.003','UpperChest'),('neck','Neck'),('head','Head')]
for side,suffix in [('Left','L'),('Right','R')]:
 mapping.extend([(f'shoulder.{suffix}',side+'_Shoulder'),(f'upper_arm_fk.{suffix}',side+'_UpperArm'),(f'forearm_fk.{suffix}',side+'_LowerArm'),(f'hand_fk.{suffix}',side+'_Hand'),(f'thigh_fk.{suffix}',side+'_UpperLeg'),(f'shin_fk.{suffix}',side+'_LowerLeg'),(f'foot_fk.{suffix}',side+'_Foot'),(f'toe_fk.{suffix}',side+'_Toes')])
 for native,target in [('Thumb','thumb'),('Index','f_index'),('Middle','f_middle'),('Ring','f_ring'),('Pinky','f_pinky')]:
  for i,part in enumerate(['Proximal','Intermediate','Distal'],1):mapping.append((f'{target}.{i:02}.{suffix}',side+'_'+native+part))
missing=[(t,d) for t,d in mapping if t not in rig.pose.bones or d not in donor.pose.bones];assert not missing,missing
rest={t:(rig.matrix_world@rig.data.bones[t].matrix_local).to_quaternion() for t,d in mapping};drest={d:(donor.matrix_world@donor.data.bones[d].matrix_local).to_quaternion() for t,d in mapping}
# Height ratio comes from untouched source world rest geometry, not guessed units.
dm=next(o for o in new if o.type=='MESH');dv=np.array([list(dm.matrix_world@v.co) for v in dm.data.vertices]);ratio=.979980466815771/dv[:,2].ptp()
dhip=donor.matrix_world@donor.data.bones['Hips'].head_local;troot=rig.matrix_world@rig.data.bones['torso'].head_local
cases=[('box_02',x) for x in [0,.35,.70]]+[('front_kick_01',x) for x in [.15,.45,.70]]+[('hit_to_head',x) for x in [.25,.60]]+[('walk',x) for x in [.20,.70]]+[('dance_01',x) for x in [.25,.50,.75]]
def retarget(name,phase):
 R.apply_pose(rig,{'name':'fkrest','ops':[('fk',)]},.98)
 clip=next(a for a in clips if name in a.name);donor.animation_data.action=clip;f=clip.frame_range[0]+phase*(clip.frame_range[1]-clip.frame_range[0]);scene.frame_set(math.floor(f),subframe=f%1);bpy.context.view_layer.update()
 for t,d in mapping:
  pb=rig.pose.bones[t];dq=(donor.matrix_world@donor.pose.bones[d].matrix).to_quaternion();worldq=dq@drest[d].inverted()@rest[t];localq=rig.matrix_world.to_quaternion().inverted()@worldq;pos=pb.matrix.translation.copy()
  if t=='torso':pos=rig.matrix_world.inverted()@(troot+((donor.matrix_world@donor.pose.bones['Hips'].head)-dhip)*ratio)
  pb.matrix=Matrix.Translation(pos)@localq.to_matrix().to_4x4();bpy.context.view_layer.update()
 deps=bpy.context.evaluated_depsgraph_get();ob=body.evaluated_get(deps);me=ob.to_mesh();co=np.empty(len(me.vertices)*3,np.float32);me.vertices.foreach_get('co',co);co=co.reshape(-1,3);mw=np.array(ob.matrix_world);co=co@mw[:3,:3].T+mw[:3,3];lo=co.min(0);hi=co.max(0);ob.to_mesh_clear()
 errors={}
 for t,d in mapping:
  desired=(donor.matrix_world@donor.pose.bones[d].matrix).to_quaternion()@drest[d].inverted()@rest[t];actual=(rig.matrix_world@rig.pose.bones[t].matrix).to_quaternion();angle=math.degrees(desired.rotation_difference(actual).angle);errors[t]=min(angle,abs(360-angle))
 return {'clip':name,'phase':phase,'sourceFrame':f,'sourceSeconds':(f-clip.frame_range[0])/24,'meshWorldMin':lo.tolist(),'meshWorldMax':hi.tolist(),'worldRotationMaxErrorDeg':max(errors.values()),'rotationErrorsDeg':errors}
reports=[]
for name,phase in cases:
 r=retarget(name,phase);reports.append(r);print('POSE_PREFLIGHT '+json.dumps(r),flush=True)
(G/'manifest-preflight.json').write_text(json.dumps({'status':'NOT_QUALITY_ACCEPTED','mapping':mapping,'heightRatio':ratio,'cases':reports},indent=2))
# Single full-body projection scale fits every complete limb; perpose center follows root motion.
fullscale=max(max(np.array(r['meshWorldMax'])-np.array(r['meshWorldMin'])) for r in reports)*1.65
for r in reports:
 retarget(r['clip'],r['phase']);lo=Vector(r['meshWorldMin']);hi=Vector(r['meshWorldMax']);center=(lo+hi)*.5
 for kind in ['fullbody','shoulders']:
  if kind=='fullbody':target=center;scale=fullscale;scene.render.resolution_x=640;scene.render.resolution_y=800
  else:target=rig.matrix_world@((rig.pose.bones['ORG-upper_arm.L'].head+rig.pose.bones['ORG-upper_arm.R'].head)*.5);target.z+=.025;scale=.64;scene.render.resolution_x=640;scene.render.resolution_y=640
  cam.location=target+Vector((.80,-2,.14));cam.rotation_euler=(target-cam.location).to_track_quat('-Z','Y').to_euler();cd.ortho_scale=scale
  # Lights follow only translation, retaining same angle/intensity/source material acrossgallery.
  for light,off in [('Key',(-1,-2,2)),('Fill',(1,-1,1)),('Rim',(0,1,2))]:
   l=bpy.data.objects[light];l.location=center+Vector(off);l.rotation_euler=(center-l.location).to_track_quat('-Z','Y').to_euler()
  filename=f"{r['clip']}-{r['phase']:.2f}-{kind}.png";scene.render.filepath=str(G/filename);bpy.ops.render.render(write_still=True);r[kind]=filename
 (G/'manifest.json').write_text(json.dumps({'status':'REVIEW_ONLY_NOT_QUALITY_ACCEPTED','mapping':mapping,'heightRatio':ratio,'fullbodyOrthoScale':fullscale,'limitations':['No IK foot-contact correction; proportions/rest differ, ground penetration can occur.','Fullbody root translation scaled by source/dense height; all limb/foot/toe/finger FK controls mapped.','Facial expression not retargeted; eye/jaw remains rest.','No full animated clip acceptance; discrete authored-phase screenshots only.'],'cases':reports},indent=2))
src=(O/'tools/01_audit_fit.py').read_text();exec(src[src.index('def hash_arrays'):src.index('def write')]);assert hash_arrays(body)==json.loads((O/'stage02.json').read_text())['sourceAttributesSHA256']
print('RESULT '+json.dumps({'cases':len(reports),'images':len(reports)*2,'denseAttributesExact':True}),flush=True)

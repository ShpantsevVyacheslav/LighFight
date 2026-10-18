"""Actual dense new-DEF pose renders; diagnostic no acceptance or rig edits."""
import bpy,types,json,math,sys,hashlib
import numpy as np
from pathlib import Path
from mathutils import Vector, Matrix
ROOT=Path(__file__).resolve().parents[4];O=ROOT/'art/drafts/newrig-sailor-v1';(O/'poses').mkdir(exist_ok=True)
bpy.ops.wm.open_mainfile(filepath=str(O/'stage02-bound-original-dense.blend'))
code=(Path.home()/'.agents/skills/scenario-blender-rigging/scripts/bx_rig.py').read_text();R=types.ModuleType('pilot_rig');R.__file__=str(Path.home()/'.agents/skills/scenario-blender-rigging/scripts/bx_rig.py');exec(compile(code,R.__file__,'exec'),R.__dict__)
rig=next(o for o in bpy.data.objects if o.type=='ARMATURE' and o.name!='FreshSailor_CONTROL_METARIG_v1');body=bpy.data.objects['Sailor_OriginalDense_UNTOUCHED'];scene=bpy.context.scene
for o in scene.objects:
 if o.type=='MESH' and o!=body:o.hide_render=True
scene.render.engine='CYCLES';scene.cycles.samples=12;scene.cycles.use_denoising=True;scene.render.resolution_x=768;scene.render.resolution_y=768;scene.render.resolution_percentage=100;scene.render.image_settings.file_format='PNG';scene.view_settings.view_transform='AgX';scene.world.color=(.15,.15,.15)
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
mapping=[('torso','Hips'),('spine_fk.001','Spine'),('spine_fk.002','Chest'),('spine_fk.003','UpperChest'),('neck','Neck'),('head','Head')]
for side,suffix in [('Left','L'),('Right','R')]:
 mapping.extend([(f'shoulder.{suffix}',side+'_Shoulder'),(f'upper_arm_fk.{suffix}',side+'_UpperArm'),(f'forearm_fk.{suffix}',side+'_LowerArm'),(f'hand_fk.{suffix}',side+'_Hand')])
mapping=[(t,d) for t,d in mapping if t in rig.pose.bones and d in donor.pose.bones]
rest={t:(rig.matrix_world@rig.data.bones[t].matrix_local).to_quaternion() for t,d in mapping}
drest={d:(donor.matrix_world@donor.data.bones[d].matrix_local).to_quaternion() for t,d in mapping}
reports=[]
for phase in ([.75] if '--save-only' in sys.argv else [.25,.5,.75]):
 R.apply_pose(rig,{'name':'fkrest','ops':[('fk',)]},.98)
 clip=next(a for a in clips if 'dance_01' in a.name);donor.animation_data.action=clip;f=clip.frame_range[0]+phase*(clip.frame_range[1]-clip.frame_range[0]);scene.frame_set(math.floor(f),subframe=f%1);bpy.context.view_layer.update()
 for t,d in mapping:
  pb=rig.pose.bones[t];dq=(donor.matrix_world@donor.pose.bones[d].matrix).to_quaternion();worldq=dq@drest[d].inverted()@rest[t];localq=rig.matrix_world.to_quaternion().inverted()@worldq;pos=pb.matrix.translation.copy();pb.matrix=Matrix.Translation(pos)@localq.to_matrix().to_4x4();bpy.context.view_layer.update()
 for view,offset in ([] if '--save-only' in sys.argv else [('front',(0,-2,.05)),('threequarter',(.95,-1.75,.05))]):
  cam.location=c+Vector(offset);cam.rotation_euler=(c-cam.location).to_track_quat('-Z','Y').to_euler();scene.render.filepath=str(O/'poses'/f'retarget-dance-{phase}-{view}.png');bpy.ops.render.render(write_still=True)
 reports.append({'phase':phase,'sourceFrame':f,'mapping':mapping})
src=(O/'tools/01_audit_fit.py').read_text();defs=src[src.index('def hash_arrays'):src.index('def write')];exec(defs)
assert hash_arrays(body)==json.loads((O/'stage02.json').read_text())['sourceAttributesSHA256']
if '--save-only' in sys.argv:
 bpy.context.preferences.filepaths.save_version=0
 bpy.ops.wm.save_as_mainfile(filepath=str(O/'stage04-dance75-retarget-review.blend'),compress=True)
 (O/'stage04-preservation.json').write_text(json.dumps({'denseAttributeHashesExact':True,'triangles':1988914,'pose':'dance_01 phase75%','status':'REVIEW_ONLY_NOT_VISUAL_ACCEPTED'},indent=2))
else:
 (O/'stage04.json').write_text(json.dumps({'status':'ROTATION_RETARGET_PILOT_NOT_ANIMATION_ACCEPTED','limitations':['root translation and fingers not retargeted','legs remain original rest','world-rest-aware rotation delta; different bind anatomy so numerical source vertex parity is not applicable'],'poses':reports},indent=2))
print('RESULT '+json.dumps(reports),flush=True)

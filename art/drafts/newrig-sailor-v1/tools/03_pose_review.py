"""Actual dense new-DEF pose renders; diagnostic no acceptance or rig edits."""
import bpy,types,json,math
from pathlib import Path
from mathutils import Vector
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
reports=[]
for name in ['rest','arm_down.L','arm_up.L','arm_forward.L','elbow_120.L']:
 skipped=R.apply_pose(rig,R.pose_by_name(name),.98);bpy.context.view_layer.update();scene.render.filepath=str(O/'poses'/f'{name}.png');bpy.ops.render.render(write_still=True);reports.append({'pose':name,'skippedOperations':skipped})
(O/'stage03.json').write_text(json.dumps({'status':'POSE_RENDERS_REVIEW_REQUIRED','poses':reports},indent=2));print('RESULT '+json.dumps(reports),flush=True)

"""Independent Blender import/render of appended GLB motion, original67 skinning."""
import bpy,json,math,hashlib
from pathlib import Path
from mathutils import Vector
ROOT=Path(__file__).resolve().parents[4];O=ROOT/'art/drafts/newrig-sailor-v1/native67-trapezius-v1';D=O/'renders';D.mkdir(exist_ok=True)
f=O/'sailor-native67-trapezius-candidate.glb';bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False);bpy.ops.import_scene.gltf(filepath=str(f));rig=next(o for o in bpy.context.scene.objects if o.type=='ARMATURE');meshes=[o for o in bpy.context.scene.objects if o.type=='MESH' and any(m.type=='ARMATURE' for m in o.modifiers)];assert len(meshes)==1;body=meshes[0]
for o in bpy.context.scene.objects:
 if o.type=='MESH' and o not in meshes:o.hide_render=True
rig.animation_data.use_nla=False;actions=list(bpy.data.actions);print('ACTIONS '+json.dumps([a.name for a in actions]),flush=True)
def action(name):
 found=[a for a in actions if a.name==name or a.name==name+'_Armature' or a.name.endswith('|'+name) or a.name.endswith('_'+name)];assert len(found)==1,(name,[a.name for a in found]);return found[0]
rig.animation_data.action=None;rig.data.pose_position='REST';bpy.context.view_layer.update();rest=[body.matrix_world@v.co for v in body.data.vertices];height=max(v.z for v in rest)-min(v.z for v in rest);rig.data.pose_position='POSE'
scene=bpy.context.scene;scene.render.engine='CYCLES';scene.cycles.samples=16;scene.cycles.use_denoising=True;scene.render.resolution_x=768;scene.render.resolution_y=768;scene.render.resolution_percentage=100;scene.render.image_settings.file_format='PNG';scene.world.color=(.15,.15,.15);scene.view_settings.view_transform='AgX'
cd=bpy.data.cameras.new('NativeGate');cam=bpy.data.objects.new('NativeGate',cd);scene.collection.objects.link(cam);scene.camera=cam;cd.type='ORTHO';cd.ortho_scale=height*.64
for name,off,power in [('Key',(-1,-2,2),450),('Fill',(1,-1,1),250),('Rim',(0,1,2),300)]:
 d=bpy.data.lights.new(name,'AREA');d.energy=power;d.size=2;l=bpy.data.objects.new(name,d);scene.collection.objects.link(l);l.location=off
reports=[]
for phase in [0,.25,.5,.75,1]:
 target=None
 for variant,name in [('baseline','dance_01'),('corrected','Dance_TrapeziusSupport_v1')]:
  a=action(name);rig.animation_data.action=a;frame=a.frame_range[0]+phase*(a.frame_range[1]-a.frame_range[0]);scene.frame_set(math.floor(frame),subframe=frame%1);bpy.context.view_layer.update()
  if target is None:target=rig.matrix_world@rig.pose.bones['UpperChest'].head;target.z+=height*.12
  for lname,off in [('Key',(-1,-2,2)),('Fill',(1,-1,1)),('Rim',(0,1,2))]:
   l=bpy.data.objects[lname];l.location=target+Vector(off);l.rotation_euler=(target-l.location).to_track_quat('-Z','Y').to_euler()
  for view,off in [('front',(0,-2,.05)),('backthreequarter',(.8,2,.10))]:
   cam.location=target+Vector(off);cam.rotation_euler=(target-cam.location).to_track_quat('-Z','Y').to_euler();filename=f'{phase:.2f}-{variant}-{view}.png';scene.render.filepath=str(D/filename);bpy.ops.render.render(write_still=True);reports.append({'phase':phase,'variant':variant,'clip':name,'frame':frame,'view':view,'image':filename})
  (O/'visual-gate.json').write_text(json.dumps({'status':'REVIEW_REQUIRED_NO_PROMOTION','candidateSHA256':hashlib.sha256(f.read_bytes()).hexdigest(),'triangles':sum(len(p.vertices)-2 for p in body.data.polygons),'bones':len(rig.data.bones),'height':height,'cameraOrthoScale':cd.ortho_scale,'images':reports},indent=2))
print('RESULT '+json.dumps({'images':len(reports),'triangles':sum(len(p.vertices)-2 for p in body.data.polygons),'status':'NEEDS_VISUAL_REVIEW'}),flush=True)

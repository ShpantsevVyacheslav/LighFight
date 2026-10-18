"""Actual GLB review; run only with exclusive heavy window, six CPU threads."""
import bpy,sys,json,hashlib,time
from pathlib import Path
from mathutils import Vector
ROOT=Path(__file__).resolve().parents[4];OUT=ROOT/'art/drafts/meeting-hybrid-v1';dest=OUT/'renders';dest.mkdir(exist_ok=True)
p=OUT/'meeting-master.glb';before=hashlib.sha256(p.read_bytes()).hexdigest()
bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False)
bpy.ops.import_scene.gltf(filepath=str(p))
scene=bpy.context.scene;scene.render.engine='CYCLES';scene.cycles.device='CPU';scene.cycles.samples=16;scene.cycles.use_denoising=True;scene.cycles.max_bounces=4;scene.render.threads_mode='FIXED';scene.render.threads=6
scene.render.resolution_x=1280;scene.render.resolution_y=760;scene.render.resolution_percentage=100;scene.render.image_settings.file_format='PNG';scene.view_settings.view_transform='AgX'
def gltf_point(p):return Vector((p[0],-p[2],p[1]))
scene.world.use_nodes=True;scene.world.node_tree.nodes.get('Background').inputs[0].default_value=(.12,.14,.16,1);scene.world.node_tree.nodes.get('Background').inputs[1].default_value=.35
for name,location,target,power,size in [('Window',(3,2.4,1.5),(0,.8,2),500,2.5),('FrontFill',(1.5,4,-2),(0,1,2.5),500,4),('Ceiling',(0,2.9,3),(0,0,3),250,2)]:
 data=bpy.data.lights.new(name,'AREA');data.energy=power;data.size=size;o=bpy.data.objects.new(name,data);scene.collection.objects.link(o);o.location=gltf_point(location);o.rotation_euler=(gltf_point(target)-o.location).to_track_quat('-Z','Y').to_euler()
data=bpy.data.cameras.new('MeetingReview');camera=bpy.data.objects.new('MeetingReview',data);scene.collection.objects.link(camera);scene.camera=camera;data.lens=32
roof=bpy.data.objects.get('Ceiling panels');assert roof is not None,'Missing separate opaque ceiling'
rows=[]
for name,location,target in [('whole',(-8,5.5,-8),(0,1.2,1.9)),('fight',(0,2.1,-6.4),(0,1.1,2.3)),('furniture',(-1.8,1.75,.75),(0,.9,3))]:
 roof.hide_render=name=='whole' # Explicit architectural cutaway only in the overview, never in fight/furniture.
 camera.location=gltf_point(location);camera.rotation_euler=(gltf_point(target)-camera.location).to_track_quat('-Z','Y').to_euler();scene.render.filepath=str(dest/(name+'.png'));started=time.time();bpy.ops.render.render(write_still=True);rows.append({'view':name,'location':location,'target':target,'lensMm':data.lens,'cutawayHiddenMeshes':['Ceiling panels'] if roof.hide_render else [],'coordinates':'authored glTF Y-up converted to Blender (x,-z,y)','seconds':time.time()-started,'file':name+'.png'});print('VIEW_DONE '+name,flush=True)
assert hashlib.sha256(p.read_bytes()).hexdigest()==before
(dest/'manifest.json').write_text(json.dumps({'status':'ACTUAL_GLB_RENDERS_PENDING_VISUAL_REVIEW','masterSHA256':before,'sourceUnchanged':True,'CPUthreads':6,'samples':16,'views':rows,'scope':'Geometry/material/layout review only; no gameplay/runtime/phone claim'},indent=2))
print('AGENT_RESULT '+json.dumps({'ok':True,'views':len(rows),'sourceUnchanged':True}),flush=True)

import bpy,sys,json,hashlib
from pathlib import Path
from mathutils import Vector
ROOT=Path(__file__).resolve().parents[4];OUT=ROOT/'art/drafts/meeting-meshy-v1';p=OUT/'review-with-room.glb'
bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False);bpy.ops.import_scene.gltf(filepath=str(p));s=bpy.context.scene;s.render.engine='CYCLES';s.cycles.samples=4;s.cycles.use_denoising=True;s.cycles.max_bounces=3;s.render.threads_mode='FIXED';s.render.threads=6;s.render.resolution_x=960;s.render.resolution_y=640;s.render.resolution_percentage=100;s.render.image_settings.file_format='PNG';s.view_settings.view_transform='AgX';s.world.use_nodes=True;s.world.node_tree.nodes.get('Background').inputs[1].default_value=.5
pnt=lambda p:Vector((p[0],-p[2],p[1]))
for name,loc,power,size in [('Fill',(1,2.7,1.8),700,4),('Window',(3,2.5,2.9),500,3)]:
 d=bpy.data.lights.new(name,'AREA');d.energy=power;d.size=size;o=bpy.data.objects.new(name,d);s.collection.objects.link(o);o.location=pnt(loc);o.rotation_euler=(pnt((-2,1.5,3.1))-o.location).to_track_quat('-Z','Y').to_euler()
d=bpy.data.cameras.new('RugCheck');o=bpy.data.objects.new('RugCheck',d);s.collection.objects.link(o);s.camera=o;d.lens=25;o.location=pnt((1.1,1.8,.5));o.rotation_euler=(pnt((-3.38,1.82,3.25))-o.location).to_track_quat('-Z','Y').to_euler();s.render.filepath=str(OUT/'rug-wall-check.png');bpy.ops.render.render(write_still=True);(OUT/'rug-wall-check.json').write_text(json.dumps({'sourceSHA256':hashlib.sha256(p.read_bytes()).hexdigest(),'view':'Opposite-window wall','meshNames':[o.name for o in s.objects if o.type=='MESH'],'status':'PENDING_VISUAL_CHECK'},indent=2));print('AGENT_RESULT rug render complete',flush=True)

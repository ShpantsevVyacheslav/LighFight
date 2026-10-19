import bpy,sys,json,hashlib,os
from pathlib import Path
from mathutils import Vector
ROOT=Path(__file__).resolve().parents[4];OUT=ROOT/'art/drafts/meeting-meshy-v1';gameplay=os.getenv('LF_ROOM_GAMEPLAY')=='1';p=OUT/('gameplay-with-room.glb' if gameplay else 'review-with-room.glb');dest=OUT/('gameplay-lod-review' if gameplay else 'placement-review');dest.mkdir(exist_ok=True)
bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False);bpy.ops.import_scene.gltf(filepath=str(p));s=bpy.context.scene;s.render.engine='CYCLES';s.cycles.samples=4;s.cycles.use_denoising=True;s.cycles.max_bounces=3;s.render.threads_mode='FIXED';s.render.threads=6;s.render.resolution_x=1100;s.render.resolution_y=700;s.render.resolution_percentage=100;s.render.image_settings.file_format='PNG';s.view_settings.view_transform='AgX';s.world.use_nodes=True;s.world.node_tree.nodes.get('Background').inputs[0].default_value=(.12,.14,.16,1);s.world.node_tree.nodes.get('Background').inputs[1].default_value=.35
pt=lambda p:Vector((p[0],-p[2],p[1]))
for name,loc,t,power,size in [('Window',(3,2.4,1.5),(0,.8,2),500,2.5),('FrontFill',(1.5,4,-2),(0,1,2.5),500,4),('Ceiling',(0,2.9,3),(0,0,3),250,2)]:
 d=bpy.data.lights.new(name,'AREA');d.energy=power;d.size=size;o=bpy.data.objects.new(name,d);s.collection.objects.link(o);o.location=pt(loc);o.rotation_euler=(pt(t)-o.location).to_track_quat('-Z','Y').to_euler()
d=bpy.data.cameras.new('PlacementReview');o=bpy.data.objects.new('PlacementReview',d);s.collection.objects.link(o);s.camera=o;d.lens=28
views=[('whole',(0,2.1,-5.4),(0,1.2,2.8)),('rug-and-corner',(1.8,1.9,.1),(-1.9,1.5,3.8)),('sill-and-table',(-.5,1.8,1.6),(2.8,1.1,2.5)),('table-closeup',(1.8,2.05,1.0),(0,1.18,3.0)),('rug-wall',(3.0,1.7,.5625),(-6.42,1.5,.5625))]
if gameplay:views=[(name,(loc[0],loc[1],loc[2]+(1.8 if name!='whole' else -.8)),(target[0],target[1],target[2]+1.8)) for name,loc,target in views]
views=[v for v in views if not os.getenv('LF_ROOM_VIEWS') or v[0] in os.environ['LF_ROOM_VIEWS'].split(',')]
for name,loc,t in views:
 o.location=pt(loc);o.rotation_euler=(pt(t)-o.location).to_track_quat('-Z','Y').to_euler();s.render.filepath=str(dest/(name+'.png'));bpy.ops.render.render(write_still=True);print('VIEW_DONE',name,flush=True)
(dest/'manifest.json').write_text(json.dumps({'sourceSHA256':hashlib.sha256(p.read_bytes()).hexdigest(),'views':views,'status':'PENDING_VISUAL_CHECK'},indent=2));print('AGENT_RESULT placement review complete',flush=True)

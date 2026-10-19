import bpy,sys,json,hashlib,math,os
from pathlib import Path
from mathutils import Vector
args=sys.argv[sys.argv.index('--')+1:];source=Path(args[0]).resolve();out=Path(args[1]).resolve();out.mkdir(parents=True,exist_ok=True);before=hashlib.sha256(source.read_bytes()).hexdigest();bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False);bpy.ops.import_scene.gltf(filepath=str(source));s=bpy.context.scene;bpy.context.view_layer.update();meshes=[o for o in s.objects if o.type=='MESH'];corners=[o.matrix_world@Vector(c) for o in meshes for c in o.bound_box];lo=Vector(tuple(min(p[k] for p in corners) for k in range(3)));hi=Vector(tuple(max(p[k] for p in corners) for k in range(3)));
if os.getenv('LF_REFERENCE_REVIEW'):
 reference=json.loads(Path(os.environ['LF_REFERENCE_REVIEW']).read_text());lo,hi=[Vector(v) for v in reference['sourceBlenderBounds']]
center=(lo+hi)/2;scale=2/(hi.z-lo.z)
root=bpy.data.objects.new('Review source normalization',None);s.collection.objects.link(root)
for obj in [o for o in list(s.objects) if o!=root and o.parent is None]:obj.parent=root
root.location=-center*scale;root.scale=(scale,)*3
s.render.engine='CYCLES';s.cycles.samples=4;s.cycles.use_denoising=True;s.cycles.max_bounces=3;s.render.threads_mode='FIXED';s.render.threads=6;s.render.resolution_x=800;s.render.resolution_y=800;s.render.resolution_percentage=100;s.render.image_settings.file_format='PNG';s.view_settings.view_transform='AgX';s.world.use_nodes=True;s.world.node_tree.nodes.get('Background').inputs[0].default_value=(.1,.12,.15,1);s.world.node_tree.nodes.get('Background').inputs[1].default_value=.45
bpy.ops.mesh.primitive_plane_add(size=200,location=(0,0,-1.03));floor=bpy.context.object;mat=bpy.data.materials.new('Review floor');mat.use_nodes=True;mat.node_tree.nodes.get('Principled BSDF').inputs['Base Color'].default_value=(.15,.17,.2,1);floor.data.materials.append(mat)
for name,loc,power,size in [('Key',(3,-4,5),700,4),('Fill',(-4,-2,3),500,4),('Rim',(1,4,4),800,3)]:
 d=bpy.data.lights.new(name,'AREA');d.energy=power;d.size=size;o=bpy.data.objects.new(name,d);s.collection.objects.link(o);o.location=loc;o.rotation_euler=(-o.location).to_track_quat('-Z','Y').to_euler()
d=bpy.data.cameras.new('GeneratedObjectReview');o=bpy.data.objects.new('GeneratedObjectReview',d);s.collection.objects.link(o);s.camera=o;d.lens=45
# Same cameras across source objects; canonical glTF front is -Z => Blender +Y.
maxsize=max((hi-lo))*scale;distance=max(5,maxsize*2.2)
views=[(n,a) for n,a in [('front',0),('three-quarter',45),('back',180),('side',90)] if not os.getenv('LF_REVIEW_VIEWS') or n in os.environ['LF_REVIEW_VIEWS'].split(',')]
for name,a in views:
 a=math.radians(a+float(os.getenv('LF_REVIEW_ANGLE_OFFSET','0')));o.location=(math.sin(a)*distance,math.cos(a)*distance,2.2);o.rotation_euler=(Vector((0,0,0))-o.location).to_track_quat('-Z','Y').to_euler();s.render.filepath=str(out/(name+'.png'));bpy.ops.render.render(write_still=True);print('VIEW_DONE',name,flush=True)
assert hashlib.sha256(source.read_bytes()).hexdigest()==before;(out/'manifest.json').write_text(json.dumps({'source':str(source),'SHA256':before,'normalizationUniformScale':scale,'sourceBlenderBounds':[list(lo),list(hi)],'views':[n for n,a in views],'scope':'Actual generated geometry/material render, no manual substitute, unedited original source.'},indent=2));print('AGENT_RESULT object review complete',flush=True)

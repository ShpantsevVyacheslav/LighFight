import bpy,json,math,hashlib
from pathlib import Path
from mathutils import Vector
SRC=Path('/Users/macbook1/Documents/ChatGPT/LighFight/art/source/kitchen-tripo-v1/original-v2.glb')
OUT=Path('/Users/macbook1/Documents/ChatGPT/LighFight/art/previews/kitchen-tripo-v1-v2')
bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False)
bpy.ops.import_scene.gltf(filepath=str(SRC));bpy.context.view_layer.update()
meshes=[o for o in bpy.context.scene.objects if o.type=='MESH']
pts=[o.matrix_world@Vector(c) for o in meshes for c in o.bound_box]
lo=Vector(tuple(min(p[i] for p in pts) for i in range(3)));hi=Vector(tuple(max(p[i] for p in pts) for i in range(3)));center=(lo+hi)/2;dims=hi-lo
stats=[]
for o in meshes:
 o.data.calc_loop_triangles();stats.append({'name':o.name,'vertices':len(o.data.vertices),'polygons':len(o.data.polygons),'triangles':len(o.data.loop_triangles),'materials':[m.name if m else None for m in o.data.materials]})
report={'blender':bpy.app.version_string,'source_sha256':hashlib.sha256(SRC.read_bytes()).hexdigest(),'bounds_min':list(lo),'bounds_max':list(hi),'dimensions':list(dims),'meshes':stats,'vertices':sum(s['vertices'] for s in stats),'triangles':sum(s['triangles'] for s in stats),'images':[{'name':i.name,'size':list(i.size)} for i in bpy.data.images]}
(OUT/'audit.json').write_text(json.dumps(report,indent=2))
scene=bpy.context.scene;scene.render.engine='CYCLES';scene.cycles.device='CPU';scene.cycles.samples=2;scene.cycles.use_denoising=False;scene.cycles.max_bounces=1;scene.render.threads_mode='FIXED';scene.render.threads=4
scene.render.resolution_x=640;scene.render.resolution_y=480;scene.render.resolution_percentage=100
scene.world.color=(.08,.08,.08);scene.view_settings.view_transform='AgX'
# White clay makes untextured geometry legible without altering the source GLB.
mat=bpy.data.materials.new('Review clay');mat.diffuse_color=(.65,.68,.72,1);mat.use_nodes=True;mat.node_tree.nodes.get('Principled BSDF').inputs['Base Color'].default_value=(.65,.68,.72,1);mat.node_tree.nodes.get('Principled BSDF').inputs['Roughness'].default_value=.8
for o in meshes:o.data.materials.clear();o.data.materials.append(mat)
span=max(dims)
for name,offset,power,size in [('Key',(-.8,-1,1.5),60,1),('Fill',(1,-.4,.9),40,1),('Back',(0,1,1.5),60,1)]:
 light=bpy.data.lights.new(name,'AREA');light.energy=power;light.shape='DISK';light.size=span*size
 ob=bpy.data.objects.new(name,light);scene.collection.objects.link(ob);ob.location=center+Vector(offset)*span;ob.rotation_euler=(center-ob.location).to_track_quat('-Z','Y').to_euler()
cam=bpy.data.cameras.new('Review camera');ob=bpy.data.objects.new('Review camera',cam);scene.collection.objects.link(ob);scene.camera=ob;cam.type='ORTHO';cam.ortho_scale=span*1.3
for name,offset in [('front',(0,-1.8,.7)),('three-quarter',(1.5,-1.7,1.15)),('top',(0,0,2.8))]:
 ob.location=center+Vector(offset)*span;ob.rotation_euler=(center-ob.location).to_track_quat('-Z','Y').to_euler();scene.render.filepath=str(OUT/(name+'.png'));bpy.ops.render.render(write_still=True)
print('AGENT_RESULT '+json.dumps(report))

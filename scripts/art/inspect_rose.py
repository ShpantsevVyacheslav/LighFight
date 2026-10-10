import bpy,json
from pathlib import Path
from mathutils import Vector
root=Path('/Users/macbook1/Documents/ChatGPT/LighFight');out=root/'art/previews/rose-premium'
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.gltf(filepath=str(root/'art/source/rose-premium/model.glb'))
objects=[o for o in bpy.context.scene.objects if o.type=='MESH'];pts=[o.matrix_world@Vector(p) for o in objects for p in o.bound_box];lo=Vector([min(v[i] for v in pts) for i in range(3)]);hi=Vector([max(v[i] for v in pts) for i in range(3)]);center=(lo+hi)/2;size=max(hi-lo)
tris=sum(len(p.vertices)-2 for o in objects for p in o.data.polygons)
(root/'art/source/rose-premium/geometry-audit.json').write_text(json.dumps({'triangles':tris,'meshes':len(objects),'bounds':list(hi-lo),'rig_limit_300000':tris<=300000},indent=2))
scene=bpy.context.scene;scene.world=bpy.data.worlds.new('Studio');scene.world.color=(.25,.25,.25)
bpy.ops.object.light_add(type='AREA',location=center+Vector((0,-size,size)));bpy.context.object.data.energy=1500;bpy.context.object.data.size=size*2
bpy.ops.object.camera_add();cam=bpy.context.object;scene.camera=cam;cam.data.type='ORTHO';cam.data.ortho_scale=size*1.2
scene.render.engine='CYCLES';scene.cycles.samples=24;scene.render.resolution_x=1000;scene.render.resolution_y=1000;scene.render.resolution_percentage=100
for name,direction in [('front',(0,-1,.08)),('back',(0,1,.08)),('oblique',(1,-1,.1))]:
 cam.location=center+Vector(direction)*size*2;cam.rotation_euler=(center-cam.location).to_track_quat('-Z','Y').to_euler();scene.render.filepath=str(out/(name+'.png'));bpy.ops.render.render(write_still=True)
bpy.ops.wm.save_as_mainfile(filepath=str(root/'art/source/rose-premium/review.blend'))

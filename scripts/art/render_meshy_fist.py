import bpy,bmesh,sys,json,math
from pathlib import Path
from mathutils import Vector
src,out=map(Path,sys.argv[sys.argv.index('--')+1:]);out.mkdir(parents=True,exist_ok=True);bpy.ops.wm.read_factory_settings(use_empty=True);bpy.ops.import_scene.fbx(filepath=str(src));meshes=[o for o in bpy.context.scene.objects if o.type=='MESH'];points=[o.matrix_world@v.co for o in meshes for v in o.data.vertices];low=Vector(tuple(min(p[i] for p in points) for i in range(3)));high=Vector(tuple(max(p[i] for p in points) for i in range(3)));center=(low+high)/2;size=max(high-low)
mat=bpy.data.materials.new('NeutralGeometry');mat.diffuse_color=(.53,.55,.58,1);mat.use_nodes=True;mat.node_tree.nodes['Principled BSDF'].inputs['Base Color'].default_value=(.53,.55,.58,1);mat.node_tree.nodes['Principled BSDF'].inputs['Roughness'].default_value=.65
report=[]
for o in meshes:
 o.data.materials.clear();o.data.materials.append(mat)
 for p in o.data.polygons:p.use_smooth=True
 bm=bmesh.new();bm.from_mesh(o.data);report.append({'name':o.name,'vertices':len(bm.verts),'faces':len(bm.faces),'boundaryEdges':sum(e.is_boundary for e in bm.edges),'nonManifoldEdges':sum(not e.is_manifold for e in bm.edges)});bm.free()
scene=bpy.context.scene;scene.render.engine='CYCLES';scene.cycles.samples=16;scene.render.resolution_x=800;scene.render.resolution_y=800;scene.render.resolution_percentage=100;scene.world=bpy.data.worlds.new('Studio');scene.world.use_nodes=True;scene.world.node_tree.nodes['Background'].inputs[0].default_value=(.15,.18,.22,1);scene.world.node_tree.nodes['Background'].inputs[1].default_value=.45
bpy.ops.object.camera_add();cam=bpy.context.object;scene.camera=cam;cam.data.type='ORTHO';cam.data.ortho_scale=size*1.25
for offset,power in [((2,-3,4),1000),((-3,1,2),650)]:
 bpy.ops.object.light_add(type='AREA',location=center+Vector(offset)*size);light=bpy.context.object;light.data.energy=power*size*size;light.data.shape='DISK';light.data.size=size*3;light.rotation_euler=(center-light.location).to_track_quat('-Z','Y').to_euler()
(out/'geometry-audit.json').write_text(json.dumps({'meshes':report,'boundsMin':list(low),'boundsMax':list(high)},indent=2));bpy.ops.wm.save_as_mainfile(filepath=str(out/'fist-review.blend'))
for name,direction in [('front',(0,-1,.2)),('back',(0,1,.2)),('left',(-1,0,.15)),('right',(1,0,.15))]:
 cam.location=center+Vector(direction).normalized()*size*3;cam.rotation_euler=(center-cam.location).to_track_quat('-Z','Y').to_euler();scene.render.filepath=str(out/(name+'.png'));bpy.ops.render.render(write_still=True)

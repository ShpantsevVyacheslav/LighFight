"""Replace the removed source watch with a rigid skinned band and rounded case."""
import bpy,bmesh,sys,math,json
from pathlib import Path
from mathutils import Vector
src,out=map(Path,sys.argv[sys.argv.index('--')+1:]);out.mkdir(parents=True,exist_ok=True)
bpy.ops.wm.open_mainfile(filepath=str(src));scene=bpy.context.scene
body=bpy.data.objects['char1'];arm=next(o for o in scene.objects if o.type=='ARMATURE')
material=bpy.data.materials.new('RigidWatch');material.use_nodes=True
bs=material.node_tree.nodes.get('Principled BSDF');bs.inputs['Base Color'].default_value=(.018,.023,.03,1);bs.inputs['Metallic'].default_value=.25;bs.inputs['Roughness'].default_value=.28
probe=bmesh.new();probe.from_mesh(body.data)
bmesh.ops.bisect_plane(probe,geom=list(probe.verts)+list(probe.edges)+list(probe.faces),dist=.00001,plane_co=(66.5,0,0),plane_no=(1,0,0))
ring=[v.co.copy() for v in probe.verts if abs(v.co.x-66.5)<.001];probe.free()
center=Vector((66.5,(min(p.y for p in ring)+max(p.y for p in ring))/2,(min(p.z for p in ring)+max(p.z for p in ring))/2))
radius_y=(max(p.y for p in ring)-min(p.y for p in ring))/2+.18;radius_z=(max(p.z for p in ring)-min(p.z for p in ring))/2+.18
points=[];faces=[];segments=64
for x,outer in [(-1.35,True),(1.35,True),(-1.35,False),(1.35,False)]:
 for j in range(segments):
  a=math.tau*j/segments;ry=radius_y+.18 if outer else radius_y;rz=radius_z+.18 if outer else radius_z
  points.append(center+Vector((x,math.cos(a)*ry,math.sin(a)*rz)))
for j in range(segments):
 k=(j+1)%segments
 for a,b in [(0,1),(1,3),(3,2),(2,0)]:faces.append((a*segments+j,a*segments+k,b*segments+k,b*segments+j))
mesh=bpy.data.meshes.new('RigidWatchBand');mesh.from_pydata(points,[],faces);mesh.update();band=bpy.data.objects.new('RigidWatchBand',mesh);scene.collection.objects.link(band);objects=[band]
for name,location,dimensions in [('RigidWatchCase',(center.x,center.y,center.z+radius_z+.30),(3.1,3.8,.7)),('RigidWatchScreen',(center.x,center.y,center.z+radius_z+.68),(2.55,3.2,.07))]:
 bpy.ops.mesh.primitive_cube_add(size=1,location=location);o=bpy.context.object;o.name=name;o.dimensions=dimensions;bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
 bevel=o.modifiers.new('ManufacturedRoundedEdges','BEVEL');bevel.width=.28 if name.endswith('Case') else .12;bevel.segments=5;bpy.context.view_layer.objects.active=o;bpy.ops.object.modifier_apply(modifier=bevel.name)
 # Mesh data needs the same centimeter coordinate system as the character.
 for v in o.data.vertices:v.co+=o.location
 o.location=(0,0,0);objects.append(o)
for o in objects:
 o.matrix_world=body.matrix_world.copy();o.data.materials.append(material)
 for face in o.data.polygons:face.use_smooth=True
 group=o.vertex_groups.new(name='LeftForeArm');group.add(list(range(len(o.data.vertices))),1,'REPLACE')
 for key in body.data.shape_keys.key_blocks:o.shape_key_add(name=key.name)
old_vertex_count=len(body.data.vertices)
bpy.ops.object.select_all(action='DESELECT');body.select_set(True)
for o in objects:o.select_set(True)
bpy.context.view_layer.objects.active=body;bpy.ops.object.join()
# Pairwise accessory distances must remain constant throughout the source jab.
watch_vertices=list(range(old_vertex_count,len(body.data.vertices)))
if len(watch_vertices)<20:raise RuntimeError('Missing rigid watch sample')
def rigidity_error():
 ref=None;maximum=0;ids=watch_vertices[:40]
 for frame in range(scene.frame_start,scene.frame_end+1):
  scene.frame_set(frame);bpy.context.view_layer.update();ev=body.evaluated_get(bpy.context.evaluated_depsgraph_get());mesh=ev.to_mesh()
  distances=[(mesh.vertices[i].co-mesh.vertices[ids[0]].co).length for i in ids]
  if ref is None:ref=distances
  maximum=max(maximum,max(abs(a-b) for a,b in zip(ref,distances)));ev.to_mesh_clear()
 return maximum
max_error=rigidity_error()
if max_error>.001:raise RuntimeError('Rigid watch changes dimensions')
vertex=watch_vertices[0];body.vertex_groups['LeftForeArm'].remove([vertex]);body.vertex_groups['LeftHand'].add([vertex],1,'REPLACE')
mutation_error=rigidity_error();body.vertex_groups['LeftHand'].remove([vertex]);body.vertex_groups['LeftForeArm'].add([vertex],1,'REPLACE')
if mutation_error<.01:raise RuntimeError('Watch audit missed wrong bone mutation')
report={'watchRigidDistanceMaxErrorCm':max_error,'wrongBoneMutationDistanceErrorCm':mutation_error,'watchWeightBone':'LeftForeArm','oldWatchRemovedByWristReplacement':True}
scene.frame_set(10);body.data.shape_keys.key_blocks['ClosedFists'].value=1;bpy.ops.wm.save_as_mainfile(filepath=str(out/'impact-fist-watch.blend'))
bpy.ops.object.select_all(action='DESELECT');body.select_set(True);arm.select_set(True)
bpy.ops.export_scene.fbx(filepath=str(out/'island-finger-candidate.fbx'),use_selection=True,use_mesh_modifiers=False,object_types={'MESH','ARMATURE'},add_leaf_bones=False,bake_anim=True,bake_anim_use_all_actions=False,bake_anim_use_nla_strips=False,bake_anim_simplify_factor=0,path_mode='STRIP')
focus=arm.matrix_world@arm.pose.bones['LeftHand'].head;cam=scene.camera;cam.location=focus+Vector((.35,-.75,.3));cam.rotation_euler=(focus-cam.location).to_track_quat('-Z','Y').to_euler();cam.data.ortho_scale=.36;scene.cycles.samples=16;scene.render.resolution_x=800;scene.render.resolution_y=600;scene.render.filepath=str(out/'jab-watch.png');bpy.ops.render.render(write_still=True)
(out/'watch-audit.json').write_text(json.dumps(report,indent=2));print(json.dumps(report))

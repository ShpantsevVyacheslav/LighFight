"""Use fresh provider rig on untouched original geometry; attach existing closed fists."""
import bpy,bmesh,json,sys
from pathlib import Path
from mathutils import Matrix,Vector
root=Path(__file__).resolve().parents[2];out=root/'art/source/original-island';bpy.ops.wm.read_factory_settings(use_empty=True);bpy.ops.import_scene.fbx(filepath=str(out/'meshy-rig/original-rig.fbx'));arm=next(o for o in bpy.data.objects if o.type=='ARMATURE');body=next(o for o in bpy.data.objects if o.type=='MESH');arm.animation_data_clear();arm.data.pose_position='REST';body.name='FreshOriginalBody';body.data.calc_loop_triangles();count=len(body.data.loop_triangles)
# Work in the armature coordinate system. FBX mesh local axes differ by 90 degrees.
# Preserve every world-space vertex while changing the coordinate basis.
bpy.context.view_layer.update()
world_before=[body.matrix_world@v.co for v in body.data.vertices]
conversion=arm.matrix_world.inverted()@body.matrix_world
for v in body.data.vertices:v.co=conversion@v.co
body.matrix_world=arm.matrix_world.copy()
bpy.context.view_layer.update()
assert max((body.matrix_world@v.co-p).length for v,p in zip(body.data.vertices,world_before))<1e-5
# Leaf-bone tails in provider FBX are display placeholders, not hand anatomy.
bpy.context.view_layer.objects.active=arm;arm.select_set(True);bpy.ops.object.mode_set(mode='EDIT')
for side in ['Left','Right']:
 hand=arm.data.edit_bones[side+'Hand'];forearm=arm.data.edit_bones[side+'ForeArm'];direction=(hand.head-forearm.head).normalized();hand.tail=hand.head+direction*10;hand.roll=0
bpy.ops.object.mode_set(mode='OBJECT')
protected=[v.co.copy() for v in body.data.vertices if abs(v.co.x)<30 or v.co.z>149]
# Import only the already reviewed closed fists from the previous combat model.
before=set(bpy.data.objects);bpy.ops.import_scene.fbx(filepath=str(root/'unity/Assets/LighFight/Art/Drafts/IslandCombat/island-motions.fbx'));added=set(bpy.data.objects)-before;oldarm=next(o for o in added if o.type=='ARMATURE');donor=next(o for o in added if o.type=='MESH');oldarm.animation_data_clear();oldarm.data.pose_position='REST'
for p in oldarm.pose.bones:p.matrix_basis=Matrix.Identity(4)
for key in donor.data.shape_keys.key_blocks:key.value=1 if 'ClosedFists' in key.name else 0
bpy.context.view_layer.update();donor.data=donor.evaluated_get(bpy.context.evaluated_depsgraph_get()).to_mesh().copy();donor.shape_key_clear();oldframes={s:oldarm.data.bones[s+'Hand'].matrix_local.copy() for s in ['Left','Right']};oldheads={s:oldarm.data.bones[s+'Hand'].head_local.copy() for s in ['Left','Right']}
keep=set()
for face in donor.data.polygons:
 if face.material_index==0 and (all(donor.data.vertices[i].co.x>oldheads['Left'].x-3.5 for i in face.vertices) or all(donor.data.vertices[i].co.x<oldheads['Right'].x+3.5 for i in face.vertices)):keep.update(face.vertices)
bm=bmesh.new();bm.from_mesh(donor.data);bmesh.ops.delete(bm,geom=[v for v in bm.verts if v.index not in keep],context='VERTS');bm.to_mesh(donor.data);bm.free()
for v in donor.data.vertices:
 side='Left' if v.co.x>0 else 'Right';v.co=arm.data.bones[side+'Hand'].matrix_local@oldframes[side].inverted()@v.co
for g in list(donor.vertex_groups):donor.vertex_groups.remove(g)
for side,sign in [('Left',1),('Right',-1)]:donor.vertex_groups.new(name=side+'Hand').add([v.index for v in donor.data.vertices if sign*v.co.x>0],1,'REPLACE')
for mod in donor.modifiers:
 if mod.type=='ARMATURE':mod.object=arm
donor.parent=arm;donor.matrix_parent_inverse=body.matrix_parent_inverse.copy();donor.matrix_world=body.matrix_world.copy()
bm=bmesh.new();bm.from_mesh(body.data)
for side,sign in [('Left',1),('Right',-1)]:
 b=arm.data.bones[side+'Hand'];direction=(b.tail_local-b.head_local).normalized();local={v for v in bm.verts if sign*v.co.x>30};edges=[e for e in bm.edges if all(v in local for v in e.verts)];faces=[f for f in bm.faces if all(v in local for v in f.verts)];bmesh.ops.bisect_plane(bm,geom=list(local)+edges+faces,plane_co=b.head_local+direction*.25,plane_no=direction,dist=1e-5,clear_outer=True)
bm.to_mesh(body.data);bm.free()
from mathutils.kdtree import KDTree
kd=KDTree(len(body.data.vertices))
for v in body.data.vertices:kd.insert(v.co,v.index)
kd.balance();error=max(kd.find(p)[2] for p in protected);assert error<.0001
# Keep the accurately placed Meshy skeleton, but independently solve weights on welded source geometry.
proxy=body.copy();proxy.data=body.data.copy();bpy.context.collection.objects.link(proxy)
for g in list(proxy.vertex_groups):proxy.vertex_groups.remove(g)
for mod in list(proxy.modifiers):proxy.modifiers.remove(mod)
bm=bmesh.new();bm.from_mesh(proxy.data);bmesh.ops.remove_doubles(bm,verts=list(bm.verts),dist=.002);bm.to_mesh(proxy.data);bm.free()
for n in ['head_end','headfront']:arm.data.bones[n].use_deform=False
bpy.ops.object.select_all(action='DESELECT');proxy.select_set(True);arm.select_set(True);bpy.context.view_layer.objects.active=arm;bpy.ops.object.parent_set(type='ARMATURE_AUTO')
kd=KDTree(len(proxy.data.vertices))
for v in proxy.data.vertices:kd.insert(v.co,v.index)
kd.balance()
for g in list(body.vertex_groups):body.vertex_groups.remove(g)
for g in proxy.vertex_groups:body.vertex_groups.new(name=g.name)
for v in body.data.vertices:
 _,idx,_=kd.find(v.co);weights=[(proxy.vertex_groups[g.group].name,g.weight) for g in proxy.data.vertices[idx].groups]
 if v.co.z>149:weights=[('Head',1)]
 assert weights,'Fresh heat solver left an unweighted vertex'
 weights=sorted(weights,key=lambda p:p[1],reverse=True)[:4];total=sum(w for n,w in weights)
 for n,w in weights:body.vertex_groups[n].add([v.index],w/total,'REPLACE')
bpy.data.objects.remove(proxy,do_unlink=True)
sys.path.insert(0,str(Path(__file__).parent));from original_torso_weights import apply
apply(body,arm)
# Existing finger UVs use actual skin pixels copied from the original forearm.
mat=donor.data.materials[0];mat.use_nodes=True;node=mat.node_tree.nodes.new('ShaderNodeTexImage');node.image=bpy.data.images.load(str(out/'hand-albedo.png'));bsdf=next(n for n in mat.node_tree.nodes if n.type=='BSDF_PRINCIPLED');mat.node_tree.links.new(node.outputs['Color'],bsdf.inputs['Base Color']);bsdf.inputs['Roughness'].default_value=.55
body.data.uv_layers[0].name='UVMap';donor.data.uv_layers[0].name='UVMap';bpy.ops.object.select_all(action='DESELECT');body.select_set(True);donor.select_set(True);bpy.context.view_layer.objects.active=body;bpy.ops.object.join();body.name='char1'
for o in list(added):
 if o!=donor and o.name in bpy.data.objects:bpy.data.objects.remove(o,do_unlink=True)
for image in bpy.data.images:
 if image.packed_file:
  image.filepath_raw=str(out/('provider-'+image.name.replace('/','_')+'.png'));image.file_format='PNG';image.save()
arm.data.pose_position='POSE';body.data.calc_loop_triangles();report={'sourceTriangles':count,'finalTriangles':len(body.data.loop_triangles),'weightSource':'fresh Meshy bone placement; welded-source heat weights, rigid head and retained fists','sourceBodyRemeshed':False,'maxProtectedGeometryErrorCm':error,'protectedSourceVertices':len(protected),'rigTask':json.loads((out/'meshy-rig-task.json').read_text())['taskId'],'status':'Provider-rig candidate, motion and Unity checks pending'};(out/'provider-audit.json').write_text(json.dumps(report,indent=2));bpy.ops.wm.save_as_mainfile(filepath=str(out/'provider-rigged.blend'));print(report)

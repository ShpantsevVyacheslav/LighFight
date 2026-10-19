"""Rig the first, user-downloaded A-pose source without remeshing body or copying old weights."""
import bpy,bmesh,json,math,os
from pathlib import Path
from mathutils import Vector,Matrix
root=Path(__file__).resolve().parents[2];out=root/'art/source/original-island';out.mkdir(parents=True,exist_ok=True)
source=root/'unity/Assets/LighFight/Art/Source/Island/Meshy_AI_Island_Pattern_Portra_1010053346_texture.fbx'
bpy.ops.wm.read_factory_settings(use_empty=True);bpy.ops.import_scene.fbx(filepath=str(root/'unity/Assets/LighFight/Art/Drafts/IslandCombat/island-motions.fbx'))
arm=next(o for o in bpy.data.objects if o.type=='ARMATURE');donor=next(o for o in bpy.data.objects if o.type=='MESH');arm.animation_data_clear()
for p in arm.pose.bones:p.matrix_basis=Matrix.Identity(4)
arm.data.pose_position='REST'
for key in donor.data.shape_keys.key_blocks:key.value=1 if 'ClosedFists' in key.name else 0
bpy.context.view_layer.update();frozen=donor.evaluated_get(bpy.context.evaluated_depsgraph_get()).to_mesh();donor.data=frozen.copy();donor.shape_key_clear();old_frames={s:arm.data.bones[s+'Hand'].matrix_local.copy() for s in ['Left','Right']};old_heads={s:arm.data.bones[s+'Hand'].head_local.copy() for s in ['Left','Right']}
# Retain only donor wrist/fist skin. The original source provides its own watch.
bm=bmesh.new();bm.from_mesh(donor.data);keep=set()
for p in donor.data.polygons:
 if p.material_index!=0:continue
 if all(donor.data.vertices[i].co.x>old_heads['Left'].x-3.5 for i in p.vertices) or all(donor.data.vertices[i].co.x<old_heads['Right'].x+3.5 for i in p.vertices):keep.update(p.vertices)
bmesh.ops.delete(bm,geom=[v for v in bm.verts if v.index not in keep],context='VERTS');bm.to_mesh(donor.data);bm.free()
# Move the existing compatible hierarchy to anatomical A-pose landmarks.
bpy.context.view_layer.objects.active=arm;arm.select_set(True);bpy.ops.object.mode_set(mode='EDIT');eb=arm.data.edit_bones
landmarks={'Hips':((0,0,86),(0,0,99)),'Spine02':((0,0,99),(0,0,115)),'Spine01':((0,0,115),(0,0,130)),'Spine':((0,0,130),(0,0,142)),'neck':((0,0,142),(0,0,149)),'Head':((0,0,149),(0,0,166)),'head_end':((0,0,166),(0,0,175)),'headfront':((0,-8,149),(0,-18,149))}
for side,sign in [('Left',1),('Right',-1)]:
 landmarks.update({side+'Shoulder':((sign*7,0,135),(sign*20,0,132)),side+'Arm':((sign*20,0,132),(sign*30,-4,108)),side+'ForeArm':((sign*30,-4,108),(sign*37,-7.2,84.6)),side+'Hand':((sign*37,-7.2,84.6),(sign*38,-7.2,74.6)),side+'UpLeg':((sign*11,0,86),(sign*13,0,48)),side+'Leg':((sign*13,0,48),(sign*14,0,10)),side+'Foot':((sign*14,0,10),(sign*14,-15,5)),side+'ToeBase':((sign*14,-15,5),(sign*14,-22,5))})
for name,(head,tail) in landmarks.items():eb[name].head=head;eb[name].tail=tail;eb[name].roll=0
bpy.ops.object.mode_set(mode='OBJECT')
for v in donor.data.vertices:
 side='Left' if v.co.x>0 else 'Right';v.co=arm.data.bones[side+'Hand'].matrix_local@old_frames[side].inverted()@v.co
for group in list(donor.vertex_groups):donor.vertex_groups.remove(group)
for side,sign in [('Left',1),('Right',-1)]:donor.vertex_groups.new(name=side+'Hand').add([v.index for v in donor.data.vertices if sign*v.co.x>0],1,'REPLACE')
before=set(bpy.data.objects);bpy.ops.import_scene.fbx(filepath=str(source));body=next(o for o in set(bpy.data.objects)-before if o.type=='MESH');body.name='OriginalIslandBody'
# Uniform metric normalization only; torso/head source coordinates remain unchanged.
points=[body.matrix_world@v.co for v in body.data.vertices];low=min(v.z for v in points);height=max(v.z for v in points)-low;centerx=(min(v.x for v in points)+max(v.x for v in points))/2;centery=(min(v.y for v in points)+max(v.y for v in points))/2
for v,p in zip(body.data.vertices,points):v.co=Vector(((p.x-centerx)*170/height,(p.y-centery)*170/height,(p.z-low)*170/height))
body.matrix_world=donor.matrix_world.copy();body.data.calc_loop_triangles();source_count=len(body.data.loop_triangles)
mat=body.data.materials[0];maps={}
for node in mat.node_tree.nodes:
 if node.type!='TEX_IMAGE' or not node.image:continue
 role=None
 for link in node.outputs['Color'].links:
  if link.to_socket.name=='Base Color':role='albedo'
  elif link.to_node.type=='NORMAL_MAP':role='normal'
  elif link.to_socket.name=='Roughness':role='roughness'
  elif link.to_socket.name=='Metallic':role='metallic'
 if role:node.image.filepath_raw=str(out/(role+'.png'));node.image.file_format='PNG';node.image.save();maps[role]=list(node.image.size)
protected=[v.co.copy() for v in body.data.vertices if abs(v.co.x)<30 or v.co.z>149]
# Cut only original palms, along the new wrist direction. Preserve body and source watch.
bm=bmesh.new();bm.from_mesh(body.data)
for side in ['Left','Right']:
 bone=arm.data.bones[side+'Hand'];direction=(bone.tail_local-bone.head_local).normalized();plane=bone.head_local+direction*.25
 sign=1 if side=='Left' else -1
 local_verts={v for v in bm.verts if os.environ.get('LIGHFIGHT_GLOBAL_WRIST_CUT') or sign*v.co.x>32}
 local_edges=[e for e in bm.edges if all(v in local_verts for v in e.verts)]
 local_faces=[f for f in bm.faces if all(v in local_verts for v in f.verts)]
 bmesh.ops.bisect_plane(bm,geom=list(local_verts)+local_edges+local_faces,dist=1e-5,plane_co=plane,plane_no=direction,clear_outer=True)
bm.to_mesh(body.data);bm.free()
from mathutils.kdtree import KDTree
check=KDTree(len(body.data.vertices))
for v in body.data.vertices:check.insert(v.co,v.index)
check.balance();max_error=max(check.find(p)[2] for p in protected);assert max_error<.0001,'Protected source geometry changed'
# Solve heat weights on a welded temporary proxy, so UV seams do not break diffusion.
proxy=body.copy();proxy.data=body.data.copy();bpy.context.collection.objects.link(proxy)
bm=bmesh.new();bm.from_mesh(proxy.data);
bmesh.ops.remove_doubles(bm,verts=list(bm.verts),dist=.002)
bm.to_mesh(proxy.data);bm.free()
for n in ['head_end','headfront']:arm.data.bones[n].use_deform=False
bpy.ops.object.select_all(action='DESELECT');proxy.select_set(True);arm.select_set(True);bpy.context.view_layer.objects.active=arm;bpy.ops.object.parent_set(type='ARMATURE_AUTO')
from mathutils.kdtree import KDTree
tree=KDTree(len(proxy.data.vertices))
for v in proxy.data.vertices:tree.insert(v.co,v.index)
tree.balance()
for g in proxy.vertex_groups:body.vertex_groups.new(name=g.name)
missing=0
for v in body.data.vertices:
 _,i,_=tree.find(v.co);weights=[(proxy.vertex_groups[g.group].name,g.weight) for g in proxy.data.vertices[i].groups]
 if v.co.z>149:weights=[('Head',1)]
 if not weights:
  missing+=1;continue
 weights=sorted(weights,key=lambda p:p[1],reverse=True)[:4];total=sum(w for n,w in weights)
 for n,w in weights:body.vertex_groups[n].add([v.index],w/total,'REPLACE')
if missing:raise RuntimeError('Unweighted welded source surface: '+str(missing))
bpy.data.objects.remove(proxy,do_unlink=True)
body.parent=arm;body.matrix_parent_inverse=arm.matrix_world.inverted();mod=body.modifiers.new('FreshSourceRig','ARMATURE');mod.object=arm
# Copy an actual forearm skin patch into the hand material rather than guessing its colour.
import numpy as np
sample=min(body.data.vertices,key=lambda v:(v.co-Vector((34,-12,94))).length_squared)
loop=next(l for l in body.data.loops if l.vertex_index==sample.index);uv=body.data.uv_layers.active.data[loop.index].uv
image=next(n.image for n in mat.node_tree.nodes if n.type=='TEX_IMAGE' and any(l.to_socket.name=='Base Color' for l in n.outputs['Color'].links))
w,h=image.size;pixels=np.array(image.pixels[:],dtype=np.float32).reshape(h,w,4);cx,cy=int(uv.x*w),int(uv.y*h);patch=pixels[max(0,cy-16):min(h,cy+16),max(0,cx-16):min(w,cx+16)].copy()
hand=bpy.data.images.new('Original forearm skin',width=patch.shape[1],height=patch.shape[0]);hand.pixels=patch.ravel();hand.filepath_raw=str(out/'hand-albedo.png');hand.file_format='PNG';hand.save()
handmat=donor.data.materials[0];handmat.use_nodes=True;bsdf=next(n for n in handmat.node_tree.nodes if n.type=='BSDF_PRINCIPLED');node=handmat.node_tree.nodes.new('ShaderNodeTexImage');node.image=hand;handmat.node_tree.links.new(node.outputs['Color'],bsdf.inputs['Base Color']);bsdf.inputs['Roughness'].default_value=.55
import sys
sys.path.insert(0,str(Path(__file__).parent))
from original_torso_weights import apply
apply(body)
# Keep hand UV channels consistent when joining the two materials.
body.data.uv_layers[0].name='UVMap';donor.data.uv_layers[0].name='UVMap'
bpy.ops.object.select_all(action='DESELECT');body.select_set(True);donor.select_set(True);bpy.context.view_layer.objects.active=body;bpy.ops.object.join();body.name='char1';arm.data.pose_position='POSE'
for p in arm.pose.bones:p.matrix_basis=Matrix.Identity(4)
body.data.calc_loop_triangles();report={'source':str(source.relative_to(root)),'sourceTriangles':source_count,'finalTriangles':len(body.data.loop_triangles),'maps':maps,'bones':len(arm.data.bones),'weightSource':'welded source proxy heat diffusion; rigid source head','oldCorrectives':False,'sourceBodyRemeshed':False,'unweighted':missing,'protectedSourceVertices':len(protected),'maxProtectedGeometryErrorCm':max_error,'status':'A-pose candidate; animations and seam verification pending'};(out/'audit.json').write_text(json.dumps(report,indent=2));bpy.ops.wm.save_as_mainfile(filepath=str(out/'rigged.blend'));print(report)

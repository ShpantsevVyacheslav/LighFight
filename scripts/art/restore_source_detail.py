"""Restore Meshy source body triangles/UVs, retaining current rig, wrist cuffs and fists.
No decimation. All outputs are candidates until Unity animation verification.
"""
import bpy,bmesh,json,sys
from pathlib import Path
from mathutils import Vector
from mathutils.bvhtree import BVHTree
from mathutils.geometry import barycentric_transform
root=Path(__file__).resolve().parents[2];kind=sys.argv[-1];island=kind=='island'
source=root/('.tools/meshy/island-premium/island-high.glb' if island else 'art/source/rose-premium-v2/model.glb')
game=root/('unity/Assets/LighFight/Art/Drafts/IslandCombat/island-motions.fbx' if island else 'unity/Assets/LighFight/Art/Drafts/Rose/rose-motions.fbx')
out=root/'unity/Assets/LighFight/Art/Drafts/RestoredDetail'/kind;out.mkdir(parents=True,exist_ok=True)
bpy.ops.wm.read_factory_settings(use_empty=True);bpy.ops.import_scene.fbx(filepath=str(game));old=next(o for o in bpy.data.objects if o.type=='MESH');arm=next(o for o in bpy.data.objects if o.type=='ARMATURE');arm.data.pose_position='REST'
old.data.calc_loop_triangles();ov=[v.co.copy() for v in old.data.vertices];tris=[tuple(t.vertices) for t in old.data.loop_triangles];tree=BVHTree.FromPolygons(ov,tris,all_triangles=True)
weights=[{old.vertex_groups[g.group].name:g.weight for g in v.groups} for v in old.data.vertices]
shapes={k.name:[v.co.copy()-b.co for v,b in zip(k.data,old.data.shape_keys.key_blocks[0].data)] for k in list(old.data.shape_keys.key_blocks)[1:]} if old.data.shape_keys else {}
old_name=old.name;before=set(bpy.data.objects);bpy.ops.import_scene.gltf(filepath=str(source));high=next(o for o in set(bpy.data.objects)-before if o.type=='MESH');high.name='RestoredBody';high.data.calc_loop_triangles();source_triangles=len(high.data.loop_triangles)
lo=min(v.co.z for v in high.data.vertices);hi=max(v.co.z for v in high.data.vertices);scale=170/(hi-lo)
for v in high.data.vertices:v.co=Vector((v.co.x*scale,v.co.y*scale,(v.co.z-lo)*scale))
high.matrix_world=old.matrix_world.copy()
# Keep all source texture maps on original UVs; export actual pixels, no rebake.
mat=high.data.materials[0];maps={}
for node in mat.node_tree.nodes:
 if node.type=='TEX_IMAGE' and node.image:
  image=node.image;role='normal' if image.colorspace_settings.name=='Non-Color' and any(l.to_node.type=='NORMAL_MAP' for l in node.outputs['Color'].links) else None
  if not role:
   for link in node.outputs['Color'].links:
    if link.to_socket.name=='Base Color':role='albedo'
    elif link.to_node.type in ['SEPARATE_COLOR','SEPRGB']:role='metallic-roughness'
  if role:
   image.filepath_raw=str(out/(role+'.png'));image.file_format='PNG';image.save();maps[role]=list(image.size)
# Original wrists are removed; the already tested cuff and closed fists stay.
cuts={'Left':63.8,'Right':arm.data.bones['RightHand'].head_local.x+3} if island else {'Left':.700*scale,'Right':-.700*scale}
bm=bmesh.new();bm.from_mesh(high.data)
for side,sign in [('Left',1),('Right',-1)]:bmesh.ops.bisect_plane(bm,geom=list(bm.verts)+list(bm.edges)+list(bm.faces),dist=1e-5,plane_co=(cuts[side]+sign*.25,0,0),plane_no=(sign,0,0),clear_outer=True)
bm.to_mesh(high.data);bm.free()
# Retain existing wrist geometry and all its shape keys on a separate renderer.
# Face selection avoids unstable shape-key bisect operators.
keep=set()
for p in old.data.polygons:
 if (island and p.material_index==1) or all(vx>cuts['Left']-.25 for vx in [ov[i].x for i in p.vertices]) or all(vx<cuts['Right']+.25 for vx in [ov[i].x for i in p.vertices]):keep.update(p.vertices)
# Delete body faces in bmesh, preserving the key layers on retained wrists.
bm=bmesh.new();bm.from_mesh(old.data);bmesh.ops.delete(bm,geom=[v for v in bm.verts if v.index not in keep],context='VERTS');bm.to_mesh(old.data);bm.free();old.name='RetainedFists'
for group in old.vertex_groups:high.vertex_groups.new(name=group.name)
interpolation=[];max_distance=0
for v in high.data.vertices:
 q,n,i,d=tree.find_nearest(v.co);max_distance=max(max_distance,d);ids=tris[i];a,b,c=[ov[k] for k in ids];factors=barycentric_transform(q,a,b,c,Vector((1,0,0)),Vector((0,1,0)),Vector((0,0,1)));factors=[max(0,f) for f in factors];total=sum(factors);factors=[f/total for f in factors];interpolation.append((ids,factors));result={}
 for k,f in zip(ids,factors):
  for name,w in weights[k].items():result[name]=result.get(name,0)+f*w
 ranked=sorted(result.items(),key=lambda p:p[1],reverse=True)[:4];total=sum(w for n,w in ranked)
 if total<.00001:raise RuntimeError('Unweighted source vertex')
 for name,w in ranked:high.vertex_groups[name].add([v.index],w/total,'REPLACE')
if shapes:
 high.shape_key_add(name='Basis')
 for name,offsets in shapes.items():
  key=high.shape_key_add(name=name)
  for v,(ids,fs) in zip(key.data,interpolation):v.co+=sum((offsets[i]*f for i,f in zip(ids,fs)),Vector())
# Original surface remains un-smoothed and un-decimated. Keep imported normals.
high.parent=arm;high.matrix_parent_inverse=old.matrix_parent_inverse.copy();high.matrix_world=old.matrix_world.copy();mod=high.modifiers.new('ExistingRig','ARMATURE');mod.object=arm
high.data.calc_loop_triangles();old.data.calc_loop_triangles();body_count=len(high.data.loop_triangles);fist_count=len(old.data.loop_triangles)
# One renderer preserves existing combat audits and corrective application.
bpy.ops.object.select_all(action='DESELECT');high.select_set(True);old.select_set(True);bpy.context.view_layer.objects.active=high;bpy.ops.object.join();high.name=old_name
arm.animation_data_clear();bpy.ops.object.select_all(action='DESELECT');high.select_set(True);arm.select_set(True);bpy.context.view_layer.objects.active=arm
work=root/'art/source/restored-detail'/kind;work.mkdir(parents=True,exist_ok=True);bpy.ops.wm.save_as_mainfile(filepath=str(work/'restored.blend'));bpy.ops.export_scene.fbx(filepath=str(out/'restored.fbx'),use_selection=True,add_leaf_bones=False,bake_anim=False,path_mode='AUTO')
high.data.calc_loop_triangles();report={'sourceTriangles':source_triangles,'restoredBodyTriangles':body_count,'retainedFistTriangles':fist_count,'maxTransferDistanceCm':max_distance,'maps':maps,'bones':len(arm.data.bones),'correctives':list(shapes),'cutsCm':cuts,'decimation':False,'status':'candidate, Unity checks pending'};(out/'audit.json').write_text(json.dumps(report,indent=2));print(report)

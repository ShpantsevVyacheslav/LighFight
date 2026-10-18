"""DEF-only uncompressed FBX + indexed source sidecars; no catalog promotion.
Run after visual gate. Source .blend/FBX unchanged. Animation files contain bones only.
"""
import bpy,json,math,hashlib,sys
from pathlib import Path
import numpy as np
from mathutils import Vector,Matrix,Quaternion
O=Path(__file__).resolve().parents[1]
s=(O/'tools/27_actual_arm_elevation.py').read_text();exec(compile(s[:s.index('# Corrected actual elevation fixture')],str(O/'tools/27_actual_arm_elevation.py'),'exec'))
P=O/'dense-def-fbx-packet-v1';P.mkdir(exist_ok=True);(P/'Textures').mkdir(exist_ok=True)
used={body.vertex_groups[g.group].name for v in body.data.vertices for g in v.groups if g.weight>0};assert all(rig.data.bones[n].use_deform for n in used)
# Only weighted deformation bones; parents skip nonexport controls, original matrix_local retained.
data=bpy.data.armatures.new('SailorDense_DEF');game=bpy.data.objects.new('SailorDense_DEF',data);scene.collection.objects.link(game);game.matrix_world=rig.matrix_world.copy();bpy.context.view_layer.objects.active=game;game.select_set(True);bpy.ops.object.mode_set(mode='EDIT')
for n in sorted(used):
 b=rig.data.bones[n];e=data.edit_bones.new(n);e.head=b.head_local;e.tail=b.tail_local;e.matrix=b.matrix_local.copy();e.use_deform=True
for n in sorted(used):
 p=rig.data.bones[n].parent
 while p and p.name not in used:p=p.parent
 if p:data.edit_bones[n].parent=data.edit_bones[p.name]
bpy.ops.object.mode_set(mode='OBJECT');names=[b.name for b in data.bones];order=sorted(names,key=lambda n:len(data.bones[n].parent_recursive));nameIndex={n:i for i,n in enumerate(names)}
copy=body.copy();copy.data=body.data;scene.collection.objects.link(copy);copy.name='Sailor_OriginalDense_GameLBS';world=copy.matrix_world.copy();copy.parent=game;copy.matrix_world=world
for mod in copy.modifiers:
 if mod.type=='ARMATURE':mod.object=game;mod.use_deform_preserve_volume=False
weights=np.zeros((N,4),np.float32);joints=np.zeros((N,4),np.int32)
for v in body.data.vertices:
 for k,g in enumerate(v.groups):weights[v.index,k]=g.weight;joints[v.index,k]=nameIndex[body.vertex_groups[g.group].name]
assert np.count_nonzero(weights.sum(1)==0)==0 and np.max(np.abs(weights.sum(1)-1))<1e-5
m=body.data;m.calc_loop_triangles();tri=np.empty(len(m.loop_triangles)*3,np.int32);m.loop_triangles.foreach_get('vertices',tri);uv=np.empty(len(m.loops)*2,np.float32);m.uv_layers.active.data.foreach_get('uv',uv);loopids=np.empty(len(m.loops),np.int32);m.loops.foreach_get('vertex_index',loopids)
np.savez(P/'original-indexed-attributes.npz',positionsBlenderLocal=raw,triangles=tri.reshape(-1,3),loopVertexIndices=loopids,loopUV=uv.reshape(-1,2),jointIndices=joints,weights=weights,DEFRestArmature=np.array([data.bones[n].matrix_local for n in names]),meshWorldBlender=np.array(body.matrix_world),armatureWorldBlender=np.array(game.matrix_world))
images=[]
for im in body.data.materials[0].node_tree.nodes:
 if im.type!='TEX_IMAGE' or not im.image:continue
 image=im.image
 if image.packed_file:blob=bytes(image.packed_file.data)
 else:blob=Path(bpy.path.abspath(image.filepath)).read_bytes()
 name=Path(image.filepath).name or image.name;dest=P/'Textures'/name;dest.write_bytes(blob);image.filepath=str(dest.resolve());images.append({'file':str(dest.relative_to(P)),'SHA256':hashlib.sha256(blob).hexdigest(),'bytes':len(blob)})
def select(objs):
 bpy.ops.object.select_all(action='DESELECT')
 for o in objs:o.select_set(True)
 bpy.context.view_layer.objects.active=game
opts=dict(use_selection=True,object_types={'ARMATURE','MESH'},use_mesh_modifiers=False,add_leaf_bones=False,use_armature_deform_only=True,axis_forward='-Z',axis_up='Y',apply_unit_scale=True,bake_space_transform=False,path_mode='RELATIVE',embed_textures=False)
select([game,copy]);bpy.ops.export_scene.fbx(filepath=str(P/'SailorOriginalDenseDEF.fbx'),bake_anim=False,**opts)
# Disable ONLY evaluation of owned scene mesh modifiers while baking skeleton curves.
modifierStates=[(mod,mod.show_viewport) for ob in scene.objects if ob.type=='MESH' for mod in ob.modifiers]
for mod,state in modifierStates:mod.show_viewport=False
fast=(O/'tools/05_full_motion_gallery.py').read_text();fast=fast[fast.index('def retarget('):fast.index('reports=[]')];a=fast.index(' deps=bpy.context.evaluated_depsgraph_get()');b=fast.index(' errors={}',a);fast=fast[:a]+' lo=np.zeros(3);hi=np.zeros(3)\n'+fast[b:];exec(compile(fast,'fast-retarget-no-mesh','exec'))
# Explicit phase samples include independent surface reference; no vertex-index equality assumed after FBX import.
ids=np.linspace(0,N-1,4096,dtype=int);references=[];fps=24;scene.render.fps=fps
for clipName in ['dance_01','box_02','front_kick_01']:
 clip=next(a for a in clips if clipName in a.name);duration=float((clip.frame_range[1]-clip.frame_range[0])/24);frames=int(round(duration*fps));action=bpy.data.actions.new('DerivedDense_'+clipName);game.animation_data_create();game.animation_data.action=action
 for frame in range(frames+1):
  phase=frame/frames;retarget(clipName,phase)
  if clipName=='dance_01':support(clipName,phase)
  mats={n:rig.pose.bones[n].matrix.copy() for n in names};scene.frame_set(frame)
  for n in order:
   pb=game.pose.bones[n];pb.rotation_mode='QUATERNION';par=data.bones[n].parent;basis=data.bones[n].convert_local_to_pose(mats[n],data.bones[n].matrix_local,parent_matrix=mats[par.name] if par else Matrix.Identity(4),parent_matrix_local=par.matrix_local if par else Matrix.Identity(4),invert=True);pb.matrix_basis=basis;pb.keyframe_insert('location',frame=frame);pb.keyframe_insert('rotation_quaternion',frame=frame);pb.keyframe_insert('scale',frame=frame)
  if frame in {0,round(frames*.25),round(frames*.5),round(frames*.75),frames}:
   # Old original rig produces authoritative full-density LBS witness in Blender world metres.
   M=np.array([rig.matrix_world@rig.pose.bones[n].matrix@rig.data.bones[n].matrix_local.inverted()@rig.matrix_world.inverted()@body.matrix_world for n in names]);p=np.concatenate([raw[ids],np.ones((len(ids),1))],axis=1);co=np.sum(np.einsum('nkij,nj->nki',M[joints[ids]],p)*weights[ids,:,None],axis=1)[:,:3];u=co[:,[0,2,1]].copy();u[:,2]*=-1;references.append({'clip':clipName,'phase':phase,'frame':frame,'positionsUnityYUp':u.tolist(),'boneWorldUnityYUp':[(np.array([[1,0,0,0],[0,0,1,0],[0,-1,0,0],[0,0,0,1]])@np.array(rig.matrix_world@rig.pose.bones[n].matrix)@np.array([[1,0,0,0],[0,0,-1,0],[0,1,0,0],[0,0,0,1]])).tolist() for n in names]})
  if frame%24==0:print('BAKE '+clipName+' '+str(frame)+'/'+str(frames),flush=True)
 scene.frame_start=0;scene.frame_end=frames;select([game]);bpy.ops.export_scene.fbx(filepath=str(P/(clipName+'-derived-DEF.fbx')),bake_anim=True,bake_anim_use_all_actions=False,bake_anim_use_nla_strips=False,bake_anim_simplify_factor=0.,bake_anim_step=1.,**opts);game.animation_data.action=None
(P/'pose-reference.json').write_text(json.dumps({'coordinateConvention':'Unity Y-up = Blender world (x,z,-y), metres; matrix B M B^-1','sampleOriginalVertexIndices':ids.tolist(),'boneNames':names,'references':references},separators=(',',':')))
for mod,state in modifierStates:mod.show_viewport=state
assert hash_arrays(body)==original and hash_arrays(copy)==original
report={'status':'EXPORTED_PENDING_UNITY_REIMPORT_PARITY_NOT_PROMOTED','sourceVertices':N,'sourceTriangles':1988914,'sourceAttributesExact':True,'boneNames':names,'DEFOnlyBoneCount':len(names),'maxInfluences':4,'unweighted':0,'imagesByteExact':images,'animationSampleRate':24,'derivedOnlyDanceCorrection':True,'otherMotionNote':'box/kick are fresh-rig retargeted reference clips without trapezius correction; no original source geometry or clips overwritten.','FBXMaySplitOrReorderVertices':True,'sourceHashes':original}
(P/'manifest.json').write_text(json.dumps(report,indent=2));print('RESULT '+json.dumps(report),flush=True)

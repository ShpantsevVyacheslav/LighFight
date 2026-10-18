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
modifierStates=[(mod,mod.show_viewport) for ob in scene.objects if ob.type=='MESH' for mod in ob.modifiers]
for mod,state in modifierStates:mod.show_viewport=False
fast=(O/'tools/05_full_motion_gallery.py').read_text();fast=fast[fast.index('def retarget('):fast.index('reports=[]')];a=fast.index(' deps=bpy.context.evaluated_depsgraph_get()');b=fast.index(' errors={}',a);fast=fast[:a]+' lo=np.zeros(3);hi=np.zeros(3)\n'+fast[b:];exec(compile(fast,'fast-retarget-no-mesh','exec'))

retarget('dance_01',.5);support('dance_01',.5);mats={n:rig.pose.bones[n].matrix.copy() for n in names};rows=[]
for n in order:
 pb=game.pose.bones[n];pb.rotation_mode='QUATERNION';par=data.bones[n].parent;basis=data.bones[n].convert_local_to_pose(mats[n],data.bones[n].matrix_local,parent_matrix=mats[par.name] if par else Matrix.Identity(4),parent_matrix_local=par.matrix_local if par else Matrix.Identity(4),invert=True);pb.matrix_basis=basis
bpy.context.view_layer.update()
for n in names:rows.append({'bone':n,'actualVsFrozenMatrixMax':float(np.abs(np.array(game.pose.bones[n].matrix)-np.array(mats[n])).max())})
frozen={n:np.array(mats[n]) for n in names};scene.frame_set(278);bpy.context.view_layer.update();sourceAfter={n:np.array(rig.pose.bones[n].matrix) for n in names};rows.sort(key=lambda x:x['actualVsFrozenMatrixMax'],reverse=True);report={'maxSourceMatrixChangeAfterFrameSet':max(float(np.abs(sourceAfter[n]-frozen[n]).max()) for n in names),'maxNewDEFMatrixError':max(x['actualVsFrozenMatrixMax'] for x in rows),'worst':rows[:20]};(P/'one-frame-basis-diagnostic.json').write_text(json.dumps(report,indent=2));print('RESULT '+json.dumps(report),flush=True)

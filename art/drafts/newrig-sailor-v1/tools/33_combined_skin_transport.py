"""Recover animation-only FBX absolute world poses; re-encode against authoritative skinned bind.
One combined skinned FBX, no Rigify replay. 24Hz review transport only.
"""
import bpy,json,math,time
from pathlib import Path
import numpy as np
from mathutils import Matrix
O=Path(__file__).resolve().parents[1];P=O/'dense-def-fbx-packet-v1';bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False);bpy.ops.import_scene.fbx(filepath=str(P/'SailorOriginalDenseDEF.fbx'),automatic_bone_orientation=False,use_anim=False);body=next(o for o in bpy.context.scene.objects if o.type=='MESH');rig=next(o for o in bpy.context.scene.objects if o.type=='ARMATURE');names=[b.name for b in rig.data.bones];states=[(m,m.show_viewport) for m in body.modifiers]
for m,state in states:m.show_viewport=False
newActions=[];diagnostics=[];start=time.monotonic();scene=bpy.context.scene;scene.render.fps=24
for clip in ['dance_01','box_02','front_kick_01']:
 before=set(bpy.data.objects);acts=set(bpy.data.actions);bpy.ops.import_scene.fbx(filepath=str(P/(clip+'-derived-DEF.fbx')),automatic_bone_orientation=False,use_anim=True);created=[o for o in bpy.data.objects if o not in before];animated=next(o for o in created if o.type=='ARMATURE');sourceAction=next(a for a in bpy.data.actions if a not in acts);first,last=sourceAction.frame_range;count=round(last-first)+1;values={n:np.empty((count,10),np.float32) for n in names};previous={};maxTRS=0.
 for i in range(count):
  scene.frame_set(int(first+i));bpy.context.view_layer.update();mats={n:rig.matrix_world.inverted()@animated.matrix_world@animated.pose.bones[n].matrix for n in names}
  for n in names:
   bone=rig.data.bones[n];par=bone.parent;basis=bone.convert_local_to_pose(mats[n],bone.matrix_local,parent_matrix=mats[par.name] if par else Matrix.Identity(4),parent_matrix_local=par.matrix_local if par else Matrix.Identity(4),invert=True);loc,q,scale=basis.decompose()
   if n in previous and previous[n].dot(q)<0:q.negate()
   previous[n]=q.copy();values[n][i]=[*loc,*q,*scale];reconstructed=Matrix.Translation(loc)@q.to_matrix().to_4x4()@Matrix.Diagonal((*scale,1.));maxTRS=max(maxTRS,float(np.abs(np.array(basis)-np.array(reconstructed)).max()))
  if i%120==0:print('RECOVER '+clip+' '+str(i)+'/'+str(count),flush=True)
 action=bpy.data.actions.new('Dense_'+clip+'_review24');newActions.append(action);frames=np.arange(count,dtype=np.float32)
 for n in names:
  for path,offset,width in [('location',0,3),('rotation_quaternion',3,4),('scale',7,3)]:
   for axis in range(width):
    fc=action.fcurves.new('pose.bones['+json.dumps(n)+'].'+path,index=axis,action_group=n);fc.keyframe_points.add(count);xy=np.column_stack((frames,values[n][:,offset+axis])).astype(np.float32);fc.keyframe_points.foreach_set('co',xy.ravel())
    for key in fc.keyframe_points:key.interpolation='LINEAR'
    fc.update()
 for obj in created:bpy.data.objects.remove(obj,do_unlink=True)
 bpy.data.actions.remove(sourceAction);diagnostics.append({'clip':clip,'frames':count,'durationSeconds':(count-1)/24,'maxBasisTRSReconstructionElementError':maxTRS})
# Static skin supplies the actual cluster/bind authority for every derived take.
rig.animation_data_clear()
for pb in rig.pose.bones:pb.rotation_mode='QUATERNION';pb.matrix_basis=Matrix.Identity(4)
for m,state in states:m.show_viewport=state
scene.frame_set(0);bpy.context.view_layer.update();rig.animation_data_create();rig.animation_data.action=newActions[0];bpy.ops.object.select_all(action='DESELECT');rig.select_set(True);body.select_set(True);bpy.context.view_layer.objects.active=rig
bpy.ops.export_scene.fbx(filepath=str(P/'SailorOriginalDenseDEF-CombinedReview24.fbx'),use_selection=True,object_types={'ARMATURE','MESH'},use_mesh_modifiers=False,add_leaf_bones=False,use_armature_deform_only=True,axis_forward='-Z',axis_up='Y',apply_unit_scale=True,bake_space_transform=False,path_mode='RELATIVE',embed_textures=False,bake_anim=True,bake_anim_use_all_actions=True,bake_anim_use_nla_strips=False,bake_anim_simplify_factor=0.,bake_anim_step=1.)
report={'status':'REVIEW_TRANSPORT_PENDING_REIMPORT','method':'Animation-only own absolute world poses converted to authoritative static skin bind, bulk local curves; combined skin carries clusters+bindpose','takes':diagnostics,'seconds':time.monotonic()-start,'offKeyOracle':'PENDING_96_OR_120_HZ'};(P/'combined-transport.json').write_text(json.dumps(report,indent=2));print('RESULT '+json.dumps(report),flush=True)

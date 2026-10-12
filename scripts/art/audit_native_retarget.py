import bpy,json,math
from pathlib import Path
from mathutils import Matrix,Vector
root=Path(__file__).resolve().parents[2]; out=root/'art/previews/source-quality'
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.fbx(filepath=str(root/'unity/Assets/LighFight/Art/Drafts/MeshyWebIsland/web-motions.fbx'))
source=next(o for o in bpy.data.objects if o.type=='ARMATURE');source_names={b.name.split(':')[-1]:b.name for b in source.data.bones}
action=next(a for a in bpy.data.actions if 'Right_Jab_from_Guard' in a.name)
def frame(arm,names):
 positions={n:arm.matrix_world@arm.data.bones[b].head_local for n,b in names.items()}
 up=(positions['Head']-positions['Hips']).normalized();right=(positions['RightArm']-positions['LeftArm']).normalized();forward=up.cross(right).normalized();right=forward.cross(up).normalized()
 return Matrix((right,forward,up)).transposed()
source_frame=frame(source,source_names);source_rest={n:(source.matrix_world@source.data.bones[b].matrix_local).to_quaternion() for n,b in source_names.items()}
source.animation_data_create();source.animation_data.action=action;source_samples=[]
for i in range(37):
 f=action.frame_range[0]+(action.frame_range[1]-action.frame_range[0])*i/36;bpy.context.scene.frame_set(int(f),subframe=f-int(f));bpy.context.view_layer.update()
 source_samples.append({n:(source.matrix_world@source.pose.bones[b].matrix).to_quaternion() for n,b in source_names.items()})
objects=set(bpy.data.objects);bpy.ops.import_scene.gltf(filepath=str(root/'art/source/island-web-visible/web-motions/right-jab.glb'))
target=next(o for o in set(bpy.data.objects)-objects if o.type=='ARMATURE');target_names={b.name.split(':')[-1]:b.name for b in target.data.bones};target_action=target.animation_data.action
align=frame(target,target_names)@source_frame.inverted();target_rest={n:(target.matrix_world@target.data.bones[b].matrix_local).to_quaternion() for n,b in target_names.items()};expected=[]
for i in range(37):
 f=target_action.frame_range[0]+(target_action.frame_range[1]-target_action.frame_range[0])*i/36;bpy.context.scene.frame_set(int(f),subframe=f-int(f));bpy.context.view_layer.update();h=target.matrix_world@target.pose.bones[target_names['Hips']].head
 expected.append({n:(target.matrix_world@target.pose.bones[b].head-h,(target.matrix_world@target.pose.bones[b].matrix).to_quaternion()) for n,b in target_names.items()})
target.animation_data_clear();arm_world_rot=target.matrix_world.to_quaternion();results=[]
for method in ['world_delta','local_delta']:
 errors=[];angles=[]
 for i,poses in enumerate(source_samples):
  for p in target.pose.bones:p.matrix_basis.identity()
  bpy.context.view_layer.update()
  for p in target.pose.bones:
   n=p.name.split(':')[-1]
   delta=(poses[n]@source_rest[n].inverted()).to_matrix();desired=(align@delta@align.inverted()).to_quaternion()@target_rest[n]
   if method=='local_delta':
    source_parent=source.data.bones[source_names[n]].parent
    if p.parent and source_parent:
     parent=source_parent.name.split(':')[-1];source_local=poses[parent].inverted()@poses[n];source_bind_local=source_rest[parent].inverted()@source_rest[n];target_bind_local=target_rest[parent].inverted()@target_rest[n]
     desired=(target.matrix_world@p.parent.matrix).to_quaternion()@target_bind_local@source_bind_local.inverted()@source_local
   desired_local=(arm_world_rot.inverted()@desired).to_matrix().to_4x4()
   if p.parent:
    head=p.parent.matrix@p.parent.bone.matrix_local.inverted()@p.bone.head_local
   else:head=p.bone.head_local.copy()
   desired_local.translation=head
   p.matrix_basis=p.bone.convert_local_to_pose(desired_local,p.bone.matrix_local,parent_matrix=p.parent.matrix if p.parent else Matrix.Identity(4),parent_matrix_local=p.parent.bone.matrix_local if p.parent else Matrix.Identity(4),invert=True)
   bpy.context.view_layer.update()
  h=target.matrix_world@target.pose.bones[target_names['Hips']].head
  for n,b in target_names.items():
   point=target.matrix_world@target.pose.bones[b].head-h;rot=(target.matrix_world@target.pose.bones[b].matrix).to_quaternion();errors.append((point-expected[i][n][0]).length);angle=rot.rotation_difference(expected[i][n][1]).angle;angles.append(min(angle,2*math.pi-angle))
 report=dict(sourceClip=action.name,referenceClip=target_action.name,samples=37,bones=len(target_names),maxHipsRelativeJointErrorMeters=max(errors),maxRotationErrorDegrees=math.degrees(max(angles)),passed=max(errors)<.001 and max(angles)<math.radians(.1),method=method)
 
 results.append(report)
(out/'native-retarget-cross-audit.json').write_text(json.dumps(results,indent=2));print(results)

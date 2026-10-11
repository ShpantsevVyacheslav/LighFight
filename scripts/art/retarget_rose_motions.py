"""Bake provider motion onto Rose's exact skeleton, preserving its bind pose and skin."""
import bpy,json,math,sys
from pathlib import Path
from mathutils import Matrix,Vector
root=Path('/Users/macbook1/Documents/ChatGPT/LighFight');folder=root/'.tools/meshy/motion-expansion';out=root/'unity/Assets/LighFight/Art/Drafts/Rose'
island='--island' in sys.argv
if island:
 bpy.ops.wm.read_factory_settings(use_empty=True);bpy.ops.import_scene.fbx(filepath=str(root/'unity/Assets/LighFight/Art/Drafts/IslandMeshyStaticFists/island-finger-candidate.fbx'));out=root/'unity/Assets/LighFight/Art/Drafts/IslandCombat';out.mkdir(parents=True,exist_ok=True)
else:bpy.ops.wm.open_mainfile(filepath=str(root/'art/source/rose-hands-v1/rigged.blend'))
scene=bpy.context.scene;scene.render.fps=60;target=next(o for o in scene.objects if o.type=='ARMATURE');bodies=[o for o in scene.objects if o.type=='MESH' and any(m.type=='ARMATURE' and m.object==target for m in o.modifiers)];target.animation_data_create()
for a in list(bpy.data.actions):bpy.data.actions.remove(a)
names={a['action_id']:a['key'] for a in json.loads((folder/'library.json').read_text())};tasks=json.loads((root/'art/source/motion-expansion/tasks.json').read_text());audit=[]
entries=[(str(t['action_id']),names[t['action_id']]) for t in tasks['animations']]+[(t['name'],t['name']) for t in tasks.get('custom_motions',[])]+([] if any(t['action_id']==689 for t in tasks['animations']) else [('Walk','Walk')])
for source_id,motion_name in entries:
 path=folder/(('191' if motion_name=='axeKick' else source_id)+'-animation_glb_url.glb')
 if not path.exists():continue
 before=set(bpy.data.objects);bpy.ops.import_scene.gltf(filepath=str(path));added=set(bpy.data.objects)-before;source=next(o for o in added if o.type=='ARMATURE');source_action=source.animation_data.action or next(a for a in bpy.data.actions if a.name.startswith(source.name))
 source.animation_data.action=source_action
 for track in source.animation_data.nla_tracks:track.mute=True
 action=bpy.data.actions.new(motion_name);target.animation_data.action=action;first,last=source_action.frame_range
 if motion_name=='axeKick':last=first+180
 for frame in range(math.ceil(first),math.floor(last)+1):
  scene.frame_set(math.ceil(first) if motion_name=='axeKick' else frame)
  for bone in sorted(target.pose.bones,key=lambda b:len(b.parent_recursive)):
   if bone.name not in source.pose.bones:continue
   incoming=source.pose.bones[bone.name];bone.rotation_mode='QUATERNION'
   if island:
    desired=incoming.matrix.to_quaternion()@incoming.bone.matrix_local.to_quaternion().inverted()@bone.bone.matrix_local.to_quaternion()
    if (bone.parent.name if bone.parent else None)!=(incoming.parent.name if incoming.parent else None):raise RuntimeError("Skeleton hierarchy mismatch: "+bone.name)
    parent_rotation=(incoming.parent.matrix.to_quaternion()@incoming.parent.bone.matrix_local.to_quaternion().inverted()) if incoming.parent else Matrix.Identity(4).to_quaternion()
    basis=parent_rotation@bone.bone.matrix_local.to_quaternion();bone.rotation_quaternion=basis.inverted()@desired
    bone.location=bone.bone.matrix_local.to_quaternion().inverted()@(incoming.bone.matrix_local.to_quaternion()@incoming.matrix_basis.translation)
   else:bone.rotation_quaternion=incoming.matrix_basis.to_quaternion();bone.location=incoming.matrix_basis.translation
  bpy.context.view_layer.update()
  if motion_name=='axeKick':
   phase=(frame-first)/(last-first);keys=[(0,0),(.1,15),(.25,85),(.4,160),(.55,90),(.67,35),(.85,0),(1,0)]
   a,b=next((a,b) for a,b in zip(keys,keys[1:]) if a[0]<=phase<=b[0]);t=(phase-a[0])/(b[0]-a[0]);t=t*t*(3-2*t);angle=math.radians(a[1]+(b[1]-a[1])*t)
   upper=target.pose.bones['RightUpLeg'];lower=target.pose.bones['RightLeg'];foot=target.pose.bones['RightFoot'];head=upper.matrix.translation.copy();direction=Vector((0,-math.sin(angle),-math.cos(angle)))
   for segment in [upper,lower,foot]:
    reference=((lower.bone.head_local-upper.bone.head_local) if segment==upper else (foot.bone.head_local-lower.bone.head_local)).normalized()
    rotation=reference.rotation_difference(direction)@segment.bone.matrix_local.to_quaternion();segment.matrix=Matrix.LocRotScale(head,rotation,Vector((1,1,1)));bpy.context.view_layer.update()
    if segment!=foot:head+=direction*((lower.bone.head_local-upper.bone.head_local).length if segment==upper else (foot.bone.head_local-lower.bone.head_local).length)
   bpy.context.view_layer.update()
  hip=target.pose.bones['Hips'];delta=hip.matrix.translation-hip.bone.head_local;delta.z=0;hip.location-=hip.bone.matrix_local.inverted().to_3x3()@delta
  for bone in target.pose.bones:
   bone.keyframe_insert(data_path='rotation_quaternion',frame=frame-math.ceil(first),group=bone.name);bone.keyframe_insert(data_path='location',frame=frame-math.ceil(first),group=bone.name)
 for obj in added:bpy.data.objects.remove(obj,do_unlink=True)
 bpy.data.actions.remove(source_action)
 audit.append({'source':source_id,'name':action.name,'seconds':(last-first)/60});print(action.name,flush=True)
target.animation_data.action=None
for bone in target.pose.bones:bone.matrix_basis=Matrix.Identity(4)
bpy.ops.object.select_all(action='DESELECT');target.select_set(True)
for body in bodies:body.select_set(True)
bpy.context.view_layer.objects.active=target
bpy.ops.export_scene.fbx(filepath=str(out/('island-motions.fbx' if island else 'rose-motions.fbx')),use_selection=True,add_leaf_bones=False,bake_anim=True,bake_anim_use_all_actions=True,bake_anim_use_nla_strips=False,bake_anim_simplify_factor=0,path_mode='COPY',embed_textures=False)
(root/('art/source/motion-expansion/island-baked-audit.json' if island else 'art/source/motion-expansion/rose-baked-audit.json')).write_text(json.dumps(audit,indent=2))

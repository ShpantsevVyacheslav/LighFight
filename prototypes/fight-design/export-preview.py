import bpy,math
from pathlib import Path
root=Path('/Users/macbook1/Documents/ChatGPT/LighFight');out=root/'prototypes/fight-design'
for kind,file in [('rose','Rose/rose-motions.fbx'),('island','IslandCombat/island-motions.fbx')]:
 bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False)
 for a in list(bpy.data.actions):bpy.data.actions.remove(a)
 bpy.ops.import_scene.fbx(filepath=str(root/'unity/Assets/LighFight/Art/Drafts'/file))
 rig=next(o for o in bpy.data.objects if o.type=='ARMATURE');jab=next(a for a in bpy.data.actions if 'Left_Jab_from_Guard' in a.name);rig.animation_data.action=jab
 start,end=jab.frame_range;bpy.context.scene.frame_set(round(start+(end-start)/6));bpy.context.view_layer.update()
 pose={b.name:(b.location.copy(),b.rotation_quaternion.copy(),b.scale.copy()) for b in rig.pose.bones}
 for a in list(bpy.data.actions):
  if a!=jab:bpy.data.actions.remove(a)
 rig.animation_data.action=None
 action=bpy.data.actions.new('Боевая стойка');rig.animation_data.action=action
 for f in range(0,121,10):
  for bone in rig.pose.bones:
   loc,rot,scale=pose[bone.name];bone.location=loc;bone.rotation_mode='QUATERNION';bone.rotation_quaternion=rot;bone.scale=scale
   if bone.name=='Hips':bone.location.z+=math.sin(f/120*math.tau)*.4
   bone.keyframe_insert('location',frame=f);bone.keyframe_insert('rotation_quaternion',frame=f);bone.keyframe_insert('scale',frame=f)
 bpy.context.scene.render.fps=60;bpy.context.scene.frame_start=0;bpy.context.scene.frame_end=120;bpy.context.scene.frame_set(0)
 # Explicit material images: FBX embeds no texture; reuse existing source skin.
 imagepath=root/('unity/Assets/LighFight/Art/Drafts/Rose/skin-albedo.png' if kind=='rose' else 'unity/Assets/LighFight/Art/Drafts/IslandMeshyStaticFists/detail-skin.png')
 for obj in bpy.data.objects:
  if obj.type=='MESH':
   for mat in obj.data.materials:
    if mat and ('skin' in mat.name.lower() or kind=='rose'):
     mat.use_nodes=True;bsdf=next(n for n in mat.node_tree.nodes if n.type=='BSDF_PRINCIPLED');tex=mat.node_tree.nodes.new('ShaderNodeTexImage');tex.image=bpy.data.images.load(str(imagepath),check_existing=True);mat.node_tree.links.new(tex.outputs['Color'],bsdf.inputs['Base Color']);bsdf.inputs['Roughness'].default_value=.7
 bpy.ops.export_scene.gltf(filepath=str(out/(kind+'-guard.glb')),export_format='GLB',export_animations=True,export_animation_mode='ACTIONS',export_frame_range=True)
 print('EXPORTED',kind,flush=True)

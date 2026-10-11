"""Strip repeated character geometry and retain each provider skeleton + motion in FBX."""
import bpy,json,struct
from pathlib import Path
root=Path('/Users/macbook1/Documents/ChatGPT/LighFight');source=root/'.tools/meshy/motion-expansion';dest=root/'unity/Assets/LighFight/Art/Motions/Meshy';dest.mkdir(parents=True,exist_ok=True)
tasks=json.loads((root/'art/source/motion-expansion/tasks.json').read_text());library=json.loads((source/'library.json').read_text());names={a['action_id']:a['key'] for a in library};report=[]
for t in tasks['animations']:
 p=source/(str(t['action_id'])+'-animation_glb_url.glb')
 if not p.exists() or struct.unpack('<III',p.read_bytes()[:12])[2]!=p.stat().st_size:continue
 name=names[t['action_id']];fbx=dest/(name+'.fbx')

 bpy.ops.wm.read_factory_settings(use_empty=True);bpy.ops.import_scene.gltf(filepath=str(p));arm=next(o for o in bpy.context.scene.objects if o.type=='ARMATURE')
 for o in list(bpy.context.scene.objects):
  if o!=arm:bpy.data.objects.remove(o,do_unlink=True)
 actions=list(bpy.data.actions)
 if len(actions)!=1:raise RuntimeError('Expected exactly one provider motion')
 action=actions[0]
 if t['action_id']==191:
  arm.animation_data_clear()
  from mathutils import Matrix
  for bone in arm.pose.bones:bone.matrix_basis=Matrix.Identity(4)
  bpy.ops.object.select_all(action='DESELECT');arm.select_set(True);bpy.context.view_layer.objects.active=arm
  bpy.ops.export_scene.fbx(filepath=str(dest/'source-reference.fbx'),use_selection=True,add_leaf_bones=False,bake_anim=False)
  arm.animation_data_create()
 factor=60/bpy.context.scene.render.fps
 for curve in action.fcurves:
  for key in curve.keyframe_points:
   key.co.x*=factor;key.handle_left.x*=factor;key.handle_right.x*=factor
 action.name=name;arm.animation_data_create();arm.animation_data.action=action
 for track in arm.animation_data.nla_tracks:arm.animation_data.nla_tracks.remove(track)
 bpy.context.scene.frame_start=int(action.frame_range[0]);bpy.context.scene.frame_end=int(action.frame_range[1]);bpy.context.scene.render.fps=60
 bpy.ops.object.select_all(action='DESELECT');arm.select_set(True);bpy.context.view_layer.objects.active=arm
 bpy.ops.export_scene.fbx(filepath=str(fbx),use_selection=True,add_leaf_bones=False,bake_anim=True,bake_anim_use_all_actions=False,bake_anim_use_nla_strips=False,bake_anim_force_startend_keying=True,bake_anim_simplify_factor=0)
 report.append({'action_id':t['action_id'],'clip':name,'frames':list(action.frame_range),'fbx':fbx.name})
(root/'art/source/motion-expansion/export-audit.json').write_text(json.dumps(report,indent=2))

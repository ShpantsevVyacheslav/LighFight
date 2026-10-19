import bpy,sys
from pathlib import Path
from mathutils import Matrix
root=Path(__file__).resolve().parents[2];sys.path.insert(0,str(Path(__file__).parent));from original_torso_weights import apply
for name in ['rigged','animated']:
 bpy.ops.wm.open_mainfile(filepath=str(root/'art/source/original-island'/(name+'.blend')));body=next(o for o in bpy.data.objects if o.type=='MESH');arm=next(o for o in bpy.data.objects if o.type=='ARMATURE');apply(body);arm.animation_data_create();arm.animation_data.action=None
 for bone in arm.pose.bones:bone.matrix_basis=Matrix.Identity(4)
 bpy.ops.wm.save_as_mainfile(filepath=str(root/'art/source/original-island'/(name+'.blend')))
 if name=='animated':
  bpy.ops.object.select_all(action='DESELECT');body.select_set(True);arm.select_set(True);bpy.context.view_layer.objects.active=arm;bpy.ops.export_scene.fbx(filepath=str(root/'unity/Assets/LighFight/Art/Drafts/OriginalIsland/island-motions.fbx'),use_selection=True,add_leaf_bones=False,bake_anim=True,bake_anim_use_all_actions=True,bake_anim_use_nla_strips=False,bake_anim_simplify_factor=0,path_mode='COPY',embed_textures=False)

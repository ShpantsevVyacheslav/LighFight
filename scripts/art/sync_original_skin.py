import bpy
from pathlib import Path
from mathutils import Matrix
root=Path(__file__).resolve().parents[2];folder=root/'art/source/original-island';bpy.ops.wm.open_mainfile(filepath=str(folder/'animated.blend'));body=next(o for o in bpy.data.objects if o.type=='MESH');arm=next(o for o in bpy.data.objects if o.type=='ARMATURE')
with bpy.data.libraries.load(str(folder/'rigged.blend'),link=False) as (src,dst):dst.objects=['char1']
source=dst.objects[0];body.data=source.data.copy()
for g in list(body.vertex_groups):body.vertex_groups.remove(g)
for g in source.vertex_groups:body.vertex_groups.new(name=g.name)

for v in source.data.vertices:
 for g in v.groups:body.vertex_groups[g.group].add([v.index],g.weight,'REPLACE')
bpy.data.objects.remove(source,do_unlink=True);arm.animation_data.action=None
for bone in arm.pose.bones:bone.matrix_basis=Matrix.Identity(4)
bpy.ops.wm.save_as_mainfile(filepath=str(folder/'animated.blend'));bpy.ops.object.select_all(action='DESELECT');body.select_set(True);arm.select_set(True);bpy.context.view_layer.objects.active=arm;bpy.ops.export_scene.fbx(filepath=str(root/'unity/Assets/LighFight/Art/Drafts/OriginalIsland/island-motions.fbx'),use_selection=True,add_leaf_bones=False,bake_anim=True,bake_anim_use_all_actions=True,bake_anim_use_nla_strips=False,bake_anim_simplify_factor=0,path_mode='COPY',embed_textures=False)

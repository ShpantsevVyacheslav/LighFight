"""Preserve the exact currently visible Meshy source for Unity comparison."""
import bpy
from pathlib import Path
root=Path(__file__).resolve().parents[2]
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.gltf(filepath=str(root/'art/source/island-web-visible/model.glb'))
meshes=[o for o in bpy.context.scene.objects if o.type=='MESH']
assert sum(len(o.data.polygons) for o in meshes)==103074
bpy.ops.object.select_all(action='SELECT')
bpy.ops.export_scene.fbx(filepath=str(root/'unity/Assets/LighFight/Art/Drafts/IslandWebVisible/source.fbx'),use_selection=True,add_leaf_bones=False,bake_anim=False,path_mode='AUTO')

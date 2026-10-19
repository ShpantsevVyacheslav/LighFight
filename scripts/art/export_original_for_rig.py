import bpy
from pathlib import Path
root=Path(__file__).resolve().parents[2];bpy.ops.wm.read_factory_settings(use_empty=True);bpy.ops.import_scene.fbx(filepath=str(root/'unity/Assets/LighFight/Art/Source/Island/Meshy_AI_Island_Pattern_Portra_1010053346_texture.fbx'));bpy.ops.export_scene.gltf(filepath=str(root/'art/source/original-island/original.glb'),export_format='GLB',export_animations=False)

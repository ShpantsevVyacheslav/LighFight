"""Export exact web rig + native clips to Unity FBX, with no geometry/weight edits."""
import bpy,json
from pathlib import Path
root=Path(__file__).resolve().parents[2];folder=root/'art/source/original-island';out=root/'unity/Assets/LighFight/Art/Drafts/MeshyWebIsland';out.mkdir(parents=True,exist_ok=True)
bpy.ops.wm.read_factory_settings(use_empty=True);bpy.ops.import_scene.gltf(filepath=str(folder/'web-native-pack.glb'));arm=next(o for o in bpy.data.objects if o.type=='ARMATURE');meshes=[o for o in bpy.context.scene.objects if o.type=='MESH' and any(m.type=='ARMATURE' for m in o.modifiers)];assert len(meshes)==1;assert len(meshes[0].data.polygons)==66364;arm.animation_data.action=None
for track in list(arm.animation_data.nla_tracks):arm.animation_data.nla_tracks.remove(track)
for bone in arm.pose.bones:bone.matrix_basis.identity()
for action in bpy.data.actions:action.use_fake_user=True
for image in bpy.data.images:
 if image.packed_file:image.filepath_raw=str(out/(image.name.replace('/','_')+'.png'));image.file_format='PNG';image.save()
bpy.ops.object.select_all(action='DESELECT');arm.select_set(True)
for mesh in meshes:mesh.select_set(True)
bpy.context.view_layer.objects.active=arm;bpy.ops.export_scene.fbx(filepath=str(out/'web-motions.fbx'),use_selection=True,add_leaf_bones=False,bake_anim=True,bake_anim_use_all_actions=True,bake_anim_use_nla_strips=False,bake_anim_simplify_factor=0,path_mode='COPY')
report={'bones':len(arm.data.bones),'triangles':sum(len(o.data.polygons) for o in meshes),'actions':[a.name for a in bpy.data.actions],'originalWeightsRetained':True};(folder/'web-unity-export-audit.json').write_text(json.dumps(report,indent=2));print(report)

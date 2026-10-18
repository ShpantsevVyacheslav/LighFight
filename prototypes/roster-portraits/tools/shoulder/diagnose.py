import bpy,json
from pathlib import Path
R=Path(__file__).resolve().parents[4];p=next((R/'art/source/tripo-striped-sailor-native-full-pack/unpacked').glob('*.fbx'));bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False);bpy.ops.import_scene.fbx(filepath=str(p),automatic_bone_orientation=False);arm=next(o for o in bpy.context.scene.objects if o.type=='ARMATURE');mesh=next(o for o in bpy.context.scene.objects if o.type=='MESH');arm.data.pose_position='REST';bpy.context.view_layer.update();r={'boneRest':{},'groups':{}}
for b in arm.data.bones:
 if any(x in b.name for x in ['Shoulder','UpperArm','UpperChest','Chest','Neck']):r['boneRest'][b.name]={'head':list(arm.matrix_world@b.head_local),'tail':list(arm.matrix_world@b.tail_local)}
for g in mesh.vertex_groups:
 weights=[x.weight for v in mesh.data.vertices for x in v.groups if x.group==g.index]
 if any(x in g.name for x in ['Shoulder','UpperArm','UpperChest','Chest','Neck']):r['groups'][g.name]={'vertices':len(weights),'over08':sum(x>.8 for x in weights),'mean':sum(weights)/max(1,len(weights))}
(R/'.tools/portrait-native-glb/shoulder-fix/diagnosis.json').write_text(json.dumps(r,indent=2));print('RESULT '+json.dumps(r))

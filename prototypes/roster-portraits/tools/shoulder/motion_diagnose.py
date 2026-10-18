import bpy,json,math
from pathlib import Path
from mathutils import Vector
R=Path(__file__).resolve().parents[4];source=next((R/'art/source/tripo-striped-sailor-native-full-pack/unpacked').glob('*.fbx'));bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False);bpy.ops.import_scene.fbx(filepath=str(source),automatic_bone_orientation=False);arm=next(o for o in bpy.context.scene.objects if o.type=='ARMATURE');arm.animation_data.use_nla=False;scene=bpy.context.scene;chest=arm.data.bones['UpperChest'];restRot=chest.matrix_local.to_3x3();records=[]
for a in bpy.data.actions:
 arm.animation_data.action=a;per=[]
 for frame in range(math.ceil(a.frame_range[0]),math.floor(a.frame_range[1])+1):
  scene.frame_set(frame);neutral=restRot@arm.pose.bones['UpperChest'].matrix.to_3x3().normalized().inverted();r={'frame':frame}
  for side in ['Left','Right']:
   for segment in ['Shoulder','UpperArm']:
    b=arm.pose.bones[side+'_'+segment];v=arm.matrix_world.to_3x3()@(neutral@(b.tail-b.head));r[side+segment+'Elevation']=math.degrees(math.atan2(v.z,math.hypot(v.x,v.y)));r[side+segment+'LocalTranslationM']=(arm.matrix_world.to_3x3()@b.location).length;r[side+segment+'ScaleDeviation']=max(abs(x-1) for x in b.scale)
  per.append(r)
 summary={'clip':a.name,'summary':{k:{'min':min(p[k] for p in per),'max':max(p[k] for p in per)} for k in per[0] if k!='frame'}}
 if 'dance' in a.name:summary['quarterFrames']=[per[round((len(per)-1)*p)] for p in [0,.25,.5,.75]]
 records.append(summary)
(R/'.tools/portrait-native-glb/shoulder-fix/motion-diagnosis.json').write_text(json.dumps(records,indent=2));print('RESULT '+json.dumps(records))

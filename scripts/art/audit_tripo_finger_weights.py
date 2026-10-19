"""Diagnose finger weights outside the hand region; read-only source rig audit."""
import bpy,json,sys,math
from pathlib import Path
from mathutils import Vector
root=Path(__file__).resolve().parents[2];folder=root/'art/source/island-tripo-v1/unity-pack';bpy.ops.wm.open_mainfile(filepath=str(folder/'native-review.blend'))
arm=next(o for o in bpy.context.scene.objects if o.type=='ARMATURE');arm.data.pose_position='REST';bpy.context.view_layer.update()
meshes=[o for o in bpy.context.scene.objects if o.type=='MESH' and len(o.data.vertices)>1000]
points=[o.matrix_world@v.co for o in meshes for v in o.data.vertices];height=max(v.z for v in points)-min(v.z for v in points)
wrists=[arm.matrix_world@arm.data.bones[n].head_local for n in ['Left_Hand','Right_Hand']]
hips=arm.matrix_world@arm.data.bones['Hips'].head_local;chest=arm.matrix_world@arm.data.bones['Chest'].head_local
weighted=[];torso=[];far=[]
for obj in meshes:
 names={g.index:g.name for g in obj.vertex_groups}
 for v in obj.data.vertices:
  weight=sum(g.weight for g in v.groups if any(k in names[g.group] for k in ['Thumb','Index','Middle','Ring','Pinky']))
  if weight<=.02:continue
  point=obj.matrix_world@v.co;distance=min((point-w).length for w in wrists);weighted.append(distance/height)
  if distance/height>.12:far.append({'vertex':v.index,'weight':weight,'distance_over_height':distance/height,'point':list(point)})
  if abs(point.x-hips.x)<height*.12 and hips.z+height*.02<point.z<chest.z-height*.02:torso.append({'vertex':v.index,'weight':weight,'point':list(point)})
weighted.sort();report={'finger_weighted_vertices':len(weighted),'distance_over_height_p95':weighted[int(len(weighted)*.95)] if weighted else 0,'distance_over_height_max':max(weighted,default=0),'finger_weight_on_central_torso':len(torso),'far_finger_weights':len(far),'torso_examples':torso[:12],'far_examples':far[:12],'geometry_modified':False}
(folder/'finger-weight-diagnostic.json').write_text(json.dumps(report,indent=2));print('FINGER_WEIGHTS '+json.dumps(report))

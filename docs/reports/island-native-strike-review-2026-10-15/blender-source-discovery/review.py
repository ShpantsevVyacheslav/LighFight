"""Read-only raw FBX bone discovery. Not Unity parity or full-surface acceptance."""
import bpy,sys,json,csv,hashlib,math
from pathlib import Path

def main():
 root=Path('/Users/macbook1/Documents/ChatGPT/LighFight')
 inp=json.loads((root/'.tools/tripo-humanoid-review-unity/IslandNativeStrikeReviewPacket/inputs.json').read_text())
 src=root/inp['source']['path'];before=hashlib.sha256(src.read_bytes()).hexdigest();assert before==inp['source']['sha256']
 out=root/'.tools/island-source-bone-review';bpy.ops.wm.read_factory_settings(use_empty=True)
 bpy.context.scene.render.threads_mode='FIXED';bpy.context.scene.render.threads=1
 bpy.ops.import_scene.fbx(filepath=str(src));arms=[o for o in bpy.data.objects if o.type=='ARMATURE'];assert len(arms)==1;arm=arms[0]
 assert len(arm.pose.bones)==67
 for o in bpy.data.objects:
  if o.type=='MESH':o.hide_viewport=True;o.hide_render=True
 arm.animation_data_create()
 for track in arm.animation_data.nla_tracks:track.mute=True
 actions={a.name.split('|')[-1]:a for a in bpy.data.actions};results=[]
 for spec in inp['takes']:
  name=spec['name'];a=actions[name];lo,hi=a.frame_range;assert abs(lo-spec['blenderStartFrame'])<.001 and abs(hi-spec['blenderEndFrame'])<.001
  arm.animation_data.action=a
  if hasattr(a,'slots') and a.slots:arm.animation_data.action_slot=a.slots[0]
  duration=spec['sourceEndSeconds'];samples=math.ceil(duration*240)
  with (out/(name+'.trajectory.csv')).open('w') as f:
   w=csv.writer(f);w.writerow(['seconds','joint','x','y','z'])
   for i in range(samples+1):
    t=min(i/240,duration);frame=lo+t*24;bpy.context.scene.frame_set(math.floor(frame),subframe=frame-math.floor(frame));bpy.context.view_layer.update()
    for b in arm.pose.bones:
     v=b.matrix.translation;assert all(math.isfinite(x) for x in v);w.writerow([format(t,'.9g'),b.name,*[format(x,'.9g') for x in v]])
  results.append({'clip':name,'frames':[lo,hi],'seconds':duration,'samples':samples+1})
 assert hashlib.sha256(src.read_bytes()).hexdigest()==before
 report={'status':'BLENDER_BONES_ONLY_NOT_UNITY_OR_SURFACE_ACCEPTANCE','sourceSHA256':before,'blender':bpy.app.version_string,'bones':67,'space':'Original imported Blender armature local XYZ; rotation-invariant reach discovery only. No heading/contact/floor inference.','retargeted':False,'sourceModified':False,'clips':results}
 (out/'source-audit.json').write_text(json.dumps(report,indent=2)+'\n');print('RESULT '+json.dumps(report))
main()

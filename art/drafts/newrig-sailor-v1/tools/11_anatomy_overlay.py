import bpy,json
from pathlib import Path
from mathutils import Vector
from bpy_extras.object_utils import world_to_camera_view
ROOT=Path(__file__).resolve().parents[4];O=ROOT/'art/drafts/newrig-sailor-v1'
s=(O/'tools/05_full_motion_gallery.py').read_text();exec(compile(s[:s.index('reports=[]')],str(O/'tools/05_full_motion_gallery.py'),'exec'))
for b in rig.data.bones:
 if b.use_deform:b.bbone_segments=1
retarget('dance_01',.75);center=rig.matrix_world@((rig.pose.bones['ORG-upper_arm.L'].head+rig.pose.bones['ORG-upper_arm.R'].head)*.5);center.z+=.04;cam.location=center+Vector((0,-2,.05));cam.rotation_euler=(center-cam.location).to_track_quat('-Z','Y').to_euler();cd.ortho_scale=.65;scene.render.resolution_x=768;scene.render.resolution_y=768;bpy.context.view_layer.update()
out={}
for side in ['L','R']:
 for part in ['shoulder','upper_arm','forearm','hand']:
  b=rig.pose.bones[f'ORG-{part}.{side}'];out[b.name]={}
  for name,p in [('head',b.head),('tail',b.tail)]:
   v=world_to_camera_view(scene,cam,rig.matrix_world@p);out[b.name][name]=[float(v.x*768),float((1-v.y)*768)]
(O/'corrective-v1/anatomy-overlay.json').write_text(json.dumps(out,indent=2));print('RESULT '+json.dumps(out),flush=True)

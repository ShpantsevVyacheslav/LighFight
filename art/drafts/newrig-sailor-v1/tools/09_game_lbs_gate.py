"""Explicit true-linear-skinning game variant; no BBone runtime promise."""
import bpy,json,hashlib
from pathlib import Path
import numpy as np
from mathutils import Vector
ROOT=Path(__file__).resolve().parents[4];O=ROOT/'art/drafts/newrig-sailor-v1';D=O/'game-lbs-v1';D.mkdir(exist_ok=True)
s=(O/'tools/05_full_motion_gallery.py').read_text();exec(compile(s[:s.index('reports=[]')],str(O/'tools/05_full_motion_gallery.py'),'exec'))
segmented={b.name:b.bbone_segments for b in rig.data.bones if b.use_deform and b.bbone_segments>1}
for b in rig.data.bones:
 if b.use_deform:b.bbone_segments=1
body.modifiers[0].use_deform_preserve_volume=False
checks=[]
for clip,phase in [('dance_01',.75),('front_kick_01',.45)]:
 r=retarget(clip,phase);ob=body.evaluated_get(bpy.context.evaluated_depsgraph_get());me=ob.to_mesh();ids=np.linspace(0,len(body.data.vertices)-1,12000,dtype=int);observed=np.array([list(ob.matrix_world@me.vertices[int(i)].co) for i in ids]);ob.to_mesh_clear()
 M={name:np.array(rig.matrix_world@rig.pose.bones[name].matrix@rig.data.bones[name].matrix_local.inverted()@rig.matrix_world.inverted()@body.matrix_world) for name in {g.name for g in body.vertex_groups}}
 expected=[]
 for i in ids:
  v=body.data.vertices[int(i)];p=np.r_[np.array(v.co),1];out=np.zeros(4)
  for g in v.groups:out+=M[body.vertex_groups[g.group].name]@p*g.weight
  expected.append(out[:3])
 error=np.linalg.norm(observed-np.array(expected),axis=1);assert error.max()<1e-5,error.max();checks.append({'clip':clip,'phase':phase,'sampleVertices':len(ids),'maxLinearSkinErrorM':float(error.max())})
r=retarget('dance_01',.75);target=rig.matrix_world@((rig.pose.bones['ORG-upper_arm.L'].head+rig.pose.bones['ORG-upper_arm.R'].head)*.5);target.z+=.025;cam.location=target+Vector((.8,-2,.14));cam.rotation_euler=(target-cam.location).to_track_quat('-Z','Y').to_euler();cd.ortho_scale=.64;scene.render.resolution_x=640;scene.render.resolution_y=640;scene.render.filepath=str(D/'dance75-shoulders-trueLBS.png');bpy.ops.render.render(write_still=True)
R.reset_pose(rig)
# Keep native donor as explicit reference in local review blend; excluded from game export.
bpy.context.preferences.filepaths.save_version=0;bpy.ops.wm.save_as_mainfile(filepath=str(O/'stage02-game-lbs-v1.blend'),compress=True)
report={'status':'TRUE_LBS_VARIANT_NOT_VISUAL_ACCEPTED','convertedSegmentedDEFBones':segmented,'remainingSegmentedDEF':0,'preserveVolume':False,'checks':checks,'baseGeometryAndWeightsChanged':False,'note':'Current gallery-v1 used BBones; this variant is the supported matrix-LBS baseline for corrective inversion/export.'};(D/'report.json').write_text(json.dumps(report,indent=2));print('RESULT '+json.dumps(report),flush=True)

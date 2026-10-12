"""Compare vendor FBX poses against a source-preserving assembled FBX.
Blender --background --python this.py -- vendor.fbx assembled.fbx report.json
Rejects mismatching core bone motion, rather than accepting matching clip names.
"""
import bpy, json, sys
from pathlib import Path

def sample(path):
 bpy.ops.wm.read_factory_settings(use_empty=True)
 bpy.ops.import_scene.fbx(filepath=str(path))
 arm=next(o for o in bpy.data.objects if o.type=='ARMATURE')
 actions=list(bpy.data.actions);actions=actions if len(actions)==1 else [a for a in actions if 'Wave_One_Hand' in a.name];assert len(actions)==1, 'Expected one explicit Wave motion: '+str([a.name for a in bpy.data.actions])
 action=actions[0];arm.animation_data_create();arm.animation_data.action=action
 lo,hi=action.frame_range;frames=[lo+(hi-lo)*i/32 for i in range(33)]
 result=[]
 for frame in frames:
  bpy.context.scene.frame_set(int(frame),subframe=frame-int(frame));bpy.context.view_layer.update()
  result.append({b.name.split(':')[-1]:list(sum((list(row) for row in arm.matrix_world@b.matrix),[])) for b in arm.pose.bones if not b.name.endswith('_end')})
 return result, float((hi-lo)/bpy.context.scene.render.fps)

a,b,out=map(Path,sys.argv[sys.argv.index('--')+1:]);source,duration=sample(a);assembled,actual=sample(b)
assert abs(duration-actual)<1e-5,'Clip duration changed'
errors=[]
for index,(left,right) in enumerate(zip(source,assembled)):
 assert left.keys()==right.keys(),'Core bone inventory changed'
 for name in left:
  errors.append((max(abs(x-y) for x,y in zip(left[name],right[name])),index,name))
worst=max(errors);report={'samples':33,'durationSeconds':duration,'maxWorldMatrixError':worst[0],'worstSample':worst[1],'worstBone':worst[2],'tolerance':0.0001,'passed':worst[0]<0.0001}
out.write_text(json.dumps(report,indent=2));print(report)
assert report['passed'],'Native pose parity failed'

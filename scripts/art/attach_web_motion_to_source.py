"""Attach a vendor clip to its unchanged source FBX after bind validation.
This is bone-name aliasing between the vendor's smartrig/Mixamo exports, not retargeting.
Blender --background --python this.py -- output.fbx
"""
import bpy,json,sys
from pathlib import Path
sys.path.insert(0,str(Path(__file__).resolve().parent))
from merge_visible_web_fbx import rig_signature,surface_snapshot
from native_fbx_compat import require_same_rig,check_round_trip
root=Path(__file__).resolve().parents[2]/'art/source/island-web-visible'
def canonical(arm):
 original=rig_signature(arm);result={}
 for name,(parent,matrix) in original.items():
  if name.endswith('_end'):continue
  key=name.split(':')[-1]
  assert key not in result,'Ambiguous bone aliases'
  result[key]=(parent.split(':')[-1] if parent else None,matrix)
 return result
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.fbx(filepath=str(root/'provider-web-rig/character.fbx'))
base=next(o for o in bpy.data.objects if o.type=='ARMATURE');meshes=[o for o in bpy.data.objects if o.type=='MESH' and len(o.data.vertices)>1000]
original=surface_snapshot(meshes);bind=canonical(base);names={b.name.split(':')[-1]:b.name for b in base.data.bones}
objects=set(bpy.data.objects);actions=set(bpy.data.actions)
clip_file=next((root/'web-greeting-fbx').glob('*.fbx'));bpy.ops.import_scene.fbx(filepath=str(clip_file))
added=set(bpy.data.objects)-objects;source=next(o for o in added if o.type=='ARMATURE')
# Observed vendor serialization difference: 1.48416e-5 matrix component.
require_same_rig(bind,canonical(source),tolerance=2e-5)
for obj in added:
 if obj.type=='MESH':
  for group in obj.vertex_groups:assert not group.name.endswith('_end'),'Export end bone has skin weights'
new_actions=set(bpy.data.actions)-actions;assert len(new_actions)==1
motion=next(iter(new_actions));motion.name='Wave_One_Hand';motion.use_fake_user=True
for curve in list(motion.fcurves):
 if not curve.data_path.startswith('pose.bones['):continue
 old=curve.data_path.split('"')[1];key=old.split(':')[-1]
 if key.endswith('_end'):motion.fcurves.remove(curve);continue
 assert key in names,'Unmapped animation bone'
 curve.data_path=curve.data_path.replace('"'+old+'"','"'+names[key]+'"')
for obj in added:bpy.data.objects.remove(obj,do_unlink=True)
base.animation_data_clear()
base.animation_data_create();base.animation_data.action=motion
for b in base.pose.bones:b.matrix_basis.identity()
bpy.context.scene.frame_start=int(motion.frame_range[0]);bpy.context.scene.frame_end=int(motion.frame_range[1])
bpy.ops.object.select_all(action='DESELECT');base.select_set(True)
for obj in meshes:obj.select_set(True)
bpy.context.view_layer.objects.active=base
target=Path(sys.argv[sys.argv.index('--')+1]);target.parent.mkdir(parents=True,exist_ok=True)
bpy.ops.export_scene.fbx(filepath=str(target),use_selection=True,add_leaf_bones=False,bake_anim=True,bake_anim_use_all_actions=False,bake_anim_use_nla_strips=False,bake_anim_simplify_factor=0)
bpy.ops.wm.read_factory_settings(use_empty=True);bpy.ops.import_scene.fbx(filepath=str(target))
result=surface_snapshot([o for o in bpy.data.objects if o.type=='MESH' and len(o.data.vertices)>1000])
report={'triangles':result['triangles'],'sourceVertices':len(original['positions']),'exportVertices':len(result['positions']),'clip':'Wave_One_Hand','boneAliasingOnly':True,'bindTolerance':2e-5,'animationPoseParityVerified':False}
report.update(check_round_trip(original,result));target.with_suffix('.audit.json').write_text(json.dumps(report,indent=2));print(report)

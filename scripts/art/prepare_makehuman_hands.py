"""Bake one Catmull-Clark level while retaining authored skin weights and closed shape."""
import bpy,sys
from pathlib import Path
src,out=map(Path,sys.argv[sys.argv.index('--')+1:]);bpy.ops.wm.open_mainfile(filepath=str(src))
arm=next(o for o in bpy.context.scene.objects if o.type=='ARMATURE');arm.data.pose_position='REST'
for hand in [bpy.data.objects['RebuiltLeftHand'],bpy.data.objects['RebuiltRightHand']]:
 sub=next(m for m in hand.modifiers if m.type=='SUBSURF');sub.levels=1
 deform=next(m for m in hand.modifiers if m.type=='ARMATURE');deform.show_viewport=False
 key=hand.data.shape_keys.key_blocks['ClosedFists'];key.value=0;bpy.context.view_layer.update()
 deps=bpy.context.evaluated_depsgraph_get();ev=hand.evaluated_get(deps);base=bpy.data.meshes.new_from_object(ev,preserve_all_data_layers=True,depsgraph=deps)
 key.value=1;bpy.context.view_layer.update();ev=hand.evaluated_get(bpy.context.evaluated_depsgraph_get());closed=ev.to_mesh();coords=[v.co.copy() for v in closed.vertices];ev.to_mesh_clear()
 if len(coords)!=len(base.vertices):raise RuntimeError('Subdivision changes key topology')
 hand.data=base;hand.modifiers.remove(sub);hand.shape_key_add(name='Basis');key=hand.shape_key_add(name='ClosedFists')
 for i,p in enumerate(coords):key.data[i].co=p
 deform.show_viewport=True;key.value=0
arm.data.pose_position='POSE';out.parent.mkdir(parents=True,exist_ok=True);bpy.ops.wm.save_as_mainfile(filepath=str(out))

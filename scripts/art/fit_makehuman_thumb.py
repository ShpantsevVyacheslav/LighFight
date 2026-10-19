"""Search authored thumb joint angles for a nonpenetrating closed pose.
Reports scoped thumb/finger overlaps; no source assets modified.
"""
import bpy,sys,math,json,itertools
from pathlib import Path
from mathutils import Matrix,Vector
from mathutils.bvhtree import BVHTree
src,out=map(Path,sys.argv[sys.argv.index('--')+1:]);bpy.ops.wm.open_mainfile(filepath=str(src));a=next(o for o in bpy.context.scene.objects if o.type=='ARMATURE');a.animation_data.action=None;a.data.pose_position='POSE'
for p in a.pose.bones:p.matrix_basis=Matrix.Identity(4)
h=bpy.data.objects['RebuiltLeftHand'];h.data.shape_keys.key_blocks['ClosedFists'].value=0
for m in h.modifiers:
 if m.type=='SUBSURF':m.show_viewport=True;m.levels=1
for digit in range(2,6):
 for j in range(3):
  p=a.pose.bones['LeftMH_finger'+str(digit)+'_'+str(j+1)];p.rotation_mode='XYZ';p.rotation_euler.x=math.radians(-[75,95,55][j])
bpy.context.view_layer.update();deps=bpy.context.evaluated_depsgraph_get();mesh=bpy.data.meshes.new_from_object(h.evaluated_get(deps),preserve_all_data_layers=True,depsgraph=deps);mesh.calc_loop_triangles();groups={g.index:g.name for g in h.vertex_groups};owners=[]
for v in mesh.vertices:
 choices=[(g.weight,groups[g.group]) for g in v.groups if 'MH_finger' in groups[g.group]];owners.append(max(choices)[1] if choices and max(choices)[0]>.35 else '')
thumb=[];fingers=[]
for t in mesh.loop_triangles:
 ids=list(t.vertices);labels=[owners[i] for i in ids]
 if all('finger1_' in s for s in labels):thumb.append(ids)
 elif all(s and 'finger1_' not in s for s in labels):fingers.append(ids)
w=a.data.bones['LeftHand'].head_local;target=w+Vector((7.7,1.3,-4.2));results=[]
for rootX,mcpX,rootZ,rootY,ipX in itertools.product([-65,-55,-45],[25,40,55],[70,80,90],[-60,-45,-30],[50,65,80]):
 mcpZ=0
 xs=[rootX,mcpX,ipX];zs=[rootZ,mcpZ,0]
 for j in range(3):
  p=a.pose.bones['LeftMH_finger1_'+str(j+1)];p.rotation_mode='XYZ';p.rotation_euler.x=math.radians(xs[j]);p.rotation_euler.z=math.radians(zs[j]);p.rotation_euler.y=math.radians(rootY if j==0 else 0)
 bpy.context.view_layer.update();ev=h.evaluated_get(bpy.context.evaluated_depsgraph_get());evaluated=ev.to_mesh();coords=[v.co.copy() for v in evaluated.vertices];ev.to_mesh_clear()
 b1=BVHTree.FromPolygons(coords,thumb,all_triangles=True,epsilon=0);b2=BVHTree.FromPolygons(coords,fingers,all_triangles=True,epsilon=0);count=len(b1.overlap(b2));tip=a.pose.bones['LeftMH_finger1_3'].tail
 score=count*100+(tip-target).length_squared+.0003*(mcpX-45)**2
 results.append({'x':xs,'y':[rootY,0,0],'z':zs,'overlapPairs':count,'tip':list(tip-w),'score':score})
results.sort(key=lambda r:r['score']);out.write_text(json.dumps(results,indent=2));print(json.dumps(results[:8]))

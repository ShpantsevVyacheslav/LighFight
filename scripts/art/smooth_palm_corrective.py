"""Smooth folded proximal hand surfaces in the closed corrective, retaining wrist and distal digits."""
import bpy,sys,json
from pathlib import Path
from mathutils import Vector
from mathutils.bvhtree import BVHTree
src,out,report_path=map(Path,sys.argv[sys.argv.index('--')+1:]);bpy.ops.wm.open_mainfile(filepath=str(src));arm=next(o for o in bpy.context.scene.objects if o.type=='ARMATURE');report={}
for side,sign in [('Left',1),('Right',-1)]:
 h=bpy.data.objects['Rebuilt'+side+'Hand'];m=h.data;m.calc_loop_triangles();w=arm.data.bones[side+'Hand'].head_local;neighbors=[set() for _ in m.vertices]
 for edge in m.edges:
  a,b=edge.vertices;neighbors[a].add(b);neighbors[b].add(a)
 triangles=[tuple(t.vertices) for t in m.loop_triangles];coords=[v.co.copy() for v in m.shape_keys.key_blocks['ClosedFists'].data]
 def overlaps(points):
  tree=BVHTree.FromPolygons(points,triangles,all_triangles=True)
  return sum(a<b and not(set(triangles[a])&set(triangles[b])) and all(sign*(m.vertices[i].co.x-w.x)<10 for i in triangles[a]+triangles[b]) for a,b in tree.overlap(tree))
 before=overlaps(coords);factors=[]
 for vertex in m.vertices:
  u=sign*(vertex.co.x-w.x);factor=min(1,max(0,u/2))*min(1,max(0,(12-u)/3));factors.append(factor*.45)
 progress=[]
 for iteration in range(120):
  updated=[point.lerp(sum((coords[j] for j in neighbors[i]),Vector())/len(neighbors[i]),factors[i]) if neighbors[i] else point.copy() for i,point in enumerate(coords)]
  coords=[point.lerp(sum((updated[j] for j in neighbors[i]),Vector())/len(neighbors[i]),-factors[i]*1.04) if neighbors[i] else point.copy() for i,point in enumerate(updated)]
  if iteration in [19,39,79,119]:progress.append([iteration+1,overlaps(coords)])
 for v,point in zip(m.shape_keys.key_blocks['ClosedFists'].data,coords):v.co=point
 report[side]={'before':before,'iterations':progress,'after':overlaps(coords)}
bpy.ops.wm.save_as_mainfile(filepath=str(out));report_path.write_text(json.dumps(report,indent=2));print(report)

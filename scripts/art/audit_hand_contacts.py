"""Detect nonadjacent thumb/finger triangle overlaps in the captured hand shape."""
import bpy,sys,json
from pathlib import Path
from mathutils.bvhtree import BVHTree
args=sys.argv[sys.argv.index('--')+1:];src,out=map(Path,args[:2]);expect_overlap='--expect-overlap' in args[2:];bpy.ops.wm.open_mainfile(filepath=str(src));report={}
for side in ['Left','Right']:
 h=bpy.data.objects['Rebuilt'+side+'Hand'];mesh=h.data;mesh.calc_loop_triangles();groups={g.index:g.name for g in h.vertex_groups}
 owner=[]
 for v in mesh.vertices:
  relevant=[(g.weight,groups[g.group]) for g in v.groups if 'MH_finger' in groups[g.group]]
  owner.append(max(relevant)[1] if relevant and max(relevant)[0]>.35 else '')
 thumb=[];fingers=[]
 for tri in mesh.loop_triangles:
  ids=list(tri.vertices);labels=[owner[i] for i in ids]
  if all('finger1_' in label for label in labels):thumb.append(ids)
  elif all(label and 'finger1_' not in label for label in labels):fingers.append(ids)
 def overlaps(coords):
  a=BVHTree.FromPolygons(coords,thumb,all_triangles=True,epsilon=0);b=BVHTree.FromPolygons(coords,fingers,all_triangles=True,epsilon=0)
  return [(i,j) for i,j in a.overlap(b) if not set(thumb[i])&set(fingers[j])]
 closed=[p.co.copy() for p in mesh.shape_keys.key_blocks['ClosedFists'].data];open_coords=[v.co.copy() for v in mesh.vertices]
 count=len(overlaps(closed));baseline=len(overlaps(open_coords))
 report[side]={'openThumbFingerTriangleOverlaps':baseline,'closedThumbFingerTriangleOverlaps':count,'thumbTriangles':len(thumb),'fingerTriangles':len(fingers),'scope':'base mesh fully digit-weighted faces; excludes palm/web and is not an all-surface collision proof'}
out.write_text(json.dumps(report,indent=2));print(json.dumps(report))

found=any(r['closedThumbFingerTriangleOverlaps'] or r['openThumbFingerTriangleOverlaps'] for r in report.values())
if found!=expect_overlap:raise RuntimeError('Thumb contact audit expectation failed')

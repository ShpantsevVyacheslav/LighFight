"""Validate thumb contacts after the export subdivision, using retained vertex weights."""
import bpy,sys,json
from pathlib import Path
from mathutils.bvhtree import BVHTree
src,out=map(Path,sys.argv[sys.argv.index('--')+1:]);bpy.ops.wm.open_mainfile(filepath=str(src));report={}
for side in ['Left','Right']:
 h=bpy.data.objects['Rebuilt'+side+'Hand'];m=h.data;m.calc_loop_triangles();groups={g.index:g.name for g in h.vertex_groups};owner=[]
 for v in m.vertices:
  weights=[(g.weight,groups[g.group]) for g in v.groups if 'MH_finger' in groups[g.group]]
  owner.append(max(weights)[1] if weights and max(weights)[0]>.35 else '')
 thumb=[];fingers=[]
 for t in m.loop_triangles:
  ids=list(t.vertices);labels=[owner[i] for i in ids]
  if all('finger1_' in x for x in labels):thumb.append(ids)
  elif all(x and 'finger1_' not in x for x in labels):fingers.append(ids)
 states={}
 for shape in ['Basis','ClosedFists']:
  coords=[v.co.copy() for v in m.shape_keys.key_blocks[shape].data]
  a=BVHTree.FromPolygons(coords,thumb,all_triangles=True);b=BVHTree.FromPolygons(coords,fingers,all_triangles=True)
  states[shape]=sum(not(set(thumb[i])&set(fingers[j])) for i,j in a.overlap(b))
 adjacent={}
 coords=[v.co.copy() for v in m.shape_keys.key_blocks['ClosedFists'].data]
 for digit in range(2,5):
  surfaces=[]
  for d in [digit,digit+1]:
   surfaces.append([list(t.vertices) for t in m.loop_triangles if all(('finger%d_'%d) in owner[i] for i in t.vertices)])
  a,b=[BVHTree.FromPolygons(coords,tris,all_triangles=True) for tris in surfaces]
  overlaps=sum(not(set(surfaces[0][i])&set(surfaces[1][j])) for i,j in a.overlap(b))
  distances=[b.find_nearest(coords[i])[3] for i in set(j for tri in surfaces[0] for j in tri)]
  adjacent[str(digit)+'-'+str(digit+1)]={'intersectingTrianglePairs':overlaps,'minimumSurfaceGapCm':min(distances)}
 report[side]={'adjacentClosedDigits':adjacent,'vertices' :len(m.vertices),'thumbTriangles':len(thumb),'fingerTriangles':len(fingers),'overlaps':states,'scope':'export-level fully digit-weighted faces; excludes palm/web'}
out.write_text(json.dumps(report,indent=2));print(json.dumps(report))
if any(any(v for v in r['overlaps'].values()) for r in report.values()):raise RuntimeError('Export thumb intersects fingers')

if any(pair["intersectingTrianglePairs"] for result in report.values() for pair in result["adjacentClosedDigits"].values()):raise RuntimeError("Adjacent closed digits intersect")

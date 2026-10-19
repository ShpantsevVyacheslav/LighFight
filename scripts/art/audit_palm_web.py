"""Inspect wrist-only boundaries and palm/web self-intersections on the final density hand mesh."""
import bpy,bmesh,json,sys
from pathlib import Path
from mathutils.bvhtree import BVHTree
src,out=map(Path,sys.argv[sys.argv.index('--')+1:]);bpy.ops.wm.open_mainfile(filepath=str(src));report={}
for side,sign in [('Left',1),('Right',-1)]:
 h=bpy.data.objects['Rebuilt'+side+'Hand'];m=h.data;m.calc_loop_triangles();w=bpy.data.objects['Armature'].data.bones[side+'Hand'].head_local if bpy.data.objects.get('Armature') else next(o for o in bpy.context.scene.objects if o.type=='ARMATURE').data.bones[side+'Hand'].head_local
 bm=bmesh.new();bm.from_mesh(m);boundaries=[e for e in bm.edges if e.is_boundary];unexplained=sum(any(sign*(v.co.x-w.x)>0 for v in e.verts) for e in boundaries);bm.free()
 coords=[v.co.copy() for v in m.shape_keys.key_blocks['ClosedFists'].data];triangles=[tuple(t.vertices) for t in m.loop_triangles];tree=BVHTree.FromPolygons(coords,triangles,all_triangles=True)
 intersections=[]
 for a,b in tree.overlap(tree):
  if a>=b or set(triangles[a])&set(triangles[b]):continue
  if all(sign*(m.vertices[i].co.x-w.x)<10 for i in triangles[a]+triangles[b]):intersections.append([a,b])
 report[side]={'boundaryEdges':len(boundaries),'nonWristBoundaryEdges':unexplained,'proximalPalmWebIntersectingPairs':len(intersections),'examples':[{'triangles':pair,'groups':[[sorted([(round(g.weight,2),h.vertex_groups[g.group].name) for g in m.vertices[i].groups],reverse=True)[:2] for i in triangles[j]] for j in pair]} for pair in intersections[:3]]}
out.write_text(json.dumps(report,indent=2));print(report)
if any(r['nonWristBoundaryEdges'] for r in report.values()):raise RuntimeError('Hand has a hole away from wrist')

if any(r["proximalPalmWebIntersectingPairs"] for r in report.values()):raise RuntimeError("Closed proximal palm/web self-intersects")

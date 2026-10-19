"""Rotation-invariant waist and rigid-head deformation checks across every exported action."""
import bpy,json,math
from pathlib import Path
from mathutils import Vector
root=Path(__file__).resolve().parents[2];bpy.ops.wm.open_mainfile(filepath=str(root/'art/source/original-island/animated.blend'));arm=next(o for o in bpy.data.objects if o.type=='ARMATURE');body=next(o for o in bpy.data.objects if o.type=='MESH');rest=[v.co.copy() for v in body.data.vertices]
def radius(points):
 center=sum(points,Vector())/len(points);return sum((p-center).length for p in points)/len(points)
rings=[[v.index for v in body.data.vertices if abs(v.co.x)<25 and abs(v.co.z-height)<.7] for height in [92,102,112]]
heads=[v.index for v in body.data.vertices if v.co.z>151];headgroup=body.vertex_groups.find('Head')
assert all(len(body.data.vertices[i].groups)==1 and body.data.vertices[i].groups[0].group==headgroup and abs(body.data.vertices[i].groups[0].weight-1)<1e-5 for i in heads),'Face must remain rigid'
reference=[radius([rest[i] for i in ids]) for ids in rings];audit=[]
for action in bpy.data.actions:
 arm.animation_data.action=action;ratios=[];lo,hi=action.frame_range
 for t in [0,.25,.5,.75,1]:
  bpy.context.scene.frame_set(round(lo+(hi-lo)*t));bpy.context.view_layer.update();mesh=body.evaluated_get(bpy.context.evaluated_depsgraph_get()).to_mesh()
  values=[radius([mesh.vertices[i].co for i in ids])/r for ids,r in zip(rings,reference)];ratios.extend(values)
  assert all(math.isfinite(v) for p in mesh.vertices for v in p.co),'Nonfinite skin'
  body.evaluated_get(bpy.context.evaluated_depsgraph_get()).to_mesh_clear()
 audit.append({'motion':action.name,'waistMinRadiusRatio':min(ratios),'waistMaxRadiusRatio':max(ratios)})
report={'motions':len(audit),'headVerticesRigid':len(heads),'waistBandsCm':[92,102,112],'minimumWaistRadiusRatio':min(a['waistMinRadiusRatio'] for a in audit),'maximumWaistRadiusRatio':max(a['waistMaxRadiusRatio'] for a in audit),'actions':audit};(root/'art/source/original-island/deformation-audit.json').write_text(json.dumps(report,indent=2));print(json.dumps({k:v for k,v in report.items() if k!='actions'}));assert report['minimumWaistRadiusRatio']>.80,'Waist collapses beyond 20%'

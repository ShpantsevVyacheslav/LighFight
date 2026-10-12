"""Compare native web GLB skin assignments against direct FBX export.
Blender --background --python this.py. No model edits or retargeting.
"""
import bpy,json
from pathlib import Path
from mathutils.kdtree import KDTree
root=Path(__file__).resolve().parents[2]/'art/source/island-web-visible'
def snapshot(path):
 bpy.ops.wm.read_factory_settings(use_empty=True)
 if path.suffix=='.glb':bpy.ops.import_scene.gltf(filepath=str(path))
 else:bpy.ops.import_scene.fbx(filepath=str(path))
 for arm in [o for o in bpy.data.objects if o.type=='ARMATURE']:
  arm.animation_data_clear()
  for b in arm.pose.bones:b.matrix_basis.identity()
 bpy.context.view_layer.update();records=[]
 for obj in bpy.data.objects:
  if obj.type!='MESH' or len(obj.data.vertices)<1000:continue
  groups={g.index:g.name.split(':')[-1] for g in obj.vertex_groups}
  for vertex in obj.data.vertices:
   records.append((obj.matrix_world@vertex.co,{groups[g.group]:g.weight for g in vertex.groups if g.weight>1e-8}))
 # Both vendor imports convert to Blender Z-up. Normalize only for matching.
 height=max(p.z for p,w in records)-min(p.z for p,w in records)
 origin=[(max(p[i] for p,w in records)+min(p[i] for p,w in records))/2 for i in (0,1)]+[min(p.z for p,w in records)]
 return [((p.x-origin[0])/height,(p.y-origin[1])/height,(p.z-origin[2])/height,w) for p,w in records]
base=snapshot(root/'provider-web-rig/character.glb');tree=KDTree(len(base))
for i,v in enumerate(base):tree.insert(v[:3],i)
tree.balance();report={}
for path in [root/'provider-web-rig/character.fbx',next((root/'web-greeting-fbx').glob('*.fbx'))]:
 candidate=snapshot(path);distances=[];errors=[];worst=[]
 for v in candidate:
  _,index,distance=tree.find(v[:3]);original=base[index][3];weights=v[3]
  delta=max((abs(original.get(k,0)-weights.get(k,0)) for k in set(original)|set(weights)),default=0)
  distances.append(distance);errors.append(delta)
  if delta>.01:worst.append({'position':v[:3],'weightMaxDifference':delta,'glb':original,'fbx':weights})
 report[path.name]={'sourceVertices':len(base),'fbxVertices':len(candidate),'surfaceNearestMax':max(distances),'weightMeanDifference':sum(errors)/len(errors),'weightMaxDifference':max(errors),'verticesDifferingOverOnePercent':len(worst),'worstExamples':sorted(worst,key=lambda x:x['weightMaxDifference'],reverse=True)[:8]}
(root/'native-glb-fbx-skin-audit.json').write_text(json.dumps(report,indent=2));print(json.dumps({k:{p:v for p,v in r.items() if p!='worstExamples'} for k,r in report.items()},indent=2))

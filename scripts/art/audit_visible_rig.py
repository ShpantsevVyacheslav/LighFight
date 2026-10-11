"""Compare exact Meshy source with provider rig in normalized rest space."""
import bpy,json,sys
from pathlib import Path
from mathutils.kdtree import KDTree
root=Path(__file__).resolve().parents[2];folder=root/'art/source/island-web-visible'
def load(file):
 bpy.ops.wm.read_factory_settings(use_empty=True);bpy.ops.import_scene.gltf(filepath=str(file));arm=next((o for o in bpy.context.scene.objects if o.type=='ARMATURE'),None)
 if arm:
  arm.animation_data_clear()
  for bone in arm.pose.bones:bone.matrix_basis.identity()
 bpy.context.view_layer.update();points=[];triangles=0
 for obj in bpy.context.scene.objects:
  if obj.type!='MESH' or len(obj.data.vertices)<1000:continue
  mesh=obj.evaluated_get(bpy.context.evaluated_depsgraph_get()).to_mesh()
  points.extend(obj.matrix_world@v.co for v in mesh.vertices);triangles+=sum(len(p.vertices)-2 for p in mesh.polygons)
 lo=min(p.z for p in points);height=max(p.z for p in points)-lo
 center=[(min(p[i] for p in points)+max(p[i] for p in points))/2 for i in [0,1]]
 return [((p.x-center[0])/height,(p.y-center[1])/height,(p.z-lo)/height) for p in points],triangles
candidate=sys.argv[sys.argv.index('--')+1] if '--' in sys.argv else 'rig'
source,st=load(folder/'model.glb');rig,rt=load(folder/(candidate+'.glb'))
tree=KDTree(len(source))
for i,p in enumerate(source):tree.insert(p,i)
tree.balance();distances=sorted(tree.find(p)[2] for p in rig)
report={'sourceTriangles':st,'rigTriangles':rt,'sourceVertices':len(source),'rigVertices':len(rig),'normalizedNearestSurface':{'mean':sum(distances)/len(distances),'p95':distances[int(len(distances)*.95)],'max':distances[-1]},'comparison':'rest pose, uniform height normalization, centered horizontal bounds, evaluated provider skin'}
(folder/(candidate+'-rest-surface-audit.json')).write_text(json.dumps(report,indent=2));print(json.dumps(report,indent=2))

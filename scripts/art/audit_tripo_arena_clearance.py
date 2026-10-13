"""Check every triangle against the clear combat volume using separating axes."""
import bpy,json,numpy as np
from pathlib import Path
root=Path(__file__).resolve().parents[2];folder=root/'art/source/courtyard-tripo-v1/game-draft';bpy.ops.wm.open_mainfile(filepath=str(folder/'courtyard-game.blend'))
obj=next(o for o in bpy.context.scene.objects if o.type=='MESH' and len(o.data.vertices)>1000);mesh=obj.data
positions=np.empty(len(mesh.vertices)*3,dtype=np.float32);mesh.vertices.foreach_get('co',positions);positions=positions.reshape(-1,3)
indices=np.empty(len(mesh.loops),dtype=np.int32);mesh.loops.foreach_get('vertex_index',indices)
if len(mesh.loops)!=len(mesh.polygons)*3:raise RuntimeError('Expected triangular source')
triangles=positions[indices.reshape(-1,3)];minimum=triangles.min(axis=1);maximum=triangles.max(axis=1)
lo=np.array([-5.5,-2,.05]);hi=np.array([5.5,2,2.85]);center=(lo+hi)/2;half=(hi-lo)/2
candidates=np.flatnonzero(np.all(maximum>lo,axis=1)&np.all(minimum<hi,axis=1))
def intersects(triangle):
 v=triangle-center;edges=[v[1]-v[0],v[2]-v[1],v[0]-v[2]];axes=list(np.eye(3));axes.append(np.cross(edges[0],edges[1]));axes.extend(np.cross(edge,axis) for edge in edges for axis in np.eye(3))
 for axis in axes:
  if np.dot(axis,axis)<1e-14:continue
  projection=v@axis;radius=half@np.abs(axis)
  if projection.min()>=radius-1e-6 or projection.max()<=-radius+1e-6:return False
 return True
collisions=[int(i) for i in candidates if intersects(triangles[i])]
report={'triangles_checked':len(triangles),'aabb_candidates':len(candidates),'intersections':len(collisions),'volume_min':lo.tolist(),'volume_max':hi.tolist(),'method':'triangle-box SAT, 13 separating axes','device_verified':False}
(folder/'clearance-audit.json').write_text(json.dumps(report,indent=2));print('EXACT_CLEARANCE '+json.dumps(report))
if collisions:raise RuntimeError('Decoration intersects combat volume: '+str(collisions[:20]))
# Positive control: deliberately place an obstacle inside the same volume.
if not intersects(np.array([[0,0,1],[.2,0,1],[0,.2,1]])):raise RuntimeError('Clearance check is blind to an injected obstacle')
print('MUTATION_CONTROL detected synthetic obstacle')

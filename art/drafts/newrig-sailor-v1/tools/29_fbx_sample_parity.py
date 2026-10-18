"""Actual FBX transport keyed-sample gate only; NOT off-key source curve proof."""
import bpy,json,math
from pathlib import Path
import numpy as np
from mathutils import Vector
from mathutils.kdtree import KDTree
O=Path(__file__).resolve().parents[1];P=O/'dense-def-fbx-packet-v1'
bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False)
bpy.ops.import_scene.fbx(filepath=str(P/'SailorOriginalDenseDEF.fbx'),automatic_bone_orientation=False,use_anim=False)
body=next(o for o in bpy.context.scene.objects if o.type=='MESH');rig=next(o for o in bpy.context.scene.objects if o.type=='ARMATURE');N=len(body.data.vertices);raw=np.empty(N*3,np.float32);body.data.vertices.foreach_get('co',raw);mw=np.array(body.matrix_world);world=raw.reshape(-1,3)@mw[:3,:3].T+mw[:3,3]
source=np.load(P/'original-indexed-attributes.npz');ref=json.loads((P/'pose-reference.json').read_text());ids=np.array(ref['sampleOriginalVertexIndices']);sw=source['meshWorldBlender'];src=source['positionsBlenderLocal'][ids]@sw[:3,:3].T+sw[:3,3]
k=KDTree(N)
for i,v in enumerate(world):k.insert(Vector(v),i)
k.balance();mapped=[];dist=[]
for p in src:
 co,i,d=k.find(Vector(p));mapped.append(i);dist.append(d)
assert max(dist)<1e-5,max(dist)
report={'scope':'Keyed review samples only; off-key96/120Hz oracle still REQUIRED','importedVertices':N,'sourceTriangles':len(source['triangles']),'bindNearestMaxM':max(dist),'mapping':'Bind world nearest for4096 diagnostic samples only, not full topology/UV/weight correspondence acceptance','samples':[]}
for clipName in ['dance_01','box_02','front_kick_01']:
 before=set(bpy.data.objects);acts=set(bpy.data.actions);bpy.ops.import_scene.fbx(filepath=str(P/(clipName+'-derived-DEF.fbx')),automatic_bone_orientation=False,use_anim=True);created=[o for o in bpy.data.objects if o not in before];action=next(a for a in bpy.data.actions if a not in acts);animatedRig=next(o for o in created if o.type=='ARMATURE');rig.rotation_mode=animatedRig.rotation_mode
 for pb in rig.pose.bones:pb.rotation_mode=animatedRig.pose.bones[pb.name].rotation_mode
 rig.animation_data_create();rig.animation_data.action=action
 for r in [r for r in ref['references'] if r['clip']==clipName]:
  frame=action.frame_range[0]+r['frame'];bpy.context.scene.frame_set(math.floor(frame),subframe=frame%1);ob=body.evaluated_get(bpy.context.evaluated_depsgraph_get());me=ob.to_mesh();co=np.array([ob.matrix_world@me.vertices[int(i)].co for i in mapped]);ob.to_mesh_clear();u=co[:,[0,2,1]].copy();u[:,2]*=-1;e=np.linalg.norm(u-np.array(r['positionsUnityYUp']),axis=1);row={'clip':clipName,'phase':r['phase'],'frame':frame,'meanErrorM':float(e.mean()),'p95ErrorM':float(np.quantile(e,.95)),'maxErrorM':float(e.max())};report['samples'].append(row);(P/'fbx-keyed-sample-parity.json').write_text(json.dumps(report,indent=2));print('SAMPLE '+json.dumps(row),flush=True)
 for o in created:bpy.data.objects.remove(o,do_unlink=True)
report['maxErrorM']=max(r['maxErrorM'] for r in report['samples']);report['status']='PASS_KEYED_ONLY_PENDING_FULL_GATES' if report['maxErrorM']<=.0001 else 'FAIL_TRANSPORT';(P/'fbx-keyed-sample-parity.json').write_text(json.dumps(report,indent=2));print('RESULT '+json.dumps(report),flush=True)

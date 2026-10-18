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

clipName='dance_01';before=set(bpy.data.objects);acts=set(bpy.data.actions);bpy.ops.import_scene.fbx(filepath=str(P/(clipName+'-derived-DEF.fbx')),automatic_bone_orientation=False,use_anim=True);action=next(a for a in bpy.data.actions if a not in acts);rig.animation_data_create();rig.animation_data.action=action;bpy.context.scene.frame_set(int(action.frame_range[0]));bpy.context.view_layer.update()
animatedRig=next(o for o in bpy.context.scene.objects if o.type=='ARMATURE' and o!=rig);bpy.context.view_layer.update();r=next(r for r in ref['references'] if r['clip']==clipName and r['phase']==0);B=np.array([[1,0,0,0],[0,0,1,0],[0,-1,0,0],[0,0,0,1]]);rows=[]
for i,n in enumerate(ref['boneNames']):
 expected=np.linalg.inv(B)@np.array(r['boneWorldUnityYUp'][i])@B@np.linalg.inv(source['armatureWorldBlender']@source['DEFRestArmature'][i]);am=np.array(animatedRig.matrix_world@animatedRig.pose.bones[n].matrix@animatedRig.data.bones[n].matrix_local.inverted()@animatedRig.matrix_world.inverted());bm=np.array(rig.matrix_world@rig.pose.bones[n].matrix@rig.data.bones[n].matrix_local.inverted()@rig.matrix_world.inverted());rows.append({'bone':n,'animatedOwnSkinError':float(np.abs(am-expected).max()),'baseOwnSkinError':float(np.abs(bm-expected).max()),'animatedWorldPoseError':float(np.abs(np.array(animatedRig.matrix_world@animatedRig.pose.bones[n].matrix)-(np.linalg.inv(B)@np.array(r['boneWorldUnityYUp'][i])@B)).max()),'baseBindVsOriginal':float(np.abs(np.array(rig.matrix_world@rig.data.bones[n].matrix_local)-(source['armatureWorldBlender']@source['DEFRestArmature'][i])).max()),'restWorldDelta':float(np.abs(np.array(animatedRig.matrix_world@animatedRig.data.bones[n].matrix_local)-np.array(rig.matrix_world@rig.data.bones[n].matrix_local)).max())})
report={'maxAnimatedOwnSkinError':max(x['animatedOwnSkinError'] for x in rows),'maxBaseOwnSkinError':max(x['baseOwnSkinError'] for x in rows),'maxRestWorldDelta':max(x['restWorldDelta'] for x in rows),'maxAnimatedWorldPoseError':max(x['animatedWorldPoseError'] for x in rows),'maxBaseBindVsOriginal':max(x['baseBindVsOriginal'] for x in rows),'rows':rows};(P/'fbx-animation-vs-base-rest.json').write_text(json.dumps(report,indent=2));print('RESULT '+json.dumps({k:v for k,v in report.items() if k!='rows'}),flush=True)

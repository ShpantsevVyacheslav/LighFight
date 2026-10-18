"""Independent ORIGINAL Rigify evaluator vs combined review24 FBX at 96-Hz-grid holdouts.
Initial stratified/high-acceleration diagnostic, NOT a full96Hz exhaustive pass.
No source edits. Run only after the serialized heavy window is released.
"""
import bpy,json,math,hashlib,time
from pathlib import Path
import numpy as np
from mathutils import Matrix,Vector,Quaternion
O=Path(__file__).resolve().parents[1];P=O/'dense-def-fbx-packet-v1'
s=(O/'tools/27_actual_arm_elevation.py').read_text();exec(compile(s[:s.index('# Corrected actual elevation fixture')],str(O/'tools/27_actual_arm_elevation.py'),'exec'))
# Freeze all original indexed attributes and correction definition; original evaluator is NOT recovered FBX curves.
assert hash_arrays(body)==original
for obj in scene.objects:
 if obj.type=='MESH':
  for mod in obj.modifiers:mod.show_viewport=False
fast=(O/'tools/05_full_motion_gallery.py').read_text();fast=fast[fast.index('def retarget('):fast.index('reports=[]')];a=fast.index(' deps=bpy.context.evaluated_depsgraph_get()');b=fast.index(' errors={}',a);fast=fast[:a]+' lo=np.zeros(3);hi=np.zeros(3)\n'+fast[b:];exec(compile(fast,'original-rigify-retarget-no-mesh','exec'))
manifest=json.loads((P/'manifest.json').read_text());names=manifest['boneNames'];lookup={n:i for i,n in enumerate(names)};weights=np.zeros((N,4),np.float64);joints=np.zeros((N,4),np.int32)
for v in body.data.vertices:
 for k,g in enumerate(v.groups):weights[v.index,k]=g.weight;joints[v.index,k]=lookup[body.vertex_groups[g.group].name]
ids=np.linspace(0,N-1,4096,dtype=int);points=np.concatenate([raw[ids],np.ones((len(ids),1))],axis=1);bindMatrices={n:rig.data.bones[n].matrix_local.inverted()@rig.matrix_world.inverted()@body.matrix_world for n in names}
def frozen_positions(mats):
 M=np.array([rig.matrix_world@mats[n]@bindMatrices[n] for n in names]);return np.sum(np.einsum('nkij,nj->nki',M[joints[ids]],points)*weights[ids,:,None],axis=1)[:,:3]
def grid_for(clip):
 duration=float((clip.frame_range[1]-clip.frame_range[0])/24);end=round(duration*96);grid=set([0,end]);desired=129 if 'dance' in clip.name else 65
 # Stratified 96Hz grid, prefer off24Hz-key timestamps (grid index modulo4 nonzero).
 for q in np.linspace(1,end-1,desired):
  i=int(round(q));i=i+1 if i%4==0 and i+1<end else i;grid.add(i)
 # High source scalar-curve acceleration intervals, evaluated from UNCHANGED source FCurves.
 frameCount=round(duration*24)+1;energy=np.zeros(max(0,frameCount-2),np.float64)
 for fc in clip.fcurves:
  if 'rotation_' not in fc.data_path:continue
  values=np.array([fc.evaluate(float(clip.frame_range[0]+k)) for k in range(frameCount)]);d2=np.diff(values,2);scale=max(float(np.ptp(values)),1e-5);energy+=np.minimum((d2/scale)**2,100)
 for k in np.argsort(energy)[-20:]:
  # Both adjacent intervals at 1/4,1/2,3/4 native-frame offsets.
  for interval in [int(k),int(k)+1]:
   for sub in [1,2,3]:
    i=interval*4+sub
    if 0<i<end:grid.add(i)
 return duration,sorted(grid)
# Candidate FBX raw import used only after authoritative original-source sample is frozen.
pre=set(bpy.data.objects);acts=set(bpy.data.actions);bpy.ops.import_scene.fbx(filepath=str(P/'SailorOriginalDenseDEF-CombinedReview24.fbx'),automatic_bone_orientation=False,use_anim=True);created=[o for o in bpy.data.objects if o not in pre];game=next(o for o in created if o.type=='ARMATURE');gameBody=next(o for o in created if o.type=='MESH');gameActions=[a for a in bpy.data.actions if a not in acts]
for mod in gameBody.modifiers:mod.show_viewport=False
# Bind-world mapping is diagnostic only; full UV/weights mapping belongs to Unity's independent gate.
from mathutils.kdtree import KDTree
kd=KDTree(len(gameBody.data.vertices))
for v in gameBody.data.vertices:kd.insert(gameBody.matrix_world@v.co,v.index)
kd.balance();sampleBind=raw[ids]@mw[:3,:3].T+mw[:3,3];mapped=[];bindError=[]
for p in sampleBind:
 co,i,d=kd.find(Vector(p));mapped.append(i);bindError.append(d)
assert max(bindError)<1e-5
# Actual imported weights/bind matrices, no transplant of source weights onto candidate.
gameNames=[b.name for b in game.data.bones];gameIndex={n:i for i,n in enumerate(gameNames)};gw=np.zeros((len(ids),4),np.float64);gj=np.zeros((len(ids),4),np.int32)
for j,i in enumerate(mapped):
 groups=[g for g in gameBody.data.vertices[i].groups if g.weight>0];assert 0<len(groups)<=4
 for k,g in enumerate(groups):gw[j,k]=g.weight;gj[j,k]=gameIndex[gameBody.vertex_groups[g.group].name]
gp=np.array([gameBody.data.vertices[i].co[:] for i in mapped]);gp=np.concatenate([gp,np.ones((len(ids),1))],axis=1);gb={n:game.data.bones[n].matrix_local.inverted()@game.matrix_world.inverted()@gameBody.matrix_world for n in gameNames}
rows=[];sourcePositions=[];sourceBones=[];sourceTimes=[];start=time.monotonic();report={'status':'RUNNING','scope':'STRATIFIED_HIGH_ACCELERATION_96HZ_GRID_NOT_FULL_GRID','surfaceThresholdM':.0001,'boneThresholdDeg':.01,'originalEvaluator':'Unchanged source nativeFBX -> true-rest freshRigify FK -> C1 support -> frozen original DEF matrices; not review24 curves','sourceAttributes':original,'blendSHA256':hashlib.sha256((O/'stage02-bound-original-dense.blend').read_bytes()).hexdigest(),'sourceFBXSHA256':hashlib.sha256(source.read_bytes()).hexdigest(),'correctionScriptSHA256':hashlib.sha256((O/'tools/27_actual_arm_elevation.py').read_bytes()).hexdigest(),'samples':rows}
for clipName in ['dance_01','box_02','front_kick_01']:
 clip=next(a for a in clips if clipName in a.name);duration,grid=grid_for(clip);action=next(a for a in gameActions if ('Dense_'+clipName+'_review24') in a.name);game.animation_data.action=action
 for count,i in enumerate(grid):
  seconds=i/96;phase=seconds/duration;retarget(clipName,phase)
  if clipName=='dance_01':support(clipName,phase)
  # Freeze source data BEFORE candidate's frame_set can alter any source action.
  frozen={n:rig.pose.bones[n].matrix.copy() for n in names};expected=frozen_positions(frozen);sourcePositions.append(expected.astype(np.float32));sourceBones.append(np.array([rig.matrix_world@frozen[n] for n in names],np.float32));sourceTimes.append([['dance_01','box_02','front_kick_01'].index(clipName),seconds])
  frame=float(action.frame_range[0])+seconds*24;scene.frame_set(math.floor(frame),subframe=frame%1);bpy.context.view_layer.update();M=np.array([game.matrix_world@game.pose.bones[n].matrix@gb[n] for n in gameNames]);observed=np.sum(np.einsum('nkij,nj->nki',M[gj],gp)*gw[:,:,None],axis=1)[:,:3];e=np.linalg.norm(observed-expected,axis=1);angles=[]
  for n in names:
   expectedQ=(rig.matrix_world@frozen[n]).to_quaternion();actualQ=(game.matrix_world@game.pose.bones[n].matrix).to_quaternion();dq=expectedQ.rotation_difference(actualQ);angle=math.degrees(2*math.atan2(math.sqrt(dq.x*dq.x+dq.y*dq.y+dq.z*dq.z),abs(dq.w)));angles.append(angle)
  row={'clip':clipName,'seconds':seconds,'phase':phase,'gridHz':96,'off24HzKey':bool(i%4),'surfaceMaxM':float(e.max()),'surfaceP95M':float(np.quantile(e,.95)),'boneMaxDeg':max(angles)};rows.append(row)
  if count%24==0:print('ORACLE '+clipName+' '+str(count)+'/'+str(len(grid))+' '+json.dumps(row),flush=True);(P/'offkey-source-oracle.json').write_text(json.dumps(report,indent=2))
report['seconds']=time.monotonic()-start;report['maxSurfaceM']=max(x['surfaceMaxM'] for x in rows);report['maxBoneDeg']=max(x['boneMaxDeg'] for x in rows);report['status']='PASS_STRATIFIED_ONLY' if report['maxSurfaceM']<=.0001 and report['maxBoneDeg']<=.01 else 'FAIL_OFFKEY_REVIEW24';report['sourceAttributesExact']=hash_arrays(body)==original;assert report['sourceAttributesExact'];np.savez_compressed(P/'offkey-original-source-reference.npz',sampleOriginalIndices=ids,timesClipIndexSeconds=np.array(sourceTimes),positionsBlenderWorldMetres=np.array(sourcePositions),boneWorldBlenderMetres=np.array(sourceBones));(P/'offkey-source-oracle.json').write_text(json.dumps(report,indent=2));print('RESULT '+json.dumps({k:v for k,v in report.items() if k!='samples'}),flush=True)

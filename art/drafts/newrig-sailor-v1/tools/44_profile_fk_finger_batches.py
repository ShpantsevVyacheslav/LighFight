"""PREPARED ONLY: exact-candidate source callback profiler, no export or source writes.
Compare all136DEF world/skin matrices and full-original AABB/LBS before using batching.
"""
import bpy,json,time,math,sys,hashlib
from pathlib import Path
import numpy as np
from mathutils import Vector,Matrix
O=Path(__file__).resolve().parents[1];P=O/'dense-def-fbx-packet-v1';sys.path.insert(0,str(O/'tools/adaptive'));from curve_core import InfluenceBounds,bone_angle_deg
s=(O/'tools/27_actual_arm_elevation.py').read_text();exec(compile(s[:s.index('# Corrected actual elevation fixture')],str(O/'tools/27_actual_arm_elevation.py'),'exec'))
for ob in scene.objects:
 if ob.type=='MESH':
  for mod in ob.modifiers:mod.show_viewport=False
fast=(O/'tools/05_full_motion_gallery.py').read_text();fast=fast[fast.index('def retarget('):fast.index('reports=[]')];i=fast.index(' deps=bpy.context.evaluated_depsgraph_get()');j=fast.index(' errors={}',i);fast=fast[:i]+' lo=np.zeros(3);hi=np.zeros(3)\n'+fast[j:];exec(compile(fast,'original-source-retarget','exec'));baseline=retarget
start=fast.index(' for t,d in mapping:\n  pb=');end=fast.index(' lo=np.zeros(3)',start);loop=fast[start:end];inner=loop.split('\n',1)[1].replace(';bpy.context.view_layer.update()','');inner='\n'.join(' '+line for line in inner.splitlines());replacement=" batches=[[pair] for pair in mapping if not is_finger(pair[0])]+[[pair for pair in mapping if is_finger(pair[0]) and pair[0].split('.')[1]==segment] for segment in ['01','02','03']]\n for batch in batches:\n  for t,d in batch:\n"+inner+'\n  bpy.context.view_layer.update()\n'
batchedCode=fast[:start]+replacement+fast[end:];batchedCode=batchedCode.replace('def retarget(', 'def retarget_batched(');is_finger=lambda n:n.startswith(('thumb.','f_index.','f_middle.','f_ring.','f_pinky.'));exec(compile(batchedCode,'candidate-finger-depth-retarget','exec'))
# Fail closed if a mapped finger control has an active constraint: batching has not
# established independence for constrained controls or externally driven controls.
fingerControls=[t for t,d in mapping if is_finger(t)]
for name in fingerControls:
 assert not [c for c in rig.pose.bones[name].constraints if not c.mute and c.influence!=0], 'Unsupported active finger-control constraint: '+name
if rig.animation_data:
 for driver in rig.animation_data.drivers:
  assert not any(('pose.bones[\"'+name+'\"]') in driver.data_path for name in fingerControls), 'Unsupported driven finger control: '+driver.data_path
manifest=json.loads((P/'manifest.json').read_text());names=manifest['boneNames'];nameIndex={n:i for i,n in enumerate(names)};weights=np.zeros((N,4),np.float64);joints=np.zeros((N,4),np.int32)
for vertex in body.data.vertices:
 for k,g in enumerate(vertex.groups):weights[vertex.index,k]=g.weight;joints[vertex.index,k]=nameIndex[body.vertex_groups[g.group].name]
bounds=InfluenceBounds.from_all_original_vertices(raw,joints,weights,len(names));bind={n:rig.data.bones[n].matrix_local.inverted()@rig.matrix_world.inverted()@body.matrix_world for n in names}
def snapshot():
 world=np.array([rig.matrix_world@rig.pose.bones[n].matrix for n in names]);skin=np.array([rig.matrix_world@rig.pose.bones[n].matrix@bind[n] for n in names]);return world,skin
def run(fn,clip,phase):
 begin=time.perf_counter();fn(clip,phase)
 if clip=='dance_01':support(clip,phase)
 return time.perf_counter()-begin
cases=[('dance_01',x) for x in [0,.25,.5,.75,1]]+[('box_02',x) for x in [0,.35,.664179104477612,1]]+[('front_kick_01',x) for x in [.15,.45,.7]]+[('walk',x) for x in [.2,.7]]
# Warm both paths. Counters/timing cover source retarget+UNCHANGED support only; verification outside measured calls.
for fn in [baseline,retarget_batched]:run(fn,'dance_01',.5)
rows=[];worst=None
for clip,phase in cases:
 baselineTimes=[];batchTimes=[]
 for repeat in range(2):
  baselineTimes.append(run(baseline,clip,phase));A,SA=snapshot();batchTimes.append(run(retarget_batched,clip,phase));B,SB=snapshot();bound=bounds.error_bound(SA,SB);angle=bone_angle_deg(A,B);element=float(np.abs(A-B).max());row={'clip':clip,'phase':phase,'repeat':repeat,'all136WorldMatrixMaxElementDifference':element,'all136BoneMaxDeg':angle,'allOriginalSurfaceBoundM':bound,'baselineCallbackSeconds':baselineTimes[-1],'batchedCallbackSeconds':batchTimes[-1]};rows.append(row)
  if worst is None or bound>worst[0]:worst=(bound,SB-SA,clip,phase)
  print('CASE '+json.dumps(row),flush=True)
actual=0.;D=worst[1]
for begin in range(0,N,65536):
 end=min(N,begin+65536);points=np.concatenate([raw[begin:end],np.ones((end-begin,1))],1);delta=np.zeros((end-begin,4))
 for k in range(4):delta+=np.einsum('nij,nj->ni',D[joints[begin:end,k]],points)*weights[begin:end,k,None]
 actual=max(actual,float(np.linalg.norm(delta[:,:3],axis=1).max()))
assert actual<=worst[0]+1e-9;geometryExact=hash_arrays(body)==original;assert geometryExact;report={'status':'PASS_FINITE_POSES_NOT_FULLCLIP' if max(r['allOriginalSurfaceBoundM'] for r in rows)<=1e-6 and max(r['all136BoneMaxDeg'] for r in rows)<=.001 and max(r['all136WorldMatrixMaxElementDifference'] for r in rows)<=1e-5 else 'FAIL_DO_NOT_USE_BATCHING','sourceVertices':N,'sourceTriangles':1988914,'sourceAttributesExact':True,'allSourceWeightsUsedForBounds':True,'explicitPerRetargetLoopDGUpdatesBaseline':len(mapping),'explicitPerRetargetLoopDGUpdatesCandidate':sum(not is_finger(t) for t,d in mapping)+3,'sharedResetSupportDGCalls':'UNCHANGED, not counted in the loop-only figure','actualAllOriginalVertexWorstDifferenceM':actual,'baselineCallbackMedianSeconds':float(np.median([r['baselineCallbackSeconds'] for r in rows])),'candidateCallbackMedianSeconds':float(np.median([r['batchedCallbackSeconds'] for r in rows])),'rows':rows,'limits':'14authoredposes×2, not fullclip or phone-performance evidence; require expanded independent source-pose gate before using for full dance.'};(P/'source-finger-batch-profile.json').write_text(json.dumps(report,indent=2));print('RESULT '+json.dumps({k:v for k,v in report.items() if k!='rows'}),flush=True)

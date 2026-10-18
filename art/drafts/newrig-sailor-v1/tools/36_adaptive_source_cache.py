"""OFFLINE-PREPARED Blender adapter. Do not run until heavy-window release.
--screen (default): source worst/stratified intervals, 96/120Hz estimate only.
--full: adaptive complete authored range, independent 1/3,2/3 holdouts; no export.
Original full vertex array builds influence bounds; no reduced-geometry acceptance.
"""
import bpy,json,math,sys,time,hashlib,argparse
from fractions import Fraction
from pathlib import Path
import numpy as np
from mathutils import Matrix,Vector,Quaternion
O=Path(__file__).resolve().parents[1];P=O/'dense-def-fbx-packet-v1';sys.path.insert(0,str(O/'tools/adaptive'));from curve_core import Skeleton,InfluenceBounds,SourceSample,AdaptiveSampler,interpolate,bone_angle_deg
args=argparse.ArgumentParser();args.add_argument('--full',action='store_true');args.add_argument('--resume-cache',default=None);args.add_argument('--adaptive-interval',action='store_true');args.add_argument('--clip',choices=['dance_01','box_02','front_kick_01'],default='dance_01');args.add_argument('--max-samples',type=int,default=20000);args.add_argument('--max-seconds',type=float,default=1200);args.add_argument('--interval-seconds',type=float,default=None);args.add_argument('--rates',type=int,nargs='+',default=[96,120]);a=args.parse_args(sys.argv[sys.argv.index('--')+1:] if '--' in sys.argv else [])
s=(O/'tools/27_actual_arm_elevation.py').read_text();exec(compile(s[:s.index('# Corrected actual elevation fixture')],str(O/'tools/27_actual_arm_elevation.py'),'exec'))
assert N==994452;body.data.calc_loop_triangles();assert len(body.data.loop_triangles)==1988914
for ob in scene.objects:
 if ob.type=='MESH':
  for mod in ob.modifiers:mod.show_viewport=False
fast=(O/'tools/05_full_motion_gallery.py').read_text();fast=fast[fast.index('def retarget('):fast.index('reports=[]')];i=fast.index(' deps=bpy.context.evaluated_depsgraph_get()');j=fast.index(' errors={}',i);fast=fast[:i]+' lo=np.zeros(3);hi=np.zeros(3)\n'+fast[j:];exec(compile(fast,'original-source-retarget','exec'))
manifest=json.loads((P/'manifest.json').read_text());used=set(manifest['boneNames']);parents={}
for n in used:
 parent=rig.data.bones[n].parent
 while parent and parent.name not in used:parent=parent.parent
 parents[n]=parent.name if parent else None
def depth(n):return 0 if parents[n] is None else 1+depth(parents[n])
names=sorted(used,key=lambda n:(depth(n),n));index={n:i for i,n in enumerate(names)};parentIndices=np.array([index[parents[n]] if parents[n] else -1 for n in names]);defRest=np.array([rig.data.bones[n].matrix_local for n in names],np.float64);skel=Skeleton(parentIndices,defRest,np.array(rig.matrix_world),np.array(body.matrix_world),tuple('FULL' for n in names));weights=np.zeros((N,4),np.float64);joints=np.zeros((N,4),np.int32)
for v in body.data.vertices:
 assert 0<len(v.groups)<=4
 for k,g in enumerate(v.groups):weights[v.index,k]=g.weight;joints[v.index,k]=index[body.vertex_groups[g.group].name]
bounds=InfluenceBounds.from_all_original_vertices(raw,joints,weights,len(names));clip=next(c for c in clips if a.clip in c.name);duration=Fraction(round(clip.frame_range[1]-clip.frame_range[0]),24);started=time.monotonic();source_calls=0
# Export hierarchy FULL scale is explicit; local TRS reexpresses ACTUAL original constrained DEF matrices.
def source_sample(t):
 global source_calls
 if time.monotonic()-started>a.max_seconds:
  save_checkpoint();raise RuntimeError('Explicit source-evaluation wall budget exhausted; verified checkpoint retained')
 phase=float(t/duration);retarget(a.clip,phase)
 if a.clip=='dance_01':support(a.clip,phase)
 mats={n:rig.pose.bones[n].matrix.copy() for n in names};local=[]
 for n in names:
  basis=rig.data.bones[n].matrix_local.inverted()@mats[n] if parents[n] is None else rig.data.bones[n].matrix_local.inverted()@rig.data.bones[parents[n]].matrix_local@mats[parents[n]].inverted()@mats[n];loc,q,scale=basis.decompose();local.append([*loc,*q,*scale])
 world=np.array([rig.matrix_world@mats[n] for n in names]);source_calls+=1
 if source_calls%48==0:print('SOURCE '+a.clip+' '+str(source_calls)+' time='+str(float(t)),flush=True)
 if source_calls%192==0:save_checkpoint()
 return SourceSample(np.array(local,np.float64),world,skel.skin(world))
sampler=AdaptiveSampler(skel,bounds,source_sample,max_samples=a.max_samples);rows=[];keys=[];mode='FULL_ADAPTIVE' if a.full else ('ADAPTIVE_INTERVAL_NOT_CLIP_ACCEPTANCE' if a.adaptive_interval else 'SCREEN_ONLY_96_120_NOT_ACCEPTANCE')
sourceConfig={'clip':a.clip,'blender':bpy.app.version_string,'attributes':original,'weightsSHA256':hashlib.sha256(weights.tobytes()+joints.tobytes()).hexdigest(),'restSHA256':hashlib.sha256(defRest.tobytes()+skel.armature_world.tobytes()+skel.mesh_world.tobytes()).hexdigest(),'sourceFBX':hashlib.sha256(source.read_bytes()).hexdigest(),'sourceBlend':hashlib.sha256((O/'stage02-bound-original-dense.blend').read_bytes()).hexdigest(),'code':{str(f.name):hashlib.sha256(f.read_bytes()).hexdigest() for f in [Path(__file__),O/'tools/05_full_motion_gallery.py',O/'tools/27_actual_arm_elevation.py',O/'tools/adaptive/curve_core.py',Path.home()/'.agents/skills/scenario-blender-rigging/scripts/bx_rig.py']},'bones':names}
configText=json.dumps(sourceConfig,sort_keys=True)
def save_checkpoint():
 ts=sorted(sampler.cache)
 if not ts:return
 temp=P/('adaptive-'+a.clip+'-'+mode+'-checkpoint-tmp.npz');dest=P/('adaptive-'+a.clip+'-'+mode+'-checkpoint.npz');np.savez_compressed(temp,sourceConfig=np.array(configText),times=np.array([[t.numerator,t.denominator] for t in ts],np.int64),localTRS=np.array([sampler.cache[t].local_trs for t in ts]),worldPose=np.array([sampler.cache[t].world_pose for t in ts]),worldSkin=np.array([sampler.cache[t].world_skin for t in ts]));temp.replace(dest)
if a.resume_cache:
 loaded=np.load(a.resume_cache);assert str(loaded['sourceConfig'])==configText,'Source/cache configuration mismatch; never reuse other evaluator data'
 for t,local,world,skin in zip(loaded['times'],loaded['localTRS'],loaded['worldPose'],loaded['worldSkin']):sampler.cache[Fraction(int(t[0]),int(t[1]))]=SourceSample(local,world,skin)
 assert len(sampler.cache)<a.max_samples,'Increase explicit budget for a larger verified cache'
def check(t,left,right):
 truth=sampler.sample(t);lo=sampler.sample(left);hi=sampler.sample(right);u=float((t-left)/(right-left));pose=skel.world_pose(interpolate(lo.local_trs,hi.local_trs,u));return {'time':float(t),'timeRational':[t.numerator,t.denominator],'leftRational':[left.numerator,left.denominator],'rightRational':[right.numerator,right.denominator],'surfaceBoundM':bounds.error_bound(truth.world_skin,skel.skin(pose)),'boneMaxDeg':bone_angle_deg(truth.world_pose,pose)}
if a.full or a.adaptive_interval:
 if a.full:
  seed={Fraction(0),duration}
  for fc in clip.fcurves:
   for key in fc.keyframe_points:
    t=Fraction(str(float(key.co.x-clip.frame_range[0])))/24
    if 0<=t<=duration:seed.add(t)
  seed=sorted(seed)
 else:
  assert a.interval_seconds is not None,'Adaptive interval requires an explicit known failing source time'
  k=math.floor(a.interval_seconds*24);seed=[Fraction(k,24),Fraction(k+1,24)];assert 0<=seed[0]<seed[-1]<=duration
 keys=sampler.fit(seed)
 # These non-dyadic times did not train the adaptive fit. Failure is reported without auto-refitting.
 for left,right in zip(keys,keys[1:]):
  for u in (Fraction(1,3),Fraction(2,3)):rows.append(check(left+(right-left)*u,left,right))
 # Independent full-range uniform120Hz holdouts shifted by1/7 cell, not dyadic fitting samples.
 keyFloats=np.array([float(t) for t in keys])
 for k in range(math.ceil(float(duration)*120)):
  t=Fraction(7*k+1,840)
  if t<seed[0] or t>=seed[-1]:continue
  i=max(0,min(int(np.searchsorted(keyFloats,float(t),side='right'))-1,len(keys)-2));row=check(t,keys[i],keys[i+1]);row['holdoutGridHz']=120;row['holdoutCellOffset']='1/7';rows.append(row)
else:
 assert all(rate>0 and rate%24==0 for rate in a.rates),'Candidate rates must be positive multiples of source24Hz'
 red=json.loads((P/'offkey-source-oracle.json').read_text());worst=sorted((r for r in red['samples'] if r['clip']==a.clip),key=lambda r:max(r['surfaceMaxM']/.0001,r['boneMaxDeg']/.01),reverse=True)[:20];intervals={int(math.floor(r['seconds']*24)) for r in worst};intervals.update(int(q) for q in np.linspace(0,float(duration)*24-1,12))
 if a.interval_seconds is not None:intervals={int(math.floor(a.interval_seconds*24))}
 for rate in a.rates:
  for k in sorted(intervals):
   # Independent source quarter/mid/threequarter holdouts within each higher-rate cell.
   for subcell in range(rate//24):
    left=Fraction(k,24)+Fraction(subcell,rate);right=left+Fraction(1,rate)
    for u in (Fraction(1,4),Fraction(1,2),Fraction(3,4)):
     row=check(left+(right-left)*u,left,right);row['candidateRate']=rate;rows.append(row)
# Actual ALL original vertices validate the conservative bound at the three worst diagnostic rows.
fullWitness=[]
for row in sorted(rows,key=lambda x:x['surfaceBoundM'],reverse=True)[:3]:
 t=Fraction(*row['timeRational']);left=Fraction(*row['leftRational']);right=Fraction(*row['rightRational']);truth=sampler.sample(t);pose=skel.world_pose(interpolate(sampler.sample(left).local_trs,sampler.sample(right).local_trs,float((t-left)/(right-left))));difference=skel.skin(pose)-truth.world_skin;actual=0.
 for begin in range(0,N,65536):
  end=min(N,begin+65536);points=np.concatenate([raw[begin:end],np.ones((end-begin,1))],axis=1);delta=np.zeros((end-begin,4))
  for influence in range(4):delta+=np.einsum('nij,nj->ni',difference[joints[begin:end,influence]],points)*weights[begin:end,influence,None]
  actual=max(actual,float(np.linalg.norm(delta[:,:3],axis=1).max()))
 assert actual<=row['surfaceBoundM']+1e-9,(actual,row['surfaceBoundM']);fullWitness.append({'time':float(t),'vertices':N,'actualAllVertexMaxM':actual,'conservativeBoundM':row['surfaceBoundM']})
save_checkpoint()
report={'sourceConfig':sourceConfig,'cacheEntries':len(sampler.cache),'mode':mode,'clip':a.clip,'sourceVertices':N,'triangles':1988914,'allWeightedBones':len(names),'sourceCalls':source_calls,'seconds':time.monotonic()-started,'sourceAttributesExact':hash_arrays(body)==original,'allOriginalVertexBoundWitness':fullWitness,'bounds':'ALL994452 source vertices, positive influence support AABBs; joint scales in full matrices; no geometry sampling for bound','originalConstrainedDEFInheritScale':{n:rig.data.bones[n].inherit_scale for n in names},'exportedHierarchyScaleInheritance':'FULL, source constrained DEF world matrices reexpressed in static-bind localTRS','rows':rows,'maxSurfaceBoundM':max(r['surfaceBoundM'] for r in rows),'maxBoneDeg':max(r['boneMaxDeg'] for r in rows),'acceptedKeys':[[t.numerator,t.denominator] for t in keys]};assert report['sourceAttributesExact'];report['status']='PASS_DIAGNOSTIC_NOT_EXPORT' if report['maxSurfaceBoundM']<=.0001 and report['maxBoneDeg']<=.01 else 'FAIL_ACCURACY';cacheTimes=sorted(sampler.cache);np.savez_compressed(P/('adaptive-'+a.clip+'-'+mode+'-cache.npz'),times=np.array([[t.numerator,t.denominator] for t in cacheTimes],np.int64),localTRS=np.array([sampler.cache[t].local_trs for t in cacheTimes]),worldPose=np.array([sampler.cache[t].world_pose for t in cacheTimes]),worldSkin=np.array([sampler.cache[t].world_skin for t in cacheTimes]),rest=defRest,parents=parentIndices,boneNames=np.array(names),sourceConfig=np.array(configText),armatureWorld=skel.armature_world,meshWorld=skel.mesh_world,boundCorners=bounds.corners,boundActive=bounds.active,maxWeightSum=bounds.max_weight_sum);(P/('adaptive-'+a.clip+'-'+mode+'.json')).write_text(json.dumps(report,indent=2));print('RESULT '+json.dumps({k:v for k,v in report.items() if k not in ['rows','acceptedKeys']}),flush=True)

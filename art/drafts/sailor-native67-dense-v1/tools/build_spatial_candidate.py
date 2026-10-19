"""PREPARED, NEVER RUN: original dense/native67 variable-influence spatial A/B.
Read SPATIAL-CANDIDATE-PLAN.md; run only after explicit heavy handoff.
"""
import os
for k in ['OPENBLAS_NUM_THREADS','OMP_NUM_THREADS','MKL_NUM_THREADS']:os.environ[k]='6'
import sys,json,time,math,hashlib,importlib.util,shutil
from pathlib import Path
import numpy as np,bpy
from mathutils import Vector
from mathutils.bvhtree import BVHTree
from mathutils.kdtree import KDTree
O=Path(__file__).resolve().parents[1];ROOT=O.parents[2];sys.path.insert(0,str(O/'tools'));from spatial_core import reconstruct;from source_loader_contract import validate
# Reuse only proven read-only source/bind loader definitions, stopping before C1/render loops.
loader=O/'tools/compare_raw_native_rigify.py';s=loader.read_text().split('# Any accidental C1 invocation')[0];ns={'__file__':str(loader),'__name__':'spatial_source_loader'};exec(compile(s,str(loader),'exec'),ns)
try:
 sourceDefs=validate(ns);clips=sourceDefs['clips']
 OUT=ns['OUT'];start=ns['start'];limit=ns['args'].max_seconds;assert shutil.disk_usage(OUT).free>=8*2**30,'Need >=8GiB free before artifact construction'
 body,donor,dm,scene,cam,cd=[ns[k] for k in ['body','donor','dm','scene','cam','cd']];N=len(body.data.vertices);names=ns['nativeNames'];raw=np.array([v.co[:] for v in body.data.vertices]);mw=np.array(body.matrix_world);sp=raw@mw[:3,:3].T+mw[:3,3];dp=ns['rawDm']@np.array(dm.matrix_world)[:3,:3].T+np.array(dm.matrix_world)[:3,3];dw=np.zeros((len(dp),67),np.float64)
 for k in range(4):np.add.at(dw,(np.arange(len(dp)),ns['nativeJoint'][:,k]),ns['nativeWeight'][:,k])
 assert np.all(dw.sum(1)>0);donorRawSumError=float(np.max(abs(dw.sum(1)-1)));dw/=dw.sum(1)[:,None]
 oldPath=ROOT/'.tools/tripo-dense-review/reconstruct_spatial_weights.py';spec=importlib.util.spec_from_file_location('rose_readonly_utils',oldPath);utils=importlib.util.module_from_spec(spec);spec.loader.exec_module(utils)
 assert utils.file_sha(ROOT/ns['meta']['original']['source'])==ns['meta']['original']['sha256'],'Original FBX changed';geometry=utils.geometry_signature(body);materials=utils.material_signature(body);bind=utils.bind_signature(donor);donorGroups=ns['group_hash'](dm)
 def action_hash():
  h=hashlib.sha256()
  for a in sorted(clips,key=lambda x:x.name):
   h.update(a.name.encode())
   for fc in a.fcurves:
    h.update((fc.data_path+str(fc.array_index)).encode())
    for p in fc.keyframe_points:h.update(np.asarray([*p.co,*p.handle_left,*p.handle_right],np.float64).tobytes());h.update((p.interpolation+p.handle_left_type+p.handle_right_type).encode())
  return h.hexdigest()
 actions=action_hash();ref=sourceDefs['ref'];assert np.array_equal(raw.astype(np.float32),ref['positionsBlenderLocal']);assert N==994452 and geometry['triangles']==1988914
 height=float(np.ptp(sp[:,2]));sigma=.007*height;assert .97<height<.99
 report={'status':'RUNNING_ALL_REST_AUDIT_NOT_ACCEPTED','source':ns['meta'],'geometryBefore':geometry,'materialBeforeSHA256':materials,'nativeBindBefore':bind,'nativeTrueBindWitness':ns['report']['nativeTrueBindWitness'],'nativeActionSHA256':actions,'algorithm':{'neighbors':48,'sigmaHeight':.007,'chunk':2048,'stableLow':.75,'stableHigh':.95,'weightPruning':False,'influenceCap':None},'limits':['Inferred weights, not original authored skin weights.','No quality or phone acceptance until actual visual review.']}
 def save():report['elapsedSeconds']=time.monotonic()-start;(OUT/'spatial-report.json').write_text(json.dumps(report,indent=2)+'\n')
 def tick(stage,done):
  if time.monotonic()-start>limit:save();raise RuntimeError('Bounded spatial job budget exhausted')
  if done%32768==0:print('PROGRESS '+json.dumps({'stage':stage,'vertices':done,'seconds':time.monotonic()-start}),flush=True);save()
 # Stable names are explicit source families; missing families fail closed.
 stableNames=[]
 for n in names:
  if n in ['Head','Neck','Left_Hand','Right_Hand','Left_Foot','Right_Foot','Left_Toes','Right_Toes'] or any(n.startswith(side+'_'+finger) for side in ['Left','Right'] for finger in ['Thumb','Index','Middle','Ring','Pinky']):stableNames.append(n)
 for family in ['Head','Neck','Left_Hand','Right_Hand','Left_Foot','Right_Foot','Left_Toes','Right_Toes']:assert family in stableNames
 stableCols=[names.index(n) for n in stableNames];report['stableNames']=stableNames
 faces=utils.triangles(dm.data);bvh=BVHTree.FromPolygons([Vector(v) for v in dp],faces,all_triangles=True);kd=KDTree(len(dp))
 for i,p in enumerate(dp):kd.insert(p,i)
 kd.balance();nearestTri=np.empty(N,np.int32);nearestPoint=np.empty((N,3),np.float64);dist=np.empty(N,np.float64)
 checkpoint=os.environ.get('LF_SPATIAL_REST_CHECKPOINT')
 if checkpoint:
  cp=Path(checkpoint).resolve();assert cp.is_relative_to(O.resolve()) and cp!=OUT
  cr=json.loads((cp/'spatial-report.json').read_text());assert cr['status']=='ALL_REST_AUDIT_ONLY_NO_WEIGHTS'
  assert cr['geometryBefore']==geometry and cr['materialBeforeSHA256']==materials and cr['nativeBindBefore']==bind and cr['nativeActionSHA256']==actions and cr['source']==ns['meta'],'Rest checkpoint source/settings mismatch'
  assert utils.file_sha(cp/'all-rest-distance.npz')==cr['restArtifactSHA256'],'Checkpoint bytes changed';z=np.load(cp/'all-rest-distance.npz');dist[:]=z['distanceM'];nearestTri[:]=z['donorTriangle'];nearestPoint[:]=z['nearestPointWorld'];assert np.all(np.isfinite(nearestPoint)) and len(dist)==N and nearestTri.min()>=0 and nearestTri.max()<len(faces)
  report['allRestDistance']=utils.nearest_summary(dist,height);report['allRestWorstVertex']=int(np.argmax(dist));report['restCheckpoint']=str(cp);save();assert dist.max()<=.001*height
 else:
  for i,p in enumerate(sp):
   q,normal,f,d=bvh.find_nearest(Vector(p));assert q is not None;nearestTri[i]=f;nearestPoint[i]=q;dist[i]=d
   if i%32768==0:tick('ALL_REST_BVH',i)
  report['allRestDistance']=utils.nearest_summary(dist,height);report['allRestWorstVertex']=int(np.argmax(dist));np.savez_compressed(OUT/'all-rest-distance.npz',distanceM=dist,donorTriangle=nearestTri,nearestPointWorld=nearestPoint);report['restArtifactSHA256']=utils.file_sha(OUT/'all-rest-distance.npz');save();assert dist.max()<=.001*height,('Rest alignment exceeds explicit.001H gate',float(dist.max()/height))
 if os.environ.get('LF_SPATIAL_REST_ONLY')=='1':report['status']='ALL_REST_AUDIT_ONLY_NO_WEIGHTS';save();raise SystemExit(0)
 # No positive coefficient is removed at any stage.
 result=np.memmap(OUT/'weights-float32.local',mode='w+',dtype=np.float32,shape=(N,67));stableMass=np.empty(N,np.float32);blend=np.empty(N,np.float32);minBaseline=1.;minResult=1.;transportMax=0.
 for begin in range(0,N,2048):
  end=min(N,begin+2048);baseline=np.zeros((end-begin,67),np.float64)
  for row,i in enumerate(range(begin,end)):
   ids=faces[nearestTri[i]];a,b,c=dp[list(ids)];v0=b-a;v1=c-a;v2=nearestPoint[i]-a;den=v0.dot(v0)*v1.dot(v1)-v0.dot(v1)**2;assert den>1e-30,'Degenerate nearest donor triangle'
   v=(v1.dot(v1)*v2.dot(v0)-v0.dot(v1)*v2.dot(v1))/den;w=(v0.dot(v0)*v2.dot(v1)-v0.dot(v1)*v2.dot(v0))/den;bary=np.maximum([1-v-w,v,w],0);bary/=sum(bary);baseline[row]=bary@dw[list(ids)]
  baseline/=baseline.sum(1)[:,None];positive=baseline[baseline>0];minBaseline=min(minBaseline,float(positive.min()));rows=[kd.find_n(Vector(p),48) for p in sp[begin:end]];ids=np.array([[x[1] for x in row] for row in rows]);dd=np.array([[x[2] for x in row] for row in rows]);out,sm,bl=reconstruct(baseline,dw[ids],dd,sigma,stableCols);result[begin:end]=out;stableMass[begin:end]=sm;blend[begin:end]=bl;minResult=min(minResult,float(out[out>0].min()));tick('FIELD48',begin)
 result.flush();counts=np.count_nonzero(result,axis=1);sums=result.sum(1,dtype=np.float64);assert np.all(counts>0) and np.max(abs(sums-1))<2e-6
 report['weights']={'maxInfluences':int(counts.max()),'histogram':{str(i):int(np.sum(counts==i)) for i in np.unique(counts)},'unweighted':0,'minPositiveDonor':float(dw[dw>0].min()),'donorRawSumMaxErrorBeforeExplicitNormalization':donorRawSumError,'minPositiveBaseline':minBaseline,'minPositiveStored':minResult,'sumMaxError':float(np.max(abs(sums-1))),'positiveInfluences':int(counts.sum()),'stableBaryOnly':int(np.sum(blend==0)),'transition':int(np.sum((blend>0)&(blend<1))),'fieldOnly':int(np.sum(blend==1)),'stableBaryOnlyBounds':{'min':sp[blend==0].min(0).tolist(),'max':sp[blend==0].max(0).tolist()} if np.any(blend==0) else None};np.savez_compressed(OUT/'stable-masks.npz',stableMass=stableMass,fieldBlend=blend,restWorld=sp.astype(np.float32))
 # Save CSR variable positives before Blender assignment; no top4/8 projection.
 indptr=np.r_[0,np.cumsum(counts,dtype=np.int64)];indices=np.empty(indptr[-1],np.uint8);values=np.empty(indptr[-1],np.float32)
 for begin in range(0,N,2048):
  end=min(N,begin+2048);r,c=np.nonzero(result[begin:end]);lo,hi=indptr[begin],indptr[end];indices[lo:hi]=c;values[lo:hi]=result[begin:end][r,c]
 np.savez_compressed(OUT/'derived-native67-weights-csr.npz',indptr=indptr,indices=indices,weights=values,boneNames=np.array(names));save()
 world=body.matrix_world.copy();body.parent=None;body.matrix_world=world
 for g in list(body.vertex_groups):body.vertex_groups.remove(g)
 groups=[body.vertex_groups.new(name=n) for n in names]
 for i in range(N):
  for k in range(indptr[i],indptr[i+1]):groups[int(indices[k])].add([i],float(values[k]),'REPLACE')
  if i%32768==0:tick('ASSIGN_ALL_POSITIVE',i)
 for m in body.modifiers:assert m.type=='ARMATURE';m.object=donor;m.use_deform_preserve_volume=False
 for v in body.data.vertices:
  a,b=indptr[v.index:v.index+2];actual={names.index(body.vertex_groups[g.group].name):g.weight for g in v.groups if g.weight>0};expected=dict(zip(map(int,indices[a:b]),map(float,values[a:b])));assert actual==expected,'Named positive weight readback changed'
 assert utils.geometry_signature(body)==geometry and utils.material_signature(body)==materials and utils.bind_signature(donor)==bind and action_hash()==actions and ns['group_hash'](dm)==donorGroups
 report['preservationPassed']=True;report['status']='CANDIDATE_BUILT_VISUAL_GATE_PENDING';save();scene.view_layers[0].material_override=None;bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'SailorOriginalDenseNative67Spatial.blend'))
 # Independent final-weight/native-bind LBS and same native poses, not Rigify retarget.
 bindMatrices=np.array([donor.data.bones[n].matrix_local.inverted()@donor.matrix_world.inverted()@body.matrix_world for n in names]);hp=np.c_[raw,np.ones(N)];report['poses']=[]
 for phase in [.45,.5,.55]:
  clip=next(a for a in clips if 'dance_01' in a.name);donor.animation_data.action=clip;f=clip.frame_range[0]+phase*(clip.frame_range[1]-clip.frame_range[0]);scene.frame_set(math.floor(f),subframe=f%1);bpy.context.view_layer.update();frozen=np.array([donor.matrix_world@donor.pose.bones[n].matrix for n in names]);oracle=np.load(O/'actual-primary-v1'/f'dance{int(phase*100)}-actual-bone-proof.npz');assert list(oracle['nativeNames'])==names and np.array_equal(frozen,oracle['nativeWorld']),'Native own dance changed from untouched primary';skin=frozen@bindMatrices;pred=np.zeros((N,3))
  for begin in range(0,N,2048):
   end=min(N,begin+2048);posed=np.einsum('gij,nj->ngi',skin,hp[begin:end])[:,:,:3];pred[begin:end]=np.einsum('ng,ngi->ni',result[begin:end].astype(float),posed)
  actual=ns['actual'](body);err=float(np.linalg.norm(pred-actual,axis=1).max());assert err<1e-5;donorSurface=ns['actual'](dm);target=np.mean([np.array(donor.matrix_world@donor.pose.bones[n].head) for n in ['Neck','Right_UpperArm','Right_LowerArm']],axis=0);scale=.58;row={'phase':phase,'all994452LBSMaxErrorM':err,'nativeWorldBoneSHA256':hashlib.sha256(frozen.tobytes()).hexdigest(),'captures':[]}
  for ob in [dm,body]:
   for view,offset in [('front',(0,-2,.06)),('backthreequarter',(-.85,2,.12)),('side',(-2,0,.06))]:
    tick('RENDER',0)
    for actor in [body,dm]:actor.hide_render=actor!=ob
    for m in ob.modifiers:m.show_render=True
    cam.location=Vector(target)+Vector(offset);cam.rotation_euler=(Vector(target)-cam.location).to_track_quat('-Z','Y').to_euler();cd.ortho_scale=scale;scene.view_layers[0].material_override=sourceDefs['clay'];scene.render.film_transparent=True;scene.render.filepath=str(OUT/f'dance{int(phase*100)}-{ob.name}-{view}-clay.png');bpy.ops.render.render(write_still=True);row['captures'].append({'file':Path(scene.render.filepath).name,'cameraWorld':list(cam.location),'target':target.tolist(),'scale':scale})
  assert np.array_equal(frozen,np.array([donor.matrix_world@donor.pose.bones[n].matrix for n in names]));report['poses'].append(row);save()
 assert utils.geometry_signature(body)==geometry and utils.material_signature(body)==materials and utils.bind_signature(donor)==bind and action_hash()==actions
 report['status']='COMPLETED_SPATIAL_AB_NOT_QUALITY_ACCEPTED';save();print('RESULT '+str(OUT/'spatial-report.json'),flush=True)
except SystemExit:
 raise
except BaseException as exc:
 out=ns['OUT'];rp=out/'spatial-report.json';failure=json.loads(rp.read_text()) if rp.exists() else {};failure.update(status='FAILED_SPATIAL_AB_NO_ACCEPTANCE',error=repr(exc),elapsedSeconds=time.monotonic()-ns['start']);rp.write_text(json.dumps(failure,indent=2)+'\n');raise

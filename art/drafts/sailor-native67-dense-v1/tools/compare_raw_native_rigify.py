"""Controlled raw own-dance comparison; no C1/correctives/new weights. Authorized window only."""
import os
for k in ['OPENBLAS_NUM_THREADS','OMP_NUM_THREADS','MKL_NUM_THREADS']:os.environ[k]='6'
import argparse,json,sys,time,hashlib,math
from pathlib import Path
import numpy as np,bpy
from mathutils import Vector
HERE=Path(__file__).resolve();O=HERE.parents[1];ROOT=O.parents[2];B=ROOT/'art/drafts/newrig-sailor-v1/skin-quality-underarm-v1';BASE=B/'tools/probe_source_off_on.py';p=argparse.ArgumentParser();p.add_argument('--output',type=Path,default=O/'actual-primary-v1');p.add_argument('--max-seconds',type=float,default=600);args=p.parse_args(sys.argv[sys.argv.index('--')+1:] if '--' in sys.argv else []);OUT=args.output.resolve();assert OUT.is_relative_to(O.resolve()) and not OUT.exists();OUT.mkdir(parents=True);start=time.monotonic();report={'status':'RUNNING_CONTROL_NO_TRANSFER','poses':[],'limits':['Different geometry/rig/weights, so a donor-vsRigify shape comparison is not same-topology error.','Clay diagnostic; no texture/normal comparison or native67skin acceptance inferred.','No source edit, C1, CorrectiveSmooth, DQS or new skin weights.']}
def save():(OUT/'report.json').write_text(json.dumps(report,indent=2)+'\n')
def budget():
 if time.monotonic()-start>args.max_seconds:raise RuntimeError('Controlled comparison budget exceeded')
oldargv=sys.argv;sys.argv=['source-defs','--','--stage','renders','--output',str(OUT),'--max-seconds',str(args.max_seconds)];text=BASE.read_text().split("if args.stage=='masks':")[0];needle="OUT=owned_output(args.output or OWN/(args.stage+('-source-v1' if args.source_material else '-clay-v1')));";assert needle in text;text=text.replace(needle,"OUT=Path(args.output).resolve();assert OUT==Path("+repr(str(OUT))+");OUT.mkdir(parents=True,exist_ok=True);");ns={'__file__':str(BASE),'__name__':'unchanged_source_defs'}
try:exec(compile(text,str(BASE),'exec'),ns)
finally:sys.argv=oldargv
body,rig,donor,dm,scene,cam,cd=[ns[k] for k in ['body','rig','donor','dm','scene','cam','cd']];nativeNames=[b.name for b in donor.data.bones];defNames=ns['names'];N=len(body.data.vertices);assert N==994452 and len(dm.data.vertices)==49992 and len(nativeNames)==67
meta=json.loads((O/'offline-source-audit.json').read_text());src=ROOT/meta['nativeDonor']['source'];assert hashlib.sha256(src.read_bytes()).hexdigest()==meta['nativeDonor']['sha256'];assert str(ns['source'].resolve())==str(src.resolve())
assert all(m.type=='ARMATURE' and not m.use_deform_preserve_volume for ob in [body,dm] for m in ob.modifiers);assert all(b.bbone_segments==1 for arm in [rig,donor] for b in arm.data.bones if b.use_deform)
originalAttrs={ob.name:ns['hash_arrays'](ob) for ob in [body,dm]}
def group_hash(ob):
 h=hashlib.sha256()
 for v in ob.data.vertices:h.update(np.asarray([(g.group,g.weight) for g in v.groups],np.float64).tobytes());h.update(b';')
 return h.hexdigest()
weightHashes={ob.name:group_hash(ob) for ob in [body,dm]}
def bind_hash(arm):return hashlib.sha256(np.array([b.matrix_local for b in arm.data.bones],np.float64).tobytes()+np.array(arm.matrix_world,np.float64).tobytes()+json.dumps([(b.name,b.parent.name if b.parent else None) for b in arm.data.bones]).encode()).hexdigest()
binds={arm.name:bind_hash(arm) for arm in [rig,donor]};bodyNames=[ob.name for ob in [body,dm]]
def actual(ob):
 for m in ob.modifiers:m.show_viewport=True
 bpy.context.view_layer.update();ev=ob.evaluated_get(bpy.context.evaluated_depsgraph_get());me=ev.to_mesh()
 try:
  assert len(me.vertices)==len(ob.data.vertices);a=np.empty(len(me.vertices)*3,np.float32);me.vertices.foreach_get('co',a)
 finally:ev.to_mesh_clear()
 for m in ob.modifiers:m.show_viewport=False
 mat=np.array(ob.matrix_world);return a.reshape(-1,3)@mat[:3,:3].T+mat[:3,3]
# Native mesh's true bind comes from ClusterLink@ClusterTransform, not animation.
action=donor.animation_data.action;donor.animation_data.action=None;donor.data.pose_position='REST';bpy.context.view_layer.update();rawBind=np.array(meta['nativeDonor']['reconstructedMeshBind']);basis=np.array(dm.matrix_world)@np.linalg.inv(rawBind);headErrors=[]
for name,head in meta['nativeDonor']['boneBindHeadsFileUnits'].items():headErrors.append(float(np.linalg.norm(np.array(donor.matrix_world@donor.data.bones[name].head_local)-(basis@np.r_[head,1])[:3])))
assert max(headErrors)<2e-6,('Native imported REST does not match actual cluster heads',max(headErrors));rawDm=np.array([v.co[:] for v in dm.data.vertices]);donorRest=rawDm@np.array(dm.matrix_world)[:3,:3].T+np.array(dm.matrix_world)[:3,3];restError=float(np.linalg.norm(actual(dm)-donorRest,axis=1).max());assert restError<1e-6
report['nativeTrueBindWitness']={'all67ClusterHeadsMaxErrorM':max(headErrors),'all49992RestSurfaceMaxErrorM':restError,'fileGlobalToBlenderWorld':basis.tolist(),'basisFrom':'imported mesh world @ inverse(raw reconstructed cluster mesh bind), no fit','restAuthority':'TransformLink@Transform, installed Blender4.2 FBX convention'};donor.data.pose_position='POSE';donor.animation_data.action=action
# Snapshot native sparse authored influences exactly; no added/pruned slot.
ndx={n:i for i,n in enumerate(nativeNames)};nativeJoint=np.zeros((len(rawDm),4),int);nativeWeight=np.zeros((len(rawDm),4));groups={g.index:g.name for g in dm.vertex_groups}
for v in dm.data.vertices:
 positive=[g for g in v.groups if g.weight>0];assert 0<len(positive)<=4
 for k,g in enumerate(positive):nativeJoint[v.index,k]=ndx[groups[g.group]];nativeWeight[v.index,k]=g.weight
nativeBind=np.array([donor.data.bones[n].matrix_local.inverted()@donor.matrix_world.inverted()@dm.matrix_world for n in nativeNames]);nativeHp=np.c_[rawDm,np.ones(len(rawDm))]
def native_lbs():
 sk=np.array([donor.matrix_world@donor.pose.bones[n].matrix for n in nativeNames])@nativeBind;out=np.zeros((len(rawDm),3))
 for k in range(4):out+=np.einsum('nij,nj->ni',sk[nativeJoint[:,k]],nativeHp)[:,:3]*nativeWeight[:,k,None]
 return out
# Any accidental C1 invocation is a hard error, not an unchecked UI flag.
calls=[]
def forbidden(*a,**k):calls.append(True);raise RuntimeError('C1 function must never execute in controlled OFF comparison')
ns['support']=forbidden
report.update(sourceTaskId='44623258-1c14-4afb-b55f-bb15ec154b50',nativeSource=str(src),nativeSourceSHA256=meta['nativeDonor']['sha256'],originalSource=meta['original'],rigifyBoundSourceSHA256=ns['report']['sourceHashes'][str(ROOT/'art/drafts/newrig-sailor-v1/stage02-bound-original-dense.blend')],sourceAttrs=originalAttrs,actualWeightHashes=weightHashes,originalBindHashes=binds,C1InvocationCount=0,skinning='Original game LBS, Bbone1, PreserveVolume false')
# Rendered unoccluded joint-only passes are labeled X-ray; no fake depth offset.
markmat=bpy.data.materials.new('QA_JointEmission');markmat.use_nodes=True;nd=markmat.node_tree.nodes;nd.clear();em=nd.new('ShaderNodeEmission');em.inputs['Color'].default_value=(1,.15,.02,1);out=nd.new('ShaderNodeOutputMaterial');markmat.node_tree.links.new(em.outputs[0],out.inputs['Surface']);markers=[]
for i in range(10):
 bpy.ops.mesh.primitive_uv_sphere_add(segments=12,ring_count=6,radius=.002);ob=bpy.context.object;ob.name='QA_joint_'+str(i);ob.data.materials.append(markmat);ob.hide_render=True;markers.append(ob)
links=[]
for i in range(9):
 cu=bpy.data.curves.new('QA_joint_link','CURVE');cu.dimensions='3D';cu.bevel_depth=.0006;cu.bevel_resolution=0;sp=cu.splines.new('POLY');sp.points.add(1);ob=bpy.data.objects.new('QA_joint_link',cu);scene.collection.objects.link(ob);ob.data.materials.append(markmat);ob.hide_render=True;links.append(ob)
semantic=['UpperChest','Neck','Right_Shoulder','Right_UpperArm','Right_LowerArm','Right_Hand','Left_Shoulder','Left_UpperArm','Left_LowerArm','Left_Hand'];rigMap=['ORG-spine.003','ORG-spine.004','ORG-shoulder.R','ORG-upper_arm.R','ORG-forearm.R','ORG-hand.R','ORG-shoulder.L','ORG-upper_arm.L','ORG-forearm.L','ORG-hand.L'];connections=[(0,1),(0,2),(2,3),(3,4),(4,5),(0,6),(6,7),(7,8),(8,9)]
def joints(arm):return np.array([arm.matrix_world@arm.pose.bones[n].head for n in (semantic if arm==donor else rigMap)])
def snapshot(arm,names):return np.array([arm.matrix_world@arm.pose.bones[n].matrix for n in names])
def render_actor(ob,arm,surface,jp,phase,view,target,scale):
 budget();offset={'front':(0,-2,.06),'backthreequarter':(-.85,2,.12),'side':(-2,0,.06)}[view];cam.location=Vector(target)+Vector(offset);cam.rotation_euler=(Vector(target)-cam.location).to_track_quat('-Z','Y').to_euler();cd.ortho_scale=scale
 for actor in [body,dm]:actor.hide_render=actor!=ob
 for m in ob.modifiers:m.show_render=True;m.show_viewport=True
 for q in markers+links:q.hide_render=True
 scene.view_layers[0].material_override=ns['clay'];scene.render.film_transparent=True;scene.render.filepath=str(OUT/f'dance{int(phase*100)}-{ob.name}-{view}-clay.png');actualPoints=actual(ob);err=float(np.linalg.norm(actualPoints-surface,axis=1).max());assert err<1e-5;bpy.ops.render.render(write_still=True);beauty=Path(scene.render.filepath).name
 ob.hide_render=True
 for q,point in zip(markers,jp):q.location=point;q.hide_render=False
 for q,(i,j) in zip(links,connections):q.data.splines[0].points[0].co=(*jp[i],1);q.data.splines[0].points[1].co=(*jp[j],1);q.hide_render=False
 scene.view_layers[0].material_override=None;scene.render.filepath=str(OUT/f'dance{int(phase*100)}-{ob.name}-{view}-joints-Xray.png');bpy.ops.render.render(write_still=True)
 for q in markers+links:q.hide_render=True
 return {'beauty':beauty,'jointPass':Path(scene.render.filepath).name,'jointPassMeaning':'Actual-world spheres/links rendered without actor occlusion, explicitly Xray','worldJointPositions':jp.tolist(),'semanticLabels':semantic,'cameraWorld':list(cam.location),'cameraTarget':list(target),'orthoScale':scale,'surfaceRepeatMaxErrorM':err}
for ob in scene.objects:
 if ob.type=='MESH':ob.hide_render=True
try:
 for phase in [.45,.5,.55]:
  budget();case={'name':'dance'+str(int(phase*100)),'clip':'dance_01','phase':phase};ns['setup_pose'](case,False);off=snapshot(rig,defNames);rawControl=np.array([rig.matrix_world@rig.pose.bones[t].matrix for t,d in ns['mapping']]);ns['retarget']('dance_01',phase);direct=snapshot(rig,defNames);rawDelta=float(np.max(abs(off-direct)));assert rawDelta==0 and not calls,'Actual C1OFF is not raw retarget matrices';controlDelta=float(np.max(abs(rawControl-np.array([rig.matrix_world@rig.pose.bones[t].matrix for t,d in ns['mapping']]))));assert controlDelta==0
  nativePose=snapshot(donor,nativeNames);denseSurface=actual(body);densePrediction,_=ns['world_surface']();donorSurface=actual(dm);donorPrediction=native_lbs();denseErr=float(np.linalg.norm(denseSurface-densePrediction,axis=1).max());nativeErr=float(np.linalg.norm(donorSurface-donorPrediction,axis=1).max());assert max(denseErr,nativeErr)<1e-5
  points={body.name:denseSurface,dm.name:donorSurface};js={body.name:joints(rig),dm.name:joints(donor)};focus=np.vstack([x[[0,1,2,3,4]] for x in js.values()]);target=(focus.min(0)+focus.max(0))*.5;scale=max(.52,float(np.ptp(focus,axis=0).max()*1.6));row={'phase':phase,'sourceFrame':1+phase*556,'sourceSeconds':phase*556/24,'C1OFFVsRawAll136DEFMatrixMaxComponentError':rawDelta,'C1OFFVsRawMappedControlMatrixMaxComponentError':controlDelta,'independentLBSMaxM':{'RigifyOriginalDense':denseErr,'Native67Donor':nativeErr},'worldBounds':{n:{'min':s.min(0).tolist(),'max':s.max(0).tolist()} for n,s in points.items()},'captures':[]}
  np.savez_compressed(OUT/f'dance{int(phase*100)}-actual-bone-proof.npz',rigifyDEFWorld=direct,rigifyDEFNames=np.array(defNames),nativeWorld=nativePose,nativeNames=np.array(nativeNames),rawMappedControlWorld=rawControl)
  for ob,arm in [(dm,donor),(body,rig)]:
   for view in ['front','backthreequarter','side']:row['captures'].append(render_actor(ob,arm,points[ob.name],js[ob.name],phase,view,target,scale))
  assert np.array_equal(direct,snapshot(rig,defNames)) and np.array_equal(nativePose,snapshot(donor,nativeNames));report['poses'].append(row);save()
 assert not calls and all(ns['hash_arrays'](ob)==originalAttrs[ob.name] and group_hash(ob)==weightHashes[ob.name] for ob in [body,dm]);assert all(bind_hash(arm)==binds[arm.name] for arm in [rig,donor]);report['status']='COMPLETED_PRIMARY_CONTROL_NO_TRANSFER_NO_QUALITY_ACCEPTANCE';report['originalAttrsWeightsBindsUnchanged']=True
except BaseException as exc:report['status']='FAILED_CONTROL';report['error']=repr(exc);raise
finally:report['elapsedSeconds']=time.monotonic()-start;report['C1InvocationCount']=len(calls);save()

"""PREPARED OFFLINE. Run only with explicit CPU6 window; no source saves."""
import os
for k in ['OPENBLAS_NUM_THREADS','OMP_NUM_THREADS','MKL_NUM_THREADS']:os.environ[k]='6'
import sys,json,hashlib,math,time,importlib.util,shutil
from pathlib import Path
import numpy as np,bpy
from mathutils import Vector
O=Path(__file__).resolve().parents[1];ROOT=O.parents[2];sys.path.insert(0,str(O/'tools'));from source_loader_contract import validate
loader=O/'tools/compare_raw_native_rigify.py';text=loader.read_text().split('# Any accidental C1 invocation')[0];ns={'__file__':str(loader),'__name__':'textured_source_loader'};exec(compile(text,str(loader),'exec'),ns);src=validate(ns);OUT=ns['OUT'];start=ns['start'];body,donor,dm,rig,scene,cam,cd=[ns[k] for k in ['body','donor','dm','rig','scene','cam','cd']];assert shutil.disk_usage(OUT).free>=8*2**30
path=ROOT/'.tools/tripo-dense-review/reconstruct_spatial_weights.py';spec=importlib.util.spec_from_file_location('readonly_hash_utils',path);u=importlib.util.module_from_spec(spec);spec.loader.exec_module(u)
C=O/'spatial-candidate-v1';manifest=json.loads((C/'LOCAL-ARTIFACTS.json').read_text());csr=C/'derived-native67-weights-csr.npz';entry=next(f for f in manifest['files'] if f['path']==csr.name);assert u.file_sha(csr)==entry['sha256'];z=np.load(csr);names=list(z['boneNames']);assert names==ns['nativeNames'];ptr=z['indptr'];ix=z['indices'];w=z['weights'];assert len(ptr)==994453 and len(w)==2922048 and np.all(w>0);geometry=u.geometry_signature(body);mat=u.material_signature(body);bind=u.bind_signature(donor);raw=np.array([v.co[:] for v in body.data.vertices]);hp=np.c_[raw,np.ones(len(raw))]
report={'status':'RUNNING_TEXTURED_GATE_NO_PROMOTION','candidateCSR_SHA256':entry['sha256'],'sourceGeometryBefore':geometry,'materialSHA256':mat,'nativeBindBefore':bind,'frames':[],'nativeClips':[{'name':a.name,'startFrame':float(a.frame_range[0]),'endFrame':float(a.frame_range[1]),'fps':24,'durationSeconds':float((a.frame_range[1]-a.frame_range[0])/24)} for a in src['clips']],'poseComparisonLimit':'Rigify BEFORE and native67 AFTER share authored time, NOT identical anatomical limb pose;10–11degree prior bias remains explicit.','display':{'view':'AgX','exposureStops':-2,'sourceMaterialsUnmodified':True}}
for item,a in zip(report['nativeClips'],src['clips']):
 h=hashlib.sha256()
 for fc in a.fcurves:
  h.update((fc.data_path+str(fc.array_index)).encode())
  for k in fc.keyframe_points:h.update(np.array([*k.co,*k.handle_left,*k.handle_right],np.float64).tobytes());h.update(k.interpolation.encode())
 item['curveKeySHA256']=h.hexdigest()
def save():report['elapsedSeconds']=time.monotonic()-start;(OUT/'report.json').write_text(json.dumps(report,indent=2)+'\n')
def budget():
 if time.monotonic()-start>ns['args'].max_seconds:raise RuntimeError('QA time budget exhausted')
scene.view_layers[0].material_override=None;scene.view_settings.view_transform='AgX';scene.view_settings.exposure=-2;scene.render.resolution_x=640;scene.render.resolution_y=800;scene.render.film_transparent=True;scene.cycles.samples=12;scene.render.threads_mode='FIXED';scene.render.threads=6
for ob in scene.objects:
 if ob.type=='MESH':ob.hide_render=ob!=body
# Material sockets/image bytes are unchanged; only camera/display/light sampling are QA settings.
def set_native(clip,phase):
 a=next(a for a in src['clips'] if clip in a.name);donor.animation_data.action=a;f=float(a.frame_range[0]+phase*(a.frame_range[1]-a.frame_range[0]));scene.frame_set(math.floor(f),subframe=f%1);bpy.context.view_layer.update();return np.array([donor.matrix_world@donor.pose.bones[n].matrix for n in names])
def normals():
 for m in body.modifiers:m.show_viewport=True
 bpy.context.view_layer.update();ob=body.evaluated_get(bpy.context.evaluated_depsgraph_get());me=ob.to_mesh()
 try:
  a=np.empty(len(me.polygons)*3,np.float32);me.polygons.foreach_get('normal',a);a=a.reshape(-1,3);length=np.linalg.norm(a,axis=1);assert np.all(np.isfinite(a));return {'polygonCount':len(a),'finite':True,'lengthQuantiles':np.quantile(length,[0,.01,.5,.99,1]).tolist(),'zeroLength':int(np.sum(length==0))}
 finally:
  ob.to_mesh_clear()
  for m in body.modifiers:m.show_viewport=False

cameraContract={}
def render(label,phase,points,view,detail=None):
 budget();side=view=='side';offset=Vector((-2,0,.06) if side else (0,-2,.06));lo,hi=points.min(0),points.max(0);target=(lo+hi)/2
 if detail is not None:target,scale=detail
 elif label.startswith('dance'):target=np.mean([np.array(donor.matrix_world@donor.pose.bones[n].head) for n in ['Neck','Right_UpperArm','Right_LowerArm']],axis=0);scale=.58
 else:scale=max(float(hi[2]-lo[2]),float((hi[1]-lo[1]) if side else (hi[0]-lo[0]))*1.25)*1.18
 cam.location=Vector(target)+offset;cam.rotation_euler=(Vector(target)-cam.location).to_track_quat('-Z','Y').to_euler();cd.ortho_scale=scale;bpy.context.view_layer.update()
 # Independent projection using actual camera view-frame width/height, all original vertices.
 inv=np.array(cam.matrix_world.inverted());q=points@inv[:3,:3].T+inv[:3,3];frame=np.array([list(v) for v in cd.view_frame(scene=scene)]);fw=float(np.ptp(frame[:,0]));fh=float(np.ptp(frame[:,1]));bounds={'x':[(float(q[:,0].min())-frame[:,0].min())/fw,(float(q[:,0].max())-frame[:,0].min())/fw],'y':[(float(q[:,1].min())-frame[:,1].min())/fh,(float(q[:,1].max())-frame[:,1].min())/fh]}
 if not label.startswith('dance') and detail is None:
  # Increase wholebody framing uniformly if the initial scale cannot fit projected bounds.
  need=max(np.max(abs(q[:,0]))/(fw*.42),np.max(abs(q[:,1]))/(fh*.42),1.);cd.ortho_scale*=float(need);frame=np.array([list(v) for v in cd.view_frame(scene=scene)]);fw=float(np.ptp(frame[:,0]));fh=float(np.ptp(frame[:,1]));bounds={'x':[(float(q[:,0].min())-frame[:,0].min())/fw,(float(q[:,0].max())-frame[:,0].min())/fw],'y':[(float(q[:,1].min())-frame[:,1].min())/fh,(float(q[:,1].max())-frame[:,1].min())/fh]};assert min(*bounds['x'],*bounds['y'])>=.04 and max(*bounds['x'],*bounds['y'])<=.96,'Fullbody crop gate'
 for m in body.modifiers:m.show_render=True
 key=(phase,view)
 if label.startswith('dance'):
  value=[*cam.location,*cam.rotation_euler,cd.ortho_scale]
  if '-before-' in label:cameraContract[key]=value
  else:assert cameraContract[key]==value,'Before/after camera drift'
 file=f'{label}-{view}.png';scene.render.filepath=str(OUT/file);bpy.ops.render.render(write_still=True);report['frames'].append({'file':file,'phase':phase,'cameraWorld':list(cam.location),'target':list(target),'orthoScale':cd.ortho_scale,'actualAllVertexProjectedBounds':bounds,'sourceColor':'original4K node graph, unchanged'});save()
try:
 for phase in [.45,.5,.55]:
  src['setup_pose']({'name':'dance','clip':'dance_01','phase':phase},False);oracle=np.load(O/'actual-primary-v1'/f'dance{int(phase*100)}-actual-bone-proof.npz');actualRig=np.array([rig.matrix_world@rig.pose.bones[n].matrix for n in src['names']]);assert np.array_equal(actualRig,oracle['rigifyDEFWorld']);points=ns['actual'](body)
  for view in ['front','side']:render(f'dance{int(phase*100)}-before-RigifyOFF',phase,points,view)
 world=body.matrix_world.copy();body.parent=None;body.matrix_world=world
 for g in list(body.vertex_groups):body.vertex_groups.remove(g)
 groups=[body.vertex_groups.new(name=n) for n in names]
 for i in range(len(raw)):
  for j in range(ptr[i],ptr[i+1]):groups[int(ix[j])].add([i],float(w[j]),'REPLACE')
  if i%32768==0:budget()
 for m in body.modifiers:assert m.type=='ARMATURE';m.object=donor;m.use_deform_preserve_volume=False
 for v in body.data.vertices:
  expected=dict(zip(map(int,ix[ptr[v.index]:ptr[v.index+1]]),map(float,w[ptr[v.index]:ptr[v.index+1]])));actual={names.index(body.vertex_groups[g.group].name):g.weight for g in v.groups if g.weight>0};assert actual==expected
 bm=np.array([donor.data.bones[n].matrix_local.inverted()@donor.matrix_world.inverted()@body.matrix_world for n in names]);report['weights']=manifest['inferredWeights'];report['poseGates']=[]
 cases=[('dance'+str(int(p*100)),'dance_01',p) for p in [.45,.5,.55]]+[('BOX-start-diagnostic','box_02',0),('BOX35','box_02',.35),('BOX70','box_02',.70),('kick35','front_kick_01',.35),('kick70','front_kick_01',.70)]
 for label,clip,phase in cases:
  frozen=set_native(clip,phase);points=ns['actual'](body);skin=frozen@bm;pred=np.zeros_like(points)
  for begin in range(0,len(raw),2048):
   end=min(len(raw),begin+2048)
   lo,hi=int(ptr[begin]),int(ptr[end]);rows=np.repeat(np.arange(end-begin),np.diff(ptr[begin:end+1]));terms=np.einsum('nij,nj->ni',skin[ix[lo:hi]],hp[begin:end][rows])[:,:3]*w[lo:hi,None];np.add.at(pred[begin:end],rows,terms)
  err=float(np.linalg.norm(points-pred,axis=1).max());assert err<1e-5
  if clip=='dance_01':assert np.array_equal(frozen,np.load(O/'actual-primary-v1'/f'dance{int(phase*100)}-actual-bone-proof.npz')['nativeWorld'])
  report['poseGates'].append({'name':label,'phase':phase,'wholeOriginalLBSMaxM':err,'normals':normals(),'nativeMatrixSHA256':hashlib.sha256(frozen.tobytes()).hexdigest()})
  for view in (['front','side'] if clip=='dance_01' else ['front']):render(label+'-after-native67',phase,points,view)
  if label=='BOX-start-diagnostic':
   for detail,bones,scale in [('head',['Head'],.24),('handR',['Right_Hand'],.18),('handL',['Left_Hand'],.18),('feet',['Left_Foot','Right_Foot'],.30)]:
    target=np.mean([np.array(donor.matrix_world@donor.pose.bones[n].head) for n in bones],axis=0);target+=np.array([0,0,.04]) if detail=='head' else 0;render('detail-'+detail,phase,points,'front',(target,scale))
  assert np.array_equal(frozen,np.array([donor.matrix_world@donor.pose.bones[n].matrix for n in names]))
 assert u.geometry_signature(body)==geometry and u.material_signature(body)==mat and u.bind_signature(donor)==bind and u.file_sha(csr)==entry['sha256'];report['sourcePostExitSHA']={k:u.file_sha(ROOT/ns['meta'][k]['source']) for k in ['original','nativeDonor']};assert all(report['sourcePostExitSHA'][k]==ns['meta'][k]['sha256'] for k in ['original','nativeDonor']);report['preservationPassed']=True;report['status']='COMPLETED_TEXTURED_SELECTED_GATE_VISUAL_REVIEW_REQUIRED';save()
except BaseException as exc:report.update(status='FAILED_TEXTURED_GATE_NO_ACCEPTANCE',error=repr(exc));save();raise

"""PREPARED OFFLINE: one combined full-density native67 review FBX + reimport.
No source writes. Requires explicit CPU6/600s engine handoff.
"""
import os
for k in ['OMP_NUM_THREADS','OPENBLAS_NUM_THREADS','MKL_NUM_THREADS']:os.environ[k]='6'
import argparse,sys,json,time,math,hashlib,shutil,importlib.util,itertools
from pathlib import Path
import numpy as np,bpy
from mathutils import Matrix,Vector
O=Path(__file__).resolve().parents[1];ROOT=O.parents[2];sys.path.insert(0,str(O/'tools'));from native67_interchange_core import csr_support_bounds,error_bound,source_id_uv
sys.path.insert(0,str(ROOT/'art/drafts/newrig-sailor-v1/tools/adaptive'));from curve_core import bone_angle_deg
p=argparse.ArgumentParser();p.add_argument('--output',type=Path,required=True);p.add_argument('--max-seconds',type=float,default=600);p.add_argument('--sample-hz',type=int,choices=(96,192),default=96);args=p.parse_args(sys.argv[sys.argv.index('--')+1:]);OUT=args.output.resolve();assert OUT.is_relative_to(O.resolve()) and not OUT.exists();assert shutil.disk_usage(O).free>=8*2**30;OUT.mkdir();start=time.monotonic();report={'status':'RUNNING_INTERCHANGE_NO_ACCEPTANCE','limits':[f'Derived{args.sample_hz}Hz carrier, not original curve-bit preservation.','Shifted120Hz finite holds, not continuous guarantee.','BOX-start diagnostic is not an authored guard/idle.','Unity typed Unlimited readback remains separate.']}
def sha(f):return hashlib.sha256(Path(f).read_bytes()).hexdigest()
def save():report['elapsedSeconds']=time.monotonic()-start;(OUT/'report.json').write_text(json.dumps(report,indent=2)+'\n')
def budget(stage):
 if time.monotonic()-start>args.max_seconds:raise RuntimeError('600s wall budget exhausted: '+stage)
 print('PROGRESS '+json.dumps({'stage':stage,'seconds':time.monotonic()-start}),flush=True);save()
path=ROOT/'.tools/tripo-dense-review/reconstruct_spatial_weights.py';spec=importlib.util.spec_from_file_location('readonly_fidelity_utils',path);u=importlib.util.module_from_spec(spec);spec.loader.exec_module(u)
try:
 C=O/'spatial-candidate-v1';cm=json.loads((C/'LOCAL-ARTIFACTS.json').read_text());
 for f in cm['sourcePostExitSHA']:assert sha(ROOT/f['path'])==f['sha256'],'Source archive changed'
 files={i['path']:i for i in cm['files']};blend=C/'SailorOriginalDenseNative67Spatial.blend';csr=C/'derived-native67-weights-csr.npz'
 for f in [blend,csr]:assert sha(f)==files[f.name]['sha256']
 bpy.ops.wm.open_mainfile(filepath=str(blend));scene=bpy.context.scene;body=bpy.data.objects['Sailor_OriginalDense_UNTOUCHED'];arm=next(m.object for m in body.modifiers if m.type=='ARMATURE');assert arm.parent is None and body.parent is None;assert len(arm.data.bones)==67 and all(b.bbone_segments==1 for b in arm.data.bones);assert not any(m.type!='ARMATURE' or m.use_deform_preserve_volume for m in body.modifiers)
 z=np.load(csr);names=list(z['boneNames']);ptr=z['indptr'];ix=z['indices'];weights=z['weights'];N=len(body.data.vertices);assert N==994452 and len(weights)==2922048 and np.all(weights>0);assert body.data.shape_keys is None,'Unexpected shape targets forbidden';assert set(names)==set(b.name for b in arm.data.bones)
 geometry=u.geometry_signature(body);material=u.material_signature(body);binding=u.bind_signature(arm);assert geometry==json.loads((C/'spatial-report.json').read_text())['geometryBefore'];assert material==json.loads((C/'spatial-report.json').read_text())['materialBeforeSHA256'];raw=np.empty(N*3,np.float32);body.data.vertices.foreach_get('co',raw);raw=raw.reshape(-1,3);loops=np.empty(len(body.data.loops),np.int32);body.data.loops.foreach_get('vertex_index',loops);uv=np.empty(len(loops)*2,np.float32);body.data.uv_layers.active.data.foreach_get('uv',uv);uv=uv.reshape(-1,2);meshWorld=np.array(body.matrix_world);rest=np.array([arm.matrix_world@arm.data.bones[n].matrix_local for n in names]);invRest=np.linalg.inv(rest);fixedArmWorld=np.array(arm.matrix_world)
 for v in body.data.vertices:
  actual={body.vertex_groups[g.group].name:g.weight for g in v.groups if g.weight>0};expected={names[int(ix[k])]:float(weights[k]) for k in range(ptr[v.index],ptr[v.index+1])};assert actual==expected
 report.update(sourceGeometry=geometry,sourceMaterialSHA256=material,sourceNativeBinding=binding,weightArchiveSHA256=sha(csr),weights=cm['inferredWeights'],sourceFiles=cm['sourcePostExitSHA']);budget('source full positive readback')
 # Derived review-only provenance UV, original UV channels/active/render remain unchanged.
 AUX='__LF_SOURCE_VERTEX_ID_V1';m=body.data;originalUVs={}
 for layer in m.uv_layers:
  arr=np.empty(len(loops)*2,np.float32);layer.data.foreach_get('uv',arr);originalUVs[layer.name]=arr.reshape(-1,2)
 assert AUX not in originalUVs and len(originalUVs)<8 and len(np.unique(loops))==N
 activeBefore=m.uv_layers.active.name;renderBefore=next(layer.name for layer in m.uv_layers if layer.active_render);auxCoordinates=source_id_uv(loops,N);aux=m.uv_layers.new(name=AUX,do_init=False);aux.data.foreach_set('uv',auxCoordinates.ravel());aux.active_render=False;m.uv_layers[renderBefore].active_render=True;m.uv_layers.active_index=list(originalUVs).index(activeBefore)
 assert m.uv_layers.active.name==activeBefore and next(layer.name for layer in m.uv_layers if layer.active_render)==renderBefore
 def original_geometry(ob):
  sig=u.geometry_signature(ob);sig['uv_sha256'].pop(AUX,None);return sig
 assert original_geometry(body)==geometry
 report['reviewUVProvenance']={'name':AUX,'encoding':'U=originalVertexID/1048576,V=0','denominator':1048576,'sourceOriginalVertexCount':N,'sourceTriangleCornerCount':len(loops),'originalVertexCoverage':len(np.unique(loops)),'allOriginalUVNamesInOrder':list(originalUVs),'derivedLayerIndex':len(originalUVs),'activeUVBefore':activeBefore,'renderUVBefore':renderBefore,'activeUVAfter':m.uv_layers.active.name,'renderUVAfter':next(layer.name for layer in m.uv_layers if layer.active_render),'perLoopCoordinateFloat32SHA256':hashlib.sha256(auxCoordinates.tobytes()).hexdigest(),'originalUVChannelsUnchanged':True,'runtimeRemoval':'Separate reviewed step only after full seam/corner/weight parity; no automatic stripping.'}

 expected={'box_02','cheer','dance_01','defeat_02','front_kick_01','greet_01','hit_to_head','walk'};actions={a.name.split('|')[-1]:a for a in bpy.data.actions if a.name.split('|')[-1] in expected};assert set(actions)==expected and len(actions)==8
 for ob in list(bpy.data.objects):
  if ob not in [body,arm] and ob.type not in ['CAMERA','LIGHT']:bpy.data.objects.remove(ob,do_unlink=True)
 for a in list(bpy.data.actions):
  if a not in actions.values():bpy.data.actions.remove(a,do_unlink=True)
 for m in body.modifiers:m.show_viewport=False
 # Source action metadata and all keyframes remain separate from resampled review carrier.
 curves=[];keyArrays={};sourceActionHash={}
 for label,a in actions.items():
  h=hashlib.sha256();rows=[]
  for j,fc in enumerate(a.fcurves):
   arr=np.array([[*k.co,*k.handle_left,*k.handle_right] for k in fc.keyframe_points],np.float64);key=f'{label}_{j}';keyArrays[key]=arr;meta={'dataPath':fc.data_path,'arrayIndex':fc.array_index,'extrapolation':fc.extrapolation,'array':key,'interpolation':[k.interpolation for k in fc.keyframe_points],'handleTypes':[[k.handle_left_type,k.handle_right_type] for k in fc.keyframe_points]};h.update(json.dumps(meta,sort_keys=True).encode());h.update(arr.tobytes());rows.append(meta)
  sourceActionHash[label]=h.hexdigest();curves.append({'nativeAction':a.name,'semantic':label,'startFrame':float(a.frame_range[0]),'endFrame':float(a.frame_range[1]),'fps':24,'durationSeconds':float((a.frame_range[1]-a.frame_range[0])/24),'curveMetadata':rows,'SHA256':h.hexdigest()})
 (OUT/'source-eight-action-metadata.json').write_text(json.dumps(curves,indent=2)+'\n');np.savez_compressed(OUT/'source-eight-keyframes.npz',**keyArrays);report['sourceActionHashes']=sourceActionHash
 # Actual weighted-surface bounds repair four previously failed detail cameras.
 a=actions['box_02'];arm.animation_data.action=a;scene.frame_set(int(a.frame_range[0]));bpy.context.view_layer.update()
 for m in body.modifiers:m.show_viewport=True
 bpy.context.view_layer.update();ob=body.evaluated_get(bpy.context.evaluated_depsgraph_get());me=ob.to_mesh();pp=np.empty(N*3,np.float32);me.vertices.foreach_get('co',pp);ob.to_mesh_clear();points=pp.reshape(-1,3)@meshWorld[:3,:3].T+meshWorld[:3,3]
 for m in body.modifiers:m.show_viewport=False
 cam=scene.camera;cd=cam.data;scene.render.resolution_x=640;scene.render.resolution_y=800;scene.render.film_transparent=True;scene.render.image_settings.file_format='PNG';scene.render.engine='CYCLES';scene.cycles.samples=12;scene.render.threads_mode='FIXED';scene.render.threads=6;scene.view_layers[0].material_override=None;scene.view_settings.view_transform='AgX';scene.view_settings.exposure=-2
 rows=np.repeat(np.arange(N),np.diff(ptr));details={'head':[i for i,n in enumerate(names) if n in ['Head','Neck']],'handR':[i for i,n in enumerate(names) if n.startswith('Right_') and any(t in n for t in ['Hand','Thumb','Index','Middle','Ring','Pinky'])],'handL':[i for i,n in enumerate(names) if n.startswith('Left_') and any(t in n for t in ['Hand','Thumb','Index','Middle','Ring','Pinky'])],'feet':[i for i,n in enumerate(names) if n in ['Left_Foot','Right_Foot','Left_Toes','Right_Toes']]};report['detailShots']=[]
 for label,cols in details.items():
  budget('weighted detail '+label);selected=np.isin(ix,cols);mass=np.bincount(rows[selected],weights=weights[selected],minlength=N);ids=np.flatnonzero(mass>.5);assert len(ids)>100;part=points[ids];target=(part.min(0)+part.max(0))*.5;cam.location=Vector(target)+Vector((0,-2,.06));cam.rotation_euler=(Vector(target)-cam.location).to_track_quat('-Z','Y').to_euler();cd.ortho_scale=1.;bpy.context.view_layer.update();M=np.array(cam.matrix_world.inverted());q=part@M[:3,:3].T+M[:3,3];frame=np.array([list(v) for v in cd.view_frame(scene=scene)]);fw=np.ptp(frame[:,0]);fh=np.ptp(frame[:,1]);cd.ortho_scale=float(max(abs(q[:,0]).max()/(fw*.40),abs(q[:,1]).max()/(fh*.40)));frame=np.array([list(v) for v in cd.view_frame(scene=scene)]);uvx=(q[:,0]-frame[:,0].min())/np.ptp(frame[:,0]);uvy=(q[:,1]-frame[:,1].min())/np.ptp(frame[:,1]);assert min(uvx.min(),uvy.min())>=.09 and max(uvx.max(),uvy.max())<=.91
  scene.render.filepath=str(OUT/f'BOX-start-diagnostic-detail-{label}.png');bpy.ops.render.render(write_still=True);report['detailShots'].append({'file':Path(scene.render.filepath).name,'cameraSelectionOnly':'family total weight>.5; NO source weight pruning','selectedVertices':len(ids),'familyNames':[names[i] for i in cols],'projectedBounds':[float(uvx.min()),float(uvx.max()),float(uvy.min()),float(uvy.max())]})
 # Raw source image bytes, not Blender save/re-encode. Path pointers may reference owned copies.
 maps=OUT/'original-maps';maps.mkdir();images=[];seen=set()
 for mat in body.data.materials:
  for node in mat.node_tree.nodes:
   if node.type!='TEX_IMAGE' or not node.image or node.image.as_pointer() in seen:continue
   im=node.image;seen.add(im.as_pointer());blob=bytes(im.packed_file.data) if im.packed_file else Path(bpy.path.abspath(im.filepath)).read_bytes();ext='.png' if blob.startswith(b'\x89PNG') else '.jpg' if blob.startswith(b'\xff\xd8') else None;assert ext;dest=maps/f'original-{len(images)}{ext}';dest.write_bytes(blob);digest=hashlib.sha256(blob).hexdigest();assert sha(dest)==digest;images.append({'image':im.name,'file':str(dest.relative_to(OUT)),'SHA256':digest,'dimensions':list(im.size),'colorSpace':im.colorspace_settings.name});im.filepath=str(dest)
 report['rawImages']=images;shutil.copyfile(ROOT/'docs/testing/dense-rigify-material-source-2026-10-18.json',OUT/'original-material-node-reference.json')
 corners,active,maxsum=csr_support_bounds(raw,ptr,ix,weights,67);times=[];clipIndex=[];world=[];report['sourceTakeTiming']=[]
 for index,label in enumerate(sorted(actions)):
  a=actions[label];arm.animation_data.action=a;first,last=map(float,a.frame_range);duration=(last-first)/24;ts=np.unique(np.r_[0.,np.arange(0,duration,1/120)+.37/120,duration]);ts=ts[(ts>=0)&(ts<=duration)];report['sourceTakeTiming'].append({'semantic':label,'duration':duration,'holdouts':len(ts)})
  for j,t in enumerate(ts):
   frame=first+t*24;scene.frame_set(math.floor(frame),subframe=frame%1);bpy.context.view_layer.update();world.append(np.array([arm.matrix_world@arm.pose.bones[n].matrix for n in names]));times.append(t);clipIndex.append(index)
   if j%256==0:budget('source holds '+label+' '+str(j))
 world=np.array(world);times=np.array(times);clipIndex=np.array(clipIndex);np.savez_compressed(OUT/'original-source-offkey-oracle.npz',times=times,clipIndex=clipIndex,clipNames=np.array(sorted(actions)),boneNames=np.array(names),worldPose=world,restWorld=rest,meshWorld=meshWorld,corners=corners,active=active,maxWeightSum=maxsum)
 arm.animation_data.action=None
 arm.matrix_world=Matrix(fixedArmWorld) # Restore authoritative object bind, not an animated final-frame default.
 for pb in arm.pose.bones:pb.matrix_basis=Matrix.Identity(4)
 bpy.context.view_layer.update();assert original_geometry(body)==geometry and u.material_signature(body)==material and u.bind_signature(arm)==binding
 bpy.ops.object.select_all(action='DESELECT');body.select_set(True);arm.select_set(True);bpy.context.view_layer.objects.active=arm;scene.render.fps=24;scene.render.fps_base=1.;dest=OUT/f'SailorOriginalDenseNative67-EightReview{args.sample_hz}-Tspace.fbx';budget('export combined native67/mesh/8takes')
 bpy.ops.export_scene.fbx(filepath=str(dest),use_selection=True,object_types={'MESH','ARMATURE'},use_mesh_modifiers=False,use_tspace=True,add_leaf_bones=False,use_armature_deform_only=False,axis_forward='-Z',axis_up='Y',apply_unit_scale=True,bake_space_transform=False,path_mode='RELATIVE',embed_textures=False,bake_anim=True,bake_anim_use_all_actions=True,bake_anim_use_nla_strips=False,bake_anim_simplify_factor=0.,bake_anim_step=24./args.sample_hz)
 for label,a in actions.items():
  h=hashlib.sha256()
  for j,fc in enumerate(a.fcurves):
   arr=np.array([[*k.co,*k.handle_left,*k.handle_right] for k in fc.keyframe_points],np.float64);meta={'dataPath':fc.data_path,'arrayIndex':fc.array_index,'extrapolation':fc.extrapolation,'array':f'{label}_{j}','interpolation':[k.interpolation for k in fc.keyframe_points],'handleTypes':[[k.handle_left_type,k.handle_right_type] for k in fc.keyframe_points]};h.update(json.dumps(meta,sort_keys=True).encode());h.update(arr.tobytes())
  assert h.hexdigest()==sourceActionHash[label],'Original source action mutated during export'
 assert original_geometry(body)==geometry and u.material_signature(body)==material;report['export']={'file':dest.name,'SHA256':sha(dest),'bytes':dest.stat().st_size,'sampleHz':args.sample_hz,'originalCurveBitsClaim':False};budget('raw full attribute audit')
 import audit_sources_offline as reader;from native67_interchange_core import raw_fbx_attributes
 report['rawFBX']=raw_fbx_attributes(dest,raw,loops,next(iter(originalUVs.values())),ptr,ix,weights,names,reader,originalUVs,{'name':AUX,'coordinates':auxCoordinates});assert report['rawFBX']['rawTangentLayerPresent'] and report['rawFBX']['rawBinormalLayerPresent'];save()
 # True independent clean scene reimport; one skinned authority, no animation-only action transplant.
 bpy.ops.wm.read_factory_settings(use_empty=True);bpy.ops.import_scene.fbx(filepath=str(dest),automatic_bone_orientation=False,use_anim=True);scene=bpy.context.scene;arm=next(o for o in scene.objects if o.type=='ARMATURE');body=next(o for o in scene.objects if o.type=='MESH');assert len([o for o in scene.objects if o.type=='MESH'])==1 and set(names)==set(b.name for b in arm.data.bones)
 for m in body.modifiers:m.show_viewport=False
 co=np.empty(len(body.data.vertices)*3,np.float32);body.data.vertices.foreach_get('co',co);co=co.reshape(-1,3);assert len(co)==N;Cworld=np.array(body.matrix_world);restError=float(np.linalg.norm((co@Cworld[:3,:3].T+Cworld[:3,3])-(raw@meshWorld[:3,:3].T+meshWorld[:3,3]),axis=1).max());assert restError<1e-5,'Index/world rest mismatch, no unchecked correspondence';li=np.empty(len(body.data.loops),np.int32);body.data.loops.foreach_get('vertex_index',li);uv2=np.empty(len(li)*2,np.float32);body.data.uv_layers[activeBefore].data.foreach_get('uv',uv2);assert np.array_equal(li,loops) and np.array_equal(uv2.reshape(-1,2),uv)
 assert [layer.name for layer in body.data.uv_layers]==[*originalUVs,AUX]
 for lname,values in originalUVs.items():
  got=np.empty(len(li)*2,np.float32);body.data.uv_layers[lname].data.foreach_get('uv',got);assert np.array_equal(got.reshape(-1,2),values),'Reimport original UV changed'
 got=np.empty(len(li)*2,np.float32);body.data.uv_layers[AUX].data.foreach_get('uv',got);got=got.reshape(-1,2);assert np.array_equal(got,auxCoordinates),'Reimport review coordinates not bitexact';decoded=got[:,0]*np.float32(1048576);assert np.array_equal(decoded,li.astype(np.float32)) and len(np.unique(decoded))==N;report['reviewUVProvenance']['BlenderReimportBitExact']=True;report['reviewUVProvenance']['BlenderImportedActiveUV']=body.data.uv_layers.active.name;report['reviewUVProvenance']['actualFBXLayerOrder']=report['rawFBX']['UVLayers']
 for v in body.data.vertices:
  actual={body.vertex_groups[g.group].name:g.weight for g in v.groups if g.weight>0};expected={names[int(ix[k])]:float(weights[k]) for k in range(ptr[v.index],ptr[v.index+1])};assert actual==expected,'Blender positive named weight loss/renorm'
 imported={a.name.split('|')[-1]:a for a in bpy.data.actions};assert set(imported)==set(actions.keys()),'Imported take set differs from exact source8'
 candidateRest=np.array([arm.matrix_world@arm.data.bones[n].matrix_local for n in names]);invCandidate=np.linalg.inv(candidateRest);K=invRest@candidateRest;fps=scene.render.fps/scene.render.fps_base;maxBound=0.;maxAngle=0.;worst=None;rowsOut=[]
 for i,(t,cid) in enumerate(zip(times,clipIndex)):
  label=sorted(actions)[cid];a=imported[label];duration=report['sourceTakeTiming'][cid]['duration'];actualDuration=float((a.frame_range[1]-a.frame_range[0])/fps);assert abs(actualDuration-duration)<1e-5,'Authored duration changed';arm.animation_data.action=a;frame=float(a.frame_range[0])+t/duration*float(a.frame_range[1]-a.frame_range[0]);scene.frame_set(math.floor(frame),subframe=frame%1);bpy.context.view_layer.update();pose=np.array([arm.matrix_world@arm.pose.bones[n].matrix for n in names]);sourceSkin=world[i]@invRest@meshWorld;candidateSkin=pose@invCandidate@Cworld;bound=error_bound(sourceSkin,candidateSkin,corners,active,maxsum);angle=bone_angle_deg(world[i]@K,pose);rowsOut.append((float(t),int(cid),bound,angle))
  if bound>maxBound:maxBound=bound;worst={'clip':label,'seconds':float(t),'boundM':bound,'boneDeg':angle}
  maxAngle=max(maxAngle,angle)
  if i%256==0:budget('independent reimport holds '+str(i))
 report['reimport']={'vertices':N,'triangles':1988914,'allNamedPositiveWeightsExact':True,'UVOrientedTopologyExact':True,'bindWorldMaxM':restError,'holdouts':len(times),'maxSurfaceBoundM':maxBound,'maxBoneDeg':maxAngle,'worst':worst,'fixedGauge':'K=inv(sourceBindWorld)*importBindWorld, never moving fit','scope':'All-original-positive-support bound, shifted120Hz+exact0/end finite samples, not continuous proof'};np.savez_compressed(OUT/'reimport-holdout-metrics.npz',rows=np.array(rowsOut));assert maxBound<=.0001 and maxAngle<=.01,'Interchange offkey RED; no threshold waiver/rebake'
 for f in cm['sourcePostExitSHA']:assert sha(ROOT/f['path'])==f['sha256']
 report['status']='PASS_SELECTED_INTERCHANGE_PENDING_UNITY_AND_SKIN_QUALITY';save()
except BaseException as exc:report.update(status='FAILED_INTERCHANGE_NO_ACCEPTANCE',error=repr(exc));save();raise

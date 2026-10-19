"""Derived96Hz motion carrier; full geometry/UV/positive weights and off-key audit."""
import bpy,sys,json,hashlib,math
import numpy as np
from pathlib import Path
R=Path(__file__).resolve().parents[3];role=sys.argv[sys.argv.index('--')+1];O=R/('art/drafts/full-roster-v1/'+role+'-native-motion');O.mkdir(exist_ok=True);source=R/('art/drafts/full-roster-v1/'+role+'-native67-bary/original-dense-native-spatial.blend');sourceHash=hashlib.sha256(source.read_bytes()).hexdigest();bpy.ops.wm.open_mainfile(filepath=str(source));body=bpy.data.objects['OriginalDenseNative'];arm=next(m.object for m in body.modifiers if m.type=='ARMATURE');arm.animation_data_create();arm.data.pose_position='POSE';N=len(body.data.vertices)
def coords(mesh,prop,width,dtype=np.float32):
 a=np.empty(len(mesh)*width,dtype);mesh.foreach_get(prop,a);return a.reshape(-1,width) if width>1 else a
positions=coords(body.data.vertices,'co',3);loops=coords(body.data.loops,'vertex_index',1,np.int32);uvs={uv.name:coords(uv.data,'uv',2) for uv in body.data.uv_layers};weights=[{body.vertex_groups[g.group].name:g.weight for g in v.groups if g.weight>0} for v in body.data.vertices];assert all(weights);actions={a.name.split('|')[-1]:a for a in bpy.data.actions};expected=set(next(x for x in json.loads((R/'art/drafts/full-roster-v1/native-clip-inventory.json').read_text()) if x['id']==role)['nativeClips']);assert set(actions)==expected,(list(actions),expected)
manifest=json.loads((R/('art/drafts/full-roster-v1/'+role+'-native67-bary/manifest.json')).read_text());maps={m['sha256']:m['path'] for m in manifest['original_maps']['images']};bindings=[]
for mat in body.data.materials:
 for node in mat.node_tree.nodes:
  if node.type=='TEX_IMAGE' and node.image:
   im=node.image;blob=im.packed_file.data if im.packed_file else Path(bpy.path.abspath(im.filepath)).read_bytes();sha=hashlib.sha256(blob).hexdigest();assert sha in maps,('Unmatched original image',im.name);path=Path(maps[sha]);path=path if path.is_absolute() else R/path;assert hashlib.sha256(path.read_bytes()).hexdigest()==sha;im.filepath=str(path);bindings.append({'node':node.name,'image':im.name,'source':str(path),'sha256':sha,'colorSpace':im.colorspace_settings.name})
# Actual complete weighted surfaces in off-key native samples; original arrays remain immutable.
def actual(ob):
 ev=ob.evaluated_get(bpy.context.evaluated_depsgraph_get());m=ev.to_mesh();a=coords(m.vertices,'co',3);matrix=np.asarray(ob.matrix_world);out=a@matrix[:3,:3].T+matrix[:3,3];ev.to_mesh_clear();return out
scene=bpy.context.scene;fps=scene.render.fps/scene.render.fps_base;cases=[];samples={};labels=['box_02','hit_to_head']+(['front_kick_01'] if 'front_kick_01' in actions else [])
for label in labels:
 action=actions[label];arm.animation_data.action=action
 for phase in [.173,.417,.683]:
  frame=action.frame_range[0]+phase*(action.frame_range[1]-action.frame_range[0]);scene.frame_set(math.floor(frame),subframe=frame%1);bpy.context.view_layer.update();samples[(label,phase)]=actual(body).astype(np.float32)
for label in labels:
 a=samples[(label,.173)];b=samples[(label,.683)];assert np.linalg.norm(a-b,axis=1).max()>1e-4,('Static animation witness',label)
print('PROGRESS source native motion witnessed',flush=True)
motionWitness={label:float(np.linalg.norm(samples[(label,.173)]-samples[(label,.683)],axis=1).max()) for label in labels}
timings={label:float((a.frame_range[1]-a.frame_range[0])/fps) for label,a in actions.items()}
# Derived source IDs preserve original UV layer and raw seam correspondence.
aux=body.data.uv_layers.new(name='__LF_SOURCE_VERTEX_ID_V1',do_init=False);ids=np.zeros((len(loops),2),np.float32);ids[:,0]=loops/np.float32(2097152);aux.data.foreach_set('uv',ids.ravel());body.data.uv_layers.active_index=0
# Bind all authored actions once; exporter Blender4.2 requires object action id_root.
for a in actions.values():arm.animation_data.action=a;assert a.id_root=='OBJECT'
arm.animation_data.action=None
for p in arm.pose.bones:p.matrix_basis.identity()
for o in bpy.context.scene.objects:o.select_set(o in [body,arm])
for modifier in body.modifiers:modifier.show_viewport=False
print('PROGRESS native motion export starting',flush=True)
bpy.context.view_layer.objects.active=arm;dest=R/('unity/Assets/Native67SpatialReview/'+role.title()+'DenseMotionReview.fbx');bpy.ops.export_scene.fbx(filepath=str(dest),use_selection=True,object_types={'MESH','ARMATURE'},use_mesh_modifiers=False,use_tspace=True,add_leaf_bones=False,use_armature_deform_only=False,axis_forward='-Z',axis_up='Y',apply_unit_scale=True,bake_space_transform=False,path_mode='COPY',embed_textures=False,bake_anim=True,bake_anim_use_all_actions=True,bake_anim_use_nla_strips=False,bake_anim_simplify_factor=0,bake_anim_step=fps/96)
bpy.ops.wm.read_factory_settings(use_empty=True);bpy.ops.import_scene.fbx(filepath=str(dest),use_anim=True,automatic_bone_orientation=False);body=next(o for o in bpy.context.scene.objects if o.type=='MESH');arm=next(o for o in bpy.context.scene.objects if o.type=='ARMATURE');arm.data.pose_position='POSE';assert len(body.data.vertices)==N;assert np.array_equal(coords(body.data.vertices,'co',3),positions),'Original positions changed';assert np.array_equal(coords(body.data.loops,'vertex_index',1,np.int32),loops),'Oriented topology changed'
for name,uv in uvs.items():assert np.array_equal(coords(body.data.uv_layers[name].data,'uv',2),uv),'UV changed'
for v in body.data.vertices:
 got={body.vertex_groups[g.group].name:g.weight for g in v.groups if g.weight>0};assert got==weights[v.index],('Positive coefficient changed',v.index)
imported={a.name.split('|')[-1]:a for a in bpy.data.actions};assert set(imported)==set(actions),'Authored take set changed'
for label,a in imported.items():assert abs(float((a.frame_range[1]-a.frame_range[0])/(bpy.context.scene.render.fps/bpy.context.scene.render.fps_base))-timings[label])<1e-5,('Clip duration changed',label)
assert np.array_equal(coords(body.data.uv_layers['__LF_SOURCE_VERTEX_ID_V1'].data,'uv',2),ids),'Source IDs changed'
for (label,phase),expected in samples.items():
 action=imported[label];arm.animation_data.action=action;frame=action.frame_range[0]+phase*(action.frame_range[1]-action.frame_range[0]);bpy.context.scene.frame_set(math.floor(frame),subframe=frame%1);bpy.context.view_layer.update();got=actual(body);error=np.linalg.norm(got-expected,axis=1);case={'clip':label,'phase':phase,'maxSurfaceErrorM':float(error.max()),'p99SurfaceErrorM':float(np.quantile(error,.99))};cases.append(case);print('POSE '+json.dumps(case),flush=True)
report={'status':'PASS_NATIVE_MOTION_CARRIER_PENDING_UNITY' if max(c['maxSurfaceErrorM'] for c in cases)<=.0001 else 'FAILED_NATIVE_OFFKEY_NO_PROMOTION','role':role,'triangles':sum(len(p.vertices)-2 for p in body.data.polygons),'vertices':N,'allOriginalPositionsTopologyUVExact':True,'allPositiveNamedWeightsExact':True,'actions':list(imported),'sampleHz':96,'sourceIdDenominator':2097152,'originalTakeDurations':timings,'sourceMotionWitnessMaxDisplacementM':motionWitness,'offKeyCases':cases,'originalSourceSHA256':sourceHash,'sourceUnchanged':hashlib.sha256(source.read_bytes()).hexdigest()==sourceHash,'fbx':str(dest.relative_to(R)),'fbxSHA256':hashlib.sha256(dest.read_bytes()).hexdigest(),'textureBindings':bindings,'limits':['96Hz derived animation carrier, not original key bit preservation','FBX PBR material equivalence still needs explicit Unity inspection','Finite off-key surface samples, not continuous guarantee or phone acceptance']};(O/'report.json').write_text(json.dumps(report,indent=2));assert report['sourceUnchanged'];assert report['status'].startswith('PASS'),report;print('RESULT '+report['status'])

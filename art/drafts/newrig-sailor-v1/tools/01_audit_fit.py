"""Read-only source audit; independent disposable proxy; fresh fitted Rigify metarig.
Run Blender --background --factory-startup --python-exit-code 1 --python this_file.
No source modification and no dense automatic weights. Output is a draft, not acceptance.
"""
import bpy,sys,json,hashlib,time,types
from pathlib import Path
import numpy as np
ROOT=Path(__file__).resolve().parents[4]; OUT=ROOT/'art/drafts/newrig-sailor-v1';OUT.mkdir(exist_ok=True)
SOURCE=next((ROOT/'art/source/tripo-striped-sailor-apose-original/unpacked').glob('*.fbx'))
def hash_arrays(obj):
 m=obj.data;h={}
 for name,collection,attr,width,dtype in [('positions',m.vertices,'co',3,np.float32),('loopVertexIndices',m.loops,'vertex_index',1,np.int32),('polygonStarts',m.polygons,'loop_start',1,np.int32),('polygonSizes',m.polygons,'loop_total',1,np.int32)]:
  a=np.empty(len(collection)*width,dtype=dtype);collection.foreach_get(attr,a);h[name]=hashlib.sha256(a.tobytes()).hexdigest()
 for uv in m.uv_layers:
  a=np.empty(len(m.loops)*2,dtype=np.float32);uv.data.foreach_get('uv',a);h['uv:'+uv.name]=hashlib.sha256(a.tobytes()).hexdigest()
 return h
def write(x): (OUT/'stage01.json').write_text(json.dumps(x,indent=2))
bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False)
bpy.ops.import_scene.fbx(filepath=str(SOURCE),use_anim=False,automatic_bone_orientation=False)
meshes=[o for o in bpy.context.scene.objects if o.type=='MESH'];assert len(meshes)==1
body=meshes[0];body.name='Sailor_OriginalDense_UNTOUCHED';before=hash_arrays(body);m=body.data
sizes=np.empty(len(m.polygons),np.int32);m.polygons.foreach_get('loop_total',sizes)
edgeIndices=np.empty(len(m.loops),np.int32);m.loops.foreach_get('edge_index',edgeIndices);users=np.bincount(edgeIndices,minlength=len(m.edges))
coords=np.empty(len(m.vertices)*3,np.float32);m.vertices.foreach_get('co',coords);coords=coords.reshape(-1,3)
world=coords@np.array(body.matrix_world)[:3,:3].T+np.array(body.matrix_world)[:3,3]
report={'status':'AUDIT_COMPLETE_FIT_PENDING','blenderVersion':bpy.app.version_string,'source':str(SOURCE.relative_to(ROOT)),'sourceSHA256':hashlib.sha256(SOURCE.read_bytes()).hexdigest(),'vertices':len(m.vertices),'polygons':len(m.polygons),'triangles':int(np.sum(sizes-2)),'edges':len(m.edges),'boundaryEdges':int(np.count_nonzero(users==1)),'overTwoFaceEdges':int(np.count_nonzero(users>2)),'looseEdges':int(np.count_nonzero(users==0)),'allTriangles':bool(np.all(sizes==3)),'uvLayers':[u.name for u in m.uv_layers],'existingArmatures':len([o for o in bpy.context.scene.objects if o.type=='ARMATURE']),'existingVertexGroups':len(body.vertex_groups),'worldMin':world.min(axis=0).tolist(),'worldMax':world.max(axis=0).tolist(),'sourceAttributesSHA256':before,'notes':['No original mesh transform, vertex, UV, polygon or material modification.','Decimation applies ONLY to an independent binding proxy, never deliverable geometry.']};write(report);print('SOURCE_AUDIT '+json.dumps(report),flush=True)
# Reuse skill algorithms on manageable proxy. Blender4.2 lacks5.2 media_type.
code=(Path.home()/'.agents/skills/scenario-blender-rigging/scripts/bx_rig.py').read_text().replace('    sc.render.image_settings.media_type = "IMAGE"\n','')
R=types.ModuleType('pilot_bx_rig');R.__file__=str(Path.home()/'.agents/skills/scenario-blender-rigging/scripts/bx_rig.py');exec(compile(code,R.__file__,'exec'),R.__dict__)
proxy=body.copy();proxy.data=body.data.copy();bpy.context.scene.collection.objects.link(proxy);proxy.name='BIND_PROXY_ONLY';bpy.context.view_layer.objects.active=proxy
for o in bpy.context.selected_objects:o.select_set(False)
proxy.select_set(True);mod=proxy.modifiers.new('ProxyOnlySimplification','DECIMATE');mod.ratio=min(1,50000/report['triangles']);bpy.ops.object.modifier_apply(modifier=mod.name)
report['proxyTriangles']=sum(len(p.vertices)-2 for p in proxy.data.polygons);report['proxyVertices']=len(proxy.data.vertices);write(report)
try:
 lm=R.landmarks(proxy);report['landmarks']={k:list(v) if hasattr(v,'to_tuple') else v for k,v in lm.items()};write(report)
 meta=R.add_metarig('human',name='FreshSailor_CONTROL_METARIG_v1');R.fit_metarig(meta,lm)
 report['metarigBones']=len(meta.data.bones);report['fitProblems']=R.check_metarig(meta,proxy);report['status']='FITTED_REVIEW_REQUIRED'
 R.placement_sheet([proxy],str(OUT/'placement'),arm_obj=meta,views=('front','right','top'),res=768)
except Exception as e:
 report['fitError']=repr(e);report['status']='FIT_BLOCKED_REQUIRES_CORRECTION';write(report);raise
finally:
 report['originalAttributesUnchanged']=hash_arrays(body)==before;assert report['originalAttributesUnchanged'];write(report)
proxy.hide_render=True;bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'stage01-audit-fitted-met-rig.blend'),compress=True)
print('RESULT '+json.dumps(report),flush=True)

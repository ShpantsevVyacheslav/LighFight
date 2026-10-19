"""Full-density rig carrier for isolated Unity retarget checks. Originals read-only."""
import bpy,sys,json,hashlib
from pathlib import Path
R=Path(__file__).resolve().parents[3];role=sys.argv[sys.argv.index('--')+1];O=R/('art/drafts/full-roster-v1/'+role+'-native67-bary');bpy.ops.wm.open_mainfile(filepath=str(O/'original-dense-native-spatial.blend'));body=bpy.data.objects['OriginalDenseNative'];arm=next(m.object for m in body.modifiers if m.type=='ARMATURE');arm.animation_data_create();arm.animation_data.action=None
for p in arm.pose.bones:p.matrix_basis.identity()
bpy.context.view_layer.update()
N=len(body.data.vertices);tri=sum(len(p.vertices)-2 for p in body.data.polygons);weights=[{body.vertex_groups[g.group].name:g.weight for g in v.groups if g.weight>0} for v in body.data.vertices];assert all(weights)
# Preserve source-index correspondence through Unity UV seams without changing original UV layer.
import numpy as np
ids=np.empty(len(body.data.loops),np.int32);body.data.loops.foreach_get('vertex_index',ids);aux=body.data.uv_layers.new(name='__LF_SOURCE_VERTEX_ID_V1',do_init=False);data=np.zeros((len(ids),2),np.float32);data[:,0]=ids/np.float32(2097152);aux.data.foreach_set('uv',data.ravel());body.data.uv_layers.active_index=0
for o in bpy.context.scene.objects:o.select_set(o in [body,arm])
bpy.context.view_layer.objects.active=arm;dest=R/('unity/Assets/Native67SpatialReview/'+role.title()+'DenseReview.fbx');bpy.ops.export_scene.fbx(filepath=str(dest),use_selection=True,object_types={'MESH','ARMATURE'},use_mesh_modifiers=False,use_tspace=True,add_leaf_bones=False,use_armature_deform_only=False,axis_forward='-Z',axis_up='Y',apply_unit_scale=True,bake_space_transform=False,path_mode='COPY',embed_textures=False,bake_anim=False)
bpy.ops.wm.read_factory_settings(use_empty=True);bpy.ops.import_scene.fbx(filepath=str(dest),use_anim=False,automatic_bone_orientation=False);m=next(o for o in bpy.context.scene.objects if o.type=='MESH');assert len(m.data.vertices)==N and sum(len(p.vertices)-2 for p in m.data.polygons)==tri
for v in m.data.vertices:
 got={m.vertex_groups[g.group].name:g.weight for g in v.groups if g.weight>0};assert got==weights[v.index],('positive weight mismatch',v.index)
report={'status':'FBX_REIMPORT_POSITIVE_WEIGHTS_PASS_UNITY_PENDING','role':role,'vertices':N,'triangles':tri,'allPositiveNamedWeightsExact':True,'maxInfluences':max(map(len,weights)),'file':str(dest.relative_to(R)),'sha256':hashlib.sha256(dest.read_bytes()).hexdigest(),'sourceIdDenominator':2097152,'originalGeometryChanged':False,'limits':['Rig-only carrier; native source animations remain in original donor.','Unity imported UV seam and weight readback still required.']};(O/'fbx-export.json').write_text(json.dumps(report,indent=2));print('RESULT '+json.dumps(report))

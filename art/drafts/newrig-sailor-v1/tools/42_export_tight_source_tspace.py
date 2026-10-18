"""Prepare one authoritative full-skinned adaptive-box review FBX after full-fit PASS.
Uses verified original-source WORLD cache; no Rigify replay, no other review takes modified.
FBX exporter sampling lattice contains EVERY adaptive/authored key, no silent24Hz rebake.
"""
import bpy,json,hashlib,math,sys
from fractions import Fraction
from functools import reduce
from pathlib import Path
import numpy as np
from mathutils import Matrix
O=Path(__file__).resolve().parents[1];P=O/'dense-def-fbx-packet-v1';reportPath=P/'adaptive-box_02-FULL_ADAPTIVE_TIGHT008.json';report=json.loads(reportPath.read_text());assert report['status']=='PASS_DIAGNOSTIC_NOT_EXPORT' and report['sourceAttributesExact'];assert report['maxSurfaceBoundM']<=.0001 and report['maxBoneDeg']<=.01
cache=np.load(P/'adaptive-box_02-FULL_ADAPTIVE_TIGHT008-cache.npz');cacheWorldPose=cache['worldPose'];cacheParents=cache['parents'];assert json.loads(str(cache['sourceConfig']))==report['sourceConfig'];names=list(cache['boneNames']);times=[Fraction(int(x[0]),int(x[1])) for x in cache['times']];lookup={t:i for i,t in enumerate(times)};keys=[Fraction(*x) for x in report['acceptedKeys']];assert keys[0]==0 and keys[-1]==Fraction(67,24)
# The exact rational frame-time GCD ensures Blender FBX bake step visits all source/adaptive keys.
frameTimes=[t*24 for t in keys];den=math.lcm(*(x.denominator for x in frameTimes));numerators=[x.numerator*(den//x.denominator) for x in frameTimes];gcd=reduce(math.gcd,numerators);step=Fraction(gcd,den);frameCount=math.ceil(Fraction(67)/step)+1;assert step>0 and frameCount<=20000,('Explicit FBX sample-budget exceeded',str(step),frameCount)
exportRate=Fraction(24)/step;assert exportRate.denominator==1 and exportRate<=32767,'Exact time lattice requires a supported integer export FPS';exportFPS=int(exportRate)
bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False);bpy.ops.import_scene.fbx(filepath=str(P/'SailorOriginalDenseDEF.fbx'),automatic_bone_orientation=False,use_anim=False);body=next(o for o in bpy.context.scene.objects if o.type=='MESH');rig=next(o for o in bpy.context.scene.objects if o.type=='ARMATURE');assert set(names)==set(b.name for b in rig.data.bones);assert len(body.data.vertices)==994452;body.data.calc_loop_triangles();assert len(body.data.loop_triangles)==1988914
for i,n in enumerate(names):
 parent=rig.data.bones[n].parent;expected=int(cacheParents[i]);assert (parent.name if parent else None)==(names[expected] if expected>=0 else None),'Parent/bind gauge mismatch'
# Use the authoritative ORIGINAL source mesh, including original custom normals/UVs and vertex-group order.
sourceBlend=O/'stage02-bound-original-dense.blend';objectsBefore=set(bpy.data.objects)
with bpy.data.libraries.load(str(sourceBlend),link=False) as (available,requested):
 assert 'Sailor_OriginalDense_UNTOUCHED' in available.objects;requested.objects=['Sailor_OriginalDense_UNTOUCHED']
oldBody=body;author=requested.objects[0];assert len(author.data.vertices)==994452;bpy.context.scene.collection.objects.link(author);body=author;body.parent=rig;originalData=np.load(P/'original-indexed-attributes.npz');body.matrix_world=Matrix(originalData['meshWorldBlender'])
for mod in body.modifiers:
 assert mod.type=='ARMATURE','Unexpected source mesh modifier';mod.object=rig;mod.use_deform_preserve_volume=False
oldMesh=oldBody.data;bpy.data.objects.remove(oldBody,do_unlink=True)
if oldMesh.users==0:bpy.data.meshes.remove(oldMesh)
originalNames=json.loads((P/'manifest.json').read_text())['boneNames'];originalNameIndex={n:i for i,n in enumerate(originalNames)};weightWitness=np.zeros((994452,4),np.float32);jointWitness=np.zeros((994452,4),np.int32)
for vertex in body.data.vertices:
 assert 0<len(vertex.groups)<=4
 for k,g in enumerate(vertex.groups):
  weightWitness[vertex.index,k]=g.weight;jointWitness[vertex.index,k]=originalNameIndex[body.vertex_groups[g.group].name]
assert np.array_equal(weightWitness.view(np.uint32),originalData['weights'].view(np.uint32));assert np.array_equal(jointWitness[weightWitness>0],originalData['jointIndices'][weightWitness>0])
for ob in list(bpy.data.objects):
 if ob not in objectsBefore and ob!=body and ob!=rig:bpy.data.objects.remove(ob,do_unlink=True)
sourceCode=(O/'tools/01_audit_fit.py').read_text();exec(sourceCode[sourceCode.index('def hash_arrays'):sourceCode.index('def write')]);assert hash_arrays(body)==json.loads((O/'stage02.json').read_text())['sourceAttributesSHA256'];body.data.calc_tangents(uvmap=body.data.uv_layers.active.name)
witness=json.loads((P/'original-bind-tangent-witness.json').read_text());normalError=0.;tangentError=0.
for sample in witness['samples']:
 loop=body.data.loops[sample['loopIndex']];normalError=max(normalError,float(np.linalg.norm(np.array(loop.normal)-sample['normalLocal'])));tangentError=max(tangentError,float(np.linalg.norm(np.array(loop.tangent)-sample['tangentLocal'])));assert float(loop.bitangent_sign)==sample['bitangentSign']
assert normalError<1e-6 and tangentError<1e-6,(normalError,tangentError)
sourceImages=[]
for mat in body.data.materials:
 for node in mat.node_tree.nodes:
  if node.type!='TEX_IMAGE' or not node.image:continue
  im=node.image;blob=bytes(im.packed_file.data) if im.packed_file else Path(bpy.path.abspath(im.filepath)).read_bytes();dest=P/'Textures'/Path(im.filepath).name;assert dest.exists() and hashlib.sha256(dest.read_bytes()).hexdigest()==hashlib.sha256(blob).hexdigest();im.filepath=str(dest.resolve());sourceImages.append({'file':str(dest.relative_to(P)),'SHA256':hashlib.sha256(blob).hexdigest(),'colorSpace':im.colorspace_settings.name})
# Snapshot every raw mesh buffer before animation authoring.
def meshhash():
 h={}
 for name,col,attr,width,dtype in [('positions',body.data.vertices,'co',3,np.float32),('loopIndices',body.data.loops,'vertex_index',1,np.int32)]:
  a=np.empty(len(col)*width,dtype);col.foreach_get(attr,a);h[name]=hashlib.sha256(a.tobytes()).hexdigest()
 for uv in body.data.uv_layers:
  a=np.empty(len(body.data.loops)*2,np.float32);uv.data.foreach_get('uv',a);h['uv:'+uv.name]=hashlib.sha256(a.tobytes()).hexdigest()
 return h
before=meshhash();action=bpy.data.actions.new('Dense_box_02_adaptive_tspace_v2');rig.animation_data_create();rig.animation_data.action=action;values={n:np.empty((len(keys),10),np.float64) for n in names};previous={};maxTRS=0.
for j,t in enumerate(keys):
 worlds={n:Matrix(cacheWorldPose[lookup[t],i]) for i,n in enumerate(names)};mats={n:rig.matrix_world.inverted()@worlds[n] for n in names}
 for n in names:
  bone=rig.data.bones[n];par=bone.parent;basis=bone.convert_local_to_pose(mats[n],bone.matrix_local,parent_matrix=mats[par.name] if par else Matrix.Identity(4),parent_matrix_local=par.matrix_local if par else Matrix.Identity(4),invert=True);loc,q,scale=basis.decompose()
  if n in previous and previous[n].dot(q)<0:q.negate()
  previous[n]=q.copy();values[n][j]=[*loc,*q,*scale];recon=Matrix.Translation(loc)@q.to_matrix().to_4x4()@Matrix.Diagonal((*scale,1.));maxTRS=max(maxTRS,float(np.abs(np.array(basis)-np.array(recon)).max()))
frames=np.array([float(t*exportFPS) for t in keys],np.float32)
for n in names:
 pb=rig.pose.bones[n];pb.rotation_mode='QUATERNION'
 for path,offset,width in [('location',0,3),('rotation_quaternion',3,4),('scale',7,3)]:
  for axis in range(width):
   fc=action.fcurves.new('pose.bones['+json.dumps(n)+'].'+path,index=axis,action_group=n);fc.keyframe_points.add(len(keys));fc.keyframe_points.foreach_set('co',np.column_stack((frames,values[n][:,offset+axis])).astype(np.float32).ravel())
   for point in fc.keyframe_points:point.interpolation='LINEAR'
   fc.update()
scene=bpy.context.scene;scene.render.fps=exportFPS;scene.render.fps_base=1.;scene.frame_start=0;scene.frame_end=frameCount-1;bpy.ops.object.select_all(action='DESELECT');rig.select_set(True);body.select_set(True);bpy.context.view_layer.objects.active=rig;dest=P/'SailorOriginalDenseDEF-BoxAdaptiveTspace-v2.fbx'
bpy.ops.export_scene.fbx(filepath=str(dest),use_selection=True,object_types={'ARMATURE','MESH'},use_mesh_modifiers=False,use_tspace=True,add_leaf_bones=False,use_armature_deform_only=True,axis_forward='-Z',axis_up='Y',apply_unit_scale=True,bake_space_transform=False,path_mode='RELATIVE',embed_textures=False,bake_anim=True,bake_anim_use_all_actions=False,bake_anim_use_nla_strips=False,bake_anim_simplify_factor=0.,bake_anim_step=1.)
assert before==meshhash();out={'status':'EXPORTED_PENDING_ORIGINAL_SOURCE_AND_UNITY_REIMPORT','file':dest.name,'SHA256':hashlib.sha256(dest.read_bytes()).hexdigest(),'bytes':dest.stat().st_size,'take':'Dense_box_02_adaptive_tspace_v2','durationSeconds':67/24,'adaptiveKeys':len(keys),'FBXBakeStepFramesRational':[1,1],'exportFramesPerSecond':exportFPS,'sourceFrame24StepEquivalent':[step.numerator,step.denominator],'FBXSampleCount':frameCount,'geometryBuffersExactBeforeAfter':True,'authoritativeOriginalSourceAttributesExact':hash_arrays(body)==json.loads((O/'stage02.json').read_text())['sourceAttributesSHA256'],'use_tspace':True,'allOriginalNamedWeightBitsExact':True,'sourceNamedNonzeroInfluences':int(np.count_nonzero(weightWitness)),'sourceHasCustomNormals':bool(body.data.has_custom_normals),'sourceTangentWitnessMaxVectorError':tangentError,'sourceNormalWitnessMaxVectorError':normalError,'originalRawImages':sourceImages,'maxImportedBindBasisTRSReconstructionElementError':maxTRS,'sourceFitReportSHA256':hashlib.sha256(reportPath.read_bytes()).hexdigest(),'originalCurvePreservationClaim':False};(P/'adaptive-box-tspace-export.json').write_text(json.dumps(out,indent=2));print('RESULT '+json.dumps(out),flush=True)

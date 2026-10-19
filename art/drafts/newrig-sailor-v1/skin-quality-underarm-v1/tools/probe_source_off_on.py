"""PREPARED ONLY: original dense game-LBS source C1 OFF/ON region diagnostics.
Run only after explicit serialized Blender window. No mesh/weight/source saves.
"""
import os
os.environ.setdefault('OPENBLAS_NUM_THREADS','6')
import argparse,hashlib,json,math,sys,time
from pathlib import Path
import numpy as np
import bpy
from mathutils import Matrix,Vector,Quaternion
OWN=Path(__file__).resolve().parents[1];PILOT=OWN.parent;PACKET=PILOT/'dense-def-fbx-packet-v1';sys.path.insert(0,str(OWN/'tools'))
from prepare_regions import propose,owned_output,sha
from skin_metrics import canonical_edges,edge_metrics,triangle_metrics,section_segments,closed_section,anchor_metrics,candidate_fences,transport_normals,projected_envelope
parser=argparse.ArgumentParser();parser.add_argument('--stage',choices=['masks','metrics','renders'],default='masks');parser.add_argument('--regions',type=Path,default=OWN/'regions-v1');parser.add_argument('--output',type=Path);parser.add_argument('--case-names',nargs='*');parser.add_argument('--max-seconds',type=float,default=900);parser.add_argument('--source-material',action='store_true');parser.add_argument('--mask-region');args=parser.parse_args(sys.argv[sys.argv.index('--')+1:] if '--' in sys.argv else [])
OUT=owned_output(args.output or OWN/(args.stage+('-source-v1' if args.source_material else '-clay-v1')));REG=owned_output(args.regions);started=time.monotonic();report={'status':'RUNNING_DIAGNOSTIC_NO_CORRECTION','stage':args.stage,'cases':[]}
def write(): (OUT/'report.json').write_text(json.dumps(report,indent=2)+'\n')
def budget():
 if time.monotonic()-started>args.max_seconds:report['status']='BUDGET_EXHAUSTED_NOT_ACCEPTED';write();raise RuntimeError('Explicit diagnostic wall budget exhausted')
# Read shared original evaluator definitions only; never execute their output loops.
helper=PILOT/'tools/27_actual_arm_elevation.py';s=helper.read_text();prefix=s[:s.index('# Corrected actual elevation fixture')].replace(';D.mkdir(exist_ok=True)','')
# Shared legacy definitions are file-relative. Supply their original __file__,
# and suppress only their incidental old-output-directory creation, not pose math.
needle="s=(O/'tools/05_full_motion_gallery.py').read_text();";assert needle in prefix
prefix=prefix.replace(needle,needle[:-1]+'.replace("(O/\'gallery-v1\').mkdir(exist_ok=True)","None");')
probeFile=__file__;__file__=str(helper)
try:exec(compile(prefix,str(helper),'exec'))
finally:__file__=probeFile
report={'status':'RUNNING_DIAGNOSTIC_NO_CORRECTION','stage':args.stage,'cases':[]}
expectedSource=json.loads((PACKET/'adaptive-dance_01-FULL_ADAPTIVE_TIGHT008_EXACT_BATCH.json').read_text())['sourceConfig']
assert sha(PILOT/'stage02-bound-original-dense.blend')==expectedSource['sourceBlend'],'Frozen source blend changed'
assert sha(source)==expectedSource['sourceFBX'],'Frozen native source changed'
for file in [helper,PILOT/'tools/05_full_motion_gallery.py',Path(R.__file__)]:assert sha(file)==expectedSource['code'][file.name],'Frozen evaluator changed: '+str(file)
# Shared helper defines D/G; all new writes below explicitly use fenced OUT/REG.
for ob in scene.objects:
 if ob.type=='MESH':
  for mod in ob.modifiers:mod.show_viewport=False
fast=(PILOT/'tools/05_full_motion_gallery.py').read_text();fast=fast[fast.index('def retarget('):fast.index('reports=[]')];i=fast.index(' deps=bpy.context.evaluated_depsgraph_get()');j=fast.index(' errors={}',i);fast=fast[:i]+' lo=np.zeros(3);hi=np.zeros(3)\n'+fast[j:];exec(compile(fast,'unchanged-original-source-no-mesh-eval','exec'))
if not (REG/'region-proposals.json').exists():propose(PACKET,REG)
regionInfo=json.loads((REG/'region-proposals.json').read_text());assert sha(REG/regionInfo['maskNPZ'])==regionInfo['maskSHA256'];regions=np.load(REG/regionInfo['maskNPZ']);regionMasks={name:np.isin(np.arange(N),regions[name],assume_unique=True) for name in regions.files};assert all(len(regions[n])>10 for n in regions.files),'Empty provisional region; inspect before scoring'
review=REG/'review-receipt.json';reviewed=False
if review.exists():
 receipt=json.loads(review.read_text());reviewed=receipt.get('reviewed') is True and receipt.get('maskSHA256')==regionInfo['maskSHA256'] and len(receipt.get('images',[]))>=3
 for item in receipt.get('images',[]):
  image=(OWN/item['path']).resolve();reviewed=reviewed and image.is_relative_to(OWN) and image.exists() and sha(image)==item['sha256']
report.update(maskStatus='REVIEWED' if reviewed else 'PROVISIONAL_NOT_ANATOMICAL_TRUTH',maskSHA256=regionInfo['maskSHA256'],probeScriptSHA256=sha(Path(__file__)),sourceVertices=N,sourceTriangles=1988914,sourceAttributes=original,sourceHashes={str(f):sha(f) for f in [PILOT/'stage02-bound-original-dense.blend',source,helper]},limits=['Metrics/masks only; no corrective or weight mutation.','Orientation opposition is a transported-normal witness, not a self-intersection proof.','Closed sections are not whole-garment volume.','C1 OFF/ON hand movement is measured natural girdle translation, not automatically an anchor failure.'])
assert sha(PACKET/'original-indexed-attributes.npz')==regionInfo['sourceIndexedSHA256'],'Region/source indexed data changed'
ref=np.load(PACKET/'original-indexed-attributes.npz');names=json.loads((PACKET/'manifest.json').read_text())['boneNames'];tri=ref['triangles'];joint=ref['jointIndices'];weight=ref['weights'].astype(float);restWorld=raw@mw[:3,:3].T+mw[:3,3];bind=np.array([rig.data.bones[n].matrix_local.inverted()@rig.matrix_world.inverted()@body.matrix_world for n in names]);edges=canonical_edges(tri) if args.stage=='metrics' else None
assert np.all(weight.sum(1)>0) and np.max((weight>0).sum(1))<=4
report['referenceWeightStats']={'unweighted':0,'maxInfluences':int(np.max((weight>0).sum(1))),'maxSumDeviation':float(np.abs(weight.sum(1)-1).max()),'nonzeroInfluences':int((weight>0).sum()),'reference':'untouched original-indexed sidecar; actual render readback separately checked'}

def world_surface():
 skin=np.array([rig.matrix_world@rig.pose.bones[n].matrix for n in names])@bind;points=np.concatenate([raw,np.ones((N,1))],1);out=np.zeros((N,3))
 for begin in range(0,N,65536):
  end=min(N,begin+65536);part=np.zeros((end-begin,4))
  for k in range(4):part+=np.einsum('nij,nj->ni',skin[joint[begin:end,k]],points[begin:end])*weight[begin:end,k,None]
  out[begin:end]=part[:,:3]
 return out,skin

def setup_pose(case,on):
 if case['name']=='neutral':R.apply_pose(rig,{'name':'trueRest','ops':[('fk',)]},.98);bpy.context.view_layer.update()
 else:retarget(case['clip'],case['phase'])
 if case.get('synthetic'):
  for side in ['L','R']:
   pb=rig.pose.bones['upper_arm_fk.'+side];up=rig.matrix_world.to_3x3()@rig.pose.bones['ORG-spine.003'].matrix.col[1].xyz;up.normalize();arm=rig.pose.bones['ORG-upper_arm.'+side];v=(rig.matrix_world.to_3x3()@(arm.tail-arm.head)).normalized();horizontal=(v-up*v.dot(up)).normalized();desired=horizontal*math.cos(math.radians(65))+up*math.sin(math.radians(65));dq=v.rotation_difference(desired);wm=rig.matrix_world@pb.matrix;pb.matrix=rig.matrix_world.inverted()@(Matrix.Translation(wm.translation)@(dq@wm.to_quaternion()).to_matrix().to_4x4());bpy.context.view_layer.update()
 angles=support('armElevation',.5) if on and case.get('synthetic') else (support('dance_01',case['phase']) if on and case['clip']=='dance_01' else [])
 if case.get('synthetic'):
  actual=[elevation(rig.matrix_world.to_3x3()@(rig.pose.bones['ORG-upper_arm.'+side].tail-rig.pose.bones['ORG-upper_arm.'+side].head),rig.matrix_world.to_3x3()@rig.pose.bones['ORG-spine.003'].matrix.col[1].xyz) for side in ['L','R']];assert max(abs(v-65) for v in actual)<.01,('Synthetic fixture is not actual65degrees',actual)
 return angles

def joints_measure():
 up=rig.matrix_world.to_3x3()@rig.pose.bones['ORG-spine.003'].matrix.col[1].xyz;out={}
 for side in ['L','R']:
  shoulder=rig.pose.bones['ORG-shoulder.'+side];arm=rig.pose.bones['ORG-upper_arm.'+side];out[side]={'clavicleElevationDeg':elevation(rig.matrix_world.to_3x3()@(shoulder.tail-shoulder.head),up),'humerusElevationDeg':elevation(rig.matrix_world.to_3x3()@(arm.tail-arm.head),up),'shoulder':list(rig.matrix_world@arm.head),'elbow':list(rig.matrix_world@rig.pose.bones['ORG-forearm.'+side].head),'wrist':list(rig.matrix_world@rig.pose.bones['ORG-hand.'+side].head)}
 return out

# Diagnostic material overrides leave original source materials/nodes/images intact.
clay=bpy.data.materials.new('OwnedSkinProbeClay');clay.use_nodes=True;bs=clay.node_tree.nodes.get('Principled BSDF');bs.inputs['Base Color'].default_value=(.45,.45,.45,1);bs.inputs['Roughness'].default_value=.7;bs.inputs['Metallic'].default_value=0
scene.cycles.samples=8;scene.cycles.use_denoising=False;scene.render.threads_mode='FIXED';scene.render.threads=6;scene.render.resolution_x=640;scene.render.resolution_y=768;scene.render.resolution_percentage=100;scene.render.image_settings.file_format='PNG';scene.view_layers[0].material_override=None if args.source_material else clay

def render(name,points,view,close=False,framing=None):
 budget();lo,hi=points.min(0),points.max(0);target=(lo+hi)*.5;scale=max(hi[2]-lo[2],(hi[0]-lo[0])*1.2)*1.14
 if close:target=np.array(rig.matrix_world@rig.pose.bones['ORG-spine.004'].head)+[0,0,-.025];scale=.64
 if framing is not None:target,scale=framing
 offsets={'front':(0,-2,.06),'backthreequarter':(.85,2,.12),'side':(2,0,.06)};cam.location=Vector(target)+Vector(offsets[view]);cam.rotation_euler=(Vector(target)-cam.location).to_track_quat('-Z','Y').to_euler();cd.ortho_scale=scale;scene.render.filepath=str(OUT/(name+'.png'))
 for mod in body.modifiers:mod.show_viewport=True;mod.show_render=True
 bpy.context.view_layer.update();deps=body.evaluated_get(bpy.context.evaluated_depsgraph_get());me=deps.to_mesh();actual=np.empty(N*3,np.float32);me.vertices.foreach_get('co',actual);deps.to_mesh_clear();actual=actual.reshape(-1,3)@mw[:3,:3].T+mw[:3,3];err=float(np.linalg.norm(actual-points,axis=1).max());assert err<1e-5,('actual game-LBS surface mismatch',err)
 bpy.ops.render.render(write_still=True)
 for mod in body.modifiers:mod.show_viewport=False
 return {'file':name+'.png','surfaceWitnessAllVerticesMaxM':err,'cameraWorld':list(cam.location),'cameraTarget':list(target),'orthoScale':scale,'material':'source-normal1' if args.source_material else 'diagnostic-clay-normalOFF'}

if args.stage=='masks':
 if args.mask_region is not None:assert args.mask_region in regions.files,'Unknown mask region'
 # A single owned corner-color material visualizes the proposals in untouched rest geometry.
 R.apply_pose(rig,{'name':'rest','ops':[('fk',)]},.98);bpy.context.view_layer.update();maskmat=bpy.data.materials.new('OwnedRegionMaskDiagnostic');maskmat.use_nodes=True;nodes=maskmat.node_tree.nodes;nodes.clear();a=nodes.new('ShaderNodeVertexColor');a.layer_name='OwnedRegionMask';em=nodes.new('ShaderNodeEmission');output=nodes.new('ShaderNodeOutputMaterial');maskmat.node_tree.links.new(a.outputs['Color'],em.inputs['Color']);maskmat.node_tree.links.new(em.outputs[0],output.inputs['Surface'])
 # Attribute lives on an owned mesh copy, never on original source data.
 originalData=body.data;body.data=originalData.copy();color=body.data.color_attributes.new(name='OwnedRegionMask',type='FLOAT_COLOR',domain='POINT');colors=np.tile([.12,.12,.12,1.],(N,1));legend={}
 for i,name in enumerate(regions.files):
  import colorsys
  rgba=(*colorsys.hsv_to_rgb((i*.61803398875)%1,.8,.95),1);
  if args.mask_region is None or args.mask_region==name:colors[regions[name]]=rgba
  legend[name]=rgba
 color.data.foreach_set('color',colors.astype(np.float32).ravel())
 if args.source_material:
  scene.view_layers[0].material_override=None
  for slot in body.material_slots:
   if slot.material is None:continue
   mat=slot.material.copy();slot.material=mat;nt=mat.node_tree
   for principled in [n for n in nt.nodes if n.type=='BSDF_PRINCIPLED']:
    base=principled.inputs['Base Color'];oldlink=base.links[0].from_socket if base.is_linked else None;oldcolor=tuple(base.default_value)
    mix=nt.nodes.new('ShaderNodeMixRGB');mix.blend_type='MIX';mix.inputs[0].default_value=.5;mix.inputs[1].default_value=oldcolor
    attr=nt.nodes.new('ShaderNodeVertexColor');attr.layer_name='OwnedRegionMask'
    if oldlink:nt.links.new(oldlink,mix.inputs[1])
    nt.links.new(attr.outputs['Color'],mix.inputs[2]);nt.links.new(mix.outputs[0],base)
 else:scene.view_layers[0].material_override=maskmat
 report['maskLegend']=legend;report['renders']=[render('mask-'+view,restWorld,view) for view in ['front','backthreequarter','side']]
 for item in report['renders']:item['material']='diagnostic-source-albedo-mask-overlay' if args.source_material else 'diagnostic-flat-mask'
 body.data=originalData
else:
 selected=json.loads((PACKET/'skin-quality-actual-pose-selection.json').read_text())['selected'];cases=[{'name':'dance'+str(int(p*100)),'clip':'dance_01','phase':p} for p in [.25,.5,.75]]+[{'name':'selected-'+r['side']+'-'+str(i),'clip':'dance_01','phase':r['phase'],'selection':r} for i,r in enumerate(selected)]+[{'name':'neutral','clip':'neutral','phase':0},{'name':'box-extreme','clip':'box_02','phase':1.8541666666666667/(67/24)},{'name':'kick-high-flexion','clip':'front_kick_01','phase':.45},{'name':'synthetic-arm65','clip':'dance_01','phase':.5,'synthetic':True}]
 if args.case_names:cases=[c for c in cases if c['name'] in args.case_names];assert cases,'No requested case matched'
 if args.stage=='renders' and not args.case_names:cases=cases[:3]
 for case in cases:
  budget();pair={}
  for on in [False,True]:
   angles=setup_pose(case,on);surface,skin=world_surface();label='ON' if on else 'OFF';entry={'support':angles,'jointMeasures':joints_measure(),'neckHeadWorld':list(rig.matrix_world@rig.pose.bones['ORG-spine.004'].head)}
   if args.stage=='metrics':
    # Transported rest-face normal is only a fold-orientation witness, not a topological proof.
    restCross=np.cross(restWorld[tri[:,1]]-restWorld[tri[:,0]],restWorld[tri[:,2]]-restWorld[tri[:,0]]);expected=np.empty_like(restCross);worldLinear=skin[:,:3,:3]@np.linalg.inv(mw[:3,:3])
    for begin in range(0,len(tri),65536):
     end=min(len(tri),begin+65536);meanLinear=np.zeros((end-begin,3,3))
     for corner in range(3):
      vi=tri[begin:end,corner]
      for k in range(4):meanLinear+=worldLinear[joint[vi,k]]*weight[vi,k,None,None]/3
     expected[begin:end]=transport_normals(meanLinear,restCross[begin:end])
    entry['regions']={}
    for name,mask in regionMasks.items():
     budget();edge=edge_metrics(restWorld,surface,edges,mask);face=triangle_metrics(restWorld,surface,tri,mask,expected);entry['regions'][name]={'edge':edge,'triangle':face}
    entry['sections']={}
    for side in ['L','R']:
     a=np.array(entry['jointMeasures'][side]['shoulder']);b=np.array(entry['jointMeasures'][side]['elbow']);normal=b-a
     for fraction in [.2,.5]:
      center=a+(b-a)*fraction;segments=section_segments(surface,tri,center,normal);section=closed_section(segments,center,normal,epsilon=1e-7);section['segmentsWorld']=segments.tolist();section['planeCenter']=center.tolist();section['planeNormal']=normal.tolist();entry['sections'][side+str(fraction)]=section
   else:entry['renders']=[]
   pair[label]=(surface,entry)
  if args.stage=='renders':
   lo=np.minimum(pair['OFF'][0].min(0),pair['ON'][0].min(0));hi=np.maximum(pair['OFF'][0].max(0),pair['ON'][0].max(0));full=((lo+hi)*.5,max(hi[2]-lo[2],(hi[0]-lo[0])*1.2)*1.14);closeFrame=(np.array(pair['OFF'][1]['neckHeadWorld'])+[0,0,-.025],.64)
   for on in [False,True]:
    setup_pose(case,on);label='ON' if on else 'OFF';surface,entry=pair[label]
    for view,close in [('front',False),('front',True),('backthreequarter',True),('side',True)]:entry['renders'].append(render(case['name']+'-'+label+'-'+view+('-close' if close else '-full'),surface,view,close,closeFrame if close else full))
  row={'case':case,'OFF':pair['OFF'][1],'ON':pair['ON'][1],'anchorDisplacements':{n:anchor_metrics(pair['OFF'][0],pair['ON'][0],regions[n]) for n in regions.files if 'anchor' in n},'numericAcceptance':'Not assigned: OFF/ON C1 is diagnostic; future correction must compare to fixed C1 baseline with reviewed masks.'}
  if args.stage=='metrics':
   row['projectionContours']={};origin=np.array(pair['OFF'][1]['neckHeadWorld'])
   for view,offset in [('front',(0,-2,.06)),('backthreequarter',(.85,2,.12)),('side',(2,0,.06))]:
    forward=-np.array(offset,float);forward/=np.linalg.norm(forward);right=np.cross(forward,[0,0,1.]);right/=np.linalg.norm(right);up=np.cross(right,forward);row['projectionContours'][view]={}
    for name in regions.files:
     ids=regions[name];yoff=(pair['OFF'][0][ids]-origin)@up;yon=(pair['ON'][0][ids]-origin)@up;bounds=[min(yoff.min(),yon.min()),max(yoff.max(),yon.max())]
     row['projectionContours'][view][name]={label:projected_envelope(pair[label][0],ids,origin,right,up,bounds) for label in ['OFF','ON']}
   row['C1MetricChanges']={n:{'p99AbsLogStrainDelta':pair['ON'][1]['regions'][n]['edge']['logStrainAbs']['p99']-pair['OFF'][1]['regions'][n]['edge']['logStrainAbs']['p99'],'worstAbsLogStrainDelta':pair['ON'][1]['regions'][n]['edge']['logStrainAbs']['max']-pair['OFF'][1]['regions'][n]['edge']['logStrainAbs']['max'],'newCollapsedCount':pair['ON'][1]['regions'][n]['triangle']['collapsedCount']-pair['OFF'][1]['regions'][n]['triangle']['collapsedCount']} for n in regions.files}
  report['cases'].append(row);assert hash_arrays(body)==original;write();print('CASE_DONE '+case['name'],flush=True)
assert hash_arrays(body)==original
for f,digest in report['sourceHashes'].items():assert sha(f)==digest,'Source file changed'
report.update(status='DIAGNOSTIC_COMPLETE_NOT_SKIN_ACCEPTANCE',seconds=time.monotonic()-started,sourceAttributesExact=True);write();print('RESULT '+json.dumps({k:v for k,v in report.items() if k not in ['cases','sourceHashes','sourceAttributes']}),flush=True)

"""Prepared-only local Blender delta-mush diagnostic. Never apply/save modifier."""
import os
for k in ['OPENBLAS_NUM_THREADS','OMP_NUM_THREADS','MKL_NUM_THREADS']:os.environ[k]='6'
import argparse,json,sys,time,hashlib,types
from pathlib import Path
import numpy as np,bpy
HERE=Path(__file__).resolve();O=HERE.parents[1];B=O.parent;BASE=B/'tools/probe_source_off_on.py';p=argparse.ArgumentParser();p.add_argument('--output',type=Path,default=O/'ordinary-motions-v1');p.add_argument('--max-seconds',type=float,default=600);a=p.parse_args(sys.argv[sys.argv.index('--')+1:] if '--' in sys.argv else []);OUT=a.output.resolve();assert OUT.is_relative_to(O.resolve()) and not OUT.exists();OUT.mkdir(parents=True);began=time.monotonic();report={'status':'RUNNING_DIAGNOSTIC','cases':[],'limits':['Blender-only local modifier diagnostic, not a portable game deformation path.','Original source weights, not B-derived weights.','Opposed baseline normals are not self-intersection proof.']}
def save():(OUT/'report.json').write_text(json.dumps(report,indent=2)+'\n')
def budget():
 if time.monotonic()-began>a.max_seconds:raise RuntimeError('Bounded diagnostic deadline exceeded')
oldargv=sys.argv;sys.argv=['source-defs','--','--stage','renders','--source-material','--output',str(OUT),'--max-seconds',str(a.max_seconds)];text=BASE.read_text().split("if args.stage=='masks':")[0];text=text.replace("'backthreequarter':(.85,2,.12),'side':(2,0,.06)","'backthreequarter':(-.85,2,.12),'side':(-2,0,.06)");needle='for mod in body.modifiers:mod.show_viewport=True;mod.show_render=True';assert needle in text;text=text.replace(needle,'for mod in body.modifiers:mod.show_viewport=(mod.type!="CORRECTIVE_SMOOTH" or diagnosticCorrectiveEnabled);mod.show_render=mod.show_viewport');ns={'__file__':str(BASE),'__name__':'source_defs','diagnosticCorrectiveEnabled':False}
try:exec(compile(text,str(BASE),'exec'),ns)
finally:sys.argv=oldargv
body,rig,scene=[ns[k] for k in ['body','rig','scene']];N=ns['N'];tri=ns['tri'];mw=ns['mw'];names=ns['names'];rest=ns['restWorld'];regions=ns['regions'];attrs=ns['hash_arrays'](body);assert all(m.type=='ARMATURE' for m in body.modifiers);assert all(not m.use_deform_preserve_volume for m in body.modifiers);assert all(rig.data.bones[n].bbone_segments==1 for n in names)
def weightsha():
 h=hashlib.sha256()
 for v in body.data.vertices:h.update(np.asarray([(g.group,g.weight) for g in v.groups],np.float64).tobytes());h.update(b';')
 return h.hexdigest()
originalWeightSHA=weightsha();maskFile=O/'garment-mask-feather-v1.npz';mask=np.load(maskFile);ids=mask['originalVertexIndices'];feather=mask['modifierWeight'];protected=mask['protectedOriginalVertexIndices'];assert len(protected)==221624 and not np.intersect1d(ids,protected).size;outside=np.ones(N,bool);outside[ids]=False
vg=body.vertex_groups.new(name='QA_ReviewedGarment_DiagnosticOnly')
for v,w in zip(ids,feather):
 if w>0:vg.add([int(v)],float(w),'REPLACE')
modifier=body.modifiers.new(name='QA_LocalCorrectiveSmooth_ORCO',type='CORRECTIVE_SMOOTH');assert 'ORCO' in modifier.bl_rna.properties['rest_source'].enum_items.keys();modifier.rest_source='ORCO';modifier.smooth_type='LENGTH_WEIGHTED';modifier.factor=.5;modifier.iterations=5;modifier.scale=1.;modifier.use_only_smooth=False;modifier.use_pin_boundary=False;modifier.vertex_group=vg.name;modifier.invert_vertex_group=False;modifier.show_viewport=False;modifier.show_render=False
# ORCO takes original mesh coordinates directly. Binding operator would select
# a different BIND mechanism; is_bind is not a success condition in this mode.
assert list(body.modifiers)[-1]==modifier and body.modifiers[0].type=='ARMATURE'
report.update(sourceVertices=N,sourceTriangles=len(tri),sourceAttrs=attrs,sourceHashes=ns['report']['sourceHashes'],originalActualWeightsSHA256=originalWeightSHA,maskSHA256=hashlib.sha256(maskFile.read_bytes()).hexdigest(),configuration={'rest_source':'ORCO','smooth_type':'LENGTH_WEIGHTED','factor':.5,'iterations':5,'scale':1.,'use_only_smooth':False,'use_pin_boundary':False,'afterOriginalLBS':True,'diagnosticVGDoesNotCountAsSkinBone':True},priorNative100kExperiment='prototypes/roster-portraits/tools/shoulder/try_corrective_v5.py: BIND,.5x10/1x20,rejected; this full2M/localORCO5 test is distinct but same algorithm family')
sys.path.insert(0,str(B/'garment-weight-transition-v1/tools'));from mixture_kernel import orientation_witnesses
edges=ns['canonical_edges'](tri);incident=np.flatnonzero(np.any(np.isin(tri,ids),axis=1));lt=tri[incident];queue=[]
def evaluated(on):
 ns['diagnosticCorrectiveEnabled']=on
 for m in body.modifiers:m.show_viewport=(m.type!='CORRECTIVE_SMOOTH' or on)
 bpy.context.view_layer.update();ob=body.evaluated_get(bpy.context.evaluated_depsgraph_get());mesh=ob.to_mesh()
 try:
  assert len(mesh.vertices)==N and len(mesh.polygons)==len(body.data.polygons);ha=ns['hash_arrays'](types.SimpleNamespace(data=mesh));assert all(ha[k]==attrs[k] for k in attrs if k!='positions'),'Evaluated topology/UV changed';v=np.empty(N*3,np.float32);mesh.vertices.foreach_get('co',v)
 finally:ob.to_mesh_clear()
 for m in body.modifiers:m.show_viewport=False
 return v.reshape(-1,3)@mw[:3,:3].T+mw[:3,3]
cases=[{'name':'box-extreme','clip':'box_02','phase':1.8541666666666667/(67/24)},{'name':'kick-high-flexion','clip':'front_kick_01','phase':.45}]
try:
 for case in cases:
  budget();ns['setup_pose'](case,True);bones=np.array([rig.matrix_world@rig.pose.bones[n].matrix for n in names]);manual,_=ns['world_surface']();surfaces={};row={'case':case,'modes':{}}
  for label,on in [('LBS',False),('CorrectiveSmooth',True)]:
   actual=evaluated(on);assert np.array_equal(bones,np.array([rig.matrix_world@rig.pose.bones[n].matrix for n in names]));surfaces[label]=actual;row['modes'][label]={'regions':{}}
   if not on:
    err=float(np.linalg.norm(actual-manual,axis=1).max());assert err<1e-5;row['independentOriginalLBSMaxErrorM']=err
   for region in ['sleeve_armhole_R','torso_underarm_R']:
    m=np.zeros(N,bool);m[regions[region]]=True;e=edges[np.any(m[edges],axis=1)];rl=np.linalg.norm(rest[e[:,1]]-rest[e[:,0]],axis=1);pl=np.linalg.norm(actual[e[:,1]]-actual[e[:,0]],axis=1);row['modes'][label]['regions'][region]={'edge':ns['edge_metrics'](rest,actual,edges,m),'absoluteExcessP99M':float(np.quantile(np.maximum(pl-rl,0),.99))}
  delta=np.linalg.norm(surfaces['CorrectiveSmooth']-surfaces['LBS'],axis=1);rr=orientation_witnesses(surfaces['LBS'][lt],surfaces['CorrectiveSmooth'][lt]);row.update(maxDeltaM=float(delta.max()),protectedMaxDeltaM=float(delta[protected].max()),outsideReviewedSupportMaxDeltaM=float(delta[outside].max()),orientation={'opposed':len(rr['opposed']),'newCollapsed':len(rr['newCollapsed']),'opposedOriginalTriangleIDs':incident[rr['opposed']].tolist()},changedSurfaceArea={'baselineM2':float(rr['beforeArea'].sum()),'candidateM2':float(rr['afterArea'].sum()),'volumePreservationClaim':False})
  report['cases'].append(row);save()
  if case['name'] in ['box-extreme','kick-high-flexion']:queue.append((case,surfaces,row,bones.copy()))
 failure=[r['case']['name'] for r in report['cases'] if r['protectedMaxDeltaM']>1e-9 or r['outsideReviewedSupportMaxDeltaM']>1e-9];report['protectedScopeFailures']=failure;save()
 if failure:raise RuntimeError('Protected/outside scope changed: '+','.join(failure))
 view=json.loads((B/'corrective-v1/weight-witness-v1/renders/report.json').read_text())['renders'][2];framing=(np.array(view['cameraTarget']),view['orthoScale'])
 for case,surfaces,row,bones in queue:
  ns['setup_pose'](case,True);assert np.array_equal(bones,np.array([rig.matrix_world@rig.pose.bones[n].matrix for n in names]))
  for label,on in [('LBS',False),('CorrectiveSmooth',True)]:
   ns['diagnosticCorrectiveEnabled']=on
   for mat,override in [('source',None),('clay',ns['clay'])]:
    scene.view_layers[0].material_override=override
    for viewname in ['side','front','backthreequarter'] if case['name']=='dance50' else ['side']:
     budget();item=ns['render'](case['name']+'-'+label+'-'+mat+'-'+viewname,surfaces[label],viewname,True,framing);item['material']='source-normal1' if override is None else 'diagnostic-flat-clay';row['modes'][label].setdefault('renders',[]).append(item)
 report['status']='COMPLETED_BLENDER_ONLY_DIAGNOSTIC_NOT_GAME_ACCEPTANCE'
except BaseException as exc:report['status']='FAILED_DIAGNOSTIC';report['error']=repr(exc);raise
finally:
 body.modifiers.remove(modifier);body.vertex_groups.remove(vg);report['originalActualWeightsUnchangedAfterRemoval']=weightsha()==originalWeightSHA;report['sourceRestAttrsUnchanged']=ns['hash_arrays'](body)==attrs;assert report['originalActualWeightsUnchangedAfterRemoval'] and report['sourceRestAttrsUnchanged'];report['elapsedSeconds']=time.monotonic()-began;save()

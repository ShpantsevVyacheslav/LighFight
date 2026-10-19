"""Owned read-only Blender skinning diagnostic. Requires explicit heavy-window release."""
import os
os.environ.setdefault('OPENBLAS_NUM_THREADS','6')
import argparse,hashlib,json,sys,time
from pathlib import Path
import numpy as np
import bpy
ROOT=Path(__file__).resolve().parents[1];BASE=ROOT.parent/'tools/probe_source_off_on.py'
p=argparse.ArgumentParser();p.add_argument('--output',type=Path,default=ROOT/'actual-v1');p.add_argument('--max-seconds',type=float,default=900);p.add_argument('--metrics-only',action='store_true');a=p.parse_args(sys.argv[sys.argv.index('--')+1:] if '--' in sys.argv else [])
out=a.output.resolve();assert out.is_relative_to(ROOT.resolve()) and not out.exists(),'New owned output required';out.mkdir(parents=True)
started=time.monotonic();report={'status':'RUNNING_DIAGNOSTIC','cases':[],'limitations':['Blender Preserve Volume comparison only; no Unity DQS path or shipping acceptance.','Region envelopes are provisional, not anatomical ground truth.','DQ surface is evaluated by Blender, not an independent DQ implementation.']}
def save(): (out/'report.json').write_text(json.dumps(report,indent=2)+'\n')
def budget():
 if time.monotonic()-started>a.max_seconds:raise RuntimeError('Bounded diagnostic deadline exceeded')
def digest_array(v):return hashlib.sha256(np.ascontiguousarray(v).tobytes()).hexdigest()
def stats(v):return dict(zip(['median','p95','p99','max'],map(float,np.quantile(v,[.5,.95,.99,1]))))
# Load original, hash-locked source evaluator definitions without its execution loop.
oldargv=sys.argv;sys.argv=['source-definitions','--','--stage','renders','--source-material','--output',str(out),'--max-seconds',str(a.max_seconds)]
text=BASE.read_text().split("if args.stage=='masks':")[0]
assert "'side':(2,0,.06)" in text
text=text.replace("'backthreequarter':(.85,2,.12),'side':(2,0,.06)","'backthreequarter':(-.85,2,.12),'side':(-2,0,.06)")
ns={'__file__':str(BASE),'__name__':'source_definitions'}
try:exec(compile(text,str(BASE),'exec'),ns)
finally:sys.argv=oldargv
body,rig,scene=[ns[k] for k in ['body','rig','scene']];N=ns['N'];names=ns['names'];tri=ns['tri'];rest=ns['restWorld'];regions=ns['regions'];mw=ns['mw'];setup=ns['setup_pose'];render=ns['render'];clay=ns['clay']
mods=[m for m in body.modifiers if m.type=='ARMATURE'];assert len(mods)==1 and mods[0].object==rig
arm=mods[0];original_mode=arm.use_deform_preserve_volume
assert not original_mode,'Baseline must be established game LBS'
assert all(rig.data.bones[n].bbone_segments==1 for n in names),'Segmented B-bones would confound comparison'
original_hash=ns['hash_arrays'](body)
def weights_hash():
 h=hashlib.sha256()
 for v in body.data.vertices:
  h.update(np.asarray([(g.group,g.weight) for g in v.groups],dtype=np.float64).tobytes());h.update(b';')
 return h.hexdigest()
weight_hash=weights_hash();bind_hash=digest_array(np.array([rig.data.bones[n].matrix_local for n in names]))
report.update(sourceVertices=N,sourceTriangles=len(tri),sourceAttributes=original_hash,actualVertexGroupsSHA256=weight_hash,bindSHA256=bind_hash,sourceHashes=ns['report']['sourceHashes'],scriptSHA256=hashlib.sha256(Path(__file__).read_bytes()).hexdigest(),sameModeParameters={'BboneSegments':1,'onlyVariable':'ArmatureModifier.use_deform_preserve_volume'})
from skin_metrics import canonical_edges,edge_metrics,triangle_metrics,anchor_metrics
edges=canonical_edges(tri)
def pose_snapshot():return np.array([rig.matrix_world@rig.pose.bones[n].matrix for n in names])
def evaluated():
 for m in body.modifiers:m.show_viewport=True
 bpy.context.view_layer.update();ob=body.evaluated_get(bpy.context.evaluated_depsgraph_get());me=ob.to_mesh()
 try:
  assert len(me.vertices)==N and len(me.loop_triangles) in (0,len(tri))
  points=np.empty(N*3,np.float32);me.vertices.foreach_get('co',points)
 finally:ob.to_mesh_clear()
 for m in body.modifiers:m.show_viewport=False
 return points.reshape(-1,3)@mw[:3,:3].T+mw[:3,3]
cases=[dict(name='dance'+str(int(q*100)),clip='dance_01',phase=q) for q in [.45,.5,.55]]+[dict(name='synthetic-arm65',clip='dance_01',phase=.5,synthetic=True),dict(name='neutral',clip='neutral',phase=0),dict(name='box-extreme',clip='box_02',phase=1.8541666666666667/(67/24)),dict(name='kick-high-flexion',clip='front_kick_01',phase=.45)]
try:
 for case in cases:
  budget();setup(case,True);bones=pose_snapshot();reference,_=ns['world_surface']();surfaces={};row={'case':case,'modes':{},'bonePoseSHA256':digest_array(bones)}
  for label,dq in [('LBS',False),('DQS',True)]:
   budget();arm.use_deform_preserve_volume=dq;surface=evaluated();assert np.array_equal(pose_snapshot(),bones),'Mode toggle changed bone pose'
   if not dq:
    err=float(np.linalg.norm(surface-reference,axis=1).max());assert err<1e-5,('Independent original LBS mismatch',err);row['independentLBSAllVerticesMaxM']=err
   surfaces[label]=surface;entry={'preserveVolume':dq,'regions':{}}
   for region in ['sleeve_armhole_R','torso_underarm_R','medial_trapezius_R']:
    # Resolve actual stored names explicitly; refuse an invented anatomical mask.
    assert region in regions.files, region
    mask=np.zeros(N,bool);mask[regions[region]]=True
    e=edge_metrics(rest,surface,edges,mask);chosen=edges[np.any(mask[edges],axis=1)];la=np.linalg.norm(rest[chosen[:,1]]-rest[chosen[:,0]],axis=1);lb=np.linalg.norm(surface[chosen[:,1]]-surface[chosen[:,0]],axis=1)
    entry['regions'][region]={'edge':e,'triangle':triangle_metrics(rest,surface,tri,mask),'absoluteDeltaM':stats(np.abs(lb-la)),'absoluteExcessM':stats(np.maximum(lb-la,0))}
   row['modes'][label]=entry
  displacement=np.linalg.norm(surfaces['DQS']-surfaces['LBS'],axis=1);row['DQSvsLBSAllVerticesM']=stats(displacement);row['anchorMovement']={k:anchor_metrics(surfaces['LBS'],surfaces['DQS'],regions[k]) for k in regions.files if 'anchor' in k}
  # Fixed same camera/framing across pair, including the correct -X right side.
  if not a.metrics_only:
   lo=np.minimum(*[v.min(0) for v in surfaces.values()]);hi=np.maximum(*[v.max(0) for v in surfaces.values()]);full=((lo+hi)*.5,max(hi[2]-lo[2],(hi[0]-lo[0])*1.2)*1.14)
   close=(np.array(rig.matrix_world@rig.pose.bones['ORG-spine.004'].head)+[0,0,-.025],.64)
   for label,dq in [('LBS',False),('DQS',True)]:
    arm.use_deform_preserve_volume=dq
    variants=[('clay',clay)] + ([('source',None)] if case['name']=='dance50' else [])
    for material,override in variants:
     scene.view_layers[0].material_override=override
     views=[('front',False),('side',True),('backthreequarter',True)] if case['name']=='dance50' else [('side',True)]
     for view,isclose in views:
      budget();row['modes'][label].setdefault('renders',[]).append(render(case['name']+'-'+label+'-'+material+'-'+view,surfaces[label],view,isclose,close if isclose else full))
  report['cases'].append(row);save()
 assert ns['hash_arrays'](body)==original_hash and weights_hash()==weight_hash
 assert digest_array(np.array([rig.data.bones[n].matrix_local for n in names]))==bind_hash
 report['status']='COMPLETED_DIAGNOSTIC_NOT_QUALITY_ACCEPTANCE';report['sourceAttributesWeightsBindUnchanged']=True
except BaseException as exc:
 report['status']='FAILED_DIAGNOSTIC';report['error']=repr(exc);raise
finally:
 arm.use_deform_preserve_volume=original_mode;report['elapsedSeconds']=time.monotonic()-started;save()

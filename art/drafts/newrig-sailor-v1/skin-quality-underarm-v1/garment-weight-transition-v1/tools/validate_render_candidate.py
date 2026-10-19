"""Bounded actual Blender LBS weight-candidate diagnostic. Explicit heavy release required."""
import os
for k in ['OPENBLAS_NUM_THREADS','OMP_NUM_THREADS','MKL_NUM_THREADS']:os.environ[k]='6'
import argparse,json,sys,time,hashlib
from pathlib import Path
import numpy as np,bpy
HERE=Path(__file__).resolve();O=HERE.parents[1];B=O.parent;BASE=B/'tools/probe_source_off_on.py';p=argparse.ArgumentParser();p.add_argument('--output',type=Path,default=O/'actual-constrained-v2');p.add_argument('--max-seconds',type=float,default=600);p.add_argument('--candidate',type=Path,default=O/'orientation-constrained-v2/lambda50-active-weights.npz');args=p.parse_args(sys.argv[sys.argv.index('--')+1:] if '--' in sys.argv else []);OUT=args.output.resolve();assert OUT.is_relative_to(O.resolve()) and not OUT.exists();OUT.mkdir(parents=True);started=time.monotonic()
r={'status':'RUNNING_ACTUAL_DIAGNOSTIC','cases':[],'limits':['Versioned derived weights intentionally differ; original archived weights unchanged.','Orientation witnesses are not self-intersection proof.','No promotion from strain reduction alone.']}
def save():(OUT/'report.json').write_text(json.dumps(r,indent=2)+'\n')
def budget():
 if time.monotonic()-started>args.max_seconds:raise RuntimeError('Bounded actual diagnostic budget exhausted')
oldargv=sys.argv;sys.argv=['source-defs','--','--stage','renders','--source-material','--output',str(OUT),'--max-seconds',str(args.max_seconds)];source=BASE.read_text().split("if args.stage=='masks':")[0];source=source.replace("'backthreequarter':(.85,2,.12),'side':(2,0,.06)","'backthreequarter':(-.85,2,.12),'side':(-2,0,.06)");ns={'__file__':str(BASE),'__name__':'source_defs'}
try:exec(compile(source,str(BASE),'exec'),ns)
finally:sys.argv=oldargv
body,rig,scene=[ns[k] for k in ['body','rig','scene']];N=ns['N'];names=ns['names'];tri=ns['tri'];mw=ns['mw'];original=ns['weight'].copy();joint=ns['joint'];rest=ns['restWorld'];regions=ns['regions'];sourceAttrs=ns['hash_arrays'](body);assert all(b.bbone_segments==1 for b in rig.data.bones if b.name in names)
assert all(not m.use_deform_preserve_volume for m in body.modifiers if m.type=='ARMATURE')
canFile=args.candidate.resolve();assert canFile.is_relative_to(O.resolve()) and canFile.exists();z=np.load(canFile);patch=z['originalVertexIndices'];old=z['originalWeights'];candidate=z['candidateWeights'];assert np.array_equal(old,original[patch]) and np.array_equal(z['jointIndices'],joint[patch]);changed=np.any(candidate!=old,axis=1);changedIDs=patch[changed];expectedProtected=np.load(O/'corridor-proposal.npz')['protectedOriginalVertexIndices'];assert not np.intersect1d(changedIDs,expectedProtected).size
combined=original.copy();combined[patch]=candidate;group=[body.vertex_groups[n] for n in names];groupNames={g.index:g.name for g in body.vertex_groups};baselineActual=None
sys.path.insert(0,str(HERE.parent));from mixture_kernel import orientation_witnesses
edges=ns['canonical_edges'](tri);incident=np.flatnonzero(np.any(np.isin(tri,changedIDs),axis=1));localTri=tri[incident]
def actual_weights_digest():
 h=hashlib.sha256()
 for v in body.data.vertices:h.update(np.asarray([(g.group,g.weight) for g in v.groups],np.float64).tobytes());h.update(b';')
 return h.hexdigest()
originalWeightSHA=actual_weights_digest()
def apply(weights):
 maximum=0.
 for v in changedIDs:
  for k in range(4):
   if weights[v,k]>0:group[int(joint[v,k])].add([int(v)],float(weights[v,k]),'REPLACE')
 for v in changedIDs:
  expected={names[int(j)]:float(w) for j,w in zip(joint[v],weights[v]) if w>0};actual={groupNames[g.group]:g.weight for g in body.data.vertices[int(v)].groups if g.weight>0};assert set(expected)==set(actual),'Unexpected/dropped actual named support';maximum=max(maximum,max(abs(expected[n]-actual[n]) for n in expected))
 assert maximum<=1e-7,'Actual stored weight mismatch';ns['weight']=weights
 return maximum

def evaluated():
 for m in body.modifiers:m.show_viewport=True
 bpy.context.view_layer.update();ob=body.evaluated_get(bpy.context.evaluated_depsgraph_get());mesh=ob.to_mesh()
 try:
  assert len(mesh.vertices)==N;v=np.empty(N*3,np.float32);mesh.vertices.foreach_get('co',v)
 finally:ob.to_mesh_clear()
 for m in body.modifiers:m.show_viewport=False
 return v.reshape(-1,3)@mw[:3,:3].T+mw[:3,3]
cases=[{'name':'dance'+str(int(q*100)),'clip':'dance_01','phase':q} for q in [.45,.5,.55]]+[{'name':'neutral','clip':'neutral','phase':0},{'name':'box-extreme','clip':'box_02','phase':1.8541666666666667/(67/24)},{'name':'kick-high-flexion','clip':'front_kick_01','phase':.45},{'name':'synthetic-arm65','clip':'dance_01','phase':.5,'synthetic':True}]
r.update(sourceVertices=N,sourceTriangles=len(tri),sourceHashes=ns['report']['sourceHashes'],originalAttrs=sourceAttrs,sourceActualWeightsSHA256=originalWeightSHA,candidateSHA256=hashlib.sha256(canFile.read_bytes()).hexdigest(),protectedCount=len(expectedProtected),changedVertices=len(changedIDs),fixedWeightsChanged=0,protectedWeightsChanged=0)
renderQueue=[]
try:
 for case in cases:
  budget();apply(original);ns['setup_pose'](case,True);bones=np.array([rig.matrix_world@rig.pose.bones[n].matrix for n in names]);surfaces={};row={'case':case,'modes':{}}
  for label,weights in [('original',original),('candidate',combined)]:
   budget();errw=apply(weights);predicted,sourceSkin=ns['world_surface']();actual=evaluated();err=float(np.linalg.norm(predicted-actual,axis=1).max());assert err<1e-5 and np.array_equal(bones,np.array([rig.matrix_world@rig.pose.bones[n].matrix for n in names]));surfaces[label]=(predicted,actual);row['modes'][label]={'actualWeightReadbackMaxError':errw,'allVerticesLBSMaxErrorM':err,'regions':{}}
   for region in ['sleeve_armhole_R','torso_underarm_R']:
    mask=np.zeros(N,bool);mask[regions[region]]=True;row['modes'][label]['regions'][region]=ns['edge_metrics'](rest,actual,edges,mask)
  witness=orientation_witnesses(surfaces['original'][1][localTri],surfaces['candidate'][1][localTri]);row['actualOrientation']={'allIncidentTriangles':len(incident),'opposedCount':len(witness['opposed']),'newCollapsedCount':len(witness['newCollapsed']),'opposedOriginalTriangleIDs':incident[witness['opposed']].tolist()};row['protectedActualSurfaceDeltaM']=float(np.linalg.norm(surfaces['candidate'][1][expectedProtected]-surfaces['original'][1][expectedProtected],axis=1).max());row['allVerticesMaxDeltaM']=float(np.linalg.norm(surfaces['candidate'][1]-surfaces['original'][1],axis=1).max())
  np.savez_compressed(OUT/('source-worldskin-'+case['name']+'.npz'),worldSkin=sourceSkin,boneNames=np.array(names),phase=case['phase'])
  row['actualOrientation']['witnesses']=[{'originalTriangleID':int(incident[i]),'originalVertexIDs':localTri[i].tolist(),'baselineNormal':witness['beforeNormal'][i].tolist(),'candidateNormal':witness['afterNormal'][i].tolist(),'beforeAreaMm2':float(witness['beforeArea'][i]*1e6),'afterAreaMm2':float(witness['afterArea'][i]*1e6),'normalDot':float(witness['dot'][i])} for i in sorted(set(witness['opposed']+witness['newCollapsed']))]
  if case['name'] in ['dance45','dance50','dance55','synthetic-arm65']:renderQueue.append((case,surfaces,row,bones.copy()))
  r['cases'].append(row);save()
 failures=[c['case']['name'] for c in r['cases'] if c['actualOrientation']['opposedCount'] or c['actualOrientation']['newCollapsedCount'] or c['protectedActualSurfaceDeltaM']>1e-9]
 r['mandatoryPoseGateFailures']=failures;save()
 if failures:raise RuntimeError('Actual pose orientation/anchor gate RED; no beauty renders: '+','.join(failures))
 for case,surfaces,row,expectedBones in renderQueue:
  apply(original);ns['setup_pose'](case,True);assert np.array_equal(expectedBones,np.array([rig.matrix_world@rig.pose.bones[n].matrix for n in names])),'Render pose replay mismatch'
  target=np.array(json.loads((B/'corrective-v1/weight-witness-v1/renders/report.json').read_text())['renders'][2]['cameraTarget']);framing=(target,.32)
  for label,weights in [('original',original),('candidate',combined)]:
   apply(weights)
   for mat,override in [('source',None),('clay',ns['clay'])] if case['name']=='dance50' else [('clay',ns['clay'])]:
    scene.view_layers[0].material_override=override
    for view in ['side','front','backthreequarter'] if case['name']=='dance50' else ['side']:
     budget();item=ns['render'](case['name']+'-'+label+'-'+mat+'-'+view,surfaces[label][0],view,True,framing);item['material']='source-normal1' if override is None else 'diagnostic-flat-clay';row['modes'][label].setdefault('renders',[]).append(item)
 apply(original);assert actual_weights_digest()==originalWeightSHA and ns['hash_arrays'](body)==sourceAttrs;r['sourceRestAttrsAndOriginalWeightsRestored']=True;r['status']='COMPLETED_ACTUAL_REVIEW_NO_PROMOTION'
except BaseException as exc:r['status']='FAILED_ACTUAL_DIAGNOSTIC';r['error']=repr(exc);raise
finally:
 apply(original);r['originalWeightsRestoredAfterExitPath']=actual_weights_digest()==originalWeightSHA;r['sourceRestAttrsUnchangedAfterExitPath']=ns['hash_arrays'](body)==sourceAttrs;assert r['originalWeightsRestoredAfterExitPath'] and r['sourceRestAttrsUnchangedAfterExitPath'];r['elapsedSeconds']=time.monotonic()-started;save()

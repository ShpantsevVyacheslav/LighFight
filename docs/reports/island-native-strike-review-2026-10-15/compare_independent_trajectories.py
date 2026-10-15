"""Frozen Blender original-native bones versus Unity imported Generic: ONE phase0 fit.
Never per-frame/per-take fits, bounds normalization or time warping. No skin acceptance.
"""
import argparse,csv,hashlib,json,pathlib,re,sys
import os
os.environ["OPENBLAS_NUM_THREADS"]="1"
import numpy as np
class Frames(dict):
 def __init__(self):super().__init__();self.original_names={}
NAMES={'box_01':531,'box_02':671,'front_kick_01':601}
SHA='5cc8b7208f3267cb1e20909bcff4eb04a9b9bf58ce7f28ebb77bbdb516c41a24'
def sha(p):return hashlib.sha256(p.read_bytes()).hexdigest()
def canonical(s):
 # Only syntactic exporter spelling; NO anatomical aliases or hierarchy remapping.
 return re.sub(r'[_\s]','',s.rsplit(':',1)[-1]).lower()
def load(path):
 frames=Frames()
 with path.open(newline='') as f:
  for r in csv.DictReader(f):
   t=float(r['seconds']);name=canonical(r['joint']);v=np.array([float(r[k]) for k in ['x','y','z']])
   if not np.isfinite(t) or not np.all(np.isfinite(v)):raise ValueError('Nonfinite '+str(path))
   if name in frames.original_names and frames.original_names[name]!=r['joint']:raise ValueError('Canonical spelling changed across frames '+name)
   frames.original_names[name]=r['joint']
   frame=frames.setdefault(t,{})
   if name in frame:raise ValueError('Ambiguous duplicate/canonical name '+name+' '+str(path))
   frame[name]=v
 if not frames:raise ValueError('No frames '+str(path))
 return frames

def compare(source,unity,max_error=1e-4):
 pairs=[];mapping={};max_time_error=0.;samples=0
 for take,count in NAMES.items():
  a=source[take];b=unity[take];ta=sorted(a);tb=np.array(sorted(b));joints=sorted(a[ta[0]])
  if len(ta)!=count or len(tb)!=count or len(joints)!=67:raise ValueError('Exact sample/bone count gate '+take)
  if abs(ta[0])>1e-8 or abs(tb[0])>1e-8:raise ValueError('Phase0 required')
  used=set();aligned=[]
  for t in ta:
   index=int(np.argmin(abs(tb-t)));u=float(tb[index]);dt=abs(t-u);max_time_error=max(max_time_error,dt)
   if dt>1e-5 or index in used:raise ValueError('Exacttime/one-to-one gate '+take+' '+str(t))
   used.add(index)
   if sorted(a[t])!=joints or any(j not in b[u] for j in joints):raise ValueError('Missing/inconsistent joint names '+take)
   aligned.append((t,u,np.stack([a[t][j] for j in joints]),np.stack([b[u][j] for j in joints])))
  mapping[take]=joints;pairs.append((take,aligned));samples+=len(aligned)
 # Uniform similarity may include reflection; only all three phase0 poses train the ONE fit.
 x=np.concatenate([a[0][2] for _,a in pairs]);y=np.concatenate([a[0][3] for _,a in pairs]);xm=x.mean(0);ym=y.mean(0);xc=x-xm;yc=y-ym
 u,s,v=np.linalg.svd(xc.T@yc)
 if np.linalg.matrix_rank(xc)<3:raise ValueError('Degenerate phase0 training points')
 rotation=u@v;scale=float(s.sum()/np.sum(xc*xc));translation=ym-scale*xm@rotation
 if not np.isfinite(scale) or scale<=0:raise ValueError('Nonpositive similarity scale')
 details=[];all_errors=[];training_errors=[];worst=None
 for take,aligned in pairs:
  errors=[];worst_take=None
  for i,(t,ut,src,dst) in enumerate(aligned):
   e=np.linalg.norm(scale*src@rotation+translation-dst,axis=1)
   if i==0:training_errors.extend(e.tolist());continue
   errors.extend(e.tolist());k=int(np.argmax(e));item={'take':take,'sourceSeconds':t,'unitySeconds':ut,'joint':mapping[take][k],'errorUnityUnits':float(e[k])}
   if worst_take is None or item['errorUnityUnits']>worst_take['errorUnityUnits']:worst_take=item
  if worst is None or worst_take['errorUnityUnits']>worst['errorUnityUnits']:worst=worst_take
  all_errors.extend(errors);details.append({'take':take,'holdoutPhases':len(aligned)-1,'rmsUnityUnits':float(np.sqrt(np.mean(np.square(errors)))),'maximumUnityUnits':max(errors),'worst':worst_take})
 passed=max(training_errors)<=max_error and max(all_errors)<=max_error
 return {'status':'BONE_TRAJECTORY_GATE_PASSED_NOT_SKIN_OR_GAMEPLAY_ACCEPTANCE' if passed else 'BLOCKED_INDEPENDENT_SOURCE_TRAJECTORY_MISMATCH','passed':passed,'phase0TrainingPoses':3,'phase0TrainingJoints':len(x),'holdoutPhases':samples-3,'holdoutJointSamples':len(all_errors),'fitCount':1,'fitSpace':'ONE uniform similarity/reflection trained ONLY on phase0 of all3takes; Blender armature-local to Unity model-local','scale':scale,'orthogonalRowMatrix':rotation.tolist(),'determinant':float(np.linalg.det(rotation)),'translation':translation.tolist(),'maximumTimeMismatchSeconds':max_time_error,'thresholdUnityUnits':max_error,'trainingMaximumUnityUnits':max(training_errors),'holdoutRMSUnityUnits':float(np.sqrt(np.mean(np.square(all_errors)))),'holdoutMaximumUnityUnits':max(all_errors),'worst':worst,'takes':details,'jointMap':{take:[{'canonical':j,'blender':getattr(source[take],'original_names',{}).get(j,j),'unity':getattr(unity[take],'original_names',{}).get(j,j)} for j in names] for take,names in mapping.items()},'sourceAuthorityLimit':'Independent Blender-imported original FBX bone centers only. Does NOT establish original skin, local rotations/normal frames, object world-root trajectory or render/physics parity. ImportedGeneric↔LNC1 is a separate self-consistency gate.'}
def main():
 p=argparse.ArgumentParser();p.add_argument('--blender-source',type=pathlib.Path,required=True);p.add_argument('--unity-evidence',type=pathlib.Path,required=True);p.add_argument('--source-fbx',type=pathlib.Path,required=True);p.add_argument('--manifest',type=pathlib.Path,default=pathlib.Path(__file__).with_name('independent-source-inputs.json'));p.add_argument('--output',type=pathlib.Path,required=True);p.add_argument('--max-error',type=float,default=1e-4);a=p.parse_args()
 try:
  frozen=json.loads(a.manifest.read_text());audit=json.loads((a.blender_source/'source-audit.json').read_text())
  if sha(a.source_fbx)!=SHA or frozen['sourceSHA256']!=SHA or audit['sourceSHA256']!=SHA:raise ValueError('Original FBX SHA authority failed')
  for row in frozen['files']:
   f=a.blender_source/row['name']
   if f.stat().st_size!=row['bytes'] or sha(f)!=row['sha256']:raise ValueError('Frozen Blender source input differs '+row['name'])
  source={n:load(a.blender_source/(n+'.trajectory.csv')) for n in NAMES};unity={n:load(a.unity_evidence/(n+'.trajectory.csv')) for n in NAMES}
  result=compare(source,unity,a.max_error);result['sourceSHA256']=SHA;result['inputManifestSHA']=sha(a.manifest);result['unityCSVsha256']={n:sha(a.unity_evidence/(n+'.trajectory.csv')) for n in NAMES};result['unityCSVFiles']=[{'take':n,'sha256':sha(a.unity_evidence/(n+'.trajectory.csv'))} for n in NAMES]
 except Exception as e:result={'status':'BLOCKED_INPUT_OR_MAPPING','passed':False,'error':str(e)}
 a.output.parent.mkdir(parents=True,exist_ok=True);a.output.write_text(json.dumps(result,indent=2)+'\n');print(json.dumps({k:result[k] for k in ['status','passed','holdoutPhases','holdoutMaximumUnityUnits','error'] if k in result}));return 0 if result['passed'] else 1
if __name__=='__main__':sys.exit(main())

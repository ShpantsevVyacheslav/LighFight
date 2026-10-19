"""Offline complete weighted feature boxes. No Unity/Blender; finite captured poses only.

prepare: every original vertex contributes once, original four influences unchanged.
query: actual target skin matrices mapped to original source geometry convention.
Matrix input is [poses,bones,4,4] and must already include original mesh binding,
fixedC+anchor as appropriate. Do not feed bare bone-world matrices.
"""
from pathlib import Path
import argparse,hashlib,json,os
os.environ.setdefault('OPENBLAS_NUM_THREADS','1')
os.environ.setdefault('OMP_NUM_THREADS','1')
import numpy as np

def sha(p):
 h=hashlib.sha256()
 with Path(p).open('rb') as f:
  while b:=f.read(8388608):h.update(b)
 return h.hexdigest()
def write_json(p,obj):Path(p).write_text(json.dumps(obj,indent=2)+'\n')
def prepare(source,manifest,output,leaf_size=0,rebase=False):
 if output.exists():raise ValueError('New output required')
 a=np.load(source);p=a['positionsBlenderLocal'].astype(np.float64);w=a['weights'].astype(np.float64);j=a['jointIndices'].astype(np.int32)
 names=json.loads(manifest.read_text())['boneNames']
 if p.shape!=(994452,3) or w.shape!=j.shape or w.shape!=(len(p),4):raise ValueError('Complete original shape required')
 if not np.isfinite(p).all() or not np.isfinite(w).all() or np.any(w<0) or np.any(j<0) or np.any(j>=len(names)):raise ValueError('Invalid inputs')
 # Retain zero slots too; never merge influences or normalize original weights.
 tuples,inverse=np.unique(j,axis=0,return_inverse=True)
 ph=np.column_stack((p,np.ones(len(p))))
 features=(w[:,:,None]*ph[:,None,:]).reshape(len(p),16)
 if rebase:features[:,:4]=features.reshape(len(p),4,4).sum(1)
 if leaf_size:
  if leaf_size<16 or leaf_size>1024:raise ValueError('Bounded leaf size16..1024')
  order=np.argsort(inverse,kind='stable');offset=np.r_[0,np.cumsum(np.bincount(inverse,minlength=len(tuples)))];leaves=[];leaf_tuples=[]
  def split(ids,tuple_id):
   if len(ids)<=leaf_size:leaves.append(ids);leaf_tuples.append(tuples[tuple_id]);return
   extent=np.ptp(features[ids],axis=0);axis=int(np.argmax(extent))
   ranked=ids[np.argsort(features[ids,axis],kind='stable')];mid=len(ids)//2
   split(ranked[:mid],tuple_id);split(ranked[mid:],tuple_id)
  for group in range(len(tuples)):split(order[offset[group]:offset[group+1]],group)
  refined=np.full(len(p),-1,dtype=np.int32);seen=np.zeros(len(p),dtype=np.uint8)
  for group,ids in enumerate(leaves):
   if np.any(seen[ids]):raise ValueError('Overlapping leaf assignment')
   refined[ids]=group;seen[ids]=1
  if np.any(seen!=1) or np.any(refined<0):raise ValueError('Missing original vertex')
  tuples=np.array(leaf_tuples,dtype=np.int32);inverse=refined
 lo=np.full((len(tuples),16),np.inf);hi=np.full_like(lo,-np.inf)
 np.minimum.at(lo,inverse,features);np.maximum.at(hi,inverse,features)
 counts=np.bincount(inverse,minlength=len(tuples))
 if counts.sum()!=len(p) or np.any(counts==0):raise ValueError('Coverage partition incomplete')
 # Features are products of exact Float32 values evaluated in Float64. Outward
 # nextafter bounds protect storage arithmetic; query adds dot-product error budget.
 error=8*np.finfo(np.float64).eps*np.maximum(np.abs(lo),np.abs(hi))
 lo=np.nextafter(lo-error,-np.inf);hi=np.nextafter(hi+error,np.inf)
 np.savez(output,lower=lo,upper=hi,joints=tuples,counts=counts,boneNames=np.array(names),vertexGroup=inverse.astype(np.int32),rebased=np.array(rebase))
 write_json(str(output)+'.json',dict(scope='COMPLETE_ORIGINAL_WEIGHTED_FEATURE_BOXES_NOT_MOTION_ACCEPTANCE',vertices=len(p),groups=len(tuples),influences=int(np.count_nonzero(w)),sourceSHA256=sha(source),manifestSHA256=sha(manifest),summarySHA256=sha(output),geometryWeightsModified=False,rebaseCommonBone=rebase,leafSizeLimit=leaf_size,partition='stable longest16D feature extent median within exacttuple' if leaf_size else 'global exacttuple'))

def evaluate(summary,matrices):
 lo=summary['lower'];hi=summary['upper'];j=summary['joints']
 # Each tuple's16D coefficient = four skin matrix rows in original joint order.
 coeff4=matrices[j,:3,:].copy()
 if 'rebased' in summary and bool(summary['rebased']):coeff4[:,1:]-=coeff4[:,:1]
 coeff=coeff4.transpose(0,2,1,3).reshape(len(j),3,16)
 low=np.where(coeff>=0,lo[:,None,:],hi[:,None,:]);high=np.where(coeff>=0,hi[:,None,:],lo[:,None,:])
 a=coeff*low;b=coeff*high
 # Float64 dot products: conservative gamma32 times absolute product sum;
 # nextafter also widens cancellation result. This covers arithmetic, not
 # an unknown upstream matrix/capture error.
 gamma=32*np.finfo(np.float64).eps/(1-32*np.finfo(np.float64).eps)
 lower=np.nextafter(a.sum(2)-gamma*np.abs(a).sum(2),-np.inf)
 upper=np.nextafter(b.sum(2)+gamma*np.abs(b).sum(2),np.inf)
 return lower.min(0),upper.max(0)

def query(summary_path,capture_path,output,margin):
 if output.exists() or not np.isfinite(margin) or margin<0:raise ValueError('New output/finite margin required')
 summary=np.load(summary_path);capture=np.load(capture_path)
 required=['skinMatrices','times','boneNames','scope','inputPoseSHA256','ownerBlendSHA256','geometrySHA256']
 if any(k not in capture for k in required):raise ValueError('Actual capture provenance incomplete')
 if list(capture['boneNames'])!=list(summary['boneNames']):raise ValueError('Named matrix order mismatch')
 metadata=json.loads(Path(str(summary_path)+'.json').read_text())
 if metadata['summarySHA256']!=sha(summary_path) or str(capture['geometrySHA256'])!=metadata['sourceSHA256']:raise ValueError('Geometry/summary identity mismatch')
 if not str(capture['inputPoseSHA256']) or not str(capture['ownerBlendSHA256']):raise ValueError('Actual transition identity missing')
 m=capture['skinMatrices'];times=capture['times']
 if m.ndim!=4 or m.shape[1:]!=(len(summary['boneNames']),4,4) or len(times)!=len(m) or len(m)<2 or not np.isfinite(m).all() or not np.isfinite(times).all() or np.any(np.diff(times)<=0):raise ValueError('Complete finite actual pose matrix domain required')
 if not np.allclose(m[:,:,3,:],np.array([0,0,0,1]),atol=0,rtol=0):raise ValueError('Affine matrices required')
 rows=[]
 for t,pose in zip(times,m):
  lo,hi=evaluate(summary,pose);rows.append(dict(time=float(t),min=lo.tolist(),max=hi.tolist()))
 low=np.min([r['min'] for r in rows],axis=0)-margin;high=np.max([r['max'] for r in rows],axis=0)+margin
 write_json(output,dict(scope='FINITE_ACTUAL_CAPTURE_FEATURE_BOX_UNION_NOT_CONTINUOUS',summarySHA256=sha(summary_path),captureSHA256=sha(capture_path),poses=len(rows),explicitMargin=margin,min=low.tolist(),max=high.tolist(),perPose=rows,actualGPUAccepted=False,directFullMeshHoldoutsAccepted=False))

def test():
 # Adversarial rotated sweep goes outside union of endpoints, captured middle
 # matrix is included and interval dot covers every assigned point.
 p=np.array([[1.,0,0,1],[.4,.2,0,1]])
 features=np.zeros((2,16));features[:,:4]=p
 s={'lower':features.min(0)[None,:],'upper':features.max(0)[None,:],'joints':np.array([[0,0,0,0]])}
 mats=[]
 for angle in [-np.pi/4,0,np.pi/4]:
  c=np.cos(angle);q=np.sin(angle);m=np.eye(4);m[:2,:2]=[[c,-q],[q,c]];mats.append(m)
 boxes=[evaluate(s,m[None,:,:]) for m in mats]
 assert boxes[1][1][0]>max(boxes[0][1][0],boxes[2][1][0]) # endpoint-only RED
 for m,(lo,hi) in zip(mats,boxes):
  actual=p@m.T
  assert np.all(actual[:,:3]>=lo) and np.all(actual[:,:3]<=hi)
 rng=np.random.default_rng(193)
 for _ in range(100):
  f=rng.normal(size=(31,16));coeff=rng.normal(size=(4,4,4));coeff[:,3,:]=[0,0,0,1]
  ss={'lower':f.min(0)[None,:],'upper':f.max(0)[None,:],'joints':np.array([[0,1,2,3]])}
  lo,hi=evaluate(ss,coeff)
  exact=f@coeff[:,:3,:].transpose(1,0,2).reshape(3,16).T
  assert np.all(exact>=lo) and np.all(exact<=hi)
 print('PASS full-feature dot bounds100random cases + middle rotation excursion rejects endpoint-only union')

if __name__=='__main__':
 ap=argparse.ArgumentParser();sub=ap.add_subparsers(dest='mode',required=True)
 p=sub.add_parser('prepare');p.add_argument('source',type=Path);p.add_argument('manifest',type=Path);p.add_argument('output',type=Path);p.add_argument('--leaf-size',type=int,default=0);p.add_argument('--rebase',action='store_true')
 q=sub.add_parser('query');q.add_argument('summary',type=Path);q.add_argument('capture',type=Path);q.add_argument('output',type=Path);q.add_argument('--margin',type=float,required=True)
 sub.add_parser('test');args=ap.parse_args()
 if args.mode=='prepare':prepare(args.source,args.manifest,args.output,args.leaf_size,args.rebase)
 elif args.mode=='query':query(args.summary,args.capture,args.output,args.margin)
 else:test()

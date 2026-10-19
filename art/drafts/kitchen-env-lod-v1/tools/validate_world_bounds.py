"""Pure read-only check: composed GLB world bounds equal accepted saved Blender placement."""
import json
from pathlib import Path
import numpy as np
from glb_static import load
ROOT=Path(__file__).resolve().parents[4];OUT=ROOT/'art/drafts/kitchen-env-lod-v1';g,b=load(OUT/'kitchen-master.glb');parents={c:i for i,n in enumerate(g['nodes']) for c in n.get('children',[])};cache={}
def world(i):
 if i in cache:return cache[i]
 n=g['nodes'][i]
 if 'matrix' in n:m=np.asarray(n['matrix']).reshape(4,4).T
 else:
  x,y,z,w=n.get('rotation',[0,0,0,1]);r=np.array([[1-2*(y*y+z*z),2*(x*y-z*w),2*(x*z+y*w)],[2*(x*y+z*w),1-2*(x*x+z*z),2*(y*z-x*w)],[2*(x*z-y*w),2*(y*z+x*w),1-2*(x*x+y*y)]]);m=np.eye(4);m[:3,:3]=r@np.diag(n.get('scale',[1,1,1]));m[:3,3]=n.get('translation',[0,0,0])
 cache[i]=(world(parents[i])@m) if i in parents else m;return cache[i]
expected={};v1=json.loads((ROOT/'art/drafts/kitchen-hybrid-v1/manifest.json').read_text());expected['table']=(v1['clearFloor']['furnitureBoundsMin'],v1['clearFloor']['furnitureBoundsMax'])
for role,folder in [('fridge','kitchen-hybrid-v2'),('coffee','kitchen-hybrid-v3')]:
 j=json.loads((ROOT/'art/drafts'/folder/'manifest.json').read_text());expected[role]=(j['newComponentWorldBoundsMin'],j['newComponentWorldBoundsMax'])
rows=[]
for role,(lo,hi) in expected.items():
 wrapper=next(i for i,n in enumerate(g['nodes']) if n.get('name')=='ROLE_'+role.upper());stack=[wrapper];points=[]
 while stack:
  i=stack.pop();n=g['nodes'][i];stack.extend(n.get('children',[]))
  if 'mesh' not in n:continue
  for p in g['meshes'][n['mesh']]['primitives']:
   a=g['accessors'][p['attributes']['POSITION']];v=g['bufferViews'][a['bufferView']];verts=np.ndarray((a['count'],3),dtype='<f4',buffer=b,offset=v.get('byteOffset',0)+a.get('byteOffset',0),strides=(v.get('byteStride',12),4));wm=world(i);q=verts@wm[:3,:3].T+wm[:3,3];q=q[:,[0,2,1]];q[:,1]*=-1;points.append((q.min(0),q.max(0)))
 actualLo=np.min([p[0] for p in points],axis=0);actualHi=np.max([p[1] for p in points],axis=0);error=float(np.max(np.abs(np.r_[actualLo-np.asarray(lo),actualHi-np.asarray(hi)])));assert error<1e-6,(role,error);rows.append({'role':role,'GLBWorldBoundsBlenderZUp':[actualLo.tolist(),actualHi.tolist()],'acceptedBlendBounds':[lo,hi],'maxAbsDifference':error})
report={'status':'PASS','scope':'Original source buffers plus uniform wrapper transforms reproduce accepted hybrid placement; no render/mobile acceptance.','rows':rows};(OUT/'portable-master-world-bounds.json').write_text(json.dumps(report,indent=2));print(json.dumps(report,indent=2))

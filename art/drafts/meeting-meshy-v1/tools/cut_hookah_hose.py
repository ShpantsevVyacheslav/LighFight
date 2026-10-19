"""Static derivative: retain exact source body triangles, cut only the low external hose.
Source has one connected component; new cut boundary is capped as a dark hose socket.
Original files and kept position/normal/UV payloads remain unchanged. Actual review required.
"""
import sys,json,copy,hashlib,struct
from pathlib import Path
import numpy as np
ROOT=Path(__file__).resolve().parents[4];sys.path.insert(0,str(ROOT/'art/drafts/kitchen-env-lod-v1/tools'));from glb_static import load,write,audit_images
src=Path(sys.argv[1]);dst=Path(sys.argv[2]);g,original=load(src);g=copy.deepcopy(g);b=bytearray(original);assert len(g['meshes'])==1 and len(g['meshes'][0]['primitives'])==1
pr=g['meshes'][0]['primitives'][0]
def read(i,n):
 a=g['accessors'][i];v=g['bufferViews'][a['bufferView']];dt={5126:'<f4',5125:'<u4',5123:'<u2'}[a['componentType']];return np.ndarray((a['count'],n),dtype=dt,buffer=original,offset=v.get('byteOffset',0)+a.get('byteOffset',0),strides=(v.get('byteStride',np.dtype(dt).itemsize*n),np.dtype(dt).itemsize))
p=read(pr['attributes']['POSITION'],3);f=read(pr['indices'],1).reshape(-1,3).astype(np.int64);c=p[f].mean(axis=1);core=np.array([-.222595,.046975]);radius=np.linalg.norm(c[:,[0,2]]-core,axis=1);limit=np.where(c[:,1]<-.78,.40,.30);keep=(c[:,1]>-.20)|(radius<=limit);kept=f[keep];assert kept.size>0 and keep.mean()>.25
# Weld only for finding physical boundary; source UV seams remain exact in kept triangles.
u,inv=np.unique(np.rint(p/1e-7).astype(np.int64),axis=0,return_inverse=True);wf=inv[f];wk=inv[kept];allEdges=np.sort(np.concatenate([wf[:,[0,1]],wf[:,[1,2]],wf[:,[2,0]]]),axis=1);edges,counts=np.unique(allEdges,axis=0,return_counts=True);keptEdges=np.sort(np.concatenate([wk[:,[0,1]],wk[:,[1,2]],wk[:,[2,0]]]),axis=1);ke,kn=np.unique(keptEdges,axis=0,return_counts=True);boundary=ke[kn==1];adj={}
for a,z in boundary:adj.setdefault(int(a),[]).append(int(z));adj.setdefault(int(z),[]).append(int(a))
assert all(len(v)==2 for v in adj.values()),'Cut produces a branched/non-manifold boundary; inspect before export'
loops=[];visited=set()
for start in adj:
 if start in visited:continue
 loop=[];cur=start;prev=None
 while cur not in visited:
  visited.add(cur);loop.append(cur);nxt=adj[cur][0] if adj[cur][0]!=prev else adj[cur][1];prev,cur=cur,nxt
 assert cur==start and len(loop)>=3;loops.append(loop)
assert loops,'No socket boundary found; source cut assumptions changed'
def add(vals,n,kind,fmt,target):
 flat=[x for v in vals for x in v] if n>1 else vals;b.extend(b'\0'*((-len(b))%4));vi=len(g['bufferViews']);raw=struct.pack('<'+str(len(flat))+fmt,*flat);g['bufferViews'].append({'buffer':0,'byteOffset':len(b),'byteLength':len(raw),'target':target});b.extend(raw);a={'bufferView':vi,'componentType':5126 if fmt=='f' else 5125,'count':len(vals),'type':kind}
 if kind=='VEC3':a.update(min=np.min(vals,axis=0).tolist(),max=np.max(vals,axis=0).tolist())
 g['accessors'].append(a);return len(g['accessors'])-1
used=np.unique(kept);mapping=np.zeros(len(p),dtype=np.int64);mapping[used]=np.arange(len(used));newattrs={}
for semantic,old in pr['attributes'].items():
 ac=g['accessors'][old];kind=ac['type'];n={'SCALAR':1,'VEC2':2,'VEC3':3,'VEC4':4}[kind];assert ac['componentType']==5126,'Static float attribute required';values=read(old,n)[used];newattrs[semantic]=add(values.tolist() if n>1 else values.reshape(-1).tolist(),n,kind,'f',34962)
pr['attributes']=newattrs;pr['indices']=add(mapping[kept].reshape(-1).tolist(),1,'SCALAR','I',34963);caps=[]
for loop in loops:
 pp=u[loop].astype(float)*1e-7;center=pp.mean(axis=0);normal=np.array([center[0]-core[0],0,center[2]-core[1]]);normal/=np.linalg.norm(normal);verts=np.concatenate([center[None,:],pp]);idx=[]
 for i in range(len(pp)):
  aa=i+1;bb=(i+1)%len(pp)+1
  if np.dot(np.cross(verts[aa]-center,verts[bb]-center),normal)>0:idx.extend([0,aa,bb])
  else:idx.extend([0,bb,aa])
 caps.append({'sourceLocalCenter':center.tolist(),'normalSourceLocal':normal.tolist(),'loopVertices':len(loop)})
 attrs={'POSITION':add(verts.tolist(),3,'VEC3','f',34962),'NORMAL':add([normal.tolist()]*len(verts),3,'VEC3','f',34962)};mat=len(g['materials']);g['materials'].append({'name':'Hose socket cut cap','pbrMetallicRoughness':{'baseColorFactor':[.012,.014,.016,1],'metallicFactor':0,'roughnessFactor':.8}});g['meshes'][0]['primitives'].append({'attributes':attrs,'indices':add(idx,1,'SCALAR','I',34963),'material':mat})
dst.parent.mkdir(parents=True,exist_ok=True);sha=write(dst,g,b);assert audit_images(src)==audit_images(dst);report={'status':'DERIVATIVE_CUT_REQUIRES_ACTUAL_VISUAL_CHECK','source':str(src),'sourceSHA256':hashlib.sha256(src.read_bytes()).hexdigest(),'SHA256':sha,'sourceTriangles':len(f),'retainedBodyTriangles':len(kept),'removedHoseTriangles':int((~keep).sum()),'caps':caps,'sourceImagesExact':True,'keptSourceVertexAttributePayloadsExact':True,'filter':'Keep all y>-.20; lower geometry within radius.30, enlarged to.40 below y=-.78 to preserve glass foot of stem centerXZ[-.222595,.046975]. Source-specific, not a generic hookah cutter.'};dst.with_suffix('.json').write_text(json.dumps(report,indent=2));print(json.dumps(report))

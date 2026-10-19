import sys,numpy as np,json
sys.path.insert(0,'art/drafts/meeting-meshy-v1/tools');from assemble import load
p='art/drafts/meeting-meshy-v1/lod/furniture-80k-weld.glb';g,b=load(p);p0=g['meshes'][0]['primitives'][0]
def arr(ai):
 a=g['accessors'][ai];v=g['bufferViews'][a['bufferView']];sz={'VEC3':3,'SCALAR':1}[a['type']];dt={5126:'<f4',5125:'<u4',5123:'<u2'}[a['componentType']];return np.ndarray((a['count'],sz),dtype=dt,buffer=b,offset=v.get('byteOffset',0)+a.get('byteOffset',0),strides=(v.get('byteStride',np.dtype(dt).itemsize*sz),np.dtype(dt).itemsize))
P=arr(p0['attributes']['POSITION']);F=arr(p0['indices']).ravel().reshape(-1,3);uniq,inv=np.unique(np.round(P,6),axis=0,return_inverse=True);parent=np.arange(len(uniq))
def find(x):
 while parent[x]!=x:parent[x]=parent[parent[x]];x=parent[x]
 return x
for ia,ib,ic in inv[F]:
 ra,rb,rc=find(ia),find(ib),find(ic);parent[rb]=ra;parent[rc]=ra
r=np.array([find(i) for i in inv]);P=P*(1.4/.3764801025390625)+[0,0,4.8];rows=[]
for i,c in zip(*np.unique(r,return_counts=True)):
 if c<30:continue
 Q=P[r==i];rows.append({'root':int(i),'vertices':int(c),'min':Q.min(0).tolist(),'max':Q.max(0).tolist(),'center':Q.mean(0).tolist()})
rows.sort(key=lambda r:-r['vertices']);print(json.dumps(rows[:30],indent=2));np.savez('art/drafts/meeting-meshy-v1/furniture-components.npz',P=P,component=r)

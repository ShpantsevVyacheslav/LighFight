"""Small auxiliary open red cup, approved lightweight authored prop, not a Meshy output."""
import sys,math,struct
from pathlib import Path
ROOT=Path(__file__).resolve().parents[4];OUT=ROOT/'art/drafts/meeting-meshy-v1';sys.path.insert(0,str(ROOT/'art/drafts/kitchen-env-lod-v1/tools'));from glb_static import write
P=[];N=[];I=[]
def face(ps,ns):
 start=len(P);P.extend(ps);N.extend(ns)
 e=[ps[1][j]-ps[0][j] for j in range(3)];f=[ps[2][j]-ps[0][j] for j in range(3)];cross=[e[1]*f[2]-e[2]*f[1],e[2]*f[0]-e[0]*f[2],e[0]*f[1]-e[1]*f[0]];ids=[0,1,2,0,2,3] if sum(cross[j]*ns[0][j] for j in range(3))>0 else [0,2,1,0,3,2];I.extend(start+i for i in ids)
for i in range(32):
 a=2*math.pi*i/32;b=2*math.pi*(i+1)/32
 for inside in [False,True]:
  r0=.047-(.003 if inside else 0);r1=.065-(.003 if inside else 0);y0=.008 if inside else 0;y1=.23;ps=[(r0*math.cos(a),y0,r0*math.sin(a)),(r1*math.cos(a),y1,r1*math.sin(a)),(r1*math.cos(b),y1,r1*math.sin(b)),(r0*math.cos(b),y0,r0*math.sin(b))];sign=-1 if inside else 1;normal=lambda t:(sign*math.cos(t),sign*(r0-r1)/.23,sign*math.sin(t));face(ps,[normal(a),normal(a),normal(b),normal(b)])
 face([(.065*math.cos(a),.23,.065*math.sin(a)),(.062*math.cos(a),.23,.062*math.sin(a)),(.062*math.cos(b),.23,.062*math.sin(b)),(.065*math.cos(b),.23,.065*math.sin(b))],[(0,1,0)]*4)
 # Interior and exterior bottom, small quads split along center (one degenerate triangle avoided below).
 for y,r,n in [(.008,.044,(0,1,0)),(0,.047,(0,-1,0))]:
  start=len(P);ps=[(0,y,0),(r*math.cos(a),y,r*math.sin(a)),(r*math.cos(b),y,r*math.sin(b))];P.extend(ps);N.extend([n]*3);I.extend([start,start+2,start+1] if n[1]>0 else [start,start+1,start+2])
g={'asset':{'version':'2.0'},'bufferViews':[],'accessors':[],'materials':[{'name':'Auxiliary red plastic cup','pbrMetallicRoughness':{'baseColorFactor':[.5,.012,.025,1],'metallicFactor':0,'roughnessFactor':.32}}],'meshes':[],'nodes':[{'name':'Auxiliary red cup','mesh':0}],'scenes':[{'nodes':[0]}],'scene':0};d=bytearray()
def acc(v,n,kind,fmt,target):
 flat=[x for row in v for x in row] if n>1 else v;d.extend(b'\0'*((-len(d))%4));vi=len(g['bufferViews']);raw=struct.pack('<'+str(len(flat))+fmt,*flat);g['bufferViews'].append({'buffer':0,'byteOffset':len(d),'byteLength':len(raw),'target':target});d.extend(raw);a={'bufferView':vi,'componentType':5126 if fmt=='f' else 5125,'count':len(v),'type':kind}
 if kind=='VEC3':a.update(min=[min(row[j] for row in v) for j in range(3)],max=[max(row[j] for row in v) for j in range(3)])
 g['accessors'].append(a);return len(g['accessors'])-1
attrs={'POSITION':acc(P,3,'VEC3','f',34962),'NORMAL':acc(N,3,'VEC3','f',34962)};ix=acc(I,1,'SCALAR','I',34963);g['meshes']=[{'name':'Auxiliary cup','primitives':[{'attributes':attrs,'indices':ix,'material':0}]}];write(OUT/'aux-red-cup.glb',g,d);print('triangles',len(I)//3)

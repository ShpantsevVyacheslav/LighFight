"""Selective raw FBX feasibility audit; NumPy/std only, no DCC/candidate creation."""
import os
for k in ['OPENBLAS_NUM_THREADS','OMP_NUM_THREADS','MKL_NUM_THREADS']:os.environ[k]='1'
import struct,zlib,json,hashlib,time
from pathlib import Path
import numpy as np
O=Path(__file__).resolve().parents[1];ROOT=O.parents[2]
WANTED={'Vertices','PolygonVertexIndex','Transform','TransformLink','Matrix','Indexes','Weights'}
class Node:
 def __init__(self,name,p,c):self.name,self.p,self.c=name,p,c
class Reader:
 def __init__(self,path):self.b=path.read_bytes();assert self.b.startswith(b'Kaydara FBX Binary');self.v=struct.unpack_from('<I',self.b,23)[0];self.o=27
 def take(self,n):p=self.b[self.o:self.o+n];self.o+=n;return p
 def prop(self,owner):
  t=self.take(1).decode();fm={'Y':'h','C':'?','I':'i','F':'f','D':'d','L':'q'}
  if t in fm:return struct.unpack('<'+fm[t],self.take(struct.calcsize(fm[t])))[0]
  if t in ['S','R']:
   n=struct.unpack('<I',self.take(4))[0];b=self.take(n);return b.decode('utf8','replace') if t=='S' else {'rawBytesSkipped':n}
  if t in 'fdilbc':
   n,enc,size=struct.unpack('<III',self.take(12));b=self.take(size)
   if owner not in WANTED:return {'arrayCountSkipped':n,'type':t}
   b=zlib.decompress(b) if enc else b;return np.frombuffer(b,dtype={'f':'<f4','d':'<f8','i':'<i4','l':'<i8','b':'?','c':'i1'}[t])
  raise ValueError(t)
 def node(self):
  end,n,size=struct.unpack('<QQQ' if self.v>=7500 else '<III',self.take(24 if self.v>=7500 else 12));ln=self.take(1)[0]
  if not end:return None
  name=self.take(ln).decode();p=[self.prop(name) for _ in range(n)];c=[]
  while self.o<end:
   ch=self.node()
   if ch is None:break
   c.append(ch)
  self.o=end;return Node(name,p,c)
 def all(self):
  out=[]
  while self.o<len(self.b):
   n=self.node()
   if n is None:break
   out.append(n)
  return out

def child(node,name):return next((c for c in node.c if c.name==name),None)
def props(node):
 p=child(node,'Properties70');return {x.p[0]:x.p[4:] for x in p.c if x.name=='P'} if p else {}
def clean(s):return s.split('\0')[0].split('::')[-1]
def audit(path):
 reader=Reader(path);ns=reader.all();objs=next(x for x in ns if x.name=='Objects').c;con=next(x for x in ns if x.name=='Connections').c;models={x.p[0]:x for x in objs if x.name=='Model'};geo=[x for x in objs if x.name=='Geometry' and x.p[-1]=='Mesh'];assert len(geo)==1;g=geo[0];v=child(g,'Vertices').p[0].reshape(-1,3);idx=child(g,'PolygonVertexIndex').p[0];ends=np.flatnonzero(idx<0);sizes=np.diff(np.r_[-1,ends]);tri=int(np.sum(sizes-2));mid=next(c.p[2] for c in con if c.name=='C' and c.p[0]=='OO' and c.p[1]==g.p[0]);meshModel=models[mid];clusters={x.p[0]:x for x in objs if x.name=='Deformer' and x.p[-1]=='Cluster'};bind={};transforms=[];meshBinds=[];influence=np.zeros(len(v),int);sums=np.zeros(len(v))
 for c in con:
  if c.name=='C' and c.p[0]=='OO' and c.p[1] in models and c.p[2] in clusters:
   cl=clusters[c.p[2]];tl=child(cl,'TransformLink');tm=child(cl,'Transform');ids=child(cl,'Indexes');ww=child(cl,'Weights');name=clean(models[c.p[1]].p[1])
   if tl:bind[name]=tl.p[0].reshape(4,4).T
   if tm:
    transforms.append(tm.p[0].reshape(4,4).T)
    if tl:meshBinds.append(tl.p[0].reshape(4,4).T@transforms[-1])
   if ids is not None and ww is not None:
    use=ww.p[0]>0;np.add.at(influence,ids.p[0][use],1);np.add.at(sums,ids.p[0],ww.p[0])
 gs=props(next(x for x in ns if x.name=='GlobalSettings'));r={'source':str(path.relative_to(ROOT)),'bytes':path.stat().st_size,'sha256':hashlib.sha256(path.read_bytes()).hexdigest(),'fbxVersion':reader.v,'vertices':len(v),'triangles':tri,'allTriangles':bool(np.all(sizes==3)),'rawVerticesMin':v.min(0).tolist(),'rawVerticesMax':v.max(0).tolist(),'meshModelName':clean(meshModel.p[1]),'meshModelProperties':{k:val for k,val in props(meshModel).items() if k in ['Lcl Translation','Lcl Rotation','Lcl Scaling','PreRotation','PostRotation','RotationOrder','InheritType','GeometricTranslation','GeometricRotation','GeometricScaling']},'globalSettings':gs,'clusterBindCount':len(bind),'boneBindHeadsFileUnits':{n:mat[:3,3].tolist() for n,mat in bind.items()},'clusterRawTransformMaxDifference':float(np.max(abs(np.array(transforms)-transforms[0]))) if transforms else None,'clusterRawTransform':transforms[0].tolist() if transforms else None,'reconstructedMeshBindMaxDisagreement':float(np.max(abs(np.array(meshBinds)-meshBinds[0]))) if meshBinds else None,'reconstructedMeshBind':meshBinds[0].tolist() if meshBinds else None,'weightStats':{'maxInfluences':int(influence.max()),'unweighted':int(np.sum(influence==0)),'positiveCount':int(influence.sum()),'maxSumDeviation':float(abs(sums-1).max())} if clusters else None}
 return r,v,transforms

def main():
 start=time.monotonic();a=next((ROOT/'art/source/tripo-striped-sailor-apose-original/unpacked').glob('*.fbx'));b=next((ROOT/'art/source/tripo-striped-sailor-native-full-pack/unpacked').glob('*.fbx'));src,sv,st=audit(a);don,dv,dt=audit(b);r={'status':'RAW_SOURCE_AUDIT_NO_ENGINE_NO_CANDIDATE','original':src,'nativeDonor':don,'sameTaskId':json.loads((a.parent.parent/'provenance.json').read_text())['taskId']==json.loads((b.parent.parent/'provenance.json').read_text())['taskId'],'limits':['Raw file coordinates and cluster data only; no DCC/import/rest-surface acceptance.','Model default may be animated. Raw cluster Transform is bone-relative in Blender FBX; TransformLink @ Transform reconstructs mesh bind. Verified against installed4.2 exporter/importer source. Do not treat raw Transform alone as global mesh bind or use first animation frame.','No weights transferred, original raw maps not swapped to retopo atlas.'],'elapsedSeconds':time.monotonic()-start};(O/'offline-source-audit.json').write_text(json.dumps(r,indent=2)+'\n');print(json.dumps({'source':{k:src[k] for k in ['vertices','triangles','rawVerticesMin','rawVerticesMax','meshModelProperties']},'donor':{k:don[k] for k in ['vertices','triangles','rawVerticesMin','rawVerticesMax','meshModelProperties','clusterBindCount','weightStats','reconstructedMeshBind','reconstructedMeshBindMaxDisagreement']},'elapsed':r['elapsedSeconds']},indent=2))
if __name__=='__main__':main()

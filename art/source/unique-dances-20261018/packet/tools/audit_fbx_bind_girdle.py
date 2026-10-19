"""Read-only FBX binary bind-link anatomy; standard library, no DCC launch."""
import struct,zlib,json,math,pathlib,sys
class Node:
 def __init__(self,name,p,c):self.name,self.p,self.c=name,p,c
class FBX:
 def __init__(self,p):self.b=open(p,'rb').read();assert self.b.startswith(b'Kaydara FBX Binary');self.v=struct.unpack_from('<I',self.b,23)[0];self.o=27
 def take(self,n):p=self.b[self.o:self.o+n];self.o+=n;return p
 def prop(self):
  t=self.take(1).decode();fmt={'Y':'h','C':'?','I':'i','F':'f','D':'d','L':'q'}
  if t in fmt:return struct.unpack('<'+fmt[t],self.take(struct.calcsize(fmt[t])))[0]
  if t in ('S','R'):
   n=struct.unpack('<I',self.take(4))[0];b=self.take(n);return b.decode('utf8','replace') if t=='S' else b
  if t in 'fdilbc':
   n,enc,size=struct.unpack('<III',self.take(12));b=self.take(size);b=zlib.decompress(b) if enc else b;return struct.unpack('<'+str(n)+{'f':'f','d':'d','i':'i','l':'q','b':'?','c':'b'}[t],b)
  raise ValueError(t)
 def node(self):
  end,count,size=struct.unpack('<QQQ' if self.v>=7500 else '<III',self.take(24 if self.v>=7500 else 12));n=self.take(1)[0]
  if not end:return None
  name=self.take(n).decode();p=[self.prop() for _ in range(count)];c=[]
  while self.o<end:
   x=self.node()
   if x is None:break
   c.append(x)
  self.o=end;return Node(name,p,c)
 def all(self):
  a=[]
  while self.o<len(self.b):
   n=self.node()
   if n is None:break
   a.append(n)
  return a

def sub(a,b):return [x-y for x,y in zip(a,b)]
def dot(a,b):return sum(x*y for x,y in zip(a,b))
def elevation(a,u):return math.degrees(math.asin(max(-1,min(1,dot(a,u)/math.sqrt(dot(a,a)*dot(u,u))))))
def audit(p):
 ns=FBX(p).all();objects=next(x for x in ns if x.name=='Objects').c;cons=next(x for x in ns if x.name=='Connections').c;models={x.p[0]:x.p[1].split('\0')[0].split('::')[-1] for x in objects if x.name=='Model'};clusters={x.p[0]:x for x in objects if x.name=='Deformer' and x.p[-1]=='Cluster'};bind={}
 for c in cons:
  if c.name=='C' and len(c.p)>=3 and c.p[0]=='OO' and c.p[1] in models and c.p[2] in clusters:
   t=next((x for x in clusters[c.p[2]].c if x.name=='TransformLink'),None)
   if t:bind[models[c.p[1]]]=list(t.p[0][12:15])
 def pos(n):return bind[n]
 native='Neck' in bind;up=sub(pos('Neck' if native else 'neck'),pos('UpperChest' if native else 'Spine'));r={'source':str(p),'bindBoneCount':len(bind),'positionsSourceUnits':bind,'girdle':{}}
 for side in ['Left','Right']:
  r['girdle'][side]={'clavicleElevationDeg':elevation(sub(pos(side+'_UpperArm' if native else side+'Arm'),pos(side+'_Shoulder' if native else side+'Shoulder')),up),'upperArmElevationDeg':elevation(sub(pos(side+'_LowerArm' if native else side+'ForeArm'),pos(side+'_UpperArm' if native else side+'Arm')),up)}
 return r
if __name__=='__main__':
 r=audit(pathlib.Path(sys.argv[1]));print(json.dumps(r,indent=2))

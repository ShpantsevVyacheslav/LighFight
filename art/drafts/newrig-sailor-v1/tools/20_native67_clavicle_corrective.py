"""Append a derived dance-only clavicle-support animation to immutable source GLB.
No mesh/UV/skin/bind/image/node-default changes; original clips retained verbatim.
Requires numpy; output stays unpromoted under art/drafts. No engine/network needed.
"""
import json,struct,hashlib,copy,math,os
from pathlib import Path
import numpy as np
ROOT=Path(__file__).resolve().parents[4];O=ROOT/'art/drafts/newrig-sailor-v1/native67-trapezius-v1';O.mkdir(exist_ok=True)
SOURCE=Path(os.environ.get('DANCE_SOURCE_GLB',str(O/'source/sailor-native67-original.glb')));raw=SOURCE.read_bytes();assert hashlib.sha256(raw).hexdigest()=='3984974ec6475885ed14b99ccc6acfd04c175dd0b79d58fbcd549c44385ca29f','Refuse changed/non-original baseline';jl=struct.unpack_from('<I',raw,12)[0];g=json.loads(raw[20:20+jl]);bo=20+jl;bl,bt=struct.unpack_from('<II',raw,bo);originalBin=raw[bo+8:bo+8+bl];data=bytearray(originalBin[:g['buffers'][0]['byteLength']]);oldLength=len(data)
DTS={5126:np.dtype('<f4'),5125:np.dtype('<u4'),5123:np.dtype('<u2'),5121:np.dtype('u1')};WIDTH={'SCALAR':1,'VEC2':2,'VEC3':3,'VEC4':4,'MAT4':16}
def access(i):
 a=g['accessors'][i];v=g['bufferViews'][a['bufferView']];dt=DTS[a['componentType']];w=WIDTH[a['type']];off=v.get('byteOffset',0)+a.get('byteOffset',0);stride=v.get('byteStride',w*dt.itemsize);return np.ndarray((a['count'],w),dtype=dt,buffer=data,offset=off,strides=(stride,dt.itemsize)).copy()
def norm(q):return np.asarray(q,dtype=float)/np.linalg.norm(q)
def mul(a,b):
 x,y,z,w=a;X,Y,Z,W=b;return norm([w*X+x*W+y*Z-z*Y,w*Y-x*Z+y*W+z*X,w*Z+x*Y-y*X+z*W,w*W-x*X-y*Y-z*Z])
def inv(q):return np.array([-q[0],-q[1],-q[2],q[3]])
def qmat(q):
 x,y,z,w=norm(q);return np.array([[1-2*(y*y+z*z),2*(x*y-z*w),2*(x*z+y*w)],[2*(x*y+z*w),1-2*(x*x+z*z),2*(y*z-x*w)],[2*(x*z-y*w),2*(y*z+x*w),1-2*(x*x+y*y)]])
def slerp(a,b,t):
 a=norm(a);b=norm(b);dot=np.dot(a,b)
 if dot<0:b=-b;dot=-dot
 if dot>.9995:return norm(a+(b-a)*t)
 angle=math.acos(np.clip(dot,-1,1));return norm((math.sin((1-t)*angle)*a+math.sin(t*angle)*b)/math.sin(angle))
nodes=g['nodes'];byname={n.get('name'):i for i,n in enumerate(nodes)};parent={c:i for i,n in enumerate(nodes) for c in n.get('children',[])}
# Exported source uses TRS; refusing matrix defaults avoids silently guessing bind conventions.
assert not any('matrix' in n for n in nodes),'Source static matrix requires explicit decomposition audit.'
def defaults():return {i:{'translation':np.array(n.get('translation',[0,0,0]),float),'rotation':norm(n.get('rotation',[0,0,0,1])),'scale':np.array(n.get('scale',[1,1,1]),float)} for i,n in enumerate(nodes)}
def world(local):
 mats={};qs={}
 def visit(i):
  if i in mats:return
  p=parent.get(i)
  if p is not None:visit(p)
  n=local[i];m=np.eye(4);m[:3,:3]=qmat(n['rotation'])@np.diag(n['scale']);m[:3,3]=n['translation'];mats[i]=(mats[p]@m) if p is not None else m;qs[i]=mul(qs[p],n['rotation']) if p is not None else n['rotation']
 for i in local:visit(i)
 return mats,qs
base=g['animations'][next(i for i,a in enumerate(g['animations']) if a['name']=='dance_01')]
def channels(anim):
 result=[]
 for c in anim['channels']:
  s=anim['samplers'][c['sampler']];assert s.get('interpolation','LINEAR')=='LINEAR';result.append((c['target']['node'],c['target']['path'],access(s['input'])[:,0],access(s['output'])))
 return result
sourceChannels=channels(base)
def sample(ch,t):
 local=defaults()
 for node,path,ts,vs in ch:
  if t<=ts[0]:value=vs[0]
  elif t>=ts[-1]:value=vs[-1]
  else:
   k=np.searchsorted(ts,t,side='right')-1;u=(t-ts[k])/(ts[k+1]-ts[k]);value=slerp(vs[k],vs[k+1],u) if path=='rotation' else vs[k]*(1-u)+vs[k+1]*u
  local[node][path]=value
 return local
bind,bq=world(defaults());neck=byname['Neck'];chest=byname['UpperChest'];restUp=bind[neck][:3,3]-bind[chest][:3,3]
def angle(v,up):return math.degrees(math.asin(np.clip(np.dot(v/np.linalg.norm(v),up/np.linalg.norm(up)),-1,1)))
def smoothcap(x,M=15.,w=2.):
 if x<=0:return 0.
 if x<w:
  t=x/w;return w*t*t*(2-t)
 if x<=M-w:return x
 if x<M:
  t=(x-(M-w))/w;return M-w+w*(t+t*t-t*t*t)
 return M
def smooth(t):t=np.clip(t,0,1);return float(t*t*(3-2*t))
duration=max(float(ts[-1]) for n,p,ts,vs in sourceChannels);correctionHz=int(os.environ.get('DANCE_CORRECTION_HZ','192'));assert correctionHz>=24 and correctionHz%24==0;times=np.arange(round(duration*correctionHz)+1,dtype=float)/correctionHz;assert abs(times[-1]-duration)<1e-4
sides=[]
for side in ['Left','Right']:
 S,A,E,H=[byname[side+'_'+p] for p in ['Shoulder','UpperArm','LowerArm','Hand']];assert parent[A]==S
 sides.append((side,S,A,E,H,angle(bind[E][:3,3]-bind[A][:3,3],restUp),angle(bind[A][:3,3]-bind[S][:3,3],restUp)))
tracks={n:[] for side,S,A,E,H,ra,rc in sides for n in [S,A]};measure=[]
for t in times:
 local=sample(sourceChannels,t);wm,wq=world(local);up=wm[neck][:3,3]-wm[chest][:3,3];up/=np.linalg.norm(up);envelope=min(smooth(t/.5),smooth((duration-t)/.5));row={'seconds':float(t),'entryReturnEnvelope':envelope,'sides':[]}
 for side,S,A,E,H,ra,rc in sides:
  arm=wm[E][:3,3]-wm[A][:3,3];clav=wm[A][:3,3]-wm[S][:3,3];ae=angle(arm,up);ce=angle(clav,up);gap=max(0,ae-ra)*.25-(ce-rc);degrees=smoothcap(gap)*envelope;axis=np.cross(clav,up);axis/=np.linalg.norm(axis);dq=np.r_[axis*math.sin(math.radians(degrees)/2),math.cos(math.radians(degrees)/2)];newWorld=mul(dq,wq[S]);local[S]['rotation']=mul(inv(wq[parent[S]]),newWorld);local[A]['rotation']=mul(inv(newWorld),wq[A]);tracks[S].append(local[S]['rotation']);tracks[A].append(local[A]['rotation']);row['sides'].append({'side':side,'supportDegrees':degrees,'armElevation':ae,'clavicleElevation':ce})
 newM,newQ=world(local);row['maxHandMovementM']=max(float(np.linalg.norm(newM[H][:3,3]-wm[H][:3,3])) for side,S,A,E,H,ra,rc in sides);row['maxUpperArmOrientationErrorDeg']=max(math.degrees(2*math.acos(np.clip(abs(np.dot(newQ[A],wq[A])),0,1))) for side,S,A,E,H,ra,rc in sides);measure.append(row)
# Append only new accessor bytes. Original payload prefix and original scene objects stay exact.
def append(a,kind):
 a=np.asarray(a,dtype='<f4');a=a.reshape(-1,WIDTH[kind]);data.extend(b'\0'*((-len(data))%4));v=len(g['bufferViews']);g['bufferViews'].append({'buffer':0,'byteOffset':len(data),'byteLength':a.nbytes});data.extend(a.tobytes());i=len(g['accessors']);desc={'bufferView':v,'componentType':5126,'count':len(a),'type':kind}
 if kind=='SCALAR':desc.update(min=[float(a.min())],max=[float(a.max())])
 g['accessors'].append(desc);return i
new=copy.deepcopy(base);new['name']='Dance_TrapeziusSupport_v1';timeAccessor=append(times,'SCALAR')
for n,values in tracks.items():
 values=np.array(values)
 for k in range(1,len(values)):
  if np.dot(values[k-1],values[k])<0:values[k]*=-1
 out=append(values,'VEC4');sampler=len(new['samplers']);new['samplers'].append({'input':timeAccessor,'output':out,'interpolation':'LINEAR'});channel=next(c for c in new['channels'] if c['target']['node']==n and c['target']['path']=='rotation');channel['sampler']=sampler
new['extras']={'derivedFrom':'dance_01','kind':'authoredDanceOnlyClavicleSupport','maxAdditionalElevationDegrees':15,'armOrientationPreservedAtCurveKeys':True,'samplingHz':correctionHz,'entryReturnFadeSeconds':.5,'sourceSHA256':hashlib.sha256(raw).hexdigest()};g['animations'].append(new)
assert bytes(data[:oldLength])==originalBin[:oldLength]
assert SOURCE.read_bytes()==raw,'Original source changed duringjob'
sourceJSON=json.loads(raw[20:20+jl]);assert g['nodes']==sourceJSON['nodes'] and g['meshes']==sourceJSON['meshes'] and g['skins']==sourceJSON['skins'] and g['images']==sourceJSON['images'] and g['materials']==sourceJSON['materials'];assert g['animations'][:-1]==sourceJSON['animations']
g['buffers'][0]['byteLength']=len(data);data.extend(b'\0'*((-len(data))%4));js=json.dumps(g,separators=(',',':')).encode();js+=b' '*((-len(js))%4);payload=struct.pack('<4sII',b'glTF',2,12+8+len(js)+8+len(data))+struct.pack('<II',len(js),0x4E4F534A)+js+struct.pack('<II',len(data),0x004E4942)+data;dest=O/'sailor-native67-trapezius-candidate.glb';dest.write_bytes(payload)
manifest={'status':'UNPROMOTED_NATIVE67_CANDIDATE','sourceGLB':str(SOURCE.relative_to(ROOT)),'sourceSHA256':hashlib.sha256(raw).hexdigest(),'candidateSHA256':hashlib.sha256(payload).hexdigest(),'sourceFBXUnmodified':True,'meshUVSkinBindImagesMaterialsNodeDefaultsExact':True,'originalAnimationsJSONExact':True,'originalBINPrefixExact':True,'sourceAnimationNames':[a['name'] for a in sourceJSON['animations']],'derivedAnimation':new['name'],'changedJointRotationTracks':[nodes[i]['name'] for i in tracks],'addedPayloadBytes':len(payload)-len(raw),'sourceFrames':round(duration*24)+1,'correctionSamples':len(times),'correctionSamplingHz':correctionHz,'durationSeconds':duration,'maxHandMovementM':max(r['maxHandMovementM'] for r in measure),'maxUpperArmOrientationErrorDeg':max(r['maxUpperArmOrientationErrorDeg'] for r in measure),'boneCount':len(g['skins'][0]['joints']),'triangleCount':access(g['meshes'][0]['primitives'][0]['indices']).size//3,'limits':['Original67-bone native100k geometry retained exactly; this is not full2M newrig promotion.','Deriveddance changes shoulder jointmotion deliberately; originaldance retained and allotherclips untouched.','Need independentrender/half-frameorientation/sourceentryreturn/floorgates beforeacceptance.']};(O/'manifest.json').write_text(json.dumps(manifest,indent=2));(O/'frame-metrics.json').write_text(json.dumps(measure,indent=2));print(json.dumps(manifest,indent=2))

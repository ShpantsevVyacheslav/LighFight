from pathlib import Path
import numpy as np,json,hashlib,shutil,sys
from PIL import Image
R=Path(__file__).resolve().parents[4];S=R/'art/drafts/sailor-native67-dense-v1'/(sys.argv[1] if len(sys.argv)>1 else 'native67-zero-twist-web-packet-v2');O=R/'prototypes/roster-portraits/twist-review'/(sys.argv[2] if len(sys.argv)>2 else '');O.mkdir(parents=True,exist_ok=True);r=json.loads((S/'export.json').read_text());assert r['status']=='EXPORTED_PENDING_WEB_ROUNDTRIP';P=R/'.tools/native67-bary-weight-packet-v1';V=r['vertices'];N=994452
G={'asset':{'version':'2.0','generator':'LighFight full-positive zero-twist review'},'scene':0,'scenes':[{'nodes':[]}],'nodes':[],'meshes':[],'skins':[],'materials':[],'animations':[],'buffers':[{'uri':'sailor.bin'}],'bufferViews':[],'accessors':[]};blob=bytearray()
def acc(a,typ,ctype=5126,bounds=False,target=None):
 a=np.ascontiguousarray(a);pad=(-len(blob))%4;blob.extend(b'\0'*pad);start=len(blob);blob.extend(a.tobytes());view={'buffer':0,'byteOffset':start,'byteLength':a.nbytes};
 if target:view['target']=target
 vi=len(G['bufferViews']);G['bufferViews'].append(view);ob={'bufferView':vi,'componentType':ctype,'count':len(a),'type':typ}
 if bounds:ob.update(min=a.min(0).tolist(),max=a.max(0).tolist())
 ai=len(G['accessors']);G['accessors'].append(ob);return ai

def vec(v):return [v[k] for k in ['x','y','z']]
def qmat(q):
 x,y,z,w=q;norm=np.linalg.norm(q);x,y,z,w=np.array(q)/norm
 return np.array([[1-2*(y*y+z*z),2*(x*y-z*w),2*(x*z+y*w)],[2*(x*y+z*w),1-2*(x*x+z*z),2*(y*z-x*w)],[2*(x*z-y*w),2*(y*z+x*w),1-2*(x*x+y*y)]])
def matrix(trs):
 m=np.eye(4);m[:3,:3]=qmat(trs[3:7])*trs[7:10];m[:3,3]=trs[:3];return m
def worlds(trs):
 out=[]
 for i,v in enumerate(trs):
  m=matrix(v);parent=r['nodes'][i]['parent'];out.append(out[parent]@m if parent>=0 else m)
 return out
def decompose(m):
 scale=np.linalg.norm(m[:3,:3],axis=0);rot=m[:3,:3]/scale;assert np.linalg.det(rot)>.9999
 # Symmetric quaternion eigenproblem, stable for all rotation angles.
 a=rot;K=np.array([[a[0,0]-a[1,1]-a[2,2],a[0,1]+a[1,0],a[0,2]+a[2,0],a[2,1]-a[1,2]],[a[0,1]+a[1,0],a[1,1]-a[0,0]-a[2,2],a[1,2]+a[2,1],a[0,2]-a[2,0]],[a[0,2]+a[2,0],a[1,2]+a[2,1],a[2,2]-a[0,0]-a[1,1],a[1,0]-a[0,1]],[a[2,1]-a[1,2],a[0,2]-a[2,0],a[1,0]-a[0,1],a.trace()]])/3
 _,v=np.linalg.eigh(K);q=v[:,-1];assert np.max(abs(qmat(q)-rot))<2e-5
 return np.r_[m[:3,3],q,scale]
reflection=np.diag([1,1,-1,1]);unit=np.diag([.01,.01,.01,1]);initial=np.array([vec(n['position'])+[n['rotation'][k] for k in ['x','y','z','w']]+vec(n['scale']) for n in r['nodes']]);initialWorld=worlds(initial);meshWorld=initialWorld[r['skinNode']];jointMap={old:i for i,old in enumerate(r['joints'])};parents=[jointMap.get(r['nodes'][old]['parent'],-1) for old in r['joints']]
def convert(trs):
 source=worlds(trs);new=[reflection@source[j]@unit@reflection for j in r['joints']];local=[]
 for i,m in enumerate(new):local.append(decompose(np.linalg.inv(new[parents[i]])@m if parents[i]>=0 else m))
 return np.array(local)
init=convert(initial);G['nodes']=[{'name':'ReviewRoot','children':[1]+[i+2 for i,p in enumerate(parents) if p<0]},{'name':'FullDenseMesh','mesh':0,'skin':0}];G['scenes'][0]['nodes']=[0]
for i,v in enumerate(init):
 node={'name':r['nodes'][r['joints'][i]]['name'],'translation':v[:3].tolist(),'rotation':v[3:7].tolist(),'scale':v[7:10].tolist()};children=[j+2 for j,p in enumerate(parents) if p==i]
 if children:node['children']=children
 G['nodes'].append(node)
p=np.fromfile(S/'positions.bin','<f4').reshape(V,3);p=(p@meshWorld[:3,:3].T+meshWorld[:3,3]).astype('<f4');p[:,2]*=-1;n=np.fromfile(S/'normals.bin','<f4').reshape(V,3);n=n@np.linalg.inv(meshWorld[:3,:3]);n/=np.linalg.norm(n,axis=1)[:,None];n=n.astype('<f4');n[:,2]*=-1;t=np.fromfile(S/'tangents.bin','<f4').reshape(V,4);t[:,:3]=t[:,:3]@meshWorld[:3,:3].T;t[:,:3]/=np.linalg.norm(t[:,:3],axis=1)[:,None];t[:,2]*=-1;t[:,3]*=-1;uv=np.fromfile(S/'uv.bin','<f4').reshape(V,2);uv[:,1]=1-uv[:,1];tri=np.fromfile(S/'triangles.bin','<u4').reshape(-1,3);tri=tri[:,[0,2,1]].copy();ids=np.fromfile(S/'originalIds.bin','<i4');assert len(np.unique(ids))==N and tri.shape[0]==1988914
counts=np.fromfile(P/'counts.bin','u1');offset=np.r_[0,np.cumsum(counts,dtype=np.int64)];ix=np.fromfile(P/'indices.bin','<i4');ww=np.fromfile(P/'weights.bin','<f4');assert counts.max()<=8;sourceNames=(P/'bones.txt').read_text().splitlines();skinNames=(S/'boneNames.txt').read_text().splitlines();mapping=np.array([skinNames.index(n) for n in sourceNames]);joints=np.zeros((V,8),'<u2');weights=np.zeros((V,8),'<f4')
for k in range(8):
 active=counts[ids]>k;at=offset[ids[active]]+k;joints[active,k]=mapping[ix[at]];weights[active,k]=ww[at]
assert np.count_nonzero(weights)==int(counts[ids].sum());assert np.max(abs(weights.sum(1)-1))<3e-7
attrs={'POSITION':acc(p,'VEC3',bounds=True,target=34962),'NORMAL':acc(n,'VEC3',target=34962),'TANGENT':acc(t,'VEC4',target=34962),'TEXCOORD_0':acc(uv,'VEC2',target=34962),'JOINTS_0':acc(joints[:,:4],'VEC4',5123,target=34962),'JOINTS_1':acc(joints[:,4:],'VEC4',5123,target=34962),'WEIGHTS_0':acc(weights[:,:4],'VEC4',target=34962),'WEIGHTS_1':acc(weights[:,4:],'VEC4',target=34962)}
G['meshes']=[{'name':'Sailor_original_dense','primitives':[{'attributes':attrs,'indices':acc(tri.reshape(-1),'SCALAR',5125,target=34963),'material':0}]}];G['nodes'][1].update(mesh=0,skin=0)
ibm=np.fromfile(S/'bindposes.bin','<f4').reshape(67,4,4).transpose(0,2,1);reflection=np.diag([1,1,-1,1]);ibm=(reflection@np.linalg.inv(unit)@ibm@np.linalg.inv(meshWorld)@reflection).astype('<f4');G['skins']=[{'joints':list(range(2,69)),'skeleton':0,'inverseBindMatrices':acc(ibm.transpose(0,2,1).reshape(67,16),'MAT4')}]
for clip in r['clips']:
 frames=clip['frames'];raw=np.fromfile(S/clip['file'],'<f4').reshape(frames,len(r['nodes']),10);raw=np.array([convert(frame) for frame in raw],dtype='<f4');time=np.linspace(0,clip['duration'],frames,dtype='<f4');anim={'name':clip['name'],'samplers':[],'channels':[]};timeId=acc(time,'SCALAR',bounds=True)
 for i in range(67):
  for path,sl,typ in [('translation',slice(0,3),'VEC3'),('rotation',slice(3,7),'VEC4'),('scale',slice(7,10),'VEC3')]:
   values=raw[:,i,sl].copy()
   if path=='translation':pass
   if path=='rotation':
    values/=np.linalg.norm(values,axis=1)[:,None]
    for k in range(1,len(values)):
     if np.dot(values[k],values[k-1])<0:values[k]*=-1
   output=acc(values,typ);sid=len(anim['samplers']);anim['samplers'].append({'input':timeId,'output':output,'interpolation':'LINEAR'});anim['channels'].append({'sampler':sid,'target':{'node':i+2,'path':path}})
 G['animations'].append(anim)
M=R/'art/drafts/sailor-native67-dense-v1/native67-interchange-v2/original-maps';shutil.copy2(M/'original-0.jpg',O/'base.jpg');shutil.copy2(M/'original-1.png',O/'normal.png');rough=np.array(Image.open(M/'original-2.jpg').convert('L'));metal=np.array(Image.open(M/'original-3.jpg').convert('L'));Image.fromarray(np.stack([np.full_like(rough,255),rough,metal],-1)).save(O/'metalrough.png')
G['images']=[{'uri':x} for x in ['base.jpg','normal.png','metalrough.png']];G['textures']=[{'source':i} for i in range(3)];G['materials']=[{'name':'Raw4K_derivedPBR','pbrMetallicRoughness':{'baseColorTexture':{'index':0},'metallicRoughnessTexture':{'index':2},'metallicFactor':1,'roughnessFactor':1},'normalTexture':{'index':1},'doubleSided':True}]
G['buffers'][0]['byteLength']=len(blob);(O/'sailor.bin').write_bytes(blob);(O/'sailor.gltf').write_text(json.dumps(G,separators=(',',':')))
report={'status':'BUILT_PENDING_BROWSER_CHECK','vertices':V,'originalVertices':N,'triangles':len(tri),'maxInfluences':int(counts.max()),'positiveSeamCoefficients':int(np.count_nonzero(weights)),'zeroSourceCoefficientsDropped':True,'sourcePacket':str(S),'animationHz':96,'sourceGuardScope':r['guardScope'],'binaryBytes':len(blob),'binarySHA256':hashlib.sha256(blob).hexdigest(),'limits':'Raw PNT/geometry and weights preserved under coordinate conversion, all<=8 influences exported as two sets Float32. Metallic/roughness repacked losslessly from decoded JPEG channels. Browser PBR differs from Unity; normal-map handedness requires visual review. No mobile optimization or phone-performance acceptance.'};(O/'manifest.json').write_text(json.dumps(report,indent=2));print(report)

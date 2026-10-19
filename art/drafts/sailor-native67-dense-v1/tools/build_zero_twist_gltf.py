from pathlib import Path
import numpy as np,json,hashlib,shutil
from PIL import Image
R=Path(__file__).resolve().parents[4];S=R/'art/drafts/sailor-native67-dense-v1/native67-zero-twist-web-packet-v2';O=R/'prototypes/roster-portraits/twist-review';r=json.loads((S/'export.json').read_text());assert r['status']=='EXPORTED_PENDING_WEB_ROUNDTRIP';P=R/'.tools/native67-bary-weight-packet-v1';V=r['vertices'];N=994452
G={'asset':{'version':'2.0','generator':'LighFight full-positive zero-twist review'},'scene':0,'scenes':[{'nodes':[]}],'nodes':[],'meshes':[],'skins':[],'materials':[],'animations':[],'buffers':[{'uri':'sailor.bin'}],'bufferViews':[],'accessors':[]};blob=bytearray()
def acc(a,typ,ctype=5126,bounds=False,target=None):
 a=np.ascontiguousarray(a);pad=(-len(blob))%4;blob.extend(b'\0'*pad);start=len(blob);blob.extend(a.tobytes());view={'buffer':0,'byteOffset':start,'byteLength':a.nbytes};
 if target:view['target']=target
 vi=len(G['bufferViews']);G['bufferViews'].append(view);ob={'bufferView':vi,'componentType':ctype,'count':len(a),'type':typ}
 if bounds:ob.update(min=a.min(0).tolist(),max=a.max(0).tolist())
 ai=len(G['accessors']);G['accessors'].append(ob);return ai

def vec(v):return [v[k] for k in ['x','y','z']]
for n in r['nodes']:
 p=vec(n['position']);p[2]*=-1;q=n['rotation'];node={'name':n['name'],'translation':p,'rotation':[-q['x'],-q['y'],q['z'],q['w']],'scale':vec(n['scale'])};G['nodes'].append(node)
for i,n in enumerate(r['nodes']):
 if n['parent']>=0:G['nodes'][n['parent']].setdefault('children',[]).append(i)
 else:G['scenes'][0]['nodes'].append(i)
p=np.fromfile(S/'positions.bin','<f4').reshape(V,3);p[:,2]*=-1;n=np.fromfile(S/'normals.bin','<f4').reshape(V,3);n[:,2]*=-1;t=np.fromfile(S/'tangents.bin','<f4').reshape(V,4);t[:,2]*=-1;t[:,3]*=-1;uv=np.fromfile(S/'uv.bin','<f4').reshape(V,2);uv[:,1]=1-uv[:,1];tri=np.fromfile(S/'triangles.bin','<u4').reshape(-1,3);tri=tri[:,[0,2,1]].copy();ids=np.fromfile(S/'originalIds.bin','<i4');assert len(np.unique(ids))==N and tri.shape[0]==1988914
counts=np.fromfile(P/'counts.bin','u1');offset=np.r_[0,np.cumsum(counts,dtype=np.int64)];ix=np.fromfile(P/'indices.bin','<i4');ww=np.fromfile(P/'weights.bin','<f4');assert counts.max()<=8;sourceNames=(P/'bones.txt').read_text().splitlines();skinNames=(S/'boneNames.txt').read_text().splitlines();mapping=np.array([skinNames.index(n) for n in sourceNames]);joints=np.zeros((V,8),'<u2');weights=np.zeros((V,8),'<f4')
for k in range(8):
 active=counts[ids]>k;at=offset[ids[active]]+k;joints[active,k]=mapping[ix[at]];weights[active,k]=ww[at]
assert np.count_nonzero(weights)==int(counts[ids].sum());assert np.max(abs(weights.sum(1)-1))<3e-7
attrs={'POSITION':acc(p,'VEC3',bounds=True,target=34962),'NORMAL':acc(n,'VEC3',target=34962),'TANGENT':acc(t,'VEC4',target=34962),'TEXCOORD_0':acc(uv,'VEC2',target=34962),'JOINTS_0':acc(joints[:,:4],'VEC4',5123,target=34962),'JOINTS_1':acc(joints[:,4:],'VEC4',5123,target=34962),'WEIGHTS_0':acc(weights[:,:4],'VEC4',target=34962),'WEIGHTS_1':acc(weights[:,4:],'VEC4',target=34962)}
G['meshes']=[{'name':'Sailor_original_dense','primitives':[{'attributes':attrs,'indices':acc(tri.reshape(-1),'SCALAR',5125,target=34963),'material':0}]}];G['nodes'][r['skinNode']].update(mesh=0,skin=0)
ibm=np.fromfile(S/'bindposes.bin','<f4').reshape(67,4,4).transpose(0,2,1);reflection=np.diag([1,1,-1,1]);ibm=(reflection@ibm@reflection).astype('<f4');G['skins']=[{'joints':r['joints'],'inverseBindMatrices':acc(ibm.transpose(0,2,1).reshape(67,16),'MAT4')}]
for clip in r['clips']:
 frames=clip['frames'];raw=np.fromfile(S/clip['file'],'<f4').reshape(frames,len(r['nodes']),10);time=np.linspace(0,clip['duration'],frames,dtype='<f4');anim={'name':clip['name'],'samplers':[],'channels':[]};timeId=acc(time,'SCALAR',bounds=True)
 for i in range(len(r['nodes'])):
  for path,sl,typ in [('translation',slice(0,3),'VEC3'),('rotation',slice(3,7),'VEC4'),('scale',slice(7,10),'VEC3')]:
   values=raw[:,i,sl].copy()
   if path=='translation':values[:,2]*=-1
   if path=='rotation':
    values[:,:2]*=-1;values/=np.linalg.norm(values,axis=1)[:,None]
    for k in range(1,len(values)):
     if np.dot(values[k],values[k-1])<0:values[k]*=-1
   output=acc(values,typ);sid=len(anim['samplers']);anim['samplers'].append({'input':timeId,'output':output,'interpolation':'LINEAR'});anim['channels'].append({'sampler':sid,'target':{'node':i,'path':path}})
 G['animations'].append(anim)
M=R/'art/drafts/sailor-native67-dense-v1/native67-interchange-v2/original-maps';shutil.copy2(M/'original-0.jpg',O/'base.jpg');shutil.copy2(M/'original-1.png',O/'normal.png');rough=np.array(Image.open(M/'original-2.jpg').convert('L'));metal=np.array(Image.open(M/'original-3.jpg').convert('L'));Image.fromarray(np.stack([np.full_like(rough,255),rough,metal],-1)).save(O/'metalrough.png')
G['images']=[{'uri':x} for x in ['base.jpg','normal.png','metalrough.png']];G['textures']=[{'source':i} for i in range(3)];G['materials']=[{'name':'Raw4K_derivedPBR','pbrMetallicRoughness':{'baseColorTexture':{'index':0},'metallicRoughnessTexture':{'index':2},'metallicFactor':1,'roughnessFactor':1},'normalTexture':{'index':1},'doubleSided':False}]
G['buffers'][0]['byteLength']=len(blob);(O/'sailor.bin').write_bytes(blob);(O/'sailor.gltf').write_text(json.dumps(G,separators=(',',':')))
report={'status':'BUILT_PENDING_BROWSER_CHECK','vertices':V,'originalVertices':N,'triangles':len(tri),'maxInfluences':int(counts.max()),'positiveSeamCoefficients':int(np.count_nonzero(weights)),'zeroSourceCoefficientsDropped':True,'sourcePacket':str(S),'animationHz':96,'sourceGuardScope':r['guardScope'],'binaryBytes':len(blob),'binarySHA256':hashlib.sha256(blob).hexdigest(),'limits':'Raw PNT/geometry and weights preserved under coordinate conversion, all<=8 influences exported as two sets Float32. Metallic/roughness repacked losslessly from decoded JPEG channels. Browser PBR differs from Unity; normal-map handedness requires visual review. No mobile optimization or phone-performance acceptance.'};(O/'manifest.json').write_text(json.dumps(report,indent=2));print(report)

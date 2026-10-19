"""Independent exact-source-buffer/material binding and derivative glTF validity gate."""
import json,hashlib,sys
from pathlib import Path
import numpy as np
from glb_static import load,audit_images
ROOT=Path(__file__).resolve().parents[4];OUT=ROOT/'art/drafts/kitchen-env-lod-v1';SRC=ROOT/'art/source/kitchen-tripo-v1';SPECS={'table':'original-v2.glb','fridge':'fridge-microwave-original-v4.glb','coffee':'coffee-original-v5.glb'}
def viewbytes(g,b,a):
 a=g['accessors'][a];v=g['bufferViews'][a['bufferView']];start=v.get('byteOffset',0);return b[start:start+v['byteLength']]
def values(g,b,i):
 a=g['accessors'][i];v=g['bufferViews'][a['bufferView']];dt={5126:'<f4',5125:'<u4',5123:'<u2',5121:'u1'}[a['componentType']];w={'SCALAR':1,'VEC2':2,'VEC3':3,'VEC4':4}[a['type']];d=np.dtype(dt);return np.ndarray((a['count'],w),dtype=d,buffer=b,offset=v.get('byteOffset',0)+a.get('byteOffset',0),strides=(v.get('byteStride',w*d.itemsize),d.itemsize))
def main():
 report={'status':'PASS','scope':'Static environment only; exact source master and all source image bytes, derivative buffer validity. No visual acceptance inferred.'};master,b=load(OUT/'kitchen-master.glb');roles={n['name']:n for n in master['nodes'] if n.get('name','').startswith('ROLE_')};assert len(roles)==3;sourceImages={}
 for role,path in SPECS.items():
  sg,sb=load(SRC/path);root=roles['ROLE_'+role.upper()];child=master['nodes'][root['children'][0]];mp=master['meshes'][child['mesh']]['primitives'][0];sp=sg['meshes'][0]['primitives'][0];assert mp['attributes'].keys()==sp['attributes'].keys();checks={}
  for key,srcIndex in list(sp['attributes'].items())+[('INDICES',sp['indices'])]:
   dstIndex=mp['indices'] if key=='INDICES' else mp['attributes'][key];assert viewbytes(master,b,dstIndex)==viewbytes(sg,sb,srcIndex),(role,key);checks[key]=True
  material=master['materials'][mp['material']];sm=sg['materials'][0]
  for kind,info in [('baseColorTexture',material['pbrMetallicRoughness']['baseColorTexture']),('metallicRoughnessTexture',material['pbrMetallicRoughness']['metallicRoughnessTexture']),('normalTexture',material['normalTexture'])]:
   mi=master['textures'][info['index']]['source'];sinfo=sm[kind] if kind=='normalTexture' else sm['pbrMetallicRoughness'][kind];si=sg['textures'][sinfo['index']]['source'];assert master['images'][mi]['name']==sg['images'][si]['name'],(role,kind)
  report[role]={'allSourceAttributeAndIndexBufferBytesExact':checks,'PBRTextureBindingsExact':True,'wrapper':{k:v for k,v in root.items() if k!='children'}};sourceImages.update({im['name']:im['SHA256'] for im in audit_images(SRC/path)})
 report['exports']={}
 for name in (['kitchen-master.glb'] if '--master-only' in sys.argv else ['kitchen-master.glb','kitchen-lod-300000.glb','kitchen-lod-100000.glb']):
  g,raw=load(OUT/name);assert not g.get('skins') and not g.get('animations');images={im['name']:im['SHA256'] for im in audit_images(OUT/name)};assert all(images.get(k)==v for k,v in sourceImages.items());total=0;stats=[]
  for mesh in g['meshes']:
   for prim in mesh['primitives']:
    p=values(g,raw,prim['attributes']['POSITION']);uv=values(g,raw,prim['attributes']['TEXCOORD_0']);normal=values(g,raw,prim['attributes']['NORMAL']);indices=values(g,raw,prim['indices']).ravel();assert np.isfinite(p).all() and np.isfinite(uv).all() and np.isfinite(normal).all();assert indices.max()<len(p);length=np.linalg.norm(normal,axis=1);assert length.min()>.99 and length.max()<1.01;(count:=len(indices)//3);total+=count;stats.append({'triangles':count,'positions':len(p),'UVmin':float(uv.min()),'UVmax':float(uv.max()),'normalLengthMinMax':[float(length.min()),float(length.max())]})
  report['exports'][name]={'all9Original4KImageBytesExact':True,'trianglesIncludingArchitecture':total,'bytes':(OUT/name).stat().st_size,'SHA256':hashlib.sha256((OUT/name).read_bytes()).hexdigest(),'meshes':stats}
 (OUT/('portable-GLB-master-proof.json' if '--master-only' in sys.argv else 'portable-GLB-proof.json')).write_text(json.dumps(report,indent=2));print(json.dumps({'status':'PASS','exports':{k:v['trianglesIncludingArchitecture'] for k,v in report['exports'].items()}},indent=2))
main()

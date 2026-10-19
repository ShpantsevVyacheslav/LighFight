"""Derivative gameplay shell, optionally with separate furniture LOD; immutable source masters."""
import sys,copy,struct,json,hashlib,argparse
from pathlib import Path
import numpy as np
ROOT=Path(__file__).resolve().parents[4];OUT=ROOT/'art/drafts/meeting-meshy-v1';sys.path.insert(0,str(ROOT/'art/drafts/kitchen-env-lod-v1/tools'));from glb_static import load,write,audit_images,combine
ap=argparse.ArgumentParser();ap.add_argument('--furniture',default='art/source/meeting-tripo-v1/original-v1.glb');args=ap.parse_args();src=ROOT/'art/drafts/meeting-hybrid-v1/meeting-architecture.glb';g,raw=load(src);g=copy.deepcopy(g);data=bytearray(raw);seen=set()
for node in g['nodes']:
 if 'mesh' not in node:continue
 if node['name']=='Light oak':node['name']='FightFloor'
 for p in g['meshes'][node['mesh']]['primitives']:
  for semantic in ['POSITION','NORMAL']:
   ai=p['attributes'][semantic]
   if ai in seen:continue
   seen.add(ai);a=g['accessors'][ai];v=g['bufferViews'][a['bufferView']];assert a['componentType']==5126 and a['type']=='VEC3' and v.get('byteStride',12)==12
   arr=np.ndarray((a['count'],3),dtype='<f4',buffer=data,offset=v.get('byteOffset',0)+a.get('byteOffset',0));arr[:,0]*=1.9 if semantic=='POSITION' else 1/1.9;arr[:,2]*=1.35 if semantic=='POSITION' else 1/1.35
   if semantic=='NORMAL':arr[:]/=np.linalg.norm(arr,axis=1)[:,None]
   else:a.update(min=arr.min(axis=0).tolist(),max=arr.max(axis=0).tolist())
arch=OUT/'gameplay-architecture.glb';write(arch,g,data);assert audit_images(src)==audit_images(arch)
furniture=ROOT/args.furniture;scale=1.4/.3764801025390625;wrapper={'name':'Furniture uniform scale unchanged from review, moved behind combat','scale':[scale]*3,'translation':[0,0,4.8]};dst=OUT/'gameplay-shell.glb';sha=combine([(furniture,wrapper),(arch,{'name':'Widened authored gameplay shell'})],dst);assembled,_=load(dst);tris=sum(assembled['accessors'][p['indices']]['count']//3 for m in assembled['meshes'] for p in m['primitives'])
(OUT/'gameplay-shell.json').write_text(json.dumps({'sourceArchitecture':str(src.relative_to(ROOT)),'sourceArchitectureSHA256':hashlib.sha256(src.read_bytes()).hexdigest(),'furniture':str(furniture.relative_to(ROOT)),'furnitureSHA256':hashlib.sha256(furniture.read_bytes()).hexdigest(),'SHA256':sha,'triangles':tris,'furnitureScaleUnchanged':True,'furnitureTranslationZ':1.8,'shellScaleXZ':[1.9,1.35],'sourceArchitectureImagesExact':True,'status':'PENDING_COMPONENT_BOUNDS_SPLIT_IN_FBX_EXPORT'},indent=2));print('shelltris',tris,sha)

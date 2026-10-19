"""Read exact glTF PBR semantics, not Blender's linked BSDF default socket values.
Source image files copied byte-exact; separate RM->Unity metallic/smoothness derivative explicit.
"""
import sys,json,io,hashlib
from pathlib import Path
import numpy as np
from PIL import Image
ROOT=Path(__file__).resolve().parents[4];sys.path.insert(0,str(ROOT/'art/drafts/kitchen-env-lod-v1/tools'));from glb_static import load
source=Path(sys.argv[1]);out=Path(sys.argv[2]);out.mkdir(parents=True,exist_ok=True);g,b=load(source);rows=[]
def image_payload(tex):
 im=g['images'][g['textures'][tex]['source']];v=g['bufferViews'][im['bufferView']];return b[v.get('byteOffset',0):v.get('byteOffset',0)+v['byteLength']],im.get('mimeType','image/png')
for i,mat in enumerate(g['materials']):
 p=mat.get('pbrMetallicRoughness',{});row={'name':mat.get('name','Material_'+str(i)),'color':p.get('baseColorFactor',[1,1,1,1]),'metallic':p.get('metallicFactor',1),'roughness':p.get('roughnessFactor',1),'baseTexture':'','normalTexture':'','metallicGlossTexture':'','normalScale':mat.get('normalTexture',{}).get('scale',1),'doubleSided':mat.get('doubleSided',False)}
 for key,target in [('baseColorTexture','baseTexture'),('normalTexture','normalTexture')]:
  slot=mat.get(key) if key=='normalTexture' else p.get(key)
  if not slot:continue
  payload,mime=image_payload(slot['index']);name=f'original-{i}-{target}'+('.jpg' if mime=='image/jpeg' else '.png');(out/name).write_bytes(payload);row[target]=name
 if p.get('metallicRoughnessTexture'):
  payload,_=image_payload(p['metallicRoughnessTexture']['index']);im=np.asarray(Image.open(io.BytesIO(payload)).convert('RGB'),dtype=np.float32)/255;packed=np.zeros((*im.shape[:2],4),dtype=np.uint8);packed[:,:,0]=np.rint(np.clip(im[:,:,2]*row['metallic'],0,1)*255);packed[:,:,3]=np.rint((1-np.clip(im[:,:,1]*row['roughness'],0,1))*255);name=f'derived-{i}-metallic-smoothness.png';Image.fromarray(packed,'RGBA').save(out/name);row['metallicGlossTexture']=name
 rows.append(row)
(out/'materials.json').write_text(json.dumps({'materials':rows,'sourceSHA256':hashlib.sha256(source.read_bytes()).hexdigest(),'textureScope':'Environment only; source maps copied byte-exact, RM channel repack explicit, Unity import2K/ASTC6x6 derivative.'},indent=2));print('materials',len(rows))

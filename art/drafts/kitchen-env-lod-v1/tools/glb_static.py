"""Lossless static GLB assembly: copy binary slices, remap references, no image codecs."""
import json,struct,copy,hashlib
from pathlib import Path
def load(path):
 b=Path(path).read_bytes();n=struct.unpack_from('<I',b,12)[0];g=json.loads(b[20:20+n]);off=20+n;size=struct.unpack_from('<I',b,off)[0];data=b[off+8:off+8+size];assert len(g.get('buffers',[]))==1;return g,data[:g['buffers'][0]['byteLength']]
def write(path,g,data):
 g=copy.deepcopy(g);g['buffers']=[{'byteLength':len(data)}];js=json.dumps(g,separators=(',',':')).encode();js+=b' '*((-len(js))%4);binary=bytes(data)+b'\0'*((-len(data))%4);out=struct.pack('<4sII',b'glTF',2,12+8+len(js)+8+len(binary))+struct.pack('<II',len(js),0x4e4f534a)+js+struct.pack('<II',len(binary),0x004e4942)+binary;Path(path).write_bytes(out);return hashlib.sha256(out).hexdigest()
def attach_original_materials(geometry,source,dest):
 g,data=load(geometry);s,raw=load(source);g=copy.deepcopy(g);data=bytearray(data);assert not g.get('skins') and not g.get('animations');g['materials']=copy.deepcopy(s['materials']);g['textures']=copy.deepcopy(s.get('textures',[]));g['samplers']=copy.deepcopy(s.get('samplers',[]));g['images']=[]
 for im in s['images']:
  view=s['bufferViews'][im['bufferView']];start=view.get('byteOffset',0);payload=raw[start:start+view['byteLength']];data.extend(b'\0'*((-len(data))%4));v=len(g['bufferViews']);g['bufferViews'].append({'buffer':0,'byteOffset':len(data),'byteLength':len(payload)});data.extend(payload);image=copy.deepcopy(im);image['bufferView']=v;g['images'].append(image)
 for mesh in g['meshes']:
  for p in mesh['primitives']:p['material']=0
 for key in ['extensionsUsed','extensionsRequired']:
  g[key]=sorted(set(g.get(key,[])+s.get(key,[])))
 return write(dest,g,data)
def combine(parts,dest):
 keys=['bufferViews','accessors','images','samplers','textures','materials','meshes','nodes'];out={k:[] for k in keys};data=bytearray();roots=[];extensions=set();required=set()
 def material_offset(value,offset,key=''):
  if isinstance(value,dict):return {k:(v+offset if k=='index' and key.endswith('Texture') else material_offset(v,offset,k)) for k,v in value.items()}
  if isinstance(value,list):return [material_offset(v,offset,key) for v in value]
  return value
 for path,wrapper in parts:
  g,raw=load(path);assert not g.get('skins') and not g.get('animations') and not g.get('cameras'),'Static environment only';base={k:len(out[k]) for k in keys};data.extend(b'\0'*((-len(data))%4));binaryOffset=len(data);data.extend(raw)
  for v in g.get('bufferViews',[]):
   v=copy.deepcopy(v);assert v.get('buffer',0)==0;v['buffer']=0;v['byteOffset']=v.get('byteOffset',0)+binaryOffset;out['bufferViews'].append(v)
  for a in g.get('accessors',[]):
   a=copy.deepcopy(a);assert 'sparse' not in a;a['bufferView']+=base['bufferViews'];out['accessors'].append(a)
  for im in g.get('images',[]):
   im=copy.deepcopy(im);assert 'bufferView' in im;im['bufferView']+=base['bufferViews'];out['images'].append(im)
  out['samplers'].extend(copy.deepcopy(g.get('samplers',[])))
  for tex in g.get('textures',[]):
   tex=copy.deepcopy(tex);assert not tex.get('extensions');tex['source']+=base['images']
   if 'sampler' in tex:tex['sampler']+=base['samplers']
   out['textures'].append(tex)
  out['materials'].extend(material_offset(v,base['textures']) for v in g.get('materials',[]))
  for mesh in g.get('meshes',[]):
   mesh=copy.deepcopy(mesh)
   for p in mesh['primitives']:
    assert not p.get('targets');p['attributes']={k:v+base['accessors'] for k,v in p['attributes'].items()};p['indices']+=base['accessors']
    if 'material' in p:p['material']+=base['materials']
   out['meshes'].append(mesh)
  for node in g.get('nodes',[]):
   node=copy.deepcopy(node);assert 'skin' not in node and 'camera' not in node
   if 'mesh' in node:node['mesh']+=base['meshes']
   if 'children' in node:node['children']=[v+base['nodes'] for v in node['children']]
   out['nodes'].append(node)
  children=[v+base['nodes'] for v in g['scenes'][g.get('scene',0)]['nodes']]
  if wrapper:
   node=copy.deepcopy(wrapper);node['children']=children;roots.append(len(out['nodes']));out['nodes'].append(node)
  else:roots.extend(children)
  extensions.update(g.get('extensionsUsed',[]));required.update(g.get('extensionsRequired',[]))
 out.update(asset={'version':'2.0','generator':'LighFight static environment lossless component assembler'},scenes=[{'nodes':roots}],scene=0)
 if extensions:out['extensionsUsed']=sorted(extensions)
 if required:out['extensionsRequired']=sorted(required)
 return write(dest,out,data)
def audit_images(path):
 g,b=load(path);result=[]
 for im in g.get('images',[]):
  v=g['bufferViews'][im['bufferView']];start=v.get('byteOffset',0);result.append({'name':im.get('name'),'SHA256':hashlib.sha256(b[start:start+v['byteLength']]).hexdigest(),'bytes':v['byteLength']})
 return result

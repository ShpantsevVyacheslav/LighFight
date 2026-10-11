"""Combine Meshy web rig and motion-only GLBs without retargeting or mesh edits."""
import json,struct,copy
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2]
def read(path):
 b=Path(path).read_bytes();assert b[:4]==b'glTF';p=12;doc=None;binary=b''
 while p<len(b):
  size,kind=struct.unpack_from('<II',b,p);chunk=b[p+8:p+8+size];p+=8+size
  if kind==0x4e4f534a:doc=json.loads(chunk)
  elif kind==0x004e4942:binary=chunk
 return doc,binary

def combine(rig,motions,target):
 doc,blob=read(rig);doc=copy.deepcopy(doc);blob=bytearray(blob);names={n['name']:i for i,n in enumerate(doc['nodes']) if 'name'in n};assert len(names)==len(doc['nodes']), 'Duplicate or unnamed rig nodes';doc.setdefault('animations',[])
 for name,path in motions:
  motion,binary=read(path);mapping={i:names[n['name']] for i,n in enumerate(motion['nodes']) if 'name'in n}
  for i,j in mapping.items():
   a,b=motion['nodes'][i],doc['nodes'][j]
   for prop in ('translation','rotation','scale','matrix'):
    assert a.get(prop)==b.get(prop), 'Rest transform mismatch: '+a.get('name','')
   assert {mapping[c] for c in a.get('children',[])}==set(b.get('children',[])), 'Hierarchy mismatch'
  blob.extend(b'\0'*((-len(blob))%4));offset=len(blob);viewoffset=len(doc.setdefault('bufferViews',[]));accessoffset=len(doc.setdefault('accessors',[]))
  for view in motion.get('bufferViews',[]):
   view=copy.deepcopy(view);assert view.get('buffer',0)==0;view['buffer']=0;view['byteOffset']=view.get('byteOffset',0)+offset;doc['bufferViews'].append(view)
  for accessor in motion.get('accessors',[]):
   accessor=copy.deepcopy(accessor)
   if 'bufferView'in accessor:accessor['bufferView']+=viewoffset
   assert 'sparse'not in accessor,'Sparse accessors require explicit handling';doc['accessors'].append(accessor)
  for animation in motion.get('animations',[]):
   animation=copy.deepcopy(animation);animation['name']=name
   for sampler in animation['samplers']:sampler['input']+=accessoffset;sampler['output']+=accessoffset
   for channel in animation['channels']:channel['target']['node']=mapping[channel['target']['node']]
   doc['animations'].append(animation)
  blob.extend(binary)
 doc['buffers']=[{'byteLength':len(blob)}];encoded=json.dumps(doc,separators=(',',':')).encode();encoded+=b' '*((-len(encoded))%4);blob+=b'\0'*((-len(blob))%4)
 Path(target).write_bytes(struct.pack('<4sII',b'glTF',2,12+8+len(encoded)+8+len(blob))+struct.pack('<II',len(encoded),0x4e4f534a)+encoded+struct.pack('<II',len(blob),0x004e4942)+blob)
 return {'jointCount':len(doc['skins'][0]['joints']),'clips':[a['name'] for a in doc['animations']],'meshModified':False,'weightsModified':False,'retargeted':False}
if __name__=='__main__':
 folder=ROOT/'art/source/original-island';report=combine(folder/'web-rig.glb',[('WebGuard',folder/'web-guard.glb'),('WebRun',folder/'web-running.glb')],folder/'web-combined.glb');(folder/'web-combined-audit.json').write_text(json.dumps(report,indent=2));print(report)

"""Check direct native motion GLB against original web-rig mesh payloads.
Usage: python3 this.py rig.glb motion.glb report.json
This deliberately checks both sets of skin influences, not just triangle counts.
"""
import hashlib,json,sys
from pathlib import Path
from combine_meshy_web_glb import read

def view(doc,blob,index):
 item=doc['bufferViews'][index];start=item.get('byteOffset',0);return blob[start:start+item['byteLength']]
def audit(source,motion):
 a,ab=read(source);b,bb=read(motion);results={}
 assert len(a['meshes'])==len(b['meshes']),'Mesh inventory differs'
 for mi,(left,right) in enumerate(zip(a['meshes'],b['meshes'])):
  assert len(left['primitives'])==len(right['primitives']),'Primitive inventory differs'
  for pi,(x,y) in enumerate(zip(left['primitives'],right['primitives'])):
   assert x['attributes'].keys()==y['attributes'].keys(),'Attribute inventory differs'
   for name in list(x['attributes'])+['indices']:
    ai=x['indices'] if name=='indices' else x['attributes'][name];bi=y['indices'] if name=='indices' else y['attributes'][name]
    ax=a['accessors'][ai];bx=b['accessors'][bi];same=all(ax.get(k)==bx.get(k) for k in ['count','type','componentType','normalized','byteOffset'])
    same=same and view(a,ab,ax['bufferView'])==view(b,bb,bx['bufferView']);results[f'{mi}/{pi}/{name}']=same
 assert len(a.get('images',[]))==len(b.get('images',[])),'Texture inventory differs'
 for i,(x,y) in enumerate(zip(a.get('images',[]),b.get('images',[]))):results[f'image/{i}']=view(a,ab,x['bufferView'])==view(b,bb,y['bufferView'])
 report={'sourceSHA256':hashlib.sha256(Path(source).read_bytes()).hexdigest(),'motionSHA256':hashlib.sha256(Path(motion).read_bytes()).hexdigest(),'clips':[x.get('name') for x in b.get('animations',[])],'sourceJoints':[len(x['joints']) for x in a['skins']],'motionJoints':[len(x['joints']) for x in b['skins']],'identicalPayloads':results,'passed':all(results.values())}
 return report
if __name__=='__main__':
 source,motion,out=map(Path,sys.argv[1:]);report=audit(source,motion);out.write_text(json.dumps(report,indent=2));print(json.dumps(report,indent=2));assert report['passed'],'Direct export changed source payloads'

"""One status pass; apply completed custom clips once within authorized budget."""
import json,urllib.request,pathlib
root=pathlib.Path('/Users/macbook1/Documents/ChatGPT/LighFight');p=root/'art/source/motion-expansion/tasks.json';x=json.loads(p.read_text());out=root/'.tools/meshy/motion-expansion';key=pathlib.Path('/Users/macbook1/.config/lighfight-meshy/api-key').read_text().strip()
def request(path,data=None):
 return json.load(urllib.request.urlopen(urllib.request.Request('https://api.meshy.ai/openapi/v1/'+path,data=json.dumps(data).encode() if data else None,headers={'Authorization':'Bearer '+key,'Content-Type':'application/json'}),timeout=60))
for t in x.get('custom_motions',[]):
 d=request('text-to-motion/'+t['task_id']);t['status']=d['status'];print(t['name'],d['status'],flush=True)
 if d['status']=='SUCCEEDED' and 'animation_task' not in t:
  assert x['submitted_credits']+3<=x['budget'];r=request('animations',{'rig_task_id':'01a124e9-8f53-7205-98aa-8fd59ad24ab1','motion_task_id':t['task_id']});t['animation_task']=r['result'];x['submitted_credits']+=3;p.write_text(json.dumps(x,indent=2));print('applied',t['name'],flush=True)
 if 'animation_task' in t:
  d=request('animations/'+t['animation_task']);t['animation_status']=d['status']
  if d['status']=='SUCCEEDED':
   dest=out/(t['name']+'-animation_glb_url.glb')
   if not dest.exists():urllib.request.urlretrieve(d['result']['animation_glb_url'],dest)
 p.write_text(json.dumps(x,indent=2))
# Free walking clip from the existing rig.
d=json.loads((out/'rose-rig-status.json').read_text());dest=out/'Walk-animation_glb_url.glb'
if not dest.exists():urllib.request.urlretrieve(d['result']['basic_animations']['walking_armature_glb_url'],dest)

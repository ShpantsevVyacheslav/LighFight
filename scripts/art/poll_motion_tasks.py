"""Read-only status/download pass; never submits chargeable operations."""
import json,urllib.request,pathlib,struct,sys
from concurrent.futures import ThreadPoolExecutor
root=pathlib.Path('/Users/macbook1/Documents/ChatGPT/LighFight');out=root/'.tools/meshy/motion-expansion';tasks=json.loads((root/'art/source/motion-expansion/tasks.json').read_text());key=pathlib.Path('/Users/macbook1/.config/lighfight-meshy/api-key').read_text().strip()
def fetch(entry):
 kind,task,name=entry
 req=urllib.request.Request('https://api.meshy.ai/openapi/v1/'+kind+'/'+task,headers={'Authorization':'Bearer '+key})
 try:
  data=json.load(urllib.request.urlopen(req));(out/(name+'-status.json')).write_text(json.dumps(data));print(name,data['status'],data.get('progress'),data.get('consumed_credits'))
  if data['status']=='SUCCEEDED':
   urls=data.get('result',{});urls={**data.get('model_urls',{}),**urls}
   for label,url in urls.items():
    if isinstance(url,str) and url.startswith('https:') and ('glb' in label or label=='glb'):
     dest=out/(name+'-'+label+'.glb')
     valid=dest.exists() and dest.stat().st_size>12 and struct.unpack('<III',dest.read_bytes()[:12])[2]==dest.stat().st_size
     if not valid:
      temp=dest.with_suffix('.partial');urllib.request.urlretrieve(url,temp);temp.replace(dest);print('saved',dest.name,flush=True)
 except Exception as e:print(name,type(e).__name__,str(e)[:120])

entries=[('rigging',tasks['rose_rig'],'rose-rig')]+[('animations',a['task_id'],str(a['action_id'])) for a in tasks['animations']]
if '--walk-only' in sys.argv:entries=[e for e in entries if e[2] in ['689','688']]
if '--new-only' in sys.argv:entries=[e for e in entries if e[2] in ['197','214','218','90']]
with ThreadPoolExecutor(max_workers=4) as pool:list(pool.map(fetch,entries))

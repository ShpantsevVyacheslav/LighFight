import json,urllib.request
from pathlib import Path
root=Path(__file__).resolve().parents[2];folder=root/'art/source/original-island';key=(Path.home()/'.config/lighfight-meshy/api-key').read_text().strip();task=json.loads((folder/'meshy-rig-task.json').read_text());req=urllib.request.Request('https://api.meshy.ai/openapi/v1/rigging/'+task['taskId'],headers={'Authorization':'Bearer '+key})
with urllib.request.urlopen(req,timeout=30) as response:data=json.load(response)
print(json.dumps({k:data.get(k) for k in ['id','status','progress','consumed_credits','task_error']}))
if data['status']=='SUCCEEDED':
 result=data['result'];target=folder/'meshy-rig';target.mkdir(exist_ok=True)
 for name in ['rigged_character_glb_url','rigged_character_fbx_url']:
  dest=target/('original-rig.'+('glb' if 'glb' in name else 'fbx'))
  if not dest.exists():urllib.request.urlretrieve(result[name],dest)
 task.update({'status':data['status'],'consumedCredits':data['consumed_credits']});(folder/'meshy-rig-task.json').write_text(json.dumps(task,indent=2))
 print('Rig downloaded')

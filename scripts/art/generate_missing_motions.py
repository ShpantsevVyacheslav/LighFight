"""Submit explicitly authorized motion batch once, with persistent credit ledger."""
import json,urllib.request,pathlib
root=pathlib.Path('/Users/macbook1/Documents/ChatGPT/LighFight');p=root/'art/source/motion-expansion/tasks.json';x=json.loads(p.read_text());key=pathlib.Path('/Users/macbook1/.config/lighfight-meshy/api-key').read_text().strip()
actions={'body':'a short right straight punch aimed at an opponents abdomen','lowKick':'a right roundhouse low kick striking an opponents outer thigh','jumpKick':'a small forward jump with a right straight kick, landing back on both feet','backfist':'a right spinning backfist strike at head height','axeKick':'a right axe kick, raising a straight leg high then dropping the heel downward','sideKick':'a right side kick at chest height, pivoting the standing foot','hammer':'a short downward right hammerfist punch at chest height','palm':'a forceful forward push using both closed fists at chest height','heelKick':'a right spinning heel kick at head height','switchKick':'switch the stance with a small hop then deliver a left roundhouse kick at waist height'}
x.setdefault('custom_motions',[])
for name,description in actions.items():
 if any(t['name']==name for t in x['custom_motions']):continue
 assert x['submitted_credits']+10<=x['budget']
 prompt='A trained unarmed fighter starts in a balanced boxing guard, performs '+description+', then returns to guard. One attack only. Natural anatomy, stable footwork, no weapons. Face forward, remain near the origin.'
 req=urllib.request.Request('https://api.meshy.ai/openapi/v1/text-to-motion',data=json.dumps({'prompt':prompt,'mode':'prime','duration':2}).encode(),headers={'Authorization':'Bearer '+key,'Content-Type':'application/json'})
 try:
  data=json.load(urllib.request.urlopen(req));x['custom_motions'].append({'name':name,'task_id':data['result'],'prompt':prompt,'credits':10});x['submitted_credits']+=10;p.write_text(json.dumps(x,indent=2));print(name,data['result'],flush=True)
 except urllib.error.HTTPError as e:print(name,e.code,e.read().decode()[:300]);break

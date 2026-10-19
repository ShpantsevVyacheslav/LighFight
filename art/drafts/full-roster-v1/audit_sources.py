import json,struct,sys,importlib.util,hashlib
from pathlib import Path
ROOT=Path(__file__).resolve().parents[3]
sys.path.insert(0,str(ROOT/'art/drafts/sailor-native67-dense-v1/tools'))
from audit_sources_offline import audit
rows=[('bone','Костяная леди','bone-tripo-v1/original.glb','bone-tripo-v1/unity-pack/*.fbx'),('witch','Ночная ведьма','tripo-witch-original/unpacked/*.fbx','tripo-witch-native-full-pack/unpacked/*.fbx'),('master','Мастер стихий','master-tripo-v1/original.glb','tripo-master-native-pack/unpacked/*.fbx'),('island','Островной боец','island-tripo-v1/dense-mixamo/*.glb','island-tripo-v1/unity-pack/*.fbx'),('rose','Розовая пантера','rose-tripo-v1/original.glb','rose-tripo-v1/unity-pack/*.fbx'),('ranger','Рейнджер','ranger-tripo-v1/original.glb','ranger-tripo-v1/unity-pack/*.fbx'),('gala','Гала-боец','tripo-gala-original/unpacked/*.fbx','tripo-gala-native-full-pack/unpacked/*.fbx'),('cobalt','Кобальтовый чемпион','tripo-cobalt-original/unpacked/*.fbx','tripo-cobalt-native-full-pack/unpacked/*.fbx'),('retro','Ретро','tripo-retro-denim-apose-original/unpacked/*.fbx','tripo-retro-denim-native-full-pack/unpacked/*.fbx'),('sailor','Моряк','tripo-striped-sailor-apose-original/unpacked/*.fbx','tripo-striped-sailor-native-full-pack/unpacked/*.fbx'),('summer','Дачник','tripo-summer-beard-apose-original/unpacked/*.fbx','tripo-summer-native-full-pack/unpacked/*.fbx')]
def one(pattern):
 files=sorted((ROOT/'art/source').glob(pattern));return files[0] if files else None
def summary(p):
 if not p:return {'status':'MISSING'}
 if p.suffix=='.glb':
  with p.open('rb') as f:
   head=f.read(20);n=struct.unpack_from('<I',head,12)[0];d=json.loads(f.read(n))
  acc=d['accessors'];tris=sum((acc[m['indices']]['count']//3 if 'indices'in m else acc[m['attributes']['POSITION']]['count']//3) for mesh in d.get('meshes',[]) for m in mesh['primitives']);bones=max([len(s['joints'])for s in d.get('skins',[])]+[0]);r={'triangles':tris,'bones':bones,'animations':len(d.get('animations',[]))}
 else:
  r,*_=audit(p);r={k:r.get(k) for k in ['vertices','triangles','clusterBindCount','weightStats']}
 return {'path':str(p.relative_to(ROOT)),'bytes':p.stat().st_size,**r}
out=[]
for i,(id,name,original,native)in enumerate(rows):
 row={'index':i,'id':id,'name':name,'original':summary(one(original)),'nativePack':summary(one(native))};out.append(row);print(json.dumps(row,ensure_ascii=False),flush=True)
(ROOT/'art/drafts/full-roster-v1/inventory.json').write_text(json.dumps({'status':'READ_ONLY_SOURCE_AUDIT','fighters':out},ensure_ascii=False,indent=2)+'\n')

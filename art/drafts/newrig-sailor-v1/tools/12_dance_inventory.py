"""Read-only FBX/GLB dance inventory. Rawcurve hashes are not semantic-uniqueness claims."""
import sys,types,importlib,json,hashlib,struct,array
from pathlib import Path
ROOT=Path(__file__).resolve().parents[4];O=ROOT/'art/drafts/newrig-sailor-v1'
addon=Path('/Users/macbook1/Applications/Blender.app/Contents/Resources/4.2/scripts/addons_core/io_scene_fbx');pkg=types.ModuleType('inventory_fbx');pkg.__path__=[str(addon)];sys.modules[pkg.__name__]=pkg;parser=importlib.import_module('inventory_fbx.parse_fbx')
paths={
'Island':'art/source/island-tripo-v1/unity-pack/*.fbx','Ranger':'art/source/ranger-tripo-v1/unity-pack/*.fbx','Bone':'art/source/bone-tripo-v1/unity-pack/*.fbx','Rose':'art/source/rose-tripo-v1/unity-pack/*.fbx',
'Master':'art/source/tripo-master-native-pack/unpacked/*.fbx','Witch':'art/source/tripo-witch-native-full-pack/unpacked/*.fbx','Retro':'art/source/tripo-retro-denim-native-full-pack/unpacked/*.fbx','Sailor':'art/source/tripo-striped-sailor-native-full-pack/unpacked/*.fbx','Summer':'art/source/tripo-summer-native-full-pack/unpacked/*.fbx','Gala':'art/source/tripo-gala-native-full-pack/unpacked/*.fbx','Cobalt':'art/source/tripo-cobalt-native-full-pack/unpacked/*.fbx'}
def name(b):return b.decode('utf8','replace').split('\x00')[0]
report={'actors':{},'additionalLocalDanceGLBs':[],'warning':'Matching names/timing are shared-template evidence; rawhash differences do not prove distinct authored choreography.'}
for actor,pattern in paths.items():
 files=list(ROOT.glob(pattern));assert len(files)==1,(actor,files);f=files[0];root,version=parser.parse(str(f));objects=next(e for e in root.elems if e.id==b'Objects');connections=next(e for e in root.elems if e.id==b'Connections');byid={e.props[0]:e for e in objects.elems if e.props and isinstance(e.props[0],int)};edges=[e.props for e in connections.elems if e.id==b'C'];stacks=[e for e in objects.elems if e.id==b'AnimationStack'];clips=[]
 for stack in stacks:
  if not any(k in name(stack.props[1]).lower() for k in ['dance','hiphop','samba','salsa']):continue
  props={name(e.props[0]):e.props[-1] for p in stack.elems if p.id==b'Properties70' for e in p.elems if e.id==b'P'};start=props.get('LocalStart',0);stop=props.get('LocalStop',0);layerids={e[1] for e in edges if len(e)>=3 and e[2]==stack.props[0]};nodeids={e[1] for e in edges if len(e)>=3 and e[2] in layerids};curveids={e[1] for e in edges if len(e)>=3 and e[2] in nodeids and e[1] in byid and byid[e[1]].id==b'AnimationCurve'};h=hashlib.sha256();keycount=0;timehash=hashlib.sha256()
  for i in sorted(curveids):
   c=byid[i]
   for e in c.elems:
    if e.id in [b'KeyTime',b'KeyValueFloat']:
     a=e.props[0];blob=a.tobytes() if hasattr(a,'tobytes') else repr(a).encode();h.update(e.id+blob)
     if e.id==b'KeyTime':keycount+=len(a);timehash.update(blob)
  clips.append({'name':name(stack.props[1]),'durationSeconds':(stop-start)/46186158000,'curveCount':len(curveids),'totalKeyTimes':keycount,'rawCurveDataSHA256':h.hexdigest(),'rawTimingSHA256':timehash.hexdigest()})
 report['actors'][actor]={'source':str(f.relative_to(ROOT)),'sourceSHA256':hashlib.sha256(f.read_bytes()).hexdigest(),'danceClips':clips};print(actor,json.dumps(clips),flush=True)
for f in ROOT.glob('art/source/**/*.glb'):
 if not any(t in f.name.lower() for t in ['dance','hip_hop','samba','salsa']):continue
 data=f.read_bytes();n=struct.unpack_from('<I',data,12)[0];g=json.loads(data[20:20+n]);anims=[]
 for a in g.get('animations',[]):
  ends=[g['accessors'][s['input']].get('max',[None])[0] for s in a.get('samplers',[])];anims.append({'name':a.get('name','unnamed'),'durationSeconds':max((x for x in ends if x is not None),default=None),'channels':len(a.get('channels',[]))})
 report['additionalLocalDanceGLBs'].append({'path':str(f.relative_to(ROOT)),'sha256':hashlib.sha256(data).hexdigest(),'animations':anims})
report['uniqueElevenAssignmentFeasibleFromVerifiedInventory']=False
report['recommendation']='Do not assign same dance_01 eleven names and call unique. Additional authored dances require explicit clip provenance/choreography comparison and retarget quality gate.'
(O/'dance-inventory.json').write_text(json.dumps(report,indent=2))

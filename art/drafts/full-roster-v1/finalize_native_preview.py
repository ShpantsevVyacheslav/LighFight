from pathlib import Path
import sys,json,struct,hashlib,io,math
from PIL import Image
ROOT=Path('/Users/macbook1/Documents/ChatGPT/LighFight');out=ROOT/'prototypes/roster-portraits/models'
def sha(b):return hashlib.sha256(b).hexdigest()
def finalize(role):
 path=out/f'{role}.glb';manifest=out/f'{role}.manifest.json';m=json.loads(manifest.read_text());raw=path.read_bytes();jl=struct.unpack_from('<I',raw,12)[0];g=json.loads(raw[20:20+jl]);bl=struct.unpack_from('<I',raw,20+jl)[0];binary=bytearray(raw[28+jl:28+jl+bl]);source=ROOT/m['sourceFBX'];fbm=source.with_suffix('.fbm')
 def append(data):
  while len(binary)%4:binary.append(0)
  offset=len(binary);binary.extend(data);i=len(g['bufferViews']);g['bufferViews'].append({'buffer':0,'byteOffset':offset,'byteLength':len(data)});return i
 def accessor(data,kind,count):
  view=append(data);i=len(g['accessors']);g['accessors'].append({'bufferView':view,'componentType':5126,'count':count,'type':kind});return i
 def bytes_accessor(index):
  a=g['accessors'][index];view=g['bufferViews'][a['bufferView']];n={'SCALAR':1,'VEC2':2,'VEC3':3,'VEC4':4,'MAT4':16}[a['type']];sizes={5126:4,5123:2,5121:1,5125:4};size=sizes[a['componentType']]*n;offset=view.get('byteOffset',0)+a.get('byteOffset',0);stride=view.get('byteStride',size);return a,b''.join(binary[offset+i*stride:offset+i*stride+size] for i in range(a['count']))
 # Keep authored originals byte-for-byte; glTF scalar layout is a derived lossless PNG only.
 images=[]
 rough=Image.open(next(fbm.glob('*roughness*'))).convert('RGB');metal=Image.open(next(fbm.glob('*metallic*'))).convert('RGB');assert rough.size==metal.size;white=Image.new('L',rough.size,255);packed=Image.merge('RGB',(white,rough.getchannel('R'),metal.getchannel('R')));stream=io.BytesIO();packed.save(stream,format='PNG');orm=stream.getvalue()
 for image in g['images']:
  name=image.get('name','').lower()
  if 'basecolor' in name:p=next(fbm.glob('*basecolor*'));data=p.read_bytes();mime='image/jpeg';method='untouched original basecolor bytes'
  elif 'normal' in name:p=next(fbm.glob('*normal*'));data=p.read_bytes();mime='image/jpeg';method='untouched original normal bytes; no gamma/Y-flip'
  elif 'rough' in name or 'metal' in name:p=None;data=orm;mime='image/png';method='derivedglTF ORM R=255,G=original roughnessR,B=original metallicR; raw scalar values, no gamma conversion'
  else:raise RuntimeError('Unexpected image '+name)
  mime='image/png' if data.startswith(b'\x89PNG') else 'image/jpeg';image['bufferView']=append(data);image['mimeType']=mime;image.pop('uri',None);im=Image.open(io.BytesIO(data));images.append({'name':image['name'],'mimeType':mime,'bytes':len(data),'dimensions':list(im.size),'sha256':sha(data),'source':str(p.relative_to(ROOT)) if p else None,'method':method});assert min(im.size)>=1024
 # Derive exactly one constant presentation clip from native box_02 first exported sample.
 g['animations']=[a for a in g['animations'] if a['name']!='PreviewGuard'];box=next(a for a in g['animations'] if a['name']=='box_02');time=accessor(struct.pack('<2f',0,1),'SCALAR',2);g['accessors'][time].update(min=[0],max=[1]);samplers=[];channels=[]
 for channel in box['channels']:
  sampler=box['samplers'][channel['sampler']];a,data=bytes_accessor(sampler['output']);components={'VEC3':3,'VEC4':4}[a['type']];offset=components*4 if sampler.get('interpolation')=='CUBICSPLINE' else 0;first=data[offset:offset+components*4];assert len(first)==components*4;index=accessor(first+first,a['type'],2);samplers.append({'input':time,'output':index,'interpolation':'LINEAR'});channels.append({'sampler':len(samplers)-1,'target':channel['target'].copy()})
 g['animations'].append({'name':'PreviewGuard','samplers':samplers,'channels':channels,'extras':{'authoredIdle':False,'derivedFrom':'box_02','sourceSeconds':0,'description':'Two identical keys from native first combat-start pose; presentation hold only, not invented idle motion'}})
 g['buffers'][0]['byteLength']=len(binary);js=json.dumps(g,separators=(',',':')).encode();js+=b' '*((-len(js))%4);binary.extend(b'\0'*((-len(binary))%4));result=struct.pack('<III',0x46546c67,2,12+8+len(js)+8+len(binary))+struct.pack('<II',len(js),0x4e4f534a)+js+struct.pack('<II',len(binary),0x004e4942)+binary;path.write_bytes(result)
 assert len(g['animations'])==9;tri=sum(g['accessors'][p['indices']]['count']//3 for mesh in g['meshes'] for p in mesh['primitives']);assert tri==m['sourceTriangles']
 # Validate skin attributes, finite normalized weights, and valid joint indices directly from finalglTF.
 weight_min=1;weight_max=0;maxinf=0;verts=0
 for mesh in g['meshes']:
  for primitive in mesh['primitives']:
   attr=primitive['attributes'];wa,wb=bytes_accessor(attr['WEIGHTS_0']);ja,jb=bytes_accessor(attr['JOINTS_0']);assert wa['componentType']==5126
   weights=struct.unpack('<'+'f'*(len(wb)//4),wb);fmt={5121:'B',5123:'H'}[ja['componentType']];joints=struct.unpack('<'+fmt*(len(jb)//struct.calcsize(fmt)),jb);assert max(joints)<m['sourceBones']
   for i in range(0,len(weights),4):
    w=weights[i:i+4];assert all(math.isfinite(x) and x>=0 for x in w);total=sum(w);assert abs(total-1)<1e-5;weight_min=min(weight_min,total);weight_max=max(weight_max,total);maxinf=max(maxinf,sum(x>0 for x in w));verts+=1
 m.update(glbSHA256=sha(result),bytes=len(result),embeddedImages=images,defaultAnimation='PreviewGuard',defaultAnimationTimeSeconds=0,defaultAnimationPlayback='loop constant clip; fullnative motions only ongesture',derivedClips=[{'name':'PreviewGuard','durationSeconds':1,'sourceAnimation':'box_02','sourceSeconds':0,'identicalKeyframes':2,'authoredIdle':False}],validation={'triangles':tri,'sourceClips':8,'totalClips':9,'skinJoints':[len(s['joints']) for s in g['skins']],'glbSplitVertices':verts,'weightsSumMin':weight_min,'weightsSumMax':weight_max,'maxInfluences':maxinf,'originalBasecolorNormalBytesPreserved':True,'finiteNormalizedWeights':True})
 m['animations']=[a for a in m['animations'] if a['name']!='PreviewGuard']+[{'name':'PreviewGuard','durationSeconds':1,'channels':len(channels),'derived':True}];m['firstFrameGuardCandidate']['poseValidity']='native authored crouched/open-hand combat-start; not A/T pose; not closed-fist standalone guard';m['firstFrameGuardCandidate']['derivedClip']='PreviewGuard';manifest.write_text(json.dumps(m,indent=2));print(json.dumps({'role':role,'bytes':len(result),'triangles':tri,'animations':[a['name'] for a in g['animations']],'validation':m['validation']}))
for role in sys.argv[1:]:finalize(role)

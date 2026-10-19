"""Lossless static Meshy model placement; does not publish Unity assets/catalog.
Run with bundled NumPy python. No engine, network or texture codecs required.
"""
import sys,json,math,struct,hashlib,copy,argparse
from pathlib import Path
import numpy as np
ROOT=Path(__file__).resolve().parents[4];OUT=ROOT/'art/drafts/meeting-meshy-v1';sys.path.insert(0,str(ROOT/'art/drafts/kitchen-env-lod-v1/tools'))
from glb_static import load,combine,write,audit_images

def local_matrix(node):
 if 'matrix' in node:return np.asarray(node['matrix'],float).reshape((4,4),order='F')
 t=np.eye(4);t[:3,3]=node.get('translation',[0,0,0]);x,y,z,w=node.get('rotation',[0,0,0,1]);r=np.eye(4);r[:3,:3]=[[1-2*(y*y+z*z),2*(x*y-z*w),2*(x*z+y*w)],[2*(x*y+z*w),1-2*(x*x+z*z),2*(y*z-x*w)],[2*(x*z-y*w),2*(y*z+x*w),1-2*(x*x+y*y)]];s=np.diag([*node.get('scale',[1,1,1]),1]);return t@r@s

def world_bounds(path):
 g,b=load(path);assert not g.get('skins') and not g.get('animations'),'Static generated object required'
 lo=np.full(3,np.inf);hi=-lo;tri=0;stack=[(n,np.eye(4)) for n in g['scenes'][g.get('scene',0)]['nodes']];seen=set()
 while stack:
  index,parent=stack.pop();assert index not in seen,'Shared node or cyclic hierarchy';seen.add(index);node=g['nodes'][index];matrix=parent@local_matrix(node)
  if 'mesh' in node:
   for p in g['meshes'][node['mesh']]['primitives']:
    a=g['accessors'][p['attributes']['POSITION']];assert a['componentType']==5126 and a['type']=='VEC3' and 'sparse' not in a
    v=g['bufferViews'][a['bufferView']];stride=v.get('byteStride',12);assert stride>=12
    pos=np.ndarray((a['count'],3),dtype='<f4',buffer=b,offset=v.get('byteOffset',0)+a.get('byteOffset',0),strides=(stride,4));world=pos@matrix[:3,:3].T+matrix[:3,3];lo=np.minimum(lo,world.min(axis=0));hi=np.maximum(hi,world.max(axis=0));tri+=g['accessors'][p['indices']]['count']//3 if 'indices' in p else a['count']//3
  stack.extend((c,matrix) for c in node.get('children',[]))
 assert np.isfinite(lo).all() and np.isfinite(hi).all() and np.all(hi>lo),'Invalid or empty model bounds'
 return lo,hi,tri

def place(row,scene):
 path=ROOT/row['source'];lo,hi,tris=world_bounds(path);axis=row.get('normalizeAxis',1);span=row.get('targetSpanMetres',row['heightMetres']);scale=span/(hi[axis]-lo[axis]);a=math.radians(row.get('yawDegrees',0));rot=np.array([[math.cos(a),0,math.sin(a)],[0,1,0],[-math.sin(a),0,math.cos(a)]]);center=np.array(row[scene+'Center']);translation=center-rot@((lo+hi)*.5*scale)
 if 'contactSourceY' in row:translation[1]=row['surfaceY']-row['contactSourceY']*scale
 elif 'anchorBottomToY' in row:translation[1]=row['anchorBottomToY']-lo[1]*scale
 wrapper={'name':'Meshy '+row['id'],'translation':translation.tolist(),'scale':[scale]*3,'rotation':[0,math.sin(a/2),0,math.cos(a/2)]}
 corners=np.array([[x,y,z] for x in [lo[0],hi[0]] for y in [lo[1],hi[1]] for z in [lo[2],hi[2]]]);bounds=corners*scale@rot.T+translation
 if scene=='gameplay':
  mn=bounds.min(axis=0);mx=bounds.max(axis=0)
  assert any(mx[k]<=[-5.5,0,-2][k] or mn[k]>=[5.5,2.2,2][k] for k in range(3)),('Generated prop crosses combat bounds',row['id'],mn.tolist(),mx.tolist())
 receipt={'id':row['id'],'source':row['source'],'sourceSHA256':hashlib.sha256(path.read_bytes()).hexdigest(),'sourceTriangles':tris,'sourceWorldBounds':[lo.tolist(),hi.tolist()],'uniformScale':scale,'placement':wrapper,'placedBounds':[bounds.min(axis=0).tolist(),bounds.max(axis=0).tolist()],'sourceImages':audit_images(path),'scope':'Bounding-box normalization only; no topology, vertex, UV, material or stored image changes.'}
 if row.get('materialOverride'):
  graph,raw=load(path);graph=copy.deepcopy(graph)
  for material in graph['materials']:
   material.setdefault('pbrMetallicRoughness',{}).update(row['materialOverride'])
   if 'metallicFactor' in row['materialOverride'] and row['materialOverride']['metallicFactor']==0:material['pbrMetallicRoughness'].pop('metallicRoughnessTexture',None)
  derived=OUT/(row['id']+'-material-'+hashlib.sha256(path.read_bytes()).hexdigest()[:8]+'.glb');write(derived,graph,raw);receipt['materialFactorOverride']=row['materialOverride'];receipt['materialDerivative']=str(derived.relative_to(ROOT));path=derived
 return path,wrapper,receipt

def rug(path,row,scene):
 # Exact supplied texture bytes embedded; single plane YZ, +X inward-facing.
 texture=ROOT/(row.get(scene+'Texture') or row['texture']);payload=texture.read_bytes();c=row[scene+'Center'];w=row.get(scene+'SizeAlongZ',row['sizeAlongZ']);h=row.get(scene+'Height',row['height']);positions=[(c[0],c[1]-h/2,c[2]-w/2),(c[0],c[1]+h/2,c[2]-w/2),(c[0],c[1]+h/2,c[2]+w/2),(c[0],c[1]-h/2,c[2]+w/2)];normal=[(1,0,0)]*4;uv=[(1,1),(1,0),(0,0),(0,1)];indices=[0,1,2,0,2,3]
 data=bytearray();g={'asset':{'version':'2.0'},'accessors':[],'bufferViews':[],'meshes':[],'nodes':[],'scenes':[{'nodes':[0]}],'scene':0}
 def add(vals,n,fmt,kind,target):
  flat=[x for v in vals for x in v] if n>1 else vals;data.extend(b'\0'*((-len(data))%4));v=len(g['bufferViews']);raw=struct.pack('<'+str(len(flat))+fmt,*flat);g['bufferViews'].append({'buffer':0,'byteOffset':len(data),'byteLength':len(raw),'target':target});data.extend(raw);a={'bufferView':v,'componentType':5126 if fmt=='f' else 5125,'count':len(vals),'type':kind}
  if kind=='VEC3':a.update(min=[min(v[j] for v in vals) for j in range(3)],max=[max(v[j] for v in vals) for j in range(3)])
  g['accessors'].append(a);return len(g['accessors'])-1
 attrs={'POSITION':add(positions,3,'f','VEC3',34962),'NORMAL':add(normal,3,'f','VEC3',34962),'TEXCOORD_0':add(uv,2,'f','VEC2',34962)};ii=add(indices,1,'I','SCALAR',34963);data.extend(b'\0'*((-len(data))%4));v=len(g['bufferViews']);g['bufferViews'].append({'buffer':0,'byteOffset':len(data),'byteLength':len(payload)});data.extend(payload);g['images']=[{'bufferView':v,'mimeType':'image/png' if payload[:8]==b'\x89PNG\r\n\x1a\n' else 'image/jpeg'}];g['textures']=[{'source':0}];g['materials']=[{'name':'Reference rug repaired textile','doubleSided':True,'pbrMetallicRoughness':{'baseColorTexture':{'index':0},'metallicFactor':0,'roughnessFactor':1}}];g['meshes']=[{'name':'Rug opposite windows','primitives':[{'attributes':attrs,'indices':ii,'material':0}]}];g['nodes']=[{'name':'Rug opposite windows','mesh':0}];write(path,g,data)
 return {'texture':str(texture.relative_to(ROOT)),'imageSHA256':hashlib.sha256(payload).hexdigest(),'normal':[1,0,0],'center':c,'sizeAlongZ':w,'height':h}

def combine_instances(parts,dst):
 # Repeated laptop meshes/materials/textures are stored once; only node trees are cloned.
 unique=[];keys={};order=[]
 for path,wrapper in parts:
  key=str(Path(path).resolve())
  if key not in keys:keys[key]=len(unique);unique.append((path,wrapper));order.append((False,keys[key],wrapper))
  else:order.append((True,keys[key],wrapper))
 combine(unique,dst);graph,raw=load(dst);roots=graph['scenes'][0]['nodes'][:]
 def clone(index):
  node=copy.deepcopy(graph['nodes'][index]);children=node.pop('children',None);new=len(graph['nodes']);graph['nodes'].append(node)
  if children is not None:node['children']=[clone(c) for c in children]
  return new
 final=[]
 for repeated,index,wrapper in order:
  root=roots[index]
  if not repeated:final.append(root);continue
  assert wrapper is not None,'Repeated model must have an explicit placement wrapper'
  node=copy.deepcopy(wrapper);node['children']=[clone(c) for c in graph['nodes'][root]['children']];final.append(len(graph['nodes']));graph['nodes'].append(node)
 graph['scenes'][0]['nodes']=final;return write(dst,graph,raw),unique,len(parts)-len(unique)

def main():
 ap=argparse.ArgumentParser();ap.add_argument('--scene',choices=['review','gameplay'],default='review');ap.add_argument('--plan',default=str(OUT/'import-plan.json'));ap.add_argument('--with-base',action='store_true');args=ap.parse_args();plan=json.loads(Path(args.plan).read_text());parts=[];rows=[];missing=[]
 assert plan['catalogPublicationAllowed'] is False,'Adapter must never publish catalog'
 if args.with_base:
  parts.append((ROOT/plan['base' if args.scene=='review' else 'gameplayBase'],{'name':'Tripo furniture and '+args.scene+' room shell'}))
 for row in plan['assets']:
  if not row.get('source'):missing.append(row['id']);continue
  path,wrapper,receipt=place(row,args.scene);parts.append((path,wrapper));rows.append(receipt)
 if plan.get('auxiliaryCup'):
  cup=plan['auxiliaryCup'];parts.append((ROOT/cup['source'],{'name':'Authored lightweight red cup','translation':cup[args.scene+'Bottom']}))
 rugrow=None
 if plan.get('rug',{}).get('texture'):
  rp=OUT/(args.scene+'-rug.glb');rugrow=rug(rp,plan['rug'],args.scene);parts.append((rp,{'name':'Textile opposite window wall'}))
 if not parts:print(json.dumps({'status':'NO_SOURCE_ASSETS','missing':missing}));return
 dst=OUT/(args.scene+('-with-room' if args.with_base else '-props')+'.glb');digest,uniqueParts,repeatedInstances=combine_instances(parts,dst)
 # Check every source binary chunk and every embedded image after reference remapping.
 _,assembled=load(dst);cursor=0;images=[]
 for part,_ in uniqueParts:
  _,original=load(part);cursor+=(-cursor)%4;assert assembled[cursor:cursor+len(original)]==original,'Binary payload changed during assembly';cursor+=len(original);images+=audit_images(part)
 assert audit_images(dst)==images,'Stored source images changed during assembly'
 result={'status':'ASSEMBLED_PENDING_ACTUAL_RENDER','scene':args.scene,'masterSHA256':digest,'missing':missing,'assets':rows,'rug':rugrow,'catalogPublished':False,'imageAndGeometryPayloadsCopied':True,'sourceBinaryChunksExact':True,'reusedModelInstances':repeatedInstances}
 (dst.with_suffix('.json')).write_text(json.dumps(result,indent=2));print(json.dumps({'status':result['status'],'file':str(dst),'assets':len(rows),'missing':missing}))
if __name__=='__main__':main()

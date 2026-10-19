import json,struct,array
from pathlib import Path
src=Path('/Users/macbook1/Documents/ChatGPT/LighFight/art/source/office-kitchen-meshy-v1/preview.glb');b=src.read_bytes();n=struct.unpack_from('<I',b,12)[0];j=json.loads(b[20:20+n]);binary=20+n+8
p=j['meshes'][0]['primitives'][0]
def data(index,code):
 a=j['accessors'][index];v=j['bufferViews'][a['bufferView']];offset=binary+v.get('byteOffset',0)+a.get('byteOffset',0);items=3 if a['type']=='VEC3' else 1;out=array.array(code);out.frombytes(b[offset:offset+a['count']*items*out.itemsize]);return out
v=data(p['attributes']['POSITION'],'f');a=j['accessors'][p['indices']];idx=data(p['indices'],'I' if a['componentType']==5125 else 'H')
points=[(x,z) for x in [-.3,0,.3] for z in [-.25,0,.25]];hits=[[] for _ in points]
for k in range(0,len(idx),3):
 ai,bi,ci=[idx[k+i]*3 for i in range(3)];ax,ay,az=v[ai:ai+3];bx,by,bz=v[bi:bi+3];cx,cy,cz=v[ci:ci+3];den=(bz-cz)*(ax-cx)+(cx-bx)*(az-cz)
 if abs(den)<1e-12:continue
 for h,(x,z) in enumerate(points):
  if x<min(ax,bx,cx) or x>max(ax,bx,cx) or z<min(az,bz,cz) or z>max(az,bz,cz):continue
  u=((bz-cz)*(x-cx)+(cx-bx)*(z-cz))/den;w=((cz-az)*(x-cx)+(ax-cx)*(z-cz))/den;t=1-u-w
  if min(u,w,t)>=-1e-7:hits[h].append(u*ay+w*by+t*cy)
result=[{'gltf_x':x,'gltf_z':z,'intersection_y':sorted(set(round(y,6) for y in hit))} for (x,z),hit in zip(points,hits)]
Path(__file__).with_name('floor-probe.json').write_text(json.dumps(result,indent=2));print(result)

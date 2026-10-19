"""Read-only LNC1 key conditioning audit; key rotations are not smoothed/repaired."""
import struct,zlib,json,math
from pathlib import Path

def audit(path):
 raw=zlib.decompress(path.read_bytes(),-15);offset=0
 def i():
  nonlocal offset
  v=struct.unpack_from('<i',raw,offset)[0];offset+=4;return v
 def f():
  nonlocal offset
  v=struct.unpack_from('<f',raw,offset)[0];offset+=4;return v
 assert i()==0x31434e4c
 times=[[f() for _ in range(i())] for _ in range(i())];curves=[]
 for _ in range(i()):
  pre,post,ti=i(),i(),i();values=[]
  for t in times[ti]:
   values.append(f());f();f();f();f();i()
  curves.append((times[ti],values))
 maps=[[i() for _ in range(10)] for _ in range(i())];assert offset==len(raw)
 paths=[x[4:] for x in path.with_name('Dance.pose.asset').read_text().splitlines() if x.startswith('  - ')]
 worst=[]
 for name,m in zip(paths,maps):
  if m[3]<0:continue
  cs=[curves[k] for k in m[3:7]];ts=cs[0][0];assert all(c[0]==ts for c in cs)
  qs=list(zip(*(c[1] for c in cs)))
  for k,(a,b) in enumerate(zip(qs,qs[1:])):
   dot=sum(x*y for x,y in zip(a,b))/math.sqrt(sum(x*x for x in a)*sum(y*y for y in b));angle=2*math.degrees(math.acos(min(1,abs(dot))))
   if angle>90:worst.append({'path':name,'from':ts[k],'to':ts[k+1],'angle_deg':angle,'signed_dot':dot})
 return {'asset':str(path),'adjacent_steps_over90deg':len(worst),'worst':sorted(worst,key=lambda x:x['angle_deg'],reverse=True)[:8]}
if __name__=='__main__':
 root=Path(__file__).resolve().parents[4];paths=sorted((root/'unity/Assets/UniqueDancesReview').glob('Fighter-*/Dance.pose.asset.curves.bytes'))
 result=[audit(p) for p in paths];out=Path(__file__).resolve().parents[1]/'audit/human-retarget-key-conditioning.json';out.write_text(json.dumps(result,indent=2));print([(p['asset'].split('/')[-2],p['adjacent_steps_over90deg']) for p in result])

#!/usr/bin/env python3
import re,json,difflib,hashlib,argparse
from pathlib import Path
parser=argparse.ArgumentParser();parser.add_argument('--version',choices=['V7','V8'],default='V7');version=parser.parse_args().version
b=Path('.tools/skinning-current100k-review/CurrentBufferBuilds'+version);out=Path('docs/reports/skinning-mode-audit-2026-10-19/buffer-diagnostic');norm={}
for mode in ['CPU','GPU']:
 t=(b/f'scene-{mode}.unity').read_text();parts=re.split(r'^--- !u!(\d+) &(-?\d+)\n',t,flags=re.M);objs=[(parts[i],parts[i+1],parts[i+2]) for i in range(1,len(parts),3)];ids={}
 for cls,ident,body in objs:
  if cls=='1':ids[ident]='GO:'+re.search(r'^  m_Name: (.*)$',body,re.M)[1]
 for cls,ident,body in objs:
  if ident not in ids:
   owner=re.search(r'^  m_GameObject: \{fileID: (-?\d+)\}',body,re.M)
   script=re.search(r'^  m_Script: .*guid: ([a-f0-9]+)',body,re.M)
   ids[ident]=(ids.get(owner[1],'ROOT') if owner else 'ROOT')+':'+cls+(':'+script[1] if script else '')
 result=[]
 for cls,ident,body in objs:
  body=re.sub(r'  buildManifest: .*?(?=^  buildMode:)', '  buildManifest: <independently-compared-manifest>\n',body,flags=re.M|re.S)
  body=re.sub(r'^  buildMode: (CPU|GPU)$','  buildMode: <declared-mode>',body,flags=re.M)
  body=re.sub(r'fileID: (-?\d+)',lambda m:'fileID: '+ids.get(m[1],m[1]),body)
  result.append((ids[ident],body))
 norm[mode]='\n'.join(k+'\n'+v for k,v in sorted(result))
 (out/f'{version.lower()}-scene-normalized-{mode}.txt').write_text(norm[mode])
diff=''.join(difflib.unified_diff(norm['CPU'].splitlines(True),norm['GPU'].splitlines(True),fromfile='CPU',tofile='GPU'));(out/(version.lower()+'-scene-normalized.diff')).write_text(diff)
r={'identical':not diff,'normalization':'Canonical local IDs by GameObject name+component class/script; sort objects, preserve order inside objects; exclude embedded manifest only because independently compared; normalize declared buildMode only. No shader/light/camera/property exclusion.','normalizedSHA':{m:hashlib.sha256(t.encode()).hexdigest() for m,t in norm.items()}};(out/(version.lower()+'-scene-identity.json')).write_text(json.dumps(r,indent=2)+'\n');print(r)

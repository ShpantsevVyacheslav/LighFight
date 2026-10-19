#!/usr/bin/env python3
"""Offline owned-only staging. No engine invocation, no source writes."""
import argparse,hashlib,json,re,shutil,subprocess
from pathlib import Path
repo=Path(__file__).resolve().parents[4];main=repo/'unity';packet=Path(__file__).resolve().parent;dest=repo/'.tools/skinning-current100k-review'
p=argparse.ArgumentParser();p.add_argument('--stage',action='store_true');a=p.parse_args()
h=lambda f:hashlib.sha256(f.read_bytes()).hexdigest()
guidmap={}
for meta in (main/'Assets').rglob('*.meta'):
 m=re.search(r'^guid: ([0-9a-f]{32})',meta.read_text(errors='replace'),re.M)
 if m:
  asset=meta.with_suffix('');guidmap[m[1]]=asset
selected=set();todo=[];unresolved=set()
def add(f):
 if f.is_file() and f not in selected:selected.add(f);todo.append(f)
 # Include meta bytes as authored inputs, without normalizing import settings.
 meta=Path(str(f)+'.meta')
 if meta.is_file() and meta not in selected:selected.add(meta);todo.append(meta)
seed=main/'Assets/LighFight/Art/Drafts/TripoMasterPlayable/MasterHumanoidCandidate.prefab';add(seed)
for folder in ['Assets/LighFight/Runtime','Assets/LighFight/Core','Assets/NativeSupport']:
 for f in (main/folder).rglob('*'):
  if f.suffix in ('.cs','.asmdef','.asmref'):add(f)
add(main/'Assets/LighFight/Resources/Shaders/FittedFilmic.shader')
while todo:
 f=todo.pop()
 if f.suffix.lower() not in ('.meta','.prefab','.asset','.controller','.mat','.anim','.shader','.shadergraph','.cs','.asmdef','.asmref'):continue
 data=f.read_bytes()
 if b'\0' in data[:4096]:continue
 for guid in re.findall(rb'guid:\s*([0-9a-f]{32})',data):
  g=guid.decode()
  if g in guidmap:add(guidmap[g])
  elif g not in ['00000000000000000000000000000000','0000000000000000f000000000000000','0000000000000000e000000000000000']:unresolved.add(g)
 # Local shader includes need closure too (built-in includes remain SDK-owned).
 for inc in re.findall(rb'#include\s+"([^"]+)"',data):
  path=f.parent/inc.decode()
  if path.is_file():add(path)
for f in (main/'ProjectSettings').glob('*'):
 if f.is_file():selected.add(f)
for name in ['manifest.json','packages-lock.json']:
 f=main/'Packages'/name
 if f.is_file():selected.add(f)
files={str(f.relative_to(main)):h(f) for f in sorted(selected)}
manifest={'scope':'UNRELEASED review only; selected original100k actors, current runtime source closure; no main mutation','gitHEAD':subprocess.check_output(['git','rev-parse','HEAD'],cwd=repo).decode().strip(),'main':str(main),'owned':str(dest),'files':files,'unresolvedGUIDs':sorted(unresolved),'sourceBytes':sum(f.stat().st_size for f in selected),'packageDelta':{'com.unity.modules.screencapture':'1.0.0'},'fixtureFiles':{str(f.relative_to(packet)):h(f) for f in sorted(packet.rglob('*.cs'))}}
# A dependency not represented in this snapshot must be resolved before building.
if unresolved:raise SystemExit('Unresolved GUIDs: '+str(sorted(unresolved)))
if a.stage:
 if dest.exists():raise SystemExit('Refusing existing owned project; choose explicit audited refresh, never merge stale copies')
 for f in selected:
  target=dest/f.relative_to(main);target.parent.mkdir(parents=True,exist_ok=True);shutil.copy2(f,target)
 for folder in ['Runtime','Editor']:
  for f in (packet/folder).glob('*.cs'):
   target=dest/'Assets/CurrentSkinningReview'/folder/f.name;target.parent.mkdir(parents=True,exist_ok=True);shutil.copy2(f,target)
 pm=dest/'Packages/manifest.json';packages=json.loads(pm.read_text());packages['dependencies']['com.unity.modules.screencapture']='1.0.0';pm.write_text(json.dumps(packages,indent=2)+'\n')
 (dest/'current-input-snapshot.json').write_text(json.dumps(manifest,indent=2)+'\n')
(packet/'closure-manifest.json').write_text(json.dumps(manifest,indent=2)+'\n')
print(json.dumps({'files':len(files),'bytes':manifest['sourceBytes'],'unresolved':len(unresolved),'staged':a.stage,'owned':str(dest)}))

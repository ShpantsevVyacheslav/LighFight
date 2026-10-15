#!/usr/bin/env python3
"""Read-only source manifest/verification. Never copies, switches or edits a project."""
import argparse,hashlib,json,subprocess
from pathlib import Path
p=argparse.ArgumentParser();p.add_argument('--repository',type=Path,required=True);p.add_argument('--mode',choices=['baseline','current'],required=True);p.add_argument('--staged-project',type=Path);a=p.parse_args();repo=a.repository.resolve();result={}
if a.mode=='baseline':
 paths=subprocess.check_output(['git','ls-tree','-r','--name-only','96f0222','--','unity/Assets/LighFight/Runtime','unity/Assets/LighFight/Core'],cwd=repo,text=True).splitlines()
 for path in paths:
  if path.endswith('.cs'):
   data=subprocess.check_output(['git','show','96f0222:'+path],cwd=repo);result[path.removeprefix('unity/')]=hashlib.sha256(data).hexdigest()
else:
 for part in ('Runtime','Core'):
  for path in (repo/'unity/Assets/LighFight'/part).rglob('*.cs'):result[str(path.relative_to(repo/'unity'))]=hashlib.sha256(path.read_bytes()).hexdigest()
if a.staged_project:
 actual={str(path.relative_to(a.staged_project)):hashlib.sha256(path.read_bytes()).hexdigest() for part in ('Runtime','Core') for path in (a.staged_project/'Assets/LighFight'/part).rglob('*.cs')}
 if actual!=result:raise SystemExit('FAIL: staged runtime/core differ from requested exact source set')
print(json.dumps(dict(mode=a.mode,baselineCommit='96f0222',files=result),indent=2,sort_keys=True))

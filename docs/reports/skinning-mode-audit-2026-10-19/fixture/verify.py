#!/usr/bin/env python3
"""Refuse stale source/owned inputs immediately before each named build."""
import hashlib,json
from pathlib import Path
repo=Path(__file__).resolve().parents[4];owned=repo/'.tools/skinning-current100k-review';packet=Path(__file__).resolve().parent
h=lambda f:hashlib.sha256(f.read_bytes()).hexdigest()
snapshot=owned/'current-input-snapshot.json';m=json.loads(snapshot.read_text());bad=[]
for name,sha in m['files'].items():
 main=repo/'unity'/name;copy=owned/name
 if not main.is_file() or h(main)!=sha:bad.append('main changed: '+name)
 if name=='Packages/manifest.json':
  expected=json.loads(main.read_text());expected['dependencies'].update(m['packageDelta'])
  if json.loads(copy.read_text())!=expected:bad.append('owned package delta')
 elif name=='Packages/packages-lock.json':
  expected=json.loads(main.read_text());expected['dependencies']['com.unity.modules.screencapture']={'version':'1.0.0','depth':0,'source':'builtin','dependencies':{'com.unity.modules.imageconversion':'1.0.0'}}
  actual=json.loads(copy.read_text())
  if actual not in [json.loads(main.read_text()),expected]:bad.append('unexpected resolved package-lock delta')
 elif not copy.is_file() or h(copy)!=sha:bad.append('owned changed: '+name)
for name,sha in m['fixtureFiles'].items():
 source=packet/name;copy=owned/'Assets/CurrentSkinningReview'/name
 if h(source)!=sha or h(copy)!=sha:bad.append('fixture changed: '+name)
if bad:raise SystemExit('\n'.join(bad))
(owned/'current-source-verified.txt').write_text(h(snapshot)+'\n');print('PASS full audited source/asset/import/settings/fixture closure; build permit written')

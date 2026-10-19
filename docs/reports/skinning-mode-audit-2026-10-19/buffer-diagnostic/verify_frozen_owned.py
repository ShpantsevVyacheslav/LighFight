#!/usr/bin/env python3
"""Explicit frozen RED-snapshot scope: validate owned bytes, report main drift."""
import hashlib,json
from pathlib import Path
repo=Path(__file__).resolve().parents[4];owned=repo/'.tools/skinning-current100k-review';packet=Path(__file__).resolve().parent
h=lambda p:hashlib.sha256(p.read_bytes()).hexdigest()
snapshot=owned/'current-input-snapshot.json';m=json.loads(snapshot.read_text());extras=json.loads((owned/'frozen-resolved-packages.json').read_text());bad=[];drift=[]
for name,sha in m['files'].items():
 target=owned/name;current=repo/'unity'/name
 if not current.is_file() or h(current)!=sha:drift.append(name)
 expected=extras.get(name,sha)
 if not target.is_file() or h(target)!=expected:bad.append(name)
for name,sha in m['fixtureFiles'].items():
 target=owned/'Assets/CurrentSkinningReview'/name
 if not target.is_file() or h(target)!=sha:bad.append('original fixture changed: '+name)
for name,sha in json.loads((owned/'buffer-source-manifest.json').read_text()).items():
 if h(owned/name)!=sha:bad.append('V3 buffer helper changed: '+name)
if bad:raise SystemExit('\n'.join(bad))
result={'status':'PASS frozen owned bytes; NOT newest production baseline','sourceSnapshotSHA':h(snapshot),'mainDriftPaths':drift,'policy':'Root explicitly authorizes retained original RED scope; never rewrite original history'}
(packet/'frozen-owned-verification.json').write_text(json.dumps(result,indent=2)+'\n');(owned/'buffer-frozen-source-verified.txt').write_text(h(snapshot)+'\n');print(json.dumps(result))

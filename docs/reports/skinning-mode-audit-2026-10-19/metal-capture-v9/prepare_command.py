#!/usr/bin/env python3
"""Print future V9 command/env only. NEVER executes player or changes parent env."""
import argparse,json
from pathlib import Path
p=argparse.ArgumentParser();p.add_argument('--app',type=Path,required=True);p.add_argument('--trace',type=Path,required=True);p.add_argument('--source-manifest',type=Path,required=True);a=p.parse_args()
if 'V8' in str(a.app):raise SystemExit('Frozen V8 is not capture-enabled; new V9 source identity required')
if a.trace.suffix!='.gputrace' or a.trace.exists():raise SystemExit('Unique new .gputrace path required')
import hashlib
print(json.dumps({'status':'PLAN ONLY; no process launched','argv':[str(a.app.resolve()),'-forceGfxMetal','-screen-width','800','-screen-height','1000','-screen-fullscreen','0','-lfMetalTracePath',str(a.trace.resolve())],'childEnvironmentAdditions':{'METAL_CAPTURE_ENABLED':'1'},'sourceManifest':str(a.source_manifest.resolve()),'sourceManifestSHA256':hashlib.sha256(a.source_manifest.read_bytes()).hexdigest(),'budgets':{'buildSeconds':240,'playerSeconds':180,'minimumFreeGiB':8},'required':'Root heavy handoff; separately compiled/identified V9; V8 correctness review; startup+dynamic-pose evidence; offline Xcode inspection'},indent=2))

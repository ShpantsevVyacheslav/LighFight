#!/usr/bin/env python3
"""Read-only native trace metadata inventory; never opens/runs an engine."""
import argparse,json,plistlib,hashlib
from pathlib import Path
p=argparse.ArgumentParser();p.add_argument('trace',type=Path);p.add_argument('--out',type=Path,required=True);a=p.parse_args();trace=a.trace.resolve();files={str(f.relative_to(trace)):{'bytes':f.stat().st_size,'sha256':hashlib.sha256(f.read_bytes()).hexdigest()} for f in trace.rglob('*') if f.is_file()};m=plistlib.loads((trace/'metadata').read_bytes());reasons=[]
if m.get('DYCaptureSession.deviceId',0)==0:reasons.append('deviceId0')
if not (trace/'capture').is_file() or (trace/'capture').stat().st_size<=8:reasons.append('capture stream absent/<=8bytes')
if m.get('DYCaptureEngine.captured_frames_count',0)<1:reasons.append('no captured frames')
if sum(x['bytes'] for x in files.values())>1024**3:reasons.append('trace exceeds1GiB')
r={'status':'REJECTED command evidence' if reasons else 'METADATA PLAUSIBLE ONLY; actual Xcode compute/draw attribution still required','reasons':reasons,'metadata':m,'files':files};a.out.write_text(json.dumps(r,indent=2,default=str)+'\n');print(r['status'],reasons)

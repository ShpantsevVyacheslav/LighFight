#!/usr/bin/env python3
import argparse,csv,json
from pathlib import Path
p=argparse.ArgumentParser();p.add_argument('project',type=Path);a=p.parse_args();project=a.project
fail=[];report={}
if (project/'ThermalRosterBuilds/assets-baseline.sha256').read_bytes()!=(project/'ThermalRosterBuilds/assets-current.sha256').read_bytes():fail.append('Artist/import/graphics input SHA lists differ')
for mode in ('baseline','current'):
 folder=project/'ThermalRosterEvidence'/mode;result=json.loads((folder/'result.json').read_text());manifest=json.loads((folder/'build-manifest.json').read_text())
 if not result['completed'] or result['failed'] or result['errors'] or result['target']!=60:fail.append(mode+': runtime gate failed')
 report[mode]={r['stage']:r for r in csv.DictReader((folder/'summary.csv').open())}
 report[mode]['manifest']=manifest
 for stage in ('roster-initial','preview','roster-return','fight'):
  samples=list(csv.DictReader((folder/(stage+'.csv')).open()))
  if len(samples)!=200 or not (folder/(stage+'.png')).exists():fail.append(mode+': missing full window/image '+stage)
for stage in ('roster-initial','roster-return'):
 r=report['baseline'][stage]
 if int(r['sceneRenderCount'])==0 and int(r['actorLateTicks'])==0 and float(r['clockDelta'])==0 and int(r['selectionRTBytes'])==0:fail.append('Baseline lacks expected hidden-work negative control: '+stage)
if report['baseline']['manifest']['meshDeformation']!=report['current']['manifest']['meshDeformation']:fail.append('Different skinning settings')
out=dict(status='FAIL' if fail else 'PASS_PENDING_SCREENSHOT_REVIEW',failures=fail,data=report,scope='Mac60fps work/restoration A/B, no power/temperature measurement')
(project/'ThermalRosterEvidence/comparison.json').write_text(json.dumps(out,indent=2));print(json.dumps(dict(status=out['status'],failures=fail),indent=2));raise SystemExit(bool(fail))

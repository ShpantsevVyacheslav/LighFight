"""Summarize actual diagnostic outputs without assigning skin acceptance."""
import argparse,csv,json
from pathlib import Path
p=argparse.ArgumentParser();p.add_argument('reports',nargs='+',type=Path);p.add_argument('--output',required=True,type=Path);a=p.parse_args();own=Path(__file__).resolve().parents[1];out=a.output.resolve();assert out.is_relative_to(own);out.parent.mkdir(parents=True,exist_ok=True)
rows=[]
for path in a.reports:
 report=json.loads(path.read_text())
 for c in report['cases']:
  for region in c['OFF'].get('regions',{}):
   for variant in ['OFF','ON']:
    r=c[variant]['regions'][region];e=r['edge'];t=r['triangle'];rows.append(dict(case=c['case']['name'],clip=c['case']['clip'],phase=c['case']['phase'],region=region,variant=variant,edgeCount=e['edgeCount'],edgeRatioP99=e['ratio']['p99'],edgeRatioMax=e['ratio']['max'],absLogStrainP99=e['logStrainAbs']['p99'],absLogStrainMax=e['logStrainAbs']['max'],areaRatioP99=t['areaRatio']['p99'],collapsed=t['collapsedCount'],opposedTransportNormals=t['opposedTransportNormalCount'],maskStatus=report['maskStatus']))
assert rows,'No actual metric rows'
with out.open('w',newline='') as f:
 writer=csv.DictWriter(f,fieldnames=list(rows[0]));writer.writeheader();writer.writerows(rows)
print(json.dumps({'status':'DIAGNOSTIC_ONLY','rows':len(rows),'output':str(out)}))

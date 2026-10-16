import csv,json,argparse
from pathlib import Path
REQUIRED=('jab','frontKick','jumpKick','sideKick')
def validate(rows,hits,all_ids):
 problems=[];mapping={row['id']:row for row in rows}
 for name in REQUIRED:
  if name not in mapping:problems.append('missing '+name);continue
  row=mapping[name];index=int(row['index'])
  if index<0 or index>=len(all_ids) or all_ids[index]!=name:problems.append('index/ID mismatch '+name)
  if int(row['observedStarts'])<3 or int(row['activeFrames'])<3:problems.append('actual active coverage insufficient '+name)
 if hits<=0:problems.append('no actual hit serial changes')
 return {'passed':not problems,'problems':problems,'requiredIds':list(REQUIRED),'rows':rows,'hitsObserved':hits}
def main():
 ap=argparse.ArgumentParser();ap.add_argument('run',type=Path);a=ap.parse_args();props=lambda p:dict(x.split('=',1) for x in p.read_text().splitlines() if '=' in x)
 config=props(a.run/'configuration.txt');coverage=props(a.run/'coverage-result.txt');result=validate(list(csv.DictReader((a.run/'coverage.csv').open())),int(coverage['hitsObserved']),config['allMoveIds'].split(';'));(a.run/'coverage-acceptance.json').write_text(json.dumps(result,indent=2)+'\n');print(json.dumps(result,indent=2));raise SystemExit(0 if result['passed'] else 1)
if __name__=='__main__':main()

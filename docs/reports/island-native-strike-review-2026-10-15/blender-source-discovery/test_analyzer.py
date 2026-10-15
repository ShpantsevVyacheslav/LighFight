"""Regression: Chest must win over UpperChest before unique suffix fallback."""
import tempfile,csv,subprocess,sys,json
from pathlib import Path
root=Path('/Users/macbook1/Documents/ChatGPT/LighFight')
source=root/'.tools/tripo-humanoid-review-unity/IslandNativeStrikeReviewPacket/analyze_trajectories.py'
def probe(script):
 with tempfile.TemporaryDirectory() as d:
  p=Path(d)
  for name in ['box_01','box_02','front_kick_01']:
   with (p/(name+'.trajectory.csv')).open('w') as f:
    w=csv.writer(f);w.writerow(['seconds','joint','x','y','z'])
    for t in range(7):
     for joint,y in [('Chest',1),('UpperChest',1.2),('Hips',.5),('LeftHand',1),('RightHand',1),('LeftFoot',0),('RightFoot',0)]:w.writerow([t/10,joint,.1*t,y,0])
  subprocess.run([sys.executable,str(script),str(p)],check=True,capture_output=True)
  return [x['center'] for x in json.loads((p/'candidate-windows.UNACCEPTED.json').read_text())]
assert probe(source)==['chest']*3
with tempfile.TemporaryDirectory() as d:
 p=Path(d)/'mutation.py';s=source.read_text();block="exact=[n for n in joints if n==suffix]\n  if len(exact)==1:return exact[0]\n  ";assert block in s;p.write_text(s.replace(block,''));assert probe(p)==['hips']*3
print('PASS exact Chest priority all3takes; original ambiguous-suffix mutation RED all3')

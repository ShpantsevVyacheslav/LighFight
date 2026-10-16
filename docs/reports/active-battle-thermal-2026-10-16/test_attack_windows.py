import csv,tempfile,unittest
from pathlib import Path
from attack_windows import summarize
class Windows(unittest.TestCase):
 def run_case(self,index=0):
  with tempfile.TemporaryDirectory() as directory:
   path=Path(directory)
   (path/'configuration.txt').write_text('allMoveIds=jumpKick\nallMoveTimings=jumpKick:2:2:3\nposeMarker=True\n')
   with (path/'frames.csv').open('w') as out:
    writer=csv.DictWriter(out,fieldnames=['moveAIndex','moveAFrame','moveBIndex','downA','wallMs','poseMs']);writer.writeheader()
    for phase in (0,2,4):writer.writerow(dict(moveAIndex=index,moveAFrame=phase,moveBIndex=-1,downA=0,wallMs=16.7,poseMs=phase))
   return summarize(path)
 def test_observed_attack_phases_and_valid_markers(self):
  result=self.run_case();windows={r['phase']:r for r in result['windows']}
  self.assertEqual(set(windows),{'startup','active','recovery'})
  self.assertEqual(windows['active']['inclusiveCpuMarkersMs']['pose']['median'],2)
  self.assertIsNone(windows['active']['inclusiveCpuMarkersMs']['main'])
 def test_unrecognized_actual_index_refuses_mislabelled_evidence(self):
  with self.assertRaises(ValueError):self.run_case(8)
if __name__=='__main__':unittest.main()

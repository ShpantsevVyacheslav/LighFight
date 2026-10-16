import unittest
from diagnostic_summary import summarize
class DiagnosticTests(unittest.TestCase):
 def row(self):
  r={m+s:'0' for m in ['main','render','wait','ground','cap','pose','contact'] for s in ['Blocks','Ms']}
  for a in 'AB':r.update({'minEvalDelta'+a:'1','supportPoints'+a:'40','supportKind'+a:'1','rawMinimum'+a:'-.01','correction'+a:'.01'})
  return r
 def test_negative_counter_rejected(self):
  r=self.row();r['minEvalDeltaA']='-1'
  with self.assertRaises(ValueError):summarize([r])
 def test_missing_support_rejected(self):
  r=self.row();r['supportPointsB']='0'
  with self.assertRaises(ValueError):summarize([r])
 def test_zero_blocks_not_cpu_proof(self):
  s=summarize([self.row()]);self.assertEqual(s['markers']['render']['observedBlockFrames'],0)
 def test_body_and_exact_floor(self):
  r=self.row();r['supportKindA']='2';s=summarize([r]);self.assertEqual(s['actors']['A']['bodyMode']['2']['frames'],1);self.assertEqual(s['actors']['A']['supportFloorAfterCorrectionM']['worst'],0)
if __name__=='__main__':unittest.main()

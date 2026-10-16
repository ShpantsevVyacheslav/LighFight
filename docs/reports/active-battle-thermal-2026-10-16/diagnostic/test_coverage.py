import unittest
from coverage_gate import validate,REQUIRED
class Coverage(unittest.TestCase):
 def rows(self):return [dict(id=name,index=str(i),scheduled='10',observedStarts='3',startupFrames='12',activeFrames='3',recoveryFrames='12') for i,name in enumerate(REQUIRED)]
 def test_scheduled_only_is_red(self):
  rows=self.rows()
  for row in rows:row.update(observedStarts='0',activeFrames='0')
  self.assertFalse(validate(rows,3,list(REQUIRED))['passed'])
 def test_actual_all_four_is_green(self):self.assertTrue(validate(self.rows(),2,list(REQUIRED))['passed'])
 def test_missing_jump_is_red(self):self.assertFalse(validate([r for r in self.rows() if r['id']!='jumpKick'],2,list(REQUIRED))['passed'])
 def test_reordered_indices_are_red(self):self.assertFalse(validate(self.rows(),2,list(reversed(REQUIRED)))['passed'])
 def test_no_physical_hits_is_red(self):self.assertFalse(validate(self.rows(),0,list(REQUIRED))['passed'])
if __name__=='__main__':unittest.main()

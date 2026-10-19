import unittest
import numpy as np
from mixture_kernel import orientation_witnesses,bounded_attenuation
class OrientationTests(unittest.TestCase):
 def test_opposed_not_selfintersection_claim(self):
  base=np.array([[[0.,0,0],[1,0,0],[0,1,0]]]);out=base.copy();out[0,2,1]=-1
  r=orientation_witnesses(base,out);self.assertEqual(r['opposed'],[0]);self.assertEqual(r['newCollapsed'],[])
 def test_existing_zero_area_not_new_collapse(self):
  a=np.zeros((1,3,3));r=orientation_witnesses(a,a);self.assertEqual(r['newCollapsed'],[]);self.assertEqual(r['opposed'],[])
 def test_smooth_bounded_local_field(self):
  f=np.array([0.,.5,.5,.5]);out=bounded_attenuation(f,np.array([0.,0.,.003,.006]),.006,np.array([1,0,0,0],bool));self.assertEqual(out[0],0);self.assertEqual(out[1],.25);self.assertEqual(out[-1],.5);self.assertTrue(np.all((out>=0)&(out<=f)))
if __name__=='__main__':unittest.main()

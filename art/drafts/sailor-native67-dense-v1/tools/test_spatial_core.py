import unittest
import numpy as np
from spatial_core import reconstruct
class SpatialTests(unittest.TestCase):
 def test_retains_tiny_and_more_than_eight(self):
  b=np.zeros((1,12));b[0,0]=1;w=np.broadcast_to(np.eye(12)[None],(1,12,12)).copy();w[0,0,11]=1e-20;w[0,0,0]-=1e-20
  r,s,a=reconstruct(b,w,np.arange(1,13)[None].astype(float),10,[])
  self.assertGreater(np.count_nonzero(r),8);self.assertGreater(r[0,11],0);self.assertAlmostEqual(float(r.sum()),1,places=6)
 def test_stable_mask_and_continuous_transition(self):
  b=np.array([[1.,0],[.85,.15],[.7,.3]]);w=np.tile([[[0.,1.],[1.,0.],[1.,0.]]],(3,1,1));r,s,a=reconstruct(b,w,np.tile([1.,2.,3.],(3,1)),1,[0]);np.testing.assert_array_equal(r[0],b[0]);np.testing.assert_allclose(a,[0,.5,1]);self.assertTrue(np.all(r>=0))
 def test_underflow_fails_closed(self):
  b=np.array([[1.,0.]]);w=np.array([[[1.,1e-100],[1.,0],[1.,0]]]);
  with self.assertRaisesRegex(AssertionError,'underflow'):reconstruct(b,w,np.array([[1.,2.,3.]]),1,[])
 def test_zero_support_rejected(self):
  with self.assertRaises(AssertionError):reconstruct(np.ones((1,1)),np.ones((1,3,1)),np.zeros((1,3)),1,[])
if __name__=='__main__':unittest.main()

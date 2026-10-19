import unittest
import numpy as np
from corrective_core import inverse_lbs_delta,compact_window,radial_contour_delta
class CorrectiveTests(unittest.TestCase):
 def test_inverse_recovers_world_delta_with_rotation_scale(self):
  L=np.array([[[0,-2,0],[1,0,0],[0,0,.5]],np.diag([.7,1.2,2])]);d=np.array([[.001,.002,-.001],[-.002,.001,.001]]);x=inverse_lbs_delta(L,d);self.assertTrue(np.allclose(np.einsum('nij,nj->ni',L,x),d,atol=1e-15))
 def test_singular_or_nonfinite_rejected_not_silently_skipped(self):
  with self.assertRaises(ValueError):inverse_lbs_delta(np.array([np.diag([1,1,0])]),np.ones((1,3)))
  with self.assertRaises(ValueError):inverse_lbs_delta(np.eye(3)[None],np.array([[np.nan,0,0]]))
 def test_mask_anchors_and_cap_are_exact(self):
  p=np.array([[0.,0,0],[.01,0,0],[.02,0,0]]);axis=np.array([0,0,1.]);q=radial_contour_delta(p,[0,0,0],axis,[1,0,0],np.array([.03,.03,.03]),np.array([0,1,1.]),.003);self.assertTrue(np.array_equal(q[0],[0,0,0]));self.assertLessEqual(np.linalg.norm(q,axis=1).max(),.003+1e-15);self.assertTrue(np.array_equal(p,np.array([[0.,0,0],[.01,0,0],[.02,0,0]])))
 def test_no_shrink_and_direction_is_radial(self):
  p=np.array([[.04,0,0],[.01,0,0]]);d=radial_contour_delta(p,[0,0,0],[0,0,1],[1,0,0],np.array([.03,.03]),np.ones(2),.003);self.assertTrue(np.array_equal(d[0],[0,0,0]));self.assertGreater(d[1,0],0);self.assertEqual(d[1,1],0)
 def test_compact_window_has_zero_boundary_and_flat_derivative(self):
  x=np.array([-1.,0,1e-6,.5,1-1e-6,1,2]);w=compact_window(x,0,1);self.assertTrue(np.array_equal(w[[0,1,5,6]],np.zeros(4)));self.assertEqual(w[3],1);self.assertLess(w[2],1e-9);self.assertLess(w[4],1e-9)
if __name__=='__main__':unittest.main()

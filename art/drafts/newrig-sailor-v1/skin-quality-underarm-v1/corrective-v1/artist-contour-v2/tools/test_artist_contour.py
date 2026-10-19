import unittest,numpy as np
from artist_contour_probe import compact_kernel,interpolate_field,swept_volume,balanced_coefficient,garment_activation,zero_boundary_vertices
class ArtistTests(unittest.TestCase):
 def test_compact_kernel_zero_outside(self):
  self.assertTrue(np.array_equal(compact_kernel(np.array([1.,2.])),[0,0]));self.assertEqual(compact_kernel(np.array([0.]))[0],1)
 def test_explicit_handles_interpolate_exactly(self):
  c=np.array([[0.,0,0],[.02,0,0],[.04,0,0]]);d=np.array([[0,0,.001],[0,0,.003],[0,0,.001]]);self.assertTrue(np.allclose(interpolate_field(c,c,d,.03),d,atol=1e-14))
 def test_duplicate_handles_fail_closed(self):
  with self.assertRaises(ValueError):interpolate_field(np.zeros((1,3)),np.zeros((2,3)),np.ones((2,3)),.02)
 def test_swept_volume_tetra_known_scale(self):
  p=np.array([[0.,0,0],[1,0,0],[0,1,0],[0,0,1]]);t=np.array([[0,2,1],[0,1,3],[0,3,2],[1,2,3]]);self.assertAlmostEqual(swept_volume(p,p*2,t),7/6)
 def test_compensation_no_bracket_rejects(self):
  with self.assertRaises(ValueError):balanced_coefficient(lambda x:x*x+1,1e-12)
 def test_compensation_solves_scalar_without_waiver(self):
  a=balanced_coefficient(lambda x:x-.25,1e-12);self.assertAlmostEqual(a,.25)
 def test_pose_driver_exact_other_clip_zero(self):
  for clip in ['box_02','front_kick_01','neutral']:self.assertEqual(garment_activation(clip,[0,90],[0,90],.5,23),0)
  self.assertEqual(garment_activation('dance_01',[0,90],[0,90],.5,23),1)
 def test_pose_driver_C1_entry_and_pose_support(self):
  self.assertEqual(garment_activation('dance_01',[15,90],[0,90],.5,23),0);self.assertLess(garment_activation('dance_01',[15-1e-5,90],[0,90],.5,23),1e-18)
  self.assertEqual(garment_activation('dance_01',[0,90],[0,90],0,23),0);self.assertLess(garment_activation('dance_01',[0,90],[0,90],1e-9,23),1e-12)
 def test_expansion_uses_excluded_zero_nodes_never_protected_cloth(self):
  allowed=np.array([True,True,False,False]);tri=np.array([[0,1,2],[1,3,2]]);boundary=zero_boundary_vertices(tri,allowed);self.assertTrue(np.array_equal(boundary,[2,3]));self.assertFalse(allowed[boundary].any())
if __name__=='__main__':unittest.main()

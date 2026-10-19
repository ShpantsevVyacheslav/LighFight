import unittest
import numpy as np
from skin_metrics import *
class MetricsTests(unittest.TestCase):
 def setUp(self):
  self.p=np.array([[0.,0,0],[1,0,0],[0,1,0],[1,1,0]])
  self.t=np.array([[0,1,2],[1,3,2]],np.int32)
 def test_rigid_rotation_does_not_change_strain_or_area(self):
  R=np.array([[0,0,1],[0,1,0],[-1,0,0.]])
  q=self.p@R.T+[3,4,5];e=edge_metrics(self.p,q,canonical_edges(self.t));f=triangle_metrics(self.p,q,self.t,expected_normals=np.tile(R@np.array([0.,0,1]),(2,1)))
  self.assertAlmostEqual(e['ratio']['p99'],1);self.assertAlmostEqual(f['areaRatio']['min'],1);self.assertEqual(f['opposedTransportNormalCount'],0)
 def test_known_scale_and_collapsed_triangle_are_detected(self):
  e=edge_metrics(self.p,self.p*2,canonical_edges(self.t));f=triangle_metrics(self.p,self.p*2,self.t)
  self.assertAlmostEqual(e['ratio']['max'],2);self.assertAlmostEqual(f['areaRatio']['max'],4)
  q=self.p.copy();q[2]=q[1];f=triangle_metrics(self.p,q,self.t);self.assertGreater(f['collapsedCount'],0);self.assertFalse(f['orientationAvailable'])
 def test_worst_tail_blocks_mean_only_false_fix(self):
  before={'logStrainAbs':{'p99':.2,'max':.3},'collapsedCount':0};after={'logStrainAbs':{'p99':.1,'max':.8},'collapsedCount':0}
  r=candidate_fences(before,after,{'maxM':0},True);self.assertFalse(r['pass']);self.assertIn('worst_strain_worsened',r['failures'])
 def test_region_boundary_edges_retain_original_vertex_pairs(self):
  edges=canonical_edges(self.t);m=np.array([True,False,False,False]);r=edge_metrics(self.p,self.p,edges,m)
  self.assertEqual(r['edgeCount'],2);self.assertEqual(set(map(tuple,r['worstEdgesVertexPairs'])),{(0,1),(0,2)})
 def test_reversed_triangle_flags_witness_not_claimed_self_intersection(self):
  q=self.p.copy();q[[1,2]]=q[[2,1]];f=triangle_metrics(self.p,q,self.t[:1],expected_normals=np.array([[0,0,1.]]))
  self.assertEqual(f['opposedTransportNormalCount'],1);self.assertEqual(f['interpretation'],'orientation witness; not a self-intersection proof')
 def test_closed_square_area_and_open_section_unavailable(self):
  a=np.array([[[0,0,0],[1,0,0]],[[1,0,0],[1,1,0]],[[1,1,0],[0,1,0]],[[0,1,0],[0,0,0.]]]);r=closed_section(a,[0,0,0],[0,0,1]);self.assertTrue(r['available']);self.assertAlmostEqual(r['areaM2'],1)
  self.assertFalse(closed_section(a[:-1],[0,0,0],[0,0,1])['available'])
 def test_multiple_or_crossing_rings_cannot_fake_volume(self):
  a=np.array([[[0,0,0],[1,1,0]],[[1,1,0],[0,1,0]],[[0,1,0],[1,0,0]],[[1,0,0],[0,0,0.]]]);self.assertFalse(closed_section(a,[0,0,0],[0,0,1])['available'])
  square=np.array([[[0,0,0],[1,0,0]],[[1,0,0],[1,1,0]],[[1,1,0],[0,1,0]],[[0,1,0],[0,0,0.]]]);self.assertFalse(closed_section(np.concatenate([square,square+[3,0,0]]),[0,0,0],[0,0,1])['available'])
 def test_unreviewed_masks_and_anchor_motion_fail_closed(self):
  baseline={'logStrainAbs':{'p99':.2,'max':.3},'collapsedCount':0};self.assertFalse(candidate_fences(baseline,baseline,{'maxM':0},False)['pass'])
  q=self.p.copy();q[1,0]+=.002;a=anchor_metrics(self.p,q,np.array([1]));self.assertAlmostEqual(a['maxM'],.002);self.assertFalse(candidate_fences(baseline,baseline,a,True)['pass'])
 def test_projection_envelope_preserves_ids_and_known_shift(self):
  a=projected_envelope(self.p,[0,1,2,3],[0,0,0],[1,0,0],[0,1,0],[0,1],2);b=projected_envelope(self.p+[.2,0,0],[0,1,2,3],[0,0,0],[1,0,0],[0,1,0],[0,1],2)
  self.assertEqual(a['bins'][0]['minOriginalVertexID'],0);self.assertAlmostEqual(b['bins'][1]['maxX']-a['bins'][1]['maxX'],.2)
 def test_nonuniform_scale_transports_face_normal_by_cofactor(self):
  a=np.array([np.diag([2.,3,4])]);n=transport_normals(a,np.array([[0.,0,1]]));self.assertTrue(np.allclose(n,[[0,0,6]]))
 def test_input_normal_is_never_mutated(self):
  normal=np.array([0.,0,2.]);section_segments(self.p,self.t,[0,0,0],normal);self.assertTrue(np.array_equal(normal,[0,0,2]))
 def test_plane_section_uses_real_triangles(self):
  p=np.array([[0.,0,-1],[1,0,1],[0,1,1]]);s=section_segments(p,np.array([[0,1,2]]),[0,0,0],[0,0,1]);self.assertEqual(s.shape,(1,2,3));self.assertTrue(np.allclose(s[:,:,2],0))
if __name__=='__main__':unittest.main()

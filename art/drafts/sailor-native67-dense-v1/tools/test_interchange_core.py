import unittest,numpy as np
from native67_interchange_core import csr_support_bounds,error_bound,source_id_uv
class BoundsTests(unittest.TestCase):
 def test_tiny_positive_included_in_support(self):
  p=np.array([[0.,0,0],[100.,0,0]]);ptr=np.array([0,1,3]);ix=np.array([0,0,1]);w=np.array([1.,1e-18,1.]);c,a,s=csr_support_bounds(p,ptr,ix,w,2);self.assertEqual(c[0,:,0].max(),100.);self.assertTrue(a.all())
 def test_bound_contains_actual_weighted_error(self):
  rng=np.random.default_rng(77);p=rng.normal(size=(100,3));ptr=np.arange(0,201,2);ix=np.tile([0,1],100);w=np.tile([.2,.8],100);c,a,s=csr_support_bounds(p,ptr,ix,w,2);source=np.tile(np.eye(4),(2,1,1));candidate=source+rng.normal(scale=.001,size=(2,4,4));bound=error_bound(source,candidate,c,a,s);hp=np.c_[p,np.ones(100)];actual=np.linalg.norm(np.einsum('bij,nj,b->ni',candidate-source,hp,[.2,.8])[:,:3],axis=1).max();self.assertGreaterEqual(bound,actual)
class ProvenanceTests(unittest.TestCase):
 def test_all994452_ids_roundtrip_without_epsilon(self):
  ids=np.arange(994452,dtype=np.int32);uv=source_id_uv(ids,len(ids));np.testing.assert_array_equal(uv[:,0]*1048576,ids);self.assertEqual(len(np.unique(uv[:,0])),len(ids));self.assertTrue(np.all(uv[:,1]==0))
 def test_invalid_id_or_capacity_rejected(self):
  for ids,total in [(np.array([-1]),5),(np.array([5]),5),(np.array([0]),1048577)]:
   with self.assertRaises(AssertionError):source_id_uv(ids,total)
class ClosedTriangleTests(unittest.TestCase):
 def test_interior_edges_vertices_and_offplane(self):
  from native67_interchange_core import closest_barycentric_closed
  p=np.array([[-1,-1,1],[.2,.2,3],[1,1,2],[2,-.2,.1],[.5,-1,0]],float);a=np.zeros_like(p);b=a+[1,0,0];c=a+[0,1,0];bb,q=closest_barycentric_closed(p,a,b,c)
  np.testing.assert_allclose(q,[[0,0,0],[.2,.2,0],[.5,.5,0],[1,0,0],[.5,0,0]],atol=1e-14);self.assertTrue((bb>=0).all());np.testing.assert_allclose(bb.sum(1),1,atol=1e-14)
 def test_degenerate_triangle_rejected(self):
  from native67_interchange_core import closest_barycentric_closed
  with self.assertRaises(AssertionError):closest_barycentric_closed(np.zeros((1,3)),np.zeros((1,3)),np.ones((1,3)),np.ones((1,3))*2)
if __name__=='__main__':unittest.main()

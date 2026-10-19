import unittest
import numpy as np
from mixture_kernel import harmonic,mix
class KernelTests(unittest.TestCase):
 def test_chain_dirichlet(self):
  out,e=harmonic(5,np.array([[0,1],[1,2],[2,3],[3,4]]),np.ones(4),np.array([0,.9,.1,.8,1]),np.array([1,0,0,0,1],bool));np.testing.assert_allclose(out,np.linspace(0,1,5),atol=1e-12);self.assertTrue(e['fixedExactlyUnchanged'])
 def test_no_anchor_rejected(self):
  with self.assertRaises(ValueError):harmonic(2,np.array([[0,1]]),np.ones(1),np.array([0.,1.]),np.zeros(2,bool))
 def test_mixture_retains_tiny_and_fixed(self):
  w=np.array([[.5,.4,.1,1e-10],[.5,.3,.2,1e-12]]);arm=np.array([[1,1,0,0],[1,1,0,0]],bool);out=mix(w,arm,np.array([.1,.9]),.5,np.array([1,0],bool));np.testing.assert_array_equal(out[0],w[0]);np.testing.assert_allclose(out.sum(1),w.sum(1),atol=1e-15);self.assertTrue(np.all(out[:,3]>0))
 def test_missing_family_refused(self):
  with self.assertRaises(ValueError):mix(np.array([[1.,0,0,0]]),np.array([[1,1,0,0]],bool),np.array([.5]),.25,np.array([0],bool))
if __name__=='__main__':unittest.main()

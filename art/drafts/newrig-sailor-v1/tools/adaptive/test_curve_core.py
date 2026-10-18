import unittest
from fractions import Fraction
import numpy as np
from curve_core import Skeleton,InfluenceBounds,SourceSample,AdaptiveSampler,bone_angle_deg,compose,interpolate

def identity_trs(n):
 a=np.zeros((n,10));a[:,3]=1;a[:,7:10]=1;return a

class FullSurfaceBoundTests(unittest.TestCase):
 def test_full_weighted_surface_bound_including_joint_scales(self):
  rng=np.random.default_rng(903);p=rng.normal(size=(1200,3));j=rng.integers(0,5,size=(1200,4));w=rng.random((1200,4));w/=w.sum(1,keepdims=True);bound=InfluenceBounds.from_all_original_vertices(p,j,w,5);a=identity_trs(5);b=identity_trs(5);a[:,:3]=rng.normal(size=(5,3));b[:,:3]=a[:,:3]+rng.normal(scale=.01,size=(5,3));a[:,7:10]=rng.uniform(.4,2,size=(5,3));b[:,7:10]=a[:,7:10]+rng.normal(scale=.02,size=(5,3));A=compose(a);B=compose(b);P=np.column_stack((p,np.ones(len(p))));actual=np.linalg.norm(np.sum(np.einsum('nkij,nj->nki',(B-A)[j],P)*w[:,:,None],axis=1)[:,:3],axis=1).max();self.assertGreater(actual,0);self.assertLessEqual(actual,bound.error_bound(A,B)+1e-12)
 def test_world_reflection_does_not_change_bound_or_bone_angle(self):
  p=np.array([[1.,2,3],[-2,1,0]]);j=np.zeros((2,1),int);w=np.ones((2,1));bounds=InfluenceBounds.from_all_original_vertices(p,j,w,1);a=compose(identity_trs(1));t=identity_trs(1);t[0,3:7]=[np.cos(.2),0,0,np.sin(.2)];b=compose(t);F=np.diag([-1.,1,1,1]);self.assertAlmostEqual(bounds.error_bound(a,b),bounds.error_bound(F@a,F@b),12);self.assertAlmostEqual(bone_angle_deg(a,b),bone_angle_deg(F@a,F@b),10)
 def test_invalid_weights_and_unsupported_inheritance_rejected(self):
  with self.assertRaises(ValueError):InfluenceBounds.from_all_original_vertices(np.zeros((1,3)),np.zeros((1,1),int),np.zeros((1,1)),1)
  with self.assertRaises(ValueError):Skeleton(np.array([-1]),np.eye(4)[None],np.eye(4),np.eye(4),('FIX_SHEAR',))
 def test_absolute_bind_hierarchy_with_nonuniform_parent_scale(self):
  rest=np.repeat(np.eye(4)[None],2,axis=0);rest[0,:3,3]=[1,2,3];rest[1,:3,3]=[1,3,3];s=Skeleton(np.array([-1,0]),rest,np.eye(4),np.eye(4),('FULL','FULL'));t=identity_trs(2);t[0,7:10]=[2,3,4];world=s.world_pose(t);expected=rest[0]@compose(t)[0]@np.linalg.inv(rest[0])@rest[1];np.testing.assert_allclose(world[1],expected);np.testing.assert_allclose(s.skin(s.world_pose(identity_trs(2))),np.repeat(np.eye(4)[None],2,axis=0),atol=1e-14)

class QuaternionTests(unittest.TestCase):
 def test_antipodal_quaternions_interpolate_same_rotation(self):
  a=identity_trs(1);a[0,3:7]=[np.cos(.3),0,np.sin(.3),0];b=a.copy();b[:,3:7]*=-1
  np.testing.assert_allclose(compose(interpolate(a,b,.5)),compose(a),atol=1e-14)
 def test_zero_and_nonfinite_quaternions_rejected(self):
  a=identity_trs(1);bad=a.copy();bad[:,3:7]=0
  with self.assertRaises(ValueError):compose(bad)
  with self.assertRaises(ValueError):interpolate(a,bad,.5)
  bad=a.copy();bad[0,4]=np.nan
  with self.assertRaises(ValueError):compose(bad)

class AdaptiveTests(unittest.TestCase):
 def test_nonlinear_source_refined_and_independent_holdouts_pass(self):
  s=Skeleton(np.array([-1]),np.eye(4)[None],np.eye(4),np.eye(4),('FULL',));bounds=InfluenceBounds.from_all_original_vertices(np.array([[0.,0,0],[1,0,0]]),np.zeros((2,1),int),np.ones((2,1)),1);calls=[]
  def source(t):
   calls.append(t);a=identity_trs(1);a[0,0]=float(t)**2;world=s.world_pose(a);return SourceSample(a,world,s.skin(world))
  sampler=AdaptiveSampler(s,bounds,source,surface_limit=.001,angle_limit=.01);keys=sampler.fit([Fraction(0),Fraction(1)]);self.assertGreater(len(keys),2);self.assertEqual(len(calls),len(set(calls)))
  for t in np.linspace(0,1,1001):
   k=min(np.searchsorted([float(x) for x in keys],t,side='right')-1,len(keys)-2);k=max(0,k);a,b=keys[k:k+2];world=s.world_pose(interpolate(sampler.sample(a).local_trs,sampler.sample(b).local_trs,(t-float(a))/float(b-a)));truth=source(Fraction(float(t)));self.assertLessEqual(bounds.error_bound(truth.world_skin,s.skin(world)),.001+1e-12)
 def test_budget_limit_fails_instead_of_accepting_unresolved_curve(self):
  s=Skeleton(np.array([-1]),np.eye(4)[None],np.eye(4),np.eye(4),('FULL',));bounds=InfluenceBounds.from_all_original_vertices(np.zeros((1,3)),np.zeros((1,1),int),np.ones((1,1)),1)
  def source(t):
   a=identity_trs(1);a[0,0]=float(t)**2;world=s.world_pose(a);return SourceSample(a,world,s.skin(world))
  with self.assertRaises(RuntimeError):AdaptiveSampler(s,bounds,source,surface_limit=1e-12,max_depth=0).fit([0,1])

if __name__=='__main__':unittest.main()

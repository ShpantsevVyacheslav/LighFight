import math
import unittest
from audit_motion_distinctness import distance


def motion(axis):
    frames=[]
    for i in range(32):
        a=.6*math.sin(i*2*math.pi/32)
        q=[0.,0.,0.,math.cos(a/2)];q[axis]=math.sin(a/2)
        frames.append({str(j):q[:] for j in range(8)})
    return frames


class MotionAuditTests(unittest.TestCase):
    def test_same_clip_or_quaternion_sign_cannot_pass(self):
        a=motion(0)
        self.assertLess(distance(a,[{k:[-x for x in q] for k,q in f.items()} for f in a]),1e-5)

    def test_renamed_cyclic_offset_cannot_pass(self):
        a=motion(0)
        self.assertLess(distance(a,a[11:]+a[:11]),1e-5)

    def test_different_limb_axis_detected(self):
        self.assertGreater(distance(motion(0),motion(1)),20)

    def test_incomplete_joint_set_is_rejected(self):
        with self.assertRaises(ValueError): distance([{'hip':[0,0,0,1]}]*32,motion(0))


if __name__=='__main__': unittest.main()

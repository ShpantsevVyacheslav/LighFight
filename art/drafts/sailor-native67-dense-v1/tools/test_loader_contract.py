import unittest,types
import numpy as np
from source_loader_contract import validate,OUTER
class ContractTests(unittest.TestCase):
 def fixture(self):
  n={k:None for k in OUTER};n.update(nativeNames=list(map(str,range(67))),nativeJoint=np.empty((49992,4)),nativeWeight=np.empty((49992,4)),rawDm=np.empty((49992,3)),actual=lambda x:x,group_hash=lambda x:x,report={'nativeTrueBindWitness':{}},ns={'clips':[None]*8,'ref':{'positionsBlenderLocal':None},'clay':None});return n
 def test_correct_nested_contract(self):
  n=self.fixture();self.assertIs(validate(n),n['ns']);self.assertNotIn('clips',n)
 def test_outer_clip_does_not_hide_missing_nested(self):
  n=self.fixture();n['clips']=[None]*8;del n['ns']['clips']
  with self.assertRaisesRegex(AssertionError,'nested source keys'):validate(n)
 def test_wrong_source_counts_rejected(self):
  n=self.fixture();n['nativeWeight']=np.empty((49991,4))
  with self.assertRaises(AssertionError):validate(n)
 def test_missing_actual_function_rejected(self):
  n=self.fixture();del n['actual']
  with self.assertRaisesRegex(AssertionError,'outer keys'):validate(n)
if __name__=='__main__':unittest.main()

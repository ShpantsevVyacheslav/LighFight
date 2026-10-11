import copy, sys, unittest
from pathlib import Path
sys.path.insert(0,str(Path(__file__).resolve().parents[2]/'scripts/art'))
from native_fbx_compat import require_same_rig

class NativeFbxCompatibility(unittest.TestCase):
    def setUp(self):
        matrix=[1.,0,0,0,0,1.,0,0,0,0,1.,0,0,0,0,1.]
        self.rig={'__object_transform__':(None,matrix[:]),'Hips':(None,matrix[:]),'Spine':('Hips',matrix[:]),'HandIndex1':('Spine',matrix[:])}
    def test_same_native_rig_and_export_rounding(self):
        other=copy.deepcopy(self.rig);other['Spine'][1][3]+=1e-6
        require_same_rig(self.rig,other)
    def test_wrong_inventory_parent_rest_and_object_scale(self):
        other=copy.deepcopy(self.rig);del other['HandIndex1']
        with self.assertRaises(ValueError):require_same_rig(self.rig,other)
        other=copy.deepcopy(self.rig);other['HandIndex1']=('Hips',other['HandIndex1'][1])
        with self.assertRaises(ValueError):require_same_rig(self.rig,other)
        for bone,index,value in [('Spine',3,.02),('__object_transform__',0,100),('Hips',0,float('nan'))]:
            other=copy.deepcopy(self.rig);other[bone][1][index]=value
            with self.assertRaises(ValueError):require_same_rig(self.rig,other)
if __name__=='__main__':unittest.main()

import unittest,tempfile,struct,json,importlib.util,hashlib
from pathlib import Path
import numpy as np
spec=importlib.util.spec_from_file_location('adapter',Path(__file__).with_name('assemble.py'));a=importlib.util.module_from_spec(spec);spec.loader.exec_module(a)
class AdapterGate(unittest.TestCase):
 def test_hierarchy_rotation_uniform_scale_and_payload(self):
  with tempfile.TemporaryDirectory() as d:
   path=Path(d)/'box.glb';points=[[-1,-2,-1],[1,-2,-1],[1,2,-1],[-1,2,-1],[-1,-2,1],[1,-2,1],[1,2,1],[-1,2,1]];idx=[0,1,2,0,2,3];raw=struct.pack('<24f6I',*[x for p in points for x in p],*idx)
   g={'asset':{'version':'2.0'},'bufferViews':[{'buffer':0,'byteOffset':0,'byteLength':96},{'buffer':0,'byteOffset':96,'byteLength':24}],'accessors':[{'bufferView':0,'componentType':5126,'count':8,'type':'VEC3'},{'bufferView':1,'componentType':5125,'count':6,'type':'SCALAR'}],'meshes':[{'primitives':[{'attributes':{'POSITION':0},'indices':1}]}],'nodes':[{'translation':[3,4,5],'scale':[2,2,2],'children':[1]},{'mesh':0}],'scenes':[{'nodes':[0]}],'scene':0};a.write(path,g,raw)
   lo,hi,tris=a.world_bounds(path);np.testing.assert_allclose(lo,[1,0,3]);np.testing.assert_allclose(hi,[5,8,7]);self.assertEqual(tris,2)
   before=hashlib.sha256(path.read_bytes()).hexdigest();row={'id':'test','source':str(path),'heightMetres':.8,'yawDegrees':90,'reviewCenter':[9,10,11]};p,w,r=a.place(row,'review');np.testing.assert_allclose(np.mean(r['placedBounds'],axis=0),[9,10,11]);self.assertAlmostEqual(r['placedBounds'][1][1]-r['placedBounds'][0][1],.8)
   dest=Path(d)/'placed.glb';a.combine([(p,w)],dest);_,b=a.load(dest);self.assertEqual(b[:len(raw)],raw);self.assertEqual(before,hashlib.sha256(path.read_bytes()).hexdigest())
   instanced=Path(d)/'instanced.glb';_,unique,repeats=a.combine_instances([(p,w),(p,dict(w,name='second',translation=[20,0,0]))],instanced);ig,ib=a.load(instanced);self.assertEqual(len(ig['meshes']),1);self.assertEqual(len(ig['scenes'][0]['nodes']),2);self.assertEqual(len(unique),1);self.assertEqual(repeats,1);self.assertEqual(ib[:len(raw)],raw)
if __name__=='__main__':unittest.main()

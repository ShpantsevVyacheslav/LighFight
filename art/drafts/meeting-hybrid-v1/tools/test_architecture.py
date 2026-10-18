import importlib.util,tempfile,unittest,struct,math
from pathlib import Path
spec=importlib.util.spec_from_file_location('meeting_build',Path(__file__).with_name('build_portable.py'));m=importlib.util.module_from_spec(spec);spec.loader.exec_module(m)
class ArchitectureGate(unittest.TestCase):
 def test_actual_triangles_have_outward_normals_and_no_degeneracy(self):
  with tempfile.TemporaryDirectory() as d:
   p=Path(d)/'a.glb';m.architecture(p);g,b=m.load(p)
   def read(i,n):
    a=g['accessors'][i];v=g['bufferViews'][a['bufferView']];fmt='f' if a['componentType']==5126 else 'I';flat=struct.unpack_from('<'+str(a['count']*n)+fmt,b,v.get('byteOffset',0)+a.get('byteOffset',0));return [flat[j:j+n] for j in range(0,len(flat),n)]
   count=0
   for mesh in g['meshes']:
    for p in mesh['primitives']:
     pos=read(p['attributes']['POSITION'],3);norm=read(p['attributes']['NORMAL'],3);ids=[x[0] for x in read(p['indices'],1)]
     self.assertTrue(all(math.isfinite(x) for v in pos for x in v))
     for j in range(0,len(ids),3):
      a,c,e=[pos[ids[j+k]] for k in range(3)];u=[c[k]-a[k] for k in range(3)];v=[e[k]-a[k] for k in range(3)];cross=(u[1]*v[2]-u[2]*v[1],u[2]*v[0]-u[0]*v[2],u[0]*v[1]-u[1]*v[0]);self.assertGreater(sum(cross[k]*norm[ids[j]][k] for k in range(3)),0);count+=1
   self.assertGreater(count,500)
 def test_separate_opaque_ceiling_covers_floor_and_stays_above_interior(self):
  with tempfile.TemporaryDirectory() as d:
   p=Path(d)/'a.glb';m.architecture(p);g,_=m.load(p)
   byname={mesh['name']:mesh for mesh in g['meshes']}
   def bounds(name):return g['accessors'][byname[name]['primitives'][0]['attributes']['POSITION']]
   roof=bounds('Ceiling panels');floor=bounds('Light oak')
   self.assertGreater(roof['min'][1],3)
   for axis in [0,2]:
    self.assertLessEqual(roof['min'][axis],floor['min'][axis]);self.assertGreaterEqual(roof['max'][axis],floor['max'][axis])
   self.assertFalse(g['materials'][7].get('doubleSided',False))
 def test_repeated_build_has_no_accumulating_geometry(self):
  with tempfile.TemporaryDirectory() as d:
   a=m.architecture(Path(d)/'a.glb');b=m.architecture(Path(d)/'b.glb');self.assertEqual(a,b)
if __name__=='__main__':unittest.main()

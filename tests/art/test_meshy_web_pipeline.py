"""Regression tests: exact native geometry/weights and animation targets survive merge."""
import copy, json, struct, tempfile, unittest
from pathlib import Path
from scripts.art.combine_meshy_web_glb import read, combine, ROOT

class WebPipelineTests(unittest.TestCase):
 def setUp(self):
  self.folder=ROOT/'art/source/original-island'
 def test_exact_source_surface_and_skin_are_retained(self):
  with tempfile.TemporaryDirectory() as tmp:
   out=Path(tmp)/'combined.glb'
   combine(self.folder/'web-rig.glb',[('Guard',self.folder/'web-guard.glb'),('Jab',self.folder/'web-right-jab.glb')],out)
   source,original=read(self.folder/'web-rig.glb'); merged,binary=read(out)
   self.assertEqual(source['meshes'],merged['meshes'])
   self.assertEqual(source['skins'],merged['skins'])
   self.assertEqual(source['nodes'],merged['nodes'])
   # This includes positions, both weight sets, inverse binds, UVs and textures.
   self.assertEqual(original,binary[:len(original)])
   self.assertEqual(66,len(merged['skins'][0]['joints']))
   for index,name in enumerate(('web-guard.glb','web-right-jab.glb')):
    motion,raw=read(self.folder/name)
    for before,after in zip(motion['animations'][0]['channels'],merged['animations'][index]['channels']):
     self.assertEqual(motion['nodes'][before['target']['node']]['name'],merged['nodes'][after['target']['node']]['name'])
     self.assertEqual(before['target']['path'],after['target']['path'])
    for before,after in zip(motion['animations'][0]['samplers'],merged['animations'][index]['samplers']):
     for key in ('input','output'):
      a=motion['accessors'][before[key]];v=motion['bufferViews'][a['bufferView']]
      c=merged['accessors'][after[key]];w=merged['bufferViews'][c['bufferView']]
      self.assertEqual(raw[v.get('byteOffset',0):v.get('byteOffset',0)+v['byteLength']],binary[w.get('byteOffset',0):w.get('byteOffset',0)+w['byteLength']])
 def test_incompatible_rest_pose_is_rejected(self):
  from unittest.mock import patch
  import scripts.art.combine_meshy_web_glb as pipeline
  original_read=pipeline.read
  def corrupt(path):
   doc,binary=original_read(path)
   if str(path).endswith('web-guard.glb'):
    doc['nodes'][3]['translation']=[1,2,3]
   return doc,binary
  with tempfile.TemporaryDirectory() as tmp,patch.object(pipeline,'read',corrupt):
   with self.assertRaisesRegex(AssertionError,'Rest transform mismatch'):
    combine(self.folder/'web-rig.glb',[('Guard',self.folder/'web-guard.glb')],Path(tmp)/'bad.glb')

if __name__=='__main__':unittest.main()

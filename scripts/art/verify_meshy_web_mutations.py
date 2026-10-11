"""Prove native merge tests fail if a channel points at another joint."""
import importlib.util, unittest, sys
from pathlib import Path
sys.path.insert(0,str(Path(__file__).resolve().parents[2]))
from scripts.art import combine_meshy_web_glb as pipeline
spec=importlib.util.spec_from_file_location('webtests',pipeline.ROOT/'tests/art/test_meshy_web_pipeline.py')
tests=importlib.util.module_from_spec(spec);spec.loader.exec_module(tests)
source=Path(pipeline.__file__).read_text()
needle="channel['target']['node']=mapping[channel['target']['node']]"
assert source.count(needle)==1
namespace={'__file__':pipeline.__file__,'__name__':'mutation'}
exec(source.replace(needle,"channel['target']['node']=(mapping[channel['target']['node']]+1)%len(doc['nodes'])"),namespace)
tests.combine=namespace['combine']
result=unittest.TestResult();tests.WebPipelineTests('test_exact_source_surface_and_skin_are_retained').run(result)
assert len(result.failures)==1 and not result.errors,'Joint-target mutation was not detected'
print('Wrong-joint mutation: detected by source-to-output channel identity check')

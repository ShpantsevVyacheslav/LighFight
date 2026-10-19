"""Sequential per-prop bounded Blender jobs; original sources remain immutable."""
import json,subprocess,sys,time
from pathlib import Path
root=Path(__file__).resolve().parents[4];packet=root/'art/drafts/meeting-meshy-v1';start=time.time();results=[]
for row in json.loads((packet/'lod-jobs.json').read_text()):
 if time.time()-start>540:raise SystemExit('Batch time guard, remaining rows deferred')
 cmd=[sys.executable,str(packet/'tools/run_blender_bounded.py'),str(packet/(row['id']+'-weld-v2.log')),'-P',str(packet/'tools/make_prop_lod.py'),'--',row['source'],row['output'],str(row['triangles'])];code=subprocess.call(cmd,cwd=root);results.append({'id':row['id'],'exit':code});(packet/'lod-job-results.json').write_text(json.dumps(results,indent=2))
 if code:raise SystemExit(code)
print(json.dumps({'status':'LOD_GEOMETRY_BATCH_REQUIRES_VISUAL_GATES','jobs':results,'seconds':time.time()-start}))

"""Run owned stages sequentially; stop at first failed gate. No promotion/build/install."""
from pathlib import Path
import subprocess,sys,os,json,time
R=Path(__file__).resolve().parents[3];role=sys.argv[1];assert role in ['master','ranger','retro'];out=R/'art/drafts/full-roster-v1';env=dict(os.environ,LF_DENSE_ROLE=role);steps=[]
def run(label,cmd,method=None):
 e=env.copy()
 if method:e['LF_SEATED_METHOD']=method
 start=time.time();p=subprocess.run(cmd,cwd=R,env=e);code=p.returncode
 if method:
  process=json.loads((R/"art/drafts/meeting-seated-v1"/(cmd[-1]+"-process.json")).read_text());code=process["exitCode"] or (124 if process.get("reason") else 0)
 steps.append({'stage':label,'exit':code,'seconds':time.time()-start});(out/(role+'-recipe-run.json')).write_text(json.dumps({'status':'RUNNING' if code==0 else 'FAILED_NOT_PROMOTED','steps':steps},indent=2));print('STAGE '+json.dumps(steps[-1]),flush=True)
 if code:raise SystemExit(code)
run('fbx-export',['python3','art/drafts/meeting-meshy-v1/tools/run_blender_bounded.py',str(out/(role+'-bary-fbx.log')),'-P','art/drafts/full-roster-v1/export_dense_review_fbx.py','--',role])
run('weight-oracle',['/Users/macbook1/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/bin/python3','art/drafts/full-roster-v1/prepare_fbx_weight_oracle.py',role])
for label,method in [('import','FullRosterDenseImport.Run'),('gpu','FullRosterFloatGpuGate.Run'),('continuity','FullRosterMotionContinuityGate.Run'),('surface','FullRosterNative67Gate.Run')]:
 run(label,['python3','.tools/meeting-seated-run.py',role+'-recipe-'+label],method)
(out/(role+'-recipe-run.json')).write_text(json.dumps({'status':'GATES_EXECUTED_VISUAL_AND_RUNTIME_REVIEW_PENDING','steps':steps},indent=2));print('RESULT stages finished; not promoted',flush=True)

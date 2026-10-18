"""Full original dance fit using exact-cache-gated callback batching. No export.
Bounded wall/sample budget; preserves legacy source/cache files and original curves.
"""
import sys,json,hashlib,argparse
opts=argparse.ArgumentParser();opts.add_argument('--resume-cache');options=opts.parse_args(sys.argv[sys.argv.index('--')+1:] if '--' in sys.argv else [])
from pathlib import Path
O=Path(__file__).resolve().parents[1];P=O/'dense-def-fbx-packet-v1';wrapper=Path(__file__).resolve();adapter=O/'tools/36_adaptive_source_cache.py';text=adapter.read_text()
for filename,status in [('finger-batch-frozen-cache-gate.json','PASS_ALL_FROZEN_SOURCE_POSES'),('support-finger-batch-frozen-gate.json','PASS_ALL498_FROZEN_ORIGINAL_POSES')]:
 report=json.loads((P/filename).read_text());assert report['status']==status,filename+' not accepted'
 assert report['sourceCallbackSHA256']==hashlib.sha256((O/'tools/44_profile_fk_finger_batches.py').read_bytes()).hexdigest()
 if 'supportScriptSHA256' in report:assert report['supportScriptSHA256']==hashlib.sha256((O/'tools/48_profile_support_finger_batches.py').read_bytes()).hexdigest()
sys.argv=[str(adapter),'--','--full','--clip','dance_01','--max-samples','40000','--max-seconds','7200'];__file__=str(adapter);prefix=text[:text.index('if a.full or a.adaptive_interval:')].replace('source_calls%192==0','source_calls%1024==0');exec(compile(prefix,str(adapter),'exec'),globals())
# Identical generated candidate code that passed frozen-original gates. No code
# edits to original evaluator/shared scripts; source_sample dynamically uses these globals.
t44=(O/'tools/44_profile_fk_finger_batches.py').read_text();exec(compile(t44[t44.index('start=fast.index'):t44.index('manifest=json.loads')],'verified-finger-batching','exec'),globals());retarget=retarget_batched
t48=(O/'tools/48_profile_support_finger_batches.py').read_text();exec(compile(t48[t48.index('sourceSupport='):t48.index('# Compare original evaluator')],'verified-support-finger-batching','exec'),globals());support=support_batched
mode='FULL_ADAPTIVE_TIGHT008_EXACT_BATCH';sampler.surface_limit=.00008;sampler.angle_limit=.008
sourceConfig['exactCallbackOptimization']={'scripts':{n:hashlib.sha256((O/'tools'/n).read_bytes()).hexdigest() for n in ['44_profile_fk_finger_batches.py','46_finger_batch_frozen_cache_gate.py','48_profile_support_finger_batches.py']},'gateReports':{n:hashlib.sha256((P/n).read_bytes()).hexdigest() for n in ['finger-batch-frozen-cache-gate.json','support-finger-batch-frozen-gate.json']},'truth':'Original unchanged source evaluator; all3311box+498independent poses gate; no approximation','checkpointEverySourceCalls':1024};configText=json.dumps(sourceConfig,sort_keys=True)
if options.resume_cache:
 loaded=np.load(options.resume_cache);assert str(loaded['sourceConfig'])==configText,'Optimized source checkpoint fingerprint mismatch'
 for t,local,world,skin in zip(loaded['times'],loaded['localTRS'],loaded['worldPose'],loaded['worldSkin']):sampler.cache[Fraction(int(t[0]),int(t[1]))]=SourceSample(local,world,skin)
 print('RESUME '+str(len(sampler.cache))+' unchanged source poses',flush=True)
tail=text[text.index('if a.full or a.adaptive_interval:'):];tail=tail.replace('Fraction(7*k+1,840)','Fraction(11*k+1,1320)').replace("row['holdoutCellOffset']='1/7'","row['holdoutCellOffset']='1/11'")
exec(compile(tail,str(adapter)+'-verified-batching-tight-policy','exec'),globals())
reportFile=P/('adaptive-dance_01-'+mode+'.json');r=json.loads(reportFile.read_text());r.update(internalFitSurfaceM=.00008,internalFitBoneDeg=.008,finalAcceptanceSurfaceM=.0001,finalAcceptanceBoneDeg=.01,holdoutOffset='1/11',wrapperSHA256=hashlib.sha256(wrapper.read_bytes()).hexdigest());reportFile.write_text(json.dumps(r,indent=2))

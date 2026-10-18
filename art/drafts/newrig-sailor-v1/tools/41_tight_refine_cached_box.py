"""Tighter fit policy around the SAME frozen source evaluator/cache.
No source pose, correction or global acceptance threshold changes. New files preserve v1.
Run only after heavy-window release. Fresh120Hz holdout phase1/11, not v1's1/7.
"""
import sys,json,hashlib,inspect,ast,io,zipfile
from pathlib import Path
import numpy as np
wrapperPath=Path(__file__).resolve();O=wrapperPath.parents[1];P=O/'dense-def-fbx-packet-v1';adapter=O/'tools/36_adaptive_source_cache.py';text=adapter.read_text();old=np.load(P/'adaptive-box_02-FULL_ADAPTIVE-cache.npz');oldConfig=json.loads(str(old['sourceConfig']));assert hashlib.sha256(adapter.read_bytes()).hexdigest()==oldConfig['code'][adapter.name],'Legacy source adapter changed; source equivalence must be reviewed before cache migration'
# Execute the unchanged source setup/callback and its strict verified cache loader, not its old fit.
sys.argv=[str(adapter),'--','--full','--clip','box_02','--resume-cache',str(P/'adaptive-box_02-FULL_ADAPTIVE-cache.npz'),'--max-samples','6000','--max-seconds','1200'];__file__=str(adapter);prefix=text[:text.index('if a.full or a.adaptive_interval:')];exec(compile(prefix,str(adapter),'exec'),globals())
# Source-only fingerprint excludes sampling accuracy, candidate interpolation and holdout policy.
physics=text[text.index(' phase=float(t/duration);retarget'):text.index(';source_calls+=1')]
sourceOnly={'clip':a.clip,'sourceFBXSHA256':sourceConfig['sourceFBX'],'sourceBlendSHA256':sourceConfig['sourceBlend'],'sourceAttributes':sourceConfig['attributes'],'weightsSHA256':sourceConfig['weightsSHA256'],'restSHA256':sourceConfig['restSHA256'],'bones':names,'blender':bpy.app.version_string,'sourcePoseCallbackPhysicsSHA256':hashlib.sha256(physics.encode()).hexdigest(),'sourceSharedScripts':{name:sourceConfig['code'][name] for name in ['05_full_motion_gallery.py','27_actual_arm_elevation.py','bx_rig.py']},'sourceSkinMatrixFormula':inspect.getsource(Skeleton.skin)}
sourceFingerprint=hashlib.sha256(json.dumps(sourceOnly,sort_keys=True).encode()).hexdigest();mode='FULL_ADAPTIVE_TIGHT008';sampler.surface_limit=.00008;sampler.angle_limit=.008
# Revalidate representation on cached truth under the stricter policy; cached truth is never altered.
for t,truth in sampler.cache.items():
 represented=skel.world_pose(truth.local_trs)
 if bounds.error_bound(truth.world_skin,skel.skin(represented))>.00008 or bone_angle_deg(truth.world_pose,represented)>.008:raise RuntimeError('Original localTRS representation cannot meet internal accuracy; more keys cannot repair it')
tail=text[text.index('if a.full or a.adaptive_interval:'):];tail=tail.replace('Fraction(7*k+1,840)','Fraction(11*k+1,1320)').replace("row['holdoutCellOffset']='1/7'","row['holdoutCellOffset']='1/11'")
# Fit uses stricter internal limits. Final source acceptance remains0.1mm/0.01degree in unchanged tail.
exec(compile(tail,str(adapter)+'-tight-policy','exec'),globals())
reportFile=P/'adaptive-box_02-FULL_ADAPTIVE_TIGHT008.json';r=json.loads(reportFile.read_text());r.update({'sourceOnlyFingerprint':sourceFingerprint,'sourceFingerprintData':sourceOnly,'sourceCacheReusedEntries':len(old['times']),'internalFitSurfaceM':.00008,'internalFitBoneDeg':.008,'finalAcceptanceSurfaceM':.0001,'finalAcceptanceBoneDeg':.01,'newUniformHoldoutCellOffset':'1/11','policyWrapperSHA256':hashlib.sha256(wrapperPath.read_bytes()).hexdigest()});reportFile.write_text(json.dumps(r,indent=2))
# Add the source-only fingerprint without rewriting or quantizing cached arrays.
for cacheFile in [P/'adaptive-box_02-FULL_ADAPTIVE_TIGHT008-cache.npz',P/'adaptive-box_02-FULL_ADAPTIVE_TIGHT008-checkpoint.npz']:
 buffer=io.BytesIO();np.save(buffer,np.array(sourceFingerprint),allow_pickle=False)
 with zipfile.ZipFile(cacheFile,'a',compression=zipfile.ZIP_DEFLATED) as z:z.writestr('sourceOnlyFingerprint.npy',buffer.getvalue())
print('SOURCE_FINGERPRINT '+sourceFingerprint,flush=True)

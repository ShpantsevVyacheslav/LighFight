"""Offline32cached-position samples only; source-elevation screening, not acceptance."""
import os
os.environ['OPENBLAS_NUM_THREADS']='1'
from pathlib import Path
import numpy as np,json,hashlib
O=Path(__file__).resolve().parents[1];ROOT=O.parents[2];D=ROOT/'art/source/unique-dances-20261018';cat=json.loads((D/'catalog-selection.json').read_text());rows=[]
for item in cat['actions']:
 ident=item['action_id'];p=D/'audit'/f'action-{ident}-positions.json';poses=json.loads(p.read_text());e=[]
 for pose in poses:
  up=np.array(pose['Spine'])-pose['Spine02'];up/=np.linalg.norm(up)
  for side in ['Left','Right']:
   arm=np.array(pose[side+'ForeArm'])-pose[side+'Arm'];arm/=np.linalg.norm(arm);e.append(float(np.degrees(np.arcsin(np.clip(up@arm,-1,1)))))
 rows.append({'actionId':ident,'catalogName':item['catalog_name'],'source':str((D/f'action-{ident}.fbx').relative_to(ROOT)),'sourceSHA256':item['source_sha256'],'alreadyAssignedActor':item['actor'],'positionSamplesPerClip':len(poses),'upperArmElevationProxyDeg':dict(zip(['median','p95','max'],map(float,np.quantile(e,[.5,.95,1])))),'positionAuditSHA256':hashlib.sha256(p.read_bytes()).hexdigest()})
rows.sort(key=lambda r:r['upperArmElevationProxyDeg']['p95']);r={'status':'OFFLINE_CACHED_POSITION_SCREEN_NOT_RETARGET_OR_VISUAL_ACCEPTANCE','definition':'Per-side ForeArm-head minus Arm-head; elevation against normalized Spine-head minus Spine02-head. Cached32sourcepositions, not fullclip localChestmatrix or a target-rig measurement.','actions':rows,'limits':['Names/unique hashes are not substituted for motion-distinctness audit. Existing catalog distinctness is separate.','Lower cached proxy does not establish natural shoulder deformation or a fixed skeleton.','Existing roster assignments must not be duplicated secretly. Review uniqueness globally before any remapping.','No sources generated, no retarget/candidate/engine launch.']};(O/'available-dances-cached-elevation.json').write_text(json.dumps(r,indent=2)+'\n');print([(a['actionId'],a['catalogName'],{k:round(v,2) for k,v in a['upperArmElevationProxyDeg'].items()}) for a in rows])

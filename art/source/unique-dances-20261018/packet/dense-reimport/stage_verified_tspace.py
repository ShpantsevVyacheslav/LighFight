"""Stage only a completed, independently source-qualified Tspace FBX.
No engine launch; copies model only into the owned review folder, keeps any.meta.
"""
from pathlib import Path
import hashlib,json,shutil,sys

def stage(packet,owned):
 packet,owned=Path(packet),Path(owned)
 export=json.loads((packet/'adaptive-box-tspace-export.json').read_text())
 oracle=json.loads((packet/'adaptive-box-tspace-fbx-offkey.json').read_text())
 assert oracle['status']=='PASS_ORIGINAL_HOLDOUTS_PENDING_UNITY','No successful independent source FBX holdout gate'
 assert export['use_tspace'] is True and export['authoritativeOriginalSourceAttributesExact'] is True and export['allOriginalNamedWeightBitsExact'] is True
 assert oracle['maxSurfaceBoundM']<=.0001 and oracle['maxBoneDeg']<=.01
 source=packet/export['file'];digest=hashlib.sha256(source.read_bytes()).hexdigest()
 assert digest==export['SHA256']==oracle['FBXSHA256']
 assert source.name=='SailorOriginalDenseDEF-BoxAdaptiveTspace-v2.fbx'
 owned.mkdir(parents=True,exist_ok=True);target=owned/source.name
 if not target.exists() or hashlib.sha256(target.read_bytes()).hexdigest()!=digest:shutil.copyfile(source,target)
 assert hashlib.sha256(target.read_bytes()).hexdigest()==digest
 manifest=dict(file=target.name,SHA256=digest,bytes=target.stat().st_size,sourceStatus=oracle['status'],sourceOracle=str((packet/'adaptive-box-tspace-fbx-offkey.json').resolve()),originalAttributesExact=True,reviewOnly=True)
 (owned/'source-tspace-stage.json').write_text(json.dumps(manifest,indent=2)+'\n');print(json.dumps(manifest,indent=2))
if __name__=='__main__':stage(*sys.argv[1:])

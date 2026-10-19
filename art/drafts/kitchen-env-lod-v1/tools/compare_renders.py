"""Compare actual GLB raster evidence; fixed conservative gates, no auto promotion."""
import json
from pathlib import Path
import numpy as np
from PIL import Image,ImageDraw
from raster_metrics import silhouette_metrics
OUT=Path(__file__).resolve().parents[1];R=OUT/'renders';states=['master','lod-300000','lod-100000'];views=['whole','table-legs','coffee-detail'];criteria={'silhouetteIoU_min':.995,'sourceHolePixelsCoveredFraction_max':.02,'RGB8_MAE_max':3.,'RGB8_P95_max':12.};results={'status':'REVIEW_REQUIRED_NO_AUTOMATIC_PROMOTION','scope':'Static environment only; source4Kbytes unchanged, no character work.','criteriaFixedBeforeImages':criteria,'states':{}};manifests={s:json.loads((R/s/'manifest.json').read_text()) for s in states}
for state in states[1:]:
 rows=[];fail=[]
 for view in views:
  a=np.asarray(Image.open(R/'master'/f'{view}.png').convert('RGB')).astype(np.int16);b=np.asarray(Image.open(R/state/f'{view}.png').convert('RGB')).astype(np.int16);ma=np.asarray(Image.open(R/'master'/f'{view}-id.pgm'));mb=np.asarray(Image.open(R/state/f'{view}-id.pgm'));assert a.shape==b.shape and ma.shape==mb.shape==a.shape[:2];cm={s:next(r for r in manifests[s]['images'] if r['view']==view) for s in ['master',state]};assert np.array_equal(cm['master']['cameraWorldMatrix'],cm[state]['cameraWorldMatrix']);assert cm['master']['orthoScale']==cm[state]['orthoScale'];delta=np.abs(a-b);unionProps=(ma>0)|(mb>0)
  for role in [1,2,3]:
   sm=ma==role;lm=mb==role;union=sm|lm
   if not union.any():continue
   RGB=delta[union];row={'view':view,'role':{1:'table+4chairs',2:'fridge+microwave',3:'coffee'}[role],**silhouette_metrics(sm,lm),'RGB8_MAE':float(RGB.mean()),'RGB8_P95':float(np.quantile(RGB,.95)),'RGB8_P99':float(np.quantile(RGB,.99)),'RGB8_Max':int(RGB.max()),'propPixelsAnyRGBDeltaOver8Fraction':float(np.mean(RGB.max(1)>8))};iou=row['silhouetteIoU'];cover=row['sourceHolePixelsCoveredFraction'];row['numericGate']=bool(iou>=criteria['silhouetteIoU_min'] and cover<=criteria['sourceHolePixelsCoveredFraction_max'] and row['RGB8_MAE']<=criteria['RGB8_MAE_max'] and row['RGB8_P95']<=criteria['RGB8_P95_max']);rows.append(row)
   if not row['numericGate']:fail.append({'view':view,'role':row['role']})
  heat=a.astype(np.uint8).copy();bad=(delta.max(2)>8)&unionProps;heat[bad]=[255,30,30];Image.fromarray(heat).save(R/state/f'{view}-difference-over8.png')
 results['states'][state]={'numericGate':'PASS' if not fail else 'FAIL','failingViewsRoles':fail,'rows':rows,'visualDecision':'REQUIRED: inspect all chair/table legs, perforations, cup handles, rack wires, seams; numeric pass does not accept mesh.'}
for view in views:
 sheet=Image.new('RGB',(1650,550),(28,28,28));draw=ImageDraw.Draw(sheet)
 for i,state in enumerate(states):
  im=Image.open(R/state/f'{view}.png').convert('RGB');im.thumbnail((550,500));sheet.paste(im,(550*i,50));tri=manifests[state]['trianglesIncludingArchitecture'];draw.text((i*550+10,10),f'{state} | actual {tri:,}tri | {view}',fill='white');draw.text((i*550+10,28),'MASTER' if state=='master' else 'UNPROMOTED ENVIRONMENT TRIAL',fill='white')
 sheet.save(OUT/f'compare-{view}.jpg',quality=95)
(OUT/'raster-review.json').write_text(json.dumps(results,indent=2));print(json.dumps({s:r['numericGate'] for s,r in results['states'].items()},indent=2))

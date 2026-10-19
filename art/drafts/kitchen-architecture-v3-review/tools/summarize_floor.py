"""Read existing full-triangle ray evidence; retain RED perimeter test explicitly."""
from pathlib import Path
import json
import numpy as np
from PIL import Image,ImageDraw
OUT=Path(__file__).resolve().parents[1];j=json.loads((OUT/'floor-grid.json').read_text());g=j['summary'];a=json.loads((OUT/'audit.json').read_text());lo=a['boundsMinXYZ_BlenderZUp'];ex=a['extentsXYZ'];N=g['grid'];core=[]
for row in j['samples']:
 ix=round(((row['x']-lo[0])/ex[0]-.05)/.9*(N-1));iy=round(((row['y']-lo[1])/ex[1]-.05)/.9*(N-1))
 if 6<=ix<=94 and 6<=iy<=94:core.append(row['floorZ'])
expected=89*89;summary={'fullGrid5PercentMarginPass':g['allInteriorGridHasLowFloor'],'fullGridRays':N*N,'fullGridFloorHits':g['floorHits'],'fullGridMissing':g['missing'],'fullGridWallOrHighHits':g['highBandHits'],'centralSubsetBoundingMarginFraction':.104,'centralSubsetExpectedRays':expected,'centralSubsetFloorHits':len(core),'centralSubsetContinuousAtSampledGrid':len(core)==expected,'centralSubsetFloorMinMax':list(map(float,[min(core),max(core)])),'centralSubsetFloorP01P50P99':np.quantile(core,[.01,.5,.99]).tolist(),'interpretation':'Full5% rectangular test remains RED. Missing samples lie near visibly beveled front corner; high hits mostly right wall. Central10.4% bounding-margin subset has no sampled low-floor gaps; this does not assert continuum/watertightness or fix cut-corner mismatch.'};(OUT/'floor-summary.json').write_text(json.dumps(summary,indent=2))
img=Image.new('RGB',(N,N),(75,180,110))
for x,y in g['missGridCoordinates']:img.putpixel((x,N-1-y),(220,50,50))
for x,y,z in g['highBandCoordinates']:img.putpixel((x,N-1-y),(240,160,30))
img=img.resize((606,606),Image.Resampling.NEAREST);sheet=Image.new('RGB',(646,686),(28,28,28));sheet.paste(img,(20,60));d=ImageDraw.Draw(sheet);d.text((20,12),'Full5% grid: green=floor red=miss orange=wall/high',fill='white');d.text((20,32),'101x101 full-triangle BVH rays | top orientation',fill='white');sheet.save(OUT/'floor-grid-map.png');print(json.dumps(summary,indent=2))

"""Offline same-camera source-native versus dense inferred spatial weights."""
import json,sys
from pathlib import Path
from PIL import Image,ImageDraw
O=Path(sys.argv[1]).resolve();r=json.loads((O/'spatial-report.json').read_text());assert r['status']=='COMPLETED_SPATIAL_AB_NOT_QUALITY_ACCEPTED'
for view in ['front','backthreequarter','side']:
 sheet=Image.new('RGB',(1280,2454),(230,230,230));d=ImageDraw.Draw(sheet)
 for y,row in enumerate(r['poses']):
  caps=[c for c in row['captures'] if '-'+view+'-clay.png' in c['file']];assert len(caps)==2
  for x,c in enumerate(caps):
   im=Image.open(O/c['file']).convert('RGBA');bg=Image.new('RGBA',im.size,(230,230,230,255));bg.alpha_composite(im);sheet.paste(bg.convert('RGB'),(640*x,818*y+50));d.text((640*x+12,818*y+8),f"{'Native67 authored 100k' if x==0 else 'Original 2M / native67 inferred spatial10max'} | dance {row['phase']:.2f}",fill='black')
 sheet.save(O/f'comparison-{view}.jpg',quality=93)

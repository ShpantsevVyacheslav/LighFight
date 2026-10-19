"""Label actual paired views; Xray overlay is explicitly diagnostic."""
import json
from pathlib import Path
from PIL import Image,ImageDraw
O=Path(__file__).resolve().parents[1]/'actual-primary-v1'
r=json.loads((O/'report.json').read_text());assert r['status'].startswith('COMPLETED')
for view in ['front','backthreequarter','side']:
 sheet=Image.new('RGB',(1280,3*818),(235,235,235));d=ImageDraw.Draw(sheet)
 for y,row in enumerate(r['poses']):
  caps=[c for c in row['captures'] if ('-'+view+'-clay.png') in c['beauty']];assert len(caps)==2
  for x,c in enumerate(caps):
   im=Image.open(O/c['beauty']).convert('RGBA');bg=Image.new('RGBA',im.size,(230,230,230,255));bg.alpha_composite(im);sheet.paste(bg.convert('RGB'),(x*640,y*818+50));d.text((x*640+12,y*818+8),f"{'Native67 authored weights' if x==0 else 'Fresh Rigify full original 2M'} | raw dance {row['phase']:.2f} | C1 OFF",fill='black')
   joint=Image.open(O/c['jointPass']).convert('RGBA');over=bg.copy();over.alpha_composite(joint);od=ImageDraw.Draw(over);od.text((8,8),'XRAY JOINTS: no actor occlusion',fill='red');over.convert('RGB').save(O/(c['beauty'].replace('-clay.png','-joints-overlay.jpg')),quality=92)
 sheet.save(O/f'comparison-{view}.jpg',quality=93)

"""Build labeled review sheets from unchanged rendered PNGs; no model/pixel recolor."""
from pathlib import Path
from PIL import Image,ImageDraw
ROOT=Path(__file__).resolve().parents[4];O=ROOT/'art/drafts/newrig-sailor-v1/native67-trapezius-v1';D=O/'renders'
for view in ['front','backthreequarter']:
 phases=[p for p in [0,.25,.5,.75,1] if all((D/f'{p:.2f}-{v}-{view}.png').exists() for v in ['baseline','corrected'])]
 if not phases:continue
 w=576;sheet=Image.new('RGB',(w*2,(w+45)*len(phases)),(28,28,28));draw=ImageDraw.Draw(sheet)
 for row,p in enumerate(phases):
  for col,v in enumerate(['baseline','corrected']):
   image=Image.open(D/f'{p:.2f}-{v}-{view}.png').convert('RGB');image.thumbnail((w,w));sheet.paste(image,(col*w,row*(w+45)+45));draw.text((col*w+12,row*(w+45)+9),f'{p:.2f} {v.upper()} | original67 native mesh | {view}',fill='white')
 sheet.save(O/f'paired-{view}.jpg',quality=95)
print('Review sheets generated; original renders unchanged.')
for phase in [.5,.75]:
 sheet=Image.new('RGB',(1536,1616),(28,28,28));draw=ImageDraw.Draw(sheet)
 for row,view in enumerate(['front','backthreequarter']):
  for col,variant in enumerate(['baseline','corrected']):
   draw.text((col*768+12,row*808+12),f'Phase {phase:.2f} | {variant.upper()} | {view} | original67 native mesh',fill='white');sheet.paste(Image.open(D/f'{phase:.2f}-{variant}-{view}.png').convert('RGB'),(col*768,row*808+40))
 sheet.save(O/f'phase{int(phase*100)}-before-after.png')

"""Diagnostic contact sheets; input source/render PNGs retained unchanged."""
from pathlib import Path
from PIL import Image,ImageDraw,ImageFont
O=Path(__file__).resolve().parents[1];folder=O/'renders-source-v1';cases=['dance25','dance50','dance75','selected-L-0','selected-R-2'];font=ImageFont.load_default(size=16)
for view in ['front','backthreequarter','side']:
 sheet=Image.new('RGB',(640,416*len(cases)),(30,30,30));draw=ImageDraw.Draw(sheet)
 for row,case in enumerate(cases):
  for col,variant in enumerate(['OFF','ON']):
   image=Image.open(folder/f'{case}-{variant}-{view}-close.png').convert('RGB').resize((320,384),Image.Resampling.LANCZOS);sheet.paste(image,(col*320,row*416+32));draw.text((col*320+5,row*416+7),f'{case}: C1 {variant}',font=font,fill='white')
 sheet.save(O/f'comparison-{view}.png')
print('3 paired diagnostic sheets; original render PNGs retained')

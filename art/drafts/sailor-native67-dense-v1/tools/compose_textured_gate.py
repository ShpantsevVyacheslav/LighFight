"""Post-exit actual-source-frame sheets and alpha-qualified RGB metrics."""
import sys,json
from pathlib import Path
import numpy as np
from PIL import Image,ImageDraw
O=Path(sys.argv[1]);r=json.loads((O/'report.json').read_text());assert r['status']=='COMPLETED_TEXTURED_SELECTED_GATE_VISUAL_REVIEW_REQUIRED'
def tile(name,size=(640,800)):
 im=Image.open(O/name).convert('RGBA');bg=Image.new('RGBA',im.size,(225,225,225,255));bg.alpha_composite(im);return bg.convert('RGB').resize(size)
for view in ['front','side']:
 im=Image.new('RGB',(1280,3*850),(225,225,225));d=ImageDraw.Draw(im)
 for y,p in enumerate([45,50,55]):
  for x,variant in enumerate(['before-RigifyOFF','after-native67']):
   name=f'dance{p}-{variant}-{view}.png';im.paste(tile(name),(640*x,850*y+50));d.text((640*x+10,850*y+8),f'{variant} | source dance{p}% | original2M/4K',fill='black');d.text((640*x+10,850*y+26),'Same clip time/camera; anatomy differs ~10-11degrees',fill='black')
 im.save(O/f'textured-dance-{view}.jpg',quality=94)
for title,names in [('fullbody',['BOX-start-diagnostic-after-native67-front.png','BOX35-after-native67-front.png','BOX70-after-native67-front.png','kick35-after-native67-front.png','kick70-after-native67-front.png']),('details',['detail-head-front.png','detail-handR-front.png','detail-handL-front.png','detail-feet-front.png'])]:
 im=Image.new('RGB',(320*len(names),440),(225,225,225));d=ImageDraw.Draw(im)
 for x,n in enumerate(names):im.paste(tile(n,(320,400)),(320*x,40));d.text((320*x+5,8),n.replace('-after-native67-front.png','').replace('-front.png',''),fill='black')
 im.save(O/f'textured-{title}.jpg',quality=94)
metrics=[]
for f in r['frames']:
 a=np.asarray(Image.open(O/f['file']).convert('RGBA'));rgb=a[:,:,:3][a[:,:,3]>=250].astype(float)/255;assert len(rgb)>100
 sat=np.divide(rgb.max(1)-rgb.min(1),rgb.max(1),out=np.zeros(len(rgb)),where=rgb.max(1)>0)
 metrics.append({'file':f['file'],'alpha250PixelCount':len(rgb),'anyChannel250Fraction':float(np.mean(np.any(rgb>=250/255,axis=1))),'allChannels250Fraction':float(np.mean(np.all(rgb>=250/255,axis=1))),'rgbQuantiles':np.quantile(rgb,[0,.5,.95,.99,1],axis=0).tolist(),'saturationQuantiles':np.quantile(sat,[.5,.95,.99]).tolist()})
(O/'rgba-color-metrics.json').write_text(json.dumps({'method':'ActualPNG alpha>=250; RGB thresholds on source-exported display bytes; not scene-linear light or skin-specific semantic mask.','frames':metrics},indent=2)+'\n')

"""Offline rest-point raster of field stabilization; not anatomical segmentation."""
from pathlib import Path
import sys,numpy as np
from PIL import Image,ImageDraw
O=Path(sys.argv[1]);z=np.load(O/'stable-masks.npz');p=z['restWorld'];b=z['fieldBlend'];colors=np.tile([140,140,140],(len(p),1));colors[b==0]=[50,115,235];colors[(b>0)&(b<1)]=[240,185,45]
for view,u,depth in [('front',p[:,0],p[:,1]),('side-minusX',-p[:,1],p[:,0])]:
 x=(u-u.mean())/.98*900+384;y=(p[:,2].max()-p[:,2])/.98*900+70;xi=np.rint(x).astype(int);yi=np.rint(y).astype(int);ok=(xi>=0)&(xi<768)&(yi>=60)&(yi<1024);ids=np.flatnonzero(ok);pix=yi[ids]*768+xi[ids];order=np.lexsort((depth[ids],pix));ids=ids[order];pix=pix[order];_,first=np.unique(pix,return_index=True);ids=ids[first];canvas=np.full((1024,768,3),235,np.uint8);canvas[yi[ids],xi[ids]]=colors[ids];im=Image.fromarray(canvas);d=ImageDraw.Draw(im);d.text((10,8),'REST stabilization only: blue=barycentric; yellow=blend; gray=field',fill='black');d.text((10,28),view+' | no anatomical mask claim',fill='black');im.save(O/f'stable-mask-{view}.png')

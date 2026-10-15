from pathlib import Path
import csv,json,math
p=Path(__file__).resolve().parent; rows=list(csv.DictReader((p/'Evidence/Green/samples.csv').open()));checks=[]
def rgb(r):return [int(r[k]) for k in ('r','g','b')]
def dec(x):return x/12.92 if x<=.04045 else ((x+.055)/1.055)**2.4
def enc(x):return 12.92*x if x<=.0031308 else 1.055*x**(1/2.4)-.055
def find(mode,case,path,point):return next(r for r in rows if r['mode']==mode and r['case']==case and r['path']==path and r['point']==point)
def check(label,a,b,tol):
 d=max(abs(x-y) for x,y in zip(a,b));checks.append(dict(check=label,actual=a,expected=b,maxDifference=d,tolerance=tol,passed=d<=tol))
for mode in ['LDR-noPost','HDR-filmic']:
 bg=rgb(find(mode,'direct-camera','backbuffer','literalCenter'))
 for name,source,tint,alpha in [('flatTintAlpha',[56,60,66],[.4,.7,1],.6),('haloTintAlpha',[56,160,220],[.3,.8,1],.65),('whiteCardTint',[255]*3,[.11,.115,.16],.95),('dynamicWhiteDisc',[255]*3,[.28,.78,1],.65)]:
  expected=[round(enc(dec(s/255)*dec(t)*alpha+dec(b/255)*(1-alpha))*255) for s,t,b in zip(source,tint,bg)]
  row=find(mode,name,'corrected','67x60');check(mode+' '+name+' physical RGB blend',rgb(row),expected,2);check(mode+' '+name+' source-over alpha',[int(row['a'])],[255],0)
 for case in ['portraitCrop','portraitStretch']:
  for point in ['12x12','67x60','125x110']:
   old=rgb(find(mode,case,'builtIn',point));new=rgb(find(mode,case,'corrected',point));check(mode+' '+case+' '+point+' single excess encoding removed',new,[round(dec(v/255)*255) for v in old],3)
 for case in ['cameraHalf','cameraLinear32','cameraSRGB32']:
  for point,reference in [('67x60','literalCenter'),('12x12','redTopLeft'),('125x110','blueBottomRight')]:check(mode+' '+case+' '+point+' direct camera reference',rgb(find(mode,case,'corrected',point)),rgb(find(mode,'direct-camera','backbuffer',reference)),1)
 # Fully rounded-out corner must reveal the direct scene at that transformed screen point.
 for i,case in [(0,'portraitCrop'),(1,'portraitStretch'),(4,'cameraHalf'),(5,'cameraLinear32'),(6,'cameraSRGB32'),(7,'whiteCardTint')]:
  x=math.floor((10+i%3*315+145+1)*1.2+20);y=math.floor((30+i//3*160+1)*1.3+70);ref='redTopLeft' if x<240 and y<170 else 'blueBottomRight' if x>960 and y>680 else 'literalCenter';check(mode+' '+case+' rounded transparent corner',rgb(find(mode,case,'corrected','1x1')),rgb(find(mode,'direct-camera','backbuffer',ref)),1)
(p/'numeric-verification.json').write_text(json.dumps(dict(method='Independent arithmetic checks on actual final windowed Metal CSV. Encoded source bytes are decoded to physical linear, multiplied by authored InterfaceColor linear tint, source-over composed over measured direct-camera background, then display encoded. No source pixels changed.',checks=checks,failed=sum(not c['passed'] for c in checks)),indent=2));print('checks',len(checks),'failed',sum(not c['passed'] for c in checks))
for c in checks:
 if not c['passed']:print(c)

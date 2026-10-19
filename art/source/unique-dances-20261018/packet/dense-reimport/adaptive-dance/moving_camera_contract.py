"""Pure finite moving-frame camera contract. Not continuous/GPU acceptance."""
import math

def smooth(u):return u*u*(3-2*u)
def bump(u):return 16*u*u*(1-u)*(1-u)
def fit(samples,idle_center,dance_center,idle_size,dance_size,aspect,margin=.02):
 if not samples or aspect<=0 or min(idle_size,dance_size)<=0 or margin<0:raise ValueError('Valid finite camera inputs required')
 amplitude=0.;rows=[]
 for u,lo,hi in samples:
  if not math.isfinite(u) or not 0<=u<=1 or any(not math.isfinite(x) for x in (*lo,*hi)) or any(x>y for x,y in zip(lo,hi)):raise ValueError('Finite ordered frame required')
  w=smooth(u);center=[a*(1-w)+b*w for a,b in zip(idle_center,dance_center)]
  needed=max(abs((lo[1]+hi[1])/2-center[1])+(hi[1]-lo[1])/2,(abs((lo[0]+hi[0])/2-center[0])+(hi[0]-lo[0])/2)/aspect)*(1+margin)
  base=idle_size*(1-w)+dance_size*w;denom=bump(u)
  if denom==0 and needed>base+1e-12:raise ValueError('Endpoint does not fit: no hidden jump permitted')
  if denom>0:amplitude=max(amplitude,(needed-base)/denom)
  rows.append((u,needed,base,center))
 if samples[0][0]!=0 or samples[-1][0]!=1:raise ValueError('Both exact endpoints required')
 return dict(scope='FINITE_MOVING_FRAME_ONLY',amplitude=amplitude,frames=[dict(u=u,size=base+amplitude*bump(u),required=needed,center=center) for u,needed,base,center in rows])

def test():
 samples=[(0,[-.3,-.4,0],[.3,.4,0]),(.5,[-.3,-1,0],[.3,1,0]),(1,[-.3,-.4,0],[.3,.4,0])]
 result=fit(samples,[0,0],[0,0],.5,.5,16/9)
 assert result['amplitude']>.5 and all(f['size']>=f['required']-1e-12 for f in result['frames'])
 failed=False
 try:fit(samples,[0,0],[0,0],.1,.5,16/9)
 except ValueError:failed=True
 assert failed
 print('PASS middle excursion gets C1 camera envelope; invalid endpoint rejects instead of hidden jump')
if __name__=='__main__':test()

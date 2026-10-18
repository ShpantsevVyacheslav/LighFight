"""One-thread bounded offline neutral studio IBL/SH preparation, no engine.
Original maps/material factors are never read or modified. Derived review light.
GGX-prefiltered cubemap in Unity face order, then radiance-convolved diffuse SH.
"""
import os
os.environ.setdefault('OPENBLAS_NUM_THREADS','1')
os.environ.setdefault('OMP_NUM_THREADS','1')
import numpy as np,json,struct,hashlib,sys,math,time
from pathlib import Path
SIZE=128;MIPS=8;SAMPLES=128
C=np.array([[-1.,0,0],[0,0,1],[0,-1,0]])
positions=np.array([[-1.,-2,2],[1,-1,1],[0,1,2]])@C.T
centres=positions/np.linalg.norm(positions,axis=1,keepdims=True)
sigma=np.array([.35,.45,.35]);powers=np.array([450.,250,300])/np.sum(positions*positions,axis=1)
amplitudes=powers/powers.mean()*2.0

def unit(a):return a/np.linalg.norm(a,axis=-1,keepdims=True)
def radiance(d):
 # Circular smooth broad panels: style/location/power ratios follow Blender areas,
 # not claimed exact finite rectangles or absolute cross-renderer radiometry.
 dot=np.clip(d@centres.T,-1,1);value=.08+np.sum(amplitudes*np.exp(-(1-dot)/(sigma*sigma)),axis=-1)
 return np.repeat(value[...,None],3,axis=-1)
def face_dirs(face,size):
 q=2*(np.arange(size)+.5)/size-1;u,v=np.meshgrid(q,q);one=np.ones_like(u)
 # CubemapFace +X,-X,+Y,-Y,+Z,-Z; rows bottom-to-top for SetPixels.
 return unit(np.stack([(one,-v,-u),(-one,-v,u),(u,one,v),(u,-one,-v),(u,-v,one),(-u,-v,-one)][face],axis=-1)).reshape(-1,3)
def radical(i):
 x=i;y=0.;f=.5
 while x:y+=f*(x&1);x>>=1;f*=.5
 return y
xi=np.array([[(i+.5)/SAMPLES,radical(i)] for i in range(SAMPLES)])
def prefilter(d,rough):
 if rough<=0:return radiance(d)
 alpha=rough*rough;phi=2*np.pi*xi[:,0];cos=np.sqrt((1-xi[:,1])/(1+(alpha*alpha-1)*xi[:,1]));sin=np.sqrt(np.maximum(0,1-cos*cos));hlocal=np.stack([np.cos(phi)*sin,np.sin(phi)*sin,cos],axis=1);out=np.empty((len(d),3))
 for start in range(0,len(d),512):
  n=d[start:start+512];up=np.tile([0.,0,1.],(len(n),1));up[np.abs(n[:,2])>.999]=[1,0,0];t=unit(np.cross(up,n));b=np.cross(n,t)
  h=t[:,None,:]*hlocal[None,:,0:1]+b[:,None,:]*hlocal[None,:,1:2]+n[:,None,:]*hlocal[None,:,2:3]
  l=2*np.sum(n[:,None,:]*h,axis=-1,keepdims=True)*h-n[:,None,:];weight=np.maximum(0,np.sum(n[:,None,:]*l,axis=-1));r=radiance(l);out[start:start+len(n)]=np.sum(r*weight[:,:,None],axis=1)/np.maximum(1e-12,weight.sum(axis=1,keepdims=True))
 return out

def main(outdir):
 started=time.perf_counter();out=Path(outdir);out.mkdir(parents=True,exist_ok=True);levels=[]
 with (out/'neutral-studio-prefiltered.sib').open('wb') as f:
  f.write(b'SIB1');f.write(struct.pack('<iii',SIZE,MIPS,SAMPLES))
  for mip in range(MIPS):
   size=max(1,SIZE>>mip);lod=min(mip,6)/6;rough=(1.7-math.sqrt(2.89-2.8*lod))/1.4
   levels.append(dict(mip=mip,size=size,perceptualRoughness=rough))
   for face in range(6):f.write(prefilter(face_dirs(face,size),rough).astype('<f4').tobytes())
 # Stable high-resolution spherical quadrature, exact latlong cell solid angle.
 W,H=512,256;theta=(np.arange(H)+.5)*np.pi/H;phi=(np.arange(W)+.5)*2*np.pi/W;th,ph=np.meshgrid(theta,phi,indexing='ij');d=np.stack([np.sin(th)*np.cos(ph),np.cos(th),np.sin(th)*np.sin(ph)],axis=-1);x,y,z=np.moveaxis(d,-1,0)
 basis=np.stack([np.ones_like(x)*.28209479177387814,.4886025119029199*y,.4886025119029199*z,.4886025119029199*x,1.0925484305920792*x*y,1.0925484305920792*y*z,.31539156525252005*(3*z*z-1),1.0925484305920792*x*z,.5462742152960396*(x*x-y*y)],axis=-1)
 solid=(2*np.pi/W)*(np.cos(np.arange(H)*np.pi/H)-np.cos((np.arange(H)+1)*np.pi/H));rgb=radiance(d);coeff=np.einsum('hwi,hwc,h->ci',basis,rgb,solid);coeff*=np.array([1,2/3,2/3,2/3,1/4,1/4,1/4,1/4,1/4])
 manifest=dict(status='PREPARED_DERIVED_REVIEW_LIGHT_NOT_SOURCE_EQUIVALENCE',format='SIB1 littleendian size,mipCount,sampleCount then mipmajor/UnityCubemapFaceorder float32RGB, SetPixels rowsbottom-to-top',baseSize=SIZE,mips=levels,importanceSamples=SAMPLES,sourcePanelBlenderPositions=[[-1,-2,2],[1,-1,1],[0,1,2]],fixedC=C.tolist(),panelDirectionsUnity=centres.tolist(),panelRadianSigma=sigma.tolist(),panelLinearNeutralAmplitude=amplitudes.tolist(),neutralBackgroundLinear=.08,shRGB=coeff.tolist(),shConvention='RealL2 order Y00,Y1-1(y),Y10(z),Y11(x),Y2-2(xy),Y2-1(yz),Y20(z2),Y21(xz),Y22(x2-y2); radianceSH times LambertA_l/PI. Diffuse reflected-light function, not un-convolvedradiance.',specularMipConvention='UnityStandard rough*(1.7-.7rough)*6, invert to assign roughness at integer mip. mip7 duplicatesrough1.',sourceOriginalAssetsUnchanged=True,approximation='Smooth broadpanel angular environment follows3sourceareas directions/relativepower/extent; not exact finitearea/Cycles/AgXmatch. Neutralwhite only. No sharp sun.',artistNormalStrengthAB=[1,.5,.25],normalCalibrationDisclosure='1 preserves source setting; .5/.25 explicit derivedartisticcalibration only. Never represent reducedstrength as sourceexact.',binarySHA256=hashlib.sha256((out/'neutral-studio-prefiltered.sib').read_bytes()).hexdigest(),seconds=time.perf_counter()-started)
 manifest['shFlat']=coeff.reshape(-1).tolist()
 probe_dirs=unit(np.array([[1,0,0],[-1,0,0],[0,1,0],[0,-1,0],[0,0,1],[0,0,-1],[1,1,1],[-1,1,1],[1,1,-1],[-1,-1,1],[.2,.9,.4],[-.7,.4,-.5]],float))
 manifest['probes']=[dict(direction=dict(zip(['x','y','z'],d.tolist())),expected=float(radiance(d)[0])) for d in probe_dirs]
 (out/'neutral-studio-ibl.json').write_text(json.dumps(manifest,indent=2)+'\n');print(json.dumps({k:v for k,v in manifest.items() if k not in ['shRGB','mips']},indent=2))
if __name__=='__main__':main(sys.argv[1])

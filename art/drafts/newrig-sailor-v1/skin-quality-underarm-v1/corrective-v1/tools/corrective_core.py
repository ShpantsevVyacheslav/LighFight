"""Local contour target math: no topology edits, smoothing or weight changes."""
import numpy as np
def compact_window(x,lo,hi):
 x=np.asarray(x,float)
 if not np.isfinite(x).all() or not hi>lo:raise ValueError('finite interval required')
 t=(x-lo)/(hi-lo);out=np.zeros_like(t);inside=(t>0)&(t<1);out[inside]=np.sin(np.pi*t[inside])**2;return out

def inverse_lbs_delta(linear,world_delta,condition_limit=50):
 L=np.asarray(linear,float);d=np.asarray(world_delta,float)
 if L.shape!=(len(d),3,3) or d.ndim!=2 or d.shape[1]!=3 or not np.isfinite(L).all() or not np.isfinite(d).all():raise ValueError('finite Nx3x3 and Nx3 required')
 cond=np.linalg.cond(L)
 if np.any(~np.isfinite(cond)) or np.any(cond>condition_limit):raise ValueError('ill-conditioned inverse LBS; refuse target')
 result=np.linalg.solve(L,d[...,None])[...,0]
 if not np.allclose(np.einsum('nij,nj->ni',L,result),d,atol=1e-12,rtol=1e-12):raise ValueError('inverse LBS roundtrip failed')
 return result

def radial_contour_delta(points,origin,axis,fallback_direction,target_radius,mask_weight,max_displacement):
 p=np.asarray(points,float);a=np.asarray(axis,float);a=a/np.linalg.norm(a);q=p-np.asarray(origin);radial=q-np.outer(q@a,a);r=np.linalg.norm(radial,axis=1);direction=np.zeros_like(radial);valid=r>1e-12;direction[valid]=radial[valid]/r[valid,None];fallback=np.array(fallback_direction,dtype=float,copy=True);fallback-=a*np.dot(fallback,a);fallback/=np.linalg.norm(fallback);direction[~valid]=fallback
 target=np.asarray(target_radius,float);w=np.asarray(mask_weight,float)
 if target.shape!=r.shape or w.shape!=r.shape or not np.isfinite(p).all() or not np.isfinite(target).all() or np.any((w<0)|(w>1)) or not max_displacement>0:raise ValueError('invalid contour parameters')
 amount=np.minimum(np.maximum(target-r,0),max_displacement)*w;return direction*amount[:,None]

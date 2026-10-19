"""Pure exact-boundary graph solve; no geometry/rig mutation or weight pruning."""
import numpy as np

def harmonic(n,edges,conductance,values,fixed,tolerance=1e-10,max_iterations=10000):
 e=np.asarray(edges,int);c=np.asarray(conductance,float);v=np.asarray(values,float);f=np.asarray(fixed,bool)
 if n<1 or v.shape!=(n,) or f.shape!=(n,) or e.ndim!=2 or e.shape[1]!=2 or len(c)!=len(e) or np.any(e<0) or np.any(e>=n) or not np.isfinite(c).all() or np.any(c<=0) or not np.isfinite(v).all():raise ValueError('finite positive graph required')
 a,b=e.T;diag=np.bincount(np.r_[a,b],np.r_[c,c],minlength=n);free=np.flatnonzero(~f)
 if np.any(diag[free]<=0) or not f.any():raise ValueError('anchored nonisolated graph required')
 def multiply(x):return diag*x-np.bincount(a,c*x[b],minlength=n)-np.bincount(b,c*x[a],minlength=n)
 fixedv=np.where(f,v,0);rhs=-multiply(fixedv)[free]
 def af(x):
  full=np.zeros(n);full[free]=x;return multiply(full)[free]
 x=v[free].copy();r=rhs-af(x);invdiag=1/diag[free];z=r*invdiag;p=z.copy();rz=float(r@z);iterations=0
 while np.max(np.abs(r),initial=0)>tolerance:
  if iterations>=max_iterations:raise RuntimeError('Dirichlet convergence deadline')
  ap=af(p);den=float(p@ap)
  if not np.isfinite(den) or den<=0:raise ValueError('unanchored/singular graph')
  step=rz/den;x+=step*p;r-=step*ap;z=r*invdiag;next_rz=float(r@z);p=z+(next_rz/rz)*p;rz=next_rz;iterations+=1
 out=v.copy();out[free]=x
 if np.any(out<min(v[f])-1e-9) or np.any(out>max(v[f])+1e-9):raise ValueError('discrete maximum principle failed')
 return out,{'iterations':iterations,'freeResidualInfinity':float(np.max(abs(r),initial=0)),'fixedExactlyUnchanged':bool(np.array_equal(out[f],v[f]))}

def mix(original,arm_slots,target,amount,fixed):
 w=np.asarray(original,float);arm=np.asarray(arm_slots,bool);target=np.asarray(target,float);fixed=np.asarray(fixed,bool)
 if w.ndim!=2 or w.shape[1]!=4 or arm.shape!=w.shape or target.shape!=(len(w),) or not np.all((np.asarray(amount)>=0)&(np.asarray(amount)<=1)) or np.any(w<0) or not np.isfinite(w).all():raise ValueError('nonnegative max4 source required')
 total=w.sum(1);a=(w*arm).sum(1);t=total-a
 if np.any(total<=0):raise ValueError('unweighted source')
 alpha=a/total;desired=(1-amount)*alpha+amount*target;out=w.copy();free=~fixed
 if np.any(free&((a<=0)|(t<=0))):raise ValueError('cannot invent missing family support')
 for slot in range(4):
  factor=np.ones(len(w));factor[free]=np.where(arm[free,slot],desired[free]*total[free]/a[free],(1-desired[free])*total[free]/t[free]);out[:,slot]*=factor
 # Both families only rescale existing positive slots, so no top4 truncation.
 if not np.array_equal(out[fixed],w[fixed]) or np.any((w>0)!=(out>0)) or not np.isfinite(out).all() or np.any(out<0):raise ValueError('support/boundary preservation failed')
 return out

def orientation_witnesses(baseline_triangles,candidate_triangles):
 a=np.asarray(baseline_triangles,float);b=np.asarray(candidate_triangles,float)
 if a.shape!=b.shape or a.ndim!=3 or a.shape[1:]!=(3,3) or not np.isfinite(a).all() or not np.isfinite(b).all():raise ValueError('same finite full triangles required')
 normal=lambda t:np.cross(t[:,1]-t[:,0],t[:,2]-t[:,0])
 na,nb=normal(a),normal(b);dot=np.einsum('ij,ij->i',na,nb);aa=np.linalg.norm(na,axis=1)*.5;ab=np.linalg.norm(nb,axis=1)*.5
 return {'opposed':np.flatnonzero(dot<0).tolist(),'newCollapsed':np.flatnonzero((ab<1e-18)&(aa>=1e-18)).tolist(),'dot':dot,'beforeNormal':na,'afterNormal':nb,'beforeArea':aa,'afterArea':ab}

def bounded_attenuation(field,distances,radius,fixed):
 f=np.asarray(field,float);d=np.asarray(distances,float);fixed=np.asarray(fixed,bool)
 if f.shape!=d.shape or f.shape!=fixed.shape or radius<=0 or np.any(f<0) or not np.isfinite(f).all() or np.any(d<0):raise ValueError('bounded nonnegative field required')
 r=np.minimum(d/radius,1);support=(1-r)**4*(4*r+1);out=f*(1-.5*support);out[fixed]=f[fixed]
 return out

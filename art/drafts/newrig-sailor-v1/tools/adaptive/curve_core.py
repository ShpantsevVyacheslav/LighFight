"""Offline adaptive exact-source sampling core; no Blender/Unity or asset mutations.
All matrices are in one declared world basis. Full positive-LBS surface bound,
not a reduced-vertex metric. FBX/Unity interpolation still requires reimport proof.
"""
from dataclasses import dataclass
from fractions import Fraction
import numpy as np

def compose(trs):
    a=np.asarray(trs,dtype=np.float64);q=a[...,3:7];norm=np.linalg.norm(q,axis=-1,keepdims=True)
    if np.any(~np.isfinite(a)) or np.any(norm<=1e-12):raise ValueError('Finite nonzero source quaternions required')
    q=q/norm;w,x,y,z=np.moveaxis(q,-1,0)
    R=np.stack((1-2*(y*y+z*z),2*(x*y-z*w),2*(x*z+y*w),2*(x*y+z*w),1-2*(x*x+z*z),2*(y*z-x*w),2*(x*z-y*w),2*(y*z+x*w),1-2*(x*x+y*y)),axis=-1).reshape(a.shape[:-1]+(3,3));out=np.zeros(a.shape[:-1]+(4,4));out[...,:3,:3]=R*a[...,None,7:10];out[...,:3,3]=a[...,:3];out[...,3,3]=1;return out

def interpolate(a,b,u):
    a=np.asarray(a,dtype=np.float64);b=np.asarray(b,dtype=np.float64);out=a*(1-u)+b*u;qa=a[...,3:7];qb=b[...,3:7].copy();na=np.linalg.norm(qa,axis=-1,keepdims=True);nb=np.linalg.norm(qb,axis=-1,keepdims=True)
    if np.any(~np.isfinite(a)) or np.any(~np.isfinite(b)) or np.any(na<=1e-12) or np.any(nb<=1e-12):raise ValueError('Finite nonzero interpolation quaternions required')
    qa=qa/na;qb=qb/nb;qb=np.where((np.sum(qa*qb,axis=-1)<0)[...,None],-qb,qb);q=qa*(1-u)+qb*u;out[...,3:7]=q/np.linalg.norm(q,axis=-1,keepdims=True);return out

@dataclass
class Skeleton:
    parents: np.ndarray
    rest: np.ndarray  # absolute armature-space rest, every exported bone
    armature_world: np.ndarray
    mesh_world: np.ndarray
    inherit_scale: tuple
    def __post_init__(self):
        n=len(self.parents)
        if len(self.inherit_scale)!=n or any(v!='FULL' for v in self.inherit_scale):raise ValueError('Explicit FULL-scale exported hierarchy required; other inheritance must be modeled, never ignored')
        if any(p>=i or p < -1 for i,p in enumerate(self.parents)):raise ValueError('Parent-first hierarchy required')
        if np.asarray(self.rest).shape!=(n,4,4):raise ValueError('Every exported bone needs authoritative rest')
        self.inv_rest=np.linalg.inv(self.rest);self.inv_armature=np.linalg.inv(self.armature_world)
    def world_pose(self,trs):
        basis=compose(trs);out=np.empty_like(basis)
        for i,p in enumerate(self.parents):out[i]=self.rest[i]@basis[i] if p<0 else out[p]@self.inv_rest[p]@self.rest[i]@basis[i]
        return self.armature_world@out
    def skin(self,world_pose):return world_pose@self.inv_rest@self.inv_armature@self.mesh_world

@dataclass
class InfluenceBounds:
    corners: np.ndarray
    active: np.ndarray
    max_weight_sum: float
    @classmethod
    def from_all_original_vertices(cls,positions,joints,weights,bone_count):
        p=np.asarray(positions,dtype=np.float64);j=np.asarray(joints);w=np.asarray(weights,dtype=np.float64)
        if p.ndim!=2 or p.shape[1]!=3 or j.shape!=w.shape or len(j)!=len(p):raise ValueError('Full indexed source positions/joints/weights required')
        if np.any(~np.isfinite(p)) or np.any(~np.isfinite(w)) or np.any(w<0) or np.any(w.sum(1)<=0):raise ValueError('Finite nonnegative weighted source required')
        if np.any(j<0) or np.any(j>=bone_count):raise ValueError('Bone order mismatch')
        lo=np.full((bone_count,3),np.inf);hi=np.full((bone_count,3),-np.inf)
        for k in range(j.shape[1]):
            valid=w[:,k]>0;np.minimum.at(lo,j[valid,k],p[valid]);np.maximum.at(hi,j[valid,k],p[valid])
        active=np.isfinite(lo).all(1);lo[~active]=0;hi[~active]=0;bits=np.array([[x,y,z] for x in (0,1) for y in (0,1) for z in (0,1)]);xyz=np.where(bits[None,:,:],hi[:,None,:],lo[:,None,:]);corners=np.concatenate((xyz,np.ones(xyz.shape[:-1]+(1,))),axis=-1)
        return cls(corners,active,float(w.sum(1).max()))
    def error_bound(self,source_skin,candidate_skin):
        d=np.asarray(candidate_skin)-np.asarray(source_skin);v=np.einsum('bij,bkj->bki',d,self.corners)[...,:3];return float(np.linalg.norm(v[self.active],axis=-1).max(initial=0)*self.max_weight_sum)

def polar_frames(world):
    u,_,v=np.linalg.svd(np.asarray(world)[...,:3,:3]);return u@v

def bone_angle_deg(source_world,candidate_world):
    # Keep O(3) polar frames: common handedness reflection cancels in relative SO(3).
    a=polar_frames(source_world);b=polar_frames(candidate_world);r=np.swapaxes(a,-1,-2)@b;xyz=np.stack((r[...,2,1]-r[...,1,2],r[...,0,2]-r[...,2,0],r[...,1,0]-r[...,0,1]),-1);s=np.linalg.norm(xyz,axis=-1)*.5;c=np.clip((np.trace(r,axis1=-2,axis2=-1)-1)*.5,-1,1);return float(np.degrees(np.arctan2(s,c)).max(initial=0))

@dataclass
class SourceSample:
    local_trs: np.ndarray
    world_pose: np.ndarray
    world_skin: np.ndarray

class AdaptiveSampler:
    def __init__(self,skeleton,bounds,source,surface_limit=.0001,angle_limit=.01,max_depth=12,max_samples=50000):
        self.skeleton=skeleton;self.bounds=bounds;self.source=source;self.surface_limit=surface_limit;self.angle_limit=angle_limit;self.max_depth=max_depth;self.max_samples=max_samples;self.cache={};self.observations=[]
    def sample(self,t):
        t=Fraction(t)
        if t not in self.cache:
            if len(self.cache)>=self.max_samples:raise RuntimeError('Bounded source-sample budget exceeded; no silent acceptance')
            truth=self.source(t);represented=self.skeleton.world_pose(truth.local_trs)
            if self.bounds.error_bound(truth.world_skin,self.skeleton.skin(represented))>self.surface_limit or bone_angle_deg(truth.world_pose,represented)>self.angle_limit:raise RuntimeError('Source localTRS representation already exceeds accuracy; additional keys cannot fix it')
            self.cache[t]=truth
        return self.cache[t]
    def fit(self,authored_times):
        keys=sorted(set(map(Fraction,authored_times)))
        if len(keys)<2:raise ValueError('Exact authored start/end required')
        accepted=set(keys)
        def interval(a,b,depth):
            sa,sb=self.sample(a),self.sample(b);failed=False
            for u in (Fraction(1,4),Fraction(1,2),Fraction(3,4)):
                t=a+(b-a)*u;truth=self.sample(t);world=self.skeleton.world_pose(interpolate(sa.local_trs,sb.local_trs,float(u)));surface=self.bounds.error_bound(truth.world_skin,self.skeleton.skin(world));angle=bone_angle_deg(truth.world_pose,world);self.observations.append((t,surface,angle));failed|=surface>self.surface_limit or angle>self.angle_limit
            if failed:
                if depth>=self.max_depth:raise RuntimeError('Unresolved interval at depth limit; no silent acceptance')
                mid=(a+b)/2;accepted.add(mid);interval(a,mid,depth+1);interval(mid,b,depth+1)
        for a,b in zip(keys,keys[1:]):interval(a,b,0)
        return sorted(accepted)

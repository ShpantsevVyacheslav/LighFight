"""No clipping, pruning or influence cap. Float64 field, explicit float32 transport gate."""
import numpy as np

def reconstruct(bary, neighbor_weights, distances, sigma, stable_columns, low=.75, high=.95):
 assert 0<low<high<=1 and sigma>0
 assert np.all(np.isfinite(bary)) and np.all(bary>=0)
 assert np.all(neighbor_weights>=0) and np.all(np.isfinite(distances))
 radius=distances[:,-1:];assert np.all(radius>0),'Degenerate48NN support'
 taper=np.maximum(1-(distances/radius)**2,0)**2
 factors=np.exp(-.5*(distances/sigma)**2)*taper
 assert np.all(factors.sum(1)>0),'No field support'
 factors/=factors.sum(1)[:,None]
 smooth=np.einsum('nk,nkg->ng',factors,neighbor_weights)
 stable=bary[:,stable_columns].sum(1)
 blend=np.clip((high-stable)/(high-low),0,1)
 result=bary*(1-blend[:,None])+smooth*blend[:,None]
 total=result.sum(1);assert np.all(total>0) and np.all(np.isfinite(result))
 result/=total[:,None]
 stored=result.astype(np.float32)
 assert not np.any((result>0)&(stored==0)),'Positive weight underflow on float32 transport'
 return stored,stable,blend

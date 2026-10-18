"""Portable independent handedness regression. No DCC, source writes or assets.
Reflection maps a normal-map basis N/T/B as vectors; its tangent handedness
changes sign. Omitting that sign flips the green-axis contribution. This test
is a mathematical prerequisite, not evidence of actual importer parity.
"""
import numpy as np

def run(mutate=False):
    # Canonical Blender->Unity importer coordinate witness: ReflectX*B.
    c=np.array([[-1.,0,0],[0,0,1],[0,-1,0]])
    assert np.linalg.det(c)==-1
    rng=np.random.default_rng(102018)
    count=0
    for _ in range(100):
        q,_=np.linalg.qr(rng.normal(size=(3,3)))
        n,t=q[:,0],q[:,1]
        for sign in [-1.,1.]:
            b=np.cross(n,t)*sign
            cn,ct,cb=c@n,c@t,c@b
            imported_sign=sign if mutate else -sign
            constructed=np.cross(cn,ct)*imported_sign
            assert np.linalg.norm(constructed-cb)<1e-12, 'Reflection tangent.w omission reverses bitangent'
            for normal in [np.array([.3,.4,np.sqrt(.75)]),np.array([0,1.,0]),np.array([0,0,1.])]:
                a=c@(t*normal[0]+b*normal[1]+n*normal[2])
                z=ct*normal[0]+constructed*normal[1]+cn*normal[2]
                assert np.linalg.norm(a-z)<1e-12,'Reflected normal-map world direction changed'
                count+=1
    return count

if __name__=='__main__':
    print('GREEN',run(),'normal-direction cases')
    try: run(True)
    except AssertionError as error: print('RED wrong tangent.w mutation rejected:',error)
    else: raise AssertionError('Mutation unexpectedly passed')

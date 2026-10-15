"""Independent triangle/box SAT acceptance + intentional intrusion mutation."""
import json,math
from pathlib import Path
ROOT=Path(__file__).resolve().parents[3]
def sub(a,b):return tuple(x-y for x,y in zip(a,b))
def dot(a,b):return sum(x*y for x,y in zip(a,b))
def cross(a,b):return (a[1]*b[2]-a[2]*b[1],a[2]*b[0]-a[0]*b[2],a[0]*b[1]-a[1]*b[0])
def intersects(triangle):
    center=(0,1.45,0);half=(5.5,1.4,2)
    points=[sub(p,center) for p in triangle];edges=[sub(points[(i+1)%3],points[i]) for i in range(3)]
    box=[(1,0,0),(0,1,0),(0,0,1)]
    axes=box+[cross(edges[0],edges[1])]+[cross(e,b) for e in edges for b in box]
    for axis in axes:
        if dot(axis,axis)<1e-18:continue
        values=[dot(v,axis) for v in points];radius=sum(abs(a)*h for a,h in zip(axis,half))
        if min(values)>radius+1e-8 or max(values)<-radius-1e-8:return False
    return True

def main():
    p=ROOT/'art/generated/courtyard-foliage/canopy-cards-v1.json';data=json.loads(p.read_text())
    vertices=[tuple(v[x] for x in ['x','y','z']) for v in data['vertices']];indices=data['triangles']
    assert all(math.isfinite(v) for p in vertices for v in p)
    assert len(vertices)==2016 and len(indices)==3024 and all(0<=i<len(vertices) for i in indices)
    hits=[i//3 for i in range(0,len(indices),3) if intersects([vertices[j] for j in indices[i:i+3]])]
    assert not hits,hits
    assert intersects([(-.5,.5,0),(.5,.5,0),(.5,1.5,0)]),'intrusion mutation was not detected'
    # Boundary and far fixtures prevent an always-Clear or always-Hit checker.
    assert intersects([(-.5,.5,2),(.5,.5,2),(.5,1.5,2)])
    assert not intersects([(-.5,.5,2.01),(.5,.5,2.01),(.5,1.5,2.01)])
    report={'passed':True,'trianglesTested':len(indices)//3,'fightVolumeIntersections':0,'intrusionMutationRejected':True,'boundaryAndFarFixturesPassed':True,'scope':'static exact triangle-vs-box; not dynamic actor or GPU acceptance'}
    (p.parent/'geometry-independent-verification-v1.json').write_text(json.dumps(report,indent=2));print(report)
if __name__=='__main__':main()

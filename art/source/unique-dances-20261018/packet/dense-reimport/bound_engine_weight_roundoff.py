"""Conservative all-source-vertex weight-only bound; CPU, no DCC/Unity.

For each frozen source pose and every bone, transform all eight corners of the
full original bind-vertex world AABB. Norm convexity bounds every source vertex
under every influence. Thus ||sum(delta_w * transformed_point)|| <=4*max_delta*R.
No sparse body sampling; no Animator/24Hz interpolation/GPU claim.
"""
from array import array
from pathlib import Path
import ast, csv, itertools, json, math, re, struct, sys, zipfile

def multiply(a,b): return [[sum(a[i][k]*b[k][j] for k in range(4)) for j in range(4)] for i in range(4)]
def inverse(m):
    a=[list(row)+[float(i==j) for j in range(4)] for i,row in enumerate(m)]
    for j in range(4):
        k=max(range(j,4),key=lambda i:abs(a[i][j])); a[j],a[k]=a[k],a[j]
        denominator=a[j][j]; assert abs(denominator)>1e-12
        a[j]=[v/denominator for v in a[j]]
        for i in range(4):
            if i!=j:
                scale=a[i][j];a[i]=[x-scale*y for x,y in zip(a[i],a[j])]
    return [row[4:] for row in a]
def point(m,p):return [sum(m[i][k]*p[k] for k in range(3))+m[i][3] for i in range(3)]
def run(packet,audit,output):
    packet,audit,output=Path(packet),Path(audit),Path(output)
    text=audit.read_text()
    assert 'lostNamedPositiveInfluences=0 unexpectedNamedInfluences=0 verticesWithCountMismatch=0' in text
    delta=float(re.search(r'maxAbsoluteWeightDelta=([^ ]+)',text)[1])
    with zipfile.ZipFile(packet/'original-indexed-attributes.npz') as z:
        def read(name):
            f=z.open(name+'.npy');assert f.read(6)==b'\x93NUMPY';major,minor=f.read(2);size=struct.unpack('<H' if major==1 else '<I',f.read(2 if major==1 else 4))[0];h=ast.literal_eval(f.read(size).decode());v=array({'<f4':'f','<f8':'d','<i4':'i'}[h['descr']]);v.frombytes(f.read());return v
        vertices=read('positionsBlenderLocal');mesh=read('meshWorldBlender');rig=read('armatureWorldBlender');rest=read('DEFRestArmature')
    mesh=[list(mesh[i:i+4]) for i in range(0,16,4)];rig=[list(rig[i:i+4]) for i in range(0,16,4)]
    minimum=[min(vertices[k::3]) for k in range(3)];maximum=[max(vertices[k::3]) for k in range(3)]
    B=[[1,0,0,0],[0,0,1,0],[0,-1,0,0],[0,0,0,1]];Bi=inverse(B)
    corners=[point(B,point(mesh,p)) for p in itertools.product(*zip(minimum,maximum))]
    ref=json.loads((packet/'pose-reference.json').read_text())
    rest_inverse=[]
    for i in range(len(ref['boneNames'])):
        r=[list(rest[i*16+j:i*16+j+4]) for j in range(0,16,4)]
        rest_inverse.append(inverse(multiply(multiply(multiply(B,rig),r),Bi)))
    rows=[]
    for pose in ref['references']:
        radius=0.;witness=None
        for bone,world in enumerate(pose['boneWorldUnityYUp']):
            transform=multiply(world,rest_inverse[bone])
            for corner_id,corner in enumerate(corners):
                p=point(transform,corner);norm=math.sqrt(sum(x*x for x in p))
                if norm>radius:radius=norm;witness=[ref['boneNames'][bone],corner_id]
        bound=4*delta*radius
        rows.append({'clip':pose['clip'],'phase':pose['phase'],'sourceFrame':pose['frame'],'maxAllBoneAABBSupportNormM':radius,'measuredMaxWeightDelta':delta,'maxInfluences':4,'allOriginalVerticesWeightOnlyErrorBoundM':bound,'boundMM':bound*1000,'supportWitness':witness})
    result={'scope':'Analytic all-original-vertex weight-only bound at15 frozen source key poses; actual Generic Animator/GPU/24Hz interpolation separate',
            'sourceVertices':len(vertices)//3,'originalBindLocalAABB':[minimum,maximum],
            'allNamedReadbackPresent':True,'inputSourceWeights':'Original IEEE values retained in raw FBX; desired API writes original weights. Measured engine Float32 readback differences only.',
            'proof':'For every actual vertex in original AABB and each of136 bones, affine-transformed point lies in transformed eight-corner convex hull. Norm maximum over hull is bounded by max corner norm R. Triangle inequality gives <=4*maxAbsDeltaWeight*R. Full geometry position error and floating evaluation error not included.',
            'coordinate':'CanonicalB source space; fixed ReflectX orthogonal importer conversion leaves norms/bounds unchanged. Actor rootidentity; additional translation/scaling requires its corresponding norm bound.',
            'poses':rows,'maxWeightOnlyBoundM':max(r['allOriginalVerticesWeightOnlyErrorBoundM'] for r in rows)}
    result['status']='PASS_SOURCE_POSE_CPU_WEIGHT_ONLY_BOUND' if result['maxWeightOnlyBoundM']<=.0001 else 'RED'
    output.write_text(json.dumps(result,indent=2)+'\n');print(json.dumps({k:v for k,v in result.items() if k!='poses'},indent=2))
    assert result['status']=='PASS_SOURCE_POSE_CPU_WEIGHT_ONLY_BOUND'

if __name__=='__main__':run(*sys.argv[1:])

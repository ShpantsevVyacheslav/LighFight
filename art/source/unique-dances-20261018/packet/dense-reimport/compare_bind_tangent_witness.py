"""CPU-only fixed-basis comparison, no fitted transforms or source edits.
Usage: python compare_bind_tangent_witness.py source.json unity.bin output.json
Requires bundled numpy. Shader/render acceptance remains independent.
"""
import sys,json,struct,time
from pathlib import Path
import numpy as np

def unit(a): return a/np.linalg.norm(a,axis=-1,keepdims=True)
def angle(a,b): return float(np.degrees(np.arctan2(np.linalg.norm(np.cross(a,b)),np.dot(a,b))))
def main(source_path,binary_path,output_path):
    started=time.perf_counter();source=json.loads(Path(source_path).read_text())
    with open(binary_path,'rb') as f:
        assert f.read(4)==b'DTB1';count=struct.unpack('<i',f.read(4))[0]
        data=np.frombuffer(f.read(count*48),'<f4').reshape(count,12).astype(np.float64)
        indices=struct.unpack('<i',f.read(4))[0];faces=np.frombuffer(f.read(indices*4),'<i4').reshape(-1,3)
        uw=np.frombuffer(f.read(64),'<f4').reshape(4,4).astype(np.float64);assert not f.read(1)
    # The SINGLE fixed importer conversion measured independently on all136 binds.
    c=np.array([[-1.,0,0],[0,0,1],[0,-1,0]])
    sw=np.array(source.get('meshWorld',source.get('meshWorldBlender')),np.float64);sp=sw[:3,:3];up=uw[:3,:3]
    world=data[:,:3]@up.T+uw[:3,3]
    normals=unit(data[:,3:6]@np.linalg.inv(up))
    tangents=unit(data[:,6:9]@up.T)
    # Standard shader accounts for the object's negative scale separately.
    bitangents=np.cross(normals,tangents)*data[:,9:10]*np.sign(np.linalg.det(up))
    packet=Path(source_path).parent/'original-indexed-attributes.npz'
    original=np.load(packet) if packet.exists() else None
    source_positions=original['positionsBlenderLocal'] if original else None
    source_faces=original['triangles'].reshape(-1,3) if original else None
    source_uv=original['loopUV'].reshape(-1,3,2) if original else None
    rows=[]
    for supplied in source['samples']:
        witness=dict(supplied)
        for old,new in [('positionMeshLocal','positionLocal'),('UV','uv'),('normalMeshLocal','normalLocal'),('tangentMeshLocal','tangentLocal'),('bitangentMeshLocal','bitangentLocal'),('originalVertexIndex','vertexIndex'),('originalLoopIndex','loopIndex')]:
            if old not in witness: witness[old]=witness[new]
        pos=c@(sp@np.array(witness['positionMeshLocal'])+sw[:3,3]);uv=np.array(witness['UV'])
        # UV candidates first, then position, retaining every coincident normal split.
        candidate=np.flatnonzero(np.max(np.abs(data[:,10:12]-uv),axis=1)<=2e-6)
        assert len(candidate),'No UV correspondence'
        distance=np.linalg.norm(world[candidate]-pos,axis=1);best=float(distance.min())
        assert best<=1e-6,('Original position correspondence RED',witness['originalVertexIndex'],best)
        candidate=candidate[distance<=best+1e-8]
        if original is not None:
            fi=witness['originalTriangleIndex']; actual_face=faces[fi]
            expected=(source_positions[source_faces[fi]]@sp.T+sw[:3,3])@c.T
            # Validate exact original facet membership before selecting a UV seam.
            for ai in actual_face:
                matches=(np.linalg.norm(expected-world[ai],axis=1)<=1e-6)&(np.max(np.abs(source_uv[fi]-data[ai,10:12]),axis=1)<=2e-6)
                assert matches.any(),('Triangle ordering/context differs: no silent seam choice',fi)
            candidate=candidate[np.isin(candidate,actual_face)]
            assert len(candidate),('Source corner missing from corresponding original triangle',fi)
        sn=unit(c@np.linalg.inv(sp).T@np.array(witness['normalMeshLocal']))
        st=unit(c@sp@np.array(witness['tangentMeshLocal']))
        sb=unit(c@sp@np.array(witness['bitangentMeshLocal']))
        # A position+UV can have several source normal splits: report all options,
        # choose only the smallest normal error (never fitting a tangent direction).
        chosen=min(candidate,key=lambda i:angle(normals[i],sn));n=normals[chosen];t=tangents[chosen];b=unit(bitangents[chosen])
        rows.append(dict(region=witness['region'],originalLoop=witness['originalLoopIndex'],originalVertex=witness['originalVertexIndex'],unityVertex=int(chosen),candidateCount=len(candidate),positionErrorM=best,normalAngleDeg=angle(n,sn),tangentAngleDeg=angle(t,st),bitangentAngleDeg=angle(b,sb),unityTangentW=float(data[chosen,9]),sourceBitangentSign=witness['bitangentSign'],sourceWorldBasisDet=float(np.linalg.det(np.column_stack([st,sb,sn]))),unityWorldBasisDet=float(np.linalg.det(np.column_stack([t,b,n]))),normalTangentDot=float(n@t)))
    result=dict(status='DIAGNOSTIC_FIXED_BASIS_NO_RENDER_ACCEPTANCE',source=str(source_path),unity=str(binary_path),vertices=count,sourceCount=len(rows),coordinateConversion=c.tolist(),noMovingFit=True,sourceGeometryUnchanged=True,seconds=time.perf_counter()-started,maxPositionM=max(r['positionErrorM'] for r in rows),maxNormalDeg=max(r['normalAngleDeg'] for r in rows),maxTangentDeg=max(r['tangentAngleDeg'] for r in rows),maxBitangentDeg=max(r['bitangentAngleDeg'] for r in rows),samples=rows)
    Path(output_path).write_text(json.dumps(result,indent=2));print(json.dumps({k:v for k,v in result.items() if k!='samples'}))
if __name__=='__main__':main(*sys.argv[1:])

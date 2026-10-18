"""Independent CPU-only full original NPZ versus combined FBX attribute gate.
No DCC, no index assumption across Unity: this checks the file BEFORE Unity.
"""
from array import array
from pathlib import Path
import ast, json, struct, sys, zipfile, zlib
sys.path.insert(0, str(Path(__file__).resolve().parents[1] / "tools"))
from audit_fbx_bind_girdle import FBX, Node

class TypedFBX(FBX):
    def prop(self):
        token = self.b[self.o:self.o+1]
        if token in (b"f", b"d", b"i", b"l", b"b", b"c"):
            self.o += 1
            count, encoding, size = struct.unpack("<III", self.take(12))
            blob = self.take(size)
            if encoding: blob = zlib.decompress(blob)
            result = array({b"f":"f", b"d":"d", b"i":"i", b"l":"q", b"b":"b", b"c":"b"}[token])
            result.frombytes(blob)
            assert len(result) == count
            return result
        return super().prop()
    def node(self):
        start = self.o
        end, count, size = struct.unpack("<QQQ" if self.v >= 7500 else "<III", self.take(24 if self.v >= 7500 else 12))
        name_size = self.take(1)[0]
        if not end: return None
        name = self.take(name_size).decode()
        self.o = start
        if name.startswith("Animation") or name in ("LayerElementNormal", "LayerElementTangent", "LayerElementBinormal"):
            self.o = end
            return Node(name, [], [])
        return super().node()

def run(packet, output, model="SailorOriginalDenseDEF-CombinedReview24.fbx"):
    packet = Path(packet)
    def npy(archive, name):
        with archive.open(name + ".npy") as stream:
            assert stream.read(6) == b"\x93NUMPY"
            major, minor = stream.read(2)
            size = struct.unpack("<H" if major == 1 else "<I", stream.read(2 if major == 1 else 4))[0]
            header = ast.literal_eval(stream.read(size).decode("latin1"))
            assert not header["fortran_order"]
            values = array({"<f4":"f", "<i4":"i", "<f8":"d"}[header["descr"]])
            values.frombytes(stream.read())
            return values
    with zipfile.ZipFile(packet / "original-indexed-attributes.npz") as archive:
        positions = npy(archive,"positionsBlenderLocal")
        triangles = npy(archive,"triangles")
        uv = npy(archive,"loopUV")
        joints = npy(archive,"jointIndices")
        weights = npy(archive,"weights")
    ref = json.loads((packet / "pose-reference.json").read_text())
    names = ref["boneNames"]
    nodes = TypedFBX(packet / model).all()
    objects = next(n for n in nodes if n.name == "Objects").c
    connections = next(n for n in nodes if n.name == "Connections").c
    geometry = [n for n in objects if n.name == "Geometry"]
    assert len(geometry) == 1
    geometry = geometry[0]
    verts = next(n.p[0] for n in geometry.c if n.name == "Vertices")
    faces = next(n.p[0] for n in geometry.c if n.name == "PolygonVertexIndex")
    layer = next(n for n in geometry.c if n.name == "LayerElementUV")
    uv_values = next(n.p[0] for n in layer.c if n.name == "UV")
    uv_indices = next(n.p[0] for n in layer.c if n.name == "UVIndex")
    assert len(verts) == len(positions)
    position_error = max(abs(a-b) for a,b in zip(verts,positions))
    assert len(faces) == len(triangles)
    face_mismatches = sum(((-value-1) if value < 0 else value) != triangles[i] for i,value in enumerate(faces))
    assert all((value < 0) == (i%3 == 2) for i,value in enumerate(faces))
    assert len(uv_indices)*2 == len(uv)
    uv_error = max(max(abs(uv_values[index*2]-uv[i*2]), abs(uv_values[index*2+1]-uv[i*2+1])) for i,index in enumerate(uv_indices))
    models = {n.p[0]:n.p[1].split("\0")[0] for n in objects if n.name == "Model"}
    clusters = {n.p[0]:n for n in objects if n.name == "Deformer" and n.p[-1] == "Cluster"}
    cluster_names = {n.p[2]:models[n.p[1]] for n in connections if n.name == "C" and n.p[0] == "OO" and n.p[1] in models and n.p[2] in clusters}
    seen = bytearray(len(weights)); weight_error = 0.; exported_count = 0
    for identifier, cluster in clusters.items():
        bone = names.index(cluster_names[identifier])
        indices = next(n.p[0] for n in cluster.c if n.name == "Indexes")
        values = next(n.p[0] for n in cluster.c if n.name == "Weights")
        for vertex,value in zip(indices,values):
            slot = next(k for k in range(vertex*4,vertex*4+4) if joints[k] == bone and weights[k] > 0)
            assert not seen[slot]
            seen[slot] = 1; exported_count += 1
            weight_error = max(weight_error,abs(value-weights[slot]))
    source_count = sum(w > 0 for w in weights)
    missing = sum(w > 0 and not seen[i] for i,w in enumerate(weights))
    result = {"scope":"Full original NPZ versus explicit FBX BEFORE Unity; no Unity-order assumption", "model":model,
              "sourceVertices":len(positions)//3,"triangles":len(triangles)//3,"cornerUVCount":len(uv)//2,
              "vertexComponentMaxError":position_error,"triangleIndexMismatches":face_mismatches,
              "cornerUVComponentMaxError":uv_error,"originalInfluences":source_count,"exportedInfluences":exported_count,
              "missingInfluences":missing,"weightMaxError":weight_error,
              "sourceNormalsTangents":"Not compared by this gate; independent original loop N/T/B oracle and explicit importer mode required"}
    result["status"] = "PASS_RAW_ATTRIBUTES" if position_error == 0 and face_mismatches == 0 and uv_error == 0 and weight_error == 0 and missing == 0 and source_count == exported_count else "RED"
    Path(output).write_text(json.dumps(result,indent=2)+"\n")
    print(json.dumps(result,indent=2))
    assert result["status"] == "PASS_RAW_ATTRIBUTES"

if __name__ == "__main__": run(*sys.argv[1:])

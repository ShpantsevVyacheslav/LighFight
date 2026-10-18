"""CPU-only, lossless sidecar conversion. Does not stage/import any Unity model.

Copies original NPY payload bytes (no coordinate or weight conversion) into a
simple local QA stream; reference JSON doubles remain doubles. Run again after
the owner's final combined-FBX handoff, so no stale oracle is silently reused.
"""
from pathlib import Path
import argparse, ast, hashlib, json, struct, zipfile

FIELDS = ("positionsBlenderLocal", "triangles", "loopUV", "jointIndices",
          "weights", "DEFRestArmature", "meshWorldBlender", "armatureWorldBlender")

def convert(packet, output):
    packet, output = Path(packet), Path(output)
    output.parent.mkdir(parents=True, exist_ok=True)
    ref = json.loads((packet / "pose-reference.json").read_text())
    hashes = {}
    with output.open("wb") as out, zipfile.ZipFile(packet / "original-indexed-attributes.npz") as archive:
        out.write(b"DRQ1")
        for field in FIELDS:
            with archive.open(field + ".npy") as stream:
                assert stream.read(6) == b"\x93NUMPY"
                major, minor = stream.read(2)
                size = struct.unpack("<H" if major == 1 else "<I", stream.read(2 if major == 1 else 4))[0]
                header = ast.literal_eval(stream.read(size).decode("latin1"))
                assert not header["fortran_order"]
                desc = header["descr"].encode("ascii")
                shape = header["shape"]
                out.write(struct.pack("<i", len(desc))); out.write(desc)
                out.write(struct.pack("<i", len(shape)))
                for n in shape: out.write(struct.pack("<i", n))
                item_size = int(header["descr"][2:])
                count = item_size
                for n in shape: count *= n
                out.write(struct.pack("<q", count))
                remaining = count; digest = hashlib.sha256()
                while remaining:
                    chunk = stream.read(min(1024 * 1024, remaining))
                    assert chunk, field
                    out.write(chunk); digest.update(chunk); remaining -= len(chunk)
                assert not stream.read(1), field
                hashes[field] = digest.hexdigest()
        def text(value):
            value = value.encode("utf8"); out.write(struct.pack("<i", len(value))); out.write(value)
        names = ref["boneNames"]
        out.write(struct.pack("<i", len(names)))
        for name in names: text(name)
        ids = ref["sampleOriginalVertexIndices"]
        out.write(struct.pack("<i", len(ids)))
        out.write(struct.pack("<" + "i" * len(ids), *ids))
        out.write(struct.pack("<i", len(ref["references"])))
        for pose in ref["references"]:
            text(pose["clip"]); out.write(struct.pack("<dd", pose["phase"], pose["frame"]))
            for point in pose["positionsUnityYUp"]: out.write(struct.pack("<ddd", *point))
            for matrix in pose["boneWorldUnityYUp"]:
                for row in matrix: out.write(struct.pack("<dddd", *row))
    report = {"status": "SIDECAR_ONLY_NOT_UNITY_VALIDATED", "sourceNPZ": str(packet / "original-indexed-attributes.npz"),
              "sourceReferenceSHA256": hashlib.sha256((packet / "pose-reference.json").read_bytes()).hexdigest(),
              "rawArraySHA256": hashes, "output": str(output), "bytes": output.stat().st_size,
              "boneCount": len(names), "referenceVertices": len(ids), "referencePoses": len(ref["references"]),
              "conversion": "Original IEEE array payloads unchanged; JSON oracle numbers written as IEEE float64; no skin/coordinate/weight changes"}
    output.with_suffix(".json").write_text(json.dumps(report, indent=2) + "\n")
    print(json.dumps(report, indent=2))

if __name__ == "__main__":
    parser = argparse.ArgumentParser(); parser.add_argument("packet"); parser.add_argument("output")
    args = parser.parse_args(); convert(args.packet, args.output)

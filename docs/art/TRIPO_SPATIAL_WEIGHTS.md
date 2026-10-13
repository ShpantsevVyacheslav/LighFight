# Reproduce full-density Rose spatial weights

`reconstruct_spatial_weights.py` is a standalone Blender **4.2 LTS** script. It reads the untouched original GLB and downloaded native FBX directly, requires no earlier `.blend` candidate, and uses only Blender's bundled NumPy/mathutils and Python standard library. It does not launch Unity, call paid APIs, upload anything, or edit the inputs/main project. The visually reviewed method is unchanged.

Run from the LighFight workspace with a **new or empty output directory**:

```sh
/Users/macbook1/Applications/Blender.app/Contents/MacOS/Blender \
  -b --factory-startup --python-exit-code 1 \
  -P scripts/art/reconstruct_tripo_spatial_weights.py -- \
  --source art/source/rose-tripo-v1/original.glb \
  --donor art/source/rose-tripo-v1/unity-pack/tripo_convert_fbd6ea7e-51f1-4d2b-862f-11e9d15b1b6e.fbx \
  --out .tools/tripo-dense-review/my-reconstruction \
  --export-fbx
```

Paths may be absolute. `--factory-startup` avoids user add-ons; `--python-exit-code 1` ensures a failed preservation/alignment gate returns a failure status. The script rejects other Blender versions because later action APIs require separate validation. The tested fresh-source run took approximately **368 seconds** on this machine. Allow several GB of memory for original mesh, rejected baseline, weight matrices, and 4K images.

## Parameters

| Argument | Default | Meaning |
|---|---:|---|
| `--source` | required | Untouched GLB with exactly one unrigged, UV-mapped mesh and embedded images. |
| `--donor` | required | Native FBX with exactly one mesh and its existing armature/weights/actions. |
| `--out` | required | Output directory; may not contain either input file. |
| `--overwrite` | off | Permit rerunning into a nonempty output only when its `manifest.json` has this script's schema. Unowned directories and linked output files are refused. No recursive deletion occurs. |
| `--sigma-height` | `0.007` | Fixed Gaussian bandwidth divided by measured original Z height. |
| `--neighbors` | `48` | Donor rest vertices considered for each source vertex. Furthest support is tapered to zero. |
| `--min-weight` | `1e-6` | Drop smaller reconstructed weights, then normalize. No four-influence truncation. |
| `--stable-low` | `0.75` | At/below this total stable-bone influence, use the full spatial field. |
| `--stable-high` | `0.95` | At/above this stable influence, preserve direct barycentric weights. Linear blend between these thresholds. |
| `--batch-size` | `5000` | Memory/processing batch size; leaves method unchanged. |
| `--max-align-p99-height` | `0.001` | Fail when p99 closest donor rest-surface distance exceeds 0.1% of original height. |
| `--max-align-max-height` | `0.002` | Fail when maximum rest distance exceeds 0.2% of original height. |
| `--metrics-samples` | `50000` | Approximate number of dense vertices sampled for posed donor-surface distances. Edge strain uses all nondegenerate edges. |
| `--pose` | four default samples | Repeatable `action:frame`. Defaults: `box_02:24`, `box_02:44`, `front_kick_01:24`, `front_kick_01:34`. Frames refer to Blender's imported native action. |
| `--export-fbx` | off | Also export a rig-only FBX. It does not bake native actions. |

Stable group names contain head/neck/hand/finger/thumb/index/middle/ring/pinky/little/foot/toe, case-insensitively. The taper is `exp(-0.5*(distance/sigma)^2) * max(1-(distance/farthest)^2,0)^2`, normalized over support. Source vertices with high stable-bone influence keep the baseline; other vertices use the reconstructed field. Parameters beyond the defaults are experiments and require renewed visual review.

## Gates and outputs

The native armature is switched to actual Blender `REST` mode and animation data detached **before** fitting/querying weights. No first animation frame is treated as bind pose, and the script never aligns by relocating bones, scaling geometry, changing pose, or reshaping the source. Failure of the rest-distance gate aborts reconstruction.

The script extracts original embedded image buffers byte-for-byte; measures the full original/donor rest alignment; constructs the rejected closest-triangle barycentric baseline; builds the continuous spatial field; and measures spatial/baseline/donor pose strain and dense-to-donor surface distances. Baseline and candidate need separate mesh datablocks because Blender's deform weights reside on mesh data. The rejected baseline is removed before saving the final candidate.

Exact preservation gates run before save and after reopening:

- Source vertex positions, all corner UV layers, topology/corner indices, polygon sizes, material indices, and object transform.
- Native bone rest matrices, names/parents and armature object transform.
- Original material node/link graph, image bytes, image sizes and color spaces.
- Zero unweighted vertices and normalized influences within float32 tolerance.
- Original source/donor file SHA256 unchanged; extracted image file SHA256 unchanged.

Outputs are:

- `rose-original-dense-native-spatial.blend`: unchanged original geometry/material/maps, reconstructed weights, native skeleton, original imported actions and hidden donor.
- `original-image-*.jpg/png`: raw original images, with original names/texture semantics in the manifest. For this Rose GLB, image0 is basecolor, image1 is packed roughness/metallic (G/B), image2 is normal; all are 4096×4096.
- `manifest.json`: source SHA256s, exact preservation signatures, parameters, weights, rest alignment, original material/texture metadata, native action samples, baseline/donor/spatial strain and surface-distance distributions.
- Optional `rose-original-dense-native-spatial-rig.fbx` plus `.fbm` image copies. Export loads owned raw filenames into a temporary in-memory material graph, avoiding unpacking arbitrary paths embedded in a GLB; it does **not** save those export-specific references into the blend.

A nonempty directory without an owned successful manifest is refused even with `--overwrite`. If an interrupted run left only partial artifacts, choose a fresh output directory. Missing/ambiguous native actions or unsupported multi-mesh inputs fail explicitly. No mesh joining, automatic fallback rig, bone movement, or silent quality reduction occurs.

## Validation and integration limits

`reproducible-run/manifest.json` records the fresh-source functional run. `package-validation.json` additionally records numerical weight reproduction against the previously visually accepted candidate, final export image-byte checks, and input-directory/unowned-directory/linked-file protection tests. `SPATIAL_REPORT.md` contains the sampled visual acceptance and prior FBX/native-action roundtrip measurements; `REPORT.md` retains rejected variants as diagnostic evidence.

This script produces reproducible assets and measurements; its numeric gates do not automatically declare visual acceptance. It intentionally does not add new visual improvements or render fresh acceptance images during packaging. For integration, retain all eight influences in Unity import/runtime skinning, configure original UVs and normal/basecolor/packed-RM maps, and attach the original provider native animations through the matching hierarchy. FBX preserves basecolor/normal references but cannot express the GLTF roughness/metallic channel-separation material faithfully. The packed RM map needs explicit engine material setup. Other animations, full timelines, engine appearance and mobile performance remain outside this package's acceptance.

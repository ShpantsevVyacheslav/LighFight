# Kitchen static-environment LOD trial — in progress, derivatives unaccepted

This is **environment optimization**, not character restoration. No rig/character geometry, source master, or shipping asset is changed. Full hybridv3 and all downloaded masters are retained.

`kitchen-master.glb` is the portable full-density master:5931348triangles total (5930952Tripo furniture +396clean manual architecture). It was assembled by copying original GLB binary buffers and remapping references, not re-exporting/re-encoding source geometry or images. `portable-GLB-master-proof.json` verifies exact POSITION/NORMAL/UV/index buffer bytes, correct PBR texture bindings, and all9original4Kimage files byte-for-byte. The small manually authored architecture uses the already baked floor PNG. Wrapper transforms preserve the hybrid layout. File~185MiB is not a mobile budget claim.

First unprotected collapse attempt stopped RED on its UV validity gate before any derivative acceptance (`build-r1-UV-RED.log`). Raw sources all have finiteUVs within[0,1]. Candidate extrema were not retained in that initial run; `tools/diagnose_uv.py` is the bounded one-source reproduction to measure/save them explicitly. Do not label the RED harmless without data or clamp its UVs.

Conservative next trial protects existing topological/UV-normal split boundaries and atlas0/1border vertices on **new copies only**, with zero collapse weights; interior vertices remain collapsible. Achieved counts are recorded independently from300k/100kwhole-furniture targets. Quality takes priority if protected boundaries prevent the target. No atlas repacking, source texture resampling, UV clamping, or vertex welding. Upstream Blender4.2collapse source permits extrapolated custom-data factors and excludes zero-weight endpoint edges: [Blender source](https://raw.githubusercontent.com/blender/blender/v4.2.0/source/blender/bmesh/tools/bmesh_decimate_collapse.cc). This motivates the trial; actual installed-binary diagnostics/raster gates remain required.

Source boundary audit is in source-boundary-audit.json. Boundaries include UV/normal splits, not automatically physical holes. LOD geometry is exported independently, then original PBR/material/image payloads attached without a codec. Original maps stay4K and byte-exact.

Planned actual portableGLB review: identical camera/light/Cycles settings for master/each derivative, whole room, table/chair legs, and coffee detail. IndexOB masks quantify silhouettes and covered reference holes; beautyPNG metrics are RGB8display values. Fixed conservative numeric gates are in tools/compare_renders.py. Any visual leg/handle/hole/seam damage rejects a candidate even if summary metrics pass. No derivative accepted or promoted before those renders are opened.

## Named jobs (serialize; do not overlap other heavy player/render work)

```
# Bounded diagnosis, one source only:
Blender --background --factory-startup --threads 6 --python-exit-code 1 --python art/drafts/kitchen-env-lod-v1/tools/diagnose_uv.py
# Conservative derivative construction, one copy/component at a time:
Blender --background --factory-startup --threads 6 --python-exit-code 1 --python art/drafts/kitchen-env-lod-v1/tools/build_lods.py
python3 art/drafts/kitchen-env-lod-v1/tools/validate_glbs.py
# Separate processes release the full master before importing another state:
Blender --background --factory-startup --threads 6 --python-exit-code 1 --python art/drafts/kitchen-env-lod-v1/tools/render_review.py -- master
Blender --background --factory-startup --threads 6 --python-exit-code 1 --python art/drafts/kitchen-env-lod-v1/tools/render_review.py -- lod-300000
Blender --background --factory-startup --threads 6 --python-exit-code 1 --python art/drafts/kitchen-env-lod-v1/tools/render_review.py -- lod-100000
python3 art/drafts/kitchen-env-lod-v1/tools/compare_renders.py
```

InstalledBlender: `/Users/macbook1/Applications/Blender.app/Contents/MacOS/Blender`4.2.22. NumPy/PIL are needed for offline validation/comparison. CPU fixed6threads. No Unity/phone/performance/production acceptance; source4Kpixels and original6Mmaster remain available regardless of derivative outcome.

## Protected-copy milestone (2026-10-18)

The bounded six-thread constructor completed successfully. Both derivatives retain original source image bytes; the portable-file validator passes finite positions/UV/normals, valid indices, material texture bindings and all nine original embedded images. Master source POSITION/NORMAL/UV/index buffers remain exact. See `portable-GLB-proof.json`, `geometry-review.json`, and `build-r2-boundary-protected.log`.

**Target labels are not achieved counts:** 300,000-target furniture retains **399,448 triangles** (399,844 including architecture); 100,000-target retains **381,372** (381,768 including architecture). Protection limits collapse; neither is a 100k asset. Both remain **UNACCEPTED** until actual portable-GLB raster, legs/openings and visual gates run. No environment promotion or mobile claim.

The prior unprotected table RED is reproducible: 100,600 triangles, 11 out-of-range UV values, minimum −0.0000101728765, maximum 1.0000001192; source SHA unchanged. No UV values were clamped. See `baseline-UV-RED-diagnostic.json`.

Four offline silhouette regression fixtures pass (`tools/test_raster_metrics.py`): unchanged perforations, missing leg, tiny filled opening despite high overall IoU, and invalid empty/mismatched masks. The shared comparator uses NumPy and Python flood fill, avoiding an unavailable SciPy dependency. This proves the gates' basic rejection behavior, not candidate visual quality.

Heavy window was released to the higher-priority shoulder task immediately after constructor exit. LOD renders have **not** run.

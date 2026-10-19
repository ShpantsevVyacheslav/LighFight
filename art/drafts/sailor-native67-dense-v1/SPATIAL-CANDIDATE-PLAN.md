# Prepared native67 spatial A/B — not executed or accepted

Scope: retain all994452original vertices/1988914triangles, every corner UV/material/source image; replace ONLY derived skin weights and Armature target in a new local scene. Untouched original FBX/maps and native67 FBX/67binds/eight actions remain authoritative. No Rigify motion, C1, corrective, geometry fit, remesh or decimation. Same native dance.45/.50/.55 removes the observed10–11° Rigify anatomical pose bias. Native itself has milder sleeve gathering, so improvement is possible, not promised.

Preflight: full original indexed position/triangle/cornerUV gate; hashes of full mesh/material/image bytes;67cluster-head authority and native rest LBS. ALL994452original rest vertices queried against donor closed-triangle BVH; report nearest distances normalized by original height and worst indices. Fail before weight construction if max distance >.001H (explicit conservative alignment acceptance, not measured result). No geometry alignment correction permitted. Keep per-vertex rest distances as an artifact.

Field: chunk2048,48nearest donor control vertices, fixed sigma=.007original height; Gaussian multiplied by (1−(d/r48)^2)^2, outermost neighbor exactly zero. Closest donor triangle barycentric weights are the baseline. Stable family columns explicitly audited from actual asset names: Head/Neck, Left/Right Hand and their named finger descendants, Left/Right Foot/Toes. Actual baseline stable mass≥.95 uses barycentric only; ≤.75 uses field only; continuous linear blend between. These are influence-derived stabilization masks, not claimed anatomical segmentation. Save stable mass/blend per original vertex, per-family bounds/counts and mask previews for review. Do not infer stable regions only from source coordinates or silently freeze a broad torso.

Unlike old Rose script, NO1e−6cleanup, NOtop4/8 projection, NO dropped positive weights. All positive donor contributions remain; float64 reconstruction→float32 transport must retain every positive coefficient, otherwise fail closed. Report actual positive influence histogram/max, min donor/baseline/reconstructed/stored values, all sum deviations, float transport errors. Normalization is explicit. Inferred weights are not authored original weights.

Candidate: preserve body world transform, switch the existing game-LBS modifier to untouched native67, set variable positive weights; keep original rig/body archive untouched on disk. Full readback compares every positive named weight and records float32 sum deviations. Verify unchanged geometry/UV/material/image/bind/action signatures before save. Save separate .blend + variable-length CSR weights (no cap), rest distances and stable-mask arrays. Large local artifacts remain outside ordinary Git staging.

Same-dance validation: native67 actual world-bone matrices before/after candidate assignment must be identical; dense actual evaluated surface vs independently computed final-weight/native-bind LBS over all994452vertices. Same shared camera native/dense pairs front/back/world−X, phases.45/.50/.55. Original donor remains unchanged. Compare visible torso pull and underarm fold, not just triangle count. Candidate remains A/B research unless actual visuals and full gates support acceptance. No other dances or shipping/phone claims.

Execution is blocked until root explicitly hands off the heavy window. Prepared script must be reference-reviewed and compiled first. Proposed one CPU6 job, proposed wall budget up to900s (NOT authorized) because ~1M BVH +48NN + unbounded positive weight assignment is not yet profiled; staged audit checkpoint after ALL-rest gate permits stopping before assignment. No claimed600s fit until actual progress evidence. No simultaneous Unity/Blender.

Prepared command (not run):

```
LF_SPATIAL_REST_ONLY=1 /Users/macbook1/Applications/Blender.app/Contents/MacOS/Blender --background --factory-startup --threads 6 --python-exit-code 1 --python art/drafts/sailor-native67-dense-v1/tools/build_spatial_candidate.py -- --output art/drafts/sailor-native67-dense-v1/spatial-all-rest-audit-v1 --max-seconds 180
```

This explicit audit-only mode exits after full BVH distances and preservation preflight; it constructs no weights. Full candidate command omits that environment variable and uses a NEW output, e.g.spatial-candidate-v1, with an explicitly authorized budget. Rest-only exit0 is not skin-transfer acceptance. Source loader depends on the retained frozen stage02/primary helper scripts and original NPZ, not a standalone distributable yet. All scripting is prepared/py-compiled only; pure field tests4/4PASS. Actual Blender/API/performance/readback gates remain unexecuted. A float32 positive underflow is a failure, not cleanup. Mask NPZ retains full original rest positions plus stable mass and field-blend arrays so stabilization scope can be visualized independently without another engine job.


## Two-stage budget/memory preflight (offline estimate)

Stage1 is proposed ALL-rest checkpoint **≤180s** (no engine authorization yet). Stage2 requires separate explicit approval, then reuses only a hash-validated checkpoint:

```
LF_SPATIAL_REST_CHECKPOINT=art/drafts/sailor-native67-dense-v1/spatial-all-rest-audit-v1 /Users/macbook1/Applications/Blender.app/Contents/MacOS/Blender --background --factory-startup --threads 6 --python-exit-code 1 --python art/drafts/sailor-native67-dense-v1/tools/build_spatial_candidate.py -- --output art/drafts/sailor-native67-dense-v1/spatial-candidate-v1 --max-seconds 900
```

**900s is a proposal, not authorization.** Checkpoint stores all nearest points/triangles/distances plus SHA256; reuse refuses source/material/bind/action or geometry mismatch. No second all-rest scan needed. If180s exhausted, retain RED/progress and release, not secretly extend.

Maximum mathematical influences67, not4/8. Actual inferred maximum is unknown until reconstruction. Worst-case allocated storage: float32field254.17MiB; max67 CSR data+indices+indptr325.30MiB; native67float64matrix25.55MiB;48NN×2048×67float64temporary50.25MiB; rest closest points/distances/triangles≈34MiB. Existing source .blend92.02MiB, reference NPZ132.79MiB. Together with Blender's original/donor meshes, source4Ktextures, fullsidecar arrays, cache and render acceleration, conservatively reserve **up to6GiB process/RAM headroom**, not measured RSS. No67-wide neighbor tensor for all1Mvertices is allocated. File memmap is254MiB; worst-case CSR~325MiB+newscene/maps/renderproof expected <1.5GiB additional; require8GiB disk free at entry to retain safety margin. Current offline disk snapshot13GiB free. No claim this predicts actual maxinfluences or timing. Record actual RSS/peak and file sizes at execution.

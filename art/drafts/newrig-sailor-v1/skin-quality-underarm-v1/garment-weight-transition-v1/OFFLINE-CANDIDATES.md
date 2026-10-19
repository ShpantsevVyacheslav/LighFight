# B bounded family-mixture candidates — not quality accepted

Root reviewed actual posed bone heads and garment corridor, approving only the
bounded experiment: control0/red torso-side fixed, control3/purple sleeve-side
fixed, yellow/green free, outside boundary fixed. No anatomical forearm-weight
error inferred: actual forearm.R head is near the elbow under the sleeve.

The corridor follows original mesh edges0→1→2→3 with8mm intrinsic margin, plus
original reviewed25mm control neighborhoods.6935vertices,400outer-boundary
vertices;50red/120purple core vertices. All221624protected vertices excluded.
No spatial weld across a self-close arm/torso gap. Existing NPZrest and NDAhead
positions agree within34nm; armature/mesh worlds exact. Their rest orientations
have1.11e−5component discrepancy, so this is a head-position witness, not a new
full-bind acceptance claim. The consistent original-indexed gauge is used for
frozen source skinning throughout.

The scalar arm-family fraction is a positive original-edge-conductance harmonic
field with original fractions on fixed bands/boundary. Only torso↔arm family
mixture changes; each family's original conditional bone proportions remain.
Explicit families are documented in offline-candidates-v1/report.json. Vertices
without both families are fixed, not assigned invented new influences.1970fixed
vertices total, including1564unmixable.232CGiterations, residual6.97e−9, fixed
values exact. λ.25/.5 interpolate original mixture toward this field.

|Candidate|Changed vertices|Max weight-component change|Predicted dance50 max displacement|
|---|---:|---:|---:|
|λ.25|4965|.0102771|1.244mm|
|λ.5|4965|.0205541|2.488mm|

Four original positive slots remain positive. No top4 projection, pruning or
new influences: projection residual0 by construction. Float32 storage error
≤2.98e−8/component; max stored sum deviation2.384e−7, explicitly retained in
report. Original outside/fixed tuples and protected weights unchanged. Neutral
mathematical-rest sum-only displacement bound<.034µm; actual Blender neutral
surface gate still pending. Original rest buffers/triangles/UV/maps/binds exact.

**Counterexamples remain open.** Although local rest>1mm p99absolute edge excess
improves5.220→4.687/4.001mm, candidateλ.25 has1triangle andλ.5 has7triangles with
normal dot baseline<0. They are orientation witnesses, not automatically proven
self-intersection. λ.25 witness originalface478899 (655226,657834,661217), area
.020868→.022218mm², maximum vertex movement.784mm. λ.5 includes larger faces up
to.804mm²; all exact IDs/areas are saved. No newly zero-area face detected. Do not
hide these with the unchanged broad-region worst tail or smoother scalar field.
No artistic acceptance until actual matched source/clay views and other poses.

The two heatmaps use the unchanged actual source beauty as background and scalar
weight overlays only. They are NOT deformed candidate renders. Sparse original
and derived tuples are separately versioned in lambda25/50-weights.npz, never
written to the original source. No engine launched for these candidates.

Run offline at1core:

```
OPENBLAS_NUM_THREADS=1 OMP_NUM_THREADS=1 <numpy-python> tools/prepare_bone_corridor.py
OPENBLAS_NUM_THREADS=1 OMP_NUM_THREADS=1 <numpy-python> tools/build_offline_candidates.py
<numpy-python> -m unittest discover -s tools -p test_mixture_kernel.py
```

Four pure numeric tests actualPASS: anchored chain, anchor rejection, fixed/tiny
support retention, missing-family refusal. Tests do not prove visual quality.

# B actual Blender verification — feasible, not a visible fold fix

Blender4.2.22 exited0,159.037seconds, CPU6,18actual paired PNGs. Actual original
and candidate use the same source C1 bone poses, original LBS, source materials
and camera. Only the separate6935-ID weight artifact is applied in memory;
4965vertices have changed tuples. No original scene save or geometry reduction.

All7poses pass full994452vertex independent-LBS/readback checks (max0.328µm),
actual protected221624vertices surface difference exactly0, no opposed normals
relative to baseline and no new collapsed incident triangles. Readback covers
original named influences and the same four positive slots, including tiny
weights. Complete actual original-weight hash and source positions/UV/topology
hashes match again after finally restoration. Original1988914triangles/maps,
DEFbinds and source animation archive remain unchanged. This numerical result
is a feasibility gate, not a skin-quality approval.

|Pose|Maximum candidate surface change|
|---|---:|
|dance45|2.654mm|
|dance50|2.450mm|
|dance55|.895mm|
|neutral|.0000597mm float readback difference|
|BOX|1.960mm|
|kick|.742mm|
|65°stress|4.451mm|

The actual−X source/clay pairs show modest local redistribution; deep underarm
fold/gathering remains. Agent does not recommend claiming the fold fixed or
promoting from these numbers. Root explicitly withholds promotion. See
**dance45-50-55-paired-clay.jpg** and **dance50-paired-source-side.jpg**, and the
individual source/clay front/back views. Heatmaps elsewhere are not beauty.

Broad torso-region p99logstrain modestly improves at nonneutral poses; broad
sleeve p99/worst stays unchanged. Tiny neutral torso p99logstrain increases
5.5882875e−6→5.5916596e−6 (3.37e−9): retain this as a strict archived1e−9-fence
counterexample rather than silently relaxing it. It is distinct from the
orientation/readback gates passed here and from visible artistic quality.

V2 actual RED is preserved separately: dance45=5,dance55=4,BOX=6,kick=1,
65°=10opposed-normal witnesses,0newcollapses. No beauty was rendered for V2.
Those witnesses motivated the versioned7pose local constraint. Neither RED nor
GREEN orientation counts prove a self-intersection or its absence. Full dance
continuity, all-eight-motion skin review and portable game export remain open.

Reproduction from repository root, serialized heavy window only:

```
Blender --background --factory-startup --threads 6 --python-exit-code 1 \
 --python art/drafts/newrig-sailor-v1/skin-quality-underarm-v1/garment-weight-transition-v1/tools/validate_render_candidate.py -- \
 --candidate art/drafts/newrig-sailor-v1/skin-quality-underarm-v1/garment-weight-transition-v1/orientation-constrained-seven-v3/lambda50-seven-pose-weights.npz \
 --output art/drafts/newrig-sailor-v1/skin-quality-underarm-v1/garment-weight-transition-v1/NEW_OWNED_OUTPUT
```

The output must not already exist. Seven numerical cases precede any beauty
render. An orientation/anchor failure stores exact witnesses and exits1, with
source restoration verified in finally. No Unity, catalog or phone claim.

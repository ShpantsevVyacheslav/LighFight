# B local orientation constraint — offline feasibility only

The originalλ.25/λ.5 candidates and1/7orientation witnesses stay unchanged in
../offline-candidates-v1/. This separate derivative keepsλ.5 on most of the
free cloth and attenuates the mixture locally around offending original faces.
No original source weights, geometry, rest positions, UVs, maps or binds changed.

All14456original triangles incident to the complete6935vertex corridor are
rechecked on every iteration, including triangles crossing the fixed boundary.
The actual stored float32weights are used for the frozen original-LBS positions.
Criteria unchanged: original-posed-normal dot new normal≥0; no new triangle area
below1e−18m². Neither criterion proves absence of self-intersection.

The7initial opposed witnesses become1,1,0after3local backtracking steps. Each
step halves the active field at offending vertices with compact6mm intrinsic
WendlandC2support, reaching unchanged strength continuously at the support edge.
Fixed/protected IDs never participate. Final freeλ minimum.0625;4478of4965free
vertices remain exactly.5. All positive original4slots retained, no truncation.
Maximum predicted dance50 displacement remains2.488mm. Entire history includes
original face IDs, baseline/new normals, dot/cosine and areas, rather than merely
reporting a count of removed witnesses. This is NOT a skin-quality acceptance.

Numeric RED recorded missing constraint helper, then7/7 pure tests GREEN after
implementation. Source geometry was not used as a solver variable. New weights
and λfield are explicit in lambda50-active-weights.npz. Actual Blender LBS
readback, images, other-pose orientation/strain and visual quality remain pending.

After explicit heavy-window handoff, named bounded actual diagnostic:

```
Blender --background --factory-startup --threads 6 --python-exit-code 1 \
 --python ../tools/validate_render_candidate.py
```

It checks7actual source poses and writes18paired review PNGs, using source/clay
primary−X/front/back atdance50. It restores original groups in finally and never
saves the source scene. Actual opposed-normal counterexamples will be retained,
not silently accepted because the offline field was feasible.

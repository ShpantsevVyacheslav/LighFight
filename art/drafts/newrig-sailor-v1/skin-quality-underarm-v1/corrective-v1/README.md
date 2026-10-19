# One-pose under-sleeve corrective v1

Owned derived experiment only. Original source/fullDance packet stays read-only.
This is a proposed local **rest-informed contour constraint**, not a claim that
weights or the rig caused the remaining fold. No whole-mesh Laplacian or mesh
reduction. A few authored profile controls replace another automatic blur loop.

Run one CPU6 process:

```
tools/run.sh --mode prepare-mask
# Inspect actual front/back/side mask-proposal PNGs and write garment-mask-review.json.
tools/run.sh --mode build
```

The second command fails closed without a receipt containing `reviewed:true`,
`garmentOnly:true`, `maskSHA256` for garment-mask-proposal.npz, and at least3
`images:[{path,sha256}]` relative to this folder. A receipt is a visual diagnostic
record, not user approval or skin-quality acceptance. No artificial GREEN mask
is permitted: original broad-region masks/counterexample remain archived.

The single dance50 target uses right upper-arm/forearm true-rest axes, a compact
axial/underside falloff inside the original right sleeve envelope, and a spatial
neck/skin exclusion. Distal/head/upper-neck proposal anchors receive exactly zero
**additional** target delta; pre-existing C1 neck deformation stays untouched.
Only a positive radial deficit is corrected. Five rest-derived median profile
radii define a smoothstep contour; candidate posed displacement is capped3mm,
then reduced by the compact0–.65falloff. It never shrinks/flattens the outer cap.
This is an explicit exploratory shape hypothesis; if the inner armpit fold does
not improve, reject it rather than increasing displacement blindly.

Each selected vertex uses its exact original blended LBS linear map. Singular,
nonfinite or condition>50 matrices abort the target; no vertices are silently
skipped. Inversion roundtrip tolerance1e-12m. Sparse original-index local deltas
are written to candidate-v1/target-deltas.npz separately. Original weights,
binds, basis positions, UVs and textures remain unchanged; temporary shape key
exists on an owned mesh copy only. No source .blend or FBX is saved.

Build records before/after regional edge/triangle tails and numeric fences,
anchor-zero proof, all-point projected contours and six same-camera renders.
All-original-vertex rendered surface is checked against predicted LBS. Numeric
fences or source preservation alone cannot approve the visual result. Other
motions, continuous pose activation and Unity morph transport remain pending
until this one pose is visibly acceptable. At most one target is built here.

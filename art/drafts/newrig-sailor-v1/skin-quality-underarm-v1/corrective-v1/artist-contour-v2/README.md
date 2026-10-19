# A-v2: explicit one-pose garment contour

Prepared derived experiment, no quality acceptance. Original dense sources,
weights, UVs, triangles and image bytes stay unchanged. `design-side-guide.png`
is labeled **design only**, not a modified/corrected-render claim. The original
−X image is retained byte-identically and SHA-locked by design.json.

One named entrypoint, single authorized Blender CPU6 process:

```
Blender --background --factory-startup --threads 6 --python-exit-code 1 \
 --python tools/artist_contour_probe.py -- --mode prepare
# Inspect actual new garment-mask front/right-back/-X images.
Blender ... --python tools/artist_contour_probe.py -- --mode build
```

Build refuses without `garment-review.json`: `reviewed:true`, `garmentOnly:true`,
`maskSHA256`, and at least3 `{path,sha256}` images from prepare. This is a
visual-membership record, not artistic approval. Ray-picked controls must hit
actual allowed garment; no snapping to another surface. Camera view_frame gives
exact pixel rays, not a guessed projection or side switch.

A-v2 defines four visible groove locations and +1/+3/+3/+1mm surface targets
along the correct −X view direction. A compact interpolating C2 field realizes
those explicit3Dpoints. It does not find a minimum ratio or inflate a radial
sleeve profile like rejected v1. Distinct bounded support can compensate local
swept volume while holding the four targets. Unsupported topology, compensation
outside±1mm, non-bracketed volume, artist handle residual>.1mm, total delta>3.5mm,
singular/condition>50 inverseLBS or protected-region movement abort the build.
A swept-patch volume measurement is not a whole-body anatomical volume claim.

Source basis is never changed. Sparse original-index deltas become one owned
shape key only for diagnostic rendering. Actual added head/skin/neck/distal
movement must be exactly zero; original C1 neck deformation remains as-is.
Dance-only C2 activation uses humerus elevation/elbow flexion with15°/20°support
and smooth authored entry/return fade. Build samples actual neighboring dance
poses and BOX/kick/neutral with exact-zero non-dance target weights. This is
local pilot evidence; runtime morph transport and full motion range remain open.

Build saves explicit desired/actual controls, inverseLBS target, strict strain
fences, absolute length/excess distributions, source/clay before/after images
for **−X**, front and right-back. Full original regional strict gates are retained;
thresholded absolute distributions are diagnostic bins, never a GREEN waiver.
After actual build, run without Blender:

```
python tools/artist_contour_probe.py --raster build
```

This extracts actual alpha row contours and labels crop/border limitations.
The internal fold needs source/clay visual review even if outer silhouette is
unchanged. No automatic score can approve stripe flow or natural garment shape.

B alternative stays separate: use an artistically accepted A surface as a
multi-pose objective for minimal garment-only max4weight redistribution, keeping
original weights separately. Do not smooth weights just because a microedge's
LBS gradient term is large. No B mutation is implemented here.

8/8 actual pure math tests pass after retained RED. Source scene/ray/garment
controls/volume brackets remain unexecuted until the coordinated heavy window.

## R3 actual mask / R4 offline support expansion

Actual R3 `prepare/` exited0 and root visually accepted garment scope only.
It is preserved unchanged. No A-v2 target has been built: the upper-sleeve
compensation ray hit a protected region, and the old eroded mask had two
interior margins below6mm. Those are retained diagnostic failures.

R4 defaults now write **prepare-r4/**, **build-r4/**, artist-mask-r4.npz and
require **garment-review-r4.json**, preserving R3 hashes/images. The compensation
control was intentionally moved to clearly visible torso cloth(170,590), with
the second torso support(245,590). `design-side-guide-r4.png` is an explicit
proposal, never a corrected/rendered result.

The constructive one-ring expansion changes the feather's zero boundary from
the last *allowed cloth* vertex to the adjacent *excluded* vertex. It adds no
protected ID and changes no6mmthreshold. Exact frozen-source analysis gives
6.606/7.200mm for handles3/4 (old5.320/5.819). New actual mask rendering/review is
still required before build. The source has221624protected vertices throughout.
See evidence/support-expansion-offline.json.9/9 actual pure tests pass; boundary
RED and exact executed R3script/hash are retained.

New preparation is pending the coordinated heavy window. An old idle standalone
playerPID71970 was observed; root explicitly authorized its coexistence without
termination. These are CPU6 beauty/geometry diagnostics, not quiet performance
benchmarks or phone thermal evidence.

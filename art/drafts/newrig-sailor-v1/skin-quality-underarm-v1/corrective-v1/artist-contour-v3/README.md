# V3 artist garment contour, no forced volume compensation

Separate new physical/artistic experiment. R4's zero-volume compensationRED
is preserved in ../artist-contour-v2/R4-RESULT.md. A free shirt may change its
air gap: V3 reports signed changed-surface volume and area instead of forcing
an unrelated torso dent. **No volume-preserved claim.**

Original994452vertices/1988914triangles/UV/map pixels/skin weights/binds stay
unchanged. One new derived sparse morph only, in an owned mesh copy for review.
No whole-mesh smoothing or source save. Existing C1bone/motion data stays exact;
additional head/skin/neck/elbow/distal morph deltas must be zero.

Four explicit -X surface controls fill the visible inner sleeve groove by
+1/+3/+3/+1mm toward camera. A compact40mmC2 interpolant gives continuous support
between handles instead of four isolated22mm bumps. Total displacement cap
3.5mm, inverseLBS condition≤50, actual handle residual≤.1mm. Those are experimental
bounds, never a claim that this desired shape is artistically accepted.

Use one named entrypoint, headless CPU6:

```
Blender --background --factory-startup --threads 6 --python-exit-code 1 \
 --python tools/artist_contour_probe.py -- --mode prepare
# Actual new garment scope must be visually reviewed; no automatic approval.
Blender ... --python tools/artist_contour_probe.py -- --mode build
```

Outputs prepare-v3/ and build-v3/. Build refuses without garment-review-v3.json
containing reviewed/garmentOnly true, artist-mask-v3.npz SHA and ≥3actual mask
PNG hashes. Exact protected221624IDs and fixed6mm margin retained. Source
camera pixels are ray-picked on unchanged original LBS; no nearest-region snap.

Build reports signed surface tetra difference (not anatomical volume), regional
area/collapse/opposed-baseline-normal witnesses, strict ratio fences and absolute
edge excess/Δposed-edge bins. Original ratioRED is never hidden by the absolute
bins. Source/clay before/after primary−X, front and right-back are saved. A
geometric C2 dance-only driver is sampled at actual neighboring poses, with
BOX/kick/neutral exact-zero added morph. No runtime/other-motion acceptance is
implied by one pose.

After actual build, ordinary Python `tools/artist_contour_probe.py --raster
build-v3` extracts local alpha contours; cropped-window limits stay explicit.
Outer contour equality alone cannot approve a deep internal fold/stripe flow.
The before/after artistic target must be visibly better on source and clay.

8/8 pure tests actualPASS. Original R4pureRED evidence remains in its folder.
This is a geometry/beauty review, not a Mac/phone performance or thermal claim.
Root explicitly allowed an old idle28MB standalone to coexist without killing it.

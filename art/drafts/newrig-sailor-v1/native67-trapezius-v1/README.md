# Sailor dance-only trapezius support — original 67-bone review candidate

This candidate addresses the user's low neck-to-shoulder transition during Dance. It deliberately changes the dance clavicle rhythm, rather than flattening the outer sleeve or rerigging all motions. It is a review candidate, not a promoted shipping asset, and not acceptance of the separate 2M fresh-rig pilot.

## Method

`../tools/20_native67_clavicle_corrective.py` reads the preserved immutable `source/sailor-native67-original.glb` (SHA3984974e…; the prototype may later select a versioned candidate), retains every original scene object and animation, then appends `Dance_TrapeziusSupport_v1`. Only four rotation tracks differ in that derived animation: left/right Shoulder and UpperArm. A bounded C1-continuous support curve lifts the clavicle relative to source humeral elevation, capped at15degrees; .5second entry/exit fades return to the original endpoints. Counter-rotation at the UpperArm preserves source arm world orientation at curve keys. Hands naturally move with the shoulder girdle; this is measured rather than hidden.

No added bones, mesh targets, geometry reduction, compression, edited textures, modified bind/rest shape, altered skin weights, or Blender-only B-bones. Original `dance_01` remains available unmodified. Every other original animation, including PreviewGuard, retains identical JSON and underlying binary payload. Mesh indices/positions/UV/normals/tangents/JOINTS/WEIGHTS, inverse binds, image bytes, materials and node defaults remain exact through append-only construction.

This is the native100k mesh (99999triangles), not the original1988914-triangle unrigged mesh. The dense fresh-rig experiment remains separate and has not passed its visual gate.

## Reproduce

Use Python3 with NumPy; no Blender required for construction/numerical proof:

```
python3 art/drafts/newrig-sailor-v1/tools/20_native67_clavicle_corrective.py
python3 art/drafts/newrig-sailor-v1/tools/22_native67_numerical_gate.py
```

The13MiB original baseline copy is retained so future prototype promotion cannot destroy reproducibility. `DANCE_SOURCE_GLB` can point to an independently preserved copy with the same enforced source SHA.

Default correction sampling192Hz, configurable by `DANCE_CORRECTION_HZ` (integer multiple of24). Only four derived tracks are resampled. Independent numerical checks evaluate at twice that rate, including inter-key poses. The earlier24Hz and96Hz strict orientation failures are retained in `versions/` rather than waived. Final review reports the measured interpolation error; it does not infer acceptance from triangle count or nonzero weights.

Render actual exported GLB through Blender4.2:

```
/Users/macbook1/Applications/Blender.app/Contents/MacOS/Blender --background --factory-startup --python-exit-code 1 --python art/drafts/newrig-sailor-v1/tools/21_native67_visual_gate.py
python3 art/drafts/newrig-sailor-v1/tools/23_native67_review_sheets.py
```

Renders use identical source PBR pixels, AgX lighting/camera, phase0/.25/.5/.75/1, front and back-three-quarter. Original PNGs remain unchanged; contact sheets only arrange/rescale them for review. Source frame/camera data are in `visual-gate.json`.

## Evidence and limits

`manifest.json` records original/candidate SHA, preserved objects, curve sampling and payload. `numerical-gate.json` records independent arm-orientation error, hand displacement, exact source payload checks, full-surface endpoint error, original full-foot vertex motion and floors. `frame-metrics.json` records continuous support angles.

The existing native mesh contains tiny shoulder influence in a head-dominated surface region: head bones are unchanged, but that surface can move about.190mm. Full-foot vertices and original full-mesh minimum are unchanged at the five inspected phases. Original native floor offsets are retained, not corrected or falsely labeled zero.

Blender actual-GLB visual proof and independent numerical LBS proof do not establish browser/Unity/iPhone playback, energy, or thermal acceptance. Root visual review and prototype integration remain explicit gates. No other character is rerigged or modified by this script.

Final192Hz output has identical complete world matrices at every rendered phase to the actual96Hz R2 Blender renders (all matrix deltas0), verified by script24 and `render-pose-identity.json`. These remain explicitly R2 renders; no unperformed R3 GPU render is claimed. Endpoint raster differences are recorded separately in `render-entry-return-pixels.json`: tiny edge/denoising differences remain, so pixel equality is not claimed.

## Final measured result —192Hz R3

Independent384Hz check:8897poses PASS, maximum upper-arm quaternion orientation difference.007464degrees and actual world-matrix basis-axis difference.007322degrees. Maximum hand translation21.1067mm (2.15365% of the native.98004m rest height). This movement is deliberate shoulder-girdle motion, not an unreported retargeting error. Source arm/finger animation channels remain unchanged except compensating UpperArm rotations in the derived dance.

At all five full-surface phases,11286original foot-influenced vertices have exactly zero movement; the full-mesh minimum is identical. Source native floor offsets remain between−3.620mm and+7.209mm at those phases; no floor correction was added.7148head-dominated vertices move at most.190mm due to existing small shoulder weights; head bones and their tracks remain unchanged. Entry/return surface difference is at most.000104mm. Bind-rest skin reconstruction error1.258µm is the unchanged source export tolerance.

Final candidate SHA256: `21d0e02f8e8fe36116c4c23188454b4f22ef10b759a78fb4f9d0bf0bb38c7da9`. Additional payload325436bytes. Original9animations retained plus one explicitly derived dance. Choose `Dance_TrapeziusSupport_v1` for prototype Dance, retain `dance_01` as the original comparison. No source FBX, original model, other fighter, or Unity shipping asset has been modified.

Visual review: neck-to-shoulder fullness improves at.25/.5/.75, with source head/neck silhouette stable. Some outer-sleeve ledge and garment stripe waviness remain; the result is a localized prototype improvement, not a claim of anatomically perfect cloth or a globally accepted fresh rig. `phase50-before-after.png` and `phase75-before-after.png` include front/back comparisons; complete five-phase sheets are `paired-front.jpg` and `paired-backthreequarter.jpg`.

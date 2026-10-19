# Hybridv3 — three authentic Tripo components, clean manual architecture

**Provenance: Tripo furniture + manually reconstructed photo-based architecture.** This is an exploratory full-density master, not a fully generated Tripo room or a mobile/Unity-approved arena.

`interior-threequarter.png`, `top.png`, and `hybrid-review-sheet.jpg` show the actual assembled scene. All appliance blocks are now replaced. The photo-derived relationships remain: table/four chairs beside the one right-wall window; fridge with adjacent microwave/plinth against the back wall; coffee cabinet/machine/cups/rack on the left. Architecture remains a clean rectangle/two straight walls, with no hallucinated interior blind sheet or fictionalU-shaped cabinets.

Three source components are preserved intact:

| Source | Triangles | Imported/source vertices |
|---|---:|---:|
| original-v2 table/four chairs |1988865|1076312|
| fridge-microwave-original-v4 |1974944|1026269|
| coffee-original-v5 |1967143|1044438|
| Total source master |5930952|3147019|

Provider metadata for the fridge is one vertex higher than its actual GLB accessor. No vertex was removed. Source positions/triangle indices/UVs/materials/maps are retained; only per-component uniform parent transforms place normalized assets. Coffee parent scale1.68125, translation[−2.35,1.5,.002]; no selective scaling, remeshing, decimation, or source texture resampling. Existing table/fridge placement is unchanged. Raw masters, baselinev1, and intermediatev2 scenes are preserved separately. Source hashes and per-component hashes are in manifests; saved-master-proof.json reopens the final blend and validates all three meshes plus packed original image bytes.

## Portable authored floor

Only the manually authored floor's procedural lookdev was baked to `textures/kitchen-floor-basecolor-2048.png`. It now uses a standard image-driven Principled base color, roughness.55, metallic0; no lighting or normal-map conversion was baked. Source Tripo materials/images are untouched.

`portable-floor.glb` is a small exported standardPBR floor proof:1mesh/12triangles/1embeddedPNG. The embeddedPNG is byte-identical to the saved texture. Actual512×512emission renders compare the original procedural color and the saved/reloadedPNG: meanRGB8difference.325, p951, max2/255, zero pixels>2. These are displayPNG byte metrics, not linear radiometry. Shader/texture filtering and8bit bake mean mathematical identity is not claimed. No browser/Unity player validation has been performed.

The original hybrid floor remains rectangular and continuous:10201/10201full-component grid rays atZ0 fromv1; its positions/shape are unchanged, only UV/material changed for portability. Open play rectangle remains2.5×3.6, wider than the furniture's1.563width. The5.4×5.6×3room is an explicit game-space expansion; the photo provides no measured dimensions. No metric photogrammetry claim.

## Reproduce

Requires the preserved/reproduciblev2blend (buildv1 thenv2 if missing):

```
/Users/macbook1/Applications/Blender.app/Contents/MacOS/Blender --background --factory-startup --threads 6 --python-exit-code 1 --python art/drafts/kitchen-hybrid-v3/tools/replace_coffee_and_bake.py
/Users/macbook1/Applications/Blender.app/Contents/MacOS/Blender --background --factory-startup --threads 6 --python-exit-code 1 --python art/drafts/kitchen-hybrid-v3/tools/verify_saved_master.py
```

The first helper-module lookup failure is preserved in attempt1-module-path-RED.log; the corrected script adds its owned tools directory explicitly. Final jobs are sequential, fixed6CPUthreads, with factory startup and error exit.

`kitchen-hybrid-v3.blend` is a536MiB local reproducible master: **do not add it to ordinaryGit**. Stage scripts, PNGs/texture, small floorGLB, JSON and provenance as appropriate. Full5.93M source triangles plus small architecture are not accepted for mobile performance. No environmentLOD/triangle reduction, character modification, Unity import/promotion, or external-generation action was performed by this pipeline. Any future runtime budget work is a separate reviewed step.

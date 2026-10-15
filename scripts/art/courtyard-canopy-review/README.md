# Courtyard canopy review, not shipping arena

Source scene reference: `assets/references/photo-05.jpg`. Root inspected the source photo and old actual component-player captures. Main differences remain surrounding canopy/distant enclosure and material/lighting fidelity. This pass prepares one combined crossed-card canopy mesh, adds five distant trunks, and retains all three source car meshes/transforms/materials. No catalog or fighter changes.

`build_geometry.py` deterministically generates canonical Unity Y-up world-space JSON and an optional OBJ. `verify_geometry.py` independently tests all1008 triangles against the11×4m fight volume with a triangle/box SAT, then requires intentional intrusion and boundary/far fixtures to work. It passed. This is static clearance, not dynamic attack/camera/device acceptance.

The v1 generated atlas crossed the quadrant split. The v2 imagegen edit leaves wide gutters. Source PNGs and provenance are retained without editing their pixels. A faint1-pixel alpha residue is measured separately; the intended alpha-test cutoff must be checked in renderer/mip/shadow tests. Material draft is two-sided alpha-test, source-color texture, preserved mip alpha coverage, optional thin-leaf fill. Shader has NOT been compiled/run in Unity yet. C# builder compiled against actual installed Unity6000.3.26f1 API references with exit0, no warnings; earlier standalone harness missing-reference errors were corrected before that verdict.

When the phone build releases the shared heavy window:

1. Use an owned isolated Unity review project with the existing component scene/dependencies, not the shipping catalog. Copy `UnityEditor/CourtyardCanopyReview.cs` under its Editor folder. Copy canonical JSON, v2PNG, shader and `review-inputs-v1.json` (renamed inputs.json) into `Assets/LighFight/Art/Drafts/CanopyReview/`.
2. Run named job `CourtyardCanopyReview.Build` with graphics and correct platform. It checks frozen input SHA, exact3source-sedan renderer identities/placements/materials and each individual card's conservative clearance; outputs only additive `CanopyReview/arena-canopy-review.prefab`.
3. Source baseline is current `ComponentCourtyardReview/arena-component-review.prefab`, not a regenerated old builder that would discard surface/ground work. Preserve original car source and all geometry/maps.
4. Capture identical photo-overview, center, left/right endpoints and actual battle cameras, dynamic all-strike/side-step bounds; compare old/final leaf silhouettes, gutters/mip seams, two-sided normal lighting, alpha/shadow stability and overdraw. Review screenshot with source photo. Pair source/current best11fighter rigs; no geometry or material edits to actors.
5. Measure target phone frame budget and memory. No promotion until actual player/visual/dynamic/phone gates pass. Recheck Shader Resources/stripping if later promoting. Current phone framework contains the previously verified five arenas, not this unaccepted draft.

Working packet under `.tools/courtyard-canopy-review-packet` is reproducible from these sources and original component dependencies. It is not a measured arena-quality improvement until rendered.

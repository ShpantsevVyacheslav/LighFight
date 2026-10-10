# Compact hand revision — 2026-10-10

Candidate-only correction following review: shorten the hand after the watch, enlarge finger volume, pack neighboring digits, and reuse body skin maps.

- Longitudinal fit changed from 1.20 to 0.96; lateral/thickness fit remains 1.25.
- Finger radial volume increased 12% around authored phalanx axes, preserving joint centers and lengths.
- Closed-pose digit root packing: index +0.24 cm, middle 0, ring −0.16 cm, pinky −0.35 cm. Open pose unchanged.
- Thumb pose revised; subdivision-level digit collision audit excludes the palm/web and is not a proof of anatomical correctness.
- Watch center moved toward wrist to source X=66.5 cm; attached rigidly to forearm.
- Hand albedo and normal detail use the original body atlas. Dedicated two-UV shader blends sampled colors/normals rather than interpolating atlas coordinates.
- A 256 px patch was rejected visually because it included watch coloration despite sparse color screening. Retained the clean 160 px patch; shortened transition to 1.5 cm under watch.

No mobile acceptance or production promotion is implied. Check isolated palm/front and whole-body views as well as Unity import/animation checks before adoption.

Additional visual feedback: thumb should cross folded pads rather than point along the index, and dorsal/palmar bulges need correction. Candidate fist capture now uses linear skinning rather than volume-preserving dual quaternion deformation. This is a visual candidate and must be checked for loss of joint volume.

Skin detail maps are copied from the source forearm atlas (160 px clean patch), repeated in 512 px output maps. The normal detail removes low-frequency directional gradients before reuse; this avoids transferring forearm shape shading onto the palm. No independent hand skin tint is applied. Lighting and UV density still require visual review.

## Palm/web defect and corrective

Full proximal surface audit (rest-space distance along hand axis <10 cm) found 65 nonadjacent intersecting triangle pairs per hand. This includes the thumb/index web and proximal finger folds, which the earlier fully digit-weighted audit excluded. There were zero non-wrist boundary edges: the visible triangular depression was a deformation defect, not a missing polygon.

A closed-pose proximal-surface corrective preserves Basis/open pose, wrist boundary and distal digits. Collision counts after 20/40/80/120 smoothing iterations: 48/32/11/0 on each hand. The unchanged defective mesh causes `audit_palm_web.py` to fail with exit 1; the corrected mesh passes. This is scoped surface verification, not device or artistic acceptance.

Reproduction: `makehuman_hand_study.py` → `prepare_makehuman_hands.py` → `smooth_palm_corrective.py` → `integrate_makehuman_hands.py` → `rebuild_rigid_watch.py` → `render_hand_review.py`. Run both palm/web and digit contact audits after the corrective and final fitting.

## Rejected automatic corrective; switch to authored/generated fist

Owner rejected visual result. Ordinary smoothing reached zero scoped intersections but destroyed thumb volume; Taubin volume compensation retained 73 intersections and was also rejected. Neither smoothing result is accepted or promoted. Restored the isolated Unity candidate to the unsmoothed compact hand with source skin detail maps, retaining its documented anatomy defects. No production/combat asset changed. Next approach: generate a reference-driven closed fist with a short forearm cuff, inspect all sides, attach to existing wrist and reuse body skin material. Meshy generation has not been started and no credits spent.

## Photo-generated closed fist integration — 2026-10-11

One authorized Meshy 7.1 multi-image geometry generation completed: task
`01a1279c-70ec-7092-afaa-c431c4491114`, 25 credits, fist front/side references.
No additional paid tasks. Original FBX retained in `art/source/user-fist-meshy-25`.

Removed proximal stub and straightened axial section centers. Reduced closed
fist from 160188 to 16002 triangles, zero nonmanifold edges. Two-direction
vertex-to-surface sampling: maximum original vertex to reduced surface 0.01128 cm;
reverse 0.00813 cm. This is vertex sampling, not a full Hausdorff bound.

Both fists rigidly bind to existing wrist bones, keeping 24 source bones.
Existing ClosedFists blendshape is intentionally constant: this candidate cannot
open its fingers. Jab left wrist aligned at impact. Wrist bridges are closed;
removing a bridge face is detected. Watch rebuilt and rigidly weighted to left
forearm; wrong-bone mutation detected (1.657 cm distortion).

Skin albedo/normal detail copied from original forearm atlas. Smart-projected
detail UV replaces collapsed planar projection and removes longitudinal streaks.
Skin crease placement is generic copied detail, not photo-specific hand texturing.

Isolated scene: `unity/Assets/LighFight/Art/Drafts/IslandMeshyStaticFists/FingerArtReview.unity`.
Rebuild/import checks: `LighFight.Editor.StaticFistAudit.Verify`.
Blender previews: `art/previews/user-fist-meshy-25/integrated/`.
Not promoted into combat or remote asset package; no phone acceptance claimed.
This supersedes the earlier 'generation not started' status above.

Unity graphics-device renders added via `StaticFistAudit.RenderPreview`: unity-front, unity-fist and unity-whole PNGs. Close views isolate baked lower-arm geometry to avoid camera/garment occlusion. Studio fill light is preview-only. Full-body source art and skin shading still need quality work; passing rig tests does not imply professional art acceptance.

## Unity surface-normal repair — 2026-10-11

Reproduced coarse facets with body correctives enabled. Disabling normal-map
strength did not remove them; disabling correctives did. The FBX importer
was calculating blendshape normals separately from imported smooth base normals.
Even unchanged fist vertices received normal deltas (maximum 1.999434).
Importing authored blendshape normals makes the unchanged-fist delta zero.
`StaticFistAudit.VerifySurfaceNormals` deliberately restores Calculate, detects
regression, then restores Import in finally. Regular Verify also guards zero
normal deformation on static fists. Body corrective geometry remains enabled.

CharacterSkin now transforms body UV0 and hand UV1 normal maps using separate
frames before blending world normals. UV1 frame comes from fragment derivatives;
using the body UV0 tangent for the hand UV1 was incorrect. Hand detail strength
0.12, body strength 0.25. Same copied skin textures; no new generation credits.

Review starts with candidate selected; description correctly says closed fists,
not open palms. Full-character `unity-jab.mp4` is a 42-frame/24fps graphics-device
render of the imported clip with correctives. Hand closeups use isolated baked
geometry; these are inspection previews, not device/combat captures.

Samoproverka: no additional Meshy tasks, no device build/install, no push/deploy.
Full-body art quality and opening-finger animation remain outstanding.

Runtime review selection verified by invoking Start and checking active models; reversing startWithCandidate detects regression. Unity EditMode after runtime UI update: 46/46 passed. Shader compiles and Unity graphics renders complete.

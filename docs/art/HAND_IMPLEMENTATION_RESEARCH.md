# Hand geometry and fist implementation research

2026-10-10. Owner requested examples from other projects after rejecting the
procedural hands. Findings guide the next implementation; none establish visual
acceptance by themselves.

## MakeHuman: authored mesh, joints and weights

Primary repository: https://github.com/makehumancommunity/makehuman
Pinned commit: a8bc2d54ff0ac92e78ff71431b1023eda42bf482

Reviewed/downloaded data:
- makehuman/data/3dobjs/base.obj: explicit CC0 header; human body quad topology.
- makehuman/data/rigs/default.mhskel: license field CC0; authored joint landmarks.
- makehuman/data/rigs/default_weights.mhw: license field CC0; vertex skin weights.
- LICENSE.md and LICENSE.ASSETS.md: retained with download provenance/hash manifest.

Source files are cached under .tools/makehuman-hands. Use the assets' CC0 license,
not the AGPL license of MakeHuman application code; no application code is copied.
https://github.com/makehumancommunity/makehuman/blob/master/LICENSE.md

Each hand has a wrist, four metacarpals and three bones per digit (including
thumb opposition at its proximal chain). This provides palm cupping and a thumb
base, absent from the earlier 14-bone procedural replacement. Positions come from
averaged base mesh joint vertices, weights from authored per-bone data.
https://static.makehumancommunity.org/mpfb/developer/rigging_and_posing.html

Implementation: extract body faces belonging to the source hand, preserve quads
and weights, fit the anatomical wrist frame into the Meshy rest pose, then inspect
open/closed dorsal/palm/side views. Do not voxel-remesh away the designed loops.
Add a closed morph only after valid bone posing; later wrist stitching must retain
UVs and all body corrective shapes.

## Blender Rigify: separate control and deform layers

Primary docs:
https://developer.blender.org/docs/features/animation/rigify/
https://docs.blender.org/manual/en/latest/addons/rigify/index.html

Rigify generates components from bone chains in a metarig. Finger flexion axes
and palm bones need correct placement/roll. For LighFight, use the geometry and
joint principles; export ordinary deform bones and baked animation/morphs.
A Blender controller rig is not itself a portable Unity runtime rig.

## Unity XR Hands: finger shape vocabulary

Primary docs:
https://docs.unity3d.com/Packages/com.unity.xr.hands@1.7/manual/gestures/finger-shapes.html

Distinguishes Full Curl, Base Curl, Tip Curl, Pinch and Spread. Useful as a vocabulary
for authored states: relaxed guard, tighter impact fist, open block. The gesture
system recognizes tracked shapes; it does not repair mesh anatomy or generate a
fist. No XR runtime dependency is needed for this mobile fighting game.

## Acceptance criteria

- Authored anatomical silhouette from dorsal, palm and radial/ulnar views.
- Thumb outside folded fingers; no through-skin intersections or collapsing web.
- Joint loops bend without pinching; neighboring finger weights do not pull each other.
- Wrist skin is continuous in jab and guard; old body corrections preserved.
- Export/Unity verification and separate device check; performance/LOD after art acceptance.

Current status: isolated Unity candidate at
`unity/Assets/LighFight/Art/Drafts/IslandMakeHumanHands/FingerArtReview.unity`.
62 bones (38 added), five corrective shapes including ClosedFists. Unity sampled
25 jab poses: finite geometry, fist displacement 0.1289693 m, source wrist morph
displacement 0 m. Zeroed fist morph was rejected by the audit.

Both wrist bridges have zero scoped open boundary edges. Removing a bridge face
makes the seam audit fail. The rejected thumb pose produced 288 scoped triangle
overlaps per hand; revised thumb pose produces zero in open and closed states.
This checks fully digit-weighted base faces, excludes palm/web, and is not proof
of zero intersections on every subdivided surface. Visual review remains required.

Skin material transition, impact-fist refinement, phone review and performance
acceptance remain pending. This candidate is separate from combat/CDN assets.
No Meshy credits were used. Source asset hashes and CC0 license are retained in
`art/source/makehuman-hand-source/`; rendered evidence is in
`art/previews/island-premium/makehuman-hands/`.

Hand flat albedo now derives from 4002 source wrist texture samples; Unity material
uses the corresponding gamma color. This reduces arbitrary coloring but does not
restore skin microdetail or guarantee visual continuity under different lighting.

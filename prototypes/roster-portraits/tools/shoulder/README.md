# Shoulder correction experiments — no accepted replacement

The original three GLBs are unchanged. FBX-versus-GLB identity-mapped deformation checks found the same defect in native source: maximum shoulder deviation below 0.73 micrometres over four dance samples for all three actors. This is not an export interpolation bug.

`artifacts/status.json` records baseline GLB hashes. The versioned Sailor v2 weight artifact is **unaccepted**, retained for reproducibility only; its control-vertex order belongs solely to Sailor. Every role computes its own rest geometry/bone-relative masks. Never apply Sailor indices to another model.

## Reproduce diagnostics

From repository root, use installed Blender 4.2 (factory startup, one process at a time):

```sh
/Users/macbook1/Applications/Blender.app/Contents/MacOS/Blender --background --factory-startup --python-exit-code 1 --python prototypes/roster-portraits/tools/shoulder/try_weights_v2.py -- sailor
```

Replace role with `retro` or `summer` to compute a distinct geometry mask. Output is diagnostic `.tools/portrait-native-glb/shoulder-fix/v2`, never shipping models. `gate_weights.py -- sailor v2` evaluates every integer source frame of all eight native takes; a numeric pass alone is insufficient without viewing close-ups. The helper imports the unchanged current baseline GLB; check its hash before reuse.

## Rejected candidates

- v1 broad 100/400 topology-weight smoothing: neckline sag at strong setting; no compelling shoulder reconstruction.
- v2 collar-anchored smoothing100: modest improvement only, sleeve ball remains.400 rejected.
- v3 chest/clavicle/humerus field reconstruction: medial concave notch; rejected.
- v4 clavicle-to-humerus transfer40/75%: worsened crease near neckline; rejected.
- v5 localized rest-preserving CorrectiveSmooth: essentially unchanged shoulder silhouette; rejected as inadequate.
- v6 localized posed-surface smoothing30/100: removes some ridge but gathers shirt texture and creates chest/arm-junction wrinkles; rejected. Blender modifiers are diagnostic only and were never exported.

Native bone local translations/scales are rest-exact within sub-micrometre/5e-7 numerical noise. Outer sleeve has virtually no Neck/NeckTwist influence (95th percentile zero), so stripping those groups is unjustified. Top caps are roughly71–75% clavicle-weighted. A direct humerus reassignment was tested and worsened the result. At problematic75% dance, left clavicle elevation is near bind, so an arbitrary clavicle rotation clamp is unjustified.

A next valid step is a small set (2–8) of anatomically designed, localized pose-space corrective morph targets, then baked native-angle weight curves. It must preserve base rest positions/UV/topology/textures, source bones/native channels/max4 skin weights, protect neckline/head/distal arms exactly, and pass all-eight-motion localized strain/displacement plus visual checks. Do not bake hundreds of full-mesh targets, ship Blender-only preserve-volume, or label the existing modest smoothing a complete fix. No such corrective target is accepted by this packet.

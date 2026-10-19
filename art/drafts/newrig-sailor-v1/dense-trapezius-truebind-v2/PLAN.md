# Dense fresh-rig trapezius continuation

This reviews the actual original-density Sailor fresh-Rigify pilot, not the separate native67/99,999-triangle prototype. No accepted shoulder fix is claimed yet.

Authoritative source: `stage02-bound-original-dense.blend`, object `Sailor_OriginalDense_UNTOUCHED`; expected 994,452 vertices / 1,988,914 triangles. Keep source positions, indices, UVs, image bytes and transferred max-four weights unchanged. Derive a versioned scene only.

Calibration and channels:

- Control FK chains are `shoulder.L/R`, `upper_arm_fk.L/R`, `forearm_fk.L/R`, `hand_fk.L/R`; evaluated ORG channels `ORG-shoulder.L/R`, `ORG-upper_arm.L/R`; deformation is DEF, not control channels directly.
- Existing gallery uses actual `rig.data.bones[*].matrix_local` as rest frames, but controls' identity pose must first be evaluated through constraints. Record evaluated ORG/DEF identity matrices alongside bone rest matrices. Do not treat the saved dance/guard pose as rest or blindly import native67 local rotations/weights.
- Explicit game-LBS baseline: DEF B-bone segments=1, armature preserve-volume=false. Preserve existing segmented-B-bone gallery as a separate historical result. Test independent weighted matrix LBS against evaluated dense vertices.
- Retarget all existing mapped body/leg/foot/finger channels from actual donor rest frames, maintain root displacement height ratio; compare actual same source phases .25/.50/.75. Use same camera for before/after each pose, front and back three-quarter, with neck/trapezius visible.
- Candidate is a smooth bounded scapuloclavicular support rotation measured relative to true bind upper-chest up and clavicle/upper-arm directions. Determine anatomical elevation axis from evaluated rest geometry, not a guessed shoulder-local Euler axis. Preserve downstream arm world orientations while allowing/reporting natural girdle hand displacement. No copied67 weights or automatic smoothing.

Gate first: same three dance phases + neutral, then arm-elevation extreme and punch/kick controls. Record source exact attributes, head/hand displacement, clavicle rotations, dense surface displacement, no unweighted vertices, original max-four weights. Inspect medial-traps continuity and collar shelf; reject a flatter outer sleeve that worsens the neck transition. An attractive native67 result is not evidence for this dense rig.

Run only after current P1 owner explicitly releases the serialized heavy window. Six CPU threads, one Blender process. No Unity or source edits; no export/promote unless actual dense visual gate passes.

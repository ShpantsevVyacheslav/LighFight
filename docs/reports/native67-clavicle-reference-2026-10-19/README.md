# Clavicle reference experiments — 19 October 2026

Controlled tests use unchanged original dense Sailor geometry, native67 skeleton, full-positive barycentric Float32 weights, raw 4K maps, same renderer/camera/light. No production model or catalog changed.

## Rest-reference audit

66 posed joint samples plus true inverse-bind and canonical reference coordinates from 11 clips. In the first clip calibration, normalized neck-to-upper-arm-origin vertical drop is source rest 0.05133, target rest 0.12055, source canonical 0.05133, target canonical 0.12029. Thus the difference predates the dance. Each skeleton uses its own bind hip-to-ankle height: these values do not prove anatomical error, nor are they world centimetres.

## Experiment A: source-directed canonical clavicle

Only the target canonical clavicle reference is aligned with source clavicle segment direction; original bind matrices and mesh remain unchanged. Same six samples as existing baseline: Boom 30/50/70%, Bass 50%, Bubble 50%, Shake It Off 50%. Generic arm-direction consistency still passes, and numeric full-surface gates pass. Framebuffer-repeat gate remains RED, so this is not full acceptance.

Visual result REJECTED: increased shrug/hunch and chest gathering. This demonstrates why arm-direction agreement and normalized joint placement alone are insufficient. Different proportions and deformation weights require a surface-level assessment. Do not deploy this reference change or mechanically raise all shoulders to source height.

Comparison JPGs: top native-rest reference, bottom experimental source-directed reference. No retouch, only layout and resize of actual captures. Next test compares pose-dependent bounded clavicle support on the same frames.

## Experiment B: bounded pose-dependent support

Same six frames with prior C1 shoulder correction, capped 16 degrees, preserving upper-arm world rotation. Actual execution completed; numeric surface gates passed, framebuffer-repeat remains RED. Wrist displacement and bone-length/neck gates passed. Visual result: insufficient; some contours shift but hunch/chest gathering remain. Not accepted for production and not evidence that skeleton-only adjustment fixes the issue. Adaptive comparison sheets retain the same top/bottom ordering. Next step should inspect weight influence in the neck/clavicle/upper-chest region, rather than increasing correction angles.

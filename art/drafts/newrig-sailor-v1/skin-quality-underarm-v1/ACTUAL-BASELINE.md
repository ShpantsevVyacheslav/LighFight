# Original dense Sailor skin-quality baseline

This is a diagnostic baseline, **not skin-quality acceptance** and not a new
weight/corrective candidate. Original 994,452 vertices / 1,988,914 triangles,
original UVs/maps and 3,088,451 nonzero named skin influences remain unchanged.
All weights are retained (max4; zero unweighted). No source file is saved.

Two actual Blender CPU6 metric runs completed: 9 poses in 198.10s and the two
remaining selected left-side poses in 55.41s. Cases cover dance .25/.5/.75,
all four previously selected high-clavicle/elbow-down phases, neutral, actual
box/kick and explicitly synthetic arm65 stress. See reports and 308-row CSV.
Every region includes all original edges/triangles touching its envelope;
there is no geometry/measurement subsampling.

The envelopes are **provisional overlapping spatial proposals**, not verified
shirt seams or an anatomical diagnosis. Source-albedo mask front/back/side
renders were inspected. Their broad torso/sleeve overlap is visible, so no
`reviewed=true` receipt is issued. Changing an envelope to hide a failure would
invalidate this baseline. Colors are diagnostic overlays on owned copies.

At dance25 the sleeve-L edge max ratio changes 2.949→2.745 and p99 1.494→1.429;
R max changes 3.008→2.739 while p99 worsens 1.427→1.495. Right-sleeve p99 also
worsens at dance50 (1.827→1.871) and dance75 (1.601→1.716). Thus visually improved
medial trapezius continuity is not a complete underarm fix. Extreme stress
retains large local strain. No collapsed triangles are detected in these
regions, which alone does not imply acceptable shape.

Only28/88 attempted upper-arm plane sections form one simple closed contour.
Others report explicit open/multiple/branched outcomes. They are not capped,
convex-hulled or labeled garment volume. Projection envelopes are all-point
extrema, not occlusion-qualified silhouettes or intersection proofs.

The broad head envelope moves up to3.515mm in synthetic stress and approximately
2.7–3.1mm in actual dance. Its worst dance boundary vertex664129 is retained as
a counterexample, with adjacent original face/weights and rendered3D marker.
Do not reinterpret earlier narrow stable-head tests as this broad envelope.
Names such as jaw/tongue/shoulder do not establish leakage without identifying
the actual surface location. Neutral/box/kick C1 OFF/ON anchor differences are
exactly zero; the dance-specific scope is preserved.

`worst-edges-source-context.json` preserves actual original endpoint IDs,
rest/posed edge lengths and named weights so tiny-edge ratios can be assessed
alongside absolute distances; this is context, never an accuracy waiver.

Next candidate, after visual/anatomical boundary review: a versioned localized
pose corrective against the fixed C1 baseline, or a specifically justified
sleeve/torso weight partition. Preserve all original geometry/maps and stable
head/neck/elbow anchors; gate local worst tails, contours and unrelated motions.
No broad smoothing, mesh reduction, bone-name-only leakage assumption or
rest-shape alteration is authorized by these measurements.

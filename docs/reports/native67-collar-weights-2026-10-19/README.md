# Collar weight experiment — 19 October 2026

Original dense geometry, bind and animations unchanged. Derived full-positive barycentric baseline weights. A rest-coordinate smooth region selects medial shoulders/upper chest; it is NOT anatomical segmentation. Front point-raster mask checked visually; geometry outside the mask remains unchanged.

Region mask >0.25 contains 28,833 original vertices. Baseline combined upper-arm mass median .0610, 90th percentile .1653, 99th .2617, max .3223; 433 selected vertices exceed .25. These are measurements, not universal error thresholds.

Candidate transfers 60% × smooth mask × upper-arm influence to UpperChest. 34,302 vertices change, max mass transferred .09183. Every pre-existing positive influence remains positive, sums preserved within 3e-7, all weights outside mask byte-equal. No influence cap, no source geometry simplification or new compression. Scalar coefficient and ROI are empirical test choices, not an accepted rig.

Test schedule: same six dance frames as prior clavicle experiments; no bone correction. Production remains unchanged pending surface review. Packet and raw captures retained in art/drafts and .tools; only lightweight receipts and comparison sheets belong in this report.

## Completed surface checks

Upper-arm and clavicle candidates both completed six poses and numeric full PNT gates, but framebuffer repeat remains RED. A fresh baseline was captured with identical current helper/reference setup. Camera still auto-frames each surface bounds; tiny framing differences are possible.

Upper-arm attenuation: small local effect, insufficient visual improvement. Clavicle attenuation: changes collar/chest stripe curvature, but shoulder silhouette/hunch remains. Neither is accepted as the shoulder fix or promoted to game. The audit does not establish that baseline clavicle mass is intrinsically erroneous: it does establish that simple local redistribution alone does not solve the observed shape. Original geometry, topology, UVs, bind matrices and bone names were SHA-verified identical in the upper-arm candidate. Original positive influence support retained in both candidates.

Next controlled work: evaluate pose-specific corrective surface deformation in shoulder and upper-chest region with return-to-rest identity, continuity, no seams and combat-pose regressions. Do not increase global bone rotations or weight suppression merely to chase a single screenshot.

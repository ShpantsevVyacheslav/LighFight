# Offline original-weight / worst-edge witness

No engine was launched and no source, weights or candidate target changed.
Actual frozen original C1 dance50 world matrices were read from the independently
validated fullDance source cache at139/12seconds. This is not the derived FBX
or an interpolated export. Named bone order is explicitly remapped.

## What3.81× means

Original edge145536–146384 has rest length**0.241654mm**, posed length
**0.920992mm** (3.811194×; reproduces baseline3.811197× within float evaluation).
It is a microedge, not evidence of a centimetre-scale contour failure by itself.
The source sleeve envelope median edge length is1.026590mm. The current rendered
cloth has visible folds, but their visible importance cannot be inferred from
this single ratio; the correct −X spatial marker is prepared and still pending.

Endpoint named weights:

| Bone | Vertex145536 | Vertex146384 |
|---|---:|---:|
| DEF-upper_arm.R | .530174851 | .522186279 |
| DEF-upper_arm.R.001 | .177600875 | .174601734 |
| DEF-breast.R | .292224348 | .295992523 |
| DEF-shoulder.R | 0 | .007219557 |

L1weight change=.021975447, percentile61.8 among71,949 original neighboring
edges in the sleeve envelope. Region median=.01787289, p99=.09436169,
max=.41689226. Therefore it is not an exceptionally abrupt adjacent-weight jump.
Its change per rest millimetre=.0909375, above the region p99=.0711058 because
the edge is short. No weight-name or percentage alone establishes leakage.

Exact original LBS edge decomposition, with xbar midpoint and wbar mean weights:

`posedEdge = Σ wbar_b L_b Δx + Σ Δw_b M_b xbar`

The constant-mean-weight term has length**0.219734mm**; the weight-gradient term
has length**0.707483mm**. Vector-sum residual is5.47e−17m. This proves that
relative bone motion acting through the spatial weight gradient dominates this
specific edge's elongation. It does not prove that a particular bone weight is
incorrect or that removing that gradient gives a plausible garment.

The largest neighboring L1jump=.416892 on a2.096446mm edge has pose/rest=.897154
(compression, not extreme extension). High weight jump alone is not a sufficient
root-cause test. All worst endpoints, adjacent triangles, rest/posed positions,
named weights, region statistics and exact decompositions are in audit.json.

## Spatial distribution scope

The overlapping sleeve envelope means are upper_arm.R43.73%, shoulder.R33.74%,
breast.R17.65%, upper_arm.R.0014.39%. The torso-underarm envelope is breast.R54.22%,
spine.00214.22%, upper_arm.R.0019.48%, upper_arm.R6.32%, shoulder.R6.16%, etc.
These are broad spatial envelopes, **not a proven shirt armhole seam**. They show
a blended torso/arm attachment transition, but do not justify a global arm/torso
partition or automatic deletion of any named group.

## Required next images

Prepared tool: `../tools/render_weight_witness.py` (one CPU6 Blender process).
It evaluates unchanged original C1 dance50, checks marked positions against the
frozen source cache, then renders source front, **−X right-side**, right-back−X
and full-body front. Red/green mark exact endpoints; yellow is the actual edge.
The side-camera assertion rejects a +X camera regression. Markers can naturally
be occluded; they are not X-ray or projected synthetic surface evidence.
No run is authorized while presentation/diagnostics own the heavy window.

## Two justified approaches

| Approach | Concrete next operation | Advantage | Risk / required gate |
|---|---|---|---|
| A: artist posed contour target | Define explicit inner-sleeve/torso transition curve and supporting surface using the actual −X/front/back source views. A small3D target mesh/landmark artifact must state the desired shape; inverse-LBS maps only its local reviewed garment patch into a sparse morph. | Direct control of the visible fold; dance-only pose activation can leave other motions unchanged. | A single view can hide dents or inflate the sleeve. Need all3views, stripe flow, local edge/area tails, no new folds, exact head/neck/distal additional-zero support, and second extreme. Few localized targets only. |
| B: garment-only weight redistribution | After anatomical boundary review, fit a minimally changed nonnegative max4weight field on the local patch, retaining an independent original-weight artifact. Prefer an accepted target from A as a concrete fitting objective across several poses, rather than smoothing weights blindly. | No morph dispatch or per-pose target required if the field works over motion range. | Changes every motion and can trade a stretched crease for collapsed armpit/chest sliding. Require neutral exact rest surface, spatial boundary continuity, all source motions/extremes, head/neck/distal unchanged weights and measured contour/strain improvement. |

Recommendation: first obtain the −X witness and define an explicit artistic
contour for A. The failed v1 radial-deficit operation was not such an explicit
shape and should not simply be strengthened. B is a defensible later fitting
experiment because the LBS gradient term is measured, but is not yet supported
as a superior or anatomically correct fix. No full-mesh smoothing, geometry
reduction, original-map change or mass retarget/rerig is implied.

## Absolute length distributions (no gate change)

The3.811× witness has positive edge excess 0.679337mm. The rejected target changes its posed length by only 0.000121623mm. Its strict worst-strain fence remains RED.

| Original rest threshold | Edges | Absolute length change median/p99/max (mm) | Positive excess p99/max (mm) | Candidate Δposed edge min/max (mm) |
|---|---:|---|---|---|
| restGreaterThan0.1MM | 71947 | 0.078284 / 0.752837 / 1.905990 | 0.743796 / 1.905990 | -0.124442 / 0.043426 |
| restGreaterThan1.0MM | 37892 | 0.101253 / 0.816724 / 1.905990 | 0.804109 / 1.905990 | -0.124442 / 0.043426 |

Largest positive absolute excess is a different original edge[290465, 300366]: 3.169205→5.075196mm, excess1.905990mm. This is included as a second colored spatial marker. Its high breast.R weights locate a torso-side attachment hypothesis but do not prove a wrong bone weight or garment seam. Thresholds are diagnostic bins only; no original edges are removed from strict acceptance.

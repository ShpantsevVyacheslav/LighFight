# B — local garment weight-transition investigation (offline prepared)

No candidate weights produced or Blender launched. A/V3 and DQS rejected results
remain intact. Preserve original bound scene and indexed sidecar; a later derived
sparse weight artifact must explicitly cease claiming original named weights
bit-exact. Original994452vertices/1988914triangles, UVs, rest positions and image
bytes remain exact, including neutral geometry. No mass correction.

1. Four reviewed actual−X ray-hit cloth faces304975/225310/88778/129382 are
   witnessed in control-topology-original.json. Each lists original triangle
   corners/rest coordinates/named weights plus two-ring topology/aggregate
   influences. Reproduce with tools/audit_control_topology.py (NumPy only).
   These are **not** automatically the sleeve/torso seam. Inspect actual rest and
   dance50 marked topology before choosing boundary conditions.
2. Build a connected triangle-graph garment patch around the visible armhole,
   using reviewed cloth support, excluding all221624protected IDs. Geodesic
   connectivity prevents close forearm/torso surfaces bleeding across space.
   Keep neck/skin/face/feet and distal elbow/cuff outer boundary weights exact.
   Do not assign torso/arm roles merely from x/z or existing bone names. Artist
   review must identify torso-owned and sleeve-owned interior anchor bands.
3. Separate torso versus arm influence families anatomically. Keep clavicle
   participation where justified; do not zero all shoulder groups. Solve a small
   topology-aware nonnegative Dirichlet transition between fixed original bands,
   not repeated whole-mesh averaging. Original outside-patch weights exact.
   The first experiment may adjust only family mixture, retaining conditional
   within-family proportions; any new bone influence must be declared.
4. Require finite nonnegative normalized weights, max4, zero unweighted. A
   max4 projection can introduce discontinuities: measure its residual and
   spatial gradient; reject if it creates spikes rather than hiding them.
   Store original and candidate tuples by original vertex ID in separate NPZ,
   source SHA, boundary IDs, solver parameters, and complete diff statistics.
5. Evaluate game LBS only, same exact source C1 bones, dance45/50/55 first. Compare
   visible stripe/crease continuity correct−X plus front/back, full-resolution
   local contour and absolute edge excess as well as unchanged strict ratio
   fences. No threshold relaxation. Then all selected high-arm poses, neutral,
   BOX/kick and actual dance interpolation samples. Protected additional movement
   must be zero relative to baseline LBS because their weights are unchanged.
   Neutral evaluated geometry must match baseline within explicit float error;
   rest buffers must be bit-exact. Check cloth/skin penetration and outer sleeve
   volume/silhouette visually; do not force an open-shirt zero-volume constraint.

A solver is not automatically an artist-correct shape: reject if local sleeve
fold worsens, stripe flow stays pinched, new seam appears, or neighboring motion
fails. No approved B target or claim of wrong source weights yet. Await explicit
heavy-window handoff before any engine job. No Unity source/catalog changes.

# Sailor original density → native67 feasibility — OFFLINE ONLY

**No Blender/Unity launched, no candidate weights built, no source edits.** The
original source remains994452FBXcontrol vertices/1988914triangles with original
UVs/maps. Native67 is a motion-compatible donor hypothesis, NOT a proven good
shoulder/cloth donor. Stop before transfer if its matched raw dance has the same
unacceptable fold/trap shape. Selecting a different dance does not fix a rig.

## Concrete lineage and source evidence

Both untouched exports have Tripo task44623258-1c14-4afb-b55f-bb15ec154b50:

- Original dense: art/source/tripo-striped-sailor-apose-original/unpacked/
  tripo_convert_7b8f380e-26b3-4dc1-92a2-d433761960f0.fbx.
- Native: art/source/tripo-striped-sailor-native-full-pack/unpacked/
  tripo_convert_f24a44a7-a87f-4f34-9d47-6924c6224646.fbx.

Raw selective FBX audit finds994452vertices/1988914triangles vs49992/99999,
67cluster binds, native maximum4positive influences,0unweighted, sum deviation
5.2154e−8. The original raw float32positions match the existing dense original-
indexed sidecar bit-exact. The source provenance's1018642vertex field is retained
as upstream metadata; it is not substituted for the measured raw FBX control-
vertex count or an excuse to alter topology. Original triangle count is identical.
Source SHA/bytes and full metadata are in offline-source-audit.json.

Both files declare YUp and UnitScaleFactor100 (one metre per file unit). Their
raw vertices have matching roughly±.248mX/.98mheight bounds. Do not use the first
animation frame or animated Model defaults as skeleton rest. Installed Blender
4.2FBX export/import source explicitly stores raw cluster Transform in bone
space; reconstruct mesh global bind as **TransformLink @ Transform**. Across67
clusters those reconstructed matrices agree within1.6728e−7component. The raw
Transform matrices intentionally differ between bones. The initial pre-
interpretation report is preserved as offline-source-audit-r1-raw-before-
interpretation.json; its misleading mesh-transform label is not authoritative.
The corrected JSON identifies the raw and reconstructed fields separately.

## Sampled rest alignment, not full-surface acceptance

Source Model transform is the explicit−90°Xrotation and small source translation;
donor mesh bind comes from cluster reconstruction. No fitting, bone relocation,
pose matching or mesh shift was performed. Exact closed donor triangle distance
was evaluated for512original vertices (fixed random seed44623258), against all
99999donor triangles per sample:

|Metric|Distance|Normalized original height|
|---|---:|---:|
|median|.02943mm|.00300%|
|p95|.08338mm|.00851%|
|p99|.12243mm|.01249%|
|max|.15382mm|.01570%|

Original height.97998047m. Worst sampled vertex151002, donor triangle86815.
This is promising alignment evidence but **512is not994452**. Full DCC bind,
all-original-vertex nearest-triangle distance and pose/import parity remain
unperformed. Exact matrices/bounds/method/elapsed14.89s(one CPUthread) are in
sampled-rest-alignment.json. No performance/capacity inference from that timing.

## Previous experiments and why they do not prove this candidate

Rose reports .tools/tripo-dense-review/REPORT.md and SPATIAL_REPORT.md preserve
failures of nearest-triangle transfer, all-influence transfer, normal
recalculation and adjacency/virtual-weld smoothing. Those failed at shoulder/
armpit/waist despite exact rest geometry. Later continuous tapered48-neighbour
Gaussian spatial reconstruction passed selected box/kick visual gates; inferred
weights reached8influences. Master under .tools/tripo-dense-review/master-
original-dense/ likewise passed sampled native reconstruction; full geometry
and raw maps were retained. Other-actor vertex indices/weights are never reusable.

Historical nativeSailor67source itself already had a shoulder defect:
prototypes/roster-portraits/tools/shoulder/README.md reports sourceFBX versusGLB
sampled deformation below.73µm, disproving an export-only cause. Native weight
v1–v4, CorrectiveSmoothv5 and posed-smoothingv6 were rejected. Later C1native
prototype improved medial traps narrowly, not full skin quality. Fresh Rigify
localC1improved dense medial traps; A/B/CorrectiveSmooth experiments left the
underarm fold open. Thus a native67transfer cannot be promised to cure inherited
motion/cloth shape. No prior equivalent Sailor original-dense spatial-transfer
candidate was found in these reviewed reports; absence is scoped to the checked
owned folders/docs, not a claim that every historical artifact was searched.

## Next controlled comparison — before any transfer build

1. Untouched native67/99999triangle donor, exact own dance_01 raw source clip,
   **C1OFF/no CorrectiveSmooth/no DQS/no weight changes**. Compare fresh dense
   Rigify game-LBS with its original weights and rest-aware raw retarget at exact
   phases.45/.50/.55. Source frames251.2/279/306.8 at24Hz, seconds10.425/
   11.583333/12.741667, authored frames1..557. Preserve source duration/endpoints.
2. Same units/lighting/clay and posed-anatomy-follow camera, front/back/correct−X,
   plus fullbody bounds. Freeze framing once per pose for both variants. Do not
   reuse fixed dance50worldtarget for BOX/kick (the CSfollow-up cameraRED proved
   that insufficient). Geometry differs in donor-vs-dense control, so this can
   localize gross pose/cloth shape, not establish same-topology error.
3. Record actual clavicle/arm joint heads/axes against true bind and local torso
   up, region edge excess/contour and visible compressed stripe/crease. If donor
   has the same unacceptable defect, stop treating transfer as a solution. Keep
   it only as a comparative rig/weights experiment with explicit limitation.
4. Only after donor visual and fullrest gates, consider separate full-density
   spatial reconstruction. Existing script accepts GLB, not this originalFBX;
   adapt import/material handling instead of converting/re-encoding images or
   silently swapping to retopo atlas. No C1or corrective in the primary experiment.
   Original geometry/UV/image bytes/native67binds must remain exact.
5. Native source max4 is measured. Inferred dense field must report its actual
   influence distribution. No silent top4/top8 or1e−6threshold pruning: original
   tiny positive donor weights stay available. Earlier Rose defaults used such
   an inferred-weight threshold; do not inherit it without a separate explicit
   precision/loss/visual gate. Primary authoring can retain all positives; any
   bounded4/8variant is separately named and measured. No runtime claim yet.

## Animation-specific alternative controls, not a rig-fix claim

All11distinct downloaded source actions are local under art/source/unique-
dances-20261018/. Cached32source joint-position samples per action allow a
limited source-arm screening. Definition: signed elevation of ForeArm-head minus
Arm-head against normalized Spine-head minusSpine02-head. This is a world-position
proxy, not fullclip localChestmatrix or target-rig acceptance. Do not mix it with
presentation's Head-minusSpine02 angle convention.

|Source action|p95proxy|maxproxy|Existing roster assignment|
|---|---:|---:|---|
|66Boom_Dance|−15.60°|1.50°|Bone|
|64All_Night_Dance|13.81°|25.17°|Summer|
|74Gangnam_Groove|62.84°|68.10°|Sailor|

66and64are proposed lower-elevation diagnostic controls, not new Sailor roster
assignments.65Bass_Beats has lower p95−10.45°but occasional sampled maximum50.05°;
64was chosen to bound that sampled maximum too. All exact hashes/task IDs/source
paths and11ranked results are in available-dances-cached-elevation.json. No new
paid generation/download. No same-clip renaming or secretly duplicated dance.

The two alternate sources use their own donor skeleton. Validate rest-aware
Generic FK mapping/calibration independently before rendering on either target;
no blind local-angle copying or Human-solver oracle. Then compare both targets
on identical sampled low/high poses, preserving full geometry and original
weights. Missing finger channels/body-only scope must be explicit. A natural-
looking lower-demand dance may be suitable choreography; it does not prove the
skeleton repaired. Global unique-dance assignment remains root's separate gate.

## Remaining hard gates

Full original rest/triangle/UV/material-image preservation; all994452rest-surface
queries; every native67bind/name/parent exact; normalized weights/no unweighted/
actualinfluencecounts; matched raw source and target pose evidence; actual
shoulder/underarm quality; head/hands/feet; other native motions; temporal
interpolation; portable export/reimport and runtime memory/frame cost. Existing
100kfoot indices cannot be reused on full original vertices. No engine work or
candidate transfer begins without a new serialized heavy-window handoff.

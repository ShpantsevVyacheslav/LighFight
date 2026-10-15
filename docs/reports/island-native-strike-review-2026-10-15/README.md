# Island original-native single-strike review — offline packet

Status: PREPARED ONLY. No Unity compilation, trajectory measurement, GPU frame, accepted segment or gameplay change has been performed for this packet. Host phone build owns the heavy window. No source image/geometry/UV/bind/weight data edited; no main writes.

## Exact authority

`inputs.json` freezes original native7 FBX SHA + original maps SHA. Native box_01 source frames1–54 (0–53/24seconds), box_02 frames1–68 (0–67/24), front_kick_01 frames1–61 (0–2.5), at24fps. These are whole-take boundaries, NOT attack segment boundaries. No in/out/contact times are invented. Unity verifies these durations/type and measures actual imported vertex count,67bones,100000triangles/maxweights. The source hash must equal `5cc8b7208f3267cb1e20909bcff4eb04a9b9bf58ce7f28ebb77bbdb516c41a24`.

Earlier `IslandCandidateEvidence/Native/native-sheet.png` is four-phase **Human SDK** evidence, not rawGeneric authority. Visually box_01 has open hands near face; box_02 starts crouched and ends extended; front_kick_01 appears a turning/backward strike at phase.35 despite provider name. None establishes a usable closed fist, exact contact trajectory, facing or completed recovery. `dense-mixamo/box-01.reference.json` belongs to different65bone earlier input, not this67bone source and is deliberately excluded. Never map full boxing loop to Guard or one swipe blindly.

## Next run, after host releases heavy window

Use the existing owned `ProductionStreamPlayerUnity` snapshot (not main), containing current `NativeSupportPoseData`. Copy ONLY packet Editor/Runtime files into `Assets/IslandNativeStrikeReviewTools/Editor` and `/Runtime`. No copies of whole main/roster or texture arrays needed. The packer requires the existing LNC1 NativeSupportPoseData API; no duplicate class or GUID substitution. Keep original map files outside the project until a real material/GPU phase is scheduled. Model importer settings apply ONLY to own SHA-identical review copy.

Run in that owned project, with absolute `-islandNativeSource` original FBX:

```
Unity -batchmode -nographics -projectPath OWNED_PROJECT -executeMethod IslandNativeStrikeReviewJob.MeasureOriginal -islandNativeSource ABSOLUTE_NATIVE_FBX -logFile OWNED_PREPARE_LOG -quit
python3 PACKET/analyze_trajectories.py OWNED_PROJECT/IslandStrikeEvidence
```

MeasureOriginal inventories every binding type/path/property/keycount/time-range/wrap for all3takes FIRST, then samples raw imported Generic clip.SampleAnimation at240Hz over exact source boundaries, without invoking Export/LNC1 or checking encoder support. Raw CSV includes exactly67skinned bones, root-relative positions and full surface floor. Source FBX, original stored mesh hash and full render-attribute hash are checked pre/post. Separate ExportCodec (ONLY after independent source comparison) exports supported whole Transform channels without re-keying/trim/time scaling and runs scoped exact LNC1 roundtrip + importedGeneric/packed fullsurface parity. Unsupported Euler blocks ONLY that codec stage. No sparse human/thumb inference.

`candidate-windows.UNACCEPTED.json` lists only extension extrema of each hand/foot relative torso, bracketing minima and source finger endpoint distances. Euclidean reach alone cannot establish attack direction, contact or a closed fist; inspect full vector trajectories and actual mesh. A take with multiple peaks or no recovered endpoint remains rejected until one isolated event is explicitly selected. Raw times are always original seconds; source curves remain whole and exact.

After raw trajectory comparison is green, run ExportCodec with the same owned project (no reimport), then actual original-Generic-controller→packed EOF fidelity:

```
Unity -batchmode -nographics -projectPath OWNED_PROJECT -executeMethod IslandNativeStrikeReviewJob.ExportCodec -logFile OWNED_CODEC_LOG -quit
Unity -batchmode -projectPath OWNED_PROJECT -executeMethod IslandNativeStrikeReviewJob.BuildCodecProbe -islandReviewPlayer OWNED_APP_PATH -logFile OWNED_BUILD_LOG -quit
OWNED_APP_EXECUTABLE -islandReviewOutput OWNED_OUTPUT
```

Run windowed; WaitForEndOfFrame is not a headless screenshot substitute. Runtime oracle uses original imported Generic AnimatorController (not cloned legacy SampleAnimation). Alternating1/3core samples per frame, current source time evaluated every tick; original world full-mesh comparison before yield and EOF, no tolerance waiver if root-motion/controller semantics disagree. This intentionally detects the earlier runtime Generic-vs-legacy issue. It is a codec diagnostic, **not gameplay contact acceptance**. Full BakeMesh each sample is QA only, never a performance claim.

## Required subsequent segment/target gates (all PENDING)

1. Choose explicit original source start/contactpeak/recoveredend after measuring both hands/feet. Inspect source finger bones+mesh, knuckle positions, thumb outside curled fingers; open/slapping poses cannot be called punch. Native finger curves must remain authoritative; no shared generic fist overlay. Record actor heading and actual source kick direction, not provider label.
2. Preserve whole original pose asset. Segment metadata may map elapsedtime into original seconds only; one outward attack per input, no loop, re-trigger only through recovery. Startup/recovery and3active ticks remain meaningful; tune no hit radii and apply no noncontact damage.
3. Target actual calibrated Island prefab: compare rawGeneric against copied target after native FK overlay, same actual2× actor scale/heading/rootXZ anchor and grounding. Mesh/binds/weights hash unchanged. Human phase carrier is not geometric source authority.
4. Full surface every phase at1/240 plus60/90/120Hz holds; entry actual shared Guard→segment and segment→Guard/restart, Walk→attack, target fingers/clothing/feet; original all-shoe floor reference, selfarm/chest and opponent-body/feet penetration. Source endpoint continuity must be measured, not hidden by a fast blend. Measure every blend frame; native source start/end may be incompatible with current Guard.
5. Real Update→SampleContactPose→late root/fingers/native hook→ground→LateUpdate→EOF: world bones/striking surface match same sampled phase/heading, rootmovement and3coreticks/frame. Original physical contact spheres, active windows and health checks. Near/far/sidestep/evade/return/interrupt holdouts, both sourcehands audited. Add red mutation skipping native hook, wrong segment/striking side, lost return; require green after restoring.
6. Actual source-PBR full-body and close hands frame at entry/startup/contact/recovery/return, plus paired fighters actual fightcamera. No quality or phone acceptance until those frames reviewed. Current packet creates no catalog entries, guard changes or combat promotion.

This packet intentionally stops at preparation/measurement. It does not claim the unresolved segment/contact/transition gates are already implemented or green.

## Addendum: source-native30 fist versus existing SDK fist

`Editor/IslandNativeFistCompareJob.cs` and `Runtime/IslandFistEOFProbe.cs` extend the same project/packet. Root's external `native-fist-candidate.UNACCEPTED.json` chooses original box_02 **1.6s** by maximizing the minimum eight nonthumb geometric bends over671 samples (minimum90.56598°, mean93.53892°). This is selection evidence only; thumb/skin/contact acceptance is not implied. The job requires explicit `sourceSHA256`, `take`, `sourceSeconds`; freezes candidate JSON SHA. No segment/end recovery decision derives from this fist time.

Before capturing any native local rotations, require source AND target FBX SHA exact; full imported stored mesh/UV/triangles/binds/weights exact; ordered67bone full paths exact; complete hierarchy paths and every original localposition/quaternion (sign equivalent)/scale agree within1e-6component; own valid Human target Avatar; existing SDK pose references that exact Avatar; all30 mapped Human finger paths present in source and equal SDK path set. Source/native capture fullsurface error <=.01mm. All native finger localtranslations/scales must stay at originalrest at capture. **Mismatch blocks; no rest-frame guess, name-only retarget, corrective rotation or gate waiver.** Calibration is not assumed to preserve localframes.

Only after those gates creates `Assets/IslandNativeStrikeReview/NativeFistCandidate.UNACCEPTED.asset` with30 source-native rotations and exact own target Avatar. Existing SDK fist asset is read only and retained unchanged. No controller/prefab/catalog replacement. SDK comparison is the exact existing30rotation target at fullclosedweight (same endpoint as NativeFingerPoseDriver), not a re-generated SDK muscle guess. Its .12s runtime blend is NOT covered by this static endpoint comparison.

Both A/B variants receive identical original box_02 body curves, then only those30 finger rotations. Original body movement remains dynamic. Each sample checks all nonfinger localtransforms unchanged and ALL vertices with zero positive finger/descendant influence unchanged against original. Finger-weighted vertices are explicitly counted/exported; that mask is authored weight ownership, **not semantic clothing segmentation**. Any clothing incorrectly weighted to fingers requires the visual/full-surface gate to reject it. Mixed hand seams remain original topology/UV/weights and must be inspected, not smoothed or reweighted.

After raw MeasureOriginal, green independent comparison and supported ExportCodec, invoke in the existing owned project:

```
Unity -batchmode -nographics -projectPath OWNED_PROJECT -executeMethod IslandNativeFistCompareJob.Run -islandTargetModel Assets/LighFight/Art/Drafts/TripoIslandPlayable/Source/Island/model.fbx -islandSdkFist Assets/LighFight/Art/Drafts/TripoIslandPlayable/fist.asset -islandFistCandidate ABSOLUTE_REPO/.tools/island-source-bone-review/native-fist-candidate.UNACCEPTED.json -islandReviewMaterial Assets/LighFight/Art/Drafts/TripoIslandPlayable/Source/Island/native-pbr.mat -logFile OWNED_FIST_LOG -quit
Unity -batchmode -projectPath OWNED_PROJECT -executeMethod IslandNativeFistCompareJob.BuildProbe -islandFistPlayer OWNED_FIST_APP -logFile OWNED_FIST_BUILD_LOG -quit
OWNED_FIST_EXECUTABLE -islandFistOutput OWNED_FIST_OUTPUT
```

All model/fist/material paths must be the current approved Island source copies in **owned project**, never main AssetDatabase. OriginalPrepare raw clone/codec must exist. Existing owned snapshot may need only these Island source dependencies copied; do not copy roster or make a new bigproject. `Owned()` refuses main paths.

Outputs: `IslandStrikeEvidence/Fist/gates.json`, full240Hz `surface-comparison.csv`; EOF player `eof.csv` alternating1/3ticks with actual originalGeneric controller oracle; all3 source/native/SDK exact-index world surface CSVs at chosen1.6s; same-camera/body and3angles perhand1024PNG each. Surface CSV includes allvertices and zero-finger-influence flag for independent thumb/skin/sphere witness comparison. Camera is controlled direct Standard PBR LDR sRGB render target, fixed .8key/.35ambient, no tonemap; **capture color transport is unvalidated until actual GPU run**. It is not source appearance/phone approval. Source PBR material is explicitly required and references original maps; no pixel edits.

EOF requires <=.01mm world fullsurface drift per variant and <=.01mm original body-mask mismatch. This is codec/body/finger endpoint consistency, not gameplay-contact proof. Target head/body physical contact, knuckle/thumb intersections, folded fingertip skin, seam normals, Guard/Walk entry, return, blend continuity, attack windows/damage and visually acceptable source closure remain PENDING. No widened capsules, IK or body/clothing pose edits are introduced. A zero numeric error alone never accepts a fist.

### Tightened render/dependency identity gate

`Editor/IslandRenderAttributeIdentity.cs` adds a second independent hash. The original `IslandNativeStrikeReviewJob.MeshHash` remains required. Before any native capture, source and target must additionally have identical full render-attribute hashes: exact vertex-buffer bytes plus perattribute semantic/format/dimension/stream/offset and perstream stride, including authored normals, tangentXYZ/handedness, packed vertexcolors, all8 UV channels and any other stored vertex attributes. It includes indexformat and entire exact16/32bit indexbuffer, submesh order/topology/indexStart/indexCount/baseVertex/firstVertex/vertexCount/bounds, meshbounds, allbinds, every bonecount/influence, and any blendshape names/frameweights/position-normal-tangent deltas. Raw bytes include stored stream padding, so this intentionally may block equivalent-but-not-identically-stored imports; it never silently normalizes, welds, recalculates or repairs them.

Both original and full render hashes are checked again after the comparison. `gates.json.fullRenderAttributeHash` records the matched full hash. These hashes are prepared code, NOT measured imported compatibility yet.

A scoped readonly `FrozenAssets` guard hashes **before and after** the SDK fist asset, required saved source-PBR material, its bound/dependency texture files (including embedded texture container asset if applicable), their existing `.meta` files, and file-backed shader source/meta. It does not traverse/hash unrelated project assets. Unity builtin dependency paths are explicitly listed, not misrepresented as filehashes; an unsaved bound texture without an assetpath blocks. Output `asset-dependencies-before.json` / `asset-dependencies-after.json` lists exact path/SHA256/bytes. It runs after all final SaveAssets/scene writes and also on exceptions after guard acquisition. Missing files or any byte difference block. SDK and sourcePBR/texture files are never written by this code. Any failure writes both `blocked.json` and a BLOCKED `gates.json`, so a former successful status cannot survive a failed rerun.

### Independent source authority — mandatory after Prepare

Actual main target metadata is inventoried read only in `actual-target-import-settings.json`: **resampleCurves1**, optimizeGameObjects0, animationCompression0, normalImportMode0, tangentImportMode3, meshOptimizationFlags-1, optimizeBones1, weld0, maxBones8, minBoneWeight.001. Main metadata/FBX is not changed.

Prepare now explicitly sets **raw review resampleCurves=false**, animationCompressionOff, and copies `importNormals`, `importTangents`, `meshOptimizationFlags`, optimizeBones and related normal calculation/smoothing/blendshape-normal settings from the distinct SHA-identical **owned** calibrated source model passed as `-islandImportTemplate` (default own `Assets/LighFight/Art/Drafts/TripoIslandPlayable/Source/Island/model.fbx`). This copies settings into the review clone only. Target template meta SHA is frozen/checks unchanged; `import-policy.json` records actual enum/value policy and source/template hashes. Geometry minWeight0 remains original-preserving; if the approved target .001 import actually loses weights, existing exact mesh gate blocks rather than hiding that difference. No normal/tangent recalculation is invoked by QA; matching the existing import policy may itself specify generated tangents, which is an importer/render convention, **not proof of original FBX tangent authority**.

The old Prepare template hash remains frozen in `pre-source-authority-template-sha256.json`, previous reference compilation in `fist-reference-compile.pre-source-authority.json`; original `inputs.json` source/map SHA gates remain unchanged. Disabling resampling does not prove source preservation: Unity can still convert FBX curves, and independent comparison is essential. If unresampled import exposes unsupported Euler channels, Export must block; never silently turn resampling back on or invent Euler conversion/rotation order.

`compare_independent_trajectories.py` compares frozen Blender original-FBX67bone traces with Prepare's actual Unity Generic CSVs. It fits **one global uniform similarity that may include reflection**, trained only on the phase0 skeleton from each of3takes (201points). All remaining **1800phases /120600bone samples** are untouched holdouts. No per-frame/per-take fit, root reanchoring, bounds normalization, anisotropic scale, anatomical aliases, phase shift or timewarp. Joint spelling canonicalization only removes namespace-prefix, underscores, whitespace and case; ambiguous/inconsistent names block. Every timestamp has a one-to-one match within.00001s, with exact531/671/601frame counts and67sourcebones each. Prepare uses exact source-duration sampling count rounded from inventory, avoiding an accidental duplicate final float frame. Extra Unity nonbone transforms are ignored only when their canonical names do not collide with required source joints.

Source FBX SHA and all4 frozen Blender input file SHA/size checks are mandatory (`independent-source-inputs.json`). Default train/holdout maximum error gate `.0001` **Unity model-local units**, reported explicitly. It is a proposed .1mm model-space threshold, not a measured error or game-scale world contact tolerance. The fixed fit matrix, scale, reflection determinant, translation, joint spelling pairs, maximum timing error, per-take RMS/max/worst bone/time are recorded. Failure exits1. Numeric pass is bone-center trajectory agreement only, not skin/tangent/localtwist/worldroot/gameplay acceptance; LNC1 imported-curve parity remains separate self-consistency evidence.

After Prepare, **before using native finger capture as source-authoritative evidence**, run with bundled Python/NumPy:

```
BUNDLED_PYTHON PACKET/compare_independent_trajectories.py --blender-source ABSOLUTE_REPO/.tools/island-source-bone-review --unity-evidence OWNED_PROJECT/IslandStrikeEvidence --source-fbx ABSOLUTE_REPO/art/source/island-tripo-v1/unity-pack/tripo_convert_d0bdd788-3a48-49bb-b11c-48b67ea2b186.fbx --output OWNED_PROJECT/IslandStrikeEvidence/independent-source-trajectory.json
```

Do not proceed with acceptance if this independent report is missing/red. ExportCodec now enforces green report plus exact raw CSV fingerprints before packing. The isolated capture asset remains UNACCEPTED in every case.

Offline verification: `test_independent_trajectory_gate.py` applies known synthetic fixed similarity/reflection to real frozen source traces, verifies all1800holdouts pass, and verifies red for one1mm holdout hand mutation, perphase drift,20microsecond timing mismatch, and ambiguous jointname. `independent-trajectory-offline-tests.json` records5passes. These are tests of the comparison algorithm, **not actual Unity source parity**. No Unity/Blender/player executed in this preparation window.


### Current canonical stage order (supersedes earlier combined Prepare behavior)

1. `IslandNativeStrikeReviewJob.MeasureOriginal` (the compatibility alias `Prepare` now does exactly this raw stage): explicit unresampled import, all3binding inventories, then original Generic clip.SampleAnimation→240Hz67bone CSV and finite fullmesh samples. No NativeSupportPoseData export, codec invocation, supported-channel filter or Euler assumption. Writes original `.trajectory.csv` and `raw-measurement-complete.json` with source/import-meta/mesh/render/policy/CSV fingerprints. The optional originalGeneric controller is built from original imported clips only.
2. `compare_independent_trajectories.py`: independent source-trajectory comparison, writes `independent-source-trajectory.json`, including exact Unity CSV SHA list. A missing/red report blocks next stage.
3. `IslandNativeStrikeReviewJob.ExportCodec`: does NOT reimport or overwrite raw CSVs. Requires raw source/import-meta/policy/hash completion, source/render identity, green independent fitCount1/holdout1800/sourceSHA plus fingerprints matching all3 current raw CSVs. Inventories remain untouched. Preflights supported bindings of ALL3 takes before creating codec pose assets. Unsupported Euler/nontransform/objectreference throws and writes `codec-blocked.json` (`CODEC_BLOCKED_RAW_MEASUREMENTS_RETAINED`). No guessed Euler→quaternion encoding or resampling waiver. Successful importedGeneric↔LNC1 traces use separate `.codec-trajectory.csv` / `.codec-source-gate.txt`; raw evidence is never overwritten by this stage.
4. Only after supported codec succeeds: codec EOF player, exact-frame native-vs-SDK fist job/player, followed by still-pending independent skin/thumb/contact/entry/recovery visual gates. These stages remain diagnostic, never automatic promotion.

Inventory/measurement and codec-stage code are reference-compiled only; actual import/inventory/Euler presence is still unknown until host releases Unity. Preservation of raw evidence does not imply imported raw curves already match original source.

# Stage/builder portability and fidelity review

Reviewed the supplied stage helper, Master-specific builder, view component, dense byte reader and measured evidence against the completed main staging result. **Current tested Master review is accepted for review. This is not yet a general production builder or a target-phone capacity acceptance.** No main files were changed by this review.

## Required installation boundary

Put runtime helpers in **Assembly-CSharp**, outside `LighFight.Runtime.asmdef` (root uses `Assets/OriginalDensityReview`). They refer to default-assembly `NativeSupportOverlay` / `CandidateFootGrounding`; placing them beneath the Runtime assembly cannot resolve those types. Keep Editor helpers in an Editor folder. Do not replace main's existing interface/overlay/finger implementations with isolated copies.

## Concrete risks before wider automation

| Priority | Location | Finding and consequence | Suggested follow-up |
|---|---|---|---|
| P2 | `OriginalDenseLocalArtifactStage.Stage` | Validates and copies one file at a time. A late missing/hash-invalid file leaves a partial cache; a retry rejects already copied files even if their contents match. | Preflight the entire manifest, reject duplicate destinations, then stage transactionally; permit existing files only when their SHA256 matches. Current completed staging passed all hashes. |
| P2 | `OriginalDenseMasterReviewBuilder.Build` | Deletes/recreates standalone Avatar/fist assets before all data/training gates finish, and saves an intermediate prefab before body enrichment/audit. A failed rerun can leave the previous review option broken or incomplete. The admitted normal Master stays intact. | Generate into a temporary owned review folder, preserve GUIDs where possible, and promote the review output only after every gate succeeds. |
| P2 | `OriginalDensityViewMode.CaptureOriginalPortrait` | No `try/finally` restores camera target and mesh/material if rendering throws or allocation fails. | Restore them in `finally`, check RT creation and release the new RT on failure. Successful captures/proofs are unaffected. |
| P2 | `OriginalDenseMasterReviewBuilder.Build` / `AuditFinalData` | Final named builder checks counts, native binds, palette order and exact vertex/influence arrays, but does not re-hash UV0/topology/material texture bytes after local staging. Those were gated by the reconstruction/import pipeline and stage SHA256, not this final method. | Carry the reviewed topology/UV/image signatures into a final end-to-end validation record so later cache edits cannot pass on counts alone. Do not weaken the existing source import gates. |
| P2 | Builder training and runtime proof | Training uses regular direct Animator states without all Core-contact replay or every blend trajectory; actual current-player proof covers four samples per Human combat state plus native entry/holdout/return. It does not establish every simulation tick / multiple-core-ticks-per-render behavior for the new dense support set. | Main owner should run its existing contact/render replay and continuous floor suites on this review option before promotion. Preserve the .22 combat cap; do not substitute a larger cap for missing support evidence. |
| P3 | Master constants / controller traversal | Hardcoded Master asset paths,1,031,866 split vertices,67bones, direct Base Layer states, four role names, private finger `pose` field, and `AnimatorController` cast intentionally match the current tested template. Blend trees, nested state machines or an OverrideController are not generalized. | Parameterize a future per-character manifest and fail explicitly on unsupported controller structures. Source alignment must be measured afresh for every donor/original pair. |
| P3 | Generated import / runtime resources | Local inputs use absolute paths and Unity6000.3.26-derived split/quantization data. GPU weight support, material normal conversion, memory and shader behavior remain platform contracts. `UploadMeshData(true)` drops the CPU copy of the shared mesh for all users of that asset. | Reproduce import/rest/UV/topology/weight and surface proofs on another toolchain/device; use the precomputed serialized grounding data at runtime. Preserve canonical raw weights/images locally. |

## Fidelity conclusions supported by evidence

- Original geometry/UV/image bytes are preserved; the renderer surface is transported to the original67 native binds, with no decimation/subdivision/bone reshaping.
- Weight associations are restored from canonical sparse source records using original vertex IDs and bone names, not the FBX import's truncated influences. Engine normalized storage rounding remains declared.
- Skin weights are an **inferred continuous transfer**, not unknown provider high-density weights. The preserved original rest surface and inferred animated deformation are different claims.
- The standalone source Generic318-pose proof and current Human/native248-pose proof are distinct. Source-equivalence comparison uses actual ordinary Generic Animator evaluation, and Human renderer measurement uses the verified `BakeMesh(true)` contract.
- Final main review's dependency graph has no low mesh / FBX / retopo-atlas load. Cloned Avatar/fist assets retain native Human compatibility without retaining FBX subassets.
- Quiet one-actor Mac guard67.7ms median /91.3ms p95, no shadows, supports keeping maximum density separate. It does not justify a phone fighting default. Cached original1024 imagery remains available without ongoing dense skinning.

No new source-fitting, geometry, weight, bone or material changes are proposed by this audit. Robustness follow-ups above are not silently merged into the already staged main result.

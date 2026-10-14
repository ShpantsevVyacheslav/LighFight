**Assembly placement:** install runtime helpers in Assembly-CSharp outside Runtime.asmdef; root successfully uses `Assets/OriginalDensityReview`. See [PORTABILITY-REVIEW.md](PORTABILITY-REVIEW.md) for the completed audit and remaining production/generalization gates.

# Current-game Master original-density review option

This packet creates a separate **MasterOriginalDensityReview** prefab. It does not replace the admitted Master or promote a phone quality setting. Its animation controller, FighterView contact configuration, native high reaction and original NativeSupport entries come from the current normal Master prefab.

## Main import and reproducible generation

1. Keep all large local sources/generated inputs outside raw git. Root should ignore the generated `Assets/NativeDense/` cache before staging; this packet does not modify `.gitignore`. The untouched original GLB, canonical303MB blend and114MiB dense rig FBX stay local. No source FBX needs to be committed.
2. Install the `.cs` runtime files used by this pipeline: `OriginalDensityViewMode.cs`, `DensePoseData.cs`, `DenseFighterCompatibilityProbe.cs`; install the named Editor helpers `OriginalDenseLocalArtifactStage.cs`, `OriginalDenseMasterReviewBuilder.cs`. For full reconstruction/import rather than reviewed generated artifacts, also install `OriginalDensePipeline.cs`, `OriginalDenseRuntimeBuilder.cs`, `DenseNativePlayerProof.cs`, and `DenseNativeGrounding.cs`; configure their original Generic FBX input path from the local artifact manifest (the tested isolated path is `Assets/NativeSupport/Master/raw.fbx`). Existing main FighterView/NativeSupport/fingers/high-reaction classes are dependencies and must not be replaced by old isolated copies. Do not install the diagnostic isolated IActorCurveWarmup interface in main; main already owns it.
3. Set `LIGHTFIGHT_DENSE_LOCAL_INPUTS` to this packet's absolute `local-generated-artifact-inputs.json` path, then call **OriginalDenseLocalArtifactStage.Stage**. It validates each SHA256, stages generated mesh/material/UV-original map/proof files and matching metadata into the ignored review cache. The sole original-atlas shader and its metadata are staged here; avoid creating a second shader asset with the same name by separately importing the shader without its original metadata.
4. Call **OriginalDenseMasterReviewBuilder.Build**. The normal source prefab must already exist at `Assets/LighFight/Art/Drafts/TripoMasterPlayable/MasterHumanoidCandidate.prefab`. The builder validates source67binds bybone name, maps the dense palette to the unchanged native hierarchy, clones a standalone native Human Avatar and fist asset, swaps the renderer's mesh/material only, and saves `Assets/NativeDense/Master/MasterOriginalDensityReview.prefab`. The admitted prefab/controller/catalog/source meshes remain unchanged.
5. The builder derives341 dense foot support points from125,243 full foot/low-rest-band vertices over1,879 regular state samples, then calls **EnrichNativeBody** for361 regular120Hz original Defeat samples across every dense vertex. That produces105 separate full-body support points. It preserves the inherited **.22 combat correction cap**. Existing native support logic derives its native offset from actual weighted support extrema; no global cap inflation or sourcevertex shift is used.
6. **AuditFinalData** gates exact final vertex arrays, native bind arrays, all quantized influence IDs/weights and renderer bone-order correspondence against the standalone proof buffer. **BuildPreviewPlayer** is an optional review-scene Mac build hook; it uses the existing runtime APIs and ordinary Animator evaluation. Root owns all main project/build actions. The tested isolated project is in `asset-inputs.json`.

All build-only data is excluded from the review prefab's runtime dependency graph. The generated prefab has one original4K atlas, a standalone native Human Avatar/fist, original native pose bytes/carriers and the dense mesh. It has **zero FBX dependencies**, zero100k mesh dependencies and no retopo atlas. The generated cache is about246MB before the additional11MB canonical sparse provenance. The final dense mesh alone is about203MB as an Editor asset; do not rawgit it.

## Latest actual player evidence

`Evidence/Fighter/Player/result.json`, `poses.csv`, screenshots and `final-data-audit.json` are authoritative for this current-game option. The earlier318-pose Generic proof remains a separate source-rig transport test.

- **248 actual EOF cases:**22 current Human combat states plus four original native support states (Victory/Dance/Greeting/Defeat), including entry, independent quarter-grid holds and return to Guard.
- **32 complete native holdouts ×1,031,866 vertices** compare final GPU/Bake surface with CPU LBS from final stored weights/binds and current native bones. Maximum discrepancy **0.001921629mm**; other entry/combat/return poses additionally sample the surface.
- Worst full-foot or full-body floor minimum **-0.2064779mm**; maximum support-proxy miss **0.2064779mm**; maximum native hip XZ anchor error **0.00003264946mm**. No unweighted vertices or reduced influence option is used.
- Final mesh/proof vertex, bind and influence-weight discrepancies are **zero**. Maximum final weight-sum error **1.192093e-7**. Renderer bone-order names match the authoritative original native order.

An earlier32-point body set failed an independent Defeat holdout by6.53mm and was rejected. The120Hz body training grid excludes the quarter-half holdout phases, and the revised105-point set passes. No failing sourcevertex or bone was moved. `BakeMesh(false)` was also rejected for this non-unit-scale Human renderer: it disagreed with final CPU LBS by up to2.35m and produced false26mm floor defects. **BakeMesh(true)** is the verified measurement contract here and matches the actual final skin to0.001922mm. This measurement correction did not change renderer data, source pose or grounding offsets.

The original surface itself is preserved:1,026,465 original vertices and1,992,276 triangles, plus5,401 engine splits; exact original vertex coverage/connectivity/UV0 and untouched original image files. Transferred skin weights are inferred from the original100k provider rig; they are not unknown original provider high-density weights. Canonicalfloat32 weights are preserved locally; final engine normalized storage has maximum3.546476e-5 weight rounding. Source rest geometry transport error is0.000206mm at source scale. There is no subdivision/decimation/fake detail.

## Portrait and capacity

`Master-original-playable-portrait.png` is a1024px original-density, original-atlas Unity render suitable for root visual review. It is not automatically installed as a main portrait. The live original2m option stays available independently of any cached image. `OriginalDensityViewMode` deliberately has no low-mesh/retopo-atlas reference in this option; choose the separate accepted prefab to change tiers.

The previous38.3ms median timing was provisional and contended. Its explicit Render stopwatch excludes BakeMesh, ReadPixels and CPU reference work, but its30 warmup frames lacked explicit rendering. A new quiet branch (`--dense-current-capacity`) uses the current HumanGuard low/high prefabs and nativeDance,30 actual raster warmups,120 actual raster measurements per mode, and no diagnostics/readbacks inside the timing windows. Both comparison prefab assets are loaded in that diagnostic build, so its total allocated memory is not a dense-only shipping RSS. Shadows are disabled for this controlled comparison. No phone or full-shadow game capacity verdict follows from it.

Quiet current-game Mac results (presentation_review confirmed its Unity/player audits idle for the measurement window):

| Controlled mode | Median wall | p95 wall | Median Render submission |
|---|---:|---:|---:|
| Accepted100k HumanGuard |6.59ms|10.28ms|3.77ms|
| Original1.992M HumanGuard |67.71ms|91.32ms|62.52ms|
| Original1.992M nativeDance |58.27ms|73.61ms|53.66ms|

120 measured frames per mode after30 real renders. Actualimages and `quiet-capacity.csv`/scope/summary are included. Mesh resource estimates:10.64MB low vs196.31MB dense. The dense max option is locally usable and faithfully rendered, but is too expensive to recommend as default fighting quality from these desktop results. A cached1024 portrait avoids ongoing dense skinning; capturing it remains an explicit one-time cost. Full source geometry remains available as the max option.

The completed bust portrait is1024×1024 with transparent selection background; `portrait-alpha.json` records alpha extrema/corner and pixel counts. It preserves original hand/face/cloth pixels and mesh; root should review its composition and original finger shape before choosing it as the main image.

The max-option view component drops the original mesh CPU copy on runtime enable (`UploadMeshData(true)`), matching the unreadable-mesh state used in player proofs and memory sampling. Editor data remains readable for all preservation gates. Runtime grounding/contact uses serialized bindpoint influences; it does not need source vertex/weight access.

Additional neutral source portraits are in `Evidence/GreetingPortrait`: original Generic Greeting frame0, front bust and closer face; no Human/finger driver, allsourcevertices parity0.01147mm. The earlier guardportrait remains unchanged.

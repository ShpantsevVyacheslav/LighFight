# Bone: calibrated Humanoid test candidate

No main edits or production promotion. Original native mesh: 100,000 triangles, 59791 Unity vertices, 67 bones, 54 Human mappings. Generic-to-Human imported geometry, UVs, indices, bindposes and weights have identical hashes. All native bone rest positions, rotations and scales have zero error. Target-specific fist captures contain 30 joints and reference this character's own Avatar.

Original native maps are preserved byte-for-byte at their actual source resolutions. Standard PBR uses albedo sRGB, linear normal/metallic/roughness, and a derived metallic-R / smoothness-A packing. Source texture pixels are untouched. Final material appearance, lighting and game-camera review remain integration gates; source preservation alone is not final presentation acceptance.

## Combat evidence and disabled moves

`BoneCandidateEvidence` contains all 24 contact/recovery/guard-return frames and sheets, 1,203 finite core-timed poses, capsule collision trials at four distances, and target contact-height measurements. Guard return after 0.20 seconds matches the same idle phase exactly.

Packaged combat contacts: 23. Disabled: sweep, roundhouseKick. Sweep was already rejected; other omitted moves fail this target's high-strike contact-height band. Packaged roles, contacts and rebuilt controller omit them. The test UI must disable them as well. Raw diagnostic prefabs retain all 24 for comparison; the main-adaptable builder creates the gated candidate.

## Corrected sole verification

Every imported vertex with any positive Foot/Toe weight is evaluated using its original complete skin influences. Across 2,694 core-tick, independently advanced half-phase, and guard-return samples, the new support proxy uses 96 original vertices. Half-phase sampling explicitly sets Animator.speed=1, advances 1/120 second, restores speed, and asserts normalizedTime advances. Initial zero-speed holdout rows were invalid and replaced.

all original Unity imported vertices with ANY positive Foot/Toe weight=7503
originalSparseProxyCountNotRetained
core tick + half tick holdout + guardreturn samples=2694
trainingExtremaVertices=95
finalSupportVertices=96
finalInfluences=230
oldSparseSoleMinimumMaximumErrorMeters=0.1205481
trainingProxyHalfTickMaxErrorBeforeEnrichmentMeters=0.0008248687
finalProxySoleMinimumMaximumErrorMeters=0.0004563928
worstCorrectedAllShoeVertexFloorMeters=-0.0004476961
all24 sampled; exact original shoe-associated vertex skin weights evaluated; mesh/UV/weights/native bones unchanged; sampled grid guarantee only, not arbitrary continuous poses or native8


This is a sampled-grid guarantee, not proof for arbitrary continuous poses or native-support transitions. No geometry or weight reduction occurs.

## Native motions

8 post-Humanoid native clips are exported in `Assets/Bone/NativeClips`. `Native/native-sheet.png` and individual frames show four phases each with the actual calibrated SDK Avatar. AlwaysAnimate is required for preview actors; initial culled captures were invalid and replaced. Inspected gestures are finite and anatomically coherent, but full native-to-combat transition, grounding and collision gates remain pending. They remain separate from the packaged shared Meshy Humanoid combat/support mapping.

## Scoped integration

Use `copy-manifest.json` to copy into `Assets/LighFight/Art/Drafts/TripoBonePlayable`. Preserve FBX and texture/material metas, including the calibrated Human importer. Copy configuration JSON and the sole support-index text file into that folder. Put `TripoBoneCandidateBuilder.cs` in an Editor folder and run `LighFight.Editor.TripoBoneCandidateBuilder.Build`. Default model/material are under `Source/Bone`; CLI overrides `-qaTargetModel` and `-qaTargetMaterial` are available.

AssetDatabase regenerates references to existing main FighterView, NativeRigBones, NativeFingerPose, NativeFingerPoseDriver and Core. Grounding uses separate test runtime helpers; instantiate CandidateGroundedContacts for the test contact resolver. Main PhysicalContacts is untouched. All 67 native rest bones are restored before saving, with existing game-height root scale ×2. Support indices are importer-specific and wrong mesh counts/indices are rejected.

`builder-verification.txt` records IsReady, accepted contact count, absent disabled states, original shared mesh, zero rest-transform errors, correct target fist Avatar and support count. Per-character actual-player performance is not measured yet. Test integration and final art review remain required before shipping.

Actual400×500 render-target previews of gated candidate guard, jab and frontkick are in BoneCandidateEvidence/Game400. These use original PBR, target fist and corrected support proxy; CPU frozen-surface rendering usesBakeMesh(true) plus originalskin.lossyScale. They are not main gameplay camera or playerperformance screenshots.

Source resolution correction: Native Bone source has4096²albedo,1024²normal,2048²metallic/roughness/RM; the earlier phrase“allmaps4K” was inaccurate. No textures are upscaled. See material-quality-audit.txt foractualUnitydimensions, linearimports andskinquality caps. Unity serializes minBoneWeight.001despiteattempted0; importedGeneric/Humanweightparity isverified, butrawFBX→Unityweightparity isnotclaimed.

Skin quality audit: all three imported native meshes have max4 influences per vertex. Importer custommax8, actualUnityminBoneWeight.001. QAqualityUnlimited andrendererAuto; chooseBone4orUnlimited in targetqualitylevel to preserve all imported influences. FBXsource bytes remainunchanged; rawFBX-to-Unity exactweightparity is not claimed.

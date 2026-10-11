# Native Meshy animation pipeline

Use the **web model's native rig and its own clips as one export**. API rigging of the same surface produced a different skeleton (24/25 versus 66 joints) and is not interchangeable with this export.

Meshy web export: Animate → select model → Added animations → Download → GLB → Mixamo → rigged character → All added → One file. Keep the complete source export. The actual downloaded file is authoritative: browser download events can time out even when a file was saved. Verify file modification time and its animation inventory.

`scripts/art/export_meshy_web_unity.py` exports only the skinned model, clears imported NLA tracks before baking each action, and retains native vertices, UVs and weights. Blender's glTF importer creates an Icosphere bone helper: it must never be exported as character geometry. No decimation, remesh, custom fists, manual torso correction or cross-rig retargeting is applied.

Unity: Generic avatar from this model, no animation compression, no mesh compression, eight influences per vertex, zero weight threshold. Materials must come from this exact export. Compare the same animation phase, camera angle and render mode; Meshy screenshot uses unlit mode, whereas directional PBR highlights reveal more surface irregularities.

Inspect poses with LighFight → Character Rig Inspector. `MeshyWebReview.PrepareAndCapture` creates a native review prefab/controller and samples every clip at 61 phases. Finite bounds checks are structural checks, not artistic acceptance. Review hands, face, shoulders, knees and waist visually before promotion.

# Meshy scenes as arenas

[Scene Compose](https://docs.meshy.ai/en/webapp/guides/scene-video/scene-compose) arranges separate models by position, rotation and scale. It is useful for arena composition, but a generated photo diorama is not automatically a playable level. Export scene assets to Blender/Unity for final assembly.

Imported arena contract: separate flat `FightFloor` at y=0 covering at least 11 × 4 m; other geometry outside that clear combat volume. Select imported FBX/prefab and use LighFight → Import selected Meshy arena. The importer preserves placement and materials, removes decorative colliders (simulation owns movement), adds the existing OfficeArena host component, and rejects scenes with obstructed or missing floors. Register the validated prefab in GameContent only after visual review. A single merged mesh cannot satisfy this contract without separating its floor and decorations.

## Verified causes of the discrepancy

The earlier path mixed the API's 25-joint rig, custom rigid fists, local corrective geometry and clips retargeted from another skeleton. The web model instead has 66 joints, including fingers. More triangles alone could not repair changed weights or incompatible animation bindings. The official multi-animation FBX export also contained frozen Running poses; its GLB counterpart retained real motion. Blender NLA strips had to be cleared to prevent that frozen action being baked into every clip.

The source used here contains exactly 66,364 triangles and 33,184 vertices with 2,048-pixel textures. Those are the actual web rig's assets, not a claim that a separate 300k/4K source has been restored. GLB assembly checks the unchanged mesh blob and every motion channel against the input. Renaming the exporter prefix from mixamorig to smartrig is allowed only with matching joint hierarchy and rest transforms (maximum observed float-rounding difference 0.0000061).

## Production integration

Island fighter (catalog slot 3) now uses this native model, 25 explicit attack mappings, native guard, forward/backward walking, crouch, hit reaction, fall, get-up and dance. Unity uses its original finger animation, mesh and textures. Gameplay adjusts the whole actor's heading and timing, never individual body bones or vertex weights. Root motion is anchored horizontally in the Animator's coordinate frame, preserving vertical motion.

Contact calibration records the striking side and a reachable phase at the opponent's actual height. Collision uses native finger knuckles as the hand centre. Attack radii remain unchanged; a short generated hammer strike was rejected and replaced instead of adding invisible reach. The geometric tests audit real contacts, misses and dodges.

The Character Rig Inspector supports playback, pause, frame stepping, speed, rest pose and skeleton overlay on the same posed surface. Its baked mesh scale compensates for FBX's centimetre import transform, preventing misleading oversized preview geometry.

Other characters have not yet been migrated to this native web export path. Phone acceptance of the new Island rig is pending; prior device results belong to the previous pipeline. No push or asset deployment.

## iOS handoff

Native framework exported and built successfully for iphoneos from commit `74db1b98dbd561cdbfa3eaea226ac4e20d6e3a72`. Delivery: `.tools/delivery/74db1b9/UnityFramework.framework`, including Data; stamp alongside. Binary SHA256: `4618ad99d0e227efd8a058b02b4d8e031419a33724eb1025f28d4ddab1704332`. Sent to the «Нативная лента iOS» thread for its next LighChat build; shared plugin framework was not overwritten during that agent's work. New phone acceptance remains pending.

## Current source revision (12 October 2026)

The later visible web source is task `01a12cb8-8e0d-75b1-973b-6f75e4c7cbcf`: 103,074 triangles. This supersedes the 66,364-triangle source for the quality investigation, but has **not** yet replaced the game fighter. Direct web Wave FBX preserves this surface and its native rig; the Blender import has 80 bones including FBX end bones. The production fighter still uses the previous 66-joint export.

`merge_visible_web_fbx.py` refuses differing bone inventories, parents, rest matrices or object transforms. It requires explicit names for multi-action files because FBX names may be truncated. Single-action filenames with `_Animation_` provide an unambiguous provider clip name. A successful merge is not visual acceptance or a round-trip geometry/weight proof: the audit reports this separately.

The web exporter limits All Added to 20 motions, including default Running/Walking/Spartan. Removed motions remain in the library. First export group currently contains 11 custom strikes, these three defaults, forward/backward fight walk, left/right jab, left hook and right uppercut. Remaining motions must be restored and exported separately. No additional credits consumed in this web selection/export pass; balance remained 3,739.

Selection no longer forces Guard on a resting preview. It uses Greeting when present, otherwise native Idle. Dance remains an icon. Import permits explicit optional Greeting and Victory states without inventing or aliasing them to unrelated movements.

Verification: 119 Unity EditMode checks passed on this change; two bind-compatibility tests reject modified parents/rest transforms/root scale/non-finite matrices. Direct Wave merge reports 103,074 triangles and the semantic `Wave_One_Hand` action. Device delivery remains separate and pending.

The Wave FBX export now has a verified Blender round trip: 103074 triangles, maximum world-position delta `2.384185791015625e-7` m, UV delta `0`, weight delta `0`. The gate compares original vertex ordering, named influence assignments and UV loops after reimport and fails on mismatch. This validates geometry/UV/weights for this file; it does not establish artistic pose acceptance, normal-map parity or animation-curve equality. External textures are assigned explicitly in Unity, not silently copied from missing `.fbm` paths.

Web bindings have now been restored: Added shows all 40 original motion cards again (including Wave, Fight Idle, reactions, throws, dance and victory); balance remains 3739. The first group is documented for future export splitting, not the current web state. The connector supplied the exact native rig GLB/FBX for independent audit; it contains 66 joints and no clips. It cannot substitute for the missing strike exports. Comparison of old/new rig node transforms rejects a direct clip transplant despite their 66 common bone names.

Selection rendering also had a separate resolution/aspect defect: fixed 800×720 was stretched into the physical preview rectangle. Maximum now sizes the render target from safe-area pixels and preserves aspect when capping at 2048. A 2868×1320 safe area maps to 1285×924 (2.06× more pixels). This increases preview clarity; it does not fix skin deformation or prove phone quality acceptance.

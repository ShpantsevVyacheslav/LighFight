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

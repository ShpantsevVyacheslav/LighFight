# Tripo room pipeline: next step after two table-only results

18 October2026. Research only: no paid generation, CUA interaction, Unity launch or source geometry change.

**Do not repeat the same full-room submission unchanged.** Photo01 and its derived cutaway both produced a table/chairs, and local GLB review confirms that walls/floor/fridge are absent. The upload provenance and output reviews are in `art/source/kitchen-tripo-v1/` and `art/previews/kitchen-tripo-v1-v2/`.

## What the official controls actually establish

The official H3 image-to-model parameter reference documents image autofix, texture/geometry quality, orientation, alignment, parts and export controls. It does not list a scene-preservation or background-removal-disable parameter. `enable_image_autofix=false` disables optional input optimization; it is not documented as retaining the background. Texture alignment and image-aligned orientation are not scene-content controls. More polygons/textures cannot restore objects never generated. H3's parts mode segments the generated model; it does not promise reconstruction of every object in the source scene. [Tripo H3 reference](https://docs.tripo3d.ai/model-generation/image-to-model-v3-0-v3-1.html).

Tripo's input guidance is subject-oriented: a clearly visible subject, clean background and little occlusion. This is consistent with a prominent table winning over the room, but **that causal explanation remains an inference**. We have no captured internal preprocessing mask/request proving that background removal caused these failures. No verified web-only “keep room” setting was found in this read-only research. [Official image-to-model input guidance](https://developers.tripo3d.ai/en/docs/generation-image-to-model).

The official Studio tutorial describes the ground plane as a positioning/measurement aid; seeing it in the viewer does not mean a floor will be exported. [Studio tutorial](https://www.tripo3d.ai/blog/tripo-studio-tutorial-english).

## What was different in the successful courtyard

`art/source/courtyard-tripo-v1/generation.json` records H3.1 using `art/references/courtyard-tripo-diorama-v1.png`. The viewed reference depicts the entire yard as one isolated diorama, with a thick, contiguous ground base and neutral exterior background. No special preprocessing/background parameter is recorded, so there is no confirmed hidden setting to copy. The original exported courtyard contains1,922,912triangles. Its later `game-draft/layout-audit.json` records ground-vertex adjustments, preserved texture bytes and validated11×4m clearance: that playable assembly was an additional step, not proof of automatic gameplay-ready reconstruction.

## Recommended next trial, only after explicit approval to spend

Use the same original `assets/references/photo-01.jpg` as the sole visual source. Prepare an **architecture-only derived reference**, clearly labelled as an edited interpretation: remove the foreground table/chairs, preserve the photographed fridge/window/door/wall finishes and their relative arrangement, show the floor perimeter as a connected thick base against neutral exterior, and make the architectural shell the dominant silhouette. Do not replace those features with an invented generic kitchen. Keep original, edited image, hashes and edit description together. First inspect that derived image before any submission.

Make at most one controlled geometry trial. Record exact input/settings/task/model and any visible preprocessing preview; use H3.1 without remesh/low-poly/face reduction. If the selected route offers geometry-first review, inspect the local mesh before buying textures. Reject unless there is an actual floor surface, at least the requested room boundaries/window/fridge, and usable depth. Compare front/three-quarter/top against photo01; no promotion merely because a task succeeds. This trial is a hypothesis, not a verified fix.

The more predictable fallback is **photo-based architectural reconstruction plus Tripo-generated reference props**, explicitly named that way. Reuse the existing table asset as furniture only; create fridge/counter/etc. from separate photo01 crops when approved; reconstruct the floor/wall/window shell to match the photo, with inferred dimensions labelled. Preserve Tripo masters unchanged. Keep X±5/Z±1.5 combat space clear, placing props at its perimeter. Do not describe the resulting room as a single full-room Tripo reconstruction. Tripo's own game workflow separates generated assets from level assembly in the engine. [Official game-scene workflow](https://www.tripo3d.ai/blog/workflow-game-scene).

No automatic decimation/compression or switch to low-poly generation is proposed; the user's full-source requirement remains in force. The next deliverable should be a reference/layout approval artifact, not another unreviewed paid retry.

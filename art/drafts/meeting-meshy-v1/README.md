# Meshy replacement props — import adapter

`import-plan.json` specifies five source paths (initially null), uniform height normalization, yaw and separate review/gameplay placement. The requested carpet sits on negative-X wall opposite the windows, with a +X inward normal. Its repaired texture is a separate input.

`tools/assemble.py --with-base` makes a review room; `--scene gameplay` makes only placed props for the gameplay shell. Missing models are explicitly reported. The assembler copies complete original geometry/material/image binary payloads and changes only parent transforms. It computes true bounds through full node hierarchies, does not trust accessor min/max, and records SHA/scale/placed bounds. Facing and aspect quality require actual renders; uniform scaling never repairs a bad generated shape.

No Unity catalog publication is implemented in this adapter. `MeetingDetailsSetup.Run` remains disabled. The older manual props are rejected review work, never used as fallback for missing Meshy objects. Actual generation files must be added to source paths before visual acceptance and Unity import. A red cup may be a separate lightweight auxiliary object, as agreed by root.

Bundled NumPy Python runs `tools/test_adapter.py`: hierarchy scale/translation, yaw normalization, target height/center and exact source geometry payload preservation gate.

`tools/build_gameplay_shell.py` creates a separate widened room shell, keeps furniture proportions/UV/maps, moves furniture behind combat volume, and checks exact stored texture payloads. `assemble.py --scene gameplay --with-base` combines that shell with generated props using gameplay placements. `export_unity_candidate.py INPUT.glb OUTPUT --gameplay-shell` separates/batches authored architecture into bounds-safe material zones. This avoids publishing the small review room or accepting a merged wall renderer that spans the combat volume. The Unity `ReviewOnly` entry may validate/capture the candidate, but never sets a catalog slot.

## Latest review state, 2026-10-19

The current source plan now places all seven generated Meshy subjects: the original five props, three shared instances of the laptop, and one source-derived hookah body. The carpet covers almost the entire wall opposite the windows. Its center text/emblem pixels are preserved; the gameplay derivative adds ornamental side bands. Chair seat measurements are in `chair-anchors.json`. Source-room assembly passes with nine subject instances and no missing files; the latest room assembly still needs fresh actual renders before replacing the preview card.

Original generation geometry and 4K maps remain immutable. Mobile derivatives target roughly 240k static arena triangles before spectators; the 1.9M-triangle original furniture cannot be published directly. Naive collapse across exported UV splits caused physical gaps. Exact-position welding fixes those gaps, demonstrated by the beanbag actual review. Cabinet40k welded topology is watertight but the old UV atlas stretches; the first new-atlas rebake was rejected after actual renders. Its atlas was fragmented because GLB export splits vertices at UV seams. The rebake script now reconnects those exact-position seams before unwrap and awaits a second actual gate. Rejected derivatives must not be used in Unity or catalog.

`tools/run_blender_bounded.py` runs one owned headless helper with a 600-second/6-GiB bound, six CPU threads, checks for another engine before start, and records exit/time/peak owned RSS. Engine slot coordination with root remains mandatory.

`MeetingDetailsSetup.ReviewOnly` derives the dynamic hookah socket from the actual cap submesh and connects `MeetingHookahInteraction.outlet` to `MeetingSpectators.hookah`. This is review candidate preparation only; no catalogue or phone build has been updated. Parent owns seated-character posing/exclusion/smoke runtime.

Latest gameplay composition review opened both actual images at 239,151 repeated-instance triangles. Furniture/major prop forms are stable. Gate remains **not passed**: full-wall rug was coplanar with widened inner wall (both X=-6.46), resulting in a black rectangle; plan now moves the rug 4cm inward, requiring a new actual render. The laptop5k retained source atlas has stretching on the screen; it requires the same welded/new-atlas PBR rebake used successfully for cabinet-v2. Catalogue remains untouched while these two regressions are corrected.

## Widened table and current passing review candidate

`resize_gameplay_furniture.py` measures six disconnected physical components in the watertight80k furniture derivative: one table and five chairs. Only the table scales1.35 inX/1.15 inZ. Width increases1.2537→1.6925m. Chairs translate rigidly rather than stretching; measured new positions are in `chair-anchors-wide.json`. The actual table top is0.942523m, correcting the obsolete1.05m guess which left table props floating. Laptop bases and hookah foot now snap to the measured surface; the plant uses its measured saucer contact vertex level.

The latest carpet uses `inputs/rug-coherent-wide.png` as one continuous texture, preserved aspect2184/720. A2.9m-high/8.7967m-wide panel fits inside the3m wall with a narrow margin; no repeated side strips. `gameplay-lod-review/rug-wall.png` shows the complete central medallion, pink writing and emblem in one actual frame. The plane remains4cm inward from the wall to avoid coplanar black rendering.

Actual source/LOD room gates were opened, and Unity `ReviewOnly` passes `ArenaLayoutValidator` without changing its combat requirements. Unity captures now use `camera.scene` explicitly; the first blank capture is rejected diagnostic evidence, not an accepted render. The latest `unity/gameplay.png` and `unity/table-closeup.png` show the actual imported arena with two capture-only static seated characters and the dynamic hookah hose. Environment geometry is239,151triangles; diagnostic capture including two~100k static NPCs is439,638triangles. NPCs are added after prefab persistence strictly for capture, never saved as embedded child characters.

`PromoteValidated` is prepared but requires a separate `unity/visual-gate.json` acceptance receipt, an installed selected-only spectator pool, unchanged layout validation and a250k static arena cap. It backs up `GameContent.asset` and records the old path/GUID before slot0 replacement. Phone build and physical-device heat behavior remain unverified.

## Local integration completed

Root's `MeetingSpectatorInstall.RunInstallAndPromote` completed successfully after all actual visual gates. The current arena0 is now `Assets/LighFight/Art/Drafts/MeetingMeshyV1/arena.prefab`,239,151 static environment triangles. It contains the seven Meshy subjects (three laptop instances), widened table with rigidly repositioned chairs, measured-contact props, single coherent wall rug, hookah socket and selected-only spectator configuration. Three accepted static candidates9/8/3 are registered through six idle/mouthpiece addresses; candidate10 remains rejected and excluded. Background pool cap is2; active fighters are excluded by runtime selection, without loading the entire roster.

Evidence: `unity/visual-gate.json`, `unity/promotion.json`, `unity/GameContent-before-promotion.asset`, `unity/gameplay.png`, `unity/table-closeup.png`, and `gameplay-lod-review/rug-wall.png`. Embedded selected-content configuration was refreshed by the joint job. Original procedural arena0 is preserved at `Assets/LighFight/Content/arena-0.prefab` with GUID recorded in the promotion receipt. This is local asset/catalog integration only: no new phone build, physical iPhone heat test or user test has been performed.

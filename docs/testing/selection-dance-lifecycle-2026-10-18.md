# Selection dance lifecycle verification — 2026-10-18

Selection now plays the complete authored Dance clip once, samples its final frame, then returns to Idle on the next update through the existing0.28second bone blend. Repeated music-button press returns through the same blend. Idle is not evaluated over the dance every frame. The selection-only manual preview records the current motion; cached bone transforms are captured only when motion changes. Nonselection `PreviewNativeMotion` remains immediate, preserving existing victory/review callers.

Manual selection placement now synchronizes FighterView movement history without sampling Idle; otherwise a subsequent Pose could misread the new selection X coordinate as walking backwards. First native fixture run identified that defect and a fixture Animator lookup that assumed the component was on the root. Animator lookup now includes children; the behavior test still requires calm Idle rather than allowing WalkBack.

Coordinated Unity runs were executed by the presentation-review agent, not concurrently by this subtask:

- Initial focused PlayMode:0/2, identifying nested Animator and stale movement-history issues (`/tmp/lighfight-selection-dance-playback.xml`).
- Fixed focused PlayMode:2/2passed (`selection-dance-lifecycle-2026-10-18/green.xml`), actual Ranger4.3s and Rose16.26667s review prefabs with native Animator/provider curves.
- Narrow old-behavior mutation:0/2passed (`selection-dance-lifecycle-2026-10-18/old-behavior-red.xml`). The selection wrapper delegated to immediate `PreviewNativeMotion` while keeping placement synchronization. Tests failed because current motion remained Idle instead of Dance and entry blend duration was0instead of0.28. This isolates the former snapping/untracked behavior; tests are not satisfied merely by the new wrapper/API existing.

The presentation-review agent restored FighterView byte-exactly after the mutation; independently verified again against the Green SHA below. It owns the subsequent player/render gate. The known Green FighterView file SHA256 is `08dbd813f43ffcb2e2d65c2f1532ec66d1e462ffdfc18db002781925c620b22b`; the restored source matches its saved Green bytes. Player visual approval and shipping dance promotion remain separate from these lifecycle tests. No master asset was reduced or edited by the lifecycle change.

## Later compatibility audit / current combined status

The separate11production Dance audit found every actual carrier length equal to its authored overlay pose duration (23.166668s, loop1). There is no confirmed legacy short-carrier defect; no defensive runtime change was added. See `dance-duration-compatibility-2026-10-18/production-audit.json`.

The earlier note reporting a combined5/6 was incorrect; independent parent XML review confirmed separate5/5 and6/6 suites. See the compatibility audit for the original files.

## Current 19 October verification

Actual graphics-backed PlayMode4/4 passed: the two selection lifecycle cases on actual Ranger/Rose native review prefabs plus two full23.1667s dense owner cases. The final dance frame remains displayed before the next-update Idle transition; the0.28s entry/return blend and unchanged-state bone-cache retention are verified. This is not an independent rendered-pixel EOF proof.

The same source also passed actual EditMode/EnterPlayMode34/34 native lookup/contact/recovery/anatomical-joint/victory regressions after state-name hash caching. The hash cache avoids repeated full-state string concatenation on known motions. First-match clip lookup semantics and live table updates remain unchanged. Neither suite establishes whole-player zero allocation or iPhone thermal acceptance.

Current FighterView SHA256: `dc5bf551871784fe5f0c17e5c9cf497e96be83a4fd097d91f6a8f3030022634c`. Earlier SHA above belongs to the historical mutation snapshot. Full authored dances remain in review assets; no mass catalogue replacement is included.

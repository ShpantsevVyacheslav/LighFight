# Tripo playable review — 2026-10-13

The shipping roster is unchanged. A separate main-project review scene uses Rose with the native 100,000-triangle Tripo rig, original native 4K PBR maps, a calibrated Unity Humanoid avatar, target-specific closed fingers and 24 combat states. Combat clips currently come from the existing Meshy library retargeted by the Unity SDK; they are not Tripo-native attacks. Sweep is deliberately unavailable because its foot penetrates the floor. The UI/input buffer must not advertise or queue that missing state.

## Verified

- Unity EditMode: 151/151 pass (`.tools/tripo-integration-suite.xml`). Removing the availability gate makes both new regression tests fail; restored source retained.
- Main candidate builder regenerated prefab references with AssetDatabase. Valid human avatar; no shipping catalog replacement.
- macOS development build succeeds. Deterministic main-player smoke: 166 simulation samples; jab/cross/frontKick damage 6/10/14. Direct game-camera captures are used: batch framebuffer screenshots were uniform gray and rejected.
- Isolated SDK review: 1,265 finite combat poses, same-phase guard return after 0.20s = 0°; 24 accepted floor-contact states, sweep rejected. Native mesh/UV/weights/bind matrices preserved during Humanoid import.
- Original dense Rose: 1,865,580 triangles retained. Continuous spatial weights preserve original positions/UV/topology/maps/native binds. Four native box/kick samples remove >5× edge strain (baseline 1,090/828 such edges → zero in sampled contact frames). Fresh reconstruction exactly matches the reviewed weight matrix.
- Dense Unity import preserves triangle count and all 67 native bone local transforms. Unity splits seams into 957,435 vertices; raw source has 952,961. Generic/Humanoid mesh hashes match. Original three maps are byte-identical, 4096².

## Remaining gates

Full-density material appearance is not accepted yet: clothes look too reflective in Unity. Dense player timing on the M1 review harness is p50 41.1ms / p95 57.9ms and about 877MB peak memory; GPU counters are unreliable. This is not a phone performance verdict. All dense combat roles, full main gameplay/collision behavior, browser rendering/performance, source-quality portraits, other seven characters and device integration remain separate acceptance tasks.

Manual native UI validation was interrupted by computer-control state changes; do not infer a product button defect without matching input evidence. The candidate review scene and deterministic player smoke are local only. Web Build Support is downloading/installing through the official Unity CLI; no Web build success claim yet.

## Self-check

No paid credits spent, no Git push, no iPhone verification. Original sources retained. Rejected candidates are not promoted. Geometry preservation, visual deformation, material correctness and frame budget are independent gates.

## Responsive Web player verification

The WebGL module is installed and the local review build succeeds. The custom LighFightReview template fills the available canvas without the vendor desktop footer/fixed dimensions; the review build restores the previous template and compression settings. Bundled Noto Sans supplies Russian labels. Four portraits (Bone, Master, Rose, Ranger) were rendered from original Tripo sources; other roster replacements remain pending.

At an explicit 844×390 browser viewport, verified roster → Rose preview → opponent chooser → actual courtyard battle. Shadows are visible. Saved `art/previews/tripo-playable/web-landscape-battle.png`. This is browser evidence on the Mac, not an iPhone performance test. The battle screenshot contains round end after the bot attacked; it does not prove the manual swipe's damage. The previously recorded actual-player physical-contact smoke remains the damage evidence.

The 1,865,580-triangle dense candidate remains a separate quality experiment. Four-influence truncation is rejected: source geometry stayed exact but severe clothing ridges appeared. A 395-vertex sole-support proxy verified against 129,137 original shoe vertices across 2,694 animation samples reduces grounding cost about 26× (max sampled sole error 0.107mm). Actual dense player median 31.3ms / p95 41.4ms still fails reliable 30/60fps acceptance. Do not automatically promote or simplify this candidate. Full production roster, native motion provenance, material presentation and phone delivery remain unfinished.

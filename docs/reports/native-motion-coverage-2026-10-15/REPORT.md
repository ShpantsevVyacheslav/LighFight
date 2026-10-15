# Production native-support coverage — 2026-10-15

Read-only offline audit of current main prefab bindings, catalogs and actual FBX AnimationStack records. No Unity/Blender/player or main writes. Source paths, SHA-256, archive member metadata, packed pose paths and runtime clip GUID resolution are in `production-coverage.json`. Both inline GameContent and selected-two EmbeddedGameContent address keys resolve all11 audited prefab GUIDs in exact slot order (production-content-bindings.json). All catalog→prefab state/source/pose GUID triples agree. All11 imported primary FBX files are byte-identical to the respective retained native source FBX. All bound poses have existing packed payloads.

| Slot / fighter | Source takes | Catalog definitions | Bound FK states | Distinct bound takes | Missing Generic take bindings |
|---|---:|---:|---:|---:|---|
| 0 Bone | 8 | 5 | 5 | 5 | box_02, hit_to_head, front_kick_01 |
| 1 Witch | 8 | 8 | 8 | 8 | None |
| 2 Master | 9 | 4 | 4 | 4 | box_02, dance_05, hit_to_head, dance_02, front_kick_01 |
| 3 Island | 7 | 4 | 4 | 4 | box_01, box_02, front_kick_01 |
| 4 Rose | 8 | 5 | 5 | 5 | front_kick_01, box_02, hit_to_head |
| 5 Ranger | 8 | 8 | 7 | 6 | box_02, hit_to_head |
| 6 Gala | 8 | 8 | 8 | 8 | None |
| 7 Cobalt | 8 | 8 | 8 | 8 | None |
| 8 Retro | 8 | 8 | 8 | 8 | None |
| 9 Sailor | 8 | 8 | 8 | 8 | None |
| 10 Summer | 8 | 8 | 8 | 8 | None |

Total88 archived source takes;73 bound state entries representing72 distinct per-character original takes. The six full8 characters each bind five ordinary support roles plus three separate diagnostic states (NativeBox02/NativeFrontKick/NativeHeadFull); this does not mean eight gameplay support categories or native combat strikes.

## Exact ordinary source role mapping

Bone/Rose: Walk=walk, Dance=dance_01, Greeting=greet_01, Defeat=fall, Victory=cheer. Island: Walk=walk, Dance=dance_01, Greeting=greet_01, Defeat=defeat_02; no cheer/head-hit exists in its7-take pack. Master: Victory=cheer, Dance=dance_01, Greeting=greet_01, Defeat=fall; its9-take pack has no Walk. Witch/Gala/Cobalt/Retro/Sailor/Summer: Victory=cheer, Greeting=greet_01, Dance=dance_01, Defeat=defeat_02, Walk=walk, plus separate raw source box_02/front_kick_01/hit_to_head diagnostic states.

Ranger has Walk/Dance/Greeting/Defeat/Victory plus Stand and Idle aliases of its own named five-second boxing-idle take. Eight catalog definitions include the same source as Guard too, but NativeSupportIntegrationBuilder.Attach defaults includeNativeGuard=false, so actual prefab has7. Active Guard remains shared Human. Original idle lowers/opens hands and was not promoted to permanent Guard. Stand/Idle are two states, one source take; Stand is not a GetUp motion.

## Actual runtime fallbacks and KO contract

All11: active Guard, WalkBack, body Hit, battle Fall and GetUp remain shared Meshy Human SDK clips (exact asset paths in JSON). Idle is shared Guard for10; Ranger Idle is its own original FK idle. Forward Walk is native FK for10; Master uses shared Walk_Fight_Forward because its provider pack has no walk. HasNativeMotion/UsesNativeMotion are runtime dispatch flags, not proof of provider provenance. Accepted22 combat move IDs remain shared Human clips through target calibrated avatars; diagnostic NativeFrontKick is not the gameplay frontKick state.

NativeHighReaction supplies post-Humanoid head clips for all11 while retaining shared waist/body Hit. Ten use their own source head take; Island uses Ranger-hit_to_head via SDK retarget because Island has no head take. This is not Island-native FK. NativeHeadFull is a separate exact-Generic diagnostic state for the six full8 characters. Do not alias it globally to Hit: the current high-versus-waist/blocked selection contract must remain conditional.

VictoryPresentation selects Victory→Greeting→Dance. Island therefore uses its own Greeting. Native Defeat is used only for the non-KO losing fighter (Health>0); health0 KO remains battle Fall. DownTicks/GetUp timings remain shared. Never alias cinematic Defeat/fall into battle Fall/GetUp merely to increase native coverage: it changes phase/KO semantics and requires independent proof. NativeSupportOverlay uses body grounding for Defeat/its returns and shared Fall/GetUp/recovery blending.

## Why takes are omitted / next narrow work

Legacy Bone/Rose/Island/Ranger packets intentionally wired support roles; omitted boxing/kick/head Generic states were not part of those accepted packets. Their omission is not a proved rejection of the entire authored clip. High-hit takes from Bone/Rose/Ranger/Master are already used through the separate SDK head-reaction path. Box-loop reuse as permanent Guard was rejected; no punch timing/contact quality is inferred from a raw provider box loop. Master selected dance_01 for the sole Dance role; dance_02/dance_05 are local unused variants, not proved visually bad. Missing native Walk (Master), Victory/head-hit (Island), backwards walk/GetUp (all) cannot be invented from absent takes.

`missing-take-review-plan.json` lists the16 exact missing Generic bindings and source SHA gates. Next narrow implementation is separate review-only exports/catalogs, not shipping count padding: Bone/Rose three states each; Island box01/box02/frontKick; Ranger box02/head; Master box02/frontKick/head plus two dances. Existing lossless Generic exporter/codec can be reused after the phone window. New floor-contact poses may require body support rather than only the old shoe set, particularly dance variants; freeze support extrema only after full-original-vertex holdouts and entry/return tests. Preserve original source keys, bones, weights, images, material references, Guard and KO/recovery contracts. Do not append unused diagnostic poses to selected-two shipping warmup simply to claim8: that adds resident curve memory without gameplay value.

No new importable motion assets are honestly available from this offline pass: source data exists, but export, source parity, transition/floor and actual EOF gates have not run. `Editor/ProductionNativeCoverageInspector.cs` is an optional metadata-only inspector for later import; it never changes assets, evaluates poses or decodes curves. It is prepared source, not Unity-compiled in this window.

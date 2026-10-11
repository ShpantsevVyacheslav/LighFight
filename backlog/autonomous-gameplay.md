# Autonomous LighFight work — 2026-10-11

Budget: 1200 Meshy credits total, 392 spent, 808 available. No push without fresh authorization.

| Task | Implementation | Device |
|---|---|---|
| Exclusive rules/arena sheets and modal input | Verified EditMode transition regression; modal background disabled | Pending |
| Audit each original source + native web weights; rendering comparison | Pending | Pending |
| Greeting, face portraits, dance icon, swipe preview, opponent selection | Dance icon, swipe preview, opponent sheet implemented; greeting and portraits under quality audit | Pending |
| Persisted graphics/audio/input/layout/pause settings | Implemented; 119 EditMode tests pass; device pending | Pending |
| Two-thumb controllers, crouch, no block button, minimal HUD | Pending | Pending |
| Contact, combos, sidestep, prevent body crossings | Pending | Pending |
| Paired throws, range, interruption, fall/get-up | Pending | Pending |
| Facial expressions and winner camera/animation | Pending | Pending |
| Courtyard composition from references | Pending | Pending |
| New high-quality roster from references | Pending | Pending |
| Unity verification, performance, iOS delivery | Pending | Pending |

Keep original source geometry and texture resolutions explicit. Island original FBX audit and native web GLB both have 66364 triangles; 350k refers to Rose. Neither source has face morph targets. Dance remains a compact icon, not a tab. Actions belong in settings; exit returns to Game Hub.

Exact visible web source acquired: task 01a12cb8-8e0d-75b1-973b-6f75e4c7cbcf, 103074 triangles / 63240 vertices / no skin. Previous original and native rig both 66364 triangles. Thus source versions differ; full stage-of-loss verdict still pending controlled comparisons.

# Tripo roster delivery

| Fighter | Main catalog | Browser | iPhone |
|---|---|---|---|
| Bone Lady | New calibrated Tripo,23 accepted combat roles | Preview checked | Pending |
| Night Witch | Previous model | Previous model | Pending replacement |
| Element Master | New calibrated Tripo,22 accepted combat roles and native support motions | Desktop source/motion and victory checked | Pending |
| Island Fighter | New calibrated Tripo,22roles,8K albedo | Preview and battle vs Ranger checked | Pending |
| Pink Panther | Calibrated Tripo,24roles | Preview and battle checked | Pending |
| Ranger | New calibrated Tripo,22roles | Battle checked | Pending |
| Gala Fighter | Previous model | Previous model | Pending replacement |
| Cobalt Champion | Previous model | Previous model | Pending replacement |

Current blocker before device artifact delivery: near-distance forearm/foot exclusion and punch timing. The automatic Humanoid root displacement is fixed and tested; actual main Mac still shows arm overlap/missed punches at0.65m. Do not inflate hit radii or apply damage to misses. Linear HDR presentation is integrated and full GPU Unity158/158 passed; phone performance remains unverified. Source/native support transitions, facial animation and final art remain open.

No provider credits spent during this integration. Local commits only, no push.


## Additional three characters requested2026-10-13

Planned roster is now11. These labels are working outfit descriptions, not asserted real identities. Original user photos preserved byte-for-byte under `art/references/additional-roster/`, provenance in `manifest.json`.

- [ ] Retro denim: pink bandana/sports glasses, denim vest/shorts, white printed T-shirt, pink wristbands, trainers. Reference `retro-denim.jpg`.
- [ ] Striped sailor: blue/white long-sleeve striped shirt, dark trousers/glasses, black trainers. Reference `striped-sailor.jpg`.
- [ ] Summer beard: cap/glasses/beard, white T-shirt, blue plaid longshorts, white socks/dark sandals. Reference `summer-beard.jpg`.
- [ ] Generate originals, native rigs/motions, source portraits, exact-source comparison, full-animation deformation/contact gates for all3.
- [x] Replace fixed8-character names/indexing/layout with catalog metadata and paged/scrollable roster/opponent selection; preserve existing8 IDs. Host catalog/export must also handle11.
- [ ] Include all11 reviewed characters in final phone build. No generation/download/skeleton or device success is claimed yet for these3.

All three neutral full-body references are prepared with built-in imagegen, preserving original photos. These are generation inputs, not generated 3D fighters. Expanded catalog code passed the full GPU Unity suite: 173/173.

## Verified progress2026-10-14

All three additional A-pose dense originals are generated and retained locally. Summer beard has a100k native Unity-compatible rig and an isolated eight-motion candidate; Striped sailor has a99,999-triangle native rig with motion export in progress; selected Retro denim A-pose has a100k native rig, export pending. None of these three is yet promoted into the shipping catalog.

Original-density Master reconstruction keeps1,992,276triangles and originalUV/maps. Main-compatible isolated player passes248 combat/native entry, held-pose and return cases. On Mac, one original-density actor costs67.71ms median versus6.59ms for the accepted100k actor in the same HumanGuard benchmark. This is a desktop capacity result, not iPhone performance. Keep dense original and review/cached portrait mode separate from shipping combat until target-device validation.

Selected-only production content loading is implemented and tested:199EditMode tests plus8focusedPlayMode tests. Actual standalone player completed20selection swaps,60loads/60releases and three live unique selected assets per settled selection. Remaining native exports, six roster replacements/additions, paired throws, facial animation, close-limb separation, arena art acceptance and iPhone artifact/device proof remain open.

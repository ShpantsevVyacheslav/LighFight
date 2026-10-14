# Tripo roster delivery

| Fighter | Main catalog | Browser | iPhone |
|---|---|---|---|
| Bone Lady | New calibrated Tripo,23 accepted combat roles | Preview checked | Pending |
| Night Witch | New calibrated Tripo,22 shared combat roles and8 own native support motions | Main native player245frames passed | Pending device proof |
| Element Master | New calibrated Tripo,22 accepted combat roles and native support motions | Desktop source/motion and victory checked | Pending |
| Island Fighter | New calibrated Tripo,22roles,8K albedo | Preview and battle vs Ranger checked | Pending |
| Pink Panther | Calibrated Tripo,24roles | Preview and battle checked | Pending |
| Ranger | New calibrated Tripo,22roles | Battle checked | Pending |
| Gala Fighter | Previous model | Previous model | Pending replacement |
| Cobalt Champion | Previous model | Previous model | Pending replacement |
| Retro Denim | Reviewed Tripo appended at8,22 shared roles/eight own native support motions | Main native transitions checked | Pending device proof |
| Striped Sailor | Reviewed Tripo appended at9,22 shared roles/eight own native support motions | Main native transitions checked | Pending device proof |
| Summer Fighter | Reviewed Tripo appended at10,22 shared roles/eight own native support motions | Main native transitions checked | Pending device proof |

Current blocker before device artifact delivery: near-distance forearm/foot exclusion and punch timing. The automatic Humanoid root displacement is fixed and tested; actual main Mac still shows arm overlap/missed punches at0.65m. Do not inflate hit radii or apply damage to misses. Linear HDR presentation is integrated and full GPU Unity158/158 passed; phone performance remains unverified. Source/native support transitions, facial animation and final art remain open.

No provider credits spent during this integration. Local commits only, no push.


## Additional three characters requested2026-10-13

Planned roster is now11. These labels are working outfit descriptions, not asserted real identities. Original user photos preserved byte-for-byte under `art/references/additional-roster/`, provenance in `manifest.json`.

- [x] Retro denim: pink bandana/sports glasses, denim vest/shorts, white printed T-shirt, pink wristbands, trainers. Reference `retro-denim.jpg`.
- [x] Striped sailor: blue/white long-sleeve striped shirt, dark trousers/glasses, black trainers. Reference `striped-sailor.jpg`.
- [x] Summer beard: cap/glasses/beard, white T-shirt, blue plaid longshorts, white socks/dark sandals. Reference `summer-beard.jpg`.
- [x] Generate originals, native rigs/motions, source portraits, exact-source comparison, full-animation deformation/contact gates for all3.
- [x] Replace fixed8-character names/indexing/layout with catalog metadata and paged/scrollable roster/opponent selection; preserve existing8 IDs. Host catalog/export must also handle11.
- [ ] Include all11 reviewed characters in final phone build. No generation/download/skeleton or device success is claimed yet for these3.

All three neutral full-body references are prepared with built-in imagegen, preserving original photos. These are generation inputs, not generated 3D fighters. Expanded catalog code passed the full GPU Unity suite: 173/173.

## Verified progress2026-10-14

All three additional A-pose dense originals and native eight-motion exports are retained locally. Summer, Sailor and Retro full native catalogs/calibrated candidates passed source and real Update/contact/render checks, were imported into main Unity, and are now appended at8/9/10. Main native transition player735frames passed; this does not establish iPhone capacity or native combat bindings. The main full suite remains200/200 after the eleven-slot catalog and Witch replacement.

Striped sailor main source preparation and candidate build passed with22 shared strikes. A shared cached-pose bug that reanchored hipXZ after collision sampling is fixed; the focused warmed PlayMode regression passes in main (`/tmp/lighfight-shared-cached-main2.xml`). The isolated before/after mutation proof and1,890 actual-render cases are retained in the Sailor packet. Close jab/cross misses remain under separate investigation; the hip fix alone does not prove their resolution.

Night Witch original1,937,213-triangle FBX/4K maps retained before retopology; reviewed99,999-triangle Unity-compatible native8 rig has replaced slot1 with a source-rendered1024portrait. Main native transition player245frames passed. Gala original1,933,246triangles and Cobalt original1,966,715 retained before changes. Both working100k-class Unity rigs are complete. Gala native8 FBX/4K export is retained; Blender inventory confirms100,000 triangles/67bones/8takes/zero unweighted vertices, with Unity source-parity/deformation checks pending. Cobalt native8 preparation is in progress. Verified balance2110; no subscription upgrade was made.

Embedded selective Addressables delivery is now an implementation task: current remote-only bootstrap cannot start a merely copied local Data catalog. Design/plan: `docs/superpowers/specs/2026-10-14-embedded-content-design.md`, `docs/superpowers/plans/2026-10-14-embedded-content.md`. No host framework replacement or phone installation yet.

Original-density Master reconstruction keeps1,992,276triangles and originalUV/maps. Main-compatible isolated player passes248 combat/native entry, held-pose and return cases. On Mac, one original-density actor costs67.71ms median versus6.59ms for the accepted100k actor in the same HumanGuard benchmark. This is a desktop capacity result, not iPhone performance. Keep dense original and review/cached portrait mode separate from shipping combat until target-device validation.

Selected-only production content loading is implemented and tested:199EditMode tests plus8focusedPlayMode tests. Actual standalone player completed20selection swaps,60loads/60releases and three live unique selected assets per settled selection. Remaining native exports, six roster replacements/additions, paired throws, facial animation, close-limb separation, arena art acceptance and iPhone artifact/device proof remain open.

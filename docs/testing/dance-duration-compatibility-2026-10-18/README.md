# Production Dance duration compatibility — 2026-10-18

Read-only audit of all11fighter references from `Assets/LighFight/Content/GameContent.asset`. Resolved their actual GUIDs to prefab files, each `FighterView.nativeClips` Dance reference to its `.carrier.anim`, and each `NativeSupportOverlay.entries` Dance pose to its `.pose.asset`.

**No current production mismatch found.** Bone, Witch, Master, Island, Rose, Ranger, Gala, Cobalt, Retro, Sailor and Summer each have carrier stop minus start =23.166668seconds and authored pose duration =23.166668seconds. Overlay Dance loop=1 in all11. Exact paths and values are in `production-audit.json`. This is serialized source inspection, not a Unity runtime/API measurement.

`PreviewDuration` remains carrier-based; `PreviewNativeMotion` uses the carrier duration and loop flag; selection uses that duration but forces a single playback. The new review-only authored dances are a separate asset set and were not substituted for production in this audit. A truly shorter carrier would require an explicit authored-duration authority to keep preview timing and normalized phase consistent; the audited11do not currently exhibit that condition.

No runtime edit, asset edit, Unity launch or false claim of an existing short-clock bug was made for this audit. Parent agent has the evidence and decides whether an additional defensive compatibility feature is in scope.

## Current test/review state

Parent decision after the11/11match: no additional runtime guard, cache, hypothetical short-carrier test or heavy job. No legacy duration defect is established. Earlier selection lifecycle evidence is archived separately under `docs/testing/selection-dance-lifecycle-2026-10-18/` (its2/2Green and old-behavior0/2RED pertain to that earlier fixture snapshot).

The animation owner reported two successive focused suites: **5/5**, then **6/6** after explicit grounding ownership was added. These are not a 5-of-6 result. Durable XML and restoration hashes are in `art/source/unique-dances-20261018/audit/lifecycle/` (`earlier-green-5-of-5.xml`, `green-6-of-6.xml`, `mutation-red-2-of-2.xml`, `manifest.json`). The two-case deliberate mutation was reported **2/2 failing**, followed by byte-exact restoration of the new driver. This compatibility subtask did not independently inspect those XML files or launch Unity. Stable fixture references and actual-player visual acceptance remain separate requirements; the archived earlier2/2 must not be substituted for the newer suite.

Parent verification subsequently parsed all three durable XML files independently: Passed5/5, Passed6/6, deliberate Failed0/2. No unexpected sixth-test failure was found in these suites.

# Authored dance inventory — 11 unique dances are not yet available

Read-only inventory parsed actual FBX animation stacks/curves, plus local dance GLB metadata. Source paths, SHA256, durations and raw timing/curve hashes: `dance-inventory.json`. Reproduce with `python3 tools/12_dance_inventory.py` from this directory via absolute scriptpath (requires installed Blender FBX parser modules; no engine launch).

| Available source take | Actors containing it | Duration | Uniqueness status |
|---|---|---:|---|
| `dance_01` | Island, Ranger, Bone, Rose, Master, Witch, Retro, Sailor, Summer, Gala, Cobalt |23.1667s|One shared-template candidate; do not count11 retargets as11 choreographies. Raw hashes differ, which does not establish choreography uniqueness.|
| `dance_02` |Master native pack|12.7917s|Additional candidate; must view full motion and retarget before acceptance.|
| `dance_05` |Master native pack|2.875s|Additional candidate; must view loop boundaries and actual motion before acceptance.|
| `Hip_Hop_Dance_1.glb` / `retarget_clip` |Original Island Meshy local source|3.3333s|Additional candidate from a different skeleton/provider; not yet proven independent from the Tripo takes or retargeted to current11.|

There are **at most four candidate choreography sources**, not eleven proven unique dances. No character is silently assigned a renamed duplicate, slowed duplicate, mirrored duplicate, or a different segment of the same dance as if it were a new authored dance.

## Proposed assignment (not implemented or accepted)

- Master: own native `dance_05`, if loop/visual gate passes.
- Island: existing own-source Hip Hop1 after rest-aware retarget/contact gate.
- Bone: Master `dance_02` transferred by an explicit rest-aware retarget; source provenance must remain Master/shared, not mislabeled Bone-original.
- Sailor: own `dance_01` only after the current shoulder/sleeve correction is accepted.
- Ranger, Rose, Retro, Summer, Witch, Gala, Cobalt: **unassigned** until seven additional genuinely distinct licensed or locally authored choreographies are available and validated. These are real missing inputs/work, not fulfilled by giving existing `dance_01` seven names.

Theme ideas for future local authoring: Ranger stomp groove, Rose step-touch, Retro twist, Summer heel-toe shuffle, Witch ritual sway, Gala solo ballroom, Cobalt robot/popping. These are design ideas only—no such motion files are claimed to exist. Each must have distinct leg/root/arm rhythm, planted-foot and loop gates, original mesh/UV/weights intact, and source-specific sleeve/shoulder visual checks. Do not mass-rerig other actors until Sailor pilot is accepted.

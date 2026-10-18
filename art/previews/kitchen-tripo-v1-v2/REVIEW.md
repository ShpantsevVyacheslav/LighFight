# Tripo kitchen v2 local review

**Verdict: rejected as a kitchen room/arena.** The local original GLB contains a table and four chairs only. Missing room geometry is real, not a web viewer visibility or clipping issue. No promotion, further generation, geometry editing, compression, or Unity launch was performed.

Source `art/source/kitchen-tripo-v1/original-v2.glb`; Tripo task `bcc8d1cd-5796-4f40-82e5-e86b8a206812`. SHA256 `fce37d2145ef34dac5a39f7ddffcecb46c1d384162019b1948e3ea864640b2ff`. Provenance `generation-v2.json` identifies the uploaded image as a derived cutaway image based on `assets/references/photo-01.jpg`, not the original photograph. The prepared cutaway includes the fridge/window/room; those do not appear in the downloaded geometry. This review does not establish why generation omitted them.

One mesh:1,076,312vertices and1,988,865triangles. Three embedded4096×4096 basecolor/normal/roughness-metallic textures. Blender imported bounds: width0.48851, depth0.98236, height0.31987 in source units (not metres). These are furniture-set proportions, not a room. Source topology retained entirely.

Viewed local headless front, three-quarter and top clay renders. They show the tabletop, legs and four chairs. There are no walls, floor slab, windows, refrigerator or kitchen cabinets. Downward geometry probes at9 central source x/z positions intersect mostly only elevated surfaces around source Y0.17–0.289; one hits a near-ground furniture foot around0.004. There is no continuous floor surface. `floor-probe.json` stores exact intersections.

Evidence: `audit.json`, `floor-probe.json`, `front.png`, `three-quarter.png`, `top.png`, reproducible `review.py`/`floor_probe.py`, and successful `blender.log` with `AGENT_RESULT`. Blender4.2.22LTS factory startup, no GUI. Initial higher-quality Cycles and Workbench attempts were stopped serially after slow/stalled headless rendering; final minimal CPU Cycles2samples/640×480 completed. Images are deliberately low-cost and noisy but their table-only content is unambiguous. No two review-owned Blender processes ran concurrently.

The wrong-provider Meshy builder was moved out of Unity into `art/source/office-kitchen-meshy-v1/rejected-tools/`, including its .meta; it cannot accidentally compile/run from Assets. This Tripo review does not revive that rejected asset or builder.

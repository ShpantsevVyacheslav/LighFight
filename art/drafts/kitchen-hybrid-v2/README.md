# Hybridv2 — actual Tripo fridge/microwave replacement

Versioned from preserved hybridv1. Replaced only5named fridge/freezer-seam/microwave/cupboard blockout objects with untouched `fridge-microwave-original-v4.glb`, SHA `8b8d3029b3d66cedadf3fb0ac4dc1b9eebac2ca3ccd3b3ee445f22caaf039fdf`. Actual GLB and imported mesh both1026269vertices/1974944triangles (provider metadata says1026270; no vertex was removed).

Position/index/UV hashes and embedded-map byte hashes are in manifest.json. Source geometry/materials were not edited; one uniform parent scale2.02499 and translation[.35,2.4,.002] place the complete fridge+microwave+plinth group. Existing1076312vertex/1988865triangle table/chair mesh and all non-camera scene transforms remain exact. Source GLB and basev1blend hashes were unchanged after render/save.

Actual interior/top PNGs viewed: both appliances are present, face into the room, remain against the back wall and avoid the clear play rectangle. Architecture/single right window unchanged. Coffee blocks are explicitly unfinished pendingv5 replacement. Floor remains procedural lookdev. No Unity/prototype/mobile acceptance.

Reproduce with `tools/replace_fridge.py` using Blender background/factory-startup/python-exit-code1/threads6. Requires the local reproduciblev1blend; buildv1first if missing. Saved `kitchen-hybrid-v2.blend` is a355MiB **local artifact, not ordinaryGit input**. Keep raw masters andv1; no compression/decimation/reduction performed.

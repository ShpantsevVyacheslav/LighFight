# Exploratory hybrid kitchenv1

**Tripo furniture + manually reconstructed photo-based architecture.** This is not a fully generated Tripo room and is not promoted to Unity/the prototype.

`interior-threequarter.png` and `top.png` are actual Cycles CPU renders, viewed after generation. The pilot has a straight rectangular floor, two beige walls, a plain back wall, and exactly one right-wall window with19vertical blinds. It removes the layout defects by constructing new architecture, not by claiming the rejectedv3 mesh was repaired or preserved in this assembly. The rejectedv3 source remains untouched separately.

The table-and-four-chair component is imported intact from `art/source/kitchen-tripo-v1/original-v2.glb` (SHA `fce37d2145ef34dac5a39f7ddffcecb46c1d384162019b1948e3ea864640b2ff`). All1076312vertices/1988865triangles, local positions, UVs, indices, original materials and embedded images remain unchanged. Only one parent uniform scale3.2 and translation[1.5,0,.002] place the normalized source in the room; no geometry scale was applied, remesh, decimation, triangle reduction, or texture recompression occurred. `manifest.json` records before/after geometry hashes and source-image byte hashes.

The photo has no measured dimensions. The5.4×5.6×3room is explicitly an expanded game-space blockout, not a metric reconstruction of the narrow photographed room. The open rectangle X[−1.9,.6], Y[−2.5,1.1] is2.5wide×3.6deep; its width exceeds the unchanged furniture's1.563wide footprint. Furniture remains next to the right-wall window. Fridge/microwave/cupboard and coffee cabinet/machine are simple named **unfinished blockouts**, placed by photographed relationships. No fictionalU-shaped kitchen was added.

The clean floor itself is a closed12-triangle box with topZ0. Independent full-component BVH101×101rays across almost the entire rectangle passed10201/10201, min/maxZ0. This proves the constructed floor sample coverage, not navigation/character collision acceptance. Furniture minimum isZ.002 (2mmplacement clearance).

The oak-like floor material is procedural lookdev; the wall/trim materials are simple PBR colors. No suitable existing authored floor texture was found. They are not claimed as reconstructed photo textures or finished assets. The original furniture retains its actual source PBR maps. Window exterior/detail, appliances, scale calibration, runtime performance and Unity/phone acceptance remain open.

## Reproduce

```
/Users/macbook1/Applications/Blender.app/Contents/MacOS/Blender --background --factory-startup --threads 6 --python-exit-code 1 --python art/drafts/kitchen-hybrid-v1/tools/build_hybrid.py
```

One headless process, fixed6CPUthreads, factory startup, error exit enabled. Result: `kitchen-hybrid-v1.blend`, source-preservation manifest, and two PNGs. The179MiB blend is a **local reproducible artifact; do not add it to ordinary Git**. Keep scripts/JSON/PNGs/provenance for review. No original asset or main/Unity project was changed.

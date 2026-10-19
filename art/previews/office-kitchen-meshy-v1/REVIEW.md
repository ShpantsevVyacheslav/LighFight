# Office kitchen Meshy preview review

Verdict: **hold texture/refine purchase for a complete room arena**. The generated kitchen is recognizable and could be useful as an unchanged backdrop, but it is not a complete combat room.

Source: `art/source/office-kitchen-meshy-v1/preview.glb`, SHA256 `c2ac8c2d2c18cebd7dcd3f7637d1ecfb1452d0cf72411e05bb5024e06ce1b706`. No source edits, remesh, decimation or compression. Blender4.2.22 LTS headless, factory startup; one process at a time; no Unity launched.

Geometry: one mesh,223,437vertices,447,378triangles, one untextured material, no source images. Imported bounds are1.90236wide ×1.25414deep ×1.02373high in normalized source units. Not authored metres.

Front/three-quarter/top clay renders show back and side cabinets, sink, microwave, cups/kettle and refrigerator. Kitchen category reads correctly, with useful depth/detail. The near right wall/fridge occludes a shallow three-quarter battle camera. Several handles and cabinet surfaces are softly fused AI geometry; textures may improve category/readability but cannot repair missing floor.

The central area has no floor geometry. `floor_probe.py` reads the original GLB triangles without altering them and tests downward projection at9 central points (x=-.3/0/.3,z=-.25/0/.25 in original glTF axes); all have zero surface intersections (`floor-probe.json`). Top render independently shows the open background through the centre.

Gameplay footprint currently spans X±5 and Z±1.5 (`Core/Combat.cs`). Uniformly scaling the source to a plausible3m wall height gives an overall width about5.57m and depth3.68m; the usable U-interior is smaller. It cannot contain the existing10m-wide combat range as a normal-scale room. Enlarging it to contain combat would make counters/walls oversized. It could instead sit behind a separate foreground fight stage/floor, retaining every source triangle, if that composition is accepted.

Recommendation to parent: do not spend the additional10credits under the assumption this is a finished arena. Choose explicitly between an unchanged background set plus a separate combat floor and a new room generation with a wide unobstructed floor/shallower side walls. No new credits spent by this review. Unity integration/phone performance/textures unverified.

Evidence: front.png, three-quarter.png, top.png; audit.json; floor-probe.json; reproducible review.py/floor_probe.py; blender.log. Final clay lighting was reduced after the initial overexposed inspection render; final images are the lower-exposure versions.

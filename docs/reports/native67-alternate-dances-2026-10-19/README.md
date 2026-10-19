# Other dance comparison — 19 October 2026

55 sampled poses across 11 existing dance clips, at 10/30/50/70/90 percent. Same original dense Sailor, native67 skeleton, all-positive Float32 barycentric weights and raw 4K maps. No clavicle correction. JPEGs are layouts of actual Unity PNG captures, resized only.

Clips: Boom, Bass Beats, All Night, Bubble, Arm Circle Shuffle, Gangnam Groove, Magic Genie, Indoor Swing, Cardio, Denim Pop, Shake It Off. These are Generic Meshy sources retargeted onto the Tripo model, not native Tripo exports. True-bind / canonical reference calibration and arm-direction agreement were checked. This does not certify anatomical correctness, twist, fingers or continuous motion.

Both Unity batches exited 0 and collected all scheduled poses. Numeric surface gates passed, but framebuffer repeat difference status remains RED; do not promote these as fully accepted or as mobile performance evidence. Rendering uses synchronous GPU readback for editor inspection.

Visual assessment: shoulder silhouette and shirt gathering depend on pose. Raised/hunched and elbows-at-waist positions expose stronger deformation in multiple clips, not only the original dance. Bubble and Shake It Off have calmer sampled positions; they are not proven fixes over the whole continuous clip. The broader sample does not justify attributing everything solely to bad motion or solely to skeleton placement. Next diagnostic comparison should hold the troublesome arm pose constant and compare source/target clavicle rotation and skin weights.

Overview: overview.jpg. Three dances-N-front-shoulders sheets cover all 55 poses; front-full and quarter-shoulders sheets provide additional views. Individual dance-N.jpg pairs detail and full body. Manifest records original PNG paths and SHA256 values. Production catalog and rigs unchanged.

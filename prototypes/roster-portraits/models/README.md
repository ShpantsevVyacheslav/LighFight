Native animated prototype models

retro.glb / sailor.glb / summer.glb use each actor's untouched native full-pack FBX. No decimation, subdivision, bone relocation, weight transfer, retargeting, Unity edits or sourcefile mutation. Blender4.2.22 imports native animations and exports sampled glTF skin animation at the source24FPS. GLB vertex count may increase at UV/normal seams; triangle counts remain100000/99999/100000,67skin joints,max4influences.

Default: PreviewGuard (derived constant clip,1second,two identicalkeys from originalbox_02 at0seconds). It is an explicitly held authored combat-start pose, not authoredidle motion. Allthree start in a forward-leaning bent-knee stance with open hands; they are notA/T, but not closed-fist guard. Do not autoplay fullbox_02 for idle. Fullnative clips remain unchanged and can play ongesture, then crossfade to distinctPreviewGuard:

- box_02 —2.791667s, native repeated boxing/punch take.
- front_kick_01 —2.5s.
- dance_01 —23.166667s.
- greet_01 —3.5s.
- cheer —12.083333s.
- defeat_02 —8.458333s.
- hit_to_head —1.833333s.
- walk —2.333333s.

Base-color4K and native normal JPEG payloads are embedded byte-for-byte. Metallic/roughness use a derived lossless glTF PNG (G=source roughnessR,B=source metallicR,R=255); raw source scalar values retained, no guessed gamma/Yflip/recolor, no swapping dense-originalatlas onto retopologyUV. This retains the source FBX material interpretation, not a claim of calibrated shippingUnityPBR parity. Provider native normal maps are1K; this export does not invent missing4K detail.

Per-actor manifest records sourceFBX/fileSHA, original inputtextureSHA, triangle/bone counts, full8clip durations, GLB SHA/size/embedded-image SHA, derivedclip provenance, normalized skinweights and actual Blender glTF reimport-vs-FBX posedvertex checks. Sourcegeometry/UV/weights/native rest matrices hash before/after export matches. Roundtrip checks samplebox0,box45%,dance35%; this is not every-frame or shippingphone validation.

Actual native-source PBR renders reviewed: *-review-box_02-0.png (heldpose), *-review-box_02-0.45.png and *-review-greet_01-0.png. Render camera/front lighting are QA only and not inGLB. glTF isY-up with frontal view from+Z; viewer should frame wholeactor rather than crop the model.

Reproduction scripts/logs: .tools/portrait-native-glb/export_native.py, finalize_glb.py, roundtrip.py. Runone Blender helper atatime with --factory-startup --python-exit-code1. Export all8actions after binding each once to repair BlenderFBX's unusedAction id_root0 metadata; no curve edits. Exporter image placeholders were caught/replaced using originalimagebytes, not accepted. DerivedPreviewGuard is appended inGLB without touching authored8clips. Roundtrip firstfixture accidentallyincluded importer-generated bone-displaymesh; corrected to actual armature-deformed meshes, retainedREDlog.

# Source Tspace Unity review — numerical transport passed, material visually rejected

Unity6000.3.26f1 Metal Editor job exited0; heavy window released immediately. LocalFBX SHA631c5b585e1206a03a7638e14c2603741eb31bc7d6375bb434f7bfc33b7d9afe; not ordinaryGit-staged or catalog-promoted.

## Source and engine transport

Independent raw-file gate: all994452 source vertices,1988914 triangles,5966742 cornerUVs and3088451 named nonzero weight values match originalNPZ exactly, no missing influences. Original4K map bytes/factors unchanged.

Unity explicitly imports original normals and tangents (Import/Import), never derivedCalculateMikk on this candidate. Import creates1305574 seam vertices; this does not change original vertex/triangle density. Typed source-weight restoration keeps all nonzero influences; imported pruning and engine Float32 readback remain documented separately. Nonweight clone buffers are unchanged.

Actual GenericAnimator BOX fivefrozen source witnesses: surface max2.746µm, bonejoint max1.915µm, boneangle max.000555°, fullimported-vertex weight-only readback surface max.362µm. Independent fullclip source1215holdout gate belongs to the sourceFBX owner; this Editor run does not add continuous/player proof.

Original Blender256 facet-qualified tangent witnesses: all triangleposition/UV contexts verified, no movingfit or globalgreenflip. All256 handedness agree. Most normals/tangents match at4–6e-6°. Remaining local maximum tangent errors: collar2.242°,shirt1.481°,hair.980°; normalmax.010097°. These residuals are recorded and not silently called exact. See FACET comparison and region summary.

## Actual images

Three BOX poses front/back/fullbody/neck use original4K Standard material, sourcefactors1/normalstrength1, LinearARGBHalf -> existinggameFilmic1 -> singleexplicitIEC-sRGB -> LinearARGB32/rawPNG. Samejob known0/.18/.5/1 transfer sentinel passes. Directionalbaseline1.1 +flatambient.35, no large repeatedlightinggrid. BlenderAgX/broadarealighting remains a different reference.

Three separate STATIC frozenDance poses .25/.50/.75 use originalpose-reference boneworld*K_i, fixedK_i from truebind only. Animator disabled. SourceSkin max3.188µm and all136boneangle max.000420°. They are pose diagnostics ONLY, not Danceplayback/interpolation/fullmotion acceptance. Explicit captions are in the subfolder.

All12 fullbody raster checks (BOX+frozenDance, front/back) agree with the projection of the full typedweighted mesh within1.466pixels and retain8.33% viewport margin. No camera shrink or sourceflooroffset applied. Originalsource ground minima are recorded, not accepted or hidden.

## Visual decision

OriginalTspace transport does NOT eliminate the pronounced shiny/foil-like shirt striping under this directional baseline. Hair also retains bright highlights. Material appearance remains REJECTED. Do not promote this as finishedcharacter/material; no normalstrength/metal/roughness fudge was applied. SourceRGB-Z reconstruction previously did not remove the effect either. Further physical source-light/display comparison or separately documented derived artwork/material calibration must preserve this baseline and explicitly distinguish altered appearance from source-preservation.

Latest complaint comparison: frozen-dance-diagnostic/frozen-dance-DIAGNOSTIC-0.50-1-neck.png and0.75front/back. No fullDance, combat, phone, all11 or production-quality acceptance.

# Actual outcome — rejected as standalone shoulder/fold fix

Two sequential headless Blender4.2.22CPU6jobs exited0:213.244+93.175=
306.419seconds aggregate, below600. Original source files were never saved.
Main job completed7poses and18paired PNGs. The approved ordinary-motion follow-up
produced8morePNG, but **its fixed dance50 camera framing failed for BOX/kick**:
BOXis mostly outside frame and kick is not centered. Preserve those as cameraRED;
they do not establish ordinary-motion visual acceptance. No engine rerun after
release. A pose-follow camera helper is prepared separately, unexecuted.

Actual configuration: original source LBS/weights, local reviewed garment group,
ORCOrest,factor.5,iterations5,LENGTH_WEIGHTED,use_only_smooth=false. Original
994452vertices/1988914triangles/UV/maps/binds unchanged. Protected221624vertices
and every vertex outside reviewed support have exactly0surface difference in
all7poses. Original actual vertex-group hash and rest/UV/topology hashes match
again after modifier and temporary nondeforming group removal, for both jobs.
Independent baseline full-mesh original-LBS error≤.328µm. No Bweights were used.

|Pose|Max corrected surface delta mm|Opposed baseline normals|New collapsed faces|
|---|---:|---:|---:|
|dance45|3.267|7|0|
|dance50|3.974|12|0|
|dance55|.999|50|0|
|neutral|.000179|0|0|
|BOX|2.160|10|0|
|kick|.605|42|0|
|65°stress|5.897|44|0|

Opposed normals are retained RED witnesses; they are not proof of self-
intersection or an incorrect rig. All exact IDs remain in report.json. This
experiment did not enforce an artificial zero open-shirt volume. Incident
surface area changes roughly−.5…−1% at nonneutral poses, measured only.

Agent inspected actualdance50 source front/back/correct−X and45/50/55clay
pairs. The algorithm slightly softens redistribution near the junction; deep
underarm gathering and compressed striped texture remain. It is insufficient
as the requested standalone correction: **REJECTED, no production promotion**.
Do not run larger factors/iterations as another unbounded microcorrection.

Numeric strain does not rescue the result: dance50 sleeve p99absolute excess
.743766→.743382mm is almost unchanged; torso2.451625→2.412737mm mildly improves,
but p99absolute-logstrain worsens sleeve.626611→.627350 andtorso1.116193→1.157984.
No threshold relaxation or claim of uniformly improved tissue/cloth quality.

Valid primary views: actual-v1/dance50-source-paired.jpg and
actual-v1/dance45-50-55-clay-paired.jpg. Numerical summaryCSV and full reports
are durable. ordinary-motions-v1/BOX-kick-*-paired.jpg explicitly remain invalid
framing evidence. The native100kBIND10/20previousrejection is documented inREADME;
this distinct full2MORCO5experiment also fails visual sufficiency.

Blender-only modifier; no portable Unity/game deformation implementation,
fullmotion export, catalog change, phone or thermal acceptance. Heavy window
explicitly released after both processes exited. No next candidate launched.

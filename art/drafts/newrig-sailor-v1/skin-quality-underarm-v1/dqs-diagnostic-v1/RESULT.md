# Actual diagnostic completed; DQS rejected as standalone fix

Blender4.2.22 exited0 after164.683seconds, CPU6. Seven poses and24PNGs completed.
All994452vertices/1988914triangles participate. Original mesh/UV hashes, actual
vertex groups and DEFbinds match before/after. All136bone matrices remain exactly
equal across skinning modes. Independent original-LBS evaluation maximum
error0.328µm. Source files were not saved or changed.

Root and agent inspected dance50 paired source/clay front, correct−X and back.
DQS changes sleeve/limb volume, but deep underarm gathering and compressed stripes
remain. **No quality promotion or Unity DQS proposal accepted.** This rules out
“turn Preserve Volume on” as a sufficient fix in this tested pose; it does not
prove that LBS alone caused the fold or that all weights are wrong.

|Pose|Full-mesh DQS−LBS max mm|R sleeve p99edge ratio LBS→DQS|R sleeve p99absolute excess mm LBS→DQS|
|---|---:|---:|---:|
|dance45|32.326|2.2997→2.5592|1.1316→1.3648|
|dance50|23.816|1.8711→1.9541|.7438→.8323|
|dance55|7.529|1.3182→1.3370|.2955→.3273|
|65°stress|92.930|2.5701→3.7821|1.3816→2.4896|
|neutral|.00326|1.00003→1.00004|.000040→.000049|
|BOX|34.453|1.7489→1.8015|.7793→.8488|
|kick|33.215|1.2209→1.2391|.2241→.2467|

Ratios and absolute excess include any edge endpoint in the original provisional
region, without subsampling. They are scoped diagnostics, not ground-truth cloth
quality. DQS affects other regions too: broad head-envelope max3.41mm atdance50,
distal-arm max17.66mm; these are skinning differences, not source-bone changes.

See actual-v1/summary.csv, report.json and dance50-paired-{source,clay}.jpg.
Inherited render-entry `material` labels in report.json always say source-normal1
because source evaluator was loaded with --source-material; filenames encode the
actual controlled override: *-clay-* uses diagnostic flat clay, *-source-* uses
untouched source materials. This metadata limitation does not alter renders.
No independent DQ algorithm, animation-export, runtime, phone or thermal claim.

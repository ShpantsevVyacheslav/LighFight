# Actual Unity original-expression review: correctness GREEN, performance not accepted

Review-only code, Unity6000.3.26f1 Editor batchmode; production Candidate and original geometry/weights untouched. No physical-phone, player, IL2CPP or thermal acceptance.

1. actual-original-leaf-diagnostic: EXIT0. BVH/full shared original-expression evaluator/original separate typed loop/production Candidate all bit-equal0x3C4000E2. Independent double minimum0.011718961216843781. Rounded-every-operation oracle differs on54691points, explaining why the previous explicit-boundary E3 strategy was integrationRED; no epsilon waiver.
2. actual-original-leaf-validation: EXIT0.65scalar poses checked exactly against Candidate;8warm poses excluded from57timed rows.14synthetic assertions;4Bake checkpoints ×143168=572672selectedpoint independent3D comparisons. Max fulltyped/Bake(true)+fullTRS world error1.2017e-7m; floorerror3.7253e-9m. Bake(false)+rigidTRS independently checked.129heldhits with no added feature/full scans;128inactivecalls with no row capture/scans.
3. actual-original-leaf-profile: EXIT0, same correctness gates retained. BVHmedian4.6431ms, p957.6917ms; fullmedian4.7480ms, p954.8271ms; rowmedian.0249ms. Median BVH only2.2%lower, p9559.3%worse. Prior validation timing medianBVH5.0607/full4.7515 also shows no robust improvement. Do not promote as heating optimization. Timings alternate full/BVH order, Editor only. Shared evaluator call overhead and bounds traversal remain costs; no mesh reduction used.

GC thread counter was calibrated unsupported (observed1MiB allocation returns0); all raw zero allocation fields are explicitly unqualified. Synthetic inactive check now asserts only meaningful no-work counters/state. Native object estimates: mesh256636576B, referencedtextures246068796B, support5347483B (these do not establish isolated process RSS).

Profile entrypoint first attempt had a review MenuItem attribute placement compiler error; repaired and actual successful rerun retained separately. Engine has fully exited, heavy window explicitly released to root. No additional heavy work authorized by this report.

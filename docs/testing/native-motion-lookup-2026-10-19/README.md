# Native animation lookup rewrite — 19 October 2026

Replaced capturing Array.Find/Exists predicates for native motion and contact-pose tables with simple linear lookups. The first-match order, live table replacement/update and missing-motion behavior remain unchanged. No lookup cache can become stale when the actor's animation tables change.

Actual graphics-backed Unity EditMode/EnterPlayMode regression: **34/34 PASS**, 0 skipped, 6.4953718s. Includes 3 new lookup contract cases, 19 anatomical-joint cases, real geometric strike-stop/recovery and artist kick reference tests, plus preview/results/victory behavior. Root parsed `actual-unity.xml`.

A Cecil audit of the actual Unity-compiled runtime assembly confirms no `newobj` or `System.Array` predicate calls in the six lookup/heading/duration methods listed in `compiled-il.log`. This establishes removal of these particular capture allocations. It does not establish zero allocation for the whole animation player; other paths still contain allocations. Unity's per-thread GC counter failed positive calibration elsewhere, so it was not used here.

Initial audit compile omitted the netstandard reference and failed; corrected with Unity Mono 4.5/Facades/netstandard.dll, then compile and audit both exited0. Reproduction source: `LookupIlAudit.cs`, referencing Unity's bundled Mono.Cecil.dll.

No new phone build or thermal acceptance. No source mesh/skin weight/texture changes. Prior unstaged selection lifecycle work in FighterView is not part of this isolated lookup commit.

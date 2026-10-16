Matched R7 actual-player result — 2026-10-16

Both Development Metal players completed120s, errors0, observed jab/frontKick/jumpKick/sideKick coverage and actual hit/damage gate passed. Baseline7112frames/100hits; candidate6984frames/94hits. Source-controlled rendering settings, geometry, original maps/materials, camera and collector SHA match. Full11/5 artist input gates before/after are byte-identical; exactly one of51production CS differs in built sources. Main51CS remain unchanged; candidate owned CS restored in finally. Main was not launched/edited.

Measured CPU marker benefit (milliseconds):

|Metric|Baseline|Vertical-row|
|---|---:|---:|
|Grounding Apply mean / p95|1.213 /2.398|0.783 /1.494|
|Grounding Cap mean / p95|0.307 /4.763|0.199 /2.443|
|Retro body Cap mean / p95|5.005 /8.851|3.109 /5.654|
|Retro body observed frames|403|407|
|Wall p95 / worst|18.158 /188.150|18.417 /388.195|
|GPU mean / worst|3.607 /8.382|3.734 /61.663|

Retro scans/frame1.034→1.029; Island.968→.974. Most scan frequency remains because animated vertical rows change. This is evidence for lower grounding cost, not proof that fewer cache misses explain the gain; the compact Vector4 weighted path also changes generated copying/access work. No whole-frame/tail improvement or phone heating fix established. Parent seven actual Unity weighted-reference/cache tests establish floor arithmetic correctness, not these time-series alone.

Actual floor/contact observation: cached selected-support minimum+correction >=0 in every recorded frame for both actors, both runs; no empty/unknown support arrays. Retro native10731/body58198points, Island142/body260. Real attacks and hits occurred in both runs. These are different real-time gameplay histories, so exact contact/damage parity is NOT claimed; no new independent full-mesh floor verification. Candidate after-screen image inspected: authored native actors, materials, shadows and kick visible; no obvious new raster/material defect. Different gameplay phases mean screenshots are not pixel parity proof.

Render Thread valid but sampleBlockCount0 in all measured frames; its zero elapsed remains unavailable evidence. Main samples observed; main inclusive elapsed contains60FPS pacing. Counter/wall outlier snapshots retained in outlier-frames.json: largest388ms wall at jumpKickFrame41 had ground1.006ms/cap.123ms, so grounding alone cannot explain it. Largest GPU61.663ms belongs to a different frame; cause unproven. Do not discard tails or claim normal GPU budget from mean.

Scope: Mac M1, Metal1280x720, CPU skinning, MacUltra/cascades4, authored Max60/4xMSAA/HDR. Not iOS quality/cascade match, energy/thermal measurement, shipping Addressables memory/load parity, or deterministic simulation A/B. Selected-inline refs are exactly sourceRetro8/Island3/arena0, remapped0/1/0. No quality cut.

Recommendation: candidate merits parent all11 floor/contact regression review because actual grounding costs fell~35–40% with preserved inputs; do not promote it as a heat/tail fix. Retain actual outliers and measure matched Release on phone when accessible. No further engine/player is active; root owns next window.

Files: comparison.json, matched-control-gates.json, post-main-source-check.json, provenance.json, reviewed-grounding.diff; Evidence/{baseline,candidate}/{frames.csv,configuration.txt,result.txt,coverage*,diagnostic-summary.json,summary.json,active-battle-*-screen.png}; Build manifests/source/art SHA lists and logs under Builds. Parser4tests and offline collector compilation passed. Original R6 retained unchanged. Reproducible R7 apps retained pending disk cleanup authorization.

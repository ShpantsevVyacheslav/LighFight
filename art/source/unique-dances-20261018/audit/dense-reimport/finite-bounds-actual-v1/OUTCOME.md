# Cached finite bounds actual Unity review

Unity6000.3.26f1 actual GPU EXIT0. Pure corner/full-landscape tests PASS.72 actual cases:8 extrema/endpoints ×yaw−90/+90/170° ×uniformscale.7/1/1.8, translatedroot(2.1,−.3,1.7). Every case checks ALL1,305,574 renderer seamvertices via typed four-weight shaderworldLBS against native-C cached bounds transformed through bridge.actorAnchor. No second C, no per-pose bound shrink, no SMR.localBounds mutation.

Actual1280×720 full-landscape opaque white actor/black background GPU masks pass nonempty/minimum2pixel edge clearance. Two editor player-loop updates, explicit render, next update and second render before readback. Eight retained scale1/yaw90 snapshots inspected representative6s: head/boots uncut; fullclip union naturally leaves pose-dependent lateral empty space. This verifies framing/no clipping at these sampled extremes, not artist appearance or desired steady pose size.

Originalweights UNGROUNDED steadyDance only. Finite4759-domain bound plus explicit.025m native margin, no continuous certificate. UpOnly/Signed analytical envelopes have not been exercised with actual runtime grounding here. B weights, arbitrary entry/return, physicalfloor, gameplayheight and material/underarm acceptance excluded. No catalog/FighterView change.

First job failed before actor load because untitled scene cannot open additively; fixed fixture to existing Single/Additive contract. That FAIL retained, no source/provider changes required. Retry passed. Both engine processes exited; heavywindow explicitly released to root.

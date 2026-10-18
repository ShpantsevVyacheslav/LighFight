# Cached-clock review owner: actual regression + mutation

Combined actual PlayMode12/12PASS,31.0549611s: owner2 plus root graphics/mobile-default10. Root FightOptions/test changes are external ownership and excluded from this owner commit.

Owner caches Animator and full Dance-state hash during typedwarmup. Only transitions use FighterView.PreviewNativeMotion; changed-pose frames use the cached tinyclock directly, then exact source-key bridge. FighterView source unchanged. Prior Animator speed/rootMotion settings restore after return/disable/disposal; source mesh/weights/maps unchanged.

Tests now enter from a real SourceBox authoredpose, compare actual local TRS entry with the0.28s source-target smoothstep blend, verify tinyclock/source-time agreement, full23.1667s EOF before return, finalneutral/restoredsettings, cancel/disable/residency/support ownership. This is explicit review playback API, not a production selection/Victory hook.

Mutation removes only cached Animator.Play while exact source still advances. Focused test0/1, EXIT2, expected normalized clock0.000962778926 versus actual0;6.6065661s. Owner source restored BYTE-EXACT to the12/12GREEN SHA caf5ef95367493fa9878dd5e1ad7b01e0b97cc610d0c7bf8964d80015d97ea27. Logs/XML/hashes retained.

Profile separate EXIT0: owner+cachedclock+bridge128changedposes p50.4572ms/p95.4713ms/max.5144ms; cold27.6MB keydecode+fixedbind159.4812ms. Editor host, groundingexcluded, not phone/quietCPU. Actual foot integration is still RED/open separately; no performance acceptance for fullactor.

## Allocation qualification FALSE

Known byte[1048576] was observably created and retained, but Unity threadcounter stayed0; GC.Alloc recorder Valid yet immediateevents0/unitTimeNanoseconds. Neither observer was positively calibrated, so NOzeroallocation claim. Profile folder contains exact calibration and scope. Earlier bridge-only UnityGC0 claim withdrawn. A later qualified Editor/Player allocation observer is required; do not infer noGC from0unsupportedcounter.

No source/artist underarm acceptance, all11unique-dance acceptance, continuous-time proof, gameplayheight/physicalfloor acceptance, productioncatalog, standaloneplayer or phone promotion.

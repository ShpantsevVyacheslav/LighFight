# LighFight motion expansion

Budget: 400 Meshy credits. Submitted: 216; remaining: 184. Full task IDs and prompts are in tasks.json. No further chargeable jobs are scheduled.

The batch contains 21 new library clips (63 credits), an earlier jab reused for free, a remesh and Rose rig (10), ten Prime text motion jobs plus application (130), and one axe-kick retry (13). Both generated axe-kick trajectories were unsuitable; its final leg trajectory is authored locally on the same skeleton. Walking comes free with the rig.

Rose has a 108,378-triangle repaired mesh, photo-generated rigid closed fists, cuffs covering the wrist transition, and a 4096px albedo atlas baked with face-sampled skin. Hands cannot open; the former palm action is presented as a closed-fist push. Hair remains generated geometry.

32 source motions are baked onto the exact character skeleton, with horizontal root movement removed. The game exposes 25 attacks; contact pose is aligned to the first active simulation tick, preserving anticipation and return. Knockdown is 90 ticks with 30 ticks reserved for getting up. Idle and guard hold a sampled defensive pose. Forward and backward fighting steps replace ordinary walking; first placement is not interpreted as movement.

Scripts save raw provider files only under ignored .tools. Unity FBXs, controllers, materials and prefabs are the reviewable game assets. The generation ledger prevents duplicate submissions. Regeneration failures are recorded rather than silently substituted.

Verification: 41 core tests and 52 Unity tests passed; contact-timing and duplicate-exit mutations rejected, Unity samples each clip at 61 phases and checks finite/bounded geometry, runtime pose audit checks all 25 attacks across 1290 combat frames per character (2580 total), verifies 9 support states and checks rigid fist centroids remain within 25 cm of their hand bones. Anatomical guard height and forward jab reach have explicit tests. These checks do not establish likeness or professional animation quality.

HostFightReview.unity is a new optional embedded battle scene, with HostBridge and local review content. It does not replace the previously tested HostArtReview scene automatically. iPhone build 63 proves the older review launched, not that this batch passed device validation. The LighChat build owner controls the device export.

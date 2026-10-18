# Native dance shoulder deformation

Sailor native FBX and exported GLB were compared at 0/25/50/75% of dance_01. Vertices were matched once in rest pose, then evaluated through each rig. Maximum shoulder-region position difference: 0.576 micrometres. Paired source/export renders show the same sleeve pinching and raised shoulder silhouette. This locates the observed deformation upstream of GLB export; it does not establish whether the source pivot placement, skin weights or motion retargeting is the primary cause.

No rig, skin weights or animation tracks were modified by this investigation. Corrective work remains open: diagnose shoulder/clavicle placement and local weights in a versioned copy, then check arm elevation/twist and all eight native motions against the source before promotion. A shortened dance is not a demonstrated fix.

See paired-shoulders.png and result.json for captured and numeric evidence.

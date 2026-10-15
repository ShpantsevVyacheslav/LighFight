# Raw source bones: read-only discovery

Blender4.2.22LTS, original SHA5cc8b7208f3267cb1e20909bcff4eb04a9b9bf58ce7f28ebb77bbdb516c41a24. Exit0. 67 bones,531+671+601 phase samples at240Hz including exact endpoints. No saved FBX/blend/source edits. Mesh viewport disabled: these CSVs deliberately do NOT certify the skinned surface, Unity fidelity, target scale, hand contact or source PBR appearance. XYZ is original Blender armature space. Distances are source units, not advertised game metres.

All67bone endpoint positions after hip-translation removal differ by at most1.239e-6 source units for box_01,0.595690 for box_02,0.014553 for front_kick_01. This supports a recovered source loop for box_01 and disproves equivalent whole-take endpoint recovery for box_02. It does not prove correct transition from the game's shared guard or a single strike.

The angle between successive proximal/intermediate/distal bone directions in box_01 stays approximately3.12–5.36degrees across all eight nonthumb fingers. In box_02 the same bends span approximately12.88–99degrees. This is geometric evidence of very different source finger action; neither a definitive fist/thumb skin certificate nor a reason to distort the source mesh. Inspect actual raw Generic skin before accepting a punch.

Discovery analyzer now prioritizes exact Chest over suffix UpperChest. Old suffix-only search incorrectly rejected Chest as ambiguous and fell back to Hips. Synthetic fixture with both bones passes3takes; restored ambiguous-search mutation yields wrong Hips for all3. Candidate metadata regenerated. No gameplay mapping accepted.

Reproduce from repository root with the read-only review.py command in its source and original FBX retained at inputs.json path. Unzip CSV.gz into an owned review directory to run analyze_trajectories.py. Full surface/pose/EOF, actual hand geometry, heading, segment and entry/return blend gates remain mandatory.

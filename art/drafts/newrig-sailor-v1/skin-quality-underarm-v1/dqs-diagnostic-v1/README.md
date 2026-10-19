# Full-density LBS versus Blender Preserve Volume diagnostic

Read-only isolated source comparison, not a proposed Unity DQS integration.
Prior newrig-partition-audit.json records preserveVolume=false; the current
true-bind/C1 and canonical FBX source use game LBS with one segment per DEF bone.
No prior equivalent paired full-density DQS visual gate was found. The CPU/GPU
skinning benchmark addresses execution mode, not linear versus dual quaternion
blending. Existing competitor research already warns that DQ cannot by itself
repair weights or guarantee a natural loose sleeve.

Named entrypoint (only after serialized heavy-window release):

```
/Users/macbook1/Applications/Blender.app/Contents/MacOS/Blender \
 --background --factory-startup --threads 6 --python-exit-code 1 \
 --python art/drafts/newrig-sailor-v1/skin-quality-underarm-v1/dqs-diagnostic-v1/tools/probe_lbs_dqs.py
```

Seven poses: dance45/50/55, actual65°arm stress, neutral, BOX extreme and front
kick. Same C1 source evaluation is frozen once per pose. Only the Armature
modifier's use_deform_preserve_volume changes. Bone matrices must remain exactly
the same. All994452vertices/1988914triangles participate; original attrs, actual
vertex groups and bind hashes must match before/after. Independent original
matrix-LBS is checked against actual evaluated LBS. Blender evaluates DQS; no
independent DQ implementation is claimed.

24PNG planned: dance50 source/clay front/full, correct−X close and back close;
other poses correct−X clay pair. Same framing across the two modes. Regional
strain/absolute excess/area/collapse and whole-mesh plus anchor displacement
are measurements, not an automatic acceptance threshold. Masks remain the
previous provisional geometric envelopes, not newly asserted anatomical seams.
A diagnostic may improve one fold while distorting another area; inspect both.

900second deadline checked between operations, CPU6, no source saves. Source
materials/images remain unchanged. Output is actual-v1/report.json, PNGs and
run-v1.log; any exception persists FAILED_DIAGNOSTIC and exits nonzero. No source
geometry reduction, no weight or pose correction, no runtime/phone claim.

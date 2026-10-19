# Original-weight local Corrective Smooth — rejected diagnostic

Actual seven-pose/18PNG job and eight-PNG ordinary-motion follow-up completed.
See RESULT.md: standalone fix rejected; ordinary-motion camera framing RED.
No production or game-export acceptance. The earlier native67/100k test was already
performed and rejected: prototypes/roster-portraits/tools/shoulder/
try_corrective_v5.py, BINDrest, LENGTH_WEIGHTED,.5×10/1×20, broad shoulder mask,
dance50/75. README in that folder records unchanged silhouette. Posed ordinary
SMOOTH30/100 was also rejected for texture gathering. Do not relabel this as an
entirely untried algorithm. The distinct question here is original full-density
Rigify/C1, original source weights, reviewed garment support, ORCOrest,.5×5.

**Blender-only diagnostic; no Unity/game export acceptance.** The modifier is
placed after original game-LBS Armature. All DEF Bbone segments1, Preserve Volume
off. No B-derived weights are loaded. Corrective Smooth uses reconstruction,
not ordinary smoothing: use_only_smooth=false, rest_source='ORCO',
smooth_type='LENGTH_WEIGHTED', factor=.5, iterations=5, scale=1. The original base
mesh supplies rest coordinates; **do not call correctivesmooth_bind** for this
mode. That operator belongs to BIND, and is_bind is not an ORCO success test.
The script checks RNA support and topology/UV/count compatibility before scoring.

The binding/API distinction is verified against official Blender4.2release
[RNA definitions](https://raw.githubusercontent.com/blender/blender/blender-v4.2-release/source/blender/makesrna/intern/rna_modifier.cc)
and [modifier implementation](https://raw.githubusercontent.com/blender/blender/blender-v4.2-release/source/blender/modifiers/intern/MOD_correctivesmooth.cc).
Runtime4.2.22 RNA/evaluation was verified in the authorized isolated jobs, with
source restoration hashes passing. That does not establish a game-export path.

Reviewed original garment support11728IDs is unchanged; an inward6mm original-
edge feather reduces positive modifier weights to10933IDs, with795boundary IDs
exactly zero. Protected221624IDs remain outside the support. The mask source SHA
is retained; this narrows/tapers existing reviewed support, never expands it.
A temporary nondeforming QA vertex group is not a fifth skin-bone influence.
The modifier/group are removed in finally; complete original actual group hash
and original mesh/UV/topology hashes must match after removal. No source save.

Named job, only after explicit serialized heavy-window release:

```
Blender --background --factory-startup --threads 6 --python-exit-code 1 \
 --python art/drafts/newrig-sailor-v1/skin-quality-underarm-v1/corrective-smooth-garment-v1/tools/probe_corrective_smooth.py
```

Seven cases dance45/50/55, neutral, BOX/kick,65°stress. Full994452vertex baseline
LBS independent witness; source994452vertices/1988914triangles and all UVs stay
exact. Same bone matrices across modifierOFF/ON. Measure all original region
edges, absolute excess, local normal-opposition/area and actual protected/outside
surface differences. Do not force zero open-shirt volume or infer skin quality
from a lower strain percentile. Report deformed neutral differences honestly;
base/rest buffers being exact does not imply evaluated neutral is identical.

18matched actual PNGs completed after scope gates: dance50 source/clay−X/front/back
plus neighboring/stress−Xclay. Corrected surface reference is Blender evaluation
itself, not a falsely independent Corrective Smooth implementation. Re-evaluation
is checked for stable positions when rendered. Any protected/outside-scope move
above the existing1nm diagnostic numerical fence stops beauty rendering and
persists failure. Orientation witnesses are recorded, not called self-intersection.
Newartquality still needs visual inspection. CPU6/600second between-operation
budget; full2M tangent/delta caches may use additional hundreds of MiB. No
geometry reduction, source texture conversion, runtime modifier or phone claim.

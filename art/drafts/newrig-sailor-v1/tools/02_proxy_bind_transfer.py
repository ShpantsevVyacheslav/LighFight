"""Next-stage executable pilot. Requires reviewed stage1; no dense heat weights.
Blender -b --factory-startup --python-exit-code 1 -P this_file -- --reviewed-fit
Not executed/accepted merely by existence. Full motion validation remains separate.
"""
import bpy,sys,json,types,hashlib
from pathlib import Path
import numpy as np
ROOT=Path(__file__).resolve().parents[4];OUT=ROOT/'art/drafts/newrig-sailor-v1'
assert '--reviewed-fit' in sys.argv,'Review placement and correct metarig before binding.'
bpy.ops.wm.open_mainfile(filepath=str(OUT/'stage01b-corrected-met-rig.blend'))
code=(Path.home()/'.agents/skills/scenario-blender-rigging/scripts/bx_rig.py').read_text();R=types.ModuleType('pilot_rig');R.__file__=str(Path.home()/'.agents/skills/scenario-blender-rigging/scripts/bx_rig.py');exec(compile(code,R.__file__,'exec'),R.__dict__)
body=bpy.data.objects['Sailor_OriginalDense_UNTOUCHED'];proxy=bpy.data.objects['BIND_PROXY_ONLY'];meta=bpy.data.objects['FreshSailor_CONTROL_METARIG_v1']
# Hash baseline using definitions only, without executing stage1 import.
source=(OUT/'tools/01_audit_fit.py').read_text();defs=source[source.index('def hash_arrays'):source.index('def write')];exec(defs)
before=hash_arrays(body);R.enable_rigify();rig,error=R.generate(meta);assert rig is not None,error
proxy.hide_set(False);proxy.hide_render=False
rep=R.bind(rig,proxy,max_influences=4,subdivision_last=False);assert rep['ok'],rep
# Dense never enters parent_set ARMATURE_AUTO or proxy weight solver.
bpy.context.view_layer.objects.active=body
for o in bpy.context.selected_objects:o.select_set(False)
body.select_set(True)
mod=body.modifiers.new('AppliedProxyBarycentricWeights','DATA_TRANSFER');mod.object=proxy;mod.use_vert_data=True;mod.data_types_verts={'VGROUP_WEIGHTS'};mod.vert_mapping='POLYINTERP_NEAREST'
bpy.ops.object.datalayout_transfer(modifier=mod.name);bpy.ops.object.modifier_apply(modifier=mod.name)
bpy.ops.object.vertex_group_limit_total(group_select_mode='ALL',limit=4)
bpy.ops.object.vertex_group_normalize_all(group_select_mode='ALL',lock_active=False)
allowed={b.name for b in rig.data.bones if b.use_deform};bad=[];unweighted=0;over4=0;maxSumError=0;maxInfluences=0
for v in body.data.vertices:
 weights=[g.weight for g in v.groups if body.vertex_groups[g.group].name in allowed and g.weight>0];total=sum(weights);unweighted+=total<.999;over4+=len(weights)>4;maxSumError=max(maxSumError,abs(total-1));maxInfluences=max(maxInfluences,len(weights))
assert unweighted==0 and over4==0 and maxSumError<1e-5,(unweighted,over4,maxSumError)
assert hash_arrays(body)==before,'Dense position/UV/topology preservation failed'
arm=body.modifiers.new('FreshGameDEFDeform','ARMATURE');arm.object=rig;arm.use_deform_preserve_volume=False
proxy.hide_render=True;proxy.hide_set(True)
report={'status':'BOUND_NOT_MOTION_ACCEPTED','proxyBind':rep,'denseVertices':len(body.data.vertices),'triangles':sum(len(p.vertices)-2 for p in body.data.polygons),'unweighted':unweighted,'maxInfluences':maxInfluences,'maxWeightSumError':maxSumError,'denseAttributesUnchanged':True,'sourceAttributesSHA256':before,'deformBones':len(allowed),'warning':'New rig requires actual retarget + motion/visual quality gate before export/promotion.'}
(OUT/'stage02.json').write_text(json.dumps(report,indent=2));bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'stage02-bound-original-dense.blend'),compress=True);print('RESULT '+json.dumps(report),flush=True)

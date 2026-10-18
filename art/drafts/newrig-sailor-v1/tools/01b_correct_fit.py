import bpy,types,json
from pathlib import Path
from mathutils import Vector
ROOT=Path(__file__).resolve().parents[4];O=ROOT/'art/drafts/newrig-sailor-v1'
bpy.ops.wm.open_mainfile(filepath=str(O/'stage01-audit-fitted-met-rig.blend'))
code=(Path.home()/'.agents/skills/scenario-blender-rigging/scripts/bx_rig.py').read_text().replace('    sc.render.image_settings.media_type = "IMAGE"\n','');R=types.ModuleType('pilot_rig');R.__file__=str(Path.home()/'.agents/skills/scenario-blender-rigging/scripts/bx_rig.py');exec(compile(code,R.__file__,'exec'),R.__dict__)
report=json.loads((O/'stage01.json').read_text());lm={k:Vector(v) if isinstance(v,list) and len(v)==3 else v for k,v in report['landmarks'].items()};old=bpy.data.objects['FreshSailor_CONTROL_METARIG_v1'];bpy.data.objects.remove(old,do_unlink=True)
# Explicit measured-view anatomical corrections, sourceunits; independent of provider skeleton.
# Automated arm crosssection confused shirt axillary/pectoral bulge with humeral pivot.
lm.update(shoulder=Vector((.130,.012,.790)),clav_head=Vector((.015,.012,.805)),clav_tail=Vector((.130,.012,.790)))
meta=R.add_metarig('human',name='FreshSailor_CONTROL_METARIG_v1');R.fit_metarig(meta,lm)
proxy=bpy.data.objects['BIND_PROXY_ONLY'];proxy.hide_render=False;proxy.hide_set(False);R.placement_sheet([proxy],str(O/'placement-corrected'),arm_obj=meta,views=('front','right','top'),res=768)
report['correctedLandmarks']={k:list(lm[k]) for k in ['shoulder','clav_head','clav_tail']};report['automaticFitVisuallyRejected']=True;report['status']='CORRECTED_FIT_REVIEW_REQUIRED';report['correctedFitProblems']=R.check_metarig(meta,proxy)
(O/'stage01b.json').write_text(json.dumps(report,indent=2));bpy.context.preferences.filepaths.save_version=0;bpy.ops.wm.save_as_mainfile(filepath=str(O/'stage01b-corrected-met-rig.blend'),compress=True);print('RESULT '+json.dumps(report),flush=True)

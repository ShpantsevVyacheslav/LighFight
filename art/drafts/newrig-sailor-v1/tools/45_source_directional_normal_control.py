"""Bounded original-material normal ON/OFF control; no source writes."""
import bpy,json,math,hashlib
from pathlib import Path
import numpy as np
from mathutils import Vector
O=Path(__file__).resolve().parents[1];D=O/'dense-def-fbx-packet-v1/source-directional-normal-control';D.mkdir(exist_ok=True)
s=(O/'tools/27_actual_arm_elevation.py').read_text();exec(compile(s[:s.index('# Corrected actual elevation fixture')],str(O/'tools/27_actual_arm_elevation.py'),'exec'))
D=O/'dense-def-fbx-packet-v1/source-directional-normal-control';D.mkdir(exist_ok=True)
retarget('dance_01',.5);angles=support('dance_01',.5)
for ob in list(scene.objects):
 if ob.type=='LIGHT':bpy.data.objects.remove(ob,do_unlink=True)
# Unity light Euler(35,-25,0): R_y * R_x * forward; canonical B^-1 converts Y-up to Blender Z-up.
x,y=map(math.radians,(35,-25));u=Vector((math.sin(y)*math.cos(x),-math.sin(x),math.cos(y)*math.cos(x)));ray=Vector((u.x,-u.z,u.y))
ld=bpy.data.lights.new('UnityDirectionalControl','SUN');ld.energy=1.1;ld.angle=0.;lo=bpy.data.objects.new('UnityDirectionalControl',ld);scene.collection.objects.link(lo);lo.rotation_euler=ray.to_track_quat('-Z','Y').to_euler()
scene.world.use_nodes=True;bg=scene.world.node_tree.nodes.get('Background');bg.inputs['Color'].default_value=(1,1,1,1);bg.inputs['Strength'].default_value=.35
scene.render.engine='CYCLES';scene.cycles.device='CPU';scene.cycles.samples=16;scene.cycles.use_denoising=False;scene.render.threads_mode='FIXED';scene.render.threads=6
scene.render.resolution_x=720;scene.render.resolution_y=720;scene.render.resolution_percentage=100
scene.view_settings.view_transform='Standard';scene.view_settings.look='None';scene.view_settings.exposure=0;scene.view_settings.gamma=1
center=rig.matrix_world@((rig.pose.bones['ORG-upper_arm.L'].head+rig.pose.bones['ORG-upper_arm.R'].head)*.5);center.z+=.025
cam.location=center+Vector((0,-2,.06));cam.rotation_euler=(center-cam.location).to_track_quat('-Z','Y').to_euler();cd.ortho_scale=.64
links=[];materials=[]
for slot in body.material_slots:
 mat=slot.material
 if not mat or not mat.use_nodes:continue
 for node in mat.node_tree.nodes:
  if node.type=='BSDF_PRINCIPLED':
   materials.append({'name':mat.name,'normalLinks':len(node.inputs['Normal'].links)})
   for link in list(node.inputs['Normal'].links):links.append((mat,link.from_socket,link.to_socket))
report={'status':'SOURCE_LIGHTING_CONTROL_NOT_ENGINE_PARITY','clip':'dance_01','phase':.5,'sourceGeometryExact':True,'sourceVertices':N,'sourceTriangles':1988914,'sunEnergy':1.1,'ambientWorldStrength':.35,'unityEulerDegrees':[35,-25,0],'unityForward':list(u),'blenderRayDirection':list(ray),'conversion':'canonical inverse B: Unity(x,y,z) -> Blender(x,-z,y); not actual Unity importer C reflection','cameraWorld':list(cam.location),'cameraTarget':list(center),'orthoScale':cd.ortho_scale,'support':angles,'materials':materials,'limitations':['Cycles SUN/world environment is a source control, not a calibrated Unity BRDF/IBL match.','PNG Standard display transfer; EXR stores scene-linear render.','No geometry, source file, texture or scalar changes.']}
for enabled in [True,False]:
 if not enabled:
  for mat,frm,to in links:
   for link in list(to.links):mat.node_tree.links.remove(link)
 name='normal-source-1' if enabled else 'normal-OFF-diagnostic';scene.render.image_settings.file_format='OPEN_EXR';scene.render.image_settings.color_depth='32';scene.render.filepath=str(D/(name+'.exr'));bpy.ops.render.render(write_still=True)
 scene.render.image_settings.file_format='PNG';scene.render.image_settings.color_depth='8';bpy.data.images['Render Result'].save_render(str(D/(name+'.png')),scene=scene)
 print('RENDER_DONE '+name,flush=True)
for mat,frm,to in links:mat.node_tree.links.new(frm,to)
assert hash_arrays(body)==original
report['files']=[{'name':f.name,'bytes':f.stat().st_size,'sha256':hashlib.sha256(f.read_bytes()).hexdigest()} for f in sorted(D.iterdir()) if f.suffix in ['.png','.exr']];(D/'report.json').write_text(json.dumps(report,indent=2));print('RESULT SOURCE_CONTROL_COMPLETE',flush=True)

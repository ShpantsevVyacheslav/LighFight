"""Editable courtyard layout inferred from office photo-05; dimensions provisional."""
import bpy, math, json
from pathlib import Path
from mathutils import Vector
root=Path('/Users/macbook1/Documents/ChatGPT/LighFight')
bpy.ops.wm.read_factory_settings(use_empty=True)
def mat(n,c):
 m=bpy.data.materials.new(n);m.diffuse_color=(*c,1);return m
stone=mat('aged warm concrete',(.48,.46,.41));wall=mat('gray plaster',(.64,.66,.62));red=mat('burgundy painted metal',(.30,.065,.055));dark=mat('rubber and windows',(.025,.035,.04));green=mat('tree canopy',(.16,.27,.08));bark=mat('tree bark',(.20,.13,.075));white=mat('white car paint',(.8,.8,.75));black=mat('charcoal car paint',(.055,.07,.075))
def box(n,p,s,m,bevel=0):
 bpy.ops.mesh.primitive_cube_add(size=1,location=p);o=bpy.context.object;o.name=n;o.dimensions=s;bpy.ops.object.transform_apply(location=False,rotation=False,scale=True);o.data.materials.append(m)
 if bevel:mod=o.modifiers.new('edge softness','BEVEL');mod.width=bevel;mod.segments=3;o.modifiers.new('smooth normals','WEIGHTED_NORMAL')
 return o
def pole(n,p,r,h,m):
 bpy.ops.mesh.primitive_cylinder_add(vertices=12,radius=r,depth=h,location=p);o=bpy.context.object;o.name=n;o.data.materials.append(m);return o
box('surrounding gravel',(-2,2,-.3),(24,18,.22),stone)
box('clear fighting court',(0,0,-.12),(12,8,.24),stone)
box('office gray wall',(0,4.8,1.8),(14,.3,3.6),wall,.04)
box('burgundy service door',(3,4.61,1.2),(1.15,.09,2.4),red,.025)
box('brick base',(0,4.58,.22),(14,.25,.44),stone)
for x in [-5.5,5.5]:
 pole('drain pipe',(x,4.4,1.6),.065,3.2,wall)
for i in range(5):box('office stair '+str(i),(5.6,-3.2+i*.36,.08+i*.12),(2.4,.36,.16+i*.24),stone,.015)
for x in [4.35,6.85]:
 for i in range(9):pole('railing spindle',(x,-3.3+i*.23,.75+i*.08),.025,1,red)
 rail=box('burgundy stair handrail',(x,-2.36,1.57),(.07,2.05,.07),red,.01);rail.rotation_euler.x=.32
for y,c in [(-2,white),(3,black)]:
 x=-8
 box('parked car body',(x,y,.65),(2,4,.7),c,.22);box('parked car cabin',(x,y,1.18),(1.65,2.15,.65),dark,.18)
 for dx in [-.94,.94]:
  for dy in [-1.2,1.2]:
   o=pole('car wheel',(x+dx,y+dy,.40),.37,.19,dark);o.rotation_euler.y=math.pi/2
for x,y in [(-7,4),(-7,-2),(7,6),(-4,10),(2,10)]:
 pole('tree trunk',(x,y,2),.22,4,bark)
 for dx,dz in [(-.7,0),(.7,.4),(0,1.2)]:
  bpy.ops.mesh.primitive_ico_sphere_add(subdivisions=2,radius=1.6,location=(x+dx,y,4+dz));o=bpy.context.object;o.name='tree foliage';o.scale=(1,1,.85);o.data.materials.append(green)
scene=bpy.context.scene;scene.world=bpy.data.worlds.new('Courtyard daylight');scene.world.color=(.3,.3,.3)
bpy.ops.object.light_add(type='AREA',location=(-3,-4,10));bpy.context.object.data.energy=2200;bpy.context.object.data.shape='DISK';bpy.context.object.data.size=9
bpy.ops.object.camera_add(location=(13,-17,12));cam=bpy.context.object;cam.rotation_euler=(Vector((0,2,1))-cam.location).to_track_quat('-Z','Y').to_euler();cam.data.type='ORTHO';cam.data.ortho_scale=23;scene.camera=cam
scene.render.engine='CYCLES';scene.cycles.samples=24;scene.render.resolution_x=1200;scene.render.resolution_y=800;scene.render.resolution_percentage=100
bpy.ops.wm.save_as_mainfile(filepath=str(root/'art/source/courtyard-v1/courtyard.blend'))
scene.render.filepath=str(root/'art/previews/courtyard-v1/layout.png');bpy.ops.render.render(write_still=True)
(root/'art/source/courtyard-v1/brief.json').write_text(json.dumps({'reference':'assets/references/photo-05.jpg','status':'layout draft, not final art','clear_arena_m':[12,8],'dimensions':'inferred, need confirmation','meshy_credits':0},indent=2))

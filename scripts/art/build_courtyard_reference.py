"""Courtyard from photo05: explicit scene objects and original-photo surface UVs.
No image synthesis, character pose, decimation, or runtime promotion.
Blender -b -P scripts/art/build_courtyard_reference.py
"""
import bpy,math,random,json,struct
from pathlib import Path
from mathutils import Vector
ROOT=Path(__file__).resolve().parents[2];out=ROOT/'art/source/courtyard-reference-v2';preview=ROOT/'art/previews/courtyard-reference-v2';unity=ROOT/'unity/Assets/LighFight/Art/Drafts/CourtyardReferenceV2'
for p in (out,preview,unity):p.mkdir(parents=True,exist_ok=True)
bpy.ops.wm.read_factory_settings(use_empty=True);rng=random.Random(1205)
def material(name,color,rough=.7):
 m=bpy.data.materials.new(name);m.diffuse_color=(*color,1);m.use_nodes=True;n=m.node_tree.nodes['Principled BSDF'];n.inputs['Base Color'].default_value=(*color,1);n.inputs['Roughness'].default_value=rough;return m
plaster=material('Photo gray plaster',(.62,.63,.60));gravel=material('Photo gravel',(.39,.37,.32));brick=material('Old brick',(.37,.23,.16));red=material('Worn burgundy metal',(.27,.065,.065),.5);dark=material('Pipe openings',(.04,.045,.04));concrete=material('Aged concrete',(.48,.46,.42));wood=material('Stained handrail wood',(.34,.17,.08));bark=material('Tree bark',(.18,.13,.085));leafmats=[material('Leaf '+str(i),(.055+i*.017,.12+i*.025,.025+i*.007)) for i in range(5)]
photo=bpy.data.images.load(str(ROOT/'assets/references/photo-05.jpg'))
for m in (plaster,gravel):
 n=m.node_tree.nodes.new('ShaderNodeTexImage');n.image=photo;m.node_tree.links.new(n.outputs['Color'],m.node_tree.nodes['Principled BSDF'].inputs['Base Color'])
def box(name,p,s,m,bevel=0):
 bpy.ops.mesh.primitive_cube_add(size=1,location=p);o=bpy.context.object;o.name=name;o.dimensions=s;bpy.ops.object.transform_apply(location=False,rotation=False,scale=True);o.data.materials.append(m)
 if bevel:
  mod=o.modifiers.new('Soft edges','BEVEL');mod.width=bevel;mod.segments=3;normal=o.modifiers.new('Normals','WEIGHTED_NORMAL')
 return o
def beam(name,a,b,r,m):
 a,b=Vector(a),Vector(b);delta=b-a;bpy.ops.mesh.primitive_cylinder_add(vertices=16,radius=r,depth=delta.length,location=(a+b)/2);o=bpy.context.object;o.name=name;o.rotation_euler=delta.to_track_quat('Z','Y').to_euler();o.data.materials.append(m)
 for p in o.data.polygons:p.use_smooth=True
 return o
def photo_grid(name,origin,u,v,nu,nv,m,patch):
 origin,u,v=map(Vector,(origin,u,v));verts=[];faces=[]
 for j in range(nv+1):
  for i in range(nu+1):verts.append(origin+u*i/nu+v*j/nv)
 for j in range(nv):
  for i in range(nu):a=j*(nu+1)+i;faces.append((a,a+1,a+nu+2,a+nu+1))
 mesh=bpy.data.meshes.new(name);mesh.from_pydata(verts,[],faces);mesh.materials.append(m);uv=mesh.uv_layers.new();u0,v0,u1,v1=patch
 for face in mesh.polygons:
  for loop,coord in zip(face.loop_indices,((u0,v0),(u1,v0),(u1,v1),(u0,v1))):uv.data[loop].uv=coord
 o=bpy.data.objects.new(name,mesh);bpy.context.collection.objects.link(o);return o
photo_grid('Surrounding gravel',(-12,-7,-.14),(24,0,0),(0,18,0),32,24,gravel,(.43,.50,.49,.58))
photo_grid('FightFloor',(-5.51,-2.01,0),(11.02,0,0),(0,4.02,0),22,8,gravel,(.43,.50,.49,.58))
photo_grid('Office wall surface',(-7,3.4,0),(14,0,0),(0,0,3.6),4,1,plaster,(.72,.50,.97,.80))
box('Office wall thickness',(0,3.58,1.8),(14,.28,3.6),concrete,.025)
box('Office right return',(7.12,5.2,1.8),(.24,3.6,3.6),concrete,.025)
for row in range(3):
 for i in range(28):box('Exposed brick plinth',(-6.85+i*.5+(row%2)*.15,3.30,.055+row*.11),(.47,.16,.10),brick,.008)
box('Burgundy service door',(2.8,3.29,1.18),(1.15,.08,2.36),red,.018)
for x in (2.18,3.42):box('Door jamb',(x,3.25,1.2),(.07,.12,2.46),red,.01)
box('Door lintel',(2.8,3.23,2.42),(1.32,.13,.08),red,.012)
box('Door weather cap',(2.8,3.18,2.51),(1.38,.24,.06),red,.012)
beam('Door handle',(3.20,3.14,1.1),(3.20,3.14,1.28),.018,dark)
beam('Downpipe',(5.7,3.19,3.6),(5.7,3.19,.45),.075,concrete);beam('Drain elbow',(5.7,3.19,.45),(5.2,2.85,.12),.075,concrete)
for i in range(6):
 y=-3.9+i*.34;z=(i+1)*.14;box('Office step '+str(i),(7.5,y,z/2),(2.1,.35,z),concrete,.012)
for x in (6.38,8.62):
 for i in range(10):
  y=-4.06+i*.205;z=.14+max(0,(y+3.9)/.34)*.14;beam('Railing spindle',(x,y,z),(x,y,z+1.02),.023,red)
 beam('Upper burgundy railing',(x,-4.09,1.1),(x,-2.1,1.92),.045,red);beam('Lower burgundy railing',(x,-4.09,.40),(x,-2.1,1.22),.025,red)
box('Wood railing cap',(8.72,-2.25,1.94),(.30,2.3,.06),wood,.012)
for i in range(45):
 x=rng.uniform(-3,1);y=rng.uniform(5,7);bpy.ops.mesh.primitive_ico_sphere_add(subdivisions=1,radius=rng.uniform(.08,.25),location=(x,y,.04));o=bpy.context.object;o.name='Backyard rubble';o.scale=(1,.7,.6);o.data.materials.append(concrete)
for ti,(x,y) in enumerate(((-7.6,3.5),(-8.1,-2.5),(-4,8),(1,9),(6,8))):
 beam('Tree trunk',(x,y,0),(x+.15,y+.12,4.6),.20,bark)
 for k in range(8):
  a=k*math.tau/8;beam('Tree branch',(x,y,2.8+k*.12),(x+math.cos(a)*1.7,y+math.sin(a)*1.7,4.8),.045,bark)
 verts=[];faces=[];indices=[]
 for k in range(1600):
  a=rng.random()*math.tau;r=2.3*math.sqrt(rng.random());z=4.6+rng.uniform(-.8,1.3);c=Vector((x+math.cos(a)*r,y+math.sin(a)*r,z));length=rng.uniform(.08,.16);angle=rng.random()*math.tau;u=Vector((math.cos(angle)*length,math.sin(angle)*length,rng.uniform(-.04,.04)));v=Vector((-u.y*.4,u.x*.4,.015));index=len(verts);verts.extend((c-u,c-v,c+u,c+v));faces.append(tuple(range(index,index+4)));indices.append(rng.randrange(5))
 mesh=bpy.data.meshes.new('Leaf canopy');mesh.from_pydata(verts,[],faces)
 for m in leafmats:mesh.materials.append(m)
 for f,i in zip(mesh.polygons,indices):f.material_index=i
 o=bpy.data.objects.new('Tree canopy '+str(ti),mesh);bpy.context.collection.objects.link(o)
car_path=ROOT/'art/source/courtyard-props/sedan-textured/model.glb'
if car_path.exists():
 # Preserve the embedded glTF PBR images byte-for-byte; FBX loses unnamed image links.
 raw=car_path.read_bytes();length,kind=struct.unpack_from('<II',raw,12);doc=json.loads(raw[20:20+length]);binary_start=20+length+8
 tex=out/'car-textures';tex.mkdir(exist_ok=True);mat=doc['materials'][0];pbr=mat['pbrMetallicRoughness']
 for name,info in [('albedo',pbr['baseColorTexture']),('metallic-roughness',pbr['metallicRoughnessTexture']),('normal',mat['normalTexture'])]:
  image=doc['images'][doc['textures'][info['index']]['source']];view=doc['bufferViews'][image['bufferView']];offset=binary_start+view.get('byteOffset',0);(tex/(name+'.png')).write_bytes(raw[offset:offset+view['byteLength']])
 before=set(bpy.data.objects);bpy.ops.import_scene.gltf(filepath=str(car_path));parts=[o for o in set(bpy.data.objects)-before if o.type=='MESH'];points=[o.matrix_world@v.co for o in parts for v in o.data.vertices];lo=Vector(tuple(min(p[i] for p in points) for i in range(3)));hi=Vector(tuple(max(p[i] for p in points) for i in range(3)));parent=bpy.data.objects.new('Parked sedan',None);bpy.context.collection.objects.link(parent)
 for o in parts:
  world=o.matrix_world.copy();o.parent=parent;o.matrix_world=world;o.name='Sedan '+o.name
 parent.scale=Vector((1,1,1))*(4.4/max((hi-lo).x,(hi-lo).y));parent.rotation_euler.z=math.pi/2 if (hi-lo).x>(hi-lo).y else 0;parent.location=Vector((-8,1.5,0))-parent.rotation_euler.to_matrix()@Vector(((hi.x+lo.x)/2,(hi.y+lo.y)/2,lo.z))*parent.scale.x
scene=bpy.context.scene;scene.world=bpy.data.worlds.new('Daylight');scene.world.color=(.30,.33,.38);scene.view_settings.view_transform='AgX'
bpy.ops.object.light_add(type='AREA',location=(-3,-4,10));bpy.context.object.data.energy=2200;bpy.context.object.data.size=9
bpy.ops.object.camera_add(location=(12,-15,10));cam=bpy.context.object;cam.rotation_euler=(Vector((0,2,1.5))-cam.location).to_track_quat('-Z','Y').to_euler();cam.data.type='ORTHO';cam.data.ortho_scale=22;scene.camera=cam
scene.render.engine='BLENDER_EEVEE_NEXT';scene.eevee.taa_render_samples=32;scene.render.resolution_x=1400;scene.render.resolution_y=900;scene.render.resolution_percentage=100
bpy.ops.wm.save_as_mainfile(filepath=str(out/'courtyard.blend'))
bpy.ops.object.select_all(action='DESELECT')
for o in scene.objects:
 if o.type=='MESH':o.select_set(True)
bpy.ops.export_scene.fbx(filepath=str(unity/'courtyard.fbx'),use_selection=True,add_leaf_bones=False,bake_anim=False,path_mode='COPY',embed_textures=True)
scene.render.filepath=str(preview/'layout.png');bpy.ops.render.render(write_still=True)
(out/'brief.json').write_text(json.dumps(dict(reference='assets/references/photo-05.jpg',status='component draft; not runtime promoted',sourcePhotoUVs=True,clearFloorMeters=[11,4],hasGeneratedCar=car_path.exists(),dimensions='inferred, not measured'),indent=2))

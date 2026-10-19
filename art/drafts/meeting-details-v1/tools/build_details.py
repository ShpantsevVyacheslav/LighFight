"""Photo-authored meeting props. New derivative; original Tripo/base master immutable."""
import sys,json,math,struct,hashlib
from pathlib import Path
ROOT=Path(__file__).resolve().parents[4];OUT=ROOT/'art/drafts/meeting-details-v1';REF=ROOT/'art/reference/meeting-details-2026-10-19'
sys.path.insert(0,str(ROOT/'art/drafts/kitchen-env-lod-v1/tools'))
from glb_static import load,write,combine
M=[];B=[];names=[]
def material(name,c):M.append({'name':name,'pbrMetallicRoughness':{'baseColorFactor':[*c,1],'metallicFactor':0,'roughnessFactor':.7}});return len(M)-1
black=material('Black appliance and cables',(.018,.023,.025));silver=material('Brushed metal',(.42,.45,.47));coal=material('Glowing coals',(.5,.018,.003));M[coal]['emissiveFactor']=[.8,.025,.002]
yellow=material('Yellow ceramic',(.94,.65,.015));red=material('Red plastic',(.55,.012,.025));paper=material('Paper',(.83,.83,.77));grey=material('Slate grey beanbag',(.14,.16,.18));white=material('Network cabinet',(.7,.73,.73));blue=material('Cabinet blue trim',(.025,.12,.47));glass=material('Dark network glass',(.075,.13,.16));terracotta=material('Terracotta pot',(.46,.19,.09));leaf=material('Vine leaves',(.13,.29,.075));stem=material('Plant stems',(.2,.3,.1));rug=material('Reference textile',(.8,.8,.8))
def mesh(name,p,n,uv,idx,mat):B.append((name,p,n,uv,idx,mat))
def box(name,c,s,mat):
 p=[];n=[];uv=[];ix=[]
 for axis,sign,u in [(0,1,1),(0,-1,1),(1,1,0),(1,-1,0),(2,1,0),(2,-1,0)]:
  v=3-axis-u;normal=[0.,0.,0.];normal[axis]=sign;start=len(p);corn=[]
  for a,b in [(-1,-1),(1,-1),(1,1),(-1,1)]:
   q=list(c);q[axis]+=sign*s[axis]/2;q[u]+=a*s[u]/2;q[v]+=b*s[v]/2;corn.append(q)
  e=[corn[1][j]-corn[0][j] for j in range(3)];f=[corn[2][j]-corn[0][j] for j in range(3)];cross=[e[1]*f[2]-e[2]*f[1],e[2]*f[0]-e[0]*f[2],e[0]*f[1]-e[1]*f[0]];ids=[0,1,2,0,2,3] if sum(cross[j]*normal[j] for j in range(3))>0 else [0,2,1,0,3,2]
  p+=corn;n +=[normal]*4;uv +=[(0,0),(1,0),(1,1),(0,1)];ix +=[start+i for i in ids]
 mesh(name,p,n,uv,ix,mat)
def ell(name,c,s,mat,seg=20,rings=12):
 p=[];n=[];uv=[];ix=[]
 for j in range(rings+1):
  t=math.pi*j/rings
  for i in range(seg+1):
   a=2*math.pi*i/seg;d=(math.sin(t)*math.cos(a),math.cos(t),math.sin(t)*math.sin(a));p.append([c[k]+s[k]*d[k] for k in range(3)]);q=[d[k]/s[k] for k in range(3)];l=math.sqrt(sum(x*x for x in q));n.append([x/l for x in q]);uv.append((i/seg,1-j/rings))
 for j in range(rings):
  for i in range(seg):
   a=j*(seg+1)+i;b=a+seg+1
   if j>0:ix +=[a,a+1,b]
   if j<rings-1:ix +=[a+1,b+1,b]
 mesh(name,p,n,uv,ix,mat)
def cable(name,pts,r,mat):
 # Small overlapping rounded segments; no runtime physics.
 for j,(a,b) in enumerate(zip(pts,pts[1:])):
  d=[b[k]-a[k] for k in range(3)];steps=max(2,int(math.sqrt(sum(x*x for x in d))/r))
  for i in range(steps):ell(name+str(j)+'-'+str(i),[a[k]+d[k]*i/(steps-1) for k in range(3)],[r]*3,mat,6,4)
# Windows at +X. All props behind z=.55 fight lane.
box('Coal heater body',(3.2,1.06,1.04),(.17,.28,.19),black)
for z in [.97,1.04,1.11]:box('Hot charcoal',(3.2,1.22,z),(.085,.055,.05),coal)
for x in [3.13,3.27]:box('Heater silver lip',(x,1.22,1.04),(.045,.02,.22),silver)
box('Coal box',(3.21,1.12,1.63),(.15,.42,.2),black);box('Papers',(3.17,.94,1.4),(.19,.014,.26),paper)
cable('Power cord',[(3.2,.95,1.02),(3.08,.93,.75),(3.3,.89,.65)],.008,black)
for z in [1.22,1.28]:box('Metal tongs',(3.18,.95,z),(.06,.012,.26),silver)
ell('Yellow ceramic body',(3.18,1.04,3.12),(.09,.12,.08),yellow);ell('Yellow head',(3.18,1.18,3.12),(.085,.07,.075),yellow)
for z in [3.05,3.19]:ell('Ceramic ear',(3.18,1.26,z),(.025,.08,.025),yellow);ell('Black eye',(3.1,1.19,z),(.008,.014,.014),black)
ell('Red cup',(3.17,1.08,3.45),(.075,.15,.075),red)
for z in [2.91,3.7]:ell('Round tin',(3.16,.95,z),(.07,.022,.07),black)
# Cabinet on back wall; inferred placement adjacent to board, not metric photo reconstruction.
box('Network cabinet body',(.05,2.05,5.05),(1.04,.62,.24),white);box('Glass front',(.05,2.05,4.91),(.83,.49,.013),glass)
for x in [-.39,.49]:box('Blue cabinet trim',(x,2.05,4.895),(.025,.54,.02),blue)
box('Network switch',(.02,1.97,4.89),(.58,.055,.025),black);box('Router',(.05,2.38,5.04),(.39,.04,.15),black)
for x in [-.12,0,.17]:box('Router antenna',(x,2.5,5.04),(.017,.23,.017),black)
for j in range(5):cable('Patch cable',[(j*.09-.18,2.2,4.87),(j*.08-.15,2.07,4.85),(.22,1.86,4.86)],.006,white)
# Beanbag tucked in rear corner, outside combat lane.
ell('Beanbag base',(-2.75,.24,4.45),(.57,.24,.54),grey);ell('Beanbag back',(-2.95,.52,4.57),(.32,.43,.38),grey)
# Rug mounted behind furniture, front facing camera.
box('Wall rug', (1.78,1.82,5.12),(2.48,1.65,.018),rug)
# Table level estimated from current 1.4m furniture assembly; editable review props.
ell('Pot saucer',(.75,.79,2.82),(.16,.025,.16),paper);ell('Terracotta pot',(.75,.9,2.82),(.12,.13,.12),terracotta)
ell('Soil',(.75,1.035,2.82),(.103,.009,.103),black)
pts=[(.75,1.02,2.82),(.98,.8,2.82),(1.25,.79,2.85),(1.55,.79,2.89),(1.87,.79,2.86),(2.05,.78,2.92)]
cable('Vine',pts,.009,stem)
for j in range(14):
 x=.85+j*.084;z=2.84+(.08 if j%2 else -.08);ell('Vine leaf',(x,.81,z),(.075,.014,.04),leaf)
box('Black power strip',(-.2,.8,2.82),(.42,.045,.075),black)
for x in [-.33,-.2,-.07]:ell('Socket',(x,.826,2.82),(.035,.005,.025),glass)
cable('Table cable',[(-.4,.8,2.82),(-.65,.79,2.75),(-.8,.78,2.5)],.007,black)
# Put tabletop props on the actual long table, not the adjacent chair row.
for bi,b in enumerate(B):
 if b[0].startswith(('Pot saucer','Terracotta','Soil','Vine','Plant stems','Black power strip','Socket','Table cable')):
  name,p,n,uv,ix,m=b
  # authored old longitudinal X becomes room Z; old lateral Z becomes X
  pp=[(.25+(q[2]-2.82),q[1]+.25,2.2+(q[0]-.75)) for q in p]
  nn=[(q[2],q[1],q[0]) for q in n]
  # Reflection swaps axes; reverse winding.
  ii=[v for k in range(0,len(ix),3) for v in (ix[k],ix[k+2],ix[k+1])]
  B[bi]=(name,pp,nn,uv,ii,m)
# Batch authored props by material to avoid hundreds of mobile draw calls.
originalPartCount=len(B)
merged=[]
for m in range(len(M)):
 p=[];n=[];uv=[];ix=[]
 for name,pp,nn,uu,ii,mm in B:
  if mm!=m:continue
  offset=len(p);p+=pp;n+=nn;uv+=uu;ix +=[offset+i for i in ii]
 if p:merged.append((M[m]['name'],p,n,uv,ix,m))
B=merged
g={'asset':{'version':'2.0'},'bufferViews':[],'accessors':[],'materials':M,'meshes':[],'nodes':[],'scenes':[{'nodes':[]}],'scene':0};data=bytearray()
def acc(v,fmt,kind,target):
 flat=[x for row in v for x in row] if kind!='SCALAR' else v;data.extend(b'\0'*((-len(data))%4));raw=struct.pack('<'+str(len(flat))+fmt,*flat);idx=len(g['bufferViews']);g['bufferViews'].append({'buffer':0,'byteOffset':len(data),'byteLength':len(raw),'target':target});data.extend(raw);a={'bufferView':idx,'componentType':5126 if fmt=='f' else 5125,'count':len(v),'type':kind}
 if kind=='VEC3':a.update(min=[min(row[k] for row in v) for k in range(3)],max=[max(row[k] for row in v) for k in range(3)])
 g['accessors'].append(a);return len(g['accessors'])-1
for name,p,n,uv,ix,m in B:
 attrs={'POSITION':acc(p,'f','VEC3',34962),'NORMAL':acc(n,'f','VEC3',34962),'TEXCOORD_0':acc(uv,'f','VEC2',34962)};indices=acc(ix,'I','SCALAR',34963);g['meshes'].append({'name':name,'primitives':[{'attributes':attrs,'indices':indices,'material':m}]});g['nodes'].append({'name':name,'mesh':len(g['meshes'])-1});g['scenes'][0]['nodes'].append(len(g['nodes'])-1)
from PIL import Image
im=Image.open(REF/'codex-clipboard-6b06ad70-dbcf-4e91-b4eb-d3a2c63c3f43.jpg');w,h=im.size
quad=[(38,0),(47,1240),(1695,1160),(1780,155)]
quad=tuple(c*(w/1824 if i%2==0 else h/1368) for i,c in enumerate([n for p in quad for n in p]))
im=im.transform((1536,1024),Image.Transform.QUAD,quad,Image.Resampling.BICUBIC);im=im.rotate(180);
# Reconstruct only occluded textile border from the opposite intact border.
im.paste(im.crop((0,900,1536,1024)).transpose(Image.Transpose.FLIP_TOP_BOTTOM),(0,0));im.paste(im.crop((0,710,360,924)).transpose(Image.Transpose.FLIP_TOP_BOTTOM),(0,0));im.paste(im.crop((1330,650,1536,1024)).transpose(Image.Transpose.FLIP_TOP_BOTTOM),(1330,0));im.save(OUT/'rug-reference.png');payload=(OUT/'rug-reference.png').read_bytes();data.extend(b'\0'*((-len(data))%4));v=len(g['bufferViews']);g['bufferViews'].append({'buffer':0,'byteOffset':len(data),'byteLength':len(payload)});data.extend(payload);g['images']=[{'bufferView':v,'mimeType':'image/png'}];g['textures']=[{'source':0}];g['materials'][rug]['pbrMetallicRoughness'].update(baseColorFactor=[1,1,1,1],baseColorTexture={'index':0})
props=OUT/'meeting-props.glb';write(props,g,data);base=ROOT/'art/drafts/meeting-hybrid-v1/meeting-master.glb';digest=combine([(base,{'name':'Unchanged meeting hybrid base'}),(props,{'name':'Photo-authored new props'})],OUT/'meeting-master.glb')
assert all(min(p[2] for p in b[1])>.55 for b in B),'Fight lane intrusion'
receipt={'status':'BUILT_REQUIRES_RENDER_REVIEW','masterSHA256':digest,'baseSHA256':hashlib.sha256(base.read_bytes()).hexdigest(),'propTriangles':sum(len(b[4])//3 for b in B),'propMeshes':len(B),'authoredPartsBeforeBatching':originalPartCount,'fightLaneZ':[-.55,.55],'allPropsBeyondLane':True,'provenance':'Manual photo-authored props, not Tripo generation. Rug texture perspective-derived from supplied photo; no private document text copied. Placement and scale inferred.'};(OUT/'manifest.json').write_text(json.dumps(receipt,indent=2));print(json.dumps(receipt))

"""Review-only meeting room: immutable Tripo furniture + explicit photo-based architecture."""
import sys,json,struct,hashlib,copy
from pathlib import Path
ROOT=Path(__file__).resolve().parents[4]
sys.path.insert(0,str(ROOT/'art/drafts/kitchen-env-lod-v1/tools'))
from glb_static import load,write,combine,audit_images
OUT=ROOT/'art/drafts/meeting-hybrid-v1'
SOURCE=ROOT/'art/source/meeting-tripo-v1/original-v1.glb'
SHA='70336cf944c64d94ae0ec4b94a58efce0d13006a036bf276a67325bd417d2cfd'
materials=[('Warm plaster',[.68,.61,.48,1]),('Light oak',[.42,.31,.19,1]),('White frame',[.82,.83,.79,1]),('Window daylight',[.46,.63,.72,1]),('Radiator',[.64,.66,.64,1]),('Board',[.76,.79,.72,1]),('Ceiling grid',[.44,.44,.41,1]),('Ceiling panels',[.82,.81,.77,1]),('Right wall',[.68,.61,.48,1])]
batches=[{'p':[],'n':[],'uv':[],'i':[]} for _ in materials]
def box(center,size,mat):
 assert all(x>0 for x in size)
 batch=batches[mat]
 faces=[(0,1,2),(0,-1,2),(1,1,0),(1,-1,0),(2,1,0),(2,-1,0)]
 for axis,sign,u in faces:
  v=3-axis-u;normal=[0.,0.,0.];normal[axis]=float(sign)
  corners=[]
  for a,b in [(-1,-1),(1,-1),(1,1),(-1,1)]:
   p=list(center);p[axis]+=sign*size[axis]/2;p[u]+=a*size[u]/2;p[v]+=b*size[v]/2;corners.append(p)
  # Winding independently oriented by cross product against face normal.
  e1=[corners[1][j]-corners[0][j] for j in range(3)];e2=[corners[2][j]-corners[0][j] for j in range(3)]
  cross=[e1[1]*e2[2]-e1[2]*e2[1],e1[2]*e2[0]-e1[0]*e2[2],e1[0]*e2[1]-e1[1]*e2[0]]
  ids=[0,1,2,0,2,3] if sum(cross[j]*normal[j] for j in range(3))>0 else [0,2,1,0,3,2]
  start=len(batch['p']);batch['p'].extend(corners);batch['n'].extend([normal]*4);batch['uv'].extend([(0,0),(1,0),(1,1),(0,1)]);batch['i'].extend(start+i for i in ids)
def architecture(path):
 for batch in batches:
  for values in batch.values():values.clear()
 # Expanded game-space dimensions, NOT measurements inferred from one photograph.
 # Keep fight lane z[-.55,.55] clear; furniture starts at z=.92.
 box((0,-.045,1.7),(6.8,.09,7),1)
 box((0,1.5,5.24),(6.96,3,.12),0)
 # Inferred plain enclosing wall; hide only for explicit whole-room cutaway review.
 box((-3.46,1.5,1.7),(.12,3,7.1),8)
 # Photo-left wall (positive glTF X when viewed toward +Z) with two actual window openings; no concealed solid wall behind panes.
 windows=[(.42,1.92),(2.62,4.12)]
 edges=[-1.8,.42,1.92,2.62,4.12,5.3]
 for a,b in zip(edges,edges[1:]):
  gap=any(abs(a-x)<1e-8 and abs(b-y)<1e-8 for x,y in windows)
  if gap:
   box((3.46,.43,(a+b)/2),(.12,.86,b-a),0);box((3.46,2.7,(a+b)/2),(.12,.6,b-a),0)
  else:box((3.46,1.5,(a+b)/2),(.12,3,b-a),0)
 for a,b in windows:
  z=(a+b)/2
  box((3.43,1.63,z),(.025,1.46,b-a-.12),3)
  for h in [.9,1.72,2.4]:box((3.39,h,z),(.1,.055,b-a),2)
  for q in [a,b]:box((3.39,1.65,q),(.1,1.55,.065),2)
  box((3.27,.89,z),(.35,.07,b-a+.14),2)
  for j in range(12):box((3.31,.46,a+.09+j*(b-a-.18)/11),(.16,.58,.055),4)
 # Whiteboard and AC, matching photo-03 layout. No unreadable private text reproduced.
 box((-1.28,1.7,5.13),(1.22,1.4,.055),5)
 for x in [-.65,-1.91]:box((x,1.7,5.09),(.035,1.46,.035),4)
 for y in [.98,2.42]:box((-1.28,y,5.09),(1.29,.035,.035),4)
 box((3.26,2.65,2.25),(.31,.34,.83),2)
 # Full ceiling is separate so review whole-room shots can explicitly hide it as a cutaway.
 box((0,3.065,1.7),(6.8,.06,7),7)
 # Grid remains visible below the panels in the actual interior view.
 for j in range(9):box((0,3.015,-1.8+j*.875),(6.8,.035,.025),6)
 for j in range(9):box((-3.4+j*.85,3.015,1.7),(.025,.035,7),6)
 box((.6,3,2.9),(1.1,.025,.55),2)
 g={'asset':{'version':'2.0'},'bufferViews':[],'accessors':[],'materials':[],'meshes':[],'nodes':[],'scenes':[{'nodes':[]}],'scene':0};data=bytearray()
 for name,color in materials:g['materials'].append({'name':name,'pbrMetallicRoughness':{'baseColorFactor':color,'metallicFactor':0,'roughnessFactor':.8}})
 def accessor(values,fmt,kind,target):
  flat=[n for row in values for n in row] if kind!='SCALAR' else values
  data.extend(b'\0'*((-len(data))%4));v=len(g['bufferViews']);raw=struct.pack('<'+str(len(flat))+fmt,*flat)
  g['bufferViews'].append({'buffer':0,'byteOffset':len(data),'byteLength':len(raw),'target':target});data.extend(raw)
  a={'bufferView':v,'componentType':5126 if fmt=='f' else 5125,'count':len(values),'type':kind}
  if kind=='VEC3':a.update(min=[min(row[j] for row in values) for j in range(3)],max=[max(row[j] for row in values) for j in range(3)])
  g['accessors'].append(a);return len(g['accessors'])-1
 for m,b in enumerate(batches):
  if not b['p']:continue
  attrs={'POSITION':accessor(b['p'],'f','VEC3',34962),'NORMAL':accessor(b['n'],'f','VEC3',34962),'TEXCOORD_0':accessor(b['uv'],'f','VEC2',34962)};idx=accessor(b['i'],'I','SCALAR',34963)
  g['meshes'].append({'name':materials[m][0],'primitives':[{'attributes':attrs,'indices':idx,'material':m}]});g['nodes'].append({'name':materials[m][0],'mesh':len(g['meshes'])-1});g['scenes'][0]['nodes'].append(len(g['nodes'])-1)
 texture=OUT/'textures/reference-derived-floor-v1.png'
 assert texture.exists(),'Missing generated reference-derived floor asset'
 payload=texture.read_bytes();data.extend(b'\0'*((-len(data))%4));view=len(g['bufferViews']);g['bufferViews'].append({'buffer':0,'byteOffset':len(data),'byteLength':len(payload)});data.extend(payload)
 g['images']=[{'name':'Photo03-derived floor basecolor','bufferView':view,'mimeType':'image/png'}];g['textures']=[{'source':0}]
 g['materials'][1]['pbrMetallicRoughness'].update(baseColorFactor=[1,1,1,1],baseColorTexture={'index':0})
 return write(path,g,data)
def main():
 OUT.mkdir(parents=True,exist_ok=True);before=hashlib.sha256(SOURCE.read_bytes()).hexdigest();assert before==SHA
 sourceGraph,_=load(SOURCE)
 assert len(sourceGraph['nodes'])==1 and set(sourceGraph['nodes'][0]).issubset({'name','mesh'}),'Furniture transform assumptions changed'
 arch=OUT/'meeting-architecture.glb';architecture(arch)
 destination=OUT/'meeting-master.glb'
 # Tripo glTF Y-up bounding box height .37648 ->1.40m. Uniform transform only.
 scale=1.4/.3764801025390625
 wrapper={'name':'ROLE_meeting-furniture','scale':[scale]*3,'translation':[0,0,3]}
 digest=combine([(SOURCE,wrapper),(arch,{'name':'ROLE_photo-based-architecture'})],destination)
 original,raw=load(SOURCE);assembled,combined=load(destination)
 assert combined[:len(raw)]==raw,'Source binary payload changed'
 originalImages=audit_images(SOURCE);assert audit_images(destination)[:len(originalImages)]==originalImages,'Original texture payload altered'
 assert hashlib.sha256(SOURCE.read_bytes()).hexdigest()==before
 tris=sum(assembled['accessors'][p['indices']]['count']//3 for m in assembled['meshes'] for p in m['primitives'])
 manifest={'status':'INTERIOR_SHELL_CHANGED_REQUIRES_FRESH_ACTUAL_RENDERS','sourcePhoto':'assets/references/photo-03.jpg','sourceTripo':str(SOURCE.relative_to(ROOT)),'sourceSHA256':before,'sourceBinaryPrefixExact':True,'originalImagesExact':True,'masterSHA256':digest,'trianglesIncludingArchitecture':tris,'furnitureWrapper':wrapper,'roomDimensionsGameMetres':[6.8,3,7],'dimensionsScope':'Unmeasured expanded game space; not metric reconstruction from photograph','fightLane':{'x':[-2.7,2.7],'z':[-.55,.55]},'sourceFurnitureZMinGameMetres':3-.4897918701171875*scale,'referenceFacing':'Front camera looks along +glTF Z: photo-left windows at +X, board at -X',
 'architecture':'Manually authored from photo: two left windows/radiators, warm wall, board, ceiling grid and separate opaque ceiling. Furniture supplied by Tripo. Plain right wall is inferred; front is open. Whole review hides right wall/ceiling explicitly.','pending':['actual GLB import/raster/collision/layout review','measured static environment budget','Unity prefab/camera/shadow/profile acceptance']}
 assert manifest['sourceFurnitureZMinGameMetres']>.55
 (OUT/'manifest.json').write_text(json.dumps(manifest,indent=2));print(json.dumps(manifest,indent=2))
if __name__=='__main__':main()

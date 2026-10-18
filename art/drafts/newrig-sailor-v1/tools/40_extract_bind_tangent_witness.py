"""Read-only bounded original-dense bind tangent-space witness; no render/export/source save."""
import bpy,json,hashlib
from pathlib import Path
import numpy as np
O=Path(__file__).resolve().parents[1];P=O/'dense-def-fbx-packet-v1';source=O/'stage02-bound-original-dense.blend';sha=hashlib.sha256(source.read_bytes()).hexdigest();bpy.ops.wm.open_mainfile(filepath=str(source));body=bpy.data.objects['Sailor_OriginalDense_UNTOUCHED'];m=body.data;assert len(m.vertices)==994452;m.calc_loop_triangles();assert len(m.loop_triangles)==1988914
s=(O/'tools/01_audit_fit.py').read_text();exec(s[s.index('def hash_arrays'):s.index('def write')]);before=hash_arrays(body);expected=json.loads((O/'stage02.json').read_text())['sourceAttributesSHA256'];assert before==expected
v=np.empty(len(m.vertices)*3,np.float32);m.vertices.foreach_get('co',v);v=v.reshape(-1,3);mw=np.array(body.matrix_world);world=v@mw[:3,:3].T+mw[:3,3];lo=world.min(0);hi=world.max(0);height=hi[2]-lo[2];center=(lo[0]+hi[0])*.5;loopVertex=np.empty(len(m.loops),np.int32);m.loops.foreach_get('vertex_index',loopVertex);uvname=m.uv_layers.active.name;m.calc_tangents(uvmap=uvname)
regions={'collar':(world[:,2]>lo[2]+.78*height)&(world[:,2]<lo[2]+.9*height)&(np.abs(world[:,0]-center)<.17*height),'shirt':(world[:,2]>lo[2]+.52*height)&(world[:,2]<lo[2]+.78*height)&(np.abs(world[:,0]-center)<.35*height),'hair':world[:,2]>lo[2]+.93*height};rows=[]
for region,count in [('collar',86),('shirt',85),('hair',85)]:
 candidates=np.flatnonzero(regions[region][loopVertex]);assert len(candidates)>=count
 for i in candidates[np.linspace(0,len(candidates)-1,count,dtype=int)]:
  loop=m.loops[int(i)];n=np.array(loop.normal);t=np.array(loop.tangent);sign=float(loop.bitangent_sign);b=np.cross(n,t)*sign;assert np.isfinite(np.r_[n,t,b,sign]).all();rows.append({'region':region,'loopIndex':int(i),'vertexIndex':int(loop.vertex_index),'originalTriangleIndex':int(i)//3,'triangleCorner':int(i)%3,'positionLocal':v[loop.vertex_index].tolist(),'uv':list(m.uv_layers.active.data[int(i)].uv),'normalLocal':n.tolist(),'tangentLocal':t.tolist(),'bitangentSign':sign,'bitangentLocal':b.tolist(),'normalLength':float(np.linalg.norm(n)),'tangentLength':float(np.linalg.norm(t)),'normalTangentDot':float(n.dot(t))})
assert hash_arrays(body)==before;assert hashlib.sha256(source.read_bytes()).hexdigest()==sha
materials=[]
for mat in body.data.materials:
 nodes=[]
 for node in mat.node_tree.nodes:
  item={'name':node.name,'type':node.type}
  if node.type=='BSDF_PRINCIPLED':
   item['inputs']={}
   for name in ['Base Color','Metallic','Roughness','IOR','Specular IOR Level','Alpha','Normal']:
    socket=node.inputs.get(name)
    if socket is None:continue
    value=socket.default_value;item['inputs'][name]={'default':float(value) if isinstance(value,(int,float)) else list(value),'links':[{'node':link.from_node.name,'socket':link.from_socket.name} for link in socket.links]}
  if node.type=='NORMAL_MAP':item.update({'space':node.space,'UVmap':node.uv_map,'strength':float(node.inputs['Strength'].default_value)})
  if node.type=='TEX_IMAGE' and node.image:
   image=node.image;blob=bytes(image.packed_file.data) if image.packed_file else Path(bpy.path.abspath(image.filepath)).read_bytes();item.update({'image':image.name,'filepath':image.filepath,'SHA256':hashlib.sha256(blob).hexdigest(),'dimensions':list(image.size),'colorSpace':image.colorspace_settings.name,'alphaMode':image.alpha_mode})
  nodes.append(item)
 materials.append({'name':mat.name,'diffuseRGBA':list(mat.diffuse_color),'nodes':nodes})
report={'status':'READ_ONLY_BIND_TANGENT_WITNESS','sourceBlendSHA256':sha,'originalVertices':994452,'originalTriangles':1988914,'UVmap':uvname,'sourceBlend':str(source),'method':'Read-only mesh.calc_tangents on original bind geometry','meshWorld':mw.tolist(),'space':'Mesh-local ORIGINAL bind geometry, never evaluated animated vertices. B=cross(N,T)*sign; transform actual normals by inverse-transpose. Unity FBX handedness C=ReflectX*B requires explicit independent frame conversion, not a guessed map flip.','hasCustomNormals':bool(m.has_custom_normals),'sourceGeometryAndUVExact':True,'materials':materials,'count':len(rows),'regionMasks':'Normalized bind-height spatial bins; diagnostic labels, not semantic garment segmentation','samples':rows};(P/'original-bind-tangent-witness.json').write_text(json.dumps(report,indent=2));print('RESULT '+json.dumps({k:v for k,v in report.items() if k!='samples'}),flush=True)

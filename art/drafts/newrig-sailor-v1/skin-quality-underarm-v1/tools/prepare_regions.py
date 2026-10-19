"""Propose geometric diagnostic envelopes; never infer a verified cloth boundary.
Pure NumPy, read-only original sidecar; all outputs fenced to this owned review.
"""
from pathlib import Path
import argparse,hashlib,json
import numpy as np
OWN=Path(__file__).resolve().parents[1];PILOT=OWN.parent;PACKET=PILOT/'dense-def-fbx-packet-v1'
def sha(path):
 h=hashlib.sha256()
 with Path(path).open('rb') as f:
  for block in iter(lambda:f.read(4*1024*1024),b''):h.update(block)
 return h.hexdigest()
def owned_output(path):
 p=Path(path).resolve()
 if not p.is_relative_to(OWN.resolve()):raise ValueError('Output must remain under skin-quality-underarm-v1')
 p.mkdir(parents=True,exist_ok=True);return p

def unit(v):return v/np.linalg.norm(v)
def capsule(points,a,b):
 v=b-a;t=(points-a)@v/(v@v);nearest=a+np.clip(t,0,1)[:,None]*v;return t,np.linalg.norm(points-nearest,axis=1)
def propose(packet=PACKET,output=OWN/'regions-v1'):
 packet=Path(packet);out=owned_output(output);source=packet/'original-indexed-attributes.npz';data=np.load(source);raw=data['positionsBlenderLocal'];mw=data['meshWorldBlender'];points=raw@mw[:3,:3].T+mw[:3,3];names=json.loads((packet/'manifest.json').read_text())['boneNames'];W=data['armatureWorldBlender']@data['DEFRestArmature'];bone={n:W[i,:3,3] for i,n in enumerate(names)};H=float(np.ptp(points[:,2]));assert len(points)==994452 and len(data['triangles'])==1988914
 neck=bone['DEF-spine.004'];chest=bone['DEF-spine.003'];left=bone['DEF-upper_arm.L'];right=bone['DEF-upper_arm.R'];up=unit(neck-chest);lateral=unit(left-right);forward=unit(np.cross(lateral,up));forward*=1 if forward@np.array([0,-1,0])>=0 else -1
 rel=points-neck;x=rel@lateral;z=rel@up;depth=rel@forward;regions={};config={'HworldM':H,'neckWorld':neck.tolist(),'chestUp':up.tolist(),'lateralL':lateral.tolist(),'forward':forward.tolist(),'units':'Blender world metres','selection':'bone-derived geometry envelopes, NOT manually verified garment boundaries'}
 regions['head_anchor_proposal']=z>.025*H
 regions['upper_neck_anchor_proposal']=(z>.008*H)&(z<.035*H)&(np.abs(x)<.028*H)&(np.abs(depth)<.045*H)
 regions['collar_proposal']=(z>-.045*H)&(z<.015*H)&(np.abs(x)<.115*H)&(np.abs(depth)<.10*H)
 regions['central_chest_measure']=(z>-.19*H)&(z<-.06*H)&(np.abs(x)<.055*H)&(np.abs(depth)<.10*H)
 for side,sign in [('L',1),('R',-1)]:
  shoulder=bone['DEF-upper_arm.'+side];elbow=bone['DEF-forearm.'+side];wrist=bone['DEF-hand.'+side];t,r=capsule(points,shoulder,elbow);tf,rf=capsule(points,elbow,wrist);sc=(shoulder-neck)@lateral*sign;sx=x*sign;sz=(points-shoulder)@up
  regions['sleeve_armhole_'+side]=(t>=-.20)&(t<=.45)&(r<.082*H)&(sx>sc*.40)
  regions['lateral_deltoid_'+side]=(np.linalg.norm(points-shoulder,axis=1)<.10*H)&(sx>sc*.85)
  regions['medial_trapezius_'+side]=(sx>.025*H)&(sx<sc*.92)&(z>-.085*H)&(z<.018*H)&(np.abs(depth)<.10*H)
  regions['torso_underarm_'+side]=(sx>sc*.30)&(sx<sc*.93)&(sz>-.17*H)&(sz<-.015*H)&(np.abs(depth)<.11*H)
  regions['elbow_distal_anchor_'+side]=((t>.75)&(t<1.1)&(r<.055*H))|((tf>=0)&(tf<=1.2)&(rf<.06*H))
 ids={n:np.flatnonzero(mask).astype(np.int32) for n,mask in regions.items()};dest=out/'region-proposals.npz';np.savez_compressed(dest,**ids)
 manifest={'status':'PROVISIONAL_ENVELOPES_REQUIRE_VISUAL_REVIEW','sourceIndexedNPZ':str(source.resolve()),'sourceIndexedSHA256':sha(source),'sourceVertices':994452,'sourceTriangles':1988914,'sourceAttributes':json.loads((PILOT/'stage02.json').read_text())['sourceAttributesSHA256'],'selectionConfig':config,'maskNPZ':dest.name,'maskSHA256':sha(dest),'regions':{n:{'count':len(i),'worldBoundsMin':points[i].min(0).tolist() if len(i) else None,'worldBoundsMax':points[i].max(0).tolist() if len(i) else None} for n,i in ids.items()},'reviewed':False,'limitations':['No weights were used to infer leakage.','Envelopes may overlap and may include torso/sleeve together; inspect mask renders before anatomical interpretation.','Collar proposal is not a proven clothing seam or an automatic pinned region.','No vertex/weight/material/source-file mutation.']};(out/'region-proposals.json').write_text(json.dumps(manifest,indent=2)+'\n');return manifest
if __name__=='__main__':
 p=argparse.ArgumentParser();p.add_argument('--packet',type=Path,default=PACKET);p.add_argument('--output',type=Path,default=OWN/'regions-v1');a=p.parse_args();r=propose(a.packet,a.output);print('RESULT '+json.dumps({'status':r['status'],'regions':{n:v['count'] for n,v in r['regions'].items()}}))

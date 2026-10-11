"""Join verified exports of the SAME web rig; never retarget another skeleton."""
import copy, json, struct, tempfile
from pathlib import Path
from combine_meshy_web_glb import read, combine, ROOT

def write_glb(path, doc, binary):
    encoded=json.dumps(doc,separators=(',',':')).encode();encoded+=b' '*(-len(encoded)%4)
    binary+=b'\0'*(-len(binary)%4)
    path.write_bytes(struct.pack('<4sII',b'glTF',2,28+len(encoded)+len(binary))+struct.pack('<II',len(encoded),0x4e4f534a)+encoded+struct.pack('<II',len(binary),0x004e4942)+binary)

def build():
    folder=ROOT/'art/source/original-island'
    rig,_=read(folder/'web-rig.glb'); official,binary=read(folder/'web-official-all.glb')
    # Only the provider's known export aliases, with an explicit rest/hierarchy check.
    aliases={'target_character':'SmartRigArmature'}
    canonical=lambda name:aliases.get(name,name.replace('mixamorig:','smartrig:'))
    names={n['name']:i for i,n in enumerate(rig['nodes'])}
    mapping={i:names[canonical(n['name'])] for i,n in enumerate(official['nodes'])}
    max_error=0
    for i,j in mapping.items():
        n=official['nodes'][i]; ref=rig['nodes'][j]
        if {mapping[c] for c in n.get('children',[])}!=set(ref.get('children',[])):raise ValueError('Different skeleton hierarchy')
        for key,default in [('translation',[0,0,0]),('rotation',[0,0,0,1]),('scale',[1,1,1])]:
            error=max(abs(a-b) for a,b in zip(n.get(key,default),ref.get(key,default)));max_error=max(max_error,error)
            if error>1e-5:raise ValueError('Different rest pose: '+n['name'])
            if key in ref:n[key]=copy.deepcopy(ref[key])
            else:n.pop(key,None)
        if n.get('matrix')!=ref.get('matrix'):raise ValueError('Different rest matrix')
        n['name']=ref['name']
    motions=[]
    with tempfile.TemporaryDirectory() as temp:
        for animation in official['animations']:
            d=copy.deepcopy(official);d['animations']=[animation];p=Path(temp)/(animation['name']+'.glb');write_glb(p,d,binary);motions.append((animation['name'],p))
        extras={'Combat_Stance':'web-combat-stance.glb','Boxing_Guard':'web-guard.glb',
                'Stand_Up1':'web-stand-up.glb','Elbow_Strike':'web-elbow.glb',
                'Roundhouse_Kick':'web-roundhouse.glb','Counterstrike':'web-counter.glb'}
        for name,file in extras.items():motions.append((name,folder/file))
        for file in sorted((folder/'web-extra').glob('*.glb')):
            motions.append((file.stem,file))
        report=combine(folder/'web-rig.glb',motions,folder/'web-native-pack.glb')
    report['exportRestRoundingMax']=max_error;report['webRigTask']='01a12b74-8ced-76eb-ac89-a69f15d9f03e'
    (folder/'web-native-pack-audit.json').write_text(json.dumps(report,indent=2));print(report)
if __name__=='__main__':build()

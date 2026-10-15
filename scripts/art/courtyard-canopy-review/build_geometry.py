"""Draft only: deterministic alpha-card canopy; no Unity/catalog/source edits."""
from pathlib import Path
import hashlib,json,math,random
ROOT=Path(__file__).resolve().parents[3]
OUT=ROOT/'art/generated/courtyard-foliage'

def build():
    rng=random.Random(505031)
    trees=[(-10,4.5,4.3),(-10,5.2,5.1),(8,5.8,4.8),(8,6.4,5.4),(-7,8,5.4),(-3.5,9,5.8),(0,9.5,5.5),(3.5,9,5.9),(7,9,5.6)]
    verts=[];uvs=[];faces=[];cards=[]
    for tree,(tx,tz,ty) in enumerate(trees):
        for branch in range(28):
            angle=rng.random()*math.tau;radius=math.sqrt(rng.random())*1.9
            center=(tx+math.cos(angle)*radius,ty+rng.uniform(-.45,1.0),tz+math.sin(angle)*radius)
            width=rng.uniform(1.1,1.9);height=rng.uniform(1.2,2.0);yaw=rng.random()*math.tau
            tile=(tree+branch)%4;col=tile%2;row=tile//2
            # PNG top-row atlas islands become V>.5; preserve straight-alpha image bytes.
            u0,u1=col*.5+.001,(col+1)*.5-.001;v0,v1=.5-row*.5+.001,1-row*.5-.001
            for cross in range(2):
                a=yaw+cross*math.pi*.5
                right=(math.cos(a),0,math.sin(a));first=len(verts)
                quad=[]
                for sx,sy in [(-.5,-.5),(.5,-.5),(.5,.5),(-.5,.5)]:
                    p=(center[0]+right[0]*sx*width,center[1]+sy*height,center[2]+right[2]*sx*width)
                    verts.append(p);quad.append(p)
                uvs.extend([(u0,v0),(u1,v0),(u1,v1),(u0,v1)])
                faces.extend([(first,first+1,first+2),(first,first+2,first+3)])
                cards.append({'tree':tree,'branch':branch,'cross':cross,'tile':tile,'firstVertex':first})
    # Validate each individual quad; a combined renderer bounds check alone can hide crossings.
    for card in cards:
        q=verts[card['firstVertex']:card['firstVertex']+4]
        mins=[min(p[i] for p in q) for i in range(3)];maxs=[max(p[i] for p in q) for i in range(3)]
        if maxs[0]>-5.5 and mins[0]<5.5 and maxs[2]>-2 and mins[2]<2 and maxs[1]>.05 and mins[1]<2.85:
            raise ValueError(('fight-volume intrusion',card,mins,maxs))
    return trees,verts,uvs,faces,cards

def main():
    trees,verts,uvs,faces,cards=build();OUT.mkdir(parents=True,exist_ok=True)
    (OUT/'canopy-cards-v1.json').write_text(json.dumps({'vertices':[dict(zip(('x','y','z'),v)) for v in verts],'uv':[dict(zip(('x','y'),v)) for v in uvs],'triangles':[i for f in faces for i in f]},separators=(',',':')))
    obj=OUT/'canopy-cards-v1.obj'
    with obj.open('w') as f:
        f.write('# Unity Y-up world meters; draft static card mesh; no colliders\no CourtyardCanopyCards\n')
        for v in verts:f.write('v '+' '.join(f'{a:.7f}' for a in v)+'\n')
        for uv in uvs:f.write('vt '+' '.join(f'{a:.7f}' for a in uv)+'\n')
        for face in faces:f.write('f '+' '.join(f'{i+1}/{i+1}' for i in face)+'\n')
    report={'draftOnly':True,'trees':trees,'vertices':len(verts),'triangles':len(faces),'quads':len(cards),'allIndividualQuadsClearOfFightVolume':True,'fightVolume':{'x':[-5.5,5.5],'y':[.05,2.85],'z':[-2,2]},'bounds':{'min':[min(v[i] for v in verts) for i in range(3)],'max':[max(v[i] for v in verts) for i in range(3)]},'objSHA256':hashlib.sha256(obj.read_bytes()).hexdigest(),'atlas':'willow-branch-atlas-v2.png','pending':['Unity canonical JSON vertex/UV parity and atlas orientation proof','cutout edge/shadow/temporal alias review','same-camera source-photo composition comparison','actual 11x4 movement/attack clearance','device overdraw/frame budget'],'catalogPromotion':False}
    (OUT/'geometry-review-v1.json').write_text(json.dumps(report,indent=2));print(json.dumps(report,indent=2))
if __name__=='__main__':main()

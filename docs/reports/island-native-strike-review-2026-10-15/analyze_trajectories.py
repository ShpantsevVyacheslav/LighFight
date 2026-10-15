"""Candidate discovery ONLY. No runtime mapping generated; all gates remain pending."""
import csv,json,math,argparse,pathlib,statistics
p=argparse.ArgumentParser();p.add_argument('evidence',type=pathlib.Path);a=p.parse_args();results=[]
def canonical(n):return n.lower().replace('mixamorig:','').replace('_','').replace(' ','')
for name in ['box_01','box_02','front_kick_01']:
 frames={}
 with (a.evidence/(name+'.trajectory.csv')).open() as f:
  for r in csv.DictReader(f):frames.setdefault(float(r['seconds']),{})[canonical(r['joint'])]=tuple(float(r[k]) for k in ['x','y','z'])
 times=sorted(frames);joints=sorted(frames[times[0]])
 def find(suffix):
  exact=[n for n in joints if n==suffix]
  if len(exact)==1:return exact[0]
  x=[n for n in joints if n.endswith(suffix)]
  return x[0] if len(x)==1 else None
 center=next((find(n) for n in ['spine2','chest','spine1','hips'] if find(n)),None)
 report={'take':name,'sampleCount':len(times),'jointNames':joints,'center':center,'events':[],'accepted':False}
 if not center:report['blocked']='No unambiguous torso joint';results.append(report);continue
 for suffix in (['lefthand','righthand'] if name.startswith('box') else ['leftfoot','rightfoot']):
  joint=find(suffix)
  if not joint:continue
  distances=[math.dist(frames[t][joint],frames[t][center]) for t in times]
  # Local maxima separated by >=100ms; raw trajectory retained, no source curves altered.
  peaks=[i for i in range(2,len(times)-2) if distances[i]>distances[i-1] and distances[i]>=distances[i+1] and distances[i]-min(distances)>0.04]
  kept=[]
  for i in sorted(peaks,key=lambda k:distances[k],reverse=True):
   if all(abs(times[i]-times[j])>=.10 for j in kept):kept.append(i)
  for i in sorted(kept):
   left=max([k for k in range(i) if distances[k]<=distances[k+1] and (k==0 or distances[k]<=distances[k-1])],default=0)
   right=min([k for k in range(i+1,len(times)) if distances[k]<=distances[k-1] and (k==len(times)-1 or distances[k]<=distances[k+1])],default=len(times)-1)
   endpoint_error=math.dist(frames[times[left]][joint],frames[times[right]][joint]);amp=distances[i]-max(distances[left],distances[right])
   report['events'].append({'effector':joint,'startSeconds':times[left],'extensionPeakSeconds':times[i],'recoverySeconds':times[right],'extensionAmplitude':amp,'effectorReturnError':endpoint_error,'warning':'Geometric extension candidate, NOT proven surface contact or single directed strike. Boundary extrema may truncate adjacent strikes. Requires full-vector, clothing, feet, source hand closure and actual target entry/return review.'})
 # Quantify all actual available source finger-to-hand distances, no assumed fist manipulation.
 report['fingerClosureDistances']={}
 for side in ['left','right']:
  palm=find(side+'hand'); tips=[j for j in joints if side in j and any(s in j for s in ['index','middle','ring','pinky','little','thumb']) and ('end' in j or 'distal' in j or j.endswith('3'))]
  if palm:report['fingerClosureDistances'][side]={j:{'min':min(math.dist(frames[t][j],frames[t][palm]) for t in times),'max':max(math.dist(frames[t][j],frames[t][palm]) for t in times)} for j in tips}
 results.append(report)
(a.evidence/'candidate-windows.UNACCEPTED.json').write_text(json.dumps(results,indent=2));print(json.dumps([{'take':r['take'],'samples':r['sampleCount'],'extensionCandidates':len(r['events']),'accepted':False} for r in results],indent=2))

"""Synthetic transforms/mutations of frozen Blender input: NOT actual Unity parity."""
import copy,importlib.util,json,pathlib,tempfile,numpy as np
root=pathlib.Path(__file__).resolve().parent
spec=importlib.util.spec_from_file_location('gate',root/'compare_independent_trajectories.py');m=importlib.util.module_from_spec(spec);spec.loader.exec_module(m)
source={n:m.load(root.parents[1]/'island-source-bone-review'/(n+'.trajectory.csv')) for n in m.NAMES}
R=np.array([[0,0,-1],[1,0,0],[0,1,0.]]) # reflected orthogonal basis
unity={n:{t:{j:1.25*v@R+np.array([.2,-.3,.4]) for j,v in frame.items()} for t,frame in frames.items()} for n,frames in source.items()}
passed=[]
r=m.compare(source,unity);assert r['passed'] and r['holdoutPhases']==1800 and r['fitCount']==1;passed.append('ONE reflected uniform phase0 fit passes all1800synthetic holdout phases')
bad=copy.deepcopy(unity);t=sorted(bad['box_02'])[400];bad['box_02'][t]['lefthand']+=np.array([.001,0,0]);r=m.compare(source,bad);assert not r['passed'];passed.append('single holdout hand1mm mutation fails')
bad=copy.deepcopy(unity)
for n,frames in bad.items():
 for t,frame in frames.items():
  for j,v in frame.items():frame[j]=v+np.array([t*.002,0,0])
assert not m.compare(source,bad)['passed'];passed.append('perphase drift fails without perframe refit')
bad=copy.deepcopy(unity);t=sorted(bad['box_01'])[200];bad['box_01'][t+.00002]=bad['box_01'].pop(t)
try:m.compare(source,bad);raise AssertionError('expected time gate')
except ValueError as e:assert 'Exacttime' in str(e)
passed.append('20microsecond timing mismatch rejected')
with tempfile.TemporaryDirectory() as tmp:
 p=pathlib.Path(tmp)/'ambiguous.csv';p.write_text('seconds,joint,x,y,z\n0,Left_Hand,0,0,0\n0,LeftHand,0,0,0\n')
 try:m.load(p);raise AssertionError('expected ambiguity gate')
 except ValueError:pass
passed.append('ambiguous canonical joint rejected')
(root/'independent-trajectory-offline-tests.json').write_text(json.dumps({'status':'SYNTHETIC_GATE_TESTS_ONLY_NO_UNITY_PARITY_CLAIM','passed':passed,'count':len(passed),'actualUnityMeasured':False},indent=2));print('PASS',len(passed),'synthetic gates')

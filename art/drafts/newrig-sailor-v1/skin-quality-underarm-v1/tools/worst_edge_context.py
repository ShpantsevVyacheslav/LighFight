"""Read-only rest-scale context for worst strain edges; no threshold waiver."""
from pathlib import Path
import json,numpy as np
O=Path(__file__).resolve().parents[1];P=O.parent/'dense-def-fbx-packet-v1';a=np.load(P/'original-indexed-attributes.npz');mw=a['meshWorldBlender'];p=a['positionsBlenderLocal']@mw[:3,:3].T+mw[:3,3];names=json.loads((P/'manifest.json').read_text())['boneNames'];records=[];joint=a['jointIndices'];weights=a['weights']
for folder in ['metrics-clay-v1','metrics-left-selected-v1']:
 r=json.loads((O/folder/'report.json').read_text())
 for c in r['cases']:
  for variant in ['OFF','ON']:
   for region,data in c[variant]['regions'].items():
    for ids,ratio in zip(data['edge']['worstEdgesVertexPairs'],data['edge']['worstRatios']):
     i,j=ids;length=float(np.linalg.norm(p[j]-p[i]));records.append({'case':c['case']['name'],'variant':variant,'region':region,'originalVertexIDs':ids,'restLengthM':length,'posedLengthM':length*ratio,'ratio':ratio,'sourceNamedWeights':[[[names[int(k)],float(w)] for k,w in zip(joint[v],weights[v]) if w>0] for v in ids]})
(O/'evidence/worst-edges-source-context.json').write_text(json.dumps({'status':'SCALE_CONTEXT_NOT_QUALITY_WAIVER','sourceIndexedNPZ':str(P/'original-indexed-attributes.npz'),'records':records},indent=2)+'\n');print('records',len(records))

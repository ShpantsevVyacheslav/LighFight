from pathlib import Path
import json,hashlib,csv,shutil
root=Path('unity');out=Path('.tools/tripo-humanoid-review-unity/ProductionSelectedContentAudit');version='selected-quality-20261014-2';delivery=Path('.tools/content/StandaloneOSX')/version
layout=max((root/'Library/com.unity.addressables/BuildReports').glob('*.json'),key=lambda p:p.stat().st_mtime);j=json.loads(layout.read_text());assert j['PlayerBuildVersion']==version and not j['BuildError'];refs={v['rid']:v for v in j['references']['RefIds']};data=lambda rid:refs[rid]['data']
groups={data(g['rid'])['Name']:data(g['rid']) for g in j['Groups']};bundles={v['rid']:data(v['rid']) for g in groups.values() for v in g['Bundles']};bundles.update({v['rid']:data(v['rid']) for v in j['BuiltInBundles']})
assets={v['rid']:v['data'] for v in refs.values() if v['type']['class'].endswith('/ExplicitAsset')};assetFile={a['rid']:f['rid'] for f in refs.values() if f['type']['class'].endswith('/File') for a in f['data']['Assets']}
pathsByBundle={rid:[a['AssetPath'] for aid,a in assets.items() if data(assetFile[aid])['Bundle']['rid']==rid] for rid in bundles}
fg=groups['LighFight Fighters'];ag=groups['LighFight Arenas'];mg=groups['LighFight Remote Starter'];sg=groups['LighFight Shared Motions'];assert len(fg['Bundles'])==8 and len(ag['Bundles'])==5 and len(sg['Bundles'])==1 and len(mg['Bundles'])==1
fighterIds={v['rid'] for v in fg['Bundles']};arenaIds={v['rid'] for v in ag['Bundles']};sharedIds={v['rid'] for v in sg['Bundles']};metaIds={v['rid'] for v in mg['Bundles']}
def closure(rid):
 seen=set();pending=[d['rid'] for d in bundles[rid]['Dependencies']]
 while pending:
  d=pending.pop()
  if d in seen:continue
  seen.add(d);pending.extend(x['rid'] for x in bundles[d]['Dependencies'])
 return seen
assert all(not(closure(rid)&(fighterIds|arenaIds|sharedIds)) for rid in metaIds)
assert all(not(closure(rid)&(fighterIds-{rid})) for rid in fighterIds)
assert all(not(closure(rid)&fighterIds) for rid in arenaIds)
master=[rid for rid in fighterIds if any('/TripoMasterPlayable/MasterHumanoidCandidate.prefab' in p for p in pathsByBundle[rid])];assert len(master)==1
rows=[]
for gname,g in groups.items():
 for v in g['Bundles']:
  rid=v['rid'];b=bundles[rid];path=delivery/b['Name'];assert path.stat().st_size==b['FileSize'];rows.append(dict(group=gname,bundle=b['Name'],bytes=b['FileSize'],assets=pathsByBundle[rid],dependencies=[bundles[d['rid']]['Name'] for d in b['Dependencies']],transitiveDependencies=[bundles[d]['Name'] for d in sorted(closure(rid))]))
duplicates=[]
for d in j['DuplicatedAssets']:
 objects=[]
 for obj in d['DuplicatedObjects']:
  appearances=[]
  for file in obj['IncludedInBundleFiles']:
   f=data(file['rid']);bid=f['Bundle']['rid'];entries=[data(v['rid']) for v in f['OtherAssets'] if data(v['rid']).get('AssetGuid')==d['AssetGuid']];assert len(entries)==1;e=entries[0];appearances.append(dict(bundle=bundles[bid]['Name'],asset=e['AssetPath'],bytes=e['SerializedSize']+e['StreamedSize'],referencingPrefabs=pathsByBundle[bid]))
  objects.append(dict(localId=obj['LocalIdentifierInFile'],appearances=appearances,totalBytes=sum(a['bytes'] for a in appearances),avoidableBytes=sum(a['bytes'] for a in appearances)-max(a['bytes'] for a in appearances)))
 duplicates.append(dict(guid=d['AssetGuid'],objects=objects))
manifest=json.loads((delivery/'manifest.json').read_text());metaBytes=sum(bundles[r]['FileSize'] for r in metaIds);assert manifest['downloadBytes']==metaBytes
result=dict(version=version,platform='StandaloneOSX',unity=j['UnityVersion'],package=j['PackageVersion'],buildResultHash=j['BuildResultHash'],buildLayoutSource=str(layout),metadataHasNoCharacterArenaMotionDependencies=True,noCrossFighterDependencies=True,noArenaFighterDependencies=True,masterIncluded=True,masterBundle=bundles[master[0]]['Name'],masterBytes=bundles[master[0]]['FileSize'],sharedAssets=[a for rid in sharedIds for a in pathsByBundle[rid]],bundleRows=rows,duplicates=duplicates,manifest=manifest,metadataBundleBytes=metaBytes,totalBundleBytes=sum(b['FileSize'] for b in bundles.values()),totalBundleCount=len(bundles),allBundleFileSizesVerified=True)
(out/'audit.json').write_text(json.dumps(result,indent=2));shutil.copy2(layout,out/'buildlayout.json');shutil.copy2(delivery/'manifest.json',out/'manifest.json')
with (out/'bundles.csv').open('w') as f:
 w=csv.writer(f);w.writerow(['group','bundle','bytes','assets','dependencies']);w.writerows([r['group'],r['bundle'],r['bytes'],';'.join(r['assets']),';'.join(r['dependencies'])] for r in rows)
print('total bundle bytes',result['totalBundleBytes'],'MiB',result['totalBundleBytes']/1024**2,'count',len(bundles),'metadata',metaBytes,'Master',result['masterBytes'],'shared assets',len(result['sharedAssets']));print(json.dumps(duplicates,indent=2))

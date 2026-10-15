#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Build.Reporting;
using LighFight.Runtime;
public static class ThermalRosterBuild {
 const string Out="Assets/ThermalRosterReview";
 [Serializable]class Manifest {public string mode,baselineCommit="96f0222",unity,meshDeformation,graphics="Metal",sourceSHAList,assetSHAList;public int targetFrameRate=60,width=1280,height=800;public bool sourceAssetsUnchanged,development=true;}
 static string Hash(string p){using(var s=SHA256.Create())return BitConverter.ToString(s.ComputeHash(File.ReadAllBytes(p))).Replace("-","").ToLowerInvariant();}
 public static void Build(){
  string owned="/Users/macbook1/Documents/ChatGPT/LighFight/.tools/tripo-humanoid-review-unity/ProductionStreamPlayerUnity";
  string actual=Path.GetFullPath(Path.Combine(Application.dataPath,"..")).TrimEnd('/');
  if(actual!=owned||(new DirectoryInfo(actual).Attributes&FileAttributes.ReparsePoint)!=0)throw new Exception("Only existing OWN ProductionStreamPlayerUnity may build; never main");
  string mode=Environment.GetEnvironmentVariable("THERMAL_ROSTER_MODE");if(mode!="baseline"&&mode!="current")throw new Exception("Set THERMAL_ROSTER_MODE=baseline or current after root stages reviewed source copies");
  Directory.CreateDirectory(Out);Directory.CreateDirectory("ThermalRosterBuilds");
  var source=Directory.GetFiles("Assets/LighFight/Runtime","*.cs",SearchOption.AllDirectories).Concat(Directory.GetFiles("Assets/LighFight/Core","*.cs",SearchOption.AllDirectories)).OrderBy(x=>x,StringComparer.Ordinal).ToDictionary(p=>p,p=>Hash(p));
  File.WriteAllLines("ThermalRosterBuilds/source-"+mode+".sha256",source.Select(p=>p.Value+" "+p.Key));
  var scene=EditorSceneManager.OpenScene("Assets/LighFight/Scenes/Prototype.unity");var game=UnityEngine.Object.FindFirstObjectByType<Prototype>();if(!game)throw new Exception("Actual Prototype scene missing");
  var full=AssetDatabase.LoadAssetAtPath<GameContent>("Assets/LighFight/Content/GameContent.asset");if(!full||full.streamed||full.arenas.Length==0)throw new Exception("Current inline catalog/arena source required");
  string island="Assets/LighFight/Art/Drafts/TripoIslandPlayable/IslandHumanoidCandidate.prefab",ranger="Assets/LighFight/Art/Drafts/TripoRangerPlayable/RangerHumanoidCandidate.prefab";
  var content=ScriptableObject.CreateInstance<GameContent>();content.fighters=new[]{AssetDatabase.LoadAssetAtPath<GameObject>(island),AssetDatabase.LoadAssetAtPath<GameObject>(ranger)};content.arenas=new[]{full.arenas[0]};
  content.fighterMetadata=new[]{new GameContent.FighterMetadata{id="island",nameRu="Островной боец",nameEn="Island Fighter",portrait=Resources.Load<Texture2D>("Selection/fighter-3")},new GameContent.FighterMetadata{id="ranger",nameRu="Рейнджер",nameEn="Ranger",portrait=Resources.Load<Texture2D>("Selection/fighter-5")}};
  if(content.fighters.Any(x=>!x)||content.fighterMetadata.Any(x=>!x.portrait))throw new Exception("Current original rigs/source portraits missing");
  foreach(var prefab in content.fighters){long triangles=prefab.GetComponentsInChildren<SkinnedMeshRenderer>(true).Sum(r=>(long)r.sharedMesh.GetIndexCount(0)/3);if(triangles<99000||triangles>101000)throw new Exception("Current100k geometry required: "+prefab.name+" "+triangles);}
  string catalog=Out+"/TwoArtistCatalog.asset";AssetDatabase.DeleteAsset(catalog);AssetDatabase.CreateAsset(content,catalog);game.content=content;
  var deps=AssetDatabase.GetDependencies(new[]{island,ranger,AssetDatabase.GetAssetPath(content.arenas[0]),"Assets/LighFight/Resources/Selection/fighter-3.png","Assets/LighFight/Resources/Selection/fighter-5.png"},true).Where(File.Exists).Where(p=>!p.EndsWith(".cs")).OrderBy(p=>p,StringComparer.Ordinal).ToDictionary(p=>p,p=>Hash(p));
  foreach(var asset in deps.Keys.ToArray())if(File.Exists(asset+".meta"))deps.Add(asset+".meta",Hash(asset+".meta"));
  foreach(string settings in new[]{"ProjectSettings/QualitySettings.asset","ProjectSettings/GraphicsSettings.asset"})if(File.Exists(settings))deps.Add(settings,Hash(settings));
  string assetList="ThermalRosterBuilds/assets-"+mode+".sha256";File.WriteAllLines(assetList,deps.Select(p=>p.Value+" "+p.Key));
  var probe=new GameObject("Actual Prototype thermal roster EOF probe").AddComponent<ThermalRosterProbe>();probe.game=game;probe.mode=mode;probe.requireRosterSleep=mode=="current";
  probe.manifest=JsonUtility.ToJson(new Manifest{mode=mode,unity=Application.unityVersion,meshDeformation=PlayerSettings.meshDeformation.ToString(),sourceSHAList=Hash("ThermalRosterBuilds/source-"+mode+".sha256"),assetSHAList=Hash(assetList),sourceAssetsUnchanged=true},true);
  string path=Out+"/ThermalRoster.unity";EditorSceneManager.SaveScene(scene,path);AssetDatabase.SaveAssets();
  var oldApis=PlayerSettings.GetGraphicsAPIs(BuildTarget.StandaloneOSX);bool oldDefault=PlayerSettings.GetUseDefaultGraphicsAPIs(BuildTarget.StandaloneOSX);int w=PlayerSettings.defaultScreenWidth,h=PlayerSettings.defaultScreenHeight;var fs=PlayerSettings.fullScreenMode;bool bg=PlayerSettings.runInBackground;string product=PlayerSettings.productName;
  try {
   PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.StandaloneOSX,false);PlayerSettings.SetGraphicsAPIs(BuildTarget.StandaloneOSX,new[]{GraphicsDeviceType.Metal});PlayerSettings.defaultScreenWidth=1280;PlayerSettings.defaultScreenHeight=800;PlayerSettings.fullScreenMode=FullScreenMode.Windowed;PlayerSettings.runInBackground=true;PlayerSettings.productName="ThermalRosterReview";
   var r=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{path},target=BuildTarget.StandaloneOSX,locationPathName="ThermalRosterBuilds/"+mode+".app",options=BuildOptions.Development|BuildOptions.CleanBuildCache});
   if(r.summary.result!=BuildResult.Succeeded)throw new Exception("Thermal player build failed");
  }finally{PlayerSettings.SetGraphicsAPIs(BuildTarget.StandaloneOSX,oldApis);PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.StandaloneOSX,oldDefault);PlayerSettings.defaultScreenWidth=w;PlayerSettings.defaultScreenHeight=h;PlayerSettings.fullScreenMode=fs;PlayerSettings.runInBackground=bg;PlayerSettings.productName=product;AssetDatabase.SaveAssets();}
  foreach(var pair in deps)if(Hash(pair.Key)!=pair.Value)throw new Exception("Asset bytes changed during build: "+pair.Key);
  foreach(var pair in source)if(Hash(pair.Key)!=pair.Value)throw new Exception("Source changed during build: "+pair.Key);
  File.WriteAllText("ThermalRosterBuilds/"+mode+"-after-gates.txt","All source and asset SHA256 values unchanged after build\n");
 }
}
#endif

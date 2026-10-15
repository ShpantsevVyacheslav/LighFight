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
public static class DenseSkinningBenchmarkBuild {
 const string Folder="Assets/DenseSkinningBenchmark";
 const string Low="Assets/LighFight/Art/Drafts/TripoMasterPlayable/MasterHumanoidCandidate.prefab";
 const string Dense="Assets/NativeDense/Master/MasterOriginalDensityReview.prefab";
 [Serializable]class Manifest {public string mode,unity,api,dependencySHA256,projectSettingsSHA256,scope="Mac-only; configuration is not proof of GPU execution";public int numericMode,width=800,height=1000,warmup=120,frames=200;public bool development=true,automaticSingleCamera=true,frameTimingStats=true;}
 public static void CPU(){Build(MeshDeformation.CPU);}
 public static void GPU(){Build(MeshDeformation.GPU);}
 public static void GPUBatched(){Build(MeshDeformation.GPUBatched);}
 static string Hash(string path){using(var sha=SHA256.Create())return BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(path))).Replace("-","").ToLowerInvariant();}
 static void CurrentCodeGate(){
  string root=Environment.GetEnvironmentVariable("LIGHTFIGHT_MAIN_READONLY_ROOT");
  if(string.IsNullOrEmpty(root))throw new Exception("Set LIGHTFIGHT_MAIN_READONLY_ROOT to repository root; no stale production logic allowed");
  foreach(var pair in new[]{new[]{"Assets/Runtime/FighterView.cs","unity/Assets/LighFight/Runtime/FighterView.cs"},new[]{"Assets/Runtime/CandidateFootGrounding.cs","unity/Assets/LighFight/Runtime/TestCandidates/CandidateFootGrounding.cs"},new[]{"Assets/NativeSupport/NativeSupportOverlay.cs","unity/Assets/NativeSupport/NativeSupportOverlay.cs"}})
   if(Hash(pair[0])!=Hash(Path.Combine(root,pair[1])))throw new Exception("Owned current-main logic copy mismatch: "+pair[0]+"; refresh OWN copy, never main");
 }
 static void Build(MeshDeformation mode){
  string allowed=Path.GetFullPath("/Users/macbook1/Documents/ChatGPT/LighFight/.tools/tripo-dense-review/shoe-unity").TrimEnd(Path.DirectorySeparatorChar);
  string actual=Path.GetFullPath(Path.Combine(Application.dataPath,"..")).TrimEnd(Path.DirectorySeparatorChar);
  if(!string.Equals(actual,allowed,StringComparison.Ordinal))throw new Exception("Refusing PlayerSettings mutation outside exact OWN shoe-unity project: "+actual);
  if((new DirectoryInfo(actual).Attributes&FileAttributes.ReparsePoint)!=0)throw new Exception("Refusing symlinked project directory");
  CurrentCodeGate();
  var old=PlayerSettings.meshDeformation;var oldApis=PlayerSettings.GetGraphicsAPIs(BuildTarget.StandaloneOSX);bool oldDefault=PlayerSettings.GetUseDefaultGraphicsAPIs(BuildTarget.StandaloneOSX),oldTiming=PlayerSettings.enableFrameTimingStats;
  int oldWidth=PlayerSettings.defaultScreenWidth,oldHeight=PlayerSettings.defaultScreenHeight;string oldProduct=PlayerSettings.productName;var oldFullscreen=PlayerSettings.fullScreenMode;
  try {
   PlayerSettings.meshDeformation=mode;PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.StandaloneOSX,false);PlayerSettings.SetGraphicsAPIs(BuildTarget.StandaloneOSX,new[]{GraphicsDeviceType.Metal});PlayerSettings.enableFrameTimingStats=true;
   PlayerSettings.defaultScreenWidth=800;PlayerSettings.defaultScreenHeight=1000;PlayerSettings.fullScreenMode=FullScreenMode.Windowed;PlayerSettings.productName="DenseSkinningBenchmark";
   if(PlayerSettings.meshDeformation!=mode)throw new Exception("Setting readback mismatch");
   Directory.CreateDirectory(Folder);Directory.CreateDirectory("DenseSkinningBuilds");
   EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
   var qa=new GameObject("Single automatic render benchmark").AddComponent<DenseSkinningBenchmark>();
   qa.acceptedPrefab=AssetDatabase.LoadAssetAtPath<GameObject>(Low);qa.densePrefab=AssetDatabase.LoadAssetAtPath<GameObject>(Dense);
   if(!qa.acceptedPrefab||!qa.densePrefab)throw new Exception("Existing reviewed original-density assets required; never rebuild or simplify geometry here");
   foreach(var prefab in new[]{qa.acceptedPrefab,qa.densePrefab}){
    var skin=prefab.GetComponentInChildren<SkinnedMeshRenderer>();if(!skin)throw new Exception("Missing renderer");
    if(prefab==qa.densePrefab&&skin.sharedMesh.vertexCount!=1031866)throw new Exception("Original dense vertex count changed");
    foreach(var mat in skin.sharedMaterials)foreach(var name in mat.GetTexturePropertyNames())if(mat.GetTexture(name) is Texture2D tex){var path=AssetDatabase.GetAssetPath(tex);var importer=AssetImporter.GetAtPath(path) as TextureImporter;if(importer!=null&&importer.textureCompression!=TextureImporterCompression.Uncompressed)throw new Exception("Benchmark requires existing uncompressed inputs: "+path);}
   }
   var dependencies=AssetDatabase.GetDependencies(new[]{Low,Dense},true).Where(File.Exists).OrderBy(x=>x,StringComparer.Ordinal).Select(p=>p+" "+Hash(p)).ToArray();
   string depFile="DenseSkinningBuilds/input-dependencies-"+mode+".txt";File.WriteAllLines(depFile,dependencies);
   var manifest=new Manifest{mode=mode.ToString(),numericMode=(int)PlayerSettings.meshDeformation,unity=Application.unityVersion,api="Metal",dependencySHA256=Hash(depFile),projectSettingsSHA256=Hash("ProjectSettings/ProjectSettings.asset")};
   qa.buildManifest=JsonUtility.ToJson(manifest,true);qa.buildMode=mode.ToString();
   var cam=new GameObject("Only automatic camera").AddComponent<Camera>();cam.enabled=true;cam.allowHDR=true;cam.orthographic=true;cam.orthographicSize=1.32f;cam.backgroundColor=new Color(.1f,.12f,.15f,0);cam.clearFlags=CameraClearFlags.SolidColor;
   cam.transform.position=new Vector3(.2f,1.16f,6);cam.transform.LookAt(new Vector3(0,1.06f,0));cam.gameObject.AddComponent<DenseBenchmarkRenderCounter>();qa.benchmarkCamera=cam;
   var fx=cam.gameObject.AddComponent<FittedFilmicCamera>();fx.filmicShader=Shader.Find("Hidden/LighFight/FittedFilmic");if(!fx.filmicShader)throw new Exception("Original fitted filmic shader missing");
   RenderSettings.ambientMode=AmbientMode.Flat;RenderSettings.ambientLight=new Color(.45f,.43f,.4f);
   var light=new GameObject("Identical key").AddComponent<Light>();light.type=LightType.Directional;light.intensity=1.2f;light.shadows=LightShadows.None;light.transform.rotation=Quaternion.Euler(45,-30,0);
   string scene=Folder+"/Benchmark.unity";EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene(),scene);AssetDatabase.SaveAssets();
   string build="DenseSkinningBuilds/"+mode+".app";
   File.WriteAllText("DenseSkinningBuilds/manifest-"+mode+".json",qa.buildManifest);
   var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{scene},target=BuildTarget.StandaloneOSX,locationPathName=build,options=BuildOptions.Development});
   if(report.summary.result!=BuildResult.Succeeded)throw new Exception("Benchmark build failed "+mode);
   File.WriteAllText("DenseSkinningBuilds/build-result-"+mode+".txt","Succeeded\nbytes="+report.summary.totalSize+"\nmode="+PlayerSettings.meshDeformation+"\n");
  }finally{
   PlayerSettings.meshDeformation=old;PlayerSettings.SetGraphicsAPIs(BuildTarget.StandaloneOSX,oldApis);PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.StandaloneOSX,oldDefault);PlayerSettings.enableFrameTimingStats=oldTiming;
   PlayerSettings.defaultScreenWidth=oldWidth;PlayerSettings.defaultScreenHeight=oldHeight;PlayerSettings.fullScreenMode=oldFullscreen;PlayerSettings.productName=oldProduct;AssetDatabase.SaveAssets();
  }
 }
}
#endif

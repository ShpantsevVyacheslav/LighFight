#if UNITY_EDITOR
using System;using System.IO;using System.Linq;using System.Security.Cryptography;
using UnityEngine;using UnityEngine.Rendering;using UnityEditor;using UnityEditor.SceneManagement;using UnityEditor.Build.Reporting;
public static class KickPerformanceBuild {
 const string Repo="/Users/macbook1/Documents/ChatGPT/LighFight";
 static string Hash(string p){using(var sha=SHA256.Create())return BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(p))).Replace("-","").ToLowerInvariant();}
 [Serializable]class Manifest{public string mode,unity,meshDeformation,inputListSHA,scope="Actual actor/core diagnostic, not full Prototype or phone power proof";public int target=60,width=1280,height=720,framesPerWindow=200;}
 public static void BuildBaseline(){Build("baseline");}public static void BuildCurrent(){Build("current");}
 static void Build(string mode){
  if(Path.GetFullPath(Application.dataPath)!=Repo+"/unity/Assets")throw new Exception("Root-authorized main staging only; do not run in another project");
  for(int i=0;i<UnityEngine.SceneManagement.SceneManager.sceneCount;i++)if(UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).isDirty)throw new Exception("Save or close dirty scenes before diagnostic build; refusing loss of edits");
  var setup=EditorSceneManager.GetSceneManagerSetup();var apis=PlayerSettings.GetGraphicsAPIs(BuildTarget.StandaloneOSX);bool def=PlayerSettings.GetUseDefaultGraphicsAPIs(BuildTarget.StandaloneOSX),bg=PlayerSettings.runInBackground;int w=PlayerSettings.defaultScreenWidth,h=PlayerSettings.defaultScreenHeight;var fs=PlayerSettings.fullScreenMode;string product=PlayerSettings.productName;
  string buildDir=Repo+"/.tools/kick-performance-review/Builds";Directory.CreateDirectory(buildDir);
  try{
   var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);var qa=new GameObject("Actual shipping actor core performance diagnostic").AddComponent<KickPerformanceProbe>();qa.mode=mode;qa.roles=new[]{"Island","Rose","Retro"};
   string[] paths={"Assets/LighFight/Art/Drafts/TripoIslandPlayable/IslandHumanoidCandidate.prefab","Assets/LighFight/Art/Drafts/TripoPlayable/RoseHumanoidCandidate.prefab","Assets/LighFight/Art/Drafts/TripoRetroPlayable/RetroHumanoidCandidate.prefab"};
   qa.prefabs=paths.Select(AssetDatabase.LoadAssetAtPath<GameObject>).ToArray();if(qa.prefabs.Any(p=>!p))throw new Exception("Shipping prefabs missing");
   var camera=new GameObject("Only automatic diagnostic camera").AddComponent<Camera>();camera.transform.position=new Vector3(0,1.35f,-6);camera.transform.LookAt(new Vector3(0,1,0));camera.fieldOfView=35;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.07f,.08f,.1f,0);camera.allowHDR=true;qa.viewCamera=camera;
   var fx=camera.gameObject.AddComponent<LighFight.Runtime.FittedFilmicCamera>();fx.filmicShader=Resources.Load<Shader>("Shaders/FittedFilmic");if(!fx.filmicShader)throw new Exception("Shipping HDR shader missing");
   var light=new GameObject("Fixed key").AddComponent<Light>();light.type=LightType.Directional;light.intensity=1.2f;light.shadows=LightShadows.None;light.transform.rotation=Quaternion.Euler(45,-30,0);RenderSettings.ambientMode=AmbientMode.Flat;RenderSettings.ambientLight=new Color(.45f,.43f,.4f);
   var files=AssetDatabase.GetDependencies(paths.Concat(new[]{AssetDatabase.GetAssetPath(fx.filmicShader)}).ToArray(),true).Where(File.Exists).OrderBy(p=>p,StringComparer.Ordinal).ToArray();
   var artist=files.Where(p=>!p.EndsWith(".cs")).Concat(files.Where(p=>!p.EndsWith(".cs")).Select(p=>p+".meta").Where(File.Exists)).OrderBy(p=>p,StringComparer.Ordinal).ToDictionary(p=>p,p=>Hash(p));
   File.WriteAllLines(buildDir+"/artist-"+mode+".sha256",artist.Select(p=>p.Value+" "+p.Key));
   var source=Directory.GetFiles("Assets/LighFight/Runtime","*.cs",SearchOption.AllDirectories).Concat(Directory.GetFiles("Assets/LighFight/Core","*.cs",SearchOption.AllDirectories)).Concat(Directory.GetFiles("Assets/NativeSupport","*.cs",SearchOption.AllDirectories)).OrderBy(p=>p,StringComparer.Ordinal).ToDictionary(p=>p,p=>Hash(p));
   File.WriteAllLines(buildDir+"/source-"+mode+".sha256",source.Select(p=>p.Value+" "+p.Key));
   qa.manifest=JsonUtility.ToJson(new Manifest{mode=mode,unity=Application.unityVersion,meshDeformation=PlayerSettings.meshDeformation.ToString(),inputListSHA=Hash(buildDir+"/artist-"+mode+".sha256")},true);
   Directory.CreateDirectory("Assets/PerfKickReview");string scenePath="Assets/PerfKickReview/KickPerf.unity";EditorSceneManager.SaveScene(scene,scenePath);
   PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.StandaloneOSX,false);PlayerSettings.SetGraphicsAPIs(BuildTarget.StandaloneOSX,new[]{GraphicsDeviceType.Metal});PlayerSettings.defaultScreenWidth=1280;PlayerSettings.defaultScreenHeight=720;PlayerSettings.fullScreenMode=FullScreenMode.Windowed;PlayerSettings.runInBackground=true;PlayerSettings.productName="KickPerformanceReview";
   var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{scenePath},target=BuildTarget.StandaloneOSX,locationPathName=buildDir+"/"+mode+".app",options=BuildOptions.Development});
   if(report.summary.result!=BuildResult.Succeeded)throw new Exception("Kick diagnostic build failed");
   foreach(var p in artist)if(Hash(p.Key)!=p.Value)throw new Exception("Artist bytes changed: "+p.Key);foreach(var p in source)if(Hash(p.Key)!=p.Value)throw new Exception("Source changed: "+p.Key);
   File.WriteAllText(buildDir+"/"+mode+"-build-result.txt","PASS build; artist/source beforeafter SHA unchanged; runtime NOT YET RUN\n");
  }finally{PlayerSettings.SetGraphicsAPIs(BuildTarget.StandaloneOSX,apis);PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.StandaloneOSX,def);PlayerSettings.defaultScreenWidth=w;PlayerSettings.defaultScreenHeight=h;PlayerSettings.fullScreenMode=fs;PlayerSettings.runInBackground=bg;PlayerSettings.productName=product;if(setup!=null&&setup.Length>0&&setup.Any(x=>x.isLoaded)&&setup.Count(x=>x.isActive&&x.isLoaded)==1&&setup.All(x=>!string.IsNullOrEmpty(x.path)&&File.Exists(x.path)))EditorSceneManager.RestoreSceneManagerSetup(setup);else Debug.Log("KICK_PERF_RESTORE_SKIPPED invalid original batch setup");}
 }
}
#endif

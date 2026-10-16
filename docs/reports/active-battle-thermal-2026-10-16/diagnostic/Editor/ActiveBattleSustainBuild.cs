#if UNITY_EDITOR
using System;using System.IO;using System.Linq;using System.Collections.Generic;using System.Reflection;using System.Security.Cryptography;
using UnityEngine;using UnityEngine.Rendering;using UnityEditor;using UnityEditor.SceneManagement;using UnityEditor.Build.Reporting;using LighFight.Runtime;using LighFight.Core;
// Stage and build ONLY in explicitly selected existing .tools project. No shipping mutation.
public static class ActiveBattleSustainBuild {
 const string Repo="/Users/macbook1/Documents/ChatGPT/LighFight",Packet=Repo+"/.tools/thermal-sustain-review/ActiveBattleScreening/NextCollectorR7",Qa="Assets/ThermalSustainReview";
 static string Hash(string path){using(var s=SHA256.Create())using(var f=File.OpenRead(path))return BitConverter.ToString(s.ComputeHash(f)).Replace("-","").ToLowerInvariant();}
 static void Guard(){
  string allowed=Environment.GetEnvironmentVariable("LIGHTFIGHT_THERMAL_OWNED_PROJECT");if(string.IsNullOrWhiteSpace(allowed))throw new Exception("Set LIGHTFIGHT_THERMAL_OWNED_PROJECT to existing warmed owned project");
  string actual=Path.GetFullPath(Path.Combine(Application.dataPath,"..")).TrimEnd('/');allowed=Path.GetFullPath(allowed).TrimEnd('/');
  if(actual!=allowed||!actual.StartsWith(Repo+"/.tools/",StringComparison.Ordinal)||actual.StartsWith(Packet+"/Builds",StringComparison.Ordinal))throw new Exception("Refusing main/shipping or non-owned project: "+actual);
  if(Path.GetFullPath(Environment.CurrentDirectory).TrimEnd('/')!=actual)throw new Exception("Owned Editor working directory mismatch");NoLinks(actual);NoLinks(Application.dataPath);for(int i=0;i<UnityEngine.SceneManagement.SceneManager.sceneCount;i++)if(UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).isDirty)throw new Exception("Refusing to close dirty scenes");
 }
 static void NoLinks(string path){for(var d=new DirectoryInfo(path);d!=null&&d.FullName.StartsWith(Repo,StringComparison.Ordinal);d=d.Parent)if(d.Exists&&(d.Attributes&FileAttributes.ReparsePoint)!=0)throw new Exception("Symlink output/project forbidden: "+d.FullName);}
 public static void Stage(){
  Guard();NoLinks(Path.GetFullPath(Qa));Directory.CreateDirectory(Qa+"/Runtime");Directory.CreateDirectory(Qa+"/Plugins/iOS");
  foreach(var file in new[]{"Runtime/ThermalSustainReview.cs","Plugins/iOS/LFThermalTelemetry.mm"}){
   string dest=Qa+"/"+file;NoLinks(Path.GetDirectoryName(Path.GetFullPath(dest)));
   if(!File.Exists(dest)||Hash(dest)!=Hash(Packet+"/"+file))File.Copy(Packet+"/"+file,dest,true);
   AssetDatabase.ImportAsset(dest,ImportAssetOptions.ForceSynchronousImport);
  }
  var importer=AssetImporter.GetAtPath(Qa+"/Plugins/iOS/LFThermalTelemetry.mm") as PluginImporter;if(importer==null)throw new Exception("iOS native source importer missing");
  importer.SetCompatibleWithAnyPlatform(false);importer.SetCompatibleWithEditor(false);importer.SetCompatibleWithPlatform(BuildTarget.StandaloneOSX,false);importer.SetCompatibleWithPlatform(BuildTarget.iOS,true);importer.SaveAndReimport();
  Debug.Log("AGENT_RESULT Thermal QA staged only in owned project. Let compilation finish, then separate BuildMacOS/ExportIOS invocation.");
 }
 [Serializable]class GroundingVariant{public string variant,mainSHA,actualSHA,reviewedCandidateSHA;}
 [Serializable]class Manifest{public string target,unity,project,qualitySettingsSHA256,graphicsSettingsSHA256,colorSpace,sourceListSHA256,artistListSHA256,runtimePacketSHA256,nativePacketSHA256,meshDeformation,embeddedDescriptorSHA256;public bool development=true,frameTimingStats=true,frameTimingStatsRestored=false;public string[] playerOnlyDefines={"LIGHTFIGHT_THERMAL_REVIEW"};public string status="BUILT_NOT_RUNTIME_VERIFIED";}
 public static void BuildMacOS(){Build(BuildTarget.StandaloneOSX);}
 public static void BuildMacOSInline(){Build(BuildTarget.StandaloneOSX,true);}
 public static void ExportIOS(){Build(BuildTarget.iOS);}
 const string GroundPath="Assets/LighFight/Runtime/TestCandidates/CandidateFootGrounding.cs";
 static string CandidatePath=>Repo+"/.tools/thermal-sustain-review/GroundingRowQA/Assets/Grounding/CandidateFootGrounding.cs";
 static bool CandidateOverride(){string mode=Environment.GetEnvironmentVariable("LIGHTFIGHT_THERMAL_GROUNDING_VARIANT")??"baseline";if(mode=="baseline")return false;if(mode!="vertical-row")throw new Exception("Unknown grounding variant");NoLinks(CandidatePath);string expected=Environment.GetEnvironmentVariable("LIGHTFIGHT_THERMAL_CANDIDATE_SHA"),original=Environment.GetEnvironmentVariable("LIGHTFIGHT_THERMAL_BASELINE_SHA");if(expected!="a90db8f33afd7dfb5150a306de405f9bbfeac6dfaf837925610134c0a7029f0a"||original!="4fc0cfc36fd23f09a6615de8dc8aa807cddd5e78c23a3d75e3f01e59b179c165"||Hash(CandidatePath)!=expected||Hash(Repo+"/unity/"+GroundPath)!=original||Hash(GroundPath)!=expected)throw new Exception("Explicit reviewed grounding candidate/baseline SHA mismatch");return true;}
 static void Build(BuildTarget target,bool inlineDiagnostic=false){Guard();bool candidate=CandidateOverride();if(candidate&&!inlineDiagnostic)throw new Exception("Candidate allowed only selected-inline diagnostic");try{BuildImpl(target,inlineDiagnostic);}finally{if(candidate){File.Copy(Repo+"/unity/"+GroundPath,GroundPath,true);AssetDatabase.ImportAsset(GroundPath,ImportAssetOptions.ForceSynchronousImport);}}}
 static void BuildImpl(BuildTarget target,bool inlineDiagnostic=false){
  Guard();if(EditorUserBuildSettings.activeBuildTarget!=target)throw new Exception("Launch correct -buildTarget before job; no in-job target switching");
  string id=Environment.GetEnvironmentVariable("LIGHTFIGHT_THERMAL_RUN_ID");if(string.IsNullOrWhiteSpace(id)||id.Length>64||id.Any(c=>!char.IsLetterOrDigit(c)&&c!='-'&&c!='_'))throw new Exception("Set unique safe LIGHTFIGHT_THERMAL_RUN_ID");
  string dir=Path.GetFullPath(Packet+"/Builds/"+id+"/"+target);NoLinks(dir);if(Directory.Exists(dir))throw new Exception("Refusing overwrite; choose a fresh run id: "+dir);
  foreach(string path in new[]{"Runtime/ThermalSustainReview.cs","Plugins/iOS/LFThermalTelemetry.mm"})if(!File.Exists(Qa+"/"+path)||Hash(Qa+"/"+path)!=Hash(Packet+"/"+path))throw new Exception("Stage latest exact QA source first: "+path);
  var source=new Dictionary<string,string>();foreach(string part in new[]{"Assets/LighFight/Runtime","Assets/LighFight/Core","Assets/NativeSupport"})foreach(string file in Directory.GetFiles(part,"*.cs",SearchOption.AllDirectories)){
   string relative=file.Replace('\\','/');string main=Repo+"/unity/"+relative;if(!File.Exists(main)||(Hash(file)!=Hash(main)&&!(relative==GroundPath&&CandidateOverride())))throw new Exception("Stale production source copy: "+relative);source.Add(relative,Hash(file));
  }
  foreach(string part in new[]{"Assets/LighFight/Runtime","Assets/LighFight/Core","Assets/NativeSupport"})foreach(string main in Directory.GetFiles(Repo+"/unity/"+part,"*.cs",SearchOption.AllDirectories))if(!source.ContainsKey(main.Substring((Repo+"/unity/").Length)))throw new Exception("Missing current production source in owned project: "+main);
  const string cb="Assets/LighFight/Editor/ContentBuild.cs";if(!File.Exists(cb)||Hash(cb)!=Hash(Repo+"/unity/"+cb))throw new Exception("Stage current Editor ContentBuild and its existing Addressables dependencies");
  if(string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("LIGHFIGHT_EMBEDDED_VERSION")))throw new Exception("Set explicit diagnostic LIGHFIGHT_EMBEDDED_VERSION");
  foreach(string settings in new[]{"ProjectSettings/QualitySettings.asset","ProjectSettings/GraphicsSettings.asset"})if(!File.Exists(settings)||Hash(settings)!=Hash(Repo+"/unity/"+settings))throw new Exception("Stage current matching source settings before QA: "+settings);
  string mainSettings=File.ReadAllText(Repo+"/unity/ProjectSettings/ProjectSettings.asset");
  var color=System.Text.RegularExpressions.Regex.Match(mainSettings,@"(?m)^  m_ActiveColorSpace: (\d+)");var deformation=System.Text.RegularExpressions.Regex.Match(mainSettings,@"(?m)^  meshDeformation: (\d+)");
  if(!color.Success||int.Parse(color.Groups[1].Value)!=(int)PlayerSettings.colorSpace||!deformation.Success||int.Parse(deformation.Groups[1].Value)!=(int)PlayerSettings.meshDeformation)throw new Exception("Owned colorSpace/meshDeformation differ from current shipping source");
  var catalog=AssetDatabase.LoadAssetAtPath<GameContent>("Assets/LighFight/Content/GameContent.asset");if(!catalog||catalog.streamed||catalog.FighterCount!=11||catalog.ArenaCount!=5)throw new Exception("Current shipping11/5 source catalog/assets required, not old 2actor fixture");
  var deps=AssetDatabase.GetDependencies("Assets/LighFight/Content/GameContent.asset",true).Where(File.Exists).Where(p=>!p.EndsWith(".cs")).ToArray();
  var art=deps.Concat(deps.Select(p=>p+".meta").Where(File.Exists)).Distinct().OrderBy(p=>p,StringComparer.Ordinal).ToDictionary(p=>p,Hash);
  foreach(var pair in art)if(pair.Key.StartsWith("Assets/",StringComparison.Ordinal)&&(!File.Exists(Repo+"/unity/"+pair.Key)||Hash(Repo+"/unity/"+pair.Key)!=pair.Value))throw new Exception("Stale/mismatched shipping artist/import input: "+pair.Key);
  Directory.CreateDirectory(dir);File.WriteAllText(dir+"/grounding-variant.json",JsonUtility.ToJson(new GroundingVariant{variant=CandidateOverride()?"vertical-row":"baseline",mainSHA=Hash(Repo+"/unity/"+GroundPath),actualSHA=Hash(GroundPath),reviewedCandidateSHA=Hash(CandidatePath)},true));File.WriteAllLines(dir+"/source-before.sha256",source.OrderBy(p=>p.Key,StringComparer.Ordinal).Select(p=>p.Value+" "+p.Key));File.WriteAllLines(dir+"/artist-before.sha256",art.Select(p=>p.Value+" "+p.Key));
  var setup=EditorSceneManager.GetSceneManagerSetup();var scenes=EditorBuildSettings.scenes;var apis=PlayerSettings.GetGraphicsAPIs(target);bool defaultApi=PlayerSettings.GetUseDefaultGraphicsAPIs(target),frameTiming=PlayerSettings.enableFrameTimingStats,bg=PlayerSettings.runInBackground;int width=PlayerSettings.defaultScreenWidth,height=PlayerSettings.defaultScreenHeight;string product=PlayerSettings.productName;var full=PlayerSettings.fullScreenMode;
  var manifest=new Manifest{target=target.ToString(),unity=Application.unityVersion,project=Path.GetFullPath("."),qualitySettingsSHA256=Hash("ProjectSettings/QualitySettings.asset"),graphicsSettingsSHA256=Hash("ProjectSettings/GraphicsSettings.asset"),colorSpace=PlayerSettings.colorSpace.ToString(),sourceListSHA256=Hash(dir+"/source-before.sha256"),artistListSHA256=Hash(dir+"/artist-before.sha256"),runtimePacketSHA256=Hash(Qa+"/Runtime/ThermalSustainReview.cs"),nativePacketSHA256=Hash(Qa+"/Plugins/iOS/LFThermalTelemetry.mm"),meshDeformation=PlayerSettings.meshDeformation.ToString()};
  var parkedPaths=new List<KeyValuePair<string,string>>();
  var addressables=UnityEditor.AddressableAssets.AddressableAssetSettingsDefaultObject.GetSettings(true);var oldAddressablesMode=addressables.BuildAddressablesWithPlayerBuild;
  try{
   if(inlineDiagnostic){addressables.BuildAddressablesWithPlayerBuild=UnityEditor.AddressableAssets.Settings.AddressableAssetSettings.PlayerBuildOption.DoNotBuildWithPlayer;EditorUtility.SetDirty(addressables);AssetDatabase.SaveAssets();if(addressables.BuildAddressablesWithPlayerBuild!=UnityEditor.AddressableAssets.Settings.AddressableAssetSettings.PlayerBuildOption.DoNotBuildWithPlayer)throw new Exception("AA auto-build disable readback failed");File.WriteAllText(dir+"/inline-aa-policy.txt","old="+oldAddressablesMode+"\nobservedBeforeBuildPlayer="+addressables.BuildAddressablesWithPlayerBuild+"\n");
    int parked=0;foreach(string path in new[]{"Assets/StreamingAssets/aa",UnityEngine.AddressableAssets.Addressables.BuildPath})if(Directory.Exists(path)){string dest=dir+"/parked-aa-"+(parked++);Directory.Move(path,dest);parkedPaths.Add(new KeyValuePair<string,string>(path,dest));if(File.Exists(path+".meta")){File.Move(path+".meta",dest+".meta");}File.AppendAllText(dir+"/inline-aa-policy.txt","parked="+path+"\n");}
    AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
    foreach(string path in new[]{"Assets/StreamingAssets/aa",UnityEngine.AddressableAssets.Addressables.BuildPath})if(Directory.Exists(path))throw new Exception("UnexpectedAAdata path recreated before inlinebuild: "+path);
   }
   PlayerSettings.enableFrameTimingStats=true;PlayerSettings.SetUseDefaultGraphicsAPIs(target,false);PlayerSettings.SetGraphicsAPIs(target,new[]{GraphicsDeviceType.Metal});PlayerSettings.runInBackground=true;PlayerSettings.productName="ActiveBattleThermalReview20261016";
   if(target==BuildTarget.StandaloneOSX){PlayerSettings.defaultScreenWidth=1280;PlayerSettings.defaultScreenHeight=720;PlayerSettings.fullScreenMode=FullScreenMode.Windowed;}
   var type=AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("LighFight.Editor.ContentBuild",false)).FirstOrDefault(t=>t!=null);if(type==null)throw new Exception("Current ContentBuild not compiled");
   string sceneToBuild="Assets/LighFight/Scenes/EmbeddedBootstrap.unity";
   if(inlineDiagnostic){
    var selected=ScriptableObject.CreateInstance<GameContent>();selected.fighters=new[]{catalog.fighters[8],catalog.fighters[3]};selected.arenas=new[]{catalog.arenas[0]};selected.fighterMetadata=new[]{catalog.fighterMetadata[8],new GameContent.FighterMetadata{id="island-fighter",nameEn="Island Fighter",nameRu="Островной боец"}};
    const string selectedPath="Assets/ThermalSustainReview/InlineSelectedContent.asset";AssetDatabase.DeleteAsset(selectedPath);AssetDatabase.CreateAsset(selected,selectedPath);
    var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);new GameObject("Host Bridge").AddComponent<HostBridge>();new GameObject("Production Prototype selected inline diagnostic").AddComponent<Prototype>().content=selected;
    sceneToBuild="Assets/ThermalSustainReview/InlineSelectedBattle.unity";EditorSceneManager.SaveScene(scene,sceneToBuild);AssetDatabase.SaveAssets();
    File.WriteAllText(dir+"/bootstrap-difference.txt","DIAGNOSTIC INLINE selected exact prefabs; NOT shipping stream memory/loading parity\nsourceActor8->diagnosticActor0 Retro; sourceOpponent3->diagnosticOpponent1 Island; sourceArena0->diagnosticArena0\nfull11/5 GetDependencies artist/import SHA gate before selection passed\n");
   }else type.GetMethod("BuildEmbedded",BindingFlags.Public|BindingFlags.Static).Invoke(null,null);
   if(!inlineDiagnostic){string descriptor="Assets/LighFight/Content/EmbeddedContentDescriptor.json";var data=JsonUtility.FromJson<EmbeddedContentDescriptor>(File.ReadAllText(descriptor));if(data==null||data.Validate(target.ToString(),Application.unityVersion)!=null)throw new Exception("Generated embedded descriptor invalid");manifest.embeddedDescriptorSHA256=Hash(descriptor);}
   if(!PlayerSettings.enableFrameTimingStats)throw new Exception("FrameTimingStats setting readback failed");
   string output=target==BuildTarget.iOS?dir+"/Xcode":dir+"/ThermalSustainReview.app";
   var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{sceneToBuild},target=target,locationPathName=output,options=BuildOptions.Development,extraScriptingDefines=new[]{"LIGHTFIGHT_THERMAL_REVIEW"}});
   if(report.summary.result!=BuildResult.Succeeded)throw new Exception("Thermal diagnostic build/export failed");
   foreach(var pair in source)if(Hash(pair.Key)!=pair.Value)throw new Exception("Production source mutated in owned build: "+pair.Key);foreach(var pair in art)if(Hash(pair.Key)!=pair.Value)throw new Exception("Shipping artist input mutated: "+pair.Key);
   File.WriteAllLines(dir+"/artist-after.sha256",art.Select(p=>Hash(p.Key)+" "+p.Key));File.WriteAllText(dir+"/build-result.txt","Succeeded; execution not verified; bytes="+report.summary.totalSize+"\n");
  }catch(Exception e){manifest.status="BUILD_FAILED";File.WriteAllText(dir+"/failure.txt",e.ToString());throw;}
  finally{
   foreach(var parked in parkedPaths){if(Directory.Exists(parked.Key))throw new Exception("Do not overwrite recreatedAApath onrestore: "+parked.Key);Directory.Move(parked.Value,parked.Key);if(File.Exists(parked.Value+".meta"))File.Move(parked.Value+".meta",parked.Key+".meta");}
   addressables.BuildAddressablesWithPlayerBuild=oldAddressablesMode;EditorUtility.SetDirty(addressables);
   PlayerSettings.enableFrameTimingStats=frameTiming;PlayerSettings.SetGraphicsAPIs(target,apis);PlayerSettings.SetUseDefaultGraphicsAPIs(target,defaultApi);PlayerSettings.runInBackground=bg;PlayerSettings.defaultScreenWidth=width;PlayerSettings.defaultScreenHeight=height;PlayerSettings.fullScreenMode=full;PlayerSettings.productName=product;EditorBuildSettings.scenes=scenes;
   if(setup!=null&&setup.Length>0&&setup.Any(s=>s.isLoaded)&&setup.Count(s=>s.isLoaded&&s.isActive)==1&&setup.All(s=>!string.IsNullOrEmpty(s.path)&&File.Exists(s.path)))EditorSceneManager.RestoreSceneManagerSetup(setup);
   AssetDatabase.SaveAssets();manifest.frameTimingStatsRestored=PlayerSettings.enableFrameTimingStats==frameTiming;File.WriteAllText(dir+"/manifest.json",JsonUtility.ToJson(manifest,true));
  }
  Debug.Log("AGENT_RESULT Thermal diagnostic build/export PASS; explicit runtime flag still required: "+dir);
 }
}
#endif

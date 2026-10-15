using System;using System.IO;using UnityEditor;using UnityEditor.SceneManagement;using UnityEditor.Build.Reporting;using UnityEngine;using LighFight.Runtime;
namespace LighFight.Editor {
public static class EmbeddedContentReview {
 public static void PrepareFixture(){
  const string path="Assets/LighFight/Content/GameContent.asset";if(AssetDatabase.LoadAssetAtPath<GameContent>(path))throw new InvalidOperationException("Existing source catalog must not be replaced by fixtures");
  var c=ScriptableObject.CreateInstance<GameContent>();c.fighters=new[]{Load("fighter-1"),Load("fighter-6"),Load("fighter-7")};c.arenas=new[]{Load("arena-0"),Load("arena-1")};c.fighterMetadata=new[]{new GameContent.FighterMetadata{id="qa-procedural-1",nameEn="QA procedural 1",nameRu="QA процедурный 1"},new GameContent.FighterMetadata{id="qa-procedural-6",nameEn="QA procedural 6",nameRu="QA процедурный 6"},new GameContent.FighterMetadata{id="qa-procedural-7",nameEn="QA procedural 7",nameRu="QA процедурный 7"}};if(!c.ValidateCatalog(out var error))throw new InvalidOperationException(error);AssetDatabase.CreateAsset(c,path);AssetDatabase.SaveAssets();
 }
 static GameObject Load(string name)=>AssetDatabase.LoadAssetAtPath<GameObject>("Assets/LighFight/Content/"+name+".prefab");
 public static void RunWithFixture(){PrepareFixture();Run();}
 public static void Run(){
  string output=Environment.GetEnvironmentVariable("LIGHFIGHT_EMBEDDED_PLAYER_PATH"),evidence=Environment.GetEnvironmentVariable("LIGHFIGHT_EMBEDDED_EVIDENCE");if(string.IsNullOrWhiteSpace(output)||string.IsNullOrWhiteSpace(evidence)||Directory.Exists(output)||Directory.Exists(evidence))throw new InvalidOperationException("Provide new LIGHFIGHT_EMBEDDED_PLAYER_PATH and LIGHFIGHT_EMBEDDED_EVIDENCE directories");
  if(EditorUserBuildSettings.activeBuildTarget!=BuildTarget.StandaloneOSX)throw new InvalidOperationException("StandaloneOSX review target required at editor launch");
  var company=PlayerSettings.companyName;var product=PlayerSettings.productName;try{
   PlayerSettings.companyName="LighFightAgentQA";PlayerSettings.productName="EmbeddedContentReview";
   ContentBuild.BuildEmbedded();var scene=EditorSceneManager.GetActiveScene();var probe=new GameObject("Real embedded delivery proof").AddComponent<EmbeddedContentPlayerProbe>();probe.bootstrap=UnityEngine.Object.FindFirstObjectByType<ContentBootstrap>();probe.output=evidence;EditorSceneManager.SaveScene(scene);
   Directory.CreateDirectory(evidence);File.WriteAllText(Path.Combine(evidence,"editor-build.json"),JsonUtility.ToJson(new BuildRecord{unity=Application.unityVersion,sourceCatalog="Assets/LighFight/Content/GameContent.asset",bootstrap="Assets/LighFight/Scenes/EmbeddedBootstrap.unity"},true));
   var result=BuildPipeline.BuildPlayer(new[]{scene.path},output,BuildTarget.StandaloneOSX,BuildOptions.Development);if(result.summary.result!=BuildResult.Succeeded)throw new InvalidOperationException("Embedded review player failed");Debug.Log("AGENT_RESULT embedded real local player built; runtime proof not yet complete");
  }finally{PlayerSettings.companyName=company;PlayerSettings.productName=product;AssetDatabase.SaveAssets();}
 }
 [Serializable]class BuildRecord{public string unity,sourceCatalog,bootstrap;}
}}

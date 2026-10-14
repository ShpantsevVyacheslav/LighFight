using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using LighFight.Core;
using LighFight.Runtime;

namespace LighFight.Editor {
 // Separate local test scene: never replace the shipping roster while reviewing a candidate.
 public static class TripoWebReviewBuild {
  public const string Folder="Assets/LighFight/Art/Drafts/TripoPlayable";
  public const string ScenePath=Folder+"/WebReview.unity";
  public static void Prepare() {
   var source=AssetDatabase.LoadAssetAtPath<GameContent>("Assets/LighFight/Content/GameContent.asset");
   var candidate=AssetDatabase.LoadAssetAtPath<GameObject>(Folder+"/RoseHumanoidCandidate.prefab");
   if(!source||!candidate)throw new InvalidOperationException("Missing Tripo review candidate or starter content");
   var view=candidate.GetComponent<FighterView>();var animator=candidate.GetComponentInChildren<Animator>();
   if(!view||!view.IsReady||!animator||!animator.avatar||!animator.avatar.isHuman)throw new InvalidOperationException("Invalid Tripo Humanoid candidate");
   foreach(string role in Moves.All.Where(m=>m.Id!="sweep").Select(m=>m.Id).Concat(new[]{"Idle","Guard","Walk","WalkBack","Crouch","Hit","Fall","GetUp"}))
    if(!view.HasNativeMotion(role))throw new InvalidOperationException("Missing review state: "+role);
   string contentPath=Folder+"/ReviewContent.asset";
   var content=AssetDatabase.LoadAssetAtPath<GameContent>(contentPath);
   if(!content){content=ScriptableObject.CreateInstance<GameContent>();AssetDatabase.CreateAsset(content,contentPath);}
   content.fighterMetadata=source.fighterMetadata==null?null:(GameContent.FighterMetadata[])source.fighterMetadata.Clone();content.fighters=(GameObject[])source.fighters.Clone();content.arenas=(GameObject[])source.arenas.Clone();content.fighters[4]=candidate;
   foreach(var entry in new[]{(index:0,name:"Bone"),(index:2,name:"Master"),(index:3,name:"Island"),(index:5,name:"Ranger")}){var replacement=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/LighFight/Art/Drafts/Tripo"+entry.name+"Playable/"+entry.name+"HumanoidCandidate.prefab");if(!replacement||!replacement.GetComponent<FighterView>().IsReady)throw new InvalidOperationException("Invalid roster replacement: "+entry.name);content.fighters[entry.index]=replacement;}
   EditorUtility.SetDirty(content);
   var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
   new GameObject("LighFight Host Bridge").AddComponent<HostBridge>();
   var prototype=new GameObject("Tripo local battle review").AddComponent<Prototype>();prototype.content=content;
   prototype.gameObject.AddComponent<TripoReviewProbe>();
   var fields=new SerializedObject(prototype);fields.FindProperty("selectedFighter").intValue=4;fields.FindProperty("selectedArena").intValue=4;fields.ApplyModifiedPropertiesWithoutUndo();
   EditorSceneManager.SaveScene(scene,ScenePath);AssetDatabase.SaveAssets();
   Debug.Log("AGENT_RESULT Tripo test scene prepared; shipping roster unchanged");
  }
  static void PreserveIslandSourceResolution() {
   string path="Assets/LighFight/Art/Drafts/TripoIslandPlayable/Source/Island/island-h31-unity-native-pack_basecolor.JPEG";
   var importer=(TextureImporter)AssetImporter.GetAtPath(path);importer.GetSourceTextureWidthAndHeight(out int width,out int height);
   if(width!=8192||height!=8192)throw new InvalidOperationException("Unexpected Island source texture version");
   importer.maxTextureSize=8192;
   foreach(string platform in new[]{"DefaultTexturePlatform","Standalone","WebGL","iPhone","Android"}){var settings=importer.GetPlatformTextureSettings(platform);settings.maxTextureSize=8192;importer.SetPlatformTextureSettings(settings);}
   importer.SaveAndReimport();var texture=AssetDatabase.LoadAssetAtPath<Texture2D>(path);
   if(texture.width!=8192||texture.height!=8192)throw new InvalidOperationException("Island texture was reduced during import");
   Debug.Log("AGENT_RESULT Island original albedo preserved at "+texture.width+"x"+texture.height);
  }
  public static void EnableFilmicReview(){PlayerSettings.colorSpace=ColorSpace.Linear;AssetDatabase.SaveAssets();Debug.Log("AGENT_RESULT Linear presentation enabled for full-scene review");}
  public static void VerifyCandidateBuildRepeatability() {
   PromoteReviewedRoster();var paths=new[]{Folder,"Assets/LighFight/Art/Drafts/TripoIslandPlayable","Assets/LighFight/Art/Drafts/TripoRangerPlayable","Assets/LighFight/Art/Drafts/TripoBonePlayable"}.SelectMany(folder=>Directory.GetFiles(folder,"*.controller")).ToArray();
   var before=paths.Select(File.ReadAllBytes).ToArray();PromoteReviewedRoster();
   for(int i=0;i<paths.Length;i++)if(!before[i].SequenceEqual(File.ReadAllBytes(paths[i])))throw new InvalidOperationException("Controller changed on repeated build: "+paths[i]);
   Debug.Log("AGENT_RESULT repeated builds preserve all "+paths.Length+" controller assets byte-for-byte");
  }
  public static void PromoteReviewedRoster() {
   PreserveIslandSourceResolution();TripoPlayableCandidateBuilder.Build();TripoIslandCandidateBuilder.Build();TripoRangerCandidateBuilder.Build();TripoBoneCandidateBuilder.Build();TripoMasterCandidateBuilder.Build();Prepare();
   var review=AssetDatabase.LoadAssetAtPath<GameContent>(Folder+"/ReviewContent.asset");
   var shipping=AssetDatabase.LoadAssetAtPath<GameContent>("Assets/LighFight/Content/GameContent.asset");
   foreach(int index in new[]{0,2,3,4,5}){var candidate=review.fighters[index];var animator=candidate.GetComponentInChildren<Animator>();var skin=candidate.GetComponentInChildren<SkinnedMeshRenderer>();if(!candidate.GetComponent<FighterView>().IsReady||!animator.avatar.isHuman||skin.sharedMesh.triangles.Length/3!=100000)throw new InvalidOperationException("Roster promotion gate failed at "+index);shipping.fighters[index]=candidate;}
   EditorUtility.SetDirty(shipping);AssetDatabase.SaveAssets();Debug.Log("AGENT_RESULT main roster now uses calibrated Tripo fighters at0,2,3,4,5; device delivery pending");
  }
  public static void Build()=>BuildTargetReview(BuildTarget.WebGL,"../.tools/web-tripo-review");
  public static void BuildMac()=>BuildTargetReview(BuildTarget.StandaloneOSX,"../.tools/mac-tripo-review/LighFight.app");
  static void BuildTargetReview(BuildTarget target,string destination) {
   if(!BuildPipeline.IsBuildTargetSupported(BuildPipeline.GetBuildTargetGroup(target),target))throw new InvalidOperationException("Build module unavailable: "+target);
   PreserveIslandSourceResolution();
   TripoPlayableCandidateBuilder.Build();TripoIslandCandidateBuilder.Build();TripoRangerCandidateBuilder.Build();TripoBoneCandidateBuilder.Build();TripoMasterCandidateBuilder.Build();
   Prepare();
   string output=Path.GetFullPath(destination);Directory.CreateDirectory(Path.GetDirectoryName(output));
   // Uncompressed local output works with a normal HTTP server, without custom CDN headers.
   var compression=PlayerSettings.WebGL.compressionFormat;bool fallback=PlayerSettings.WebGL.decompressionFallback;string template=PlayerSettings.WebGL.template;
   try {
    PlayerSettings.WebGL.compressionFormat=WebGLCompressionFormat.Disabled;
    PlayerSettings.WebGL.decompressionFallback=false;
    if(target==BuildTarget.WebGL)PlayerSettings.WebGL.template="PROJECT:LighFightReview";
    var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{ScenePath},target=target,locationPathName=output,options=BuildOptions.Development});
    if(report.summary.result!=UnityEditor.Build.Reporting.BuildResult.Succeeded)throw new InvalidOperationException("Web review build failed: "+report.summary.result);
    Debug.Log("AGENT_RESULT "+target+" review build succeeded: "+output);
   }finally{PlayerSettings.WebGL.compressionFormat=compression;PlayerSettings.WebGL.decompressionFallback=fallback;PlayerSettings.WebGL.template=template;AssetDatabase.SaveAssets();}
  }
 }
}

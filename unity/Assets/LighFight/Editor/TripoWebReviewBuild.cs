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
   content.fighters=(GameObject[])source.fighters.Clone();content.arenas=(GameObject[])source.arenas.Clone();content.fighters[4]=candidate;
   EditorUtility.SetDirty(content);
   var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
   new GameObject("LighFight Host Bridge").AddComponent<HostBridge>();
   var prototype=new GameObject("Tripo local battle review").AddComponent<Prototype>();prototype.content=content;
   prototype.gameObject.AddComponent<TripoReviewProbe>();
   var fields=new SerializedObject(prototype);fields.FindProperty("selectedFighter").intValue=4;fields.FindProperty("selectedArena").intValue=4;fields.ApplyModifiedPropertiesWithoutUndo();
   EditorSceneManager.SaveScene(scene,ScenePath);AssetDatabase.SaveAssets();
   Debug.Log("AGENT_RESULT Tripo test scene prepared; shipping roster unchanged");
  }
  public static void Build()=>BuildTargetReview(BuildTarget.WebGL,"../.tools/web-tripo-review");
  public static void BuildMac()=>BuildTargetReview(BuildTarget.StandaloneOSX,"../.tools/mac-tripo-review/LighFight.app");
  static void BuildTargetReview(BuildTarget target,string destination) {
   if(!BuildPipeline.IsBuildTargetSupported(BuildPipeline.GetBuildTargetGroup(target),target))throw new InvalidOperationException("Build module unavailable: "+target);
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

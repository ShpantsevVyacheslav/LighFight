using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using LighFight.Runtime;
namespace LighFight.Editor {
 // Explicit post-review authoring step. Never called by the pose baker or model importer.
 public static class MeetingSpectatorInstall {
  public static void RunInstallAndPromote(){Run();MeetingDetailsSetup.PromoteValidated();RefreshEmbedded();}
  public static void RunInstallAndRefresh(){Run();RefreshEmbedded();}
  public static void RefreshEmbedded(){
   var source=AssetDatabase.LoadAssetAtPath<GameContent>("Assets/LighFight/Content/GameContent.asset");
   var settings=UnityEditor.AddressableAssets.AddressableAssetSettingsDefaultObject.GetSettings(true);
   var catalog=ContentBuild.ConfigureEmbeddedContent(source,settings);
   var group=settings.FindGroup("LighFight Embedded Spectators");if(group==null||group.entries.Count<6)throw new InvalidOperationException("Selected-only spectator addresses not registered");
   if(catalog.arenaAddresses.Length==0)throw new InvalidOperationException("Embedded arena address missing");
   Debug.Log("AGENT_RESULT embedded_catalog_refreshed=true spectator_addresses="+group.entries.Count+" phone_build_unchanged=true");
  }
  static Material AuthoredMaterial(string folder,string name,string shaderName,Color color){
   string path=folder+"/"+name+".mat";var material=AssetDatabase.LoadAssetAtPath<Material>(path);if(!material){var shader=Shader.Find(shaderName);if(!shader)throw new InvalidOperationException("Missing hookah shader "+shaderName);material=new Material(shader);AssetDatabase.CreateAsset(material,path);}material.color=color;EditorUtility.SetDirty(material);return material;
  }
  public static void Run(){
   string approved=Environment.GetEnvironmentVariable("LF_SEATED_APPROVED_INDICES");if(string.IsNullOrEmpty(approved))throw new InvalidOperationException("Specify indices after actual seated/context review");
   string path=(Environment.GetEnvironmentVariable("LF_MEETING_CANDIDATE_FOLDER")??"Assets/LighFight/Art/Drafts/MeetingMeshyV1")+"/arena.prefab";
   var arena=PrefabUtility.LoadPrefabContents(path);
   try{
    var crowd=arena.GetComponent<MeetingSpectators>();if(!crowd||!crowd.hookah||!crowd.hookah.outlet)throw new InvalidOperationException("Arena hookah/socket not authored");
    string materialFolder=Path.GetDirectoryName(path).Replace('\\','/');
    crowd.hookah.hoseMaterial=AuthoredMaterial(materialFolder,"spectator-hose","Standard",new Color(.025f,.025f,.028f));
    crowd.hookah.mouthpieceMaterial=AuthoredMaterial(materialFolder,"spectator-mouthpiece","Standard",new Color(.44f,.27f,.12f));
    crowd.hookah.smokeMaterial=AuthoredMaterial(materialFolder,"spectator-smoke","Sprites/Default",Color.white);
    var candidates=new List<MeetingSpectators.Candidate>();
    foreach(string token in approved.Split(',')){
     int index=int.Parse(token);if(index==10)throw new InvalidOperationException("Summer seated mesh rejected; requires a separate deformation fix");
     var candidate=new MeetingSpectators.Candidate{fighterIndex=index};
     foreach(string variant in new[]{"idle","mouthpiece"}){
      string asset="Assets/LighFight/MeetingSpectatorReview/seated-"+index+"-"+variant+".prefab";
      var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(asset);if(!prefab)throw new InvalidOperationException("Missing seated asset "+asset);
      if(prefab.GetComponentInChildren<Animator>(true)||prefab.GetComponentInChildren<SkinnedMeshRenderer>(true))throw new InvalidOperationException("Background must be a static baked surface");
      string evidence=Path.GetFullPath("../art/drafts/meeting-seated-v1/seated-"+index+"-"+variant+".png");if(!File.Exists(evidence))throw new InvalidOperationException("Missing actual pose capture");
      var pose=prefab.GetComponent<SeatedSpectatorPose>();if(!pose)throw new InvalidOperationException("Missing seated provenance");
      pose.seatedPoseVisuallyAccepted=true;pose.poseEvidence=evidence+"; context: meeting-meshy-v1 Unity review";EditorUtility.SetDirty(pose);EditorUtility.SetDirty(prefab);PrefabUtility.SavePrefabAsset(prefab);
      string address="lighfight/spectator/"+AssetDatabase.AssetPathToGUID(asset);
      if(variant=="idle"){candidate.address=address;candidate.fighterId=pose.fighterId;}else candidate.smokingAddress=address;
     }
     candidates.Add(candidate);
    }
    crowd.candidates=candidates.ToArray();crowd.maximumSpectators=2;
    PrefabUtility.SaveAsPrefabAsset(arena,path);AssetDatabase.SaveAssets();
    Debug.Log("AGENT_RESULT accepted_static_spectators="+candidates.Count+" arena="+path+" no_roster_mesh_changes=true");
   }finally{PrefabUtility.UnloadPrefabContents(arena);}
  }
 }
}

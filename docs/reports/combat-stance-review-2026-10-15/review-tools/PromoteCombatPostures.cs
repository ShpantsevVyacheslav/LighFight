using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Security.Cryptography;
using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using LighFight.Runtime;
public static class PromoteCombatPostures {
 [Serializable]sealed class Row{public int index;public string name,stanceSource,stanceOutput;}
 [Serializable]sealed class Rows{public Row[] rows;}
 [Serializable]sealed class HashRecord{public string path,sha256;}
 [Serializable]sealed class Freeze{public HashRecord[] files;}
 [Serializable]sealed class SourceManifest{public string sourcePrefab,sourceController;public HashRecord[] preservedInputs;}
 [Serializable]sealed class Change{public string source,before,after,backup,metaSHA,controller;}
 [Serializable]sealed class Result{public string status;public Change[] changes;}
 static string Hash(string path){using(var sha=SHA256.Create())return BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(path))).Replace("-","").ToLowerInvariant();}
 static void Need(bool ok,string message){if(!ok)throw new InvalidOperationException(message);}
 static string Relative(Transform t,Transform root){return AnimationUtility.CalculateTransformPath(t,root);}
 static string RenderIdentity(GameObject root){var animator=root.GetComponentInChildren<Animator>();return AssetDatabase.GetAssetPath(animator.avatar)+"\n"+string.Join("\n",root.GetComponentsInChildren<SkinnedMeshRenderer>(true).Select(r=>Relative(r.transform,root.transform)+"|"+AssetDatabase.GetAssetPath(r.sharedMesh)+"|"+string.Join(",",r.sharedMaterials.Select(AssetDatabase.GetAssetPath))+"|"+Relative(r.rootBone,root.transform)+"|"+string.Join(",",r.bones.Select(b=>Relative(b,root.transform)))));}
 static void CopyArray(Component from,Component to,string field){Need(from&&to,"Required pose component missing");var src=new SerializedObject(from);var dst=new SerializedObject(to);var value=src.FindProperty(field);Need(value!=null&&value.isArray,"Required serialized pose array missing: "+field);dst.CopyFromSerializedProperty(value);dst.ApplyModifiedPropertiesWithoutUndo();}
 public static void Run(){
  string repository=Path.GetFullPath(Path.Combine(Application.dataPath,"../.."));Need(Application.dataPath==Path.Combine(repository,"unity/Assets"),"Main project only");
  string evidence=Path.Combine(repository,".tools/combat-stance-source-review/ActualRoster-CorrectSchedule");Need(File.Exists(Path.Combine(evidence,"COMPLETED-NOT-VISUALLY-ACCEPTED.txt"))&&!File.Exists(Path.Combine(evidence,"EOF-RED.txt"))&&!File.Exists(Path.Combine(evidence,"PLAYER-ERROR.txt")),"Completed error-free actual roster evidence required");
  var frozen=JsonUtility.FromJson<Freeze>(File.ReadAllText(Path.Combine(repository,".tools/combat-posture-promotion/review/reviewed-build-inputs.json")));Need(frozen.files.Length>=100,"Review input freeze required");foreach(var file in frozen.files)Need(Hash(Path.Combine(repository,file.path))==file.sha256,"Reviewed candidate/runtime changed: "+file.path);
  var coverage=File.ReadAllLines(Path.Combine(evidence,"case-results.tsv"));Need(coverage.Length==110&&coverage.Distinct().Count()==110,"All11 actors,2 variants,5 scenarios required");Need(Directory.GetFiles(evidence,"*.json").Length==9900,"Full frame coverage required");
  var rows=JsonUtility.FromJson<Rows>("{\"rows\":"+File.ReadAllText("Assets/CombatStanceSourceReview/Editor/all11-review-inputs.json")+"}").rows;Need(rows.Length==11,"Full roster required");
  string output=Path.Combine(repository,".tools/combat-posture-promotion/Evidence");Directory.CreateDirectory(output);var changes=new List<Change>();
  // Validate all sources before the first source-prefab write.
  foreach(var row in rows){var manifest=JsonUtility.FromJson<SourceManifest>(File.ReadAllText(row.stanceOutput+"/manifest.json"));Need(manifest.sourcePrefab==row.stanceSource,"Mismatched source manifest");foreach(var item in manifest.preservedInputs)Need(Hash(item.path)==item.sha256,"Source changed since candidate review: "+item.path);}
  try{foreach(var row in rows){
   var candidate=AssetDatabase.LoadAssetAtPath<GameObject>(row.stanceOutput+"/IslandStanceCandidate.UNACCEPTED.prefab");Need(candidate,"Approved candidate missing");var ca=candidate.GetComponentInChildren<Animator>();var controller=ca.runtimeAnimatorController as AnimatorController;Need(controller&&controller.layers.Length==2,"Expected two-layer source controller");var arms=controller.layers[1];Need(arms.name==FighterView.CombatPostureLayerName&&arms.stateMachine.name==arms.name,"Arms state path mismatch");
   var change=new Change{source=row.stanceSource,before=Hash(row.stanceSource),backup=Path.Combine(output,row.index.ToString("D2")+"-before.prefab"),metaSHA=Hash(row.stanceSource+".meta"),controller=AssetDatabase.GetAssetPath(controller)};File.Copy(row.stanceSource,change.backup,false);
   var actor=PrefabUtility.LoadPrefabContents(row.stanceSource);try{string identity=RenderIdentity(actor);var a=actor.GetComponentInChildren<Animator>();Need(a.avatar==ca.avatar,"Own Avatar differs");a.runtimeAnimatorController=controller;CopyArray(candidate.GetComponent<FighterView>(),actor.GetComponent<FighterView>(),"nativeClips");CopyArray(candidate.GetComponent<NativeFingerPoseDriver>(),actor.GetComponent<NativeFingerPoseDriver>(),"closedStates");Need(RenderIdentity(actor)==identity,"Original mesh/material/bone bindings changed");changes.Add(change);PrefabUtility.SaveAsPrefabAsset(actor,row.stanceSource);change.after=Hash(row.stanceSource);}finally{PrefabUtility.UnloadPrefabContents(actor);}
   Need(Hash(row.stanceSource+".meta")==change.metaSHA,"Prefab GUID changed");Debug.Log("COMBAT_POSTURE_PROMOTED "+row.name);
  }AssetDatabase.SaveAssets();File.WriteAllText(Path.Combine(output,"promotion.json"),JsonUtility.ToJson(new Result{status="CODE_AND_LOCAL_REVIEW_ONLY_PHONE_ACCEPTANCE_PENDING",changes=changes.ToArray()},true));}
  catch{foreach(var change in changes){if(change.after==null){Need(Hash(change.source)==change.before,"Uncertain save; preserved backup needs review: "+change.backup);continue;}Need(Hash(change.source)==change.after,"Concurrent prefab change prevents automatic rollback: "+change.source);File.Copy(change.backup,change.source,true);AssetDatabase.ImportAsset(change.source,ImportAssetOptions.ForceUpdate);}throw;}
 }
}

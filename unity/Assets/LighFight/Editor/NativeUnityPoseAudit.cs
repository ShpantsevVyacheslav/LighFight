using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
namespace LighFight.Editor {
 public static class NativeUnityPoseAudit {
  [Serializable] public class Sample {public Vector3[] points;}
  [Serializable] public class Reference {public float duration;public string[] boneNames;public Vector3[] rest;public Sample[] samples;}
  [Serializable] public class Result {public bool resampled;public int samples,bones;public float restError,durationError,maxPoseError;public string coordinateMap;public bool passed;}
  public static void Run() {
   const string path="Assets/LighFight/Art/Drafts/IslandWebVisible/direct-jab.fbx";
   var reference=JsonUtility.FromJson<Reference>(File.ReadAllText(Path.ChangeExtension(path,"reference.json")));
   var importer=(ModelImporter)AssetImporter.GetAtPath(path);
   importer.animationType=ModelImporterAnimationType.Generic;importer.avatarSetup=ModelImporterAvatarSetup.CreateFromThisModel;importer.animationCompression=ModelImporterAnimationCompression.Off;
   var results=new System.Collections.Generic.List<Result>();
   try {
    foreach(bool resample in new[]{true,false}) {
     importer.resampleCurves=resample;importer.SaveAndReimport();
     var actor=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(path));
     try {
      foreach(var animator in actor.GetComponentsInChildren<Animator>()){animator.Rebind();animator.Update(0);animator.enabled=false;}
      var all=actor.GetComponentsInChildren<Transform>().ToDictionary(t=>t.name);
      var bones=reference.boneNames.Select(n=>all[n]).ToArray();
      var skin=actor.GetComponentInChildren<SkinnedMeshRenderer>();
      var rest=bones.Select(t=>skin.transform.localToWorldMatrix.MultiplyPoint3x4(skin.sharedMesh.bindposes[Array.IndexOf(skin.bones,t)].inverse.MultiplyPoint3x4(Vector3.zero))).ToArray();
      int[][] axes={new[]{0,1,2},new[]{0,2,1},new[]{1,0,2},new[]{1,2,0},new[]{2,0,1},new[]{2,1,0}};
      float best=float.PositiveInfinity;int[] chosen=null;int signs=0;
      foreach(var permutation in axes)for(int sign=0;sign<8;sign++) {
       float error=reference.rest.Select((p,i)=>Vector3.Distance(Map(p,permutation,sign),rest[i])).Max();
       if(error<best){best=error;chosen=permutation;signs=sign;}
      }
      var clip=AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>().Single(c=>!c.name.StartsWith("__preview"));NativeFighterImport.ValidateClip(actor,clip);
      float maximum=0;
      for(int sample=0;sample<reference.samples.Length;sample++) {
       clip.SampleAnimation(actor,reference.duration*sample/(reference.samples.Length-1));
       for(int i=0;i<bones.Length;i++)maximum=Mathf.Max(maximum,Vector3.Distance(Map(reference.samples[sample].points[i],chosen,signs),bones[i].position));
      }
      var result=new Result{resampled=resample,samples=reference.samples.Length,bones=bones.Length,restError=best,durationError=Mathf.Abs(clip.length-reference.duration),maxPoseError=maximum,coordinateMap=string.Join(",",chosen)+" signs "+signs,passed=best<.0001f&&maximum<.001f&&Mathf.Abs(clip.length-reference.duration)<.0001f};
      results.Add(result);Debug.Log(JsonUtility.ToJson(result));
     } finally {UnityEngine.Object.DestroyImmediate(actor);}
    }
   } finally {importer.resampleCurves=false;importer.SaveAndReimport();}
   Directory.CreateDirectory("../art/previews/source-quality");File.WriteAllText("../art/previews/source-quality/native-unity-pose-parity.json","["+string.Join(",",results.Select(JsonUtility.ToJson))+"]");
   if(!results.Single(r=>!r.resampled).passed)throw new Exception("Native Unity pose parity failed; candidate must remain unpromoted");
  }
  static Vector3 Map(Vector3 source,int[] axes,int signs)=>new Vector3(source[axes[0]]*((signs&1)==0?1:-1),source[axes[1]]*((signs&2)==0?1:-1),source[axes[2]]*((signs&4)==0?1:-1));
 }
}

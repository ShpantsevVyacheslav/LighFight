using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
namespace LighFight.Editor {
 public static class TripoPoseParityAudit {
  [Serializable] public class Sample {public Vector3[] points;}
  [Serializable] public class Reference {public float duration;public string[] boneNames;public Vector3[] rest;public Sample[] samples,vertexSamples;public int[] vertexIndices;public Vector3[] vertexRest;public Vector2[] vertexUV;}
  [Serializable] public class Result {public bool resampled;public int samples,bones,verticesPerSample,importedVertices,triangles;public float restError,durationError,maxPoseError,maxVertexError;public string coordinateMap;public bool passed;}
  public static void RunPack() {
   string model=Environment.GetEnvironmentVariable("LIGHFIGHT_NATIVE_AUDIT_MODEL");
   if(string.IsNullOrEmpty(model))throw new ArgumentException("Set native audit model");
   string report=Environment.GetEnvironmentVariable("LIGHFIGHT_NATIVE_AUDIT_REPORT");
   var references=Directory.GetFiles(Path.GetDirectoryName(model),"*.reference.json").OrderBy(p=>p).ToArray();
   if(references.Length!=8)throw new InvalidOperationException("Require all eight native reference clips");
   var results=new System.Collections.Generic.List<string>();
   string oldReference=Environment.GetEnvironmentVariable("LIGHFIGHT_NATIVE_AUDIT_REFERENCE"),oldClip=Environment.GetEnvironmentVariable("LIGHFIGHT_NATIVE_AUDIT_CLIP");
   try {foreach(string reference in references){
    string clip=Path.GetFileName(reference).Replace(".reference.json","");
    string output=Path.Combine(Path.GetDirectoryName(report),clip+"-parity.json");
    Environment.SetEnvironmentVariable("LIGHFIGHT_NATIVE_AUDIT_REFERENCE",reference);Environment.SetEnvironmentVariable("LIGHFIGHT_NATIVE_AUDIT_CLIP",clip);Environment.SetEnvironmentVariable("LIGHFIGHT_NATIVE_AUDIT_REPORT",output);
    Run();results.Add("{\"clip\":\""+clip+"\",\"results\":"+File.ReadAllText(output)+"}");
   }File.WriteAllText(report,"["+string.Join(",",results)+"]");Debug.Log("NATIVE_PACK_PARITY 8 clips passed");}
   finally {Environment.SetEnvironmentVariable("LIGHFIGHT_NATIVE_AUDIT_REFERENCE",oldReference);Environment.SetEnvironmentVariable("LIGHFIGHT_NATIVE_AUDIT_CLIP",oldClip);Environment.SetEnvironmentVariable("LIGHFIGHT_NATIVE_AUDIT_REPORT",report);}
  }
  public static void Run() {
   QualitySettings.skinWeights=SkinWeights.Unlimited;
   string path=Environment.GetEnvironmentVariable("LIGHFIGHT_NATIVE_AUDIT_MODEL")??"Assets/LighFight/Art/Drafts/IslandWebVisible/direct-jab.fbx";
   var reference=JsonUtility.FromJson<Reference>(File.ReadAllText(Environment.GetEnvironmentVariable("LIGHFIGHT_NATIVE_AUDIT_REFERENCE")??Path.ChangeExtension(path,"reference.json")));
   var importer=(ModelImporter)AssetImporter.GetAtPath(path);
   NativeFighterImport.PrepareImporter(importer);
   var results=new System.Collections.Generic.List<Result>();
   try {
    foreach(bool resample in new[]{true,false}) {
     importer.resampleCurves=resample;importer.SaveAndReimport();
     var actor=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(path));
     try {
      foreach(var animator in actor.GetComponentsInChildren<Animator>()){animator.Rebind();animator.Update(0);animator.enabled=false;}
      var all=actor.GetComponentsInChildren<Transform>().ToDictionary(t=>t.name);
      var skin=actor.GetComponentInChildren<SkinnedMeshRenderer>();
      var boneIndices=Enumerable.Range(0,reference.boneNames.Length).Where(i=>all.ContainsKey(reference.boneNames[i])&&Array.IndexOf(skin.bones,all[reference.boneNames[i]])>=0).ToArray();
      var bones=boneIndices.Select(i=>all[reference.boneNames[i]]).ToArray();
      var rest=bones.Select(t=>skin.transform.localToWorldMatrix.MultiplyPoint3x4(skin.sharedMesh.bindposes[Array.IndexOf(skin.bones,t)].inverse.MultiplyPoint3x4(Vector3.zero))).ToArray();
      int[][] axes={new[]{0,1,2},new[]{0,2,1},new[]{1,0,2},new[]{1,2,0},new[]{2,0,1},new[]{2,1,0}};
      float best=float.PositiveInfinity;int[] chosen=null;int signs=0;
      foreach(var permutation in axes)for(int sign=0;sign<8;sign++) {
       float error=boneIndices.Select((i,j)=>Vector3.Distance(Map(reference.rest[i],permutation,sign),rest[j])).Max();
       if(error<best){best=error;chosen=permutation;signs=sign;}
      }
      var nativeClips=AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>().Where(c=>!c.name.StartsWith("__preview")).ToArray();
      Debug.Log("PARITY_CLIPS "+string.Join(",",nativeClips.Select(c=>c.name)));
      var clip=nativeClips.Length==1?nativeClips[0]:nativeClips.Single(c=>c.name.Split('|').Last()==(Environment.GetEnvironmentVariable("LIGHFIGHT_NATIVE_AUDIT_CLIP")??"box_01"));NativeFighterImport.ValidateClip(actor,clip);
      var sourceVertices=skin.sharedMesh.vertices;var sourceUV=skin.sharedMesh.uv;var vertexMap=new int[reference.vertexRest.Length];
      for(int i=0;i<vertexMap.Length;i++) {
       float nearest=float.PositiveInfinity;int index=-1;var target=Map(reference.vertexRest[i],chosen,signs);
       for(int j=0;j<sourceVertices.Length;j++) {
        if(Vector2.Distance(sourceUV[j],reference.vertexUV[i])>.00001f)continue;
        float distance=Vector3.Distance(skin.transform.TransformPoint(sourceVertices[j]),target);
        if(distance<nearest){nearest=distance;index=j;}
       }
       if(nearest>.00001f)throw new Exception("Original rest/UV vertex not found in Unity: "+i+" error "+nearest);
       vertexMap[i]=index;
      }
      float maximum=0,vertexMaximum=0;var sampledMesh=new Mesh();
      for(int sample=0;sample<reference.samples.Length;sample++) {
       clip.SampleAnimation(actor,reference.duration*sample/(reference.samples.Length-1));
       skin.BakeMesh(sampledMesh,false);
       var scale=skin.transform.localScale;
       var sampledWorld=skin.transform.localToWorldMatrix*Matrix4x4.Scale(new Vector3(1/scale.x,1/scale.y,1/scale.z));
       var vertices=sampledMesh.vertices;
       for(int i=0;i<reference.vertexIndices.Length;i++)vertexMaximum=Mathf.Max(vertexMaximum,Vector3.Distance(Map(reference.vertexSamples[sample].points[i],chosen,signs),sampledWorld.MultiplyPoint3x4(vertices[vertexMap[i]])));
       for(int i=0;i<bones.Length;i++)maximum=Mathf.Max(maximum,Vector3.Distance(Map(reference.samples[sample].points[boneIndices[i]],chosen,signs),bones[i].position));
      }
      var result=new Result{resampled=resample,samples=reference.samples.Length,bones=bones.Length,verticesPerSample=reference.vertexIndices.Length,importedVertices=skin.sharedMesh.vertexCount,triangles=skin.sharedMesh.triangles.Length/3,restError=best,durationError=Mathf.Abs(clip.length-reference.duration),maxPoseError=maximum,maxVertexError=vertexMaximum,coordinateMap=string.Join(",",chosen)+" signs "+signs,passed=best<.0001f&&maximum<.001f&&vertexMaximum<.001f&&Mathf.Abs(clip.length-reference.duration)<.0001f};
      UnityEngine.Object.DestroyImmediate(sampledMesh);results.Add(result);Debug.Log(JsonUtility.ToJson(result));
     } finally {UnityEngine.Object.DestroyImmediate(actor);}
    }
   } finally {importer.resampleCurves=false;importer.SaveAndReimport();}
   Directory.CreateDirectory("../art/previews/source-quality");File.WriteAllText(Environment.GetEnvironmentVariable("LIGHFIGHT_NATIVE_AUDIT_REPORT")??"../art/previews/source-quality/native-unity-pose-parity.json","["+string.Join(",",results.Select(JsonUtility.ToJson))+"]");
   if(!results.Any(r=>r.passed))throw new Exception("Native Unity pose parity failed; candidate must remain unpromoted");
  }
  static Vector3 Map(Vector3 source,int[] axes,int signs)=>new Vector3(source[axes[0]]*((signs&1)==0?1:-1),source[axes[1]]*((signs&2)==0?1:-1),source[axes[2]]*((signs&4)==0?1:-1));
 }
}

using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
namespace LighFight.Editor {
 public static class NativeUnityPoseAudit {
  [Serializable] public class Sample {public Vector3[] points;}
  [Serializable] public class Reference {public float duration;public string[] boneNames;public Vector3[] rest;public Sample[] samples,vertexSamples;public int[] vertexIndices;public Vector3[] vertexRest;public Vector2[] vertexUV;}
  [Serializable] public class Result {public bool resampled;public int samples,bones,verticesPerSample,importedVertices,triangles;public float restError,durationError,maxPoseError,maxVertexError;public string coordinateMap;public bool passed;}
  public static void Run() {
   QualitySettings.skinWeights=SkinWeights.Unlimited;
   string path=Environment.GetEnvironmentVariable("LIGHFIGHT_NATIVE_AUDIT_MODEL")??"Assets/LighFight/Art/Drafts/IslandWebVisible/direct-jab.fbx";
   var reference=JsonUtility.FromJson<Reference>(File.ReadAllText(Path.ChangeExtension(path,"reference.json")));
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
       for(int i=0;i<bones.Length;i++)maximum=Mathf.Max(maximum,Vector3.Distance(Map(reference.samples[sample].points[i],chosen,signs),bones[i].position));
      }
      var result=new Result{resampled=resample,samples=reference.samples.Length,bones=bones.Length,verticesPerSample=reference.vertexIndices.Length,importedVertices=skin.sharedMesh.vertexCount,triangles=skin.sharedMesh.triangles.Length/3,restError=best,durationError=Mathf.Abs(clip.length-reference.duration),maxPoseError=maximum,maxVertexError=vertexMaximum,coordinateMap=string.Join(",",chosen)+" signs "+signs,passed=best<.0001f&&maximum<.001f&&vertexMaximum<.001f&&Mathf.Abs(clip.length-reference.duration)<.0001f};
      UnityEngine.Object.DestroyImmediate(sampledMesh);results.Add(result);Debug.Log(JsonUtility.ToJson(result));
     } finally {UnityEngine.Object.DestroyImmediate(actor);}
    }
   } finally {importer.resampleCurves=false;importer.SaveAndReimport();}
   Directory.CreateDirectory("../art/previews/source-quality");File.WriteAllText(Environment.GetEnvironmentVariable("LIGHFIGHT_NATIVE_AUDIT_REPORT")??"../art/previews/source-quality/native-unity-pose-parity.json","["+string.Join(",",results.Select(JsonUtility.ToJson))+"]");
   if(!results.Single(r=>!r.resampled).passed)throw new Exception("Native Unity pose parity failed; candidate must remain unpromoted");
  }
  static Vector3 Map(Vector3 source,int[] axes,int signs)=>new Vector3(source[axes[0]]*((signs&1)==0?1:-1),source[axes[1]]*((signs&2)==0?1:-1),source[axes[2]]*((signs&4)==0?1:-1));
 }
}

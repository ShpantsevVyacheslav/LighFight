using System;using System.IO;using System.Linq;using System.Collections.Generic;using UnityEngine;using UnityEditor;using LighFight.Runtime;
public static class UniqueDanceFootCalibrationWitness {
 public static void Run(){
  const string folder="Assets/UniqueDancesReview/Fighter-9",src="Assets/UniqueDancesReview/GenericSources/action-74.fbx",output="../art/source/unique-dances-20261018/audit/generic-sailor";var rows=new List<string>{"kind,seconds,pointCount,quality,skinWeights,fullMeshMinBefore,supportMin,correction,fullMeshMinAfter,lowestVertex,influences"};
  var raw=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(src));var clip=AssetDatabase.LoadAllAssetsAtPath(src).OfType<AnimationClip>().Single(c=>!c.name.StartsWith("__preview"));var mesh=new Mesh();var vertices=new List<Vector3>();
  try{
   var rawAnimator=raw.GetComponentInChildren<Animator>();if(rawAnimator)rawAnimator.enabled=false;var skin=raw.GetComponentInChildren<SkinnedMeshRenderer>();
   foreach(float t in new[]{0f,clip.length*.25f,clip.length*.5f,clip.length*.75f,clip.length,5.116136f}){clip.SampleAnimation(raw,t);int index;float min=Floor(skin,mesh,vertices,out index);rows.Add("rawGeneric,"+t+",0,"+skin.quality+","+QualitySettings.skinWeights+","+min+",NaN,0,"+min+","+index+","+Weights(skin,index));}
   File.WriteAllLines(output+"/foot-canonical-calibration-witness.csv",rows);
   foreach(string variant in new[]{"Review.exact-generic.prefab","Review.exact-generic-anatomic-v2.prefab","Review.exact-generic-anatomic-v3.prefab"}){
    var actor=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(folder+"/"+variant));try{foreach(var b in actor.GetComponentsInChildren<MonoBehaviour>(true))b.enabled=false;actor.GetComponentInChildren<Animator>().enabled=false;var driver=actor.GetComponent<ExactGenericDanceRetarget>();var ground=actor.GetComponent<CandidateFootGrounding>();skin=actor.GetComponentInChildren<SkinnedMeshRenderer>();var defaultPoints=ground.shoePoints;var nativePoints=actor.GetComponent<NativeSupportOverlay>().supportPoints;
     foreach(float t in new[]{0f,clip.length*.25f,clip.length*.5f,clip.length*.75f,clip.length,5.116136f})foreach(bool native in new[]{false,true}){ground.shoePoints=native?nativePoints:defaultPoints;driver.EvaluateForReview(t);int index;float min=Floor(skin,mesh,vertices,out index);float support=ground.MeasureMinimum();ground.ApplyForReview();int ignored;float after=Floor(skin,mesh,vertices,out ignored);rows.Add(variant+(native?"-nativePoints":"-sharedPoints")+","+t+","+ground.shoePoints.Length+","+skin.quality+","+QualitySettings.skinWeights+","+min+","+support+","+ground.LastCorrection+","+after+","+index+","+Weights(skin,index));}
    File.WriteAllLines(output+"/foot-canonical-calibration-witness.csv",rows);
    }finally{foreach(var b in actor.GetComponents<MonoBehaviour>())if(b is IActorCurveWarmup warm)warm.Dispose();UnityEngine.Object.DestroyImmediate(actor);}
   }
   File.WriteAllLines(output+"/foot-canonical-calibration-witness.csv",rows);Debug.Log("AGENT_RESULT rawGeneric andtargetgirdle/sole calibration witnesses retained");
  }finally{UnityEngine.Object.DestroyImmediate(raw);UnityEngine.Object.DestroyImmediate(mesh);}
 }
 static float Floor(SkinnedMeshRenderer skin,Mesh mesh,List<Vector3> vertices,out int index){skin.BakeMesh(mesh);mesh.GetVertices(vertices);float min=float.PositiveInfinity;index=-1;for(int i=0;i<vertices.Count;i++){float y=skin.transform.TransformPoint(vertices[i]).y;if(y<min){min=y;index=i;}}return min;}
 static string Weights(SkinnedMeshRenderer skin,int index){var w=skin.sharedMesh.boneWeights[index];return string.Join("|",new[]{w.boneIndex0,w.boneIndex1,w.boneIndex2,w.boneIndex3}.Zip(new[]{w.weight0,w.weight1,w.weight2,w.weight3},(b,v)=>skin.bones[b].name+":"+v));}
}

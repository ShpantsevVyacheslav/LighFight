using System;using System.IO;using System.Linq;using System.Collections.Generic;using UnityEngine;using UnityEditor;using LighFight.Runtime;
public static class UniqueDanceSkinScaleWitness {
 public static void Run(){
  const string folder="Assets/UniqueDancesReview/Fighter-9";var rows=new List<string>{"variant,seconds,rootScale,rendererLossyScale,nativeRigScale,supportCount,supportMin,lbsWorldMin,bakeDefaultThenTransform,bakeFalseThenTransform,bakeTrueThenTransform,lbsAfterGround,bakeFalseAfterGround"};
  foreach(string variant in new[]{"Review.exact-generic.prefab","Review.exact-generic-anatomic-v2.prefab","Review.exact-generic-anatomic-v3.prefab"}){
   var actor=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(folder+"/"+variant));var mesh=new Mesh();try{foreach(var b in actor.GetComponentsInChildren<MonoBehaviour>(true))b.enabled=false;actor.GetComponentInChildren<Animator>().enabled=false;var skin=actor.GetComponentInChildren<SkinnedMeshRenderer>();var oracle=new OriginalSkinWorldOracle(skin);var driver=actor.GetComponent<ExactGenericDanceRetarget>();var ground=actor.GetComponent<CandidateFootGrounding>();ground.shoePoints=actor.GetComponent<NativeSupportOverlay>().supportPoints;var vertices=new List<Vector3>();
    foreach(float t in new[]{0f,5.116136f,7.6f}){driver.EvaluateForReview(t);int index;float lbs=oracle.Minimum(out index);float support=ground.MeasureMinimum();float def=Bake(skin,mesh,vertices,-1),no=Bake(skin,mesh,vertices,0),yes=Bake(skin,mesh,vertices,1);ground.ApplyForReview();float after=oracle.Minimum(out index);float bakedAfter=Bake(skin,mesh,vertices,0);rows.Add(variant+","+t+","+V(actor.transform.localScale)+","+V(skin.transform.lossyScale)+","+V(ground.nativeRig.localScale)+","+ground.shoePoints.Length+","+support+","+lbs+","+def+","+no+","+yes+","+after+","+bakedAfter);}
   }finally{foreach(var b in actor.GetComponents<MonoBehaviour>())if(b is IActorCurveWarmup w)w.Dispose();UnityEngine.Object.DestroyImmediate(actor);UnityEngine.Object.DestroyImmediate(mesh);}
  }
  File.WriteAllLines("../art/source/unique-dances-20261018/audit/generic-sailor/skin-scale-coordinate-witness.csv",rows);Debug.Log("AGENT_RESULT independent skin matrices and BakeMesh scale spaces compared");
 }
 static string V(Vector3 v)=>v.x+"|"+v.y+"|"+v.z;
 static float Bake(SkinnedMeshRenderer skin,Mesh mesh,List<Vector3> vertices,int mode){if(mode<0)skin.BakeMesh(mesh);else skin.BakeMesh(mesh,mode==1);mesh.GetVertices(vertices);float min=float.PositiveInfinity;foreach(var p in vertices)min=Mathf.Min(min,skin.transform.TransformPoint(p).y);return min;}
}

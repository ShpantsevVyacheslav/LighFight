using System;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using LighFight.Core;
using LighFight.Runtime;
public class SourceDetailTests {
 [TestCase(3,66364)] [TestCase(4,350000)]
 public void ArtistSourceDetailAndPbrSurviveAllAttackPoses(int id,int minimumTriangles){
  var prefab=AssetDatabase.LoadAssetAtPath<GameObject>($"Assets/LighFight/Art/Fighters/fighter-{id}.prefab");var obj=UnityEngine.Object.Instantiate(prefab);var baked=new Mesh();
  try{
   var skin=obj.GetComponentInChildren<SkinnedMeshRenderer>();Assert.GreaterOrEqual(skin.sharedMesh.triangles.Length/3,minimumTriangles,"Reduced body was reintroduced");
   var mat=skin.sharedMaterials[0];Assert.AreEqual("Standard",mat.shader.name);Assert.AreEqual(id==3?2048:4096,mat.mainTexture.width);Assert.IsNotNull(mat.GetTexture("_BumpMap"));Assert.AreEqual(id==3?2048:4096,mat.GetTexture("_BumpMap").width);Assert.IsNotNull(mat.GetTexture("_MetallicGlossMap"));Assert.IsTrue(mat.IsKeywordEnabled("_NORMALMAP"));Assert.IsTrue(mat.IsKeywordEnabled("_METALLICGLOSSMAP"));
   var counts=skin.sharedMesh.GetBonesPerVertex();var weights=skin.sharedMesh.GetAllBoneWeights();try{int offset=0;foreach(var count in counts){float sum=0;for(int w=0;w<count;w++)sum+=weights[offset++].weight;Assert.That(sum,Is.EqualTo(1).Within(.001f));}}finally{counts.Dispose();weights.Dispose();}
   if(id==3){Assert.AreEqual(66,skin.bones.Length);Assert.IsTrue(obj.GetComponent<FighterView>().UsesNativeMotion);Assert.IsNull(obj.GetComponent<JointCorrectives>());}
   var view=obj.GetComponent<FighterView>();var animator=obj.GetComponent<Animator>();
   for(int i=0;i<Moves.All.Length;i++){
    var fighter=new Fighter();fighter.Start(i);fighter.Frame=Moves.All[i].Startup;view.Pose(fighter,1,0);animator.Update(0);obj.GetComponent<JointCorrectives>()?.Apply();skin.BakeMesh(baked,false);
    foreach(var v in baked.vertices){Assert.IsFalse(float.IsNaN(v.x)||float.IsNaN(v.y)||float.IsNaN(v.z)||float.IsInfinity(v.x)||float.IsInfinity(v.y)||float.IsInfinity(v.z));}
    Assert.Less(baked.bounds.size.magnitude,5,"Exploded deformation: "+Moves.All[i].Id);
   }
  }finally{UnityEngine.Object.DestroyImmediate(baked);UnityEngine.Object.DestroyImmediate(obj);}
 }
}

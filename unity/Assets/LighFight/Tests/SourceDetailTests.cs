using System;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using LighFight.Core;
using LighFight.Runtime;
public class SourceDetailTests {
 [TestCase(3,300000)] [TestCase(4,350000)]
 public void ArtistSourceDetailAndPbrSurviveAllAttackPoses(int id,int minimumTriangles){
  var prefab=AssetDatabase.LoadAssetAtPath<GameObject>($"Assets/LighFight/Art/Fighters/fighter-{id}.prefab");var obj=UnityEngine.Object.Instantiate(prefab);var baked=new Mesh();
  try{
   var skin=obj.GetComponentInChildren<SkinnedMeshRenderer>();Assert.GreaterOrEqual(skin.sharedMesh.triangles.Length/3,minimumTriangles,"Reduced body was reintroduced");
   var mat=skin.sharedMaterials[0];Assert.AreEqual("Standard",mat.shader.name);Assert.AreEqual(4096,mat.mainTexture.width);Assert.IsNotNull(mat.GetTexture("_BumpMap"));Assert.AreEqual(4096,mat.GetTexture("_BumpMap").width);Assert.IsNotNull(mat.GetTexture("_MetallicGlossMap"));Assert.IsTrue(mat.IsKeywordEnabled("_NORMALMAP"));Assert.IsTrue(mat.IsKeywordEnabled("_METALLICGLOSSMAP"));
   foreach(var w in skin.sharedMesh.boneWeights)Assert.That(w.weight0+w.weight1+w.weight2+w.weight3,Is.EqualTo(1).Within(.001f));
   if(id==3){Assert.AreEqual(3,skin.sharedMaterials.Length);Assert.GreaterOrEqual(skin.sharedMesh.GetTriangles(2).Length/3,692,"Watch geometry was lost");}
   var view=obj.GetComponent<FighterView>();var animator=obj.GetComponent<Animator>();
   for(int i=0;i<Moves.All.Length;i++){
    var fighter=new Fighter();fighter.Start(i);fighter.Frame=Moves.All[i].Startup;view.Pose(fighter,1,0);animator.Update(0);obj.GetComponent<JointCorrectives>()?.Apply();skin.BakeMesh(baked);
    foreach(var v in baked.vertices){Assert.IsFalse(float.IsNaN(v.x)||float.IsNaN(v.y)||float.IsNaN(v.z)||float.IsInfinity(v.x)||float.IsInfinity(v.y)||float.IsInfinity(v.z));}
    Assert.Less(baked.bounds.size.magnitude,5,"Exploded deformation: "+Moves.All[i].Id);
   }
  }finally{UnityEngine.Object.DestroyImmediate(baked);UnityEngine.Object.DestroyImmediate(obj);}
 }
}

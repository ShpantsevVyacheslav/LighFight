using System.Linq;

using System;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
using LighFight.Core;
using LighFight.Runtime;
namespace LighFight.Editor {
 public static class SourceDeformationAudit {
  public static void Run(){var report=new StringBuilder();foreach(int id in new[]{3,4}){
   var obj=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>($"Assets/LighFight/Art/Fighters/fighter-{id}.prefab"));var skin=obj.GetComponentInChildren<SkinnedMeshRenderer>();var mesh=new Mesh();skin.BakeMesh(mesh);var before=mesh.vertices;var world=Array.ConvertAll(before,v=>skin.transform.TransformPoint(v));
   report.AppendLine("fighter "+id);var head=Array.FindIndex(skin.bones,b=>b.name=="Head");int count=0,mixed=0;var weights=skin.sharedMesh.boneWeights;
   for(int i=0;i<world.Length;i++)if(world[i].y>1.50f){count++;var w=weights[i];float h=(w.boneIndex0==head?w.weight0:0)+(w.boneIndex1==head?w.weight1:0)+(w.boneIndex2==head?w.weight2:0)+(w.boneIndex3==head?w.weight3:0);if(h>.1f&&h<.95f)mixed++;}
   report.AppendLine($"rest head-region vertices {count}; mixed head weights {mixed}");
   var fighter=new Fighter{Guard=true};obj.GetComponent<FighterView>().Pose(fighter,1,0);obj.GetComponent<Animator>().Update(0);obj.GetComponent<JointCorrectives>()?.Apply();skin.BakeMesh(mesh);var after=mesh.vertices;
   // Compare surface neighborhoods by fixed REST anatomical bands, distances invariant to root yaw.
   for(float y=.85f;y<1.35f;y+=.10f){var indices=System.Linq.Enumerable.Where(System.Linq.Enumerable.Range(0,world.Length),i=>Mathf.Abs(world[i].y-y)<.025f&&Mathf.Abs(world[i].x)<.35f).ToArray();if(indices.Length<10)continue;float minA=float.MaxValue,maxA=float.MinValue,minB=float.MaxValue,maxB=float.MinValue;foreach(int i in indices){minA=Mathf.Min(minA,before[i].x);maxA=Mathf.Max(maxA,before[i].x);minB=Mathf.Min(minB,after[i].x);maxB=Mathf.Max(maxB,after[i].x);}report.AppendLine($"rest band y={y:F2}, vertices={indices.Length}, local x span rest={maxA-minA:F3} guard={maxB-minB:F3}");}
   UnityEngine.Object.DestroyImmediate(mesh);UnityEngine.Object.DestroyImmediate(obj);
  }File.WriteAllText("../.tools/source-deformation-audit.txt",report.ToString());Debug.Log(report.ToString());}
 }
}

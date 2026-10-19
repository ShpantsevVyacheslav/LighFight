using System;
using System.IO;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
namespace LighFight.Editor {
 public static class IslandSurfaceRefinement {
  [Serializable] class Deltas {public Vector3[] points,offsets;}
  static Vector3Int Key(Vector3 v)=>new Vector3Int(Mathf.RoundToInt(v.x*100000),Mathf.RoundToInt(v.y*100000),Mathf.RoundToInt(v.z*100000));
  public const string MeshPath="Assets/LighFight/Art/Drafts/IslandQuality/refined-surface.asset";
  public static void Build(){
   var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/LighFight/Art/Drafts/IslandMeshyStaticFists/island-finger-candidate.fbx");var original=prefab.GetComponentInChildren<SkinnedMeshRenderer>().sharedMesh;
   var data=JsonUtility.FromJson<Deltas>(File.ReadAllText("../art/source/island-quality-v1/unity-deltas.json"));var vertices=original.vertices;
   Dictionary<Vector3Int,Vector3> best=null;int bestCount=-1;Vector3 bestScale=Vector3.zero;
   foreach(float sx in new[]{-.01f,.01f})foreach(float sy in new[]{-.01f,.01f}) {
    var scale=new Vector3(sx,sy,.01f);var lookup=new Dictionary<Vector3Int,Vector3>();for(int i=0;i<data.points.Length;i++)lookup[Key(Vector3.Scale(data.points[i],scale))]=Vector3.Scale(data.offsets[i],scale);
    int count=0;foreach(var v in vertices)if(lookup.ContainsKey(Key(v)))count++;
    if(count>bestCount){bestCount=count;best=lookup;bestScale=scale;}
   }
   if(bestCount<vertices.Length*.99f)throw new Exception("Surface correspondence incomplete: "+bestCount+"/"+vertices.Length);
   var mesh=UnityEngine.Object.Instantiate(original);mesh.name="Island refined skin surface";var changed=new bool[vertices.Length];int total=0;float max=0;
   for(int i=0;i<vertices.Length;i++)if(best.TryGetValue(Key(vertices[i]),out var offset)){vertices[i]+=offset;changed[i]=offset.sqrMagnitude>1e-12f;if(changed[i])total++;max=Mathf.Max(max,offset.magnitude);}
   if(max>.0061f)throw new Exception("Surface displacement exceeds 6mm");
   mesh.vertices=vertices;
   int headIndex=Array.FindIndex(prefab.GetComponentInChildren<SkinnedMeshRenderer>().bones,b=>b.name=="Head");var weights=mesh.boneWeights;int headWeights=0;
   for(int i=0;i<weights.Length;i++){
    var w=weights[i];float h=(w.boneIndex0==headIndex?w.weight0:0)+(w.boneIndex1==headIndex?w.weight1:0)+(w.boneIndex2==headIndex?w.weight2:0)+(w.boneIndex3==headIndex?w.weight3:0);
    // No facial rig exists: the face should move coherently with the skull.
    if(vertices[i].z>1.50f&&h>.25f&&h<.9999f){weights[i]=new BoneWeight{boneIndex0=headIndex,weight0=1};headWeights++;}
   }
   mesh.boneWeights=weights;mesh.RecalculateNormals();var normals=mesh.normals;
   // Weld normal shading across coincident UV splits without altering topology.
   var sums=new Dictionary<Vector3Int,Vector3>();for(int i=0;i<vertices.Length;i++){var key=Key(vertices[i]);sums.TryGetValue(key,out var n);sums[key]=n+normals[i];}
   for(int i=0;i<vertices.Length;i++)normals[i]=sums[Key(vertices[i])].normalized;mesh.normals=normals;mesh.RecalculateTangents();mesh.RecalculateBounds();
   var existing=AssetDatabase.LoadAssetAtPath<Mesh>(MeshPath);if(existing!=null){EditorUtility.CopySerialized(mesh,existing);UnityEngine.Object.DestroyImmediate(mesh);}else AssetDatabase.CreateAsset(mesh,MeshPath);AssetDatabase.SaveAssets();
   File.WriteAllText("../art/source/island-quality-v1/unity-surface-audit.txt",$"Matched {bestCount}/{vertices.Length}; refined {total}; max displacement {max:F6} m; source scale {bestScale}. Original triangles, UVs, bind poses and blendshape deltas retained. {headWeights} skull vertices assigned coherently to Head; all other weights retained.");
   Debug.Log("Island surface refinement: "+total+" vertices, max "+max);
  }
  public static void Promote(){
   const string prefabPath="Assets/LighFight/Art/Fighters/fighter-3.prefab";
   var obj=PrefabUtility.LoadPrefabContents(prefabPath);
   try {
    var skin=obj.GetComponentInChildren<SkinnedMeshRenderer>();var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(MeshPath);
    if(mesh==null || mesh.vertexCount!=skin.sharedMesh.vertexCount)throw new Exception("Refined surface missing or incompatible");
    skin.sharedMesh=mesh;
    var materials=skin.sharedMaterials;const string path="Assets/LighFight/Art/Drafts/IslandQuality/refined-skin.mat";
    var material=new Material(materials[0]);material.name="Island refined skin";material.SetFloat("_NormalStrength",.06f);
    var existing=AssetDatabase.LoadAssetAtPath<Material>(path);if(existing==null){AssetDatabase.CreateAsset(material,path);existing=material;}else{EditorUtility.CopySerialized(material,existing);UnityEngine.Object.DestroyImmediate(material);}
    materials[0]=existing;skin.sharedMaterials=materials;PrefabUtility.SaveAsPrefabAsset(obj,prefabPath);AssetDatabase.SaveAssets();
   }finally{PrefabUtility.UnloadPrefabContents(obj);}
  }

 }
}

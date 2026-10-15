using System;using System.IO;using System.Security.Cryptography;using System.Linq;using UnityEditor;using UnityEngine;
// Additive review only: no shipping catalog, source prefab, camera, actor or car material edits.
public static class CourtyardCanopyReview {
 const string Source="Assets/LighFight/Art/Drafts/ComponentCourtyardReview/arena-component-review.prefab";
 const string Dir="Assets/LighFight/Art/Drafts/CanopyReview";
 [Serializable]sealed class SourceFile {public string path="";public string sha256="";}
 [Serializable]sealed class Inputs {public SourceFile[] sourceFiles=Array.Empty<SourceFile>();}
 [Serializable]sealed class Data {public Vector3[] vertices=Array.Empty<Vector3>();public Vector2[] uv=Array.Empty<Vector2>();public int[] triangles=Array.Empty<int>();}
 public static void Build(){
  var inputs=JsonUtility.FromJson<Inputs>(File.ReadAllText(Dir+"/inputs.json"));if(inputs.sourceFiles.Length<5)throw new Exception("Frozen source inputs missing");foreach(var item in inputs.sourceFiles){if(!File.Exists(item.path))throw new Exception("Frozen input missing "+item.path);using(var h=SHA256.Create())if(BitConverter.ToString(h.ComputeHash(File.ReadAllBytes(item.path))).Replace("-","").ToLowerInvariant()!=item.sha256)throw new Exception("Frozen source changed "+item.path);}
  var source=AssetDatabase.LoadAssetAtPath<GameObject>(Source);if(!source)throw new Exception("Reviewed component source required");
  var data=JsonUtility.FromJson<Data>(File.ReadAllText(Dir+"/canopy-cards-v1.json"));
  if(data.vertices==null||data.vertices.Length!=2016||data.uv.Length!=2016||data.triangles.Length!=3024)throw new Exception("Unexpected source canopy fixture");
  for(int i=0;i<data.vertices.Length;i+=4){var b=new Bounds(data.vertices[i],Vector3.zero);for(int j=1;j<4;j++)b.Encapsulate(data.vertices[i+j]);if(InFight(b))throw new Exception("Individual alpha card intrudes fight volume");}
  var importer=(TextureImporter)AssetImporter.GetAtPath(Dir+"/willow-branch-atlas-v2.png");if(importer==null)throw new Exception("Generated atlas missing");
  importer.textureType=TextureImporterType.Default;importer.sRGBTexture=true;importer.alphaSource=TextureImporterAlphaSource.FromInput;importer.alphaIsTransparency=true;importer.npotScale=TextureImporterNPOTScale.None;importer.maxTextureSize=2048;importer.mipmapEnabled=true;importer.mipMapsPreserveCoverage=true;importer.alphaTestReferenceValue=.45f;importer.isReadable=false;
  var ios=importer.GetPlatformTextureSettings("iPhone");ios.overridden=true;ios.name="iPhone";ios.maxTextureSize=2048;ios.format=TextureImporterFormat.ASTC_4x4;ios.compressionQuality=100;ios.textureCompression=TextureImporterCompression.CompressedHQ;importer.SetPlatformTextureSettings(ios);importer.SaveAndReimport();
  var shader=AssetDatabase.LoadAssetAtPath<Shader>(Dir+"/CanopyCutout.shader");if(!shader||!shader.isSupported)throw new Exception("Review cutout shader unavailable");
  var material=AssetDatabase.LoadAssetAtPath<Material>(Dir+"/canopy.mat");if(!material){material=new Material(shader);AssetDatabase.CreateAsset(material,Dir+"/canopy.mat");}material.shader=shader;material.mainTexture=AssetDatabase.LoadAssetAtPath<Texture2D>(Dir+"/willow-branch-atlas-v2.png");material.SetFloat("_Cutoff",.45f);material.SetFloat("_Fill",.08f);EditorUtility.SetDirty(material);
  var mesh=new Mesh{name="Nine tree crown atlas cards, canonical Unity coordinates"};mesh.vertices=data.vertices;mesh.uv=data.uv;mesh.triangles=data.triangles;mesh.RecalculateNormals();mesh.RecalculateBounds();var saved=AssetDatabase.LoadAssetAtPath<Mesh>(Dir+"/canopy.mesh.asset");if(saved){EditorUtility.CopySerialized(mesh,saved);UnityEngine.Object.DestroyImmediate(mesh);mesh=saved;}else AssetDatabase.CreateAsset(mesh,Dir+"/canopy.mesh.asset");
  var root=PrefabUtility.LoadPrefabContents(Source);try{
   var cars=root.GetComponentsInChildren<MeshFilter>(true).Where(x=>x.sharedMesh&&AssetDatabase.GetAssetPath(x.sharedMesh).EndsWith("/source-sedan.mesh.asset",StringComparison.Ordinal)).ToArray();if(cars.Length!=3)throw new Exception("Expected exactly three preserved source sedan renderers");var carSources=cars.Select(x=>x.sharedMesh).ToArray();var carMatrices=cars.Select(x=>x.transform.localToWorldMatrix).ToArray();var carMaterials=cars.Select(x=>x.GetComponent<Renderer>().sharedMaterials).ToArray();
   foreach(var t in root.GetComponentsInChildren<Transform>(true).Where(x=>x.name.StartsWith("Separate foliage cluster")||x.name=="Thin separate leaf surface"||x.name=="Nine atlas tree crowns").ToArray())UnityEngine.Object.DestroyImmediate(t.gameObject);
   var go=new GameObject("Nine atlas tree crowns");go.transform.SetParent(root.transform,false);go.AddComponent<MeshFilter>().sharedMesh=mesh;var renderer=go.AddComponent<MeshRenderer>();renderer.sharedMaterial=material;renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.TwoSided;renderer.receiveShadows=true;
   var bark=AssetDatabase.LoadAssetAtPath<Material>("Assets/LighFight/Art/Drafts/ComponentCourtyardReview/BranchGeometryReview.mat");if(!bark)throw new Exception("Reviewed bark material required");
   foreach(var p in new[]{new Vector3(-7,0,8),new Vector3(-3.5f,0,9),new Vector3(0,0,9.5f),new Vector3(3.5f,0,9),new Vector3(7,0,9)}){
    var trunk=GameObject.CreatePrimitive(PrimitiveType.Cylinder);trunk.name="Backdrop trunk";trunk.transform.SetParent(root.transform,false);trunk.transform.localPosition=p+Vector3.up*2.4f;trunk.transform.localScale=new Vector3(.28f,2.4f,.28f);trunk.GetComponent<Renderer>().sharedMaterial=bark;UnityEngine.Object.DestroyImmediate(trunk.GetComponent<Collider>());
   }
   for(int i=0;i<cars.Length;i++)if(cars[i].sharedMesh!=carSources[i]||cars[i].transform.localToWorldMatrix!=carMatrices[i]||!cars[i].GetComponent<Renderer>().sharedMaterials.SequenceEqual(carMaterials[i]))throw new Exception("Source car mesh/placement/material changed");
   if(root.GetComponentsInChildren<Collider>(true).Any())throw new Exception("Unexpected draft collision component; review before promotion");
   PrefabUtility.SaveAsPrefabAsset(root,Dir+"/arena-canopy-review.prefab");AssetDatabase.SaveAssets();
  }finally{PrefabUtility.UnloadPrefabContents(root);}
  Debug.Log("AGENT_RESULT canopy draft built:1008triangles,504quads; no catalog promotion; GPU/player/dynamic-clearance/device gates pending");
 }
 static bool InFight(Bounds b)=>b.max.x>-5.5f&&b.min.x<5.5f&&b.max.z>-2&&b.min.z<2&&b.max.y>.05f&&b.min.y<2.85f;
}

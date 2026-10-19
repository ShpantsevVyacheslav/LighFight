#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using LighFight.Runtime;
// Named, explicit candidate job. Does not update GameContent or the shipping arena.
public static class GeneratedKitchenArenaBuilder {
 const string Source="art/source/office-kitchen-meshy-v1/textured";
 const string Folder="Assets/LighFight/Art/GeneratedKitchenV1";
 const string Evidence="art/previews/office-kitchen-meshy-v1/integration";
 [Serializable]sealed class Manifest {
  public string source,shader,prefab;public int meshTriangles,meshVertices,meshes;
  public Vector3 backdropMin,backdropMax,floorMin,floorMax;
  public float sourceHeight,targetHeight=3,minimumBackdropZ=2.6f;
  public string scope="Candidate component assembly; source mesh unchanged; separate floor; no shipping content changed";
 }
 [MenuItem("LighFight/Review/Build Generated Kitchen Candidate")]
 public static void Run(){
  string project=Directory.GetParent(Application.dataPath).Parent.FullName;
  string source=Path.Combine(project,Source),evidence=Path.Combine(project,Evidence);Directory.CreateDirectory(evidence);
  if(!Directory.Exists(source))throw new DirectoryNotFoundException("Wait for textured source: "+source);
  var models=Directory.GetFiles(source,"*.fbx",SearchOption.AllDirectories);
  if(models.Length!=1)throw new InvalidOperationException("Expected exactly one original textured FBX in "+source+"; found "+models.Length);
  string destination=Path.Combine(Application.dataPath,"LighFight/Art/GeneratedKitchenV1/Source");Directory.CreateDirectory(destination);
  foreach(string file in Directory.GetFiles(source,"*",SearchOption.AllDirectories)){
   var extension=Path.GetExtension(file).ToLowerInvariant();
   if(!new[]{".fbx",".png",".jpg",".jpeg",".tga",".exr"}.Contains(extension))continue;
   string relative=file.Substring(source.Length).TrimStart(Path.DirectorySeparatorChar);
   string target=Path.Combine(destination,relative);Directory.CreateDirectory(Path.GetDirectoryName(target));File.Copy(file,target,true);
  }
  AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
  foreach(string path in AssetDatabase.FindAssets("t:Texture2D",new[]{Folder+"/Source"}).Select(AssetDatabase.GUIDToAssetPath)){
   var importer=(TextureImporter)AssetImporter.GetAtPath(path);importer.textureCompression=TextureImporterCompression.Uncompressed;importer.maxTextureSize=16384;importer.SaveAndReimport();
  }
  string modelPath=Folder+"/Source/"+models[0].Substring(source.Length).TrimStart(Path.DirectorySeparatorChar).Replace('\\','/');
  var modelImporter=(ModelImporter)AssetImporter.GetAtPath(modelPath);
  modelImporter.meshCompression=ModelImporterMeshCompression.Off;modelImporter.optimizeMeshPolygons=false;modelImporter.optimizeMeshVertices=false;modelImporter.isReadable=true;modelImporter.SaveAndReimport();
  var model=AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);if(!model)throw new InvalidOperationException("FBX import failed");
  var scene=UnityEditor.SceneManagement.EditorSceneManager.NewPreviewScene();var root=new GameObject("Generated Kitchen V1 candidate");UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(root,scene);
  try{
   root.AddComponent<OfficeArena>().UseSharedMaterials();
   var set=(GameObject)PrefabUtility.InstantiatePrefab(model,scene);set.name="Original generated kitchenette (all geometry)";set.transform.SetParent(root.transform,false);
   var renderers=set.GetComponentsInChildren<Renderer>(true);if(renderers.Length==0)throw new InvalidOperationException("No generated renderers");
   Bounds original=BoundsOf(renderers);if(original.size.y<.0001f)throw new InvalidOperationException("Invalid generated bounds");
   set.transform.localScale*=3/original.size.y;
   var scaled=BoundsOf(renderers);set.transform.position+=new Vector3(-scaled.center.x,-scaled.min.y,2.6f-scaled.min.z);
   var shader=Shader.Find("Standard");if(!shader||!shader.isSupported)throw new InvalidOperationException("Built-in Standard shader unavailable");
   var materials=new System.Collections.Generic.Dictionary<Material,Material>();int textured=0;
   foreach(var renderer in renderers){var slots=renderer.sharedMaterials;for(int i=0;i<slots.Length;i++){
    var imported=slots[i];if(!imported)throw new InvalidOperationException("Missing imported material");
    if(!materials.TryGetValue(imported,out var material)){
     var texture=imported.mainTexture;if(!texture)throw new InvalidOperationException("Textured FBX material has no base texture: "+imported.name+". Extract/link its original textures before this job.");
     material=new Material(shader){name="Kitchen "+imported.name,mainTexture=texture,color=Color.white};material.SetFloat("_Glossiness",.25f);
     if(imported.HasProperty("_BumpMap")&&imported.GetTexture("_BumpMap")){material.SetTexture("_BumpMap",imported.GetTexture("_BumpMap"));material.EnableKeyword("_NORMALMAP");}
     material=SaveMaterial(Folder+"/Kitchen_"+Safe(imported.name)+".mat",material);materials.Add(imported,material);
    }slots[i]=material;textured++;
   }renderer.sharedMaterials=slots;renderer.shadowCastingMode=ShadowCastingMode.On;renderer.receiveShadows=true;}
   if(textured==0)throw new InvalidOperationException("No textured generated surfaces");
   var floorMaterial=new Material(Shader.Find("LighFight/OfficeLit")){name="Kitchen combat floor",color=new Color(.30f,.27f,.23f)};floorMaterial=SaveMaterial(Folder+"/CombatFloor.mat",floorMaterial);
   var floor=GameObject.CreatePrimitive(PrimitiveType.Cube);floor.name="Independent unobstructed combat floor";floor.transform.SetParent(root.transform,false);floor.transform.localPosition=new Vector3(0,-.1f,1);floor.transform.localScale=new Vector3(14,.2f,11);floor.GetComponent<Renderer>().sharedMaterial=floorMaterial;UnityEngine.Object.DestroyImmediate(floor.GetComponent<Collider>());
   var report=new Manifest{source=models[0],shader=shader.name,prefab=Folder+"/KitchenCandidate.prefab",sourceHeight=original.size.y};
   foreach(var mesh in set.GetComponentsInChildren<MeshFilter>(true).Select(x=>x.sharedMesh)){report.meshes++;report.meshVertices+=mesh.vertexCount;report.meshTriangles+=mesh.triangles.Length/3;}
   var actual=BoundsOf(renderers);report.backdropMin=actual.min;report.backdropMax=actual.max;var floorBounds=floor.GetComponent<Renderer>().bounds;report.floorMin=floorBounds.min;report.floorMax=floorBounds.max;
   if(actual.min.z<2.59f||actual.min.y<-.01f||floorBounds.min.x> -5||floorBounds.max.x<5||floorBounds.min.z> -1.5f||floorBounds.max.z<1.5f)throw new InvalidOperationException("Combat clearance contract failed");
   PrefabUtility.SaveAsPrefabAsset(root,report.prefab);AssetDatabase.SaveAssets();File.WriteAllText(Path.Combine(evidence,"layout.json"),JsonUtility.ToJson(report,true));
   Render(root,scene,Path.Combine(evidence,"fight-view.png"));
   Debug.Log("AGENT_RESULT generated kitchen candidate PASS triangles="+report.meshTriangles+" prefab="+report.prefab);
  }finally{UnityEngine.Object.DestroyImmediate(root);UnityEditor.SceneManagement.EditorSceneManager.ClosePreviewScene(scene);}
 }
 static Material SaveMaterial(string path,Material candidate){var existing=AssetDatabase.LoadAssetAtPath<Material>(path);if(existing){EditorUtility.CopySerialized(candidate,existing);UnityEngine.Object.DestroyImmediate(candidate);EditorUtility.SetDirty(existing);return existing;}AssetDatabase.CreateAsset(candidate,path);return candidate;}
 static string Safe(string text)=>new string(text.Select(c=>char.IsLetterOrDigit(c)?c:'_').ToArray());
 static Bounds BoundsOf(Renderer[] renderers){var b=renderers[0].bounds;foreach(var r in renderers)b.Encapsulate(r.bounds);return b;}
 static void Render(GameObject root,UnityEngine.SceneManagement.Scene scene,string path){
  var camera=new GameObject("Candidate fight view").AddComponent<Camera>();UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(camera.gameObject,scene);camera.transform.position=new Vector3(0,2.3f,-7.5f);camera.transform.LookAt(new Vector3(0,1.1f,1));camera.fieldOfView=42;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.07f,.08f,.1f);
  var light=new GameObject("Candidate key").AddComponent<Light>();UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(light.gameObject,scene);light.type=LightType.Directional;light.intensity=1.3f;light.transform.rotation=Quaternion.Euler(45,-25,0);light.shadows=LightShadows.Soft;
  var rt=new RenderTexture(1280,720,24,RenderTextureFormat.ARGB32);var old=RenderTexture.active;var image=new Texture2D(1280,720,TextureFormat.RGB24,false);
  try{rt.Create();camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;image.ReadPixels(new Rect(0,0,1280,720),0,0);image.Apply();File.WriteAllBytes(path,image.EncodeToPNG());}finally{camera.targetTexture=null;RenderTexture.active=old;rt.Release();UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(image);UnityEngine.Object.DestroyImmediate(camera.gameObject);UnityEngine.Object.DestroyImmediate(light.gameObject);}
 }
}
#endif

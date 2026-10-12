using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEditor.SceneManagement;
namespace LighFight.Editor {
 public static class CourtyardReferenceSetup {
  public const string Folder="Assets/LighFight/Art/Drafts/CourtyardReferenceV2";
  public static void ImportDraft(){
   AssetDatabase.Refresh();var path=Folder+"/courtyard.fbx";var importer=(ModelImporter)AssetImporter.GetAtPath(path);
   if(!importer)throw new System.InvalidOperationException("Build the courtyard source first");
   Directory.CreateDirectory(Folder+"/Textures");importer.ExtractTextures(Folder+"/Textures");AssetDatabase.Refresh();
   importer.meshCompression=ModelImporterMeshCompression.Off;importer.SaveAndReimport();
   var source=AssetDatabase.LoadAssetAtPath<GameObject>(path);
   var probe=Object.Instantiate(source);try{var floor=probe.GetComponentsInChildren<Renderer>().Single(r=>r.name=="FightFloor");Debug.Log("Courtyard floor min="+floor.bounds.min.ToString("G9")+" max="+floor.bounds.max.ToString("G9"));}finally{Object.DestroyImmediate(probe);}
   var prefab=MeshyArenaImport.Import(source,Folder+"/arena.prefab");
   const string textures="../art/source/courtyard-reference-v2/car-textures/";
   var carMaterial=NativeMaterialImport.Create(Folder+"/CarPbr",textures+"albedo.png",textures+"normal.png",textures+"metallic-roughness.png");
   var edited=PrefabUtility.LoadPrefabContents(Folder+"/arena.prefab");
   try{foreach(var renderer in edited.GetComponentsInChildren<Renderer>().Where(r=>r.name.StartsWith("Sedan ")))renderer.sharedMaterials=renderer.sharedMaterials.Select(_=>carMaterial).ToArray();PrefabUtility.SaveAsPrefabAsset(edited,Folder+"/arena.prefab");}
   finally{PrefabUtility.UnloadPrefabContents(edited);}
   prefab=AssetDatabase.LoadAssetAtPath<GameObject>(Folder+"/arena.prefab");
   var actor=Object.Instantiate(prefab);
   try{
    MeshyArenaImport.Validate(actor);
    int triangles=actor.GetComponentsInChildren<MeshFilter>().Sum(m=>m.sharedMesh.triangles.Length/3);
    Directory.CreateDirectory("../art/previews/courtyard-reference-v2");
    File.WriteAllText("../art/previews/courtyard-reference-v2/unity-layout-audit.txt",triangles+" triangles; one FightFloor covers 11x4m at y0; decoration clear; no catalog promotion; no device validation");
   }finally{Object.DestroyImmediate(actor);}
  }
  public static void CaptureDraft(){
   ShaderUtil.allowAsyncCompilation=false;
   EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
   var actor=Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(Folder+"/arena.prefab"));
   MeshyArenaImport.Validate(actor);
   var light=new GameObject("Review daylight").AddComponent<Light>();light.type=LightType.Directional;light.intensity=1;light.transform.rotation=Quaternion.Euler(45,-30,0);
   RenderSettings.ambientLight=new Color(.4f,.4f,.4f);
   var cam=new GameObject("Courtyard review").AddComponent<Camera>();cam.orthographic=true;cam.orthographicSize=11;cam.backgroundColor=new Color(.18f,.21f,.25f);cam.clearFlags=CameraClearFlags.SolidColor;
   cam.transform.position=new Vector3(12,10,15);cam.transform.LookAt(new Vector3(0,1.5f,-2));
   var rt=new RenderTexture(1400,900,24){antiAliasing=4};var image=new Texture2D(1400,900,TextureFormat.RGB24,false);
   try{cam.targetTexture=rt;cam.Render();RenderTexture.active=rt;image.ReadPixels(new Rect(0,0,1400,900),0,0);image.Apply();File.WriteAllBytes("../art/previews/courtyard-reference-v2/unity-layout.png",image.EncodeToPNG());}
   finally{RenderTexture.active=null;cam.targetTexture=null;Object.DestroyImmediate(image);Object.DestroyImmediate(rt);Object.DestroyImmediate(actor);Object.DestroyImmediate(cam.gameObject);Object.DestroyImmediate(light.gameObject);}
  }
 }
}

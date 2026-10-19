using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
namespace LighFight.Editor {
 public static class TripoArenaCombatCapture {
  public static void Run(){
   const string folder="Assets/LighFight/Art/Drafts/TripoIsland";
   AssetDatabase.ImportAsset(folder+"/native-pack.fbx",ImportAssetOptions.ForceSynchronousImport);NativeFighterImport.PrepareImporter((ModelImporter)AssetImporter.GetAtPath(folder+"/native-pack.fbx"));
   var source=AssetDatabase.LoadAssetAtPath<GameObject>(folder+"/native-pack.fbx");var clip=AssetDatabase.LoadAllAssetsAtPath(folder+"/native-pack.fbx").OfType<AnimationClip>().Single(c=>c.name=="box_02");
   var texture=AssetDatabase.LoadAssetAtPath<Texture2D>(folder+"/island-h31-unity-native-pack_basecolor.JPEG");var material=new Material(Shader.Find("Unlit/Texture")){mainTexture=texture};
   var scene=EditorSceneManager.NewPreviewScene();var arena=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(TripoArenaSetup.Folder+"/arena.prefab"));EditorSceneManager.MoveGameObjectToScene(arena,scene);
   var actors=new GameObject[2];var cameraObject=new GameObject("Combat framing");EditorSceneManager.MoveGameObjectToScene(cameraObject,scene);var camera=cameraObject.AddComponent<Camera>();camera.scene=scene;camera.enabled=false;camera.transform.position=new Vector3(0,1.6f,-3.6f);camera.transform.LookAt(new Vector3(0,1,0));camera.fieldOfView=42;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.09f,.11f,.14f);
   var render=new RenderTexture(1600,900,24);render.Create();camera.targetTexture=render;var image=new Texture2D(1600,900,TextureFormat.RGB24,false);var previous=RenderTexture.active;
   try{
    for(int i=0;i<2;i++){
     var wrapper=new GameObject("Native fighter framing "+i);actors[i]=wrapper;EditorSceneManager.MoveGameObjectToScene(wrapper,scene);
     var model=UnityEngine.Object.Instantiate(source);EditorSceneManager.MoveGameObjectToScene(model,scene);model.transform.SetParent(wrapper.transform,false);
     var skins=model.GetComponentsInChildren<SkinnedMeshRenderer>();Bounds bounds=default;bool first=true;
     foreach(var skin in skins){var mesh=new Mesh();skin.BakeMesh(mesh);foreach(var v in mesh.vertices){var point=skin.transform.TransformPoint(v);if(first){bounds=new Bounds(point,Vector3.zero);first=false;}else bounds.Encapsulate(point);}UnityEngine.Object.DestroyImmediate(mesh);}
     Debug.Log("ACTOR_NORMALIZATION source visible height "+bounds.size.y);
     wrapper.transform.localScale=Vector3.one*(1.78f/bounds.size.y);wrapper.transform.position=new Vector3(i==0?-1:1,0,0);wrapper.transform.rotation=Quaternion.Euler(0,i==0?90:-90,0);
     foreach(var skin in skins){skin.sharedMaterials=skin.sharedMaterials.Select(_=>material).ToArray();skin.updateWhenOffscreen=true;skin.shadowCastingMode=ShadowCastingMode.Off;skin.receiveShadows=false;}
     clip.SampleAnimation(model,clip.length*.35f);
    }
    ShaderUtil.allowAsyncCompilation=false;camera.Render();RenderTexture.active=render;image.ReadPixels(new Rect(0,0,1600,900),0,0);image.Apply();
    var args=Environment.GetCommandLineArgs();int index=Array.IndexOf(args,"-tripoOutput");string output=index>=0?Path.GetFullPath(args[index+1]):Path.GetFullPath("../art/previews/courtyard-tripo-v1/unity");Directory.CreateDirectory(output);File.WriteAllBytes(output+"/native-fighter-framing.png",image.EncodeToPNG());Debug.Log("TRIPO_COMBAT_FRAMING captured two original native fighters, no gameplay/device claim");
   }finally{RenderTexture.active=previous;camera.targetTexture=null;UnityEngine.Object.DestroyImmediate(image);UnityEngine.Object.DestroyImmediate(render);foreach(var actor in actors)if(actor)UnityEngine.Object.DestroyImmediate(actor);UnityEngine.Object.DestroyImmediate(arena);UnityEngine.Object.DestroyImmediate(cameraObject);UnityEngine.Object.DestroyImmediate(material);EditorSceneManager.ClosePreviewScene(scene);}
  }
 }
}

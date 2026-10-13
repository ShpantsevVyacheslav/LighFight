using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace LighFight.Editor {
 [InitializeOnLoad]
 public static class TripoNativeAudit {
  const string Folder="Assets/LighFight/Art/Drafts/TripoIsland";
  static readonly string Request=Path.GetFullPath("Library/tripo-review.request");
  static double next;
  static TripoNativeAudit(){EditorApplication.update+=Tick;}
  static void Tick(){
   if(EditorApplication.timeSinceStartup<next)return;
   next=EditorApplication.timeSinceStartup+1;
   if(EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlayingOrWillChangePlaymode||!File.Exists(Request))return;
   string running=Request+"."+Guid.NewGuid().ToString("N")+".running";
   File.Move(Request,running);
   try{Run();File.Move(running,running+".done");}
   catch(Exception e){File.WriteAllText(Request+".failed",e.ToString());Debug.LogException(e);}
  }
  static string Argument(string[] args,string flag,string fallback){int i=Array.IndexOf(args,flag);if(i<0)return fallback;if(i+1>=args.Length)throw new ArgumentException("Missing value for "+flag);return args[i+1];}
  [MenuItem("LighFight/Review Tripo Native Pack")]
  public static void Run(){QualitySettings.skinWeights=SkinWeights.Unlimited;
   var args=Environment.GetCommandLineArgs();int outputIndex=Array.IndexOf(args,"-tripoOutput");
   string output=outputIndex>=0?Path.GetFullPath(args[outputIndex+1]):Path.GetFullPath("../art/previews/island-tripo-v1/unity-native");Directory.CreateDirectory(output);
   string folder=Argument(args,"-tripoFolder",Folder);
   int expectedTriangles=int.Parse(Argument(args,"-tripoTriangles","100000"));
   int expectedClips=int.Parse(Argument(args,"-tripoClips","7"));
   int expectedTexture=int.Parse(Argument(args,"-tripoTextureSize","8192"));
   string path=folder+"/native-pack.fbx";
   AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);
   var importer=(ModelImporter)AssetImporter.GetAtPath(path);
   NativeFighterImport.PrepareImporter(importer);
   string texturePath=Argument(args,"-tripoTexture",folder+"/island-h31-unity-native-pack_basecolor.JPEG");
   var ti=(TextureImporter)AssetImporter.GetAtPath(texturePath);
   ti.maxTextureSize=expectedTexture;ti.textureCompression=TextureImporterCompression.Uncompressed;ti.mipmapEnabled=true;ti.SaveAndReimport();
   var texture=AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
   if(texture==null||texture.width!=expectedTexture)throw new InvalidOperationException("Original texture resolution was not retained");
   var model=AssetDatabase.LoadAssetAtPath<GameObject>(path);
   var clips=AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>().Where(c=>!c.name.StartsWith("__preview")).ToArray();
   if(clips.Length!=expectedClips)throw new InvalidOperationException("Expected "+expectedClips+" native clips, got "+clips.Length);
   var scene=EditorSceneManager.NewPreviewScene();
   var owned=new List<UnityEngine.Object>();
   try{
    var obj=UnityEngine.Object.Instantiate(model);EditorSceneManager.MoveGameObjectToScene(obj,scene);foreach(var a in obj.GetComponentsInChildren<Animator>()){a.Rebind();a.Update(0);a.enabled=false;}owned.Add(obj);
    var material=new Material(Shader.Find("Unlit/Texture")){mainTexture=texture};owned.Add(material);
    var cameraObj=new GameObject("Tripo review camera");EditorSceneManager.MoveGameObjectToScene(cameraObj,scene);owned.Add(cameraObj);
    var camera=cameraObj.AddComponent<Camera>();camera.scene=scene;camera.enabled=false;camera.nearClipPlane=.01f;camera.farClipPlane=100f;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.18f,.19f,.22f);camera.orthographic=true;
    var target=new RenderTexture(1000,1200,24);target.Create();owned.Add(target);camera.targetTexture=target;
    var skins=obj.GetComponentsInChildren<SkinnedMeshRenderer>();
    int triangles=skins.Sum(s=>s.sharedMesh.triangles.Length/3);
    if(triangles!=expectedTriangles)throw new InvalidOperationException("Native topology changed: "+triangles);
    foreach(var clip in clips){
     NativeFighterImport.ValidateClip(model,clip);
     foreach(float phase in new[]{0f,.35f,.7f,1f}){
      clip.SampleAnimation(obj,clip.length*phase);
      var bakedObjects=new List<GameObject>();var bakedMeshes=new List<Mesh>();Bounds bounds=default;bool first=true;
      try{
       foreach(var skin in skins){
        var mesh=new Mesh();skin.BakeMesh(mesh,false);bakedMeshes.Add(mesh);
        var copy=new GameObject("CPU native surface");EditorSceneManager.MoveGameObjectToScene(copy,scene);bakedObjects.Add(copy);
        copy.transform.SetPositionAndRotation(skin.transform.position,skin.transform.rotation);copy.transform.localScale=Vector3.Scale(skin.transform.lossyScale,new Vector3(1/skin.transform.localScale.x,1/skin.transform.localScale.y,1/skin.transform.localScale.z));
        copy.AddComponent<MeshFilter>().sharedMesh=mesh;var renderer=copy.AddComponent<MeshRenderer>();renderer.sharedMaterial=material;
        skin.enabled=false;
        if(first){bounds=renderer.bounds;first=false;}else bounds.Encapsulate(renderer.bounds);
       }
       camera.transform.position=bounds.center+Vector3.forward*bounds.size.y*3;
       camera.transform.rotation=Quaternion.LookRotation(bounds.center-camera.transform.position,Vector3.up);
       camera.orthographicSize=Mathf.Max(bounds.size.y*.58f,bounds.size.x*.65f);
       camera.Render();var previous=RenderTexture.active;RenderTexture.active=target;
       var image=new Texture2D(target.width,target.height,TextureFormat.RGB24,false);
       try{image.ReadPixels(new Rect(0,0,target.width,target.height),0,0);image.Apply();File.WriteAllBytes(Path.Combine(output,clip.name+"-"+phase.ToString("0.00",System.Globalization.CultureInfo.InvariantCulture)+".png"),image.EncodeToPNG());}
       finally{RenderTexture.active=previous;UnityEngine.Object.DestroyImmediate(image);}
      }finally{foreach(var item in bakedObjects)UnityEngine.Object.DestroyImmediate(item);foreach(var mesh in bakedMeshes)UnityEngine.Object.DestroyImmediate(mesh);}
     }
    }
    File.WriteAllText(Path.Combine(output,"audit.json"),"{\"triangles\":"+triangles+",\"textureWidth\":"+texture.width+",\"clips\":"+clips.Length+",\"captures\":"+(clips.Length*4)+",\"resampleCurves\":false,\"meshCompression\":\"Off\"}");
    Debug.Log("TRIPO_REVIEW_RESULT "+triangles+" triangles, "+texture.width+" texture, "+clips.Length+" clips, "+(clips.Length*4)+" captures");
   }finally{foreach(var item in owned.AsEnumerable().Reverse())if(item!=null)UnityEngine.Object.DestroyImmediate(item);EditorSceneManager.ClosePreviewScene(scene);}
  }
 }
}

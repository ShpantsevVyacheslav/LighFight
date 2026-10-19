using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace LighFight.Editor {
public static class MeshyNativeReview {
 const string Folder="Assets/LighFight/Art/Drafts/MeshyNativeIsland";
 public static void PrepareAndCapture() {
  Directory.CreateDirectory(Folder);
  File.Copy("../art/source/original-island/albedo.png",Folder+"/albedo.png",true);
  File.Copy("../art/source/original-island/normal.png",Folder+"/normal.png",true);
  AssetDatabase.Refresh();
  var normal=(TextureImporter)AssetImporter.GetAtPath(Folder+"/normal.png");normal.textureType=TextureImporterType.NormalMap;normal.textureCompression=TextureImporterCompression.Uncompressed;normal.maxTextureSize=2048;normal.SaveAndReimport();
  var importer=(ModelImporter)AssetImporter.GetAtPath(Folder+"/native-motions.fbx");importer.animationType=ModelImporterAnimationType.Generic;importer.avatarSetup=ModelImporterAvatarSetup.CreateFromThisModel;importer.isReadable=true;importer.meshCompression=ModelImporterMeshCompression.Off;importer.SaveAndReimport();
  var clips=AssetDatabase.LoadAllAssetsAtPath(Folder+"/native-motions.fbx").OfType<AnimationClip>().Where(c=>!c.name.StartsWith("__preview")).ToArray();
  if(clips.Length!=2)throw new System.Exception("Expected two native Meshy animations, got "+clips.Length);
  var material=new Material(Shader.Find("Standard"));material.mainTexture=AssetDatabase.LoadAssetAtPath<Texture2D>(Folder+"/albedo.png");material.SetTexture("_BumpMap",AssetDatabase.LoadAssetAtPath<Texture2D>(Folder+"/normal.png"));material.EnableKeyword("_NORMALMAP");material.SetFloat("_Glossiness",.25f);AssetDatabase.DeleteAsset(Folder+"/source.mat");AssetDatabase.CreateAsset(material,Folder+"/source.mat");
  AssetDatabase.DeleteAsset(Folder+"/native.controller");var controller=AnimatorController.CreateAnimatorControllerAtPath(Folder+"/native.controller");foreach(var clip in clips)controller.AddMotion(clip).name=clip.name;
  var obj=Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(Folder+"/native-motions.fbx"));foreach(var skin in obj.GetComponentsInChildren<SkinnedMeshRenderer>())skin.sharedMaterials=skin.sharedMaterials.Select(m=>material).ToArray();obj.GetComponent<Animator>().runtimeAnimatorController=controller;PrefabUtility.SaveAsPrefabAsset(obj,Folder+"/native-review.prefab");Object.DestroyImmediate(obj);AssetDatabase.SaveAssets();
  ShaderUtil.allowAsyncCompilation=false;EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);RenderSettings.ambientLight=new Color(.4f,.4f,.4f);
  var light=new GameObject("Key").AddComponent<Light>();light.type=LightType.Directional;light.intensity=1;light.transform.rotation=Quaternion.Euler(25,-35,0);
  var camera=new GameObject("Camera").AddComponent<Camera>();camera.backgroundColor=new Color(.06f,.07f,.09f);camera.clearFlags=CameraClearFlags.SolidColor;camera.fieldOfView=35;
  obj=Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(Folder+"/native-review.prefab"));var anim=obj.GetComponent<Animator>();anim.Rebind();
  Directory.CreateDirectory("../art/previews/meshy-native");
  foreach(var clip in clips){anim.Play(clip.name,0,0);anim.Update(0);Bounds bounds=obj.GetComponentsInChildren<SkinnedMeshRenderer>()[0].bounds;camera.transform.position=bounds.center+new Vector3(.3f,.05f,-bounds.size.y*1.9f);camera.transform.LookAt(bounds.center);var rt=new RenderTexture(900,1100,24);camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;var png=new Texture2D(900,1100,TextureFormat.RGB24,false);png.ReadPixels(new Rect(0,0,900,1100),0,0);DrawSkeleton(png,camera,obj);png.Apply();File.WriteAllBytes("../art/previews/meshy-native/unity-"+clip.name.Replace('|','-')+".png",png.EncodeToPNG());RenderTexture.active=null;camera.targetTexture=null;Object.DestroyImmediate(png);Object.DestroyImmediate(rt);}
  var audit=new System.Text.StringBuilder();
  foreach(var clip in clips) {
   for(int frame=0;frame<=60;frame++) {
    anim.Play(clip.name,0,frame/60f);anim.Update(0);
    foreach(var skin in obj.GetComponentsInChildren<SkinnedMeshRenderer>()) {
     var mesh=new Mesh();skin.BakeMesh(mesh);
     foreach(var v in mesh.vertices) if(float.IsNaN(v.x)||float.IsInfinity(v.x)||float.IsNaN(v.y)||float.IsInfinity(v.y)||float.IsNaN(v.z)||float.IsInfinity(v.z))throw new System.Exception("Non-finite Meshy pose");
     if(mesh.bounds.size.magnitude>5)throw new System.Exception("Exploded Meshy pose");
     Object.DestroyImmediate(mesh);
    }
   }
   audit.AppendLine(clip.name+": 61 sampled poses finite; bounds below 5 m");
  }
  File.WriteAllText("../art/source/original-island/native-unity-audit.txt",audit.ToString());
  Object.DestroyImmediate(obj);
 }
 static void DrawSkeleton(Texture2D image,Camera camera,GameObject obj) {
  var bones=new System.Collections.Generic.HashSet<Transform>(obj.GetComponentsInChildren<SkinnedMeshRenderer>().SelectMany(r=>r.bones).Where(b=>b));
  foreach(var bone in bones) {
   var point=camera.WorldToViewportPoint(bone.position);if(point.z<=0)continue;
   int x=Mathf.RoundToInt(point.x*image.width),y=Mathf.RoundToInt(point.y*image.height);
   if(bone.parent&&bones.Contains(bone.parent)) {
    var parent=camera.WorldToViewportPoint(bone.parent.position);int px=Mathf.RoundToInt(parent.x*image.width),py=Mathf.RoundToInt(parent.y*image.height);
    int length=Mathf.Max(Mathf.Abs(x-px),Mathf.Abs(y-py));
    for(int i=0;i<=length;i++)Dot(image,Mathf.RoundToInt(Mathf.Lerp(px,x,i/(float)Mathf.Max(1,length))),Mathf.RoundToInt(Mathf.Lerp(py,y,i/(float)Mathf.Max(1,length))),1,new Color(.7f,1,.15f));
   }
   Dot(image,x,y,4,new Color(1,.65f,.15f));
  }
 }
 static void Dot(Texture2D image,int x,int y,int radius,Color color) {for(int dx=-radius;dx<=radius;dx++)for(int dy=-radius;dy<=radius;dy++)if(dx*dx+dy*dy<=radius*radius&&x+dx>=0&&x+dx<image.width&&y+dy>=0&&y+dy<image.height)image.SetPixel(x+dx,y+dy,color);}

}
}

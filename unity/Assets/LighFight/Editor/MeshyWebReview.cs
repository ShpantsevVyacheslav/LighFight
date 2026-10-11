using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace LighFight.Editor {
public static class MeshyWebReview {
 const string Folder="Assets/LighFight/Art/Drafts/MeshyWebIsland";
 public static void PrepareAndCapture() {
  Directory.CreateDirectory(Folder);
  File.Copy("../art/source/original-island/web-albedo.png",Folder+"/albedo.png",true);
  File.Copy("../art/source/original-island/web-normal.png",Folder+"/normal.png",true);
  AssetDatabase.Refresh();
  var normal=(TextureImporter)AssetImporter.GetAtPath(Folder+"/normal.png");normal.textureType=TextureImporterType.NormalMap;normal.textureCompression=TextureImporterCompression.Uncompressed;normal.maxTextureSize=2048;normal.SaveAndReimport();
  var importer=(ModelImporter)AssetImporter.GetAtPath(Folder+"/web-motions.fbx");importer.animationType=ModelImporterAnimationType.Generic;importer.avatarSetup=ModelImporterAvatarSetup.CreateFromThisModel;importer.isReadable=true;importer.meshCompression=ModelImporterMeshCompression.Off;importer.skinWeights=ModelImporterSkinWeights.Custom;importer.maxBonesPerVertex=8;importer.minBoneWeight=0;importer.animationCompression=ModelImporterAnimationCompression.Off;importer.SaveAndReimport();
  var clips=AssetDatabase.LoadAllAssetsAtPath(Folder+"/web-motions.fbx").OfType<AnimationClip>().Where(c=>!c.name.StartsWith("__preview")).ToArray();
  if(clips.Length<12)throw new System.Exception("Expected complete native Meshy animation pack, got "+clips.Length);
  var material=new Material(Shader.Find("Unlit/Texture"));material.mainTexture=AssetDatabase.LoadAssetAtPath<Texture2D>(Folder+"/albedo.png");AssetDatabase.DeleteAsset(Folder+"/source.mat");AssetDatabase.CreateAsset(material,Folder+"/source.mat");
  AssetDatabase.DeleteAsset(Folder+"/native.controller");var controller=AnimatorController.CreateAnimatorControllerAtPath(Folder+"/native.controller");foreach(var clip in clips)controller.AddMotion(clip).name=clip.name;
  var obj=Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(Folder+"/web-motions.fbx"));foreach(var skin in obj.GetComponentsInChildren<SkinnedMeshRenderer>()){skin.sharedMaterials=skin.sharedMaterials.Select(m=>material).ToArray();skin.quality=SkinQuality.Auto;}obj.GetComponent<Animator>().runtimeAnimatorController=controller;PrefabUtility.SaveAsPrefabAsset(obj,Folder+"/native-review.prefab");Object.DestroyImmediate(obj);AssetDatabase.SaveAssets();
  QualitySettings.skinWeights=SkinWeights.Unlimited;ShaderUtil.allowAsyncCompilation=false;EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);RenderSettings.ambientLight=new Color(.4f,.4f,.4f);
  var light=new GameObject("Key").AddComponent<Light>();light.type=LightType.Directional;light.intensity=1;light.transform.rotation=Quaternion.Euler(25,-35,0);
  var camera=new GameObject("Camera").AddComponent<Camera>();camera.backgroundColor=new Color(.06f,.07f,.09f);camera.clearFlags=CameraClearFlags.SolidColor;camera.fieldOfView=35;
  obj=Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(Folder+"/native-review.prefab"));var anim=obj.GetComponent<Animator>();anim.runtimeAnimatorController=null;anim.Rebind();anim.enabled=false;
  Directory.CreateDirectory("../art/previews/meshy-web");
  foreach(var clip in clips){clip.SampleAnimation(obj,0);var skeleton=SnapshotSkeleton(obj);var frozen=new System.Collections.Generic.List<GameObject>();foreach(var skin in obj.GetComponentsInChildren<SkinnedMeshRenderer>()){var mesh=new Mesh();skin.BakeMesh(mesh,false);var baked=new GameObject("Baked sampled pose");baked.transform.SetParent(skin.transform,false);var scale=skin.transform.localScale;baked.transform.localScale=new Vector3(1/scale.x,1/scale.y,1/scale.z);baked.AddComponent<MeshFilter>().sharedMesh=mesh;baked.AddComponent<MeshRenderer>().sharedMaterials=skin.sharedMaterials;skin.enabled=false;frozen.Add(baked);}Bounds bounds=frozen[0].GetComponent<MeshRenderer>().bounds;camera.transform.position=bounds.center+new Vector3(.3f,.05f,bounds.size.y*1.9f);camera.transform.LookAt(bounds.center);var rt=new RenderTexture(900,1100,24){antiAliasing=4};camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;var png=new Texture2D(900,1100,TextureFormat.RGB24,false);png.ReadPixels(new Rect(0,0,900,1100),0,0);DrawSkeleton(png,camera,skeleton);png.Apply();File.WriteAllBytes("../art/previews/meshy-web/unity-"+clip.name.Replace('|','-')+".png",png.EncodeToPNG());RenderTexture.active=null;camera.targetTexture=null;Object.DestroyImmediate(png);Object.DestroyImmediate(rt);foreach(var baked in frozen){Object.DestroyImmediate(baked.GetComponent<MeshFilter>().sharedMesh);Object.DestroyImmediate(baked);}foreach(var skin in obj.GetComponentsInChildren<SkinnedMeshRenderer>())skin.enabled=true;}
  var audit=new System.Text.StringBuilder();
  foreach(var clip in clips) {
   for(int frame=0;frame<=60;frame++) {
    clip.SampleAnimation(obj,clip.length*frame/60f);
    foreach(var skin in obj.GetComponentsInChildren<SkinnedMeshRenderer>()) {
     var mesh=new Mesh();skin.BakeMesh(mesh,false);
     foreach(var v in mesh.vertices) if(float.IsNaN(v.x)||float.IsInfinity(v.x)||float.IsNaN(v.y)||float.IsInfinity(v.y)||float.IsNaN(v.z)||float.IsInfinity(v.z))throw new System.Exception("Non-finite Meshy pose");
     if(mesh.bounds.size.magnitude>5)throw new System.Exception("Exploded Meshy pose");
     Object.DestroyImmediate(mesh);
    }
   }
   audit.AppendLine(clip.name+": 61 sampled poses finite; bounds below 5 m");
  }
  File.WriteAllText("../art/source/original-island/web-unity-audit.txt",audit.ToString());
  Object.DestroyImmediate(obj);
 }
 static Vector3[][] SnapshotSkeleton(GameObject obj) {
  var bones=new System.Collections.Generic.HashSet<Transform>(obj.GetComponentsInChildren<SkinnedMeshRenderer>().SelectMany(r=>r.bones).Where(b=>b));
  return bones.Select(b=>new[]{b.position,b.parent&&bones.Contains(b.parent)?b.parent.position:b.position}).ToArray();
 }
 static void DrawSkeleton(Texture2D image,Camera camera,Vector3[][] bones) {
  foreach(var bone in bones) {
   var point=camera.WorldToViewportPoint(bone[0]);if(point.z<=0)continue;
   int x=Mathf.RoundToInt(point.x*image.width),y=Mathf.RoundToInt(point.y*image.height);
   var parent=camera.WorldToViewportPoint(bone[1]);int px=Mathf.RoundToInt(parent.x*image.width),py=Mathf.RoundToInt(parent.y*image.height);
   int length=Mathf.Max(Mathf.Abs(x-px),Mathf.Abs(y-py));
   for(int i=0;i<=length;i++)Dot(image,Mathf.RoundToInt(Mathf.Lerp(px,x,i/(float)Mathf.Max(1,length))),Mathf.RoundToInt(Mathf.Lerp(py,y,i/(float)Mathf.Max(1,length))),1,new Color(.7f,1,.15f));
   Dot(image,x,y,4,new Color(1,.65f,.15f));
  }
 }
 static void Dot(Texture2D image,int x,int y,int radius,Color color) {for(int dx=-radius;dx<=radius;dx++)for(int dy=-radius;dy<=radius;dy++)if(dx*dx+dy*dy<=radius*radius&&x+dx>=0&&x+dx<image.width&&y+dy>=0&&y+dy<image.height)image.SetPixel(x+dx,y+dy,color);}

}
}

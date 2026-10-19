using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace LighFight.Editor {
 public static class WebGreetingComparison {
  public static void Run() {
   const string path="Assets/LighFight/Art/Drafts/IslandWebVisible/web-greeting.fbx";
   var importer=(ModelImporter)AssetImporter.GetAtPath(path);
   importer.animationType=ModelImporterAnimationType.Generic;importer.avatarSetup=ModelImporterAvatarSetup.CreateFromThisModel;
   importer.meshCompression=ModelImporterMeshCompression.Off;importer.animationCompression=ModelImporterAnimationCompression.Off;
   importer.skinWeights=ModelImporterSkinWeights.Custom;importer.maxBonesPerVertex=8;importer.minBoneWeight=0;importer.isReadable=true;importer.SaveAndReimport();
   EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);ShaderUtil.allowAsyncCompilation=false;
   var obj=Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(path));
   foreach(var filter in obj.GetComponentsInChildren<MeshFilter>())if(filter.sharedMesh.vertexCount<1000)filter.gameObject.SetActive(false);
   var material=AssetDatabase.LoadAssetAtPath<Material>("Assets/LighFight/Art/Drafts/IslandWebVisible/Pbr/native.mat");
   var unlit=new Material(Shader.Find("Unlit/Texture")){mainTexture=material.mainTexture};
   foreach(var skin in obj.GetComponentsInChildren<SkinnedMeshRenderer>()){skin.sharedMaterials=skin.sharedMaterials.Select(_=>unlit).ToArray();skin.quality=SkinQuality.Auto;skin.updateWhenOffscreen=true;}
   QualitySettings.skinWeights=SkinWeights.Unlimited;
   var clip=AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>().First(c=>!c.name.StartsWith("__preview"));
   clip.SampleAnimation(obj,clip.length*.5f);obj.transform.rotation=Quaternion.Euler(0,180,0)*obj.transform.rotation;
   var skins=obj.GetComponentsInChildren<SkinnedMeshRenderer>();var bounds=skins[0].bounds;foreach(var r in skins)bounds.Encapsulate(r.bounds);
   var cam=new GameObject("Exact web FBX camera").AddComponent<Camera>();cam.clearFlags=CameraClearFlags.SolidColor;cam.backgroundColor=new Color(.04f,.08f,.14f);cam.orthographic=true;cam.orthographicSize=bounds.size.y*.55f;cam.transform.position=bounds.center+Vector3.back*bounds.size.y*2;cam.transform.LookAt(bounds.center);
   var rt=new RenderTexture(700,1000,24){antiAliasing=4};cam.targetTexture=rt;cam.Render();RenderTexture.active=rt;
   var image=new Texture2D(700,1000,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,700,1000),0,0);image.Apply();
   File.WriteAllBytes("../art/previews/source-quality/unity-direct-web-greeting.png",image.EncodeToPNG());
   File.WriteAllText("../art/previews/source-quality/unity-direct-web-greeting.txt",$"{skins.Sum(s=>s.sharedMesh.triangles.Length/3)} triangles; direct web FBX; {clip.name}; {clip.length}s; 8 influences; no retarget or runtime pose");
   RenderTexture.active=null;cam.targetTexture=null;Object.DestroyImmediate(image);Object.DestroyImmediate(rt);Object.DestroyImmediate(obj);
  }
 }
}

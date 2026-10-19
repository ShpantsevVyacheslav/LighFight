using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace LighFight.Editor {
 public static class TripoShadowComparison {
  public static Material MaterialFor(string tag) {
   string folder="Assets/LighFight/Art/Drafts/Tripo"+char.ToUpperInvariant(tag[0])+tag.Substring(1),prefix=folder+"/"+tag+"-h31-unity-native-pack_";
   string albedo=prefix+"basecolor.JPEG",normal=prefix+"normal.JPEG";
   foreach(string path in new[]{albedo,normal}){var importer=(TextureImporter)AssetImporter.GetAtPath(path);if(!importer)throw new InvalidOperationException("Missing native texture "+path);importer.textureType=path==normal?TextureImporterType.NormalMap:TextureImporterType.Default;importer.sRGBTexture=path==albedo;importer.maxTextureSize=4096;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.SaveAndReimport();}
   var roughness=new Texture2D(2,2,TextureFormat.RGBA32,false,true);var metallic=new Texture2D(2,2,TextureFormat.RGBA32,false,true);Texture2D packed=null;
   try {
    if(!roughness.LoadImage(File.ReadAllBytes(prefix+"roughness.JPEG"))||!metallic.LoadImage(File.ReadAllBytes(prefix+"metallic.JPEG")))throw new InvalidOperationException("Invalid PBR maps");
    if(roughness.width!=metallic.width||roughness.height!=metallic.height)throw new InvalidOperationException("PBR maps must share dimensions");
    var r=roughness.GetPixels32();var m=metallic.GetPixels32();for(int i=0;i<r.Length;i++)r[i]=new Color32(m[i].r,0,0,(byte)(255-r[i].r));
    packed=new Texture2D(roughness.width,roughness.height,TextureFormat.RGBA32,false,true);packed.SetPixels32(r);packed.Apply();string packedPath=folder+"/metallic-smoothness.png";File.WriteAllBytes(packedPath,packed.EncodeToPNG());AssetDatabase.ImportAsset(packedPath,ImportAssetOptions.ForceSynchronousImport);
    var importer=(TextureImporter)AssetImporter.GetAtPath(packedPath);importer.sRGBTexture=false;importer.maxTextureSize=4096;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.SaveAndReimport();
    string materialPath=folder+"/native-pbr.mat";var material=AssetDatabase.LoadAssetAtPath<Material>(materialPath);if(!material){material=new Material(Shader.Find("LighFight/Native PBR"));AssetDatabase.CreateAsset(material,materialPath);}
    NativeMaterialImport.ApplySurfaceMode(material,true);material.color=Color.white;material.mainTexture=AssetDatabase.LoadAssetAtPath<Texture2D>(albedo);material.SetTexture("_BumpMap",AssetDatabase.LoadAssetAtPath<Texture2D>(normal));material.SetTexture("_MetallicGlossMap",AssetDatabase.LoadAssetAtPath<Texture2D>(packedPath));material.SetFloat("_BumpScale",1);material.SetFloat("_GlossMapScale",1);material.EnableKeyword("_NORMALMAP");material.EnableKeyword("_METALLICGLOSSMAP");EditorUtility.SetDirty(material);AssetDatabase.SaveAssets();return material;
   }finally{UnityEngine.Object.DestroyImmediate(roughness);UnityEngine.Object.DestroyImmediate(metallic);if(packed)UnityEngine.Object.DestroyImmediate(packed);}
  }
  static GameObject Actor(UnityEngine.SceneManagement.Scene scene,string tag,Material material,int side) {
   string folder="Assets/LighFight/Art/Drafts/Tripo"+char.ToUpperInvariant(tag[0])+tag.Substring(1),path=folder+"/native-pack.fbx";
   var model=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(path));EditorSceneManager.MoveGameObjectToScene(model,scene);
   foreach(var animator in model.GetComponentsInChildren<Animator>()){animator.Rebind();animator.Update(0);animator.enabled=false;}
   var skins=model.GetComponentsInChildren<SkinnedMeshRenderer>();Bounds bounds=default;bool first=true;
   foreach(var skin in skins){var mesh=new Mesh();skin.BakeMesh(mesh,false);var scale=skin.transform.localScale;var matrix=skin.transform.localToWorldMatrix*Matrix4x4.Scale(new Vector3(1/scale.x,1/scale.y,1/scale.z));foreach(var v in mesh.vertices){var point=matrix.MultiplyPoint3x4(v);if(first){bounds=new Bounds(point,Vector3.zero);first=false;}else bounds.Encapsulate(point);}UnityEngine.Object.DestroyImmediate(mesh);}
   var wrapper=new GameObject(tag);EditorSceneManager.MoveGameObjectToScene(wrapper,scene);model.transform.SetParent(wrapper.transform,false);wrapper.transform.localScale=Vector3.one*(1.78f/bounds.size.y);wrapper.transform.position=new Vector3(side*.85f,0,0);wrapper.transform.rotation=Quaternion.Euler(0,side<0?90:-90,0);
   foreach(var skin in skins){skin.sharedMaterials=skin.sharedMaterials.Select(_=>material).ToArray();skin.updateWhenOffscreen=true;skin.shadowCastingMode=ShadowCastingMode.On;skin.receiveShadows=true;}
   var clip=AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>().Single(c=>c.name=="box_02");clip.SampleAnimation(model,clip.length*.35f);return wrapper;
  }
  public static void Verify() {
   string output=Path.GetFullPath("../art/previews/tripo-shadows");var textures=new List<Texture2D>();
   try{var pixels=new List<Color32[]>();foreach(string name in new[]{"shadows-off","shadows-on","negative-control"}){var texture=new Texture2D(2,2,TextureFormat.RGBA32,false);textures.Add(texture);if(!texture.LoadImage(File.ReadAllBytes(output+"/"+name+".png")))throw new InvalidOperationException("Missing shadow image");pixels.Add(texture.GetPixels32());}
    int changed=0,unexpected=0,ground=0;for(int i=0;i<pixels[0].Length;i++){var a=pixels[0][i];var b=pixels[1][i];var c=pixels[2][i];if(Math.Abs(a.r-b.r)+Math.Abs(a.g-b.g)+Math.Abs(a.b-b.b)>12){changed++;if(i/1600<300)ground++;}if(Math.Abs(a.r-c.r)+Math.Abs(a.g-c.g)+Math.Abs(a.b-c.b)>3)unexpected++;}
    File.WriteAllText(output+"/audit.json","{\"shadowChangedPixels\":"+changed+",\"groundChangedPixels\":"+ground+",\"negativeControlChangedPixels\":"+unexpected+",\"sourceTextures\":4096,\"cameraFov\":42,\"separateProcessFrames\":true,\"deviceVerified\":false}");
    if(changed<1000||ground<1000||unexpected>100)throw new InvalidOperationException("Independent shadow comparison failed: "+changed+"/"+unexpected);Debug.Log("TRIPO_SHADOW_COMPARISON "+changed+" pixels, negative control "+unexpected);
   }finally{foreach(var texture in textures)UnityEngine.Object.DestroyImmediate(texture);}
  }
  public static void Run() {
   var oldShadows=QualitySettings.shadows;float oldDistance=QualitySettings.shadowDistance;var oldResolution=QualitySettings.shadowResolution;var oldAmbient=RenderSettings.ambientLight;var oldMode=RenderSettings.ambientMode;
   QualitySettings.skinWeights=SkinWeights.Unlimited;QualitySettings.shadowDistance=25;QualitySettings.shadowResolution=ShadowResolution.High;
   var materials=new[]{MaterialFor("rose"),MaterialFor("ranger")};var scene=EditorSceneManager.NewPreviewScene();var owned=new List<GameObject>();
   var render=new RenderTexture(1600,900,24);render.Create();var image=new Texture2D(1600,900,TextureFormat.RGB24,false);var previous=RenderTexture.active;
   try {
    var arena=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(TripoArenaSetup.Folder+"/arena.prefab"));owned.Add(arena);EditorSceneManager.MoveGameObjectToScene(arena,scene);foreach(var renderer in arena.GetComponentsInChildren<Renderer>()){renderer.receiveShadows=true;renderer.shadowCastingMode=ShadowCastingMode.On;}
    owned.Add(Actor(scene,"rose",materials[0],-1));owned.Add(Actor(scene,"ranger",materials[1],1));
    var lightObject=new GameObject("Game directional light");owned.Add(lightObject);EditorSceneManager.MoveGameObjectToScene(lightObject,scene);var light=lightObject.AddComponent<Light>();light.type=LightType.Directional;light.intensity=1.2f;light.transform.rotation=Quaternion.Euler(45,-30,0);light.shadowStrength=.65f;light.shadowBias=.03f;light.shadowNormalBias=.2f;
    RenderSettings.ambientMode=AmbientMode.Flat;RenderSettings.ambientLight=new Color(.45f,.43f,.4f);
    var cameraObject=new GameObject("Game camera");owned.Add(cameraObject);EditorSceneManager.MoveGameObjectToScene(cameraObject,scene);var camera=cameraObject.AddComponent<Camera>();camera.scene=scene;camera.enabled=false;camera.nearClipPlane=.05f;camera.transform.position=new Vector3(0,1.6f,-3.6f);camera.transform.LookAt(new Vector3(0,1,0));camera.fieldOfView=42;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.36f,.45f,.55f);camera.targetTexture=render;ShaderUtil.allowAsyncCompilation=false;
    string output=Path.GetFullPath("../art/previews/tripo-shadows");Directory.CreateDirectory(output);
    Func<string,Color32[]> capture=name=>{camera.Render();camera.Render();camera.Render();RenderTexture.active=render;image.ReadPixels(new Rect(0,0,1600,900),0,0);image.Apply();File.WriteAllBytes(output+"/"+name+".png",image.EncodeToPNG());return image.GetPixels32();};
    var args=Environment.GetCommandLineArgs();int modeIndex=Array.IndexOf(args,"-shadowMode");
    if(modeIndex>=0){bool enabled=args[modeIndex+1]=="on";QualitySettings.shadows=enabled?ShadowQuality.All:ShadowQuality.Disable;light.shadows=enabled?LightShadows.Soft:LightShadows.None;int nameIndex=Array.IndexOf(args,"-shadowName");capture(nameIndex>=0?args[nameIndex+1]:enabled?"shadows-on":"shadows-off");Debug.Log("TRIPO_SHADOW_FRAME "+enabled);return;}
    QualitySettings.shadows=ShadowQuality.Disable;light.shadows=LightShadows.None;var off=capture("shadows-off");
    QualitySettings.shadows=ShadowQuality.All;light.shadows=LightShadows.Soft;var on=capture("shadows-on");
    QualitySettings.shadows=ShadowQuality.Disable;light.shadows=LightShadows.None;var control=capture("negative-control");
    int changed=0,unexpected=0;for(int i=0;i<off.Length;i++){if(Math.Abs(off[i].r-on[i].r)+Math.Abs(off[i].g-on[i].g)+Math.Abs(off[i].b-on[i].b)>12)changed++;if(Math.Abs(off[i].r-control[i].r)+Math.Abs(off[i].g-control[i].g)+Math.Abs(off[i].b-control[i].b)>3)unexpected++;}
    File.WriteAllText(output+"/audit.json","{\"shadowChangedPixels\":"+changed+",\"negativeControlChangedPixels\":"+unexpected+",\"sourceTextures\":4096,\"cameraFov\":42,\"deviceVerified\":false}");
    if(changed<1000||unexpected>100)throw new InvalidOperationException("Shadow comparison or negative control failed: "+changed+"/"+unexpected);
    Debug.Log("TRIPO_SHADOW_COMPARISON "+changed+" changed pixels, negative control "+unexpected);
   }finally{RenderTexture.active=previous;UnityEngine.Object.DestroyImmediate(image);UnityEngine.Object.DestroyImmediate(render);foreach(var obj in owned)if(obj)UnityEngine.Object.DestroyImmediate(obj);EditorSceneManager.ClosePreviewScene(scene);QualitySettings.shadows=oldShadows;QualitySettings.shadowDistance=oldDistance;QualitySettings.shadowResolution=oldResolution;RenderSettings.ambientLight=oldAmbient;RenderSettings.ambientMode=oldMode;}
  }
 }
}

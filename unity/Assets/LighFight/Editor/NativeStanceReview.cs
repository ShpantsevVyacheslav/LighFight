using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace LighFight.Editor {
 public static class NativeStanceReview {
  public const string Source="Assets/LighFight/Art/Drafts/MeshyWebIsland/SourceStance/native-stance.fbx";
  public static void Run(){
   ShaderUtil.allowAsyncCompilation=false;QualitySettings.skinWeights=SkinWeights.Unlimited;QualitySettings.shadows=ShadowQuality.Disable;
   EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
   NativeFighterImport.PrepareImporter((ModelImporter)AssetImporter.GetAtPath(Source));
   var source=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(Source));
   var actor=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/LighFight/Art/Fighters/fighter-3.prefab"));
   var sourceSkin=source.GetComponentInChildren<SkinnedMeshRenderer>();var skin=actor.GetComponentInChildren<SkinnedMeshRenderer>();
   float bindError=0;
   for(int i=0;i<skin.bones.Length;i++){
    int j=Array.FindIndex(sourceSkin.bones,b=>b.name==skin.bones[i].name);if(j<0)throw new InvalidOperationException("Missing source bone "+skin.bones[i].name);
    for(int k=0;k<16;k++)bindError=Mathf.Max(bindError,Mathf.Abs(skin.sharedMesh.bindposes[i][k]-sourceSkin.sharedMesh.bindposes[j][k]));
   }
   if(bindError>0.0001f)throw new InvalidOperationException("Stance bind matrices differ: "+bindError);
   var clip=AssetDatabase.LoadAllAssetsAtPath(Source).OfType<AnimationClip>().Single(c=>!c.name.StartsWith("__preview"));NativeFighterImport.ValidateClip(actor,clip);
   actor.GetComponent<Animator>().enabled=false;UnityEngine.Object.DestroyImmediate(source);
   actor.transform.rotation=Quaternion.Euler(0,180,0);
   var mesh=new Mesh();var frozen=new GameObject("Sampled stance");frozen.transform.SetParent(skin.transform,false);var scale=skin.transform.localScale;frozen.transform.localScale=new Vector3(1/scale.x,1/scale.y,1/scale.z);frozen.AddComponent<MeshFilter>().sharedMesh=mesh;var renderer=frozen.AddComponent<MeshRenderer>();renderer.sharedMaterials=skin.sharedMaterials;skin.enabled=false;
   RenderSettings.ambientMode=UnityEngine.Rendering.AmbientMode.Flat;RenderSettings.ambientLight=new Color(.45f,.45f,.45f);
   var light=new GameObject("Neutral stance light").AddComponent<Light>();light.type=LightType.Directional;light.intensity=.7f;light.shadows=LightShadows.None;light.transform.rotation=Quaternion.Euler(20,-25,0);
   var cam=new GameObject("Stance review camera").AddComponent<Camera>();cam.orthographic=true;cam.clearFlags=CameraClearFlags.SolidColor;cam.backgroundColor=new Color(.04f,.08f,.14f);
   Directory.CreateDirectory("../art/previews/source-quality/stance-review");
   var original=renderer.sharedMaterial;var noNormal=new Material(original);noNormal.DisableKeyword("_NORMALMAP");noNormal.SetTexture("_BumpMap",null);
   var diffuse=new Material(noNormal);diffuse.DisableKeyword("_METALLICGLOSSMAP");diffuse.SetTexture("_MetallicGlossMap",null);diffuse.SetFloat("_Metallic",0);diffuse.SetFloat("_Glossiness",0);diffuse.EnableKeyword("_SPECULARHIGHLIGHTS_OFF");diffuse.EnableKeyword("_GLOSSYREFLECTIONS_OFF");
   var unlit=new Material(Shader.Find("LighFight/Native Unlit")){mainTexture=original.mainTexture};
   foreach(float phase in new[]{0f,.5f})foreach(string variant in new[]{"original","no-normal","diffuse","unlit"}){
    var mat=variant=="original"?original:variant=="no-normal"?noNormal:variant=="diffuse"?diffuse:unlit;renderer.sharedMaterials=renderer.sharedMaterials.Select(_=>mat).ToArray();
    clip.SampleAnimation(actor,clip.length*phase);skin.BakeMesh(mesh,false);mesh.RecalculateBounds();renderer.ResetBounds();var bounds=renderer.bounds;cam.orthographicSize=bounds.size.y*.55f;cam.transform.position=bounds.center+Vector3.back*4;cam.transform.LookAt(bounds.center);
    var rt=new RenderTexture(900,1100,24){antiAliasing=4};var image=new Texture2D(900,1100,TextureFormat.RGB24,false);
    try{cam.targetTexture=rt;cam.Render();RenderTexture.active=rt;image.ReadPixels(new Rect(0,0,900,1100),0,0);image.Apply();File.WriteAllBytes("../art/previews/source-quality/stance-review/unity-"+variant+"-"+phase+".png",image.EncodeToPNG());}
    finally{RenderTexture.active=null;cam.targetTexture=null;UnityEngine.Object.DestroyImmediate(image);UnityEngine.Object.DestroyImmediate(rt);}
   }
   File.WriteAllText("../art/previews/source-quality/stance-review/compatibility.txt","Bind matrix maximum difference="+bindError+"; source clip applied directly to existing fighter, no promotion or rest edits");
   UnityEngine.Object.DestroyImmediate(noNormal);UnityEngine.Object.DestroyImmediate(diffuse);UnityEngine.Object.DestroyImmediate(unlit);UnityEngine.Object.DestroyImmediate(mesh);UnityEngine.Object.DestroyImmediate(actor);UnityEngine.Object.DestroyImmediate(cam.gameObject);UnityEngine.Object.DestroyImmediate(light.gameObject);
  }
 }
}

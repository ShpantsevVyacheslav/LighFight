using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace LighFight.Editor {
 public static class NativeWebMotionReview {
  public static void Run() {
   ShaderUtil.allowAsyncCompilation=false;QualitySettings.skinWeights=SkinWeights.Unlimited;
   EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
   const string folder="Assets/LighFight/Art/Drafts/IslandWebVisible/";
   var original=AssetDatabase.LoadAssetAtPath<Material>(folder+"Pbr/native.mat");
   var unlit=new Material(Shader.Find("LighFight/Native Unlit")){mainTexture=original.mainTexture};
   foreach(var item in new[]{new[]{"source-wave.fbx","","wave"},new[]{"direct-jab.fbx","","jab"}}){
    var path=folder+item[0];var importer=(ModelImporter)AssetImporter.GetAtPath(path);
    NativeFighterImport.PrepareImporter(importer);
    var actor=Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(path));
    foreach(var animator in actor.GetComponentsInChildren<Animator>()){animator.Rebind();animator.Update(0);animator.enabled=false;}
    var skins=actor.GetComponentsInChildren<SkinnedMeshRenderer>();
    foreach(var skin in skins){skin.sharedMaterials=skin.sharedMaterials.Select(_=>unlit).ToArray();skin.updateWhenOffscreen=true;skin.quality=SkinQuality.Auto;}
    var clips=AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>().Where(c=>!c.name.StartsWith("__preview")&&(item[1]==""||c.name.Contains(item[1]))).ToArray();
    if(clips.Length!=1)throw new System.Exception("Expected one native motion: "+string.Join(",",clips.Select(c=>c.name)));
    var clip=clips[0];NativeFighterImport.ValidateClip(actor,clip);actor.transform.rotation=Quaternion.Euler(0,180,0)*actor.transform.rotation;
    var frozen=skins.Select(skin=>{var child=new GameObject("Sampled surface");child.transform.SetParent(skin.transform,false);var scale=skin.transform.localScale;child.transform.localScale=new Vector3(1/scale.x,1/scale.y,1/scale.z);child.AddComponent<MeshFilter>().sharedMesh=new Mesh();child.AddComponent<MeshRenderer>().sharedMaterials=skin.sharedMaterials;skin.enabled=false;return child;}).ToArray();
    var cam=new GameObject("Native motion camera").AddComponent<Camera>();cam.orthographic=true;cam.clearFlags=CameraClearFlags.SolidColor;cam.backgroundColor=new Color(.04f,.08f,.14f);
    foreach(var phase in new[]{0f,.35f,.65f}){
     clip.SampleAnimation(actor,clip.length*phase);for(int i=0;i<skins.Length;i++){var mesh=frozen[i].GetComponent<MeshFilter>().sharedMesh;skins[i].BakeMesh(mesh,false);mesh.RecalculateBounds();frozen[i].GetComponent<MeshRenderer>().ResetBounds();}
     var bounds=frozen[0].GetComponent<MeshRenderer>().bounds;foreach(var child in frozen)bounds.Encapsulate(child.GetComponent<MeshRenderer>().bounds);
     cam.orthographicSize=bounds.size.y*.55f;cam.transform.position=bounds.center+Vector3.back*4;cam.transform.LookAt(bounds.center);
     unlit.SetFloat("_Cull",2);Capture(cam,"../art/previews/source-quality/native-"+item[2]+"-"+Mathf.RoundToInt(phase*100)+"-culled.png");
     unlit.SetFloat("_Cull",0);Capture(cam,"../art/previews/source-quality/native-"+item[2]+"-"+Mathf.RoundToInt(phase*100)+".png");
    }
    File.WriteAllText("../art/previews/source-quality/native-"+item[2]+"-unity.txt",$"{skins.Sum(s=>s.sharedMesh.triangles.Length/3)} triangles; {clip.name}; {clip.length}s; original albedo; Generic native animation, 8 influences, no compression, no FighterView or retarget; desktop audit, not phone verification");
    foreach(var child in frozen)Object.DestroyImmediate(child.GetComponent<MeshFilter>().sharedMesh);Object.DestroyImmediate(actor);Object.DestroyImmediate(cam.gameObject);
   }
   Object.DestroyImmediate(unlit);
  }
  static void Capture(Camera cam,string path){var rt=new RenderTexture(900,1100,24){antiAliasing=4};cam.targetTexture=rt;cam.Render();RenderTexture.active=rt;var t=new Texture2D(900,1100,TextureFormat.RGB24,false);t.ReadPixels(new Rect(0,0,900,1100),0,0);t.Apply();File.WriteAllBytes(path,t.EncodeToPNG());RenderTexture.active=null;cam.targetTexture=null;Object.DestroyImmediate(t);Object.DestroyImmediate(rt);}
 }
}

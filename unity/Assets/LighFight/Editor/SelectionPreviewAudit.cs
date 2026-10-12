using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using LighFight.Runtime;
using LighFight.Core;
namespace LighFight.Editor {
 public static class SelectionPreviewAudit {
  public static void Run(){
   ShaderUtil.allowAsyncCompilation=false;EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
   var actor=Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/LighFight/Art/Fighters/fighter-3.prefab"));
   actor.GetComponent<FighterView>().Pose(new Fighter(),1,0);actor.transform.rotation=Quaternion.Euler(0,195,0);
   QualitySettings.skinWeights=SkinWeights.Unlimited;
   var light=new GameObject("Runtime key").AddComponent<Light>();light.type=LightType.Directional;light.intensity=1.2f;light.transform.rotation=Quaternion.Euler(45,-30,0);RenderSettings.ambientLight=new Color(.45f,.43f,.4f);
   var camera=new GameObject("Selection camera").AddComponent<Camera>();camera.fieldOfView=42;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.045f,.085f,.15f);camera.transform.position=new Vector3(0,1.18f,-2.65f);camera.transform.LookAt(new Vector3(0,1,0));
   string folder="../art/previews/source-quality";Directory.CreateDirectory(folder);
   var size=Prototype.SelectionPreviewSize(new Vector2(2868,1320),2);
   var old=new RenderTexture(800,720,24);var physical=new RenderTexture(size.x,size.y,0);var native=new RenderTexture(size.x,size.y,24){antiAliasing=4};
   camera.targetTexture=old;camera.aspect=800f/720;camera.Render();Graphics.Blit(old,physical);Save(physical,folder+"/preview-old-stretched.png");
   camera.targetTexture=native;camera.aspect=(float)size.x/size.y;camera.Render();Save(native,folder+"/preview-native-aspect.png");
   File.WriteAllText(folder+"/preview-resolution-audit.txt",$"Same production model, native Idle, unchanged mesh/materials/weights. {actor.GetComponentsInChildren<SkinnedMeshRenderer>().Sum(s=>s.sharedMesh.triangles.Length/3)} triangles. Before: 800x720 rendered then stretched to {size.x}x{size.y}. After: {size.x}x{size.y} MSAA4. Not a device screenshot or artistic skinning acceptance.");
   camera.targetTexture=null;RenderTexture.active=null;Object.DestroyImmediate(old);Object.DestroyImmediate(physical);Object.DestroyImmediate(native);Object.DestroyImmediate(actor);
  }
  static void Save(RenderTexture rt,string path){RenderTexture.active=rt;var texture=new Texture2D(rt.width,rt.height,TextureFormat.RGB24,false);texture.ReadPixels(new Rect(0,0,rt.width,rt.height),0,0);texture.Apply();File.WriteAllBytes(path,texture.EncodeToPNG());Object.DestroyImmediate(texture);RenderTexture.active=null;}
 }
}

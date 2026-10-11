using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using LighFight.Core;
using LighFight.Runtime;
namespace LighFight.Editor {
public static class CharacterQualityReview {
 public static void Capture() {
  EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
  RenderSettings.ambientMode=UnityEngine.Rendering.AmbientMode.Flat;
  RenderSettings.ambientLight=new Color(.38f,.40f,.46f);
  var light=new GameObject("Key").AddComponent<Light>();light.type=LightType.Directional;light.intensity=1.1f;light.transform.rotation=Quaternion.Euler(25,-35,0);
  var camera=new GameObject("Review camera").AddComponent<Camera>();camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.055f,.065f,.085f);camera.fieldOfView=35;
  string tag=System.Environment.GetEnvironmentVariable("LIGHFIGHT_REVIEW_TAG")??"current";
  string folder=Path.GetFullPath("../art/previews/character-quality");Directory.CreateDirectory(folder);
  foreach(int id in new[]{3,4}) {
   var obj=Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>($"Assets/LighFight/Art/Fighters/fighter-{id}.prefab"));
   obj.GetComponent<FighterView>().Pose(new Fighter{Guard=true},1,0);obj.GetComponent<Animator>().Update(0);obj.GetComponent<JointCorrectives>()?.Apply();
   Bounds bounds=new Bounds();bool first=true;
   foreach(var skin in obj.GetComponentsInChildren<SkinnedMeshRenderer>()) {var mesh=new Mesh();skin.BakeMesh(mesh);var frozen=new GameObject("Frozen");frozen.transform.SetParent(skin.transform,false);frozen.AddComponent<MeshFilter>().sharedMesh=mesh;var r=frozen.AddComponent<MeshRenderer>();r.sharedMaterials=skin.sharedMaterials;skin.enabled=false;if(first){bounds=r.bounds;first=false;}else bounds.Encapsulate(r.bounds);}
   camera.transform.position=bounds.center+new Vector3(.35f,.08f,-bounds.size.y*1.9f);camera.transform.LookAt(bounds.center);
   var rt=new RenderTexture(900,1000,24);camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;var png=new Texture2D(900,1000,TextureFormat.RGB24,false);png.ReadPixels(new Rect(0,0,900,1000),0,0);png.Apply();File.WriteAllBytes(Path.Combine(folder,$"fighter-{id}-{tag}.png"),png.EncodeToPNG());RenderTexture.active=null;camera.targetTexture=null;Object.DestroyImmediate(rt);Object.DestroyImmediate(png);Object.DestroyImmediate(obj);
  }
 }
}}

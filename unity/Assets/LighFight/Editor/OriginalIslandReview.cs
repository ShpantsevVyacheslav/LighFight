using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using LighFight.Core;
using LighFight.Runtime;
namespace LighFight.Editor {
public static class OriginalIslandReview {
 public static void Capture() {
  ShaderUtil.allowAsyncCompilation=false;
  EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
  RenderSettings.ambientMode=UnityEngine.Rendering.AmbientMode.Flat;
  RenderSettings.ambientLight=new Color(.38f,.40f,.46f);
  var light=new GameObject("Key").AddComponent<Light>();light.type=LightType.Directional;light.intensity=1.1f;light.transform.rotation=Quaternion.Euler(25,-35,0);
  var camera=new GameObject("Review camera").AddComponent<Camera>();camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.055f,.065f,.085f);camera.fieldOfView=35;
  string tag=System.Environment.GetEnvironmentVariable("LIGHFIGHT_REVIEW_TAG")??"original-source";
  string folder=Path.GetFullPath("../art/previews/character-quality");Directory.CreateDirectory(folder);
  bool portraits=tag=="portraits";
  foreach(int id in portraits?new[]{0,1,2,3,4,5,6,7}:new[]{3}) {
   var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(OriginalIslandSetup.Folder+"/candidate.prefab");
   if(prefab==null)prefab=AssetDatabase.LoadAssetAtPath<GameContent>("Assets/LighFight/Content/GameContent.asset").fighters[id];
   var obj=Object.Instantiate(prefab);
   obj.GetComponent<FighterView>().Pose(new Fighter{Guard=true},1,0);var animator=obj.GetComponent<Animator>();if(animator!=null)animator.Update(0);var correctives=obj.GetComponent<JointCorrectives>();if(correctives!=null)correctives.Apply();
   Bounds bounds=new Bounds();bool first=true;
   foreach(var skin in obj.GetComponentsInChildren<SkinnedMeshRenderer>()) {var mesh=new Mesh();skin.BakeMesh(mesh);var frozen=new GameObject("Frozen");frozen.transform.SetParent(skin.transform,false);frozen.AddComponent<MeshFilter>().sharedMesh=mesh;var r=frozen.AddComponent<MeshRenderer>();r.sharedMaterials=skin.sharedMaterials;skin.enabled=false;if(first){bounds=r.bounds;first=false;}else bounds.Encapsulate(r.bounds);}
   if(first)foreach(var renderer in obj.GetComponentsInChildren<Renderer>()){if(first){bounds=renderer.bounds;first=false;}else bounds.Encapsulate(renderer.bounds);}
   camera.transform.position=bounds.center+new Vector3(.35f,.08f,-bounds.size.y*1.9f);camera.transform.LookAt(bounds.center);
   if(portraits){var target=bounds.center+Vector3.up*bounds.size.y*.28f;camera.transform.position=target+new Vector3(.12f,0,-bounds.size.y*.72f);camera.transform.LookAt(target);}
   if(tag.Contains("close")){var focus=obj.GetComponent<FighterView>().BonePoint("Head",Vector3.up*1.6f)+Vector3.down*.10f;camera.transform.position=focus+new Vector3(.3f,.02f,-1.25f);camera.transform.LookAt(focus);}
   int width=portraits?180:900,height=portraits?200:1000;var rt=new RenderTexture(width,height,24);camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;var png=new Texture2D(width,height,TextureFormat.RGB24,false);png.ReadPixels(new Rect(0,0,width,height),0,0);png.Apply();File.WriteAllBytes(portraits?$"Assets/LighFight/Resources/Selection/fighter-{id}.png":Path.Combine(folder,$"fighter-{id}-{tag}.png"),png.EncodeToPNG());RenderTexture.active=null;camera.targetTexture=null;Object.DestroyImmediate(rt);Object.DestroyImmediate(png);Object.DestroyImmediate(obj);
  }
 }
}}

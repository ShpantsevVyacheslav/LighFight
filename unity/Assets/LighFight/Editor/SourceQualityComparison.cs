using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace LighFight.Editor {
 public static class SourceQualityComparison {
  public static void Run(){
   ShaderUtil.allowAsyncCompilation=false;
   EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
   RenderSettings.ambientMode=UnityEngine.Rendering.AmbientMode.Flat;RenderSettings.ambientLight=new Color(.5f,.5f,.5f);
   var light=new GameObject("Soft key").AddComponent<Light>();light.type=LightType.Directional;light.intensity=.55f;light.transform.rotation=Quaternion.Euler(20,-25,0);
   var camera=new GameObject("Comparison camera").AddComponent<Camera>();camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.04f,.08f,.14f);camera.orthographic=true;
   Directory.CreateDirectory("../art/previews/source-quality");
   var material=NativeMaterialImport.Create("Assets/LighFight/Art/Drafts/IslandWebVisible/Pbr","../art/source/island-web-visible/model_textures/base_color.png","../art/source/island-web-visible/model_textures/normal.png","../art/source/island-web-visible/model_textures/metallic_roughness.png");
   var source=Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/LighFight/Art/Drafts/IslandWebVisible/source.fbx"));foreach(var r in source.GetComponentsInChildren<Renderer>())r.sharedMaterials=r.sharedMaterials.Select(_=>material).ToArray();source.transform.rotation=Quaternion.Euler(0,180,0)*source.transform.rotation;
   var renderers=source.GetComponentsInChildren<Renderer>();var bounds=renderers[0].bounds;foreach(var r in renderers)bounds.Encapsulate(r.bounds);
   // Fixed normalized framing of original neutral surface, no Animator or FighterView.
   var head=new Vector3(bounds.center.x,bounds.max.y-bounds.size.y*.085f,bounds.center.z);camera.orthographicSize=bounds.size.y*.13f;camera.transform.position=head+Vector3.back*bounds.size.y*2;camera.transform.LookAt(head);
   Capture(camera,"../art/previews/source-quality/island-visible-source-portrait.png",1024);
   // Isolate material contributions using unchanged geometry, camera and lights.
   var noNormal=new Material(material);noNormal.DisableKeyword("_NORMALMAP");noNormal.SetTexture("_BumpMap",null);
   foreach(var r in renderers)r.sharedMaterials=r.sharedMaterials.Select(_=>noNormal).ToArray();
   Capture(camera,"../art/previews/source-quality/island-visible-no-normal.png",1024);
   var diffuse=new Material(noNormal);diffuse.DisableKeyword("_METALLICGLOSSMAP");diffuse.SetTexture("_MetallicGlossMap",null);diffuse.SetFloat("_Metallic",0);diffuse.SetFloat("_Glossiness",0);diffuse.SetFloat("_SpecularHighlights",0);diffuse.SetFloat("_GlossyReflections",0);
   foreach(var r in renderers)r.sharedMaterials=r.sharedMaterials.Select(_=>diffuse).ToArray();
   Capture(camera,"../art/previews/source-quality/island-visible-diffuse.png",1024);
   var unlit=new Material(Shader.Find("Unlit/Texture"));unlit.mainTexture=material.mainTexture;foreach(var r in renderers)r.sharedMaterials=r.sharedMaterials.Select(_=>unlit).ToArray();
   Capture(camera,"../art/previews/source-quality/island-visible-source-unlit.png",1024);
   Capture(camera,"Assets/LighFight/Resources/Selection/fighter-3.png",512);
   foreach(var r in renderers)r.sharedMaterials=r.sharedMaterials.Select(_=>material).ToArray();
   camera.orthographicSize=bounds.size.y*.55f;camera.transform.position=bounds.center+Vector3.back*bounds.size.y*2;camera.transform.LookAt(bounds.center);Capture(camera,"../art/previews/source-quality/island-visible-source-full.png",1024);
   File.WriteAllText("../art/previews/source-quality/source-unity-count.txt",source.GetComponentsInChildren<MeshFilter>().Sum(r=>r.sharedMesh.triangles.Length/3)+" triangles; no animation; no deformation; original PBR textures");
   Object.DestroyImmediate(source);AssetDatabase.Refresh();
  }
  static void Capture(Camera camera,string path,int size){var rt=new RenderTexture(size,size,24){antiAliasing=4};camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;var image=new Texture2D(size,size,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,size,size),0,0);image.Apply();File.WriteAllBytes(path,image.EncodeToPNG());RenderTexture.active=null;camera.targetTexture=null;Object.DestroyImmediate(image);Object.DestroyImmediate(rt);}
 }
}

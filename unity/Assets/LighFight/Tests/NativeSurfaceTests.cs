using System;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;
public class NativeSurfaceTests {
 [Test]
 public void DoubleSidedSourceMaterialRendersTheInsideOfClothing() {
  var type=AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("LighFight.Editor.NativeMaterialImport")).First(t=>t!=null);
  var mat=new Material(Shader.Find("Standard"));var quad=GameObject.CreatePrimitive(PrimitiveType.Quad);quad.layer=31;
  var cameraObject=new GameObject("Native surface regression camera");var camera=cameraObject.AddComponent<Camera>();camera.cullingMask=1<<31;camera.orthographic=true;camera.orthographicSize=.65f;camera.transform.position=Vector3.back*2;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Color.black;
  var target=new RenderTexture(64,64,24);var pixels=new Texture2D(64,64,TextureFormat.RGB24,false);
  var ambient=RenderSettings.ambientLight;var mode=RenderSettings.ambientMode;var active=RenderTexture.active;
  try {
   type.GetMethod("ApplySurfaceMode").Invoke(null,new object[]{mat,true});mat.color=Color.white;mat.SetFloat("_Glossiness",0);quad.GetComponent<Renderer>().sharedMaterial=mat;
   RenderSettings.ambientMode=AmbientMode.Flat;RenderSettings.ambientLight=Color.white;camera.targetTexture=target;
   Func<int> coverage=()=>{camera.Render();RenderTexture.active=target;pixels.ReadPixels(new Rect(0,0,64,64),0,0);pixels.Apply();return pixels.GetPixels().Count(c=>c.maxColorComponent>.03f);};
   Assert.Greater(coverage(),1000,"Front face must render before testing the reverse side");quad.transform.rotation=Quaternion.Euler(0,180,0);
   Assert.Greater(coverage(),1000,"Meshy doubleSided material must retain the inside of sleeves");
   mat.SetFloat("_Cull",2);Assert.Less(coverage(),10,"Back-face mutation must expose the original missing surface");
  } finally {
   RenderTexture.active=active;RenderSettings.ambientMode=mode;RenderSettings.ambientLight=ambient;camera.targetTexture=null;
   UnityEngine.Object.DestroyImmediate(quad);UnityEngine.Object.DestroyImmediate(cameraObject);UnityEngine.Object.DestroyImmediate(mat);UnityEngine.Object.DestroyImmediate(target);UnityEngine.Object.DestroyImmediate(pixels);
  }
 }
}

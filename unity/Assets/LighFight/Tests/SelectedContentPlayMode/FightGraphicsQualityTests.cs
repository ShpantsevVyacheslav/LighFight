using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using LighFight.Runtime;
namespace LighFight.Tests {
 public sealed class FightGraphicsQualityTests {
  const BindingFlags Flags=BindingFlags.Instance|BindingFlags.NonPublic;
  readonly string[] ints={"lf.shadows","lf.swipes","lf.graphics","lf.fps","lf.sound","lf.shake","lf.left","lf.diagnostics","lf.attackButton.0","lf.attackButton.1","lf.attackButton.2","lf.attackButton.3"};
  readonly string[] floats={"lf.controlScale","lf.controlOpacity"};
  readonly Dictionary<string,float> prefs=new Dictionary<string,float>();readonly HashSet<string> existing=new HashSet<string>();
  int aa,vsync,fps,mips;float lod,distance;SkinWeights skin;ShadowQuality shadows;ShadowResolution resolution;GameObject root;Prototype game;
  [SetUp]public void Setup(){
   foreach(var key in ints){if(PlayerPrefs.HasKey(key))existing.Add(key);prefs[key]=PlayerPrefs.GetInt(key);}foreach(var key in floats){if(PlayerPrefs.HasKey(key))existing.Add(key);prefs[key]=PlayerPrefs.GetFloat(key);}
   aa=QualitySettings.antiAliasing;vsync=QualitySettings.vSyncCount;fps=Application.targetFrameRate;mips=QualitySettings.globalTextureMipmapLimit;lod=QualitySettings.lodBias;skin=QualitySettings.skinWeights;distance=QualitySettings.shadowDistance;shadows=QualitySettings.shadows;resolution=QualitySettings.shadowResolution;
   root=new GameObject("real graphics options test");game=root.AddComponent<Prototype>();PlayerPrefs.SetInt("lf.diagnostics",0);
  }
  [TearDown]public void Cleanup(){
   Object.DestroyImmediate(root);QualitySettings.antiAliasing=aa;QualitySettings.vSyncCount=vsync;Application.targetFrameRate=fps;QualitySettings.globalTextureMipmapLimit=mips;QualitySettings.lodBias=lod;QualitySettings.skinWeights=skin;QualitySettings.shadowDistance=distance;QualitySettings.shadows=shadows;QualitySettings.shadowResolution=resolution;
   foreach(var key in ints)if(existing.Contains(key))PlayerPrefs.SetInt(key,(int)prefs[key]);else PlayerPrefs.DeleteKey(key);foreach(var key in floats)if(existing.Contains(key))PlayerPrefs.SetFloat(key,prefs[key]);else PlayerPrefs.DeleteKey(key);PlayerPrefs.Save();prefs.Clear();existing.Clear();
  }
  void Set(string name,object value)=>typeof(Prototype).GetField(name,Flags).SetValue(game,value);
  void Apply()=>typeof(Prototype).GetMethod("ApplyOptions",Flags).Invoke(game,null);
  [Test]public void TiersChangeActualRenderBudgetsWhileKeepingSourceDetail(){
   var light=new GameObject("late authored light").AddComponent<Light>();light.transform.SetParent(root.transform);light.gameObject.SetActive(false);
   var aaValues=new List<int>();var distances=new List<float>();var maps=new List<ShadowResolution>();
   for(int tier=0;tier<3;tier++){
    Set("graphicsQuality",tier);Set("shadowsEnabled",true);Apply();aaValues.Add(QualitySettings.antiAliasing);distances.Add(QualitySettings.shadowDistance);maps.Add(QualitySettings.shadowResolution);
    Assert.AreEqual(tier==0?LightShadows.Hard:LightShadows.Soft,light.shadows,"Inactive/new actors must get current lighting budget");
    Assert.AreEqual(0,QualitySettings.globalTextureMipmapLimit,"Original texture mip detail stays available");Assert.AreEqual(SkinWeights.Unlimited,QualitySettings.skinWeights);Assert.AreEqual(4,QualitySettings.lodBias,"Low tier must not choose reduced model detail");
   }
   CollectionAssert.AreEqual(new[]{0,2,4},aaValues);Assert.Less(distances[0],distances[1]);Assert.Less(distances[1],distances[2]);Assert.AreNotEqual(maps[0],maps[1]);Assert.AreNotEqual(maps[1],maps[2]);
   Set("shadowsEnabled",false);Apply();Assert.AreEqual(ShadowQuality.Disable,QualitySettings.shadows);Assert.AreEqual(LightShadows.None,light.shadows);
  }
  [TestCase(30)][TestCase(60)]public void RequestedFpsPersistsAndOverridesDesktopVsync(int requested){
   Set("graphicsQuality",0);Set("frameRate",requested);QualitySettings.vSyncCount=1;Apply();Assert.AreEqual(requested,Application.targetFrameRate);Assert.AreEqual(0,QualitySettings.vSyncCount);Assert.AreEqual(requested,PlayerPrefs.GetInt("lf.fps"));
   Set("graphicsQuality",2);Set("frameRate",requested==30?60:30);typeof(Prototype).GetMethod("LoadOptions",Flags).Invoke(game,null);
   Assert.AreEqual(requested,Application.targetFrameRate);Assert.AreEqual(0,QualitySettings.antiAliasing,"Saved Low tier must be applied on reload");
  }
  [Test]public void CorruptSavedTierAndFpsAreNormalizedAndReapplied(){
   PlayerPrefs.SetInt("lf.graphics",-100);PlayerPrefs.SetInt("lf.fps",-1);typeof(Prototype).GetMethod("LoadOptions",Flags).Invoke(game,null);
   Assert.AreEqual(0,PlayerPrefs.GetInt("lf.graphics"));Assert.AreEqual(60,PlayerPrefs.GetInt("lf.fps"));Assert.AreEqual(0,QualitySettings.antiAliasing);Assert.AreEqual(60,Application.targetFrameRate);
  }
  [Test]public void MobileFramebufferBudgetRestoresMaximumWithoutCompoundingAndPreservesAspect(){
   var original=new Vector2Int(2532,1170);var low=MobileFightResolution.Size(original,FightGraphicsQuality.RenderScale(0),true);var high=MobileFightResolution.Size(original,FightGraphicsQuality.RenderScale(1),true);var maximum=MobileFightResolution.Size(original,FightGraphicsQuality.RenderScale(2),true);
   Assert.Less(low.x*low.y,high.x*high.y);Assert.Less(high.x*high.y,maximum.x*maximum.y);Assert.AreEqual(original,maximum);
   Assert.AreEqual((float)original.x/original.y,(float)low.x/low.y,.003f);var portrait=MobileFightResolution.Size(original,FightGraphicsQuality.RenderScale(0),false);Assert.AreEqual(new Vector2Int(low.y,low.x),portrait);
   Assert.Less(Prototype.SelectionPreviewSize(original,0).x,Prototype.SelectionPreviewSize(original,1).x);
  }

  [UnityTest]public IEnumerator ActualFightStartupLightsDiffuseSurfacesIndependentlyOfIncomingSkybox(){
   if(SystemInfo.graphicsDeviceType==GraphicsDeviceType.Null)Assert.Ignore("Requires a real graphics device and camera render");
   var previous=SceneManager.GetActiveScene();var scene=SceneManager.CreateScene("Fight startup ambient GPU regression");
   Material sky=null,mat=null;Cubemap cube=null;RenderTexture target=null;Texture2D readback=null;
   try{
    SceneManager.SetActiveScene(scene);SceneManager.MoveGameObjectToScene(root,scene);game.enabled=false;
    cube=new Cubemap(4,TextureFormat.RGBA32,false);var black=new Color[16];for(int face=0;face<6;face++)cube.SetPixels(black,(CubemapFace)face);cube.Apply();
    sky=new Material(Shader.Find("Skybox/Cubemap"));sky.SetTexture("_Tex",cube);RenderSettings.skybox=sky;
    RenderSettings.ambientMode=AmbientMode.Skybox;RenderSettings.ambientProbe=new SphericalHarmonicsL2();

    // Exercise the real startup, including the camera/actor/options path, without running its UI.
    typeof(Prototype).GetMethod("Start",Flags).Invoke(game,null);
    foreach(var light in root.GetComponentsInChildren<Light>(true))light.enabled=false;
    foreach(var camera in root.GetComponentsInChildren<Camera>(true))camera.enabled=false;
    foreach(var listener in root.GetComponentsInChildren<AudioListener>(true))listener.enabled=false;
    var sphere=GameObject.CreatePrimitive(PrimitiveType.Sphere);sphere.name="18 percent diffuse startup witness";
    sphere.transform.SetParent(root.transform,false);sphere.layer=29;
    var renderer=sphere.GetComponent<Renderer>();mat=new Material(Shader.Find("Legacy Shaders/Diffuse"));mat.color=new Color(.18f,.18f,.18f).gamma;renderer.sharedMaterial=mat; // Shader Color properties are sRGB; the card reflects 18% in linear light.
    renderer.reflectionProbeUsage=ReflectionProbeUsage.Off;
    var cameraObject=new GameObject("Actual ambient regression camera");cameraObject.transform.SetParent(root.transform,false);
    var probe=cameraObject.AddComponent<Camera>();probe.enabled=false;probe.cullingMask=1<<29;probe.clearFlags=CameraClearFlags.SolidColor;probe.backgroundColor=Color.black;
    probe.renderingPath=RenderingPath.Forward;probe.orthographic=true;probe.orthographicSize=.7f;probe.allowHDR=true;
    probe.transform.position=new Vector3(0,0,-2);probe.transform.LookAt(Vector3.zero);
    target=new RenderTexture(64,64,24,RenderTextureFormat.ARGBFloat,RenderTextureReadWrite.Linear);target.Create();probe.targetTexture=target;
    readback=new Texture2D(64,64,TextureFormat.RGBAFloat,false,true);
    yield return null;yield return null;
    Color filled=ReadAmbient(probe,target,readback);
    Assert.Greater(filled.grayscale,.005f,"Real startup must provide diffuse fill with no direct lights");

    // Control reproduces the previous behavior: the same ambientLight color with a black skybox.
    RenderSettings.ambientMode=AmbientMode.Skybox;RenderSettings.ambientProbe=new SphericalHarmonicsL2();DynamicGI.UpdateEnvironment();
    yield return null;yield return null;
    Color skyOnly=ReadAmbient(probe,target,readback);
    Debug.Log("Startup diffuse="+filled+"; incoming-skybox control="+skyOnly);
    Assert.Less(skyOnly.grayscale,filled.grayscale*.2f,"The fixture must distinguish ignored ambientLight from actual fill");
   }finally{
    if(root)Object.DestroyImmediate(root);if(target){target.Release();Object.DestroyImmediate(target);}if(readback)Object.DestroyImmediate(readback);
    if(mat)Object.DestroyImmediate(mat);if(sky)Object.DestroyImmediate(sky);if(cube)Object.DestroyImmediate(cube);
    SceneManager.SetActiveScene(previous);SceneManager.UnloadSceneAsync(scene);
   }
  }
  static Color ReadAmbient(Camera camera,RenderTexture target,Texture2D texture){
   var old=RenderTexture.active;try{camera.Render();RenderTexture.active=target;texture.ReadPixels(new Rect(0,0,64,64),0,0);texture.Apply();return texture.GetPixel(32,32);}finally{RenderTexture.active=old;}
  }
 }
}

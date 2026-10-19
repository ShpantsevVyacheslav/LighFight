using System;using System.IO;using System.Collections;using System.Collections.Generic;using UnityEngine;using UnityEngine.Experimental.Rendering;
// Read-only observers: no OnRenderImage component and no render-state assignment.
public static class CurrentBufferCaptureControl {
 [Serializable]sealed class RT {public string label,name,graphicsFormat,legacyFormat;public bool exists,sRGB,hasAlpha;public int width,height,msaa,alphaComponents;}
 [Serializable]sealed class Sample {public string stage,point,shader;public int frame,renders;public double uptime;public string utc;public bool splashFinished;public bool rendererEnabled,isVisible,sRGBWrite,hdr,allowMSAA;public Color clear;public string clearFlags;public RT active,cameraActive,target;}
 [Serializable]sealed class Result {public string status="CONTROL ONLY; calibration/dispatch/physical alpha UNACCEPTED";public Sample[] observations;}
 static RT Describe(string label,RenderTexture texture){var r=new RT{label=label,exists=texture};if(!texture)return r;var d=texture.descriptor;r.name=texture.name;r.graphicsFormat=d.graphicsFormat.ToString();r.legacyFormat=texture.format.ToString();r.sRGB=d.sRGB;r.msaa=d.msaaSamples;r.width=d.width;r.height=d.height;r.hasAlpha=GraphicsFormatUtility.HasAlphaChannel(d.graphicsFormat);r.alphaComponents=(int)GraphicsFormatUtility.GetAlphaComponentCount(d.graphicsFormat);return r;}
 [Serializable]sealed class StartupSample {public string stage,utc;public double uptime;public int frame;public bool splashFinished;}
 public static IEnumerator AwaitStartup(string output){
  var samples=new List<StartupSample>();Action<string> sample=stage=>{samples.Add(new StartupSample{stage=stage,utc=DateTime.UtcNow.ToString("O"),uptime=Time.realtimeSinceStartupAsDouble,frame=Time.frameCount,splashFinished=UnityEngine.Rendering.SplashScreen.isFinished});File.WriteAllText(Path.Combine(output,"startup-fence.json"),JsonUtility.ToJson(new StartupResult{observations=samples.ToArray()},true));};
  double start=Time.realtimeSinceStartupAsDouble;sample("entry");
  while(!UnityEngine.Rendering.SplashScreen.isFinished){if(Time.realtimeSinceStartupAsDouble-start>30){sample("timeout");throw new TimeoutException("Splash did not finish within 30 real seconds");}yield return null;}
  sample("finished");double finished=Time.realtimeSinceStartupAsDouble;
  while(Time.realtimeSinceStartupAsDouble-finished<2){if(!UnityEngine.Rendering.SplashScreen.isFinished)throw new Exception("Splash completion regressed during settle");yield return new WaitForEndOfFrame();}
  sample("settled");
 }
 [Serializable]sealed class StartupResult {public StartupSample[] observations;}
 public static IEnumerator Run(SkinnedMeshRenderer skin,string output){
  var cameras=UnityEngine.Object.FindObjectsByType<Camera>(FindObjectsSortMode.None);if(cameras.Length!=1)throw new Exception("One camera control required");var camera=cameras[0];var counter=camera.GetComponent<CurrentSkinningRenderCounter>();var samples=new List<Sample>();string stage="start";
  Action<string> observe=point=>samples.Add(new Sample{stage=stage,point=point,uptime=Time.realtimeSinceStartupAsDouble,utc=DateTime.UtcNow.ToString("O"),splashFinished=UnityEngine.Rendering.SplashScreen.isFinished,frame=Time.frameCount,renders=counter.renders,rendererEnabled=skin.enabled,isVisible=skin.isVisible,sRGBWrite=GL.sRGBWrite,hdr=camera.allowHDR,allowMSAA=camera.allowMSAA,clear=camera.backgroundColor,clearFlags=camera.clearFlags.ToString(),active=Describe("RenderTexture.active",RenderTexture.active),cameraActive=Describe("Camera.activeTexture",camera.activeTexture),target=Describe("Camera.targetTexture",camera.targetTexture)});
  Camera.CameraCallback pre=c=>{if(c==camera)observe("onPreCull");},post=c=>{if(c==camera)observe("onPostRender");};
  Camera.onPreCull+=pre;Camera.onPostRender+=post;
  try{
   CurrentSkinningBufferReadback.WriteMatrixState(skin,Path.Combine(output,"control-before.matrices"));
   foreach(string current in new[]{"ON1","ON2","OFF1","OFF2","ON3"}){
    stage=current;skin.enabled=current.StartsWith("ON");int renders=counter.renders;yield return new WaitForEndOfFrame();if(counter.renders-renders!=1)throw new Exception("Control requires one automatic render per stage");
    observe("beforeScreenshot");CurrentBufferStateSnapshot.Write(skin,Path.Combine(output,"control-"+stage+"-state.json"));
    var image=ScreenCapture.CaptureScreenshotAsTexture();observe("afterScreenshot");var format=image.graphicsFormat;
    File.WriteAllText(Path.Combine(output,"control-"+stage+"-texture.txt"),"graphicsFormat="+format+" textureFormat="+image.format+" hasAlpha="+GraphicsFormatUtility.HasAlphaChannel(format)+" firstRGBA="+image.GetPixels32()[0]);
    File.WriteAllBytes(Path.Combine(output,"control-"+stage+".png"),image.EncodeToPNG());UnityEngine.Object.Destroy(image);observe("afterEncodeDestroyScheduled");
   }
   skin.enabled=true;CurrentSkinningBufferReadback.WriteMatrixState(skin,Path.Combine(output,"control-after.matrices"));
   File.WriteAllText(Path.Combine(output,"control-result.json"),JsonUtility.ToJson(new Result{observations=samples.ToArray()},true));
  }finally{Camera.onPreCull-=pre;Camera.onPostRender-=post;skin.enabled=true;}
 }
}

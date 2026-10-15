using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Build.Reporting;
using UnityEngine;
public static class GuiColorTransportProbeBuild {
 public static void Run(){const string dir="Assets/GuiColorProbe";Directory.CreateDirectory(dir);const string portrait=dir+"/actual-fighter-0.png";if(!File.Exists(portrait))throw new Exception("Exact source portrait0 must be copied first");
  var pixels=Enumerable.Repeat(new Color32(56,60,66,255),256).ToArray();var flat=new Texture2D(16,16,TextureFormat.RGBA32,false,true);flat.SetPixels32(pixels);flat.Apply();var bytes=flat.EncodeToPNG();UnityEngine.Object.DestroyImmediate(flat);
  foreach(string n in new[]{"flat-srgb","flat-linear"})File.WriteAllBytes(dir+"/"+n+".png",bytes);AssetDatabase.Refresh();
  var originalImporter=(TextureImporter)AssetImporter.GetAtPath(portrait);originalImporter.isReadable=true;originalImporter.SaveAndReimport();
  foreach(string p in new[]{dir+"/flat-srgb.png",dir+"/flat-linear.png"}){var i=(TextureImporter)AssetImporter.GetAtPath(p);i.textureType=TextureImporterType.Default;i.sRGBTexture=!p.Contains("flat-linear");i.isReadable=true;i.textureCompression=TextureImporterCompression.Uncompressed;i.mipmapEnabled=false;i.alphaIsTransparency=false;i.maxTextureSize=8192;i.SaveAndReimport();}
  PlayerSettings.colorSpace=ColorSpace.Linear;PlayerSettings.defaultScreenWidth=1200;PlayerSettings.defaultScreenHeight=700;PlayerSettings.fullScreenMode=FullScreenMode.Windowed;
  var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);var camera=new GameObject("HDR source camera").AddComponent<Camera>();camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Color.black;var fx=camera.gameObject.AddComponent<LighFight.Runtime.FittedFilmicCamera>();fx.filmicShader=AssetDatabase.LoadAssetAtPath<Shader>("Assets/LighFight/Resources/Shaders/FittedFilmic.shader");
  var probe=new GameObject("Exact IMGUI color transport probe").AddComponent<GuiColorTransportProbe>();probe.viewCamera=camera;probe.portrait=AssetDatabase.LoadAssetAtPath<Texture2D>(portrait);probe.importedSrgb=AssetDatabase.LoadAssetAtPath<Texture2D>(dir+"/flat-srgb.png");probe.importedLinear=AssetDatabase.LoadAssetAtPath<Texture2D>(dir+"/flat-linear.png");EditorSceneManager.SaveScene(scene,dir+"/Probe.unity");AssetDatabase.SaveAssets();
  var report=BuildPipeline.BuildPlayer(new[]{dir+"/Probe.unity"},"GuiColorProbeBuild/GuiColorProbe.app",BuildTarget.StandaloneOSX,BuildOptions.Development);if(report.summary.result!=BuildResult.Succeeded)throw new Exception("GUI probe build failed");Debug.Log("AGENT_RESULT GUI color transport probe built; source bytes unchanged");
 }
}

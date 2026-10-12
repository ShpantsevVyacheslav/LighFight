using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using LighFight.Runtime;
namespace LighFight.Editor {
 [InitializeOnLoad] public static class SelectionScreenCapture {
  static int frames,stage;static double captureTime;
  static SelectionScreenCapture(){EditorApplication.update+=Tick;}
  public static void Run(){frames=0;stage=0;ShaderUtil.allowAsyncCompilation=false;EditorSceneManager.OpenScene(HostFightReviewSetup.ScenePath);SessionState.SetBool("LighFightCapture",true);var type=typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView");var game=EditorWindow.GetWindow(type);game.position=new Rect(50,50,1440,810);game.Show();game.Focus();EditorApplication.isPlaying=true;}
  public static void RunNative(){Run();var prototype=Object.FindFirstObjectByType<Prototype>();var properties=new SerializedObject(prototype);properties.FindProperty("selectedFighter").intValue=3;properties.ApplyModifiedPropertiesWithoutUndo();}
  public static void RunButtonSettings(){SessionState.SetBool("LighFightCaptureBindings",true);RunNative();}
  static void Capture(string name){Directory.CreateDirectory("../art/previews/selection-unity");ScreenCapture.CaptureScreenshot(Path.GetFullPath("../art/previews/selection-unity/"+name+".png"));captureTime=EditorApplication.timeSinceStartup;}
  static void Tick(){
   if(!SessionState.GetBool("LighFightCapture",false)||!EditorApplication.isPlaying)return;
   if(SessionState.GetBool("LighFightCaptureBindings",false)){TickBindings();return;}
   if(stage==0&&++frames==120){Capture("setup");stage=1;return;}
   if(stage==0||EditorApplication.timeSinceStartup-captureTime<3)return;
   var game=Object.FindFirstObjectByType<Prototype>();var flags=BindingFlags.Instance|BindingFlags.NonPublic;
   if(stage==1){typeof(Prototype).GetField("rulesOpen",flags).SetValue(game,true);stage=2;captureTime=EditorApplication.timeSinceStartup;return;}
   if(stage==2){Capture("rules");stage=3;return;}
   if(stage==3){typeof(Prototype).GetField("rulesOpen",flags).SetValue(game,false);typeof(Prototype).GetField("arenaOpen",flags).SetValue(game,true);stage=4;captureTime=EditorApplication.timeSinceStartup;return;}
   if(stage==4){Capture("arenas");stage=5;return;}
   if(stage==5){game.BeginFight();stage=6;captureTime=EditorApplication.timeSinceStartup;return;}
   if(stage==6){Capture("battle-transition");stage=7;return;}
   SessionState.SetBool("LighFightCapture",false);EditorApplication.update-=Tick;EditorApplication.isPlaying=false;EditorApplication.Exit(0);
  }
  static void TickBindings(){
   var game=Object.FindFirstObjectByType<Prototype>();var flags=BindingFlags.Instance|BindingFlags.NonPublic;
   if(stage==0){if(++frames<120)return;typeof(Prototype).GetMethod("OpenOptions",flags).Invoke(game,null);stage=1;captureTime=EditorApplication.timeSinceStartup;return;}
   if(EditorApplication.timeSinceStartup-captureTime<3)return;
   if(stage==1){Capture("controls-options");stage=2;return;}
   if(stage==2){typeof(Prototype).GetField("bindingSlot",flags).SetValue(game,0);stage=3;captureTime=EditorApplication.timeSinceStartup;return;}
   if(stage==3){Capture("button-bindings");stage=4;return;}
   SessionState.SetBool("LighFightCaptureBindings",false);SessionState.SetBool("LighFightCapture",false);EditorApplication.update-=Tick;EditorApplication.isPlaying=false;EditorApplication.Exit(0);
  }
 }
}

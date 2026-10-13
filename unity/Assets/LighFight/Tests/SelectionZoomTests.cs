using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using LighFight.Runtime;
namespace LighFight.Tests {
 public sealed class SelectionZoomTests {
  [UnityTest]public IEnumerator RosterSelectionResetsCombatMenuAndResizeKeepsDisplayCamera(){
   yield return new EnterPlayMode();
   var root=new GameObject("Selection reentry regression");var game=root.AddComponent<Prototype>();
   game.content=AssetDatabase.LoadAssetAtPath<GameContent>("Assets/LighFight/Content/GameContent.asset");
   var flags=BindingFlags.Instance|BindingFlags.NonPublic;
   var cameraField=typeof(Prototype).GetField("fightCamera",flags);
   for(int i=0;i<100&&cameraField.GetValue(game)==null;i++)yield return null;
   var menu=typeof(Prototype).GetField("advancedMoves",flags);menu.SetValue(game,true);
   game.SelectRosterFighter(3);bool menuClosed=!(bool)menu.GetValue(game);
   var update=typeof(Prototype).GetMethod("UpdateSelection",flags);update.Invoke(game,null);
   var render=(RenderTexture)typeof(Prototype).GetField("selectionRender",flags).GetValue(game);
   render.Release();render.width=Mathf.Max(1,render.width-1);render.Create();
   update.Invoke(game,null);
   var display=(Camera)typeof(Prototype).GetField("selectionDisplayCamera",flags).GetValue(game);
   bool displayEnabled=display.enabled;
   var resized=(RenderTexture)typeof(Prototype).GetField("selectionRender",flags).GetValue(game);
   bool targetCreated=resized!=null&&resized.IsCreated();
   Object.Destroy(root);yield return null;yield return new ExitPlayMode();
   Assert.True(menuClosed,"A combat submenu must not disable gestures after choosing a fighter");
   Assert.True(displayEnabled,"Preview texture recreation must not disable the screen-clearing camera");
   Assert.True(targetCreated,"The negotiated preview format and MSAA combination must allocate on the test GPU");
  }
  [UnityTest]public IEnumerator UserZoomMovesSelectionCameraCloser(){
   yield return new EnterPlayMode();
   var root=new GameObject("Selection zoom integration");var game=root.AddComponent<Prototype>();
   game.content=AssetDatabase.LoadAssetAtPath<GameContent>("Assets/LighFight/Content/GameContent.asset");
   var flags=BindingFlags.Instance|BindingFlags.NonPublic;var cameraField=typeof(Prototype).GetField("fightCamera",flags);
   for(int i=0;i<100&&cameraField.GetValue(game)==null;i++)yield return null;
   Assert.NotNull(cameraField.GetValue(game));var camera=(Camera)cameraField.GetValue(game);
   var distance=typeof(Prototype).GetField("previewDistance",flags);var update=typeof(Prototype).GetMethod("UpdateSelection",flags);
   distance.SetValue(game,2.65f);update.Invoke(game,null);float normal=-camera.transform.position.z;
   distance.SetValue(game,1.7f);update.Invoke(game,null);float close=-camera.transform.position.z;
   Object.Destroy(root);yield return null;yield return new ExitPlayMode();
   Assert.Less(close,normal*.9f,"User zoom must not be cancelled by automatic full-body fitting");
  }
 }
}

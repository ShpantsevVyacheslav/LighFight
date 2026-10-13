using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using LighFight.Runtime;
namespace LighFight.Tests {
 public sealed class SelectionZoomTests {
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

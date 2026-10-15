using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using LighFight.Runtime;
namespace LighFight.Tests {
 public sealed class SelectionZoomTests {
  [Test]public void EveryLocalizedRosterTitleFitsWithoutSplittingOrClipping(){
   var fit=typeof(Prototype).GetMethod("FitSelectionTitleFontSize",BindingFlags.Static|BindingFlags.NonPublic);
   var font=Resources.Load<Font>("Fonts/NotoSans-Regular");Assert.NotNull(font);
   var style=new GUIStyle{font=font,fontSize=30,fontStyle=FontStyle.BoldAndItalic,wordWrap=true,alignment=TextAnchor.MiddleLeft};
   var catalog=AssetDatabase.LoadAssetAtPath<GameContent>("Assets/LighFight/Content/GameContent.asset");
   var root=new GameObject("Actual localized title layout");var game=root.AddComponent<Prototype>();game.content=catalog;
   try{foreach(bool russian in new[]{true,false}){
    typeof(Prototype).GetField("russian",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(game,russian);
    for(int i=0;i<game.RosterCount;i++){
     string title=game.FighterLabel(i).ToUpperInvariant();Assert.IsNotEmpty(title);var box=new Rect(30,96,212,92);
     int size=fit==null?30:(int)fit.Invoke(null,new object[]{style,title,box,30});
     Assert.AreEqual(30,style.fontSize,"Measurement must preserve the caller style");Assert.That(size,Is.InRange(1,30));
     style.fontSize=size;Assert.LessOrEqual(style.CalcHeight(new GUIContent(title),box.width),box.height,title);
     foreach(string word in title.Split(' '))Assert.LessOrEqual(style.CalcSize(new GUIContent(word)).x,box.width,"Do not split a name within a word: "+title);
     style.fontSize=30;
    }
   }}finally{Object.DestroyImmediate(root);}
  }
  [UnityTest]public IEnumerator RosterSelectionResetsCombatMenuAndResizeKeepsDisplayCamera(){
   yield return new EnterPlayMode();
   var root=new GameObject("Selection reentry regression");var game=root.AddComponent<Prototype>();
   game.content=AssetDatabase.LoadAssetAtPath<GameContent>("Assets/LighFight/Content/GameContent.asset");
   var flags=BindingFlags.Instance|BindingFlags.NonPublic;
   var cameraField=typeof(Prototype).GetField("fightCamera",flags);
   for(int i=0;i<100&&cameraField.GetValue(game)==null;i++)yield return null;
   yield return ActorCurveWarmupTests.WaitReady(game);
   var menu=typeof(Prototype).GetField("advancedMoves",flags);menu.SetValue(game,true);
   game.SelectRosterFighter(3);bool menuClosed=!(bool)menu.GetValue(game);
   yield return ActorCurveWarmupTests.WaitReady(game);
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
   yield return ActorCurveWarmupTests.WaitReady(game);
   Assert.NotNull(cameraField.GetValue(game));var camera=(Camera)cameraField.GetValue(game);
   game.SelectRosterFighter(3);yield return ActorCurveWarmupTests.WaitReady(game);
   var distance=typeof(Prototype).GetField("previewDistance",flags);var update=typeof(Prototype).GetMethod("UpdateSelection",flags);
   distance.SetValue(game,2.65f);update.Invoke(game,null);float normal=-camera.transform.position.z;
   distance.SetValue(game,1.7f);update.Invoke(game,null);float close=-camera.transform.position.z;
   Object.Destroy(root);yield return null;yield return new ExitPlayMode();
   Assert.Less(close,normal*.9f,"User zoom must not be cancelled by automatic full-body fitting");
  }
 }
}

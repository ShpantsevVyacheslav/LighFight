using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using LighFight.Runtime;
namespace LighFight.Tests {
 public sealed class HiddenRosterRenderingTests {
  const BindingFlags Flags=BindingFlags.Instance|BindingFlags.NonPublic;
  sealed class RenderCounter {public Camera target;public int count;public void Observe(Camera value){if(value==target)count++;}}
  static T Get<T>(Prototype game,string field)=>(T)typeof(Prototype).GetField(field,Flags).GetValue(game);
  [UnityTest]public IEnumerator RosterSkipsHiddenActorRenderAndResumesPreviewAndFight(){
   yield return new EnterPlayMode();
   var root=new GameObject("Roster hidden rendering regression");var game=root.AddComponent<Prototype>();
   for(int i=0;i<100&&Get<Camera>(game,"fightCamera")==null;i++)yield return null;
   yield return null;
   var camera=Get<Camera>(game,"fightCamera");Assert.NotNull(camera);
   var counter=new RenderCounter{target=camera};Camera.onPreCull+=counter.Observe;
   try {
    float clock=Get<float>(game,"previewClock");
    for(int i=0;i<3;i++)yield return new WaitForEndOfFrame();
    Assert.False(camera.enabled,"Invisible preview camera must stop submitting work");
    Assert.AreEqual(0,counter.count,"Opaque roster must not render the invisible fighter");
    Assert.False(Get<FighterView>(game,"player").gameObject.activeSelf,"Hidden actor animation/late hooks must also stop");
    Assert.AreEqual(clock,Get<float>(game,"previewClock"),"Hidden preview must not keep advancing");
    Assert.IsNull(Get<RenderTexture>(game,"selectionRender"),"Roster alone needs no HDR/MSAA preview target");
    Assert.True(Get<Camera>(game,"selectionDisplayCamera").enabled,"UI still needs a screen-clearing camera");
    game.SelectRosterFighter(0);yield return ActorCurveWarmupTests.WaitReady(game);
    for(int i=0;i<100&&!camera.enabled;i++)yield return null;
    yield return new WaitForEndOfFrame();
    Assert.True(camera.enabled);Assert.True(Get<FighterView>(game,"player").gameObject.activeSelf);
    Assert.NotNull(Get<RenderTexture>(game,"selectionRender"));Assert.Greater(counter.count,0,"Visible preview must resume rendering");
    typeof(Prototype).GetField("rosterOpen",Flags).SetValue(game,true);
    yield return null;yield return new WaitForEndOfFrame();counter.count=0;
    for(int i=0;i<3;i++)yield return new WaitForEndOfFrame();
    Assert.AreEqual(0,counter.count,"Returning to roster must stop the preview again");
    game.BeginFight();yield return null;yield return new WaitForEndOfFrame();
    Assert.True(camera.enabled,"Entering combat directly must restore the fight camera");
    Assert.True(Get<FighterView>(game,"player").gameObject.activeSelf,"Combat player must be visible");
    Assert.True(Get<FighterView>(game,"opponent").gameObject.activeSelf,"Combat opponent must be visible");
   }finally{Camera.onPreCull-=counter.Observe;Object.Destroy(root);}
   yield return null;yield return new ExitPlayMode();
  }
 }
}

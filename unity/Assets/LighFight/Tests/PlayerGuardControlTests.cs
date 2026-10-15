using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using LighFight.Runtime;
using LighFight.Core;
namespace LighFight.Tests {
 public sealed class PlayerGuardControlTests {
  const BindingFlags Flags=BindingFlags.Instance|BindingFlags.NonPublic;
  static T Get<T>(Prototype game,string name)=>(T)typeof(Prototype).GetField(name,Flags).GetValue(game);
  [Test]public void GuardButtonDoesNotOverlapJoystickAtAnySupportedScale(){
   var root=new GameObject("Guard layout");var game=root.AddComponent<Prototype>();
   try{var property=typeof(Prototype).GetProperty("GuardArea",Flags);Assert.NotNull(property,"Block needs a visible accessible control");
    foreach(float scale in new[]{.8f,1f,1.25f})foreach(bool mirrored in new[]{false,true}){
     typeof(Prototype).GetField("controlsScale",Flags).SetValue(game,scale);typeof(Prototype).GetField("leftHanded",Flags).SetValue(game,mirrored);
     var guard=(Rect)property.GetValue(game);var stick=(Rect)typeof(Prototype).GetProperty("StickArea",Flags).GetValue(game);
     Assert.GreaterOrEqual(guard.xMin,0);Assert.LessOrEqual(guard.xMax,960);Assert.GreaterOrEqual(guard.yMin,70);Assert.LessOrEqual(guard.yMax,540);
     Assert.Greater(Vector2.Distance(guard.center,stick.center),(guard.width+stick.width)/2+4);
    }
   }finally{Object.DestroyImmediate(root);}
  }
  [UnityTest]public IEnumerator GuardTogglesClearsForAttackAndResetsOnPause(){
   yield return new EnterPlayMode();var root=new GameObject("Actual player defence");var game=root.AddComponent<Prototype>();
   try{
    for(int i=0;i<100&&Get<Camera>(game,"fightCamera")==null;i++)yield return null;
    game.BeginFight();var session=Get<FightSession>(game,"session");var toggle=typeof(Prototype).GetMethod("TogglePlayerGuard",Flags);Assert.NotNull(toggle);
    toggle.Invoke(game,null);Assert.True(session.Match.A.Guard);toggle.Invoke(game,null);Assert.False(session.Match.A.Guard);
    toggle.Invoke(game,null);typeof(Prototype).GetField("pendingAttack",Flags).SetValue(game,0);
    for(int i=0;i<100&&!session.Match.A.Attack.HasValue;i++)yield return null;
    Assert.True(session.Match.A.Attack.HasValue,"Queued strike should remain available while guarding");Assert.False(session.Match.A.Guard,"Next accepted attack must remove block");
    session.Match.A.Attack=null;toggle.Invoke(game,null);Assert.True(session.Match.A.Guard);
    typeof(Prototype).GetField("stickCrouch",Flags).SetValue(game,true);typeof(Prototype).GetMethod("OpenOptions",Flags).Invoke(game,null);Assert.False(session.Match.A.Guard,"Pause must discard held/toggled combat input");Assert.False(Get<bool>(game,"stickCrouch"),"Pause must discard joystick crouch intent");
   }finally{Object.Destroy(root);}
   yield return null;yield return new ExitPlayMode();
  }
 }
}

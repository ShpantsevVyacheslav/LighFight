using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using LighFight.Core;
using LighFight.Runtime;
namespace LighFight.Tests {
 public sealed class PhysicalContactSamplingTests {
  FighterView a,b;Match match;PhysicalContacts contacts;
  [SetUp]public void Setup(){a=FighterView.Create("samplerA",Color.blue,false);b=FighterView.Create("samplerB",Color.red,false);match=new Match(true);contacts=new PhysicalContacts(match,a,b);}
  [TearDown]public void Teardown(){Object.DestroyImmediate(a.gameObject);Object.DestroyImmediate(b.gameObject);}
  static int Count(PhysicalContacts value){var p=typeof(PhysicalContacts).GetProperty("SampledPosePairs");Assert.NotNull(p,"Contact sampling must expose pair count to prove idle/recovery does not resample both skeletons");return (int)p.GetValue(value);}
  [Test]public void IdleAndRecoveryDoNotEvaluateContactPoses(){for(int i=0;i<20;i++)match.Step();Assert.AreEqual(0,Count(contacts));match.A.DownTicks=40;for(int i=0;i<20;i++)match.Step();Assert.AreEqual(0,Count(contacts));}
  [Test]public void SamplingStartsWithApproachAndStopsAtActiveEnd(){match.A.X=-2;match.B.X=2;Assert.True(match.Attack(match.A,0));var move=Moves.All[0];for(int i=0;i<move.Startup+move.Active;i++)match.Step();Assert.AreEqual(move.Startup+move.Active,Count(contacts),"An approaching source hand can contact before the nominal active frame");for(int i=0;i<10;i++)match.Step();Assert.AreEqual(move.Startup+move.Active,Count(contacts));}

  [TestCase(1)][TestCase(-1)]public void CloseContactStopsSourceAndNextAttackIsNotFrozen(int side){match.A.X=-side*.325f;match.B.X=side*.325f;match.A.FacingX=side;match.B.FacingX=-side;Assert.True(match.Attack(match.A,0));bool stopped=false;float previous=float.PositiveInfinity;for(int tick=0;tick<45;tick++){match.Step();a.PoseTowards(match.A,match.B,1f/60);b.PoseTowards(match.B,match.A,1f/60);if(a.StrikeStopped(match.A)){stopped=true;float source=a.ContactAttackFrame(match.A);Assert.LessOrEqual(source,previous+.0001f);previous=source;}}Assert.True(stopped,"Near source proxy contact must stop rather than pass through");Assert.Less(match.B.Health,150,"Close attacks must remain functional");match.B.Stun=0;Assert.True(match.Attack(match.A,0));Assert.False(a.StrikeStopped(match.A),"A repeated source move must receive a new response epoch");}
  sealed class Hook:IContactPoseHook {public bool valid;public void ApplyContactPose(){valid=true;}public void InvalidateContactPose(){valid=false;}public bool TryGetContactPhase(int state,out float phase){phase=.2f;return valid;}}
  [Test]public void LeavingActiveWindowClearsSameFrameContactCache(){var h=new Hook();typeof(FighterView).GetField("contactPoseHook",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(a,h);typeof(FighterView).GetField("contactPoseHooksChecked",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(a,true);match.A.X=-2;match.B.X=2;Assert.True(match.Attack(match.A,0));match.A.Frame=Moves.All[0].Startup+Moves.All[0].Active-1;match.Step();Assert.True(h.valid);match.Step();Assert.False(h.valid,"A skipped recovery tick must not replay an earlier active pose from this render frame");}
  [Test]public void AllMoveOutcomesMatchAlwaysSampledReference(){
   var ra=FighterView.Create("referenceA",Color.blue,false);var rb=FighterView.Create("referenceB",Color.red,false);
   try{for(int move=0;move<Moves.All.Length;move++)foreach(float gap in new[]{.65f,3f}){
    var optimized=new Match(true);var reference=new Match(true);optimized.A.X=reference.A.X=-gap/2;optimized.B.X=reference.B.X=gap/2;
    var oc=new PhysicalContacts(optimized,a,b);var rc=new PhysicalContacts(reference,ra,rb);var sample=typeof(PhysicalContacts).GetMethod("SampleBoth",BindingFlags.Instance|BindingFlags.NonPublic);
    // Before the optimization Sample itself is the always-sampled reference.
    if(sample==null)sample=typeof(PhysicalContacts).GetMethod("Sample",BindingFlags.Instance|BindingFlags.NonPublic);
    reference.BeforeContact=m=>sample.Invoke(rc,new object[]{m});
    Assert.True(optimized.Attack(optimized.A,move));Assert.True(reference.Attack(reference.A,move));
    for(int tick=0;tick<90;tick++){optimized.Step();reference.Step();Assert.AreEqual(reference.B.Health,optimized.B.Health,"damage "+Moves.All[move].Id+" gap "+gap+" tick "+tick);Assert.AreEqual(reference.B.HitSerial,optimized.B.HitSerial);Assert.AreEqual(reference.B.DownTicks,optimized.B.DownTicks);}
   }}finally{Object.DestroyImmediate(ra.gameObject);Object.DestroyImmediate(rb.gameObject);}
  }
 }
}

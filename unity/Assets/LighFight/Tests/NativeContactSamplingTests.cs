using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using LighFight.Core;
using LighFight.Runtime;
namespace LighFight.Tests {
 public sealed class NativeContactSamplingTests {
  [UnityTest]public IEnumerator CloseNativeStrikeHasGeometricStopAndBoundedRecovery(){
   yield return new EnterPlayMode();var roots=new List<GameObject>();var leases=new List<IActorCurveWarmup>();
   try{var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/LighFight/Art/Drafts/TripoIslandPlayable/IslandHumanoidCandidate.prefab");Assert.NotNull(prefab);var views=new FighterView[2];for(int i=0;i<2;i++){var root=Object.Instantiate(prefab);roots.Add(root);views[i]=root.GetComponent<FighterView>();foreach(var warm in root.GetComponentsInChildren<MonoBehaviour>(true).OfType<IActorCurveWarmup>()){leases.Add(warm);yield return warm.Warmup();Assert.True(warm.IsWarm);}}
    foreach(int side in new[]{1,-1})foreach(int move in new[]{0,4})foreach(float gap in new[]{.65f,.9f,3f}){
     var match=new Match(true);match.A.X=-side*gap*.5f;match.B.X=side*gap*.5f;match.A.FacingX=side;match.B.FacingX=-side;match.B.Crouch=move==4;match.B.Guard=move==0;var contacts=new PhysicalContacts(match,views[0],views[1]);Assert.True(match.Attack(match.A,move));bool stopped=false;float previous=float.PositiveInfinity;
     for(int tick=0;tick<Moves.All[move].Duration+2;tick++){match.Step();views[0].PoseTowards(match.A,match.B,1f/60);views[1].PoseTowards(match.B,match.A,1f/60);if(views[0].StrikeStopped(match.A)){stopped=true;float phase=views[0].ContactAttackFrame(match.A);Assert.LessOrEqual(phase,previous+.0001f);previous=phase;var mapping=views[0].CopyContactPoses().First(p=>p.moveId==Moves.All[move].Id);float end=mapping.end>mapping.start?mapping.end:1;float expected=mapping.start+(end-mapping.start)*MotionTiming.Phase(Moves.All[move],phase,(mapping.phase-mapping.start)/(end-mapping.start));Assert.AreEqual(expected,views[0].GetComponentInChildren<Animator>().GetCurrentAnimatorStateInfo(0).normalizedTime,.0002f,"Rendered source phase must use stopped/retracting pose, not full strike continuation");}Assert.GreaterOrEqual(Match.Distance(match.A,match.B),Match.MinimumSeparation-.0001f);yield return null;}
     yield return new WaitForEndOfFrame();if(gap==3){Assert.AreEqual(150,match.B.Health);Assert.AreEqual(0,match.B.HitSerial);}else{Assert.True(stopped,"Native "+Moves.All[move].Id+" gap="+gap+" side="+side+" must not pass through");Assert.Greater(contacts.ConfirmedPoseContacts,0);Assert.AreEqual(1,match.B.HitSerial,"Close attacks must remain functional, including a real guarded contact");if(move==4)Assert.Less(match.B.Health,150);}
     Assert.False(match.A.Attack.HasValue);Assert.True(match.Attack(match.A,move));Assert.False(views[0].StrikeStopped(match.A),"Recovery cannot freeze the next native attack");yield return null;
    }
   }finally{foreach(var warm in leases)warm.Dispose();foreach(var root in roots)Object.Destroy(root);}yield return null;yield return new ExitPlayMode();
  }
  [UnityTest]public IEnumerator ArtistKickContactsMatchAlwaysSampledReference(){
   yield return new EnterPlayMode();
   var roots=new List<GameObject>();var leases=new List<IActorCurveWarmup>();
   try{
    var paths=new[]{"Assets/LighFight/Art/Drafts/TripoRetroPlayable/RetroHumanoidCandidate.prefab","Assets/LighFight/Art/Drafts/TripoIslandPlayable/IslandHumanoidCandidate.prefab"};
    var views=new FighterView[4];for(int i=0;i<4;i++){var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(paths[i%2]);Assert.NotNull(prefab);var root=Object.Instantiate(prefab);roots.Add(root);views[i]=root.GetComponent<FighterView>();foreach(var warm in root.GetComponentsInChildren<MonoBehaviour>(true).OfType<IActorCurveWarmup>()){leases.Add(warm);yield return warm.Warmup();Assert.True(warm.IsWarm);}}
    var always=typeof(PhysicalContacts).GetMethod("SampleBoth",BindingFlags.Instance|BindingFlags.NonPublic);Assert.NotNull(always);
    foreach(int move in new[]{4,11})foreach(float gap in new[]{.65f,3f}){
     var actual=new Match(true);var reference=new Match(true);actual.A.X=reference.A.X=-gap/2;actual.B.X=reference.B.X=gap/2;var contacts=new PhysicalContacts(actual,views[0],views[1]);var refContacts=new PhysicalContacts(reference,views[2],views[3]);reference.BeforeContact=m=>always.Invoke(refContacts,new object[]{m});Assert.True(actual.Attack(actual.A,move));Assert.True(reference.Attack(reference.A,move));
     for(int tick=0;tick<105;tick++){
      actual.Step();reference.Step();views[0].PoseTowards(actual.A,actual.B,1f/60);views[1].PoseTowards(actual.B,actual.A,1f/60);views[2].PoseTowards(reference.A,reference.B,1f/60);views[3].PoseTowards(reference.B,reference.A,1f/60);
      yield return new WaitForEndOfFrame();Assert.AreEqual(reference.B.Health,actual.B.Health,"Native damage "+Moves.All[move].Id+" gap "+gap+" tick "+tick);Assert.AreEqual(reference.B.HitSerial,actual.B.HitSerial);Assert.AreEqual(reference.B.DownTicks,actual.B.DownTicks);yield return null;
     }
     Assert.Less(contacts.SampledPosePairs,refContacts.SampledPosePairs);if(gap==3)Assert.AreEqual(150,actual.B.Health);
    }
   }finally{foreach(var warm in leases)warm.Dispose();foreach(var root in roots)Object.Destroy(root);}
   yield return null;yield return new ExitPlayMode();
  }
 }
}

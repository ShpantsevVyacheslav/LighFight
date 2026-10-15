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

using System;
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using LighFight.Runtime;
namespace LighFight.Tests {
 public sealed class ActorCurveWarmupTests {
  public static IEnumerator WaitReady(Prototype game){float deadline=Time.realtimeSinceStartup+30;do{yield return null;}while(game&&game.ActorsLoading&&Time.realtimeSinceStartup<deadline);Assert.NotNull(game);Assert.False(game.ActorsLoading,"Selected actors must finish native curve loading within 30 seconds");}
  [UnityTest] public IEnumerator LoadingBlocksPreviewAndQueuesCombatAfterRapidSelection(){
   yield return new EnterPlayMode();
   var sample=Actor();var baseline=Counts(sample);UnityEngine.Object.Destroy(sample);yield return null;
   var root=new GameObject("Native curve loading integration");var game=root.AddComponent<Prototype>();game.content=AssetDatabase.LoadAssetAtPath<GameContent>("Assets/LighFight/Content/GameContent.asset");
   yield return null;yield return WaitReady(game);
   game.SelectRosterFighter(3);Assert.True(game.ActorsLoading);
   var flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;
   Assert.False(((FighterView)typeof(Prototype).GetField("player",flags).GetValue(game)).gameObject.activeSelf,"Unwarmed actor must stay hidden under loading UI");
   game.SelectRosterFighter(4);game.SelectRosterFighter(3);game.BeginFight();
   Assert.True((bool)typeof(Prototype).GetField("selecting",flags).GetValue(game),"BeginFight must wait for both selected actors");
   yield return WaitReady(game);
   Assert.False((bool)typeof(Prototype).GetField("selecting",flags).GetValue(game));
   foreach(var component in game.GetComponentsInChildren<MonoBehaviour>(true))if(component is IActorCurveWarmup warmup)Assert.True(warmup.IsWarm);
   Assert.True(((FighterView)typeof(Prototype).GetField("opponent",flags).GetValue(game)).gameObject.activeSelf);
   UnityEngine.Object.Destroy(root);yield return null;
   foreach(var pair in baseline)Assert.AreEqual(pair.Value,Users(pair.Key),"Prototype destruction must explicitly release selected actor leases");
   yield return new ExitPlayMode();
  }
  static GameObject Actor(){var content=AssetDatabase.LoadAssetAtPath<GameContent>("Assets/LighFight/Content/GameContent.asset");var actor=UnityEngine.Object.Instantiate(content.fighters[3]);actor.SetActive(false);return actor;}
  static IActorCurveWarmup Warmup(GameObject actor){foreach(var component in actor.GetComponentsInChildren<MonoBehaviour>(true))if(component is IActorCurveWarmup warmup)return warmup;Assert.Fail("Approved native fighter needs typed curve warmup");return null;}
  static Dictionary<UnityEngine.Object,int> Counts(GameObject actor){var result=new Dictionary<UnityEngine.Object,int>();foreach(var component in actor.GetComponentsInChildren<MonoBehaviour>(true)){if(!(component is IActorCurveWarmup))continue;var entries=(Array)component.GetType().GetField("entries").GetValue(component);foreach(var entry in entries){var pose=(UnityEngine.Object)entry.GetType().GetField("pose").GetValue(entry);if(!result.ContainsKey(pose))result[pose]=Users(pose);}}return result;}
  static int Users(UnityEngine.Object pose)=>(int)pose.GetType().GetProperty("ActiveUsers").GetValue(pose);
  [UnityTest] public IEnumerator RuntimeDestructionReleasesLeasesWithoutExplicitOwnerDispose(){
   yield return new EnterPlayMode();
   var actor=Actor();var warmup=Warmup(actor);var before=Counts(actor);
   yield return warmup.Warmup();Assert.True(warmup.IsWarm);
   foreach(var pair in before)Assert.AreEqual(pair.Value+1,Users(pair.Key));
   UnityEngine.Object.Destroy(actor);yield return null;
   foreach(var pair in before)Assert.AreEqual(pair.Value,Users(pair.Key),"Runtime destruction must also release dormant actor leases");
   yield return new ExitPlayMode();
  }
  [Test] public void ResidentCurveLeasesSurviveDisableAndReleaseAtActorDestruction(){
   var actor=Actor();var warmup=Warmup(actor);var before=Counts(actor);
   try{var routine=warmup.Warmup();while(routine.MoveNext()){}(routine as IDisposable)?.Dispose();Assert.True(warmup.IsWarm);foreach(var pair in before)Assert.AreEqual(pair.Value+1,Users(pair.Key),"Exactly one resident lease per distinct payload");actor.SetActive(true);actor.SetActive(false);foreach(var pair in before)Assert.AreEqual(pair.Value+1,Users(pair.Key),"Hidden opponent must retain its decoded curves");}
   finally{warmup.Dispose();warmup.Dispose();UnityEngine.Object.DestroyImmediate(actor);}
   foreach(var pair in before)Assert.AreEqual(pair.Value,Users(pair.Key),"Actor destruction releases resident leases");
  }
  [Test] public void PartialWarmupCancellationCannotReleaseTheNextWarmupLeases(){
   var actor=Actor();var warmup=Warmup(actor);var before=Counts(actor);
   try{
    var stale=warmup.Warmup();Assert.True(stale.MoveNext());Assert.True(stale.MoveNext());bool acquired=false;foreach(var pair in before)acquired|=Users(pair.Key)>pair.Value;Assert.True(acquired,"Cancellation must exercise an acquired partial lease");
    warmup.CancelWarmup();foreach(var pair in before)Assert.AreEqual(pair.Value,Users(pair.Key));
    var current=warmup.Warmup();Assert.True(current.MoveNext());Assert.True(current.MoveNext());Assert.False(stale.MoveNext());(stale as IDisposable)?.Dispose();
    while(current.MoveNext()){}(current as IDisposable)?.Dispose();Assert.True(warmup.IsWarm);foreach(var pair in before)Assert.AreEqual(pair.Value+1,Users(pair.Key));
   }finally{warmup.Dispose();warmup.Dispose();UnityEngine.Object.DestroyImmediate(actor);}
   foreach(var pair in before)Assert.AreEqual(pair.Value,Users(pair.Key));
  }
 }
}

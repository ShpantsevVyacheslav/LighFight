using System;using System.Collections;using System.Collections.Generic;using System.Linq;using NUnit.Framework;using UnityEngine;using UnityEngine.TestTools;using LighFight.Runtime;
public sealed class SelectedWarmupProbe:MonoBehaviour,IActorCurveWarmup {
 public static int disposed,canceled,warmed;bool ready,released;
 public bool fail;public bool IsWarm=>ready;public IEnumerator Warmup(){yield return null;yield return null;if(fail)throw new InvalidOperationException("fixture warmup failure");ready=true;warmed++;}
 public void CancelWarmup(){canceled++;}public void Dispose(){if(released)return;released=true;disposed++;}
}
public class SelectedContentLifecycleTests {
 sealed class Op:ISelectedAssetLoad{public bool done;public GameObject prefab;public string error;public int releases;public bool IsDone=>done;public GameObject Asset=>prefab;public string Error=>error;public void Dispose(){releases++;}}
 sealed class Provider:ISelectedAssetProvider{public List<Op> ops=new List<Op>();public List<string> keys=new List<string>();public GameObject fighter,arena;public ISelectedAssetLoad Load(string key){keys.Add(key);var op=new Op{prefab=key.StartsWith("a")?arena:fighter};ops.Add(op);return op;}}
 GameObject root,fighter,arena;GameContent catalog;Provider provider;Prototype game;
 IEnumerator Frames(int count){for(int i=0;i<count;i++)yield return null;}
 void Create(){SelectedWarmupProbe.disposed=0;SelectedWarmupProbe.canceled=0;SelectedWarmupProbe.warmed=0;fighter=FighterView.Create("stream fixture",Color.red,false).gameObject;fighter.AddComponent<SelectedWarmupProbe>();fighter.SetActive(false);arena=OfficeArena.Create(OfficeLocation.Yard).gameObject;arena.SetActive(false);catalog=ScriptableObject.CreateInstance<GameContent>();catalog.streamed=true;catalog.fighterAddresses=Enumerable.Range(0,11).Select(i=>"f"+i).ToArray();catalog.arenaAddresses=new[]{"a0"};provider=new Provider{fighter=fighter,arena=arena};root=new GameObject("Selected streaming lifecycle");game=root.AddComponent<Prototype>();game.content=catalog;game.SelectedAssetProvider=provider;}
 [UnityTearDown]public IEnumerator Cleanup(){if(root)UnityEngine.Object.Destroy(root);yield return Frames(3);if(fighter)UnityEngine.Object.Destroy(fighter);if(arena)UnityEngine.Object.Destroy(arena);if(catalog)UnityEngine.Object.Destroy(catalog);yield return null;}
 [UnityTest]public IEnumerator CancellationLatestSelectionWarmupAndDestroyRelease(){
  Create();yield return Frames(5);Assert.AreEqual(3,provider.ops.Count);var stale=provider.ops.ToArray();game.SelectRosterFighter(5);yield return Frames(5);Assert.True(stale.All(o=>o.releases==1));Assert.AreEqual(6,provider.ops.Count);foreach(var op in stale)op.done=true;Assert.AreEqual(0,root.GetComponentsInChildren<FighterView>(true).Length);
  foreach(var op in provider.ops.Skip(3))op.done=true;yield return Frames(12);Assert.False(game.ActorsLoading);Assert.AreEqual(2,root.GetComponentsInChildren<FighterView>(true).Length);Assert.AreEqual(2,SelectedWarmupProbe.warmed);CollectionAssert.AreEquivalent(new[]{"f5","f8","a0"},provider.keys.Skip(3));game.BeginFight();yield return null;Assert.True(provider.ops.Skip(3).All(o=>o.releases==0));UnityEngine.Object.Destroy(root);yield return Frames(3);Assert.True(provider.ops.All(o=>o.releases==1));Assert.AreEqual(2,SelectedWarmupProbe.disposed);
 }
 [UnityTest]public IEnumerator FailureReleasesAllAndRetryClearsLoadingError(){
  Create();yield return Frames(5);provider.ops[0].error="fixture failure";LogAssert.Expect(LogType.Warning,"Selected content: fixture failure");yield return Frames(4);Assert.True(game.ActorsLoading);Assert.True(provider.ops.All(o=>o.releases==1));game.SelectRosterFighter(0);yield return Frames(5);foreach(var op in provider.ops.Skip(3))op.done=true;yield return Frames(12);Assert.False(game.ActorsLoading);Assert.AreEqual(2,root.GetComponentsInChildren<FighterView>(true).Length);
 }
 [UnityTest]public IEnumerator DisablePendingRequestReleasesAndResumeLoadsFresh(){
  Create();yield return Frames(5);root.SetActive(false);yield return Frames(3);Assert.True(provider.ops.All(o=>o.releases==1));root.SetActive(true);yield return Frames(5);Assert.AreEqual(6,provider.ops.Count);foreach(var op in provider.ops.Skip(3))op.done=true;yield return Frames(12);Assert.False(game.ActorsLoading);
 }
 [UnityTest]public IEnumerator RebuildReleasesWarmupAndInstancesBeforeOldAssets(){
  Create();yield return Frames(5);foreach(var op in provider.ops)op.done=true;yield return Frames(12);Assert.AreEqual(2,SelectedWarmupProbe.warmed);var oldActors=root.GetComponentsInChildren<FighterView>(true);game.SelectRosterFighter(7);Assert.AreEqual(2,SelectedWarmupProbe.disposed);Assert.True(provider.ops.Take(3).All(o=>o.releases==0));yield return Frames(5);Assert.True(oldActors.All(v=>!v));Assert.True(provider.ops.Take(3).All(o=>o.releases==1));Assert.AreEqual(6,provider.ops.Count);
 }
 [UnityTest]public IEnumerator WarmupFailureReleasesAssetsAndRetryGetsFreshLeases(){
  Create();fighter.GetComponent<SelectedWarmupProbe>().fail=true;yield return Frames(5);LogAssert.Expect(LogType.Exception,new System.Text.RegularExpressions.Regex("InvalidOperationException: fixture warmup failure"));foreach(var op in provider.ops)op.done=true;yield return Frames(12);Assert.True(game.ActorsLoading);Assert.AreEqual(0,root.GetComponentsInChildren<FighterView>(true).Length);Assert.AreEqual(2,SelectedWarmupProbe.disposed);Assert.True(provider.ops.All(o=>o.releases==1));fighter.GetComponent<SelectedWarmupProbe>().fail=false;game.SelectRosterFighter(0);yield return Frames(5);foreach(var op in provider.ops.Skip(3))op.done=true;yield return Frames(12);Assert.False(game.ActorsLoading);
 }

 [UnityTest]public IEnumerator LegacyInlineCatalogRemainsImmediateAndUsesTypedWarmup(){
  Create();catalog.streamed=false;catalog.fighters=Enumerable.Repeat(fighter,8).ToArray();catalog.arenas=new[]{arena};yield return Frames(12);Assert.AreEqual(0,provider.ops.Count);Assert.False(game.ActorsLoading);Assert.AreEqual(2,SelectedWarmupProbe.warmed);Assert.AreEqual(2,root.GetComponentsInChildren<FighterView>(true).Length);game.SelectRosterFighter(5);yield return Frames(12);Assert.AreEqual(0,provider.ops.Count);Assert.False(game.ActorsLoading);game.BeginFight();yield return null;
 }

}

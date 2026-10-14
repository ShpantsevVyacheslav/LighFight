using System;using System.Collections;using System.Reflection;using NUnit.Framework;using UnityEngine;using UnityEngine.AddressableAssets;using UnityEngine.ResourceManagement.AsyncOperations;using UnityEngine.TestTools;using LighFight.Runtime;
public class SelectedDeliveryTeardownTests {
 sealed class Op:ISelectedAssetLoad {public int releases;public bool IsDone=>false;public GameObject Asset=>null;public string Error=>null;public void Dispose(){releases++;}}
 sealed class Provider:ISelectedAssetProvider {public System.Collections.Generic.List<Op> ops=new System.Collections.Generic.List<Op>();public ISelectedAssetLoad Load(string key){var op=new Op();ops.Add(op);return op;}}
 static void Set(object target,string name,object value){target.GetType().GetField(name,BindingFlags.Instance|BindingFlags.NonPublic).SetValue(target,value);}
 [UnityTest]public IEnumerator DownloaderDestroyReleasesPendingOperation(){var root=new GameObject("pending downloader");var downloader=root.AddComponent<ContentDownloader>();downloader.enabled=false;var operation=Addressables.ResourceManager.CreateCompletedOperation(123L,null);Set(downloader,"pending",(AsyncOperationHandle)operation);UnityEngine.Object.Destroy(root);yield return null;Assert.False(operation.IsValid());}
 [UnityTest]public IEnumerator BootstrapDestroyOwnsGameAndReleasesCatalogAndSelectedAssets(){
  var catalog=ScriptableObject.CreateInstance<GameContent>();catalog.streamed=true;catalog.fighterAddresses=new[]{"f0","f1"};catalog.arenaAddresses=new[]{"a0"};var provider=new Provider();var lease=new SelectedContentLease(catalog,0,1,0,provider);
  var gameRoot=new GameObject("detached owned game");var game=gameRoot.AddComponent<Prototype>();game.enabled=false;game.content=catalog;Set(game,"selectedContentLease",lease);
  var bootRoot=new GameObject("bootstrap ownership");var bootstrap=bootRoot.AddComponent<ContentBootstrap>();bootstrap.enabled=false;var catalogOperation=Addressables.ResourceManager.CreateCompletedOperation(catalog,null);var locatorOperation=Addressables.ResourceManager.CreateCompletedOperation(new object(),null);Set(bootstrap,"loadedContent",catalogOperation);Set(bootstrap,"pendingCatalog",(AsyncOperationHandle)locatorOperation);Set(bootstrap,"loadedGame",game);
  UnityEngine.Object.Destroy(bootRoot);for(int i=0;i<4;i++)yield return null;Assert.False(gameRoot);Assert.False(catalogOperation.IsValid());Assert.False(locatorOperation.IsValid());foreach(var op in provider.ops)Assert.AreEqual(1,op.releases);UnityEngine.Object.Destroy(catalog);
 }
}

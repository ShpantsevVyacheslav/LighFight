using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
namespace LighFight.Runtime {
 public interface ISelectedAssetLoad:IDisposable {
  bool IsDone {get;} GameObject Asset {get;} string Error {get;}
 }
 public interface ISelectedAssetProvider {ISelectedAssetLoad Load(string address);}
 public sealed class AddressableSelectedAssetProvider:ISelectedAssetProvider {
  sealed class LoadHandle:ISelectedAssetLoad {
   AsyncOperationHandle<GameObject> handle;bool released;
   public LoadHandle(string key){handle=Addressables.LoadAssetAsync<GameObject>(key);}
   public bool IsDone=>!released&&handle.IsDone;
   public GameObject Asset=>!released&&handle.Status==AsyncOperationStatus.Succeeded?handle.Result:null;
   public string Error=>released?"released":handle.IsDone&&handle.Status!=AsyncOperationStatus.Succeeded?(handle.OperationException?.Message??"asset_load_failed"):null;
   public void Dispose(){if(released)return;released=true;if(handle.IsValid())Addressables.Release(handle);}
  }
  public ISelectedAssetLoad Load(string address)=>new LoadHandle(address);
 }
 // A request owns exactly the selected unique addresses. No scene or prefab references in its catalog.
 public sealed class SelectedContentLease:IDisposable {
  readonly Dictionary<string,ISelectedAssetLoad> loads=new Dictionary<string,ISelectedAssetLoad>();
  readonly string playerKey,opponentKey,arenaKey;bool disposed;
  public SelectedContentLease(GameContent content,int player,int opponent,int arena,ISelectedAssetProvider provider){
   if(content==null||!content.streamed||!content.ValidateCatalog(out var error))throw new ArgumentException("Invalid stream catalog");
   if(player<0||player>=content.FighterCount||opponent<0||opponent>=content.FighterCount||arena<0||arena>=content.ArenaCount)throw new ArgumentOutOfRangeException("selection");
   playerKey=content.fighterAddresses[player];opponentKey=content.fighterAddresses[opponent];arenaKey=content.arenaAddresses[arena];
   try{foreach(string key in new[]{playerKey,opponentKey,arenaKey})if(!loads.ContainsKey(key))loads.Add(key,provider.Load(key));}
   catch{Dispose();throw;}
  }
  public int UniqueAssetCount=>loads.Count;
  public bool IsDone {get{if(disposed)return false;foreach(var load in loads.Values)if(!load.IsDone)return false;return true;}}
  public string Error {get{if(disposed)return "released";foreach(var load in loads.Values)if(load.Error!=null)return load.Error;return null;}}
  public GameObject Player=>Asset(playerKey);public GameObject Opponent=>Asset(opponentKey);public GameObject Arena=>Asset(arenaKey);
  GameObject Asset(string key)=>disposed?null:loads[key].Asset;
  public bool ValidateSelected(out string error){
   error=Error;if(error!=null)return false;if(!IsDone){error="not_loaded";return false;}
   foreach(var fighter in new[]{Player,Opponent})if(!fighter||!fighter.GetComponent<FighterView>()||!fighter.GetComponent<FighterView>().IsReady){error="invalid_fighter";return false;}
   if(!Arena||!Arena.GetComponent<OfficeArena>()){error="invalid_arena";return false;}return true;
  }
  public void Dispose(){if(disposed)return;disposed=true;foreach(var load in loads.Values)load.Dispose();loads.Clear();}
 }
}

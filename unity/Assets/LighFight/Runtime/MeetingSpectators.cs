using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace LighFight.Runtime {
 /// <summary>Loads only a bounded pair of pre-baked seated background meshes; never loads the roster.</summary>
 public sealed class MeetingSpectators:MonoBehaviour {
  [Serializable] public struct Candidate {public int fighterIndex;public string fighterId,address,smokingAddress;}
  [Serializable] public struct Seat {public Vector3 floorOrigin;public float yaw,seatHeight;}
  public Candidate[] candidates=new Candidate[0];
  public Seat[] seats=new[]{new Seat{floorOrigin=new Vector3(1.2234f,0,5.75243f),yaw=-90,seatHeight=.6122f},new Seat{floorOrigin=new Vector3(-1.1844f,0,6.05603f),yaw=90,seatHeight=.6127f}};
  [Range(0,2)] public int maximumSpectators=2;
  public MeetingHookahInteraction hookah;
  int first=-1,second=-1;bool configured;Coroutine pending;ISelectedAssetProvider provider;
  readonly List<GameObject> instances=new List<GameObject>();readonly List<ISelectedAssetLoad> loads=new List<ISelectedAssetLoad>();
  sealed class ReleaseBatch:IDisposable {readonly ISelectedAssetLoad[] owned;public ReleaseBatch(ISelectedAssetLoad[] value){owned=value;}public void Dispose(){foreach(var item in owned)item.Dispose();}}
  public static int[] Select(Candidate[] source,int player,int opponent,int limit){
   var result=new List<int>();var indices=new HashSet<int>();var ids=new HashSet<string>();var addresses=new HashSet<string>();if(source==null)return result.ToArray();
   for(int i=0;i<source.Length&&result.Count<Mathf.Clamp(limit,0,2);i++){
    var item=source[i];if(item.fighterIndex<0||item.fighterIndex==player||item.fighterIndex==opponent||string.IsNullOrEmpty(item.address)||string.IsNullOrEmpty(item.fighterId)||indices.Contains(item.fighterIndex)||ids.Contains(item.fighterId)||addresses.Contains(item.address))continue;
    indices.Add(item.fighterIndex);ids.Add(item.fighterId);addresses.Add(item.address);result.Add(i);
   }return result.ToArray();
  }
  public static int BudgetForQuality(int tier)=>tier<=0?0:tier==1?1:2;
  public void SetBudget(int count){count=Mathf.Clamp(count,0,2);if(maximumSpectators==count)return;maximumSpectators=count;if(configured&&isActiveAndEnabled)Restart();}
  public void Configure(int player,int opponent,ISelectedAssetProvider assetProvider=null){first=player;second=opponent;provider=assetProvider??new AddressableSelectedAssetProvider();configured=true;if(isActiveAndEnabled)Restart();}
  void OnEnable(){if(configured)Restart();}
  void OnDisable(){Clear();}
  void OnDestroy(){Clear();}
  void Restart(){Clear();pending=StartCoroutine(Load());}
  void Clear(){
   if(hookah)hookah.SetUser(null);
   if(pending!=null){StopCoroutine(pending);pending=null;}
   foreach(var obj in instances)if(obj)Destroy(obj);instances.Clear();
   if(loads.Count>0){SelectedContentReleaseQueue.Defer(new ReleaseBatch(loads.ToArray()));loads.Clear();}
  }
  IEnumerator Load(){
   var selected=Select(candidates,first,second,Mathf.Min(maximumSpectators,seats?.Length??0));var loadedIndices=new List<int>();
   foreach(var index in selected){ISelectedAssetLoad load=null;try{load=provider.Load(loadedIndices.Count==0&&!string.IsNullOrEmpty(candidates[index].smokingAddress)?candidates[index].smokingAddress:candidates[index].address);}catch(Exception e){Debug.LogWarning("Spectator load skipped: "+e.Message,this);}if(load!=null){loads.Add(load);loadedIndices.Add(index);}}
   for(int i=0;i<loads.Count;i++){
    var load=loads[i];while(!load.IsDone&&load.Error==null)yield return null;
    if(load.Error!=null||!load.Asset){Debug.LogWarning("Spectator candidate unavailable",this);continue;}
    // Background assets must be authored and baked in a seated pose before they can enter this pool.
    var pose=load.Asset.GetComponent<SeatedSpectatorPose>();
    if(!pose||!pose.seatedPoseVisuallyAccepted||load.Asset.GetComponentInChildren<Animator>(true)||load.Asset.GetComponentInChildren<SkinnedMeshRenderer>(true)){Debug.LogWarning("Spectator skipped: requires accepted static seated mesh",this);continue;}
    var instance=Instantiate(load.Asset,transform);instance.name="Seated spectator "+candidates[loadedIndices[i]].fighterId;instance.transform.localPosition=seats[i].floorOrigin;instance.transform.localRotation=Quaternion.Euler(0,seats[i].yaw,0);instances.Add(instance);if(hookah){var seated=instance.GetComponent<SeatedSpectatorPose>();if(seated&&seated.holdingMouthpiece)hookah.SetUser(seated);}
   }
   pending=null;
  }
 }
}

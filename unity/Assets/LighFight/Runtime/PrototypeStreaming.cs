using System;
using System.Collections;
using UnityEngine;
namespace LighFight.Runtime {
 public sealed partial class Prototype {
  [NonSerialized] public ISelectedAssetProvider SelectedAssetProvider;
  SelectedContentLease selectedContentLease;Coroutine selectedContentRoutine;
  int selectedContentGeneration;bool streamSelectionLoading,streamSelectionPaused;
  void CancelSelectedContent(){
   selectedContentGeneration++;
   if(selectedContentRoutine!=null){StopCoroutine(selectedContentRoutine);selectedContentRoutine=null;}
   SelectedContentReleaseQueue.Defer(selectedContentLease);selectedContentLease=null;
   streamSelectionLoading=false;
  }
  void RebuildStreamedViews(){
   streamSelectionLoading=true;actorsLoadingError=null;
   selectedContentRoutine=StartCoroutine(LoadSelectedViews(selectedContentGeneration,selectedFighter,OpponentIndex,selectedArena));
  }
  IEnumerator LoadSelectedViews(int generation,int first,int second,int location){
   // Destroy old instances and release their asset handles before loading another high-detail pair.
   yield return null;yield return null;
   if(generation!=selectedContentGeneration)yield break;
   SelectedContentLease lease=null;string error=null;
   try{lease=new SelectedContentLease(content,first,second,location,SelectedAssetProvider??new AddressableSelectedAssetProvider());selectedContentLease=lease;}
   catch(Exception exception){error=exception.Message;Debug.LogException(exception,this);}
   if(error==null){
    while(!lease.IsDone&&lease.Error==null){yield return null;if(generation!=selectedContentGeneration)yield break;}
    if(!lease.ValidateSelected(out error))Debug.LogWarning("Selected content: "+error,this);
   }
   if(generation!=selectedContentGeneration)yield break;
   if(error==null){
    try{player=Instantiate(lease.Player).GetComponent<FighterView>();opponent=Instantiate(lease.Opponent).GetComponent<FighterView>();arena=Instantiate(lease.Arena).GetComponent<OfficeArena>();}
    catch(Exception exception){error=exception.Message;Debug.LogException(exception,this);}
   }
   selectedContentRoutine=null;streamSelectionLoading=false;
   if(error!=null){
    if(player)Destroy(player.gameObject);if(opponent)Destroy(opponent.gameObject);if(arena)Destroy(arena.gameObject);
    player=null;opponent=null;arena=null;SelectedContentReleaseQueue.Defer(lease);selectedContentLease=null;
    actorsLoading=true;actorsLoadingError=T("Не удалось загрузить бойцов. Повтори попытку.","Fighter loading failed. Retry.");yield break;
   }
   ConfigureActorViews();
  }
  void ReleaseFailedSelectedWarmup(){
   if(content==null||!content.streamed)return;
   foreach(var warmup in actorWarmups)warmup.Dispose();actorWarmups=new IActorCurveWarmup[0];
   if(player){player.gameObject.SetActive(false);Destroy(player.gameObject);}if(opponent){opponent.gameObject.SetActive(false);Destroy(opponent.gameObject);}if(arena)Destroy(arena.gameObject);
   player=null;opponent=null;arena=null;SelectedContentReleaseQueue.Defer(selectedContentLease);selectedContentLease=null;
  }
  void PauseSelectedContent(){
   if(streamSelectionLoading){CancelSelectedContent();streamSelectionPaused=true;}
  }
  void ResumeSelectedContent(){
   if(streamSelectionPaused){streamSelectionPaused=false;RebuildViews();}
  }
 }
}

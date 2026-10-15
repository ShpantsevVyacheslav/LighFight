using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace LighFight.Runtime {
 // Default-assembly provider components implement this boundary; Runtime never reflects into them.
 public interface IActorCurveWarmup : IDisposable {
  bool IsWarm {get;}
  IEnumerator Warmup();
  void CancelWarmup();
 }
 public sealed partial class Prototype {
  bool actorsLoading,beginFightAfterWarmup;
  string actorsLoadingError;
  int actorWarmupGeneration;
  Coroutine actorWarmupRoutine;
  IEnumerator actorWarmupStep;
  IActorCurveWarmup[] actorWarmups=new IActorCurveWarmup[0];
  public bool ActorsLoading=>actorsLoading||streamSelectionLoading;
  void CancelActorWarmup(){
   actorWarmupGeneration++;
   if(actorWarmupRoutine!=null){StopCoroutine(actorWarmupRoutine);actorWarmupRoutine=null;}
   (actorWarmupStep as IDisposable)?.Dispose();actorWarmupStep=null;
   foreach(var warmup in actorWarmups)warmup.CancelWarmup();
  }
  void ReleaseActorWarmup(){
   CancelActorWarmup();
   foreach(var warmup in actorWarmups)warmup.Dispose();
   actorWarmups=new IActorCurveWarmup[0];
  }
  void StartActorWarmup(){
   var sources=new List<IActorCurveWarmup>();
   foreach(var fighter in new[]{player,opponent})foreach(var component in fighter.GetComponentsInChildren<MonoBehaviour>(true))if(component is IActorCurveWarmup warmup)sources.Add(warmup);
   actorWarmups=sources.ToArray();actorsLoadingError=null;actorsLoading=false;
   foreach(var warmup in actorWarmups)if(!warmup.IsWarm){actorsLoading=true;break;}
   if(!actorsLoading){FinishActorWarmup();return;}
   actorWarmupRoutine=StartCoroutine(WarmActorCurves(actorWarmupGeneration,player,opponent,actorWarmups));
  }
  IEnumerator WarmActorCurves(int generation,FighterView first,FighterView second,IActorCurveWarmup[] sources){
   // Let the opaque loading UI reach a rendered frame before synchronous curve decoding starts.
   yield return null;yield return null;
   foreach(var source in sources){
    if(generation!=actorWarmupGeneration||!first||!second)yield break;
    var routine=source.Warmup();actorWarmupStep=routine;
    bool failed=false;
    while(true){
     bool more=false;object next=null;
     try{more=routine.MoveNext();if(more)next=routine.Current;}
     catch(Exception error){actorsLoadingError=T("Не удалось загрузить бойцов","Fighter loading failed");Debug.LogException(error,this);failed=true;}
     if(failed||!more)break;
     yield return next;
     if(generation!=actorWarmupGeneration)yield break;
    }
    (routine as IDisposable)?.Dispose();if(ReferenceEquals(actorWarmupStep,routine))actorWarmupStep=null;
    if(failed){foreach(var warmup in sources)warmup.CancelWarmup();actorWarmupRoutine=null;ReleaseFailedSelectedWarmup();yield break;}
   }
   if(generation!=actorWarmupGeneration||!first||!second)yield break;
   actorWarmupRoutine=null;actorsLoading=false;FinishActorWarmup();
  }
  void FinishActorWarmup(){
   player.gameObject.SetActive(true);opponent.gameObject.SetActive(true);
   if(beginFightAfterWarmup){beginFightAfterWarmup=false;BeginFight();}
   if(selecting){previewFighter.X=match.A.X;player.FullPreviewMotion=true;player.Pose(previewFighter,1,0);}
   else player.Pose(match.A,1,0);
   opponent.Pose(match.B,-1,0);opponent.gameObject.SetActive(!selecting);arena.gameObject.SetActive(!selecting);
   accumulator=0;pendingAttack=-1;stickInput=Vector2.zero;stickFinger=-1;strikeGesture.Cancel();
  }
  void OnDisable(){ClearPlayerGuard();PauseSelectedContent();if(actorsLoading)CancelActorWarmup();}
  void OnEnable(){ResumeSelectedContent();if(actorsLoading&&player&&opponent)StartActorWarmup();}
  void ActorLoadingGUI(){
   SelectionResources();Card(new Rect(0,0,960,540),new Color(.025f,.028f,.045f));
   Text(new Rect(180,215,600,45),actorsLoadingError??T("Загрузка бойцов…","Loading fighters…"),23,selectionAccent,TextAnchor.MiddleCenter);
   if(actorsLoadingError!=null&&Pill(new Rect(380,285,200,42),T("Повторить","Retry")))RebuildViews();
  }
 }
}

using UnityEngine;
using LighFight.Core;
namespace LighFight.Runtime {
 public sealed partial class Prototype {
  private float victoryElapsed,victoryStartYaw;
  private int presentedWinner=-1;
  public static string VictoryMotion(FighterView view)=>view.HasNativeMotion("Victory")?"Victory":view.HasNativeMotion("Greeting")?"Greeting":view.HasNativeMotion("Dance")?"Dance":null;
  private void UpdateVictoryPresentation(float delta) {
   var first=session.Winner==0;var winner=first?player:opponent;var loser=first?opponent:player;
   var winning=first?match.A:match.B;var losing=first?match.B:match.A;
   if(presentedWinner!=session.Winner){presentedWinner=session.Winner;victoryElapsed=0;victoryStartYaw=winner.transform.eulerAngles.y;pendingAttack=-1;stickInput=Vector2.zero;stickFinger=-1;strikeGesture.Cancel();}
   victoryElapsed+=Mathf.Max(0,delta);
   if(!winner.ResultPoseHeld){
    winner.PoseTowards(winning,losing,delta);
    var motion=VictoryMotion(winner);if(motion!=null){winner.PreviewNativeMotion(motion,victoryElapsed);winner.RequestResultPoseHold(motion,victoryElapsed);}
   }
   if(!loser.ResultPoseHeld){
    loser.PoseTowards(losing,winning,delta);
    if(losing.Health>0 && loser.HasNativeMotion("Defeat")){loser.PreviewNativeMotion("Defeat",victoryElapsed);loser.RequestResultPoseHold("Defeat",victoryElapsed);}
    else if(losing.Health<=0)loser.RequestResultPoseHold("Fall",victoryElapsed);
   }
   if(!winner.ResultPoseHeld)winner.transform.rotation=Quaternion.Euler(0,Mathf.LerpAngle(victoryStartYaw,180,Mathf.SmoothStep(0,1,Mathf.Clamp01(victoryElapsed/.7f))),0);
   var focus=new Vector3(winning.X,1.25f,winning.Z);
   var position=focus+new Vector3(.7f,.4f,-4.2f);
   fightCamera.transform.position=Vector3.Lerp(fightCamera.transform.position,position,1-Mathf.Exp(-delta*3));
   fightCamera.transform.LookAt(focus);
  }
  private void VictoryGUI() {
   if(Pill(new Rect(888,12,52,40),"☰"))OpenOptions();
   Card(new Rect(24,382,365,132),new Color(.025f,.035f,.055f,.88f));
   Text(new Rect(42,393,320,38),T("ПОБЕДА","VICTORY"),28,selectionAccent);
   Text(new Rect(42,438,320,56),FighterLabel(session.Winner==0?selectedFighter:OpponentIndex),23);
  }
 }
}

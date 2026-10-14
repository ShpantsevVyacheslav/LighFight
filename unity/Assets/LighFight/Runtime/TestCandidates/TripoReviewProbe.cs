using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using LighFight.Core;
namespace LighFight.Runtime {
 // Attached only to the local candidate review scene; no persistent user data is logged.
 public sealed class TripoReviewProbe:MonoBehaviour {
  static readonly BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
  static void Set(Prototype p,string field,object value)=>typeof(Prototype).GetField(field,Private).SetValue(p,value);
  static T Get<T>(Prototype p,string field)=>(T)typeof(Prototype).GetField(field,Private).GetValue(p);
  static void Capture(string path,Prototype prototype){var camera=Get<Camera>(prototype,"fightCamera");var match=Get<FightSession>(prototype,"session").Match;var center=new Vector3((match.A.X+match.B.X)/2,1,(match.A.Z+match.B.Z)/2);camera.transform.position=center+new Vector3(0,.6f,-Prototype.FitCameraDistance(match.A.X,match.B.X,1280f/720));camera.transform.LookAt(center);var previous=camera.targetTexture;var active=RenderTexture.active;var render=new RenderTexture(1280,720,24);var image=new Texture2D(1280,720,TextureFormat.RGB24,false);try{render.Create();camera.targetTexture=render;camera.Render();camera.Render();camera.Render();RenderTexture.active=render;image.ReadPixels(new Rect(0,0,1280,720),0,0);image.Apply();File.WriteAllBytes(path,image.EncodeToPNG());}finally{camera.targetTexture=previous;RenderTexture.active=active;render.Release();Destroy(render);Destroy(image);}}
  IEnumerator Start(){
   Application.runInBackground=true;var run=Run();float deadline=Time.realtimeSinceStartup+600;
   while(true){bool more=false;object value=null;Exception failure=null;
    try{if(Time.realtimeSinceStartup>deadline)throw new TimeoutException("Review exceeded600seconds");more=run.MoveNext();if(more)value=run.Current;}catch(Exception error){failure=error;}
    if(failure!=null){Debug.LogError("REVIEW_FAILED "+failure);Application.Quit(1);yield break;}if(!more)yield break;
    if(value is IEnumerator nested){while(true){bool n=false;object item=null;try{if(Time.realtimeSinceStartup>deadline)throw new TimeoutException("Nested review exceeded600seconds");n=nested.MoveNext();if(n)item=nested.Current;}catch(Exception error){failure=error;}if(failure!=null){Debug.LogError("REVIEW_FAILED "+failure);Application.Quit(1);yield break;}if(!n)break;yield return item;}}
    else yield return value;
   }
  }
  IEnumerator Run(){
   var args=Environment.GetCommandLineArgs();int victoryArg=Array.IndexOf(args,"-qaVictoryOutput");if(victoryArg>=0){yield return VictorySmoke(args[victoryArg+1]);yield break;}int outputIndex=Array.IndexOf(args,"-qaSmokeOutput");if(outputIndex<0)yield break;
   string output=args[outputIndex+1];Directory.CreateDirectory(output);
   for(int i=0;i<5;i++)yield return null;
   int fighterIndex=4;int fighterArg=Array.IndexOf(args,"-qaSmokeFighter");if(fighterArg>=0)fighterIndex=int.Parse(args[fighterArg+1]);
   var prototype=GetComponent<Prototype>();Set(prototype,"selectedFighter",fighterIndex);Set(prototype,"selectedOpponent",fighterIndex);
   typeof(Prototype).GetMethod("RebuildViews",Private).Invoke(prototype,null);while(prototype.ActorsLoading)yield return null;prototype.BeginFight();Set(prototype,"paused",true);
   var player=Get<FighterView>(prototype,"player");var opponent=Get<FighterView>(prototype,"opponent");
   var late=typeof(FighterView).GetMethod("LateUpdate",Private);int samples=0;
   foreach(string id in new[]{"jab","cross","frontKick"}){
    typeof(Prototype).GetMethod("Reset",Private).Invoke(prototype,null);Set(prototype,"paused",true);
    var session=Get<FightSession>(prototype,"session");var match=session.Match;float gap=id=="frontKick"?1.1f:.65f;match.A.X=-gap/2;match.B.X=gap/2;
    var contacts=new PhysicalContacts(match,player,opponent);int index=Array.FindIndex(Moves.All,m=>m.Id==id);session.QueueAttack(true,index);
    for(int tick=0;tick<Moves.All[index].Duration+18;tick++){
     session.Step();player.PoseTowards(match.A,match.B,1f/60);opponent.PoseTowards(match.B,match.A,1f/60);
     late.Invoke(player,null);late.Invoke(opponent,null);
     foreach(var view in new[]{player,opponent}){view.GetComponent<NativeFingerPoseDriver>().ApplyForReview(true,1f/60);view.GetComponent<CandidateFootGrounding>().ApplyForReview();}
     samples++;
     if(tick==Moves.All[index].Startup+Moves.All[index].Active/2){yield return new WaitForEndOfFrame();Capture(Path.Combine(output,id+"-contact.png"),prototype);}
    }
    for(int i=0;i<4;i++)yield return new WaitForEndOfFrame();Capture(Path.Combine(output,id+"-return.png"),prototype);
    Debug.Log("REVIEW_SMOKE move="+id+" damage="+(150-match.B.Health)+" human="+player.GetComponentInChildren<Animator>().avatar.isHuman);
   }
   for(int i=0;i<5;i++)yield return new WaitForEndOfFrame();
   Debug.Log("AGENT_RESULT main development player smoke samples="+samples+" images="+Directory.GetFiles(output,"*.png").Length);
   Application.Quit(0);
  }
  IEnumerator VictorySmoke(string output){
   Directory.CreateDirectory(output);for(int i=0;i<5;i++)yield return null;
   var prototype=GetComponent<Prototype>();
   foreach(int fighter in new[]{2,3,4})foreach(int winningSide in new[]{0,1}){
    Set(prototype,"selectedFighter",fighter);Set(prototype,"selectedOpponent",fighter);
    typeof(Prototype).GetMethod("RebuildViews",Private).Invoke(prototype,null);
    float deadline=Time.realtimeSinceStartup+30;while(prototype.ActorsLoading&&Time.realtimeSinceStartup<deadline)yield return null;if(prototype.ActorsLoading)throw new Exception("Actors did not finish warming");
    while(prototype.ActorsLoading)yield return null;prototype.BeginFight();Set(prototype,"paused",true);
    var session=new FightSession(false,1,60);if(winningSide==0)session.Match.B.Health=0;else session.Match.A.Health=0;session.Step();Set(prototype,"session",session);
    if(session.Phase!=FightPhase.Complete)throw new Exception("Victory probe did not reach completion");
    var winner=Get<FighterView>(prototype,winningSide==0?"player":"opponent");var motion=Prototype.VictoryMotion(winner);if(string.IsNullOrEmpty(motion)||!winner.HasNativeMotion(motion))throw new Exception("Authored victory motion missing");float duration=winner.PreviewDuration(motion);if(duration<=0)throw new Exception("Victory duration missing");
    var update=typeof(Prototype).GetMethod("UpdateVictoryPresentation",Private);int frames=Mathf.CeilToInt((duration+1)*60);
    for(int tick=0;tick<=frames;tick++){
     update.Invoke(prototype,new object[]{1f/60});yield return new WaitForEndOfFrame();
     if(tick==60||tick==frames){string path=Path.Combine(output,"fighter-"+fighter+"-winner-"+winningSide+"-"+tick+".png");var previous=RenderTexture.active;Texture2D image=null;try{RenderTexture.active=null;image=new Texture2D(Screen.width,Screen.height,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,Screen.width,Screen.height),0,0);image.Apply();File.WriteAllBytes(path,image.EncodeToPNG());}finally{if(image)Destroy(image);RenderTexture.active=previous;}yield return null;}
    }
    Debug.Log("VICTORY_REVIEW fighter="+fighter+" motion="+motion+" duration="+duration+" winner="+session.Winner);
   }
   File.WriteAllText(Path.Combine(output,"completed.txt"),"Actual completed-session victory presentation; Master/Island/Rose; full authored duration plus one second.");
   Debug.Log("AGENT_RESULT victory review complete");Application.Quit(0);
  }
  void OnGUI(){if(!Debug.isDebugBuild)return;var e=Event.current;if(e.type==EventType.MouseDown||e.type==EventType.MouseUp)Debug.Log("REVIEW_POINTER type="+e.type+" event="+e.mousePosition+" input="+Input.mousePosition+" screen="+Screen.width+"x"+Screen.height+" enabled="+GUI.enabled);}
 }
}

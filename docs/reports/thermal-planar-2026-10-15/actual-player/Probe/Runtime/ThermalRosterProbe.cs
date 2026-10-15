using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Globalization;
using UnityEngine;
using UnityEngine.Profiling;
using LighFight.Runtime;
using LighFight.Core;
public sealed class ThermalRosterProbe:MonoBehaviour {
 public Prototype game;public string mode,manifest;public bool requireRosterSleep;
 const int Frames=200;
 struct Frame {public double wall,main;public int sceneRenders,clearRenders,actorTicks,opponentTicks,coreTick;public bool playerActive,opponentActive,cameraEnabled;public float zA,zB;}
 readonly WaitForEndOfFrame eof=new WaitForEndOfFrame();readonly List<string> failures=new List<string>();
 string output;int errors,sceneRenders,clearRenders;Camera fight,clear;ThermalActorTickCounter first,second;
 readonly List<string> summary=new List<string>{"stage,frames,wallMeanMs,wallMedianMs,wallP95Ms,wallWorstMs,mainMedianMs,mainP95Ms,allocatedBytes,reservedBytes,processWorkingSetBytes,selectionRTBytes,allRTBytes,clockDelta,sceneRenderCount,actorLateTicks,coreTickDelta"};
 void OnEnable(){Application.logMessageReceived+=Log;Camera.onPreCull+=Count;}
 void OnDisable(){Application.logMessageReceived-=Log;Camera.onPreCull-=Count;}
 void Log(string m,string s,LogType t){if(t==LogType.Error||t==LogType.Exception||t==LogType.Assert)errors++;}
 void Count(Camera c){if(c==fight)sceneRenders++;if(c==clear)clearRenders++;}
 object Get(string name){var f=typeof(Prototype).GetField(name,BindingFlags.Instance|BindingFlags.NonPublic|BindingFlags.Public);if(f==null)throw new Exception("Production field missing "+name);return f.GetValue(game);}
 void Set(string name,object value){var f=typeof(Prototype).GetField(name,BindingFlags.Instance|BindingFlags.NonPublic|BindingFlags.Public);if(f==null)throw new Exception("Production field missing "+name);f.SetValue(game,value);}
 void Call(string name){typeof(Prototype).GetMethod(name,BindingFlags.Instance|BindingFlags.NonPublic|BindingFlags.Public).Invoke(game,null);}
 bool Loading(){var p=typeof(Prototype).GetProperty("ActorsLoading",BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic);if(p==null)throw new Exception("Warmup contract missing");return (bool)p.GetValue(game);}
 IEnumerator Start(){
  output=Path.GetFullPath("ThermalRosterEvidence/"+mode);Directory.CreateDirectory(output);File.WriteAllText(output+"/build-manifest.json",manifest);
  var run=Run();double deadline=Time.realtimeSinceStartupAsDouble+240;
  while(true){bool more;try{more=run.MoveNext();if(Time.realtimeSinceStartupAsDouble>deadline)throw new TimeoutException("Thermal probe overall deadline");}catch(Exception e){File.WriteAllText(output+"/failure.txt",e.ToString());File.WriteAllText(output+"/result.json","{\"completed\":false,\"failed\":true}");Application.Quit(1);yield break;}if(!more)yield break;yield return run.Current;}
 }
 IEnumerator Run(){
  if(!game||!Debug.isDebugBuild||SystemInfo.graphicsDeviceType!=UnityEngine.Rendering.GraphicsDeviceType.Metal)throw new Exception("Actual development Metal Prototype required");
  yield return null; // Prototype.Start has initialized ordinary session/camera.
  Set("graphicsQuality",2);Set("frameRate",60);Set("shadowsEnabled",true);Set("cameraShake",false);Call("ApplyOptions");
  Application.targetFrameRate=60;QualitySettings.vSyncCount=0;
  Set("selectedOpponent",1);game.SelectRosterFighter(0);
  double wait=Time.realtimeSinceStartupAsDouble+40;while(Loading()){if(Time.realtimeSinceStartupAsDouble>wait)throw new TimeoutException("Native curve warmup deadline");yield return null;}
  fight=(Camera)Get("fightCamera");
  var a=(FighterView)Get("player");var b=(FighterView)Get("opponent");
  first=a.gameObject.AddComponent<ThermalActorTickCounter>();second=b.gameObject.AddComponent<ThermalActorTickCounter>();
  if(!a.UsesNativeMotion||!b.UsesNativeMotion)throw new Exception("Artist native rigs required");
  Set("rosterOpen",true);Set("selecting",true);
  foreach(string stage in new[]{"roster-initial","preview","roster-return","fight"}) {
   if(stage=="preview"){Set("rosterOpen",false);Set("previewMove",-2);Set("previewClock",0f);}
   if(stage=="roster-return")Set("rosterOpen",true);
   if(stage=="fight")game.BeginFight();
   // Identical real-time quiet warmup; actor warmup still bounded independently.
   double warm=Time.realtimeSinceStartupAsDouble+2;
   while(Time.realtimeSinceStartupAsDouble<warm||Loading()){if(Time.realtimeSinceStartupAsDouble>wait+60)throw new TimeoutException("Transition warmup");yield return eof;}
   clear=(Camera)Get("selectionDisplayCamera");
   var recorder=Recorder.Get("Main Thread");recorder.enabled=true;
   var liveSession=(FightSession)Get("session");int tickBefore=liveSession.Match.Tick;
   var samples=new Frame[Frames];int sr=sceneRenders,cr=clearRenders,at=first.lateTicks,bt=second.lateTicks;double prev=Time.realtimeSinceStartupAsDouble;float clockBefore=(float)Get("previewClock");
   for(int i=0;i<Frames;i++){
    yield return eof;
    double now=Time.realtimeSinceStartupAsDouble;
    samples[i]=new Frame{coreTick=liveSession.Match.Tick,wall=(now-prev)*1000,main=recorder.elapsedNanoseconds/1e6,sceneRenders=sceneRenders-sr,clearRenders=clearRenders-cr,actorTicks=first.lateTicks-at,opponentTicks=second.lateTicks-bt,playerActive=a.gameObject.activeInHierarchy,opponentActive=b.gameObject.activeInHierarchy,cameraEnabled=fight.enabled,zA=a.transform.position.z,zB=b.transform.position.z};
    prev=now;sr=sceneRenders;cr=clearRenders;at=first.lateTicks;bt=second.lateTicks;
   }
   // Measurement ended: allocations, memory, object enumeration, CSV and readback only below.
   var rt=(RenderTexture)Get("selectionRender");long rtBytes=rt?Profiler.GetRuntimeMemorySizeLong(rt):0;
   long allRT=Resources.FindObjectsOfTypeAll<RenderTexture>().Sum(r=>Profiler.GetRuntimeMemorySizeLong(r));
   long allocated=Profiler.GetTotalAllocatedMemoryLong(),reserved=Profiler.GetTotalReservedMemoryLong(),working=System.Diagnostics.Process.GetCurrentProcess().WorkingSet64;
   float clockDelta=(float)Get("previewClock")-clockBefore;bool roster=stage.StartsWith("roster");
   if(requireRosterSleep&&roster&&(rt||samples.Any(x=>x.playerActive||x.opponentActive||x.cameraEnabled||x.sceneRenders!=0||x.actorTicks!=0||x.opponentTicks!=0)||Mathf.Abs(clockDelta)>1e-6f))failures.Add(stage+": hidden 3D/clock/RT active");
   if(roster&&samples.Any(x=>x.clearRenders!=1))failures.Add(stage+": UI clear camera did not render once/frame");
   if(stage=="preview"&&(!rt||clockDelta<=0||samples.Any(x=>!x.playerActive||x.opponentActive||!x.cameraEnabled||x.sceneRenders!=1||x.actorTicks!=1)))failures.Add("Preview restoration failed");
   if(stage=="fight"&&(rt||samples.Any(x=>!x.playerActive||!x.opponentActive||!x.cameraEnabled||x.sceneRenders!=1||x.actorTicks!=1||x.opponentTicks!=1)))failures.Add("Battle restoration failed");
   if(stage=="fight"&&liveSession.Match.Tick-tickBefore<100)failures.Add("Battle core did not advance100ticks; focus/background/pause must be investigated");
   if(requireRosterSleep&&stage=="fight"&&samples.Any(x=>Mathf.Abs(x.zA)>1e-6f||Mathf.Abs(x.zB)>1e-6f))failures.Add("Nonplanar actual fight roots");
   var rows=new List<string>{"frame,wallMs,mainThreadMs,sceneRenders,clearRenders,actorLateTicks,opponentLateTicks,playerActive,opponentActive,cameraEnabled,actorZ,opponentZ,coreTick"};
   for(int i=0;i<Frames;i++){var s=samples[i];rows.Add(string.Join(",",i,N(s.wall),N(s.main),s.sceneRenders,s.clearRenders,s.actorTicks,s.opponentTicks,s.playerActive,s.opponentActive,s.cameraEnabled,N(s.zA),N(s.zB),s.coreTick));}
   File.WriteAllLines(output+"/"+stage+".csv",rows);
   var walls=samples.Select(s=>s.wall).ToArray();var mains=samples.Select(s=>s.main).ToArray();
   summary.Add(string.Join(",",stage,Frames,N(walls.Average()),N(Q(walls,.5)),N(Q(walls,.95)),N(walls.Max()),N(Q(mains,.5)),N(Q(mains,.95)),allocated,reserved,working,rtBytes,allRT,N(clockDelta),samples.Sum(x=>x.sceneRenders),samples.Sum(x=>x.actorTicks),liveSession.Match.Tick-tickBefore));
   File.WriteAllLines(output+"/summary.csv",summary);
   var image=ScreenCapture.CaptureScreenshotAsTexture();File.WriteAllBytes(output+"/"+stage+".png",image.EncodeToPNG());Destroy(image);
  }
  File.WriteAllLines(output+"/failures.txt",failures);
  File.WriteAllText(output+"/result.json",JsonUtility.ToJson(new Result{completed=true,failed=failures.Count>0||errors>0,errors=errors,failures=failures.ToArray(),mode=mode,target=Application.targetFrameRate,api=SystemInfo.graphicsDeviceType.ToString()},true));Application.Quit(failures.Count>0||errors>0?1:0);
 }
 [Serializable]class Result{public bool completed,failed;public int errors,target;public string mode,api;public string[] failures;}
 static string N(double x)=>x.ToString("R",CultureInfo.InvariantCulture);
 static double Q(double[] x,double q){var a=(double[])x.Clone();Array.Sort(a);return a[(int)Math.Ceiling(a.Length*q)-1];}
}

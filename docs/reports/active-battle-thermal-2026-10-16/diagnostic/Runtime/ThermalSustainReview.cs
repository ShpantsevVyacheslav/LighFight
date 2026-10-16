#if DEVELOPMENT_BUILD && LIGHTFIGHT_THERMAL_REVIEW
using System;using System.IO;using System.Linq;using System.Reflection;using System.Collections;using System.Collections.Generic;using System.Globalization;using System.Runtime.InteropServices;
using UnityEngine;using UnityEngine.Profiling;using LighFight.Runtime;using LighFight.Core;
// Optional QA component: no creation without explicit development-build command flag.
public sealed class ThermalSustainReview:MonoBehaviour {
 const string Flag="--lighfight-thermal-sustain";const int Capacity=74000;
 struct Frame{public int minDeltaA,minDeltaB,pointsA,pointsB,supportA,supportB,mainBlocks,renderBlocks,waitBlocks,groundBlocks,capBlocks,poseBlocks,contactBlocks;public float rawMinA,rawMinB,correctionA,correctionB;public double elapsed,wall,main,render,wait,ground,cap,pose,contact,gpuRaw,cpuFrame,cpuPresent;public long batches,setPass,triangles,vertices;public int moveAIndex,moveAFrame,moveBIndex,moveBFrame,downA,downB,hitA,hitB;public ulong stamp;public int frame,thermal,lowPower,cameraRenders,totalCameraRenders,width,height;}
 struct Slow{public double elapsed,cpuSeconds;public ulong footprint;public int thermal,lowPower,coreTick,healthA,healthB,downA,downB,hitA,hitB;public long allocated;}
 readonly Frame[] frames=new Frame[Capacity];readonly Slow[] slow=new Slow[900];readonly FrameTiming[] timing=new FrameTiming[1];
 readonly List<string> events=new List<string>();readonly WaitForEndOfFrame eof=new WaitForEndOfFrame();
 Unity.Profiling.ProfilerRecorder batchesRec,setPassRec,trianglesRec,verticesRec;Recorder main,render,wait,ground,cap,pose,contact;Prototype game;Camera viewCamera;FightSession session;FieldInfo sessionField,pendingField;MethodInfo resetMethod;object[] boxedMoves;int[] moveIndices;string[] moveIds;int[] scheduled,observedStarts,startupFrames,activeFrames,recoveryFrames;string lastAttack;int lastAttackFrame=-1;int downFramesA,downFramesB,hitsObserved,lastHitA,lastHitB;bool driving;
 CandidateFootGrounding groundA,groundB;NativeSupportOverlay overlayA,overlayB;int previousMinA,previousMinB;
 string output,stage,shadowMode="keep";int durationSeconds=600;
 readonly Dictionary<Light,UnityEngine.LightShadows> originalLightShadows=new Dictionary<Light,UnityEngine.LightShadows>();string materialsBefore;Renderer[] shadowRenderers;int used,slowUsed,renders,lastRender,totalRenders,lastTotalRender,thermal=-1,lowPower=-1,nextMove,errors;double start,previous,nextSlow,nextAttack,absoluteDeadline;bool capturing,autofight,finished;
#if UNITY_IOS && !UNITY_EDITOR
 [DllImport("__Internal")]static extern int LFReviewThermalState();[DllImport("__Internal")]static extern int LFReviewLowPower();[DllImport("__Internal")]static extern double LFReviewCPUSeconds();[DllImport("__Internal")]static extern ulong LFReviewPhysicalFootprint();
#else
 static int LFReviewThermalState()=>-1;static int LFReviewLowPower()=>-1;static double LFReviewCPUSeconds()=>-1;static ulong LFReviewPhysicalFootprint()=>0;
#endif
 [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]static void Bootstrap(){if(Environment.GetCommandLineArgs().Contains(Flag)){var go=new GameObject("Explicit development thermal sustain review");DontDestroyOnLoad(go);go.AddComponent<ThermalSustainReview>();}}
 void OnEnable(){Camera.onPreCull+=CameraCount;Application.logMessageReceived+=Log;}void OnDisable(){Camera.onPreCull-=CameraCount;Application.logMessageReceived-=Log;}
 void OnApplicationPause(bool value){if(value&&capturing)Finish("background_stop");}
 void OnApplicationQuit(){if(capturing)Finish("application_quit");}
 void CameraCount(Camera c){totalRenders++;if(c==viewCamera)renders++;}void Log(string s,string t,LogType type){if(type==LogType.Error||type==LogType.Exception||type==LogType.Assert)errors++;}
 static string Arg(string key,string fallback){var p=key+"=";var item=Environment.GetCommandLineArgs().FirstOrDefault(a=>a.StartsWith(p,StringComparison.Ordinal));return item==null?fallback:item.Substring(p.Length);}
 static FieldInfo Field(string name){var f=typeof(Prototype).GetField(name,BindingFlags.Instance|BindingFlags.NonPublic|BindingFlags.Public);if(f==null)throw new Exception("Production field absent "+name);return f;}
 IEnumerator Start(){
  output=Path.Combine(Application.persistentDataPath,"ThermalSustainReview",DateTime.UtcNow.ToString("yyyyMMdd-HHmmss"));Directory.CreateDirectory(output);absoluteDeadline=Time.realtimeSinceStartupAsDouble+740;
  var run=Run();while(true){if(finished)yield break;bool more;try{more=run.MoveNext();if(Time.realtimeSinceStartupAsDouble>absoluteDeadline)throw new TimeoutException("Thermal QA740s watchdog");}catch(Exception e){File.WriteAllText(output+"/failure.txt",e.ToString());Finish("exception");yield break;}if(!more)yield break;yield return run.Current;}
 }
 IEnumerator Run(){
  if(!Debug.isDebugBuild||!Environment.GetCommandLineArgs().Contains(Flag))throw new Exception("Explicit Development flag required");
  durationSeconds=int.Parse(Arg("--lf-thermal-seconds","600"));if(durationSeconds!=120&&durationSeconds!=600)throw new Exception("Thermal duration must be120screening or600sustained");absoluteDeadline=Time.realtimeSinceStartupAsDouble+durationSeconds+140;
  shadowMode=Arg("--lf-thermal-shadow","keep");if(shadowMode!="keep"&&shadowMode!="authored-on"&&shadowMode!="authored-none")throw new Exception("Invalid shadow diagnostic mode");
  stage=Arg("--lf-thermal-stage","fight");if(stage!="fight"&&stage!="preview"&&stage!="roster")throw new Exception("Invalid thermal stage");
  events.Add("beforeLoad,cpu="+N(LFReviewCPUSeconds())+",footprint="+LFReviewPhysicalFootprint()+",unityAllocated="+Profiler.GetTotalAllocatedMemoryLong());
  double loadingDeadline=Time.realtimeSinceStartupAsDouble+120;
  while(!(game=FindFirstObjectByType<Prototype>())){if(Time.realtimeSinceStartupAsDouble>loadingDeadline)throw new TimeoutException("Embedded Prototype not launched");yield return null;}
  int actor=int.Parse(Arg("--lf-thermal-actor","8")),opponent=int.Parse(Arg("--lf-thermal-opponent","3")),arena=int.Parse(Arg("--lf-thermal-arena","0"));
  Field("selectedOpponent").SetValue(game,opponent);Field("selectedArena").SetValue(game,arena);game.SelectRosterFighter(actor);
  var loading=typeof(Prototype).GetProperty("ActorsLoading",BindingFlags.Instance|BindingFlags.NonPublic|BindingFlags.Public);
  while((bool)loading.GetValue(game)){if(Time.realtimeSinceStartupAsDouble>loadingDeadline)throw new TimeoutException("Native selected warmup");yield return null;}
  events.Add("afterSelectedWarmup,cpu="+N(LFReviewCPUSeconds())+",footprint="+LFReviewPhysicalFootprint()+",unityAllocated="+Profiler.GetTotalAllocatedMemoryLong());
  viewCamera=(Camera)Field("fightCamera").GetValue(game);
  if(stage=="fight")game.BeginFight();else{Field("rosterOpen").SetValue(game,stage=="roster");if(stage=="preview"){Field("previewMove").SetValue(game,-2);Field("previewClock").SetValue(game,0f);}}
  yield return eof; // Observe actual production camera/RT state after the selected stage Update.
  ConfigurePureShadows();
  sessionField=Field("session");pendingField=Field("pendingAttack");resetMethod=typeof(Prototype).GetMethod("Reset",BindingFlags.Instance|BindingFlags.NonPublic);
  session=(FightSession)sessionField.GetValue(game);
  moveIds=new[]{"jab","frontKick","jumpKick","sideKick"};
  moveIndices=moveIds.Select(id=>{int index=Array.FindIndex(Moves.All,m=>m.Id==id);if(index<0)throw new Exception("Required workload move missing: "+id);return index;}).ToArray();
  boxedMoves=moveIndices.Select(i=>(object)i).ToArray();scheduled=new int[moveIds.Length];observedStarts=new int[moveIds.Length];startupFrames=new int[moveIds.Length];activeFrames=new int[moveIds.Length];recoveryFrames=new int[moveIds.Length];autofight=stage=="fight";
  var player=(FighterView)Field("player").GetValue(game);foreach(string id in moveIds)if(player.UsesNativeMotion&&!player.HasNativeMotion(id))throw new Exception("Actual actor lacks required own/shared move: "+id);
  var opponentView=(FighterView)Field("opponent").GetValue(game);groundA=player.GetComponentInChildren<CandidateFootGrounding>(true);groundB=opponentView.GetComponentInChildren<CandidateFootGrounding>(true);overlayA=player.GetComponentInChildren<NativeSupportOverlay>(true);overlayB=opponentView.GetComponentInChildren<NativeSupportOverlay>(true);if(!groundA||!groundB)throw new Exception("Actual actor grounding missing");
  main=Recorder.Get("Main Thread");render=Recorder.Get("Render Thread");wait=Recorder.Get("Gfx.WaitForPresentOnGfxThread");ground=Recorder.Get("LighFight.Grounding.Apply");cap=Recorder.Get("LighFight.Grounding.Cap");pose=Recorder.Get("LighFight.Actor.Pose");contact=Recorder.Get("LighFight.Contact.Sample");main.enabled=render.enabled=wait.enabled=ground.enabled=cap.enabled=pose.enabled=contact.enabled=true;
  // Exercise real production bot/core/contacts/animation before timed collection; warm native curves are already complete.
  driving=autofight;double warmUntil=Time.realtimeSinceStartupAsDouble+10;
  while(Time.realtimeSinceStartupAsDouble<warmUntil)yield return eof;
  CaptureBackbuffer("active-battle-before-screen.png");
  double quietUntil=Time.realtimeSinceStartupAsDouble+1;while(Time.realtimeSinceStartupAsDouble<quietUntil)yield return eof;
  Array.Clear(scheduled,0,scheduled.Length);lastAttack=null;lastAttackFrame=-1;lastHitA=session.Match.A.HitSerial;lastHitB=session.Match.B.HitSerial;
  if(Application.targetFrameRate!=60||(int)Field("graphicsQuality").GetValue(game)!=2)throw new Exception("Actual Max/60 settings required; probe does not lower or silently reset options");
  batchesRec=Unity.Profiling.ProfilerRecorder.StartNew(Unity.Profiling.ProfilerCategory.Render,"Batches Count",1);setPassRec=Unity.Profiling.ProfilerRecorder.StartNew(Unity.Profiling.ProfilerCategory.Render,"SetPass Calls Count",1);trianglesRec=Unity.Profiling.ProfilerRecorder.StartNew(Unity.Profiling.ProfilerCategory.Render,"Triangles Count",1);verticesRec=Unity.Profiling.ProfilerRecorder.StartNew(Unity.Profiling.ProfilerCategory.Render,"Vertices Count",1);
  File.WriteAllText(output+"/configuration.txt",Configuration(actor,opponent,arena)+"\nworkloadIds="+string.Join(";",moveIds)+"\nworkloadIndices="+string.Join(";",moveIndices)+"\nwarmupSeconds=10\npostCaptureQuietSeconds=1\ncoverageMinStarts=3\ncoverageMinActiveFrames=3");
  File.AppendAllText(output+"/configuration.txt","\nactorObject="+player.name+"\nopponentObject="+((FighterView)Field("opponent").GetValue(game)).name+"\nallMoveTimings="+string.Join(";",Moves.All.Select(m=>m.Id+":"+m.Startup+":"+m.Active+":"+m.Recovery))+"\nallMoveIds="+string.Join(";",Moves.All.Select(m=>m.Id))+"\nbatchesRecorderValid="+batchesRec.Valid+"\nsetPassRecorderValid="+setPassRec.Valid+"\ntrianglesRecorderValid="+trianglesRec.Valid+"\nverticesRecorderValid="+verticesRec.Valid);
  // No quality, resolution, target FPS, AA, shadows or texture settings are changed by this probe.
  previousMinA=groundA.MinimumEvaluationCount;previousMinB=groundB.MinimumEvaluationCount;File.AppendAllText(output+"/configuration.txt","\ncollector=R7\nsupportLabels=-1:missing;0:other;1:nativeSupport;2:defeatBody\nrecorderBlocks=sampleBlockCount;zero elapsed with zero blocks means no observed samples\nminimumDelta=EOF cumulative difference;includes all production callers;no additional MeasureMinimum calls");
  start=previous=Time.realtimeSinceStartupAsDouble;nextSlow=start;lastRender=renders;lastTotalRender=totalRenders;capturing=true;
  while(!finished&&Time.realtimeSinceStartupAsDouble-start<durationSeconds){
   FrameTimingManager.CaptureFrameTimings();yield return eof;
   ObserveCoverage();double now=Time.realtimeSinceStartupAsDouble;
   if(now>=nextSlow){if(slowUsed>=slow.Length)throw new Exception("Slow telemetry overflow");thermal=LFReviewThermalState();lowPower=LFReviewLowPower();slow[slowUsed++]=new Slow{elapsed=now-start,cpuSeconds=LFReviewCPUSeconds(),footprint=LFReviewPhysicalFootprint(),thermal=thermal,lowPower=lowPower,allocated=Profiler.GetTotalAllocatedMemoryLong(),coreTick=session?.Match.Tick??-1,healthA=session?.Match.A.Health??-1,healthB=session?.Match.B.Health??-1,downA=session?.Match.A.DownTicks??-1,downB=session?.Match.B.DownTicks??-1,hitA=session?.Match.A.HitSerial??-1,hitB=session?.Match.B.HitSerial??-1};nextSlow=now+1;}
   if(used>=frames.Length)throw new Exception("Frame buffer overflow (unexpected uncapped/high refresh workload)");
   FrameTiming f=default;bool available=FrameTimingManager.GetLatestTimings(1,timing)>0;if(available)f=timing[0];
   frames[used++]=new Frame{minDeltaA=groundA.MinimumEvaluationCount-previousMinA,minDeltaB=groundB.MinimumEvaluationCount-previousMinB,pointsA=groundA.shoePoints==null?0:groundA.shoePoints.Length,pointsB=groundB.shoePoints==null?0:groundB.shoePoints.Length,supportA=Support(groundA,overlayA),supportB=Support(groundB,overlayB),rawMinA=groundA.UncorrectedMinimum,rawMinB=groundB.UncorrectedMinimum,correctionA=groundA.LastCorrection,correctionB=groundB.LastCorrection,mainBlocks=main.sampleBlockCount,renderBlocks=render.sampleBlockCount,waitBlocks=wait.sampleBlockCount,groundBlocks=ground.sampleBlockCount,capBlocks=cap.sampleBlockCount,poseBlocks=pose.sampleBlockCount,contactBlocks=contact.sampleBlockCount,elapsed=now-start,wall=(now-previous)*1000,main=main.elapsedNanoseconds/1e6,render=render.elapsedNanoseconds/1e6,wait=wait.elapsedNanoseconds/1e6,ground=ground.elapsedNanoseconds/1e6,cap=cap.elapsedNanoseconds/1e6,pose=pose.elapsedNanoseconds/1e6,contact=contact.elapsedNanoseconds/1e6,gpuRaw=available?f.gpuFrameTime:double.NaN,cpuFrame=available?f.cpuFrameTime:double.NaN,cpuPresent=available?f.cpuMainThreadPresentWaitTime:double.NaN,stamp=available?f.frameStartTimestamp:0,frame=Time.frameCount,thermal=thermal,lowPower=lowPower,cameraRenders=renders-lastRender,totalCameraRenders=totalRenders-lastTotalRender,width=Screen.width,height=Screen.height,moveAIndex=MoveIndex(session.Match.A),moveAFrame=session.Match.A.Attack.HasValue?session.Match.A.Frame:-1,moveBIndex=MoveIndex(session.Match.B),moveBFrame=session.Match.B.Attack.HasValue?session.Match.B.Frame:-1,downA=session.Match.A.DownTicks,downB=session.Match.B.DownTicks,hitA=session.Match.A.HitSerial,hitB=session.Match.B.HitSerial,batches=batchesRec.Valid?batchesRec.LastValue:-1,setPass=setPassRec.Valid?setPassRec.LastValue:-1,triangles=trianglesRec.Valid?trianglesRec.LastValue:-1,vertices=verticesRec.Valid?verticesRec.LastValue:-1};previousMinA=groundA.MinimumEvaluationCount;previousMinB=groundB.MinimumEvaluationCount;previous=now;lastRender=renders;lastTotalRender=totalRenders;
   if(thermal==3){Finish("critical_thermal_stop");yield break;}
  }
  if(!finished)Finish("completed"+durationSeconds+"seconds");
 }
 void Update(){
  if(!driving||!autofight||!game)return;
  session=(FightSession)sessionField.GetValue(game);if(session==null)return;
  if(session.Phase==FightPhase.Complete){resetMethod.Invoke(game,null);session=(FightSession)sessionField.GetValue(game);lastAttack=null;lastAttackFrame=-1;lastHitA=session.Match.A.HitSerial;lastHitB=session.Match.B.HitSerial;events.Add("rematch,"+N(Time.realtimeSinceStartupAsDouble-start));return;}
  if(session.Phase!=FightPhase.Fighting)return;
  if(Time.realtimeSinceStartupAsDouble>=nextAttack&&!session.Match.A.Attack.HasValue&&session.Match.A.DownTicks==0&&session.Match.A.Stun==0){int slot=nextMove++%boxedMoves.Length;pendingField.SetValue(game,boxedMoves[slot]);if(capturing)scheduled[slot]++;nextAttack=Time.realtimeSinceStartupAsDouble+.35;}
 }
 string Configuration(int actor,int opponent,int arena){var rt=viewCamera?viewCamera.targetTexture:null;return string.Join("\n","scope=Development diagnostic; not power meter","frameBufferCapacity="+Capacity,"frameBufferStructBytes="+Marshal.SizeOf(typeof(Frame)),"unity="+Application.unityVersion,"deviceModel="+SystemInfo.deviceModel,"OS="+SystemInfo.operatingSystem,"graphics="+SystemInfo.graphicsDeviceType+" "+SystemInfo.graphicsDeviceName,"durationSeconds="+durationSeconds,"shadowDiagnostic="+shadowMode,"actualLightShadows="+string.Join(";",FindObjectsByType<Light>(FindObjectsSortMode.None).Select(l=>l.name+":"+l.shadows)),"authoredMaterialInvariant="+(shadowMode=="keep"?"not_requested":(MaterialFingerprint()==materialsBefore).ToString()),"stage="+stage,"actor="+actor,"opponent="+opponent,"arena="+arena,"qualityLevel="+QualitySettings.GetQualityLevel(),"requestedGraphicsTier="+Field("graphicsQuality").GetValue(game),"targetFPS="+Application.targetFrameRate,"vSyncCount="+QualitySettings.vSyncCount,"displayRefreshHz="+Screen.currentResolution.refreshRateRatio.value,"AA="+QualitySettings.antiAliasing,"shadows="+QualitySettings.shadows,"shadowResolution="+QualitySettings.shadowResolution,"shadowCascades="+QualitySettings.shadowCascades,"shadowDistance="+QualitySettings.shadowDistance,"textureMipmapLimit="+QualitySettings.globalTextureMipmapLimit,"streamingMipmaps="+QualitySettings.streamingMipmapsActive,"screenBrightness="+(Application.isMobilePlatform?Screen.brightness:-1),"screen="+Screen.width+"x"+Screen.height,"cameraPixels="+(viewCamera?viewCamera.pixelWidth+"x"+viewCamera.pixelHeight:"missing"),"cameraHDR="+(viewCamera&&viewCamera.allowHDR),"cameraMSAA="+(viewCamera&&viewCamera.allowMSAA),"targetRT="+(rt?rt.width+"x"+rt.height+" "+rt.format+" samples"+rt.antiAliasing:"backbuffer"),"frameTimingEnabled="+FrameTimingManager.IsFeatureEnabled(),"cpuTimerHz="+FrameTimingManager.GetCpuTimerFrequency(),"gpuTimerHz="+FrameTimingManager.GetGpuTimerFrequency(),"mainMarker="+main.isValid,"renderMarker="+render.isValid,"waitMarker="+wait.isValid,"groundMarker="+ground.isValid,"capMarker="+cap.isValid,"poseMarker="+pose.isValid,"contactMarker="+contact.isValid);}
 static int Support(CandidateFootGrounding g,NativeSupportOverlay o){if(!g)return -1;if(o&&ReferenceEquals(g.shoePoints,o.defeatBodyPoints))return 2;if(o&&ReferenceEquals(g.shoePoints,o.supportPoints))return 1;return 0;}
 static int MoveIndex(Fighter f){if(!f.Attack.HasValue)return -1;string id=f.Attack.Value.Id;for(int i=0;i<Moves.All.Length;i++)if(Moves.All[i].Id==id)return i;return -2;}
 void ObserveCoverage(){
  var a=session.Match.A;var b=session.Match.B;if(a.DownTicks>0)downFramesA++;if(b.DownTicks>0)downFramesB++;
  hitsObserved+=Math.Max(0,a.HitSerial-lastHitA)+Math.Max(0,b.HitSerial-lastHitB);lastHitA=a.HitSerial;lastHitB=b.HitSerial;
  if(!a.Attack.HasValue){lastAttack=null;lastAttackFrame=-1;return;}var m=a.Attack.Value;int slot=Array.IndexOf(moveIds,m.Id);if(slot<0)return;
  if(lastAttack!=m.Id||a.Frame<lastAttackFrame)observedStarts[slot]++;
  if(a.Frame<m.Startup)startupFrames[slot]++;else if(a.Frame<m.Startup+m.Active)activeFrames[slot]++;else recoveryFrames[slot]++;
  lastAttack=m.Id;lastAttackFrame=a.Frame;
 }
 void CaptureBackbuffer(string name){var old=RenderTexture.active;Texture2D image=null;try{RenderTexture.active=null;image=new Texture2D(Screen.width,Screen.height,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,Screen.width,Screen.height),0,0);image.Apply();File.WriteAllBytes(output+"/"+name,image.EncodeToPNG());}finally{if(image)Destroy(image);RenderTexture.active=old;}}
 void ConfigurePureShadows(){
  if(shadowMode=="keep")return;
  if(stage=="roster")throw new Exception("Pure-shadow condition requires preview or fight");
  if(!(bool)Field("shadowsEnabled").GetValue(game)||QualitySettings.shadows==UnityEngine.ShadowQuality.Disable)throw new Exception("Select authored Shadows ON in normal options before pure-shadow QA; do not use shadowless materials");
  shadowRenderers=FindObjectsByType<Renderer>(FindObjectsSortMode.None);
  foreach(var r in shadowRenderers)foreach(var m in r.sharedMaterials)if(m&&m.shader&&m.shader.name=="LighFight/Shadowless Character")throw new Exception("Shadowless BRDF invalidates pure-shadow condition");
  materialsBefore=MaterialFingerprint();
  foreach(var light in FindObjectsByType<Light>(FindObjectsSortMode.None)){originalLightShadows.Add(light,light.shadows);if(shadowMode=="authored-none")light.shadows=LightShadows.None;}
  if(MaterialFingerprint()!=materialsBefore)throw new Exception("Material/property mutation during pure-shadow setup");
  File.WriteAllText(output+"/pure-shadow-materials.txt",materialsBefore);
 }
 string MaterialFingerprint(){
  if(shadowRenderers==null)return "not_requested";var text=new System.Text.StringBuilder();
  foreach(var r in shadowRenderers){if(!r)continue;text.Append(r.GetInstanceID()).Append('|').Append(r.shadowCastingMode).Append('|').Append(r.receiveShadows);
   foreach(var m in r.sharedMaterials){if(!m){text.Append("null");continue;}var shader=m.shader;text.Append('|').Append(m.GetInstanceID()).Append('|').Append(shader.GetInstanceID()).Append('|').Append(m.renderQueue).Append('|').Append(string.Join(";",m.shaderKeywords.OrderBy(k=>k,StringComparer.Ordinal)));
    for(int i=0;i<shader.GetPropertyCount();i++){string p=shader.GetPropertyName(i);text.Append('|').Append(p).Append('=');switch(shader.GetPropertyType(i)){
     case UnityEngine.Rendering.ShaderPropertyType.Color:var c=m.GetColor(p);text.Append(Bits(c.r)).Append(',').Append(Bits(c.g)).Append(',').Append(Bits(c.b)).Append(',').Append(Bits(c.a));break;
     case UnityEngine.Rendering.ShaderPropertyType.Vector:var v=m.GetVector(p);text.Append(Bits(v.x)).Append(',').Append(Bits(v.y)).Append(',').Append(Bits(v.z)).Append(',').Append(Bits(v.w));break;
     case UnityEngine.Rendering.ShaderPropertyType.Texture:var t=m.GetTexture(p);var scale=m.GetTextureScale(p);var offset=m.GetTextureOffset(p);text.Append(t?t.GetInstanceID():0).Append(',').Append(Bits(scale.x)).Append(',').Append(Bits(scale.y)).Append(',').Append(Bits(offset.x)).Append(',').Append(Bits(offset.y));break;
     case UnityEngine.Rendering.ShaderPropertyType.Int:text.Append(m.GetInteger(p));break;
     default:text.Append(Bits(m.GetFloat(p)));break;
    }}
   }
  }return text.ToString();
 }
 static string Bits(float f)=>BitConverter.SingleToInt32Bits(f).ToString("X8",CultureInfo.InvariantCulture);
 public void StopReview(string ignored){Finish("manual_stop");}
 void Finish(string reason){
  if(finished)return;finished=true;capturing=false;driving=false;autofight=false;
  try{
  if(stage=="fight"&&observedStarts!=null){
   var coverage=new List<string>{"id,index,scheduled,observedStarts,startupFrames,activeFrames,recoveryFrames"};bool covered=true;
   for(int i=0;i<moveIds.Length;i++){coverage.Add(string.Join(",",moveIds[i],moveIndices[i],scheduled[i],observedStarts[i],startupFrames[i],activeFrames[i],recoveryFrames[i]));covered&=observedStarts[i]>=3&&activeFrames[i]>=3;}
   File.WriteAllLines(output+"/coverage.csv",coverage);File.WriteAllText(output+"/coverage-result.txt","passed="+covered+"\nhitsObserved="+hitsObserved+"\ndownFramesA="+downFramesA+"\ndownFramesB="+downFramesB+"\n");
   if(reason=="completed"+durationSeconds+"seconds"&&(!covered||hitsObserved==0)){errors++;reason="coverage_failed_"+reason;}
   CaptureBackbuffer("active-battle-after-screen.png");
  }
  if(shadowMode!="keep"&&materialsBefore!=null&&MaterialFingerprint()!=materialsBefore){errors++;reason="material_invariant_failed_"+reason;}
  var lines=new List<string>{"elapsed,wallMs,mainMs,renderMs,waitMs,groundMs,capMs,poseMs,contactMs,gpuRawMs,cpuFrameMs,cpuPresentWaitMs,timestamp,unityFrame,thermal,lowPower,sceneCameraRenders,totalCameraRenders,width,height,moveAIndex,moveAFrame,moveBIndex,moveBFrame,downA,downB,hitA,hitB,batches,setPass,triangles,vertices,minEvalDeltaA,minEvalDeltaB,supportPointsA,supportPointsB,supportKindA,supportKindB,rawMinimumA,rawMinimumB,correctionA,correctionB,mainBlocks,renderBlocks,waitBlocks,groundBlocks,capBlocks,poseBlocks,contactBlocks"};
  for(int i=0;i<used;i++){var s=frames[i];lines.Add(string.Join(",",N(s.elapsed),N(s.wall),N(s.main),N(s.render),N(s.wait),N(s.ground),N(s.cap),N(s.pose),N(s.contact),N(s.gpuRaw),N(s.cpuFrame),N(s.cpuPresent),s.stamp,s.frame,s.thermal,s.lowPower,s.cameraRenders,s.totalCameraRenders,s.width,s.height,s.moveAIndex,s.moveAFrame,s.moveBIndex,s.moveBFrame,s.downA,s.downB,s.hitA,s.hitB,s.batches,s.setPass,s.triangles,s.vertices,s.minDeltaA,s.minDeltaB,s.pointsA,s.pointsB,s.supportA,s.supportB,N(s.rawMinA),N(s.rawMinB),N(s.correctionA),N(s.correctionB),s.mainBlocks,s.renderBlocks,s.waitBlocks,s.groundBlocks,s.capBlocks,s.poseBlocks,s.contactBlocks));}File.WriteAllLines(output+"/frames.csv",lines);
  lines=new List<string>{"elapsed,cpuProcessSeconds,physicalFootprintBytes,unityAllocatedBytes,thermal,lowPower,coreTick,healthA,healthB,downA,downB,hitA,hitB"};for(int i=0;i<slowUsed;i++){var s=slow[i];lines.Add(string.Join(",",N(s.elapsed),N(s.cpuSeconds),s.footprint,s.allocated,s.thermal,s.lowPower,s.coreTick,s.healthA,s.healthB,s.downA,s.downB,s.hitA,s.hitB));}File.WriteAllLines(output+"/slow.csv",lines);File.WriteAllLines(output+"/events.csv",events);
  File.WriteAllText(output+"/result.txt","reason="+reason+"\nframes="+used+"\nerrors="+errors+"\nelapsed="+(used>0?N(frames[used-1].elapsed):"0")+"\n");
  }catch(Exception e){errors++;reason="flush_failed_"+reason;Debug.LogException(e);}
  finally{
  // Bound actual QA workload; close the embedded game via existing protocol, no host changes.
  batchesRec.Dispose();setPassRec.Dispose();trianglesRec.Dispose();verticesRec.Dispose();
  foreach(var pair in originalLightShadows)if(pair.Key)pair.Key.shadows=pair.Value;
  var bridge=FindFirstObjectByType<HostBridge>();if(bridge)bridge.Exit("thermal_review_"+reason);else if(game){foreach(var name in new[]{"player","opponent"}){var v=Field(name).GetValue(game) as FighterView;if(v)v.gameObject.SetActive(false);}game.gameObject.SetActive(false);}
  if(Application.platform!=RuntimePlatform.IPhonePlayer)Application.Quit(reason=="completed"+durationSeconds+"seconds"&&errors==0?0:1);
  }
 }
 static string N(double d)=>d.ToString("R",CultureInfo.InvariantCulture);
}
#endif

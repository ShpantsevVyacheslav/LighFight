using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using UnityEngine;
using UnityEngine.Profiling;
using Unity.Profiling;
using Unity.Profiling.LowLevel.Unsafe;
using LighFight.Runtime;
using LighFight.Core;
// Review player only. No geometry changes, manual camera render, or readback inside measurement.
public sealed class DenseSkinningBenchmark : MonoBehaviour {
 public GameObject acceptedPrefab,densePrefab;
 public Camera benchmarkCamera;
 public string buildManifest,buildMode;
 public int warmupFrames=120, measuredFrames=200;
 struct Sample { public double wall,main,gpu; public int renders; public ulong stamp; }
 readonly FrameTiming[] timing=new FrameTiming[1];
 readonly List<GameObject> actors=new List<GameObject>();
 FighterView[] views; Animator[] animators; NativeSupportOverlay[] overlays;
 Fighter[] actorFighters,targets; Move? activeMove; bool drive,holdAttack,holdGreeting; int tick; string motion;
 readonly List<string> poseRows=new List<string>{"image,actor,commandState,commandFrame,commandSourceSeconds,observedAnimatorHash,observedAnimatorPhase,overlayState,overlayPhase,overlayWeight,fighterBlendElapsed,fighterBlendDuration,settleRealSeconds,stableEOFs"};
 static readonly FieldInfo BlendElapsed=typeof(FighterView).GetField("blendElapsed",BindingFlags.Instance|BindingFlags.NonPublic),BlendDuration=typeof(FighterView).GetField("blendDuration",BindingFlags.Instance|BindingFlags.NonPublic);
 string output; int errors; double deadline; readonly WaitForEndOfFrame eof=new WaitForEndOfFrame();
 void OnEnable(){Application.logMessageReceived+=OnLog;}
 void OnDisable(){Application.logMessageReceived-=OnLog;}
 void OnLog(string s,string trace,LogType t){if(t==LogType.Exception||t==LogType.Error||t==LogType.Assert)errors++;}
 void Update(){
  if(views==null)return;
  if(holdGreeting){for(int i=0;i<views.Length;i++){if(!views[i].PreviewNativeMotion("Greeting",0))throw new Exception("Held Greeting unavailable");animators[i].speed=0;}return;}
  if(!drive)return;
  for(int i=0;i<views.Length;i++) {
   var f=actorFighters[i];
   f.Attack=activeMove;
   if(activeMove.HasValue&&!holdAttack)f.Frame=tick%activeMove.Value.Duration;
   views[i].PoseTowards(f,targets[i],holdAttack?Time.unscaledDeltaTime:1f/60);
  }
  tick++;
 }
 IEnumerator Start(){
  output=Path.GetFullPath("DenseSkinningEvidence/"+buildMode);Directory.CreateDirectory(output);
  File.WriteAllText(output+"/build-manifest.json",buildManifest);
  deadline=Time.realtimeSinceStartupAsDouble+1200;
  var run=Run();
  while(true){bool more;try{more=run.MoveNext();if(Time.realtimeSinceStartupAsDouble>deadline)throw new TimeoutException("Benchmark deadline");}catch(Exception e){File.WriteAllText(output+"/failure.txt",e.ToString());Application.Quit(1);yield break;}if(!more)yield break;yield return run.Current;}
 }
 IEnumerator Run(){
  Application.targetFrameRate=-1;QualitySettings.vSyncCount=0;QualitySettings.skinWeights=SkinWeights.Unlimited;
  if(SystemInfo.graphicsDeviceType!=UnityEngine.Rendering.GraphicsDeviceType.Metal)throw new Exception("Metal required");
  if(!Debug.isDebugBuild)throw new Exception("Development player required");
  if(Camera.allCameras.Count(c=>c.enabled)!=1||!benchmarkCamera.enabled||benchmarkCamera.targetTexture)throw new Exception("Exactly one automatic backbuffer camera required");
  var counter=benchmarkCamera.gameObject.GetComponent<DenseBenchmarkRenderCounter>();
  var available=new List<ProfilerRecorderHandle>();ProfilerRecorderHandle.GetAvailable(available);
  var markerNames=available.Select(h=>ProfilerRecorderHandle.GetDescription(h)).Where(d=>d.Name.IndexOf("skin",StringComparison.OrdinalIgnoreCase)>=0||d.Name.IndexOf("mesh",StringComparison.OrdinalIgnoreCase)>=0).Select(d=>d.Category+","+d.Name+","+d.UnitType).ToArray();
  File.WriteAllLines(output+"/available-skin-mesh-markers.csv",markerNames);
  var recorder=Recorder.Get("Main Thread");recorder.enabled=true;
  var summary=new List<string>{"scenario,motion,frames,wallMedianMs,wallP95Ms,wallWorstMs,mainMedianMs,mainP95Ms,mainWorstMs,gpuValidUniqueFrames,gpuMedianMs,gpuP95Ms,gpuWorstMs,allocatedBytes,reservedBytes,monoUsedBytes,processWorkingSetBytes"};
  foreach(string scenario in new[]{"100k-1actor","2m-1actor","2m-2actors"}) {
   int count=scenario=="2m-2actors"?2:1;
   for(int i=0;i<count;i++) {
    var a=Instantiate(scenario.StartsWith("100k")?acceptedPrefab:densePrefab);actors.Add(a);
    a.transform.position=new Vector3(count==1?0:(i==0?-.5f:.5f),0,0);
    foreach(var skin in a.GetComponentsInChildren<SkinnedMeshRenderer>()) {skin.quality=SkinQuality.Auto;skin.forceMatrixRecalculationPerRender=true;if(skin.sharedMesh.vertexCount<10000)throw new Exception("Unexpected mesh");}
   }
   views=actors.Select(a=>a.GetComponent<FighterView>()).ToArray();animators=actors.Select(a=>a.GetComponentInChildren<Animator>()).ToArray();
   overlays=actors.Select(a=>a.GetComponent<NativeSupportOverlay>()).ToArray();
   actorFighters=new Fighter[count];targets=new Fighter[count];
   for(int i=0;i<count;i++){float x=count==1?0:(i==0?-.5f:.5f);actorFighters[i]=new Fighter{X=x,FacingX=1,Guard=true};targets[i]=new Fighter{X=x+1,Guard=true};}
   if(views.Any(v=>!v)||animators.Any(a=>!a))throw new Exception("Production animation dependencies missing");
   foreach(var a in actors){var o=a.GetComponent<NativeSupportOverlay>();if(!o)throw new Exception("Native overlay missing");var warm=o.Warmup();while(warm.MoveNext())yield return warm.Current;}
   foreach(string state in new[]{"guard","jab","frontKick"}) {
    holdGreeting=false;motion=state;activeMove=state=="guard"?(Move?)null:Moves.All.Single(m=>m.Id==state);holdAttack=false;tick=0;drive=true;foreach(var a in animators)a.speed=1;
    for(int j=0;j<warmupFrames;j++)yield return eof;
    GC.Collect();yield return eof;yield return eof;
    var samples=new Sample[measuredFrames];
    double previous=Time.realtimeSinceStartupAsDouble;
    int before=counter.renders;ulong lastStamp=0;
    for(int j=0;j<samples.Length;j++) {
     FrameTimingManager.CaptureFrameTimings();
     yield return eof;
     double now=Time.realtimeSinceStartupAsDouble;
     double gpu=double.NaN;ulong stamp=0;
     if(FrameTimingManager.GetLatestTimings(1,timing)>0){var ft=timing[0];stamp=ft.frameStartTimestamp;if(stamp!=0&&stamp!=lastStamp&&ft.gpuFrameTime>0&&!double.IsInfinity(ft.gpuFrameTime)&&!double.IsNaN(ft.gpuFrameTime)){gpu=ft.gpuFrameTime;lastStamp=stamp;}}
     samples[j]=new Sample{wall=(now-previous)*1000,main=recorder.elapsedNanoseconds/1e6,gpu=gpu,renders=counter.renders-before,stamp=stamp};
     previous=now;before=counter.renders;
    }
    // Measurement ends here. Memory reads, formatting, writes and image readback are outside it.
    drive=false;
    if(samples.Any(s=>s.renders!=1))throw new Exception("Automatic camera rendered other than once per measured frame");
    long allocated=Profiler.GetTotalAllocatedMemoryLong(),reserved=Profiler.GetTotalReservedMemoryLong(),mono=Profiler.GetMonoUsedSizeLong();
    long working=System.Diagnostics.Process.GetCurrentProcess().WorkingSet64;
    var csv=new List<string>{"frame,wallMs,mainThreadMs,gpuMs,automaticCameraRenders,gpuTimestamp"};
    for(int j=0;j<samples.Length;j++){var s=samples[j];csv.Add(j+","+N(s.wall)+","+N(s.main)+","+N(s.gpu)+","+s.renders+","+s.stamp);}
    File.WriteAllLines(output+"/"+scenario+"-"+state+".csv",csv);
    var walls=samples.Select(s=>s.wall).ToArray();var mains=samples.Select(s=>s.main).Where(x=>x>0).ToArray();var gpus=samples.Select(s=>s.gpu).Where(x=>!double.IsNaN(x)).ToArray();
    summary.Add(string.Join(",",scenario,state,samples.Length,N(Q(walls,.5)),N(Q(walls,.95)),N(walls.Max()),N(Q(mains,.5)),N(Q(mains,.95)),N(Q(mains,1)),gpus.Length,N(Q(gpus,.5)),N(Q(gpus,.95)),N(Q(gpus,1)),allocated,reserved,mono,working));
    // Same original production pose phase in every mode; automatic rendering remains enabled.
    holdGreeting=true;
    foreach(var v in views){if(!v.PreviewNativeMotion("Greeting",0))throw new Exception("Greeting source pose missing");}
    foreach(var a in animators)a.speed=0;
    var settle=SettlePose(scenario+"-"+state+"-Greeting0.png","Greeting",0,0,true);
    while(settle.MoveNext())yield return settle.Current;
    var image=ScreenCapture.CaptureScreenshotAsTexture();
    File.WriteAllBytes(output+"/"+scenario+"-"+state+"-Greeting0.png",image.EncodeToPNG());Destroy(image);
    // Actual framebuffer at independent attack phases, outside performance windows.
    if(state!="guard")for(int phase=0;phase<4;phase++) {
     holdGreeting=false;activeMove=Moves.All.Single(m=>m.Id==state);holdAttack=true;
     int commandedFrame=Mathf.RoundToInt((phase+.5f)/4*activeMove.Value.Duration);
     for(int i=0;i<views.Length;i++)actorFighters[i].Frame=commandedFrame;
     drive=true;
     var attackSettle=SettlePose(scenario+"-"+state+"-phase"+phase+".png",state,commandedFrame,float.NaN,false);
     while(attackSettle.MoveNext())yield return attackSettle.Current;
     drive=false;
     var frame=ScreenCapture.CaptureScreenshotAsTexture();File.WriteAllBytes(output+"/"+scenario+"-"+state+"-phase"+phase+".png",frame.EncodeToPNG());Destroy(frame);
    }
    File.WriteAllLines(output+"/summary.csv",summary);File.WriteAllLines(output+"/capture-poses.csv",poseRows);
   }
   holdGreeting=false;views=null;animators=null;foreach(var a in actors)Destroy(a);actors.Clear();yield return null;
   var unload=Resources.UnloadUnusedAssets();while(!unload.isDone)yield return null;
  }
  File.WriteAllText(output+"/runtime.json",JsonUtility.ToJson(new RuntimeResult{completed=true,errors=errors,graphics=SystemInfo.graphicsDeviceName,api=SystemInfo.graphicsDeviceType.ToString(),compute=SystemInfo.supportsComputeShaders,unity=Application.unityVersion,width=Screen.width,height=Screen.height},true));
  Application.Quit(errors==0?0:1);
 }
 // Real elapsed-time and actual pose state gate; frame count alone is not a blend-completion oracle.
 IEnumerator SettlePose(string image,string commandedState,int commandFrame,float sourceSeconds,bool native){
  double started=Time.realtimeSinceStartupAsDouble;int stable=0;
  var previousPhase=new float[views.Length];var previousSource=new float[views.Length];
  var bones=actors.Select(a=>a.GetComponentsInChildren<Transform>(true)).ToArray();
  var positions=bones.Select(bs=>new Vector3[bs.Length]).ToArray();
  var rotations=bones.Select(bs=>new Quaternion[bs.Length]).ToArray();
  var scales=bones.Select(bs=>new Vector3[bs.Length]).ToArray();
  int expectedHash=Animator.StringToHash(commandedState);
  while(true){
   yield return eof;
   bool ready=true;
   for(int i=0;i<views.Length;i++){
    var state=animators[i].GetCurrentAnimatorStateInfo(0);var o=overlays[i];
    float elapsed=(float)BlendElapsed.GetValue(views[i]),duration=(float)BlendDuration.GetValue(views[i]);
    ready &= !animators[i].IsInTransition(0)&&state.shortNameHash==expectedHash&&elapsed>=duration;
    ready &= native ? o.ActiveState==commandedState&&o.LastBlendWeight>=1&&Mathf.Abs(o.LastSamplePhase-sourceSeconds)<1e-5f : o.ActiveState==null&&o.LastBlendWeight>=1;
    ready &= Mathf.Abs(state.normalizedTime-previousPhase[i])<1e-6f&&Mathf.Abs(o.LastSamplePhase-previousSource[i])<1e-6f;
    for(int j=0;j<bones[i].Length;j++){
     var t=bones[i][j];ready &= Vector3.Distance(t.localPosition,positions[i][j])<1e-6f&&Quaternion.Angle(t.localRotation,rotations[i][j])<.001f&&Vector3.Distance(t.localScale,scales[i][j])<1e-6f;
     positions[i][j]=t.localPosition;rotations[i][j]=t.localRotation;scales[i][j]=t.localScale;
    }
    previousPhase[i]=state.normalizedTime;previousSource[i]=o.LastSamplePhase;
   }
   stable=ready?stable+1:0;
   double seconds=Time.realtimeSinceStartupAsDouble-started;
   if(seconds>10)throw new Exception("Pose failed to settle: "+image+" overlay="+overlays[0].ActiveState+" weight="+overlays[0].LastBlendWeight);
   if(stable>=6&&seconds>=.5){
    for(int i=0;i<views.Length;i++){
     var st=animators[i].GetCurrentAnimatorStateInfo(0);var o=overlays[i];
     poseRows.Add(string.Join(",",image,i,commandedState,commandFrame,N(sourceSeconds),st.shortNameHash,N(st.normalizedTime),o.ActiveState??"none",N(o.LastSamplePhase),N(o.LastBlendWeight),N((float)BlendElapsed.GetValue(views[i])),N((float)BlendDuration.GetValue(views[i])),N(seconds),stable));
    }
    using(var w=new BinaryWriter(File.Create(output+"/"+image+".local-matrices.bin"))){
     w.Write(bones.Length);foreach(var bs in bones){w.Write(bs.Length);foreach(var t in bs){var m=Matrix4x4.TRS(t.localPosition,t.localRotation,t.localScale);for(int k=0;k<16;k++)w.Write(m[k]);}}
    }
    yield break;
   }
  }
 }
 [Serializable]class RuntimeResult{public bool completed,compute;public int errors,width,height;public string graphics,api,unity;}
 static string N(double x)=>double.IsNaN(x)?"unavailable":x.ToString("R",CultureInfo.InvariantCulture);
 static double Q(double[] xs,double q){if(xs.Length==0)return double.NaN;var a=(double[])xs.Clone();Array.Sort(a);return a[Math.Min(a.Length-1,(int)Math.Ceiling(q*a.Length)-1)];}
}
public sealed class DenseBenchmarkRenderCounter:MonoBehaviour {public int renders;void OnPreCull(){renders++;}}

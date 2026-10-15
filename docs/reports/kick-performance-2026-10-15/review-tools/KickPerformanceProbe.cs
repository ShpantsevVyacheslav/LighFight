using System;using System.Reflection;using System.IO;using System.Linq;using System.Collections;using System.Collections.Generic;using System.Globalization;
using UnityEngine;using UnityEngine.Profiling;using LighFight.Runtime;using LighFight.Core;
public sealed class KickPerformanceProbe:MonoBehaviour {
 public GameObject[] prefabs;public string[] roles;public Camera viewCamera;public string manifest,mode="baseline";
 const int Frames=200;readonly WaitForEndOfFrame eof=new WaitForEndOfFrame();
 struct Sample{public double wall,main,ground,cap,pose,contact,gcMarker;public long alloc;public int steps,coreTick,attackFrame,downTicks,groundPoints,groundInfluences,bodyMode,renders,stateHash;public float phase,blend;public ulong contactHash,poseHash;}
 readonly List<IActorCurveWarmup> leases=new List<IActorCurveWarmup>();
 readonly string[] markers={"Main Thread","LighFight.Grounding.Apply","LighFight.Grounding.Cap","LighFight.Actor.Pose","LighFight.Contact.Sample","GC.Alloc"};Recorder[] rec;
 readonly Dictionary<CandidateFootGrounding.Point[],int> influenceCounts=new Dictionary<CandidateFootGrounding.Point[],int>();
 FighterView a,b;Animator animator;CandidateFootGrounding ground;NativeSupportOverlay overlay;Match match;PhysicalContacts contacts;bool running;int moveIndex=-1,steps,renders;float accumulator;ulong contactHash;int errors;string output;double deadline;
 void OnEnable(){Application.logMessageReceived+=Log;Camera.onPreCull+=OnCamera;}
 void OnDisable(){Application.logMessageReceived-=Log;Camera.onPreCull-=OnCamera;}
 void DisposeWarmups(){foreach(var warm in leases){warm.CancelWarmup();warm.Dispose();}leases.Clear();}
 void OnDestroy(){DisposeWarmups();}
 void OnCamera(Camera c){if(c==viewCamera)renders++;}
 void Log(string m,string st,LogType t){if(t==LogType.Error||t==LogType.Exception||t==LogType.Assert)errors++;}
 void Update(){if(!running)return;accumulator+=Mathf.Min(Time.deltaTime,.1f);steps=0;while(accumulator>=1f/60){accumulator-=1f/60;if(moveIndex>=0&&!match.A.Attack.HasValue&&match.A.DownTicks==0)match.Attack(match.A,moveIndex);match.Step();steps++;}a.PoseTowards(match.A,match.B,Time.deltaTime);b.PoseTowards(match.B,match.A,Time.deltaTime);}
 IEnumerator Start(){output=Path.GetFullPath("KickPerformanceEvidence/"+mode);Directory.CreateDirectory(output);File.WriteAllText(output+"/manifest.json",manifest);deadline=Time.realtimeSinceStartupAsDouble+270;var run=Run();while(true){bool more;try{more=run.MoveNext();if(Time.realtimeSinceStartupAsDouble>deadline)throw new TimeoutException("Kick diagnostic270s deadline");}catch(Exception e){running=false;DisposeWarmups();File.WriteAllText(output+"/failure.txt",e.ToString());File.WriteAllText(output+"/result.json","{\"completed\":false,\"failed\":true}");Application.Quit(1);yield break;}if(!more)yield break;yield return run.Current;}}
 IEnumerator Run(){
  if(!Debug.isDebugBuild||SystemInfo.graphicsDeviceType!=UnityEngine.Rendering.GraphicsDeviceType.Metal)throw new Exception("Development Metal player required");
  Application.targetFrameRate=60;QualitySettings.vSyncCount=0;QualitySettings.skinWeights=SkinWeights.Unlimited;Application.runInBackground=true;
  rec=markers.Select(Recorder.Get).ToArray();foreach(var r in rec)r.enabled=true;
  File.WriteAllLines(output+"/marker-availability.csv",markers.Select((n,i)=>n+","+rec[i].isValid));
  var summary=new List<string>{"role,stage,frames,wallMeanMs,wallP95Ms,wallWorstMs,mainMeanMs,groundMeanMs,capMeanMs,poseMeanMs,contactMeanMs,threadAllocBytes,allocatedMemoryBytes,processWorkingSetBytes,minimumEvalBefore,minimumEvalAfter,minimumEvalDelta"};
  for(int role=0;role<prefabs.Length;role++){
   var ga=Instantiate(prefabs[role]);var gb=Instantiate(prefabs[0]);a=ga.GetComponent<FighterView>();b=gb.GetComponent<FighterView>();animator=a.GetComponentInChildren<Animator>();ground=a.GetComponent<CandidateFootGrounding>();overlay=a.GetComponent<NativeSupportOverlay>();
   foreach(var g in new[]{ga,gb})foreach(var warmup in g.GetComponentsInChildren<MonoBehaviour>(true).OfType<IActorCurveWarmup>()){
    leases.Add(warmup);var step=warmup.Warmup();try{while(step.MoveNext())yield return step.Current;}finally{(step as IDisposable)?.Dispose();}
   }
   if(!overlay)throw new Exception("Original native hook missing");
   if(!a.UsesNativeMotion||!b.UsesNativeMotion||!animator.avatar.isValid||!animator.avatar.isHuman||!ground)throw new Exception("Shipping native Human contract missing");
   foreach(var points in new[]{ground.shoePoints,overlay.supportPoints,overlay.defeatBodyPoints})if(points!=null&&!influenceCounts.ContainsKey(points))influenceCounts.Add(points,points.Sum(p=>p.influences.Length));
   foreach(string stage in new[]{"idle","frontKick","jumpKick","fall-getup"}){
    running=false;moveIndex=stage=="frontKick"||stage=="jumpKick"?Array.FindIndex(Moves.All,m=>m.Id==stage):-1;
    match=new Match(planar:true);contacts=new PhysicalContacts(match,a,b);var original=match.BeforeContact;
    match.BeforeContact=m=>{original(m);contactHash=HashPose(a);};
    match.A.Guard=stage=="idle";accumulator=0;running=true;
    // Warm same state for real1.0s, excludes first-use caches. Fall window is triggered only AFTER warmup.
    double warmUntil=Time.realtimeSinceStartupAsDouble+1;while(Time.realtimeSinceStartupAsDouble<warmUntil)yield return eof;
    if(stage=="fall-getup"){match.A.Attack=null;match.A.DownTicks=Match.KnockdownTicks;}
    long minimumBefore=MinimumCount(ground);
    var samples=new Sample[Frames];double previous=Time.realtimeSinceStartupAsDouble;long allocatedBefore=GC.GetAllocatedBytesForCurrentThread();int before=renders;
    for(int i=0;i<Frames;i++){
     yield return eof;double now=Time.realtimeSinceStartupAsDouble;var state=animator.GetCurrentAnimatorStateInfo(0);var points=ground.shoePoints;
     int influences;if(!influenceCounts.TryGetValue(points,out influences))throw new Exception("Uncatalogued support array");
     samples[i]=new Sample{wall=(now-previous)*1000,main=rec[0].elapsedNanoseconds/1e6,ground=rec[1].elapsedNanoseconds/1e6,cap=rec[2].elapsedNanoseconds/1e6,pose=rec[3].elapsedNanoseconds/1e6,contact=rec[4].elapsedNanoseconds/1e6,gcMarker=rec[5].elapsedNanoseconds/1e6,alloc=GC.GetAllocatedBytesForCurrentThread()-allocatedBefore,steps=steps,coreTick=match.Tick,attackFrame=match.A.Frame,downTicks=match.A.DownTicks,groundPoints=points.Length,groundInfluences=influences,bodyMode=ReferenceEquals(points,overlay.defeatBodyPoints)?1:0,renders=renders-before,stateHash=state.shortNameHash,phase=state.normalizedTime,blend=overlay.LastBlendWeight,contactHash=contactHash,poseHash=HashPose(a)};
     previous=now;before=renders;
    }
    running=false;long minimumAfter=MinimumCount(ground);
    if(samples.Any(s=>s.renders!=1))throw new Exception("Expected exactly one automatic render per sample");
    if(stage=="fall-getup"&&(!samples.Any(s=>s.bodyMode==1)||samples[Frames-1].downTicks!=0||samples[Frames-1].bodyMode!=0))throw new Exception("Forced Fall/GetUp lifecycle missing/not returned");
    var rows=new List<string>{"frame,wallMs,mainMs,groundMs,capMs,poseMs,contactMs,gcMarkerMs,cumulativeThreadAllocBytes,coreSteps,coreTick,attackFrame,downTicks,supportPoints,influences,bodyMode,cameraRenders,animatorState,animatorPhase,overlayBlend,contactHash,EOFHash"};
    for(int i=0;i<Frames;i++){var s=samples[i];rows.Add(string.Join(",",i,N(s.wall),N(s.main),N(s.ground),N(s.cap),N(s.pose),N(s.contact),N(s.gcMarker),s.alloc,s.steps,s.coreTick,s.attackFrame,s.downTicks,s.groundPoints,s.groundInfluences,s.bodyMode,s.renders,s.stateHash,N(s.phase),N(s.blend),s.contactHash,s.poseHash));}
    string name=roles[role]+"-"+stage;File.WriteAllLines(output+"/"+name+".csv",rows);
    summary.Add(string.Join(",",roles[role],stage,Frames,N(samples.Average(s=>s.wall)),N(Q(samples.Select(s=>s.wall).ToArray(),.95)),N(samples.Max(s=>s.wall)),N(samples.Average(s=>s.main)),N(samples.Average(s=>s.ground)),N(samples.Average(s=>s.cap)),N(samples.Average(s=>s.pose)),N(samples.Average(s=>s.contact)),samples[Frames-1].alloc,Profiler.GetTotalAllocatedMemoryLong(),System.Diagnostics.Process.GetCurrentProcess().WorkingSet64,minimumBefore,minimumAfter,minimumBefore>=0&&minimumAfter>=0?minimumAfter-minimumBefore:-1));File.WriteAllLines(output+"/summary.csv",summary);
    var image=CaptureBackbuffer();File.WriteAllBytes(output+"/"+name+".png",image.EncodeToPNG());Destroy(image);
   }
   DisposeWarmups();Destroy(ga);Destroy(gb);yield return null;influenceCounts.Clear();
  }
  File.WriteAllText(output+"/result.json","{\"completed\":true,\"errors\":"+errors+",\"failed\":"+(errors>0?"true":"false")+",\"frames\":2400,\"target\":60}");Application.Quit(errors>0?1:0);
 }
 static Texture2D CaptureBackbuffer(){var old=RenderTexture.active;Texture2D image=null;try{RenderTexture.active=null;image=new Texture2D(Screen.width,Screen.height,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,Screen.width,Screen.height),0,0,false);image.Apply(false,false);return image;}catch{if(image)UnityEngine.Object.Destroy(image);throw;}finally{RenderTexture.active=old;}}
 static long MinimumCount(CandidateFootGrounding g){var p=typeof(CandidateFootGrounding).GetProperty("MinimumEvaluationCount",BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic);return p==null?-1:Convert.ToInt64(p.GetValue(g));}
 static readonly string[] HashBones={"Hips","Head","LeftHand","RightHand","LeftToeBase","RightToeBase"};
 static ulong HashPose(FighterView view){ulong h=14695981039346656037UL;for(int i=0;i<HashBones.Length;i++){var p=view.BonePoint(HashBones[i],Vector3.zero);h=Mix(h,p.x);h=Mix(h,p.y);h=Mix(h,p.z);}return h;}
 static ulong Mix(ulong h,float f)=>unchecked((h^(uint)BitConverter.SingleToInt32Bits(f))*1099511628211UL);
 static string N(double d)=>d.ToString("R",CultureInfo.InvariantCulture);static double Q(double[] x,double q){Array.Sort(x);return x[(int)Math.Ceiling(x.Length*q)-1];}
}

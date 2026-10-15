using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using LighFight.Core;
using LighFight.Runtime;
// Isolated-player proof only. Does not advance Animator or write any bone transforms.
public sealed class CombatStanceEOFProbe:MonoBehaviour {
 [Serializable] public sealed class ActorPair{public string id;public GameObject original,candidate;}
 public ActorPair[] roster=new ActorPair[0];
 public GameObject original,candidate;
 string actorId="Island";bool rosterMode;
 public string output="CombatStanceEOFEvidence";
 readonly List<IActorCurveWarmup> leases=new List<IActorCurveWarmup>();
 [Serializable] public sealed class Bone {public string path;public Vector3 position;public Quaternion rotation;}
 [Serializable] public sealed class Pose {public int state;public float phase;public int armsState;public float armsPhase,armsWeight;public Bone[] bones;}
 [Serializable] public sealed class Frame {public string actorId,variant,scenario;public float gap;public Vector3 cameraPosition;public float cameraFov;public int sampleFrame,preFrame,eofFrame;public int tick,attackFrame,damage;public bool crouch,guard;public Pose before,after,defenderBefore,defenderAfter;public float maxPosition,maxRotation,defenderMaxPosition,defenderMaxRotation;}
 static string PathOf(Transform t,Transform root){return t==root?"":PathOf(t.parent,root)+(t.parent==root?"":"/")+t.name;}
 static Pose Capture(FighterView view){var a=view.GetComponentInChildren<Animator>();var s=a.GetCurrentAnimatorStateInfo(0);int layer=a.GetLayerIndex("Review independent source arms");var arms=layer>=0?a.GetCurrentAnimatorStateInfo(layer):default(AnimatorStateInfo);return new Pose{state=s.fullPathHash,phase=s.normalizedTime,armsState=arms.fullPathHash,armsPhase=arms.normalizedTime,armsWeight=layer>=0?a.GetLayerWeight(layer):0,bones=a.GetComponentsInChildren<Transform>().Select(t=>new Bone{path=PathOf(t,a.transform),position=t.position,rotation=t.rotation}).ToArray()};}
 static string Arg(string key,string value){var args=Environment.GetCommandLineArgs();int i=Array.IndexOf(args,key);return i>=0&&i+1<args.Length?args[i+1]:value;}
 float started;bool complete,failed;
 IEnumerator Start(){
  Application.runInBackground=true;rosterMode=roster!=null&&roster.Length>0;started=Time.realtimeSinceStartup;Application.targetFrameRate=60;QualitySettings.vSyncCount=0;
  output=Arg("-stanceEvidence",output);Exception failure=null;
  try{if(Directory.Exists(output)&&Directory.GetFiles(output).Length>0)throw new InvalidOperationException("Use a fresh empty evidence directory");Directory.CreateDirectory(output);}catch(Exception e){failure=e;}
  if(failure==null){
   // Drive nested warmup enumerators here too: their exceptions must terminate the player.
   var stack=new Stack<IEnumerator>();stack.Push(Run());
   while(stack.Count>0){object next=null;bool more=false;
    try{more=stack.Peek().MoveNext();if(more)next=stack.Peek().Current;else{(stack.Pop() as IDisposable)?.Dispose();}}
    catch(Exception e){failure=e;}
    if(failure!=null)break;
    if(!more)continue;
    if(next is IEnumerator nested){stack.Push(nested);continue;}
    yield return next;
   }
   while(stack.Count>0){try{(stack.Pop() as IDisposable)?.Dispose();}catch(Exception e){if(failure==null)failure=e;}}
  }
  if(failure!=null){failed=true;Debug.LogException(failure);try{File.WriteAllText(Path.Combine(output,"PLAYER-ERROR.txt"),failure.ToString());}catch{}yield return new WaitForEndOfFrame();SaveErrorScreen("exception");complete=true;Application.Quit(1);yield break;}
  complete=true;bool red=File.Exists(Path.Combine(output,"EOF-RED.txt"));Debug.Log("STANCE_EOF_RESULT "+(red?"RED":"NUMERIC_PARITY_COMPLETE_VISUAL_PENDING"));Application.Quit(red?1:0);
 }
 // Only called after WaitForEndOfFrame: read the actually presented backbuffer, no rerender.
 static Texture2D CaptureBackbuffer(){
  var previous=RenderTexture.active;Texture2D image=null;
  try{RenderTexture.active=null;image=new Texture2D(Screen.width,Screen.height,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,Screen.width,Screen.height),0,0,false);image.Apply(false,false);return image;}
  catch{if(image)UnityEngine.Object.Destroy(image);throw;}
  finally{RenderTexture.active=previous;}
 }
 void SaveErrorScreen(string reason){try{var image=CaptureBackbuffer();File.WriteAllBytes(Path.Combine(output,"PLAYER-ERROR-"+reason+".png"),image.EncodeToPNG());Destroy(image);}catch(Exception e){Debug.LogWarning("Error capture failed: "+e.Message);}}
 void Update(){if(!complete&&!failed&&Time.realtimeSinceStartup-started>(rosterMode?570:240)){failed=true;Debug.LogError("STANCE_EOF_RESULT WATCHDOG_RED timeout");try{File.WriteAllText(Path.Combine(output,"WATCHDOG-RED.txt"),"Player exceeded bounded real-time watchdog");}catch{}StartCoroutine(WatchdogQuit());}}
 IEnumerator WatchdogQuit(){yield return new WaitForEndOfFrame();SaveErrorScreen("watchdog");complete=true;Application.Quit(1);}

 IEnumerator Run(){
  var actors=rosterMode?roster:new[]{new ActorPair{id="Island",original=original,candidate=candidate}};
  foreach(var pair in actors){actorId=pair.id;original=pair.original;candidate=pair.candidate;if(string.IsNullOrEmpty(actorId)||!original||!candidate)throw new InvalidOperationException("Missing actor pair");
  foreach(bool changed in new[]{false,true})foreach(string scenario in Arg("-stanceContactOnly","0")=="1"?new[]{"CrouchUppercut"}:new[]{"Crouch","WalkBack","GuardBack","CrouchUppercut"})foreach(float gap in scenario=="CrouchUppercut"?(rosterMode?new[]{.9f,3f}:new[]{.65f,.9f,1.05f,1.2f,3f}):new[]{1.2f}){
   var proofCamera=UnityEngine.Object.FindFirstObjectByType<Camera>();if(!proofCamera)throw new InvalidOperationException("Proof camera missing");proofCamera.transform.position=new Vector3(0,1.3f,Arg("-stanceCloseCamera","0")=="1"&&scenario!="WalkBack"&&scenario!="GuardBack"&&gap<=1.2f?-3.8f:-5.5f);proofCamera.transform.LookAt(new Vector3(0,.95f,0));
   var left=Instantiate(changed?candidate:original);var right=Instantiate(original);var a=left.GetComponent<FighterView>();var b=right.GetComponent<FighterView>();
   foreach(var actor in new[]{left,right})foreach(var warm in actor.GetComponentsInChildren<MonoBehaviour>(true).OfType<IActorCurveWarmup>()){leases.Add(warm);yield return warm.Warmup();if(!warm.IsWarm)throw new InvalidOperationException("Native curve warmup failed");}
   var m=new Match(true);m.A.X=-gap/2;m.B.X=gap/2;m.A.Crouch=scenario=="Crouch"||scenario=="CrouchUppercut";m.A.Guard=scenario=="GuardBack";var physical=new PhysicalContacts(m,a,b);
   var diagnostic=new ContactDiagnostic(physical);var originalValidator=m.ContactValidator;
   m.ContactValidator=(attacker,defender)=>{
    bool result=originalValidator(attacker,defender); // preserve original call, result and side effects exactly
    if(result){var view=attacker==m.A?a:b;var target=attacker==m.A?b:a;var record=diagnostic.Record(m,attacker,view,target,actorId,changed?"candidate":"original",scenario,gap);File.AppendAllText(Path.Combine(output,"positive-contacts.jsonl"),JsonUtility.ToJson(record)+"\n");}
    return result;
   };
   // Establish genuine runtime blend entry before committing an attack.
   for(int t=0;t<90;t++){
    // Previous iteration resumed inside EOF. Return to next Update before sampling physics.
    yield return null;
    int sampleFrame=Time.frameCount;
    if(t==18&&scenario=="CrouchUppercut"&&!m.Attack(m.A,7))throw new InvalidOperationException("Uppercut rejected");
    for(int sub=0;sub<3;sub++){
     if(t<18&&(scenario=="WalkBack"||scenario=="GuardBack"))m.Walk(m.A,-1);
     // PhysicalContacts.BeforeContact owns canonical tick poses; do not add render evaluation per tick.
     m.Step();
    }
    // One render-pose evaluation after the3coreticks, as Prototype does after its accumulator loop.
    // Total native elapsed time stays3/60seconds per render; final canonical sample is diagnostic.
    a.PoseTowards(m.A,m.B,3f/60);b.PoseTowards(m.B,m.A,3f/60);
    // Canonical final contact pose in Update -> same-frame engine LateUpdate -> screen EOF.
    a.SampleContactPose(m.A,m.B);b.SampleContactPose(m.B,m.A);int preFrame=Time.frameCount;if(preFrame!=sampleFrame)throw new InvalidOperationException("Sample/pre crossed Unity frames");var pre=Capture(a);if(changed)AssertArms(a,m.A,pre);var defenderPre=Capture(b);yield return new WaitForEndOfFrame();int eofFrame=Time.frameCount;if(eofFrame!=sampleFrame)throw new InvalidOperationException("Contact/EOF frame mismatch sample="+sampleFrame+" pre="+preFrame+" eof="+eofFrame);var post=Capture(a);if(changed)AssertArms(a,m.A,post);var defenderPost=Capture(b);
    float mp=0,mr=0;for(int i=0;i<pre.bones.Length;i++){if(pre.bones[i].path!=post.bones[i].path)throw new InvalidOperationException("Rig changed");mp=Mathf.Max(mp,Vector3.Distance(pre.bones[i].position,post.bones[i].position));mr=Mathf.Max(mr,Quaternion.Angle(pre.bones[i].rotation,post.bones[i].rotation));}
    float dp=0,dr=0;for(int i=0;i<defenderPre.bones.Length;i++){dp=Mathf.Max(dp,Vector3.Distance(defenderPre.bones[i].position,defenderPost.bones[i].position));dr=Mathf.Max(dr,Quaternion.Angle(defenderPre.bones[i].rotation,defenderPost.bones[i].rotation));}
    var f=new Frame{sampleFrame=sampleFrame,preFrame=preFrame,eofFrame=eofFrame,actorId=actorId,variant=changed?"candidate":"original",scenario=scenario,gap=gap,cameraPosition=proofCamera.transform.position,cameraFov=proofCamera.fieldOfView,tick=m.Tick,attackFrame=m.A.Frame,damage=150-m.B.Health,crouch=m.A.Crouch,guard=m.A.Guard,before=pre,after=post,defenderBefore=defenderPre,defenderAfter=defenderPost,maxPosition=mp,maxRotation=mr,defenderMaxPosition=dp,defenderMaxRotation=dr};
    string id=actorId+"-"+f.variant+"-"+scenario+"-"+gap.ToString("0.00",System.Globalization.CultureInfo.InvariantCulture)+"-"+t.ToString("D3");File.WriteAllText(Path.Combine(output,id+".json"),JsonUtility.ToJson(f));
    if(rosterMode?(t==0||t==8||t==17||t==25||t==27||t==40||t==89):(t==0||t==4||t==8||t==12||t==17||t==18||t==25||t==26||t==27||t==30||t==40||t==60||t==89)){var image=CaptureBackbuffer();File.WriteAllBytes(Path.Combine(output,id+".png"),image.EncodeToPNG());Destroy(image);}
    if(mp>.00001f||mr>.01f||dp>.00001f||dr>.01f||pre.state!=post.state||Mathf.Abs(pre.phase-post.phase)>.00001f||pre.armsState!=post.armsState||Mathf.Abs(pre.armsPhase-post.armsPhase)>.00001f||defenderPre.state!=defenderPost.state||Mathf.Abs(defenderPre.phase-defenderPost.phase)>.00001f)File.AppendAllText(Path.Combine(output,"EOF-RED.txt"),id+" p="+mp+" r="+mr+"\n");
   }
   int totalDamage=150-m.B.Health;string caseResult=actorId+"\t"+(changed?"candidate":"original")+"\t"+scenario+"\t"+gap.ToString(System.Globalization.CultureInfo.InvariantCulture)+"\t"+totalDamage;File.AppendAllText(Path.Combine(output,"case-results.tsv"),caseResult+"\n");Debug.Log("STANCE_EOF_CASE "+caseResult);
   if(scenario=="CrouchUppercut"&&gap>=3&&totalDamage!=0)throw new InvalidOperationException("Far uppercut damaged target");
   foreach(var warm in leases){warm.Dispose();}leases.Clear();Destroy(left);Destroy(right);yield return null;
  }
  }
  File.WriteAllText(Path.Combine(output,"COMPLETED-NOT-VISUALLY-ACCEPTED.txt"),"Screen EOF and full rig world pose captured; inspect EOF-RED and contact/visual gates. No automatic quality acceptance.");
 }
 void AssertArms(FighterView view,Fighter fighter,Pose pose){
  const string layer="Review independent source arms";bool strike=fighter.Crouch&&fighter.Attack.HasValue&&fighter.Attack.Value.Id=="uppercut";bool guard=pose.state==Animator.StringToHash("Base Layer.Crouch")||pose.state==Animator.StringToHash("Base Layer.WalkBack");if(!strike&&!guard)return;
  if(strike&&(pose.state!=Animator.StringToHash("Base Layer.CrouchUppercut")||Mathf.Abs(pose.phase-.3f)>.00001f))throw new InvalidOperationException("Crouch uppercut base state missing");
  float expected=.2f;if(strike){var contact=Array.Find(view.CopyContactPoses(),x=>x.moveId=="uppercut");if(contact.phase<=0)throw new InvalidOperationException("Uppercut contact phase absent");float end=contact.end>contact.start?contact.end:1;float normalized=(contact.phase-contact.start)/(end-contact.start);expected=Mathf.Lerp(contact.start,end,MotionTiming.Phase(fighter.Attack.Value,fighter.Frame,normalized));}
  string prefix=Arg("-stanceMutateWrongArmsPath","0")=="1"?"BROKEN_PATH":layer;int state=Animator.StringToHash(prefix+"."+(strike?"SourceUppercut":"SourceGuard"));if(pose.armsState!=state||Mathf.Abs(pose.armsPhase-expected)>.00001f||Mathf.Abs(pose.armsWeight-1)>.00001f)throw new InvalidOperationException("ARMS STATE/PHASE RED coreFrame="+fighter.Frame+" requested="+state+"/"+expected+" actual="+pose.armsState+"/"+pose.armsPhase+" weight="+pose.armsWeight);
 }
 [Serializable]public sealed class CapsuleEvidence {public string semantic;public Vector3 start,end;public float bodyRadius,strikeRadius,distance,combinedRadius,signedGap;public bool intersects;}
 [Serializable]public sealed class ContactEvidence {public string actorId,variant,scenario,moveId,strikeBone,nearestSemantic;public int tick,attackFrame,strikeSide;public float initialGap;public Vector3 strikeBefore,strikeNow;public CapsuleEvidence[] capsules;public Pose attacker,defender;}
 // QA-only reflection is resolved once per match. No production code or collider is modified.
 sealed class ContactDiagnostic {
  readonly object first,second;readonly Dictionary<string,FieldInfo> fields;
  public ContactDiagnostic(PhysicalContacts physical){var flags=BindingFlags.Instance|BindingFlags.NonPublic;first=typeof(PhysicalContacts).GetField("a",flags).GetValue(physical);second=typeof(PhysicalContacts).GetField("b",flags).GetValue(physical);fields=first.GetType().GetFields(BindingFlags.Instance|BindingFlags.Public).ToDictionary(f=>f.Name);foreach(string required in new[]{"now","before","head","chest","hips","leftKnee","rightKnee","leftFoot","rightFoot"})if(!fields.ContainsKey(required))throw new InvalidOperationException("PhysicalContacts diagnostic schema changed: "+required);}
  Vector3 V(object body,string name){return (Vector3)fields[name].GetValue(body);}
  public ContactEvidence Record(Match m,Fighter attacker,FighterView view,FighterView target,string actorId,string variant,string scenario,float gap){
   if(!attacker.Attack.HasValue||attacker.Attack.Value.Id!="uppercut")throw new InvalidOperationException("Diagnostic radius/selection scoped to original uppercut only");
   bool isA=attacker==m.A;object strike=isA?first:second,body=isA?second:first;int side=view.NativeStrikeSide("uppercut");int index=side>0?side-1:1;if(index<0||index>1)throw new InvalidOperationException("Unexpected uppercut hand side");var before=(Vector3[])fields["before"].GetValue(strike);var now=(Vector3[])fields["now"].GetValue(strike);var from=before[index];var to=now[index];var list=new List<CapsuleEvidence>();
   Action<string,Vector3,Vector3,float> add=(name,c,d,r)=>{float distance=Mathf.Sqrt(HitGeometry.SegmentDistanceSquared(from.x,from.y,from.z,to.x,to.y,to.z,c.x,c.y,c.z,d.x,d.y,d.z));float total=.095f+r;list.Add(new CapsuleEvidence{semantic=name,start=c,end=d,bodyRadius=r,strikeRadius=.095f,distance=distance,combinedRadius=total,signedGap=distance-total,intersects=distance*distance<=total*total});};
   add("head",V(body,"head"),V(body,"head"),.14f);add("torso",V(body,"hips"),V(body,"chest"),.20f);add("left-thigh",V(body,"hips"),V(body,"leftKnee"),.115f);add("right-thigh",V(body,"hips"),V(body,"rightKnee"),.115f);add("left-shin",V(body,"leftKnee"),V(body,"leftFoot"),.09f);add("right-shin",V(body,"rightKnee"),V(body,"rightFoot"),.09f);
   if(!list.Any(x=>x.intersects))throw new InvalidOperationException("Positive original validator not reproduced by diagnostic capsule schema");
   return new ContactEvidence{actorId=actorId,variant=variant,scenario=scenario,initialGap=gap,moveId="uppercut",tick=m.Tick,attackFrame=attacker.Frame,strikeSide=side,strikeBone=index==0?"LeftHand knuckle center":"RightHand knuckle center",strikeBefore=from,strikeNow=to,capsules=list.ToArray(),nearestSemantic=list.OrderBy(x=>x.signedGap).First().semantic,attacker=Capture(view),defender=Capture(target)};
  }
 }
 void OnDestroy(){foreach(var warm in leases){warm.CancelWarmup();warm.Dispose();}leases.Clear();}
}

using System;using System.Collections;using System.Collections.Generic;using UnityEngine;
// Original Generic provider curves, scoped to support states. Runs after fingers, before shoe grounding.
[DefaultExecutionOrder(250)]
public sealed class NativeSupportOverlay:MonoBehaviour,LighFight.Runtime.IContactPoseHook,LighFight.Runtime.IActorCurveWarmup {
 [Serializable]public sealed class Entry {public string state,sourceName;public AnimationClip original;public NativeSupportPoseData pose;[NonSerialized]public Transform[] targets;public bool loop,held;public float heldPhase=.99f;}
 public Animator animator;public CandidateFootGrounding grounding;public Entry[] entries=new Entry[0];
 public Vector3 hipBindAnchor;public bool hasHipBindAnchor;
 public CandidateFootGrounding.Point[] supportPoints,defeatBodyPoints;
 public float baseMaxCorrection=.18f,nativeMaxCorrection=.18f;
 NativeSupportPoseData activePose;void OnDisable(){InvalidateContactPose();if(activePose){activePose.Release();activePose=null;}}
 readonly List<IDisposable> residentCurveLeases=new List<IDisposable>();
 List<IDisposable> pendingCurveLeases;int curveWarmupGeneration;
 public bool IsWarm{get;private set;}
 public IEnumerator Warmup(){
  if(IsWarm)yield break;
  CancelWarmup();int generation=curveWarmupGeneration;
  var pending=new List<IDisposable>();pendingCurveLeases=pending;bool complete=false;
  try{
   var unique=new HashSet<NativeSupportPoseData>();
   foreach(var entry in entries){
    if(!entry.pose)throw new InvalidOperationException("Original provider curve data required");
    if(!unique.Add(entry.pose))continue;
    yield return null;
    if(!this||generation!=curveWarmupGeneration)yield break;
    pending.Add(entry.pose.PinAndPrewarm());
   }
   foreach(var entry in entries)if(entry.targets==null)entry.targets=entry.pose.Bind(animator.transform);
   if(!this||generation!=curveWarmupGeneration)yield break;
   residentCurveLeases.AddRange(pending);pending.Clear();IsWarm=true;complete=true;
  }finally{
   if(!complete)foreach(var lease in pending)lease.Dispose();pending.Clear();
   if(ReferenceEquals(pendingCurveLeases,pending))pendingCurveLeases=null;
  }
 }
 public void CancelWarmup(){curveWarmupGeneration++;if(pendingCurveLeases!=null){foreach(var lease in pendingCurveLeases)lease.Dispose();pendingCurveLeases.Clear();pendingCurveLeases=null;}}
 void OnDestroy(){Dispose();}
 public void Dispose(){CancelWarmup();if(activePose){activePose.Release();activePose=null;}foreach(var lease in residentCurveLeases)lease.Dispose();residentCurveLeases.Clear();IsWarm=false;}
 Matrix4x4[] groundMatrices;Transform[] bones;Vector3[] previousPositions,startPositions;Quaternion[] previousRotations,startRotations;Transform hip;Vector3 hipRest;string previousState;float previousPhase,blendElapsed;bool havePrevious,returning,returningFromDefeat,blendingFromDefeat,blendingFromRecovery,previousPoseContainsRecovery;LighFight.Runtime.FighterView fighterView;
 public float LastSamplePhase{get;private set;}public float LastBlendWeight{get;private set;}public string ActiveState{get;private set;}
 public void Initialize(){fighterView=GetComponent<LighFight.Runtime.FighterView>();bones=animator.GetComponentsInChildren<Transform>();previousPositions=new Vector3[bones.Length];previousRotations=new Quaternion[bones.Length];startPositions=new Vector3[bones.Length];startRotations=new Quaternion[bones.Length];hip=Array.Find(bones,t=>t.name.Substring(t.name.LastIndexOf(':')+1)=="Hips");if(!hip)throw new InvalidOperationException("Native Hips missing");if(!hasHipBindAnchor){hipBindAnchor=animator.transform.InverseTransformPoint(hip.position);hasHipBindAnchor=true;}hipRest=hipBindAnchor;foreach(var e in entries)if(!e.pose)throw new InvalidOperationException("Original provider curve data required");SavePrevious();}
 void SavePrevious(){for(int i=0;i<bones.Length;i++){previousPositions[i]=bones[i].localPosition;previousRotations[i]=bones[i].localRotation;}havePrevious=true;}
 public float Phase(Entry entry,float seconds){if(entry.held)return entry.heldPhase;float n=seconds/Mathf.Max(.001f,entry.pose?entry.pose.duration:entry.original.length);return entry.loop?Mathf.Repeat(n,1):Mathf.Clamp(n,0,.999999f);}
 public bool Preview(string state,float seconds){var e=Array.Find(entries,x=>x.state==state);if(e==null)return false;animator.Play("Base Layer."+state,0,Phase(e,seconds));animator.Update(0);return true;}
 int contactFrame=-1,advancedFrame=-1,contactStateHash;float contactCarrierPhase,contactSourcePhase,contactBlendWeight;string contactState;bool contactRecoveryInPose;Vector3[] contactPositions,contactScales;Quaternion[] contactRotations;
 public void InvalidateContactPose(){contactFrame=-1;}
 public bool TryGetContactPhase(int stateHash,out float phase){phase=contactCarrierPhase;return isActiveAndEnabled&&Application.isPlaying&&contactFrame==Time.frameCount&&stateHash==contactStateHash;}
 void EvaluateOnce(AnimatorStateInfo state){Entry entry=Array.Find(entries,e=>Animator.StringToHash(e.state)==state.shortNameHash);float delta=Application.isPlaying&&advancedFrame!=Time.frameCount?Time.deltaTime:0;EvaluateForReview(entry==null?null:entry.state,state.normalizedTime,delta,true);advancedFrame=Time.frameCount;}
 public void ApplyContactPose(){if(!isActiveAndEnabled)return;var state=animator.GetCurrentAnimatorStateInfo(0);EvaluateOnce(state);if(contactPositions==null||contactPositions.Length!=bones.Length){contactPositions=new Vector3[bones.Length];contactRotations=new Quaternion[bones.Length];contactScales=new Vector3[bones.Length];}for(int i=0;i<bones.Length;i++){contactPositions[i]=bones[i].localPosition;contactRotations[i]=bones[i].localRotation;contactScales[i]=bones[i].localScale;}contactRecoveryInPose=previousPoseContainsRecovery;contactFrame=Time.frameCount;contactStateHash=state.shortNameHash;contactCarrierPhase=state.normalizedTime;contactState=ActiveState;contactSourcePhase=LastSamplePhase;contactBlendWeight=LastBlendWeight;}
 void LateUpdate(){var state=animator.GetCurrentAnimatorStateInfo(0);if(contactFrame==Time.frameCount&&state.shortNameHash==contactStateHash&&Mathf.Abs(state.normalizedTime-contactCarrierPhase)<.00001f){grounding.ResetForSample();for(int i=0;i<bones.Length;i++){if(bones[i]==animator.transform)continue;bones[i].localPosition=contactPositions[i];bones[i].localRotation=contactRotations[i];bones[i].localScale=contactScales[i];}if(contactState!=null||returning){var p=animator.transform.InverseTransformPoint(hip.position);p.x=hipRest.x;p.z=hipRest.z;hip.position=animator.transform.TransformPoint(p);}ActiveState=contactState;LastSamplePhase=contactSourcePhase;LastBlendWeight=contactBlendWeight;SelectGrounding(contactState!=null||returning,contactState=="Defeat"||returning&&returningFromDefeat||blendingFromDefeat&&LastBlendWeight<1,contactRecoveryInPose);return;}EvaluateOnce(state);}
 public void EvaluateForReview(string state,float phase,float delta,bool blend){if(bones==null)Initialize();var entry=Array.Find(entries,e=>e.state==state);ActiveState=entry==null?null:entry.state;var needed=entry==null?null:entry.pose;if(activePose!=needed){if(activePose)activePose.Release();activePose=needed;if(activePose)activePose.Acquire();}
  if(entry==null){if(previousState!=null){for(int i=0;i<bones.Length;i++){startPositions[i]=previousPositions[i];startRotations[i]=previousRotations[i];}returningFromDefeat=previousState=="Defeat";blendingFromRecovery=previousPoseContainsRecovery;returning=true;blendElapsed=0;}if(returning){blendElapsed+=Mathf.Max(0,delta);LastBlendWeight=blend?Mathf.SmoothStep(0,1,Mathf.Clamp01(blendElapsed/.10f)):1;for(int i=0;i<bones.Length;i++){if(bones[i]==animator.transform)continue;bones[i].localRotation=Quaternion.Slerp(startRotations[i],bones[i].localRotation,LastBlendWeight);bones[i].localPosition=Vector3.Lerp(startPositions[i],bones[i].localPosition,LastBlendWeight);}var anchor=animator.transform.InverseTransformPoint(hip.position);anchor.x=hipRest.x;anchor.z=hipRest.z;hip.position=animator.transform.TransformPoint(anchor);if(LastBlendWeight>=1)returning=false;}SelectGrounding(returning,returning&&returningFromDefeat);previousState=null;previousPhase=phase;SavePrevious();return;}
  float samplePhase=entry.held?entry.heldPhase:entry.loop?Mathf.Repeat(phase,1):Mathf.Clamp(phase,0,.999999f);
  LastSamplePhase=samplePhase;
  bool changed=previousState!=state||entry.loop&&samplePhase+.5f<previousPhase;
  if(changed){for(int i=0;i<bones.Length;i++){startPositions[i]=havePrevious?previousPositions[i]:bones[i].localPosition;startRotations[i]=havePrevious?previousRotations[i]:bones[i].localRotation;}blendingFromDefeat=previousState=="Defeat";blendingFromRecovery=previousPoseContainsRecovery;blendElapsed=0;returning=false;}
  grounding.ResetForSample();var rig=animator.transform;var rootPosition=rig.localPosition;var rootRotation=rig.localRotation;var rootScale=rig.localScale;
  if(!entry.pose)throw new InvalidOperationException("Native original curve data required");if(entry.targets==null)entry.targets=entry.pose.Bind(rig);entry.pose.Apply(entry.targets,samplePhase*entry.pose.duration);
  rig.localPosition=rootPosition;rig.localRotation=rootRotation;rig.localScale=rootScale;
  blendElapsed+=Mathf.Max(0,delta);float duration=state=="Walk"?.10f:state=="Fall"||state=="Defeat"?.10f:.28f;LastBlendWeight=blend?Mathf.SmoothStep(0,1,Mathf.Clamp01(blendElapsed/duration)):1;
  if(LastBlendWeight<1)for(int i=0;i<bones.Length;i++){if(bones[i]==rig)continue;bones[i].localRotation=Quaternion.Slerp(startRotations[i],bones[i].localRotation,LastBlendWeight);bones[i].localPosition=Vector3.Lerp(startPositions[i],bones[i].localPosition,LastBlendWeight);}
  // Same late native-root XZ contract as FighterView; vertical provider motion is retained.
  var p=rig.InverseTransformPoint(hip.position);p.x=hipRest.x;p.z=hipRest.z;hip.position=rig.TransformPoint(p);
  previousState=state;previousPhase=samplePhase;SavePrevious();
  SelectGrounding(true,state=="Defeat"||blendingFromDefeat&&LastBlendWeight<1);
 }
 void SelectGrounding(bool native,bool body,bool cachedRecovery=false){
  int recoveryState=animator.GetCurrentAnimatorStateInfo(0).shortNameHash;
  bool sharedRecovery=recoveryState==Animator.StringToHash("Fall")||recoveryState==Animator.StringToHash("GetUp");
  sharedRecovery|=fighterView&&fighterView.RecoveryBodySupportRequired;
  bool recoveryInPose=cachedRecovery||sharedRecovery||blendingFromRecovery&&LastBlendWeight<1;
  native|=recoveryInPose;body|=recoveryInPose;previousPoseContainsRecovery=recoveryInPose;
  if(body&&defeatBodyPoints!=null&&defeatBodyPoints.Length>0)grounding.shoePoints=defeatBodyPoints;else if(supportPoints!=null&&supportPoints.Length>0)grounding.shoePoints=supportPoints;grounding.maxCorrection=baseMaxCorrection;if(!native)return;
  // Native support poses and their blends use the actual original-vertex floor requirement.
  // A cap trained on one entry phase must not clip another entry's valid measured lift.
  grounding.ResetForSample();if(groundMatrices==null||groundMatrices.Length!=grounding.bones.Length)groundMatrices=new Matrix4x4[grounding.bones.Length];for(int i=0;i<groundMatrices.Length;i++)groundMatrices[i]=grounding.bones[i].localToWorldMatrix;float min=float.PositiveInfinity;foreach(var point in grounding.shoePoints){var p=Vector3.zero;foreach(var influence in point.influences)p+=groundMatrices[influence.bone].MultiplyPoint3x4(influence.bindPoint)*influence.weight;min=Mathf.Min(min,p.y);}grounding.maxCorrection=nativeMaxCorrection;if(!float.IsNaN(min)&&!float.IsInfinity(min))grounding.maxCorrection=Mathf.Max(nativeMaxCorrection,Mathf.Max(0,-min)+.001f);
 }
}

using System;using UnityEngine;
// Original Generic provider curves, scoped to support states. Runs after fingers, before shoe grounding.
[DefaultExecutionOrder(250)]
public sealed class NativeSupportOverlay:MonoBehaviour {
 [Serializable]public sealed class Entry {public string state,sourceName;public AnimationClip original;public NativeSupportPoseData pose;[NonSerialized]public Transform[] targets;public bool loop,held;public float heldPhase=.99f;}
 public Animator animator;public CandidateFootGrounding grounding;public Entry[] entries=new Entry[0];
 public Vector3 hipBindAnchor;public bool hasHipBindAnchor;
 public CandidateFootGrounding.Point[] supportPoints,defeatBodyPoints;
 public float baseMaxCorrection=.18f,nativeMaxCorrection=.18f;
 Matrix4x4[] groundMatrices;Transform[] bones;Vector3[] previousPositions,startPositions;Quaternion[] previousRotations,startRotations;Transform hip;Vector3 hipRest;string previousState;float previousPhase,blendElapsed;bool havePrevious,returning,returningFromDefeat,blendingFromDefeat;
 public float LastSamplePhase{get;private set;}public float LastBlendWeight{get;private set;}public string ActiveState{get;private set;}
 public void Initialize(){bones=animator.GetComponentsInChildren<Transform>();previousPositions=new Vector3[bones.Length];previousRotations=new Quaternion[bones.Length];startPositions=new Vector3[bones.Length];startRotations=new Quaternion[bones.Length];hip=Array.Find(bones,t=>t.name.Substring(t.name.LastIndexOf(':')+1)=="Hips");if(!hip)throw new InvalidOperationException("Native Hips missing");if(!hasHipBindAnchor){hipBindAnchor=animator.transform.InverseTransformPoint(hip.position);hasHipBindAnchor=true;}hipRest=hipBindAnchor;foreach(var e in entries)if(!e.pose)throw new InvalidOperationException("Original provider curve data required");SavePrevious();}
 void SavePrevious(){for(int i=0;i<bones.Length;i++){previousPositions[i]=bones[i].localPosition;previousRotations[i]=bones[i].localRotation;}havePrevious=true;}
 public float Phase(Entry entry,float seconds){if(entry.held)return entry.heldPhase;float n=seconds/Mathf.Max(.001f,entry.pose?entry.pose.duration:entry.original.length);return entry.loop?Mathf.Repeat(n,1):Mathf.Clamp(n,0,.999999f);}
 public bool Preview(string state,float seconds){var e=Array.Find(entries,x=>x.state==state);if(e==null)return false;animator.Play("Base Layer."+state,0,Phase(e,seconds));animator.Update(0);return true;}
 void LateUpdate(){var state=animator.GetCurrentAnimatorStateInfo(0);Entry entry=Array.Find(entries,e=>Animator.StringToHash(e.state)==state.shortNameHash);EvaluateForReview(entry==null?null:entry.state,state.normalizedTime,Time.deltaTime,true);}
 public void EvaluateForReview(string state,float phase,float delta,bool blend){if(bones==null)Initialize();var entry=Array.Find(entries,e=>e.state==state);ActiveState=entry==null?null:entry.state;
  if(entry==null){if(previousState!=null){for(int i=0;i<bones.Length;i++){startPositions[i]=previousPositions[i];startRotations[i]=previousRotations[i];}returningFromDefeat=previousState=="Defeat";returning=true;blendElapsed=0;}if(returning){blendElapsed+=Mathf.Max(0,delta);LastBlendWeight=blend?Mathf.SmoothStep(0,1,Mathf.Clamp01(blendElapsed/.10f)):1;for(int i=0;i<bones.Length;i++){if(bones[i]==animator.transform)continue;bones[i].localRotation=Quaternion.Slerp(startRotations[i],bones[i].localRotation,LastBlendWeight);bones[i].localPosition=Vector3.Lerp(startPositions[i],bones[i].localPosition,LastBlendWeight);}var anchor=animator.transform.InverseTransformPoint(hip.position);anchor.x=hipRest.x;anchor.z=hipRest.z;hip.position=animator.transform.TransformPoint(anchor);if(LastBlendWeight>=1)returning=false;}SelectGrounding(returning,returning&&returningFromDefeat);previousState=null;previousPhase=phase;SavePrevious();return;}
  float samplePhase=entry.held?entry.heldPhase:entry.loop?Mathf.Repeat(phase,1):Mathf.Clamp(phase,0,.999999f);
  LastSamplePhase=samplePhase;
  bool changed=previousState!=state||entry.loop&&samplePhase+.5f<previousPhase;
  if(changed){for(int i=0;i<bones.Length;i++){startPositions[i]=havePrevious?previousPositions[i]:bones[i].localPosition;startRotations[i]=havePrevious?previousRotations[i]:bones[i].localRotation;}blendingFromDefeat=previousState=="Defeat";blendElapsed=0;returning=false;}
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
 void SelectGrounding(bool native,bool body){if(body&&defeatBodyPoints!=null&&defeatBodyPoints.Length>0)grounding.shoePoints=defeatBodyPoints;else if(supportPoints!=null&&supportPoints.Length>0)grounding.shoePoints=supportPoints;grounding.maxCorrection=baseMaxCorrection;if(!native)return;
  // Native support poses and their blends use the actual original-vertex floor requirement.
  // A cap trained on one entry phase must not clip another entry's valid measured lift.
  grounding.ResetForSample();if(groundMatrices==null||groundMatrices.Length!=grounding.bones.Length)groundMatrices=new Matrix4x4[grounding.bones.Length];for(int i=0;i<groundMatrices.Length;i++)groundMatrices[i]=grounding.bones[i].localToWorldMatrix;float min=float.PositiveInfinity;foreach(var point in grounding.shoePoints){var p=Vector3.zero;foreach(var influence in point.influences)p+=groundMatrices[influence.bone].MultiplyPoint3x4(influence.bindPoint)*influence.weight;min=Mathf.Min(min,p.y);}grounding.maxCorrection=nativeMaxCorrection;if(!float.IsNaN(min)&&!float.IsInfinity(min))grounding.maxCorrection=Mathf.Max(nativeMaxCorrection,Mathf.Max(0,-min)+.001f);
 }
}

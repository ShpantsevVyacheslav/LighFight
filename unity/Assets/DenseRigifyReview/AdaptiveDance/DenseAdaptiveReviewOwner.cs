using System;
using System.Collections;
using LighFight.Runtime;
using UnityEngine;

// Review actor only. Explicit commands own Dance -> EOF -> neutral hold. No combat hooks.
[DefaultExecutionOrder(275)]
public sealed class DenseAdaptiveReviewOwner : MonoBehaviour,IActorCurveWarmup,IResultMotionSource
{
 public DenseAdaptivePoseBridge bridge;
 public FighterView view;
 public CandidateFootGrounding grounding;
 public float transitionSeconds=.28f;
 public enum PlaybackPhase { Neutral,Entering,Dancing,Endpoint,Returning }
 public PlaybackPhase Phase {get;private set;}
 public double Elapsed {get;private set;}
 public int ExactPoseApplications {get;private set;}
 public bool IsWarm=>bridge&&bridge.IsWarm&&nodes!=null&&!disposed;
 public bool Active=>Phase!=PlaybackPhase.Neutral;
 Transform[] nodes;
 Vector3[] entryP,entryS,returnP,returnS,initialP,initialS;
 Quaternion[] entryQ,returnQ,initialQ;
 double transitionElapsed;
 Animator clockAnimator;float priorAnimatorSpeed;bool priorRootMotion,ownsClock;
 static readonly int DanceState=Animator.StringToHash("Base Layer.Dance");
 int generation,endpointFrame;
 bool disposed;
 public bool TryGetResultDuration(string state,out float duration){duration=state=="Dance"&&bridge?(float)bridge.AuthoredDuration:0;return duration>0;}
 public IEnumerator Warmup()
 {
  if(disposed)throw new ObjectDisposedException(nameof(DenseAdaptiveReviewOwner));
  if(IsWarm)yield break;if(!bridge||!view||!bridge.target)throw new InvalidOperationException("Complete review actor owner required");
  int ticket=++generation;
  if(!bridge.IsWarm){var step=bridge.Warmup();try{while(step.MoveNext()){yield return step.Current;if(ticket!=generation||disposed)yield break;}}finally{(step as IDisposable)?.Dispose();}}
  if(ticket!=generation||disposed)yield break;
  clockAnimator=view.GetComponentInChildren<Animator>(true);if(!clockAnimator||!clockAnimator.HasState(0,DanceState))throw new InvalidOperationException("Explicit tiny Dance clock state required");
  nodes=bridge.target.bones;entryP=new Vector3[nodes.Length];entryS=new Vector3[nodes.Length];entryQ=new Quaternion[nodes.Length];returnP=new Vector3[nodes.Length];returnS=new Vector3[nodes.Length];returnQ=new Quaternion[nodes.Length];initialP=new Vector3[nodes.Length];initialS=new Vector3[nodes.Length];initialQ=new Quaternion[nodes.Length];
 }
 public void PlayDance()
 {
  if(!IsWarm||!isActiveAndEnabled)throw new InvalidOperationException("Visible owner must be warm before playback");
  Capture(initialP,initialQ,initialS);Capture(entryP,entryQ,entryS);
  if(!ownsClock){priorAnimatorSpeed=clockAnimator.speed;priorRootMotion=clockAnimator.applyRootMotion;ownsClock=true;}
  clockAnimator.speed=0;clockAnimator.applyRootMotion=false;
  if(!view.PreviewNativeMotion("Dance",0)){RestoreClock();throw new InvalidOperationException("Full authored tiny Dance clock missing");}
  Elapsed=0;transitionElapsed=0;Phase=PlaybackPhase.Entering;
 }
 public void ReturnToNeutral()
 {
  if(!IsWarm)throw new InvalidOperationException("Cold owner return");
  if(!Active)return;Capture(entryP,entryQ,entryS);
  if(!view.PreviewNativeMotion("Idle",0))throw new InvalidOperationException("Explicit imported-neutral review hold missing");
  Capture(returnP,returnQ,returnS);Restore(entryP,entryQ,entryS);transitionElapsed=0;Phase=PlaybackPhase.Returning;
 }
 void LateUpdate(){AdvanceReviewFrame(Mathf.Max(0,Time.unscaledDeltaTime));}
 public void AdvanceReviewFrame(float delta)
 {
  if(!IsWarm||!Active)return;
  if(Phase==PlaybackPhase.Endpoint){if(Time.frameCount>endpointFrame)ReturnToNeutral();return;}
  if(float.IsNaN(delta)||float.IsInfinity(delta)||delta<0)throw new ArgumentException("Finite nonnegative review frame delta required");transitionElapsed+=delta;
  if(Phase==PlaybackPhase.Returning)
  {
   float weight=BlendWeight();for(int i=0;i<nodes.Length;i++){nodes[i].SetLocalPositionAndRotation(Vector3.Lerp(entryP[i],returnP[i],weight),Quaternion.Slerp(entryQ[i],returnQ[i],weight));nodes[i].localScale=Vector3.Lerp(entryS[i],returnS[i],weight);}
   if(weight>=1){Phase=PlaybackPhase.Neutral;RestoreClock();Restore(returnP,returnQ,returnS);}return;
  }
  Elapsed=Math.Min(bridge.AuthoredDuration,Elapsed+delta);
  clockAnimator.Play(DanceState,0,FighterView.NativePreviewPhase((float)Elapsed,(float)bridge.AuthoredDuration,false));clockAnimator.Update(0);
  bridge.Apply(Elapsed);ExactPoseApplications++;
  float entryWeight=BlendWeight();if(entryWeight<1)for(int i=0;i<nodes.Length;i++){nodes[i].SetLocalPositionAndRotation(Vector3.Lerp(entryP[i],nodes[i].localPosition,entryWeight),Quaternion.Slerp(entryQ[i],nodes[i].localRotation,entryWeight));nodes[i].localScale=Vector3.Lerp(entryS[i],nodes[i].localScale,entryWeight);}else Phase=PlaybackPhase.Dancing;
  if(Elapsed>=bridge.AuthoredDuration){Phase=PlaybackPhase.Endpoint;endpointFrame=Time.frameCount;}
 }
 float BlendWeight(){float t=transitionSeconds<=0?1:Mathf.Clamp01((float)(transitionElapsed/transitionSeconds));return t*t*(3-2*t);}
 void Capture(Vector3[] p,Quaternion[] q,Vector3[] s){for(int i=0;i<nodes.Length;i++){p[i]=nodes[i].localPosition;q[i]=nodes[i].localRotation;s[i]=nodes[i].localScale;}}
 void Restore(Vector3[] p,Quaternion[] q,Vector3[] s){for(int i=0;i<nodes.Length;i++){if(!nodes[i])continue;nodes[i].SetLocalPositionAndRotation(p[i],q[i]);nodes[i].localScale=s[i];}}
 public void CancelWarmup(){generation++;if(bridge)bridge.CancelWarmup();}
 void RestoreClock(){if(!ownsClock)return;if(clockAnimator){clockAnimator.speed=priorAnimatorSpeed;clockAnimator.applyRootMotion=priorRootMotion;}ownsClock=false;}
 void OnDisable(){if(nodes!=null&&Active){RestoreClock();Restore(initialP,initialQ,initialS);Phase=PlaybackPhase.Neutral;if(grounding)grounding.ResetForSample();}if(!IsWarm)CancelWarmup();}
 public void Dispose(){if(disposed)return;RestoreClock();if(nodes!=null&&Active)Restore(initialP,initialQ,initialS);if(grounding)grounding.ResetForSample();disposed=true;CancelWarmup();if(bridge)bridge.Dispose();nodes=null;entryP=null;entryS=null;entryQ=null;returnP=null;returnS=null;returnQ=null;initialP=null;initialS=null;initialQ=null;Phase=PlaybackPhase.Neutral;}
 void OnDestroy(){Dispose();}
}

using System;using System.Collections;using UnityEngine;using LighFight.Runtime;
// Isolated review candidate: exact original Generic curves, no Human retarget solver.
// Source transforms contain no mesh/renderer/Animator. Original target bind data stays intact.
[DefaultExecutionOrder(275)]
public sealed class ExactGenericDanceRetarget:MonoBehaviour,IActorCurveWarmup,IResultMotionSource {
 public NativeSupportPoseData source;
 public Transform sourceRoot;public Animator targetAnimator;public CandidateFootGrounding grounding;
 public Transform[] sourceMapped,targetMapped,targetNodes;
 public Quaternion[] sourceRestRotations,targetCanonicalRotations,targetLocalRotations;
 public Vector3[] targetLocalPositions,targetLocalScales;
 public Quaternion bodyAlignment=Quaternion.identity;
 public Vector3 sourceHipRest,targetHipRest;public float hipHeightScale=1;
 public float nativeCorrectionCap=.3f,inactiveCorrectionCap=.18f;
 public bool clavicleSupport;public float[] supportRestArmElevation,supportRestClavicleElevation;
 public long LastGroundingTicks{get;private set;}public int LastFullShoeScanDelta{get;private set;}
 public long LastEvaluationTicks{get;private set;}public long PoseCacheCaptureCount{get;private set;}public long PoseEvaluationCount{get;private set;}
 Transform[] sourceTargets;Vector3[] previousPositions,blendPositions;Quaternion[] previousRotations,blendRotations;
 FighterView view;IDisposable resident;int generation;bool initialized,wasDance,returning,havePrevious;float blendTime;const float BlendDuration=.28f;
 static readonly int DanceHash=Animator.StringToHash("Dance");
 public bool IsWarm{get;private set;}
 public bool TryGetResultDuration(string state,out float duration){duration=source?source.duration:0;return state=="Dance"&&duration>0;}
 public IEnumerator Warmup(){if(IsWarm)yield break;CancelWarmup();int token=generation;yield return null;if(!this||token!=generation)yield break;var lease=source.PinAndPrewarm();if(!this||token!=generation){lease.Dispose();yield break;}try{Initialize();resident=lease;IsWarm=true;}catch{lease.Dispose();throw;}}
 public void CancelWarmup(){generation++;}
 public void Dispose(){CancelWarmup();resident?.Dispose();resident=null;IsWarm=false;wasDance=false;returning=false;havePrevious=false;RestoreGrounding();}
 void OnDisable(){wasDance=false;returning=false;havePrevious=false;RestoreGrounding();}
 void OnDestroy(){Dispose();}
 void Initialize(){if(initialized)return;if(!source||!sourceRoot||!targetAnimator||!grounding)throw new InvalidOperationException("Exact dance source/rig/grounding missing");int n=targetMapped.Length;if(n==0||sourceMapped.Length!=n||sourceRestRotations.Length!=n||targetCanonicalRotations.Length!=n)throw new InvalidOperationException("Exact dance mapping mismatch");if(clavicleSupport&&(supportRestArmElevation==null||supportRestArmElevation.Length!=2||supportRestClavicleElevation==null||supportRestClavicleElevation.Length!=2||targetMapped[7].parent!=targetMapped[6]||targetMapped[11].parent!=targetMapped[10]))throw new InvalidOperationException("Unsupported clavicle anatomy");n=targetNodes.Length;if(targetLocalPositions.Length!=n||targetLocalRotations.Length!=n||targetLocalScales.Length!=n)throw new InvalidOperationException("Canonical pose mismatch");if(Array.IndexOf(targetNodes,transform)>=0||Array.IndexOf(targetNodes,sourceRoot)>=0)throw new InvalidOperationException("Dance mapping must not contain actor/source world root");view=GetComponent<FighterView>();sourceTargets=source.Bind(sourceRoot);previousPositions=new Vector3[n];previousRotations=new Quaternion[n];blendPositions=new Vector3[n];blendRotations=new Quaternion[n];SavePrevious();initialized=true;}
 void LateUpdate(){if(!IsWarm)return;var state=targetAnimator.GetCurrentAnimatorStateInfo(0);bool dance=state.shortNameHash==DanceHash;
  if(!dance&&!wasDance&&!returning){if(view&&view.FullPreviewMotion)SavePrevious();else havePrevious=false;return;}
  if(dance){if(!wasDance){CaptureBlend();blendTime=0;}blendTime+=Mathf.Max(0,Time.deltaTime);Evaluate(Mathf.Clamp01(state.normalizedTime)*source.duration);Blend(Mathf.SmoothStep(0,1,Mathf.Clamp01(blendTime/BlendDuration)));ConfigureGrounding();returning=false;}
  else if(wasDance||returning){if(wasDance){CaptureBlend();blendTime=0;returning=true;}blendTime+=Mathf.Max(0,Time.deltaTime);Blend(Mathf.SmoothStep(0,1,Mathf.Clamp01(blendTime/BlendDuration)));if(blendTime>=BlendDuration)returning=false;ConfigureGrounding();}
  wasDance=dance;SavePrevious();
 }
 // Explicit no-blend sampling for source/player independent parity probes only.
 public void EvaluateForReview(float seconds){Initialize();Evaluate(Mathf.Clamp(seconds,0,source.duration));ConfigureGrounding();}
 void Evaluate(float seconds){PoseEvaluationCount++;long started=System.Diagnostics.Stopwatch.GetTimestamp();source.Apply(sourceTargets,seconds);for(int n=0;n<targetNodes.Length;n++){targetNodes[n].localPosition=targetLocalPositions[n];targetNodes[n].localRotation=targetLocalRotations[n];targetNodes[n].localScale=targetLocalScales[n];}
  Quaternion actorInverse=Quaternion.Inverse(transform.rotation);Vector3 hip=targetHipRest+bodyAlignment*(transform.InverseTransformPoint(sourceMapped[0].position)-sourceHipRest)*hipHeightScale;hip.x=targetHipRest.x;hip.z=targetHipRest.z;targetMapped[0].position=transform.TransformPoint(hip);
  Quaternion alignmentInverse=Quaternion.Inverse(bodyAlignment);for(int n=0;n<targetMapped.Length;n++){Quaternion sourceRotation=actorInverse*sourceMapped[n].rotation;targetMapped[n].rotation=transform.rotation*bodyAlignment*(sourceRotation*Quaternion.Inverse(sourceRestRotations[n]))*alignmentInverse*targetCanonicalRotations[n];}if(clavicleSupport)ApplyClavicleSupport(seconds);LastEvaluationTicks=System.Diagnostics.Stopwatch.GetTimestamp()-started;
 }
 // Separate authored Sailor correction; original Generic curves remain unchanged.
 // Raise distal clavicle toward torso-up and counterrotate upper arm in world space.
 void ApplyClavicleSupport(float seconds){
  Vector3 up=targetMapped[4].position-targetMapped[3].position;float envelope=Smooth(seconds/.5f)*Smooth((source.duration-seconds)/.5f);
  for(int side=0;side<2;side++){int s=side==0?6:10;var shoulder=targetMapped[s];var arm=targetMapped[s+1];var elbow=targetMapped[s+2];Vector3 clavicle=arm.position-shoulder.position;
   float gap=.25f*Mathf.Max(0,Elevation(elbow.position-arm.position,up)-supportRestArmElevation[side])-(Elevation(clavicle,up)-supportRestClavicleElevation[side]);float degrees=Cap(gap)*envelope;if(degrees<=0)continue;
   Vector3 axis=Vector3.Cross(clavicle,up);if(axis.sqrMagnitude<1e-12f)throw new InvalidOperationException("Degenerate clavicle support axis");Quaternion armWorld=arm.rotation;Quaternion shoulderWorld=Quaternion.AngleAxis(degrees,axis.normalized)*shoulder.rotation;
   shoulder.localRotation=Quaternion.Inverse(shoulder.parent.rotation)*shoulderWorld;arm.localRotation=Quaternion.Inverse(shoulderWorld)*armWorld;
  }
 }
 static float Elevation(Vector3 v,Vector3 up){if(v.sqrMagnitude<1e-12||up.sqrMagnitude<1e-12)throw new InvalidOperationException("Invalid support anatomy");return Mathf.Asin(Mathf.Clamp(Vector3.Dot(v.normalized,up.normalized),-1,1))*Mathf.Rad2Deg;}
 static float Smooth(float x){x=Mathf.Clamp01(x);return x*x*(3-2*x);}
 static float Cap(float x){if(x<=0)return 0;if(x<2){float t=x/2;return 2*t*t*(2-t);}if(x<=13)return x;if(x<15){float t=(x-13)/2;return 13+2*(t+t*t-t*t*t);}return 15;}
 void RestoreGrounding(){if(!grounding)return;grounding.maxCorrection=inactiveCorrectionCap;grounding.InvalidateMinimumCache();grounding.ResetForSample();}
 void ConfigureGrounding(){long started=System.Diagnostics.Stopwatch.GetTimestamp();int before=grounding.MinimumEvaluationCount;grounding.InvalidateMinimumCache();float min=grounding.MeasureMinimum();if(float.IsNaN(min)||float.IsInfinity(min))throw new InvalidOperationException("Nonfinite exact-dance shoe floor");grounding.maxCorrection=Mathf.Max(nativeCorrectionCap,Mathf.Max(0,-min)+.001f);LastFullShoeScanDelta=grounding.MinimumEvaluationCount-before;LastGroundingTicks=System.Diagnostics.Stopwatch.GetTimestamp()-started;}
 void CaptureBlend(){for(int i=0;i<targetNodes.Length;i++){blendPositions[i]=havePrevious?previousPositions[i]:targetNodes[i].localPosition;blendRotations[i]=havePrevious?previousRotations[i]:targetNodes[i].localRotation;}}
 void Blend(float weight){if(weight>=1)return;for(int i=0;i<targetNodes.Length;i++){if(targetNodes[i]==targetAnimator.transform)continue;targetNodes[i].localPosition=Vector3.Lerp(blendPositions[i],targetNodes[i].localPosition,weight);targetNodes[i].localRotation=Quaternion.Slerp(blendRotations[i],targetNodes[i].localRotation,weight);}}
 void SavePrevious(){PoseCacheCaptureCount++;for(int i=0;i<targetNodes.Length;i++){previousPositions[i]=targetNodes[i].localPosition;previousRotations[i]=targetNodes[i].localRotation;}havePrevious=true;}
}

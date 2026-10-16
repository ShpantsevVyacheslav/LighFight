using System;using UnityEngine;
[DefaultExecutionOrder(300)]public sealed class CandidateFootGrounding:MonoBehaviour{
 [Serializable]public struct Influence{public int bone;public Vector3 bindPoint;public float weight;}
 [Serializable]public struct Point{public Influence[] influences;}
 public Transform nativeRig;public Transform[] bones;public Point[] shoePoints;public Vector3 nativeRest;public float maxCorrection=.18f;Vector4[] verticalRows;public float LastCorrection{get;private set;}public float UncorrectedMinimum{get;private set;}
 [SerializeField]Animator signedFloorAnimator;[SerializeField]string[] signedFloorStates=new string[0];[SerializeField]float maxDownCorrection=.16f;int[] signedFloorHashes;
 // Review-only opt-in; no configured actor changes the existing upward-only policy.
 public void ConfigureSignedFloorForReview(Animator animator,string[] states,float cap){signedFloorAnimator=animator;signedFloorStates=states??new string[0];maxDownCorrection=Mathf.Max(0,cap);signedFloorHashes=null;}
 bool SignedFloorActive(){if(!signedFloorAnimator||signedFloorStates==null||signedFloorStates.Length==0)return false;if(signedFloorHashes==null)signedFloorHashes=Array.ConvertAll(signedFloorStates,Animator.StringToHash);return Array.IndexOf(signedFloorHashes,signedFloorAnimator.GetCurrentAnimatorStateInfo(0).shortNameHash)>=0;}
 void Update(){ResetForSample();}void LateUpdate(){ApplyForReview();}
 public void ResetForSample(){if(nativeRig)nativeRig.localPosition=nativeRest;}
 static readonly Unity.Profiling.ProfilerMarker ApplyMarker=new Unity.Profiling.ProfilerMarker("LighFight.Grounding.Apply");
 public void ApplyForReview(){using(ApplyMarker.Auto())ApplyForReviewImpl();}
 public int MinimumEvaluationCount{get;private set;}
 Point[] measuredPoints;bool minimumValid;float measuredMinimum;
 // Support arrays are immutable runtime authoring data; invalidate after in-place authoring edits.
 public void InvalidateMinimumCache(){minimumValid=false;}
 public float MeasureMinimum(){
  ResetForSample();if(!nativeRig||bones==null||shoePoints==null)return float.PositiveInfinity;
  bool same=minimumValid&&ReferenceEquals(measuredPoints,shoePoints);
  if(verticalRows==null||verticalRows.Length!=bones.Length){verticalRows=new Vector4[bones.Length];same=false;}
  for(int i=0;i<bones.Length;i++){var value=bones[i].localToWorldMatrix;var row=new Vector4(value.m10,value.m11,value.m12,value.m13);if(!verticalRows[i].Equals(row))same=false;verticalRows[i]=row;}
  if(same)return measuredMinimum;
  float min=float.PositiveInfinity;
  foreach(var point in shoePoints){float y=0;foreach(var influence in point.influences){var m=verticalRows[influence.bone];var p=influence.bindPoint;y+=(m.x*p.x+m.y*p.y+m.z*p.z+m.w)*influence.weight;}min=Mathf.Min(min,y);}
  MinimumEvaluationCount++;measuredMinimum=min;measuredPoints=shoePoints;minimumValid=true;return min;
 }
 void ApplyForReviewImpl(){if(!nativeRig||bones==null||shoePoints==null)return;float min=MeasureMinimum();UncorrectedMinimum=min;LastCorrection=Mathf.Clamp(-min,SignedFloorActive()?-maxDownCorrection:0,maxCorrection);nativeRig.position+=Vector3.up*LastCorrection;}
}

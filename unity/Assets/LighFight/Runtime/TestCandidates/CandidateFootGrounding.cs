using System;using UnityEngine;
[DefaultExecutionOrder(300)]public sealed class CandidateFootGrounding:MonoBehaviour{
 [Serializable]public struct Influence{public int bone;public Vector3 bindPoint;public float weight;}
 [Serializable]public struct Point{public Influence[] influences;}
 public Transform nativeRig;public Transform[] bones;public Point[] shoePoints;public Vector3 nativeRest;public float maxCorrection=.18f;Matrix4x4[] matrices;public float LastCorrection{get;private set;}public float UncorrectedMinimum{get;private set;}
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
  if(matrices==null||matrices.Length!=bones.Length){matrices=new Matrix4x4[bones.Length];same=false;}
  for(int i=0;i<bones.Length;i++){var value=bones[i].localToWorldMatrix;if(!matrices[i].Equals(value))same=false;matrices[i]=value;}
  if(same)return measuredMinimum;
  float min=float.PositiveInfinity;
  foreach(var point in shoePoints){float y=0;foreach(var influence in point.influences){var m=matrices[influence.bone];var p=influence.bindPoint;y+=(m.m10*p.x+m.m11*p.y+m.m12*p.z+m.m13)*influence.weight;}min=Mathf.Min(min,y);}
  MinimumEvaluationCount++;measuredMinimum=min;measuredPoints=shoePoints;minimumValid=true;return min;
 }
 void ApplyForReviewImpl(){if(!nativeRig||bones==null||shoePoints==null)return;float min=MeasureMinimum();UncorrectedMinimum=min;LastCorrection=Mathf.Clamp(-min,0,maxCorrection);nativeRig.position+=Vector3.up*LastCorrection;}
}

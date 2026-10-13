using System;using UnityEngine;
[DefaultExecutionOrder(300)]public sealed class CandidateFootGrounding:MonoBehaviour{
 [Serializable]public struct Influence{public int bone;public Vector3 bindPoint;public float weight;}
 [Serializable]public struct Point{public Influence[] influences;}
 public Transform nativeRig;public Transform[] bones;public Point[] shoePoints;public Vector3 nativeRest;public float maxCorrection=.18f;Matrix4x4[] matrices;public float LastCorrection{get;private set;}public float UncorrectedMinimum{get;private set;}
 void Update(){ResetForSample();}void LateUpdate(){ApplyForReview();}
 public void ResetForSample(){if(nativeRig)nativeRig.localPosition=nativeRest;}
 public void ApplyForReview(){if(!nativeRig||bones==null||shoePoints==null)return;ResetForSample();if(matrices==null||matrices.Length!=bones.Length)matrices=new Matrix4x4[bones.Length];for(int i=0;i<bones.Length;i++)matrices[i]=bones[i].localToWorldMatrix;float min=float.PositiveInfinity;foreach(var point in shoePoints){var p=Vector3.zero;foreach(var influence in point.influences)p+=matrices[influence.bone].MultiplyPoint3x4(influence.bindPoint)*influence.weight;min=Mathf.Min(min,p.y);}UncorrectedMinimum=min;LastCorrection=Mathf.Clamp(-min,0,maxCorrection);nativeRig.position+=Vector3.up*LastCorrection;}
}

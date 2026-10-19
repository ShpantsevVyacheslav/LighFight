using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
// Empirical pose-derived review correction. Never changes source clips/bind/mesh.
// Not a universally validated anatomical model; only sampled local quality candidate.
public static class Native67ShoulderPoseReview {
 [Serializable] public sealed class Proof {
  public string side;
  public float armElevationDeg,raiseDeg,clavicleLengthBeforeM,clavicleLengthAfterM;
  public float neckGapBeforeM,neckGapAfterM,acromionShiftM,wristShiftM;
  public float upperArmRotationChangeDeg,neckShiftM;
 }
 static float RotationDifference(Quaternion a,Quaternion b) {
  double al=Math.Sqrt((double)a.x*a.x+(double)a.y*a.y+(double)a.z*a.z+(double)a.w*a.w);
  double bl=Math.Sqrt((double)b.x*b.x+(double)b.y*b.y+(double)b.z*b.z+(double)b.w*b.w);
  double dot=Math.Abs(((double)a.x*b.x+(double)a.y*b.y+(double)a.z*b.z+(double)a.w*b.w)/(al*bl));
  dot=Math.Min(1,dot);return (float)(Math.Atan2(Math.Sqrt(Math.Max(0,1-dot*dot)),dot)*360/Math.PI);
 }
 public static List<Proof> Apply(Transform[] bones) {
  var map=bones.ToDictionary(b=>b.name,b=>b);
  var neck=map["Neck"];var chest=map["UpperChest"];
  var neckBefore=neck.position;Vector3 up=(neck.position-chest.position).normalized;
  if(up.sqrMagnitude<.99f)throw new InvalidOperationException("No chest/neck axis");
  var result=new List<Proof>();
  foreach(string side in new[]{"Left","Right"}) {
   var clavicle=map[side+"_Shoulder"];var arm=map[side+"_UpperArm"];
   var elbow=map[side+"_LowerArm"];var wrist=map[side+"_Hand"];
   var oldArm=arm.position;var oldWrist=wrist.position;var oldRotation=arm.rotation;
   Vector3 clavector=arm.position-clavicle.position;
   float elevation=Vector3.Angle(-up,elbow.position-arm.position);
   // C1 continuous activation and cap: no slope jump at threshold or saturation.
   float raise=16*Mathf.SmoothStep(0,1,Mathf.Clamp01((elevation-35)/60));
   var proof=new Proof{side=side,armElevationDeg=elevation,raiseDeg=raise,
    clavicleLengthBeforeM=clavector.magnitude,neckGapBeforeM=Vector3.Dot(neck.position-oldArm,up)};
   Vector3 axis=Vector3.Cross(clavector,up);
   if(raise>0&&axis.sqrMagnitude>1e-12f) {
    clavicle.rotation=Quaternion.AngleAxis(raise,axis.normalized)*clavicle.rotation;
    // Preserve authored world orientation of upper arm and descendants.
    // Arm origin/wrist translation is intentionally changed and measured below.
    arm.rotation=oldRotation;
   }
   proof.clavicleLengthAfterM=(arm.position-clavicle.position).magnitude;
   proof.neckGapAfterM=Vector3.Dot(neck.position-arm.position,up);
   proof.acromionShiftM=Vector3.Distance(oldArm,arm.position);
   proof.wristShiftM=Vector3.Distance(oldWrist,wrist.position);
   proof.upperArmRotationChangeDeg=RotationDifference(oldRotation,arm.rotation);
   proof.neckShiftM=Vector3.Distance(neckBefore,neck.position);
   if(Math.Abs(proof.clavicleLengthBeforeM-proof.clavicleLengthAfterM)>1e-5f||
      proof.wristShiftM>.03f||proof.neckShiftM>1e-6f||
      proof.upperArmRotationChangeDeg>.01f||proof.neckGapAfterM>proof.neckGapBeforeM+1e-6f)
    throw new InvalidOperationException("Shoulder pose review gate: "+JsonUtility.ToJson(proof));
   result.Add(proof);
  }
  return result;
 }
}

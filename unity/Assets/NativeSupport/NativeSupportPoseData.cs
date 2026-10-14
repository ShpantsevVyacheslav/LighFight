using System;using UnityEngine;
// Provider transform curves exported without re-keying or smoothing.
public sealed class NativeSupportPoseData:ScriptableObject {
 [Serializable]public sealed class Joint {public string path;public AnimationCurve px,py,pz,qx,qy,qz,qw,sx,sy,sz;}
 public string sourceName,sourceCurveHash;public float duration;public Joint[] joints;
 public Transform[] Bind(Transform root){var targets=new Transform[joints.Length];for(int i=0;i<targets.Length;i++){targets[i]=joints[i].path.Length==0?root:root.Find(joints[i].path);if(!targets[i])throw new InvalidOperationException("Native original curve path absent "+joints[i].path);}return targets;}
 public void Apply(Transform[] targets,float seconds){for(int i=0;i<joints.Length;i++){var j=joints[i];var t=targets[i];if(j.px!=null)t.localPosition=new Vector3(j.px.Evaluate(seconds),j.py.Evaluate(seconds),j.pz.Evaluate(seconds));if(j.qx!=null){var q=new Quaternion(j.qx.Evaluate(seconds),j.qy.Evaluate(seconds),j.qz.Evaluate(seconds),j.qw.Evaluate(seconds));t.localRotation=Quaternion.Normalize(q);}if(j.sx!=null)t.localScale=new Vector3(j.sx.Evaluate(seconds),j.sy.Evaluate(seconds),j.sz.Evaluate(seconds));}}
}

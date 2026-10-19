using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using LighFight.Runtime;

// A separate authored dance-only candidate. Baseline Meshy retargets stay immutable.
public static class UniqueDanceGirdleReview
{
 const string Root="Assets/UniqueDancesReview";
 static readonly string[] Fields={"px","py","pz","qx","qy","qz","qw","sx","sy","sz"};
 sealed class Side { public Transform shoulder,arm,elbow,hand;public float restArm,restClavicle;public readonly List<Quaternion> shoulders=new List<Quaternion>(),arms=new List<Quaternion>(); }
 public static void Run()
 {
  string evidence="../art/source/unique-dances-20261018/audit/girdle";Directory.CreateDirectory(evidence);
  var rows=new List<string>{"actor,samples192PlusSourceKnots,holdout384,armOrientationMaxDeg,armMatrixAxisMaxDeg,handMovementMaxM,restHeightM,headFootBoneMaxM,endpointMaxM"};
  foreach(int actorIndex in new[]{9})
  {
   string folder=Root+"/Fighter-"+actorIndex;
   var baseline=AssetDatabase.LoadAssetAtPath<NativeSupportPoseData>(folder+"/Dance.pose.asset");
   var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(folder+"/Review.prefab");
   var actor=(GameObject)PrefabUtility.InstantiatePrefab(prefab);
   try
   {
    var behaviours=actor.GetComponentsInChildren<MonoBehaviour>(true);var wasEnabled=behaviours.Select(b=>b.enabled).ToArray();
    var transforms=actor.GetComponentsInChildren<Transform>(true);var restP=transforms.Select(t=>t.localPosition).ToArray();var restQ=transforms.Select(t=>t.localRotation).ToArray();var restS=transforms.Select(t=>t.localScale).ToArray();
    foreach(var b in behaviours)b.enabled=false;
    var animator=actor.GetComponentInChildren<Animator>();
    var neck=animator.GetBoneTransform(HumanBodyBones.Neck);var chest=animator.GetBoneTransform(HumanBodyBones.UpperChest);if(!chest)chest=animator.GetBoneTransform(HumanBodyBones.Chest);
    var sides=new[]{new Side{shoulder=animator.GetBoneTransform(HumanBodyBones.LeftShoulder),arm=animator.GetBoneTransform(HumanBodyBones.LeftUpperArm),elbow=animator.GetBoneTransform(HumanBodyBones.LeftLowerArm),hand=animator.GetBoneTransform(HumanBodyBones.LeftHand)},new Side{shoulder=animator.GetBoneTransform(HumanBodyBones.RightShoulder),arm=animator.GetBoneTransform(HumanBodyBones.RightUpperArm),elbow=animator.GetBoneTransform(HumanBodyBones.RightLowerArm),hand=animator.GetBoneTransform(HumanBodyBones.RightHand)}};
    if(!neck||!chest||sides.Any(s=>!s.shoulder||!s.arm||!s.elbow||!s.hand||s.arm.parent!=s.shoulder))throw new Exception("Unsupported girdle hierarchy "+actorIndex);
    Vector3 restUp=neck.position-chest.position;
    foreach(var side in sides){side.restArm=Elevation(side.elbow.position-side.arm.position,restUp);side.restClavicle=Elevation(side.arm.position-side.shoulder.position,restUp);}
    var mesh=actor.GetComponentInChildren<SkinnedMeshRenderer>();float min=100,max=-100;foreach(var v in mesh.sharedMesh.vertices){float y=mesh.transform.TransformPoint(v).y;min=Mathf.Min(min,y);max=Mathf.Max(max,y);}float height=max-min;
    var baselineJoints=baseline.joints;var targets=baseline.Bind(animator.transform);
    // Retain every original 60 Hz knot, then add 192 Hz knots. No original channel
    // except the four named joint rotations is resampled or changed.
    var sourceTimes=baselineJoints.SelectMany(j=>new[]{j.qx,j.qy,j.qz,j.qw}).Where(c=>c!=null).SelectMany(c=>c.keys.Select(k=>k.time)).Distinct().OrderBy(t=>t).ToList();
    int n=Mathf.CeilToInt(baseline.duration*192);var times=new List<float>(sourceTimes);
    for(int i=0;i<=n;i++){float t=baseline.duration*i/n;if(!sourceTimes.Any(x=>Mathf.Abs(x-t)<.000001f))times.Add(t);}times.Sort();
    double trainingError=0;
    foreach(float time in times)
    {
     baseline.Apply(targets,time);Vector3 up=neck.position-chest.position;
     float envelope=Smooth(time/.5f)*Smooth((baseline.duration-time)/.5f);
     foreach(var side in sides)
     {
      Vector3 clavicle=side.arm.position-side.shoulder.position;
      float gap=Mathf.Max(0,Elevation(side.elbow.position-side.arm.position,up)-side.restArm)*.25f-(Elevation(clavicle,up)-side.restClavicle);
      float degrees=Cap(gap)*envelope;
      Vector3 axis=Vector3.Cross(clavicle,up);
      if(axis.sqrMagnitude<1e-12f)throw new Exception("Indeterminate clavicle rotation axis");
      Quaternion oldArm=side.arm.rotation;
      Quaternion newShoulderWorld=Quaternion.AngleAxis(degrees,axis.normalized)*side.shoulder.rotation;
      side.shoulder.localRotation=Quaternion.Inverse(side.shoulder.parent.rotation)*newShoulderWorld;
      side.arm.localRotation=Quaternion.Inverse(newShoulderWorld)*oldArm;
      trainingError=Math.Max(trainingError,Angle(oldArm,side.arm.rotation));
      AddContinuous(side.shoulders,side.shoulder.localRotation);AddContinuous(side.arms,side.arm.localRotation);
     }
    }
    var joints=baselineJoints.Select(Clone).ToArray();
    foreach(var side in sides){Replace(joints,animator.transform,side.shoulder,times,side.shoulders);Replace(joints,animator.transform,side.arm,times,side.arms);}
    string path=folder+"/Dance.girdle.pose.asset";var candidate=AssetDatabase.LoadAssetAtPath<NativeSupportPoseData>(path);
    if(!candidate){candidate=ScriptableObject.CreateInstance<NativeSupportPoseData>();AssetDatabase.CreateAsset(candidate,path);}
    candidate.sourceName=baseline.sourceName+"_GirdleSupport_v1";candidate.duration=baseline.duration;candidate.sourceCurveHash=baseline.sourceCurveHash;candidate.joints=joints;
    var candidateTargets=candidate.Bind(animator.transform);
    var head=animator.GetBoneTransform(HumanBodyBones.Head);var feet=new[]{animator.GetBoneTransform(HumanBodyBones.LeftFoot),animator.GetBoneTransform(HumanBodyBones.RightFoot)};
    double orientation=0,matrixAngle=0;float handMove=0,headFoot=0,endpoint=0,worstTime=0;int holdouts=Mathf.CeilToInt(baseline.duration*384);
    for(int i=0;i<=holdouts;i++)
    {
     float time=baseline.duration*i/holdouts;baseline.Apply(targets,time);
     var armQ=sides.Select(s=>s.arm.rotation).ToArray();var armMatrices=sides.Select(s=>s.arm.localToWorldMatrix).ToArray();var hands=sides.Select(s=>s.hand.position).ToArray();var fixedPoints=new[]{head.position,feet[0].position,feet[1].position};
     candidate.Apply(candidateTargets,time);
     for(int s=0;s<2;s++){double error=Angle(armQ[s],sides[s].arm.rotation);if(error>orientation){orientation=error;worstTime=time;}for(int axis=0;axis<3;axis++)matrixAngle=Math.Max(matrixAngle,AxisAngle(armMatrices[s].GetColumn(axis),sides[s].arm.localToWorldMatrix.GetColumn(axis)));float delta=Vector3.Distance(hands[s],sides[s].hand.position);handMove=Mathf.Max(handMove,delta);if(i==0||i==holdouts)endpoint=Mathf.Max(endpoint,delta);}
     headFoot=Mathf.Max(headFoot,Vector3.Distance(fixedPoints[0],head.position),Vector3.Distance(fixedPoints[1],feet[0].position),Vector3.Distance(fixedPoints[2],feet[1].position));
    }
    rows.Add(string.Join(",",actorIndex,times.Count,holdouts+1,orientation,matrixAngle,handMove,height,headFoot,endpoint));
    File.WriteAllLines(evidence+"/kinematics.csv",rows);
    File.WriteAllText(evidence+"/debug-"+actorIndex+".txt","trainingError="+trainingError+" worstTime="+worstTime+" closestKeyDelta="+times.Min(t=>Mathf.Abs(t-worstTime)));
    if(orientation>.15||matrixAngle>.15||handMove>height*.03f||headFoot>.00001f||endpoint>.00001f)throw new Exception("Girdle holdout failed for "+actorIndex+": "+rows.Last());
    UniqueDanceCurvePack.PackOnly("packing-girdle-"+actorIndex+".csv");
    var overlay=actor.GetComponent<NativeSupportOverlay>();foreach(var entry in overlay.entries)if(entry.state=="Dance")entry.pose=candidate;
    for(int i=0;i<transforms.Length;i++){transforms[i].localPosition=restP[i];transforms[i].localRotation=restQ[i];transforms[i].localScale=restS[i];}
    for(int i=0;i<behaviours.Length;i++)behaviours[i].enabled=wasEnabled[i];
    PrefabUtility.SaveAsPrefabAsset(actor,folder+"/Review.girdle.prefab");
    EditorUtility.SetDirty(candidate);
    if(baseline.ActiveUsers==0)baseline.ReleaseDecoded();
   }
   finally{foreach(var b in actor.GetComponents<MonoBehaviour>())if(b is IActorCurveWarmup warm)warm.Dispose();UnityEngine.Object.DestroyImmediate(actor);}
  }
  // This packs and verifies every key bit, with the existing decoder. Baselines
  // are already packed and skipped; the original FBXs and original retargets stay.
  Debug.Log("AGENT_RESULT Sailor-only separate girdle candidate; 384 Hz holdout gates passed; no promotion");
 }
 static NativeSupportPoseData.Joint Clone(NativeSupportPoseData.Joint source){var j=new NativeSupportPoseData.Joint{path=source.path};foreach(string f in Fields){var field=typeof(NativeSupportPoseData.Joint).GetField(f);field.SetValue(j,field.GetValue(source));}return j;}
 static float Elevation(Vector3 v,Vector3 up){if(v.sqrMagnitude<1e-12||up.sqrMagnitude<1e-12)throw new Exception("Invalid anatomy reference");return Mathf.Asin(Mathf.Clamp(Vector3.Dot(v.normalized,up.normalized),-1,1))*Mathf.Rad2Deg;}
 static float Smooth(float t){t=Mathf.Clamp01(t);return t*t*(3-2*t);}
 static float Cap(float x){if(x<=0)return 0;if(x<2){float t=x/2;return 2*t*t*(2-t);}if(x<=13)return x;if(x<15){float t=(x-13)/2;return 13+2*(t+t*t-t*t*t);}return 15;}
 static void AddContinuous(List<Quaternion> values,Quaternion q){if(values.Count>0&&Quaternion.Dot(values.Last(),q)<0)q=new Quaternion(-q.x,-q.y,-q.z,-q.w);values.Add(q);}
 static void Replace(NativeSupportPoseData.Joint[] joints,Transform root,Transform bone,List<float> times,List<Quaternion> rotations){var j=joints.Single(x=>x.path==AnimationUtility.CalculateTransformPath(bone,root));j.qx=Curve(times,rotations.Select(q=>q.x));j.qy=Curve(times,rotations.Select(q=>q.y));j.qz=Curve(times,rotations.Select(q=>q.z));j.qw=Curve(times,rotations.Select(q=>q.w));}
 static AnimationCurve Curve(List<float> times,IEnumerable<float> values){var curve=new AnimationCurve(times.Zip(values,(t,v)=>new Keyframe(t,v)).ToArray());for(int i=0;i<curve.length;i++){AnimationUtility.SetKeyLeftTangentMode(curve,i,AnimationUtility.TangentMode.Linear);AnimationUtility.SetKeyRightTangentMode(curve,i,AnimationUtility.TangentMode.Linear);}return curve;}
 static double Angle(Quaternion a,Quaternion b){double dot=(double)a.x*b.x+(double)a.y*b.y+(double)a.z*b.z+(double)a.w*b.w;double aa=(double)a.x*a.x+(double)a.y*a.y+(double)a.z*a.z+(double)a.w*a.w;double bb=(double)b.x*b.x+(double)b.y*b.y+(double)b.z*b.z+(double)b.w*b.w;return 2*Math.Acos(Math.Min(1,Math.Abs(dot)/Math.Sqrt(aa*bb)))*180/Math.PI;}
 static double AxisAngle(Vector4 a,Vector4 b){double dot=(double)a.x*b.x+(double)a.y*b.y+(double)a.z*b.z;double aa=(double)a.x*a.x+(double)a.y*a.y+(double)a.z*a.z;double bb=(double)b.x*b.x+(double)b.y*b.y+(double)b.z*b.z;return Math.Acos(Math.Max(-1,Math.Min(1,dot/Math.Sqrt(aa*bb))))*180/Math.PI;}
}

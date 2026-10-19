using System;using System.IO;using System.Linq;using System.Collections.Generic;using UnityEngine;using UnityEditor;using LighFight.Runtime;
public static class UniqueDanceGenericSourceAudit {
 public static void Run(){RunFor(9,74);}
 public static void RunFor(int actorIndex,int actionId){
  string src="Assets/UniqueDancesReview/GenericSources/action-"+actionId+".fbx",folder="Assets/UniqueDancesReview/Fighter-"+actorIndex;var clip=AssetDatabase.LoadAllAssetsAtPath(src).OfType<AnimationClip>().Single(c=>!c.name.StartsWith("__preview"));if(clip.humanMotion)throw new Exception("Not Generic source");var bindings=AnimationUtility.GetCurveBindings(clip);var joints=new List<NativeSupportPoseData.Joint>();
  foreach(string path in bindings.Select(b=>b.path).Distinct()){
   var joint=new NativeSupportPoseData.Joint{path=path};foreach(var b in bindings.Where(b=>b.path==path)){
    string field=b.propertyName.StartsWith("m_LocalPosition.")?"p"+b.propertyName.Last():b.propertyName.StartsWith("m_LocalRotation.")?"q"+b.propertyName.Last():b.propertyName.StartsWith("m_LocalScale.")?"s"+b.propertyName.Last():null;if(field==null)throw new Exception("Unsupported exact Generic channel "+b.type+" "+b.propertyName);typeof(NativeSupportPoseData.Joint).GetField(field).SetValue(joint,AnimationUtility.GetEditorCurve(clip,b));
   }joints.Add(joint);
  }
  string pathAsset=folder+"/Dance.generic.source.pose.asset";var data=AssetDatabase.LoadAssetAtPath<NativeSupportPoseData>(pathAsset);if(!data){data=ScriptableObject.CreateInstance<NativeSupportPoseData>();AssetDatabase.CreateAsset(data,pathAsset);}data.sourceName="Meshy"+actionId+"_ExactGenericSource";data.duration=clip.length;data.joints=joints.ToArray();
  var source=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(src));var native=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(src));
  try{
   foreach(var actor in new[]{source,native}){var a=actor.GetComponentInChildren<Animator>();if(a)a.enabled=false;}
   var nt=data.Bind(native.transform);var st=data.JointPaths.Select(p=>p.Length==0?source.transform:source.transform.Find(p)).ToArray();double maximumAngle=0;float maximumPosition=0;int count=Mathf.CeilToInt(data.duration*317);var rows=new List<string>{"time,maxLocalRotationErrorDeg,maxWorldPositionErrorM"};
   for(int k=0;k<=count;k++){float t=data.duration*k/count;clip.SampleAnimation(source,t);data.Apply(nt,t);double a=0;float p=0;for(int n=0;n<st.Length;n++){a=Math.Max(a,Angle(st[n].localRotation,nt[n].localRotation));p=Mathf.Max(p,Vector3.Distance(st[n].position,nt[n].position));}maximumAngle=Math.Max(maximumAngle,a);maximumPosition=Mathf.Max(maximumPosition,p);rows.Add(t+","+a+","+p);}
   string output=actorIndex==9?"../art/source/unique-dances-20261018/audit/generic-sailor":"../art/source/unique-dances-20261018/audit/generic-roster/fighter-"+actorIndex;Directory.CreateDirectory(output);File.WriteAllLines(output+"/exact-source-parity.csv",rows);File.WriteAllText(output+"/exact-source-summary.txt","joints="+joints.Count+" holdouts="+(count+1)+" maxLocalAngleDeg="+maximumAngle+" maxWorldPositionM="+maximumPosition);
   if(maximumAngle>.06||maximumPosition>.00001f)throw new Exception("Native Generic evaluator differs from actual Generic clip "+maximumAngle+" "+maximumPosition);
   UniqueDanceCurvePack.PackOnly(actorIndex==9?"packing-exact-generic-source.csv":"packing-exact-generic-source-"+actorIndex+".csv",pathAsset);Debug.Log("AGENT_RESULT exact Generic source curves parity passed; no retarget approximation");
  }finally{UnityEngine.Object.DestroyImmediate(source);UnityEngine.Object.DestroyImmediate(native);}
 }
 static double Angle(Quaternion a,Quaternion b){double d=(double)a.x*b.x+(double)a.y*b.y+(double)a.z*b.z+(double)a.w*b.w,aa=(double)a.x*a.x+(double)a.y*a.y+(double)a.z*a.z+(double)a.w*a.w,bb=(double)b.x*b.x+(double)b.y*b.y+(double)b.z*b.z+(double)b.w*b.w;return 2*Math.Acos(Math.Min(1,Math.Abs(d)/Math.Sqrt(aa*bb)))*180/Math.PI;}
}

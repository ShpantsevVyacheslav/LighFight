using System;using System.IO;using System.Linq;using System.Collections.Generic;using UnityEngine;using UnityEditor;using UnityEditor.Animations;using LighFight.Runtime;
public static class UniqueDanceQuaternionWitness {
 public static void Run(){
  const string folder="Assets/UniqueDancesReview/Fighter-9/";var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(folder+"Review.prefab");var data=AssetDatabase.LoadAssetAtPath<NativeSupportPoseData>(folder+"Dance.pose.asset");var human=(GameObject)PrefabUtility.InstantiatePrefab(prefab);var native=(GameObject)PrefabUtility.InstantiatePrefab(prefab);var rows=new List<string>{"seconds,path,rawNorm,previousKeyTime,nextKeyTime,adjacentKeyDot,humanNativeLocalAngleDeg"};
  try{
   foreach(var a in new[]{human,native})foreach(var b in a.GetComponentsInChildren<MonoBehaviour>(true))b.enabled=false;
   var ha=human.GetComponentInChildren<Animator>();ha.applyRootMotion=false;ha.cullingMode=AnimatorCullingMode.AlwaysAnimate;var na=native.GetComponentInChildren<Animator>();var controller=new AnimatorController();controller.AddLayer("DanceBake");var state=controller.layers[0].stateMachine.AddState("Source");state.motion=AssetDatabase.LoadAssetAtPath<AnimationClip>(folder+"Dance.carrier.anim");state.iKOnFeet=false;ha.runtimeAnimatorController=controller;ha.Rebind();var nt=data.Bind(na.transform);var ht=data.JointPaths.Select(p=>p.Length==0?ha.transform:ha.transform.Find(p)).ToArray();var js=data.joints;
   float witness=5.834738f;double max=0;string maxPath="";float maxTime=0;
   var times=Enumerable.Range(-32,65).Select(k=>witness+k/7680f).Concat(js.SelectMany(j=>j.qx.keys.Select(k=>k.time)).Where(t=>Mathf.Abs(t-witness)<.04f)).Distinct().OrderBy(t=>t);
   foreach(float t in times){
    ha.Play("DanceBake.Source",0,t/data.duration);ha.Update(0);data.Apply(nt,t);
    for(int j=0;j<js.Length;j++){
     var q=js[j];if(q.qx==null)continue;double angle=Angle(ht[j].localRotation,nt[j].localRotation);if(angle>max){max=angle;maxPath=q.path;maxTime=t;}
     if(!(q.path.Contains("shoulder")||q.path.Contains("Shoulder")||q.path.Contains("arm")||q.path.Contains("Arm")))continue;
     var keys=q.qx.keys;int right=Array.FindIndex(keys,x=>x.time>=t);if(right<1)right=1;if(right>=keys.Length)right=keys.Length-1;float a=keys[right-1].time,b=keys[right].time;
     Quaternion raw=new Quaternion(q.qx.Evaluate(t),q.qy.Evaluate(t),q.qz.Evaluate(t),q.qw.Evaluate(t));Quaternion qa=new Quaternion(q.qx.Evaluate(a),q.qy.Evaluate(a),q.qz.Evaluate(a),q.qw.Evaluate(a)),qb=new Quaternion(q.qx.Evaluate(b),q.qy.Evaluate(b),q.qz.Evaluate(b),q.qw.Evaluate(b));
     rows.Add(string.Join(",",t,q.path,Math.Sqrt(Quaternion.Dot(raw,raw)),a,b,Quaternion.Dot(qa,qb),angle));
    }
   }
   File.WriteAllLines("../art/source/unique-dances-20261018/audit/girdle/source-quaternion-witness.csv",rows);File.WriteAllText("../art/source/unique-dances-20261018/audit/girdle/human-native-witness.txt","maxLocalAngleDeg="+max+" path="+maxPath+" time="+maxTime);Debug.Log("AGENT_RESULT witness "+max+" "+maxPath+" "+maxTime);
  }finally{foreach(var a in new[]{human,native}){foreach(var b in a.GetComponents<MonoBehaviour>())if(b is IActorCurveWarmup warm)warm.Dispose();UnityEngine.Object.DestroyImmediate(a);}}
 }
 static double Angle(Quaternion a,Quaternion b){double d=(double)a.x*b.x+(double)a.y*b.y+(double)a.z*b.z+(double)a.w*b.w;double aa=(double)a.x*a.x+(double)a.y*a.y+(double)a.z*a.z+(double)a.w*a.w,bb=(double)b.x*b.x+(double)b.y*b.y+(double)b.z*b.z+(double)b.w*b.w;return 2*Math.Acos(Math.Min(1,Math.Abs(d)/Math.Sqrt(aa*bb)))*180/Math.PI;}
}

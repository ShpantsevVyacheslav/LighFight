#if UNITY_EDITOR
using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEditor;
using UnityEngine.TestTools;
using LighFight.Core;
using LighFight.Runtime;
namespace LighFight.Tests {
 public sealed class SettledActorPoseTests {
  [UnityTest]public IEnumerator NativeRosterFinalPosesRemainExactAndResume(){
   var catalog=AssetDatabase.LoadAssetAtPath<GameContent>("Assets/LighFight/Content/GameContent.asset");
   foreach(var prefab in catalog.fighters){
    var actor=Object.Instantiate(prefab);var view=actor.GetComponent<FighterView>();
    try{
     foreach(var warmup in actor.GetComponents<MonoBehaviour>().OfType<IActorCurveWarmup>())yield return warmup.Warmup();
     foreach(string motion in new[]{Prototype.VictoryMotion(view),"Fall","Defeat","Dance"}.Distinct()){
      if(motion==null||!view.HasNativeMotion(motion))continue;
      view.ResumeResultPose();view.PoseTowards(new Fighter{X=0,Guard=true},new Fighter{X=1},.016f);
      view.RequestResultPoseHold(motion,0);yield return null;
      Assert.IsFalse(view.ResultPoseHeld,"Do not truncate a result animation");
      bool looping=false;foreach(var provider in actor.GetComponents<MonoBehaviour>().OfType<IResultMotionSource>())if(provider.TryGetResultDuration(motion,out var duration))looping=float.IsPositiveInfinity(duration);
      view.PreviewNativeMotion(motion,view.PreviewDuration(motion)+1);
      view.RequestResultPoseHold(motion,view.PreviewDuration(motion)+1);yield return null;
      if(looping)Assert.IsFalse(view.ResultPoseHeld,"Keep authored loops running");
      else {Assert.IsTrue(view.ResultPoseHeld,"Production request must freeze after the final late pass");view.Pose(new Fighter{Guard=true},1,.016f);Assert.IsFalse(view.ResultPoseHeld,"Direct selection pose resumes the actor");view.PreviewNativeMotion(motion,view.PreviewDuration(motion)+1);}
      yield return null; // let fingers, original curves, and floor finish normally
      var bones=actor.GetComponentsInChildren<Transform>(true);
      var pos=bones.Select(x=>x.localPosition).ToArray();var rot=bones.Select(x=>x.localRotation).ToArray();var scale=bones.Select(x=>x.localScale).ToArray();
      var behaviours=actor.GetComponentsInChildren<Behaviour>(true);var enabled=behaviours.Select(x=>x.enabled).ToArray();
      var hold=actor.GetComponent<SettledActorPose>();if(!hold)hold=actor.AddComponent<SettledActorPose>();
      hold.Request();typeof(SettledActorPose).GetMethod("LateUpdate",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(hold,null);
      var ground=actor.GetComponent<CandidateFootGrounding>();int evaluations=ground?ground.MinimumEvaluationCount:0;
      for(int frame=0;frame<4;frame++){
       yield return null;
       for(int i=0;i<bones.Length;i++){Assert.AreEqual(pos[i],bones[i].localPosition,prefab.name+" "+motion+" position");Assert.AreEqual(rot[i],bones[i].localRotation,prefab.name+" "+motion+" rotation");Assert.AreEqual(scale[i],bones[i].localScale,prefab.name+" "+motion+" scale");}
       if(ground)Assert.AreEqual(evaluations,ground.MinimumEvaluationCount,"No grounding while held");
      }
      hold.Resume();for(int i=0;i<behaviours.Length;i++)Assert.AreEqual(enabled[i],behaviours[i].enabled,"Restore original enabled flags");
      view.PoseTowards(new Fighter{X=0,Guard=true},new Fighter{X=1},.016f);yield return null;
      Assert.IsFalse(hold.IsHeld);Assert.IsTrue(view.GetComponentInChildren<Animator>(true).enabled,"Native motion resumes");
     }
    }finally{Object.DestroyImmediate(actor);}
   }
  }
  [Test]public void PendingHoldCanBeCancelledAndDisabledWritersStayDisabled(){
   var actor=new GameObject("cancel pending hold");try{
    var animator=actor.AddComponent<Animator>();animator.enabled=false;
    var hold=actor.AddComponent<SettledActorPose>();var late=typeof(SettledActorPose).GetMethod("LateUpdate",BindingFlags.Instance|BindingFlags.NonPublic);
    hold.Request();hold.Resume();late.Invoke(hold,null);Assert.IsFalse(hold.IsHeld);
    hold.Request();late.Invoke(hold,null);Assert.IsTrue(hold.IsHeld);hold.Resume();Assert.IsFalse(animator.enabled);
   }finally{Object.DestroyImmediate(actor);}
  }
 }
}
#endif

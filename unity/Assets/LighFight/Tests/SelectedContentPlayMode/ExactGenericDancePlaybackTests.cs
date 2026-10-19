#if UNITY_EDITOR
using System;using System.Collections;using System.Linq;using System.Reflection;using NUnit.Framework;using UnityEditor;using UnityEngine;using UnityEngine.TestTools;using LighFight.Runtime;
namespace LighFight.Tests {
 public sealed class ExactGenericDancePlaybackTests {
  const string Prefab="Assets/UniqueDancesReview/Fighter-9/Review.exact-generic.prefab";
  static MonoBehaviour Exact(GameObject actor)=>(MonoBehaviour)actor.GetComponent("ExactGenericDanceRetarget");
  static object Field(object o,string name)=>o.GetType().GetField(name).GetValue(o);
  static T Property<T>(object o,string name)=>(T)o.GetType().GetProperty(name).GetValue(o);
  static void Dispose(GameObject a){if(!a)return;foreach(var w in a.GetComponents<MonoBehaviour>().OfType<IActorCurveWarmup>())w.Dispose();UnityEngine.Object.DestroyImmediate(a);}
  [UnityTest]public IEnumerator ColdDanceCannotDecodeBeforeTypedWarmup(){
   var actor=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(Prefab));
   try{var component=Exact(actor);Assert.NotNull(component);var source=Field(component,"source");Assert.AreEqual(0,Property<int>(source,"ActiveUsers"));source.GetType().GetMethod("ReleaseDecoded").Invoke(source,null);actor.GetComponent<FighterView>().FullPreviewMotion=false;actor.GetComponent<FighterView>().PreviewNativeMotion("Dance",1);
    for(int i=0;i<3;i++)yield return null;
    Assert.False(((IActorCurveWarmup)component).IsWarm);Assert.False(Property<bool>(source,"IsDecoded"),"Cold LateUpdate must not decode outside visible loading");Assert.AreEqual(0,Property<int>(source,"ActiveUsers"));Assert.AreEqual(0,Property<long>(component,"PoseEvaluationCount"));Assert.AreEqual(0,Property<long>(component,"PoseCacheCaptureCount"));
   }finally{Dispose(actor);}
  }
  [UnityTest]public IEnumerator NativeDanceLeasesSurvivePauseAndDisposeExactlyOnce(){
   var actor=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(Prefab));
   try{var component=Exact(actor);var warm=(IActorCurveWarmup)component;var source=Field(component,"source");int start=Property<int>(source,"ActiveUsers");yield return warm.Warmup();Assert.True(warm.IsWarm);Assert.AreEqual(start+1,Property<int>(source,"ActiveUsers"));actor.SetActive(false);yield return null;Assert.AreEqual(start+1,Property<int>(source,"ActiveUsers"),"Disable retains resident selected-source lease");warm.CancelWarmup();Assert.AreEqual(start+1,Property<int>(source,"ActiveUsers"));warm.Dispose();warm.Dispose();Assert.AreEqual(start,Property<int>(source,"ActiveUsers"));UnityEngine.Object.Destroy(actor);actor=null;yield return null;Assert.AreEqual(start,Property<int>(source,"ActiveUsers"),"Destroy cannot double-release another owner");
   }finally{Dispose(actor);}
  }
  [UnityTest]public IEnumerator CancelledColdWarmupCannotReleaseNewLeaseOrTouchCombatBones(){
   var actor=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(Prefab));
   try{var component=Exact(actor);var warm=(IActorCurveWarmup)component;var source=Field(component,"source");int start=Property<int>(source,"ActiveUsers");var cancelled=warm.Warmup();Assert.True(cancelled.MoveNext());warm.CancelWarmup();yield return warm.Warmup();Assert.AreEqual(start+1,Property<int>(source,"ActiveUsers"));Assert.False(cancelled.MoveNext());(cancelled as IDisposable)?.Dispose();Assert.AreEqual(start+1,Property<int>(source,"ActiveUsers"));actor.GetComponent<FighterView>().FullPreviewMotion=false;long writes=Property<long>(component,"PoseCacheCaptureCount"),evaluations=Property<long>(component,"PoseEvaluationCount");for(int i=0;i<3;i++)yield return null;Assert.AreEqual(writes,Property<long>(component,"PoseCacheCaptureCount"),"NonDance battle must not snapshot all bones every frame");Assert.AreEqual(evaluations,Property<long>(component,"PoseEvaluationCount"),"NonDance battle never writes animated source/target bones");
   }finally{Dispose(actor);}
  }
  [UnityTest]public IEnumerator DanceOwnsOriginalShoeSetAndDisableRestoresPriorGrounding(){
   var actor=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(Prefab));
   try{var component=Exact(actor);var warm=(IActorCurveWarmup)component;yield return warm.Warmup();var overlay=actor.GetComponent("NativeSupportOverlay") as Behaviour;overlay.enabled=false;var grounding=actor.GetComponent("CandidateFootGrounding");var native=Field(overlay,"supportPoints");var prior=((Array)Field(grounding,"shoePoints")).Clone();grounding.GetType().GetField("shoePoints").SetValue(grounding,prior);
    component.GetType().GetMethod("EvaluateForReview").Invoke(component,new object[]{5.116136f});Assert.True(ReferenceEquals(native,Field(grounding,"shoePoints")),"Dance explicitly selects original native full-shoe points without another overlay side effect");grounding.GetType().GetMethod("ApplyForReview").Invoke(grounding,null);var rig=(Transform)Field(grounding,"nativeRig");Assert.Greater(Vector3.Distance(rig.localPosition,(Vector3)Field(grounding,"nativeRest")),.01f,"Witness must exercise active floor offset");
    component.enabled=false;Assert.True(ReferenceEquals(prior,Field(grounding,"shoePoints")));Assert.AreEqual((float)Field(component,"inactiveCorrectionCap"),(float)Field(grounding,"maxCorrection"));Assert.Less(Vector3.Distance(rig.localPosition,(Vector3)Field(grounding,"nativeRest")),.000001f,"Disable clears dance-owned floor translation before combat");Assert.True(warm.IsWarm,"Pause retains source lease");warm.Dispose();Assert.True(ReferenceEquals(prior,Field(grounding,"shoePoints")));
   }finally{Dispose(actor);}
  }
 }
}
#endif

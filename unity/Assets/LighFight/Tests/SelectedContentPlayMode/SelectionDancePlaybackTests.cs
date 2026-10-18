#if UNITY_EDITOR
using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using LighFight.Core;
using LighFight.Runtime;
namespace LighFight.Tests {
 public sealed class SelectionDancePlaybackTests {
  const BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
  static void Call(object target,string method,params object[] args)=>target.GetType().GetMethod(method,Private).Invoke(target,args);
  static void Set(object target,string field,object value)=>target.GetType().GetField(field,Private).SetValue(target,value);
  static T Get<T>(object target,string field)=>(T)target.GetType().GetField(field,Private).GetValue(target);
  [UnityTest]public IEnumerator ActualNativeDanceReachesItsFullEndBeforeBlendedIdleReturn(){
   // Ranger's authored4.3s dance, not a synthetic/short carrier.
   var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/UniqueDancesReview/Fighter-5/Review.prefab");Assert.NotNull(prefab);
   var actor=Object.Instantiate(prefab);var host=new GameObject("selection dance lifecycle");var game=host.AddComponent<Prototype>();game.enabled=false;Set(game,"session",new FightSession(false,2,60));var view=actor.GetComponent<FighterView>();
   try{
    foreach(var warmup in actor.GetComponents<MonoBehaviour>().OfType<IActorCurveWarmup>())yield return warmup.Warmup();
    view.FullPreviewMotion=true;view.Pose(new Fighter(),1,0);Set(game,"player",view);Set(game,"previewMove",-2);Set(game,"previewClock",0f);
    float duration=view.PreviewDuration("Dance");Assert.Greater(duration,4,"Must use the full authored routine");
    Call(game,"UpdateSelectionPose",duration-.1f);yield return null;Assert.AreEqual(-2,Get<int>(game,"previewMove"));
    var animator=actor.GetComponentInChildren<Animator>();Assert.IsTrue(animator.GetCurrentAnimatorStateInfo(0).IsName("Base Layer.Dance"));Assert.AreEqual("Dance",Get<string>(view,"previousMotion"),"Manual state must be recorded for return blending");
    Call(game,"UpdateSelectionPose",.11f);yield return null;
    Assert.AreEqual(-1,Get<int>(game,"previewMove"));Assert.IsTrue(animator.GetCurrentAnimatorStateInfo(0).IsName("Base Layer.Dance"),"Finish frame must sample Dance, not cut to Idle");Assert.Greater(animator.GetCurrentAnimatorStateInfo(0).normalizedTime,.999f);
    var bones=actor.GetComponentsInChildren<Transform>();var end=bones.Select(t=>t.localRotation).ToArray();
    Call(game,"UpdateSelectionPose",0f);yield return null;
    Assert.IsTrue(animator.GetCurrentAnimatorStateInfo(0).IsName("Base Layer.Idle"));Assert.AreEqual(.28f,Get<float>(view,"blendDuration"),.0001f);
    Assert.Greater(bones.Select((t,i)=>Quaternion.Angle(t.localRotation,end[i])).Count(a=>a<.01f),bones.Length/2,"Zero-time return begins from the displayed dance pose");
    Call(game,"UpdateSelectionPose",.3f);yield return null;Assert.GreaterOrEqual(Get<float>(view,"blendElapsed"),Get<float>(view,"blendDuration"));
   }finally{Object.DestroyImmediate(actor);Object.DestroyImmediate(host);}
  }
  [UnityTest]public IEnumerator ToggleOffReturnsSmoothlyAndDoesNotRecaptureAllBonesEachDanceFrame(){
   var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/UniqueDancesReview/Fighter-4/Review.prefab");Assert.NotNull(prefab);
   var actor=Object.Instantiate(prefab);var host=new GameObject("dance toggle return");var game=host.AddComponent<Prototype>();game.enabled=false;Set(game,"session",new FightSession(false,2,60));var view=actor.GetComponent<FighterView>();
   try{
    foreach(var warmup in actor.GetComponents<MonoBehaviour>().OfType<IActorCurveWarmup>())yield return warmup.Warmup();
    view.FullPreviewMotion=true;view.Pose(new Fighter(),1,0);Set(game,"player",view);Set(game,"previewMove",-2);Set(game,"previewClock",0f);
    Call(game,"UpdateSelectionPose",.02f);Assert.AreEqual(.28f,Get<float>(view,"blendDuration"),.0001f);Assert.Less(Get<float>(view,"blendElapsed"),Get<float>(view,"blendDuration"),"Dance entry must blend from the current pose");yield return null;
    Call(game,"UpdateSelectionPose",.4f);yield return null;var captured=Get<Quaternion[]>(view,"blendRotations");var contents=captured.ToArray();
    Call(game,"UpdateSelectionPose",.2f);yield return null;Assert.AreSame(captured,Get<Quaternion[]>(view,"blendRotations"));CollectionAssert.AreEqual(contents,captured,"Unchanged dance state must not capture bones every frame");
    Assert.Greater(view.PreviewDuration("Dance"),16,"Rose must retain its complete long routine");
    Set(game,"previewMove",-1);Call(game,"UpdateSelectionPose",.01f);yield return null;Assert.AreEqual("Idle",Get<string>(view,"previousMotion"));Assert.AreEqual(.28f,Get<float>(view,"blendDuration"),.0001f);Assert.Less(Get<float>(view,"blendElapsed"),Get<float>(view,"blendDuration"));
    Assert.IsTrue(view.PreviewNativeMotion("Dance",1));Assert.AreEqual(Get<float>(view,"blendDuration"),Get<float>(view,"blendElapsed"),"Existing nonselection PreviewNativeMotion behavior must remain immediate");
   }finally{Object.DestroyImmediate(actor);Object.DestroyImmediate(host);}
  }
 }
}
#endif

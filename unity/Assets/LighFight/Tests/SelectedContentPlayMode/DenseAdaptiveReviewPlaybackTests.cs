#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using LighFight.Runtime;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace LighFight.Tests
{
 public sealed class DenseAdaptiveReviewPlaybackTests
 {
  const string PrefabPath="Assets/DenseRigifyReview/PlayableReview/SailorDenseBOXReview.prefab";
  GameObject actor;int residentBaseline,supportBaseline;
  static Type BridgeType=>Type.GetType("DenseAdaptivePoseBridge, Assembly-CSharp",true);
  static Type SupportType=>Type.GetType("DenseRigifySupportOwner, Assembly-CSharp",true);
  static int ResidentSupport=>(int)SupportType.GetProperty("ResidentSupportPoints").GetValue(null);
  static Type OwnerType=>Type.GetType("DenseAdaptiveReviewOwner, Assembly-CSharp",true);
  static int Resident=>(int)BridgeType.GetProperty("ResidentKeyPackets").GetValue(null);
  static object Get(Component c,string name)=>c.GetType().GetProperty(name).GetValue(c);
  static void Call(Component c,string name)=>c.GetType().GetMethod(name).Invoke(c,null);
  static string Phase(Component c)=>Get(c,"Phase").ToString();
  IActorCurveWarmup[] Warmups(){var values=new List<IActorCurveWarmup>();foreach(var c in actor.GetComponentsInChildren<MonoBehaviour>(true))if(c is IActorCurveWarmup warm)values.Add(warm);return values.ToArray();}
  void Spawn(){residentBaseline=Resident;supportBaseline=ResidentSupport;var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);Assert.That(prefab,Is.Not.Null,"BuildDensePlayableReview must create actual standalone original actor; no synthetic fallback");actor=UnityEngine.Object.Instantiate(prefab);}
  IEnumerator WarmAll(){foreach(var warm in Warmups()){yield return warm.Warmup();Assert.That(warm.IsWarm,Is.True);}Assert.That((int)Get(actor.GetComponent(SupportType),"PointCount"),Is.EqualTo(143168),"All original selected foot vertices required");yield return null;}
  [UnityTearDown] public IEnumerator Cleanup(){if(actor){foreach(var warm in Warmups())warm.Dispose();UnityEngine.Object.Destroy(actor);}yield return null;Assert.That(Resident,Is.EqualTo(residentBaseline),"Actor destruction must not leave or release another actor's resident packet");Assert.That(ResidentSupport,Is.EqualTo(supportBaseline),"Fullshoe resources must be released by actor owner cleanup");}
  struct Pose {public Vector3 p,s;public Quaternion q;}
  static Pose[] Capture(Transform[] bones){var result=new Pose[bones.Length];for(int i=0;i<bones.Length;i++)result[i]=new Pose{p=bones[i].localPosition,s=bones[i].localScale,q=bones[i].localRotation};return result;}
  static void EqualPose(Transform[] bones,Pose[] expected,string context){for(int i=0;i<bones.Length;i++){Assert.That(Vector3.Distance(bones[i].localPosition,expected[i].p),Is.LessThan(.0001f),context+" position "+bones[i].name);Assert.That(Quaternion.Angle(bones[i].localRotation,expected[i].q),Is.LessThan(.03f),context+" rotation "+bones[i].name);Assert.That(Vector3.Distance(bones[i].localScale,expected[i].s),Is.LessThan(.0001f),context+" scale "+bones[i].name);}}
  static void RestorePose(Transform[] bones,Pose[] pose){for(int i=0;i<bones.Length;i++){bones[i].SetLocalPositionAndRotation(pose[i].p,pose[i].q);bones[i].localScale=pose[i].s;}}
  static Pose[] Blended(Pose[] from,Pose[] to,float weight){var result=new Pose[from.Length];for(int i=0;i<result.Length;i++)result[i]=new Pose{p=Vector3.Lerp(from[i].p,to[i].p,weight),q=Quaternion.Slerp(from[i].q,to[i].q,weight),s=Vector3.Lerp(from[i].s,to[i].s,weight)};return result;}
  [UnityTest] public IEnumerator FullAuthoredOwnerEntryEndpointAndSmoothNeutralReturn()
  {
   Spawn();yield return WarmAll();var owner=(Behaviour)actor.GetComponent(OwnerType);var bridge=actor.GetComponent(BridgeType);var view=actor.GetComponent<FighterView>();var bones=actor.GetComponentInChildren<SkinnedMeshRenderer>().bones;var neutral=Capture(bones);
   double duration=(double)Get(bridge,"AuthoredDuration");Assert.That(duration,Is.EqualTo(556.0/24).Within(1e-12));Assert.That(view.PreviewDuration("Dance"),Is.EqualTo((float)duration).Within(.00001f));Assert.That(view.HasNativeMotion("SourceBox"),Is.True);
   var animator=actor.GetComponentInChildren<Animator>();animator.applyRootMotion=false;animator.speed=.65f;
   Assert.That(view.PreviewNativeMotion("SourceBox",.9f),Is.True);yield return null;var entry=Capture(bones);
   Call(owner,"PlayDance");yield return null;Assert.That(Phase(owner),Is.EqualTo("Entering"),"Entry must blend rather than snap to direct source pose");
   Assert.That(animator.speed,Is.EqualTo(0));Assert.That(animator.applyRootMotion,Is.False);
   double elapsed=(double)Get(owner,"Elapsed");Assert.That(elapsed,Is.GreaterThan(0).And.LessThan(.28));
   Assert.That(animator.GetCurrentAnimatorStateInfo(0).normalizedTime,Is.EqualTo(FighterView.NativePreviewPhase((float)elapsed,(float)duration,false)).Within(.00001f),"Cached tiny clock must track actual source clock");
   var entered=Capture(bones);BridgeType.GetMethod("Apply").Invoke(bridge,new object[]{elapsed});var exactSource=Capture(bones);RestorePose(bones,entered);
   float t=Mathf.Clamp01((float)(elapsed/.28));EqualPose(bones,Blended(entry,exactSource,t*t*(3-2*t)),"Actual prior BOX -> source Dance C1 entry blend");
   float deadline=Time.realtimeSinceStartup+(float)duration+3;
   while(Phase(owner)!="Endpoint"&&Time.realtimeSinceStartup<deadline)yield return null;
   Assert.That(Phase(owner),Is.EqualTo("Endpoint"),"Full endpoint must survive a rendered frame before return");Assert.That((double)Get(owner,"Elapsed"),Is.EqualTo(duration));Assert.That(animator.GetCurrentAnimatorStateInfo(0).normalizedTime,Is.EqualTo(.999999f).Within(.00001f));Assert.That((int)Get(owner,"ExactPoseApplications"),Is.GreaterThan(100));
   yield return null;Assert.That(Phase(owner),Is.EqualTo("Returning"));
   deadline=Time.realtimeSinceStartup+1;while(Phase(owner)!="Neutral"&&Time.realtimeSinceStartup<deadline)yield return null;
   Assert.That(Phase(owner),Is.EqualTo("Neutral"));EqualPose(bones,neutral,"Complete return to explicit imported-neutral hold");Assert.That(animator.speed,Is.EqualTo(.65f));Assert.That(animator.applyRootMotion,Is.False);
   int applied=(int)Get(owner,"ExactPoseApplications");yield return null;yield return null;Assert.That((int)Get(owner,"ExactPoseApplications"),Is.EqualTo(applied),"Inactive owner must perform zero exact evaluations");
  }
  [UnityTest] public IEnumerator CancelWarmupDisableAndDisposeRetainOnlyOwnedResources()
  {
   Spawn();var owner=(Behaviour)actor.GetComponent(OwnerType);var warm=(IActorCurveWarmup)owner;var bridge=actor.GetComponent(BridgeType);
   var pending=warm.Warmup();Assert.That(pending.MoveNext(),Is.True);warm.CancelWarmup();Assert.That(pending.MoveNext(),Is.False);(pending as IDisposable)?.Dispose();Assert.That(warm.IsWarm,Is.False);Assert.That(Resident,Is.EqualTo(residentBaseline));
   yield return WarmAll();Assert.That(Resident,Is.EqualTo(residentBaseline+1));var bones=actor.GetComponentInChildren<SkinnedMeshRenderer>().bones;var entry=Capture(bones);
   var animator=actor.GetComponentInChildren<Animator>();float priorSpeed=animator.speed;bool priorRootMotion=animator.applyRootMotion;
   Call(owner,"PlayDance");yield return null;Assert.That(Phase(owner),Is.EqualTo("Entering"));owner.enabled=false;
   Assert.That(animator.speed,Is.EqualTo(priorSpeed));Assert.That(animator.applyRootMotion,Is.EqualTo(priorRootMotion));
   Assert.That(warm.IsWarm,Is.True,"Disable retains owner arrays and actor key lease");Assert.That(Phase(owner),Is.EqualTo("Neutral"));EqualPose(bones,entry,"Disable restores pose captured before entry");
   int applied=(int)Get(owner,"ExactPoseApplications");yield return null;yield return null;Assert.That((int)Get(owner,"ExactPoseApplications"),Is.EqualTo(applied));Assert.That(Resident,Is.EqualTo(residentBaseline+1));
   warm.Dispose();Assert.That(warm.IsWarm,Is.False);Assert.That((bool)Get(bridge,"IsWarm"),Is.False);Assert.That(Resident,Is.EqualTo(residentBaseline));warm.Dispose();Assert.That(Resident,Is.EqualTo(residentBaseline),"Repeated dispose cannot release another owner's packet");
  }
 }
}
#endif

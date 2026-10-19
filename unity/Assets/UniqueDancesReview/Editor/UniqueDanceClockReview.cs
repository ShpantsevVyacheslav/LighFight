using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using LighFight.Runtime;

// Tiny phase carriers are clocks, never the authored choreography. The exact LNC1
// target curves remain the sole dance-pose authority after Animator evaluation.
public static class UniqueDanceClockReview
{
 const string Root="Assets/UniqueDancesReview";
 public static void Run()
 {
  var report=new List<string>{"actor,duration,humanClock,bytes,maxBonePositionDeltaM,maxBoneAngleDeltaDeg"};
  for(int i=0;i<11;i++)
  {
   string folder=Root+"/Fighter-"+i;
   var baseline=AssetDatabase.LoadAssetAtPath<GameObject>(folder+"/Review.prefab");
   float duration=baseline.GetComponent<FighterView>().PreviewDuration("Dance");
   string cp=folder+"/Dance.clock.anim";var clip=AssetDatabase.LoadAssetAtPath<AnimationClip>(cp);
   if(!clip){clip=new AnimationClip();AssetDatabase.CreateAsset(clip,cp);}clip.ClearCurves();clip.name="DanceClock_"+i;clip.frameRate=60;
   AnimationUtility.SetEditorCurve(clip,EditorCurveBinding.FloatCurve("",typeof(Animator),HumanTrait.MuscleName[0]),AnimationCurve.Constant(0,duration,0));
   var settings=AnimationUtility.GetAnimationClipSettings(clip);settings.loopTime=false;settings.startTime=0;settings.stopTime=duration;AnimationUtility.SetAnimationClipSettings(clip,settings);
   if(Mathf.Abs(clip.length-duration)>.00001f)throw new Exception("Clock duration differs");
   EditorUtility.SetDirty(clip);AssetDatabase.SaveAssets();
   var original=AssetDatabase.LoadAssetAtPath<GameContent>("Assets/LighFight/Content/GameContent.asset").fighters[i];
   var actor=(GameObject)PrefabUtility.InstantiatePrefab(original);
   try
   {
    // Variant directly from the approved original avoids Human review dependency
    // and avoids duplicating original serialized grounding/support arrays.
    var baselineDance=baseline.GetComponent<NativeSupportOverlay>().entries.Single(e=>e.state=="Dance");
    var overlay=actor.GetComponent<NativeSupportOverlay>();
    overlay.entries=overlay.entries.Where(e=>e.state!="Dance").Concat(new[]{new NativeSupportOverlay.Entry{state="Dance",sourceName=baselineDance.sourceName,pose=baselineDance.pose,loop=false}}).ToArray();
    var animator=actor.GetComponentInChildren<Animator>();var sourceController=(AnimatorController)animator.runtimeAnimatorController;
    string controllerPath=folder+"/Clock.controller";if(!File.Exists(controllerPath))AssetDatabase.CopyAsset(AssetDatabase.GetAssetPath(sourceController),controllerPath);
    var controller=AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);var danceState=controller.layers[0].stateMachine.states.Single(s=>s.state.name=="Dance").state;danceState.motion=clip;EditorUtility.SetDirty(danceState);EditorUtility.SetDirty(controller);AssetDatabase.SaveAssets();animator.runtimeAnimatorController=controller;
    var view=actor.GetComponent<FighterView>();var field=typeof(FighterView).GetField("nativeClips",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic);
    var clips=((FighterView.NativeClip[])field.GetValue(view)).ToArray();for(int c=0;c<clips.Length;c++)if(clips[c].state=="Dance")clips[c].clip=clip;view.UseNativeMotion(clips);
    PrefabUtility.SaveAsPrefabAsset(actor,folder+"/Review.clock.prefab");
   }
   finally{foreach(var b in actor.GetComponents<MonoBehaviour>())if(b is IActorCurveWarmup warm)warm.Dispose();UnityEngine.Object.DestroyImmediate(actor);}
   var clock=AssetDatabase.LoadAssetAtPath<GameObject>(folder+"/Review.clock.prefab");
   var reference=(GameObject)PrefabUtility.InstantiatePrefab(baseline);var candidate=(GameObject)PrefabUtility.InstantiatePrefab(clock);
   float position=0,angle=0;
   try
   {
    var sourceSkin=reference.GetComponentInChildren<SkinnedMeshRenderer>();var targetSkin=candidate.GetComponentInChildren<SkinnedMeshRenderer>();
    if(sourceSkin.sharedMesh!=targetSkin.sharedMesh||!sourceSkin.sharedMaterials.SequenceEqual(targetSkin.sharedMaterials)||reference.GetComponentInChildren<Animator>().avatar!=candidate.GetComponentInChildren<Animator>().avatar)throw new Exception("Clock changed original assets");
    var sourceBones=sourceSkin.bones;var targetBones=targetSkin.bones;
    for(int sample=0;sample<=64;sample++)
    {
     float phase=sample/64f;Sample(reference,phase,duration);Sample(candidate,phase,duration);
     for(int b=0;b<sourceBones.Length;b++){position=Mathf.Max(position,Vector3.Distance(sourceBones[b].position,targetBones[b].position));angle=Mathf.Max(angle,Quaternion.Angle(sourceBones[b].rotation,targetBones[b].rotation));}
    }
   }
   finally{foreach(var a in new[]{reference,candidate}){foreach(var b in a.GetComponents<MonoBehaviour>())if(b is IActorCurveWarmup warm)warm.Dispose();UnityEngine.Object.DestroyImmediate(a);}}
   report.Add(string.Join(",",i,duration,clip.humanMotion,new FileInfo(cp).Length,position,angle));
   if(position>.00001f||angle>.06f)throw new Exception("Clock pose parity failed "+report.Last());
   var dependencies=AssetDatabase.GetDependencies(folder+"/Review.clock.prefab",true);
   File.WriteAllLines("../art/source/unique-dances-20261018/audit/clock-dependencies-"+i+".txt",dependencies);
   File.WriteAllLines("../art/source/unique-dances-20261018/audit/clock-editor-parity.csv",report);
   var redundant=dependencies.Where(p=>p.StartsWith(Root+"/Sources/")||p==folder+"/Dance.carrier.anim").ToArray();
   if(redundant.Length>0)throw new Exception("Clock still depends on redundant donor Human clip: "+string.Join(";",redundant));
  }
  File.WriteAllLines("../art/source/unique-dances-20261018/audit/clock-editor-parity.csv",report);
  Debug.Log("AGENT_RESULT 11 tiny full-duration clock candidates; editor pose parity; actual player still required");
 }
 static void Sample(GameObject actor,float phase,float duration){var view=actor.GetComponent<FighterView>();view.FullPreviewMotion=true;view.PreviewNativeMotion("Dance",phase*duration);var animator=actor.GetComponentInChildren<Animator>();var state=animator.GetCurrentAnimatorStateInfo(0);if(state.shortNameHash!=Animator.StringToHash("Dance")||Mathf.Abs(state.normalizedTime-Mathf.Min(phase,.999999f))>.0001f||Mathf.Abs(state.length-duration)>.001f)throw new Exception("Carrier did not drive the real Animator state/phase/duration");actor.GetComponent<NativeSupportOverlay>().EvaluateForReview("Dance",phase,0,false);actor.GetComponent<CandidateFootGrounding>().ApplyForReview();}
}

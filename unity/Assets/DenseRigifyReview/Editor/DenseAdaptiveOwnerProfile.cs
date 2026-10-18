using System;
using System.Collections;
using System.Diagnostics;
using System.IO;
using UnityEditor;
using UnityEngine;
using Unity.Profiling;
using Debug=UnityEngine.Debug;

public static partial class DenseRigifyReimportReview
{
 public static void RunAdaptiveOwnerProfile(){if(pendingReview!=null)throw new Exception("Review active");Output=BaseOutput+"/cached-clock-owner-profile-v1";pendingReview=AdaptiveOwnerProfileSteps();reviewTicks=0;EditorApplication.update+=PumpReview;EditorApplication.QueuePlayerLoopUpdate();}
 static long AllocatorEvents(ProfilerRecorder recorder){long sum=0;for(int i=0;i<recorder.Count;i++)sum+=recorder.GetSample(i).Value;return sum;}
 static IEnumerator AdaptiveOwnerProfileSteps()
 {
  Directory.CreateDirectory(Output);var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(PlayableRoot+"/SailorDenseBOXReview.prefab");if(!prefab)throw new Exception("Actual standalone actor required");
  GameObject actor=null;DenseAdaptiveReviewOwner owner=null;ProfilerRecorder recorder=default;
  int resident=DenseAdaptivePoseBridge.ResidentKeyPackets;bool priorProfiler=UnityEngine.Profiling.Profiler.enabled;
  try
  {
   actor=UnityEngine.Object.Instantiate(prefab);owner=actor.GetComponent<DenseAdaptiveReviewOwner>();var animator=actor.GetComponentInChildren<Animator>();animator.Rebind();animator.Update(0);
   var warm=owner.Warmup();while(warm.MoveNext())yield return warm.Current;(warm as IDisposable)?.Dispose();
   if(!owner.IsWarm)throw new Exception("Owner warmup incomplete");
   File.WriteAllText(Output+"/cold-key-warmup.txt","Synchronous decode+fixedbind calibration ms="+owner.bridge.LastDecodeAndBindMilliseconds+"; excludes prior visible-loading yield, supportpoint decode and geometry/material setup. Source2535Float64keys27621700bytes; no perframe decode. Editor host only, not phone CPU.\n");
   // Calibrate the previously unqualified thread counter against an observable allocation.
   long threadBefore=GC.GetAllocatedBytesForCurrentThread(),heapBefore=GC.GetTotalMemory(false);
   UnityEngine.Profiling.Profiler.enabled=true;
   recorder=ProfilerRecorder.StartNew(ProfilerCategory.Memory,"GC.Alloc",1024,ProfilerRecorderOptions.StartImmediately|ProfilerRecorderOptions.CollectOnlyOnCurrentThread);
   if(recorder.Valid)recorder.Reset();
   var sentinel=new byte[1048576];sentinel[123]=89;GC.KeepAlive(sentinel);
   long threadDelta=GC.GetAllocatedBytesForCurrentThread()-threadBefore,heapDelta=GC.GetTotalMemory(false)-heapBefore;
   long markerSentinel=recorder.Valid?AllocatorEvents(recorder):-1;int sentinelEvents=recorder.Valid?recorder.Count:-1;bool immediateMarkerQualified=recorder.Valid&&sentinelEvents>0;string markerUnit=recorder.Valid?recorder.UnitType.ToString():"Unavailable";
   if(recorder.Valid)recorder.Stop();
   File.WriteAllText(Output+"/allocation-positive-calibration.txt","KnownnewbyteLength="+sentinel.Length+" observedByte="+sentinel[123]+" threadCounterDelta="+threadDelta+" heapDelta="+heapDelta+" GC.AllocValid="+recorder.Valid+" immediateMarkerValue="+markerSentinel+" unit="+markerUnit+" events="+sentinelEvents+" immediateMarkerQualified="+immediateMarkerQualified+". Threadcounter0 cannot establish zero allocation; marker result valid only with positive sentinel.\n");
   owner.PlayDance();for(int i=0;i<40;i++)owner.AdvanceReviewFrame(1f/120);
   var ticks=new long[128];if(recorder.Valid){recorder.Reset();recorder.Start();}
   for(int i=0;i<ticks.Length;i++){long start=Stopwatch.GetTimestamp();owner.AdvanceReviewFrame(1f/120);ticks[i]=Stopwatch.GetTimestamp()-start;}
   if(recorder.Valid)recorder.Stop();long markerLoop=recorder.Valid?AllocatorEvents(recorder):-1;int markerEvents=recorder.Valid?recorder.Count:-1;
   Array.Sort(ticks);double ms=1000.0/Stopwatch.Frequency;
   File.WriteAllText(Output+"/owner-profile.txt","128changedposes owner+cachedAnimatorClock+exactBridge p50ms="+ticks[64]*ms+" p95ms="+ticks[121]*ms+" maxms="+ticks[127]*ms+" GC.AllocValue="+markerLoop+" unit="+markerUnit+" markerEvents="+markerEvents+" qualified="+immediateMarkerQualified+"; Editor walltime only. Grounding excluded (independent floor gate remains RED/open), not phone or quietCPU.\n");
   if(immediateMarkerQualified&&markerEvents!=0)throw new Exception("Warm cached-clock owner allocated in qualified marker scope");
   owner.ReturnToNeutral();for(int i=0;i<40;i++)owner.AdvanceReviewFrame(1f/120);
   if(owner.Active)throw new Exception("Owner did not return to neutral");int applications=owner.ExactPoseApplications;
   for(int i=0;i<128;i++)owner.AdvanceReviewFrame(1f/120);
   if(owner.ExactPoseApplications!=applications)throw new Exception("Neutral owner evaluated source poses");
   File.WriteAllText(Output+"/COMPLETE.txt","Cached-clock owner changed-pose profile and positive allocation calibration complete; allocation acceptance="+immediateMarkerQualified+". Actual grounding/player/mobile remains open.");Debug.Log("AGENT_RESULT cached-clock owner profile complete");
  }
  finally{if(recorder.Valid)recorder.Dispose();UnityEngine.Profiling.Profiler.enabled=priorProfiler;if(owner)owner.Dispose();if(actor)UnityEngine.Object.DestroyImmediate(actor);if(DenseAdaptivePoseBridge.ResidentKeyPackets!=resident)throw new Exception("Profile actor leaked or released another key packet");}
 }
}

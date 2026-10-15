#if UNITY_EDITOR
using System;using System.IO;using System.Linq;using System.Collections;using System.Collections.Generic;using System.Globalization;using System.Reflection;using System.Security.Cryptography;
using UnityEditor;using UnityEngine;using LighFight.Runtime;using LighFight.Core;
// Read-only shipping prefab gate. All pose/bone perturbations are on disposable instances.
public static class KickGroundingHeldPoseGate {
 const string Output="/Users/macbook1/Documents/ChatGPT/LighFight/.tools/kick-performance-review/HeldPoseEvidence";
 static double deadline;static int cases;static float maximum;static readonly List<string> rows=new List<string>();
 public static void Run(){
  Directory.CreateDirectory(Output);rows.Clear();rows.Add("slot,prefab,state,commandPhase,observedHash,observedPhase,points,legacyMinimum,newMinimum,error,correction,repeatScanDelta,boneChangeScanDelta,restoredScanDelta");cases=0;maximum=0;deadline=EditorApplication.timeSinceStartup+240;
  try{
   var catalog=AssetDatabase.LoadAssetAtPath<GameContent>("Assets/LighFight/Content/GameContent.asset");Require(catalog&&catalog.fighters!=null&&catalog.fighters.Length==11,"Actual11 inline shipping catalog required");
   var inputs=AssetDatabase.GetDependencies(catalog.fighters.Select(AssetDatabase.GetAssetPath).ToArray(),true).Where(File.Exists).Where(p=>!p.EndsWith(".cs")).OrderBy(p=>p,StringComparer.Ordinal).ToDictionary(p=>p,Hash);
   File.WriteAllLines(Output+"/artist-before.sha256",inputs.Select(p=>p.Value+" "+p.Key));
   for(int slot=0;slot<catalog.fighters.Length;slot++)RunActor(slot,catalog.fighters[slot]);
   foreach(var p in inputs)Require(Hash(p.Key)==p.Value,"Source asset changed "+p.Key);
   File.WriteAllLines(Output+"/artist-after.sha256",inputs.Select(p=>Hash(p.Key)+" "+p.Key));
   File.WriteAllLines(Output+"/poses.csv",rows);File.WriteAllText(Output+"/result.json","{\"completed\":true,\"failed\":false,\"actors\":11,\"cases\":"+cases+",\"maxErrorMeters\":"+N(maximum)+"}");Debug.Log("AGENT_RESULT GroundingHeldPoseGate PASS actors=11 cases="+cases+" maxError="+N(maximum));
  }catch(Exception e){File.WriteAllLines(Output+"/poses.csv",rows);File.WriteAllText(Output+"/failure.txt",e.ToString());File.WriteAllText(Output+"/result.json","{\"completed\":false,\"failed\":true}");Debug.LogError("AGENT_RESULT GroundingHeldPoseGate RED "+e.Message);throw;}
 }
 static void RunActor(int slot,GameObject prefab){
  var actor=UnityEngine.Object.Instantiate(prefab);var leases=new List<IActorCurveWarmup>();
  try{
   var view=actor.GetComponent<FighterView>();var g=actor.GetComponent<CandidateFootGrounding>();var animator=actor.GetComponentInChildren<Animator>();Require(view&&view.UsesNativeMotion&&g&&animator&&animator.avatar&&animator.avatar.isValid&&animator.avatar.isHuman,"Shipping Human/native/grounding required "+prefab.name);
   Require(animator.avatar==prefab.GetComponentInChildren<Animator>().avatar,"Shipping Avatar reference changed");
   var sourceSkins=prefab.GetComponentsInChildren<SkinnedMeshRenderer>(true);var actualSkins=actor.GetComponentsInChildren<SkinnedMeshRenderer>(true);Require(sourceSkins.Length==actualSkins.Length,"Renderer count changed");
   for(int i=0;i<sourceSkins.Length;i++)Require(actualSkins[i].sharedMesh==sourceSkins[i].sharedMesh&&actualSkins[i].bones.Length==sourceSkins[i].bones.Length,"Shipping mesh/bone palette reference changed");
   foreach(var warmup in actor.GetComponentsInChildren<MonoBehaviour>(true).OfType<IActorCurveWarmup>()){
    leases.Add(warmup);var step=warmup.Warmup();try{while(step.MoveNext()){CheckDeadline();Require(step.Current==null,"Unexpected async warmup: use async runner before accepting");}}finally{(step as IDisposable)?.Dispose();}Require(warmup.IsWarm,"Warmup incomplete");
   }
   var other=new Fighter{X=1,Guard=true};
   foreach(string state in new[]{"Crouch","WalkBack","frontKick","jumpKick","Fall","GetUp"})foreach(float phase in new[]{0f,.25f,.5f,.75f,.99f}){
    CheckDeadline();var f=new Fighter{X=0,FacingX=1,Guard=false};
    if(state=="Crouch")f.Crouch=true;
    if(state=="frontKick"||state=="jumpKick"){var move=Moves.All.Single(m=>m.Id==state);f.Attack=move;f.Frame=Mathf.Clamp(Mathf.RoundToInt(phase*move.Duration),0,move.Duration-1);}
    if(state=="Fall")f.DownTicks=Mathf.Clamp(Match.GetUpTicks+Mathf.RoundToInt((1-phase)*(Match.KnockdownTicks-Match.GetUpTicks)),Match.GetUpTicks+1,Match.KnockdownTicks);
    if(state=="GetUp")f.DownTicks=Mathf.Clamp(Mathf.RoundToInt((1-phase)*Match.GetUpTicks),1,Match.GetUpTicks);
    // Advance real production blend to a held pose; no bone edits for the native/source sample.
    view.PoseTowards(f,other,1);
    Set(view,"nativeTime",phase*Mathf.Max(.001f,view.PreviewDuration(state)));Set(view,"motionHold",0f);Set(view,"previousX",state=="WalkBack"?.02f:0f);Set(view,"previousZ",0f);Set(view,"stride",0f);
    view.SampleContactPose(f,other);
    var info=animator.GetCurrentAnimatorStateInfo(0);Require(info.shortNameHash==Animator.StringToHash(state),"Commanded shipping state not sampled: "+prefab.name+" "+state);
    g.ResetForSample();float legacy=Legacy(g);float value=g.MeasureMinimum();float error=Mathf.Abs(legacy-value);maximum=Mathf.Max(maximum,error);Require(error<=1e-5f,"Legacy XYZ minimum mismatch");
    int count=g.MinimumEvaluationCount;float repeat=g.MeasureMinimum();Require(g.MinimumEvaluationCount==count&&repeat.Equals(value),"Identical measurement rescanned");
    float expected=Mathf.Clamp(-legacy,0,g.maxCorrection);var before=g.nativeRig.position;
    g.ApplyForReview();Require(Mathf.Abs(g.UncorrectedMinimum-legacy)<=1e-5f&&Mathf.Abs(g.LastCorrection-expected)<=1e-5f,"Apply correction/raw minimum changed");Require(Vector3.Distance(g.nativeRig.position,before+Vector3.up*expected)<=1e-5f,"Whole-rig correction changed");
    g.ApplyForReview();Require(g.MinimumEvaluationCount==count,"Repeated apply rescanned identical rest matrices");
    // Cache invalidation diagnostic only: disposable clone perturbation, restored before next source pose.
    var bone=g.bones.First(t=>t!=g.nativeRig);var old=bone.localPosition;int changedDelta,restoredDelta;
    try{g.ResetForSample();bone.localPosition=old+new Vector3(.0007f,.0003f,-.0002f);float changedLegacy=Legacy(g);int n=g.MinimumEvaluationCount;float changed=g.MeasureMinimum();changedDelta=g.MinimumEvaluationCount-n;Require(changedDelta==1&&Mathf.Abs(changed-changedLegacy)<=1e-5f,"Changed bone matrix did not invalidate/cache math mismatch");}
    finally{bone.localPosition=old;}
    int restoredBefore=g.MinimumEvaluationCount;float restored=g.MeasureMinimum();restoredDelta=g.MinimumEvaluationCount-restoredBefore;Require(restoredDelta==1&&Mathf.Abs(restored-legacy)<=1e-5f,"Restored bone pose reused stale minimum");
    cases++;rows.Add(string.Join(",",slot,prefab.name,state,N(phase),info.shortNameHash,N(info.normalizedTime),g.shoePoints.Length,N(legacy),N(value),N(error),N(expected),0,changedDelta,restoredDelta));
   }
  }finally{foreach(var warm in leases){warm.CancelWarmup();warm.Dispose();}UnityEngine.Object.DestroyImmediate(actor);}
 }
 static float Legacy(CandidateFootGrounding g){g.ResetForSample();var matrices=g.bones.Select(t=>t.localToWorldMatrix).ToArray();float minimum=float.PositiveInfinity;foreach(var point in g.shoePoints){Vector3 weighted=Vector3.zero;foreach(var influence in point.influences)weighted+=matrices[influence.bone].MultiplyPoint3x4(influence.bindPoint)*influence.weight;minimum=Mathf.Min(minimum,weighted.y);}return minimum;}
 static void Set(FighterView view,string field,float value){var f=typeof(FighterView).GetField(field,BindingFlags.Instance|BindingFlags.NonPublic);Require(f!=null,"QA clock/context field missing "+field);f.SetValue(view,value);}
 static string Hash(string p){using(var sha=SHA256.Create())return BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(p))).Replace("-","").ToLowerInvariant();}
 static string N(double n)=>n.ToString("R",CultureInfo.InvariantCulture);static void Require(bool v,string message){if(!v)throw new InvalidOperationException(message);}static void CheckDeadline(){Require(EditorApplication.timeSinceStartup<deadline,"Held pose gate240s deadline");}
}
#endif

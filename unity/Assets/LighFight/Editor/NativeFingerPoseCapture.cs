using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using LighFight.Runtime;
namespace LighFight.Editor {
 public static class NativeFingerPoseCapture {
  public static NativeFingerPose Capture(GameObject model,AnimationClip clip,float seconds,string output){
   if(!model||!clip||seconds<0||seconds>clip.length)throw new ArgumentException("Invalid native finger pose source");
   var instance=UnityEngine.Object.Instantiate(model);
   try{
    var animator=instance.GetComponentInChildren<Animator>();if(!animator||!animator.avatar)throw new InvalidOperationException("Missing native avatar");
    animator.Rebind();animator.Update(0);animator.enabled=false;clip.SampleAnimation(instance,seconds);
    var entries=animator.GetComponentsInChildren<Transform>().Where(t=>IsFinger(t.name)).Select(t=>new NativeFingerPose.Joint{path=AnimationUtility.CalculateTransformPath(t,animator.transform),rotation=t.localRotation}).ToArray();
    if(entries.Length!=30)throw new InvalidOperationException("Expected thirty articulated finger joints, got "+entries.Length);
    var asset=AssetDatabase.LoadAssetAtPath<NativeFingerPose>(output);if(!asset){asset=ScriptableObject.CreateInstance<NativeFingerPose>();AssetDatabase.CreateAsset(asset,output);}
    asset.sourceAvatar=animator.avatar;asset.joints=entries;asset.sourceClip=clip.name;asset.sourceSeconds=seconds;EditorUtility.SetDirty(asset);AssetDatabase.SaveAssets();return asset;
   }finally{UnityEngine.Object.DestroyImmediate(instance);}
  }
  static bool IsFinger(string name){if(name.EndsWith("End",StringComparison.OrdinalIgnoreCase))return false;return new[]{"Thumb","Index","Middle","Ring","Pinky","Little"}.Any(n=>name.IndexOf(n,StringComparison.OrdinalIgnoreCase)>=0);}
 }
}

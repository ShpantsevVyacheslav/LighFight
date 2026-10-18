using NUnit.Framework;
using UnityEngine;
using LighFight.Runtime;
namespace LighFight.Tests {
 public sealed class NativeMotionLookupTests {
  GameObject root;AnimationClip first,second;FighterView view;
  [SetUp]public void Setup(){root=new GameObject("Native lookup contracts");view=root.AddComponent<FighterView>();first=Clip(1);second=Clip(2);}
  AnimationClip Clip(float duration){var clip=new AnimationClip();clip.SetCurve("",typeof(Transform),"localPosition.x",AnimationCurve.Linear(0,0,duration,1));return clip;}
  [TearDown]public void Cleanup(){Object.DestroyImmediate(root);Object.DestroyImmediate(first);Object.DestroyImmediate(second);}
  [Test]public void ReplacingAndUpdatingClipTableDoesNotRetainStaleLookup(){
   var clips=new[]{new FighterView.NativeClip{state="Dance",clip=first}};view.UseNativeMotion(clips);
   Assert.AreEqual(1,view.PreviewDuration("Dance"));Assert.IsFalse(view.HasNativeMotion("Kick"));Assert.AreEqual(0,view.PreviewDuration("Kick"));
   clips[0]=new FighterView.NativeClip{state="Kick",clip=second};Assert.IsFalse(view.HasNativeMotion("Dance"));Assert.AreEqual(2,view.PreviewDuration("Kick"));
   view.UseNativeMotion(new[]{new FighterView.NativeClip{state="Dance",clip=second}});Assert.AreEqual(2,view.PreviewDuration("Dance"));Assert.IsFalse(view.HasNativeMotion("Kick"));
  }
  [Test]public void DuplicateClipsRetainFirstMatchAndNullClipDoesNotCreateMotion(){
   view.UseNativeMotion(new[]{new FighterView.NativeClip{state="Dance",clip=first},new FighterView.NativeClip{state="Dance",clip=second},new FighterView.NativeClip{state="Missing",clip=null}});
   Assert.AreEqual(1,view.PreviewDuration("Dance"));Assert.IsFalse(view.HasNativeMotion("Missing"));Assert.AreEqual(0,view.PreviewDuration("Missing"));
  }
  [Test]public void ContactSideUsesFirstMatchAndReconfigurationImmediately(){
   view.ConfigureAnimation(null,new[]{new FighterView.ContactPose{moveId="jab",strikeSide=1},new FighterView.ContactPose{moveId="jab",strikeSide=2}});view.UseNativeMotion();
   Assert.AreEqual(1,view.NativeStrikeSide("jab"));Assert.AreEqual(0,view.NativeStrikeSide("unknown"));
   view.ConfigureAnimation(null,new[]{new FighterView.ContactPose{moveId="jab",strikeSide=2}});Assert.AreEqual(2,view.NativeStrikeSide("jab"));
  }
 }
}

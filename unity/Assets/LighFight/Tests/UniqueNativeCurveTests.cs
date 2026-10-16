using System;using System.Linq;using System.Reflection;using NUnit.Framework;using UnityEngine;
// NativeSupportPoseData lives in Assembly-CSharp; this test remains in the existing Tests asmdef.
public sealed class UniqueNativeCurveTests {
 ScriptableObject data;Type type,jointType;GameObject root;Transform[] targets;
 [SetUp]public void Setup(){type=AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("NativeSupportPoseData",false)).FirstOrDefault(t=>t!=null);Assert.NotNull(type);jointType=type.GetNestedType("Joint");data=ScriptableObject.CreateInstance(type);root=new GameObject("unique-curve-test");targets=new[]{root.transform};}
 [TearDown]public void Cleanup(){UnityEngine.Object.DestroyImmediate(root);UnityEngine.Object.DestroyImmediate(data);}
 object Joint(AnimationCurve px=null,AnimationCurve py=null,AnimationCurve pz=null,AnimationCurve qx=null,AnimationCurve qy=null,AnimationCurve qz=null,AnimationCurve qw=null){object j=Activator.CreateInstance(jointType);string[] names={"px","py","pz","qx","qy","qz","qw"};AnimationCurve[] curves={px,py,pz,qx,qy,qz,qw};for(int i=0;i<names.Length;i++)jointType.GetField(names[i]).SetValue(j,curves[i]);return j;}
 void SetJoint(object j){var a=Array.CreateInstance(jointType,1);a.SetValue(j,0);type.GetProperty("joints").SetValue(data,a);}
 void Apply(float seconds){try{type.GetMethod("Apply").Invoke(data,new object[]{targets,seconds});}catch(TargetInvocationException e){throw e.InnerException;}}
 int Count(){var p=type.GetProperty("LastCurveEvaluationCount");Assert.NotNull(p,"Per-Apply unique evaluator diagnostic missing");return (int)p.GetValue(data);}
 int Users()=>(int)type.GetProperty("ActiveUsers").GetValue(data);
 IDisposable Pin()=>(IDisposable)type.GetMethod("PinAndPrewarm").Invoke(data,null);
 [Test]public void SharedCurveObjectIsEvaluatedOncePerApply(){var c=AnimationCurve.Linear(0,0,1,1);SetJoint(Joint(c,c,c));Apply(.25f);Assert.AreEqual(1,Count());Assert.AreEqual(new Vector3(.25f,.25f,.25f),root.transform.localPosition);Apply(.75f);Assert.AreEqual(1,Count());Assert.AreEqual(new Vector3(.75f,.75f,.75f),root.transform.localPosition);}
 [Test]public void EqualButSeparateCurveObjectsKeepSeparateIdentity(){var a=AnimationCurve.Linear(0,0,1,1);var b=AnimationCurve.Linear(0,0,1,1);SetJoint(Joint(a,b,a));Apply(.4f);Assert.AreEqual(2,Count());}
 [Test]public void InPlacePublicCurveReplacementCannotUseStalePlan(){var a=AnimationCurve.Constant(0,1,1);var b=AnimationCurve.Constant(0,1,3);object j=Joint(a,a,a);SetJoint(j);Apply(.4f);jointType.GetField("py").SetValue(j,b);Apply(.4f);Assert.AreEqual(2,Count());Assert.AreEqual(new Vector3(1,3,1),root.transform.localPosition);}
 [Test]public void MutatedCurveKeysAtSameSecondsAreReevaluated(){var c=AnimationCurve.Constant(0,1,1);SetJoint(Joint(c,c,c));Apply(.5f);c.keys=new[]{new Keyframe(0,4),new Keyframe(1,4)};Apply(.5f);Assert.AreEqual(new Vector3(4,4,4),root.transform.localPosition);}
 [Test]public void LeaseAndReleaseDoNotRetainDecodedPlan(){var c=AnimationCurve.Constant(0,1,1);SetJoint(Joint(c,c,c));using(Pin()){Assert.AreEqual(1,Users());Apply(0);}Assert.AreEqual(0,Users());Apply(.5f);Assert.AreEqual(1,Count());}
 [Test]public void MissingChannelsPreserveExistingTransformAndQuaternionNormalization(){var c=AnimationCurve.Constant(0,1,2);root.transform.localPosition=new Vector3(3,4,5);root.transform.localScale=new Vector3(6,7,8);SetJoint(Joint(qx:c,qy:c,qz:c,qw:c));Apply(.5f);Assert.AreEqual(new Vector3(3,4,5),root.transform.localPosition);Assert.AreEqual(new Vector3(6,7,8),root.transform.localScale);Assert.AreEqual(Quaternion.Normalize(new Quaternion(2,2,2,2)),root.transform.localRotation);Assert.AreEqual(1,Count());}
 [Test]public void IncompleteLegacyChannelGroupKeepsOriginalFailureAndEarlierWrites(){var c=AnimationCurve.Constant(0,1,2);SetJoint(Joint(c,c,c,c));using(Pin()){Assert.Throws<NullReferenceException>(()=>Apply(0));Assert.AreEqual(new Vector3(2,2,2),root.transform.localPosition);}}
}

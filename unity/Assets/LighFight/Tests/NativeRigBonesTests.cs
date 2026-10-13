using NUnit.Framework;
using UnityEngine;
using LighFight.Runtime;
namespace LighFight.Tests {
 public class NativeRigBonesTests {
  GameObject root;
  [TearDown] public void Clean(){if(root)Object.DestroyImmediate(root);}
  FighterView Rig(){root=new GameObject("Native contact rig");var view=root.AddComponent<FighterView>();view.ConfigureAnimation(root.AddComponent<Animator>(),new FighterView.ContactPose[0]);view.UseNativeMotion();return view;}
  void Bone(string name,Vector3 position){var bone=new GameObject(name).transform;bone.SetParent(root.transform,false);bone.position=position;}
  [TestCase("Left_Hand","LeftHand")]
  [TestCase("Right_LowerArm","RightForeArm")]
  [TestCase("Left_LowerLeg","LeftLeg")]
  [TestCase("Right_Toes","RightToeBase")]
  [TestCase("Left_MiddleProximal","LeftHandMiddle1")]
  public void TripoContactUsesActualJoint(string source,string contact){var view=Rig();var expected=new Vector3(2.3f,1.7f,-.9f);Bone(source,expected);Assert.That(view.BonePoint(contact,Vector3.zero),Is.EqualTo(expected));}
  [Test] public void ClosedFistContactUsesThreeNativeKnuckles(){var view=Rig();Bone("Left_Hand",new Vector3(0,1,0));Bone("Left_LowerArm",new Vector3(0,.8f,0));Bone("Left_IndexProximal",new Vector3(.1f,1,.2f));Bone("Left_MiddleProximal",new Vector3(.2f,1,.2f));Bone("Left_RingProximal",new Vector3(.3f,1,.2f));var center=view.StrikeHandCenter(true);Assert.That(float.IsNaN(center.sqrMagnitude)||float.IsInfinity(center.sqrMagnitude),Is.False);Assert.That(Vector3.Distance(center,new Vector3(.2f,1,.2f)),Is.LessThan(.00001f));}
  [Test] public void LegacyMixamoJointStillResolves(){var view=Rig();var expected=new Vector3(-2,1,3);Bone("mixamorig:RightHand",expected);Assert.That(view.BonePoint("RightHand",Vector3.zero),Is.EqualTo(expected));}
  [Test] public void MissingAnimatedJointCannotBecomeSyntheticContact(){var view=Rig();Assert.That(float.IsNaN(view.BonePoint("LeftHand",Vector3.zero).x),Is.True);}
 }
}

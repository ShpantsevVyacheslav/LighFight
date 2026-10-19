#if UNITY_EDITOR
using System;using System.Reflection;using NUnit.Framework;using UnityEditor;using UnityEngine;
namespace LighFight.Tests {
 public sealed class UniqueDanceBindCalibrationTests {
  const string Folder="Assets/UniqueDancesReview/Fighter-9/";
  static T Field<T>(Component c,string n)=>(T)c.GetType().GetField(n).GetValue(c);
  static Quaternion SkinBindRotation(SkinnedMeshRenderer skin,string bone){int i=Array.FindIndex(skin.bones,b=>b.name==bone);Assert.GreaterOrEqual(i,0,"Original bind bone "+bone);return (skin.transform.localToWorldMatrix*skin.sharedMesh.bindposes[i].inverse).rotation;}
  static double Angle(Quaternion a,Quaternion b){double d=(double)a.x*b.x+(double)a.y*b.y+(double)a.z*b.z+(double)a.w*b.w,aa=(double)a.x*a.x+(double)a.y*a.y+(double)a.z*a.z+(double)a.w*a.w,bb=(double)b.x*b.x+(double)b.y*b.y+(double)b.z*b.z+(double)b.w*b.w;return 2*Math.Acos(Math.Min(1,Math.Abs(d)/Math.Sqrt(aa*bb)))*180/Math.PI;}
  [Test]public void SourceAndTargetGirdleUseOriginalInverseBindRatherThanSerializedDanceGuard(){
   var source=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/UniqueDancesReview/GenericSources/action-74.fbx"));var current=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(Folder+"Review.exact-generic-anatomic-v4.prefab"));var rejected=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(Folder+"Review.exact-generic-anatomic-v3.prefab"));
   try{var driver=current.GetComponent("ExactGenericDanceRetarget");var old=rejected.GetComponent("ExactGenericDanceRetarget");var sourceSkin=source.GetComponentInChildren<SkinnedMeshRenderer>();var targetSkin=current.GetComponentInChildren<SkinnedMeshRenderer>();var sourceRest=Field<Quaternion[]>(driver,"sourceRestRotations");var targetRest=Field<Quaternion[]>(driver,"targetCanonicalRotations");var oldSource=Field<Quaternion[]>(old,"sourceRestRotations");var oldTarget=Field<Quaternion[]>(old,"targetCanonicalRotations");var inverse=Quaternion.Inverse(current.transform.rotation);
    foreach(int i in new[]{6,10}){string side=i==6?"Left":"Right";var sourceExpected=SkinBindRotation(sourceSkin,side+"Shoulder");var targetExpected=inverse*SkinBindRotation(targetSkin,side+"_Shoulder");Assert.Less(Angle(sourceRest[i],sourceExpected),.01,"Donor girdle must use true bind, not animated FBX Model default");Assert.Less(Angle(targetRest[i],targetExpected),.01,"Target anatomical girdle must use true bind, not approved serialized guard");Assert.Greater(Angle(oldSource[i],sourceExpected),1,"Rejected donor-default fixture must still witness the original bug");Assert.Greater(Angle(oldTarget[i],targetExpected),1,"Rejected guard-default fixture must still witness the original bug");}
    Assert.AreEqual(0,Field<Transform>(driver,"sourceRoot").GetComponentsInChildren<Renderer>(true).Length,"Runtime source skeleton cannot retain donor mesh");Assert.AreSame(targetSkin.sharedMesh,rejected.GetComponentInChildren<SkinnedMeshRenderer>().sharedMesh,"Calibration preserves exact approved target source mesh");
   }finally{UnityEngine.Object.DestroyImmediate(source);UnityEngine.Object.DestroyImmediate(current);UnityEngine.Object.DestroyImmediate(rejected);}
  }
 }
}
#endif

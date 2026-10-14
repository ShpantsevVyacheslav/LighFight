using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using LighFight.Core;
using LighFight.Runtime;

namespace LighFight.Tests
{
 public class SharedCachedHipPoseTests
 {
  [UnityTest]
  public IEnumerator SharedCachedPoseKeepsCapturedHipXZAfterCurveWarmup()
  {
   yield return new EnterPlayMode();
   var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/LighFight/Art/Drafts/TripoSailorPlayable/SailorHumanoidCandidate.prefab");
   Assert.That(prefab,Is.Not.Null);
   var root=Object.Instantiate(prefab);
   try
   {
    var overlay=root.GetComponents<MonoBehaviour>().Single(c=>c.GetType().Name=="NativeSupportOverlay");
    var field=overlay.GetType().GetField("hipBindAnchor");
    field.SetValue(overlay,(Vector3)field.GetValue(overlay)+new Vector3(.09f,0,.12f));
    overlay.GetType().GetMethod("Initialize").Invoke(overlay,null);
    var warm=(IActorCurveWarmup)overlay;
    yield return warm.Warmup();
    Assert.That(warm.IsWarm,Is.True);
    var view=root.GetComponent<FighterView>();
    view.SampleContactPose(new Fighter{Guard=true},new Fighter{X=1});
    var animator=root.GetComponentInChildren<Animator>();
    var hip=animator.GetBoneTransform(HumanBodyBones.Hips);
    var before=animator.transform.InverseTransformPoint(hip.position);
    overlay.GetType().GetMethod("LateUpdate",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(overlay,null);
    var after=animator.transform.InverseTransformPoint(hip.position);
    Assert.That(Vector2.Distance(new Vector2(before.x,before.z),new Vector2(after.x,after.z)),Is.LessThan(.0001f));
    yield return new WaitForEndOfFrame();
   }
   finally
   {
    foreach(var c in root.GetComponents<MonoBehaviour>())if(c is IActorCurveWarmup warm)warm.Dispose();
    Object.Destroy(root);
   }
   yield return null;
   yield return new ExitPlayMode();
  }
 }
}

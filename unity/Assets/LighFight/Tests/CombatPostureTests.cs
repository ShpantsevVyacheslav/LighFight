using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using LighFight.Core;
using LighFight.Runtime;
namespace LighFight.Tests {
 public sealed class CombatPostureTests {
  const string Layer="Review independent source arms";
  [UnityTest]public IEnumerator ProtectedRetreatAndCrouchedStrikeUseRequestedSourceStates(){
   yield return new EnterPlayMode();
   var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/LighFight/Art/Drafts/TripoIslandPlayable/IslandHumanoidCandidate.prefab");Assert.NotNull(prefab);
   var actor=Object.Instantiate(prefab);var leases=new List<IActorCurveWarmup>();
   try{
    foreach(var warm in actor.GetComponentsInChildren<MonoBehaviour>(true).OfType<IActorCurveWarmup>()){leases.Add(warm);yield return warm.Warmup();Assert.True(warm.IsWarm);}
    var view=actor.GetComponent<FighterView>();var anim=actor.GetComponentInChildren<Animator>();int layer=anim.GetLayerIndex(Layer);Assert.GreaterOrEqual(layer,0);
    var m=new Match(true);m.A.X=-.6f;m.B.X=.6f;m.A.Guard=true;view.PoseTowards(m.A,m.B,1f/60);
    m.A.X-=.03f;view.PoseTowards(m.A,m.B,1f/60);
    Assert.AreEqual(Animator.StringToHash("WalkBack"),anim.GetCurrentAnimatorStateInfo(0).shortNameHash,"Holding block must preserve backward gait");
    Assert.AreEqual(Animator.StringToHash(Layer+".SourceGuard"),anim.GetCurrentAnimatorStateInfo(layer).fullPathHash);Assert.AreEqual(.2f,anim.GetCurrentAnimatorStateInfo(layer).normalizedTime,.00001f);
    m.A.Crouch=true;view.PoseTowards(m.A,m.B,1f/60);Assert.AreEqual(Animator.StringToHash("Crouch"),anim.GetCurrentAnimatorStateInfo(0).shortNameHash,"Low guard must remain crouched");
    Assert.True(m.Attack(m.A,7));Assert.True(m.A.Crouch);Assert.False(m.A.Guard);
    m.A.Frame=22;view.SampleContactPose(m.A,m.B);
    Assert.AreEqual(Animator.StringToHash("CrouchUppercut"),anim.GetCurrentAnimatorStateInfo(0).shortNameHash);Assert.AreEqual(.3f,anim.GetCurrentAnimatorStateInfo(0).normalizedTime,.00001f);
    Assert.AreEqual(Animator.StringToHash(Layer+".SourceUppercut"),anim.GetCurrentAnimatorStateInfo(layer).fullPathHash,"An attack cannot silently remain on the guard pose");Assert.Greater(anim.GetCurrentAnimatorStateInfo(layer).normalizedTime,.4f);Assert.AreEqual(1,anim.GetLayerWeight(layer));
    m.A.Attack=null;view.PoseTowards(m.A,m.B,.1f);Assert.AreEqual(Animator.StringToHash("Crouch"),anim.GetCurrentAnimatorStateInfo(0).shortNameHash);Assert.AreEqual(Animator.StringToHash(Layer+".SourceGuard"),anim.GetCurrentAnimatorStateInfo(layer).fullPathHash);
   }finally{foreach(var lease in leases)lease.Dispose();Object.Destroy(actor);}
   yield return null;yield return new ExitPlayMode();
  }
 }
}

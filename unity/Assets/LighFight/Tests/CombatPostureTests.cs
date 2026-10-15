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
  [Test] public void NativeRosterHasDistinctCoverAndFourFullSourceLowStrikes(){
   var catalog=AssetDatabase.LoadAssetAtPath<GameContent>("Assets/LighFight/Content/GameContent.asset");Assert.NotNull(catalog);Assert.AreEqual(11,catalog.fighters.Length);
   foreach(var prefab in catalog.fighters){
    var animator=prefab.GetComponentInChildren<Animator>();var controller=animator.runtimeAnimatorController as UnityEditor.Animations.AnimatorController;Assert.NotNull(controller,prefab.name);
    var states=controller.layers[0].stateMachine.states.ToDictionary(x=>x.state.name,x=>x.state.motion);
    Assert.AreNotSame(states["Idle"],states["Guard"],prefab.name+": block must visibly differ from ready stance");
    foreach(var pair in new[]{new[]{"jab","CrouchJab"},new[]{"cross","CrouchCross"},new[]{"body","CrouchBody"},new[]{"uppercut","CrouchUppercut"}})Assert.AreSame(states[pair[0]],states[pair[1]],prefab.name+": low strike retains full source torso/heading");
    var grounding=new SerializedObject(prefab.GetComponent<CandidateFootGrounding>());var configured=grounding.FindProperty("signedFloorStates");var names=Enumerable.Range(0,configured.arraySize).Select(i=>configured.GetArrayElementAtIndex(i).stringValue).ToArray();
    CollectionAssert.AreEquivalent(new[]{"Crouch","CrouchJab","CrouchCross","CrouchBody","CrouchUppercut"},names,prefab.name+": return to crouch keeps foot support");Assert.That(grounding.FindProperty("maxDownCorrection").floatValue,Is.InRange(.20f,.30f),prefab.name+": measured support cap remains scoped and bounded");
   }
  }
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
    Assert.AreEqual(Animator.StringToHash("CrouchUppercut"),anim.GetCurrentAnimatorStateInfo(0).shortNameHash);Assert.Greater(anim.GetCurrentAnimatorStateInfo(0).normalizedTime,.4f,"Low strikes must advance the original full attack, including torso twist");
    Assert.AreEqual(0,anim.GetLayerWeight(layer),"Arms-only override must not remove the original strike heading");
    int low=anim.GetLayerIndex("Review source shallow duck"),idle=anim.GetLayerIndex("Review source idle duck");Assert.GreaterOrEqual(low,0);Assert.GreaterOrEqual(idle,0);Assert.AreEqual(.55f,anim.GetLayerWeight(low),.00001f);Assert.AreEqual(0,anim.GetLayerWeight(idle));
    m.A.Attack=null;view.PoseTowards(m.A,m.B,.1f);Assert.AreEqual(Animator.StringToHash("Crouch"),anim.GetCurrentAnimatorStateInfo(0).shortNameHash);Assert.AreEqual(Animator.StringToHash(Layer+".SourceGuard"),anim.GetCurrentAnimatorStateInfo(layer).fullPathHash);
   }finally{foreach(var lease in leases)lease.Dispose();Object.Destroy(actor);}
   yield return null;yield return new ExitPlayMode();
  }
 }
}

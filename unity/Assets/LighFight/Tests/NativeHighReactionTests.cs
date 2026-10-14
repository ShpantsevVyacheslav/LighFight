using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using LighFight.Core;
using LighFight.Runtime;
namespace LighFight.Tests {
 public sealed class NativeHighReactionTests {
  [TestCase(0)][TestCase(3)][TestCase(4)][TestCase(5)]
  public void HeadReactionIsPerActorAndRestoresBodyReaction(int index) {
   var catalog=AssetDatabase.LoadAssetAtPath<GameContent>("Assets/LighFight/Content/GameContent.asset");
   var actor=Object.Instantiate(catalog.fighters[index]);
   var other=Object.Instantiate(catalog.fighters[index]);
   try {
    var reaction=actor.GetComponent<NativeHighReaction>();Assert.NotNull(reaction);
    var animator=actor.GetComponentInChildren<Animator>();var original=animator.runtimeAnimatorController;
    var state=new Fighter{Stun=18,ReactionDuration=18,LastHitLevel=Level.High};
    reaction.SelectFor(state);Assert.True(reaction.HeadActive);
    Assert.AreNotSame(original,animator.runtimeAnimatorController);
    Assert.AreSame(original,other.GetComponentInChildren<Animator>().runtimeAnimatorController);
    state.LastHitLevel=Level.Mid;reaction.SelectFor(state);Assert.False(reaction.HeadActive);
    state.LastHitLevel=Level.High;state.LastBlocked=true;reaction.SelectFor(state);Assert.False(reaction.HeadActive);
    state.LastBlocked=false;state.Stun=0;reaction.SelectFor(state);Assert.False(reaction.HeadActive);
    Assert.AreEqual(18,state.ReactionDuration);Assert.AreEqual(0,state.Stun);
   } finally {Object.DestroyImmediate(actor);Object.DestroyImmediate(other);}
  }
 }
}

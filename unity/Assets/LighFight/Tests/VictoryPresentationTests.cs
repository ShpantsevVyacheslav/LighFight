using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using LighFight.Runtime;
namespace LighFight.Tests {
 public sealed class VictoryPresentationTests {
  [Test] public void CompletedPreviewHoldsItsLastFrameWhileDanceLoops() {
   Assert.That(FighterView.NativePreviewPhase(10,2,false),Is.InRange(.999f,1f));
   Assert.That(FighterView.NativePreviewPhase(10.5f,2,true),Is.EqualTo(.25f).Within(.00001f));
   Assert.AreEqual(0,FighterView.NativePreviewPhase(-1,2,false));
  }
  [TestCase(0,"Victory")][TestCase(3,"Greeting")][TestCase(4,"Victory")][TestCase(5,"Victory")]
  public void WinnerUsesAnAvailableAuthoredAnimation(int index,string state) {
   var catalog=AssetDatabase.LoadAssetAtPath<GameContent>("Assets/LighFight/Content/GameContent.asset");
   Assert.AreEqual(state,Prototype.VictoryMotion(catalog.fighters[index].GetComponent<FighterView>()));
  }
 }
}

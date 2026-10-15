using NUnit.Framework;
using System.Reflection;
using UnityEditor.Animations;
using LighFight.Core;
using UnityEditor;
using UnityEngine;
using LighFight.Runtime;
namespace LighFight.Tests {
 public sealed class VictoryPresentationTests {
  [TestCase(0,0,"Fall")][TestCase(1,0,"Fall")]
  [TestCase(0,20,"Defeat")][TestCase(1,20,"Defeat")]
  public void ResultPresentationKeepsKnockoutsDownAndAllowsStandingDecisionDefeat(int winningSide,int loserHealth,string expectedState){
   var root=new GameObject("Result animation regression");
   var controller=new AnimatorController();controller.AddLayer("Base Layer");
   var clip=new AnimationClip();clip.SetCurve("",typeof(Transform),"localPosition.y",AnimationCurve.Linear(0,0,1,0));
   foreach(var name in new[]{"Idle","Fall","Defeat","Victory"})controller.layers[0].stateMachine.AddState(name).motion=clip;
   try{
    var game=root.AddComponent<Prototype>();
    var views=new FighterView[2];
    for(int i=0;i<2;i++){var actor=new GameObject("Result actor"+i);actor.transform.SetParent(root.transform);var animator=actor.AddComponent<Animator>();animator.runtimeAnimatorController=controller;views[i]=actor.AddComponent<FighterView>();views[i].ConfigureAnimation(animator,new FighterView.ContactPose[0]);views[i].UseNativeMotion(new[]{new FighterView.NativeClip{state="Defeat",clip=clip},new FighterView.NativeClip{state="Victory",clip=clip},new FighterView.NativeClip{state="Fall",clip=clip}});}
    var session=new FightSession(false,1,60);var losing=winningSide==0?session.Match.B:session.Match.A;losing.Health=0;session.Step();Assert.AreEqual(winningSide,session.Winner);losing.Health=loserHealth;
    const BindingFlags flags=BindingFlags.Instance|BindingFlags.NonPublic;
    typeof(Prototype).GetField("session",flags).SetValue(game,session);
    typeof(Prototype).GetField("player",flags).SetValue(game,views[0]);typeof(Prototype).GetField("opponent",flags).SetValue(game,views[1]);
    var camera=new GameObject("Result camera");camera.transform.SetParent(root.transform);typeof(Prototype).GetField("fightCamera",flags).SetValue(game,camera.AddComponent<Camera>());
    var update=typeof(Prototype).GetMethod("UpdateVictoryPresentation",flags);
    foreach(float delta in new[]{.05f,2f}){update.Invoke(game,new object[]{delta});Assert.True(views[1-winningSide].GetComponent<Animator>().GetCurrentAnimatorStateInfo(0).IsName("Base Layer."+expectedState),"Winner presentation must preserve the actual loss animation semantics");}
   }finally{Object.DestroyImmediate(root);Object.DestroyImmediate(controller);Object.DestroyImmediate(clip);}
  }

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

using System.Collections;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using LighFight.Core;
using LighFight.Runtime;
namespace LighFight.Tests {
 public sealed class TripoRenderParityTests {
  [UnityTest]public IEnumerator AutomaticAnimationPassKeepsPhysicalTorsoPose(){
   yield return new EnterPlayMode();
   var catalog=AssetDatabase.LoadAssetAtPath<GameContent>("Assets/LighFight/Content/GameContent.asset");float worst=0;
   foreach(int index in new[]{0,3,4,5}){
    var actor=Object.Instantiate(catalog.fighters[index]);var view=actor.GetComponent<FighterView>();
    var fighter=new Fighter{X=-.325f};var other=new Fighter{X=.325f};fighter.Start(0);fighter.Frame=Moves.All[0].Startup+Moves.All[0].Active/2;
    view.SampleContactPose(fighter,other);var hips=view.BonePoint("Hips",Vector3.zero);var chest=view.BonePoint("Spine2",Vector3.zero);
    yield return null;yield return null;
    var afterHips=view.BonePoint("Hips",Vector3.zero);var afterChest=view.BonePoint("Spine2",Vector3.zero);
    worst=Mathf.Max(worst,Vector2.Distance(new Vector2(hips.x,hips.z),new Vector2(afterHips.x,afterHips.z)),Vector2.Distance(new Vector2(chest.x,chest.z),new Vector2(afterChest.x,afterChest.z)));
    Object.Destroy(actor);yield return null;
   }
   yield return new ExitPlayMode();Assert.Less(worst,.001f,"Rendered torso drifted from the collision-sampled pose by "+worst+"m");
  }
 }
}
